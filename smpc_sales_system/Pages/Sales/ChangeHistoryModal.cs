using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using smpc_sales_system.Models;
using smpc_sales_system.Pages.Sales;

namespace smpc_sales_app.Pages.Sales
{
    // Full, scrollable Change History for the whole project - not scoped to whichever tab
    // happens to be selected (that's what the inline flowLayoutPanelChangeHistory on the
    // Project Quotation screen shows a shorter version of). Opened via the "FULL DETAILS"
    // button next to that panel; entries are supplied by the caller (Quotation.RenderTabHistory
    // builds them off SalesProjectListData) rather than fetched here, so this modal has no
    // dependency on the API or the current form's state beyond what it's handed.
    public partial class ChangeHistoryModal : Form
    {
        public ChangeHistoryModal(string projectName, List<SalesProjectHistory> entries)
        {
            InitializeComponent();

            this.Text = string.IsNullOrWhiteSpace(projectName)
                ? "Change History - Full Details"
                : $"Change History - Full Details ({projectName})";

            entries = entries ?? new List<SalesProjectHistory>();

            foreach (var entry in entries.OrderByDescending(h => h.history_id))
            {
                var h = new UC_History();
                h.SetHistory(entry);

                foreach (Control ctrl in h.Controls)
                {
                    ctrl.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
                }

                flowLayoutPanelHistory.Controls.Add(h);
            }

            if (!entries.Any())
            {
                flowLayoutPanelHistory.Controls.Add(new Label
                {
                    Text = "No change history yet.",
                    AutoSize = true,
                    Margin = new Padding(8)
                });
            }
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
