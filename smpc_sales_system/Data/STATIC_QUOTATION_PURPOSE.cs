using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace smpc_app.Data
{
    internal class STATIC_QUOTATION_PURPOSE
    { 
        public static DataTable LIST()
        {
            IEnumerable<string> data = new List<String>()
            {
                "BUDGET",
                "BIDDING",
                "AWARDED"
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
