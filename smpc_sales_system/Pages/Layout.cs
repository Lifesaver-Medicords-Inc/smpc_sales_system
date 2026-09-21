using smpc_app.Services.Helpers;
using smpc_sales_app.Data;
using smpc_sales_app.Pages.Sales;
using smpc_sales_app.Services;
using smpc_sales_system.Pages.Sales;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace smpc_sales_app.Pages
{
    internal partial class Layout : Form
    {

        private int tabCount = 0;
        public Layout()
        {
            InitializeComponent();
            InitializeTabEvents();
            // redBoxControl lives permanently in the right-side panel (not opened via
            // showForm like the sidebar pages), so it needs its own hook here to open a
            // Sales Order/Quotation tab when a document link inside it is clicked.
            redBoxControl.TriggerNewForm += showForm;

            // SO Approvals (spec 3.2, 3.4). Added at runtime rather than in the
            // generated tree so the queue ships without reshuffling the designer file,
            // and placed straight after Sales Order - it is the other half of the same
            // job. Guarded so a later designer entry does not produce two nodes.
            //if (Sidebar.Nodes.Find("Sales Order Approvals", true).Length == 0)
            //{
            //    var salesOrder = Sidebar.Nodes.Find("Sales Order", false);
            //    var approvals = new TreeNode("SO Approvals") { Name = "SO Approvals" };
            //    if (salesOrder.Length > 0)
            //    {
            //        Sidebar.Nodes.Insert(Sidebar.Nodes.IndexOf(salesOrder[0]) + 1, approvals);
            //    }
            //    else
            //    {
            //        Sidebar.Nodes.Add(approvals);
            //    }
            //}

            tabContainer.SelectedIndexChanged += (s, e) => RecalculateContentWidth();

            // Phase 4.6 (UI uniformity): set the initial capped/centered width before
            // the form is ever shown - the Resize event alone would leave tabContainer
            // at its Designer-time placeholder size (406,428) for one frame on startup.
            RecalculateContentWidth();
        }

        // Phase 4.6 (UI uniformity): the main content area (tabContainer - everything
        // left of the sidebar and RedBox) always fills the full available space, no
        // left/right margin. RedBox's own panel (panel5) is untouched either way - it's
        // persistent utility chrome, not the "page" being viewed.
        //
        // The page in the active tab fills its tab as well. On a screen too small for it,
        // it keeps its natural size (its Designer size, or the size its own code gives it,
        // e.g. Quotation.cs: "this.Size = new Size(1386 - 80, 950);"), tabContainer grows
        // past the available space, and pnl_content_capped (AutoScroll, Designer) scrolls
        // the whole work area, tab strip included. Helpers.PageFit does both. Pages used to
        // keep their Designer size and sit top-left with dead space around them on a large
        // monitor (user-reported 2026-09-15). Earlier tries, all reverted: a TabPage-level
        // AutoScroll (its scrollbar would not reliably appear), a 1280px capped/centered
        // column, and proportional Control.Scale() (garbled labels).

        private void pnl_content_capped_Resize(object sender, EventArgs e)
        {
            RecalculateTabSizes();
            RecalculateContentWidth();
        }

        // Guards both pnl_content_capped/tabContainer being null (a Resize event can
        // fire mid-InitializeComponent(), before every field this method touches is
        // necessarily assigned) and, as a last-resort safety net, any other WinForms
        // internal-timing surprise this hasn't anticipated - this is a purely cosmetic
        // sizing pass, so silently skipping one recalculation is far preferable to
        // crashing the app over it.
        private void RecalculateContentWidth()
        {
            if (pnl_content_capped == null || tabContainer == null) return;

            try
            {
                Helpers.PageFit.Fit(pnl_content_capped, tabContainer, RecalculateContentWidth);
            }
            catch (Exception)
            {
                // Cosmetic only - never let a sizing quirk take the app down.
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void InitializeTabEvents()
        {
            tabContainer.DrawMode = TabDrawMode.OwnerDrawFixed;

            tabContainer.DrawItem += (sender, e) =>
            {
                var tab = tabContainer.TabPages[e.Index];
                var rect = tabContainer.GetTabRect(e.Index);

                var textRect = new Rectangle(rect.X + 5, rect.Y + 4, rect.Width - 25, rect.Height - 8);
                
                TextRenderer.DrawText(
                    e.Graphics,
                    tab.Text,
                    e.Font,
                    textRect,
                    Color.Black,
                    TextFormatFlags.EndEllipsis | TextFormatFlags.VerticalCenter | TextFormatFlags.Left
                );

                TextRenderer.DrawText(
                    e.Graphics,
                    "×",
                    e.Font,
                    new Point(rect.Right - 18, rect.Y + 2),
                    Color.Red
                );
            };

            tabContainer.MouseDown += (sender, e) =>
            {
                for (int i = 0; i < tabContainer.TabPages.Count; i++)
                {
                    var rect = tabContainer.GetTabRect(i);
                    var closeArea = new Rectangle(rect.Right - 15, rect.Top + 4, 14, 14);

                    if (closeArea.Contains(e.Location))
                    {
                        tabContainer.TabPages.RemoveAt(i);
                        break;
                    }
                }
            };
        }


        private void showForm(string tabTitle, Control control)
        {
            tabCount++;

            TabPage newTab = new TabPage(tabTitle);

            //closeButton.Location = new Point(newTab.Width, 10); // Adjust position as needed
            if (control is Opportunities)
            {
                Opportunities OpportunitiesControl = (Opportunities)control;
                OpportunitiesControl.TriggerNewForm += showForm;
            }
            // §5.25 REMARKS reference link (Orders.cs): needed here, not just on
            // redBoxControl above, since an Orders tab can be opened either from the
            // sidebar (Sidebar_NodeMouseClick -> showForm directly) or from RedBox's own
            // link - showForm is the one place both paths pass through.
            else if (control is Orders)
            {
                Orders OrdersControl = (Orders)control;
                OrdersControl.TriggerNewForm += showForm;
            }
            // The approval queue opens the order it is pointing at in its own tab,
            // the same way RedBox and the SO's remarks link do.
            else if (control is SOApprovals)
            {
                SOApprovals approvalsControl = (SOApprovals)control;
                approvalsControl.TriggerNewForm += showForm;
            }

            // The page's size is Helpers.PageFit's job: it fills the tab, is never made
            // smaller than the size it was built at (a narrower page clips or overlaps its
            // own fixed-position controls - it doesn't reflow), and follows the page when its
            // own code resizes it (Quotation: 950 high for Quick Quote, 2354 for Project). A
            // page that swaps itself for another inside this tab (Quotation -> Sales Order
            // and back) is re-fitted as the new one arrives.
            Helpers.PageFit.Track(control, RecalculateContentWidth);
            newTab.ControlAdded += (s, e) => RecalculateContentWidth();
            newTab.ControlRemoved += (s, e) => RecalculateContentWidth();

            newTab.Controls.Add(control);
            tabContainer.TabPages.Add(newTab);
            tabContainer.SelectTab(newTab);
            tabContainer.ItemSize = new Size(200, 28);
            // SelectTab above should already raise SelectedIndexChanged and trigger this,
            // but calling it directly here too is cheap and removes any doubt that a
            // freshly-added tab's own width need is accounted for immediately.
            RecalculateTabSizes();
            RecalculateContentWidth();
        }

        private void removeTab(object sender, EventArgs e)
        {
            tabContainer.TabPages.Remove(tabContainer.SelectedTab);
            // Same reasoning as showForm - SelectedIndexChanged should already cover this
            // as selection shifts to another tab (or clears, if none remain), but calling
            // it directly removes any doubt.
            RecalculateTabSizes();
            RecalculateContentWidth();
        }
        private void Sidebar_NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            
            if (e.Node.Name.Contains("Dashboard"))
            {
                Helpers.ShowDialogMessage("error", "This module is not available at the moment!");
                return;
            }
            if (!e.Node.Name.Contains("parent"))
            {
                RoutesServices route = new RoutesServices(e.Node.Name);
                showForm(route.GetTitle(), route.GetForm());
            }
        }
        private async void Layout_Load(object sender, EventArgs e)
        {
            Login login = new Login();
            if (DialogResult.OK == login.ShowDialog())
            {
                lbl_name.Text = CacheData.CurrentUser.first_name + " " + CacheData.CurrentUser.last_name;
                // The position's NAME, as inventory and dispatching show it - the id
                // alone read as "Position: 1".
                lbl_position.Text = CacheData.CurrentUser.position?.name ?? CacheData.CurrentUser.position_id;
                lbl_department.Text = CacheData.CurrentUser.department;

                // No BPI entry for positions without access (spec 3.2). The page
                // gates itself too, for the modals that open it.
                if (!smpc_inventory_app.Model.BpiAccess.CanOpen(CacheData.CurrentUser))
                    Sidebar.Nodes.RemoveByKey("Business Partners");

                // Everything else the position has no access to goes the same way, from the
                // grants Admin's Access Control screen maintains. See NavigationAccess.
                smpc_sales_system.Models.NavigationAccess.Apply(Sidebar);

                this.Enabled = true;

                // Only safe to call now that CacheData.SessionToken is actually set - see
                // the comment on RedBox.RefreshData() for why this can't run any earlier.
                await redBoxControl.RefreshData();
            }
            else
            {
                Application.Exit();
            }
        }
        private void Sidebar_AfterSelect(object sender, TreeViewEventArgs e)
        {

        }

        private void RecalculateTabSizes()
        {
            if (tabContainer == null || tabContainer.TabPages.Count == 0) return;

            try
            {
                int availableWidth = pnl_content_capped != null
                    ? pnl_content_capped.ClientSize.Width
                    : tabContainer.Width;

                int count = tabContainer.TabPages.Count;
                const int minTabWidth = 90;
                const int maxTabWidth = 200;
                const int tabHeight = 28;

                int computedWidth = availableWidth / count;
                computedWidth = Math.Max(minTabWidth, Math.Min(maxTabWidth, computedWidth));

                tabContainer.ItemSize = new Size(computedWidth, tabHeight);

                // If even minTabWidth-per-tab doesn't fit everyone on one row, let
                // the strip wrap to a second row instead of clipping/scrolling.
                tabContainer.Multiline = (long)computedWidth * count > availableWidth;
            }
            catch (Exception)
            {
                // Cosmetic only - same reasoning as RecalculateContentWidth.
            }
        }
    }
}
