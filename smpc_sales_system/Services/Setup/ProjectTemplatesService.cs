using smpc_app.Services.Helpers;
using smpc_inventory_app.Services.Helpers;
using smpc_sales_system.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace smpc_sales_system.Services.Setup
{
    class ProjectTemplatesService
    {

        static string url = "/setup/templates";
        public static async Task<ProjectTemplateList> GetProjectTemplates()
        {
            var response = await RequestToApi<ApiResponseModel<ProjectTemplateList>>.Get(url);
            ProjectTemplateList projectdata = response.Data;
            return projectdata;
        }

        public static async Task<ApiResponseModel> Insert(Dictionary<string, dynamic> data)
        {
            var response = await RequestToApi<ApiResponseModel>.Post(url, data);
            return response;
        }
    }
}
