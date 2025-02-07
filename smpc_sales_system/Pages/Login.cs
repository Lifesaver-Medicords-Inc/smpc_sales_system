using smpc_app.Services.Helpers;
using smpc_inventory_app.Services.Setup.Item;
using smpc_sales_app.Data;
using smpc_sales_app.Services.Sales;
using smpc_sales_app.Services.Setup;
using smpc_sales_system.Services;
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
    public partial class Login : Form
    {
        public Login()
        {
            InitializeComponent();
        }

        private void frm_login_FormClosing(object sender, FormClosingEventArgs e)
        {
            
        }

        private void btn_cancel_Click(object sender, EventArgs e)
        { 
            Application.Exit();
        }

        private async void btn_login_Click(object sender, EventArgs e)
        {
        
        }

        private void frm_login_Load(object sender, EventArgs e)
        {

        }

        private void txt_password_TextChanged(object sender, EventArgs e)
        {

        }

        private async void btn_login_Click_1(object sender, EventArgs e)
        {
            //CacheData.ShipTypeSetup = await ShipService.GetAsDatatable(); 

            var data =   Helpers.GetControlsValues(pnl_auth);
            data.Add("motherboard_serial_no", Helpers.GetSerialNumber());
            data.Add("machine_name", Environment.MachineName);
            var currentUser = await AuthServices.Login(data);
            CacheData.CurrentUser = currentUser.Data;
            CacheData.PaymentTerms = await PaymentTermsServices.GetAsDatatable();
            CacheData.ApplicationSetup = await ApplicationService.GetAsDatatable();
            CacheData.UoM = await UnitOfMeasurementServices.GetAsDatatable();

            this.DialogResult = DialogResult.OK;
        }
    }
}
