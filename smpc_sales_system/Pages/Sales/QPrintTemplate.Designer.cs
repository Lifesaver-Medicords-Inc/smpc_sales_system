
namespace smpc_sales_system.Pages.Sales
{
    partial class QPrintTemplate
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
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(QPrintTemplate));
            this.toolStrip1 = new System.Windows.Forms.ToolStrip();
            this.btn_prev = new System.Windows.Forms.ToolStripButton();
            this.toolStripButton6 = new System.Windows.Forms.ToolStripButton();
            this.Save = new System.Windows.Forms.ToolStripButton();
            this.panel6 = new System.Windows.Forms.Panel();
            this.label1 = new System.Windows.Forms.Label();
            this.panel2 = new System.Windows.Forms.Panel();
            this.dgv_quote = new System.Windows.Forms.DataGridView();
            this.quick_quotes = new System.Windows.Forms.BindingSource(this.components);
            this.dataSet1 = new System.Data.DataSet();
            this.tbl_quick_quotes = new System.Data.DataTable();
            this.id = new System.Data.DataColumn();
            this.based_id = new System.Data.DataColumn();
            this.item_id = new System.Data.DataColumn();
            this.item_name_id = new System.Data.DataColumn();
            this.item_class_id = new System.Data.DataColumn();
            this.qty = new System.Data.DataColumn();
            this.unit_id = new System.Data.DataColumn();
            this.unit_price = new System.Data.DataColumn();
            this.percent_discount = new System.Data.DataColumn();
            this.net_discount = new System.Data.DataColumn();
            this.net_total = new System.Data.DataColumn();
            this.line_total = new System.Data.DataColumn();
            this.item_code = new System.Data.DataColumn();
            this.short_desc = new System.Data.DataColumn();
            this.label17 = new System.Windows.Forms.Label();
            this.label18 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.pnl_header = new System.Windows.Forms.Panel();
            this.txt_sales_exec = new System.Windows.Forms.TextBox();
            this.txt_ship_to = new System.Windows.Forms.TextBox();
            this.txt_branch_name = new System.Windows.Forms.TextBox();
            this.txt_receiver = new System.Windows.Forms.TextBox();
            this.txt_date = new System.Windows.Forms.TextBox();
            this.txt_document_no = new System.Windows.Forms.TextBox();
            this.txt_type = new System.Windows.Forms.TextBox();
            this.label6 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.pnl_footer = new System.Windows.Forms.Panel();
            this.btn_print = new System.Windows.Forms.Button();
            this.rtxt_terms = new System.Windows.Forms.RichTextBox();
            this.label13 = new System.Windows.Forms.Label();
            this.rtxt_exclusions = new System.Windows.Forms.RichTextBox();
            this.label12 = new System.Windows.Forms.Label();
            this.rtxt_inclusion = new System.Windows.Forms.RichTextBox();
            this.label11 = new System.Windows.Forms.Label();
            this.txt_grand_total = new System.Windows.Forms.TextBox();
            this.txt_net_amount_due = new System.Windows.Forms.TextBox();
            this.txt_cash_discount = new System.Windows.Forms.TextBox();
            this.label10 = new System.Windows.Forms.Label();
            this.txt_add_discount = new System.Windows.Forms.TextBox();
            this.label7 = new System.Windows.Forms.Label();
            this.label9 = new System.Windows.Forms.Label();
            this.label8 = new System.Windows.Forms.Label();
            this.img = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.desc = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.qtys = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.unitprice = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.percentdiscount = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.amount = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.idDataGridViewTextBoxColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.basedidDataGridViewTextBoxColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.itemidDataGridViewTextBoxColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.itemnameidDataGridViewTextBoxColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.itemclassidDataGridViewTextBoxColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.unitidDataGridViewTextBoxColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.netdiscountDataGridViewTextBoxColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.nettotalDataGridViewTextBoxColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.itemcodeDataGridViewTextBoxColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.shortdescDataGridViewTextBoxColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.toolStrip1.SuspendLayout();
            this.panel6.SuspendLayout();
            this.panel2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgv_quote)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.quick_quotes)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.dataSet1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.tbl_quick_quotes)).BeginInit();
            this.pnl_header.SuspendLayout();
            this.pnl_footer.SuspendLayout();
            this.SuspendLayout();
            // 
            // toolStrip1
            // 
            this.toolStrip1.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
            this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.btn_prev,
            this.toolStripButton6,
            this.Save});
            this.toolStrip1.Location = new System.Drawing.Point(0, 47);
            this.toolStrip1.Name = "toolStrip1";
            this.toolStrip1.RenderMode = System.Windows.Forms.ToolStripRenderMode.Professional;
            this.toolStrip1.Size = new System.Drawing.Size(790, 25);
            this.toolStrip1.TabIndex = 14;
            this.toolStrip1.Text = "toolStrip1";
            // 
            // btn_prev
            // 
            this.btn_prev.Image = ((System.Drawing.Image)(resources.GetObject("btn_prev.Image")));
            this.btn_prev.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.btn_prev.Name = "btn_prev";
            this.btn_prev.Size = new System.Drawing.Size(52, 22);
            this.btn_prev.Text = "Back";
            // 
            // toolStripButton6
            // 
            this.toolStripButton6.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.toolStripButton6.Image = ((System.Drawing.Image)(resources.GetObject("toolStripButton6.Image")));
            this.toolStripButton6.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButton6.Name = "toolStripButton6";
            this.toolStripButton6.Size = new System.Drawing.Size(23, 22);
            this.toolStripButton6.Text = "toolStripButton6";
            this.toolStripButton6.Visible = false;
            // 
            // Save
            // 
            this.Save.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.Save.Image = ((System.Drawing.Image)(resources.GetObject("Save.Image")));
            this.Save.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.Save.Name = "Save";
            this.Save.Size = new System.Drawing.Size(23, 22);
            this.Save.Text = "Save";
            this.Save.Visible = false;
            // 
            // panel6
            // 
            this.panel6.Controls.Add(this.label1);
            this.panel6.Dock = System.Windows.Forms.DockStyle.Top;
            this.panel6.Location = new System.Drawing.Point(0, 0);
            this.panel6.Name = "panel6";
            this.panel6.Size = new System.Drawing.Size(790, 47);
            this.panel6.TabIndex = 13;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 15.75F);
            this.label1.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.label1.Location = new System.Drawing.Point(18, 10);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(139, 25);
            this.label1.TabIndex = 1;
            this.label1.Text = "Q-Print Temp";
            // 
            // panel2
            // 
            this.panel2.Controls.Add(this.dgv_quote);
            this.panel2.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panel2.Location = new System.Drawing.Point(0, 220);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(790, 246);
            this.panel2.TabIndex = 16;
            // 
            // dgv_quote
            // 
            this.dgv_quote.AllowUserToAddRows = false;
            this.dgv_quote.AllowUserToDeleteRows = false;
            this.dgv_quote.AutoGenerateColumns = false;
            this.dgv_quote.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.dgv_quote.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgv_quote.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.img,
            this.desc,
            this.qtys,
            this.unitprice,
            this.percentdiscount,
            this.amount,
            this.idDataGridViewTextBoxColumn,
            this.basedidDataGridViewTextBoxColumn,
            this.itemidDataGridViewTextBoxColumn,
            this.itemnameidDataGridViewTextBoxColumn,
            this.itemclassidDataGridViewTextBoxColumn,
            this.unitidDataGridViewTextBoxColumn,
            this.netdiscountDataGridViewTextBoxColumn,
            this.nettotalDataGridViewTextBoxColumn,
            this.itemcodeDataGridViewTextBoxColumn,
            this.shortdescDataGridViewTextBoxColumn});
            this.dgv_quote.DataSource = this.quick_quotes;
            this.dgv_quote.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgv_quote.Location = new System.Drawing.Point(0, 0);
            this.dgv_quote.Name = "dgv_quote";
            this.dgv_quote.Size = new System.Drawing.Size(790, 246);
            this.dgv_quote.TabIndex = 0;
            // 
            // quick_quotes
            // 
            this.quick_quotes.DataMember = "tbl_quick_quotes";
            this.quick_quotes.DataSource = this.dataSet1;
            // 
            // dataSet1
            // 
            this.dataSet1.DataSetName = "NewDataSet";
            this.dataSet1.Tables.AddRange(new System.Data.DataTable[] {
            this.tbl_quick_quotes});
            // 
            // tbl_quick_quotes
            // 
            this.tbl_quick_quotes.Columns.AddRange(new System.Data.DataColumn[] {
            this.id,
            this.based_id,
            this.item_id,
            this.item_name_id,
            this.item_class_id,
            this.qty,
            this.unit_id,
            this.unit_price,
            this.percent_discount,
            this.net_discount,
            this.net_total,
            this.line_total,
            this.item_code,
            this.short_desc});
            this.tbl_quick_quotes.TableName = "tbl_quick_quotes";
            // 
            // id
            // 
            this.id.ColumnName = "id";
            // 
            // based_id
            // 
            this.based_id.ColumnName = "based_id";
            // 
            // item_id
            // 
            this.item_id.ColumnName = "item_id";
            // 
            // item_name_id
            // 
            this.item_name_id.ColumnName = "item_name_id";
            // 
            // item_class_id
            // 
            this.item_class_id.ColumnName = "item_class_id";
            // 
            // qty
            // 
            this.qty.ColumnName = "qty";
            // 
            // unit_id
            // 
            this.unit_id.ColumnName = "unit_id";
            // 
            // unit_price
            // 
            this.unit_price.ColumnName = "unit_price";
            // 
            // percent_discount
            // 
            this.percent_discount.ColumnName = "percent_discount";
            // 
            // net_discount
            // 
            this.net_discount.ColumnName = "net_discount";
            // 
            // net_total
            // 
            this.net_total.ColumnName = "net_total";
            // 
            // line_total
            // 
            this.line_total.ColumnName = "line_total";
            // 
            // item_code
            // 
            this.item_code.ColumnName = "item_code";
            // 
            // short_desc
            // 
            this.short_desc.ColumnName = "short_desc";
            // 
            // label17
            // 
            this.label17.AutoSize = true;
            this.label17.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.label17.Location = new System.Drawing.Point(8, 67);
            this.label17.Name = "label17";
            this.label17.Size = new System.Drawing.Size(63, 13);
            this.label17.TabIndex = 246;
            this.label17.Text = "COMPANY:";
            // 
            // label18
            // 
            this.label18.AutoSize = true;
            this.label18.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.label18.Location = new System.Drawing.Point(8, 48);
            this.label18.Name = "label18";
            this.label18.Size = new System.Drawing.Size(39, 13);
            this.label18.TabIndex = 245;
            this.label18.Text = "DATE:";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.label3.Location = new System.Drawing.Point(8, 29);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(96, 13);
            this.label3.TabIndex = 244;
            this.label3.Text = "QUOTATION NO.:";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.label2.Location = new System.Drawing.Point(8, 10);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(38, 13);
            this.label2.TabIndex = 243;
            this.label2.Text = "TYPE:";
            // 
            // pnl_header
            // 
            this.pnl_header.Controls.Add(this.txt_sales_exec);
            this.pnl_header.Controls.Add(this.txt_ship_to);
            this.pnl_header.Controls.Add(this.txt_branch_name);
            this.pnl_header.Controls.Add(this.txt_receiver);
            this.pnl_header.Controls.Add(this.txt_date);
            this.pnl_header.Controls.Add(this.txt_document_no);
            this.pnl_header.Controls.Add(this.txt_type);
            this.pnl_header.Controls.Add(this.label6);
            this.pnl_header.Controls.Add(this.label5);
            this.pnl_header.Controls.Add(this.label4);
            this.pnl_header.Controls.Add(this.label17);
            this.pnl_header.Controls.Add(this.label3);
            this.pnl_header.Controls.Add(this.label18);
            this.pnl_header.Controls.Add(this.label2);
            this.pnl_header.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnl_header.Location = new System.Drawing.Point(0, 72);
            this.pnl_header.Name = "pnl_header";
            this.pnl_header.Size = new System.Drawing.Size(790, 142);
            this.pnl_header.TabIndex = 17;
            // 
            // txt_sales_exec
            // 
            this.txt_sales_exec.Location = new System.Drawing.Point(111, 121);
            this.txt_sales_exec.Name = "txt_sales_exec";
            this.txt_sales_exec.Size = new System.Drawing.Size(90, 20);
            this.txt_sales_exec.TabIndex = 256;
            // 
            // txt_ship_to
            // 
            this.txt_ship_to.Location = new System.Drawing.Point(111, 83);
            this.txt_ship_to.Name = "txt_ship_to";
            this.txt_ship_to.ReadOnly = true;
            this.txt_ship_to.Size = new System.Drawing.Size(90, 20);
            this.txt_ship_to.TabIndex = 255;
            // 
            // txt_branch_name
            // 
            this.txt_branch_name.Location = new System.Drawing.Point(111, 64);
            this.txt_branch_name.Name = "txt_branch_name";
            this.txt_branch_name.ReadOnly = true;
            this.txt_branch_name.Size = new System.Drawing.Size(90, 20);
            this.txt_branch_name.TabIndex = 254;
            // 
            // txt_receiver
            // 
            this.txt_receiver.Location = new System.Drawing.Point(111, 102);
            this.txt_receiver.Name = "txt_receiver";
            this.txt_receiver.Size = new System.Drawing.Size(90, 20);
            this.txt_receiver.TabIndex = 253;
            // 
            // txt_date
            // 
            this.txt_date.Location = new System.Drawing.Point(111, 45);
            this.txt_date.Name = "txt_date";
            this.txt_date.ReadOnly = true;
            this.txt_date.Size = new System.Drawing.Size(90, 20);
            this.txt_date.TabIndex = 252;
            // 
            // txt_document_no
            // 
            this.txt_document_no.Location = new System.Drawing.Point(111, 26);
            this.txt_document_no.Name = "txt_document_no";
            this.txt_document_no.ReadOnly = true;
            this.txt_document_no.Size = new System.Drawing.Size(90, 20);
            this.txt_document_no.TabIndex = 251;
            // 
            // txt_type
            // 
            this.txt_type.Location = new System.Drawing.Point(52, 7);
            this.txt_type.Name = "txt_type";
            this.txt_type.ReadOnly = true;
            this.txt_type.Size = new System.Drawing.Size(149, 20);
            this.txt_type.TabIndex = 250;
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.label6.Location = new System.Drawing.Point(8, 124);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(41, 13);
            this.label6.TabIndex = 249;
            this.label6.Text = "FROM:";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.label5.Location = new System.Drawing.Point(8, 105);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(90, 13);
            this.label5.TabIndex = 248;
            this.label5.Text = "ATTENTION TO:";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.label4.Location = new System.Drawing.Point(8, 86);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(62, 13);
            this.label4.TabIndex = 247;
            this.label4.Text = "ADDRESS:";
            // 
            // pnl_footer
            // 
            this.pnl_footer.Controls.Add(this.btn_print);
            this.pnl_footer.Controls.Add(this.rtxt_terms);
            this.pnl_footer.Controls.Add(this.label13);
            this.pnl_footer.Controls.Add(this.rtxt_exclusions);
            this.pnl_footer.Controls.Add(this.label12);
            this.pnl_footer.Controls.Add(this.rtxt_inclusion);
            this.pnl_footer.Controls.Add(this.label11);
            this.pnl_footer.Controls.Add(this.txt_grand_total);
            this.pnl_footer.Controls.Add(this.txt_net_amount_due);
            this.pnl_footer.Controls.Add(this.txt_cash_discount);
            this.pnl_footer.Controls.Add(this.label10);
            this.pnl_footer.Controls.Add(this.txt_add_discount);
            this.pnl_footer.Controls.Add(this.label7);
            this.pnl_footer.Controls.Add(this.label9);
            this.pnl_footer.Controls.Add(this.label8);
            this.pnl_footer.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.pnl_footer.Location = new System.Drawing.Point(0, 466);
            this.pnl_footer.Name = "pnl_footer";
            this.pnl_footer.Size = new System.Drawing.Size(790, 448);
            this.pnl_footer.TabIndex = 255;
            // 
            // btn_print
            // 
            this.btn_print.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btn_print.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.btn_print.Location = new System.Drawing.Point(701, 422);
            this.btn_print.Name = "btn_print";
            this.btn_print.Size = new System.Drawing.Size(75, 23);
            this.btn_print.TabIndex = 272;
            this.btn_print.Text = "PREVIEW";
            this.btn_print.UseVisualStyleBackColor = true;
            this.btn_print.Click += new System.EventHandler(this.btn_print_Click);
            // 
            // rtxt_terms
            // 
            this.rtxt_terms.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(217)))), ((int)(((byte)(210)))), ((int)(((byte)(233)))));
            this.rtxt_terms.Location = new System.Drawing.Point(23, 294);
            this.rtxt_terms.Name = "rtxt_terms";
            this.rtxt_terms.Size = new System.Drawing.Size(753, 124);
            this.rtxt_terms.TabIndex = 270;
            this.rtxt_terms.Text = resources.GetString("rtxt_terms.Text");
            // 
            // label13
            // 
            this.label13.AutoSize = true;
            this.label13.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label13.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.label13.Location = new System.Drawing.Point(20, 278);
            this.label13.Name = "label13";
            this.label13.Size = new System.Drawing.Size(165, 13);
            this.label13.TabIndex = 269;
            this.label13.Text = "TERMS AND CONDITIONS:\t";
            // 
            // rtxt_exclusions
            // 
            this.rtxt_exclusions.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(217)))), ((int)(((byte)(210)))), ((int)(((byte)(233)))));
            this.rtxt_exclusions.Location = new System.Drawing.Point(23, 202);
            this.rtxt_exclusions.Name = "rtxt_exclusions";
            this.rtxt_exclusions.Size = new System.Drawing.Size(753, 72);
            this.rtxt_exclusions.TabIndex = 268;
            this.rtxt_exclusions.Text = resources.GetString("rtxt_exclusions.Text");
            // 
            // label12
            // 
            this.label12.AutoSize = true;
            this.label12.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label12.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.label12.Location = new System.Drawing.Point(20, 186);
            this.label12.Name = "label12";
            this.label12.Size = new System.Drawing.Size(89, 13);
            this.label12.TabIndex = 267;
            this.label12.Text = "EXCLUSIONS:";
            // 
            // rtxt_inclusion
            // 
            this.rtxt_inclusion.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(217)))), ((int)(((byte)(210)))), ((int)(((byte)(233)))));
            this.rtxt_inclusion.Location = new System.Drawing.Point(23, 111);
            this.rtxt_inclusion.Name = "rtxt_inclusion";
            this.rtxt_inclusion.Size = new System.Drawing.Size(753, 72);
            this.rtxt_inclusion.TabIndex = 266;
            this.rtxt_inclusion.Text = resources.GetString("rtxt_inclusion.Text");
            // 
            // label11
            // 
            this.label11.AutoSize = true;
            this.label11.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label11.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.label11.Location = new System.Drawing.Point(20, 95);
            this.label11.Name = "label11";
            this.label11.Size = new System.Drawing.Size(86, 13);
            this.label11.TabIndex = 264;
            this.label11.Text = "INCLUSIONS:";
            // 
            // txt_grand_total
            // 
            this.txt_grand_total.Location = new System.Drawing.Point(686, 75);
            this.txt_grand_total.Name = "txt_grand_total";
            this.txt_grand_total.Size = new System.Drawing.Size(90, 20);
            this.txt_grand_total.TabIndex = 263;
            // 
            // txt_net_amount_due
            // 
            this.txt_net_amount_due.Location = new System.Drawing.Point(686, 19);
            this.txt_net_amount_due.Name = "txt_net_amount_due";
            this.txt_net_amount_due.ReadOnly = true;
            this.txt_net_amount_due.Size = new System.Drawing.Size(90, 20);
            this.txt_net_amount_due.TabIndex = 260;
            // 
            // txt_cash_discount
            // 
            this.txt_cash_discount.Location = new System.Drawing.Point(686, 57);
            this.txt_cash_discount.Name = "txt_cash_discount";
            this.txt_cash_discount.ReadOnly = true;
            this.txt_cash_discount.Size = new System.Drawing.Size(90, 20);
            this.txt_cash_discount.TabIndex = 262;
            // 
            // label10
            // 
            this.label10.AutoSize = true;
            this.label10.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label10.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.label10.Location = new System.Drawing.Point(563, 22);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(120, 13);
            this.label10.TabIndex = 256;
            this.label10.Text = "SUB-TOTAL          :";
            // 
            // txt_add_discount
            // 
            this.txt_add_discount.Location = new System.Drawing.Point(686, 38);
            this.txt_add_discount.Name = "txt_add_discount";
            this.txt_add_discount.Size = new System.Drawing.Size(90, 20);
            this.txt_add_discount.TabIndex = 261;
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label7.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.label7.Location = new System.Drawing.Point(563, 78);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(119, 13);
            this.label7.TabIndex = 259;
            this.label7.Text = "GRAND TOTAL     :";
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label9.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.label9.Location = new System.Drawing.Point(563, 60);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(120, 13);
            this.label9.TabIndex = 258;
            this.label9.Text = "CASH DISCOUNT  :";
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label8.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.label8.Location = new System.Drawing.Point(563, 41);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(121, 13);
            this.label8.TabIndex = 257;
            this.label8.Text = "ADD. DISCOUNT   :";
            // 
            // img
            // 
            this.img.HeaderText = "IMAGE";
            this.img.Name = "img";
            this.img.Resizable = System.Windows.Forms.DataGridViewTriState.True;
            // 
            // desc
            // 
            this.desc.DataPropertyName = "short_desc";
            this.desc.HeaderText = "ITEM DESCRIPTION";
            this.desc.Name = "desc";
            // 
            // qtys
            // 
            this.qtys.DataPropertyName = "qty";
            this.qtys.HeaderText = "QTY";
            this.qtys.Name = "qtys";
            // 
            // unitprice
            // 
            this.unitprice.DataPropertyName = "unit_price";
            this.unitprice.HeaderText = "UNIT PRICE";
            this.unitprice.Name = "unitprice";
            // 
            // percentdiscount
            // 
            this.percentdiscount.DataPropertyName = "percent_discount";
            this.percentdiscount.HeaderText = "DISCOUNT";
            this.percentdiscount.Name = "percentdiscount";
            // 
            // amount
            // 
            this.amount.DataPropertyName = "line_total";
            this.amount.HeaderText = "AMOUNT";
            this.amount.Name = "amount";
            // 
            // idDataGridViewTextBoxColumn
            // 
            this.idDataGridViewTextBoxColumn.DataPropertyName = "id";
            this.idDataGridViewTextBoxColumn.HeaderText = "id";
            this.idDataGridViewTextBoxColumn.Name = "idDataGridViewTextBoxColumn";
            this.idDataGridViewTextBoxColumn.Visible = false;
            // 
            // basedidDataGridViewTextBoxColumn
            // 
            this.basedidDataGridViewTextBoxColumn.DataPropertyName = "based_id";
            this.basedidDataGridViewTextBoxColumn.HeaderText = "based_id";
            this.basedidDataGridViewTextBoxColumn.Name = "basedidDataGridViewTextBoxColumn";
            this.basedidDataGridViewTextBoxColumn.Visible = false;
            // 
            // itemidDataGridViewTextBoxColumn
            // 
            this.itemidDataGridViewTextBoxColumn.DataPropertyName = "item_id";
            this.itemidDataGridViewTextBoxColumn.HeaderText = "item_id";
            this.itemidDataGridViewTextBoxColumn.Name = "itemidDataGridViewTextBoxColumn";
            this.itemidDataGridViewTextBoxColumn.Visible = false;
            // 
            // itemnameidDataGridViewTextBoxColumn
            // 
            this.itemnameidDataGridViewTextBoxColumn.DataPropertyName = "item_name_id";
            this.itemnameidDataGridViewTextBoxColumn.HeaderText = "item_name_id";
            this.itemnameidDataGridViewTextBoxColumn.Name = "itemnameidDataGridViewTextBoxColumn";
            this.itemnameidDataGridViewTextBoxColumn.Visible = false;
            // 
            // itemclassidDataGridViewTextBoxColumn
            // 
            this.itemclassidDataGridViewTextBoxColumn.DataPropertyName = "item_class_id";
            this.itemclassidDataGridViewTextBoxColumn.HeaderText = "item_class_id";
            this.itemclassidDataGridViewTextBoxColumn.Name = "itemclassidDataGridViewTextBoxColumn";
            this.itemclassidDataGridViewTextBoxColumn.Visible = false;
            // 
            // unitidDataGridViewTextBoxColumn
            // 
            this.unitidDataGridViewTextBoxColumn.DataPropertyName = "unit_id";
            this.unitidDataGridViewTextBoxColumn.HeaderText = "unit_id";
            this.unitidDataGridViewTextBoxColumn.Name = "unitidDataGridViewTextBoxColumn";
            this.unitidDataGridViewTextBoxColumn.Visible = false;
            // 
            // netdiscountDataGridViewTextBoxColumn
            // 
            this.netdiscountDataGridViewTextBoxColumn.DataPropertyName = "net_discount";
            this.netdiscountDataGridViewTextBoxColumn.HeaderText = "net_discount";
            this.netdiscountDataGridViewTextBoxColumn.Name = "netdiscountDataGridViewTextBoxColumn";
            this.netdiscountDataGridViewTextBoxColumn.Visible = false;
            // 
            // nettotalDataGridViewTextBoxColumn
            // 
            this.nettotalDataGridViewTextBoxColumn.DataPropertyName = "net_total";
            this.nettotalDataGridViewTextBoxColumn.HeaderText = "net_total";
            this.nettotalDataGridViewTextBoxColumn.Name = "nettotalDataGridViewTextBoxColumn";
            this.nettotalDataGridViewTextBoxColumn.Visible = false;
            // 
            // itemcodeDataGridViewTextBoxColumn
            // 
            this.itemcodeDataGridViewTextBoxColumn.DataPropertyName = "item_code";
            this.itemcodeDataGridViewTextBoxColumn.HeaderText = "item_code";
            this.itemcodeDataGridViewTextBoxColumn.Name = "itemcodeDataGridViewTextBoxColumn";
            this.itemcodeDataGridViewTextBoxColumn.Visible = false;
            // 
            // shortdescDataGridViewTextBoxColumn
            // 
            this.shortdescDataGridViewTextBoxColumn.DataPropertyName = "short_desc";
            this.shortdescDataGridViewTextBoxColumn.HeaderText = "short_desc";
            this.shortdescDataGridViewTextBoxColumn.Name = "shortdescDataGridViewTextBoxColumn";
            this.shortdescDataGridViewTextBoxColumn.Visible = false;
            // 
            // QPrintTemplate
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.pnl_header);
            this.Controls.Add(this.panel2);
            this.Controls.Add(this.toolStrip1);
            this.Controls.Add(this.panel6);
            this.Controls.Add(this.pnl_footer);
            this.Name = "QPrintTemplate";
            this.Size = new System.Drawing.Size(790, 914);
            this.Load += new System.EventHandler(this.QPrintTemplate_Load);
            this.toolStrip1.ResumeLayout(false);
            this.toolStrip1.PerformLayout();
            this.panel6.ResumeLayout(false);
            this.panel6.PerformLayout();
            this.panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dgv_quote)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.quick_quotes)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.dataSet1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.tbl_quick_quotes)).EndInit();
            this.pnl_header.ResumeLayout(false);
            this.pnl_header.PerformLayout();
            this.pnl_footer.ResumeLayout(false);
            this.pnl_footer.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.ToolStripButton btn_prev;
        private System.Windows.Forms.ToolStripButton toolStripButton6;
        private System.Windows.Forms.ToolStripButton Save;
        private System.Windows.Forms.Panel panel6;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Panel panel2;
        private System.Windows.Forms.Label label17;
        private System.Windows.Forms.Label label18;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Panel pnl_header;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TextBox txt_document_no;
        private System.Windows.Forms.TextBox txt_type;
        private System.Windows.Forms.DataGridView dgv_quote;
        private System.Windows.Forms.TextBox txt_sales_exec;
        private System.Windows.Forms.TextBox txt_ship_to;
        private System.Windows.Forms.TextBox txt_branch_name;
        private System.Windows.Forms.TextBox txt_receiver;
        private System.Windows.Forms.TextBox txt_date;
        private System.Data.DataSet dataSet1;
        private System.Data.DataTable tbl_quick_quotes;
        private System.Data.DataColumn id;
        private System.Data.DataColumn based_id;
        private System.Data.DataColumn item_id;
        private System.Data.DataColumn item_name_id;
        private System.Data.DataColumn item_class_id;
        private System.Data.DataColumn qty;
        private System.Data.DataColumn unit_id;
        private System.Data.DataColumn unit_price;
        private System.Data.DataColumn percent_discount;
        private System.Data.DataColumn net_discount;
        private System.Data.DataColumn net_total;
        private System.Data.DataColumn line_total;
        private System.Data.DataColumn item_code;
        private System.Data.DataColumn short_desc;
        private System.Windows.Forms.BindingSource quick_quotes;
        private System.Windows.Forms.Panel pnl_footer;
        private System.Windows.Forms.RichTextBox rtxt_terms;
        private System.Windows.Forms.Label label13;
        private System.Windows.Forms.RichTextBox rtxt_exclusions;
        private System.Windows.Forms.Label label12;
        private System.Windows.Forms.RichTextBox rtxt_inclusion;
        private System.Windows.Forms.Label label11;
        private System.Windows.Forms.TextBox txt_grand_total;
        private System.Windows.Forms.TextBox txt_net_amount_due;
        private System.Windows.Forms.TextBox txt_cash_discount;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.TextBox txt_add_discount;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.Button btn_print;
        private System.Windows.Forms.DataGridViewTextBoxColumn img;
        private System.Windows.Forms.DataGridViewTextBoxColumn desc;
        private System.Windows.Forms.DataGridViewTextBoxColumn qtys;
        private System.Windows.Forms.DataGridViewTextBoxColumn unitprice;
        private System.Windows.Forms.DataGridViewTextBoxColumn percentdiscount;
        private System.Windows.Forms.DataGridViewTextBoxColumn amount;
        private System.Windows.Forms.DataGridViewTextBoxColumn idDataGridViewTextBoxColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn basedidDataGridViewTextBoxColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn itemidDataGridViewTextBoxColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn itemnameidDataGridViewTextBoxColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn itemclassidDataGridViewTextBoxColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn unitidDataGridViewTextBoxColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn netdiscountDataGridViewTextBoxColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn nettotalDataGridViewTextBoxColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn itemcodeDataGridViewTextBoxColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn shortdescDataGridViewTextBoxColumn;
    }
}
