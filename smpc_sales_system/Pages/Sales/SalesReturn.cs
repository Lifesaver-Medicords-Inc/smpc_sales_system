using smpc_app.Services.Helpers;
using smpc_sales_app.Pages.Sales.Modal;
using smpc_sales_app.Services.Sales;
using smpc_sales_system.Services.Sales.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace smpc_sales_app.Pages.Sales
{
    // Sales Return (SRT#), spec §5.13. Phase 1 item 1.3, CRUD pass - layout
    // was already in place from the earlier design-only pass (see
    // SalesReturn.Designer.cs). This wires it to /api/sales-returns (Go,
    // item 1.1).
    //
    // The Go API only exposes Create and Approve for this document - there
    // is no update route (routes/sales_return_route.go). That shapes what
    // "Edit" means here: it only ever applies to a brand-new, unsaved
    // record; an already-persisted Sales Return cannot be edited at all,
    // same as Purchase Return/Credit Memo/Debit Memo. See btn_edit_Click.
    //
    // Two reference-document paths, deliberately asymmetric (see the plan
    // this was built from): a Sales Invoice line already carries its own
    // unit_price, so picking one is a single fetch. A Delivery Receipt line
    // carries no price at all - resolving one means a second fetch against
    // Accounting's customer_so view, joined on sales_order_details_id. See
    // PopulateGridFromSalesInvoice / PopulateGridFromDeliveryReceiptAsync.
    public partial class SalesReturn : UserControl
    {
        private SalesReturnGet _data = new SalesReturnGet();
        private int _currentIndex = -1;
        private bool _isEditing = false;
        private bool _suppressGridEvents = false;
        private string _lastRefDocType = "--Select--";

        public SalesReturn()
        {
            InitializeComponent();
            WireEvents();
            SetEditMode(false);
            _ = LoadRecordsAsync();
        }

        private void WireEvents()
        {
            btn_new.Click += btn_new_Click;
            btn_search.Click += btn_search_Click;
            btn_edit.Click += btn_edit_Click;
            btn_prev.Click += btn_prev_Click;
            btn_next.Click += btn_next_Click;
            btn_save.Click += btn_save_Click;
            btn_cancel.Click += btn_cancel_Click;
            btn_approve.Click += btn_approve_Click;
            btn_generate_credit_memo.Click += btn_generate_credit_memo_Click;
            cmb_ref_doc_type.SelectedIndexChanged += cmb_ref_doc_type_SelectedIndexChanged;
            txt_ref_doc_no.Click += txt_ref_doc_no_Click;
            dgv_sales_return_details.CellValueChanged += dgv_sales_return_details_CellValueChanged;
            dgv_sales_return_details.CurrentCellDirtyStateChanged += dgv_sales_return_details_CurrentCellDirtyStateChanged;
        }

        // ---------------------------------------------------------------
        // Load / navigate
        // ---------------------------------------------------------------

        private async Task LoadRecordsAsync(uint? selectId = null)
        {
            Helpers.Loading.ShowLoading(this, "Loading Sales Returns...");
            try
            {
                _data = await SalesReturnService.GetSalesReturns() ?? new SalesReturnGet();
            }
            finally
            {
                Helpers.Loading.HideLoading(this);
            }

            if (_data.sales_return.Count == 0)
            {
                _currentIndex = -1;
                ClearForm();
                SetEditMode(false);
                return;
            }

            int index;
            if (selectId.HasValue)
            {
                int found = _data.sales_return.FindIndex(r => r.id == selectId.Value);
                index = found >= 0 ? found : _data.sales_return.Count - 1;
            }
            else
            {
                index = _data.sales_return.Count - 1;
            }

            BindRecord(index);
        }

        private void BindRecord(int index)
        {
            _currentIndex = index;
            var h = _data.sales_return[index];
            var details = _data.sales_return_details.Where(d => d.sales_return_id == h.id).ToList();

            txt_customer_id.Text = h.customer_id.ToString();
            txt_customer_code.Text = h.customer_code;
            txt_customer_name.Text = h.customer_name;
            txt_address.Text = h.address;
            cmb_ref_doc_type.Text = h.ref_doc_type;
            txt_ref_doc_id.Text = h.ref_doc_id.ToString();
            txt_ref_doc_no.Text = h.ref_doc_no;
            txt_document_no.Text = "SRT#" + h.doc_no;
            if (DateTime.TryParse(h.doc_date, out DateTime docDate)) dtp_date.Value = docDate;
            if (DateTime.TryParse(h.expected_returned_date, out DateTime expDate)) dtp_expected_returned_date.Value = expDate;
            txt_transaction_type.Text = h.transaction_type;
            txt_ship_to.Text = h.ship_to;
            txt_currency.Text = h.currency;
            txt_sales_period.Text = h.sales_period;
            txt_location_group.Text = h.location_group;
            txt_location_code.Text = h.location_code;
            txt_salesperson.Text = h.salesperson;
            txt_cm_reason_code.Text = h.cm_reason_code;
            txt_ref_cm_no.Text = h.ref_cm_no;
            txt_approved_by.Text = h.approved_by_name;
            txt_approval_date.Text = h.approval_date;
            txt_header_remarks.Text = h.header_remarks;
            txt_description.Text = h.description;
            txt_total.Text = Helpers.MoneyFormat(h.total);
            _lastRefDocType = cmb_ref_doc_type.Text;

            _suppressGridEvents = true;
            dgv_sales_return_details.Rows.Clear();
            foreach (var d in details)
            {
                int rowIndex = dgv_sales_return_details.Rows.Add();
                var row = dgv_sales_return_details.Rows[rowIndex];
                row.Cells["col_details_id"].Value = d.id;
                row.Cells["col_item_id"].Value = d.item_id;
                row.Cells["col_item_code"].Value = d.item_code;
                row.Cells["col_description"].Value = d.description;
                row.Cells["col_uom"].Value = d.unit_of_measure;
                row.Cells["col_qty_returned"].Value = d.qty_returned;
                row.Cells["col_qty_received"].Value = d.qty_received;
                row.Cells["col_qty_discrepancy"].Value = d.qty_discrepancy;
                row.Cells["col_qty_for_replacement"].Value = d.qty_for_replacement;
                row.Cells["col_qty_to_stock"].Value = d.qty_to_stock;
                row.Cells["col_qty_for_purchase_return"].Value = d.qty_for_purchase_return;
                row.Cells["col_unit_price"].Value = Helpers.MoneyFormat(d.unit_price);
                row.Cells["col_total_cost"].Value = Helpers.MoneyFormat(d.total_cost);
                row.Cells["col_reason_for_return"].Value = d.reason_for_return;
            }
            _suppressGridEvents = false;

            SetEditMode(false);
            UpdateApprovalButtons();
        }

        private void ClearForm()
        {
            txt_customer_id.Text = "";
            txt_customer_code.Text = "";
            txt_customer_name.Text = "";
            txt_address.Text = "";
            cmb_ref_doc_type.SelectedIndex = 0;
            txt_ref_doc_id.Text = "";
            txt_ref_doc_no.Text = "";
            txt_document_no.Text = "";
            dtp_date.Value = DateTime.Now;
            dtp_expected_returned_date.Value = DateTime.Now;
            txt_transaction_type.Text = "";
            txt_ship_to.Text = "";
            txt_currency.Text = "";
            txt_sales_period.Text = "";
            txt_location_group.Text = "";
            txt_location_code.Text = "";
            txt_salesperson.Text = "";
            txt_cm_reason_code.Text = "";
            txt_ref_cm_no.Text = "";
            txt_approved_by.Text = "";
            txt_approval_date.Text = "";
            txt_header_remarks.Text = "";
            txt_description.Text = "";
            txt_total.Text = "0.00";
            dgv_sales_return_details.Rows.Clear();
            lbl_title.Text = "SALES RETURN";
            _lastRefDocType = cmb_ref_doc_type.Text;
            btn_approve.Visible = false;
            btn_generate_credit_memo.Enabled = false;
        }

        // ---------------------------------------------------------------
        // Edit-mode / toolbar state
        // ---------------------------------------------------------------

        // pnl_header mixes system-derived fields (always read-only, e.g.
        // CUSTOMER NAME - it only ever comes from the reference document)
        // with the small set the user actually types into. Helpers
        // .ReadOnlyControls/ResetReadOnlyControls blanket-toggle every
        // TextBox in a panel, which would wrongly unlock the derived ones
        // too - so those genuinely-editable controls are toggled by hand
        // instead.
        private void SetEditMode(bool editing)
        {
            _isEditing = editing;

            cmb_ref_doc_type.Enabled = editing;
            dtp_expected_returned_date.Enabled = editing;
            txt_cm_reason_code.ReadOnly = !editing;
            txt_header_remarks.ReadOnly = !editing;
            txt_description.ReadOnly = !editing;
            dgv_sales_return_details.ReadOnly = !editing;

            // Visible, not just Enabled, while editing - a grayed-out-but-still-there
            // New/Search/Prev/Next read as "maybe clickable" and were confusing mid-edit.
            // Prev/Next's Enabled still reflects real record availability once they're
            // visible again (nothing to page to at either end of the list) - that's a
            // genuinely different case from "this button doesn't apply in this mode".
            btn_new.Visible = !editing;
            btn_search.Visible = !editing;
            btn_edit.Visible = !editing;
            btn_prev.Visible = !editing;
            btn_prev.Enabled = _currentIndex > 0;
            btn_next.Visible = !editing;
            btn_next.Enabled = _currentIndex >= 0 && _currentIndex < _data.sales_return.Count - 1;
            btn_save.Visible = editing;
            btn_cancel.Visible = editing;

            UpdateApprovalButtons();
        }

        private void UpdateApprovalButtons()
        {
            bool hasRecord = _currentIndex >= 0 && _currentIndex < _data.sales_return.Count;
            bool isApproved = hasRecord && _data.sales_return[_currentIndex].is_approved;

            btn_approve.Visible = hasRecord && !isApproved && !_isEditing;
            btn_generate_credit_memo.Enabled = hasRecord && isApproved;
        }

        // ---------------------------------------------------------------
        // Toolbar actions
        // ---------------------------------------------------------------

        private void btn_new_Click(object sender, EventArgs e)
        {
            ClearForm();
            _currentIndex = -1;
            SetEditMode(true);
        }

        // See the class comment - there is no update endpoint for this
        // document, so Edit on an already-saved record has nothing to do.
        private void btn_edit_Click(object sender, EventArgs e)
        {
            if (_currentIndex < 0) return;

            MessageBox.Show(
                "A Sales Return cannot be edited after it's saved - like Purchase Return and Credit Memo, " +
                "it's meant to be reviewed and approved as submitted. If this was entered wrong, create a new " +
                "Sales Return instead of approving this one.",
                "SMPC SOFTWARE", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void btn_cancel_Click(object sender, EventArgs e)
        {
            if (_currentIndex >= 0)
            {
                BindRecord(_currentIndex);
            }
            else
            {
                ClearForm();
                SetEditMode(false);
            }
        }

        private void btn_prev_Click(object sender, EventArgs e)
        {
            if (_currentIndex <= 0) return;
            BindRecord(_currentIndex - 1);
        }

        private void btn_next_Click(object sender, EventArgs e)
        {
            if (_currentIndex < 0 || _currentIndex >= _data.sales_return.Count - 1) return;
            BindRecord(_currentIndex + 1);
        }

        private void btn_search_Click(object sender, EventArgs e)
        {
            var table = new DataTable();
            table.Columns.Add("doc_no", typeof(string));
            table.Columns.Add("customer_name", typeof(string));
            table.Columns.Add("ref_doc_no", typeof(string));
            table.Columns.Add("status", typeof(string));

            foreach (var h in _data.sales_return)
            {
                table.Rows.Add("SRT#" + h.doc_no, h.customer_name, h.ref_doc_no, h.is_approved ? "Approved" : "Pending");
            }

            using (var search = new SearchSalesReturn("Sales Return List", table))
            {
                if (search.ShowDialog() == DialogResult.OK)
                {
                    int result = search.GetResult();
                    if (result >= 0 && result < _data.sales_return.Count)
                    {
                        BindRecord(result);
                    }
                }
            }
        }

        // ---------------------------------------------------------------
        // Reference document type / pick
        // ---------------------------------------------------------------

        private void cmb_ref_doc_type_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!_isEditing)
            {
                _lastRefDocType = cmb_ref_doc_type.Text;
                return;
            }

            if (dgv_sales_return_details.Rows.Count > 0 && cmb_ref_doc_type.Text != _lastRefDocType)
            {
                var confirm = MessageBox.Show(
                    "Changing REF. DOC. TYPE will discard the currently picked reference document and its lines. Continue?",
                    "SMPC SOFTWARE", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

                if (confirm != DialogResult.Yes)
                {
                    cmb_ref_doc_type.SelectedIndexChanged -= cmb_ref_doc_type_SelectedIndexChanged;
                    cmb_ref_doc_type.Text = _lastRefDocType;
                    cmb_ref_doc_type.SelectedIndexChanged += cmb_ref_doc_type_SelectedIndexChanged;
                    return;
                }
            }

            dgv_sales_return_details.Rows.Clear();
            txt_ref_doc_id.Text = "";
            txt_ref_doc_no.Text = "";
            txt_customer_id.Text = "";
            txt_customer_code.Text = "";
            txt_customer_name.Text = "";
            txt_address.Text = "";
            txt_salesperson.Text = "";
            txt_currency.Text = "";
            txt_sales_period.Text = "";
            RecalculateTotal();

            _lastRefDocType = cmb_ref_doc_type.Text;
        }

        private async void txt_ref_doc_no_Click(object sender, EventArgs e)
        {
            if (!_isEditing) return;

            if (cmb_ref_doc_type.SelectedIndex <= 0)
            {
                MessageBox.Show("Choose a REF. DOC. TYPE first.", "SMPC SOFTWARE", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string refType = cmb_ref_doc_type.Text;

            if (refType == "Sales Invoice")
            {
                await PickSalesInvoiceAsync();
            }
            else if (refType == "Delivery Receipt")
            {
                await PickDeliveryReceiptAsync();
            }
        }

        private async Task PickSalesInvoiceAsync()
        {
            Helpers.Loading.ShowLoading(this, "Loading Sales Invoices...");
            SalesInvoiceRefGet data;
            try
            {
                data = await SalesInvoiceRefService.GetSalesInvoices();
            }
            finally
            {
                Helpers.Loading.HideLoading(this);
            }

            using (var picker = new SalesInvoicePickerModal(data.sales_invoice))
            {
                if (picker.ShowDialog() != DialogResult.OK) return;

                var header = data.sales_invoice.FirstOrDefault(x => x.id == picker.SelectedInvoiceId);
                if (header == null) return;
                var lines = data.sales_invoice_details.Where(x => x.sales_invoice_id == header.id).ToList();

                txt_customer_id.Text = header.customer_id.ToString();
                txt_customer_code.Text = header.customer_code;
                txt_customer_name.Text = header.customer;
                txt_address.Text = header.customer_address;
                txt_salesperson.Text = header.sales_person;
                txt_currency.Text = header.currency;
                txt_ref_doc_id.Text = header.id.ToString();
                txt_ref_doc_no.Text = "SI#" + header.doc_no;

                _suppressGridEvents = true;
                dgv_sales_return_details.Rows.Clear();
                foreach (var line in lines)
                {
                    int rowIndex = dgv_sales_return_details.Rows.Add();
                    var row = dgv_sales_return_details.Rows[rowIndex];
                    row.Cells["col_item_id"].Value = line.item_id;
                    row.Cells["col_item_code"].Value = line.item_code;
                    row.Cells["col_description"].Value = line.item_description;
                    row.Cells["col_uom"].Value = line.item_uom;
                    row.Cells["col_qty_returned"].Value = line.item_qty;
                    row.Cells["col_qty_received"].Value = 0;
                    row.Cells["col_qty_discrepancy"].Value = line.item_qty;
                    row.Cells["col_qty_for_replacement"].Value = 0;
                    row.Cells["col_qty_to_stock"].Value = 0;
                    row.Cells["col_qty_for_purchase_return"].Value = 0;
                    // Sales Invoice lines already carry their own price -
                    // single fetch, no join needed (contrast the Delivery
                    // Receipt path below).
                    row.Cells["col_unit_price"].Value = Helpers.MoneyFormat(line.unit_price);
                    row.Cells["col_total_cost"].Value = Helpers.MoneyFormat(line.total_cost);
                }
                _suppressGridEvents = false;

                RecalculateTotal();
            }
        }

        private async Task PickDeliveryReceiptAsync()
        {
            Helpers.Loading.ShowLoading(this, "Loading Delivery Receipts...");
            List<DeliveryReceiptRefModel> receipts;
            try
            {
                receipts = await DeliveryReceiptRefService.GetDeliveryReceipts();
            }
            finally
            {
                Helpers.Loading.HideLoading(this);
            }

            using (var picker = new DeliveryReceiptPickerModal(receipts))
            {
                if (picker.ShowDialog() != DialogResult.OK) return;

                var dr = receipts.FirstOrDefault(x => x.id == picker.SelectedReceiptId);
                if (dr == null) return;

                txt_customer_id.Text = dr.customer_id.ToString();
                txt_customer_code.Text = dr.customer_code;
                txt_customer_name.Text = dr.customer_name;
                txt_address.Text = dr.address;
                txt_salesperson.Text = dr.sales_executive;
                txt_ref_doc_id.Text = dr.id.ToString();
                txt_ref_doc_no.Text = "DR#" + dr.doc_no;
                // A Delivery Receipt carries no currency/sales period of its
                // own anywhere in the schema explored for this build (nor
                // does the customer_so view used below) - left blank rather
                // than guessed. Flagged as a follow-up, not invented here.
                txt_currency.Text = "";
                txt_sales_period.Text = "";

                Helpers.Loading.ShowLoading(this, "Resolving prices...");
                CustomerSoRefGet soData;
                try
                {
                    soData = await CustomerSoRefService.GetCustomerSo(dr.customer_id);
                }
                finally
                {
                    Helpers.Loading.HideLoading(this);
                }

                _suppressGridEvents = true;
                dgv_sales_return_details.Rows.Clear();
                foreach (var item in dr.delivery_receipt_items)
                {
                    var priced = soData.sales_order_details_view
                        .FirstOrDefault(d => d.sales_order_details_id == item.sales_order_details_id);

                    int rowIndex = dgv_sales_return_details.Rows.Add();
                    var row = dgv_sales_return_details.Rows[rowIndex];
                    row.Cells["col_item_id"].Value = item.item_id;
                    row.Cells["col_item_code"].Value = item.item_code;
                    row.Cells["col_description"].Value = item.item_description;
                    row.Cells["col_uom"].Value = item.unit_of_measure;
                    row.Cells["col_qty_returned"].Value = item.qty;
                    row.Cells["col_qty_received"].Value = 0;
                    row.Cells["col_qty_discrepancy"].Value = item.qty;
                    row.Cells["col_qty_for_replacement"].Value = 0;
                    row.Cells["col_qty_to_stock"].Value = 0;
                    row.Cells["col_qty_for_purchase_return"].Value = 0;

                    if (priced != null)
                    {
                        row.Cells["col_unit_price"].Value = Helpers.MoneyFormat(priced.unit_price);
                        row.Cells["col_total_cost"].Value = Helpers.MoneyFormat(priced.total_cost);
                    }
                    else
                    {
                        // Red = attention, standing convention (CLAUDE.md
                        // §1.4) - a line whose price couldn't be resolved
                        // must not silently save as free.
                        row.Cells["col_unit_price"].Value = "";
                        row.Cells["col_total_cost"].Value = "";
                        row.DefaultCellStyle.BackColor = Color.MistyRose;
                    }
                }
                _suppressGridEvents = false;

                RecalculateTotal();
            }
        }

        // ---------------------------------------------------------------
        // Grid edit
        // ---------------------------------------------------------------

        private void dgv_sales_return_details_CurrentCellDirtyStateChanged(object sender, EventArgs e)
        {
            if (dgv_sales_return_details.IsCurrentCellDirty)
            {
                dgv_sales_return_details.CommitEdit(DataGridViewDataErrorContexts.Commit);
            }
        }

        private void dgv_sales_return_details_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (_suppressGridEvents || e.RowIndex < 0) return;

            string colName = dgv_sales_return_details.Columns[e.ColumnIndex].Name;
            if (colName != "col_qty_received" && colName != "col_qty_for_replacement" &&
                colName != "col_qty_to_stock" && colName != "col_qty_for_purchase_return")
            {
                return;
            }

            RecalculateRow(dgv_sales_return_details.Rows[e.RowIndex]);
            RecalculateTotal();
        }

        // §14 test #65: QTY FOR REPLACEMENT + QTY TO STOCK + QTY FOR
        // PURCHASE RETURN must equal QTY RECEIVED on every line. This is a
        // client-side pre-check only for a fast, visible nudge (the
        // highlighted row) - CreateSalesReturn re-validates the same rule
        // server-side and remains the real gate.
        private void RecalculateRow(DataGridViewRow row)
        {
            int qtyReturned = ParseInt(row.Cells["col_qty_returned"].Value);
            int qtyReceived = ParseInt(row.Cells["col_qty_received"].Value);
            int forReplacement = ParseInt(row.Cells["col_qty_for_replacement"].Value);
            int toStock = ParseInt(row.Cells["col_qty_to_stock"].Value);
            int forPurchaseReturn = ParseInt(row.Cells["col_qty_for_purchase_return"].Value);

            _suppressGridEvents = true;
            row.Cells["col_qty_discrepancy"].Value = qtyReturned - qtyReceived;
            _suppressGridEvents = false;

            bool balanced = (forReplacement + toStock + forPurchaseReturn) == qtyReceived;
            row.DefaultCellStyle.BackColor = balanced ? Color.White : Color.MistyRose;
        }

        // Total = Σ TOTAL COST across included lines (models.SalesReturnContent
        // .Total's own comment) - TOTAL COST is sourced from the reference
        // document per line and never recomputed from qty_received (see
        // SalesReturnDetailsContent's comment), so this only sums it, it
        // doesn't multiply anything itself. A line with QTY RECEIVED = 0 is
        // not part of this return (same "0 = excluded" convention already
        // used for Purchase Return's IR-line grid) and is skipped.
        private void RecalculateTotal()
        {
            double total = 0;
            foreach (DataGridViewRow row in dgv_sales_return_details.Rows)
            {
                if (row.IsNewRow) continue;
                if (ParseInt(row.Cells["col_qty_received"].Value) <= 0) continue;
                total += ParseMoney(row.Cells["col_total_cost"].Value);
            }
            txt_total.Text = Helpers.MoneyFormat(total);
        }

        private static int ParseInt(object value) => int.TryParse(value?.ToString(), out int result) ? result : 0;

        private static double ParseMoney(object value)
        {
            string s = value?.ToString().Replace(",", "").Replace("₱", "").Trim();
            return double.TryParse(s, out double result) ? result : 0;
        }

        // ---------------------------------------------------------------
        // Save / Approve / Generate Credit Memo
        // ---------------------------------------------------------------

        private async void btn_save_Click(object sender, EventArgs e)
        {
            if (cmb_ref_doc_type.SelectedIndex <= 0 || string.IsNullOrWhiteSpace(txt_ref_doc_id.Text))
            {
                MessageBox.Show("Choose a REF. DOC. TYPE and pick a reference document before saving.", "SMPC SOFTWARE", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var includedRows = dgv_sales_return_details.Rows.Cast<DataGridViewRow>()
                .Where(r => !r.IsNewRow && ParseInt(r.Cells["col_qty_received"].Value) > 0)
                .ToList();

            if (includedRows.Count == 0)
            {
                MessageBox.Show("Enter a QTY RECEIVED greater than 0 on at least one line.", "SMPC SOFTWARE", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            foreach (var row in includedRows)
            {
                int qtyReceived = ParseInt(row.Cells["col_qty_received"].Value);
                int sum = ParseInt(row.Cells["col_qty_for_replacement"].Value)
                        + ParseInt(row.Cells["col_qty_to_stock"].Value)
                        + ParseInt(row.Cells["col_qty_for_purchase_return"].Value);

                if (sum != qtyReceived)
                {
                    MessageBox.Show(
                        "QTY FOR REPLACEMENT + QTY TO STOCK + QTY FOR PURCHASE RETURN must equal QTY RECEIVED on every line (see the highlighted row).",
                        "SMPC SOFTWARE", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            }

            var body = new SalesReturnBody
            {
                sales_return = new SalesReturnModel
                {
                    customer_id = uint.TryParse(txt_customer_id.Text, out uint custId) ? custId : 0,
                    customer_code = txt_customer_code.Text,
                    customer_name = txt_customer_name.Text,
                    address = txt_address.Text,
                    ref_doc_type = cmb_ref_doc_type.Text,
                    ref_doc_id = uint.TryParse(txt_ref_doc_id.Text, out uint refId) ? refId : 0,
                    ref_doc_no = txt_ref_doc_no.Text,
                    doc_date = dtp_date.Value.ToString("yyyy-MM-dd"),
                    expected_returned_date = dtp_expected_returned_date.Value.ToString("yyyy-MM-dd"),
                    transaction_type = txt_transaction_type.Text,
                    ship_to = txt_ship_to.Text,
                    location_group = txt_location_group.Text,
                    location_code = txt_location_code.Text,
                    salesperson = txt_salesperson.Text,
                    currency = txt_currency.Text,
                    sales_period = txt_sales_period.Text,
                    total = ParseMoney(txt_total.Text),
                    cm_reason_code = txt_cm_reason_code.Text,
                    header_remarks = txt_header_remarks.Text,
                    description = txt_description.Text
                },
                sales_return_details = includedRows.Select(row => new SalesReturnDetailsModel
                {
                    item_id = uint.TryParse(row.Cells["col_item_id"].Value?.ToString(), out uint itemId) ? itemId : 0,
                    item_code = row.Cells["col_item_code"].Value?.ToString(),
                    description = row.Cells["col_description"].Value?.ToString(),
                    unit_of_measure = row.Cells["col_uom"].Value?.ToString(),
                    qty_returned = ParseInt(row.Cells["col_qty_returned"].Value),
                    qty_received = ParseInt(row.Cells["col_qty_received"].Value),
                    qty_discrepancy = ParseInt(row.Cells["col_qty_discrepancy"].Value),
                    qty_for_replacement = ParseInt(row.Cells["col_qty_for_replacement"].Value),
                    qty_to_stock = ParseInt(row.Cells["col_qty_to_stock"].Value),
                    qty_for_purchase_return = ParseInt(row.Cells["col_qty_for_purchase_return"].Value),
                    unit_price = ParseMoney(row.Cells["col_unit_price"].Value),
                    total_cost = ParseMoney(row.Cells["col_total_cost"].Value),
                    reason_for_return = row.Cells["col_reason_for_return"].Value?.ToString()
                }).ToList()
            };

            // Inline saving/saved feedback via the existing title label -
            // no "saved successfully" modal (CLAUDE.md §2.1), and no new
            // Designer control introduced for this.
            lbl_title.Text = "SALES RETURN — saving...";
            Helpers.SetButtonsEnabled(panel1, false);

            ApiResponseModel<SalesReturnBody> response = null;
            try
            {
                response = await SalesReturnService.CreateSalesReturn(body);
            }
            finally
            {
                Helpers.SetButtonsEnabled(panel1, true);
            }

            if (response != null && response.Success)
            {
                lbl_title.Text = "SALES RETURN — saved";
                await LoadRecordsAsync();
            }
            else
            {
                lbl_title.Text = "SALES RETURN";
                Helpers.ShowDialogMessage("error", response?.message ?? "Failed to save the Sales Return - no response from the server.");
            }
        }

        private async void btn_approve_Click(object sender, EventArgs e)
        {
            if (_currentIndex < 0) return;
            var header = _data.sales_return[_currentIndex];

            var confirm = MessageBox.Show(
                $"Approve Sales Return SRT#{header.doc_no}? This cannot be undone from here.",
                "SMPC SOFTWARE", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (confirm != DialogResult.Yes) return;

            lbl_title.Text = "SALES RETURN — saving...";
            Helpers.SetButtonsEnabled(panel1, false);

            ApiResponseModel<object> response = null;
            try
            {
                response = await SalesReturnService.ApproveSalesReturn(header.id);
            }
            finally
            {
                Helpers.SetButtonsEnabled(panel1, true);
            }

            if (response != null && response.Success)
            {
                lbl_title.Text = "SALES RETURN — saved";
                await LoadRecordsAsync(header.id);
            }
            else
            {
                lbl_title.Text = "SALES RETURN";
                Helpers.ShowDialogMessage("error", response?.message ?? "Failed to approve - no response from the server.");
            }
        }

        // Per the user's decision on this build: the six apps are separate
        // processes, so there is no in-process way to open Accounting's
        // Customer Credit Memo screen from here, and per spec the memo is
        // A/R's document to raise, not something a Sales Return produces
        // itself. This stays enabled only once approved (UpdateApprovalButtons)
        // but takes no further action beyond pointing the user at A/R.
        private void btn_generate_credit_memo_Click(object sender, EventArgs e)
        {
            MessageBox.Show(
                "The Credit Memo for this return is raised by Accounts Receivable, in the Accounting app's " +
                "Customer Credit Memo screen - not from here. Notify A/R that this Sales Return is approved and ready.",
                "SMPC SOFTWARE", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}
