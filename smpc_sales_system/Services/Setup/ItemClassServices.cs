//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using System.Data;
//using smpc_inventory_app.Services.Helpers;
//using smpc_inventory_app.Services.Setup.Model;


//namespace smpc_inventory_app.Services.Setup.Item
//{
//    internal static class ItemClassServices
//    {
//        static string url = "/setup/item";


//        public static async Task<DataTable> GetAsDatatable()
//        {
//            var response = await RequestToApi<ApiResponseModel<List<ItemClassModel>>>.Get(url);
         
//            DataTable itemClass = JsonHelper.ToDataTable(response.Data);

//            return itemClass;
//        }

//        public static async Task<ItemClassModel[]> Get()
//        {
//            var response = await RequestToApi<ApiResponseModel<ItemClassModel[]>>.Get(url);
//            var itemClass = response.Data;

//            return itemClass;
//        }

//        public static async Task<Boolean> Insert(Dictionary<string,dynamic> data)
//        {
//            var response = await RequestToApi<ApiResponseModel<ItemClassModel[]>>.Post(url,data);
//            var itemClass = response.Success;

//            return itemClass;
//        }
//    }
//}
