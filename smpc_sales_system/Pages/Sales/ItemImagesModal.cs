using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace smpc_sales_system.Pages.Sales
{
    public partial class ItemImagesModal : Form
    {
        DataTable images;
        DataTable items;
        DataTable selectedImages;
        ImageList imageList;
        string itemName;
        public List<Dictionary<string, object>> SelectedImages { get; private set; }
        public ItemImagesModal(string itemName, DataTable dt, DataTable dtIMage, DataTable dtSelectedImages)
        {
            InitializeComponent();
            this.images = dtIMage;
            this.selectedImages = dtSelectedImages;
            this.items = dt;
            this.itemName = itemName;
        }

        private void ItemImagesModal_Load(object sender, EventArgs e)
        {
            label1.Text = itemName;
            listView1.View = View.LargeIcon;
            listView1.CheckBoxes = true;
            listView1.MultiSelect = true;
            SetupListView();
            LoadItemImages();

        }
        private void SetupListView()
        {
            listView1.View = View.LargeIcon;
            listView1.MultiSelect = false;
            listView1.HideSelection = false;

            imageList = new ImageList();
            imageList.ImageSize = new Size(100, 100);
            listView1.LargeImageList = imageList;

            listView1.ItemChecked += listView1_ItemChecked;
        }
        private void listView1_ItemChecked(object sender, ItemCheckedEventArgs e)
        {
            lbl_counter.Text = $"Selected: {listView1.CheckedItems.Count}.";
        }
        // Properties.Settings.Default.imagePath was a leftover hardcoded
        // "http://localhost:3000/api/vfile/" value - that only ever worked on the original
        // developer's own machine (the same class of problem REPORTPATH had). Build the URL
        // the same way the Inventory app's item entry screen does, from the actual
        // environment-resolved server address, so images resolve correctly wherever this
        // app is actually running.
        private static string BuildImageUrl(string imagePath)
        {
            string path = (imagePath ?? string.Empty).Trim();

            if (path.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                return path;

            return $"{smpc_sales_system.Program.ApiBaseUrl}/vfile/{path}";
        }

        private void LoadItemImages()
        {
            listView1.Items.Clear();
            imageList.Images.Clear();

            int index = 0;
            foreach (DataRow row in images.Rows)
            {
                string imageId = row["id"].ToString();
                string imagePath = row["image"].ToString();
                string filename = row["filename"].ToString();
                string imageUrl = BuildImageUrl(imagePath);

                Image thumb = null;
                try
                {
                    // Load thumbnail from file or url
                    thumb =  Image.FromStream(new WebClient().OpenRead(imageUrl));
                }
                catch
                {
                    //thumb = SystemIcons.Application.ToBitmap();
                    thumb = Properties.Resources.no_pictures;
                }

                imageList.Images.Add(thumb);

                ListViewItem item = new ListViewItem(filename, index);
                item.Tag = new { Id = imageId, Url = imageUrl };

                bool isPreviouslySelected = selectedImages.AsEnumerable()
                    .Any(r => r["image_id"].ToString() == imageId);
                item.Checked = isPreviouslySelected;


                listView1.Items.Add(item);

                index++;
            }
        }

        private void ListView1_ItemSelectionChanged(object sender, ListViewItemSelectionChangedEventArgs e)
        {
            if (e.IsSelected)
            {
                string imageUrl = e.Item.Tag.ToString();

                //pictureBox1.SizeMode = PictureBoxSizeMode.Zoom;
                //pictureBox1.ImageLocation = imageUrl;

                //label2.Text = e.Item.Text; // show filename
            }
        }

        private void btn_select_Click(object sender, EventArgs e)
        {
            SelectedImages = new List<Dictionary<string, object>>();

            foreach (ListViewItem item in listView1.Items)
            {
                if (item.Checked)
                {
                    var tagData = (dynamic)item.Tag; // since you used new { Id, Url }
                    int imageId = Convert.ToInt32(tagData.Id);
                    var data = new Dictionary<string, object>
                    {
                        { "image_id", imageId },
                        { "is_selected", item.Checked }
                    };

                    SelectedImages.Add(data);
                }
            }

            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private int hoveredIndex = -1;

        private void listView1_MouseMove(object sender, MouseEventArgs e)
        {
            ListViewItem item = listView1.GetItemAt(e.X, e.Y);
            if (item != null)
                hoveredIndex = item.Index;
            else
                hoveredIndex = -1;

            listView1.Invalidate(); // force redraw
        }

        private void listView1_DrawItem(object sender, DrawListViewItemEventArgs e)
        {
            e.DrawDefault = true; // draw normal item

            if (e.Item.Index == hoveredIndex)
            {
                // Draw checkbox manually
                CheckBoxRenderer.DrawCheckBox(
                    e.Graphics,
                    new Point(e.Bounds.Left, e.Bounds.Top),
                    System.Windows.Forms.VisualStyles.CheckBoxState.UncheckedNormal
                );
            }
        }
    }
}
