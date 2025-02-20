using Microsoft.Reporting.WinForms;
using smpc_sales_app.Services.Helpers;
using smpc_sales_app.Services.Sales;
using smpc_sales_system.Services.Sales.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace smpc_sales_system.Pages.Sales
{
    public partial class QuotationPrint : UserControl
    {
        public QuotationPrint()
        {
            InitializeComponent();
        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void label2_Click(object sender, EventArgs e)
        {

        }
        public DataTable allTransactionList { get; set; } = new DataTable();
        public DataTable transactionList { get; set; } = new DataTable();
        public DataTable childList { get; set; } = new DataTable();
        public DataTable ItemList { get; set; } = new DataTable();

        private async 
        Task
fetchQuotationDetails()
        {
            SalesQuotationList data = await QuotationService.GetQuotations();

            if (data != null && data.SalesQuotation != null && data.SalesQuotation.Any())
            {
                // Version filter
                var latestQuotations = data.SalesQuotation
                    .GroupBy(q => q.document_no)
                    .Select(group => group.OrderByDescending(q => q.version_no)
                    .First())
                    .ToList();

                // GET the latest version
                transactionList = JsonHelper.ToDataTable(latestQuotations);
                allTransactionList = JsonHelper.ToDataTable(data.SalesQuotation);
                childList = JsonHelper.ToDataTable(data.SalesQuotationQuick);

            }
            else
            {
                MessageBox.Show("Please create a new data!");
            }
        }
        private async void QuotationPrint_Load(object sender, EventArgs e)
        {
            await fetchQuotationDetails();

            // Assuming the data is fetched and stored in the transactionList, childList, etc.
            if (transactionList != null && transactionList.Rows.Count > 0)
            {
                // Create your report data source
                ReportDataSource reportDataSource = new ReportDataSource("QuotationQuick", childList); // Assuming 'childList' holds the data for your report

                // Set the report path
                reportViewer1.LocalReport.ReportPath = @"C:\Users\SMPC\source\repos\smpc_sales_system\smpc_sales_system2\smpc_sales_system\Pages\Sales\QuotationReport.rdlc";

                // Clear existing data sources and add new one
                reportViewer1.LocalReport.DataSources.Clear();
                reportViewer1.LocalReport.DataSources.Add(reportDataSource);

                // Refresh the report to show the data
                reportViewer1.RefreshReport();
            }
            else
            {
                MessageBox.Show("No quotation data available for the report.");
            }
        }
    }
}
