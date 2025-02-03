using smpc_app.Services.Helpers;
using smpc_inventory_app.Services.Helpers;
using smpc_inventory_app.Services.Setup;
using smpc_sales_app.Services;
using smpc_sales_app.Services.Helpers;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace smpc_sales_app.Services.Setup
{
   internal static  class PaymentTermsServices
    {

        static readonly string url = "/setup/payment_terms";

        public static async Task<DataTable> GetAsDatatable()
        {
            var response = await RequestToApi<ApiResponseModel<List<PaymentTermsModel>>>.Get(url);
            DataTable itemBrands = JsonHelper.ToDataTable(response.Data);

            return itemBrands;
        }

        public static async Task<ApiResponseModel> Insert(Dictionary<string, dynamic> data)
        {
            var response = await RequestToApi<ApiResponseModel>.Post(url, data);

            return response;
        }


        public static async Task<Boolean> Delete(Dictionary<string, dynamic> data)
        {
            var response = await RequestToApi<ApiResponseModel<bool>>.Delete(url, data);
            var isSuccess = response.Success;

            return isSuccess;
        }

        public static async Task<ApiResponseModel> Update(Dictionary<string, dynamic> data)
        {

            var response = await RequestToApi<ApiResponseModel>.Put(url, data);
            return response;
        }
    }
}
