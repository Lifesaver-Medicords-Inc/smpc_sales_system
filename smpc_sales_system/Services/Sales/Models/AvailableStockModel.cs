using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace smpc_sales_system.Services.Sales.Models
{
    // Mirrors ERP_API's inventory_models.AvailableStockView. One row per item: physical
    // stock (summed across every bin), how much of it is already reserved by other
    // quotation lines, and what's actually free to promise (physical - reserved).
    public class AvailableStockModel
    {
        public int item_id { get; set; }
        public int physical { get; set; }
        public int reserved { get; set; }
        public int available { get; set; }
    }
}
