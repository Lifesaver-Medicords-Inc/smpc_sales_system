using smpc_app.Services.Helpers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace smpc_sales_app.Pages.Sales
{
    public partial class Orders : UserControl
    {
        public Orders()
        {
            InitializeComponent();
        }
 

        private void Orders_Load(object sender, EventArgs e)
        {
            Helpers.LoadDirectory("D:\\LIFESAVER\\LIFESAVER\\TEST", treeview_sales);
        }

        private void treeview_sales_NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            if (e.Node.Tag != null)
            {
                // Check if the clicked node is a file (has a Tag property)
                string filePath = e.Node.Tag.ToString();

                if (File.Exists(filePath))
                {
                    try
                    {
                        // Open the file using the default associated application
                        Process.Start(filePath);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error opening file: {ex.Message}");
                    }
                }
            }
        }
    }
}
