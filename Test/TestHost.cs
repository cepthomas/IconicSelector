using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Net.Http;
using System.Drawing.Imaging;
using System.Linq;
using System.Threading.Tasks;
using Ephemera.NBagOfTricks;
using Ephemera.NBagOfUis;
using Ephemera.IconicSelector;


namespace Ephemera.IconicSelector.Test
{
    public class TestHost : Form
    {
        readonly Dictionary<string, string> _states = [];
        const int DEF_IMAGE_SIZE = 32;
        Bitmap[] bmps = [];
        readonly Selector icsel;

        /// <summary>
        /// 
        /// </summary>
        public TestHost()
        {
            SetStyle(ControlStyles.DoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint, true);

            icsel = new Selector()
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
            };
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="e"></param>
        protected override void OnLoad(EventArgs e)
        {
            // Init the images.
            var srcdir = MiscUtils.GetSourcePath();

            var bmp1 = new Bitmap(Path.Combine(srcdir, "Files", "glyphicons-22-snowflake.png"));
            var bmp2 = new Bitmap(Path.Combine(srcdir, "Files", "color-picker-small.png"));
            using var icon = new Icon(Path.Combine(srcdir, "Files", "crabe.ico"));
            var bmp3 = icon.ToBitmap();
            var bmp4 = new Bitmap(Path.Combine(srcdir, "Files", "color-picker.png"));
            //var defbmp = new Bitmap(Path.Combine(srcdir, "Files", "default.png"));
            var defbmp = Icon.ExtractIcon("shell32.dll", 77, false)!.ToBitmap();

            // Add entries to selector. Null forces selector default.
            bmps = [bmp1, bmp2, bmp3, bmp4, defbmp];

            //BuildSelector(SelectorStyle.Icon, OpMode.SingleSelect, new(DEF_IMAGE_SIZE, DEF_IMAGE_SIZE), 4);

            BuildSelector(SelectorStyle.Tile, OpMode.Click, new(DEF_IMAGE_SIZE, DEF_IMAGE_SIZE), 2);

            //BuildSelector(SelectorStyle.Fill, OpMode.Click, new(128, 64), 3);

            //BuildSelector(SelectorStyle.Clip, OpMode.MultiSelect, new(200, 50), 1);

            base.OnLoad(e);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="disposing"></param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                icsel.Dispose();
            }
            base.Dispose(disposing);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="style"></param>
        /// <param name="mode"></param>
        /// <param name="imageSize"></param>
        /// <param name="numCols"></param>
        void BuildSelector(SelectorStyle style, OpMode mode, Size imageSize, int numCols)
        {
            var config = new Config()
            {
                AllowExternalSource = true,
                //DrawFont = new Font("Calibri", 10F, FontStyle.Regular, GraphicsUnit.Point, 0),
                IndicatorColor = Color.Purple,
                EnableToolTip = true,
                Spacing = 10,
                Pad = 8,
                Mode = mode,
                Style = style,
                NumColumns = numCols,
                ImageSize = imageSize,
            };

            icsel.Init(config);

            // User items.
            var rand = new Random();

            // Resource items.
            string[] res =
            [
                // Windows standard locations  %PROGRAMDATA%\Microsoft\Windows\Start Menu\Programs
                @"%PROGRAMDATA%\Microsoft\Windows\Start Menu\Programs\Firefox.lnk",
                @"%PROGRAMDATA%\Microsoft\Windows\Start Menu\Programs\Administrative Tools\Performance Monitor.lnk",
                @"%PROGRAMDATA%\Microsoft\Windows\Start Menu\Programs\Notepad++.lnk",
                @"%PROGRAMDATA%\Microsoft\Windows\Start Menu\Programs\Visual Studio 2022.lnk",
                @"%PROGRAMDATA%\Microsoft\Windows\Start Menu\Programs\System Tools\Task Manager.lnk",
                // Plain files
                @"%USERPROFILE%\OneDrive\Tools\backup_loose.py",
                @"%USERPROFILE%\OneDrive\Tools\Wavosaur.exe",
                @"%USERPROFILE%\OneDrive\Tools\procexp.exe",
                @"C:\Dev\Libs\IconicSelector\Test\Files\color_wheel.png",
                @"%USERPROFILE%\OneDrive\OneDriveDocuments\eat\Dried Cherry Scones.txt",
                @"%USERPROFILE%\OneDrive\OneDriveDocuments\eat\faves\bean-potato-gratin.pdf",
                @"%USERPROFILE%\OneDrive\OneDriveDocuments\eat\faves\Beans.docx",
                // Plain folders
                @"%PROGRAMDATA%\Microsoft\Windows\Start Menu\Programs\Startup",
                @"%USERPROFILE%\OneDrive\OneDriveDocuments",
                @"C:\Dev\Apps",
                // URLs
                @"https://www.bobrosslipsum.com/",
                @"https://en.wikipedia.org/wiki/INI_file",
            ];

            for (int i = 0; i < res.Length; i++)
            {
                icsel.AddResourceItem(Environment.ExpandEnvironmentVariables(res[i]));
                if (rand.Next(0, 4) == 0)
                {
                    icsel.AddUserItem($"Item {i} AAA BBB CCC DDD EEE", bmps[rand.Next(0, bmps.Length)], $"This the payload for Item {i}");
                }
            }

            // Hook up events.
            icsel.Click += (sender, e) => {Tell($"Click -> [{e.ClickedItem}]"); };

            // Size me up.
            var area = icsel.GetTotalArea();
            ClientSize = new(area.Width + SystemInformation.VerticalScrollBarWidth, area.Height + 20);

            Controls.Add(icsel);
        }

