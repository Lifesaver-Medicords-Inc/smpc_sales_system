using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace smpc_sales_system.Services.Sales.Models
{
    class ItemBomList
    {
        public int id { get; set; }
        public int item_id { get; set; }
        public int production_qty { get; set; }
        public string production_type { get; set; }
        public string labor { get; set; }
        public int production_cost { get; set; }
    }


    class ItemBomDetails
    {

        public int id { get; set; }
        public int item_bom_id { get; set; }
        public int item_id { get; set; }
        public int size { get; set; }
        public int bom_qty { get; set; }
        public string uom_name { get; set; }
        public string item_code { get; set; }
        public string short_desc { get; set; }
        public ItemBomDetails(int id, int itemBomId, int itemId, int size, int bomQty)
        {
            this.id = id;
            this.item_bom_id = itemBomId;
            this.item_id = itemId;
            this.size = size;
            this.bom_qty = bomQty;

        }
    }
    class bom
    {
        public List<ItemBomList> Bom { get; set; }
        public List<ItemBomDetails> BomDetails { get; set; }

    }
}