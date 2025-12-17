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
            var data = await ProjectTemplatesService.GetProjectTemplates();

            Template = JsonHelper.ToDataTable(data.SalesProjectTemplate);
            TemplateChild = JsonHelper.ToDataTable(data.sales_project_template_child);

            var itemData = await ItemService.GetItem();

            var bomData = await ProjectService.GetBom();
            var companyData = await CompanyService.GetAsDatatable();

            if (itemData == null || bomData == null)
                return;

            ItemList = JsonHelper.ToDataTable(itemData.items);
            BomHead = JsonHelper.ToDataTable(bomData.bom_head);
            BomDetails = JsonHelper.ToDataTable(bomData.bom_details);

            LoadAll();
        }

        int selectedRow = 0;
        int template_id = 0;

        private async void LoadAll()
        {
            dgv_template.ReadOnly = false;
            stockQuickDataTable = Helpers.GetDataTableFromUnboundGrid(dgv_template);

            if (Template.Rows.Count > 0 && TemplateChild.Rows.Count > 0)
            {
                var item = await ItemService.GetItem();

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
                
                quickQuoteList.Add(data);
            }

            parent["sales_project_template_child"] = quickQuoteList;

            var send = new ApiResponseModel();

            if (editMode)
            {
                send = await ProjectTemplatesService.Update(parent);
            }
            else
            {  
                send = await ProjectTemplatesService.Insert(parent);
            }

            if (send.Success)
            {
                MessageBox.Show("Data successfully saved");
            }
            else
            {
                MessageBox.Show("Data failed to save");
            }

            NewButtonActive(true);
        }

        private void btn_close_Click(object sender, EventArgs e)
        {
            NewButtonActive(true);
        }

        private void dgv_template_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            try
            {
                // Skip header clicks
                if (e.RowIndex < 0 || e.ColumnIndex < 0)
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

                if (dgv.Rows[rowIndex].Cells["level"].Value == DBNull.Value)
                    dgv.Rows[rowIndex].Cells["level"].Value = 0;



                if (dgv.Rows[rowIndex].Cells["ItemId"].Value == DBNull.Value)
                    dgv.Rows[rowIndex].Cells["ItemId"].Value = 0;

                int parentItemId = Convert.ToInt32(dgv.Rows[rowIndex].Cells["ItemId"].Value);
                int level = Convert.ToInt32(dgv.Rows[rowIndex].Cells["level"].Value);

                if (!child)
                {
                    level = 0;
                }
                else
                {
                    level += 1;
                }

                    GetItemData(rowIndex, itemid, dgv, level, parentItemId);

            }
        }

        private int contextRowIndex = -1;
        private int contextColIndex = -1;

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
                newRow["Components"] = new string(' ', level * 4) + row["item_name"];
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

        private void btn_search_Click(object sender, EventArgs e)
        {

        }
    }
}
