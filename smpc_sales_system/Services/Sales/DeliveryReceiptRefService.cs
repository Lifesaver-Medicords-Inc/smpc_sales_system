using System.Collections.Generic;
using System.Threading.Tasks;
using smpc_app.Services.Helpers;
using smpc_sales_app.Services.Helpers;
using smpc_sales_system.Services.Sales.Models;

namespace smpc_sales_app.Services.Sales
{
    // Reuses the existing GET /api/delivery-receipts directly - no new Go
    // endpoint. Unlike Sales Invoice, this returns a bare array (Go's
    // GetDeliveryReceiptsHandler responds with the list straight in "data",
    // not a named-wrapper object), and it has no customer filter param
    // (confirmed against the handler - only "id" and "sales_order_id" query
    // params exist), so the picker fetches everything and filters/searches
    // client-side, same as the Sales Invoice picker.
    internal class DeliveryReceiptRefService
    {
        static string url = "/delivery-receipts";

        public static async Task<List<DeliveryReceiptRefModel>> GetDeliveryReceipts()
        {
            var response = await RequestToApi<ApiResponseModel<List<DeliveryReceiptRefModel>>>.Get(url);
            return response?.Data ?? new List<DeliveryReceiptRefModel>();
        }
    }
}
