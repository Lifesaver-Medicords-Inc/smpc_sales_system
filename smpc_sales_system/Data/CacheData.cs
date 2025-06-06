using smpc_inventory_app.Services.Setup;
using smpc_sales_system.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace smpc_sales_app.Data
{
    public class CacheData
    {
        public static DataTable PaymentTerms { get; set; } = new DataTable();
        public static DataTable ApplicationSetup { get; set; } = new DataTable();
        public static DataTable UoM { get; set; } = new DataTable();

        public static DataTable ItemList { get; set; } = new DataTable();
        public static DataTable ShipTypeSetup { get; set; } = new DataTable();
        public static DataTable Orders { get; set; } = new DataTable();

        public static String SessionToken { get; set; } = "";
        public static CurrentUserModel CurrentUser { get; set; } = null;



        public static Dictionary<string, dynamic> Cached_Advanced_Conditions_Data { get; set; }
        public static Dictionary<string, dynamic> Cached_Content_Data { get; set; }




    }
}
