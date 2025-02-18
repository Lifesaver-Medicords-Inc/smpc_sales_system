using smpc_app.Data;
using smpc_app.Services.Helpers;
using smpc_inventory_app.Pages;
using smpc_sales_app.Data;
using smpc_sales_app.Services.Helpers;
using smpc_sales_app.Services.Sales;
using smpc_sales_app.Utils;
using smpc_sales_system.Models;
using smpc_sales_system.Pages;
using smpc_sales_system.Pages.Sales;
using smpc_sales_system.Services.Sales.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
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
        public DataTable a { get; set; } = new DataTable();

        private async void fetchItemData()
        {
            var itemData = await ItemService.GetItem();
            ItemList = JsonHelper.ToDataTable(itemData.items);
            a = JsonHelper.ToDataTable(itemData.itemspecs);
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
                button1.Enabled = true;

                toolstrip_quotation.Enabled = false;
                dgv_quick_quote_details.Enabled = true;
                dgv_quick_quote_details.Enabled = true;
                toolstrip_quotation.Enabled = true;

                if (data != null)
                {
                    await Task.Delay(2000);
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
                   
                    if (int.TryParse(latestRow["document_no"].ToString(), out int documentNumber))
                    {
                        
                        docNum = (documentNumber + 1).ToString().PadLeft(4, '0'); 
                    }
                    else
                    {
                       
                        docNum = "0001";
                    }
                }
                else
                {
                   
                    docNum = "0001"; 
                }
            }
            else
            {
                
                docNum = "0001";
            }
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
                    data.Add("net_discount", decimal.Parse(item[10].ToString()));
                    data.Add("net_total", decimal.Parse(item[11].ToString()));
                    data.Add("line_total", decimal.Parse(item[12].ToString()));
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
                        //dgv_quick_quote_details.DataSource = this.childList.Clone();
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


                    computationLoop();
    
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("err" + ex);
            }
        }

    
        private void computationLoop()
        {
            double gross_sales = 0, vat_amount = 0, net_sales = 0;
            double percent_discount = 0;
            double net_amount_due = 0, total_amount_due = 0;
            double cash_discount = double.Parse(txt_cash_discount.Text);
            const double VAT_RATE = 0.12; // 12% VAT
            const double TAX_RATE = 0.12; // 12% Tax

            // First pass: Calculate gross sales and total discounts
            foreach (DataGridViewRow row in this.dgv_quick_quote_details.Rows)
            {
                if (row.Cells[QuickQuoteDGV.NET_AMOUNT].Value != null &&
                    !String.IsNullOrEmpty(row.Cells[QuickQuoteDGV.NET_AMOUNT].Value.ToString()))
                {
                    // Get unit price * quantity = net total
                    double netAmount = double.Parse(row.Cells[QuickQuoteDGV.NET_AMOUNT].Value.ToString());
                    gross_sales += netAmount;

                    // Get line total (after discount)
                    if (row.Cells[QuickQuoteDGV.LINE_TOTAL].Value != null &&
                        !String.IsNullOrEmpty(row.Cells[QuickQuoteDGV.LINE_TOTAL].Value.ToString()))
                    {
                        double lineTotal = double.Parse(row.Cells[QuickQuoteDGV.LINE_TOTAL].Value.ToString());
                        net_sales += lineTotal;
                    }
                }
            }

            // Calculate percent discount
            if (gross_sales != 0)
            {
                percent_discount = ((gross_sales - net_sales) / gross_sales) * 100;
            }


            // Calculate VAT (12% of net sales)
            vat_amount = net_sales * VAT_RATE;

            // Calculate net amount due (subtract cash discount)
            net_amount_due = net_sales - cash_discount;

            // Calculate total amount due (net amount + VAT + tax)
            total_amount_due = net_amount_due + vat_amount;

            // Format and display results
            txt_gross_sales.Text = Helpers.MoneyFormat(gross_sales);
            txt_vat_amount.Text = Helpers.MoneyFormat(vat_amount);
            //txt_tax_amount.Text = Helpers.MoneyFormat(tax_amount);
            txt_net_sales.Text = Helpers.MoneyFormat(net_sales);
            //txt_sub_total_before_discount.Text = Helpers.MoneyFormat(sub_total_before_discount);
            txt_percent_discount.Text = percent_discount + "%";
            //txt_sub_total.Text = Helpers.MoneyFormat(sub_total);
            txt_cash_discount.Text = Helpers.MoneyFormat(cash_discount);
            txt_net_amount_due.Text = Helpers.MoneyFormat(net_amount_due);
            txt_total_amount_due.Text = Helpers.MoneyFormat(total_amount_due);
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

            dtp_date.Format = DateTimePickerFormat.Custom;
            dtp_date.CustomFormat = "MMM dd yyyy";
            dtp_valid_until.Format = DateTimePickerFormat.Custom;
            dtp_valid_until.CustomFormat = "MMM dd yyyy";


            if (!string.IsNullOrEmpty(documentNo))
            {
                fetchQuotationDetailsByDocumentNo(documentNo);
            }
            else
            {
                this.btn_quick_quote.BackColor = Color.FromArgb(255, 128, 128);
                this.btn_project.BackColor = Color.White;

                this.tabControl.SelectedIndex = 0;
                this.tabControl.Height = 600;  // Set the desired width and height for the form
                this.Size = new Size(1386 - 80, 900);  // Set the desired width and height for the form

                this.tabControl.ItemSize = new Size(0, 0);

                cmb_application.DataSource = CacheData.ApplicationSetup;
                cmb_application.DisplayMember = "code";
                cmb_application.ValueMember = "id";

                cmb_purpose.DataSource = STATIC_QUOTATION_PURPOSE.LIST();
                cmb_purpose.DisplayMember = "code";
                cmb_purpose.ValueMember = "title";

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
                bs_payment_terms.DataSource = CacheData.PaymentTerms;
                bs_ship_type.DataSource = CacheData.ShipTypeSetup;

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

                bs_ship_to.DataSource = bpi_address;
                bs_bill_to.DataSource = bpi_address;

                
                foreach (DataRow parentRow in this.transactionList.Rows)
                {
                    DataRow newRow = HeaderList.NewRow();
                    foreach (DataColumn col in this.transactionList.Columns)
                    {
                        newRow[col.ColumnName] = parentRow[col.ColumnName];
                    }

                    string ID = parentRow["customer_id"].ToString();
                    string BillToId = parentRow["bill_to_id"].ToString();
                    string ShipToId = parentRow["ship_to_id"].ToString();


                    DataRow[] bpiRows = bpi_general.Select($"based_id = '{ID}'");
                    DataRow[] contactsRows = bpi_contacts.Select($"contacts_based_id = '{ID}'");
                   


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
            var noOfDays = txt_days.Text;

            // Default to "30" days if input is empty
            if (string.IsNullOrEmpty(noOfDays))
            {
                noOfDays = "30";
            }

            // Parse the number of days
            if (int.TryParse(noOfDays, out int days) && days > 0 && days < 1000)
            {
                // Add the valid number of days to the selected date
                dtp_valid_until.Value = date.AddDays(days);
            }
            else
            {
                // Invalid or out of range input, reset to default (30 days)
                txt_days.Text = "30";
                dtp_valid_until.Value = date.AddDays(30); // Adding 30 days as the default
            }
        }


        private void dtp_date_ValueChanged(object sender, EventArgs e)
        {
            ValidUntilDate();
        }

        public  DataTable customerList { get; set; } = new DataTable();
        private DataTable bpi_dt = new DataTable();
        private DataTable bpi_general = new DataTable();
        private DataTable bpi_address = new DataTable();
        private DataTable bpi_contacts = new DataTable();

        private async void btn_new_Click_1(object sender, EventArgs e)
        {
            Helpers.ResetControls(pnl_header);
            Helpers.ResetControls(pnl_footer);

          //
         // resets the datasource so that only customers would specific address would be seen.
        //
            bs_bill_to.DataSource = null;
            bs_ship_to.DataSource = null;
            Panel[] pnls = { pnl_header, pnl_footer };
            Helpers.ReadOnlyControls(pnls);
            txt_cash_discount.ReadOnly = false;
            

            foreach (Control ctrl in pnl_footer.Controls)
            {
                if (ctrl is TextBox)
                {
                    TextBox txtBox = (TextBox)ctrl;
                    txtBox.Text = "0";
                }
            }

            toolstrip_quotation.Enabled = false;
            dgv_quick_quote_details.Enabled = true;

            bs_quick_quotes_details.DataSource = childList.Clone();

            bind(false);
            DocumentIncrementer();
            txt_vat_percent.Text = "12";
            txt_vat_percent.ReadOnly = true;
            btn_add_customer.Enabled = true;
            pnl_header.Enabled = true;
            pnl_footer.Enabled = true;
            btn_save.Enabled = true;

            DataTable dt = (DataTable)bs_quick_quotes_details.DataSource;
        }

        private void btn_new_version_Click(object sender, EventArgs e)
        {
            pnl_header.Enabled = true;
            pnl_footer.Enabled = true;
            Panel[] pnl_list = { pnl_header, pnl_footer };
            Helpers.ResetReadOnlyControls(pnl_list);

            toolstrip_quotation.Enabled = false;
            dgv_quick_quote_details.Enabled = true;

            string documentNo = txt_document_no.Text;

            var latestVer = allTransactionList.AsEnumerable()
                     .Where(row => row["document_no"].ToString() == documentNo)
                     .GroupBy(row => row["document_no"])
                     .Select(group => group.OrderByDescending(row => row["version_no"])
                     .First()) 
                     .ToList();


            if (latestVer.Any())
            {
                int latestVersionNo = Convert.ToInt32(latestVer.First()["version_no"]);
                txt_version_no.Text = (latestVersionNo + 1).ToString();
            }
            else
            {
                txt_version_no.Text = "1"; 
            }
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
                SelectedRow++;
         
                bind(true);
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
        DataTable PerCustomerAddressList = new DataTable();
        private async void button1_Click(object sender, EventArgs e)
        {
            List<int> t1 = new List<int>();
            List<string> s1 = new List<string>();
            string Title = "Business Partner Info";
            string endpoint = "/api/bpi";
            SetupSelectionModal bpi = new SetupSelectionModal(Title, endpoint, bpi_general, t1, s1, 0);
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



                    var GeneralBpi =  Helpers.FilterDataTable(bpi_general, id, "based_id");
                    var BillAddress = Helpers.FilterDataTable(bpi_address, id, "address_based_id");
                    var ShipAddress = Helpers.FilterDataTable(bpi_address, id, "address_based_id");

                    bs_ship_to.DataSource = ShipAddress;




                    cmb_bill_to.DataSource = ShipAddress;
                    cmb_bill_to.DisplayMember = "location";
                    cmb_bill_to.ValueMember = "address_id";

                    Helpers.BindControls(pnl_list, GeneralBpi);
                    


                    Helpers.ResetReadOnlyControls(pnl_list);

                    // sets the version to 1 if new data and make it readonly to prevent editing
                    txt_version_no.Text = "1";
                    txt_version_no.ReadOnly = true;
                    txt_document_no.ReadOnly = true;

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
                        this.NetAmount = this.Qty * this.UnitPrice;
                        //MessageBox.Show("" + this.NetAmount);

                        //// COMPUTE DISCOUNTED AMOUNT
                        if (!string.IsNullOrEmpty(this.DiscountPercent) && this.DiscountPercent != "0")
                        {
                            if (this.DiscountPercent.Contains("/"))
                            {
                       
                                string[] discounts = this.DiscountPercent.Split('/');
                                decimal cumulativeMultiplier = 1;

                                foreach (string discount in discounts)
                                {
                                    if (decimal.TryParse(discount, out decimal discountValue))
                                    {
                                        cumulativeMultiplier *= (1 - (discountValue / 100));
                                    }
                                }

                                //this.DiscountedAmount = this.UnitPrice * (1 - cumulativeMultiplier);
                                this.DiscountedAmount = this.UnitPrice * cumulativeMultiplier;
                            }
                            else
                            {
                                // Single discount scenario
                                this.DiscountedAmount = this.UnitPrice * (decimal.Parse(this.DiscountPercent) / 100);
                            }
                        }
                        else
                        {
                            this.DiscountedAmount = 0;
                        }


                        //// COMPUTE NET DISCOUNT
                        this.NetDiscount = this.DiscountedAmount * this.Qty;

                        //// COMPUTE LINE TOTAL
                        this.LineTotal = this.NetAmount - this.NetDiscount;
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

        private void button1_Click_1(object sender, EventArgs e)
        {
            ProjectTest s = new ProjectTest();
            s.Show();
        }

        private void cmb_purpose_ValueMemberChanged(object sender, EventArgs e)
        {

        }

        private void vat_amount_computed_TextChanged(object sender, EventArgs e)
        {

        }

        private void txt_cash_discount_TextChanged(object sender, EventArgs e)
        {
           
        }

        private void txt_cash_discount_DoubleClick(object sender, EventArgs e)
        {
            computationLoop();
        }
    }
}