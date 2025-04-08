using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace smpc_sales_system.Services.Sales.Models
{
    class PurchaseRequisitionOrderModel
    {
        public uint pr_order_id { get; set; }
        public uint based_id { get; set; }
        public uint item_id { get; set; }
        public float qty { get; set; }
        public string status { get; set; }
    }
}
