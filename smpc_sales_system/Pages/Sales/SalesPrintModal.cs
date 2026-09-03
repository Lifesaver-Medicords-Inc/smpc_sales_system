using Microsoft.Reporting.WinForms;
using smpc_sales_app.Pages.Sales;
using smpc_sales_app.Services.Helpers;
using smpc_sales_app.Services.Sales;
using smpc_sales_app.Services.Sales.Models;
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
using System.Text.RegularExpressions;
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
        // Older records can still have "Q#"/"FQ#" baked into their stored document_no (a
        // now-fixed save bug used to persist it that way), while documentNo passed in here
        // is always the bare number. Stripping both sides the same way before comparing
        // means lookups work for old and new records alike, without needing a database
        // migration to clean up the existing prefixed values.
        // Delegates to the shared DocumentNo helper (fully qualified - this file uses the
        // sibling smpc_sales_app.Services.Helpers namespace, so it can't rely on a short name).
        private static string NormalizeDocumentNo(string docNo) =>
            smpc_app.Services.Helpers.DocumentNo.Strip(docNo);

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
        // Returns true once transactionList has actually been populated, so the caller
        // knows whether it's safe to keep going or whether this method already showed the
        // relevant error (avoids stacking a second, redundant "no data" message box for the
        // same underlying "not found" failure).
        private async Task<bool> fetchQuotationDetailsByDocumentNo(string documentNo)
        {
            SalesQuotationList data = await QuotationService.GetQuotations();

            //SalesQuotationSelectedImageModel imageData = await QuotationService.GetItems();
            if (data == null || string.IsNullOrEmpty(documentNo))
            {
                MessageBox.Show("No document number received");
                return false;
            }
            // Any of these can legitimately come back null from the API (e.g. an empty
            // array serializes fine, but a missing/omitted field deserializes to null) -
            // calling .Where() directly on a null source throws ArgumentNullException, so
            // fall back to an empty list instead of assuming these are always populated.
            var filteredSalesQuotation = (data.SalesQuotation ?? Enumerable.Empty<SalesQuotationModel>())
                .Where(q => NormalizeDocumentNo(q.document_no) == documentNo)
                .ToList();
            var quotationId = filteredSalesQuotation.FirstOrDefault()?.id;

            if (quotationId != null)
            {
                var filteredSalesQuotationQuick = (data.SalesQuotationQuick ?? Enumerable.Empty<SalesQuotationQuicksModel>())
                    .Where(q => q.based_id == quotationId)
                    .ToList();

                var idsQuotationQuick = filteredSalesQuotationQuick.Select(q => q.id).ToList();

                var filteredSalesQuotationImage = (data.SalesQuotationSelectedImages ?? Enumerable.Empty<SalesQuotationSelectedImageModel>())
                    .Where(q => idsQuotationQuick.Contains(q.quotation_quick_id))
                    .ToList();

                transactionList = JsonHelper.ToDataTable(filteredSalesQuotation);
                childList = JsonHelper.ToDataTable(filteredSalesQuotationQuick);
                selectedImageList = JsonHelper.ToDataTable(filteredSalesQuotationImage);



                if (filteredSalesQuotation.Any() || filteredSalesQuotationQuick.Any())
                {
                    //bind(true); continue
                    return true;
                }
                else
                {
                    MessageBox.Show("No records found for the provided document number.");
                    return false;
                }
            }
            else
            {
                MessageBox.Show("No SalesQuotation found for the provided document number.");
                return false;
            }
        }
        private async Task fetchQuotationProjectByDocumentNo(string documentNo)
        {
            SalesProjectList data = await ProjectService.GetProjects();
            if (data == null || string.IsNullOrEmpty(documentNo))
            {
                return;
            }
            // Any of these can legitimately come back null from the API - fall back to an
            // empty list instead of letting .Where() throw ArgumentNullException on a null
            // source.
            var filteredSalesQuotation = (data.SalesQuotation ?? Enumerable.Empty<SalesQuotationModel>())
                .Where(q => NormalizeDocumentNo(q.document_no) == documentNo)
                .ToList();
            var quotationId = filteredSalesQuotation.FirstOrDefault()?.id;

            if (quotationId != null)
            {
                var filteredItemSets = (data.sales_project_item_set ?? Enumerable.Empty<SalesProjectItemSet>())
                    .Where(q => q.based_id == quotationId)
                    .ToList();
                transactionList = JsonHelper.ToDataTable(filteredSalesQuotation);
                ItemSets = JsonHelper.ToDataTable(filteredItemSets);

                var itemsIds = filteredItemSets.Select(q => q.itemset_id).ToList();

                var filteredcontent = (data.sales_project_content ?? Enumerable.Empty<SalesProjectContent>())
                .Where(q => itemsIds.Contains(q.based_id))
                .ToList();
                ItemSetContent = JsonHelper.ToDataTable(filteredcontent);

                var filteredProjectItems = (data.sales_project_items ?? Enumerable.Empty<SalesProjectItems>())
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

                // Selected images for project items ride in the same table/service Quick
                // Quote's do (SalesProjectList.sales_project_items_selected_images) - the
                // column is still named quotation_quick_id on the wire, but for these rows it
                // actually holds the project item's items_id (see CreateProjectItems on the
                // API side). Match by that, the same way fetchQuotationDetailsByDocumentNo
                // matches Quick Quote's own images by quotation_quick_id.
                var projectItemIds = filteredProjectItems.Select(i => i.items_id).ToList();
                var filteredProjectSelectedImages = (data.sales_project_items_selected_images ?? Enumerable.Empty<SalesQuotationSelectedImageModel>())
                    .Where(q => projectItemIds.Contains(q.quotation_quick_id))
                    .ToList();
                selectedImageList = JsonHelper.ToDataTable(filteredProjectSelectedImages);
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

            var filteredSalesOrder = (data.order ?? Enumerable.Empty<OrderModel>())
                .Where(q => q.doc == documentNo)
                .ToList();

            var orderId = filteredSalesOrder.FirstOrDefault()?.order_id;

            if (orderId != null)
            {
                var filteredSalesOrderDetails = (data.sales_order_details ?? Enumerable.Empty<OrderDetailsModel>())
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
        // Every await below is a point where the user can close this dialog (or it can get
        // disposed some other way) before the awaited call returns. When that happens, the
        // code resuming after the await was still touching reportViewer1, which by then had
        // already been disposed - producing ObjectDisposedException. Bail out instead of
        // continuing to build/assign the report on a dialog that's no longer there.
        private bool IsSafeToUpdateReport => !this.IsDisposed && reportViewer1 != null && !reportViewer1.IsDisposed;
        private async void SalesPrintModal_Load(object sender, EventArgs e)
        {
            // Must finish before anything below touches ItemList/bpi_general/bpi_address -
            // these used to be fired-and-forgotten from the constructor, which raced with
            // this handler and could leave those tables empty (no columns at all), causing
            // "Cannot find column [id]" when .Select() ran against them.
            await fetchBpiData();
            await fetchItemData();

            if (!IsSafeToUpdateReport) return;

            if (isProject)
            {
                await fetchQuotationProjectByDocumentNo(documentNo);

                if (!IsSafeToUpdateReport) return;

                if (transactionList != null && transactionList.Rows.Count > 0)
                {
                    // transactionList was already filtered down to this exact document by
                    // fetchQuotationProjectByDocumentNo/fetchQuotationDetailsByDocumentNo
                    // using a prefix-normalized comparison. Re-filtering here with an exact
                    // "document_no = '{documentNo}'" string match failed for older records
                    // whose stored document_no still has "Q#"/"FQ#" baked in, producing
                    // "Document not found in this quotation for the report." even though the
                    // data had already loaded correctly - so just use the rows already here.
                    DataRow[] filteredRows = transactionList.Rows.Cast<DataRow>().ToArray();

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

                        foreach (DataRow itemSetRow in ItemSets.Select())
                        {

                            int itemSetId = (int)itemSetRow["itemset_id"];
                            var filterComponentItemRows = ProjectItemList.Select($"based_id = '{itemSetId}' ");

                            // Resets to 1 for every item set instead of counting continuously
                            // through the whole flat list (see item_no's own comment).
                            int itemNo = 0;

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
                                template_id = 0,
                                is_header_row = true,
                                percent_discount = 0,
                                item_no = 0,
                                Image = null
                            });


                            foreach (DataRow componentItemRow in filterComponentItemRows)
                            {
                                int itemsId = (int)componentItemRow["items_id"];
                                itemNo++;

                                // Same convention Quick Quote's DISCOUNT column uses
                                // (percent_discount, e.g. "15%") - Project items don't store a
                                // percent directly, only the raw multiplier string, so derive it
                                // the same way the live grid computes the actual charged price
                                // (ItemSetUC.CalculateDiscountMultiplier): ratio < 1 is a
                                // discount (positive %), ratio > 1 is a markup (negative %).
                                decimal multiplierRatio = ItemSetUC.CalculateDiscountMultiplier(componentItemRow["multiplier"]?.ToString());
                                // Round before it ever reaches the report - the raw division
                                // (e.g. a 1/7-derived multiplier) produces a repeating decimal
                                // with far more digits than fit in the DISCOUNT column, wrapping
                                // across multiple lines and blowing out the row's height.
                                decimal percentDiscount = Math.Round((1 - multiplierRatio) * 100, 2);

                                QuotationDetails.Add(new SalesProjectQuotationDetailsReportModel
                                {
                                    items_id = itemsId,
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
                                    template_id = (int)componentItemRow["template_id"],
                                    is_header_row = false,
                                    percent_discount = percentDiscount,
                                    item_no = itemNo,
                                    Image = GetFirstUploadedProjectItemImageBytes(itemsId)
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
                        // ProjectReport.rdlc had no Inclusion/Exclusion/TermsAndConditions
                        // parameters or report items at all - the section simply didn't exist,
                        // so Project Quotation prints never showed any of this even though the
                        // Project-specific Inclusions/Exclusions/Terms panels on the Quotation
                        // form (ProjectInclusionsRichTextBox etc.) were being filled in from the
                        // same quote-terms data Quick Quote uses. Passed in from the constructor
                        // the same way Quick Quote's branch below does.
                        ReportParameter inclusionParameter = new ReportParameter("Inclusion", inclusion);
                        ReportParameter exclusionParameter = new ReportParameter("Exclusion", exclusion);
                        ReportParameter termAndConditionsParameter = new ReportParameter("TermsAndConditions", termsAndCondition);
                        ReportDataSource headerReportDataSource = new ReportDataSource("DataSet1", transactionList);
                        ReportDataSource childReportDataSource = new ReportDataSource("DataSet2", ItemSetContent);
                        ReportDataSource ComponentsReportDataSource = new ReportDataSource("DataSet3", QuotationDetails);

                        // Same reasoning as Quick Quote's reportFileName switch below: every
                        // item row's DESCRIPTION cell reserves image-sized space whether that
                        // item actually has one or not, so a quotation with no images at all
                        // ended up with every row rendering at full (chunky) height for nothing.
                        // "ProjectReport without image.rdlc" is the same layout with a plain,
                        // compact-height description cell and no Image control; only switch to
                        // the taller image-capable layout when at least one item actually has one.
                        bool anyProjectItemHasImage = QuotationDetails.Any(d => d.Image != null && d.Image.Length > 0);
                        string projectReportFileName = anyProjectItemHasImage
                            ? "ProjectReport.rdlc"
                            : "ProjectReport without image.rdlc";

                        reportViewer1.LocalReport.ReportPath = Path.Combine(Settings.Default.REPORTPATH, projectReportFileName);
                        reportViewer1.LocalReport.DataSources.Clear();
                        reportViewer1.LocalReport.DataSources.Add(headerReportDataSource);
                        reportViewer1.LocalReport.DataSources.Add(childReportDataSource);
                        // ProjectReport.rdlc declares three datasets (DataSet1/2/3) - this one
                        // (DataSet3, the actual priced line items - QuotationDetails) was built
                        // above but never added here, so RefreshReport() always threw "A data
                        // source instance has not been supplied for the data source 'DataSet3'."
                        // and the report's line-item table would have been empty even if it hadn't.
                        reportViewer1.LocalReport.DataSources.Add(ComponentsReportDataSource);
                        reportViewer1.LocalReport.SubreportProcessing += new SubreportProcessingEventHandler(MapSubreportData);
                        reportViewer1.LocalReport.SetParameters(new ReportParameter[] { branchNameParameter, qtySumParameter, qtyParameter, addressNameParameter, unitpricesParameter, unitpricesSumParameter, itemDescriptionParameter, detailParameter, inclusionParameter, exclusionParameter, termAndConditionsParameter });
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
                bool foundQuotation = await fetchQuotationDetailsByDocumentNo(documentNo);

                if (!IsSafeToUpdateReport) return;

                // fetchQuotationDetailsByDocumentNo already showed the relevant message box
                // when it couldn't find/populate the data - don't show a second, redundant
                // "no data" message on top of that for the same underlying failure.
                if (!foundQuotation) return;

                if (transactionList != null && transactionList.Rows.Count > 0)
                {
                    // transactionList was already filtered down to this exact document by
                    // fetchQuotationProjectByDocumentNo/fetchQuotationDetailsByDocumentNo
                    // using a prefix-normalized comparison. Re-filtering here with an exact
                    // "document_no = '{documentNo}'" string match failed for older records
                    // whose stored document_no still has "Q#"/"FQ#" baked in, producing
                    // "Document not found in this quotation for the report." even though the
                    // data had already loaded correctly - so just use the rows already here.
                    DataRow[] filteredRows = transactionList.Rows.Cast<DataRow>().ToArray();

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

                        // Pick the report layout based on whether any line item actually has
                        // an image: "QuotationReport.rdlc" has the image column/layout,
                        // "QuotationReport without image.rdlc" is the plain layout used when
                        // there's nothing to show there (avoids empty image placeholders).
                        bool anyItemHasImage = childList.Columns.Contains("Image") &&
                            childList.AsEnumerable().Any(r =>
                                r["Image"] != DBNull.Value && r["Image"] is byte[] imgBytes && imgBytes.Length > 0);

                        string reportFileName = anyItemHasImage
                            ? "QuotationReport.rdlc"
                            : "QuotationReport without image.rdlc";

                        reportViewer1.LocalReport.ReportPath = Path.Combine(Settings.Default.REPORTPATH, reportFileName);
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

                if (!IsSafeToUpdateReport) return;

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
                        // Orders converted from a project quotation never saved their itemset
                        // "header" rows (item_id = 0 rows are skipped on save to avoid an
                        // item_id FK violation) - each surviving item row instead carries the
                        // header's tab name in item_set_header. Re-insert a header row before
                        // every group of items so the print shows them the same way the
                        // project quotation did, even though their qty is 0.
                        ReportDataSource childReportDataSource = new ReportDataSource("DataSet2", BuildDetailsWithHeaders(DetailsList));
                        
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

        // Re-inserts the dynamic itemset header rows (tab name, e.g. "A1") that were
        // dropped at save time, using the item_set_header label carried on each real item
        // row. Defensive about the column not existing at all (old API/DB before the
        // item_set_header migration ships, or a non-project order) - in that case this is a
        // no-op and the original table is returned unchanged.
        private DataTable BuildDetailsWithHeaders(DataTable details)
        {
            if (details == null || !details.Columns.Contains("item_set_header") || !details.Columns.Contains("item_code"))
                return details;

            bool hasAnyHeaderLabel = details.AsEnumerable()
                .Any(row => !string.IsNullOrWhiteSpace(row["item_set_header"]?.ToString()));
            if (!hasAnyHeaderLabel)
                return details;

            DataTable result = details.Clone();
            string lastHeader = null;

            foreach (DataRow row in details.Rows)
            {
                string header = row["item_set_header"]?.ToString();

                if (!string.IsNullOrWhiteSpace(header) && header != lastHeader)
                {
                    DataRow headerRow = result.NewRow();
                    foreach (DataColumn col in result.Columns)
                    {
                        headerRow[col.ColumnName] = DBNull.Value;
                    }
                    headerRow["item_code"] = header;
                    if (result.Columns.Contains("qty"))
                    {
                        headerRow["qty"] = 0;
                    }
                    result.Rows.Add(headerRow);

                    lastHeader = header;
                }

                result.LoadDataRow(row.ItemArray, true);
            }

            return result;
        }

        private byte[] LoadImageAsBytes(string imageName)
        {
            if (string.IsNullOrEmpty(imageName))
                return null;

            // Properties.Settings.Default.imagePath was a leftover hardcoded
            // "http://localhost:3000/api/vfile/" value that only worked on the original
            // developer's own machine - build the URL from the actual environment-resolved
            // server address instead (same fix as the item image pickers), and stop using
            // Path.Combine for a URL, which isn't what it's meant for.
            string imagePath = $"{smpc_sales_system.Program.ApiBaseUrl}/vfile/{imageName.Trim()}";

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

        // Same "prefer the row explicitly marked selected, else fall back to the first
        // uploaded" logic the Quick Quote branch above uses (see the childList.Rows loop
        // around line 592) - just matched against a project item's items_id instead of a
        // Quick Quote item's id, since that's the key selectedImageList rows carry for
        // Project Quotation (see fetchQuotationProjectByDocumentNo).
        private byte[] GetFirstUploadedProjectItemImageBytes(int itemsId)
        {
            var matchingImageRows = selectedImageList != null
                ? selectedImageList.AsEnumerable()
                    .Where(row => row.Field<int>("quotation_quick_id") == itemsId)
                    .ToList()
                : new List<DataRow>();

            DataRow matchedImageRow = matchingImageRows
                .FirstOrDefault(row => row.Field<bool>("is_selected"))
                ?? matchingImageRows.FirstOrDefault();

            if (matchedImageRow == null)
                return null;

            int imageId = matchedImageRow.Field<int>("image_id");

            string imageName = ImageList.AsEnumerable()
                .Where(row => row.Field<int>("id") == imageId)
                .Select(row => row.Field<string>("image"))
                .FirstOrDefault();

            return imageName != null ? LoadImageAsBytes(imageName) : null;
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
       
    

