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
using Ephemera.NBagOfTricks;
using Ephemera.NBagOfUis;


namespace Ephemera.IconicSelector
{
    /// <summary>Master control.</summary>
    public class Selector : UserControl // ScrollableControl?
    {
        #region Properties - API
        /// <summary>Current config.</summary>
        public SelectorStyle Style { get { return _style; } set { _style = value; InitGeometry(); } }
        SelectorStyle _style = SelectorStyle.Icon;

        /// <summary>Current config.</summary>
        public int NumColumns { get { return _numColumns; } set { _numColumns = value; InitGeometry(); } }
        int _numColumns = 1;

        /// <summary>What the mouse click does.</summary>
        public OpMode Mode { get; set; } = OpMode.Click;

        /// <summary>Allow drag and drop (files) from other applications.</summary>
        public bool AllowExternalDrop { get; set; } = false;

        /// <summary>Image size.</summary>
        public Size ImageSize { get; set; } = new(32, 32);

        /// <summary>Cosmetics.</summary>
        public Font DrawFont { get; set; } = new("Calibri", 11, FontStyle.Regular, GraphicsUnit.Point, 0);

        /// <summary>Cosmetics.</summary>
        public Color IndicatorColor { get; set; } = Color.Purple;

        /// <summary>If no valid image available.</summary>
        public Bitmap DefaultImage  { set { _defaultImage = value; } }

        /// <summary>Visual space at edges.</summary>
        public int Pad { get; set; } = 4;

        /// <summary>Space between items</summary>
        public int Spacing { get; set; } = 10;
        #endregion

        #region Fields
        /// <summary>All entries in the collection.</summary>
        readonly List<ItemDisplay> _itemds = [];

        /// <summary>If no valid image available.</summary>
        Bitmap _defaultImage;

        /// <summary>ItemDisplay geometry.</summary>
        Rectangle _itemdImageRect = new();

        /// <summary>ItemDisplay geometry.</summary>
        Rectangle _itemdTextRect = new();

        /// <summary>ItemDisplay geometry.</summary>
        Size _itemdSize;

        /// <summary>Needs to be refreshed.</summary>
        //bool _refresh = true;

        /// <summary>Where to move/insert item.</summary>
        int _insertIndex = NOT_IN_TARGET;

        /// <summary>Meta index.</summary>
        const int NOT_IN_TARGET = -1;

        /// <summary>Meta index.</summary>
        const int IN_TARGET_CENTER = -2;
        #endregion

        #region Events - API
        /// <summary>Tell client that item was clicked - OpMode = Click.</summary>
        public new event EventHandler<ClickEventArgs>? Click;

        /// <summary>Tell client that selection(s) have changed - OpMode = *Select.</summary>
        public event EventHandler<SelectionEventArgs>? Selection;

        /// <summary>Debug hook - something permanent?</summary>
        public event EventHandler<TraceEventArgs>? Trace;
        #endregion

        #region Lifecycle
        /// <summary>
        /// Constructor.
        /// </summary>
        public Selector()
        {
            // Init myself.
            SetStyle(ControlStyles.DoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint, true);
            AllowDrop = true;
            AutoScroll = true;

            // Make a default image.
            _defaultImage = new(32, 32);
            using Graphics gr = Graphics.FromImage(_defaultImage);
            gr.Clear(Color.LightSalmon);
            gr.DrawString($"????", Font, Brushes.Black, 2, 2);
        }

