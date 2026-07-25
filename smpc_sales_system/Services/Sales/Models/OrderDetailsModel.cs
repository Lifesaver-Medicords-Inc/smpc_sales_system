using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace smpc_sales_app.Services.Sales.Models
{
    class OrderDetailsModel
    {
        public int order_details_id { get; set; }
        public int quotation_quick_id { get; set; }
        public int qty { get; set; }
        public int based_id { get; set; }
        public int item_id { get; set; }
        public string delivery_preference { get; set; }
        public string status { get; set; }
        public bool has_stocks { get; set; }
        public string item_code { get; set; }
        public string item_description { get; set; }
        public string numbering { get; set; }
        public float list_price { get; set; }
        public float total_price { get; set; }
        public int? allocated_qty { get; set; }
        // Tab name of the itemset (project quotation "header", e.g. "A1") this line item
        // belongs to. Only populated for orders converted from a project quotation - blank
        // otherwise. Used by SalesPrintModal to re-insert the dynamic header rows into the
        // printed Sales Order (they're never saved as their own row - see Orders.cs's save
        // handler for why).
        public string item_set_header { get; set; }

    }
}
