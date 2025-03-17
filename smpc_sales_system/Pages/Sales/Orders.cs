using smpc_app.Services.Helpers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using smpc_app.Services.Helpers;
using smpc_sales_app.Data;
using smpc_sales_app.Services.Sales;
using smpc_sales_system.Services.Sales.Models;
using smpc_sales_app.Services.Helpers;
using smpc_sales_system.Models;
using smpc_sales_system.Pages;
using smpc_sales_system.Services.Sales;
using smpc_sales_system.Pages.Sales;

namespace smpc_sales_app.Pages.Sales
{
    public partial class Orders : UserControl
    {
        int SelectedRow = 0;
        private string documentNo;
        private ImageList imageList = new ImageList();

        public Orders(string documentNo = null)
        {
            InitializeComponent();
            Helpers.ResetControls(pnl_header);
            Helpers.ResetControls(pnl_footer);
            this.documentNo = documentNo;

            imageList.ImageSize = new Size(64, 64);  
            Image defaultIcon = smpc_sales_system.Properties.Resources.FileIcon;
            imageList.Images.Add("default", new Bitmap(defaultIcon, new Size(64, 64)));

            listView1.LargeImageList = imageList;
            listView1.View = View.LargeIcon;
        }
        private DataTable bpi_dt = new DataTable();
        private DataTable bpi_general = new DataTable();
        private DataTable bpi_address = new DataTable();
        private DataTable bpi_contacts = new DataTable();
        public DataTable OrderList { get; set; } = new DataTable();
        public DataTable DetailsList { get; set; } = new DataTable();
        public DataTable transactionList { get; set; } = new DataTable();
        public DataTable childList { get; set; } = new DataTable();
        public DataTable ItemList { get; set; } = new DataTable();
        public DataTable ItemSets { get; set; } = new DataTable();
        public DataTable ProjectItemList { get; set; } = new DataTable();
        public DataTable bom { get; set; } = new DataTable();
        public DataTable bomdetail { get; set; } = new DataTable();

        private async Task FetchData(bool isReload)
        {

            OrderList data = await OrderService.GetOrders();

            if (data != null && data.order != null && data.order.Any())
            {
                if (data != null && data.order != null && data.order.Count > 0)
                {
                    OrderList = JsonHelper.ToDataTable(data.order);
                }
                else
                {
                    OrderList = new DataTable();
                }
                if (data != null && data.sales_order_details != null && data.sales_order_details.Count > 0)
                {
                    DetailsList = JsonHelper.ToDataTable(data.sales_order_details);
                }
                else
                {
                    DetailsList = new DataTable();
                }

                if (!isReload && data != null)
                {
                    bindOrder(true);
                    CalculateTotalPrice();
                }
            }
            else
            {
                MessageBox.Show("There's no sales order now.");
            }
            
        }

