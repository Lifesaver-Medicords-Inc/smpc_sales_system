namespace smpc_sales_app.Pages.Sales.Modal
{
    partial class RequestForEngrModal
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

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            this.lbl_title = new System.Windows.Forms.Label();
            this.lbl_engineer = new System.Windows.Forms.Label();
            this.cmb_engineer = new System.Windows.Forms.ComboBox();
            this.btn_send = new System.Windows.Forms.Button();
            this.btn_cancel = new System.Windows.Forms.Button();
            this.SuspendLayout();
            //
            // lbl_title
            //
            this.lbl_title.AutoSize = true;
            this.lbl_title.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold);
            this.lbl_title.Location = new System.Drawing.Point(16, 15);
            this.lbl_title.Name = "lbl_title";
            this.lbl_title.Size = new System.Drawing.Size(199, 20);
            this.lbl_title.TabIndex = 0;
            this.lbl_title.Text = "Request for Engineering";
            //
            // lbl_engineer
            //
            this.lbl_engineer.AutoSize = true;
            this.lbl_engineer.Location = new System.Drawing.Point(18, 58);
            this.lbl_engineer.Name = "lbl_engineer";
            this.lbl_engineer.Size = new System.Drawing.Size(52, 13);
            this.lbl_engineer.TabIndex = 1;
            this.lbl_engineer.Text = "ENGINEER";
            //
            // cmb_engineer
            //
            this.cmb_engineer.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmb_engineer.FormattingEnabled = true;
            this.cmb_engineer.Location = new System.Drawing.Point(21, 78);
            this.cmb_engineer.Name = "cmb_engineer";
            this.cmb_engineer.Size = new System.Drawing.Size(300, 21);
            this.cmb_engineer.TabIndex = 2;
            //
            // btn_send
            //
            this.btn_send.Location = new System.Drawing.Point(146, 120);
            this.btn_send.Name = "btn_send";
            this.btn_send.Size = new System.Drawing.Size(85, 27);
            this.btn_send.TabIndex = 3;
            this.btn_send.Text = "Send";
            this.btn_send.UseVisualStyleBackColor = true;
            this.btn_send.Click += new System.EventHandler(this.btn_send_Click);
            //
            // btn_cancel
            //
            this.btn_cancel.Location = new System.Drawing.Point(237, 120);
            this.btn_cancel.Name = "btn_cancel";
            this.btn_cancel.Size = new System.Drawing.Size(84, 27);
            this.btn_cancel.TabIndex = 4;
            this.btn_cancel.Text = "Cancel";
            this.btn_cancel.UseVisualStyleBackColor = true;
            this.btn_cancel.Click += new System.EventHandler(this.btn_cancel_Click);
            //
            // RequestForEngrModal
            //
            this.AcceptButton = this.btn_send;
            this.CancelButton = this.btn_cancel;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(340, 162);
            this.Controls.Add(this.btn_cancel);
            this.Controls.Add(this.btn_send);
            this.Controls.Add(this.cmb_engineer);
            this.Controls.Add(this.lbl_engineer);
            this.Controls.Add(this.lbl_title);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "RequestForEngrModal";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Request for Engineering";
            this.Load += new System.EventHandler(this.RequestForEngrModal_Load);
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private System.Windows.Forms.Label lbl_title;
        private System.Windows.Forms.Label lbl_engineer;
        private System.Windows.Forms.ComboBox cmb_engineer;
        private System.Windows.Forms.Button btn_send;
        private System.Windows.Forms.Button btn_cancel;
    }
}
