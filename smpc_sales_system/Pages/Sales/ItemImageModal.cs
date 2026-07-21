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
            //dataGridView1.DataSource = items;
            dataGridView1.DataSource = images;
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
                LoadItemImage(e.RowIndex);
            }
        }

        // Properties.Settings.Default.imagePath was a leftover hardcoded
        // "http://localhost:3000/api/vfile/" value that only ever worked on the original
        // developer's own machine. Build the URL from the actual environment-resolved
        // server address instead (same fix applied in ItemImagesModal.cs).
        private static string BuildImageUrl(string imagePath)
        {
            string path = (imagePath ?? string.Empty).Trim();

            if (path.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                return path;

            return $"{smpc_sales_system.Program.ApiBaseUrl}/vfile/{path}";
        }

        private void LoadItemImage(int rowIndex)
        {
            string imagePath = images.Rows[rowIndex]["image"].ToString();
            string imageUrl = BuildImageUrl(imagePath);

            pictureBox1.SizeMode = PictureBoxSizeMode.Zoom;
            pictureBox1.ImageLocation = imageUrl;

            // optional: show info
            label2.Text = $"Image {rowIndex + 1} of {images.Rows.Count}";
        }
    }
}
