using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace smpc_sales_system.Services.Sales.Models
{
    class SalesQuotationModel
    {
        public int id { get; set; }
        public int customer_id { get; set; }
        public int application_id { get; set; }
        public int payment_terms_id { get; set; }
        public int ship_type_id { get; set; }
        public int ship_to_id { get; set; }
        public int bill_to_id { get; set; }
        public string purpose { get; set; }
        public string date { get; set; }
        public string validity_days { get; set; }
        public string valid_until { get; set; }
        public string warranty { get; set; }
        public string address_to { get; set; }
        public string thru { get; set; }
        public double gross_sales { get; set; }
        public double vat_amount { get; set; }
        public double net_sales { get; set; }
        public double sub_total_before_discount { get; set; }
        public double percent_discount { get; set; }
        public double sub_total { get; set; }
        public double cash_discount { get; set; }
        public double net_amount_due { get; set; }
        public bool is_vat { get; set; }
        public double vat_percent { get; set; }
        public string contact_1 { get; set; }
        public string contact_2 { get; set; }
        public string document_no { get; set; }
        public string version_no { get; set; }
        public string created_by { get; set; }
        public double discounted_amount { get; set; }
    }
}