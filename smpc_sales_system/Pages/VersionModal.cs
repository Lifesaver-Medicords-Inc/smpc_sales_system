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
    public partial class VersionModal : Form
    {
        DataTable DT = new DataTable();

        Dictionary<string, string> result;
        string documentNum;
        public VersionModal(DataTable data, string doc_no)
        {
            InitializeComponent();
            this.DT = data;
            this.documentNum = doc_no;
            label1.Text = "Versions for " + "Q#" +doc_no;
        }

        private void dataGridView1_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                string version = dgv_version_modal.Rows[e.RowIndex].Cells["version_no"].Value.ToString();
                string doc = dgv_version_modal.Rows[e.RowIndex].Cells["document_no"].Value.ToString();

                foreach (DataGridViewColumn column in dgv_version_modal.Columns)
                {
                    Console.WriteLine(column.HeaderText);  
                }


                Dictionary<string, string> data = new Dictionary<string, string>()
                {
                    {"version_no", version},
                    {"document_no", doc }
                };

                this.result = data;

                this.DialogResult = DialogResult.OK;
                this.Close();
            }
        }
        public Dictionary<string, string> GetResult()
        {
            return result;
        }
       
        private void VersionModal_Load(object sender, EventArgs e)
        {
            var data = DT;

            if (data != null)
            {
                bindVersion(true);
            }
        }

        public DataTable versionList { get; set; } = new DataTable(); 
        private void bindVersion(bool isBind = false)
        {
            if (isBind)
            {
                DataView dataview = new DataView(DT);

                var documentNo = documentNum;

                var allVersionsForDocument = dataview.Cast<DataRowView>()
                     .Where(q => q["document_no"].ToString() == documentNo)
                     .OrderBy(q => Convert.ToInt32(q["version_no"]))
                     .ToList();


                dgv_version_modal.DataSource = allVersionsForDocument;

                foreach (DataGridViewColumn column in dgv_version_modal.Columns)
                {
                    if (column.Name != "v_no" && column.Name != "v_desc" && column.Name != "ver_status" && column.Name != "v_remark")
                    {
                        column.Visible = false;
                    }
                }
            }
        }

    }
}
