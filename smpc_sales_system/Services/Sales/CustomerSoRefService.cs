using System.Collections.Generic;
using System.Threading.Tasks;
using smpc_app.Services.Helpers;
using smpc_sales_app.Services.Helpers;
using smpc_sales_system.Services.Sales.Models;

namespace smpc_sales_app.Services.Sales
{
    // Reuses Accounting's existing GET /api/accounting/customer_so/:id
    // directly - see CustomerSoRefModel.cs for why Sales Return needs this
    // endpoint at all. Returns 404 with no data when the customer has no
    // SOs at all - GetCustomerSo() below treats that as "no pricing found"
    // rather than an error, since a customer with a Delivery Receipt
    // necessarily has at least one SO, but the call is defensive regardless.
    internal class CustomerSoRefService
    {
        static string url = "/accounting/customer_so/";

        public static async Task<CustomerSoRefGet> GetCustomerSo(uint customerId)
        {
            var response = await RequestToApi<ApiResponseModel<CustomerSoRefGet>>.Get(url + customerId, silent: true);
            var result = response?.Data ?? new CustomerSoRefGet();
            // Same nil-slice-marshals-to-null risk noted in SalesReturnService
            // - the Go handler's anonymous response struct has no omitempty
            // either.
            if (result.sales_order_view == null) result.sales_order_view = new List<CustomerSoRefModel>();
            if (result.sales_order_details_view == null) result.sales_order_details_view = new List<CustomerSoRefDetailModel>();
            return result;
        }
    }
}
