using System.Collections.Generic;

namespace smpc_sales_system.Services.Sales.Models
{
    // Only the fields Red Box actually needs are declared - Newtonsoft ignores the rest of
    // the JSON payload (amounts, tax breakdown, etc. from accounting_sales_invoice_model.go)
    // without erroring, so this doesn't need to mirror the full Go model.
    class SalesInvoiceModel
    {
        public int id { get; set; }
        public string reference_doc_so { get; set; }
    }

    class SalesInvoiceDetailsModel
    {
        public int sales_invoice_id { get; set; }
        // This is the link back to tbl_trans_sales_order_details.order_details_id - if any
        // line of a Sales Order shows up here, that order has been invoiced.
        public int sales_order_details_id { get; set; }
    }

    class SalesInvoiceList
    {
        public List<SalesInvoiceModel> sales_invoice { get; set; }
        public List<SalesInvoiceDetailsModel> sales_invoice_details { get; set; }
    }
}
