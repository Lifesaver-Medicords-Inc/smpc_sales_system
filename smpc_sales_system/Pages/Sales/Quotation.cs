using smpc_app.Data;
using smpc_app.Services.Helpers;
using smpc_inventory_app.Pages;
using smpc_sales_app.Data;
using smpc_sales_app.Services.Helpers;
using smpc_sales_app.Services.Sales;
using smpc_sales_app.Utils;
using smpc_sales_system.Models;
using smpc_sales_system.Pages;
using smpc_sales_system.Services.Sales.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace smpc_sales_app.Pages.Sales
{
    public partial class Quotation : UserControl
    {
        private ItemService itemService = new ItemService();

        private int SelectedRow = 0;
        private string documentNo;

        public Quotation(string documentNo = null)
        {
            InitializeComponent();

            cmb_warranty.Text = "1 year";
            // CALL THE DEFAULT VALUES OF DATAGRIDVIEW
            //this.QuickQuotesDgvDefaultValues();
            this.documentNo = documentNo;
        }

        private void panel3_Paint(object sender, PaintEventArgs e)
        {
        }

        private void textBox34_TextChanged(object sender, EventArgs e)
        {
        }

        private void tabControl_SelectedIndexChanged(object sender, EventArgs e)
        {
            string activeTab = tabControl.SelectedTab.Text;
            if (activeTab == "quick_tab")
            {
                quick_tab.Height = 307;
            }
        }

        private void btn_new_Click(object sender, EventArgs e)
        {
        }

        private void btn_quick_quote_Click(object sender, EventArgs e)
        {
            //1028, 2354
            this.btn_quick_quote.BackColor = Color.FromArgb(255, 128, 128);
            this.btn_project.BackColor = Color.White;

            this.tabControl.SelectedIndex = 0;
            this.tabControl.Height = 600;
            this.Size = new Size(1386 - 80, 800);
        }

        private void btn_project_Click(object sender, EventArgs e)
        {
            //1028, 2354
            this.btn_quick_quote.BackColor = Color.White;
            this.btn_project.BackColor = Color.FromArgb(255, 128, 128);

            this.tabControl.SelectedIndex = 1;
            this.tabControl.Height = 600;
            this.Size = new Size(1386 - 80, 2354);
        }

        public DataTable allTransactionList { get; set; } = new DataTable();
        public DataTable transactionList { get; set; } = new DataTable();
        public DataTable childList { get; set; } = new DataTable();
        public DataTable ItemList { get; set; } = new DataTable();

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

            customerList.Merge(bpi_dt);
            customerList.Merge(bpi_general);
            customerList.Merge(bpi_address);
            customerList.Merge(bpi_contacts);
        }



        private async void fetchQuotationDetails()
        {
            SalesQuotationList data = await QuotationService.GetQuotations();

            if (data != null && data.SalesQuotation != null && data.SalesQuotation.Any())
            {
                // Version filter
                var latestQuotations = data.SalesQuotation
                    .GroupBy(q => q.document_no)
                    .Select(group => group.OrderByDescending(q => q.version_no)
                    .First())
                    .ToList();

                // GET the latest version
                transactionList = JsonHelper.ToDataTable(latestQuotations);
                allTransactionList = JsonHelper.ToDataTable(data.SalesQuotation);
                childList = JsonHelper.ToDataTable(data.SalesQuotationQuick);


                pnl_header.Enabled = true;
                pnl_footer.Enabled = true;

                Panel[] pnl_list = { pnl_header, pnl_footer };
                Helpers.ReadOnlyControls(pnl_list);

                toolstrip_quotation.Enabled = false;
                dgv_quick_quote_details.Enabled = true;
                dgv_quick_quote_details.Enabled = true;
                toolstrip_quotation.Enabled = true;

                if (data != null)
                {
                    bind(true);
                }
            }
            else
            {
                MessageBox.Show("Please create a new data!");
            }
        }

        private async void fetchQuotationDetailsByDocumentNo(string documentNo)
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

                // Enable the panels and controls as needed
                pnl_header.Enabled = true;
                pnl_footer.Enabled = true;
                toolstrip_quotation.Enabled = false;
                dgv_quick_quote_details.Enabled = true;

                // Enable the toolbar and DataGridView again after loading
                toolstrip_quotation.Enabled = true;

                // If filtered data exists, bind it to the DataGridView
                if (filteredSalesQuotation.Any() || filteredSalesQuotationQuick.Any())
                {
                    bind(true);
                }
                else
                {
                    // Optionally, handle the case where no matching documentNo was found
                    MessageBox.Show("No records found for the provided document number.");
                }
            }
            else
            {
                // If no matching SalesQuotation was found
                MessageBox.Show("No SalesQuotation found for the provided document number.");
            }
        }

        private void DocumentIncrementer()
        {
            string docNum;

            if (transactionList.Rows.Count > 0)
            {
                int latestIndex = transactionList.Rows.Count - 1;
                DataRow latestRow = transactionList.Rows[latestIndex];

                // Check if "document_no" is not null or DBNull
                if (latestRow["document_no"] != DBNull.Value && !string.IsNullOrEmpty(latestRow["document_no"].ToString()))
                {
                    // Parse the document number
                    if (int.TryParse(latestRow["document_no"].ToString(), out int documentNumber))
                    {
                        // Increment the document number
                        docNum = (documentNumber + 1).ToString().PadLeft(4, '0'); // Pad with leading zeros
                    }
                    else
                    {
                        // Handle parsing failure
                        docNum = "0001";
                    }
                }
                else
                {
                    // Handle null or empty document_no (e.g., use default value)
                    docNum = "0001"; // Default value
                }
            }
            else
            {
                // Handle empty DataTable (e.g., use default value)
                docNum = "0001";
            }

            // Assign the document number to the TextBox
            txt_document_no.Text = "Q#" + docNum;
        }

        private void btn_new_setup_1_Click(object sender, EventArgs e)
        {
        }

        private async void btn_save_Click(object sender, EventArgs e)
        {
            try
            {
                Panel[] pnl_list = { pnl_header, pnl_footer };
                var parentData = Helpers.GetControlsValues(pnl_list);
                var dataSource = Helpers.ConvertDataGridViewToDataTable(dgv_quick_quote_details);
                List<Dictionary<string, dynamic>> quickQuoteList = new List<Dictionary<string, dynamic>>();

                foreach (DataRow item in dataSource.Rows)
                {
                    Dictionary<string, object> data = new Dictionary<string, object>();
                    data.Add("item_id", int.Parse(item[2].ToString()));
                    data.Add("qty", int.Parse(item[5].ToString()));
                    data.Add("unit_id", int.Parse(item[6].ToString()));
                    data.Add("unit_price", decimal.Parse(item[7].ToString()));
                    data.Add("percent_discount", decimal.Parse(item[8].ToString()));
                    data.Add("net_discount", decimal.Parse(item[9].ToString()));
                    data.Add("net_total", decimal.Parse(item[10].ToString()));
                    data.Add("line_total", decimal.Parse(item[11].ToString()));
                    quickQuoteList.Add(data);
                }

                if (quickQuoteList != null)
                {
                    List<Dictionary<string, dynamic>> childCollection = new List<Dictionary<string, dynamic>>();

                    // loops thru the items
                    foreach (var childData in quickQuoteList)
                    {
                        childCollection.Add(childData);
                    }

                    // trims the Q# from the input
                    if (parentData.ContainsKey("document_no") && parentData["document_no"] is string documentNo)
                    {
                        parentData["document_no"] = documentNo.StartsWith("Q#")
                            ? documentNo.Substring(2) // Remove "Q#"
                            : documentNo; // Keep as is if "Q#" is not present
                    }

                    //
                    // MAKE A HELPER THAT CONVERT ID TO INT 
                    if (parentData.ContainsKey("customer_id") && parentData["customer_id"] is string customerIdStr)
                    {
                        if (int.TryParse(customerIdStr, out int customerId))
                        {
                            parentData["customer_id"] = customerId;
                        }
                        else
                        {
                            MessageBox.Show("Invalid customer ID");
                            return;
                        }
                    }


                    parentData["sales_quotation_quick"] = childCollection;

                    if (parentData.ContainsKey("sales_quotation_quick"))
                    {
                        await QuotationService.Insert(parentData);

                        //// this should await a response in the future if the response is success proceed to create if not notify the user
                        Helpers.ResetControls(pnl_header);
                        Helpers.ResetControls(pnl_footer);
                        dgv_quick_quote_details.DataSource = this.childList.Clone();
                        //dgv_quick_quotes_show.Visible = true;
                        //dgv_quick_quotes_show.Enabled = false;
                        toolstrip_quotation.Enabled = true;

                        MessageBox.Show("Quotation Successfully saved");
                        fetchQuotationDetails();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("err: " + ex);
            }
        }

        private int selectedItem;

        private void dgv_quick_quote_details_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.ColumnIndex == 3)
            {
                DataGridViewRow clickedRow = dgv_quick_quote_details.Rows[e.RowIndex];

                ItemModal itemModal = new ItemModal(ItemList);
                DialogResult r = itemModal.ShowDialog();

                if (r == DialogResult.OK)
                {
                    int selectedIndex = itemModal.GetResult(); // Get the index from the modal

                    if (selectedIndex >= 0 && selectedIndex < ItemList.Rows.Count) // Ensure the index is valid
                    {

                        DataRow selectedItem = ItemList.Rows[selectedIndex];
                        this.dgv_quick_quote_details.Rows[e.RowIndex].Cells[2].Value = selectedItem["id"].ToString();
                        this.dgv_quick_quote_details.Rows[e.RowIndex].Cells[3].Value = selectedItem["item_code"].ToString();
                        this.dgv_quick_quote_details.Rows[e.RowIndex].Cells[4].Value = selectedItem["item_name"].ToString();
                    }
                    else
                    {
                        MessageBox.Show("Invalid selection", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void ComputeDgv(DataGridViewCellEventArgs e)
        {
            try
            {
                var qty_cell = dgv_quick_quote_details.Rows[e.RowIndex].Cells[QuickQuoteDGV.QTY].Value;
                var unit_price_cell = dgv_quick_quote_details.Rows[e.RowIndex].Cells[QuickQuoteDGV.UNIT_PRICE].Value;
                var discount_cell = dgv_quick_quote_details.Rows[e.RowIndex].Cells[QuickQuoteDGV.DISCOUNT].Value == null ? "0" : dgv_quick_quote_details.Rows[e.RowIndex].Cells[QuickQuoteDGV.DISCOUNT].Value.ToString();

                if (qty_cell != null && unit_price_cell != null && discount_cell != null)
                {
                    double gross_sales = 0, vat_amount_computed_temp = 0, net_sales = 0, sub_total_before_discount = 0, percent_discount = 0, sub_total = 0, vat_amount = 0, cash_discount = 0, net_amount_due = 0, total_amount_due = 0;

                    decimal unitPrice;
                    int qty = int.Parse(this.dgv_quick_quote_details.Rows[e.RowIndex].Cells[QuickQuoteDGV.QTY].Value.ToString());
                    bool unitPriceValid = decimal.TryParse(this.dgv_quick_quote_details.Rows[e.RowIndex].Cells[QuickQuoteDGV.UNIT_PRICE].Value.ToString(), out unitPrice);
                    string discountPercent = this.dgv_quick_quote_details.Rows[e.RowIndex].Cells[QuickQuoteDGV.DISCOUNT].Value == null ? "0" : dgv_quick_quote_details.Rows[e.RowIndex].Cells[QuickQuoteDGV.DISCOUNT].Value.ToString();

                    DGVComputation DgvComputation = new DGVComputation(qty, unitPrice, discountPercent);
                    DgvComputation.ComputeQuickQuote();

                    this.dgv_quick_quote_details.Rows[e.RowIndex].Cells[QuickQuoteDGV.NET_AMOUNT].Value = DgvComputation.NetAmount;
                    this.dgv_quick_quote_details.Rows[e.RowIndex].Cells[QuickQuoteDGV.NET_DISCOUNT].Value = DgvComputation.NetDiscount;
                    this.dgv_quick_quote_details.Rows[e.RowIndex].Cells[QuickQuoteDGV.DISCOUNT_AMOUNT].Value = DgvComputation.DiscountedAmount;
                    this.dgv_quick_quote_details.Rows[e.RowIndex].Cells[QuickQuoteDGV.LINE_TOTAL].Value = DgvComputation.LineTotal;

                    foreach (DataGridViewRow row in this.dgv_quick_quote_details.Rows)
                    {
                        var netAmount = row.Cells[QuickQuoteDGV.NET_AMOUNT].Value;
                        var netDiscount = row.Cells[QuickQuoteDGV.NET_DISCOUNT].Value;
                        var discountedAmount = row.Cells[QuickQuoteDGV.DISCOUNT_AMOUNT].Value;
                        var lineTotal = row.Cells[QuickQuoteDGV.LINE_TOTAL].Value;

                        if (netAmount != null && !String.IsNullOrEmpty(netAmount.ToString()))
                        {
                            double netAmountValue = double.Parse(netAmount.ToString());  // Parse once
                            gross_sales += netAmountValue;

                            Taxation Tax = new Taxation(netAmountValue, chk_isVat.Checked ? double.Parse(txt_vat_percent.Text) : 0);
                            double vatAmount = chk_isVat.Checked
                                ? Tax.GetVatInclusive() - netAmountValue
                                : netAmountValue - Tax.GetVatExclusive();

                            vat_amount_computed_temp += vatAmount;
                            net_sales = gross_sales - vat_amount_computed_temp;

                            sub_total_before_discount += net_sales;

                            // If the discount is a percentage, ensure it's added correctly
                            percent_discount += double.Parse(discountedAmount.ToString());  // Accumulate discount
                            sub_total = sub_total_before_discount - percent_discount;

                            vat_amount += vatAmount;
                            net_amount_due += netAmountValue - double.Parse(txt_cash_discount.Text);
                            total_amount_due += net_sales - (percent_discount + double.Parse(txt_cash_discount.Text));

                            // Formatting results with Helpers.MoneyFormat
                            txt_gross_sales.Text = Helpers.MoneyFormat(gross_sales);
                            vat_amount_computed.Text = Helpers.MoneyFormat(vat_amount_computed_temp);
                            txt_net_sales.Text = Helpers.MoneyFormat(net_sales);

                            txt_sub_total_before_discount.Text = Helpers.MoneyFormat(sub_total_before_discount);
                            txt_percent_discount.Text = Helpers.MoneyFormat(percent_discount);

                            txt_sub_total.Text = Helpers.MoneyFormat(sub_total);
                            txt_vat_amount.Text = Helpers.MoneyFormat(vat_amount);
                            txt_cash_discount.Text = Helpers.MoneyFormat(double.Parse(cash_discount.ToString()));
                            txt_net_amount_due.Text = Helpers.MoneyFormat(net_amount_due);
                            txt_total_amount_due.Text = Helpers.MoneyFormat(total_amount_due);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("err" + ex);
            }
        }

        private void dgv_quick_quote_details_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            ComputeDgv(e);
        }

        private void QuickQuotesDgvDefaultValues()
        {
            //// Set the initial row count to 20
            this.dgv_quick_quote_details.RowCount = 1;

            if (this.dgv_quick_quote_details.RowCount == 1)
            {
                this.dgv_quick_quote_details.Rows.Add();
            }

            // Optionally, you can add default data to the rows
            for (int i = 0; i <= this.dgv_quick_quote_details.RowCount; i++)
            {
                this.dgv_quick_quote_details.Rows[i].Cells[QuickQuoteDGV.UNIT_PRICE].Value = 0;
                this.dgv_quick_quote_details.Rows[i].Cells[QuickQuoteDGV.DISCOUNT].Value = 50;
                this.dgv_quick_quote_details.Rows[i].Cells[QuickQuoteDGV.DISCOUNT_AMOUNT].Value = 0;
                this.dgv_quick_quote_details.Rows[i].Cells[QuickQuoteDGV.NET_AMOUNT].Value = 0;
                this.dgv_quick_quote_details.Rows[i].Cells[QuickQuoteDGV.NET_DISCOUNT].Value = 0;
                this.dgv_quick_quote_details.Rows[i].Cells[QuickQuoteDGV.LINE_TOTAL].Value = 0;
            }
        }

        private void dgv_quick_quote_details_KeyUp(object sender, KeyEventArgs e)
        {
        }

        private async void Quotation_Load(object sender, EventArgs e)
        {

            fetchItemData();
            fetchBpiData();


            if (!string.IsNullOrEmpty(documentNo))
            {
                this.btn_quick_quote.BackColor = Color.FromArgb(255, 128, 128);
                this.btn_project.BackColor = Color.White;

                this.tabControl.SelectedIndex = 0;
                this.tabControl.Height = 600;  // Set the desired width and height for the form
                this.Size = new Size(1386 - 80, 900);  // Set the desired width and height for the form

                this.tabControl.ItemSize = new Size(0, 0);

                fetchQuotationDetailsByDocumentNo(documentNo);
                bs_unit.DataSource = CacheData.UoM;
            }
            else
            {
                this.btn_quick_quote.BackColor = Color.FromArgb(255, 128, 128);
                this.btn_project.BackColor = Color.White;

                this.tabControl.SelectedIndex = 0;
                this.tabControl.Height = 600;  // Set the desired width and height for the form
                this.Size = new Size(1386 - 80, 900);  // Set the desired width and height for the form

                this.tabControl.ItemSize = new Size(0, 0);

                cmb_payment_terms.DataSource = CacheData.PaymentTerms;
                cmb_payment_terms.DisplayMember = "code";
                cmb_payment_terms.ValueMember = "id";

                cmb_ship_to.DataSource = CacheData.PaymentTerms;
                cmb_ship_to.DisplayMember = "code";
                cmb_ship_to.ValueMember = "id";

                cmb_bill_to.DataSource = CacheData.PaymentTerms;
                cmb_bill_to.DisplayMember = "code";
                cmb_bill_to.ValueMember = "id";

                cmb_application.DataSource = CacheData.ApplicationSetup;
                cmb_application.DisplayMember = "code";
                cmb_application.ValueMember = "id";

                cmb_purpose.DataSource = STATIC_QUOTATION_PURPOSE.LIST();
                cmb_purpose.DisplayMember = "code";
                cmb_purpose.ValueMember = "title";

                cmb_ship_type.DataSource = STATIC_SHIPPED_TYPE.LIST();
                cmb_ship_type.DisplayMember = "code";
                cmb_ship_type.ValueMember = "title";

                //cmb_unit_code.DataSource = STATIC_SHIPPED_TYPE.LIST();
                //cmb_unit_code.DisplayMember = "title";
                //cmb_unit_code.ValueMember = "value";

                //DataTable dtQuotationDetails = ds_quick_quote.Tables["quotation_details"];

                //foreach (DataRow item in CacheData.PaymentTerms.Rows)
                //{
                //    int ID = 0;
                //    int CODE = 1;

                //    DataRow newRow = dtQuotationDetails.NewRow();
                //    newRow["title"] = item[CODE];
                //    newRow["value"] = item[ID];
                //    dtQuotationDetails.Rows.Add(newRow);
                //}

                var data = ds_quick_quote.Tables["quotation_details"];

                bs_unit.DataSource = CacheData.UoM;
                //var combobox = (DataGridViewComboBoxColumn)dgv_quick_quote_details.Columns["unit_code"];
                //combobox.DataSource = CacheData.UoM;
                //combobox.DisplayMember = "name";
                //combobox.ValueMember = "id";

                fetchQuotationDetails();
            }
        }

        private void bind(bool isBind = false)
        {
            if (isBind) 
            {
                Panel[] pnlList = { pnl_header, pnl_footer };

                DataTable HeaderList = this.transactionList.Clone();
                HeaderList.Columns.Add("branch_name", typeof(string));
                HeaderList.Columns.Add("customer_code", typeof(string));
                HeaderList.Columns.Add("number", typeof(string));

                foreach (DataRow parentRow in this.transactionList.Rows)
                {
                    DataRow newRow = HeaderList.NewRow();
                    foreach (DataColumn col in this.transactionList.Columns)
                    {
                        newRow[col.ColumnName] = parentRow[col.ColumnName];
                    }

                    string ID = parentRow["customer_id"].ToString();
                    DataRow[] bpiRows = customerList.Select($"based_id = '{ID}'");
                    DataRow[] contactsRows = customerList.Select($"contacts_based_id = '{ID}'");

                    if (bpiRows.Length > 0)
                    {
                        newRow["branch_name"] = bpiRows[0]["branch_name"].ToString();
                        newRow["customer_code"] = bpiRows[0]["customer_code"].ToString();
                        newRow["number"] = contactsRows[0]["number"].ToString();
                    }
                    else
                    {
                        newRow["branch_name"] = "Unknown Branch";
                        newRow["customer_code"] = "N/A";
                    }

                    HeaderList.Rows.Add(newRow);
                }

                Helpers.BindControls(pnlList, HeaderList, SelectedRow);
                // Clone childList and add item_name column
                DataTable withItemList = this.childList.Clone();
                withItemList.Columns.Add("item_name", typeof(string));
                withItemList.Columns.Add("item_code", typeof(string));

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
                        newRow["item_name"] = itemRows[0]["item_name"].ToString();
                        newRow["item_code"] = itemRows[0]["item_code"].ToString();
                    }
                    else
                    {
                        newRow["item_name"] = "Unknown Item";
                        newRow["item_code"] = "N/A";
                    }
                    withItemList.Rows.Add(newRow);
                }

                // Create filtered view
                DataView dataview = new DataView(withItemList);
                dataview.RowFilter = "based_id = '" + this.transactionList.Rows[this.SelectedRow]["id"].ToString() + "'";
                bs_quick_quotes_details.DataSource = dataview;
            }
        }

        private void txt_days_TextChanged(object sender, EventArgs e)
        {
            ValidUntilDate();
        }

        private void ValidUntilDate()
        {
            var date = dtp_date.Value;
            var noOfDays = txt_days.Text == "" ? "0" : txt_days.Text;

            if (int.Parse(noOfDays) > 0 && int.Parse(noOfDays) < 1000)
            {
                dtp_valid_until.Text = date.AddDays(double.Parse(noOfDays)).ToString();
            }
            else
            {
                txt_days.Text = "30";
            }
        }

        private void dtp_date_ValueChanged(object sender, EventArgs e)
        {
            ValidUntilDate();
        }

        public DataTable customerList { get; set; } = new DataTable();
        private DataTable bpi_dt = new DataTable();
        private DataTable bpi_general = new DataTable();
        private DataTable bpi_address = new DataTable();
        private DataTable bpi_contacts = new DataTable();

        private async void btn_new_Click_1(object sender, EventArgs e)
        {
            Helpers.ResetControls(pnl_header);
            Helpers.ResetControls(pnl_footer);
            Panel[] pnls = { pnl_header, pnl_footer };
            Helpers.ResetReadOnlyControls(pnls);

            // sets the version to 1 if new data and make it readonly to prevent editing
            txt_version_no.Text = "1";
            txt_version_no.ReadOnly = true;

            foreach (Control ctrl in pnl_footer.Controls)
            {
                if (ctrl is TextBox)
                {
                    ((TextBox)ctrl).Text = "0";
                }
            }

            pnl_header.Enabled = true;
            pnl_footer.Enabled = true;

            toolstrip_quotation.Enabled = false;
            dgv_quick_quote_details.Enabled = true;

            bs_quick_quotes_details.DataSource = childList.Clone();

            bind(false);
            DocumentIncrementer();
            txt_vat_percent.Text = "12";
            txt_vat_percent.ReadOnly = true;
            //this.QuickQuotesDgvDefaultValues();
        }

        private void btn_new_version_Click(object sender, EventArgs e)
        {
            pnl_header.Enabled = true;
            pnl_footer.Enabled = true;
            Panel[] pnl_list = { pnl_header, pnl_footer };
            Helpers.ResetReadOnlyControls(pnl_list);

            toolstrip_quotation.Enabled = false;
            dgv_quick_quote_details.Enabled = true;

            txt_version_no.Text = (int.Parse(txt_version_no.Text) + 1).ToString();
        }

        private void btn_cancel_Click(object sender, EventArgs e)
        {
            pnl_header.Enabled = false;
            pnl_footer.Enabled = false;

            toolstrip_quotation.Enabled = true;
        }

        private void btn_next_Click(object sender, EventArgs e)
        {
            int rowCount = transactionList.Rows.Count;
            if (SelectedRow < rowCount - 1)
            {

                var nextRow = transactionList.Rows.Cast<DataRow>()
                                      .Skip(SelectedRow + 1)
                                      .FirstOrDefault();
                if (nextRow != null)
                {
                    SelectedRow = transactionList.Rows.IndexOf(nextRow);
                    bind(true);
                }

                //SelectedRow++;
                //bind(true);
                //fetchQuotationDetails();
            }
        }

        private void btn_prev_Click(object sender, EventArgs e)
        {
            if (SelectedRow >= 1)
            {
                SelectedRow--;
                bind(true);
            }
        }
        private async void button1_Click(object sender, EventArgs e)
        {
            List<int> t1 = new List<int>();
            List<string> s1 = new List<string>();
            string Title = "Business Partner Info";
            string endpoint = "/api/bpi";
            SetupSelectionModal bpi = new SetupSelectionModal(Title, endpoint, customerList, t1, s1, 0);
            DialogResult r = bpi.ShowDialog();

            if (r == DialogResult.OK)
            {
                Dictionary<string, string> result = bpi.GetResult();

                if (result != null)
                {
                    string id = "";

                    var isSuccess_baseid = result.TryGetValue("id", out id);

                    Panel[] pnl_list = { pnl_header };
                    txt_customer_id.Text = id.ToString();
                    Helpers.BindControls(pnl_list, bpi_general);
                    Helpers.BindControls(pnl_list, bpi_address);
                    Helpers.BindControls(pnl_list, bpi_contacts);
                    //MessageBox.Show("" + data);
                }
            }
        }

        private void btn_search_Click(object sender, EventArgs e)
        {
            string Title = "Quotation List";
            SetupModal setup = new SetupModal(Title, transactionList);
            DialogResult r = setup.ShowDialog();

            if (r == DialogResult.OK)
            {
                int result = setup.GetResult();

                if (result != -1)
                {
                    SelectedRow = result;
                    fetchQuotationDetails();
                    //fetchQuotationDetails();
                }
            }
        }

        private static class QuickQuoteDGV
        {
            public static int QTY = 5;
            //public static int UNIT_MEASURE = ;
            public static int UNIT_PRICE = 7;
            public static int DISCOUNT = 8;
            public static int DISCOUNT_AMOUNT = 9;
            public static int NET_DISCOUNT = 10;
            public static int NET_AMOUNT = 11;
            public static int LINE_TOTAL = 12;
        }

        private class DGVComputation
        {
            private decimal Qty { get; set; }
            private decimal UnitPrice { get; set; }
            private string DiscountPercent { get; set; }
            public decimal DiscountedAmount { get; private set; }
            public decimal NetAmount { get; private set; }
            public decimal NetDiscount { get; private set; }
            public decimal LineTotal { get; private set; }

            public DGVComputation(decimal qty, decimal unitPrice, string discountPercent = "")
            {
                this.Qty = qty;
                this.UnitPrice = unitPrice;
                this.DiscountPercent = discountPercent;
            }

            public void ComputeQuickQuote()
            {
                try
                {
                    if (this.Qty > 0 && this.UnitPrice > 0)
                    {
                        // COMPUTE NET AMOUNT
                        this.NetAmount = this.Qty * this.UnitPrice;      

                        //// COMPUTE DISCOUNTED AMOUNT
                        if (!String.IsNullOrEmpty(this.DiscountPercent) && this.DiscountPercent != "0")
                        {
                            this.DiscountedAmount = this.UnitPrice - (this.UnitPrice * (decimal.Parse(this.DiscountPercent) / 100));
                        }
                        //// COMPUTE NET DISCOUNT
                        this.NetDiscount = this.DiscountedAmount * this.Qty;

                        //// COMPUTE LINE TOTAL
                        this.LineTotal = this.DiscountedAmount > 0 ? this.DiscountedAmount * this.Qty : this.NetAmount;
                    }
                }
                catch (Exception)
                {
                    throw;
                }
            }
        }

        // When the vat is changed trigger the computation
        private void txt_vat_percent_TextChanged(object sender, EventArgs e)
        {
        }

        private void dgv_quick_quote_details_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
        }

        private void fetchQuotationBasedOnVersion()
        {
            bindVersion(true);
        }

        private void txt_version_no_DoubleClick(object sender, EventArgs e)
        {
            string docNum = txt_document_no.Text.ToString();
            VersionModal vm = new VersionModal(allTransactionList, docNum);
            DialogResult r = vm.ShowDialog();

            if (r == DialogResult.OK)
            {
                Dictionary<string, string> result = vm.GetResult();

                if (result != null)
                {
                    string ver;
                    string doc;

                    result.TryGetValue("version_no", out ver);
                    result.TryGetValue("document_no", out doc);

                    var versionFilter = allTransactionList.AsEnumerable()
                        .Where(row => row["document_no"].ToString() == doc && row["version_no"].ToString() == ver)
                        .CopyToDataTable();

                    bindVersion(true, versionFilter);
                }
            }
        }

        private void bindVersion(bool isBind = false, DataTable ver = null)
        {
            if (isBind && ver != null)
            {
                Panel[] pnlList = { pnl_header, pnl_footer };

                Helpers.BindControls(pnlList, ver, SelectedRow);
                DataView dataview = new DataView(this.childList);
                dataview.RowFilter = "based_id = '" + ver.Rows[this.SelectedRow]["id"].ToString() + "'";
                bs_quick_quotes_details.DataSource = dataview;
            }
        }

        private void btn_search_Click_1(object sender, EventArgs e)
        {
            string Title = "Quotation List";
            SetupModal setup = new SetupModal(Title, transactionList);
            DialogResult r = setup.ShowDialog();

            if (r == DialogResult.OK)
            {
                int result = setup.GetResult();

                if (result != -1)
                {
                    SelectedRow = result;
                    bind(true);
                }
            }
        }
    }
}