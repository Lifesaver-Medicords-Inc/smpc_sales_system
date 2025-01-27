using smpc_app.Services.Helpers;
using smpc_inventory_app.Services.Helpers;
using smpc_sales_app.Models;
using smpc_sales_app.Services.Helpers;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace smpc_sales_app.Services.Sales
{
     class ItemService
     {
        public string id { get; set; }
        static string url = "/setup/item/";
        
        public static async Task<DataTable> GetAsDataTable()
        {
            var response = await RequestToApi<ApiResponseModel<List<ItemModel>>>.Get(url);
            DataTable itemList = JsonHelper.ToDataTable(response.Data);
            return itemList;
        }

        public static async Task<ItemModel[]> GetItem()
        {
            var response = await RequestToApi<ApiResponseModel< ItemModel[] >>.Get(url);
            var itemData = response.Data;

            return itemData;
        }
    

        public async Task<DataTable> GetItemById(string setId)
        {
            var response = await RequestToApi<ApiResponseModel<List<ItemModel>>>.Get(url + setId);
            DataTable idItem = JsonHelper.ToDataTable(response.Data);

            return idItem;
        }
    }
}
