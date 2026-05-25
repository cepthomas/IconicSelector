using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Ephemera.NBagOfTricks;


namespace Ephemera.IconicSelector
{
    #region Types
    /// <summary>Where the cursor is in ItemDisplay.</summary>
    internal enum CursorLocation { None, Left, Right, Center }

    /// <summary>Drag and drop payload data type.</summary>
    internal enum DroppedDataType { Item, File, Url } // File could be dir, lnk, ...

    /// <summary>User drag-dropped something from elsewhere.</summary>
    internal class DroppedPayloadEventArgs(DroppedDataType dataType, object payload) : EventArgs
    {
        /// <summary>The payload type</summary>
        public DroppedDataType DataType { get; init; } = dataType;

        /// <summary>The drag source</summary>
        public object Payload { get; init; } = payload;

        /// <summary>Read me.</summary>
        public override string ToString() { return $"DataType:{DataType} Payload:[{Payload}]"; }
    }

    /// <summary>User moving over item.</summary>
    internal class CursorLocationEventArgs(CursorLocation cloc) : EventArgs
    {
        /// <summary>The payload type</summary>
        public CursorLocation Location { get; init; } = cloc;

        /// <summary>Read me.</summary>
        public override string ToString() { return $"Location:{Location}"; }
    }
    #endregion

    /// <summary>
    /// One selectable item. Differentiates start DragAndDrop from simple click.
    /// </summary>
    [ToolboxItem(false), Browsable(false)] // not useable in designer
    internal class ItemDisplay : UserControl
    {
        #region Properties - instance
        /// <summary>The owned item.</summary>
        public Item Item { get; init; }

        /// <summary></summary>
        public bool Selected = false;
        #endregion

        #region Properties - class
        /// <summary>Allow drag and drop frome external sources - file/folder/url only.</summary>
        public static bool AllowExternalSource { get; set; } = false;

        /// <summary>Cosmetics.</summary>
        public static Color IndicatorColor { get; set; } = Color.Aqua;

        /// <summary>Image placement inside control.</summary>
        public static Rectangle ImageRect { get; set; } = new();

        /// <summary>Text placement inside control.</summary>
        public static Rectangle TextRect { get; set; } = new();

        /// <summary>Total control real estate.</summary>
        public static Size DisplaySize { get; set; } = new();
        #endregion

        #region Events
        public event EventHandler<MouseEventArgs>? DoMouseClick;
        public event EventHandler<DroppedPayloadEventArgs>? DroppedPayload;
        public event EventHandler<CursorLocationEventArgs>? CursorLocationChanged;
        #endregion

        #region Fields
        /// <summary>For differentiating drag from click.</summary>
        Point _dragStart;

        /// <summary>For tracking cursor position inside an ItemDisplay control.</summary>
        CursorLocation _cursorLoc = CursorLocation.None;

        /// <summary>Indicates focus.</summary>
        static Color _focusColor = DefaultBackColor;

        /// <summary>Indicates one of the controls is currently dragging.</summary>
        static bool _dragging = false;
        #endregion

        #region Lifecycle
        /// <summary>
        /// Constructor.
        /// </summary>
        public ItemDisplay(Item item)
        {
            SetStyle(ControlStyles.DoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint, true);
            Item = item;
            AllowDrop = true;
        }

