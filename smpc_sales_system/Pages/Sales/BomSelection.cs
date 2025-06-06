using smpc_sales_app.Services.Helpers;
using smpc_sales_system.Services.Sales;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace smpc_sales_system.Pages.Sales
{
    public partial class BomSelection : Form
    {
        public BomSelection(DataTable dt)
        {
            InitializeComponent();
            FetchBomItems();
            this.itemlist = dt;
        }

        Dictionary<string, dynamic> result = new Dictionary<string, dynamic>();
        DataTable table { get; set; }
        DataTable childTable { get; set; }
        public Dictionary<string,dynamic> GetResult()
        {
            return result;
        }

        public DataTable GetChildTable()
        {
            return table;
        }
        public DataTable GetParentTable()
        {
            return childTable;
        }

        DataTable itemlist = new DataTable();
        private async void FetchBomItems()
        {
            var data = await ProjectService.GetBom();
            DataTable parent = JsonHelper.ToDataTable(data.bom_head);
            DataTable child = JsonHelper.ToDataTable(data.bom_details);
            table = child;
            childTable = parent;

            DataTable parentCopy = parent.Clone();
            DataTable childCopy = child.Clone();

            parentCopy.Columns.Add("item_name", typeof(string));
            //childCopy.Columns.Add("item_name", typeof(string));


            foreach (DataRow parentRow in parent.Rows)
            {
                DataRow newRow = parentCopy.NewRow();
                foreach (DataColumn col in parent.Columns)
                {
                    newRow[col.ColumnName] = parentRow[col.ColumnName];
                }

                string itemId = parentRow["item_id"].ToString();
                DataRow[] itemRows = itemlist.Select($"id = '{itemId}'");

                newRow["item_name"] = itemRows.Length > 0 ? itemRows[0]["item_name"].ToString() : "Unknown Item";

                parentCopy.Rows.Add(newRow);
            }

            // Populate childCopy
            foreach (DataRow childRow in child.Rows)
            {
                DataRow newRow = childCopy.NewRow();
                foreach (DataColumn col in child.Columns)
                {
                    newRow[col.ColumnName] = childRow[col.ColumnName];
                }

                string itemId = childRow["item_id"].ToString();
                DataRow[] itemRows = itemlist.Select($"id = '{itemId}'");

                newRow["item_name"] = itemRows.Length > 0 ? itemRows[0]["item_name"].ToString() : "Unknown Item";

                childCopy.Rows.Add(newRow);
            }

            dataGridView1.DataSource = parentCopy;

            foreach (DataGridViewColumn column in dataGridView1.Columns)
            {
                if (column.Name != "item_name" && column.Name != "id")
                {
                    column.Visible = false;
                }
            }
        }

        private void dataGridView1_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                string id = dataGridView1.Rows[e.RowIndex].Cells[0].Value.ToString();
                Dictionary<string, dynamic> data = new Dictionary<string, dynamic>()
                {
                    {"id", id }
                };
                this.result = data;
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
        }
    }
}
