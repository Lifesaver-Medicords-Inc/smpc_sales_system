
namespace smpc_sales_system.Pages
{
    partial class VersionModal
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
            this.panel1 = new System.Windows.Forms.Panel();
            this.label1 = new System.Windows.Forms.Label();
            this.dgv_version_modal = new System.Windows.Forms.DataGridView();
            this.v_desc = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.v_no = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.v_date = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.ver_status = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.v_remark = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.panel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgv_version_modal)).BeginInit();
            this.SuspendLayout();
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.label1);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Top;
            this.panel1.Location = new System.Drawing.Point(0, 0);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(652, 59);
            this.panel1.TabIndex = 0;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 15.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(5, 17);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(125, 25);
            this.label1.TabIndex = 0;
            this.label1.Text = "Version List";
            // 
            // dgv_version_modal
            // 
            this.dgv_version_modal.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgv_version_modal.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.v_desc,
            this.v_no,
            this.v_date,
            this.ver_status,
            this.v_remark});
            this.dgv_version_modal.Location = new System.Drawing.Point(0, 65);
            this.dgv_version_modal.Name = "dgv_version_modal";
            this.dgv_version_modal.Size = new System.Drawing.Size(652, 384);
            this.dgv_version_modal.TabIndex = 1;
            this.dgv_version_modal.CellClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.dataGridView1_CellClick);
            // 
            // v_desc
            // 
            this.v_desc.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.v_desc.DataPropertyName = "version_description";
            this.v_desc.HeaderText = "DESCRIPTION";
            this.v_desc.Name = "v_desc";
            // 
            // v_no
            // 
            this.v_no.DataPropertyName = "version_no";
            this.v_no.HeaderText = "VERSION NO.";
            this.v_no.Name = "v_no";
            // 
            // v_date
            // 
            this.v_date.DataPropertyName = "date";
            this.v_date.HeaderText = "DATE";
            this.v_date.Name = "v_date";
            // 
            // ver_status
            // 
            this.ver_status.HeaderText = "STATUS";
            this.ver_status.Name = "ver_status";
            // 
            // v_remark
            // 
            this.v_remark.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.v_remark.DataPropertyName = "version_remark";
            this.v_remark.HeaderText = "REMARK";
            this.v_remark.Name = "v_remark";
            // 
            // VersionModal
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(652, 450);
            this.Controls.Add(this.dgv_version_modal);
            this.Controls.Add(this.panel1);
            this.Name = "VersionModal";
            this.Text = "Form1";
            this.Load += new System.EventHandler(this.VersionModal_Load);
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgv_version_modal)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.DataGridView dgv_version_modal;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.DataGridViewTextBoxColumn v_desc;
        private System.Windows.Forms.DataGridViewTextBoxColumn v_no;
        private System.Windows.Forms.DataGridViewTextBoxColumn v_date;
        private System.Windows.Forms.DataGridViewTextBoxColumn ver_status;
        private System.Windows.Forms.DataGridViewTextBoxColumn v_remark;
    }
}