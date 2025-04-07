using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace smpc_inventory_app.Pages
{
    public partial class SetupSelectionModal : Form
    {
        private string Title { get; }
        private string EndPoint { get;}
        private List<int> CurrentValues { get; }
        private List<string> CurrentGridValues { get; }
        private Dictionary<string, string> result { get; set; }
        //private DataView result { get; set; }
        private DataTable Dt { get; set; }
        public SetupSelectionModal(string title, string api, DataTable dt, List<int> currentValues, List<string> currentGridValues, int recordIndex=0)
        {
            InitializeComponent();
       
            lbl_title.Text = title;
            this.Text = title;
            this.EndPoint = api;
            this.CurrentValues = currentValues;
            this.CurrentGridValues = (currentGridValues != null && recordIndex >= 0 && recordIndex < currentGridValues.Count && !string.IsNullOrEmpty(currentGridValues[recordIndex]))
                   ? new List<string>(currentGridValues[recordIndex].Split(','))
                   : new List<string>();
            this.Dt = dt;
        }

        private void SelectionModal_Load(object sender, EventArgs e)

        {

            dg_general.DataSource = this.Dt;
            foreach (DataGridViewColumn column in dg_general.Columns)
            {
                if (column.Name != "cust_code" && column.Name != "cust_name")
                {
                    column.Visible = false;
                }
            }
        }

        public Dictionary<string, string> GetResult()
        {
            return result;
        }


        private void dg_general_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                string base_id = dg_general.Rows[e.RowIndex].Cells[0].Value.ToString();

                Dictionary<string, string> data = new Dictionary<string, string>()
                {
                    { "id", base_id}
                };

                this.result = data;
                this.DialogResult = DialogResult.OK;
                this.Close();
            };
            }
        }
}
