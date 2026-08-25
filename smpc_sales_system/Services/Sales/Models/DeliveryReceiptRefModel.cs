using System.Collections.Generic;

namespace smpc_sales_system.Services.Sales.Models
{
    // Read-only view of a Delivery Receipt, for Sales Return's "Delivery
    // Receipt" reference-doc picker. Mirrors dispatching_models
    // .DeliveryReceiptContent (ERP_API/models/dispatching_model
    // /delivery_receipt_model.go). Reuses the existing GET
    // /api/delivery-receipts endpoint directly - no new Go work.
    //
    // Unlike a Sales Invoice line, a Delivery Receipt Items row carries NO
    // price at all (confirmed against DeliveryReceiptItemsContent - it has
    // qty/uom/item identity only). Resolving a price for a DR-sourced line
    // requires a second fetch against CustomerSoRefService, joined on
    // sales_order_details_id - see SalesReturn.cs's DR-picker handler.
    internal class DeliveryReceiptRefModel
    {
        public uint id { get; set; }
        public int doc_no { get; set; }
        public uint customer_id { get; set; }
        public string customer_name { get; set; }
        public string customer_code { get; set; }
        public string address { get; set; }
        public uint sales_order_id { get; set; }
        public string sales_executive { get; set; }
        public string date { get; set; }
        public string delivery_date { get; set; }
        public List<DeliveryReceiptItemRefModel> delivery_receipt_items { get; set; } = new List<DeliveryReceiptItemRefModel>();
    }

    // Mirrors dispatching_models.DeliveryReceiptItemsContent exactly - note
    // there is deliberately no unit_price field here, it does not exist on
    // the source model.
    internal class DeliveryReceiptItemRefModel
    {
        public uint id { get; set; }
        public uint delivery_receipt_id { get; set; }
        public uint sales_order_details_id { get; set; }
        public uint item_id { get; set; }
        public int qty { get; set; }
        public string unit_of_measure { get; set; }
        public string item_code { get; set; }
        public string item_description { get; set; }
        public string serial_no { get; set; }
    }
}
