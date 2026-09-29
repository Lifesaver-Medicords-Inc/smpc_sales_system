using Newtonsoft.Json;
using smpc_sales_app.Pages.Sales;
using smpc_sales_app.Services.Helpers;
using smpc_sales_system.Models;
using smpc_sales_system.Services.Sales.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;

namespace smpc_sales_system.Pages.Sales
{
    /// <summary>
    /// Puts what the other app saved onto an open project quotation IN PLACE, field by field
    /// and row by row, around whatever the user here is doing - instead of tearing every tab
    /// down and building it again.
    ///
    /// "When update the advance conditions and items using sales and engineering it will not
    /// update possible the it keep refreshing. I think the websocket refresh happen per
    /// textbox, combobox and datagridview row?" (user, 2026-09-26). The rebuild it replaces
    /// waited until the user stopped typing, and then reloaded every tab - so an update could
    /// wait indefinitely, and when it came the page visibly reloaded.
    ///
    /// A three-way merge, per item set:
    ///   baseline - what this screen was built from (plus anything it has saved since)
    ///   fresh    - what is stored now
    ///   screen   - what a save from this screen would send now
    /// A field or row the OTHER app changed (baseline -> fresh) is put on screen unless the
    /// user here has changed that same field (baseline -> screen) and not saved it yet; then
    /// theirs stays, and their autosave sends it - the last one to save a field wins it, as in
    /// a shared spreadsheet. Both comparisons are the diff engine's own (DiffStoredTabs,
    /// DiffScreenTabs), so "what changed" means exactly what it means to a save.
    ///
    /// Not merged - the host rebuilds instead, as before: a tab added, removed or renamed. A
    /// grid with a cell open for typing is left alone for a moment (Deferred), since its
    /// content cannot be swapped under the editor.
    ///
    /// Everything is applied inside ItemSetUC.ApplyRemote, so the controls' change events do
    /// not read it as the user's edit and autosave it back.
    /// </summary>
    public static class ProjectLiveMerge
    {
        public sealed class Outcome
        {
            /// <summary>The shape changed (a tab added, removed or renamed): rebuild instead. Nothing was applied.</summary>
            public bool NeedsRebuild;

            /// <summary>The quotation header differs - the host decides whether that matters to it.</summary>
            public bool HeaderChanged;

            /// <summary>The PROJECT MULTIPLIERS differ.</summary>
            public bool MultipliersChanged;

            /// <summary>Item sets left alone because a grid cell there is open for typing. Try again shortly.</summary>
            public readonly List<int> Deferred = new List<int>();

            /// <summary>Tabs whose red flag (excluded) the other app changed, and to what.</summary>
            public readonly Dictionary<int, bool> Exclusions = new Dictionary<int, bool>();

            /// <summary>How many sections (panel or grid) were updated on screen.</summary>
            public int Applied;

            /// <summary>The host's baseline from now on.</summary>
            public SalesProjectList Baseline;
        }

