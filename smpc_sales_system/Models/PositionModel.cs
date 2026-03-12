using smpc_inventory_app.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace smpc_sales_system.Models
{
    public class PositionModel
    {
        public int id { get; set; }
        public string name { get; set; }
        public ICollection<PositionAccessModel> access { get; set; } = new List<PositionAccessModel>();
        public ICollection<CurrentUserModel> users { get; set; } = new List<CurrentUserModel>();
    }
}
