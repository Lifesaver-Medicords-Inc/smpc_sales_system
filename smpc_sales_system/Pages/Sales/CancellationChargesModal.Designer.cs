namespace smpc_sales_system.Pages.Sales
{
    partial class CancellationChargesModal
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
            this.lbl_intro = new System.Windows.Forms.Label();
            this.lbl_restocking = new System.Windows.Forms.Label();
            this.txt_restocking = new System.Windows.Forms.TextBox();
            this.lbl_cancellation = new System.Windows.Forms.Label();
            this.txt_cancellation = new System.Windows.Forms.TextBox();
            this.lbl_base_c = new System.Windows.Forms.Label();
            this.lbl_base = new System.Windows.Forms.Label();
            this.lbl_restocking_amount_c = new System.Windows.Forms.Label();
            this.lbl_restocking_amount = new System.Windows.Forms.Label();
            this.lbl_cancellation_amount_c = new System.Windows.Forms.Label();
            this.lbl_cancellation_amount = new System.Windows.Forms.Label();
            this.lbl_total_c = new System.Windows.Forms.Label();
            this.lbl_total = new System.Windows.Forms.Label();
            this.lbl_note = new System.Windows.Forms.Label();
            this.btn_back = new System.Windows.Forms.Button();
            this.btn_proceed = new System.Windows.Forms.Button();
            this.SuspendLayout();
            //
            // lbl_title
            //
            this.lbl_title.AutoSize = true;
            this.lbl_title.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold);
            this.lbl_title.Location = new System.Drawing.Point(18, 16);
            this.lbl_title.Name = "lbl_title";
            this.lbl_title.Size = new System.Drawing.Size(220, 20);
            this.lbl_title.Text = "Cancellation Charges";
            //
            // lbl_intro
            //
            this.lbl_intro.Location = new System.Drawing.Point(20, 44);
            this.lbl_intro.Name = "lbl_intro";
            this.lbl_intro.Size = new System.Drawing.Size(456, 34);
            this.lbl_intro.Text = "Charged on what was not delivered. Enter 0 for a fee that is not being charged.";
            //
            // percent inputs
            //
            this.lbl_restocking.AutoSize = true;
            this.lbl_restocking.Location = new System.Drawing.Point(20, 90);
            this.lbl_restocking.Name = "lbl_restocking";
            this.lbl_restocking.Size = new System.Drawing.Size(120, 13);
            this.lbl_restocking.Text = "RESTOCKING FEE (%)";
            this.txt_restocking.Location = new System.Drawing.Point(190, 87);
            this.txt_restocking.Name = "txt_restocking";
            this.txt_restocking.Size = new System.Drawing.Size(90, 20);
            this.txt_restocking.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;

            this.lbl_cancellation.AutoSize = true;
            this.lbl_cancellation.Location = new System.Drawing.Point(20, 118);
            this.lbl_cancellation.Name = "lbl_cancellation";
            this.lbl_cancellation.Size = new System.Drawing.Size(126, 13);
            this.lbl_cancellation.Text = "CANCELLATION FEE (%)";
            this.txt_cancellation.Location = new System.Drawing.Point(190, 115);
            this.txt_cancellation.Name = "txt_cancellation";
            this.txt_cancellation.Size = new System.Drawing.Size(90, 20);
            this.txt_cancellation.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            //
            // computed figures
            //
            this.lbl_base_c.AutoSize = true;
            this.lbl_base_c.Location = new System.Drawing.Point(20, 158);
            this.lbl_base_c.Name = "lbl_base_c";
            this.lbl_base_c.Size = new System.Drawing.Size(160, 13);
            this.lbl_base_c.Text = "FEE BASE (undelivered, incl. VAT)";
            this.lbl_base.Location = new System.Drawing.Point(300, 158);
            this.lbl_base.Name = "lbl_base";
            this.lbl_base.Size = new System.Drawing.Size(160, 16);
            this.lbl_base.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.lbl_base.Text = "0.00";

            this.lbl_restocking_amount_c.AutoSize = true;
            this.lbl_restocking_amount_c.Location = new System.Drawing.Point(20, 182);
            this.lbl_restocking_amount_c.Name = "lbl_restocking_amount_c";
            this.lbl_restocking_amount_c.Size = new System.Drawing.Size(100, 13);
            this.lbl_restocking_amount_c.Text = "RESTOCKING FEE";
            this.lbl_restocking_amount.Location = new System.Drawing.Point(300, 182);
            this.lbl_restocking_amount.Name = "lbl_restocking_amount";
            this.lbl_restocking_amount.Size = new System.Drawing.Size(160, 16);
            this.lbl_restocking_amount.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.lbl_restocking_amount.Text = "0.00";

            this.lbl_cancellation_amount_c.AutoSize = true;
            this.lbl_cancellation_amount_c.Location = new System.Drawing.Point(20, 206);
            this.lbl_cancellation_amount_c.Name = "lbl_cancellation_amount_c";
            this.lbl_cancellation_amount_c.Size = new System.Drawing.Size(110, 13);
            this.lbl_cancellation_amount_c.Text = "CANCELLATION FEE";
            this.lbl_cancellation_amount.Location = new System.Drawing.Point(300, 206);
            this.lbl_cancellation_amount.Name = "lbl_cancellation_amount";
            this.lbl_cancellation_amount.Size = new System.Drawing.Size(160, 16);
            this.lbl_cancellation_amount.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.lbl_cancellation_amount.Text = "0.00";

            this.lbl_total_c.AutoSize = true;
            this.lbl_total_c.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold);
            this.lbl_total_c.Location = new System.Drawing.Point(20, 232);
            this.lbl_total_c.Name = "lbl_total_c";
            this.lbl_total_c.Size = new System.Drawing.Size(100, 13);
            this.lbl_total_c.Text = "TOTAL CHARGE";
            this.lbl_total.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold);
            this.lbl_total.Location = new System.Drawing.Point(300, 232);
            this.lbl_total.Name = "lbl_total";
            this.lbl_total.Size = new System.Drawing.Size(160, 16);
            this.lbl_total.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.lbl_total.Text = "0.00";
            //
            // lbl_note
            //
            this.lbl_note.Location = new System.Drawing.Point(20, 258);
            this.lbl_note.Name = "lbl_note";
            this.lbl_note.Size = new System.Drawing.Size(456, 46);
            this.lbl_note.Text = "PROCEED submits the cancellation for Sales Manager or CBDO approval. "
                + "Nothing moves until it is approved: stock stays reserved and no department is notified.";
            //
            // buttons
            //
            this.btn_back.Location = new System.Drawing.Point(268, 316);
            this.btn_back.Name = "btn_back";
            this.btn_back.Size = new System.Drawing.Size(96, 30);
            this.btn_back.Text = "BACK";
            this.btn_back.UseVisualStyleBackColor = true;

            this.btn_proceed.Location = new System.Drawing.Point(374, 316);
            this.btn_proceed.Name = "btn_proceed";
            this.btn_proceed.Size = new System.Drawing.Size(102, 30);
            this.btn_proceed.Text = "PROCEED";
            this.btn_proceed.UseVisualStyleBackColor = true;
            //
            // CancellationChargesModal
            //
            this.AcceptButton = this.btn_proceed;
            // BACK is the escape route, never a button called CANCEL - the header
            // button that starts the cancellation is already CANCEL (spec 5.4).
            this.CancelButton = this.btn_back;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(496, 362);
            this.Controls.Add(this.btn_proceed);
            this.Controls.Add(this.btn_back);
            this.Controls.Add(this.lbl_note);
            this.Controls.Add(this.lbl_total);
            this.Controls.Add(this.lbl_total_c);
            this.Controls.Add(this.lbl_cancellation_amount);
            this.Controls.Add(this.lbl_cancellation_amount_c);
            this.Controls.Add(this.lbl_restocking_amount);
            this.Controls.Add(this.lbl_restocking_amount_c);
            this.Controls.Add(this.lbl_base);
            this.Controls.Add(this.lbl_base_c);
            this.Controls.Add(this.txt_cancellation);
            this.Controls.Add(this.lbl_cancellation);
            this.Controls.Add(this.txt_restocking);
            this.Controls.Add(this.lbl_restocking);
            this.Controls.Add(this.lbl_intro);
            this.Controls.Add(this.lbl_title);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "CancellationChargesModal";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Cancellation Charges";
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        private System.Windows.Forms.Label lbl_title;
        private System.Windows.Forms.Label lbl_intro;
        private System.Windows.Forms.Label lbl_restocking;
        private System.Windows.Forms.TextBox txt_restocking;
        private System.Windows.Forms.Label lbl_cancellation;
        private System.Windows.Forms.TextBox txt_cancellation;
        private System.Windows.Forms.Label lbl_base_c;
        private System.Windows.Forms.Label lbl_base;
        private System.Windows.Forms.Label lbl_restocking_amount_c;
        private System.Windows.Forms.Label lbl_restocking_amount;
        private System.Windows.Forms.Label lbl_cancellation_amount_c;
        private System.Windows.Forms.Label lbl_cancellation_amount;
        private System.Windows.Forms.Label lbl_total_c;
        private System.Windows.Forms.Label lbl_total;
        private System.Windows.Forms.Label lbl_note;
        private System.Windows.Forms.Button btn_back;
        private System.Windows.Forms.Button btn_proceed;
    }
}
