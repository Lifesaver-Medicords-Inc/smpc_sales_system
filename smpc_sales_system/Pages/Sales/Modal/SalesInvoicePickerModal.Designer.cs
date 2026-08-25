namespace smpc_sales_app.Pages.Sales.Modal
{
    partial class SalesInvoicePickerModal
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
            this.lbl_search = new System.Windows.Forms.Label();
            this.pnl_bottom = new System.Windows.Forms.Panel();
            this.btn_select = new System.Windows.Forms.Button();
            this.btn_cancel = new System.Windows.Forms.Button();
            this.dgv_invoices = new System.Windows.Forms.DataGridView();
            this.col_id = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.col_doc_no = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.col_customer = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.col_doc_date = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.col_so = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.pnl_top.SuspendLayout();
            this.pnl_bottom.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgv_invoices)).BeginInit();
            this.SuspendLayout();
            //
            // pnl_top
            //
            this.pnl_top.Controls.Add(this.txt_search);
            this.pnl_top.Controls.Add(this.lbl_search);
            this.pnl_top.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnl_top.Location = new System.Drawing.Point(0, 0);
            this.pnl_top.Name = "pnl_top";
            this.pnl_top.Padding = new System.Windows.Forms.Padding(8);
            this.pnl_top.Size = new System.Drawing.Size(700, 40);
            this.pnl_top.TabIndex = 0;
            //
            // txt_search
            //
            this.txt_search.Location = new System.Drawing.Point(60, 10);
            this.txt_search.Name = "txt_search";
            this.txt_search.Size = new System.Drawing.Size(300, 20);
            this.txt_search.TabIndex = 1;
            this.txt_search.TextChanged += new System.EventHandler(this.txt_search_TextChanged);
            //
            // lbl_search
            //
            this.lbl_search.AutoSize = true;
            this.lbl_search.Location = new System.Drawing.Point(8, 13);
            this.lbl_search.Name = "lbl_search";
            this.lbl_search.Size = new System.Drawing.Size(44, 13);
            this.lbl_search.TabIndex = 0;
            this.lbl_search.Text = "Search";
            //
            // pnl_bottom
            //
            this.pnl_bottom.Controls.Add(this.btn_select);
            this.pnl_bottom.Controls.Add(this.btn_cancel);
            this.pnl_bottom.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.pnl_bottom.Location = new System.Drawing.Point(0, 410);
            this.pnl_bottom.Name = "pnl_bottom";
            this.pnl_bottom.Padding = new System.Windows.Forms.Padding(8);
            this.pnl_bottom.Size = new System.Drawing.Size(700, 48);
            this.pnl_bottom.TabIndex = 2;
            //
            // btn_select
            //
            this.btn_select.Location = new System.Drawing.Point(524, 10);
            this.btn_select.Name = "btn_select";
            this.btn_select.Size = new System.Drawing.Size(80, 28);
            this.btn_select.TabIndex = 0;
            this.btn_select.Text = "Select";
            this.btn_select.UseVisualStyleBackColor = true;
            this.btn_select.Click += new System.EventHandler(this.btn_select_Click);
            //
            // btn_cancel
            //
            this.btn_cancel.Location = new System.Drawing.Point(610, 10);
            this.btn_cancel.Name = "btn_cancel";
            this.btn_cancel.Size = new System.Drawing.Size(80, 28);
            this.btn_cancel.TabIndex = 1;
            this.btn_cancel.Text = "Cancel";
            this.btn_cancel.UseVisualStyleBackColor = true;
            this.btn_cancel.Click += new System.EventHandler(this.btn_cancel_Click);
            //
            // dgv_invoices
            //
            this.dgv_invoices.AllowUserToAddRows = false;
            this.dgv_invoices.AllowUserToDeleteRows = false;
            this.dgv_invoices.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgv_invoices.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.col_id,
            this.col_doc_no,
            this.col_customer,
            this.col_doc_date,
            this.col_so});
            this.dgv_invoices.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgv_invoices.Location = new System.Drawing.Point(0, 40);
            this.dgv_invoices.MultiSelect = false;
            this.dgv_invoices.Name = "dgv_invoices";
            this.dgv_invoices.ReadOnly = true;
            this.dgv_invoices.RowHeadersVisible = false;
            this.dgv_invoices.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgv_invoices.Size = new System.Drawing.Size(700, 370);
            this.dgv_invoices.TabIndex = 1;
            this.dgv_invoices.CellDoubleClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.dgv_invoices_CellDoubleClick);
            //
            // col_id
            //
            this.col_id.HeaderText = "id";
            this.col_id.Name = "col_id";
            this.col_id.Visible = false;
            //
            // col_doc_no
            //
            this.col_doc_no.HeaderText = "SI#";
            this.col_doc_no.Name = "col_doc_no";
            //
            // col_customer
            //
            this.col_customer.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.col_customer.HeaderText = "CUSTOMER";
            this.col_customer.Name = "col_customer";
            //
            // col_doc_date
            //
            this.col_doc_date.HeaderText = "DOC DATE";
            this.col_doc_date.Name = "col_doc_date";
            //
            // col_so
            //
            this.col_so.HeaderText = "REF. SO";
            this.col_so.Name = "col_so";
            //
            // SalesInvoicePickerModal
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(700, 458);
            this.Controls.Add(this.dgv_invoices);
            this.Controls.Add(this.pnl_bottom);
            this.Controls.Add(this.pnl_top);
            this.MinimizeBox = false;
            this.MaximizeBox = false;
            this.Name = "SalesInvoicePickerModal";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Select Sales Invoice";
            this.pnl_top.ResumeLayout(false);
            this.pnl_top.PerformLayout();
            this.pnl_bottom.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dgv_invoices)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel pnl_top;
        private System.Windows.Forms.TextBox txt_search;
        private System.Windows.Forms.Label lbl_search;
        private System.Windows.Forms.Panel pnl_bottom;
        private System.Windows.Forms.Button btn_select;
        private System.Windows.Forms.Button btn_cancel;
        private System.Windows.Forms.DataGridView dgv_invoices;
        private System.Windows.Forms.DataGridViewTextBoxColumn col_id;
        private System.Windows.Forms.DataGridViewTextBoxColumn col_doc_no;
        private System.Windows.Forms.DataGridViewTextBoxColumn col_customer;
        private System.Windows.Forms.DataGridViewTextBoxColumn col_doc_date;
        private System.Windows.Forms.DataGridViewTextBoxColumn col_so;
    }
}
