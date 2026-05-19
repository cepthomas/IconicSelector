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
using System.Drawing.Imaging;


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

    /// <summary>User clicked item.</summary>
    public class ClickEventArgs(Item item) : EventArgs
    {
        /// <summary>The item</summary>
        public Item ClickedItem { get; init; } = item;
    }

    /// <summary>User selection(s) have changed.</summary>
    public class SelectionEventArgs(List<Item> items) : EventArgs
    {
        /// <summary>The selection(s)</summary>
        public List<Item> SelectedItems { get; init; } = items;
    }

    /// <summary>Debugging.</summary>
    public class TraceEventArgs(string line) : EventArgs
    {
        /// <summary>Log line.</summary>
        public string Line { get; set; } = line;
    }
    #endregion


    /// <summary>Master control API.</summary>
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

        /// <summary>Allow drag and drop (files) from other applications.</summary>
        public bool AllowExternalDrop { get; set; } = false;

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

        #region Events
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
        #endregion



        protected override void OnDragEnter(DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effect = DragDropEffects.Copy;
                TraceLine("OnDragEnter");
            }
            else
            {
                e.Effect = DragDropEffects.None;
            }
        }


        protected override void OnDragDrop(DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                TraceLine("OnDragDrop");
            }
        }





        #region Functions
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
                    // Use image as provided.
                    break;

                case SelectorStyle.Tile:
                    // Use image as provided.
                    break;

                case SelectorStyle.Clip:
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

                case SelectorStyle.Fill:
                    bmp = ResizeBitmap(bmp, ImageSize.Width, ImageSize.Height);
                    break;

                case SelectorStyle.FitHeight:
                    {
                        float ratio = (float)_itemdSize.Height / bmp.Height;
                        int tnWidth = (int)(bmp.Width * ratio);
                        int tnHeight = (int)(bmp.Height * ratio);
                        var bmpt = ResizeBitmap(bmp, tnWidth, tnHeight);
                        bmp = bmpt.Clone(new(0, 0, _itemdSize.Width, _itemdSize.Height), PixelFormat.Format32bppArgb);
                    }
                    break;

                case SelectorStyle.FitWidth:
                    {
                        float ratio = (float)_itemdSize.Width / bmp.Width;
                        int tnHeight = (int)(bmp.Height * ratio);
                        int tnWidth = (int)(bmp.Width * ratio);
                        var bmpt = ResizeBitmap(bmp, tnWidth, tnHeight);
                        bmp = bmpt.Clone(new(0, 0, _itemdSize.Width, _itemdSize.Height), PixelFormat.Format32bppArgb);
                    }
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
            itemd.Trace += (object? sender, TraceEventArgs e) => Trace?.Invoke(sender, e);

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
    }
}
