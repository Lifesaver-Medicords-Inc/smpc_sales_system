using smpc_inventory_app.Services.Setup.Model.Item;
using smpc_sales_app.Services.Sales;
using smpc_sales_system.Services.Sales.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace smpc_app.Services.Helpers
{
    // Fixed minimal schemas for the sales-side item merge tables (ItemList,
    // ItemAdditionalSpecs, ImageList) plus the projections that fill them.
    //
    // Phase 3: the module no longer downloads the whole 3,707-row catalogue.
    // These tables start with columns but no rows; picked rows (ItemPickerRow.
    // MergeInto) and per-item detail fetches add to them, and every existing
    // consumer (GetItemData's id lookup, description/specs matching, image
    // RowFilters, pump name filter) works off the merged subset unchanged.
    // Fixed schemas (instead of adopting the first payload's shape) keep every
    // writer compatible: picker merges, detail merges and BOM resolutions all
    // target the same columns with the same types.
    public static class ItemCatalogTables
    {
        public static DataTable NewItemTable()
        {
            DataTable table = new DataTable();
            table.Columns.Add("id", typeof(int));
            table.Columns.Add("item_code", typeof(string));
            table.Columns.Add("item_name", typeof(string));
            table.Columns.Add("item_model", typeof(string));
            table.Columns.Add("item_name_id", typeof(int));
            table.Columns.Add("unit_of_measure", typeof(string));
            // Print modal fallback descriptions (short_desc) live here too.
            table.Columns.Add("short_desc", typeof(string));
            return table;
        }

        public static DataTable NewItemSpecsTable()
        {
            DataTable table = new DataTable();
            table.Columns.Add("id", typeof(int));
            table.Columns.Add("based_id", typeof(int));
            table.Columns.Add("long_description", typeof(string));
            return table;
        }

        public static DataTable NewItemImageTable()
        {
            DataTable table = new DataTable();
            table.Columns.Add("id", typeof(int));
            table.Columns.Add("based_id", typeof(int));
            table.Columns.Add("image", typeof(string));
            table.Columns.Add("filename", typeof(string));
            return table;
        }

        public static bool ContainsId(DataTable table, int id)
        {
            if (table == null || !table.Columns.Contains("id") || id <= 0)
                return false;
            foreach (DataRow row in table.Rows)
            {
                if (row["id"] != DBNull.Value && Convert.ToInt32(row["id"]) == id)
                    return true;
            }
            return false;
        }

        // based_id match, the way getItemShortDescription and the image picker
        // resolve rows.
        public static bool ContainsBasedId(DataTable table, int basedId)
        {
            if (table == null || !table.Columns.Contains("based_id") || basedId <= 0)
                return false;
            foreach (DataRow row in table.Rows)
            {
                if (row["based_id"] != DBNull.Value && Convert.ToInt32(row["based_id"]) == basedId)
                    return true;
            }
            return false;
        }

        public static void AddItemRow(DataTable table, ItemModel item)
        {
            if (table == null || item == null || ContainsId(table, item.id))
                return;
            DataRow row = table.NewRow();
            SetInt(table, row, "id", item.id);
            SetText(table, row, "item_code", item.item_code);
            SetText(table, row, "item_name", item.item_name);
            SetText(table, row, "item_model", item.item_model);
            SetInt(table, row, "item_name_id", item.item_name_id);
            SetText(table, row, "unit_of_measure", item.unit_of_measure);
            SetText(table, row, "short_desc", item.short_desc);
            table.Rows.Add(row);
        }

        public static void AddSpecRows(DataTable table, IEnumerable<AdditionalSpecsModel> specs)
        {
            if (table == null || specs == null)
                return;
            foreach (var spec in specs)
            {
                if (spec == null || ContainsId(table, spec.id))
                    continue;
                DataRow row = table.NewRow();
                SetInt(table, row, "id", spec.id);
                SetInt(table, row, "based_id", spec.based_id);
                SetText(table, row, "long_description", spec.long_description);
                table.Rows.Add(row);
            }
        }

        public static void AddImageRows(DataTable table, IEnumerable<smpc_sales_system.Services.Sales.Models.ItemImageModel> images)
        {
            if (table == null || images == null)
                return;
            foreach (var image in images)
            {
                if (image == null || ContainsId(table, image.id))
                    continue;
                DataRow row = table.NewRow();
                SetInt(table, row, "id", image.id);
                SetInt(table, row, "based_id", image.based_id);
                SetText(table, row, "image", image.image);
                SetText(table, row, "filename", image.filename);
                table.Rows.Add(row);
            }
        }

        private static void SetInt(DataTable table, DataRow row, string column, int value)
        {
            if (table.Columns.Contains(column))
                row[column] = value;
        }

        private static void SetText(DataTable table, DataRow row, string column, string value)
        {
            if (table.Columns.Contains(column))
                row[column] = (object)value ?? DBNull.Value;
        }

        // Fetches one item's detail and merges it into the three tables.
        // True when the item's rows are present afterwards (already loaded or
        // just merged); false on unknown id or failed request. Callers treat
        // false as "not found" exactly like the old full-table miss.
        public static async Task<bool> FetchAndMergeAsync(DataTable items, DataTable specs, DataTable images, HashSet<int> loaded, int itemId)
        {
            if (itemId <= 0)
                return false;
            if (items != null && ContainsId(items, itemId) && loaded != null && loaded.Contains(itemId))
                return true;
            try
            {
                ItemDetailResult detail = await ItemService.GetItemDetail(itemId);
                if (detail == null)
                    return false;
                if (detail.item != null)
                    AddItemRow(items, detail.item);
                AddSpecRows(specs, detail.specs);
                AddImageRows(images, detail.images);
                if (loaded != null)
                    loaded.Add(itemId);
                return true;
            }
            catch
            {
                return false;
            }
        }

        // Brings every item referenced by a grid's rows into the merge tables,
        // then backfills blank unit_of_measure cells from them. One batch of
        // tiny parallel fetches instead of the old full-catalogue download -
        // used after picks, BOM explosions, template applies and opening
        // records whose lines reference items never loaded this session.
        public static async Task EnsureGridItemsAsync(DataTable items, DataTable specs, DataTable images, HashSet<int> loaded, DataGridView grid, string itemCol = "item_id", string uomCol = "unit_of_measure")
        {
            if (grid == null || items == null)
                return;

            var missing = new HashSet<int>();
            foreach (DataGridViewRow gridRow in grid.Rows)
            {
                if (gridRow.IsNewRow)
                    continue;
                if (!grid.Columns.Contains(itemCol))
                    return;
                if (int.TryParse(gridRow.Cells[itemCol].Value?.ToString(), out int id) && id > 0 && !ContainsId(items, id))
                    missing.Add(id);
            }

            if (missing.Count > 0)
            {
                var fetches = missing.Select(id => FetchAndMergeAsync(items, specs, images, loaded, id));
                await Task.WhenAll(fetches);
            }

            if (!grid.Columns.Contains(uomCol) || !items.Columns.Contains("unit_of_measure"))
                return;
            foreach (DataGridViewRow gridRow in grid.Rows)
            {
                if (gridRow.IsNewRow)
                    continue;
                if (!string.IsNullOrWhiteSpace(gridRow.Cells[uomCol].Value?.ToString()))
                    continue;
                if (!int.TryParse(gridRow.Cells[itemCol].Value?.ToString(), out int id) || id <= 0)
                    continue;
                DataRow match = items.AsEnumerable().FirstOrDefault(r => r["id"] != DBNull.Value && Convert.ToInt32(r["id"]) == id);
                if (match != null)
                    gridRow.Cells[uomCol].Value = match["unit_of_measure"]?.ToString() ?? "";
            }
        }

        // Every positive int in a table's column (skips blanks/non-numeric).
        public static HashSet<int> CollectIds(DataTable table, string column)
        {
            var ids = new HashSet<int>();
            if (table == null || !table.Columns.Contains(column))
                return ids;
            foreach (DataRow row in table.Rows)
            {
                if (int.TryParse(row[column]?.ToString(), out int id) && id > 0)
                    ids.Add(id);
            }
            return ids;
        }

        // Projects one light pump-picker row onto the fixed item schema
        // (same columns ItemPickerRow.MergeInto maintains).
        public static void AddPickerRow(DataTable table, smpc_sales_app.Services.Sales.ItemPickerRow row)
        {
            if (table == null || row == null || ContainsId(table, row.id))
                return;
            DataRow dataRow = table.NewRow();
            SetInt(table, dataRow, "id", row.id);
            SetText(table, dataRow, "item_code", row.item_code);
            SetText(table, dataRow, "item_name", row.item_name);
            SetText(table, dataRow, "item_model", row.item_model);
            SetInt(table, dataRow, "item_name_id", row.item_name_id);
            SetText(table, dataRow, "unit_of_measure", row.unit_of_measure);
            table.Rows.Add(dataRow);
        }

        // Every item id in a BOM subtree (parent + all descendants), walked
        // from the fully-loaded BOM tables: heads keyed by id with item_id,
        // details keyed by item_bom_id (head id) with item_id. Guards each
        // column so a shape change degrades to a partial set, never a throw.
        public static HashSet<int> CollectBomSubtreeIds(DataTable bomHead, DataTable bomDetails, int bomId)
        {
            var ids = new HashSet<int>();
            if (bomHead == null || bomDetails == null || bomId <= 0)
                return ids;
            if (!bomHead.Columns.Contains("id") || !bomHead.Columns.Contains("item_id") ||
                !bomDetails.Columns.Contains("item_bom_id") || !bomDetails.Columns.Contains("item_id"))
                return ids;

            var queue = new Queue<int>();
            queue.Enqueue(bomId);
            var seenBoms = new HashSet<int>();
            while (queue.Count > 0)
            {
                int current = queue.Dequeue();
                if (!seenBoms.Add(current))
                    continue;
                foreach (DataRow head in bomHead.Select("id = " + current))
                {
                    if (int.TryParse(head["item_id"]?.ToString(), out int headItem) && headItem > 0)
                        ids.Add(headItem);
                }
                foreach (DataRow child in bomDetails.Select("item_bom_id = " + current))
                {
                    if (!int.TryParse(child["item_id"]?.ToString(), out int childItem) || childItem <= 0)
                        continue;
                    ids.Add(childItem);
                    foreach (DataRow sub in bomHead.Select("item_id = " + childItem))
                    {
                        if (int.TryParse(sub["id"]?.ToString(), out int subBom) && subBom > 0)
                            queue.Enqueue(subBom);
                    }
                }
            }
            return ids;
        }
    }
}
