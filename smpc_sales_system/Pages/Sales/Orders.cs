using smpc_app.Services.Helpers;
using smpc_app.Services.Helpers;
using smpc_sales_app.Data;
using smpc_sales_app.Services.Helpers;
using smpc_sales_app.Services.Sales;
using smpc_sales_system.Models;
using smpc_sales_system.Pages;
using smpc_sales_system.Pages.Sales;
using smpc_sales_system.Properties;
using smpc_sales_system.Services.Sales;
using smpc_sales_system.Services.Sales.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using WebSocketSharp;

namespace smpc_sales_app.Pages.Sales
{
    public partial class Orders : UserControl
    {
        // Same pattern as RedBox.cs/Opportunities.cs: lets this control ask its host
        // (Layout.cs's showForm) to open another document in a new tab. Needed here so
        // the §5.25 REMARKS reference link (see UpdateRemarksReferenceLink) can open the
        // original SO it points to, whether this Orders instance was itself opened from
        // the sidebar or from another document's own link.
        public delegate void TriggerNewFormDelegate(string title, Control control);
        public event TriggerNewFormDelegate TriggerNewForm;

        int SelectedRow = 0;
        private string documentNo;
        // True only while this screen is being used to convert a just-finalized
        // quotation into a brand-new order (nothing to view yet, so it starts
        // editable). False when browsing/viewing an order that already exists -
        // that starts locked, until the user clicks Edit.
        private bool isCreatingNewOrder = false;
        private bool isEditingExisting = false;
        private ImageList imageList = new ImageList(), imageList2 = new ImageList();
        string SalesPath = Settings.Default.SALESPATH;
        string AfterSalesPath = Settings.Default.AFTERSALESPATH;
        public Orders(string documentNo = null)
        {
            InitializeComponent();
            Helpers.ResetControls(pnl_header);
            Helpers.ResetControls(pnl_footer);
            this.documentNo = documentNo;

            imageList.ImageSize = new Size(64, 64);
            imageList2.ImageSize = new Size(16, 16);
            Image defaultIcon = smpc_sales_system.Properties.Resources.FileIcon;
            imageList.Images.Add("default", new Bitmap(defaultIcon, new Size(64, 64)));
            Image folderIcon = smpc_sales_system.Properties.Resources.FolderIcon;
            imageList2.Images.Add("folder", new Bitmap(folderIcon, new Size(64, 64)));

            AFTERSALES_LV.LargeImageList = imageList;
            AFTERSALES_LV.View = View.LargeIcon;
            SALES_LV.LargeImageList = imageList;
            SALES_LV.View = View.LargeIcon;

            AFTERSALES_TV.ImageList = imageList2;
            SALES_TV.ImageList = imageList2;

            // Live feedback while typing (e.g. composing a §5.25 chargeable-repair SO
            // and typing the original SO# into REMARKS) - CheckStatus() also calls this
            // after every load/edit-toggle, so it's covered either way, but this means
            // the link doesn't wait on one of those to appear.
            txt_remarks.TextChanged += (s, e) => UpdateRemarksReferenceLink();
        }
        private DataTable bpi_dt = new DataTable();
        private DataTable bpi_general = new DataTable();
        private DataTable bpi_address = new DataTable();
        private DataTable bpi_contacts = new DataTable();
        public DataTable OrderList { get; set; } = new DataTable();
        public DataTable DetailsList { get; set; } = new DataTable();
        public DataTable transactionList { get; set; } = new DataTable();
        public DataTable transactionProjectList { get; set; } = new DataTable();
        public DataTable childList { get; set; } = new DataTable();
        public DataTable ItemList { get; set; } = new DataTable();
        public DataTable ItemSpecs { get; set; } = new DataTable();
        public DataTable ItemSets { get; set; } = new DataTable();
        public DataTable ProjectItemList { get; set; } = new DataTable();
        public DataTable bom { get; set; } = new DataTable();
        public DataTable bomdetail { get; set; } = new DataTable();

