using System.Collections.Generic;
using System.Threading.Tasks;
using smpc_app.Services.Helpers;
using smpc_sales_app.Services.Helpers;
using smpc_sales_system.Services.Sales.Models;

namespace smpc_sales_app.Services.Sales
{
    // Reuses Accounting's existing GET /api/accounting/sales_invoice
    // directly - no new Go endpoint. Same precedent as Purchase Return
    // reusing Invoice Receipt's endpoint from the Inventory app. Returns
    // every Sales Invoice; the picker modal filters client-side (this
    // endpoint takes no filter params server-side either, confirmed against
    // sales_invoice_handlers.GetSalesInvoice).
    internal class SalesInvoiceRefService
    {
        static string url = "/accounting/sales_invoice";

        public static async Task<SalesInvoiceRefGet> GetSalesInvoices()
        {
            var response = await RequestToApi<ApiResponseModel<SalesInvoiceRefGet>>.Get(url);
            var result = response?.Data ?? new SalesInvoiceRefGet();
            // Same nil-slice-marshals-to-null risk as SalesReturnService -
            // accounting_models.SalesInvoiceGet's fields have no omitempty.
            if (result.sales_invoice == null) result.sales_invoice = new List<SalesInvoiceRefModel>();
            if (result.sales_invoice_details == null) result.sales_invoice_details = new List<SalesInvoiceRefDetailsModel>();
            return result;
        }
    }
}
