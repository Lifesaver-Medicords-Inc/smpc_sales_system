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
    public partial class ModelSelection : Form
    {
        DataTable dt = new DataTable();
        DataTable dt2 = new DataTable();

        


        public ModelSelection(DataTable DT, DataTable DT2)
        {
            InitializeComponent();
            this.dt = DT;
            this.dt2 = DT2;

          
        }
      
        private void tableLayoutPanel1_Paint(object sender, PaintEventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
            foreach (DataRow row1 in dt.Rows)
            {
                // Create a new row in the layout for each item in dt
                model_layout.RowCount++;
                model_layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                int newRow = model_layout.RowCount - 1;

                // Get the item name from dt (first table)
                string name = row1["item_name"].ToString();

                // Add a Label for the item name
                model_layout.Controls.Add(new Label { Text = name }, 0, newRow);

                // Create the ComboBox for item specs
                ComboBox comboBox = new ComboBox();

                // Assuming dt2 contains the item specs in a "template" column
                foreach (DataRow row2 in dt2.Rows)
                {
                    string specs = row2["template"].ToString();

                    if (!string.IsNullOrEmpty(specs))
                    {
                        // Split specs into a list and add them to the ComboBox
                        var specsList = specs.Split(';');
                        comboBox.Items.AddRange(specsList);
                    }
                }

                // Optionally set the default selection if there are items
                if (comboBox.Items.Count > 0)
                    comboBox.SelectedIndex = 0;

                // Add the ComboBox to the layout in column 1
                model_layout.Controls.Add(comboBox, 1, newRow);
            }
        }

    }
}
