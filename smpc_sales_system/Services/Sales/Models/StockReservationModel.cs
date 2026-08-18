using System;

namespace smpc_sales_system.Services.Sales.Models
{
    // Mirrors ERP_API's inventory_models.StockReservation. Only ever comes back from
    // GET /inventory/item_stocks/reservations, which returns null (not an error) when
    // the line being checked has no reservation - see ItemStockCheckService.GetReservation.
    public class StockReservationModel
    {
        public int id { get; set; }
        public int item_id { get; set; }
        public int qty { get; set; }
        public string source_type { get; set; }
        public int source_id { get; set; }
        public int quotation_id { get; set; }
        public DateTime reserved_at { get; set; }
        public DateTime? expires_at { get; set; }

        // "Pending" until a dispatcher/inventory manager approves or rejects it (see
        // ERP_API's ReservationApprovalAccessCode) - still holds the stock either way,
        // Rejected is the only status that doesn't (and a rejected row won't come back
        // here at all, since GetReservation only returns each line's newest one and a
        // rejected reservation is functionally "not reserved" going forward).
        public string status { get; set; }
    }
}
