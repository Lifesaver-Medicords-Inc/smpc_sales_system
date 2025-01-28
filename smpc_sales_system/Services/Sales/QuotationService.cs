
using smpc_app.Services.Helpers;
using smpc_inventory_app.Services.Helpers;
using smpc_sales_app.Services.Helpers;
using smpc_sales_app.Services.Sales.Models;
using smpc_sales_system.Services.Sales.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace smpc_sales_app.Services.Sales
{
    static class QuotationService
    {
        static string url = "/sales/quotation";


        // GET
        public static async Task<SalesQuotationList> GetQuotations()
        {
            var response = await RequestToApi<ApiResponseModel<SalesQuotationList>>.Get(url);
            SalesQuotationList quotationData = response.Data;
            return quotationData;
        }

        //public static async Task<DataTable> GetAsDataTable()
        //{
        //    var response = await RequestToApi<ApiResponseModel<List<ItemModel>>>.Get(url);
        //    DataTable itemList = JsonHelper.ToDataTable(response.Data);
        //    return itemList;
        //}



        public static async Task<SalesQuotationModel[]> GetQuotation()
        {
            var response = await RequestToApi<ApiResponseModel<SalesQuotationModel[]>>.Get(url);
            var quotationData = response.Data;

            return quotationData;
        }

    

        // POST
        public static async Task<ApiResponseModel> Insert(Dictionary<string, dynamic> data)
        {
            var response = await RequestToApi<ApiResponseModel>.Post(url, data);
            return response;
        }

        // DELETE
        public static async Task<Boolean> Delete(Dictionary<string, dynamic> data)
        {
            var response = await RequestToApi<ApiResponseModel<bool>>.Delete(url, data);
            var isSuccess = response.Success;

            return isSuccess;
        }

        // UPDATE
        public static async Task<ApiResponseModel> Update(Dictionary<string, dynamic> data)
        {
            var response = await RequestToApi<ApiResponseModel>.Put(url, data);
            return response;
        }
    }
}
