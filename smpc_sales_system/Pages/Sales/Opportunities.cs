using smpc_app.Services.Helpers;
using System.Linq;
using smpc_sales_app.Pages.Sales;
using smpc_sales_app.Services.Helpers;
using smpc_sales_app.Services.Sales;
using smpc_sales_system.Services.Sales;
using smpc_sales_system.Services.Sales.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;

using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace smpc_sales_system.Pages.Sales
{
    public partial class Opportunities : UserControl
    {
        public Opportunities()
        {
            InitializeComponent();
        }
        public DataTable transactionList { get; set; } = new DataTable();
        public DataTable opportunities { get; set; } = new DataTable();
        private async void fetchQuotationDetails()
        {
            SalesQuotationList data = await QuotationService.GetQuotations();

            transactionList = JsonHelper.ToDataTable(data.SalesQuotation);
            //childList = JsonHelper.ToDataTable(data.SalesQuotationQuick);

            if (data != null)
            {
                bindQuotation(true);
            }
        }

        private void bindQuotation(bool isBind = false)
        {
            if (isBind)
            {
                DataView dataview = new DataView(this.transactionList);

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

                dgv_sales_opportunities.DataSource = dataview;

                // Hide other columns if they exist
                foreach (DataGridViewColumn column in dgv_sales_opportunities.Columns)
                {
                    if (column.Name != "document_no" && column.Name != "customer_name" && column.Name != "date" &&
                        column.Name != "tag" && column.Name != "project_name" && column.Name != "client_req" &&
                        column.Name != "value" && column.Name != "last_update" && column.Name != "stage" &&
                        column.Name != "status" && column.Name != "special_deal")
                    {
                        column.Visible = false;
                    }
                }
            }
        }

        private void dgv_sales_opportunities_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                string documentNo = dgv_sales_opportunities.Rows[e.RowIndex].Cells["document_no"].Value.ToString();
                if (documentNo.StartsWith("Q#"))
                {
                    documentNo = documentNo.Substring(2);
                }
                Quotation quotationPage = new Quotation(documentNo);

                this.Parent.Controls.Add(quotationPage);

                this.Hide();
            }
        }
        private async void Opportunities_Load(object sender, EventArgs e)
        {
            fetchQuotationDetails();
        }

    }
}
