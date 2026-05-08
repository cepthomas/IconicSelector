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
    public enum SelectorStyle { Icon, Tile }

    /// <summary></summary>
    public enum MouseFunction { SingleSelect, MultiSelect, Click }

    ///// <summary>Reporting client errors.</summary>
    //public class AppException(string message) : Exception(message);

    /// <summary>User selection(s) have changed.</summary>
    public class SelectionEventArgs : EventArgs
    {
        /// <summary>The selection</summary>
        public List<Item> SelectedItems { get; set; } = [];
    }

    /// <summary>User drag-dropped something from elsewhere.</summary>
    public class DroppedTargetEventArgs : EventArgs
    {
        /// <summary>The new item</summary>
        public Item NewItem { get; set; }
    }
}
