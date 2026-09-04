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
        /// (Re)fetches the item and BOM catalogs. Each table is only replaced when its
        /// fetch actually returned something, so a failed call leaves the previous
        /// catalog in place instead of emptying the picker.
        /// </summary>
        public async Task LoadCatalogsAsync()
        {
            var itemData = await smpc_sales_app.Services.Sales.ItemService.GetItem();
            var bomData = await ProjectService.GetBom();
            var companyData = await CompanyService.GetAsDatatable();

            if (itemData != null)
                ItemList = JsonHelper.ToDataTable(itemData.items);

            if (bomData != null)
            {
                BomHead = JsonHelper.ToDataTable(bomData.bom_head);
                BomDetails = JsonHelper.ToDataTable(bomData.bom_details);
            }

            if (companyData != null)
                Company = companyData;
        }

        /// <summary>
        /// Opens the item/BOM picker and inserts whatever was chosen at rowIndex.
        /// </summary>
        public void HandleItemSelectionClick(int rowIndex, DataGridView dgv)
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
            }
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

            foreach (DataRow row in itemList.Rows)
            {
                DataRow newRow = dataSource.NewRow();
                if (dataSource.Columns.Contains("unit_of_measure"))
                    newRow["unit_of_measure"] = row["unit_of_measure"];

                reference = (reference != null) ? reference : (counter != null) ? counter : CounterReference.ToString();

                newRow["item_id"] = row["id"];
                newRow["model"] = row["item_model"];
                newRow["components"] = new string(' ', level * 4) + row["item_name"];
                newRow["reference_code"] = reference;

                dataSource.Rows.InsertAt(newRow, rowIndex);

                // Bug #089: this used dataSource.Rows.Count - 1 as "the row we just
                // added", but InsertAt puts it AT rowIndex and shifts the rest down, so
                // the styling/stock check landed on an unrelated row.
                int addedRowIndex = rowIndex;
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

        private DataRow GetCompanyRow()
        {
            if (Company == null || Company.Rows.Count == 0) return null;
            return Company.AsEnumerable().FirstOrDefault(r => r.Field<int>("Id") == 1);
        }
    }
}
