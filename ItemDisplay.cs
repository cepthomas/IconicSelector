
using System;
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
using Ephemera.NBagOfTricks;
using System.Net.Http;


namespace Ephemera.IconicSelector
{
    /// <summary>Drag and drop payload data.</summary>
    internal enum DroppedPayloadType
    {
        /// <summary>Default</summary>
        None,
        /// <summary>File name</summary>
        File,
        /// <summary>URL</summary>
        Url,
        /// <summary>Internal - DisplayItem</summary>
        Item
    }

    /// <summary>Where the cursor is in ItemDisplay.</summary>
    internal enum CursorLocation
    {
        /// <summary>Default</summary>
        None,
        /// <summary></summary>
        Left,
        /// <summary></summary>
        Right,
        /// <summary></summary>
        Center
    }

    /// <summary>User drag-dropped something from elsewhere.</summary>
    internal class DroppedPayloadEventArgs(DroppedPayloadType tgttype, object payload) : EventArgs
    {
        /// <summary>The payload type</summary>
        public DroppedPayloadType DataType { get; init; } = tgttype;

        /// <summary>The target</summary>
        public object Payload { get; init; } = payload;

        /// <summary>Read me.</summary>
        public override string ToString()
        {
            return $"{DataType} [{Payload}]";
        }
    }

    /// <summary>User moving over item.</summary>
    internal class CursorLocationEventArgs(CursorLocation cloc) : EventArgs
    {
        /// <summary>The payload type</summary>
        public CursorLocation Location { get; init; } = cloc;

        /// <summary>Read me.</summary>
        public override string ToString()
        {
            return $"{Location} [{Location}]";
        }
    }

    /// <summary>
    /// One selectable item.
    /// Differentiates start DragAndDrop from simple click.
    /// </summary>
    [ToolboxItem(false), Browsable(false)] // not useable in designer
    internal class ItemDisplay : UserControl
    {
        #region Properties
        /// <summary>The owned item.</summary>
        public Item Item { get; init; }

        /// <summary></summary>
        public bool Selected = false;

        /// <summary></summary>
        public bool AllowExternalDrop = false;

        /// <summary>Geometry.</summary>
        public Rectangle ImageRect { get; init; } = new();

        /// <summary>Geometry.</summary>
        public Rectangle TextRect { get; init; } = new();

        /// <summary>Cosmetics.</summary>
        public Color IndicatorColor { get; set; } = Color.Aqua;
        #endregion

        #region Events
        public event EventHandler<MouseEventArgs>? DoMouseClick;
        public event EventHandler<DroppedPayloadEventArgs>? DroppedPayload;
        public event EventHandler<CursorLocationEventArgs>? CursorLocationChanged;
        #endregion

        #region Fields
        Point _dragStart;
        bool _dragging = false;
        CursorLocation _lastCursorLoc = CursorLocation.None;
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
        ///  Clean up any resources being used.
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

        /// <summary>Read me</summary>
        public override string ToString()
        {
            return $"Item:{Item} Location:{ Location} Selected:{Selected}";
        }
        #endregion

        #region Drag and drop        
        /// <summary>
        /// Sets the target drop effect.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnDragEnter(DragEventArgs e)
        {
            DroppedPayloadType tgttype = GetTargetType(e);
            //TraceLine($"OnDragEnter() index:{index}");

            e.Effect = tgttype switch
            {
                DroppedPayloadType.Item => DragDropEffects.Move,
                DroppedPayloadType.File or DroppedPayloadType.Url => AllowExternalDrop ? DragDropEffects.Copy : DragDropEffects.None,
                _ => DragDropEffects.None,// Reject the drop
            };

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

            if (pt.X < (Width / 4))
            {
                newLoc = CursorLocation.Left;
            }
            else if (pt.X > (Width * 3 / 4))
            {
                newLoc = CursorLocation.Right;
            }
            else
            {
                newLoc = CursorLocation.Center;
            }

            if (newLoc != _lastCursorLoc)
            {
                CursorLocationChanged?.Invoke(this, new(newLoc));
            }

            _lastCursorLoc = newLoc;

            base.OnDragOver(e);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="e"></param>
        protected override void OnDragLeave(EventArgs e)
        {
            //TraceLine($"OnDragLeave() index:{index}");
            _lastCursorLoc = CursorLocation.None;
            CursorLocationChanged?.Invoke(this, new(_lastCursorLoc));

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
            DroppedPayloadType tgttype = GetTargetType(e);

            if (_lastCursorLoc == CursorLocation.Left || _lastCursorLoc == CursorLocation.Right)
            {
                switch (tgttype)
                {
                    case DroppedPayloadType.Item:
                        var data = e.Data.GetData(typeof(ItemDisplay));
                        var src = (ItemDisplay)data!;
                        DroppedPayload?.Invoke(this, new(DroppedPayloadType.Item, src));
                        break;

                    case DroppedPayloadType.File:
                        var fdata = (string[]?)e.Data.GetData(DataFormats.FileDrop) ?? [];
                        fdata.ForEach(fn => DroppedPayload?.Invoke(this, new(DroppedPayloadType.File, fn)));
                        break;

                    case DroppedPayloadType.Url:
                        var hdata = e.Data.GetData(DataFormats.Html);

                        var s = hdata as string ?? "";
                        var parts = s.SplitByToken(Environment.NewLine);

                        parts.Where(p => p.Contains("<!--StartFragment")).ForEach(p =>
                        {
                            //<!--StartFragment--><A HREF="https://www.aaa.com/watch?what">Title</A>
                            int start = p.IndexOf("http");
                            int end = p.IndexOf("\">", start);
                            var fullurl = p[start..end];
                            DroppedPayload?.Invoke(this, new(DroppedPayloadType.Url, fullurl));
                        });
                        break;

                    default:
                        throw new InvalidOperationException();
                }
            }

            base.OnDragDrop(e);
        }
        #endregion

        /// <summary>
        /// Helper function.
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        DroppedPayloadType GetTargetType(DragEventArgs e)
        {
            DroppedPayloadType ttype = DroppedPayloadType.None;

            if (e.Data is null) throw new InvalidOperationException();

            var dt = e.Data.GetFormats();

            if (dt.Contains("Ephemera.IconicSelector.ItemDisplay"))
            {
                ttype = DroppedPayloadType.Item;
            }
            else if (dt.Contains(DataFormats.FileDrop))
            {
                ttype = DroppedPayloadType.File;
            }
            else if (dt.Contains(DataFormats.Html))
            {
                ttype = DroppedPayloadType.Url;
            }

            return ttype;
        }

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
                    //StartDragDrop?.Invoke(this, e);
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
            //pe.Graphics.Clear(BackColor);
            pe.Graphics.Clear(Color.LightBlue);

            // Main content.
            if (!ImageRect.IsEmpty)
            {
                pe.Graphics.DrawImage(Item.Bitmap, ImageRect);
            }

            if (!TextRect.IsEmpty)
            {
                using StringFormat sfmt = new() { LineAlignment = StringAlignment.Center, Alignment = StringAlignment.Center };
                pe.Graphics.DrawString(Item.Caption, Font, Brushes.Black, TextRect, sfmt);
            }

            if (Selected) // Draw selection box.
            {
                int boxsz = 3;
                Rectangle rect = ClientRectangle;
                //rect.Inflate(-boxsz, -boxsz);
                using Pen pen = new(IndicatorColor, boxsz);
                pe.Graphics.DrawRectangle(pen, rect);
            }

            base.OnPaint(pe);
        }
        #endregion
    }
}
