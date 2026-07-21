using FastReport;
using FastReport.Dialog;
using FastReport.Preview;
using Newtonsoft.Json.Linq;
using smpc_sales_app.Models;
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
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using static smpc_sales_system.Models.SalesPrintModel;

namespace smpc_sales_system.Pages.Sales
{
    public partial class SalesPrint : Form
    {

        private Report report;
        private PreviewControl previewControl;

        private string documentNo;
        private bool isQuotation;
        private bool isProject;
        private string inclusion;
        private string exclusion;
        private string terms_and_conditions;
        string branchName = "Branch not found";
        List<string> unitprices = new List<string>();
        string addressName = "Address not found";

        public SalesPrint(bool isProject = false, string documentNo = null,
            string inclusion = null, string exclusion = null, string termsAndCondition = null)
        {
            InitializeComponent();

            fetchBpiData();
            fetchItemData();

            this.documentNo = documentNo;
            this.isProject = isProject;
            this.inclusion = inclusion;
            this.exclusion = exclusion;
            this.terms_and_conditions = termsAndCondition;

            PreviewControl previewControl = new PreviewControl();
            previewControl.Dock = DockStyle.Fill;
            this.Controls.Add(previewControl);
        }

        //public class SalesQuotationReportModel
        //{
        //    // Header Info
        //    public string DocumentNo { get; set; }
        //    public string ProjectName { get; set; }
        //    public DateTime QuotationDate { get; set; }
        //    public string ValidityDays { get; set; }
        //    public string ValidUntil { get; set; }

        //    // Customer Info
        //    public string CustomerName { get; set; }
        //    public string BranchName { get; set; }
        //    public string ShipToAddress { get; set; }
        //    public string BillToAddress { get; set; }
        //    public string Contact1 { get; set; }
        //    public string Contact2 { get; set; }

        //    // Totals
        //    public double GrossSales { get; set; }
        //    public double VatAmount { get; set; }
        //    public double NetSales { get; set; }
        //    public double PercentDiscount { get; set; }
        //    public double DiscountedAmount { get; set; }
        //    public double NetAmountDue { get; set; }
        //    public double TotalAmountDue { get; set; }

        //    // Items
        //    public List<QuotationItemModel> Items { get; set; } = new List<QuotationItemModel>();
        //}

        public class QuotationItemModel
        {
            public string ItemDescription { get; set; }
            public string ItemModel { get; set; }
            public double Quantity { get; set; }
            public double UnitPrice { get; set; }
            public double LineTotal { get; set; }
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

