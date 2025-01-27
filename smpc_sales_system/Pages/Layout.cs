using smpc_sales_app.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace smpc_sales_app.Pages
{
    public partial class Layout : Form
    {

        private int tabCount = 0;
        public Layout()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void showForm(string tabTitle, Control control)
        {
            tabCount++;
            Button closeButton = new Button();
            closeButton.Text = "X";
            closeButton.Size = new Size(20, 20);
            closeButton.Click += removeTab;
            closeButton.ForeColor = Color.Red;

            TabPage newTab = new TabPage(tabTitle);
            newTab.Controls.Add(closeButton);
            closeButton.Location = new Point(newTab.Width, 10); // Adjust position as needed

            //control.Width = this.Width - 235; 
            tabContainer.Height = this.Height * 2;
            //control.Height = this.Height;
            control.Width = this.Width - 550;
            newTab.Controls.Add(control);
            newTab.AutoScroll = true;
            tabContainer.TabPages.Add(newTab);
            tabContainer.SelectTab(newTab);
        }
        private void removeTab(object sender, EventArgs e)
        {
            tabContainer.TabPages.Remove(tabContainer.SelectedTab);
            //tabControl1.SelectTab();

        }

        private void Sidebar_NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            if (!e.Node.Name.Contains("parent"))
            {
                RoutesServices route = new RoutesServices(e.Node.Name);
                showForm(route.GetTitle(), route.GetForm());
            }
        }

        private void Layout_Load(object sender, EventArgs e)
        {
            Login login = new Login();
            if (DialogResult.OK == login.ShowDialog())
            {
                this.Enabled = true;
            }
            else
            {
                Application.Exit();
            }
        }

        private void Sidebar_AfterSelect(object sender, TreeViewEventArgs e)
        {

        }
    }
}
