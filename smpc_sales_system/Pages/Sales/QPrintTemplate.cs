using smpc_app.Services.Helpers;
using smpc_sales_app.Services.Helpers;
using smpc_sales_app.Services.Sales;
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

namespace smpc_sales_system.Pages.Sales
{
    public partial class QPrintTemplate : UserControl
    {
        private string documentNo;
        public QPrintTemplate(string documentNo = null)
        {
            InitializeComponent();
            fetchBpiData();
            fetchItemData();
            this.documentNo = documentNo;
        }
        public DataTable allTransactionList { get; set; } = new DataTable();
        public DataTable transactionList { get; set; } = new DataTable();
        public DataTable childList { get; set; } = new DataTable();
        public DataTable ItemList { get; set; } = new DataTable();
        private DataTable bpi_dt = new DataTable();
        private DataTable bpi_general = new DataTable();
        private DataTable bpi_address = new DataTable();
        private DataTable bpi_contacts = new DataTable();
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
        private async Task fetchQuotationDetailsByDocumentNo(string documentNo)
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

                // If filtered data exists, bind it to the DataGridView
                if (filteredSalesQuotation.Any() || filteredSalesQuotationQuick.Any())
                {
                    bindQuotation(documentNo, true);
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
                }

                Helpers.BindControls(pnlList, HeaderList);

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

                dgv_quote.DataSource = dataview;
            }
        }
        private void QPrintTemplate_Load(object sender, EventArgs e)
        {
            fetchQuotationDetailsByDocumentNo(documentNo);
        }

        private void btn_print_Click(object sender, EventArgs e)
        {
            string[] img = GetDataFromDataGridView(dgv_quote, "img");
            string[] desc = GetDataFromDataGridView(dgv_quote, "desc");
            string[] qtys = GetDataFromDataGridView(dgv_quote, "qtys");
            string[] unitprice = GetDataFromDataGridView(dgv_quote, "unitprice");
            string[] percentdiscount = GetDataFromDataGridView(dgv_quote, "percentdiscount");
            string[] amount = GetDataFromDataGridView(dgv_quote, "amount");

            string docno = txt_document_no.Text;
            string date = txt_date.Text;
            string company = txt_branch_name.Text;
            string address = txt_ship_to.Text;
            string receiver = txt_receiver.Text;
            string exec = txt_sales_exec.Text;

            string subtotal = txt_net_amount_due.Text;
            string adddiscount = txt_add_discount.Text;
            string cashdiscount = txt_cash_discount.Text;
            string grandtotal = txt_grand_total.Text;

            string inclusion = rtxt_inclusion.Text;
            string exclusion = rtxt_exclusions.Text;
            string terms = rtxt_terms.Text;

            // Pass the data to the QuotationPrintModal
            QuotationPrintModal printModal = new QuotationPrintModal(
            img, desc, qtys, unitprice, percentdiscount, amount,
            docno, date, company, address, receiver, exec,
            subtotal, adddiscount, cashdiscount, grandtotal,
            inclusion, exclusion, terms
        );
            printModal.ShowDialog();
        }

        private string[] GetDataFromDataGridView(DataGridView dgv, string columnName)
        {
            return dgv.Rows.Cast<DataGridViewRow>()
                            .Where(row => !row.IsNewRow && row.Cells[columnName].Value != null)
                            .Select(row => row.Cells[columnName].Value.ToString())
                            .ToArray();
        }
    }
}
