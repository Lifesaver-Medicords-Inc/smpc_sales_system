using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using smpc_app.Data;
using smpc_app.Services.Helpers;
using smpc_inventory_app.Model;
using smpc_inventory_app.Pages;
using smpc_sales_app.Data;
using smpc_sales_app.Services.Helpers;
using smpc_sales_app.Services.Sales;
using smpc_sales_system.Models;
using smpc_sales_system.Pages;
using smpc_sales_system.Pages.Sales;
using smpc_sales_system.Properties;
using smpc_sales_system.Services.Sales;
using smpc_sales_system.Services.Sales.Models;
using smpc_sales_system.Services.Setup;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net.WebSockets;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using System.Windows.Forms;
using WebSocketSharp;
using System.Text.RegularExpressions;

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

            // Every column dgv_quick_quote_details needs is already defined explicitly in
            // the designer (quick_id, quick_images, reference_code, etc.). With
            // AutoGenerateColumns left at its WinForms default (true), every time the grid
            // gets rebound to a new data source, WinForms silently appends an extra column
            // for any bound field that doesn't already have a matching column - which is
            // exactly what kept producing the stray duplicate "images" column (and, before
            // that, duplicate "quick_images" columns) no matter how much the binding code
            // was patched around it. Turning this off is the actual fix: no more columns
            // will ever be auto-added.
            dgv_quick_quote_details.AutoGenerateColumns = false;

            cmb_warranty.Text = "1 year";
            //KRIS: NEED ITONG DALAWA KAPAG MAY VERSION_NO NA PERO GINAGAMIT KO NA RIN GANYAN
            this.documentNo = documentNo;
            this.versionNo = version_no;
            this.subVersionNo = sub_version_no;
            this.isFinalized = is_finalized;

            // websocket related
            _websocket = new ClientWebSocket();
            _cancelTokenSource = new CancellationTokenSource();

            // Redraw the Change History panel for whichever tab is currently selected - see
            // RenderTabHistory.
            tabControl2.SelectedIndexChanged += tabControl2_SelectedIndexChanged;
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

                    //if (item_data != null && item_data.ContainsKey("items_id") && Convert.ToInt32(item_data["items_id"]) != 0)
                    //{
                    //    MessageBox.Show("update");
                    //    var isSuccess = await ProjectService.UpdateProjectItems(item_data);

                    //    if (isSuccess.Success)
                    //    {
                    //        MessageBox.Show($"Item Updated successfully");
                    //    }
                    //}

                    //if (item_data != null && item_data.ContainsKey("items_id") && Convert.ToInt32(item_data["items_id"]) == 0)
                    //{
                    //    MessageBox.Show("add");
                    //    item_data["items_id"] = "";
                    //    item_data["based_id"] = CurrentProjectItemBasedID;
                    //    var isSuccess = await ProjectService.InsertItems(item_data);

                    //    if (isSuccess.Success)
                    //    {
                    //        MessageBox.Show($"Item Added successfully");
                    //    }
                    //}
                }
            }
        }

        private async void Cell_DataChanged(object sender, EventArgs e)
        {
            if (IsEdit)
            {
                if (tabControl2.SelectedTab.Controls[0] is ItemSetUC currentControl)
                {
                    //var item_data = currentControl.GetProjectItems();
                    //if (item_data != null && item_data.ContainsKey("sales_project_items"))
                    //{
                    //    var salesProjectItems = (List<SalesProjectItems>)item_data["sales_project_items"];

                    //    var itemsToInsert = salesProjectItems
                    //        .Where(item => item.items_id == 0)
                    //        .ToList();

                    //    var itemsToUpdate = salesProjectItems
                    //        .Where(item => item.items_id != 0)
                    //        .ToList();

                    //    if (itemsToUpdate.Any())
                    //    {
                    //        // prepare update
                    //        item_data["sales_project_items"] = itemsToUpdate;
                    //        var updateResult = await ProjectService.UpdateProjectItems(item_data);
                    //        if (updateResult.Success)
                    //            MessageBox.Show("Updated successfully");
                    //        else
                    //            MessageBox.Show(updateResult.message);
                    //    }

                    //    if (itemsToInsert.Any())
                    //    {
                    //        foreach (var item in itemsToInsert)
                    //        {
                    //            item.based_id = CurrentProjectItemBasedID;
                    //        }

                    //        item_data["sales_project_items"] = itemsToInsert;
                    //        var insertResult = await ProjectService.InsertItems(item_data);

                    //        if (insertResult.Success)
                    //            MessageBox.Show("Added successfully");
                    //        else
                    //            MessageBox.Show(insertResult.message);
                    //    }
                    //}
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
                    }
                }
            }
        }

        private async void Cell_ClickedUC(object sender, EventArgs e)
        {
            // Setting DataGridView.ReadOnly (see UpdateProjectControlsEditableState) only
            // blocks typing directly into cells - it does nothing to stop a CellClick handler
            // like this one from firing and opening the item picker, which is how a component
            // (and its MODEL, list price, etc. - everything GetItemData fills in) could still
            // get added while just viewing a project, before Edit/New had been clicked.
            if (isProject && !isNewRecord && !IsEdit)
                return;

            if (tabControl2.SelectedTab.Controls[0] is ItemSetUC currentControl)
            {
                int index = currentControl.GetIndex();

                // counterReference/counterParent live on this form, not on ItemSetUC, so
                // they were being shared across every Project tab - adding an item on Tab A
                // then switching to Tab B would keep counting up from Tab A's numbers
                // instead of Tab B's own. Re-deriving them from the currently selected
                // tab's own grid right before use (same approach as the Quick Quote
                // Edit-numbering fix) makes numbering independent per tab.
                counterReference = GetMaxTopLevelReferenceCode(currentControl.DgvProjectItems);
                counterParent = 1;

                HandleItemSelectionClick(index, currentControl.DgvProjectItems);
            }
        }
        private async void Cell_EditedUC(object sender, EventArgs e)
        {
            RecomputeParentTotals();
        }

        private void RecomputeParentTotals()
        {
            decimal gross = 0, vat = 0, net = 0, percent = 0, cash_disc = 0, net_amount = 0, total_amount = 0;

            foreach (TabPage tab in tabControl2.TabPages)
            {
                // Red-flagged tabs (toggled via the right-click menu) are excluded from the
                // quotation's totals - that's the point of flagging a tab red, to set it
                // aside without deleting it.
                bool isRedFlagged = _redFlaggedTabs.Contains(tab);
                if (isRedFlagged)
                    continue;

                var itemControls = tab.Controls.OfType<ItemSetUC>();

                foreach (var currentControl in itemControls)
                {
                    var data = currentControl.ProjectComputationLoop();
                    if (data == null) continue;

                    Console.WriteLine("Quotation - net sales: " + net);

                    AddDecimal(data, "gross_sales", ref gross);
                    AddDecimal(data, "vat_amount", ref vat);
                    AddDecimal(data, "net_sales", ref net);
                    AddDecimal(data, "percent_discount", ref percent);
                    AddDecimal(data, "cash_discount", ref cash_disc);
                    AddDecimal(data, "net_amount_due", ref net_amount);
                    AddDecimal(data, "total_amount_due", ref total_amount);
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

        private void AddDecimal(Dictionary<string, object> data, string key, ref decimal target)
        {
            if (data.TryGetValue(key, out var val) && val != null)
            {
                if (decimal.TryParse(val.ToString(), out var result))
                {
                    target += result;
                }
            }
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
                        
                        currentControl.SetProjectItemsData(template_data, temp.GetTemplateName());
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
            this.tabControl.Width = System.Windows.Forms.Screen.AllScreens.Length;
            this.Size = new Size(1386 - 80, 950);

            //use state
            isProject = false;
            IsEdit = false;

            UpdateDescriptionFieldsVisibility();

            fetchQuotationDetails();

            Helpers.ResetControls(pnl_header);
            ResetControls(pnl_footer);
        }

        // Short/Long Description (txt_short_description, txt_long_description, and their
        // labels label34/label33) belong to Quick Quote only - Project Quotation has no
        // matching data for them. They live on pnl_footer alongside everything else, so
        // they stay visible whichever tab was active last unless explicitly toggled here.
        private void UpdateDescriptionFieldsVisibility()
        {
            bool show = !isProject;
            label34.Visible = show;
            label33.Visible = show;
            txt_short_description.Visible = show;
            txt_long_description.Visible = show;
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

            UpdateDescriptionFieldsVisibility();

            fetchSalesProjectData();
        }

        // Every SalesProjectHistory row for the whole current project - every tab's item set
        // plus the header-level entries (project name/other top fields, multipliers - see
        // BuildHeaderAutoHistoryEntries), keyed to the quotation's own id. Change History was
        // previously scoped to whichever tab happened to be selected, which hid header/other
        // tabs' entries; it's now a whole-project view, so this is the single source both
        // RenderTabHistory (inline panel) and the "FULL DETAILS" modal read from.
        private List<SalesProjectHistory> GetFullProjectHistory(int quotationId)
        {
            if (quotationId <= 0 || SalesProjectListData == null)
                return new List<SalesProjectHistory>();

            var itemSetIds = new HashSet<uint>(
                (SalesProjectListData.sales_project_item_set ?? new List<SalesProjectItemSet>())
                    .Where(s => s.based_id == quotationId)
                    .Select(s => (uint)s.itemset_id)
            );
            itemSetIds.Add((uint)quotationId);

            return (SalesProjectListData.sales_project_history ?? new List<SalesProjectHistory>())
                .Where(h => itemSetIds.Contains(h.based_id))
                .OrderByDescending(h => h.history_id)
                .ToList();
        }

        // Redraws flowLayoutPanelChangeHistory from real data - this used to just get another
        // copy of a hardcoded mockup control permanently appended on every click of the
        // Project nav button (see btn_project_Click history), which is why the same fake entry
        // could show up duplicated. It's driven by the actual SalesProjectHistory rows for the
        // whole project (not just whichever tab is selected) and redraws whenever the selected
        // tab changes, since that's also when it's convenient to catch a freshly loaded project.
        private void RenderTabHistory()
        {
            flowLayoutPanelChangeHistory.Controls.Clear();

            if (!isProject || tabControl2.SelectedTab == null)
                return;

            int quotationId = ToInt(txt_id.Text);
            var entries = GetFullProjectHistory(quotationId);

            foreach (var entry in entries)
            {
                UC_History h = new UC_History();
                h.SetHistory(entry);

                foreach (Control ctrl in h.Controls)
                {
                    ctrl.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
                }

                flowLayoutPanelChangeHistory.Controls.Add(h);
            }
        }

        // Opens the full, scrollable Change History list for the whole project - the inline
        // panel is small and only meant to give a quick glance.
        private void btn_full_history_Click(object sender, EventArgs e)
        {
            int quotationId = ToInt(txt_id.Text);
            var entries = GetFullProjectHistory(quotationId);

            using (var modal = new ChangeHistoryModal(txt_project_name.Text, entries))
            {
                modal.ShowDialog(this);
            }
        }

        private void tabControl2_SelectedIndexChanged(object sender, EventArgs e)
        {
            RenderTabHistory();
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

            //Apply Quotation Terms and Conditions
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

        SalesProject projectData;
        private async Task fetchQuotationDetails()
        {
            Panel[] panels = { pnl_header, pnl_footer };
            Helpers.ReadOnlyControls(panels);

            //pnl_header.Enabled = false;
            //pnl_footer.Enabled = false;
            toolstrip_quotation.Enabled = false;
            dgv_quick_quote_details.Enabled = false;

            data = await QuotationService.GetQuotations();

            //projectData = await 

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

                // Don't default to row 0 of the full table - that could be someone else's
                // quotation. Land on the first record that's actually the current user's own,
                // and if they don't have any yet, leave the form blank/ready for New instead
                // of showing another user's data.
                List<int> ownedIndexes = GetOwnedRowIndexes(transactionList);

                if (ownedIndexes.Count == 0)
                {
                    MessageBox.Show("You have no saved quotations yet. Click New to create one.");
                    Helpers.ResetReadOnlyControls(panels);
                }
                else
                {
                    SelectedRow = ownedIndexes[0];

                    await Task.Delay(2000); // optional wait
                    bind(transactionList, SelectedRow, true);

                    createFilterViewDgvQuickQouteDetails();
                }

            }
            else
            {
                MessageBox.Show("Please create a new data!");

                Helpers.ResetReadOnlyControls(panels);
                //pnl_header.Enabled = true;
                //pnl_footer.Enabled = true;

            }

            toolstrip_quotation.Enabled = true;
        }
        public DataTable dt_multiplier { get; set; }
        public DataTable dt_content { get; set; }
        public DataTable dt_content_final { get; set; }
        public DataTable dt_advanced_conditions { get; set; }
        public DataTable dt_items { get; set; }
        public DataTable dt_items_selected_images { get; set; }
        public DataTable dt_wiring { get; set; }


        public int CurrentProjectItemBasedID { get; set; }

        private int selectedProjectRow = 0;
        private string selectedProjectID = "0";

        public static DataTable ToDataTable<T>(List<T> items)
        {
            var dataTable = new DataTable(typeof(T).Name);

            // Get public instance properties
            var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (var prop in properties)
            {
                // Handle Nullable types so the DataTable column has the correct base type
                var propType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
                dataTable.Columns.Add(prop.Name, propType);
            }

            if (items != null)
            {
                foreach (var item in items)
                {
                    var row = dataTable.NewRow();
                    foreach (var prop in properties)
                    {
                        // Use DBNull.Value for nulls to avoid errors in DataTable rows
                        row[prop.Name] = prop.GetValue(item) ?? DBNull.Value;
                    }
                    dataTable.Rows.Add(row);
                }
            }

            return dataTable;
        }
        private DataTable ConvertToDataTable<T>(List<T> items)
        {
            DataTable table = new DataTable();
            PropertyInfo[] properties = typeof(T).GetProperties();

            foreach (PropertyInfo prop in properties)
                table.Columns.Add(prop.Name, Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType);

            foreach (T item in items)
            {
                DataRow row = table.NewRow();
                foreach (PropertyInfo prop in properties)
                    row[prop.Name] = prop.GetValue(item) ?? DBNull.Value;
                table.Rows.Add(row);
            }

            return table;
        }

        SalesProjectList SalesProjectListData = new SalesProjectList();
        DataTable transactionProjectDataTable = new DataTable();

        private async Task fetchSalesProjectData()
        {
            Helpers.ResetControls(pnl_header);
            ResetControls(pnl_footer);


            SalesProjectListData = await ProjectService.GetProjects();

            if (SalesProjectListData?.SalesQuotation == null) return;

            var latestQuotations = SalesProjectListData.SalesQuotation
            //.GroupBy(q => q.document_no)
            .Select(group => group)
            .OrderByDescending(q => q.version_no)
            .ToList();

            transactionProjectDataTable = JsonHelper.ToDataTable(latestQuotations);

            // Don't default to row 0 of the full table - that could be someone else's project
            // quotation. Only the current user's own records count here; if there are project
            // quotations in the system but none of them are this user's, treat it the same as
            // "no project data found" below (fresh blank project, ready for New) instead of
            // showing another user's data.
            List<int> ownedProjectIndexes = GetOwnedRowIndexes(transactionProjectDataTable);

            if (SalesProjectListData == null || (SalesProjectListData.sales_project_item_set == null || !SalesProjectListData.sales_project_item_set.Any()) || ownedProjectIndexes.Count == 0)
            {
                MessageBox.Show("No project data found. Creating a new entry.", "Empty Data", MessageBoxButtons.OK, MessageBoxIcon.Information);

                // Get the last index (before the add new tab)
                var lastIndex = this.tabControl2.TabCount - 1;
                // Create a new TabPage
                TabPage newTab = new TabPage("New Project 1");

                // Create an instance of ItemSetUC
                ItemSetUC UC = new ItemSetUC
                {
                    Dock = DockStyle.Fill,
                    BackColor = Color.White
                };
                // So this tab's IMAGES column picker has something to show/filter from -
                // ItemSetUC.ImageList was never being set anywhere, so the picker always
                // came up empty.
                UC.ImageList = this.ImageList;

                // Attach event handlers
                UC.ButtonClicked += Button_ClickedUC;
                UC.DataChangedConditions += ItemSet_DataChanged;
                UC.DataChangedContent += Content_DataChanged;

                UC.UpdateProjectConditions += UpdateProjectConditions;
                UC.UpdateProjectContent += UpdateProjectContent;


                UC.CellChangedProject += Cell_DataChanged;
                UC.CellClicked += Cell_ClickedUC;
                UC.CellClicked += Cell_EditedUC;
                UC.CellClickedModel += CellClickedModelUC;
                //UC.DeleteReferenceCode += DeleteRowsByReferenceCode;

                // Add the UserControl to the new tab
                newTab.Controls.Add(UC);

                // Insert the new tab before the last tab
                this.tabControl2.TabPages.Insert(lastIndex, newTab);

                // Select the newly added tab
                this.tabControl2.SelectedIndex = lastIndex;

                // There's nothing saved yet to view read-only - this is a fresh starting
                // point, so it should be editable right away.
                UC.SetEditable(true);
                Panel[] freshProjectPanels = { pnl_header, pnl_footer, pnl_project_name };
                Helpers.ResetReadOnlyControls(freshProjectPanels);
                dgv_project_multiplier.ReadOnly = false;

                return;
            }

            selectedProjectRow = ownedProjectIndexes[0];
            fetchSalesProject();
        }

        private async void fetchSalesProject()
        {
            // async void with no unhandled exception guard: any error in here (bad
            // grid data, a null cell, etc.) used to propagate as an unhandled
            // exception on the UI thread and crash the whole app instead of just
            // showing an error message, since async void methods can't be awaited
            // or wrapped in a try/catch by their caller.
            try
            {
            if (transactionProjectDataTable.Rows.Count == 0) return;

            string selectedId = this.transactionProjectDataTable.Rows[this.selectedProjectRow]["id"].ToString();
            int selectedIdInt = int.Parse(selectedId);

            // Filter each list using LINQ Where()
            List<SalesQuotationModel> filtered = SalesProjectListData.SalesQuotation
            .Where(x => x.id == selectedIdInt)
            .ToList();

            DataTable transactionData = ConvertToDataTable(filtered);

            if (transactionData == null || transactionData.Rows.Count == 0) return;

            bind(transactionProjectDataTable, selectedProjectRow, true);

            List<SalesProjectItemSet> fetchedTabs = SalesProjectListData.sales_project_item_set;
            
            //get the content final
            var allFinals = SalesProjectListData.sales_project_content
            .Where(c => c.sales_project_content_final != null)
            .SelectMany(c => c.sales_project_content_final)
            .ToList();

            dt_multiplier = JsonHelper.ToDataTable(SalesProjectListData.sales_project_multiplier);
            dt_content = JsonHelper.ToDataTable(SalesProjectListData.sales_project_content);
            dt_content_final = JsonHelper.ToDataTable(allFinals);
            dt_advanced_conditions = JsonHelper.ToDataTable(SalesProjectListData.sales_project_content_advanced_condition);
            dt_items = JsonHelper.ToDataTable(SalesProjectListData.sales_project_items);
            dt_items_selected_images = JsonHelper.ToDataTable(SalesProjectListData.sales_project_items_selected_images);
            dt_wiring = JsonHelper.ToDataTable(SalesProjectListData.sales_project_wiring);

            //Helpers.BindControls(pnls, dt2, selectedProject);

            txt_project_name.Text = transactionData.Rows[0]["project_name"].ToString();

            //DataView dataview = new DataView(dt_multiplier);
            //dataview.RowFilter = "based_id = '" + this.allTransactionList.Rows[this.selectedProject]["id"].ToString() + "'";
            //dgv_project_multiplier.DataSource = dataview;

             tabControl2.TabPages.Clear();

            var filteredtabs = fetchedTabs.Where(tab => tab.based_id.ToString() == selectedId).ToList();
            foreach (var tab in filteredtabs)
            {
                TabPage newTab = new TabPage(tab.tab_number);
                newTab.Tag = tab.itemset_id;

                ItemSetUC UC = new ItemSetUC
                {
                    Dock = DockStyle.Fill
                };
                // ImageList/selectedImageList were never being set here, so this tab's
                // IMAGES column picker had nothing to show and no record of previously
                // saved selections. selectedImageList is passed unfiltered (same as Quick
                // Quote) - SetFetchedItemData matches rows to this tab's own items by
                // items_id, so passing every tab the same full table is fine.
                UC.ImageList = this.ImageList;
                UC.selectedImageList = dt_items_selected_images;

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
                UC.CellClickedModel += CellClickedModelUC;
                //UC.DeleteReferenceCode += DeleteRowsByReferenceCode;

                //UC.ItemChanged += ItemChanged;
                UC.FinalTxtBoxClicked += FinalTxtBoxClicked;
                //UC.SetUnitsOfMeasure(CacheData.UoM, CacheData.UoM);

                DataView multipliers = new DataView(dt_multiplier);
                multipliers.RowFilter = $"based_id = '{tab.based_id}'";
                //bs_project_multipliers.DataSource = multipliers.ToTable();
                dgv_project_multiplier.DataSource = multipliers;

                // setMultiplier() was only ever called for a brand-new tab (the "+" handler)
                // or as a side effect of editing the multiplier setup grid on whichever tab
                // happened to be selected at that moment - never here, when a project's
                // existing tabs are actually loaded. So the MULTIPLIER dropdown column had no
                // choices bound to it (empty/unusable) on every loaded tab except by
                // coincidence, if that side effect had happened to touch it first. Reading
                // fetchMultiplierData() here (right after dgv_project_multiplier is populated
                // for this tab, above) instead of before it, so it isn't stale from whichever
                // tab happened to be bound last.
                UC.setMultiplier(fetchMultiplierData());

                DataView contentView = new DataView(dt_content);
                contentView.RowFilter = $"based_id = '{tab.itemset_id}'";

                DataView contentFinalView = new DataView(dt_content_final);
                contentFinalView.RowFilter = $"sales_project_content_id = '{tab.itemset_id}'";

                DataView conditionsView = new DataView(dt_advanced_conditions);
                conditionsView.RowFilter = $"based_id = '{tab.itemset_id}'";

                DataView itemView = new DataView(dt_items);
                itemView.RowFilter = $"based_id = '{tab.itemset_id}'";

                DataView wiringView = new DataView(dt_wiring);
                wiringView.RowFilter = $"based_id = '{tab.itemset_id}'";

                CurrentProjectItemBasedID = tab.itemset_id;

                DataTable contentTable = contentView.ToTable();

                UC.SetAdvancedPanelData(conditionsView.ToTable());
                UC.SetContentsPanelData(contentTable);
                UC.SetFinalData(contentFinalView.ToTable());

                bool hasContentRow = contentTable.Rows.Count > 0;
                UC.SetTemplateName(hasContentRow ? contentTable.Rows[0]["template_project_id"]?.ToString() ?? "0" : "0");
                UC.SetWiring(hasContentRow ? contentTable.Rows[0]["is_wiring"]?.ToString() ?? "false" : "false");


                newTab.Controls.Add(UC);
                tabControl2.TabPages.Add(newTab);

                UC.SetFetchedItemData(itemView.ToTable());
                UC.SetProjectWiring(wiringView.ToTable());
            }

            TabPage addNewTab = new TabPage("+");
            tabControl2.TabPages.Add(addNewTab);

            fetchProjectMultipliers();
            //ConnectToWebSocket("Sales", selectedSalesQuotationId);

            // Newly (re)built tabs default to whatever the form's current isNewRecord/IsEdit
            // state actually is - locked (view mode) unless the user already clicked Edit or
            // this is a brand new project being built out.
            UpdateProjectControlsEditableState();

            RenderTabHistory();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading project quotation: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void CellClickedModelUC(object sender, EventArgs e)
        {
            if (tabControl2.SelectedTab.Controls[0] is ItemSetUC currentControl)
            {

                int index = currentControl.GetIndex();

                    HandleModelSelectionClick(index, currentControl.DgvProjectItems);     
            }
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
                        currentControl.SetProjectItemsData(items_dt, "fetchSalesProjectRT");
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
                    bind(transactionList, selectedProjectRow, true);
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
            int maxDocNum = 0;

            // Check BOTH DataTables to find the global maximum document number
            foreach (DataTable table in new[] { transactionList, transactionProjectDataTable })
            {
                if (table.Rows.Count > 0)
                {
                    foreach (DataRow row in table.Rows)
                    {
                        if (row["document_no"] != DBNull.Value && !string.IsNullOrEmpty(row["document_no"].ToString()))
                        {
                            if (int.TryParse(row["document_no"].ToString(), out int documentNumber))
                            {
                                if (documentNumber > maxDocNum)
                                {
                                    maxDocNum = documentNumber;
                                }
                            }
                            else
                            {
                                string digitsOnly = new string(row["document_no"].ToString().Where(char.IsDigit).ToArray());

                                if (!string.IsNullOrEmpty(digitsOnly) && int.TryParse(digitsOnly, out int extractedNumber))
                                {
                                    if (extractedNumber > maxDocNum)
                                    {
                                        maxDocNum = extractedNumber;
                                    }
                                }
                            }
                        }
                    }
                }
            }

            // Increment the max number found across both tables
            docNum = (maxDocNum + 1).ToString().PadLeft(4, '0');
            txt_document_no.Text = "Q#" + docNum;
        }

        private void btn_save_Click(object sender, EventArgs e)
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

        private async void  IsProject()
        {
            // Belt-and-suspenders check alongside the one in btn_edit_Click/btn_update_Click -
            // IsEdit only means "editing an existing record" (see IsEdit's setter), so this
            // only fires on an update to a record that already exists, never on a brand new one.
            if (IsEdit && !IsRecordCreatedByCurrentUser(txt_created_by.Text))
            {
                MessageBox.Show("Only the user who created this quotation can update it.", "Not Allowed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            Panel[] pnl_list = { pnl_header, pnl_footer, pnl_project_name };
            var pnl_quotation = Helpers.GetControlsValues(pnl_list);

            pnl_quotation["project_name"] = txt_project_name.Text.Trim();

            if (string.IsNullOrWhiteSpace(txt_project_name.Text))
            {
                MessageBox.Show("Please enter a valid project name. The project name cannot be empty or consist only of spaces.",
                                "Invalid Project Name", MessageBoxButtons.OK, MessageBoxIcon.Error);
                txt_project_name.Focus();
                return;
            }

            var multiplierSource = Helpers.ConvertDataGridViewToDataTable(dgv_project_multiplier);

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

            pnl_quotation["sales_project_multiplier"] = multipliers;

            var allTabsData = new List<Dictionary<string, object>>();

            foreach (TabPage selectedTab in this.tabControl2.TabPages)
            {
                if (selectedTab != null && selectedTab.Controls.Count > 0)
                {
                    var selectedControl = selectedTab.Controls[0] as ItemSetUC;

                    if (selectedControl != null)
                    {
                        var tabData = new Dictionary<string, object>();

                        int basedId;
                        if (pnl_quotation["id"] is long)
                            basedId = (int)(long)pnl_quotation["id"];
                        else
                            int.TryParse(pnl_quotation["id"].ToString(), out basedId);

                        tabData["sales_project_item_set"] = new Dictionary<string, object>
                        {
                            { "based_id",   basedId },
                            { "tab_number", selectedTab.Text },
                            { "itemset_id", selectedTab.Tag },
                            { "is_new_tab", _newlyCreatedTabs.Contains(selectedTab) }
                        };

                        tabData["sales_project_history"] = selectedControl.GetHistoryList();
                        tabData["sales_project_content"] = selectedControl.GetProjectContentsData();
                        tabData["sales_project_content_advanced_condition"] = selectedControl.GetAdvancedConditionsData();
                        tabData["sales_project_items"] = selectedControl.GetProjectItems()["sales_project_items"];
                        tabData["sales_project_wiring"] = selectedControl.GetProjectWiringData()["sales_project_wiring"];

                        allTabsData.Add(tabData);
                    }
                }
            }

            pnl_quotation["sales_project_all_tabs"] = allTabsData;

            if (!ConvertToInt(pnl_quotation, "customer_id", "Invalid customer ID"))
                return;

            if (isNewRecord)
                pnl_quotation["id"] = 0;

            if (IsEdit)
                pnl_quotation["id"] = int.Parse(pnl_quotation["id"].ToString());

            // document_no is saved with its "Q#"/"FQ#" prefix intact - that's the intended
            // identifier for draft vs. finalized status at a glance, not an accident. Search
            // and print lookups are normalized (NormalizeDocumentNo) to match regardless of
            // whether a given record has the prefix or not, so this doesn't break those.

            pnl_quotation["percent_discount"] = float.TryParse(txt_additional_discount.Text, out float discount) ? discount : 0;

            var quotation = JsonConvert.SerializeObject(pnl_quotation, Formatting.Indented);

            if (isNewRecord)
            {
                var response = await ProjectService.Insert(pnl_quotation);
                if (response.Success)
                {
                    MessageBox.Show("Saved");
                    SetNewFormMode(false);

                    // A successful save should always drop back to read-only View mode -
                    // isNewRecord/IsEdit weren't being reset here, so the grids/textboxes
                    // stayed unlocked (still "editable") even though Save had already
                    // succeeded and the New/Edit buttons had reappeared.
                    isNewRecord = false;
                    IsEdit = false;

                    // Refetch so SalesProjectListData (and therefore Change History) reflects
                    // what was actually just saved instead of staying stale until the user
                    // happens to navigate away and back.
                    await fetchSalesProjectData();
                }
                else
                    MessageBox.Show($"Insert error: {response.message}");
            }
            if (IsEdit)
            {
                SalesProjectList dbData = SalesProjectListData;

                Dictionary<string, dynamic> changes = GetFullDiff(dbData, pnl_quotation);

                changes["id"] = (int)pnl_quotation["id"];

                var response = await ProjectService.UpdateChange(changes);

                if(response.Success)
                {
                    MessageBox.Show("Updated successfully.");
                    SetNewFormMode(false);

                    // Same as the isNewRecord branch above - drop back to read-only View mode.
                    isNewRecord = false;
                    IsEdit = false;

                    // Same reason as the isNewRecord branch - without this, the newly
                    // auto-generated Change History entries (project fields, multipliers,
                    // per-tab changes) wouldn't show up until the next unrelated refresh.
                    await fetchSalesProjectData();
                }
                else
                    MessageBox.Show($"Update error: {response.message}");

            }
        }
        // ─── Extended diff models ───────────────────────────────────────────────────

        public class FieldChange
        {
            public object OldValue { get; set; }
            public object NewValue { get; set; }
        }

        public class ModelDiff<T>
        {
            public List<T> Added { get; set; } = new List<T>();
            public List<T> Removed { get; set; } = new List<T>();

            public bool HasChanges()
            {
                return Added.Any() || Removed.Any();
            }
        }

        public class UpdatedModel<T>
        {
            public T Item { get; set; }
            public Dictionary<string, FieldChange> Changes { get; set; } = new Dictionary<string, FieldChange>();
        }

        public class ModelUpdateDiff<T>
        {
            public List<T> Added { get; set; } = new List<T>();
            public List<T> Removed { get; set; } = new List<T>();
            public List<UpdatedModel<T>> Updated { get; set; } = new List<UpdatedModel<T>>();

            public bool HasChanges()
            {
                return Added.Any() || Removed.Any() || Updated.Any();
            }
        }

        // ─── TabDiff ────────────────────────────────────────────────────────────────

        public class TabDiff
        {
            public int BasedId { get; set; }

            public ModelUpdateDiff<SalesProjectItems> SalesProjectItems { get; set; } = new ModelUpdateDiff<SalesProjectItems>();
            public ModelUpdateDiff<SalesProjectContent> SalesProjectContent { get; set; } = new ModelUpdateDiff<SalesProjectContent>();
            public ModelUpdateDiff<SalesProjectAdvancedConditions> SalesProjectContentAdvancedCondition { get; set; } = new ModelUpdateDiff<SalesProjectAdvancedConditions>();
            public ModelUpdateDiff<SalesWiringModel> SalesProjectWirings { get; set; } = new ModelUpdateDiff<SalesWiringModel>();
            public ModelUpdateDiff<SalesProjectItemSet> SalesProjectItemSet { get; set; } = new ModelUpdateDiff<SalesProjectItemSet>();
            public ModelUpdateDiff<SalesProjectHistory> SalesProjectHistory { get; set; } = new ModelUpdateDiff<SalesProjectHistory>();

            public bool HasChanges()
            {
                return SalesProjectItems.HasChanges()
                    || SalesProjectContent.HasChanges()
                    || SalesProjectContentAdvancedCondition.HasChanges()
                    || SalesProjectWirings.HasChanges()
                    || SalesProjectItemSet.HasChanges()
                    || SalesProjectHistory.HasChanges();
            }
        }

        // ─── Header-level diff ──────────────────────────────────────────────────────

        public class HeaderDiff
        {
            public Dictionary<string, FieldChange> QuotationFields { get; set; } = new Dictionary<string, FieldChange>();
            public ModelUpdateDiff<SalesProjectMultiplier> Multipliers { get; set; } = new ModelUpdateDiff<SalesProjectMultiplier>();

            public bool HasChanges()
            {
                return QuotationFields.Any() || Multipliers.HasChanges();
            }
        }

        // ─── Root ───────────────────────────────────────────────────────────────────

        public class FullProjectDiff
        {
            public HeaderDiff Header { get; set; } = new HeaderDiff();
            public List<TabDiff> Tabs { get; set; } = new List<TabDiff>();

            public bool HasChanges()
            {
                return Header.HasChanges() || Tabs.Any(t => t.HasChanges());
            }
        }

        // ─── Tab data container (replaces anonymous tuple) ──────────────────────────

        private class TabDbData
        {
            public List<SalesProjectItemSet> ItemSets { get; set; } = new List<SalesProjectItemSet>();
            public List<SalesProjectContent> Contents { get; set; } = new List<SalesProjectContent>();
            public List<SalesProjectAdvancedConditions> Conditions { get; set; } = new List<SalesProjectAdvancedConditions>();
            public List<SalesProjectItems> Items { get; set; } = new List<SalesProjectItems>();
            public List<SalesWiringModel> Wiring { get; set; } = new List<SalesWiringModel>();
            public List<SalesProjectHistory> History { get; set; } = new List<SalesProjectHistory>();
        }

        // ─── Diff builder ───────────────────────────────────────────────────────────

        public Dictionary<string, dynamic> GetFullDiff(SalesProjectList dbData, Dictionary<string, object> pnlQuotation)
        {
            var result = new Dictionary<string, dynamic>();

            int ProjectQuotationId = (int)pnlQuotation["id"];

            SalesQuotationModel firstQuotation = dbData.SalesQuotation
                .FirstOrDefault(q => q.id == ProjectQuotationId);

            List<SalesProjectMultiplier> projectMultiplier = dbData.sales_project_multiplier
                .Where(m => m.based_id == ProjectQuotationId)
                .ToList();


            var quotationFieldChanges = GetQuotationFieldChanges(firstQuotation, pnlQuotation);
            var multiplierDiff = DiffByIndex(projectMultiplier,
                DeserializeList<SalesProjectMultiplier>(pnlQuotation, "sales_project_multiplier"),
                GetMultiplierChanges
            );

            // Change History used to only be generated per-tab - edits to the top part
            // (project name and the other header fields) and to the multipliers grid never
            // produced any history entries at all. This generates them the same way
            // BuildAutoHistoryEntries does for tabs, keyed to the quotation's own id so
            // RenderTabHistory can show them regardless of which tab is selected.
            var headerHistoryEntries = BuildHeaderAutoHistoryEntries(ProjectQuotationId, quotationFieldChanges, multiplierDiff);

            result["Header"] = new Dictionary<string, dynamic>
            {
                { "QuotationFields", quotationFieldChanges },
                { "Multipliers", multiplierDiff },
                {
                    "SalesProjectHistory", new ModelUpdateDiff<SalesProjectHistory>
                    {
                        Added = headerHistoryEntries
                    }
                }
            };

            var dbByTab = new Dictionary<int, TabDbData>();
            PopulateDbByTab(dbData, dbByTab, ProjectQuotationId);

            var allTabs = DeserializeList<Dictionary<string, object>>(pnlQuotation, "sales_project_all_tabs");

            // Brand-new tabs (added via "+" this session, never saved) are processed entirely
            // separately below, by TabPage-flagged identity rather than by id - matching them
            // in with everything else by id is what let a new tab's placeholder id collide
            // with (and be silently shadowed by) another tab's id, real or placeholder.
            var existingTabs = allTabs.Where(t => !IsNewTabFlag(t)).ToList();
            var newTabs = allTabs.Where(t => IsNewTabFlag(t)).ToList();

            var existingTabIds = new HashSet<int>();
            foreach (var tab in existingTabs)
            {
                int bid = GetItemSetIdFromTab(tab);
                if (bid > 0)
                    existingTabIds.Add(bid);
            }

            var allItemSetIds = new HashSet<int>(dbByTab.Keys);
            allItemSetIds.UnionWith(existingTabIds);

            foreach (int itemSetId in allItemSetIds)
            {
                var db = dbByTab.ContainsKey(itemSetId) ? dbByTab[itemSetId] : new TabDbData();

                Dictionary<string, object> matchedTab = null;
                foreach (var tab in existingTabs)
                {
                    if (GetItemSetIdFromTab(tab) == itemSetId)
                    {
                        matchedTab = tab;
                        break;
                    }
                }

                BuildTabDiffEntry(itemSetId, db, matchedTab, result);
            }

            // Every genuinely new tab gets its own diff entry, unconditionally treated as new
            // (nothing in the db to diff against) and never deduped/matched by id with anything
            // else - so its placeholder id can never shadow, or be shadowed by, another tab.
            foreach (var tab in newTabs)
            {
                int itemSetId = GetItemSetIdFromTab(tab);
                BuildTabDiffEntry(itemSetId, new TabDbData(), tab, result);
            }

            return result;
        }

        // Builds and appends one tab's diff entry to result["Tabs"] if it has any changes.
        // Shared by GetFullDiff's existing-tab (id-matched) and new-tab (unconditional) paths.
        private void BuildTabDiffEntry(int itemSetId, TabDbData db, Dictionary<string, object> matchedTab, Dictionary<string, dynamic> result)
        {
            var newItemSets = new List<SalesProjectItemSet>();
            if (matchedTab != null && matchedTab.ContainsKey("sales_project_item_set")
                && matchedTab["sales_project_item_set"] != null)
            {
                newItemSets.Add(BuildItemSet(matchedTab, itemSetId));
            }

            var tabDiff = new TabDiff { BasedId = itemSetId };

            List<SalesProjectContent> ContentMatchedTab = new List<SalesProjectContent>();
            List<SalesProjectAdvancedConditions> AdvanceConditionMatchedTab = new List<SalesProjectAdvancedConditions>();

            if (matchedTab != null)
            {
                var content = DeserializeSingleFromTab<SalesProjectContent>(matchedTab, "sales_project_content");
                if (content != null)
                {
                    content.based_id = itemSetId;
                    ContentMatchedTab.Add(content);
                }

                var advCondition = DeserializeSingleFromTab<SalesProjectAdvancedConditions>(matchedTab, "sales_project_content_advanced_condition");
                if (advCondition != null)
                {
                    advCondition.based_id = itemSetId;
                    AdvanceConditionMatchedTab.Add(advCondition);
                }
            }

            List<SalesProjectItems> ItemsMatchedTab = DeserializeFromTab<SalesProjectItems>(matchedTab, "sales_project_items");
            List<SalesWiringModel> WiringMatchedTab = DeserializeFromTab<SalesWiringModel>(matchedTab, "sales_project_wiring");
            List<SalesProjectHistory> HistoryMatchedTab = DeserializeFromTab<SalesProjectHistory>(matchedTab, "sales_project_history");

            tabDiff.SalesProjectItemSet = DiffModels(db.ItemSets, newItemSets, x => x.itemset_id, GetItemSetChanges);
            tabDiff.SalesProjectContent = DiffModels(db.Contents, ContentMatchedTab, x => x.content_id, GetContentChanges);
            tabDiff.SalesProjectContentAdvancedCondition = DiffModels(db.Conditions, AdvanceConditionMatchedTab, x => x.conditions_id, GetAdvancedConditionsChanges);
            tabDiff.SalesProjectItems = DiffModels(db.Items, ItemsMatchedTab, x => x.items_id, GetItemFieldChanges);
            tabDiff.SalesProjectWirings = DiffModels(db.Wiring, WiringMatchedTab, x => x.id, GetWiringChanges);
            tabDiff.SalesProjectHistory = DiffModels(db.History, HistoryMatchedTab, x => (int)x.history_id, GetHistoryChanges);

            // Auto-generate a readable Change History entry for every meaningful change this
            // save is about to make - GetHistoryList() (ItemSetUC) never produced real entries
            // on its own, so this is what actually populates the history table now, driven
            // straight off the diffs already computed above rather than requiring anything to
            // be logged manually.
            tabDiff.SalesProjectHistory.Added.AddRange(BuildAutoHistoryEntries(itemSetId, tabDiff));

            if (tabDiff.HasChanges())
            {
                if (!result.ContainsKey("Tabs")) result["Tabs"] = new List<Dictionary<string, dynamic>>();

                ((List<Dictionary<string, dynamic>>)result["Tabs"]).Add(JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(JsonConvert.SerializeObject(tabDiff)));
            }
        }

        // Turns the diffs already computed for one tab into readable Change History rows -
        // one per changed field/item, matching the "OLD DESCRIPTION => NEW VALUE" layout the
        // (formerly static-mockup) UC_History control displays.
        private List<SalesProjectHistory> BuildAutoHistoryEntries(int itemSetId, TabDiff tabDiff)
        {
            var entries = new List<SalesProjectHistory>();

            string user = CacheData.CurrentUser != null
                ? $"{CacheData.CurrentUser.first_name} {CacheData.CurrentUser.last_name}".Trim()
                : string.Empty;
            string date = DateTime.Now.ToString("M/d/yyyy");
            string time = DateTime.Now.ToString("h:mm tt");

            void AddEntry(string label, object oldVal, object newVal)
            {
                entries.Add(new SalesProjectHistory
                {
                    based_id = (uint)itemSetId,
                    user = user,
                    date = date,
                    time = time,
                    old_data = $"ITEM/SET {itemSetId} - {label}",
                    new_data = $"{FormatHistoryValue(oldVal)} => {FormatHistoryValue(newVal)}"
                });
            }

            foreach (var updated in tabDiff.SalesProjectItems.Updated)
                foreach (var change in updated.Changes)
                    AddEntry($"ITEM - {change.Key.ToUpperInvariant()}", change.Value.OldValue, change.Value.NewValue);

            // A brand-new item is diffed against a blank SalesProjectItems the same way an
            // edit is diffed against the old row - that's what makes an empty field getting
            // its first value ("" -> "Gate Valve") show up as its own readable line instead of
            // being collapsed into one generic "item added" entry.
            foreach (var added in tabDiff.SalesProjectItems.Added)
                foreach (var change in GetItemFieldChanges(new SalesProjectItems(), added))
                    AddEntry($"ITEM - {change.Key.ToUpperInvariant()}", change.Value.OldValue, change.Value.NewValue);

            foreach (var removed in tabDiff.SalesProjectItems.Removed)
                AddEntry("ITEM REMOVED", string.IsNullOrWhiteSpace(removed.model) ? removed.components : removed.model, null);

            foreach (var updated in tabDiff.SalesProjectContent.Updated)
                foreach (var change in updated.Changes)
                    AddEntry($"CONTENT - {change.Key.ToUpperInvariant()}", change.Value.OldValue, change.Value.NewValue);

            // Same idea for a tab's content record the first time it's ever saved (no prior
            // row existed, so it lands in Added rather than Updated) - each field the user
            // actually typed into (Application, Additional, Item/Set Notes, etc.) gets logged
            // as its own "(empty) -> new value" line instead of being silently skipped.
            foreach (var added in tabDiff.SalesProjectContent.Added)
                foreach (var change in GetContentChanges(new SalesProjectContent(), added))
                    AddEntry($"CONTENT - {change.Key.ToUpperInvariant()}", change.Value.OldValue, change.Value.NewValue);

            return entries;
        }

        // Same idea as BuildAutoHistoryEntries, but for changes that don't belong to any one
        // tab - the top-part header fields (project name, customer, purpose, etc.) and the
        // project multipliers grid. These are keyed to the quotation's own id (not a tab's
        // item_set_id) so RenderTabHistory can surface them no matter which tab is selected.
        private List<SalesProjectHistory> BuildHeaderAutoHistoryEntries(
            int quotationId,
            Dictionary<string, FieldChange> quotationFieldChanges,
            ModelUpdateDiff<SalesProjectMultiplier> multiplierDiff)
        {
            var entries = new List<SalesProjectHistory>();

            string user = CacheData.CurrentUser != null
                ? $"{CacheData.CurrentUser.first_name} {CacheData.CurrentUser.last_name}".Trim()
                : string.Empty;
            string date = DateTime.Now.ToString("M/d/yyyy");
            string time = DateTime.Now.ToString("h:mm tt");

            void AddEntry(string label, object oldVal, object newVal)
            {
                entries.Add(new SalesProjectHistory
                {
                    based_id = (uint)quotationId,
                    user = user,
                    date = date,
                    time = time,
                    old_data = $"PROJECT - {label}",
                    new_data = $"{FormatHistoryValue(oldVal)} => {FormatHistoryValue(newVal)}"
                });
            }

            foreach (var change in quotationFieldChanges)
                AddEntry(change.Key.ToUpperInvariant(), change.Value.OldValue, change.Value.NewValue);

            foreach (var updated in multiplierDiff.Updated)
                foreach (var change in updated.Changes)
                    AddEntry($"MULTIPLIER - {change.Key.ToUpperInvariant()}", change.Value.OldValue, change.Value.NewValue);

            // A brand-new multiplier row is diffed against a blank one, same as items/content -
            // an empty cell getting its first value shows up as its own readable line instead
            // of one generic "multiplier added" entry.
            foreach (var added in multiplierDiff.Added)
                foreach (var change in GetMultiplierChanges(new SalesProjectMultiplier(), added))
                    AddEntry($"MULTIPLIER - {change.Key.ToUpperInvariant()}", change.Value.OldValue, change.Value.NewValue);

            foreach (var removed in multiplierDiff.Removed)
                AddEntry("MULTIPLIER REMOVED", string.IsNullOrWhiteSpace(removed.description) ? removed.component : removed.description, null);

            return entries;
        }

        private static string FormatHistoryValue(object value)
        {
            if (value == null) return "-";
            var text = value.ToString();
            return string.IsNullOrWhiteSpace(text) ? "-" : text;
        }

        private T DeserializeSingleFromTab<T>(Dictionary<string, object> tab, string key) where T : class, new()
        {
            if (tab == null || !tab.ContainsKey(key) || tab[key] == null)
                return new T();

            var raw = tab[key];

            // Already a JObject
            if (raw is JObject jObj)
                return jObj.ToObject<T>() ?? new T();

            // Dictionary<string, object>
            if (raw is Dictionary<string, object> dict)
                return JObject.FromObject(dict).ToObject<T>() ?? new T();

            // JSON string
            if (raw is string json)
            {
                try
                {
                    return JObject.Parse(json).ToObject<T>() ?? new T();
                }
                catch
                {
                    return new T();
                }
            }

            return new T();
        }

        // Converts [{fieldName, value}, {fieldName, value}...] → Dictionary<string, object>
        private Dictionary<string, object> FlattenIndexedFieldPairs(Dictionary<string, object> tab, string key)
        {
            var result = new Dictionary<string, object>();

            // Get the indexed entries for this key
            Dictionary<string, object> indexed = null;

            if (tab.ContainsKey(key) && tab[key] is Dictionary<string, object> direct)
                indexed = direct;
            else
            {
                // Search inside numeric-keyed outer tab
                foreach (var kvp in tab)
                {
                    var inner = kvp.Value as Dictionary<string, object>;
                    if (inner != null && inner.ContainsKey(key))
                    {
                        indexed = inner[key] as Dictionary<string, object>;
                        break;
                    }

                    var jObj = kvp.Value as JObject;
                    if (jObj != null && jObj.ContainsKey(key))
                    {
                        indexed = jObj[key].ToObject<Dictionary<string, object>>();
                        break;
                    }
                }
            }

            if (indexed == null) return result;

            // Each entry is like [0] = {size_up_5, }, [1] = {item_set_notes, }
            // Extract the field name (key) and value from each pair
            foreach (var kvp in indexed)
            {
                if (!int.TryParse(kvp.Key, out _)) continue;

                var inner = kvp.Value as Dictionary<string, object>;
                if (inner != null)
                {
                    foreach (var field in inner)
                        result[field.Key] = field.Value;
                    continue;
                }

                var jObj = kvp.Value as JObject;
                if (jObj != null)
                {
                    foreach (var field in jObj)
                        result[field.Key] = field.Value;
                }
            }

            return result;
        }
        private SalesProjectContent BuildContent(Dictionary<string, object> tab)
        {
            var fields = FlattenIndexedFieldPairs(tab, "sales_project_content");
            if (fields.Count == 0)
                return new SalesProjectContent();

            try
            {
                return JObject.FromObject(fields).ToObject<SalesProjectContent>()
                       ?? new SalesProjectContent();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    string.Format("Failed to deserialize SalesProjectContent. Fields: {0}",
                        string.Join(", ", fields.Keys)), ex);
            }
        }
        private SalesProjectAdvancedConditions BuildAdvancedConditions(Dictionary<string, object> tab)
        {
            var fields = FlattenIndexedFieldPairs(tab, "sales_project_content_advanced_condition");
            if (fields.Count == 0)
                return new SalesProjectAdvancedConditions();

            try
            {
                return JObject.FromObject(fields).ToObject<SalesProjectAdvancedConditions>()
                       ?? new SalesProjectAdvancedConditions();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    string.Format("Failed to deserialize SalesProjectAdvancedConditions. Fields: {0}",
                        string.Join(", ", fields.Keys)), ex);
            }
        }

        // ---- NEW OVERLOAD — won't break the 3 existing callers ----
        private static object DiffById<T>(List<T> oldList, List<T> newList, Func<T, int> keySelector, Func<T, T, Dictionary<string, FieldChange>> getChanges)
        {
            // ---- safe dictionary, last one wins on duplicate ----
            var oldDict = oldList
                .GroupBy(keySelector)
                .ToDictionary(g => g.Key, g => g.Last());

            var newDict = newList
                .GroupBy(keySelector)
                .ToDictionary(g => g.Key, g => g.Last());

            var added = newList.Where(n => !oldDict.ContainsKey(keySelector(n))).ToList();
            var removed = oldList.Where(o => !newDict.ContainsKey(keySelector(o))).ToList();

            var updated = new List<object>();
            foreach (var newItem in newList)
            {
                var id = keySelector(newItem);
                if (!oldDict.TryGetValue(id, out var oldItem)) continue;

                var changes = getChanges(oldItem, newItem);
                if (changes.Count == 0) continue;

                updated.Add(new { Item = newItem, Changes = changes });
            }

            return new { Added = added, Removed = removed, Updated = updated };
        }


        // For top-level dictionary keys
        //private List<T> DeserializeList<T>(Dictionary<string, object> dict, string key)
        //{
        //    if (dict == null || !dict.ContainsKey(key) || dict[key] == null)
        //        return new List<T>();

        //    var raw = dict[key];

        //    // ---- DEBUG: see what's actually coming in ----
        //    Console.WriteLine($"DeserializeList key={key} type={raw.GetType().Name}");
        //    Console.WriteLine($"DeserializeList value={JsonConvert.SerializeObject(raw)}");
        //    // -----------------------------------------------

        //    if (raw is List<T> typed)
        //        return typed;

        //    if (raw is JArray jArr)
        //        return jArr.ToObject<List<T>>() ?? new List<T>();

        //    if (raw is IEnumerable<object> enumerable)
        //        return enumerable
        //            .Select(x => JObject.FromObject(x).ToObject<T>())
        //            .ToList();

        //    if (raw is Dictionary<string, object> d && d.Keys.All(k => int.TryParse(k, out _)))
        //    {
        //        var list = d
        //            .OrderBy(kv => int.Parse(kv.Key))
        //            .Select(kv => kv.Value)
        //            .ToList();

        //        return list
        //            .Select(x => JObject.FromObject(x).ToObject<T>())
        //            .ToList();
        //    }

        //    return new List<T>();
        //}

        private List<T> DeserializeList<T>(Dictionary<string, object> dict, string key, bool preserveCase = false)
        {
            if (dict == null || !dict.ContainsKey(key) || dict[key] == null)
                return new List<T>();

            var raw = dict[key];

            Console.WriteLine($"DeserializeList key={key} type={raw.GetType().Name}");
            Console.WriteLine($"DeserializeList value={JsonConvert.SerializeObject(raw)}");

            // use DefaultContractResolver only when preserveCase is true
            var serializer = preserveCase
                ? Newtonsoft.Json.JsonSerializer.Create(new JsonSerializerSettings
                {
                    ContractResolver = new DefaultContractResolver()
                })
                : Newtonsoft.Json.JsonSerializer.CreateDefault();

            if (raw is List<T> typed)
                return typed;

            if (raw is JArray jArr)
                return jArr.ToObject<List<T>>(serializer) ?? new List<T>();

            if (raw is IEnumerable<object> enumerable)
                return enumerable
                    .Select(x => JObject.FromObject(x).ToObject<T>(serializer))
                    .Where(x => x != null)
                    .ToList();

            if (raw is Dictionary<string, object> d && d.Keys.All(k => int.TryParse(k, out _)))
                return d.OrderBy(kv => int.Parse(kv.Key))
                        .Select(kv => JObject.FromObject(kv.Value).ToObject<T>(serializer))
                        .Where(x => x != null)
                        .ToList();

            return new List<T>();
        }

        // For tab-level keys (matchedTab can be null)
        private List<T> DeserializeFromTab<T>(Dictionary<string, object> tab, string key)
        {
            if (tab == null)
                return new List<T>();

            if (tab.ContainsKey(key))
                return DeserializeList<T>(tab, key);

            //System.Diagnostics.Debug.WriteLine($"[DeserializeFromTab] key={key} not found directly. Tab keys: {string.Join(", ", tab.Keys)}");

            var collectedItems = new List<object>();

            foreach (var kvp in tab)
            {
                //System.Diagnostics.Debug.WriteLine($"[DeserializeFromTab] checking kvp.Key={kvp.Key} valueType={kvp.Value?.GetType().FullName ?? "null"}");

                var inner = kvp.Value as Dictionary<string, object>;
                if (inner != null)
                {
                    //System.Diagnostics.Debug.WriteLine($"[DeserializeFromTab] inner dict keys: {string.Join(", ", inner.Keys)}");
                    if (inner.ContainsKey(key))
                    {
                        CollectItems(inner[key], collectedItems);
                        continue;
                    }
                }

                var jObj = kvp.Value as JObject;
                if (jObj != null)
                {
                    //System.Diagnostics.Debug.WriteLine($"[DeserializeFromTab] jObj keys: {string.Join(", ", jObj.Properties().Select(p => p.Name))}");
                    if (jObj.ContainsKey(key))
                    {
                        CollectItems(jObj[key], collectedItems);
                        continue;
                    }
                }
            }

            //System.Diagnostics.Debug.WriteLine($"[DeserializeFromTab] collectedItems count={collectedItems.Count}");

            if (collectedItems.Count > 0)
            {
                return collectedItems
                    .Select(x =>
                    {
                        try
                        {
                            if (x is JObject j)
                                return j.ToObject<T>();
                            if (x is T direct)
                                return direct;
                            return JObject.FromObject(x).ToObject<T>();
                        }
                        catch { return default(T); }
                    })
                    .Where(x => x != null)
                    .ToList();
            }

            return new List<T>();
        }

        // Helper: flattens a value into individual items
        private void CollectItems(object val, List<object> target)
        {
            if (val == null) return;

            // It's already a list/array
            if (val is JArray jArr)
            {
                target.AddRange(jArr.Cast<object>());
                return;
            }

            if (val is IEnumerable<object> enumerable)
            {
                target.AddRange(enumerable);
                return;
            }

            // It's a single object — wrap it
            if (val is JObject || val is Dictionary<string, object>)
            {
                target.Add(val);
                return;
            }

            // Numeric-keyed dictionary (PHP-style array)
            if (val is Dictionary<string, object> d && d.Keys.All(k => int.TryParse(k, out _)))
            {
                var ordered = d.OrderBy(kv => int.Parse(kv.Key))
                               .Select(kv => kv.Value);
                target.AddRange(ordered);
                return;
            }

            target.Add(val);
        }
        // Extract to reduce clutter in GetFullDiff
        private void PopulateDbByTab(SalesProjectList dbData, Dictionary<int, TabDbData> dbByTab, int quotationId)
        {
            // Filter each list by based_id == quotationId's item sets
            // First get the valid itemset IDs for this quotation
            var validItemSetIds = new HashSet<int>(
                dbData.sales_project_item_set
                    .Where(s => s.based_id == quotationId)
                    .Select(s => s.itemset_id)
            );

            foreach (int itemSetId in validItemSetIds)
            {
                if (!dbByTab.ContainsKey(itemSetId))
                    dbByTab[itemSetId] = new TabDbData();

                var tab = dbByTab[itemSetId];

                tab.ItemSets = dbData.sales_project_item_set
                    .Where(s => s.itemset_id == itemSetId)
                    .ToList();

                tab.Contents = dbData.sales_project_content
                    .Where(c => c.based_id == itemSetId)
                    .ToList();

                tab.Conditions = dbData.sales_project_content_advanced_condition
                    .Where(a => a.based_id == itemSetId)
                    .ToList();

                tab.Items = dbData.sales_project_items
                    .Where(i => i.based_id == itemSetId)
                    .ToList();

                tab.Wiring = dbData.sales_project_wiring
                    .Where(w => w.based_id == itemSetId)
                    .ToList();

                tab.History = dbData.sales_project_history
                    .Where(h => h.based_id == itemSetId)
                    .ToList();
            }
        }

        private int GetItemSetIdFromTab(Dictionary<string, object> tab)
        {
            if (tab == null || !tab.ContainsKey("sales_project_item_set"))
                return 0;

            var setDict = tab["sales_project_item_set"] as Dictionary<string, object>;
            if (setDict == null || !setDict.ContainsKey("itemset_id"))
                return 0;

            int itemsetId = ToInt(setDict["itemset_id"]);

            return itemsetId;
        }

        // Whether the client flagged this tab as brand-new this session (see
        // _newlyCreatedTabs). GetFullDiff uses this - not the tab's id - to decide whether to
        // process it unconditionally as new, so an id collision (placeholder vs. placeholder,
        // or placeholder vs. a real db id) can never cause one tab's data to shadow another's.
        private bool IsNewTabFlag(Dictionary<string, object> tab)
        {
            if (tab == null || !tab.ContainsKey("sales_project_item_set"))
                return false;

            var setDict = tab["sales_project_item_set"] as Dictionary<string, object>;
            if (setDict == null || !setDict.ContainsKey("is_new_tab"))
                return false;

            return setDict["is_new_tab"] is bool isNew && isNew;
        }
        private int ToInt(object value)
        {
            if (value == null) return 0;
            if (value is int) return (int)value;
            if (value is long) return (int)(long)value;

            int result;
            int.TryParse(value.ToString(), out result);
            return result;
        }

        private Dictionary<string, FieldChange> GetItemFieldChanges(SalesProjectItems db, SalesProjectItems upd)
        {
            var changes = new Dictionary<string, FieldChange>();

            Compare(changes, "components", db.components, upd.components);
            Compare(changes, "model", db.model, upd.model);
            Compare(changes, "item_inv_type", db.item_inv_type, upd.item_inv_type);
            Compare(changes, "reference_code", db.reference_code, upd.reference_code);

            Compare(changes, "qty", db.qty, upd.qty);
            Compare(changes, "list_price_per_unit", db.list_price_per_unit, upd.list_price_per_unit);
            Compare(changes, "unit_price", db.unit_price, upd.unit_price);
            Compare(changes, "discount_price", db.discount_price, upd.discount_price);
            Compare(changes, "component_total", db.component_total, upd.component_total);

            Compare(changes, "multiplier", db.multiplier, upd.multiplier);
            Compare(changes, "notes", db.notes, upd.notes);

            Compare(changes, "man_days", db.man_days, upd.man_days);
            Compare(changes, "labor_rate", db.labor_rate, upd.labor_rate);

            return changes;
        }

        private ModelUpdateDiff<T> DiffSingle<T>(
            T dbItem,
            T newItem,
            Func<T, T, Dictionary<string, FieldChange>> fieldComparer) where T : class
        {
            var diff = new ModelUpdateDiff<T>();

            if (dbItem == null && newItem == null)
                return diff;

            if (dbItem == null && newItem != null)
            {
                diff.Added.Add(newItem);
                return diff;
            }

            if (dbItem != null && newItem == null)
            {
                diff.Removed.Add(dbItem);
                return diff;
            }

            var changes = fieldComparer(dbItem, newItem);
            if (changes.Count > 0)
            {
                diff.Updated.Add(new UpdatedModel<T>
                {
                    Item = newItem,
                    Changes = changes
                });
            }

            return diff;
        }

        // Was typed to return an anonymous object - changed to the strongly-typed
        // ModelUpdateDiff<SalesProjectMultiplier> (same JSON shape: Added/Removed/Updated,
        // Updated entries carrying Item/Changes) so BuildHeaderAutoHistoryEntries can walk
        // the multiplier diff the same way BuildAutoHistoryEntries already walks tab diffs,
        // instead of needing reflection over an anonymous type.
        private static ModelUpdateDiff<SalesProjectMultiplier> DiffByIndex(
            List<SalesProjectMultiplier> oldList,
            List<SalesProjectMultiplier> newList,
            Func<SalesProjectMultiplier, SalesProjectMultiplier, Dictionary<string, FieldChange>> getChanges)
        {
            var diff = new ModelUpdateDiff<SalesProjectMultiplier>();

            var minCount = Math.Min(oldList.Count, newList.Count);

            for (int i = 0; i < minCount; i++)
            {
                var oldItem = oldList[i];
                var newItem = newList[i];

                var changes = getChanges(oldItem, newItem);
                if (changes.Count == 0) continue;

                // ---- carry real IDs from DB, use new values for content ----
                newItem.multiplier_id = oldItem.multiplier_id;
                newItem.based_id = oldItem.based_id;

                diff.Updated.Add(new UpdatedModel<SalesProjectMultiplier> { Item = newItem, Changes = changes });
            }

            for (int i = minCount; i < newList.Count; i++)
                diff.Added.Add(newList[i]);

            for (int i = minCount; i < oldList.Count; i++)
                diff.Removed.Add(oldList[i]);

            return diff;
        }

        private ModelUpdateDiff<T> DiffModels<T>(List<T> dbList, List<T> newList, Func<T, int> keySelector, Func<T, T, Dictionary<string, FieldChange>> fieldComparer)
        {
            var diff = new ModelUpdateDiff<T>();

            if (dbList == null) dbList = new List<T>();
            if (newList == null) newList = new List<T>();

            var dbDict = new Dictionary<int, T>();
            var newDict = new Dictionary<int, T>();

            foreach (var item in dbList)
            {
                int key = keySelector(item);
                if (!dbDict.ContainsKey(key))
                    dbDict[key] = item;
            }

            foreach (var item in newList)
            {
                int key = keySelector(item);
                if (!newDict.ContainsKey(key))
                    newDict[key] = item;
            }

            foreach (var kvp in newDict)
            {
                if (!dbDict.ContainsKey(kvp.Key))
                    diff.Added.Add(kvp.Value);
            }

            foreach (var kvp in dbDict)
            {
                if (!newDict.ContainsKey(kvp.Key))
                    diff.Removed.Add(kvp.Value);
            }

            foreach (var kvp in dbDict)
            {
                if (!newDict.ContainsKey(kvp.Key))
                    continue;

                var changes = fieldComparer(kvp.Value, newDict[kvp.Key]);
                if (changes.Count > 0)
                {
                    diff.Updated.Add(new UpdatedModel<T>
                    {
                        Item = newDict[kvp.Key],
                        Changes = changes
                    });
                }
            }

            return diff;
        }

        // ─── Per-model field comparers ───────────────────────────────────────────────

        private Dictionary<string, FieldChange> GetItemSetChanges(SalesProjectItemSet db, SalesProjectItemSet newItem)
        {
            var changes = new Dictionary<string, FieldChange>();

            Compare(changes, "tab_number", db.tab_number, newItem.tab_number);

            return changes;
        }

        private Dictionary<string, FieldChange> GetQuotationFieldChanges(
            SalesQuotationModel db,
            Dictionary<string, object> upd)
        {
            var c = new Dictionary<string, FieldChange>();
            if (db == null) return c;

            object val;
            Compare(c, "project_name", db.project_name, upd.TryGetValue("project_name", out val) ? val : null);
            Compare(c, "customer_id", db.customer_id, upd.TryGetValue("customer_id", out val) ? val : null);
            Compare(c, "application_id", db.application_id, upd.TryGetValue("application_id", out val) ? val : null);
            Compare(c, "payment_terms_id", db.payment_terms_id, upd.TryGetValue("payment_terms_id", out val) ? val : null);
            Compare(c, "ship_to_id", db.ship_to_id, upd.TryGetValue("ship_to_id", out val) ? val : null);
            Compare(c, "bill_to_id", db.bill_to_id, upd.TryGetValue("bill_to_id", out val) ? val : null);
            Compare(c, "ship_type_id", db.ship_type_id, upd.TryGetValue("ship_type_id", out val) ? val : null);
            Compare(c, "purpose", db.purpose, upd.TryGetValue("purpose", out val) ? val : null);
            Compare(c, "date", db.date, upd.TryGetValue("date", out val) ? val : null);
            Compare(c, "validity_days", db.validity_days, upd.TryGetValue("validity_days", out val) ? val : null);
            Compare(c, "warranty", db.warranty, upd.TryGetValue("warranty", out val) ? val : null);
            Compare(c, "address_to", db.address_to, upd.TryGetValue("address_to", out val) ? val : null);
            Compare(c, "thru", db.thru, upd.TryGetValue("thru", out val) ? val : null);
            Compare(c, "gross_sales", db.gross_sales, upd.TryGetValue("gross_sales", out val) ? val : null);
            Compare(c, "vat_amount", db.vat_amount, upd.TryGetValue("vat_amount", out val) ? val : null);
            Compare(c, "net_sales", db.net_sales, upd.TryGetValue("net_sales", out val) ? val : null);
            Compare(c, "percent_discount", db.percent_discount, upd.TryGetValue("percent_discount", out val) ? val : null);
            Compare(c, "discounted_amount", db.discounted_amount, upd.TryGetValue("discounted_amount", out val) ? val : null);
            Compare(c, "additional_discounted", db.additional_discounted_amount, upd.TryGetValue("additional_discounted", out val) ? val : null);
            Compare(c, "cash_discount", db.cash_discount, upd.TryGetValue("cash_discount", out val) ? val : null);
            Compare(c, "net_amount_due", db.net_amount_due, upd.TryGetValue("net_amount_due", out val) ? val : null);
            Compare(c, "total_amount_due", db.total_amount_due, upd.TryGetValue("total_amount_due", out val) ? val : null);
            Compare(c, "contact_1", db.contact_1, upd.TryGetValue("contact_1", out val) ? val : null);
            Compare(c, "contact_2", db.contact_2, upd.TryGetValue("contact_2", out val) ? val : null);
            Compare(c, "document_no", db.document_no, upd.TryGetValue("document_no", out val) ? val : null);
            Compare(c, "version_no", db.version_no, upd.TryGetValue("version_no", out val) ? val : null);
            Compare(c, "sub_version_no", db.sub_version_no, upd.TryGetValue("sub_version_no", out val) ? val : null);
            Compare(c, "created_by", db.created_by, upd.TryGetValue("created_by", out val) ? val : null);
            Compare(c, "final_ref_no", db.final_ref_no, upd.TryGetValue("final_ref_no", out val) ? val : null);
            Compare(c, "is_finalized", db.is_finalized, upd.TryGetValue("is_finalized", out val) ? val : null);
            Compare(c, "is_project", db.is_project, upd.TryGetValue("is_project", out val) ? val : null);
            return c;
        }

        private Dictionary<string, FieldChange> GetMultiplierChanges(SalesProjectMultiplier db, SalesProjectMultiplier upd)
        {
            var c = new Dictionary<string, FieldChange>();
            Compare(c, "brand", db.brand, upd.brand);
            Compare(c, "component", db.component, upd.component);
            Compare(c, "description", db.description, upd.description);
            Compare(c, "multiplier", db.multiplier, upd.multiplier);
            return c;
        }

        private Dictionary<string, FieldChange> GetContentChanges(SalesProjectContent db, SalesProjectContent upd)
        {
            var c = new Dictionary<string, FieldChange>();
            Compare(c, "item_designation", db.item_designation, upd.item_designation);
            Compare(c, "item_set_description", db.item_set_description, upd.item_set_description);
            Compare(c, "application", db.application, upd.application);
            Compare(c, "additional", db.additional, upd.additional);
            Compare(c, "flow", db.flow, upd.flow);
            Compare(c, "head", db.head, upd.head);
            Compare(c, "voltage", db.voltage, upd.voltage);
            Compare(c, "rpm", db.rpm, upd.rpm);
            Compare(c, "hp", db.hp, upd.hp);
            Compare(c, "phase", db.phase, upd.phase);
            Compare(c, "no_of_sets", db.no_of_sets, upd.no_of_sets);
            Compare(c, "no_of_pump_set", db.no_of_pump_set, upd.no_of_pump_set);
            Compare(c, "item_set_notes", db.item_set_notes, upd.item_set_notes);
            Compare(c, "is_wiring", db.is_wiring, upd.is_wiring);
            return c;
        }

        private Dictionary<string, FieldChange> GetAdvancedConditionsChanges(
            SalesProjectAdvancedConditions db,
            SalesProjectAdvancedConditions upd)
        {
            var c = new Dictionary<string, FieldChange>();
            Compare(c, "pump_brand", db.pump_brand, upd.pump_brand);
            Compare(c, "driver_type", db.driver_type, upd.driver_type);
            Compare(c, "pressure", db.pressure, upd.pressure);
            Compare(c, "motor_enclosure", db.motor_enclosure, upd.motor_enclosure);
            Compare(c, "motor_manufacturer", db.motor_manufacturer, upd.motor_manufacturer);
            Compare(c, "liquid_type", db.liquid_type, upd.liquid_type);
            Compare(c, "controller_manufacturer", db.controller_manufacturer, upd.controller_manufacturer);
            Compare(c, "starting_method", db.starting_method, upd.starting_method);
            Compare(c, "suction_size", db.suction_size, upd.suction_size);
            Compare(c, "discharge_size", db.discharge_size, upd.discharge_size);
            return c;
        }

        private Dictionary<string, FieldChange> GetWiringChanges(SalesWiringModel db, SalesWiringModel upd)
        {
            var c = new Dictionary<string, FieldChange>();
            // replace these with actual SalesWiringModel field names
            Compare(c, "description", db.description, upd.description);
            Compare(c, "qty", db.qty, upd.qty);
            return c;
        }
        private Dictionary<string, FieldChange> GetHistoryChanges(
            SalesProjectHistory db,
            SalesProjectHistory upd)
        {
            var c = new Dictionary<string, FieldChange>();
            Compare(c, "old_data", db.old_data, upd.old_data);
            Compare(c, "new_data", db.new_data, upd.new_data);
            return c;
        }

        // ─── Existing helpers (unchanged) ───────────────────────────────────────────

        private void Compare(Dictionary<string, FieldChange> changes, string field, object oldVal, object newVal)
        {
            // Treat 0 and null as equivalent
            var normalizedOld = IsZeroOrNull(oldVal) ? null : oldVal;
            var normalizedNew = IsZeroOrNull(newVal) ? null : newVal;

            if (AreEqual(normalizedOld, normalizedNew))
                return;

            changes[field] = new FieldChange
            {
                OldValue = oldVal,
                NewValue = newVal
            };
        }

        private bool IsZeroOrNull(object val)
        {
            if (val == null) return true;
            if (val is int i) return i == 0;
            if (val is long l) return l == 0;
            if (val is decimal d) return d == 0;
            if (val is double db) return db == 0;
            if (val is float f) return f == 0;
            if (val is short s) return s == 0;
            if (val is byte b) return b == 0;
            return false;
        }

        private bool AreEqual(object oldVal, object newVal)
        {
            var oldNorm = Normalize(oldVal);
            var newNorm = Normalize(newVal);

            if (oldNorm == newNorm) return true;

            if (IsNumeric(oldVal) && IsNumeric(newVal))
            {
                decimal d1 = Math.Round(Convert.ToDecimal(oldVal), 2, MidpointRounding.AwayFromZero);
                decimal d2 = Math.Round(Convert.ToDecimal(newVal), 2, MidpointRounding.AwayFromZero);
                return d1 == d2;
            }

            return false;
        }

        private bool IsNumeric(object value)
        {
            decimal dummy;
            return value != null && decimal.TryParse(value.ToString(), out dummy);
        }

        private string Normalize(object value)
        {
            if (value == null) return string.Empty;
            return value.ToString().Trim();
        }

        // ─── ItemSet builder ─────────────────────────────────────────────────────────
        private static readonly HashSet<Type> SafeTypes = new HashSet<Type>
{
    typeof(string), typeof(bool), typeof(byte), typeof(short), typeof(int), typeof(long),
    typeof(float), typeof(double), typeof(decimal), typeof(DateTime), typeof(Guid),
    typeof(bool?), typeof(byte?), typeof(short?), typeof(int?), typeof(long?),
    typeof(float?), typeof(double?), typeof(decimal?), typeof(DateTime?), typeof(Guid?)
};

        private static Dictionary<string, object> StripUnsafeValues(Dictionary<string, object> dict)
        {
            var clean = new Dictionary<string, object>();
            foreach (var kvp in dict)
            {
                if (kvp.Value == null)
                {
                    clean[kvp.Key] = null;
                    continue;
                }

                var type = kvp.Value.GetType();

                if (SafeTypes.Contains(type))
                {
                    clean[kvp.Key] = kvp.Value;
                }
                else if (kvp.Value is JObject || kvp.Value is JArray || kvp.Value is JToken)
                {
                    clean[kvp.Key] = kvp.Value;
                }
                else if (kvp.Value is Dictionary<string, object> nested)
                {
                    clean[kvp.Key] = StripUnsafeValues(nested);
                }
                else if (type.IsEnum)
                {
                    clean[kvp.Key] = kvp.Value;
                }
                // Skip DataRowView, BindingContext, and any other UI/binding objects
            }
            return clean;
        }

        private SalesProjectItemSet BuildItemSet(Dictionary<string, object> tab, int basedId)
        {
            if (basedId < 0)
                throw new ArgumentOutOfRangeException("basedId", string.Format("basedId must be >= 0, got {0}", basedId));

            if (!tab.ContainsKey("sales_project_item_set") || tab["sales_project_item_set"] == null)
                return new SalesProjectItemSet { based_id = basedId };

            var rawValue = tab["sales_project_item_set"];

            JObject raw = null;
            if (rawValue is JObject jObj)
                raw = jObj;
            else if (rawValue is Dictionary<string, object> dict)
            {
                var clean = StripUnsafeValues(dict);
                raw = JObject.FromObject(clean);
            }
            else if (rawValue is string json)
                raw = JObject.Parse(json);

            if (raw == null)
                return new SalesProjectItemSet { based_id = basedId };

            SalesProjectItemSet result;
            try
            {
                result = raw.ToObject<SalesProjectItemSet>() ?? new SalesProjectItemSet();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    string.Format("Failed to deserialize SalesProjectItemSet. Raw JSON: {0}", raw), ex);
            }

            result.based_id = basedId;
            return result;
        }
        //Here is the end for updates

        public static bool ConvertToInt(Dictionary<string, dynamic> dict,string key,string errorMessage)
        {
            if (!dict.TryGetValue(key, out var value))
                return true;

            if (value is int)
                return true;

            if (value is string s && int.TryParse(s, out int result))
            {
                dict[key] = result;
                return true;
            }

            MessageBox.Show(errorMessage);
            return false;
        }

        private void removeColumn()
        {
            // Make sure to handle the removal from the bottom to avoid index shifting issues
            for (int i = dgv_quick_quote_details.Rows.Count - 1; i >= 0; i--)
            {

                if (!dgv_quick_quote_details.Rows[i].IsNewRow)
                {
                    var cellValue = dgv_quick_quote_details.Rows[i].Cells["item_id"].Value;

                    if (cellValue != null && int.TryParse(cellValue.ToString(), out int itemId) && itemId == 0)
                    {
                        dgv_quick_quote_details.Rows.RemoveAt(i);
                    }
                }
            }
        }
        private async void IsQuickQuote()
        {
            // Belt-and-suspenders check alongside the one in btn_edit_Click/btn_update_Click -
            // IsEdit only means "editing an existing record" (see IsEdit's setter), so this
            // only fires on an update to a record that already exists, never on a brand new one.
            if (IsEdit && !IsRecordCreatedByCurrentUser(txt_created_by.Text))
            {
                MessageBox.Show("Only the user who created this quotation can update it.", "Not Allowed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                Panel[] pnl_list = { pnl_header, pnl_footer };
                var parentData = Helpers.GetControlsValuesV2(pnl_list);
                bool isParsed = int.TryParse(txt_id.Text,out int id);

                int bill_to_id = 0;
                int ship_to_id = 0;

                if (cmb_bill_to.SelectedValue == null)
                {
                    MessageBox.Show("bill to is required.");
                }
                else
                {
                    bill_to_id = int.Parse(cmb_bill_to.SelectedValue.ToString());
                }

                if (cmb_ship_to.SelectedValue == null)
                {
                    MessageBox.Show("ship to is required.");
                }
                else
                {
                    ship_to_id = int.Parse(cmb_ship_to.SelectedValue.ToString());
                }

                parentData["id"] = id;

                if (isNewRecord || isSubVersion)
                {
                    parentData.Remove("id");
                }

                 
                parentData["ship_to_id"] = ship_to_id;
                //parentData["isProject"] = false;


                var dataSource = Helpers.ConvertDataGridViewToDataTable(dgv_quick_quote_details);
                var newDatasource = Helpers.ConvertDataTableToStringTable(dataSource);

                List<Dictionary<string, dynamic>> quickQuoteList = new List<Dictionary<string, dynamic>>();
                // Tracks which original grid row each quickQuoteList entry came from, since
                // rows with item_id == 0 are skipped below and would otherwise throw the
                // indices out of sync with SelectedImagesByRow.
                List<int> quickQuoteRowIndexes = new List<int>();

                for (int i = 0; i < newDatasource.Rows.Count; i++)
                {
                    DataRow item = newDatasource.Rows[i];

                    int itemId = int.TryParse(item["item_id"].ToString(), out int ival) ? ival : 0;

                    if (itemId == 0)
                        continue;

                    Dictionary<string, object> data = new Dictionary<string, object>();

                    data.Add("item_id", itemId);
                    data.Add("bom_id", int.TryParse(item["quick_bom_id"].ToString(), out int bomid) ? bomid : 0);
                    data.Add("components", item["quick_item_code"]);
                    data.Add("model", item["quick_item_name"]);
                    data.Add("qty", int.TryParse(item["quick_qty"].ToString(), out int val) ? val : 0);
                    data.Add("unit_of_measure", item["quick_unit_of_measure"]);
                    data.Add("unit_price", decimal.TryParse(item["quick_unit_price"].ToString(), out decimal unitPrice) ? unitPrice : 0);
                    data.Add("percent_discount", item["quick_discount"].ToString());
                    data.Add("net_discount", decimal.Parse(Helpers.GetCleanedPriceValue(item["quick_net_discount"].ToString())));
                    data.Add("net_total", decimal.Parse(Helpers.GetCleanedPriceValue(item["quick_net_total"].ToString())));
                    data.Add("line_total", decimal.Parse(Helpers.GetCleanedPriceValue(item["quick_line_total"].ToString())));
                    data.Add("reference_code", item["reference_code"].ToString());
                    data.Add("short_description", item["short_description"].ToString());
                    data.Add("man_days", int.TryParse(item["man_days"].ToString(), out int manday) ? manday : 0);
                    data.Add("labor_rate", decimal.TryParse(item["labor_rate"].ToString(), out decimal laborday) ? laborday : 0);
                quickQuoteList.Add(data);
                quickQuoteRowIndexes.Add(i);

                }

                if (quickQuoteList != null)
                {
                    List<Dictionary<string, dynamic>> childCollection = new List<Dictionary<string, dynamic>>();

                    // loops thru the items - each row only gets the images that were
                    // selected for that specific row (falls back to empty if none picked)
                    for (int q = 0; q < quickQuoteList.Count; q++)
                    {
                        var dict = new Dictionary<string, dynamic>(quickQuoteList[q]);

                        int rowIndex = quickQuoteRowIndexes[q];
                        dict["quick_selected_image"] = SelectedImagesByRow.TryGetValue(rowIndex, out var rowImages)
                            ? rowImages
                            : new List<Dictionary<string, object>>();

                        childCollection.Add(dict);

                    }

                    // document_no is saved with its "Q#"/"FQ#" prefix intact - that's the
                    // intended identifier for draft vs. finalized status at a glance, not an
                    // accident. Search and print lookups are normalized (NormalizeDocumentNo)
                    // to match regardless of whether a given record has the prefix or not.

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
                        else
                            MessageBox.Show(isSuccess.message);
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
                if (dgv_quick_quote_details.Columns[e.ColumnIndex].Name == "quick_images" && !IsView)
                {
                    var row = dgv_quick_quote_details.Rows[e.RowIndex];
                    var cellQuickId = row.Cells["quick_id"].Value?.ToString();
                    var cellItemId = row.Cells["item_id"].Value?.ToString();

                    cellQuickId = string.IsNullOrWhiteSpace(cellQuickId) ? "0" : cellQuickId;


                    if (int.TryParse(cellQuickId, out int quickId) &&
                        int.TryParse(cellItemId, out int itemId))
                    {
                        HandleItemImageSelectionClick(e.RowIndex, quickId, itemId);
                    }
                }
                // Components Column
                if (dgv_quick_quote_details.Columns[e.ColumnIndex].Name == "quick_item_code" && !IsView)
                {
                    HandleItemSelectionClick(e.RowIndex, dgv_quick_quote_details);
                }

                //Model Column
                if (dgv_quick_quote_details.Columns[e.ColumnIndex].Name == "quick_item_name" && e.RowIndex >= 0 && !IsView)
                {
                    HandleModelSelectionClick(e.RowIndex, dgv_quick_quote_details);
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
            // Null check must run before dt is dereferenced below.
            if (dt == null)
                return 0;

            var ParentRow = dt.AsEnumerable()
                .FirstOrDefault(row => row.Field<string>("reference_code") == parentReferenceCode);

            // Find all direct children of the parent reference_code
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

                // Recursively find the total for this child's descendants.
                // Was "AllChildTotal = ChildTotal" (overwrite instead of
                // accumulate), so only the last child's subtree total ever
                // survived the loop and grandchild+ totals never rolled up
                // into the parent.
                decimal ChildTotal = GetTotalUnitPriceForChildren(dt, childReferenceCode);
                AllChildTotal += ChildTotal;
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

            // AllChildTotal (each direct child's own recursively-rolled-up
            // subtree total) was computed above but never folded in here -
            // add it so grandchild+ totals actually reach the parent.
            decimal TotalAmount = (totalLaborCost + totalUnitPrice) * 1.186m + AllChildTotal;
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
                int item_id_parent = int.Parse(dgv.Rows[RowIndex].Cells["item_id"].Value.ToString());
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

        private void HandleModelSelectionClick(int RowIndex, DataGridView dgv)
        {
            string Id = dgv.Rows[RowIndex].Cells["item_id"].Value.ToString();

            ModelModal createModal = new ModelModal(ItemList, BomHead, BomDetails, Id);
            DialogResult result = createModal.ShowDialog();

            string referenceCode = dgv.Rows[RowIndex].Cells["reference_code"].Value.ToString();


            if (result == DialogResult.OK)
            {
                int itemId = createModal.GetItemId();
                int bomId = createModal.GetBomId();

                DataTable dataSource = dgv.DataSource as DataTable;
                if (dataSource == null) return;

                temp_refence_code = dgv.Rows[RowIndex].Cells["reference_code"].Value.ToString();
                DeleteRowsByReferenceCode(RowIndex, dgv);

                if (bomId != 0)
                {
                    GetBomDataRecursive(RowIndex, bomId, itemId, dgv, referenceCode);
                    counterParent = 1;
                }
                else
                {
                    GetItemData(RowIndex, itemId, dgv, referenceCode);
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
        // Selected images picked via the IMAGES column, keyed by the grid row index they
        // belong to. Previously a single shared "SelectedImages" field was applied to every
        // row on Save, so every line item ended up with a copy of whichever item's images
        // were picked last - this keeps each row's image selection independent.
        private Dictionary<int, List<Dictionary<string, object>>> SelectedImagesByRow { get; set; } = new Dictionary<int, List<Dictionary<string, object>>>();
        private void HandleItemImageSelectionClick(int rowIndex, int quickId, int itemId)
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
                SelectedImagesByRow[rowIndex] = SelectedImages;
                int selectedImageCount = SelectedImages.Count();
                MessageBox.Show($"{selectedImageCount} images selected.");
            }

        }

        // Counters for reference codes
        int counterReference = 0;
        int counterParent = 1;

        // Red-flagged Project tabs (toggled via right-click menu). Used to be stored as
        // Color.Red/Color.White directly in TabPage.Tag, but Tag is also where a tab's real
        // itemset_id lives (see newTab.Tag = tab.itemset_id and the "+" new-tab handler) -
        // toggling red flag was silently destroying the tab's id, which made GetItemSetIdFromTab
        // treat an existing, previously-saved tab as brand new on the next save. Tracking
        // flagged tabs here instead leaves Tag free to always hold the tab's id.
        private readonly HashSet<TabPage> _redFlaggedTabs = new HashSet<TabPage>();

        // Tabs added via "+" this session that have never been saved. GetFullDiff uses this
        // (not the placeholder id below) to decide whether a tab is brand-new: matching by id
        // is what let one new tab's placeholder collide with, and get silently shadowed by,
        // another tab's id (real or placeholder) - see the incident this comment thread
        // documents. A tab in this set is always treated as new and diffed unconditionally,
        // never looked up or deduped by id, so an id collision can no longer cause data loss.
        private readonly HashSet<TabPage> _newlyCreatedTabs = new HashSet<TabPage>();

        // Placeholder id given to every brand-new, not-yet-saved Project tab's Tag. Used to be
        // a counter starting at a "surely big enough" constant (1000000000, later bumped to
        // 2000000000) so GetFullDiff's id-based tab matching wouldn't treat it as "no id" - but
        // that same id-matching is what let a new tab's placeholder collide with (and get
        // silently shadowed by) another tab, real or placeholder, when the constant wasn't as
        // collision-proof as assumed (see the incident this comment thread documents). Now that
        // GetFullDiff decides "is this tab new" via _newlyCreatedTabs (identity, not a number),
        // the placeholder's actual value no longer matters for correctness - a plain 0 works,
        // and it's permanently collision-proof since no real database id is ever 0.
        private const int NewTabPlaceholderId = 0;

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

        // Finds the highest top-level reference_code already on the grid (e.g. for codes
        // "1", "2", "3", "3.1" this returns 3) so numbering can continue from there instead
        // of restarting at 0 - restarting is what caused new items added while editing an
        // already-saved quotation to reuse codes that were already in use (1,2,3 -> 1,2
        // instead of 4,5).
        private int GetMaxTopLevelReferenceCode(DataGridView dgv)
        {
            int max = 0;

            if (!(dgv?.DataSource is DataTable dataSource) || !dataSource.Columns.Contains("reference_code"))
                return max;

            foreach (DataRow row in dataSource.Rows)
            {
                string value = row["reference_code"]?.ToString();
                if (string.IsNullOrWhiteSpace(value))
                    continue;

                // Only the part before the first "." is the top-level item number
                // (sub-item numbering restarts per parent, so it shouldn't count here).
                string topLevelPart = value.Split('.')[0];
                if (int.TryParse(topLevelPart, out int num) && num > max)
                {
                    max = num;
                }
            }

            return max;
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
                //if (isProject)
                //{
                //    if (tabControl2.SelectedTab.Controls[0] is ItemSetUC currentControl)
                //    {
                //        string item_id = row["id"].ToString();
                //        string item_name = row["item_name"].ToString();
                //        string bomid = "0";
                //        string itemcode = row["item_code"].ToString();
                //        string size = null;

                //        currentControl.SetComponentData(rowIndex, item_id, item_name, itemcode, size, bomid);
                //    }
                //}
                //else
                //{
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
                //}
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
            // Was Convert.ToDecimal with no guard - threw a FormatException
            // whenever the textbox was blank or mid-edit with non-numeric text.
            decimal.TryParse(txt_cash_discount.Text, out decimal cash_disc);
            return cash_disc;
        }
        private void computationLoop()
        {
            double gross_sales = 0, vat_amount = 0, net_sales = 0;
            double percent_discount = 0;
            double net_amount_due = 0, total_amount_due = 0;
            // Was double.Parse with no guard - same unguarded-parse issue as GetCashDiscount above.
            double.TryParse(txt_cash_discount.Text, out double cash_discount);
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
        bool IsView = true;
        private async void Quotation_Load(object sender, EventArgs e)
        {
            //int dgvWidth = dgv_quick_quote_details.Width;
            //int dgvHeight = dgv_quick_quote_details.Height;

            Panel[] panels = { pnl_header, pnl_footer };
            Helpers.ReadOnlyControls(panels);
 
            SetNewFormMode(false);

            IsView = true;

            await LoadExistingRecord();

        }
        CurrentUserModel CurrentUser { get; set; }
        private async Task LoadExistingRecord()
        {
            stockQuickDataTable = Helpers.GetDataTableFromUnboundGrid(dgv_quick_quote_details);
            await fetchItemData();
            await fetchBpiData();
            CurrentUser = CacheData.CurrentUser;

            //tabControl2.DrawMode = TabDrawMode.OwnerDrawFixed;
            //tabControl2.DrawItem += tabControl2_DrawItem;
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
                // A document can only ever be a Quick Quote OR a Project quotation
                // (the API filters each list by whether project_name is set), so exactly
                // one of these two lookups is expected to come back empty - that's normal,
                // not an error. We wait for both, then set isProject based on whichever
                // one actually found the record, so Print (and anything else keyed off
                // isProject) uses the correct data source afterwards.
                bool foundAsQuickQuote = await FetchQuotationDetailsByDocumentNo(documentNo, versionNo, subVersionNo);
                bool foundAsProject = await FetchProjectDetailsByDocumentNo(documentNo, versionNo, subVersionNo);

                if (foundAsProject)
                {
                    isProject = true;
                    this.btn_quick_quote.BackColor = Color.White;
                    this.btn_project.BackColor = Color.FromArgb(255, 128, 128);
                }
                else
                {
                    isProject = false;
                    this.btn_quick_quote.BackColor = Color.FromArgb(255, 128, 128);
                    this.btn_project.BackColor = Color.White;
                }

                UpdateDescriptionFieldsVisibility();

                if (!foundAsQuickQuote && !foundAsProject)
                {
                    MessageBox.Show("No SalesQuotation found for the provided document number.");
                }

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

                cmb_application.DataSource = CacheData.ApplicationSetup;
                cmb_application.DisplayMember = "code";
                cmb_application.ValueMember = "id";

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
        // PSEUDOCODE / PLAN
        // - When binding childList to the DataGridView ensure the underlying DataTable contains a "quick_images" column.
        // - If the column is missing, add it to the DataTable before assigning DataSource.
        // - This guarantees the column exists (designer or runtime) and LoadQuickImageCounts can populate it.
        // - Keep the rest of the bind logic unchanged.

        private void bind(DataTable transactionList, int SelectedRow, bool isBind = false)
        {
            if (isBind)
            {
                Panel[] pnlList = { pnl_header, pnl_footer };
                DataTable HeaderList = transactionList.Clone();
                HeaderList.Columns.Add("branch_name", typeof(string));
                HeaderList.Columns.Add("customer_code", typeof(string));
                HeaderList.Columns.Add("number", typeof(string));

                //bs_ship_to.DataSource = bpi_address;
                //bs_bill_to.DataSource = bpi_address;


                foreach (DataRow parentRow in transactionList.Rows)
                {
                    DataRow newRow = HeaderList.NewRow();
                    foreach (DataColumn col in transactionList.Columns)
                    {
                        newRow[col.ColumnName] = parentRow[col.ColumnName];
                    }

                    string ID = parentRow["customer_id"].ToString();
                    string BillToId = parentRow["bill_to_id"].ToString();
                    string ShipToId = parentRow["ship_to_id"].ToString();


                    // bpi_general/bpi_contacts are populated asynchronously by fetchBpiData().
                    // If bind() runs before that finishes (or the BPI fetch failed/returned
                    // null), these tables are still their empty DataTable() initializers with
                    // zero columns, and .Select() throws "Cannot find column [...]" instead of
                    // just returning no matches. Guard against that instead of crashing.
                    DataRow[] bpiRows = (bpi_general != null && bpi_general.Columns.Contains("general_based_id"))
                        ? bpi_general.Select($"general_based_id = '{ID}'")
                        : Array.Empty<DataRow>();
                    DataRow[] contactsRows = (bpi_contacts != null && bpi_contacts.Columns.Contains("contacts_based_id"))
                        ? bpi_contacts.Select($"contacts_based_id = '{ID}'")
                        : Array.Empty<DataRow>();

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

                // SelectedItem expects an item object from the bound list, not a raw id -
                // assigning appId/billId/shipId to it was a no-op that never matched
                // anything in Items. SelectedValue (below) is what actually drives the
                // ValueMember-bound selection, so the SelectedItem assignments are removed.
                cmb_application.SelectedValue = appId;

                cmb_bill_to.SelectedValue = billId;

                cmb_ship_to.SelectedValue = shipId;

                Helpers.BindControls(pnlList, HeaderList, SelectedRow);

                // LEM - Button visibility condition
                isFinalized = Convert.ToBoolean(HeaderList.Rows[SelectedRow]["is_finalized"]);
                btn_finalize.Enabled = !isFinalized || string.IsNullOrEmpty(txt_id.Text);
                btn_sales_order.Enabled = isFinalized;

                btn_new_version.Visible = !isFinalized;
                btn_duplicate.Visible = !isFinalized;

                btn_edit.Visible = !isFinalized;
                btn_add_customer.Visible = !isFinalized;

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
            }
        }

        private void createFilterViewDgvQuickQouteDetails()
        {
            // Create filtered view
            DataView dataview = new DataView(childList);
            dataview.RowFilter = $"based_id = " + this.transactionList.Rows[this.SelectedRow]["id"].ToString();

            dgv_quick_quote_details.DataSource = dataview;

            LoadQuickImageCounts();
            SeedSelectedImagesByRowFromView(dataview);
        }

        // This is the one place every "view an existing document's items" path actually
        // funnels through - opening the module blank then Searching for a document (the
        // normal day-to-day flow, via fetchQuotationDetails -> here), Next/Prev, and
        // re-selecting after a search all call this. FetchQuotationDetailsByDocumentNo (used
        // only when a documentNo is passed straight into the Quotation constructor, e.g. from
        // Opportunities/Orders) had its own copy of this seeding, but that path is never hit
        // when a document is opened via Search from a blank Quotation screen - which is why
        // seeding SelectedImagesByRow only there didn't fix "images didn't copy from Q#0019
        // to Q#0025" (Q#0019 was opened via Search, not via a documentNo passed at
        // construction). Seeding here instead covers every path, since they all end up
        // calling this method to actually populate the grid.
        private void SeedSelectedImagesByRowFromView(DataView dataview)
        {
            SelectedImagesByRow.Clear();

            if (selectedImageList == null || !selectedImageList.Columns.Contains("quotation_quick_id"))
                return;

            for (int rowIdx = 0; rowIdx < dataview.Count; rowIdx++)
            {
                DataRowView rowView = dataview[rowIdx];

                if (!rowView.Row.Table.Columns.Contains("id"))
                    continue;

                if (!int.TryParse(rowView["id"].ToString(), out int quickId))
                    continue;

                var imagesForRow = selectedImageList.AsEnumerable()
                    .Where(img => int.TryParse(img["quotation_quick_id"].ToString(), out int qId) && qId == quickId)
                    .Select(img => new Dictionary<string, object>
                    {
                        { "image_id", img["image_id"] },
                        { "is_selected", img["is_selected"] }
                    })
                    .ToList();

                if (imagesForRow.Count > 0)
                {
                    SelectedImagesByRow[rowIdx] = imagesForRow;
                }
            }
        }

        private void LoadQuickImageCounts()
        {
            // Defensive cleanup: this method (and the DataSource rebind that precedes it
            // in createFilterViewDgvQuickQouteDetails) can run several times over the life
            // of the form - e.g. re-selecting a row after a search. If more than one column
            // ends up named "quick_images", keep only the first and drop the rest so the
            // grid never shows a duplicate IMAGES column, and so
            // Helpers.ConvertDataGridViewToDataTable doesn't choke on Save.
            var duplicateImageColumns = dgv_quick_quote_details.Columns
                .Cast<DataGridViewColumn>()
                .Where(c => c.Name == "quick_images")
                .Skip(1)
                .ToList();

            foreach (var dupe in duplicateImageColumns)
            {
                dgv_quick_quote_details.Columns.Remove(dupe);
            }

            bool isColumnExist = false;

            if (dgv_quick_quote_details.Columns.Contains("quick_images"))
                isColumnExist = true;

            if(dgv_quick_quote_details.Columns.Contains("IMAGES"))
                isColumnExist = true;

            //// Ensure quick_images column exists
            if (!isColumnExist)
            {
                // TO BE CHANGED/FIND INSIDE DGV COLUMN INSTEAD OF CREATING
                var col = new DataGridViewTextBoxColumn();
                col.Name = "quick_images";
                col.HeaderText = "IMAGES";

                if (dgv_quick_quote_details.Columns.Count > 1)
                    dgv_quick_quote_details.Columns.Insert(1, col);
                else
                    dgv_quick_quote_details.Columns.Add(col);
            }

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
            IsEdit = false;
            IsView = false;

            Helpers.ResetControls(pnl_header);
            ResetControls(pnl_footer);

            txt_version_no.Text = GetNextVersionNo(allTransactionList, txt_document_no.Text);

            // New Quick Quote
            if (!isProject)
            {
                DocumentIncrementer();
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

                bind(transactionList, SelectedRow, false);


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
                TabPage newTab = new TabPage("New Project 1");
                // Create an instance of ItemSetUC
                ItemSetUC UC = new ItemSetUC
                {
                    Dock = DockStyle.Fill,
                    BackColor = Color.White
                };
                UC.ImageList = this.ImageList;

                // Attach event handlers
                UC.ButtonClicked += Button_ClickedUC;
                UC.DataChangedConditions += ItemSet_DataChanged;
                UC.DataChangedContent += Content_DataChanged;
                UC.ItemChanged += ItemChanged;
                UC.CellChangedProject += Cell_DataChanged;
                UC.CellClicked += Cell_ClickedUC;
                UC.CellClickedModel += CellClickedModelUC;
                UC.CellEdited += Cell_EditedUC;
                UC.FinalTxtBoxClicked += FinalTxtBoxClicked;
                UC.HandleItemSelectionClick += HandleItemSelectionClick;
                //UC.DeleteReferenceCode += DeleteRowsByReferenceCode;
                //UC.SetUnitsOfMeasure(CacheData.UoM, CacheData.UoM);

                // Add the UserControl to the new tab
                newTab.Controls.Add(UC);
                pnl_header.Focus();

                // Insert the new tab before the last tab
                this.tabControl2.TabPages.Insert(lastIndex, newTab);

                // Select the newly added tab
                this.tabControl2.SelectedIndex = lastIndex;
                setProjectMultiplier();

                // This tab didn't exist yet when isNewRecord/IsEdit were set further up in
                // this handler, so it never got unlocked - do it now that it's actually in
                // tabControl2.
                UC.SetEditable(true);

                // This branch returns early, before the shared "new quotations always
                // default to 30 days" line below ever runs - so clicking New while in
                // Project mode left txt_validays blank instead of reset to 30 like Quick
                // Quote's New already does.
                txt_validays.Text = "30";
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
            DataTable items = new DataTable();


            var data = await ProjectService.GetPumpsViewList();

            if (data == null || data.ItemPumpsView == null || !data.ItemPumpsView.Any())
            {
                MessageBox.Show("No pump items are set up yet. Please add pump items before using this.", "No Pump Data", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            //if (tabControl2.SelectedTab.Controls[0] is ItemSetUC currentControl)
            //{
            //    var data = currentControl.GetSizeUpData();
            //    List<KeyValuePair<string, dynamic>> size_up = data.ToList();
            //    pumps = JsonHelper.ToDataTable(size_up);
            //}

            pumps = JsonHelper.ToDataTable(data.ItemPumpsView);

            List<int> pumpId = pumps.AsEnumerable()
                                .Select(row => row.Field<int>("item_id"))
                                .Distinct()
                                .ToList();

            // .CopyToDataTable() throws InvalidOperationException("The source contains no
            // DataRows.") if the filtered sequence comes up empty - happened whenever none
            // of ItemList's rows matched any of the pump view's item_ids (e.g. a pump item
            // was removed from the item catalog but is still referenced in the pumps view).
            // Filtering to a list first lets us validate before ever calling
            // CopyToDataTable, instead of crashing.
            var filteredPumpItems = ItemList.AsEnumerable()
                                .Where(row => int.TryParse(row["id"]?.ToString(), out int rowId) && pumpId.Contains(rowId))
                                .ToList();

            if (filteredPumpItems.Count == 0)
            {
                MessageBox.Show("None of the items in the item list match the pump data. Please check that the pump items still exist in the item catalog.", "No Matching Items", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            DataTable ItemListPump = filteredPumpItems.CopyToDataTable();

            ModelModal createModal = new ModelModal(ItemListPump, BomHead, BomDetails, "0");

            DialogResult dr = createModal.ShowDialog();

            if (dr == DialogResult.OK)
            {
                string id = (createModal.GetItemId().ToString());
                var itemModelName = ItemList.AsEnumerable()
                                .FirstOrDefault(row => row["id"].ToString() == id)?["item_model"].ToString();

                var FLA = pumps.AsEnumerable()
                        .FirstOrDefault(row => row["item_title"].ToString() == "FLA" && row["item_id"].ToString() == id)?["item_value"].ToString();

                var Voltage = pumps.AsEnumerable()
                                    .FirstOrDefault(row => row["item_title"].ToString() == "VOLTAGE" && row["item_id"].ToString() == id)?["item_value"].ToString();

                if (tabControl2.SelectedTab.Controls[0] is ItemSetUC currentControl2 && !FLA.IsNullOrEmpty() && !Voltage.IsNullOrEmpty()
                    && !itemModelName.IsNullOrEmpty())
                {
                    currentControl2.SetFinalPumpData(FLA, Voltage, itemModelName);
                }
                else
                {
                    MessageBox.Show("Final/FLA/Voltage is possible empty");
                }

            }
        }
        private void btn_new_version_Click(object sender, EventArgs e)
        {
            GetLatestDate();
            SetNewFormMode(true);
            isNewRecord = true;
            IsEdit = false;


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
            // Previous/Next only step through the current user's OWN records - same
            // restriction as Search - otherwise these two buttons would be a way to browse
            // straight past that restriction, one record at a time.
            if (!isProject)
            {
                List<int> ownedIndexes = GetOwnedRowIndexes(transactionList);
                if (ownedIndexes.Count == 0)
                {
                    MessageBox.Show("You have no saved quotations yet. Click New to create one.");
                    return;
                }

                int pos = ownedIndexes.IndexOf(SelectedRow);
                if (pos < ownedIndexes.Count - 1)
                {
                    SelectedRow = ownedIndexes[pos == -1 ? 0 : pos + 1];
                    bind(transactionList, SelectedRow, true);
                    createFilterViewDgvQuickQouteDetails();
                }
            }
            else
            {
                List<int> ownedIndexes = GetOwnedRowIndexes(transactionProjectDataTable);
                if (ownedIndexes.Count == 0)
                {
                    MessageBox.Show("You have no project quotations yet. Click New to create one.");
                    return;
                }

                int pos = ownedIndexes.IndexOf(selectedProjectRow);
                if (pos < ownedIndexes.Count - 1)
                {
                    selectedProjectRow = ownedIndexes[pos == -1 ? 0 : pos + 1];
                    // Navigating to a different record shouldn't carry over edit mode from
                    // whatever was previously open.
                    IsEdit = false;
                    bind(transactionProjectDataTable, selectedProjectRow, true);
                    fetchSalesProject();
                }
            }
        }
        private void btn_prev_Click(object sender, EventArgs e)
        {
            // Same "own records only" restriction as btn_next_Click - see the comment there.
            if (!isProject)
            {
                List<int> ownedIndexes = GetOwnedRowIndexes(transactionList);
                if (ownedIndexes.Count == 0)
                {
                    MessageBox.Show("You have no saved quotations yet. Click New to create one.");
                    return;
                }

                int pos = ownedIndexes.IndexOf(SelectedRow);
                if (pos == -1)
                {
                    SelectedRow = ownedIndexes[0];
                    bind(transactionList, SelectedRow, true);
                    createFilterViewDgvQuickQouteDetails();
                }
                else if (pos >= 1)
                {
                    SelectedRow = ownedIndexes[pos - 1];
                    bind(transactionList, SelectedRow, true);
                    createFilterViewDgvQuickQouteDetails();
                }
            }
            else
            {
                List<int> ownedIndexes = GetOwnedRowIndexes(transactionProjectDataTable);
                if (ownedIndexes.Count == 0)
                {
                    MessageBox.Show("You have no project quotations yet. Click New to create one.");
                    return;
                }

                int pos = ownedIndexes.IndexOf(selectedProjectRow);
                if (pos == -1)
                {
                    selectedProjectRow = ownedIndexes[0];
                    IsEdit = false;
                    bind(transactionProjectDataTable, selectedProjectRow, true);
                    fetchSalesProject();
                }
                else if (pos >= 1)
                {
                    selectedProjectRow = ownedIndexes[pos - 1];
                    // Navigating to a different record shouldn't carry over edit mode from
                    // whatever was previously open.
                    IsEdit = false;
                    bind(transactionProjectDataTable, selectedProjectRow, true);
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

            var filtered = bpi_general.AsEnumerable()
                           .Where(x => x.Field<string>("branch_sales_id") == CacheData.CurrentUser.employee_id);

            DataTable bpiGeneralFilter = filtered.Any()
                                         ? filtered.CopyToDataTable()
                                         : bpi_general.Clone();

            SetupSelectionModal bpi = new SetupSelectionModal(Title, endpoint, bpiGeneralFilter, t1, s1, 0);

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
            // Leading blank entry so the MULTIPLIER dropdown on each item row can be left
            // unset instead of forcing the user to pick one of the real multiplier values.
            List<string> multiplier = new List<string> { string.Empty };

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
                                // Was "UnitPrice * cumulativeMultiplier", which stores the
                                // POST-discount unit price into DiscountedAmount instead of
                                // the discount amount itself - inverted vs. the single-discount
                                // branch below (and vs. what NetDiscount/LineTotal expect).
                                this.DiscountedAmount = this.UnitPrice * (1 - cumulativeMultiplier);
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
                SeedSelectedImagesByRowFromView(dataview);
            }
        }

        // Only the user who created a quotation (quick quote or project) should see it in
        // the Search results - other users' records are filtered out entirely rather than
        // just shown read-only. Returns a NEW DataTable (same columns, subset of rows) so
        // transactionList/transactionProjectDataTable themselves stay untouched - other
        // logic in this file (document numbering, version lookups, save/edit) still needs
        // the full, unfiltered data to keep working correctly.
        private DataTable FilterToCurrentUserQuotations(DataTable source)
        {
            DataTable filtered = source.Clone();

            if (CacheData.CurrentUser == null)
                return source;

            string currentUserName = $"{CacheData.CurrentUser.first_name} {CacheData.CurrentUser.last_name}".Trim();

            foreach (DataRow row in source.Rows)
            {
                string createdBy = source.Columns.Contains("created_by") && row["created_by"] != DBNull.Value
                    ? row["created_by"].ToString()
                    : null;

                if (string.IsNullOrEmpty(createdBy) ||
                    string.Equals(createdBy.Trim(), currentUserName, StringComparison.OrdinalIgnoreCase))
                {
                    filtered.ImportRow(row);
                }
            }

            return filtered;
        }

        // SetupModal returns a row index into whatever DataTable it was given - since search
        // now gets a filtered copy instead of the real transactionList/transactionProjectDataTable,
        // that index has to be translated back to the matching row in the real table (by "id")
        // before it's used anywhere else in this file.
        private int FindRowIndexById(DataTable table, object idValue)
        {
            string idString = idValue?.ToString();

            for (int i = 0; i < table.Rows.Count; i++)
            {
                if (table.Rows[i]["id"].ToString() == idString)
                    return i;
            }

            return -1;
        }

        // Same ownership rule as FilterToCurrentUserQuotations, but returns the matching row
        // INDEXES into the given table instead of a filtered copy - used by Previous/Next,
        // which need to keep stepping through the real transactionList/transactionProjectDataTable
        // (SelectedRow/selectedProjectRow are indexes into those, used all over this file), just
        // skipping over any row that isn't the current user's own.
        private List<int> GetOwnedRowIndexes(DataTable table)
        {
            List<int> indexes = new List<int>();

            string currentUserName = CacheData.CurrentUser != null
                ? $"{CacheData.CurrentUser.first_name} {CacheData.CurrentUser.last_name}".Trim()
                : null;

            for (int i = 0; i < table.Rows.Count; i++)
            {
                string createdBy = table.Columns.Contains("created_by") && table.Rows[i]["created_by"] != DBNull.Value
                    ? table.Rows[i]["created_by"].ToString()
                    : null;

                if (string.IsNullOrEmpty(createdBy) ||
                    string.IsNullOrEmpty(currentUserName) ||
                    string.Equals(createdBy.Trim(), currentUserName, StringComparison.OrdinalIgnoreCase))
                {
                    indexes.Add(i);
                }
            }

            return indexes;
        }

        private void btn_search_Click_1(object sender, EventArgs e)
        {
            // One Search button is shared between Quick Quote and Project mode, but this
            // always searched transactionList - Quick Quote's list only - even while in
            // Project mode. Project mode has its own separate list
            // (transactionProjectDataTable); searching it while in Project mode picked a
            // row index out of the wrong table and tried to bind Project's UI with Quick
            // Quote data (or a mismatched/nonexistent record).
            if (isProject)
            {
                string projectTitle = "Project List";
                DataTable ownProjects = FilterToCurrentUserQuotations(transactionProjectDataTable);
                SetupModal projectSetup = new SetupModal(projectTitle, ownProjects);
                DialogResult projectResult = projectSetup.ShowDialog();

                if (projectResult == DialogResult.OK)
                {
                    int projectRowResult = projectSetup.GetResult();

                    if (projectRowResult != -1)
                    {
                        int mappedRow = FindRowIndexById(transactionProjectDataTable, ownProjects.Rows[projectRowResult]["id"]);
                        selectedProjectRow = mappedRow != -1 ? mappedRow : projectRowResult;
                        // IsEdit doesn't get reset just by opening a different record - if the
                        // user was editing another project earlier in this same session,
                        // IsEdit was still true here, so this newly opened project would come
                        // up unlocked/editable by default instead of view-only.
                        IsEdit = false;
                        bind(transactionProjectDataTable, selectedProjectRow, true);
                        fetchSalesProject();
                    }
                }

                return;
            }

            string Title = "Quotation List";
            DataTable ownQuotes = FilterToCurrentUserQuotations(transactionList);
            SetupModal setup = new SetupModal(Title, ownQuotes);
            DialogResult r = setup.ShowDialog();

            if (r == DialogResult.OK)
            {
                int result = setup.GetResult();

                if (result != -1)
                {
                    int mappedRow = FindRowIndexById(transactionList, ownQuotes.Rows[result]["id"]);
                    SelectedRow = mappedRow != -1 ? mappedRow : result;
                    bind(transactionList, SelectedRow, true);
                    createFilterViewDgvQuickQouteDetails();

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

            // Red-flag state is tracked in _redFlaggedTabs, not Tag - Tag holds the tab's
            // itemset_id (see toolStripMenuItemTagRed_Click).
            Color tabColor = _redFlaggedTabs.Contains(tabPage) ? Color.Red : Color.White;

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
            // Don't let the "+" tab spawn a new tab while just viewing a project quotation -
            // only New/Edit mode should be able to add tabs.
            if (isProject && !isNewRecord && !IsEdit)
                return;

            //if (tabControl2.TabPages.Count == 0) return;

            //if (e.Button == MouseButtons.Right)
            //{
            //    for (int i = 0; i < tabControl2.TabPages.Count; i++)
            //    {
            //        Rectangle tabRect = tabControl2.GetTabRect(i);

            //        if (tabRect.Contains(e.Location))
            //        {
            //            Color currentColor = tabControl2.TabPages[i].Tag is Color ? (Color)tabControl2.TabPages[i].Tag : Color.White;

            //            if (currentColor == Color.Red)
            //            {
            //                tabControl2.TabPages[i].Tag = Color.White;
            //            }
            //            else
            //            {
            //                tabControl2.TabPages[i].Tag = Color.Red;
            //            }


            //            tabControl2.Invalidate();
            //            break;
            //        }
            //    }
            //}

            var lastIndex = this.tabControl2.TabCount - 1;
            if (this.tabControl2.GetTabRect(lastIndex).Contains(e.Location))
            {
                //here is the code for adding new tab (+)

                // Create a new TabPage
                string tabNewName = NamingTabControl(lastIndex);

                var newTabPage = new TabPage(tabNewName);

                // Tag must never be left null here - it's how this tab's itemset_id gets
                // written into the save payload (see the sales_project_all_tabs builder in
                // btn_save_Click and GetItemSetIdFromTab). This placeholder stands in until the
                // server assigns a real item_set_id on insert; _newlyCreatedTabs (not this
                // value) is what tells GetFullDiff to treat the tab as new.
                newTabPage.Tag = NewTabPlaceholderId;

                // The authoritative "this tab is new" signal - see _newlyCreatedTabs.
                _newlyCreatedTabs.Add(newTabPage);

                // Create an instance of your UserControl
                ItemSetUC myControl = new ItemSetUC
                {
                    Dock = DockStyle.Fill,
                    BackColor = Color.White

                };
                myControl.ImageList = this.ImageList;

                //plus events function
                myControl.CellClicked += Cell_ClickedUC;
                myControl.CellClickedModel += CellClickedModelUC;
                myControl.CellEdited += Cell_EditedUC;
                myControl.FinalTxtBoxClicked += FinalTxtBoxClicked;
                myControl.setMultiplier(fetchMultiplierData());

                // Add the UserControl to the new tab
                newTabPage.Controls.Add(myControl);

                // Insert the new TabPage into the TabControl
                this.tabControl2.TabPages.Insert(lastIndex, newTabPage);

                // A new tab you just added should match whatever editable state the rest of
                // the project is currently in (it'll only actually be reachable while
                // editing/creating, but match explicitly rather than assume).
                myControl.SetEditable(isNewRecord || IsEdit);

                // Select the new tab
                this.tabControl2.SelectedIndex = lastIndex;
            }

        }

        int rightClickedTabIndex = -1;

        private void tabControl2_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            //D2 ka mag-edit ng tabcontrol name
            //if (e.Button == MouseButtons.Right)
            //{
            //    for (int i = 0; i < tabControl2.TabCount; i++)
            //    {
            //        // Check if the click coordinates fall within the header area of a tab
            //        if (tabControl2.GetTabRect(i).Contains(e.Location))
            //        {
            //            rightClickedTabIndex = i;
            //            // Select the tab that was right-clicked (optional, but good UX)
            //            int selectedIndex = tabControl2.SelectedIndex;
            //            string tabNewName = NamingTabControl(selectedIndex);

            //            // Renames the tab here
            //            tabControl2.TabPages[selectedIndex].Text = tabNewName;

            //            tabControl1.SelectedIndex = i;

            //            return;
            //        }
            //    }
            //    rightClickedTabIndex = -1; // No tab was clicked
            //}

        }

        private string NamingTabControl(int selectedIndex)
        {

            string tabs = string.Empty;
            ItemSetModal modal = new ItemSetModal();
            DialogResult r = modal.ShowDialog();


            if (r == DialogResult.OK)
            {
                tabs = modal.GetResult();
            }

            if (string.IsNullOrWhiteSpace(tabs))
            {
                MessageBox.Show("Empty ");
                int CountTab = selectedIndex + 1;
                return "New Project " + CountTab;
            }

            return tabs;
        }

        private void RenameMenuItem_Click(object sender, EventArgs e)
        {
            if (rightClickedTabIndex != -1)
            {
                TabPage pageToRename = tabControl1.TabPages[rightClickedTabIndex];

                // In a real application, you would use a custom dialog box
                // or an overlaid TextBox control here to get the new name.
                string newName = "test";//ShowRenameDialog(pageToRename.Text);

                if (!string.IsNullOrEmpty(newName))
                {
                    pageToRename.Text = newName; // This is where the renaming happens
                }
            }
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
        private bool _isEdit;
        // isNewRecord (set true by the various "New" handlers) was never being reset back to
        // false anywhere in this class. So once "New" was clicked at any point during this
        // form's lifetime, isNewRecord stayed true even after the user went on to open/edit a
        // totally different, already-saved record - at which point IsEdit also became true.
        // With both true, IsProject()/FinalizeProjectQuotation() ran BOTH their "if
        // (isNewRecord)" Insert/POST branch AND their "if (IsEdit)" Update/PUT branch for the
        // same save click. The POST tried to re-insert tabs/item sets that already exist in
        // the DB (PRIMARY KEY violation), and a stale/leftover id from the earlier "New" click
        // rode along as the top-level id on that same POST. Routing IsEdit's setter through
        // here keeps the two flags mutually exclusive no matter which of the several call
        // sites sets them.
        public bool IsEdit
        {
            get => _isEdit;
            private set
            {
                _isEdit = value;
                if (value)
                    isNewRecord = false;
                UpdateProjectControlsEditableState();
            }
        }
        public bool isSubVersion { get; private set; }

        // Project Quotation had no read-only state at all - every textbox, checkbox, combobox
        // and gridview (header fields, the multiplier setup grid, and everything inside every
        // tab's ItemSetUC) stayed editable even while just viewing an already-saved project,
        // before Edit was ever clicked. Called any time isNewRecord/IsEdit changes and any
        // time a project's tabs are (re)built, so the lock always matches current state:
        // editable only while starting a brand new project (isNewRecord) or actively editing
        // an existing one (IsEdit); locked otherwise (view mode).
        private void UpdateProjectControlsEditableState()
        {
            if (!isProject) return;

            bool editable = isNewRecord || IsEdit;

            Panel[] projectPanels = { pnl_header, pnl_footer, pnl_project_name };
            if (editable)
                Helpers.ResetReadOnlyControls(projectPanels);
            else
                Helpers.ReadOnlyControls(projectPanels);

            dgv_project_multiplier.ReadOnly = !editable;

            foreach (TabPage tab in tabControl2.TabPages)
            {
                if (tab.Controls.Count > 0 && tab.Controls[0] is ItemSetUC uc)
                    uc.SetEditable(editable);
            }
        }
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
                if (isProject)
                    FinalizeProjectQuotation();
                else
                    FinalizeQuickQuotation();
            }
        }

        private async void FinalizeProjectQuotation()
        {
            Panel[] pnl_list = { pnl_header, pnl_footer, pnl_project_name };
            var pnl_quotation = Helpers.GetControlsValues(pnl_list);

            pnl_quotation["project_name"] = txt_project_name.Text.Trim();

            if (string.IsNullOrWhiteSpace(txt_project_name.Text))
            {
                MessageBox.Show("Please enter a valid project name. The project name cannot be empty or consist only of spaces.",
                                "Invalid Project Name", MessageBoxButtons.OK, MessageBoxIcon.Error);
                txt_project_name.Focus();
                return;
            }

            var multiplierSource = Helpers.ConvertDataGridViewToDataTable(dgv_project_multiplier);

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

            pnl_quotation["sales_project_multiplier"] = multipliers;

            var allTabsData = new List<Dictionary<string, object>>();

            foreach (TabPage selectedTab in this.tabControl2.TabPages)
            {
                if (selectedTab != null && selectedTab.Controls.Count > 0)
                {
                    var selectedControl = selectedTab.Controls[0] as ItemSetUC;

                    if (selectedControl != null)
                    {
                        var tabData = new Dictionary<string, object>();

                        int basedId;
                        if (pnl_quotation["id"] is long)
                            basedId = (int)(long)pnl_quotation["id"];
                        else
                            int.TryParse(pnl_quotation["id"].ToString(), out basedId);

                        tabData["sales_project_item_set"] = new Dictionary<string, object>
                        {
                            { "based_id",   basedId },
                            { "tab_number", selectedTab.Text },
                            { "itemset_id", selectedTab.Tag }
                        };

                        tabData["sales_project_history"] = selectedControl.GetHistoryList();
                        tabData["sales_project_content"] = selectedControl.GetProjectContentsData();
                        tabData["sales_project_content_advanced_condition"] = selectedControl.GetAdvancedConditionsData();
                        tabData["sales_project_items"] = selectedControl.GetProjectItems()["sales_project_items"];
                        tabData["sales_project_wiring"] = selectedControl.GetProjectWiringData()["sales_project_wiring"];

                        allTabsData.Add(tabData);
                    }
                }
            }

            pnl_quotation["sales_project_all_tabs"] = allTabsData;

            if (!ConvertToInt(pnl_quotation, "customer_id", "Invalid customer ID"))
                return;

            // Finalizing always creates a brand new, frozen "FQ#" record - same behavior
            // as FinalizeQuickQuotation. Whatever record we started from (new or an
            // existing draft being viewed/edited) is left completely untouched under
            // its own "Q#" - that's why id is forced to 0 for the insert below
            // regardless of isNewRecord/IsEdit.
            pnl_quotation["id"] = 0;
            pnl_quotation["is_finalized"] = true;

            // Bakes "FQ#" into document_no, mirroring FinalizeQuickQuotation - the
            // deliberate identifier for "this is a finalized quotation" at a glance.
            // Search/print already normalize away the "Q#"/"FQ#" prefix, so this
            // doesn't break those lookups.
            if (pnl_quotation.ContainsKey("document_no") && pnl_quotation["document_no"] is string documentNo)
            {
                string tempDocNo = documentNo.StartsWith("Q#") ? documentNo.Substring(2) : documentNo;
                tempDocNo = "FQ#" + tempDocNo;
                pnl_quotation["document_no"] = tempDocNo;
            }
            else
            {
                MessageBox.Show("Document number is missing or invalid.");
                return;
            }

            // Same duplicate guard FinalizeQuickQuotation runs against allTransactionList,
            // just against the Project quotation list instead.
            var duplicateTransaction = transactionProjectDataTable.AsEnumerable()
                .Where(t => t.Field<string>("document_no") == pnl_quotation["document_no"].ToString());

            if (duplicateTransaction.Any())
            {
                MessageBox.Show("A transaction cannot be finalized because this document number already exists.");
                return;
            }

            pnl_quotation["percent_discount"] = float.TryParse(txt_additional_discount.Text, out float discount) ? discount : 0;

            var quotation = JsonConvert.SerializeObject(pnl_quotation, Formatting.Indented);

            // Always insert as a new (finalized) record - this used to be gated behind
            // "if (isNewRecord)", so clicking Finalize on an already-existing project
            // quotation (View or Edit mode) silently did nothing at all.
            var response = await ProjectService.Insert(pnl_quotation);
            if (response.Success)
            {
                MessageBox.Show("Saved");
                SetNewFormMode(false);

                // Same as IsProject()'s save handling - drop back to read-only View mode
                // once the finalized record is actually saved.
                isNewRecord = false;
                IsEdit = false;

                await fetchSalesProjectData();
            }
            else
                MessageBox.Show($"Insert error: {response.message}");
        }

        private async void FinalizeQuickQuotation()
        {
            //try
            //{
            //    int quotationId = Convert.ToInt32(txt_id.Text);

            //    var finalizeData = new Dictionary<string, dynamic>
            //    {
            //        ["id"] = quotationId,
            //        ["is_finalized"] = true
            //    };

            //    await QuotationService.Update(finalizeData);

            //    Helpers.ResetControls(pnl_header);
            //    //Helpers.ResetControls(pnl_footer);
            //    ResetControls(pnl_footer);

            //    Panel[] panel = { pnl_header, pnl_footer };

            //    //Helpers.ResetControls(panel);

            //    MessageBox.Show("Quotation successfully finalized.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);

            //    await fetchQuotationDetails();
            //    SetFormEditMode("Close");
            //}
            //catch (Exception ex)
            //{
            //    MessageBox.Show("Error: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            //}

            try
            {
                Panel[] pnl_list = { pnl_header, pnl_footer };
                var parentData = Helpers.GetControlsValuesV2(pnl_list);
                bool isParsed = int.TryParse(txt_id.Text, out int id);

                int bill_to_id = 0;
                int ship_to_id = 0;

                if (cmb_bill_to.SelectedValue == null)
                {
                    MessageBox.Show("bill to is required.");
                }
                else
                {
                    bill_to_id = int.Parse(cmb_bill_to.SelectedValue.ToString());
                }

                if (cmb_ship_to.SelectedValue == null)
                {
                    MessageBox.Show("ship to is required.");
                }
                else
                {
                    ship_to_id = int.Parse(cmb_ship_to.SelectedValue.ToString());
                }

                parentData["id"] = 0;

                //if (isNewRecord || isSubVersion)
                //{
                //    parentData.Remove("id");
                //}


                parentData["ship_to_id"] = ship_to_id;


                var dataSource = Helpers.ConvertDataGridViewToDataTable(dgv_quick_quote_details);
                var newDatasource = Helpers.ConvertDataTableToStringTable(dataSource);

                List<Dictionary<string, dynamic>> quickQuoteList = new List<Dictionary<string, dynamic>>();
                // Tracks which original grid row each quickQuoteList entry came from, since
                // rows with item_id == 0 are skipped below and would otherwise throw the
                // indices out of sync with SelectedImagesByRow.
                List<int> quickQuoteRowIndexes = new List<int>();

                for (int i = 0; i < newDatasource.Rows.Count; i++)
                {
                    DataRow item = newDatasource.Rows[i];

                    int itemId = int.TryParse(item["item_id"].ToString(), out int ival) ? ival : 0;

                    if (itemId == 0)
                        continue;

                    Dictionary<string, object> data = new Dictionary<string, object>();

                    data.Add("item_id", itemId);
                    data.Add("bom_id", int.TryParse(item["quick_bom_id"].ToString(), out int bomid) ? bomid : 0);
                    data.Add("components", item["quick_item_code"]);
                    data.Add("model", item["quick_item_name"]);
                    data.Add("qty", int.TryParse(item["quick_qty"].ToString(), out int val) ? val : 0);
                    data.Add("unit_of_measure", item["quick_unit_of_measure"]);
                    data.Add("unit_price", decimal.TryParse(item["quick_unit_price"].ToString(), out decimal unitPrice) ? unitPrice : 0);
                    data.Add("percent_discount", item["quick_discount"].ToString());
                    data.Add("net_discount", decimal.Parse(Helpers.GetCleanedPriceValue(item["quick_net_discount"].ToString())));
                    data.Add("net_total", decimal.Parse(Helpers.GetCleanedPriceValue(item["quick_net_total"].ToString())));
                    data.Add("line_total", decimal.Parse(Helpers.GetCleanedPriceValue(item["quick_line_total"].ToString())));
                    data.Add("reference_code", item["reference_code"].ToString());
                    data.Add("short_description", item["short_description"].ToString());
                    data.Add("man_days", int.TryParse(item["man_days"].ToString(), out int manday) ? manday : 0);
                    data.Add("labor_rate", decimal.TryParse(item["labor_rate"].ToString(), out decimal laborday) ? laborday : 0);
                    quickQuoteList.Add(data);
                    quickQuoteRowIndexes.Add(i);

                }

                if (quickQuoteList != null)
                {
                    List<Dictionary<string, dynamic>> childCollection = new List<Dictionary<string, dynamic>>();

                    // loops thru the items - each row only gets the images that were
                    // selected for that specific row (falls back to empty if none picked)
                    for (int q = 0; q < quickQuoteList.Count; q++)
                    {
                        var dict = new Dictionary<string, dynamic>(quickQuoteList[q]);

                        int rowIndex = quickQuoteRowIndexes[q];
                        dict["quick_selected_image"] = SelectedImagesByRow.TryGetValue(rowIndex, out var rowImages)
                            ? rowImages
                            : new List<Dictionary<string, object>>();

                        childCollection.Add(dict);

                    }

                    // Finalizing bakes "FQ#" directly into the saved document_no - that's the
                    // deliberate identifier for "this is a finalized quotation" at a glance.
                    // Search and print lookups are normalized (NormalizeDocumentNo) to match
                    // regardless of the "Q#"/"FQ#" prefix, so this doesn't break those.
                    if (parentData.ContainsKey("document_no") && parentData["document_no"] is string documentNo)
                    {
                        string tempDocNo = documentNo.StartsWith("Q#") ? documentNo.Substring(2) : documentNo;

                        tempDocNo = "FQ#" + tempDocNo;

                        parentData["document_no"] = tempDocNo;
                    }
                    else
                    {
                        MessageBox.Show("Document number is missing or invalid.");
                        return;
                    }
                    parentData["is_finalized"] = true;

                    // MAKE A HELPER THAT CONVERT ID TO INT 
                    if (!Helpers.ConvertToIntIfString(parentData, "customer_id") ||
                        !Helpers.ConvertToIntIfString(parentData, "payment_terms_id") ||
                        !Helpers.ConvertToIntIfString(parentData, "ship_type_id"))
                    {
                        return;
                    }

                    parentData["sales_quotation_quick"] = childCollection;

                    var duplicateTransaction = allTransactionList.AsEnumerable()
                                                .Where(t => t.Field<string>("document_no") == parentData["document_no"].ToString());

                    if (duplicateTransaction.Any())
                    {
                        MessageBox.Show("A transaction cannot be finalized because this document number already exists.");
                        return;
                    }

                    if (parentData.ContainsKey("sales_quotation_quick"))
                    {

                        var isSuccess = await QuotationService.Insert(parentData);

                        if (isSuccess.Success)
                        {
                            Helpers.ResetControls(pnl_header);
                            ResetControls(pnl_footer);

                            dgv_quick_quote_details.DataSource = this.childList.Clone();
                            toolstrip_quotation.Enabled = true;

                            MessageBox.Show("Quotation Successfully saved");
                            await fetchQuotationDetails();

                            SetNewFormMode(false);
                        }
                        else
                            MessageBox.Show(isSuccess.message);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("ERROR: " + ex);
            }
        }
        private void btn_sales_order_Click(object sender, EventArgs e)
        {
            string documentNo = txt_document_no.Text;

            if (string.IsNullOrEmpty(documentNo))
            {
                MessageBox.Show("Please enter a valid document number.");
                return;
            }

            // Create an instance of Orders user control
            Orders ordersPage = new Orders(documentNo);
            // Match the width-fitting the generic tab-hosting path (Layout.showForm)
            // does — without this, Orders keeps its designed width (1229) and gets
            // clipped by the tab, since it's never actually resized to fit here.
            ordersPage.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            ordersPage.Width = this.Parent.ClientSize.Width;
            this.Parent.Controls.Add(ordersPage);
            this.Hide();
        }
        private void btn_print_Click(object sender, EventArgs e)
        {
            string documentNo = Regex.Replace(txt_document_no.Text, @"FQ#|Q#", "").Trim();
            if (isProject)
            {
                SalesPrintModal printPage = new SalesPrintModal(false, true, documentNo, InclusionsRichTextBox.Text, ExclusionsRichTextBox.Text, TermAndConditionsRichTextBox.Text);
                int screenHeight = Screen.PrimaryScreen.Bounds.Height;
                printPage.Height = (int)(screenHeight);
                printPage.StartPosition = FormStartPosition.CenterParent;
                printPage.ShowDialog();
            }
            else
            {
                SalesPrintModal printPage = new SalesPrintModal(true, false, documentNo, InclusionsRichTextBox.Text, ExclusionsRichTextBox.Text, TermAndConditionsRichTextBox.Text);
                int screenHeight = Screen.PrimaryScreen.Bounds.Height;
                printPage.Height = (int)(screenHeight);
                printPage.StartPosition = FormStartPosition.CenterParent;
                printPage.ShowDialog();
            }
        }
        // Older records can still have "Q#"/"FQ#" baked into their stored document_no (a
        // now-fixed save bug used to persist it that way), while documentNo passed into the
        // lookups below is always the bare number. Stripping both sides the same way before
        // comparing means lookups work for old and new records alike, without needing a
        // database migration to clean up the existing prefixed values.
        private static string NormalizeDocumentNo(string docNo) =>
            string.IsNullOrEmpty(docNo) ? docNo : Regex.Replace(docNo, @"FQ#|Q#", "").Trim();

        // Returns true if a Quick Quote record matching documentNo was found and bound.
        private async Task<bool> FetchQuotationDetailsByDocumentNo(string documentNo, string version_no = null, string sub_version_no = null)
        {
            SalesQuotationList data = await QuotationService.GetQuotations();
            var itemData = await ItemService.GetItem();
            ItemList = JsonHelper.ToDataTable(itemData.items);

            if (data == null || string.IsNullOrEmpty(documentNo))
            {
                return false;
            }

            // Either of these can legitimately come back null from the API - fall back to
            // an empty list instead of letting .Where() throw ArgumentNullException on a
            // null source.
            var filteredSalesQuotation = (data.SalesQuotation ?? Enumerable.Empty<SalesQuotationModel>())
                .Where(q => NormalizeDocumentNo(q.document_no) == documentNo &&
                           (version_no == null || q.version_no == version_no) &&
                           (sub_version_no == null || q.sub_version_no == sub_version_no))
                .ToList();

            var quotationId = filteredSalesQuotation.FirstOrDefault()?.id;

            if (quotationId != null)
            {
                var filteredSalesQuotationQuick = (data.SalesQuotationQuick ?? Enumerable.Empty<SalesQuotationQuicksModel>())
                    .Where(q => q.based_id == quotationId)
                    .ToList();

                var idsQuotationQuick = filteredSalesQuotationQuick.Select(q => q.id).ToList();

                // This was missing entirely, unlike the equivalent loader in
                // SalesPrintModal.cs - without it, the app has no record of which images
                // were already selected per item whenever an existing quotation gets opened
                // (including "New Version"), so every row's previously-saved images looked
                // like they'd been lost/never carried over.
                var filteredSalesQuotationImage = (data.SalesQuotationSelectedImages ?? Enumerable.Empty<SalesQuotationSelectedImageModel>())
                    .Where(q => idsQuotationQuick.Contains(q.quotation_quick_id))
                    .ToList();

                transactionList = JsonHelper.ToDataTable(filteredSalesQuotation);
                childList = JsonHelper.ToDataTable(filteredSalesQuotationQuick);
                selectedImageList = JsonHelper.ToDataTable(filteredSalesQuotationImage);

                // SelectedImagesByRow now gets (re)seeded down in
                // createFilterViewDgvQuickQouteDetails() once the grid is actually bound
                // below - that's the one place every "view an existing document" path goes
                // through, so seeding lives there instead of being duplicated per-caller.

                Panel[] panels = { pnl_header, pnl_footer };
                Helpers.ResetReadOnlyControls(panels);

                //pnl_header.Enabled = true;
                //pnl_footer.Enabled = true;
                toolstrip_quotation.Enabled = false;
                dgv_quick_quote_details.Enabled = true;

                toolstrip_quotation.Enabled = true;
                if (filteredSalesQuotation.Any() || filteredSalesQuotationQuick.Any())
                {
                    SelectedRow = 0;
                    bind(transactionList, SelectedRow, true);
                    // bind() only fills the header/footer text controls - without this the
                    // items grid itself stays empty when a document is opened straight from
                    // a documentNo (e.g. from Opportunities/Orders), unlike opening the
                    // module blank and using Search, which does call this.
                    createFilterViewDgvQuickQouteDetails();
                }
                else
                {
                    MessageBox.Show("No records found for the provided document number.");
                }

                return true;
            }
            else
            {
                // Not found here just means this document is a Project quotation instead -
                // the caller checks both lookups before deciding it's a real error.
                return false;
            }
        }

        // Returns true if a Project quotation record matching documentNo was found and bound.
        private async Task<bool> FetchProjectDetailsByDocumentNo(string documentNo, string version_no = null, string sub_version_no = null)
        {
            SalesProjectList data = await ProjectService.GetProjects();

            if (data == null || string.IsNullOrEmpty(documentNo))
            {
                return false;
            }

            // Can legitimately come back null from the API - fall back to an empty list
            // instead of letting .Where() throw ArgumentNullException on a null source.
            var filteredSalesQuotation = (data.SalesQuotation ?? Enumerable.Empty<SalesQuotationModel>())
                .Where(q => NormalizeDocumentNo(q.document_no) == documentNo &&
                           (version_no == null || q.version_no == version_no) &&
                           (sub_version_no == null || q.sub_version_no == sub_version_no))
                .ToList();

            var quotationId = filteredSalesQuotation.FirstOrDefault()?.id;

            if (quotationId != null)
            {
                var filteredSalesQuotationQuick = (data.SalesQuotation ?? Enumerable.Empty<SalesQuotationModel>())
                    .Where(q => q.id == quotationId)
                    .ToList();

                transactionProjectDataTable = JsonHelper.ToDataTable(filteredSalesQuotation);


                Panel[] panels = { pnl_header, pnl_footer };
                Helpers.ResetReadOnlyControls(panels);

                toolstrip_quotation.Enabled = false;
                dgv_quick_quote_details.Enabled = true;

                toolstrip_quotation.Enabled = true;
                if (filteredSalesQuotation.Any() || filteredSalesQuotationQuick.Any())
                {
                    bind(transactionProjectDataTable, selectedProjectRow, true);
                }
                else
                {
                    MessageBox.Show("No records found for the provided document number.");
                }

                return true;
            }
            else
            {
                // Not found here just means this document is a Quick Quote instead -
                // the caller checks both lookups before deciding it's a real error.
                return false;
            }
        }
        private void back_Click(object sender, EventArgs e)
        {
            Opportunities OpportunitiesPage = new Opportunities();
            //OpportunitiesPage.Width = this.Parent.Width;
            this.Parent.Controls.Add(OpportunitiesPage);
            this.Dispose();
        }
        private void btn_request_for_engr_Click(object sender, EventArgs e)
        {
            ProjectTest pt = new ProjectTest();
            pt.Show();

        }
        // Only the user who originally created a quotation (quick quote or project) is
        // allowed to edit/update it. txt_created_by is already bound to the loaded record's
        // "created_by" value by bind()/BindControls before either Edit or Update can be
        // clicked, so it reflects whoever actually owns the record on screen right now - not
        // necessarily the current user. A blank value (e.g. a record predating this field)
        // isn't treated as a block, only a genuine mismatch is.
        private bool IsRecordCreatedByCurrentUser(string createdBy)
        {
            if (string.IsNullOrWhiteSpace(createdBy) || CacheData.CurrentUser == null)
                return true;

            string currentUserName = $"{CacheData.CurrentUser.first_name} {CacheData.CurrentUser.last_name}".Trim();
            return string.Equals(createdBy.Trim(), currentUserName, StringComparison.OrdinalIgnoreCase);
        }

        private void btn_update_Click(object sender, EventArgs e)
        {
            if (!IsRecordCreatedByCurrentUser(txt_created_by.Text))
            {
                MessageBox.Show("Only the user who created this quotation can update it.", "Not Allowed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

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
        // bpi_address is only guaranteed populated after fetchBpiData() finishes (same
        // async-load pattern as bpi_general/bpi_contacts, see bind() above). Called before
        // that, it's still the empty new DataTable() initializer with zero columns - filtering
        // it returns an equally columnless clone, and setting DisplayMember/ValueMember on it
        // throws "Cannot bind to the new display member" instead of leaving the combo empty.
        private static bool HasAddressColumns(DataTable table)
        {
            return table != null
                && table.Columns.Contains("address_based_id")
                && table.Columns.Contains("location")
                && table.Columns.Contains("address_ids");
        }

        private void LoadCustomerBillAddress(string id)
        {
            if (!HasAddressColumns(bpi_address)) return;

            var BillAddress = Helpers.FilterDataTable(bpi_address, id, "address_based_id");

            cmb_bill_to.DataSource = BillAddress;
            cmb_bill_to.DisplayMember = "location";
            cmb_bill_to.ValueMember = "address_ids";
        }
        private void LoadCustomerShipAddress(string id)
        {
            if (!HasAddressColumns(bpi_address)) return;

            var ShipAddress = Helpers.FilterDataTable(bpi_address, id, "address_based_id");

            cmb_ship_to.DataSource = ShipAddress;
            cmb_ship_to.DisplayMember = "location";
            cmb_ship_to.ValueMember = "address_ids";

        }

        private void btn_edit_Click(object sender, EventArgs e)
        {
            // Belt-and-suspenders like the IsRecordCreatedByCurrentUser check right
            // below: bind() hides this button when isFinalized, but that's the only
            // thing stopping a finalized quotation from being edited. If the button
            // is ever re-enabled by something else, this stops the edit at the
            // handler itself instead of relying solely on button visibility.
            if (isFinalized)
            {
                MessageBox.Show("This quotation is already finalized and can no longer be edited.", "Not Allowed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!IsRecordCreatedByCurrentUser(txt_created_by.Text))
            {
                MessageBox.Show("Only the user who created this quotation can edit it.", "Not Allowed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            IsView = false;
            string customerId = txt_customer_id.Text;

            // NOTE: previously called GetLatestDate() here, which forced dtp_date.Value = DateTime.Now
            // on every Edit click - even for an already-saved record. That overwrote the record's real
            // stored date with "today" before any save happened, so the auto Change History always
            // detected a false "date changed" diff. Editing an existing record must keep its own date;
            // only genuinely new records (New / New Version / Duplicate) should default to today.
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
                //Helpers.ReadOnlyControls(pnls);
                dgv_quick_quote_details.ReadOnly = false;
                txt_cash_discount.ReadOnly = false;
                txt_additional_discount.ReadOnly = false;
                EnableDescription(true);


                foreach (Control ctrl in pnl_footer.Controls)
                {
                    if(ctrl.Name == "txt_short_description" || ctrl.Name == "txt_long_description")
                    {
                        continue;
                    }

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

            // Continue numbering from the highest CODE already on the loaded rows instead
            // of restarting at 0 - otherwise items added while editing an already-saved
            // quotation reuse codes that are already in use (e.g. 1,2,3 -> 1,2 again
            // instead of continuing on to 4,5).
            counterReference = GetMaxTopLevelReferenceCode(dgv_quick_quote_details);
            counterParent = 1;
            SelectedRowIndex = 0;
            IsEdit = true;

        }
        private void GetLatestDate()
        {
            dtp_date.Value = DateTime.Now;
            txt_validays.Text = "30";
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
            //tssb_Print.Visible = !isTrue;
            btn_finalize.Enabled = !isTrue;

            // Show action button
            btn_savee.Visible = isTrue;
            btn_close.Visible = isTrue;
            btn_add_customer.Visible = isTrue;
            dgv_quick_quote_details.Enabled = isTrue;
            dgv_quick_quote_details.ReadOnly = !isTrue;
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
            //tssb_Print.Visible = false;

            // Enable editing controls
            btn_savee.Visible = true;
            btn_close.Visible = true;
            btn_add_customer.Visible = true;

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
                //tssb_Print.Visible = true;

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
            IsView = true;
            SetNewFormMode(false);
            SetFormEditMode("Close");

            await LoadExistingRecord();

            Panel[] panels = { pnl_header, pnl_footer };
            Helpers.ReadOnlyControls(panels);
            Helpers.ResetControls(panels);
            //pnl_header.Enabled = false;
            //pnl_footer.Enabled = false;

            toolstrip_quotation.Enabled = true;

        }
        private void btn_duplicate_Click(object sender, EventArgs e)
        {
            GetLatestDate();
            isNewRecord = true;
            IsEdit = false;
        }
        // NOTE: ComputeQuickQuoteTotal / ComputeDgvHierarchy / computationLoop / DGVComputation
        // below are not called from any wired event or other live code path (confirmed via
        // Designer.cs and full-file search) - this looks like an abandoned parallel
        // implementation of what ComputeByReferenceHierarchy/ComputeReferenceNonHierarchy/
        // ComputeFooterTotals do live. Left in place rather than deleted since they're
        // unreachable either way, but flagging so nobody reconnects them expecting them to
        // match the live computation path's behavior without re-checking them first.
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
            BoldWords(TermAndConditionsRichTextBox, "LIABILITY:");

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

        private void toolStripMenuItemTagRed_Click(object sender, EventArgs e)
        {
            int selectedIndex = tabControl2.SelectedIndex;
            TabPage selectedTabPage = tabControl2.TabPages[selectedIndex];

            // Tracked separately from Tag - Tag holds the tab's itemset_id and must not be
            // overwritten with a color (that used to silently make a saved tab look brand-new
            // again on the next save, since GetItemSetIdFromTab couldn't parse a Color as an id).
            if (_redFlaggedTabs.Contains(selectedTabPage))
            {
                _redFlaggedTabs.Remove(selectedTabPage);
            }
            else
            {
                _redFlaggedTabs.Add(selectedTabPage);
            }

            tabControl2.Invalidate();

            // Flagging/unflagging a tab changes which tabs count toward the totals - update
            // the bottom panel right away instead of waiting for the next item edit.
            RecomputeParentTotals();
        }

        private void toolStripMenuItemRenameTabs_Click(object sender, EventArgs e)
        {
            // Select the tab that was right-clicked (optional, but good UX)
            int selectedIndex = tabControl2.SelectedIndex;
            string tabNewName = NamingTabControl(selectedIndex);

            // Renames the tab here
            tabControl2.TabPages[selectedIndex].Text = tabNewName;
        }

        // "Remove Tabs" already existed in the right-click menu (Designer.cs), but had no
        // Click handler wired to it at all, so clicking it did nothing. This removes the
        // currently selected Project tab, the same "act on the selected tab" pattern the
        // RedFlag/Rename menu items already use.
        private void toolStripMenuItemRemoveTabs_Click(object sender, EventArgs e)
        {
            int selectedIndex = tabControl2.SelectedIndex;

            if (selectedIndex < 0 || selectedIndex >= tabControl2.TabPages.Count)
                return;

            // The last tab is always the "+" add-tab control, not a real item set - never
            // let it be removed this way.
            if (tabControl2.TabPages[selectedIndex].Text == "+")
            {
                MessageBox.Show("That's the \"+\" add-tab button, not a project tab.");
                return;
            }

            // Keep at least one real tab - a project quotation needs somewhere for its items
            // to live (the "+" tab doesn't count as a real one).
            int realTabCount = tabControl2.TabPages.Count - 1;
            if (realTabCount <= 1)
            {
                MessageBox.Show("A project quotation needs at least one tab - add another before removing this one.");
                return;
            }

            string tabName = tabControl2.TabPages[selectedIndex].Text;
            DialogResult confirm = MessageBox.Show(
                $"Remove tab \"{tabName}\" and all its items? This can't be undone.",
                "Remove Tab",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (confirm != DialogResult.Yes)
                return;

            _redFlaggedTabs.Remove(tabControl2.TabPages[selectedIndex]);
            _newlyCreatedTabs.Remove(tabControl2.TabPages[selectedIndex]);
            tabControl2.TabPages.RemoveAt(selectedIndex);
            RecomputeParentTotals();
        }

        //
        const string HeaderLabel = "Header -> ";
        const string FooterLabel = "Footer -> ";
        const string ProjectLabel = "Project Header: ";
        const string ProjectMulti = "Multiplier Table -> ";

        //Per tabs
        const string ActionDetails = "Sub Project: ";
        const string AdvanceConditions = "Advance Conditions: ";
        const string ProjectItems = "Project Items Table -> ";
        const string ProjectWiring = "Project Wiring Table -> ";

        //Additional
        const string ActionArrowLeft = "-> ";

        private void AddProjectHistory(Dictionary<string, dynamic> pnlQuotation, uint basedId, string user, string oldData, string newData)
        {
            // Create history entry
            var now = DateTime.Now;

            var historyEntry = new Dictionary<string, object>
            {
                ["based_id"] = basedId,
                ["user"] = user,
                ["date"] = now.ToString("yyyy-MM-dd"),
                ["time"] = now.ToString("HH:mm:ss"),
                ["old_data"] = oldData,
                ["new_data"] = newData
            };

            // Check if history list exists
            if (!pnlQuotation.ContainsKey("sales_project_history"))
            {
                pnlQuotation["sales_project_history"] = new List<Dictionary<string, object>>();
            }

            // Add to list
            var historyList = pnlQuotation["sales_project_history"] as List<Dictionary<string, object>>;
            historyList.Add(historyEntry);
        }
        private void printShow()
        {
            string documentNo = Regex.Replace(txt_document_no.Text, @"FQ#|Q#", "").Trim();
            if (isProject)
            {
                SalesPrintModal printPage = new SalesPrintModal(false, true, documentNo, InclusionsRichTextBox.Text, ExclusionsRichTextBox.Text, TermAndConditionsRichTextBox.Text);
                int screenHeight = Screen.PrimaryScreen.Bounds.Height;
                printPage.Height = (int)(screenHeight);
                printPage.StartPosition = FormStartPosition.CenterParent;
                printPage.ShowDialog();
            }
            else
            {
                SalesPrintModal printPage = new SalesPrintModal(true, false, documentNo, InclusionsRichTextBox.Text, ExclusionsRichTextBox.Text, TermAndConditionsRichTextBox.Text);
                int screenHeight = Screen.PrimaryScreen.Bounds.Height;
                printPage.Height = (int)(screenHeight);
                printPage.StartPosition = FormStartPosition.CenterParent;
                printPage.ShowDialog();
            }
        }

        private void advancePrintToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string documentNo = Regex.Replace(txt_document_no.Text, @"FQ#|Q#", "").Trim();
            if (isProject)
            {
                SalesPrint printPage = new SalesPrint(true, documentNo, InclusionsRichTextBox.Text, ExclusionsRichTextBox.Text, TermAndConditionsRichTextBox.Text);
                int screenHeight = Screen.PrimaryScreen.Bounds.Height;
                printPage.Height = (int)(screenHeight);
                printPage.StartPosition = FormStartPosition.CenterParent;
                printPage.ShowDialog();
            }
            else
            {
                SalesPrint printPage = new SalesPrint(false, documentNo, InclusionsRichTextBox.Text, ExclusionsRichTextBox.Text, TermAndConditionsRichTextBox.Text);
                int screenHeight = Screen.PrimaryScreen.Bounds.Height;
                printPage.Height = (int)(screenHeight);
                printPage.StartPosition = FormStartPosition.CenterParent;
                printPage.ShowDialog();
            }
        }

        private void txt_project_name_Leave(object sender, EventArgs e)
        {
            string action = ProjectLabel + txt_project_name.Text;
        }

        private void basicPrintToolStripMenuItem_Click(object sender, EventArgs e)
        {
            printShow();
        }



        private void tssb_Print_ButtonClick(object sender, EventArgs e)
        {
            printShow();
        }

        private void txt_contact_1_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!char.IsDigit(e.KeyChar) && !char.IsControl(e.KeyChar))
            {
                e.Handled = true;
            }
        }

        private void txt_contact_2_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!char.IsDigit(e.KeyChar) && !char.IsControl(e.KeyChar))
            {
                e.Handled = true;
            }
        }
    }
}