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
using Ephemera.NBagOfTricks;
using Ephemera.NBagOfUis;


namespace Ephemera.IconicSelector
{
    /// <summary>Master control.</summary>
    public class Selector : UserControl
    {
        #region Properties
        // /// <summary>Select style.</summary>
        // public SelectorStyle Style { set { Init(value); } }

        /// <summary>What the mouse click does.</summary>
        public MouseFunction LeftMouseClick { get; set; } = MouseFunction.Click;

        /// <summary>Allow drag and drop (files) from other applications.</summary>
        public bool AllowExternalDrop { get; set; } = false;

        /// <summary>Image size.</summary>
        public int ImageSize { get; set; } = 32;

        /// <summary>Cosmetics.</summary>
        public Font DrawFont { get; set; } = new("Calibri", 11, FontStyle.Regular, GraphicsUnit.Point, 0);

        /// <summary>Cosmetics.</summary>
        public Color TargetColor { get; set; } = Color.Aqua;

        /// <summary>Visual space at edges.</summary>
        public int Pad { get; set; } = 4;

        /// <summary>Space between items</summary>
        public int Spacing { get; set; } = 10;
        #endregion

        #region Fields
        // /// <summary>Current config.</summary>
        //SelectorStyle _style;
        //ItemGeometry _geometry;

        /// <summary>All entries in the collection.</summary>
        readonly List<ItemDisplay> _itemds = [];

        /// <summary>If no valid image available.</summary>
        Bitmap _defaultImage;

        /// <summary>ItemDisplay geometry.</summary>
        Point _imageLoc;

        /// <summary>ItemDisplay geometry.</summary>
        Rectangle _textRect;

        /// <summary>ItemDisplay geometry.</summary>
        Size _itemSize;
        #endregion




        #region Events
        /// <summary></summary>
        public event EventHandler<SelectionEventArgs>? Selection;

        /// <summary></summary>
        public event EventHandler<DroppedTargetEventArgs>? DroppedTarget;

        /// <summary>Debug hook</summary>
        public event EventHandler<string>? Trace;
        #endregion

