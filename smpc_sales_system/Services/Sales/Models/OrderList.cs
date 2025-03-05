using smpc_sales_app.Services.Sales.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace smpc_sales_system.Services.Sales.Models
{
    class OrderList
    {
        public List<OrderModel> order { get; set; }
        public List<OrderDetailsModel> sales_order_details { get; set; }
    }
}