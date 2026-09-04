using smpc_app.Services.Helpers;
using smpc_sales_app.Data;
using smpc_sales_app.Models;
using smpc_sales_app.Pages.Sales;
using smpc_sales_app.Services.Helpers;
using smpc_sales_app.Services.Sales;
using smpc_sales_system.Models;
using smpc_sales_system.Services.Sales;
using smpc_sales_system.Services.Sales.Models;
using smpc_sales_system.Services.Setup;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics.Metrics;
using System.Drawing;
using System.Linq;
using System.Management.Instrumentation;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TextBox;

namespace smpc_sales_system.Pages.Sales
{
    public partial class ItemSetUC : UserControl
    {
        // Company Setup-sourced values, injected by the host (Quotation.cs, right
        // alongside its existing ImageList assignment on every ItemSetUC it
        // constructs) since this control has no API access of its own.
        // Sales_Quotation_Bug_Report_2026-08-03.md #18 - both used to be hardcoded
        // (1.186 markup, 0.12 VAT) with no company-wide setting behind either and
        // no enforced relationship between the two numbers. Defaults here match
        // the historical hardcoded values, so a tab built without the host
        // injecting these (shouldn't happen, but safer than a silent $0/unmarked-
        // up computation) behaves exactly as before.
        public decimal MarkUpMultiplier { get; set; } = 1.186m;
        public decimal VatRate { get; set; } = 0.12m;

        public event EventHandler UpdateProjectConditions;
        public event EventHandler UpdateProjectContent;

        public event EventHandler DataChangedConditions;
        public event EventHandler DataChangedContent;
        public event EventHandler ItemChanged;
        public event EventHandler CellChangedProject;
        public event EventHandler CellChangedWiring;
        public event EventHandler ButtonClicked;
        public event EventHandler CellClicked;
        public event EventHandler CellClickedModel;
        public event EventHandler CellClickedCanvas;
        // Raised when the INV. column is clicked or the QTY column header is
        // right-clicked (both gated by _isEditable, same as every other picker on this
        // grid) - Quotation.cs owns the actual stock-check logic (it already has this
        // exact machinery for Quick Quote), so this just asks it to run that logic
        // against this tab's own grid instead of duplicating it here.
        public event EventHandler CellClickedStock;
        public event EventHandler FinalTxtBoxClicked;
        // Trello #044/#043/#049: raised when a SIZE UP row is clicked, so Quotation.cs
        // can open the pump picker and append the choice to this tab's SIZE UP grid.
        public event EventHandler SizeUpClicked;
        public event EventHandler DeleteReferenceCode;
        public event EventHandler GetEngineerUsers;

        public ItemSetUC()
        {
            InitializeComponent();

            // methods for event changes
            AttachTextChangedEventConditions(pnl_advanced_conditions);
            AttachTextChangedEventContent(pnl_project_content);
            AttachCellValuechangedEventProjectItems(dgv_project_items);
            AttachCellValuechangedEventWiring(dgv_wiring);
            dgv_project_items.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;

            // Clicking a header on either grid was re-sorting rows by that column, which
            // silently detaches them from the reference_code/hierarchy ordering these two
            // grids depend on (parent/child components, wiring sets) - disable it on every
            // column instead of editing each one's SortMode individually in the designer.
            foreach (DataGridViewColumn col in dgv_project_items.Columns)
                col.SortMode = DataGridViewColumnSortMode.NotSortable;
            foreach (DataGridViewColumn col in dgv_wiring.Columns)
                col.SortMode = DataGridViewColumnSortMode.NotSortable;

            // Trello #044/#043/#049: SIZE UP was 5 fixed textboxes with a hard ceiling
            // and no connection to FINAL. Spec §5.1.4: "more than five allowed,
            // scrollable" and "Final Selection - dropdown limited to what is listed in
            // Size Up". Replaced with dgv_size_up (declared in the Designer now, see
            // ItemSetUC.Designer.cs) - unlimited rows, native scroll, MODEL/BRAND/LIST
            // PRICE columns per spec's picker, restricted to pump items only same as
            // FINAL's picker. The 5 old textboxes are hidden in the Designer, not here.

            // Trello #070/#071: QTY accepted letters, and typing them into the grid's
            // trailing blank row committed it - adding a stray item row from garbage
            // input instead of only from an actual item selection (spec §5.1.2:
            // "Selecting an item adds exactly one blank row... MUST NOT" skip lines).
            // HandleNumericColumns already existed for this but was never wired to any
            // event, so it never ran.
            dgv_project_items.EditingControlShowing += (s, e) =>
                HandleNumericColumns(dgv_project_items, e, new[] { "project_items_qty" });

            setProjectWirings();

            //Default hide wiring
            WiringVisible(false);

            //Get the User for Engineering
            
        }

        public void SetUnitsOfMeasure(DataTable qty, DataTable qty_set)
        {
            bs_units_qty.DataSource = qty;
            cmb_unit.DisplayMember = "code";
            cmb_unit.ValueMember = "id";
            bs_units_qty_set.DataSource = qty_set;
        }


        private void AttachTextChangedEventContent(Panel pnls)
        {
            foreach (Control control in pnls.Controls)
            {
                if (control is TextBox textBox)
                {
                    textBox.TextChanged += TextBox_TextChangedContent;
                }
            }
        }

        private void AttachTextChangedEventConditions(Panel pnls)
        {
            foreach (Control control in pnls.Controls)
            {
                if (control is TextBox textBox)
                {
                    textBox.TextChanged += TextBox_TextChangedConditions;
                }
            }
        }


        private void AttachCellValuechangedEventProjectItems(DataGridView dgv)
        {
            dgv.CellValueChanged += DataGridView_CellValueChangedProjectItems;
        }

        private void AttachCellValuechangedEventWiring(DataGridView dgv)
        {
            dgv.CellValueChanged += DataGridView_CellValueChangedWiring;
        }


        // project items
        private void DataGridView_CellValueChangedProjectItems(object sender, DataGridViewCellEventArgs e)
        {
            timer_send_message_cell_items.Stop();
            timer_send_message_cell_items.Start();
        }

        // wiring
        private void DataGridView_CellValueChangedWiring(object sender, DataGridViewCellEventArgs e)
        {

            timer_send_message_cell_wiring.Stop();
            timer_send_message_cell_wiring.Start();
        }

        // project content
        private void TextBox_TextChangedContent(object sender, EventArgs e)
        {
            timer_update_content.Stop();
            timer_send_message_content.Stop();
            timer_update_content.Start();
            timer_send_message_content.Start();
        }

        // project advanced conditions
        private void TextBox_TextChangedConditions(object sender, EventArgs e)
        {
            timer_update_conditions.Stop();
            timer_send_message_conditions.Stop();
            timer_update_conditions.Start();
            timer_send_message_conditions.Start();
        }

        //
        //    TIMERS
        //
        private void timer_push_Tick(object sender, EventArgs e)
        {
            timer_send_message_conditions.Stop();
            DataChangedConditions?.Invoke(this, EventArgs.Empty);
        }

        private void timer_push_content_Tick(object sender, EventArgs e)
        {
            timer_send_message_content.Stop();
            DataChangedContent?.Invoke(this, EventArgs.Empty);
        }

        private void timer_push_cell_project_Tick(object sender, EventArgs e)
        {
            timer_send_message_cell_items.Stop();
            CellChangedProject?.Invoke(this, EventArgs.Empty);
        }
        private void timer_push_cell_wiring_Tick(object sender, EventArgs e)
        {
            timer_send_message_cell_wiring.Stop();
            CellChangedWiring?.Invoke(this, EventArgs.Empty);
        }
        private void timer_update_conditions_Tick(object sender, EventArgs e)
        {
            timer_update_conditions.Stop();
            UpdateProjectConditions?.Invoke(this, EventArgs.Empty);
        }
        private void timer_update_content_Tick(object sender, EventArgs e)
        {
            timer_update_content.Stop();
            UpdateProjectContent?.Invoke(this, EventArgs.Empty);
        }

        //
        //  GETTERS
        //
        public Dictionary<string, dynamic> GetAdvancedConditionsData()
        {
            Panel[] pnl_adv = { pnl_advanced_conditions };
            var data = Helpers.GetControlsValues(pnl_adv);
            Dictionary<string, dynamic> conditions = new Dictionary<string, dynamic>();

            if (data.ContainsKey("conditions_id") && data["conditions_id"] is string customerIdStr)
            {
                if (int.TryParse(customerIdStr, out int Id))
                {
                    data["conditions_id"] = Id;
                }
                else
                {
                    MessageBox.Show("Invalid ID");
                }
            }
            //conditions["sales_project_content_advanced_condition"] = data;

            return data;
        }

        public Dictionary<string, dynamic> GetAdvancedConditionsDataFiltered()
        {
            Panel[] pnl_adv = { pnl_advanced_conditions };
            var data = Helpers.GetControlsValues(pnl_adv);
            Dictionary<string, dynamic> conditions = new Dictionary<string, dynamic>();

            if (data.ContainsKey("conditions_id") && data["conditions_id"] is string customerIdStr)
            {
                if (int.TryParse(customerIdStr, out int Id))
                {
                    data["conditions_id"] = Id;
                }
                else
                {
                    MessageBox.Show("Invalid ID");
                }
            }

            //conditions["sales_project_content_advanced_condition"] = data;

            return data;
        }

        // A combo's SelectedValue as an int, or 0 when there is no usable selection.
        // Guards the three ways it can be unusable: null (nothing picked), a DataRowView
        // (bound before its ValueMember resolved - the same case GetControlsValues skips),
        // and anything that simply isn't a number.
        private static int SelectedIdOrZero(ComboBox combo)
        {
            if (combo?.SelectedValue == null) return 0;
            if (combo.SelectedValue is DataRowView) return 0;
            return int.TryParse(combo.SelectedValue.ToString(), out int id) ? id : 0;
        }

        public Dictionary<string, dynamic>  GetProjectContentsData()
        {
            Panel[] pnl_content = { pnl_project_content };
            var data = Helpers.GetControlsValues(pnl_content);
            Dictionary<string, dynamic> contents = new Dictionary<string, dynamic>();

            if (data.ContainsKey("content_id") && data["content_id"] is string contentIdStr)
            {
                if (int.TryParse(contentIdStr, out int Id))
                {
                    data["content_id"] = Id;
                }
                else
                {
                    MessageBox.Show("Invalid ID");
                }
            }

            var projectFinalSource = Helpers.ConvertDataGridViewToDataTable(dgv_final);
            List<SalesProjectContentFinal> finals = new List<SalesProjectContentFinal>();

            //added the datagridview
            foreach (DataRow final in projectFinalSource.Rows)
            {
                if (final == null) continue;

                var pfs = new SalesProjectContentFinal()
                {
                    id = int.TryParse(final["Id"]?.ToString(), out int tempId) ? tempId : 0,
                    sales_project_content_id = int.TryParse(final["content_id"]?.ToString(), out int tempContentId) ? tempContentId : 0,
                    // Persist which pump this row actually is, so it survives a reload -
                    // see SalesProjectContentFinal.item_id. Without this the duplicate
                    // guard in SetFinalPumpData is dead for every reloaded row.
                    item_id = final.Table.Columns.Contains("final_item_id")
                        && int.TryParse(final["final_item_id"]?.ToString(), out int tempItemId) ? tempItemId : 0,
                    final = final["final"]?.ToString() ?? string.Empty,
                    fla = decimal.TryParse(final["fla"]?.ToString(), out decimal tempFla) ? tempFla : 0,
                    voltage = decimal.TryParse(final["voltage"]?.ToString(), out decimal tempVoltage) ? tempVoltage : 0,

                };

                finals.Add(pfs);

            }

            data["sales_project_content_final"] = finals;
            data["sales_project_size_up"] = GetSizeUpData();

            //this is not include in panel but we need to get the value of wiring for the project
            data["is_wiring"] = chk_wiring.Checked;
            // Both of these are non-nullable ints on SalesProjectContent, but a ComboBox
            // with nothing selected hands back null - "-- No Template --" is exactly that,
            // and ASSIGNED ENGR. is empty until wiring is filled in. Sending the raw value
            // put a null in the payload and the save died deserializing it:
            // "Error converting value {null} to type 'System.Int32'. Path
            // 'template_project_id'". Nothing selected means 0, which is what the column
            // already stores for "no template".
            data["template_project_id"] = SelectedIdOrZero(cmb_template_project);

            // Never let a combo that FAILED TO LOAD erase a stored value (2026-09-04).
            // SelectedIdOrZero cannot tell "the user chose nothing" apart from "the
            // dropdown is empty because its fetch died", and both come back as 0 - which
            // the save then writes over a real engineer id. That is exactly how the
            // existing rows got zeroed. When the box is empty but we know what was
            // loaded, the loaded value wins; an explicit change still overrides it,
            // because then the combo has a selection and this never fires.
            int selectedEngineer = SelectedIdOrZero(cmb_assign_engineer_user_id);
            if (selectedEngineer == 0 && _pendingAssignedEngineerId > 0
                && cmb_assign_engineer_user_id.Items.Count == 0)
                selectedEngineer = _pendingAssignedEngineerId;

            data["assign_engineer_user_id"] = selectedEngineer;

            return data;
        }

        public DataTable selectedImageList { get; set; } = new DataTable();

        public Dictionary<string, dynamic> GetProjectWiringData()
        {
            var wiringSource = Helpers.ConvertDataGridViewToDataTable(dgv_wiring);

            List<SalesWiringModel> wiring = new List<SalesWiringModel>();

            foreach (DataRow item in wiringSource.Rows)
            {
                if (wiring == null) continue;

                var wire_contents = new SalesWiringModel
                {
                    id = int.TryParse(item["project_wiring_id"]?.ToString(), out int idVal) ? idVal : 0,
                    based_id = int.TryParse(item["project_wiring_based_id"]?.ToString(), out int based_id_Val) ? based_id_Val : 0,
                    materials = item["project_wiring_materials"]?.ToString() ?? string.Empty,
                    //amp_req = item["project_wiring_amp_req"]?.ToString() ?? string.Empty,
                    wire_req = item["project_wiring_wire_amp"]?.ToString() ?? string.Empty,
                    description = item["project_wiring_description"]?.ToString() ?? string.Empty,
                    num_of_wires_set = item["project_wiring_num_of_wiring_set"]?.ToString() ?? string.Empty,
                    // Trello #084: was commented out, so this factor never reached the
                    // save payload at all - the API always received an empty string here.
                    num_of_qty_set = item["project_wiring_num_of_qty_set"]?.ToString() ?? string.Empty,
                    distance_travelled_set = item["project_wiring_distance_travelled"]?.ToString() ?? string.Empty,
                    allowance_wire_set = item["project_wiring_allowance"]?.ToString() ?? string.Empty,
                    qty = int.TryParse(item["project_wiring_qty"]?.ToString(), out int qtyVal) ? qtyVal : 0,
                    num_of_sets = item["project_wiring_num_of_sets"]?.ToString() ?? string.Empty,
                    total_qty = int.TryParse(item["project_wiring_total_qty"]?.ToString(), out int totalQtyVal) ? totalQtyVal : 0,
                    cost = decimal.TryParse(item["project_wiring_cost"]?.ToString(), out decimal costVal) ? costVal : 0m,
                    total_cost = decimal.TryParse(item["project_wiring_total_cost"]?.ToString(), out decimal totalCostVal) ? totalCostVal : 0m,

                };

                wiring.Add(wire_contents);
            }

            Dictionary<string, object> data = new Dictionary<string, object>();

            data["sales_project_wiring"] = wiring;
            return data;
        }