        /// <summary>
        /// Post creation.
        /// </summary>
        protected override void OnLoad(EventArgs e)
        {
            int shift = 10;
            _focusColor = Color.FromArgb(BackColor.R - shift, BackColor.G - shift, BackColor.B - shift);

            base.OnLoad(e);
        }

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">True if managed resources should be disposed.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                Item.Dispose();
            }
            base.Dispose(disposing);
        }
        #endregion

        #region Drag and drop        
        /// <summary>
        /// Sets the drop effect.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnDragEnter(DragEventArgs e)
        {
            if (e.Data is not null)
            {
                if (e.Data.GetDataPresent(typeof(ItemDisplay)))
                {
                    e.Effect = DragDropEffects.Move;
                }
                else if (e.Data.GetDataPresent(DataFormats.FileDrop))
                {
                    e.Effect = AllowExternalSource ? DragDropEffects.Copy : DragDropEffects.None;
                }
                else if (e.Data.GetDataPresent(DataFormats.Html))
                {
                    e.Effect = AllowExternalSource ? DragDropEffects.Copy : DragDropEffects.None;
                }
                else
                {
                    e.Effect = DragDropEffects.None;
                }

                Tell($"OnDragEnter() e.Effect:{e.Effect}");
            }

            base.OnDragEnter(e);
        }

        /// <summary>
        /// Determine behavior based on location in control.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnDragOver(DragEventArgs e)
        {
            var pt = PointToClient(new Point(e.X, e.Y));
            CursorLocation newLoc;

            if (pt.X < (Width / 4)) { newLoc = CursorLocation.Left; }
            else if (pt.X > (Width * 3 / 4)) { newLoc = CursorLocation.Right; }
            else { newLoc = CursorLocation.Center; }

            if (newLoc != _cursorLoc)
            {
                //Tell($"OnDragOver() CursorLocationChanged new:{newLoc} last:{_lastCursorLoc}");
                CursorLocationChanged?.Invoke(this, new(newLoc));
            }

            _cursorLoc = newLoc;

            base.OnDragOver(e);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="e"></param>
        protected override void OnDragLeave(EventArgs e)
        {
            Tell($"OnDragLeave()");
            _cursorLoc = CursorLocation.None;
            CursorLocationChanged?.Invoke(this, new(_cursorLoc));

            base.OnDragLeave(e);
        }

        /// <summary>
        /// Process dropped payload.
        /// </summary>
        /// <param name="e"></param>
        /// <exception cref="InvalidOperationException"></exception>
        protected override void OnDragDrop(DragEventArgs e)
        {
            if (e.Data is null) throw new InvalidOperationException();
            if (_cursorLoc == CursorLocation.None || _cursorLoc == CursorLocation.Center) return;

            if (e.Data.GetDataPresent(typeof(ItemDisplay)))
            {
                // Pass along as is.
                var itemd = e.Data.GetData(typeof(ItemDisplay));
                var src = (ItemDisplay)itemd!;
                DroppedPayload?.Invoke(this, new(DroppedDataType.Item, src));
            }
            else if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                // Flatten to one event per drop.
                var d = (string[])e.Data.GetData(DataFormats.FileDrop)!;
                d.ForEach(path => { DroppedPayload?.Invoke(this, new(DroppedDataType.File, path)); });
            }
            else if (e.Data.GetDataPresent(DataFormats.Html))
            {
                // Extract url from standard format.
                var s = (string)e.Data.GetData(DataFormats.Html)!;
                var parts = s.SplitByToken(Environment.NewLine);
                parts.Where(p => p.Contains("<!--StartFragment")).ForEach(p =>
                {
                    //<!--StartFragment--><A HREF="https://www.aaa.com/watch?what">Title</A>
                    int start = p.IndexOf("http");
                    int end = p.IndexOf("\">", start);
                    var fullurl = p[start..end];
                    DroppedPayload?.Invoke(this, new(DroppedDataType.Url, fullurl));
                });
            }

            base.OnDragDrop(e);
        }
        #endregion

        #region Mouse events
        /// <summary>
        /// 
        /// </summary>
        /// <param name="e"></param>
        protected override void OnMouseDown(MouseEventArgs e)
        {
            // Record the starting point of the click
            _dragStart = e.Location;
            _dragging = false;

            base.OnMouseDown(e);
        }

        /// <summary>
        /// Determine if this is a drag start.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnMouseMove(MouseEventArgs e)
        {
            bool handled = false;
            if (e.Button == MouseButtons.Left)
            {
                // Calculate how far the mouse has moved
                int deltaX = Math.Abs(e.X - _dragStart.X);
                int deltaY = Math.Abs(e.Y - _dragStart.Y);

                // Use system metrics for the drag threshold (usually 4x4 pixels)
                if (!_dragging && (deltaX > SystemInformation.DragSize.Width || deltaY > SystemInformation.DragSize.Height))
                {
                    _dragging = true;
                    DoDragDrop(this, DragDropEffects.Move);
                    handled = true;
                }
            }

            if (!handled) base.OnMouseMove(e);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="e"></param>
        protected override void OnMouseUp(MouseEventArgs e)
        {
            bool handled = false;
            if (!_dragging)
            {
                // This was just a click, not a drag!
                DoMouseClick?.Invoke(this, e);
            }

            if (!handled) base.OnMouseUp(e);
        }
        #endregion

        #region Drawing
        /// <summary>
        /// Draw me.
        /// </summary>
        /// <param name="pe"></param>
        protected override void OnPaint(PaintEventArgs pe)
        {
            pe.Graphics.Clear(_cursorLoc != CursorLocation.None ? _focusColor : BackColor);

            // Main content.
            if (!ImageRect.IsEmpty)
            {
                if (Item.Bitmap != null)
                {
                    pe.Graphics.DrawImage(Item.Bitmap, ImageRect);
                }
                else
                {
                    Rectangle rect = ClientRectangle;
                    rect.Inflate(-10, -10);
                    pe.Graphics.FillRectangle(Brushes.Crimson, rect);
                }
            }

            if (!TextRect.IsEmpty)
            {
                using StringFormat sfmt = new() { LineAlignment = StringAlignment.Center, Alignment = StringAlignment.Center };
                pe.Graphics.DrawString(Item.Caption, Font, Brushes.Black, TextRect, sfmt);
            }

            if (Selected) // Draw selection box.
            {
                Rectangle rect = ClientRectangle;
                int boxsz = 3;
                rect.Inflate(-boxsz, -boxsz);
                using Pen pen = new(IndicatorColor, boxsz);
                pe.Graphics.DrawRectangle(pen, rect);
            }

            base.OnPaint(pe);
        }
        #endregion

        #region Misc
        /// <summary>Read me</summary>
        public override string ToString()
        {
            //return $"Location:{Location} Selected:{Selected} Item:{Item}";
            return $"Location:{Location} Size:{Size} Caption:{Item.Caption}";
        }

        /// <summary>
        /// Hello.
        /// </summary>
        /// <param name="s"></param>
        void Tell(string s)
        {
            Console.WriteLine($"DISP {s}");
        }
        #endregion
    }
}
