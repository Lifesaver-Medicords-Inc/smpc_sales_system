using smpc_app.Services.Helpers;
using smpc_sales_app.Services.Helpers;
using smpc_sales_system.Services.Sales.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace smpc_sales_app.Services.Sales
{
    // Backs the per-item stock checker on Quick Quote / Project Quotation (the INV.
    // column + its stock-check modal). Calls ERP_API's GET /inventory/item_stocks/available,
    // which is physical stock minus whatever's already held by other quotations' soft
    // reservations - see stock_reservation_service.go.
    static class ItemStockCheckService
    {
        static string url = "/inventory/item_stocks/available";
        static string reservation_url = "/inventory/item_stocks/reservations";

        // itemId = 0 would return every item - callers here always pass a real item id
        // since this is checked one grid row at a time.
        public static async Task<AvailableStockModel> GetAvailableStock(int itemId)
        {
            var response = await RequestToApi<ApiResponseModel<List<AvailableStockModel>>>.Get($"{url}?item_id={itemId}");

            var list = response?.Data;
            if (list == null || list.Count == 0)
            {
                // No stock/reservation rows at all for this item yet - treat as zero
                // rather than leaving callers to null-check.
                return new AvailableStockModel { item_id = itemId, physical = 0, reserved = 0, available = 0 };
            }

            return list[0];
        }

        // Reports whether a quotation line currently has a manual reservation - called
        // once per line when the stock-check modal opens, since nothing reserves a line
        // automatically anymore (see the RESERVE checkbox on StockCheckModal). Returns
        // null when there's nothing reserved for that line, not an error.
        public static async Task<StockReservationModel> GetReservation(int sourceId, string sourceType = "sales_quotation")
        {
            var response = await RequestToApi<ApiResponseModel<StockReservationModel>>.Get(
                $"{reservation_url}?source_type={sourceType}&source_id={sourceId}");
            return response?.Data;
        }

        // Places the soft hold for a quotation line - the RESERVE checkbox on
        // StockCheckModal being checked by a sales rep/manager. This is the only place a
        // reservation gets created from the UI now; creating/editing a quotation line no
        // longer reserves stock on its own (see quick_quotation_service.go). expiresAt is
        // the quotation's own ValidUntil, formatted "yyyy-MM-dd" - pass null if it isn't
        // known, but that means the periodic expiry sweep will never clean this up on
        // its own.
        public static async Task CreateReservation(int itemId, int qty, int sourceId, int quotationId, DateTime? expiresAt, string sourceType = "sales_quotation")
        {
            var data = new Dictionary<string, dynamic>
            {
                { "item_id", itemId },
                { "qty", qty },
                { "source_type", sourceType },
                { "source_id", sourceId },
                { "quotation_id", quotationId },
                { "expires_at", expiresAt?.ToString("yyyy-MM-dd") ?? "" }
            };

            await RequestToApi<ApiResponseModel>.Post(reservation_url, data);
        }

        // Manually releases the soft hold placed on a quotation line (see the RESERVE
        // checkbox on StockCheckModal being unchecked). There's no automatic re-create -
        // re-reserving means checking RESERVE again.
        public static async Task ReleaseReservation(int sourceId, string sourceType = "sales_quotation")
        {
            await RequestToApi<ApiResponseModel>.Delete(
                $"{reservation_url}?source_type={sourceType}&source_id={sourceId}",
                new Dictionary<string, object>());
        }
    }
}
