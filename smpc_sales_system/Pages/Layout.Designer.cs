
namespace smpc_sales_app.Pages
{
    partial class Layout
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.Windows.Forms.TreeNode treeNode1 = new System.Windows.Forms.TreeNode("Dashboard");
            System.Windows.Forms.TreeNode treeNode2 = new System.Windows.Forms.TreeNode("Sales Quotation");
            System.Windows.Forms.TreeNode treeNode3 = new System.Windows.Forms.TreeNode("Sales Order");
            System.Windows.Forms.TreeNode treeNode4 = new System.Windows.Forms.TreeNode("Sales Return");
            System.Windows.Forms.TreeNode treeNode5 = new System.Windows.Forms.TreeNode("Business Partners");
            System.Windows.Forms.TreeNode treeNode6 = new System.Windows.Forms.TreeNode("Application Setup");
            System.Windows.Forms.TreeNode treeNode7 = new System.Windows.Forms.TreeNode("Ship Type Setup");
            System.Windows.Forms.TreeNode treeNode8 = new System.Windows.Forms.TreeNode("Opportunities");
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Layout));
            this.tabContainer = new System.Windows.Forms.TabControl();
            this.panel3 = new System.Windows.Forms.Panel();
            this.Sidebar = new System.Windows.Forms.TreeView();
            this.panel4 = new System.Windows.Forms.Panel();
            this.panel5 = new System.Windows.Forms.Panel();
            this.label1 = new System.Windows.Forms.Label();
            this.panel6 = new System.Windows.Forms.Panel();
            this.panel3.SuspendLayout();
            this.panel4.SuspendLayout();
            this.panel5.SuspendLayout();
            this.SuspendLayout();
            // 
            // tabContainer
            // 
            this.tabContainer.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabContainer.Location = new System.Drawing.Point(0, 0);
            this.tabContainer.Name = "tabContainer";
            this.tabContainer.SelectedIndex = 0;
            this.tabContainer.Size = new System.Drawing.Size(406, 450);
            this.tabContainer.SizeMode = System.Windows.Forms.TabSizeMode.Fixed;
            this.tabContainer.TabIndex = 0;
            // 
            // panel3
            // 
            this.panel3.Controls.Add(this.Sidebar);
            this.panel3.Dock = System.Windows.Forms.DockStyle.Left;
            this.panel3.Location = new System.Drawing.Point(0, 0);
            this.panel3.Name = "panel3";
            this.panel3.Size = new System.Drawing.Size(200, 450);
            this.panel3.TabIndex = 2;
            // 
            // Sidebar
            // 
            this.Sidebar.Dock = System.Windows.Forms.DockStyle.Fill;
            this.Sidebar.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F);
            this.Sidebar.Location = new System.Drawing.Point(0, 0);
            this.Sidebar.Name = "Sidebar";
            treeNode1.Name = "Dashboard";
            treeNode1.Text = "Dashboard";
            treeNode2.Name = "Sales Quotation";
            treeNode2.Text = "Sales Quotation";
            treeNode3.Name = "Sales Order";
            treeNode3.Text = "Sales Order";
            treeNode4.Name = "SalesReturn";
            treeNode4.Text = "Sales Return";
            treeNode5.Name = "BusinessPartners";
            treeNode5.Text = "Business Partners";
            treeNode6.Name = "Application Setup";
            treeNode6.Text = "Application Setup";
            treeNode7.Name = "Ship Type Setup";
            treeNode7.Text = "Ship Type Setup";
            treeNode8.Name = "Opportunities";
            treeNode8.Text = "Opportunities";
            this.Sidebar.Nodes.AddRange(new System.Windows.Forms.TreeNode[] {
            treeNode1,
            treeNode2,
            treeNode3,
            treeNode4,
            treeNode5,
            treeNode6,
            treeNode7,
            treeNode8});
            this.Sidebar.Size = new System.Drawing.Size(200, 450);
            this.Sidebar.TabIndex = 0;
            this.Sidebar.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.Sidebar_AfterSelect);
            this.Sidebar.NodeMouseClick += new System.Windows.Forms.TreeNodeMouseClickEventHandler(this.Sidebar_NodeMouseClick);
            // 
            // panel4
            // 
            this.panel4.Controls.Add(this.tabContainer);
            this.panel4.Controls.Add(this.panel5);
            this.panel4.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel4.Location = new System.Drawing.Point(200, 0);
            this.panel4.Name = "panel4";
            this.panel4.Size = new System.Drawing.Size(719, 450);
            this.panel4.TabIndex = 3;
            // 
            // panel5
            // 
            this.panel5.BackColor = System.Drawing.Color.LightCoral;
            this.panel5.Controls.Add(this.label1);
            this.panel5.Controls.Add(this.panel6);
            this.panel5.Dock = System.Windows.Forms.DockStyle.Right;
            this.panel5.Location = new System.Drawing.Point(406, 0);
            this.panel5.Name = "panel5";
            this.panel5.Size = new System.Drawing.Size(313, 450);
            this.panel5.TabIndex = 1;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 15.75F, System.Drawing.FontStyle.Bold);
            this.label1.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.label1.Location = new System.Drawing.Point(108, 9);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(113, 25);
            this.label1.TabIndex = 1;
            this.label1.Text = "RED BOX";
            // 
            // panel6
            // 
            this.panel6.BackColor = System.Drawing.Color.DarkRed;
            this.panel6.Location = new System.Drawing.Point(1, 39);
            this.panel6.Name = "panel6";
            this.panel6.Size = new System.Drawing.Size(312, 1);
            this.panel6.TabIndex = 0;
            // 
            // Layout
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(919, 450);
            this.Controls.Add(this.panel4);
            this.Controls.Add(this.panel3);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "Layout";
            this.Text = "SMPC -SALES SYSTEM";
            this.WindowState = System.Windows.Forms.FormWindowState.Maximized;
            this.Load += new System.EventHandler(this.Layout_Load);
            this.panel3.ResumeLayout(false);
            this.panel4.ResumeLayout(false);
            this.panel5.ResumeLayout(false);
            this.panel5.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TabControl tabContainer;
        private System.Windows.Forms.Panel panel3;
        private System.Windows.Forms.TreeView Sidebar;
        private System.Windows.Forms.Panel panel4;
        private System.Windows.Forms.Panel panel5;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Panel panel6;
    }
}