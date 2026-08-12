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
    }
}
