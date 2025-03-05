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

namespace smpc_sales_app.Pages.Sales
{
    public partial class Orders : UserControl
    {
        int SelectedRow = 0;
        private string documentNo;
        public Orders(string documentNo = null)
        {
            InitializeComponent();
            fetchBpiData();
            fetchItemData();
            fetchQuotationDetails();
            FetchData(true);
            Helpers.ResetControls(pnl_header);
            Helpers.ResetControls(pnl_footer);
            this.documentNo = documentNo;
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
            }
            else
            {
                MessageBox.Show("There's no sales order now.");
            }
            if (!isReload && data != null)
            {
                bindOrder(true);
                CalculateTotalPrice();
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
                //bindQuotation(true);
                CalculateTotalPrice();
                SOIncrementer();
            }
        }
        private async void fetchProject()
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
                Panel[] pnlList = { pnl_header, pnl_footer };

                DataTable HeaderList = this.OrderList.Clone();
                HeaderList.Columns.Add("branch_name", typeof(string));
                HeaderList.Columns.Add("customer_code", typeof(string));
                HeaderList.Columns.Add("bill_to", typeof(string));
                HeaderList.Columns.Add("ship_to", typeof(string));
                HeaderList.Columns.Add("tin", typeof(string));
                HeaderList.Columns.Add("vat_amount", typeof(string));    
                HeaderList.Columns.Add("gross_sales", typeof(string));
                HeaderList.Columns.Add("total_amount_due", typeof(string));
                foreach (DataRow parentRow in this.OrderList.Rows)
                {
                    DataRow newRow = HeaderList.NewRow();
                    foreach (DataColumn col in this.OrderList.Columns)
                    {
                        newRow[col.ColumnName] = parentRow[col.ColumnName];
                    }

                    int quotationID = Convert.ToInt32(parentRow["quotation_id"]);
                    DataRow[] quotation = transactionList.Select($"id = '{quotationID}'");
                    if (quotation.Length > 0)
                    {
                        // Check for project_name field in the transactionList
                        if (quotation[0]["project_name"] != DBNull.Value && quotation[0]["project_name"].ToString() == "1")
                        {
                            MessageBox.Show("PROJECT TO");
                        }
                    }
                    IsProject(false);
                    string customerID = quotation[0]["customer_id"].ToString();
                    newRow["vat_amount"] = quotation[0]["vat_amount"].ToString();
                    newRow["gross_sales"] = quotation[0]["gross_sales"].ToString();
                    newRow["total_amount_due"] = quotation[0]["total_amount_due"].ToString();
                    //int ID = (int)parentRow["customer_id"];
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
                }


                Helpers.BindControls(pnlList, HeaderList, SelectedRow);
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

