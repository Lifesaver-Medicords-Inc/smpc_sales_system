using Microsoft.Reporting.WinForms;
using smpc_sales_app.Services.Helpers;
using smpc_sales_app.Services.Sales;
using smpc_sales_system.Models;
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
    public partial class QuotationPrintModal : Form
    {
        private string documentNo;
        private string[] img;
        private string[] desc;
        private string[] qtys;
        private string[] unitprice;
        private string[] percentdiscount;
        private string[] amount;

        private string docno;
        private string date;
        private string company;
        private string address;
        private string receiver;
        private string exec;

        private string subtotal;
        private string adddiscount;
        private string cashdiscount;
        private string grandtotal;

        private string inclusion;
        private string exclusion;
        private string terms;

        public QuotationPrintModal(string[] img, string[] desc, string[] qtys, string[] unitprice,
                                string[] percentdiscount, string[] amount,
                                string docno, string date, string company, string address,
                                string receiver, string exec,
                                string subtotal, string adddiscount, string cashdiscount,
                                string grandtotal,
                                string inclusion, string exclusion, string terms)
        {
            InitializeComponent();  
            this.documentNo = documentNo;
            this.img = img;
            this.desc = desc;
            this.qtys = qtys;
            this.unitprice = unitprice;
            this.percentdiscount = percentdiscount;
            this.amount = amount;

            this.docno = docno;
            this.date = date;
            this.company = company;
            this.address = address;
            this.receiver = receiver;
            this.exec = exec;

            this.subtotal = subtotal;
            this.adddiscount = adddiscount;
            this.cashdiscount = cashdiscount;
            this.grandtotal = grandtotal;

            this.inclusion = inclusion;
            this.exclusion = exclusion;
            this.terms = terms;
        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void label2_Click(object sender, EventArgs e)
        {

        }
        public DataTable transactionList { get; set; } = new DataTable();
        public DataTable childList { get; set; } = new DataTable();
        private async Task fetchQuotationDetailsByDocumentNo(string documentNo)
        {
            SalesQuotationList data = await QuotationService.GetQuotations();
            if (data == null || string.IsNullOrEmpty(documentNo))
            {
                return;  // Exit if no data or documentNo is provided
            }
            var filteredSalesQuotation = data.SalesQuotation
                .Where(q => q.document_no == documentNo)  // Assuming document_no is int
                .ToList();

            var quotationId = filteredSalesQuotation.FirstOrDefault()?.id;

            if (quotationId != null)
            {
                var filteredSalesQuotationQuick = data.SalesQuotationQuick
                    .Where(q => q.based_id == quotationId)  // Filter by based_id, converted to int
                    .ToList();
                // Convert the filtered lists to DataTables (using your helper method)
                transactionList = JsonHelper.ToDataTable(filteredSalesQuotation);
                childList = JsonHelper.ToDataTable(filteredSalesQuotationQuick);
            }
            else
            {
                MessageBox.Show("No SalesQuotation found for the provided document number.");
            }
        }

        private void btn_prev_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private async void QuotationPrintModal_Load(object sender, EventArgs e)
        {
            await fetchQuotationDetailsByDocumentNo(documentNo);

            //HEADER PANEL
            ReportDataSource childReportDataSource = new ReportDataSource("DataSet2", childList);
            ReportParameter docnoParameter = new ReportParameter("docno", docno);
            ReportParameter dateParameter = new ReportParameter("date", date);
            ReportParameter companyParameter = new ReportParameter("company", company);
            ReportParameter addressParameter = new ReportParameter("address", address);
            ReportParameter receiverParameter = new ReportParameter("receiver", receiver);
            ReportParameter execParameter = new ReportParameter("exec", exec);

            //DGV
            ReportParameter descParameter = new ReportParameter("desc", desc);
            //ReportParameter imgParameter = new ReportParameter("img", img);
            ReportParameter qtysParameter = new ReportParameter("qtys", qtys);
            ReportParameter unitpriceParameter = new ReportParameter("unitprice", unitprice);
            ReportParameter percentdiscountParameter = new ReportParameter("percentdiscount", percentdiscount);
            ReportParameter amountParameter = new ReportParameter("amount", amount);

            //FOOTER PANEL
            ReportParameter subtotalParameter = new ReportParameter("subtotal", subtotal);
            ReportParameter adddiscountParameter = new ReportParameter("adddiscount", adddiscount);
            ReportParameter cashdiscountParameter = new ReportParameter("cashdiscount", cashdiscount);
            ReportParameter grandtotalParameter = new ReportParameter("grandtotal", grandtotal);

            ReportParameter inclusionParameter = new ReportParameter("inclusion", inclusion);
            ReportParameter exclusionParameter = new ReportParameter("exclusion", exclusion);
            ReportParameter termsParameter = new ReportParameter("terms", terms);
            reportViewer1.LocalReport.ReportPath = @"C:\Users\SMPC\source\repos\smpc_sales_system\smpc_sales_system2\smpc_sales_system\Pages\Sales\QuotationReport.rdlc";
            reportViewer1.LocalReport.DataSources.Clear();
            //, imgParameter
            reportViewer1.LocalReport.SetParameters(new ReportParameter[] {
            descParameter, qtysParameter, unitpriceParameter,
            percentdiscountParameter, amountParameter,
            docnoParameter, dateParameter, companyParameter, addressParameter,
            receiverParameter, execParameter,
            subtotalParameter, adddiscountParameter, cashdiscountParameter, grandtotalParameter,
            inclusionParameter, exclusionParameter, termsParameter
        });

            reportViewer1.LocalReport.DataSources.Add(childReportDataSource);
            reportViewer1.RefreshReport();
                }
                
            }
        }
    

