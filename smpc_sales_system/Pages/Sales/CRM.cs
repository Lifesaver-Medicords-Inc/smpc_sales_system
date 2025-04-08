using smpc_app.Services.Helpers;
using smpc_sales_app.Pages;
using smpc_sales_system.Services.Sales;
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
    public partial class CRM : UserControl
    {
        ApiResponseModel response;
        public CRM()
        {
            InitializeComponent();
        }
        public DataTable crm { get; set; } = new DataTable();
        public DataTable crmtable { get; set; } = new DataTable();
        //FETCHERS AND BINDER
        private async Task fetchCRM()
        {
            crm = await CRMService.GetAsDatatable();
            crmtable = await CRMService.GetCRM();
            if (crm != null)
            {
                bindQuotation(true);
            }
        }
        private void bindQuotation(bool isBind = false)
        {
            if (isBind)
            {
                dgv_branch.DataSource = crm;
            }
        }

        private async void CRM_Load(object sender, EventArgs e)
        {
            await fetchCRM();
        }

        private void dgv_branch_CellContentDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (dgv_branch.Columns[e.ColumnIndex].Name == "date")
            {
                if (e.RowIndex >= 0)
                {
                    int id = (int)dgv_branch.Rows[e.RowIndex].Cells["based_id"].Value;
                    string branch = (string)dgv_branch.Rows[e.RowIndex].Cells["branch_name"].Value;
                    DataRow[] filteredRows = crmtable.Select($"based_id = {id}");
                    DataTable filteredTable = crmtable.Clone();
                    foreach (var row in filteredRows)
                    {
                        filteredTable.ImportRow(row);
                    }
                    CRMModal itemModal = new CRMModal(filteredTable, branch);
                    itemModal.StartPosition = FormStartPosition.CenterParent;
                    DialogResult r = itemModal.ShowDialog();
                }
            }
        }

        private async void dgv_branch_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            try
            {
                bool isInsert = false;
                var dataSource = Helpers.ConvertDataGridViewToDataTable(dgv_branch);
                // List to hold existing data in the DataGridView (already populated)
                List<Dictionary<string, dynamic>> crm = new List<Dictionary<string, dynamic>>();

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
                    crm.Add(existingData);
                }
                DataRow editedItem = dataSource.Rows[e.RowIndex];
                Dictionary<string, object> data = new Dictionary<string, object>();

                string[] requiredColumns = { "tag", "branch_name", "number", "name", "email", "date", "remark", "based_id", "crm_id" };
                foreach (var column in requiredColumns)
                {
                    string columnValue = editedItem[column].ToString();
                    data[column] = string.IsNullOrWhiteSpace(columnValue) ? null : columnValue;
                }

                // Check and process based_id
                if (editedItem.Table.Columns.Contains("based_id"))
                {
                    var basedIdValue = editedItem["based_id"].ToString();
                    if (uint.TryParse(basedIdValue, out uint basedIdUint))
                    {
                        // Convert to int (ensure the value is within int's range)
                        if (basedIdUint <= int.MaxValue)
                        {
                            data["based_id"] = (int)basedIdUint;  // Safely cast to int
                        }
                        else
                        {
                            // Handle the case where the uint value exceeds the int range (optional)
                            MessageBox.Show("based_id value is too large to fit into an int.");
                            return;  // Exit or handle accordingly
                        }
                    }
                    else
                    {
                        // If the conversion fails, you could handle it here (e.g., set to null or show an error message)
                        data["based_id"] = null; // Or handle the case where the value is invalid
                    }
                }

                // Check and process crm_id
                if (editedItem.Table.Columns.Contains("crm_id"))
                {
                    var crmIdValue = editedItem["crm_id"].ToString();
                    if (uint.TryParse(crmIdValue, out uint crmIdUint))
                    {
                        // Convert to int (ensure the value is within int's range)
                        if (crmIdUint <= int.MaxValue)
                        {
                            data["crm_id"] = (int)crmIdUint;  // Safely cast to int
                        }
                        else
                        {
                            // Handle the case where the uint value exceeds the int range (optional)
                            MessageBox.Show("crm_id value is too large to fit into an int.");
                            return;  // Exit or handle accordingly
                        }
                    }
                    else
                    {
                        // If the conversion fails, you could handle it here (e.g., set to null or show an error message)
                        data["crm_id"] = null; // Or handle the case where the value is invalid
                    }
                }

                // Get the value of the 'date' column
                string dateValue = editedItem["date"].ToString();
                string currentDate = DateTime.Now.ToString("dd-MM-yyyy");

                // Check if the 'date' column has a value
                if (string.IsNullOrWhiteSpace(dateValue))
                {
                    // No value, set the current date
                    editedItem["date"] = currentDate;
                }
                else
                {
                    // If the value doesn't match today's date, update it to the current date
                    if (dateValue != currentDate)
                    {
                        editedItem["date"] = currentDate;
                        isInsert = true;
                    }
                }

                var crmId = data.ContainsKey("crm_id") ? data["crm_id"]?.ToString().Trim() : null;

                if (crmId == "0" || isInsert)
                {
                    data["crm_id"] = 0;
                    data["date"] = DateTime.Now.ToString("dd-MM-yyyy");
                    response = await CRMService.Insert(data);
                    if (response != null && response.Success)
                    {
                        MessageBox.Show("Edit Successfully saved");
                        fetchCRM();
                    }
                    else
                    {
                        MessageBox.Show("Failed to save quotation. Please try againsadsa.");
                    }
                }
                else
                {
                    response = await CRMService.Update(data);
                    if (response != null && response.Success)
                    {
                        MessageBox.Show("Edit Successfully updated");
                        fetchCRM();
                    }
                    else
                    {
                        MessageBox.Show("Failed to update quotation. Please try again.");
                    }
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message);
            }
        }

        private void txt_search_TextChanged(object sender, EventArgs e)
        {
            string searchval = txt_search.Text.ToString();
            var data = Helpers.FilterDataTable(crm, searchval, "tag", "branch_name", "number", "name", "email", "date", "remark");
            if (string.IsNullOrEmpty(searchval))
            {
                bindQuotation(true);
            }
            else
            {
                dgv_branch.DataSource = data;
            }
        }
    }
}
