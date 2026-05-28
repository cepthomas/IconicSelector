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
using System.Drawing.Imaging;
using Ephemera.NBagOfTricks;
using Ephemera.NBagOfUis;


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

    /// <summary>
    /// User clicked. If item is not null it's a simple click: OpMode is Click.
    /// Else it's a notification that selection(s) changed: OpMode is *Select.
    /// </summary>
    public class ClickEventArgs(Item? item) : EventArgs
    {
        public Item? ClickedItem { get; init; } = item;
    }
    #endregion

    /// <summary>Configuration for instance.</summary>
    public class Config
    {
        #region Properties
        /// <summary>Selector flavor.</summary>
        public SelectorStyle Style { get; set; } = SelectorStyle.Icon;

        /// <summary>What the mouse click does.</summary>
        public OpMode Mode { get; set; } = OpMode.Click;

        /// <summary>How wide.</summary>
        public int NumColumns { get; set; } = 1;

        /// <summary>Image size.</summary>
        public Size ImageSize { get; set; } = new(32, 32);

        /// <summary>Allow drag and drop frome external sources - file/folder/url only. TODO1 images?</summary>
        public bool AllowExternalSource { get; set; } = false;

        /// <summary>If enabled, show item ToString() when hovering.</summary>
        public bool EnableToolTip { get; set; } = false;

        /// <summary>Optional font.</summary>
        public Font? DrawFont { get; set; } = null;

        /// <summary>Optional if no valid image available.</summary>
        public Bitmap? DefaultImage { get; set; } = null;

        /// <summary>Cosmetics.</summary>
        public Color IndicatorColor { get; set; } = Color.Violet;

        /// <summary>Visual space at edges.</summary>
        public int Pad { get; set; } = 4;

        /// <summary>Space between items.</summary>
        public int Spacing { get; set; } = 10;

        /// <summary>Trim collection tail if > 0.</summary>
        public int MaxItems { get; set; } = 0;
        #endregion
    }
}
