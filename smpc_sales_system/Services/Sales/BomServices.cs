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
    internal static class BomServices
    {
        // Get the BOMs as a DataTable

        static string url = "/setup/bom/parent_detail";
        static string url2 = "/setup/bom/child_detail";
        public static async Task<DataTable> GetBomsAsDatatable()
        {
            var response = await RequestToApi<ApiResponseModel<List<BomHead>>>.Get(url);
            DataTable boms = JsonHelper.ToDataTable(response.Data);
            return boms;
        }
        public static async Task<DataTable> GetBomsdetailAsDatatable()
        {
            var response = await RequestToApi<ApiResponseModel<List<BomDetail>>>.Get(url2);
            DataTable boms = JsonHelper.ToDataTable(response.Data);
            return boms;
        }
    }
}
