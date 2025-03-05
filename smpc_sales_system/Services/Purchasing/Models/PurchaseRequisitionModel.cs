using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace smpc_sales_system.Services.Sales.Models
{
    class PurchaseRequisitionModel
    {
		public uint pr_id { get; set; }
		public string date_request { get; set; }
		public string date_required { get; set; }
		public string request_by { get; set; }
		public string department { get; set; }
		public string contact_no { get; set; }
		public string doc_no { get; set; }
		public string justification { get; set; }
		public string remarks { get; set; }
		public string approval { get; set; }
		public bool is_approved { get; set; }
		public string status { get; set; }
	}
}
