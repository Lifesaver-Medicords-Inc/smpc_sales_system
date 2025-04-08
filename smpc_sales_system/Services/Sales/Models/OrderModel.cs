using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace smpc_sales_system.Services.Sales.Models
{
    class OrderModel
    {
        public uint order_id { get; set; }
		public uint ship_type_id { get; set; }
		public uint bill_to_id { get; set; }
		public uint ship_to_id { get; set; }
		public uint customer_id { get; set; }
		public uint quotation_id { get; set; }
		public uint payment_terms_id { get; set; }
		public string approved_by { get; set; }
		public uint approved_by_id { get; set; }
		public string doc { get; set; }
		public uint ref_po { get; set; }
		public string date { get; set; }
		public string delivery_date { get; set; }
		public string document_no { get; set; }
		public string status { get; set; }
		public string receiver { get; set; }
		public string sales_executive { get; set; }
		public string contact_no { get; set; }
		public string remarks { get; set; }
		public string project_name { get; set; }
		public float gross_sales { get; set; }
		public float vat_amount { get; set; }
		public float total_amount_due { get; set; }
	}
}
