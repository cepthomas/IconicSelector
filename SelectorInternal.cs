using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;
using System.Net.Http;
using System.Threading.Tasks;
using System.IO;
using Ephemera.NBagOfTricks;
using Ephemera.NBagOfUis;


namespace Ephemera.IconicSelector
{
    /// <summary>Master control internals.</summary>
    public partial class Selector : UserControl
    {
        #region Fields
        /// <summary>Backing field for Style.</summary>
        SelectorStyle _style = SelectorStyle.Icon;

        /// <summary>Backing field for NumColumns.</summary>
        int _numColumns = 1;

        /// <summary>Backing field for ImageSize.</summary>
        Size _imageSize = new(32, 32);

        /// <summary>All entries in the collection.</summary>
        readonly List<ItemDisplay> _itemds = [];

        /// <summary>ItemDisplay geometry.</summary>
        Rectangle _itemdImageRect = new();

        /// <summary>ItemDisplay geometry.</summary>
        Rectangle _itemdTextRect = new();

        /// <summary>ItemDisplay geometry.</summary>
        Size _itemdSize;

        /// <summary>Where to move/insert item.</summary>
        int _insertIndex = NOT_IN_TARGET;

        /// <summary>Meta index.</summary>
        const int NOT_IN_TARGET = -1;

        /// <summary>Meta index.</summary>
        const int IN_TARGET_CENTER = -2;
        #endregion

