using System;
using System.Collections.Generic;
using System.Drawing;


namespace Ephemera.IconicSelector
{
    /// <summary>Drag and drop payload data.</summary>
    public enum ItemDataType { None, Item, File, Url }

    /// <summary>Describes one item in the collection. Part of API.</summary>
    public class Item : IDisposable
    {
        /// <summary>Displayed text</summary>
        public ItemDataType DataType { get; set; } = ItemDataType.None;

        /// <summary>Displayed text</summary>
        public string Caption { get; set; } = "";

        /// <summary>Associated image.</summary>
        public Bitmap Bitmap { get; set; }

        /// <summary>Carries data supplied by client.</summary>
        public object Value { get; set; } = "???";

        /// <summary>Normal constructor</summary>
        public Item(ItemDataType dtype, string text, Bitmap bmp, object value)
        {
            DataType = dtype;
            Caption = text;
            Bitmap = bmp;
            Value = value;
        }

        /// <summary>Copy constructor</summary>
        public Item(Item rhs)
        {
            DataType = rhs.DataType;
            Caption = rhs.Caption;
            Bitmap = rhs.Bitmap;
            Value = rhs.Value;
        }

        /// <summary>Clean up</summary>
        public void Dispose()
        {
            Bitmap.Dispose();
        }

        /// <summary>Read me</summary>
        public override string ToString()
        {
            return $"dtype:{DataType} caption:{Caption} bmp:{Bitmap?.Size} value:{Value}";
        }
    }
}
