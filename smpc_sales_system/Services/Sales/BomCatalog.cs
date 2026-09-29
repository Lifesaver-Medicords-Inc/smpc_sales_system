using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using smpc_sales_app.Services.Helpers;
using static smpc_sales_system.Services.Sales.Models.ItemBomListModel;

namespace smpc_sales_system.Services.Sales
{
    // The BOMs the quotation screens (Sales and Engineering) have needed so far, fetched one BOM
    // at a time - each with every sub-BOM under it - the moment a BOM is picked or a BOM row's
    // quantity is changed, and shared by every screen and tab in the app.
    //
    // Every one of those screens used to download GET /setup/bom: every BOM head and every BOM
    // line in the system - 5,842 heads and 130,380 lines, 32.6 MB of JSON on test_fresh - and
    // turn it into DataTables, on every quotation opened, again in each Project tab's shared
    // lookups, again in Engineering's grid editor and again in Template Setup, to expand the one
    // or two BOMs a user picks (user request 2026-09-26: "optimize the BOM ... also update the
    // engineering side"). GET /setup/bom/tree?ids= returns just those.
    //
    // Head and Details are the same two tables for the life of the app - only their rows
    // change - so a screen can hold on to them. They have the columns GET /setup/bom's lists
    // produced (JsonHelper.ToDataTable over the same models), so every query written against
    // the old tables reads these unchanged. Everything is on the UI thread; a merge never runs
    // while a query does.
    //
    // A BOM is re-fetched after KeepFor, so a BOM edited in Inventory reaches an open app. An
    // API older than 2026-09-26 has no /tree: then the whole catalogue is read once, as before,
    // and kept for KeepFor.
    internal static class BomCatalog
    {
        private const string TreeUrl = "/setup/bom/tree?ids=";
        private const int IdsPerRequest = 200;
        private static readonly TimeSpan KeepFor = TimeSpan.FromMinutes(10);

        public static DataTable Head { get; } = JsonHelper.ToDataTable(new List<ItemBomList>());
        public static DataTable Details { get; } = JsonHelper.ToDataTable(new List<ItemBomDetails>());

        private static readonly Dictionary<int, DateTime> _fetchedUtc = new Dictionary<int, DateTime>();
        private static bool _treeMissing;
        private static DateTime _everythingUtc = DateTime.MinValue;

        public static bool IsLoaded(int bomId)
        {
            if (bomId <= 0) return true;
            if (DateTime.UtcNow - _everythingUtc < KeepFor) return true;
            return _fetchedUtc.TryGetValue(bomId, out DateTime at) && DateTime.UtcNow - at < KeepFor;
        }

        public static Task<bool> EnsureAsync(int bomId)
        {
            return EnsureAsync(new[] { bomId });
        }

        // True when every BOM asked for (and its sub-BOMs) is in the tables - or turned out not
        // to exist. False when the API could not be reached; the tables are left as they were.
        public static async Task<bool> EnsureAsync(IEnumerable<int> bomIds)
        {
            List<int> wanted = (bomIds ?? Enumerable.Empty<int>())
                .Where(id => id > 0)
                .Distinct()
                .Where(id => !IsLoaded(id))
                .ToList();
            if (wanted.Count == 0) return true;

            if (_treeMissing)
                return await LoadEverythingAsync();

            bool allAnswered = true;
            for (int start = 0; start < wanted.Count; start += IdsPerRequest)
            {
                List<int> chunk = wanted.Skip(start).Take(IdsPerRequest).ToList();

                // Silent: a missing route must not raise the connection dialog - see below.
                var response = await RequestToApi<smpc_app.Services.Helpers.ApiResponseModel<BomList>>.Get(
                    TreeUrl + string.Join(",", chunk), silent: true);

                if (response != null && response.Success && response.Data != null)
                {
                    Merge(response.Data, chunk);
                    continue;
                }

                // An error from the API carries its own message. Nothing at all is either no
                // connection or an API without /tree; the full download tells the two apart.
                if (response != null && !string.IsNullOrEmpty(response.message))
                {
                    allAnswered = false;
                    continue;
                }

                if (await LoadEverythingAsync())
                {
                    _treeMissing = true;
                    return true;
                }
                return false;
            }

            return allAnswered;
        }

