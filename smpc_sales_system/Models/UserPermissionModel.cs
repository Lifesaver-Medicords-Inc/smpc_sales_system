using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace smpc_sales_system.Models
{
    public class UserPermissionModel
    {
        public int id { get; set; }
        public int user_id { get; set; }
        public bool can_create { get; set; }
        public bool can_update { get; set; }
        public bool can_delete { get; set; }
    }
}
