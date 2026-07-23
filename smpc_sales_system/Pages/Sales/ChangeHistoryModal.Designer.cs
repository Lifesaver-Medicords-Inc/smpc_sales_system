namespace smpc_sales_app.Pages.Sales
{
    partial class ChangeHistoryModal
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
            this.flowLayoutPanelHistory = new System.Windows.Forms.FlowLayoutPanel();
            this.btnClose = new System.Windows.Forms.Button();
            this.SuspendLayout();
            //
            // flowLayoutPanelHistory
            //
            this.flowLayoutPanelHistory.AutoScroll = true;
            this.flowLayoutPanelHistory.Dock = System.Windows.Forms.DockStyle.Fill;
            this.flowLayoutPanelHistory.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.flowLayoutPanelHistory.Location = new System.Drawing.Point(0, 0);
            this.flowLayoutPanelHistory.Name = "flowLayoutPanelHistory";
            this.flowLayoutPanelHistory.Padding = new System.Windows.Forms.Padding(8);
            this.flowLayoutPanelHistory.Size = new System.Drawing.Size(484, 521);
            this.flowLayoutPanelHistory.TabIndex = 0;
            this.flowLayoutPanelHistory.WrapContents = false;
            //
            // btnClose
            //
            this.btnClose.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.btnClose.Location = new System.Drawing.Point(0, 521);
            this.btnClose.Name = "btnClose";
            this.btnClose.Size = new System.Drawing.Size(484, 40);
            this.btnClose.TabIndex = 1;
            this.btnClose.Text = "CLOSE";
            this.btnClose.UseVisualStyleBackColor = true;
            this.btnClose.Click += new System.EventHandler(this.btnClose_Click);
            //
            // ChangeHistoryModal
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(484, 561);
            this.Controls.Add(this.flowLayoutPanelHistory);
            this.Controls.Add(this.btnClose);
            this.MinimumSize = new System.Drawing.Size(360, 300);
            this.Name = "ChangeHistoryModal";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Change History - Full Details";
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanelHistory;
        private System.Windows.Forms.Button btnClose;
    }
}
