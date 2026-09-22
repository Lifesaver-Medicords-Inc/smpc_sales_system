using smpc_app.Services.Helpers;
using smpc_sales_app.Services.Helpers;
using System;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace smpc_sales_app.Pages.Sales.Modal
{
    // Server-side search picker shared by Quick Quote and Project search:
    // term box + 20-row result pages + pager, mirroring the item picker's
    // SalesItemModal. One page crosses the VPN per navigation step instead of
    // the whole header list. The caller opens the picked record through the
    // headers' `at` (double-click returns the header id, -1 when cancelled).
    //
    // fetch(term, page) is supplied by the caller (quick vs project search)
    // and answers (rows, hasPrev, hasNext, pageText). Rows must carry at least
    // id/document_no/version_no/sub_version_no/date - extra columns are shown
    // as-is if present, ignored otherwise.
    public partial class QuotationSearchModal : Form
    {
        public delegate Task<SearchFetchResult> SearchFetch(string term, int page);

        public class SearchFetchResult
        {
            public DataTable Rows { get; set; } = new DataTable();
            public bool HasPrev { get; set; }
            public bool HasNext { get; set; }
            public string PageText { get; set; } = "";
        }

        private readonly SearchFetch _fetch;
        private string _term = "";
        private int _page = 1;
        private bool _loading;
        private int _pickedId = -1;

        private readonly TextBox _txtTerm = new TextBox { Dock = DockStyle.Fill };
        private readonly Button _btnSearch = new Button { Text = "Search", Dock = DockStyle.Right, Width = 90 };
        private readonly DataGridView _grid = new DataGridView { Dock = DockStyle.Fill };
        private readonly Panel _pnlTop = new Panel { Dock = DockStyle.Top, Height = 34, Padding = new Padding(8, 5, 8, 5) };
        private readonly Panel _pnlPager = new Panel { Dock = DockStyle.Bottom, Height = 40, Padding = new Padding(8, 6, 8, 6) };
        private readonly Button _btnPrev = new Button { Text = "<< PREV", Dock = DockStyle.Left, Width = 90 };
        private readonly Button _btnNext = new Button { Text = "NEXT >>", Dock = DockStyle.Left, Width = 90 };
        private readonly Label _lblPage = new Label { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter };

        private static readonly string[] VisibleColumns =
            { "document_no", "version_no", "sub_version_no", "project_name", "date", "created_by" };

        public QuotationSearchModal(string title, SearchFetch fetch)
        {
            Text = title;
            StartPosition = FormStartPosition.CenterParent;
            Size = new Size(640, 480);
            MinimizeBox = false;
            MaximizeBox = false;

            _fetch = fetch;

            _pnlTop.Controls.Add(_txtTerm);
            _pnlTop.Controls.Add(_btnSearch);
            _pnlPager.Controls.Add(_lblPage);
            _pnlPager.Controls.Add(_btnNext);
            _pnlPager.Controls.Add(_btnPrev);
            Controls.Add(_grid);
            Controls.Add(_pnlPager);
            Controls.Add(_pnlTop);

            _grid.ReadOnly = true;
            _grid.AllowUserToAddRows = false;
            _grid.RowHeadersVisible = false;
            _grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            _grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            _btnSearch.Click += async (s, e) => { _term = _txtTerm.Text.Trim(); await LoadPage(1); };
            _txtTerm.KeyDown += async (s, e) => {
                if (e.KeyCode == Keys.Enter) { _term = _txtTerm.Text.Trim(); await LoadPage(1); e.Handled = true; e.SuppressKeyPress = true; }
            };
            _btnPrev.Click += async (s, e) => { if (_page > 1) await LoadPage(_page - 1); };
            _btnNext.Click += async (s, e) => { await LoadPage(_page + 1); };
            _grid.CellDoubleClick += Grid_Pick;

            _lblPage.Text = "Type a document number and press Search.";
        }

        public int GetPickedId()
        {
            return _pickedId;
        }

        private void Grid_Pick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= _grid.Rows.Count)
                return;
            if (!_grid.Columns.Contains("id"))
                return;

            object val = _grid.Rows[e.RowIndex].Cells["id"].Value;
            int id;
            if (val != null && int.TryParse(val.ToString(), out id))
            {
                _pickedId = id;
                DialogResult = DialogResult.OK;
                Close();
            }
        }

        private async Task LoadPage(int page)
        {
            if (_loading || _fetch == null) return;
            if (string.IsNullOrWhiteSpace(_term))
            {
                _lblPage.Text = "Type a document number and press Search.";
                return;
            }
            _loading = true;
            Helpers.Loading.ShowLoading(this);
            try
            {
                SearchFetchResult result = await _fetch(_term, page);
                _page = page;

                DataTable rows = result != null && result.Rows != null ? result.Rows : new DataTable();
                _grid.DataSource = null;
                _grid.DataSource = rows;
                ApplyColumnVisibility();

                _lblPage.Text = result != null ? result.PageText : "";
                _btnPrev.Enabled = result != null && result.HasPrev;
                _btnNext.Enabled = result != null && result.HasNext;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Search failed." + Environment.NewLine + ex.Message,
                    "Search", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                Helpers.Loading.HideLoading(this);
                _loading = false;
            }
        }

        private void ApplyColumnVisibility()
        {
            if (_grid.Columns.Count == 0) return;
            foreach (DataGridViewColumn col in _grid.Columns)
                col.Visible = VisibleColumns.Contains(col.Name);
            if (_grid.Columns.Contains("id"))
                _grid.Columns["id"].Visible = false;
        }
    }
}
