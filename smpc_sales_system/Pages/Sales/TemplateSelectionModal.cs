using smpc_app.Services.Helpers;
using smpc_sales_app.Services.Helpers;
using smpc_sales_system.Services.Setup;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace smpc_sales_system.Pages.Sales
{
    public partial class TemplateSelectionModal : Form
    {
        public TemplateSelectionModal()
        {
            InitializeComponent();
            fetchTemplates();
        }

        private async void fetchTemplates()
        {
            var data = await ProjectTemplatesService.GetProjectTemplates();
            var dt1 = JsonHelper.ToDataTable(data.SalesProjectTemplate);
            var dt2 = JsonHelper.ToDataTable(data.sales_project_template_child);

            dataGridView1.DataSource = dt1;

            foreach (DataGridViewColumn column in dataGridView1.Columns)
            {
                if (column.Name != "template_name")
                {
                    column.Visible = false;
                }
            }

        }

        private Dictionary<string, dynamic> result { get; set; }
        public Dictionary<string, dynamic> GetResult()
        {
            return result;
        }

        private void dataGridView1_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                string id = dataGridView1.Rows[e.RowIndex].Cells[0].Value.ToString();

                Dictionary<string, dynamic> data = new Dictionary<string, dynamic>()
                {
                    {"template_id", id }
                };

                this.result = data;
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
        }
    }
}
