namespace smpc_sales_app.Pages.Sales
{
    partial class SearchSalesReturn
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

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            this.pnl_top = new System.Windows.Forms.Panel();
            this.txt_search = new System.Windows.Forms.TextBox();
            this.lbl_setup_title = new System.Windows.Forms.Label();
            this.dgv_list = new System.Windows.Forms.DataGridView();
            this.pnl_top.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgv_list)).BeginInit();
            this.SuspendLayout();
            //
            // pnl_top
            //
            this.pnl_top.Controls.Add(this.txt_search);
            this.pnl_top.Controls.Add(this.lbl_setup_title);
            this.pnl_top.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnl_top.Location = new System.Drawing.Point(0, 0);
            this.pnl_top.Name = "pnl_top";
            this.pnl_top.Padding = new System.Windows.Forms.Padding(8);
            this.pnl_top.Size = new System.Drawing.Size(760, 52);
            this.pnl_top.TabIndex = 0;
            //
            // txt_search
            //
            this.txt_search.Location = new System.Drawing.Point(11, 27);
            this.txt_search.Name = "txt_search";
            this.txt_search.Size = new System.Drawing.Size(300, 20);
            this.txt_search.TabIndex = 1;
            this.txt_search.TextChanged += new System.EventHandler(this.txt_search_TextChanged);
            //
            // lbl_setup_title
            //
            this.lbl_setup_title.AutoSize = true;
            this.lbl_setup_title.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Bold);
            this.lbl_setup_title.Location = new System.Drawing.Point(8, 6);
            this.lbl_setup_title.Name = "lbl_setup_title";
            this.lbl_setup_title.Size = new System.Drawing.Size(65, 17);
            this.lbl_setup_title.TabIndex = 0;
            this.lbl_setup_title.Text = "Search";
            //
            // dgv_list
            //
            this.dgv_list.AllowUserToAddRows = false;
            this.dgv_list.AllowUserToDeleteRows = false;
            this.dgv_list.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgv_list.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgv_list.Location = new System.Drawing.Point(0, 52);
            this.dgv_list.MultiSelect = false;
            this.dgv_list.Name = "dgv_list";
            this.dgv_list.ReadOnly = true;
            this.dgv_list.RowHeadersVisible = false;
            this.dgv_list.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgv_list.Size = new System.Drawing.Size(760, 398);
            this.dgv_list.TabIndex = 1;
            this.dgv_list.CellDoubleClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.dgv_list_CellDoubleClick);
            //
            // SearchSalesReturn
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(760, 450);
            this.Controls.Add(this.dgv_list);
            this.Controls.Add(this.pnl_top);
            this.MinimizeBox = false;
            this.MaximizeBox = false;
            this.Name = "SearchSalesReturn";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Search";
            this.Load += new System.EventHandler(this.SearchSalesReturn_Load);
            this.pnl_top.ResumeLayout(false);
            this.pnl_top.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgv_list)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel pnl_top;
        private System.Windows.Forms.TextBox txt_search;
        private System.Windows.Forms.Label lbl_setup_title;
        private System.Windows.Forms.DataGridView dgv_list;
    }
}
