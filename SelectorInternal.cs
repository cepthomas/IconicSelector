using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;
using System.Net.Http;
using System.Threading.Tasks;
using System.IO;
using Ephemera.NBagOfTricks;
using Ephemera.NBagOfUis;


namespace Ephemera.IconicSelector
{
    /// <summary>Master control internals.</summary>
    public partial class Selector : UserControl
    {
        #region Fields
        /// <summary>Current configg.</summary>
        Config _config = new();

        /// <summary>All entries in the collection.</summary>
        readonly List<ItemDisplay> _itemds = [];

        /// <summary>If no valid image available.</summary>
        Bitmap _defaultImage;

        /// <summary>ItemDisplay geometry.</summary>
        Size _itemdSize;

        /// <summary>Where to move/insert item.</summary>
        int _insertIndex = NOT_IN_TARGET;

        /// <summary>Meta index.</summary>
        const int NOT_IN_TARGET = -1;

        /// <summary>Meta index.</summary>
        const int IN_TARGET_CENTER = -2;

        /// <summary>For dirs.</summary>
        Bitmap _bmpDir;

        /// <summary>If no favicon available.</summary>
        Bitmap _bmpUrl;

        /// <summary>Extra info.</summary>
        readonly ToolTip toolTip = new();
        #endregion

        #region Events
        /// <summary>Tell client that item was clicked - OpMode = Click.</summary>
        public new event EventHandler<ClickEventArgs>? Click;
        #endregion

