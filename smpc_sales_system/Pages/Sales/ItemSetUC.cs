using smpc_app.Services.Helpers;
using smpc_sales_app.Pages.Sales;
using smpc_sales_app.Services.Helpers;
using smpc_sales_system.Models;
using smpc_sales_system.Services.Sales;
using smpc_sales_system.Services.Setup;
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
    public partial class ItemSetUC : UserControl
    {
        public event EventHandler DataChangedConditions;
        public event EventHandler DataChangedContent;
        public event EventHandler CellChangedProject;
        public event EventHandler CellChangedWiring;
        public event EventHandler ButtonClicked;
        public event EventHandler CellClicked;
        public event EventHandler CellClickedCanvas;
        public event EventHandler FinalTxtBoxClicked;

        public ItemSetUC()
        {
            InitializeComponent();

            // methods for event changes
            AttachTextChangedEventConditions(pnl_advanced_conditions);
            AttachTextChangedEventContent(pnl_project_content);
            AttachCellValuechangedEventConditions(dgv_project_items);
            AttachCellValuechangedEventContent(dgv_wiring);
            dgv_project_items.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            setProjectWirings();
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

        private void AttachCellValuechangedEventConditions(DataGridView dgv)
        {
            dgv.CellValueChanged += DataGridView_CellValueChangedConditions;
        }

        private void AttachCellValuechangedEventContent(DataGridView dgv)
        {
            dgv.CellValueChanged += DataGridView_CellValueChangedContent;
        }

        private void DataGridView_CellValueChangedConditions(object sender, DataGridViewCellEventArgs e)
        {
            //timer_push_content.Stop();
            //timer_push_content.Start();
        }

        private void DataGridView_CellValueChangedContent(object sender, DataGridViewCellEventArgs e)
        {
            //timer_push.Stop();
            //timer_push.Start();
        }

        private void TextBox_TextChangedContent(object sender, EventArgs e)
        {
            //timer_push_content.Stop();
            //timer_push_content.Start();
        }
        private void TextBox_TextChangedConditions(object sender, EventArgs e)
        {
            //timer_push.Stop();
            //timer_push.Start();
        }


      
        //
        //    TIMERS
        //
        private void timer_push_Tick(object sender, EventArgs e)
        {
            timer_push.Stop();
            DataChangedConditions?.Invoke(this, EventArgs.Empty);
        }

        private void timer_push_content_Tick(object sender, EventArgs e)
        {
            timer_push_content.Stop();
            DataChangedContent?.Invoke(this, EventArgs.Empty);
        }

        private void timer_push_cell_project_Tick(object sender, EventArgs e)
        {
            timer_push_cell_project.Stop();
            CellChangedProject?.Invoke(this, EventArgs.Empty);
        }
        private void timer_push_cell_wiring_Tick(object sender, EventArgs e)
        {
            timer_push_cell_wiring.Stop();
            CellChangedWiring?.Invoke(this, EventArgs.Empty);
        }




        //
        //  GETTERS
        //
        public Dictionary<string, dynamic> GetAdvancedConditionsData()
        {
            Panel[] pnl_adv = { pnl_advanced_conditions};
            var data = Helpers.GetControlsValues(pnl_adv);
            Dictionary<string, dynamic> conditions = new Dictionary<string, dynamic>();

            if (data.ContainsKey("id") && data["id"] is string customerIdStr)
            {
                if (int.TryParse(customerIdStr, out int Id))
                {
                    data["id"] = Id;
                }
                else
                {
                    MessageBox.Show("Invalid ID");
                }
            }
            conditions["sales_project_content_advanced_condition"] = data;

            return conditions;
        }

        public Dictionary<string, dynamic> GetProjectContentsData()
        {
            Panel[] pnl_content = { pnl_project_content };
            var data = Helpers.GetControlsValues(pnl_content);
            Dictionary<string, dynamic> contents = new Dictionary<string, dynamic>();

            if (data.ContainsKey("id") && data["id"] is string customerIdStr)
            {
                if (int.TryParse(customerIdStr, out int Id))
                {
                    data["id"] = Id;
                }
                else
                {
                    MessageBox.Show("Invalid ID");
                }
            }
            contents["sales_project_content"] = data;

            return contents;
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
                    materials = item[0]?.ToString() ?? string.Empty,
                    amp_req = item[1]?.ToString() ?? string.Empty,
                    wire_req = item[2]?.ToString() ?? string.Empty,
                    description = item[3]?.ToString() ?? string.Empty,
                    num_of_wires_set = item[4]?.ToString() ?? string.Empty,
                    num_of_qty_set = item[5]?.ToString() ?? string.Empty,
                    distance_travelled_set = item[6]?.ToString() ?? string.Empty,
                    allowance_wire_set = item[7]?.ToString() ?? string.Empty,
                    qty = int.TryParse(item[8]?.ToString(), out int qtyVal) ? qtyVal : 0,
                    num_of_sets = item[9]?.ToString() ?? string.Empty,
                    total_qty = int.TryParse(item[10]?.ToString(), out int totalQtyVal) ? totalQtyVal : 0,
                    cost = decimal.TryParse(item[11]?.ToString(), out decimal costVal) ? costVal : 0m,
                    total_cost = decimal.TryParse(item[12]?.ToString(), out decimal totalCostVal) ? totalCostVal : 0m,
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
                    item_id = int.TryParse(item[0]?.ToString(), out int tempItemId) ? tempItemId : 0,
                    bom_id = int.TryParse(item[3]?.ToString(), out int tempBomId) ? tempBomId : 0,
                    node_id = int.TryParse(item[4]?.ToString(), out int tempNodeId) ? tempNodeId : 0,
                    node_name = item[5]?.ToString() ?? string.Empty,
                    parent_node_id = int.TryParse(item[6]?.ToString(), out int tempParentNode) ? tempParentNode : 0,
                    node_order = int.TryParse(item[7]?.ToString(), out int tempNodeOrder) ? tempNodeOrder : 0,
                    node_type = item[8]?.ToString() ?? string.Empty,
                    components = item[10]?.ToString() ?? string.Empty,
                    model = item[11]?.ToString() ?? string.Empty,
                    item_inv_type = item[12]?.ToString() ?? string.Empty,
                    qty = int.TryParse(item[13]?.ToString(), out int qty) ? qty : 0,
                    multiplier = item[14]?.ToString() ?? string.Empty,
                    discount_price = double.TryParse(item[15]?.ToString(), out double discountPrice) ? discountPrice : 0.0,
                    list_price_per_unit = double.TryParse(item[16]?.ToString(), out double listPrice) ? listPrice : 0.0,
                    component_total = double.TryParse(item[17]?.ToString(), out double total) ? total : 0.0,
                };

                items.Add(spi);
            }
            Dictionary<string, object> data = new Dictionary<string, object>();

            data["sales_project_items"] = items;
            return data;
        }





        private static class ProjectQuoteDGV
        {
            public static int QTY = 13;
            public static int MULTIPLIER = 14;
            public static int DISCOUNT = 15;
            public static int LIST_PRICE = 16;
            public static int NET_TOTAL = 17 ;
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
                                    Console.WriteLine("Each discount factor must be between 0 and 1.");
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
                            price = 1 - (adjustmentPercent / 100);
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
        }

        private void ComputeProjectDgv(DataGridViewCellEventArgs e)
        {
            try
            {
                var qty_cell = dgv_project_items.Rows[e.RowIndex].Cells[ProjectQuoteDGV.QTY].Value;
                var list_price_cell = dgv_project_items.Rows[e.RowIndex].Cells[ProjectQuoteDGV.LIST_PRICE].Value;
                var multiplier_cell = dgv_project_items.Rows[e.RowIndex].Cells[ProjectQuoteDGV.MULTIPLIER].Value == null ? "0" :
                    dgv_project_items.Rows[e.RowIndex].Cells[ProjectQuoteDGV.MULTIPLIER].Value.ToString();

                if (qty_cell != null && list_price_cell != null)
                {
                    decimal listPrice;
                    decimal qty = decimal.Parse(this.dgv_project_items.Rows[e.RowIndex].Cells[ProjectQuoteDGV.QTY].Value.ToString());
                    bool listPriceValid = decimal.TryParse(this.dgv_project_items.Rows[e.RowIndex].Cells[ProjectQuoteDGV.LIST_PRICE].Value.ToString(), out listPrice);
                    string multiplier = this.dgv_project_items.Rows[e.RowIndex].Cells[ProjectQuoteDGV.MULTIPLIER].Value == null ? "0" :
                        dgv_project_items.Rows[e.RowIndex].Cells[ProjectQuoteDGV.MULTIPLIER].Value.ToString();

                    DGVProjectComputation projectComputation = new DGVProjectComputation(qty, listPrice, multiplier);
                    projectComputation.ComputeProjectQuote();

                    this.dgv_project_items.Rows[e.RowIndex].Cells[ProjectQuoteDGV.DISCOUNT].Value = projectComputation.Discount;
                    this.dgv_project_items.Rows[e.RowIndex].Cells[ProjectQuoteDGV.NET_TOTAL].Value = projectComputation.NetTotal.ToString("F2");

                    ProjectComputationLoop();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("err" + ex);
            }
        }
        public Dictionary<string, dynamic> ProjectComputationLoop()
        {
            Quotation q = new Quotation();
            double gross_sales = 0, vat_amount = 0, net_sales = 0;
            double percent_discount = 0;
            double net_amount_due = 0, total_amount_due = 0;
            double cash_discount = q.GetCashDiscount();
            const double VAT_RATE = 0.12; 

            foreach (DataGridViewRow row in this.dgv_project_items.Rows)
            {
                if (row.Cells[ProjectQuoteDGV.QTY].Value != null &&
                    row.Cells[ProjectQuoteDGV.LIST_PRICE].Value != null)
                {
                    // Calculate gross amount (qty * list price)
                    decimal qty = decimal.Parse(row.Cells[ProjectQuoteDGV.QTY].Value.ToString());
                    decimal listPrice = decimal.Parse(row.Cells[ProjectQuoteDGV.LIST_PRICE].Value.ToString());
                    double rowGross = (double)(qty * listPrice);
                    gross_sales += rowGross;

                    // Get net total (after discount)
                    if (row.Cells[ProjectQuoteDGV.NET_TOTAL].Value != null &&
                        !String.IsNullOrEmpty(row.Cells[ProjectQuoteDGV.NET_TOTAL].Value.ToString()))
                    {
                        double netTotal = double.Parse(row.Cells[ProjectQuoteDGV.NET_TOTAL].Value.ToString());
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
            data.Add("gross_sales", Helpers.MoneyFormat(gross_sales));
            data.Add("vat_amount", Helpers.MoneyFormat(vat_amount));
            data.Add("net_sales", Helpers.MoneyFormat(net_sales));
            data.Add("percent_discount", percent_discount.ToString("0.00") + "%");
            data.Add("cash_discount", Helpers.MoneyFormat(cash_discount));
            data.Add("net_amount_due", Helpers.MoneyFormat(net_amount_due));
            data.Add("total_amount_due", Helpers.MoneyFormat(total_amount_due));
            return data;
       }  

        private void computeWiringDgv(DataGridViewCellEventArgs e)
        {
            try
            {


                //var wire_set_cell = dgv_wiring.Rows[e.RowIndex].Cells[].Value;
                //var qty_set_cell = dgv_wiring.Rows[e.RowIndex].Cells[].Value;
                //var qty_cell = dgv_wiring.Rows[e.RowIndex].Cells[2].Value;
                //var cost_cell = dgv_wiring.Rows[e.RowIndex].Cells[1].Value;
                

            }
            catch (Exception ex)
            {
                MessageBox.Show("ERROR" + ex);
            }
        }

        //
        // SETTERS
        //
        public void SetAdvancedPanelData(DataTable dt)
        {
            Panel[] pnls = { pnl_advanced_conditions};
            Helpers.BindControls(pnls, dt);
        }

        public void SetContentsPanelData(DataTable dt)
        {
            Panel[] pnls = { pnl_project_content };
            Helpers.BindControls(pnls, dt);  
        }

        public async void SetProjectItemsData(DataTable dt)
        {

            dgv_project_items.Rows.Clear();

            dgv_project_items.Columns[8].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;

            Font boldFont = new Font(dgv_project_items.DefaultCellStyle.Font, FontStyle.Bold);
            Font normalFont = new Font(dgv_project_items.DefaultCellStyle.Font, FontStyle.Regular);

            Dictionary<int, ProjectTemplateChildModel> nodeLookup = dt.AsEnumerable()
                          .ToDictionary(
                              row => row.Field<int>("node_id"),
                              row => new ProjectTemplateChildModel
                              {
                                  node_id = row.Field<int>("node_id"),
                                  node_name = row.Field<string>("node_name"),
                                  parent_node_id = row.Field<int>("parent_node_id"),
                                  node_order = row.Field<int>("node_order"),
                                  node_type = row.Field<string>("node_type")
                              }
                          );

            var rootNodes = dt.AsEnumerable()
                          .Where(row => row.Field<int>("parent_node_id") == 0)
                          .OrderBy(row => row.Field<int>("node_order"))
                          .ToList();

            foreach (var rootNode in rootNodes)
            {
                int parentRowIndex = dgv_project_items.Rows.Add();

                DataGridViewRow newRow = dgv_project_items.Rows[parentRowIndex];

                newRow.Cells[5].Value = rootNode.Field<string>("node_name");
                newRow.Cells[4].Value = rootNode.Field<int>("node_id");
                newRow.Cells[6].Value = rootNode.Field<int>("parent_node_id");
                newRow.Cells[7].Value = rootNode.Field<int>("node_order");
                newRow.Cells[8].Value = rootNode.Field<string>("node_type");
                newRow.Cells[10].Value = "▶ " + rootNode.Field<string>("node_name");


                newRow.Cells[10].Style.BackColor = Color.LightCoral;
                newRow.Cells[10].Style.Font = boldFont;

                // Recursively add child nodes
                AddChildNodesFromDb(rootNode.Field<int>("node_id"), dt, nodeLookup, 1);

            }

            dgv_project_items.Columns[1].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
        }

        public void SetFetchedItemData(DataTable dt)
        {
            if (dt.Columns.Contains("id"))
            {
                dt.Columns.Remove("id");
            }

            if (!dt.Columns.Contains("node_type"))
            {
                MessageBox.Show("Column 'node_type' not found in DataTable.");
                return;
            }

            dgv_project_items.DataSource = dt;
            dgv_project_items.ReadOnly = false;
            dgv_project_items.EnableHeadersVisualStyles = false; // Allow styling

            // Apply colors after data is fully loaded
            dgv_project_items.DataBindingComplete += (s, e) => ApplyRowStyles();
        }

        private void ApplyRowStyles()
        {
            foreach (DataGridViewRow row in dgv_project_items.Rows)
            {
                if (!row.IsNewRow) 
                {
                    DataGridViewCell cell = row.Cells[9];
                    int nodeTypeColumnIndex = dgv_project_items.Columns["node_type"].Index;
                    string nodeType = row.Cells[nodeTypeColumnIndex].Value?.ToString().Trim();

                    MessageBox.Show($"Processing Row: {nodeType}");

                    row.DefaultCellStyle.BackColor = Color.White; // Reset

                    if (nodeType == "Parent")
                    {
                        cell.Style.BackColor = Color.Yellow;
                        MessageBox.Show("red");
                    }
                    else if (nodeType == "Leaf")
                    {
                        cell.Style.BackColor = Color.LightCoral;
                        MessageBox.Show("yellow");
                    }
                    else if (string.IsNullOrWhiteSpace(nodeType))
                    {
                        cell.Style.BackColor = Color.LightGreen;
                        MessageBox.Show("none");
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

                newRow.Cells[5].Value = childNode.Field<string>("node_name");     
                newRow.Cells[4].Value = childNode.Field<int>("node_id");            
                newRow.Cells[6].Value = childNode.Field<int>("parent_node_id");      
                newRow.Cells[7].Value = childNode.Field<int>("node_order");         
                newRow.Cells[8].Value = childNode.Field<string>("node_type");

                newRow.Cells[10].Value = indent + childNode.Field<string>("node_name");


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


        public void SetFinalPumpData (string FLA, string Voltage)
        {
            txt_FLA.Text = FLA.ToString();
            txt_VOLT.Text = Voltage.ToString();
        }

        public void SetComponentData(int index, string itemid, string itemName, string size, string model)
        {
           
            dgv_project_items.Rows.Insert(index);

           
            DataGridViewRow newRow = dgv_project_items.Rows[index];

           
            newRow.Cells[0].Value = itemid;  
            newRow.Cells[8].Value = itemName; 
            newRow.Cells[9].Value = size;    
                                            
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

        // for wiring soon
        private void ItemSetUC_Load(object sender, EventArgs e)
        {
            
        }

        private void textBox64_MouseClick(object sender, MouseEventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
            ButtonClicked?.Invoke(this, EventArgs.Empty);
        }

        private void dgv_project_items_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
           
        }
        int index { get; set; }

        public int GetIndex()
        {
            return index;
        }

        private void dgv_project_items_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.ColumnIndex == 9)
            {
                index = e.RowIndex;
                CellClicked?.Invoke(this, EventArgs.Empty);
            }
          

            if (e.ColumnIndex == 3)
            {

            }
        }


        public EventHandler CellEdited;
        private void dgv_project_items_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
             ComputeProjectDgv(e);
             CellEdited?.Invoke(this, EventArgs.Empty);
        }

        //bool isInsertControllerToMotor = false;
        DataTable wiringTable = new DataTable();
        private void setProjectWirings()
        {
            //if (isInsertControllerToMotor)
            //{

            //}
            wiringTable.Columns.Add("Materials", typeof(string));
            wiringTable.Columns.Add("num", typeof(string));
            wiringTable.Columns.Add("AMP REQ.", typeof(string));

            int counter = 1;
            string[] defaultWiring = { "ECB To Controller", "Conduit Pipe", "Elbow", "Coupling", "Flexible Conduit", "Straight Connector", "Controller to motor", "Ground" };

            foreach (string value in defaultWiring)
            {
               DataRow row = wiringTable.NewRow();
               row["Materials"] = value;
               row["num"] = counter.ToString();
               wiringTable.Rows.Add(row);
               counter++;
            }

          
            bs_project_wiring.DataSource = wiringTable;
            dgv_wiring.DataSource = bs_project_wiring;

           
        }

        private void setDefaultVal()
        {
            DataTable dt = new DataTable();
            dt.Columns.Add("", typeof(string));

            string[] defaultValue = { "T&C LABOR", "BONDS" };

            foreach (string value in defaultValue)
            {
                DataRow row = dt.NewRow();
                row[""] = value;
                dt.Rows.Add(row);
            }

            dataGridView1.DataSource = dt;

        }

        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

        }

        private void label58_Click(object sender, EventArgs e)
        {

        }

        private void textBox50_Click(object sender, EventArgs e)
        {
            FinalTxtBoxClicked?.Invoke(this, EventArgs.Empty);
        }

        private void label7_Click(object sender, EventArgs e)
        {

        }

        private void textBox9_TextChanged(object sender, EventArgs e)
        {

        }

        private void cmb_starting_method_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txt_FLA.Text) || string.IsNullOrWhiteSpace(txt_VOLT.Text))
            {
                MessageBox.Show(
                        "Pump selection is required to proceed.",
                        "Missing Pump Selection",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                errorProvider1.SetError(txt_final, "Pump selection is required!");
                txt_final.Focus();

                return;
            }

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
                        row["AMP REQ."] = ampRequirement;
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
                        row["AMP REQ."] = ampRequirement;
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
                        row["AMP REQ."] = FLA;
                        break;
                    }
                }
            }



        }

        private void txt_final_TextChanged(object sender, EventArgs e)
        {

        }

        private void ComputeWiringDGV(DataGridViewCellEventArgs e)
        {
            try
            {
                var noOfWiresCell = dgv_wiring.Rows[e.RowIndex].Cells[4].Value;
                var noOfQtyCell = dgv_wiring.Rows[e.RowIndex].Cells[5].Value;
                var distanceTravelledCell = dgv_wiring.Rows[e.RowIndex].Cells[6].Value;
                var allowanceWireSetCell = dgv_wiring.Rows[e.RowIndex].Cells[7].Value;
                var noOfSetsCell = dgv_wiring.Rows[e.RowIndex].Cells[9].Value;
                var costCell = dgv_wiring.Rows[e.RowIndex].Cells[11].Value;

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
                      


                double qty = noOfQty * (distanceTravelled + allowanceWireSet);
                this.dgv_wiring.Rows[e.RowIndex].Cells[8].Value = qty.ToString();


                double totalQty = qty * noOfSets;
                this.dgv_wiring.Rows[e.RowIndex].Cells[10].Value = totalQty.ToString();

                decimal totalCost = (decimal)totalQty * (decimal)costs;
                this.dgv_wiring.Rows[e.RowIndex].Cells[12].Value = totalCost.ToString();

            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message);
            }
        }





        private void dgv_wiring_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {

            ComputeWiringDGV(e);
   
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
                            row["AMP REQ."] = ECB;
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
    }
}
