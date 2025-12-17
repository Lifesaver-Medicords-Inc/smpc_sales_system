
namespace smpc_sales_system.Pages.Sales
{
    partial class PumpItemModal
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
            this.dgv_itemList = new System.Windows.Forms.DataGridView();
            this.pnl_title = new System.Windows.Forms.Panel();
            this.btn_search = new System.Windows.Forms.Button();
            this.txt_specs = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.dgv_itemList)).BeginInit();
            this.pnl_title.SuspendLayout();
            this.SuspendLayout();
            // 
            // dgv_itemList
            // 
            this.dgv_itemList.AllowUserToAddRows = false;
            this.dgv_itemList.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgv_itemList.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgv_itemList.Location = new System.Drawing.Point(0, 62);
            this.dgv_itemList.MultiSelect = false;
            this.dgv_itemList.Name = "dgv_itemList";
            this.dgv_itemList.ReadOnly = true;
            this.dgv_itemList.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgv_itemList.Size = new System.Drawing.Size(535, 427);
            this.dgv_itemList.TabIndex = 1;
            this.dgv_itemList.CellClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.dgv_itemList_CellClick);
            // 
            // pnl_title
            // 
            this.pnl_title.Controls.Add(this.btn_search);
            this.pnl_title.Controls.Add(this.txt_specs);
            this.pnl_title.Controls.Add(this.label1);
            this.pnl_title.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnl_title.Location = new System.Drawing.Point(0, 0);
            this.pnl_title.Name = "pnl_title";
            this.pnl_title.Size = new System.Drawing.Size(535, 62);
            this.pnl_title.TabIndex = 2;
            // 
            // btn_search
            // 
            this.btn_search.Location = new System.Drawing.Point(450, 20);
            this.btn_search.Name = "btn_search";
            this.btn_search.Size = new System.Drawing.Size(75, 23);
            this.btn_search.TabIndex = 4;
            this.btn_search.Text = "SEARCH";
            this.btn_search.UseVisualStyleBackColor = true;
            // 
            // txt_specs
            // 
            this.txt_specs.Location = new System.Drawing.Point(307, 22);
            this.txt_specs.Name = "txt_specs";
            this.txt_specs.Size = new System.Drawing.Size(141, 20);
            this.txt_specs.TabIndex = 2;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(12, 25);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(86, 16);
            this.label1.TabIndex = 0;
            this.label1.Text = "PUMP LIST";
            // 
            // PumpItemModal
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(535, 489);
            this.Controls.Add(this.dgv_itemList);
            this.Controls.Add(this.pnl_title);
            this.Name = "PumpItemModal";
            this.Text = "PumpItemModal";
            this.Load += new System.EventHandler(this.PumpItemModal_Load);
            ((System.ComponentModel.ISupportInitialize)(this.dgv_itemList)).EndInit();
            this.pnl_title.ResumeLayout(false);
            this.pnl_title.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.DataGridView dgv_itemList;
        private System.Windows.Forms.Panel pnl_title;
        private System.Windows.Forms.Button btn_search;
        private System.Windows.Forms.TextBox txt_specs;
        private System.Windows.Forms.Label label1;
    }
}