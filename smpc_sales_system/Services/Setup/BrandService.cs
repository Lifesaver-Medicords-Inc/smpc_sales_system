using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using smpc_app.Services.Helpers;
using smpc_inventory_app.Services.Helpers;
using smpc_sales_app.Services.Helpers;

namespace smpc_sales_app.Services.Sales
{
    // The brand setup list - the same one Item Entry uses. A project quotation's Advanced
    // Conditions BRAND dropdown is filled from this and stores the brand's id, so a brand
    // renamed in setup reads correctly on every quotation already saved.
    internal static class BrandService
    {
        static string url = "/setup/item/brand";

        public class BrandModel
        {
            public int id { get; set; }
            public string code { get; set; }
            public string name { get; set; }
        }

        // Cached for the life of the app: it is a short setup list, every item set on a
        // project quotation needs it, and a tab is created per set.
        private static DataTable _cached;

        public static async Task<DataTable> GetAsDatatable()
        {
            if (_cached != null && _cached.Rows.Count > 0)
                return _cached;

            var response = await RequestToApi<ApiResponseModel<List<BrandModel>>>.Get(url);
            _cached = JsonHelper.ToDataTable(response?.Data ?? new List<BrandModel>());
            return _cached;
        }

        // A brand's name for Change History, from the list already loaded; null if it is not
        // in hand yet. Never fetches - history is written during a save.
        public static string NameOf(int id)
        {
            DataTable cached = _cached;
            if (cached == null || !cached.Columns.Contains("id") || !cached.Columns.Contains("name")) return null;

            foreach (DataRow row in cached.Rows)
            {
                if (row["id"]?.ToString() == id.ToString()) return row["name"]?.ToString();
            }
            return null;
        }
    }
}