        /// <param name="diffEngine">A Quotation - the page itself in Sales, a throwaway one in Engineering.</param>
        /// <param name="baseline">What the screen was built from, or what it saved from.</param>
        /// <param name="fresh">The stored project now.</param>
        /// <param name="screenPayload">Exactly what this screen's autosave would send now (with "id").</param>
        /// <param name="tabs">Each open tab's control, by item set id.</param>
        public static Outcome Apply(Quotation diffEngine, SalesProjectList baseline, SalesProjectList fresh,
            Dictionary<string, object> screenPayload, int quotationId, IDictionary<int, ItemSetUC> tabs)
        {
            var outcome = new Outcome { Baseline = baseline };
            if (fresh == null || diffEngine == null) return outcome;
            if (baseline == null)
            {
                outcome.NeedsRebuild = true;
                return outcome;
            }

            outcome.HeaderChanged = !SameJson(
                baseline.SalesQuotation?.FirstOrDefault(q => q.id == quotationId),
                fresh.SalesQuotation?.FirstOrDefault(q => q.id == quotationId));
            outcome.MultipliersChanged = !SameJson(Multipliers(baseline, quotationId), Multipliers(fresh, quotationId));

            Dictionary<int, Quotation.TabDiff> remote = diffEngine.DiffStoredTabs(baseline, fresh, quotationId);

            // The shape first: a tab added, removed or renamed is a rebuild, and then nothing
            // here is applied at all.
            foreach (var entry in remote)
            {
                if (!ChangesContent(entry.Value) && !entry.Value.SalesProjectItemSet.HasChanges()) continue;

                if (entry.Value.SalesProjectItemSet.HasChanges()
                    || !tabs.TryGetValue(entry.Key, out ItemSetUC open) || open == null || open.IsDisposed)
                {
                    outcome.NeedsRebuild = true;
                    return outcome;
                }
            }

            Dictionary<int, Quotation.TabDiff> local = diffEngine.DiffScreenTabs(baseline, screenPayload);

            foreach (var entry in remote)
            {
                Quotation.TabDiff theirs = entry.Value;
                if (!ChangesContent(theirs)) continue;

                ItemSetUC uc = tabs[entry.Key];

                if ((theirs.SalesProjectItems.HasChanges() && uc.IsItemsGridEditing)
                    || (theirs.SalesProjectWirings.HasChanges() && uc.IsWiringGridEditing)
                    || ((theirs.SalesProjectContent.HasChanges() || theirs.SalesProjectContentAdvancedCondition.HasChanges()) && DropdownOpen(uc)))
                {
                    outcome.Deferred.Add(entry.Key);
                    continue;
                }

                Quotation.TabDiff mine = local.TryGetValue(entry.Key, out var found) ? found : new Quotation.TabDiff();
                Quotation.TabDbData before = diffEngine.StoredTab(baseline, quotationId, entry.Key);
                Quotation.TabDbData stored = diffEngine.StoredTab(fresh, quotationId, entry.Key);
                Quotation.TabDbData screen = diffEngine.ScreenTab(screenPayload, entry.Key);

                FocusKeeper keeper = FocusKeeper.Capture(uc);
                try
                {
                    uc.ApplyRemote(() => ApplyTab(uc, entry.Key, theirs, mine, before, stored, screen, fresh, outcome));
                }
                finally
                {
                    keeper.Restore();
                }
            }

            outcome.Baseline = ComposeBaseline(fresh, baseline, outcome.Deferred);
            return outcome;
        }

