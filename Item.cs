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


// PUBLIC_API 


namespace Ephemera.IconicSelector
{
    /// <summary></summary>
    public class Item : IDisposable
    {
        /// <summary>Displayed text</summary>
        public string Caption { get; set; } = "";

        /// <summary>Associated image.</summary>
        public Bitmap Bitmap { get; set; }

        /// <summary>Carries data supplied by client.</summary>
        public object Value { get; set; } = "???";

        /// <summary>Normal constructor</summary>
        public Item(string text, Bitmap bmp, object value)
        {
            Caption = text;
            Bitmap = bmp;
            Value = value;
        }

        /// <summary>Copy constructor</summary>
        public Item(Item rhs)
        {
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
            return $"caption:{Caption} bmp:{Bitmap?.Size} value:{Value}";
        }
    }
}
