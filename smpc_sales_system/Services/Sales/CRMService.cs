using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using smpc_app.Services.Helpers;
using smpc_inventory_app.Services.Helpers;
using smpc_sales_app.Services;
using smpc_sales_app.Services.Helpers;
using smpc_sales_app.Services.Sales;
using smpc_sales_app.Services.Sales.Models;
using smpc_sales_system.Services.Sales.Models;

namespace smpc_sales_system.Services.Sales
{
    class CRMService
    {
        static string url = "/sales/crm";
        static string url2 = "/sales/crm/table";
        // GET - Returns data as DataTable
        public static async Task<DataTable> GetAsDatatable()
        {
            var response = await RequestToApi<ApiResponseModel<List<CRMViewModel>>>.Get(url);
            DataTable crmItems = JsonHelper.ToDataTable(response.Data);
            return crmItems;
        }
        public static async Task<DataTable> GetCRM()
        {
            var response = await RequestToApi<ApiResponseModel<List<CRMModel>>>.Get(url2);
            DataTable crmItems = JsonHelper.ToDataTable(response.Data);
            return crmItems;
        } 
        // POST - Insert data
        public static async Task<ApiResponseModel> Insert(Dictionary<string, dynamic> data)
        {
            var response = await RequestToApi<ApiResponseModel>.Post(url, data);
            return response;
        }

        // UPDATE - Update data
        public static async Task<ApiResponseModel> Update(Dictionary<string, dynamic> data)
        {
            var response = await RequestToApi<ApiResponseModel>.Put(url, data);
            return response;
        }
    }
}
