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

       
        DataTable applicationData { get; set; }  = new DataTable();
        private async void FetchData()
        {
            var data = await ApplicationService.GetAsDatatable();
            applicationData = data;
            dgv_application_setup.DataSource = data;
        }

        // The three states this form can be in (spec 2.1: a form opens read-only and an
        // Edit button enters edit mode; saving returns it to read-only). Deliberately
        // identical to ShipTypeSetup's - the two are sibling setup screens and had
        // drifted into behaving differently.
        private enum Mode { View, Add, Edit }

        private Mode _mode = Mode.View;

        // One place decides what every control does, because the old version spread it
        // across four handlers and each leaked:
        //
        //   - btn_new disabled itself on click and nothing re-enabled it, so New worked
        //     exactly once per visit to the page.
        //   - Saving cleared the fields but left Save showing and New dead, so the form
        //     never returned to view mode.
        //   - btn_edit made txt_id editable, which it must never be - it is the key the
        //     save path uses to decide insert vs update.
        //
        // Save is DISABLED rather than hidden in view mode. It used to vanish off the
        // strip entirely; greying it keeps the action strip a fixed shape across modes
        // and matches its sibling screen.
        private void SetMode(Mode mode)
        {
            _mode = mode;

            bool editing = mode != Mode.View;
            bool hasSelection = !string.IsNullOrWhiteSpace(txt_id.Text);

            btn_save.Visible = true;
            btn_save.Enabled = editing;

            btn_new.Enabled = !editing;
            btn_edit.Enabled = !editing && hasSelection;
            btn_delete.Enabled = !editing && hasSelection;

            txt_code.Enabled = true;
            txt_name.Enabled = true;
            txt_code.ReadOnly = !editing;
            txt_name.ReadOnly = !editing;

            // Never editable: it decides insert vs update on save.
            txt_id.Enabled = false;
            txt_id.ReadOnly = true;
        }

        private async void Applications_Load(object sender, EventArgs e)
        {
            SetMode(Mode.View);
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
                Panel[] pnl_list = { pnl_input };
                Helpers.BindControls(pnl_list, applicationData, e.RowIndex);

                // Edit and Delete become available because there is now a selection,
                // which SetMode works out from txt_id rather than being told twice.
                SetMode(Mode.View);
            }
        }

        private void btn_edit_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txt_id.Text)) return;

            SetMode(Mode.Edit);
            txt_name.Focus();
        }

        // SAVE (either new data or updating the data)
        private async void btn_save_Click(object sender, EventArgs e)
        {
            var data = Helpers.GetControlsValues(pnl_input);
            ApiResponseModel response = new ApiResponseModel();

            string errorMessage =
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
                ? await ApplicationService.Insert(data)
                : await ApplicationService.Update(data);

            if (response.Success)
            {
                Helpers.ResetControls(pnl_input);
                txt_id.Text = string.Empty;
                FetchData();

                // "Saving MUST persist and settle the form" (spec 2.1): back to
                // read-only, Save disabled, New and Edit usable again. Without this
                // the form stayed in edit mode with cleared fields, so the next Save
                // posted a blank record.
                SetMode(Mode.View);
            }

            string message = response.Success
                ? (isNewRecord ? "Application saved successfully." : "Application updated successfully.")
                : (isNewRecord ? "Failed to save application.\n" + response.message : "Failed to update application.\n" + response.message);

            Helpers.ShowDialogMessage(response.Success ? "success" : "error", message);
        }

        // ADD NEW 
        private void btn_new_Click(object sender, EventArgs e)
        {
            // New starts genuinely blank (spec 2.1) - the id has to go too, or Save
            // would treat it as an update of whichever row was last selected.
            Helpers.ResetControls(pnl_input);
            txt_id.Text = string.Empty;
            dgv_application_setup.ClearSelection();

            SetMode(Mode.Add);
            txt_name.Focus();
        }

        // DELETE
        private async void btn_delete_Click(object sender, EventArgs e)
        {
            DialogResult result = MessageBox.Show("Are you sure you want to delete this item?",
               "Confirm Deletion", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                var id = txt_id.Text;
                int appId;
                if (int.TryParse(id, out appId))
                {
                    var data = new Dictionary<string, dynamic>
                    {
                         { "id", appId }
                    };
                    bool isSuccess = await ApplicationService.Delete(data);
                    if (isSuccess)
                    {
                        Helpers.ResetControls(pnl_input);
                        txt_id.Text = string.Empty;
                        Helpers.ShowDialogMessage("success", "Application deleted successfully!");
                        FetchData();

                        // The row it was pointing at no longer exists, so Edit and
                        // Delete go back to unavailable.
                        SetMode(Mode.View);
                    }
                    else
                    {
                        Helpers.ShowDialogMessage("error", "Failed to delete the application");
                    }
                }
            }
        }
    }
}
