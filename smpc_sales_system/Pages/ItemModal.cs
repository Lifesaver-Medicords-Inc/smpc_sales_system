using smpc_sales_app.Pages.Sales;
using smpc_sales_app.Services.Helpers;
using smpc_sales_app.Services.Sales;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace smpc_sales_app.Pages
{
    public partial class ItemModal : Form
    {

        private DataTable dt;
        Quotation quote = new Quotation();
        int result;

        public ItemModal(DataTable dgv)
        {
            InitializeComponent();
            this.dt = dgv;
        }

        public int GetResult()
        {
            return result;
        }

        private void dgv_itemList_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                this.result = e.RowIndex;
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
    }
}
