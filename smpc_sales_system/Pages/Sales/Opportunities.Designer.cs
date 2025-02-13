
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
            this.btn_find = new System.Windows.Forms.Button();
            this.label2 = new System.Windows.Forms.Label();
            this.txt_search = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.panel2 = new System.Windows.Forms.Panel();
            this.panel3 = new System.Windows.Forms.Panel();
            this.dgv_sales_opportunities = new System.Windows.Forms.DataGridView();
            this.tag = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.document_no = new System.Windows.Forms.DataGridViewTextBoxColumn();
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
            this.panel1.SuspendLayout();
            this.panel2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgv_sales_opportunities)).BeginInit();
            this.SuspendLayout();
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.btn_find);
            this.panel1.Controls.Add(this.label2);
            this.panel1.Controls.Add(this.txt_search);
            this.panel1.Controls.Add(this.label1);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Top;
            this.panel1.Location = new System.Drawing.Point(0, 0);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(1143, 64);
            this.panel1.TabIndex = 0;
            // 
            // btn_find
            // 
            this.btn_find.BackColor = System.Drawing.Color.DimGray;
            this.btn_find.FlatAppearance.BorderColor = System.Drawing.Color.White;
            this.btn_find.FlatAppearance.MouseDownBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.btn_find.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(255)))), ((int)(((byte)(128)))));
            this.btn_find.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btn_find.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btn_find.ForeColor = System.Drawing.Color.White;
            this.btn_find.Location = new System.Drawing.Point(1070, 23);
            this.btn_find.Name = "btn_find";
            this.btn_find.Size = new System.Drawing.Size(73, 22);
            this.btn_find.TabIndex = 106;
            this.btn_find.Text = "Find";
            this.btn_find.UseVisualStyleBackColor = false;
            this.btn_find.Click += new System.EventHandler(this.btn_find_Click);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(820, 27);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(41, 13);
            this.label2.TabIndex = 3;
            this.label2.Text = "Search";
            // 
            // txt_search
            // 
            this.txt_search.Location = new System.Drawing.Point(861, 24);
            this.txt_search.Name = "txt_search";
            this.txt_search.Size = new System.Drawing.Size(209, 20);
            this.txt_search.TabIndex = 2;
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
            this.panel2.Controls.Add(this.panel3);
            this.panel2.Controls.Add(this.dgv_sales_opportunities);
            this.panel2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel2.Location = new System.Drawing.Point(0, 64);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(1143, 535);
            this.panel2.TabIndex = 1;
            // 
            // panel3
            // 
            this.panel3.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panel3.Location = new System.Drawing.Point(0, 489);
            this.panel3.Name = "panel3";
            this.panel3.Size = new System.Drawing.Size(1143, 46);
            this.panel3.TabIndex = 1;
            // 
            // dgv_sales_opportunities
            // 
            this.dgv_sales_opportunities.AllowUserToAddRows = false;
            this.dgv_sales_opportunities.AllowUserToDeleteRows = false;
            this.dgv_sales_opportunities.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgv_sales_opportunities.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.tag,
            this.document_no,
            this.branch_name,
            this.project_name,
            this.date,
            this.client_req,
            this.value,
            this.last_update,
            this.stage,
            this.status,
            this.special_deal,
            this.Opportunity_id});
            this.dgv_sales_opportunities.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgv_sales_opportunities.Location = new System.Drawing.Point(0, 0);
            this.dgv_sales_opportunities.Name = "dgv_sales_opportunities";
            this.dgv_sales_opportunities.Size = new System.Drawing.Size(1143, 535);
            this.dgv_sales_opportunities.TabIndex = 0;
            this.dgv_sales_opportunities.CellDoubleClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.dgv_sales_opportunities_CellDoubleClick);
            this.dgv_sales_opportunities.CellEndEdit += new System.Windows.Forms.DataGridViewCellEventHandler(this.dgv_sales_opportunities_CellEndEdit);
            // 
            // tag
            // 
            this.tag.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.tag.DataPropertyName = "tag";
            this.tag.HeaderText = "TAG";
            this.tag.Name = "tag";
            // 
            // document_no
            // 
            this.document_no.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.document_no.DataPropertyName = "document_no";
            this.document_no.HeaderText = "QUOTE REF.";
            this.document_no.Name = "document_no";
            this.document_no.ReadOnly = true;
            // 
            // branch_name
            // 
            this.branch_name.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.branch_name.DataPropertyName = "branch_name";
            this.branch_name.HeaderText = "COMPANY NAME";
            this.branch_name.Name = "branch_name";
            this.branch_name.ReadOnly = true;
            // 
            // project_name
            // 
            this.project_name.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.project_name.DataPropertyName = "project_name";
            this.project_name.HeaderText = "PROJECT NAME";
            this.project_name.Name = "project_name";
            this.project_name.ReadOnly = true;
            // 
            // date
            // 
            this.date.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.date.DataPropertyName = "date";
            this.date.HeaderText = "INQUIRY DATE";
            this.date.Name = "date";
            this.date.ReadOnly = true;
            // 
            // client_req
            // 
            this.client_req.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.client_req.DataPropertyName = "client_req";
            this.client_req.HeaderText = "CLIENT REQ";
            this.client_req.Name = "client_req";
            // 
            // value
            // 
            this.value.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.value.DataPropertyName = "value";
            this.value.HeaderText = "VALUE";
            this.value.Name = "value";
            this.value.ReadOnly = true;
            // 
            // last_update
            // 
            this.last_update.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.last_update.DataPropertyName = "last_update";
            this.last_update.HeaderText = "LAST UPDATE";
            this.last_update.Name = "last_update";
            this.last_update.ReadOnly = true;
            // 
            // stage
            // 
            this.stage.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.stage.DataPropertyName = "stage";
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
        private System.Windows.Forms.Panel panel3;
        private System.Windows.Forms.Button btn_find;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox txt_search;
        private System.Windows.Forms.DataGridViewTextBoxColumn tag;
        private System.Windows.Forms.DataGridViewTextBoxColumn document_no;
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
    }
}
