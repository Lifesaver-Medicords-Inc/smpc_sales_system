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

        Quotation quote = new Quotation();
        private DataTable dt;
        public ItemModal(DataTable dgv)
        {
            InitializeComponent();
            this.dt = dgv;
        }

        private async void fetchData()
        {
            dgv_itemList.DataSource = this.dt;
        }

        // load data
        private void ItemModal_Load(object sender, EventArgs e)
        {
            fetchData();
        }

        int result;
        public int GetResult()
        {
            return result;
        }
        //public Dictionary<string, string> GetResult()
        //{
        //    return result;
        //}
        private void dgv_itemList_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
            {

                ////string id =  dgv_itemList.Rows[e.RowIndex].Cells[0].Value.ToString();
                //string name_id = dgv_itemList.Rows[e.RowIndex].Cells[1].Value.ToString();


                //Dictionary<string, string> data = new Dictionary<string, string>()
                //{
                //    //{ "id", id},
                //    //{ "code", name_id },
                //    //{ "name" , name},
                //    //{ "unitprice", unit_price},
                //    //{ "short_desc", short_desc}
                //};
                this.result = e.RowIndex;
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
        }
    }
}