        private static void ApplyTab(ItemSetUC uc, int itemSetId, Quotation.TabDiff theirs, Quotation.TabDiff mine,
            Quotation.TabDbData before, Quotation.TabDbData stored, Quotation.TabDbData screen, SalesProjectList fresh, Outcome outcome)
        {
            // ADVANCED CONDITIONS - one row, merged field by field.
            if (theirs.SalesProjectContentAdvancedCondition.HasChanges())
            {
                SalesProjectAdvancedConditions freshRow = stored.Conditions.FirstOrDefault();
                if (freshRow != null)
                {
                    SalesProjectAdvancedConditions merged = MergeSingle(freshRow, mine.SalesProjectContentAdvancedCondition);
                    uc.SetAdvancedPanelData(JsonHelper.ToDataTable(new List<SalesProjectAdvancedConditions> { merged }));
                    outcome.Applied++;
                }
            }

            // The item and wiring tables go before CLIENT NEEDS: the WIRING tick adds or takes
            // away the wiring block in the item table, and the table it acts on must already
            // be the merged one (as on load, SetWiring runs after SetFetchedItemData).
            if (theirs.SalesProjectItems.HasChanges())
            {
                List<SalesProjectItems> merged = MergeRows(screen.Items, theirs.SalesProjectItems, mine.SalesProjectItems,
                    i => i.items_id, (i, id) => i.items_id = id, i => i.reference_code);

                if (uc.selectedImageList != null && fresh.sales_project_items_selected_images != null)
                    uc.selectedImageList = JsonHelper.ToDataTable(fresh.sales_project_items_selected_images);

                uc.ApplyStoredItems(JsonHelper.ToDataTable(merged));
                outcome.Applied++;
            }

            if (theirs.SalesProjectWirings.HasChanges())
            {
                List<SalesWiringModel> merged = MergeRows(screen.Wiring, theirs.SalesProjectWirings, mine.SalesProjectWirings,
                    w => w.id, (w, id) => w.id = id, w => null);
                uc.ApplyStoredWiring(JsonHelper.ToDataTable(merged));
                outcome.Applied++;
            }

            // CLIENT NEEDS and what hangs off the content row: Size Up, FINAL, template, WIRING.
            if (theirs.SalesProjectContent.HasChanges())
            {
                SalesProjectContent freshRow = stored.Contents.FirstOrDefault();
                if (freshRow != null)
                {
                    HashSet<string> changedThere = ChangedKeys(theirs.SalesProjectContent);
                    HashSet<string> changedHere = ChangedKeys(mine.SalesProjectContent);
                    bool Take(string key) => changedThere.Contains(key) && !changedHere.Contains(key);

                    SalesProjectContent merged = MergeSingle(freshRow, mine.SalesProjectContent);
                    uc.SetContentsPanelData(JsonHelper.ToDataTable(new List<SalesProjectContent> { merged }));

                    // Size Up and FINAL are lists of pumps, merged pump by pump - not taken
                    // whole or left whole, as the fields are. Both apps pick pumps, and "theirs
                    // unless I changed it too" meant one app's pick was dropped whenever both
                    // had picked since the last save (2026-09-28). See MergePumps.
                    SalesProjectContent beforeRow = before?.Contents.FirstOrDefault();
                    SalesProjectContent screenRow = screen?.Contents.FirstOrDefault();

                    if (changedThere.Contains("size_up"))
                    {
                        List<SalesProjectSizeUp> sizeUps = MergePumps(
                            beforeRow?.sales_project_size_up, freshRow.sales_project_size_up, screenRow?.sales_project_size_up,
                            r => r.id, (r, id) => r.id = id, r => r.item_id, r => r.model, null);
                        uc.SetSizeUpData(JsonHelper.ToDataTable(sizeUps));
                    }
                    if (changedThere.Contains("final_selection"))
                    {
                        List<SalesProjectContentFinal> finals = MergePumps(
                            beforeRow?.sales_project_content_final, freshRow.sales_project_content_final, screenRow?.sales_project_content_final,
                            r => r.id, (r, id) => r.id = id, r => r.item_id, r => r.final,
                            (row, theirsRow, beforeRowOfIt) =>
                            {
                                // FLA / VOLTAGE edited there and not here: theirs.
                                if (beforeRowOfIt != null && row.fla == beforeRowOfIt.fla && row.voltage == beforeRowOfIt.voltage)
                                {
                                    row.fla = theirsRow.fla;
                                    row.voltage = theirsRow.voltage;
                                }
                            });
                        uc.SetFinalData(JsonHelper.ToDataTable(finals));
                    }
                    if (Take("template_project_id"))
                        uc.ShowStoredTemplate(freshRow.template_project_id.ToString());
                    if (Take("is_wiring"))
                        uc.SetWiring(freshRow.is_wiring.ToString().ToLowerInvariant());
                    if (Take("is_excluded"))
                        outcome.Exclusions[itemSetId] = freshRow.IsExcluded;

                    outcome.Applied++;
                }
            }
        }

        // ---- Merging --------------------------------------------------------------------

        // The stored row, with every field the user here has changed and not saved kept as
        // they have it.
        private static T MergeSingle<T>(T fresh, Quotation.ModelUpdateDiff<T> mine) where T : class
        {
            T merged = Clone(fresh);
            foreach (var entry in mine.Updated)
            {
                foreach (string key in entry.Changes.Keys)
                    CopyField(entry.Item, merged, key);
            }
            return merged;
        }

