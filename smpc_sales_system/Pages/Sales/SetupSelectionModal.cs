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

        // Bug #058 (Trello, "No Search function in BPI modal"): panel_search has
        // existed empty in the Designer since this form was built - a search box
        // was clearly intended here but never actually added.
        private const string BaseRowFilter = "customer_code IS NOT NULL AND customer_code <> '' AND customer_code LIKE 'C#%'";
        private TextBox txt_search;
        private DataView filteredCustomer;

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

            txt_search = new TextBox
            {
                Dock = DockStyle.Top,
                ForeColor = Color.Gray,
                Text = "Search..."
            };
            txt_search.Enter += (s, ev) =>
            {
                if (txt_search.Text == "Search...")
                {
                    txt_search.Text = "";
                    txt_search.ForeColor = Color.Black;
                }
            };
            txt_search.Leave += (s, ev) =>
            {
                if (string.IsNullOrEmpty(txt_search.Text))
                {
                    txt_search.Text = "Search...";
                    txt_search.ForeColor = Color.Gray;
                }
            };
            txt_search.TextChanged += txt_search_TextChanged;
            panel_search.Controls.Add(txt_search);
        }

        private void SelectionModal_Load(object sender, EventArgs e)

        {
            // Front End Customer Filtering: to be refactored
            filteredCustomer = new DataView(this.Dt);
            filteredCustomer.RowFilter = BaseRowFilter;

            dg_general.DataSource = filteredCustomer;
            foreach (DataGridViewColumn column in dg_general.Columns)
            {
                if (column.Name != "cust_code" && column.Name != "cust_name")
                {
                    column.Visible = false;
                }
            }
        }

        private void txt_search_TextChanged(object sender, EventArgs e)
        {
            string search = txt_search.Text.Trim().Replace("'", "''");

            filteredCustomer.RowFilter = string.IsNullOrEmpty(search) || search == "Search..."
                ? BaseRowFilter
                : $"({BaseRowFilter}) AND (branch_name LIKE '%{search}%' OR customer_code LIKE '%{search}%')";
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
