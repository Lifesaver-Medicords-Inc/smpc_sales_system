using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace smpc_inventory_app.Services.Setup.Model.Item
{
    class AdditionalSpecsModel
    {
        public int id { get; set; }
        public int based_id { get; set; }
        public string item_code { get; set; }
        public string suction_pressure { get; set; }
        public string driver_type { get; set; }
        public string motor_enclosure { get; set; }
        public string motor_manufacturer { get; set; }
        public string service_factor { get; set; }
        public string liquid_type { get; set; }
        public float volume { get; set; }
        public float weight { get; set; }
        public string long_description { get; set; }
    }
    class AdditionalSpecs
    {
        public List<AdditionalSpecsModel> additionalspecs { get; set; }
    }
}
