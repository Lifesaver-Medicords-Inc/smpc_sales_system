namespace smpc_sales_system.Pages.Sales
{
    partial class StockCheckModal
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
            this.lbl_title = new System.Windows.Forms.Label();
            this.dgv_projected_inventory = new System.Windows.Forms.DataGridView();
            this.col_item = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.col_stock = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.col_arrow = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.col_proj = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.col_reserve = new System.Windows.Forms.DataGridViewCheckBoxColumn();
            this.lbl_reserve_note = new System.Windows.Forms.Label();
            this.btn_ok = new System.Windows.Forms.Button();
            this.btn_close = new System.Windows.Forms.Button();
            this.btn_send_request = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.dgv_projected_inventory)).BeginInit();
            this.SuspendLayout();
            //
            // lbl_title
            //
            this.lbl_title.AutoSize = true;
            this.lbl_title.Font = new System.Drawing.Font("Microsoft Sans Serif", 10.5F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lbl_title.Location = new System.Drawing.Point(15, 15);
            this.lbl_title.Name = "lbl_title";
            this.lbl_title.Size = new System.Drawing.Size(200, 20);
            this.lbl_title.TabIndex = 0;
            this.lbl_title.Text = "PROJECTED INVENTORY";
            //
            // dgv_projected_inventory
            //
            this.dgv_projected_inventory.AllowUserToAddRows = false;
            this.dgv_projected_inventory.AllowUserToDeleteRows = false;
            this.dgv_projected_inventory.AllowUserToResizeRows = false;
            this.dgv_projected_inventory.RowHeadersVisible = false;
            this.dgv_projected_inventory.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgv_projected_inventory.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.col_item,
            this.col_stock,
            this.col_arrow,
            this.col_proj,
            this.col_reserve});
            this.dgv_projected_inventory.Location = new System.Drawing.Point(15, 45);
            this.dgv_projected_inventory.MultiSelect = false;
            this.dgv_projected_inventory.Name = "dgv_projected_inventory";
            this.dgv_projected_inventory.RowTemplate.Height = 26;
            this.dgv_projected_inventory.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgv_projected_inventory.Size = new System.Drawing.Size(430, 260);
            this.dgv_projected_inventory.TabIndex = 1;
            this.dgv_projected_inventory.CellFormatting += new System.Windows.Forms.DataGridViewCellFormattingEventHandler(this.dgv_projected_inventory_CellFormatting);
            this.dgv_projected_inventory.CurrentCellDirtyStateChanged += new System.EventHandler(this.dgv_projected_inventory_CurrentCellDirtyStateChanged);
            this.dgv_projected_inventory.CellValueChanged += new System.Windows.Forms.DataGridViewCellEventHandler(this.dgv_projected_inventory_CellValueChanged);
            //
            // col_item
            //
            this.col_item.HeaderText = "ITEM";
            this.col_item.Name = "col_item";
            this.col_item.ReadOnly = true;
            this.col_item.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.col_item.FillWeight = 55F;
            //
            // col_stock
            //
            this.col_stock.HeaderText = "STOCK";
            this.col_stock.Name = "col_stock";
            this.col_stock.ReadOnly = true;
            this.col_stock.Width = 60;
            this.col_stock.DefaultCellStyle.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
            //
            // col_arrow
            //
            this.col_arrow.HeaderText = "";
            this.col_arrow.Name = "col_arrow";
            this.col_arrow.ReadOnly = true;
            this.col_arrow.Width = 28;
            this.col_arrow.DefaultCellStyle.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
            //
            // col_proj
            //
            this.col_proj.HeaderText = "PROJ.";
            this.col_proj.Name = "col_proj";
            this.col_proj.ReadOnly = true;
            this.col_proj.Width = 60;
            this.col_proj.DefaultCellStyle.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
            //
            // col_reserve
            //
            this.col_reserve.HeaderText = "RESERVE";
            this.col_reserve.Name = "col_reserve";
            this.col_reserve.Width = 65;
            //
            // lbl_reserve_note
            //
            this.lbl_reserve_note.AutoSize = true;
            this.lbl_reserve_note.ForeColor = System.Drawing.Color.Gray;
            this.lbl_reserve_note.Font = new System.Drawing.Font("Microsoft Sans Serif", 7.5F);
            this.lbl_reserve_note.Location = new System.Drawing.Point(15, 312);
            this.lbl_reserve_note.MaximumSize = new System.Drawing.Size(430, 0);
            this.lbl_reserve_note.Name = "lbl_reserve_note";
            this.lbl_reserve_note.Size = new System.Drawing.Size(430, 15);
            this.lbl_reserve_note.TabIndex = 2;
            this.lbl_reserve_note.Text = "";
            //
            // btn_ok
            //
            this.btn_ok.Location = new System.Drawing.Point(235, 340);
            this.btn_ok.Name = "btn_ok";
            this.btn_ok.Size = new System.Drawing.Size(100, 28);
            this.btn_ok.TabIndex = 3;
            this.btn_ok.Text = "OK";
            this.btn_ok.UseVisualStyleBackColor = true;
            this.btn_ok.Click += new System.EventHandler(this.btn_ok_Click);
            //
            // btn_close
            //
            this.btn_close.Location = new System.Drawing.Point(345, 340);
            this.btn_close.Name = "btn_close";
            this.btn_close.Size = new System.Drawing.Size(100, 28);
            this.btn_close.TabIndex = 4;
            this.btn_close.Text = "CANCEL";
            this.btn_close.UseVisualStyleBackColor = true;
            this.btn_close.Click += new System.EventHandler(this.btn_close_Click);
            //
            // btn_send_request
            //
            this.btn_send_request.Location = new System.Drawing.Point(15, 340);
            this.btn_send_request.Name = "btn_send_request";
            this.btn_send_request.Size = new System.Drawing.Size(140, 28);
            this.btn_send_request.TabIndex = 5;
            this.btn_send_request.Text = "SEND REQUEST";
            this.btn_send_request.UseVisualStyleBackColor = true;
            this.btn_send_request.Click += new System.EventHandler(this.btn_send_request_Click);
            //
            // StockCheckModal
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(460, 385);
            this.Controls.Add(this.lbl_title);
            this.Controls.Add(this.dgv_projected_inventory);
            this.Controls.Add(this.lbl_reserve_note);
            this.Controls.Add(this.btn_ok);
            this.Controls.Add(this.btn_close);
            this.Controls.Add(this.btn_send_request);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "StockCheckModal";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Stock Check";
            this.Load += new System.EventHandler(this.StockCheckModal_Load);
            ((System.ComponentModel.ISupportInitialize)(this.dgv_projected_inventory)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label lbl_title;
        private System.Windows.Forms.DataGridView dgv_projected_inventory;
        private System.Windows.Forms.DataGridViewTextBoxColumn col_item;
        private System.Windows.Forms.DataGridViewTextBoxColumn col_stock;
        private System.Windows.Forms.DataGridViewTextBoxColumn col_arrow;
        private System.Windows.Forms.DataGridViewTextBoxColumn col_proj;
        private System.Windows.Forms.DataGridViewCheckBoxColumn col_reserve;
        private System.Windows.Forms.Label lbl_reserve_note;
        private System.Windows.Forms.Button btn_ok;
        private System.Windows.Forms.Button btn_close;
        private System.Windows.Forms.Button btn_send_request;
    }
}