        // GETTING THE PROJECT ITEMS FROM THE DGV
        public Dictionary<string, object>  GetProjectItems()
        {
            var projectSource = Helpers.ConvertDataGridViewToDataTable(dgv_project_items);
            List<SalesProjectItems> items = new List<SalesProjectItems>();

            for (int rowIndex = 0; rowIndex < projectSource.Rows.Count; rowIndex++)
            {
                DataRow item = projectSource.Rows[rowIndex];
                if (item == null) continue;

                // Skip the grid's own blank filler row rather than saving it as a real
                // item (fixed 2026-09-04). Same rule GetSizeUpData/GetFinalData already
                // apply: a row with no item, no BOM/component label and no reference
                // code carries nothing worth persisting - it is empty space in the grid,
                // not a candidate row. Without this, a blank row saved once (items_id 98
                // is exactly this shape: item_id 0, reference_code "", components "") and
                // CompareRef's int.Parse in SetFetchedItemData throws "Input string was
                // not in a correct format" on every future load of that quote, because
                // "".Split('.') yields one empty segment that isn't a number.
                bool hasItem = int.TryParse(item["item_id"]?.ToString(), out int filterItemId) && filterItemId > 0;
                bool hasComponentLabel = !string.IsNullOrWhiteSpace(item["project_items_components"]?.ToString());
                bool hasReferenceCode = !string.IsNullOrWhiteSpace(item["reference_code"]?.ToString());
                if (!hasItem && !hasComponentLabel && !hasReferenceCode)
                    continue;

                var spi = new SalesProjectItems
                {
                    // PK
                    items_id = int.TryParse(item["project_items_id"]?.ToString(), out int tempItemsId) ? tempItemsId : 0,
                    item_id = int.TryParse(item["item_id"]?.ToString(), out int tempItemId) ? tempItemId : 0,
                    based_id = int.TryParse(item["project_items_based_id"]?.ToString(), out int tempBasedId) ? tempBasedId : 0,
                    bom_id = int.TryParse(item["project_items_bom_id"]?.ToString(), out int tempBomId) ? tempBomId : 0,
                    reference_code = item["reference_code"]?.ToString() ?? string.Empty,
                    template_id = int.TryParse(item["project_items_template_id"]?.ToString(), out int templateId) ? templateId : 0,
                    man_days = int.TryParse(item["man_days"]?.ToString(), out int manDays) ? manDays : 0,
                    labor_rate = decimal.TryParse(item["labor_rate"]?.ToString(), out decimal laborRate) ? laborRate : 0,
                    components = item["project_items_components"]?.ToString() ?? string.Empty,
                    model = item["project_items_model"]?.ToString() ?? string.Empty,
                    item_inv_type = item["project_items_item_inv_type"]?.ToString() ?? string.Empty,
                    qty = int.TryParse(item["project_items_qty"]?.ToString(), out int qty) ? qty : 0,


                    list_price_per_unit = decimal.TryParse(Helpers.GetCleanedPriceValue(item["project_items_list_price"]?.ToString()), out decimal listPrice) ? listPrice : 0.0m,
                    unit_price = decimal.TryParse(Helpers.GetCleanedPriceValue(item["project_items_unit_price"]?.ToString()), out decimal unitPrice) ? unitPrice : 0.0m,

                    multiplier = item["project_items_multiplier"]?.ToString() ?? string.Empty,
                    discount_price = decimal.TryParse(Helpers.GetCleanedPriceValue(item["project_items_discount"]?.ToString()), out decimal discountPrice) ? discountPrice : 0.0m,
                    component_total = decimal.TryParse(Helpers.GetCleanedPriceValue(item["project_items_line_total"]?.ToString()), out decimal total) ? total : 0.0m,
                };

                // Each row only carries the images actually picked for that row (falls
                // back to empty if none picked) - see HandleItemImageSelectionClick, which
                // records selections in SelectedImagesByRow keyed by this same grid row
                // index.
                spi.quick_selected_image = SelectedImagesByRow.TryGetValue(rowIndex, out var rowImages)
                    ? rowImages
                    : new List<Dictionary<string, object>>();

                items.Add(spi);
            }
            Dictionary<string, object> data = new Dictionary<string, object>();

            data["sales_project_items"] = items;
            return data;
        }

        private static class ProjectQuoteDGV
        {
            public static string QTY = "project_items_qty";
            public static string MULTIPLIER = "project_items_multiplier";
            public static string DISCOUNT = "project_items_discount";
            public static string LIST_PRICE = "project_items_list_price";
            public static string UNIT_PRICE = "project_items_unit_price";
            public static string NET_TOTAL = "project_items_line_total";
        }

        // DGVProjectComputation / ComputeProjectQuote() deleted: this was a second,
        // independently-parsed multiplier engine (different rules for "*" and "/" tokens
        // than CalculateDiscountMultiplier below) whose only call site, ComputeProjectDgv(),
        // was itself dead code - its one would-be caller (dgv_project_items_CellEndEdit) has
        // had that call commented out, so neither ever ran. The DISCOUNT/MARK UP PRICE column
        // they used to write to was therefore never actually updated by any live code path.
        // ComputeReferenceNonHierarchy (the function that *does* run, via
        // Quotation.RecomputeParentTotals -> ProjectComputationLoop on every cell edit) now
        // populates that column itself, using the same CalculateDiscountMultiplier result
        // that already determines the real, saved line total - so what's displayed always
        // matches what's charged.

        public void setMultiplier(List<string> multiplier)
        {
            bs_multiplier.DataSource = multiplier;
            //this.project_items_multiplier.DataSource = multiplier;
        }

        public Dictionary<string, dynamic> ProjectComputationLoop()
        {
            //dgv_project_items.EndEdit();

            ComputeByReferenceHierarchy(dgv_project_items);
            ComputeReferenceNonHierarchy(dgv_project_items);

            // Cash discount is a single project-wide figure the user types once on the
            // hosting Quotation form (txt_cash_discount) - it must only ever be applied
            // once against the whole project's net sales. This used to walk up to the
            // hosting Quotation and read/subtract that same field once per tab; summed
            // across every tab in Quotation.RecomputeParentTotals, that double- (or N-)
            // counted the discount for any project with more than one active tab, and -
            // because RecomputeParentTotals then wrote the summed total back into this
            // very field - caused it to inflate further on every subsequent edit. The
            // single, authoritative subtraction now happens once in
            // Quotation.RecomputeParentTotals after all tabs' net sales are summed;
            // this method no longer touches cash discount at all.
            decimal gross_sales = 0, vat_amount = 0, net_sales = 0;
            decimal percent_discount = 0;
            decimal net_amount_due = 0, total_amount_due = 0;
            // Was a hardcoded const - now the company-wide, configurable value
            // injected by the host (see VatRate's own comment at the top of this class).
            decimal VAT_RATE = VatRate;

            foreach (DataGridViewRow row in this.dgv_project_items.Rows)
            {
                if (row.Cells[ProjectQuoteDGV.QTY].Value != null && row.Cells[ProjectQuoteDGV.LIST_PRICE].Value != null)
                {
                    // Calculate gross amount (qty * list price)
                    decimal qty = decimal.TryParse(row.Cells[ProjectQuoteDGV.QTY].Value.ToString(), out decimal parsedQty) ? parsedQty : 0m;


                    decimal listPrice = decimal.Parse(Helpers.GetCleanedPriceValue(row.Cells[ProjectQuoteDGV.LIST_PRICE].Value.ToString()));
                    decimal rowGross = (decimal)(qty * listPrice);
                    gross_sales += rowGross;

                    // Get net total (after discount)
                    if (row.Cells[ProjectQuoteDGV.NET_TOTAL].Value != null &&
                        !String.IsNullOrEmpty(row.Cells[ProjectQuoteDGV.NET_TOTAL].Value.ToString()))
                    {                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                   

                        decimal netTotal = decimal.Parse(Helpers.GetCleanedPriceValue(row.Cells[ProjectQuoteDGV.NET_TOTAL].Value.ToString()));
                        net_sales += netTotal;
                    }
                }
            }

            Console.WriteLine("ProjectComputationLoop - net sales: " + net_sales);

            // Calculate percent discount
            if (gross_sales != 0)
            {
                percent_discount = ((gross_sales - net_sales) / gross_sales) * 100;
            }
            // Calculate VAT (12% of net sales)
            vat_amount = net_sales * VAT_RATE;

            // Cash discount is applied once at the project level (see comment above), not
            // per tab - this tab's own "net amount due"/"total amount due" are therefore
            // just its net sales and net sales + VAT, before that single project-wide
            // discount is subtracted.
            net_amount_due = net_sales;

            // Calculate total amount due (net amount + VAT)
            total_amount_due = net_amount_due + vat_amount;

            // Format and display results
            Dictionary<string, dynamic> data = new Dictionary<string, dynamic>();
            data.Add("gross_sales", Helpers.MoneyFormatDecimal(gross_sales));
            data.Add("vat_amount", Helpers.MoneyFormatDecimal(vat_amount));
            data.Add("net_sales", Helpers.MoneyFormatDecimal(net_sales));
            data.Add("percent_discount", percent_discount.ToString("0.00") + "%");
            data.Add("net_amount_due", Helpers.MoneyFormatDecimal(net_amount_due));
            data.Add("total_amount_due", Helpers.MoneyFormatDecimal(total_amount_due));
            return data;

            //dgv_project_items.EndEdit();
        }
        //
        // SETTERS
        //
        public void SetAdvancedPanelData(DataTable dt)
        {
            Panel[] pnls = { pnl_advanced_conditions };
            Helpers.BindControls(pnls, dt);
        }

        public void SetContentsPanelData(DataTable dt)
        {
            Panel[] pnls = { pnl_project_content };
            Helpers.BindControls(pnls, dt);

            // BindControls does set cmb_assign_engineer_user_id, but it is a no-op here:
            // the combo has no DataSource yet (ItemSetUC_Load fills it asynchronously,
            // later), and assigning SelectedValue to an unbound ComboBox does nothing.
            // So the fetched engineer has to be held until that load finishes - see the
            // restore block in ItemSetUC_Load. Same shape as SetTemplateName/txt_template_id.
            _pendingAssignedEngineerId = 0;
            if (dt != null && dt.Rows.Count > 0 && dt.Columns.Contains("assign_engineer_user_id")
                && int.TryParse(dt.Rows[0]["assign_engineer_user_id"]?.ToString(), out int engrId))
            {
                _pendingAssignedEngineerId = engrId;

                // If the load already ran (a rebind on an open form), apply it now -
                // nothing else will.
                if (cmb_assign_engineer_user_id.DataSource != null && engrId > 0)
                    cmb_assign_engineer_user_id.SelectedValue = engrId;
            }
        }

        // The assign_engineer_user_id read off the content row, waiting for the engineer
        // dropdown to finish loading so it can actually be selected. 0 means "none".
        private int _pendingAssignedEngineerId;

        public void SetTemplateName(string template_id)
        {
            txt_template_id.Text = template_id;
        }
        // FLA / VOLTAGE are DERIVED from the FINAL list, not typed - spec 8.4 feeds both
        // amp formulas from them, and "with multiple pumps, base the calculation on the
        // largest FLA", hence the max rather than the last row.
        //
        // Extracted 2026-09-03. This ran only inside SetFinalPumpData - the interactive
        // "pick a final pump" path - so on a REOPENED quote the finals loaded into the
        // grid but FLA and VOLTAGE came back blank. Both amp formulas bail on an
        // unparseable FLA, so AMP REQ. on rows 1 and 7 stayed empty on every saved quote
        // until someone re-picked a pump. Same shape as the other load-path gaps found
        // today: the interactive path derived state that the load path never restored.
        private void RefreshFlaVoltageFromFinals()
        {
            if (dgv_final == null) return;

            decimal fla_highest = 0;
            decimal voltage_highest = 0;

            foreach (DataGridViewRow row in dgv_final.Rows)
            {
                if (row.IsNewRow) continue;

                decimal fla = decimal.TryParse(row.Cells["Fla"].Value?.ToString() ?? "0", out var fl) ? fl : 0m;
                decimal voltage = decimal.TryParse(row.Cells["Voltage"].Value?.ToString() ?? "0", out var vol) ? vol : 0m;

                if (fla > fla_highest) fla_highest = fla;
                if (voltage > voltage_highest) voltage_highest = voltage;
            }

            // Nothing usable on the list - leave whatever is on screen rather than
            // stamping "0" over it.
            if (fla_highest <= 0 && voltage_highest <= 0) return;

            txt_FLA.Text = fla_highest.ToString();
            txt_VOLT.Text = voltage_highest.ToString();
        }

        // Recomputes the two AMP REQ. cells spec 8.4 defines as formulas: row 1
        // (ECB -> controller) and row 7 (controller -> motor, per starting method).
        // Called after the wiring grid is loaded - NOT from SetFinalData, which runs
        // earlier in the load sequence while dgv_wiring is still empty, and
        // SetWiringAmpReq no-ops on an empty grid.
        // Guards against re-entry: this calls the starting-method handler, which writes
        // into dgv_wiring, whose own cell-changed handler recomputes the row totals.
        // None of those currently loop back here, but this is the one place every
        // trigger funnels through, so it is the right place to make that safe.
        private bool _refreshingWiringAmps;

        private void RefreshWiringAmpRequirements()
        {
            if (_refreshingWiringAmps) return;
            if (dgv_wiring == null || dgv_wiring.Rows.Count == 0) return;

            _refreshingWiringAmps = true;
            try
            {
                computeECBToController();                                  // row 1
                cmb_starting_method_SelectedIndexChanged(this, EventArgs.Empty); // row 7
            }
            finally
            {
                _refreshingWiringAmps = false;
            }
        }

        public void SetFinalData(DataTable dt)
        {

            if(dt.Rows.Count > 0)
            {
                foreach (DataRow row in dt.Rows)
                {
                    // final_item_id now comes back with the row (item_id is persisted as
                    // of 2026-09-03). It used to be left blank because the saved shape had
                    // no item id - which quietly disabled SetFinalPumpData's duplicate
                    // guard for every reloaded row, so re-picking a pump already on the
                    // list appended a second copy and the next save stored both.
                    object itemId = dt.Columns.Contains("item_id") ? row["item_id"] : null;
                    dgv_final.Rows.Add(row["id"], row["sales_project_content_id"], row["final"], row["fla"], row["voltage"], itemId);
                }

                // Derive FLA / VOLTAGE from what was just loaded - see
                // RefreshFlaVoltageFromFinals. The amp cells themselves are refreshed
                // later, once the wiring grid exists (SetProjectWiring).
                RefreshFlaVoltageFromFinals();
            }
        }

        public void SetWiring(string Checked)
        {
            chk_wiring.Checked = bool.Parse(Checked);
        }

        // Trello #044/#043/#049: was a fixed 5-textbox scan. Kept the same method name/
        // signature (the one call site was already commented out, but keeping it avoids
        // surprising a future caller expecting the old shape) while reading from the
        // grid instead.
        // Was dead code with no callers, returning a Dictionary keyed " size_up_1",
        // " size_up_2"... (note the leading space) holding only the model string - a shape
        // nothing could persist, and which dropped item_id entirely. Now returns the same
        // list shape the finals use, and GetProjectContentsData actually sends it.
        public List<SalesProjectSizeUp> GetSizeUpData()
        {
            var sizeUps = new List<SalesProjectSizeUp>();
            if (dgv_size_up == null) return sizeUps;

            foreach (DataGridViewRow row in dgv_size_up.Rows)
            {
                if (row.IsNewRow) continue;

                string model = row.Cells["size_up_model"].Value?.ToString() ?? string.Empty;
                int.TryParse(row.Cells["size_up_item_id"].Value?.ToString(), out int itemId);

                // A row with neither an item nor a model is the grid's own empty filler,
                // not a candidate pump.
                if (itemId <= 0 && string.IsNullOrWhiteSpace(model)) continue;

                sizeUps.Add(new SalesProjectSizeUp
                {
                    id = int.TryParse(row.Cells["size_up_id"]?.Value?.ToString(), out int rowId) ? rowId : 0,
                    sales_project_content_id = 0, // server stamps this from the owning content row
                    item_id = itemId,
                    model = model,
                });
            }

            return sizeUps;
        }

        // Counterpart to GetSizeUpData for the FINAL grid. GetProjectContent (the Sales
        // save path) built this list inline; pulling it out lets a caller that does NOT
        // want the whole content dictionary read just this one grid - specifically the
        // Engineering app's Sales Quotation page, which sends the rest of the content
        // back exactly as fetched on purpose and only lets an engineer's Size Up / Final
        // / item table / wiring edits through (§3.2).
        public List<SalesProjectContentFinal> GetFinalData()
        {
            var finals = new List<SalesProjectContentFinal>();
            if (dgv_final == null) return finals;

            foreach (DataGridViewRow row in dgv_final.Rows)
            {
                if (row.IsNewRow) continue;

                string model = row.Cells["Final"].Value?.ToString() ?? string.Empty;
                int.TryParse(row.Cells["final_item_id"].Value?.ToString(), out int itemId);

                // Same "empty filler row" rule GetSizeUpData applies.
                if (itemId <= 0 && string.IsNullOrWhiteSpace(model)) continue;

                finals.Add(new SalesProjectContentFinal
                {
                    id = int.TryParse(row.Cells["Id"]?.Value?.ToString(), out int rowId) ? rowId : 0,
                    sales_project_content_id = int.TryParse(row.Cells["content_Id"]?.Value?.ToString(), out int contentId) ? contentId : 0,
                    item_id = itemId,
                    final = model,
                    fla = decimal.TryParse(row.Cells["Fla"].Value?.ToString(), out decimal fla) ? fla : 0,
                    voltage = decimal.TryParse(row.Cells["Voltage"].Value?.ToString(), out decimal voltage) ? voltage : 0,
                });
            }

            return finals;
        }

        // Mirrors dgv_final_CellClick exactly - single MODEL column now, same as FINAL,
        // so there's no longer a per-column decision to make about which cell opens
        // the picker.
        private void dgv_size_up_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (!_isEditable) return;
            SizeUpClicked?.Invoke(this, EventArgs.Empty);
        }

