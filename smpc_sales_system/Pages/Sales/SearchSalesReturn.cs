using System;
using System.Data;
using System.Windows.Forms;

namespace smpc_sales_app.Pages.Sales
{
    // Sales Return's own search modal - same public shape as
    // Pages.SearchOrder (constructor takes a title + the already-fetched
    // DataTable, GetResult() hands back a row index for the caller to
    // rebind at) so SalesReturn.cs's Prev/Next/Search trio reads the same
    // way Orders.cs's does.
    //
    // Deliberately does NOT reassign dgv_list.DataSource when filtering
    // (SearchOrder's txt_search_TextChanged does, via Helpers.FilterDataTable)
    // - reassigning to a filtered copy means a filtered grid's row index no
    // longer lines up with the original DataTable's row index, so a
    // double-click after searching would resolve to the wrong record. This
    // toggles row.Visible instead, so dgv_list's row order/index always
    // matches Dt's, searched or not - same technique already used by
    // SizeUpPickerModal/the reference-doc pickers built alongside this file.
    public partial class SearchSalesReturn : Form
    {
        private readonly DataTable _dt;
        private int _result = -1;

        public SearchSalesReturn(string title, DataTable dt)
        {
            InitializeComponent();
            lbl_setup_title.Text = title;
            _dt = dt ?? new DataTable();
        }

        private void SearchSalesReturn_Load(object sender, EventArgs e)
        {
            dgv_list.AutoGenerateColumns = true;
            dgv_list.DataSource = _dt;

            // Hide the id column if the caller included one - it's there for
            // the double-click lookup convenience only, never meant to be
            // read by the user picking a row.
            if (dgv_list.Columns.Contains("id"))
            {
                dgv_list.Columns["id"].Visible = false;
            }

            // Columns used to size to their content, leaving every list in this
            // modal squeezed into the left third of the window with a wide grey
            // gap beside it - and long values ("SEWAGE / DRAINAGE") truncated
            // while empty space sat unused next to them.
            //
            // Fill divides the full width across the VISIBLE columns, so the
            // grid always uses the window it was given. FillWeight biases that
            // split toward the text column: a count or a status needs a fraction
            // of the room a name does, and an equal split is what made the
            // headers wrap onto two lines.
            dgv_list.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgv_list.ColumnHeadersDefaultCellStyle.WrapMode = DataGridViewTriState.False;

            foreach (DataGridViewColumn column in dgv_list.Columns)
            {
                if (!column.Visible)
                    continue;

                column.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                column.FillWeight = IsNarrowColumn(column) ? 25 : 100;
                column.DefaultCellStyle.Alignment = IsNarrowColumn(column)
                    ? DataGridViewContentAlignment.MiddleRight
                    : DataGridViewContentAlignment.MiddleLeft;
            }
        }

        // Numeric and short-code columns do not need the width a name does.
        // Decided from the bound column's TYPE rather than a list of hard-coded
        // header names, so this keeps working for whatever a future caller
        // passes in - this modal is shared, and its callers choose their own
        // columns.
        private static bool IsNarrowColumn(DataGridViewColumn column)
        {
            Type type = column.ValueType;

            return type == typeof(int) || type == typeof(long) || type == typeof(short)
                || type == typeof(decimal) || type == typeof(double) || type == typeof(float);
        }

        public int GetResult() => _result;

        private void txt_search_TextChanged(object sender, EventArgs e)
        {
            string search = txt_search.Text.Trim();

            foreach (DataGridViewRow row in dgv_list.Rows)
            {
                if (string.IsNullOrEmpty(search))
                {
                    row.Visible = true;
                    continue;
                }

                bool match = false;
                foreach (DataGridViewCell cell in row.Cells)
                {
                    string value = cell.Value?.ToString() ?? string.Empty;
                    if (value.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        match = true;
                        break;
                    }
                }

                row.Visible = match;
            }
        }

        private void dgv_list_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;

            _result = e.RowIndex;
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
