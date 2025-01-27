using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace smpc_app.Data
{ 
    static class STATIC_WARRANTY
    { 
        public static DataTable LIST()
        {
            IEnumerable<string> data = new List<String>()
            {
                "1 YEAR",
                "2 YEAR",
                "3 YEAR",
            };

            DataTable table = new DataTable();
            table.Columns.Add("title");
            table.Columns.Add("value");

            foreach (string item in data)
            {

                DataRow dr = table.NewRow();
                dr["title"] = item;
                dr["value"] = item;

                table.Rows.Add(dr); 
            }

            return table;
        }
    }
         
}
