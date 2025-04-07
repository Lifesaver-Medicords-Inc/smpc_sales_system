
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
            this.label2 = new System.Windows.Forms.Label();
            this.button2 = new System.Windows.Forms.Button();
            this.txt_specs = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.pnl_dgv = new System.Windows.Forms.Panel();
            this.dgv_itemList = new System.Windows.Forms.DataGridView();
            this.ds_item_list = new System.Data.DataSet();
            this.pnl_title.SuspendLayout();
            this.pnl_dgv.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgv_itemList)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.ds_item_list)).BeginInit();
            this.SuspendLayout();
            // 
            // pnl_title
            // 
            this.pnl_title.Controls.Add(this.label2);
            this.pnl_title.Controls.Add(this.button2);
            this.pnl_title.Controls.Add(this.txt_specs);
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
            this.label2.Location = new System.Drawing.Point(253, 23);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(44, 13);
            this.label2.TabIndex = 5;
            this.label2.Text = "Search:";
            // 
            // button2
            // 
            this.button2.Location = new System.Drawing.Point(450, 20);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(75, 23);
            this.button2.TabIndex = 4;
            this.button2.Text = "search";
            this.button2.UseVisualStyleBackColor = true;
            this.button2.Click += new System.EventHandler(this.button2_Click);
            // 
            // txt_specs
            // 
            this.txt_specs.Location = new System.Drawing.Point(303, 20);
            this.txt_specs.Name = "txt_specs";
            this.txt_specs.Size = new System.Drawing.Size(141, 20);
            this.txt_specs.TabIndex = 2;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 25);
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
        private System.Data.DataSet ds_item_list;
        private System.Windows.Forms.Button button2;
        private System.Windows.Forms.TextBox txt_specs;
        private System.Windows.Forms.Label label2;
    }
}