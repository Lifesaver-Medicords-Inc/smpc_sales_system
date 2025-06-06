using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace smpc_sales_system.Services.Sales.Models
{
    class ItemBomListModel
    {
        public class ItemBomList
        {
            public int id { get; set; }
            public int item_id { get; set; }
            public string general_name { get; set; }
            public string item_model { get; set; }
            public int production_qty { get; set; }
            public string production_type { get; set; }
            public decimal production_cost { get; set; }
            public string labor { get; set; }
        }

   
        public class ItemBomDetails
        {
            public int id { get; set; }
            public int item_bom_id { get; set; }
            public int item_id { get; set; }
            public string size { get; set; }
            public int bom_qty { get; set; }
            public string uom_name { get; set; }
            public string item_name { get; set; }
            public string unit_price { get; set; }
           
        }

        public class BomList
        {
            public List<ItemBomList> bom_head { get; set; }
            public List<ItemBomDetails> bom_details { get; set; }
        }
    }
}
