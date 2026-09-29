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

            // Only the designer's columns. Left at the default, every column of the bound table
            // became a grid column as well: ItemName showed up as a raw column, and RefCode -
            // added to the table after binding - came back as a column of its own on Next and
            // pushed the designer's # column out (user-reported 2026-09-25).
            dgv_template.AutoGenerateColumns = false;
            dgv_template.MultiSelect = false;

            // Rows are added from the right-click menu, never from a blank row at the bottom:
            // that row half-exists in the bound table while the item picker is open, and what
            // it left behind was a stray empty row, or an insert in the wrong place.
            dgv_template.AllowUserToAddRows = false;

            addParentMenuItem.Click += addParentMenuItem_Click;
            deleteMenuItem.Click += deleteMenuItem_Click;
            contextMenuStrip1.Items.Insert(0, addParentMenuItem);
            contextMenuStrip1.Items.Add(deleteMenuItem);
            contextMenuStrip1.Opening += contextMenuStrip1_Opening;

            dgv_template.MouseDown += dgv_template_MouseDown;
            dgv_template.UserDeletingRow += dgv_template_UserDeletingRow;
        }

        private readonly ToolStripMenuItem addParentMenuItem = new ToolStripMenuItem("Add Parent");
        private readonly ToolStripMenuItem deleteMenuItem = new ToolStripMenuItem("Delete (with everything under it)");

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

            // The empty shape of the list. It used to be scraped off the grid, so whatever
            // columns the grid had grown by then (see the constructor) came along too - two
            // RefCode columns, for one.
            stockQuickDataTable = EmptyTemplateTable();

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

                DataTable firstDetailsRow = filteredRows.Any()
                    ? filteredRows.CopyToDataTable()
                    : TemplateChild.Clone(); // Returns empty DataTable with same schema

                // RefCode before binding, so the # column has its property from the start.
                dgv_template.DataSource = WithRefCode(firstDetailsRow);
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
            dgv_template.AllowUserToAddRows = false;
            dgv_template.AllowUserToDeleteRows = !Visibled;

            // The menu is the way rows are added, so it belongs to the whole list - including
            // the empty space under the last row, which is where an empty template starts. The
            // designer attaches it to the COMPONENTS column only. None in view mode: a menu that
            // appears and then does nothing looks broken.
            component.ContextMenuStrip = null;
            dgv_template.ContextMenuStrip = Visibled ? null : contextMenuStrip1;
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

                // Clicking a component swaps the item on that row; the row keeps its place.
                if (dgv_template.Columns[e.ColumnIndex].Name == "component")
                {
                    ReplaceComponentAt(e.RowIndex);
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

                AddChild(contextRowIndex);
            }
            catch (Exception ex)
            {
                MessageBox.Show("An error occurred while processing your request: " + ex.Message,
                                "Error",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Error);
            }
        }

        // ---- Building the list ------------------------------------------------------------
        //
        // Right-click menu (edit mode only):
        //   Add Parent - a new top-level component, below the whole group of the row that was
        //                right-clicked (at the end when the click was on empty space). Clicking
        //                2 CONTROLLER and adding BLADDER TANK gives 3 BLADDER TANK, and VFD moves
        //                from 3 to 4.
        //   Add Child  - a component one level under the row, as its LAST child, so children
        //                read in the order they were added: 3.1, then 3.2.
        //   Delete     - the row and everything under it. A child left behind would be
        //                re-numbered into whatever row is now above it.
        // Clicking a COMPONENTS cell swaps the item on that row and keeps its place.
        //
        // Every change rebuilds the list in its new order (Rebind), so the grid, the table
        // behind it and the save always agree on the order.

        private int contextRowIndex = -1;
        private int contextColIndex = -1;

        // The row the menu is for - or -1 for the empty space under the rows.
        private void dgv_template_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right) return;

            DataGridView.HitTestInfo hit = dgv_template.HitTest(e.X, e.Y);
            contextRowIndex = hit.RowIndex;
            contextColIndex = hit.ColumnIndex;

            dgv_template.ClearSelection();
            if (contextRowIndex >= 0)
                dgv_template.Rows[contextRowIndex].Selected = true;
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

        private void contextMenuStrip1_Opening(object sender, CancelEventArgs e)
        {
            if (!isEditing)
            {
                e.Cancel = true;
                return;
            }

            bool onRow = RowAtGridIndex(contextRowIndex) != null;
            addChildToolStripMenuItem.Enabled = onRow;
            deleteMenuItem.Enabled = onRow;
        }

        private void addParentMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                if (!isEditing) return;
                AddParent(contextRowIndex);
            }
            catch (Exception ex)
            {
                MessageBox.Show("The component could not be added: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void deleteMenuItem_Click(object sender, EventArgs e)
        {
            if (!isEditing) return;
            DeleteWithChildren(contextRowIndex);
        }

        // The Delete key goes the same way as the menu, so a parent never leaves its children.
        private void dgv_template_UserDeletingRow(object sender, DataGridViewRowCancelEventArgs e)
        {
            e.Cancel = true;
            if (!isEditing) return;

            int index = e.Row.Index;
            BeginInvoke(new Action(() => DeleteWithChildren(index)));
        }

        // Nothing reaches here any more - deletes are cancelled and done by
        // DeleteWithChildren - but the designer still wires it.
        private void dgv_template_UserDeletedRow(object sender, DataGridViewRowEventArgs e)
        {
            RenumberTemplateRows();
        }

        private void AddParent(int gridIndex)
        {
            CommitGridEdit();
            List<DataRow> rows = LiveRows();

            int at = rows.Count;
            DataRow anchor = RowAtGridIndex(gridIndex);
            if (anchor != null)
            {
                // Below the whole group the right-clicked row belongs to: up to its top-level
                // row, then past everything under that.
                int i = rows.IndexOf(anchor);
                while (i > 0 && LevelOf(rows[i]) > 1) i--;
                at = SubtreeEnd(rows, i);
            }

            DataRow added = NewComponentRow(1);
            if (added == null) return;

            rows.Insert(at, added);
            Rebind(rows, added);
        }

        private void AddChild(int gridIndex)
        {
            CommitGridEdit();
            List<DataRow> rows = LiveRows();

            DataRow parent = RowAtGridIndex(gridIndex);
            if (parent == null) return;

            int i = rows.IndexOf(parent);
            DataRow added = NewComponentRow(LevelOf(parent) + 1);
            if (added == null) return;

            rows.Insert(SubtreeEnd(rows, i), added);
            Rebind(rows, added);
        }

        private void DeleteWithChildren(int gridIndex)
        {
            CommitGridEdit();
            List<DataRow> rows = LiveRows();

            DataRow target = RowAtGridIndex(gridIndex);
            if (target == null) return;

            int i = rows.IndexOf(target);
            int end = SubtreeEnd(rows, i);
            int under = end - i - 1;

            if (under > 0)
            {
                string name = (target["Components"]?.ToString() ?? "").Trim();
                if (MessageBox.Show($"Delete {name} and the {under} component{(under == 1 ? "" : "s")} under it?",
                        "Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                    return;
            }

            rows.RemoveRange(i, end - i);
            Rebind(rows, rows.Count == 0 ? null : rows[Math.Min(i, rows.Count - 1)]);
        }

        // Swaps the item on an existing row, keeping its level and therefore its place in
        // the hierarchy. The name comes from the new item; a note in brackets belonged to the
        // old one and goes with it.
        private void ReplaceComponentAt(int gridIndex)
        {
            CommitGridEdit();
            DataRow target = RowAtGridIndex(gridIndex);
            if (target == null) return;

            DataRow picked = PickItem();
            if (picked == null) return;

            target["ItemId"] = picked["id"];
            target["Components"] = picked["item_name"];
            if (target.Table.Columns.Contains("ItemName"))
                target["ItemName"] = picked["item_name"];
            if (target.Table.Columns.Contains("unit_of_measure") && picked.Table.Columns.Contains("unit_of_measure"))
                target["unit_of_measure"] = picked["unit_of_measure"];

            RenumberTemplateRows();
        }

        // A new row for the item the user picks, at the given level - or null if they picked
        // nothing. Not yet in the list: the caller puts it where it belongs.
        //
        // The component is the item's NAME - the picker lists one row per item name (the
        // item's item_name_id), and a template names a kind of component, not one model of
        // it. The row keeps the item id the picker handed back, which is what the quotation
        // uses when the template is applied.
        private DataRow NewComponentRow(int level)
        {
            DataRow picked = PickItem();
            if (picked == null) return null;

            DataTable table = GridTable;
            DataRow row = table.NewRow();
            row["ItemId"] = picked["id"];
            row["Components"] = picked["item_name"];
            row["Level"] = level;
            if (table.Columns.Contains("ItemName"))
                row["ItemName"] = picked["item_name"];
            if (table.Columns.Contains("unit_of_measure") && picked.Table.Columns.Contains("unit_of_measure"))
                row["unit_of_measure"] = picked["unit_of_measure"];
            return row;
        }

        // The picker's choice, as a row of ItemList (the picker leaves it there).
        private DataRow PickItem()
        {
            using (SalesItemModal itemModal = new SalesItemModal(ItemList))
            {
                if (itemModal.ShowDialog() != DialogResult.OK) return null;

                int itemId = itemModal.GetParentItemId();
                DataTable found = Helpers.FilterExactDataTable(ItemList, itemId.ToString(), "id");
                if (found.Rows.Count == 0)
                {
                    MessageBox.Show("Invalid selection. Item not found.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return null;
                }
                return found.Rows[0];
            }
        }

        private DataTable GridTable => dgv_template.DataSource as DataTable;

        // The rows in the order the list shows them.
        private List<DataRow> LiveRows()
        {
            DataTable table = GridTable;
            if (table == null) return new List<DataRow>();
            return table.Rows.Cast<DataRow>().Where(r => r.RowState != DataRowState.Deleted).ToList();
        }

        private DataRow RowAtGridIndex(int gridIndex)
        {
            if (gridIndex < 0 || gridIndex >= dgv_template.Rows.Count) return null;
            return (dgv_template.Rows[gridIndex].DataBoundItem as DataRowView)?.Row;
        }

        private static int LevelOf(DataRow row)
        {
            int.TryParse(row["Level"]?.ToString(), out int level);
            return Math.Max(1, level);
        }

        // Just past row i's group: everything after it that sits deeper than it.
        private static int SubtreeEnd(List<DataRow> rows, int i)
        {
            int level = LevelOf(rows[i]);
            int j = i + 1;
            while (j < rows.Count && LevelOf(rows[j]) > level) j++;
            return j;
        }

        // A typed QTY still in its cell belongs to the row before the list is rebuilt.
        private void CommitGridEdit()
        {
            dgv_template.EndEdit();
            if (GridTable != null)
                BindingContext[GridTable].EndCurrentEdit();
        }

        // Rebinds the list to a fresh table holding `rows` in this order, renumbers it, and
        // selects `select`. A fresh table rather than InsertAt/Remove on the bound one: a
        // bound view does not reliably show an InsertAt row where it was inserted, and the
        // numbering, the grid and the save all have to agree on one order.
        private void Rebind(List<DataRow> rows, DataRow select)
        {
            DataTable source = GridTable ?? stockQuickDataTable;
            DataTable fresh = WithRefCode(source.Clone());

            DataRow selectCopy = null;
            foreach (DataRow row in rows)
            {
                DataRow copy = fresh.Rows.Add(row.ItemArray);
                if (row == select) selectCopy = copy;
            }

            dgv_template.DataSource = fresh;
            RenumberTemplateRows();

            if (selectCopy != null)
            {
                int index = fresh.Rows.IndexOf(selectCopy);
                if (index >= 0 && index < dgv_template.Rows.Count)
                {
                    dgv_template.ClearSelection();
                    dgv_template.Rows[index].Selected = true;
                    dgv_template.CurrentCell = dgv_template.Rows[index].Cells["component"];
                }
            }
        }

        // The list's table with no rows: the shape the API sends, or the model's own before
        // anything has been fetched - with RefCode, which the # column shows.
        private DataTable EmptyTemplateTable()
        {
            DataTable empty = TemplateChild != null && TemplateChild.Columns.Count > 0
                ? TemplateChild.Clone()
                : JsonHelper.ToDataTable(new List<ProjectTemplateChildModel>());
            return WithRefCode(empty);
        }

        private static DataTable WithRefCode(DataTable table)
        {
            if (!table.Columns.Contains("RefCode"))
                table.Columns.Add("RefCode", typeof(string));
            return table;
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

            // The stored levels of the rows still "open" above this one - its parent, the
            // parent's parent... See below.
            List<int> openLevels = new List<int>();

            foreach (DataRow row in rows.Rows)
            {
                if (row.RowState == DataRowState.Deleted) continue;

                int level = 1;
                if (rows.Columns.Contains("Level"))
                    int.TryParse(row["Level"]?.ToString(), out level);

                // A row saved by an older build as level 0 is a top-level row.
                if (level < 1) level = 1;

                // A child is always ONE level under its parent: the child of 9 is 9.1. The
                // stored data has rows two levels under their parent (CPS's valves sit at 3
                // under the level-1 BLADDER TANK), which this used to number 9.1.1 on purpose
                // - the user ruled that a bug (2026-09-25).
                //
                // So the level used is the row's depth among the rows open above it, not the
                // stored number: close every open row stored at this level or deeper, and
                // what is left is this row's ancestors. Siblings stored at the same level stay
                // siblings (GATE VALVE and BALL VALVE both become 9.x), which pulling each row
                // up to "one under the row above" would not do. The corrected level is written
                // back, so the next Save stores it.
                while (openLevels.Count > 0 && openLevels[openLevels.Count - 1] >= level)
                    openLevels.RemoveAt(openLevels.Count - 1);
                openLevels.Add(level);
                level = openLevels.Count;

                if (rows.Columns.Contains("Level") && row["Level"]?.ToString() != level.ToString())
                    row["Level"] = level;

                while (counters.Count <= level)
                    counters.Add(0);

                // Coming back up, everything deeper starts again - which is what turns the
                // row after 2.2.4 into 2.3 rather than 2.5.
                for (int deeper = level + 1; deeper < counters.Count; deeper++)
                    counters[deeper] = 0;

                counters[level]++;

                // Skip(1): slot 0 is not part of any code.
                row["RefCode"] = string.Join(".", counters.Skip(1).Take(level));

                if (rows.Columns.Contains("Components"))
                {
                    // The component is its item's name (see ProjectTemplateChildModel.
                    // ComponentText), so a rename in Item Entry shows here without anybody
                    // editing the template; a note in brackets after the name is kept. A row
                    // with no item - a heading somebody typed - keeps its own text.
                    string text = (row["Components"]?.ToString() ?? "").TrimStart();

                    if (rows.Columns.Contains("ItemName") && rows.Columns.Contains("ItemId"))
                    {
                        int.TryParse(row["ItemId"]?.ToString(), out int itemId);
                        if (itemId > 0)
                            text = ProjectTemplateChildModel.ComponentText(text, row["ItemName"]?.ToString());
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
