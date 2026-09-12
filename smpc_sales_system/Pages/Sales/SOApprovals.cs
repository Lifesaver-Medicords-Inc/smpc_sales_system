using smpc_app.Services.Helpers;
using smpc_sales_app.Data;
using smpc_sales_app.Pages.Sales;
using smpc_sales_app.Services.Helpers;
using smpc_sales_app.Services.Sales;
using smpc_sales_system.Printing;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows.Forms;

namespace smpc_sales_system.Pages.Sales
{
    // SO Approval List (spec 3.4, 7.2, 3.2's Sales and Admin rows).
    //
    // A sales order is invisible to every other department until it is approved
    // (CLAUDE.md invariant 1), and until now the only way to approve one was to
    // open it and press Check on the order form. That works if you already know
    // which order is waiting; nothing told the Sales Manager or CBDO that anything
    // was. This is that missing queue.
    //
    // Approving here sends exactly the payload the order form's Check button sends
    // - same status, same approver stamping, same detail rows - so there is one
    // approval behaviour, not two that can drift. The server-side access gate in
    // order_service.go is what actually authorises it; hiding the button is a
    // courtesy.
    public partial class SOApprovals : UserControl
    {
        public delegate void TriggerNewFormDelegate(string title, Control control);
        public event TriggerNewFormDelegate TriggerNewForm;

        // Must match the constant of the same name in Orders.cs and the API's
        // order_service.go.
        private const string OrderApproveAccessCode = "Sales - Order.Orders.Approve";

        private DataTable _orders = new DataTable();
        private DataTable _details = new DataTable();
        private readonly DataTable _view = new DataTable();

        public SOApprovals()
        {
            InitializeComponent();
            BuildColumns();

            btn_refresh.Click += async (s, e) => await LoadAsync();
            btn_approve.Click += async (s, e) => await ApproveSelectedAsync();
            btn_open.Click += (s, e) => OpenSelected();
            btn_print.Click += btn_print_Click;
            tabs.SelectedIndexChanged += (s, e) => Rebind();
            txt_search.TextChanged += (s, e) => Rebind();
            dgv_orders.CellDoubleClick += (s, e) => { if (e.RowIndex >= 0) OpenSelected(); };
        }

        protected override async void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            await LoadAsync();
        }

        private void BuildColumns()
        {
            _view.Columns.Add("SO #", typeof(string));
            _view.Columns.Add("DOC DATE", typeof(string));
            _view.Columns.Add("CUSTOMER", typeof(string));
            _view.Columns.Add("PROJECT NAME", typeof(string));
            _view.Columns.Add("SALES EXECUTIVE", typeof(string));
            _view.Columns.Add("RECEIVER", typeof(string));
            _view.Columns.Add("TOTAL AMOUNT DUE", typeof(double));
            _view.Columns.Add("APPROVED BY", typeof(string));
            _view.Columns.Add("STATUS", typeof(string));
            // Carried for the row actions, never shown.
            _view.Columns.Add("_doc", typeof(string));
            _view.Columns.Add("_order_id", typeof(int));
        }

        // Spec 7.2 gives this screen its own vocabulary - PENDING, ONGOING,
        // FINISHED - over the same stored status the sales SO List shows as
        // FOR APPROVAL / ACTIVE / CLOSED. The two are deliberately different and
        // must not be harmonised (CLAUDE.md invariant 11), so the mapping lives
        // here rather than anything being renamed in the database.
        //
        // Orders created today are stored with "-" (Orders.cs's SetDefaultIfEmpty)
        // rather than an explicit FOR APPROVAL, so anything not yet ACTIVE, CLOSED
        // or CANCELLED counts as awaiting approval. That reads the existing data
        // correctly without changing what the order form writes.
        private static string ApprovalStatus(string stored)
        {
            string value = (stored ?? "").Trim().ToUpperInvariant();
            switch (value)
            {
                case "ACTIVE": return "ONGOING";
                case "CLOSED": return "FINISHED";
                case "CANCELLED": return "CANCELLED";
                default: return "PENDING";
            }
        }

        private string SelectedTabStatus()
        {
            if (tabs.SelectedTab == tab_ongoing) return "ONGOING";
            if (tabs.SelectedTab == tab_finished) return "FINISHED";
            return "PENDING";
        }

        private async System.Threading.Tasks.Task LoadAsync()
        {
            lbl_loading.Visible = true;
            try
            {
                var data = await OrderService.GetOrders();
                _orders = data?.order != null ? JsonHelper.ToDataTable(data.order) : new DataTable();
                _details = data?.sales_order_details != null && data.sales_order_details.Any()
                    ? JsonHelper.ToDataTable(data.sales_order_details)
                    : new DataTable();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Could not load the order list: " + ex.Message, "SO Approvals",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                _orders = new DataTable();
            }
            finally
            {
                lbl_loading.Visible = false;
            }

            Rebind();
        }

        private static string Text(DataRow row, string column)
        {
            return _HasColumn(row, column) && row[column] != DBNull.Value ? row[column].ToString() : "";
        }

        private static bool _HasColumn(DataRow row, string column)
        {
            return row.Table.Columns.Contains(column);
        }