                dtp_date.Value = Convert.ToDateTime(OrderList.Rows[SelectedRow]["date"]);
                dtp_delivery_date.Value = Convert.ToDateTime(OrderList.Rows[SelectedRow]["delivery_date"]);
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
                CheckStatus();
            }
        }
        private void bindOrderByDocNo(string documentNo, bool isBind = false)
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
                HeaderList.Columns.Add("vat_amount", typeof(string));
                HeaderList.Columns.Add("gross_sales", typeof(string));
                HeaderList.Columns.Add("total_amount_due", typeof(string));

                // Filter the rows based on the passed documentNo
                DataRow[] filteredRows = this.OrderList.Select($"document_no = '{documentNo}'");

                if (filteredRows.Length > 0)
                {
                    DataRow parentRow = filteredRows[0];  // Get the first matching row

                    DataRow newRow = HeaderList.NewRow();
                    foreach (DataColumn col in this.OrderList.Columns)
                    {
                        newRow[col.ColumnName] = parentRow[col.ColumnName];
                    }

                    int quotationID = Convert.ToInt32(parentRow["quotation_id"]);
                    DataRow[] quotation = transactionList.Select($"id = '{quotationID}'");
                    if (quotation.Length > 0)
                    {
                        // Check for project_name field in the transactionList
                        if (quotation[0]["project_name"] != DBNull.Value && quotation[0]["project_name"].ToString() == "1")
                        {
                            MessageBox.Show("PROJECT TO");
                        }
                    }
                    IsProject(false);
                    MessageBox.Show("QUOTE TO");
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
                        DataRow newRow2 = withItemListTwo.NewRow();
                        foreach (DataColumn col in DetailsList.Columns)
                        {
                            newRow2[col.ColumnName] = childRow[col.ColumnName];
                        }

                        // Look up item name from ItemList
                        string itemId = childRow["item_id"].ToString();
                        string quotationId = childRow["quotation_quick_id"].ToString();
                        DataRow[] itemRows = ItemList.Select($"id = '{itemId}'");
                        DataRow[] quickquote = childList.Select($"id = '{quotationId}'");
                        newRow2["qty"] = quickquote[0]["qty"].ToString();
                        newRow2["unit_price"] = quickquote[0]["unit_price"].ToString();
                        newRow2["line_total"] = quickquote[0]["line_total"].ToString();

                        if (itemRows.Length > 0)
                        {
                            newRow2["short_desc"] = itemRows[0]["item_model"].ToString() + " - " + itemRows[0]["short_desc"].ToString();
                            newRow2["item_code"] = itemRows[0]["item_code"].ToString();
                        }
                        else
                        {
                            newRow2["short_desc"] = "Unknown Item";
                            newRow2["item_code"] = "N/A";
                        }
                        withItemListTwo.Rows.Add(newRow2);
                    }

                    // Create filtered view
                    DataView dataview = new DataView(withItemListTwo);
                    dataview.RowFilter = "based_id = '" + parentRow["order_id"].ToString() + "'";

                    dgv_order_sales.DataSource = dataview;
                    CheckStatus();
                }
            }
        }
        private void bindQuotation(string documentNo, bool isBind = false)
        {
            if (isBind)
            {
                Panel[] pnlList = { pnl_header, pnl_footer };

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
                    DataRow firstRow = filteredRows[0];
                    //if (firstRow["project_name"] != DBNull.Value && firstRow["project_name"].ToString() == "1")
                    //{
                    //    IsProject(true);
                    //    MessageBox.Show("PROJECT TO");
                    //    bindProject(documentNo, true);
                    //}
                    //else
                    //{
                        IsProject(false);
                        MessageBox.Show("QUOTE TO");
                        cmb_payment_terms.SelectedValue = filteredRows[0]["payment_terms_id"].ToString();
                        cmb_payment_terms.SelectedItem = filteredRows[0]["payment_terms_id"].ToString();

                        cmb_ship_type.SelectedValue = filteredRows[0]["ship_type_id"].ToString();
                        cmb_ship_type.SelectedItem = filteredRows[0]["ship_type_id"].ToString();
                        Helpers.BindControls(pnlList, HeaderList, SelectedRow);

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

                        // Create filtered view based on document_no
                        DataView dataview = new DataView(withItemList);
                        dataview.RowFilter = "based_id = '" + Convert.ToInt32(filteredRows[0]["id"]) + "'";

                        dgv_order_sales.DataSource = dataview;
                    }
                //}

                
            }
        }
        private void bindProject(string documentNo, bool isBind = false)
        {
            if (isBind)
            {
                Panel[] pnlList = { pnl_header, pnl_footer };

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

                Helpers.BindControls(pnlList, HeaderList, SelectedRow);

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

                // Check if any IDs were extracted
                if (ids.Count > 0)
                {
                    // Now, filter dataview2 based on the extracted IDs
                    string idFilter = string.Join("','", ids); // Join IDs with commas for SQL-like filtering
                    DataView dataview2 = new DataView(ProjectItemList);
                    dataview2.RowFilter = $"based_id IN ('{idFilter}')"; // Use IN to filter by multiple IDs

                    // Set the DataSource for dgv_order_sales
                    dgv_project.DataSource = dataview2;
                }
                else
                {
                    // If no IDs were found, you can handle this case accordingly
                    MessageBox.Show("No matching IDs found.");
                }
            }
        }

        private async void Orders_Load(object sender, EventArgs e)
            {
                bs_payment_terms.DataSource = CacheData.PaymentTerms;
                bs_ship_type.DataSource = CacheData.ShipTypeSetup;
                await FetchData(false);
                fetchProject();

                if (!string.IsNullOrEmpty(documentNo))
                {
                    // Check if the documentNo exists in the OrderList DataTable
                    DataRow[] matchingRows = OrderList.Select($"document_no = '{documentNo}'");

                    if (matchingRows.Length > 0)
                    {
                        // Document number exists in OrderList, bind the order data
                        Helpers.ResetControls(pnl_header);
                        Helpers.ResetControls(pnl_footer);
                        btn_search.Visible = false;
                        btn_back.Visible = true;
                        btn_prev.Visible = false;
                        btn_next.Visible = false;
                        bindOrderByDocNo(documentNo, true);
                        CalculateTotalPrice();
                    }
                    else
                    {
                        Helpers.ResetControls(pnl_header);
                        Helpers.ResetControls(pnl_footer);
                        btn_search.Visible = false;
                        btn_back.Visible = true;
                        btn_prev.Visible = false;
                        btn_next.Visible = false;
                        bindQuotation(documentNo, true);
                        CalculateTotalPrice();
                        SOIncrementer();
                    }
                if (string.IsNullOrEmpty(txt_document_no.Text))
                {
                    Helpers.ResetControls(pnl_header);
                    Helpers.ResetControls(pnl_footer);
                    btn_search.Visible = true;
                    btn_back.Visible = false;
                    btn_prev.Visible = true;
                    btn_next.Visible = true;
                    FetchData(false);
                    CalculateTotalPrice();
                }
                CheckStatus();
            }
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

        private void btn_save_Click(object sender, EventArgs e)
        {
            saving();
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

                var quoteIdValue = ((TextBox)pnl_header.Controls["txt_quotation_id"]).Text;
                if (string.IsNullOrEmpty(txt_id.Text))
                {
                    txt_id.Text = quoteIdValue;
                }
                var txtIdValue = ((TextBox)pnl_header.Controls["txt_id"]).Text;
                
                var docno = ((TextBox)pnl_header.Controls["txt_document_no"]).Text;
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

                if (parentDataHeader.ContainsKey("doc") && parentDataHeader["doc"] is string documentNo)
                {
                    parentDataHeader["doc"] = documentNo.StartsWith("SO#")
                        ? documentNo.Substring(3)
                        : documentNo;
                }
                if (parentDataHeader.ContainsKey("document_no") && parentDataHeader["document_no"] is string document_no)
                {
                    parentDataHeader["document_no"] = document_no.StartsWith("Q#")
                        ? document_no.Substring(2)
                        : document_no;
                }

                // List of columns to be converted to int
                var columnsToConvert = new List<string> { "ship_to_id", "bill_to_id", "customer_id", "quotation_id", "ref_po" };

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
                    if (!parentData.ContainsKey(kvp.Key))
                    {
                        parentData.Add(kvp.Key, kvp.Value);
                    }
                    else
                    {
                        parentData[kvp.Key] = kvp.Value;
                    }
                }

                var dataSource = Helpers.ConvertDataGridViewToDataTable(dgv_order_sales);

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

                    data.Add("based_id", int.Parse(item["basedid"].ToString()));

                    if (!isExistingDoc) // If it's an insert
                    {
                        data.Add("quotation_quick_id", int.Parse(item["quick_quote_id"].ToString()));
                    }  

                    data.Add("qty", int.Parse(item["qtydgv"].ToString()));
                    data.Add("item_code", (item["itemcodedgv"].ToString()));
                    data.Add("item_description", (item["shortdescdgv"].ToString()));
                    data.Add("list_price", float.Parse(item["unitpricedgv"].ToString()));
                    data.Add("total_price", float.Parse(item["linetotaldgv"].ToString()));
                    data.Add("item_id", int.Parse(item["itemid"].ToString()));
                    data.Add("delivery_preference", item["delivery_preference"].ToString());
                    data.Add("status", item["status"].ToString());

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
                            FetchData(true);
                            bindOrderByDocNo(docno, true);
                            CheckStatus();
                        }
                        else
                        {
                            // Perform insert if document number does not exist
                            await OrderService.Insert(parentData);
                            MessageBox.Show("Data added successfully");
                            FetchData(true);
                            bindOrderByDocNo(docno, true);
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
                string docIdValue = ((TextBox)pnl_header.Controls["txt_doc"]).Text;
                string docnoValue = ((TextBox)pnl_header.Controls["txt_document_no"]).Text;

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
                string docIdValue = ((TextBox)pnl_header.Controls["txt_doc"]).Text;
                string docnoValue = ((TextBox)pnl_header.Controls["txt_document_no"]).Text;

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
            if (!dgv_order_sales.Columns.Contains("linetotaldgv"))
            {
                MessageBox.Show("The 'line_total' column is missing in the DataGridView.");
                return;
            }

            decimal total = dgv_order_sales.Rows.Cast<DataGridViewRow>()
                                .Where(row => row.Cells["linetotaldgv"].Value != null && decimal.TryParse(row.Cells["linetotaldgv"].Value.ToString(), out _))
                                .Sum(row => Convert.ToDecimal(row.Cells["linetotaldgv"].Value));

            txt_total.Text = total.ToString("#,0.00");
        }
        private async void btn_next_Click(object sender, EventArgs e)
        {
            int rowCount = OrderList.Rows.Count;
            //int rowCount = transactionList.Rows.Count;
            if (SelectedRow < rowCount - 1)
            {
                SelectedRow++;
                FetchData(false);
                Helpers.ResetControls(pnl_header);
                Helpers.ResetControls(pnl_footer);
            }
        }

        private async void btn_prev_Click_1(object sender, EventArgs e)
        {
            if (SelectedRow >= 1)
            {
                SelectedRow--;
                FetchData(false);
                Helpers.ResetControls(pnl_header);
                Helpers.ResetControls(pnl_footer);
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
            }
            else
            {
                dgv_order_sales.Visible = true;
                dgv_project.Visible = false;
            }
        }
    }
}
