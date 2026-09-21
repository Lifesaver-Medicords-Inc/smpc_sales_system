using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using smpc_sales_app.Data;

namespace smpc_sales_system.Models
{
    // Hides the sidebar entries this user's position has no access to. Same rules as the
    // inventory, engineering, accounting and dispatching apps, against the same grants:
    // tbl_position_access, seeded per position from the access-level workbook (ERP_API's
    // SeedPositionAccess) and maintained through Admin's Access Control screen.
    //
    // An entry with no code below stays visible, and so does the whole sidebar for a position
    // carrying no grants at all - a configuration gap should not blank the app.
    internal static class NavigationAccess
    {
        // Sidebar node name (Layout.Designer.cs TreeNode.Name) -> the access codes that open
        // it. Sales Order Approvals and Sales Return are absent: neither has a code in the
        // catalogue yet, so they are left visible rather than guessed at. Sales Calendar,
        // Item Entry, the two inventory entries and Item Request are catalogued under the
        // apps those screens came from - a code is granted to a position, not to an app.
        private static readonly Dictionary<string, string[]> NodeCodes =
            new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
            {
                { "Dashboard", new[] { "Main Shell.Dashboard Widget (Client Retention / Quotes & Sales Orders)" } },
                { "Business Partners", new[] { "Business Partner Info.Business Partner Info (main)" } },
                { "CRM", new[] { "Sales - CRM.CRM" } },
                { "Opportunities", new[] { "Sales - Opportunities.Opportunities" } },
                { "Sales Quotation", new[] { "Sales - Quotation.Quotation" } },
                { "Sales Order", new[] { "Sales - Order.Orders" } },
                { "Sales Calendar", new[] { "Sales Calendar.Calendar View" } },
                { "Item Entry", new[] { "Item.Item Entry (main)" } },
                { "Purchase Requisition", new[] { "Purchasing.Purchase Requisition" } },
                { "Logbook", new[] { "Inventory.Inventory Logbook" } },
                { "Tracker", new[] { "Inventory.Inventory Tracker" } },
                { "Item Request", new[] { "Item Request.Item Request", "Item Request.Item Request (v2)" } },
                { "Ship Type Setup", new[] { "Setup / Configuration.Ship Type Setup" } },
                { "Application Setup", new[] { "Setup / Configuration.Application Setup" } },
                { "Template Setup", new[] { "Setup / Configuration.Template Setup (list)" } },
            };

        private static HashSet<string> GrantedCodes()
        {
            var granted = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var access = CacheData.CurrentUser?.position?.access;

            if (access == null)
                return granted;

            foreach (var row in access)
            {
                if (!string.IsNullOrWhiteSpace(row?.code))
                    granted.Add(row.code.Trim());
            }

            return granted;
        }

        public static bool HasAccess(params string[] codes)
        {
            if (codes == null || codes.Length == 0)
                return true;

            var granted = GrantedCodes();

            if (granted.Count == 0)
                return true;

            return codes.Any(code => granted.Contains(code.Trim()));
        }

        public static void Apply(TreeView sidebar)
        {
            if (sidebar == null)
                return;

            var granted = GrantedCodes();
            if (granted.Count == 0)
                return;

            RemoveUngranted(sidebar.Nodes, granted);
        }

        private static void RemoveUngranted(TreeNodeCollection nodes, HashSet<string> granted)
        {
            for (int i = nodes.Count - 1; i >= 0; i--)
            {
                TreeNode node = nodes[i];

                RemoveUngranted(node.Nodes, granted);

                string[] codes;
                if (NodeCodes.TryGetValue(node.Name ?? "", out codes))
                {
                    if (!codes.Any(code => granted.Contains(code.Trim())))
                        nodes.RemoveAt(i);

                    continue;
                }

                // A heading left with nothing under it goes too; a leaf with no code stays.
                if (string.Equals(node.Name, "parent", StringComparison.OrdinalIgnoreCase)
                    && node.Nodes.Count == 0)
                {
                    nodes.RemoveAt(i);
                }
            }
        }
    }
}