        //FETCH METHODS
        private async Task FetchSalesOrder(bool isReload)
        {
            OrderList data = await OrderService.GetOrders();
            if (data == null || data.order == null || !data.order.Any())
            {
                MessageBox.Show("There's no sales order now.");
                return;
            }
            OrderList = JsonHelper.ToDataTable(data.order);
            DetailsList = data.sales_order_details != null && data.sales_order_details.Any()
                          ? JsonHelper.ToDataTable(data.sales_order_details)
                          : new DataTable();
            if (!isReload)
            {
                bindOrder(true);
                CalculateTotalPrice();
            }
        }
        private async Task FetchItemData()
        {
            var itemData = await ItemService.GetItem();
            ItemList = JsonHelper.ToDataTable(itemData.items);
            ItemSpecs = JsonHelper.ToDataTable(itemData.additionalspecs);
        }
        private async Task FetchBpiData()
        {
            Bpi_Class bpi_data = await QuotationService.GetBpiCustomers();
            bpi_dt = JsonHelper.ToDataTable(bpi_data.bpi);
            bpi_general = JsonHelper.ToDataTable(bpi_data.general);
            bpi_address = JsonHelper.ToDataTable(bpi_data.address);
            bpi_contacts = JsonHelper.ToDataTable(bpi_data.contacts);
        }
        private async Task   FetchQuotationDetails()
        {
            SalesQuotationList data = await QuotationService.GetQuotations();

            // data / data.SalesQuotation can legitimately come back null (e.g. no
            // finalized quotations exist yet) - .AsEnumerable() silently passes a null
            // straight through, and the chained .Where() then throws
            // "ArgumentNullException: Value cannot be null. Parameter name: source"
            // instead of just meaning "nothing to show yet".
            var salesQuotations = data?.SalesQuotation ?? Enumerable.Empty<SalesQuotationModel>();

            transactionList = JsonHelper.ToDataTable(salesQuotations
            .Where(q => q.document_no != null &&
                q.document_no.Contains("FQ"))
            .ToList());
            childList = JsonHelper.ToDataTable(data?.SalesQuotationQuick);
        }
        private async Task FetchProject()
        {
            SalesProjectList data = await ProjectService.GetProjects();
            transactionProjectList = JsonHelper.ToDataTable(data.SalesQuotation);
            ItemSets = JsonHelper.ToDataTable(data.sales_project_item_set);
            ProjectItemList = JsonHelper.ToDataTable(data.sales_project_items);
        }
        //BIND METHODS
        private void bindOrder(bool isBind = false)
        {
            if (isBind)
            {
                Panel[] pnlList = { pnl_header, pnl_header_2, pnl_footer, pnl_footer_2 };
                DataTable HeaderList = this.OrderList.Clone();
                HeaderList.Columns.Add("branch_name", typeof(string));
                HeaderList.Columns.Add("customer_code", typeof(string));
                HeaderList.Columns.Add("bill_to", typeof(string));
                HeaderList.Columns.Add("ship_to", typeof(string));
                HeaderList.Columns.Add("tin", typeof(string));

                DataRow parentRow = this.OrderList.Rows[SelectedRow];
                DataRow newRow = HeaderList.NewRow();

                foreach (DataColumn col in this.OrderList.Columns)
                {
                    newRow[col.ColumnName] = parentRow[col.ColumnName];
                }

                int quotationID = Convert.ToInt32(parentRow["quotation_id"]);
                DataRow[] quotation = transactionList.Select($"id = '{quotationID}'");
                if (quotation.Length > 0)
                {
                    HandleProjectNameVisibility(quotation[0]);
                }

                 string customerID = parentRow["customer_id"].ToString();
                string ShipID = parentRow["ship_to_id"].ToString();
                string BillID = parentRow["bill_to_id"].ToString();
                PopulateCustomerAndAddressInfo(customerID, ShipID, BillID, newRow);
                HeaderList.Rows.Add(newRow);
                Helpers.BindControls(pnlList, HeaderList, 0);
                UpdateTextBoxes(pnlList);
                txt_status.Text = SetDefaultIfEmpty(txt_status.Text);

                dtp_date.Value = Convert.ToDateTime(OrderList.Rows[SelectedRow]["date"]);
                dtp_delivery_date.Value = Convert.ToDateTime(OrderList.Rows[SelectedRow]["delivery_date"]);
                cmb_payment_terms.SelectedValue = this.OrderList.Rows[this.SelectedRow]["payment_terms_id"].ToString();
                cmb_payment_terms.SelectedItem = this.OrderList.Rows[this.SelectedRow]["payment_terms_id"].ToString();
                cmb_ship_type.SelectedValue = this.OrderList.Rows[this.SelectedRow]["ship_type_id"].ToString();
                cmb_ship_type.SelectedItem = this.OrderList.Rows[this.SelectedRow]["ship_type_id"].ToString();

                string orderId = this.OrderList.Rows[this.SelectedRow]["order_id"].ToString();



                DataView filteredDetailsView = new DataView(this.DetailsList);
                filteredDetailsView.RowFilter = $"based_id = '{orderId}'";
                dgv_order_sales.DataSource = filteredDetailsView;

                // Same reason as bindOrderByDocNo below - always show the grid with the REAL
                // saved order details, even for a project-sourced order. dgv_project is only
                // ever populated as a creation-time preview (bindProject), never with this
                // order's actual saved rows.
                dgv_order_sales.Visible = true;
                dgv_project.Visible = false;

                CheckStatus();
            }
        }
        private void bindOrderByDocNo(string documentNo, bool isBind = false)
        {
            if (isBind)
            {
                Panel[] pnlList = { pnl_header, pnl_header_2, pnl_footer, pnl_footer_2 };

                DataTable HeaderList = this.OrderList.Clone();
                HeaderList.Columns.Add("branch_name", typeof(string));
                HeaderList.Columns.Add("customer_code", typeof(string));
                HeaderList.Columns.Add("bill_to", typeof(string));
                HeaderList.Columns.Add("ship_to", typeof(string));
                HeaderList.Columns.Add("tin", typeof(string));

                DataRow[] filteredRows = this.OrderList.Select($"document_no = '{documentNo}'");

                if (filteredRows.Length > 0)
                {
                    DataRow parentRow = filteredRows[0];

                    DataRow newRow = HeaderList.NewRow();
                    foreach (DataColumn col in this.OrderList.Columns)
                    {
                        newRow[col.ColumnName] = parentRow[col.ColumnName];
                    }
                    int quotationID = Convert.ToInt32(parentRow["quotation_id"]);
                    DataRow[] quotation = transactionList.Select($"id = '{quotationID}'");
                    if (quotation.Length > 0)
                    {
                        HandleProjectNameVisibility(quotation[0]);
                    }
                    string customerID = parentRow["customer_id"].ToString();
                    newRow["vat_amount"] = parentRow["vat_amount"].ToString();
                    newRow["gross_sales"] = parentRow["gross_sales"].ToString();
                    newRow["total_amount_due"] = parentRow["total_amount_due"].ToString();

                    string ShipID = parentRow["ship_to_id"].ToString();
                    string BillID = parentRow["bill_to_id"].ToString();
                    PopulateCustomerAndAddressInfo(customerID, ShipID, BillID, newRow);
                    HeaderList.Rows.Add(newRow);
                    Helpers.BindControls(pnlList, HeaderList, SelectedRow);

                    cmb_payment_terms.SelectedValue = filteredRows[0]["payment_terms_id"].ToString();
                    cmb_payment_terms.SelectedItem = filteredRows[0]["payment_terms_id"].ToString();
                    cmb_ship_type.SelectedValue = filteredRows[0]["ship_type_id"].ToString();
                    cmb_ship_type.SelectedItem = filteredRows[0]["ship_type_id"].ToString();
                    UpdateTextBoxes(pnlList);
                    txt_status.Text = SetDefaultIfEmpty(txt_status.Text);

                    dtp_date.Value = Convert.ToDateTime(parentRow["date"]);
                    dtp_delivery_date.Value = Convert.ToDateTime(parentRow["delivery_date"]);
                    DataView ordertable = new DataView(this.OrderList);
                    ordertable.RowFilter = $"document_no = '{documentNo}'";
                    string orderId = ordertable[0]["order_id"].ToString();

                    DataView filteredDetailsView = new DataView(this.DetailsList);
                    filteredDetailsView.RowFilter = $"based_id = '{orderId}'";
                    dgv_order_sales.DataSource = filteredDetailsView;

                    // bindOrderByDocNo loads the REAL saved sales_order_details rows into
                    // dgv_order_sales - always show that grid here, even for a project-sourced
                    // order. dgv_project is only ever populated by bindProject as a preview
                    // while creating a NEW order from a quotation (via bindQuotation); this
                    // function never touches it, so if IsProject(true) had been left on from an
                    // earlier action in the same session, dgv_project would still be visible and
                    // showing that stale preview (blank description/delivery/list price/status,
                    // since those were never wired up there) instead of what was actually saved.
                    dgv_order_sales.Visible = true;
                    dgv_project.Visible = false;

                    CheckStatus();
                }
            }
        }
        private void bindQuotation(string documentNo, bool isBind = false)
        {
            if (isBind)
            {
                Panel[] pnlList = { pnl_header, pnl_header_2, pnl_footer, pnl_footer_2 };
                DataTable HeaderList = this.transactionList.Clone();
                HeaderList.Columns.Add("branch_name", typeof(string));
                HeaderList.Columns.Add("customer_code", typeof(string));
                HeaderList.Columns.Add("bill_to", typeof(string));
                HeaderList.Columns.Add("ship_to", typeof(string));
                HeaderList.Columns.Add("tin", typeof(string));

                DataRow[] filteredRows = this.transactionList.Select($"document_no = '{documentNo}'");

                if (filteredRows.Length == 0)
                {
                    filteredRows = this.transactionProjectList.Select($"document_no = '{documentNo}'");
                }

                if (filteredRows.Length > 0)
                {
                    foreach (DataRow parentRow in filteredRows)
                    {
                        DataRow newRow = HeaderList.NewRow();
                        foreach (DataColumn col in this.transactionList.Columns)
                        {
                            newRow[col.ColumnName] = parentRow[col.ColumnName];
                        }

                        string customerID = parentRow["customer_id"].ToString();
                        string ShipID = parentRow["ship_to_id"].ToString();
                        string BillID = parentRow["bill_to_id"].ToString();
                        PopulateCustomerAndAddressInfo(customerID, ShipID, BillID, newRow);
                        HeaderList.Rows.Add(newRow);
                    }
                    DataRow firstRow = filteredRows[0];
                    if (!string.IsNullOrEmpty(firstRow["project_name"]?.ToString()))
                    {
                        IsProject(true);
                        bindProject(documentNo, true);
                        return;
                    }
                    dgv_order_sales.Columns["unitprice"].DataPropertyName = "unit_price";
                    dgv_order_sales.Columns["linetotal"].DataPropertyName = "line_total";
                    IsProject(false);

                    cmb_payment_terms.SelectedValue = filteredRows[0]["payment_terms_id"].ToString();
                    cmb_payment_terms.SelectedItem = filteredRows[0]["payment_terms_id"].ToString();
                    cmb_ship_type.SelectedValue = filteredRows[0]["ship_type_id"].ToString();
                    cmb_ship_type.SelectedItem = filteredRows[0]["ship_type_id"].ToString();
                    Helpers.BindControls(pnlList, HeaderList, SelectedRow);

                    UpdateTextBoxes(pnlList);

                    int basedId = Convert.ToInt32(filteredRows[0]["id"]);
                    DataRow[] filteredChildRows = childList.Select($"based_id = {basedId}");

                    DataTable withItemList = this.childList.Clone(); // Clone the structure of childList
                    withItemList.Columns.Add("item_description", typeof(string));
                    withItemList.Columns.Add("item_code", typeof(string));
                    withItemList.Columns.Add("numbering", typeof(string));
                    withItemList.Columns.Add("has_stocks", typeof(bool));

                    int itemcounter = 1;

                    // Loop through the filtered rows
                    foreach (DataRow childRow in filteredChildRows)
                    {
                        DataRow newRow = withItemList.NewRow();

                        // Copy data from childRow to newRow
                        foreach (DataColumn col in childList.Columns)
                        {
                            newRow[col.ColumnName] = childRow[col.ColumnName];
                        }

                        string itemId = childRow["item_id"].ToString();
                        DataRow[] itemRows = ItemList.Select($"id = '{itemId}'");
                        DataRow[] itemspecRows = ItemSpecs.Select($"based_id = '{itemId}'");


                        //string allocationQty = string.IsNullOrEmpty(childRow["allocation_qty"].ToString()) ? "0" : childRow["allocation_qty"].ToString();
                        string qty = string.IsNullOrEmpty(childRow["qty"].ToString()) ? "0" : childRow["qty"].ToString();


                        newRow["has_stocks"] = int.Parse(qty) > 0 ? false : true;

                        // Add item details to newRow

                        if (itemspecRows.Length > 0)
                        {
                            newRow["item_description"] = itemspecRows[0]["long_description"].ToString();
                        }
                        else
                        {
                            newRow["item_description"] = "Unknown Item";
                        }

                        if (itemRows.Length > 0)
                        {
                            newRow["item_code"] = itemRows[0]["item_code"].ToString();
                            newRow["numbering"] = itemcounter;
                        }
                        else
                        {
                            newRow["item_code"] = "N/A";
                            newRow["numbering"] = itemcounter;
                        }
                        itemcounter += 1;
                        withItemList.Rows.Add(newRow);
                    }

                    // Apply DataView for final filtering based on based_id if needed (this part seems redundant but kept for consistency)
                    DataView dataview = new DataView(withItemList);
                    dataview.RowFilter = $"based_id = '{basedId}'"; // Re-filtering after creating new DataTable (if necessary)
                    dgv_order_sales.DataSource = dataview;
                }
            }
        }
        private void bindProject(string documentNo, bool isBind = false)
        {
            if (isBind)
            {
                Panel[] pnlList = { pnl_header, pnl_header_2, pnl_footer, pnl_footer_2 };
                DataTable HeaderList = this.transactionProjectList.Clone();
                HeaderList.Columns.Add("branch_name", typeof(string));
                HeaderList.Columns.Add("customer_code", typeof(string));
                HeaderList.Columns.Add("bill_to", typeof(string));
                HeaderList.Columns.Add("ship_to", typeof(string));
                HeaderList.Columns.Add("tin", typeof(string));

                DataRow[] filteredRows = this.transactionProjectList.Select($"document_no = '{documentNo}'");

                if (filteredRows.Length > 0)
                {
                    foreach (DataRow parentRow in filteredRows)
                    {
                        DataRow newRow = HeaderList.NewRow();
                        foreach (DataColumn col in this.transactionProjectList.Columns)
                        {
                            newRow[col.ColumnName] = parentRow[col.ColumnName];
                        }
                        string customerID = parentRow["customer_id"].ToString();
                        string ShipID = parentRow["ship_to_id"].ToString();
                        string BillID = parentRow["bill_to_id"].ToString();
                        PopulateCustomerAndAddressInfo(customerID, ShipID, BillID, newRow);
                        HeaderList.Rows.Add(newRow);
                    }
                    cmb_payment_terms.SelectedValue = filteredRows[0]["payment_terms_id"].ToString();
                    cmb_payment_terms.SelectedItem = filteredRows[0]["payment_terms_id"].ToString();
                    cmb_ship_type.SelectedValue = filteredRows[0]["ship_type_id"].ToString();
                    cmb_ship_type.SelectedItem = filteredRows[0]["ship_type_id"].ToString();
                }

                Helpers.BindControls(pnlList, HeaderList, 0);
                UpdateTextBoxes(pnlList);

                // Filter ItemSets based on the "based_id" of the first filtered row
                int basedId = Convert.ToInt32(filteredRows[0]["id"]);

                DataView itemSetView = new DataView(ItemSets);
                itemSetView.RowFilter = "based_id = '" + basedId + "'";
                var ids = itemSetView.Cast<DataRowView>().Select(rowView => Convert.ToInt32(rowView["itemset_id"])).ToList();

                double bomCounter = 0;
                int bomDetailIndex = 1;

                if (ids.Count > 0)
                {
                    // Filter ProjectItemList based on the item set IDs
                    string idFilter = string.Join("','", ids);
                    DataView projectItemView = new DataView(ProjectItemList);
                    projectItemView.RowFilter = $"based_id IN ('{idFilter}')";
                    DataTable transformedTable = projectItemView.ToTable();

                    // Create a new DataTable to store processed rows
                    DataTable withItemListTwo = transformedTable.Clone();
                    withItemListTwo.Columns.Add("short_desc", typeof(string));
                    withItemListTwo.Columns.Add("item_code", typeof(string));
                    withItemListTwo.Columns.Add("number", typeof(string));
                    withItemListTwo.Columns.Add("level", typeof(int));
                    withItemListTwo.Columns.Add("itemset_header", typeof(int));
                    // Carries the itemset's tab name (e.g. "A1") onto every real item row
                    // under it, not just the synthetic header row - the header row itself
                    // never survives to the saved order (item_id = 0 rows get skipped
                    // below to avoid an item_id FK violation), so this is how the print
                    // reconstruction later on knows which dynamic header to re-insert for
                    // each surviving item.
                    withItemListTwo.Columns.Add("item_set_header_name", typeof(string));

                    var uniqueItemsets = ids.Distinct().ToList();

                    foreach (DataRowView itemSetRow in itemSetView)
                    {
                        int itemsetId = (int)itemSetRow["itemset_id"];

                        // ADD ITEMSET HEADER
                        DataRow headerRow = withItemListTwo.NewRow();
                        foreach (DataColumn col in transformedTable.Columns)
                        {
                            headerRow[col.ColumnName] = DBNull.Value;
                        }
                        headerRow["level"] = 0;
                        headerRow["itemset_header"] = itemsetId;
                        headerRow["item_code"] = itemSetRow["tab_number"];
                        headerRow["item_set_header_name"] = itemSetRow["tab_number"];
                        headerRow["number"] = "";

                        // update the transformedTable
                        headerRow["qty"] = 0;
                        headerRow["list_price_per_unit"] = 0m;
                        headerRow["component_total"] = 0m;
                        headerRow["based_id"] = basedId;
                        headerRow["item_id"] = 0;

                        withItemListTwo.Rows.Add(headerRow);

                        bomCounter = 0;
                        bomDetailIndex = 1;

                        // Process rows for this itemset
                        foreach (DataRow row in transformedTable.Rows)
                        {
                            int rowItemsetId = Convert.ToInt32(row["based_id"]);
                            if (rowItemsetId != itemsetId)
                                continue;  // Skip rows from other itemsets

                            DataRow newRow = withItemListTwo.NewRow();

                            foreach (DataColumn col in transformedTable.Columns)
                            {
                                newRow[col.ColumnName] = row[col.ColumnName];
                            }
                            newRow["item_set_header_name"] = itemSetRow["tab_number"];
                            int itemId = Convert.ToInt32(row["item_id"]);
                            int bomId = Convert.ToInt32(row["bom_id"]);
                            string model = row["model"].ToString();
                            // CHECKER IF THE ROW IS HEAD OF BOM THAT HAS EXISTING ITEMS
                            if (itemId == 0 && bomId == 0 && !string.IsNullOrEmpty(model))
                            {
                                bomCounter += 1;
                                newRow["level"] = 1;
                                newRow["number"] = bomCounter;
                                newRow["item_code"] = row["components"].ToString();
                                newRow["itemset_header"] = itemsetId;
                                withItemListTwo.Rows.Add(newRow);

                                if (bomDetailIndex > 1)
                                {
                                    bomDetailIndex = 1;
                                }
                            }
                            // CHECKER IF THE ROW IS AN ITEM / ACCESSORIES
                            if (itemId > 0 && bomId == 0)
                            {
                                bomCounter += 1;
                                DataRow[] itemRows = ItemList.Select($"id = {itemId}");
                                if (itemRows.Length > 0)
                                {
                                    string itemCode = itemRows[0]["item_code"].ToString();
                                    string shortDesc = itemRows[0]["short_desc"].ToString();

                                    newRow["short_desc"] = shortDesc;
                                    newRow["item_code"] = itemCode;
                                }
                                else
                                {
                                    newRow["short_desc"] = "Unknown";
                                    newRow["item_code"] = "Unknown";
                                }
                                newRow["level"] = 1;
                                newRow["number"] = bomCounter;
                                newRow["itemset_header"] = itemsetId;
                                withItemListTwo.Rows.Add(newRow);
                            }
                            // CHECKER IF THE ROW IS AN ITEM OF A BOM
                            else if (bomId > 0 && itemId > 0)
                            {
                                DataRow[] itemRows = ItemList.Select($"id = {itemId}");
                                if (itemRows.Length > 0)
                                {
                                    string itemCode = itemRows[0]["item_code"].ToString();
                                    newRow["number"] = $"{bomCounter}.{bomDetailIndex}";
                                    newRow["level"] = 1;
                                    newRow["item_code"] = itemCode;
                                }
                                else
                                {
                                    newRow["number"] = $"{bomCounter}.{bomDetailIndex}";
                                    newRow["level"] = 1;
                                    newRow["item_code"] = "Unknown";
                                }
                                newRow["itemset_header"] = itemsetId;
                                withItemListTwo.Rows.Add(newRow);
                                bomDetailIndex += 1;
                            }
                        }
                    }

                    dgv_project.DataSource = withItemListTwo;

                    // Format the DataGridView with colors/styles
                    FormatHierarchicalGrid();
                }
            }
        }

