
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


namespace Ephemera.IconicSelector
{
    /// <summary>
    /// One selectable item.
    /// Differentiates DragAndDrop from simple click.
    /// </summary>
    [ToolboxItem(false), Browsable(false)] // not useable in designer
    public class ItemDisplay : UserControl
    {
        #region Properties
        /// <summary>The owned item.</summary>
        public Item Item { get; init; }

        /// <summary>Geometry.</summary>
        public DisplayRect ImageRect { get; init; } = new();

        /// <summary>Geometry.</summary>
        public DisplayRect TextRect { get; init; } = new();

        /// <summary>Cosmetics.</summary>
        public Color TargetColor { get; set; } = Color.Aqua;

        /// <summary></summary>
        public bool Selected = false;

        /// <summary></summary>
        public bool IsTarget = false;
        #endregion

        #region Events
        public event EventHandler<MouseEventArgs>? DoMouseClick;
        public event EventHandler<MouseEventArgs>? StartDragDrop;
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
        /// 
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
                    StartDragDrop?.Invoke(this, e);
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
            pe.Graphics.Clear(IsTarget ? TargetColor : BackColor);

            if (Selected) // Draw box.
            {
                Rectangle rect = ClientRectangle;
                rect.Inflate(-1, -1);
                using Pen pen = new(Color.Black, 2);
                pe.Graphics.DrawRectangle(pen, rect);
            }

            // Main content.
            if (ImageRect.IsValid)
            {
                pe.Graphics.DrawImage(Item.Bitmap, ImageRect.WinRect);
            }

            if (TextRect.IsValid)
            {
                using StringFormat sfmt = new() { LineAlignment = StringAlignment.Center, Alignment = StringAlignment.Center };
                pe.Graphics.DrawString(Item.Caption, Font, Brushes.Black, TextRect.WinRect, sfmt);
                pe.Graphics.DrawRectangle(Pens.Green, TextRect.WinRect);

            }

            base.OnPaint(pe);
        }
        #endregion
    }
}
