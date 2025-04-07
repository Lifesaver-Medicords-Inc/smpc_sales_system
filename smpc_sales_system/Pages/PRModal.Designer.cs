
namespace smpc_sales_app.Pages
{
    partial class PRModal
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
            this.pnl_title = new System.Windows.Forms.Panel();
            this.label2 = new System.Windows.Forms.Label();
            this.txt_search = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.pnl_dgv = new System.Windows.Forms.Panel();
            this.dgv_prlist = new System.Windows.Forms.DataGridView();
            this.id = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Column1 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.pr_id = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.doc_no = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.status = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.ds_item_list = new System.Data.DataSet();
            this.pnl_title.SuspendLayout();
            this.pnl_dgv.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgv_prlist)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.ds_item_list)).BeginInit();
            this.SuspendLayout();
            // 
            // pnl_title
            // 
            this.pnl_title.Controls.Add(this.label2);
            this.pnl_title.Controls.Add(this.txt_search);
            this.pnl_title.Controls.Add(this.label1);
            this.pnl_title.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnl_title.Location = new System.Drawing.Point(0, 0);
            this.pnl_title.Name = "pnl_title";
            this.pnl_title.Size = new System.Drawing.Size(537, 62);
            this.pnl_title.TabIndex = 0;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(365, 24);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(44, 13);
            this.label2.TabIndex = 5;
            this.label2.Text = "Search:";
            // 
            // txt_search
            // 
            this.txt_search.Location = new System.Drawing.Point(410, 20);
            this.txt_search.Name = "txt_search";
            this.txt_search.Size = new System.Drawing.Size(115, 20);
            this.txt_search.TabIndex = 4;
            this.txt_search.TextChanged += new System.EventHandler(this.txt_search_TextChanged);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 23);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(126, 13);
            this.label1.TabIndex = 0;
            this.label1.Text = "Purchase Requisition List";
            // 
            // pnl_dgv
            // 
            this.pnl_dgv.Controls.Add(this.dgv_prlist);
            this.pnl_dgv.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnl_dgv.Location = new System.Drawing.Point(0, 62);
            this.pnl_dgv.Name = "pnl_dgv";
            this.pnl_dgv.Size = new System.Drawing.Size(537, 382);
            this.pnl_dgv.TabIndex = 1;
            // 
            // dgv_prlist
            // 
            this.dgv_prlist.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgv_prlist.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.id,
            this.Column1,
            this.pr_id,
            this.doc_no,
            this.status});
            this.dgv_prlist.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgv_prlist.Location = new System.Drawing.Point(0, 0);
            this.dgv_prlist.MultiSelect = false;
            this.dgv_prlist.Name = "dgv_prlist";
            this.dgv_prlist.ReadOnly = true;
            this.dgv_prlist.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgv_prlist.Size = new System.Drawing.Size(537, 382);
            this.dgv_prlist.TabIndex = 0;
            this.dgv_prlist.CellClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.dgv_itemList_CellClick);
            // 
            // id
            // 
            this.id.DataPropertyName = "id";
            this.id.HeaderText = "id";
            this.id.Name = "id";
            this.id.ReadOnly = true;
            this.id.Visible = false;
            // 
            // Column1
            // 
            this.Column1.DataPropertyName = "item_name_id";
            this.Column1.HeaderText = "NAME ID";
            this.Column1.Name = "Column1";
            this.Column1.ReadOnly = true;
            this.Column1.Visible = false;
            // 
            // pr_id
            // 
            this.pr_id.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.pr_id.DataPropertyName = "pr_id";
            this.pr_id.HeaderText = "Purchase Requisition ID";
            this.pr_id.Name = "pr_id";
            this.pr_id.ReadOnly = true;
            // 
            // doc_no
            // 
            this.doc_no.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.doc_no.DataPropertyName = "doc_no";
            this.doc_no.HeaderText = "DOCUMENT NO";
            this.doc_no.Name = "doc_no";
            this.doc_no.ReadOnly = true;
            // 
            // status
            // 
            this.status.DataPropertyName = "status";
            this.status.HeaderText = "STATUS";
            this.status.Name = "status";
            this.status.ReadOnly = true;
            // 
            // ds_item_list
            // 
            this.ds_item_list.DataSetName = "ds_item_list";
            // 
            // PRModal
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(537, 450);
            this.Controls.Add(this.pnl_dgv);
            this.Controls.Add(this.pnl_title);
            this.Name = "PRModal";
            this.Text = "PRModal";
            this.Load += new System.EventHandler(this.ItemModal_Load);
            this.pnl_title.ResumeLayout(false);
            this.pnl_title.PerformLayout();
            this.pnl_dgv.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dgv_prlist)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.ds_item_list)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel pnl_title;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Panel pnl_dgv;
        private System.Windows.Forms.DataGridView dgv_prlist;
        private System.Data.DataSet ds_item_list;
        private System.Windows.Forms.DataGridViewTextBoxColumn id;
        private System.Windows.Forms.DataGridViewTextBoxColumn Column1;
        private System.Windows.Forms.DataGridViewTextBoxColumn pr_id;
        private System.Windows.Forms.DataGridViewTextBoxColumn doc_no;
        private System.Windows.Forms.DataGridViewTextBoxColumn status;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox txt_search;
    }
}