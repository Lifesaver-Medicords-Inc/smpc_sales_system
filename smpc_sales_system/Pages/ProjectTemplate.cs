using smpc_app.Services.Helpers;
using smpc_sales_app.Models;
using smpc_sales_app.Pages;
using smpc_sales_app.Services.Helpers;
using smpc_sales_app.Services.Sales;
using smpc_sales_system.Models;
using smpc_sales_system.Pages.Sales;
using smpc_sales_system.Services.Sales;
using smpc_sales_system.Services.Sales.Models;
using smpc_sales_system.Services.Setup;
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
    public partial class ProjectTemplate : UserControl
    {
        public ProjectTemplate()
        {
            InitializeComponent();
        }

        private DataTable Template = new DataTable();
        private DataTable TemplateChild = new DataTable();
        private DataTable item = new DataTable();

        public DataTable ItemList { get; set; } = new DataTable();
        public DataTable BomHead { get; set; } = new DataTable();
        public DataTable BomDetails { get; set; } = new DataTable();
        public DataTable stockQuickDataTable = new DataTable();

        private async void ProjectTemplate_Load(object sender, EventArgs e)
        {
            // Bug #051 (Trello, "Template Setup cannot be accessed"): none of
            // these four fetches or their null checks were guarded, so a null
            // response from GetProjectTemplates() (data.SalesProjectTemplate on
            // a null data) or a null list handed to JsonHelper.ToDataTable threw
            // unhandled the moment this screen opened.
            try
            {
                var data = await ProjectTemplatesService.GetProjectTemplates();
                if (data == null)
                {
                    Helpers.ShowDialogMessage("error", "Failed to load Template Setup: no data returned.");
                    return;
                }

                Template = JsonHelper.ToDataTable(data.SalesProjectTemplate ?? new List<ProjectTemplateModel>());
                TemplateChild = JsonHelper.ToDataTable(data.sales_project_template_child ?? new List<ProjectTemplateChildModel>());

                var bomData = await ProjectService.GetBom();
                var companyData = await CompanyService.GetAsDatatable();

                if (bomData == null)
                    return;

                // Phase 3: no full-catalogue download (picked rows merge via
                // the picker modal, like the quotation module). Fixed schema
                // so lookups keep working with zero rows merged yet.
                if (ItemList == null || ItemList.Columns.Count == 0)
                    ItemList = ItemCatalogTables.NewItemTable();
                BomHead = JsonHelper.ToDataTable(bomData.bom_head);
                BomDetails = JsonHelper.ToDataTable(bomData.bom_details);

                LoadAll();
            }
            catch (Exception ex)
            {
                Helpers.ShowDialogMessage("error", $"Failed to load Template Setup: {ex.Message}");
            }
        }

        int selectedRow = 0;
        int template_id = 0;

        private async void LoadAll()
        {
            // Was unconditionally false, which left the grid editable from the
            // moment this screen opened - a template could be altered in view
            // mode with no Edit ever pressed. NewButtonActive owns the mode now
            // and is called at the end of this method; ReadOnly is set true here
            // only so the grid is never briefly editable while loading.
            dgv_template.ReadOnly = true;
            stockQuickDataTable = Helpers.GetDataTableFromUnboundGrid(dgv_template);

            if (Template.Rows.Count > 0 && TemplateChild.Rows.Count > 0)
            {
                // Dead full-catalogue download removed (Phase 3): its result
                // was never read. Picked rows merge via the picker modal.

                DataRow firstRow = Template.Rows[selectedRow];

                template_id = firstRow.Field<int>("template_id");
                txt_template_name.Text = firstRow["template_name"].ToString();

                //DataTable firstDetailsRow = TemplateChild.AsEnumerable()
                //                                .Where(row => row.Field<int>("ParentId") == template_id)
                //                                .CopyToDataTable();

                var filteredRows = TemplateChild.AsEnumerable()
                                .Where(row => row.Field<int>("ParentId") == template_id);

                foreach(DataRow row in TemplateChild.Rows)
                {
                    Console.WriteLine(row["Id"] + " " + row["ParentId"] + " " + row["ItemId"] + " " + row["Components"]);
                }

                DataTable firstDetailsRow = filteredRows.Any()
                    ? filteredRows.CopyToDataTable()
                    : TemplateChild.Clone(); // Returns empty DataTable with same schema

                dgv_template.DataSource = firstDetailsRow;
            }
            else
            {
                dgv_template.DataSource = stockQuickDataTable;
            }

            RenumberTemplateRows();
            NewButtonActive(true);
        }

        private void btn_new_Click(object sender, EventArgs e)
        {
            editMode = false;

            txt_template_name.Text = "";

            if (dgv_template.DataSource is DataTable dt)
            {
                dt.Rows.Clear();
            }
            else if (dgv_template.DataSource is DataView dv)
            {
                dv.Table.Clear(); // clear the underlying DataTable
            }
            else
            {
                dgv_template.Rows.Clear(); // fallback for unbound
            }

            dgv_template.DataSource = stockQuickDataTable.Clone();

            NewButtonActive(false);
        }

        // isEditing - the single source of truth for "the form is in an editing
        // state", covering BOTH New and Edit. The existing editMode flag cannot
        // serve: it distinguishes insert from update (btn_new deliberately sets
        // it false), so it is false during New, when the form is very much
        // editable. Every guard below tests this instead.
        private bool isEditing = false;

        // Visibled == true means VIEW mode (the nav buttons are visible).
        // Duplicate had no Click handler wired at all - the button sat on the
        // strip and was inert. It copies the template currently on screen into
        // a NEW, unsaved one: same components, same quantities, name suffixed
        // " - COPY", left in New mode for the user to rename and save.
        //
        // Nothing is written until they press Save, and Close abandons it (that
        // now really does reload - see btn_close_Click), so a mis-click costs
        // nothing.
        private void btn_duplicate_Click(object sender, EventArgs e)
        {
            if (Template == null || Template.Rows.Count == 0)
            {
                Helpers.ShowDialogMessage("error", "There is no template to duplicate.");
                return;
            }

            // Copy what is ON SCREEN rather than re-reading TemplateChild: the
            // grid is bound to its own CopyToDataTable, and copying the bound
            // table is what makes the duplicate independent - editing it must
            // never reach back into the source template's rows.
            DataTable current = dgv_template.DataSource as DataTable;
            DataTable copy = current != null ? current.Copy() : stockQuickDataTable.Clone();

            // btn_save_Click only ever reads ItemId/component/level/qty, so the
            // identity columns are already ignored on save. Clearing them anyway
            // - a row that still carries the ORIGINAL template's Id and ParentId
            // while sitting in a brand-new template is exactly the kind of stale
            // value that becomes a real bug the moment someone reads it.
            foreach (DataRow row in copy.Rows)
            {
                if (copy.Columns.Contains("Id"))
                    row["Id"] = 0;

                if (copy.Columns.Contains("ParentId"))
                    row["ParentId"] = 0;
            }

            // editMode false + template_id 0 is what sends Save down the Insert
            // branch, same as New. Leaving template_id pointing at the source
            // would be the one way this OVERWRITES the template it copied.
            editMode = false;
            template_id = 0;

            txt_template_name.Text = txt_template_name.Text.Trim() + " - COPY";

            dgv_template.DataSource = copy;
            RenumberTemplateRows();

            NewButtonActive(false);
        }

        private void NewButtonActive(bool Visibled)
        {
            btn_new.Visible = Visibled;
            btn_duplicate.Visible = Visibled;
            btn_prev.Visible = Visibled;
            btn_next.Visible = Visibled;
            btn_search.Visible = Visibled;
            btn_edit.Visible = Visibled;

            btn_save.Visible = !Visibled;
            btn_close.Visible = !Visibled;

            txt_template_name.ReadOnly = Visibled;

            isEditing = !Visibled;

            // The grid was the one control this method never locked, which is
            // why the template stayed editable in view mode. AllowUserToAddRows
            // matters as much as ReadOnly: the blank new-row placeholder is how
            // the first component of an empty template gets added (its
            // component cell is clicked to open the item picker), so it must be
            // gone in view mode and present while editing.
            dgv_template.ReadOnly = Visibled;
            dgv_template.AllowUserToAddRows = !Visibled;

            // The "Add Child" menu is attached to the component COLUMN in the
            // designer, so guarding its handler is not enough on its own - the
            // menu would still appear on right-click and simply do nothing,
            // which looks broken. Detach it outright in view mode.
            component.ContextMenuStrip = Visibled ? null : contextMenuStrip1;
        }

        private async void btn_save_Click(object sender, EventArgs e)
        {
            Dictionary<string, dynamic> parent = new Dictionary<string, dynamic>();

            int templateId = template_id;

            if (editMode)
            {
                parent["template_id"] = templateId;
            }

            parent["template_name"] = txt_template_name.Text;

            // Once more before the payload is built, so what reaches the server carries the
            // same indent that was on screen.
            RenumberTemplateRows();

            var dataSource = Helpers.ConvertDataGridViewToDataTable(dgv_template);
            var newDatasource = Helpers.ConvertDataTableToStringTable(dataSource);
            List<Dictionary<string, dynamic>> quickQuoteList = new List<Dictionary<string, dynamic>>();

            for (int i = 0; i < newDatasource.Rows.Count; i++)
            {
                DataRow item = newDatasource.Rows[i];

                int itemId = int.TryParse(item["ItemId"].ToString(), out int ival) ? ival : 0;

                if (itemId == 0)
                    continue;

                Dictionary<string, object> data = new Dictionary<string, object>();

                data.Add("item_id", itemId);
                data.Add("components", item["component"]);
                data.Add("level", int.Parse(item["level"].ToString()));

                // Quantity is optional and stays optional. A blank cell is sent
                // as null, NOT as 0: null means "no quantity specified" and
                // applies to a project quotation as blank, while 0 would read
                // as a deliberate zero. Nothing substitutes a default here -
                // that is what keeps existing templates and the quotations
                // built from them unchanged.
                string qtyText = item["qty"]?.ToString().Trim();
                if (!string.IsNullOrEmpty(qtyText) && int.TryParse(qtyText, out int qtyValue) && qtyValue > 0)
                {
                    data.Add("qty", qtyValue);
                }
                else
                {
                    data.Add("qty", null);
                }

                quickQuoteList.Add(data);
            }

            parent["sales_project_template_child"] = quickQuoteList;

            Helpers.Loading.ShowLoading(this);
            try
            {
                var send = new ApiResponseModel();

                if (editMode)
                {
                    send = await ProjectTemplatesService.Update(parent);
                    // Tabs cache the template list; a saved template must show up on the next one.
                    smpc_sales_system.Services.Sales.ItemSetLookups.InvalidateTemplates();
                }
                else
                {  
                    send = await ProjectTemplatesService.Insert(parent);
                    // Tabs cache the template list; a saved template must show up on the next one.
                    smpc_sales_system.Services.Sales.ItemSetLookups.InvalidateTemplates();
                }

                if (send.Success)
                {
                    MessageBox.Show("Data successfully saved");
                }
                else
                {
                    MessageBox.Show("Data failed to save");
                }
            }
            finally
            {
                Helpers.Loading.HideLoading(this);
            }

            NewButtonActive(true);
        }

        private void btn_close_Click(object sender, EventArgs e)
        {
            // Close used to only flip the buttons back, leaving every abandoned
            // edit sitting in the grid - where the next Save would write it.
            // LoadAll rebinds from the fetched TemplateChild table (the grid is
            // bound to a CopyToDataTable of it, so edits never touched the
            // source) and ends by calling NewButtonActive(true), which is what
            // returns the form to view mode.
            LoadAll();
        }

        private void dgv_template_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            try
            {
                // Skip header clicks
                if (e.RowIndex < 0 || e.ColumnIndex < 0)
                    return;

                // DataGridView.ReadOnly does not stop this handler - a plain
                // click still raises CellClick - so the item picker opened in
                // view mode and a single click could replace a component.
                if (!isEditing)
                    return;

                // Components Column
                if (dgv_template.Columns[e.ColumnIndex].Name == "component")
                {
                    HandleItemSelectionClick(e.RowIndex, dgv_template, false);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("An error occurred while processing your request: " + ex.Message,
                                "Error",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Error);
            }
        }
        private void addChildToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                // Skip header clicks
                if (contextRowIndex < 0 || contextColIndex < 0)
                    return;

                // "Add Child" was reachable by right-click in view mode - the
                // context menu is attached to the component column in the
                // designer and nothing checked the mode. Guarding the handler
                // as well as hiding the menu (see dgv_template_CellMouseDown)
                // keeps a stale contextRowIndex from firing after Close.
                if (!isEditing)
                    return;

                if (dgv_template.Rows[contextRowIndex].Cells["component"].Value.ToString() == ""
                    && dgv_template.Rows[contextRowIndex].Cells["component"].Value == DBNull.Value)
                    return;

                // Components Column
                if (dgv_template.Columns[contextColIndex].Name == "component")
                {
                    HandleItemSelectionClick(contextRowIndex, dgv_template, true);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("An error occurred while processing your request: " + ex.Message,
                                "Error",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Error);
            }
        }

        private void HandleItemSelectionClick(int rowIndex, DataGridView dgv, bool child = false)
        {
            SalesItemModal itemModal = new SalesItemModal(ItemList);
            DialogResult r = itemModal.ShowDialog();

            if (r == DialogResult.OK)
            {
                int itemid = itemModal.GetParentItemId();

                // Level 1 is the top - there is no level 0. Every template row already
                // stored is 1-based (levels 1/2/3 on the live data), which is also what the
                // quotation assumes when it applies a template: it indents by
                // (level - 1) * 4 and numbers from counter 1. A parent written as 0 here
                // therefore disagreed with every row saved before it, and reached the
                // quotation as a row it numbers from a counter slot that is not part of the
                // code at all - a blank reference_code.
                if (dgv.Rows[rowIndex].Cells["level"].Value == DBNull.Value)
                    dgv.Rows[rowIndex].Cells["level"].Value = 1;



                if (dgv.Rows[rowIndex].Cells["ItemId"].Value == DBNull.Value)
                    dgv.Rows[rowIndex].Cells["ItemId"].Value = 0;

                int parentItemId = Convert.ToInt32(dgv.Rows[rowIndex].Cells["ItemId"].Value);
                int level = Convert.ToInt32(dgv.Rows[rowIndex].Cells["level"].Value);

                // Clicking a row that already holds a component means "this one, not that
                // one" - so the item is swapped on the row and its place in the tree is
                // left exactly as it was. It used to insert a NEW row at the top level
                // instead, which is how a child came back as a parent after somebody tried
                // to rename it: nothing on screen says the way to change a component is to
                // delete the row and add it again in the right place.
                //
                // Add Child still adds; only a plain click on a filled row replaces.
                if (!child && parentItemId > 0)
                {
                    ReplaceComponent(rowIndex, itemid, dgv, Math.Max(level, 1));
                    return;
                }

                if (!child)
                {
                    level = 1;
                }
                else
                {
                    // A row saved before the top level was settled at 1 reads as 0; its
                    // child is still level 2, not level 1.
                    level = Math.Max(level, 1) + 1;
                }

                    GetItemData(rowIndex, itemid, dgv, level, parentItemId);

            }
        }

        private int contextRowIndex = -1;
        private int contextColIndex = -1;

        // Deleting a row shifts everything below it, so the codes have to be worked out
        // again - otherwise the list keeps the numbers of a shape it no longer has, with a
        // gap where the deleted row was and children still quoting a parent number that has
        // moved. UserDeletedRow fires after the row has gone from the bound table.
        private void dgv_template_UserDeletedRow(object sender, DataGridViewRowEventArgs e)
        {
            RenumberTemplateRows();
        }

        private void dgv_template_CellMouseDown(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right && e.RowIndex >= 0)
            {
                contextRowIndex = e.RowIndex;
                contextColIndex = e.ColumnIndex;

                dgv_template.ClearSelection();
                dgv_template.Rows[e.RowIndex].Selected = true;
            }
        }

        // Swaps the item on an existing row, keeping its level and therefore its place in
        // the hierarchy. The name comes from the item, never from what was in the cell.
        private void ReplaceComponent(int rowIndex, int itemID, DataGridView dgv, int level)
        {
            DataTable itemList = Helpers.FilterExactDataTable(ItemList, itemID.ToString(), "id");
            if (itemList.Rows.Count == 0)
            {
                MessageBox.Show("Invalid selection. Item not found.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            DataTable dataSource = dgv.DataSource as DataTable;
            if (dataSource == null || rowIndex < 0 || rowIndex >= dataSource.Rows.Count) return;

            DataRow target = dataSource.Rows[rowIndex];
            DataRow picked = itemList.Rows[0];

            target["ItemId"] = picked["id"];
            target["Components"] = new string(' ', Math.Max(0, level - 1) * IndentPerLevel)
                                 + picked["item_name"];
            target["Level"] = level;

            if (dataSource.Columns.Contains("unit_of_measure") && itemList.Columns.Contains("unit_of_measure"))
                target["unit_of_measure"] = picked["unit_of_measure"];

            RenumberTemplateRows();
        }

        private void GetItemData(int rowIndex, int itemID, DataGridView dgv, int level, int ParentItemId)
        {
            DataTable itemList = Helpers.FilterExactDataTable(ItemList, itemID.ToString(), "id");

            if (itemList.Rows.Count == 0)
            {
                MessageBox.Show("Invalid selection. Item not found.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            DataTable dataSource = dgv.DataSource as DataTable;
            if (dataSource == null) return;

            foreach (DataRow row in itemList.Rows)
            {
                DataRow newRow = dataSource.NewRow();
                if (dataSource.Columns.Contains("unit_of_measure"))
                    newRow["unit_of_measure"] = row["unit_of_measure"];

                newRow["ItemId"] = row["id"];
                newRow["Components"] = new string(' ', Math.Max(0, level - 1) * IndentPerLevel) + row["item_name"];
                newRow["Level"] = level;

                if (ParentItemId != 0)
                {
                    rowIndex += 1;
                }

                dataSource.Rows.InsertAt(newRow, rowIndex);

                // 🎨 Style as Single Item
                int addedRowIndex = dataSource.Rows.Count - 1;
                Helpers.SalesItemRowStyler.ApplyStyle(dgv, addedRowIndex, "single"); 
            }

            // The new row shifts everything below it, so the codes and indents below are
            // now wrong - rebuild the lot rather than trying to patch the difference.
            RenumberTemplateRows();

        }

        // Four spaces per level, the same step the quotation uses when it applies a
        // template (ItemSetUC's template branch), so a set of components reads the same on
        // both screens.
        private const int IndentPerLevel = 4;

        // Gives every row its place in the hierarchy - 1, 2, 2.1, 2.2.1, 2.3, 3 - and the
        // indent that shows it at a glance.
        //
        // Worked out from Level and row order every time, never stored: Level already IS the
        // record, so a code computed from it cannot go stale, and an old template picks up
        // correct codes the first time it is opened with no data migration. GetItemData
        // writes an indent when a row is added, but nothing ever rebuilt it, so a saved
        // template came back flat however deep its levels went.
        //
        // No depth limit (user decision, 2026-09-23): 2.2.4.1.3 is as valid as 2.1.
        //
        // Levels are 1-based: the top of the tree is level 1 and there is no level 0. That
        // is what the live template data holds and what the quotation assumes when it
        // applies a template, so both screens number and indent the same components the
        // same way.
        private void RenumberTemplateRows()
        {
            DataTable rows = dgv_template.DataSource as DataTable;
            if (rows == null) return;

            if (!rows.Columns.Contains("RefCode"))
                rows.Columns.Add("RefCode", typeof(string));

            // One counter per depth, indexed by level, so slot 0 is never used. Walking top
            // to bottom is what makes this work: a row's code depends only on the rows above
            // it.
            List<int> counters = new List<int>();

            foreach (DataRow row in rows.Rows)
            {
                if (row.RowState == DataRowState.Deleted) continue;

                int level = 1;
                if (rows.Columns.Contains("Level"))
                    int.TryParse(row["Level"]?.ToString(), out level);

                // A row saved by an older build as level 0 is a top-level row.
                if (level < 1) level = 1;

                while (counters.Count <= level)
                    counters.Add(0);

                // Coming back up, everything deeper starts again - which is what turns the
                // row after 2.2.4 into 2.3 rather than 2.5.
                for (int deeper = level + 1; deeper < counters.Count; deeper++)
                    counters[deeper] = 0;

                counters[level]++;

                // A depth nobody has used yet still counts as the first of its family, so a
                // child above any parent reads as 1.1 rather than 0.1 (user decision,
                // 2026-09-23), and a jump from level 1 to level 3 gives 7.1.1 rather than
                // 7.1 - which is a level-2 code belonging to a different row.
                for (int above = 1; above < level; above++)
                    if (counters[above] == 0) counters[above] = 1;

                // Skip(1): slot 0 is not part of any code.
                row["RefCode"] = string.Join(".", counters.Skip(1).Take(level));

                if (rows.Columns.Contains("Components"))
                {
                    // The name comes from the item wherever the row has one, so a rename in
                    // Item Entry shows here without anybody editing the template, and a row
                    // cannot drift from the item it points at. A row with no item - a
                    // heading somebody typed - keeps its own text.
                    string text = (row["Components"]?.ToString() ?? "").TrimStart();

                    if (rows.Columns.Contains("ItemName") && rows.Columns.Contains("ItemId"))
                    {
                        string itemName = row["ItemName"]?.ToString() ?? "";
                        int.TryParse(row["ItemId"]?.ToString(), out int itemId);
                        if (itemId > 0 && !string.IsNullOrWhiteSpace(itemName))
                            text = itemName.Trim();
                    }

                    row["Components"] = new string(' ', (level - 1) * IndentPerLevel) + text;
                }
            }
        }

        private void btn_prev_Click(object sender, EventArgs e)
        {
            if (selectedRow > 0)
            {
                selectedRow--;
                LoadAll();
            }
        }

        private void btn_next_Click(object sender, EventArgs e)
        {
            int rowCount = Template.Rows.Count;

            if (selectedRow < rowCount - 1)
            {
                selectedRow++;
                LoadAll();
            }
        }

        private bool editMode = false;

        private void btn_edit_Click(object sender, EventArgs e)
        {
            editMode = true;
            NewButtonActive(false);
        }

        // Search was an empty handler - the button existed on the strip and did
        // nothing at all, so the only way to reach a template was to click
        // through Prev/Next one at a time.
        //
        // Reuses SearchSalesReturn rather than adding another search form. It
        // is named for the screen it was first built for but is entirely
        // generic (AutoGenerateColumns, any DataTable, returns a row index),
        // and it is the better of the two search modals in this app: it filters
        // by toggling row.Visible, so a row's grid index still matches its
        // index in the source table after a search. SearchOrder reassigns
        // DataSource to a filtered copy instead, which means picking a row
        // after typing a filter resolves to the wrong record.
        //
        // Fully qualified because this file already pulls in both
        // smpc_sales_app.Pages and smpc_sales_system.Pages.Sales; adding a
        // using for a third, similarly-named namespace invites an ambiguity.
        private void btn_search_Click(object sender, EventArgs e)
        {
            if (Template == null || Template.Rows.Count == 0)
            {
                Helpers.ShowDialogMessage("error", "There are no templates to search yet.");
                return;
            }

            // Built by walking Template.Rows in order, so the index the modal
            // returns IS selectedRow - no lookup or mapping to get out of step.
            DataTable list = new DataTable();
            list.Columns.Add("TEMPLATE NAME", typeof(string));
            list.Columns.Add("COMPONENTS", typeof(int));

            foreach (DataRow row in Template.Rows)
            {
                int templateId = row.Field<int>("template_id");
                int componentCount = TemplateChild.AsEnumerable()
                    .Count(child => child.Field<int>("ParentId") == templateId);

                list.Rows.Add(row["template_name"]?.ToString(), componentCount);
            }

            using (var search = new smpc_sales_app.Pages.Sales.SearchSalesReturn("Project Template List", list))
            {
                if (search.ShowDialog() != DialogResult.OK)
                    return;

                int result = search.GetResult();

                if (result >= 0 && result < Template.Rows.Count)
                {
                    selectedRow = result;
                    LoadAll();
                }
            }
        }
    }
}