        private async void fetchItemData()
        {
            var itemData = await ItemService.GetItem();
            ItemList = JsonHelper.ToDataTable(itemData.items);
            ImageList = JsonHelper.ToDataTable(itemData.ItemImages);
        }
        private async void fetchBpiData()
        {
            Bpi_Class bpi_data = await QuotationService.GetBpiCustomers();
            bpi_general = JsonHelper.ToDataTable(bpi_data.general);
            bpi_address = JsonHelper.ToDataTable(bpi_data.address);
        }
        private async void SalesPrint_Load(object sender, EventArgs e)
        {
            try
            {
                if (isProject)
                {
                    await ShowProjectQuotationReport();
                }
                else
                {
                    await ShowQuotationReport();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading report: {ex.Message}");
            }
        }

        private async Task ShowProjectQuotationReport()
        {
            await fetchQuotationProjectByDocumentNo(documentNo);

            if (transactionList == null || transactionList.Rows.Count == 0)
            {
                MessageBox.Show("No quotation data available for the report.");
                return;
            }

            // Convert DataTable to Model
            var reportModel = ConvertToReportModel(transactionList.Rows[0]);

            // Load and display report
            DisplayReport(reportModel);
        }

        private async Task ShowQuotationReport()
        {
            await fetchQuotationDetailsByDocumentNo(documentNo);

            if (transactionList == null || transactionList.Rows.Count == 0)
            {
                MessageBox.Show("No quotation data available for the report.");
                return;
            }

            // transactionList was already filtered down to this exact document by
            // fetchQuotationDetailsByDocumentNo using a prefix-normalized comparison.
            // Re-filtering here with an exact "document_no = '{documentNo}'" string match
            // failed for older records whose stored document_no still has "Q#"/"FQ#" baked
            // in, so just use the rows already here instead.
            DataRow[] filteredRows = transactionList.Rows.Cast<DataRow>().ToArray();

            if (filteredRows.Length == 0)
            {
                MessageBox.Show("No quotation data available for the report.");
                return;
            }

            var reportModel = ConvertToReportModel(filteredRows[0]);

            // Add items to the model

            int quotationId = int.Parse(filteredRows[0]["id"].ToString());

            reportModel.QuotationQuicks = GetQuotationItems(quotationId);

            // Load and display report
            DisplayReport(reportModel);
        }

        private SalesQuotationReportModel ConvertToReportModel(DataRow row)
        {

            int customerId = Convert.ToInt32(row["customer_id"]);
            int shipToId = Convert.ToInt32(row["ship_to_id"]);


        return new SalesQuotationReportModel
        {
            id = Convert.ToInt32(row["id"]),
            document_no = row["document_no"].ToString(),
            project_name = row["project_name"].ToString(),
            date = Convert.ToDateTime(row["date"]),
            validity_days = row["validity_days"].ToString(),
            valid_until = row["valid_until"].ToString(),
            warranty = row["warranty"].ToString(),

            // Look up from other tables
            //customer_name = GetCustomerName(customerId),
            branch_name = GetBranchName(customerId),
            ship_to_address = GetShipToAddress(shipToId),
            bill_to_address = row["address_to"].ToString(),
            contact_1 = row["contact_1"].ToString(),
            contact_2 = row["contact_2"].ToString(),

            // Totals
            gross_sales = Convert.ToDouble(row["gross_sales"] ?? 0),
            vat_amount = Convert.ToDouble(row["vat_amount"] ?? 0),
            net_sales = Convert.ToDouble(row["net_sales"] ?? 0),
            percent_discount = Convert.ToDouble(row["percent_discount"] ?? 0),
            discounted_amount = Convert.ToDouble(row["discounted_amount"] ?? 0),
            additional_discounted_amount = Convert.ToDouble(row["additional_discounted_amount"] ?? 0),
            cash_discount = Convert.ToDouble(row["cash_discount"] ?? 0),
            net_amount_due = Convert.ToDouble(row["net_amount_due"] ?? 0),
            total_amount_due = Convert.ToDouble(row["total_amount_due"] ?? 0),

            // Content (populate with your actual data)
            //inclusion = GetInclusionText(),
            //exclusion = GetExclusionText(),
            //terms_and_conditions = GetTermsAndConditions()
        };

        }

        private List<QuotationQuickModel> GetQuotationItems(int quotationId)
        {
            var items = new List<QuotationQuickModel>();

            try
            {
                // Get items from childList (your existing data)
                DataRow[] itemRows = childList.Select($"based_id = '{quotationId}'");

                foreach (DataRow itemRow in itemRows)
                {
                    var quickModel = new QuotationQuickModel
                    {
                        id = Convert.ToInt32(itemRow["id"]),
                        quotation_id = quotationId,
                        based_id = Convert.ToInt32(itemRow["based_id"]),
                        description = itemRow["short_description"].ToString(),
                        price = Convert.ToDouble(itemRow["unit_price"] ?? 0),
                        quantity = Convert.ToInt32(itemRow["qty"] ?? 0),
                        line_total = Convert.ToDouble(itemRow["line_total"] ?? 0),
                        item_id = Convert.ToInt32(itemRow["item_id"]),
                        itemset_id = itemRow.Table.Columns.Contains("itemset_id") ? Convert.ToInt32(itemRow["itemset_id"]) : 0,
                        template_id = itemRow.Table.Columns.Contains("template_id") ? Convert.ToInt32(itemRow["template_id"]) : 0
                    };

                    // Get related images
                    quickModel.SelectedImages = GetImagesForItem(quickModel.id);
                    items.Add(quickModel);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error getting quotation items: {ex.Message}");
            }

            return items;
        }

        private List<QuotationSelectedImageModel> GetImagesForItem(int itemId)
        {
            var images = new List<QuotationSelectedImageModel>();

            try
            {
                // Search in your ItemSetContent table or similar
                if (ItemSetContent != null)
                {
                    DataRow[] imageRows = ItemSetContent.Select($"based_id = '{itemId}'");

                    foreach (DataRow imgRow in imageRows)
                    {
                        images.Add(new QuotationSelectedImageModel
                        {
                            id = Convert.ToInt32(imgRow["id"] ?? 0),
                            quotation_quick_id = itemId,
                            image_url = imgRow["image_url"].ToString(),
                            image_name = imgRow["image_name"].ToString()
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting images: {ex.Message}");
            }

            return images;
        }

        private void DisplayReport(SalesQuotationReportModel reportModel)
        {
            try
            {
                if (reportModel == null)
                {
                    MessageBox.Show("Report model is null");
                    return;
                }

                reportModel.inclusion = this.inclusion ?? "No inclusion details";
                reportModel.exclusion = this.exclusion ?? "No exclusion details"; 
                reportModel.termsandconditions = this.terms_and_conditions ?? "No terms and conditions";
                reportModel.project_name = "test project";

                report = new Report();
                report.Load(Path.Combine(Settings.Default.REPORTPATH, "QuotationReport.frx"));

                report.RegisterData(new List<SalesQuotationReportModel> { reportModel }, "Quotation");
                report.GetDataSource("Quotation").Enabled = true;

                // Modify the report template directly using Find and Replace in the XML
                string reportXml = File.ReadAllText(Path.Combine(Settings.Default.REPORTPATH, "QuotationReport.frx"));

                reportXml = reportXml.Replace("TEXT=\"\"", $"TEXT=\"{reportModel.branch_name}\"");

                // This approach is getting too complex...
                report.Preview = previewControl;

                if (report.Prepare())
                {
                    report.ShowPrepared();
                }
                else
                {
                    MessageBox.Show("Failed to prepare");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}\n{ex.StackTrace}");
            }
        }

        // Helper methods
        private string GetBranchName(int customerId)
        {
            DataRow[] rows = bpi_general.Select($"general_based_id = '{customerId}'");
            return rows.Length > 0 ? rows[0]["branch_name"].ToString() : "Branch not found";
        }

        private string GetShipToAddress(int shipToId)
        {
            DataRow[] rows = bpi_address.Select($"address_ids = '{shipToId}'");
            return rows.Length > 0 ? rows[0]["location"].ToString() : "Address not found";
        }

        // Older records can still have "Q#"/"FQ#" baked into their stored document_no (a
        // now-fixed save bug used to persist it that way), while documentNo passed into the
        // lookups below is always the bare number. Stripping both sides the same way before
        // comparing means lookups work for old and new records alike, without needing a
        // database migration to clean up the existing prefixed values.
        private static string NormalizeDocumentNo(string docNo) =>
            string.IsNullOrEmpty(docNo) ? docNo : Regex.Replace(docNo, @"FQ#|Q#", "").Trim();

        private async Task fetchQuotationProjectByDocumentNo(string documentNo)
        {
            try
            {
                // Call your service to get the data
                SalesProjectList data = await ProjectService.GetProjects();

                if (data == null || string.IsNullOrEmpty(documentNo))
                {
                    return;
                }

                List<SalesQuotationModel> filteredSalesQuotation = (data.SalesQuotation ?? Enumerable.Empty<SalesQuotationModel>())
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
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error fetching quotation: {ex.Message}");
            }
        }

        private async Task fetchQuotationDetailsByDocumentNo(string documentNo)
        {
            try
            {
                // Get quotation details
                SalesQuotationList data = await QuotationService.GetQuotations();
                var itemData = await ItemService.GetItem();

                ImageList = JsonHelper.ToDataTable(itemData.ItemImages);

                if (data == null || string.IsNullOrEmpty(documentNo))
                {
                    return;
                }

                // Get the main quotation
                List<SalesQuotationModel> filteredSalesQuotation = (data.SalesQuotation ?? Enumerable.Empty<SalesQuotationModel>())
                    .Where(q => NormalizeDocumentNo(q.document_no) == documentNo)
                    .ToList();

                if (filteredSalesQuotation.Count == 0)
                {
                    MessageBox.Show("No quotation found for the provided document number.");
                    return;
                }

                var QuotationId = filteredSalesQuotation.FirstOrDefault()?.id;

                // Get the quick items - IMPORTANT: Use based_id, not id
                List<SalesQuotationQuicksModel> filteredSalesQuotationQuick = (data.SalesQuotationQuick ?? Enumerable.Empty<SalesQuotationQuicksModel>())
                    .Where(q => q.based_id == QuotationId)  // ✅ CHANGED: from q.id to q.based_id
                    .ToList();

                var tempSelectedImages = new List<SalesQuotationSelectedImageModel>();

                // Get images for each item and attach them
                foreach (var item in filteredSalesQuotationQuick)
                {
                    var itemImages = (data.SalesQuotationSelectedImages ?? Enumerable.Empty<SalesQuotationSelectedImageModel>())
                        .Where(q => q.quotation_quick_id == item.id)
                        .ToList();

                    tempSelectedImages = itemImages;
                }

                // Convert to DataTables - THIS IS IMPORTANT
                transactionList = JsonHelper.ToDataTable(filteredSalesQuotation);
                childList = JsonHelper.ToDataTable(filteredSalesQuotationQuick);

                // Optional: Also convert images
                List<SalesQuotationSelectedImageModel> allImages = tempSelectedImages.ToList();

                selectedImageList = JsonHelper.ToDataTable(allImages);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error fetching quotation details: {ex.Message}");
            }
        }
    }
}
