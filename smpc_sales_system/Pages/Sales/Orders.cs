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

namespace smpc_sales_app.Pages.Sales
{
    public partial class Orders : UserControl
    {
        int SelectedRow = 0;
        public Orders()
        {
            fetchBpiData();
            fetchItemData();
            InitializeComponent();
            Helpers.ResetControls(pnl_header);
            Helpers.ResetControls(pnl_footer);
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

        private async void FetchData()
        {
            OrderList data = await OrderService.GetOrders();

            OrderList = JsonHelper.ToDataTable(data.order);
            DetailsList = JsonHelper.ToDataTable(data.sales_order_details);

            // Add a default row to DetailsList
            //if (DetailsList != null)
            //{
            //    DataRow defaultRow = DetailsList.NewRow();
            //    defaultRow["based_id"] = OrderList.Rows[SelectedRow]["order_id"];
            //    defaultRow["qty"] = "ADD NEW ITEM";
            //    defaultRow["has_stocks"] = DBNull.Value;
            //    DetailsList.Rows.Add(defaultRow);
            //}

            if (data != null)
            {
                //bind(true);
            }
            else
            {

            }
        }
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
        private async void fetchQuotationDetails()
        {
            SalesQuotationList data = await QuotationService.GetQuotations();

            transactionList = JsonHelper.ToDataTable(data.SalesQuotation);
            childList = JsonHelper.ToDataTable(data.SalesQuotationQuick);

            if (data != null)
            {
                bindQuotation(true);
                CalculateTotalPrice();
                SOIncrementer();
            }
        }
        private void bindOrder(bool isBind = false)
        {
            if (isBind)
            {
                Panel[] pnlList = { pnl_header, pnl_footer };

                DataTable HeaderList = this.OrderList.Clone();
                HeaderList.Columns.Add("branch_name", typeof(string));
                HeaderList.Columns.Add("customer_code", typeof(string));
                HeaderList.Columns.Add("bill_to", typeof(string));
                HeaderList.Columns.Add("ship_to", typeof(string));
                HeaderList.Columns.Add("tin", typeof(string));
          

                foreach (DataRow parentRow in this.OrderList.Rows)
                {
                    DataRow newRow = HeaderList.NewRow();
                    foreach (DataColumn col in this.OrderList.Columns)
                    {
                        newRow[col.ColumnName] = parentRow[col.ColumnName];
                    }

                    int quotationID = Convert.ToInt32(parentRow["quotation_id"]);
                    DataRow[] quotation = transactionList.Select($"id = '{quotationID}'");
                    string customerID = quotation[0]["customer_id"].ToString();

                    //int ID = (int)parentRow["customer_id"];
                    string ShipID = parentRow["ship_to_id"].ToString();
                    string BillID = parentRow["bill_to_id"].ToString();
                    DataRow[] bpiGenRows = bpi_general.Select($"based_id = '{customerID}'");
                    DataRow[] billRows = bpi_address.Select($"address_id = '{BillID}'");
                    DataRow[] shipRows = bpi_address.Select($"address_id = '{ShipID}'");
                    if (bpiGenRows.Length > 0)
                    {
                        newRow["branch_name"] = bpiGenRows[0]["branch_name"].ToString();
                        newRow["customer_code"] = bpiGenRows[0]["customer_code"].ToString();
                        string BasedID = bpiGenRows[0]["based_id"].ToString();
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


                Helpers.BindControls(pnlList, HeaderList, SelectedRow);
                

                if (string.IsNullOrEmpty(txt_status.Text))
                {
                    txt_status.Text = "-";
                }

                cmb_payment_terms.SelectedValue = this.OrderList.Rows[this.SelectedRow]["payment_terms_id"].ToString();
                cmb_payment_terms.SelectedItem = this.OrderList.Rows[this.SelectedRow]["payment_terms_id"].ToString();

                cmb_ship_type.SelectedValue = this.OrderList.Rows[this.SelectedRow]["ship_type_id"].ToString();
                cmb_ship_type.SelectedItem = this.OrderList.Rows[this.SelectedRow]["ship_type_id"].ToString();

                // Clone childList and add item_name column
                DataTable withItemListTwo = this.DetailsList.Clone();
                withItemListTwo.Columns.Add("short_desc", typeof(string));
                withItemListTwo.Columns.Add("item_code", typeof(string));
                withItemListTwo.Columns.Add("item_model", typeof(string));
                withItemListTwo.Columns.Add("qty", typeof(string));
                withItemListTwo.Columns.Add("unit_price", typeof(string));
                withItemListTwo.Columns.Add("line_total", typeof(string));

                // Iterate through childList (not ItemList)
                foreach (DataRow childRow in this.DetailsList.Rows)
                {
                    DataRow newRow = withItemListTwo.NewRow();
                    foreach (DataColumn col in DetailsList.Columns)
                    {
                        newRow[col.ColumnName] = childRow[col.ColumnName];
                    }

                    // Look up item name from ItemList
                    string itemId = childRow["item_id"].ToString();
                    string quotationId = childRow["quotation_quick_id"].ToString();
                    DataRow[] itemRows = ItemList.Select($"id = '{itemId}'");
                    DataRow[] quickquote = childList.Select($"id = '{quotationId}'");
                    newRow["qty"] = quickquote[0]["qty"].ToString();
                    newRow["unit_price"] = quickquote[0]["unit_price"].ToString();
                    newRow["line_total"] = quickquote[0]["line_total"].ToString();

                    if (itemRows.Length > 0)
                    {
                        newRow["short_desc"] = itemRows[0]["item_model"].ToString() + " - " + itemRows[0]["short_desc"].ToString();
                        newRow["item_code"] = itemRows[0]["item_code"].ToString();
                    }
                    else
                    {
                        newRow["short_desc"] = "Unknown Item";
                        newRow["item_code"] = "N/A";
                    }
                    withItemListTwo.Rows.Add(newRow);
                }

                // Create filtered view
                DataView dataview = new DataView(withItemListTwo);
                dataview.RowFilter = "based_id = '" + this.OrderList.Rows[this.SelectedRow]["order_id"].ToString() + "'";

                dgv_order_sales.DataSource = dataview;
                //foreach (DataGridViewRow row in dgv_order_sales.Rows)
                //{
                //    // Check each cell for null/DBNull and replace with "N/A"
                //    foreach (DataGridViewCell cell in row.Cells)
                //    {
                //        if (cell.Value == DBNull.Value || cell.Value == null)
                //        {
                //            cell.Value = "N/A"; // Replace null/DBNull with "N/A"
                //        }
                //    }
                //}
                //dgv_quick_quote_details.DataSource = dataview;
            }
        }
        private void bindQuotation(bool isBind = false)
        {
            if (isBind)
            {
                Panel[] pnlList = { pnl_header, pnl_footer };

                DataTable HeaderList = this.transactionList.Clone();    
                HeaderList.Columns.Add("branch_name", typeof(string));
                //HeaderList.Columns.Add("tin", typeof(string));
                HeaderList.Columns.Add("customer_code", typeof(string));
                HeaderList.Columns.Add("bill_to", typeof(string));
                HeaderList.Columns.Add("ship_to", typeof(string));
                HeaderList.Columns.Add("tin", typeof(string));

                foreach (DataRow parentRow in this.transactionList.Rows)
                {
                    DataRow newRow = HeaderList.NewRow();
                    foreach (DataColumn col in this.transactionList.Columns)
                    {
                        newRow[col.ColumnName] = parentRow[col.ColumnName];
                    }

                    int ID = (int)parentRow["customer_id"];
                    string ShipID = parentRow["ship_to_id"].ToString();
                    string BillID = parentRow["bill_to_id"].ToString();
                    DataRow[] bpiGenRows = bpi_general.Select($"based_id = '{ID}'");
                    DataRow[] billRows = bpi_address.Select($"address_id = '{BillID}'");
                    DataRow[] shipRows = bpi_address.Select($"address_id = '{ShipID}'");
                    if (bpiGenRows.Length > 0)
                    {
                        newRow["branch_name"] = bpiGenRows[0]["branch_name"].ToString();
                        newRow["customer_code"] = bpiGenRows[0]["customer_code"].ToString();
                        string BasedID = bpiGenRows[0]["based_id"].ToString();
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


                Helpers.BindControls(pnlList, HeaderList, SelectedRow);
                if (string.IsNullOrEmpty(txt_status.Text))
                {
                    txt_status.Text = "-";
                }

                cmb_payment_terms.SelectedValue = this.transactionList.Rows[this.SelectedRow]["payment_terms_id"].ToString();
                cmb_payment_terms.SelectedItem = this.transactionList.Rows[this.SelectedRow]["payment_terms_id"].ToString();

                cmb_ship_type.SelectedValue = this.transactionList.Rows[this.SelectedRow]["ship_type_id"].ToString();
                cmb_ship_type.SelectedItem = this.transactionList.Rows[this.SelectedRow]["ship_type_id"].ToString();

                // Clone childList and add item_name column
                DataTable withItemList = this.childList.Clone();
                withItemList.Columns.Add("short_desc", typeof(string));
                withItemList.Columns.Add("item_code", typeof(string));
                withItemList.Columns.Add("item_model", typeof(string));

                // Iterate through childList (not ItemList)
                foreach (DataRow childRow in this.childList.Rows)
                {
                    DataRow newRow = withItemList.NewRow();
                    foreach (DataColumn col in childList.Columns)
                    {
                        newRow[col.ColumnName] = childRow[col.ColumnName];
                    }

                    // Look up item name from ItemList
                    string itemId = childRow["item_id"].ToString();
                    DataRow[] itemRows = ItemList.Select($"id = '{itemId}'");
                    if (itemRows.Length > 0)
                    {
                        newRow["short_desc"] = itemRows[0]["item_model"].ToString() + " - " + itemRows[0]["short_desc"].ToString();
                        newRow["item_code"] = itemRows[0]["item_code"].ToString();
                    }
                    else
                    {
                        newRow["short_desc"] = "Unknown Item";
                        newRow["item_code"] = "N/A";
                    }
                    withItemList.Rows.Add(newRow);
                }

                // Create filtered view
                DataView dataview = new DataView(withItemList);
                dataview.RowFilter = "based_id = '" + this.transactionList.Rows[this.SelectedRow]["id"].ToString() + "'";

                dgv_order_sales.DataSource = dataview;
                //foreach (DataGridViewRow row in dgv_order_sales.Rows)
                //{
                //    // Check each cell for null/DBNull and replace with "N/A"
                //    foreach (DataGridViewCell cell in row.Cells)
                //    {
                //        if (cell.Value == DBNull.Value || cell.Value == null)
                //        {
                //            cell.Value = "N/A"; // Replace null/DBNull with "N/A"
                //        }
                //    }
                //}
                //dgv_quick_quote_details.DataSource = dataview;
            }
        }

        private void dgv_order_sales_CellClick(object sender, DataGridViewCellEventArgs e)
        {
           
        }
        private void bind(bool isBind = false)
        {
            if (isBind)
            {
                //dgv_quick_quote_details.DataSource = dataView;
                // dgv_quick_quote_details.DataSource = childList;
                Panel[] pnlList = { pnl_header, pnl_footer };
                Helpers.BindControls(pnlList, OrderList, SelectedRow);

                DataView dataview = new DataView(this.DetailsList);
                dataview.RowFilter = "based_id = '" + this.OrderList.Rows[this.SelectedRow]["order_id"].ToString() + "'";
                dgv_order_sales.DataSource = dataview;

                foreach (DataGridViewRow row in dgv_order_sales.Rows)
                {
                    var hasStocksValue = row.Cells["has_stocks"].Value;

                    if (hasStocksValue == DBNull.Value || hasStocksValue == null)
                    {
                        foreach (DataGridViewCell cell in row.Cells)
                        {
                            if (cell.OwningColumn.Name != "qty")
                            {
                                cell.Style.BackColor = Color.LightGray;
                            }
                        }
                    }
                    else
                    {
                        bool hasStocks = Convert.ToBoolean(hasStocksValue);  
                        if (!hasStocks)
                        {
                            row.Cells["has_stocks"].Style.BackColor = Color.Red;
                        }
                        else
                        {
                            row.Cells["has_stocks"].Style.BackColor = Color.White;
                        }
                    }
                }
            }
        }

        private void CalculateTotalPrice()
        {
            decimal total = 0.0m;

            // Ensure the column "line_total" exists
            if (dgv_order_sales.Columns.Contains("linetotal"))
            {
                foreach (DataGridViewRow row in dgv_order_sales.Rows)
                {
                    if (row.Cells["linetotal"].Value != null)
                    {
                        decimal totalPrice;
                        if (decimal.TryParse(row.Cells["linetotal"].Value.ToString(), out totalPrice))
                        {
                            total += totalPrice;
                        }
                    }
                }
            }
            else
            {
                MessageBox.Show("The 'line_total' column is missing in the DataGridView.");
            }

            txt_total.Text = total.ToString("0.00");
        }
        //private void CalculateTotalPrice()
        //{
        //    decimal total = 0.0m;

        //    // Ensure the column "line_total" exists
        //    if (dgv_order_sales.Columns.Contains("line_total"))
        //    {
        //        foreach (DataGridViewRow row in dgv_order_sales.Rows)
        //        {
        //            if (row.Cells["line_total"].Value != null)
        //            {
        //                decimal totalPrice;
        //                if (decimal.TryParse(row.Cells["line_total"].Value.ToString(), out totalPrice))
        //                {
        //                    total += totalPrice;
        //                }
        //            }
        //        }
        //    }
        //    else
        //    {
        //        MessageBox.Show("The 'line_total' column is missing in the DataGridView.");
        //    }

        //    txt_total.Text = total.ToString("0.00");
        //}
        
        private void Orders_Load(object sender, EventArgs e)
        {
            //FetchData();

            pnl_footer.Width = this.ClientSize.Width;
            fetchItemData();
            fetchBpiData();
            fetchQuotationDetails();
            FetchData();
            bs_payment_terms.DataSource = CacheData.PaymentTerms;
            bs_ship_type.DataSource = CacheData.ShipTypeSetup;
            // Helpers.LoadDirectory("D:\\LIFESAVER\\LIFESAVER\\TEST", treeview_sales);
        }

        private void treeview_sales_NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            if (e.Node.Tag != null)
            {
                // Check if the clicked node is a file (has a Tag property)
                string filePath = e.Node.Tag.ToString();

                if (File.Exists(filePath))
                {
                    try
                    {
                        // Open the file using the default associated application
                        Process.Start(filePath);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error opening file: {ex.Message}");
                    }
                }
            }
        }

        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            //// Ensure we are not clicking on header row or invalid rows
            //if (e.RowIndex >= 0)
            //{
            //    DataGridViewRow clickedRow = dgv_order_sales.Rows[e.RowIndex];

            //    // Check if the clicked row is the default row (last row)
            //    if (clickedRow.Index == dgv_order_sales.Rows.Count - 1)
            //    {
            //        // Show the modal dialog for the default row
            //        ItemModal itemModal = new ItemModal();
            //        DialogResult r = itemModal.ShowDialog();

            //        if (r == DialogResult.OK)
            //        {
            //            Dictionary<string, string> result = itemModal.GetResult();

            //            if (result != null)
            //            {
            //                string code = "";
            //                string name = "";
            //                string unit_price = "";
            //                string short_desc = "N/A";

            //                result.TryGetValue("name", out name);
            //                result.TryGetValue("code", out code);
            //                result.TryGetValue("unitprice", out unit_price);
            //                result.TryGetValue("short_desc", out short_desc);

            //                DataRow newRow = DetailsList.NewRow();
            //                newRow["based_id"] = OrderList.Rows[SelectedRow]["order_id"];
            //                newRow["item_code"] = code;
            //                newRow["total_price"] = unit_price;
            //                newRow["item_description"] = short_desc;

            //                //newRow["qty"] = 1;
            //                //newRow["unit_measure"] = "COD";
                            
            //                DetailsList.Rows.InsertAt(newRow, DetailsList.Rows.Count - 1);
            //                CalculateTotalPrice();
            //            }
            //        }
            //    }
            //}
        }
          
        private void panel3_Paint(object sender, PaintEventArgs e)
        {

        }

        private void btn_next_Click(object sender, EventArgs e)
        {
            //int rowCount = OrderList.Rows.Count;
            int rowCount = transactionList.Rows.Count;
            if (SelectedRow < rowCount - 1)
            {
                SelectedRow++;
                //FetchData();
                Helpers.ResetControls(pnl_header);
                Helpers.ResetControls(pnl_footer);
                fetchQuotationDetails();
                SOIncrementer();
                
            }
        }

        private void btn_prev_Click_1(object sender, EventArgs e)
        {
            if (SelectedRow >= 1)
            {
                SelectedRow--;
                //FetchData();
                Helpers.ResetControls(pnl_header);
                Helpers.ResetControls(pnl_footer);
                fetchQuotationDetails();
                SOIncrementer();
            }
        }

        private async void btn_save_Click(object sender, EventArgs e)
        {
            try
            {
                // Initialize a list to hold the missing field names
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

                // If there are any missing fields, show an alert with those fields
                if (missingFields.Count > 0)
                {
                    string missingFieldsMessage = "Please fill in the following fields: " + string.Join(", ", missingFields);
                    MessageBox.Show(missingFieldsMessage, "Missing Information", MessageBoxButtons.OK);
                    return; // Stop further execution if validation fails
                }

                var parentDataHeader = Helpers.GetControlsValues(pnl_header);
                var parentDataFooter = Helpers.GetControlsValues(pnl_footer);

                var txtIdValue = ((TextBox)pnl_header.Controls["txt_id"]).Text;
                parentDataHeader["quotation_id"] = txtIdValue;

                if (parentDataHeader.ContainsKey("payment_terms_id") && parentDataHeader["payment_terms_id"] is string shipto)
                {
                    if (int.TryParse(shipto, out int shiptoId))
                    {
                        parentDataHeader["payment_terms_id"] = shiptoId;
                    }
                    else
                    {
                        MessageBox.Show("Invalid ship to ID");
                        return;
                    }
                }
                // trims the Q# from the input
                if (parentDataHeader.ContainsKey("doc") && parentDataHeader["doc"] is string documentNo)
                {
                    parentDataHeader["doc"] = documentNo.StartsWith("SO#")
                        ? documentNo.Substring(3) // Remove "Q#"
                        : documentNo; // Keep as is if "Q#" is not present
                }
                // List of columns to be converted to int
                var columnsToConvert = new List<string> { "ship_to_id", "bill_to_id", "customer_id", "quotation_id", "ref_po", "document_no", "doc" };

                foreach (var column in columnsToConvert)
                {
                    if (parentDataHeader.ContainsKey(column) && parentDataHeader[column] is string columnValue)
                    {
                        if (int.TryParse(columnValue, out int parsedValue))
                        {
                            parentDataHeader[column] = parsedValue;
                        }
                        else
                        {
                            MessageBox.Show($"Invalid {column} value. It must be a valid integer.");
                            return;
                        }
                    }
                }

                // Merge the two dictionaries
                var parentData = new Dictionary<string, dynamic>(parentDataHeader);

                foreach (var kvp in parentDataFooter)
                {
                    // If the key already exists in the parentData, you can decide to overwrite or skip
                    if (!parentData.ContainsKey(kvp.Key))
                    {
                        parentData.Add(kvp.Key, kvp.Value);
                    }
                    else
                    {
                        // Optionally, overwrite the existing value (if desired)
                        parentData[kvp.Key] = kvp.Value;
                    }
                }

                var dataSource = Helpers.ConvertDataGridViewToDataTable(dgv_order_sales);

                List<Dictionary<string, dynamic>> orderDetailsList = new List<Dictionary<string, dynamic>>();

                //Dictionary<string, dynamic> quickQuoteData = new Dictionary<string, dynamic>();

                foreach (DataRow item in dataSource.Rows)
                {
                    Dictionary<string, object> data = new Dictionary<string, object>();

                    data.Add("based_id", int.Parse(item["basedid"].ToString()));
                    data.Add("quotation_quick_id", int.Parse(item["quick_quote_id"].ToString()));
                    data.Add("item_id", int.Parse(item["itemid"].ToString()));
                    data.Add("delivery_preference", item["delivery_preference"].ToString());
                    data.Add("status", item["status"].ToString());
                    //data.Add("has_stocks", bool.Parse(item["has_stocks"].ToString()));

                    // data.Add("SalesQuotationQuick", childData);
                    orderDetailsList.Add(data);
                }


                if (orderDetailsList != null)
                {
                    List<Dictionary<string, dynamic>> childCollection = new List<Dictionary<string, dynamic>>();

                    // loops thru the items
                    foreach (var childData in orderDetailsList)
                    {
                        //parentData["sales_quotation_quick"] = childData;
                        childCollection.Add(childData);
                    }



                    


                    parentData["sales_order_details"] = childCollection;

                    if (parentData.ContainsKey("sales_order_details"))
                    {
                        await OrderService.Insert(parentData);
                        MessageBox.Show("Added data");
                        FetchData();
                        fetchQuotationDetails();
                        // this should await a response in the future if the response is sucess proceed to create if not notify the user
                        //Helpers.ResetControls(pnl_header);
                        //Helpers.ResetControls(pnl_footer);
                        //Helpers.ClearDataGridView(dgv_quick_quote_details);
                        //dgv_quick_quotes_show.Visible = true;
                        //dgv_quick_quotes_show.Enabled = false;
                        //toolstrip_quotation.Enabled = true;


                        // edit
                        //dgv_quick_quote_details.Visible = false;

                    }


                }


            }
            catch (Exception ex)
            {
                MessageBox.Show("err: " + ex);
            }
        }

        private void SOIncrementer()
        {
            // Get the number of rows in the OrderList
            int rowCount = OrderList.Rows.Count;
            string docNum = (rowCount + 1).ToString().PadLeft(4, '0'); // Ensure 4 digits (e.g., 0001, 0002, etc.)

            // Set the document number to the TextBox, prefix it with "SO#"
            txt_doc.Text = "SO#" + docNum; 
        }

        private void button2_Click(object sender, EventArgs e)
        {
            fetchQuotationDetails();
        }

        private void txt_doc_TextChanged(object sender, EventArgs e)
        {

        }

        private void treeview_sales_AfterSelect(object sender, TreeViewEventArgs e)
        {

        }

        private void txt_sales_executive_TextChanged(object sender, EventArgs e)
        {

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
                }
            }
        }
    }
}
