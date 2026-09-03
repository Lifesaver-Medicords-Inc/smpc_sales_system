using System;

namespace smpc_app.Services.Helpers
{
    // Single source of truth for document-number prefix handling across the Sales app.
    //
    // Document numbers are shown with a type prefix (Q# draft quote, FQ# finalized quote,
    // SO# sales order, and for related docs SRT#/SI#/DR#) but stored differently per record
    // type - quotations keep the prefix in document_no, orders store bare. Historically the
    // add/strip logic was copied into four near-duplicate private helpers plus a dozen inline
    // "StartsWith(...) ? Substring(...)" / Regex.Replace / raw "FQ#" + x sites, which is how
    // a value already carrying "Q#" got "FQ#" prepended without stripping first, producing the
    // doubled "FQ#Q#0007" on the SO reference doc. Routing every strip/prepend through here
    // makes that class of bug impossible.
    //
    // This is presentation/normalization only. It does NOT change what is sent to or stored on
    // the server: callers still decide whether to send a bare or a prefixed value.
    public static class DocumentNo
    {
        // Longest / compound prefixes first so "FQ#" is matched before "Q#". Every prefix the
        // Sales app decorates a document number with.
        private static readonly string[] Prefixes = { "FQ#", "SRT#", "SO#", "SI#", "DR#", "Q#" };

        // Removes leading known prefixes, repeatedly, until none remain - so a doubled
        // "FQ#Q#0005" collapses all the way to "0005", not just "Q#0005". A bare number is
        // returned unchanged, so this is safe to call on a value that may or may not be
        // prefixed. Null/empty pass through.
        public static string Strip(string value)
        {
            if (string.IsNullOrEmpty(value)) return value;

            string v = value.Trim();
            bool removed = true;
            while (removed)
            {
                removed = false;
                foreach (string p in Prefixes)
                {
                    if (v.StartsWith(p, StringComparison.OrdinalIgnoreCase))
                    {
                        v = v.Substring(p.Length);
                        removed = true;
                        break;
                    }
                }
            }
            return v;
        }

        // Applies exactly one prefix. Because it strips first, re-decorating an already-
        // prefixed (or doubly-prefixed) value can never double it: Apply("Q#0005", "FQ#") and
        // Apply("FQ#Q#0005", "FQ#") both yield "FQ#0005", and Apply("0005", "FQ#") too.
        public static string Apply(string value, string prefix)
        {
            return prefix + Strip(value);
        }
    }
}
