using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace smpc_sales_system.Services.Sales.Models
{
    class BomClass
    {
        public List<BomHead> bom_head { get; set; }
        public List<BomDetail> bom_details { get; set; }
    }

    class BomHead
    {
        public int id { get; set; }
        public int item_id { get; set; }
        public string general_name { get; set; }
        public string item_model { get; set; }
        public string item_code { get; set; }
        public int production_qty { get; set; }
        public string production_type { get; set; }
        public string labor { get; set; }
        public float production_cost { get; set; }
    }

    class BomDetail
    {
        public int id { get; set; }
        public int item_bom_id { get; set; }
        public int item_id { get; set; }
        public string size { get; set; }
        public int bom_qty { get; set; }
        public string item_code { get; set; }
        public string short_desc { get; set; }
        public string uom_name { get; set; }
        public int unit_price { get; set; }
    }
}
