using System.Data;

namespace smpc_app.Data
{
    // The units beside FLOW and HEAD on a project quotation's CLIENT NEEDS panel.
    //
    // Hard-coded for now, on the user's instruction (2026-09-23) - but what the quotation
    // STORES is the id, not the text, so that turning either of these into a Setup list
    // later is a change to the list and nothing else. Renumbering, on the other hand, would
    // mean rewriting flow_unit_id / head_unit_id on every item set ever saved, so:
    //
    //   THE IDS ARE FIXED. Append at the end, never renumber, never reuse a retired id.
    //
    // The ids the user set:
    //
    //     FLOW  1 = gpm lpm     HEAD  1 = psi
    //           2 = m^2/hr            2 = ft
    //           3 = lps               3 = m
    //
    // Spec §5.1 lists FLOW as (lps, lpm, gpm, m³/hr) and HEAD as (m, psi, ft). The head
    // lists agree apart from order, which the id already makes irrelevant. The flow lists
    // do not: "gpm lpm" is two units in one entry, and m^2/hr is an area per hour where the
    // spec says m³/hr, a volume. Raised on 2026-09-23 and the current lists kept as they
    // are. Splitting "gpm lpm" later is additive - gpm and lpm become ids 4 and 5, and
    // 1 stays what it has always meant.
    static class STATIC_CLIENT_NEEDS_UNIT
    {
        public static DataTable FLOW()
        {
            return Build("gpm lpm", "m^2/hr", "lps");
        }

        public static DataTable HEAD()
        {
            return Build("psi", "ft", "m");
        }

        // id is a string column so ComboBox.SelectedValue matches what BindControls assigns
        // it - the value read off the record arrives as text.
        private static DataTable Build(params string[] units)
        {
            DataTable table = new DataTable();
            table.Columns.Add("id", typeof(string));
            table.Columns.Add("title", typeof(string));

            for (int i = 0; i < units.Length; i++)
                table.Rows.Add((i + 1).ToString(), units[i]);

            return table;
        }
    }
}
