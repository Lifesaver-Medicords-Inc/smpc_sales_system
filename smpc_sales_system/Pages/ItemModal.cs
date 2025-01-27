using smpc_sales_app.Pages.Sales;
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
        private Dictionary<string, string> result { get; set; }
        public ItemModal()
        {
            InitializeComponent();
        }

        private async void fetchData()
        {
            var data = await ItemService.GetAsDataTable();
            dgv_itemList.DataSource = data;

        }

        private void ItemModal_Load(object sender, EventArgs e)
        {
            fetchData();
        }
        public Dictionary<string, string> GetResult()
        {
            return result;
        }
        private void dgv_itemList_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                
                //string id =  dgv_itemList.Rows[e.RowIndex].Cells[0].Value.ToString();
                string code = dgv_itemList.Rows[e.RowIndex].Cells[1].Value.ToString();
                string name =  dgv_itemList.Rows[e.RowIndex].Cells[2].Value.ToString();
                string unit_price =  dgv_itemList.Rows[e.RowIndex].Cells[3].Value.ToString();
                string short_desc = dgv_itemList.Rows[e.RowIndex].Cells[4].Value.ToString();

                Dictionary<string, string> data = new Dictionary<string, string>()
                {
                    //{ "id", id},
                    { "code", code },
                    { "name" , name},
                    { "unitprice", unit_price},
                    { "short_desc", short_desc}
                };
                this.result = data;
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
        }
    }
}
