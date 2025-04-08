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

namespace smpc_sales_app.Services.Purchasing
{
    internal static class PurchaseRequisitionService
    {
        static string url = "/purchasing/purchase_requisition";
        static string childurl = "/purchasing/child/purchase_requisition";

        // GET
        //public static async Task<DataTable> GetAsDatatable()
        //{
        //    var response = await RequestToApi<ApiResponseModel<List<OrderDetailsModel>>>.Get(url);
        //    DataTable orderDetailsItems = JsonHelper.ToDataTable(response.Data);
        //    return orderDetailsItems;
        //}

        public static async Task<PurchaseRequisitionList> GetPRs()
        {
            var response = await RequestToApi<ApiResponseModel<PurchaseRequisitionList>>.Get(url);
            PurchaseRequisitionList listData = response.Data;
            return listData;
        }

        public static async Task<PurchaseRequisitionModel[]> GetPR()
        {
            var response = await RequestToApi<ApiResponseModel<PurchaseRequisitionModel[]>>.Get(url);
            var listData = response.Data;

            return listData;
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
        // DELETE
        public static async Task<ApiResponseModel> DeleteChild(Dictionary<string, dynamic> data)
        {
            var response = await RequestToApi<ApiResponseModel>.Delete(childurl, data);
            return response;
            //bool 
        }
        // UPDATE   
        public static async Task<ApiResponseModel> Update(Dictionary<string,dynamic> data)
        {
            var response = await RequestToApi<ApiResponseModel>.Put(url, data);
            return response;
        }

    }
}
