using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace smpc_sales_system.Services.Sales.Models
{
    class SalesQuotationQuicksModel
    {
        public int id { get; set; }
        public int based_id { get; set; }
        public int bom_id { get; set; }
        public int item_id { get; set; }
        public string reference_code { get; set; }
        public string components { get; set; }
        public string model { get; set; }
        public int qty { get; set; }
        public int man_days { get; set; }
        public decimal labor_rate { get; set; }
        public string unit_of_measure { get; set; }
        public decimal list_price { get; set; }
        public decimal unit_price { get; set; }
        public string percent_discount { get; set; }
        public decimal net_discount { get; set; }
        public decimal net_total { get; set; }
        public decimal line_total { get; set; }
        public string short_description { get; set; }
    }

    class SalesQuotationSelectedImageModel
    {
        public int id { get; set; }
        public int quotation_quick_id { get; set; }
        public int image_id { get; set; }
        public bool is_selected { get; set; }

    }
}
