using smpc_sales_system.Services.Sales.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace smpc_inventory_app.Services.Setup.Model.Item
{
    class ItemModel
    {

        public int id { get; set; }
        public int item_name_id { get; set; }
        public int item_model_id { get; set; }
        public string item_code { get; set; }
        public string short_desc { get; set; }
        public int item_brand_id { get; set; }
        public int unit_of_measure_id { get; set; }
        public bool is_inventory_item { get; set; }
        public bool is_sales_item { get; set; }
        public bool is_purchase_item { get; set; }
        public string item_name { get; set; }
        public string item_model { get; set; }
        public string item_class { get; set; }
        public string item_brand { get; set; }
        public string unit_of_measure { get; set; }
    }
    class Items
    {
        public List<ItemModel> items { get; set; }
        public List<ItemSpecsModel> itemspecs {get; set;}
        public List<AdditionalSpecsModel> additionalspecs { get; set; }
        public List<ItemImageModel> ItemImages { get; set; }

    }
}
