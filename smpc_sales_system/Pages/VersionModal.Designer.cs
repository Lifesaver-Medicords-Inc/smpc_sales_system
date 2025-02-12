
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
            this.DESCRIPTION = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Column1 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Column2 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Column3 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Column4 = new System.Windows.Forms.DataGridViewTextBoxColumn();
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
            this.label1.Location = new System.Drawing.Point(11, 18);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(125, 25);
            this.label1.TabIndex = 0;
            this.label1.Text = "Version List";
            // 
            // dgv_version_modal
            // 
            this.dgv_version_modal.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgv_version_modal.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.DESCRIPTION,
            this.Column1,
            this.Column2,
            this.Column3,
            this.Column4});
            this.dgv_version_modal.Location = new System.Drawing.Point(0, 65);
            this.dgv_version_modal.Name = "dgv_version_modal";
            this.dgv_version_modal.Size = new System.Drawing.Size(652, 384);
            this.dgv_version_modal.TabIndex = 1;
            this.dgv_version_modal.CellClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.dataGridView1_CellClick);
            // 
            // DESCRIPTION
            // 
            this.DESCRIPTION.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.DESCRIPTION.HeaderText = "DESCRIPTION";
            this.DESCRIPTION.Name = "DESCRIPTION";
            // 
            // Column1
            // 
            this.Column1.HeaderText = "VERSION NO.";
            this.Column1.Name = "Column1";
            // 
            // Column2
            // 
            this.Column2.HeaderText = "DATE";
            this.Column2.Name = "Column2";
            // 
            // Column3
            // 
            this.Column3.HeaderText = "STATUS";
            this.Column3.Name = "Column3";
            // 
            // Column4
            // 
            this.Column4.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.Column4.HeaderText = "REMARK";
            this.Column4.Name = "Column4";
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
        private System.Windows.Forms.DataGridViewTextBoxColumn DESCRIPTION;
        private System.Windows.Forms.DataGridViewTextBoxColumn Column1;
        private System.Windows.Forms.DataGridViewTextBoxColumn Column2;
        private System.Windows.Forms.DataGridViewTextBoxColumn Column3;
        private System.Windows.Forms.DataGridViewTextBoxColumn Column4;
    }
}