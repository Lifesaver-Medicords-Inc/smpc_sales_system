using System;
using smpc_app.Services.Helpers;
using smpc_sales_app.Data;
using smpc_sales_app.Services.Sales;
using smpc_sales_app.Services.Sales.Models;
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
    public partial class ShipTypeSetup : UserControl
    {
        public ShipTypeSetup()
        {
            InitializeComponent();

        }

        private async void FetchData()
        {
            CacheData.ShipTypeSetup = await ShipService.GetAsDatatable();
            dgv_shiptype_setup.DataSource = CacheData.ShipTypeSetup;
        }

        private async void DisableBtn()
        {
            btn_delete.Enabled = false;
            btn_edit.Enabled = false;
            txt_id.Enabled = false;
            txt_ship_name.Enabled = false;
            btn_save.Visible = false;
        }

        private async void EnableTxtBtn()
        {
            txt_ship_name.Enabled = true;
        }

        private async void ShipTypes_Load(object sender, EventArgs e)
        {
            DisableBtn();
            FetchData();
            dgv_shiptype_setup.CellClick += dgv_shiptype_setup_CellClick;
        }
        private void label1_Click(object sender, EventArgs e)
        {

        }
        private void dgv_shiptype_setup_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                btn_edit.Enabled = true;
                btn_delete.Enabled = true;

                var nameValue = dgv_shiptype_setup.Rows[e.RowIndex].Cells["ship_name"].Value;
                var idValue = dgv_shiptype_setup.Rows[e.RowIndex].Cells["id"].Value;

                if (nameValue != null)
                {
                    txt_id.Text = idValue.ToString();
                    txt_ship_name.Text = nameValue.ToString();

                    txt_ship_name.Enabled = true;

                    txt_ship_name.ReadOnly = true;

                }
            }
        }

        private async void btn_edit_Click(object sender, EventArgs e)
        {
            txt_id.ReadOnly = false;
            txt_ship_name.ReadOnly = false;

            btn_save.Visible = true;
        }

        private async void btn_save_Click(object sender, EventArgs e)
        {
            var data = Helpers.GetControlsValues(pnl_input);
            ApiResponseModel response = new ApiResponseModel();

            string errorMessage =
                string.IsNullOrWhiteSpace(txt_ship_name.Text) ? "Name cannot be empty." : null;

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
            else
            {
                if (data.ContainsKey("id"))
                {
                    var idValue = data["id"];
                    if (idValue is string idString && int.TryParse(idString, out int id))
                    {
                        data["id"] = id;
                    }
                }
            }

            response = isNewRecord
                ? await ShipService.Insert(data)
                : await ShipService.Update(data);

            if (response.Success)
            {
                Helpers.ResetControls(pnl_input);
                FetchData();
            }

            string message = response.Success
                ? (isNewRecord ? "Ship type saved successfully." : "Ship type updated successfully.")
                : (isNewRecord ? "Failed to save ship type.\n" + response.message : "Failed to update ship type.\n" + response.message);

            Helpers.ShowDialogMessage(response.Success ? "success" : "error", message);
        }


        private async void btn_new_Click(object sender, EventArgs e)
        {
            btn_save.Visible = true;
            btn_new.Enabled = false;

            txt_ship_name.Enabled = true;
            txt_ship_name.ReadOnly = false;
        }
        private async void btn_delete_Click(object sender, EventArgs e)
        {
            DialogResult result = MessageBox.Show("Are you sure you want to delete this item?",
                "Confirm Deletion", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                //var data = Helpers.GetControlsValues(pnl_input);
                var id = txt_id.Text;
                int appId;
                if (int.TryParse(id, out appId))
                {
                    var data = new Dictionary<string, dynamic>
                    {
                         { "id", appId }
                    };
                    bool isSuccess = await ShipService.Delete(data);
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


        private void toolStrip1_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {

        }

        private void pnl_input_Paint(object sender, PaintEventArgs e)
        {

        }

        private void dgv_shiptype_setup_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

        }

        private void lbl_code_Click(object sender, EventArgs e)
        {

        }

        private void pnl_input_Paint_1(object sender, PaintEventArgs e)
        {

        }

        private void dgv_shiptype_setup_CellContentClick_1(object sender, DataGridViewCellEventArgs e)
        {

        }
    }
}