        // Called from Quotation.cs's SizeUpClicked handler once the user picks pumps
        // from SizeUpPickerModal (one call per selected pump - the modal itself handles
        // multi-select). Silently ignores a re-pick of the same item instead of listing
        // it twice.
        public void AddSizeUpRow(string itemId, string model)
        {
            if (dgv_size_up == null) return;

            foreach (DataGridViewRow row in dgv_size_up.Rows)
                if (row.Cells["size_up_item_id"].Value?.ToString() == itemId) return;

            // Set by cell name, not positionally: Rows.Add(itemId, model) relied on
            // size_up_item_id being column 0, which stopped being true once the hidden
            // size_up_id column was added in front of it. Naming the cells makes this
            // independent of column order.
            int index = dgv_size_up.Rows.Add();
            dgv_size_up.Rows[index].Cells["size_up_item_id"].Value = itemId;
            dgv_size_up.Rows[index].Cells["size_up_model"].Value = model;

            // Size Up is what FINAL may be chosen from, so a change here can change the
            // pump behind FLA. Cheap to refresh and keeps the amps from going stale.
            RefreshWiringAmpRequirements();
        }

        // Loads saved Size Up rows back onto the grid, mirroring SetFinalData. Nothing did
        // this before because Size Up was never persisted at all - see GetSizeUpData.
        public void SetSizeUpData(DataTable dt)
        {
            if (dgv_size_up == null) return;

            dgv_size_up.DataSource = null;
            dgv_size_up.Rows.Clear();

            if (dt == null) return;

            foreach (DataRow row in dt.Rows)
            {
                int index = dgv_size_up.Rows.Add();
                dgv_size_up.Rows[index].Cells["size_up_id"].Value = row.Table.Columns.Contains("id") ? row["id"] : null;
                dgv_size_up.Rows[index].Cells["size_up_item_id"].Value = row.Table.Columns.Contains("item_id") ? row["item_id"] : null;
                dgv_size_up.Rows[index].Cells["size_up_model"].Value = row.Table.Columns.Contains("model") ? row["model"] : null;
            }
        }

        // What FINAL selection filters against (spec §5.1.4: "Final Selection - dropdown
        // limited to what is listed in Size Up").
        public List<int> GetSizeUpItemIds()
        {
            var ids = new List<int>();
            if (dgv_size_up == null) return ids;

            foreach (DataGridViewRow row in dgv_size_up.Rows)
                if (int.TryParse(row.Cells["size_up_item_id"].Value?.ToString(), out int id))
                    ids.Add(id);

            return ids;
        }



        public async void SetProjectItemsData(DataTable dt, string TemplateName)
        {
            /* Pseudocode (plan):
               - Build a lookup/dictionary of node_id -> ProjectTemplateChildModel without throwing on duplicate keys.
                 Use GroupBy(node_id) and pick the first row for each group (optionally log duplicates).
               - Find root nodes (parent_node_id == 0) and order by node_order.
               - For each root node:
                 - Add a row to dgv_project_items and populate the standard fields.
                 - Style the components cell (bold + background).
                 - Recursively add child nodes by calling AddChildNodesFromDb with the full dt and the prepared nodeLookup.
               - Finally set column autosize and call bondsTncReadOnly() to apply special rules.
            */

            txt_template_name.Text = TemplateName;

            //dgv_project_items.Rows.Clear();

            ClearProjectItemsDgv();

            //dgv_project_items.Columns[8].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;

            //Font boldFont = new Font(dgv_project_items.DefaultCellStyle.Font, FontStyle.Bold);
            //Font normalFont = new Font(dgv_project_items.DefaultCellStyle.Font, FontStyle.Regular);

            //// Build a dictionary that handles duplicate node_id values by grouping and taking the first row for each id.
            //var nodeLookup = dt.AsEnumerable()
            //                   .GroupBy(row => row.Field<int>("node_id"))
            //                   .ToDictionary(
            //                       g => g.Key,
            //                       g =>
            //                       {
            //                           var row = g.First();
            //                           return new ProjectTemplateChildModel
            //                           {
            //                               Id = row.Field<int>("id"),
            //                               ParentId = row.Field<int>("parent_id"),
            //                               ItemId = row.Field<int>("item_id"),
            //                               Components = row.Field<string>("components"),
            //                               Level = row.Field<int>("level")
            //                           };
            //                       });

            //var rootNodes = dt.AsEnumerable()
            //                  .Where(row => row.Field<int>("parent_node_id") == 0)
            //                  .OrderBy(row => row.Field<int>("node_order"))
            //                  .ToList();

            //foreach (var rootNode in rootNodes)
            //{
            //    int parentRowIndex = dgv_project_items.Rows.Add();

            //    DataGridViewRow newRow = dgv_project_items.Rows[parentRowIndex];

            //    newRow.Cells["project_items_node_name"].Value = rootNode.Field<string>("node_name");
            //    newRow.Cells["project_items_node_id"].Value = rootNode.Field<int>("node_id");
            //    newRow.Cells["project_items_parent_node_id"].Value = rootNode.Field<int>("parent_node_id");
            //    newRow.Cells["project_items_node_order"].Value = rootNode.Field<int>("node_order");
            //    newRow.Cells["project_items_node_type"].Value = rootNode.Field<string>("node_type");
            //    newRow.Cells["project_items_components"].Value = "▶ " + rootNode.Field<string>("node_name");


            //    newRow.Cells["project_items_components"].Style.BackColor = Color.LightCoral;
            //    newRow.Cells["project_items_components"].Style.Font = boldFont;

            //    // Recursively add child nodes
            //    //AddChildNodesFromDb(rootNode.Field<int>("node_id"), dt, nodeLookup, 1);

            //}

            //dgv_project_items.Columns[1].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            bondsTncReadOnly();
        }

        private void bondsTncReadOnly()
        {
            foreach (DataGridViewRow row in dgv_project_items.Rows)
            {
                if (row.IsNewRow)
                    continue;

                var cellValue = row.Cells["project_items_components"].Value;



                if (cellValue != null)
                {
                    string components = cellValue.ToString().ToLower();
                    Console.WriteLine($"Row {row.Index}: {components}");


                    bool containsTnC = components.Contains("t&c labor");
                    bool containsBonds = components.Contains("bonds");

                    Console.WriteLine($"Row {row.Index} | containsTnC: {containsTnC}, containsBonds: {containsBonds}");


                    if (containsTnC || containsBonds)
                    {
                        //row.ReadOnly = true;

                        row.Cells["project_items_model"].Style.BackColor = Color.Silver;
                        row.Cells["project_items_item_inv_type"].Style.BackColor = Color.Silver;

                    }

                    if (containsTnC)
                    {
                        DataGridViewTextBoxCell textBoxCell = new DataGridViewTextBoxCell();
                        row.Cells["project_items_multiplier"] = textBoxCell;
                        row.Cells["project_items_multiplier"].Value = "1";
                    }

                    if (containsBonds)
                    {
                        // project_items_multiplier is a combo box column bound to a fixed
                        // list of standard multipliers (bs_multiplier / setMultiplier) - "0.035"
                        // isn't one of those choices, so setting it directly on a combo cell
                        // threw "DataGridViewComboBoxCell value is not valid". The "t&c labor"
                        // case above already avoids this by swapping to a plain text cell first;
                        // do the same here (a no-op if the t&c swap above already ran for this row).
                        if (!(row.Cells["project_items_multiplier"] is DataGridViewTextBoxCell))
                        {
                            DataGridViewTextBoxCell bondsTextBoxCell = new DataGridViewTextBoxCell();
                            row.Cells["project_items_multiplier"] = bondsTextBoxCell;
                        }
                        row.Cells["project_items_multiplier"].Value = "0.035";
                    }
                }
            }
        }

        bool isViewProjectItem = false;

        public void SetFetchedItemData(DataTable dt)
        {
            try
            {
                var stringTable = Helpers.ConvertDataTableToStringTable(dt);

                string lastRef = null;

                foreach (DataRow row in stringTable.Rows)
                {
                    string listprice = row["list_price_per_unit"].ToString();
                    string unitprice = row["unit_price"].ToString();
                    string discountprice = row["discount_price"].ToString();
                    string componenttotal = row["component_total"].ToString();

                    row["list_price_per_unit"] = Helpers.FormatAsCurrency(listprice);
                    row["unit_price"] = Helpers.FormatAsCurrency(unitprice);
                    row["discount_price"] = Helpers.FormatAsCurrency(discountprice);
                    row["component_total"] = Helpers.FormatAsCurrency(componenttotal);

                    string current = row["reference_code"].ToString();

                    if (lastRef == null || CompareRef(current, lastRef) > 0)
                    {
                        lastRef = current;
                    }
                }

                // A tab with no items yet (e.g. a brand-new tab) means stringTable has no
                // rows, so the loop above never ran and lastRef is still null - int.Parse(null)
                // used to throw ArgumentNullException here and abort the whole load. Reference
                // codes start counting from 0 in that case, same as counterReference's default.
                LastRefInt = lastRef != null && int.TryParse(lastRef, out int parsedLastRef) ? parsedLastRef : 0;

                int CompareRef(string a, string b)
                {
                    // TryParse, not Parse (fixed 2026-09-04): a blank reference_code -
                    // whether from a legacy row saved before GetProjectItems started
                    // filtering them out, or any other unexpected value - split into one
                    // empty segment, and int.Parse("") threw FormatException ("Input
                    // string was not in a correct format"), aborting the ENTIRE item load
                    // for every row in the tab, not just the bad one. An unparseable
                    // segment now sorts as 0, the same way a missing segment already does
                    // a few lines down, so the bad row just sorts first instead of taking
                    // the whole grid down with it.
                    int ParseSegment(string s) => int.TryParse(s, out int n) ? n : 0;
                    var aParts = a.Split('.').Select(ParseSegment).ToArray();
                    var bParts = b.Split('.').Select(ParseSegment).ToArray();

                    int length = Math.Max(aParts.Length, bParts.Length);

                    for (int i = 0; i < length; i++)
                    {
                        int aVal = i < aParts.Length ? aParts[i] : 0;
                        int bVal = i < bParts.Length ? bParts[i] : 0;

                        if (aVal != bVal)
                            return aVal.CompareTo(bVal);
                    }

                    return 0;
                }

                DgvProjectItems.DataSource = stringTable;
                RefreshAllStockIndicators();

                // Seed SelectedImagesByRow from what was already saved for this tab, so a
                // row the user doesn't touch this session still saves with its existing
                // images (see GetProjectItems, which reads from SelectedImagesByRow on
                // Save). Row index here lines up with the row index each item occupies in
                // the grid, since stringTable is bound directly (no extra sort/filter).
                SelectedImagesByRow.Clear();
                if (selectedImageList != null && selectedImageList.Columns.Contains("quotation_quick_id"))
                {
                    for (int rowIdx = 0; rowIdx < stringTable.Rows.Count; rowIdx++)
                    {
                        if (!int.TryParse(stringTable.Rows[rowIdx]["items_id"]?.ToString(), out int itemsId) || itemsId == 0)
                            continue;

                        var imagesForRow = selectedImageList.AsEnumerable()
                            .Where(img => int.TryParse(img["quotation_quick_id"]?.ToString(), out int qId) && qId == itemsId)
                            .Select(img => new Dictionary<string, object>
                            {
                                { "image_id", img["image_id"] },
                                { "is_selected", img["is_selected"] }
                            })
                            .ToList();

                        if (imagesForRow.Count > 0)
                            SelectedImagesByRow[rowIdx] = imagesForRow;
                    }
                }
                LoadProjectImageCounts();

                isViewProjectItem = true;



            }
            catch (Exception ex)
            {
                MessageBox.Show("Error setting fetched item data: " + ex.Message);
            }

        }
        public void SetProjectWiring(DataTable dt)
        {
            // Bug #080 (Trello, "Repeating table data for wiring"): this grid is
            // populated by manually adding rows (dgv_wiring.Rows.Add() below), not
            // by data-binding - DataSource = null doesn't clear rows added that way,
            // so calling this again (switching tabs, reloading) kept stacking a new
            // full copy of the wiring rows on top of whatever was already there.
            dgv_wiring.DataSource = null;
            dgv_wiring.Rows.Clear();

            int i = 0;

            foreach (DataRow row in dt.Rows)
            {
                int rowIndex = dgv_wiring.Rows.Add();
                DataGridViewRow dgvRow = dgv_wiring.Rows[rowIndex];

                if (dt.Columns.Contains("Id"))
                {
                    dgvRow.Cells["project_wiring_id"].Value = row["id"];
                    dgvRow.Cells["project_wiring_based_id"].Value = row["based_id"];
                    dgvRow.Cells["project_wiring_materials"].Value = defaultWiring[i];
                    dgvRow.Cells["project_wiring_wire_amp"].Value = row["wire_req"];
                    dgvRow.Cells["project_wiring_description"].Value = row["description"];
                    dgvRow.Cells["project_wiring_num_of_wiring_set"].Value = row["num_of_wires_set"];
                    // Trello #084: was never loaded back onto the grid, so a reopened
                    // quote always showed this factor blank even if it had been saved.
                    dgvRow.Cells["project_wiring_num_of_qty_set"].Value = row["num_of_qty_set"];
                    dgvRow.Cells["project_wiring_distance_travelled"].Value = row["distance_travelled_set"];
                    dgvRow.Cells["project_wiring_allowance"].Value = row["allowance_wire_set"];
                    dgvRow.Cells["project_wiring_qty"].Value = row["qty"];
                    dgvRow.Cells["project_wiring_num_of_sets"].Value = row["num_of_sets"];
                    dgvRow.Cells["project_wiring_num_of_wiring_set_format"].Value = defaultQTYFormat[i];
                    dgvRow.Cells["project_wiring_total_qty"].Value = row["total_qty"];
                    dgvRow.Cells["project_wiring_qty_format"].Value = defaultQTYFormat[i];
                    dgvRow.Cells["project_wiring_cost"].Value = row["cost"];
                    dgvRow.Cells["project_wiring_total_cost"].Value = row["total_cost"];



                }

                i++;
            }

            // The grid now exists, so the two computed AMP REQ. cells can be filled in.
            // Doing this here rather than in SetFinalData is deliberate: that runs earlier
            // in the load sequence, while this grid is still empty.
            RefreshWiringAmpRequirements();
        }

        private void ApplyRowStyles()
        {
            foreach (DataGridViewRow row in dgv_project_items.Rows)
            {
                //if (!row.IsNewRow)
                //{
                //    DataGridViewCell cell = row.Cells[9];
                //    int nodeTypeColumnIndex = dgv_project_items.Columns["project_items_node_type"].Index;
                //    string nodeType = row.Cells[nodeTypeColumnIndex].Value?.ToString().Trim();

                //    //MessageBox.Show($"Processing Row: {nodeType}");

                //    row.DefaultCellStyle.BackColor = Color.White; // Reset

                //    if (nodeType == "Parent")
                //    {
                //        cell.Style.BackColor = Color.Yellow;
                //        //MessageBox.Show("red");
                //    }
                //    else if (nodeType == "Leaf")
                //    {
                //        cell.Style.BackColor = Color.LightCoral;
                //        //MessageBox.Show("yellow");
                //    }
                //    else if (string.IsNullOrWhiteSpace(nodeType))
                //    {
                //        cell.Style.BackColor = Color.LightGreen;
                //        // MessageBox.Show("none");
                //    }
                //}
            }

            dgv_project_items.Invalidate(); // Force UI update
        }

        private void AddChildNodesFromDb(int parentId, DataTable allNodes,
                                         Dictionary<int, ProjectTemplateChildModel> nodeLookup, int level)
        {
            var childNodes = allNodes.AsEnumerable()
                                     .Where(row => row.Field<int>("parent_node_id") == parentId)
                                     .OrderBy(row => row.Field<int>("node_order"))
                                     .ToList();

            Font boldFont = new Font(dgv_project_items.DefaultCellStyle.Font, FontStyle.Bold);
            Font normalFont = new Font(dgv_project_items.DefaultCellStyle.Font, FontStyle.Regular);

            foreach (var childNode in childNodes)
            {
                int rowIndex = dgv_project_items.Rows.Add();

                DataGridViewRow newRow = dgv_project_items.Rows[rowIndex];

                string indent = new string(' ', level * 4) + "└▶ ";

                newRow.Cells["project_items_node_name"].Value = childNode.Field<string>("node_name");
                newRow.Cells["project_items_node_id"].Value = childNode.Field<int>("node_id");
                newRow.Cells["project_items_parent_node_id"].Value = childNode.Field<int>("parent_node_id");
                newRow.Cells["project_items_node_order"].Value = childNode.Field<int>("node_order");
                newRow.Cells["project_items_node_type"].Value = childNode.Field<string>("node_type");

                newRow.Cells["project_items_components"].Value = indent + childNode.Field<string>("node_name");


                if (childNode.Field<string>("node_type") == "Parent")
                {
                    newRow.Cells[8].Style.BackColor = Color.LightGreen;
                }
                else
                {
                    newRow.Cells[8].Style.BackColor = Color.LightYellow;
                }
                newRow.Cells[8].Style.Font = boldFont;


                AddChildNodesFromDb(childNode.Field<int>("node_id"), allNodes, nodeLookup, level + 1);
            }

        }


