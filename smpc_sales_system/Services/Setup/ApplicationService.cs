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
using smpc_sales_app.Services.Sales.Models;

namespace smpc_sales_app.Services.Sales
{
    internal static class ApplicationService
    {
        static string url = "/sales/application";

        // GET
        public static async Task<DataTable> GetAsDatatable()
        {
            var response = await RequestToApi<ApiResponseModel<List<ApplicationModel>>>.Get(url);
            DataTable applicationItems = JsonHelper.ToDataTable(response.Data);
            return applicationItems;
        }

        public static async Task<ApplicationModel[]> GetApplication()
        {
            var response = await RequestToApi<ApiResponseModel<ApplicationModel[]>>.Get(url);
            var applicationData = response.Data;

            return applicationData;
        }

      
        // POST
        public static async Task<ApiResponseModel> Insert(Dictionary<string, dynamic> data)
        {
            var response = await RequestToApi<ApiResponseModel>.Post(url, data);
            return response;
        }

        // DELETE
        public static async Task<bool> Delete(Dictionary<string, dynamic> data)
        {
            var response = await RequestToApi <ApiResponseModel<ApplicationModel>>.Delete(url, data);
            bool isSuccess = response.Success;

            return isSuccess;
        }

        // UPDATE
        public static async Task<ApiResponseModel> Update(Dictionary<string,dynamic> data)
        {
            var response = await RequestToApi<ApiResponseModel>.Put(url, data);
            return response;
        }
    }
}