        private async Task fetchItemData()
        {
            var itemData = await ItemService.GetItem();
            ItemList = JsonHelper.ToDataTable(itemData.items);
        }
        private async Task fetchBomData()
        {
            var bomData = await BomServices.GetBomsAsDatatable();
            bom = bomData;

            var bomData2 = await BomServices.GetBomsdetailAsDatatable();
            bomdetail = bomData2;
        }
        private async Task fetchBpiData()
        {
            Bpi_Class bpi_data = await QuotationService.GetBpiCustomers();
            bpi_dt = JsonHelper.ToDataTable(bpi_data.bpi);
            bpi_general = JsonHelper.ToDataTable(bpi_data.general);
            bpi_address = JsonHelper.ToDataTable(bpi_data.address);
            bpi_contacts = JsonHelper.ToDataTable(bpi_data.contacts);
        }
        private async Task fetchQuotationDetails()
        {
            SalesQuotationList data = await QuotationService.GetQuotations();
            transactionList = JsonHelper.ToDataTable(data.SalesQuotation);
            childList = JsonHelper.ToDataTable(data.SalesQuotationQuick);

            if (data != null)
            {
                //bindQuotation(true);
                CalculateTotalPrice();
                SOIncrementer();
            }
        }
        private async Task fetchProject()
        {
            SalesProjectList data = await ProjectService.GetProjects();
            ItemSets = JsonHelper.ToDataTable(data.sales_project_item_set);
            ProjectItemList = JsonHelper.ToDataTable(data.sales_project_items);

            if (data != null)
            {
                //bindQuotation(true);
            }
        }
        private void bindOrder(bool isBind = false)
        {
            if (isBind)
            {
                Panel[] pnlList = { pnl_header, pnl_header_2, pnl_footer, pnl_footer_2 };

                // Clone the OrderList schema to create HeaderList
                DataTable HeaderList = this.OrderList.Clone();
                HeaderList.Columns.Add("branch_name", typeof(string));
                HeaderList.Columns.Add("customer_code", typeof(string));
                HeaderList.Columns.Add("bill_to", typeof(string));
                HeaderList.Columns.Add("ship_to", typeof(string));
                HeaderList.Columns.Add("tin", typeof(string));

                // Only use the row from the OrderList that matches the SelectedRow index
                DataRow parentRow = this.OrderList.Rows[SelectedRow];
                DataRow newRow = HeaderList.NewRow();

                foreach (DataColumn col in this.OrderList.Columns)
                {
                    newRow[col.ColumnName] = parentRow[col.ColumnName];
                }

                // Get the relevant data from transactionList
                int quotationID = Convert.ToInt32(parentRow["quotation_id"]);
                string docnumber = (string)parentRow["document_no"];
                DataRow[] quotation = transactionList.Select($"id = '{quotationID}'");

                if (quotation.Length > 0)
                {
                    if (quotation[0]["project_name"] != DBNull.Value && !string.IsNullOrEmpty(quotation[0]["project_name"].ToString()))
                    {
                        label26.Visible = true;
                        txt_project_name.Visible = true;
                    }
                    else
                    {
                        label26.Visible = false;
                        txt_project_name.Visible = false;
                    }
                }
                string customerID = quotation[0]["customer_id"].ToString();

                // Get address info (ShipID and BillID)
                string ShipID = parentRow["ship_to_id"].ToString();
                string BillID = parentRow["bill_to_id"].ToString();
                DataRow[] bpiGenRows = bpi_general.Select($"general_based_id = '{customerID}'");
                DataRow[] billRows = bpi_address.Select($"address_id = '{BillID}'");
                DataRow[] shipRows = bpi_address.Select($"address_id = '{ShipID}'");

                if (bpiGenRows.Length > 0)
                {
                    newRow["branch_name"] = bpiGenRows[0]["branch_name"].ToString();
                    newRow["customer_code"] = bpiGenRows[0]["customer_code"].ToString();
                    string BasedID = bpiGenRows[0]["general_based_id"].ToString();
                    DataRow[] bpiRows = bpi_dt.Select($"id = '{BasedID}'");
                    if (bpiRows.Length > 0)
                    {
                        newRow["tin"] = bpiRows[0]["tin"].ToString();
                    }
                    else
                    {
                        newRow["tin"] = "No TIN";
                    }

                    if (billRows.Length > 0)
                    {
                        newRow["bill_to"] = billRows[0]["location"].ToString();
                        newRow["ship_to"] = shipRows[0]["location"].ToString();
                    }
                    else
                    {
                        newRow["bill_to"] = "No Location";
                    }
                }
                else
                {
                    newRow["branch_name"] = "Unknown Customer";
                    newRow["customer_code"] = "N/A";
                }

                // Add the new row to HeaderList
                HeaderList.Rows.Add(newRow);

                // Bind the controls with the data
                Helpers.BindControls(pnlList, HeaderList, 0);

                // Iterate through controls to adjust text
                foreach (var pnl in pnlList)
                {
                    foreach (Control control in pnl.Controls)
                    {
                        if (control is TextBox textBox && textBox.Name.Contains("txt_document_no"))
                        {
                            if (!textBox.Text.StartsWith("Q#"))
                            {
                                textBox.Text = "Q#" + textBox.Text;
                            }
                        }
                        if (control is TextBox textBox2 && textBox2.Name.Contains("txt_doc"))
                        {
                            if (!textBox2.Text.StartsWith("SO#"))
                            {
                                textBox2.Text = "SO#" + textBox2.Text;
                            }
                        }
                        if (control is TextBox textBox3 && textBox3.Name.Contains("txt_document_no"))
                        {
                            if (textBox3.Text.StartsWith("SO#"))
                            {
                                textBox3.Text = textBox3.Text.Substring(3);
                            }
                        }
                    }
                }

                // Set default status if empty
                if (string.IsNullOrEmpty(txt_status.Text))
                {
                    txt_status.Text = "-";
                }

                // Set date and other details based on SelectedRow
                dtp_date.Value = Convert.ToDateTime(OrderList.Rows[SelectedRow]["date"]);
                dtp_delivery_date.Value = Convert.ToDateTime(OrderList.Rows[SelectedRow]["delivery_date"]);
                cmb_payment_terms.SelectedValue = this.OrderList.Rows[this.SelectedRow]["payment_terms_id"].ToString();
                cmb_payment_terms.SelectedItem = this.OrderList.Rows[this.SelectedRow]["payment_terms_id"].ToString();
                cmb_ship_type.SelectedValue = this.OrderList.Rows[this.SelectedRow]["ship_type_id"].ToString();
                cmb_ship_type.SelectedItem = this.OrderList.Rows[this.SelectedRow]["ship_type_id"].ToString();

                string orderId = this.OrderList.Rows[this.SelectedRow]["order_id"].ToString();
                DataView filteredDetailsView = new DataView(this.DetailsList);
                filteredDetailsView.RowFilter = $"based_id = '{orderId}'";
                
                // Set the filtered data source to the DataGridView
                dgv_order_sales.DataSource = filteredDetailsView;

                // Call CheckStatus if needed
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


                // Filter the rows based on the passed documentNo
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
                        if (quotation[0]["project_name"] != DBNull.Value && !string.IsNullOrEmpty(quotation[0]["project_name"].ToString()))
                        {
                            label26.Visible = true;
                            txt_project_name.Visible = true;
                        }
                        else
                        {
                            label26.Visible = false;
                            txt_project_name.Visible = false;
                        }
                    }

                    string customerID = quotation[0]["customer_id"].ToString();
                    newRow["vat_amount"] = quotation[0]["vat_amount"].ToString();
                    newRow["gross_sales"] = quotation[0]["gross_sales"].ToString();
                    newRow["total_amount_due"] = quotation[0]["total_amount_due"].ToString();

                    string ShipID = parentRow["ship_to_id"].ToString();
                    string BillID = parentRow["bill_to_id"].ToString();
                    DataRow[] bpiGenRows = bpi_general.Select($"general_based_id = '{customerID}'");
                    DataRow[] billRows = bpi_address.Select($"address_id = '{BillID}'");
                    DataRow[] shipRows = bpi_address.Select($"address_id = '{ShipID}'");

                    if (bpiGenRows.Length > 0)
                    {
                        newRow["branch_name"] = bpiGenRows[0]["branch_name"].ToString();
                        newRow["customer_code"] = bpiGenRows[0]["customer_code"].ToString();
                        string BasedID = bpiGenRows[0]["general_based_id"].ToString();
                        DataRow[] bpiRows = bpi_dt.Select($"id = '{BasedID}'");
                        if (bpiRows.Length > 0)
                        {
                            newRow["tin"] = bpiRows[0]["tin"].ToString();
                        }
                        else
                        {
                            newRow["tin"] = "No TIN";
                        }

                        if (billRows.Length > 0)
                        {
                            newRow["bill_to"] = billRows[0]["location"].ToString();
                            newRow["ship_to"] = shipRows[0]["location"].ToString();
                        }
                        else
                        {
                            newRow["bill_to"] = "No Location";
                        }
                    }
                    else
                    {
                        newRow["branch_name"] = "Unknown Customer";
                        newRow["customer_code"] = "N/A";
                    }

                    HeaderList.Rows.Add(newRow);

                    Helpers.BindControls(pnlList, HeaderList, SelectedRow);

                    cmb_payment_terms.SelectedValue = filteredRows[0]["payment_terms_id"].ToString();
                    cmb_payment_terms.SelectedItem = filteredRows[0]["payment_terms_id"].ToString();

                    cmb_ship_type.SelectedValue = filteredRows[0]["ship_type_id"].ToString();
                    cmb_ship_type.SelectedItem = filteredRows[0]["ship_type_id"].ToString();

                    // Iterate through the controls and set the document numbers
                    foreach (var pnl in pnlList)
                    {
                        foreach (Control control in pnl.Controls)
                        {
                            if (control is TextBox textBox && textBox.Name.Contains("txt_document_no"))
                            {
                                if (!textBox.Text.StartsWith("Q#"))
                                {
                                    textBox.Text = "Q#" + textBox.Text;
                                }
                            }
                            if (control is TextBox textBox2 && textBox2.Name.Contains("txt_doc"))
                            {
                                if (!textBox2.Text.StartsWith("SO#"))
                                {
                                    textBox2.Text = "SO#" + textBox2.Text;
                                }
                            }
                            if (control is TextBox textBox3 && textBox3.Name.Contains("txt_document_no"))
                            {
                                if (textBox3.Text.StartsWith("SO#"))
                                {
                                    textBox3.Text = textBox3.Text.Substring(3);
                                }
                            }
                        }
                    }

                    if (string.IsNullOrEmpty(txt_status.Text))
                    {
                        txt_status.Text = "-";
                    }   

                    dtp_date.Value = Convert.ToDateTime(parentRow["date"]);
                    dtp_delivery_date.Value = Convert.ToDateTime(parentRow["delivery_date"]);
                    DataView ordertable = new DataView(this.OrderList);
                    ordertable.RowFilter = $"document_no = '{documentNo}'";
                    string orderId = ordertable[0]["order_id"].ToString();

                    DataView filteredDetailsView = new DataView(this.DetailsList);
                    filteredDetailsView.RowFilter = $"based_id = '{orderId}'";

                    dgv_order_sales.DataSource = filteredDetailsView;
                    CheckStatus();
                }
            }
        }
        private void bindQuotation(string documentNo, bool isBind = false)
        {
            if (!isBind) return;

            // Initialize necessary controls and DataTables
            Panel[] pnlList = { pnl_header, pnl_header_2, pnl_footer, pnl_footer_2 };
            DataTable headerList = CreateHeaderListStructure();

            // Filter the transactionList based on documentNo
            DataRow[] filteredRows = this.transactionList.Select($"document_no = '{documentNo}'");

            if (filteredRows.Length == 0) return;

            foreach (DataRow parentRow in filteredRows)
            {
                DataRow newRow = CreateHeaderRow(parentRow, headerList);

                // Add filtered customer information
                int customerId = (int)parentRow["customer_id"];
                string shipId = parentRow["ship_to_id"].ToString();
                string billId = parentRow["bill_to_id"].ToString();

                AddCustomerInformation(newRow, customerId, shipId, billId);

                // Add row to the header list
                headerList.Rows.Add(newRow);
            }

            // Handle project-specific logic
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
            SetQuotationDetails(firstRow);

            // Bind controls and update the textboxes
            Helpers.BindControls(pnlList, headerList, SelectedRow);
            UpdateDocumentNumberTextBoxes(pnlList);

            // Bind the child items to the DataGridView
            DataTable withItemList = CreateItemList();
            BindItemListToDataGridView(withItemList, firstRow);
        }

        private DataTable CreateHeaderListStructure()
        {
            var headerList = this.transactionList.Clone();
            headerList.Columns.Add("branch_name", typeof(string));
            headerList.Columns.Add("customer_code", typeof(string));
            headerList.Columns.Add("bill_to", typeof(string));
            headerList.Columns.Add("ship_to", typeof(string));
            headerList.Columns.Add("tin", typeof(string));

            return headerList;
        }

        private DataRow CreateHeaderRow(DataRow parentRow, DataTable headerList)
        {
            DataRow newRow = headerList.NewRow();
            foreach (DataColumn col in this.transactionList.Columns)
            {
                newRow[col.ColumnName] = parentRow[col.ColumnName];
            }
            return newRow;
        }
            
        private void AddCustomerInformation(DataRow newRow, int customerId, string shipId, string billId)
        {
            DataRow[] bpiGenRows = bpi_general.Select($"general_based_id = '{customerId}'");
            DataRow[] billRows = bpi_address.Select($"address_id = '{billId}'");
            DataRow[] shipRows = bpi_address.Select($"address_id = '{shipId}'");

            if (bpiGenRows.Length > 0)
            {
                newRow["branch_name"] = bpiGenRows[0]["branch_name"].ToString();
                newRow["customer_code"] = bpiGenRows[0]["customer_code"].ToString();
                string basedId = bpiGenRows[0]["general_based_id"].ToString();

                // Fetch TIN
                DataRow[] bpiRows = bpi_dt.Select($"id = '{basedId}'");
                newRow["tin"] = bpiRows.Length > 0 ? bpiRows[0]["tin"].ToString() : "No TIN";

                // Fetch billing and shipping locations
                newRow["bill_to"] = billRows.Length > 0 ? billRows[0]["location"].ToString() : "No Location";
                newRow["ship_to"] = shipRows.Length > 0 ? shipRows[0]["location"].ToString() : "No Location";
            }
            else
            {
                newRow["branch_name"] = "Unknown Customer";
                newRow["customer_code"] = "N/A";
            }
        }

        private void SetQuotationDetails(DataRow firstRow)
        {
            cmb_payment_terms.SelectedValue = firstRow["payment_terms_id"].ToString();
            cmb_payment_terms.SelectedItem = firstRow["payment_terms_id"].ToString();

            cmb_ship_type.SelectedValue = firstRow["ship_type_id"].ToString();
            cmb_ship_type.SelectedItem = firstRow["ship_type_id"].ToString();
        }

        private void UpdateDocumentNumberTextBoxes(Panel[] pnlList)
        {
            foreach (var pnl in pnlList)
            {
                foreach (Control control in pnl.Controls)
                {
                    if (control is TextBox textBox && textBox.Name.Contains("txt_document_no") && !textBox.Text.StartsWith("Q#"))
                    {
                        textBox.Text = "Q#" + textBox.Text;
                    }
                }
            }
        }

        private DataTable CreateItemList()
        {
            // Clone the child list and add new columns for item details
            DataTable withItemList = this.childList.Clone();
            withItemList.Columns.Add("item_description", typeof(string));
            withItemList.Columns.Add("item_code", typeof(string));
            withItemList.Columns.Add("item_model", typeof(string));
            withItemList.Columns.Add("numbering", typeof(string));
            int itemcounter = 1;

            // Iterate through child rows and add item info
            foreach (DataRow childRow in this.childList.Rows)
            {
                DataRow newRow = withItemList.NewRow();
                foreach (DataColumn col in childList.Columns)
                {
                    newRow[col.ColumnName] = childRow[col.ColumnName];
                }

                // Add item description and code
                string itemId = childRow["item_id"].ToString();
                DataRow[] itemRows = ItemList.Select($"id = '{itemId}'");
                if (itemRows.Length > 0)
                {
                    newRow["item_description"] = $"{itemRows[0]["item_model"]} - {itemRows[0]["short_desc"]}";
                    newRow["item_code"] = itemRows[0]["item_code"].ToString();
                    newRow["numbering"] = itemcounter;
                }
                else
                {
                    newRow["item_description"] = "Unknown Item";
                    newRow["item_code"] = "N/A";
                    newRow["numbering"] = itemcounter;
                }
                itemcounter += 1;
                withItemList.Rows.Add(newRow);
            }

            return withItemList;
        }

        private void BindItemListToDataGridView(DataTable withItemList, DataRow firstRow)
        {
            // Filter the item list based on the document ID
            DataView dataView = new DataView(withItemList);
            dataView.RowFilter = $"based_id = '{Convert.ToInt32(firstRow["id"])}'";

            // Bind the filtered data to the DataGridView
            dgv_order_sales.DataSource = dataView;
        }

        private void bindProject(string documentNo, bool isBind = false)
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

                // Filter the transactionList based on document_no
                DataRow[] filteredRows = this.transactionList.Select($"document_no = '{documentNo}'");

                if (filteredRows.Length > 0)
                {
                    foreach (DataRow parentRow in filteredRows)
                    {
                        DataRow newRow = HeaderList.NewRow();
                        foreach (DataColumn col in this.transactionList.Columns)
                        {
                            newRow[col.ColumnName] = parentRow[col.ColumnName];
                        }

                        int ID = (int)parentRow["customer_id"];
                        string ShipID = parentRow["ship_to_id"].ToString();
                        string BillID = parentRow["bill_to_id"].ToString();
                        DataRow[] bpiGenRows = bpi_general.Select($"general_based_id = '{ID}'");
                        DataRow[] billRows = bpi_address.Select($"address_id = '{BillID}'");
                        DataRow[] shipRows = bpi_address.Select($"address_id = '{ShipID}'");

                        if (bpiGenRows.Length > 0)
                        {
                            newRow["branch_name"] = bpiGenRows[0]["branch_name"].ToString();
                            newRow["customer_code"] = bpiGenRows[0]["customer_code"].ToString();
                            string BasedID = bpiGenRows[0]["general_based_id"].ToString();
                            DataRow[] bpiRows = bpi_dt.Select($"id = '{BasedID}'");

                            if (bpiRows.Length > 0)
                            {
                                newRow["tin"] = bpiRows[0]["tin"].ToString();
                            }
                            else
                            {
                                newRow["tin"] = "No TIN";
                            }

                            if (billRows.Length > 0)
                            {
                                newRow["bill_to"] = billRows[0]["location"].ToString();
                                newRow["ship_to"] = shipRows[0]["location"].ToString();
                            }
                            else
                            {
                                newRow["bill_to"] = "No Location";
                            }
                        }
                        else
                        {
                            newRow["branch_name"] = "Unknown Customer";
                            newRow["customer_code"] = "N/A";
                        }

                        HeaderList.Rows.Add(newRow);
                    }
                        cmb_payment_terms.SelectedValue = filteredRows[0]["payment_terms_id"].ToString();
                        cmb_payment_terms.SelectedItem = filteredRows[0]["payment_terms_id"].ToString();

                        cmb_ship_type.SelectedValue = filteredRows[0]["ship_type_id"].ToString();
                        cmb_ship_type.SelectedItem = filteredRows[0]["ship_type_id"].ToString();
                }

                Helpers.BindControls(pnlList, HeaderList, 0);

                foreach (var pnl in pnlList)
                {
                    foreach (Control control in pnl.Controls)
                    {
                        if (control is TextBox textBox && textBox.Name.Contains("txt_document_no"))
                        {
                            if (!textBox.Text.StartsWith("Q#"))
                            {
                                textBox.Text = "Q#" + textBox.Text;
                            }
                        }
                    }
                }

                DataView dataview = new DataView(ItemSets);
                dataview.RowFilter = "based_id = '" + Convert.ToInt32(filteredRows[0]["id"]) + "'";

                var ids = new List<int>();
                foreach (DataRowView rowView in dataview)
                {
                    int id = Convert.ToInt32(rowView["itemset_id"]);
                    ids.Add(id);
                }

                double bomCounter = 0; // Primary counter
                int bomDetailIndex = 1; // Sub-counter for bom details

                DataView dataview2 = new DataView(ProjectItemList);

                if (ids.Count > 0)
                {
                    // Filter dataview2 based on the extracted IDs
                    string idFilter = string.Join("','", ids); // Join IDs with commas for SQL-like filtering
                    dataview2.RowFilter = $"based_id IN ('{idFilter}')"; // Use IN to filter by multiple IDs
                    DataTable transformedTable = dataview2.ToTable();

                    DataTable withItemListTwo = this.ProjectItemList.Clone();
                    withItemListTwo.Columns.Add("short_desc", typeof(string));
                    withItemListTwo.Columns.Add("item_code", typeof(string));
                    withItemListTwo.Columns.Add("number", typeof(string));

                    // Process each row
                    foreach (DataRow row in transformedTable.Rows)
                    {
                        // Check if item_id > 0 and bom_id == 0
                        int itemId = Convert.ToInt32(row["item_id"]);
                        int bomId = Convert.ToInt32(row["bom_id"]);
                        DataRow newRow = withItemListTwo.NewRow();

                        // Copy columns from transformedTable to newRow
                        foreach (DataColumn col in transformedTable.Columns)
                        {
                            newRow[col.ColumnName] = row[col.ColumnName];
                        }

                        // If itemId == 0 and bomId == 0, then it's a main counter row
                        if (itemId == 0 && bomId == 0)
                        {
                            
                            string model = row["model"].ToString();
                            if (string.IsNullOrEmpty(model))
                            {
                                continue; // Skip this row if there's no model
                            }
                            bomCounter += 1;
                            // Increment primary counter for bomId == 0 and itemId == 0
                            newRow["number"] = bomCounter; // Use primary counter
                            newRow["item_code"] = row["components"].ToString(); // Assuming 'components' field is used for item code
                            withItemListTwo.Rows.Add(newRow);
                            if (bomDetailIndex > 1)
                            {
                                bomDetailIndex = 1;
                            }
                        }
                        // If itemId > 0 and bomId == 0, then it's a regular item
                        else if (itemId > 0 && bomId == 0)
                        {
                            if (bomDetailIndex > 1)
                            {
                                bomCounter += 1;
                                bomDetailIndex = 1;
                            }
                            DataRow[] itemRows = ItemList.Select($"id = {itemId}");

                            if (itemRows.Length > 0)
                            {
                                string itemCode = itemRows[0]["item_code"].ToString();
                                string shortDesc = itemRows[0]["short_desc"].ToString();

                                newRow["number"] = bomCounter; // Regular item row uses primary counter
                                newRow["short_desc"] = shortDesc;
                                newRow["item_code"] = itemCode;
                            }
                            else
                            {
                                newRow["number"] = bomCounter; // Use primary counter
                                newRow["short_desc"] = "Unknown";
                                newRow["item_code"] = "Unknown";
                            }

                            withItemListTwo.Rows.Add(newRow);
                            bomCounter += 1;
                        }
                        // If bomId > 0 and itemId > 0, it's a sub-item under a BOM (item with BOM details)
                        else if (bomId > 0 && itemId > 0)
                        {
                            DataRow[] itemRows = ItemList.Select($"id = {itemId}");
                            if (itemRows.Length > 0)
                            {
                                string itemCode = itemRows[0]["item_code"].ToString();
                                newRow["number"] = $"{bomCounter}.{bomDetailIndex}"; // Sub-counter for BOM items
                                newRow["item_code"] = itemCode;
                            }
                            else
                            {
                                newRow["number"] = $"{bomCounter}.{bomDetailIndex}"; // Sub-counter
                                newRow["item_code"] = "Unknown";
                            }

                            withItemListTwo.Rows.Add(newRow);
                            bomDetailIndex += 1; // Increment sub-counter for each BOM item
                        }
                    }

                    dgv_project.DataSource = withItemListTwo;
                }

            }
        }

        private void bindProject2(string documentNo, bool isBind = false)
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

                // Filter the transactionList based on document_no
                DataRow[] filteredRows = this.OrderList.Select($"document_no = '{documentNo}'");

                    DataRow parentRow = filteredRows[0];
                    DataRow newRow = HeaderList.NewRow();

                    foreach(DataColumn col in this.OrderList.Columns)
                    {
                        newRow[col.ColumnName] = parentRow[col.ColumnName];
                    }

                    int quotationID = Convert.ToInt32(parentRow["quotation_id"]);
                    DataRow[] quotation = transactionList.Select($"id = '{quotationID}'");
                    int ID = (int)quotation[0]["customer_id"];
                        string ShipID = parentRow["ship_to_id"].ToString();
                        string BillID = parentRow["bill_to_id"].ToString();
                        DataRow[] bpiGenRows = bpi_general.Select($"general_based_id = '{ID}'");
                        DataRow[] billRows = bpi_address.Select($"address_id = '{BillID}'");
                        DataRow[] shipRows = bpi_address.Select($"address_id = '{ShipID}'");

                        if (bpiGenRows.Length > 0)
                        {
                            newRow["branch_name"] = bpiGenRows[0]["branch_name"].ToString();
                            newRow["customer_code"] = bpiGenRows[0]["customer_code"].ToString();
                            string BasedID = bpiGenRows[0]["general_based_id"].ToString();
                            DataRow[] bpiRows = bpi_dt.Select($"id = '{BasedID}'");

                            if (bpiRows.Length > 0)
                            {
                                newRow["tin"] = bpiRows[0]["tin"].ToString();
                            }
                            else
                            {
                                newRow["tin"] = "No TIN";
                            }

                            if (billRows.Length > 0)
                            {
                                newRow["bill_to"] = billRows[0]["location"].ToString();
                                newRow["ship_to"] = shipRows[0]["location"].ToString();
                            }
                            else
                            {
                                newRow["bill_to"] = "No Location";
                            }
                        }
                        else
                        {
                            newRow["branch_name"] = "Unknown Customer";
                            newRow["customer_code"] = "N/A";
                        }

                        HeaderList.Rows.Add(newRow);
                    
                    cmb_payment_terms.SelectedValue = filteredRows[0]["payment_terms_id"].ToString();
                    cmb_payment_terms.SelectedItem = filteredRows[0]["payment_terms_id"].ToString();

                    cmb_ship_type.SelectedValue = filteredRows[0]["ship_type_id"].ToString();
                    cmb_ship_type.SelectedItem = filteredRows[0]["ship_type_id"].ToString();
                

                Helpers.BindControls(pnlList, HeaderList, 0);

                foreach (var pnl in pnlList)
                {
                    foreach (Control control in pnl.Controls)
                    {
                        if (control is TextBox textBox && textBox.Name.Contains("txt_document_no"))
                        {
                            if (!textBox.Text.StartsWith("Q#"))
                            {
                                textBox.Text = "Q#" + textBox.Text;
                            }
                        }
                    }
                }

                DataView dataview = new DataView(ItemSets);
                dataview.RowFilter = "based_id = '" + Convert.ToInt32(filteredRows[0]["quotation_id"]) + "'";

                var ids = new List<int>();
                foreach (DataRowView rowView in dataview)
                {
                    int id = Convert.ToInt32(rowView["itemset_id"]);
                    ids.Add(id);
                }
                double bomCounter = 1;
                DataView dataview2 = new DataView(ProjectItemList);
                if (ids.Count > 0)
                {
                    // Now, filter dataview2 based on the extracted IDs
                    string idFilter = string.Join("','", ids); // Join IDs with commas for SQL-like filtering
                    dataview2.RowFilter = $"based_id IN ('{idFilter}')"; // Use IN to filter by multiple IDs
                    DataTable transformedTable = dataview2.ToTable();

                    DataTable withItemListTwo = this.ProjectItemList.Clone();
                    withItemListTwo.Columns.Add("short_desc", typeof(string));
                    withItemListTwo.Columns.Add("item_code", typeof(string));
                    withItemListTwo.Columns.Add("number", typeof(string));

                    // GET ITEM DETAILS BASED ON ITEM ID IF THERE'S ITEM ID
                    foreach (DataRow row in transformedTable.Rows)
                    {
                        // Check if item_id > 0 and bom_id == 0
                        int itemId = Convert.ToInt32(row["item_id"]);
                        int bomId = Convert.ToInt32(row["bom_id"]);
                        newRow = withItemListTwo.NewRow();

                        foreach (DataColumn col in transformedTable.Columns)
                        {
                            newRow[col.ColumnName] = row[col.ColumnName];
                        }
                        // If item_id is greater than 0 and bom_id is 0, perform the regular process
                        if (itemId > 0 && bomId == 0)
                        {
                            DataRow[] itemRows = ItemList.Select($"id = {itemId}");

                            if (itemRows.Length > 0)
                            {
                                string itemCode = itemRows[0]["item_code"].ToString();
                                string shortDesc = itemRows[0]["short_desc"].ToString();

                                newRow["number"] = bomCounter;
                                newRow["short_desc"] = shortDesc;
                                newRow["item_code"] = itemCode;
                            }
                            else
                            {
                                newRow["number"] = bomCounter;
                                newRow["short_desc"] = "Unknown";
                                newRow["item_code"] = "Unknown";
                            }
                            bomCounter += 1.0;
                            withItemListTwo.Rows.Add(newRow);
                        }
                        else if (bomId > 0 && itemId == 0)
                        {

                            DataRow[] bomRows = bom.Select($"id = {bomId}");
                            if (bomRows.Length > 0)
                            {
                                string itemCode = bomRows[0]["item_code"].ToString();
                                newRow["number"] = bomCounter;
                                newRow["item_code"] = itemCode;
                            }
                            else
                            {
                                newRow["number"] = bomCounter;
                                newRow["item_code"] = "Unknown";
                            }
                            withItemListTwo.Rows.Add(newRow);

                            DataRow[] bomdetailRows = bomdetail.Select($"item_bom_id = {bomId}");

                            if (bomdetailRows.Length > 0)
                            {
                                int bomDetailIndex = 1;
                                foreach (DataRow bomDetail in bomdetailRows)
                                {
                                    
                                    // Create a new row to add to the DataGridView
                                    DataRow newBomDetailRow = withItemListTwo.NewRow();

                                    foreach (DataColumn col in transformedTable.Columns)
                                    {
                                        newBomDetailRow[col.ColumnName] = newRow[col.ColumnName];
                                    }

                                    // For example, add BOM details like "bom_description"
                                    string itemcode = bomDetail["item_code"].ToString();
                                    string bomqty = bomDetail["bom_qty"].ToString();
                                    newBomDetailRow["number"] = $"{bomCounter}.{bomDetailIndex}";
                                    newBomDetailRow["item_code"] = itemcode;
                                    newBomDetailRow["qty"] = bomqty;

                                    bomDetailIndex += 1;
                                    // Add the new row to the DataGridView
                                    withItemListTwo.Rows.Add(newBomDetailRow);
                                }
                            }
                            bomCounter += 1.0;
                        }
                    }
                    dgv_project.DataSource = withItemListTwo;

                }
                else
                {
                    // If no IDs were found, you can handle this case accordingly
                    MessageBox.Show("No matching IDs found.");
                }
                CheckStatus();
            }
        }
        
        private async void Orders_Load(object sender, EventArgs e)
        {
            LoadDirectory(targetDirectory);
            // Initialize data sources for controls
            bs_payment_terms.DataSource = CacheData.PaymentTerms;
            bs_ship_type.DataSource = CacheData.ShipTypeSetup;

            // Fetch required data asynchronously
            await fetchQuotationDetails();
            await fetchProject();
            await fetchBpiData();
            await fetchItemData();
            await fetchBomData();
            await FetchData(false);

            // If document number is provided, perform further processing
            if (!string.IsNullOrEmpty(documentNo))
            {
                // Check if the documentNo exists in OrderList
                if (OrderList != null && OrderList.Rows.Count > 0)
                {
                    DataRow[] matchingRows = OrderList.Select($"document_no = '{documentNo}'");

                    if (matchingRows.Length > 0)
                    {
                        // Document exists in OrderList, bind order data
                        BindOrderControlsForExistingOrder();
                        bindOrderByDocNo(documentNo, true);
                    }
                    else
                    {
                        // Document not found in OrderList, bind quotation data
                        BindControlsForNewOrder();
                        bindQuotation(documentNo, true);
                        SOIncrementer();
                    }
                    CalculateTotalPrice();
                }

                // If document number is empty, fetch and display data
                if (string.IsNullOrEmpty(txt_document_no.Text))
                {
                    BindControlsForNewOrder();
                    FetchData(false);
                    CalculateTotalPrice();
                }

                // Check status after processing
                CheckStatus();
            }
            // Helpers.LoadDirectory("D:\\LIFESAVER\\LIFESAVER\\TEST", treeview_sales);
        }

        private void BindOrderControlsForExistingOrder()
        {
            // Reset the controls to prepare for existing order data
            Helpers.ResetControls(pnl_header);
            Helpers.ResetControls(pnl_footer);
            btn_search.Visible = false;
            btn_back.Visible = true;
            btn_prev.Visible = false;
            btn_next.Visible = false;
        }

        private void BindControlsForNewOrder()
        {
            // Reset the controls to prepare for a new order or quotation
            Helpers.ResetControls(pnl_header);
            Helpers.ResetControls(pnl_footer);
            btn_search.Visible = false;
            btn_back.Visible = true;
            btn_prev.Visible = false;
            btn_next.Visible = false;
        }
        private async void saving()
        {
            try
            {
                List<string> missingFields = new List<string>();

                // Check each field and add missing ones to the list
                if (string.IsNullOrWhiteSpace(txt_receiver.Text))
                {
                    missingFields.Add("Receiver");
                }
                if (string.IsNullOrWhiteSpace(txt_contact_no.Text))
                {
                    missingFields.Add("Contact Number");
                }
                if (string.IsNullOrWhiteSpace(txt_ref_po.Text))
                {
                    missingFields.Add("Reference PO");
                }
                if (string.IsNullOrEmpty(txt_status.Text))
                {
                    txt_status.Text = "-";
                }
                
                if (missingFields.Count > 0)
                {
                    string missingFieldsMessage = "Please fill in the following fields: " + string.Join(", ", missingFields);
                    MessageBox.Show(missingFieldsMessage, "Missing Information", MessageBoxButtons.OK);
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
                    parentDataHeader2["doc"] = documentNo.StartsWith("SO#")
                        ? documentNo.Substring(3)
                        : documentNo;
                }
                if (parentDataHeader2.ContainsKey("document_no") && parentDataHeader2["document_no"] is string document_no)
                {
                    parentDataHeader2["document_no"] = document_no.StartsWith("Q#")
                        ? document_no.Substring(2)
                        : document_no;
                }

                // List of columns to be converted to int
                var columnsToConvert = new List<string> { "ship_to_id", "bill_to_id", "customer_id", "quotation_id", "ref_po" };

                foreach (var column in columnsToConvert)
                {
                    if (parentDataHeader2.ContainsKey(column) && parentDataHeader2[column] is string columnValue)
                    {
                        if (int.TryParse(columnValue, out int parsedValue))
                        {
                            parentDataHeader2[column] = parsedValue;
                        }
                        else
                        {
                            MessageBox.Show($"Invalid {column} value. It must be a valid integer.");
                            return;
                        }
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

                // Get the document number (txt_doc) and remove the "SO#" prefix before checking
                string docNumber = txt_doc.Text.StartsWith("SO#")
                                    ? txt_doc.Text.Substring(3)  // Remove "SO#"
                                    : txt_doc.Text;

                // Check if the document number already exists in OrderList
                bool isExistingDoc = OrderList.Rows.Cast<DataRow>()
                    .Any(row => row["doc"].ToString() == docNumber);

                foreach (DataRow item in dataSource.Rows)
                {
                    Dictionary<string, object> data = new Dictionary<string, object>();
                    if (!string.IsNullOrEmpty(projectName))
                    {
                        data.Add("based_id", int.Parse(item["basedidproject"].ToString()));
                        if (!isExistingDoc) // If it's an insert
                        {
                            //data.Add("quotation_quick_id", int.Parse(item["quick_quote_id"].ToString()));
                        }
                        data.Add("numbering", (item["number"].ToString()));
                        data.Add("qty", int.Parse(item["qtyproject"].ToString()));
                        data.Add("item_code", (item["itemcode"].ToString()));
                        data.Add("item_description", (item["short_descproject"].ToString()));
                        data.Add("list_price", float.Parse(item["listpriceproject"].ToString()));
                        data.Add("total_price", float.Parse(item["componenttotalproject"].ToString()));
                        data.Add("item_id", int.Parse(item["itemiddgv"].ToString()));
                        data.Add("delivery_preference", item["delivery_preferenceproject"].ToString());
                        data.Add("status", item["statusproject"].ToString());
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
                        data.Add("list_price", float.Parse(item["unitprice"].ToString()));
                        data.Add("total_price", float.Parse(item["linetotal"].ToString()));
                        data.Add("item_id", int.Parse(item["itemid"].ToString()));
                        data.Add("delivery_preference", item["delivery_preference"].ToString());
                        data.Add("status", item["status"].ToString());
                    }
                    orderDetailsList.Add(data);

                }

                // If there are order details to add
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
                            // Perform update if document number exists
                            await OrderService.Update(parentData);
                            MessageBox.Show("Data updated successfully");

                            await FetchData(true);
                            bindOrderByDocNo(docno, true);
                            CheckStatus();
                        }
                        else
                        {
                            // Perform insert if document number does not exist
                            await OrderService.Insert(parentData);
                            MessageBox.Show("Data added successfully");
                            await FetchData(true);
                            CheckStatus();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message + "\n\n" + "Stack Trace: " + ex.StackTrace);
            }
        }

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
                    bindOrder(true);
                    CalculateTotalPrice();
                }
            }
        }
        private void Save_Click(object sender, EventArgs e)
        {
            saving();
        }
        private async void btn_check_Click(object sender, EventArgs e)
        {
            try
            {
                string docIdValue = ((TextBox)pnl_header_2.Controls["txt_doc"]).Text;
                string docnoValue = ((TextBox)pnl_header_2.Controls["txt_document_no"]).Text;

                docIdValue = docIdValue.StartsWith("SO#") ? docIdValue.Substring(3) : docIdValue;
                docnoValue = docnoValue.StartsWith("Q#") ? docnoValue.Substring(2) : docnoValue;

                if (int.TryParse(docIdValue, out int selectedDoc) && selectedDoc > 0)
                {
                    var selectedOrder = OrderList.Select($"doc = {selectedDoc}").FirstOrDefault();

                    if (selectedOrder != null)
                    {
                        selectedOrder["status"] = "ACTIVE";
                        var parentDataHeader = new Dictionary<string, dynamic>
                {
                    { "doc", selectedOrder["doc"] },
                    { "status", "ACTIVE" }
                };

                        await OrderService.Update(parentDataHeader);
                        MessageBox.Show("Order status updated to ACTIVE.");
                        FetchData(false);
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
        private async void btn_cancel_Click(object sender, EventArgs e)
        {
            try
            {
                string docIdValue = ((TextBox)pnl_header_2.Controls["txt_doc"]).Text;
                string docnoValue = ((TextBox)pnl_header_2.Controls["txt_document_no"]).Text;

                docIdValue = docIdValue.StartsWith("SO#") ? docIdValue.Substring(3) : docIdValue;
                docnoValue = docnoValue.StartsWith("Q#") ? docnoValue.Substring(2) : docnoValue;

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
                        FetchData(false);
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
        private void CheckStatus()
        {
            bool isStatusActive = txt_status.Text == "ACTIVE";
            bool isStatusCancelled = txt_status.Text == "CANCELLED";

            // Handle button and field enabling/disabling based on status
            btn_check.Enabled = !string.IsNullOrEmpty(txt_status.Text) && !isStatusActive;
            btn_cancel.Enabled = !string.IsNullOrEmpty(txt_status.Text) && !isStatusCancelled;

            // Ref PO field
            txt_ref_po.ReadOnly = isStatusActive || isStatusCancelled;

            // DatePickers and other fields based on status
            dtp_date.Enabled = !isStatusCancelled;
            dtp_delivery_date.Enabled = !isStatusCancelled;
            txt_sales_executive.ReadOnly = isStatusCancelled;
            txt_receiver.ReadOnly = isStatusCancelled;
            txt_contact_no.ReadOnly = isStatusCancelled;
            txt_remarks.ReadOnly = isStatusCancelled;
            txt_approved_by.ReadOnly = isStatusCancelled;
            btn_save.Enabled = !isStatusCancelled;

            // Handle DataGridView column read-only based on status
            foreach (DataGridViewColumn column in dgv_order_sales.Columns)
            {
                column.ReadOnly = isStatusCancelled;
            }
        }
        private void SOIncrementer()
        {
            txt_doc.Text = "SO#" + (OrderList.Rows.Count + 1).ToString("D4");
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
        private async void btn_next_Click(object sender, EventArgs e)
        {
            int rowCount = OrderList.Rows.Count;
            //int rowCount = transactionList.Rows.Count;
            if (SelectedRow < rowCount - 1)
            {
                SelectedRow++;
                Helpers.ResetControls(pnl_header);
                Helpers.ResetControls(pnl_footer);
                await FetchData(false);
            }
        }

        private async void btn_prev_Click_1(object sender, EventArgs e)
        {
            if (SelectedRow >= 1)
            {
                SelectedRow--;
                Helpers.ResetControls(pnl_header);
                Helpers.ResetControls(pnl_footer);
                await FetchData(false);
            }
        }
        private void btn_back_Click(object sender, EventArgs e)
        {
            Quotation quotationPage = new Quotation(documentNo);

            this.Parent.Controls.Add(quotationPage);
            this.Dispose();
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

        private void btn_print_Click(object sender, EventArgs e)
        {
            string documentNo = txt_doc.Text.Trim();
            documentNo = documentNo.Replace("SO#", "").Trim();
            SalesPrintModal printPage = new SalesPrintModal(false, documentNo);
            int screenHeight = Screen.PrimaryScreen.Bounds.Height;
            printPage.Height = (int)(screenHeight);
            // Set the parent form to be non-interactive while the modal is active
            printPage.StartPosition = FormStartPosition.CenterParent; // Optional: centers the modal
            printPage.ShowDialog();
        }
        private string targetDirectory = @"C:\Users\SMPC\source\repos\smpc_sales_system\smpc_sales_system2\smpc_sales_system\Data\TempFiles";
        private void LoadDirectory(string directoryPath)
        {
            treeView1.Nodes.Clear();
            listView1.Items.Clear();

            TreeNode rootNode = new TreeNode(directoryPath);
            treeView1.Nodes.Add(rootNode);

            LoadDirectories(directoryPath, rootNode);
        }
        private void LoadDirectories(string path, TreeNode node)
        {
            try
            {
                // Get all subdirectories in the current directory
                string[] directories = Directory.GetDirectories(path);

                // Iterate through each directory and add them to the TreeView
                foreach (string directory in directories)
                {
                    TreeNode directoryNode = new TreeNode(Path.GetFileName(directory)); // Display the folder name
                    node.Nodes.Add(directoryNode); // Add the node to the current node
                    LoadDirectories(directory, directoryNode);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading directories: {ex.Message}");
            }
        }
        private string GetFullPathFromTreeNode(TreeNode node)
        {
            string path = node.Text;
            while (node.Parent != null)
            {
                path = Path.Combine(node.Parent.Text, path);
                node = node.Parent;
            }
            return path;
        }

        private void LoadFiles(string path)
        {
            listView1.Items.Clear();

            try
            {
                string[] files = Directory.GetFiles(path);

                foreach (string file in files)
                {
                    ListViewItem item = new ListViewItem(Path.GetFileName(file));
                    item.SubItems.Add(new FileInfo(file).Length.ToString()); 
                    item.SubItems.Add(File.GetLastWriteTime(file).ToString());

                    item.ImageKey = "default";

                    listView1.Items.Add(item);
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
        private void btn_save_Click_1(object sender, EventArgs e)
        {
            saving();
        }

        private void treeView1_AfterSelect(object sender, TreeViewEventArgs e)
        {
            // Get the full path of the selected node
            string selectedPath = GetFullPathFromTreeNode(e.Node);
            //txtFolderPath.Text = selectedPath; // Update the TextBox with the selected folder path
            LoadFiles(selectedPath); // Load files in the selected folder
        }

        private void listView1_DoubleClick_1(object sender, EventArgs e)
        {
            if (listView1.SelectedItems.Count > 0)
            {
                string folderPath = GetFullPathFromTreeNode(treeView1.SelectedNode);
                string filePath = Path.Combine(folderPath, listView1.SelectedItems[0].Text);

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
    }
}
