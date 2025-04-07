
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
        static string url_customer = "/bpi/";
        static string url_search = "/bpi/";


        // GET: quotations list
        public static async Task<SalesQuotationList> GetQuotations()
        {
            var response = await RequestToApi<ApiResponseModel<SalesQuotationList>>.Get(url);
            SalesQuotationList quotationData = response.Data;
            return quotationData;
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
            Bpi_Class  customerData = response.Data;
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
    }
}
