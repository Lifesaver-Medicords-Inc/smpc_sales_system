using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace smpc_app.Data
{
    static class STATIC_ENTITY_TYPE
    {
        public static DataTable ENTITYLIST()
        {

            IEnumerable<string> data= new List<string>()
            {
                "SUPPLIER",
                "CUSTOMER",
                "BLACKLISTED",
                "NON-AFFILIATED",
                "AFFILIATED",
                "CLOSED"
            };
            DataTable table = new DataTable();
            table.Columns.Add("code");
            table.Columns.Add("name");
           

            foreach (string item in data)
            {

                DataRow dr = table.NewRow();
                dr["name"] = item;
                dr["code"] = item;
            
                table.Rows.Add(dr);

            }

            return table;
        }
    }
}
