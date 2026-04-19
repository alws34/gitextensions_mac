// Minimal stubs so GitExtUtils compiles on non-Windows platforms.
#if !WINDOWS
#pragma warning disable SA1502, SA1136, SA1201, SA1402, SA1649, SA1600, SA1516, SA1203, CS1591

// ReSharper disable All
// These are intentionally minimal stubs.

namespace System.Windows.Forms
{
    public enum FlatStyle
    {
        Flat = 0,
        Popup = 1,
        Standard = 2,
        System = 3,
    }

    public enum TextFormatFlags
    {
        Default = 0,
        GlyphOverhangPadding = 0,
        SingleLine = 0x20,
        VerticalCenter = 0x04,
        HorizontalCenter = 0x01,
        NoClipping = 0x100,
        NoPrefix = 0x800,
        Left = 0,
        Right = 0x02,
        Bottom = 0x08,
        WordBreak = 0x10,
        ExpandTabs = 0x40,
        EndEllipsis = 0x8000,
    }

    public static class TextRenderer
    {
        public static void DrawText(Drawing.IDeviceContext dc, string? text, Drawing.Font? font, Drawing.Rectangle bounds, Drawing.Color foreColor, TextFormatFlags flags)
        {
        }

        public static Drawing.Size MeasureText(Drawing.IDeviceContext dc, string? text, Drawing.Font? font, Drawing.Size proposedSize, TextFormatFlags flags)
        {
            return Drawing.Size.Empty;
        }

        public static Drawing.Size MeasureText(string? text, Drawing.Font? font)
        {
            return Drawing.Size.Empty;
        }
    }

    public enum ToolStripRenderMode
    {
        Custom = 0,
        System = 1,
        Professional = 2,
        ManagerRenderMode = 3,
    }

    public class PaintEventArgs : EventArgs
    {
#pragma warning disable CS8618
        public Drawing.Graphics Graphics { get; set; }
#pragma warning restore CS8618

        public Drawing.Rectangle ClipRectangle { get; set; }
    }

    public class Cursor
    {
        public Cursor()
        {
        }

        public Cursor(nint handle)
        {
        }

        public static Drawing.Point Position { get; set; }
    }

    public static class Cursors
    {
        public static Cursor Default { get; } = new Cursor();

        public static Cursor Hand { get; } = new Cursor();

        public static Cursor WaitCursor { get; } = new Cursor();
    }

    public static class SystemInformation
    {
        public static int VerticalScrollBarWidth => 17;

        public static int HorizontalScrollBarHeight => 17;
    }

    public class ButtonBase : Control
    {
        public Drawing.Image? Image { get; set; }

        public FlatStyle FlatStyle { get; set; }
    }

    public class Button : ButtonBase, IButtonControl
    {
        public bool UseVisualStyleBackColor { get; set; } = true;

        public DialogResult DialogResult { get; set; }

        public new event EventHandler? Click
        {
            add { }
            remove { }
        }
    }

    public class LinkLabel : Label
    {
        public Drawing.Color LinkColor { get; set; }

        public Drawing.Color VisitedLinkColor { get; set; }

        public Drawing.Color ActiveLinkColor { get; set; }

        public event EventHandler<LinkLabelLinkClickedEventArgs>? LinkClicked
        {
            add { }
            remove { }
        }
    }

    public class TextBoxBase : Control
    {
        public int SelectionStart { get; set; }

        public int SelectionLength { get; set; }

        public bool ReadOnly { get; set; }
    }

    public class UpDownBase : Control
    {
    }

    public class PictureBox : Control
    {
        public Drawing.Image? Image { get; set; }

        public new Drawing.Image? BackgroundImage { get; set; }
    }

    public class SplitContainer : Control
    {
        public int SplitterWidth { get; set; }
    }

    public class ImageList : System.ComponentModel.Component
    {
        public ImageCollection Images { get; } = new ImageCollection();

        public Drawing.Size ImageSize { get; set; }

        public nint Handle => nint.Zero;

        public class ImageCollection : System.Collections.Generic.List<Drawing.Image?>
        {
            public Drawing.Image? this[string key] => null;
        }
    }

    public class ImageCollection : System.Collections.Generic.List<Drawing.Image>
    {
    }

    public class TabPage : Control
    {
        public int ImageIndex { get; set; } = -1;

        public string ImageKey { get; set; } = string.Empty;
    }

    public class TabControl : Control
    {
        public System.Collections.Generic.List<TabPage> TabPages { get; } = [];