        // The rows on screen, with the other app's changes applied to them: its new rows added
        // (in reference-code order), its deleted rows taken away, its edited fields written -
        // except any row or field the user here has changed and not saved, which stays theirs.
        private static List<T> MergeRows<T>(List<T> screen, Quotation.ModelUpdateDiff<T> theirs, Quotation.ModelUpdateDiff<T> mine,
            Func<T, int> key, Action<T, int> setKey, Func<T, string> referenceCode) where T : class
        {
            var result = new List<T>(screen ?? new List<T>());

            var byKey = new Dictionary<int, T>();
            foreach (T row in result)
            {
                int k = key(row);
                if (k > 0 && !byKey.ContainsKey(k)) byKey[k] = row;
            }

            var mineByKey = new Dictionary<int, HashSet<string>>();
            foreach (var entry in mine.Updated)
            {
                int k = key(entry.Item);
                if (k <= 0) continue;
                if (!mineByKey.TryGetValue(k, out var fields)) mineByKey[k] = fields = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                fields.UnionWith(entry.Changes.Keys);
            }

            foreach (T gone in theirs.Removed)
            {
                int k = key(gone);
                if (k <= 0 || !byKey.TryGetValue(k, out T row)) continue;

                if (!mineByKey.ContainsKey(k))
                {
                    result.Remove(row);
                }
                else
                {
                    // Deleted there while being edited here: the edit keeps the row, as a NEW
                    // row. Its old id is gone from the database, and a row keeping it would be
                    // re-inserted under a new id and then shown twice - once as this screen's,
                    // once as the server's. At 0 the next save inserts it once, and the merge
                    // after that save gives it the id the server issued (AdoptServerIds).
                    setKey(row, 0);
                }
                byKey.Remove(k);
            }

            foreach (var entry in theirs.Updated)
            {
                int k = key(entry.Item);
                if (!byKey.TryGetValue(k, out T row)) continue;     // gone here: the user removed it

                mineByKey.TryGetValue(k, out var myFields);
                foreach (string field in entry.Changes.Keys)
                {
                    if (myFields == null || !myFields.Contains(field))
                        CopyField(entry.Item, row, field);
                }
            }

            foreach (T added in theirs.Added)
            {
                int k = key(added);
                if (k > 0 && byKey.ContainsKey(k)) continue;        // already here: this screen's own new row

                InsertByReference(result, Clone(added), referenceCode);
                if (k > 0) byKey[k] = added;
            }

            return result;
        }

        // A Size Up or FINAL list after the other app's save, pump by pump:
        //   before - the list as this screen last read it
        //   fresh  - the list as stored now
        //   screen - the list on this screen now
        // A pump on screen stays unless the other app took it off (it was read here and is no
        // longer stored); one only on screen is this screen's own pick, not saved yet, and
        // stays. A stored pump this screen does not show is the other app's pick and is added -
        // unless this screen had it and took it off, which its next save sends. A pump on both
        // takes the stored row's id: that is how this screen's own pick learns the id it was
        // given, so its next save does not insert it again. takeTheirs(row, stored, before)
        // may copy the other app's edits onto the row.
        //
        // The same pump is the same item id where both rows have one, else the same model -
        // the rule the server's matchStoredPump uses.
        internal static List<T> MergePumps<T>(IEnumerable<T> before, IEnumerable<T> fresh, IEnumerable<T> screen,
            Func<T, int> id, Action<T, int> setId, Func<T, int> itemId, Func<T, string> model,
            Action<T, T, T> takeTheirs) where T : class
        {
            List<T> beforeRows = (before ?? Enumerable.Empty<T>()).Where(r => r != null).ToList();
            List<T> freshRows = (fresh ?? Enumerable.Empty<T>()).Where(r => r != null).ToList();

            bool SamePump(T a, T b)
            {
                int ia = itemId(a), ib = itemId(b);
                if (ia > 0 && ib > 0) return ia == ib;
                string ma = model(a)?.Trim(), mb = model(b)?.Trim();
                return !string.IsNullOrEmpty(ma) && string.Equals(ma, mb, StringComparison.OrdinalIgnoreCase);
            }

            T Find(List<T> rows, T row, ICollection<T> taken)
            {
                int rowId = id(row);
                if (rowId > 0)
                {
                    T byId = rows.FirstOrDefault(r => id(r) == rowId && (taken == null || !taken.Contains(r)));
                    if (byId != null) return byId;
                }
                return rows.FirstOrDefault(r => (taken == null || !taken.Contains(r)) && SamePump(r, row));
            }

            var result = new List<T>();
            var matched = new List<T>();

            foreach (T row in (screen ?? Enumerable.Empty<T>()).Where(r => r != null))
            {
                T stored = Find(freshRows, row, matched);
                T wasRead = Find(beforeRows, row, null);

                if (stored == null)
                {
                    if (wasRead == null) result.Add(Clone(row));   // picked here, not saved yet
                    continue;                                       // else: taken off there
                }

                matched.Add(stored);
                T merged = Clone(row);
                if (id(stored) > 0) setId(merged, id(stored));
                takeTheirs?.Invoke(merged, stored, wasRead);
                result.Add(merged);
            }

            foreach (T stored in freshRows)
            {
                if (matched.Contains(stored)) continue;
                if (Find(beforeRows, stored, null) != null) continue;   // taken off here, not saved yet
                result.Add(Clone(stored));                              // picked there
            }

            return result;
        }

