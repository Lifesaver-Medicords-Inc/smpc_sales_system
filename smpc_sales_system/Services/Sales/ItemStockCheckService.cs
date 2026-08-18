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

        // Batched counterpart to GetAvailableStock(int) above - one request for every
        // item's available stock instead of one request per distinct item on the grid.
        // item_id=0 already means "every item" server-side (see GetAvailableStock's own
        // remark); this just keeps the whole list back instead of only its first row.
        // Used by RefreshAllStockIndicators (Quotation.cs / ItemSetUC.cs) to prefetch once
        // per grid (re)bind instead of firing one fire-and-forget request per row, which
        // was both slow and, under enough concurrent rows, occasionally tripped
        // "An error occurred while sending the request" once per row that lost the race.
        //
        // silent: true - this backs a purely informational indicator (the INV. column /
        // shortage flag), so a failed prefetch shouldn't interrupt the user with an
        // exception dialog; rows just fall back to no indicator for whichever items
        // didn't make it into the cache (see RequestToApi.SendRequestAsync's silent param).
        public static async Task<List<AvailableStockModel>> GetAllAvailableStock()
        {
            var response = await RequestToApi<ApiResponseModel<List<AvailableStockModel>>>.Get($"{url}?item_id=0", silent: true);
            return response?.Data ?? new List<AvailableStockModel>();
        }

        // Reports whether a quotation line currently has a manual reservation - called
        // once per line both when the stock-check modal opens (explicit, wants a real
        // error if it fails) and from the background per-row INV. indicator refresh
        // (RefreshStockIndicator in Quotation.cs / ItemSetUC.cs, silent - see silent
        // param below). Returns null when there's nothing reserved for that line, not an
        // error.
        //
        // silent: true skips RequestToApi's own MessageBox on failure, same as
        // GetAllAvailableStock above. RefreshStockIndicator already wraps its own call to
        // this in a try/catch specifically to "swallow rather than pop a MessageBox for
        // every row on a flaky network" - but that catch never actually ran, because
        // RequestToApi.SendRequestAsync catches the exception itself and shows its own
        // box first, then returns default(T) instead of rethrowing. With enough rows
        // refreshing in the background across a long session (RefreshAllStockIndicators
        // fires on every grid (re)bind, with nothing stopping multiple runs overlapping),
        // that meant one popup per failed row, repeating every time the background
        // refresh ran again - silent: true actually lets that swallow-not-popup intent
        // take effect.
        public static async Task<StockReservationModel> GetReservation(int sourceId, string sourceType = "sales_quotation", bool silent = false)
        {
            var response = await RequestToApi<ApiResponseModel<StockReservationModel>>.Get(
                $"{reservation_url}?source_type={sourceType}&source_id={sourceId}", silent: silent);
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
