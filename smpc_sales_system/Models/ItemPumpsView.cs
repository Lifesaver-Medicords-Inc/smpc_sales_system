using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace smpc_sales_system.Models
{
    public class ItemPumpsView
    {
        public int item_id { get; set; }
        public int item_name_id { get; set; }
        public string item_name { get; set; }
        public string template_name { get; set; }
        public string item_title { get; set; }
        public int item_value { get; set; }
    }
    public class ItemPumpsViewList
    {
        public List<ItemPumpsView> ItemPumpsView { get; set; }
    }
}