using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace smpc_inventory_app.Services.Setup.Model.Item
{
    class ItemSpecsModel
    {
        public int id { get; set; }
        public int based_id { get; set; }
        public string template { get; set; }
        public string title { get; set; }
        public string value { get; set; }
    }
    class ItemSpecs
    {
        public List<ItemSpecsModel> itemspecs { get; set; }
    }
}
