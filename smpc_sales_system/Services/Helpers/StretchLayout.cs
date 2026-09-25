using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace smpc_app.Services.Helpers
{
    // Spreads a panel's contents across whatever width the panel is given, instead of leaving
    // them packed against the left edge at their designed size.
    //
    // The Project Quotation (Sales) and its engineering view were laid out at fixed pixel
    // positions for a ~1130-1190px area. On a wider window the containers already widened -
    // they are docked or anchored - but their contents did not, so the extra width showed as
    // a wide empty band down the right of every block (user-reported 2026-09-24: "there are
    // space not suppose to be big like this"). The user chose to STRETCH the work areas, with
    // text kept at the same size as every other screen, over zooming the whole page.
    //
    // How: each child's left edge and width are recorded as fractions of the panel's designed
    // width, and re-applied against the panel's current width on every resize. Absolute, not
    // incremental, so repeated resizing never drifts. Only the horizontal axis moves; tops and
    // heights are left exactly as designed.
    //
    //   * Inputs, grids and inner panels widen with the panel.
    //   * Labels, buttons and check boxes only move - a stretched button or caption reads as
    //     a layout bug, and their text does not need the room.
    //
    // Never narrower than designed: below the designed width everything stays put and the
    // page scrolls, as it always has.
    public static class StretchLayout
    {
        private sealed class Slot
        {
            public float Left;
            public float Width;
            public bool Widens;
        }

        private sealed class State
        {
            public int DesignWidth;
            public readonly Dictionary<Control, Slot> Slots = new Dictionary<Control, Slot>();
        }

        private static readonly Dictionary<Control, State> Panels = new Dictionary<Control, State>();

        public static void Attach(Control panel)
        {
            if (panel == null || panel.IsDisposed || Panels.ContainsKey(panel)) return;

            int design = panel.ClientSize.Width;
            if (design <= 0) return;

            var state = new State { DesignWidth = design };
            foreach (Control child in panel.Controls)
            {
                // A docked child is sized by its parent already; an anchored-right one would be
                // moved twice, once by its anchor and once here.
                if (child.Dock != DockStyle.None) continue;
                if ((child.Anchor & AnchorStyles.Right) != 0) continue;

                state.Slots[child] = new Slot
                {
                    Left = (float)child.Left / design,
                    Width = (float)child.Width / design,
                    Widens = Widens(child),
                };
            }

            Panels[panel] = state;
            panel.Resize += (s, e) => Apply(panel);
            panel.Disposed += (s, e) => Panels.Remove(panel);

            Apply(panel);
        }

        // Grid columns widen with the grid. Each column keeps its designed width as a floor and
        // as its share of any extra room, so the proportions the designer chose are kept and
        // nothing is ever narrower than it was.
        public static void FillColumns(DataGridView grid)
        {
            if (grid == null || grid.AutoSizeColumnsMode == DataGridViewAutoSizeColumnsMode.Fill) return;

            // FillWeight totals above 65535 are rejected by DataGridView; designed pixel widths
            // are nowhere near that, but scale down rather than throw if a grid ever is.
            float total = 0;
            foreach (DataGridViewColumn col in grid.Columns) total += col.Width;
            float factor = total > 60000 ? 60000f / total : 1f;

            foreach (DataGridViewColumn col in grid.Columns)
            {
                if (col.AutoSizeMode != DataGridViewAutoSizeColumnMode.NotSet) continue;

                col.MinimumWidth = Math.Max(col.MinimumWidth, Math.Max(2, col.Width));
                col.FillWeight = Math.Max(1f, col.Width * factor);
            }

            grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        }

        private static bool Widens(Control c)
        {
            return !(c is Label || c is ButtonBase || c is PictureBox);
        }

        private static void Apply(Control panel)
        {
            State state;
            if (!Panels.TryGetValue(panel, out state) || panel.IsDisposed) return;

            int width = Math.Max(panel.ClientSize.Width, state.DesignWidth);

            panel.SuspendLayout();
            try
            {
                foreach (var entry in state.Slots)
                {
                    Control child = entry.Key;
                    if (child.IsDisposed) continue;

                    Slot slot = entry.Value;
                    int left = (int)Math.Round(slot.Left * width);

                    if (slot.Widens)
                        child.SetBounds(left, child.Top, (int)Math.Round(slot.Width * width), child.Height);
                    else
                        child.Left = left;
                }
            }
            finally
            {
                panel.ResumeLayout(true);
            }
        }
    }
}
