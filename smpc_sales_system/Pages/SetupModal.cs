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
    public partial class SetupModal : Form
    {
        public string SetupTitle { get; set; }
        public SetupModal(string setupTitle)
        {
            InitializeComponent();
            lbl_setup_title.Text = setupTitle;
            
        }

        public SetupModal()
        {
            InitializeComponent();
        }

        private void btn_save_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private async void FetchData()
        {
            // 
            // try catch block to catch errors
            //
            try
            {
                var data = await ApplicationService.GetAsDatatable();
                dgv_application_setup.DataSource = data;
            }
            catch (Exception ex )
            {
                MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            
        }

        private void SetupModal_Load(object sender, EventArgs e)
        {
            FetchData();
        }
    }
}
