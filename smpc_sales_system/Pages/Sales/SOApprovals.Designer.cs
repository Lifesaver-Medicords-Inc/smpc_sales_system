namespace smpc_sales_system.Pages.Sales
{
    partial class SOApprovals
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.toolStrip1 = new System.Windows.Forms.ToolStrip();
            this.btn_refresh = new System.Windows.Forms.ToolStripButton();
            this.btn_approve = new System.Windows.Forms.ToolStripButton();
            this.btn_open = new System.Windows.Forms.ToolStripButton();
            this.btn_print = new System.Windows.Forms.ToolStripButton();
            this.sep1 = new System.Windows.Forms.ToolStripSeparator();
            this.lbl_search = new System.Windows.Forms.ToolStripLabel();
            this.txt_search = new System.Windows.Forms.ToolStripTextBox();
            this.lbl_title = new System.Windows.Forms.Label();
            this.tabs = new System.Windows.Forms.TabControl();
            this.tab_pending = new System.Windows.Forms.TabPage();
            this.tab_ongoing = new System.Windows.Forms.TabPage();
            this.tab_finished = new System.Windows.Forms.TabPage();
            this.dgv_orders = new System.Windows.Forms.DataGridView();
            this.lbl_loading = new System.Windows.Forms.Label();
            this.lbl_count = new System.Windows.Forms.Label();
            this.toolStrip1.SuspendLayout();
            this.tabs.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgv_orders)).BeginInit();
            this.SuspendLayout();
            //
            // toolStrip1
            //
            this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.btn_refresh,
            this.btn_approve,
            this.btn_open,
            this.btn_print,
            this.sep1,
            this.lbl_search,
            this.txt_search});
            this.toolStrip1.Location = new System.Drawing.Point(0, 0);
            this.toolStrip1.Name = "toolStrip1";
            this.toolStrip1.Size = new System.Drawing.Size(1100, 25);
            this.toolStrip1.TabIndex = 0;
            //
            this.btn_refresh.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.btn_refresh.Name = "btn_refresh";
            this.btn_refresh.Size = new System.Drawing.Size(52, 22);
            this.btn_refresh.Text = "Refresh";
            this.btn_approve.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.btn_approve.Name = "btn_approve";
            this.btn_approve.Size = new System.Drawing.Size(58, 22);
            this.btn_approve.Text = "Approve";
            this.btn_open.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.btn_open.Name = "btn_open";
            this.btn_open.Size = new System.Drawing.Size(70, 22);
            this.btn_open.Text = "Open Order";
            this.btn_print.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.btn_print.Name = "btn_print";
            this.btn_print.Size = new System.Drawing.Size(40, 22);
            this.btn_print.Text = "Print";
            this.sep1.Name = "sep1";
            this.sep1.Size = new System.Drawing.Size(6, 25);
            this.lbl_search.Name = "lbl_search";
            this.lbl_search.Size = new System.Drawing.Size(45, 22);
            this.lbl_search.Text = "Search:";
            this.txt_search.Name = "txt_search";
            this.txt_search.Size = new System.Drawing.Size(220, 25);
            //
            // lbl_title
            //
            this.lbl_title.Dock = System.Windows.Forms.DockStyle.Top;
            this.lbl_title.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold);
            this.lbl_title.Location = new System.Drawing.Point(0, 25);
            this.lbl_title.Name = "lbl_title";
            this.lbl_title.Padding = new System.Windows.Forms.Padding(8, 6, 0, 4);
            this.lbl_title.Size = new System.Drawing.Size(1100, 34);
            this.lbl_title.TabIndex = 1;
            this.lbl_title.Text = "SO Approval List";
            //
            // lbl_loading
            //
            this.lbl_loading.Dock = System.Windows.Forms.DockStyle.Top;
            this.lbl_loading.Location = new System.Drawing.Point(0, 59);
            this.lbl_loading.Name = "lbl_loading";
            this.lbl_loading.Padding = new System.Windows.Forms.Padding(8, 2, 0, 2);
            this.lbl_loading.Size = new System.Drawing.Size(1100, 20);
            this.lbl_loading.TabIndex = 2;
            this.lbl_loading.Text = "Loading... Please wait";
            //
            // tabs
            //
            this.tabs.Controls.Add(this.tab_pending);
            this.tabs.Controls.Add(this.tab_ongoing);
            this.tabs.Controls.Add(this.tab_finished);
            this.tabs.Dock = System.Windows.Forms.DockStyle.Top;
            this.tabs.Location = new System.Drawing.Point(0, 79);
            this.tabs.Name = "tabs";
            this.tabs.SelectedIndex = 0;
            this.tabs.Size = new System.Drawing.Size(1100, 24);
            this.tabs.TabIndex = 3;
            //
            // Spec 7.2: the SO Approval List's own status vocabulary. It is deliberately
            // different from the sales SO List's, and must not be harmonised with it.
            //
            this.tab_pending.Name = "tab_pending";
            this.tab_pending.Text = "PENDING";
            this.tab_ongoing.Name = "tab_ongoing";
            this.tab_ongoing.Text = "ONGOING";
            this.tab_finished.Name = "tab_finished";
            this.tab_finished.Text = "FINISHED";
            //
            // dgv_orders
            //
            this.dgv_orders.AllowUserToAddRows = false;
            this.dgv_orders.AllowUserToDeleteRows = false;
            this.dgv_orders.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.dgv_orders.BackgroundColor = System.Drawing.Color.White;
            this.dgv_orders.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgv_orders.Location = new System.Drawing.Point(0, 103);
            this.dgv_orders.MultiSelect = false;
            this.dgv_orders.Name = "dgv_orders";
            this.dgv_orders.ReadOnly = true;
            this.dgv_orders.RowHeadersVisible = false;
            this.dgv_orders.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgv_orders.Size = new System.Drawing.Size(1100, 475);
            this.dgv_orders.TabIndex = 4;
            //
            // lbl_count
            //
            this.lbl_count.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.lbl_count.Location = new System.Drawing.Point(0, 578);
            this.lbl_count.Name = "lbl_count";
            this.lbl_count.Padding = new System.Windows.Forms.Padding(8, 4, 0, 0);
            this.lbl_count.Size = new System.Drawing.Size(1100, 22);
            this.lbl_count.TabIndex = 5;
            //
            // SOApprovals
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.dgv_orders);
            this.Controls.Add(this.lbl_count);
            this.Controls.Add(this.tabs);
            this.Controls.Add(this.lbl_loading);
            this.Controls.Add(this.lbl_title);
            this.Controls.Add(this.toolStrip1);
            this.Name = "SOApprovals";
            this.Size = new System.Drawing.Size(1100, 600);
            this.toolStrip1.ResumeLayout(false);
            this.toolStrip1.PerformLayout();
            this.tabs.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dgv_orders)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.ToolStripButton btn_refresh;
        private System.Windows.Forms.ToolStripButton btn_approve;
        private System.Windows.Forms.ToolStripButton btn_open;
        private System.Windows.Forms.ToolStripButton btn_print;
        private System.Windows.Forms.ToolStripSeparator sep1;
        private System.Windows.Forms.ToolStripLabel lbl_search;
        private System.Windows.Forms.ToolStripTextBox txt_search;
        private System.Windows.Forms.Label lbl_title;
        private System.Windows.Forms.Label lbl_loading;
        private System.Windows.Forms.TabControl tabs;
        private System.Windows.Forms.TabPage tab_pending;
        private System.Windows.Forms.TabPage tab_ongoing;
        private System.Windows.Forms.TabPage tab_finished;
        private System.Windows.Forms.DataGridView dgv_orders;
        private System.Windows.Forms.Label lbl_count;
    }
}
