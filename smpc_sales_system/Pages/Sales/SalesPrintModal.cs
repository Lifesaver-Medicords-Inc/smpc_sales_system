using Microsoft.Reporting.WinForms;
using smpc_sales_app.Pages.Sales;
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
    public partial class SalesPrintModal : Form
    {
        private string documentNo;
        private bool isQuotation;
        public SalesPrintModal(bool isQuotation = false, string documentNo = null)
        {
            InitializeComponent();
            fetchBpiData();
            fetchItemData();
            this.documentNo = documentNo;
            this.isQuotation = isQuotation;
        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void label2_Click(object sender, EventArgs e)
        {

        }
        public DataTable OrderList { get; set; } = new DataTable();
        public DataTable DetailsList { get; set; } = new DataTable();
        public DataTable allTransactionList { get; set; } = new DataTable();
        public DataTable transactionList { get; set; } = new DataTable();
        public DataTable childList { get; set; } = new DataTable();
        public DataTable ItemList { get; set; } = new DataTable();
        private DataTable bpi_dt = new DataTable();
        private DataTable bpi_general = new DataTable();
        private DataTable bpi_address = new DataTable();
        private DataTable bpi_contacts = new DataTable();

        //        private async 
        //        Task
        //fetchQuotationDetails()
        //        {
        //            SalesQuotationList data = await QuotationService.GetQuotations();

        //            if (data != null && data.SalesQuotation != null && data.SalesQuotation.Any())
        //            {
        //                // Version filter
        //                var latestQuotations = data.SalesQuotation
        //                    .GroupBy(q => q.document_no)
        //                    .Select(group => group.OrderByDescending(q => q.version_no)
        //                    .First())
        //                    .ToList();

        //                // GET the latest version
        //                transactionList = JsonHelper.ToDataTable(latestQuotations);
        //                allTransactionList = JsonHelper.ToDataTable(data.SalesQuotation);
        //                childList = JsonHelper.ToDataTable(data.SalesQuotationQuick);

        //            }
        //            else
        //            {
        //                MessageBox.Show("Please create a new data!");
        //            }
        //        }
        private async void fetchItemData()
        {
            var itemData = await ItemService.GetItem();
            ItemList = JsonHelper.ToDataTable(itemData.items);
        }
        private async void fetchBpiData()
        {
            Bpi_Class bpi_data = await QuotationService.GetBpiCustomers();
            bpi_dt = JsonHelper.ToDataTable(bpi_data.bpi);
            bpi_general = JsonHelper.ToDataTable(bpi_data.general);
            bpi_address = JsonHelper.ToDataTable(bpi_data.address);
            bpi_contacts = JsonHelper.ToDataTable(bpi_data.contacts);
        }
        private async Task fetchQuotationDetailsByDocumentNo(string documentNo)
        {
            // Get all the quotations from the service
            SalesQuotationList data = await QuotationService.GetQuotations();
            var itemData = await ItemService.GetItem();
            ItemList = JsonHelper.ToDataTable(itemData.items);
            // Check if data is valid
            if (data == null || string.IsNullOrEmpty(documentNo))
            {
                return;  // Exit if no data or documentNo is provided
            }
            // Filter the SalesQuotation and SalesQuotationQuick based on the converted documentNo
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

                // If filtered data exists, bind it to the DataGridView
                if (filteredSalesQuotation.Any() || filteredSalesQuotationQuick.Any())
                {
                    //bind(true);
                }
                else
                {
                    MessageBox.Show("No records found for the provided document number.");
                }
            }
            else
            {
                MessageBox.Show("No SalesQuotation found for the provided document number.");
            }
        }
        private async Task fetchOrderDetailsByDocumentNo(string documentNo)
        {
            // Get all the quotations from the service
            OrderList data = await OrderService.GetOrders();
 
            if (data == null || string.IsNullOrEmpty(documentNo))
            {
                return;  // Exit if no data or documentNo is provided
            }
            // Filter the SalesQuotation and SalesQuotationQuick based on the converted documentNo
            var filteredSalesOrder = data.order
                .Where(q => q.doc == documentNo)  // Assuming document_no is int
                .ToList();

            var orderId = filteredSalesOrder.FirstOrDefault()?.order_id;

            if (orderId != null)
            {
                var filteredSalesOrderDetails = data.sales_order_details
                    .Where(q => q.based_id == orderId)  // Filter by based_id, converted to int
                    .ToList();
                // Convert the filtered lists to DataTables (using your helper method)
                OrderList = JsonHelper.ToDataTable(filteredSalesOrder);
                DetailsList = JsonHelper.ToDataTable(filteredSalesOrderDetails);

                // If filtered data exists, bind it to the DataGridView
                if (filteredSalesOrder.Any() || filteredSalesOrderDetails.Any())
                {
                    //bind(true);
                }
                else
                {
                    MessageBox.Show("No records found for the provided document number.");
                }
            }
            else
            {
                MessageBox.Show("No SalesQuotation found for the provided document number.");
            }
        }

        private async void QuotationPrintModal_Load(object sender, EventArgs e)
        {
            if (isQuotation)
            {
                await fetchQuotationDetailsByDocumentNo(documentNo);

                if (transactionList != null && transactionList.Rows.Count > 0)
                {
                    // Filter the transactionList based on document_no (use the passed documentNo)  
                    DataRow[] filteredRows = transactionList.Select($"document_no = '{documentNo}'");

                    if (filteredRows.Length > 0)
                    {
                        int Id = (int)filteredRows[0]["id"];
                        int customerId = (int)filteredRows[0]["customer_id"];
                        int shiptoId = (int)filteredRows[0]["ship_to_id"];

                        DataRow[] bpiRows = bpi_general.Select($"general_based_id = '{customerId}'");
                        DataRow[] bpiaddrows = bpi_address.Select($"address_id = '{shiptoId}'");
                        string addressName = "Address not found";
                        addressName = bpiaddrows[0]["location"].ToString();

                        string branchName = "Branch not found";
                        if (bpiRows.Length > 0)
                        {
                            branchName = bpiRows[0]["branch_name"].ToString();
                        }

                        DataRow[] quotequoteRows = childList.Select($"based_id = '{Id}'");
                        List<string> itemDescriptions = new List<string>();
                        if (quotequoteRows.Length > 0)
                        {
                            foreach (DataRow quoteRow in quotequoteRows)
                            {
                                int itemid = (int)quoteRow["item_id"];
                                DataRow[] itemrows = ItemList.Select($"id = '{itemid}'");

                                foreach (DataRow itemRow in itemrows)
                                {
                                    string shortDesc = itemRow["short_desc"].ToString();
                                    string itemModel = itemRow["item_model"].ToString();

                                    // Concatenate the item_model and short_desc in the desired format
                                    //string itemDescription = $"{itemModel} - {shortDesc}";
                                    string itemDescription = $"{shortDesc}";

                                    itemDescriptions.Add(itemDescription);
                                }
                            }
                        }
                        string[] itemDescriptionArray = itemDescriptions.ToArray();
                        ReportParameter itemDescriptionParameter = new ReportParameter("ItemDescriptions", itemDescriptionArray);

                        ReportParameter branchNameParameter = new ReportParameter("BranchName", branchName);
                        ReportParameter addressNameParameter = new ReportParameter("AddressName", addressName);
                        ReportDataSource headerReportDataSource = new ReportDataSource("DataSet1", transactionList);
                        ReportDataSource childReportDataSource = new ReportDataSource("DataSet2", childList);

                        reportViewer1.LocalReport.ReportPath = @"C:\Users\SMPC\source\repos\smpc_sales_system\smpc_sales_system2\smpc_sales_system\Pages\Sales\QuotationReport.rdlc";
                        reportViewer1.LocalReport.DataSources.Clear();
                        reportViewer1.LocalReport.DataSources.Add(headerReportDataSource);
                        reportViewer1.LocalReport.DataSources.Add(childReportDataSource);
                        reportViewer1.LocalReport.SetParameters(new ReportParameter[] { branchNameParameter, addressNameParameter, itemDescriptionParameter });
                        reportViewer1.RefreshReport();
                    }
                    else
                    {
                        MessageBox.Show("No quotation data available for the report.");
                    }
                }
            }
            else
            {
                await fetchOrderDetailsByDocumentNo(documentNo);
                if (OrderList != null && OrderList.Rows.Count > 0)
                {
                    // Filter the transactionList based on document_no (use the passed documentNo)  
                    DataRow[] filteredRows = OrderList.Select($"doc = '{documentNo}'");

                    if (filteredRows.Length > 0)
                    {

                        int customerId = Convert.ToInt32(filteredRows[0]["customer_id"]);
                        int shiptoId = Convert.ToInt32(filteredRows[0]["ship_to_id"]);
                        int billtoId = Convert.ToInt32(filteredRows[0]["bill_to_id"]);

                        DataRow[] bpiRows = bpi_general.Select($"general_based_id = '{customerId}'");
                        DataRow[] bpishipaddrows = bpi_address.Select($"address_id = '{shiptoId}'");
                        DataRow[] bpibilladdrows = bpi_address.Select($"address_id = '{billtoId}'");
                        string shipaddressName = "Address not found";
                        string billaddressName = "Address not found";
                        shipaddressName = bpishipaddrows[0]["location"].ToString();
                        billaddressName = bpibilladdrows[0]["location"].ToString();

                        string branchName = "Branch not found";
                        string codeName = "Code not found";
                        if (bpiRows.Length > 0)
                        {
                            branchName = bpiRows[0]["branch_name"].ToString();
                            codeName = bpiRows[0]["customer_code"].ToString();
                        }

                        ReportParameter branchNameParameter = new ReportParameter("BranchName", branchName);
                        ReportParameter shipaddressNameParameter = new ReportParameter("ShipName", shipaddressName);
                        ReportParameter billaddressNameParameter = new ReportParameter("BillName", billaddressName);
                        ReportParameter codeNameParameter = new ReportParameter("CodeName", codeName);
                        ReportDataSource headerReportDataSource = new ReportDataSource("DataSet1", OrderList);
                        ReportDataSource childReportDataSource = new ReportDataSource("DataSet2", DetailsList);

                        reportViewer1.LocalReport.ReportPath = @"C:\Users\SMPC\source\repos\smpc_sales_system\smpc_sales_system2\smpc_sales_system\Pages\Sales\OrderReport.rdlc";
                        reportViewer1.LocalReport.DataSources.Clear();
                        reportViewer1.LocalReport.DataSources.Add(headerReportDataSource);
                        reportViewer1.LocalReport.DataSources.Add(childReportDataSource);
                        reportViewer1.LocalReport.SetParameters(new ReportParameter[] { branchNameParameter, shipaddressNameParameter, billaddressNameParameter, codeNameParameter });
                        reportViewer1.RefreshReport();
                    }
                    else
                    {
                        MessageBox.Show("No quotation data available for the report.");
                    }
                }
            }
        }

        private void btn_prev_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
       
    

