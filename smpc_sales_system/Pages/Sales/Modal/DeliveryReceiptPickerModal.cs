using System;
using System.Collections.Generic;
using System.Windows.Forms;
using smpc_sales_system.Services.Sales.Models;

namespace smpc_sales_app.Pages.Sales.Modal
{
    // Sales Return's picker when REF. DOC. TYPE = "Delivery Receipt". Same
    // shape as SalesInvoicePickerModal - see that file's comment.
    // internal - same reason as SalesInvoicePickerModal.
    internal partial class DeliveryReceiptPickerModal : Form
    {
        public uint SelectedReceiptId { get; private set; }

        public DeliveryReceiptPickerModal(List<DeliveryReceiptRefModel> receipts)
        {
            InitializeComponent();

            foreach (var dr in receipts)
            {
                int rowIndex = dgv_receipts.Rows.Add();
                var row = dgv_receipts.Rows[rowIndex];
                row.Cells["col_id"].Value = dr.id;
                row.Cells["col_doc_no"].Value = "DR#" + dr.doc_no;
                row.Cells["col_customer"].Value = dr.customer_name;
                row.Cells["col_delivery_date"].Value = dr.delivery_date;
            }
        }

        private void txt_search_TextChanged(object sender, EventArgs e)
        {
            string search = txt_search.Text.Trim();

            foreach (DataGridViewRow row in dgv_receipts.Rows)
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

        private void dgv_receipts_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            SelectRow(dgv_receipts.Rows[e.RowIndex]);
        }

        private void btn_select_Click(object sender, EventArgs e)
        {
            if (dgv_receipts.CurrentRow == null)
            {
                MessageBox.Show("Select a Delivery Receipt first.", "No Selection", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            SelectRow(dgv_receipts.CurrentRow);
        }

        private void SelectRow(DataGridViewRow row)
        {
            if (!uint.TryParse(row.Cells["col_id"].Value?.ToString(), out uint id)) return;

            SelectedReceiptId = id;
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
