using System.Collections.Generic;

namespace smpc_sales_system.Services.Sales.Models
{
    // Read-only view of a Sales Invoice, for Sales Return's "Sales Invoice"
    // reference-doc picker only - never written back to. Mirrors
    // accounting_models.SalesInvoiceContent (ERP_API/models/accounting_models
    // /accounting_sales_invoice_model.go), trimmed to the fields the picker
    // and the grid-population step actually need. Same "reuse the owning
    // app's endpoint directly instead of building a duplicate" precedent as
    // Purchase Return's InvoiceReceiptModel.cs in the Inventory app.
    internal class SalesInvoiceRefModel
    {
        public uint id { get; set; }
        public int doc_no { get; set; }
        public string doc_date { get; set; }
        public uint customer_id { get; set; }
        public string customer { get; set; }
        public string customer_code { get; set; }
        public string customer_address { get; set; }
        public string sales_person { get; set; }
        public string currency { get; set; }
        public string reference_doc_so { get; set; }
    }

    // Mirrors accounting_models.SalesInvoiceDetailsContent - the fields
    // already carry unit_price directly, so this is the simple reference
    // path (contrast DeliveryReceiptRefModel, which needs a second join).
    internal class SalesInvoiceRefDetailsModel
    {
        public uint id { get; set; }
        public uint sales_invoice_id { get; set; }
        public uint sales_order_details_id { get; set; }
        public uint item_id { get; set; }
        public string item_code { get; set; }
        public string item_description { get; set; }
        public uint item_qty { get; set; }
        public string item_uom { get; set; }
        public double unit_price { get; set; }
        public double total_cost { get; set; }
    }

    // GET /accounting/sales_invoice response shape - matches
    // accounting_models.SalesInvoiceGet.
    internal class SalesInvoiceRefGet
    {
        public List<SalesInvoiceRefModel> sales_invoice { get; set; } = new List<SalesInvoiceRefModel>();
        public List<SalesInvoiceRefDetailsModel> sales_invoice_details { get; set; } = new List<SalesInvoiceRefDetailsModel>();
    }
}
