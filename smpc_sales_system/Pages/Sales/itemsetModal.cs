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
    public partial class ItemSetModal : Form
    {
        public ItemSetModal()
        {
            InitializeComponent();
        }
        public string result { get; set; }


        public string GetResult()
        {
            return result;
        }


        private void btn_save_Click(object sender, EventArgs e)
        {
            result = txt_item_set_name.Text;

            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}
