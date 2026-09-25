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
    // One row of the quotation's item and model pickers - mirrors ERP_API's ItemPickerRow.
    // It carries everything SalesItemGridEditor.GetItemData and ProjectTemplate.GetItemData
    // write onto a quotation line, so a picked item needs no second round trip: the modal
    // merges this row into the page's ItemList and those methods find it there as before.
    public class ItemPickerRow
    {
        public int id { get; set; }
        public string item_code { get; set; }
        public string item_name { get; set; }
        public string item_model { get; set; }
        public int item_name_id { get; set; }
        public string unit_of_measure { get; set; }
        // 0 when the item has no BOM - drives BOM vs SINGLE and which branch the
        // quotation takes after a pick.
        public int bom_id { get; set; }

        // Adds this row to the page's ItemList if it isn't already there.
        //
        // This is what lets the pickers page without touching their callers: the page no
        // longer needs to have downloaded an item for GetItemData to find it - the modal
        // deposits the picked one on the way out. Columns the table doesn't have are
        // skipped, so it works against ItemList, a project template's table, or anything
        // else keyed on "id".
        public void MergeInto(DataTable table)
        {
            if (table == null)
                return;

            // A table with no columns at all is an item list that never loaded (the page's
            // item fetch failed). Skipping it left the caller unable to find the item it was
            // just handed - "Invalid selection. Item not found." Give it the columns this
            // row carries so the pick still goes through.
            if (table.Columns.Count == 0)
            {
                table.Columns.Add("id", typeof(int));
                table.Columns.Add("item_code", typeof(string));
                table.Columns.Add("item_name", typeof(string));
                table.Columns.Add("item_model", typeof(string));
                table.Columns.Add("item_name_id", typeof(int));
                table.Columns.Add("unit_of_measure", typeof(string));
            }

            if (!table.Columns.Contains("id"))
                return;

            foreach (DataRow existing in table.Rows)
            {
                if (existing["id"] != DBNull.Value && Convert.ToInt32(existing["id"]) == id)
                    return;
            }

            DataRow row = table.NewRow();
            SetIfPresent(table, row, "id", id);
            SetIfPresent(table, row, "item_code", item_code);
            SetIfPresent(table, row, "item_name", item_name);
            SetIfPresent(table, row, "item_model", item_model);
            SetIfPresent(table, row, "item_name_id", item_name_id);
            SetIfPresent(table, row, "unit_of_measure", unit_of_measure);
            table.Rows.Add(row);
        }

        private static void SetIfPresent(DataTable table, DataRow row, string column, object value)
        {
            if (table.Columns.Contains(column))
                row[column] = value ?? (object)DBNull.Value;
        }
    }

    public class ItemPickerPage
    {
        public List<ItemPickerRow> Rows { get; set; } = new List<ItemPickerRow>();
        public PaginationModel Pagination { get; set; }

        // Carried so a picker can tell "this list really is empty" apart from "the server
        // refused the request" - a row pointing at an item that is no longer in the
        // catalogue answers 404 with a message saying so, and the modal shows it rather
        // than the same "No models found" an unmatched search gives.
        public bool Success { get; set; }
        public string Message { get; set; }
    }

    // One catalogue row with only its own descriptions and images - the lazy
    // answer to what the full GetItem() download used to provide. Mirrors
    // ERP_API's ItemDetailResponse keys (item/specs/images).
    public class ItemDetailResult
    {
        public ItemModel item { get; set; }
        public List<AdditionalSpecsModel> specs { get; set; }
        public List<smpc_sales_system.Services.Sales.Models.ItemImageModel> images { get; set; }
    }

     class ItemService
     {
        static string url = "/setup/item";


        public static async Task<Items> GetItem()
        {
            var response = await RequestToApi<ApiResponseModel<Items>>.Get(url);
            var itemData = response.Data;
            return itemData;
        }

        // The ITEM picker: one row per item name, 20 to a page. The dedup is a
        // catalogue-wide GROUP BY, which is why it is done on the server and not by
        // filtering a downloaded table.
        public static async Task<ItemPickerPage> GetPickerNames(string search, int page = 1)
        {
            string query = $"?page={page}";
            if (!string.IsNullOrWhiteSpace(search))
                query += $"&search={Uri.EscapeDataString(search)}";

            var response = await RequestToApi<ApiResponseModel<List<ItemPickerRow>>>.Get(url + "/picker" + query);

            return new ItemPickerPage
            {
                Rows = response?.Data ?? new List<ItemPickerRow>(),
                Pagination = response?.pagination
            };
        }

        // The MODEL picker: every model sharing the given item's item name, 20 to a page.
        public static async Task<ItemPickerPage> GetPickerModels(int itemId, string search, int page = 1)
        {
            string query = $"?item_id={itemId}&page={page}";
            if (!string.IsNullOrWhiteSpace(search))
                query += $"&search={Uri.EscapeDataString(search)}";

            var response = await RequestToApi<ApiResponseModel<List<ItemPickerRow>>>.Get(url + "/picker/models" + query);

            return new ItemPickerPage
            {
                Rows = response?.Data ?? new List<ItemPickerRow>(),
                Pagination = response?.pagination,
                Success = response?.Success ?? false,
                Message = response?.message
            };
        }

        // One catalogue row with only its own descriptions and images (null when
        // the id is unknown). The lazy replacement for looking an item up in the
        // old full-catalogue tables.
        public static async Task<ItemDetailResult> GetItemDetail(int itemId)
        {
            var response = await RequestToApi<ApiResponseModel<ItemDetailResult>>.Get(url + "/detail/" + itemId);
            return response.Data;
        }

        // Light picker rows for item_name = PUMP (Size Up / Final pickers),
        // fetched on modal open. Optional search narrows by code/name/model.
        //
        // ids asks for exactly those items and nothing else. SIZE UP passes none - it
        // offers every pump - while FINAL passes the ids already on its SIZE UP grid,
        // because spec 5.1.4 limits FINAL to what SIZE UP lists. It used to fetch all of
        // them and filter client-side: 3,684 rows over the wire to show three.
        public static async Task<List<ItemPickerRow>> GetPumpPickerRows(string search = null, IEnumerable<int> ids = null)
        {
            var query = new List<string>();

            if (!string.IsNullOrWhiteSpace(search))
                query.Add($"search={Uri.EscapeDataString(search)}");

            string idList = JoinIds(ids);
            if (idList.Length > 0)
                query.Add($"ids={idList}");

            string queryString = query.Count > 0 ? "?" + string.Join("&", query) : "";

            var response = await RequestToApi<ApiResponseModel<List<ItemPickerRow>>>.Get(url + "/picker/pumps" + queryString);
            return response?.Data ?? new List<ItemPickerRow>();
        }

        // A comma-separated id list for an ?ids= filter, or "" for no filter. Shared so
        // the two calls FINAL makes build it the same way.
        internal static string JoinIds(IEnumerable<int> ids)
        {
            if (ids == null)
                return string.Empty;

            return string.Join(",", ids.Where(id => id > 0).Distinct());
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
