using smpc_sales_system.Services.Sales.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace smpc_sales_app.Models
{
    // For Item Listing Modal
    class ItemParent

    {
        public int id { get; set; }
        public int item_name_id{ get; set; }
        public string item_code { get; set; }
        public string item_name { get; set; }
    }

    class itemlist
    {
        public List<ItemParent> items { get; set; }

    }
}
