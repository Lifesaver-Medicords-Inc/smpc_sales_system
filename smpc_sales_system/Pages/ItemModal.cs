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
    public partial class SalesItemModal : Form
    {

        private DataTable dtItem;
        private DataTable bomHead;
        private DataTable bomDetails;
        Quotation quote = new Quotation();
        int result;
        int bomResult;
        int itemId;

        public SalesItemModal(DataTable Item)
        {
            InitializeComponent();
            this.dtItem = Item;
        }

        public SalesItemModal(DataTable Item, DataTable BomHead, DataTable BomDetails)
        {
            InitializeComponent();
            this.dtItem = Item;
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
            }
        }

        private void checkIfItemHasBom(int itemid, int rowIndex)
        {
            if (bomHead == null || bomHead.Rows.Count == 0)
            {
                MessageBox.Show("No BOM data available at all.");
                isItem = true;
                this.result = rowIndex;
                this.itemId = itemid;
                this.DialogResult = DialogResult.OK;
                this.Close();
                return;
            }

            int? checkData = bomHead.AsEnumerable()
                        .Where(row => row.Field<int>("item_id") == itemid)
                        .Select(row => row.Field<int>("id"))
                        .FirstOrDefault();

            if (checkData != 0)
            {
                this.bomResult = checkData.Value;
                this.itemId = itemid;
                isBom = true;
            }
            else
            {
                this.result = rowIndex;
                this.itemId = itemid;
                isItem = true;
            }

            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void fetchData()
        {
            DataView dataview = new DataView(dtItem);
            dgv_itemList.DataSource = dataview;

            IdentifyItemType(dgv_itemList);

            dgv_itemList.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        }

        private void ItemModal_Load(object sender, EventArgs e)
        {
            fetchData();
        }

        private void btn_search_Click(object sender, EventArgs e)
        {
            string searchText = txt_specs.Text.Trim();
            DataView dv = new DataView(dtItem);
            dv.RowFilter = $"item_code LIKE '%{searchText}%' OR item_name LIKE '%{searchText}%'";
            dgv_itemList.DataSource = dv;

            IdentifyItemType(dgv_itemList);
        }
        private void IdentifyItemType(DataGridView dgv)
        {
            // Ensure "Type" column exists
            if (!dgv.Columns.Contains("Type"))
            {
                dgv.Columns.Add("Type", "Type");
            }

            foreach (DataGridViewRow row in dgv.Rows)
            {
                if (row.IsNewRow) continue; 


                int itemId = Convert.ToInt32(row.Cells["id"].Value);

                bool isBOM = bomHead != null &&
                             bomHead.AsEnumerable().Any(r => r.Field<int>("item_id") == itemId);

                row.Cells["Type"].Value = isBOM ? "BOM" : "SINGLE";

                // //Optional: color coding
                //if (isBOM)
                //{
                //    row.DefaultCellStyle.BackColor = Color.SkyBlue;
                //    row.DefaultCellStyle.ForeColor = Color.White;
                //}
                //else
                //{
                //    row.DefaultCellStyle.BackColor = Color.White;
                //    row.DefaultCellStyle.ForeColor = Color.Black;
                //}
            }
            foreach (DataGridViewColumn col in dgv.Columns)
            {
                if (col.Name == "item_code" || col.Name == "item_name" || col.Name == "Type")
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
            if (dgv.Columns.Contains("item_name"))
                dgv.Columns["item_name"].HeaderText = "ITEM NAME";
            if (dgv.Columns.Contains("Type"))
                dgv.Columns["Type"].HeaderText = "TYPE";
        }

        private void dgv_itemList_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (dgv_itemList.Columns[e.ColumnIndex].Name == "item_code" && e.Value != null)
            {
                string modelValue = e.Value.ToString();
                if (!modelValue.StartsWith("I#"))
                {
                    e.Value = "I#" + modelValue;
                }
                e.FormattingApplied = true; // prevent re-formatting loop
            }
        }
    }
}
