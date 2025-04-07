using smpc_app.Services.Helpers;
using smpc_sales_app.Services.Helpers;
using smpc_sales_app.Services.Sales;
using smpc_sales_system.Services.Sales.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace smpc_sales_app.Pages
{
    public partial class SetupModal : Form
    {
        private DataTable Dt { get; set; }
        public string SetupTitle { get; set; }
        int result;
        public SetupModal(string setupTitle, DataTable dt)
        {
            InitializeComponent();
            lbl_setup_title.Text = setupTitle;
            this.Dt = dt;
        }

        public SetupModal()
        {
            InitializeComponent();
        }

        private void btn_save_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        public DataTable transactionList { get; set; } = new DataTable();
        private async void fetchQuotationDetails()
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

                foreach (DataRow row in this.transactionList.Rows)
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
                    if (column.Name != "d_document_no" && column.Name != "d_customer_name" && column.Name != "d_version_no")
                    {
                        column.Visible = false;
                    }
                }
            }
        }

      
        private void SetupModal_Load(object sender, EventArgs e)
        {
            fetchQuotationDetails();
        }
        public int GetResult()
        {
            return result;
        }
        private void dgv_application_setup_CellContentDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                this.result = e.RowIndex;
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
        }

        private void dgv_application_setup_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
        //    if (e.RowIndex >= 0)
        //    {
        //        this.result = e.RowIndex;
        //        this.DialogResult = DialogResult.OK;
        //        this.Close();
        //    }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            string searchval = txt_search.Text.ToString();
            var data = Helpers.FilterDataTable(Dt, searchval, "document_no", "customer_id");
            dgv_application_setup.DataSource = data;
        }
    }
}
