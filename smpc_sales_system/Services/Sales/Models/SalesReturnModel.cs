using System.Collections.Generic;

namespace smpc_sales_system.Services.Sales.Models
{
    // Mirrors models.SalesReturnContent (ERP_API/models/sales_return_model.go).
    // Field names are snake_case to match the Go JSON tags directly -
    // Newtonsoft binds case-sensitively against C# property names by
    // default, so these must be spelled exactly as the API sends them.
    internal class SalesReturnModel
    {
        public uint id { get; set; }
        public int doc_no { get; set; }

        public uint customer_id { get; set; }
        public string customer_code { get; set; }
        public string customer_name { get; set; }
        public string tin_no { get; set; }
        public string address { get; set; }

        // "Sales Invoice" or "Delivery Receipt" - must be chosen before any
        // item selection (spec §5.13, §14 test #62).
        public string ref_doc_type { get; set; }
        public uint ref_doc_id { get; set; }
        public string ref_doc_no { get; set; }

        public string doc_date { get; set; }
        public string expected_returned_date { get; set; }
        public string transaction_type { get; set; }
        public string ship_to { get; set; }
        public string location_group { get; set; }
        public string location_code { get; set; }

        public string salesperson { get; set; }
        public string currency { get; set; }
        public string sales_period { get; set; }

        public double total { get; set; }

        public bool is_approved { get; set; }
        public uint approved_by_id { get; set; }
        public string approved_by_name { get; set; }
        public string approval_date { get; set; }

        public string cm_reason_code { get; set; }
        public uint ref_cm_id { get; set; }
        public string ref_cm_no { get; set; }

        public string header_remarks { get; set; }
        public string description { get; set; }
    }

    // Mirrors models.SalesReturnDetailsContent.
    internal class SalesReturnDetailsModel
    {
        public uint id { get; set; }
        public uint sales_return_id { get; set; }

        public uint item_id { get; set; }
        public string item_code { get; set; }
        public string description { get; set; }
        public string unit_of_measure { get; set; }

        public int qty_returned { get; set; }
        public int qty_received { get; set; }
        public int qty_discrepancy { get; set; }

        public int qty_for_replacement { get; set; }
        public int qty_to_stock { get; set; }
        public int qty_for_purchase_return { get; set; }

        public double unit_price { get; set; }
        public double total_cost { get; set; }

        public string reason_for_return { get; set; }
    }

    // POST /sales-returns request body - matches models.SalesReturnBody.
    internal class SalesReturnBody
    {
        public SalesReturnModel sales_return { get; set; }
        public List<SalesReturnDetailsModel> sales_return_details { get; set; }
    }

    // GET /sales-returns (list) and GET /sales-returns/:id response -
    // matches models.SalesReturnGet. Both endpoints wrap the same shape,
    // GetById just comes back with a single-element array (same convention
    // as every other document's GetById in this codebase).
    internal class SalesReturnGet
    {
        public List<SalesReturnModel> sales_return { get; set; } = new List<SalesReturnModel>();
        public List<SalesReturnDetailsModel> sales_return_details { get; set; } = new List<SalesReturnDetailsModel>();
    }
}
