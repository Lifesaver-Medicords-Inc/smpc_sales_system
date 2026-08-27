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
            tabContainer.SelectedIndexChanged += (s, e) => RecalculateContentWidth();

            // Phase 4.6 (UI uniformity): set the initial capped/centered width before
            // the form is ever shown - the Resize event alone would leave tabContainer
            // at its Designer-time placeholder size (406,428) for one frame on startup.
            RecalculateContentWidth();
        }

        // Phase 4.6 (UI uniformity): the main content area (tabContainer - everything
        // left of the sidebar and RedBox) caps at 1280px and stays centered on wide/
        // ultrawide monitors. RedBox's own panel (panel5) is left uncapped/full-width
        // on purpose - it's persistent utility chrome, not the "page" being viewed.
        //
        // Individual pages (Quotation.cs etc.) hardcode their own size in their own
        // code (e.g. Quotation.cs: "this.Size = new Size(1386 - 80, 950);") and are
        // never resized to fit whatever tabContainer happens to be - see showForm.
        // First cut of this had newTab.AutoScroll=true try to handle a page wider than
        // its TabPage, but that left the scrollbar unreliable (it wouldn't reliably
        // appear until the whole window was maximized). Moved scrolling to the outer
        // pnl_content_capped instead (see its own AutoScroll=true in the Designer) and
        // made tabContainer never shrink narrower than the ACTIVE tab's own page needs -
        // so instead of a page silently clipping inside a too-small TabPage, the whole
        // work area (tab strip included) becomes exactly as wide as the open page needs
        // and pnl_content_capped scrolls it into view.
        private const int MaxContentWidth = 1280;

        private void pnl_content_capped_Resize(object sender, EventArgs e)
        {
            RecalculateContentWidth();
        }

        private Control GetActiveTabPageControl()
        {
            // Live crash: NullReferenceException on tabContainer.SelectedTab, with
            // tabContainer itself confirmed non-null (found in smpc_inventory_app,
            // same class of code). TabControl.SelectedTab's getter indexes
            // TabPages[SelectedIndex] - the Designer sets SelectedIndex=0 at design
            // time with zero TabPages actually behind it (true at every fresh app
            // launch, before anything's been opened), and querying SelectedTab in that
            // state can throw internally rather than returning null the way an
            // out-of-range SelectedIndex would suggest. Checking TabPages.Count first
            // avoids the property entirely when there's nothing to select anyway.
            if (tabContainer == null || tabContainer.TabPages.Count == 0) return null;
            TabPage selected = tabContainer.SelectedTab;
            return selected != null && selected.Controls.Count > 0 ? selected.Controls[0] : null;
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
                int availableWidth = pnl_content_capped.ClientSize.Width;
                int cappedWidth = Math.Min(MaxContentWidth, availableWidth);

                Control activePage = GetActiveTabPageControl();
                int neededWidth = activePage != null ? Math.Max(cappedWidth, activePage.Width) : cappedWidth;

                tabContainer.Width = neededWidth;
                tabContainer.Height = pnl_content_capped.ClientSize.Height;
                // Centers only when everything actually fits (neededWidth == cappedWidth);
                // once the active page needs more room than's available, flush-left is
                // the only position that makes sense for something you're about to
                // scroll to see the rest of.
                tabContainer.Left = neededWidth <= availableWidth ? (availableWidth - neededWidth) / 2 : 0;
                tabContainer.Top = 0;
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

                TextRenderer.DrawText(
                    e.Graphics,
                    tab.Text,
                    e.Font,
                    new Point(rect.X + 5, rect.Y + 4),
                    Color.Black
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

            //control.Width = this.Width - 235;
            tabContainer.Height = this.Height * 2;
            //control.Height = this.Height;
            // Phase 4.6 (UI uniformity): was "control.Width = this.Width - 550", forcing
            // the page to a computed width no matter what. Tried capping that to
            // tabContainer's own (now 1280-capped) width instead, then tried proportional
            // Control.Scale() on top of that - both made the page itself shrink, which
            // broke because these pages are built with fixed absolute control positions
            // (Quotation.designer.cs has 3 Anchor/Dock declarations in 5000+ lines): a
            // narrower page just clips or overlaps its own controls, it doesn't reflow.
            // Per user direction: don't touch the page's width at all - it keeps its own
            // Designer-authored/hardcoded size. Scrolling to see all of it when it's
            // wider than the available space is now pnl_content_capped's job (see
            // RecalculateContentWidth) rather than this TabPage's own AutoScroll, which
            // didn't reliably trigger.
            newTab.Controls.Add(control);
            tabContainer.TabPages.Add(newTab);
            tabContainer.SelectTab(newTab);
            tabContainer.ItemSize = new Size(200, 28);
            // SelectTab above should already raise SelectedIndexChanged and trigger this,
            // but calling it directly here too is cheap and removes any doubt that a
            // freshly-added tab's own width need is accounted for immediately.
            RecalculateContentWidth();
        }

        private void removeTab(object sender, EventArgs e)
        {
            tabContainer.TabPages.Remove(tabContainer.SelectedTab);
            // Same reasoning as showForm - SelectedIndexChanged should already cover this
            // as selection shifts to another tab (or clears, if none remain), but calling
            // it directly removes any doubt.
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
                lbl_position.Text = CacheData.CurrentUser.position_id;
                lbl_department.Text = CacheData.CurrentUser.department;
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
    }
}
