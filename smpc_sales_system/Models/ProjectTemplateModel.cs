using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace smpc_sales_system.Models
{
    public class ProjectTemplateModel
    {
        public int template_id { get; set; }
        public string template_name { get; set; }
    }

    public class ProjectTemplateChildModel
    {
        [JsonProperty("id")]
        public int Id { get; set; }
        [JsonProperty("parent_id")]
        public int ParentId { get; set; }
        [JsonProperty("item_id")]
        public int ItemId { get; set; }
        [JsonProperty("components")]
        public string Components { get; set; }
        // The item's name as Item Entry holds it today, joined on by the API and never
        // stored. Components is the text that was typed when the row was made; this is
        // what the row's item is actually called now.
        [JsonProperty("item_name")]
        public string ItemName { get; set; }
        [JsonProperty("level")]
        public int Level { get; set; }
        // Nullable on purpose: "no quantity set" and "quantity is zero" are
        // different states. Component rows that predate this column stay null
        // and apply to a project quotation as blank, so no existing quotation
        // total changes. Nothing defaults it - see the Go model's own note.
        [JsonProperty("qty")]
        public int? Qty { get; set; }

        // What a template row is called: its item's NAME as Item Entry has it today (the
        // item's item_name_id), so the template names a kind of component and follows a
        // rename. A note in brackets after the old name stays with it - "BLADDER TANK (at
        // least 60L)", "FLOAT SWITCH (cistern)" - because it says something the item's name
        // does not. With no name to go on (an API that does not send one), the stored text.
        public static string ComponentText(string components, string itemName)
        {
            string stored = (components ?? "").Trim();
            string name = (itemName ?? "").Trim();
            if (name.Length == 0) return stored;

            if (stored.StartsWith(name, StringComparison.OrdinalIgnoreCase))
                return name + stored.Substring(name.Length);

            var note = System.Text.RegularExpressions.Regex.Match(stored, @"\(.*\)\s*$");
            if (!note.Success || name.EndsWith(note.Value.Trim(), StringComparison.OrdinalIgnoreCase))
                return name;

            return name + " " + note.Value.Trim();
        }
    }

    public class ProjectTemplateList
    {
        public List<ProjectTemplateModel> SalesProjectTemplate { get; set; }
        public List<ProjectTemplateChildModel> sales_project_template_child { get; set; }
    }

}
