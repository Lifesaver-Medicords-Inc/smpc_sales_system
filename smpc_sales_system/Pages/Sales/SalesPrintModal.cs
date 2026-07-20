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
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static smpc_sales_system.Models.SalesPrintModel;

namespace smpc_sales_system.Pages.Sales
{
    public partial class SalesPrintModal : Form
    {
        private string documentNo;
        private bool isQuotation;
        private bool isProject;
        private string inclusion;
        private string exclusion;
        private string termsAndCondition;
        string branchName = "Branch not found";
        List<string> unitprices = new List<string>();
        string addressName = "Address not found";

        public SalesPrintModal(bool isQuotation = false, bool isProject = false, string documentNo = null, 
            string inclusion = null, string exclusion = null, string termsAndCondition = null)
        {
            InitializeComponent();
            this.documentNo = documentNo;
            this.isQuotation = isQuotation;
            this.isProject = isProject;
            this.inclusion = inclusion;
            this.exclusion = exclusion;
            this.termsAndCondition = termsAndCondition;
        }
        public DataTable OrderList { get; set; } = new DataTable();
        public DataTable DetailsList { get; set; } = new DataTable();
        public DataTable allTransactionList { get; set; } = new DataTable();
        public DataTable transactionList { get; set; } = new DataTable();
        public DataTable childList { get; set; } = new DataTable();
        public DataTable selectedImageList { get; set; } = new DataTable();
        public DataTable ImageList { get; set; } = new DataTable();
        public DataTable ItemList { get; set; } = new DataTable();
        public DataTable ItemSets { get; set; } = new DataTable();
        public DataTable ItemSetContent { get; set; } = new DataTable();
        public DataTable ProjectItemList { get; set; } = new DataTable();
        public DataTable OriginalProjectItemList { get; set; } = new DataTable();
        private DataTable bpi_general = new DataTable();
        private DataTable bpi_address = new DataTable();
        //FETCHERS OF DATA METHODS
        private async Task fetchItemData()
        {
            var itemData = await ItemService.GetItem();
            ItemList = JsonHelper.ToDataTable(itemData.items);
            ImageList = JsonHelper.ToDataTable(itemData.ItemImages);
        }
        private async Task fetchBpiData()
        {
            Bpi_Class bpi_data = await QuotationService.GetBpiCustomers();
            bpi_general = JsonHelper.ToDataTable(bpi_data.general);
            bpi_address = JsonHelper.ToDataTable(bpi_data.address);
        }
        private async Task fetchQuotationDetailsByDocumentNo(string documentNo)
        {
            SalesQuotationList data = await QuotationService.GetQuotations();

            //SalesQuotationSelectedImageModel imageData = await QuotationService.GetItems();
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

                var idsQuotationQuick = filteredSalesQuotationQuick.Select(q => q.id).ToList();

                var filteredSalesQuotationImage = data.SalesQuotationSelectedImages
                    .Where(q => idsQuotationQuick.Contains(q.quotation_quick_id))
                    .ToList();

                transactionList = JsonHelper.ToDataTable(filteredSalesQuotation);
                childList = JsonHelper.ToDataTable(filteredSalesQuotationQuick);
                selectedImageList = JsonHelper.ToDataTable(filteredSalesQuotationImage);



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
                    .Where(item => item.template_id == 0)
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
            // Must finish before anything below touches ItemList/bpi_general/bpi_address -
            // these used to be fired-and-forgotten from the constructor, which raced with
            // this handler and could leave those tables empty (no columns at all), causing
            // "Cannot find column [id]" when .Select() ran against them.
            await fetchBpiData();
            await fetchItemData();

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

                        if (bpi_general == null && bpi_address == null)
                        {
                            return;
                        }

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
                        List<int> qty = new List<int>();
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
                                        string shortDesc = string.IsNullOrEmpty(itemRow["short_desc"].ToString()) ? " " : itemRow["short_desc"].ToString();
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
                                        string shortDesc = itemRow["item_set_description"].ToString() == "" ? "none" : itemRow["item_set_description"].ToString();
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
                                    int qtys;

                                    if (templateId == 0)
                                    {
                                        qtys = int.Parse(componentdetailRow["qty"].ToString());
                                        qty.Add(qtys);
                                    }
                                    else
                                    {
                                        qtys = int.Parse(itemrows[0]["no_of_sets"].ToString() == "" ? "0" : itemrows[0]["no_of_sets"].ToString());
                                        qty.Add(qtys);
                                    }
                                }
                            }
                        }

                        List<SalesProjectQuotationDetailsReportModel> QuotationDetails = new List<SalesProjectQuotationDetailsReportModel>();

                        foreach (DataRow itemSetRow in ItemList.Select())
                        {

                            int itemSetId = (int)itemSetRow["item_set_id"];
                            var filterComponentItemRows = ProjectItemList.Select($"based_id = '{itemSetId}' ");

                            QuotationDetails.Add(new SalesProjectQuotationDetailsReportModel
                            {
                                items_id = 0,
                                bom_id = 0,
                                item_id = 0,
                                based_id = 0,
                                reference_code = "0",
                                man_days = 0,
                                labor_rate = 0,
                                components = itemSetRow["tab_number"].ToString(),
                                model = " ",
                                item_inv_type = " ",
                                qty = 0,
                                list_price_per_unit = 0,
                                unit_price = 0,
                                multiplier = " ",
                                discount_price = 0,
                                component_total = 0,
                                notes = " ",
                                template_id = 0
                            });


                            foreach (DataRow componentItemRow in filterComponentItemRows)
                            {
                                QuotationDetails.Add(new SalesProjectQuotationDetailsReportModel
                                {
                                    items_id = (int)componentItemRow["items_id"],
                                    bom_id = (int)componentItemRow["bom_id"],
                                    item_id = (int)componentItemRow["bom_id"],
                                    based_id = (int)componentItemRow["based_id"],
                                    reference_code = componentItemRow["reference_code"].ToString(),
                                    man_days = (int)componentItemRow["man_days"],
                                    labor_rate = (decimal)componentItemRow["labor_rate"],
                                    components = componentItemRow["components"].ToString(),
                                    model = componentItemRow["model"].ToString(),
                                    item_inv_type = componentItemRow["item_inv_type"].ToString(),
                                    qty = (int)componentItemRow["qty"],
                                    list_price_per_unit = (decimal)componentItemRow["list_price_per_unit"],
                                    unit_price = (decimal)componentItemRow["unit_price"],
                                    multiplier = componentItemRow["multiplier"].ToString(),
                                    discount_price = (decimal)componentItemRow["discount_price"],
                                    component_total = (decimal)componentItemRow["component_total"],
                                    notes = componentItemRow["notes"].ToString(),
                                    template_id = (int)componentItemRow["template_id"]
                                });


                            }

                        }

                        string[] detailsArray = details.ToArray();
                        string[] itemDescriptionArray = itemDescriptions.ToArray();
                        int[] qtyArray = qty.ToArray();
                        string[] unitpricesArray = unitprices.ToArray();
                        float[] unitpricesFloatArray = unitpricesArray.Select(x => float.Parse(x)).ToArray();
                        float unitpricesSum = unitpricesFloatArray.Sum();
                        int[] qtytotalArray = qtyArray.Select(x => x).ToArray();
                        int qtySum = qtytotalArray.Sum();

                        ReportParameter detailParameter = new ReportParameter("details", detailsArray);
                        ReportParameter qtyParameter = new ReportParameter("qty", qtyArray.ToString());
                        ReportParameter itemDescriptionParameter = new ReportParameter("ItemDescriptions", itemDescriptionArray);
                        ReportParameter unitpricesParameter = new ReportParameter("unitprices", unitpricesArray);
                        ReportParameter unitpricesSumParameter = new ReportParameter("unitpricesSum", unitpricesSum.ToString()); 
                        ReportParameter qtySumParameter = new ReportParameter("qtySum", qtySum.ToString());

                        ReportParameter branchNameParameter = new ReportParameter("BranchName", branchName);
                        ReportParameter addressNameParameter = new ReportParameter("AddressName", addressName);
                        ReportDataSource headerReportDataSource = new ReportDataSource("DataSet1", transactionList);
                        ReportDataSource childReportDataSource = new ReportDataSource("DataSet2", ItemSetContent);
                        ReportDataSource ComponentsReportDataSource = new ReportDataSource("DataSet3", QuotationDetails);

                        reportViewer1.LocalReport.ReportPath = Path.Combine(Settings.Default.REPORTPATH, "ProjectReport.rdlc");
                        reportViewer1.LocalReport.DataSources.Clear();
                        reportViewer1.LocalReport.DataSources.Add(headerReportDataSource);
                        reportViewer1.LocalReport.DataSources.Add(childReportDataSource);
                        //reportViewer1.LocalReport.DataSources.Add(ComponentsReportDataSource);
                        //reportViewer1.LocalReport.DataSources.Add(itemSetDataSource);
                        reportViewer1.LocalReport.SubreportProcessing += new SubreportProcessingEventHandler(MapSubreportData);
                        reportViewer1.LocalReport.SetParameters(new ReportParameter[] { branchNameParameter, qtySumParameter, qtyParameter, addressNameParameter, unitpricesParameter, unitpricesSumParameter, itemDescriptionParameter, detailParameter });
                        reportViewer1.RefreshReport();
                    }

                }
                else
                {
                    MessageBox.Show("No quotation data available for the report.");
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

                        if(bpi_general == null || bpi_general.Rows.Count == 0 
                           || bpi_address == null || bpi_address.Rows.Count == 0)
                        {
                            return;
                        }

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

                                string shortDesc = quoteRow["short_description"].ToString();

                                if (shortDesc != "")
                                {
                                    itemDescriptions.Add(shortDesc);
                                }
                                else
                                {
                                    foreach (DataRow itemRow in itemrows)
                                    {
                                        shortDesc = string.IsNullOrEmpty(itemRow["short_desc"].ToString()) ? " " : itemRow["short_desc"].ToString();
                                        string itemModel = itemRow["item_model"].ToString();
                                        string itemDescription = $"{shortDesc}";

                                        itemDescriptions.Add(itemDescription);
                                    }
                                }

                            }
                        }

                        // Add Image column with appropriate type
                        if (!childList.Columns.Contains("Image"))
                        {
                            childList.Columns.Add("Image", typeof(byte[]));
                        }

                        foreach (DataRow childRow in childList.Rows)
                        {
                            int quotationQuickId = (int)childRow["id"];

                            var matchingImageRows = selectedImageList != null
                                ? selectedImageList.AsEnumerable()
                                    .Where(row => row.Field<int>("quotation_quick_id") == quotationQuickId)
                                    .ToList()
                                : new List<DataRow>();

                            // Prefer the row explicitly marked as selected for this line item
                            DataRow matchedImageRow = matchingImageRows
                                .FirstOrDefault(row => row.Field<bool>("is_selected"))
                                ?? matchingImageRows.FirstOrDefault();

                            if (matchedImageRow != null)
                            {
                                int ImageId = matchedImageRow.Field<int>("image_id");

                                string imageName = ImageList.AsEnumerable()
                                    .Where(row => row.Field<int>("id") == ImageId)
                                    .Select(row => row.Field<string>("image"))
                                    .FirstOrDefault();

                                if(imageName != null)
                                {
                                    byte[] imageBytes = LoadImageAsBytes(imageName);
                                    childRow["Image"] = imageBytes;
                                }
                                else
                                {
                                    childRow["Image"] = DBNull.Value;
                                }
                            }
                            else
                            {
                                childRow["Image"] = DBNull.Value;
                            }

                        }


                        string[] itemDescriptionArray = itemDescriptions.ToArray();
                        ReportParameter itemDescriptionParameter = new ReportParameter("ItemDescriptions", itemDescriptionArray);
                        ReportParameter branchNameParameter = new ReportParameter("BranchName", branchName);
                        ReportParameter addressNameParameter = new ReportParameter("AddressName", addressName);
                        ReportParameter inclusionParameter = new ReportParameter("Inclusion", inclusion);
                        ReportParameter exclusionParameter = new ReportParameter("Exclusion", exclusion);
                        ReportParameter termAndConditionsParameter = new ReportParameter("TermsAndConditions", termsAndCondition);
                        ReportDataSource headerReportDataSource = new ReportDataSource("DataSet1", transactionList);
                        ReportDataSource childReportDataSource = new ReportDataSource("DataSet2", childList);

                        reportViewer1.LocalReport.ReportPath = Path.Combine(Settings.Default.REPORTPATH, "QuotationReport.rdlc");
                        reportViewer1.LocalReport.DataSources.Clear();
                        reportViewer1.LocalReport.DataSources.Add(headerReportDataSource);
                        reportViewer1.LocalReport.DataSources.Add(childReportDataSource);
                        reportViewer1.LocalReport.SetParameters(new ReportParameter[] { branchNameParameter, addressNameParameter, itemDescriptionParameter, inclusionParameter, exclusionParameter, termAndConditionsParameter });
                        //reportViewer1.LocalReport.SetParameters(parameters.ToArray());
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
                        MessageBox.Show("Document not found in this quotation for the report.");
                    }
                }
                else
                {
                    MessageBox.Show("No quotation data available for the report.");
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

        private byte[] LoadImageAsBytes(string imageName)
        {
            if (string.IsNullOrEmpty(imageName))
                return null;

            string imagePath = Path.Combine(Properties.Settings.Default.imagePath,imageName);

            if (imagePath.StartsWith("http://") || imagePath.StartsWith("https://"))
            {
                try
                {
                    using (HttpClient client = new HttpClient())
                    {
                        byte[] imageBytes = client.GetByteArrayAsync(imagePath).Result;
                        return imageBytes;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error downloading image from URL: {ex.Message}");
                    return null;
                }
            }

            return null;
        }

        void MapSubreportData(object sender, SubreportProcessingEventArgs e)
        {
            // 1. Get the parameter passed from the main report row
            int parentId = Convert.ToInt32(e.Parameters["ParentID"].Values[0]);

            // 2. Fetch ONLY the details that match this specific ParentID
            DataTable detailData = ProjectItemList.AsEnumerable().
                                Where(x => x.Field<int>("based_id") == parentId).
                                CopyToDataTable();

            // 3. Bind it to the subreport (Must match the dataset name inside the subreport RDLC)
            e.DataSources.Add(new ReportDataSource("DetailsDataSetName", detailData));
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
       
    

