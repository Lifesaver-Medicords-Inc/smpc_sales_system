using Newtonsoft.Json;
using smpc_sales_system.Services.Sales.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace smpc_sales_system.Models
{
    class SalesProject
    {
        
    }

    public class SalesProjectMultiplier
    {
        public int multiplier_id { get; set; }
        public uint based_id { get; set; }
        public string brand { get; set; }
        public string component { get; set; }
        public string description { get; set; }
        public string multiplier { get; set; }
    }

    public class SalesProjectHistory
    {
        // The API's actual primary key for this row is "history_id" (models.SalesProjectHistory.HistoryID
        // on the Go side) - it was never returned as "id", so a plain "id" property here always
        // deserialized to 0 for every fetched row. That silently broke both the Change History sort
        // (OrderByDescending on a field that's always 0 is a no-op - list/insertion order, not
        // chronological order) and DiffModels' history matching (every db row collapsed onto the same
        // dictionary key, since the key selector also read the always-0 "id"). Replaced with the real key.
        public uint history_id { get; set; }
        public uint based_id { get; set; }
        public string user { get; set; }
        public string date { get; set; }
        public string time { get; set; }
        public string old_data { get; set; }
        public string new_data { get; set; }
    }

    // Which quotation a Change History row belongs to, and whether it is a PROJECT (header)
    // row or a tab row.
    //
    // tbl_trans_sales_project_history.based_id holds two different kinds of id: a TAB row
    // stores its item set's id, a PROJECT row stores the quotation's id. The two sequences
    // overlap - quotation 25 and item set 25 both exist - so "based_id == this quotation" also
    // picked up another quotation's tab rows, and "based_id is one of my item sets" could pick
    // up another quotation's header rows. That is part of why the history "combined" wrongly.
    //
    // Every row this system writes is labelled - PROJECT rows "PROJECT - ...", tab rows
    // "ITEM/SET ..." - and the older builders used other fixed prefixes, so the label settles
    // it. A row with a label from neither family is taken at face value.
    public static class ProjectHistoryRows
    {
        private static readonly string[] HeaderPrefixes =
            { "PROJECT - ", "Header -> ", "Footer -> ", "Project Header: ", "Multiplier Table -> " };

        private static readonly string[] TabPrefixes =
            { "ITEM/SET ", "Sub Project: ", "Advance Conditions: ", "Project Items Table -> ", "Project Wiring Table -> " };

        private static bool StartsWithAny(string text, string[] prefixes)
        {
            if (string.IsNullOrEmpty(text)) return false;
            foreach (string prefix in prefixes)
            {
                if (text.StartsWith(prefix, StringComparison.Ordinal)) return true;
            }
            return false;
        }

        public static bool IsProjectRow(SalesProjectHistory row, int quotationId)
        {
            return row != null && row.based_id == (uint)quotationId && !StartsWithAny(row.old_data, TabPrefixes);
        }

        public static bool IsTabRow(SalesProjectHistory row, ICollection<int> itemSetIds)
        {
            return row != null && itemSetIds.Contains((int)row.based_id) && !StartsWithAny(row.old_data, HeaderPrefixes);
        }
    }

    public class SalesProjectItemSet
    {
        public int itemset_id { get; set; }
        public int based_id { get; set; }
        public string tab_number { get; set; }
    }

    public class SalesProjectContent
    {
        public int content_id { get; set; }
        public int based_id { get; set; }
        public string item_designation { get; set; }
        public string item_set_description { get; set; }
        public string application { get; set; }
        public string additional { get; set; }
        public string flow { get; set; }
        public string head { get; set; }
        public string voltage { get; set; }
        public string rpm { get; set; }
        public string hp { get; set; }
        public string phase { get; set; }
        public string no_of_sets { get; set; }
        public string no_of_pump_set { get; set; }
        public string item_set_notes { get; set; }
        public int template_project_id { get; set; }
        public bool is_wiring { get; set; }

        // The client-needs unit dropdowns (FLOW: 1 gpm/lpm, 2 m^2/hr, 3 lps; HEAD: 1 psi,
        // 2 ft, 3 m) and ASSIGNED ENGR. ItemSetUC has always put all three in the save
        // payload, and the API has the columns - but this class had no property for any of
        // them, so they were dropped the moment the payload was read into it (on save) and
        // never existed in the table the panel is bound from (on load). Every change to them
        // was lost, and every reload showed the default (user-reported 2026-09-24).
        //
        // Nullable: "not in the payload" has to stay distinguishable from "set to 0", or a
        // save that simply did not carry one of these would erase it. See GetContentChanges.
        public int? flow_id { get; set; }
        public int? head_id { get; set; }
        public int? assign_engineer_user_id { get; set; }

        // §5.1.4's right-click tab exclusion. Persisted as of 2026-09-05 - it was a
        // HashSet<TabPage> in Quotation.cs and nothing else, so it never survived a reload
        // and the print modal could not see it.
        //
        // Nullable deliberately. The column was added to a table that already had rows, so
        // every existing content row holds NULL, and the Go model sends it through as a
        // *bool - deserializing that into a plain bool threw "Null object cannot be
        // converted to a value type" on /sales/projects, which took out the whole Project
        // tab rather than just this field. A tab with no flag recorded is simply not
        // excluded; see IsExcluded.
        public bool? is_excluded { get; set; }

        // Null means "never flagged", which is not excluded.
        public bool IsExcluded => is_excluded == true;

        public SalesProjectContentFinal []sales_project_content_final { get; set; }
        public SalesProjectSizeUp []sales_project_size_up { get; set; }
    }

    public class SalesProjectContentFinal
    {
        public int id { get; set; }
        public int sales_project_content_id { get; set; }
        // item_id added 2026-09-03. Finals had no item id, so a row reloaded from the
        // database came back with a blank final_item_id - and that is the column
        // ItemSetUC.SetFinalPumpData compares against to reject a pump already on the
        // list. With it blank the guard could never match, so re-picking the same pump
        // after a reload appended a duplicate row instead of being ignored, and the
        // next save wrote both. Size Up always had item_id, which is why its own
        // picker guard (AddSizeUpRow) never had this problem.
        public int item_id { get; set; }
        public string final { get; set; }
        public decimal fla { get; set; }
        public decimal voltage { get; set; }
    }

    // Size Up - spec 5.1.4's list of candidate pumps that Final Selection is limited to.
    // Had no model, no table and no save path at all before; the grid was in-session only.
    public class SalesProjectSizeUp
    {
        public int id { get; set; }
        public int sales_project_content_id { get; set; }
        public int item_id { get; set; }
        public string model { get; set; }
    }


    public class SalesProjectAdvancedConditions
    {
        public int conditions_id { get; set; }
        public int based_id { get; set; }
        public string pump_brand { get; set; }
        public string driver_type { get; set; }
        public string pressure { get; set; }
        public string motor_enclosure { get; set; }
        public string motor_manufacturer { get; set; }
        public string liquid_type { get; set; }
        public string controller_manufacturer { get; set; }
        public string starting_method { get; set; }
        public string suction_size { get; set; }
        public string discharge_size { get; set; }

        // The six Advanced Conditions dropdowns store an id, not text (2026-09-24). The
        // dropdowns were built and the API and database were given the columns, but this
        // class never got the properties - so what the user picked was dropped on save,
        // and on load every dropdown came back as "-- Select --" because the column was not
        // in the table it binds from. That is the "Advanced Conditions not updating on either
        // side" report. The old text columns above stay for rows saved before the change.
        //
        // Nullable for the same reason as the content ids: absent is not the same as 0.
        public int? pump_brand_id { get; set; }
        public int? driver_type_id { get; set; }
        public int? motor_enclosure_id { get; set; }
        public int? motor_manufacturer_id { get; set; }
        public int? liquid_type_id { get; set; }
        public int? controller_manufacturer_id { get; set; }
    }

    public class SalesProjectItems
    {
        public int items_id { get; set; }
        public int bom_id { get; set; }
        public int item_id { get; set; }
        public int based_id { get; set; }

        public string reference_code {get; set;}
        public int man_days { get; set; }
        public decimal labor_rate { get; set; }
        public string components { get; set; }
        public string model { get; set; }
        public string item_inv_type { get; set; }
        public int qty { get; set; }
        public decimal list_price_per_unit {get; set; }
        public decimal unit_price { get; set; }
        public string multiplier { get; set; }

        public decimal discount_price { get; set; }
        public decimal component_total { get; set; }
        public string notes { get; set; }
        public int template_id { get; set; }

        // Only used when SENDING this item to Create (mirrors how Quick Quote items carry
        // "quick_selected_image" alongside their own fields) - on the way back from GET this
        // stays null, since selected images come back separately as
        // SalesProjectList.sales_project_items_selected_images and get matched up client-side
        // by items_id, the same way Quick Quote matches by quotation_quick_id.
        public List<Dictionary<string, object>> quick_selected_image { get; set; }
    }

    public class SalesProjectList
    {
        public List<SalesQuotationModel> SalesQuotation { get; set; }
        public List<SalesProjectMultiplier> sales_project_multiplier { get; set; }
        public List<SalesProjectHistory> sales_project_history { get; set; }
        public List<SalesProjectItemSet> sales_project_item_set { get; set; }
        public List<SalesProjectContent> sales_project_content { get; set; }
        public List<SalesProjectAdvancedConditions> sales_project_content_advanced_condition { get; set; }
        public List<SalesProjectItems> sales_project_items { get; set; }
        public List<SalesWiringModel> sales_project_wiring { get; set; }
        public List<SalesQuotationSelectedImageModel> sales_project_items_selected_images { get; set; }
    }

}
