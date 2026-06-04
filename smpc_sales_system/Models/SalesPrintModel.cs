using smpc_sales_system.Pages.Sales;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

namespace smpc_sales_system.Models
{
    public class SalesPrintModel
    {
        public class SalesQuotationReportModel
        {
            // Basic Info
            public int id { get; set; }
            public string document_no { get; set; }
            public DateTime date { get; set; }
            public string project_name { get; set; }

            // Customer Info
            public int customer_id { get; set; }
            public string branch_name { get; set; }
            public string ship_to_address { get; set; }
            public int ship_to_id { get; set; }
            public string bill_to_address { get; set; }

            // Contact
            public string contact_1 { get; set; }
            public string contact_2 { get; set; }

            // Validity
            public string validity_days { get; set; }
            public string valid_until { get; set; }
            public string warranty { get; set; }

            // Totals
            public double gross_sales { get; set; }
            public double vat_amount { get; set; }
            public double net_sales { get; set; }
            public double percent_discount { get; set; }
            public double discounted_amount { get; set; }
            public double additional_discounted_amount { get; set; }
            public double cash_discount { get; set; }
            public double net_amount_due { get; set; }
            public double total_amount_due { get; set; }

            // Report Content
            public string inclusion { get; set; }
            public string exclusion { get; set; }
            public string termsandconditions { get; set; }

            // Nested Data
             public List<QuotationQuickModel> QuotationQuicks { get; set; } = new List<QuotationQuickModel>();
        }

        public class QuotationQuickModel
        {
            public int id { get; set; }
            public int quotation_id { get; set; }
            public int based_id { get; set; }
            public string description { get; set; }
            public double price { get; set; }
            public int quantity { get; set; }
            public double line_total { get; set; }
            public int item_id { get; set; }
            public int itemset_id { get; set; }
            public int template_id { get; set; }

            // Nested Images
            public List<QuotationSelectedImageModel> SelectedImages { get; set; } = new List<QuotationSelectedImageModel>();
        }

        public class QuotationSelectedImageModel
        {
            public int id { get; set; }
            public int quotation_quick_id { get; set; }
            public string image_url { get; set; }
            public string image_name { get; set; }
        }
        public class SalesQuotationDetailsReportModel
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
            public byte[] Image { get; set; }
        }

        public class SalesProjectQuotationDetailsReportModel
        {
            public int items_id { get; set; }
            public int bom_id { get; set; }
            public int item_id { get; set; }
            public int based_id { get; set; }
            public string reference_code { get; set; } 
            public int man_days { get; set; }
            public decimal labor_rate { get; set; }
            public string components { get; set; }
            public string model { get; set; }
            public string item_inv_type { get; set; }
            public int qty { get; set; }
            public decimal list_price_per_unit { get; set; }
            public decimal unit_price { get; set; }
            public string multiplier { get; set; }
            public decimal discount_price { get; set; }
            public decimal component_total { get; set; }
            public string notes { get; set; }
            public int template_id { get; set; }
            public byte[] Image { get; set; }
        }
    }
}
