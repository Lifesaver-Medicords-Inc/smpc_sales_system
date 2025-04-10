
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
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle8 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle2 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle3 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle4 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle5 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle6 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle7 = new System.Windows.Forms.DataGridViewCellStyle();
            this.panel1 = new System.Windows.Forms.Panel();
            this.panel3 = new System.Windows.Forms.Panel();
            this.txt_search = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.panel2 = new System.Windows.Forms.Panel();
            this.dgv_sales_opportunities = new System.Windows.Forms.DataGridView();
            this.tag = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.prospectref = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.final_ref_no = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.branch_name = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.project_name = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.date = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.client_req = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.value = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.last_update = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.stage = new System.Windows.Forms.DataGridViewComboBoxColumn();
            this.status = new System.Windows.Forms.DataGridViewComboBoxColumn();
            this.special_deal = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Opportunity_id = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.version_no = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.document_no = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.panel1.SuspendLayout();
            this.panel3.SuspendLayout();
            this.panel2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgv_sales_opportunities)).BeginInit();
            this.SuspendLayout();
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.panel3);
            this.panel1.Controls.Add(this.label1);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Top;
            this.panel1.Location = new System.Drawing.Point(0, 0);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(1143, 70);
            this.panel1.TabIndex = 0;
            this.panel1.Paint += new System.Windows.Forms.PaintEventHandler(this.panel1_Paint);
            // 
            // panel3
            // 
            this.panel3.Controls.Add(this.txt_search);
            this.panel3.Controls.Add(this.label2);
            this.panel3.Dock = System.Windows.Forms.DockStyle.Right;
            this.panel3.Location = new System.Drawing.Point(872, 0);
            this.panel3.Name = "panel3";
            this.panel3.Size = new System.Drawing.Size(271, 70);
            this.panel3.TabIndex = 4;
            // 
            // txt_search
            // 
            this.txt_search.Location = new System.Drawing.Point(48, 25);
            this.txt_search.Name = "txt_search";
            this.txt_search.Size = new System.Drawing.Size(209, 20);
            this.txt_search.TabIndex = 2;
            this.txt_search.TextChanged += new System.EventHandler(this.txt_search_TextChanged);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(7, 28);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(41, 13);
            this.label2.TabIndex = 3;
            this.label2.Text = "Search";
            this.label2.Click += new System.EventHandler(this.label2_Click);
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
            this.label1.Click += new System.EventHandler(this.label1_Click);
            // 
            // panel2
            // 
            this.panel2.Controls.Add(this.dgv_sales_opportunities);
            this.panel2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel2.Location = new System.Drawing.Point(0, 70);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(1143, 716);
            this.panel2.TabIndex = 1;
            this.panel2.Paint += new System.Windows.Forms.PaintEventHandler(this.panel2_Paint);
            // 
            // dgv_sales_opportunities
            // 
            this.dgv_sales_opportunities.AllowUserToAddRows = false;
            this.dgv_sales_opportunities.AllowUserToDeleteRows = false;
            this.dgv_sales_opportunities.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgv_sales_opportunities.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.tag,
            this.prospectref,
            this.final_ref_no,
            this.branch_name,
            this.project_name,
            this.date,
            this.client_req,
            this.value,
            this.last_update,
            this.stage,
            this.status,
            this.special_deal,
            this.Opportunity_id,
            this.version_no,
            this.document_no});
            dataGridViewCellStyle8.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
            dataGridViewCellStyle8.BackColor = System.Drawing.SystemColors.Window;
            dataGridViewCellStyle8.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            dataGridViewCellStyle8.ForeColor = System.Drawing.SystemColors.ControlText;
            dataGridViewCellStyle8.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle8.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle8.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            this.dgv_sales_opportunities.DefaultCellStyle = dataGridViewCellStyle8;
            this.dgv_sales_opportunities.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgv_sales_opportunities.Location = new System.Drawing.Point(0, 0);
            this.dgv_sales_opportunities.Name = "dgv_sales_opportunities";
            this.dgv_sales_opportunities.Size = new System.Drawing.Size(1143, 716);
            this.dgv_sales_opportunities.TabIndex = 0;
            this.dgv_sales_opportunities.CellBeginEdit += new System.Windows.Forms.DataGridViewCellCancelEventHandler(this.dgv_sales_opportunities_CellBeginEdit);
            this.dgv_sales_opportunities.CellContentClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.dgv_sales_opportunities_CellContentClick);
            this.dgv_sales_opportunities.CellDoubleClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.dgv_sales_opportunities_CellDoubleClick);
            this.dgv_sales_opportunities.CellEndEdit += new System.Windows.Forms.DataGridViewCellEventHandler(this.dgv_sales_opportunities_CellEndEdit);
            this.dgv_sales_opportunities.CellFormatting += new System.Windows.Forms.DataGridViewCellFormattingEventHandler(this.dgv_sales_opportunities_CellFormatting);
            // 
            // tag
            // 
            this.tag.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.tag.DataPropertyName = "tag";
            this.tag.FillWeight = 55.1813F;
            this.tag.HeaderText = "TAG";
            this.tag.Name = "tag";
            // 
            // prospectref
            // 
            this.prospectref.DataPropertyName = "combined_column";
            dataGridViewCellStyle1.BackColor = System.Drawing.Color.Gainsboro;
            dataGridViewCellStyle1.ForeColor = System.Drawing.Color.Teal;
            this.prospectref.DefaultCellStyle = dataGridViewCellStyle1;
            this.prospectref.HeaderText = "PROSPECT REF.";
            this.prospectref.Name = "prospectref";
            this.prospectref.ReadOnly = true;
            this.prospectref.Width = 80;
            // 
            // final_ref_no
            // 
            this.final_ref_no.DataPropertyName = "is_finalized";
            dataGridViewCellStyle2.BackColor = System.Drawing.Color.Gainsboro;
            dataGridViewCellStyle2.ForeColor = System.Drawing.Color.Teal;
            this.final_ref_no.DefaultCellStyle = dataGridViewCellStyle2;
            this.final_ref_no.FillWeight = 629.9146F;
            this.final_ref_no.HeaderText = "QUOTE REF.";
            this.final_ref_no.Name = "final_ref_no";
            this.final_ref_no.ReadOnly = true;
            this.final_ref_no.Width = 80;
            // 
            // branch_name
            // 
            this.branch_name.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.branch_name.DataPropertyName = "branch_name";
            dataGridViewCellStyle3.BackColor = System.Drawing.Color.Gainsboro;
            this.branch_name.DefaultCellStyle = dataGridViewCellStyle3;
            this.branch_name.FillWeight = 44.14505F;
            this.branch_name.HeaderText = "COMPANY NAME";
            this.branch_name.Name = "branch_name";
            this.branch_name.ReadOnly = true;
            // 
            // project_name
            // 
            this.project_name.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.project_name.DataPropertyName = "project_name";
            dataGridViewCellStyle4.BackColor = System.Drawing.Color.Gainsboro;
            this.project_name.DefaultCellStyle = dataGridViewCellStyle4;
            this.project_name.FillWeight = 44.14505F;
            this.project_name.HeaderText = "PROJECT NAME";
            this.project_name.Name = "project_name";
            this.project_name.ReadOnly = true;
            // 
            // date
            // 
            this.date.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.date.DataPropertyName = "date";
            dataGridViewCellStyle5.BackColor = System.Drawing.Color.Gainsboro;
            this.date.DefaultCellStyle = dataGridViewCellStyle5;
            this.date.FillWeight = 44.14505F;
            this.date.HeaderText = "INQUIRY DATE";
            this.date.Name = "date";
            this.date.ReadOnly = true;
            // 
            // client_req
            // 
            this.client_req.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.client_req.DataPropertyName = "client_req";
            this.client_req.FillWeight = 44.14505F;
            this.client_req.HeaderText = "CLIENT REQ";
            this.client_req.Name = "client_req";
            // 
            // value
            // 
            this.value.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.value.DataPropertyName = "total_amount_due";
            dataGridViewCellStyle6.BackColor = System.Drawing.Color.Gainsboro;
            dataGridViewCellStyle6.Format = "C2";
            dataGridViewCellStyle6.NullValue = null;
            this.value.DefaultCellStyle = dataGridViewCellStyle6;
            this.value.FillWeight = 44.14505F;
            this.value.HeaderText = "VALUE";
            this.value.Name = "value";
            this.value.ReadOnly = true;
            // 
            // last_update
            // 
            this.last_update.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.last_update.DataPropertyName = "last_update";
            dataGridViewCellStyle7.Format = "d";
            dataGridViewCellStyle7.NullValue = null;
            this.last_update.DefaultCellStyle = dataGridViewCellStyle7;
            this.last_update.FillWeight = 44.14505F;
            this.last_update.HeaderText = "LAST UPDATE";
            this.last_update.Name = "last_update";
            this.last_update.ToolTipText = "double click to update";
            // 
            // stage
            // 
            this.stage.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.stage.DataPropertyName = "stage";
            this.stage.FillWeight = 52.46223F;
            this.stage.HeaderText = "STAGE";
            this.stage.Items.AddRange(new object[] {
            "QUOTED",
            "FOLLOW-UP",
            "PRESENTATION",
            "NEGOTIATION",
            "APPROVAL",
            "CLOSED"});
            this.stage.Name = "stage";
            this.stage.Resizable = System.Windows.Forms.DataGridViewTriState.True;
            this.stage.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Automatic;
            // 
            // status
            // 
            this.status.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.status.DataPropertyName = "status";
            this.status.FillWeight = 53.42654F;
            this.status.HeaderText = "STATUS";
            this.status.Items.AddRange(new object[] {
            "QUOTED",
            "BIDDING",
            "WON - PO",
            "WON - DELIVERY",
            "WON - CLOSED",
            "LOST",
            "ABANDONED"});
            this.status.Name = "status";
            this.status.Resizable = System.Windows.Forms.DataGridViewTriState.True;
            this.status.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Automatic;
            // 
            // special_deal
            // 
            this.special_deal.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.special_deal.DataPropertyName = "special_deal";
            this.special_deal.FillWeight = 44.14505F;
            this.special_deal.HeaderText = "SPECIAL DEAL/NOTES";
            this.special_deal.Name = "special_deal";
            // 
            // Opportunity_id
            // 
            this.Opportunity_id.DataPropertyName = "id";
            this.Opportunity_id.HeaderText = "Opportunity_id";
            this.Opportunity_id.Name = "Opportunity_id";
            this.Opportunity_id.Visible = false;
            // 
            // version_no
            // 
            this.version_no.DataPropertyName = "version_no";
            this.version_no.HeaderText = "ver_no";
            this.version_no.Name = "version_no";
            this.version_no.Visible = false;
            // 
            // document_no
            // 
            this.document_no.DataPropertyName = "document_no";
            this.document_no.HeaderText = "doc_no";
            this.document_no.Name = "document_no";
            this.document_no.Visible = false;
            // 
            // Opportunities
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.panel2);
            this.Controls.Add(this.panel1);
            this.Name = "Opportunities";
            this.Size = new System.Drawing.Size(1143, 786);
            this.Load += new System.EventHandler(this.Opportunities_Load);
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.panel3.ResumeLayout(false);
            this.panel3.PerformLayout();
            this.panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dgv_sales_opportunities)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Panel panel2;
        private System.Windows.Forms.DataGridView dgv_sales_opportunities;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox txt_search;
        private System.Windows.Forms.Panel panel3;
        private System.Windows.Forms.DataGridViewTextBoxColumn tag;
        private System.Windows.Forms.DataGridViewTextBoxColumn prospectref;
        private System.Windows.Forms.DataGridViewTextBoxColumn final_ref_no;
        private System.Windows.Forms.DataGridViewTextBoxColumn branch_name;
        private System.Windows.Forms.DataGridViewTextBoxColumn project_name;
        private System.Windows.Forms.DataGridViewTextBoxColumn date;
        private System.Windows.Forms.DataGridViewTextBoxColumn client_req;
        private System.Windows.Forms.DataGridViewTextBoxColumn value;
        private System.Windows.Forms.DataGridViewTextBoxColumn last_update;
        private System.Windows.Forms.DataGridViewComboBoxColumn stage;
        private System.Windows.Forms.DataGridViewComboBoxColumn status;
        private System.Windows.Forms.DataGridViewTextBoxColumn special_deal;
        private System.Windows.Forms.DataGridViewTextBoxColumn Opportunity_id;
        private System.Windows.Forms.DataGridViewTextBoxColumn version_no;
        private System.Windows.Forms.DataGridViewTextBoxColumn document_no;
    }
}
