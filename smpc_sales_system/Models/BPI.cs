using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace smpc_sales_system.Models
{
    class Bpi
    {

        public int id { get; set; }

        public string sales_id { get; set; }

        public string name { get; set; }

        public string website { get; set; }

        public string tin { get; set; }

        public string tel_no { get; set; }

        public string industry_ids { get; set; }

        public string industry_names { get; set; }


    }

    class BpiGeneral
    {

        //public int id { get; set; }

        public int general_based_id { get; set; }
        public int social_id { get; set; }
        public string branch_name { get; set; }

        public string transaction_type { get; set; }

        public string class_name { get; set; }

        public string branch_tel_no { get; set; }
        public string branch_website { get; set; }
        public string customer_code { get; set; }
        public string supplier_code { get; set; }

        public string fax_no { get; set; }
        public string notes { get; set; }
        public string entity_ids { get; set; }
        public string entity_names { get; set; }

        public string branch_industry_names { get; set; }
        public string branch_industry_ids { get; set; }

    }

    class BpiContacts
    {

        public int contacts_id { get; set; }
        public int contacts_based_id { get; set; }
        public string number { get; set; }
        public string name { get; set; }
        public string email { get; set; }
        public string preferences { get; set; }
        public int position { get; set; }

    }

    class BpiAddress
    {
        public int address_ids { get; set; }
        public int address_based_id { get; set; }
        public string location { get; set; }
    }

    class BpiItems
    {
        public int bpi_itembased_id { get; set; }
        public float price { get; set; }
        public int item_id { get; set; }
    }

    class BpiSuppliers
    {
        public int id { get; set; }
        public int based_id { get; set; }   
        public string supplier_code { get; set; }
        public int item_id { get; set; }
        public string item_name { get; set; }
        public float price { get; set; }
    }
    class BpiSupplierList
    {
        public List<BpiSuppliers> BpiSuppliers { get; set; }
    }









    

    //class Bpi
    class Bpi_Class
    {
        public List<Bpi> bpi { get; set; }
        public List<BpiGeneral> general { get; set; }
        public List<BpiContacts> contacts { get; set; }
        public List<BpiAddress> address { get; set; }
        public List<BpiItems> items { get; set; }

    }
}
