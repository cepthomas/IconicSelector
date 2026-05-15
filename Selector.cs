using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;
using System.Text.Json.Serialization;
using System.Net.Http;
using System.Drawing.Drawing2D;
using Ephemera.NBagOfTricks;
using Ephemera.NBagOfUis;


namespace Ephemera.IconicSelector
{
    /// <summary>Master control.</summary>
    public class Selector : ScrollableControl //UserControl  TODO! scroll   https://www.cyotek.com/blog/creating-a-custom-single-axis-scrolling-control-in-winforms
    {
        #region Properties
        /// <summary>What the mouse click does.</summary>
        public MouseFunction LeftMouseClick { get; set; } = MouseFunction.Click;

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
        /// <summary>Current config.</summary>
        SelectorStyle _style = SelectorStyle.Icon;

        /// <summary>All entries in the collection.</summary>
        readonly List<ItemDisplay> _itemds = [];

        /// <summary>If no valid image available.</summary>
        Bitmap _defaultImage;

        /// <summary>ItemDisplay geometry. TODO! do as needed not cached?</summary>
        Rectangle _itemdImageRect = new();

        /// <summary>ItemDisplay geometry.</summary>
        Rectangle _itemdTextRect = new();

        /// <summary>ItemDisplay geometry.</summary>
        Size _itemdSize;

        /// <summary>Where to move/insert item.</summary>
        int _insertIndex = -1;

        /// <summary>meta indexes</summary>
        const int NOT_IN_TARGET = -1;
        const int IN_TARGET_CENTER = -2;
        #endregion

        #region Events
        /// <summary></summary>
        public event EventHandler<SelectionEventArgs>? Selection;

        /// <summary></summary>
        public event EventHandler<DroppedTargetEventArgs>? DroppedTarget;

        /// <summary>Debug hook</summary>
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
            // Default mode.
            Init(SelectorStyle.Icon);

            _defaultImage = new(32, 32);
            using Graphics gr = Graphics.FromImage(_defaultImage);
            gr.Clear(Color.Cyan);
        }