        #region Lifecycle
        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _itemds.ForEach(itemd => { itemd.Dispose(); });
                _itemds.Clear();
                DefaultImage?.Dispose();
                FolderImage?.Dispose();
                UrlImage?.Dispose();
            }
            base.Dispose(disposing);
        }
        #endregion

        #region ItemDisplay event handlers
        /// <summary>
        /// Handle cursor change.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Itemd_CursorLocationChanged(object? sender, CursorLocationEventArgs e)
        {
            int index = GetItemIndex(sender);

            switch(e.Location)
            {
                case CursorLocation.Left:
                    _insertIndex = index;
                    break;

                case CursorLocation.Right:
                    _insertIndex = index + 1;
                    break;

                case CursorLocation.Center:
                    _insertIndex = IN_TARGET_CENTER;
                    break;

                case CursorLocation.None:
                    _insertIndex = NOT_IN_TARGET;
                    break;
            }

            Invalidate(); // just this control
        }

        /// <summary>
        /// User item selection(s). Could be select or click.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Itemd_DoMouseClick(object? sender, MouseEventArgs e)
        {
            ArgumentNullException.ThrowIfNull(sender);

            var itemd = (ItemDisplay)sender;
            bool sel = itemd.Selected; // current

            switch (e.Button, Mode)
            {
                case (MouseButtons.Left, OpMode.Click):
                    Click?.Invoke(this, new(itemd.Item));
                    break;

                case (MouseButtons.Left, OpMode.SingleSelect):
                    if (sel)
                    {
                        itemd.Selected = false;
                        Click?.Invoke(this, new(null));
                    }
                    else
                    {
                        // Deselect others first.
                        _itemds.ForEach(itemd => itemd.Selected = false);
                        // Select this one.
                        itemd.Selected = true;
                        Click?.Invoke(this, new(null));
                    }
                    break;

                case (MouseButtons.Left, OpMode.MultiSelect):
                    itemd.Selected = !sel;
                    Click?.Invoke(this, new(null));
                    break;

                case (_, _):
                    // ignored
                    break;
            }

            Invalidate(true); // refresh everything
        }

        /// <summary>
        /// Handle dropped data.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Itemd_DroppedPayload(object? sender, DroppedPayloadEventArgs e)
        {
            int index = GetItemIndex(sender);
            TraceLine($"Itemd_DroppedPayload() index:{index} e:{e}");

            switch (e.DataType)
            {
                case DroppedDataType.Item: // handle here
                    var draggedItem = (ItemDisplay)e.Payload;
                    TraceLine($"Dropped item -> [{draggedItem}]");
                    // Insert a copy of the dragged item at the insert index.
                    Item item = draggedItem.Item;
                    AddItem(ItemDataType.Item, item.Caption, item.Bitmap, item.Value, _insertIndex);
                    // Remove the original dragged item.
                    RemoveItem(draggedItem);
                    break;

                case DroppedDataType.File:
                case DroppedDataType.Url:
                    var res = (string)e.Payload;
                    AddResourceItem(res, index);
                    break;

                default:
                    throw new InvalidOperationException();
            }

            _insertIndex = NOT_IN_TARGET;
            Invalidate(true);
        }
        #endregion

        #region Drawing
        /// <summary>
        /// Draw the whole control.
        /// </summary>
        /// <param name="pe"></param>
        protected override void OnPaint(PaintEventArgs pe)
        {
            pe.Graphics.Clear(BackColor);

            // Insert marker?
            if (_insertIndex >= 0)
            {
                using Pen pen = new(IndicatorColor, 4);
                int offset = 3;

                // Special case for last item.
                if (_insertIndex >= _itemds.Count)
                {
                    var itemd = _itemds.Last();
                    var loc = itemd.Location;
                    pe.Graphics.DrawLine(pen, loc.X + itemd.Width + offset, loc.Y, loc.X + itemd.Width + offset, loc.Y + itemd.Height);
                }
                else
                {
                    var itemd = _itemds[_insertIndex];
                    var loc = itemd.Location;
                    pe.Graphics.DrawLine(pen, loc.X - offset, loc.Y, loc.X - offset, loc.Y + itemd.Height);
                }
            }

            base.OnPaint(pe);
        }
        #endregion

        #region Internals
        /// <summary>
        /// Common function to add a new item. Adjusts image for mode.
        /// </summary>
        /// <param name="dtype">The value type</param>
        /// <param name="caption">For display below/next to image</param>
        /// <param name="bmp">Bitmap</param>
        /// <param name="value">Meaningful for client use</param>
        /// <param name="index">Where to insert, -1 is append</param>
        void AddItem(ItemDataType dtype, string caption, Bitmap bmp, object value, int index = -1)
        {
            switch (Style)
            {
                case SelectorStyle.Icon:
                    // Use image as provided.
                    break;

                case SelectorStyle.Tile:
                    // Use image as provided.
                    break;

                case SelectorStyle.Clip:
                    // Copy pixels starting from 0, 0 to fill the visible area.
                    PixelBitmap pbmpin = new(bmp);
                    PixelBitmap pbmpout = new(ImageSize.Width, ImageSize.Height);

                    for (int x = 0; x < ImageSize.Width && x < bmp.Width; x++)
                    {
                        for (int y = 0; y < ImageSize.Height && y < bmp.Height; y++)
                        {
                            pbmpout.SetPixel(x, y, pbmpin.GetPixel(x, y));
                        }
                    }

                    bmp = pbmpout.GetBitmap();
                    pbmpin.Dispose();
                    pbmpout.Dispose();
                    break;

                case SelectorStyle.Fill:
                    bmp = ResizeBitmap(bmp, ImageSize.Width, ImageSize.Height);
                    break;

                case SelectorStyle.FitHeight:
                    {
                        float ratio = (float)_itemdSize.Height / bmp.Height;
                        int tnWidth = (int)(bmp.Width * ratio);
                        int tnHeight = (int)(bmp.Height * ratio);
                        var bmpt = ResizeBitmap(bmp, tnWidth, tnHeight);
                        bmp = bmpt.Clone(new(0, 0, _itemdSize.Width, _itemdSize.Height), PixelFormat.Format32bppArgb);
                    }
                    break;

                case SelectorStyle.FitWidth:
                    {
                        float ratio = (float)_itemdSize.Width / bmp.Width;
                        int tnHeight = (int)(bmp.Height * ratio);
                        int tnWidth = (int)(bmp.Width * ratio);
                        var bmpt = ResizeBitmap(bmp, tnWidth, tnHeight);
                        bmp = bmpt.Clone(new(0, 0, _itemdSize.Width, _itemdSize.Height), PixelFormat.Format32bppArgb);
                    }
                    break;
            }

            Item item = new(dtype, caption, bmp, value);

            ItemDisplay itemd = new(item)
            {
                IndicatorColor = IndicatorColor,
                ImageRect = _itemdImageRect,
                TextRect = _itemdTextRect,
                Size = _itemdSize,
                AllowExternalSource = AllowExternalSource,
            };
            itemd.DoMouseClick += Itemd_DoMouseClick;
            itemd.DroppedPayload += Itemd_DroppedPayload;
            itemd.CursorLocationChanged += Itemd_CursorLocationChanged;
            toolTip1.SetToolTip(itemd, value.ToString());


            Controls.Add(itemd);

            // Where to put it?
            if (index >= 0 && index < _itemds.Count)
            {
                _itemds.Insert(index, itemd);
            }
            else // append
            {
                _itemds.Add(itemd);
            }

            UpdateItemsList();
            Invalidate(true); // refresh everything
        }

        /// <summary>
        /// Calculates geometry of display elements.
        /// </summary>
        void InitGeometry()
        {
            // Figure geometry.
            switch (Style)
            {
                case SelectorStyle.Icon:
                    {
                        _itemdImageRect = new(Pad + ImageSize.Width / 2, Pad, ImageSize.Width, ImageSize.Height);
                        _itemdTextRect = new(Pad, _itemdImageRect.Bottom + Pad, 2 * ImageSize.Width, ImageSize.Height);
                        _itemdSize = new(_itemdTextRect.Right + Pad, _itemdTextRect.Bottom + Pad);
                        Width = Spacing + NumColumns * (_itemdSize.Width + Spacing) + SystemInformation.VerticalScrollBarWidth;
                    }
                    break;

                case SelectorStyle.Tile:
                    {
                        _itemdImageRect = new(Pad, Pad, ImageSize.Width, ImageSize.Height);
                        _itemdTextRect = new(_itemdImageRect.Right + Pad, Pad, 2 * ImageSize.Width, ImageSize.Height);
                        _itemdSize = new(_itemdTextRect.Right + Pad, _itemdTextRect.Bottom + Pad);
                        Width = Spacing + NumColumns * (_itemdSize.Width + Spacing) + SystemInformation.VerticalScrollBarWidth;
                    }
                    break;

                case SelectorStyle.Clip:
                case SelectorStyle.Fill:
                case SelectorStyle.FitWidth:
                case SelectorStyle.FitHeight:
                    {
                        _itemdImageRect = new(0, 0, ImageSize.Width, ImageSize.Height);
                        _itemdTextRect = new(); // not used
                        _itemdSize = _itemdImageRect.Size;
                        Width = Spacing + NumColumns * (_itemdSize.Width + Spacing) + SystemInformation.VerticalScrollBarWidth;
                    }
                    break;
            }

            TraceLine($"geometry Width:{Width} _itemdSize:{_itemdSize}");
        }

        /// <summary>
        /// Called after list changes.
        /// </summary>
        void UpdateItemsList()
        {
            // Calc grid layout.
            int xinc = _itemdSize.Width + Spacing;
            int yinc = _itemdSize.Height + Spacing;

            // Configure item draw.
            for (int i = 0; i < _itemds.Count; i++)
            {
                int row = i / NumColumns;
                int col = i % NumColumns;
                int xloc = xinc * col + Spacing;
                int yloc = yinc * row + Spacing;

                _itemds[i].Location = new Point(xloc, yloc);
            }
        }

        /// <summary>
        /// Source - https://stackoverflow.com/a/64126237
        /// Posted by GLJ
        /// Retrieved 2026-05-22, License - CC BY-SA 4.0
        /// </summary>
        /// <param name="filepath"></param>
        /// <returns></returns>
        string? GetLinkTarget(string filepath)  // TODO1 fix and put somewhere else?
        {
            //python version  https://stackoverflow.com/a/28952464
            //file spec  https://learn.microsoft.com/en-us/openspecs/windows_protocols/ms-shllink/16cb4ca1-9339-4d0c-a68d-bf1d6cc0f943?redirectedfrom=MSDN

            // ok @"%PROGRAMDATA%\Microsoft\Windows\Start Menu\Programs\Firefox.lnk",
            // ng @"%PROGRAMDATA%\Microsoft\Windows\Start Menu\Programs\Administrative Tools\Performance Monitor.lnk",

            string? path = null;

            // HeaderSize: (4 bytes, offset 0x0000), 0x0000004C as required.
            // LinkCLSID: (16 bytes, offset 0x0004), 00021401-0000-0000-C000-000000000046.
            // LinkFlags: (4 bytes, offset 0x0014), 0x0008009B means the following LinkFlags (section 2.1.1) are set:
            //     §   HasLinkTargetIDList
            //     §   HasLinkInfo
            //     §   HasRelativePath
            //     §   HasWorkingDir
            //     §   IsUnicode
            //     §   EnableTargetMetadata
            // FileAttributes: (4 bytes, offset 0x0018), 0x00000020, means the following FileAttributesFlags (section 2.1.2) are set:
            //     §   FILE_ATTRIBUTE_ARCHIVE
            // CreationTime: (8 bytes, offset 0x001C) FILETIME 9/12/08, 8:27:17PM.
            // AccessTime: (8 bytes, offset 0x0024) FILETIME 9/12/08, 8:27:17PM.
            // WriteTime: (8 bytes, offset 0x002C) FILETIME 9/12/08, 8:27:17PM.
            // FileSize: (4 bytes, offset 0x0034), 0x00000000.
            // IconIndex: (4 bytes, offset 0x0038), 0x00000000.
            // ShowCommand: (4 bytes, offset 0x003C), SW_SHOWNORMAL(1).
            // Hotkey: (2 bytes, offset 0x0040), 0x0000.
            // Reserved: (2 bytes, offset 0x0042), 0x0000.
            // Reserved2: (4 bytes, offset 0x0044), 0 x00000000.
            // Reserved3: (4 bytes, offset 0x0048), 0 x00000000.


            using var br = new BinaryReader(File.OpenRead(filepath));
            try
            {
                // skip HeaderSize
                br.ReadBytes(4);

                // skip LinkCLSID
                br.ReadBytes(16);

                // read the LinkFlags structure
                uint lflags = br.ReadUInt32();

                bool HasLinkTargetIDList = (lflags & 0X00000001) > 0; // ok
                bool HasLinkInfo = (lflags & 0X00000002) > 0; // ok
                bool HasName = (lflags & 0X00000004) > 0; // ok  ng
                bool HasRelativePath = (lflags & 0X00000008) > 0; // ok

                bool HasWorkingDir = (lflags & 0X00000010) > 0; // ok
                bool HasArguments = (lflags & 0X00000020) > 0; // ng
                bool HasIconLocation = (lflags & 0X00000040) > 0; // ng
                bool IsUnicode = (lflags & 0X00000080) > 0; // ok ng

                bool ForceNoLinkInfo = (lflags & 0X00000100) > 0; // ng
                bool HasExpString = (lflags & 0X00000200) > 0; // ng
                bool RunInSeparateProcess = (lflags & 0X00000400) > 0;
                bool Unused1 = (lflags & 0X00000800) > 0;

                bool HasDarwinID = (lflags & 0X00001000) > 0;
                bool RunAsUser = (lflags & 0X00002000) > 0;
                bool HasExpIcon = (lflags & 0X00004000) > 0;
                bool NoPidlAlias = (lflags & 0X00008000) > 0;

                bool Unused2 = (lflags & 0X00010000) > 0;
                bool RunWithShimLayer = (lflags & 0X00020000) > 0;
                bool ForceNoLinkTrack = (lflags & 0X00040000) > 0;
                bool EnableTargetMetadata = (lflags & 0X00080000) > 0;

                bool DisableLinkPathTracking = (lflags & 0X00100000) > 0;
                bool DisableKnownFolderTracking = (lflags & 0X00200000) > 0;
                bool DisableKnownFolderAlias = (lflags & 0X00400000) > 0;
                bool AllowLinkToLink = (lflags & 0X00800000) > 0;

                bool UnaliasOnSave = (lflags & 0X01000000) > 0;
                bool PreferEnvironmentPath = (lflags & 0X02000000) > 0; // ng
                bool KeepLocalIDListForUNCTarget = (lflags & 0X04000000) > 0;


                //>>>>> Because HasLinkTargetIDList is set, a LinkTargetIDList structure(section 2.2) follows:
                //>>>>> Because HasLinkInfo is set, a LinkInfo structure(section 2.3) follows:
                //>>>>> Because HasRelativePath is set, the RELATIVE_PATH StringData structure(section 2.4) follows:
                //>>>>> Because HasWorkingDir is set, the WORKING_DIR StringData structure(section 2.4) follows:
                //>>>>> Extra data section: (100 bytes, offset 0x0167), an ExtraData structure(section 2.5) follows:


                // if the HasLinkTargetIDList bit is set then skip the stored IDList structure and header
                if ((lflags & 0x01) == 1)  // ok 0x009F   ng 0x020003e4 -> 0010000000000000001111100100
                {
                    br.ReadBytes(52);
                    var skip = br.ReadUInt16(); // this counts of how far we need to skip ahead
                    br.ReadBytes(skip);
                }

                // get the number of bytes the path contains
                var length = br.ReadUInt32();

                // skip LinkInfoHeaderSize, LinkInfoFlgas, and VolumeIDOffset
                var bb = br.ReadBytes(12);

                // Find the location of the LocalBasePath position
                var lbpos = br.ReadUInt32();

                // Skip to the path position (subtract the length of the read (4 bytes), the length of
                // the skip (12 bytes), and the length of the lbpos read (4 bytes) from the lbpos)
                br.ReadBytes((int)lbpos - 20);

                var size = length - lbpos - 2;
                var bytePath = br.ReadBytes((int)size);

                path = Encoding.UTF8.GetString(bytePath, 0, bytePath.Length);
            }
            catch (Exception e)
            {
            }

            return path;
        }


        string? GetLinkTarget_orig(string filepath)  // TODO1 fix and put somewhere else?
        {
            //python version  https://stackoverflow.com/a/28952464
            //file spec  https://learn.microsoft.com/en-us/openspecs/windows_protocols/ms-shllink/16cb4ca1-9339-4d0c-a68d-bf1d6cc0f943?redirectedfrom=MSDN

            // ok @"%PROGRAMDATA%\Microsoft\Windows\Start Menu\Programs\Firefox.lnk",
            // ng @"%PROGRAMDATA%\Microsoft\Windows\Start Menu\Programs\Administrative Tools\Performance Monitor.lnk",


            string? path = null;

            using var br = new BinaryReader(File.OpenRead(filepath));
            try
            {
                // skip the first 20 bytes (HeaderSize and LinkCLSID)
                br.ReadBytes(0x14);

                // read the LinkFlags structure (4 bytes)
                uint lflags = br.ReadUInt32();

                // if the HasLinkTargetIDList bit is set then skip the stored IDList structure and header
                if ((lflags & 0x01) == 1)
                {
                    br.ReadBytes(0x34);
                    var skip = br.ReadUInt16(); // this counts of how far we need to skip ahead
                    br.ReadBytes(skip);
                }

                // get the number of bytes the path contains
                var length = br.ReadUInt32();

                // skip 12 bytes (LinkInfoHeaderSize, LinkInfoFlgas, and VolumeIDOffset)
                br.ReadBytes(0x0C);

                // Find the location of the LocalBasePath position
                var lbpos = br.ReadUInt32();

                // Skip to the path position (subtract the length of the read (4 bytes), the length of
                // the skip (12 bytes), and the length of the lbpos read (4 bytes) from the lbpos)
                br.ReadBytes((int)lbpos - 0x14); //TODO1 lbpos is 0 for:
                                                 //C:\ProgramData\Microsoft\Windows\Start Menu\Programs\Administrative Tools\Performance Monitor.lnk
                                                 //C:\ProgramData\Microsoft\Windows\Start Menu\Programs\System Tools\Task Manager.lnk

                var size = length - lbpos - 0x02;
                var bytePath = br.ReadBytes((int)size);

                path = Encoding.UTF8.GetString(bytePath, 0, bytePath.Length);
            }
            catch (Exception e)
            {
            }

            return path;
        }



        /// <summary>
        /// Version that doesn't throw. TODO1 put somewhere else?
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        Icon? SafeExtractIcon(string name)
        {
            try
            {
                var icon = Icon.ExtractAssociatedIcon(name);
                return icon;
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>Resize the image to the specified width and height.</summary>
        /// <param name="bmp">The image to resize.</param>
        /// <param name="width">The width to resize to.</param>
        /// <param name="height">The height to resize to.</param>
        /// <returns>The resized image.</returns>
        Bitmap ResizeBitmap(Bitmap bmp, int width, int height)
        {
            Bitmap result = new(width, height);
            result.SetResolution(bmp.HorizontalResolution, bmp.VerticalResolution);

            using (Graphics graphics = Graphics.FromImage(result))
            {
                // Set high quality.
                graphics.CompositingQuality = CompositingQuality.HighQuality;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.SmoothingMode = SmoothingMode.HighQuality;
                graphics.CompositingQuality = CompositingQuality.HighQuality;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.SmoothingMode = SmoothingMode.HighQuality;

                // Draw the image.
                graphics.DrawImage(bmp, 0, 0, result.Width, result.Height);
            }

            return result;
        }

        /// <summary>
        /// Remove this item.
        /// </summary>
        /// <param name="itemd"></param>
        /// <exception cref="ArgumentNullException"></exception>
        void RemoveItem(ItemDisplay itemd)
        {
            ArgumentNullException.ThrowIfNull(itemd);

            Controls.Remove(itemd);
            _itemds.Remove(itemd);

            UpdateItemsList();
            Invalidate(true); // refresh everything
        }

        /// <summary>
        /// Get the item safely.
        /// </summary>
        /// <param name="item">ItemDisplay to test</param>
        /// <returns>The item if valid</returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException"></exception>
        ItemDisplay GetItemd(object? item)
        {
            ArgumentNullException.ThrowIfNull(item);
            if (item.GetType() != typeof(ItemDisplay)) throw new ArgumentException("Invalid type");
            var itemd = (ItemDisplay)item;
            return itemd;
        }

        /// <summary>
        /// Get the index in the collection.
        /// </summary>
        /// <param name="item">ItemDisplay to test</param>
        /// <returns>Index or -1 if invalid</returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException"></exception>
        int GetItemIndex(object? item)
        {
            var itemd = GetItemd(item);
            int index = _itemds.IndexOf(itemd);
            return index;
        }

        /// <summary>
        /// Debug.
        /// </summary>
        /// <param name="line"></param>
        void TraceLine(string line)
        {
            Console.WriteLine($"SELECTOR {line}");
        }
        #endregion
    }
}
