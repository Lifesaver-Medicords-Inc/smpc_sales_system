using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using smpc_sales_app.Services.Helpers;
using smpc_sales_system.Models;
using smpc_sales_system.Services.Setup;
using static smpc_sales_system.Services.Sales.Models.ItemBomListModel;

namespace smpc_sales_system.Services.Sales
{
    // The lookup lists every item-set tab needs: engineers (ASSIGNED ENGR.), project templates
    // (TEMPLATE), and the BOM catalogue (model picker and BOM quantity cascade).
    //
    // Each ItemSetUC used to fetch all of these itself, on every load. A quotation with five
    // tabs made five of each request; and since live sync rebuilds the tabs whenever the other
    // app saves, that happened again every few seconds while two people worked. The BOM call
    // is every BOM head and every BOM line in the system. (Each tab also fetched EVERY PROJECT
    // in the database and never used the result - that call is simply gone.)
    //
    // Now one request is shared: the first tab to ask starts it and every other tab awaits the
    // same task. Results are kept for a few minutes, so a live rebuild costs no requests at
    // all. A failed or empty fetch is not kept - the next caller tries again.
    //
    // Everything handed out is SHARED and must be treated as read-only. That is true of the BOM
    // tables (only ever queried). The engineer list is bound to a ComboBox, and two combos
    // bound to one list on one form share a selection - so callers copy it; see
    // EngineersForBinding.
    internal static class ItemSetLookups
    {
        public sealed class BomTables
        {
            public DataTable Head;
            public DataTable Details;
        }

        private sealed class Slot<T> where T : class
        {
            public Task<T> Task;
            public DateTime FetchedUtc;
        }

        private static Slot<List<EngineerModel>> _engineers;
        private static Slot<ProjectTemplateList> _templates;
        private static Slot<BomTables> _bom;

        private static readonly TimeSpan EngineersFor = TimeSpan.FromMinutes(10);
        private static readonly TimeSpan TemplatesFor = TimeSpan.FromMinutes(5);
        private static readonly TimeSpan BomFor = TimeSpan.FromMinutes(10);

        private static Task<T> Get<T>(ref Slot<T> slot, Func<Task<T>> fetch, TimeSpan keepFor) where T : class
        {
            Slot<T> current = slot;
            if (current != null && DateTime.UtcNow - current.FetchedUtc < keepFor && !IsSpent(current.Task))
                return current.Task;

            var fresh = new Slot<T> { Task = fetch(), FetchedUtc = DateTime.UtcNow };
            slot = fresh;
            return fresh.Task;
        }

        // A task worth handing out again: still running, or finished with a usable result.
        private static bool IsSpent<T>(Task<T> task) where T : class
        {
            if (task == null || task.IsFaulted || task.IsCanceled) return true;
            return task.Status == TaskStatus.RanToCompletion && task.Result == null;
        }

        public static Task<List<EngineerModel>> Engineers()
        {
            return Get(ref _engineers, EngineerService.GetEngineerList, EngineersFor);
        }

        // A private copy for a ComboBox DataSource - see the class note.
        public static async Task<List<EngineerModel>> EngineersForBinding()
        {
            List<EngineerModel> shared = await Engineers();
            return shared == null ? new List<EngineerModel>() : new List<EngineerModel>(shared);
        }

        public static Task<ProjectTemplateList> Templates()
        {
            return Get(ref _templates, ProjectTemplatesService.GetProjectTemplates, TemplatesFor);
        }

        // Template Setup calls this after saving, so a template just created or renamed shows
        // up on the next tab that loads instead of after the cache runs out.
        public static void InvalidateTemplates()
        {
            _templates = null;
        }

        public static Task<BomTables> Bom()
        {
            return Get(ref _bom, FetchBom, BomFor);
        }

        private static async Task<BomTables> FetchBom()
        {
            BomList bom = await ProjectService.GetBom();
            if (bom == null) return null;

            return new BomTables
            {
                Head = JsonHelper.ToDataTable(bom.bom_head),
                Details = JsonHelper.ToDataTable(bom.bom_details),
            };
        }

        // Names for Change History, from whatever has already been fetched. Never fetches -
        // history is built during a save, which must not wait on a lookup - so an id with no
        // name in hand yet is shown as the id.
        public static string EngineerName(int id)
        {
            var task = _engineers?.Task;
            if (task == null || task.Status != TaskStatus.RanToCompletion || task.Result == null) return null;
            return task.Result.FirstOrDefault(e => e.Id == id)?.FullName;
        }

        public static string TemplateName(int id)
        {
            var task = _templates?.Task;
            if (task == null || task.Status != TaskStatus.RanToCompletion || task.Result?.SalesProjectTemplate == null) return null;
            return task.Result.SalesProjectTemplate.FirstOrDefault(t => t.template_id == id)?.template_name;
        }
    }
}
