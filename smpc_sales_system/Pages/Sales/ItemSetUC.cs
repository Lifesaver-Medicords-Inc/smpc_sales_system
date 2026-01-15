using smpc_app.Services.Helpers;
using smpc_sales_app.Data;
using smpc_sales_app.Models;
using smpc_sales_app.Pages.Sales;
using smpc_sales_app.Services.Helpers;
using smpc_sales_app.Services.Sales;
using smpc_sales_system.Models;
using smpc_sales_system.Services.Sales;
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
        public event EventHandler FinalTxtBoxClicked;
        public event EventHandler DeleteReferenceCode;

        public ItemSetUC()
        {
            InitializeComponent();

            // methods for event changes
            AttachTextChangedEventConditions(pnl_advanced_conditions);
            AttachTextChangedEventContent(pnl_project_content);
            AttachCellValuechangedEventProjectItems(dgv_project_items);
            AttachCellValuechangedEventWiring(dgv_wiring);
            dgv_project_items.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            setProjectWirings();

            //Default hide wiring
            WiringVisible(false);

            // project template

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

        public Dictionary<string, dynamic> GetProjectContentsData()
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


            return data;
        }


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
                    amp_req = item["project_wiring_amp_req"]?.ToString() ?? string.Empty,
                    wire_amp = item["project_wiring_wire_amp"]?.ToString() ?? string.Empty,
                    description = item["project_wiring_description"]?.ToString() ?? string.Empty,
                    num_of_wires_set = item["project_wiring_num_of_wires_set"]?.ToString() ?? string.Empty,
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
        public Dictionary<string, object> GetProjectItems()
        {
            var projectSource = Helpers.ConvertDataGridViewToDataTable(dgv_project_items);
            List<SalesProjectItems> items = new List<SalesProjectItems>();

            foreach (DataRow item in projectSource.Rows)
            {
                if (item == null) continue;

                var spi = new SalesProjectItems
                {
                    // PK
                    items_id = int.TryParse(item["project_items_id"]?.ToString(), out int tempItemsId) ? tempItemsId : 0,

                    item_id = int.TryParse(item["project_items_item_id"]?.ToString(), out int tempItemId) ? tempItemId : 0,
                    based_id = int.TryParse(item["project_items_based_id"]?.ToString(), out int tempBasedId) ? tempBasedId : 0,

                    bom_id = int.TryParse(item["project_items_bom_id"]?.ToString(), out int tempBomId) ? tempBomId : 0,
                    node_id = int.TryParse(item["project_items_node_id"]?.ToString(), out int tempNodeId) ? tempNodeId : 0,
                    node_name = item["project_items_node_name"]?.ToString() ?? string.Empty,
                    parent_node_id = int.TryParse(item["project_items_parent_node_id"]?.ToString(), out int tempParentNode) ? tempParentNode : 0,
                    node_order = int.TryParse(item["project_items_node_order"]?.ToString(), out int tempNodeOrder) ? tempNodeOrder : 0,
                    node_type = item["project_items_node_type"]?.ToString() ?? string.Empty,
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

        private class DGVProjectComputation
        {
            private decimal Qty { get; set; }
            private string Multiplier { get; set; }
            public decimal Discount { get; private set; }
            public decimal ListPrice { get; private set; }
            public decimal NetTotal { get; private set; }

            public DGVProjectComputation(decimal qty, decimal listPrice, string discountPercent = "")
            {
                this.Qty = qty;
                this.ListPrice = listPrice;
                this.Multiplier = discountPercent;
                this.Discount = 0;
                this.NetTotal = 0;
            }

            public void ComputeProjectQuote()
            {
                if (!string.IsNullOrEmpty(Multiplier) && Multiplier != "0")
                {
                    decimal totalBeforeAdjustment = Qty * ListPrice;
                    decimal price = ListPrice;

                    if (Multiplier.Contains("*"))
                    {
                        string[] factors = Multiplier.Split('*');
                        foreach (string factor in factors)
                        {
                            if (decimal.TryParse(factor, out decimal factorValue))
                            {
                                if (factorValue > 0 && factorValue < 1)
                                {
                                    // Apply each discount factor directly
                                    price *= factorValue;
                                }
                                else
                                {
                                    Console.WriteLine("Each discount factor must be bet en 0 and 1.");
                                    Discount = 0;
                                    NetTotal = totalBeforeAdjustment;
                                    return;
                                }
                            }
                        }
                    }
                    else if (Multiplier.Contains("/"))
                    {
                        string[] markups = Multiplier.Split('/');
                        foreach (string markup in markups)
                        {
                            if (decimal.TryParse(markup, out decimal markupValue))
                            {
                                if (markupValue >= 0 && markupValue <= 100)
                                {
                                    price /= markupValue;
                                }
                                else
                                {
                                    Console.WriteLine("Each markup percentage must be between 0 and 100.");
                                    Discount = 0;
                                    NetTotal = totalBeforeAdjustment;
                                    return;
                                }
                            }
                        }
                    }
                    else if (decimal.TryParse(Multiplier, out decimal adjustmentPercent))
                    {
                        if (adjustmentPercent >= 0 && adjustmentPercent <= 100)
                        {
                            //price = 1 - (adjustmentPercent / 100);\
                            // Single discount scenario
                            price = this.ListPrice * (decimal.Parse(this.Multiplier));
                        }
                        else
                        {
                            Console.WriteLine("Adjustment percentage must be between 0 and 100.");
                            Discount = 0;
                            NetTotal = totalBeforeAdjustment;
                            return;
                        }
                    }

                    if (price >= 0)
                    {
                        Discount = price;
                        NetTotal = Discount * Qty;
                    }
                }
                else
                {
                    Discount = 0;
                    NetTotal = Qty * ListPrice;

                }
            }
        }

        public void setMultiplier(List<string> multiplier)
        {
            bs_multiplier.DataSource = multiplier;
            //this.project_items_multiplier.DataSource = multiplier;
        }

        private void ComputeProjectDgv(DataGridViewCellEventArgs e)
        {
            try
            {
                var qty_cell = dgv_project_items.Rows[e.RowIndex].Cells[ProjectQuoteDGV.QTY].Value;
                var list_price_cell = dgv_project_items.Rows[e.RowIndex].Cells[ProjectQuoteDGV.LIST_PRICE].Value;
                var unit_price_cell = dgv_project_items.Rows[e.RowIndex].Cells[ProjectQuoteDGV.UNIT_PRICE].Value;
                var multiplier_cell = dgv_project_items.Rows[e.RowIndex].Cells[ProjectQuoteDGV.MULTIPLIER].Value == null ? "0" :
                    dgv_project_items.Rows[e.RowIndex].Cells[ProjectQuoteDGV.MULTIPLIER].Value.ToString();

                this.dgv_project_items.Rows[e.RowIndex].Cells[ProjectQuoteDGV.LIST_PRICE].Value = Helpers.FormatAsCurrency(list_price_cell);
                this.dgv_project_items.Rows[e.RowIndex].Cells[ProjectQuoteDGV.UNIT_PRICE].Value = Helpers.FormatAsCurrency(unit_price_cell);

                if (qty_cell != null && list_price_cell != null)
                {
                    decimal listPrice;
                    decimal qty = decimal.Parse(this.dgv_project_items.Rows[e.RowIndex].Cells[ProjectQuoteDGV.QTY].Value.ToString());
                    bool listPriceValid = decimal.TryParse(Helpers.GetCleanedPriceValue(this.dgv_project_items.Rows[e.RowIndex].Cells[ProjectQuoteDGV.LIST_PRICE].Value.ToString()), out listPrice);
                    string multiplier = this.dgv_project_items.Rows[e.RowIndex].Cells[ProjectQuoteDGV.MULTIPLIER].Value == null ? "0" :
                        dgv_project_items.Rows[e.RowIndex].Cells[ProjectQuoteDGV.MULTIPLIER].Value.ToString();

                    DGVProjectComputation projectComputation = new DGVProjectComputation(qty, listPrice, multiplier);
                    projectComputation.ComputeProjectQuote();


                    // currency converter
                    this.dgv_project_items.Rows[e.RowIndex].Cells[ProjectQuoteDGV.DISCOUNT].Value = projectComputation.Discount.ToString("C2");
                    this.dgv_project_items.Rows[e.RowIndex].Cells[ProjectQuoteDGV.NET_TOTAL].Value = projectComputation.NetTotal.ToString("C2");

                    ProjectComputationLoop();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("ERROR 101:    " +
                    "" + ex);
            }
        }

        public Dictionary<string, dynamic> ProjectComputationLoop()
        {
            Quotation q = new Quotation();
            decimal gross_sales = 0, vat_amount = 0, net_sales = 0;
            decimal percent_discount = 0;
            decimal net_amount_due = 0, total_amount_due = 0;
            decimal cash_discount = q.GetCashDiscount();
            const decimal VAT_RATE = 0.12m;

            foreach (DataGridViewRow row in this.dgv_project_items.Rows)
            {
                if (row.Cells[ProjectQuoteDGV.QTY].Value != null &&
                    row.Cells[ProjectQuoteDGV.LIST_PRICE].Value != null)
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

            // Calculate percent discount
            if (gross_sales != 0)
            {
                percent_discount = ((gross_sales - net_sales) / gross_sales) * 100;
            }
            // Calculate VAT (12% of net sales)
            vat_amount = net_sales * VAT_RATE;

            // Calculate net amount due (subtract cash discount)
            net_amount_due = net_sales - cash_discount;

            // Calculate total amount due (net amount + VAT)
            total_amount_due = net_amount_due + vat_amount;

            // Format and display results
            Dictionary<string, dynamic> data = new Dictionary<string, dynamic>();
            data.Add("gross_sales", Helpers.MoneyFormatDecimal(gross_sales));
            data.Add("vat_amount", Helpers.MoneyFormatDecimal(vat_amount));
            data.Add("net_sales", Helpers.MoneyFormatDecimal(net_sales));
            data.Add("percent_discount", percent_discount.ToString("0.00") + "%");
            data.Add("cash_discount", Helpers.MoneyFormatDecimal(cash_discount));
            data.Add("net_amount_due", Helpers.MoneyFormatDecimal(net_amount_due));
            data.Add("total_amount_due", Helpers.MoneyFormatDecimal(total_amount_due));
            return data;
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
        }

        public Dictionary<string, dynamic> GetSizeUpData()
        {
            Panel[] pnl = { pnl_project_content };
            Dictionary<string, dynamic> values = new Dictionary<string, dynamic>();

            foreach (var panels in pnl)
            {
                foreach (Control ctrl in panels.Controls)
                {
                    if (ctrl is TextBox textbox && textbox.Name.Contains("size_up"))
                    {
                        string key = textbox.Name.Replace("txt_", " ");
                        dynamic val = null;

                        val = textbox.Text.ToString();

                        values[key] = val;
                    }
                }

            }

            return values;
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

            dgv_project_items.Rows.Clear();

            dgv_project_items.Columns[8].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;

            Font boldFont = new Font(dgv_project_items.DefaultCellStyle.Font, FontStyle.Bold);
            Font normalFont = new Font(dgv_project_items.DefaultCellStyle.Font, FontStyle.Regular);

            // Build a dictionary that handles duplicate node_id values by grouping and taking the first row for each id.
            var nodeLookup = dt.AsEnumerable()
                               .GroupBy(row => row.Field<int>("node_id"))
                               .ToDictionary(
                                   g => g.Key,
                                   g =>
                                   {
                                       var row = g.First();
                                       return new ProjectTemplateChildModel
                                       {
                                           Id = row.Field<int>("id"),
                                           ParentId = row.Field<int>("parent_id"),
                                           ItemId = row.Field<int>("item_id"),
                                           Components = row.Field<string>("components"),
                                           Level = row.Field<int>("level")
                                       };
                                   });

            var rootNodes = dt.AsEnumerable()
                              .Where(row => row.Field<int>("parent_node_id") == 0)
                              .OrderBy(row => row.Field<int>("node_order"))
                              .ToList();

            foreach (var rootNode in rootNodes)
            {
                int parentRowIndex = dgv_project_items.Rows.Add();

                DataGridViewRow newRow = dgv_project_items.Rows[parentRowIndex];

                newRow.Cells["project_items_node_name"].Value = rootNode.Field<string>("node_name");
                newRow.Cells["project_items_node_id"].Value = rootNode.Field<int>("node_id");
                newRow.Cells["project_items_parent_node_id"].Value = rootNode.Field<int>("parent_node_id");
                newRow.Cells["project_items_node_order"].Value = rootNode.Field<int>("node_order");
                newRow.Cells["project_items_node_type"].Value = rootNode.Field<string>("node_type");
                newRow.Cells["project_items_components"].Value = "▶ " + rootNode.Field<string>("node_name");


                newRow.Cells["project_items_components"].Style.BackColor = Color.LightCoral;
                newRow.Cells["project_items_components"].Style.Font = boldFont;

                // Recursively add child nodes
                AddChildNodesFromDb(rootNode.Field<int>("node_id"), dt, nodeLookup, 1);

            }

            dgv_project_items.Columns[1].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
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
                        row.Cells["project_items_multiplier"].Value = "0.035";
                    }
                }
            }
        }

        public void SetFetchedItemData(DataTable dt)
        {

            if (!dt.Columns.Contains("node_type"))
            {
                MessageBox.Show("Column 'node_type' not found in DataTable.");
                return;
            }


            var stringTable = Helpers.ConvertDataTableToStringTable(dt);

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
            }

            dgv_project_items.DataSource = stringTable;
            dgv_project_items.ReadOnly = false;
            dgv_project_items.EnableHeadersVisualStyles = false; // Allow styling

            // Apply colors after data is fully loaded
            dgv_project_items.DataBindingComplete += (s, e) => ApplyRowStyles();
        }

        public void SetProjectWiring(DataTable dt)
        {
            var stringtable = Helpers.ConvertDataTableToStringTable(dt);
            dgv_wiring.DataSource = stringtable;
            dgv_wiring.ReadOnly = false;

        }

        private void ApplyRowStyles()
        {
            foreach (DataGridViewRow row in dgv_project_items.Rows)
            {
                if (!row.IsNewRow)
                {
                    DataGridViewCell cell = row.Cells[9];
                    int nodeTypeColumnIndex = dgv_project_items.Columns["project_items_node_type"].Index;
                    string nodeType = row.Cells[nodeTypeColumnIndex].Value?.ToString().Trim();

                    //MessageBox.Show($"Processing Row: {nodeType}");

                    row.DefaultCellStyle.BackColor = Color.White; // Reset

                    if (nodeType == "Parent")
                    {
                        cell.Style.BackColor = Color.Yellow;
                        //MessageBox.Show("red");
                    }
                    else if (nodeType == "Leaf")
                    {
                        cell.Style.BackColor = Color.LightCoral;
                        //MessageBox.Show("yellow");
                    }
                    else if (string.IsNullOrWhiteSpace(nodeType))
                    {
                        cell.Style.BackColor = Color.LightGreen;
                        // MessageBox.Show("none");
                    }
                }
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


        public void SetFinalPumpData(string FLA, string Voltage, string Final)
        {

            dgv_final.Rows.Add(Final.ToString(), FLA.ToString(), Voltage.ToString());

            decimal fla_highest = 0;
            decimal voltage_highest = 0;
            decimal fla_total = 0;
            decimal Pump_Total_Qty = 0;


            foreach (DataGridViewRow row in dgv_final.Rows)
            {

                if (row.IsNewRow) continue;

                decimal fla = decimal.Parse(row.Cells["fla"].Value.ToString());
                decimal voltage = decimal.Parse(row.Cells["voltage"].Value.ToString());

                if (fla > fla_highest)
                {
                    fla_highest = fla;
                }
                if (voltage > voltage_highest)
                {
                    voltage_highest = voltage;
                }

                fla_total += fla;
            }

            txt_FLA.Text = fla_highest.ToString();
            txt_VOLT.Text = voltage_highest.ToString();

            foreach (DataGridViewRow row in dgv_project_items.Rows)
            {
                if (row.IsNewRow) continue;

                if (row.Cells["project_items_components"].Value == null)
                {
                    return;
                }

                if (row.Cells["project_items_components"].Value.ToString().ToLower() == "pump")
                {
                    Pump_Total_Qty += int.Parse(row.Cells["project_items_qty"].Value.ToString());
                }
            }

            //ECB To Controller Value AMP REQ.
            dgv_wiring.Rows[0].Cells["project_wiring_amp_req"].Value = fla_highest * Pump_Total_Qty * 1.25m;

        }

        public DataGridView DgvProjectItems
        {
            get { return this.dgv_project_items; }
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
                }
            }

        }


        public void SetComponentModelDataUnbound(int index, string itemid, string bomid, string model)
        {
            //dgv_project_items.Rows.Insert(index);
            DataGridViewRow newRow = dgv_project_items.Rows[index - 1];
            newRow.Cells["project_items_bom_id"].Value = bomid;
            newRow.Cells["project_items_item_id"].Value = itemid;
            newRow.Cells["project_items_model"].Value = model;
            // add styles soon
        }


        // NOT BOUND TO DATASOURCE
        public void SetComponentDataUnbound(int index, string itemid, string itemName, string size, string model)
        {

            dgv_project_items.Rows.Insert(index);

            //DataGridViewRow nRow = dgv_project_items.Rows[index - 1];
            //nRow.Cells["project_items_model"].Value = model;

            DataGridViewRow newRow = dgv_project_items.Rows[index];

            newRow.Cells["project_items_item_id"].Value = itemid;
            newRow.Cells["project_items_components"].Value = itemName;
            newRow.Cells["project_items_model"].Value = model;


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

        private DataTable stockQuickDataTable;

        public DataTable ItemList { get; set; } = new DataTable();
        public DataTable BomHead { get; set; } = new DataTable();
        public DataTable BomDetails { get; set; } = new DataTable();

        // for wiring soon
        private async void ItemSetUC_Load(object sender, EventArgs e)
        {

            stockQuickDataTable = Helpers.GetDataTableFromUnboundGrid(dgv_project_items);

            var dt = await ProjectTemplatesService.GetProjectTemplates();
            DataTable listOfTemplates = JsonHelper.ToDataTable(dt.SalesProjectTemplate);
            DataTable templates = JsonHelper.ToDataTable(dt.sales_project_template_child);

            var itemData = await ItemService.GetItem();
            var bomData = await ProjectService.GetBom();

            ItemList = JsonHelper.ToDataTable(itemData.items);
            BomHead = JsonHelper.ToDataTable(bomData.bom_head);
            BomDetails = JsonHelper.ToDataTable(bomData.bom_details);

            // --- ADD initial 0/default row ---
            DataRow defaultRow = listOfTemplates.NewRow();
            defaultRow["template_id"] = 0; // or DBNull.Value
            defaultRow["template_name"] = "-- Select Template --";
            listOfTemplates.Rows.InsertAt(defaultRow, 0); // Insert at index 0

            cb_template_project.DataSource = listOfTemplates;
            cb_template_project.DisplayMember = "template_name";
            cb_template_project.ValueMember = "template_id";

            cb_template_project.SelectedIndexChanged += cb_template_project_SelectedIndexChanged;

            var dtProjectTemplates = await ProjectService.GetProjects();

            ClearProjectItemsDgv();

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

            dgv_project_items.DataSource = stockQuickDataTable.Clone();
        }

        bool isLoadingTemplate = false;

        private async void cb_template_project_SelectedIndexChanged(object sender, EventArgs e)
        {

            if (isLoadingTemplate)
                return;

            try
            {
                isLoadingTemplate = true;

                ClearProjectItemsDgv();

                if (cb_template_project.SelectedValue == null || cb_template_project.SelectedValue == DBNull.Value)
                    return;

                string templateId = cb_template_project.SelectedValue.ToString();
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

                string lastRef = "";

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

                    string indent = new string(' ', level * 4);

                    newRow["components"] = indent + (row["Components"]?.ToString() ?? "");
                    newRow["item_id"] = row["ItemId"];
                    newRow["reference_code"] = refCode;
                    newRow["template_id"] = templateId;

                    dataSource.Rows.Add(newRow);

                    lastRef = refCode.Split('.')[0];
                }

                int LastRefInt = 0;

                if (lastRef != "")
                    LastRefInt = int.Parse(lastRef);
                else
                    LastRefInt = 0;

                AddWiringRowsComponent(LastRefInt);
            }
            finally
            {
                isLoadingTemplate = false;
            }

        }

        private void AddWiringRowsComponent(int Reference)
        {
            int NewReference = Reference + 1;

            DataTable dataSource = dgv_project_items.DataSource as DataTable;

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

        private void textBox64_MouseClick(object sender, MouseEventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
            ButtonClicked?.Invoke(this, EventArgs.Empty);
        }

        int index { get; set; }
        public Action<int, DataGridView> HandleItemSelectionClick { get; internal set; }

        public int GetIndex()
        {
            return index;
        }

        private void dgv_project_items_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0)
                return;

            index = e.RowIndex;
            if (dgv_project_items.Columns[e.ColumnIndex].Name == "project_items_components")
            {
                CellClicked?.Invoke(this, EventArgs.Empty);
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

            ComputeByReferenceHierarchy();
        }

        private void AddModel(DataGridView dgv, int rowIndex, bool isBom, int BomId, int ItemId, string referenceCode, int templateId = 0)
        {
            decimal unitPrice = 0.00m;

            if (rowIndex >= 0)
            {
                DataGridViewRow DataGridRow = dgv.Rows[rowIndex];

                unitPrice = decimal.Parse(DataGridRow.Cells["project_items_unit_price"].Value?.ToString());
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

            dataSource.Rows.InsertAt(projectItem, rowIndex);
        }

        private void AssignModel(int index, DataGridView dgv)
        {
            string Id = dgv.Rows[index].Cells["item_id"].Value.ToString();

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

        //private (DataTable tempTable, decimal subTotal) GetRecursiveBOM(int rowIndex, int itemId, DataTable tempBOM, DataTable templateSelected, string parentReference, int BomId, int itemIdTemplate, int level = 1, int parentItemId = 0)
        //{
        //    // Get parent rom
        //    var parent = BomHead.AsEnumerable()
        //            .SingleOrDefault(r => r.Field<int>("id") == BomId);

        //    if (parent == null)
        //        return (tempBOM, 0);
            
        //    string ParentReferenceCode = templateSelected.AsEnumerable()
        //                        .Where(r => r.Field<int>("item_id") == itemIdTemplate
        //                            && r.Field<int>("level") == level)
        //                        .Select(r => r.Field<string>("reference_code"))
        //                        .FirstOrDefault();

        //    decimal ParentPrice = parent.Field<decimal>("production_cost");
        //    decimal laborCost = parent.Field<int>("man_days") * parent.Field<decimal>("labor_rate");
        //    decimal totalCost = laborCost;

        //    //Add parent to tempBOM
        //    tempBOM.Rows.Add(
        //        parent.Field<int>("item_id"),
        //        parent.Field<string>("general_name").Trim(),
        //        level,
        //        parentItemId,
        //        ParentReferenceCode,
        //        parent.Field<string>("item_model").Trim(),
        //        parent.Field<int>("production_qty"),
        //        parent.Field<int>("man_days"),
        //        parent.Field<decimal>("labor_rate"),
        //        ParentPrice
        //        );

        //    // Get children rows
        //    var children = BomDetails.AsEnumerable()
        //        .Where(r => r.Field<int>("item_bom_id") == parent.Field<int>("id"))
        //        .ToList();

        //    if (ParentReferenceCode == null || ParentReferenceCode == "")
        //        return (tempBOM, 0);

        //    int nextLevel = level + 1;

        //    int nextRowIndex = rowIndex + 1;

        //    foreach (var row in children)
        //    {

        //        int childId = row.Field<int>("item_id");

        //        var subParent = BomHead.AsEnumerable()
        //            .SingleOrDefault(r => r.Field<int>("item_id") == childId);

        //        string ChildModel = ItemList.AsEnumerable()
        //                               .Where(r => r.Field<int>("id") == childId)
        //                               .Select(r => r.Field<string>("item_model"))
        //                               .FirstOrDefault();

        //        if (subParent != null)
        //        {
        //            int subBomId = subParent["id"] != DBNull.Value ? subParent.Field<int>("id") : 0;
        //            var result = GetRecursiveBOM(nextRowIndex, childId, tempBOM, templateSelected, parentReference, subBomId, childId, nextLevel, parent.Field<int>("id"));

        //            totalCost += result.subTotal;
        //        }
        //        else
        //        {
        //            decimal lineTotal = decimal.Parse(row.Field<string>("unit_price").Trim()) * row.Field<int>("bom_qty");
        //            totalCost += lineTotal;

        //            // Add leaf to tempBOM
        //            tempBOM.Rows.Add(
        //                row.Field<int>("item_id"),
        //                row.Field<string>("item_name").Trim(),
        //                nextLevel, 
        //                parent.Field<int>("id"),
        //                IncrementReferenceCode(ParentReferenceCode),
        //                ChildModel,
        //                row.Field<int>("bom_qty"),
        //                0,
        //                0,
        //                decimal.Parse(row.Field<string>("unit_price").Trim())
        //                );
        //        } 
        //    }

        //    DataTable TemplateChild = null;

        //    if (templateSelected.Rows.Count > 1)
        //    {

        //        TemplateChild = templateSelected.AsEnumerable()
        //             .Where(r => r.Field<string>("reference_code").ToString().StartsWith(ParentReferenceCode)
        //             && r.Field<int>("level") == nextLevel)
        //            .CopyToDataTable();

        //        //Already added the TemplateChild here
        //        foreach (DataRow row in TemplateChild.Rows)
        //        {

        //            if (!children.AsEnumerable().Any(r => r.Field<string>("item_name").Trim() == row.Field<string>("item_name").Trim()))
        //            {
        //                tempBOM.Rows.Add(
        //                       row.Field<int>("item_id"),
        //                       row.Field<string>("item_name").Trim(),
        //                       nextLevel,
        //                       parent.Field<int>("id"),
        //                       row.Field<string>("reference_code"),
        //                       ""
        //                       );
        //            }
        //        }

        //    }

        //    // Update the parent unit_price to total of all its descendants
        //    //1.186 is for 18% VAT
        //    decimal TotalCostWithMarkup = decimal.Parse(totalCost.ToString()) * 1.186m;
        //    tempBOM.Rows[rowIndex]["unit_price"] = TotalCostWithMarkup.ToString();
        //    return (tempBOM, totalCost);
        //}

        public static string IncrementReferenceCode(string referenceCode)
        {
            if (string.IsNullOrWhiteSpace(referenceCode))
                throw new ArgumentException("Reference code cannot be empty.");

            var parts = referenceCode.Split('.');

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
            //ComputeProjectDgv(e);
            //CellEdited?.Invoke(this, EventArgs.Empty);
            //ItemChanged?.Invoke(this, EventArgs.Empty);
            ComputeByReferenceHierarchy();
        }

        //bool isInsertControllerToMotor = false;
        DataTable wiringTable = new DataTable();
        private void setProjectWirings()
        {

            wiringTable.Columns.Add("Materials", typeof(string));
            wiringTable.Columns.Add("num", typeof(string));
            wiringTable.Columns.Add("NumberOfQtyFormat", typeof(string));
            wiringTable.Columns.Add("QtyFormat", typeof(string));
            wiringTable.Columns.Add("NoOfWiresSet", typeof(string));
            wiringTable.Columns.Add("AMPREQ", typeof(string));

            int counter = 1;
            string[] defaultWiring = { "ECB To Controller", "Conduit Pipe", "Elbow", "Coupling", "Flexible Conduit", "Straight Connector", "Controller to motor", "Ground" };
            string[] defaultQTYFormat = { "m", "pcs", "pcs", "pcs", "m", "pcs", "m", "m" };
            string[] defaultNumberOfWiresSet = { "3", "", "", "", "", "", "3", "1" };

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


            //Grouping the columns in wiring
            string[] NumberOfQtySet = { "project_wiring_num_of_wiring_set", "project_wiring_num_of_wiring_set_format" };
            string NumberOfQtySetHeaderName = "#" + Environment.NewLine + " OF QTY " + Environment.NewLine + " / SET";

            Dictionary<string, string[]> FirstGrouping = new Dictionary<string, string[]>
            {
                { NumberOfQtySetHeaderName, NumberOfQtySet }
            };

            Helpers.EnableGroupHeaders(dgv_wiring, FirstGrouping);

            string[] QtyHeader = { "project_wiring_qty", "project_wiring_qty_format" };
            string QtyHeaderName = "Project Inventory";

            Dictionary<string, string[]> SecondGrouping = new Dictionary<string, string[]>
            {
                { QtyHeaderName, QtyHeader }
            };

            Helpers.EnableGroupHeaders(dgv_wiring, SecondGrouping);
        }

        private void cmb_starting_method_SelectedIndexChanged(object sender, EventArgs e)
        {

            if (wiringTable == null || wiringTable.Rows.Count == 0)
            {
                MessageBox.Show("Wiring table is empty");
                return;
            }

            if (cmb_starting_method.Text == "WYE-DELTA CLOSED" || cmb_starting_method.Text == "WYE-DELTA OPEN")
            {
                double FLA = double.Parse(txt_FLA.Text);
                double VOLT = double.Parse(txt_VOLT.Text);
                double ampRequirement = FLA * 0.6 * 1.25;

                foreach (DataRow row in wiringTable.Rows)
                {
                    if (row["Materials"].ToString() == "Controller to motor")
                    {
                         row["AMPREQ"] = ampRequirement;
                        break;
                    }
                }
            }

            if (cmb_starting_method.Text == "DIRECT ONLINE")
            {
                double FLA = double.Parse(txt_FLA.Text);
                
                double ampRequirement = FLA * 1.25;

                foreach (DataRow row in wiringTable.Rows)
                {
                    if (row["Materials"].ToString() == "Controller to motor")
                    {
                        row["AMPREQ"] = ampRequirement;
                        break;
                    }
                }
            }

            if (cmb_starting_method.Text == "SOFT STARTER")
            {
                double FLA = double.Parse(txt_FLA.Text);
                
                foreach (DataRow row in wiringTable.Rows)
                {
                    if (row["Materials"].ToString() == "Controller to motor")
                    {
                        row["AMPREQ"] = FLA;
                        break;
                    }
                }
            }
        }

        private void ComputeWiringDGV(DataGridViewCellEventArgs e)
        {
            try
            {
                var noOfWiresCell = dgv_wiring.Rows[e.RowIndex].Cells["project_wiring_num_of_wires_set"].Value;
                var noOfQtyCell = dgv_wiring.Rows[e.RowIndex].Cells["project_wiring_num_of_wiring_set"].Value;
                var distanceTravelledCell = dgv_wiring.Rows[e.RowIndex].Cells["project_wiring_distance_travelled"].Value;
                var allowanceWireSetCell = dgv_wiring.Rows[e.RowIndex].Cells["project_wiring_allowance"].Value;
                var noOfSetsCell = dgv_wiring.Rows[e.RowIndex].Cells["project_wiring_qty"].Value;
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

                //double WiresAndQty = noOfWires * noOfQty;
                double qty = noOfQty * (distanceTravelled + allowanceWireSet);
                dgv_wiring.Rows[e.RowIndex].Cells["project_wiring_qty"].Value = qty.ToString();

                double totalQty = qty * noOfSets;
                dgv_wiring.Rows[e.RowIndex].Cells["project_wiring_total_qty"].Value = totalQty.ToString();

                decimal totalCost = (decimal)totalQty * (decimal)costs;
                dgv_wiring.Rows[e.RowIndex].Cells["project_wiring_total_cost"].Value = totalCost.ToString();

            }
            catch (Exception ex)
            {
                MessageBox.Show("Error [Compute Wiring]: " + ex.Message);
            }
        }

        private void dgv_wiring_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            ComputeWiringDGV(e);
            CellChangedWiring?.Invoke(this, EventArgs.Empty);
        }


        private void computeECBToController()
        {
            if (!string.IsNullOrWhiteSpace(txt_FLA.Text) && !string.IsNullOrWhiteSpace(txt_no_of_pump_set.Text))
            {
                if (double.TryParse(txt_FLA.Text, out double FLA) && double.TryParse(txt_no_of_pump_set.Text, out double PumpSet))
                {
                    double ECB = FLA * PumpSet;

                    foreach (DataRow row in wiringTable.Rows)
                    {
                        if (row["Materials"] != null && row["Materials"].ToString() == "ECB To Controller")
                        {
                            row["AMPREQ"] = ECB;
                            break;
                        }
                    }
                }
            }
        }


        private void txt_no_of_pump_set_TextChanged(object sender, EventArgs e)
        {
            computeECBToController();
        }

        private void checkBox_Wiring_CheckedChanged(object sender, EventArgs e)
        {
            WiringVisible(checkBox_Wiring.Checked);
        }
         
        private void WiringVisible(bool checkBox)
        {
            dgv_wiring.Visible = checkBox;
            label6.Visible = checkBox;
            txt_FLA.Visible = checkBox;
            label7.Visible = checkBox;
            txt_VOLT.Visible = checkBox;
            label58.Visible = checkBox;

        }

        private void txt_final_Click(object sender, EventArgs e)
        {
            FinalTxtBoxClicked?.Invoke(this, EventArgs.Empty);
        }

        private void dgv_final_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            FinalTxtBoxClicked?.Invoke(this, EventArgs.Empty);
        }

        private void ComputeByReferenceHierarchy()
        {

            DataTable dataSourceQuickQuote = dgv_project_items.DataSource as DataTable;

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
                foreach (DataGridViewRow row in dgv_project_items.Rows)
                {
                    if (row.Cells["reference_code"].Value?.ToString() == parentReferenceCode)
                    {
                        row.Cells["project_items_unit_price"].Value = totalUnitPrice.ToString();//Helpers.FormatAsCurrency(totalUnitPrice.ToString());
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
                decimal manDays = Convert.IsDBNull(ParentRow["man_days"]) ? 0m : Convert.ToDecimal(ParentRow["man_days"]);
                decimal laborRate = Convert.IsDBNull(ParentRow["labor_rate"]) ? 0m : Convert.ToDecimal(ParentRow["labor_rate"]);

                totalLaborCost = laborRate * manDays;

                Console.WriteLine($"Adding labor cost for parent '{parentReferenceCode}': {manDays} * {laborRate} = {totalLaborCost:C}");
            }

            decimal AllChildTotal = 0;

            // For each child, recursively sum their descendants' unit_prices
            foreach (var child in children)
            {
                string childReferenceCode = child.Field<string>("reference_code");

                // Recursively find the total for this child's descendants
                decimal ChildTotal = GetTotalUnitPriceForChildren(dt, childReferenceCode);
                AllChildTotal = ChildTotal;
                Console.WriteLine($"Adding total from child '{childReferenceCode}': 'Child Total:' {ChildTotal}': 'Total Amount:' {AllChildTotal:C}");
            }

            // Sum the unit_price for the current direct children
            decimal totalUnitPrice = children.Sum(row =>
            {
                string value = Helpers.GetCleanedPriceValue(row["unit_price"]?.ToString());
                string qty = Helpers.GetCleanedPriceValue(row["qty"]?.ToString());
                decimal NewUnitPrice = (decimal.TryParse(value, out decimal parsed) ? parsed : 0) * (decimal.TryParse(qty, out decimal qtyParsed) ? qtyParsed : 0);

                Console.WriteLine($"sum all the child  unit_price for '{row.Field<string>("reference_code")} ': {value} ' * ' {qty} ' = ' {NewUnitPrice:C}");

                return NewUnitPrice;
            });

            decimal TotalAmount = (totalLaborCost + totalUnitPrice) * 1.186m;
            //decimal TotalAmount = (totalLaborCost + totalUnitPrice);

            return TotalAmount;
        }

    }
}
