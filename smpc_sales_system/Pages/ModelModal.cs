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
        private DataTable _ItemData, _BomHead, _BomDetail;
        public event Action<String> CreateSuccess;
        int itemId = 0, bomId = 0, id = 0;
        bool isBom = false;

        public ModelModal(DataTable Item, string Id)
        {
            InitializeComponent();

            id = int.Parse(Id);
            _ItemData = Item;

            fetchData();
        }

        public ModelModal(DataTable Item, DataTable BomHead, DataTable BomDetails, string Id)
        {
            InitializeComponent();

            id = int.Parse(Id);
            _ItemData = Item;
            _BomHead = BomHead;
            _BomDetail = BomDetails;

            fetchData();
        }


        private void fetchData()
        {
            int item_name_id = _ItemData.AsEnumerable()
                .Where(row => row.Field<int>("id") == id)
                .Select(row => row.Field<int>("item_name_id"))
                .FirstOrDefault();

            DataTable ItemData = _ItemData.AsEnumerable()
            .Where(row => row.Field<int>("item_name_id") == item_name_id)
            .CopyToDataTable();

            if (!ItemData.Columns.Contains("Type"))
                ItemData.Columns.Add("Type", typeof(string));

            foreach (DataRow row in ItemData.Rows)
            {
                int itemId = Convert.ToInt32(row["id"]);
                bool isBOM = _BomHead != null &&
                             _BomHead.AsEnumerable().Any(r => r.Field<int>("item_id") == itemId);
                row["Type"] = isBOM ? "BOM" : "SINGLE";
            }

            DataView dv = new DataView(ItemData);
            DataGridViewModel.DataSource = dv;

            IdentifyItemType(DataGridViewModel);
        }

        private void IdentifyItemType(DataGridView dgv)
        {
            foreach (DataGridViewColumn col in dgv.Columns)
            {
                if (col.Name == "item_code" || col.Name == "item_model" || col.Name == "Type")
                {
                    col.Visible = true;
                }
                else
                {
                    col.Visible = false;
                }
            }

            // Set column headers (safe check in case they exist)
            if (dgv.Columns.Contains("item_code"))
                dgv.Columns["item_code"].HeaderText = "ITEM CODE";
            if (dgv.Columns.Contains("item_model"))
                dgv.Columns["item_model"].HeaderText = "ITEM MODEL";
            if (dgv.Columns.Contains("Type"))
                dgv.Columns["Type"].HeaderText = "TYPE";
        }
        public int GetItemId()
        {
            return itemId;
        }
        public int GetBomId()
        {
            return bomId;
        }
        public bool IsBom()
        {
            return isBom;
        }
        private void DataGridViewModel_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {

            var row = DataGridViewModel.Rows[e.RowIndex];
            int id = row.Cells["id"].Value != null ? Convert.ToInt32(row.Cells["id"].Value) : 0;

            if (id == 0)
            {
                MessageBox.Show("Invalid selection. Please select a valid item.");
                return;
            }
            else
            {
                itemId = id;

                bomId = _BomHead.AsEnumerable()
                .Where(hrow => hrow.Field<int>("item_id") == id)
                .Select(hrow => hrow.Field<int>("id"))
                .FirstOrDefault();

                this.DialogResult = DialogResult.OK;
                this.Close();
            }
        }
    }
}
