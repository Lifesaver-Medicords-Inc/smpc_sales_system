using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace smpc_sales_system.Services.Sales.Models
{
    class OrderModel
    {
        public int order_id { get; set; }
        public string customer { get; set; }
        public string code { get; set; }
        public string delivery_to { get; set; }
        public string bill_to { get; set; }
        public string document_no { get; set; }
        public string date { get; set; }
        public string delivery_date { get; set; }
        public string payment_terms { get; set; }
        public string ship_type { get; set; }
        public string ref_doc { get; set; }
        public int ref_id { get; set; }
        public string status { get; set; }
        public string sales_executive { get; set; }
        public string receiver { get; set; }
        public string contact_no { get; set; }
        public string remarks { get; set; }
        public float vat { get; set; }
        public float net_of_vat { get; set; }
        public float total_amount_due { get; set; }
        public string approved_by { get; set; }
        public int approved_by_id { get; set; }
    }
}
