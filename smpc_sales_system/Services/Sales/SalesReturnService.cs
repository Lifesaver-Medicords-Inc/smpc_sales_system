using System.Collections.Generic;
using System.Threading.Tasks;
using smpc_app.Services.Helpers;
using smpc_sales_app.Services.Helpers;
using smpc_sales_system.Services.Sales.Models;

namespace smpc_sales_app.Services.Sales
{
    // Client for /api/sales-returns (own top-level Go route group, item 1.1
    // of Phase 1 - see sales_return_services.SalesReturnService on the API
    // side). The wrapper shape ({"sales_return": [...], "sales_return_details":
    // [...]}) doesn't match ApiResponseModel<T>'s "data IS T" assumption, so
    // this needs the same custom Get/Create/Approve pattern already used for
    // Credit Memo and Purchase Return's client services.
    internal class SalesReturnService
    {
        static string url = "/sales-returns";

        public static async Task<SalesReturnGet> GetSalesReturns()
        {
            var response = await RequestToApi<ApiResponseModel<SalesReturnGet>>.Get(url);
            var result = response?.Data ?? new SalesReturnGet();
            // models.SalesReturnGet's slice fields carry no `omitempty` tag on
            // the Go side, so a zero-row result marshals as explicit JSON
            // null, not [] - Newtonsoft then overwrites this class's own
            // "= new List<>()" defaults with null. Guard here once instead of
            // at every call site.
            if (result.sales_return == null) result.sales_return = new List<SalesReturnModel>();
            if (result.sales_return_details == null) result.sales_return_details = new List<SalesReturnDetailsModel>();
            return result;
        }

        public static async Task<ApiResponseModel<SalesReturnBody>> CreateSalesReturn(SalesReturnBody body)
        {
            return await RequestToApi<ApiResponseModel<SalesReturnBody>>.Post(url, body);
        }

        public static async Task<ApiResponseModel<object>> ApproveSalesReturn(uint id)
        {
            return await RequestToApi<ApiResponseModel<object>>.Post($"{url}/{id}/approve", new { });
        }
    }
}
