using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Linq;
using smpc_app.Data;
using smpc_app.Services.Helpers;
using smpc_inventory_app.Pages;
using smpc_inventory_app.Services.Setup.Model.Item;
using smpc_sales_app.Data;
using smpc_sales_app.Services.Helpers;
using smpc_sales_app.Services.Sales;
using smpc_sales_app.Utils;
using smpc_sales_system.Models;
using smpc_sales_system.Pages;
using smpc_sales_system.Pages.Sales;
using smpc_sales_system.Services.Sales;
using smpc_sales_system.Services.Sales.Models;
using smpc_sales_system.Services.Setup;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net.WebSockets;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Security.Policy;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using WebSocketSharp;

namespace smpc_sales_app.Pages.Sales
{
    public partial class Quotation : UserControl
    {
        private ItemService itemService = new ItemService();

        private int SelectedRow = 0;
        private string quotation_quick_id;
        private string documentNo;
        private string versionNo;
        private string subVersionNo;
        private bool isFinalized;
        private bool isNewRecord;

        private ClientWebSocket _websocket;
        private CancellationTokenSource _cancelTokenSource;

        public Quotation(string documentNo = null, string version_no = null, string sub_version_no = null, bool is_finalized = false)
        {
            InitializeComponent();

            cmb_warranty.Text = "1 year";
            //KRIS: NEED ITONG DALAWA KAPAG MAY VERSION_NO NA PERO GINAGAMIT KO NA RIN GANYAN
            this.documentNo = documentNo;
            this.versionNo = version_no;
            this.versionNo = sub_version_no;
            this.isFinalized = is_finalized;

            // websocket related 
            _websocket = new ClientWebSocket();
            _cancelTokenSource = new CancellationTokenSource();
        }

        private async void UpdateProjectConditions(object sender, EventArgs e)
        {
            if (IsEdit)
            {
                if (tabControl2.SelectedTab.Controls[0] is ItemSetUC currentControl)
                {
                    Dictionary<string, dynamic> data = new Dictionary<string, dynamic>();
                    var updatedConditionsData = currentControl.GetAdvancedConditionsData();
                    data["sales_project_content_advanced_condition"] = updatedConditionsData;

                    var isSuccess = await ProjectService.UpdateConditions(data);

                    if (isSuccess.Success)
                    {
                        MessageBox.Show(isSuccess.message);
                    }
                }
            }
        }

        private async void UpdateProjectContent(object sender, EventArgs e)
        {
            if (IsEdit)
            {
                if (tabControl2.SelectedTab.Controls[0] is ItemSetUC currentControl)
                {
                    Dictionary<string, dynamic> data = new Dictionary<string, dynamic>();
                    var updatedContentsData = currentControl.GetProjectContentsData();
                    data["sales_project_content"] = updatedContentsData;

                    var isSuccess = await ProjectService.UpdateContents(data);

                    if (isSuccess.Success)
                    {
                        MessageBox.Show(isSuccess.message);
                    }

                }
            }
        }

        private async void ItemSet_DataChanged(object sender, EventArgs e)
        {
            if (IsEdit)
            {
                if (tabControl2.SelectedTab.Controls[0] is ItemSetUC currentControl)
                {
                    Dictionary<string, dynamic> data = new Dictionary<string, dynamic>();
                    var updatedConditionsData = currentControl.GetAdvancedConditionsData();
                    data["branch"] = "Sales";
                    data["project_id"] = this.selectedProjectID;
                    data["sales_project_content_advanced_condition"] = updatedConditionsData;

                    await SendMessageAsync(data);

                }
            }
        }

        private async void Content_DataChanged(object sender, EventArgs e)
        {
            if (IsEdit)
            {
                if (tabControl2.SelectedTab.Controls[0] is ItemSetUC currentControl)
                {
                    Dictionary<string, dynamic> data = new Dictionary<string, dynamic>();
                    var updatedContentsData = currentControl.GetProjectContentsData();
                    data["branch"] = "Sales";
                    data["project_id"] = this.selectedProjectID;
                    data["sales_project_content"] = updatedContentsData;

                    await SendMessageAsync(data);

                }
            }
        }

        private async void ItemChanged(object sender, EventArgs e)
        {
            if (IsEdit)
            {
                if (tabControl2.SelectedTab.Controls[0] is ItemSetUC currentControl)
                {
                    var item_data = currentControl.GetProjectItems();

                    if (item_data != null && item_data.ContainsKey("items_id") && Convert.ToInt32(item_data["items_id"]) != 0)
                    {
                        MessageBox.Show("update");
                        var isSuccess = await ProjectService.UpdateProjectItems(item_data);

                        if (isSuccess.Success)
                        {
                            MessageBox.Show($"Item Updated successfully");
                        }
                    }

                    if (item_data != null && item_data.ContainsKey("items_id") && Convert.ToInt32(item_data["items_id"]) == 0)
                    {
                        MessageBox.Show("add");
                        item_data["items_id"] = "";
                        item_data["based_id"] = CurrentProjectItemBasedID;
                        var isSuccess = await ProjectService.InsertItems(item_data);

                        if (isSuccess.Success)
                        {
                            MessageBox.Show($"Item Added successfully");
                        }
                    }
                }
            }
        }

        private async void Cell_DataChanged(object sender, EventArgs e)
        {
            if (IsEdit)
            {
                if (tabControl2.SelectedTab.Controls[0] is ItemSetUC currentControl)
                {
                    var item_data = currentControl.GetProjectItems();
                    if (item_data != null && item_data.ContainsKey("sales_project_items"))
                    {
                        var salesProjectItems = (List<SalesProjectItems>)item_data["sales_project_items"];

                        var itemsToInsert = salesProjectItems
                            .Where(item => item.items_id == 0)
                            .ToList();

                        var itemsToUpdate = salesProjectItems
                            .Where(item => item.items_id != 0)
                            .ToList();

                        if (itemsToUpdate.Any())
                        {
                            // prepare update
                            item_data["sales_project_items"] = itemsToUpdate;
                            var updateResult = await ProjectService.UpdateProjectItems(item_data);
                            if (updateResult.Success)
                                MessageBox.Show("Updated successfully");
                            else
                                MessageBox.Show(updateResult.message);
                        }

                        if (itemsToInsert.Any())
                        {
                            foreach (var item in itemsToInsert)
                            {
                                item.based_id = CurrentProjectItemBasedID;
                            }

                            item_data["sales_project_items"] = itemsToInsert;
                            var insertResult = await ProjectService.InsertItems(item_data);

                            if (insertResult.Success)
                                MessageBox.Show("Added successfully");
                            else
                                MessageBox.Show(insertResult.message);
                        }
                    }
                }
            }
        }

        private async void Cell_WiringChanged(object sender, EventArgs e)
        {
            if (IsEdit)
            {
                if (tabControl2.SelectedTab.Controls[0] is ItemSetUC currentControl)
                {
                    var item_data = currentControl.GetProjectWiringData();

                    if (item_data != null && item_data.ContainsKey("sales_project_wiring"))
                    {
                        var salesProjectWirings = (List<SalesWiringModel>)item_data["sales_project_wiring"];

                        var itemsToInsert = salesProjectWirings
                            .Where(item => item.id == 0)
                            .ToList();

                        var itemsToUpdate = salesProjectWirings
                            .Where(item => item.id != 0)
                            .ToList();

                        if (itemsToUpdate.Any())
                        {
                            item_data["sales_project_wiring"] = itemsToUpdate;
                            var updateResult = await ProjectService.UpdateWiring(item_data);

                            if (updateResult.Success)
                                MessageBox.Show("Wiring Updated Successfully");
                            else
                                MessageBox.Show(updateResult.message);
                        }

                        if (itemsToInsert.Any())
                        {
                            item_data["sales_project_wiring"] = itemsToInsert;
                            foreach (var item in itemsToInsert)
                            {
                                item.id = null;
                                item.based_id = CurrentProjectItemBasedID;
                            }

                            item_data["sales_project_items"] = itemsToInsert;
                            var insertResult = await ProjectService.InsertWiring(item_data);

                            if (insertResult.Success)
                                MessageBox.Show("Wiring Added successfully");
                            else
                                MessageBox.Show(insertResult.message);


                        }
                    }
                }
            }
        }

        private async void Cell_ClickedUC(object sender, EventArgs e)
        {
            if (tabControl2.SelectedTab.Controls[0] is ItemSetUC currentControl)
            {
                int index = currentControl.GetIndex();
                int insertionIndex = index + 1;
                //HandleItemSelectionClick(insertionIndex, currentControl.DgvProjectItems);
            }
        }
        private async void Cell_EditedUC(object sender, EventArgs e)
        {
            decimal gross = 0, vat = 0, net = 0, percent = 0, cash_disc = 0, net_amount = 0, total_amount = 0;

            foreach (TabPage tab in tabControl2.TabPages)
            {
                if (tab.Controls.Count > 0 && tab.Controls[0] is ItemSetUC currentControl) // Check if control exists
                {
                    var data = currentControl.ProjectComputationLoop() ?? new Dictionary<string, object>();

                    if (data.TryGetValue("gross_sales", out var grossVal)) gross += Convert.ToDecimal(grossVal);
                    if (data.TryGetValue("vat_amount", out var vatVal)) vat += Convert.ToDecimal(vatVal);
                    if (data.TryGetValue("net_sales", out var netVal)) net += Convert.ToDecimal(netVal);
                    //if (data.TryGetValue("percent_discount", out var percentVal)) percent += Convert.ToDecimal(percentVal);
                    if (data.TryGetValue("cash_discount", out var cashDiscVal)) cash_disc += Convert.ToDecimal(cashDiscVal);
                    if (data.TryGetValue("net_amount_due", out var netAmountVal)) net_amount += Convert.ToDecimal(netAmountVal);
                    if (data.TryGetValue("total_amount_due", out var totalAmountVal)) total_amount += Convert.ToDecimal(totalAmountVal);

                }
            }

            txt_gross_sales.Text = Helpers.MoneyFormatDecimal(gross);
            txt_vat_amount.Text = Helpers.MoneyFormatDecimal(vat);
            txt_net_sales.Text = Helpers.MoneyFormatDecimal(net);
            txt_percent_discount.Text = Helpers.MoneyFormatDecimal(percent);
            txt_cash_discount.Text = Helpers.MoneyFormatDecimal(cash_disc);
            txt_net_amount_due.Text = Helpers.MoneyFormatDecimal(net_amount);
            txt_total_amount_due.Text = Helpers.MoneyFormatDecimal(total_amount);
        }
        private async void Button_ClickedUC(object sender, EventArgs e)
        {
            var dt = await ProjectTemplatesService.GetProjectTemplates();
            DataTable templates = JsonHelper.ToDataTable(dt.sales_project_template_child);

            if (tabControl2.SelectedTab.Controls[0] is ItemSetUC currentControl)
            {
                TemplateSelectionModal temp = new TemplateSelectionModal();
                DialogResult r = temp.ShowDialog();

                if (r == DialogResult.OK)
                {
                    Dictionary<string, dynamic> result = temp.GetResult();

                    if (result != null)
                    {
                        dynamic id = "";

                        result.TryGetValue("template_id", out id);

                        var template_data = Helpers.FilterDataTable(templates, id, "based_id");

                        currentControl.SetProjectItemsData(template_data);
                    }
                }
            }
        }

        // Fix ambiguous reference to WebSocketState by fully qualifying the type

        // In all places where you use "_websocket.State == WebSocketState.Open", 
        // change to "_websocket.State == System.Net.WebSockets.WebSocketState.Open"

