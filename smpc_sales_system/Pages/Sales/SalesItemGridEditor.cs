using smpc_app.Services.Helpers;
using smpc_sales_app.Pages;
using smpc_sales_app.Services.Helpers;
using smpc_sales_system.Services.Sales;
using smpc_sales_system.Services.Setup;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace smpc_sales_system.Pages.Sales
{
    /// <summary>
    /// Adds items and BOM trees to a sales item grid.
    ///
    /// Extracted from Quotation.cs on 2026-09-04. It lived there as seven private
    /// methods, which meant the Engineering app's Sales Quotation screen - which uses
    /// the same ItemSetUC and the same grid - had no way to reach it, so an engineer
    /// could not add an accessory at all (the page never even subscribed to
    /// ItemSetUC.CellClicked, because there was nothing to point it at).
    ///
    /// It does NOT live on ItemSetUC, despite that being the control both apps share.
    /// The quick quote grid (dgv_quick_quote_details) sits directly on the Quotation
    /// form and has no ItemSetUC, yet it calls HandleItemSelectionClick and
    /// GetMaxTopLevelReferenceCode too - so putting this in the control would have
    /// forced the quick-quote path through a control that isn't there. Every method
    /// here already took its target DataGridView as a parameter, so the logic was
    /// grid-agnostic all along; it was only ever in the wrong class.
    ///
    /// Quotation keeps its own private methods as one-line delegates to an instance of
    /// this class, so all of its existing call sites are untouched and the two apps
    /// cannot drift apart.
    /// </summary>
    public class SalesItemGridEditor
    {
        // Catalogs. The caller owns these and re-assigns them whenever it refetches;
        // they are read on every call rather than cached so a refresh takes effect.
        public DataTable ItemList { get; set; } = new DataTable();
        public DataTable BomHead { get; set; } = new DataTable();
        public DataTable BomDetails { get; set; } = new DataTable();
        public DataTable Company { get; set; } = new DataTable();

        /// <summary>
        /// Project quote vs quick quote. Only affects the guard in GetItemData, which
        /// tolerates a null DataSource on a project grid but not on a quick-quote one.
        /// </summary>
        public bool IsProject { get; set; } = true;

        /// <summary>
        /// Called after a row is added so the caller can populate its stock indicator.
        /// Optional - Engineering has no INV. column to fill, and a null here simply
        /// skips it rather than forcing that page to supply a no-op.
        /// </summary>
        public Action<int, DataGridView> RefreshStockIndicator { get; set; }

        // Reference-code counters. Public because Quotation drives them directly from
        // its own call sites (counterParent = 1 before a BOM insert, and so on); its
        // fields are now properties that proxy straight to these, so that code did not
        // have to change.
        public int CounterReference { get; set; } = 0;
        public int CounterParent { get; set; } = 1;

        // The tables items fetched one at a time are merged into (see
        // ItemCatalogTables.EnsureGridItemsAsync). Quotation feeds its own in, so a pick here
        // lands in the same tables the rest of that form reads; Engineering keeps these.
        public DataTable ItemAdditionalSpecs { get; set; } = new DataTable();
        public DataTable ImageList { get; set; } = new DataTable();
        public HashSet<int> LoadedItemDetailIds { get; set; } = new HashSet<int>();

        /// <summary>
        /// Builds an editor with its catalogs already fetched.
        ///
        /// This exists for the Engineering app. ItemService, ProjectService,
        /// CompanyService and JsonHelper are all internal to the Sales assembly, so
        /// Engineering cannot load the catalogs itself - and widening four service
        /// classes to public just to let one page fetch three tables is the wrong trade.
        /// This class already lives in the Sales assembly, so it can do the fetch on the
        /// caller's behalf and hand back something ready to use.
        ///
        /// Quotation does NOT use this - it already fetches these tables for its own
        /// screen and feeds them in through the properties, so making it fetch them a
        /// second time would double the API calls on every quotation load.
        /// </summary>
        public static async Task<SalesItemGridEditor> CreateLoadedAsync(bool isProject = true)
        {
            var editor = new SalesItemGridEditor { IsProject = isProject };
            await editor.LoadCatalogsAsync();
            return editor;
        }

        /// <summary>
        /// Readies the editor's tables, the way the Sales quotation screen readies its own:
        /// item rows merge in one item at a time as they are picked (the pickers page on the
        /// server and hand back the row picked) and BOMs are fetched as they are expanded
        /// (BomCatalog). Only Company Setup is read here.
        ///
        /// This used to download the whole item catalogue (GET /setup/item: every item with
        /// every spec, image, purchasing and inventory row) and the whole BOM catalogue (GET
        /// /setup/bom: 32.6 MB of JSON on test_fresh) each time Engineering opened a quotation
        /// (user request 2026-09-26: "optimize the BOM ... also update the engineering side").
        /// </summary>
        public async Task LoadCatalogsAsync()
        {
            if (ItemList == null || ItemList.Columns.Count == 0)
                ItemList = ItemCatalogTables.NewItemTable();
            if (ItemAdditionalSpecs == null || ItemAdditionalSpecs.Columns.Count == 0)
                ItemAdditionalSpecs = ItemCatalogTables.NewItemSpecsTable();
            if (ImageList == null || ImageList.Columns.Count == 0)
                ImageList = ItemCatalogTables.NewItemImageTable();

            BomHead = BomCatalog.Head;
            BomDetails = BomCatalog.Details;

            var companyData = await CompanyService.GetAsDatatable();
            if (companyData != null)
                Company = companyData;
        }

        /// <summary>
        /// Opens the item/BOM picker and inserts whatever was chosen at rowIndex. A BOM is
        /// fetched - with its sub-BOMs - before it is expanded, and the rows it adds get their
        /// items' details (UOM) afterwards.
        /// </summary>
        public async Task HandleItemSelectionClickAsync(int rowIndex, DataGridView dgv)
        {
            // Was counterReference++ - a running counter that only ever goes up, so it
            // drifted away from what's actually on the grid the moment anything got
            // deleted/renumbered. Recompute from the grid's real current max every time.
            CounterReference = GetMaxTopLevelReferenceCode(dgv) + 1;

            SalesItemModal itemModal = new SalesItemModal(ItemList, BomHead, BomDetails);
            DialogResult r = itemModal.ShowDialog();

            if (r != DialogResult.OK)
                return;

            int itemid = itemModal.GetParentItemId();

            if (itemModal.isBom)
            {
                int bomID = itemModal.GetBomResult();
                if (!await BomCatalog.EnsureAsync(bomID))
                {
                    MessageBox.Show("The BOM could not be loaded. Please try again in a moment.",
                                    "BOM", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                GetBomDataRecursive(rowIndex, bomID, itemid, dgv);
                CounterParent = 1;
            }
            else if (itemModal.isItem)
            {
                GetItemData(rowIndex, itemid, dgv, null);
            }
            else
            {
                MessageBox.Show("Invalid selection. The chosen item could not be matched to an Item or BOM.",
                                "Invalid Selection",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Error);
                return;
            }

            await ItemCatalogTables.EnsureGridItemsAsync(ItemList, ItemAdditionalSpecs, ImageList, LoadedItemDetailIds, dgv);
        }

        /// <summary>
        /// The MODEL cell: swap the row's component for another model of the same item, or
        /// for a BOM of it. The row and everything under it make way for the new pick, which
        /// keeps the row's reference code.
        ///
        /// Moved from Quotation.cs (HandleModelSelectionClick) on 2026-09-26. The Engineering
        /// app had to answer the same click with "isn't available from this screen yet"
        /// because the logic lived out of its reach; QA found the MODEL could not be changed
        /// there, which is not meant to be the case.
        /// </summary>
        public async Task HandleModelSelectionClickAsync(int rowIndex, DataGridView dgv)
        {
            if (dgv == null || rowIndex < 0 || rowIndex >= dgv.Rows.Count) return;

            string id = dgv.Rows[rowIndex].Cells["item_id"].Value?.ToString();

            // No component on the row yet - passing a blank id to ModelModal's constructor
            // is what threw the raw "Input string was not in a correct format".
            if (string.IsNullOrWhiteSpace(id) || id == "0")
            {
                MessageBox.Show(
                    "It doesn't have a component, that's why it can't select any models. Please select a component first.",
                    "No Component Selected", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int itemId;
            int bomId;
            using (var modal = new ModelModal(ItemList, BomHead, BomDetails, id))
            {
                if (modal.ShowDialog() != DialogResult.OK) return;

                itemId = modal.GetItemId();
                bomId = modal.GetBomId();
            }

            // Read after the modal: nothing on the grid moves while it is open, but the row
            // is about to be removed, and with it the code the new pick has to keep.
            string referenceCode = dgv.Rows[rowIndex].Cells["reference_code"].Value?.ToString();

            // Only a grid bound straight to a DataTable: GetItemData and GetBomDataRecursive
            // insert into that and nothing else, so on any other binding the row would be
            // removed below with nothing put back in its place.
            if (!(dgv.DataSource is DataTable)) return;

            // Fetched before the row is cleared, so a failed fetch leaves the row as it was.
            if (bomId != 0 && !await BomCatalog.EnsureAsync(bomId))
            {
                MessageBox.Show("The BOM could not be loaded. Please try again in a moment.",
                                "BOM", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            DeleteRowsByReferenceCode(rowIndex, dgv);

            if (bomId != 0)
            {
                GetBomDataRecursive(rowIndex, bomId, itemId, dgv, referenceCode);
                CounterParent = 1;
            }
            else
            {
                GetItemData(rowIndex, itemId, dgv, referenceCode);
            }

            // Same post-fill as the item picker path: BOM children resolve here so their UOM
            // cells don't stay blank.
            await ItemCatalogTables.EnsureGridItemsAsync(ItemList, ItemAdditionalSpecs, ImageList, LoadedItemDetailIds, dgv);
        }

        /// <summary>
        /// Removes the row at rowIndex and every row under it (reference codes "3" and
        /// "3.x"). Works on a grid bound to a DataTable or to a DataView of one.
        /// </summary>
        public void DeleteRowsByReferenceCode(int rowIndex, DataGridView dgv)
        {
            string referenceCode = dgv.Rows[rowIndex].Cells["reference_code"].Value?.ToString();
            if (string.IsNullOrEmpty(referenceCode)) return;

            DataTable dataSource = dgv.DataSource as DataTable ?? (dgv.DataSource as DataView)?.Table;
            if (dataSource == null || !dataSource.Columns.Contains("reference_code")) return;

            var rowsToDelete = dataSource.Rows.Cast<DataRow>()
                .Where(row =>
                {
                    string code = row["reference_code"]?.ToString();
                    return !string.IsNullOrEmpty(code) && (code == referenceCode || code.StartsWith(referenceCode + "."));
                })
                .ToList();

            foreach (var row in rowsToDelete)
                dataSource.Rows.Remove(row);
        }

        /// <summary>
        /// Highest top-level reference_code already on the grid (for "1", "2", "3.1"
        /// this returns 3), so numbering continues instead of restarting.
        /// </summary>
        public int GetMaxTopLevelReferenceCode(DataGridView dgv)
        {
            int max = 0;

            // Viewing/editing an existing quotation binds this grid to a DataView, not a
            // DataTable, which "is DataTable" doesn't match - that silently made this
            // always return 0 on an existing quotation, so newly added items collided
            // with codes already in use.
            DataTable dataSource = dgv?.DataSource as DataTable ?? (dgv?.DataSource as DataView)?.Table;
            if (dataSource == null || !dataSource.Columns.Contains("reference_code"))
                return max;

            foreach (DataRow row in dataSource.Rows)
            {
                string value = row["reference_code"]?.ToString();
                if (string.IsNullOrWhiteSpace(value))
                    continue;

                // Only the part before the first "." is the top-level item number.
                string topLevelPart = value.Split('.')[0];
                if (int.TryParse(topLevelPart, out int num) && num > max)
                    max = num;
            }

            return max;
        }

        /// <summary>
        /// Inserts a BOM and its whole subtree at rowIndex. Returns the subtree's cost.
        /// </summary>
        public decimal GetBomDataRecursive(int rowIndex, int bomID, int itemID, DataGridView dgv,
                                           string additionalReference = null, int level = 0,
                                           HashSet<int> visited = null)
        {
            Dictionary<int, DataRow> bomHeadDict = new Dictionary<int, DataRow>();
            Dictionary<int, List<DataRow>> bomChildDict = new Dictionary<int, List<DataRow>>();

            if (BomHead != null && BomHead.Rows.Count > 0)
                bomHeadDict = BomHead.AsEnumerable().ToDictionary(r => r.Field<int>("id"));

            if (BomDetails != null && BomDetails.Rows.Count > 0)
            {
                bomChildDict = BomDetails.AsEnumerable()
                    .GroupBy(r => r.Field<int>("item_bom_id"))
                    .ToDictionary(g => g.Key, g => g.ToList());
            }

            if (visited == null)
                visited = new HashSet<int>();

            if (visited.Contains(bomID))
                return 0;
            visited.Add(bomID);

            if (CounterParent == 1)
                CounterParent = CounterReference;

            if (string.IsNullOrEmpty(additionalReference))
                additionalReference = CounterParent.ToString();

            if (level == 0)
            {
                string[] arrayReference = additionalReference.Split('.');
                level = arrayReference.Length - 1;
            }

            string ParentLevel = new string(' ', level * 4);

            DataTable dataSource = dgv.DataSource as DataTable;
            if (dataSource == null)
                return 0;

            if (!bomHeadDict.TryGetValue(bomID, out DataRow parentRow))
                return 0;

            decimal manDays = Convert.ToDecimal(parentRow["man_days"]);
            decimal laborRate = Convert.ToDecimal(parentRow["labor_rate"]);
            decimal laborCost = manDays * laborRate;

            decimal totalCost = laborCost;

            DataRow newParent = dataSource.NewRow();
            newParent["reference_code"] = additionalReference;
            newParent["bom_id"] = parentRow["id"];
            newParent["item_id"] = parentRow["item_id"];
            newParent["components"] = ParentLevel + parentRow["general_name"];
            newParent["model"] = parentRow["item_model"];
            newParent["qty"] = parentRow["production_qty"];
            newParent["man_days"] = parentRow["man_days"];
            newParent["labor_rate"] = parentRow["labor_rate"];
            newParent["unit_price"] = Convert.ToDecimal(parentRow["production_cost"]);
            if (dataSource.Columns.Contains("unit_of_measure"))
                newParent["unit_of_measure"] = ResolveItemUom(Convert.ToInt32(parentRow["item_id"]));

            dataSource.Rows.InsertAt(newParent, rowIndex);

            int insertIndex = rowIndex + 1; // children go after the parent

            level++;

            if (!bomChildDict.TryGetValue(bomID, out List<DataRow> childRows))
                return 0;

            int counterSub = 1;
            foreach (DataRow child in childRows)
            {
                int childItemId = Convert.ToInt32(child["item_id"]);

                DataRow subBomRow = bomHeadDict.Values.FirstOrDefault(r => r.Field<int>("item_id") == childItemId);

                if (subBomRow != null)
                {
                    int subBomId = Convert.ToInt32(subBomRow["id"]);

                    decimal subTotal = GetBomDataRecursive(insertIndex, subBomId, childItemId, dgv,
                                                           $"{additionalReference}.{counterSub}", level, visited);
                    totalCost += subTotal;

                    // Skip past everything the recursion just inserted.
                    int subtreeRows = CountRowsByReference(dataSource, $"{additionalReference}.{counterSub}");
                    insertIndex += subtreeRows;
                }
                else
                {
                    decimal unitPrice = Convert.ToDecimal(child["unit_price"]);
                    decimal qty = Convert.ToDecimal(child["bom_qty"]);
                    totalCost += unitPrice * qty;

                    DataRow newChild = dataSource.NewRow();
                    newChild["bom_id"] = child["item_bom_id"];
                    newChild["item_id"] = childItemId;
                    newChild["components"] = new string(' ', level * 4) + child["item_name"];
                    newChild["model"] = child["size"];
                    newChild["qty"] = qty;
                    newChild["unit_price"] = unitPrice.ToString();
                    newChild["reference_code"] = $"{additionalReference}.{counterSub}";
                    if (dataSource.Columns.Contains("unit_of_measure"))
                        newChild["unit_of_measure"] = ResolveItemUom(childItemId);

                    dataSource.Rows.InsertAt(newChild, insertIndex);
                    dgv.Rows[insertIndex].ReadOnly = true;

                    // QTY stays editable even on a BOM leaf row (user decision,
                    // 2026-09-05): "we should be able to edit the qty when BOM is
                    // exist... it give the user free will to decide how many will be
                    // need for the quote". The row above is still locked structurally -
                    // components/model/reference_code come from the BOM template and
                    // shouldn't be hand-edited - but a cell's own ReadOnly overrides its
                    // row's in WinForms' resolution order, so reopening just this one
                    // cell leaves everything else on the row locked. Found by
                    // DataPropertyName rather than column Name, since the name differs
                    // between the two grids this runs against ("project_items_qty" vs
                    // "quick_qty") while the property both map to ("qty") does not.
                    DataGridViewColumn qtyColumn = dgv.Columns.Cast<DataGridViewColumn>()
                        .FirstOrDefault(c => c.DataPropertyName == "qty");
                    if (qtyColumn != null)
                        dgv.Rows[insertIndex].Cells[qtyColumn.Index].ReadOnly = false;

                    insertIndex++;
                }

                counterSub++;
            }

            // Parent price is the whole subtree plus the company markup. This was once a
            // hardcoded * 1.186m commented as "18% VAT" - it is a markup figure, not VAT.
            decimal TotalCostWithMarkup = totalCost * GetCompanyMarkupMultiplier();
            dataSource.Rows[rowIndex]["unit_price"] = TotalCostWithMarkup.ToString();

            CounterParent++;
            return totalCost;
        }

        /// <summary>
        /// Inserts a single (non-BOM) item at rowIndex.
        /// If a reference (parent reference_code) is provided, the new item is inserted
        /// at the bottom of existing children of that parent, not at the clicked row.
        /// </summary>
        public void GetItemData(int rowIndex, int itemID, DataGridView dgv, string reference, string counter = null)
        {
            DataTable itemList = Helpers.FilterExactDataTable(ItemList, itemID.ToString(), "id");

            int level = 0;
            if (reference != null)
                level = reference.Split('.').Length - 1;

            if (itemList.Rows.Count == 0)
            {
                MessageBox.Show("Invalid selection. Item not found.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            DataTable dataSource = dgv.DataSource as DataTable;
            if (!IsProject && dataSource == null) return;

            // If a reference (parent reference_code) is provided, find the correct insertion point
            // at the bottom of existing children of that parent, not at the clicked rowIndex.
            int insertIndex = rowIndex;
            if (reference != null && dataSource != null)
            {
                int lastChildIndex = FindLastChildIndex(dataSource, reference);
                if (lastChildIndex >= 0)
                {
                    insertIndex = lastChildIndex + 1;
                }
            }

            foreach (DataRow row in itemList.Rows)
            {
                DataRow newRow = dataSource.NewRow();
                if (dataSource.Columns.Contains("unit_of_measure"))
                    newRow["unit_of_measure"] = row["unit_of_measure"];

                string refCode = (reference != null) ? reference : (counter != null) ? counter : CounterReference.ToString();

                newRow["item_id"] = row["id"];
                newRow["model"] = row["item_model"];
                newRow["components"] = new string(' ', level * 4) + row["item_name"];
                newRow["reference_code"] = reference ?? refCode;

                dataSource.Rows.InsertAt(newRow, insertIndex);

                // Bug #089: this used dataSource.Rows.Count - 1 as "the row we just
                // added", but InsertAt puts it AT rowIndex and shifts the rest down, so
                // the styling/stock check landed on an unrelated row.
                int addedRowIndex = insertIndex;
                Helpers.SalesItemRowStyler.ApplyStyle(dgv, addedRowIndex, "single");

                RefreshStockIndicator?.Invoke(addedRowIndex, dgv);
            }
        }

        // Counts rows whose reference_code starts with the given prefix - used to work
        // out how many rows a recursive insert actually produced.
        private int CountRowsByReference(DataTable dt, string referencePrefix)
        {
            int count = 0;
            foreach (DataRow row in dt.Rows)
            {
                var refCode = row.Table.Columns.Contains("reference_code") ? row["reference_code"]?.ToString() : null;
                if (!string.IsNullOrEmpty(refCode) && refCode.StartsWith(referencePrefix))
                    count++;
            }
            return count;
        }

        /// <summary>
        /// Finds the index of the last child row of a given parent reference_code.
        /// Returns -1 if no children found.
        /// </summary>
        private int FindLastChildIndex(DataTable dt, string parentRef)
        {
            if (string.IsNullOrEmpty(parentRef) || dt == null)
                return -1;

            string childPrefix = parentRef + ".";
            int lastIndex = -1;

            for (int i = 0; i < dt.Rows.Count; i++)
            {
                var refCode = dt.Rows[i].Table.Columns.Contains("reference_code") ? dt.Rows[i]["reference_code"]?.ToString() : null;
                if (!string.IsNullOrEmpty(refCode) && refCode.StartsWith(childPrefix))
                {
                    // Check if it's a direct child (one level deeper) not a grandchild
                    string remainder = refCode.Substring(parentRef.Length + 1);
                    if (!remainder.Contains("."))
                    {
                        lastIndex = i;
                    }
                }
            }

            return lastIndex;
        }

        private string ResolveItemUom(int itemId)
        {
            if (itemId <= 0 || ItemList == null || ItemList.Rows.Count == 0)
                return "";

            DataTable match = Helpers.FilterExactDataTable(ItemList, itemId.ToString(), "id");
            if (match == null || match.Rows.Count == 0 || !match.Columns.Contains("unit_of_measure"))
                return "";

            return match.Rows[0]["unit_of_measure"]?.ToString() ?? "";
        }

        private decimal GetCompanyMarkupMultiplier()
        {
            DataRow row = GetCompanyRow();
            if (row != null)
            {
                decimal value = Convert.ToDecimal(row["MarkUpMultiplierPrice"]);
                if (value > 0) return value;
            }
            return 1.186m;
        }

        /// <summary>
        /// Company Setup's price multiplier and VAT rate (a fraction: 0.12), the two figures
        /// ItemSetUC prices lines with. Sales hands its own to every tab; this is so the
        /// Engineering app can hand over the same ones instead of leaving the control on its
        /// built-in 1.186 and 12% - prices on the two screens have to agree.
        /// </summary>
        public decimal CompanyMarkupMultiplier => GetCompanyMarkupMultiplier();

        public decimal CompanyVatRate
        {
            get
            {
                DataRow row = GetCompanyRow();
                if (row != null && row.Table.Columns.Contains("VatRatePercent"))
                {
                    decimal percent = Convert.ToDecimal(row["VatRatePercent"]);
                    if (percent > 0) return percent / 100m;
                }
                return 0.12m;
            }
        }

        private DataRow GetCompanyRow()
        {
            if (Company == null || Company.Rows.Count == 0) return null;
            return Company.AsEnumerable().FirstOrDefault(r => r.Field<int>("Id") == 1);
        }
    }
}
