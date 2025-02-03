
using smpc_app.Services.Helpers;
using smpc_inventory_app.Services.Helpers;
using smpc_sales_app.Services.Helpers;
using smpc_sales_app.Services.Sales.Models;
using smpc_sales_system.Models;
using smpc_sales_system.Services.Sales.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace smpc_sales_app.Services.Sales
{
     class QuotationService
    {
        static string url = "/sales/quotation";
        //static string url_child = "/sales/child/quotation";
        static string url_customer = "/bpi/customers";
        static string url_search = "/bpi/";


        // GET
        public static async Task<SalesQuotationList> GetQuotations()
        {
            var response = await RequestToApi<ApiResponseModel<SalesQuotationList>>.Get(url);
            SalesQuotationList quotationData = response.Data;
            return quotationData;
        }
       
        public static async Task<bpi_list> GetBpiId(string id)
        {
            var response = await RequestToApi<ApiResponseModel<bpi_list>>.Get(url_search + id);
            bpi_list bpiData = response.Data;
            return bpiData;
        }


        public static async Task<GetBpiList> GetBpiCustomers()
        {
            var response = await RequestToApi<ApiResponseModel<GetBpiList>>.Get(url_customer);
            GetBpiList customerData = response.Data;
            return customerData;
        }

        //public static async Task<DataTable> GetBpiCustomerAsDatatable()
        //{
        //    var response = await RequestToApi<ApiResponseModel<List<BpiCustomer>>>.Get(url_customer);
        //    DataTable customerItems = JsonHelper.ToDataTable(response.Data);
        //    return customerItems;
        //}


        public static async Task<SalesQuotationModel[]> GetQuotation()
        {
            var response = await RequestToApi<ApiResponseModel<SalesQuotationModel[]>>.Get(url);
            var quotationData = response.Data;

            return quotationData;
        }

    

        // POST
        public static async Task<ApiResponseModel> Insert(Dictionary<string, dynamic> data)
        {
            var response = await RequestToApi<ApiResponseModel>.Post(url, data);
            return response;
        }


        //public static async Task<ApiResponseModel> InsertChild(Dictionary<string, dynamic> data)
        //{
        //    var response = await RequestToApi<ApiResponseModel>.Post(url_child, data);
        //    return response;
        //}



        // UPDATE
        public static async Task<ApiResponseModel> Update(Dictionary<string, dynamic> data)
        {
            var response = await RequestToApi<ApiResponseModel>.Put(url, data);
            return response;
        }
    }
}
