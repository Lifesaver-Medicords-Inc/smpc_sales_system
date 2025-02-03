using smpc_app.Data;
using smpc_app.Services.Helpers;
using smpc_inventory_app.Pages;
using smpc_sales_app.Data;
using smpc_sales_app.Services.Helpers;
using smpc_sales_app.Services.Sales;
using smpc_sales_app.Services.Setup;
using smpc_sales_app.Utils;
using smpc_sales_system.Models;
using smpc_sales_system.Services.Sales.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;



namespace smpc_sales_app.Pages.Sales
{
    public partial class Quotation : UserControl
    {
        ItemService itemService = new ItemService();

        int SelectedRow = 0;

        public Quotation()
        {

            InitializeComponent();

            cmb_warranty.Text = "1 year";
            // CALL THE DEFAULT VALUES OF DATAGRIDVIEW
            //this.QuickQuotesDgvDefaultValues();

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
            //Panel[] pnl_list = { pnl_header, pnl_footer };


        }

        private void btn_quick_quote_Click(object sender, EventArgs e)
        {
            //1028, 2354  
            this.btn_quick_quote.BackColor = Color.FromArgb(255, 128, 128);
            this.btn_project.BackColor = Color.White;

            this.tabControl.SelectedIndex = 0;
            this.tabControl.Height = 600;  // Set the desired width and height for the form
            this.Size = new Size(1386 - 80, 800);  // Set the desired width and height for the form

        }

        private void btn_project_Click(object sender, EventArgs e)
        {
            //1028, 2354 
            this.btn_quick_quote.BackColor = Color.White;
            this.btn_project.BackColor = Color.FromArgb(255, 128, 128);

            this.tabControl.SelectedIndex = 1;
            this.tabControl.Height = 600;  // Set the desired width and height for the form
            this.Size = new Size(1386 - 80, 2354);  // Set the desired width and height for the form
        }

        public DataTable transactionList { get; set; } = new DataTable();
        public DataTable childList { get; set; } = new DataTable();
        //public DataTable customerList { get; set; } = new DataTable();

        //public DataView dataView { get; set; } = new DataView();