        public new Drawing.Point Padding { get; set; }

        public int SelectedIndex { get; set; } = -1;

        public int TabCount => TabPages.Count;

        public ImageList? ImageList { get; set; }

        public event EventHandler<PaintEventArgs>? Paint
        {
            add { }
            remove { }
        }

        public Drawing.Rectangle GetTabRect(int index)
        {
            return Drawing.Rectangle.Empty;
        }
    }

    public class ListViewItem
    {
        public int ImageIndex { get; set; } = -1;

        public ImageList? ImageList { get; set; }

        public string? Text { get; set; }

        public bool Selected { get; set; }

        public ListView? ListView { get; set; }

        public int Index { get; set; } = -1;

        public object? Tag { get; set; }

        public Drawing.Rectangle Bounds { get; set; }
    }

    public class ListViewGroup
    {
        public string? Header { get; set; }

        public object? Tag { get; set; }
    }

    public class SelectedListViewItemCollection : System.Collections.Generic.List<ListViewItem>
    {
    }

    public class ListView : Control
    {
        public System.Collections.Generic.List<ListViewItem> Items { get; } = [];

        public System.Collections.Generic.List<ListViewGroup> Groups { get; } = [];

        public ListViewItem? FocusedItem { get; set; }

        public SelectedListViewItemCollection SelectedItems { get; } = new SelectedListViewItemCollection();
    }

    public class PropertyGrid : Control
    {
    }

    public class DataGridView : Control
    {
        public bool EnableHeadersVisualStyles { get; set; } = true;
    }

    public class ToolStripRenderer
    {
    }

    public class ToolStrip : Control
    {
        public System.Collections.Generic.List<ToolStripItem> Items { get; } = [];

        public ToolStripRenderer? Renderer { get; set; }

        public ToolStripRenderMode RenderMode { get; set; }
    }

    public class ToolStripComboBox : ToolStripItem
    {
        private readonly ComboBox _comboBox = new ComboBox();

        public ComboBox? ComboBox => _comboBox;

        public new Drawing.Size Size { get; set; }

        public Control Control => _comboBox;

        public int DropDownWidth
        {
            get => _comboBox.DropDownWidth;
            set => _comboBox.DropDownWidth = value;
        }

        public ComboBoxItemCollection Items => _comboBox.Items;

        public int SelectedIndex
        {
            get => _comboBox.SelectedIndex;
            set => _comboBox.SelectedIndex = value;
        }
    }

    public class ToolStripDropDownItem : ToolStripItem
    {
        public System.Collections.Generic.List<ToolStripItem> DropDownItems { get; } = [];
    }

    public class ToolStripLabel : ToolStripItem
    {
        public Drawing.Color LinkColor { get; set; }

        public Drawing.Color VisitedLinkColor { get; set; }

        public Drawing.Color ActiveLinkColor { get; set; }
    }

    public class ToolStripRenderEventArgs : EventArgs
    {
        public ToolStrip? ToolStrip { get; set; }

        public Drawing.Graphics? Graphics { get; set; }

        public Drawing.Rectangle AffectedBounds { get; set; }
    }

    public class ToolStripItemRenderEventArgs : EventArgs
    {
        public ToolStrip? ToolStrip { get; set; }

        public ToolStripItem? Item { get; set; }

        public Drawing.Graphics? Graphics { get; set; }
    }

    public class ToolStripSystemRenderer : ToolStripRenderer
    {
        public bool RoundedEdges { get; set; }

        protected virtual void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
        {
        }

        protected virtual void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
        {
        }
    }

    public class ToolStripProfessionalRenderer : ToolStripRenderer
    {
        public bool RoundedEdges { get; set; }

        protected virtual void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
        {
        }

        protected virtual void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
        {
        }
    }

    public static class Clipboard
    {
        public static void SetText(string text)
        {
        }

        public static void SetText(string text, TextDataFormat format)
        {
        }

        public static void SetDataObject(object data, bool copy = false, int retryTimes = 10, int retryDelay = 100)
        {
        }

        public static string GetText()
        {
            return string.Empty;
        }

        public static string GetText(TextDataFormat format)
        {
            return string.Empty;
        }

        public static bool ContainsText()
        {
            return false;
        }

        public static void Clear()
        {
        }
    }
}

#pragma warning restore SA1502, SA1136, SA1201, SA1402, SA1649, SA1600, SA1516, SA1203, CS1591
#endif