        private void DebugGridViewBindings()
        {
            try
            {
                string debugInfo = "DataGridView Column Bindings:\n\n";

                foreach (DataGridViewColumn col in dgv_project.Columns)
                {
                    debugInfo += $"Column Name: {col.Name}\n";
                    debugInfo += $"  Header Text: {col.HeaderText}\n";
                    debugInfo += $"  DataPropertyName: {col.DataPropertyName}\n";
                    debugInfo += $"  Visible: {col.Visible}\n";
                    debugInfo += "\n";
                }

                MessageBox.Show(debugInfo);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message);
            }
        }

        private void FormatHierarchicalGrid()
        {
            try
            {
                string columnsList = "Columns in grid:\n";
                foreach (DataGridViewColumn col in dgv_project.Columns)
                {
                    columnsList += "- " + col.Name + "\n";
                }

                string rowData = "First 3 rows data:\n\n";
                for (int i = 0; i < Math.Min(3, dgv_project.Rows.Count); i++)
                {
                    DataGridViewRow row = dgv_project.Rows[i];

                    string val = row.Cells["number"].Value?.ToString();
                    string numberValue = string.IsNullOrWhiteSpace(val) ? "0" : val;

                    rowData += $"Row {i}:\n";
                    rowData += $"number = '{numberValue}'\n";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error in FormatHierarchicalGrid:\n" + ex.Message + "\n\n" + ex.StackTrace);
            }
        }

        // ALTERNATIVE: Add click event to make headers collapsible (optional)
        private void dgv_project_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                int level = (int)dgv_project.Rows[e.RowIndex].Cells["level"].Value;

                // Only toggle on header rows
                if (level == 0)
                {
                    int itemsetId = (int)dgv_project.Rows[e.RowIndex].Cells["itemset_id"].Value;
                    bool isCollapsed = dgv_project.Rows[e.RowIndex].Cells["item_code"].Value.ToString().StartsWith("▶");

                    if (isCollapsed)
                    {
                        // Expand - show detail rows
                        for (int i = e.RowIndex + 1; i < dgv_project.Rows.Count; i++)
                        {
                            int nextLevel = (int)dgv_project.Rows[i].Cells["level"].Value;
                            int nextItemset = (int)dgv_project.Rows[i].Cells["itemset_id"].Value;

                            if (nextLevel == 0)  // Next header found
                                break;

                            if (nextItemset == itemsetId)
                                dgv_project.Rows[i].Visible = true;
                        }
                    }
                    else
                    {
                        // Collapse - hide detail rows
                        for (int i = e.RowIndex + 1; i < dgv_project.Rows.Count; i++)
                        {
                            int nextLevel = (int)dgv_project.Rows[i].Cells["level"].Value;
                            int nextItemset = (int)dgv_project.Rows[i].Cells["itemset_id"].Value;

                            if (nextLevel == 0)  // Next header found
                                break;

                            if (nextItemset == itemsetId)
                                dgv_project.Rows[i].Visible = false;
                        }
                    }
                }
            }
        }


        //ON LOAD OF ORDER
        private async void Orders_Load(object sender, EventArgs e)
        {
            // A (re)load always starts back in view mode; the branches below set
            // isCreatingNewOrder = true again if this load is actually for
            // converting a quotation into a brand-new order.
            isEditingExisting = false;
            isCreatingNewOrder = false;
            bs_payment_terms.DataSource = CacheData.PaymentTerms;
            bs_ship_type.DataSource = CacheData.ShipTypeSetup;
            toBenchedToolStripMenuItem.Click += toBenchedToolStripMenuItem_Click;
            toActiveToolStripMenuItem.Click += toActiveToolStripMenuItem_Click;
            renameFileToolStripMenuItem.Click += renameFileToolStripMenuItem_Click;
            AddFoldertoolStripMenuItem.Click += addFolderToolStripMenuItem_Click;
            renameFolderToolStripMenuItem.Click += renameFolderToolStripMenuItem_Click;
            this.Width = 1380;
            await FetchQuotationDetails();
            await FetchProject();
            await FetchBpiData();
            await FetchItemData();
            await FetchSalesOrder(false);
            

            if (!string.IsNullOrEmpty(documentNo))
            {
                if (OrderList != null && OrderList.Rows.Count > 0)
                {
                    DataRow[] matchingRows = OrderList.Select($"document_no = '{documentNo}'");

                    if (matchingRows.Length > 0)
                    {
                        // documentNo matches an order that already exists - viewing it,
                        // not creating one.
                        isCreatingNewOrder = false;
                        BindControlsForNewOrderORexisting();
                        bindOrderByDocNo(documentNo, true);
                    }
                    else
                    {
                        // documentNo didn't match an existing order, so it's a quotation
                        // being converted into a brand-new one.
                        isCreatingNewOrder = true;
                        btn_refresh.Visible = false;
                        BindControlsForNewOrderORexisting();
                        SetCreatedByToCurrentUser();
                        bindQuotation(documentNo, true);
                        SOIncrementer();
                        TV1_preview.Visible = true;
                        TV2_preview.Visible = true;
                    }
                    CalculateTotalPrice();
                }
                else if (documentNo == "0")
                {
                    isCreatingNewOrder = true;
                    BindControlsForNewOrderORexisting();
                    SetCreatedByToCurrentUser();
                    await FetchSalesOrder(false);
                    CalculateTotalPrice();
                    SOIncrementer();
                }
                else
                {
                    isCreatingNewOrder = true;
                    btn_refresh.Visible = false;
                    BindControlsForNewOrderORexisting();
                    SetCreatedByToCurrentUser();
                    bindQuotation(documentNo, true);
                    SOIncrementer();
                    TV1_preview.Visible = true;
                    TV2_preview.Visible = true;
                }
            }

            // Outside the documentNo branch on purpose. CheckStatus() used to run only when
            // the screen was opened FOR a specific document (converting a quotation, or
            // following a link) - opening Sales Order straight from the sidebar passes no
            // documentNo, so it never ran and the buttons kept whatever the designer set.
            // That was survivable while they defaulted to visible-but-disabled; now that
            // the approve/cancel pair defaults to hidden so the access gate fails closed,
            // never running this left them hidden even for a user who does hold the code.
            CheckStatus();

            LoadDirectory(AFTERSALES_TV, AfterSalesPath);
            LoadDirectory(SALES_TV, SalesPath);
        }
        //ACTIONS METHOD (BUTTONS, CLICKS)
        private void btn_search_Click(object sender, EventArgs e)
        {
            string Title = "Order List";
            SearchOrder setup = new SearchOrder(Title, OrderList);
            DialogResult r = setup.ShowDialog();
            if (r == DialogResult.OK)
            {
                int result = setup.GetResult();

                if (result != -1)
                {
                    SelectedRow = result;
                    isEditingExisting = false;
                    bindOrder(true);
                    CalculateTotalPrice();
                }
            }
        }
        private void Save_Click(object sender, EventArgs e)
        {
            SaveSalesOrder();
        }
        private async void btn_check_Click(object sender, EventArgs e)
        {
            try
            {
                string docIdValue = ((TextBox)pnl_header_2.Controls["txt_doc"]).Text;
                string docnoValue = ((TextBox)pnl_header_2.Controls["txt_document_no"]).Text;
                docIdValue = DocumentNo.Strip(docIdValue);
                docnoValue = DocumentNo.Strip(docnoValue);

                if (int.TryParse(docIdValue, out int selectedDoc) && selectedDoc > 0)
                {
                    var selectedOrder = OrderList.Select($"doc = {selectedDoc}").FirstOrDefault();
                if (selectedOrder != null)
                {
                    int orderId = Convert.ToInt32(selectedOrder["order_id"]);

                    // Re-fetch before building the payload instead of reading dgv_order_sales /
                    // dgv_project directly: those grids are only populated depending on which
                    // bind path was last run (e.g. converting a project quotation into an order
                    // only ever populates dgv_project, never dgv_order_sales), so reading "the
                    // visible grid" silently sent an empty/stale detail list and item statuses
                    // never got updated even though the order itself was marked ACTIVE.
                    // DetailsList always reflects the real persisted sales_order_details rows.
                    await FetchSalesOrder(true);

                    selectedOrder = OrderList.Select($"doc = {selectedDoc}").FirstOrDefault();
                    if (selectedOrder == null)
                    {
                        MessageBox.Show("No order found with the selected ID.");
                        return;
                    }

                    // SO APPROVED BY (spec 5.4: "Approval strip shows `SO APPROVED BY:`
                    // with the approver's name"). Approving only ever sent doc/order_id/
                    // status, so approved_by and approved_by_id stayed empty on every order
                    // ever activated - and txt_approved_by, which Helpers.BindControls
                    // already maps to the approved_by column, therefore had nothing to show.
                    // Stamped here rather than at save time because the approver is whoever
                    // clicks this, which is not necessarily whoever created the order (the
                    // strip is a Sales Manager / CBDO action).
                    string approverName = CacheData.CurrentUser != null
                        ? $"{CacheData.CurrentUser.first_name} {CacheData.CurrentUser.last_name}".Trim()
                        : string.Empty;

                    var parentDataHeader = new Dictionary<string, dynamic>
                        {
                            { "doc", selectedOrder["doc"] },
                            {"order_id", selectedOrder["order_id"] },
                            { "status", "ACTIVE" },
                            { "approved_by", approverName },
                            { "approved_by_id", CacheData.CurrentUser != null ? CacheData.CurrentUser.id : 0 }
                        };

                        DataRow[] detailRows = DetailsList != null && DetailsList.Columns.Contains("based_id")
                            ? DetailsList.Select($"based_id = {orderId}")
                            : new DataRow[0];

                        List<Dictionary<string, dynamic>> orderDetailsList = new List<Dictionary<string, dynamic>>();

                        foreach (DataRow item in detailRows)
                        {
                            Dictionary<string, object> data = new Dictionary<string, object>();

                            // Deliberately NOT sending "status" here (confirmed with the user:
                            // Activate is a one-time action, before any Job Order/Item Release
                            // progress could exist - so this used to be harmless, but computing
                            // it from qty/allocated_qty here duplicated - and could stomp - the
                            // real §7.1 status the server's own recompute engine now owns for
                            // this column. Omitting the key entirely leaves the column untouched
                            // server-side (DbUpdate's UpdateColumns skips zero-valued fields on a
                            // partial struct, same as every other field this payload already
                            // leaves out) rather than writing a coarse CANVASS/IN STOCK value
                            // over whatever's already there.
                            data.Add("order_details_id", int.TryParse(item["order_details_id"]?.ToString(), out var orderDetailsId) ? orderDetailsId : 0);
                            data.Add("based_id", orderId);

                            orderDetailsList.Add(data);
                        }

                        if (orderDetailsList.Count == 0)
                        {
                            MessageBox.Show("No order details found to update.");
                        }
                        else
                        {
                            List<Dictionary<string, dynamic>> childCollection = new List<Dictionary<string, dynamic>>();
                            foreach (var childData in orderDetailsList)
                            {
                                childCollection.Add(childData);
                            }
                            parentDataHeader["sales_order_details"] = childCollection;

                            if (parentDataHeader.ContainsKey("sales_order_details"))
                            {
                                parentDataHeader["sales_order_details"] = childCollection;

                               var response = await OrderService.Update(parentDataHeader);

                                if(response.Success)
                                {
                                    // Show the approver straight away. FetchSalesOrder below
                                    // re-binds from the server and would fill this in anyway,
                                    // but it is fire-and-forget here (not awaited), so without
                                    // this the strip stays blank until the next real refresh.
                                    txt_approved_by.Text = approverName;

                                    MessageBox.Show("Order status updated to ACTIVE.");
                                    FetchSalesOrder(false);
                                    CheckStatus();
                                }
                                else
                                {
                                    MessageBox.Show("Failed to update order status. Please try again. this is possible cause is " + response.message);
                                }
                            }
                        }
                }
                else
                {
                    MessageBox.Show("No order found with the selected ID.");
                }
                }
                else
                {
                    MessageBox.Show("Please select a valid order to update.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}\n\nStack Trace: {ex.StackTrace}");
            }
        }
        private async void btn_cancel_Click(object sender, EventArgs e)
        {
            try
            {
                string docIdValue = ((TextBox)pnl_header_2.Controls["txt_doc"]).Text;
                string docnoValue = ((TextBox)pnl_header_2.Controls["txt_document_no"]).Text;
                docIdValue = DocumentNo.Strip(docIdValue);
                docnoValue = DocumentNo.Strip(docnoValue);

                if (int.TryParse(docIdValue, out int selectedDoc) && selectedDoc > 0)
                {
                    var selectedOrder = OrderList.Select($"doc = {selectedDoc}").FirstOrDefault();
                    if (selectedOrder != null)
                    {
                        selectedOrder["status"] = "CANCELLED";
                        var parentDataHeader = new Dictionary<string, dynamic>
                            {
                                { "doc", selectedOrder["doc"] },
                                { "status", "CANCELLED" }
                            };
                        await OrderService.Update(parentDataHeader);
                        MessageBox.Show("Order status updated to CANCELLED.");
                        FetchSalesOrder(false);
                        CheckStatus();
                    }
                    else
                    {
                        MessageBox.Show("No order found with the selected ID.");
                    }
                }
                else
                {
                    MessageBox.Show("Please select a valid order to update.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}\n\nStack Trace: {ex.StackTrace}");
            }
        }
        // DELETE - permanently removes the order (unlike Cancel, which just sets
        // status to CANCELLED and keeps the record). Only allowed while the order
        // hasn't gone ACTIVE yet, so this is for cleaning up mistakes/duplicates
        // (e.g. two orders accidentally created from the same quotation) rather
        // than voiding an order that already has real activity against it.
        private async void btn_delete_Click(object sender, EventArgs e)
        {
            try
            {
                string docIdValue = ((TextBox)pnl_header_2.Controls["txt_doc"]).Text;
                docIdValue = DocumentNo.Strip(docIdValue);

                if (!int.TryParse(docIdValue, out int selectedDoc) || selectedDoc <= 0)
                {
                    MessageBox.Show("Please select a valid order to delete.");
                    return;
                }

                var selectedOrder = OrderList.Select($"doc = {selectedDoc}").FirstOrDefault();
                if (selectedOrder == null)
                {
                    MessageBox.Show("No order found with the selected ID.");
                    return;
                }

                if (selectedOrder["status"]?.ToString() == "ACTIVE")
                {
                    MessageBox.Show(
                        "This order is ACTIVE and can't be deleted. Use Cancel instead if it needs to be voided.",
                        "Delete Not Allowed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                string docNoDisplay = selectedOrder["document_no"]?.ToString() ?? selectedDoc.ToString();
                DialogResult confirm = MessageBox.Show(
                    $"Are you sure you want to permanently delete order {docNoDisplay}? This cannot be undone.",
                    "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

                if (confirm != DialogResult.Yes)
                {
                    return;
                }

                var data = new Dictionary<string, dynamic>
                {
                    { "order_id", selectedOrder["order_id"] }
                };

                bool isSuccess = await OrderService.Delete(data);
                if (isSuccess)
                {
                    MessageBox.Show("Order deleted successfully.");
                    Helpers.ResetControls(pnl_header);
                    Helpers.ResetControls(pnl_footer);
                    await FetchSalesOrder(false);
                    ViewEnable();
                }
                else
                {
                    MessageBox.Show("Failed to delete the order.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}\n\nStack Trace: {ex.StackTrace}");
            }
        }
        private async void btn_next_Click(object sender, EventArgs e)
        {
            int rowCount = OrderList.Rows.Count;
            if (SelectedRow < rowCount - 1)
            {
                SelectedRow++;
                isEditingExisting = false;
                Helpers.ResetControls(pnl_header);
                Helpers.ResetControls(pnl_footer);
                await FetchSalesOrder(false);
                LoadDirectory(AFTERSALES_TV, AfterSalesPath);
                LoadDirectory(SALES_TV, SalesPath);
                sales_preview.Visible = true;
                aftersales_preview.Visible = true;
            }
        }
        private async void btn_prev_Click_1(object sender, EventArgs e)
        {
            if (SelectedRow >= 1)
            {
                SelectedRow--;
                isEditingExisting = false;
                Helpers.ResetControls(pnl_header);
                Helpers.ResetControls(pnl_footer);
                await FetchSalesOrder(false);
                LoadDirectory(AFTERSALES_TV, AfterSalesPath);
                LoadDirectory(SALES_TV, SalesPath);
                sales_preview.Visible = true;
                aftersales_preview.Visible = true;
            }
        }
        private void btn_back_Click(object sender, EventArgs e)
        {
            Quotation quotationPage = new Quotation(documentNo);
            this.Parent.Controls.Add(quotationPage);
            this.Dispose();
        }
        private void btn_save_Click_1(object sender, EventArgs e)
        {
            SaveSalesOrder();
        }
        private void btn_print_Click(object sender, EventArgs e)
        {
            string documentNo = DocumentNo.Strip(txt_doc.Text);
            SalesPrintModal printPage = new SalesPrintModal(false, false, documentNo);
            int screenHeight = Screen.PrimaryScreen.Bounds.Height;
            printPage.Height = (int)(screenHeight);
            printPage.StartPosition = FormStartPosition.CenterParent;
            printPage.ShowDialog();
        }
        //METHODS FOR LOADING THE DIRECTORIES PATHS =================================================
        private void LoadDirectory(TreeView treeView, string directoryPath)
        {
            if (!Directory.Exists(directoryPath))
            {
                // Optionally, create the directory if it doesn't exist
                Directory.CreateDirectory(directoryPath);
            }
            CreateSubDirectories(directoryPath);
            treeView.Nodes.Clear();
            treeView.ImageKey = "folder";
            treeView.SelectedImageKey = "folder";
            TreeNode rootNode = new TreeNode(directoryPath);
            treeView.Nodes.Add(rootNode);
            LoadDirectories(directoryPath, rootNode, treeView);
            rootNode.Expand();
        }
        private void CreateSubDirectories(string directoryPath)
        {
            // Create ACTIVE and BENCHED
            string activeDir = Path.Combine(directoryPath, "ACTIVE");
            if (!Directory.Exists(activeDir))
            {
                Directory.CreateDirectory(activeDir);
            }

            string benchedDir = Path.Combine(directoryPath, "BENCHED");
            if (!Directory.Exists(benchedDir))
            {
                Directory.CreateDirectory(benchedDir);
            }

            // Determine folder type based on parent path
            string[] innerFolders;

            if (directoryPath.ToUpper().Contains("AFTERSALES") || directoryPath.ToUpper().Contains("AFTERSALES"))
            {
                innerFolders = new[]
                {
                    "Warranty", "Distributorship", "Test Certificates", "Manufacture Conformities", "Certificate of Origin",
                    "Technical Data Sheet", "Brochure", "Operating Instruction Manuals", "Wiring Diagrams", "Sequence of Operations",
                    "Testing & Commissioning Methodology", "Site Visit Reports", "Serial Numbers of Equipment",
                    "General Arrangement", "CAD Files", "Bill of Quantities & Bill of Materials"
                };
            }
            else if (directoryPath.ToUpper().Contains("SALES"))
            {
                innerFolders = new[]
                {
                    "Quotation Versions", "Technical Evaluation Report", "Clarificatories",
                    "Bid Bulletin", "Client Purchase Order"
                };
            }
            else
            {
                // Unknown type – optionally skip or log
                return;
            }

            // Create folders in ACTIVE and BENCHED
            foreach (string subDir in new[] { activeDir, benchedDir })
            {
                foreach (string folderName in innerFolders)
                {
                    string folderPath = Path.Combine(subDir, folderName);
                    if (!Directory.Exists(folderPath))
                    {
                        Directory.CreateDirectory(folderPath);
                    }
                }
            }
        }
        private void LoadDirectories(string path, TreeNode node, TreeView treeView)
        {
            try
            {
                string[] directories = Directory.GetDirectories(path);
                foreach (string directory in directories)
                {
                    string folderName = Path.GetFileName(directory);

                    if (folderName.ToUpper().Contains("- SO#"))
                    {
                        // Extract the SO# part from folder
                        string soTagFromFolder = folderName.Split('-').LastOrDefault()?.Trim();
                        if (!string.Equals(soTagFromFolder, txt_doc.Text, StringComparison.OrdinalIgnoreCase))
                        {
                            continue; // Skip this folder since it doesn't match txt_doc.Text
                        }
                    }

                    TreeNode directoryNode = new TreeNode(folderName);
                    directoryNode.ImageKey = "folder";
                    directoryNode.SelectedImageKey = "folder";
                    node.Nodes.Add(directoryNode);

                    // Recursively process subfolders
                    LoadDirectories(directory, directoryNode, treeView);
                    directoryNode.Expand();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading directories: {ex.Message}");
            }
        }
        private void LoadFiles(ListView listView, string path)
        {
            listView.Items.Clear();
            try
            {
                string[] files = Directory.GetFiles(path);
                bool isQuotationVersions = Path.GetFileName(path).Equals("Quotation Versions", StringComparison.OrdinalIgnoreCase);
                bool isBOQ = Path.GetFileName(path).Equals("Bill of Quantities & Bill of Materials", StringComparison.OrdinalIgnoreCase);

                // Toggle the visibility of the Rename option
                renameFileToolStripMenuItem.Visible = !isQuotationVersions;

                foreach (string file in files)
                {
                    string fileName = Path.GetFileName(file);
                    string documentTag = (isQuotationVersions || isBOQ)
                    ? txt_document_no.Text
                    : txt_doc.Text;

                    bool isMatch = isQuotationVersions || isBOQ
                        ? fileName.Contains(documentTag)
                        : fileName.Contains("- " + documentTag);

                    if (isMatch)
                    {
                        ListViewItem item = new ListViewItem(fileName);
                        item.SubItems.Add(new FileInfo(file).Length.ToString());
                        item.SubItems.Add(File.GetLastWriteTime(file).ToString());
                        item.ImageKey = "default";
                        listView.Items.Add(item);
                    }
                }
            }
            catch (UnauthorizedAccessException)
            {
                MessageBox.Show("Access denied to the folder.");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading files: {ex.Message}");
            }
        }
        //END=========================================================================================

        //FUNCTIONS OF FOLDERS/DIRECTORIES============================================================
        private void renameFolderToolStripMenuItem_Click(object sender, EventArgs e)
        {
            TreeNode selectedNode = SALES_TV.SelectedNode ?? AFTERSALES_TV.SelectedNode;
            if (selectedNode == null)
            {
                MessageBox.Show("Please select a folder to rename.", "No Folder Selected", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            string oldFolderName = selectedNode.Text;

            if (!oldFolderName.ToUpper().Contains("- SO#"))
            {
                MessageBox.Show("Only folders with ' - SO#' in the name can be renamed.", "Rename Not Allowed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string tag = txt_doc.Text;
            string userInput = Microsoft.VisualBasic.Interaction.InputBox("Enter new folder name:", "Rename Folder", "");

            if (string.IsNullOrWhiteSpace(userInput)) return;

            string newFolderName = $"{userInput} - {tag}";

            string relativePath = GetRelativePathFromNode(selectedNode);

            // Determine base paths for ACTIVE and BENCHED
            string activeBase = SalesPath;
            string activeRelPath = relativePath.Replace("BENCHED", "ACTIVE");
            string benchedRelPath = relativePath.Replace("ACTIVE", "BENCHED");

            RenameFolder(activeBase, activeRelPath, oldFolderName, newFolderName);
            RenameFolder(activeBase, benchedRelPath, oldFolderName, newFolderName);
            LoadDirectory(SALES_TV, SalesPath);
            LoadDirectory(AFTERSALES_TV, AfterSalesPath);
        }
        private void RenameFolder(string basePath, string relativePath, string oldName, string newName)
        {
            try
            {
                string fullPath = Path.Combine(basePath, relativePath);
                string currentFolder = Path.Combine(Path.GetDirectoryName(fullPath), oldName);
                string renamedFolder = Path.Combine(Path.GetDirectoryName(fullPath), newName);

                if (Directory.Exists(currentFolder))
                {
                    Directory.Move(currentFolder, renamedFolder);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error renaming folder in '{basePath}': {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private string GetRelativePathFromNode(TreeNode node)
        {
            if (node == null) return string.Empty;

            var parts = new Stack<string>();
            TreeNode current = node;

            while (current != null)
            {
                parts.Push(current.Text);
                current = current.Parent;
            }

            return Path.Combine(parts.ToArray());
        }
        private void addFolderToolStripMenuItem_Click(object sender, EventArgs e)
        {
            TreeView targetTreeView = null;
            string basePath = "";
            bool isSales = false;
            // Treeview checker
            if (SALES_TV.Focused)
            {
                targetTreeView = SALES_TV;
                basePath = SalesPath;
                isSales = true;
            }
            else if (AFTERSALES_TV.Focused)
            {
                targetTreeView = AFTERSALES_TV;
                basePath = AfterSalesPath;
                isSales = false;
            }

            if (targetTreeView == null)
            {
                MessageBox.Show("Please click on a TreeView to add the folder.", "No TreeView Selected", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string folderName = Microsoft.VisualBasic.Interaction.InputBox("Enter folder name:", "Add Folder", "New Folder");
            string tag = txt_doc.Text;
            folderName = folderName + " - " + tag;
            if (string.IsNullOrWhiteSpace(folderName)) return;

            string[] categories = { "ACTIVE", "BENCHED" };
            foreach (var category in categories)
            {
                string fullPath = Path.Combine(basePath, category, folderName);
                try
                {
                    if (!Directory.Exists(fullPath))
                        Directory.CreateDirectory(fullPath);
                    if (isSales)
                    {
                        LoadDirectory(SALES_TV, SalesPath);
                    }
                    else
                    {
                        LoadDirectory(AFTERSALES_TV, AfterSalesPath);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to create folder in '{category}':\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            MessageBox.Show($"Folder '{folderName}' created in both ACTIVE and BENCHED.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        //END=========================================================================================

        //METHODS FOR TREEVIEWS AND LISTVIEWS
        private void SALES_TV_AfterSelect(object sender, TreeViewEventArgs e)
        {
            string nodeText = e.Node.Text.ToUpper();
            if (e.Node.Parent == null || nodeText == "ACTIVE" || nodeText == "BENCHED")
            {
                sales_preview.Visible = true;
            }
            else
            {
                sales_preview.Visible = false;
            }

            string selectedPath = GetPathFromTreeNode(e.Node);
            lbl_path1.Text = selectedPath;
            LoadFiles(SALES_LV, selectedPath);
        }

        private void SALES_LV_DoubleClick(object sender, EventArgs e)
        {
            if (SALES_LV.SelectedItems.Count > 0)
            {
                string folderPath = GetPathFromTreeNode(SALES_TV.SelectedNode);
                string filePath = Path.Combine(folderPath, SALES_LV.SelectedItems[0].Text);
                if (File.Exists(filePath))
                {
                    try
                    {
                        System.Diagnostics.Process.Start(filePath);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error opening file: {ex.Message}");
                    }
                }
                else
                {
                    MessageBox.Show("The selected file does not exist.");
                }
            }
        }
        private string GetPathFromTreeNode(TreeNode node, bool isRelative = false)
        {
            if (node == null) return string.Empty;

            string path = node.Text;
            TreeNode current = node;

            while (current.Parent != null)
            {
                current = current.Parent;
                path = Path.Combine(current.Text, path);
            }

            if (isRelative)
                return path;

            // Assuming the root node text is the base path
            string basePath = current.Text;
            return Path.Combine(basePath, path.Substring(basePath.Length).TrimStart(Path.DirectorySeparatorChar));
        }

        private void renameFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ListView activeListView = null;
            string currentPath = "";

            if (SALES_LV.SelectedItems.Count > 0)
            {
                activeListView = SALES_LV;
                currentPath = lbl_path1.Text;
            }
            else if (AFTERSALES_LV.SelectedItems.Count > 0)
            {
                activeListView = AFTERSALES_LV;
                currentPath = lbl_path2.Text;
            }

            if (activeListView == null || activeListView.SelectedItems.Count == 0)
            {
                MessageBox.Show("Please select a file to rename.", "No File Selected", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string selectedFileName = activeListView.SelectedItems[0].Text;
            string fullCurrentFilePath = Path.Combine(currentPath, selectedFileName);
            string fileExtension = Path.GetExtension(selectedFileName);
            string tag = txt_doc.Text;

            string input = Microsoft.VisualBasic.Interaction.InputBox("Enter new File name:", "Rename File", "");

            if (string.IsNullOrWhiteSpace(input))
            {
                MessageBox.Show("No input provided. Rename cancelled.", "Cancelled", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string newFileName = $"{input} - {tag}{fileExtension}";
            string newFilePath = Path.Combine(currentPath, newFileName);

            try
            {
                File.Move(fullCurrentFilePath, newFilePath);
                activeListView.SelectedItems[0].Text = newFileName;

                MessageBox.Show("File renamed successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error renaming file: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private void toBenchedToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (SALES_LV.SelectedItems.Count > 0)
            {
                MoveSelectedFileToBenched(SALES_LV, lbl_path1.Text);
            }
            else if (AFTERSALES_LV.SelectedItems.Count > 0)
            {
                MoveSelectedFileToBenched(AFTERSALES_LV, lbl_path2.Text);
            }
            else
            {
                MessageBox.Show("Please select a file to move.", "No File Selected", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void toActiveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (SALES_LV.SelectedItems.Count > 0)
            {
                MoveSelectedFileToActive(SALES_LV, lbl_path1.Text);
            }
            else if (AFTERSALES_LV.SelectedItems.Count > 0)
            {
                MoveSelectedFileToActive(AFTERSALES_LV, lbl_path2.Text);
            }
            else
            {
                MessageBox.Show("Please select a file to move.", "No File Selected", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
        private void MoveSelectedFileToActive(ListView listView, string currentPath)
        {
            if (listView.SelectedItems.Count == 0) return;

            string selectedFileName = listView.SelectedItems[0].Text;
            string fullCurrentFilePath = Path.Combine(currentPath, selectedFileName);

            if (currentPath.Contains(@"\ACTIVE\"))
            {
                MessageBox.Show("The file is already in ACTIVE.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string activePath = currentPath.Replace(@"\BENCHED\", @"\ACTIVE\");

            try
            {
                // Ensure target directory exists
                if (!Directory.Exists(activePath))
                {
                    Directory.CreateDirectory(activePath);
                }

                string targetFilePath = Path.Combine(activePath, selectedFileName);

                File.Move(fullCurrentFilePath, targetFilePath);
                listView.Items.Remove(listView.SelectedItems[0]);

                MessageBox.Show($"File moved to ACTIVE:\n{targetFilePath}", "Success");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error moving file: {ex.Message}");
            }
        }

        private void MoveSelectedFileToBenched(ListView listView, string currentPath)
        {
            if (listView.SelectedItems.Count == 0) return;

            string selectedFileName = listView.SelectedItems[0].Text;
            string fullCurrentFilePath = Path.Combine(currentPath, selectedFileName);

            if (currentPath.Contains(@"\BENCHED\"))
            {
                MessageBox.Show("The file is already in BENCHED.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string benchedPath = currentPath.Replace(@"\ACTIVE\", @"\BENCHED\");

            try
            {
                // Ensure target directory exists
                if (!Directory.Exists(benchedPath))
                {
                    Directory.CreateDirectory(benchedPath);
                }

                string targetFilePath = Path.Combine(benchedPath, selectedFileName);

                File.Move(fullCurrentFilePath, targetFilePath);
                listView.Items.Remove(listView.SelectedItems[0]);

                MessageBox.Show($"File moved to BENCHED:\n{targetFilePath}", "Success");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error moving file: {ex.Message}");
            }
        }
        

        private void AFTERSALES_TV_AfterSelect(object sender, TreeViewEventArgs e)
        {
            string nodeText = e.Node.Text.ToUpper();
            if (e.Node.Parent == null || nodeText == "ACTIVE" || nodeText == "BENCHED")
            {
                aftersales_preview.Visible = true;
            }
            else
            {
                aftersales_preview.Visible = false;
            }
            
            string selectedPath = GetPathFromTreeNode(e.Node);    
            lbl_path2.Text = selectedPath;
            LoadFiles(AFTERSALES_LV, selectedPath);
        }
        private void SALES_LV_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effect = DragDropEffects.Copy;
            }
            else
            {
                e.Effect = DragDropEffects.None;
            }
        }
        private void SALES_LV_DragDrop(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            foreach (var file in files)
            {
                try
                {
                    string tag = txt_doc.Text;
                    string fileName = Path.GetFileNameWithoutExtension(file);
                    string extension = Path.GetExtension(file);       
                    string taggedFileName = $"{fileName} - {tag}{extension}";  

                    string latestPath = lbl_path1.Text;
                    string targetFilePath = Path.Combine(latestPath, taggedFileName);

                    File.Copy(file, targetFilePath, true); // true to overwrite if the file already exists

                    ListViewItem item = new ListViewItem(taggedFileName);
                    item.ImageKey = "default";
                    SALES_LV.Items.Add(item);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error moving file: {ex.Message}");
                }
            }
        }
        private void AFTERSALES_LV_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effect = DragDropEffects.Copy;
            }
            else
            {
                e.Effect = DragDropEffects.None;
            }
        }
        private void AFTERSALES_LV_DragDrop(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            foreach (var file in files)
            {
                try
                {
                    string tag = txt_doc.Text;
                    string fileName = Path.GetFileNameWithoutExtension(file); 
                    string extension = Path.GetExtension(file);               
                    string taggedFileName = $"{fileName} - {tag}{extension}"; 

                    string latestpath = lbl_path2.Text;
                    string targetFilePath = Path.Combine(latestpath, taggedFileName);

                    File.Copy(file, targetFilePath, true); // true to overwrite if the file already exists

                    ListViewItem item = new ListViewItem(taggedFileName);
                    item.ImageKey = "default";
                    AFTERSALES_LV.Items.Add(item);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error moving file: {ex.Message}");
                }
            }
        }
        private void AFTERSALES_LV_DoubleClick(object sender, EventArgs e)
        {
            if (AFTERSALES_LV.SelectedItems.Count > 0)
            {
                string folderPath = GetPathFromTreeNode(AFTERSALES_TV.SelectedNode);
                string filePath = Path.Combine(folderPath, AFTERSALES_LV.SelectedItems[0].Text);

                if (File.Exists(filePath))
                {
                    try
                    {
                        System.Diagnostics.Process.Start(filePath);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error opening file: {ex.Message}");
                    }
                }
                else
                {
                    MessageBox.Show("The selected file does not exist.");
                }
            }
        }
        //METHODS TO BE USED FOR THE WHOLE ORDER DETAILS
        public string SetDefaultIfEmpty(string value, string defaultValue = "-")
        {
            return string.IsNullOrEmpty(value) ? defaultValue : value;
        }
        private void HandleProjectNameVisibility(DataRow quotation)
        {
            bool isProjectNameValid = quotation["project_name"] != DBNull.Value && !string.IsNullOrEmpty(quotation["project_name"].ToString());
            label26.Visible = isProjectNameValid;
            txt_project_name.Visible = isProjectNameValid;
        }
        private void PopulateCustomerAndAddressInfo(string customerID, string shipID, string billID, DataRow newRow)
        {
            DataRow[] bpiGenRows = bpi_general.Select($"general_based_id = '{customerID}'");
            DataRow[] billRows = bpi_address.Select($"address_ids = '{billID}'");
            DataRow[] shipRows = bpi_address.Select($"address_ids = '{shipID}'");

            if (bpiGenRows.Length > 0)
            {
                newRow["branch_name"] = bpiGenRows[0]["branch_name"].ToString();
                newRow["customer_code"] = bpiGenRows[0]["customer_code"].ToString();

                string BasedID = bpiGenRows[0]["general_based_id"].ToString();
                DataRow[] bpiRows = bpi_dt.Select($"id = '{BasedID}'");
                newRow["tin"] = bpiRows.Length > 0 ? bpiRows[0]["tin"].ToString() : "No TIN";

                // Set address information
                newRow["bill_to"] = billRows.Length > 0 ? billRows[0]["location"].ToString() : "No Location";
                newRow["ship_to"] = shipRows.Length > 0 ? shipRows[0]["location"].ToString() : "No Location";
            }
            else
            {
                newRow["branch_name"] = "Unknown Customer";
                newRow["customer_code"] = "N/A";
            }
        }
        public void UpdateTextBoxes(Panel[] pnlArray)
        {
            foreach (var pnl in pnlArray)
            {
                foreach (Control control in pnl.Controls)
                {
                    if (!(control is TextBox textBox)) continue;

                    // Exact names, not Contains: "txt_doc" is a substring of "txt_document_no",
                    // so the old code fired several Contains blocks on the same control that
                    // only happened to net out. DocumentNo.Apply strips any existing prefix
                    // before adding one, so re-decoration is idempotent and can never produce
                    // the doubled "FQ#Q#..." / "SO#FQ#...". Only these two controls exist.
                    if (textBox.Name == "txt_document_no")
                    {
                        // The finalized-quote reference this SO was created from.
                        textBox.Text = DocumentNo.Apply(textBox.Text, "FQ#");
                    }
                    else if (textBox.Name == "txt_doc")
                    {
                        // The SO's own number.
                        textBox.Text = DocumentNo.Apply(textBox.Text, "SO#");
                    }
                }
            }
        }
        // Delegates to the shared DocumentNo helper, which also collapses doubled prefixes
        // (e.g. "FQ#Q#0007" -> "0007") rather than stripping only one. Kept as a thin wrapper
        // so existing call sites are untouched.
        private static string StripDocPrefix(string value) => DocumentNo.Strip(value);

        private Dictionary<string, dynamic> MergeDictionaries(params Dictionary<string, dynamic>[] dictionaries)
        {
            var mergedDict = new Dictionary<string, dynamic>();

            foreach (var dict in dictionaries)
            {
                foreach (var kvp in dict)
                {
                    mergedDict[kvp.Key] = kvp.Value;
                }
            }
            return mergedDict;
        }
        private void IsProject(bool isProject)
        {
            if (isProject)
            {
                dgv_order_sales.Visible = false;
                dgv_project.Visible = true;
                label26.Visible = true;
                txt_project_name.Visible = true;
            }
            else
            {
                dgv_order_sales.Visible = true;
                dgv_project.Visible = false;
                label26.Visible = false;
                txt_project_name.Visible = false;
            }
        }
        private void CalculateTotalPrice()
        {
            if (!dgv_order_sales.Columns.Contains("linetotal"))
            {
                MessageBox.Show("The 'line_total' column is missing in the DataGridView.");
                return;
            }
            decimal total = dgv_order_sales.Rows.Cast<DataGridViewRow>()
                                .Where(row => row.Cells["linetotal"].Value != null && decimal.TryParse(row.Cells["linetotal"].Value.ToString(), out _))
                                .Sum(row => Convert.ToDecimal(row.Cells["linetotal"].Value));
            txt_total.Text = total.ToString("#,0.00");
        }
        private void SOIncrementer()
        {
            txt_doc.Text = DocumentNo.Apply((OrderList.Rows.Count + 1).ToString("D4"), "SO#");
        }
        // The tbl_position_access codes for the two restricted Sales Order actions. Must
        // match the constants of the same name in the API's order_service.go and the rows
        // in access_modules_seed.json - the seed is what puts them in the Access Control
        // tree so they can be granted to Sales Manager / CBDO.
        private const string OrderApproveAccessCode = "Sales - Order.Orders.Approve";
        private const string OrderCancelAccessCode = "Sales - Order.Orders.Cancel Order";

        // Whether the logged-in user's Position has been granted an access code.
        //
        // CurrentUser.position.access is populated from the login response; the API only
        // started preloading Position.Access for this, so on an older API build the list
        // comes back empty. That fails CLOSED - an ungranted user simply doesn't see the
        // buttons, and the server would refuse the action anyway - rather than open.
        private bool HasAccessCode(string code)
        {
            var access = CacheData.CurrentUser?.position?.access;
            if (access == null) return false;

            return access.Any(a => string.Equals(a?.code, code, StringComparison.OrdinalIgnoreCase));
        }

        private void CheckStatus()
        {
            bool isStatusActive = txt_status.Text == "ACTIVE";
            bool isStatusCancelled = txt_status.Text == "CANCELLED";
            bool hasStatus = !string.IsNullOrEmpty(txt_status.Text);

            // Was `!isStatusActive`, which permanently locked Check out once an order went
            // ACTIVE. Kept enabled while ACTIVE so it can be re-run to resync item-level
            // status (see btn_check_Click) if a prior approve ran before item details existed.
            // Spec 3.3: "Approve / cancel a Sales Order - Sales Manager or CBDO only
            // (check + cancel buttons HIDDEN from everyone else)". Hidden, not merely
            // disabled - a greyed button still tells an ordinary sales user this is an
            // action that exists for them. Driven off the granted access code rather than
            // a position-name match, so who can approve stays a Position Access setup
            // decision (same reasoning as the server-side gate in order_service.go).
            //
            // This is presentation only. The real gate is server-side: the update endpoint
            // rejects an ACTIVE/CANCELLED status from a user without the code, so hiding
            // the buttons cannot be worked around by calling the API directly.
            bool canApprove = HasAccessCode(OrderApproveAccessCode);
            bool canCancelOrder = HasAccessCode(OrderCancelAccessCode);

            btn_check.Visible = canApprove;
            btn_cancel.Visible = canCancelOrder;

            btn_check.Enabled = canApprove && hasStatus && !isStatusCancelled;
            btn_cancel.Enabled = canCancelOrder && hasStatus && !isStatusCancelled;
            // Delete is only for orders that haven't gone ACTIVE yet (cleaning up
            // mistakes/duplicates) - once ACTIVE, use Cancel instead. Not applicable
            // while still creating a brand-new order (nothing saved yet to delete).
            // Hidden (not just disabled) when it doesn't apply, same as Edit/Save.
            bool canDelete = !isCreatingNewOrder && hasStatus && !isStatusActive;
            btn_delete.Visible = canDelete;
            btn_delete.Enabled = canDelete;
            btn_refresh.Enabled = isStatusActive || !isStatusCancelled;

            // Edit mode: a brand-new order (just converted from a finalized quotation)
            // starts editable immediately since there's nothing to view yet. An
            // existing order starts locked and needs Edit clicked - and can't be
            // edited at all once CANCELLED.
            bool canEdit = isCreatingNewOrder || isEditingExisting;

            btn_edit.Visible = !isCreatingNewOrder;
            btn_edit.Enabled = !isCreatingNewOrder && !isEditingExisting && hasStatus && !isStatusCancelled;

            // Back-to-view: only meaningful while actively editing an *existing*
            // order (discards unsaved edits and re-locks the form). Not shown while
            // creating a brand-new order, since there's no prior view to return to.
            btn_cancel_edit.Visible = isEditingExisting;

            Save.Visible = canEdit;
            btn_save.Visible = canEdit;
            btn_save.Enabled = canEdit;

            Panel[] editablePanels = { pnl_header, pnl_header_2, pnl_footer };
            if (canEdit && !isStatusCancelled)
            {
                Helpers.ResetReadOnlyControls(editablePanels);
            }
            else
            {
                Helpers.ReadOnlyControls(editablePanels);
            }

            txt_ref_po.ReadOnly = isStatusActive || isStatusCancelled || !canEdit;
            dtp_date.Enabled = !isStatusCancelled && canEdit;
            dtp_delivery_date.Enabled = !isStatusCancelled && canEdit;
            txt_receiver.ReadOnly = isStatusCancelled || !canEdit;
            txt_contact_no.ReadOnly = isStatusCancelled || !canEdit;
            txt_remarks.ReadOnly = isStatusCancelled || !canEdit;
            // Always read-only. This line used to be inert (the control was Enabled=false),
            // and now that it is enabled so the approver's name is legible, letting edit mode
            // flip it writable would allow someone to type over who actually approved the
            // order. btn_check_Click is the only thing that sets it.
            txt_approved_by.ReadOnly = true;

            // Same rule for SALES EXECUTIVE: it is the user who saved the order, stamped by
            // SetCreatedByToCurrentUser, not something the user types. It sits on pnl_header_2,
            // which ResetReadOnlyControls above unlocks wholesale in edit mode - so without
            // this it became typeable on Edit. Pin it read-only in every state.
            txt_sales_executive.ReadOnly = true;

            foreach (DataGridViewColumn column in dgv_order_sales.Columns)
            {
                column.ReadOnly = isStatusCancelled || !canEdit;
            }

            // Runs after every load path (bindOrder, bindOrderByDocNo) and every
            // edit-mode toggle, so this is a single place that keeps the §5.25
            // reference link in sync regardless of how this record got here.
            UpdateRemarksReferenceLink();
        }

        // §5.25 - repair/replacement: "nothing chargeable" cites the closed original SO
        // read-only from dispatching; "something chargeable" raises a new SQ/SO, and the
        // link back to that original sale is made on the new SO's REMARKS, typed as the
        // original SO# and rendered as a clickable link (not a dedicated reference field
        // - the spec is explicit none is added, and the SQ carries no reference fields
        // at all). Detects "SO#<digits>" anywhere in the free-typed text.
        private static readonly System.Text.RegularExpressions.Regex RemarksSoRefPattern =
            new System.Text.RegularExpressions.Regex(@"SO#(\d+)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        private void UpdateRemarksReferenceLink()
        {
            var match = RemarksSoRefPattern.Match(txt_remarks.Text ?? string.Empty);

            if (match.Success)
            {
                string referencedSo = match.Groups[1].Value;
                lnk_remarks_ref.Text = "Go to SO#" + referencedSo;
                lnk_remarks_ref.Tag = referencedSo;
                lnk_remarks_ref.Visible = true;
            }
            else
            {
                lnk_remarks_ref.Visible = false;
                lnk_remarks_ref.Tag = null;
            }
        }

        private void lnk_remarks_ref_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            string referencedSo = lnk_remarks_ref.Tag as string;
            if (string.IsNullOrEmpty(referencedSo)) return;

            // Read-only citation only, same as dispatching's own use of a closed SO
            // (§5.4/§5.25): opens the original order in a new tab exactly like RedBox's
            // own document links (new Orders(rawDocNo) + TriggerNewForm). It never
            // reopens or rewrites that order - Orders.cs has no path that unlocks a
            // CLOSED order for editing, so viewing it this way is inherently safe.
            Orders ordersPage = new Orders(referencedSo);
            TriggerNewForm?.Invoke(DocumentNo.Apply(referencedSo, "SO#"), ordersPage);
        }
        private void btn_edit_Click(object sender, EventArgs e)
        {
            if (isCreatingNewOrder)
            {
                return;
            }

            isEditingExisting = true;
            CheckStatus();
        }
        // Leaves edit mode without saving - reloads the order's original values
        // (discarding whatever was typed) and re-locks the form. Only reachable
        // while editing an existing order (see CheckStatus's btn_cancel_edit.Visible).
        private void btn_cancel_edit_Click(object sender, EventArgs e)
        {
            isEditingExisting = false;

            bool matchesExistingOrder = !string.IsNullOrEmpty(documentNo)
                && OrderList != null
                && OrderList.Select($"document_no = '{documentNo}'").Length > 0;

            if (matchesExistingOrder)
            {
                bindOrderByDocNo(documentNo, true);
            }
            else
            {
                bindOrder(true);
            }
            CalculateTotalPrice();
        }
        private async void SaveSalesOrder()
        {
            try
            {
                List<string> missingFields = new List<string>();

                if (string.IsNullOrWhiteSpace(txt_receiver.Text)) missingFields.Add("Receiver");
                if (string.IsNullOrWhiteSpace(txt_contact_no.Text)) missingFields.Add("Contact Number");
                //if (string.IsNullOrWhiteSpace(txt_ref_po.Text)) missingFields.Add("Reference PO");
                txt_status.Text = SetDefaultIfEmpty(txt_status.Text);

                if (missingFields.Count > 0)
                {
                    MessageBox.Show("Please fill in the following fields: " + string.Join(", ", missingFields), "Missing Information", MessageBoxButtons.OK);
                    return;
                }

                var parentDataHeader = Helpers.GetControlsValues(pnl_header);
                var parentDataFooter = Helpers.GetControlsValues(pnl_footer);
                var parentDataHeader2 = Helpers.GetControlsValues(pnl_header_2);
                var parentDataFooter2 = Helpers.GetControlsValues(pnl_footer_2);
                var quoteIdValue = ((TextBox)pnl_header_2.Controls["txt_quotation_id"]).Text;
                if (string.IsNullOrEmpty(txt_id.Text))
                {
                    txt_id.Text = quoteIdValue;
                }

                var txtIdValue = ((TextBox)pnl_header_2.Controls["txt_id"]).Text;
                var docno = ((TextBox)pnl_header_2.Controls["txt_document_no"]).Text;
                parentDataHeader2["quotation_id"] = txtIdValue;
                if (parentDataHeader2.ContainsKey("payment_terms_id") && parentDataHeader2["payment_terms_id"] is string shipto)
                {
                    if (int.TryParse(shipto, out int shiptoId))
                    {
                        parentDataHeader2["payment_terms_id"] = shiptoId;
                    }
                    else
                    {
                        MessageBox.Show("Invalid ship to ID");
                        return;
                    }
                }

                if (parentDataHeader2.ContainsKey("doc") && parentDataHeader2["doc"] is string documentNo)
                {
                    parentDataHeader2["doc"] = DocumentNo.Strip(documentNo);
                }
                if (parentDataHeader2.ContainsKey("document_no") && parentDataHeader2["document_no"] is string document_no)
                {
                    // Store the bare number. This stripped only "FQ#", so a source that still
                    // carried "Q#" (e.g. an SO built before the reference was normalized) was
                    // saved as "Q#0007" instead of "0007" - which then displayed as
                    // "FQ#Q#0007" once UpdateTextBoxes re-applied the prefix. Strip every
                    // prefix so what's stored matches the bare-number rows (SO id 1 = "0004").
                    parentDataHeader2["document_no"] = StripDocPrefix(document_no);
                }

                var columnsToConvert = new List<string> { "ship_to_id", "bill_to_id", "customer_id", "quotation_id", "ref_po" };
                foreach (var column in columnsToConvert)
                {
                    if (parentDataHeader2.ContainsKey(column) && parentDataHeader2[column] is string columnValue)
                    {
                        if (!int.TryParse(columnValue, out int parsedValue))
                        {
                            MessageBox.Show($"Invalid {column} value. It must be a valid integer.");
                            return;
                        }
                        parentDataHeader2[column] = parsedValue;
                    }
                }
               
                var parentData = MergeDictionaries(parentDataHeader, parentDataHeader2, parentDataFooter, parentDataFooter2);

                string projectName = parentDataHeader["project_name"]?.ToString();
                var dataSource = Helpers.ConvertDataGridViewToDataTable(dgv_order_sales);
                if (!string.IsNullOrEmpty(projectName))
                {
                    dataSource = Helpers.ConvertDataGridViewToDataTable(dgv_project);
                }

                List<Dictionary<string, dynamic>> orderDetailsList = new List<Dictionary<string, dynamic>>();
                string docNumber = DocumentNo.Strip(txt_doc.Text);
                bool isExistingDoc = OrderList.Rows.Cast<DataRow>().Any(row => row["doc"].ToString() == docNumber);
                bool InSalesOrderDGV = false;

                if (isExistingDoc)
                {
                    dataSource = Helpers.ConvertDataGridViewToDataTable(dgv_order_sales);
                    InSalesOrderDGV = true;
                }
                foreach (DataRow item in dataSource.Rows)
                {
                    Dictionary<string, object> data = new Dictionary<string, object>();
                    if (InSalesOrderDGV)
                    {
                        data.Add("based_id", int.Parse(item["basedid"].ToString()));
                        if (!isExistingDoc) // If it's an insert
                        {
                            data.Add("quotation_quick_id", int.Parse(item["quick_quote_id"].ToString()));
                        }

                        data.Add("order_details_id", int.Parse(item["order_details_id"].ToString()));
                        data.Add("numbering", (item["number1"].ToString()));
                        data.Add("qty", int.Parse(item["qtydgv"].ToString()));

                        Console.WriteLine("Number: " + item["number1"].ToString() +" Item Code:" + item["itemcodedgv"].ToString() + " QTY:" + item["qtydgv"].ToString());

                        data.Add("item_code", (item["itemcodedgv"].ToString()));
                        data.Add("item_description", (item["shortdesc"].ToString()));
                        data.Add("delivery_preference", (item["delivery_preference"].ToString()));
                        data.Add("list_price", float.Parse(item["unitprice"].ToString()));
                        data.Add("total_price", float.Parse(item["linetotal"].ToString()));
                        data.Add("item_id", int.Parse(item["itemid"].ToString()));
                        data.Add("status", item["status"].ToString());
                        data.Add("has_stocks", bool.Parse(item["checkHasStock"].ToString()));
                        // Re-saving an already-existing order: dgv_order_sales here is bound
                        // from the previously-fetched DetailsList, which only carries
                        // "item_set_header" once the API model/DB column for it exists -
                        // guard against the column being absent so resaving an order doesn't
                        // break before that ships.
                        data.Add("item_set_header", dataSource.Columns.Contains("item_set_header")
                            ? (item["item_set_header"]?.ToString() ?? "")
                            : "");
                    }
                    else if (!string.IsNullOrEmpty(projectName))
                    {
                        // dgv_project's rows aren't all real, purchasable items - bindProject
                        // adds purely structural rows for display grouping: the itemset
                        // "header" row (labeled with the tab name, e.g. "A1") and BOM
                        // group-head rows. Both always carry item_id = 0, since neither
                        // represents an actual item. Sending them through as order details hit
                        // a FOREIGN KEY violation on z_tbl_trans_sales_order_details_at (item_id
                        // 0 doesn't exist in tbl_setup_item) - skip anything without a real
                        // item_id instead of trying to save it as a line item.
                        int projectItemId = int.TryParse(item["itemiddgv"].ToString(), out int parsedProjectItemId) ? parsedProjectItemId : 0;
                        if (projectItemId <= 0)
                            continue;

                        data.Add("based_id", int.Parse(item["basedidproject"].ToString()));
                        data.Add("numbering", (item["number"].ToString()));
                        data.Add("qty", int.Parse(item["qtyproject"].ToString()));
                        data.Add("item_code", (item["itemcode"].ToString()));
                        data.Add("item_description", (item["short_descproject"].ToString()));
                        data.Add("delivery_preference", (item["delivery_preferenceproject"].ToString()));
                        data.Add("list_price", float.Parse(item["listpriceproject"].ToString()));
                        data.Add("total_price", float.Parse(item["componenttotalproject"].ToString()));
                        data.Add("item_id", projectItemId);
                        data.Add("status", item["statusproject"].ToString());

                        // dgv_project (unlike dgv_order_sales) has no "checkHasStock" column at
                        // all - it was never given the stock-check UI/column the quick-quote
                        // grid has, so reading item["checkHasStock"] here always threw "Column
                        // 'checkHasStock' does not belong to table" and aborted the whole save
                        // before any project-order rows were persisted (which is also why they
                        // never made it into the purchase list downstream). Compute has_stocks
                        // the same way the quick-quote branch above does, from this row's own
                        // qty instead.
                        string projectQty = string.IsNullOrEmpty(item["qtyproject"].ToString()) ? "0" : item["qtyproject"].ToString();
                        data.Add("has_stocks", int.Parse(projectQty) > 0 ? false : true);

                        // Carry the itemset's tab name onto this real item row so printing
                        // can re-insert the (dynamic, per-project) header row it belongs to -
                        // the header row itself is never saved (item_id = 0 rows are skipped
                        // above).
                        // Guarded the same way the re-save branch above is: a missing column
                        // throws out of the whole foreach and aborts the save before a single
                        // detail row is written, so one absent column silently costs the
                        // entire order rather than just its header labels.
                        data.Add("item_set_header", dataSource.Columns.Contains("item_set_header_name")
                            ? (item["item_set_header_name"]?.ToString() ?? "")
                            : "");
                    }
                    else
                    {
                        data.Add("based_id", int.Parse(item["basedid"].ToString()));
                        if (!isExistingDoc) // If it's an insert
                        {
                            data.Add("quotation_quick_id", int.Parse(item["quick_quote_id"].ToString()));
                        }
                        data.Add("numbering", (item["number1"].ToString()));
                        data.Add("qty", int.Parse(item["qtydgv"].ToString()));
                        data.Add("item_code", (item["itemcodedgv"].ToString()));
                        data.Add("item_description", (item["shortdesc"].ToString()));
                        data.Add("delivery_preference", (item["delivery_preference"].ToString()));
                        data.Add("list_price", float.Parse(item["unitprice"].ToString()));
                        data.Add("total_price", float.Parse(item["linetotal"].ToString()));
                        data.Add("item_id", int.Parse(item["itemid"].ToString()));
                        data.Add("status", item["status"].ToString());
                        data.Add("has_stocks", bool.Parse(item["checkHasStock"].ToString()));
                    }
                    orderDetailsList.Add(data);
                }

                if (orderDetailsList != null)
                {
                    List<Dictionary<string, dynamic>> childCollection = new List<Dictionary<string, dynamic>>();
                    foreach (var childData in orderDetailsList)
                    {
                        childCollection.Add(childData);
                    }
                    parentData["sales_order_details"] = childCollection;

                    if (parentData.ContainsKey("sales_order_details"))
                    {
                        if (isExistingDoc)
                        {
                            var success = await OrderService.Update(parentData);

                            // Bug #095 (Trello, "Unable to save SALES ORDER"): RequestToApi
                            // deserializes and returns the response body the exact same way
                            // whether the HTTP call succeeded OR the server rejected it (400/
                            // 500 with {"success": false, "message": "..."}) - only a network
                            // exception or malformed body ever actually returns null. So
                            // "success != null" was true on a rejected save too, and the code
                            // showed "Data updated successfully" and reset the form while
                            // nothing was actually persisted - the real reason the API gave
                            // was there in success.message/Message and never shown.
                            if (success != null && success.Success)
                            {
                                MessageBox.Show("Data updated successfully");
                                await FetchSalesOrder(true);
                                bindOrderByDocNo(docno, true);
                                // Close back out of edit mode now that the update is saved.
                                isEditingExisting = false;
                                CheckStatus();
                            }
                            else
                            {
                                MessageBox.Show("Failed to update Sales Order.\n" + (success?.Message ?? success?.message ?? "No response from server."));
                            }
                        }
                        else
                        {
                            // Block creating a second Sales Order from a Sales Quotation
                            // that's already been converted - a quotation should only
                            // ever become one Sales Order.
                            var quotationIdStr = parentDataHeader2.ContainsKey("quotation_id")
                                ? parentDataHeader2["quotation_id"]?.ToString()
                                : null;

                            if (!string.IsNullOrWhiteSpace(quotationIdStr))
                            {
                                bool duplicateQuotation = OrderList.Rows.Cast<DataRow>().Any(row =>
                                    row["quotation_id"] != DBNull.Value &&
                                    row["quotation_id"].ToString() == quotationIdStr);

                                if (duplicateQuotation)
                                {
                                    MessageBox.Show(
                                        "A Sales Order already exists for this Sales Quotation. " +
                                        "A quotation can only be converted to one Sales Order.",
                                        "Duplicate Sales Order", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                    return;
                                }
                            }

                            //added the sales when saving the first time the SO
                            parentData["sales_executive"] = CacheData.CurrentUser.first_name + " " + CacheData.CurrentUser.last_name;

                            var success = await OrderService.Insert(parentData);

                            // Bug #095 (Trello) - see the matching comment on the Update
                            // branch above: success != null was never actually gated on the
                            // API's own success flag, so a rejected Sales Order still showed
                            // "Data added successfully".
                            if (success != null && success.Success)
                            {
                                MessageBox.Show("Data added successfully");
                                await FetchSalesOrder(true);
                                CheckStatus();
                                // Finalized quotation -> new Sales Order: once the first
                                // save succeeds, close out of edit mode (hide Save/Back,
                                // show New) instead of leaving the form sitting open in
                                // the same editable state.
                                ViewEnable();
                            }
                            else
                            {
                                MessageBox.Show("Failed to save Sales Order.\n" + (success?.Message ?? success?.message ?? "No response from server."));
                            }

                        }
                        TV1_preview.Visible = false;
                        TV2_preview.Visible = false;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message + "\n\n" + "Stack Trace: " + ex.StackTrace);
            }
        }

        public void ViewEnable()
        {
            btn_back.Visible = false;
            btn_new.Visible = true;
            // The order that was just created now exists, so this screen is no
            // longer "creating a new order" - fall back to normal view mode
            // (locked fields, Save/Delete hidden or gated the same way as any
            // other existing order, Edit available).
            isCreatingNewOrder = false;
            isEditingExisting = false;
            CheckStatus();
        }

        private void dgv_order_sales_CellMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            //Console.WriteLine(dgv_order_sales.Rows[e.RowIndex].Cells["delivery_preference"].Value.ToString());
        }

        private void dgv_order_sales_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if ((dgv_order_sales.Columns[e.ColumnIndex].Name == "checkHasStock" && e.Value != null))
            {
                bool hasStock = Convert.ToBoolean(e.Value);

                e.Value = hasStock ? "" : "!";
                e.CellStyle.ForeColor = hasStock ? dgv_order_sales.DefaultCellStyle.ForeColor : Color.Red;
                e.CellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                e.CellStyle.Font = new Font(dgv_order_sales.Font, FontStyle.Bold);
                e.FormattingApplied = true;
            }
        }

        private void dgv_order_sales_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            if (dgv_order_sales.Columns[e.ColumnIndex].Name == "qtydgv")
            {
                DataGridViewRow row = dgv_order_sales.Rows[e.RowIndex];

                if(!int.TryParse(row.Cells["allocated_qty"].Value?.ToString(), out int qtyAllocation))
                {
                    qtyAllocation = 0;
                }

                if (int.TryParse(row.Cells["qtydgv"].Value?.ToString(), out int qty))
                {
                    bool hasStock = qty <= qtyAllocation;
                    row.Cells["checkHasStock"].Value = hasStock;

                    dgv_order_sales.InvalidateCell(
                        dgv_order_sales.Columns["checkHasStock"].Index,
                        e.RowIndex
                    );
                }
            }
        }

        private void btn_refresh_Click(object sender, EventArgs e)
        {
            Orders_Load(sender, e);
        }

        private void toolStripButton1_Click(object sender, EventArgs e)
        {

        }

        private void BindControlsForNewOrderORexisting()
        {
            Helpers.ResetControls(pnl_header);
            Helpers.ResetControls(pnl_footer);
            Helpers.ResetControls(pnl_header_2);
            Helpers.ResetControls(pnl_footer_2);
            btn_search.Visible = false;
            btn_back.Visible = true;
            btn_prev.Visible = false;
            btn_next.Visible = false;
        }

        // SALES EXECUTIVE - the user who saved the order.
        //
        // The control used to be named txt_created_by, and Helpers.BindControls maps a
        // control to a column by its name minus the "txt_" prefix - so it looked for a
        // "created_by" column that does not exist on an order (the model and the table both
        // call it sales_executive). Nothing matched, so opening a saved order left the field
        // blank; only a brand-new order showed anything, because this method filled it in
        // directly. Renamed to txt_sales_executive so the binding finds the real column.
        //
        // Only the new-order paths call this, never the existing-order one, so it cannot
        // overwrite a loaded order's original executive with whoever opened it. On a resave
        // the box already holds the loaded value, so it round-trips unchanged - which is what
        // keeps RedBox's "only my own orders" filter pointing at the original creator.
        private void SetCreatedByToCurrentUser()
        {
            txt_sales_executive.Text = $"{CacheData.CurrentUser.first_name} {CacheData.CurrentUser.last_name}";
        }
    }
}
