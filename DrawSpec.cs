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
using System.Text.Json.Serialization;
using Ephemera.NBagOfTricks;


namespace Ephemera.IconicSelector
{
    /// <summary>How to draw an item.</summary>
    public abstract class DrawSpec
    {
        /// <summary>Image size.</summary>
        public int ImageSize { get; set; } = 32;

        ///// <summary>Select style.</summary>
        //public SelectorStyle Style { get; set; } = SelectorStyle.Icon;

        /// <summary>Cosmetics.</summary>
        public Color TargetColor { get; set; } = Color.Aqua;

        /// <summary>Visual space at edges</summary>
        public int Pad { get; set; } = 4;

        /// <summary>Cosmetics.</summary>
        public Font DrawFont { get; set; } = new("Calibri", 11, FontStyle.Regular, GraphicsUnit.Point, 0);

        /// <summary>Geometry.</summary>
        public abstract Point ImageLoc { get; }

        /// <summary>Geometry.</summary>
        public abstract Rectangle TextRect { get; }

        /// <summary>Geometry.</summary>
        public abstract Size Size { get; }
    }

    /// <summary>Specific flavor.</summary>
    public class DrawSpecTile : DrawSpec
    {
        public override Point ImageLoc
        {
            get { return new(Pad, Pad); }
        }

        public override Rectangle TextRect
        {
            get { return new(Pad + ImageSize + Pad, Pad, ImageSize / 2 - Pad, ImageSize - Pad * 2); }
        }

        public override Size Size
        {
            get
            {
                var width = ImageSize * 3 + Pad * 3;
                var height = ImageSize + Pad * 2;
                return new(width, height);
            }
        }
    }

    /// <summary>Specific flavor.</summary>
    public class DrawSpecIcon : DrawSpec
    {
        public override Point ImageLoc
        {
            get { return new(Pad + ImageSize / 2, Pad); }
        }

        public override Rectangle TextRect
        {
            get { return new(Pad, Pad + ImageSize, ImageSize - Pad * 2, ImageSize / 2 - Pad); }
        }

        public override Size Size
        {
            get
            {
                var width = ImageSize * 2 + Pad * 2;
                var height = ImageSize * 2 + Pad * 2;
                return new(width, height);
            }
        }
    }
}