        // unit code 
        private async void fetchQuotationDetails()
        {
            SalesQuotationList data = await QuotationService.GetQuotations();
          
            transactionList = JsonHelper.ToDataTable(data.SalesQuotation);
            childList = JsonHelper.ToDataTable(data.SalesQuotationQuick);
          
            pnl_header.Enabled = true;
            pnl_footer.Enabled = true;

            toolstrip_quotation.Enabled = false;
            dgv_quick_quote_details.Enabled = true;

            // call the default values of datagridview
            //this.quickquotesdgvdefaultvalues();
            dgv_quick_quote_details.Enabled = true;
            //bind(true);
            toolstrip_quotation.Enabled = true;

            if (data != null)
            {
                bind(true);
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
            //MessageBox.Show("" + docNum);
        }




        private void btn_new_setup_1_Click(object sender, EventArgs e)
        {
            SetupModal setupModal = new SetupModal("Application");


            if (DialogResult.OK == setupModal.ShowDialog())
            {
                MessageBox.Show("test");
            }
        }

        //   PROBLEM FOR TOMMOROW ADD DATAA
        //     - Add Data from parent and child
        //     - Base on parent id for the child base id
        //
        private async void btn_save_Click(object sender, EventArgs e)
        {
            
            try
            {
                var parentData = Helpers.GetControlsValues(pnl_header);

                var dataSource = Helpers.ConvertDataGridViewToDataTable(dgv_quick_quote_details);

                List<Dictionary<string, dynamic>> quickQuoteList = new List<Dictionary<string, dynamic>>();

                //Dictionary<string, dynamic> quickQuoteData = new Dictionary<string, dynamic>();


                foreach (DataRow item in dataSource.Rows)
                {
                    Dictionary<string, object> data = new Dictionary<string, object>();

                    data.Add("item_class_id", int.Parse(item["qty"].ToString()));
                    data.Add("unit_code",  item["unit"].ToString());
                    data.Add("unit_price",  decimal.Parse(item["unit_price"].ToString()));
                    data.Add("percent_discount",  decimal.Parse(item["percent_discount"].ToString()));
                    data.Add("net_discount",  decimal.Parse(item["net_discount"].ToString()));
                    data.Add("net_total",  decimal.Parse(item["net_total"].ToString()));
                    data.Add("line_total", decimal.Parse(item["line_total"].ToString()));

                    // data.Add("SalesQuotationQuick", childData);
                    quickQuoteList.Add(data);
                }


                if (quickQuoteList != null)
                {
                    List<Dictionary<string, dynamic>> childCollection = new List<Dictionary<string, dynamic>>();
                 
                    // loops thru the items
                    foreach (var childData in quickQuoteList)
                    {
                        //parentData["sales_quotation_quick"] = childData;
                        childCollection.Add(childData);
                    }



                    // trims the Q# from the input
                    if (parentData.ContainsKey("document_no") && parentData["document_no"] is string documentNo)
                    {
                        parentData["document_no"] = documentNo.StartsWith("Q#")
                            ? documentNo.Substring(2) // Remove "Q#"
                            : documentNo; // Keep as is if "Q#" is not present
                    }


                    parentData["sales_quotation_quick"] = childCollection;

                    if (parentData.ContainsKey("sales_quotation_quick"))
                    {
                        await QuotationService.Insert(parentData);

                        // this should await a response in the future if the response is success proceed to create if not notify the user
                        Helpers.ResetControls(pnl_header);
                        Helpers.ResetControls(pnl_footer);
                        Helpers.ClearDataGridView(dgv_quick_quote_details);
                        //dgv_quick_quotes_show.Visible = true;
                        //dgv_quick_quotes_show.Enabled = false;
                        toolstrip_quotation.Enabled = true;


                        // edit
                        dgv_quick_quote_details.Visible = false;

                    }
                   

                }


            }
            catch(Exception ex)
            {
                MessageBox.Show("err: " + ex);
            }
           
        }

        private void toolStripButton7_Click(object sender, EventArgs e)
        {

        }


        private void dgv_quick_quote_details_ColumnWidthChanged(object sender, DataGridViewColumnEventArgs e)
        {

        }

        private void dgv_quick_quote_details_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.ColumnIndex == 3)
            {

                DataGridViewRow clickedRow = dgv_quick_quote_details.Rows[e.RowIndex];

                ItemModal itemModal = new ItemModal();
                DialogResult r = itemModal.ShowDialog();

                if (r == DialogResult.OK)
                {
                    // this contains datagridviewrow data, how to add it on quick quote data grid view 
                    Dictionary<string, string> result = itemModal.GetResult();

                    if (result != null)
                    {
                        string id = "";
                        string code = "";
                        string name = "";
                        string unit_price = "";
                        string desc = "";
                       
                        var isSuccess_name = result.TryGetValue("name", out name);
                        var isSuccess_code = result.TryGetValue("code", out code);
                        var isSuccess_unit = result.TryGetValue("unitprice", out unit_price);
                        var isSuccess_desc = result.TryGetValue("short_desc", out desc);


                        this.dgv_quick_quote_details.Rows[e.RowIndex].Cells[3].Value = code;
                        this.dgv_quick_quote_details.Rows[e.RowIndex].Cells[4].Value = name;
                        this.dgv_quick_quote_details.Rows[e.RowIndex].Cells[7].Value = unit_price;
                        //this.dgv_quick_quote_details.Rows[e.RowIndex].Cells[13].Value = desc;

                    }
                    
                    
                }
            }

            //if (e.RowIndex >= 0)
            //{
            //    var nameValue = dgv_quick_quote_details.Rows[e.RowIndex].Cells[13].Value;

            //    if (nameValue != null)
            //    {
            //        txt_short_description.Text = nameValue.ToString();
            //    }

              
            //}
        }

       

        private void dgv_quick_quote_details_CellLeave(object sender, DataGridViewCellEventArgs e)
        {

        }




