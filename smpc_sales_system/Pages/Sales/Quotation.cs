using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using smpc_app.Data;
using smpc_app.Services.Helpers;
using smpc_inventory_app.Model;
using smpc_inventory_app.Pages;
using smpc_sales_app.Data;
using smpc_sales_app.Pages.Sales.Modal;
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
using System.Configuration;
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

        // Polls the server every 5 minutes while a Project Quotation is open so any changes
        // another user saved in the meantime (fields, tabs, multipliers) show up here too,
        // and Change History (RenderTabHistory) stays current - see
        // ProjectAutoRefreshTimer_Tick / RefreshCurrentProjectQuotationAsync. Kept running as
        // a fallback even once real-time notifications (below) are connected, in case that
        // socket ever silently drops.
        private System.Windows.Forms.Timer projectAutoRefreshTimer;

        // Dedicated "someone else just saved this project" notification channel - separate
        // from _websocket/ConnectToWebSocket above (that one drives the still-unfinished,
        // never-actually-connected live field-by-field co-editing relay - see
        // fetchSalesProjectRT/SendMessageAsync). Keeping this one independent means wiring up
        // real-time save notifications can't accidentally revive that other, untested code
        // path as a side effect. The server broadcasts a small { "event": "quotation_saved" }
        // ping on this project's own channel (see BroadcastToProject in UpdateSalesProject)
        // whenever anyone saves it.
        private ClientWebSocket _saveNotifyWebSocket;
        private CancellationTokenSource _saveNotifyCts;
        private string _saveNotifyProjectId;

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

            // Auto-refresh Project Quotation every 5 minutes so other users' saved changes
            // (and Change History) show up without needing a manual reopen. The tick handler
            // itself decides whether there's actually anything worth refreshing (Project tab
            // open, a saved record loaded, not mid-edit), so it's safe to just let this run
            // for the lifetime of the control.
            projectAutoRefreshTimer = new System.Windows.Forms.Timer { Interval = 5 * 60 * 1000 };
            projectAutoRefreshTimer.Tick += ProjectAutoRefreshTimer_Tick;
            projectAutoRefreshTimer.Start();
            this.Disposed += (s, e) =>
            {
                projectAutoRefreshTimer.Stop();
                projectAutoRefreshTimer.Dispose();
                DisconnectSaveNotify();
            };
        }

        // ---- Real-time "someone else saved this project" notifications ----

        private static string GetWebSocketBaseUrl()
        {
            string env = ConfigurationManager.AppSettings["Environment"] ?? "Development";

            // No hardcoded fallback URL - App.config's ApiBaseUrl.{env} is the one place this
            // is supposed to live, since it changes (localhost in dev, the real host in
            // production). Silently falling back to a hardcoded address just masks a missing/
            // misspelled App.config entry instead of surfacing it, and if the two ever
            // drifted, this would happily keep pointing at the wrong server. Failing loudly
            // here is safe: EnsureSaveNotifyConnected below already catches this and falls
            // back to the 5-minute polling timer, it just logs why real-time didn't connect
            // instead of silently guessing an address.
            string apiBaseUrl = ConfigurationManager.AppSettings[$"ApiBaseUrl.{env}"];
            if (string.IsNullOrWhiteSpace(apiBaseUrl))
                throw new ConfigurationErrorsException($"App.config is missing \"ApiBaseUrl.{env}\" - add it under <appSettings> instead of relying on a hardcoded default.");

            if (apiBaseUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                return "wss://" + apiBaseUrl.Substring("https://".Length);
            if (apiBaseUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
                return "ws://" + apiBaseUrl.Substring("http://".Length);
            return apiBaseUrl;
        }

        // Connects (or reconnects, if a different project is now open) to this project's own
        // broadcast channel so a save from another user shows up right away instead of
        // waiting for the next 5-minute timer tick. Safe to call every time fetchSalesProject()
        // resolves a project id - it no-ops if already listening for that same id, and never
        // throws out to its caller (a fresh new record with no id yet, or the server/socket
        // being unreachable, should just silently fall back to the 5-minute timer instead of
        // interrupting the user).
        private async void EnsureSaveNotifyConnected(string projectId)
        {
            if (string.IsNullOrWhiteSpace(projectId) || projectId == "0") return;
            if (_saveNotifyProjectId == projectId && _saveNotifyWebSocket?.State == System.Net.WebSockets.WebSocketState.Open) return;

            DisconnectSaveNotify();

            _saveNotifyProjectId = projectId;
            CancellationTokenSource cts = new CancellationTokenSource();
            ClientWebSocket socket = new ClientWebSocket();
            _saveNotifyCts = cts;
            _saveNotifyWebSocket = socket;

            try
            {
                Uri uri = new Uri($"{GetWebSocketBaseUrl()}/ws/setup/test?branch=Sales&projectid={projectId}");
                await socket.ConnectAsync(uri, cts.Token);
                _ = ListenForSaveNotificationsAsync(socket, cts, projectId);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Real-time update channel unavailable, falling back to periodic refresh: {ex.Message}");
            }
        }

        private async Task ListenForSaveNotificationsAsync(ClientWebSocket socket, CancellationTokenSource cts, string projectId)
        {
            byte[] buffer = new byte[64 * 1024];
            try
            {
                while (socket.State == System.Net.WebSockets.WebSocketState.Open)
                {
                    List<byte> messageBuffer = new List<byte>();
                    WebSocketReceiveResult result;
                    do
                    {
                        result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), cts.Token);
                        messageBuffer.AddRange(buffer.Take(result.Count));
                    } while (!result.EndOfMessage);

                    if (result.MessageType != WebSocketMessageType.Text) continue;

                    string json = Encoding.UTF8.GetString(messageBuffer.ToArray());

                    JToken token;
                    try { token = JToken.Parse(json); }
                    catch { continue; }

                    if (token["event"]?.ToString() != "quotation_saved") continue;

                    Invoke(new Action(() => HandleQuotationSavedNotification(projectId)));
                }
            }
            catch (Exception)
            {
                // Connection dropped (server restart, network blip, etc.) - the 5-minute
                // timer keeps things eventually-consistent even without reconnect logic here.
            }
        }

        // async void, but with its own try/catch (same reasoning as fetchSalesProject above) -
        // this runs off the back of Invoke() from the socket's background receive loop, so an
        // unhandled exception here would surface as an unhandled exception on the UI thread
        // instead of just skipping this one notification.
        private async void HandleQuotationSavedNotification(string projectId)
        {
            try
            {
                // Never refresh out from under someone who's actively editing/creating, and
                // ignore a stale notification for a record that isn't even open anymore (the
                // user navigated away since this notification was sent).
                if (!isProject || IsEdit || isNewRecord) return;
                if (ToInt(txt_id.Text) != ToInt(projectId)) return;

                await RunWithLoadingAsync(async () => await RefreshCurrentProjectQuotationAsync(), "Another user just updated this quotation - refreshing...");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error handling real-time quotation update: {ex.Message}");
            }
        }

        private void DisconnectSaveNotify()
        {
            try { _saveNotifyCts?.Cancel(); } catch { }
            try { _saveNotifyWebSocket?.Dispose(); } catch { }
            _saveNotifyWebSocket = null;
            _saveNotifyCts = null;
            _saveNotifyProjectId = null;
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
            decimal gross = 0, vat = 0, net = 0;

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

                    AddDecimal(data, "gross_sales", ref gross);
                    AddDecimal(data, "vat_amount", ref vat);
                    AddDecimal(data, "net_sales", ref net);
                }
            }

            // Cash discount and the project-wide discount percentage are computed exactly
            // once here, against the whole project's summed totals - not per tab. This used
            // to be summed in from each tab's own copy of the same GetCashDiscount() value
            // (see the removed cash-discount handling in ItemSetUC.ProjectComputationLoop),
            // which double- (or N-) counted the discount for any project with more than one
            // active tab, and then wrote that inflated sum straight back into
            // txt_cash_discount.Text - causing it to balloon further on every subsequent
            // edit. Re-parsing/re-formatting the same single value here is safe (idempotent);
            // it just normalizes whatever the user typed.
            decimal cash_disc = GetCashDiscount();
            decimal percent = gross != 0 ? ((gross - net) / gross) * 100 : 0;
            decimal net_amount = net - cash_disc;
            decimal total_amount = net_amount + vat;

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
        private async void btn_quick_quote_Click(object sender, EventArgs e)
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

            // Leaving Project Quotation - no point staying connected to its real-time
            // save-notification channel.
            DisconnectSaveNotify();

            Helpers.ResetControls(pnl_header);
            ResetControls(pnl_footer);

            // Same loading overlay + button lock used for Project Quotation's equivalent
            // (btn_project_Click) - this previously fired fetchQuotationDetails() without
            // awaiting it, so the handler returned (and buttons stayed clickable) before the
            // fetch had even reached its first await, and nothing locked the screen while
            // bind() repopulated pnl_header/pnl_footer.
            await RunWithLoadingAsync(async () => await fetchQuotationDetails(), "Loading...");
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

        private async void btn_project_Click(object sender, EventArgs e)
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

            // fetchSalesProjectData() fetches from the server and then (via fetchSalesProject())
            // rebuilds the header/footer fields, the multiplier grid and every per-tab
            // ItemSetUC (with its own item/wiring/final grids) - all of that is empty/stale
            // until it finishes, so cover the whole switch-to-Project-Quotation flow with the
            // same loading overlay + button lock used for Quick Quote.
            await RunWithLoadingAsync(async () => await fetchSalesProjectData(), "Loading...");
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
            projectQuotationTerms();
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

            // Cover pnl_header/pnl_footer too, not just the grid - bind() (below) fills
            // both of those from the same fetch, so leaving them uncovered meant the header/
            // footer fields visibly popped in (or briefly showed blank/reset text - see
            // btn_quick_quote_Click, which clears them right after kicking this off) instead
            // of staying hidden behind the overlay until everything is actually ready.
            Control[] loadingTargets = { pnl_header, pnl_footer, dgv_quick_quote_details };
            Helpers.Loading.ShowLoading(loadingTargets, "Loading...");

            try
            {
                data = await QuotationService.GetQuotations();

                //projectData = await

                if (data != null && data.SalesQuotation != null && data.SalesQuotation.Any())
                {
                    // Get latest quotation by version and subversion
                    var latestQuotations = data.SalesQuotation
                        .GroupBy(q => q.document_no)
                        .Select(group => group
                        .OrderByDescending(q => VersionNoAsInt(q.version_no))
                        .ThenByDescending(q => VersionNoAsInt(q.sub_version_no))
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
                        // Stay locked/read-only here (matches the rest of the blank/view state) -
                        // "New Quote" is what should unlock the fields, not just landing on this
                        // module with nothing to show yet.

                        // bind() (which normally sets these off isFinalized) never runs here
                        // since there's no record - without this, Finalize/Sales Order were left
                        // at whatever SetNewFormMode(false) set them to (Finalize enabled), even
                        // though there's nothing to finalize yet.
                        btn_finalize.Enabled = false;
                        btn_sales_order.Enabled = false;
                    }
                    else
                    {
                        SelectedRow = ownedIndexes[0];

                        bind(transactionList, SelectedRow, true);

                        createFilterViewDgvQuickQouteDetails();
                    }

                }
                else
                {
                    MessageBox.Show("Please create a new data!");

                    // Same as above - leave the form locked; "New Quote" unlocks it.
                    //pnl_header.Enabled = true;
                    //pnl_footer.Enabled = true;

                    btn_finalize.Enabled = false;
                    btn_sales_order.Enabled = false;
                }
            }
            finally
            {
                Helpers.Loading.HideLoading(loadingTargets);
                toolstrip_quotation.Enabled = true;
            }
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

            // Deliberately not grouped down to one row per document_no here (unlike
            // fetchQuotationDetails above) - Project Quotation's list intentionally keeps
            // every version so Prev/Next can page through a project's version history. The
            // ordering still needs to be numeric, though, so index 0 (the default-opened row,
            // see selectedProjectRow below) is actually the latest version and not just
            // whichever version happens to sort first as a string.
            var latestQuotations = SalesProjectListData.SalesQuotation
            .OrderByDescending(q => VersionNoAsInt(q.version_no))
            .ThenByDescending(q => VersionNoAsInt(q.sub_version_no))
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
                UC.CellClickedStock += ItemSetUC_CellClickedStock;
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

        private async void ProjectAutoRefreshTimer_Tick(object sender, EventArgs e)
        {
            // Only meaningful for Project Quotation, only once a real (already-saved) record
            // is open (nothing to pull for a brand-new one that hasn't been created yet), and
            // never while the user is actively editing or building a new record - a refresh
            // mid-edit would rebuild every tab from scratch and silently wipe unsaved changes.
            if (!isProject || IsEdit || isNewRecord) return;
            if (ToInt(txt_id.Text) <= 0) return;

            await RunWithLoadingAsync(async () => await RefreshCurrentProjectQuotationAsync(), "Checking for updates...");
        }

        // Silent background refresh for the auto-refresh timer above - unlike
        // fetchSalesProjectData(), this never shows a MessageBox, never fabricates a new blank
        // project tab, and never snaps the view back to row 0. It re-fetches from the server
        // and rebinds the SAME record (matched by id) that's already open, so another user's
        // edits appear without yanking focus to a different version. If that record can't be
        // found anymore (e.g. deleted), it leaves the current view untouched rather than
        // guessing.
        private async Task RefreshCurrentProjectQuotationAsync()
        {
            if (!isProject) return;

            int currentId = ToInt(txt_id.Text);
            if (currentId <= 0) return;

            SalesProjectList refreshedData = await ProjectService.GetProjects();
            if (refreshedData?.SalesQuotation == null) return;

            var latestQuotations = refreshedData.SalesQuotation
                .OrderByDescending(q => VersionNoAsInt(q.version_no))
                .ThenByDescending(q => VersionNoAsInt(q.sub_version_no))
                .ToList();

            DataTable refreshedTable = JsonHelper.ToDataTable(latestQuotations);

            int matchedRow = -1;
            for (int i = 0; i < refreshedTable.Rows.Count; i++)
            {
                if (ToInt(refreshedTable.Rows[i]["id"]) == currentId)
                {
                    matchedRow = i;
                    break;
                }
            }

            if (matchedRow == -1) return; // record no longer present - leave the view as-is

            SalesProjectListData = refreshedData;
            transactionProjectDataTable = refreshedTable;
            selectedProjectRow = matchedRow;

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

            // Listen for real-time "someone else saved this" notifications for whichever
            // project is actually open now - no-ops if already connected to this same id.
            EnsureSaveNotifyConnected(selectedId);

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
                UC.CellClickedStock += ItemSetUC_CellClickedStock;
                //UC.DeleteReferenceCode += DeleteRowsByReferenceCode;

                //UC.ItemChanged += ItemChanged;
                UC.FinalTxtBoxClicked += FinalTxtBoxClicked;
                UC.SizeUpClicked += SizeUpClicked;
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

        // Same required-field checks IsQuickQuote() already enforces (tickets
        // #243/#245/#246/#247) - Project Quotation shares the same header controls
        // (cmb_bill_to, cmb_ship_to, cmb_payment_terms, cmb_ship_type, dtp_valid_until) but
        // its save/finalize paths never applied any of these checks, so a Project Quotation
        // could previously be saved or finalized with all of them left blank, or with a
        // Valid Until date already in the past (reaching the API as a raw error instead of
        // a friendly message, same as bug #243/#245 did for Quick Quote).
        private bool ValidateProjectRequiredFields()
        {
            if (cmb_bill_to.SelectedValue == null)
            {
                MessageBox.Show("Bill To is required.", "Missing Information", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            if (cmb_ship_to.SelectedValue == null)
            {
                MessageBox.Show("Ship To is required.", "Missing Information", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            if (cmb_payment_terms.SelectedValue == null)
            {
                MessageBox.Show("Payment Terms is required.", "Missing Information", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                cmb_payment_terms.Focus();
                return false;
            }

            if (cmb_ship_type.SelectedValue == null)
            {
                MessageBox.Show("Ship Type is required.", "Missing Information", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                cmb_ship_type.Focus();
                return false;
            }

            if (dtp_valid_until.Value.Date < DateTime.Today)
            {
                MessageBox.Show("Valid Until date cannot be in the past. Please choose a later date.",
                    "Invalid Date", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                dtp_valid_until.Focus();
                return false;
            }

            return true;
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

            // No customer selected (txt_customer_id is only ever populated by the
            // "Select Customer" dialog in btn_add_customer_Click) - block the save
            // instead of letting a quotation with no customer through.
            if (string.IsNullOrWhiteSpace(txt_customer_id.Text))
            {
                MessageBox.Show("Please select a customer before saving.", "Missing Information", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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

            if (!ValidateProjectRequiredFields())
                return;

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
                    await RunWithLoadingAsync(async () => await fetchSalesProjectData(), "Loading...");

                    // Every tab's rows now have real project_items_id values from the reload -
                    // apply any RESERVE/release toggled in a tab's stock checker before this save.
                    await ApplyPendingProjectReservationsAsync();
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
                    await RunWithLoadingAsync(async () => await fetchSalesProjectData(), "Loading...");

                    // Same as the isNewRecord branch above.
                    await ApplyPendingProjectReservationsAsync();
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
            // Without this, a missing "is_finalized"/"is_project" key in the save payload
            // (the normal save path never sets either - only FinalizeProjectQuotation sets
            // is_finalized) compared against the real db value of `false` was treated as a
            // genuine change (false vs null aren't equal), so every regular save logged a
            // bogus "False to -" change-history entry for both fields even though nothing
            // about them actually changed.
            if (val is bool bl) return bl == false;
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

            // No customer selected (txt_customer_id is only ever populated by the
            // "Select Customer" dialog in btn_add_customer_Click) - block the save
            // instead of letting a quotation with no customer through.
            if (string.IsNullOrWhiteSpace(txt_customer_id.Text))
            {
                MessageBox.Show("Please select a customer before saving.", "Missing Information", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
                    MessageBox.Show("Bill To is required.", "Missing Information", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    // Previously fell through and saved anyway with bill_to_id defaulted to 0 -
                    // the warning was shown but never actually stopped the save (bug #246).
                    return;
                }
                else
                {
                    bill_to_id = int.Parse(cmb_bill_to.SelectedValue.ToString());
                }

                if (cmb_ship_to.SelectedValue == null)
                {
                    MessageBox.Show("Ship To is required.", "Missing Information", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                else
                {
                    ship_to_id = int.Parse(cmb_ship_to.SelectedValue.ToString());
                }

                // Bug #246 (continued): Bill To/Ship To were the only required dropdowns that
                // actually blocked the save - Payment Terms and Ship Type were left completely
                // unvalidated, so a quotation could be saved with those blank (as reported:
                // saves fine with Payment Terms/Ship Type left at "--Select--"/empty).
                if (cmb_payment_terms.SelectedValue == null)
                {
                    MessageBox.Show("Payment Terms is required.", "Missing Information", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    cmb_payment_terms.Focus();
                    return;
                }

                if (cmb_ship_type.SelectedValue == null)
                {
                    MessageBox.Show("Ship Type is required.", "Missing Information", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    cmb_ship_type.Focus();
                    return;
                }

                // "Valid Until" is derived from the document date + number of days
                // (ValidUntilDate()), but the document date itself is user-editable, so it's
                // still possible to end up with a Valid Until in the past. That used to reach
                // the API and come back as a raw DB/validation error (bug #243/#245). Catch it
                // here with a friendly message instead.
                if (dtp_valid_until.Value.Date < DateTime.Today)
                {
                    MessageBox.Show("Valid Until date cannot be in the past. Please choose a later date.",
                        "Invalid Date", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    dtp_valid_until.Focus();
                    return;
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

                        // Grab both now, before anything below resets/reloads them -
                        // Insert() always creates a fresh row on an edit (isSubVersion),
                        // orphaning the old quick_id/header id (snapshot is a no-op when
                        // this isn't an edit - see SnapshotReservedReferenceCodesAsync).
                        // documentNo is what MigrateSnapshottedReservationsAsync uses to
                        // find this same save's new ids afterward - captured here because
                        // Helpers.ResetControls(pnl_header) below blanks txt_document_no.
                        var reservationSnapshot = await SnapshotReservedReferenceCodesAsync(dgv_quick_quote_details);
                        string savedDocumentNo = txt_document_no.Text;

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
                            await RunWithLoadingAsync(async () => await fetchQuotationDetails(), "Loading...");

                            // Any RESERVE/release toggled in StockCheckModal before this
                            // save is still just pending intent (see
                            // _pendingReservationByReferenceCode) - every line now has a
                            // real id after that reload, so apply it for real.
                            var appliedReferenceCodes = await ApplyPendingReservationsAsync(savedDocumentNo);

                            // Carry over whatever was already reserved before this edit onto
                            // the new version's ids (see SnapshotReservedReferenceCodesAsync).
                            await MigrateSnapshottedReservationsAsync(savedDocumentNo, reservationSnapshot, appliedReferenceCodes);

                            SetNewFormMode(false);
                        }
                        else
                            MessageBox.Show(isSuccess.message);
                    }
                }
            }
            catch (Exception ex)
            {
                // Previously showed the raw exception (including DB error text) directly to
                // general users (bug #243/#245). Keep the technical detail in the debug output
                // for support/devs, but show a plain, user-friendly message on screen.
                System.Diagnostics.Debug.WriteLine("IsQuickQuote save error: " + ex);
                MessageBox.Show(
                    "We couldn't save this quotation. Please check that all required fields are filled in correctly and try again. If the problem continues, contact support.",
                    "Unable to Save", MessageBoxButtons.OK, MessageBoxIcon.Error);
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

        // Hard guard on top of reference_code.ReadOnly (Designer.cs) - cancels editing at
        // the moment it's about to start, for this column specifically, regardless of
        // whatever let typing through despite ReadOnly being set. CODE is an auto-generated
        // hierarchy/tracking id (drives reservation matching and parent/child computation),
        // never meant to be hand-edited.
        private void dgv_quick_quote_details_CellBeginEdit(object sender, DataGridViewCellCancelEventArgs e)
        {
            if (e.ColumnIndex >= 0 && dgv_quick_quote_details.Columns[e.ColumnIndex].Name == "reference_code")
            {
                e.Cancel = true;
            }
        }

        // Deleting a row (select via row header, press Delete - AllowUserToDeleteRows is
        // on, default WinForms behavior) used to just remove that single bound row,
        // leaving gaps in reference_code (delete "2" out of 1/2/3 -> left with 1/3
        // instead of 1/2) and orphaned children behind if the deleted row was a parent
        // (its "2.1"/"2.2" sub-items stuck around with no visible parent). Handle the
        // deletion ourselves: cascade-delete the row's whole subtree (same helper the
        // "re-select model" flow already uses), then renumber everything so the codes
        // stay gapless and hierarchical.
        private void dgv_quick_quote_details_UserDeletingRow(object sender, DataGridViewRowCancelEventArgs e)
        {
            e.Cancel = true;

            if (e.Row.IsNewRow) return;

            string referenceCode = e.Row.Cells["reference_code"].Value?.ToString();
            if (string.IsNullOrWhiteSpace(referenceCode)) return;

            DeleteRowsByReferenceCode(e.Row.Index, dgv_quick_quote_details);
            RenumberReferenceCodes(dgv_quick_quote_details);
        }

        // Walks the grid's rows in their current display order and rebuilds every
        // reference_code from scratch so numbering stays gapless after a delete -
        // top-level items are renumbered 1, 2, 3... in order, and every descendant keeps
        // its original sub-level suffix but adopts its (possibly renumbered) parent's new
        // top-level number, so e.g. "3.3.1" becomes "2.3.1" if the row that used to be
        // "3" is now "2".
        private void RenumberReferenceCodes(DataGridView dgv)
        {
            // Same DataView-vs-DataTable unwrap as DeleteRowsByReferenceCode - the grid is
            // bound to a DataView, not a DataTable, whenever an existing quotation is
            // loaded, which "dgv.DataSource is DataTable" doesn't match.
            DataTable dataSource = dgv.DataSource as DataTable ?? (dgv.DataSource as DataView)?.Table;
            if (dataSource == null || !dataSource.Columns.Contains("reference_code"))
                return;

            bool wasUpdating = isUpdatingHierarchy;
            isUpdatingHierarchy = true;

            int topLevelCounter = 0;
            string currentNewTopPrefix = null;

            foreach (DataRow row in dataSource.Rows)
            {
                string oldCode = row["reference_code"]?.ToString();
                if (string.IsNullOrWhiteSpace(oldCode))
                    continue;

                string[] segments = oldCode.Split('.');

                if (segments.Length == 1)
                {
                    topLevelCounter++;
                    currentNewTopPrefix = topLevelCounter.ToString();
                    row["reference_code"] = currentNewTopPrefix;
                }
                else
                {
                    string newTopPrefix = currentNewTopPrefix ?? segments[0];
                    string suffix = string.Join(".", segments.Skip(1));
                    row["reference_code"] = $"{newTopPrefix}.{suffix}";
                }
            }

            isUpdatingHierarchy = wasUpdating;

            ComputeByReferenceHierarchy();
            ComputeReferenceNonHierarchy();
            ComputeFooterTotals();
        }

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

                // INV. Column - stock checker. Always opens on click, flagged or not (the
                // icon just tells you whether there's a shortage - it doesn't gate
                // whether you can check/reserve). Gated on !IsView, same as the other
                // line-editing columns above - checking/reserving stock is an Add/Edit
                // action, not something available while just viewing a saved quotation.
                if (dgv_quick_quote_details.Columns[e.ColumnIndex].Name == "quick_inv_stock" && !IsView)
                {
                    HandleStockCheckClick(e.RowIndex, dgv_quick_quote_details);
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
                        // Was a bare MessageBox that let the loop keep going with this
                        // segment silently skipped - the returned multiplier looked
                        // valid but was computed from only part of the input. Now
                        // returns the neutral multiplier (1 = no discount) so a
                        // malformed entry can't silently produce a partial discount.
                        MessageBox.Show("Invalid discount format. Division should be at the start of the part. No discount was applied.");
                        return 1m;
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
            string Id = dgv.Rows[RowIndex].Cells["item_id"].Value?.ToString();

            // No component picked yet for this row - item_id is blank, and passing that
            // straight into ModelModal's constructor is what threw the raw "Input string
            // was not in a correct format" FormatException. Catch it here instead with a
            // message that actually tells the user what to do.
            if (string.IsNullOrWhiteSpace(Id) || Id == "0")
            {
                MessageBox.Show(
                    "It doesn't have a component, that's why it can't select any models. Please select a component first.",
                    "No Component Selected", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

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
            // Was counterReference++ - a running counter that only ever goes up, so it
            // drifted away from what's actually on the grid the moment anything got
            // deleted/renumbered (e.g. grid shows 1, 2 but counterReference was already at
            // 4 from earlier adds, so the next item became "5" instead of "3"). Recompute
            // from the grid's real current max every time instead, so it's always correct
            // regardless of delete/renumber history.
            counterReference = GetMaxTopLevelReferenceCode(dgv) + 1;
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

        // Cache of the last-fetched available stock per item, keyed by item_id - avoids
        // re-hitting /inventory/item_stocks/available on every keystroke/repaint. Cleared
        // per item on qty edit (the row's own required qty changing doesn't change the
        // server-side numbers, but re-fetching keeps "reserved" honest against what other
        // quotations are doing concurrently).
        private readonly Dictionary<int, AvailableStockModel> _availableStockByItemId = new Dictionary<int, AvailableStockModel>();

        // Repaints every row's INV. cell after the grid's DataSource is (re)assigned -
        // unbound columns don't survive a DataSource swap, so this is what puts the
        // numbers (and, through them, the flag icon) back after every load/reload (see
        // the DataBindingComplete wire-up in Quotation.Designer.cs).
        //
        // Runs regardless of IsView - the flag itself is still worth seeing while just
        // viewing a saved quotation (it's useful, at-a-glance info either way); it's only
        // the check/reserve interaction that's Add/Edit-only (see dgv_quick_quote_details_
        // CellClick / CellMouseDown).
        private void dgv_quick_quote_details_DataBindingComplete(object sender, DataGridViewBindingCompleteEventArgs e)
        {
            RefreshAllStockIndicators(dgv_quick_quote_details);
        }

        // Was: fire one RefreshStockIndicator per row, each independently hitting
        // /inventory/item_stocks/available for its own item - fine for one or two rows,
        // but a quotation with many distinct items fired that many concurrent
        // fire-and-forget requests at once, and losing that race (or just a flaky
        // network moment) meant "An error occurred while sending the request" popping up
        // once per row that failed. GetAllAvailableStock() below does the equivalent of
        // every row's lookup in a single request instead, so at most one such error can
        // ever surface here - and since it's silent (see GetAllAvailableStock's remarks),
        // now not even that: a failed prefetch just leaves whichever items didn't cache
        // showing no indicator, the same "convenience, not critical" fallback this screen
        // already used per-row.
        //
        // _refreshingStockIndicators guards against a second class of the same symptom:
        // this is called from many places (row add/edit, tab switches, DataBindingComplete,
        // etc.) as async void/fire-and-forget, with nothing stopping two calls from being
        // in flight at once. Each in-flight call still loops every row and calls
        // RefreshStockIndicator -> GetReservation per row (not batched the way available
        // stock is - see GetReservation's own remarks) - so several overlapping calls
        // during a busy editing session meant several times as many concurrent reservation
        // lookups, which is exactly what was flooding /inventory/item_stocks/reservations
        // and popping "An error occurred while sending the request" over and over. Skipping
        // a redundant concurrent pass is safe here since this only ever reflects current
        // grid state - the next call (there's always another one coming) picks up whatever
        // this one would have.
        private bool _refreshingStockIndicators = false;

        private async void RefreshAllStockIndicators(DataGridView dgv)
        {
            if (_refreshingStockIndicators) return;
            _refreshingStockIndicators = true;

            try
            {
                var all = await ItemStockCheckService.GetAllAvailableStock();
                foreach (var stock in all)
                {
                    _availableStockByItemId[stock.item_id] = stock;
                }

                for (int i = 0; i < dgv.Rows.Count; i++)
                {
                    if (dgv.Rows[i].IsNewRow) continue;
                    // Sequential, not fire-and-forget - see _refreshingStockIndicators'
                    // remarks above for why this needs to actually finish, not just start,
                    // before the guard releases.
                    await RefreshStockIndicator(i, dgv);
                }
            }
            finally
            {
                _refreshingStockIndicators = false;
            }
        }

        // Fetches (or reuses the cached) available stock for this row's item and writes
        // it into the quick_inv_stock cell - dgv_quick_quote_details_CellFormatting then
        // replaces that number with just the flag icon before it's ever seen (see INV.'s
        // remarks in Quotation.Designer.cs), but the real number still has to land here
        // first since that's what the formatting check compares against quick_qty. async
        // void, fire-and-forget, matching this file's existing convention for UI-triggered
        // API calls (see UpdateProjectConditions etc.) - a slow/failed lookup just leaves
        // the cell blank rather than blocking the grid.
        //
        // Writes effective available (this row's own reservation, if any, added back) -
        // raw available already nets out every active reservation including this row's
        // own, so a line already sitting on exactly the stock it reserved would otherwise
        // red-flag itself for a "shortage" that's really just its own already-secured
        // stock. A different quotation's line for the same item has no reservation of its
        // own to add back, so it still correctly sees the real, lower number.
        // Task, not void - RefreshAllStockIndicators awaits this per row (sequentially,
        // not Task.WhenAll) so the whole pass finishes, and _refreshingStockIndicators
        // stays true, for as long as the actual per-row reservation lookups are still in
        // flight - see RefreshAllStockIndicators' own remarks. Existing fire-and-forget
        // call sites (e.g. after adding a single new row) still compile unchanged; a
        // discarded Task behaves the same as the old async void did for them.
        private async Task RefreshStockIndicator(int rowIndex, DataGridView dgv)
        {
            if (rowIndex < 0 || rowIndex >= dgv.Rows.Count || dgv.Rows[rowIndex].IsNewRow) return;
            if (!dgv.Columns.Contains("quick_inv_stock") || !dgv.Columns.Contains("item_id")) return;

            var itemIdValue = dgv.Rows[rowIndex].Cells["item_id"].Value;
            if (!int.TryParse(itemIdValue?.ToString(), out int itemId) || itemId <= 0) return;

            int quickId = 0;
            if (dgv.Columns.Contains("quick_id"))
            {
                int.TryParse(dgv.Rows[rowIndex].Cells["quick_id"].Value?.ToString(), out quickId);
            }

            try
            {
                if (!_availableStockByItemId.TryGetValue(itemId, out AvailableStockModel stock))
                {
                    stock = await ItemStockCheckService.GetAvailableStock(itemId);
                    _availableStockByItemId[itemId] = stock;
                }

                int ownReservedQty = 0;
                if (quickId > 0)
                {
                    // silent: true - see ItemStockCheckService.GetReservation's remarks;
                    // this background refresh already means to swallow a failure here, not
                    // pop a MessageBox per row.
                    var reservation = await ItemStockCheckService.GetReservation(quickId, silent: true);
                    if (reservation != null) ownReservedQty = reservation.qty;
                }

                if (rowIndex < dgv.Rows.Count && !dgv.Rows[rowIndex].IsNewRow)
                {
                    dgv.Rows[rowIndex].Cells["quick_inv_stock"].Value = stock.available + ownReservedQty;
                    dgv.InvalidateRow(rowIndex);
                }
            }
            catch (Exception)
            {
                // Stock lookup is a convenience indicator, not part of the save path -
                // swallow rather than pop a MessageBox for every row on a flaky network.
            }
        }

        // INV. is icon-only, and only shows anything at all when this row is actually
        // short - a good/covered row is just blank, not a black flag, so the flag itself
        // reads as "something needs attention here" rather than a status indicator every
        // row has. The actual available number still lives in this same cell underneath
        // (written by RefreshStockIndicator) purely so this method has something to
        // compare quick_qty against; it just never reaches the screen. Left as a real
        // DataGridViewTextBoxColumn (not a custom-painted one) since CellFormatting is the
        // pattern this grid already leans on elsewhere for computed display values.
        private void dgv_quick_quote_details_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= dgv_quick_quote_details.Rows.Count) return;
            if (dgv_quick_quote_details.Columns[e.ColumnIndex].Name != "quick_inv_stock") return;
            if (dgv_quick_quote_details.Rows[e.RowIndex].IsNewRow) return;

            if (!int.TryParse(e.Value?.ToString(), out int available)) return;

            int required = 0;
            if (dgv_quick_quote_details.Columns.Contains("quick_qty"))
            {
                int.TryParse(dgv_quick_quote_details.Rows[e.RowIndex].Cells["quick_qty"].Value?.ToString(), out required);
            }

            bool isShort = required > 0 && available < required;
            e.Value = isShort ? "\U0001F6A9" : "";
            e.CellStyle.ForeColor = Color.Red;
            e.CellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            e.FormattingApplied = true;
        }

        // Right-clicking the QTY column's header opens the stock checker - unlike a
        // per-row click, this isn't gated on any row being flagged, since RESERVE needs
        // to work even when nothing's currently short (see HandleStockCheckClick). Wired
        // to dgv_quick_quote_details.CellMouseDown in Quotation.Designer.cs; header cells
        // report RowIndex == -1.
        private void dgv_quick_quote_details_CellMouseDown(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.RowIndex != -1 || e.Button != MouseButtons.Right) return;
            if (dgv_quick_quote_details.Columns[e.ColumnIndex].Name != "quick_qty") return;
            // Same Add/Edit-only gate as the INV. column click - see dgv_quick_quote_details_CellClick.
            if (IsView) return;

            HandleStockCheckClick(-1, dgv_quick_quote_details);
        }

        // Opens the stock checker for the whole quotation - every line item that has
        // both an item and a nonzero QTY, regardless of which row (if any) triggered it
        // (matching the "PROJECTED INVENTORY" mockup, which shows every item at once, not
        // one item per screen). Items with required qty 0 are skipped per the same
        // mockup's notes.
        //
        // RESERVE is a plain manual toggle in the modal now - nothing reserves a line on
        // its own anymore (see quick_quotation_service.go), so each line's actual
        // reservation status is looked up fresh here rather than assumed.
        private async void HandleStockCheckClick(int rowIndex, DataGridView dgv)
        {
            // rowIndex isn't used to filter anything - this always opens the same
            // full-quotation list regardless of what triggered it (right-clicking QTY's
            // header passes -1, since that's not a real row). Kept as a parameter in case
            // a future "scroll to/highlight this row" tweak wants it.
            if (!dgv.Columns.Contains("item_id")) return;

            var lines = new List<StockCheckRow>();
            DateTime? expiresAt = dtp_valid_until.Value.Date;

            for (int i = 0; i < dgv.Rows.Count; i++)
            {
                var row = dgv.Rows[i];
                if (row.IsNewRow) continue;

                if (!int.TryParse(row.Cells["item_id"].Value?.ToString(), out int itemId) || itemId <= 0) continue;

                int.TryParse(row.Cells["quick_qty"].Value?.ToString(), out int requiredQty);
                if (requiredQty <= 0) continue;

                // A brand-new, not-yet-saved line has no id yet (CreateSalesQuotationQuick
                // hasn't run for it), so there's nothing to attach a reservation to - but
                // stock/availability is still worth showing. QuickId stays 0 for these;
                // StockCheckModal disables RESERVE for any row with QuickId <= 0 rather
                // than hiding the row entirely.
                int.TryParse(row.Cells["quick_id"].Value?.ToString(), out int quickId);
                int.TryParse(row.Cells["quick_based_id"].Value?.ToString(), out int quotationId);
                string referenceCode = row.Cells["reference_code"].Value?.ToString();

                string itemName = dgv.Columns.Contains("quick_item_name")
                    ? row.Cells["quick_item_name"].Value?.ToString()
                    : null;
                if (string.IsNullOrWhiteSpace(itemName) && dgv.Columns.Contains("quick_item_code"))
                {
                    itemName = row.Cells["quick_item_code"].Value?.ToString();
                }

                AvailableStockModel stock;
                bool isReserved = false;
                int ownReservedQty = 0;
                try
                {
                    if (!_availableStockByItemId.TryGetValue(itemId, out stock))
                    {
                        stock = await ItemStockCheckService.GetAvailableStock(itemId);
                        _availableStockByItemId[itemId] = stock;
                    }

                    if (quickId > 0)
                    {
                        var reservation = await ItemStockCheckService.GetReservation(quickId);
                        isReserved = reservation != null;
                        // Stock.available already nets this line's own reservation out of
                        // the shared pool - StockCheckModal adds it back (EffectiveAvailable)
                        // so this line doesn't flag itself as short of stock it already has.
                        if (reservation != null) ownReservedQty = reservation.qty;
                    }

                    // Nothing actually changes in the backend until the quotation itself
                    // is saved (see ApplyPendingReservationsAsync), so if this line already
                    // has an unsaved pending choice from an earlier OK, that choice - not
                    // the true-but-not-yet-applied backend state - is what the checkbox
                    // should show on reopen. ownReservedQty deliberately stays the real
                    // backend figure either way - the shared available pool genuinely
                    // hasn't moved yet for a pending-but-unapplied reservation.
                    if (!string.IsNullOrEmpty(referenceCode) &&
                        _pendingReservationByReferenceCode.TryGetValue(referenceCode, out bool pendingState))
                    {
                        isReserved = pendingState;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to check stock for \"{itemName}\": {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    continue;
                }

                lines.Add(new StockCheckRow
                {
                    ItemId = itemId,
                    ItemName = itemName,
                    QuickId = quickId,
                    QuotationId = quotationId,
                    RequiredQty = requiredQty,
                    Stock = stock,
                    IsReserved = isReserved,
                    OwnReservedQty = ownReservedQty,
                    ExpiresAt = expiresAt,
                    ReferenceCode = referenceCode
                });
            }

            if (lines.Count == 0)
            {
                MessageBox.Show("No line items with an item and a QTY yet.", "Nothing To Check", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var modal = new StockCheckModal(lines, line => HandleCanvasSelectionClick(-1, line.ItemId.ToString()));
            modal.ShowDialog();

            // Nothing was actually reserved/released just now, for any line - OK only
            // hands back which lines changed. Merge those in (overwriting any earlier
            // pending choice for the same line) so ApplyPendingReservationsAsync can act
            // on all of it, together, right after the quotation itself is actually saved.
            foreach (var change in modal.PendingChangesByReferenceCode)
            {
                _pendingReservationByReferenceCode[change.Key] = change.Value;
            }
        }

        // reference_code -> the state a line should end up in (true = reserved, false =
        // released), for every line whose RESERVE checkbox was toggled in StockCheckModal
        // and confirmed with OK - for ANY line, not just unsaved ones. Nothing actually
        // calls CreateReservation/ReleaseReservation until the quotation itself is saved
        // (see ApplyPendingReservationsAsync below), so a reservation change can never
        // outlive a quotation edit that was never committed.
        private readonly Dictionary<string, bool> _pendingReservationByReferenceCode = new Dictionary<string, bool>();

        // Called right after a successful save+reload (see IsQuickQuote/
        // FinalizeQuickQuotation) - by then, every line (new or pre-existing) has its real
        // quick_id, since fetchQuotationDetails() rebinds dgv_quick_quote_details fresh
        // from the server. Matches back on reference_code, the one thing that survives
        // that rebind unchanged.
        // Resolves the lines this save actually wrote, keyed by reference_code, and hands
        // back the header id they belong to.
        //
        // Deliberately does NOT read ids off dgv_quick_quote_details. fetchQuotationDetails()
        // rebinds the grid to GetOwnedRowIndexes()[0] - "this user's first owned record",
        // not "the record that was just saved" - so after a reload the grid is usually
        // showing a different document than the one being saved. reference_codes are only
        // per-quotation sequence numbers ("1", "1.1", "2"), so they collide across
        // documents constantly, and matching on them against whatever the grid happens to
        // hold lands the write on that other document's quick_id/quick_based_id. That is
        // the "the reservation keeps coming back to Q#0001" bug - Q#0001 is simply the
        // user's oldest owned record, so it is what the grid falls back to every time.
        //
        // `data` (repopulated by that same reload) holds every version of every owned
        // document regardless of which one the grid bound to, so resolve from there
        // instead - keyed by documentNo (stable across an edit; only version_no/
        // sub_version_no change) and ordered by the row's own id, since Insert() always
        // writes a brand new row with a higher id than anything before it. Normalized
        // (NormalizeDocumentNo) so Finalize's "FQ#" still matches back to the same family.
        private Dictionary<string, SalesQuotationQuicksModel> ResolveSavedQuickLines(string documentNo, out int quotationId)
        {
            quotationId = 0;

            if (string.IsNullOrEmpty(documentNo)) return null;
            if (data?.SalesQuotation == null || data.SalesQuotationQuick == null) return null;

            var header = data.SalesQuotation
                .Where(q => NormalizeDocumentNo(q.document_no) == NormalizeDocumentNo(documentNo))
                .OrderByDescending(q => q.id)
                .FirstOrDefault();

            if (header == null) return null;

            quotationId = header.id;

            return data.SalesQuotationQuick
                .Where(q => q.based_id == header.id && !string.IsNullOrEmpty(q.reference_code))
                .GroupBy(q => q.reference_code)
                .ToDictionary(g => g.Key, g => g.First());
        }

        // Returns the reference_codes it actually acted on, so the snapshot migration that
        // runs straight after can skip them - both halves can hold an entry for the same
        // line (unchecking RESERVE on a line that was already reserved queues a release
        // here AND leaves that line in the pre-save snapshot), and without this the
        // migration would faithfully re-create the hold that was just released.
        private async Task<HashSet<string>> ApplyPendingReservationsAsync(string documentNo)
        {
            var applied = new List<string>();
            if (_pendingReservationByReferenceCode.Count == 0) return new HashSet<string>();

            var savedLines = ResolveSavedQuickLines(documentNo, out int quotationId);
            if (savedLines == null)
            {
                // Couldn't identify the document that was just saved. Leaving the intent
                // pending is the safe failure here - falling back to the grid is exactly
                // what put the hold on the wrong document in the first place.
                MessageBox.Show(
                    "The quotation saved, but the reservation changes couldn't be applied - the saved document couldn't be identified. Reopen the quotation and set RESERVE again.",
                    "Reservation Changes Not Applied",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return new HashSet<string>();
            }

            var failures = new List<string>();
            var affectedItemIds = new HashSet<int>();

            foreach (var pending in _pendingReservationByReferenceCode.ToList())
            {
                string referenceCode = pending.Key;
                bool shouldReserve = pending.Value;

                // Not a line of the document that was just saved (a leftover intent from
                // another doc still open in this session) - leave it pending.
                if (!savedLines.TryGetValue(referenceCode, out var line)) continue;

                // Saved without an id somehow (e.g. that specific line failed) - leave it
                // pending rather than silently dropping the intent.
                if (line.id <= 0) continue;

                try
                {
                    if (shouldReserve)
                    {
                        if (line.item_id <= 0 || line.qty <= 0)
                        {
                            // Item/qty got cleared before saving - nothing sensible left
                            // to reserve, so just drop the stale intent instead of erroring.
                            applied.Add(referenceCode);
                            continue;
                        }

                        await ItemStockCheckService.CreateReservation(
                            line.item_id, line.qty, line.id, quotationId, dtp_valid_until.Value.Date);
                    }
                    else
                    {
                        await ItemStockCheckService.ReleaseReservation(line.id);
                    }

                    if (line.item_id > 0) affectedItemIds.Add(line.item_id);
                    applied.Add(referenceCode);
                }
                catch (Exception ex)
                {
                    failures.Add($"{referenceCode}: {ex.Message}");
                }
            }

            foreach (var referenceCode in applied)
            {
                _pendingReservationByReferenceCode.Remove(referenceCode);
            }

            if (affectedItemIds.Count > 0)
            {
                foreach (var itemId in affectedItemIds)
                {
                    _availableStockByItemId.Remove(itemId);
                }
                RefreshAllStockIndicators(dgv_quick_quote_details);
            }

            if (failures.Count > 0)
            {
                MessageBox.Show(
                    "The quotation saved, but some pending reservation changes couldn't be applied:\n" + string.Join("\n", failures),
                    "Some Reservation Changes Failed",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }

            return new HashSet<string>(applied);
        }

        // Reservations are tied to a specific line's quick_id (see CreateReservation's
        // sourceId param) and that line's quick_based_id as quotation_id - but editing a
        // saved quotation always Insert()s a brand new SalesQuotationQuick row on save
        // (Update() is dead code - see Sales_Quotation_Bug_Report_2026-08-03.md #6), so
        // every line gets a new quick_id and the header gets a new id. Left alone, a
        // reservation placed on Q#0001 stays pointed at Q#0001's now-orphaned ids forever:
        // it silently stops showing as reserved on the doc the user is actually looking
        // at (the new Q#0002), even though it's still holding stock server-side.
        //
        // Same story applies to Finalize - it also always inserts fresh rows (see the
        // finalize save handler's own comment above its ApplyPendingReservationsAsync
        // call), whether or not the doc was edited first, so this isn't gated on
        // isSubVersion. Call this BEFORE the save/reload (so quick_id on screen still
        // points at the OLD, still-valid row) to snapshot which reference_codes currently
        // have an active reservation. A brand-new, never-saved quotation naturally has no
        // quick_id yet on any row, so this is a cheap no-op for that case.
        private async Task<Dictionary<string, StockReservationModel>> SnapshotReservedReferenceCodesAsync(DataGridView dgv)
        {
            var snapshot = new Dictionary<string, StockReservationModel>();
            if (!dgv.Columns.Contains("reference_code") || !dgv.Columns.Contains("quick_id")) return snapshot;

            foreach (DataGridViewRow row in dgv.Rows)
            {
                if (row.IsNewRow) continue;

                string referenceCode = row.Cells["reference_code"].Value?.ToString();
                if (string.IsNullOrEmpty(referenceCode)) continue;

                int.TryParse(row.Cells["quick_id"].Value?.ToString(), out int oldQuickId);
                if (oldQuickId <= 0) continue;

                var reservation = await ItemStockCheckService.GetReservation(oldQuickId);
                if (reservation != null)
                {
                    snapshot[referenceCode] = reservation;
                }
            }

            return snapshot;
        }

        // Second half of the pair above - call AFTER fetchQuotationDetails() has refreshed
        // the class-level `data` field with this save's real, post-save ids.
        //
        // Deliberately does NOT read the "new" ids off dgv_quick_quote_details (only the
        // PRE-save snapshot above does that): fetchQuotationDetails() picks which record to
        // land the grid on via GetOwnedRowIndexes()[0] - "this user's first owned record in
        // the list", not "the record that was just saved" - so after a reload the grid can
        // easily be showing a *different* document than the one this save just created a
        // new version of, and reading quick_id/quick_based_id off it would silently
        // "migrate" the reservation onto the wrong document's ids (this is exactly what
        // happened the first time this shipped - the reservation just kept re-landing on
        // Q#0001 because that's whatever fetchQuotationDetails() happened to bind to, not
        // because the migration didn't run).
        //
        // `data` (repopulated by that same reload) holds every version of every owned
        // document regardless of which one ended up bound to the grid, so resolve the new
        // header/line ids from there instead - keyed by documentNo (stable across an edit;
        // only version_no/sub_version_no actually change - see DocumentIncrementer only
        // ever being called from New/New Version/Duplicate, never Edit) plus reference_code
        // (stable across a re-save, same key ApplyPendingReservationsAsync matches on).
        //
        // alreadyApplied is whatever ApplyPendingReservationsAsync just acted on. Both
        // halves can hold an entry for the same line - unchecking RESERVE on a line that
        // was already reserved queues a release there and still leaves that line in this
        // pre-save snapshot - and the release has to win, so those codes are skipped here.
        private async Task MigrateSnapshottedReservationsAsync(string documentNo, Dictionary<string, StockReservationModel> snapshot, HashSet<string> alreadyApplied = null)
        {
            if (snapshot.Count == 0) return;
            if (string.IsNullOrEmpty(documentNo)) return;
            if (data?.SalesQuotation == null || data.SalesQuotationQuick == null) return;

            // Resolved off `data` rather than the grid, and ordered by the row's own id
            // rather than version_no/sub_version_no - see ResolveSavedQuickLines for why
            // both of those matter. In short: sub_version_no is effectively always "0"
            // (GetNextSubVersionNo compares a stripped "0001" against a still-prefixed
            // "Q#0001", so its lookup never matches), which makes every same-document row
            // tie on version ordering and resolve to whichever the API listed first - the
            // oldest one. Insert() always writes a higher id, so id ordering is immune to
            // that.
            var newLinesByReferenceCode = ResolveSavedQuickLines(documentNo, out int newQuotationId);
            if (newLinesByReferenceCode == null) return;

            var failures = new List<string>();

            foreach (var entry in snapshot)
            {
                string referenceCode = entry.Key;
                var oldReservation = entry.Value;

                // ApplyPendingReservationsAsync already had its say on this line - don't
                // undo it by re-creating the hold it just released.
                if (alreadyApplied != null && alreadyApplied.Contains(referenceCode)) continue;

                if (!newLinesByReferenceCode.TryGetValue(referenceCode, out var newLine) || newLine.qty <= 0)
                {
                    // The line was deleted, or its QTY zeroed, in this version. The old
                    // hold still exists server-side under an id nothing on screen points
                    // at any more, so it would go on holding stock until the expiry sweep
                    // eventually caught it - release it now instead of orphaning it.
                    try
                    {
                        await ItemStockCheckService.ReleaseReservation(oldReservation.source_id);
                    }
                    catch (Exception ex)
                    {
                        failures.Add($"{referenceCode}: {ex.Message}");
                    }
                    continue;
                }

                int newQuickId = newLine.id;

                // QTY is part of what has to carry over, not just the ids. The hold was
                // placed for whatever the line said at the time, so an edit that cuts QTY
                // from 5 to 3 has to move the hold down to 3 - re-issuing with
                // oldReservation.qty would leave all 5 held, and a save that happens not to
                // change the ids would skip the update altogether. Compare QTY alongside
                // the ids and re-issue whenever any of the three moved.
                if (newQuickId == oldReservation.source_id &&
                    newQuotationId == oldReservation.quotation_id &&
                    newLine.qty == oldReservation.qty)
                    continue;

                try
                {
                    // Release the old hold first, then re-create it on the new line/doc -
                    // the old id is no longer a row on screen (fetchQuotationDetails()
                    // replaced it), but the reservation itself still exists server-side
                    // under that id until explicitly released.
                    await ItemStockCheckService.ReleaseReservation(oldReservation.source_id);
                    await ItemStockCheckService.CreateReservation(
                        oldReservation.item_id, newLine.qty, newQuickId, newQuotationId,
                        oldReservation.expires_at);
                }
                catch (Exception ex)
                {
                    failures.Add($"{referenceCode}: {ex.Message}");
                }
            }

            if (failures.Count > 0)
            {
                MessageBox.Show(
                    "The quotation saved, but the following existing reservations couldn't be carried over to the new version:\n" + string.Join("\n", failures),
                    "Some Reservations Couldn't Be Migrated",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
        }

        // Project Quotation's counterpart to the CellClicked/CellClickedModel wiring -
        // ItemSetUC raises CellClickedStock (INV. column click or right-click on the QTY
        // header, both already gated by _isEditable there) and this runs the actual
        // stock-check logic against that tab's own grid, reusing the same StockCheckModal/
        // _pendingReservationByReferenceCode/HandleCanvasSelectionClick machinery Quick
        // Quote already uses.
        private void ItemSetUC_CellClickedStock(object sender, EventArgs e)
        {
            if (sender is ItemSetUC uc)
            {
                HandleProjectStockCheckClick(uc);
            }
        }

        // Same shape as HandleStockCheckClick above, adapted to dgv_project_items' own
        // column names (project_items_qty instead of quick_qty, project_items_id/"items_id"
        // instead of quick_id, and no per-row based_id column - the project's own id and
        // Valid Until apply to every tab alike, so they're read directly from this form's
        // own controls instead). Reservations for project lines are tagged with source_type
        // "sales_project_item" (see ItemStockCheckService.GetReservation/CreateReservation)
        // so their line ids - a completely separate id space from SalesQuotationQuick's -
        // never collide with Quick Quote's reservations for the same item.
        private async void HandleProjectStockCheckClick(ItemSetUC uc)
        {
            var dgv = uc.DgvProjectItems;
            if (dgv == null || !dgv.Columns.Contains("item_id")) return;

            var lines = new List<StockCheckRow>();
            DateTime? expiresAt = dtp_valid_until.Value.Date;
            int quotationId = ToInt(txt_id.Text);

            for (int i = 0; i < dgv.Rows.Count; i++)
            {
                var row = dgv.Rows[i];
                if (row.IsNewRow) continue;

                if (!int.TryParse(row.Cells["item_id"].Value?.ToString(), out int itemId) || itemId <= 0) continue;

                int.TryParse(row.Cells["project_items_qty"].Value?.ToString(), out int requiredQty);
                if (requiredQty <= 0) continue;

                // A brand-new, not-yet-saved line has no id yet, same as Quick Quote's
                // quickId == 0 case - lineId stays 0 and StockCheckModal disables RESERVE
                // for it rather than hiding the row entirely.
                int.TryParse(row.Cells["project_items_id"].Value?.ToString(), out int lineId);
                string referenceCode = row.Cells["reference_code"].Value?.ToString();

                string itemName = dgv.Columns.Contains("project_items_components")
                    ? row.Cells["project_items_components"].Value?.ToString()
                    : null;

                AvailableStockModel stock;
                bool isReserved = false;
                int ownReservedQty = 0;
                try
                {
                    if (!_availableStockByItemId.TryGetValue(itemId, out stock))
                    {
                        stock = await ItemStockCheckService.GetAvailableStock(itemId);
                        _availableStockByItemId[itemId] = stock;
                    }

                    if (lineId > 0)
                    {
                        var reservation = await ItemStockCheckService.GetReservation(lineId, "sales_project_item");
                        isReserved = reservation != null;
                        if (reservation != null) ownReservedQty = reservation.qty;
                    }

                    if (!string.IsNullOrEmpty(referenceCode) &&
                        _pendingReservationByReferenceCode.TryGetValue(referenceCode, out bool pendingState))
                    {
                        isReserved = pendingState;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to check stock for \"{itemName}\": {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    continue;
                }

                lines.Add(new StockCheckRow
                {
                    ItemId = itemId,
                    ItemName = itemName,
                    QuickId = lineId,
                    QuotationId = quotationId,
                    RequiredQty = requiredQty,
                    Stock = stock,
                    IsReserved = isReserved,
                    OwnReservedQty = ownReservedQty,
                    ExpiresAt = expiresAt,
                    ReferenceCode = referenceCode
                });
            }

            if (lines.Count == 0)
            {
                MessageBox.Show("No line items with an item and a QTY yet.", "Nothing To Check", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var modal = new StockCheckModal(lines, line => HandleCanvasSelectionClick(-1, line.ItemId.ToString()));
            modal.ShowDialog();

            foreach (var change in modal.PendingChangesByReferenceCode)
            {
                _pendingReservationByReferenceCode[change.Key] = change.Value;
            }
        }

        // Extends ApplyPendingReservationsAsync (below it, right after this) to also cover
        // every Project Quotation tab currently in tabControl2 - called from the exact same
        // places, right after a Project save/finalize reload gives every tab's rows their
        // real project_items_id. Safe to always run regardless of isProject: a Quick Quote
        // pending reference_code will simply never match any row in a project tab's grid
        // (and vice versa), so calling both costs nothing extra in the common case.
        private async Task ApplyPendingProjectReservationsAsync()
        {
            if (_pendingReservationByReferenceCode.Count == 0) return;

            int quotationId = ToInt(txt_id.Text);
            var applied = new List<string>();
            var failures = new List<string>();
            var affectedItemIds = new HashSet<int>();
            var affectedTabs = new List<ItemSetUC>();

            foreach (TabPage tab in tabControl2.TabPages)
            {
                if (!(tab.Controls.Count > 0 && tab.Controls[0] is ItemSetUC uc)) continue;

                var dgv = uc.DgvProjectItems;
                if (dgv == null || !dgv.Columns.Contains("reference_code")) continue;

                foreach (DataGridViewRow row in dgv.Rows)
                {
                    if (row.IsNewRow) continue;

                    string referenceCode = row.Cells["reference_code"].Value?.ToString();
                    if (string.IsNullOrEmpty(referenceCode) ||
                        !_pendingReservationByReferenceCode.TryGetValue(referenceCode, out bool shouldReserve))
                        continue;

                    int.TryParse(row.Cells["project_items_id"].Value?.ToString(), out int lineId);
                    if (lineId <= 0) continue; // still not saved somehow - leave it pending

                    int.TryParse(row.Cells["item_id"].Value?.ToString(), out int itemId);

                    try
                    {
                        if (shouldReserve)
                        {
                            int.TryParse(row.Cells["project_items_qty"].Value?.ToString(), out int qty);
                            if (itemId <= 0 || qty <= 0) { applied.Add(referenceCode); continue; }
                            await ItemStockCheckService.CreateReservation(itemId, qty, lineId, quotationId, dtp_valid_until.Value.Date, "sales_project_item");
                        }
                        else
                        {
                            await ItemStockCheckService.ReleaseReservation(lineId, "sales_project_item");
                        }

                        if (itemId > 0) affectedItemIds.Add(itemId);
                        if (!affectedTabs.Contains(uc)) affectedTabs.Add(uc);
                        applied.Add(referenceCode);
                    }
                    catch (Exception ex)
                    {
                        failures.Add($"{referenceCode}: {ex.Message}");
                    }
                }
            }

            foreach (var referenceCode in applied)
            {
                _pendingReservationByReferenceCode.Remove(referenceCode);
            }

            if (affectedItemIds.Count > 0)
            {
                foreach (var itemId in affectedItemIds)
                {
                    _availableStockByItemId.Remove(itemId);
                    foreach (TabPage tab in tabControl2.TabPages)
                    {
                        if (tab.Controls.Count > 0 && tab.Controls[0] is ItemSetUC anyUc)
                        {
                            anyUc.ClearStockCache(itemId);
                        }
                    }
                }

                foreach (var uc in affectedTabs)
                {
                    uc.RefreshAllStockIndicators();
                }
            }

            if (failures.Count > 0)
            {
                MessageBox.Show(
                    "The project quotation saved, but some pending reservation changes couldn't be applied:\n" + string.Join("\n", failures),
                    "Some Reservation Changes Failed",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
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

            // Same DataView-vs-DataTable unwrap as DeleteRowsByReferenceCode/
            // RenumberReferenceCodes - viewing/editing an existing quotation binds this
            // grid to a DataView, not a DataTable, which "dgv.DataSource is DataTable"
            // doesn't match. That silently made this always return 0 on an existing
            // quotation, so newly added items reused/collided with codes already in use
            // instead of continuing from the real max.
            DataTable dataSource = dgv?.DataSource as DataTable ?? (dgv?.DataSource as DataView)?.Table;
            if (dataSource == null || !dataSource.Columns.Contains("reference_code"))
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

                    // Show available stock for the item just picked before the user has
                    // even typed a QTY yet. Uses addedRowIndex, not rowIndex - see the
                    // styling call just above, which is why that's the grid position the
                    // new row actually lands on.
                    RefreshStockIndicator(addedRowIndex, dgv);
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
        // computationLoop() deleted: it was an abandoned parallel implementation of
        // ComputeFooterTotals with a *different* formula (summed all rows instead of
        // top-level only, ignored the additional discount) and was only reachable
        // through other dead code. Keeping it risked someone rewiring it and
        // silently changing footer totals. ComputeFooterTotals is the live path.
        private void dgv_quick_quote_details_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            // Re-check stock the moment QTY changes, so the red flag reflects what was
            // just typed rather than whatever it was when the item was first picked. The
            // flag icon lives on INV. (see dgv_quick_quote_details_CellFormatting), a
            // different cell than the one just edited, so it needs an explicit repaint -
            // WinForms only auto-refreshes the cell that was actually being edited.
            if (e.RowIndex >= 0 && dgv_quick_quote_details.Columns[e.ColumnIndex].Name == "quick_qty" &&
                dgv_quick_quote_details.Columns.Contains("quick_inv_stock"))
            {
                dgv_quick_quote_details.InvalidateCell(dgv_quick_quote_details.Columns["quick_inv_stock"].Index, e.RowIndex);
            }

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

        // Every data-bearing panel/grid on the form - both Quick Quote's (header/footer/
        // dgv_quick_quote_details) and Project Quotation's (header/footer/project name panel/
        // dgv_project_multiplier/Project_Items_Tab, which hosts tabControl2 and each tab's
        // ItemSetUC with its own dgv_project_items, dgv_wiring and dgv_final grids) - so a
        // single overlay call covers "every part" regardless of which of the two a given load
        // turns out to be.
        // NOTE: overlay tabControl2's *parent* TabPage (Project_Items_Tab), not tabControl2
        // itself - a TabControl's Controls collection only accepts TabPage children, so adding
        // the overlay UserControl directly to it throws
        // "Cannot add 'UserControl' to TabControl. Only TabPages can be directly added to
        // TabControls." Project_Items_Tab is a plain TabPage (Panel-like) that already contains
        // tabControl2, so overlaying it covers the same area safely and also survives
        // fetchSalesProject() clearing/rebuilding tabControl2.TabPages mid-load.
        private Control[] GetLoadingOverlayTargets()
        {
            return new Control[] { pnl_header, pnl_footer, pnl_project_name, dgv_quick_quote_details, dgv_project_multiplier, Project_Items_Tab };
        }

        // Shows a loading overlay across every panel/grid above and disables every button on
        // the form while `action` runs, then always restores both - even if `action` throws -
        // so a slow server response can't be raced by a click before the fields/grids have
        // actually finished loading.
        private async Task RunWithLoadingAsync(Func<Task> action, string message = "Loading, please wait...")
        {
            Control[] targets = GetLoadingOverlayTargets();
            Helpers.Loading.ShowLoading(targets, message);
            Helpers.SetButtonsEnabled(this, false);
            try
            {
                await action();
            }
            finally
            {
                Helpers.Loading.HideLoading(targets);
                Helpers.SetButtonsEnabled(this, true);
                ReapplyFinalizeButtonState();
            }
        }

        // Helpers.SetButtonsEnabled(this, true) above is a blanket re-enable with no
        // awareness of isFinalized or whether a record is even loaded - every load that
        // goes through RunWithLoadingAsync (including landing on a totally blank form
        // with nothing to finalize yet, per the earlier fetchQuotationDetails() fix) was
        // stomping right back over that and leaving Finalize/Sales Order clickable again
        // the instant loading finished. Reapply the real rule immediately after.
        private void ReapplyFinalizeButtonState()
        {
            bool hasRecord = ToInt(txt_id.Text) > 0;
            btn_finalize.Enabled = hasRecord && !isFinalized;
            btn_sales_order.Enabled = hasRecord && isFinalized;
        }

        private async void Quotation_Load(object sender, EventArgs e)
        {
            //int dgvWidth = dgv_quick_quote_details.Width;
            //int dgvHeight = dgv_quick_quote_details.Height;

            Panel[] panels = { pnl_header, pnl_footer };
            Helpers.ReadOnlyControls(panels);

            SetNewFormMode(false);

            IsView = true;

            // Header/footer textboxes/comboboxes and the Quick Quote/Project grids are still
            // empty at this point and only get filled once LoadExistingRecord() finishes
            // fetching from the server (whichever of the two it turns out to be) - cover that
            // gap with a loading overlay and lock every button on the form.
            await RunWithLoadingAsync(async () => await LoadExistingRecord(), "Loading...");

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
                // Was gated on string.IsNullOrEmpty(txt_id.Text) - txt_id is only ever
                // populated by Helpers.BindControls' loose Name-Contains-column-name
                // matching, which isn't guaranteed to have run/matched by this point. sId
                // (parsed straight from HeaderList a few lines up) is the same "is this a
                // real, already-saved record" signal without that dependency, so an already
                // finalized quotation can no longer have FINALIZE re-enabled just because
                // txt_id happened to still be blank when this ran.
                isFinalized = Convert.ToBoolean(HeaderList.Rows[SelectedRow]["is_finalized"]);
                btn_finalize.Enabled = !isFinalized || sId <= 0;
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
            var noOfDays = txt_validity_days.Text;

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
                txt_validity_days.Text = "30";
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
                UC.CellClickedStock += ItemSetUC_CellClickedStock;
                UC.CellEdited += Cell_EditedUC;
                UC.FinalTxtBoxClicked += FinalTxtBoxClicked;
                UC.SizeUpClicked += SizeUpClicked;
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
                // Project mode left txt_validity_days blank instead of reset to 30 like Quick
                // Quote's New already does.
                txt_validity_days.Text = "30";
                return;
            }

            //for new quatations always 30 days
            txt_validity_days.Text = "30";
            counterReference = 0;
            SelectedRowIndex = 0;
        }
        // Trello #044: opens the same pump picker FINAL uses, restricted to pump items
        // only, and appends the choice to this tab's SIZE UP grid instead of replacing a
        // single FINAL selection. Deliberately not de-duplicated against
        // FinalTxtBoxClicked below - they diverge right after the picker closes, and
        // this keeps the working FINAL path untouched.
        private void SizeUpClicked(object sender, EventArgs e)
        {
            // "Is this a pump" = ITEM NAME "PUMP" (spec §17.2, code PMP) - a required
            // field on every item, already present on ItemList (vw_items). Deliberately
            // NOT item_class (spec §4.2.1: "There is no ... PUMP ... class; any code or
            // report filter still keying on one is stale") and NOT the engineering specs
            // table (tbl_setup_item_specs.template), which only exists for an item once
            // someone has actually filled its electrical specs in - keying on that
            // excluded every pump that hadn't been through that separate step yet.
            var sizeUpFilteredItems = ItemList.AsEnumerable()
                                .Where(row => string.Equals(row["item_name"]?.ToString(), "PUMP", StringComparison.OrdinalIgnoreCase))
                                .ToList();

            if (sizeUpFilteredItems.Count == 0)
            {
                MessageBox.Show("No pump items are set up yet. Please add pump items before using this.", "No Pump Data", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            DataTable sizeUpItemListPump = sizeUpFilteredItems.CopyToDataTable();

            if (!(tabControl2.SelectedTab.Controls[0] is ItemSetUC sizeUpControl)) return;

            // Multi-select picker, not ModelModal - see SizeUpPickerModal's class remarks
            // for why this got its own modal instead of reusing/extending ModelModal.
            // Pumps already on this tab's SIZE UP list arrive pre-checked.
            using (var sizeUpModal = new SizeUpPickerModal(sizeUpItemListPump, sizeUpControl.GetSizeUpItemIds()))
            {
                if (sizeUpModal.ShowDialog() == DialogResult.OK)
                {
                    foreach (var pick in sizeUpModal.GetSelectedItems())
                        sizeUpControl.AddSizeUpRow(pick.ItemId.ToString(), pick.Model);
                }
            }
        }

        private async void FinalTxtBoxClicked(object sender, EventArgs e)
        {
            // "Is this a pump" for the purposes of what FINAL's picker OFFERS = ITEM NAME
            // "PUMP" (spec §17.2), same as SizeUpClicked - NOT GetPumpsViewList() (vw_
            // PumpSpecifications). That used to gate entry to this picker entirely, so a
            // real SIZE UP list could never fully appear if some of its pumps lacked
            // electrical specs. GetPumpsViewList() is still used below, per pick, to look
            // up FLA/Voltage where available - it no longer decides what's offered.
            var data = await ProjectService.GetPumpsViewList();
            DataTable pumps = (data?.ItemPumpsView != null) ? JsonHelper.ToDataTable(data.ItemPumpsView) : new DataTable();

            var filteredPumpItems = ItemList.AsEnumerable()
                                .Where(row => string.Equals(row["item_name"]?.ToString(), "PUMP", StringComparison.OrdinalIgnoreCase))
                                .ToList();

            // Trello #043/#049: FINAL must only offer what's actually listed in this
            // tab's SIZE UP (spec §5.1.4: "Final Selection - dropdown limited to what is
            // listed in Size Up"), not every pump item in the system.
            if (tabControl2.SelectedTab.Controls[0] is ItemSetUC currentControlForFilter)
            {
                var sizeUpIds = currentControlForFilter.GetSizeUpItemIds();
                if (sizeUpIds.Count == 0)
                {
                    MessageBox.Show("Add at least one candidate under SIZE UP before selecting FINAL.", "Size Up Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                filteredPumpItems = filteredPumpItems
                                    .Where(row => int.TryParse(row["id"]?.ToString(), out int rowId) && sizeUpIds.Contains(rowId))
                                    .ToList();
            }

            if (filteredPumpItems.Count == 0)
            {
                MessageBox.Show("None of the items in the item list match the pump data. Please check that the pump items still exist in the item catalog.", "No Matching Items", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            DataTable ItemListPump = filteredPumpItems.CopyToDataTable();

            if (!(tabControl2.SelectedTab.Controls[0] is ItemSetUC currentControl2)) return;

            // Multi-select, same as SIZE UP's own picker (Trello #044/#043/#049) - reuses
            // SizeUpPickerModal rather than a second copy of it, since the picking UI
            // (search + checkboxes + BRAND/MODEL NAME/LIST PRICE + Save/Cancel) is
            // identical; only what happens with the picks afterward differs. Pumps
            // already in FINAL arrive pre-checked.
            using (var finalModal = new SizeUpPickerModal(ItemListPump, currentControl2.GetFinalItemIds(), "Select Pumps for Final"))
            {
                if (finalModal.ShowDialog() != DialogResult.OK) return;

                // FINAL's choices MUST match SIZE UP's exactly, regardless of whether
                // FLA/VOLTAGE exist yet - added unconditionally, same as SIZE UP itself
                // never gates on anything beyond "was it picked". A pump missing FLA/
                // VOLTAGE just gets a blank cell here (SetFinalPumpData's own aggregate
                // already treats an unparseable/blank FLA as 0 - see its decimal.TryParse
                // fallback), not left out of FINAL entirely.
                foreach (var pick in finalModal.GetSelectedItems())
                {
                    if (pick.Model.IsNullOrEmpty()) continue;

                    string id = pick.ItemId.ToString();

                    var FLA = pumps.AsEnumerable()
                            .FirstOrDefault(row => row["item_title"].ToString() == "FLA" && row["item_id"].ToString() == id)?["item_value"].ToString();

                    var Voltage = pumps.AsEnumerable()
                                        .FirstOrDefault(row => row["item_title"].ToString() == "VOLTAGE" && row["item_id"].ToString() == id)?["item_value"].ToString();

                    currentControl2.SetFinalPumpData(FLA ?? string.Empty, Voltage ?? string.Empty, pick.Model, id);
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

                    // fetchQuotationDetails() already overlays dgv_quick_quote_details while
                    // it fetches, but pnl_header/pnl_footer stay editable and empty until its
                    // bind() call finishes - RunWithLoadingAsync covers those too (and disables
                    // every button on the form) so a slow server response can't be raced by a
                    // click.
                    await RunWithLoadingAsync(async () => await fetchQuotationDetails(), "Loading...");
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

        // QuickQuoteDGV (column index constants) deleted: its only consumer was the
        // dead compute path removed below.

        // DGVComputation class deleted: only consumer was ComputeDgvHierarchy (also
        // deleted, dead code). The live per-row math is in ComputeReferenceNonHierarchy.

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
        // txt_cash_discount_TextChanged / txt_cash_discount_DoubleClick deleted:
        // neither was wired in the Designer (the live handler is
        // txt_cash_discount_TextChanged_1), and DoubleClick's only body was a call
        // into the deleted computationLoop().
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
                myControl.CellClickedStock += ItemSetUC_CellClickedStock;
                myControl.CellEdited += Cell_EditedUC;
                myControl.FinalTxtBoxClicked += FinalTxtBoxClicked;
                myControl.SizeUpClicked += SizeUpClicked;
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

        private async void btn_finalize_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Are you sure you want to finalize?", "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                // Lock every button for the whole finalize operation - previously the
                // button stayed clickable while the (async) save was still in flight, so
                // clicking Finalize again before it finished could race past the
                // duplicate-document_no check and insert a second finalized copy of the
                // same quotation (one of them ending up double-prefixed, e.g. "FQ#FQ#0003",
                // since the second click read back the first click's not-yet-committed
                // "FQ#..." value as its own starting point).
                Helpers.SetButtonsEnabled(this, false);
                try
                {
                    if (isProject)
                        await FinalizeProjectQuotation();
                    else
                        await FinalizeQuickQuotation();
                }
                finally
                {
                    Helpers.SetButtonsEnabled(this, true);
                    // The blanket re-enable above doesn't know about the isFinalized rule
                    // (see bind()'s btn_finalize.Enabled = !isFinalized || sId <= 0) - reapply
                    // it so a just-finalized (or already-finalized) quotation doesn't have
                    // Finalize re-enabled out from under that.
                    btn_finalize.Enabled = !isFinalized;
                }
            }
        }

        private async Task FinalizeProjectQuotation()
        {
            // No customer selected (txt_customer_id is only ever populated by the
            // "Select Customer" dialog in btn_add_customer_Click) - block finalize
            // instead of letting a quotation with no customer through.
            if (string.IsNullOrWhiteSpace(txt_customer_id.Text))
            {
                MessageBox.Show("Please select a customer before saving.", "Missing Information", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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

            if (!ValidateProjectRequiredFields())
                return;

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
                // Same fix as FinalizeQuickQuotation - strip both prefixes (NormalizeDocumentNo)
                // before re-adding "FQ#", so re-finalizing an already-finalized project doesn't
                // produce a never-before-seen "FQ#FQ#..." string that slips past the duplicate
                // check right below.
                string tempDocNo = "FQ#" + NormalizeDocumentNo(documentNo);
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

                await RunWithLoadingAsync(async () => await fetchSalesProjectData(), "Loading...");

                // Same as IsProject()'s save handling - every tab's rows now have real
                // project_items_id values, so any pending RESERVE/release can actually apply.
                await ApplyPendingProjectReservationsAsync();
            }
            else
                MessageBox.Show($"Insert error: {response.message}");
        }

        private async Task FinalizeQuickQuotation()
        {
            // No customer selected (txt_customer_id is only ever populated by the
            // "Select Customer" dialog in btn_add_customer_Click) - block finalize
            // instead of letting a quotation with no customer through.
            if (string.IsNullOrWhiteSpace(txt_customer_id.Text))
            {
                MessageBox.Show("Please select a customer before saving.", "Missing Information", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

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
                        // Was only stripping a leading "Q#" - re-finalizing a record whose
                        // document_no already had "FQ#" baked in (i.e. clicking Finalize
                        // again on an already-finalized quotation) left that "FQ#" in place
                        // and prepended a second one ("FQ#FQ#0003"), which is a string that
                        // had never existed before - so the duplicate-document_no guard right
                        // below never caught it and happily inserted another "finalized" copy.
                        // NormalizeDocumentNo strips both prefixes, so the bare number always
                        // gets exactly one "FQ#" regardless of what was already stored.
                        string tempDocNo = "FQ#" + NormalizeDocumentNo(documentNo);

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

                        // Grab this now, while dgv_quick_quote_details.quick_id still points
                        // at the pre-finalize row - the DataSource gets swapped for an empty
                        // clone a few lines below on success, and Insert() below always
                        // creates fresh rows either way (see the comment further down).
                        // parentData["document_no"] was overwritten with tempDocNo ("FQ#" +
                        // the bare number) above - that's exactly what got sent as the new
                        // document_no, and MigrateSnapshottedReservationsAsync needs that
                        // (not the pre-finalize "Q#..." string) to find this save's new ids.
                        // tempDocNo itself is out of scope by here (declared inside the
                        // earlier if-block), so read it back off parentData instead.
                        var reservationSnapshot = await SnapshotReservedReferenceCodesAsync(dgv_quick_quote_details);
                        string savedDocumentNo = parentData["document_no"].ToString();

                        var isSuccess = await QuotationService.Insert(parentData);

                        if (isSuccess.Success)
                        {
                            Helpers.ResetControls(pnl_header);
                            ResetControls(pnl_footer);

                            dgv_quick_quote_details.DataSource = this.childList.Clone();
                            toolstrip_quotation.Enabled = true;

                            MessageBox.Show("Quotation Successfully saved");
                            await RunWithLoadingAsync(async () => await fetchQuotationDetails(), "Loading...");

                            // Same as IsQuickQuote() - this finalize path also inserts fresh
                            // SalesQuotationQuick rows (parentData["id"] = 0 above), so any
                            // RESERVE checked before finalizing was queued against a
                            // reference_code, not a real id. Apply it now that the reload
                            // gave these lines real ids.
                            var appliedReferenceCodes = await ApplyPendingReservationsAsync(savedDocumentNo);

                            // Same reasoning as IsQuickQuote() - carry over whatever was
                            // already reserved before finalizing onto FQ#'s new ids.
                            await MigrateSnapshottedReservationsAsync(savedDocumentNo, reservationSnapshot, appliedReferenceCodes);

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
                // Project Quotation has its own Inclusions/Exclusions/Terms rich text boxes
                // (ProjectInclusionsRichTextBox etc., populated from the same quote-terms
                // source as the Quick Quote ones - see bind()/fetchSalesProject around
                // line 6208) - this was passing the Quick Quote boxes' text here regardless
                // of isProject, so a Project Quotation print always showed whatever text
                // happened to be in the Quick Quote panel (often blank, since that panel
                // isn't populated while viewing a Project Quotation).
                SalesPrintModal printPage = new SalesPrintModal(false, true, documentNo, ProjectInclusionsRichTextBox.Text, ProjectExclusionsRichTextBox.Text, ProjectTermAndConditionsRichTextBox.Text);
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

        // version_no/sub_version_no are stored as strings but represent integers -
        // OrderByDescending(q => q.version_no) sorts them lexicographically ("9" > "10"), so
        // a document past its 9th revision could sort an old draft ahead of the true latest.
        // Parse to int for ordering instead; unparsable/blank values sort as 0 (oldest)
        // rather than throwing.
        private static int VersionNoAsInt(string versionNo) =>
            int.TryParse(versionNo, out int parsed) ? parsed : 0;

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

                // Opening a quotation this way (straight from a documentNo, e.g. from
                // Opportunities/Orders) used to call ResetReadOnlyControls unconditionally,
                // making every header/footer textbox instantly editable on load regardless
                // of finalized status - the same bug family as the Finalize-stays-enabled
                // issue. Loading a record should always land in view mode (locked); Edit is
                // what unlocks it, and bind() below still governs button enablement off
                // isFinalized.
                Panel[] panels = { pnl_header, pnl_footer };
                Helpers.ReadOnlyControls(panels);
                dgv_quick_quote_details.ReadOnly = true;

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


                // Same fix as FetchQuotationDetailsByDocumentNo above - loading should
                // always land in locked/view mode, not editable, regardless of isFinalized.
                Panel[] panels = { pnl_header, pnl_footer };
                Helpers.ReadOnlyControls(panels);

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
        // §3.2/§6.3 REQUEST FOR ENGR. (Phase 4 item 4.1) - was a dead stub that opened an
        // old test form (ProjectTest) and did nothing else. Now makes the explicit,
        // per-quote, per-engineer grant: opens RequestForEngrModal to pick the engineer,
        // then POSTs it so this quotation appears on that engineer's Sales Quotation
        // List / the engineering red box (see RequestQuotationForEngr in quotation_service.go
        // and the rewritten vw_get_engineering_redbox_quotation_list.sql).
        private async void btn_request_for_engr_Click(object sender, EventArgs e)
        {
            int sId = ToInt(txt_id.Text);
            if (isNewRecord || sId <= 0)
            {
                MessageBox.Show("Please save this quotation before requesting it for engineering.",
                    "Save Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using (var modal = new RequestForEngrModal())
            {
                if (modal.ShowDialog() != DialogResult.OK)
                    return;

                try
                {
                    var response = await QuotationService.RequestForEngr(sId, modal.SelectedEngrId);
                    if (response.Success)
                    {
                        MessageBox.Show("Quotation sent to engineering.", "Success",
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        MessageBox.Show(response.message, "Error",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("RequestForEngr error: " + ex);
                    MessageBox.Show(
                        "We couldn't send this quotation to engineering. Please try again. If the problem continues, contact support.",
                        "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
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

            // dgv_quick_quote_details_DataBindingComplete is what normally populates the
            // INV. flags, but that only fires on a DataSource swap - this grid is already
            // bound (Edit doesn't reload it), so without this the flags would stay blank
            // (as they should while IsView was still true) until something else happened
            // to rebind the grid.
            RefreshAllStockIndicators(dgv_quick_quote_details);

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
            txt_validity_days.Text = "30";
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
            // Was unconditionally "!isTrue" - fine for a brand-new record, but this method
            // also runs right after fetchQuotationDetails()/bind() reloads an existing
            // record (see the finalize/save success paths below), and bind() had already
            // correctly disabled Finalize for an already-finalized quotation via isFinalized.
            // Blindly re-enabling it here undid that, letting Finalize be clicked again on a
            // quotation that's already finalized.
            btn_finalize.Enabled = !isTrue && !isFinalized;

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

            await RunWithLoadingAsync(async () => await LoadExistingRecord(), "Loading...");

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
        // ComputeQuickQuoteTotal (empty stub) deleted along with the rest of the dead
        // compute path (ComputeDgvHierarchy / computationLoop / DGVComputation) - none
        // were called from any wired event or live code path. The live computation is
        // ComputeByReferenceHierarchy / ComputeReferenceNonHierarchy / ComputeFooterTotals.

        private void DeleteRowsByReferenceCode(int RowIndex, DataGridView dgv)
        {

            string referenceCode = dgv.Rows[RowIndex].Cells["reference_code"].Value.ToString();

            // The grid isn't always bound straight to a DataTable - viewing/editing an
            // existing quotation binds it to a DataView instead (see
            // createFilterViewDgvQuickQouteDetails), which "dgv.DataSource is DataTable"
            // doesn't match. That silently no-op'd this whole method (and the delete-row
            // renumbering that calls it) for every already-saved quotation - only brand
            // new, not-yet-saved ones (bound straight to a DataTable) actually deleted
            // anything. Unwrap the DataView to its underlying Table so both cases work.
            DataTable dataSource = dgv.DataSource as DataTable ?? (dgv.DataSource as DataView)?.Table;

            if (dataSource != null)
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

        // ComputeDgvHierarchy() deleted: dead code (never called from any wired
        // event or live path). Its per-row math lived in the also-deleted
        // DGVComputation and it ended by calling the also-deleted computationLoop().
        // The live equivalents are ComputeByReferenceHierarchy /
        // ComputeReferenceNonHierarchy / ComputeFooterTotals.

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
                // Was never written back here, so the payload built for save/finalize
                // (which reads this cell) sent whatever net_discount happened to be
                // bound from the original load - stale or blank - instead of the
                // value actually matching the qty/price/discount just recalculated.
                row.Cells["quick_net_discount"].Value = netDiscount;
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

            //txt_additional_discount.Text = txt_additional_discount.Text != "" ? txt_additional_discount.Text : "0%";

            // Both parses below used to be unguarded decimal.Parse calls (on the raw
            // textbox text, with no currency-symbol/comma cleanup), throwing a
            // FormatException and crashing this recompute the moment either field
            // held anything decimal.Parse couldn't handle directly (e.g. a
            // currency-formatted value like "₱1,000.00", which other code in this
            // same form writes into txt_cash_discount). Clean and TryParse instead,
            // defaulting to 0 like the other cash-discount guards already fixed
            // elsewhere in this file.
            string AdditionalDiscountString = txt_additional_discount.Text.Replace('%', ' ').TrimEnd();
            decimal.TryParse(Helpers.GetCleanedPriceValue(AdditionalDiscountString), out decimal AdditionalDiscount);

             AdditionalDiscount = AdditionalDiscount / 100;

            decimal DiscountedTotal = netSalesTotal * AdditionalDiscount;

            // Was "netSalesTotal * 0.12m", computed before the additional discount
            // above was even known - so VAT was charged on the pre-discount amount
            // instead of the discounted (NET OF VAT) base. Per spec §8.2: VAT is
            // computed on net sales *after* the additional discount is deducted,
            // never before - taxing the undiscounted figure overcharges VAT on
            // money the customer never actually pays. This only changed the result
            // when an additional discount is actually present; with none, NET OF
            // VAT == netSalesTotal and the old and new figures are identical.
            decimal netOfVat = netSalesTotal - DiscountedTotal;
            decimal netSalesWithVat = netOfVat * 0.12m;

            decimal NetAmountDue = netOfVat + netSalesWithVat;

            decimal.TryParse(Helpers.GetCleanedPriceValue(txt_cash_discount.Text), out decimal cashDiscountValue);
            decimal TotalAmountDue = NetAmountDue - cashDiscountValue;

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

        // Project Quotation's own Quote Terms tab (Project_Quote_Terms) - same company-wide
        // terms content as Quick Quote's Quote_Terms tab (quotationTerms() above), just bound
        // to their own RichTextBoxes since a control can only live under one parent tab.
        private void projectQuotationTerms()
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
                    ProjectInclusionsRichTextBox.Text = inclusions;
                    string exclusions = row.Field<string>("ExclusionsQuotationTerms");
                    ProjectExclusionsRichTextBox.Text = exclusions;
                    string terms_and_conditions = row.Field<string>("TermAndConditions");
                    ProjectTermAndConditionsRichTextBox.Text = terms_and_conditions;
                }
            }

            //Styling Inclusions Rich Text Box
            ColorSelectedAndUnderlineWordsAndBold(ProjectInclusionsRichTextBox, "(PLACE)", Color.Blue);
            UnderlineWords(ProjectInclusionsRichTextBox, "during company regular working hours only.");
            ColorSelectedAndUnderlineWordsAndBold(ProjectInclusionsRichTextBox, "3 DAYS", Color.Black);
            BoldWords(ProjectInclusionsRichTextBox, "want more than the allowable and beyond working hours, additional charges will be applied.");

            //Styling Exclusions Rich Text Box
            MakeAllTextBlue(ProjectExclusionsRichTextBox);

            //Styling Terms and Conditions Rich Text Box
            BoldWords(ProjectTermAndConditionsRichTextBox, "PAYMENT TERMS:");
            ColorSelectedAndUnderlineWordsAndBold(ProjectTermAndConditionsRichTextBox, "CASH ON DELIVERY", Color.Blue);
            BoldWords(ProjectTermAndConditionsRichTextBox, "QUOTATION VALIDITY");
            BoldAndUnderlineWords(ProjectTermAndConditionsRichTextBox, "30 DAYS");
            BoldWords(ProjectTermAndConditionsRichTextBox, "thereafter, it shall be subject to reconfirmation");
            BoldWords(ProjectTermAndConditionsRichTextBox, "AVAILABILITY OF STOCK(S) AND/OR SERVICE(S): ");
            ColorSelectedAndUnderlineWordsAndBold(ProjectTermAndConditionsRichTextBox, "4-6 MONTHS", Color.Blue);
            BoldWords(ProjectTermAndConditionsRichTextBox, "DELIVERY TERMS:");
            ColorSelectedAndUnderlineWordsAndBold(ProjectTermAndConditionsRichTextBox, "WAREHOUSE TO SITE VIA SEA (w/o HAULING).", Color.Blue);
            BoldWords(ProjectTermAndConditionsRichTextBox, "OTHER CHARGES, TITLE, RISK OF LOSS:");
            BoldWords(ProjectTermAndConditionsRichTextBox, "within three(3) days");
            BoldWords(ProjectTermAndConditionsRichTextBox, "STORAGE:");
            BoldWords(ProjectTermAndConditionsRichTextBox, "SALES RETURN / CANCELLATION POLICY:");
            ColorSelectedAndUnderlineWordsAndBold(ProjectTermAndConditionsRichTextBox, "(as agreed upon %", Color.Blue);
            ColorSelectedAndUnderlineWordsAndBold(ProjectTermAndConditionsRichTextBox, "or fixed", Color.Red);
            BoldWords(ProjectTermAndConditionsRichTextBox, "a cancellation fee");
            ColorSelectedAndUnderlineWordsAndBold(ProjectTermAndConditionsRichTextBox, "(fixed %) ", Color.Red);
            BoldWords(ProjectTermAndConditionsRichTextBox, "WARRANTY:");
            ColorSelectedAndUnderlineWordsAndBold(ProjectTermAndConditionsRichTextBox, "ONE (1) YEAR", Color.Blue);
            BoldWords(ProjectTermAndConditionsRichTextBox, "SERVICES:");
            BoldWords(ProjectTermAndConditionsRichTextBox, "LIABILITY:");
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
            // This fired ComputeFooterTotals() (the Quick Quote footer math) unconditionally,
            // even while on the Project tab - so typing a cash discount for a Project
            // Quotation never actually refreshed that tab's own totals (RecomputeParentTotals)
            // until some unrelated grid cell edit happened to trigger it instead.
            if (isProject)
                RecomputeParentTotals();
            else
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
                // Same fix as btn_print_Click - use the Project Quotation's own
                // Inclusions/Exclusions/Terms rich text boxes, not Quick Quote's.
                SalesPrintModal printPage = new SalesPrintModal(false, true, documentNo, ProjectInclusionsRichTextBox.Text, ProjectExclusionsRichTextBox.Text, ProjectTermAndConditionsRichTextBox.Text);
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