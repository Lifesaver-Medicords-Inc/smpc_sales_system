using smpc_app.Services.Helpers;
using smpc_sales_app.Services.Helpers;
using smpc_sales_system.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace smpc_sales_system.Services.Setup
{
    internal class CompanyService
    {
        static string url = "/companies";

        public static async Task<DataTable> GetAsDatatable()
        {
            var response = await RequestToApi<ApiResponseModel<List<CompanyModel>>>.Get(url);
            DataTable applicationItems = JsonHelper.ToDataTable(response.Data);
            return applicationItems;
        }
    }
}
