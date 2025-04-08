using smpc_sales_app.Pages.Sales;
using smpc_sales_system.Pages.Sales;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace smpc_sales_app.Services
{
    class RoutesServices
    {
        private Dictionary<string, Control> _pages = new Dictionary<string, Control>()
        {
            //========================================================================
            // SETUP  
            //{ "OPPORTUNITIES", new frm_opportunities() },
            //{ "BUSINESS PARTNER INFO", new frm_Business_Partner_Info()  },

            //{ "ITEM SETUP", new frm_item()  },
            //{ "ITEM TYPE SETUP", new frm_item_type_setup()  },
            //{ "ITEM CLASS SETUP", new frm_item_class_setup()  },
            //{ "UNIT MEASURE SETUP", new frm_unit_measure_setup()  },
            //{ "BRAND SETUP", new frm_brand_setup()  },


            // OTHER SETUP
            //{ "PAYMENT TERMS", new frm_payment_terms_setup()  },


            //========================================================================
            // TRANSACTIONS 
            { "Sales Quotation", new Quotation() },
            { "Sales Order", new Orders() },
            { "Opportunities", new Opportunities() },
            { "Purchase Requisition", new PurchaseRequisition() },
            { "CRM", new CRM() },

            // application setup transaction
            { "Application Setup", new Applications() },
            { "Ship Type Setup", new ShipTypeSetup() },
            { "Template Setup", new TemplateSelectionModal() },

        };

        private string _selectedRoute;
        public RoutesServices(string selectedRoute)
        {
            this._selectedRoute = selectedRoute;
        }

        public Control GetForm()
        {
            return _pages.First(v => v.Key == this._selectedRoute).Value;
        }

        public String GetTitle()
        {
            return _pages.First(v => v.Key == this._selectedRoute).Key;
        }
    }
}
