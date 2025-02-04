
namespace smpc_sales_system.Pages.Sales
{
    partial class Opportunities
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
            this.label1 = new System.Windows.Forms.Label();
            this.panel2 = new System.Windows.Forms.Panel();
            this.dgv_sales_opportunities = new System.Windows.Forms.DataGridView();
            this.tag = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.document_no = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.customer_name = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.project_name = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.date = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.client_req = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.value = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.last_update = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.stage = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.status = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.special_deal = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.panel1.SuspendLayout();
            this.panel2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgv_sales_opportunities)).BeginInit();
            this.SuspendLayout();
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.label1);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Top;
            this.panel1.Location = new System.Drawing.Point(0, 0);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(1143, 64);
            this.panel1.TabIndex = 0;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 15.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(3, 19);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(194, 25);
            this.label1.TabIndex = 1;
            this.label1.Text = "OPPORTUNITIES";
            // 
            // panel2
            // 
            this.panel2.Controls.Add(this.dgv_sales_opportunities);
            this.panel2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel2.Location = new System.Drawing.Point(0, 64);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(1143, 535);
            this.panel2.TabIndex = 1;
            // 
            // dgv_sales_opportunities
            // 
            this.dgv_sales_opportunities.AllowUserToAddRows = false;
            this.dgv_sales_opportunities.AllowUserToDeleteRows = false;
            this.dgv_sales_opportunities.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgv_sales_opportunities.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.tag,
            this.document_no,
            this.customer_name,
            this.project_name,
            this.date,
            this.client_req,
            this.value,
            this.last_update,
            this.stage,
            this.status,
            this.special_deal});
            this.dgv_sales_opportunities.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgv_sales_opportunities.Location = new System.Drawing.Point(0, 0);
            this.dgv_sales_opportunities.Name = "dgv_sales_opportunities";
            this.dgv_sales_opportunities.Size = new System.Drawing.Size(1143, 535);
            this.dgv_sales_opportunities.TabIndex = 0;

            this.dgv_sales_opportunities.CellDoubleClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.dgv_sales_opportunities_CellDoubleClick);

            // 
            // tag
            // 
            this.tag.DataPropertyName = "tag";
            this.tag.HeaderText = "TAG";
            this.tag.Name = "tag";
            // 
            // document_no
            // 
            this.document_no.DataPropertyName = "document_no";
            this.document_no.HeaderText = "QUOTE REF.";
            this.document_no.Name = "document_no";
            this.document_no.ReadOnly = true;
            // 
            // customer_name
            // 
            this.customer_name.DataPropertyName = "customer_name";
            this.customer_name.HeaderText = "COMPANY NAME";
            this.customer_name.Name = "customer_name";
            this.customer_name.ReadOnly = true;
            // 
            // project_name
            // 
            this.project_name.HeaderText = "PROJECT NAME";
            this.project_name.Name = "project_name";
            this.project_name.ReadOnly = true;
            // 
            // date
            // 
            this.date.DataPropertyName = "date";
            this.date.HeaderText = "INQUIRY DATE";
            this.date.Name = "date";
            this.date.ReadOnly = true;
            // 
            // client_req
            // 
            this.client_req.HeaderText = "CLIENT REQ";
            this.client_req.Name = "client_req";
            // 
            // value
            // 
            this.value.HeaderText = "VALUE";
            this.value.Name = "value";
            this.value.ReadOnly = true;
            // 
            // last_update
            // 
            this.last_update.HeaderText = "LAST UPDATE";
            this.last_update.Name = "last_update";
            // 
            // stage
            // 
            this.stage.HeaderText = "STAGE";
            this.stage.Name = "stage";
            // 
            // status
            // 
            this.status.HeaderText = "STATUS";
            this.status.Name = "status";
            // 
            // special_deal
            // 
            this.special_deal.HeaderText = "SPECIAL DEAL/NOTES";
            this.special_deal.Name = "special_deal";
            // 
            // Opportunities
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.panel2);
            this.Controls.Add(this.panel1);
            this.Name = "Opportunities";
            this.Size = new System.Drawing.Size(1143, 599);
            this.Load += new System.EventHandler(this.Opportunities_Load);
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dgv_sales_opportunities)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Panel panel2;
        private System.Windows.Forms.DataGridView dgv_sales_opportunities;
        private System.Windows.Forms.DataGridViewTextBoxColumn tag;
        private System.Windows.Forms.DataGridViewTextBoxColumn document_no;
        private System.Windows.Forms.DataGridViewTextBoxColumn customer_name;
        private System.Windows.Forms.DataGridViewTextBoxColumn project_name;
        private System.Windows.Forms.DataGridViewTextBoxColumn date;
        private System.Windows.Forms.DataGridViewTextBoxColumn client_req;
        private System.Windows.Forms.DataGridViewTextBoxColumn value;
        private System.Windows.Forms.DataGridViewTextBoxColumn last_update;
        private System.Windows.Forms.DataGridViewTextBoxColumn stage;
        private System.Windows.Forms.DataGridViewTextBoxColumn status;
        private System.Windows.Forms.DataGridViewTextBoxColumn special_deal;
    }
}
