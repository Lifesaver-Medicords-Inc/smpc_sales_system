using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace smpc_sales_system.Pages
{
    // The quotation's version table (spec 5.1): every copy of one quote - each version, each EDIT
    // sub-version and each FQ - oldest first. Opened by double-clicking the version number.
    //
    // Clicking a row opens that copy; clicking an SO number in STATUS opens the SO (both in a new
    // tab, done by the caller). The caller builds the rows (see Quotation.BuildVersionTable):
    //   id, version_description, version_code, date, so_ref, version_remark
    //
    // It used to take every copy with a version number, filter them on the raw document number
    // (so a quote's FQ copies and drafts never showed together), show the version number without
    // the sub-version, bind REMARK to a field that does not exist (version_remark; the model's is
    // version_remarks, and neither is stored) and leave STATUS unbound.
    public partial class VersionModal : Form
    {
        private readonly DataTable versions;
        private readonly int currentId;

        // The copy picked (0 = none). OpenSalesOrder: the SO number in its STATUS was clicked,
        // so the caller opens that SO rather than the copy.
        public int SelectedQuotationId { get; private set; }
        public bool OpenSalesOrder { get; private set; }

        public VersionModal(DataTable versions, string title, int currentId)
        {
            InitializeComponent();
            this.versions = versions ?? new DataTable();
            this.currentId = currentId;
            label1.Text = "Versions for " + title;
            this.Text = "Versions";
            this.StartPosition = FormStartPosition.CenterParent;
        }

        private void VersionModal_Load(object sender, EventArgs e)
        {
            dgv_version_modal.AutoGenerateColumns = false;
            dgv_version_modal.AllowUserToAddRows = false;
            dgv_version_modal.AllowUserToDeleteRows = false;
            dgv_version_modal.ReadOnly = true;
            dgv_version_modal.MultiSelect = false;
            dgv_version_modal.RowHeadersVisible = false;
            dgv_version_modal.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgv_version_modal.DataSource = versions;

            // Document numbers are links (spec 1.4 conventions): underlined, no colour of their own.
            ver_status.DefaultCellStyle.Font = new Font(dgv_version_modal.Font, FontStyle.Underline);

            dgv_version_modal.ClearSelection();
            foreach (DataGridViewRow row in dgv_version_modal.Rows)
            {
                if (RowId(row) == currentId)
                {
                    row.Selected = true;
                    row.DefaultCellStyle.Font = new Font(dgv_version_modal.Font, FontStyle.Bold);
                    dgv_version_modal.FirstDisplayedScrollingRowIndex = row.Index;
                }
            }

            dgv_version_modal.CellMouseMove += (s, ev) =>
            {
                bool onLink = ev.RowIndex >= 0 && ev.ColumnIndex == ver_status.Index
                    && !string.IsNullOrWhiteSpace(Convert.ToString(dgv_version_modal.Rows[ev.RowIndex].Cells[ev.ColumnIndex].Value));
                dgv_version_modal.Cursor = onLink ? Cursors.Hand : Cursors.Default;
            };
        }

        private static int RowId(DataGridViewRow row)
        {
            var view = row.DataBoundItem as DataRowView;
            return view != null && int.TryParse(Convert.ToString(view["id"]), out int id) ? id : 0;
        }

        private void dataGridView1_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;

            DataGridViewRow row = dgv_version_modal.Rows[e.RowIndex];
            int id = RowId(row);
            if (id <= 0) return;

            OpenSalesOrder = e.ColumnIndex == ver_status.Index
                && !string.IsNullOrWhiteSpace(Convert.ToString(row.Cells[e.ColumnIndex].Value));

            // The copy already on screen: nothing to open, unless it is its SO.
            if (id == currentId && !OpenSalesOrder) return;

            SelectedQuotationId = id;
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
