using smpc_inventory_app.Pages;
using smpc_inventory_app.Pages.Engineering.Boq;
using smpc_sales_app.Pages;
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
    public partial class ModelModal : Form
    {
        private DataTable _ItemData;
        public event Action<String> CreateSuccess;
        string itemId, bomId;

        public ModelModal(DataTable Item, string Id)
        {
            InitializeComponent();

            int item_name_id = Item.AsEnumerable()
                .Where(row => row.Field<int>("id") == int.Parse(Id))
                .Select(row => row.Field<int>("item_name_id"))
                .FirstOrDefault();

            _ItemData = Item.AsEnumerable()
            .Where(row => row.Field<int>("item_name_id") == item_name_id)
            .CopyToDataTable();

            fetchData();
        }

        private void fetchData()
        {
            DataView dv = new DataView(_ItemData);
            DataGridViewModel.DataSource = dv;

            foreach (DataGridViewColumn col in DataGridViewModel .Columns)
            {
                if (col.Name == "item_model" || col.Name == "item_brand")
                {
                    col.Visible = true;
                }
                else
                {
                    col.Visible = false;
                }
            }

            if (DataGridViewModel.Columns.Contains("item_model"))
                DataGridViewModel.Columns["item_model"].HeaderText = "MODEL";
            if (DataGridViewModel.Columns.Contains("item_brand"))
                DataGridViewModel.Columns["item_brand"].HeaderText = "BRAND";
        }
        public string GetItemId()
        {
            return itemId;
        }
        public string GetBomId()
        {
            return bomId;
        }
        private void DataGridViewModel_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {

            var row = DataGridViewModel.Rows[e.RowIndex];
            itemId = row.Cells["item_id"].Value?.ToString();
            bomId = row.Cells["bom_id"].Value?.ToString();

            
            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}
