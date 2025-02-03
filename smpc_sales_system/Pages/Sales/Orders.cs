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

namespace smpc_sales_app.Pages.Sales
{
    public partial class Orders : UserControl
    {
        int SelectedRow = 0;
        public Orders()
        {
            InitializeComponent();
        }
        public DataTable OrderList { get; set; } = new DataTable();
        public DataTable DetailsList { get; set; } = new DataTable();
        public DataTable transactionList { get; set; } = new DataTable();
        public DataTable childList { get; set; } = new DataTable();
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

        private void bindQuotation(bool isBind = false)
        {
            if (isBind)
            {
                Panel[] pnlList = { pnl_header, pnl_footer };
                Helpers.BindControls(pnlList, transactionList, SelectedRow);
                //dgv_quick_quote_details.DataSource = dataView;
                // dgv_quick_quote_details.DataSource = childList;
                
                    DataView dataview = new DataView(this.childList);
                dataview.RowFilter = "based_id = '" + this.transactionList.Rows[this.SelectedRow]["id"].ToString() + "'";
                //dgv_quick_quotes_show.DataSource = dataview;

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
            if (dgv_order_sales.Columns.Contains("line_total"))
            {
                foreach (DataGridViewRow row in dgv_order_sales.Rows)
                {
                    if (row.Cells["line_total"].Value != null)
                    {
                        decimal totalPrice;
                        if (decimal.TryParse(row.Cells["line_total"].Value.ToString(), out totalPrice))
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

        private void Orders_Load(object sender, EventArgs e)
        {
            FetchData();
            fetchQuotationDetails();
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
                if (cmb_payment_terms.SelectedItem == null)
                {
                    missingFields.Add("Payment Terms");
                }
                if (cmb_ship_type.SelectedItem == null)
                {
                    missingFields.Add("Shipping Type");
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

                    data.Add("qty", item["qty"].ToString());
                    data.Add("item_code", item["unit_code"].ToString());
                    data.Add("item_description", item["item_description"].ToString());
                    data.Add("delivery_preference", item["delivery_preference"].ToString());
                    data.Add("list_price", float.Parse(item["unit_price"].ToString()));
                    data.Add("total_price", float.Parse(item["line_total"].ToString()));
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



                    // trims the Q# from the input
                    if (parentData.ContainsKey("doc") && parentData["doc"] is string documentNo)
                    {
                        parentData["doc"] = documentNo.StartsWith("SO#")
                            ? documentNo.Substring(3) // Remove "Q#"
                            : documentNo; // Keep as is if "Q#" is not present
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
    }
}
