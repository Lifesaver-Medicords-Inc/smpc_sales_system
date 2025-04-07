using smpc_app.Services.Helpers;
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
    public partial class PRModal : Form
    {

        private DataTable dt;
        Quotation quote = new Quotation();
        int result;

        public PRModal(DataTable dgv)
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
            dgv_prlist.DataSource = dataview;

            foreach (DataGridViewColumn column in dgv_prlist.Columns)
            {
                if (column.Name != "pr_id" && column.Name != "doc_no" && column.Name != "status")
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

        private void txt_search_TextChanged(object sender, EventArgs e)
        {
            string searchval = txt_search.Text.ToString();
            var data = Helpers.FilterDataTable(dt, searchval, "status", "doc_no", "pr_id");
            dgv_prlist.DataSource = data;
        }
    }
}