        // Trello #044/#043/#049: FINAL's own picker is now multi-select, same as SIZE
        // UP's - ItemId lets repeated picks (re-opening the picker, checking the same
        // pump again) skip re-adding a duplicate row instead of piling up copies.
        public void SetFinalPumpData(string FLA, string Voltage, string Final, string ItemId)
        {
            // Duplicate guard. Matches on the pump's item id, and falls back to the model
            // name when either side has no item id - rows saved before finals carried an
            // item_id (2026-09-03) still come back with it blank, and without the fallback
            // those legacy rows skip the check entirely and can be added a second time.
            // Mirrors finalIdentity() on the server, which now backstops the same rule.
            foreach (DataGridViewRow existingRow in dgv_final.Rows)
            {
                if (existingRow.IsNewRow) continue;

                string existingItemId = existingRow.Cells["final_item_id"].Value?.ToString();

                if (!string.IsNullOrEmpty(ItemId) && !string.IsNullOrEmpty(existingItemId))
                {
                    if (existingItemId == ItemId) return;
                    continue;
                }

                string existingModel = existingRow.Cells["Final"].Value?.ToString();
                if (!string.IsNullOrWhiteSpace(existingModel)
                    && string.Equals(existingModel.Trim(), (Final ?? string.Empty).Trim(), StringComparison.OrdinalIgnoreCase))
                    return;
            }

            //The first parameter is Id, the 2nd paramter ContentId
            //When adding

            dgv_final.Rows.Add(0, 0, Final.ToString(), FLA.ToString(), Voltage.ToString(), ItemId);

            // Spec §5.1.4: "Additional pumps may be added (+add final pump) and are
            // inserted into the table as further pumps without repeating the template" -
            // only reached when the dedupe check above didn't already return, so this
            // runs once per genuinely new FINAL pick, never on a re-pick of the same pump.
            AddFinalPumpToItemsGrid(ItemId, Final);

            decimal Pump_Total_Qty = 0;

            RefreshFlaVoltageFromFinals();

            foreach (DataGridViewRow row in dgv_project_items.Rows)
            {
                if (row.IsNewRow) continue;


                if (row.Cells["project_items_components"].Value == null || row.Cells["project_items_qty"].Value.ToString() == null || row.Cells["project_items_qty"].Value.ToString() == "")
                {
                    return;
                }
                 
                if (row.Cells["project_items_components"].Value.ToString().ToLower() == "pump")
                {
                    Pump_Total_Qty += int.Parse(row.Cells["project_items_qty"].Value.ToString());
                }
            }

            //ECB To Controller Value AMP REQ.
            // This wrote into project_wiring_wire_amp - the WIRE AMP. column - because the
            // AMP REQ. column, though declared, was never added to the grid, so there was
            // nowhere else to put it. Spec 8.4 keeps them distinct: AMP REQ. is the computed
            // requirement (rows 1 and 7), WIRE AMP. is the rating of the wire actually
            // chosen. Now that the column exists, the value goes where the comment always
            // said it should.
            // Spec 8.4 decides which of the two implementations of this figure wins.
            // This one multiplied by Pump_Total_Qty - a count of "pump" rows in the item
            // table - but 8.4's input is number_of_pumps_in_set, and 5.1.4 lists
            // "NO. OF PUMP/SET" as the Client Needs field holding it. The two can disagree.
            // computeECBToController reads that field (with txt_FLA, which the lines above
            // have just set to fla_highest, satisfying 8.4's "base the calculation on the
            // largest FLA"), so defer to it rather than keep a second, divergent copy of
            // the formula writing to the same cell on a last-writer-wins basis.
            // Pump_Total_Qty is left computed above only because that loop also carries an
            // early return that guards this method; it no longer feeds the amp.
            // FINAL is where FLA comes from, so both computed rows are stale now - not
            // just row 1, which is all this used to refresh.
            if (chk_wiring.Checked)
                RefreshWiringAmpRequirements();

        }

        // Spec §5.1.4's "+add final pump ... inserted into the table as further pumps
        // without repeating the template" - a plain appended row, never touching any
        // other row (template-driven or otherwise). Skips adding if a row for this exact
        // model is already there, so re-picking the same pump in FINAL doesn't pile up
        // duplicate line items (mirrors SetFinalPumpData's own dedupe on the FINAL grid).
        // QTY defaults to 1 - the same starting point a manually-added item gets - and is
        // freely editable afterward like any other row.
        // template_id value AddFinalPumpToItemsGrid stamps its own rows with - see that
        // method for why.
        private const string FinalPumpTemplateId = "final_pump";

        private void AddFinalPumpToItemsGrid(string itemId, string model)
        {
            if (string.IsNullOrEmpty(model)) return;

            // dgv_project_items is normally bound to a DataTable (see the DataSource-as-
            // DataTable pattern used everywhere else in this file, e.g.
            // AddWiringRowsComponent) - DataGridView refuses Rows.Add()/Rows.Insert()
            // directly on a bound grid ("Rows cannot be programmatically added ... when
            // the control is data-bound"), so a new row has to go through the underlying
            // DataTable instead, using its actual column names ("components"/"model"/
            // "qty"), not the DataGridViewColumn names ("project_items_components" etc,
            // which only apply to Cells[] access on an unbound grid).
            if (dgv_project_items.DataSource is DataTable dataSource)
            {
                // A "PUMP" row identifies a pump slot regardless of where it came from -
                // a template's own child list always includes one (blank until filled),
                // and a row this method inserted fresh also gets "PUMP" as its
                // components text. Matching on that instead of template_id lets both
                // directions share one rule: already-filled with this exact model =
                // dedupe; blank = the template's own slot waiting to be filled; neither
                // found = fall through to inserting a brand-new row.
                // lastPumpRowIndex tracks the last PUMP-type row seen (the template's own
                // slot, whether blank or already filled, or a previously-added final pump)
                // so a genuinely new pick can be inserted right next to it instead of
                // always landing at the tail before wiring - keeps every pump clustered
                // together regardless of how many other template rows sit between the
                // first pump and wiring.
                DataRow blankTemplateSlot = null;
                int lastPumpRowIndex = -1;
                for (int i = 0; i < dataSource.Rows.Count; i++)
                {
                    DataRow row = dataSource.Rows[i];
                    if (!string.Equals(row["components"]?.ToString()?.Trim(), "PUMP", StringComparison.OrdinalIgnoreCase)) continue;

                    string existingModel = row["model"]?.ToString();
                    if (string.Equals(existingModel, model, StringComparison.OrdinalIgnoreCase)) return; // dedupe

                    if (blankTemplateSlot == null && string.IsNullOrWhiteSpace(existingModel)) blankTemplateSlot = row;

                    lastPumpRowIndex = i;
                }

                if (blankTemplateSlot != null)
                {
                    // Fill the template's own slot in place - same reference_code, same
                    // position, no insert/renumber needed.
                    blankTemplateSlot["item_id"] = itemId;
                    blankTemplateSlot["model"] = model;
                    return;
                }

                // Ordering rule: [template rows] -> [final pump rows] -> [wiring rows].
                // A pump added after wiring rows already exist (chk_wiring was checked
                // before FINAL was used, or a second pump is added after the first)
                // MUST be inserted before wiring, not appended after it.
                int wiringStartIndex = dataSource.Rows.Count;
                for (int i = 0; i < dataSource.Rows.Count; i++)
                {
                    if (string.Equals(dataSource.Rows[i]["template_id"]?.ToString(), "wiring", StringComparison.OrdinalIgnoreCase))
                    {
                        wiringStartIndex = i;
                        break;
                    }
                }

                // Cluster with the last existing PUMP row rather than always inserting at
                // the tail - if there's no PUMP row at all yet, fall back to the old
                // before-wiring position.
                int insertIndex = lastPumpRowIndex >= 0 ? lastPumpRowIndex + 1 : wiringStartIndex;

                // Top-level reference codes are plain integers ("1", "2", ...); children
                // hang off them as "N.1", "N.2" (see AddWiringRowsComponent/
                // AddChildNodesFromDb). The new pump becomes the next top-level number
                // after however many top-level rows already sit before the insertion
                // point, and every top-level number from there on shifts up by one so
                // reference_code stays sequential (children keep their own suffix). Since
                // insertIndex can now land mid-list (right after a clustered pump) rather
                // than only ever at wiringStartIndex, the renumber sweep runs to the end
                // of the grid, not just from wiringStartIndex.
                int TopLevelHead(string code, out string tail)
                {
                    int dot = code?.IndexOf('.') ?? -1;
                    tail = dot >= 0 ? code.Substring(dot) : string.Empty;
                    string head = dot >= 0 ? code.Substring(0, dot) : code;
                    return int.TryParse(head, out int n) ? n : 0;
                }

                int topLevelCountBeforeInsert = 0;
                for (int i = 0; i < insertIndex; i++)
                {
                    string code = dataSource.Rows[i]["reference_code"]?.ToString() ?? "";
                    if (!code.Contains(".")) topLevelCountBeforeInsert++;
                }
                int newTopLevelNumber = topLevelCountBeforeInsert + 1;

                for (int i = insertIndex; i < dataSource.Rows.Count; i++)
                {
                    var row = dataSource.Rows[i];
                    string code = row["reference_code"]?.ToString() ?? "";
                    int head = TopLevelHead(code, out string tail);
                    row["reference_code"] = (head + 1) + tail;
                }

                DataRow newDataRow = dataSource.NewRow();
                newDataRow["item_id"] = itemId;
                newDataRow["components"] = "PUMP";
                newDataRow["model"] = model;
                newDataRow["qty"] = "1";
                newDataRow["reference_code"] = newTopLevelNumber;
                newDataRow["template_id"] = FinalPumpTemplateId;
                dataSource.Rows.InsertAt(newDataRow, insertIndex);
                return;
            }

            // Unbound fallback - matches SetComponentDataUnbound/AddChildNodesFromDb's
            // own Cells[]-based row-add pattern elsewhere in this file. In practice
            // dgv_project_items is always bound by the time FINAL is used, so this just
            // appends (no template/wiring-position renumbering) rather than duplicating
            // that logic for a path that shouldn't actually run.
            foreach (DataGridViewRow row in dgv_project_items.Rows)
            {
                if (row.IsNewRow) continue;
                bool isPumpRow = string.Equals(row.Cells["project_items_components"].Value?.ToString(), "Pump", StringComparison.OrdinalIgnoreCase);
                bool sameModel = string.Equals(row.Cells["project_items_model"].Value?.ToString(), model, StringComparison.OrdinalIgnoreCase);
                if (isPumpRow && sameModel) return;
            }

            int rowIndex = dgv_project_items.Rows.Add();
            DataGridViewRow newRow = dgv_project_items.Rows[rowIndex];
            newRow.Cells["item_id"].Value = itemId;
            newRow.Cells["project_items_components"].Value = "PUMP";
            newRow.Cells["project_items_model"].Value = model;
            newRow.Cells["project_items_qty"].Value = "1";
        }

        // What the multi-select FINAL picker pre-checks/dedupes against - see the
        // final_item_id Designer comment for why a row reloaded from a saved project
        // won't appear here.
        public List<int> GetFinalItemIds()
        {
            var ids = new List<int>();

            foreach (DataGridViewRow row in dgv_final.Rows)
                if (int.TryParse(row.Cells["final_item_id"].Value?.ToString(), out int id))
                    ids.Add(id);

            return ids;
        }

        public DataGridView DgvProjectItems
        {
            get { return this.dgv_project_items; }
        }

        // Declare up front that this control is showing an EXISTING record, before any
        // data is pushed into it (2026-09-04).
        //
        // isViewProjectItem is what stops cb_template_project_SelectedIndexChanged from
        // clearing the items grid and re-applying the template (and, with it, re-adding
        // the wiring block) on a quote that already has its items. Until now the only
        // thing that set it was the END of SetFetchedItemData, which makes the protection
        // a RACE: ItemSetUC_Load is async, and the moment it finishes awaiting the
        // engineer/template fetches it assigns cmb_template_project.SelectedValue, which
        // fires that handler. Whether the flag is set in time depends purely on whether
        // the caller managed to call SetFetchedItemData during that await window.
        //
        // Sales happens to win that race; the Engineering page adds the control to its tab
        // (starting the load) and only calls SetFetchedItemData afterwards, so it is
        // relying on the same accident. Calling this first makes it deterministic.
        public void MarkAsExistingRecord()
        {
            isViewProjectItem = true;
        }

        // BOUND TO DATASOURCE
        public void SetComponentData(int index, string itemid, string itemName, string size, string model, string bomid)
        {
            if (dgv_project_items.DataSource == null)
            {
                SetComponentDataUnbound(index, itemid, itemName, size, model);
            }
            else
            {
                DataTable dt = (DataTable)dgv_project_items.DataSource;

                if (index >= 0 && index <= dt.Rows.Count)
                {
                    DataRow newRow = dt.NewRow();
                    newRow["item_id"] = itemid;
                    newRow["components"] = itemName;
                    newRow["model"] = model;
                    newRow["bom_id"] = bomid;

                    dt.Rows.InsertAt(newRow, index);

                    // Show available stock for the item just picked, before the user has
                    // even typed a QTY yet - same as Quick Quote's GetItemData.
                    RefreshStockIndicator(index);
                }
            }

        }


        public void SetComponentModelDataUnbound(int index, string itemid, string bomid, string model)
        {
            //dgv_project_items.Rows.Insert(index);
            DataGridViewRow newRow = dgv_project_items.Rows[index - 1];
            newRow.Cells["project_items_bom_id"].Value = bomid;
            newRow.Cells["item_id"].Value = itemid;
            newRow.Cells["project_items_model"].Value = model;
            // add styles soon

            RefreshStockIndicator(index - 1);
        }


        // NOT BOUND TO DATASOURCE
        public void SetComponentDataUnbound(int index, string itemid, string itemName, string size, string model)
        {

            dgv_project_items.Rows.Insert(index);

            //DataGridViewRow nRow = dgv_project_items.Rows[index - 1];
            //nRow.Cells["project_items_model"].Value = model;

            DataGridViewRow newRow = dgv_project_items.Rows[index];

            newRow.Cells["item_id"].Value = itemid;
            newRow.Cells["project_items_components"].Value = itemName;
            newRow.Cells["project_items_model"].Value = model;

            RefreshStockIndicator(index);

            DataGridViewCellStyle cellStyle = new DataGridViewCellStyle
            {
                Font = new Font(dgv_project_items.Font, FontStyle.Bold),
                BackColor = Color.LightGreen,
                Padding = new Padding(50, 0, 0, 0)
            };

            DataGridViewCellStyle cellStyle2 = new DataGridViewCellStyle
            {
                Font = new Font(dgv_project_items.Font, FontStyle.Bold),
                Alignment = DataGridViewContentAlignment.MiddleCenter
            };

            // Apply cell styles to specific columns
            newRow.Cells[7].Style = cellStyle;  // Style for itemName (Column 3)
            newRow.Cells[4].Style = cellStyle2; // Style for size (Column 4)
        }

        private DataTable stockProjectItemDataTable;

        public DataTable ItemList { get; set; } = new DataTable();
        public DataTable BomHead { get; set; } = new DataTable();
        public DataTable BomDetails { get; set; } = new DataTable();

