using smpc_inventory_app.Services.Setup;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace smpc_app.Services.Helpers
{
    internal class ApiResponseModel<T>
    {
        public bool Success { get; set; }
        public T Data { get; set; }
        public string message { get; set; }
        public string token { get; set; }
        public PaginationModel pagination { get; set; } = null;
    }

    internal class ApiResponseModel
    {
        public bool Success { get; set; }
        public string message { get; set; }
        public string Message { get; set; }
        public dynamic Data { get; set; }
    }
    // This is the PaginationModel the sales app actually binds: it is declared in this
    // namespace, which wins over the smpc_inventory_app.Services.Setup one imported above.
    // The last four are sent by the endpoints that page in both directions or show a page
    // counter (the quotation's item and model pickers); older callers leave them at zero.
    public class PaginationModel
    {
        public bool has_next { get; set; }
        public int page_size { get; set; }
        public bool has_prev { get; set; }
        public int page { get; set; }
        public int total_pages { get; set; }
        public long total { get; set; }
    }
}
