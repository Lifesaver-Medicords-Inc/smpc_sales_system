using smpc_app.Services.Helpers;
using smpc_sales_app.Services.Helpers;
using smpc_sales_system.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace smpc_sales_system.Services.Setup
{
    internal class EngineerService
    {
        static string url = "/engineering/job_order/engr_list";

        // Filtered server-side to tbl_setup_users where department = 'Engineering'
        // (see ERP_API vw_get_users_engr_list).
        public static async Task<List<EngineerModel>> GetEngineerList()
        {
            var response = await RequestToApi<ApiResponseModel<List<EngineerModel>>>.Get(url);
            return response?.Data;
        }
    }
}
