using System.Threading.Tasks;
using smpc_app.Services.Helpers;
using smpc_sales_app.Services.Helpers;
using smpc_sales_system.Services.Sales.Models;

namespace smpc_sales_system.Services.Sales
{
    // Thin read-only client for the accounting module's sales invoice endpoint. Red Box uses
    // this to tell whether a Sales Order has already been invoiced (its "PV na" / done signal),
    // by checking whether any of the order's line items shows up as an invoiced line here.
    internal static class SalesInvoiceService
    {
        static string url = "/accounting/sales_invoice";

        public static async Task<SalesInvoiceList> GetSalesInvoices()
        {
            var response = await RequestToApi<ApiResponseModel<SalesInvoiceList>>.Get(url);
            return response.Data;
        }
    }
}
