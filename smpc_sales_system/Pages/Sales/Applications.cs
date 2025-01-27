using smpc_app.Services.Helpers;
using smpc_inventory_app.Services.Helpers;
using smpc_sales_app.Data;
using smpc_sales_app.Services.Sales;
using smpc_sales_app.Services.Sales.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace smpc_sales_app.Pages.Sales
{
    public partial class Applications : UserControl
    {
        public Applications()
        {
            InitializeComponent();
        }

        private async void FetchData()
        {
            var data = await ApplicationService.GetAsDatatable();
            dgv_application_setup.DataSource = data;
        }

        private async void DisableBtn()
        {
            btn_delete.Enabled = false;
            btn_edit.Enabled = false;
            txt_code.Enabled = false;
            txt_id.Enabled = false;
            txt_name.Enabled = false;
            btn_save.Visible = false;
        }

        private async void EnableTxtBtn()
        {
            txt_code.Enabled = true;
            txt_name.Enabled = true;
        }

        private async void Applications_Load(object sender, EventArgs e)
        {
            DisableBtn();
            FetchData();
            dgv_application_setup.CellClick += dgv_application_setup_CellClick;
        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void dgv_application_setup_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                btn_edit.Enabled = true;
                btn_delete.Enabled = true;

                var nameValue = dgv_application_setup.Rows[e.RowIndex].Cells["NAME"].Value;
                var codeValue = dgv_application_setup.Rows[e.RowIndex].Cells["CODE"].Value;
                var idValue = dgv_application_setup.Rows[e.RowIndex].Cells["ID"].Value;

                if (nameValue != null)
                {
                    txt_code.Text = codeValue.ToString();
                    txt_id.Text = idValue.ToString();
                    txt_name.Text = nameValue.ToString();

                    txt_code.Enabled = true;
                    txt_name.Enabled = true;

                    txt_code.ReadOnly = true;
                    txt_name.ReadOnly = true;

                }
            }
        }

        private async void btn_edit_Click(object sender, EventArgs e)
        {
            txt_code.ReadOnly = false;
            txt_id.ReadOnly = false;
            txt_name.ReadOnly = false;

            btn_save.Visible = true;
        }

        // SAVE (either new data or updating the data)
        private async void btn_save_Click(object sender, EventArgs e)
        {
            
            var data = Helpers.GetControlsValues(pnl_input);
            ApiResponseModel response = new ApiResponseModel();
            
            string errorMessage =
                string.IsNullOrWhiteSpace(txt_code.Text) && string.IsNullOrWhiteSpace(txt_name.Text) ? "Code and Name cannot be empty." :
                string.IsNullOrWhiteSpace(txt_code.Text) ? "Code cannot be empty." :
                string.IsNullOrWhiteSpace(txt_name.Text) ? "Name cannot be empty." : null;
            if (!string.IsNullOrEmpty(errorMessage))
            {
                Helpers.ShowDialogMessage("error", errorMessage);
                return;
            }
           
            bool isNewRecord = string.IsNullOrWhiteSpace(txt_id.Text);
            if (isNewRecord)
            {
                data.Remove("id");
            }
            response = isNewRecord
                ? await ApplicationService.Insert(data)
                : await ApplicationService.Update(data);

            if (response.Success)
            {
                Helpers.ResetControls(pnl_input);
                FetchData();
            }
           
            string message = response.Success
                ? (isNewRecord ? "Application saved successfully." : "Application updated successfully.")
                : (isNewRecord ? "Failed to save application.\n" + response.message : "Failed to update application.\n" + response.message);
            Helpers.ShowDialogMessage(response.Success ? "success" : "error", message);
        }

        // ADD NEW 
        private async void btn_new_Click(object sender, EventArgs e)
        {
            btn_save.Visible = true;
            btn_new.Enabled = false;

            txt_code.Enabled = true;
            txt_name.Enabled = true;
            txt_name.ReadOnly = false;
            txt_code.ReadOnly = false;
        }

        // DELETE
        private async void btn_delete_Click(object sender, EventArgs e)
        {
            DialogResult result = MessageBox.Show("Are you sure you want to delete this item?",
                "Confirm Deletion", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                var data = Helpers.GetControlsValues(pnl_input);
                bool isSuccess = await ApplicationService.Delete(data);

                if (isSuccess)
                {
                    Helpers.ResetControls(pnl_input);
                    Helpers.ShowDialogMessage("success", "Application deleted successfully!");
                    FetchData();
                }
                else
                {
                    Helpers.ShowDialogMessage("error", "Failed to delete the application");
                }
            }
        }
    }
}
