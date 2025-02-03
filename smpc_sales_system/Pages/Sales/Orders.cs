using smpc_app.Services.Helpers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using smpc_app.Services.Helpers;
using smpc_sales_app.Data;
using smpc_sales_app.Services.Sales;
using smpc_sales_system.Services.Sales.Models;
using smpc_sales_app.Services.Helpers;

namespace smpc_sales_app.Pages.Sales
{
    public partial class Orders : UserControl
    {
        int SelectedRow = 0;
        public Orders()
        {
            InitializeComponent();
            
        }
        public DataTable OrderList { get; set; } = new DataTable();
        public DataTable DetailsList { get; set; } = new DataTable();
        private async void FetchData()
        {
            OrderList data = await OrderService.GetOrders();

            OrderList = JsonHelper.ToDataTable(data.order);
            DetailsList = JsonHelper.ToDataTable(data.orderdetails);

            // Add a default row to DetailsList
            //if (DetailsList != null)
            //{
            //    DataRow defaultRow = DetailsList.NewRow();
            //    defaultRow["based_id"] = OrderList.Rows[SelectedRow]["order_id"];
            //    defaultRow["qty"] = "ADD NEW ITEM";
            //    defaultRow["has_stocks"] = DBNull.Value;
            //    DetailsList.Rows.Add(defaultRow);
            //}

            if (data != null)
            {
                bind(true);
                CalculateTotalPrice();
            }
        }
        private void dgv_order_sales_CellClick(object sender, DataGridViewCellEventArgs e)
        {
           
        }
        private void bind(bool isBind = false)
        {
            if (isBind)
            {
                //dgv_quick_quote_details.DataSource = dataView;
                // dgv_quick_quote_details.DataSource = childList;
                Panel[] pnlList = { pnl_header, pnl_footer };
                Helpers.BindControls(pnlList, OrderList, SelectedRow);

                DataView dataview = new DataView(this.DetailsList);
                dataview.RowFilter = "based_id = '" + this.OrderList.Rows[this.SelectedRow]["order_id"].ToString() + "'";
                dgv_order_sales.DataSource = dataview;

                foreach (DataGridViewRow row in dgv_order_sales.Rows)
                {
                    var hasStocksValue = row.Cells["has_stocks"].Value;

                    if (hasStocksValue == DBNull.Value || hasStocksValue == null)
                    {
                        foreach (DataGridViewCell cell in row.Cells)
                        {
                            if (cell.OwningColumn.Name != "qty")
                            {
                                cell.Style.BackColor = Color.LightGray;
                            }
                        }
                    }
                    else
                    {
                        bool hasStocks = Convert.ToBoolean(hasStocksValue);  
                        if (!hasStocks)
                        {
                            row.Cells["has_stocks"].Style.BackColor = Color.Red;
                        }
                        else
                        {
                            row.Cells["has_stocks"].Style.BackColor = Color.White;
                        }
                    }
                }
            }
        }

        private void CalculateTotalPrice()
        {
            decimal total = 0.0m; 
            foreach (DataGridViewRow row in dgv_order_sales.Rows)
            {
                if (row.Cells["total_price"].Value != null)
                {
                    
                    decimal totalPrice;
                    if (decimal.TryParse(row.Cells["total_price"].Value.ToString(), out totalPrice))
                    {
                        total += totalPrice;  
                    }
                }
            }

            
            txt_total.Text = total.ToString("0.00"); 
        }

        private void Orders_Load(object sender, EventArgs e)
        {
            FetchData();
            
            // Helpers.LoadDirectory("D:\\LIFESAVER\\LIFESAVER\\TEST", treeview_sales);
        }

        private void treeview_sales_NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            if (e.Node.Tag != null)
            {
                // Check if the clicked node is a file (has a Tag property)
                string filePath = e.Node.Tag.ToString();

                if (File.Exists(filePath))
                {
                    try
                    {
                        // Open the file using the default associated application
                        Process.Start(filePath);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error opening file: {ex.Message}");
                    }
                }
            }
        }

        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            //// Ensure we are not clicking on header row or invalid rows
            //if (e.RowIndex >= 0)
            //{
            //    DataGridViewRow clickedRow = dgv_order_sales.Rows[e.RowIndex];

            //    // Check if the clicked row is the default row (last row)
            //    if (clickedRow.Index == dgv_order_sales.Rows.Count - 1)
            //    {
            //        // Show the modal dialog for the default row
            //        ItemModal itemModal = new ItemModal();
            //        DialogResult r = itemModal.ShowDialog();

            //        if (r == DialogResult.OK)
            //        {
            //            Dictionary<string, string> result = itemModal.GetResult();

            //            if (result != null)
            //            {
            //                string code = "";
            //                string name = "";
            //                string unit_price = "";
            //                string short_desc = "N/A";

            //                result.TryGetValue("name", out name);
            //                result.TryGetValue("code", out code);
            //                result.TryGetValue("unitprice", out unit_price);
            //                result.TryGetValue("short_desc", out short_desc);

            //                DataRow newRow = DetailsList.NewRow();
            //                newRow["based_id"] = OrderList.Rows[SelectedRow]["order_id"];
            //                newRow["item_code"] = code;
            //                newRow["total_price"] = unit_price;
            //                newRow["item_description"] = short_desc;

            //                //newRow["qty"] = 1;
            //                //newRow["unit_measure"] = "COD";
                            
            //                DetailsList.Rows.InsertAt(newRow, DetailsList.Rows.Count - 1);
            //                CalculateTotalPrice();
            //            }
            //        }
            //    }
            //}
        }

        private void panel3_Paint(object sender, PaintEventArgs e)
        {

        }

        private void btn_next_Click(object sender, EventArgs e)
        {
            int rowCount = OrderList.Rows.Count;
            if (SelectedRow < rowCount - 1)
            {
                SelectedRow++;
                FetchData();
            }
        }

        private void btn_prev_Click_1(object sender, EventArgs e)
        {
            if (SelectedRow >= 1)
            {
                SelectedRow--;
                FetchData();
            }
        }
    }
}
