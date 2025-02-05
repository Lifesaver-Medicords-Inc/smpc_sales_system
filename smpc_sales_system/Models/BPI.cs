using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace smpc_sales_system.Models
{
    class Bpi
    {

        public int id { get; set; }

        //public string name { get; set; }
    }

    class BpiGeneral
    {

        public int based_id { get; set; }
        public string branch_name { get; set; }
        public string customer_code { get; set; }
    }

    class BpiContacts
    {
        public int contacts_based_id { get; set; }
        public string number { get; set; }
    }

    class BpiAddress
    {
        public int address_based_id { get; set; }
        public string location { get; set; }
    }
}
