using System;
using System.Collections.Generic;
using System.Windows.Forms;
using smpc_sales_system.Services.Sales.Models;

namespace smpc_sales_app.Pages.Sales.Modal
{
    // Sales Return's picker when REF. DOC. TYPE = "Sales Invoice". Mirrors
    // SizeUpPickerModal's shape (pre-fetched data in the constructor, search
    // toggles row Visible, never rebinds) but single-select, since only one
    // SI can back one SRT - closer to Inventory's InvoiceReceiptPickerModal
    // in that respect.
    // internal, not public - the constructor takes a List<SalesInvoiceRefModel>,
    // and that model is internal (matching the PRT/CM cross-app read-only
    // reference model precedent), so this can't be more accessible than its
    // own parameter type.
    internal partial class SalesInvoicePickerModal : Form
    {
        public uint SelectedInvoiceId { get; private set; }

        public SalesInvoicePickerModal(List<SalesInvoiceRefModel> invoices)
        {
            InitializeComponent();

            foreach (var inv in invoices)
            {
                int rowIndex = dgv_invoices.Rows.Add();
                var row = dgv_invoices.Rows[rowIndex];
                row.Cells["col_id"].Value = inv.id;
                row.Cells["col_doc_no"].Value = smpc_app.Services.Helpers.DocumentNo.Apply(inv.doc_no.ToString(), "SI#");
                row.Cells["col_customer"].Value = inv.customer;
                row.Cells["col_doc_date"].Value = inv.doc_date;
                row.Cells["col_so"].Value = inv.reference_doc_so;
            }
        }

        private void txt_search_TextChanged(object sender, EventArgs e)
        {
            string search = txt_search.Text.Trim();

            foreach (DataGridViewRow row in dgv_invoices.Rows)
            {
                if (string.IsNullOrEmpty(search))
                {
                    row.Visible = true;
                    continue;
                }

                string docNo = row.Cells["col_doc_no"].Value?.ToString() ?? string.Empty;
                string customer = row.Cells["col_customer"].Value?.ToString() ?? string.Empty;

                row.Visible = docNo.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0
                           || customer.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0;
            }
        }

        private void dgv_invoices_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            SelectRow(dgv_invoices.Rows[e.RowIndex]);
        }

        private void btn_select_Click(object sender, EventArgs e)
        {
            if (dgv_invoices.CurrentRow == null)
            {
                MessageBox.Show("Select a Sales Invoice first.", "No Selection", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            SelectRow(dgv_invoices.CurrentRow);
        }

        private void SelectRow(DataGridViewRow row)
        {
            if (!uint.TryParse(row.Cells["col_id"].Value?.ToString(), out uint id)) return;

            SelectedInvoiceId = id;
            DialogResult = DialogResult.OK;
            Close();
        }

        private void btn_cancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }
    }
}
