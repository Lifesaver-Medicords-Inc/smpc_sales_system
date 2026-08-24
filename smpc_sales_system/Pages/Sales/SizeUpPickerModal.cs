using System;
using System.Collections.Generic;
using System.Data;
using System.Windows.Forms;

namespace smpc_sales_system.Pages.Sales
{
    // One pump the user checked off in this modal - Quotation.cs's SizeUpClicked handler
    // loops over these calling ItemSetUC.AddSizeUpRow once per pick.
    public class SizeUpSelection
    {
        public int ItemId { get; set; }
        public string Model { get; set; }
    }

    // Trello #044/#043/#049: SIZE UP's own picker. Deliberately NOT a variant of
    // ModelModal - ModelModal is shared by two unrelated single-item pickers (component
    // selection on ItemSetUC/Quotation.cs, and FINAL's own picker) that depend on its
    // BOM lookup/item_name_id scoping; retrofitting multi-select onto it risked
    // regressing those. This modal only ever needs a flat pump list, a search box, and
    // a checkbox per row.
    //
    // Rows are built once from the DataTable the caller hands in and never rebuilt -
    // the search box only ever toggles a row's Visible flag, it never re-binds or
    // recreates rows, so a row's checkbox state can never be lost the way it would be
    // if this were backed by a DataView.RowFilter (that approach recreates
    // DataGridViewRow objects - along with their unbound cell values - on every filter
    // change).
    public partial class SizeUpPickerModal : Form
    {
        // pumpItems is expected to already be pre-filtered to pump items only (see
        // Quotation.cs's SizeUpClicked, which filters ItemList by item_name == "PUMP"
        // before ever constructing this modal) and to carry "id", "item_brand",
        // "item_model" columns, same as ItemList elsewhere in this form.
        // alreadySelectedIds are the pumps already on the tab's SIZE UP list (ItemSetUC.
        // GetSizeUpItemIds()), so reopening this modal shows their true current state
        // instead of letting the user "re-add" them with no visible indication.
        // title overrides the Designer's default "Select Pumps for Size Up" - this same
        // modal is reused for FINAL's own picker (Quotation.cs's FinalTxtBoxClicked),
        // which needs its own wording rather than always saying "Size Up" regardless of
        // which one actually opened it.
        public SizeUpPickerModal(DataTable pumpItems, List<int> alreadySelectedIds, string title = null)
        {
            InitializeComponent();

            if (!string.IsNullOrEmpty(title))
            {
                this.Text = title;
                label1.Text = title;
            }

            alreadySelectedIds = alreadySelectedIds ?? new List<int>();

            foreach (DataRow row in pumpItems.Rows)
            {
                if (!int.TryParse(row["id"]?.ToString(), out int itemId)) continue;

                string brand = pumpItems.Columns.Contains("item_brand") ? row["item_brand"]?.ToString() : string.Empty;
                string model = pumpItems.Columns.Contains("item_model") ? row["item_model"]?.ToString() : string.Empty;

                int rowIndex = dgv_pumps.Rows.Add();
                var gridRow = dgv_pumps.Rows[rowIndex];
                gridRow.Cells["col_select"].Value = alreadySelectedIds.Contains(itemId);
                gridRow.Cells["col_item_id"].Value = itemId;
                gridRow.Cells["col_brand"].Value = brand;
                gridRow.Cells["col_model"].Value = model;
                // col_list_price stays blank - see the Designer comment on that column.
            }
        }

        // Standard WinForms idiom: a DataGridViewCheckBoxColumn's edit doesn't commit
        // (so its Value isn't up to date yet) until the cell loses focus, unless forced
        // here - needed since Save reads every row's current checkbox Value, including
        // whichever cell is still "current".
        private void dgv_pumps_CurrentCellDirtyStateChanged(object sender, EventArgs e)
        {
            if (dgv_pumps.IsCurrentCellDirty)
            {
                dgv_pumps.CommitEdit(DataGridViewDataErrorContexts.Commit);
            }
        }

        // Toggles row visibility only - never rebinds/rebuilds rows, so a checked row
        // that gets filtered out by a search and then back in keeps its check.
        private void txt_search_TextChanged(object sender, EventArgs e)
        {
            string search = txt_search.Text.Trim();

            foreach (DataGridViewRow row in dgv_pumps.Rows)
            {
                if (string.IsNullOrEmpty(search))
                {
                    row.Visible = true;
                    continue;
                }

                string brand = row.Cells["col_brand"].Value?.ToString() ?? string.Empty;
                string model = row.Cells["col_model"].Value?.ToString() ?? string.Empty;

                row.Visible = brand.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0
                           || model.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0;
            }
        }

        // What Quotation.cs's SizeUpClicked reads back after Save - every currently
        // checked row, regardless of whether it's presently hidden by an active search.
        public List<SizeUpSelection> GetSelectedItems()
        {
            var result = new List<SizeUpSelection>();

            foreach (DataGridViewRow row in dgv_pumps.Rows)
            {
                bool isChecked = row.Cells["col_select"].Value is bool b && b;
                if (!isChecked) continue;

                if (!int.TryParse(row.Cells["col_item_id"].Value?.ToString(), out int itemId)) continue;

                result.Add(new SizeUpSelection
                {
                    ItemId = itemId,
                    Model = row.Cells["col_model"].Value?.ToString() ?? string.Empty
                });
            }

            return result;
        }

        private void btn_save_Click(object sender, EventArgs e)
        {
            dgv_pumps.EndEdit();
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
