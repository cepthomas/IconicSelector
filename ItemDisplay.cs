
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
    /// <summary>
    /// One selectable item.
    /// Differentiates start DragAndDrop from simple click.
    /// </summary>
    [ToolboxItem(false), Browsable(false)] // not useable in designer
    public class ItemDisplay : UserControl
    {
        #region Properties
        /// <summary>The owned item.</summary>
        public Item Item { get; init; }

        /// <summary>Geometry.</summary>
        public Rectangle ImageRect { get; init; } = new();

        /// <summary>Geometry.</summary>
        public Rectangle TextRect { get; init; } = new();

        /// <summary>Cosmetics.</summary>
        public Color IndicatorColor { get; set; } = Color.Aqua;

        /// <summary></summary>
        public bool Selected = false;
        #endregion



public bool AllowExternalDrop = false; //TODO1



        #region Events
        public event EventHandler<MouseEventArgs>? DoMouseClick;
        //public event EventHandler<MouseEventArgs>? StartDragDrop;
        public event EventHandler<DroppedDataEventArgs>? DroppedData;
        #endregion

        #region Fields
        Point _dragStart;
        bool _dragging = false;
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
            return $"item:{Item} sel:{Selected}";
        }
        #endregion

        #region Drag and drop
        /// <summary>
        /// Check if the data being dragged contains file paths etc.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnDragEnter(DragEventArgs e)
        {
            DroppedDataType tgttype = GetTargetType(e);

            switch (tgttype)
            {
                case DroppedDataType.Item:
                    e.Effect = DragDropEffects.Move;
                    break;

                case DroppedDataType.File:
                case DroppedDataType.Url:
                    e.Effect = AllowExternalDrop ? DragDropEffects.Copy : DragDropEffects.None;
                    break;

                default:
                    e.Effect = DragDropEffects.None; // Reject the drop
                    break;
            }

            base.OnDragEnter(e);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="e"></param>
        protected override void OnDragOver(DragEventArgs e)
        {
            // <param name="index">which display</param>
            // <param name="xpos">where in display</param>
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

                TraceState($"OnDragOver index:{index} xpos:{xpos} _insertIndex:{_insertIndex} ");
            }

            base.OnDragOver(e);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="e"></param>
        protected override void OnDragEnter(DragEventArgs e)
        {
            DroppedDataType tgttype = GetTargetType(e);

            switch (tgttype)
            {
                case DroppedDataType.Item:
                    e.Effect = DragDropEffects.Move;
                    break;

                case DroppedDataType.File:
                case DroppedDataType.Url:
                    e.Effect = AllowExternalDrop ? DragDropEffects.Copy : DragDropEffects.None;
                    break;

                default:
                    e.Effect = DragDropEffects.None; // Reject the drop
                    break;
            }

            base.OnDragEnter(e);
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="e"></param>
        protected override void OnDragLeave(EventArgs e)
        {
            // ????
            base.OnDragLeave(e);
        }

        protected override void OnDragDrop(DragEventArgs e)
        {
            if (e.Data is null) throw new InvalidOperationException();
            //int index = GetItemIndex(sender);

            DroppedDataType tgttype = GetTargetType(e);

            switch (tgttype)
            {
                case DroppedDataType.Item:
                    var data = e.Data.GetData(typeof(ItemDisplay));
                    var src = (ItemDisplay)data!;

                    //TraceLine($"OnDragDrop() index:{index} items:{_itemds.Count}");

                    DroppedData?.Invoke(this, new(DroppedDataType.Item, src));
                    break;

                case DroppedDataType.File:
                    if (AllowExternalDrop)
                    {
                        var fdata = (string[]?)e.Data.GetData(DataFormats.FileDrop) ?? [];
                        fdata.ForEach(fn => DroppedData?.Invoke(this, new(DroppedDataType.File, fn)));
                    }
                    break;

                case DroppedDataType.Url:
                    if (AllowExternalDrop)
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
                            DroppedData?.Invoke(this, new(DroppedDataType.Url, fullurl));
                        });
                    }
                    break;

                default:
                    // TODO1
                    break;
            }

            base.OnDragDrop(e);
        }
        #endregion

        /// <summary>
        /// 
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        DroppedDataType GetTargetType(DragEventArgs e)
        {
            DroppedDataType ttype = DroppedDataType.None;

            if (e.Data is null) throw new InvalidOperationException();

            var dt = e.Data.GetFormats();

            //if (dt.Contains("System.Int32"))
            if (dt.Contains("Ephemera.IconicSelector.ItemDisplay"))
            {
                ttype = DroppedDataType.Item;
            }
            else if (dt.Contains(DataFormats.FileDrop))
            {
                ttype = DroppedDataType.File;
            }
            else if (dt.Contains(DataFormats.Html))
            {
                ttype = DroppedDataType.Url;
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
            pe.Graphics.Clear(BackColor);

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
                int box = 3;
                Rectangle rect = ClientRectangle;
                //rect.Inflate(-box, -box);
                using Pen pen = new(IndicatorColor, box);
                pe.Graphics.DrawRectangle(pen, rect);
            }

            base.OnPaint(pe);
        }
        #endregion
    }
}