        private void Rebind()
        {
            _view.Rows.Clear();

            string wanted = SelectedTabStatus();
            string search = (txt_search.Text ?? "").Trim();

            foreach (DataRow row in _orders.Rows)
            {
                if (ApprovalStatus(Text(row, "status")) != wanted) continue;

                string docNo = Text(row, "document_no");
                if (string.IsNullOrWhiteSpace(docNo)) docNo = DocumentNo.Apply(Text(row, "doc"), "SO#");

                string customer = Text(row, "customer_name");
                string project = Text(row, "project_name");
                string executive = Text(row, "sales_executive");

                if (search.Length > 0)
                {
                    string haystack = (docNo + " " + customer + " " + project + " " + executive).ToLowerInvariant();
                    if (!haystack.Contains(search.ToLowerInvariant())) continue;
                }

                double total;
                double.TryParse(Text(row, "total_amount_due"), out total);

                int orderId;
                int.TryParse(Text(row, "order_id"), out orderId);

                _view.Rows.Add(
                    docNo,
                    Text(row, "date"),
                    customer,
                    project,
                    executive,
                    Text(row, "receiver"),
                    total,
                    Text(row, "approved_by"),
                    wanted,
                    Text(row, "doc"),
                    orderId);
            }

            dgv_orders.DataSource = _view;
            if (dgv_orders.Columns.Contains("_doc")) dgv_orders.Columns["_doc"].Visible = false;
            if (dgv_orders.Columns.Contains("_order_id")) dgv_orders.Columns["_order_id"].Visible = false;
            if (dgv_orders.Columns.Contains("TOTAL AMOUNT DUE"))
            {
                dgv_orders.Columns["TOTAL AMOUNT DUE"].DefaultCellStyle.Format = "N2";
                dgv_orders.Columns["TOTAL AMOUNT DUE"].DefaultCellStyle.Alignment =
                    DataGridViewContentAlignment.MiddleRight;
            }

            lbl_count.Text = _view.Rows.Count + " order(s) " + wanted.ToLowerInvariant();

            // Approving is only meaningful on the pending queue, and only for a user
            // whose position carries the code. Hidden rather than greyed, the same
            // way Orders.cs hides Check: a disabled button still advertises an action
            // an ordinary sales user will never have.
            bool canApprove = HasAccessCode(OrderApproveAccessCode);
            btn_approve.Visible = canApprove;
            btn_approve.Enabled = canApprove && wanted == "PENDING" && _view.Rows.Count > 0;
        }

        private bool HasAccessCode(string code)
        {
            var access = CacheData.CurrentUser?.position?.access;
            if (access == null) return false;
            return access.Any(a => string.Equals(a?.code, code, StringComparison.OrdinalIgnoreCase));
        }

        private DataRow SelectedRow()
        {
            var bound = dgv_orders.CurrentRow?.DataBoundItem as DataRowView;
            return bound?.Row;
        }

        // Opens the order itself in a new tab, the same way RedBox's document links
        // and the SO's own remarks reference do (spec 2.1: a document number is a
        // hyperlink and following it opens a new tab).
        private void OpenSelected()
        {
            var row = SelectedRow();
            if (row == null) return;

            string doc = row["_doc"]?.ToString();
            if (string.IsNullOrWhiteSpace(doc)) return;

            var page = new Orders(doc);
            TriggerNewForm?.Invoke(DocumentNo.Apply(doc, "SO#"), page);
        }

        private async System.Threading.Tasks.Task ApproveSelectedAsync()
        {
            var row = SelectedRow();
            if (row == null)
            {
                MessageBox.Show("Select an order to approve.", "SO Approvals",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string doc = row["_doc"]?.ToString();
            int orderId = row["_order_id"] != DBNull.Value ? Convert.ToInt32(row["_order_id"]) : 0;
            string docNo = row["SO #"]?.ToString();

            if (orderId == 0 || string.IsNullOrWhiteSpace(doc))
            {
                MessageBox.Show("That row is missing its order reference and cannot be approved here. "
                    + "Open the order and approve it from the form.", "SO Approvals",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (MessageBox.Show("Approve " + docNo + "? It becomes visible to every department.",
                    "SO Approvals", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            {
                return;
            }

            // The same detail payload Orders.cs sends: order_details_id + based_id per
            // line, and deliberately no per-line "status" - that column belongs to the
            // server's own recompute (sp_RecomputeSoItemStatus), and writing a coarse
            // value over it here would stomp the real 7.1 status.
            var detailRows = _details.Columns.Contains("based_id")
                ? _details.Select("based_id = " + orderId)
                : new DataRow[0];

            if (detailRows.Length == 0)
            {
                MessageBox.Show("That order has no detail lines to update. Open it and use Check "
                    + "on the order form so the lines are loaded first.", "SO Approvals",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var children = new List<Dictionary<string, dynamic>>();
            foreach (DataRow detail in detailRows)
            {
                int detailId;
                int.TryParse(Text(detail, "order_details_id"), out detailId);
                children.Add(new Dictionary<string, dynamic>
                {
                    { "order_details_id", detailId },
                    { "based_id", orderId },
                });
            }

            string approver = CacheData.CurrentUser != null
                ? (CacheData.CurrentUser.first_name + " " + CacheData.CurrentUser.last_name).Trim()
                : string.Empty;

            var payload = new Dictionary<string, dynamic>
            {
                { "doc", doc },
                { "order_id", orderId },
                { "status", "ACTIVE" },
                { "approved_by", approver },
                { "approved_by_id", CacheData.CurrentUser != null ? CacheData.CurrentUser.id : 0 },
                { "sales_order_details", children },
            };

            var response = await OrderService.Update(payload);
            if (response == null || !response.Success)
            {
                MessageBox.Show("Approval failed: " + (response?.message ?? "no response from the server."),
                    "SO Approvals", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            await LoadAsync();
        }

        // Lists print on the Class A house template (spec 2.10).
        private void btn_print_Click(object sender, EventArgs e)
        {
            if (_view.Rows.Count == 0)
            {
                MessageBox.Show("Nothing to print.", "SO Approvals",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var report = HouseTemplateReport.FromGrid(dgv_orders, "SO APPROVAL LIST - " + SelectedTabStatus());
            new PrintPreview(report).ShowDialog();
        }
    }
}
