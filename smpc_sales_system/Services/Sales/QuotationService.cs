using smpc_app.Services.Helpers;
using smpc_inventory_app.Services.Helpers;
using smpc_sales_app.Services.Helpers;
using smpc_sales_app.Services.Sales.Models;
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

        public static async Task<DataTable> GetAsDatatable()
        {
            var response = await RequestToApi<ApiResponseModel<List<QuotationModel>>>.Get(url);

            DataTable itemClass = JsonHelper.ToDataTable(response.Data);

            return itemClass;
        }

        public static async Task<bool> Insert(Dictionary<string, dynamic> data)
        {
            var response = await RequestToApi<ApiResponseModel<QuotationModel[]>>.Post(url, data);
            var itemClass = response.Success;

            return itemClass;
        }
    }
}
