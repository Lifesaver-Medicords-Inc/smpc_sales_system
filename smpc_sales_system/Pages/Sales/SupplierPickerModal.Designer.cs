namespace smpc_sales_system.Pages.Sales
{
    partial class SupplierPickerModal
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.lbl_title = new System.Windows.Forms.Label();
            this.lst_suppliers = new System.Windows.Forms.ListBox();
            this.btn_ok = new System.Windows.Forms.Button();
            this.btn_cancel = new System.Windows.Forms.Button();
            this.SuspendLayout();
            //
            // lbl_title
            //
            this.lbl_title.AutoSize = true;
            this.lbl_title.Font = new System.Drawing.Font("Microsoft Sans Serif", 10.5F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lbl_title.Location = new System.Drawing.Point(15, 15);
            this.lbl_title.Name = "lbl_title";
            this.lbl_title.Size = new System.Drawing.Size(150, 20);
            this.lbl_title.TabIndex = 0;
            this.lbl_title.Text = "Select Supplier";
            //
            // lst_suppliers
            //
            this.lst_suppliers.FormattingEnabled = true;
            this.lst_suppliers.Location = new System.Drawing.Point(15, 45);
            this.lst_suppliers.Name = "lst_suppliers";
            this.lst_suppliers.Size = new System.Drawing.Size(330, 199);
            this.lst_suppliers.TabIndex = 1;
            //
            // btn_ok
            //
            this.btn_ok.Location = new System.Drawing.Point(135, 254);
            this.btn_ok.Name = "btn_ok";
            this.btn_ok.Size = new System.Drawing.Size(100, 28);
            this.btn_ok.TabIndex = 2;
            this.btn_ok.Text = "OK";
            this.btn_ok.UseVisualStyleBackColor = true;
            this.btn_ok.Click += new System.EventHandler(this.btn_ok_Click);
            //
            // btn_cancel
            //
            this.btn_cancel.Location = new System.Drawing.Point(245, 254);
            this.btn_cancel.Name = "btn_cancel";
            this.btn_cancel.Size = new System.Drawing.Size(100, 28);
            this.btn_cancel.TabIndex = 3;
            this.btn_cancel.Text = "CANCEL";
            this.btn_cancel.UseVisualStyleBackColor = true;
            this.btn_cancel.Click += new System.EventHandler(this.btn_cancel_Click);
            //
            // SupplierPickerModal
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(360, 297);
            this.Controls.Add(this.lbl_title);
            this.Controls.Add(this.lst_suppliers);
            this.Controls.Add(this.btn_ok);
            this.Controls.Add(this.btn_cancel);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "SupplierPickerModal";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Select Supplier";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        private System.Windows.Forms.Label lbl_title;
        private System.Windows.Forms.ListBox lst_suppliers;
        private System.Windows.Forms.Button btn_ok;
        private System.Windows.Forms.Button btn_cancel;
    }
}
