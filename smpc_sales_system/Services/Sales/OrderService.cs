using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using smpc_app.Services.Helpers;
using smpc_inventory_app.Services.Helpers;
using smpc_sales_app.Services;
using smpc_sales_app.Services.Helpers;
using smpc_sales_app.Services.Sales.Models;
using smpc_sales_system.Services.Sales.Models;

namespace smpc_sales_app.Services.Sales
{
    internal static class OrderService
    {
        static string url = "/sales/order";
        static string childurl = "/sales/child/order";

        // GET
        //public static async Task<DataTable> GetAsDatatable()
        //{
        //    var response = await RequestToApi<ApiResponseModel<List<OrderDetailsModel>>>.Get(url);
        //    DataTable orderDetailsItems = JsonHelper.ToDataTable(response.Data);
        //    return orderDetailsItems;
        //}

        public static async Task<OrderList> GetOrders()
        {
            var response = await RequestToApi<ApiResponseModel<OrderList>>.Get(url);
            OrderList orderData = response.Data;
            return orderData;
        }

        public static async Task<OrderModel[]> GetOrder()
        {
            var response = await RequestToApi<ApiResponseModel<OrderModel[]>>.Get(url);
            var orderData = response.Data;

            return orderData;
        }

        // POST
        public static async Task<ApiResponseModel> Insert(Dictionary<string, dynamic> data)
        {
            var response = await RequestToApi<ApiResponseModel>.Post(url, data);
            return response;
        }

        public static async Task<ApiResponseModel> InsertChild(Dictionary<string, dynamic> data)
        {
            var response = await RequestToApi<ApiResponseModel>.Post(childurl, data);
            return response;
        }

        // DELETE
        public static async Task<Boolean> Delete(Dictionary<string, dynamic> data)
        {
            var response = await RequestToApi<ApiResponseModel<bool>>.Delete(url, data);
            bool isSucccess = response.Success;
            return isSucccess;
        }

        // UPDATE   
        public static async Task<ApiResponseModel> Update(Dictionary<string,dynamic> data)
        {
            var response = await RequestToApi<ApiResponseModel>.Put(url, data);
            return response;
        }

    }
}