        /// <summary>
        /// 
        /// </summary>
        void GetFavicon()
        {
            // Play with uri and favicons.
            var uri = new Uri("https://www.youtube.com/category/color/watch?v=0ju5LRTMFLw&list=RD0ju5LRTMFLw&start_radio=1");
            Tell($"Host [{uri.Host}]");
            Tell($"AbsolutePath [{uri.AbsolutePath}]");
            Tell($"Query [{uri.Query}]");
            uri.Segments.ForEach(seg => Tell($"Segment [{seg}]"));

            // Download the image and write to the file.
            try
            {
                //https://www.google.com/s2/favicons?domain=the-domain favicons from Google cache
                using var httpClient = new HttpClient();
                var ss = $"https://www.google.com/s2/favicons?domain={uri.Host}not";

                // Run async client synchronously. Could be dangerous...
                var task = Task.Run(() => httpClient.GetStreamAsync(ss));
                task.Wait();
                using var img = Image.FromStream(task.Result);
                //bmp = new Bitmap(img);

                // async:
                //var imageBytes = await httpClient.GetByteArrayAsync(ss);
                //await File.WriteAllBytesAsync("array", imageBytes);
                // var stream = await httpClient.GetStreamAsync(ss);
                // using var img = Image.FromStream(stream);

                img.Save("file.png", ImageFormat.Png);
                img.Save("file.jpg", ImageFormat.Jpeg);
            }
            catch (Exception e)
            {
                // Async ops carry the originating exception in inner.
                e = e.InnerException ?? e;

                switch (e)
                {
                    case HttpRequestException ex:
                        Tell($"Favicon request failed: {ex.Message}");
                        break;

                    default:
                        Tell($"Other error: {e.Message}");
                        break;
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        void Dump()
        {
            icsel?.GetAllItems().ForEach(it => Tell($">>> {it}"));
        }

        /// <summary>
        /// 
        /// </summary>
        void DefImage()
        {
            // Rainbow
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="line"></param>
        void Tell(string line)
        {
            Console.WriteLine($"TEST {line}");
        }
    }
}
