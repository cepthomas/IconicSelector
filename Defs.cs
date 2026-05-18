using System;
using System.Collections.Generic;


namespace Ephemera.IconicSelector
{
    /// <summary>Selector style options.</summary>
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

    /// <summary>User clicked item.</summary>
    public class ClickEventArgs(Item item) : EventArgs
    {
        /// <summary>The item</summary>
        public Item ClickedItem { get; init; } = item;
    }

    /// <summary>User selection(s) have changed.</summary>
    public class SelectionEventArgs(List<Item> items) : EventArgs
    {
        /// <summary>The selection(s)</summary>
        public List<Item> SelectedItems { get; init; } = items;
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
