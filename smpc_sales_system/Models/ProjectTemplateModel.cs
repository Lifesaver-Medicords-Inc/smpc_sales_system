using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace smpc_sales_system.Models
{
    public class ProjectTemplateModel
    {
        public int template_id { get; set; }
        public string template_name { get; set; }
    }



    public class ProjectTemplateChildModel
    {
        public int nodes_id { get; set; }
        public int node_id { get; set; }
        public int parent_node_id { get; set; }
        public int based_id { get; set; }
        public string node_name { get; set; }
        public int node_level { get; set; }
        public int node_order { get; set; }
        public int item_id { get; set; }
        public string node_type { get; set; }
    }

    public class ProjectTemplateList
    {
        public List<ProjectTemplateModel> SalesProjectTemplate { get; set; }
        public List<ProjectTemplateChildModel> sales_project_template_child { get; set; }
    }

}