        #region Lifecycle
        /// <summary>
        /// Constructor.
        /// </summary>
        public Selector()
        {
            // Init myself.
            AllowDrop = true;
            AutoScroll = true;
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        public void Init(SelectorStyle style)
        {
            switch (style)
            {
                case SelectorStyle.Tile:
                    _imageLoc = new(Pad, Pad);
                    _textRect = new(Pad + ImageSize + Pad, Pad, ImageSize / 2 - Pad, ImageSize - Pad * 2);
                    _itemSize = new(ImageSize * 3 + Pad * 3, ImageSize + Pad * 2);
                    break;

                default:
                case SelectorStyle.Icon:
                    _imageLoc = new(Pad + ImageSize / 2, Pad);
                    _textRect = new(Pad, Pad + ImageSize, ImageSize - Pad * 2, ImageSize / 2 - Pad);
                    _itemSize = new(ImageSize * 2 + Pad * 2, ImageSize * 2 + Pad * 2);
                    break;
            }

            // Make a default image. Big X.
            _defaultImage = new(ImageSize, ImageSize);
            using (Graphics gr = Graphics.FromImage(_defaultImage))
            {
                Pen pen = new(Color.Purple, 4);
                int pad = 2;
                int sz = ImageSize - 2 * pad;
                gr.DrawLine(pen, pad, pad, sz, sz);
                gr.DrawLine(pen, pad, sz, sz, pad);
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
                _defaultImage?.Dispose();
            }
            base.Dispose(disposing);
        }
        #endregion

        #region API
        /// <summary>
        /// Add a new item. If insertion mark is visible, insert there, otherwise append.
        /// </summary>
        /// <param name="text">For display below/next to image</param>
        /// <param name="bmp">Bitmap</param>
        /// <param name="value">Meaningful for client use</param>
        /// <param name="index">Where to insert. -1 is append</param>
        public void AddItem(string text, Bitmap? bmp, object value, int index = -1)
        {
            Item item = new()
            {
                Caption = text,
                Bitmap = bmp is null ? _defaultImage : bmp.Resize(ImageSize, ImageSize),
                Value = value,
            };


            ItemDisplay itemd = new(item) //, _geometry);
            {
                TargetColor = TargetColor,
                ImageLoc = _imageLoc,
                TextRect = _textRect,
                Size = _itemSize,
            };

            itemd.MouseClick += Item_MouseClick;
            itemd.MouseDown += Item_MouseDown;
            // itemd.QueryContinueDrag += Item_QueryContinueDrag;
            // itemd.MouseUp += Item_MouseUp;
            // itemd.MouseMove += Item_MouseMove;
            itemd.DragOver += Item_DragOver;
            itemd.DragDrop += Item_DragDrop;
            itemd.DragEnter += Item_DragEnter;
            itemd.DragLeave += Item_DragLeave;

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
        /// 
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
        /// 
        /// </summary>
        public void RemoveSelectedItems()
        {
            _itemds.Where(itemd => itemd.Selected).ForEach(itemd => { RemoveItem(itemd); });
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
            int xinc = _itemSize.Width + Spacing;
            int yinc = _itemSize.Height + Spacing;
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

            base.OnPaint(pe);
        }
        #endregion

        #region Standard events
        /// <summary>
        /// User item selection(s).
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Item_MouseClick(object? sender, MouseEventArgs e)// TODO doesn't fire if DandD - prob need mousedown/up
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
        }
        #endregion

        #region Drag and drop
        /// <summary>
        /// Starts the drag-and-drop operation when an item is dragged.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Item_MouseDown(object? sender, MouseEventArgs e)
        {
            int index = GetItemIndex(sender);

            Trace?.Invoke(this, $"Item_MouseDown() index:{index}");

            DoDragDrop(index, DragDropEffects.Move);
        }

        /// <summary>
        /// Sets the target drop effect.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Item_DragEnter(object? sender, DragEventArgs e)
        {
            int index = GetItemIndex(sender);
            if (e.Data is null) throw new InvalidOperationException();
            var srcIndex = e.Data!.GetData(typeof(int));

            if (srcIndex is not null && index != (int)srcIndex)
            {
                Trace?.Invoke(this, $"Item_DragEnter() index:{index}");
                e.Effect = e.AllowedEffect;

                SetTarget(index);
            }

            for (int i = 0; i < _itemds.Count; i++)
            {
                if (_itemds[i].IsTarget) Trace?.Invoke(this, $"IsTarget:{i}");
            }

            Invalidate();
        }

        /// <summary>
        /// Moves the insertion mark as the item is dragged.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Item_DragOver(object? sender, DragEventArgs e)
        {
            // Nothing?
            //int index = GetItemIndex(sender);
            //var itemd = _itemds[index];
            //Trace?.Invoke(this, $"Item_DragOver() index:{index} IsTarget:{itemd.IsTarget}");
            //Invalidate();
        }

        /// <summary>
        /// Removes the insertion mark when the mouse leaves the control.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>d
        void Item_DragLeave(object? sender, EventArgs e)
        {
            int index = GetItemIndex(sender);

            Trace?.Invoke(this, $"Item_DragLeave() index:{index}");

            SetTarget(-1);

            Invalidate();
        }

        /// <summary>
        /// Moves the item to the location of the insertion mark.
        /// Handles drag sources of internal items and external files.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Item_DragDrop(object? sender, DragEventArgs e)
        {
            int index = GetItemIndex(sender);
            if (e.Data is null) throw new InvalidOperationException();

            Trace?.Invoke(this, $"Item_DragDrop() index:{index}");

            // What do we have here?
            var dt = e.Data.GetFormats();

            if (dt.Contains("System.Int32"))
            {
                var idata = e.Data.GetData(typeof(int));
                int srcIndex = (int)idata!;

                var draggedItem = _itemds[srcIndex];
                // Insert a copy of the dragged item at the target index.
                //Item copy = new(draggedItem.Item);
                Item it = draggedItem.Item;
                AddItem(it.Caption, it.Bitmap, it.Value, index);
                // Remove the original dragged item.
                RemoveItem(draggedItem);

                SetTarget(-1);

                return;
            }

            if (dt.Contains(DataFormats.FileDrop) && AllowExternalDrop)
            {
                var fdata = e.Data.GetData(DataFormats.FileDrop);
                foreach (string target in (string[])fdata!)
                {
                    Trace?.Invoke(this, $"Dropped file -> [{target}]");
                    var icon = GraphicsUtils.ExtractIconFromExecutable(target, 0, true);
                    var fn = Path.GetFileName(target);
                    AddItem(fn, icon?.ToBitmap(), target);
                }
                return;
            }

            if (dt.Contains(DataFormats.Html) && AllowExternalDrop)
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
                    Trace?.Invoke(this, $"Dropped url -> [{fullurl}]");

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
                        Trace?.Invoke(this, $"Request failed - use default [{ex.Message}]");
                        AddItem(uri.Host, _defaultImage, fullurl);
                    }
                    catch (Exception)
                    {
                        // Client handles.
                        throw;
                    }
                });

                return;
            }

            // else ignore

            Invalidate();
        }
        #endregion

        #region Internals
        /// <summary>
        /// Set the target property for specific item. Clears all others.
        /// </summary>
        /// <param name="index">If -1 clear all</param>
        void SetTarget(int index)
        {
            Trace?.Invoke(this, $"SetTarget {index}");
            for (int i = 0; i < _itemds.Count; i++)
            {
                _itemds[i].IsTarget = index == i;
            }
        }

        /// <summary>
        /// Get the index in the collection
        /// </summary>
        /// <param name="item">ItemDisplay to test</param>
        /// <returns>Index or -1 if invalid</returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException"></exception>
        int GetItemIndex(object? item)
        {
            ArgumentNullException.ThrowIfNull(item);
            if (item.GetType() != typeof(ItemDisplay)) throw new ArgumentException("Invalid type");

            var itemd = (ItemDisplay)item;
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
        #endregion
    }
}
