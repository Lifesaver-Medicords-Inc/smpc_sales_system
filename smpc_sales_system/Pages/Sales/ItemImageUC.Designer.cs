
namespace smpc_sales_system.Pages.Sales
{
    partial class ItemImageUC
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
            this.lbl_tssss = new System.Windows.Forms.Label();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.isSelected = new System.Windows.Forms.CheckBox();
            this.panel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.SuspendLayout();
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.isSelected);
            this.panel1.Controls.Add(this.pictureBox1);
            this.panel1.Controls.Add(this.lbl_tssss);
            this.panel1.Location = new System.Drawing.Point(6, 12);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(162, 156);
            this.panel1.TabIndex = 0;
            // 
            // lbl_tssss
            // 
            this.lbl_tssss.AutoSize = true;
            this.lbl_tssss.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lbl_tssss.Location = new System.Drawing.Point(51, 126);
            this.lbl_tssss.Name = "lbl_tssss";
            this.lbl_tssss.Size = new System.Drawing.Size(67, 15);
            this.lbl_tssss.TabIndex = 0;
            this.lbl_tssss.Text = "Filename";
            // 
            // pictureBox1
            // 
            this.pictureBox1.Location = new System.Drawing.Point(36, 14);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(100, 100);
            this.pictureBox1.TabIndex = 5;
            this.pictureBox1.TabStop = false;
            // 
            // isSelected
            // 
            this.isSelected.AutoSize = true;
            this.isSelected.Location = new System.Drawing.Point(15, 14);
            this.isSelected.Name = "isSelected";
            this.isSelected.Size = new System.Drawing.Size(15, 14);
            this.isSelected.TabIndex = 6;
            this.isSelected.UseVisualStyleBackColor = true;
            // 
            // ItemImageUC
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.panel1);
            this.Name = "ItemImageUC";
            this.Size = new System.Drawing.Size(178, 183);
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Label lbl_tssss;
        private System.Windows.Forms.CheckBox isSelected;
        private System.Windows.Forms.PictureBox pictureBox1;
    }
}
