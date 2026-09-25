using System.Data;

namespace smpc_app.Data
{
    // The Advanced Conditions dropdowns on a project quotation's item set.
    //
    // Hard-coded for now (user, 2026-09-24), but what the quotation STORES is the id, not
    // the text - so turning any of these into a Setup list later is a change to the list
    // and nothing else. Renumbering would mean rewriting every quotation already saved:
    //
    //   THE IDS ARE FIXED. Append at the end, never renumber, never reuse a retired id.
    //
    // BRAND is not here: it comes from the brand setup list Item Entry uses
    // (GET /setup/item/brand), so a brand renamed in setup reads correctly everywhere.
    //
    // Every list starts with "-- Select --" at id 0, because a dropdown nobody has
    // answered is not the same as one answered with the first value (spec 1.3).
    static class STATIC_ADVANCED_CONDITIONS
    {
        public const string SELECT_TEXT = "-- Select --";

        public static DataTable DRIVER_TYPE()
        {
            return Build("Electric Motor", "Engine");
        }

        public static DataTable MOTOR_ENCLOSURE()
        {
            return Build("NEMA 2", "NEMA 3", "NEMA 4", "NEMA 12");
        }

        public static DataTable MOTOR_MANUFACTURER()
        {
            return Build("Teco Motor", "Franklin Motor", "Calpeda Motor");
        }

        public static DataTable LIQUID_TYPE()
        {
            return Build("Clean", "Sewage", "Seawater");
        }

        public static DataTable CONTROLLER_MANUFACTURER()
        {
            return Build("Eaton", "Marathon", "WEG");
        }

        // A two-column table the combo binds to: id as text, so ComboBox.SelectedValue
        // matches what Helpers.BindControls assigns it - the id read off the record
        // arrives as a string.
        public static DataTable Build(params string[] values)
        {
            DataTable table = NewTable();
            for (int i = 0; i < values.Length; i++)
                table.Rows.Add((i + 1).ToString(), values[i]);

            return table;
        }

        // The same shape, headed by "-- Select --", for a list that comes from the server.
        public static DataTable NewTable()
        {
            DataTable table = new DataTable();
            table.Columns.Add("id", typeof(string));
            table.Columns.Add("title", typeof(string));
            table.Rows.Add("0", SELECT_TEXT);
            return table;
        }
    }
}
