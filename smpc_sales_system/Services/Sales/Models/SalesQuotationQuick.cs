using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace smpc_sales_system.Services.Sales.Models
{
    class SalesQuotationQuicks
    {
        public int id { get; set; }
        public int based_id { get; set; }
        public int item_id { get; set; }
        public int item_name_id { get; set; }
        public int item_class_id { get; set; }
        public int qty { get; set; }
        public int unit_id { get; set; }
        public decimal unit_price { get; set; }
        public string percent_discount { get; set; }
        public decimal net_discount { get; set; }
        public decimal net_total { get; set; }
        public decimal line_total { get; set; }
    }
}
