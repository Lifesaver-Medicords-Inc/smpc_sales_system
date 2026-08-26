using System;
using System.Collections.Generic;
using System.Windows.Forms;
using smpc_sales_system.Models;
using smpc_sales_system.Services.Setup;

namespace smpc_sales_app.Pages.Sales.Modal
{
    // §3.2/§6.3 REQUEST FOR ENGR. (Phase 4 item 4.1) - picks which engineer this
    // quotation is being sent to. Reuses EngineerService.GetEngineerList(), the
    // same Engineering-department-filtered source ItemSetUC's own
    // cmb_assign_engineer_user_id already uses for a different purpose (per-item
    // work assignment) - this is the quote-level "who does this land in front of"
    // grant instead.
    internal partial class RequestForEngrModal : Form
    {
        public uint SelectedEngrId { get; private set; }

        public RequestForEngrModal()
        {
            InitializeComponent();
        }

        private async void RequestForEngrModal_Load(object sender, EventArgs e)
        {
            try
            {
                var engineers = await EngineerService.GetEngineerList() ?? new List<EngineerModel>();
                cmb_engineer.DataSource = engineers;
                cmb_engineer.DisplayMember = nameof(EngineerModel.FullName);
                cmb_engineer.ValueMember = nameof(EngineerModel.Id);
                cmb_engineer.SelectedIndex = -1;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load engineers: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btn_send_Click(object sender, EventArgs e)
        {
            if (cmb_engineer.SelectedIndex < 0 || cmb_engineer.SelectedValue == null)
            {
                MessageBox.Show("Please select an engineer.", "Engineer Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            SelectedEngrId = Convert.ToUInt32(cmb_engineer.SelectedValue);
            DialogResult = DialogResult.OK;
            Close();
        }

        private void btn_cancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }
    }
}
