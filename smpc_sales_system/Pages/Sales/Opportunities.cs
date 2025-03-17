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
using smpc_sales_app.Pages;

namespace smpc_sales_system.Pages.Sales
{
    public delegate void HandleShowForm(string tabTitle, Control control);

    public partial class Opportunities : UserControl
    {
        private Layout layoutForm;
        public Opportunities()
        {
            InitializeComponent();
 
            //showQuotation += Layout.Instance.showForm;
        }
        public DataTable transactionList { get; set; } = new DataTable();
        public DataTable opportunities { get; set; } = new DataTable();
        private async Task fetchQuotationDetails()
        {
            transactionList = await OpportunityService.GetAsDatatable();

            //childList = JsonHelper.ToDataTable(data.SalesQuotationQuick);
            AddCombinedColumn();
            if (transactionList != null)
            {
                bindQuotation(true);
            }
        }

        private void bindQuotation(bool isBind = false)
        {
            if (isBind)
            {
                dgv_sales_opportunities.DataSource = transactionList;
            }
        }
        private void AddCombinedColumn()
        {
            if (!transactionList.Columns.Contains("combined_column"))
            {
                transactionList.Columns.Add("combined_column", typeof(string));

                // Iterate through each row and populate the new column with combined document_no and version_no
                foreach (DataRow row in transactionList.Rows)
                {
                    string documentNo = row["document_no"].ToString();
                    string versionNo = row["version_no"].ToString();

                    // Combine the document_no and version_no as needed (e.g., "document_no-version_no")
                    row["combined_column"] = $"Q#{documentNo}-{versionNo}";
                }
            }
        }


        private event HandleShowForm showQuotation;
        private void dgv_sales_opportunities_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (dgv_sales_opportunities.Columns[e.ColumnIndex].Name == "prospectref") 
            {
                if (e.RowIndex >= 0)
                {
                    string documentNo = dgv_sales_opportunities.Rows[e.RowIndex].Cells["document_no"].Value.ToString();
                    string versionNo = dgv_sales_opportunities.Rows[e.RowIndex].Cells["version_no"].Value.ToString();
                    if (documentNo.StartsWith("Q#"))
                    {
                        documentNo = documentNo.Substring(2);
                    }
                    Quotation quotationPage = new Quotation(documentNo, versionNo);

                    this.Parent.Controls.Add(quotationPage);

                    //showQuotation.Invoke("Sales Quotation", quotationPage);
                    this.Hide();
                }
            }
        }
        private async void Opportunities_Load(object sender, EventArgs e)
        {
            await fetchQuotationDetails();
            
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

                // Iterate through the DataTable and populate the opportunity list
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

                // Get the edited item from the DataGridView
                DataRow editedItem = dataSource.Rows[e.RowIndex];
                Dictionary<string, object> data = new Dictionary<string, object>();

                // Define the columns to be processed
                string[] requiredColumns = { "tag", "document_no", "client_req", "stage", "status", "special_deal", "last_update", "version_no" };

                // Populate the data dictionary with the required columns' values
                foreach (var column in requiredColumns)
                {
                    string columnValue = editedItem[column].ToString();
                    data[column] = string.IsNullOrWhiteSpace(columnValue) ? null : columnValue;
                }

                // Extract document_no and version_no from the edited row
                var documentNo = data.ContainsKey("document_no") ? data["document_no"] : null;
                var versionNo = data.ContainsKey("version_no") ? data["version_no"] : null;

                if (documentNo != null && versionNo != null)
                {
                    // Check if a row with the same document_no and version_no exists in the opportunity list
                    var matchingRow = opportunity.FirstOrDefault(d =>
                        d.ContainsKey("document_no") && d.ContainsKey("version_no") &&
                        d["document_no"]?.ToString().Trim() == documentNo.ToString().Trim() &&
                        d["version_no"]?.ToString().Trim() == versionNo.ToString().Trim());

                    if (matchingRow != null)
                    {
                        var opportunityId = matchingRow.ContainsKey("Opportunity_id") ? matchingRow["Opportunity_id"]?.ToString().Trim() : null;

                        if (opportunityId == "0")
                        {
                            response = await OpportunityService.Insert(data);
                            if (response != null && response.Success)
                            {
                                MessageBox.Show("Opportunity Successfully saved");
                                fetchQuotationDetails();
                            }
                            else
                            {
                                MessageBox.Show("Failed to save quotation. Please try again.");
                            }
                        }
                        else
                        {
                            response = await OpportunityService.Update(data);
                            if (response != null && response.Success)
                            {
                                MessageBox.Show("Opportunity Successfully updated");
                                fetchQuotationDetails();
                            }
                            else
                            {
                                MessageBox.Show("Failed to update quotation. Please try again.");
                            }
                        }
                    }
                    else
                    {
                        MessageBox.Show("Document No. and version_no cannot be null.");
                    }
                }
                else
                {
                    MessageBox.Show("Document No. and Version No. cannot be null.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message);
            }
        }
        private DateTimePicker dateTimePicker;

        private void dgv_sales_opportunities_CellBeginEdit(object sender, DataGridViewCellCancelEventArgs e)
        {
            if (dgv_sales_opportunities.Columns[e.ColumnIndex].Name == "last_update") 
            {
                if (dateTimePicker != null)
                {
                    dgv_sales_opportunities.Controls.Remove(dateTimePicker);
                }

                dateTimePicker = new DateTimePicker();
                dateTimePicker.Format = DateTimePickerFormat.Short; 
                dateTimePicker.Size = new Size(dgv_sales_opportunities.Columns[e.ColumnIndex].Width, 22);

                object cellValue = dgv_sales_opportunities.Rows[e.RowIndex].Cells[e.ColumnIndex].Value;

                if (cellValue != DBNull.Value && cellValue != null)
                {
                    DateTime parsedDate;
                    if (DateTime.TryParse(cellValue.ToString(), out parsedDate))
                    {
                        dateTimePicker.Value = parsedDate.Date; 
                    }
                    else
                    {
                        dateTimePicker.Value = DateTime.Now.Date;
                    }
                }
                else
                {
                    dateTimePicker.Value = DateTime.Now.Date;
                }

                dgv_sales_opportunities.Controls.Add(dateTimePicker);

                Rectangle rect = dgv_sales_opportunities.GetCellDisplayRectangle(e.ColumnIndex, e.RowIndex, true);
                dateTimePicker.Location = new Point(rect.Left, rect.Top);

                dateTimePicker.Visible = true;

                // Set event for DateTimePicker value change
                dateTimePicker.CloseUp += (sender1, e1) =>
                {
                    // Save the selected date in the DataGridView cell, removing the time part
                    dgv_sales_opportunities.Rows[e.RowIndex].Cells[e.ColumnIndex].Value = dateTimePicker.Value.Date;

                };
            }
        }

        private void txt_search_TextChanged(object sender, EventArgs e)
        {
            string searchval = txt_search.Text.ToString();
            var data = Helpers.FilterDataTable(transactionList, searchval, "tag", "document_no", "stage", "status", "special_deal", "branch_name", "project_name", "last_update");
            dgv_sales_opportunities.DataSource = data;
        }
    }
}
