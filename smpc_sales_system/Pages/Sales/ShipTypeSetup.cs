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

        // The three states this form can be in (spec 2.1: a form opens read-only and an
        // Edit button enters edit mode; saving returns it to read-only).
        private enum Mode { View, Add, Edit }

        private Mode _mode = Mode.View;

        // One place decides what every control does, because the old version spread it
        // across four handlers and each of them leaked:
        //
        //   - btn_save was never disabled by anything, so Save sat enabled in view mode
        //     with nothing being edited - press it on a freshly opened form and it POSTed
        //     whatever happened to be in the fields.
        //   - btn_new disabled itself on click and nothing ever re-enabled it, so New
        //     worked exactly once per visit to the page.
        //   - btn_edit set Visible = false on click and nothing restored it, so Edit
        //     disappeared off the strip for good.
        //   - Saving cleared the fields but left all of the above stuck wherever they
        //     were, so the form never actually returned to view mode.
        private void SetMode(Mode mode)
        {
            _mode = mode;

            bool editing = mode != Mode.View;
            bool hasSelection = !string.IsNullOrWhiteSpace(txt_id.Text);

            // Save belongs to add and edit only - this is the reported bug.
            btn_save.Enabled = editing;

            btn_new.Enabled = !editing;
            btn_edit.Visible = true;
            btn_edit.Enabled = !editing && hasSelection;
            btn_delete.Enabled = !editing && hasSelection;

            txt_ship_name.Enabled = true;
            txt_ship_name.ReadOnly = !editing;
            txt_id.Enabled = false;
        }

        private async void ShipTypes_Load(object sender, EventArgs e)
        {
            SetMode(Mode.View);
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
                var nameValue = dgv_shiptype_setup.Rows[e.RowIndex].Cells["ship_name"].Value;
                var idValue = dgv_shiptype_setup.Rows[e.RowIndex].Cells["id"].Value;

                if (nameValue != null)
                {
                    txt_id.Text = idValue.ToString();
                    txt_ship_name.Text = nameValue.ToString();

                    // Picking a row shows it; it does not start editing it. Edit and
                    // Delete become available because there is now a selection, and
                    // SetMode works that out from txt_id rather than being told twice.
                    SetMode(Mode.View);
                }
            }
        }

        private void btn_edit_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txt_id.Text)) return;

            SetMode(Mode.Edit);
            txt_ship_name.Focus();
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
                txt_id.Text = string.Empty;
                FetchData();

                // "Saving MUST persist and settle the form" (spec 2.1): back to
                // read-only, Save disabled again, New and Edit usable again. Without
                // this the form stayed in edit mode with cleared fields, so the next
                // Save posted a blank record.
                SetMode(Mode.View);
            }

            string message = response.Success
                ? (isNewRecord ? "Ship type saved successfully." : "Ship type updated successfully.")
                : (isNewRecord ? "Failed to save ship type.\n" + response.message : "Failed to update ship type.\n" + response.message);

            Helpers.ShowDialogMessage(response.Success ? "success" : "error", message);
        }


        private void btn_new_Click(object sender, EventArgs e)
        {
            // New starts genuinely blank (spec 2.1) - the id has to go too, or Save
            // would treat it as an update of whichever row was last selected.
            Helpers.ResetControls(pnl_input);
            txt_id.Text = string.Empty;
            dgv_shiptype_setup.ClearSelection();

            SetMode(Mode.Add);
            txt_ship_name.Focus();
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
                        txt_id.Text = string.Empty;
                        // "Ship type", not "Application" - copied from the Applications
                        // setup screen and never renamed.
                        Helpers.ShowDialogMessage("success", "Ship type deleted successfully!");
                        FetchData();

                        // The row it was pointing at no longer exists, so Edit and
                        // Delete must go back to unavailable.
                        SetMode(Mode.View);
                    }
                    else
                    {
                        Helpers.ShowDialogMessage("error", "Failed to delete the ship type");
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

        private void btn_close_Click(object sender, EventArgs e)
        {

        }
    }
}
