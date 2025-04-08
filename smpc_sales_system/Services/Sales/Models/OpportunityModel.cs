using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace smpc_sales_system.Services.Sales.Models
{
    class OpportunityModel
    {
        public int id { get; set; }
        public string tag { get; set; }
        public string quote_ref { get; set; }
    }   
    class OpportunityViewModel
    {

        public int id { get; set; }
        public string tag { get; set; }
        public string version_no { get; set; }
        public bool is_finalized { get; set; }
        public string document_no { get; set; }
        public string branch_name { get; set; }
        public string project_name { get; set; }
        public string date { get; set; }
        public string client_req { get; set; }
        public float total_amount_due { get; set; }
        public string last_update { get; set; }
        public string stage { get; set; }
        public string status { get; set; }
        public string special_deal { get; set; }


    }
}
