
namespace smpc_sales_system.Pages.Sales
{
    partial class CRM
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
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle2 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle8 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle3 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle4 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle5 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle6 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle7 = new System.Windows.Forms.DataGridViewCellStyle();
            this.panel2 = new System.Windows.Forms.Panel();
            this.dgv_branch = new System.Windows.Forms.DataGridView();
            this.panel1 = new System.Windows.Forms.Panel();
            this.panel3 = new System.Windows.Forms.Panel();
            this.txt_search = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.based_id = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.tag = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.branch_name = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.number = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.name = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.email = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.date = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.remark = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.crm_id = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.sales_id = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.panel2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgv_branch)).BeginInit();
            this.panel1.SuspendLayout();
            this.panel3.SuspendLayout();
            this.SuspendLayout();
            // 
            // panel2
            // 
            this.panel2.Controls.Add(this.dgv_branch);
            this.panel2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel2.Location = new System.Drawing.Point(0, 64);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(1050, 499);
            this.panel2.TabIndex = 3;
            // 
            // dgv_branch
            // 
            this.dgv_branch.AllowUserToAddRows = false;
            this.dgv_branch.AllowUserToDeleteRows = false;
            dataGridViewCellStyle1.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
            this.dgv_branch.AlternatingRowsDefaultCellStyle = dataGridViewCellStyle1;
            this.dgv_branch.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.dgv_branch.AutoSizeRowsMode = System.Windows.Forms.DataGridViewAutoSizeRowsMode.AllCells;
            this.dgv_branch.ColumnHeadersBorderStyle = System.Windows.Forms.DataGridViewHeaderBorderStyle.Single;
            dataGridViewCellStyle2.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
            dataGridViewCellStyle2.BackColor = System.Drawing.SystemColors.Control;
            dataGridViewCellStyle2.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            dataGridViewCellStyle2.ForeColor = System.Drawing.SystemColors.WindowText;
            dataGridViewCellStyle2.Padding = new System.Windows.Forms.Padding(15, 0, 0, 0);
            dataGridViewCellStyle2.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle2.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle2.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            this.dgv_branch.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle2;
            this.dgv_branch.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgv_branch.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.based_id,
            this.tag,
            this.branch_name,
            this.number,
            this.name,
            this.email,
            this.date,
            this.remark,
            this.crm_id,
            this.sales_id});
            dataGridViewCellStyle8.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
            dataGridViewCellStyle8.BackColor = System.Drawing.SystemColors.Window;
            dataGridViewCellStyle8.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            dataGridViewCellStyle8.ForeColor = System.Drawing.SystemColors.ControlText;
            dataGridViewCellStyle8.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle8.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle8.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.dgv_branch.DefaultCellStyle = dataGridViewCellStyle8;
            this.dgv_branch.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgv_branch.Location = new System.Drawing.Point(0, 0);
            this.dgv_branch.Name = "dgv_branch";
            this.dgv_branch.Size = new System.Drawing.Size(1050, 499);
            this.dgv_branch.TabIndex = 0;
            this.dgv_branch.CellContentDoubleClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.dgv_branch_CellContentDoubleClick);
            this.dgv_branch.CellEndEdit += new System.Windows.Forms.DataGridViewCellEventHandler(this.dgv_branch_CellEndEdit);
            this.dgv_branch.CellPainting += new System.Windows.Forms.DataGridViewCellPaintingEventHandler(this.dgv_branch_CellPainting);
            this.dgv_branch.CellToolTipTextNeeded += new System.Windows.Forms.DataGridViewCellToolTipTextNeededEventHandler(this.dgv_branch_CellToolTipTextNeeded);
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.panel3);
            this.panel1.Controls.Add(this.label1);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Top;
            this.panel1.Location = new System.Drawing.Point(0, 0);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(1050, 64);
            this.panel1.TabIndex = 2;
            // 
            // panel3
            // 
            this.panel3.Controls.Add(this.txt_search);
            this.panel3.Controls.Add(this.label2);
            this.panel3.Dock = System.Windows.Forms.DockStyle.Right;
            this.panel3.Location = new System.Drawing.Point(779, 0);
            this.panel3.Name = "panel3";
            this.panel3.Size = new System.Drawing.Size(271, 64);
            this.panel3.TabIndex = 5;
            // 
            // txt_search
            // 
            this.txt_search.Location = new System.Drawing.Point(50, 24);
            this.txt_search.Name = "txt_search";
            this.txt_search.Size = new System.Drawing.Size(209, 20);
            this.txt_search.TabIndex = 2;
            this.txt_search.TextChanged += new System.EventHandler(this.txt_search_TextChanged);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(9, 27);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(41, 13);
            this.label2.TabIndex = 3;
            this.label2.Text = "Search";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 15.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(3, 34);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(479, 25);
            this.label1.TabIndex = 1;
            this.label1.Text = "CUSTOMER RELATIONSHIP MANAGEMENT";
            // 
            // based_id
            // 
            this.based_id.DataPropertyName = "id";
            this.based_id.HeaderText = "id";
            this.based_id.Name = "based_id";
            this.based_id.Visible = false;
            // 
            // tag
            // 
            this.tag.DataPropertyName = "tag";
            this.tag.FillWeight = 114.466F;
            this.tag.HeaderText = "TAG";
            this.tag.Name = "tag";
            // 
            // branch_name
            // 
            this.branch_name.DataPropertyName = "branch_name";
            dataGridViewCellStyle3.BackColor = System.Drawing.Color.Gainsboro;
            this.branch_name.DefaultCellStyle = dataGridViewCellStyle3;
            this.branch_name.FillWeight = 114.466F;
            this.branch_name.HeaderText = "COMPANY NAME";
            this.branch_name.Name = "branch_name";
            this.branch_name.ReadOnly = true;
            // 
            // number
            // 
            this.number.DataPropertyName = "number";
            dataGridViewCellStyle4.BackColor = System.Drawing.Color.Gainsboro;
            this.number.DefaultCellStyle = dataGridViewCellStyle4;
            this.number.FillWeight = 71.06599F;
            this.number.HeaderText = "WORK PHONE";
            this.number.Name = "number";
            this.number.ReadOnly = true;
            // 
            // name
            // 
            this.name.DataPropertyName = "name";
            dataGridViewCellStyle5.BackColor = System.Drawing.Color.Gainsboro;
            this.name.DefaultCellStyle = dataGridViewCellStyle5;
            this.name.FillWeight = 114.466F;
            this.name.HeaderText = "CONTACT PERSON";
            this.name.Name = "name";
            this.name.ReadOnly = true;
            // 
            // email
            // 
            this.email.DataPropertyName = "email";
            dataGridViewCellStyle6.BackColor = System.Drawing.Color.Gainsboro;
            this.email.DefaultCellStyle = dataGridViewCellStyle6;
            this.email.FillWeight = 114.466F;
            this.email.HeaderText = "EMAIL";
            this.email.Name = "email";
            this.email.ReadOnly = true;
            // 
            // date
            // 
            this.date.DataPropertyName = "date";
            dataGridViewCellStyle7.BackColor = System.Drawing.Color.Gainsboro;
            dataGridViewCellStyle7.ForeColor = System.Drawing.Color.Teal;
            this.date.DefaultCellStyle = dataGridViewCellStyle7;
            this.date.FillWeight = 56.60406F;
            this.date.HeaderText = "DATE";
            this.date.Name = "date";
            this.date.ReadOnly = true;
            // 
            // remark
            // 
            this.remark.DataPropertyName = "remark";
            this.remark.FillWeight = 114.466F;
            this.remark.HeaderText = "REMARKS";
            this.remark.Name = "remark";
            this.remark.ToolTipText = "Every 3 minutes update";
            // 
            // crm_id
            // 
            this.crm_id.DataPropertyName = "crm_id";
            this.crm_id.HeaderText = "crm_id";
            this.crm_id.Name = "crm_id";
            this.crm_id.Visible = false;
            // 
            // sales_id
            // 
            this.sales_id.DataPropertyName = "sales_id";
            this.sales_id.HeaderText = "sales_id";
            this.sales_id.Name = "sales_id";
            this.sales_id.Visible = false;
            // 
            // CRM
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.panel2);
            this.Controls.Add(this.panel1);
            this.Name = "CRM";
            this.Size = new System.Drawing.Size(1050, 563);
            this.Load += new System.EventHandler(this.CRM_Load);
            this.panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dgv_branch)).EndInit();
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.panel3.ResumeLayout(false);
            this.panel3.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.Panel panel2;
        private System.Windows.Forms.DataGridView dgv_branch;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Panel panel3;
        private System.Windows.Forms.TextBox txt_search;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.DataGridViewTextBoxColumn based_id;
        private System.Windows.Forms.DataGridViewTextBoxColumn tag;
        private System.Windows.Forms.DataGridViewTextBoxColumn branch_name;
        private System.Windows.Forms.DataGridViewTextBoxColumn number;
        private System.Windows.Forms.DataGridViewTextBoxColumn name;
        private System.Windows.Forms.DataGridViewTextBoxColumn email;
        private System.Windows.Forms.DataGridViewTextBoxColumn date;
        private System.Windows.Forms.DataGridViewTextBoxColumn remark;
        private System.Windows.Forms.DataGridViewTextBoxColumn crm_id;
        private System.Windows.Forms.DataGridViewTextBoxColumn sales_id;
    }
}
