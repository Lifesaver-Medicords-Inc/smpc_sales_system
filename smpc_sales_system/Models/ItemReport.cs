using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace smpc_sales_system.Models
{
    public class ItemReport
    {
        public int id { get; set; }
        public string image { get; set; }
        public int item_name_id { get; set; }
        public int item_model_id { get; set; }
        public string catalogue_year { get; set; }
        public string item_code { get; set; }
        public string short_desc { get; set; }
        public int item_class_id { get; set; }
        public int item_brand_id { get; set; }
        public int unit_of_measure_id { get; set; }
        public string trade_type_id { get; set; }
        public string trade_type_names { get; set; }
        public string item_tangibility_type { get; set; }
        public bool? is_stop_selling { get; set; }
        public float price { get; set; }
        public string item_name { get; set; }
        public string item_model { get; set; }
        public string item_class { get; set; }
        public string item_brand { get; set; }
        public string unit_of_measure { get; set; }
    }
}
