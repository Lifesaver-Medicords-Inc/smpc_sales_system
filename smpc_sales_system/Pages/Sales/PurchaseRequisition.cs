using smpc_app.Services.Helpers;
using smpc_inventory_app.Services.Setup.Item;
using smpc_inventory_app.Services.Setup.Model;
using smpc_sales_app.Pages;
using smpc_sales_app.Services.Helpers;
using smpc_sales_app.Services.Purchasing;
using smpc_sales_app.Services.Sales;
using smpc_sales_system.Services.Sales.Models;
using smpc_sales_system.Services.Setup;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Windows.Forms;

namespace smpc_sales_system.Pages.Sales
{
    public partial class PurchaseRequisition : UserControl
    {
        public PurchaseRequisition()
        {
            InitializeComponent();
        }
        int SelectedRow = 0;
        public DataTable PRList { get; set; } = new DataTable();
        public DataTable PROrderList { get; set; } = new DataTable();
        public DataTable ItemList { get; set; } = new DataTable();
        public DataTable ItemName { get; set; } = new DataTable();
        public DataTable UOM { get; set; } = new DataTable();
        //FETCHERS METHODS
        private async Task fetchItemData()
        {
            var itemData = await ItemService.GetItem();
            ItemList = JsonHelper.ToDataTable(itemData.items);
        }
        private async void fetchPR()
        {
            PurchaseRequisitionList data = await PurchaseRequisitionService.GetPRs();
            PRList = JsonHelper.ToDataTable(data.purchase_requisition);
            PROrderList = JsonHelper.ToDataTable(data.purchasing_purchase_requisition_orders);
            if (data != null)
            {
                bindPR(true);
                CheckStatus();
            }
        }
        private async void fetchItemThings()
        {
            ItemNameModel[] itemNameModels = await ItemNameServices.GetName();
            JArray itemNameJsonArray = JArray.FromObject(itemNameModels);
            ItemName = JsonHelper.ToDataTable(itemNameJsonArray);
        }
        private async void fetchUOM()
        {
            var UOM_data = await UnitOfMeasurementServices.GetAsDatatable();
            UOM = UOM_data;
        }
        //BIND METHOD AND ON LOAD OF PAGE
        private void bindPR(bool isBind = false)
        {
            if (isBind)
            {
                Panel[] pnlList = { pnl_header, pnl_header_2, pnl_body, pnl_footer, pnl_footer_2 };
                Helpers.BindControls(pnlList, PRList, SelectedRow);

                if (pnl_header_2.Controls["txt_doc_no"] is TextBox txtDocNo)
                {
                    if (!txtDocNo.Text.StartsWith("PRQ#"))
                    {
                        txtDocNo.Text = "PRQ#" + txtDocNo.Text;
                    }
                }
                dtp_date_request.Value = Convert.ToDateTime(PRList.Rows[SelectedRow]["date_request"]);
                dtp_date_required.Value = Convert.ToDateTime(PRList.Rows[SelectedRow]["date_required"]);

                DataTable withItemListTwo = this.PROrderList.Clone();
                withItemListTwo.Columns.Add("uom", typeof(string));
                withItemListTwo.Columns.Add("item_name", typeof(string));
                withItemListTwo.Columns.Add("short_desc", typeof(string));
                withItemListTwo.Columns.Add("item_code", typeof(string));

                foreach (DataRow childRow in this.PROrderList.Rows)
                {
                    DataRow newRow = withItemListTwo.NewRow();
                    foreach (DataColumn col in PROrderList.Columns)
                    {
                        newRow[col.ColumnName] = childRow[col.ColumnName];
                    }

                    string itemId = childRow["item_id"].ToString();
                    DataRow[] itemRows = ItemList.Select($"id = '{itemId}'");
                    int iduom = (int)itemRows[0]["unit_of_measure_id"];
                    int iditemname = (int)itemRows[0]["item_name_id"];
                    DataRow[] itemnameRows = ItemName.Select($"id = '{iditemname}'");
                    
                    DataRow[] uom = UOM.Select($"id = '{iduom}'");
                    newRow["uom"] = uom[0]["name"].ToString();
                    newRow["item_name"] = itemnameRows[0]["name"].ToString();
                    newRow["short_desc"] = itemRows[0]["short_desc"].ToString();
                    newRow["item_code"] = itemRows[0]["item_code"].ToString();

                    withItemListTwo.Rows.Add(newRow);
                }
                DataView dataview = new DataView(withItemListTwo);
                dataview.RowFilter = "based_id = '" + this.PRList.Rows[this.SelectedRow]["pr_id"].ToString() + "'";

                dgv_pr_order.DataSource = dataview.ToTable();
            }
        }
        private async void PurchaseRequisition_Load(object sender, EventArgs e)
        {
            await fetchItemData();
            fetchItemThings();
            fetchUOM();
            fetchPR();
        }
        //CLICK METHODS (BUTTONS)
        private void btn_prev_Click(object sender, EventArgs e)
        {
            if (SelectedRow >= 1)
            {
                SelectedRow--;
                fetchPR();
                Helpers.ResetControls(pnl_header);
                Helpers.ResetControls(pnl_body);
                Helpers.ResetControls(pnl_footer);
                Panel[] pnls = { pnl_header, pnl_header_2, pnl_footer, pnl_body, pnl_footer_2 };
                Helpers.ReadOnlyControls(pnls);
                CheckStatus();
            }
        }
        private void btn_next_Click(object sender, EventArgs e)
        {
            int rowCount = PRList.Rows.Count;
            if (SelectedRow < rowCount - 1)
            {
                SelectedRow++;
                fetchPR();
                Helpers.ResetControls(pnl_header);
                Helpers.ResetControls(pnl_body);
                Helpers.ResetControls(pnl_footer);
                Panel[] pnls = { pnl_header, pnl_header_2, pnl_footer, pnl_body, pnl_footer_2 };
                Helpers.ReadOnlyControls(pnls);
                CheckStatus();
            }
        }
        private async void btn_check_Click(object sender, EventArgs e)
        {
            try
            {
                string docIdValue = ((TextBox)pnl_header_2.Controls["txt_doc_no"]).Text;

                docIdValue = docIdValue.StartsWith("PRQ#") ? docIdValue.Substring(4) : docIdValue;

                if (!string.IsNullOrEmpty(docIdValue))
                {
                    var selectedPR = PRList.Select($"doc_no = '{docIdValue}'").FirstOrDefault();

                    if (selectedPR != null)
                    {
                        selectedPR["status"] = "APPROVED";
                        var parentDataHeader = new Dictionary<string, dynamic>
                        {
                            { "doc_no", selectedPR["doc_no"] },
                            { "status", "APPROVED" }
                        };
                        await PurchaseRequisitionService.Update(parentDataHeader);
                        MessageBox.Show("Purchase Requisition status updated to APPROVED.");
                        fetchPR();
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
        private async void btn_new_Click(object sender, EventArgs e)
        {
            Helpers.ResetControls(pnl_header);
            Helpers.ResetControls(pnl_body);
            Helpers.ResetControls(pnl_header_2);
            Helpers.ResetControls(pnl_footer_2);
            Helpers.ResetControls(pnl_footer);
            PRQIncrementer();
            Panel[] pnls = { pnl_header, pnl_footer, pnl_body, pnl_footer_2};
            Helpers.ResetReadOnlyControls(pnls);
            btn_check.Enabled = false;
            dtp_date_request.Value = DateTime.Today;
            dtp_date_request.Enabled = false;
            dtp_date_required.Value = DateTime.Today;
            btn_edit.Enabled = false;
            txt_contact_no.ReadOnly = false;
            dgv_pr_order.Enabled = true;
            dgv_pr_order.DataSource = null; 
            dgv_pr_order.Rows.Clear(); 
            bs_purchase_requisition.DataSource = PROrderList.Clone();
            bindPR(false);
        }
        private async void btn_save_Click(object sender, EventArgs e)
        {
            try
            {
                List<string> missingFields = new List<string>();

                if (string.IsNullOrWhiteSpace(txt_request_by.Text)) missingFields.Add("Request By");
                if (string.IsNullOrWhiteSpace(txt_contact_no.Text)) missingFields.Add("Contact Number");
                if (string.IsNullOrWhiteSpace(txt_department.Text)) missingFields.Add("Department");
                if (missingFields.Count > 0)
                {
                    MessageBox.Show("Please fill in the following fields: " + string.Join(", ", missingFields), "Missing Information", MessageBoxButtons.OK);
                    return;
                }

                if (dgv_pr_order.Rows.Count == 1)
                {
                    MessageBox.Show("Please input an item in the Order to make a Purchase Requisition.", "Input Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                for (int i = 0; i < dgv_pr_order.Rows.Count - 1; i++)
                {
                    DataGridViewRow row = dgv_pr_order.Rows[i];

                    if (row.Cells["itemiddgv"].Value == DBNull.Value || string.IsNullOrEmpty(row.Cells["itemiddgv"].Value.ToString()))
                    {
                        MessageBox.Show("Please select an Item", "Select Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                }

                if (string.IsNullOrEmpty(txt_status.Text))
                {
                    txt_status.Text = "WAITING";
                }

                string docNo = txt_doc_no.Text;
                if (docNo.StartsWith("PRQ#"))
                {
                    docNo = docNo.Substring(4); // Remove the "PRQ#" part
                    txt_doc_no.Text = docNo;
                }
                var parentDataHeader = Helpers.GetControlsValues(pnl_header);
                var parentDataHeader2 = Helpers.GetControlsValues(pnl_header_2);
                var parentDataBody = Helpers.GetControlsValues(pnl_body);
                var parentDataFooter = Helpers.GetControlsValues(pnl_footer);
                var parentDataFooter2 = Helpers.GetControlsValues(pnl_footer_2);

                var parentData = MergeDictionaries(parentDataHeader, parentDataHeader2, parentDataBody, parentDataFooter, parentDataFooter2);
                var dataSource = Helpers.ConvertDataGridViewToDataTable(dgv_pr_order);

                List<Dictionary<string, dynamic>> PROrderList = new List<Dictionary<string, dynamic>>();

                bool isExistingDoc = PRList.Rows.Cast<DataRow>()
                    .Any(row => row["doc_no"].ToString() == docNo);

                foreach (DataRow item in dataSource.Rows)
                {
                    Dictionary<string, object> data = new Dictionary<string, object>();
                    int existingbasedid = 0; // Default value

                    if (dataSource.Rows[0]["basediddgv"] != DBNull.Value)
                    {
                        existingbasedid = int.Parse(dataSource.Rows[0]["basediddgv"].ToString());
                    }
                  
                    if (string.IsNullOrEmpty(item["qtydgv"].ToString()) || int.Parse(item["qtydgv"].ToString()) <= 0)
                    {
                        MessageBox.Show("Quantity must have a valid value greater than 0.", "Input Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    if (isExistingDoc)
                    {
                        if (!string.IsNullOrEmpty(item["pr_order_id"].ToString()) && !string.IsNullOrEmpty(item["basediddgv"].ToString()))
                        {
                            data.Add("pr_order_id", int.Parse(item["pr_order_id"].ToString()));
                            data.Add("based_id", int.Parse(item["basediddgv"].ToString()));
                        }
                        else
                        {
                            data.Add("based_id", existingbasedid);
                        }
                    }
                    data.Add("item_id", int.Parse(item["itemiddgv"].ToString()));
                    data.Add("qty", int.Parse(item["qtydgv"].ToString()));
                    data.Add("item_code", (item["item_code"].ToString()));
                    data.Add("item_description", (item["description"].ToString()));
                    data.Add("status", (item["status"].ToString()));

                    PROrderList.Add(data);
                }

                if (PROrderList != null)
                {
                    List<Dictionary<string, dynamic>> childCollection = new List<Dictionary<string, dynamic>>();
                    foreach (var childData in PROrderList)
                    {
                        childCollection.Add(childData);
                    }

                    parentData["purchasing_purchase_requisition_orders"] = childCollection;

                    if (parentData.ContainsKey("purchasing_purchase_requisition_orders"))
                    {
                        if (isExistingDoc)
                        {
                            foreach (var childData in PROrderList)
                            {
                                if (!childData.ContainsKey("pr_order_id") || !childData.ContainsKey("based_id"))
                                {
                                    await PurchaseRequisitionService.InsertChild(childData);
                                }
                            }
                            await PurchaseRequisitionService.Update(parentData);
                            MessageBox.Show("Data updated successfully");
                        }
                        else
                        {
                            await PurchaseRequisitionService.Insert(parentData);
                            MessageBox.Show("Data added successfully");
                        }
                    }
                    CheckStatus();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message + "\n\n" + "Stack Trace: " + ex.StackTrace);
            }
        }
        private void btn_edit_Click(object sender, EventArgs e)
        {
            Panel[] pnls = { pnl_header, pnl_footer, pnl_body, pnl_footer_2 };
            Helpers.ResetReadOnlyControls(pnls);
            dtp_date_request.Enabled = false;
            btn_edit.Enabled = false;
            txt_contact_no.ReadOnly = false;
            dgv_pr_order.Enabled = true;
        }
        private void btn_search_Click(object sender, EventArgs e)
        {
            PRModal setup = new PRModal(PRList);
            DialogResult r = setup.ShowDialog();
            if (r == DialogResult.OK)
            {
                int result = setup.GetResult();
                if (result != -1)
                {
                    SelectedRow = result;
                    bindPR(true);
                }
            }
        }
        //METHODS TO BE USED IN PURCHASE REQUISITION
        private Dictionary<string, dynamic> MergeDictionaries(params Dictionary<string, dynamic>[] dictionaries)
        {
            var mergedDict = new Dictionary<string, dynamic>();
            foreach (var dict in dictionaries)
            {
                foreach (var kvp in dict)
                {
                    mergedDict[kvp.Key] = kvp.Value;
                }
            }
            return mergedDict;
        }
        private void PRQIncrementer()
        {
            txt_doc_no.Text = "PRQ#" + (PRList.Rows.Count + 1).ToString("D4");
        }
        private void CheckStatus()
        {
            bool isStatusApproved = txt_status.Text == "APPROVED";
            bool isStatusWaiting = txt_status.Text == "WAITING";

            btn_edit.Enabled = !isStatusApproved;
            dgv_pr_order.Enabled = false;
            txt_contact_no.ReadOnly = isStatusApproved || isStatusWaiting;

            if (isStatusApproved)
            {
                Panel[] pnls = { pnl_header, pnl_header_2, pnl_footer, pnl_body, pnl_footer_2 };
                Helpers.ReadOnlyControls(pnls);
            } else if (isStatusWaiting)
            {
                Panel[] pnls = { pnl_header, pnl_header_2, pnl_footer, pnl_body, pnl_footer_2 };
                Helpers.ReadOnlyControls(pnls);
                btn_edit.Enabled = !isStatusApproved;
            }
        }
        //DGV ACTIONS
        private async void dgv_pr_order_UserDeletingRow(object sender, DataGridViewRowCancelEventArgs e)
        {
            if (e.Row != null)
            {
                var prOrderId = e.Row.Cells["pr_order_id"].Value;
                
                if (prOrderId == DBNull.Value || Convert.ToInt32(prOrderId) == 0)
                {
                    MessageBox.Show("Item removed.", "Deleted", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    DialogResult result = MessageBox.Show("Do you want to delete this row with PR_Order_ID: " + prOrderId.ToString() + "?",
                                                         "Delete Confirmation", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                    if (result == DialogResult.No)
                    {
                        e.Cancel = true;
                    }
                    else
                    {
                        var data = new Dictionary<string, dynamic>
                        {
                            { "pr_order_id", Convert.ToInt64(prOrderId) }
                        };

                        var response =  await PurchaseRequisitionService.DeleteChild(data);
                        if (response.Success)
                        {
                            MessageBox.Show("Item has been deleted from the Order.", "Deleted", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                    }
                }
            }
        }
        private void dgv_pr_order_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.ColumnIndex == 0)
            {
                ItemModal itemModal = new ItemModal(ItemList);
                DialogResult r = itemModal.ShowDialog();

                if (r == DialogResult.OK)
                {
                    int selectedIndex = itemModal.GetResult(); // Get the index from the modal

                    if (selectedIndex >= 0 && selectedIndex < ItemList.Rows.Count) // Ensure the index is valid
                    {
                        DataRow selectedItem = ItemList.Rows[selectedIndex];

                        int ID = (int)selectedItem["item_name_id"];
                        DataRow[] name = ItemName.Select($"id = '{ID}'");
                        string itemname = name[0]["name"].ToString();

                        int UOMID = (int)selectedItem["unit_of_measure_id"];
                        DataRow[] UOMname = UOM.Select($"id = '{UOMID}'");
                        string unitname = UOMname[0]["name"].ToString();

                        this.dgv_pr_order.Rows[e.RowIndex].Cells[8].Value = selectedItem["id"].ToString();
                        this.dgv_pr_order.Rows[e.RowIndex].Cells[0].Value = selectedItem["item_code"].ToString();
                        this.dgv_pr_order.Rows[e.RowIndex].Cells[1].Value = itemname;
                        this.dgv_pr_order.Rows[e.RowIndex].Cells[2].Value = selectedItem["short_desc"].ToString();
                        this.dgv_pr_order.Rows[e.RowIndex].Cells[4].Value = unitname;
                    }
                    else
                    {
                        MessageBox.Show("Invalid selection", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

    }

}
