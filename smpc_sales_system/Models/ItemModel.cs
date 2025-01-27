using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace smpc_sales_app.Models
{
    // For Item Listing Modal
    class ItemModel
    {
        public int id { get; set; }
        public string item_code { get; set; }
       
        public string item_name_id { get; set; }
        public float unit_price { get; set; }
        public string long_desc { get; set; }
        public string short_desc { get; set; }
    }
}