        // Example fix for the first occurrence:
        public async void ConnectToWebSocket(string branch, string projectid)
        {
            Uri serverUri = new Uri($"ws://localhost:3000/api/ws/setup/test?branch={branch}&projectid={projectid}");
            await _websocket.ConnectAsync(serverUri, _cancelTokenSource.Token);
            MessageBox.Show("Connected!");
            CacheProjectData();
            ReceiveDataAsync();
        }
        private void CacheProjectData()
        {
            if (tabControl2.SelectedTab.Controls[0] is ItemSetUC currentControl)
            {
                CacheData.Cached_Advanced_Conditions_Data = currentControl.GetAdvancedConditionsData();
                CacheData.Cached_Content_Data = currentControl.GetProjectContentsData();
            }
        }
        private async void ReceiveDataAsync()
        {
            byte[] buffer = new byte[100 * 1024 * 1024];
            List<byte> messageBuffer = new List<byte>();
            while (_websocket.State == System.Net.WebSockets.WebSocketState.Open)
            {
                try
                {
                    WebSocketReceiveResult result = await _websocket.ReceiveAsync(new ArraySegment<byte>(buffer), _cancelTokenSource.Token);

                    if (result.MessageType == WebSocketMessageType.Text)
                    {

                        // receives the fragmented data
                        messageBuffer.AddRange(buffer.Take(result.Count));
                        Console.WriteLine(messageBuffer);

                        // once the data is complete proceed to decode the bytes to string and convert to JSON object then convert to table to use as datasource;
                        if (result.EndOfMessage)
                        {
                            string completeData = Encoding.UTF8.GetString(messageBuffer.ToArray());
                            Console.WriteLine($"Payload size: {messageBuffer.Count} bytes");
                            messageBuffer.Clear();


                            IsEdit = false;

                            var json = JToken.Parse(completeData);

                            Invoke(new Action(() =>
                            {
                                fetchSalesProjectRT(json);

                            }));

                            CacheProjectData();

                            IsEdit = true;

                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error receiving message: {ex.Message}");
                }
            }

        }
        private void setAdvancedConditionsData()
        {

        }
        private async Task SendMessageAsync(Dictionary<string, dynamic> data)
        {
            if (_websocket.State == System.Net.WebSockets.WebSocketState.Open)
            {
                try
                {
                    string jsonString = JsonConvert.SerializeObject(data);

                    byte[] messageBytes = Encoding.UTF8.GetBytes(jsonString);
                    ArraySegment<byte> messageSegment = new ArraySegment<byte>(messageBytes);

                    // Send the message
                    await _websocket.SendAsync(messageSegment, WebSocketMessageType.Text, true, _cancelTokenSource.Token);

                    //MessageBox.Show("Message sent!");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error sending message: {ex.Message}");
                }
            }
        }
        private void tabControl_SelectedIndexChanged(object sender, EventArgs e)
        {
            string activeTab = tabControl.SelectedTab.Text;
            if (activeTab == "quick_tab")
            {
                quick_tab.Height = 507;
            }
        }
        private void btn_quick_quote_Click(object sender, EventArgs e)
        {
            //1028, 2354
            this.btn_quick_quote.BackColor = Color.FromArgb(255, 128, 128);
            this.btn_project.BackColor = Color.White;

            this.tabControl.SelectedIndex = 0;
            this.tabControl.Height = 600;
            this.Size = new Size(1386 - 80, 950);
            isProject = false;
            IsEdit = false;
        }
        private void btn_project_Click(object sender, EventArgs e)
        {

            if (_websocket != null && _websocket.State == System.Net.WebSockets.WebSocketState.Open)
            {
                _websocket.Dispose();
            }
            _cancelTokenSource = new CancellationTokenSource();


            //1028, 2354
            this.btn_quick_quote.BackColor = Color.White;
            this.btn_project.BackColor = Color.FromArgb(255, 128, 128);

            this.tabControl.SelectedIndex = 1;
            this.tabControl.Height = 600;
            this.Size = new Size(1386 - 80, 2354);

            // set state 
            isProject = true;

            UC_History h = new UC_History();
            flowLayoutPanel2.Controls.Add(h);

            foreach (Control ctrl in h.Controls)
            {
                ctrl.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
            }


            fetchSalesProject();
        }
        private void setProjectMultiplier()
        {
            DataTable dt = new DataTable();
            dt.Columns.Add("Multiplier", typeof(string));

            string[] defaultMultipliers = { "0.7 * 0.9 * 0.95", "0.7 * 0.9 * 0.9 * 0.95", "0.7 * 0.9 * 0.7 * 0.95", "/.7", "/.75", "0.3 * 0.035", "0.035" };

            foreach (string value in defaultMultipliers)
            {
                DataRow row = dt.NewRow();
                row["Multiplier"] = value;
                dt.Rows.Add(row);
            }

            bs_project_multipliers.DataSource = dt;
            dgv_project_multiplier.DataSource = bs_project_multipliers;

            if (dgv_project_multiplier.Rows.Count > 0)
            {
                DataGridViewCellEventArgs e = new DataGridViewCellEventArgs(0, 0);
                dgv_project_multiplier_CellEndEdit(dgv_project_multiplier, e);
            }
        }

        public DataTable allTransactionList { get; set; } = new DataTable();
        public DataTable transactionList { get; set; } = new DataTable();
        public DataTable childList { get; set; } = new DataTable();
        public DataTable selectedImageList { get; set; } = new DataTable();
        public DataTable ItemList { get; set; } = new DataTable();
        public DataTable ItemAdditionalSpecs { get; set; } = new DataTable();
        public DataTable ImageList { get; set; } = new DataTable();
        public DataTable BomHead { get; set; } = new DataTable();
        public DataTable BomDetails { get; set; } = new DataTable();
        public DataTable Company { get; set; } = new DataTable();

        private async Task fetchItemData()
        {
            var itemData = await ItemService.GetItem();
            var bomData = await ProjectService.GetBom();
            var companyData = await CompanyService.GetAsDatatable();

            if (itemData == null || bomData == null)
                return;

            ItemList = JsonHelper.ToDataTable(itemData.items);
            ItemAdditionalSpecs = JsonHelper.ToDataTable(itemData.additionalspecs);
            ImageList = JsonHelper.ToDataTable(itemData.ItemImages);
            BomHead = JsonHelper.ToDataTable(bomData.bom_head);
            BomDetails = JsonHelper.ToDataTable(bomData.bom_details);
            Company = companyData;

            //Apply Quotation
            quotationTerms();
        }

        private async Task fetchBpiData()
        {
            Bpi_Class bpi_data = await QuotationService.GetBpiCustomers();

            if (bpi_data == null)
                return;

            bpi_dt = JsonHelper.ToDataTable(bpi_data.bpi);
            bpi_general = JsonHelper.ToDataTable(bpi_data.general);
            bpi_address = JsonHelper.ToDataTable(bpi_data.address);
            bpi_address2 = JsonHelper.ToDataTable(bpi_data.address);
            bpi_contacts = JsonHelper.ToDataTable(bpi_data.contacts);
            bpi_items = JsonHelper.ToDataTable(bpi_data.items);
        }
        SalesQuotationList data;
        private async Task fetchQuotationDetails()
        {
            Panel[] panels = { pnl_header, pnl_footer };
            Helpers.ReadOnlyControls(panels);

            //pnl_header.Enabled = false;
            //pnl_footer.Enabled = false;
            toolstrip_quotation.Enabled = false;
            dgv_quick_quote_details.Enabled = false;

            data = await QuotationService.GetQuotations();

            if (data != null && data.SalesQuotation != null && data.SalesQuotation.Any())
            {
                // Get latest quotation by version and subversion
                var latestQuotations = data.SalesQuotation
                    .GroupBy(q => q.document_no)
                    .Select(group => group
                    .OrderByDescending(q => q.version_no)
                    .ThenByDescending(q => q.sub_version_no)
                    .First())
                    .ToList();

                transactionList = JsonHelper.ToDataTable(latestQuotations);
                allTransactionList = JsonHelper.ToDataTable(data.SalesQuotation);
                childList = JsonHelper.ToDataTable(data.SalesQuotationQuick);
                selectedImageList = JsonHelper.ToDataTable(data.SalesQuotationSelectedImages);

                dgv_quick_quote_details.ReadOnly = true;
                dgv_quick_quote_details.Enabled = true;

                await Task.Delay(2000); // optional wait
                bind(true);
            }
            else
            {
                MessageBox.Show("Please create a new data!");

                Helpers.ResetReadOnlyControls(panels);
                //pnl_header.Enabled = true;
                //pnl_footer.Enabled = true;

                Helpers.ResetReadOnlyControls(panels);
            }

            toolstrip_quotation.Enabled = true;
        }
        public DataTable dt_multiplier { get; set; }
        public DataTable dt_content { get; set; }
        public DataTable dt_advanced_conditions { get; set; }
        public DataTable dt_items { get; set; }
        public DataTable dt_wiring { get; set; }

        public int CurrentProjectItemBasedID { get; set; }

        private int selectedProject = 0;
        private string selectedProjectID = "0";
        private async void fetchSalesProject()
        {
            SalesProjectList data = await ProjectService.GetProjects();

            DataTable project_quote = new DataTable();

            project_quote = JsonHelper.ToDataTable(data.SalesQuotation);

            if (data == null || project_quote == null || project_quote.Rows.Count <= 0)
                return;

            if (data == null || (data.sales_project_item_set == null || !data.sales_project_item_set.Any()))
            {
                MessageBox.Show("No project data found. Creating a new entry.", "Empty Data", MessageBoxButtons.OK, MessageBoxIcon.Information);

                // Get the last index (before the add new tab)
                var lastIndex = this.tabControl2.TabCount - 1;
                // Create a new TabPage
                TabPage newTab = new TabPage("New Project");
                // Create an instance of ItemSetUC
                ItemSetUC UC = new ItemSetUC
                {
                    Dock = DockStyle.Fill,
                    BackColor = Color.White
                };

                // Attach event handlers
                UC.ButtonClicked += Button_ClickedUC;
                UC.DataChangedConditions += ItemSet_DataChanged;
                UC.DataChangedContent += Content_DataChanged;

                UC.UpdateProjectConditions += UpdateProjectConditions;
                UC.UpdateProjectContent += UpdateProjectContent;


                UC.CellChangedProject += Cell_DataChanged;
                UC.CellClicked += Cell_ClickedUC;

                // Add the UserControl to the new tab
                newTab.Controls.Add(UC);

                // Insert the new tab before the last tab
                this.tabControl2.TabPages.Insert(lastIndex, newTab);

                // Select the newly added tab
                this.tabControl2.SelectedIndex = lastIndex;

                return;
            }


            List<SalesProjectItemSet> fetchedTabs = data.sales_project_item_set;


            dt_multiplier = JsonHelper.ToDataTable(data.sales_project_multiplier);
            dt_content = JsonHelper.ToDataTable(data.sales_project_content);
            dt_advanced_conditions = JsonHelper.ToDataTable(data.sales_project_content_advanced_condition);
            dt_items = JsonHelper.ToDataTable(data.sales_project_items);
            dt_wiring = JsonHelper.ToDataTable(data.sales_project_wiring);

            //Helpers.BindControls(pnls, dt2, selectedProject);s

            string selectedSalesQuotationId = null;// project_quote.Rows[0]["id"].ToString();
            this.selectedProjectID = selectedSalesQuotationId;
            txt_project_name.Text = project_quote.Rows[0]["project_name"].ToString();

            //DataView dataview = new DataView(dt_multiplier);
            //dataview.RowFilter = "based_id = '" + this.allTransactionList.Rows[this.selectedProject]["id"].ToString() + "'";
            //dgv_project_multiplier.DataSource = dataview;

            tabControl2.TabPages.Clear();

            var filteredtabs = fetchedTabs.Where(tab => tab.based_id.ToString() == selectedSalesQuotationId).ToList();
            foreach (var tab in filteredtabs)
            {
                TabPage newTab = new TabPage(tab.tab_number);

                ItemSetUC UC = new ItemSetUC
                {
                    Dock = DockStyle.Fill
                };


                //UC.DataChangedConditions += ItemSet_DataChanged;
                //UC.DataChangedContent += Content_DataChanged;
                //UC.CellChangedProject += Cell_DataChanged;
                //UC.ButtonClicked += Button_ClickedUC;
                UC.ButtonClicked += Button_ClickedUC;
                UC.DataChangedConditions += ItemSet_DataChanged;
                UC.DataChangedContent += Content_DataChanged;
                UC.CellChangedProject += Cell_DataChanged;
                UC.CellChangedWiring += Cell_WiringChanged;
                UC.CellClicked += Cell_ClickedUC;
                UC.CellEdited += Cell_EditedUC;

                //UC.ItemChanged += ItemChanged;
                UC.FinalTxtBoxClicked += FinalTxtBoxClicked;
                UC.SetUnitsOfMeasure(CacheData.UoM, CacheData.UoM);

                DataView multipliers = new DataView(dt_multiplier);
                multipliers.RowFilter = $"based_id = '{tab.based_id}'";
                //bs_project_multipliers.DataSource = multipliers.ToTable();
                dgv_project_multiplier.DataSource = multipliers;

                DataView contentView = new DataView(dt_content);
                contentView.RowFilter = $"based_id = '{tab.itemset_id}'";

                DataView conditionsView = new DataView(dt_advanced_conditions);
                conditionsView.RowFilter = $"based_id = '{tab.itemset_id}'";

                DataView itemView = new DataView(dt_items);
                itemView.RowFilter = $"based_id = '{tab.itemset_id}'";

                DataView wiringView = new DataView(dt_wiring);
                wiringView.RowFilter = $"based_id = '{tab.itemset_id}'";

                CurrentProjectItemBasedID = tab.itemset_id;

                UC.SetAdvancedPanelData(conditionsView.ToTable());
                UC.SetContentsPanelData(contentView.ToTable());
                UC.SetFetchedItemData(itemView.ToTable());
                UC.SetProjectWiring(wiringView.ToTable());


                newTab.Controls.Add(UC);
                tabControl2.TabPages.Add(newTab);
            }

            TabPage addNewTab = new TabPage("+");
            tabControl2.TabPages.Add(addNewTab);

            fetchProjectMultipliers();
            //ConnectToWebSocket("Sales", selectedSalesQuotationId);
        }
        private void fetchSalesProjectRT(JToken data)
        {
            if (tabControl2.SelectedTab.Controls[0] is ItemSetUC currentControl)
            {
                // for datagridview
                if (data is JArray)
                {
                    // converts the JToken back to string
                    var token_string = data.ToString();
                    var dict_array = JsonConvert.DeserializeObject<Dictionary<string, JArray>>(token_string);

                    if (dict_array.TryGetValue("sales_project_items", out JArray token))
                    {
                        var items_dt = JsonHelper.ToDataTable(token);
                        currentControl.SetProjectItemsData(items_dt);
                    }

                }

                //for single json data not encapsulated into arrays
                if (data is JObject)
                {
                    var token_string = data.ToString();
                    var dict_object = JsonConvert.DeserializeObject<Dictionary<string, JObject>>(token_string);

                    if (dict_object.TryGetValue("sales_project_content_advanced_condition", out JObject token))
                    {
                        var parsed_token = Helpers.GetChangedEntries(token, CacheData.Cached_Advanced_Conditions_Data);
                        var conditions_dt = JsonHelper.ToDataTableFromJObject(parsed_token);
                        currentControl.SetAdvancedPanelData(conditions_dt);
                    }

                    if (dict_object.TryGetValue("sales_project_content", out JObject CONTENT))
                    {
                        var parsed_token = Helpers.GetChangedEntries(CONTENT, CacheData.Cached_Advanced_Conditions_Data);
                        var conditions_dt = JsonHelper.ToDataTableFromJObject(parsed_token);
                        currentControl.SetContentsPanelData(conditions_dt);
                    }

                }
            }
        }

        private void LoadMultipliers(DataTable dt)
        {
            dgv_project_multiplier.DataSource = dt;
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
                return;
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
                selectedImageList = JsonHelper.ToDataTable(filteredSalesQuotationQuick);

                // Enable the panels and controls as needed

                Panel[] panels = { pnl_header, pnl_footer };
                Helpers.ResetReadOnlyControls(panels);
                //pnl_header.Enabled = true;
                //pnl_footer.Enabled = true;
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

        // For creating new quotation or version
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

        private void btn_save_Click(object sender, EventArgs e)
        {

            if (!IsEdit)
            {
                if (!isProject)
                {
                    IsQuickQuote();
                }
                else
                {
                    IsProject();
                }
            }

        }
        public SalesProjectItemSet GetProjectItemSet()
        {
            TabPage selectedTab = this.tabControl2.SelectedTab;
            if (selectedTab != null && !selectedTab.Text.StartsWith("+"))
            {
                return new SalesProjectItemSet
                {
                    tab_number = selectedTab.Text
                };
            }
            return new SalesProjectItemSet();
        }
        private async void IsProject()
        {
            Panel[] pnl_list = { pnl_header, pnl_footer, pnl_project_name };
            var pnl_quotation = Helpers.GetControlsValues(pnl_list);

            pnl_quotation["project_name"] = txt_project_name.Text.Trim();


            //
            // Checker if project name is null or its empty it would not proceed.
            if (string.IsNullOrWhiteSpace(txt_project_name.Text))
            {
                MessageBox.Show("Please enter a valid project name. The project name cannot be empty or consist only of spaces.",
                                "Invalid Project Name", MessageBoxButtons.OK, MessageBoxIcon.Error);

                txt_project_name.Focus();
                return;
            }

            var multiplierSource = Helpers.ConvertDataGridViewToDataTable(dgv_project_multiplier);

            // multiplier child
            List<SalesProjectMultiplier> multipliers = new List<SalesProjectMultiplier>();
            foreach (DataRow item in multiplierSource.Rows)
            {
                SalesProjectMultiplier mult = new SalesProjectMultiplier
                {
                    brand = item[0].ToString(),
                    component = item[1].ToString(),
                    description = item[2].ToString(),
                    multiplier = item[3].ToString(),

                };
                multipliers.Add(mult);
            }

            ItemSetUC UC = new ItemSetUC();
            TabPage selectedTab = this.tabControl2.SelectedTab;

            if (selectedTab != null && selectedTab.Controls.Count > 0)
            {
                var selectedControl = selectedTab.Controls[0] as ItemSetUC;

                if (selectedControl != null)
                {
                    pnl_quotation["sales_project_content_advanced_condition"] = selectedControl.GetAdvancedConditionsData();
                    pnl_quotation["sales_project_content"] = selectedControl.GetProjectContentsData();
                    pnl_quotation["sales_project_item_set"] = GetProjectItemSet();
                    pnl_quotation["sales_project_multiplier"] = multipliers;
                    pnl_quotation["sales_project_items"] = selectedControl.GetProjectItems()["sales_project_items"];
                    pnl_quotation["sales_project_wiring"] = selectedControl.GetProjectWiringData()["sales_project_wiring"];

                    if (pnl_quotation.ContainsKey("customer_id") && pnl_quotation["customer_id"] is string customerIdStr)
                    {
                        if (int.TryParse(customerIdStr, out int customerId))
                        {
                            pnl_quotation["customer_id"] = customerId;
                        }
                        else
                        {
                            MessageBox.Show("Invalid customer ID");
                            return;
                        }
                    }

                    var post = await ProjectService.Insert(pnl_quotation);
                    if (post.Success)
                    {
                        MessageBox.Show(post.message);
                    }
                    else
                    {
                        MessageBox.Show(post.message);
                    }
                }
            }
        }
        private void removeColumn()
        {
            // Make sure to handle the removal from the bottom to avoid index shifting issues
            for (int i = dgv_quick_quote_details.Rows.Count - 1; i >= 0; i--)
            {

                if (!dgv_quick_quote_details.Rows[i].IsNewRow)
                {
                    var cellValue = dgv_quick_quote_details.Rows[i].Cells["quick_item_id"].Value;

                    if (cellValue != null && int.TryParse(cellValue.ToString(), out int itemId) && itemId == 0)
                    {
                        dgv_quick_quote_details.Rows.RemoveAt(i);
                    }
                }
            }
        }
        private async void IsQuickQuote()
        {
            try
            {
                Panel[] pnl_list = { pnl_header, pnl_footer };
                var parentData = Helpers.GetControlsValuesV2(pnl_list);
                //var childData = Helpers.GetControlsValues(pnl_list);
                int id;
                bool isParsed = int.TryParse(txt_id.Text, out id);
                var bill_to_id = int.Parse(cmb_bill_to.SelectedValue.ToString());
                var ship_to_id = int.Parse(cmb_ship_to.SelectedValue.ToString());

                parentData["id"] = id;

                if (isNewRecord || isSubVersion)
                {
                    parentData.Remove("id");
                }

                parentData["bill_to_id"] = bill_to_id;
                parentData["ship_to_id"] = ship_to_id;


                var dataSource = Helpers.ConvertDataGridViewToDataTable(dgv_quick_quote_details);
                var newDatasource = Helpers.ConvertDataTableToStringTable(dataSource);
                List<Dictionary<string, dynamic>> quickQuoteList = new List<Dictionary<string, dynamic>>();

                for (int i = 0; i < newDatasource.Rows.Count - 1; i++)
                {
                    DataRow item = newDatasource.Rows[i];

                    int itemId = int.TryParse(item["quick_item_id"].ToString(), out int ival) ? ival : 0;

                    if (itemId == 0)
                        continue;

                    Dictionary<string, object> data = new Dictionary<string, object>();

                    data.Add("item_id", itemId);
                    data.Add("bom_id", int.TryParse(item["quick_bom_id"].ToString(), out int bomid) ? bomid : 0);
                    data.Add("components", item["quick_item_code"]);
                    data.Add("model", item["quick_item_name"]);
                    data.Add("qty", int.TryParse(item["quick_qty"].ToString(), out int val) ? val : 0);
                    data.Add("unit_of_measure", item["quick_unit_of_measure"]);
                    data.Add("unit_price", decimal.Parse((item["quick_unit_price"].ToString())));
                    data.Add("percent_discount", item["quick_discount"].ToString());
                    data.Add("net_discount", decimal.Parse(Helpers.GetCleanedPriceValue(item["quick_net_discount"].ToString())));
                    data.Add("net_total", decimal.Parse(Helpers.GetCleanedPriceValue(item["quick_net_total"].ToString())));
                    data.Add("line_total", decimal.Parse(Helpers.GetCleanedPriceValue(item["quick_line_total"].ToString())));
                    data.Add("reference_code", item["reference_code"].ToString());
                    data.Add("short_description", item["short_description"].ToString());
                    data.Add("man_days", int.TryParse(item["man_days"].ToString(), out int manday) ? manday : 0);
                    data.Add("labor_rate", decimal.TryParse(item["labor_rate"].ToString(), out decimal laborday) ? laborday : 0);
                quickQuoteList.Add(data);
                }

                if (quickQuoteList != null)
                {
                    List<Dictionary<string, dynamic>> childCollection = new List<Dictionary<string, dynamic>>();

                    // loops thru the items
                    foreach (var childData in quickQuoteList)
                    {
                        //childCollection.Add(childData);

                        // ensure it's a dictionary (clone or cast if necessary)
                        var dict = new Dictionary<string, dynamic>(childData);

                        // attach quick_selected_image here
                        dict["quick_selected_image"] = SelectedImages;

                        childCollection.Add(dict);

                    }

                    // trims the Q# from the input
                    if (parentData.ContainsKey("document_no") && parentData["document_no"] is string documentNo)
                    {
                        parentData["document_no"] = documentNo.StartsWith("Q#")
                            ? documentNo.Substring(2)
                            : documentNo;
                    }

                    //
                    // MAKE A HELPER THAT CONVERT ID TO INT 
                    if (!Helpers.ConvertToIntIfString(parentData, "customer_id") ||
                        !Helpers.ConvertToIntIfString(parentData, "payment_terms_id") ||
                        !Helpers.ConvertToIntIfString(parentData, "ship_type_id"))
                    {
                        return;
                    }

                    parentData["sales_quotation_quick"] = childCollection;
                    //parentData["additional_discounted_amount"] = decimal.Parse(txt_additional_discount.Text);
                    //parentData["cash_discount"] = decimal.Parse(txt_cash_discount.Text);


                    if (parentData.ContainsKey("sales_quotation_quick"))
                    {

                        var isSuccess = await QuotationService.Insert(parentData);

                        if (isSuccess.Success)
                        {
                            //// this should await a response in the future if the response is success proceed to create if not notify the user
                            Helpers.ResetControls(pnl_header);
                            //Helpers.ResetControls(pnl_footer);
                            //dgv_quick_quote_details.DataSource = this.childList.Clone();
                            //dgv_quick_quotes_show.Visible = true;
                            //dgv_quick_quotes_show.Enabled = false;
                            //toolstrip_quotation.Enabled = true;

                            Panel[] panel = { pnl_header, pnl_footer };

                            ResetControls(pnl_footer);

                            // IF SUCCESS

                            MessageBox.Show("Quotation Successfully saved");
                            await fetchQuotationDetails();

                            SetNewFormMode(false);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("ERROR ERROR " + ex);
            }
        }

        private void getItemShortDescription(int id)
        {
            var matchingItem = ItemAdditionalSpecs.AsEnumerable()
                .FirstOrDefault(item => item["id"].ToString() == id.ToString());

            if (matchingItem != null)
            {
                txt_long_description.Text = matchingItem["long_description"].ToString();

            }
            else
            {
                Console.WriteLine("Item not found.");
            }
        }
        private int selectedItem;

        private void dgv_quick_quote_details_CellClick(object sender, DataGridViewCellEventArgs e)
        {

            try
            {
                // Skip header clicks
                if (e.RowIndex < 0 || e.ColumnIndex < 0)
                    return;

                // Display long and short description
                //if (dgv_quick_quote_details.Rows[e.RowIndex].Cells["quick_item_name"].Value != null &&
                //    !string.IsNullOrEmpty(dgv_quick_quote_details.Rows[e.RowIndex].Cells["quick_item_name"].Value.ToString()))
                //{
                //    var itemID = int.Parse(dgv_quick_quote_details.Rows[e.RowIndex].Cells["quick_item_id"].Value.ToString());
                //    getItemShortDescription(itemID);
                //}
                // Image Column
                if (dgv_quick_quote_details.Columns[e.ColumnIndex].Name == "quick_images")
                {
                    var row = dgv_quick_quote_details.Rows[e.RowIndex];
                    var cellQuickId = row.Cells["quick_id"].Value?.ToString();
                    var cellItemId = row.Cells["quick_item_id"].Value?.ToString();

                    cellQuickId = string.IsNullOrWhiteSpace(cellQuickId) ? "0" : cellQuickId;


                    if (int.TryParse(cellQuickId, out int quickId) &&
                        int.TryParse(cellItemId, out int itemId))
                    {
                        HandleItemImageSelectionClick(quickId, itemId);
                    }
                }
                // Components Column
                if (dgv_quick_quote_details.Columns[e.ColumnIndex].Name == "quick_item_code")
                {
                    HandleItemSelectionClick(e.RowIndex, dgv_quick_quote_details);
                }

                //Model Column
                if (dgv_quick_quote_details.Columns[e.ColumnIndex].Name == "quick_item_name" && e.RowIndex >= 0)
                {
                    if (dgv_quick_quote_details.Rows[e.RowIndex].IsNewRow)
                        return;
                    if (dgv_quick_quote_details.Rows[e.RowIndex].Cells["quick_item_name"].Value == null || string.IsNullOrWhiteSpace(dgv_quick_quote_details.Rows[e.RowIndex].Cells["quick_item_name"].Value.ToString())
                        || dgv_quick_quote_details.Rows[e.RowIndex].Cells["quick_item_id"].Value == null || string.IsNullOrWhiteSpace(dgv_quick_quote_details.Rows[e.RowIndex].Cells["quick_item_id"].Value.ToString()))
                        return;

                    string id = dgv_quick_quote_details.Rows[e.RowIndex].Cells["quick_item_id"].Value.ToString();
                    HandleModelSelectionClick(e.RowIndex, id);
                }

                ConnectGridviewToDescriptionText(e.RowIndex, dgv_quick_quote_details);
                ComputeByReferenceHierarchy();

            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message + Environment.NewLine + "Dev Error: from dgv_quick_quote_details_CellClick");
            }
        }

        private void ComputeByReferenceHierarchy()
        {

            DataTable dataSourceQuickQuote = dgv_quick_quote_details.DataSource as DataTable;

            if (dataSourceQuickQuote == null) return;

            var parentReferenceCodes = dataSourceQuickQuote.AsEnumerable()
                .Select(row => GetParentReferenceCode(dataSourceQuickQuote, row.Field<string>("reference_code")))
                .Where(parentCode => !string.IsNullOrEmpty(parentCode))
                .Distinct()
                .ToList();

            foreach (var parentReferenceCode in parentReferenceCodes)
            {
                // Calculate the total unit_price for the given parent and its descendants
                decimal totalUnitPrice = GetTotalUnitPriceForChildren(dataSourceQuickQuote, parentReferenceCode);

                // Output the total unit_price for this parent reference_code
                Console.WriteLine($"Total unit_price for children of '{parentReferenceCode}' and its descendants: {totalUnitPrice:C}");

                // Update the DataGridView cell for the parent reference_code
                foreach (DataGridViewRow row in dgv_quick_quote_details.Rows)
                {
                    if (row.Cells["reference_code"].Value?.ToString() == parentReferenceCode)
                    {
                        row.Cells["quick_unit_price"].Value = totalUnitPrice.ToString();//Helpers.FormatAsCurrency(totalUnitPrice.ToString());
                        break;           

                    }
                }
            }
        }

        private string GetParentReferenceCode(DataTable dt, string v)
        {
            if (string.IsNullOrWhiteSpace(v))
                return null;

            int lastDot = v.LastIndexOf('.');
            if (lastDot > 0)
                return v.Substring(0, lastDot);

            // No parent (top-level)
            return null;
        }

        private decimal GetTotalUnitPriceForChildren(DataTable dt, string parentReferenceCode)
        {

            var ParentRow = dt.AsEnumerable()
                .FirstOrDefault(row => row.Field<string>("reference_code") == parentReferenceCode);

            // Find all direct children of the parent reference_code
            if (dt == null)
                return 0;

            var children = dt.AsEnumerable()
                .Where(row =>
                {
                    var refCode = row.Field<string>("reference_code");

                    if (refCode == null) return false;

                    if (refCode == parentReferenceCode) return false;

                    // must start with parentReferenceCode + "."
                    if (!refCode.StartsWith(parentReferenceCode + ".")) return false;

                    // remove parent prefix and check if there's another dot — means it's a grandchild
                    var remainder = refCode.Substring(parentReferenceCode.Length + 1);
                    return !remainder.Contains(".");
                })
                .ToList();

            // If no children found, return 0
            if (!children.Any())
                return 0;

            decimal totalLaborCost = 0m;

            if (ParentRow != null)
            {
                decimal manDays = Convert.ToDecimal(ParentRow["man_days"]);
                decimal laborRate = Convert.ToDecimal(ParentRow["labor_rate"]);

                totalLaborCost = laborRate * manDays;

                //Console.WriteLine($"Adding labor cost for parent '{parentReferenceCode}': {manDays} * {laborRate} = {totalLaborCost:C}");
            }

            decimal AllChildTotal = 0;

            // For each child, recursively sum their descendants' unit_prices
            foreach (var child in children)
            {
                string childReferenceCode = child.Field<string>("reference_code");

                // Recursively find the total for this child's descendants
                decimal ChildTotal = GetTotalUnitPriceForChildren(dt, childReferenceCode);
                AllChildTotal = ChildTotal;
                //Console.WriteLine($"Adding total from child '{childReferenceCode}': 'Child Total:' {ChildTotal}': 'Total Amount:' {AllChildTotal:C}");
            }

            // Sum the unit_price for the current direct children
            decimal totalUnitPrice = children.Sum(row =>
            {
                string value = Helpers.GetCleanedPriceValue(row["unit_price"]?.ToString());
                string qty = Helpers.GetCleanedPriceValue(row["qty"]?.ToString());
                decimal NewUnitPrice = (decimal.TryParse(value, out decimal parsed) ? parsed : 0) * (decimal.TryParse(qty, out decimal qtyParsed) ? qtyParsed : 0);

                //Console.WriteLine($"sum all the child  unit_price for '{row.Field<string>("reference_code")} ': {value} ' * ' {qty} ' = ' {NewUnitPrice:C}");

                return NewUnitPrice;
            });

            decimal TotalAmount = (totalLaborCost + totalUnitPrice) * 1.186m;
            //decimal TotalAmount = (totalLaborCost + totalUnitPrice);

            return TotalAmount;
        }

        public static decimal CalculateDiscountMultiplier(string discountString)
        {
            if (string.IsNullOrWhiteSpace(discountString))
                return 1m;

            try
            {
                // Replace all spaces for safety
                discountString = discountString.Replace(" ", "");

                // Split by '*' while keeping '/' info
                var parts = discountString.Split(new[] { '*' }, StringSplitOptions.RemoveEmptyEntries);

                decimal result = 1m;

                foreach (var part in parts)
                {
                    if (part.StartsWith("/"))
                    {
                        // Handle division case like "/.7"
                        var value = decimal.Parse(part.Substring(1));
                        result *= (1m / value);
                    }
                    else if (part.Contains('/'))
                    { 
                        MessageBox.Show("Invalid discount format. Division should be at the start of the part.");
                    }
                    else
                    {
                        // Normal multiplier
                        var value = decimal.Parse(part);
                        result *= value;
                    }
                }

                return result;
            }
            catch
            {
                throw new ArgumentException("Invalid discount string format.");
            }
        }

        private static bool IsValidMoneyFormat(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return false;

            // Remove currency symbols and thousands separators
            string cleaned = input.Replace("₱", "")
                                  .Replace("$", "")
                                  .Replace(",", "")
                                  .Trim();

            decimal value;
            return decimal.TryParse(cleaned, out value);
        }

        // Example usage in your code (add this check before parsing money values):
        private static decimal GetCleanedPriceValue(string input)
        {
            if (!IsValidMoneyFormat(input))
            {
                MessageBox.Show("Invalid money format. Please enter a valid number.");
                return 0;
            }

            // Remove currency symbols and thousands separators
            var cleaned = decimal.Parse(input.Replace("₱", "")
                                   .Replace("$", "")
                                   .Replace(",", "")
                                   .Trim());
            return cleaned;
        }
        int SelectedRowIndex = 0;

        private void ConnectGridviewToDescriptionText(int RowIndex, DataGridView dgv)
        {

            DataTable dataSource = dgv_quick_quote_details.DataSource as DataTable;
            if (dataSource == null)
                return;

            SelectedRowIndex = RowIndex;

            if (dgv.Rows[RowIndex].Cells["reference_code"].Value == null || string.IsNullOrEmpty(dgv.Rows[RowIndex].Cells["reference_code"].Value.ToString()))
                return;


            //extract the reference code and turn to array string to count how many level happen in reference code
            string reference = dgv.Rows[RowIndex].Cells["reference_code"].Value.ToString();
            string[] arrayReference = reference.Split('.');
            int referenceCount = arrayReference.Length;

            //check if the row is parent using the reference_code if only get level 1
            if (referenceCount == 1)
            {
                EnableDescription(true);
                int item_id_parent = int.Parse(dgv.Rows[RowIndex].Cells["quick_item_id"].Value.ToString());
                getItemShortDescription(item_id_parent);
                txt_short_description.Text = (dgv.Rows[RowIndex].Cells["short_description"].Value.ToString() == "" ? txt_long_description.Text : dgv.Rows[RowIndex].Cells["short_description"].Value.ToString());
            }
            else
            {
                txt_short_description.Text = "";
                txt_long_description.Text = "";
                EnableDescription(false);
            }
        }

        private void EnableDescription(bool Enable)
        {
            txt_short_description.Enabled = Enable;
            txt_long_description.Enabled = Enable;
        }

        string temp_refence_code = null;

        private void HandleModelSelectionClick(int RowIndex, string Id)
        {
            ModelModal createModal = new ModelModal(ItemList, BomHead, BomDetails, Id);
            DialogResult result = createModal.ShowDialog();

            string referenceCode = dgv_quick_quote_details.Rows[RowIndex].Cells["reference_code"].Value.ToString();


            if (result == DialogResult.OK)
            {
                int itemId = createModal.GetItemId();
                int bomId = createModal.GetBomId();

                DataTable dataSource = dgv_quick_quote_details.DataSource as DataTable;
                if (dataSource == null) return;

                temp_refence_code = dgv_quick_quote_details.Rows[RowIndex].Cells["reference_code"].Value.ToString();
                DeleteRowsByReferenceCode(RowIndex, dgv_quick_quote_details);

                if (bomId != 0)
                {
                    GetBomDataRecursive(RowIndex, bomId, itemId, dgv_quick_quote_details, referenceCode);
                    counterParent = 1;
                }
                else
                {
                    GetItemData(RowIndex, itemId, dgv_quick_quote_details, referenceCode);
                }

            }
        }

        private void dgv_quick_quote_details_CellContentDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            // Skip header clicks
            //if (e.RowIndex < 0 || e.ColumnIndex < 0)
            //    return;
            //// Components Column
            //if (e.ColumnIndex == 7)
            //{
            //    HandleItemSelectionClick(e.RowIndex, dgv_quick_quote_details);
            //}
            //// Canvass Sheet Column
            //if (e.ColumnIndex == 8)
            //{
            //    string id = dgv_quick_quote_details.Rows[e.RowIndex].Cells[5].Value.ToString();
            //    HandleCanvasSelectionClick(e.RowIndex, id);
            //}
        }

        // Selected item from item list
        private void HandleItemSelectionClick(int rowIndex, DataGridView dgv)
        {
            counterReference++;
            SalesItemModal itemModal = new SalesItemModal(ItemList, BomHead, BomDetails);
            DialogResult r = itemModal.ShowDialog();

            if (r == DialogResult.OK)
            {
                int itemid = itemModal.GetParentItemId();

                if (itemModal.isBom)
                {
                    int bomID = itemModal.GetBomResult();
                    GetBomDataRecursive(rowIndex, bomID, itemid, dgv);
                    counterParent = 1;
                }
                else if (itemModal.isItem)
                {
                    GetItemData(rowIndex, itemid, dgv, null);
                }
                else
                {
                    // Invalid/unmatched case
                    MessageBox.Show("Invalid selection. The chosen item could not be matched to an Item or BOM.",
                                    "Invalid Selection",
                                    MessageBoxButtons.OK,
                                    MessageBoxIcon.Error);
                }
            }
        }

        public List<Dictionary<string, object>> SelectedImages { get; private set; }
        private void HandleItemImageSelectionClick(int quickId, int itemId)
        {
            DataView dvItems = new DataView(ItemList);
            DataTable filteredItems = dvItems.ToTable();

            if (filteredItems.Rows.Count == 0)
            {
                MessageBox.Show("Item not found.");
                return;
            }

            string itemName = filteredItems.Rows[0]["item_name"].ToString();

            DataView dvImages = new DataView(ImageList);
            dvImages.RowFilter = $"based_id = {itemId}";
            DataTable filteredImages = dvImages.ToTable();


            DataView dvSelectedImages = new DataView(selectedImageList);
            dvSelectedImages.RowFilter = $"quotation_quick_id = {quickId}";
            DataTable filteredSelectedImages = dvSelectedImages.ToTable();

            ItemImagesModal itemImageModal = new ItemImagesModal(itemName, filteredItems, filteredImages, filteredSelectedImages);
            DialogResult r = itemImageModal.ShowDialog();

            if (r == DialogResult.OK)
            {
                SelectedImages = itemImageModal.SelectedImages;
                int selectedImageCount = SelectedImages.Count();
                MessageBox.Show($"{selectedImageCount} images selected.");
            }

        }

        // Counters for reference codes
        int counterReference = 0;
        int counterParent = 1;

        private decimal GetBomDataRecursive(int rowIndex, int bomID, int itemID, DataGridView dgv, string additionalReference = null, int level = 0, HashSet<int> visited = null)
        {
            Dictionary<int, DataRow> bomHeadDict = new Dictionary<int, DataRow>();
            Dictionary<int, List<DataRow>> bomChildDict = new Dictionary<int, List<DataRow>>();

            if (BomHead != null && BomHead.Rows.Count > 0)
            {
                bomHeadDict = BomHead.AsEnumerable()
                    .ToDictionary(r => r.Field<int>("id"));
            }

            if (BomDetails != null && BomDetails.Rows.Count > 0)
            {
                bomChildDict = BomDetails.AsEnumerable()
                    .GroupBy(r => r.Field<int>("item_bom_id"))
                    .ToDictionary(g => g.Key, g => g.ToList());
            }

            // --- Initialize ---
            if (visited == null)
                visited = new HashSet<int>();

            if (visited.Contains(bomID))
                return 0;
            visited.Add(bomID);

            if (counterParent == 1)
            {
                counterParent = counterReference;
            }

            string ParentLevel = null;

            if (string.IsNullOrEmpty(additionalReference))
                additionalReference = counterParent.ToString();

            if (level == 0)
            {
                string[] arrayReference = additionalReference.Split('.');
                int referenceCount = arrayReference.Length - 1;
                level = referenceCount;
            }

            ParentLevel = new string(' ', level * 4);

            DataTable dataSource = dgv.DataSource as DataTable;
            if (dataSource == null)
                return 0;

            if (!bomHeadDict.TryGetValue(bomID, out DataRow parentRow))
                return 0;

            // --- Compute parent labor cost ---
            decimal manDays = Convert.ToDecimal(parentRow["man_days"]);
            decimal laborRate = Convert.ToDecimal(parentRow["labor_rate"]);
            decimal laborCost = manDays * laborRate;

            // --- Initial total cost for this parent (production + labor) ---
            decimal totalCost = laborCost;

            // --- Add parent row ---
            DataRow newParent = dataSource.NewRow();
            newParent["reference_code"] = additionalReference;
            newParent["bom_id"] = parentRow["id"];
            newParent["item_id"] = parentRow["item_id"];
            newParent["components"] = ParentLevel + parentRow["general_name"];
            newParent["model"] = parentRow["item_model"];
            newParent["qty"] = parentRow["production_qty"];
            newParent["man_days"] = parentRow["man_days"];
            newParent["labor_rate"] = parentRow["labor_rate"];
            newParent["unit_price"] = Convert.ToDecimal(parentRow["production_cost"]);

            dataSource.Rows.InsertAt(newParent, rowIndex);

            int item_id_parent = Convert.ToInt32(parentRow["item_id"]);

            //Helpers.SalesItemRowStyler.ApplyStyle(dgv, rowIndex, "parent");

            int insertIndex = rowIndex + 1; // Start inserting children after parent

            level++; // Increase level for children

            // --- Process children ---
            if (!bomChildDict.TryGetValue(bomID, out List<DataRow> childRows))
            {
                return 0;
            }

            int counterSub = 1;
            foreach (DataRow child in childRows)
            {
                int childItemId = Convert.ToInt32(child["item_id"]);

                // Check if child item is also a BOM
                DataRow subBomRow = bomHeadDict.Values.FirstOrDefault(r => r.Field<int>("item_id") == childItemId);


                // Recursive case: child is a BOM if not then it's a leaf item
                if (subBomRow != null)
                {
                    int subBomId = Convert.ToInt32(subBomRow["id"]);

                    decimal subTotal = GetBomDataRecursive(insertIndex, subBomId, childItemId, dgv, $"{additionalReference}.{counterSub}", level, visited);

                    totalCost += subTotal;

                    // After recursion, update insertIndex to point after the last inserted child subtree
                    // Count how many rows were inserted for this subtree
                    int subtreeRows = CountRowsByReference(dataSource, $"{additionalReference}.{counterSub}");
                    insertIndex += subtreeRows;
                }
                else
                {
                    decimal unitPrice = Convert.ToDecimal(child["unit_price"]);
                    decimal qty = Convert.ToDecimal(child["bom_qty"]);
                    decimal lineTotal = unitPrice * qty;
                    totalCost += lineTotal;

                    // Leaf item
                    DataRow newChild = dataSource.NewRow();
                    newChild["bom_id"] = child["item_bom_id"];
                    newChild["item_id"] = childItemId;
                    newChild["components"] = new string(' ', level * 4) + child["item_name"];
                    newChild["model"] = child["size"];
                    newChild["qty"] = qty;
                    newChild["unit_price"] = unitPrice.ToString();
                    newChild["reference_code"] = $"{additionalReference}.{counterSub}";

                    int addedChildIndex = rowIndex + 1;

                    dataSource.Rows.InsertAt(newChild, insertIndex);
                    dgv.Rows[insertIndex].ReadOnly = true;
                    //Helpers.SalesItemRowStyler.ApplyStyle(dgv, insertIndex, "child");
                    insertIndex++; // Move to next position for next child
                }

                counterSub++;
            }


            // Update the parent unit_price to total of all its descendants
            //1.186 is for 18% VAT
            decimal TotalCostWithMarkup = decimal.Parse(totalCost.ToString()) * 1.186m;
            dataSource.Rows[rowIndex]["unit_price"] = TotalCostWithMarkup.ToString();




            counterParent++;
            return totalCost;

        }

        // Helper to count rows by reference code prefix
        private int CountRowsByReference(DataTable dt, string referencePrefix)
        {
            int count = 0;
            foreach (DataRow row in dt.Rows)
            {
                var refCode = row.Table.Columns.Contains("reference_code") ? row["reference_code"]?.ToString() : null;
                if (!string.IsNullOrEmpty(refCode) && refCode.StartsWith(referencePrefix))
                    count++;
            }
            return count;
        }

        private void GetItemData1(int rowIndex, int itemID, DataGridView dgv)
        {
            int selectedIndex = itemID;
            MessageBox.Show("ITEM ID: " + itemID);
            if (selectedIndex >= 0 && selectedIndex < ItemList.Rows.Count)
            {
                DataTable itemList = Helpers.FilterExactDataTable(ItemList, selectedIndex.ToString(), "id");

                DataTable dataSource = dgv.DataSource as DataTable;

                if (!isProject)
                {
                    if (dataSource == null) return;
                }

                foreach (DataRow row in itemList.Rows)
                {
                    if (isProject)
                    {
                        if (tabControl2.SelectedTab.Controls[0] is ItemSetUC currentControl)
                        {
                            string item_id = row["id"].ToString();
                            string item_name = row["item_name"].ToString();
                            string bomid = "0";
                            string itemcode = row["item_code"].ToString();
                            string size = null;
                            currentControl.SetComponentData(rowIndex, item_id, item_name, itemcode, size, bomid);
                        }
                    }
                    else
                    {
                        DataRow newRow = dataSource.NewRow();

                        if (dataSource.Columns.Contains("unit_of_measure"))
                        {
                            newRow["unit_of_measure"] = row["unit_of_measure"];
                        }

                        newRow["item_id"] = row["id"];
                        newRow["model"] = row["item_code"];
                        newRow["components"] = row["item_name"];
                        //newRow["unit_price"] = Helpers.FormatAsCurrency(row["unit_price"]);
                        dataSource.Rows.Add(newRow);
                    }

                }
            }
            else
            {
                MessageBox.Show("Invalid selection", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void GetItemData(int rowIndex, int itemID, DataGridView dgv, string reference, string counter = null)
        {
            DataTable itemList = Helpers.FilterExactDataTable(ItemList, itemID.ToString(), "id");

            int level = 0;

            if (reference != null)
            {
                string[] arrayReference = reference.Split('.');
                int referenceCount = arrayReference.Length - 1;
                level = referenceCount;
            }


            if (itemList.Rows.Count == 0)
            {
                MessageBox.Show("Invalid selection. Item not found.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            DataTable dataSource = dgv.DataSource as DataTable;
            if (!isProject && dataSource == null) return;

            foreach (DataRow row in itemList.Rows)
            {
                if (isProject)
                {
                    if (tabControl2.SelectedTab.Controls[0] is ItemSetUC currentControl)
                    {
                        string item_id = row["id"].ToString();
                        string item_name = row["item_name"].ToString();
                        string bomid = "0";
                        string itemcode = row["item_code"].ToString();
                        string size = null;

                        currentControl.SetComponentData(rowIndex, item_id, item_name, itemcode, size, bomid);
                    }
                }
                else
                {
                    DataRow newRow = dataSource.NewRow();
                    if (dataSource.Columns.Contains("unit_of_measure"))
                        newRow["unit_of_measure"] = row["unit_of_measure"];

                    reference = (reference != null) ? reference : (counter != null) ? counter : counterReference.ToString();

                    newRow["item_id"] = row["id"];
                    newRow["model"] = row["item_model"];
                    newRow["components"] = new string(' ', level * 4) + row["item_name"];
                    newRow["reference_code"] = reference;

                    dataSource.Rows.InsertAt(newRow, rowIndex);

                    // 🎨 Style as Single Item
                    int addedRowIndex = dataSource.Rows.Count - 1;
                    Helpers.SalesItemRowStyler.ApplyStyle(dgv, addedRowIndex, "single");
                }
            }

        }



        private void HandleCanvasSelectionClick(int rowIndex, string item_id)
        {
            frm_canvas_modal canvas = new frm_canvas_modal(item_id, bpi_general, bpi_items);

            DialogResult r = canvas.ShowDialog();

            if (r == DialogResult.OK)
            {

            }
        }
        private decimal? TryParseDecimal(object value)
        {
            decimal result;
            if (decimal.TryParse(value?.ToString(), out result))
                return result;
            return null;
        }

        public decimal GetCashDiscount()
        {
            decimal cash_disc = Convert.ToDecimal(txt_cash_discount.Text.ToString());
            return cash_disc;
        }
        private void computationLoop()
        {
            double gross_sales = 0, vat_amount = 0, net_sales = 0;
            double percent_discount = 0;
            double net_amount_due = 0, total_amount_due = 0;
            double cash_discount = double.Parse(txt_cash_discount.Text);
            const double VAT_RATE = 0.12; // 12% VAT



            // First pass: Calculate gross sales and total discounts
            foreach (DataGridViewRow row in this.dgv_quick_quote_details.Rows)
            {
                if (row.Cells["quick_net_total"].Value != null &&
                    !String.IsNullOrEmpty(row.Cells["quick_net_total"].Value.ToString()))
                {
                    // Get unit price * quantity = net total
                    double netAmount = double.Parse(Helpers.GetCleanedPriceValue(row.Cells["quick_net_total"].Value.ToString()));
                    gross_sales += netAmount;

                    // Get line total (after discount)
                    if (row.Cells["quick_line_total"].Value != null &&
                    !string.IsNullOrEmpty(row.Cells["quick_line_total"].Value.ToString()))
                    {
                        double lineTotal = double.Parse(Helpers.GetCleanedPriceValue(row.Cells["quick_line_total"].Value.ToString()));
                        net_sales += lineTotal;
                    }
                }
            }


            if (gross_sales != 0)
            {
                percent_discount = ((gross_sales - net_sales) / gross_sales) * 100;
            }

            vat_amount = net_sales * VAT_RATE;

            net_amount_due = net_sales - cash_discount;

            total_amount_due = net_amount_due + vat_amount;

            // Format and display results
            txt_gross_sales.Text = Helpers.MoneyFormat(gross_sales);
            txt_vat_amount.Text = Helpers.MoneyFormat(vat_amount);
            txt_net_sales.Text = Helpers.MoneyFormat(net_sales);

            txt_percent_discount.Text = percent_discount.ToString("N2") /*+ " %"*/;
            txt_cash_discount.Text = Helpers.MoneyFormat(cash_discount);
            txt_net_amount_due.Text = Helpers.MoneyFormat(net_amount_due);
            txt_total_amount_due.Text = Helpers.MoneyFormat(total_amount_due);
        }
        private void dgv_quick_quote_details_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            //var value = dgv_quick_quote_details.Rows[e.RowIndex].Cells["quick_qty"].Value;

            //if (int.TryParse(value?.ToString(), out int qty) && qty != 0)
            //{
            //    ComputeDgv(e);
            //}
            //else
            //{
            //    MessageBox.Show("Quantity is required.");
            //    this.BeginInvoke(new Action(() =>
            //    {
            //        dgv_quick_quote_details.CurrentCell = dgv_quick_quote_details.Rows[e.RowIndex].Cells["quick_qty"];
            //        dgv_quick_quote_details.BeginEdit(true);
            //    }));
            //}

        }
        DataTable stockQuickDataTable = new DataTable();
        private async void Quotation_Load(object sender, EventArgs e)
        {
            int dgvWidth = dgv_quick_quote_details.Width;
            int dgvHeight = dgv_quick_quote_details.Height;

            Panel[] panels = { pnl_header, pnl_footer };
            Helpers.ReadOnlyControls(panels);

            SetNewFormMode(false);

            await LoadExistingRecord();

        }
        private async Task LoadExistingRecord()
        {
            stockQuickDataTable = Helpers.GetDataTableFromUnboundGrid(dgv_quick_quote_details);
            await fetchItemData();
            await fetchBpiData();
            tabControl2.DrawMode = TabDrawMode.OwnerDrawFixed;
            tabControl2.DrawItem += tabControl2_DrawItem;
            dtp_date.Format = DateTimePickerFormat.Custom;
            dtp_date.CustomFormat = "MMM dd yyyy";
            dtp_valid_until.Format = DateTimePickerFormat.Custom;
            dtp_valid_until.CustomFormat = "MMM dd yyyy";

            // if quotation comes from opportunities or other sources
            if (!string.IsNullOrEmpty(documentNo))
            {
                back.Visible = true;
                if (isFinalized)
                {
                    Panel[] panels = { pnl_header, pnl_footer };
                    Helpers.ReadOnlyControls(panels);
                    dgv_quick_quote_details.ReadOnly = true;
                }

                // button state if is Quick Quote
                this.btn_quick_quote.BackColor = Color.FromArgb(255, 128, 128);
                this.btn_project.BackColor = Color.White;

                //set height if is Quick Quote
                this.tabControl.SelectedIndex = 0;
                this.tabControl.Height = 600;
                this.Size = new Size(1386 - 80, 900);

                this.tabControl.ItemSize = new Size(0, 0);

                // overload, added version
                FetchQuotationDetailsByDocumentNo(documentNo, versionNo, subVersionNo);
                bs_unit.DataSource = CacheData.UoM;
            }
            else
            {
                // button state if is Project
                this.btn_quick_quote.BackColor = Color.FromArgb(255, 128, 128);
                this.btn_project.BackColor = Color.White;

                //set height if is Project
                this.tabControl.SelectedIndex = 0;
                this.tabControl.Height = 600;  // Set the desired width and height for the form
                this.Size = new Size(1386 - 80, 950);  // Set the desired width and height for the form

                this.tabControl.ItemSize = new Size(0, 0);

                // LEM
                LoadApplicationSetup();
                LoadPurposeSetup();
                LoadShipTypeSetup();
                // ----

                //cmb_application.DataSource = CacheData.ApplicationSetup;
                //cmb_application.DisplayMember = "code";
                //cmb_application.ValueMember = "id";

                //cmb_purpose.DataSource = STATIC_QUOTATION_PURPOSE.LIST();
                //cmb_purpose.DisplayMember = "code";
                //cmb_purpose.ValueMember = "title";

                //cmb_ship_type.DataSource = CacheData.ShipTypeSetup;
                //cmb_ship_type.DisplayMember = "ship_name";
                //cmb_ship_type.ValueMember = "id";



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

                await fetchQuotationDetails();
            }

        }
        private void GetCMBValues()
        {

        }
        // PSEUDOCODE / PLAN
        // - When binding childList to the DataGridView ensure the underlying DataTable contains a "quick_images" column.
        // - If the column is missing, add it to the DataTable before assigning DataSource.
        // - This guarantees the column exists (designer or runtime) and LoadQuickImageCounts can populate it.
        // - Keep the rest of the bind logic unchanged.

        private void bind(bool isBind = false)
        {
            if (isBind)
            {

                Panel[] pnlList = { pnl_header, pnl_footer };
                DataTable HeaderList = this.transactionList.Clone();
                HeaderList.Columns.Add("branch_name", typeof(string));
                HeaderList.Columns.Add("customer_code", typeof(string));
                HeaderList.Columns.Add("number", typeof(string));

                //bs_ship_to.DataSource = bpi_address;
                //bs_bill_to.DataSource = bpi_address;


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


                    DataRow[] bpiRows = bpi_general.Select($"general_based_id = '{ID}'");
                    DataRow[] contactsRows = bpi_contacts.Select($"contacts_based_id = '{ID}'");

                    if (bpiRows.Length > 0)
                    {
                        newRow["branch_name"] = bpiRows[0]["branch_name"].ToString();
                        newRow["customer_code"] = bpiRows[0]["customer_code"].ToString();
                        //newRow["number"] = contactsRows[0]["number"].ToString();
                    }
                    else
                    {
                        newRow["branch_name"] = "Unknown Branch";
                        newRow["customer_code"] = "N/A";
                    }
                    HeaderList.Rows.Add(newRow);
                }


                int sId = Convert.ToInt32(HeaderList.Rows[SelectedRow]["id"]);
                int cId = Convert.ToInt32(HeaderList.Rows[SelectedRow]["customer_id"]);
                int appId = Convert.ToInt32(HeaderList.Rows[SelectedRow]["application_id"]);
                int billId = Convert.ToInt32(HeaderList.Rows[SelectedRow]["bill_to_id"]);
                int shipId = Convert.ToInt32(HeaderList.Rows[SelectedRow]["ship_to_id"]);


                LoadApplicationSetup();
                LoadCustomerBillAddress(cId.ToString());
                LoadCustomerShipAddress(cId.ToString());

                cmb_application.SelectedItem = appId;
                cmb_application.SelectedValue = appId;

                cmb_bill_to.SelectedItem = billId;
                cmb_bill_to.SelectedValue = billId;

                cmb_ship_to.SelectedItem = shipId;
                cmb_ship_to.SelectedValue = shipId;

                GetCMBValues();
                Helpers.BindControls(pnlList, HeaderList, SelectedRow);



                // LEM - Button visibility condition
                isFinalized = Convert.ToBoolean(HeaderList.Rows[SelectedRow]["is_finalized"]);
                btn_finalize.Enabled = !isFinalized || string.IsNullOrEmpty(txt_id.Text);
                btn_sales_order.Enabled = isFinalized;

                btn_new_version.Visible = !isFinalized;
                btn_duplicate.Visible = !isFinalized;

                // Label DocNumber
                foreach (var pnl in pnlList)
                {
                    foreach (Control control in pnl.Controls)
                    {
                        if (control is TextBox textBox && textBox.Name.Contains("txt_document_no"))
                        {
                            string docNo = textBox.Text;

                            if (!docNo.StartsWith("Q#") && !docNo.StartsWith("FQ#"))
                            {
                                textBox.Text = isFinalized ? $"FQ#{docNo}" : $"Q#{docNo}";
                            }
                            else if (docNo.StartsWith("Q#") && isFinalized)
                            {
                                // Replace "Q#" with "FQ#"
                                textBox.Text = "FQ#" + docNo.Substring(2);
                            }
                        }
                    }
                }

                // Create filtered view
                DataView dataview = new DataView(childList);
                dataview.RowFilter = $"based_id = " + this.transactionList.Rows[this.SelectedRow]["id"].ToString();

                //// Ensure quick_images column exists
                //// It not existing on the childList DataTable that why we need to check and add it here
                //if (dataview.Table != null && !dataview.Table.Columns.Contains("quick_images"))
                //{
                //    var col = new DataGridViewTextBoxColumn();
                //    col.Name = "quick_images";
                //    col.HeaderText = "IMAGES";

                //    dgv_quick_quote_details.Columns.Insert(4, col);
                //}

                dgv_quick_quote_details.DataSource = dataview;

                LoadQuickImageCounts();
            }
        }
        private void LoadQuickImageCounts()
        {
            //// Ensure quick_images column exists
            //if (!dgv_quick_quote_details.Columns.Contains("quick_images"))
            //{
            //    // TO BE CHANGED/FIND INSIDE DGV COLUMN INSTEAD OF CREATING
            //    var col = new DataGridViewTextBoxColumn();
            //    col.Name = "quick_images";
            //    col.HeaderText = "IMAGES";

            //    if (dgv_quick_quote_details.Columns.Count > 6)
            //        dgv_quick_quote_details.Columns.Insert(6, col);
            //    else
            //        dgv_quick_quote_details.Columns.Add(col);
            //}

            // Loop through each row in the DataGridView

            if (dgv_quick_quote_details.Columns.Contains("quick_images"))
            {
                foreach (DataGridViewRow row in dgv_quick_quote_details.Rows)
                {
                    if (row.Cells["quick_id"].Value == null) continue;

                    int quickId = Convert.ToInt32(row.Cells["quick_id"].Value);

                    // Count how many images are linked to this quickId
                    int count = selectedImageList.AsEnumerable()
                        .Count(r => Convert.ToInt32(r["quotation_quick_id"]) == quickId);

                    // Put the count in quick_images column
                    row.Cells["quick_images"].Value = $"SELECTED: {count}";
                }
            }
        }
        private void txt_days_TextChanged(object sender, EventArgs e)
        {
            ValidUntilDate();
        }
        private void ValidUntilDate()
        {
            var date = dtp_date.Value;
            var noOfDays = txt_validays.Text;

            if (string.IsNullOrEmpty(noOfDays))
            {
                noOfDays = "30";
            }

            if (int.TryParse(noOfDays, out int days) && days > 0 && days < 1000)
            {
                dtp_valid_until.Value = date.AddDays(days);
            }
            else
            {
                txt_validays.Text = "30";
                dtp_valid_until.Value = date.AddDays(30);
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
        private DataTable bpi_address2 = new DataTable();
        private DataTable bpi_contacts = new DataTable();
        private DataTable bpi_items = new DataTable();
        private object previousDataSource;


        //Create this to handle resetting of controls with specific tags like money_format and percent_format
        private void ResetControls(Panel panel)
        {
            foreach (Control ctrl in panel.Controls)
            {
                if (ctrl is TextBox)
                {
                    TextBox txtBox = (TextBox)ctrl;
                    if (txtBox.Tag == "money_format")
                    {
                        txtBox.Text = "0.00";
                    }
                    else if (txtBox.Tag == "percent_format")
                    {
                        txtBox.Text = "0%";
                    }
                    txtBox.Text = "";
                }
            }
        }

        private void btn_new_Click(object sender, EventArgs e)
        {
            GetLatestDate();
            SetNewFormMode(true);
            isNewRecord = true;

            // New Quick Quote
            if (!isProject)
            {


                Helpers.ResetControls(pnl_header);
                ResetControls(pnl_footer);
                
              

                //Helpers.ResetControls(panel);

                // resets the datasource so that only customers would specific address would be seen.

                bs_bill_to.DataSource = null;
                bs_ship_to.DataSource = null;
                bs_unit.DataSource = CacheData.UoM;
                Panel[] pnls = { pnl_header, pnl_footer };
                Helpers.ReadOnlyControls(pnls);
                dgv_quick_quote_details.ReadOnly = false;
                txt_cash_discount.ReadOnly = false;


                foreach (Control ctrl in pnl_footer.Controls)
                {
                    if (ctrl is TextBox)
                    {
                        TextBox txtBox = (TextBox)ctrl;
                        txtBox.Text = "0";
                    }
                }

                //toolstrip_quotation.Enabled = false;
                //dgv_quick_quote_details.Enabled = true;
                //previousDataSource = dgv_quick_quote_details.DataSource;
                //((DataView)dgv_quick_quote_details.DataSource).Table.Clear();

                if (dgv_quick_quote_details.DataSource is DataTable dt)
                {
                    dt.Rows.Clear();
                }
                else if (dgv_quick_quote_details.DataSource is DataView dv)
                {
                    dv.Table.Clear(); // clear the underlying DataTable
                }
                else
                {
                    dgv_quick_quote_details.Rows.Clear(); // fallback for unbound
                }

                dgv_quick_quote_details.DataSource = stockQuickDataTable.Clone();



                bind(false);
                DocumentIncrementer();

                txt_created_by.Text = CacheData.CurrentUser.first_name + " " + CacheData.CurrentUser.last_name;
                txt_vat_percent.Text = "12";
                txt_vat_percent.ReadOnly = true;
                btn_add_customer.Enabled = true;
                btn_save.Enabled = true;

                Panel[] panels = { pnl_header, pnl_footer };
                Helpers.ResetReadOnlyControls(panels);


                //pnl_header.Enabled = true;
                //pnl_footer.Enabled = true;

                //DataTable dt = (DataTable)bs_quick_quotes_details.DataSource;
            }
            // New Project
            else
            {
                DocumentIncrementer();
                bs_bill_to.DataSource = null;
                bs_ship_to.DataSource = null;

                txt_project_name.Clear();
                txt_project_name.ReadOnly = false;

                this.tabControl2.Controls.Clear();
                MessageBox.Show("No project data found. Creating a new entry.", "Empty Data", MessageBoxButtons.OK, MessageBoxIcon.Information);

                DataTable dt = new DataTable();
                bs_project_multipliers.DataSource = dt.Clone();

                TabPage newTabs = new TabPage("+");
                this.tabControl2.TabPages.Add(newTabs);

                // Get the last index (before the add new tab)
                var lastIndex = this.tabControl2.TabCount - 1;

                // Create a new TabPage
                TabPage newTab = new TabPage("New Project");
                // Create an instance of ItemSetUC
                ItemSetUC UC = new ItemSetUC
                {
                    Dock = DockStyle.Fill,
                    BackColor = Color.White
                };

                // Attach event handlers
                UC.ButtonClicked += Button_ClickedUC;
                UC.DataChangedConditions += ItemSet_DataChanged;
                UC.DataChangedContent += Content_DataChanged;
                UC.ItemChanged += ItemChanged;
                UC.CellChangedProject += Cell_DataChanged;
                UC.CellClicked += Cell_ClickedUC;
                UC.CellEdited += Cell_EditedUC;
                UC.FinalTxtBoxClicked += FinalTxtBoxClicked;
                UC.SetUnitsOfMeasure(CacheData.UoM, CacheData.UoM);

                // Add the UserControl to the new tab
                newTab.Controls.Add(UC);
                pnl_header.Focus();

                // Insert the new tab before the last tab
                this.tabControl2.TabPages.Insert(lastIndex, newTab);

                // Select the newly added tab
                this.tabControl2.SelectedIndex = lastIndex;
                setProjectMultiplier();
                return;
            }

            //for new quatations always 30 days
            txt_validays.Text = "30";
            counterReference = 0;
            SelectedRowIndex = 0;
        }
        private async void FinalTxtBoxClicked(object sender, EventArgs e)
        {
            DataTable pumps = new DataTable();


            var data = await ProjectService.GetPumpsViewList();

            //if (tabControl2.SelectedTab.Controls[0] is ItemSetUC currentControl)
            //{
            //    var data = currentControl.GetSizeUpData();
            //    List<KeyValuePair<string, dynamic>> size_up = data.ToList();
            //    pumps = JsonHelper.ToDataTable(size_up);
            //}

            pumps = JsonHelper.ToDataTable(data.ItemPumpsView);

            SalesItemModal im = new SalesItemModal(pumps);
            DialogResult r = im.ShowDialog();

            if (r == DialogResult.OK)
            {
                int selectedIndex = im.GetResult();

                if (selectedIndex >= 0 && selectedIndex < ItemList.Rows.Count)
                {
                    DataRow selectedItem = pumps.Rows[selectedIndex];

                    var id = selectedItem["item_id"].ToString();


                    var FLA = pumps.AsEnumerable()
                           .FirstOrDefault(row => row["item_title"].ToString() == "FLA" && row["item_id"].ToString() == id)?["item_value"].ToString();

                    var Voltage = pumps.AsEnumerable()
                                      .FirstOrDefault(row => row["item_title"].ToString() == "VOLTAGE" && row["item_id"].ToString() == id)?["item_value"].ToString();


                    if (tabControl2.SelectedTab.Controls[0] is ItemSetUC currentControl2)
                    {
                        currentControl2.SetFinalPumpData(FLA, Voltage);
                    }

                }
                else
                {
                    MessageBox.Show("Invalid selection", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
        private void btn_new_version_Click(object sender, EventArgs e)
        {
            GetLatestDate();
            SetNewFormMode(true);
            isNewRecord = true;


            Panel[] panels = { pnl_header, pnl_footer };
            Helpers.ResetReadOnlyControls(panels);

            //pnl_header.Enabled = true;
            //pnl_footer.Enabled = true;
            Panel[] pnl_list = { pnl_header, pnl_footer };
            Helpers.ResetReadOnlyControls(pnl_list);

            //toolstrip_quotation.Enabled = false;
            dgv_quick_quote_details.Enabled = true;

            txt_version_no.Text = GetNextVersionNo(allTransactionList, txt_document_no.Text);
        }
        private string GetNextVersionNo(DataTable allTransactions, string rawDocNo)
        {
            // Remove "Q#" prefix if present
            string documentNo = rawDocNo.StartsWith("Q#")
                ? rawDocNo.Substring(2)
                : rawDocNo;

            // Get the latest matching row by version_no
            var latestRow = allTransactions.AsEnumerable()
                .Where(row => row["document_no"].ToString() == documentNo)
                .OrderByDescending(row => Convert.ToInt32(row["version_no"]))
                .FirstOrDefault();

            if (latestRow != null && int.TryParse(latestRow["version_no"].ToString(), out int latestVersion))
            {
                return (latestVersion + 1).ToString();
            }

            return "1";
        }
        private string GetNextSubVersionNo(DataTable allTransactions, string rawDocNo, string rawVersionNo)
        {
            string versionNo = rawVersionNo;

            // Remove "Q#" prefix if present
            string documentNo = rawDocNo.StartsWith("Q#")
              ? rawDocNo.Substring(2)
              : rawDocNo;

            // Filter by both document_no and version_no
            var latestRow = allTransactions.AsEnumerable()
                .Where(row => row["document_no"].ToString() == documentNo &&
                              row["version_no"].ToString() == rawVersionNo)
                .OrderByDescending(row => Convert.ToInt32(row["sub_version_no"]))
                .FirstOrDefault();

            if (latestRow != null && int.TryParse(latestRow["sub_version_no"].ToString(), out int latestSubVersion))
            {
                return (latestSubVersion + 1).ToString();
            }

            return "0";
        }
        private void btn_cancel_Click(object sender, EventArgs e)
        {

            Panel[] panels = { pnl_header, pnl_footer };
            Helpers.ReadOnlyControls(panels);

            //pnl_header.Enabled = false;
            //pnl_footer.Enabled = false;

            toolstrip_quotation.Enabled = true;
            SelectedRowIndex = 0;
        }
        private bool isProject = false;
        private void btn_next_Click(object sender, EventArgs e)
        {
            if (!isProject)
            {
                int rowCount = transactionList.Rows.Count;
                if (SelectedRow < rowCount - 1)
                {
                    SelectedRow++;

                    bind(true);
                }
            }
            else
            {
                int rowCount = dt_multiplier.Rows.Count;
                if (selectedProject < rowCount - 1)
                {
                    selectedProject++;
                    fetchSalesProject();
                }
            }
        }
        private void btn_prev_Click(object sender, EventArgs e)
        {
            if (!isProject)
            {
                if (SelectedRow >= 1)
                {
                    SelectedRow--;
                    bind(true);
                }
            }
            else
            {
                if (selectedProject >= 1)
                {
                    selectedProject--;
                    fetchSalesProject();
                }
            }
        }


        DataTable PerCustomerAddressList = new DataTable();
        private void btn_add_customer_Click(object sender, EventArgs e)
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

                    var GeneralBpi = Helpers.FilterDataTable(bpi_general, id, "general_based_id");
                    var BillAddress = Helpers.FilterDataTable(bpi_address, id, "address_based_id");
                    var ShipAddress = Helpers.FilterDataTable(bpi_address, id, "address_based_id");

                    //cmb_ship_to.DataSource = ShipAddress;
                    //cmb_ship_to.DisplayMember = "location";
                    //cmb_ship_to.ValueMember = "address_ids";

                    //cmb_bill_to.DataSource = BillAddress;
                    //cmb_bill_to.DisplayMember = "location";
                    //cmb_bill_to.ValueMember = "address_ids";

                    LoadCustomerShipAddress(id);
                    LoadCustomerBillAddress(id);

                    Helpers.BindControls(pnl_list, GeneralBpi);
                    Helpers.ResetReadOnlyControls(pnl_list);
                    txt_version_no.Text = "1";
                    txt_sub_version_no.Text = "0";
                    txt_version_no.ReadOnly = true;
                    txt_sub_version_no.ReadOnly = true;
                    txt_document_no.ReadOnly = true;
                }
            }
        }

        private async void btn_search_Click(object sender, EventArgs e)
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
                    await fetchQuotationDetails();
                }
            }
        }

        public List<string> fetchMultiplierData()
        {
            List<string> multiplier = new List<string>();

            foreach (DataGridViewRow row in dgv_project_multiplier.Rows)
            {
                if (row.Cells[3].Value != null)
                {
                    multiplier.Add(row.Cells[3].Value.ToString());
                }
            }
            return multiplier;
        }

        private static class QuickQuoteDGV
        {
            public static int QTY = 5;
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
                dgv_quick_quote_details.DataSource = dataview;
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
            //ProjectTest s = new ProjectTest();
            ProjectTemplateSetup s = new ProjectTemplateSetup(ItemList);
            s.Show();
        }
        private void txt_cash_discount_TextChanged(object sender, EventArgs e)
        {
            // add the discount here soon

        }
        private void txt_cash_discount_DoubleClick(object sender, EventArgs e)
        {
            computationLoop();
        }
        private void tabControl2_DrawItem(object sender, DrawItemEventArgs e)
        {
            if (tabControl2.TabPages.Count == 0 || e.Index >= tabControl2.TabPages.Count) return;

            TabControl tabControl = sender as TabControl;
            if (tabControl == null) return;

            TabPage tabPage = tabControl.TabPages[e.Index];
            Rectangle tabBounds = tabControl.GetTabRect(e.Index);

            // Get custom color from Tag, default is Gray
            Color tabColor = tabPage.Tag is Color color ? color : Color.White;

            using (Brush brush = new SolidBrush(tabColor))
            {
                e.Graphics.FillRectangle(brush, tabBounds);
            }

            // Draw Text
            TextRenderer.DrawText(
                e.Graphics,
                tabPage.Text,
                tabControl.Font,
                tabBounds,
                Color.Black, // Text color (change if needed)
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter
            );
        }
        private void tabControl2_MouseDown(object sender, MouseEventArgs e)
        {
            if (tabControl2.TabPages.Count == 0) return;


            if (e.Button == MouseButtons.Right)
            {
                for (int i = 0; i < tabControl2.TabPages.Count; i++)
                {
                    Rectangle tabRect = tabControl2.GetTabRect(i);

                    if (tabRect.Contains(e.Location))
                    {
                        Color currentColor = tabControl2.TabPages[i].Tag is Color ? (Color)tabControl2.TabPages[i].Tag : Color.White;

                        if (currentColor == Color.Red)
                        {
                            tabControl2.TabPages[i].Tag = Color.White;
                        }
                        else
                        {
                            tabControl2.TabPages[i].Tag = Color.Red;
                        }


                        tabControl2.Invalidate();
                        break;
                    }
                }
            }

            var lastIndex = this.tabControl2.TabCount - 1;
            if (this.tabControl2.GetTabRect(lastIndex).Contains(e.Location))
            {
                // Create a new TabPage

                string tabs = string.Empty;
                ItemSetModal modal = new ItemSetModal();
                DialogResult r = modal.ShowDialog();


                if (r == DialogResult.OK)
                {
                    tabs = modal.GetResult();
                }

                if (string.IsNullOrWhiteSpace(tabs))
                {
                    MessageBox.Show("Cannot save tab without a name please type again.");
                    return;
                }

                var newTabPage = new TabPage(tabs);

                // Create an instance of your UserControl
                ItemSetUC myControl = new ItemSetUC
                {
                    Dock = DockStyle.Fill,
                    BackColor = Color.White

                };

                // Add the UserControl to the new tab
                newTabPage.Controls.Add(myControl);

                // Insert the new TabPage into the TabControl
                this.tabControl2.TabPages.Insert(lastIndex, newTabPage);

                // Select the new tab
                this.tabControl2.SelectedIndex = lastIndex;
            }

        }
        private void tabControl2_MouseDoubleClick(object sender, MouseEventArgs e)
        {


        }
        private void button2_Click(object sender, EventArgs e)
        {
            if (panel2.Visible == true)
            {
                panel2.Visible = false;
                pnl_project_name.Height = 34;
            }
            else
            {
                panel2.Visible = true;
                pnl_project_name.Height = 225;
            }
        }
        public bool IsEdit { get; private set; }
        public bool isSubVersion { get; private set; }
        private async void timer1_Tick(object sender, EventArgs e)
        {
            timer1.Stop();

            if (tabControl2.SelectedTab.Controls[0] is ItemSetUC currentControl)
            {


                Dictionary<string, dynamic> data = new Dictionary<string, dynamic>();
                var updatedContentsData = currentControl.GetProjectContentsData();
                data["Branch"] = "Sales";
                data["ProjectId"] = this.selectedProjectID;
                data["sales_project_content"] = updatedContentsData;


                await SendMessageAsync(data);

            }
        }
        private void button3_Click(object sender, EventArgs e)
        {
            TemplateSelectionModal sm = new TemplateSelectionModal();
            sm.Show();
        }
        private void dgv_project_multiplier_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            fetchProjectMultipliers();
        }
        private void fetchProjectMultipliers()
        {
            List<string> multiply = fetchMultiplierData();

            if (tabControl2.SelectedTab.Controls[0] is ItemSetUC currentControl)
            {
                currentControl.setMultiplier(multiply);
            }
        }
        private void btn_finalize_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Are you sure you want to finalize?", "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                FinalizeQuickQuotation();
            }
        }
        private async void FinalizeQuickQuotation()
        {
            try
            {
                int quotationId = Convert.ToInt32(txt_id.Text);

                var finalizeData = new Dictionary<string, dynamic>
                {
                    ["id"] = quotationId,
                    ["is_finalized"] = true
                };

                await QuotationService.Update(finalizeData);

                Helpers.ResetControls(pnl_header);
                //Helpers.ResetControls(pnl_footer);
                ResetControls(pnl_footer);

                Panel[] panel = { pnl_header, pnl_footer };

                //Helpers.ResetControls(panel);

                MessageBox.Show("Quotation successfully finalized.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);

                await fetchQuotationDetails();
                SetFormEditMode("Close");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private void btn_sales_order_Click(object sender, EventArgs e)
        {
            string documentNo = txt_document_no.Text.Trim();  // Assuming you have a document_no textbox in Quotation

            if (string.IsNullOrEmpty(documentNo))
            {
                MessageBox.Show("Please enter a valid document number.");
                return;
            }

            // Create an instance of Orders user control
            Orders ordersPage = new Orders(documentNo);
            ordersPage.Width = this.Parent.Width;
            this.Parent.Controls.Add(ordersPage);
            this.Hide();
        }
        private void btn_print_Click(object sender, EventArgs e)
        {
            string documentNo = txt_document_no.Text.Trim();
            string proj = txt_project_name.Text.Trim();
            if (string.IsNullOrEmpty(proj))
            {
                SalesPrintModal printPage = new SalesPrintModal(true, false, documentNo);
                int screenHeight = Screen.PrimaryScreen.Bounds.Height;
                printPage.Height = (int)(screenHeight);
                printPage.StartPosition = FormStartPosition.CenterParent;
                printPage.ShowDialog();
            }
            else
            {
                SalesPrintModal printPage = new SalesPrintModal(false, true, documentNo);
                int screenHeight = Screen.PrimaryScreen.Bounds.Height;
                printPage.Height = (int)(screenHeight);
                printPage.StartPosition = FormStartPosition.CenterParent;
                printPage.ShowDialog();
            }
        }
        private async void FetchQuotationDetailsByDocumentNo(string documentNo, string version_no = null, string sub_version_no = null)
        {
            SalesQuotationList data = await QuotationService.GetQuotations();
            var itemData = await ItemService.GetItem();
            ItemList = JsonHelper.ToDataTable(itemData.items);

            if (data == null || string.IsNullOrEmpty(documentNo))
            {
                return;
            }

            var filteredSalesQuotation = data.SalesQuotation
                .Where(q => q.document_no == documentNo &&
                           (version_no == null || q.version_no == version_no) &&
                           (sub_version_no == null || q.sub_version_no == sub_version_no))
                .ToList();

            var quotationId = filteredSalesQuotation.FirstOrDefault()?.id;

            if (quotationId != null)
            {
                var filteredSalesQuotationQuick = data.SalesQuotationQuick
                    .Where(q => q.based_id == quotationId)
                    .ToList();

                transactionList = JsonHelper.ToDataTable(filteredSalesQuotation);
                childList = JsonHelper.ToDataTable(filteredSalesQuotationQuick);


                Panel[] panels = { pnl_header, pnl_footer };
                Helpers.ResetReadOnlyControls(panels);

                //pnl_header.Enabled = true;
                //pnl_footer.Enabled = true;
                toolstrip_quotation.Enabled = false;
                dgv_quick_quote_details.Enabled = true;

                toolstrip_quotation.Enabled = true;
                if (filteredSalesQuotation.Any() || filteredSalesQuotationQuick.Any())
                {
                    bind(true);
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
        private void back_Click(object sender, EventArgs e)
        {
            Opportunities OpportunitiesPage = new Opportunities();
            OpportunitiesPage.Width = this.Parent.Width;
            this.Parent.Controls.Add(OpportunitiesPage);
            this.Dispose();
        }
        private void btn_request_for_engr_Click(object sender, EventArgs e)
        {
            ProjectTest pt = new ProjectTest();
            pt.Show();

        }
        private void btn_update_Click(object sender, EventArgs e)
        {
            IsEdit = true;
            MessageBox.Show("EDIT MODE ON");
        }
        private void LoadApplicationSetup()
        {
            cmb_application.DataSource = CacheData.ApplicationSetup;
            cmb_application.DisplayMember = "code";
            cmb_application.ValueMember = "id";
        }
        private void LoadPurposeSetup()
        {
            cmb_purpose.DataSource = STATIC_QUOTATION_PURPOSE.LIST();
            cmb_purpose.DisplayMember = "code";
            cmb_purpose.ValueMember = "title";
        }
        private void LoadShipTypeSetup()
        {
            cmb_ship_type.DataSource = CacheData.ShipTypeSetup;
            cmb_ship_type.DisplayMember = "ship_name";
            cmb_ship_type.ValueMember = "id";
        }
        private void LoadCustomerBillAddress(string id)
        {
            var BillAddress = Helpers.FilterDataTable(bpi_address, id, "address_based_id");

            cmb_bill_to.DataSource = BillAddress;
            cmb_bill_to.DisplayMember = "location";
            cmb_bill_to.ValueMember = "address_ids";
        }
        private void LoadCustomerShipAddress(string id)
        {
            var ShipAddress = Helpers.FilterDataTable(bpi_address, id, "address_based_id");

            cmb_ship_to.DataSource = ShipAddress;
            cmb_ship_to.DisplayMember = "location";
            cmb_ship_to.ValueMember = "address_ids";

        }

        private void btn_edit_Click(object sender, EventArgs e)
        {
            string customerId = txt_customer_id.Text;

            GetLatestDate();
            LoadCustomerBillAddress(customerId);
            LoadCustomerShipAddress(customerId);

            string docNo = txt_document_no.Text;

            if (string.IsNullOrEmpty(docNo))
            {
                MessageBox.Show("Document No is empty.");
                return;
            }

            isSubVersion = true;
            SetFormEditMode(docNo.StartsWith("Q#") ? "Finalize" : "Order");


            txt_sub_version_no.Text = GetNextSubVersionNo(allTransactionList, txt_document_no.Text, txt_version_no.Text);

            // New Quick Quote
            if (!isProject)
            {

                // resets the datasource so that only customers would specific address would be seen.

                bs_bill_to.DataSource = null;
                bs_ship_to.DataSource = null;
                bs_unit.DataSource = CacheData.UoM;
                Panel[] pnls = { pnl_header, pnl_footer };
                Helpers.ReadOnlyControls(pnls);
                dgv_quick_quote_details.ReadOnly = false;
                txt_cash_discount.ReadOnly = false;
                txt_additional_discount.ReadOnly = false;


                foreach (Control ctrl in pnl_footer.Controls)
                {
                    if (ctrl is TextBox)
                    {
                        TextBox txtBox = (TextBox)ctrl;
                        txtBox.Text = "0";
                    }
                }

                //To retain the value from viewing to editing
                DataTable dgvCopy = Helpers.GetDataTableFromUnboundGrid(dgv_quick_quote_details);

                dgv_quick_quote_details.DataSource = dgvCopy;


                DocumentIncrementer();

                txt_created_by.Text = CacheData.CurrentUser.first_name + " " + CacheData.CurrentUser.last_name;
                txt_vat_percent.Text = "12";
                txt_vat_percent.ReadOnly = true;
                btn_add_customer.Enabled = false;
                btn_save.Enabled = true;

            }

            counterReference = 0;
            SelectedRowIndex = 0;

        }
        private void GetLatestDate()
        {
            dtp_date.Value = DateTime.Now;
        }
        private void ChangeDocumentType()
        {

        }
        private void SetNewFormMode(bool isTrue)
        {
            // Hide action buttons
            btn_new.Visible = !isTrue;
            btn_duplicate.Visible = !isTrue;
            btn_new_version.Visible = !isTrue;
            btn_search.Visible = !isTrue;
            btn_prev.Visible = !isTrue;
            btn_next.Visible = !isTrue;
            btn_edit.Visible = !isTrue;
            btn_update.Visible = !isTrue;
            btn_print.Visible = !isTrue;
            btn_finalize.Enabled = !isTrue;

            // Show action button
            btn_savee.Visible = isTrue;
            btn_close.Visible = isTrue;
            dgv_quick_quote_details.Enabled = isTrue;

        }
        private void SetFormEditMode(string mode)
        {
            // Hide all action buttons by default
            btn_new.Visible = false;
            btn_duplicate.Visible = false;
            btn_new_version.Visible = false;
            btn_search.Visible = false;
            btn_prev.Visible = false;
            btn_next.Visible = false;
            btn_edit.Visible = false;
            btn_update.Visible = false;
            btn_print.Visible = false;

            // Enable editing controls
            btn_savee.Visible = true;
            btn_close.Visible = true;

            Panel[] panels = { pnl_header, pnl_footer };
            Helpers.ResetReadOnlyControls(panels);

            //pnl_header.Enabled = true;
            //pnl_footer.Enabled = true;
            dgv_quick_quote_details.Enabled = true;
            dgv_quick_quote_details.ReadOnly = false;

            // Mode-specific logic
            if (mode == "Finalize")
            {
                btn_finalize.Enabled = false;
            }
            else if (mode == "Order")
            {
                btn_sales_order.Enabled = true;
            }
            else // Default mode: view-only
            {
                btn_new.Visible = true;
                btn_duplicate.Visible = true;
                btn_new_version.Visible = true;
                btn_search.Visible = true;
                btn_prev.Visible = true;
                btn_next.Visible = true;
                btn_edit.Visible = true;
                btn_update.Visible = true;
                btn_print.Visible = true;

                btn_savee.Visible = false;
                btn_close.Visible = false;
                Helpers.ReadOnlyControls(panels);
                //pnl_header.Enabled = false;
                //pnl_footer.Enabled = false;
                dgv_quick_quote_details.Enabled = false;
                dgv_quick_quote_details.ReadOnly = true;
            }
        }
        private async void btn_close_Click(object sender, EventArgs e)
        {
            SetNewFormMode(false);
            SetFormEditMode("Close");

            await LoadExistingRecord();

            Panel[] panels = { pnl_header, pnl_footer };
            Helpers.ReadOnlyControls(panels);
            //pnl_header.Enabled = false;
            //pnl_footer.Enabled = false;

            toolstrip_quotation.Enabled = true;

        }
        private void btn_duplicate_Click(object sender, EventArgs e)
        {
            GetLatestDate();
            isNewRecord = true;
        }
        private void ComputeQuickQuoteTotal()
        {



        }

        private void DeleteRowsByReferenceCode(int RowIndex, DataGridView dgv)
        {

            string referenceCode = dgv.Rows[RowIndex].Cells["reference_code"].Value.ToString();

            if (dgv.DataSource is DataTable dataSource)
            {


                // Collect rows to delete (avoid modifying collection while iterating)
                var rowsToDelete = new List<DataRow>();
                foreach (DataRow row in dataSource.Rows)
                {
                    var refCode = row.Table.Columns.Contains("reference_code") ? row["reference_code"]?.ToString() : null;
                    if (!string.IsNullOrEmpty(refCode) &&
                        (refCode == referenceCode || refCode.StartsWith(referenceCode + ".")))
                    {
                        rowsToDelete.Add(row);
                    }
                }

                // Remove rows
                foreach (var row in rowsToDelete)
                {
                    dataSource.Rows.Remove(row);
                }

                // Optionally refresh DataGridView
                //dgv.Refresh();
            }
        }

        private void txt_short_description_TextChanged(object sender, EventArgs e)
        {
            UpdateTextDescription();
        }

        private void UpdateTextDescription()
        {
            dgv_quick_quote_details.Rows[SelectedRowIndex].Cells["short_description"].Value = txt_short_description.Text;
        }

        private void ComputeDgvHierarchy()
        {
            // (Move the entire "hierarchy" ComputeDgv implementation here)
            try
            {
                var dgv = dgv_quick_quote_details;

                // Helper to get cell by preferred names
                Func<DataGridViewRow, string[], object> GetCell = (row, names) =>
                {
                    foreach (var name in names)
                    {
                        if (dgv.Columns.Contains(name))
                            return row.Cells[name].Value;
                    }
                    return null;
                };

                // Build list of rows that have a reference_code
                var rows = new List<(DataGridViewRow Row, string Ref, int Depth, int Index)>();
                for (int i = 0; i < dgv.Rows.Count; i++)
                {
                    var row = dgv.Rows[i];
                    if (row.IsNewRow) continue;

                    var refObj = GetCell(row, new[] { "reference_code", "quick_reference_code", "ref" });
                    string reference = refObj?.ToString();
                    if (string.IsNullOrWhiteSpace(reference))
                        continue;

                    int depth = reference.Count(countRow => countRow == '.') + 1;
                    rows.Add((row, reference, depth, i));
                }

                if (!rows.Any())
                    return;

                // Sort by depth descending so we compute leaves first
                var sorted = rows.OrderByDescending(row => row.Depth).ThenBy(row => row.Ref).ToList();

                // Will store computed unit price per reference (as unit price PER UNIT)
                var computedUnitPrice = new Dictionary<string, decimal>();
                var computedQty = new Dictionary<string, decimal>(); // helpful for parents if needed

                // Pre-fill computedQty from the rows
                foreach (var t in sorted)
                {
                    var qtyObj = GetCell(t.Row, new[] { "quick_qty", "qty" });
                    decimal qty = 1;
                    if (qtyObj != null && decimal.TryParse(qtyObj.ToString(), out decimal qv))
                        qty = qv;
                    computedQty[t.Ref] = qty;
                }

                // Process deepest first
                foreach (var entry in sorted)
                {
                    var row = entry.Row;
                    string reference = entry.Ref;
                    int depth = entry.Depth;

                    // Find immediate children (depth + 1)
                    var immediateChildren = sorted
                        .Where(r => r.Ref.StartsWith(reference + ".") && r.Depth == depth + 1)
                        .ToList();

                    // Get man_days and labor_rate (if present)
                    decimal manDays = 0, laborRate = 0;
                    var manDaysObj = GetCell(row, new[] { "man_days" });
                    var laborRateObj = GetCell(row, new[] { "labor_rate" });
                    if (manDaysObj != null) decimal.TryParse(manDaysObj.ToString(), out manDays);
                    if (laborRateObj != null) decimal.TryParse(laborRateObj.ToString(), out laborRate);

                    decimal laborCost = manDays * laborRate;

                    // Sum immediate children cost (childUnitPrice * childQty) using previously computed child unit prices
                    decimal childrenSum = 0;
                    foreach (var child in immediateChildren)
                    {
                        if (computedUnitPrice.TryGetValue(child.Ref, out decimal childUnit))
                        {
                            decimal childQty = computedQty.ContainsKey(child.Ref) ? computedQty[child.Ref] : 1m;
                            childrenSum += childUnit * childQty;
                        }
                        else
                        {
                            // fallback: attempt to read child's unit price cell
                            var childUnitObj = GetCell(child.Row, new[] { "quick_unit_price", "unit_price" });
                            if (childUnitObj != null && decimal.TryParse(Helpers.GetCleanedPriceValue(childUnitObj.ToString()), out decimal cu))
                            {
                                decimal childQty = computedQty.ContainsKey(child.Ref) ? computedQty[child.Ref] : 1m;
                                childrenSum += cu * childQty;
                            }
                        }
                    }

                    // Determine base unit price for this row
                    // Priority:
                    // 1) If row is a parent (has children or has man_days/labor_rate) -> base = childrenSum + laborCost (if none, laborCost may be zero)
                    // 2) else if quick_list_price exists -> use it
                    // 3) else if quick_unit_price exists -> use it
                    // 4) else try unit_price (bound column)
                    decimal baseUnit = 0m;
                    bool isParent = immediateChildren.Any() || (manDays > 0 && laborRate > 0);

                    if (isParent)
                    {
                        baseUnit = childrenSum + laborCost;
                    }
                    else
                    {
                        // try quick_list_price
                        var listPriceObj = GetCell(row, new[] { "quick_list_price", "list_price" });
                        if (listPriceObj != null && decimal.TryParse(Helpers.GetCleanedPriceValue(listPriceObj.ToString()), out decimal lp))
                        {
                            baseUnit = lp;
                        }
                        else
                        {
                            var unitPriceObj = GetCell(row, new[] { "quick_unit_price", "unit_price" });
                            if (unitPriceObj != null && decimal.TryParse(Helpers.GetCleanedPriceValue(unitPriceObj.ToString()), out decimal upv))
                            {
                                baseUnit = upv;
                            }
                        }
                    }

                    // Apply VAT multiplier for parents (as per sample): 1.186
                    decimal finalUnitPrice = baseUnit;
                    if (isParent)
                        finalUnitPrice = baseUnit * 1.186m;

                    // Save computed unit price for parent aggregation
                    computedUnitPrice[reference] = finalUnitPrice;

                    // Write back unit price to grid cell
                    if (dgv.Columns.Contains("quick_unit_price"))
                        row.Cells["quick_unit_price"].Value = finalUnitPrice.ToString("C2");
                    else if (dgv.Columns.Contains("unit_price"))
                        row.Cells["unit_price"].Value = finalUnitPrice.ToString("C2");

                    // Now compute line values using DGVComputation
                    var qtyObjRow = GetCell(row, new[] { "quick_qty", "qty" });
                    int qtyInt = 0;
                    if (qtyObjRow != null && int.TryParse(qtyObjRow.ToString(), out int qint))
                        qtyInt = qint;
                    else
                        qtyInt = 1;

                    var discountObj = GetCell(row, new[] { "quick_discount", "percent_discount", "discount" });
                    string discountStr = discountObj?.ToString() ?? "0";

                    // Use unit price (numeric) for computation
                    DGVComputation dgvComputation = new DGVComputation(qtyInt, finalUnitPrice, discountStr);
                    dgvComputation.ComputeQuickQuote();

                    // Write computed fields
                    if (dgv.Columns.Contains("quick_net_total"))
                        row.Cells["quick_net_total"].Value = dgvComputation.NetAmount.ToString("C2");
                    if (dgv.Columns.Contains("quick_net_discount"))
                        row.Cells["quick_net_discount"].Value = dgvComputation.NetDiscount.ToString("C2");
                    if (dgv.Columns.Contains("quick_line_total"))
                        row.Cells["quick_line_total"].Value = dgvComputation.LineTotal.ToString("C2");
                }

                // After recomputing hierarchy, update overall totals
                computationLoop();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message);
            }
        }

        private bool isUpdatingHierarchy = false;

        private void dgv_quick_quote_details_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {


            if (isUpdatingHierarchy) 
                return;

            isUpdatingHierarchy = true;
            ComputeByReferenceHierarchy();
            ComputeReferenceNonHierarchy();
            ComputeFooterTotals();
            isUpdatingHierarchy = false;
        }


        private void ComputeReferenceNonHierarchy()
        {

            foreach (DataGridViewRow row in dgv_quick_quote_details.Rows)
            {
                if (row.IsNewRow) continue;

                var referenceCode = row.Cells["reference_code"].Value?.ToString();
                if (string.IsNullOrWhiteSpace(referenceCode) || referenceCode.Contains("."))
                    continue;

                if (row.Cells["quick_qty"].Value == null || string.IsNullOrEmpty(row.Cells["quick_qty"].Value.ToString()) ||
                    row.Cells["quick_unit_price"].Value == null || string.IsNullOrEmpty(row.Cells["quick_unit_price"].Value.ToString()))
                    continue;

                decimal unitPrice = Convert.ToDecimal(Helpers.GetCleanedPriceValue(row.Cells["quick_unit_price"].Value.ToString()));
                decimal discount = CalculateDiscountMultiplier(row.Cells["quick_discount"].Value?.ToString());
                decimal qty = Convert.ToDecimal(row.Cells["quick_qty"].Value);
                decimal TotalUnitPrice = unitPrice * qty;
                decimal discounted = TotalUnitPrice * discount;
                decimal netDiscount = discounted - TotalUnitPrice;
                decimal netTotal = TotalUnitPrice;

                row.Cells["quick_line_total"].Value = discounted;
                row.Cells["quick_net_total"].Value = netTotal;
            }
        }

        private void ComputeFooterTotals()
        {
            decimal grossSalesTotal = 0m;
            decimal netSalesTotal = 0m;
            foreach (DataGridViewRow row in dgv_quick_quote_details.Rows)
            {
                if (row.IsNewRow) continue;

                var referenceCode = row.Cells["reference_code"].Value?.ToString();
                if (!string.IsNullOrWhiteSpace(referenceCode) && !referenceCode.Contains("."))
                { 
                    grossSalesTotal += decimal.Parse(Helpers.GetCleanedPriceValue(row.Cells["quick_net_total"].Value.ToString()));
                    netSalesTotal += decimal.Parse(Helpers.GetCleanedPriceValue(row.Cells["quick_line_total"].Value.ToString()));
                }

            }


            if (grossSalesTotal != 0)
            {
                decimal percentDiscount = ((grossSalesTotal - netSalesTotal) / grossSalesTotal) * 100;

                //if the pecentage go below 0 it will set to 0
                percentDiscount = (percentDiscount <= 0) ? 0 : percentDiscount;

                txt_percent_discount.Text = percentDiscount.ToString();
            }

            decimal netSalesWithVat = netSalesTotal * 0.12m;

            //txt_additional_discount.Text = txt_additional_discount.Text != "" ? txt_additional_discount.Text : "0%";

            string AdditionalDiscountString = txt_additional_discount.Text.Replace('%', ' ').TrimEnd();
            decimal AdditionalDiscount = decimal.Parse(AdditionalDiscountString != "" ? AdditionalDiscountString : "0");

             AdditionalDiscount = AdditionalDiscount / 100;

            decimal DiscountedTotal = netSalesTotal * AdditionalDiscount;

            decimal NetAmountDue = (netSalesTotal - DiscountedTotal) + netSalesWithVat;

            decimal TotalAmountDue = NetAmountDue - decimal.Parse(txt_cash_discount.Text != "" ? txt_cash_discount.Text : "0");

            txt_gross_sales.Text = Helpers.FormatAsCurrency(grossSalesTotal.ToString());
            txt_net_sales.Text = Helpers.FormatAsCurrency(netSalesTotal.ToString());
            txt_vat_amount.Text = Helpers.FormatAsCurrency(netSalesWithVat.ToString());
            txt_net_amount_due.Text = Helpers.FormatAsCurrency(NetAmountDue.ToString());
            txt_total_amount_due.Text = Helpers.FormatAsCurrency(TotalAmountDue.ToString());
        }

        private void canvasToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // Canvass Sheet Column
            string id = dgv_quick_quote_details.Rows[SelectedRowIndex].Cells[5].Value.ToString();
            HandleCanvasSelectionClick(SelectedRowIndex, id);
        }

        private void quotationTerms()
        {
            //Set Quotation Terms from Company Data Table
            //Hardcoded to company id 1 for now
            var SelectedCompany = Company.AsEnumerable()
                .FirstOrDefault(row => row.Field<int>("id") == 1);

            foreach (DataRow row in SelectedCompany.Table.Rows)
            {
                if (row.Field<int>("id") == 1)
                {
                    string inclusions = row.Field<string>("InclusionsQuotationTerms");
                    InclusionsRichTextBox.Text = inclusions;
                    string exclusions = row.Field<string>("ExclusionsQuotationTerms");
                    ExclusionsRichTextBox.Text = exclusions;
                    string terms_and_conditions = row.Field<string>("TermAndConditions");
                    TermAndConditionsRichTextBox.Text = terms_and_conditions;
                }
            }


            //Styling Inclusions Rich Text Box
            ColorSelectedAndUnderlineWordsAndBold(InclusionsRichTextBox, "(PLACE)", Color.Blue);
            UnderlineWords(InclusionsRichTextBox, "during company regular working hours only.");
            ColorSelectedAndUnderlineWordsAndBold(InclusionsRichTextBox, "3 DAYS", Color.Black);
            BoldWords(InclusionsRichTextBox, "want more than the allowable and beyond working hours, additional charges will be applied.");

            //Styling Exclusions Rich Text Box
            MakeAllTextBlue(ExclusionsRichTextBox);

            //Styling Terms and Conditions Rich Text Box 
            BoldWords(TermAndConditionsRichTextBox, "PAYMENT TERMS:");
            ColorSelectedAndUnderlineWordsAndBold(TermAndConditionsRichTextBox, "CASH ON DELIVERY", Color.Blue);
            BoldWords(TermAndConditionsRichTextBox, "QUOTATION VALIDITY");
            BoldAndUnderlineWords(TermAndConditionsRichTextBox, "30 DAYS");
            BoldWords(TermAndConditionsRichTextBox, "thereafter, it shall be subject to reconfirmation");
            BoldWords(TermAndConditionsRichTextBox, "AVAILABILITY OF STOCK(S) AND/OR SERVICE(S): ");
            ColorSelectedAndUnderlineWordsAndBold(TermAndConditionsRichTextBox, "4-6 MONTHS", Color.Blue);
            BoldWords(TermAndConditionsRichTextBox, "DELIVERY TERMS:");
            ColorSelectedAndUnderlineWordsAndBold(TermAndConditionsRichTextBox, "WAREHOUSE TO SITE VIA SEA (w/o HAULING).", Color.Blue);
            BoldWords(TermAndConditionsRichTextBox, "OTHER CHARGES, TITLE, RISK OF LOSS:");
            BoldWords(TermAndConditionsRichTextBox, "within three(3) days");
            BoldWords(TermAndConditionsRichTextBox, "STORAGE:");
            BoldWords(TermAndConditionsRichTextBox, "SALES RETURN / CANCELLATION POLICY:");
            ColorSelectedAndUnderlineWordsAndBold(TermAndConditionsRichTextBox, "(as agreed upon %", Color.Blue);
            ColorSelectedAndUnderlineWordsAndBold(TermAndConditionsRichTextBox, "or fixed", Color.Red);
            BoldWords(TermAndConditionsRichTextBox, "a cancellation fee");
            ColorSelectedAndUnderlineWordsAndBold(TermAndConditionsRichTextBox, "(fixed %) ", Color.Red);
            BoldWords(TermAndConditionsRichTextBox, "WARRANTY:");
            ColorSelectedAndUnderlineWordsAndBold(TermAndConditionsRichTextBox, "ONE (1) YEAR", Color.Blue);
            BoldWords(TermAndConditionsRichTextBox, "SERVICES:");
            BoldWords(TermAndConditionsRichTextBox, "WARRANTY:");

        }

        private void BoldWords(RichTextBox rtb, string wordToBold)
        {
            int startIndex = 0;
            while (wordToBold.Length >= startIndex)
            {
                int wordStartIndex = rtb.Find(wordToBold, startIndex, RichTextBoxFinds.None);
                if (wordStartIndex == -1)
                    break;

                rtb.Select(wordStartIndex, wordToBold.Length);
                rtb.SelectionFont = new Font(rtb.SelectionFont ?? rtb.Font, FontStyle.Bold);

                startIndex = wordStartIndex + wordToBold.Length;
            }
            rtb.Select(0, 0); // Deselect
        }



        private void UnderlineWords(RichTextBox rtb, string wordToUnderline)
        {
            int startIndex = 0;

            while (startIndex < rtb.Text.LastIndexOf(wordToUnderline))
            {
                int wordStartIndex = rtb.Find(wordToUnderline, startIndex, RichTextBoxFinds.None);
                if (wordStartIndex != -1)
                {
                    // Select the word
                    rtb.Select(wordStartIndex, wordToUnderline.Length);

                    // Apply underline to the selection
                    rtb.SelectionFont = new Font(rtb.SelectionFont ?? rtb.Font,
                                                 rtb.SelectionFont.Style | FontStyle.Underline);

                    // Move to the next word
                    startIndex = wordStartIndex + wordToUnderline.Length;
                }
                else
                {
                    break;
                }
            }

            // Deselect text
            rtb.Select(0, 0);
        }

        private void ColorBlueWords(RichTextBox rtb, string wordToUnderline)
        {
            int startIndex = 0;

            while (startIndex < rtb.Text.LastIndexOf(wordToUnderline))
            {
                int wordStartIndex = rtb.Find(wordToUnderline, startIndex, RichTextBoxFinds.None);
                if (wordStartIndex != -1)
                {
                    // Select the word
                    rtb.Select(wordStartIndex, wordToUnderline.Length);

                    // Apply underline to the selection
                    rtb.SelectionColor = Color.Blue;

                    // Move to the next word
                    startIndex = wordStartIndex + wordToUnderline.Length;
                }
                else
                {
                    break;
                }
            }

            // Deselect text
            rtb.Select(0, 0);
        }

        private void MakeAllTextBlue(RichTextBox rtb)
        {
            // Select all text
            rtb.SelectAll();

            // Change color to blue
            rtb.SelectionColor = Color.Blue;
            rtb.SelectionFont = new Font(rtb.SelectionFont ?? rtb.Font, FontStyle.Bold);

            // Deselect everything
            rtb.Select(0, 0);
        }

        private void txt_cash_discount_TextChanged_1(object sender, EventArgs e)
        {
            ComputeFooterTotals();
        }

        private void txt_additional_discount_TextChanged(object sender, EventArgs e)
        {
            int number = 0, value = 0;

            if(int.TryParse(txt_additional_discount.Text, out number))
            {
                value = int.Parse(txt_additional_discount.Text);
            }

            if (value <= 100 && value >= 0)
                ComputeFooterTotals();
            else
                MessageBox.Show("To be able to work the additional Discount. Please enter 100% to 0% only");
        }
        private void BoldAndUnderlineWords(RichTextBox rtb, string word)
        {
            int startIndex = 0;
            while (startIndex < rtb.Text.LastIndexOf(word))
            {
                int wordStartIndex = rtb.Find(word, startIndex, RichTextBoxFinds.None);
                if (wordStartIndex != -1)
                {
                    rtb.Select(wordStartIndex, word.Length);
                    rtb.SelectionFont = new Font(rtb.SelectionFont ?? rtb.Font, FontStyle.Bold | FontStyle.Underline);
                    startIndex = wordStartIndex + word.Length;
                }
                else
                {
                    break;
                }
            }
            rtb.Select(0, 0);
        }
        private void ColorSelectedAndUnderlineWords(RichTextBox rtb, string word, Color SelectecColor)
        {
            int startIndex = 0;
            while (startIndex < rtb.Text.LastIndexOf(word))
            {
                int wordStartIndex = rtb.Find(word, startIndex, RichTextBoxFinds.None);
                if (wordStartIndex != -1)
                {
                    rtb.Select(wordStartIndex, word.Length);
                    rtb.SelectionColor = SelectecColor;
                  
                    rtb.SelectionFont = new Font(rtb.SelectionFont ?? rtb.Font, FontStyle.Underline);
                    startIndex = wordStartIndex + word.Length;
                }
                else
                {
                    break;
                }
            }
            rtb.Select(0, 0);
        }

        private void ColorSelectedAndUnderlineWordsAndBold(RichTextBox rtb, string word, Color SelectecColor)
        {
            int startIndex = 0;
            while (startIndex < rtb.Text.LastIndexOf(word))
            {
                int wordStartIndex = rtb.Find(word, startIndex, RichTextBoxFinds.None);
                if (wordStartIndex != -1)
                {
                    rtb.Select(wordStartIndex, word.Length);
                    rtb.SelectionColor = SelectecColor;

                    rtb.SelectionFont = new Font(rtb.SelectionFont ?? rtb.Font, FontStyle.Bold | FontStyle.Underline);
                    startIndex = wordStartIndex + word.Length;
                }
                else
                {
                    break;
                }
            }
            rtb.Select(0, 0);
        }
    }
}