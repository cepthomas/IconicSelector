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
    /// <summary>Supported styles.</summary>
    public enum SelectorStyle
    {
        /// <summary>Icon of ImageSize Bitmap above, text of 2 X ImageSize below</summary>
        Icon,
        /// <summary>Icon of ImageSize Bitmap left, text of 2 X ImageSize right</summary>
        Tile,
        /// <summary>Fill with Bitmap of ImageSize</summary>
        Image
    }

    /// <summary>Image fitting to ImageSize.</summary>
    public enum ImageFit
    {
        /// <summary>Client is in charge of images</summary>
        None,
        /// <summary>Rendered image height from client, width scaled</summary>
        FitHeight,
        /// <summary>Rendered image width from client, height scaled</summary>
        FitWidth,
        /// <summary>Use all available space</summary>
        Fill,
    }

    /// <summary></summary>
    public enum MouseFunction { SingleSelect, MultiSelect, Click }

    /// <summary>Custom rectangle for this application.</summary>
    public class DisplayRect
    {
        public int Left { get; init; } = -1;
        public int Top { get; init; } = -1;
        public int Right { get; init; } = -1;
        public int Bottom { get; init; } = -1;
        public Rectangle WinRect { get { return new Rectangle(Left, Top, Right - Left, Bottom - Top); } }
        public bool IsValid { get; init; } = false;

        /// <summary>Default constructor - invalid.</summary>
        public DisplayRect()
        {
            IsValid = false;
        }

        /// <summary>Normal constructor - invalid.</summary>
        public DisplayRect(int left, int top, int width, int height)
        {
            IsValid = top >= 0 && left >= 0 && width >= 0 && height >= 0;
            if (!IsValid) throw new ArgumentException("Invalid args");
            Left = left;
            Top = top;
            Right = left + width;
            Bottom = top + height;
        }

        /// <summary>Read me.</summary>
        public override string ToString()
        {
            return IsValid ? $"L:{Left} T:{Top} R:{Right} B:{Bottom}" : "Invalid";
        }
    }

    /// <summary>User selection(s) have changed.</summary>
    public class SelectionEventArgs : EventArgs
    {
        /// <summary>The selection</summary>
        public List<Item> SelectedItems { get; set; } = [];
    }

    /// <summary>User drag-dropped something from elsewhere.</summary>
    /// <remarks>Default constructor.</remarks>
    public class DroppedTargetEventArgs(Item item) : EventArgs
    {
        /// <summary>The new item</summary>
        public Item NewItem { get; set; } = item;
    }
}
