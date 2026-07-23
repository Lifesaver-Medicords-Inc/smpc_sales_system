using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using smpc_sales_system.Models;

namespace smpc_sales_system.Pages.Sales
{
    public partial class UC_History : UserControl
    {
        public UC_History()
        {
            InitializeComponent();
        }

        // Every label here used to be permanently hardcoded in the designer (fixed date,
        // "Jerome", "Not yet working", etc.) - this control was a static mockup that was
        // never actually wired to real data. This is what makes an instance show a real
        // SalesProjectHistory row instead.
        public void SetHistory(SalesProjectHistory entry)
        {
            label1.Text = entry.date;
            label2.Text = entry.time;
            label3.Text = entry.user;
            label4.Text = entry.old_data;
            label6.Text = entry.new_data;
            // label7 ("Not yet working") had no corresponding data field - it was a leftover
            // dev placeholder, not real UI. Left blank since there's nothing to show there.
            label7.Text = string.Empty;
        }

        private void label5_Click(object sender, EventArgs e)
        {

        }
    }
}
