using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace smpc_sales_system.Services.Sales.Models
{
    class BpiCustomerModel
    {
        public int general_based_id { get; set; }
        public string branch_name { get; set; }
        public string customer_code { get; set; }
    }
    class BpiCustomer
    {
        public List<BpiCustomerModel> bpi { get; set; }
    }
}
