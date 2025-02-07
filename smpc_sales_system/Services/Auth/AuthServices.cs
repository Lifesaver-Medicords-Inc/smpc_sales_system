using smpc_app.Services.Helpers;
using smpc_inventory_app.Services.Helpers;
using smpc_sales_app.Services.Helpers;
using smpc_sales_app.Services.Sales;
using smpc_sales_system.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace smpc_sales_system.Services
{ 
        internal class AuthServices
        { 
            static string url = "/login";
             
            // Login
            public static async Task<ApiResponseModel<CurrentUserModel>> Login(Dictionary<string, dynamic> data)
            {
                var response = await RequestToApi<ApiResponseModel<CurrentUserModel>>.Post(url, data);
                return response;
            }

            // Logout
            public static async Task<ApiResponseModel> Logout(Dictionary<string, dynamic> data)
            {
                var response = await RequestToApi<ApiResponseModel>.Post(url, data);
                return response;
            }
        } 
}
