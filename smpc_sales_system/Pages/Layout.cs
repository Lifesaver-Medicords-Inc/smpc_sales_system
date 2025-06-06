using smpc_app.Services.Helpers;
using smpc_sales_app.Data;
using smpc_sales_app.Pages.Sales;
using smpc_sales_app.Services;
using smpc_sales_system.Pages.Sales;
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

            if (control is Opportunities)
            {
                Opportunities OpportunitiesControl = (Opportunities)control;
                OpportunitiesControl.TriggerNewForm += showForm;
            }

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
        }
        private void Sidebar_NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            
            if (e.Node.Name.Contains("Dashboard") || e.Node.Name.Contains("Sales Return") || e.Node.Name.Contains("Business Partners") || e.Node.Name.Contains("Application Setup") || e.Node.Name.Contains("Ship Type Setup"))
            {
                Helpers.ShowDialogMessage("error", "This module is not available at the moment!");
                return;
            }
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
                lbl_name.Text = CacheData.CurrentUser.first_name + " " + CacheData.CurrentUser.last_name;
                lbl_position.Text = CacheData.CurrentUser.position;
                lbl_department.Text = CacheData.CurrentUser.department;
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
