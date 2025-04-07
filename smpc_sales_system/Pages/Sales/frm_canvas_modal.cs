using smpc_app.Services.Helpers;
using smpc_sales_app.Services.Helpers;
using smpc_sales_system.Models;
using smpc_sales_system.Services.Sales;
using smpc_sales_system.Services.Sales.Models;
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
    public partial class frm_canvas_modal : Form
    {

        string items_id { get; set; }
        DataTable bpi { get; set; }
        DataTable bpi_items { get; set; }
        public frm_canvas_modal(string item_id, DataTable dt, DataTable dt2)
        {
            InitializeComponent();
            this.items_id = item_id;
            this.bpi_items = dt2;
            fetchBpiSuppliers();
        }

        private async void fetchCanvasSheet()
        {

        }

        private async void fetchBpiSuppliers()
        {
            var data = await ProjectService.GetSuppliers();
            var view_data = await ProjectService.GetCanvasView();

            //var canvas_data = await ProjectService.Get;
            List<BpiSuppliers> suppliersList = data.BpiSuppliers;
            List<SalesCanvasView> viewList = view_data.sales_canvas_sheet_view;

            //if (viewList == null || !viewList.Any())
            //{
            var filteredData = suppliersList
                .Where(s => s.item_id.ToString() == this.items_id)
                .Select(s => new 
                {
                    supplier_code = s.supplier_code
                })
                .ToList();
            dataGridView1.DataSource = filteredData;
            //    
            //}
        }

        private void dataGridView1_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            computeLoop(e.RowIndex);
        }

        private void computeLoop(int rowIndex)
        {
            double net_price, unit_price;
            double discount;

            DataGridViewRow row = this.dataGridView1.Rows[rowIndex];

           
            object netPriceValue = row.Cells["NetPrice"].Value;
            object discountValue = row.Cells["Discount"].Value;

            Console.WriteLine($"Row {rowIndex} | Net Price: {netPriceValue} | Discount: {discountValue}");

            bool isNetPriceValid = double.TryParse(netPriceValue?.ToString(), out net_price);
            bool isDiscountValid = double.TryParse(discountValue?.ToString(), out discount);

            if (isNetPriceValid && isDiscountValid)
            {
                discount = discount / 100;
                unit_price = net_price * (1 - discount);

                   
                row.Cells["UnitPrice"].Value = unit_price;
            }
            else
            {
                row.Cells["UnitPrice"].Value = net_price;
            }
        }

        public Dictionary<string, object> GetDGVData()
        {
            var source = Helpers.ConvertDataGridViewToDataTable(dataGridView1);
            List<SalesCanvasModel> canvas = new List<SalesCanvasModel>();

            foreach (DataRow item in source.Rows)
            {
                if (item == null) continue;

                int.TryParse(item["supplier_id"]?.ToString(), out int supplierId);
                int.TryParse(items_id, out int itemId);
                decimal.TryParse(item["NetPrice"]?.ToString(), out decimal netPrice);
                decimal.TryParse(item["UnitPrice"]?.ToString(), out decimal unitPrice);
                int.TryParse(item["LeadTime"]?.ToString(), out int leadTime);

                var canvasdata = new SalesCanvasModel
                {
                    supplier_based_id = supplierId,
                    item_based_id = itemId,
                    net_price = netPrice,
                    discount = item["Discount"]?.ToString() ?? string.Empty,
                    unit_price = unitPrice,
                    validity = item["validity_col"]?.ToString(),
                    lead_time = leadTime
                };

                // **Exclude entries where net_price or unit_price is 0**
                if (canvasdata.net_price > 0 && canvasdata.unit_price > 0)
                {
                    canvas.Add(canvasdata);
                }
            }

            Dictionary<string, dynamic> data = new Dictionary<string, dynamic>();
            data["SalesCanvasSheet"] = canvas;
            return data;
        }


        private async void button1_Click(object sender, EventArgs e)
        {
            var response = await ProjectService.InsertCanvas(GetDGVData());

            if (response.Success)
            {
                MessageBox.Show("Success");
            }
        }
    }
}
