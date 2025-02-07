using smpc_app.Services.Helpers;
using smpc_inventory_app.Services.Helpers;
using smpc_inventory_app.Services.Setup.Model.Item;
using smpc_sales_app.Models;
using smpc_sales_app.Services.Helpers;
using smpc_sales_system.Services.Sales.Models;
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
        static string url = "/setup/item";
        
        
        public static async Task<Items> GetItem()
        {
            var response = await RequestToApi<ApiResponseModel<Items>>.Get(url);
            var itemData = response.Data;
            return itemData;
        }


        //public async Task<itemlist> GetItemId(string id)
        //{
        //    var response = await RequestToApi<ApiResponseModel<itemlist>>.Get(url_search + id);
        //    itemlist itemdata = response.Data;
        //    return itemdata;
        //}


        //public static async Task<ItemLists> GetItemQuotationId()
        //{
        //    var response = await RequestToApi<ApiResponseModel<ItemLists>>.Get(url);
        //    ItemLists itemdata = response.Data;
        //    return itemdata;
        //}

  
    }
}
