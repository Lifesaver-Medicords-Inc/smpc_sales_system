
namespace smpc_sales_app.Pages.Sales
{
    partial class SalesReturn
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

        #region Component Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.panel1 = new System.Windows.Forms.Panel();
            this.pnl_footer = new System.Windows.Forms.Panel();
            this.btn_cancel = new System.Windows.Forms.Button();
            this.btn_save = new System.Windows.Forms.Button();
            this.btn_approve = new System.Windows.Forms.Button();
            this.btn_generate_credit_memo = new System.Windows.Forms.Button();
            this.lbl_total = new System.Windows.Forms.Label();
            this.txt_total = new System.Windows.Forms.TextBox();
            this.pnl_main = new System.Windows.Forms.Panel();
            this.dgv_sales_return_details = new System.Windows.Forms.DataGridView();
            this.col_details_id = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.col_item_id = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.col_item_code = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.col_description = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.col_uom = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.col_qty_returned = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.col_qty_received = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.col_qty_discrepancy = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.col_qty_for_replacement = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.col_qty_to_stock = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.col_qty_for_purchase_return = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.col_unit_price = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.col_total_cost = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.col_reason_for_return = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.panel_top = new System.Windows.Forms.Panel();
            this.pnl_header = new System.Windows.Forms.Panel();
            this.lbl_customer_code = new System.Windows.Forms.Label();
            this.txt_customer_code = new System.Windows.Forms.TextBox();
            this.txt_customer_id = new System.Windows.Forms.TextBox();
            this.lbl_ref_doc_type = new System.Windows.Forms.Label();
            this.cmb_ref_doc_type = new System.Windows.Forms.ComboBox();
            this.lbl_doc_no = new System.Windows.Forms.Label();
            this.txt_document_no = new System.Windows.Forms.TextBox();
            this.lbl_customer_name = new System.Windows.Forms.Label();
            this.txt_customer_name = new System.Windows.Forms.TextBox();
            this.lbl_ref_doc_no = new System.Windows.Forms.Label();
            this.txt_ref_doc_no = new System.Windows.Forms.TextBox();
            this.txt_ref_doc_id = new System.Windows.Forms.TextBox();
            this.lbl_doc_date = new System.Windows.Forms.Label();
            this.dtp_date = new System.Windows.Forms.DateTimePicker();
            this.lbl_transaction_type = new System.Windows.Forms.Label();
            this.txt_transaction_type = new System.Windows.Forms.TextBox();
            this.lbl_expected_returned_date = new System.Windows.Forms.Label();
            this.dtp_expected_returned_date = new System.Windows.Forms.DateTimePicker();
            this.lbl_salesperson = new System.Windows.Forms.Label();
            this.txt_salesperson = new System.Windows.Forms.TextBox();
            this.lbl_ship_to = new System.Windows.Forms.Label();
            this.txt_ship_to = new System.Windows.Forms.TextBox();
            this.lbl_currency = new System.Windows.Forms.Label();
            this.txt_currency = new System.Windows.Forms.TextBox();
            this.lbl_sales_period = new System.Windows.Forms.Label();
            this.txt_sales_period = new System.Windows.Forms.TextBox();
            this.lbl_location_group = new System.Windows.Forms.Label();
            this.txt_location_group = new System.Windows.Forms.TextBox();
            this.lbl_location_code = new System.Windows.Forms.Label();
            this.txt_location_code = new System.Windows.Forms.TextBox();
            this.lbl_address = new System.Windows.Forms.Label();
            this.txt_address = new System.Windows.Forms.TextBox();
            this.lbl_cm_reason_code = new System.Windows.Forms.Label();
            this.txt_cm_reason_code = new System.Windows.Forms.TextBox();
            this.lbl_ref_cm_no = new System.Windows.Forms.Label();
            this.txt_ref_cm_no = new System.Windows.Forms.TextBox();
            this.lbl_approved_by = new System.Windows.Forms.Label();
            this.txt_approved_by = new System.Windows.Forms.TextBox();
            this.lbl_approval_date = new System.Windows.Forms.Label();
            this.txt_approval_date = new System.Windows.Forms.TextBox();
            this.lbl_header_remarks = new System.Windows.Forms.Label();
            this.txt_header_remarks = new System.Windows.Forms.TextBox();
            this.lbl_description = new System.Windows.Forms.Label();
            this.txt_description = new System.Windows.Forms.TextBox();
            this.toolStrip1 = new System.Windows.Forms.ToolStrip();
            this.btn_new = new System.Windows.Forms.ToolStripButton();
            this.btn_search = new System.Windows.Forms.ToolStripButton();
            this.btn_prev = new System.Windows.Forms.ToolStripButton();
            this.btn_next = new System.Windows.Forms.ToolStripButton();
            this.btn_edit = new System.Windows.Forms.ToolStripButton();
            this.btn_cancel_edit = new System.Windows.Forms.ToolStripButton();
            this.lbl_title = new System.Windows.Forms.Label();
            this.panel1.SuspendLayout();
            this.pnl_footer.SuspendLayout();
            this.pnl_main.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgv_sales_return_details)).BeginInit();
            this.panel_top.SuspendLayout();
            this.pnl_header.SuspendLayout();
            this.toolStrip1.SuspendLayout();
            this.SuspendLayout();
            //
            // panel1
            //
            this.panel1.Controls.Add(this.pnl_footer);
            this.panel1.Controls.Add(this.pnl_main);
            this.panel1.Controls.Add(this.panel_top);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel1.Location = new System.Drawing.Point(0, 0);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(1229, 900);
            this.panel1.TabIndex = 0;
            //
            // pnl_footer
            //
            this.pnl_footer.Controls.Add(this.btn_cancel);
            this.pnl_footer.Controls.Add(this.btn_save);
            this.pnl_footer.Controls.Add(this.btn_approve);
            this.pnl_footer.Controls.Add(this.btn_generate_credit_memo);
            this.pnl_footer.Controls.Add(this.lbl_total);
            this.pnl_footer.Controls.Add(this.txt_total);
            this.pnl_footer.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.pnl_footer.Location = new System.Drawing.Point(0, 840);
            this.pnl_footer.Name = "pnl_footer";
            this.pnl_footer.Size = new System.Drawing.Size(1229, 60);
            this.pnl_footer.TabIndex = 1;
            //
            // btn_cancel
            //
            this.btn_cancel.BackColor = System.Drawing.Color.Firebrick;
            this.btn_cancel.Enabled = false;
            this.btn_cancel.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btn_cancel.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold);
            this.btn_cancel.ForeColor = System.Drawing.Color.White;
            this.btn_cancel.Location = new System.Drawing.Point(1121, 14);
            this.btn_cancel.Name = "btn_cancel";
            this.btn_cancel.Size = new System.Drawing.Size(92, 32);
            this.btn_cancel.TabIndex = 6;
            this.btn_cancel.Text = "Cancel";
            this.btn_cancel.UseVisualStyleBackColor = false;
            //
            // btn_save
            //
            this.btn_save.BackColor = System.Drawing.Color.SeaGreen;
            this.btn_save.Enabled = false;
            this.btn_save.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btn_save.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold);
            this.btn_save.ForeColor = System.Drawing.Color.White;
            this.btn_save.Location = new System.Drawing.Point(1015, 14);
            this.btn_save.Name = "btn_save";
            this.btn_save.Size = new System.Drawing.Size(92, 32);
            this.btn_save.TabIndex = 5;
            this.btn_save.Text = "Save";
            this.btn_save.UseVisualStyleBackColor = false;
            //
            // btn_approve
            //
            // Sales Manager approval gate (Sec5.13/Sec3.3) - visible only on a saved,
            // unapproved return; nothing about approval is implied by Save itself.
            this.btn_approve.BackColor = System.Drawing.Color.SteelBlue;
            this.btn_approve.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btn_approve.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold);
            this.btn_approve.ForeColor = System.Drawing.Color.White;
            this.btn_approve.Location = new System.Drawing.Point(909, 14);
            this.btn_approve.Name = "btn_approve";
            this.btn_approve.Size = new System.Drawing.Size(92, 32);
            this.btn_approve.TabIndex = 4;
            this.btn_approve.Text = "Approve";
            this.btn_approve.UseVisualStyleBackColor = false;
            this.btn_approve.Visible = false;
            //
            // btn_generate_credit_memo
            //
            // Sec5.13/Sec14.63 - enabled only once IsApproved is true; A/R presses this
            // deliberately, it never fires automatically on approval.
            this.btn_generate_credit_memo.Enabled = false;
            this.btn_generate_credit_memo.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btn_generate_credit_memo.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold);
            this.btn_generate_credit_memo.Location = new System.Drawing.Point(741, 14);
            this.btn_generate_credit_memo.Name = "btn_generate_credit_memo";
            this.btn_generate_credit_memo.Size = new System.Drawing.Size(160, 32);
            this.btn_generate_credit_memo.TabIndex = 3;
            this.btn_generate_credit_memo.Text = "Generate Credit Memo";
            this.btn_generate_credit_memo.UseVisualStyleBackColor = true;
            //
            // lbl_total
            //
            this.lbl_total.AutoSize = true;
            this.lbl_total.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold);
            this.lbl_total.Location = new System.Drawing.Point(20, 20);
            this.lbl_total.Name = "lbl_total";
            this.lbl_total.Size = new System.Drawing.Size(38, 13);
            this.lbl_total.TabIndex = 1;
            this.lbl_total.Text = "TOTAL";
            //
            // txt_total
            //
            this.txt_total.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Bold);
            this.txt_total.Location = new System.Drawing.Point(80, 17);
            this.txt_total.Name = "txt_total";
            this.txt_total.ReadOnly = true;
            this.txt_total.Size = new System.Drawing.Size(140, 22);
            this.txt_total.TabIndex = 2;
            this.txt_total.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.txt_total.Text = "0.00";
            //
            // pnl_main
            //
            this.pnl_main.Controls.Add(this.dgv_sales_return_details);
            this.pnl_main.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnl_main.Location = new System.Drawing.Point(0, 330);
            this.pnl_main.Name = "pnl_main";
            this.pnl_main.Padding = new System.Windows.Forms.Padding(8);
            this.pnl_main.Size = new System.Drawing.Size(1229, 510);
            this.pnl_main.TabIndex = 2;
            //
            // dgv_sales_return_details
            //
            this.dgv_sales_return_details.AllowUserToAddRows = false;
            this.dgv_sales_return_details.AutoGenerateColumns = false;
            this.dgv_sales_return_details.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgv_sales_return_details.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.col_details_id,
            this.col_item_id,
            this.col_item_code,
            this.col_description,
            this.col_uom,
            this.col_qty_returned,
            this.col_qty_received,
            this.col_qty_discrepancy,
            this.col_qty_for_replacement,
            this.col_qty_to_stock,
            this.col_qty_for_purchase_return,
            this.col_unit_price,
            this.col_total_cost,
            this.col_reason_for_return});
            this.dgv_sales_return_details.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgv_sales_return_details.Location = new System.Drawing.Point(8, 8);
            this.dgv_sales_return_details.Name = "dgv_sales_return_details";
            this.dgv_sales_return_details.RowHeadersWidthSizeMode = System.Windows.Forms.DataGridViewRowHeadersWidthSizeMode.AutoSizeToAllHeaders;
            this.dgv_sales_return_details.Size = new System.Drawing.Size(1213, 494);
            this.dgv_sales_return_details.TabIndex = 0;
            //
            // col_details_id
            //
            this.col_details_id.HeaderText = "id";
            this.col_details_id.Name = "col_details_id";
            this.col_details_id.Visible = false;
            //
            // col_item_id
            //
            this.col_item_id.HeaderText = "item_id";
            this.col_item_id.Name = "col_item_id";
            this.col_item_id.Visible = false;
            //
            // col_item_code
            //
            this.col_item_code.HeaderText = "ITEM CODE";
            this.col_item_code.Name = "col_item_code";
            this.col_item_code.ReadOnly = true;
            //
            // col_description
            //
            this.col_description.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.col_description.HeaderText = "DESCRIPTION";
            this.col_description.Name = "col_description";
            this.col_description.ReadOnly = true;
            //
            // col_uom
            //
            this.col_uom.HeaderText = "UOM";
            this.col_uom.Name = "col_uom";
            this.col_uom.ReadOnly = true;
            //
            // col_qty_returned
            //
            this.col_qty_returned.HeaderText = "QTY RETURNED";
            this.col_qty_returned.Name = "col_qty_returned";
            this.col_qty_returned.ReadOnly = true;
            //
            // col_qty_received
            //
            this.col_qty_received.HeaderText = "QTY RECEIVED";
            this.col_qty_received.Name = "col_qty_received";
            //
            // col_qty_discrepancy
            //
            // Computed (QtyReturned - QtyReceived), never typed - Sec5.13.
            this.col_qty_discrepancy.HeaderText = "QTY DISCREPANCY";
            this.col_qty_discrepancy.Name = "col_qty_discrepancy";
            this.col_qty_discrepancy.ReadOnly = true;
            //
            // col_qty_for_replacement
            //
            this.col_qty_for_replacement.HeaderText = "QTY FOR REPLACEMENT";
            this.col_qty_for_replacement.Name = "col_qty_for_replacement";
            //
            // col_qty_to_stock
            //
            this.col_qty_to_stock.HeaderText = "QTY TO STOCK";
            this.col_qty_to_stock.Name = "col_qty_to_stock";
            //
            // col_qty_for_purchase_return
            //
            this.col_qty_for_purchase_return.HeaderText = "QTY FOR PURCHASE RETURN";
            this.col_qty_for_purchase_return.Name = "col_qty_for_purchase_return";
            //
            // col_unit_price
            //
            // Read-only, from the reference document - Sec14.95. No discount fields exist
            // on this document (Sec14.94).
            this.col_unit_price.HeaderText = "UNIT PRICE";
            this.col_unit_price.Name = "col_unit_price";
            this.col_unit_price.ReadOnly = true;
            //
            // col_total_cost
            //
            this.col_total_cost.HeaderText = "TOTAL COST";
            this.col_total_cost.Name = "col_total_cost";
            this.col_total_cost.ReadOnly = true;
            //
            // col_reason_for_return
            //
            this.col_reason_for_return.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.col_reason_for_return.HeaderText = "REASON FOR RETURN";
            this.col_reason_for_return.Name = "col_reason_for_return";
            //
            // panel_top
            //
            this.panel_top.Controls.Add(this.pnl_header);
            this.panel_top.Controls.Add(this.toolStrip1);
            this.panel_top.Controls.Add(this.lbl_title);
            this.panel_top.Dock = System.Windows.Forms.DockStyle.Top;
            this.panel_top.Location = new System.Drawing.Point(0, 0);
            this.panel_top.Name = "panel_top";
            this.panel_top.Size = new System.Drawing.Size(1229, 330);
            this.panel_top.TabIndex = 0;
            //
            // pnl_header
            //
            // Field values marked read-only below (SALESPERSON, CURRENCY, SALES PERIOD,
            // UNIT PRICE, etc.) always come from the reference document once REF. DOC.
            // TYPE/NO. are chosen - never editable here, never re-derived from today's
            // data. Sec5.13, Sec14.94, Sec14.95, Sec14.97.
            this.pnl_header.Controls.Add(this.lbl_customer_code);
            this.pnl_header.Controls.Add(this.txt_customer_code);
            this.pnl_header.Controls.Add(this.txt_customer_id);
            this.pnl_header.Controls.Add(this.lbl_ref_doc_type);
            this.pnl_header.Controls.Add(this.cmb_ref_doc_type);
            this.pnl_header.Controls.Add(this.lbl_doc_no);
            this.pnl_header.Controls.Add(this.txt_document_no);
            this.pnl_header.Controls.Add(this.lbl_customer_name);
            this.pnl_header.Controls.Add(this.txt_customer_name);
            this.pnl_header.Controls.Add(this.lbl_ref_doc_no);
            this.pnl_header.Controls.Add(this.txt_ref_doc_no);
            this.pnl_header.Controls.Add(this.txt_ref_doc_id);
            this.pnl_header.Controls.Add(this.lbl_doc_date);
            this.pnl_header.Controls.Add(this.dtp_date);
            this.pnl_header.Controls.Add(this.lbl_transaction_type);
            this.pnl_header.Controls.Add(this.txt_transaction_type);
            this.pnl_header.Controls.Add(this.lbl_expected_returned_date);
            this.pnl_header.Controls.Add(this.dtp_expected_returned_date);
            this.pnl_header.Controls.Add(this.lbl_salesperson);
            this.pnl_header.Controls.Add(this.txt_salesperson);
            this.pnl_header.Controls.Add(this.lbl_ship_to);
            this.pnl_header.Controls.Add(this.txt_ship_to);
            this.pnl_header.Controls.Add(this.lbl_currency);
            this.pnl_header.Controls.Add(this.txt_currency);
            this.pnl_header.Controls.Add(this.lbl_sales_period);
            this.pnl_header.Controls.Add(this.txt_sales_period);
            this.pnl_header.Controls.Add(this.lbl_location_group);
            this.pnl_header.Controls.Add(this.txt_location_group);
            this.pnl_header.Controls.Add(this.lbl_location_code);
            this.pnl_header.Controls.Add(this.txt_location_code);
            this.pnl_header.Controls.Add(this.lbl_address);
            this.pnl_header.Controls.Add(this.txt_address);
            this.pnl_header.Controls.Add(this.lbl_cm_reason_code);
            this.pnl_header.Controls.Add(this.txt_cm_reason_code);
            this.pnl_header.Controls.Add(this.lbl_ref_cm_no);
            this.pnl_header.Controls.Add(this.txt_ref_cm_no);
            this.pnl_header.Controls.Add(this.lbl_approved_by);
            this.pnl_header.Controls.Add(this.txt_approved_by);
            this.pnl_header.Controls.Add(this.lbl_approval_date);
            this.pnl_header.Controls.Add(this.txt_approval_date);
            this.pnl_header.Controls.Add(this.lbl_header_remarks);
            this.pnl_header.Controls.Add(this.txt_header_remarks);
            this.pnl_header.Controls.Add(this.lbl_description);
            this.pnl_header.Controls.Add(this.txt_description);
            this.pnl_header.Location = new System.Drawing.Point(0, 47);
            this.pnl_header.Name = "pnl_header";
            this.pnl_header.Size = new System.Drawing.Size(1229, 283);
            this.pnl_header.TabIndex = 1;
            //
            // lbl_customer_code
            //
            this.lbl_customer_code.AutoSize = true;
            this.lbl_customer_code.Location = new System.Drawing.Point(12, 15);
            this.lbl_customer_code.Name = "lbl_customer_code";
            this.lbl_customer_code.Size = new System.Drawing.Size(85, 13);
            this.lbl_customer_code.TabIndex = 0;
            this.lbl_customer_code.Text = "CUSTOMER CODE";
            //
            // txt_customer_code
            //
            this.txt_customer_code.Location = new System.Drawing.Point(140, 12);
            this.txt_customer_code.Name = "txt_customer_code";
            this.txt_customer_code.ReadOnly = true;
            this.txt_customer_code.Size = new System.Drawing.Size(180, 20);
            this.txt_customer_code.TabIndex = 1;
            //
            // txt_customer_id
            //
            // Hidden - resolved BPI id backing txt_customer_code, not itself displayed.
            this.txt_customer_id.Location = new System.Drawing.Point(140, 12);
            this.txt_customer_id.Name = "txt_customer_id";
            this.txt_customer_id.Size = new System.Drawing.Size(180, 20);
            this.txt_customer_id.TabIndex = 2;
            this.txt_customer_id.Visible = false;
            //
            // lbl_ref_doc_type
            //
            // Sec5.13/Sec14.62 - MUST be chosen before item selection is allowed.
            this.lbl_ref_doc_type.AutoSize = true;
            this.lbl_ref_doc_type.Location = new System.Drawing.Point(340, 15);
            this.lbl_ref_doc_type.Name = "lbl_ref_doc_type";
            this.lbl_ref_doc_type.Size = new System.Drawing.Size(72, 13);
            this.lbl_ref_doc_type.TabIndex = 3;
            this.lbl_ref_doc_type.Text = "REF. DOC. TYPE";
            //
            // cmb_ref_doc_type
            //
            this.cmb_ref_doc_type.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmb_ref_doc_type.FormattingEnabled = true;
            this.cmb_ref_doc_type.Items.AddRange(new object[] {
            "--Select--",
            "Sales Invoice",
            "Delivery Receipt"});
            this.cmb_ref_doc_type.Location = new System.Drawing.Point(468, 12);
            this.cmb_ref_doc_type.Name = "cmb_ref_doc_type";
            this.cmb_ref_doc_type.Size = new System.Drawing.Size(180, 21);
            this.cmb_ref_doc_type.TabIndex = 4;
            //
            // lbl_doc_no
            //
            this.lbl_doc_no.AutoSize = true;
            this.lbl_doc_no.Location = new System.Drawing.Point(668, 15);
            this.lbl_doc_no.Name = "lbl_doc_no";
            this.lbl_doc_no.Size = new System.Drawing.Size(48, 13);
            this.lbl_doc_no.TabIndex = 5;
            this.lbl_doc_no.Text = "SRT#";
            //
            // txt_document_no
            //
            this.txt_document_no.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold);
            this.txt_document_no.Location = new System.Drawing.Point(796, 12);
            this.txt_document_no.Name = "txt_document_no";
            this.txt_document_no.ReadOnly = true;
            this.txt_document_no.Size = new System.Drawing.Size(180, 20);
            this.txt_document_no.TabIndex = 6;
            this.txt_document_no.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            //
            // lbl_customer_name
            //
            this.lbl_customer_name.AutoSize = true;
            this.lbl_customer_name.Location = new System.Drawing.Point(12, 41);
            this.lbl_customer_name.Name = "lbl_customer_name";
            this.lbl_customer_name.Size = new System.Drawing.Size(87, 13);
            this.lbl_customer_name.TabIndex = 7;
            this.lbl_customer_name.Text = "CUSTOMER NAME";
            //
            // txt_customer_name
            //
            this.txt_customer_name.Location = new System.Drawing.Point(140, 38);
            this.txt_customer_name.Name = "txt_customer_name";
            this.txt_customer_name.ReadOnly = true;
            this.txt_customer_name.Size = new System.Drawing.Size(180, 20);
            this.txt_customer_name.TabIndex = 8;
            //
            // lbl_ref_doc_no
            //
            this.lbl_ref_doc_no.AutoSize = true;
            this.lbl_ref_doc_no.Location = new System.Drawing.Point(340, 41);
            this.lbl_ref_doc_no.Name = "lbl_ref_doc_no";
            this.lbl_ref_doc_no.Size = new System.Drawing.Size(65, 13);
            this.lbl_ref_doc_no.TabIndex = 9;
            this.lbl_ref_doc_no.Text = "REF. DOC. NO.";
            //
            // txt_ref_doc_no
            //
            // Populated via a search modal scoped by cmb_ref_doc_type's selection - see
            // btn_ref_doc-equivalent click handler in the code-behind (not yet wired,
            // this is the design pass).
            this.txt_ref_doc_no.Location = new System.Drawing.Point(468, 38);
            this.txt_ref_doc_no.Name = "txt_ref_doc_no";
            this.txt_ref_doc_no.ReadOnly = true;
            this.txt_ref_doc_no.Size = new System.Drawing.Size(180, 20);
            this.txt_ref_doc_no.TabIndex = 10;
            //
            // txt_ref_doc_id
            //
            this.txt_ref_doc_id.Location = new System.Drawing.Point(468, 38);
            this.txt_ref_doc_id.Name = "txt_ref_doc_id";
            this.txt_ref_doc_id.Size = new System.Drawing.Size(180, 20);
            this.txt_ref_doc_id.TabIndex = 11;
            this.txt_ref_doc_id.Visible = false;
            //
            // lbl_doc_date
            //
            this.lbl_doc_date.AutoSize = true;
            this.lbl_doc_date.Location = new System.Drawing.Point(668, 41);
            this.lbl_doc_date.Name = "lbl_doc_date";
            this.lbl_doc_date.Size = new System.Drawing.Size(64, 13);
            this.lbl_doc_date.TabIndex = 12;
            this.lbl_doc_date.Text = "DOCUMENT DATE";
            //
            // dtp_date
            //
            this.dtp_date.Enabled = false;
            this.dtp_date.Format = System.Windows.Forms.DateTimePickerFormat.Short;
            this.dtp_date.Location = new System.Drawing.Point(796, 38);
            this.dtp_date.Name = "dtp_date";
            this.dtp_date.Size = new System.Drawing.Size(180, 20);
            this.dtp_date.TabIndex = 13;
            //
            // lbl_transaction_type
            //
            this.lbl_transaction_type.AutoSize = true;
            this.lbl_transaction_type.Location = new System.Drawing.Point(12, 67);
            this.lbl_transaction_type.Name = "lbl_transaction_type";
            this.lbl_transaction_type.Size = new System.Drawing.Size(83, 13);
            this.lbl_transaction_type.TabIndex = 14;
            this.lbl_transaction_type.Text = "TRANSACTION TYPE";
            //
            // txt_transaction_type
            //
            this.txt_transaction_type.Location = new System.Drawing.Point(140, 64);
            this.txt_transaction_type.Name = "txt_transaction_type";
            this.txt_transaction_type.ReadOnly = true;
            this.txt_transaction_type.Size = new System.Drawing.Size(180, 20);
            this.txt_transaction_type.TabIndex = 15;
            //
            // lbl_expected_returned_date
            //
            this.lbl_expected_returned_date.AutoSize = true;
            this.lbl_expected_returned_date.Location = new System.Drawing.Point(340, 67);
            this.lbl_expected_returned_date.Name = "lbl_expected_returned_date";
            this.lbl_expected_returned_date.Size = new System.Drawing.Size(115, 13);
            this.lbl_expected_returned_date.TabIndex = 16;
            this.lbl_expected_returned_date.Text = "EXPECTED RETURNED DATE";
            //
            // dtp_expected_returned_date
            //
            this.dtp_expected_returned_date.Format = System.Windows.Forms.DateTimePickerFormat.Short;
            this.dtp_expected_returned_date.Location = new System.Drawing.Point(468, 64);
            this.dtp_expected_returned_date.Name = "dtp_expected_returned_date";
            this.dtp_expected_returned_date.Size = new System.Drawing.Size(180, 20);
            this.dtp_expected_returned_date.TabIndex = 17;
            //
            // lbl_salesperson
            //
            // Sec14.97 - from the reference document, never the logged-in user.
            this.lbl_salesperson.AutoSize = true;
            this.lbl_salesperson.Location = new System.Drawing.Point(668, 67);
            this.lbl_salesperson.Name = "lbl_salesperson";
            this.lbl_salesperson.Size = new System.Drawing.Size(66, 13);
            this.lbl_salesperson.TabIndex = 18;
            this.lbl_salesperson.Text = "SALESPERSON";
            //
            // txt_salesperson
            //
            this.txt_salesperson.Location = new System.Drawing.Point(796, 64);
            this.txt_salesperson.Name = "txt_salesperson";
            this.txt_salesperson.ReadOnly = true;
            this.txt_salesperson.Size = new System.Drawing.Size(180, 20);
            this.txt_salesperson.TabIndex = 19;
            //
            // lbl_ship_to
            //
            this.lbl_ship_to.AutoSize = true;
            this.lbl_ship_to.Location = new System.Drawing.Point(12, 93);
            this.lbl_ship_to.Name = "lbl_ship_to";
            this.lbl_ship_to.Size = new System.Drawing.Size(42, 13);
            this.lbl_ship_to.TabIndex = 20;
            this.lbl_ship_to.Text = "SHIP TO";
            //
            // txt_ship_to
            //
            this.txt_ship_to.Location = new System.Drawing.Point(140, 90);
            this.txt_ship_to.Name = "txt_ship_to";
            this.txt_ship_to.ReadOnly = true;
            this.txt_ship_to.Size = new System.Drawing.Size(180, 20);
            this.txt_ship_to.TabIndex = 21;
            //
            // lbl_currency
            //
            this.lbl_currency.AutoSize = true;
            this.lbl_currency.Location = new System.Drawing.Point(340, 93);
            this.lbl_currency.Name = "lbl_currency";
            this.lbl_currency.Size = new System.Drawing.Size(48, 13);
            this.lbl_currency.TabIndex = 22;
            this.lbl_currency.Text = "CURRENCY";
            //
            // txt_currency
            //
            this.txt_currency.Location = new System.Drawing.Point(468, 90);
            this.txt_currency.Name = "txt_currency";
            this.txt_currency.ReadOnly = true;
            this.txt_currency.Size = new System.Drawing.Size(180, 20);
            this.txt_currency.TabIndex = 23;
            //
            // lbl_sales_period
            //
            this.lbl_sales_period.AutoSize = true;
            this.lbl_sales_period.Location = new System.Drawing.Point(668, 93);
            this.lbl_sales_period.Name = "lbl_sales_period";
            this.lbl_sales_period.Size = new System.Drawing.Size(64, 13);
            this.lbl_sales_period.TabIndex = 24;
            this.lbl_sales_period.Text = "SALES PERIOD";
            //
            // txt_sales_period
            //
            this.txt_sales_period.Location = new System.Drawing.Point(796, 90);
            this.txt_sales_period.Name = "txt_sales_period";
            this.txt_sales_period.ReadOnly = true;
            this.txt_sales_period.Size = new System.Drawing.Size(180, 20);
            this.txt_sales_period.TabIndex = 25;
            //
            // lbl_location_group
            //
            this.lbl_location_group.AutoSize = true;
            this.lbl_location_group.Location = new System.Drawing.Point(12, 119);
            this.lbl_location_group.Name = "lbl_location_group";
            this.lbl_location_group.Size = new System.Drawing.Size(76, 13);
            this.lbl_location_group.TabIndex = 26;
            this.lbl_location_group.Text = "LOCATION GROUP";
            //
            // txt_location_group
            //
            this.txt_location_group.Location = new System.Drawing.Point(140, 116);
            this.txt_location_group.Name = "txt_location_group";
            this.txt_location_group.ReadOnly = true;
            this.txt_location_group.Size = new System.Drawing.Size(180, 20);
            this.txt_location_group.TabIndex = 27;
            //
            // lbl_location_code
            //
            this.lbl_location_code.AutoSize = true;
            this.lbl_location_code.Location = new System.Drawing.Point(340, 119);
            this.lbl_location_code.Name = "lbl_location_code";
            this.lbl_location_code.Size = new System.Drawing.Size(71, 13);
            this.lbl_location_code.TabIndex = 28;
            this.lbl_location_code.Text = "LOCATION CODE";
            //
            // txt_location_code
            //
            this.txt_location_code.Location = new System.Drawing.Point(468, 116);
            this.txt_location_code.Name = "txt_location_code";
            this.txt_location_code.ReadOnly = true;
            this.txt_location_code.Size = new System.Drawing.Size(180, 20);
            this.txt_location_code.TabIndex = 29;
            //
            // lbl_address
            //
            this.lbl_address.AutoSize = true;
            this.lbl_address.Location = new System.Drawing.Point(668, 119);
            this.lbl_address.Name = "lbl_address";
            this.lbl_address.Size = new System.Drawing.Size(45, 13);
            this.lbl_address.TabIndex = 30;
            this.lbl_address.Text = "ADDRESS";
            //
            // txt_address
            //
            this.txt_address.Location = new System.Drawing.Point(796, 116);
            this.txt_address.Name = "txt_address";
            this.txt_address.ReadOnly = true;
            this.txt_address.Size = new System.Drawing.Size(180, 20);
            this.txt_address.TabIndex = 31;
            //
            // lbl_cm_reason_code
            //
            // Sec5.13 - optional, pre-fills the eventual Credit Memo's required REASON
            // CODE. The memo itself still requires one at save regardless.
            this.lbl_cm_reason_code.AutoSize = true;
            this.lbl_cm_reason_code.Location = new System.Drawing.Point(12, 145);
            this.lbl_cm_reason_code.Name = "lbl_cm_reason_code";
            this.lbl_cm_reason_code.Size = new System.Drawing.Size(90, 13);
            this.lbl_cm_reason_code.TabIndex = 32;
            this.lbl_cm_reason_code.Text = "CM REASON CODE";
            //
            // txt_cm_reason_code
            //
            this.txt_cm_reason_code.Location = new System.Drawing.Point(140, 142);
            this.txt_cm_reason_code.Name = "txt_cm_reason_code";
            this.txt_cm_reason_code.Size = new System.Drawing.Size(180, 20);
            this.txt_cm_reason_code.TabIndex = 33;
            //
            // lbl_ref_cm_no
            //
            // Sec5.13 - read-only, populated once GENERATE CREDIT MEMO produces a CM#;
            // blank where no credit was granted.
            this.lbl_ref_cm_no.AutoSize = true;
            this.lbl_ref_cm_no.Location = new System.Drawing.Point(340, 145);
            this.lbl_ref_cm_no.Name = "lbl_ref_cm_no";
            this.lbl_ref_cm_no.Size = new System.Drawing.Size(62, 13);
            this.lbl_ref_cm_no.TabIndex = 34;
            this.lbl_ref_cm_no.Text = "REF. CM NO.";
            //
            // txt_ref_cm_no
            //
            this.txt_ref_cm_no.Location = new System.Drawing.Point(468, 142);
            this.txt_ref_cm_no.Name = "txt_ref_cm_no";
            this.txt_ref_cm_no.ReadOnly = true;
            this.txt_ref_cm_no.Size = new System.Drawing.Size(180, 20);
            this.txt_ref_cm_no.TabIndex = 35;
            //
            // lbl_approved_by
            //
            // Sec3.4/Sec3.3 - the approver's name MUST be displayed once approved.
            this.lbl_approved_by.AutoSize = true;
            this.lbl_approved_by.Location = new System.Drawing.Point(668, 145);
            this.lbl_approved_by.Name = "lbl_approved_by";
            this.lbl_approved_by.Size = new System.Drawing.Size(66, 13);
            this.lbl_approved_by.TabIndex = 36;
            this.lbl_approved_by.Text = "APPROVED BY";
            //
            // txt_approved_by
            //
            this.txt_approved_by.Location = new System.Drawing.Point(796, 142);
            this.txt_approved_by.Name = "txt_approved_by";
            this.txt_approved_by.ReadOnly = true;
            this.txt_approved_by.Size = new System.Drawing.Size(180, 20);
            this.txt_approved_by.TabIndex = 37;
            //
            // lbl_approval_date
            //
            this.lbl_approval_date.AutoSize = true;
            this.lbl_approval_date.Location = new System.Drawing.Point(12, 171);
            this.lbl_approval_date.Name = "lbl_approval_date";
            this.lbl_approval_date.Size = new System.Drawing.Size(78, 13);
            this.lbl_approval_date.TabIndex = 38;
            this.lbl_approval_date.Text = "APPROVAL DATE";
            //
            // txt_approval_date
            //
            this.txt_approval_date.Location = new System.Drawing.Point(140, 168);
            this.txt_approval_date.Name = "txt_approval_date";
            this.txt_approval_date.ReadOnly = true;
            this.txt_approval_date.Size = new System.Drawing.Size(180, 20);
            this.txt_approval_date.TabIndex = 39;
            //
            // lbl_header_remarks
            //
            this.lbl_header_remarks.AutoSize = true;
            this.lbl_header_remarks.Location = new System.Drawing.Point(12, 201);
            this.lbl_header_remarks.Name = "lbl_header_remarks";
            this.lbl_header_remarks.Size = new System.Drawing.Size(85, 13);
            this.lbl_header_remarks.TabIndex = 40;
            this.lbl_header_remarks.Text = "HEADER REMARKS";
            //
            // txt_header_remarks
            //
            this.txt_header_remarks.Location = new System.Drawing.Point(140, 198);
            this.txt_header_remarks.Multiline = true;
            this.txt_header_remarks.Name = "txt_header_remarks";
            this.txt_header_remarks.Size = new System.Drawing.Size(508, 36);
            this.txt_header_remarks.TabIndex = 41;
            //
            // lbl_description
            //
            this.lbl_description.AutoSize = true;
            this.lbl_description.Location = new System.Drawing.Point(668, 201);
            this.lbl_description.Name = "lbl_description";
            this.lbl_description.Size = new System.Drawing.Size(60, 13);
            this.lbl_description.TabIndex = 42;
            this.lbl_description.Text = "DESCRIPTION";
            //
            // txt_description
            //
            this.txt_description.Location = new System.Drawing.Point(796, 198);
            this.txt_description.Multiline = true;
            this.txt_description.Name = "txt_description";
            this.txt_description.Size = new System.Drawing.Size(180, 36);
            this.txt_description.TabIndex = 43;
            //
            // toolStrip1
            //
            this.toolStrip1.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
            this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.btn_new,
            this.btn_search,
            this.btn_prev,
            this.btn_next,
            this.btn_edit,
            this.btn_cancel_edit});
            this.toolStrip1.Location = new System.Drawing.Point(0, 22);
            this.toolStrip1.Name = "toolStrip1";
            this.toolStrip1.RenderMode = System.Windows.Forms.ToolStripRenderMode.Professional;
            this.toolStrip1.Size = new System.Drawing.Size(1229, 25);
            this.toolStrip1.TabIndex = 0;
            this.toolStrip1.Text = "toolStrip1";
            //
            // btn_new
            //
            this.btn_new.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.btn_new.Name = "btn_new";
            this.btn_new.Size = new System.Drawing.Size(35, 22);
            this.btn_new.Text = "New";
            //
            // btn_search
            //
            this.btn_search.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.btn_search.Name = "btn_search";
            this.btn_search.Size = new System.Drawing.Size(46, 22);
            this.btn_search.Text = "Search";
            //
            // btn_prev
            //
            this.btn_prev.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.btn_prev.Name = "btn_prev";
            this.btn_prev.Size = new System.Drawing.Size(45, 22);
            this.btn_prev.Text = "<< PREV";
            //
            // btn_next
            //
            this.btn_next.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.btn_next.Name = "btn_next";
            this.btn_next.Size = new System.Drawing.Size(45, 22);
            this.btn_next.Text = "NEXT >>";
            //
            // btn_edit
            //
            this.btn_edit.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.btn_edit.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold);
            this.btn_edit.Name = "btn_edit";
            this.btn_edit.Size = new System.Drawing.Size(31, 22);
            this.btn_edit.Text = "Edit";
            //
            // btn_cancel_edit
            //
            this.btn_cancel_edit.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.btn_cancel_edit.Name = "btn_cancel_edit";
            this.btn_cancel_edit.Size = new System.Drawing.Size(76, 22);
            this.btn_cancel_edit.Text = "Cancel Edit";
            this.btn_cancel_edit.Visible = false;
            //
            // lbl_title
            //
            this.lbl_title.AutoSize = true;
            this.lbl_title.Dock = System.Windows.Forms.DockStyle.Top;
            this.lbl_title.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold);
            this.lbl_title.Location = new System.Drawing.Point(0, 0);
            this.lbl_title.Name = "lbl_title";
            this.lbl_title.Padding = new System.Windows.Forms.Padding(8, 4, 0, 4);
            this.lbl_title.Size = new System.Drawing.Size(120, 22);
            this.lbl_title.TabIndex = 2;
            this.lbl_title.Text = "SALES RETURN";
            //
            // SalesReturn
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.panel1);
            this.Name = "SalesReturn";
            this.Size = new System.Drawing.Size(1229, 900);
            this.panel1.ResumeLayout(false);
            this.pnl_footer.ResumeLayout(false);
            this.pnl_footer.PerformLayout();
            this.pnl_main.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dgv_sales_return_details)).EndInit();
            this.panel_top.ResumeLayout(false);
            this.panel_top.PerformLayout();
            this.pnl_header.ResumeLayout(false);
            this.pnl_header.PerformLayout();
            this.toolStrip1.ResumeLayout(false);
            this.toolStrip1.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Panel pnl_footer;
        private System.Windows.Forms.Button btn_cancel;
        private System.Windows.Forms.Button btn_save;
        private System.Windows.Forms.Button btn_approve;
        private System.Windows.Forms.Button btn_generate_credit_memo;
        private System.Windows.Forms.Label lbl_total;
        private System.Windows.Forms.TextBox txt_total;
        private System.Windows.Forms.Panel pnl_main;
        private System.Windows.Forms.DataGridView dgv_sales_return_details;
        private System.Windows.Forms.DataGridViewTextBoxColumn col_details_id;
        private System.Windows.Forms.DataGridViewTextBoxColumn col_item_id;
        private System.Windows.Forms.DataGridViewTextBoxColumn col_item_code;
        private System.Windows.Forms.DataGridViewTextBoxColumn col_description;
        private System.Windows.Forms.DataGridViewTextBoxColumn col_uom;
        private System.Windows.Forms.DataGridViewTextBoxColumn col_qty_returned;
        private System.Windows.Forms.DataGridViewTextBoxColumn col_qty_received;
        private System.Windows.Forms.DataGridViewTextBoxColumn col_qty_discrepancy;
        private System.Windows.Forms.DataGridViewTextBoxColumn col_qty_for_replacement;
        private System.Windows.Forms.DataGridViewTextBoxColumn col_qty_to_stock;
        private System.Windows.Forms.DataGridViewTextBoxColumn col_qty_for_purchase_return;
        private System.Windows.Forms.DataGridViewTextBoxColumn col_unit_price;
        private System.Windows.Forms.DataGridViewTextBoxColumn col_total_cost;
        private System.Windows.Forms.DataGridViewTextBoxColumn col_reason_for_return;
        private System.Windows.Forms.Panel panel_top;
        private System.Windows.Forms.Panel pnl_header;
        private System.Windows.Forms.Label lbl_customer_code;
        private System.Windows.Forms.TextBox txt_customer_code;
        private System.Windows.Forms.TextBox txt_customer_id;
        private System.Windows.Forms.Label lbl_ref_doc_type;
        private System.Windows.Forms.ComboBox cmb_ref_doc_type;
        private System.Windows.Forms.Label lbl_doc_no;
        private System.Windows.Forms.TextBox txt_document_no;
        private System.Windows.Forms.Label lbl_customer_name;
        private System.Windows.Forms.TextBox txt_customer_name;
        private System.Windows.Forms.Label lbl_ref_doc_no;
        private System.Windows.Forms.TextBox txt_ref_doc_no;
        private System.Windows.Forms.TextBox txt_ref_doc_id;
        private System.Windows.Forms.Label lbl_doc_date;
        private System.Windows.Forms.DateTimePicker dtp_date;
        private System.Windows.Forms.Label lbl_transaction_type;
        private System.Windows.Forms.TextBox txt_transaction_type;
        private System.Windows.Forms.Label lbl_expected_returned_date;
        private System.Windows.Forms.DateTimePicker dtp_expected_returned_date;
        private System.Windows.Forms.Label lbl_salesperson;
        private System.Windows.Forms.TextBox txt_salesperson;
        private System.Windows.Forms.Label lbl_ship_to;
        private System.Windows.Forms.TextBox txt_ship_to;
        private System.Windows.Forms.Label lbl_currency;
        private System.Windows.Forms.TextBox txt_currency;
        private System.Windows.Forms.Label lbl_sales_period;
        private System.Windows.Forms.TextBox txt_sales_period;
        private System.Windows.Forms.Label lbl_location_group;
        private System.Windows.Forms.TextBox txt_location_group;
        private System.Windows.Forms.Label lbl_location_code;
        private System.Windows.Forms.TextBox txt_location_code;
        private System.Windows.Forms.Label lbl_address;
        private System.Windows.Forms.TextBox txt_address;
        private System.Windows.Forms.Label lbl_cm_reason_code;
        private System.Windows.Forms.TextBox txt_cm_reason_code;
        private System.Windows.Forms.Label lbl_ref_cm_no;
        private System.Windows.Forms.TextBox txt_ref_cm_no;
        private System.Windows.Forms.Label lbl_approved_by;
        private System.Windows.Forms.TextBox txt_approved_by;
        private System.Windows.Forms.Label lbl_approval_date;
        private System.Windows.Forms.TextBox txt_approval_date;
        private System.Windows.Forms.Label lbl_header_remarks;
        private System.Windows.Forms.TextBox txt_header_remarks;
        private System.Windows.Forms.Label lbl_description;
        private System.Windows.Forms.TextBox txt_description;
        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.ToolStripButton btn_new;
        private System.Windows.Forms.ToolStripButton btn_search;
        private System.Windows.Forms.ToolStripButton btn_prev;
        private System.Windows.Forms.ToolStripButton btn_next;
        private System.Windows.Forms.ToolStripButton btn_edit;
        private System.Windows.Forms.ToolStripButton btn_cancel_edit;
        private System.Windows.Forms.Label lbl_title;
    }
}
