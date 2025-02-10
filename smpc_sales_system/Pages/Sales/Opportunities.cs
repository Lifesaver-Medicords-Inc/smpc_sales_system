using smpc_app.Services.Helpers;
using System.Linq;
using smpc_sales_app.Pages.Sales;
using smpc_sales_app.Services.Helpers;
using smpc_sales_app.Services.Sales;
using smpc_sales_system.Services.Sales;
using smpc_sales_system.Services.Sales.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;

using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace smpc_sales_system.Pages.Sales
{
    public partial class Opportunities : UserControl
    {
        public Opportunities()
        {
            InitializeComponent();
        }
        public DataTable transactionList { get; set; } = new DataTable();
        public DataTable opportunities { get; set; } = new DataTable();
        private async void fetchQuotationDetails()
        {
            transactionList = await OpportunityService.GetAsDatatable(); 

            //childList = JsonHelper.ToDataTable(data.SalesQuotationQuick);

            if (transactionList != null)
            {
                bindQuotation(true);
            }
        }

        private void bindQuotation(bool isBind = false)
        {
            if (isBind)
            {
                //DataView dataview = new DataView(this.transactionList);

                dgv_sales_opportunities.DataSource = transactionList;

                //foreach (DataRow row in this.transactionList.Rows)
                //{
                //    if (row["document_no"] != DBNull.Value)
                //    {
                //        string documentNo = row["document_no"].ToString();

                //        if (!documentNo.StartsWith("Q#"))
                //        {
                //            row["document_no"] = "Q#" + documentNo;
                //        }
                //    }
                //}

                //dgv_sales_opportunities.DataSource = transactionList;

                //// Hide other columns if they exist
                //foreach (DataGridViewColumn column in dgv_sales_opportunities.Columns)
                //{
                //    if (column.Name != "document_no" && column.Name != "customer_name" && column.Name != "date" &&
                //        column.Name != "tag" && column.Name != "project_name" && column.Name != "client_req" &&
                //        column.Name != "value" && column.Name != "last_update" && column.Name != "stage" &&
                //        column.Name != "status" && column.Name != "special_deal")
                //    {
                //        column.Visible = false;
                //    }
                //}
            }
        }

        private void dgv_sales_opportunities_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                string documentNo = dgv_sales_opportunities.Rows[e.RowIndex].Cells["document_no"].Value.ToString();
                if (documentNo.StartsWith("Q#"))
                {
                    documentNo = documentNo.Substring(2);
                }
                Quotation quotationPage = new Quotation(documentNo);

                this.Parent.Controls.Add(quotationPage);

                this.Hide();
            }
        }
        private async void Opportunities_Load(object sender, EventArgs e)
        {
            fetchQuotationDetails();
        }
        ApiResponseModel response;
        private async void dgv_sales_opportunities_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            try
            {
                // Convert DataGridView to DataTable to get the current state of the rows
                var dataSource = Helpers.ConvertDataGridViewToDataTable(dgv_sales_opportunities);

                // List to hold existing data in the DataGridView (already populated)
                List<Dictionary<string, dynamic>> opportunity = new List<Dictionary<string, dynamic>>();

                // Populate the 'opportunity' list with current rows data
                foreach (DataRow item in dataSource.Rows)
                {
                    Dictionary<string, object> existingData = new Dictionary<string, object>();

                    for (int i = 0; i < item.ItemArray.Length; i++)
                    {
                        string columnName = dataSource.Columns[i].ColumnName;
                        string columnValue = item[i].ToString();

                        existingData[columnName] = string.IsNullOrWhiteSpace(columnValue) ? null : columnValue;
                    }

                    opportunity.Add(existingData);
                }

                DataRow editedItem = dataSource.Rows[e.RowIndex];
                Dictionary<string, object> data = new Dictionary<string, object>();


                string[] requiredColumns = { "tag", "document_no", "client_req", "stage", "status", "special_deal" };

                foreach (var column in requiredColumns)
                {
                    // Get the value for each column
                    string columnValue = editedItem[column].ToString();

                    // Nullify the value if it is empty or whitespace
                    data[column] = string.IsNullOrWhiteSpace(columnValue) ? null : columnValue;

                    // Optionally, print the column name and value to the console
                    Console.WriteLine($"{column}: {columnValue}");
                }

                var documentNo = data.ContainsKey("document_no") ? data["document_no"] : null;

                if (documentNo != null)
                {
                    var existingRow = opportunity.FirstOrDefault(d => d.ContainsKey("document_no") && d["document_no"]?.ToString() == documentNo.ToString());

                    

                    if (existingRow != null)
                    {
                        // If the row already exists (matching document_no), update the record
                        response = await OpportunityService.Update(data);
                    }
                    else if (existingRow == null)
                    {
                        // If no matching document_no is found, insert a new record
                        response = await OpportunityService.Insert(data);
                    }

                    // Check if the operation was successful
                    if (response != null && response.Success)
                    {
                        MessageBox.Show("Quotation Successfully saved");
                        fetchQuotationDetails();  // Refresh the quotation details
                    }
                    else
                    {
                        MessageBox.Show("Failed to save quotation. Please try again.");
                    }
                }
                else
                {
                    MessageBox.Show("Document No. cannot be null.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message);
            }
        }

    }
}
