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

        // Counts clicks, so a slow download for an earlier row cannot land on top of
        // the image the user has since moved on to.
        private int _imageRequest;

        // Downloaded with the session token instead of handed to ImageLocation:
        // PictureBox fetches a URL itself and cannot send a header, and uploaded
        // files are served only to a logged-in session once the API sets
        // FILES_REQUIRE_AUTH.
        private async void LoadItemImage(int rowIndex)
        {
            string imagePath = images.Rows[rowIndex]["image"].ToString();
            string imageUrl = BuildImageUrl(imagePath);
            int request = ++_imageRequest;

            pictureBox1.SizeMode = PictureBoxSizeMode.Zoom;

            // optional: show info
            label2.Text = $"Image {rowIndex + 1} of {images.Rows.Count}";

            Image image;
            smpc_app.Services.Helpers.Helpers.Loading.ShowLoading(this);
            try
            {
                try
                {
                    using (var client = new System.Net.Http.HttpClient())
                    {
                        string token = smpc_sales_app.Data.CacheData.SessionToken;
                        if (!string.IsNullOrEmpty(token))
                            client.DefaultRequestHeaders.Add("Authorization", token);

                        byte[] data = await client.GetByteArrayAsync(imageUrl);
                        using (var stream = new System.IO.MemoryStream(data))
                        using (var decoded = Image.FromStream(stream))
                        {
                            image = new Bitmap(decoded);
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[ItemImageModal] Failed to load \"{imageUrl}\": {ex.Message}");
                    image = null;
                }
            }
            finally
            {
                smpc_app.Services.Helpers.Helpers.Loading.HideLoading(this);
            }

            if (request != _imageRequest || IsDisposed)
            {
                image?.Dispose();
                return;
            }

            Image previous = pictureBox1.Image;
            pictureBox1.Image = image ?? pictureBox1.ErrorImage;
            if (previous != null && previous != pictureBox1.ErrorImage)
                previous.Dispose();
        }
    }
}
