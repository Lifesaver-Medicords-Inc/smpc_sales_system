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

        // "Pending" until the Warehouse Manager approves or declines it. Only "Approved"
        // takes stock out of availability (§10.4.3); a declined ("Rejected") row stays.
        public string status { get; set; }

        // Set once the reservation reaches its quotation's VALID UNTIL. It then waits for
        // the owning sales executive or the Warehouse Manager to keep it on hold or let it
        // go (§10.4.5). The at-limit list the sales red box reads also fills the fields below.
        public DateTime? limit_reached_at { get; set; }
        public string item_name { get; set; }
        public string item_model { get; set; }
        public string document_no { get; set; }
        public string requested_by { get; set; }
    }
}
