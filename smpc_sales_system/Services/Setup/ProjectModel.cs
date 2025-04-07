using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace smpc_sales_system.Services.Setup
{
    class Project
    {
        public int id { get; set; }
        public string project_name { get; set; }
        public string customer_name { get; set; }
        public int web_socket_id { get; set; }

    }

    class ProjectList
    {
        public List<Project> Projects { get; set; }
    }
}