        // for wiring soon
        private async void ItemSetUC_Load(object sender, EventArgs e)
        {

            stockProjectItemDataTable = Helpers.GetDataTableFromUnboundGrid(dgv_project_items);

            // This has to happen before any of the awaits below, not after all of them (as it
            // did before) - ClearProjectItemsDgv() doesn't actually use any of the awaited
            // data, but running it at the end of this async method left a window where a
            // brand-new tab's grid looked ready to use immediately, while this handler was
            // still resolving in the background. Adding an item during that window (very easy
            // to do on a fresh tab) got silently wiped out the moment this method finally
            // reached the clear call - dgv_project_items.DataSource got replaced with a fresh
            // empty clone - so the item vanished, the user re-added it, and every add attempt
            // computed its reference code from an apparently-empty grid, landing on "1" every
            // time instead of counting up.
            if (!isViewProjectItem)
                ClearProjectItemsDgv();

            // Load the engineer dropdown before the project-templates call below, which
            // returns early on failure/empty data. It used to sit after that check, so
            // any hiccup fetching templates (unrelated to engineers) silently skipped
            // this block entirely and left the ASSIGNED ENGR. dropdown with no items to
            // choose from - it wasn't a data/filtering problem, the code just never ran.
            var engineers = await EngineerService.GetEngineerList();
            cmb_assign_engineer_user_id.DataSource = engineers ?? new List<EngineerModel>();
            cmb_assign_engineer_user_id.DisplayMember = nameof(EngineerModel.FullName);
            cmb_assign_engineer_user_id.ValueMember = nameof(EngineerModel.Id);

            // Restore the saved engineer instead of blanking the box (fixed 2026-09-04).
            //
            // This load handler is async and runs AFTER SetContentsPanelData has already
            // bound the content row, so the unconditional "SelectedIndex = -1" that used
            // to sit here threw away the assign_engineer_user_id that had just been
            // restored. It was worse than a display bug: GetProjectContentsData sends
            // SelectedIdOrZero(cmb_assign_engineer_user_id), so a blank box saved as 0 and
            // every re-save of a reopened quote silently WIPED the assigned engineer.
            // 9 of 11 content rows are already sitting at 0 because of this, 5 of them
            // with is_wiring = 1 - rows that are supposed to name an engineer.
            //
            // cmb_template_project two blocks down never had this problem because it
            // stashes its fetched id in txt_template_id and re-applies it after binding.
            // _pendingAssignedEngineerId is the same trick for this combo; it just never
            // got one. Still clears to -1 for a genuinely new item set, which is where
            // that line was actually right.
            if (_pendingAssignedEngineerId > 0)
                cmb_assign_engineer_user_id.SelectedValue = _pendingAssignedEngineerId;
            else
                cmb_assign_engineer_user_id.SelectedIndex = -1;

            var dt = await ProjectTemplatesService.GetProjectTemplates();

            if (dt == null || dt.SalesProjectTemplate == null) return;

            DataTable listOfTemplates = JsonHelper.ToDataTable(dt.SalesProjectTemplate);
            DataTable templates = JsonHelper.ToDataTable(dt.sales_project_template_child);

            var itemData = await ItemService.GetItem();
            var bomData = await ProjectService.GetBom();

            // Both return null when the API call fails - same guard Quotation.fetchItemData
            // already had, which this call site was missing. The template dropdown above is
            // populated by this point and stays usable; only the item/BOM lookups are lost.
            if (itemData == null || bomData == null) return;

            ItemList = JsonHelper.ToDataTable(itemData.items);
            BomHead = JsonHelper.ToDataTable(bomData.bom_head);
            BomDetails = JsonHelper.ToDataTable(bomData.bom_details);

            // --- ADD initial 0/default row ---
            DataRow defaultRow = listOfTemplates.NewRow();
            defaultRow["template_id"] = 0; // or DBNull.Value
            defaultRow["template_name"] = "-- No Template --";
            listOfTemplates.Rows.InsertAt(defaultRow, 0); // Insert at index 0

            cmb_template_project.DataSource = listOfTemplates;
            cmb_template_project.DisplayMember = "template_name";
            cmb_template_project.ValueMember = "template_id";

            cmb_template_project.SelectedIndexChanged += cb_template_project_SelectedIndexChanged;

            var dtProjectTemplates = await ProjectService.GetProjects();

            if (txt_template_id.Text != null && txt_template_id.Text != "")
                cmb_template_project.SelectedValue = txt_template_id.Text;

        }

        private void ClearProjectItemsDgv()
        {
            if (dgv_project_items.DataSource is DataTable dtpi)
            {
                dtpi.Rows.Clear();
            }
            else if (dgv_project_items.DataSource is DataView dv)
            {
                dv.Table.Clear(); // clear the underlying DataTable
            }
            else
            {
                dgv_project_items.Rows.Clear(); // fallback for unbound
            }

            dgv_project_items.DataSource = stockProjectItemDataTable.Clone();
        }

        bool isLoadingTemplate = false;
        int LastRefInt = 0;

        private async void cb_template_project_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (isViewProjectItem)
                return;

            if (isLoadingTemplate)
                return;

            string lastRef = "";

            // Capture any pumps FINAL already added before ClearProjectItemsDgv() wipes
            // the grid below, so picking a template (or switching back to "-- No
            // Template --") never destroys them. The template's own PUMP row gets filled
            // from the first one instead of staying blank (see the loop below); anything
            // left over is re-added afterward in the finally block, the same way FINAL
            // itself adds a pump (right before wiring).
            var existingFinalPumps = new List<(string ItemId, string Model)>();
            if (dgv_project_items.DataSource is DataTable existingItemsDs)
            {
                foreach (DataRow existingRow in existingItemsDs.Rows)
                {
                    string existingComponents = existingRow["components"]?.ToString()?.Trim() ?? "";
                    string existingModel = existingRow["model"]?.ToString();
                    if (string.Equals(existingComponents, "PUMP", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(existingModel))
                    {
                        existingFinalPumps.Add((existingRow["item_id"]?.ToString(), existingModel));
                    }
                }
            }
            int usedFinalPumps = 0;

            try
            {
                isLoadingTemplate = true;


                if (!isViewProjectItem)
                     ClearProjectItemsDgv();

                if (cmb_template_project.SelectedValue == null || cmb_template_project.SelectedValue == DBNull.Value)
                    return;

                string templateId = cmb_template_project.SelectedValue.ToString();
                if (templateId == "0")
                    return;

                // Get templates
                var dt = await ProjectTemplatesService.GetProjectTemplates();
                DataTable templatesChild = JsonHelper.ToDataTable(dt.sales_project_template_child);

                // Filter children
                var childRows = templatesChild.AsEnumerable()
                    .Where(r => r.Field<int>("ParentId").ToString() == templateId)
                    .ToList();

                if (childRows.Count == 0)
                    return;

                DataTable dataSource = dgv_project_items.DataSource as DataTable;



                // Stack for hierarchical numbering
                List<int> levelCounters = new List<int>();

                foreach (var row in childRows)
                {
                    DataRow newRow = dataSource.NewRow();

                    int level = row.Field<int?>("Level") ?? 0;

                    // Expand counters list to match this level
                    while (levelCounters.Count <= level)
                        levelCounters.Add(0);

                    // Reset deeper levels when moving up
                    for (int i = level + 1; i < levelCounters.Count; i++)
                        levelCounters[i] = 0;

                    // Increment this level counter
                    levelCounters[level]++;

                    // Create hierarchical number like 1.2.3
                    string refCode = string.Join(".", levelCounters.Where(v => v > 0));

                    // Level is 1-based (top-level rows are stored as Level=1, not 0) -
                    // same convention already relied on elsewhere in this file (see the
                    // BOM-insert path a few hundred lines down: "((int)row["level"] - 1) *
                    // 4"). Using the raw Level here gave every row, top-level included, a
                    // uniform +4 space offset - visually harmless-looking for deep rows
                    // (the 4-space gaps between levels were still correct) but wrong for
                    // level 1, which should have no indent at all. Subtract 1 to match.
                    string indent = new string(' ', Math.Max(0, level - 1) * 4);
                    string componentText = (row["Components"]?.ToString() ?? "").TrimStart();

                    newRow["components"] = indent + componentText;
                    newRow["item_id"] = row["ItemId"];
                    newRow["reference_code"] = refCode;
                    newRow["template_id"] = templateId;

                    // Fill the template's own PUMP slot from an already-added FINAL pump
                    // instead of leaving it blank, if one was captured above - this is
                    // what keeps a final pump added BEFORE the template from turning into
                    // a duplicate generic row once the template comes in.
                    if (usedFinalPumps < existingFinalPumps.Count &&
                        string.Equals(componentText.Trim(), "PUMP", StringComparison.OrdinalIgnoreCase))
                    {
                        newRow["item_id"] = existingFinalPumps[usedFinalPumps].ItemId;
                        newRow["model"] = existingFinalPumps[usedFinalPumps].Model;
                        usedFinalPumps++;
                    }

                    dataSource.Rows.Add(newRow);

                    lastRef = refCode.Split('.')[0];
                }
            }
            finally
            {
                if (lastRef != "")
                    LastRefInt = int.Parse(lastRef);
                else
                    LastRefInt = 0;

                // Any captured final pumps beyond the one that filled the template's own
                // PUMP slot (or all of them, if there was no PUMP slot / no template was
                // actually applied) get re-added exactly the way FINAL itself adds a pump
                // - right before wiring, via AddFinalPumpToItemsGrid's own dedupe/insert/
                // renumber logic.
                for (int i = usedFinalPumps; i < existingFinalPumps.Count; i++)
                {
                    AddFinalPumpToItemsGrid(existingFinalPumps[i].ItemId, existingFinalPumps[i].Model);
                }

                if (chk_wiring.Checked)
                {
                    // Routed through AddWiringRowsComponentProject (fixed 2026-09-04).
                    // This used to call AddWiringRowsComponent directly, which is the raw
                    // inserter with NO duplicate protection - the guard lives one level up
                    // in AddWiringRowsComponentProject. So every time this template path
                    // ran against a grid that already held a wiring block, it appended a
                    // second one. Its sibling trigger (chk_wiring's own CheckedChanged)
                    // always went through the guarded wrapper; only this call site skipped
                    // it. The wrapper computes the same GetMaxTopLevelReferenceCode() this
                    // did, so behaviour is unchanged when there is genuinely nothing there.
                    AddWiringRowsComponentProject();
                }

                isLoadingTemplate = false;
            }

        }

        private void AddWiringRowsComponent(int Reference)
        {

            int NewReference = Reference + 1;

            DataTable dataSource = dgv_project_items.DataSource as DataTable;

            if (dataSource != null)
            {
                DataRow newRow = dataSource.NewRow();

                newRow["components"] = "WIRING MATERIALS";
                newRow["item_id"] = 0;
                newRow["reference_code"] = NewReference;
                newRow["template_id"] = "wiring";

                dataSource.Rows.Add(newRow);

                DataRow newRow2 = dataSource.NewRow();

                newRow2["components"] = "    CTL-MOTOR";
                newRow2["item_id"] = 0;
                newRow2["reference_code"] = NewReference.ToString() + ".1";
                newRow2["template_id"] = "wiring";

                dataSource.Rows.Add(newRow2);


                DataRow newRow3 = dataSource.NewRow();

                newRow3["components"] = "    CTL-ECB";
                newRow3["item_id"] = 0;
                newRow3["reference_code"] = NewReference.ToString() + ".2";
                newRow3["template_id"] = "wiring";

                dataSource.Rows.Add(newRow3);

                DataRow newRow4 = dataSource.NewRow();

                newRow4["components"] = "WIRING LABOR";
                newRow4["item_id"] = 0;
                newRow4["reference_code"] = NewReference + 1;
                newRow4["template_id"] = "wiring";

                dataSource.Rows.Add(newRow4);
            }
        }

        // The four component rows AddWiringRowsComponent inserts as one block. Used to
        // recognise a wiring row after a save/reload, when the in-session "wiring"
        // template_id marker is gone - see IsWiringComponentRow.
        private static readonly string[] WiringComponentNames =
        {
            "WIRING MATERIALS",
            "CTL-MOTOR",
            "CTL-ECB",
            "WIRING LABOR",
        };

        // Is this row part of the wiring block?
        //
        // AddWiringRowsComponent stamps template_id = "wiring" on the rows it adds, and
        // both the duplicate guard and the removal below used to test ONLY that. But
        // template_id is an integer column in tbl_trans_sales_project_items (its live
        // values are 0, 1 and 2 - never the string "wiring"), so the marker exists only
        // for the session that created the rows and is gone the moment the quote is
        // saved and reloaded. On an existing quote that broke both directions:
        //
        //   * ticking WIRING added a SECOND full block, because the guard could not see
        //     the block already loaded from the database
        //   * unticking it removed nothing at all, for the same reason
        //
        // Falling back to the component name fixes both without a schema change, since
        // the names are already persisted. Children are stored indented ("    CTL-MOTOR"),
        // hence the trim.
        private static bool IsWiringComponentRow(string templateId, string components)
        {
            if (string.Equals(templateId, "wiring", StringComparison.OrdinalIgnoreCase))
                return true;

            string name = (components ?? string.Empty).Trim();
            if (name.Length == 0)
                return false;

            foreach (string wiringName in WiringComponentNames)
            {
                if (string.Equals(name, wiringName, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        private void RemoveWiringRowsComponentByBaseReference()
        {
            DataTable dataSource = dgv_project_items.DataSource as DataTable;
            if (dataSource == null)
                return;

            var rowsToRemove = dataSource.AsEnumerable()
                .Where(r => IsWiringComponentRow(
                    r.Table.Columns.Contains("template_id") ? r["template_id"]?.ToString() : null,
                    r.Table.Columns.Contains("components") ? r["components"]?.ToString() : null))
                .ToList();

            foreach (DataRow row in rowsToRemove)
                dataSource.Rows.Remove(row);
        }

        private void textBox64_MouseClick(object sender, MouseEventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
            // Opens the template selection modal (Button_ClickedUC in Quotation.cs) - an edit
            // action, so it shouldn't be reachable while this tab is locked (view mode).
            if (!_isEditable)
                return;

            ButtonClicked?.Invoke(this, EventArgs.Empty);
        }

        int index { get; set; }
        public Action<int, DataGridView> HandleItemSelectionClick { get; internal set; }

        public int GetIndex()
        {
            return index;
        }

        // Locks/unlocks every textbox, checkbox, combobox, and gridview in this tab (Content,
        // Advanced Conditions, Items grid, Wiring grid, Final grid, notes, template/engineer
        // pickers - everything). Project Quotation had no read-only state at all before this,
        // so a tab's details could be edited freely even while just viewing a saved project,
        // before clicking Edit.
        //
        // DataGridView.ReadOnly (set by Helpers.SetControlsEditable below) only blocks typing
        // directly into a cell - it does nothing to stop a CellClick handler from firing, and
        // this grid's component/model/image pickers are all launched from CellClick
        // (dgv_project_items_CellClick), not from in-cell editing. _isEditable is what those
        // handlers actually check before opening a picker or assigning anything.
        private bool _isEditable = true;
        public void SetEditable(bool editable)
        {
            _isEditable = editable;
            Helpers.SetControlsEditable(this, editable);

            // SIZE UP and FINAL are never typed into - both are filled by their picker
            // (SizeUpPickerModal, reached by clicking the grid), and their rows carry an
            // item_id that free text could not supply. The designer marks both grids
            // ReadOnly, but SetControlsEditable walks every DataGridView it finds and
            // blanket-assigns ReadOnly = !editable, which quietly undid that the moment a
            // tab went editable - in Sales on Edit, and in the Engineering app always,
            // since it calls SetEditable(true) on load. Re-lock them here, after the
            // helper has had its say.
            if (dgv_size_up != null) dgv_size_up.ReadOnly = true;
            if (dgv_final != null) dgv_final.ReadOnly = true;
        }

        // MULTIPLIER (project_items_multiplier) is a combo box column bound to a fixed list
        // of standard multipliers. Any time a row's stored multiplier value isn't in that
        // list - a legacy value, one entered before the standard list changed, or anything
        // else that doesn't match exactly - WinForms throws "DataGridViewComboBoxCell value
        // is not valid" while rendering, and with no DataError handler that showed its own
        // ugly default dialog and left the grid in whatever half-loaded state it was in.
        // Swallowing it here (as the dialog itself suggests) lets the grid keep rendering;
        // the mismatched cell just shows blank instead of taking the app down.
        private void dgv_project_items_DataError(object sender, DataGridViewDataErrorEventArgs e)
        {
            e.ThrowException = false;
            e.Cancel = true;
        }

        // ---- Stock tracking (INV. column) ----
        //
        // Mirrors Quick Quote's quick_inv_stock setup in Quotation.cs, adapted to this
        // grid's own column names (item_id is the same, but qty is "project_items_qty"
        // and the per-line saved id is "project_items_id"/"items_id" instead of
        // "quick_id"). Kept as its own small cache here rather than sharing Quotation.cs's
        // _availableStockByItemId - Quotation.cs invalidates entries there and calls
        // ClearStockCache below on this tab too whenever a reservation it applied would
        // have changed the number, so the two stay in sync without this tab needing direct
        // access to Quotation.cs's internals.
        private readonly Dictionary<int, AvailableStockModel> _availableStockByItemId = new Dictionary<int, AvailableStockModel>();

        public void ClearStockCache(int itemId)
        {
            _availableStockByItemId.Remove(itemId);
        }

        // See Quotation.cs's RefreshAllStockIndicators for why this prefetches once via
        // GetAllAvailableStock() instead of letting every row's RefreshStockIndicator fire
        // its own request - same fix, same reasoning (one request instead of one per
        // distinct item, and silent so a failed prefetch doesn't pop an error dialog).
        //
        // _refreshingStockIndicators guards against overlapping calls the same way
        // Quotation.cs's own copy does - this is called from several places as
        // fire-and-forget, and each row's RefreshStockIndicator still makes its own
        // (unbatched) reservation lookup, so multiple calls in flight at once meant that
        // many times the concurrent reservation requests, which is what was flooding
        // /inventory/item_stocks/reservations and popping "An error occurred while sending
        // the request" repeatedly.
        private bool _refreshingStockIndicators = false;

        public async void RefreshAllStockIndicators()
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

                for (int i = 0; i < dgv_project_items.Rows.Count; i++)
                {
                    if (dgv_project_items.Rows[i].IsNewRow) continue;
                    // Sequential, not fire-and-forget - so the guard above stays true for
                    // as long as the actual per-row reservation lookups are still in flight,
                    // not just until this loop finishes kicking them off.
                    await RefreshStockIndicator(i);
                }
            }
            finally
            {
                _refreshingStockIndicators = false;
            }
        }

        // Task, not void - RefreshAllStockIndicators above awaits this per row. Existing
        // fire-and-forget call sites elsewhere still compile unchanged; a discarded Task
        // behaves the same as the old async void did for them.
        private async Task RefreshStockIndicator(int rowIndex)
        {
            if (rowIndex < 0 || rowIndex >= dgv_project_items.Rows.Count || dgv_project_items.Rows[rowIndex].IsNewRow) return;
            if (!dgv_project_items.Columns.Contains("project_inv_stock") || !dgv_project_items.Columns.Contains("item_id")) return;

            var itemIdValue = dgv_project_items.Rows[rowIndex].Cells["item_id"].Value;
            if (!int.TryParse(itemIdValue?.ToString(), out int itemId) || itemId <= 0) return;

            int.TryParse(dgv_project_items.Rows[rowIndex].Cells["project_items_id"].Value?.ToString(), out int lineId);

            try
            {
                if (!_availableStockByItemId.TryGetValue(itemId, out AvailableStockModel stock))
                {
                    stock = await ItemStockCheckService.GetAvailableStock(itemId);
                    _availableStockByItemId[itemId] = stock;
                }

                int ownReservedQty = 0;
                if (lineId > 0)
                {
                    // silent: true - see ItemStockCheckService.GetReservation's remarks;
                    // this background refresh already means to swallow a failure here, not
                    // pop a MessageBox per row.
                    var reservation = await ItemStockCheckService.GetReservation(lineId, "sales_project_item", silent: true);
                    if (reservation != null) ownReservedQty = reservation.qty;
                }

                if (rowIndex < dgv_project_items.Rows.Count && !dgv_project_items.Rows[rowIndex].IsNewRow)
                {
                    dgv_project_items.Rows[rowIndex].Cells["project_inv_stock"].Value = stock.available + ownReservedQty;
                    dgv_project_items.InvalidateRow(rowIndex);
                }
            }
            catch (Exception)
            {
                // Convenience indicator, not part of the save path - swallow rather than
                // pop a MessageBox for every row on a flaky network.
            }
        }

        // Icon-only, same rule as Quick Quote's INV. column - blank unless this row is
        // actually short.
        private void dgv_project_items_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= dgv_project_items.Rows.Count) return;
            if (dgv_project_items.Columns[e.ColumnIndex].Name != "project_inv_stock") return;
            if (dgv_project_items.Rows[e.RowIndex].IsNewRow) return;

            if (!int.TryParse(e.Value?.ToString(), out int available)) return;

            int required = 0;
            if (dgv_project_items.Columns.Contains("project_items_qty"))
            {
                int.TryParse(dgv_project_items.Rows[e.RowIndex].Cells["project_items_qty"].Value?.ToString(), out required);
            }

            bool isShort = required > 0 && available < required;
            e.Value = isShort ? "\U0001F6A9" : "";
            e.CellStyle.ForeColor = Color.Red;
            e.CellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            e.FormattingApplied = true;
        }

