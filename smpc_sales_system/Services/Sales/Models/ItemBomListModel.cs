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
            public int production_qty { get; set; }
            public string production_type { get; set; }
            public string labor { get; set; }
        }

   
        public class ItemBomDetails
        {
            public int id { get; set; }
            public int item_bom_id { get; set; }
            public int item_id { get; set; }
            public int size { get; set; }
            public int bom_qty { get; set; }
           
        }

        public class BomList
        {
            public List<ItemBomList> BomParent { get; set; }
            public List<ItemBomDetails> setup_bom_details { get; set; }
        }
    }
}