        // Cascades a BOM row's new quantity to its children (Helpers.CascadeBomQuantity) now if
        // its BOM is in hand, otherwise once it has been fetched - a quotation opened from the
        // database has rows of BOMs this session never expanded. afterLateCascade runs only in
        // the second case, for the grid's recompute that the caller already ran on the first.
        public static void CascadeQuantity(DataGridView dgv, int rowIndex,
            string qtyColumn, string referenceCodeColumn, string itemIdColumn, string bomIdColumn,
            Action afterLateCascade = null)
        {
            if (dgv == null || rowIndex < 0 || rowIndex >= dgv.Rows.Count) return;

            DataGridViewRow row = dgv.Rows[rowIndex];
            if (row.IsNewRow) return;

            int.TryParse(row.Cells[bomIdColumn].Value?.ToString(), out int bomId);
            if (IsLoaded(bomId))
            {
                smpc_app.Services.Helpers.Helpers.CascadeBomQuantity(dgv, Details, rowIndex,
                    qtyColumn, referenceCodeColumn, itemIdColumn, bomIdColumn);
                return;
            }

            string referenceCode = row.Cells[referenceCodeColumn].Value?.ToString();
            var ignored = CascadeWhenLoadedAsync(dgv, bomId, referenceCode,
                qtyColumn, referenceCodeColumn, itemIdColumn, bomIdColumn, afterLateCascade);
        }

        private static async Task CascadeWhenLoadedAsync(DataGridView dgv, int bomId, string referenceCode,
            string qtyColumn, string referenceCodeColumn, string itemIdColumn, string bomIdColumn,
            Action afterLateCascade)
        {
            if (!await EnsureAsync(bomId)) return;
            if (dgv.IsDisposed || string.IsNullOrEmpty(referenceCode)) return;

            // The grid may have moved on while the BOM was fetched: find the row again by its
            // code and BOM, and leave it alone if it is gone.
            foreach (DataGridViewRow row in dgv.Rows)
            {
                if (row.IsNewRow) continue;
                if (row.Cells[referenceCodeColumn].Value?.ToString() != referenceCode) continue;
                if (!int.TryParse(row.Cells[bomIdColumn].Value?.ToString(), out int rowBom) || rowBom != bomId) continue;

                smpc_app.Services.Helpers.Helpers.CascadeBomQuantity(dgv, Details, row.Index,
                    qtyColumn, referenceCodeColumn, itemIdColumn, bomIdColumn);
                afterLateCascade?.Invoke();
                return;
            }
        }

        // Replaces the rows of every BOM in the answer - and of every BOM asked for, which may
        // have been deleted since - with the answer's.
        private static void Merge(BomList data, IEnumerable<int> asked)
        {
            DataTable heads = JsonHelper.ToDataTable(data.bom_head ?? new List<ItemBomList>());
            DataTable lines = JsonHelper.ToDataTable(data.bom_details ?? new List<ItemBomDetails>());

            var replaced = new HashSet<int>(asked);
            foreach (DataRow head in heads.Rows)
                replaced.Add(Convert.ToInt32(head["id"]));

            RemoveWhere(Head, "id", replaced);
            RemoveWhere(Details, "item_bom_id", replaced);

            Head.BeginLoadData();
            foreach (DataRow head in heads.Rows) Head.ImportRow(head);
            Head.EndLoadData();

            Details.BeginLoadData();
            foreach (DataRow line in lines.Rows) Details.ImportRow(line);
            Details.EndLoadData();

            DateTime now = DateTime.UtcNow;
            foreach (int id in replaced)
                _fetchedUtc[id] = now;
        }

        private static void RemoveWhere(DataTable table, string column, HashSet<int> ids)
        {
            var doomed = table.Rows.Cast<DataRow>()
                .Where(r => ids.Contains(Convert.ToInt32(r[column])))
                .ToList();
            foreach (DataRow row in doomed)
                table.Rows.Remove(row);
        }

        // The whole catalogue, as every screen read it before /tree existed.
        private static async Task<bool> LoadEverythingAsync()
        {
            BomList all = await ProjectService.GetBom();
            if (all == null || all.bom_head == null) return false;

            Head.Clear();
            Details.Clear();
            _fetchedUtc.Clear();
            Merge(all, Enumerable.Empty<int>());
            _everythingUtc = DateTime.UtcNow;
            return true;
        }
    }
}
