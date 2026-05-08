
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
    /// </summary>
    [ToolboxItem(false), Browsable(false)] // not useable in designer
    public class ItemDisplay : UserControl
    {
        #region Properties
        /// <summary>The owned item.</summary>
        public Item Item { get; init; }

        /// <summary>Geometry.</summary>
        public Point ImageLoc { get; init; }

        /// <summary>Geometry.</summary>
        public Rectangle TextRect { get; init; }

        ///// <summary>Geometry.</summary>
        //public Size ItemSize { get; init; }

        /// <summary>Cosmetics.</summary>
        public Color TargetColor { get; set; } = Color.Aqua;

        /// <summary></summary>
        public bool Selected = false;

        /// <summary></summary>
        public bool IsTarget = false;
        #endregion

        //#region Fields
        ///// <summary>How to draw</summary>
        //readonly ItemGeometry _geometry;
        //#endregion

        #region Lifecycle
        /// <summary>
        /// Constructor.
        /// </summary>
        public ItemDisplay(Item item) //, ItemGeometry geometry)
        {
            Item = item;
            //_geometry = geometry;
            AllowDrop = true;
            //Size = geometry.Size;
        }

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                Item.Dispose();
            }
            base.Dispose(disposing);
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
            pe.Graphics.DrawRectangle(Pens.Green, TextRect);
            pe.Graphics.DrawImage(Item.Bitmap, ImageLoc);

            using StringFormat sfmt = new() { LineAlignment = StringAlignment.Center, Alignment = StringAlignment.Center };
            pe.Graphics.DrawString(Item.Caption, Font, Brushes.Black, TextRect, sfmt);
        }
        #endregion

        /// <summary>Read me</summary>
        public override string ToString()
        {
            return $"item:{Item} sel:{Selected}";
        }
    }
}