        private void dgv_quick_quote_details_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            try
            {
                
                var qty_cell = dgv_quick_quote_details.Rows[e.RowIndex].Cells[QuickQuoteDGV.QTY].Value;
                var unit_price_cell = dgv_quick_quote_details.Rows[e.RowIndex].Cells[QuickQuoteDGV.UNIT_PRICE].Value;
                var discount_cell = dgv_quick_quote_details.Rows[e.RowIndex].Cells[QuickQuoteDGV.DISCOUNT].Value == null ? "0" : dgv_quick_quote_details.Rows[e.RowIndex].Cells[QuickQuoteDGV.DISCOUNT].Value.ToString();


                if (qty_cell != null && unit_price_cell != null && discount_cell != null)
                {
                    double gross_sales = 0, vat_amount_computed_temp = 0, net_sales = 0, sub_total_before_discount = 0, percent_discount = 0, sub_total = 0, vat_amount = 0, cash_discount = 0, net_amount_due = 0, total_amount_due = 0;

                    int qty = int.Parse(this.dgv_quick_quote_details.Rows[e.RowIndex].Cells[QuickQuoteDGV.QTY].Value.ToString());
                    decimal unitPrice = decimal.Parse(this.dgv_quick_quote_details.Rows[e.RowIndex].Cells[QuickQuoteDGV.UNIT_PRICE].Value.ToString());
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
                            gross_sales += double.Parse(netAmount.ToString());

                            Taxation Tax = new Taxation(double.Parse(netAmount.ToString()), chk_isVat.Checked ? double.Parse(txt_vat_percent.Text) : 0);
                            double vatAmount = chk_isVat.Checked ? Tax.GetVatInclusive() - double.Parse(netAmount.ToString()) : double.Parse(netAmount.ToString()) - Tax.GetVatExclusive();

                            vat_amount_computed_temp += vatAmount;

                            net_sales = gross_sales - vat_amount_computed_temp;

                            sub_total_before_discount += net_sales;
                            percent_discount = +double.Parse(discountedAmount.ToString());
                            sub_total += sub_total_before_discount - percent_discount;
                            vat_amount += vatAmount;
                            net_amount_due += double.Parse(netAmount.ToString()) - double.Parse(txt_cash_discount.Text);
                            total_amount_due += net_sales - (percent_discount + double.Parse(txt_cash_discount.Text));

                            txt_gross_sales.Text = Helpers.MoneyFormat(gross_sales);
                            vat_amount_computed.Text = Helpers.MoneyFormat(vat_amount_computed_temp);
                            txt_net_sales.Text = Helpers.MoneyFormat(net_sales);

                            txt_sub_total_before_discount.Text = Helpers.MoneyFormat(sub_total_before_discount);

                            txt_percent_discount.Text = Helpers.MoneyFormat(double.Parse(percent_discount.ToString()));

                            txt_sub_total.Text = Helpers.MoneyFormat(double.Parse(sub_total.ToString()));
                            txt_vat_amount.Text = Helpers.MoneyFormat(double.Parse(vat_amount.ToString()));
                            txt_cash_discount.Text = Helpers.MoneyFormat(double.Parse(cash_discount.ToString()));
                            txt_net_amount_due.Text = Helpers.MoneyFormat(double.Parse(net_amount_due.ToString()));
                            txt_total_amount_due.Text = Helpers.MoneyFormat(double.Parse(total_amount_due.ToString()));

                        }

                    }
                }


            }
            catch (Exception ex)
            {

            }
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
                //this.dgv_quick_quote_details.Rows[i].Cells[QuickQuoteDGV.QTY].Value = $"1";
                this.dgv_quick_quote_details.Rows[i].Cells[QuickQuoteDGV.UNIT_MEASURE].Value = 2;
                //MessageBox.Show("test");
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

            cmb_unit_code.DataSource = STATIC_SHIPPED_TYPE.LIST();
            cmb_unit_code.DisplayMember = "title";
            cmb_unit_code.ValueMember = "value";

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

            bs_unit.DataSource = CacheData.PaymentTerms;
            //var combobox = (DataGridViewComboBoxColumn)dgv_quick_quote_details.Columns["cmb_unit_code"];
            //combobox.DataSource = CacheData.PaymentTerms;
            //combobox.DisplayMember = "code";
            //combobox.ValueMember = "id";

            fetchQuotationDetails();
            
        }

        private void bind(bool isBind = false) 
        {
            //if (parentData.ContainsKey("document_no") && parentData["document_no"] is string documentNo)
            //{
            //    parentData["document_no"] = documentNo.StartsWith("Q#")
            //        ? documentNo.Substring(2) // Remove "Q#"
            //        : documentNo; // Keep as is if "Q#" is not present
            //}


            if (isBind)
            {
                Panel[] pnlList = { pnl_header, pnl_footer };
                if (transactionList != null)
                {
                
                        foreach (DataRow row in transactionList.Rows)
                        {
                            var doc = row["document_no"];
                            MessageBox.Show("" + doc);
                        }

                }
               
                
                Helpers.BindControls(pnlList, transactionList, SelectedRow);
                //dgv_quick_quote_details.DataSource = dataView;
                // dgv_quick_quote_details.DataSource = childList;

                DataView dataview = new DataView(this.childList);
                dataview.RowFilter = "based_id = '" + this.transactionList.Rows[this.SelectedRow]["id"].ToString() + "'";
                //dgv_quick_quotes_show.DataSource = dataview;
                 
                bs_quick_quotes_details.DataSource = dataview;
                //dgv_quick_quote_details.DataSource = dataview;
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
        private async void btn_new_Click_1(object sender, EventArgs e)
        {
            GetBpiList data = await QuotationService.GetBpiCustomers();
            customerList = JsonHelper.ToDataTable(data.GetBpiCustomer);
            
            Helpers.ResetControls(pnl_header);
            Helpers.ResetControls(pnl_footer);

            pnl_header.Enabled = true;
            pnl_footer.Enabled = true;

            toolstrip_quotation.Enabled = false;
            dgv_quick_quote_details.Enabled = true;
            //dgv_quick_quote_details.DataSource = null;
            //dgv_quick_quotes_show.Visible = false;
            //dgv_quick_quotes_show.Enabled = false;


            //clears the source
            //dgv_quick_quote_details = new DataGridView(); 
            bs_quick_quotes_details.DataSource = childList.Clone();
            bind(false);
            // CALL THE DEFAULT VALUES OF DATAGRIDVIEW
            DocumentIncrementer();
            //this.QuickQuotesDgvDefaultValues();
        }

        //// CREATE DEFAULT DT STRUCT
        //private DataTable CreateDefaultDataTableStructure()
        //{
        //    // Create a new DataTable
        //    DataTable defaultTable = new DataTable();

        //    // Add columns with their data types
        //    defaultTable.Columns.Add("id", typeof(int)); // Assuming 'id' is an integer
        //    defaultTable.Columns.Add("based_id", typeof(int)); // Assuming 'based_id' is an integer
        //    defaultTable.Columns.Add("item_id", typeof(int)); // Assuming 'item_id' is an integer
        //    defaultTable.Columns.Add("ItemCode", typeof(string)); // Assuming 'ItemCode' is a string
        //    defaultTable.Columns.Add("ItemName", typeof(string)); // Assuming 'ItemName' is a string
        //    defaultTable.Columns.Add("qty", typeof(decimal)); // Assuming 'qty' is a decimal
        //    defaultTable.Columns.Add("unit", typeof(string)); // Assuming 'unit' is a string
        //    defaultTable.Columns.Add("unit_price", typeof(decimal)); // Assuming 'unit_price' is a decimal
        //    defaultTable.Columns.Add("percent_discount", typeof(decimal)); // Assuming 'percent_discount' is a decimal
        //    defaultTable.Columns.Add("amount_discounted", typeof(decimal)); // Assuming 'amount_discounted' is a decimal
        //    defaultTable.Columns.Add("net_discount", typeof(decimal)); // Assuming 'net_discount' is a decimal
        //    defaultTable.Columns.Add("net_total", typeof(decimal)); // Assuming 'net_total' is a decimal
        //    defaultTable.Columns.Add("line_total", typeof(decimal)); // Assuming 'line_total' is a decimal

        //    return defaultTable;
        //}

        //private void BindDefaultStructureToDataGridView()
        //{
        //    // Create the default DataTable structure
        //    DataTable defaultTable = CreateDefaultDataTableStructure();

        //    // Bind the DataTable to the DataGridView
        //    dgv_quick_quote_details.DataSource = defaultTable;
        //}



        private void btn_new_version_Click(object sender, EventArgs e)
        {
            pnl_header.Enabled = true;
            pnl_footer.Enabled = true; 

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
                SelectedRow++;
                fetchQuotationDetails();
            }
        }

        private void btn_prev_Click(object sender, EventArgs e)
        {
            if (SelectedRow >= 1)
            {
                SelectedRow--;
                fetchQuotationDetails();
            }
        }




        // 
        // fetching bpi parent and childrens
        //
         
        DataTable bpi_general = new DataTable();
        DataTable bpi_address = new DataTable();
        DataTable bpi_contacts = new DataTable();
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

                if(result != null)
                {
                    string id = "";

                    var isSuccess_baseid = result.TryGetValue("id", out id);

                    
                    var data = await QuotationService.GetBpiId(id);
                    bpi_general = JsonHelper.ToDataTable(data.general);
                    bpi_address = JsonHelper.ToDataTable(data.address);
                    bpi_contacts = JsonHelper.ToDataTable(data.contacts);
                    

                    Panel[] pnl_list = { pnl_header };
                    Helpers.BindControls(pnl_list, bpi_general);
                    Helpers.BindControls(pnl_list, bpi_address);
                    Helpers.BindControls(pnl_list, bpi_contacts);
                    //MessageBox.Show("" + data);
                }
            }
        }
    }



    static class QuickQuoteDGV
    {
        public static int QTY = 5;
        public static int UNIT_MEASURE = 6;
        public static int UNIT_PRICE = 7;
        public static int DISCOUNT = 8;
        public static int DISCOUNT_AMOUNT = 9;
        public static int NET_DISCOUNT = 10;
        public static int NET_AMOUNT = 11;
        public static int LINE_TOTAL = 12;
    }

    class DGVComputation
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
}