        // Before the first row whose reference code comes after this one's; at the end when it
        // has none. 3.1 lands between 3 and 3.2, not after the last row.
        private static void InsertByReference<T>(List<T> rows, T row, Func<T, string> referenceCode)
        {
            string code = referenceCode(row);
            if (!string.IsNullOrWhiteSpace(code))
            {
                for (int i = 0; i < rows.Count; i++)
                {
                    string other = referenceCode(rows[i]);
                    if (!string.IsNullOrWhiteSpace(other) && CompareReference(other, code) > 0)
                    {
                        rows.Insert(i, row);
                        return;
                    }
                }
            }
            rows.Add(row);
        }

        private static int CompareReference(string a, string b)
        {
            int[] Parts(string s) => s.Split('.').Select(p => int.TryParse(p, out int n) ? n : 0).ToArray();
            int[] x = Parts(a), y = Parts(b);
            for (int i = 0; i < Math.Max(x.Length, y.Length); i++)
            {
                int xv = i < x.Length ? x[i] : 0, yv = i < y.Length ? y[i] : 0;
                if (xv != yv) return xv.CompareTo(yv);
            }
            return x.Length.CompareTo(y.Length);
        }

        // A change key names a model property - except the two child lists, which the
        // comparers report under a readable name of their own.
        private static readonly Dictionary<string, string> PropertyForKey = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "size_up", "sales_project_size_up" },
            { "final_selection", "sales_project_content_final" },
        };

        private static void CopyField(object from, object to, string key)
        {
            if (from == null || to == null || string.IsNullOrEmpty(key)) return;

            string name = PropertyForKey.TryGetValue(key, out string mapped) ? mapped : key;
            PropertyInfo property = to.GetType().GetProperty(name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            if (property == null || !property.CanRead || !property.CanWrite) return;

            property.SetValue(to, property.GetValue(from));
        }

        private static HashSet<string> ChangedKeys<T>(Quotation.ModelUpdateDiff<T> diff)
        {
            return new HashSet<string>(diff.Updated.SelectMany(u => u.Changes.Keys), StringComparer.OrdinalIgnoreCase);
        }

        // A dropdown the user has open: rebinding the panel under it would close it or change
        // what it shows mid-pick.
        private static bool DropdownOpen(ItemSetUC uc)
        {
            Control active = uc.ActiveControl;
            while (active is ContainerControl container && container.ActiveControl != null)
                active = container.ActiveControl;
            return active is ComboBox combo && combo.DroppedDown;
        }

        private static bool ChangesContent(Quotation.TabDiff diff)
        {
            return diff.SalesProjectContent.HasChanges()
                || diff.SalesProjectContentAdvancedCondition.HasChanges()
                || diff.SalesProjectItems.HasChanges()
                || diff.SalesProjectWirings.HasChanges();
        }

        // ---- The baseline --------------------------------------------------------------------

        // The stored project - except the item sets that were left alone, which keep what the
        // screen still shows. Taking those from fresh would tell the next save that the screen
        // already has the other app's change there, and that save would undo it.
        private static SalesProjectList ComposeBaseline(SalesProjectList fresh, SalesProjectList old, ICollection<int> keepOld)
        {
            if (keepOld == null || keepOld.Count == 0) return fresh;

            SalesProjectList composed = Clone(fresh);
            List<T> Swap<T>(List<T> newRows, List<T> oldRows, Func<T, int> itemSet) =>
                (newRows ?? new List<T>()).Where(r => !keepOld.Contains(itemSet(r)))
                    .Concat((oldRows ?? new List<T>()).Where(r => keepOld.Contains(itemSet(r))))
                    .ToList();

            composed.sales_project_content = Swap(composed.sales_project_content, old.sales_project_content, r => r.based_id);
            composed.sales_project_content_advanced_condition = Swap(composed.sales_project_content_advanced_condition, old.sales_project_content_advanced_condition, r => r.based_id);
            composed.sales_project_items = Swap(composed.sales_project_items, old.sales_project_items, r => r.based_id);
            composed.sales_project_wiring = Swap(composed.sales_project_wiring, old.sales_project_wiring, r => r.based_id);
            return composed;
        }

        // Whether two PROJECT MULTIPLIERS lists hold the same rows in the same order - what the
        // save's DiffByIndex compares. A screen whose list still equals what it was built from
        // has not been edited there, so the other app's change can be shown on it; one that
        // differs keeps its own, and its next save sends it (last to save wins, as with the
        // header). Both apps may edit the list since 2026-09-28.
        public static bool SameMultipliers(IEnumerable<SalesProjectMultiplier> a, IEnumerable<SalesProjectMultiplier> b)
        {
            string Row(SalesProjectMultiplier m) => string.Join("\u001f",
                (m?.brand ?? "").Trim(), (m?.component ?? "").Trim(), (m?.description ?? "").Trim(), (m?.multiplier ?? "").Trim());

            List<string> x = (a ?? Enumerable.Empty<SalesProjectMultiplier>()).Select(Row).ToList();
            List<string> y = (b ?? Enumerable.Empty<SalesProjectMultiplier>()).Select(Row).ToList();
            return x.SequenceEqual(y);
        }

        public static List<SalesProjectMultiplier> StoredMultipliers(SalesProjectList data, int quotationId)
        {
            return (data?.sales_project_multiplier ?? new List<SalesProjectMultiplier>())
                .Where(m => m.based_id == quotationId)
                .ToList();
        }

        private static List<SalesProjectMultiplier> Multipliers(SalesProjectList data, int quotationId)
        {
            return (data?.sales_project_multiplier ?? new List<SalesProjectMultiplier>())
                .Where(m => m.based_id == quotationId)
                .OrderBy(m => m.multiplier_id)
                .ToList();
        }

        private static T Clone<T>(T value) where T : class
        {
            return value == null ? null : JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(value));
        }

        private static bool SameJson(object a, object b)
        {
            return JsonConvert.SerializeObject(a) == JsonConvert.SerializeObject(b);
        }

        // ---- The user's place --------------------------------------------------------------

        // Which field had focus inside the tab, and where the caret was in it. Rebinding a
        // panel re-sets every text box - an unchanged one included - and a re-set text box
        // puts its caret at the start: someone typing would find their next keystroke landing
        // at the front of the field.
        private sealed class FocusKeeper
        {
            private Control _focused;
            private int _selectionStart = -1;
            private int _selectionLength;

            public static FocusKeeper Capture(ItemSetUC uc)
            {
                var keeper = new FocusKeeper();
                Control active = uc.ActiveControl;
                while (active is ContainerControl container && container.ActiveControl != null)
                    active = container.ActiveControl;

                keeper._focused = active;
                if (active is TextBoxBase box)
                {
                    keeper._selectionStart = box.SelectionStart;
                    keeper._selectionLength = box.SelectionLength;
                }
                return keeper;
            }

            public void Restore()
            {
                if (_focused == null || _focused.IsDisposed) return;

                try
                {
                    if (!_focused.Focused && _focused.CanFocus && _focused.FindForm()?.ActiveControl != null)
                        _focused.Focus();

                    if (_focused is TextBoxBase box && _selectionStart >= 0)
                    {
                        int start = Math.Min(_selectionStart, box.TextLength);
                        box.Select(start, Math.Min(_selectionLength, box.TextLength - start));
                    }
                }
                catch (InvalidOperationException)
                {
                    // A control mid-layout keeps where the rebind left it.
                }
            }
        }
    }
}
