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
    public partial class PumpItemModal : Form
    {
        private DataTable dtItem;
        private int itemId;
        public PumpItemModal(DataTable dtItem)
        {
            InitializeComponent();
            this.dtItem = dtItem;
        }
        private void fetchData()
        {
            DataView dataview = new DataView(dtItem);
            dgv_itemList.DataSource = dataview;

            IdentifyItemType(dgv_itemList);

            dgv_itemList.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        }

        private void IdentifyItemType(DataGridView dgv)
        {


            foreach (DataGridViewRow row in dgv.Rows)
            {
                if (row.IsNewRow) continue;

                int itemId = Convert.ToInt32(row.Cells["item_id"].Value);
            }
            foreach (DataGridViewColumn col in dgv.Columns)
            {
                if (col.Name == "item_name" || col.Name == "item_title" || col.Name == "item_value")
                {
                    col.Visible = true;
                }
                else
                {
                    col.Visible = false;
                }
            }

            // Set column headers (safe check in case they exist)
            if (dgv.Columns.Contains("item_name"))
                dgv.Columns["item_name"].HeaderText = "ITEM NAME";
            if (dgv.Columns.Contains("item_title"))
                dgv.Columns["item_title"].HeaderText = "ITEM TITLE";
            if (dgv.Columns.Contains("item_value"))
                dgv.Columns["item_value"].HeaderText = "VALUE";
        }

        public int GetItemId()
        {
            return itemId;
        }

        private void PumpItemModal_Load(object sender, EventArgs e)
        {
            fetchData();
        }

        private void dgv_itemList_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                int itemID = Convert.ToInt32(dgv_itemList.Rows[e.RowIndex].Cells["item_id"].Value);
            }
        }
    }
}
