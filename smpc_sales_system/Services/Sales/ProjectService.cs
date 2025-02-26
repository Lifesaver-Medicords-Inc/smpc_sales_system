using smpc_app.Services.Helpers;
using smpc_inventory_app.Services.Helpers;
using smpc_sales_system.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace smpc_sales_system.Services.Sales
{
    internal static class ProjectService
    {
        static string url = "/sales/projects";
        static string url_conditions = "/sales/project_conditions";
        static string url_content = "/sales/project_content";

        public static async Task<SalesProjectList> GetProjects()
        {
            var response = await RequestToApi<ApiResponseModel<SalesProjectList >>.Get(url);
            SalesProjectList projectData = response.Data;
            return projectData;
        }

        public static async Task<ApiResponseModel> Insert(Dictionary<string, dynamic> data)
        {
            var response = await RequestToApi<ApiResponseModel>.Post(url, data);
            return response;
        }

        public static async Task<ApiResponseModel> UpdateConditions(Dictionary<string, dynamic> data)
        {
            var response = await RequestToApi<ApiResponseModel>.Put(url_conditions, data);
            return response;
        }

        public static async Task<ApiResponseModel> UpdateContents(Dictionary<string, dynamic> data)
        {
            var response = await RequestToApi<ApiResponseModel>.Put(url_content, data);
            return response;
        }
    }
}
