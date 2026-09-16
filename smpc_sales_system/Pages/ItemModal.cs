using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using smpc_app.Services.Helpers;
using smpc_inventory_app.Services.Setup.Model.Item;
using smpc_sales_app.Pages.Sales;
using smpc_sales_app.Services.Helpers;
using smpc_sales_app.Services.Sales;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace smpc_sales_app.Pages
{
    public partial class SalesItemModal : Form
    {

        // dtItem is no longer the list this modal shows - the server is (GetPickerNames,
        // one row per item name, 20 a page). It is kept as the caller's CACHE: the picked
        // row is merged into it on the way out, so Quotation/SalesItemGridEditor/
        // ProjectTemplate's GetItemData still finds the item by id exactly as before and
        // none of those call sites had to change.
        //
        // It used to be the source, filtered in memory with distinctByItemNameId - a
        // catalogue-wide GroupBy over all 3,707 items, which is precisely what cannot be
        // done on one page and why the dedup moved to the server.
        private DataTable dtItem;
        private DataTable bomHead;
        private DataTable bomDetails;
        // Removed: a `new Quotation()` field, unreferenced anywhere in this class - it
        // built an entire Quotation page every time the picker opened.
        int result;
        int bomResult;
        int itemId;

        private List<ItemPickerRow> _rows = new List<ItemPickerRow>();
        private string _search = "";
        private int _page = 1;
        private int _totalPages;
        private bool _isLoading;

        private readonly Panel pnl_pager = new Panel { Dock = DockStyle.Bottom, Height = 40, Padding = new Padding(8, 6, 8, 6) };
        private readonly Button btn_page_prev = new Button { Text = "<< PREV", Dock = DockStyle.Left, Width = 90 };
        private readonly Button btn_page_next = new Button { Text = "NEXT >>", Dock = DockStyle.Left, Width = 90 };
        private readonly Label lbl_page = new Label { Dock = DockStyle.Left, Width = 230, TextAlign = ContentAlignment.MiddleCenter };

        public SalesItemModal(DataTable Item)
        {
            InitializeComponent();
            this.dtItem = Item;
            BuildPager();
            SetupColumns();
        }

        public SalesItemModal(DataTable Item, DataTable BomHead, DataTable BomDetails)
        {
            InitializeComponent();
            this.dtItem = Item;
            this.bomHead = BomHead;
            this.bomDetails = BomDetails;
            BuildPager();
            SetupColumns();
        }

        // Built in code, not the Designer, so neither ItemModal.Designer.cs nor its .resx
        // is touched.
        private void BuildPager()
        {
            pnl_pager.Controls.Add(lbl_page);
            pnl_pager.Controls.Add(btn_page_next);
            pnl_pager.Controls.Add(btn_page_prev);
            Controls.Add(pnl_pager);
            pnl_pager.BringToFront();

            btn_page_prev.Click += async (s, e) => { if (_page > 1) await LoadPage(_page - 1); };
            btn_page_next.Click += async (s, e) => { if (_page < _totalPages) await LoadPage(_page + 1); };
        }

        private void SetupColumns()
        {
            dgv_itemList.AutoGenerateColumns = false;
            dgv_itemList.Columns.Clear();
            dgv_itemList.Columns.Add(new DataGridViewTextBoxColumn { Name = "item_code", DataPropertyName = "item_code", HeaderText = "ITEM CODE", FillWeight = 30 });
            dgv_itemList.Columns.Add(new DataGridViewTextBoxColumn { Name = "item_name", DataPropertyName = "item_name", HeaderText = "ITEM NAME", FillWeight = 50 });
            // Unbound: filled in CellFormatting from the row's bom_id, which replaces the
            // old IdentifyItemType scan over a locally-held BOM table.
            dgv_itemList.Columns.Add(new DataGridViewTextBoxColumn { Name = "Type", HeaderText = "TYPE", FillWeight = 20 });
            dgv_itemList.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgv_itemList.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgv_itemList.ReadOnly = true;
            dgv_itemList.AllowUserToAddRows = false;
            dgv_itemList.RowHeadersVisible = false;
        }

        private async Task LoadPage(int page)
        {
            if (_isLoading) return;
            _isLoading = true;

            Helpers.Loading.ShowLoading(this);
            try
            {
                var result = await ItemService.GetPickerNames(_search, page);

                _rows = result.Rows;
                dgv_itemList.DataSource = null;
                dgv_itemList.DataSource = _rows;

                _page = result.Pagination?.page ?? page;
                _totalPages = result.Pagination?.total_pages ?? 0;

                lbl_page.Text = _totalPages == 0
                    ? "No items found"
                    : $"Page {_page} of {_totalPages}  ({result.Pagination?.total ?? 0} items)";

                btn_page_prev.Enabled = result.Pagination?.has_prev ?? false;
                btn_page_next.Enabled = result.Pagination?.has_next ?? false;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to load the item list.\n" + ex.Message, "Item List",
                                MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                Helpers.Loading.HideLoading(this);
                _isLoading = false;
            }
        }

        public int GetResult()
        {
            return result;
        }
        public int GetBomResult()
        {
            return bomResult;
        }
        public int GetParentItemId()
        {
            return itemId;
        }

        public bool isBom { get; set; }

        public bool isItem { get; set; }

        private void dgv_itemList_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= dgv_itemList.Rows.Count)
                return;

            ItemPickerRow picked = dgv_itemList.Rows[e.RowIndex].DataBoundItem as ItemPickerRow;
            if (picked == null)
                return;

            // The caller looks the item up in its own table straight after this closes, and
            // with paging it may never have downloaded this one - so leave it there.
            picked.MergeInto(dtItem);

            this.itemId = picked.id;

            // bom_id comes from the server now (a TOP 1 over tbl_setup_item_bom), instead of
            // scanning a BOM table the client had to hold in full.
            if (picked.bom_id != 0)
            {
                this.bomResult = picked.bom_id;
                isBom = true;
            }
            else
            {
                isItem = true;
            }

            // Kept only so GetResult() still answers something sensible; both live callers
            // read GetParentItemId()/GetBomResult() and ignore this row index.
            this.result = dtItem != null ? dtItem.Rows.Count - 1 : e.RowIndex;

            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private async void ItemModal_Load(object sender, EventArgs e)
        {
            await LoadPage(1);
        }

        private async void btn_search_Click(object sender, EventArgs e)
        {
            // Searches the whole catalogue on the server rather than filtering the rows
            // this modal happens to be showing.
            _search = txt_specs.Text.Trim();
            await LoadPage(1);
        }

        private void dgv_itemList_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= _rows.Count)
                return;

            string column = dgv_itemList.Columns[e.ColumnIndex].Name;

            if (column == "item_code" && e.Value != null)
            {
                string modelValue = e.Value.ToString();
                if (!modelValue.StartsWith("I#"))
                {
                    e.Value = "I#" + modelValue;
                }
                e.FormattingApplied = true; // prevent re-formatting loop
            }
            else if (column == "Type")
            {
                e.Value = _rows[e.RowIndex].bom_id != 0 ? "BOM" : "SINGLE";
                e.FormattingApplied = true;
            }
        }
    }
}
