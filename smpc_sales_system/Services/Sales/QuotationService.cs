
using smpc_app.Services.Helpers;
using smpc_inventory_app.Services.Helpers;
using smpc_inventory_app.Services.Setup.Model.Item;
using smpc_sales_app.Services.Helpers;
using smpc_sales_app.Services.Sales.Models;
using smpc_sales_system.Models;
using smpc_sales_system.Services.Sales.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace smpc_sales_app.Services.Sales
{
    class QuotationService
    {
        static string url = "/sales/quotation";
        static string latest_url = "/sales/quotation/latest";
        static string url_customer = "/bpi/";
        static string url_customer_only = "/sales/quotation/customers";
        static string url_search = "/bpi/";


        // GET: quotation list
        public static async Task<SalesQuotationList> GetQuotations()
        {
            var response = await RequestToApi<ApiResponseModel<SalesQuotationList>>.Get(url);
            SalesQuotationList quotationData = response.Data;
            return quotationData;
        }
        // GET: latest quotation list (one row per document - the module's
        // default view; the full history lives behind GetQuotationVersions)
        public static async Task<SalesQuotationList> GetLatestQuotations()
        {
            var response = await RequestToApi<ApiResponseModel<SalesQuotationList>>.Get(latest_url);
            SalesQuotationList quotationData = response.Data;
            return quotationData;
        }
        // GET: one page of latest headers, newest first, no children.
        // after/before/at are ids (keyset); all zero means the first page.
        // The envelope carries the page meta (has_next/has_prev/page/total).
        public static async Task<ApiResponseModel<SalesQuotationList>> GetQuotationHeadersPage(int after = 0, int before = 0, int at = 0)
        {
            var response = await RequestToApi<ApiResponseModel<SalesQuotationList>>.Get(
                url + "/headers?after=" + after + "&before=" + before + "&at=" + at);
            return response;
        }
        // GET: one header with only its own lines and images (lazy detail).
        // Null when the id is unknown.
        public static async Task<SalesQuotationList> GetQuotationDetail(int id)
        {
            var response = await RequestToApi<ApiResponseModel<SalesQuotationList>>.Get(url + "/detail/" + id);
            return response.Data;
        }
        // GET: flat matching headers, 20 to a page, no children.
        public static async Task<ApiResponseModel<SalesQuotationList>> SearchQuotations(string term, int page = 1)
        {
            var response = await RequestToApi<ApiResponseModel<SalesQuotationList>>.Get(
                url + "/search?term=" + Uri.EscapeDataString(term ?? "") + "&page=" + page);
            return response;
        }
        // GET: one document's full version history (headers + only that
        // document's lines/images) for the version viewer and
        // open-by-document flows - replaces downloading the whole list
        // to look at a single document.
        public static async Task<SalesQuotationList> GetQuotationVersions(string documentNo)
        {
            var response = await RequestToApi<ApiResponseModel<SalesQuotationList>>.Get(
                url + "/versions?document_no=" + Uri.EscapeDataString(documentNo ?? ""));
            return response.Data;
        }
        //public static async Task

        public static async Task<Items> GetItems()
        {
            var response = await RequestToApi<ApiResponseModel<Items>>.Get(url);
            Items quotationData = response.Data;
            return quotationData;
        }

        // GET: bpi by id
        public static async Task<bpi_list> GetBpiId(string id)
        {
            var response = await RequestToApi<ApiResponseModel<bpi_list>>.Get(url_search + id);
            bpi_list bpiData = response.Data;
            return bpiData;
        }

        // GET: bpi customer view
        public static async Task<Bpi_Class> GetBpiCustomers()
        {
            var response = await RequestToApi<ApiResponseModel<Bpi_Class>>.Get(url_customer);
            Bpi_Class customerData = response.Data;
            return customerData;
        }

        // GET: BPI Customer only
        public static async Task<BpiCustomer> GetBpiCustomersDetails()
        {
            var response = await RequestToApi<ApiResponseModel<BpiCustomer>>.Get(url_customer_only);
            BpiCustomer customerData = response.Data;
            return customerData;
        }

        // GET: quotation parent only
        public static async Task<SalesQuotationModel[]> GetQuotation()
        {
            var response = await RequestToApi<ApiResponseModel<SalesQuotationModel[]>>.Get(url);
            var quotationData = response.Data;

            return quotationData;
        }

        // POST: quotation
        public static async Task<ApiResponseModel> Insert(Dictionary<string, dynamic> data)
        {
            var response = await RequestToApi<ApiResponseModel>.Post(url, data);
            return response;
        }

        // PUT: quotation
        public static async Task<ApiResponseModel> Update(Dictionary<string, dynamic> data)
        {
            var response = await RequestToApi<ApiResponseModel>.Put(url, data);
            return response;
        }

        // POST: §3.2/§6.3 REQUEST FOR ENGR. (Phase 4 item 4.1) - the explicit per-quote
        // grant. Checking REQUEST FOR ENGR. is the whole action (changed 2026-09-03,
        // user decision - no engineer picker anymore, no engr_id to send). Every
        // engineer sees every checked quotation on a shared list; see
        // GetEngineeringQuotationListByEngr on the API side.
        public static async Task<ApiResponseModel> RequestForEngr(int quotationId)
        {
            var response = await RequestToApi<ApiResponseModel>.Post(
                $"{url}/{quotationId}/request_for_engr",
                new { });
            return response;
        }

        // POST: reverses RequestForEngr above - added on user request so the button
        // toggles (REQUEST FOR ENGR. / CANCEL REQUEST) instead of being one-way.
        public static async Task<ApiResponseModel> CancelRequestForEngr(int quotationId)
        {
            var response = await RequestToApi<ApiResponseModel>.Post(
                $"{url}/{quotationId}/cancel_request_for_engr",
                new { });
            return response;
        }

    }
}
