using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Drawing.Drawing2D;
using System.Net.Http;
using System.Threading.Tasks;
using System.IO;
using System.Drawing.Imaging;
using Ephemera.NBagOfTricks;
using Ephemera.NBagOfUis;


namespace Ephemera.IconicSelector
{
    /// <summary>API.</summary>
    public partial class Selector : UserControl
    {
        #region Lifecycle
        /// <summary>
        /// Constructor. Doesn't need much so it can be used in the designer.
        /// </summary>
        public Selector()
        {
            // Init myself.
            SetStyle(ControlStyles.DoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint, true);
            AllowDrop = false;
            AutoScroll = true;

            // Default images.
            _bmpDir = Icon.ExtractIcon("shell32.dll", 3, false)!.ToBitmap();
            _bmpUrl = Icon.ExtractIcon("shell32.dll", 13, false)!.ToBitmap();
            _defaultImage = Icon.ExtractIcon("shell32.dll", 23, false)!.ToBitmap();
        }

        /// <summary>
        /// Does the real configuration.
        /// </summary>
        /// <param name="config">The configuration</param>
        public void Init(Config config)
        {
            _config = config;

            _itemdSize = ItemDisplay.Init(config);

            if (_config.DefaultImage is not null)
            {
                _defaultImage = _config.DefaultImage;
            }
        }
        #endregion

        #region Functions - API
        /// <summary>
        /// Add a resource - file, directory, url. Will determine the type, icon, and caption.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="index">Where to insert, -1 is append</param>
        public void AddResourceItem(string name, int index = -1)
        {
            Bitmap? bmp = null;// = _defaultImage;
            ItemDataType dtype = ItemDataType.None;
            string caption = "???";
            string namelc = name.ToLower();
            string targetname = "???";

            // Determine target type.
            FileInfo finfo = new(name);

            // Directory?
            if (Directory.Exists(name))
            {
                DirectoryInfo dinfo = new(name);
                caption = dinfo.Name;
                targetname = name;
                bmp = _bmpDir;
                dtype = ItemDataType.Dir;
            }

            // File?
            else if (File.Exists(name))
            {
                // Remove some extensions.
                caption = (namelc.EndsWith(".exe") || namelc.EndsWith(".lnk")) ?
                           Path.GetFileNameWithoutExtension(name) : finfo.Name;
                targetname = name;
                var icon = GraphicsUtils.SafeExtractIcon(targetname);
                bmp = icon is null ? _defaultImage : icon.ToBitmap();
                dtype = ItemDataType.File;
            }

            // URL?
            else if (namelc.StartsWith("http://") || namelc.StartsWith("https://") || namelc.StartsWith("file://"))
            {
                targetname = name;
                var fullurl = targetname;
                var uri = new Uri(fullurl);
                var parts = name.Split("://");
                caption = uri.Host;

                try
                {
                    // Try to get favicon.
                    using var httpClient = new HttpClient();
                    var ss = $"https://www.google.com/s2/favicons?domain={uri.Host}";
                    // Run async client synchronously. Could be dangerous...
                    var task = Task.Run(() => httpClient.GetStreamAsync(ss));
                    task.Wait();
                    using var img = Image.FromStream(task.Result);
                    bmp = new Bitmap(img);
                }
                catch (Exception e)
                {
                    // Async ops carry the originating exception in inner.
                    e = e.InnerException ?? e;

                    switch (e)
                    {
                        case HttpRequestException ex:
                            Tell($"Favicon request failed - using default: {ex.Message}");
                            bmp = _bmpUrl;
                            break;

                        default: // Client handles other errors.
                            throw;
                    }
                }

                dtype = ItemDataType.Url;
            }

            if (dtype != ItemDataType.None)
            {
                AddItem(dtype, caption, bmp ?? _defaultImage, targetname, index);
            }
            else
            {
                throw new ArgumentException($"Invalid resource [{name}]");
            }
        }

        /// <summary>
        /// Add a user-defined item.
        /// </summary>
        /// <param name="caption"></param>
        /// <param name="bmp"></param>
        /// <param name="value"></param>
        /// <param name="index">Where to insert, -1 is append</param>
        public void AddUserItem(string caption, Bitmap bmp, object value, int index = -1)
        {
            AddItem(ItemDataType.User, caption, bmp, value, index);
        }

        /// <summary>
        /// Get all items.
        /// </summary>
        /// <returns>Items.</returns>
        public List<Item> GetAllItems()
        {
            List<Item> res = [];
            _itemds.ForEach(itemd => res.Add(itemd.Item));
            return res;
        }

        /// <summary>
        /// Get selected items.
        /// </summary>
        /// <returns>Items.</returns>
        public List<Item> GetSelectedItems()
        {
            List<Item> res = [];
            _itemds.Where(itemd => itemd.Selected).ForEach(itemd => { res.Add(itemd.Item); });
            return res;
        }

        /// <summary>
        /// Remove item.
        /// </summary>
        /// <param name="item"></param>
        public void RemoveItem(Item item)
        {
            var res = _itemds.Where(itemd => itemd.Item == item).ToList();
            res.ForEach(itemd => RemoveItem(itemd));
        }

        /// <summary>
        /// Total real estate after populating with items. Does not include scrollbars.
        /// </summary>
        /// <returns>Items.</returns>
        public Size GetTotalArea()
        {
            // Calc client area.
            int totalWidth = _config.Spacing + _config.NumColumns * (_itemdSize.Width + _config.Spacing);
            int numRows = _itemds.Count / _config.NumColumns;
            if (_itemds.Count % _config.NumColumns > 0) numRows++;
            int totalHeight = _config.Spacing + numRows * (_itemdSize.Height + _config.Spacing);
            return new Size(totalWidth, totalHeight);
        }
        #endregion
    }
}
