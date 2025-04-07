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

namespace smpc_sales_system.Pages.Sales
{
    public partial class ItemImageModal : Form
    {
        public ItemImageModal(DataTable dt, DataTable dtIMage)
        {
            InitializeComponent();
            this.images = dtIMage;
            this.items = dt;
        }
        DataTable images;
        DataTable items;

        private void GetItemData()
        {
            dataGridView1.DataSource = items;
        }

        private void ItemImageModal_Load(object sender, EventArgs e)
        {
            GetItemData();
        }
        private int selectedProject = 0;
        private void dataGridView1_CellClick(object sender, DataGridViewCellEventArgs e)
        {
           
            if (e.RowIndex >= 0)
            {
               
                string selectedSalesQuotationId = this.items.Rows[this.selectedProject]["id"].ToString();

              
                DataView dataView = new DataView(images);
                string itemName = this.items.Rows[e.RowIndex]["item_name"].ToString();
                label2.Text = itemName.ToString();

                dataView.RowFilter = "based_id = '" + this.items.Rows[e.RowIndex]["id"].ToString() + "'";

                
                if (dataView.Count > 0)
                {
                 
                    string imagePath = dataView[0]["image"].ToString();
                    pictureBox1.ImageLocation = "http://" + imagePath;
                  
                }
                else
                {
                    
                }
            }
        }
    }
}
