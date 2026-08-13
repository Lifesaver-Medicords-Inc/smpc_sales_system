using smpc_app.Services.Helpers;
using smpc_inventory_app.Pages.Item;
using smpc_sales_app.Services.Helpers;
using smpc_sales_system.Models;
using smpc_sales_system.Services.Sales;
using smpc_sales_system.Services.Sales.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace smpc_sales_system.Pages.Sales
{
    public partial class frm_canvas_modal : Form
    {

        string items_id { get; set; }
        DataTable bpi { get; set; }
        DataTable bpi_items { get; set; }

        // Every supplier set up for this item, name-resolved, that ISN'T already a row on
        // the sheet - the candidate list ADD SUPPLIER hands to SupplierPickerModal. Kept
        // in sync as rows get added so re-opening the picker doesn't offer a duplicate.
        private List<EligibleSupplier> _eligibleSuppliers = new List<EligibleSupplier>();

        public frm_canvas_modal(string item_id, DataTable dt, DataTable dt2)
        {
            InitializeComponent();
            this.items_id = item_id;
            this.bpi = dt; // was dropped entirely before - needed to resolve a supplier's actual name below
            this.bpi_items = dt2;
            fetchBpiSuppliers();
        }

        private async void fetchCanvasSheet()
        {

        }

        // A supplier's real name lives in bpi (general BPI info), keyed by
        // general_based_id - BpiSuppliers only carries based_id (that same key) and a
        // supplier_code, which is why the grid used to show "S#0008" instead of an actual
        // name. Falls back to the code, then a placeholder, if bpi wasn't passed in or
        // doesn't have this supplier (e.g. Quotation.cs's bpi_general is customer-focused
        // and may not always include every supplier).
        private string ResolveSupplierName(int basedId, string fallbackCode)
        {
            if (bpi != null && bpi.Columns.Contains("general_based_id") && bpi.Columns.Contains("branch_name"))
            {
                DataRow[] rows = bpi.Select($"general_based_id = {basedId}");
                if (rows.Length > 0 && !string.IsNullOrWhiteSpace(rows[0]["branch_name"]?.ToString()))
                {
                    return rows[0]["branch_name"].ToString();
                }
            }

            return string.IsNullOrWhiteSpace(fallbackCode) ? "(unnamed supplier)" : fallbackCode;
        }

        private async void fetchBpiSuppliers()
        {
            var data = await ProjectService.GetSuppliers();
            // data comes back null (not an empty list) when the request itself failed -
            // RequestToApi's shared catch already popped a MessageBox for that, so just
            // bail rather than NullReferenceException-ing on data.BpiSuppliers next.
            if (data == null) return;

            List<BpiSuppliers> suppliersList = data.BpiSuppliers;

            //var view_data = await ProjectService.GetCanvasView();
            //List<SalesCanvasView> viewList = view_data.sales_canvas_sheet_view;

            // Was "if (suppliersList == null || !suppliersList.Any())" - inverted, so the
            // one case this could ever run was when suppliersList was already empty/null,
            // and it would NullReferenceException calling .Where on a null list. Filtering
            // only makes sense once there's something to filter.
            if (suppliersList != null && suppliersList.Any())
            {
                // Rows already on the sheet - this runs again after ADD SUPPLIER closes
                // (see btn_add_bpi_Click), so without this every already-added row would
                // get duplicated alongside its fresh copy from this re-fetch.
                var alreadyOnSheet = new HashSet<int>();
                foreach (DataGridViewRow row in dataGridView1.Rows)
                {
                    if (row.IsNewRow) continue;
                    if (int.TryParse(row.Cells["supplier_id"].Value?.ToString(), out int existingId))
                    {
                        alreadyOnSheet.Add(existingId);
                    }
                }

                var eligible = suppliersList
                    .Where(s => s.item_id.ToString() == this.items_id && !alreadyOnSheet.Contains(s.based_id))
                    .Select(s => new EligibleSupplier
                    {
                        SupplierId = s.based_id,
                        SupplierName = ResolveSupplierName(s.based_id, s.supplier_code)
                    })
                    .ToList();

                _eligibleSuppliers = eligible;

                // Grid is populated manually (not via DataSource) so ADD SUPPLIER's
                // counterpart (SupplierPickerModal) could add rows the same way if it's
                // ever wired back in - a data-bound DataGridView can't take Rows.Add()
                // calls.
                //
                // Iterating a snapshot (.ToList()), not "eligible" itself - AddSupplierRow
                // calls _eligibleSuppliers.RemoveAll(...), and _eligibleSuppliers IS
                // eligible (same list, assigned just above), so mutating it while this
                // foreach is still enumerating it threw "Collection was modified".
                foreach (var supplier in eligible.ToList())
                {
                    AddSupplierRow(supplier);
                }
            }
        }

        // Shared by the initial fetch above and ADD SUPPLIER - appends one row and takes
        // it out of the "still eligible to add" pool so the picker won't offer the same
        // supplier twice.
        private void AddSupplierRow(EligibleSupplier supplier)
        {
            int rowIndex = dataGridView1.Rows.Add();
            var row = dataGridView1.Rows[rowIndex];
            row.Cells["supplier_id"].Value = supplier.SupplierId;
            row.Cells["Column1"].Value = supplier.SupplierName;

            _eligibleSuppliers.RemoveAll(s => s.SupplierId == supplier.SupplierId);
        }

        // Goes straight to the Business Partner module to register a supplier there,
        // rather than picking from ones already tied to this item (that's what
        // SupplierPickerModal was for - still here, just not wired to this button
        // anymore, in case a "pick an existing one" entry point is wanted elsewhere
        // later). Passing items_id as canvassForm is what lets BusinessPartnerInfo offer
        // "Temporary Supplier" as an entity type in there - the quick, no-formal-
        // onboarding option that fits adding someone just to get a canvass quote from.
        //
        // Doesn't add anything to the sheet automatically on close - BusinessPartnerInfo
        // is a full add/edit screen with no "return what I just created" contract, so
        // there's nothing here to grab. Re-running fetchBpiSuppliers() at least means a
        // supplier who got tied to this item while that screen was open (or on a previous
        // visit) will show up next time - see class remarks on ADD SUPPLIER's counterpart
        // if that's later brought back for this button.
        private void btn_add_bpi_Click(object sender, EventArgs e)
        {
            var modal = new BusnessPartnerInfoModal(items_id);
            modal.StartPosition = FormStartPosition.CenterParent;
            modal.ShowDialog();

            fetchBpiSuppliers();
        }

            private void dataGridView1_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            computeLoop(e.RowIndex);
        }

        private void computeLoop(int rowIndex)
        {
            double net_price, unit_price;
            double discount;

            DataGridViewRow row = this.dataGridView1.Rows[rowIndex];

           
            object netPriceValue = row.Cells["NetPrice"].Value;
            object discountValue = row.Cells["Discount"].Value;

            Console.WriteLine($"Row {rowIndex} | Net Price: {netPriceValue} | Discount: {discountValue}");

            bool isNetPriceValid = double.TryParse(netPriceValue?.ToString(), out net_price);
            bool isDiscountValid = double.TryParse(discountValue?.ToString(), out discount);

            if (isNetPriceValid && isDiscountValid)
            {
                discount = discount / 100;
                unit_price = net_price * (1 - discount);
                row.Cells["UnitPrice"].Value = unit_price;
            }
            else
            {
                row.Cells["UnitPrice"].Value = net_price;
            }
        }

        public Dictionary<string, object> GetDGVData()
        {
            var source = Helpers.ConvertDataGridViewToDataTable(dataGridView1);
            List<SalesCanvasModel> canvas = new List<SalesCanvasModel>();

            foreach (DataRow item in source.Rows)
            {
                if (item == null) continue;

                int.TryParse(item["supplier_id"]?.ToString(), out int supplierId);
                int.TryParse(items_id, out int itemId);
                decimal.TryParse(item["NetPrice"]?.ToString(), out decimal netPrice);
                decimal.TryParse(item["UnitPrice"]?.ToString(), out decimal unitPrice);
                int.TryParse(item["LeadTime"]?.ToString(), out int leadTime);

                var canvasdata = new SalesCanvasModel
                {
                    supplier_based_id = supplierId,
                    item_based_id = itemId,
                    net_price = netPrice,
                    discount = item["Discount"]?.ToString() ?? string.Empty,
                    unit_price = unitPrice,
                    validity = item["validity_col"]?.ToString(),
                    lead_time = leadTime
                };

                // **Exclude entries where net_price or unit_price is 0**
                if (canvasdata.net_price > 0 && canvasdata.unit_price > 0)
                {
                    canvas.Add(canvasdata);
                }
            }

            Dictionary<string, dynamic> data = new Dictionary<string, dynamic>();
            data["SalesCanvasSheet"] = canvas;
            return data;
        }

        private async void btn_save_Click(object sender, EventArgs e)
        {
            var response = await ProjectService.InsertCanvas(GetDGVData());

            if (response.Success)
            {
                MessageBox.Show("Success");
            }
        }
    }
}