        /// <summary>
        /// Calculates geometry of display elements.
        /// </summary>
        void InitGeometry()
        {
            // Figure geometry.
            switch (Style)
            {
                case SelectorStyle.Icon:
                    {
                        _itemdImageRect = new(Pad + ImageSize.Width / 2, Pad, ImageSize.Width, ImageSize.Height);
                        _itemdTextRect = new(Pad, _itemdImageRect.Bottom + Pad, 2 * ImageSize.Width, ImageSize.Height);
                        _itemdSize = new(_itemdTextRect.Right + Pad, _itemdTextRect.Bottom + Pad);
                        Width = Spacing + NumColumns * (_itemdSize.Width + Spacing) + SystemInformation.VerticalScrollBarWidth;
                    }
                    break;

                case SelectorStyle.Tile:
                    {
                        _itemdImageRect = new(Pad, Pad, ImageSize.Width, ImageSize.Height);
                        _itemdTextRect = new(_itemdImageRect.Right + Pad, Pad, 2 * ImageSize.Width, ImageSize.Height);
                        _itemdSize = new(_itemdTextRect.Right + Pad, _itemdTextRect.Bottom + Pad);
                        Width = Spacing + NumColumns * (_itemdSize.Width + Spacing) + SystemInformation.VerticalScrollBarWidth;
                    }
                    break;

                case SelectorStyle.Image:
                case SelectorStyle.Fit:
                    {
                        _itemdImageRect = new(0, 0, ImageSize.Width, ImageSize.Height);
                        _itemdTextRect = new(); // not used
                        _itemdSize = new(ImageSize.Width, ImageSize.Height);
                        Width = Spacing + NumColumns * (_itemdSize.Width + Spacing) + SystemInformation.VerticalScrollBarWidth;
                    }
                    break;
            }

            TraceLine($"geometry Width:{Width} _itemdSize:{_itemdSize}");
        }

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _itemds.ForEach(itemd => { itemd.Dispose(); });
                _itemds.Clear();
                _defaultImage.Dispose();
            }
            base.Dispose(disposing);
        }
        #endregion

        #region Functions - API
        /// <summary>
        /// Add a new item.
        /// </summary>
        /// <param name="caption">For display below/next to image</param>
        /// <param name="bmp">Bitmap</param>
        /// <param name="value">Meaningful for client use</param>
        /// <param name="index">Where to insert, -1 is append</param>
        public void AddItem(string caption, Bitmap? bmp, object value, int index = -1)
        {
            // Make a new item. Maybe adjust the image.
            bmp ??= _defaultImage;

            switch (Style)
            {
                case SelectorStyle.Icon:
                    // Image as is.
                    break;

                case SelectorStyle.Tile:
                    // Image as is.
                    break;

                case SelectorStyle.Image:
                    // Copy pixels starting from 0, 0 to fill the visible area.
                    PixelBitmap pbmpin = new(bmp);
                    PixelBitmap pbmpout = new(ImageSize.Width, ImageSize.Height);

                    for (int x = 0; x < ImageSize.Width && x < bmp.Width; x++)
                    {
                        for (int y = 0; y < ImageSize.Height && y < bmp.Height; y++)
                        {
                            pbmpout.SetPixel(x, y, pbmpin.GetPixel(x, y));
                        }
                    }

                    bmp = pbmpout.GetBitmap();
                    pbmpin.Dispose();
                    pbmpout.Dispose();
                    break;

                case SelectorStyle.Fit:
                    bmp = ResizeBitmap(bmp, ImageSize.Width, ImageSize.Height);
                    break;
            }

            Item item = new(caption, bmp, value);

            ItemDisplay itemd = new(item)
            {
                IndicatorColor = IndicatorColor,
                ImageRect = _itemdImageRect,
                TextRect = _itemdTextRect,
                Size = _itemdSize,
                AllowExternalDrop = AllowExternalDrop,
            };
            itemd.DoMouseClick += Itemd_DoMouseClick;
            itemd.DroppedPayload += Itemd_DroppedPayload;
            itemd.CursorLocationChanged += Itemd_CursorLocationChanged;

            Controls.Add(itemd);

            // Where to put it?
            if (index >= 0 && index < _itemds.Count)
            {
                _itemds.Insert(index, itemd);
            }
            else // append
            {
                _itemds.Add(itemd);
            }

            UpdateItemsList();
            Invalidate(true); // refresh everything
        }

        /// <summary>
        /// Remove this item.
        /// </summary>
        /// <param name="itemd"></param>
        /// <exception cref="ArgumentNullException"></exception>
        void RemoveItem(ItemDisplay itemd)
        {
            ArgumentNullException.ThrowIfNull(itemd);

            Controls.Remove(itemd);
            _itemds.Remove(itemd);

            UpdateItemsList();
            Invalidate(true); // refresh everything
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
        #endregion

        #region Events
        /// <summary>
        /// Handle cursor change.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Itemd_CursorLocationChanged(object? sender, CursorLocationEventArgs e)
        {
            int index = GetItemIndex(sender);
            TraceLine($"Itemd_CursorLocationChange() index:{index}e:{e}");

            switch(e.Location)
            {
                case CursorLocation.Left:
                    _insertIndex = index;
                    break;

                case CursorLocation.Right:
                    _insertIndex = index + 1;
                    break;

                case CursorLocation.Center:
                    _insertIndex = IN_TARGET_CENTER;
                    break;

                case CursorLocation.None:
                    _insertIndex = NOT_IN_TARGET;
                    break;
            }

            Invalidate(); // just this control
        }

        /// <summary>
        /// User item selection(s). Could be select or click.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Itemd_DoMouseClick(object? sender, MouseEventArgs e)
        {
            ArgumentNullException.ThrowIfNull(sender);

            var itemd = (ItemDisplay)sender;
            bool sel = itemd.Selected; // current

            switch (e.Button, Mode)
            {
                case (MouseButtons.Left, OpMode.Click):
                    Click?.Invoke(this, new(itemd.Item));
                    break;

                case (MouseButtons.Left, OpMode.SingleSelect):
                    if (sel)
                    {
                        itemd.Selected = false;
                        Selection?.Invoke(this, new([])); // no select
                    }
                    else
                    {
                        // Deselect others first.
                        _itemds.ForEach(itemd => itemd.Selected = false);
                        // Select this one.
                        itemd.Selected = true;
                        Selection?.Invoke(this, new([itemd.Item]));
                    }
                    break;

                case (MouseButtons.Left, OpMode.MultiSelect):
                    itemd.Selected = !sel;
                    Selection?.Invoke(this, new([.. _itemds.Where(itemd => itemd.Selected).Select(itemd => itemd.Item)]));
                    break;

                case (_, _):
                    // ignored
                    break;
            }

            Invalidate(true); // refresh everything
        }
        #endregion

        #region Drawing
        /// <summary>
        /// Draw the whole control.
        /// </summary>
        /// <param name="pe"></param>
        protected override void OnPaint(PaintEventArgs pe)
        {
            pe.Graphics.Clear(BackColor);

            // Insert marker?
            if (_insertIndex >= 0)
            {
                using Pen pen = new(IndicatorColor, 4);
                int offset = 3;

                // Special case for last item.
                if (_insertIndex >= _itemds.Count)
                {
                    var itemd = _itemds.Last();
                    var loc = itemd.Location;
                    pe.Graphics.DrawLine(pen, loc.X + itemd.Width + offset, loc.Y, loc.X + itemd.Width + offset, loc.Y + itemd.Height);
                }
                else
                {
                    var itemd = _itemds[_insertIndex];
                    var loc = itemd.Location;
                    pe.Graphics.DrawLine(pen, loc.X - offset, loc.Y, loc.X - offset, loc.Y + itemd.Height);
                }
            }

            base.OnPaint(pe);
        }
        #endregion

        #region Drag and drop
        /// <summary>
        /// Handle dropped data.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Itemd_DroppedPayload(object? sender, DroppedPayloadEventArgs e)
        {
            int index = GetItemIndex(sender);
            TraceLine($"Itemd_DroppedPayload() index:{index}e:{e}");

            switch (e.DataType)
            {
                case DroppedPayloadType.Item:
                    var draggedItem = (ItemDisplay)e.Payload;
                    TraceLine($"Dropped item -> [{draggedItem}]");
                    // Insert a copy of the dragged item at the insert index.
                    Item it = draggedItem.Item;
                    AddItem(it.Caption, it.Bitmap, it.Value, _insertIndex);
                    // Remove the original dragged item.
                    RemoveItem(draggedItem);
                    break;

                case DroppedPayloadType.File:
                    var payload = (string)e.Payload;
                    TraceLine($"Dropped file -> [{payload}]");
                    var icon = GraphicsUtils.ExtractIconFromExecutable(payload, 0, true);
                    var fn = Path.GetFileName(payload);
                    AddItem(fn, icon?.ToBitmap(), payload);
                    break;

                case DroppedPayloadType.Url:
                    var fullurl = (string)e.Payload;
                    var uri = new Uri(fullurl);

                    try
                    {
                        // Try to get favicon.
                        using var httpClient = new HttpClient();
                        var ss = $"https://www.google.com/s2/favicons?domain={uri.Host}";
                        // Run async client synchronously. Could be dangerous...
                        var task = Task.Run(() => httpClient.GetStreamAsync(ss));
                        task.Wait();
                        using var img = Image.FromStream(task.Result);
                        AddItem(uri.Host, new Bitmap(img), fullurl);
                    }
                    catch (HttpRequestException ex)
                    {
                        TraceLine($"Request failed - use default [{ex.Message}]");
                        AddItem(uri.Host, _defaultImage, fullurl);
                    }
                    catch (Exception)
                    {
                        // Client handles.
                        throw;
                    }
                    break;

                default:
                    throw new InvalidOperationException();
            }
        }
        #endregion

        #region Internals
        /// <summary>
        /// Called after list changes.
        /// </summary>
        void UpdateItemsList()
        {
            // Calc grid layout.
            int xinc = _itemdSize.Width + Spacing;
            int yinc = _itemdSize.Height + Spacing;

            // Configure item draw.
            for (int i = 0; i < _itemds.Count; i++)
            {
                int row = i / NumColumns;
                int col = i % NumColumns;
                int xloc = xinc * col + Spacing;
                int yloc = yinc * row + Spacing;

                _itemds[i].Location = new Point(xloc, yloc);
            }
        }

        /// <summary>Resize the image to the specified width and height.</summary>
        /// <param name="bmp">The image to resize.</param>
        /// <param name="width">The width to resize to.</param>
        /// <param name="height">The height to resize to.</param>
        /// <returns>The resized image.</returns>
        Bitmap ResizeBitmap(Bitmap bmp, int width, int height)
        {
            Bitmap result = new(width, height);
            result.SetResolution(bmp.HorizontalResolution, bmp.VerticalResolution);

            using (Graphics graphics = Graphics.FromImage(result))
            {
                // Set high quality.
                graphics.CompositingQuality = CompositingQuality.HighQuality;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.SmoothingMode = SmoothingMode.HighQuality;
                graphics.CompositingQuality = CompositingQuality.HighQuality;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.SmoothingMode = SmoothingMode.HighQuality;

                // Draw the image.
                graphics.DrawImage(bmp, 0, 0, result.Width, result.Height);
            }

            return result;
        }

        /// <summary>
        /// Get the item safely.
        /// </summary>
        /// <param name="item">ItemDisplay to test</param>
        /// <returns>The item if valid</returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException"></exception>
        ItemDisplay GetItemd(object? item)
        {
            ArgumentNullException.ThrowIfNull(item);
            if (item.GetType() != typeof(ItemDisplay)) throw new ArgumentException("Invalid type");

            var itemd = (ItemDisplay)item;
            return itemd;
        }

        /// <summary>
        /// Get the index in the collection.
        /// </summary>
        /// <param name="item">ItemDisplay to test</param>
        /// <returns>Index or -1 if invalid</returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException"></exception>
        int GetItemIndex(object? item)
        {
            var itemd = GetItemd(item);
            int index = _itemds.IndexOf(itemd);
            return index;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="line"></param>
        void TraceLine(string line)
        {
            Trace?.Invoke(this, new() { Line = line });
        }

        /// <summary>
        /// Diagnostic.
        /// </summary>
        /// <param name="state"></param>
        void TraceState(string state)
        {
            Trace?.Invoke(this, new() { State = state });
        }
        #endregion
    }
}
