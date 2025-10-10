using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace smpc_sales_system.Models
{
    public class CurrentUserModel
    { 
        public int id { get; set; }
        public string employee_id { get; set; } 
        public string first_name { get; set; } 
        public string last_name { get; set; }
        public string password { get; set; }
        public string department { get; set; }
        public string position_id { get; set; }
        public UserPermissionModel permissions { get; set; }
        public PositionModel position { get; set; }

    }

   

}
