using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Ephemera.NBagOfTricks;
using Ephemera.IconicSelector;
using System.Net.Http;
using System.Drawing.Imaging;


#pragma warning disable CS0168 // Variable is declared but never used

namespace Ephemera.IconicSelector.Test
{
    public partial class TestHost : Form
    {
        const int IMAGE_SIZE = 32;

        public TestHost()
        {
            InitializeComponent();
        }

        protected override void OnLoad(EventArgs e)
        {
            ///// Init controls.
            tvInfo.BackColor = Color.Cornsilk;
            tvInfo.Matchers =
            [
                new("ERR ", Color.Red),
                new("WRN ", Color.Green),
            ];

            ///// IconicSelector
            var spec = new DrawSpecIcon()
            {
                TargetColor = Color.LightSkyBlue,
                Pad = 8,
                ImageSize = IMAGE_SIZE
            };
            icsel.AllowExternalDrop = true;
            icsel.LeftMouseClick = MouseFunction.Click;
            icsel.Style = SelectorStyle.Tile;

            // Init the image list.
            var sdir = MiscUtils.GetSourcePath();

            var bmp1 = new Bitmap(Path.Combine(sdir, "Files", "glyphicons-22-snowflake.png"));
            using var icon = new Icon(Path.Combine(sdir, "Files", "crabe.ico"));
            var bmp2 = new Bitmap(Path.Combine(sdir, "Files", "color-picker-small.png"));

            // Default image - rainbow.
            using PixelBitmap pbmp = new(IMAGE_SIZE, IMAGE_SIZE);
            int incr = 256 / IMAGE_SIZE;
            for (int y = 0; y < IMAGE_SIZE; y++)
            {
                for (int x = 0; x < IMAGE_SIZE; x++)
                {
                    pbmp.SetPixel(x, y, Color.FromArgb(255, x * incr % 256, y * incr % 256, 150));
                }
            }
            var defbmp = pbmp.GetBitmap();

            // Add entries to selector
            Bitmap[] bmps = [bmp1, icon.ToBitmap(), bmp2, defbmp];
            var rand = new Random();
            for (int i = 0; i < 15; i++)
            {
                var text = $"Item {i} etc etc etc etc";
                icsel.AddItem(text, bmps[rand.Next(0, bmps.Length)], $"fullname{i}");
            }

            icsel.Selection += (sender, e) =>
            {
                tvInfo.Append($"Selections ->");
                e.SelectedItems.ForEach(it => tvInfo.Append($"  [{it}]"));
            };

            icsel.DroppedTarget += (sender, e) =>
            {
                tvInfo.Append($"DroppedTarget -> [{e.NewItem}]");
            };

            icsel.Trace += (sender, e) =>
            {
                tvInfo.Append($"TRACE -> [{e}]");
            };

            base.OnLoad(e);
        }

        void BtnDnD_Click(object sender, EventArgs e)
        {
            try
            {
                var formDnD = new Snip_DragNDrop.FormDnd();
                formDnD.ShowDialog();
            }
            catch (Exception ex)
            {
                tvInfo.Append($"ERR -> [{ex}]");
            }
        }

        async void Button1_Click(object sender, EventArgs e)
        {
            // Play with uri and favicons.
            var uri = new Uri("https://www.youtube.com/category/color/watch?v=0ju5LRTMFLw&list=RD0ju5LRTMFLw&start_radio=1");
            tvInfo.Append($"Host [{uri.Host}]");
            tvInfo.Append($"AbsolutePath [{uri.AbsolutePath}]");
            tvInfo.Append($"Query [{uri.Query}]");
            uri.Segments.ForEach(seg => tvInfo.Append($"Segment [{seg}]"));

            // Download the image and write to the file.
            try
            {
                //https://www.google.com/s2/favicons?domain=the-domain lets you get png favicons from Google cache
                using var httpClient = new HttpClient();
                var ss = $"https://www.google.com/s2/favicons?domain={uri.Host}not";
                //var imageBytes = await httpClient.GetByteArrayAsync(ss);
                //await File.WriteAllBytesAsync("array", imageBytes);
                var stream = await httpClient.GetStreamAsync(ss);
                using var img = Image.FromStream(stream);
                img.Save("file.png", ImageFormat.Png);
                img.Save("file.jpg", ImageFormat.Jpeg);
            }
            catch (HttpRequestException ex)
            {

            }
            catch (Exception ex)
            {

            }
        }
    }
}
