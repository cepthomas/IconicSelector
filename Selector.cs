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
    #region Types
    /// <summary>Selector style options.</summary>
    public enum SelectorStyle
    {
        /// <summary>Icon of ImageSize above, text block below</summary>
        Icon,
        /// <summary>Icon of ImageSize left, text block right</summary>
        Tile,
        /// <summary>Verbatim as provided by client, will clip from top left</summary>
        Clip,
        /// <summary>Fill/stretch with image</summary>
        Fill,
        /// <summary>Rendered image width from client, height scaled</summary>
        FitWidth,
        /// <summary>Rendered image height from client, width scaled</summary>
        FitHeight,
    }

    /// <summary>Selector operation mode.</summary>
    public enum OpMode
    {
        /// <summary>One only selection.</summary>
        SingleSelect,
        /// <summary>One or more selection.</summary>
        MultiSelect,
        /// <summary>Standard single click.</summary>
        Click,
    }

    /// <summary>
    /// User clicked. If item is not null it's a simple click: OpMode is Click.
    /// Else it's a notification that selection(s) changed: OpMode is *Select.
    /// </summary>
    public class ClickEventArgs(Item? item) : EventArgs
    {
        public Item? ClickedItem { get; init; } = item;
    }
    #endregion


    /// <summary>API.</summary>
    public partial class Selector : UserControl
    {
        #region Properties
        /// <summary>Current config.</summary>
        public SelectorStyle Style { get { return _style; } set { _style = value; InitGeometry(); } }

        /// <summary>Current config.</summary>
        public int NumColumns { get { return _numColumns; } set { _numColumns = value; InitGeometry(); } }

        /// <summary>Image size.</summary>
        public Size ImageSize { get { return _imageSize; } set { _imageSize = value; InitGeometry(); } }

        /// <summary>What the mouse click does.</summary>
        public OpMode Mode { get; set; } = OpMode.Click;

        /// <summary>Allow drag and drop frome external sources - file/folder/url only.</summary>
        public bool AllowExternalSource { get; set; } = false;

        /// <summary>Cosmetics.</summary>
        public Font DrawFont { get; set; } = new("Calibri", 11, FontStyle.Regular, GraphicsUnit.Point, 0);

        /// <summary>Cosmetics.</summary>
        public Color IndicatorColor { get; set; } = Color.Purple;

        /// <summary>Visual space at edges.</summary>
        public int Pad { get; set; } = 4;

        /// <summary>Space between items</summary>
        public int Spacing { get; set; } = 10;

        /// <summary>If no valid image available.</summary>
        public Bitmap DefaultImage { get; set; }

        /// <summary>If no valid image available.</summary>
        public Bitmap FolderImage { get; set; }

        /// <summary>If no valid image available.</summary>
        public Bitmap UrlImage { get; set; }
        #endregion

        #region Events
        /// <summary>Tell client that item was clicked - OpMode = Click.</summary>
        public new event EventHandler<ClickEventArgs>? Click;
        #endregion

        #region Lifecycle
        /// <summary>
        /// Constructor.
        /// </summary>
        public Selector()
        {
            // Init myself.
            SetStyle(ControlStyles.DoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint, true);
            AllowDrop = false;
            AutoScroll = true;

            // Make default images.
            FolderImage = Icon.ExtractIcon("shell32.dll", 3, false)!.ToBitmap();
            UrlImage = Icon.ExtractIcon("shell32.dll", 13, false)!.ToBitmap();
            DefaultImage = Icon.ExtractIcon("shell32.dll", 23, false)!.ToBitmap();

            toolTip1.SetToolTip(userControl1, "Your text here");
        }
        #endregion

        #region Functions
        /// <summary>
        /// Add a resource - file, directory, url. Will determine the type, icon, and caption.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="index">Where to insert, -1 is append</param>
        public void AddResourceItem(string name, int index = -1)
        {
            Bitmap bmp = DefaultImage;
            ItemDataType dtype = ItemDataType.None;
            string caption = "???";
            string namelc = name.ToLower();
            string targetname = "???";

            ///// Determine target type.
            FileInfo finfo = new(name);

            // Expand Link?
            if (namelc.EndsWith(".lnk"))
            {
                var lname = GetLinkTarget(name);
                if (lname != null)
                {
                    name = lname;
                    namelc = name.ToLower();
                }
            }

            // Directory?
            if (Directory.Exists(name))
            {
                DirectoryInfo dinfo = new(name);
                caption = dinfo.Name;
                targetname = name;
                bmp = FolderImage;
                dtype = ItemDataType.Dir;
            }
            // File?
            else if (File.Exists(name))
            {
                caption = namelc.EndsWith(".exe") ? Path.GetFileNameWithoutExtension(name) : finfo.Name;
                targetname = name;
                var icon = SafeExtractIcon(targetname);
                bmp = icon is null ? DefaultImage : icon.ToBitmap();
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
                    // Async ops carry the original exception in inner.
                    e = e.InnerException ?? e;

                    switch (e)
                    {
                        case HttpRequestException ex:
                            TraceLine($"Favicon request failed - using default: {ex.Message}");
                            bmp = UrlImage;
                            break;

                        default: // Client handles.
                            throw;
                    }
                }

                dtype = ItemDataType.Url;
            }

            if (dtype != ItemDataType.None)
            {
                AddItem(dtype, caption, bmp, targetname, index);
            }
            else
            {
                //TODO1 _logger.Error($"Invalid target [{targetname}]");
            }

            //case DroppedDataType.File:
            //{
            //    var fpath = (string)e.Payload;

            //    if (File.Exists(fpath))
            //    {
            //        var fn = Path.GetFileName(fpath);
            //        TraceLine($"Dropped file -> [{fpath}]");

            //        try
            //        {
            //            var bmp = SafeExtractIcon(fpath)!.ToBitmap();
            //            AddItem(ItemDataType.File, fn, bmp, fpath, _insertIndex);
            //        }
            //        catch (Exception)
            //        {
            //            AddItem(ItemDataType.File, fn, DefaultImage, fpath, _insertIndex);
            //        }
            //    }
            //    else if (Directory.Exists(fpath))
            //    {
            //        var dpath = (string)e.Payload;
            //        //var dn = Directory.GetDirectoryRoot(dpath);
            //        var dn = Path.GetFileName(dpath);
            //        TraceLine($"Dropped dir -> [{dpath}]");

            //        try
            //        {
            //            var bmp = SafeExtractIcon(dpath)!.ToBitmap();
            //            AddItem(ItemDataType.File, dn, bmp, dpath, _insertIndex);
            //        }
            //        catch (Exception)
            //        {
            //            AddItem(ItemDataType.File, dn, FolderImage, dpath, _insertIndex);
            //        }
            //    }
            //    // else TODO1???
            //}

            //case DroppedDataType.Url:
            //    var fullurl = (string)e.Payload;
            //    var uri = new Uri(fullurl);

            //    try
            //    {
            //        // Try to get favicon.
            //        using var httpClient = new HttpClient();
            //        var ss = $"https://www.google.com/s2/favicons?domain={uri.Host}";
            //        // Run async client synchronously. Could be dangerous...
            //        var task = Task.Run(() => httpClient.GetStreamAsync(ss));
            //        task.Wait();
            //        using var img = Image.FromStream(task.Result);
            //        AddItem(ItemDataType.Url, uri.Host, new Bitmap(img), fullurl, _insertIndex);
            //    }
            //    catch (HttpRequestException ex)
            //    {
            //        TraceLine($"Favicon request failed - using default: {ex.Message}");
            //        AddItem(ItemDataType.Url, uri.Host, UrlImage, fullurl, _insertIndex);
            //    }
            //    catch (Exception)
            //    {
            //        // Client handles.
            //        throw;
            //    }
            //    break;
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
        #endregion
    }
}
