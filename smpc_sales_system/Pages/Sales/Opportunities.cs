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
    
    
    public partial class Opportunities : UserControl
    {
        public delegate void TriggerNewFormDelegate(string title, Control control);
        public event TriggerNewFormDelegate TriggerNewForm;
        private DateTimePicker dateTimePicker;
        ApiResponseModel response;
        public Opportunities()
        {
            InitializeComponent();
        }
        public DataTable transactionList { get; set; } = new DataTable();
        public DataTable opportunities { get; set; } = new DataTable();
        //FETCHERS AND BINDER
        private async Task fetchQuotationDetails()
        {
            transactionList = await OpportunityService.GetAsDatatable();
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
                var latestDocuments = transactionList.AsEnumerable()
                .GroupBy(row => row.Field<string>("document_no"))
                .Select(group => group.OrderByDescending(row => row.Field<string>("date")).First())
                .ToList();
                DataTable filteredTable = transactionList.Clone();
                foreach (var row in latestDocuments)
                {
                    filteredTable.ImportRow(row);
                }
                dgv_sales_opportunities.DataSource = filteredTable;
                CheckStatus();
            }
        }
        private async void Opportunities_Load(object sender, EventArgs e)
        {
            await fetchQuotationDetails();
        }
        //DGV ACTIONS
        

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
                    string title = "Q#"+documentNo;
                    TriggerNewForm?.Invoke(title, quotationPage);

                }
            }
            if (dgv_sales_opportunities.Columns[e.ColumnIndex].Name == "final_ref_no")
            {
                bool finalRefNo = dgv_sales_opportunities.Rows[e.RowIndex].Cells["final_ref_no"].Value != null && (bool)dgv_sales_opportunities.Rows[e.RowIndex].Cells["final_ref_no"].Value;
                if (finalRefNo)
                {
                    string documentNo = dgv_sales_opportunities.Rows[e.RowIndex].Cells["document_no"].Value.ToString();
                    string versionNo = dgv_sales_opportunities.Rows[e.RowIndex].Cells["version_no"].Value.ToString();

                    if (documentNo.StartsWith("Q#"))
                    {
                        documentNo = documentNo.Substring(2);
                    }
                    Quotation quotationPage = new Quotation(documentNo, versionNo, true);
                    this.Parent.Controls.Add(quotationPage);
                    this.Hide();
                }
            }
        }
        private async void dgv_sales_opportunities_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            try
            {
                if (dateTimePicker != null && dgv_sales_opportunities.Controls.Contains(dateTimePicker))
                {
                    dgv_sales_opportunities.Controls.Remove(dateTimePicker);
                    dateTimePicker.Visible = false;
                }
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
                DataRow editedItem = dataSource.Rows[e.RowIndex];
                Dictionary<string, object> data = new Dictionary<string, object>();

                string[] requiredColumns = { "tag", "document_no", "client_req", "stage", "status", "special_deal", "last_update", "version_no" };
                foreach (var column in requiredColumns)
                {
                    string columnValue = editedItem[column].ToString();
                    data[column] = string.IsNullOrWhiteSpace(columnValue) ? null : columnValue;
                }

                var documentNo = data.ContainsKey("document_no") ? data["document_no"] : null;
                var versionNo = data.ContainsKey("version_no") ? data["version_no"] : null;

                if (documentNo != null && versionNo != null)
                {
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
        private int currentRowIndex = -1;
        private int currentColumnIndex = -1;

        private void dgv_sales_opportunities_CellBeginEdit(object sender, DataGridViewCellCancelEventArgs e)
        {
            if (dgv_sales_opportunities.Columns[e.ColumnIndex].Name == "last_update")
            {
                e.Cancel = true;  // This prevents the default editing behavior

                if (dateTimePicker != null)
                {
                    dgv_sales_opportunities.Controls.Remove(dateTimePicker);
                }

                currentRowIndex = e.RowIndex;
                currentColumnIndex = e.ColumnIndex;

                dateTimePicker = new DateTimePicker();
                dateTimePicker.Format = DateTimePickerFormat.Custom;
                dateTimePicker.CustomFormat = "dd/MM/yyyy"; // Allow custom date input format

                // Enable the user to type manually
                dateTimePicker.ShowUpDown = false; // This will show a text input and allow free typing

                // Set the size of the DateTimePicker
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

                // Handle the CloseUp event when the user selects a date
                dateTimePicker.CloseUp += async (sender1, e1) =>
                {
                    dgv_sales_opportunities.Rows[currentRowIndex].Cells[currentColumnIndex].Value = dateTimePicker.Value.Date;
                    dgv_sales_opportunities.Controls.Remove(dateTimePicker);
                    await Task.Delay(10); // Optional: small delay to ensure UI updates
                    dgv_sales_opportunities_CellEndEdit(sender, new DataGridViewCellEventArgs(currentColumnIndex, currentRowIndex));
                };

                // Handle the CellLeave event to update and remove the DateTimePicker when the user clicks away
                dgv_sales_opportunities.CellLeave += (sender1, e1) =>
                {
                    if (dateTimePicker != null && dgv_sales_opportunities.Controls.Contains(dateTimePicker))
                    {
                        if (e1.RowIndex == currentRowIndex && e1.ColumnIndex == currentColumnIndex)
                        {
                            dgv_sales_opportunities.Rows[currentRowIndex].Cells[currentColumnIndex].Value = dateTimePicker.Value.Date;
                            dgv_sales_opportunities.Controls.Remove(dateTimePicker);
                            dgv_sales_opportunities_CellEndEdit(sender, new DataGridViewCellEventArgs(currentColumnIndex, currentRowIndex));
                        }
                    }
                };


            }
        }
        private void dgv_sales_opportunities_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex >= 0 && dgv_sales_opportunities.Columns[e.ColumnIndex].Name == "final_ref_no")
            {
                if (e.Value != null && e.Value is bool)
                {
                    bool boolValue = (bool)e.Value;
                    var nameValue = dgv_sales_opportunities.Rows[e.RowIndex].Cells["prospectref"].Value;
                    string name = nameValue.ToString();
                    e.Value = boolValue ? "F" + name : "";
                }
            }
        }
        //METHODS TO BE USED FOR OPPORTUNITIES
        private void AddCombinedColumn()
        {
            if (!transactionList.Columns.Contains("combined_column"))
            {
                transactionList.Columns.Add("combined_column", typeof(string));
                foreach (DataRow row in transactionList.Rows)
                {
                    string documentNo = row["document_no"].ToString();
                    string versionNo = row["version_no"].ToString();
                    row["combined_column"] = $"Q#{documentNo}-{versionNo}";
                }
            }
        }
        private void txt_search_TextChanged(object sender, EventArgs e)
        {
            string searchval = txt_search.Text.ToString();
            var data = Helpers.FilterDataTable(transactionList, searchval, "tag", "document_no", "stage", "status", "special_deal", "branch_name", "project_name", "last_update");
            if (string.IsNullOrEmpty(searchval))
            {
                bindQuotation(true);
            }
            else
            {
                dgv_sales_opportunities.DataSource = data;
                CheckStatus();
            }
        }
        private void CheckStatus()
        {
            foreach (DataGridViewRow row in dgv_sales_opportunities.Rows)
            {
                if (!row.IsNewRow)
                {
                    var statusCell = row.Cells["status"];
                    if (statusCell is DataGridViewComboBoxCell comboBoxCell)
                    {
                        var statusValue = comboBoxCell.Value?.ToString();
                        if (statusValue == "LOST")
                        {
                            row.DefaultCellStyle.BackColor = Color.FromArgb(255, 204, 204); // Lighter red
                        }
                        else
                        {
                            row.DefaultCellStyle.BackColor = Color.White;
                        }
                    }

                    // If you want to keep other column styles intact (like custom fonts, colors), don't change them here.
                }
            }
        }


        private void label2_Click(object sender, EventArgs e)
        {

        }

        private void dgv_sales_opportunities_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void panel2_Paint(object sender, PaintEventArgs e)
        {

        }

        private void panel1_Paint(object sender, PaintEventArgs e)
        {

        }

    }
}