        /// <summary>
        /// Constructor. Determines geometry of display elements.
        /// <param name="style">Our flavor.</param>
        /// </summary>
        public void Init(SelectorStyle style)
        {
            _style = style;

            // Figure geometry.
            switch (style)
            {
                case SelectorStyle.Icon:
                    {
                        _itemdImageRect = new(Pad + ImageSize.Width / 2, Pad, ImageSize.Width, ImageSize.Height);
                        _itemdTextRect = new(Pad, _itemdImageRect.Bottom + Pad, 2 * ImageSize.Width, ImageSize.Height);
                        _itemdSize = new(_itemdTextRect.Right + Pad, _itemdTextRect.Bottom + Pad);
                    }
                    break;

                case SelectorStyle.Tile:
                    {
                        _itemdImageRect = new(Pad, Pad, ImageSize.Width, ImageSize.Height);
                        _itemdTextRect = new(_itemdImageRect.Right + Pad, Pad, 2 * ImageSize.Width, ImageSize.Height);
                        _itemdSize = new(_itemdTextRect.Right + Pad, _itemdTextRect.Bottom + Pad);
                    }
                    break;

                case SelectorStyle.Image:
                case SelectorStyle.Fit:
                    {
                        _itemdImageRect = new(0, 0, ImageSize.Width, ImageSize.Height);
                        _itemdTextRect = new(); // not used
                        _itemdSize = new(ImageSize.Width, ImageSize.Height);
                    }
                    break;
            }
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

        #region API
        /// <summary>
        /// Add a new item.
        /// </summary>
        /// <param name="text">For display below/next to image</param>
        /// <param name="bmp">Bitmap</param>
        /// <param name="value">Meaningful for client use</param>
        /// <param name="index">Where to insert, -1 is append</param>
        public void AddItem(string text, Bitmap? bmp, object value, int index = -1)
        {
            // Make a new item. Maybe adjust the image.
            bmp ??= _defaultImage;

            switch (_style)
            {
                case SelectorStyle.Icon:
                    // Image as is.
                    break;

                case SelectorStyle.Tile:
                    // Image as is.
                    break;

                case SelectorStyle.Image:
                    // Copy pixels starting from 0, 0 to fill the visible area.
                    //Bitmap bmpout = new(ImageSize.Width, ImageSize.Height);
                    //using (Graphics gr = Graphics.FromImage(bmpout))
                    //{
                    //    gr.Clear(Color.Transparent);
                    //}

                    //// Stupid/slow but infrequent small images. TODO!.
                    //for (int x = 0; x < bmpout.Width; x++)
                    //{
                    //    for (int y = 0; y < bmpout.Height; y++)
                    //    {
                    //        if (x < bmp.Width && y < bmp.Height)
                    //        {
                    //            bmpout.SetPixel(x, y, bmp.GetPixel(x, y));
                    //        }
                    //    }
                    //}

                    bmp = bmpout;
                    break;

                case SelectorStyle.Fit:
                    bmp = ResizeBitmap(bmp, ImageSize.Width, ImageSize.Height);
                    break;
            }

            Item item = new(text, bmp, value);

            ItemDisplay itemd = new(item)
            {
                IndicatorColor = IndicatorColor,
                ImageRect = _itemdImageRect,
                TextRect = _itemdTextRect,
                Size = _itemdSize,
            };

            itemd.DoMouseClick += Itemd_DoMouseClick;
            itemd.StartDragDrop += Itemd_StartDragDrop;
            itemd.DragOver += Itemd_DragOver;
            itemd.DragDrop += Itemd_DragDrop;
            itemd.DragEnter += Itemd_DragEnter;
            itemd.DragLeave += Itemd_DragLeave;

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
        }

        /// <summary>
        /// Item management.
        /// </summary>
        public void RemoveItem(int index)
        {
            if (index >= 0 && index < _itemds.Count)
            {
                var _itemd = _itemds[index];
                RemoveItem(_itemd);
            }
        }

        /// <summary>
        /// Item management.
        /// </summary>
        public void RemoveSelectedItems()
        {
            _itemds.Where(itemd => itemd.Selected).ForEach(itemd => { RemoveItem(itemd); });
        }

        /// <summary>
        /// Get all items.
        /// </summary>
        /// <returns>Items.</returns>
        public List<Item> GetItems()
        {
            List<Item> res = [];
            _itemds.ForEach(itemd => res.Add(itemd.Item));
            return res;
        }

        /// <summary>
        /// Diagnostic.
        /// </summary>
        /// <returns></returns>
        public List<string> Dump()
        {
            List<string> res = [];
            _itemds.ForEach(itemd => res.Add(itemd.Item.ToString()));
            return res;
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

            // Calc grid layout.
            int xinc = _itemdSize.Width + Spacing;
            int yinc = _itemdSize.Height + Spacing;
            int numColumns = Math.Max(1, (Width - Spacing) / xinc);

            // Configure item draw.
            for (int i = 0; i < _itemds.Count; i++)
            {
                int row = i / numColumns;
                int col = i % numColumns;
                int xloc = xinc * col + Spacing;
                int yloc = yinc * row + Spacing;

                _itemds[i].Location = new Point(xloc, yloc);
                _itemds[i].Invalidate();
            }

            if (_insertIndex != -1)
            {
                var itemd = _itemds[_insertIndex];
                var loc = itemd.Location;

                //var pt = PointToClient(new(0, 0));
                using Pen pen = new(IndicatorColor, 4);

                int offset = 3;
                pe.Graphics.DrawLine(pen, loc.X - offset, loc.Y, loc.X - offset, loc.Y + itemd.Height);
            }

            base.OnPaint(pe);
        }
        #endregion

        #region Standard events
        /// <summary>
        /// User item selection(s).
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Itemd_DoMouseClick(object? sender, MouseEventArgs e)
        {
            ArgumentNullException.ThrowIfNull(sender);

            var itemd = (ItemDisplay)sender;
            bool sel = itemd.Selected; // current

            switch (e.Button, LeftMouseClick)
            {
                case (MouseButtons.Left, MouseFunction.Click):
                    Selection?.Invoke(this, new() { SelectedItems = [itemd.Item] });
                    break;

                case (MouseButtons.Left, MouseFunction.SingleSelect):
                    if (sel)
                    {
                        itemd.Selected = false;
                        Selection?.Invoke(this, new()); // no select
                    }
                    else
                    {
                        // Deselect others first.
                        _itemds.ForEach(itemd => itemd.Selected = false);
                        // Select this one.
                        itemd.Selected = true;
                        Selection?.Invoke(this, new() { SelectedItems = [itemd.Item] });
                    }
                    break;

                case (MouseButtons.Left, MouseFunction.MultiSelect):
                    itemd.Selected = !sel;
                    Selection?.Invoke(this, new() { SelectedItems = [.. _itemds.Where(itemd => itemd.Selected).Select(itemd => itemd.Item)] });
                    break;

                case (_, _):
                    // ignored
                    break;
            }

            Invalidate();
        }
        #endregion

        #region Drag and drop
        /// <summary>
        /// Starts the drag-and-drop operation when an item is dragged.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Itemd_StartDragDrop(object? sender, MouseEventArgs e)
        {
            int index = GetItemIndex(sender);

            TraceLine($"Itemd_ItemDragDropStart() index:{index}");

            DoDragDrop(index, DragDropEffects.Move);
        }

        /// <summary>
        /// Sets the target drop effect.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Itemd_DragEnter(object? sender, DragEventArgs e)
        {
            int index = GetItemIndex(sender);
            if (e.Data is null) throw new InvalidOperationException();
            var srcIndex = e.Data!.GetData(typeof(int));

            if (srcIndex is not null && index != (int)srcIndex)
            {
                TraceLine($"Itemd_DragEnter() index:{index}");
                e.Effect = e.AllowedEffect;
            }

            SetInsert(IN_TARGET_CENTER);

            Invalidate();
        }

        /// <summary>
        /// Moves the insertion indicator as the item is dragged.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Itemd_DragOver(object? sender, DragEventArgs e)
        {
            int index = GetItemIndex(sender);
            var itemd = _itemds[index];
            
            TraceState($"Itemd_DragOver index:{index} _insertIndex:{_insertIndex} X:{e.X} Y:{e.Y}");

            var itemdPoint = itemd.PointToClient(new Point(e.X, e.Y));

            SetInsert(index, itemdPoint.X);

            Invalidate();
        }

        /// <summary>
        /// Removes the insertion indicator when the mouse leaves the control.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>d
        void Itemd_DragLeave(object? sender, EventArgs e)
        {
            int index = GetItemIndex(sender);

            TraceLine($"Itemd_DragLeave() index:{index}");

            SetInsert(NOT_IN_TARGET);

            Invalidate();
        }

        /// <summary>
        /// Moves the source item to the insertion indicator.
        /// Handles drag sources of internal items and external files.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Itemd_DragDrop(object? sender, DragEventArgs e)
        {
            if (e.Data is null) throw new InvalidOperationException();
            int index = GetItemIndex(sender);

            TraceLine($"Itemd_DragDrop() index:{index}");

            // What do we have here?
            var dt = e.Data.GetFormats();
            bool handled = _insertIndex < 0;

            if (!handled && dt.Contains("System.Int32"))
            {
                var idata = e.Data.GetData(typeof(int));
                int srcIndex = (int)idata!;

                var draggedItem = _itemds[srcIndex];
                // Insert a copy of the dragged item at the target index.
                Item it = draggedItem.Item;
                AddItem(it.Caption, it.Bitmap, it.Value, index);
                // Remove the original dragged item.
                RemoveItem(draggedItem);

                handled = true;
            }

            if (!handled && dt.Contains(DataFormats.FileDrop) && AllowExternalDrop)
            {
                var fdata = e.Data.GetData(DataFormats.FileDrop);
                foreach (string target in (string[])fdata!)
                {
                    TraceLine($"Dropped file -> [{target}]");
                    var icon = GraphicsUtils.ExtractIconFromExecutable(target, 0, true);
                    var fn = Path.GetFileName(target);
                    AddItem(fn, icon?.ToBitmap(), target);
                }

                handled = true;
            }

            if (!handled && dt.Contains(DataFormats.Html) && AllowExternalDrop)
            {
                var hdata = e.Data.GetData(DataFormats.Html);

                var s = hdata as string ?? "";
                var parts = s.SplitByToken(Environment.NewLine);

                parts.Where(p => p.Contains("<!--StartFragment")).ForEach(p =>
                {
                    //<!--StartFragment--><A HREF="https://www.aaa.com/watch?what">Title</A>
                    int start = p.IndexOf("http");
                    int end = p.IndexOf("\">", start);
                    var fullurl = p[start..end];
                    TraceLine($"Dropped url -> [{fullurl}]");

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
                });

                handled = true;
            }

            if (!handled)
            {
                // ignore
            }

            SetInsert(NOT_IN_TARGET);

            Invalidate();
        }
        #endregion

        #region Internals
        /// <summary>Resize the image to the specified width and height.</summary>
        /// <param name="bmp">The image to resize.</param>
        /// <param name="width">The width to resize to.</param>
        /// <param name="height">The height to resize to.</param>
        /// <returns>The resized image.</returns>
        public Bitmap ResizeBitmap(Bitmap bmp, int width, int height)
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
        /// 
        /// </summary>
        /// <param name="index">which display</param>
        /// <param name="xpos">where in display</param>
        void SetInsert(int index, int xpos = -1)
        {
            if (index == -1) // not in a target
            {
                _insertIndex = NOT_IN_TARGET;
            }
            else // in a target
            {
                // where?
                if (xpos < 0) // shouldn't happen
                {
                    _insertIndex = NOT_IN_TARGET;
                }
                else if (xpos < (_itemdSize.Width / 4)) // at left edge
                {
                    _insertIndex = index;
                }
                else if (xpos > (_itemdSize.Width * 3 / 4)) // at right edge
                {
                    _insertIndex = index + 1;
                }
                else
                {
                    _insertIndex = IN_TARGET_CENTER;
                }
            }

            TraceState($"SetInsert index:{index} xpos:{xpos} _insertIndex:{_insertIndex} ");
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
        /// Remove this item.
        /// </summary>
        /// <param name="itemd"></param>
        /// <exception cref="ArgumentNullException"></exception>
        void RemoveItem(ItemDisplay itemd)
        {
            ArgumentNullException.ThrowIfNull(itemd);

            Controls.Remove(itemd);
            _itemds.Remove(itemd);
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
