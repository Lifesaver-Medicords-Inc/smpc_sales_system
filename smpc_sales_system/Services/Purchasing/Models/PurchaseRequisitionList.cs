using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace smpc_sales_system.Services.Sales.Models
{
    class PurchaseRequisitionList
    {
        public List<PurchaseRequisitionModel> purchase_requisition { get; set; }
        public List<PurchaseRequisitionOrderModel> purchasing_purchase_requisition_orders { get; set; }
    }
}
