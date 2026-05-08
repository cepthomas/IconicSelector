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
    public class ItemGeometry
    {
        /// <summary>Geometry.</summary>
        public Point ImageLoc { get; init; }

        /// <summary>Geometry.</summary>
        public Rectangle TextRect { get; init; }

        /// <summary>Geometry.</summary>
        public Size Size { get; init; }

        /// <summary>Determine.</summary>
        //public ItemGeometry(SelectorStyle style)
        //{
        //    switch (style)
        //    {
        //        case SelectorStyle.Icon:
        //            ImageLoc = new(Pad + ImageSize / 2, Pad);
        //            TextRect = new(Pad, Pad + ImageSize, ImageSize - Pad * 2, ImageSize / 2 - Pad);
        //            Size = new(ImageSize * 2 + Pad * 2, ImageSize * 2 + Pad * 2);
        //            break;

        //        case SelectorStyle.Tile:
        //            ImageLoc = new(Pad, Pad);
        //            TextRect = new(Pad + ImageSize + Pad, Pad, ImageSize / 2 - Pad, ImageSize - Pad * 2);
        //            Size = new(ImageSize * 3 + Pad * 3, ImageSize + Pad * 2);
        //            break;
        //    }
        //}
    }
}