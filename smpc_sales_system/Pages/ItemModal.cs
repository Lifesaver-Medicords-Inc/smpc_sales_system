using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using smpc_app.Services.Helpers;
using smpc_inventory_app.Services.Setup.Model.Item;
using smpc_sales_app.Pages.Sales;
using smpc_sales_app.Services.Helpers;
using smpc_sales_app.Services.Sales;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace smpc_sales_app.Pages
{
    public partial class ItemModal : Form
    {

        private DataTable dt;
        private DataTable bomHead;
        private DataTable bomDetails;
        Quotation quote = new Quotation();
        int result;
        int bomResult;
        int itemId;




        public ItemModal(DataTable dgv)
        {
            InitializeComponent();
            this.dt = dgv;
        }

        public ItemModal(DataTable dgv, DataTable BomHead, DataTable BomDetails)
        {
            InitializeComponent();
            this.dt = dgv;
            this.bomHead = BomHead;
            this.bomDetails = BomDetails;
        }


        public int GetResult()
        {
            return result;
        }
        public int GetBomResult()
        {
            return bomResult;
        }
        public int GetParentItemId()
        {
            return itemId;
        }

        public bool isBom { get; set; }

        public bool isItem { get; set; }
  




            

        private void dgv_itemList_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                int itemID = Convert.ToInt32(dgv_itemList.Rows[e.RowIndex].Cells["id"].Value);

                checkIfItemHasBom(itemID, e.RowIndex);
                //this.DialogResult = DialogResult.OK;
                //this.Close();
            }
        }


        private void checkIfItemHasBom(int itemid, int rowIndex)
        {
            int? checkData = bomHead.AsEnumerable()
                        .Where(row => row.Field<int>("item_id") == itemid)
                        .Select(row => row.Field<int>("id"))
                        .FirstOrDefault();

            if (checkData != 0)
            {
                MessageBox.Show("Item has bom");
                this.bomResult = checkData.Value;
                this.itemId = itemid;
                isBom = true;
                this.DialogResult = DialogResult.OK;
                this.Close();

            }
            else
            {
                MessageBox.Show("no bom");
                this.result = rowIndex;
                this.itemId = itemid;
                isItem = true;
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
        }

        private void fetchData()
        {
            DataView dataview = new DataView(dt);
            dgv_itemList.DataSource = dataview;

            foreach (DataGridViewColumn column in dgv_itemList.Columns)
            {
                if (column.Name != "item_code" && column.Name != "item_name")
                {
                    column.Visible = false;
                }
            }
        }

        // load data
        private void ItemModal_Load(object sender, EventArgs e)
        {
            fetchData();
        }


        private async void button2_Click(object sender, EventArgs e)
        {
            Panel[] pnl_list = { pnl_title };
            var data = Helpers.GetControlsValues(pnl_list);
            
        }

    }
}
