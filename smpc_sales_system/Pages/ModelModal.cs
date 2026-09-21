using smpc_app.Services.Helpers;
using smpc_inventory_app.Pages;
using smpc_inventory_app.Pages.Engineering.Boq;
using smpc_sales_app.Data;
using smpc_sales_app.Pages;
using smpc_sales_app.Services.Sales;
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
    public partial class ModelModal : Form
    {
        // _ItemData is the caller's table, kept as a CACHE rather than the source: the
        // models shown come from the server (GetPickerModels - every item sharing this
        // item's item_name_id, 20 a page), and the picked row is merged back in so
        // Quotation's GetItemData still finds it by id. That filter used to run in memory
        // over the page's whole ItemList, which is why it had to move server-side.
        private DataTable _ItemData, _BomHead, _BomDetail;
        public event Action<String> CreateSuccess;
        int itemId = 0, bomId = 0, id = 0;
        bool isBom = false;

        private List<ItemPickerRow> _rows = new List<ItemPickerRow>();
        private string _search = "";
        private int _page = 1;
        private int _totalPages;
        private bool _isLoading;
        private readonly Timer _typingTimer = new Timer { Interval = 350 };

        private readonly Panel pnl_pager = new Panel { Dock = DockStyle.Bottom, Height = 40, Padding = new Padding(8, 6, 8, 6) };
        private readonly Button btn_page_prev = new Button { Text = "<< PREV", Dock = DockStyle.Left, Width = 90 };
        private readonly Button btn_page_next = new Button { Text = "NEXT >>", Dock = DockStyle.Left, Width = 90 };
        // Fills whatever the two buttons leave, so a message longer than the old fixed 230px
        // (the "item is not in the catalogue" one) is readable instead of cut off. It is added
        // to the panel before the buttons, so docking gives them their width first.
        private readonly Label lbl_page = new Label { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter, AutoEllipsis = true };

        public ModelModal(DataTable Item, string Id)
        {
            InitializeComponent();

            id = int.Parse(Id);
            _ItemData = Item;

            Initialize();
        }

        public ModelModal(DataTable Item, DataTable BomHead, DataTable BomDetails, string Id)
        {
            InitializeComponent();

            id = int.Parse(Id);
            _ItemData = Item;
            _BomHead = BomHead;
            _BomDetail = BomDetails;

            Initialize();
        }

        private void Initialize()
        {
            SetupColumns();
            BuildPager();

            // The fetch is a round trip now, so it runs on Load rather than in the
            // constructor - a constructor cannot await, and the handle does not exist yet.
            _typingTimer.Tick += async (s, e) => { _typingTimer.Stop(); await LoadPage(1); };
            Load += async (s, e) => await LoadPage(1);
            // Not a Dispose(bool) override - ModelModal.Designer.cs already declares one,
            // and a second would be a duplicate member.
            FormClosed += (s, e) => { _typingTimer.Stop(); _typingTimer.Dispose(); };
        }

        private void SetupColumns()
        {
            DataGridViewModel.AutoGenerateColumns = false;
            DataGridViewModel.Columns.Clear();
            DataGridViewModel.Columns.Add(new DataGridViewTextBoxColumn { Name = "item_code", DataPropertyName = "item_code", HeaderText = "ITEM CODE", FillWeight = 34 });
            DataGridViewModel.Columns.Add(new DataGridViewTextBoxColumn { Name = "item_model", DataPropertyName = "item_model", HeaderText = "ITEM MODEL", FillWeight = 46 });
            DataGridViewModel.Columns.Add(new DataGridViewTextBoxColumn { Name = "Type", HeaderText = "TYPE", FillWeight = 20 });
            DataGridViewModel.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            DataGridViewModel.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            DataGridViewModel.ReadOnly = true;
            DataGridViewModel.AllowUserToAddRows = false;
            DataGridViewModel.RowHeadersVisible = false;
            DataGridViewModel.CellFormatting += DataGridViewModel_CellFormatting;
        }

        // Added in code so ModelModal.Designer.cs and its .resx stay untouched.
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

        private async Task LoadPage(int page)
        {
            if (_isLoading) return;
            _isLoading = true;

            Helpers.Loading.ShowLoading(this);
            try
            {
                var result = await ItemService.GetPickerModels(id, _search, page);

                _rows = result.Rows;
                DataGridViewModel.DataSource = null;
                DataGridViewModel.DataSource = _rows;

                _page = result.Pagination?.page ?? page;
                _totalPages = result.Pagination?.total_pages ?? 0;

                // A refused request is not an empty list. The one that actually happens is a
                // row whose item is no longer in the catalogue - a project template keeps the
                // item id it was built from, and those do not survive a rebuilt database - and
                // it used to read as "this component has no models at all".
                if (!result.Success && !string.IsNullOrWhiteSpace(result.Message))
                    lbl_page.Text = result.Message;
                else
                    lbl_page.Text = _totalPages == 0
                        ? "No models found"
                        : $"Page {_page} of {_totalPages}  ({result.Pagination?.total ?? 0} models)";

                btn_page_prev.Enabled = result.Pagination?.has_prev ?? false;
                btn_page_next.Enabled = result.Pagination?.has_next ?? false;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to load the model list.\n" + ex.Message, "Model List",
                                MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                Helpers.Loading.HideLoading(this);
                _isLoading = false;
            }
        }

        private void txt_search_TextChanged(object sender, EventArgs e)
        {
            _search = txt_search.Text.Trim();
            _typingTimer.Stop();
            _typingTimer.Start();
        }

        private void DataGridViewModel_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= _rows.Count)
                return;

            if (DataGridViewModel.Columns[e.ColumnIndex].Name == "Type")
            {
                e.Value = _rows[e.RowIndex].bom_id != 0 ? "BOM" : "SINGLE";
                e.FormattingApplied = true;
            }
        }

        private void DataGridViewModel_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= DataGridViewModel.Rows.Count)
                return;

            ItemPickerRow picked = DataGridViewModel.Rows[e.RowIndex].DataBoundItem as ItemPickerRow;
            if (picked == null || picked.id == 0)
            {
                MessageBox.Show("Invalid selection. Please select a valid item.");
                return;
            }

            // Same reason as SalesItemModal: the caller resolves this id against its own
            // table immediately afterwards and may never have downloaded this model.
            picked.MergeInto(_ItemData);

            itemId = picked.id;
            // Was a scan of the locally-held BOM head table, which also threw when
            // _BomHead was null (the two-argument constructor never sets it).
            bomId = picked.bom_id;
            isBom = picked.bom_id != 0;

            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        public int GetItemId()
        {
            return itemId;
        }
        public int GetBomId()
        {
            return bomId;
        }
        public bool IsBom()
        {
            return isBom;
        }
    }
}
