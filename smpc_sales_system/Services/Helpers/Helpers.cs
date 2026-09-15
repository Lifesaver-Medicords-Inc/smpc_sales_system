using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data; 
using System.Data.SqlTypes;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Management;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace smpc_app.Services.Helpers
{
    internal static class Helpers
    {
        // Simple semi-transparent overlay with a message, dropped on top of whatever control
        // is passed in (e.g. a DataGridView while it's fetching data). Ported from the same
        // pattern already used in smpc_inventory_app's Helpers.Loading for consistency.
        // The one standard loading screen (spec 2.1): "Loading... Please wait", it
        // covers the WHOLE PAGE rather than one section, and it clears only once
        // every part of the page has finished loading.
        //
        // Rewritten 2026-09-12. The previous version broke all three of those rules
        // and the bugs were not cosmetic:
        //
        //   - It overlaid whatever control the caller passed - in practice a single
        //     DataGridView - so the grid was covered while the toolbar, the header
        //     fields and the action buttons stayed live. A user could press Save on
        //     a form whose data had not arrived yet.
        //
        //   - One static overlay for the entire app, first-wins: a second load
        //     starting while one was up returned silently, and then the FIRST
        //     HideLoading tore the overlay down while the other load was still
        //     running. A page with two or three fetches cleared early, every time.
        //
        //   - HideLoading disposed and nulled the static field while removing the
        //     panel from whichever control it was handed. Hand it a different
        //     control than Show got - easy, since callers pass grids - and the
        //     overlay is orphaned on screen with nothing left pointing at it.
        //
        // Now: the host is resolved UP to the page that contains it, input to the
        // page is held back for the duration, and shows are REF-COUNTED per
        // page, so the screen lifts on the last completion rather than the first.
        //
        // Callers need no change - every existing call site passes some control on
        // the page, which is exactly what this resolves from.
        public static class Loading
        {
            // Spec 2.1's exact wording. Callers may pass their own message, but the
            // default is the standard one and should stay that way.
            public const string StandardMessage = "Loading... Please wait";

            private class PageOverlay
            {
                public Panel Panel;
                public int Depth;
            }

            // Clicks, keys and wheel turns aimed at anything on a covered page are swallowed
            // here, before the control they were meant for sees them. This used to be done by
            // disabling the page's controls and switching them all back on when the screen
            // cleared - which restored the state from when the screen went UP, so a save that
            // settles the form read-only (spec 2.1) while the screen is up would have had its
            // locked panels switched straight back on. Holding the input back instead leaves
            // every control's Enabled state to the page's own code. The cover itself still takes
            // the mouse: there is nothing on it to press, and a wheel turn over it scrolls the page.
            private class InputBlocker : IMessageFilter
            {
                private const int FirstKeyMessage = 0x0100;   // WM_KEYDOWN
                private const int LastKeyMessage = 0x0109;    // WM_UNICHAR
                private const int FirstMouseMessage = 0x0201; // WM_LBUTTONDOWN - movement passes
                private const int LastMouseMessage = 0x020E;  // WM_MOUSEHWHEEL

                public bool PreFilterMessage(ref Message m)
                {
                    if (Overlays.Count == 0) return false;

                    bool input = (m.Msg >= FirstKeyMessage && m.Msg <= LastKeyMessage)
                        || (m.Msg >= FirstMouseMessage && m.Msg <= LastMouseMessage);
                    if (!input) return false;

                    Control target = Control.FromChildHandle(m.HWnd);
                    for (Control c = target; c != null; c = c.Parent)
                    {
                        PageOverlay record;
                        if (Overlays.TryGetValue(c, out record))
                            return target != record.Panel;
                    }

                    return false;
                }
            }

            private static bool inputBlockerInstalled;

            // Keyed per page, so two pages loading at once do not cancel each other.
            private static readonly Dictionary<Control, PageOverlay> Overlays =
                new Dictionary<Control, PageOverlay>();

            // Resolves the PAGE a control belongs to - the UserControl sitting in the
            // tab - and deliberately stops short of the Form.
            //
            // The first version of this accepted `c is UserControl || c is Form` while
            // walking all the way up, overwriting as it went, so it always finished on
            // the Form: the loading screen covered the entire window, sidebar, red box
            // and tab strip included, instead of the tab's content.
            //
            // Outermost UserControl rather than innermost, because pages are built from
            // nested UserControls (ItemSetUC inside a quotation, BpiBranchUC inside a
            // partner) and covering only the inner one would leave most of the page
            // live. The Form is used only when there is no UserControl above the
            // control at all, which is the modal-dialog case - there the dialog IS the
            // page.
            private static Control ResolvePage(Control control)
            {
                if (control == null) return null;

                Control page = null;
                for (Control c = control; c != null; c = c.Parent)
                {
                    if (c is UserControl) page = c;
                }

                // A dialog, or a control not parented yet - a load kicked off from a
                // constructor, where walking up finds nothing.
                return page ?? control.FindForm() ?? control;
            }

            public static void ShowLoading(Control host, string message = StandardMessage)
            {
                Control page = ResolvePage(host);
                if (page == null || page.IsDisposed) return;

                PageOverlay existing;
                if (Overlays.TryGetValue(page, out existing))
                {
                    // Another fetch on the same page. Count it and leave the screen up.
                    existing.Depth++;
                    return;
                }

                // Deliberately NOT Dock = Fill. A Fill-docked child is added at the end
                // of the Controls collection, which makes it dock FIRST and lays every
                // other docked sibling out in what is left - nothing. The siblings then
                // reflow again when it is removed, and any page whose layout is not
                // perfectly reversible comes back subtly rearranged.
                //
                // Explicit bounds keep the overlay out of dock layout entirely.
                // ClientRectangle rather than DisplayRectangle on purpose - on a
                // scrollable page DisplayRectangle can be larger than the visible area,
                // and a child that big extends the scroll region, which is its own
                // phantom-space bug.
                //
                // No anchors either. Sales Quotation starts loading from its Load event,
                // before its tab has given it its final size, and it scrolls (AutoScroll):
                // an anchored overlay stayed at the size it started with - a grey block over
                // one corner, the message out of sight (user-reported 2026-09-15). The overlay
                // is instead put back over the visible area whenever the page lays out,
                // resizes or scrolls, for as long as it is up (keepOverPage, below).
                var overlay = new Panel
                {
                    // Darker than a plain grey wash so the white box stands out against it.
                    BackColor = Color.FromArgb(150, 40, 40, 40),
                    Name = "pnl_page_loading",
                    Bounds = page.ClientRectangle,
                };

                // Everything is painted by the cover itself, in one pass into an off-screen buffer,
                // and redrawn in full whenever it resizes. The box used to be a child panel of its
                // own and the cover was drawn straight to the screen in two passes - the page behind
                // it, then the grey - with Windows filling the box in as a separate, later step. At
                // the start of every load that showed as a flicker: grey with a hole in it, then the
                // box (user-reported 2026-09-15). Cover, box, ring and message now reach the screen
                // in the same frame.
                typeof(Control).GetMethod("SetStyle", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                    ?.Invoke(overlay, new object[] { ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.ResizeRedraw, true });

                // A plain box in the middle of the covered page (the page stays covered, spec 2.1):
                // a turning ring above the standard message, moved by a UI-thread timer so it keeps
                // turning while the page awaits the API and stops only if the UI thread itself is
                // busy. Square corners like the rest of the app; the message in the default C# font,
                // bold as a modal's header is (1.4); neutral greys, since red is the attention colour
                // and there is no blue convention. The spec names only the message and the full-page
                // cover, so the ring and the box are a provisional standard, chosen 2026-09-15 - the
                // sliding bar, rounded corners and Segoe UI first tried with them were taken out the
                // same day at the user's request.
                var messageFont = new Font(Control.DefaultFont, FontStyle.Bold);
                var boxSize = new Size(Math.Max(240, TextRenderer.MeasureText(message, messageFont).Width + 64), 108);
                var ink = Color.FromArgb(70, 70, 70);
                var faint = Color.FromArgb(225, 225, 225);
                int angle = 0;

                // Worked out at paint time, so the box stays in the middle as the cover follows the page.
                Func<Rectangle> boxBounds = () => new Rectangle(
                    Math.Max(0, (overlay.ClientSize.Width - boxSize.Width) / 2),
                    Math.Max(0, (overlay.ClientSize.Height - boxSize.Height) / 2),
                    boxSize.Width, boxSize.Height);
                Func<Rectangle> ringBounds = () =>
                {
                    var b = boxBounds();
                    return new Rectangle(b.X + b.Width / 2 - 20, b.Y + 18, 40, 40);
                };

                overlay.Paint += (s, e) =>
                {
                    var g = e.Graphics;

                    var b = boxBounds();
                    using (var fill = new SolidBrush(Color.White))
                        g.FillRectangle(fill, b);

                    // Anti-aliased for the ring only; the box edges are straight.
                    g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                    var ring = ringBounds();
                    using (var track = new Pen(faint, 5))
                    using (var arc = new Pen(ink, 5))
                    {
                        arc.StartCap = System.Drawing.Drawing2D.LineCap.Round;
                        arc.EndCap = System.Drawing.Drawing2D.LineCap.Round;
                        g.DrawEllipse(track, ring);
                        g.DrawArc(arc, ring, angle, 100);
                    }

                    TextRenderer.DrawText(g, message, messageFont, new Rectangle(b.X, b.Y + 68, b.Width, 24), ink,
                        TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                };

                var spin = new System.Windows.Forms.Timer { Interval = 40 };
                spin.Tick += (s, e) =>
                {
                    angle = (angle + 12) % 360;
                    // Only the ring changes from one frame to the next.
                    var ring = ringBounds();
                    ring.Inflate(6, 6);
                    overlay.Invalidate(ring);
                };
                overlay.Disposed += (s, e) =>
                {
                    spin.Dispose();
                    messageFont.Dispose();
                };
                spin.Start();

                var record = new PageOverlay { Panel = overlay, Depth = 1 };

                // Suspended around the add so the overlay never triggers a layout pass
                // of its own over the page's real controls.
                page.SuspendLayout();
                page.Controls.Add(overlay);
                overlay.BringToFront();
                page.ResumeLayout(false);

                // Painted now rather than whenever the message loop next gets to it. The caller
                // usually carries straight on with work of its own - binding, the first request -
                // and until the cover has painted the page shows through where it should be.
                overlay.Update();

                // Input to the page is held back until the screen clears (InputBlocker). A
                // Cancel pressed during a long save would otherwise throw the save away.
                if (!inputBlockerInstalled)
                {
                    Application.AddMessageFilter(new InputBlocker());
                    inputBlockerInstalled = true;
                }

                // Bounds are written only when they differ, and the overlay is brought forward
                // only when something has been put above it, so the Layout this answers never
                // sets itself off again. Unhooked when HideLoading disposes the overlay.
                EventHandler keepOverPage = (s, e) =>
                {
                    if (overlay.IsDisposed || page.IsDisposed || overlay.Parent != page) return;
                    if (overlay.Bounds != page.ClientRectangle) overlay.Bounds = page.ClientRectangle;
                    if (page.Controls.GetChildIndex(overlay) != 0) overlay.BringToFront();
                };
                LayoutEventHandler keepOnLayout = (s, e) => keepOverPage(s, e);
                ScrollEventHandler keepOnScroll = (s, e) => keepOverPage(s, e);
                var scrollable = page as ScrollableControl;

                page.Layout += keepOnLayout;
                page.ClientSizeChanged += keepOverPage;
                if (scrollable != null) scrollable.Scroll += keepOnScroll;
                // The mouse wheel scrolls a page without raising Scroll, and every kind of scroll
                // carries the overlay along with the content - so it also snaps back when moved.
                overlay.LocationChanged += keepOverPage;
                overlay.Disposed += (s, e) =>
                {
                    page.Layout -= keepOnLayout;
                    page.ClientSizeChanged -= keepOverPage;
                    if (scrollable != null) scrollable.Scroll -= keepOnScroll;
                };

                Overlays[page] = record;
            }

            public static void HideLoading(Control host)
            {
                Control page = ResolvePage(host);
                if (page == null) return;

                PageOverlay record;
                if (!Overlays.TryGetValue(page, out record)) return;

                // Only the last outstanding load clears the screen (spec 2.1).
                record.Depth--;
                if (record.Depth > 0) return;

                Overlays.Remove(page);

                if (!page.IsDisposed)
                {
                    page.SuspendLayout();
                    page.Controls.Remove(record.Panel);
                    page.ResumeLayout(true);
                }

                record.Panel.Dispose();
            }

            // Clears a page's loading screen no matter how many shows are outstanding.
            // For a failure path that has to bail out of several fetches at once - a
            // dropped connection - where matching every Show with a Hide is not
            // practical. Never call it to "make sure" the screen is gone: that is the
            // early-clear bug this class was rewritten to remove.
            public static void ForceHide(Control host)
            {
                Control page = ResolvePage(host);
                if (page == null) return;

                PageOverlay record;
                if (!Overlays.TryGetValue(page, out record)) return;

                record.Depth = 1;
                HideLoading(page);
            }
        }

        // Fits the page in the active tab to the space the window gives it (spec 1.3: responsive,
        // never by squeezing the centre pane). A page is never made smaller than its natural size -
        // its Designer size, or whatever size its own code later gives it - but it is stretched to
        // fill the tab whenever the window has more room than that. Nearly every page is built from
        // docked bands (header Top, body Fill, footer Bottom), so the bands, grids, inner tab
        // controls and right-aligned toolbar buttons follow the new size; fields placed at fixed
        // positions inside a band stay where they are. A page bigger than the window keeps its size:
        // the tab control grows past the window and the panel it sits in scrolls.
        //
        // Before this only the tab control was sized. The page kept its Designer size and sat in
        // the top-left corner of its tab with dead space to the right and below on a large monitor
        // (user-reported 2026-09-15). Proportional Control.Scale() was tried in August 2026 and
        // garbled the pages' AutoSize labels, so pages are stretched, never scaled. Copy-identical
        // in the four apps that open pages in tabs (sales, inventory, engineering, accounting).
        public static class PageFit
        {
            private static readonly Dictionary<Control, Size> NaturalSizes = new Dictionary<Control, Size>();

            // Set while Fit resizes a page, so the page's SizeChanged can tell that apart from the
            // page resizing itself.
            private static bool fitting;

            // Starts watching a page. Safe to call more than once for the same page.
            public static void Track(Control page, Action refit)
            {
                if (page == null || page.IsDisposed || NaturalSizes.ContainsKey(page)) return;

                NaturalSizes[page] = page.Size;

                // Only Fit sizes a tracked page. Anchored to the right or bottom edge it would also
                // stretch on its own, that stretched size would be taken for its natural size, and it
                // could never shrink again; AutoSize would fight the sizes Fit gives it.
                page.Anchor = AnchorStyles.Top | AnchorStyles.Left;
                page.AutoSize = false;

                // A page may resize itself once it is open - Quotation changes its height when it
                // switches between Quick Quote and Project, Orders sets its own width - and the size it
                // chooses becomes the one it is never made smaller than.
                page.SizeChanged += (s, e) =>
                {
                    if (fitting || page.IsDisposed) return;
                    NaturalSizes[page] = page.Size;
                    refit?.Invoke();
                };
                page.Disposed += (s, e) => NaturalSizes.Remove(page);
            }

            // The page showing in the selected tab. Pages replace themselves inside their own tab by
            // adding the next page and hiding or disposing themselves (Opportunities -> Quotation,
            // Quotation -> Sales Order and back), so the one on screen is the last one added.
            public static Control ActivePage(TabControl tabs)
            {
                // SelectedTab can throw while the Designer's SelectedIndex points at a tab that does
                // not exist yet - every fresh launch, before anything is opened - so check first.
                if (tabs == null || tabs.TabPages.Count == 0) return null;
                TabPage selected = tabs.SelectedTab;
                if (selected == null) return null;

                for (int i = selected.Controls.Count - 1; i >= 0; i--)
                {
                    if (!selected.Controls[i].IsDisposed) return selected.Controls[i];
                }
                return null;
            }

            // Sizes the tab control, and the page in its selected tab, to the scrolling panel (host)
            // they sit in. refit is what the host calls to run this again.
            public static void Fit(ScrollableControl host, TabControl tabs, Action refit)
            {
                if (host == null || tabs == null) return;

                Size available = host.ClientSize;
                Control page = ActivePage(tabs);

                // The tab control goes at the top-left of the host's content, not of its visible
                // area. Bounds are client coordinates, which move as the host scrolls: placed at
                // (0, 0) while the host was scrolled down, the tab control landed that far below the
                // start of the content, and when the page then shrank (Sales Quotation, Project back
                // to Quick Quote) the scroll range collapsed and left that distance as empty space
                // above the tab (user-reported 2026-09-15). AutoScrollPosition is where the content's
                // origin currently is.
                Point origin = host.AutoScrollPosition;

                // Nothing open, or a page that docks itself and so looks after its own size.
                if (page == null || page.Dock != DockStyle.None)
                {
                    tabs.Bounds = new Rectangle(origin.X, origin.Y, available.Width, available.Height);
                    return;
                }

                Track(page, refit);
                Size natural;
                if (!NaturalSizes.TryGetValue(page, out natural)) natural = page.Size;

                Padding padding = page.Parent is TabPage tabPage ? tabPage.Padding : Padding.Empty;

                // The tab strip and border around a tab's display area, plus the tab page's padding.
                int chromeWidth = tabs.Width - tabs.DisplayRectangle.Width + padding.Horizontal;
                int chromeHeight = tabs.Height - tabs.DisplayRectangle.Height + padding.Vertical;

                tabs.Bounds = new Rectangle(origin.X, origin.Y,
                    Math.Max(available.Width, natural.Width + chromeWidth),
                    Math.Max(available.Height, natural.Height + chromeHeight));

                Rectangle display = tabs.DisplayRectangle;
                fitting = true;
                try
                {
                    page.Size = new Size(
                        Math.Max(natural.Width, display.Width - padding.Horizontal),
                        Math.Max(natural.Height, display.Height - padding.Vertical));
                }
                finally
                {
                    fitting = false;
                }
            }
        }

        // Recursively walks every control under `root` (Panels, GroupBoxes, TabPages,
        // ToolStrips, etc.) and enables/disables every clickable Button and ToolStripButton
        // it finds. Used to lock the whole form's buttons while data is still being fetched
        // from the server, so a slow response can't be raced by a user click (e.g. hitting
        // Save before the record has finished loading).
        public static void SetButtonsEnabled(Control root, bool enabled)
        {
            if (root == null) return;

            foreach (Control ctrl in root.Controls)
            {
                if (ctrl is Button button)
                {
                    button.Enabled = enabled;
                }
                else if (ctrl is ToolStrip toolStrip)
                {
                    foreach (ToolStripItem item in toolStrip.Items)
                    {
                        if (item is ToolStripButton || item is ToolStripSplitButton || item is ToolStripDropDownButton)
                        {
                            item.Enabled = enabled;
                        }
                    }
                }

                if (ctrl.Controls.Count > 0)
                {
                    SetButtonsEnabled(ctrl, enabled);
                }
            }
        }

        public static void ResetControls(Panel pnl)
        {
            foreach (Control control in pnl.Controls)
            {
                // Check if the control is a TextBox
                if (control is TextBox textBox)
                {
                    // Reset the TextBox's text
                    textBox.Text = "";
                }
            }
        }

        public static void ResetControls(Panel[] parents)
        {
            foreach (Panel pnl in parents)
            {
                foreach (Control control in pnl.Controls)
                {
                    if (control is TextBox textBox)
                    {
                        // Check for money_format tag
                        if (textBox.Tag?.ToString() == "money_format")
                        {
                            string input = textBox.Text?.Trim() ?? "";

                            if (!string.IsNullOrEmpty(input))
                            {
                                // Remove everything except digits, dot, comma, and minus
                                string cleaned = Regex.Replace(input, @"[^0-9\.,\-]", "");

                                // Replace comma with dot if comma used as decimal
                                if (cleaned.Count(c => c == ',') == 1 && cleaned.Count(c => c == '.') == 0)
                                    cleaned = cleaned.Replace(',', '.');

                                // Attempt parsing
                                bool isValid =
                                    decimal.TryParse(cleaned, NumberStyles.Any, CultureInfo.InvariantCulture, out _) ||
                                    decimal.TryParse(cleaned, NumberStyles.Any, CultureInfo.CurrentCulture, out _);

                                if (!isValid)
                                {
                                    MessageBox.Show(
                                        $"Invalid money format in \"{textBox.Name}\".\n" +
                                        $"Value: \"{textBox.Text}\" could not be parsed.\nPlease enter a valid number.",
                                        "Invalid Input",
                                        MessageBoxButtons.OK,
                                        MessageBoxIcon.Warning
                                    );
                                    textBox.Focus();
                                    return; // stop processing
                                }
                            }
                        }

                        // Clear textbox
                        textBox.Clear();
                    }
                    else if (control is ComboBox comboBox)
                    {
                        comboBox.SelectedIndex = -1;
                    }
                    else if (control is CheckBox checkBox)
                    {
                        checkBox.Checked = false;
                    }
                    else if (control is RadioButton radioButton)
                    {
                        radioButton.Checked = false;
                    }
                    else if (control is DateTimePicker dateTimePicker)
                    {
                        dateTimePicker.Value = DateTime.Now;
                    }
                    else if (control is NumericUpDown numericUpDown)
                    {
                        numericUpDown.Value = numericUpDown.Minimum;
                    }
                    else if (control is PictureBox pictureBox)
                    {
                        pictureBox.Image = null;
                    }
                }
            }
        }


        public static void ReadOnlyControls(Panel[] pnl_list)
        {
            foreach (Panel pnl in pnl_list)
            {
                foreach (Control ctrl in pnl.Controls)
                {
                    if (ctrl is TextBox)
                    {
                        ((TextBox)ctrl).ReadOnly = true;
                    }
                    //if (ctrl is Button)
                    //{
                    //    ((Button)ctrl).Enabled = false;
                    //}
                    if (ctrl is ComboBox)
                    {
                        // Bugs #021/#022 (Trello, "PURPOSE/SHIP TYPE dropdown text option
                        // is editable"): locking a combo switched its DropDownStyle to
                        // Simple, which is the one style whose text portion is a free-type
                        // textbox - Enabled=false should mask that, but there's no reason a
                        // "read-only" combo needs a different, typing-capable style at all.
                        // Just disable it and leave DropDownStyle exactly as the form
                        // designed it (DropDownList everywhere it's used).
                        ((ComboBox)ctrl).Enabled = false;
                    }
                    if (ctrl is DateTimePicker)
                    {
                        ((DateTimePicker)ctrl).Enabled = false;
                    }
                }
            }
        }

        // Bug #030 (Trello, "CODE should be read-only"): this unconditionally
        // unlocked every TextBox in the panel, including the system-generated
        // id/document/version fields - Quotation.cs's own Select-Customer path
        // already knew this and manually relocked txt_document_no/txt_version_no/
        // txt_sub_version_no right after calling this, but every OTHER call site
        // (btn_edit_Click's SetFormEditMode, btn_new_version_Click, the
        // fetch-from-elsewhere reload path) didn't, so Edit/New Version left the
        // document's own code directly typable. CLAUDE.md's own convention: "DOC
        // NO. and DOC DATE are header fields... system-set and read-only" -
        // universal across every document form, so this exclusion belongs in the
        // shared helper rather than patched at each call site.
        private static readonly string[] SystemGeneratedFieldNames =
            { "txt_id", "txt_document_no", "txt_version_no", "txt_sub_version_no" };

        public static void ResetReadOnlyControls(Panel[] pnl_list)
        {
            foreach (Panel pnl in pnl_list)
            {
                foreach (Control ctrl in pnl.Controls)
                {
                    if (ctrl is TextBox && SystemGeneratedFieldNames.Contains(ctrl.Name))
                    {
                        continue;
                    }

                    if (ctrl is TextBox)
                    {
                        ((TextBox)ctrl).ReadOnly = false;
                    }
                    //if (ctrl is Button)
                    //{
                    //    ((Button)ctrl).Enabled = true;
                    //}
                    if (ctrl is ComboBox)
                    {
                        ((ComboBox)ctrl).DropDownStyle = ComboBoxStyle.DropDownList;
                        ((ComboBox)ctrl).Enabled = true;
                    }
                    if (ctrl is DateTimePicker)
                    {
                        ((DateTimePicker)ctrl).Enabled = true;
                    }
                }
            }
        }

        // ReadOnlyControls/ResetReadOnlyControls only walk one level of a panel's direct
        // children and don't touch CheckBox or DataGridView at all - fine for the flat quick
        // quote header panels, but Project Quotation's controls (ItemSetUC) are nested inside
        // several layers of panels and include checkboxes (chk_wiring) and grids
        // (dgv_project_items, dgv_wiring, dgv_final) that were never being locked, so
        // everything stayed editable even before clicking Edit. This walks the whole control
        // tree under `root` and locks/unlocks every field type it finds. DataGridViews are
        // set ReadOnly rather than Disabled so they can still be scrolled/viewed while locked.
        public static void SetControlsEditable(Control root, bool editable)
        {
            foreach (Control ctrl in root.Controls)
            {
                switch (ctrl)
                {
                    case TextBox textBox:
                        textBox.ReadOnly = !editable;
                        break;
                    case ComboBox comboBox:
                        comboBox.Enabled = editable;
                        break;
                    case CheckBox checkBox:
                        checkBox.Enabled = editable;
                        break;
                    case DateTimePicker dateTimePicker:
                        dateTimePicker.Enabled = editable;
                        break;
                    case NumericUpDown numericUpDown:
                        numericUpDown.ReadOnly = !editable;
                        break;
                    case DataGridView dataGridView:
                        dataGridView.ReadOnly = !editable;
                        break;
                }

                // Recurse into anything that can itself contain fields (panels, group boxes,
                // split container panels, nested user controls, etc.) so nothing buried a
                // couple of levels deep gets skipped.
                if (ctrl.Controls.Count > 0)
                    SetControlsEditable(ctrl, editable);
            }
        }



        public static Dictionary<string, dynamic> GetControlsValues(Panel pnl)
        {
            Dictionary<string, dynamic> values = new Dictionary<string, dynamic>();
            foreach (Control control in pnl.Controls)
            {
                // Check if the control is a TextBox
                if (control is TextBox textBox)
                {
                    string key = textBox.Name.Replace("txt_", "");
                    dynamic val = null; 

                    if (textBox.Tag != null && textBox.Tag.ToString() == "money_format")
                    {
                        // Only strips commas, not the "₱" MoneyFormat/BindControls puts on
                        // these fields - so any field that had already been displayed with
                        // its currency symbol (i.e. any money field that was simply left
                        // untouched since the record was loaded) failed to parse here and
                        // showed "Invalid money format" even though the value was fine.
                        bool isParsed = decimal.TryParse(GetCleanedPriceValue(textBox.Text), out decimal tempVal);
                        if (isParsed)
                        {
                            val = tempVal;
                        }
                        else
                        {
                            MessageBox.Show("Invalid money format. Please enter a valid number.");
                            val = 0;
                        }
                    }
                    else
                    {
                        val = textBox.Text.ToString();
                    }
                    values[key] = val;
                }

            // Check if the control is a Combobox
            if (control is ComboBox comboBox)
                {
                    string key = comboBox.Name.Replace("cmb_", "");
                    string val = "";

                    if (comboBox.Tag == "DYNAMIC")
                    {
                        key = key + "_id";
                        val = comboBox.SelectedValue.ToString();
                    }
                    else if (string.IsNullOrEmpty(comboBox.Text.ToString()))
                    {
                        val = "";
                    }
                    else
                    {
                        val = comboBox.Text.ToString();
                    }

                    if (comboBox.Tag == "DYNAMIC")
                    {
                        values.Add(key, int.Parse(val));
                    }
                    else
                    {
                        values.Add(key, val);
                    }
                    
                }

                // Check if the control is a Checkbox
                if (control is CheckBox checkbox)
                {
                    string key = checkbox.Name.Replace("chk_", "");
                    string val = String.Format("{0}", checkbox.Checked ? 1 : 0);
                    values.Add(key, val);
                }

                // Check if the control is a DATETIME PICKER
                if (control is DateTimePicker dateTimePicker)
                {
                    string key = dateTimePicker.Name.Replace("dtp_", "");
                    string val = String.Format("{0:yyyy-MM-dd}", dateTimePicker.Value);
                    values.Add(key, val);
                }

                // Check if the control is a NUMERIC
                if (control is NumericUpDown numericUpDown)
                {
                    string key = numericUpDown.Name.Replace("txt_", "");
                    string val = String.Format("{0}", numericUpDown.Value);
                    values.Add(key, val);
                }
            }

            return values;
        }

        public static Dictionary<string, dynamic> GetControlsValues(Panel[] pnl1)
        { 
            Dictionary<string, dynamic> values = new Dictionary<string, dynamic>();

            foreach (Panel pnl in pnl1)
            {
                foreach (Control control in pnl.Controls)
                {
                    // Check if the control is a TextBox
                    // Check if the control is a TextBox
                    if (control is TextBox textBox)
                    {
                        string key = textBox.Name.Replace("txt_", "");
                        dynamic val = null;

                        // Handle money formatting
                        if (textBox.Tag != null && textBox.Tag.ToString() == "money_format")
                        {
                            // Only stripped commas, not the "₱" that MoneyFormat/BindControls
                            // puts on these fields - so a money field that had simply been
                            // displayed with its currency symbol (i.e. left untouched since
                            // the record loaded) failed to parse here and showed "Invalid
                            // money format" on save even though nothing was actually wrong
                            // with the value.
                            if (decimal.TryParse(GetCleanedPriceValue(textBox.Text), out decimal tempVal))
                            {
                                val = tempVal;
                            }
                            else
                            {
                                MessageBox.Show("Invalid money format. Please enter a valid number.");
                                val = 0m;
                            }
                        }
                        // Handle _id conversion
                        else if (key.EndsWith("_id"))
                        {
                            // A blank id field (e.g. content_id on a tab whose content row
                            // hasn't been created/saved yet) is a normal "doesn't exist yet"
                            // state, not a user input mistake - it used to trip the same
                            // "Invalid ID format" warning as actually-malformed text, and it
                            // fired on tab switch/content-changed events, not just Save, since
                            // this runs any time this panel's values get read. Only warn when
                            // there's text that genuinely isn't a number.
                            if (string.IsNullOrWhiteSpace(textBox.Text))
                            {
                                val = 0;
                            }
                            else if (int.TryParse(textBox.Text, out int idVal))
                            {
                                val = idVal;
                            }
                            else
                            {
                                MessageBox.Show($"Invalid ID format for '{key}'. Please enter a valid number.");
                                val = 0;
                            }
                        }
                        else
                        {
                            // Default to string if no special formatting
                            val = textBox.Text.ToString();
                        }

                        values[key] = val;
                    }


                    if (control is ComboBox comboBox)
                    {
                        string key = comboBox.Name.Replace("cmb_", "");
                        string val = "";

                        if (comboBox.Tag?.ToString() == "DYNAMIC")
                        {
                            key += "_id";
                            var selectedValue = comboBox.SelectedValue;

                            // Handle null SelectedValue
                            if (selectedValue != null && !(selectedValue is DataRowView))
                            {
                                values.Add(key, int.Parse(selectedValue.ToString()));
                            }
                            else
                            {
                                Console.WriteLine($"Warning: No valid selected value for {comboBox.Name}");
                            }
                        }
                        else
                        {
                            val = comboBox.Text?.ToString() ?? string.Empty;
                            values.Add(key, val);
                        }
                    }



                    if (control is CheckBox checkbox)
                    {
                        string key = checkbox.Name.Replace("chk_", "");
                        string val = String.Format("{0}", checkbox.Checked ? 1 : 0);
                        values.Add(key, val);
                    }


                    if (control is DateTimePicker dateTimePicker)
                    {
                        string key = dateTimePicker.Name.Replace("dtp_", "");
                        string val = String.Format("{0:yyyy-MM-dd HH:mm:ss}", dateTimePicker.Value);
                        values.Add(key, val);
                    }


                    if (control is NumericUpDown numericUpDown)
                    {
                        string key = numericUpDown.Name.Replace("txt_", "");
                        string val = String.Format("'{0}'", numericUpDown.Value);
                        values.Add(key, val);
                    }
                }
            }
            return values;
        }

        public static Dictionary<string, dynamic> GetControlsValuesV2(Panel[] pnl1)
        {
            Dictionary<string, dynamic> values = new Dictionary<string, dynamic>();

            foreach (Panel pnl in pnl1)
            {
                foreach (Control control in pnl.Controls)
                {
                    // Check if the control is a TextBox
                    // Check if the control is a TextBox
                    if (control is TextBox textBox)
                    {
                        string key = textBox.Name.Replace("txt_", "");
                        dynamic val = null;

                        // Handle money formatting
                        if (textBox.Tag != null && (textBox.Tag.ToString() == "money_format" || textBox.Tag.ToString() == "percent_format"))
                        {
                            if (decimal.TryParse(GetCleanedPriceValue(textBox.Text), out decimal tempVal))
                            {
                                val = tempVal;
                            }
                            else
                            {
                                MessageBox.Show("Invalid money format. Please enter a valid number.");
                                val = 0m;
                            }
                        }
                        // Handle _id conversion
                        else if (key.EndsWith("_id"))
                        {
                            // A blank id field (e.g. content_id on a tab whose content row
                            // hasn't been created/saved yet) is a normal "doesn't exist yet"
                            // state, not a user input mistake - it used to trip the same
                            // "Invalid ID format" warning as actually-malformed text, and it
                            // fired on tab switch/content-changed events, not just Save, since
                            // this runs any time this panel's values get read. Only warn when
                            // there's text that genuinely isn't a number.
                            if (string.IsNullOrWhiteSpace(textBox.Text))
                            {
                                val = 0;
                            }
                            else if (int.TryParse(textBox.Text, out int idVal))
                            {
                                val = idVal;
                            }
                            else
                            {
                                MessageBox.Show($"Invalid ID format for '{key}'. Please enter a valid number.");
                                val = 0;
                            }
                        }
                        else
                        {
                            // Default to string if no special formatting
                            val = textBox.Text.ToString();
                        }

                        values[key] = val;
                    }


                    if (control is ComboBox comboBox)
                    {
                        string key = comboBox.Name.Replace("cmb_", "");
                        string val = "";

                        if (comboBox.Tag?.ToString() == "DYNAMIC")
                        {
                            key += "_id";
                            var selectedValue = comboBox.SelectedValue;

                            // Handle null SelectedValue
                            if (selectedValue != null && !(selectedValue is DataRowView))
                            {
                                values.Add(key, int.Parse(selectedValue.ToString()));
                            }
                            else
                            {
                                Console.WriteLine($"Warning: No valid selected value for {comboBox.Name}");
                            }
                        }
                        else
                        {
                            val = comboBox.Text?.ToString() ?? string.Empty;
                            values.Add(key, val);
                        }
                    }



                    if (control is CheckBox checkbox)
                    {
                        string key = checkbox.Name.Replace("chk_", "");
                        string val = String.Format("{0}", checkbox.Checked ? 1 : 0);
                        values.Add(key, val);
                    }


                    if (control is DateTimePicker dateTimePicker)
                    {
                        string key = dateTimePicker.Name.Replace("dtp_", "");
                        string val = String.Format("{0:yyyy-MM-dd HH:mm:ss}", dateTimePicker.Value);
                        values.Add(key, val);
                    }


                    if (control is NumericUpDown numericUpDown)
                    {
                        string key = numericUpDown.Name.Replace("txt_", "");
                        string val = String.Format("'{0}'", numericUpDown.Value);
                        values.Add(key, val);
                    }
                }
            }
            return values;
        }

        public static Boolean ValidateControlsValues(Panel pnl)
        {
            Boolean isError = false;
            Dictionary<string, dynamic> values = new Dictionary<string, dynamic>();
            foreach (Control control in pnl.Controls)
            {
                // Check if the control is a TextBox
                if (control is TextBox textBox)
                {
                    string key = textBox.Name.Replace("txt_", "");
                    string val = "";
                    if (textBox.Tag == "REQUIRED" && textBox.Text == "")
                    {
                        control.BackColor = Color.Red;
                        control.ForeColor = Color.White;
                        isError = true; 
                    }
                    else
                    {
                        control.BackColor = Color.White;
                        control.ForeColor = Color.Black;
                    }
                } 
            } 
            return isError;
        }
        public static void BindControls(Panel[] pnl_list, DataTable dt, int selectedIndex = 0)
        {
            // dt can legitimately be empty (e.g. a fresh/no-record state) or selectedIndex can
            // be stale from a previous, larger dataset - either used to throw
            // IndexOutOfRangeException: "There is no row at position 0." Nothing to bind to, so
            // just leave the controls as they are instead of crashing.
            if (dt == null || dt.Rows.Count == 0 || selectedIndex < 0 || selectedIndex >= dt.Rows.Count)
                return;

            Dictionary<string, dynamic> values = new Dictionary<string, dynamic>();

            foreach (var col_name in dt.Columns)
            { 
                foreach (var pnl in pnl_list)
                { 
                    foreach (Control control in pnl.Controls)
                    {
                        
                        if (control.Name.Contains(col_name.ToString() ))
                        {
                            string column_name = col_name.ToString();
                            Console.WriteLine(column_name);

                            // Check if the control is a TextBox 
                            if (control is TextBox textBox && textBox.Name.Replace("txt_", "") == column_name)
                            {
                               
                                string key = textBox.Name.Replace("txt_", "");
                             
                                if (textBox.Tag == "money_format")
                                { 
                                    textBox.Text = Helpers.MoneyFormat(double.Parse(dt.Rows[selectedIndex][column_name].ToString()));
                                }
                                else
                                {
                                   
                                    textBox.Text = (string)dt.Rows[selectedIndex][column_name].ToString();
                                }
                            }

                            // Check if the control is a Combobox
                            if (control is ComboBox comboBox)
                            {
                                Console.WriteLine(comboBox.Name);
                                string key = comboBox.Name.Replace("cmb_", "") + "_id";

                                if (comboBox.Tag == "DYNAMIC")
                                {
                                   
                                    //Console.WriteLine(comboBox.Name);
                                    comboBox.SelectedValue = (string)dt.Rows[selectedIndex][key].ToString();
                                }
                                else
                                {
                                    string keys = comboBox.Name.Replace("cmb_", "");
                                    //Console.WriteLine(comboBox.Name);
                                    comboBox.Text = (string)dt.Rows[selectedIndex][column_name].ToString();
                                }
                             
                            }

                            // Check if the control is a Checkbox
                            if (control is CheckBox checkbox)
                            {
                                
                                string key = checkbox.Name.Replace("chk_", ""); 
                                checkbox.Checked = (string)dt.Rows[selectedIndex][column_name].ToString() == "1" ? true : false; 
                            }

                            // Check if the control is a DATETIME PICKER
                            if (control is DateTimePicker dateTimePicker)
                            {
                                string key = dateTimePicker.Name.Replace("dtp_", "");
                                string val = String.Format("'{0}'", dateTimePicker.Value);
                                object valueFromDataTable = dt.Rows[selectedIndex][column_name];

                                if (valueFromDataTable != DBNull.Value)
                                {
                                    if (valueFromDataTable is DateTime dateTimeValue)
                                    {
                                        dateTimePicker.Value = dateTimeValue;
                                    }
                                    else if (DateTime.TryParse(valueFromDataTable.ToString(), out DateTime parsedDate))
                                    {
                                        dateTimePicker.Value = parsedDate;
                                    }
                                    else
                                    {
                                        dateTimePicker.Value = DateTime.Now;
                                    }
                                }
                                else
                                {
                                    dateTimePicker.Value = DateTime.Now;
                                }

                            }

                            // Check if the control is a NUMERIC
                            if (control is NumericUpDown numericUpDown)
                            {
                                string key = numericUpDown.Name.Replace("txt_", "");
                                numericUpDown.Text = (string)dt.Rows[selectedIndex][column_name].ToString();
                            }
                        } 
                    }
                }
            }
             
        }
        public static string GetLocalIPAddress()
        {
            string localIP = string.Empty;

            // Get the host name
            string hostName = Dns.GetHostName();

            // Get the list of IP addresses associated with the host
            foreach (var ip in Dns.GetHostAddresses(hostName))
            {
                // Check if it's an IPv4 address
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    localIP = ip.ToString();
                    break; // Exit the loop after Getting the first IPv4 address
                }
            }

            return localIP;
        }

        public static DataTable GetDataTableFromUnboundGrid(DataGridView dgv)
        {
            DataTable dt = new DataTable();

            // Create columns using DataPropertyName
            foreach (DataGridViewColumn col in dgv.Columns)
            {
                string columnName = string.IsNullOrWhiteSpace(col.DataPropertyName) ? col.Name : col.DataPropertyName;
                Type columnType = col.ValueType ?? typeof(string);
                dt.Columns.Add(columnName, columnType); 
            }

            // Add rows
            foreach (DataGridViewRow row in dgv.Rows)
            {
                if (!row.IsNewRow)
                {
                    DataRow dtRow = dt.NewRow();
                    for (int i = 0; i < row.Cells.Count; i++)
                    {
                        dtRow[i] = row.Cells[i].Value ?? DBNull.Value;
                    }
                    dt.Rows.Add(dtRow);
                }
            }

            return dt;
        }
        public static string GetSerialNumber()
        {
            try
            {
                string serialNumber = string.Empty;
                ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT * FROM Win32_BaseBoard");

                foreach (ManagementObject mo in searcher.Get())
                {
                    serialNumber = mo["SerialNumber"].ToString();
                    break; // Assuming only one motherboard
                }
                return serialNumber;
            }
            catch (Exception ex)
            {
                
                Console.WriteLine("Error: " + ex.Message);
                return "";
            }
        }
        public static void ShowDialogMessage(string status,string message="")
        {
            switch (status)
            {
                case "success":
                    MessageBox.Show(message, "SMPC SOFTWARE", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    break;
                case "error":
                    MessageBox.Show(message, "SMPC SOFTWARE", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    break;
                default:
                    // Handle unexpected status values
                    MessageBox.Show("Unknown status: " + status, "SMPC SOFTWARE", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    break;
            }
        }
        public static void CopyFileTo(string filePath,string destinationPath)
        {
            try
            {
                File.Copy(filePath, destinationPath, true);
            }
            catch (Exception)
            { 
                throw;
            }
        }
        public static DataTable ConvertDataGridViewToDataTable(DataGridView dgv)
        {
            DataTable dataTable = new DataTable();

            foreach (DataGridViewColumn column in dgv.Columns)
            {
                if (!dataTable.Columns.Contains(column.Name))
                {
                    dataTable.Columns.Add(column.Name);
                }
            }

            foreach (DataGridViewRow row in dgv.Rows)
            {

                if (!row.IsNewRow)
                {
                    DataRow dataRow = dataTable.NewRow();
                    for (int i = 0; i < dgv.Columns.Count; i++)
                    {
                        string columnName = dgv.Columns[i].Name;

                        if (dataRow[columnName] == DBNull.Value || dataRow[columnName] == null)
                        {
                            dataRow[columnName] = row.Cells[i].Value ?? DBNull.Value;
                        }
                    }
                    dataTable.Rows.Add(dataRow);
                }
            }

            return dataTable;
        }
        public static string MoneyFormat(double money)
        {
            return String.Format("{0:N2}", money);
        }

        public static string MoneyFormatDecimal(decimal money)
        {
            return String.Format("{0:N2}", money);
        }


        // format to peso
        public static string FormatAsCurrency(object value)
        {
            if (decimal.TryParse(value?.ToString().Replace("₱", "").Replace(",", "").Trim(), out decimal number))
            {
                return number.ToString("C2", System.Globalization.CultureInfo.GetCultureInfo("en-PH"));
            }
            return "₱0.00";
        }


        // trims the peso sign
        public static string GetCleanedPriceValue(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return "0";
            // Remove currency symbols and thousands separators
            var cleaned = input.Replace("₱", "")
                               .Replace("$", "")
                               .Replace(",", "")
                               .Trim();

            return cleaned;
        }


        // Converts the data types to string so it can be easily editable
        public static DataTable ConvertDataTableToStringTable(DataTable originalTable)
        {
            DataTable stringTable = new DataTable();

            
            foreach (DataColumn col in originalTable.Columns)
            {
                stringTable.Columns.Add(col.ColumnName, typeof(string));
            }

            // Copy rows as strings
            foreach (DataRow row in originalTable.Rows)
            {
                var newRow = stringTable.NewRow();
                foreach (DataColumn col in originalTable.Columns)
                {
                    newRow[col.ColumnName] = row[col]?.ToString();
                }
                stringTable.Rows.Add(newRow);
            }

            return stringTable;
        }





        public static void GetModalData(TextBox textBox, DataView dataView)
        {
            int recordIndex = 0;
            textBox.Text = "";

            foreach (DataRowView rowView in dataView)
            {

                textBox.Text += recordIndex == 0 ? rowView["name"].ToString() : ", " + rowView["name"].ToString();
                recordIndex++;

            }

        }

        public static bool ConvertToIntIfString(Dictionary<string, object> data, string key)
        {
            if (data.ContainsKey(key) && data[key] is string strValue)
            {
                if (int.TryParse(strValue, out int intValue))
                {
                    data[key] = intValue;
                    return true;
                }
                else
                {
                    MessageBox.Show($"Invalid {key.Replace("_", " ")}");
                    return false;
                }
            }
            return true; // If the key is not present or not a string, no conversion needed
        }
        
        public static DataTable FilterDataTable(DataTable dataTable, string searchTerm, params string[] columnsToSearch)
        {
            if (dataTable == null || columnsToSearch == null || columnsToSearch.Length == 0)
            {
                return dataTable;
            }

            searchTerm = searchTerm?.ToLower() ?? string.Empty;

            var filteredRows = dataTable.AsEnumerable().Where(row =>
                columnsToSearch.Any(column =>
                    row[column]?.ToString().ToLower().Contains(searchTerm) == true));

            return filteredRows.Any() ? filteredRows.CopyToDataTable() : dataTable.Clone();
        }

        // The position, in the table a search picker was GIVEN, of the grid row the
        // user clicked. The pickers (SearchOrder, PRModal, SetupModal) hand their
        // caller a row index and the caller indexes its own table with it - but
        // e.RowIndex is only that index while the grid shows the table unfiltered
        // and unsorted. FilterDataTable above rebinds the grid to a COPY of the
        // matching rows, and a column-header click re-sorts the view; either way
        // grid row 0 stops being table row 0, and the caller opened the wrong record.
        //
        // Resolved through the row itself: a row bound from the source (a DataView
        // over it, sorted or not) is found by reference; a FilterDataTable copy is
        // matched by its values - the copy keeps the source's columns in order, so
        // the arrays line up. -1 when it cannot be found - never a guess.
        public static int SourceRowIndex(DataTable source, DataGridView grid, int gridRowIndex)
        {
            if (source == null || grid == null || gridRowIndex < 0 || gridRowIndex >= grid.Rows.Count)
                return -1;

            DataRow picked = (grid.Rows[gridRowIndex].DataBoundItem as DataRowView)?.Row;
            if (picked == null)
                return -1;

            int index = source.Rows.IndexOf(picked);
            if (index >= 0)
                return index;

            object[] values = picked.ItemArray;
            for (int i = 0; i < source.Rows.Count; i++)
            {
                DataRow candidate = source.Rows[i];
                if (candidate.RowState == DataRowState.Deleted)
                    continue;
                if (candidate.ItemArray.SequenceEqual(values))
                    return i;
            }

            return -1;
        }


        public static DataTable FilterExactDataTable(DataTable dataTable, string searchTerm, params string[] columnsToSearch)
        {
            if (dataTable == null || columnsToSearch == null || columnsToSearch.Length == 0)
            {
                return dataTable;
            }

            searchTerm = searchTerm?.ToLower() ?? string.Empty;

            var filteredRows = dataTable.AsEnumerable().Where(row =>
                columnsToSearch.Any(column =>
                    row[column]?.ToString().ToLower() == searchTerm));

            return filteredRows.Any() ? filteredRows.CopyToDataTable() : dataTable.Clone();
        }

        public static void GetBPIModalData(TextBox textBox, DataView dataView, int columnIndex)
        {
            if (dataView != null && dataView.Count > 0)
            {
                textBox.Text = dataView[0][columnIndex].ToString();
            }
        }
        public static void SetRowNumber(DataGridView grid, DataGridViewRowPostPaintEventArgs e, int columnIndex = 0)
        {
            if (grid != null && e.RowIndex >= 0 && columnIndex >= 0 && columnIndex < grid.ColumnCount)
            {
                grid.Rows[e.RowIndex].Cells[columnIndex].Value = (e.RowIndex + 1).ToString();
            }
        }
        public static void ClearDataGridView(DataGridView grid)
        {
            if (grid != null && grid.Rows.Count > 0)
            {
                grid.Rows.Clear();
            }
        }
        public static void LoadDirectory(string path, TreeView treeView)
        { 
            // Clear any existing nodes
            treeView.Nodes.Clear();

            // Get the top-level directory and create a root node
            DirectoryInfo dirInfo = new DirectoryInfo(path);
            TreeNode rootNode = new TreeNode(dirInfo.Name);
            treeView.Nodes.Add(rootNode);

            // Load subdirectories and files recursively
            LoadSubdirectoriesAndFiles(rootNode, dirInfo.FullName);
        } 
        private static void LoadSubdirectoriesAndFiles(TreeNode parentNode, string path)
        {
            try
            {
                // Get all subdirectories in the given path
                string[] subdirectories = Directory.GetDirectories(path);

                foreach (string subdirectory in subdirectories)
                {
                    // Create a node for the subdirectory
                    DirectoryInfo dirInfo = new DirectoryInfo(subdirectory);
                    TreeNode subDirNode = new TreeNode(dirInfo.Name);

                    // Add the subdirectory node to the parent node
                    parentNode.Nodes.Add(subDirNode);

                    // Recursively load subdirectories and files into the current subdirectory node
                    LoadSubdirectoriesAndFiles(subDirNode, subdirectory);
                }

                // Get all files in the current directory and add them as leaf nodes
                string[] files = Directory.GetFiles(path);
                foreach (string file in files)
                {
                    FileInfo fileInfo = new FileInfo(file);
                    TreeNode fileNode = new TreeNode(fileInfo.Name);
                    fileNode.Tag = file; // Store the full file path in the Tag property
                    parentNode.Nodes.Add(fileNode); // Add the file node
                }
            }
            catch (UnauthorizedAccessException)
            {
                // Handle access permissions issues if necessary
            }
        }

        public static JObject GetChangedEntries(JObject newData, Dictionary<string, dynamic> cachedData)
        {
            var changedEntries = new Dictionary<string, dynamic>();
           
            foreach (var kvp in newData)
            {
                string key = kvp.Key;
                var newValue = kvp.Value;

                if (cachedData.TryGetValue(key, out var cachedValue))
                {
                    string newJson = JsonConvert.SerializeObject(newValue);
                    string cachedJson = JsonConvert.SerializeObject(cachedValue);

                    if (newJson == cachedJson)
                    {
                        continue; // Value is the same, skip it
                    }
                }

                // Either new key or changed value
                changedEntries[key] = newValue;
            }

            return JObject.FromObject(changedEntries);
        }

        public static Dictionary<string, dynamic> GetChangedEntries(Dictionary<string, JArray> newData, Dictionary<string, dynamic> cachedData)
        {
            var changedEntries = new Dictionary<string, dynamic>();

            foreach (var kvp in newData)
            {
                string key = kvp.Key;
                var newValue = kvp.Value;

                if (cachedData.TryGetValue(key, out var cachedValue))
                {
                    string newJson = JsonConvert.SerializeObject(newValue);
                    string cachedJson = JsonConvert.SerializeObject(cachedValue);

                    if (newJson == cachedJson)
                    {
                        continue; // Value is the same, skip it
                    }
                }

                // Either new key or changed value
                changedEntries[key] = newValue;
            }

            return changedEntries;
        }
        public static class SalesItemRowStyler
        {
            public static void ApplyStyle(DataGridView dgv, int rowIndex, string type)
            {
                if (rowIndex < 0 || rowIndex >= dgv.Rows.Count) return;

                DataGridViewRow row = dgv.Rows[rowIndex];

                switch (type.ToLower())
                {
                    case "parent":
                        row.DefaultCellStyle.BackColor = Color.LightBlue;
                        row.DefaultCellStyle.Font = new Font(dgv.Font, FontStyle.Bold);
                        if (row.Cells["quick_item_code"].Value != null)
                            row.Cells["quick_item_code"].Value = "▶ " + row.Cells["quick_item_code"].Value.ToString();
                        break;

                    case "child":
                        row.DefaultCellStyle.BackColor = Color.LightGray;
                        row.DefaultCellStyle.Font = new Font(dgv.Font, FontStyle.Italic);
                        if (row.Cells["quick_item_code"].Value != null)
                            row.Cells["quick_item_code"].Value = "   ↳ " + row.Cells["quick_item_code"].Value.ToString();
                        break;

                    case "single":
                        row.DefaultCellStyle.BackColor = Color.White;
                        row.DefaultCellStyle.Font = new Font(dgv.Font, FontStyle.Regular);
                        // No arrow for single items
                        break;
                }
            }
        }

        // Cascades a BOM head's quantity down to its children (user-reported bug,
        // 2026-09-04, seen in Sales Quotation and confirmed to affect Project Quote,
        // Quick Quote and Engineering alike, since all three insert BOM rows through
        // the same code and edit them in a grid shaped the same way).
        //
        // GetBomDataRecursive (the inserter) sets every child's qty straight from its
        // raw bom_qty in the BOM template - correct for "1 head", but nothing ever
        // revisits it if the head's own qty is then changed. Typing 10 into a head
        // that needs 2 of a child left that child at 2 forever, not 20 - understating
        // real material need on anything downstream (an Item Request, a Purchase
        // Requisition) built off these quantities.
        //
        // No schema change: the ratio isn't stored on the transaction row at all (it's
        // only ever written once, into the same "qty" column the user can then edit),
        // so it is re-looked-up here from the BOM template itself (bomDetails - the
        // same table GetBomDataRecursive read to build the rows in the first place),
        // keyed by (this row's own bom_id, the child's item_id). That pair is exactly
        // what GetBomDataRecursive used to find the child, so it is stable across any
        // number of edits - re-deriving it, rather than trying to remember a ratio
        // computed diff-style, means each edit is quantity-in produces quantity-out.
        //
        // Column names differ between the two grids this is called from (project's
        // "project_items_qty"/"project_items_bom_id" vs quick quote's "quick_qty"/
        // "quick_bom_id"), which is why they are parameters rather than hardcoded.
        public static void CascadeBomQuantity(DataGridView dgv, DataTable bomDetails, int editedRowIndex,
            string qtyColumn, string referenceCodeColumn, string itemIdColumn, string bomIdColumn)
        {
            if (dgv == null || bomDetails == null || bomDetails.Rows.Count == 0)
                return;
            if (editedRowIndex < 0 || editedRowIndex >= dgv.Rows.Count)
                return;

            DataGridViewRow editedRow = dgv.Rows[editedRowIndex];
            if (editedRow.IsNewRow)
                return;
            if (!decimal.TryParse(editedRow.Cells[qtyColumn].Value?.ToString(), out decimal newQty))
                return;

            CascadeBomQuantityToChildren(dgv, bomDetails, editedRow, newQty,
                qtyColumn, referenceCodeColumn, itemIdColumn, bomIdColumn);
        }

        private static void CascadeBomQuantityToChildren(DataGridView dgv, DataTable bomDetails,
            DataGridViewRow parentRow, decimal parentQty,
            string qtyColumn, string referenceCodeColumn, string itemIdColumn, string bomIdColumn)
        {
            string parentRef = parentRow.Cells[referenceCodeColumn].Value?.ToString();
            if (string.IsNullOrWhiteSpace(parentRef))
                return;

            // A leaf row's bom_id is the PARENT's bom, not its own - it has no
            // children of its own to cascade into, which is exactly what "no bom_id
            // of its own" (0, or unparseable) means here.
            if (!int.TryParse(parentRow.Cells[bomIdColumn].Value?.ToString(), out int parentBomId) || parentBomId <= 0)
                return;

            string prefix = parentRef + ".";

            foreach (DataGridViewRow row in dgv.Rows)
            {
                if (row.IsNewRow || row == parentRow)
                    continue;

                string refCode = row.Cells[referenceCodeColumn].Value?.ToString();
                if (string.IsNullOrWhiteSpace(refCode) || !refCode.StartsWith(prefix))
                    continue;

                // Direct child only - one more segment than the parent ("1.1" under
                // "1" qualifies; "1.1.1" does not, since it isn't reached until this
                // same walk recurses into "1.1" below).
                if (refCode.Substring(prefix.Length).Contains("."))
                    continue;

                if (!int.TryParse(row.Cells[itemIdColumn].Value?.ToString(), out int childItemId) || childItemId <= 0)
                    continue;

                DataRow ratioRow = bomDetails.AsEnumerable().FirstOrDefault(r =>
                    Convert.ToInt32(r["item_bom_id"]) == parentBomId && Convert.ToInt32(r["item_id"]) == childItemId);
                if (ratioRow == null)
                    continue;

                decimal ratio = Convert.ToDecimal(ratioRow["bom_qty"]);
                decimal childQty = parentQty * ratio;

                row.Cells[qtyColumn].Value = childQty == Math.Floor(childQty)
                    ? childQty.ToString("0")
                    : childQty.ToString();

                CascadeBomQuantityToChildren(dgv, bomDetails, row, childQty,
                    qtyColumn, referenceCodeColumn, itemIdColumn, bomIdColumn);
            }
        }

        public static void EnableGroupHeaders(DataGridView dgv, Dictionary<string, string[]> columnGroups)
        {
            if (dgv == null || columnGroups == null || columnGroups.Count == 0)
                return;

            // Double buffer to reduce flickering
            typeof(DataGridView).InvokeMember("DoubleBuffered",
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.SetProperty,
                null, dgv, new object[] { true });

            // Redraw on scroll/resize
            dgv.Scroll += (s, e) => dgv.Invalidate();
            dgv.ColumnWidthChanged += (s, e) => dgv.Invalidate();

            // Paint group headers
            dgv.Paint += (s, e) => DrawGroupHeaders(dgv, e, columnGroups);

            // Override column header painting
            dgv.CellPainting += (s, e) => DrawGroupedHeaderCells(dgv, e, columnGroups);
        }

        private static void DrawGroupHeaders(DataGridView dgv, PaintEventArgs e, Dictionary<string, string[]> groups)
        {
            foreach (var group in groups)
            {
                string groupName = group.Key;
                string[] cols = group.Value;

                if (!cols.All(c => dgv.Columns.Contains(c)))
                    continue;

                DataGridViewColumn firstCol = dgv.Columns[cols.First()];
                DataGridViewColumn lastCol = dgv.Columns[cols.Last()];

                Rectangle r1 = dgv.GetCellDisplayRectangle(firstCol.Index, -1, true);
                Rectangle r2 = dgv.GetCellDisplayRectangle(lastCol.Index, -1, true);

                if (r1.IsEmpty || r2.IsEmpty) continue;

                Rectangle headerRect = new Rectangle(r1.X, r1.Y, r2.Right - r1.X, r1.Height / 2);

                using (Brush b = new SolidBrush(SystemColors.Control))
                    e.Graphics.FillRectangle(b, headerRect);

                e.Graphics.DrawRectangle(Pens.Gray, headerRect);

                // WordBreak lets a long group label wrap to fit the width it spans, the
                // same way an ordinary column header does when the grid's
                // ColumnHeadersDefaultCellStyle has WrapMode = True (e.g. "ITEM INV TYPE"
                // stacking to three lines in a 40px column). Without it the label was
                // drawn on one line, so callers had to hard-code Environment.NewLine into
                // the text to break it - which produced ragged leading spaces. Explicit
                // newlines still break where they're given, so this is additive.
                TextRenderer.DrawText(e.Graphics, groupName,
                    dgv.ColumnHeadersDefaultCellStyle.Font,
                    headerRect, Color.Black,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter |
                    TextFormatFlags.WordBreak);
            }
        }

        private static void DrawGroupedHeaderCells(DataGridView dgv, DataGridViewCellPaintingEventArgs e, Dictionary<string, string[]> columnGroups)
        {
            if (e.RowIndex == -1 && e.ColumnIndex >= 0)
            {
                // Only take over headers that actually sit under a group label. This used
                // to repaint EVERY header in the grid, which meant ordinary columns were
                // drawn single-line by the TextRenderer call below and e.Handled = true
                // suppressed the grid's own rendering - so the header style's
                // WrapMode = True never applied and long labels like
                // "DISTANCE TRAVELLED / SET" were truncated rather than wrapped. Ungrouped
                // columns are now left to the grid to paint normally, which wraps them.
                string columnName = dgv.Columns[e.ColumnIndex].Name;
                bool isGrouped = columnGroups != null &&
                                 columnGroups.Values.Any(cols => cols != null && cols.Contains(columnName));

                if (!isGrouped)
                    return;

                e.PaintBackground(e.CellBounds, true);

                Rectangle fullRect = e.CellBounds;

                // Bottom half for column text
                Rectangle textRect = fullRect;
                textRect.Y += textRect.Height / 2;
                textRect.Height /= 2;

                TextRenderer.DrawText(e.Graphics,
                    e.FormattedValue?.ToString() ?? "",
                    e.CellStyle.Font, textRect,
                    e.CellStyle.ForeColor,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);

                e.Handled = true;
            }
        }

    }
} 
