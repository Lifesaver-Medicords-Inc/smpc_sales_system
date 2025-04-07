using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace smpc_sales_system.Models
{
    class SalesCanvasModel
    {
        public int id { get; set; }
        public int supplier_based_id { get; set; }
        public int item_based_id { get; set; }
        public decimal net_price { get; set; }
        public string discount { get; set; }
        public decimal unit_price { get; set; }
        public string start_date { get; set; }
        public string validity { get; set; }
        public int lead_time { get; set; }
    }

    public class SalesCanvasView
    {
        public int id { get; set; }
        public int supplier_based_id { get; set; }
        public int item_based_id { get; set; }
        public decimal net_price { get; set; }
        public string discount { get; set; }
        public decimal unit_price { get; set; }
        public string validity { get; set; }
        public int lead_time { get; set; }
        public int RemainingDays { get; set; }
    }

    public class CanvasViewList
    {
        public List<SalesCanvasView> sales_canvas_sheet_view { get; set; }

    }

}
