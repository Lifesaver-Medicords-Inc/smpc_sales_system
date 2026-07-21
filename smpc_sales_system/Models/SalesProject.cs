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
        public int id { get; set; }
        public uint based_id { get; set; }
        public string user { get; set; }
        public string date { get; set; }
        public string time { get; set; }
        public string old_data { get; set; }
        public string new_data { get; set; }
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
        public SalesProjectContentFinal []sales_project_content_final { get; set; }
    }

    public class SalesProjectContentFinal
    {
        public int id { get; set; }
        public int sales_project_content_id { get; set; }
        public string final { get; set; }
        public decimal fla { get; set; }
        public decimal voltage { get; set; }
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
