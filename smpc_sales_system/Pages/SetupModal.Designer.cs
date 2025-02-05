
namespace smpc_sales_app.Pages
{
    partial class SetupModal
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SetupModal));
            this.panel1 = new System.Windows.Forms.Panel();
            this.lbl_setup_title = new System.Windows.Forms.Label();
            this.dgv_application_setup = new System.Windows.Forms.DataGridView();
            this.bs_quotation_list = new System.Windows.Forms.BindingSource(this.components);
            this.ds_quotation_list = new System.Data.DataSet();
            this.dataTable1 = new System.Data.DataTable();
            this.document_no = new System.Data.DataColumn();
            this.dataColumn1 = new System.Data.DataColumn();
            this.dataTable2 = new System.Data.DataTable();
            this.panel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgv_application_setup)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.bs_quotation_list)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.ds_quotation_list)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.dataTable1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.dataTable2)).BeginInit();
            this.SuspendLayout();
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.lbl_setup_title);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Top;
            this.panel1.Location = new System.Drawing.Point(0, 0);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(400, 47);
            this.panel1.TabIndex = 0;
            // 
            // lbl_setup_title
            // 
            this.lbl_setup_title.AutoSize = true;
            this.lbl_setup_title.Location = new System.Drawing.Point(131, 9);
            this.lbl_setup_title.Name = "lbl_setup_title";
            this.lbl_setup_title.Size = new System.Drawing.Size(134, 13);
            this.lbl_setup_title.TabIndex = 0;
            this.lbl_setup_title.Text = "SALES QUOTATION LIST";
            // 
            // dgv_application_setup
            // 
            this.dgv_application_setup.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgv_application_setup.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgv_application_setup.Location = new System.Drawing.Point(0, 47);
            this.dgv_application_setup.MultiSelect = false;
            this.dgv_application_setup.Name = "dgv_application_setup";
            this.dgv_application_setup.ReadOnly = true;
            this.dgv_application_setup.Size = new System.Drawing.Size(400, 459);
            this.dgv_application_setup.TabIndex = 2;
            this.dgv_application_setup.CellContentDoubleClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.dgv_application_setup_CellContentDoubleClick);
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
            // SetupModal
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSize = true;
            this.ClientSize = new System.Drawing.Size(400, 506);
            this.Controls.Add(this.dgv_application_setup);
            this.Controls.Add(this.panel1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "SetupModal";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Setup";
            this.Load += new System.EventHandler(this.SetupModal_Load);
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgv_application_setup)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.bs_quotation_list)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.ds_quotation_list)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.dataTable1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.dataTable2)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.DataGridView dgv_application_setup;
        private System.Windows.Forms.Label lbl_setup_title;
        private System.Windows.Forms.BindingSource bs_quotation_list;
        private System.Data.DataSet ds_quotation_list;
        private System.Data.DataTable dataTable1;
        private System.Data.DataColumn document_no;
        private System.Data.DataColumn dataColumn1;
        private System.Data.DataTable dataTable2;
    }
}