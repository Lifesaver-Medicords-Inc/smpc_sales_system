using smpc_sales_app.Data;
using smpc_sales_app.Pages.Sales;
using smpc_sales_app.Services.Helpers;
using smpc_sales_app.Services.Sales;
using smpc_sales_app.Services.Sales.Models;
using smpc_sales_system.Models;
using smpc_sales_system.Services.Sales;
using smpc_sales_system.Services.Sales.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace smpc_sales_system.Pages.Sales
{
    // "Red Box" performance-metrics dashboard: two live-derived lists -
    //   1) open Quotations/Sales Orders, sorted by how long they've been sitting
    //   2) customers overdue for a follow-up (Client Retention)
    // Everything here is DERIVED from existing data (quotation/order/CRM/BPI/sales-invoice
    // records) - there is no new "Red Box" table. See the code comments below for the exact
    // rules used and the assumptions made where the source mockup's wording was ambiguous.
    public partial class RedBox : UserControl
    {
        public delegate void TriggerNewFormDelegate(string title, Control control);
        public event TriggerNewFormDelegate TriggerNewForm;

        // Auto-refresh every 5 minutes so the dashboard stays current without the user
        // having to click Refresh. Not started in the constructor - like the Load event
        // (see the comment on RefreshData() below), a tick firing before login succeeds
        // would hit the same "Set-Cookie header not found" problem. Instead it's started
        // the first time RefreshData() runs, which Layout.cs only calls once login has
        // actually completed.
        private readonly Timer _autoRefreshTimer = new Timer { Interval = 5 * 60 * 1000 };

        public RedBox()
        {
            InitializeComponent();
            _autoRefreshTimer.Tick += async (s, e) => await LoadData();
            this.Disposed += (s, e) => _autoRefreshTimer.Dispose();
        }

        private class QuoteOrderEntry
        {
            public string ClientName;
            public string ProjectName;
            public string DocumentNoDisplay; // e.g. "SO#0004" / "FQ#0012"
            public string DocumentNoRaw;     // bare number, for opening the source record
            public bool IsOrder;
            public string Status;
            public DateTime BasisDate;       // date the final quote/SO was created
            public string CommitmentDate;
        }

        private class RetentionEntry
        {
            public string Company;
            public string ContactNo;
            public DateTime LastContact;
            public string Status;
        }

        // Deliberately NOT wired to this control's own Load event. RedBox now lives inside
        // Layout's permanent right-side panel, so its Load fires as soon as the main window
        // is constructed - before (or concurrently with) the modal Login dialog, while
        // CacheData.SessionToken is still empty. Every one of the API calls in LoadData()
        // goes through RequestToApi, which - only on the very first call made with an empty
        // SessionToken - tries to read a "Set-Cookie" response header to capture the session
        // token; endpoints other than login don't send that header, so that read throws
        // ("The given header was not found.") and RequestToApi's own catch block pops up an
        // error MessageBox before this control ever gets a chance to handle it. Layout.cs
        // calls RefreshData() explicitly once login has actually succeeded instead.
        public async Task RefreshData()
        {
            await LoadData();

            if (!_autoRefreshTimer.Enabled)
                _autoRefreshTimer.Start();
        }

        private async void btn_refresh_Click(object sender, EventArgs e)
        {
            await LoadData();
        }

        private async Task LoadData()
        {
            btn_refresh.Enabled = false;
            lbl_status.Text = "Loading...";
            try
            {
                var quotationData = await QuotationService.GetQuotations();
                var orderData = await OrderService.GetOrders();
                var bpiData = await QuotationService.GetBpiCustomers();
                DataTable crm = await CRMService.GetAsDatatable();
                SalesInvoiceList invoices = null;
                try
                {
                    invoices = await SalesInvoiceService.GetSalesInvoices();
                }
                catch
                {
                    // Accounting endpoint being unreachable shouldn't take the whole
                    // dashboard down - just fall back to "nothing invoiced yet known",
                    // which only means the removal-on-invoice rule won't fire this refresh.
                    invoices = null;
                }

                DataTable bpiGeneral = JsonHelper.ToDataTable(bpiData?.general);

                var invoicedOrderDetailIds = new HashSet<int>(
                    (invoices?.sales_invoice_details ?? new List<SalesInvoiceDetailsModel>())
                        .Select(d => d.sales_order_details_id));

                var quoteOrderEntries = BuildQuoteOrderEntries(quotationData, orderData, bpiGeneral, invoicedOrderDetailIds);
                RenderQuoteOrderSection(quoteOrderEntries);

                var retentionEntries = BuildRetentionEntries(crm);
                RenderRetentionSection(retentionEntries);

                // Temporary diagnostic breakdown while we track down why nothing is
                // showing: total orders fetched / how many are ACTIVE / how many of
                // those survived the invoiced-removal check / how many order-detail
                // lines came back in total / how many distinct order_details_id values
                // the invoiced-lookup set actually contains.
                int totalOrders = orderData?.order?.Count ?? 0;
                int activeOrders = orderData?.order?.Count(o => string.Equals(o.status, "ACTIVE", StringComparison.OrdinalIgnoreCase)) ?? 0;
                int shownOrders = quoteOrderEntries.Count(en => en.IsOrder);
                int totalLines = orderData?.sales_order_details?.Count ?? 0;
                lbl_status.Text = $"{DateTime.Now:h:mm tt} - SO {shownOrders}/{activeOrders}/{totalOrders} - lines {totalLines} - inv {invoicedOrderDetailIds.Count}";
            }
            catch (Exception ex)
            {
                lbl_status.Text = "Failed to load: " + ex.Message;
            }
            finally
            {
                btn_refresh.Enabled = true;
            }
        }

        // ------------------------------------------------------------------
        // SECTION 1: Quotes and Sales Orders
        // ------------------------------------------------------------------
        //
        // Rules implemented (see chat summary for the full list of assumptions):
        //  - A finalized-or-not Quotation shows as "FQ#..." until it's converted into a
        //    Sales Order (i.e. until some Order.quotation_id points back to it); once
        //    converted, only the Order entry ("SO#...") is shown, not the quotation.
        //  - FQ status: QUOTED once is_finalized, otherwise BIDDING.
        //  - SO status: PREPARING while any line still needs procurement (order-detail
        //    status = CANVASS), otherwise DISPATCHING. There's no signal in the current
        //    schema to distinguish "out for delivery" from "billing in progress" beyond
        //    that, so this is a two-state simplification of the mockup's three states.
        //  - Removed entirely once the order is invoiced (has a Sales Invoice line
        //    referencing one of its order-detail rows - this is what "PV na" was mapped
        //    to, per your confirmation that PV meant "already invoiced").
        //  - Removed if Order.status isn't "ACTIVE" (covers CANCELLED and anything else
        //    non-active as "closed").
        //  - Sorted by longest time elapsed first, where "time elapsed" is measured from
        //    the Order's creation date if one exists, otherwise the Quotation's date.
        //  - Only shows records the CURRENT logged-in user created themselves, not the
        //    whole sales team's. Quotation.created_by and Order.sales_executive are both
        //    saved as the plain "Firstname Lastname" string of whoever was logged in at
        //    creation time (there's no numeric user id backing either field) - so that's
        //    what's compared here, not CacheData.CurrentUser.employee_id (that's a
        //    different field used elsewhere for BPI/CRM's sales_id matching, and would
        //    silently match nothing if used here). Note sales_executive is only stamped on
        //    an order's very first save, so an order resaved/edited later still keeps its
        //    original creator for this filter, which is the intended behavior.
        private List<QuoteOrderEntry> BuildQuoteOrderEntries(
            SalesQuotationList quotationData,
            OrderList orderData,
            DataTable bpiGeneral,
            HashSet<int> invoicedOrderDetailIds)
        {
            var result = new List<QuoteOrderEntry>();

            string currentUserName = (CacheData.CurrentUser != null)
                ? $"{CacheData.CurrentUser.first_name} {CacheData.CurrentUser.last_name}".Trim()
                : null;

            var orders = orderData?.order ?? new List<OrderModel>();
            var orderDetails = orderData?.sales_order_details ?? new List<OrderDetailsModel>();
            var convertedQuotationIds = new HashSet<int>(
                orders.Where(o => o.quotation_id > 0).Select(o => (int)o.quotation_id));

            foreach (var order in orders)
            {
                if (!string.Equals(order.status, "ACTIVE", StringComparison.OrdinalIgnoreCase))
                    continue;

                if (!string.IsNullOrEmpty(currentUserName) &&
                    !string.Equals(order.sales_executive, currentUserName, StringComparison.OrdinalIgnoreCase))
                    continue;

                var lines = orderDetails.Where(d => d.based_id == (int)order.order_id).ToList();
                bool invoiced = lines.Any(l => invoicedOrderDetailIds.Contains(l.order_details_id));
                if (invoiced)
                    continue;

                bool needsProcurement = lines.Any(l => string.Equals(l.status, "CANVASS", StringComparison.OrdinalIgnoreCase));

                DateTime basisDate;
                if (!DateTime.TryParse(order.date, out basisDate))
                    basisDate = DateTime.Now;

                result.Add(new QuoteOrderEntry
                {
                    ClientName = LookupBranchName(bpiGeneral, (int)order.customer_id),
                    ProjectName = string.IsNullOrWhiteSpace(order.project_name) ? "-" : order.project_name,
                    DocumentNoDisplay = "SO#" + StripDocPrefix(order.doc),
                    DocumentNoRaw = StripDocPrefix(order.doc),
                    IsOrder = true,
                    Status = needsProcurement ? "PREPARING" : "DISPATCHING",
                    BasisDate = basisDate,
                    CommitmentDate = FormatMonthYear(order.delivery_date)
                });
            }

            var quotations = quotationData?.SalesQuotation ?? new List<SalesQuotationModel>();
            foreach (var q in quotations)
            {
                if (convertedQuotationIds.Contains(q.id))
                    continue;

                if (!string.IsNullOrEmpty(currentUserName) &&
                    !string.Equals(q.created_by, currentUserName, StringComparison.OrdinalIgnoreCase))
                    continue;

                DateTime basisDate;
                if (!DateTime.TryParse(q.date, out basisDate))
                    basisDate = DateTime.Now;

                // "FQ#" is this app's actual designation for a FINALIZED quotation - an
                // unfinalized one is just "Q#". Using "FQ#" here regardless of is_finalized
                // was the bug: it displayed "FQ#0012" for a still-BIDDING quote, but opening
                // it landed on a record the Quotation screen itself identifies as "Q#0012",
                // which looked like the link was going to a different document.
                string docPrefix = q.is_finalized ? "FQ#" : "Q#";

                result.Add(new QuoteOrderEntry
                {
                    ClientName = LookupBranchName(bpiGeneral, q.customer_id),
                    ProjectName = string.IsNullOrWhiteSpace(q.project_name) ? "-" : q.project_name,
                    DocumentNoDisplay = docPrefix + StripDocPrefix(q.document_no),
                    DocumentNoRaw = StripDocPrefix(q.document_no),
                    IsOrder = false,
                    Status = q.is_finalized ? "QUOTED" : "BIDDING",
                    BasisDate = basisDate,
                    CommitmentDate = "-"
                });
            }

            return result.OrderByDescending(en => DateTime.Now - en.BasisDate).ToList();
        }

        // ------------------------------------------------------------------
        // SECTION 2: Client Retention
        // ------------------------------------------------------------------
        //
        // vw_get_CRM already collapses each BPI branch down to its single latest CRM
        // remark (ROW_NUMBER() ... WHERE rn = 1), so CRMService.GetAsDatatable() gives one
        // row per branch with that branch's most recent contact date - no extra grouping
        // needed here. This only counts CRM remarks as "contact", not orders/purchases
        // (the mockup says "contact/purchase") - cross-referencing order dates would need
        // resolving a customer_id/BPI-branch-id mismatch between the Order and CRM tables
        // that isn't worth the risk of misattributing a company without verifying it first.
        //
        //  - Enters the list once 7+ days have passed since that last CRM remark.
        //  - Status: RECONNECT while under 3 months idle, SUBJECT FOR RETURN at 3+ months
        //    (mockup's cue to reassign to another sales person - this only labels it that
        //    way; it does NOT automatically clear/reassign BPI.sales_id, since that's a
        //    real write action against customer ownership that should be a deliberate,
        //    reviewed step rather than something a dashboard silently does on load).
        //  - Sorted by longest idle time first.
        //  - The mockup's "**time elapsed sa red box will always start with 1 M 0D" note
        //    describes a specific display convention (counter pinned to entering the box,
        //    not raw calendar time since contact) that isn't implemented literally here -
        //    TIME ELAPSED below is the actual calendar time since the last CRM remark.
        private List<RetentionEntry> BuildRetentionEntries(DataTable crm)
        {
            var result = new List<RetentionEntry>();
            if (crm == null)
                return result;

            foreach (DataRow row in crm.Rows)
            {
                DateTime lastContact;
                if (!DateTime.TryParse(row["date"]?.ToString(), out lastContact))
                    continue;

                double daysSince = (DateTime.Now - lastContact).TotalDays;
                if (daysSince < 7)
                    continue;

                result.Add(new RetentionEntry
                {
                    Company = row["branch_name"]?.ToString(),
                    ContactNo = row["number"]?.ToString(),
                    LastContact = lastContact,
                    Status = daysSince >= 90 ? "SUBJECT FOR RETURN" : "RECONNECT"
                });
            }

            return result.OrderByDescending(r => DateTime.Now - r.LastContact).ToList();
        }

        // ------------------------------------------------------------------
        // Rendering
        // ------------------------------------------------------------------

        private void RenderQuoteOrderSection(List<QuoteOrderEntry> entries)
        {
            pnl_quotes.SuspendLayout();
            pnl_quotes.Controls.Clear();
            if (entries.Count == 0)
            {
                pnl_quotes.Controls.Add(MakeEmptyLabel("Nothing open right now."));
            }
            else
            {
                foreach (var entry in entries)
                    pnl_quotes.Controls.Add(BuildQuoteOrderCard(entry));
            }
            pnl_quotes.ResumeLayout();
        }

        private void RenderRetentionSection(List<RetentionEntry> entries)
        {
            pnl_retention.SuspendLayout();
            pnl_retention.Controls.Clear();
            if (entries.Count == 0)
            {
                pnl_retention.Controls.Add(MakeEmptyLabel("No customers overdue for a follow-up."));
            }
            else
            {
                foreach (var entry in entries)
                    pnl_retention.Controls.Add(BuildRetentionCard(entry));
            }
            pnl_retention.ResumeLayout();
        }

        // This control is mounted inside the existing right-side "RED BOX" panel, which is
        // narrow (~300px) - fields are laid out 2-per-row (label/value pairs side by side,
        // matching the original mockup) rather than one giant single column.
        //
        // Built entirely out of FlowLayoutPanels nested three deep (card -> row -> field
        // block), deliberately avoiding TableLayoutPanel here: a TableLayoutPanel combined
        // with AutoSize=true and Percent column widths does not reliably keep two columns
        // side by side (it collapsed back to a single column when tried) - FlowLayoutPanel's
        // AutoSize behavior is the one that's actually proven to work in this app (it's what
        // fixed the earlier empty-card bug), so the whole card structure sticks to it.
        private static readonly Color CardBackColor = Color.MistyRose;
        private static readonly Color HeaderColor = Color.FromArgb(150, 20, 20);
        private static readonly int CardWidth = 264;
        private static readonly int CardColumnWidth = (CardWidth - 16) / 2 - 4;

        private FlowLayoutPanel BuildQuoteOrderCard(QuoteOrderEntry entry)
        {
            FlowLayoutPanel card = StartCard();

            AddFieldRow(card, "CLIENT NAME", MakeValueLabel(entry.ClientName), "PROJECT NAME", MakeValueLabel(entry.ProjectName));

            var docLink = new LinkLabel
            {
                Text = entry.DocumentNoDisplay,
                AutoSize = true,
                Font = new Font("Segoe UI", 8.5F)
            };
            docLink.LinkClicked += (s, e) => OpenQuoteOrder(entry);
            AddFieldRow(card, "DOCUMENT NO.", docLink, "STATUS", MakeValueLabel(entry.Status));

            AddFieldRow(card, "TIME ELAPSED", MakeValueLabel(FormatElapsed(entry.BasisDate)), "COMMITMENT DATE", MakeValueLabel(entry.CommitmentDate));

            return card;
        }

        private FlowLayoutPanel BuildRetentionCard(RetentionEntry entry)
        {
            FlowLayoutPanel card = StartCard();

            AddFieldRow(card, "COMPANY", MakeValueLabel(entry.Company), "CONTACT NO.", MakeValueLabel(entry.ContactNo));
            AddFieldRow(card, "LAST CONTACT", MakeValueLabel(entry.LastContact.ToString("M/d/yy")), "TIME ELAPSED", MakeValueLabel(FormatElapsed(entry.LastContact)));
            AddFieldRow(card, "STATUS", MakeValueLabel(entry.Status));

            return card;
        }

        private FlowLayoutPanel StartCard()
        {
            return new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                BackColor = CardBackColor,
                BorderStyle = BorderStyle.FixedSingle,
                Padding = new Padding(8),
                Margin = new Padding(4),
                MinimumSize = new Size(CardWidth, 0),
                MaximumSize = new Size(CardWidth, 0)
            };
        }

        // One row = up to two field blocks placed side by side (LeftToRight flow).
        private void AddFieldRow(FlowLayoutPanel card, string header1, Control value1, string header2 = null, Control value2 = null)
        {
            var row = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Margin = new Padding(0, 0, 0, 4)
            };
            row.Controls.Add(BuildFieldBlock(header1, value1));
            if (header2 != null)
                row.Controls.Add(BuildFieldBlock(header2, value2));

            card.Controls.Add(row);
        }

        // One field block = a header label stacked above its value, pinned to a fixed
        // column width so two of these sit side by side without overflowing the card.
        private FlowLayoutPanel BuildFieldBlock(string header, Control valueControl)
        {
            var block = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                MinimumSize = new Size(CardColumnWidth, 0),
                MaximumSize = new Size(CardColumnWidth, 0),
                Margin = new Padding(0, 0, 4, 0),
                Padding = new Padding(0)
            };

            var lbl = new Label
            {
                Text = header,
                AutoSize = true,
                Font = new Font("Segoe UI", 7F, FontStyle.Bold),
                ForeColor = HeaderColor,
                Margin = new Padding(0)
            };
            valueControl.Margin = new Padding(0);
            valueControl.MaximumSize = new Size(CardColumnWidth, 0);

            block.Controls.Add(lbl);
            block.Controls.Add(valueControl);
            return block;
        }

        private Label MakeValueLabel(string text)
        {
            return new Label
            {
                Text = string.IsNullOrWhiteSpace(text) ? "-" : text,
                AutoSize = true,
                Font = new Font("Segoe UI", 9F)
            };
        }

        private Label MakeEmptyLabel(string text)
        {
            return new Label
            {
                Text = text,
                AutoSize = true,
                Margin = new Padding(10),
                ForeColor = Color.Gray,
                Font = new Font("Segoe UI", 9F, FontStyle.Italic)
            };
        }

        // ------------------------------------------------------------------
        // Navigation
        // ------------------------------------------------------------------

        private void OpenQuoteOrder(QuoteOrderEntry entry)
        {
            if (entry.IsOrder)
            {
                Orders ordersPage = new Orders(entry.DocumentNoRaw);
                TriggerNewForm?.Invoke(entry.DocumentNoDisplay, ordersPage);
            }
            else
            {
                Quotation quotationPage = new Quotation(entry.DocumentNoRaw);
                // Reuse the exact same prefix shown on the card (DocumentNoDisplay is
                // already "FQ#..." or "Q#..." depending on is_finalized) rather than
                // hardcoding "FQ#" again here, so the opened tab's title can never drift
                // from what the link itself said.
                TriggerNewForm?.Invoke(entry.DocumentNoDisplay, quotationPage);
            }
        }

        // ------------------------------------------------------------------
        // Formatting helpers
        // ------------------------------------------------------------------

        private string LookupBranchName(DataTable bpiGeneral, int customerId)
        {
            if (bpiGeneral == null || !bpiGeneral.Columns.Contains("general_based_id"))
                return "Unknown";
            DataRow[] rows = bpiGeneral.Select($"general_based_id = '{customerId}'");
            return rows.Length > 0 ? rows[0]["branch_name"]?.ToString() : "Unknown";
        }

        private static string StripDocPrefix(string doc)
        {
            if (string.IsNullOrEmpty(doc))
                return doc;
            if (doc.StartsWith("FQ#")) return doc.Substring(3);
            if (doc.StartsWith("SO#")) return doc.Substring(3);
            if (doc.StartsWith("Q#")) return doc.Substring(2);
            return doc;
        }

        private static string FormatMonthYear(string dateStr)
        {
            DateTime dt;
            if (string.IsNullOrWhiteSpace(dateStr) || !DateTime.TryParse(dateStr, out dt))
                return "-";
            return dt.ToString("MMMM yyyy").ToUpperInvariant();
        }

        private static string FormatElapsed(DateTime from)
        {
            DateTime now = DateTime.Now;
            if (from > now)
                from = now;

            int months = ((now.Year - from.Year) * 12) + now.Month - from.Month;
            DateTime approx = from.AddMonths(months);
            if (approx > now)
            {
                months--;
                approx = from.AddMonths(months);
            }
            int days = (now - approx).Days;
            return $"{months} M, {days} D";
        }
    }
}
