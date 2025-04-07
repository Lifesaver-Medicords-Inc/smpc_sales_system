using smpc_app.Services.Helpers;
using smpc_inventory_app.Services.Helpers;
using smpc_sales_app.Services.Helpers;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace smpc_sales_system.Services.Setup
{
    internal static class ProjectServicesss
    {
        static string url = "/setup/project";


        // GET
        public static async Task<DataTable> GetAsDatatable()
        {
            var response = await RequestToApi<ApiResponseModel<List<Project>>>.Get(url);
            DataTable projectItems = JsonHelper.ToDataTable(response.Data);
            return projectItems;
        }

        public static async Task<ProjectList> GetProjects()
        {
            var response = await RequestToApi<ApiResponseModel<ProjectList>>.Get(url);
            ProjectList projectdata = response.Data;
            return projectdata;
        }

     

        // POST
        public static async Task<ApiResponseModel> Insert(Dictionary<string, dynamic> data)
        {
            var response = await RequestToApi<ApiResponseModel>.Post(url, data);
            return response;
        }


        // UPDATE
        public static async Task<ApiResponseModel> Update(Dictionary<string, dynamic> data)
        {
            var response = await RequestToApi<ApiResponseModel>.Put(url, data);
            return response;
        }

        public static async Task<ProjectList> GetItemImages()
        {
            var response = await RequestToApi<ApiResponseModel<ProjectList>>.Get(url);
            ProjectList projectdata = response.Data;
            return projectdata;
        }



        public static async Task<ProjectList> GetBOM()
        {
            var response = await RequestToApi<ApiResponseModel<ProjectList>>.Get(url);
            ProjectList projectdata = response.Data;
            return projectdata;
        }
    }
}
