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
        /// <summary>Icon of ImageSize above, text block below</summary>
        Icon,
        /// <summary>Icon of ImageSize left, text block right</summary>
        Tile,
        /// <summary>Verbatim as provided by client, will truncate</summary>
        Image,
        /// <summary>Fill/stretch with image</summary>
        Fit
    }

    /// <summary>Drag and drop data.</summary>
    public enum DroppedDataType
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

    /// <summary>What the left mouse button does.</summary>
    public enum MouseFunction
    {
        /// <summary>One only selected.</summary>
        SingleSelect,
        /// <summary>One or more selected.</summary>
        MultiSelect,
        /// <summary>Standard single click.</summary>
        Click,
    }

    /// <summary>User selection(s) have changed.</summary>
    public class SelectionEventArgs : EventArgs
    {
        /// <summary>The selection</summary>
        public List<Item> SelectedItems { get; set; } = [];
    }

    /// <summary>User drag-dropped something from elsewhere.</summary>
    public class DroppedDataEventArgs(DroppedDataType tgttype, object data) : EventArgs
    {
        /// <summary>The data type</summary>
        public DroppedDataType DataType { get; set; } = tgttype;

        /// <summary>The target</summary>
        public object Data { get; set; } = data;

        /// <summary>Read me.</summary>
        public override string ToString()
        {
            return $"{DataType} [{data}]";
        }
    }

    /// <summary>Debugging.</summary>
    public class TraceEventArgs() : EventArgs
    {
        /// <summary>Log line.</summary>
        public string Line { get; set; } = "";

        /// <summary>Stuff moving too fast for Line.</summary>
        public string State { get; set; } = "";
    }
}
