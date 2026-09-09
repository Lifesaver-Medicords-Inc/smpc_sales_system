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
        [JsonProperty("level")]
        public int Level { get; set; }
        // Nullable on purpose: "no quantity set" and "quantity is zero" are
        // different states. Component rows that predate this column stay null
        // and apply to a project quotation as blank, so no existing quotation
        // total changes. Nothing defaults it - see the Go model's own note.
        [JsonProperty("qty")]
        public int? Qty { get; set; }
    }

    public class ProjectTemplateList
    {
        public List<ProjectTemplateModel> SalesProjectTemplate { get; set; }
        public List<ProjectTemplateChildModel> sales_project_template_child { get; set; }
    }

}
