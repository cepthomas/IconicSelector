using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Ephemera.NBagOfTricks;
using Ephemera.IconicSelector;
using System.Net.Http;
using System.Drawing.Imaging;
using System.Collections.Generic;
using System.Media;


namespace Ephemera.IconicSelector.Test
{
    public partial class TestHost : Form
    {
        readonly Dictionary<string, string> _states = [];
        const int DEF_IMAGE_SIZE = 32;

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

            //////
            //icsel.Style = SelectorStyle.Icon;
            //icsel.ImageSize = new(DEF_IMAGE_SIZE, DEF_IMAGE_SIZE);

            //////
            icsel.Style = SelectorStyle.Tile;
            icsel.ImageSize = new(DEF_IMAGE_SIZE, DEF_IMAGE_SIZE);

            //////
            //icsel.Style = SelectorStyle.Fit;
            //icsel.ImageSize = new(200, 64);

            //////
            //icsel.Style = SelectorStyle.Image;
            //icsel.ImageSize = new(100, 50);

            icsel.AllowExternalDrop = true;
            icsel.NumColumns = 3;
            icsel.LeftMouseClick = MouseFunction.SingleSelect;
            //icsel.LeftMouseClick = MouseFunction.Click;
            icsel.IndicatorColor = Color.Red;
            icsel.Pad = 8;

            icsel.Init();


            // Init the image list.
            var sdir = MiscUtils.GetSourcePath();

            var bmp1 = new Bitmap(Path.Combine(sdir, "Files", "glyphicons-22-snowflake.png"));
            var bmp2 = new Bitmap(Path.Combine(sdir, "Files", "color-picker-small.png"));
            using var icon = new Icon(Path.Combine(sdir, "Files", "crabe.ico"));
            var bmp3 = icon.ToBitmap();
            var bmp4 = new Bitmap(Path.Combine(sdir, "Files", "color-picker.png"));
            var defbmp = new Bitmap(Path.Combine(sdir, "Files", "default.png"));

            //// Default image - rainbow.
            //using PixelBitmap pbmp = new(DEF_IMAGE_SIZE, DEF_IMAGE_SIZE);
            //int incr = 256 / DEF_IMAGE_SIZE;
            //for (int y = 0; y < DEF_IMAGE_SIZE; y++)
            //{
            //    for (int x = 0; x < DEF_IMAGE_SIZE; x++)
            //    {
            //        pbmp.SetPixel(x, y, Color.FromArgb(255, x * incr % 256, y * incr % 256, 150));
            //    }
            //}
            //var defbmp = pbmp.GetBitmap();

            //icsel.AddItem("BOOM", bmp4, $"fullnameXXX");

            // Add entries to selector. Null forces selector default.
            Bitmap?[] bmps = [bmp1, bmp2, bmp3, bmp4, defbmp, null];
            var rand = new Random();
            for (int i = 0; i < 30; i++)
            {
                //icsel.AddItem("333", bmp1, "fullname111");
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
                tvInfo.Append($"DroppedTarget ->");
                tvInfo.Append($"  [{e.NewItem}]");
            };

            icsel.Trace += (sender, e) =>
            {
                if (e.Line.Length > 0)
                {
                    tvInfo.Append($"-> [{e.Line}]");
                }
                if (e.State.Length > 0)
                {
                    var parts = e.State.SplitByToken(" ");
                    if (!_states.ContainsKey(parts[0]))
                    {
                        _states.Add(parts[0], e.State);
                    }
                    else
                    {
                        _states[parts[0]] = e.State;
                    }

                    var s = string.Join(Environment.NewLine, _states.Values);
                    tbState.Text = s;
                }
            };

            base.OnLoad(e);
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
            try
            {
                ColorDialog dlg = new();
                dlg.ShowDialog();

                //var formDnD = new Snip_DragNDrop.FormDnd();
                //formDnD.ShowDialog();
            }
            catch (Exception ex)
            {
                tvInfo.Append($"ERR -> [{ex}]");
            }
        }

        /////////////////////////////////////////////////////////////////////
        ////////////////////// leftovers ////////////////////////////////////
        /////////////////////////////////////////////////////////////////////

        /// <summary>Image fitting to ImageSize.</summary>
        public enum ImageFit
        {
            /// <summary>Client is in charge of images</summary>
            None,
            /// <summary>Rendered image height from client, width scaled</summary>
            FitHeight,
            /// <summary>Rendered image width from client, height scaled</summary>
            FitWidth,
            /// <summary>Use all available space</summary>
            Fill,
        }

        public Bitmap FitOne(Bitmap bmp, Size sz, ImageFit fit)
        {
            // Make a thumbnail scaled to available real estate.
            float ratio = (float)sz.Height / bmp.Height;
            int tnWidth = (int)(bmp.Width * ratio);
            int tnHeight = sz.Height;
            //var res = bmp.Resize(tnWidth, tnHeight);
            //return res;
            return bmp;
        }
    }
}
