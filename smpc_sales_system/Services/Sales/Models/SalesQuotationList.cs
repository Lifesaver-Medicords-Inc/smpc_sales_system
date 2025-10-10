using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace smpc_sales_system.Services.Sales.Models
{
    class SalesQuotationList
    {
        //public List<SalesQuotationModel> SalesQuotationModel { get; set; }

        // PARENT
        public List<SalesQuotationModel> SalesQuotation { get; set; }


        // CHILD
        public List<SalesQuotationQuicksModel> SalesQuotationQuick { get; set; }
        public List<SalesQuotationSelectedImageModel> SalesQuotationSelectedImages { get; set; }

    }
}