        // Right-clicking the QTY column header opens the stock checker for this tab,
        // same as Quick Quote's equivalent on dgv_quick_quote_details.
        private void dgv_project_items_CellMouseDown(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.RowIndex != -1 || e.Button != MouseButtons.Right) return;
            if (dgv_project_items.Columns[e.ColumnIndex].Name != "project_items_qty") return;
            if (!_isEditable) return;

            CellClickedStock?.Invoke(this, EventArgs.Empty);
        }

        // Hard guard on top of reference_code.ReadOnly (Designer.cs) - cancels editing at
        // the moment it's about to start, for this column specifically. CODE is an
        // auto-generated hierarchy/tracking id, never meant to be hand-edited.
        private void dgv_project_items_CellBeginEdit(object sender, DataGridViewCellCancelEventArgs e)
        {
            if (e.ColumnIndex >= 0 && dgv_project_items.Columns[e.ColumnIndex].Name == "reference_code")
            {
                e.Cancel = true;
            }
        }

        private void dgv_project_items_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0)
                return;

            // Component/Model/Image picking all launch from this click handler, not from
            // in-cell editing, so DataGridView.ReadOnly alone doesn't stop any of them while
            // the tab is locked (view mode, before New/Edit).
            if (!_isEditable)
                return;

            index = e.RowIndex;
            if (dgv_project_items.Columns[e.ColumnIndex].Name == "project_items_components")
            {
                CellClicked?.Invoke(this, EventArgs.Empty);
            }

            // INV. column - always opens the stock checker on click, flagged or not,
            // same as Quick Quote's equivalent column.
            if (dgv_project_items.Columns[e.ColumnIndex].Name == "project_inv_stock")
            {
                CellClickedStock?.Invoke(this, EventArgs.Empty);
            }

            if (dgv_project_items.Columns[e.ColumnIndex].Name == "project_items_model")
            {

                if (dgv_project_items.Rows[index].Cells["project_items_template_id"].Value.ToString() == "" ||
                dgv_project_items.Rows[index].Cells["project_items_template_id"].Value == DBNull.Value ||
                dgv_project_items.Rows[index].Cells["project_items_template_id"].Value.ToString() == "0")

                    CellClickedModel?.Invoke(index, EventArgs.Empty);

                else if (dgv_project_items.Rows[index].Cells["project_items_template_id"].Value.ToString() == "wiring")

                    MessageBox.Show("Wiring don't have models");

                else
                    AssignModel(index, dgv_project_items);

            }
            if (dgv_project_items.Columns[e.ColumnIndex].Name == "project_items_images")
            {
                // Was checking for a column named "quick_images" (that's Quick Quote's
                // column, not this grid's - this grid's is "project_items_images"), and
                // reading a "quick_id" cell that doesn't exist on this grid at all, so this
                // never actually fired. Project items don't have a saved quick-id concept;
                // rows are identified by their own grid position instead (see
                // HandleItemImageSelectionClick).
                var row = dgv_project_items.Rows[e.RowIndex];
                var cellItemId = row.Cells["item_id"].Value?.ToString();

                if (int.TryParse(cellItemId, out int itemId) && itemId != 0)
                {
                    HandleItemImageSelectionClick(e.RowIndex, itemId);
                }
                else
                {
                    MessageBox.Show("Select an item for this row first.");
                }
            }


            //ComputeByReferenceHierarchy(dgv_project_items);
            //ComputeReferenceNonHierarchy(dgv_project_items);
            ProjectComputationLoop();
        }

        public DataTable ImageList { get; set; } = new DataTable();
        // Selected images picked via the IMAGES column, keyed by the grid row index they
        // belong to (not by items_id - a brand-new, unsaved row has items_id == 0 and would
        // collide with every other new row). Same fix already applied to Quick Quote's grid
        // for the same reason: a single shared field meant every row ended up with a copy of
        // whichever item's images were picked last.
        private Dictionary<int, List<Dictionary<string, object>>> SelectedImagesByRow { get; set; } = new Dictionary<int, List<Dictionary<string, object>>>();

        private void HandleItemImageSelectionClick(int rowIndex, int itemId)
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

            // ItemImagesModal only needs this to know which checkboxes should start
            // checked - build it from whatever's currently recorded for this row instead
            // of filtering selectedImageList (which has no row-index concept).
            DataTable filteredSelectedImages = BuildSelectedImagesTableForRow(rowIndex);

            ItemImagesModal itemImageModal = new ItemImagesModal(itemName, filteredItems, filteredImages, filteredSelectedImages);
            DialogResult r = itemImageModal.ShowDialog();

            if (r == DialogResult.OK)
            {
                SelectedImagesByRow[rowIndex] = itemImageModal.SelectedImages;
                int selectedImageCount = itemImageModal.SelectedImages.Count();
                MessageBox.Show($"{selectedImageCount} images selected.");
                LoadProjectImageCounts();
            }
        }

        private DataTable BuildSelectedImagesTableForRow(int rowIndex)
        {
            DataTable table = new DataTable();
            table.Columns.Add("image_id", typeof(object));
            table.Columns.Add("is_selected", typeof(object));

            if (SelectedImagesByRow.TryGetValue(rowIndex, out var images))
            {
                foreach (var img in images)
                {
                    DataRow row = table.NewRow();
                    row["image_id"] = img.TryGetValue("image_id", out var imgId) ? imgId : DBNull.Value;
                    row["is_selected"] = img.TryGetValue("is_selected", out var sel) ? sel : DBNull.Value;
                    table.Rows.Add(row);
                }
            }

            return table;
        }

        // Shows a "SELECTED: n" count on the IMAGES column per row, same UX as Quick
        // Quote's LoadQuickImageCounts. The column is unbound (see Designer), so this needs
        // re-running any time the grid gets a fresh DataSource (SetFetchedItemData) since
        // rebinding wipes unbound cell values.
        private void LoadProjectImageCounts()
        {
            if (!dgv_project_items.Columns.Contains("project_items_images"))
                return;

            for (int rowIdx = 0; rowIdx < dgv_project_items.Rows.Count; rowIdx++)
            {
                if (dgv_project_items.Rows[rowIdx].IsNewRow)
                    continue;

                int count = SelectedImagesByRow.TryGetValue(rowIdx, out var images) ? images.Count : 0;
                dgv_project_items.Rows[rowIdx].Cells["project_items_images"].Value =
                    count > 0 ? $"SELECTED: {count}" : string.Empty;
            }
        }

        private void AddModel(DataGridView dgv, int rowIndex, bool isBom, int BomId, int ItemId, string referenceCode, int templateId = 0)
        {
            decimal unitPrice = 0.00m;
            int Qty = 0; 

            if (rowIndex >= 0)
            {
                DataGridViewRow DataGridRow = dgv.Rows[rowIndex];

                var cellValue = DataGridRow.Cells["project_items_unit_price"].Value;
                var cellQty = DataGridRow.Cells["project_items_qty"].Value;

                if (cellValue != null && decimal.TryParse(cellValue.ToString(), out decimal parsedValue))
                {
                    unitPrice = parsedValue;
                }

                if (cellQty != null && int.TryParse(cellQty.ToString(), out int parsedQty))
                {
                    Qty = parsedQty;
                }
            }

            DeleteRowsByReferenceCode(rowIndex, dgv);

            DataRow row = ItemList.AsEnumerable()
                .FirstOrDefault(r => r.Field<int>("id") == ItemId);

            int level = referenceCode.Count(f => f == '.');
            string indent = new string(' ', level * 4);

            DataTable dataSource = dgv.DataSource as DataTable;

            if (dataSource == null) return;

            DataRow projectItem = dataSource.NewRow();
            projectItem["item_id"] = ItemId;
            projectItem["components"] = indent + row["item_name"];
            projectItem["reference_code"] = referenceCode;
            projectItem["model"] = row["item_model"];
            projectItem["template_id"] = templateId;
            projectItem["unit_price"] = unitPrice;
            projectItem["qty"] = Qty;

            dataSource.Rows.InsertAt(projectItem, rowIndex);
        }

        private void AssignModel(int index, DataGridView dgv)
        {
            // Reverts 76bd5b5's block-and-redirect-to-FINAL behavior for this grid
            // entirely (confirmed wrong for Project Quotation, including a template's own
            // PUMP placeholder - not just a directly-added PUMP line item). A PUMP row
            // here picks its model the same way any other component does; SIZE UP/FINAL
            // is a Quick Quote mechanism this grid never goes through.
            string components = dgv.Rows[index].Cells["project_items_components"].Value?.ToString()?.Trim();
            bool isPumpRow = string.Equals(components, "PUMP", StringComparison.OrdinalIgnoreCase);

            string Id = dgv.Rows[index].Cells["item_id"].Value.ToString();

            if (string.IsNullOrWhiteSpace(Id) || Id == "0")
            {
                if (isPumpRow)
                {
                    // A blank PUMP slot has no item_name_id of its own for ModelModal to
                    // scope from (its lookup finds nothing against id "0" and falls back to
                    // the entire unfiltered catalog) - resolve any existing item named
                    // "PUMP" purely as an anchor so ModelModal's own item_name_id lookup
                    // scopes the list to pumps, same mechanism every already-filled
                    // component already relies on. The anchor is never itself selected;
                    // GetItemId() below still returns whatever the user actually picks.
                    DataRow pumpAnchor = ItemList.AsEnumerable()
                        .FirstOrDefault(row => string.Equals(row["item_name"]?.ToString(), "PUMP", StringComparison.OrdinalIgnoreCase));

                    if (pumpAnchor == null)
                    {
                        MessageBox.Show(
                            "No pump items are set up yet, so a model can't be picked here.",
                            "No Pump Items Found", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    Id = pumpAnchor["id"].ToString();
                }
                else
                {
                    // Same guard as Quotation.cs's HandleModelSelectionClick - a row with no
                    // component picked yet has item_id "0", and ModelModal has no way to scope
                    // its list from that. Block it here instead of opening the modal.
                    MessageBox.Show(
                        "It doesn't have a component, that's why it can't select any models. Please select a component first.",
                        "No Component Selected", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            }

            ModelModal createModal = new ModelModal(ItemList, BomHead, BomDetails, Id);
            DialogResult result = createModal.ShowDialog();

            string referenceCode = dgv.Rows[index].Cells["reference_code"].Value.ToString();

            if (result == DialogResult.OK)
            {
                bool isBom = createModal.IsBom();
                int BomId = createModal.GetBomId();
                int ItemId = createModal.GetItemId();

                DataTable SelectedItem = ItemList.AsEnumerable()
                    .Where(row => row.Field<int>("id") == createModal.GetItemId())
                    .CopyToDataTable();

                int templateId = int.Parse(dgv.Rows[index].Cells["project_items_template_id"].Value.ToString());
                int itemIdTemplate = int.Parse(dgv  .Rows[index].Cells["item_id"].Value.ToString());

                if (templateId != 0)
                    ValidateAndApplyTemplateBOM(index, dgv, ItemId, referenceCode, templateId, BomId, isBom, itemIdTemplate);
                else
                {
                    AddModel(dgv, index, isBom, BomId, ItemId, referenceCode);
                }
            }
        }

        private void ValidateAndApplyTemplateBOM(int rowIndex, DataGridView dgv, int itemId, string parentReferenceCode, int templateId, int BomId, bool isBom,int itemIdTemplate)
        {
            if (!(dgv.DataSource is DataTable projectTable))
                return;

            DataTable templateChildren = GetTemplateChildren(parentReferenceCode);

            DataTable tempCompared = new DataTable();

            tempCompared.Columns.Add("item_id", typeof(int));
            tempCompared.Columns.Add("item_name", typeof(string));
            tempCompared.Columns.Add("level", typeof(int));
            tempCompared.Columns.Add("parent_item_id", typeof(int));
            tempCompared.Columns.Add("reference_code", typeof(string));
            tempCompared.Columns.Add("model", typeof(string));
            tempCompared.Columns.Add("qty", typeof(int));
            tempCompared.Columns.Add("man_days", typeof(decimal));
            tempCompared.Columns.Add("labor_cost", typeof(decimal));
            tempCompared.Columns.Add("unit_price", typeof(decimal));

            var result = GetRecursiveBOM(itemId, tempCompared, templateChildren, BomId, itemIdTemplate);

            tempCompared = result.tempTable;

            //fix the Reference
            GenerateReferenceCode(tempCompared, parentReferenceCode);


            DataTable ComparedData = tempCompared;


            if (ComparedData == null || ComparedData.Rows.Count == 0)
            {
                AddModel(dgv, index, isBom, BomId, itemId, parentReferenceCode);

                return;
             }

            DeleteRowsByReferenceCode(rowIndex, dgv);

            foreach (DataRow row in ComparedData.Rows)
            {

                DataTable dataSource = dgv.DataSource as DataTable;
                if (dataSource == null) return;

                string spaceLevel = new string(' ', ((int)row["level"] - 1) * 4);

                DataRow projectItem = dataSource.NewRow();
                projectItem["item_id"] = row["item_id"];
                projectItem["components"] = spaceLevel + row["item_name"];
                projectItem["reference_code"] = row["reference_code"];
                projectItem["model"] = row["model"];
                projectItem["template_id"] = templateId;
                projectItem["man_days"] = row["man_days"];
                projectItem["labor_rate"] = row["labor_cost"];
                projectItem["qty"] = row["qty"];
                projectItem["unit_price"] = row["unit_price"];

                dataSource.Rows.InsertAt(projectItem, rowIndex);

                rowIndex++;
            }
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

        // Deleting a row (select via row header, press Delete - AllowUserToDeleteRows is
        // on) used to just remove that single bound row, leaving gaps in reference_code
        // and orphaned children behind if the deleted row was a parent. Same fix as
        // Quotation.cs's dgv_quick_quote_details: cascade-delete the row's whole subtree,
        // then renumber everything so the codes stay gapless and hierarchical.
        private void dgv_project_items_UserDeletingRow(object sender, DataGridViewRowCancelEventArgs e)
        {
            e.Cancel = true;

            if (e.Row.IsNewRow) return;

            string referenceCode = e.Row.Cells["reference_code"].Value?.ToString();
            if (string.IsNullOrWhiteSpace(referenceCode)) return;

            DeleteRowsByReferenceCode(e.Row.Index, dgv_project_items);
            RenumberReferenceCodes(dgv_project_items);
        }

        // Walks the grid's rows in their current display order and rebuilds every
        // reference_code from scratch so numbering stays gapless after a delete -
        // top-level items are renumbered 1, 2, 3... in order, and every descendant keeps
        // its original sub-level suffix but adopts its (possibly renumbered) parent's new
        // top-level number, so e.g. "3.3.1" becomes "2.3.1" if the row that used to be
        // "3" is now "2".
        private void RenumberReferenceCodes(DataGridView dgv)
        {
            if (!(dgv.DataSource is DataTable dataSource) || !dataSource.Columns.Contains("reference_code"))
                return;

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
        }

        private void GenerateReferenceCode(DataTable tempBOM, string startReference)
        {
            // Parse starting reference into level counters
            Dictionary<int, int> levelCounter = new Dictionary<int, int>();

            string[] parts = startReference.Split('.');
            for (int i = 0; i < parts.Length; i++)
                levelCounter[i + 1] = int.Parse(parts[i]);

            int maxInitialLevel = parts.Length;
            bool firstRow = true;

            foreach (DataRow row in tempBOM.Rows)
            {
                int level = row.Field<int>("level");

                if (firstRow)
                {
                    // First row uses the starting reference exactly
                    row["reference_code"] = startReference;
                    firstRow = false;
                    continue;
                }

                // When level stays inside or under initial depth
                if (!levelCounter.ContainsKey(level))
                    levelCounter[level] = 1;
                else
                    levelCounter[level]++;

                // Clean deeper levels when going up
                var removeKeys = levelCounter.Keys.Where(k => k > level).ToList();
                foreach (var key in removeKeys)
                    levelCounter.Remove(key);

                // Build the final reference code
                string refCode = string.Join(".",
                    levelCounter.Where(x => x.Key <= level)
                                .OrderBy(x => x.Key)
                                .Select(x => x.Value)
                );

                row["reference_code"] = refCode;
            }
        }

        private (DataTable tempTable, decimal subTotal) GetRecursiveBOM(int itemId, DataTable tempBOM, DataTable templateSelected, int bomId, int itemIdTemplate, int level = 1, int parentItemId = 0)
        {
            // Find parent BOM
            var parent = BomHead.AsEnumerable()
                .SingleOrDefault(r => r.Field<int>("id") == bomId);

            if (parent == null)
                return (tempBOM, 0m);

            // Get parent reference code from template
            string parentReferenceCode = templateSelected.AsEnumerable()
                .Where(r => r.Field<int>("item_id") == itemIdTemplate &&
                            r.Field<int>("level") == level)
                .Select(r => r.Field<string>("reference_code"))
                .FirstOrDefault();

            if (string.IsNullOrWhiteSpace(parentReferenceCode))
                return (tempBOM, 0m);

            // Base cost (labor + production)
            decimal laborCost = parent.Field<int>("man_days") * parent.Field<decimal>("labor_rate");
            decimal productionCost = parent.Field<decimal>("production_cost");
            decimal totalCost = laborCost + productionCost;

            // Add parent row (capture reference!)
            DataRow parentRow = tempBOM.Rows.Add(
                parent.Field<int>("item_id"),
                parent.Field<string>("general_name").Trim(),
                level,
                parentItemId,
                parentReferenceCode,
                parent.Field<string>("item_model").Trim(),
                parent.Field<int>("production_qty"),
                parent.Field<int>("man_days"),
                parent.Field<decimal>("labor_rate"),
                0m // unit_price placeholder
            );

            int nextLevel = level + 1;

            // Get children
            var children = BomDetails.AsEnumerable()
                .Where(r => r.Field<int>("item_bom_id") == parent.Field<int>("id"))
                .ToList();

            foreach (var child in children)
            {
                int childItemId = child.Field<int>("item_id");

                var childBom = BomHead.AsEnumerable()
                    .SingleOrDefault(r => r.Field<int>("item_id") == childItemId);

                string childModel = ItemList.AsEnumerable()
                    .Where(r => r.Field<int>("id") == childItemId)
                    .Select(r => r.Field<string>("item_model"))
                    .FirstOrDefault();

                // Child has its own BOM → recurse
                if (childBom != null)
                {
                    int childBomId = childBom.Field<int>("id");

                    var result = GetRecursiveBOM(
                        childItemId,
                        tempBOM,
                        templateSelected,
                        childBomId,
                        childItemId,
                        nextLevel,
                        parent.Field<int>("id")
                    );

                    totalCost += result.subTotal;
                }
                else
                {
                    // Leaf item
                    decimal unitPrice = decimal.Parse(child.Field<string>("unit_price"));
                    int qty = child.Field<int>("bom_qty");
                    decimal lineTotal = unitPrice * qty;

                    totalCost += lineTotal;

                    tempBOM.Rows.Add(
                        childItemId,
                        child.Field<string>("item_name").Trim(),
                        nextLevel,
                        parent.Field<int>("id"),
                        IncrementReferenceCode(parentReferenceCode),
                        childModel,
                        qty,
                        0,
                        0,
                        unitPrice
                    );
                }
            }

            // Add missing template children (if any)
            var templateChildren = templateSelected.AsEnumerable()
                .Where(r => r.Field<string>("reference_code").StartsWith(parentReferenceCode) &&
                            r.Field<int>("level") == nextLevel);

            if (templateChildren.Any())
            {
                foreach (var row in templateChildren)
                {
                    bool exists = children.Any(c =>
                        c.Field<string>("item_name").Trim() ==
                        row.Field<string>("item_name").Trim());

                    if (!exists)
                    {
                        tempBOM.Rows.Add(
                            row.Field<int>("item_id"),
                            row.Field<string>("item_name").Trim(),
                            nextLevel,
                            parent.Field<int>("id"),
                            row.Field<string>("reference_code"),
                            "",
                            0,
                            0,
                            0,
                            0m
                        );
                    }
                }
            }

            // Apply VAT (18%)
            decimal totalWithVat = totalCost * 1.18m;

            // Update parent safely
            parentRow["unit_price"] = totalWithVat;

            return (tempBOM, totalCost);
        }
        public static string IncrementReferenceCode(string referenceCode)
        {
            if (string.IsNullOrWhiteSpace(referenceCode))
                throw new ArgumentException("Reference code cannot be empty.");

            var parts = referenceCode.Split('.');

            // Parse and increment the last part
            // Parse and increment the last part
            if (int.TryParse(parts[parts.Length - 1], out int lastNumber))
            {
                lastNumber++;
                parts[parts.Length - 1] = lastNumber.ToString();
            }
            else
            {
                throw new FormatException("Invalid reference code format.");
            }

            return string.Join(".", parts);
        }

        public DataTable GetTemplateChildren(string referenceCode)
        {
            DataTable TemplateChildren = new DataTable();

            TemplateChildren.Columns.Add("item_id", typeof(int));
            TemplateChildren.Columns.Add("item_name", typeof(string));
            TemplateChildren.Columns.Add("level", typeof(int));
            TemplateChildren.Columns.Add("reference_code", typeof(string));
            //TemplateChildren.Columns.Add("parent_item_id", typeof(int));

            DataTable dgv_project_temp = new DataTable();

            dgv_project_temp = (DataTable)dgv_project_items.DataSource;

            dgv_project_temp = dgv_project_temp.AsEnumerable()
                .Where(r => r.Field<string>("reference_code").StartsWith(referenceCode + ".") ||
                r.Field<string>("reference_code") == referenceCode)
                .CopyToDataTable();


            foreach (DataRow row in dgv_project_temp.Rows)
            {
                // Add parent to tempTemplateChildren
                TemplateChildren.Rows.Add(
                    Convert.ToInt32(row["item_id"]),
                    row["components"]?.ToString().Trim() ?? "",
                    GetLevel(row["reference_code"]?.ToString() ?? ""),
                    row["reference_code"]?.ToString()
                    );
            }

            return TemplateChildren;
        }

        public string GetParentReference(string referenceCode)
        {
            if (string.IsNullOrWhiteSpace(referenceCode))
                return null;

            int lastDotIndex = referenceCode.LastIndexOf('.');

            if (lastDotIndex == -1)
                return null; // This is a root-level reference (e.g., "1")

            return referenceCode.Substring(0, lastDotIndex);
        }

        private int GetLevel(string referenceCode)
        {
            if (string.IsNullOrWhiteSpace(referenceCode))
                return 0;

            return referenceCode.Split('.').Length;
        }

        public EventHandler CellEdited;
        private void dgv_project_items_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            // Re-check stock the moment QTY changes, same as Quick Quote's equivalent -
            // the flag lives on a different cell (project_inv_stock), so it needs an
            // explicit repaint to reflect what was just typed.
            if (e.RowIndex >= 0 && dgv_project_items.Columns[e.ColumnIndex].Name == "project_items_qty" &&
                dgv_project_items.Columns.Contains("project_inv_stock"))
            {
                dgv_project_items.InvalidateCell(dgv_project_items.Columns["project_inv_stock"].Index, e.RowIndex);
            }

            // The actual recompute (ComputeByReferenceHierarchy + ComputeReferenceNonHierarchy,
            // which also now sets the DISCOUNT column - see ComputeReferenceNonHierarchy) runs
            // via CellEdited -> Quotation.Cell_EditedUC -> RecomputeParentTotals ->
            // ProjectComputationLoop for every tab, not here directly.
            CellEdited?.Invoke(this, EventArgs.Empty);
            ItemChanged?.Invoke(this, EventArgs.Empty);
            dgv_project_items.CommitEdit(DataGridViewDataErrorContexts.Commit);
            dgv_project_items.EndEdit();
        }


        //bool isInsertControllerToMotor = false;
        DataTable wiringTable = new DataTable();

        private string[] defaultWiring = { "ECB To Controller", "Conduit Pipe", "Elbow", "Coupling", "Flexible Conduit", "Straight Connector", "Controller to motor", "Ground" };
        private string[] defaultQTYFormat = { "m", "pcs", "pcs", "pcs", "m", "pcs", "m", "m" };
        private string[] defaultNumberOfWiresSet = { "3", "", "", "", "", "", "3", "1" };

        private void setProjectWirings()
        {

            wiringTable.Columns.Add("Materials", typeof(string));
            wiringTable.Columns.Add("num", typeof(string));
            wiringTable.Columns.Add("NumberOfQtyFormat", typeof(string));
            wiringTable.Columns.Add("QtyFormat", typeof(string));
            wiringTable.Columns.Add("NoOfWiresSet", typeof(string));
            // Trello #084: the real "# OF QTY / SET" (B) value - left blank by default,
            // same as every other white/input cell on this table (spec §8.4). See the
            // grouping fix above for why this was missing.
            wiringTable.Columns.Add("NumOfQtySet", typeof(string));
            wiringTable.Columns.Add("AMPREQ", typeof(string));

            int counter = 1;

            for (int i = 0; i < defaultWiring.Length; i++)
            {
                DataRow row = wiringTable.NewRow();
                row["num"] = counter.ToString();
                row["Materials"] = defaultWiring[i];
                row["NumberOfQtyFormat"] = defaultQTYFormat[i];
                row["QtyFormat"] = defaultQTYFormat[i];
                row["NoOfWiresSet"] = defaultNumberOfWiresSet[i];

                //project_wiring_num_of_qty_set_format
                wiringTable.Rows.Add(row);
                counter++;
            }

          
            bs_project_wiring.DataSource = wiringTable;
            dgv_wiring.DataSource = bs_project_wiring;


            // Grouping the columns in wiring, to match how the reference sheet reads.
            //
            // Each value that carries a unit is shown the way Excel shows it: one header
            // spanning the value column and the small blank column holding its unit, so
            // "3 | m" reads as a single "# OF QTY / SET" entry rather than two unrelated
            // columns with a divider between them. EnableGroupHeaders paints the group
            // label across the top half of the header and leaves the bottom half for each
            // column's own HeaderText - so the grouped columns' own headers are blanked in
            // the Designer, otherwise the label would appear twice.
            //
            // "# OF WIRES / SET" (A) is deliberately NOT grouped: it has no unit column of
            // its own, so it keeps a plain single header. A previous version grouped A
            // with project_wiring_num_of_wiring_set_format, but that blank column is the
            // unit for B, not for A - which both mislabelled the column and produced the
            // stacked "double header" look.
            Dictionary<string, string[]> wiringGroups = new Dictionary<string, string[]>
            {
                {
                    // Plain text, no hard-coded line breaks: the painter wraps it to the
                    // width the group spans, matching how "ITEM INV TYPE" stacks itself on
                    // the items grid.
                    "# OF QTY / SET",
                    new string[] { "project_wiring_num_of_qty_set", "project_wiring_num_of_wiring_set_format" }
                },
                {
                    "Project Inventory",
                    new string[] { "project_wiring_qty", "project_wiring_qty_format" }
                }
            };

            // One call, not one per group: each call registers its own Paint/CellPainting
            // handlers on the grid, so calling it repeatedly stacks duplicate painters.
            Helpers.EnableGroupHeaders(dgv_wiring, wiringGroups);
        }

        private void cmb_starting_method_SelectedIndexChanged(object sender, EventArgs e)
        {

            // SelectedIndexChanged fires on programmatic changes too - the generic bind
            // helper reassigns this combo whenever the tab is loaded or rebound after a
            // save - so the two MessageBoxes that used to sit here ("Wiring table is
            // empty" / "FLA and voltage are required here") popped during save and load,
            // not just when a user picked a method. A change handler whose job is to
            // compute a value should no-op silently when its inputs aren't ready, which
            // is exactly what computeECBToController already does.
            if (dgv_wiring == null || dgv_wiring.Rows.Count == 0)
                return;

            // Was: txt_FLA.Text == "" && txt_VOLT.Text == "" - an AND, so a blank FLA with
            // a filled VOLTAGE fell straight through to double.Parse(txt_FLA.Text) and threw
            // a FormatException. Only FLA actually feeds the formulas below (VOLTAGE was
            // parsed into an unused local), so require that, and TryParse it.
            if (!double.TryParse(txt_FLA.Text, out double FLA))
                return;

            if (cmb_starting_method.Text == "WYE-DELTA CLOSED" || cmb_starting_method.Text == "WYE-DELTA OPEN")
            {
                double ampRequirement = FLA * 0.6 * 1.25;

                SetWiringAmpReq("Controller to motor", (decimal)ampRequirement);
            }

            if (cmb_starting_method.Text == "DIRECT ONLINE")
            {
                double ampRequirement = FLA * 1.25;

                SetWiringAmpReq("Controller to motor", (decimal)ampRequirement);
            }

            if (cmb_starting_method.Text == "SOFT STARTER")
            {
                SetWiringAmpReq("Controller to motor", (decimal)FLA);
            }

            // Row 1 (ECB -> controller) shares FLA with the row 7 formulas above, so refresh
            // it here too rather than leaving it stale until someone retypes NO. OF PUMP/SET.
            // Safe to call unconditionally - it no-ops when FLA or the pump count is blank.
            computeECBToController();
        }

        private void ComputeWiringDGV(DataGridViewCellEventArgs e)
        {
            //try
            //{
                var noOfWiresCell = dgv_wiring.Rows[e.RowIndex].Cells["project_wiring_num_of_wiring_set"].Value;
                // Trello #084: was reading the same "# OF WIRES/SET" cell as noOfWiresCell
                // above (squaring A instead of computing A x B), because the real B column
                // didn't exist on the grid yet. See the column/grouping additions above.
                var noOfQtyCell = dgv_wiring.Rows[e.RowIndex].Cells["project_wiring_num_of_qty_set"].Value;
                var distanceTravelledCell = dgv_wiring.Rows[e.RowIndex].Cells["project_wiring_distance_travelled"].Value;
                var allowanceWireSetCell = dgv_wiring.Rows[e.RowIndex].Cells["project_wiring_allowance"].Value;
                // Spec 8.4: TOTAL QTY = QTY x # OF SETS. This read the QTY cell - which is
                // overwritten with the computed qty a few lines below, so it multiplied by a
                // stale value of the very cell being recalculated, while the real "# OF SETS"
                // column went unused.
                var noOfSetsCell = dgv_wiring.Rows[e.RowIndex].Cells["project_wiring_num_of_sets"].Value;
                var costCell = dgv_wiring.Rows[e.RowIndex].Cells["project_wiring_cost"].Value;

                 
                if (!double.TryParse(noOfWiresCell?.ToString(), out double noOfWires))
                    noOfWires = 0;

                if (!double.TryParse(noOfQtyCell?.ToString(), out double noOfQty))
                    noOfQty = 0;

                if (!double.TryParse(distanceTravelledCell?.ToString(), out double distanceTravelled))
                    distanceTravelled = 0;

                if (!double.TryParse(allowanceWireSetCell?.ToString(), out double allowanceWireSet))
                    allowanceWireSet = 0;

                if (!double.TryParse(noOfSetsCell?.ToString(), out double noOfSets))
                    noOfSets = 0;

                if (!decimal.TryParse(costCell?.ToString(), out decimal costs))
                    costs = 0;

                // Spec 8.4: QTY = A x B x (C + D), where A = # OF WIRES / SET,
                // B = # OF QTY / SET, C = DISTANCE TRAVELLED / SET, D = ALLOWANCE / WIRE / SET.
                // A was missing from this calculation, so every wiring line came out short by
                // the wires-per-set factor.
                double qty = noOfWires * noOfQty * (distanceTravelled + allowanceWireSet);
                dgv_wiring.Rows[e.RowIndex].Cells["project_wiring_qty"].Value = qty.ToString();

                double totalQty = qty * noOfSets;
                dgv_wiring.Rows[e.RowIndex].Cells["project_wiring_total_qty"].Value = totalQty.ToString();

                decimal totalCost = (decimal)totalQty * (decimal)costs;
                dgv_wiring.Rows[e.RowIndex].Cells["project_wiring_total_cost"].Value = totalCost.ToString();

            //}
            //catch (Exception ex)
            //{
            //    MessageBox.Show("Error [Compute Wiring]: " + ex.Message);
            //}
            ComputeWiringToProjectItem();
        }

        private void ComputeWiringToProjectItem()
        {
            double TotalECG = 0, TotalMotor = 0;

            for (int i = 0; i < 4; i++)
                TotalECG += GetRowValue(i);

            for (int i = 4; i < 8; i++)
                TotalMotor += GetRowValue(i);

            var RowIndexCTLMotor = dgv_project_items.Rows
                .Cast<DataGridViewRow>()
                .Where(r => !r.IsNewRow)
                .FirstOrDefault(r =>
                    r.Cells["project_items_components"].Value?.ToString().Trim() == "CTL-MOTOR"
                )?.Index ?? -1;


            var RowIndexCTLECB = dgv_project_items.Rows
                .Cast<DataGridViewRow>()
                .Where(r => !r.IsNewRow)
                .FirstOrDefault(r =>
                    r.Cells["project_items_components"].Value?.ToString().Trim() == "CTL-ECB"
                )?.Index ?? -1;


            if (RowIndexCTLECB != -1)
            {
                dgv_project_items.Rows[RowIndexCTLECB].Cells["project_items_unit_price"].Value = TotalECG;
            }

            if (RowIndexCTLMotor != -1)
            {
                dgv_project_items.Rows[RowIndexCTLMotor].Cells["project_items_unit_price"].Value = TotalMotor;
            }

        }

        double GetRowValue(int rowIndex)
        {
            if (rowIndex >= dgv_wiring.Rows.Count) return 0;

            var value = dgv_wiring.Rows[rowIndex]
                .Cells["project_wiring_total_cost"]
                .Value;

            return double.TryParse(value?.ToString(), out var result)
                ? result
                : 0;
        }

        private void dgv_wiring_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            ComputeWiringDGV(e);
            CellChangedWiring?.Invoke(this, EventArgs.Empty);

            AddWiringRowsComponentProject();
        }


        // Both amp writers used to target the wiringTable DataTable. That only reaches the
        // screen when setProjectWirings() built and bound it - the load path
        // (SetProjectWiring) sets DataSource = null, clears the grid and adds rows by hand,
        // so on any saved quote the grid is unbound and every write to wiringTable was
        // invisible. Writing to the grid works under both paths: when the grid IS bound,
        // setting a cell propagates back to the underlying row anyway.
        private void SetWiringAmpReq(string materialName, decimal value)
        {
            if (dgv_wiring == null || dgv_wiring.Rows.Count == 0) return;
            if (!dgv_wiring.Columns.Contains("project_wiring_amp_req")) return;
            if (!dgv_wiring.Columns.Contains("project_wiring_materials")) return;

            foreach (DataGridViewRow row in dgv_wiring.Rows)
            {
                if (row.IsNewRow) continue;

                string material = row.Cells["project_wiring_materials"].Value?.ToString();
                if (string.Equals(material, materialName, StringComparison.OrdinalIgnoreCase))
                {
                    row.Cells["project_wiring_amp_req"].Value = value;
                    return;
                }
            }
        }

        private void computeECBToController()
        {
            if (!string.IsNullOrWhiteSpace(txt_FLA.Text) && !string.IsNullOrWhiteSpace(txt_no_of_pump_set.Text))
            {
                if (double.TryParse(txt_FLA.Text, out double FLA) && double.TryParse(txt_no_of_pump_set.Text, out double PumpSet))
                {
                    // Spec 8.4: AMP (ECB -> controller, row 1)
                    //   = FLA_of_1_pump x number_of_pumps_in_set x 1.25, 3 wires only.
                    // The x1.25 factor was missing here, so row 1's amp requirement came
                    // out 20% under spec - which then feeds wire selection (DESCRIPTION is
                    // meant to offer wires matching AMP REQ. or one step above) and cost.
                    // The controller-to-motor formulas in cmb_starting_method_SelectedIndexChanged
                    // already apply their factors correctly.
                    double ECB = FLA * PumpSet * 1.25;

                    SetWiringAmpReq("ECB To Controller", (decimal)ECB);
                }
            }
        }


        // NO. OF PUMP/SET feeds row 1's formula directly (FLA x pumps x 1.25). Refreshes
        // both computed rows rather than just row 1 - see RefreshWiringAmpRequirements,
        // the single entry point every trigger uses.
        private void txt_no_of_pump_set_TextChanged(object sender, EventArgs e)
        {
            RefreshWiringAmpRequirements();
        }

        // Row 1's amp (FLA x NO. OF PUMP/SET x 1.25, spec 8.4) depends on FLA as much as on
        // the pump count, but computeECBToController was only ever reached from
        // txt_no_of_pump_set's TextChanged - so an FLA arriving from the pump's Item Entry
        // (spec 8.4: "FLA and VOLTAGE fetched from the selected pump's Item Entry specs")
        // left AMP REQ. on row 1 blank while row 7 populated fine from the starting-method
        // handler.
        private void txt_FLA_TextChanged(object sender, EventArgs e)
        {
            // Refresh BOTH amp rows. Row 7's formula lives in the starting-method handler,
            // which only ran when the dropdown itself changed - so an FLA arriving later
            // from Final Selection (SetFinalPumpData sets txt_FLA) left row 7 stale while
            // row 1 updated. That handler is safe to call directly now: it no-ops quietly
            // when the wiring table or FLA isn't ready, and ends by refreshing row 1.
            cmb_starting_method_SelectedIndexChanged(this, EventArgs.Empty);
        }

        private void checkBox_Wiring_CheckedChanged(object sender, EventArgs e)
        {
            WiringVisible(chk_wiring.Checked);

            AddWiringRowsComponentProject();

            // Ticking WIRING is the first moment the grid exists, so this is where the
            // computed cells get their initial values. Its absence is why re-ticking the
            // box appeared to "fix" the computation - that was the only path that ever
            // ended up refreshing them on an already-loaded quote.
            if (chk_wiring.Checked)
                RefreshWiringAmpRequirements();
        }

        private void AddWiringRowsComponentProject()
        {
            // Validate against duplicating the wiring block. This used to compare only
            // template_id to "wiring", which is an in-session marker that does not
            // survive a save (template_id is an integer column) - so on a reloaded quote
            // the guard saw nothing and ticking WIRING appended a second full block.
            // IsWiringComponentRow also recognises the block by component name, which is
            // persisted, so the guard now holds for loaded rows too.
            if (dgv_project_items.Rows.Cast<DataGridViewRow>().Any(r => !r.IsNewRow
                    && IsWiringComponentRow(
                        r.Cells["project_items_template_id"].Value?.ToString(),
                        r.Cells["project_items_components"].Value?.ToString())))
                return;

            if (chk_wiring.Checked)
            {
                // LastRefInt is only ever set by the "load from Template" flow
                // (cb_template_project_SelectedIndexChanged) - if items were added
                // manually one at a time instead (no template selected), it stays at its
                // default 0, so wiring rows started over at "1" and collided with
                // reference codes that already existed (e.g. a manually-added PUMP already
                // using "1"/"1.1"). Reading the actual max reference code off the grid
                // right now instead avoids that regardless of how the existing items got
                // there.
                int nextReference = GetMaxTopLevelReferenceCode();
                AddWiringRowsComponent(nextReference);
            }
            else
                RemoveWiringRowsComponentByBaseReference();
        }

        // Finds the highest top-level reference_code already on the grid (e.g. for codes
        // "1", "1.1", "2" this returns 2) so a following block of rows (like wiring
        // materials) continues numbering from there instead of restarting at 1.
        private int GetMaxTopLevelReferenceCode()
        {
            int max = 0;

            if (!(dgv_project_items.DataSource is DataTable dataSource) || !dataSource.Columns.Contains("reference_code"))
                return max;

            foreach (DataRow row in dataSource.Rows)
            {
                string value = row["reference_code"]?.ToString();
                if (string.IsNullOrWhiteSpace(value))
                    continue;

                string topLevelPart = value.Split('.')[0];
                if (int.TryParse(topLevelPart, out int num) && num > max)
                {
                    max = num;
                }
            }

            return max;
        }



        private void WiringVisible(bool checkBox)
        {
            dgv_wiring.Visible = checkBox;
            label6.Visible = checkBox;
            txt_FLA.Visible = checkBox;
            label7.Visible = checkBox;
            txt_VOLT.Visible = checkBox;
            label58.Visible = checkBox;
            cmb_assign_engineer_user_id.Visible = checkBox;

        }

        private void txt_final_Click(object sender, EventArgs e)
        {
            // Opens the pump FLA/Voltage picker - an edit action, shouldn't be reachable
            // while this tab is locked (view mode).
            if (!_isEditable)
                return;

            FinalTxtBoxClicked?.Invoke(this, EventArgs.Empty);
        }

        private void dgv_final_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (!_isEditable)
                return;

            FinalTxtBoxClicked?.Invoke(this, EventArgs.Empty);
        }

        private void ComputeByReferenceHierarchy(DataGridView dgv)
        {

            DataTable dt = dgv.DataSource as DataTable;

            if (dt == null || dgv == null) return;

            var parentReferenceCodes = dt.AsEnumerable()
                .Select(row => GetParentReferenceCode(dt, row.Field<string>("reference_code")))
                .Where(parentCode => !string.IsNullOrEmpty(parentCode))
                .Distinct()
                .ToList();

            foreach (var parentReferenceCode in parentReferenceCodes)
            {
                // Calculate the total unit_price for the given parent and its descendants
                decimal totalUnitPrice = GetTotalUnitPriceForChildren(dt, parentReferenceCode);

                // Output the total unit_price for this parent reference_code
                //Console.WriteLine($"Total unit_price for children of '{parentReferenceCode}' and its descendants: {totalUnitPrice.ToString():C}");

                // Update the DataGridView cell for the parent reference_code
                foreach (DataGridViewRow row in dgv.Rows)
                {
                    if (row.Cells["reference_code"].Value?.ToString() == parentReferenceCode)
                    {
                        row.Cells["project_items_unit_price"].Value = totalUnitPrice;
                        break;
                         
                    }
                }
            }
        }

        private void ComputeReferenceNonHierarchy(DataGridView dgv)
        {

            foreach (DataGridViewRow row in dgv.Rows)  
            {
                if (row.IsNewRow) continue;

                var referenceCode = row.Cells["reference_code"].Value?.ToString();
                if (string.IsNullOrWhiteSpace(referenceCode) || referenceCode.Contains("."))
                    continue;

                if (row.Cells["project_items_qty"].Value == null || string.IsNullOrEmpty(row.Cells["project_items_qty"].Value.ToString()) ||
                    row.Cells["project_items_unit_price"].Value == null || string.IsNullOrEmpty(row.Cells["project_items_unit_price"].Value.ToString()))
                    continue;

                decimal unitPrice = Convert.ToDecimal(Helpers.GetCleanedPriceValue(row.Cells["project_items_unit_price"].Value.ToString()));
                decimal discount = CalculateDiscountMultiplier(row.Cells["project_items_multiplier"].Value?.ToString());
                decimal qty = Convert.ToDecimal(row.Cells["project_items_qty"].Value);
                decimal TotalUnitPrice = unitPrice * qty;
                decimal discounted = TotalUnitPrice * discount;

                row.Cells["project_items_line_total"].Value = discounted;

                // DISCOUNT/MARK UP PRICE (project_items_discount) - the per-unit price after
                // the multiplier is applied. This used to only ever be set by a separate, dead
                // code path (see ComputeProjectDgv's removal above) with its own, incompatible
                // parsing of the multiplier string, so it never reflected what was actually
                // charged. Deriving it from the same `discount` ratio used for the real line
                // total above keeps the two in sync.
                row.Cells[ProjectQuoteDGV.DISCOUNT].Value = (unitPrice * discount).ToString("C2");
            }
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

            //var children = dt.AsEnumerable()
            //.Where(r =>
            //     r.Field<string>("reference_code") is string code &&
            //    code.StartsWith(parentReferenceCode + ".") &&
            //    code.Count(c => c == '.') == parentReferenceCode.Count(c => c == '.') + 1)
            //.ToList();  

            // If no children found, return 0
            if (!children.Any())
                return 0;

            decimal totalLaborCost = 0m;

            if (ParentRow != null)
            {
                decimal manDays = Convert.IsDBNull(ParentRow["man_days"]) ? 0m : Convert.ToDecimal(ParentRow["man_days"]);
                decimal laborRate = Convert.IsDBNull(ParentRow["labor_rate"]) ? 0m : Convert.ToDecimal(ParentRow["labor_rate"]);

                totalLaborCost = laborRate * manDays;

                //Console.WriteLine($"Adding labor cost for parent '{parentReferenceCode}': {manDays} * {laborRate} = {totalLaborCost:C}");
            }

            decimal AllChildTotal = 0;

            // For each child, recursively sum their descendants' unit_prices
            foreach (var child in children)
            {
                string childReferenceCode = child.Field<string>("reference_code");

                // Recursively find the total for this child's descendants
                decimal ChildTotal =  GetTotalUnitPriceForChildren(dt, childReferenceCode);
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

            decimal TotalAmount = (totalLaborCost + totalUnitPrice) * MarkUpMultiplier;
            //decimal TotalAmount = (totalLaborCost + totalUnitPrice);

            return TotalAmount;
        }


        private void dgv_project_items_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {

            if (e.RowIndex < 0 || e.ColumnIndex < 0)
                return;
            //ComputeByReferenceHierarchy(dgv_project_items);
            //ComputeReferenceNonHierarchy(dgv_project_items);
            //ProjectComputationLoop();
        }

        public static void HandleNumericColumns(DataGridView dgv, DataGridViewEditingControlShowingEventArgs e, string[] numericColumnNames, params char[] extraAllowedChars)
        {
            if (dgv.CurrentCell == null)
                return;

            string columnName = dgv.Columns[dgv.CurrentCell.ColumnIndex].Name;

            // Always detach first
            e.Control.KeyPress -= NumericColumn_KeyPress;

            if (numericColumnNames.Contains(columnName))
            {
                // Pass allowed characters via Tag
                if (e.Control is TextBox tb)
                {
                    tb.Tag = extraAllowedChars;
                }

                e.Control.KeyPress += NumericColumn_KeyPress;
            }
        }

        private static void NumericColumn_KeyPress(object sender, KeyPressEventArgs e)
        {
            var tb = sender as TextBox;
            var extraAllowedChars = tb?.Tag as char[];

            // Allow control keys
            if (char.IsControl(e.KeyChar))
                return;

            // Allow digits
            if (char.IsDigit(e.KeyChar))
                return;

            // Allow decimal point (only once)
            if (e.KeyChar == '.' && tb != null && !tb.Text.Contains("."))
                return;

            // Allow extra characters
            if (extraAllowedChars != null &&
                extraAllowedChars.Contains(e.KeyChar))
                return;

            // Block everything else
            e.Handled = true;
        }

        public List<SalesProjectHistory> GetHistoryList()
        {
            var historyList = new List<SalesProjectHistory>();

            // Example: loop through your DataTable or whatever source
            //foreach (DataRow row in historySource.Rows)
            //{
            //    historyList.Add(new SalesProjectHistory
            //    {
            //        HistoryID = Convert.ToUInt32(row["HistoryID"]),
            //        BasedId = Convert.ToUInt32(row["BasedId"]),
            //        User = row["User"].ToString(),
            //        Date = row["Date"].ToString(),
            //        Time = row["Time"].ToString(),
            //        OldData = row["OldData"].ToString(),
            //        NewData = row["NewData"].ToString()
            //    });
            //}

            return historyList;
        }
    }
}
