
namespace smpc_sales_system.Pages.Sales
{
    partial class SalesPrintModal
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SalesPrintModal));
            this.toolStrip1 = new System.Windows.Forms.ToolStrip();
            this.btn_prev = new System.Windows.Forms.ToolStripButton();
            this.panel6 = new System.Windows.Forms.Panel();
            this.label1 = new System.Windows.Forms.Label();
            this.pnl_print = new System.Windows.Forms.Panel();
            this.pnl_dgv = new System.Windows.Forms.Panel();
            this.reportViewer1 = new Microsoft.Reporting.WinForms.ReportViewer();
            this.printDialog1 = new System.Windows.Forms.PrintDialog();
            this.toolStrip1.SuspendLayout();
            this.panel6.SuspendLayout();
            this.pnl_print.SuspendLayout();
            this.pnl_dgv.SuspendLayout();
            this.SuspendLayout();
            // 
            // toolStrip1
            // 
            this.toolStrip1.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
            this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.btn_prev});
            this.toolStrip1.Location = new System.Drawing.Point(0, 47);
            this.toolStrip1.Name = "toolStrip1";
            this.toolStrip1.RenderMode = System.Windows.Forms.ToolStripRenderMode.Professional;
            this.toolStrip1.Size = new System.Drawing.Size(800, 25);
            this.toolStrip1.TabIndex = 14;
            this.toolStrip1.Text = "toolStrip1";
            // 
            // btn_prev
            // 
            this.btn_prev.Image = ((System.Drawing.Image)(resources.GetObject("btn_prev.Image")));
            this.btn_prev.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.btn_prev.Name = "btn_prev";
            this.btn_prev.Size = new System.Drawing.Size(52, 22);
            this.btn_prev.Text = "Back";
            this.btn_prev.Click += new System.EventHandler(this.btn_prev_Click);
            // 
            // panel6
            // 
            this.panel6.Controls.Add(this.label1);
            this.panel6.Dock = System.Windows.Forms.DockStyle.Top;
            this.panel6.Location = new System.Drawing.Point(0, 0);
            this.panel6.Name = "panel6";
            this.panel6.Size = new System.Drawing.Size(800, 47);
            this.panel6.TabIndex = 13;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 15.75F);
            this.label1.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.label1.Location = new System.Drawing.Point(18, 10);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(138, 25);
            this.label1.TabIndex = 1;
            this.label1.Text = "Print Preview";
            // 
            // pnl_print
            // 
            this.pnl_print.Controls.Add(this.pnl_dgv);
            this.pnl_print.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnl_print.Location = new System.Drawing.Point(0, 72);
            this.pnl_print.Name = "pnl_print";
            this.pnl_print.Size = new System.Drawing.Size(800, 686);
            this.pnl_print.TabIndex = 15;
            // 
            // pnl_dgv
            // 
            this.pnl_dgv.Controls.Add(this.reportViewer1);
            this.pnl_dgv.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnl_dgv.Location = new System.Drawing.Point(0, 0);
            this.pnl_dgv.Name = "pnl_dgv";
            this.pnl_dgv.Size = new System.Drawing.Size(800, 686);
            this.pnl_dgv.TabIndex = 1;
            // 
            // reportViewer1
            // 
            this.reportViewer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.reportViewer1.LocalReport.ReportEmbeddedResource = "smpc_sales_system.Pages.Sales.QuotationReport.rdlc";
            this.reportViewer1.Location = new System.Drawing.Point(0, 0);
            this.reportViewer1.Name = "reportViewer1";
            this.reportViewer1.ServerReport.BearerToken = null;
            this.reportViewer1.Size = new System.Drawing.Size(800, 686);
            this.reportViewer1.TabIndex = 0;
            // 
            // printDialog1
            // 
            this.printDialog1.UseEXDialog = true;
            // 
            // SalesPrintModal
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 758);
            this.Controls.Add(this.pnl_print);
            this.Controls.Add(this.toolStrip1);
            this.Controls.Add(this.panel6);
            this.Name = "SalesPrintModal";
            this.Text = "QuotationPrintModal";
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.SalesPrintModal_FormClosed);
            this.Load += new System.EventHandler(this.SalesPrintModal_Load);
            this.toolStrip1.ResumeLayout(false);
            this.toolStrip1.PerformLayout();
            this.panel6.ResumeLayout(false);
            this.panel6.PerformLayout();
            this.pnl_print.ResumeLayout(false);
            this.pnl_dgv.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.ToolStripButton btn_prev;
        private System.Windows.Forms.Panel panel6;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Panel pnl_print;
        private System.Windows.Forms.Panel pnl_dgv;
        private System.Windows.Forms.PrintDialog printDialog1;
        private Microsoft.Reporting.WinForms.ReportViewer reportViewer1;
    }
}