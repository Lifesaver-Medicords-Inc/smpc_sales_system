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
            table.Columns.Add("id", typeof(int));
            table.Columns.Add("title", typeof(string));
            table.Columns.Add("value", typeof(string));

            int id = 1;

            foreach (string item in data)
            {

                DataRow dr = table.NewRow();
                dr["id"] = id++;
                dr["title"] = item;
                dr["value"] = item;

                table.Rows.Add(dr);
            }

            return table;
        }

    }
}
