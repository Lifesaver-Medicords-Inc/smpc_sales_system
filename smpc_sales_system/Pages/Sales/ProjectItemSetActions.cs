using smpc_app.Services.Helpers;
using smpc_sales_app.Services.Helpers;
using smpc_sales_app.Services.Sales;
using smpc_sales_system.Services.Sales;
using smpc_sales_system.Services.Sales.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace smpc_sales_system.Pages.Sales
{
    /// <summary>
    /// What a project quotation's item set can do beyond typing: SIZE UP and FINAL pump
    /// picks, applying a template, and the stock check.
    ///
    /// Extracted from Quotation.cs on 2026-09-26, for the same reason SalesItemGridEditor was
    /// (2026-09-04): the Engineering app shows the same ItemSetUC, and every one of these
    /// lived as private methods on the Sales form, out of its reach. Engineering grew its own
    /// copies instead - a FINAL that dropped FLA and VOLTAGE (so no amp could ever be worked
    /// out there), a SIZE UP that downloaded the whole catalogue - or went without (template,
    /// stock check). "All the function had in sales project quotation suppose on the
    /// engineering" (user, 2026-09-26): one implementation, both screens.
    ///
    /// Everything here works from the ItemSetUC it is given. The one-picker-at-a-time guard
    /// and the busy state stay with each screen, which owns the clicks.
    /// </summary>
    public static class ProjectItemSetActions
    {
        // ---- SIZE UP -------------------------------------------------------------------

        /// <summary>
        /// The pump picker for SIZE UP. "Is this a pump" = ITEM NAME "PUMP" (spec 17.2) -
        /// not item_class, and not the specs table, which only has a row once someone has
        /// filled a pump's electrical specs in. Pumps already listed arrive pre-checked.
        /// </summary>
        public static async Task PickSizeUpAsync(ItemSetUC uc)
        {
            if (uc == null) return;

            List<ItemPickerRow> pumpRows;
            try
            {
                pumpRows = await ItemService.GetPumpPickerRows();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Could not load the pump list." + Environment.NewLine + ex.Message, "No Pump Data", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (pumpRows.Count == 0)
            {
                MessageBox.Show("No pump items are set up yet. Please add pump items before using this.", "No Pump Data", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            DataTable pumps = JsonHelper.ToDataTable(pumpRows);

            using (var modal = new SizeUpPickerModal(pumps, uc.GetSizeUpItemIds()))
            {
                if (modal.ShowDialog() != DialogResult.OK) return;

                foreach (var pick in modal.GetSelectedItems())
                    uc.AddSizeUpRow(pick.ItemId.ToString(), pick.Model);
            }
        }

        // ---- FINAL ---------------------------------------------------------------------

        /// <summary>
        /// The pump picker for FINAL: only what SIZE UP lists (spec 5.1.4), multi-select,
        /// and each pick carries the FLA and VOLTAGE from its Item Entry specs - the inputs of
        /// both wiring amp formulas (spec 8.4). A pump with no FLA still goes in, with a
        /// blank one.
        /// </summary>
        public static async Task PickFinalAsync(ItemSetUC uc)
        {
            if (uc == null) return;

            var sizeUpIds = uc.GetSizeUpItemIds();
            if (sizeUpIds.Count == 0)
            {
                MessageBox.Show("Add at least one candidate under SIZE UP before selecting FINAL.", "Size Up Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            List<ItemPickerRow> pumpRows;
            try
            {
                pumpRows = await ItemService.GetPumpPickerRows(null, sizeUpIds);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Could not load the pump list." + Environment.NewLine + ex.Message, "No Matching Items", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Kept even though the server filters: an API that predates ?ids= ignores the
            // parameter and answers with every pump, and FINAL must never offer more than
            // SIZE UP lists.
            var sizeUpIdSet = new HashSet<int>(sizeUpIds);
            pumpRows = pumpRows.Where(row => sizeUpIdSet.Contains(row.id)).ToList();

            if (pumpRows.Count == 0)
            {
                MessageBox.Show("None of the items in the item list match the pump data. Please check that the pump items still exist in the item catalog.", "No Matching Items", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            DataTable pumpTable = JsonHelper.ToDataTable(pumpRows);

            using (var modal = new SizeUpPickerModal(pumpTable, uc.GetFinalItemIds(), "Select Pumps for Final"))
            {
                if (modal.ShowDialog() != DialogResult.OK) return;

                var picks = modal.GetSelectedItems()
                                 .Where(pick => !string.IsNullOrEmpty(pick.Model))
                                 .ToList();

                // The electrical specs of the pumps just picked, and nothing else.
                DataTable specs = new DataTable();
                if (picks.Count > 0)
                {
                    var data = await ProjectService.GetPumpsViewList(picks.Select(pick => pick.ItemId).ToList());
                    if (data?.ItemPumpsView != null)
                        specs = JsonHelper.ToDataTable(data.ItemPumpsView);
                }

                foreach (var pick in picks)
                {
                    string id = pick.ItemId.ToString();
                    uc.SetFinalPumpData(PumpSpec(specs, id, "FLA"), PumpSpec(specs, id, "VOLTAGE"), pick.Model, id);
                }
            }
        }

        private static string PumpSpec(DataTable specs, string itemId, string title)
        {
            if (specs == null || specs.Rows.Count == 0) return string.Empty;

            return specs.AsEnumerable()
                .FirstOrDefault(row => row["item_title"].ToString() == title && row["item_id"].ToString() == itemId)?["item_value"]
                ?.ToString() ?? string.Empty;
        }

        // ---- TEMPLATE ------------------------------------------------------------------

        /// <summary>
        /// The TEMPLATE button: pick a project template and lay its components into the
        /// item table.
        /// </summary>
        public static async Task ApplyTemplateAsync(ItemSetUC uc)
        {
            if (uc == null) return;

            var lookups = await ItemSetLookups.Templates();
            DataTable templates = JsonHelper.ToDataTable(lookups.sales_project_template_child);

            using (var modal = new TemplateSelectionModal())
            {
                if (modal.ShowDialog() != DialogResult.OK) return;

                Dictionary<string, dynamic> result = modal.GetResult();
                if (result == null) return;

                result.TryGetValue("template_id", out dynamic id);
                var templateRows = Helpers.FilterDataTable(templates, id, "based_id");
                uc.SetProjectItemsData(templateRows, modal.GetTemplateName());
            }
        }

        // ---- STOCK CHECK ---------------------------------------------------------------

        /// <summary>
        /// One stock-check line per item-table row that has an item and a QTY, with the
        /// item's available stock and whatever this quotation already holds of it.
        ///
        /// stockCache and pendingReservations are the calling screen's own: Sales keeps
        /// both across opens (a reservation toggled but not yet saved has to show as such the
        /// next time). Either may be null.
        /// </summary>
        public static async Task<List<StockCheckRow>> BuildStockCheckRowsAsync(
            DataGridView dgv, int quotationId, DateTime? expiresAt,
            IDictionary<int, AvailableStockModel> stockCache,
            IDictionary<string, bool> pendingReservations)
        {
            var lines = new List<StockCheckRow>();
            if (dgv == null || !dgv.Columns.Contains("item_id")) return lines;

            if (stockCache == null) stockCache = new Dictionary<int, AvailableStockModel>();

            for (int i = 0; i < dgv.Rows.Count; i++)
            {
                var row = dgv.Rows[i];
                if (row.IsNewRow) continue;

                if (!int.TryParse(row.Cells["item_id"].Value?.ToString(), out int itemId) || itemId <= 0) continue;

                int.TryParse(row.Cells["project_items_qty"].Value?.ToString(), out int requiredQty);
                if (requiredQty <= 0) continue;

                // A line not saved yet has no id; the modal keeps it checkable but cannot
                // reserve against it until the quotation is saved.
                int.TryParse(row.Cells["project_items_id"].Value?.ToString(), out int lineId);
                string referenceCode = row.Cells["reference_code"].Value?.ToString();

                string itemName = dgv.Columns.Contains("project_items_components")
                    ? row.Cells["project_items_components"].Value?.ToString()
                    : null;

                AvailableStockModel stock;
                bool isReserved = false;
                int ownReservedQty = 0;
                try
                {
                    if (!stockCache.TryGetValue(itemId, out stock))
                    {
                        stock = await ItemStockCheckService.GetAvailableStock(itemId);
                        stockCache[itemId] = stock;
                    }

                    if (lineId > 0)
                    {
                        var reservation = await ItemStockCheckService.GetReservation(lineId, "sales_project_item");
                        isReserved = reservation != null;
                        if (reservation != null) ownReservedQty = reservation.qty;
                    }

                    if (pendingReservations != null && !string.IsNullOrEmpty(referenceCode) &&
                        pendingReservations.TryGetValue(referenceCode, out bool pendingState))
                    {
                        isReserved = pendingState;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to check stock for \"{itemName}\": {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    continue;
                }

                lines.Add(new StockCheckRow
                {
                    ItemId = itemId,
                    ItemName = itemName,
                    QuickId = lineId,
                    QuotationId = quotationId,
                    RequiredQty = requiredQty,
                    Stock = stock,
                    IsReserved = isReserved,
                    OwnReservedQty = ownReservedQty,
                    ExpiresAt = expiresAt,
                    ReferenceCode = referenceCode
                });
            }

            return lines;
        }

        /// <summary>
        /// The stock check without the reservation, for a screen whose user may not raise
        /// one (spec 10.4 gives that to sales). Same figures as Sales sees.
        /// </summary>
        public static async Task ShowStockCheckReadOnlyAsync(ItemSetUC uc, int quotationId)
        {
            if (uc == null) return;

            var lines = await BuildStockCheckRowsAsync(uc.DgvProjectItems, quotationId, null, null, null);
            if (lines.Count == 0)
            {
                MessageBox.Show("No line items with an item and a QTY yet.", "Nothing To Check", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using (var modal = new StockCheckModal(lines, null, allowReserve: false))
            {
                modal.ShowDialog();
            }
        }

        /// <summary>
        /// The stock check WITH the RESERVE column, for a screen that saves on its own - the
        /// Engineering app (user decision 2026-09-28: the engineer the quote was sent to may
        /// reserve too; spec 3.3, 10.4.1). What is ticked or unticked is kept in
        /// <paramref name="pending"/> - this tab's own, by reference code - and applied at once
        /// to lines that are already saved; a line not saved yet keeps its change until
        /// <see cref="ApplyPendingReservationsAsync"/> runs after the next save.
        /// </summary>
        public static async Task ShowStockCheckWithReserveAsync(ItemSetUC uc, int quotationId, DateTime? validUntil,
            IDictionary<string, bool> pending, ICollection<int> affectedItemIds = null)
        {
            if (uc == null || pending == null) return;

            var lines = await BuildStockCheckRowsAsync(uc.DgvProjectItems, quotationId, validUntil, null, pending);
            if (lines.Count == 0)
            {
                MessageBox.Show("No line items with an item and a QTY yet.", "Nothing To Check", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // A reservation always points at a saved quote (10.4.1).
            using (var modal = new StockCheckModal(lines, null, allowReserve: quotationId > 0))
            {
                modal.ShowDialog();
                foreach (var change in modal.PendingChangesByReferenceCode)
                    pending[change.Key] = change.Value;
            }

            List<string> failures = await ApplyPendingReservationsAsync(uc, pending, quotationId, validUntil, affectedItemIds);
            if (failures.Count > 0)
            {
                MessageBox.Show("Some reservation changes could not be applied:\n" + string.Join("\n", failures),
                    "Some Reservation Changes Failed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        /// <summary>
        /// Raises or releases the reservations ticked or unticked on this tab's stock check, for
        /// every line that has been saved (a line id is what a reservation points at). What is
        /// applied leaves <paramref name="pending"/>; a line not saved yet stays in it. Returns
        /// the lines that failed. Keyed per tab: reference codes repeat from tab to tab.
        /// <paramref name="affectedItemIds"/>, when given, collects the items whose reservations
        /// changed, so the caller can refresh the stock figures its OTHER tabs show for them.
        /// </summary>
        public static async Task<List<string>> ApplyPendingReservationsAsync(ItemSetUC uc, IDictionary<string, bool> pending,
            int quotationId, DateTime? validUntil, ICollection<int> affectedItemIds = null)
        {
            var failures = new List<string>();
            if (uc == null || pending == null || pending.Count == 0) return failures;

            var dgv = uc.DgvProjectItems;
            if (dgv == null || !dgv.Columns.Contains("reference_code")) return failures;

            var applied = new List<string>();
            var affected = new HashSet<int>();

            foreach (DataGridViewRow row in dgv.Rows)
            {
                if (row.IsNewRow) continue;

                string referenceCode = row.Cells["reference_code"].Value?.ToString();
                if (string.IsNullOrEmpty(referenceCode) || !pending.TryGetValue(referenceCode, out bool shouldReserve))
                    continue;

                int.TryParse(row.Cells["project_items_id"].Value?.ToString(), out int lineId);
                if (lineId <= 0) continue;   // not saved yet - stays pending

                int.TryParse(row.Cells["item_id"].Value?.ToString(), out int itemId);

                try
                {
                    if (shouldReserve)
                    {
                        decimal.TryParse(row.Cells["project_items_qty"].Value?.ToString(), out decimal qty);
                        if (itemId <= 0 || qty <= 0) { applied.Add(referenceCode); continue; }
                        await ItemStockCheckService.CreateReservation(itemId, (int)Math.Ceiling(qty), lineId, quotationId, validUntil, "sales_project_item");
                    }
                    else
                    {
                        await ItemStockCheckService.ReleaseReservation(lineId, "sales_project_item");
                    }

                    if (itemId > 0) affected.Add(itemId);
                    applied.Add(referenceCode);
                }
                catch (Exception ex)
                {
                    failures.Add($"{referenceCode}: {ex.Message}");
                }
            }

            foreach (string referenceCode in applied)
                pending.Remove(referenceCode);

            if (affected.Count > 0)
            {
                foreach (int itemId in affected)
                {
                    uc.ClearStockCache(itemId);
                    affectedItemIds?.Add(itemId);
                }
                uc.RefreshAllStockIndicators();
            }

            return failures;
        }
    }
}
