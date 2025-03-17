using smpc_app.Services.Helpers;
using smpc_inventory_app.Services.Helpers;
using smpc_sales_app.Services.Helpers;
using smpc_sales_system.Services.Sales.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace smpc_sales_system.Services.Sales
{
    class ItemListBomServices
    {
        static string url = "/setup/bom";
        static string url2 = "/setup/bom/item_list";
        
        public static async Task<DataTable> GetAsDatatable()
        {
            var response = await RequestToApi<ApiResponseModel<List<ItemBomList>>>.Get(url);
            DataTable entityType = JsonHelper.ToDataTable(response.Data);
            return entityType;
        }
        public static async Task<DataTable> GetAsDatatableDetails()
        {
            var response = await RequestToApi<ApiResponseModel<List<ItemBomDetails>>>.Get(url2);
            DataTable entityType = JsonHelper.ToDataTable(response.Data);
            return entityType;
        }
    }
}
