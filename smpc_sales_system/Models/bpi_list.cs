using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace smpc_sales_system.Models
{
        class bpi_list
        {
            public List<Bpi> bpi { get; set; }
            public List<BpiGeneral> general { get; set; }
            public List<BpiContacts> contacts { get; set; }
            public List<BpiAddress> address { get; set; }
            public List<BpiItems> items { get; set; }

        }
}
