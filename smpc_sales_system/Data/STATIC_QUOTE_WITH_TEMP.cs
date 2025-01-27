using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace smpc_app.Data
{
    static class STATIC_QUOTE_WITH_TEMP
    {
        public static DataTable QUOTE_LIST()
        {
            IEnumerable<string> data = new List<string>()
            {
                "CPS",
                "TRANSFER PUMP",
                "BOOSTER PUMP",
                "SEWAGE DRAINAGE",
                "NON-ULFM FIRE PUMP",
                "NON-ULFM JOCKEY PUMP"
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
        public static DataTable CPS()
        {
            IEnumerable<string> data = new List<String>()
            {
                "PUMP",
                "CONTROLLER",
                "VFD",
                "COMMON PACKAGE",
                "DISCHARGE COMMON HEADER",
            };

            DataTable table = new DataTable();
            table.Columns.Add("SUB PARTS");
            table.Columns.Add("value");

            foreach (string item in data)
            {

                DataRow dr = table.NewRow();
                dr["SUB PARTS"] = item;

                dr["value"] = "";

                table.Rows.Add(dr);

            }

            return table;
        }
    }
}
