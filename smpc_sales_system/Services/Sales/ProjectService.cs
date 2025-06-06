using smpc_app.Services.Helpers;
using smpc_inventory_app.Services.Helpers;
using smpc_sales_app.Services.Helpers;
using smpc_sales_system.Models;
using smpc_sales_system.Services.Sales.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static smpc_sales_system.Services.Sales.Models.ItemBomListModel;

namespace smpc_sales_system.Services.Sales
{
    internal static class ProjectService
    {
        static string url = "/sales/projects";
        static string url_conditions = "/sales/project_conditions";
        static string url_content = "/sales/project_contents";
        static string url_items = "/sales/project_items";
        static string url_bom = "/setup/bom";
        static string url_suppliers = "/BpiSuppliers";
        static string url_canvas = "/sales/salescanvas";
        static string url_pumps = "/sales/projects_pumps";
        static string url_wiring = "/sales/project_wiring";
        public static async Task<SalesProjectList> GetProjects()
        {
            var response = await RequestToApi<ApiResponseModel<SalesProjectList>>.Get(url);
            SalesProjectList projectData = response.Data;
            return projectData;
        }
        
        public static async Task<BpiSupplierList> GetSuppliers()
        {
            var response = await RequestToApi<ApiResponseModel<BpiSupplierList>>.Get(url_suppliers);
            BpiSupplierList suppliers = response.Data;
            return suppliers;
        }

        public static async Task<CanvasViewList> GetCanvasView()
        {
            var response = await RequestToApi<ApiResponseModel<CanvasViewList>>.Get(url_canvas);
            CanvasViewList suppliers = response.Data;
            return suppliers;
        }


        public static async Task<ApiResponseModel> InsertCanvas(Dictionary<string, dynamic> data)
        {
            var response = await RequestToApi<ApiResponseModel>.Post(url_canvas, data);
            return response;
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

        public static async Task<ApiResponseModel> InsertItems(Dictionary<string, dynamic> data)
        {
            var response = await RequestToApi<ApiResponseModel>.Post(url_items, data);
            return response;
        }

        public static async Task<ApiResponseModel> InsertWiring(Dictionary<string, dynamic> data)
        {
            var response = await RequestToApi<ApiResponseModel>.Post(url_wiring, data);
            return response;
        }

        public static async Task<ApiResponseModel> UpdateWiring(Dictionary<string, dynamic> data)
        {
            var response = await RequestToApi<ApiResponseModel>.Put(url_wiring, data);
            return response;
        }

        public static async Task<ApiResponseModel> UpdateProjectItems(Dictionary<string, dynamic> data)
        {
            var response = await RequestToApi<ApiResponseModel>.Put(url_items, data);
            return response;
        }

    
        public static async Task<DataTable> GetAsDatatableBom()
        {
            var response = await RequestToApi<ApiResponseModel<List<ItemBomListModel.ItemBomList>>>.Get(url_bom);
            DataTable BomList = JsonHelper.ToDataTable(response.Data);
            return BomList;
        }


        public static async Task<BomList> GetBom()
        {
            var response = await RequestToApi<ApiResponseModel<BomList>>.Get(url_bom);
            BomList bomData = response.Data;
            return bomData;
        }

        public static async Task<ItemPumpsViewList> GetPumpsViewList()
        {
            var response = await RequestToApi<ApiResponseModel<ItemPumpsViewList>>.Get(url_pumps);
            ItemPumpsViewList pumpsData = response.Data;
            return pumpsData;
        }

    }
}
