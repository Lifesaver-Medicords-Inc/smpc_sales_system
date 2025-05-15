using Microsoft.Reporting.WinForms;
using smpc_sales_app.Pages.Sales;
using smpc_sales_app.Services.Helpers;
using smpc_sales_app.Services.Sales;
using smpc_sales_system.Models;
using smpc_sales_system.Properties;
using smpc_sales_system.Services.Sales;
using smpc_sales_system.Services.Sales.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
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
        private bool isProject;
        string branchName = "Branch not found";
        List<string> unitprices = new List<string>();
        string addressName = "Address not found";


        public SalesPrintModal(bool isQuotation = false, bool isProject = false, string documentNo = null)
        {
            InitializeComponent();
            fetchBpiData();
            fetchItemData();
            this.documentNo = documentNo;
            this.isQuotation = isQuotation;
            this.isProject = isProject;
        }
        public DataTable OrderList { get; set; } = new DataTable();
        public DataTable DetailsList { get; set; } = new DataTable();
        public DataTable allTransactionList { get; set; } = new DataTable();
        public DataTable transactionList { get; set; } = new DataTable();
        public DataTable childList { get; set; } = new DataTable();
        public DataTable ItemList { get; set; } = new DataTable();
        public DataTable ItemSets { get; set; } = new DataTable();
        public DataTable ItemSetContent { get; set; } = new DataTable();
        public DataTable ProjectItemList { get; set; } = new DataTable();
        public DataTable OriginalProjectItemList { get; set; } = new DataTable();
        private DataTable bpi_general = new DataTable();
        private DataTable bpi_address = new DataTable();
        //FETCHERS OF DATA METHODS
        private async void fetchItemData()
        {
            var itemData = await ItemService.GetItem();
            ItemList = JsonHelper.ToDataTable(itemData.items);
        }
        private async void fetchBpiData()
        {
            Bpi_Class bpi_data = await QuotationService.GetBpiCustomers();
            bpi_general = JsonHelper.ToDataTable(bpi_data.general);
            bpi_address = JsonHelper.ToDataTable(bpi_data.address);
        }
        private async Task fetchQuotationDetailsByDocumentNo(string documentNo)
        {
            SalesQuotationList data = await QuotationService.GetQuotations();
            if (data == null || string.IsNullOrEmpty(documentNo))
            {
                MessageBox.Show("No document number received");
                return;
            }
            var filteredSalesQuotation = data.SalesQuotation
                .Where(q => q.document_no == documentNo)
                .ToList();
            var quotationId = filteredSalesQuotation.FirstOrDefault()?.id;

            if (quotationId != null)
            {
                var filteredSalesQuotationQuick = data.SalesQuotationQuick
                    .Where(q => q.based_id == quotationId)
                    .ToList();
                transactionList = JsonHelper.ToDataTable(filteredSalesQuotation);
                childList = JsonHelper.ToDataTable(filteredSalesQuotationQuick);

                if (filteredSalesQuotation.Any() || filteredSalesQuotationQuick.Any())
                {
                    //bind(true); continue
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
        private async Task fetchQuotationProjectByDocumentNo(string documentNo)
        {
            SalesProjectList data = await ProjectService.GetProjects();
            if (data == null || string.IsNullOrEmpty(documentNo))
            {
                return;
            }
            var filteredSalesQuotation = data.SalesQuotation
                .Where(q => q.document_no == documentNo)
                .ToList();
            var quotationId = filteredSalesQuotation.FirstOrDefault()?.id;

            if (quotationId != null)
            {
                var filteredItemSets = data.sales_project_item_set
                    .Where(q => q.based_id == quotationId)  
                    .ToList();
                transactionList = JsonHelper.ToDataTable(filteredSalesQuotation);
                ItemSets = JsonHelper.ToDataTable(filteredItemSets);

                var itemsIds = filteredItemSets.Select(q => q.itemset_id).ToList();

                var filteredcontent = data.sales_project_content
                .Where(q => itemsIds.Contains(q.based_id))
                .ToList();
                ItemSetContent = JsonHelper.ToDataTable(filteredcontent);

                var filteredProjectItems = data.sales_project_items
                    .Where(q => itemsIds.Contains(q.based_id))
                    .ToList();

                // Split the data into two lists based on template_id
                var templateGreaterThanZero = filteredProjectItems
                    .Where(item => item.template_id > 0)
                    .GroupBy(item => item.based_id)
                    .Select(group => group.First())
                    .ToList();

                var templateZero = filteredProjectItems
                    .Where(item => item.template_id == 0) // Filter rows where template_id == 0
                    .ToList();

                var filteredProjectItems2 = templateGreaterThanZero.Concat(templateZero).ToList();
                ProjectItemList = JsonHelper.ToDataTable(filteredProjectItems2);
                OriginalProjectItemList = JsonHelper.ToDataTable(filteredProjectItems);
            }
            else
            {
                MessageBox.Show("No Quotation found for the provided document number.");
            }
        }
        private async Task fetchOrderDetailsByDocumentNo(string documentNo)
        {
            OrderList data = await OrderService.GetOrders();
            if (data == null || string.IsNullOrEmpty(documentNo))
            {
                return;  // Exit if no data or documentNo is provided
            }

            var filteredSalesOrder = data.order
                .Where(q => q.doc == documentNo) 
                .ToList();

            var orderId = filteredSalesOrder.FirstOrDefault()?.order_id;

            if (orderId != null)
            {
                var filteredSalesOrderDetails = data.sales_order_details
                    .Where(q => q.based_id == orderId)
                    .ToList();

                OrderList = JsonHelper.ToDataTable(filteredSalesOrder);
                DetailsList = JsonHelper.ToDataTable(filteredSalesOrderDetails);

                if (filteredSalesOrder.Any() || filteredSalesOrderDetails.Any())
                {
                    //bind(true); continue
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
        //ON LOAD FOR PRINTMODAL
        public bool AutoExport { get; set; } = false;
        public string ExportPath { get; set; } = "";
        private async void SalesPrintModal_Load(object sender, EventArgs e)
        {
            if (isProject)
            {
                await fetchQuotationProjectByDocumentNo(documentNo);

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
                        DataRow[] bpiaddrows = bpi_address.Select($"address_ids = '{shiptoId}'");
                        
                        addressName = bpiaddrows[0]["location"].ToString();

                        if (bpiRows.Length > 0)
                        {
                            branchName = bpiRows[0]["branch_name"].ToString();
                        }

                        var itemsetIds = ItemSets.AsEnumerable()
                       .Select(row => row.Field<int>("itemset_id"))  // Assuming 'items_id' is an integer column
                       .ToList();

                        foreach (var itemsetId in itemsetIds)
                        {
                            DataRow[] componentRows = OriginalProjectItemList.Select($"based_id = '{itemsetId}'");

                            float componentTotalSum = 0f;
                            foreach (DataRow row in componentRows)
                            {
                                if ((int)row["template_id"] == 0)
                                {
                                    var componentTotal = row["component_total"];

                                    if (componentTotal != DBNull.Value && !string.IsNullOrWhiteSpace(componentTotal.ToString()))
                                    {
                                        if (float.TryParse(componentTotal.ToString(), out float parsedValue))
                                        {
                                            unitprices.Add(parsedValue.ToString("F2"));
                                        }
                                    }
                                }
                                else
                                {
                                    var componentTotal = row["component_total"];
                                    if (componentTotal != DBNull.Value && !string.IsNullOrWhiteSpace(componentTotal.ToString()))
                                    {
                                        if (float.TryParse(componentTotal.ToString(), out float parsedValue))
                                        {
                                            componentTotalSum += parsedValue;
                                        }
                                    }
                                }
                            }

                            if (componentTotalSum > 0)
                            {
                                unitprices.Add(componentTotalSum.ToString("F2"));
                            }
                        }
                        DataRow[] componentitemRows = ProjectItemList.Select();

                        List<string> itemDescriptions = new List<string>();
                        List<string> details = new List<string>();
                        List<string> qty = new List<string>();
                        if (componentitemRows.Length > 0)
                        {
                            foreach (DataRow componentRow in componentitemRows)
                            {
                                int itemid = (int)componentRow["item_id"];

                                // Check if item_id is 0 and add "N/A" directly
                                if (itemid == 0)
                                {
                                    itemDescriptions.Add("N/A");
                                }
                                else
                                {
                                    // Otherwise, proceed with the selection from ItemList
                                    DataRow[] itemrows = ItemList.Select($"id = '{itemid}'");

                                    foreach (DataRow itemRow in itemrows)
                                    {
                                        string shortDesc = itemRow["short_desc"].ToString();
                                        string itemModel = itemRow["item_model"].ToString();

                                        // Concatenate the item_model and short_desc in the desired format
                                        string itemDescription = $"{shortDesc}";

                                        itemDescriptions.Add(itemDescription);
                                    }
                                }
                            }

                            
                            foreach (DataRow componentdetailRow in componentitemRows)
                            {
                                int itemid = (int)componentdetailRow["based_id"];
                                DataRow[] itemrows = ItemSetContent.Select();

                                    foreach (DataRow itemRow in itemrows)
                                    {
                                        string shortDesc = itemRow["item_set_description"].ToString();
                                        string detail = $"{shortDesc}";
                                        details.Add(detail);
                                    }
                            }

                            foreach (DataRow componentdetailRow in componentitemRows)
                            {
                                int itemid = (int)componentdetailRow["based_id"];   
                                int templateId = (int)componentdetailRow["template_id"];
                                DataRow[] itemrows = ItemSetContent.Select($"based_id = {itemid}");

                                if (itemrows.Length > 0 || componentitemRows.Length > 0)
                                {
                                    string qtys;

                                    if (templateId == 0)
                                    {
                                        qtys = Convert.ToString(componentdetailRow["qty"]);
                                        qty.Add(qtys);
                                    }
                                    else
                                    {
                                        qtys = itemrows[0]["no_of_sets"].ToString();
                                        qty.Add(qtys);
                                    }
                                }
                            }
                        }

                        string[] detailsArray = details.ToArray();
                        string[] itemDescriptionArray = itemDescriptions.ToArray();
                        string[] qtyArray = qty.ToArray();
                        string[] unitpricesArray = unitprices.ToArray();
                        float[] unitpricesFloatArray = unitpricesArray.Select(x => float.Parse(x)).ToArray();
                        float unitpricesSum = unitpricesFloatArray.Sum();
                        int[] qtytotalArray = qtyArray.Select(x => int.Parse(x)).ToArray();
                        int qtySum = qtytotalArray.Sum();

                        ReportParameter detailParameter = new ReportParameter("details", detailsArray);
                        ReportParameter qtyParameter = new ReportParameter("qty", qtyArray);
                        ReportParameter itemDescriptionParameter = new ReportParameter("ItemDescriptions", itemDescriptionArray);
                        ReportParameter unitpricesParameter = new ReportParameter("unitprices", unitpricesArray);
                        ReportParameter unitpricesSumParameter = new ReportParameter("unitpricesSum", unitpricesSum.ToString()); 
                        ReportParameter qtySumParameter = new ReportParameter("qtySum", qtySum.ToString());

                        ReportParameter branchNameParameter = new ReportParameter("BranchName", branchName);
                        ReportParameter addressNameParameter = new ReportParameter("AddressName", addressName);
                        ReportDataSource headerReportDataSource = new ReportDataSource("DataSet1", transactionList);
                        ReportDataSource childReportDataSource = new ReportDataSource("DataSet2", ItemSetContent);
                        ReportDataSource ComponentsReportDataSource = new ReportDataSource("DataSet3", ProjectItemList);

                        reportViewer1.LocalReport.ReportPath = Path.Combine(Settings.Default.REPORTPATH, "ProjectReport.rdlc");
                        reportViewer1.LocalReport.DataSources.Clear();
                        reportViewer1.LocalReport.DataSources.Add(headerReportDataSource);
                        reportViewer1.LocalReport.DataSources.Add(childReportDataSource);
                        reportViewer1.LocalReport.DataSources.Add(ComponentsReportDataSource);
                        reportViewer1.LocalReport.SetParameters(new ReportParameter[] { branchNameParameter, qtySumParameter, qtyParameter, addressNameParameter, unitpricesParameter, unitpricesSumParameter, itemDescriptionParameter, detailParameter });
                        reportViewer1.RefreshReport();
                    }
                    else
                    {
                        MessageBox.Show("No quotation data available for the report.");
                    }
                }
            }
            else if (isQuotation)
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
                        DataRow[] bpiaddrows = bpi_address.Select($"address_ids = '{shiptoId}'");
                        string addressName = "Address not found";
                        if (bpiaddrows.Length > 0)
                        {
                            addressName = bpiaddrows[0]["location"].ToString();
                        }
                        

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

                        reportViewer1.LocalReport.ReportPath = Path.Combine(Settings.Default.REPORTPATH, "QuotationReport.rdlc");
                        reportViewer1.LocalReport.DataSources.Clear();
                        reportViewer1.LocalReport.DataSources.Add(headerReportDataSource);
                        reportViewer1.LocalReport.DataSources.Add(childReportDataSource);
                        reportViewer1.LocalReport.SetParameters(new ReportParameter[] { branchNameParameter, addressNameParameter, itemDescriptionParameter });
                        reportViewer1.RefreshReport();

                        if (AutoExport && !string.IsNullOrWhiteSpace(ExportPath))
                        {
                            Warning[] warnings;
                            string[] streamIds;
                            string mimeType, encoding, extension;

                            byte[] pdfBytes = reportViewer1.LocalReport.Render("PDF", null, out mimeType, out encoding, out extension, out streamIds, out warnings);
                            File.WriteAllBytes(ExportPath, pdfBytes);

                            // Optionally close the form after exporting if shown manually
                            this.Close();
                        }
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
                        DataRow[] bpishipaddrows = bpi_address.Select($"address_ids = '{shiptoId}'");
                        DataRow[] bpibilladdrows = bpi_address.Select($"address_ids = '{billtoId}'");
                        string shipaddressName = "Address not found";
                        string billaddressName = "Address not found";
                        if (bpishipaddrows.Length > 0)
                        {
                            shipaddressName = bpishipaddrows[0]["location"].ToString();
                        }
                        if (bpibilladdrows.Length > 0)
                        {
                            billaddressName = bpibilladdrows[0]["location"].ToString();
                        }
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
                        
                        reportViewer1.LocalReport.ReportPath = Path.Combine(Settings.Default.REPORTPATH, "OrderReport.rdlc");
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
        private void DisposeTables()
        {
            // Public properties
            OrderList?.Dispose(); OrderList = null;
            DetailsList?.Dispose(); DetailsList = null;
            allTransactionList?.Dispose(); allTransactionList = null;
            transactionList?.Dispose(); transactionList = null;
            childList?.Dispose(); childList = null;
            ItemList?.Dispose(); ItemList = null;
            ItemSets?.Dispose(); ItemSets = null;
            ItemSetContent?.Dispose(); ItemSetContent = null;
            ProjectItemList?.Dispose(); ProjectItemList = null;
            OriginalProjectItemList?.Dispose(); OriginalProjectItemList = null;

            // Private fields
            bpi_general?.Dispose(); bpi_general = null;
            bpi_address?.Dispose(); bpi_address = null;
        }

        private void SalesPrintModal_FormClosed(object sender, FormClosedEventArgs e)
        {
            DisposeTables();

            if (reportViewer1 != null)
            {
                reportViewer1.LocalReport.ReleaseSandboxAppDomain();
                reportViewer1.LocalReport.DataSources.Clear();
                reportViewer1.Dispose();
            }

            GC.Collect(); // optional: force immediate cleanup
            GC.WaitForPendingFinalizers();
        }
      

    }
}
       
    

