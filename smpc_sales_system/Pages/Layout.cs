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

            // Phase 4.6 (UI uniformity): set the initial capped/centered width before
            // the form is ever shown - the Resize event alone would leave tabContainer
            // at its Designer-time placeholder size (406,428) for one frame on startup.
            RecalculateContentWidth();
        }

        // Phase 4.6 (UI uniformity): the main content area (tabContainer - everything
        // left of the sidebar and RedBox) caps at 1280px and stays centered on wide/
        // ultrawide monitors, shrinking to fit below that. RedBox's own panel (panel5)
        // is left uncapped/full-width on purpose - it's persistent utility chrome, not
        // the "page" being viewed.
        private const int MaxContentWidth = 1280;

        private void pnl_content_capped_Resize(object sender, EventArgs e)
        {
            RecalculateContentWidth();
        }

        private void RecalculateContentWidth()
        {
            int cappedWidth = Math.Min(MaxContentWidth, pnl_content_capped.ClientSize.Width);
            tabContainer.Width = cappedWidth;
            tabContainer.Height = pnl_content_capped.ClientSize.Height;
            tabContainer.Left = (pnl_content_capped.ClientSize.Width - cappedWidth) / 2;
            tabContainer.Top = 0;
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
            // Designer-authored size, and newTab.AutoScroll (below, already existed
            // before this session) shows a scrollbar to pan across it whenever
            // tabContainer ends up narrower than the page actually needs. tabContainer
            // itself still caps/centers at 1280px on wide monitors (RecalculateContentWidth) -
            // this only changes whether the PAGE inside it gets force-resized too.
            newTab.Controls.Add(control);
            newTab.AutoScroll = true;
            tabContainer.TabPages.Add(newTab);
            tabContainer.SelectTab(newTab);
            tabContainer.ItemSize = new Size(200, 28);
        }

        private void removeTab(object sender, EventArgs e)
        {
            tabContainer.TabPages.Remove(tabContainer.SelectedTab);
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
