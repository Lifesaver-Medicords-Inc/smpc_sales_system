using smpc_app.Services.Helpers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace smpc_sales_system.Pages
{
    public partial class CRMModal : Form
    {
        private DataTable dt;
        private string branch;
        int result;
        //, DataTable crm
        public CRMModal(DataTable dgv, string branch)
        {
            InitializeComponent();
            this.dt = dgv;
            this.branch = branch;
        }
        private void fetchData()
        {
            txt_branch.Text = branch;
            DataView dataview = new DataView(dt);
            dataview.Sort = "date DESC";
            dgv_history.DataSource = dataview;

            foreach (DataGridViewColumn column in dgv_history.Columns)
            {
                if (column.Name != "remark" && column.Name != "date" && column.Name != "tag")
                {
                    column.Visible = false;
                }
            }
        }

        private void CRMModal_Load(object sender, EventArgs e)
        {
            fetchData();
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            string searchval = textBox1.Text.ToString();
            var data = Helpers.FilterDataTable(dt, searchval, "tag", "date", "remark");
            
            if (string.IsNullOrEmpty(searchval))
            {
                fetchData();
            }
            else
            {
                dgv_history.DataSource = data;
            }
        }
    }
}
