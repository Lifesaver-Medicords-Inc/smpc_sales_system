using smpc_inventory_app.Services.Setup;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace smpc_sales_app.Data
{
    public static class CacheData
    {
        public static DataTable PaymentTerms { get; set; } = new DataTable();
        public static DataTable ApplicationSetup { get; set; } = new DataTable();

        public static DataTable ItemList { get; set; } = new DataTable();
        public static DataTable ShipTypeSetup { get; set; } = new DataTable();
    }
}