        #region Lifecycle
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
                _bmpDir?.Dispose();
                _bmpUrl?.Dispose();
                _config.DefaultImage?.Dispose();
            }
            base.Dispose(disposing);
        }
        #endregion

        #region ItemDisplay event handlers
        /// <summary>
        /// Handle cursor change.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Itemd_CursorLocationChanged(object? sender, CursorLocationEventArgs e)
        {
            int index = GetItemIndex(sender);

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

            switch (e.Button, _config.Mode, itemd.Selected)
            {

                case (MouseButtons.Left, OpMode.Click, _):
                    Click?.Invoke(this, new(itemd.Item));
                    break;

                case (MouseButtons.Left, OpMode.SingleSelect, true):
                    itemd.Selected = false;
                    Click?.Invoke(this, new(null));
                    break;

                case (MouseButtons.Left, OpMode.SingleSelect, false):
                    // Deselect others first.
                    _itemds.ForEach(itemd => itemd.Selected = false);
                    // Select this one.
                    itemd.Selected = true;
                    Click?.Invoke(this, new(null));
                    break;

                case (MouseButtons.Left, OpMode.MultiSelect, true):
                    itemd.Selected = false;
                    Click?.Invoke(this, new(null));
                    break;

                case (MouseButtons.Left, OpMode.MultiSelect, false):
                    itemd.Selected = true;
                    Click?.Invoke(this, new(null));
                    break;

                case (_, _, _):
                    // ignored
                    break;
            }

            Invalidate(true); // refresh everything
        }

        /// <summary>
        /// Handle dropped data.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Itemd_DroppedPayload(object? sender, DroppedPayloadEventArgs e)
        {
            int index = GetItemIndex(sender);
            Tell($"Itemd_DroppedPayload() index:{index} e:{e}");

            switch (e.DataType)
            {
                case DroppedDataType.Item: // handle here
                    var draggedItem = (ItemDisplay)e.Payload;
                    Tell($"Dropped item -> [{draggedItem}]");
                    // Insert a copy of the dragged item at the insert index.
                    Item item = draggedItem.Item;
                    AddItem(ItemDataType.Item, item.Caption, item.Bitmap, item.Value, _insertIndex);
                    // Remove the original dragged item.
                    RemoveItem(draggedItem);
                    break;

                case DroppedDataType.File:
                case DroppedDataType.Url:
                    var res = (string)e.Payload;
                    AddResourceItem(res, index);
                    break;

                default:
                    throw new InvalidOperationException();
            }

            _insertIndex = NOT_IN_TARGET;
            Invalidate(true);
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
                using Pen pen = new(_config.IndicatorColor, 4);
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

        #region Internals
        /// <summary>
        /// Common function to add a new item. Adjusts image for mode.
        /// </summary>
        /// <param name="dtype">The value type</param>
        /// <param name="caption">For display below/next to image</param>
        /// <param name="bmp">Bitmap</param>
        /// <param name="value">Meaningful for client use</param>
        /// <param name="index">Where to insert, -1 is append</param>
        void AddItem(ItemDataType dtype, string caption, Bitmap bmp, object value, int index = -1)
        {
            switch (_config.Style)
            {
                case SelectorStyle.Icon:
                    // Use image as provided.
                    break;

                case SelectorStyle.Tile:
                    // Use image as provided.
                    break;

                case SelectorStyle.Clip:
                    // Copy pixels starting from 0, 0 to fill the visible area.
                    PixelBitmap pbmpin = new(bmp);
                    PixelBitmap pbmpout = new(_config.ImageSize.Width, _config.ImageSize.Height);

                    for (int x = 0; x < _config.ImageSize.Width && x < bmp.Width; x++)
                    {
                        for (int y = 0; y < _config.ImageSize.Height && y < bmp.Height; y++)
                        {
                            pbmpout.SetPixel(x, y, pbmpin.GetPixel(x, y));
                        }
                    }

                    bmp = pbmpout.GetBitmap();
                    pbmpin.Dispose();
                    pbmpout.Dispose();
                    break;

                case SelectorStyle.Fill:
                    bmp = MiscUtils.ResizeBitmap(bmp, _config.ImageSize.Width, _config.ImageSize.Height);
                    break;

                case SelectorStyle.FitHeight:
                    {
                        float ratio = (float)_itemdSize.Height / bmp.Height;
                        int tnWidth = (int)(bmp.Width * ratio);
                        int tnHeight = (int)(bmp.Height * ratio);
                        var bmpt = MiscUtils.ResizeBitmap(bmp, tnWidth, tnHeight);
                        bmp = bmpt.Clone(new(0, 0, _itemdSize.Width, _itemdSize.Height), PixelFormat.Format32bppArgb);
                    }
                    break;

                case SelectorStyle.FitWidth:
                    {
                        float ratio = (float)_itemdSize.Width / bmp.Width;
                        int tnHeight = (int)(bmp.Height * ratio);
                        int tnWidth = (int)(bmp.Width * ratio);
                        var bmpt = MiscUtils.ResizeBitmap(bmp, tnWidth, tnHeight);
                        bmp = bmpt.Clone(new(0, 0, _itemdSize.Width, _itemdSize.Height), PixelFormat.Format32bppArgb);
                    }
                    break;
            }

            Item item = new(dtype, caption, bmp, value);

            ItemDisplay itemd = new(item)
            {
                // Init instance properties.
                Size = _itemdSize,
                Font = _config.DrawFont
            };
            // Hook events.
            itemd.DoMouseClick += Itemd_DoMouseClick;
            itemd.DroppedPayload += Itemd_DroppedPayload;
            itemd.CursorLocationChanged += Itemd_CursorLocationChanged;
            toolTip.SetToolTip(itemd, value.ToString());

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
        /// Called after list changes.
        /// </summary>
        void UpdateItemsList()
        {
            // Calc grid layout.
            int xinc = _itemdSize.Width + _config.Spacing;
            int yinc = _itemdSize.Height + _config.Spacing;

            // Configure item draw.
            for (int i = 0; i < _itemds.Count; i++)
            {
                int row = i / _config.NumColumns;
                int col = i % _config.NumColumns;
                int xloc = xinc * col + _config.Spacing;
                int yloc = yinc * row + _config.Spacing;

                _itemds[i].Location = new Point(xloc, yloc);
            }
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
        /// Get the item display.
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
        /// Debug.
        /// </summary>
        /// <param name="line"></param>
        void Tell(string line)
        {
            Console.WriteLine($"SLCT {line}");
        }
        #endregion
    }
}
