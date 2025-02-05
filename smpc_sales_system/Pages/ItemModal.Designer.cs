
namespace smpc_sales_app.Pages
{
    partial class ItemModal
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
            this.label1 = new System.Windows.Forms.Label();
            this.pnl_dgv = new System.Windows.Forms.Panel();
            this.dgv_itemList = new System.Windows.Forms.DataGridView();
            this.id = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Column1 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.item_code = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.item_name = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.ds_item_list = new System.Data.DataSet();
            this.pnl_title.SuspendLayout();
            this.pnl_dgv.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgv_itemList)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.ds_item_list)).BeginInit();
            this.SuspendLayout();
            // 
            // pnl_title
            // 
            this.pnl_title.Controls.Add(this.label1);
            this.pnl_title.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnl_title.Location = new System.Drawing.Point(0, 0);
            this.pnl_title.Name = "pnl_title";
            this.pnl_title.Size = new System.Drawing.Size(537, 62);
            this.pnl_title.TabIndex = 0;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 23);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(46, 13);
            this.label1.TabIndex = 0;
            this.label1.Text = "Item List";
            // 
            // pnl_dgv
            // 
            this.pnl_dgv.Controls.Add(this.dgv_itemList);
            this.pnl_dgv.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnl_dgv.Location = new System.Drawing.Point(0, 62);
            this.pnl_dgv.Name = "pnl_dgv";
            this.pnl_dgv.Size = new System.Drawing.Size(537, 382);
            this.pnl_dgv.TabIndex = 1;
            // 
            // dgv_itemList
            // 
            this.dgv_itemList.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgv_itemList.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.id,
            this.Column1,
            this.item_code,
            this.item_name});
            this.dgv_itemList.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgv_itemList.Location = new System.Drawing.Point(0, 0);
            this.dgv_itemList.MultiSelect = false;
            this.dgv_itemList.Name = "dgv_itemList";
            this.dgv_itemList.ReadOnly = true;
            this.dgv_itemList.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgv_itemList.Size = new System.Drawing.Size(537, 382);
            this.dgv_itemList.TabIndex = 0;
            this.dgv_itemList.CellClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.dgv_itemList_CellClick);
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
            // item_code
            // 
            this.item_code.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.item_code.DataPropertyName = "item_code";
            this.item_code.HeaderText = "ITEM CODE";
            this.item_code.Name = "item_code";
            this.item_code.ReadOnly = true;
            // 
            // item_name
            // 
            this.item_name.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.item_name.DataPropertyName = "item_name";
            this.item_name.HeaderText = "ITEM NAME";
            this.item_name.Name = "item_name";
            this.item_name.ReadOnly = true;
            // 
            // ds_item_list
            // 
            this.ds_item_list.DataSetName = "ds_item_list";
            // 
            // ItemModal
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(537, 450);
            this.Controls.Add(this.pnl_dgv);
            this.Controls.Add(this.pnl_title);
            this.Name = "ItemModal";
            this.Text = "ItemModal";
            this.Load += new System.EventHandler(this.ItemModal_Load);
            this.pnl_title.ResumeLayout(false);
            this.pnl_title.PerformLayout();
            this.pnl_dgv.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dgv_itemList)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.ds_item_list)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel pnl_title;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Panel pnl_dgv;
        private System.Windows.Forms.DataGridView dgv_itemList;
        private System.Windows.Forms.DataGridViewTextBoxColumn id;
        private System.Windows.Forms.DataGridViewTextBoxColumn Column1;
        private System.Windows.Forms.DataGridViewTextBoxColumn item_code;
        private System.Windows.Forms.DataGridViewTextBoxColumn item_name;
        private System.Data.DataSet ds_item_list;
    }
}