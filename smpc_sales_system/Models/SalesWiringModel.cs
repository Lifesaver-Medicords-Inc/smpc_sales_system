using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace smpc_sales_system.Models
{
    public class SalesWiringModel
    {
        public dynamic id { get; set; }
        public int based_id { get; set; }
        public string materials { get; set; }
        public string amp_req { get; set; }
        public string wire_amp { get; set; }
        public string description {get; set;}
        public string num_of_wires_set { get; set; }
        public string num_of_qty_set { get; set; }
        public string distance_travelled_set { get; set; }
        public string allowance_wire_set { get; set; }
        public int qty { get; set; }
        public string num_of_sets { get; set; }
        public int total_qty { get; set; }
        public decimal cost { get; set; }
        public decimal total_cost { get; set; }
    }
}
