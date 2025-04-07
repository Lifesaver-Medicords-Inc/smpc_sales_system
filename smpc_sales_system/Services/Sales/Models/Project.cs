using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace smpc_sales_system.Services.Sales.Models
{
    class Project
    {
        public int id { get; set; }
        public string project_name { get; set; }
        public string customer_name { get; set; }
        public string test_1 { get; set; }
        public string test_2 { get; set; }
        public string test_3 { get; set; }
        public string test_4 { get; set; }
        public string test_5 { get; set; }
        public string test_6 { get; set; }
    }

    class ProjectList
    {
        public List<Project> Projects { get; set; }
    }
}
