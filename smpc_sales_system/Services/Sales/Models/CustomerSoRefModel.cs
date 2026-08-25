using System.Collections.Generic;

namespace smpc_sales_system.Services.Sales.Models
{
    // Mirrors accounting_models.InvoiceSOView/InvoiceSODetailView (ERP_API
    // /models/accounting_models/accounting_invoice_so_view_model.go) - the
    // same SP-backed view Accounting's own SalesInvoicePage.cs already uses
    // to turn a delivered SO/DR into priced Sales Invoice lines
    // (sp_GetSalesOrderInvoice / sp_GetSalesOrderDetailsInvoice via
    // GET /api/accounting/customer_so/:customer_id).
    //
    // Sales Return reuses it for the OPPOSITE reason: a Delivery Receipt
    // line carries no price of its own (see DeliveryReceiptRefModel's
    // comment), and this is the only existing place in the schema where a
    // DR line's price can be recovered - via the shared
    // sales_order_details_id key. Only the details side's unit_price/
    // total_cost are actually needed; the parent (InvoiceSOView) fields are
    // kept for completeness/debugging only.
    internal class CustomerSoRefModel
    {
        public uint sales_order_id { get; set; }
        public string so_number { get; set; }
        public int dr_number { get; set; }
        public string doc_date { get; set; }
        public string customer_name { get; set; }
        public string sales_person { get; set; }
        public double total_sales { get; set; }
    }

    internal class CustomerSoRefDetailModel
    {
        public uint sales_order_details_id { get; set; }
        public uint sales_order_id { get; set; }
        public uint item_id { get; set; }
        public string item_code { get; set; }
        public string item_description { get; set; }
        public string item_uom { get; set; }
        public uint item_qty { get; set; }
        public double discount { get; set; }
        public double unit_price { get; set; }
        public double total_cost { get; set; }
        public string date_deliver { get; set; }
    }

    // GET /accounting/customer_so/:customer_id response - an anonymous
    // struct on the Go side ({"sales_order_view": [...],
    // "sales_order_details_view": [...]}), given a name here since C# needs
    // one to deserialize into.
    internal class CustomerSoRefGet
    {
        public List<CustomerSoRefModel> sales_order_view { get; set; } = new List<CustomerSoRefModel>();
        public List<CustomerSoRefDetailModel> sales_order_details_view { get; set; } = new List<CustomerSoRefDetailModel>();
    }
}
