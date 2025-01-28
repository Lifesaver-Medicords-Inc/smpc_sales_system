using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace smpc_sales_app.Services.Sales.Models
{
    class OrderDetailsModel
    {
        public int order_details_id { get; set; }
        public int based_id { get; set; }
        public string qty { get; set; }
        public string item_code { get; set; }
        public string item_description { get; set; }
        public string delivery_preference { get; set; }
        public float list_price { get; set; }
        public float total_price { get; set; }
        public string status { get; set; }
    }
}
