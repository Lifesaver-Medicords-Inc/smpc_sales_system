using Microsoft.Reporting.WinForms;
using System;
using System.Windows.Forms;

namespace smpc_sales_system.Printing
{
    // The one preview window every house-template print opens through, so the
    // toolbar, zoom and export options are the same wherever a list is printed.
    public partial class PrintPreview : Form
    {
        public PrintPreview(HouseTemplateReport report)
        {
            InitializeComponent();

            reportViewer1.LocalReport.ReportPath = report.ReportPath;
            reportViewer1.LocalReport.DataSources.Clear();

            foreach (var source in report.DataSources())
                reportViewer1.LocalReport.DataSources.Add(source);

            reportViewer1.LocalReport.SetParameters(report.Parameters());
            reportViewer1.ZoomMode = ZoomMode.PageWidth;
            reportViewer1.RefreshReport();
        }
    }
}
