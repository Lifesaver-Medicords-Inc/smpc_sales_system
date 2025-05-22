using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace smpc_sales_system.Services.Sales.Models
{
    class CRMModel
    {
        public uint based_id { get; set; } 
        public string tag { get; set; } 
        public string date { get; set; }   
        public string remark { get; set; }  
    }
    class CRMViewModel
    {
        public int id { get; set; }
        public string branch_name { get; set; }
        public string number { get; set; }
        public string name { get; set; }
        public string email { get; set; }
        public string tag { get; set; }
        public string date { get; set; }
        public string remark { get; set; }
        public int crm_id { get; set; }
        public string sales_id { get; set; }
    }
}
 