
namespace smpc_sales_system.Pages
{
    partial class SearchOrder
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
            this.components = new System.ComponentModel.Container();
            this.dgv_application_setup = new System.Windows.Forms.DataGridView();
            this.d_document_no = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.d_quote_ref = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.d_quotation = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.d_status = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.panel1 = new System.Windows.Forms.Panel();
            this.label1 = new System.Windows.Forms.Label();
            this.txt_search = new System.Windows.Forms.TextBox();
            this.lbl_setup_title = new System.Windows.Forms.Label();
            this.bs_quotation_list = new System.Windows.Forms.BindingSource(this.components);
            this.ds_quotation_list = new System.Data.DataSet();
            this.dataTable1 = new System.Data.DataTable();
            this.document_no = new System.Data.DataColumn();
            this.dataColumn1 = new System.Data.DataColumn();
            this.dataTable2 = new System.Data.DataTable();
            ((System.ComponentModel.ISupportInitialize)(this.dgv_application_setup)).BeginInit();
            this.panel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.bs_quotation_list)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.ds_quotation_list)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.dataTable1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.dataTable2)).BeginInit();
            this.SuspendLayout();
            // 
            // dgv_application_setup
            // 
            this.dgv_application_setup.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgv_application_setup.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.d_document_no,
            this.d_quote_ref,
            this.d_quotation,
            this.d_status});
            this.dgv_application_setup.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgv_application_setup.Location = new System.Drawing.Point(0, 47);
            this.dgv_application_setup.MultiSelect = false;
            this.dgv_application_setup.Name = "dgv_application_setup";
            this.dgv_application_setup.ReadOnly = true;
            this.dgv_application_setup.Size = new System.Drawing.Size(478, 403);
            this.dgv_application_setup.TabIndex = 4;
            this.dgv_application_setup.CellContentDoubleClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.dgv_application_setup_CellContentDoubleClick_1);
            // 
            // d_document_no
            // 
            this.d_document_no.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.d_document_no.DataPropertyName = "doc";
            this.d_document_no.HeaderText = "DOCUMENT NO.";
            this.d_document_no.Name = "d_document_no";
            this.d_document_no.ReadOnly = true;
            // 
            // d_quote_ref
            // 
            this.d_quote_ref.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.d_quote_ref.DataPropertyName = "document_no";
            this.d_quote_ref.HeaderText = "QUOTE REF";
            this.d_quote_ref.Name = "d_quote_ref";
            this.d_quote_ref.ReadOnly = true;
            // 
            // d_quotation
            // 
            this.d_quotation.DataPropertyName = "quotation_id";
            this.d_quotation.HeaderText = "QUOTATION ID";
            this.d_quotation.Name = "d_quotation";
            this.d_quotation.ReadOnly = true;
            // 
            // d_status
            // 
            this.d_status.DataPropertyName = "status";
            this.d_status.HeaderText = "STATUS";
            this.d_status.Name = "d_status";
            this.d_status.ReadOnly = true;
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.label1);
            this.panel1.Controls.Add(this.txt_search);
            this.panel1.Controls.Add(this.lbl_setup_title);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Top;
            this.panel1.Location = new System.Drawing.Point(0, 0);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(478, 47);
            this.panel1.TabIndex = 3;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(306, 16);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(44, 13);
            this.label1.TabIndex = 2;
            this.label1.Text = "Search:";
            // 
            // txt_search
            // 
            this.txt_search.Location = new System.Drawing.Point(351, 12);
            this.txt_search.Name = "txt_search";
            this.txt_search.Size = new System.Drawing.Size(115, 20);
            this.txt_search.TabIndex = 1;
            this.txt_search.TextChanged += new System.EventHandler(this.txt_search_TextChanged);
            // 
            // lbl_setup_title
            // 
            this.lbl_setup_title.AutoSize = true;
            this.lbl_setup_title.Location = new System.Drawing.Point(12, 15);
            this.lbl_setup_title.Name = "lbl_setup_title";
            this.lbl_setup_title.Size = new System.Drawing.Size(109, 13);
            this.lbl_setup_title.TabIndex = 0;
            this.lbl_setup_title.Text = "SALES ORDER LIST";
            // 
            // bs_quotation_list
            // 
            this.bs_quotation_list.DataSource = this.ds_quotation_list;
            this.bs_quotation_list.Position = 0;
            // 
            // ds_quotation_list
            // 
            this.ds_quotation_list.DataSetName = "NewDataSet";
            this.ds_quotation_list.Tables.AddRange(new System.Data.DataTable[] {
            this.dataTable1,
            this.dataTable2});
            // 
            // dataTable1
            // 
            this.dataTable1.Columns.AddRange(new System.Data.DataColumn[] {
            this.document_no,
            this.dataColumn1});
            this.dataTable1.TableName = "Quotation List";
            // 
            // document_no
            // 
            this.document_no.ColumnName = "document_no";
            // 
            // dataColumn1
            // 
            this.dataColumn1.ColumnName = "customer_name";
            // 
            // dataTable2
            // 
            this.dataTable2.TableName = "Table2";
            // 
            // SearchOrder
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(478, 450);
            this.Controls.Add(this.dgv_application_setup);
            this.Controls.Add(this.panel1);
            this.Name = "SearchOrder";
            this.Text = "SearchOrder";
            this.Load += new System.EventHandler(this.SearchOrder_Load);
            ((System.ComponentModel.ISupportInitialize)(this.dgv_application_setup)).EndInit();
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.bs_quotation_list)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.ds_quotation_list)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.dataTable1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.dataTable2)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.DataGridView dgv_application_setup;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox txt_search;
        private System.Windows.Forms.Label lbl_setup_title;
        private System.Windows.Forms.BindingSource bs_quotation_list;
        private System.Data.DataSet ds_quotation_list;
        private System.Data.DataTable dataTable1;
        private System.Data.DataColumn document_no;
        private System.Data.DataColumn dataColumn1;
        private System.Data.DataTable dataTable2;
        private System.Windows.Forms.DataGridViewTextBoxColumn d_document_no;
        private System.Windows.Forms.DataGridViewTextBoxColumn d_quote_ref;
        private System.Windows.Forms.DataGridViewTextBoxColumn d_quotation;
        private System.Windows.Forms.DataGridViewTextBoxColumn d_status;
    }
}