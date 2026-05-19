using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Net.Http;
using System.Drawing.Imaging;
using Ephemera.NBagOfTricks;
using Ephemera.NBagOfUis;
using Ephemera.IconicSelector;


namespace Ephemera.IconicSelector.Test
{
    public partial class TestHost : Form
    {
        readonly Dictionary<string, string> _states = [];
        const int DEF_IMAGE_SIZE = 32;
        Selector? icsel = null;

        Bitmap?[] bmps = [];


        public TestHost()
        {
            SetStyle(ControlStyles.DoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint, true);
            InitializeComponent();
        }

        protected override void OnLoad(EventArgs e)
        {
            tvInfo.BackColor = Color.Cornsilk;
            tvInfo.Matchers =
            [
                new("ERR ", Color.Red),
                new("WRN ", Color.Green),
            ];

            // Init the images.
            var srcdir = MiscUtils.GetSourcePath();

            var bmp1 = new Bitmap(Path.Combine(srcdir, "Files", "glyphicons-22-snowflake.png"));
            var bmp2 = new Bitmap(Path.Combine(srcdir, "Files", "color-picker-small.png"));
            using var icon = new Icon(Path.Combine(srcdir, "Files", "crabe.ico"));
            var bmp3 = icon.ToBitmap();
            var bmp4 = new Bitmap(Path.Combine(srcdir, "Files", "color-picker.png"));
            //var defbmp = new Bitmap(Path.Combine(srcdir, "Files", "default.png"));
            var defbmp = GraphicsUtils.ExtractIconFromExecutable("shell32.dll", 77, true)!.ToBitmap();

            // Add entries to selector. Null forces selector default.
            bmps = [bmp1, bmp2, bmp3, bmp4, defbmp, null];

            BuildSelector(SelectorStyle.Icon, OpMode.SingleSelect, new(DEF_IMAGE_SIZE, DEF_IMAGE_SIZE), 4);

            //BuildSelector(SelectorStyle.Tile, OpMode.MultiSelect, new(DEF_IMAGE_SIZE, DEF_IMAGE_SIZE), 2);

            //BuildSelector(SelectorStyle.Fill, OpMode.Click, new(200, 64), 3);

            //BuildSelector(SelectorStyle.FitWidth, OpMode.Click, new(200, 50), 3);

            //BuildSelector(SelectorStyle.FitHeight, OpMode.Click, new(50, 200), 3);

            base.OnLoad(e);
        }

        void BuildSelector(SelectorStyle style, OpMode mode, Size imageSize, int numCols)
        {
            if (icsel is not null)
            {
                Controls.Remove(icsel);
            }

            icsel = new Selector()
            {
                AllowDrop = true,
                AllowExternalDrop = false,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                AutoScroll = true,
                BorderStyle = BorderStyle.FixedSingle,
                DrawFont = new Font("Calibri", 11F, FontStyle.Regular, GraphicsUnit.Point, 0),
                IndicatorColor = Color.Purple,
                Location = new Point(12, 22),
                Size = new Size(184, 453),
                Spacing = 10,
                Pad = 8,

                // variable
                Mode = mode,
                Style = style,
                NumColumns = numCols,
                ImageSize = imageSize,
            };

            var rand = new Random();
            for (int i = 0; i < 30; i++)
            {
                var text = $"Item {i} AAA BBB CCC DDD EEE";
                icsel.AddItem(text, bmps[rand.Next(0, bmps.Length)], $"fullname{i}");
            }

            // Hook up events.
            icsel.Selection += (sender, e) => { e.SelectedItems.ForEach(it => tvInfo.Append($"Selection -> [{it}]")); };

            icsel.Click += (sender, e) => { tvInfo.Append($"Click -> [{e.ClickedItem}]"); };

            icsel.Trace += (sender, e) => { tvInfo.Append($"-> [{e.Line}]"); };

            Controls.Add(icsel);
        }

        async void BtnGo1_Click(object sender, EventArgs e)
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

        void BtnGo2_Click(object sender, EventArgs e)
        {
            icsel.GetAllItems().ForEach(it => tvInfo.Append($">>> {it}"));
        }

        void DefImageRainbow()
        {
            using PixelBitmap pbmp = new(DEF_IMAGE_SIZE, DEF_IMAGE_SIZE);
            int blue = 128;
            int incr = 256 / DEF_IMAGE_SIZE;
            for (int y = 0; y < DEF_IMAGE_SIZE; y++)
            {
                for (int x = 0; x < DEF_IMAGE_SIZE; x++)
                {
                    pbmp.SetPixel(x, y, Color.FromArgb(255, x * incr % 256, y * incr % 256, blue));
                }
            }
            var defbmp = pbmp.GetBitmap();
        }
    }
}
