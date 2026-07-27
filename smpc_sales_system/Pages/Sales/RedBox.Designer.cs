namespace smpc_sales_system.Pages.Sales
{
    partial class RedBox
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

        #region Component Designer generated code

        // Narrow (~300px) layout - this control is mounted directly inside the existing
        // "RED BOX" panel on the right edge of the main Layout screen (always visible,
        // not a navigable tab), so it does not draw its own title - that panel already has
        // one. Just a slim refresh/status strip up top, then the two stacked sections.
        private void InitializeComponent()
        {
            this.pnl_root = new System.Windows.Forms.Panel();
            this.pnl_body = new System.Windows.Forms.Panel();
            this.pnl_retention_section = new System.Windows.Forms.Panel();
            this.pnl_retention = new System.Windows.Forms.FlowLayoutPanel();
            this.lbl_retention_header = new System.Windows.Forms.Label();
            this.pnl_quotes_section = new System.Windows.Forms.Panel();
            this.pnl_quotes = new System.Windows.Forms.FlowLayoutPanel();
            this.lbl_quotes_header = new System.Windows.Forms.Label();
            this.pnl_top = new System.Windows.Forms.Panel();
            this.lbl_status = new System.Windows.Forms.Label();
            this.btn_refresh = new System.Windows.Forms.Button();
            this.pnl_root.SuspendLayout();
            this.pnl_body.SuspendLayout();
            this.pnl_retention_section.SuspendLayout();
            this.pnl_quotes_section.SuspendLayout();
            this.pnl_top.SuspendLayout();
            this.SuspendLayout();
            // 
            // pnl_root
            // 
            this.pnl_root.Controls.Add(this.pnl_body);
            this.pnl_root.Controls.Add(this.pnl_top);
            this.pnl_root.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnl_root.Location = new System.Drawing.Point(0, 0);
            this.pnl_root.Name = "pnl_root";
            this.pnl_root.Size = new System.Drawing.Size(300, 714);
            this.pnl_root.TabIndex = 0;
            // 
            // pnl_body
            // 
            this.pnl_body.Controls.Add(this.pnl_retention_section);
            this.pnl_body.Controls.Add(this.pnl_quotes_section);
            this.pnl_body.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnl_body.Location = new System.Drawing.Point(0, 24);
            this.pnl_body.Name = "pnl_body";
            this.pnl_body.Size = new System.Drawing.Size(300, 690);
            this.pnl_body.TabIndex = 1;
            // 
            // pnl_retention_section
            // 
            this.pnl_retention_section.Controls.Add(this.pnl_retention);
            this.pnl_retention_section.Controls.Add(this.lbl_retention_header);
            this.pnl_retention_section.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnl_retention_section.Location = new System.Drawing.Point(0, 566);
            this.pnl_retention_section.Name = "pnl_retention_section";
            this.pnl_retention_section.Size = new System.Drawing.Size(300, 124);
            this.pnl_retention_section.TabIndex = 1;
            // 
            // pnl_retention
            // 
            this.pnl_retention.AutoScroll = true;
            this.pnl_retention.BackColor = System.Drawing.Color.LightCoral;
            this.pnl_retention.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnl_retention.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.pnl_retention.Location = new System.Drawing.Point(0, 22);
            this.pnl_retention.Name = "pnl_retention";
            this.pnl_retention.Size = new System.Drawing.Size(300, 102);
            this.pnl_retention.TabIndex = 1;
            this.pnl_retention.WrapContents = false;
            // 
            // lbl_retention_header
            // 
            this.lbl_retention_header.BackColor = System.Drawing.Color.IndianRed;
            this.lbl_retention_header.Dock = System.Windows.Forms.DockStyle.Top;
            this.lbl_retention_header.Font = new System.Drawing.Font("Segoe UI", 8.5F, System.Drawing.FontStyle.Bold);
            this.lbl_retention_header.ForeColor = System.Drawing.Color.Black;
            this.lbl_retention_header.Location = new System.Drawing.Point(0, 0);
            this.lbl_retention_header.Name = "lbl_retention_header";
            this.lbl_retention_header.Padding = new System.Windows.Forms.Padding(6, 0, 0, 0);
            this.lbl_retention_header.Size = new System.Drawing.Size(300, 22);
            this.lbl_retention_header.TabIndex = 0;
            this.lbl_retention_header.Text = "CLIENT RETENTION";
            this.lbl_retention_header.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // pnl_quotes_section
            // 
            this.pnl_quotes_section.Controls.Add(this.pnl_quotes);
            this.pnl_quotes_section.Controls.Add(this.lbl_quotes_header);
            this.pnl_quotes_section.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnl_quotes_section.Location = new System.Drawing.Point(0, 0);
            this.pnl_quotes_section.Name = "pnl_quotes_section";
            this.pnl_quotes_section.Size = new System.Drawing.Size(300, 566);
            this.pnl_quotes_section.TabIndex = 0;
            // 
            // pnl_quotes
            // 
            this.pnl_quotes.AutoScroll = true;
            this.pnl_quotes.BackColor = System.Drawing.Color.LightCoral;
            this.pnl_quotes.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnl_quotes.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.pnl_quotes.Location = new System.Drawing.Point(0, 22);
            this.pnl_quotes.Name = "pnl_quotes";
            this.pnl_quotes.Size = new System.Drawing.Size(300, 544);
            this.pnl_quotes.TabIndex = 1;
            this.pnl_quotes.WrapContents = false;
            // 
            // lbl_quotes_header
            // 
            this.lbl_quotes_header.BackColor = System.Drawing.Color.IndianRed;
            this.lbl_quotes_header.Dock = System.Windows.Forms.DockStyle.Top;
            this.lbl_quotes_header.Font = new System.Drawing.Font("Segoe UI", 8.5F, System.Drawing.FontStyle.Bold);
            this.lbl_quotes_header.ForeColor = System.Drawing.Color.Black;
            this.lbl_quotes_header.Location = new System.Drawing.Point(0, 0);
            this.lbl_quotes_header.Name = "lbl_quotes_header";
            this.lbl_quotes_header.Padding = new System.Windows.Forms.Padding(6, 0, 0, 0);
            this.lbl_quotes_header.Size = new System.Drawing.Size(300, 22);
            this.lbl_quotes_header.TabIndex = 0;
            this.lbl_quotes_header.Text = "QUOTES / SALES ORDERS";
            this.lbl_quotes_header.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // pnl_top
            // 
            this.pnl_top.Controls.Add(this.lbl_status);
            this.pnl_top.Controls.Add(this.btn_refresh);
            this.pnl_top.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnl_top.Location = new System.Drawing.Point(0, 0);
            this.pnl_top.Name = "pnl_top";
            this.pnl_top.Size = new System.Drawing.Size(300, 24);
            this.pnl_top.TabIndex = 0;
            // 
            // lbl_status
            // 
            this.lbl_status.AutoSize = true;
            this.lbl_status.Font = new System.Drawing.Font("Segoe UI", 7F);
            this.lbl_status.ForeColor = System.Drawing.Color.DimGray;
            this.lbl_status.Location = new System.Drawing.Point(4, 6);
            this.lbl_status.Name = "lbl_status";
            this.lbl_status.Size = new System.Drawing.Size(0, 12);
            this.lbl_status.TabIndex = 1;
            // 
            // btn_refresh
            // 
            this.btn_refresh.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btn_refresh.Font = new System.Drawing.Font("Segoe UI", 7F);
            this.btn_refresh.Location = new System.Drawing.Point(230, 1);
            this.btn_refresh.Name = "btn_refresh";
            this.btn_refresh.Size = new System.Drawing.Size(65, 21);
            this.btn_refresh.TabIndex = 0;
            this.btn_refresh.Text = "Refresh";
            this.btn_refresh.UseVisualStyleBackColor = true;
            this.btn_refresh.Click += new System.EventHandler(this.btn_refresh_Click);
            // 
            // RedBox
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.pnl_root);
            this.Name = "RedBox";
            this.Size = new System.Drawing.Size(300, 714);
            this.pnl_root.ResumeLayout(false);
            this.pnl_body.ResumeLayout(false);
            this.pnl_retention_section.ResumeLayout(false);
            this.pnl_quotes_section.ResumeLayout(false);
            this.pnl_top.ResumeLayout(false);
            this.pnl_top.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel pnl_root;
        private System.Windows.Forms.Panel pnl_top;
        private System.Windows.Forms.Label lbl_status;
        private System.Windows.Forms.Button btn_refresh;
        private System.Windows.Forms.Panel pnl_body;
        private System.Windows.Forms.Panel pnl_quotes_section;
        private System.Windows.Forms.Label lbl_quotes_header;
        private System.Windows.Forms.FlowLayoutPanel pnl_quotes;
        private System.Windows.Forms.Panel pnl_retention_section;
        private System.Windows.Forms.Label lbl_retention_header;
        private System.Windows.Forms.FlowLayoutPanel pnl_retention;
    }
}
