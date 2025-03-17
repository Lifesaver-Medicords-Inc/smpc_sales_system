using smpc_app.Services.Helpers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace smpc_sales_system.Pages
{
    public partial class SearchOrder : Form
    {
        private DataTable Dt { get; set; }
        public string SetupTitle { get; set; }
        int result;
        public SearchOrder(string setupTitle, DataTable dt)
        {
            InitializeComponent();
            lbl_setup_title.Text = setupTitle;
            this.Dt = dt;
        }

        public SearchOrder()
        {
            InitializeComponent();
        }

        private void btn_save_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        public DataTable OrderList { get; set; } = new DataTable();
        private async void fetchOrder()
        {
            var data = Dt;

            if (data != null)
            {
                bindQuotation(true);
            }
        }

        private void bindQuotation(bool isBind = false)
        {
            if (isBind)
            {
                DataView dataview = new DataView(Dt);

                foreach (DataRow row in this.OrderList.Rows)
                {
                    if (row["document_no"] != DBNull.Value)
                    {
                        string documentNo = row["document_no"].ToString();

                        if (!documentNo.StartsWith("Q#"))
                        {
                            row["document_no"] = "Q#" + documentNo;
                        }
                    }
                }

                dgv_application_setup.DataSource = dataview;

                //de other columns if they exist
                foreach (DataGridViewColumn column in dgv_application_setup.Columns)
                {
                    if (column.Name != "d_document_no" && column.Name != "d_quotation" && column.Name != "d_status" && column.Name != "d_quote_ref")
                    {
                        column.Visible = false;
                    }
                }
            }
        }


        private void SearchOrder_Load(object sender, EventArgs e)
        {
            fetchOrder();
        }
        public int GetResult()
        {
            return result;
        }

        private void dgv_application_setup_CellContentDoubleClick_1(object sender, DataGridViewCellEventArgs e)
        {
            if (e.ColumnIndex >= 0)
            {
                this.result = e.RowIndex;
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
        }
        private void txt_search_TextChanged(object sender, EventArgs e)
        {
            string searchval = txt_search.Text.ToString();
            var data = Helpers.FilterDataTable(Dt, searchval, "document_no", "status", "doc", "quotation_id");
            dgv_application_setup.DataSource = data;
        }
    }
}

