using smpc_sales_app.Services.Sales;
using smpc_sales_system.Services.Sales.Models;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace smpc_sales_system.Pages.Sales
{
    // One line item's worth of what StockCheckModal needs - built by Quotation.cs from
    // the quotation's own grid rows, a stock lookup per item, and a reservation-status
    // lookup per line (nothing reserves a line automatically anymore, so this has to be
    // asked for explicitly - see ItemStockCheckService.GetReservation).
    public class StockCheckRow
    {
        public int ItemId { get; set; }
        public string ItemName { get; set; }
        public int QuickId { get; set; }
        public int QuotationId { get; set; }
        public int RequiredQty { get; set; }
        public AvailableStockModel Stock { get; set; }
        public bool IsReserved { get; set; }
        // How much this exact line already has reserved (0 if IsReserved is false). Stock
        // (below) is the shared, already-netted-of-everyone's-reservations figure for the
        // item, so a line that already holds its own reservation needs this added back
        // before judging ITS OWN shortage - otherwise a line correctly holding exactly the
        // stock it needs would see itself as short by its own qty (see class remarks).
        public int OwnReservedQty { get; set; }
        // The quotation's own ValidUntil, if there was one to parse - passed through so a
        // freshly-checked RESERVE has something to expire against. Null just means this
        // reservation won't get picked up by the periodic expiry sweep.
        public DateTime? ExpiresAt { get; set; }
        // The line's reference_code - the only identifier a not-yet-saved line has that
        // survives the save+reload round trip (QuickId is 0 until then). Used to match
        // "this unsaved line wants RESERVE" back to its real row afterward - see
        // Quotation.cs's ApplyPendingReservationsAsync.
        public string ReferenceCode { get; set; }
    }

    // Per-quotation stock checker opened by clicking the INV. column (flagged or not) or
    // right-clicking QTY's header on Quick Quote / Project Quotation's line-item grid (see
    // Quotation.cs's HandleStockCheckClick) - lists every
    // line item in the current quotation at once, styled to match the "PROJECTED
    // INVENTORY" mockup (STOCK / PROJ. / RESERVE columns, negative projections in red
    // parentheses).
    //
    // RESERVE is a plain manual checkbox, editable freely by any sales user while this
    // modal is open - it does NOT call the backend on every click. Nothing is applied
    // until SAVE is pressed, which then compares each row's checkbox against what it
    // started as and only touches the lines that actually changed
    // (CreateStockReservation for newly checked, ReleaseStockReservation for newly
    // unchecked). CANCEL discards everything typed/toggled here.
    //
    // A line that hasn't been saved yet (QuickId <= 0) has nothing to attach a
    // reservation to, but the checkbox is still checkable - checking it just records
    // the intent in PendingReserveReferenceCodes instead of calling the backend.
    // Quotation.cs holds onto that intent and actually reserves the stock right after
    // the quotation is saved and the line gets a real id (see
    // ApplyPendingReservationsAsync).
    //
    // STOCK/PROJ. for a line that already has its own reservation are shown net of that
    // line's own hold added back (see EffectiveAvailable) - Stock.available already
    // subtracts every active reservation including this line's, so without adding it
    // back a line sitting on exactly the stock it needs would flag itself as short by
    // its own qty even though nothing else is competing for it. A different, unreserved
    // quotation checking the same item still sees the real (lower) available, since it
    // has no reservation of its own to add back - that's the correct "someone already
    // has this" shortage.
    public partial class StockCheckModal : Form
    {
        // Every quick_id whose reservation was actually created/released by the last
        // SAVE, so the caller can refresh what it treats as reserved.
        public HashSet<int> ChangedQuickIds { get; } = new HashSet<int>();

        // reference_code of every not-yet-saved line that got checked this SAVE - see
        // class remarks.
        public List<string> PendingReserveReferenceCodes { get; } = new List<string>();

        // Quotation.cs supplies this - opening frm_canvas_modal needs bpi_general/
        // bpi_items, which live over there, not here, so SEND REQUEST just hands back
        // which line was picked rather than this modal opening the canvas sheet itself.
        private readonly Action<StockCheckRow> _onSendRequest;

        public StockCheckModal(List<StockCheckRow> rows, Action<StockCheckRow> onSendRequest)
        {
            InitializeComponent();

            _onSendRequest = onSendRequest;

            bool anyUnsaved = false;

            foreach (var line in rows)
            {
                int rowIndex = dgv_projected_inventory.Rows.Add();
                var gridRow = dgv_projected_inventory.Rows[rowIndex];
                gridRow.Tag = line;

                int effectiveAvailable = EffectiveAvailable(line);
                int projected = effectiveAvailable - line.RequiredQty;
                bool isUnsaved = line.QuickId <= 0;

                string itemLabel = string.IsNullOrWhiteSpace(line.ItemName) ? "(unnamed item)" : line.ItemName;
                if (isUnsaved) itemLabel += " (not saved yet)";

                gridRow.Cells["col_item"].Value = itemLabel;
                // Available, not physical - physical never changes when something's
                // reserved, so it'd show the same number forever no matter how much of
                // it other quotations have already claimed. Available already nets that
                // out (see GetAvailableStock), so checking Q#0019 right after reserving
                // 1 unit for Q#0005 correctly shows STOCK 4, not a static 5. But it also
                // nets out THIS line's own reservation, so effectiveAvailable (not the
                // raw figure) is what's shown here - see EffectiveAvailable/class remarks.
                gridRow.Cells["col_stock"].Value = effectiveAvailable;
                gridRow.Cells["col_arrow"].Value = "▶";
                gridRow.Cells["col_proj"].Value = projected;
                gridRow.Cells["col_reserve"].Value = line.IsReserved;

                if (isUnsaved)
                {
                    // No SalesQuotationQuick id yet to attach a reservation to (see
                    // Quotation.cs's HandleStockCheckClick) - still checkable though;
                    // checking it just queues the intent (see PendingReserveReferenceCodes)
                    // rather than calling the backend immediately.
                    gridRow.DefaultCellStyle.ForeColor = Color.DimGray;
                    anyUnsaved = true;
                }
            }

            lbl_reserve_note.Text = anyUnsaved
                ? "Toggle RESERVE for any line, then press SAVE to apply. Lines marked \"not saved yet\" will be reserved automatically once you save the whole quotation."
                : "Toggle RESERVE for any line, then press SAVE to apply - nothing changes until you save.";
        }

        private void StockCheckModal_Load(object sender, EventArgs e)
        {
        }

        // What's actually free for THIS line to claim: the shared available figure with
        // this line's own existing reservation added back (0 if it doesn't have one). See
        // class remarks for why - Stock.available is shared across every line in this
        // modal (and across other quotations), so it already reflects everyone else's
        // holds; a line's own hold shouldn't count against itself.
        private static int EffectiveAvailable(StockCheckRow line) => line.Stock.available + line.OwnReservedQty;

        // Colors/parenthesizes PROJ. in red when it's negative - matches the mockup's
        // shortage highlighting.
        private void dgv_projected_inventory_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (dgv_projected_inventory.Columns[e.ColumnIndex].Name != "col_proj") return;
            if (!int.TryParse(e.Value?.ToString(), out int proj)) return;

            if (proj < 0)
            {
                e.Value = $"({Math.Abs(proj)})";
                e.CellStyle.ForeColor = Color.Red;
                e.FormattingApplied = true;
            }
        }

        // Standard WinForms idiom: a DataGridViewCheckBoxColumn's edit doesn't commit
        // (so its Value isn't up to date yet) until the cell loses focus, unless you
        // force it here - needed since SAVE reads every row's current checkbox Value,
        // including whichever cell was clicked last and may still be "current".
        private void dgv_projected_inventory_CurrentCellDirtyStateChanged(object sender, EventArgs e)
        {
            if (dgv_projected_inventory.IsCurrentCellDirty)
            {
                dgv_projected_inventory.CommitEdit(DataGridViewDataErrorContexts.Commit);
            }
        }

        // Blocks checking RESERVE when there isn't enough available stock to cover this
        // line's required qty (STOCK 0 is just the extreme case of this - a shortage is
        // a shortage whether it's 0 or "have 2, need 5"). Unlike the actual
        // reserve/release calls, this is purely local validation - nothing's been sent
        // to the backend yet at this point (see class remarks on SAVE being what
        // applies changes), so reverting the checkbox here has nothing to undo.
        private void dgv_projected_inventory_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            if (dgv_projected_inventory.Columns[e.ColumnIndex].Name != "col_reserve") return;

            var gridRow = dgv_projected_inventory.Rows[e.RowIndex];
            var line = gridRow.Tag as StockCheckRow;
            if (line == null) return;

            var cell = gridRow.Cells["col_reserve"];
            bool nowChecked = cell.Value is bool b && b;

            // Only validate an actual new check, not the initial row population (which
            // sets this cell to line.IsReserved before any user interaction) and not a
            // no-op re-check back to where it started. Skipping this guard would trip
            // the warning on load for any row that's already validly reserved but whose
            // available happens to look negative now that its own reservation is
            // netted out of the total.
            if (!nowChecked || nowChecked == line.IsReserved) return;

            int effectiveAvailable = EffectiveAvailable(line);
            if (effectiveAvailable < line.RequiredQty)
            {
                MessageBox.Show(
                    $"Not enough stock to reserve \"{line.ItemName}\" - available is {effectiveAvailable}, this line needs {line.RequiredQty}.",
                    "Insufficient Stock",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                cell.Value = false;
            }
        }

        private async void btn_save_Click(object sender, EventArgs e)
        {
            dgv_projected_inventory.EndEdit();

            var toReserve = new List<StockCheckRow>();
            var toRelease = new List<StockCheckRow>();
            PendingReserveReferenceCodes.Clear();

            foreach (DataGridViewRow gridRow in dgv_projected_inventory.Rows)
            {
                var line = gridRow.Tag as StockCheckRow;
                if (line == null) continue;

                bool nowChecked = gridRow.Cells["col_reserve"].Value is bool b && b;
                if (nowChecked == line.IsReserved) continue;

                if (line.QuickId <= 0)
                {
                    // Not saved yet - nothing to attach a reservation to right now (and
                    // nothing to release either, since an unsaved line was never
                    // reserved in the first place). Queue the intent instead; Quotation.cs
                    // applies it once this line has a real id.
                    if (nowChecked && !string.IsNullOrEmpty(line.ReferenceCode))
                    {
                        PendingReserveReferenceCodes.Add(line.ReferenceCode);
                    }
                    continue;
                }

                if (nowChecked) toReserve.Add(line);
                else toRelease.Add(line);
            }

            if (toReserve.Count == 0 && toRelease.Count == 0)
            {
                DialogResult = DialogResult.OK;
                Close();
                return;
            }

            if (toRelease.Count > 0)
            {
                var names = string.Join(", ", toRelease.ConvertAll(l => l.ItemName));
                var confirm = MessageBox.Show(
                    $"This will release reserved stock for: {names}. It won't be automatically re-reserved - continue?",
                    "Confirm Changes",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);

                if (confirm != DialogResult.Yes) return;
            }

            btn_save.Enabled = false;
            btn_close.Enabled = false;

            var failures = new List<string>();

            foreach (var line in toReserve)
            {
                try
                {
                    await ItemStockCheckService.CreateReservation(line.ItemId, line.RequiredQty, line.QuickId, line.QuotationId, line.ExpiresAt);
                    line.IsReserved = true;
                    ChangedQuickIds.Add(line.QuickId);
                }
                catch (Exception ex)
                {
                    failures.Add($"{line.ItemName}: {ex.Message}");
                }
            }

            foreach (var line in toRelease)
            {
                try
                {
                    await ItemStockCheckService.ReleaseReservation(line.QuickId);
                    line.IsReserved = false;
                    ChangedQuickIds.Add(line.QuickId);
                }
                catch (Exception ex)
                {
                    failures.Add($"{line.ItemName}: {ex.Message}");
                }
            }

            btn_save.Enabled = true;
            btn_close.Enabled = true;

            if (failures.Count > 0)
            {
                MessageBox.Show(
                    "Some changes couldn't be applied:\n" + string.Join("\n", failures),
                    "Some Changes Failed",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                // Leave the modal open so the checkboxes still reflect what the user
                // asked for and they can retry SAVE rather than losing the edits.
                return;
            }

            DialogResult = DialogResult.OK;
            Close();
        }

        private void btn_close_Click(object sender, EventArgs e)
        {
            // Discards whatever's been toggled - nothing was ever sent to the backend
            // until SAVE, so there's nothing to undo here.
            DialogResult = DialogResult.Cancel;
            Close();
        }

        // Opens the Canvas Sheet for whichever line is currently selected, so a sales rep
        // looking at a shortage can go straight to lining up supplier quotes for it -
        // replaces the old REQUEST PURCHASE button, which asked for a purchase directly
        // rather than canvassing suppliers first. Only makes sense for a line that's
        // actually short, so it's blocked the same way a fresh RESERVE would be (see
        // dgv_projected_inventory_CellValueChanged) - already-covered lines have nothing
        // to request.
        private void btn_send_request_Click(object sender, EventArgs e)
        {
            var gridRow = dgv_projected_inventory.CurrentRow;
            if (gridRow == null)
            {
                MessageBox.Show("Select a line first.", "No Line Selected", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var line = gridRow.Tag as StockCheckRow;
            if (line == null) return;

            if (EffectiveAvailable(line) >= line.RequiredQty)
            {
                MessageBox.Show(
                    $"\"{line.ItemName}\" already has enough stock - nothing to request.",
                    "Stock OK",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            _onSendRequest?.Invoke(line);
        }
    }
}
