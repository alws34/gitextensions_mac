// Minimal stubs so GitExtensions.Extensibility compiles on non-Windows platforms.
// On Windows the real System.Windows.Forms types are used.
// On Mac/Linux these stubs allow compilation; the Avalonia UI layer provides real implementations.
#if !WINDOWS
#pragma warning disable SA1502, SA1136, SA1201, SA1402, SA1649, SA1600, SA1516, SA1203, CS1591

// ReSharper disable All
// These are intentionally minimal stubs.

namespace System.Windows.Forms
{
    public interface IWin32Window
    {
        nint Handle { get; }
    }

    public interface IButtonControl
    {
        DialogResult DialogResult { get; set; }
    }

    public struct Message
    {
        public nint HWnd { get; set; }

        public nint LParam { get; set; }

        public nint WParam { get; set; }

        public int Msg { get; set; }
    }

    public enum DialogResult
    {
        None,
        OK,
        Cancel,
        Abort,
        Retry,
        Ignore,
        Yes,
        No,
    }

    public enum MessageBoxButtons
    {
        OK,
        OKCancel,
        AbortRetryIgnore,
        YesNoCancel,
        YesNo,
        RetryCancel,
    }

    public enum MessageBoxIcon
    {
        None = 0,
        Error = 16,
        Hand = 16,
        Stop = 16,
        Question = 32,
        Exclamation = 48,
        Warning = 48,
        Asterisk = 64,
        Information = 64,
    }

    public enum MessageBoxDefaultButton
    {
        Button1 = 0,
        Button2 = 256,
        Button3 = 512,
    }

    public enum CheckState
    {
        Unchecked,
        Checked,
        Indeterminate,
    }

    public enum DockStyle
    {
        None,
        Top,
        Bottom,
        Left,
        Right,
        Fill,
    }

    public enum BorderStyle
    {
        None,
        FixedSingle,
        Fixed3D,
    }

    public enum AutoScaleMode
    {
        None,
        Font,
        Dpi,
        Inherit,
    }

    public enum SizeType
    {
        AutoSize,
        Absolute,
        Percent,
    }

    public enum ComboBoxStyle
    {
        Simple,
        DropDown,
        DropDownList,
    }

    public enum SystemColorMode
    {
        Classic = 0,
        Dark = 1,
    }

    public enum ControlStyles
    {
        None = 0,
        UserPaint = 0x200,
        AllPaintingInWmPaint = 0x2000,
        DoubleBuffer = 0x10000,
        OptimizedDoubleBuffer = 0x20000,
    }

    [Flags]
    public enum AnchorStyles
    {
        None = 0,
        Top = 1,
        Bottom = 2,
        Left = 4,
        Right = 8,
    }

    public enum AutoSizeMode
    {
        GrowAndShrink = 0,
        GrowOnly = 1,
    }

    public enum FormBorderStyle
    {
        None = 0,
        FixedSingle = 1,
        Fixed3D = 2,
        FixedDialog = 3,
        Sizable = 4,
        FixedToolWindow = 5,
        SizableToolWindow = 6,
    }

    public enum FormStartPosition
    {
        Manual = 0,
        CenterScreen = 1,
        WindowsDefaultLocation = 2,
        WindowsDefaultBounds = 3,
        CenterParent = 4,
    }

    public class MouseEventArgs : EventArgs
    {
        public Drawing.Point Location { get; set; }

        public int X => Location.X;

        public int Y => Location.Y;

        public int Delta { get; set; }
    }

    public class LinkLabelLinkClickedEventArgs : EventArgs
    {
    }

    public class TreeNodeCollection : System.Collections.Generic.List<TreeNode>
    {
        public TreeNode[] Find(string key, bool searchAllChildren)
        {
            return [];
        }

        public new void AddRange(System.Collections.Generic.IEnumerable<TreeNode> nodes)
        {
            foreach (TreeNode node in nodes)
            {
                Add(node);
            }
        }
    }

    public class TreeNode
    {
        public TreeNode()
        {
        }

        public TreeNode(string? text)
        {
            Text = text;
        }

        public string? Text { get; set; }

        public string? Name { get; set; }

        public object? Tag { get; set; }

        public Drawing.Color ForeColor { get; set; }

        public TreeNodeCollection Nodes { get; } = new TreeNodeCollection();

        public void Expand()
        {
        }
    }

    public class TreeViewCancelEventArgs : EventArgs
    {
        public TreeNode? Node { get; set; }

        public bool Cancel { get; set; }
    }

    public class TreeView : Control
    {
        public TreeNodeCollection Nodes { get; } = new TreeNodeCollection();

        public TreeNode? SelectedNode { get; set; }

        public event EventHandler<TreeViewEventArgs>? AfterSelect
        {
            add { }
            remove { }
        }

        public event EventHandler<TreeViewCancelEventArgs>? BeforeExpand
        {
            add { }
            remove { }
        }

        public event EventHandler<MouseEventArgs>? MouseDoubleClick
        {
            add { }
            remove { }
        }
    }

    public enum TextDataFormat
    {
        Text = 0,
        UnicodeText = 1,
        Rtf = 2,
        Html = 3,
        CommaSeparatedValue = 4,
    }

    public class DataObject
    {
        public DataObject()
        {
        }

        public DataObject(string format, object data)
        {
        }

        public void SetText(string textData)
        {
        }

        public void SetText(string textData, TextDataFormat format)
        {
        }

        public string[] GetFormats()
        {
            return [];
        }

        public string? GetText()
        {
            return null;
        }

        public bool TryGetData<T>(string format, out T? data)
        {
            data = default;
            return false;
        }
    }

    public class TreeViewEventArgs : EventArgs
    {
        public TreeNode? Node { get; set; }
    }

    public class TaskDialogIcon
    {
        public static readonly TaskDialogIcon None = new TaskDialogIcon();

        public static readonly TaskDialogIcon Information = new TaskDialogIcon();

        public static readonly TaskDialogIcon Warning = new TaskDialogIcon();

        public static readonly TaskDialogIcon Error = new TaskDialogIcon();

        public static readonly TaskDialogIcon Shield = new TaskDialogIcon();

        public static readonly TaskDialogIcon ShieldSuccessGreenBar = new TaskDialogIcon();

        public static readonly TaskDialogIcon ShieldWarningYellowBar = new TaskDialogIcon();

        public static readonly TaskDialogIcon ShieldErrorRedBar = new TaskDialogIcon();

        public static readonly TaskDialogIcon ShieldBlueBar = new TaskDialogIcon();

        public static readonly TaskDialogIcon ShieldGrayBar = new TaskDialogIcon();
    }

    public class TaskDialogButton
    {
        public static readonly TaskDialogButton OK = new TaskDialogButton();

        public static readonly TaskDialogButton Cancel = new TaskDialogButton();

        public static readonly TaskDialogButton Yes = new TaskDialogButton();

        public static readonly TaskDialogButton No = new TaskDialogButton();

        public static readonly TaskDialogButton Retry = new TaskDialogButton();

        public static readonly TaskDialogButton Close = new TaskDialogButton();

        public static readonly TaskDialogButton Abort = new TaskDialogButton();

        public static readonly TaskDialogButton Ignore = new TaskDialogButton();

        public TaskDialogButton()
        {
        }

        public TaskDialogButton(string text)
        {
        }

        public bool Enabled { get; set; } = true;
    }

    public class TaskDialogPage
    {
        public string? Caption { get; set; }

        public string? Heading { get; set; }

        public string? Text { get; set; }

        public TaskDialogIcon? Icon { get; set; }

        public bool AllowCancel { get; set; }

        public System.Collections.Generic.List<TaskDialogButton> Buttons { get; } = [];
    }

    public static class TaskDialog
    {
        public static TaskDialogButton ShowDialog(IWin32Window? owner, TaskDialogPage page)
        {
            return TaskDialogButton.OK;
        }

        public static TaskDialogButton ShowDialog(TaskDialogPage page)
        {
            return TaskDialogButton.OK;
        }

        public static System.Threading.Tasks.Task<TaskDialogButton> ShowDialogAsync(IWin32Window? owner, TaskDialogPage page)
        {
            return System.Threading.Tasks.Task.FromResult(TaskDialogButton.OK);
        }

        public static System.Threading.Tasks.Task<TaskDialogButton> ShowDialogAsync(TaskDialogPage page)
        {
            return System.Threading.Tasks.Task.FromResult(TaskDialogButton.OK);
        }
    }

    public class KeyEventArgs : EventArgs
    {
        public Keys KeyCode { get; set; }

        public Keys KeyData { get; set; }

        public bool Control { get; set; }

        public bool Shift { get; set; }

        public bool Alt { get; set; }

        public bool Handled { get; set; }

        public bool SuppressKeyPress { get; set; }
    }

    public enum Keys
    {
        None = 0,
        Back = 0x08,
        Tab = 0x09,
        Return = 0x0D,
        Escape = 0x1B,
        Space = 0x20,
        PageUp = 0x21,
        Prior = 0x21,
        PageDown = 0x22,
        Next = 0x22,
        End = 0x23,
        Home = 0x24,
        Left = 0x25,
        Up = 0x26,
        Right = 0x27,
        Down = 0x28,
        Insert = 0x2D,
        Delete = 0x2E,
        D0 = 0x30,
        D1 = 0x31,
        D2 = 0x32,
        D3 = 0x33,
        D4 = 0x34,
        D5 = 0x35,
        D6 = 0x36,
        D7 = 0x37,
        D8 = 0x38,
        D9 = 0x39,
        A = 0x41,
        B = 0x42,
        C = 0x43,
        D = 0x44,
        E = 0x45,
        F = 0x46,
        G = 0x47,
        H = 0x48,
        I = 0x49,
        J = 0x4A,
        K = 0x4B,
        L = 0x4C,
        M = 0x4D,
        N = 0x4E,
        O = 0x4F,
        P = 0x50,
        Q = 0x51,
        R = 0x52,
        S = 0x53,
        T = 0x54,
        U = 0x55,
        V = 0x56,
        W = 0x57,
        X = 0x58,
        Y = 0x59,
        Z = 0x5A,
        F1 = 0x70,
        F2 = 0x71,
        F3 = 0x72,
        F4 = 0x73,
        F5 = 0x74,
        F6 = 0x75,
        F7 = 0x76,
        F8 = 0x77,
        F9 = 0x78,
        F10 = 0x79,
        F11 = 0x7A,
        F12 = 0x7B,
        ShiftKey = 0x10,
        ControlKey = 0x11,
        Menu = 0x12,
        Oem1 = 0xBA,
        Oemcomma = 0xBC,
        OemMinus = 0xBD,
        OemPeriod = 0xBE,
        OemQuestion = 0xBF,
        Oemtilde = 0xC0,
        OemOpenBrackets = 0xDB,
        OemPipe = 0xDC,
        OemCloseBrackets = 0xDD,
        OemQuotes = 0xDE,
        Oem8 = 0xDF,
        Oem102 = 0xE2,
        Decimal = 0x6E,
        Modifiers = unchecked((int)0xFFFF0000),
        KeyCode = 0xFFFF,
        Control = 0x20000,
        Shift = 0x10000,
        Alt = 0x40000,
    }

    public class Control : System.ComponentModel.IComponent, IWin32Window
    {
        private string _text = string.Empty;

        [System.Diagnostics.CodeAnalysis.AllowNull]
        public virtual string Text
        {
            get => _text;
            set => _text = value ?? string.Empty;
        }

        public virtual bool Enabled { get; set; } = true;

        public virtual bool Visible { get; set; } = true;

        public bool IsDisposed { get; protected set; }

        public int Height { get; set; }

        public int Width { get; set; }

        public int TabIndex { get; set; }

        public string Name { get; set; } = string.Empty;

        public Drawing.Size Size { get; set; }

        public Drawing.Point Location { get; set; }

        public Padding Margin { get; set; } = new Padding(0);

        public Padding Padding { get; set; } = new Padding(0);

        public Drawing.SizeF AutoScaleDimensions { get; set; }

        public AutoScaleMode AutoScaleMode { get; set; }

        public AutoSizeMode AutoSizeMode { get; set; }

        public Drawing.Size MinimumSize { get; set; }

        public Drawing.Size MaximumSize { get; set; }

        public DockStyle Dock { get; set; }

        public bool AutoSize { get; set; }

        public Control? Parent { get; set; }

        public object? Tag { get; set; }

        private Drawing.Font _font = Drawing.SystemFonts.DefaultFont ?? new Drawing.Font("Arial", 9);

        [System.Diagnostics.CodeAnalysis.AllowNull]
        public virtual Drawing.Font Font
        {
            get => _font;
            set => _font = value ?? Drawing.SystemFonts.DefaultFont ?? new Drawing.Font("Arial", 9);
        }

        public Drawing.Color ForeColor { get; set; }

        public Drawing.Color BackColor { get; set; }

        public Drawing.Image? BackgroundImage { get; set; }

        public nint Handle => nint.Zero;

        public AnchorStyles Anchor { get; set; }

        public bool TabStop { get; set; } = true;

        public bool Focus()
        {
            return false;
        }

        public event EventHandler? TextChanged
        {
            add { }
            remove { }
        }

        public event EventHandler? Click
        {
            add { }
            remove { }
        }

        public Drawing.Graphics CreateGraphics()
        {
            return default!;
        }

        public Drawing.Point PointToClient(Drawing.Point p)
        {
            return p;
        }

        public void Refresh()
        {
        }

        public void SetStyle(ControlStyles flag, bool value)
        {
        }

        public void Update()
        {
        }

        public ControlCollection Controls { get; } = new ControlCollection();

        public virtual void Show()
        {
            Visible = true;
        }

        public virtual void Hide()
        {
            Visible = false;
        }

        public virtual void SuspendLayout()
        {
        }

        public virtual void ResumeLayout(bool performLayout = true)
        {
        }

        public virtual void PerformLayout()
        {
        }

        public event EventHandler? Load
        {
            add { }
            remove { }
        }

        public event EventHandler? Disposed
        {
            add { }
            remove { }
        }

        public event EventHandler<KeyEventArgs>? KeyDown
        {
            add { }
            remove { }
        }

        public System.ComponentModel.ISite? Site { get; set; }

        public Form? FindForm()
        {
            return null;
        }

        protected virtual void Dispose(bool disposing)
        {
        }

        protected virtual void OnLoad(EventArgs e)
        {
        }

        protected virtual bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            return false;
        }

        protected virtual void WndProc(ref Message m)
        {
        }

        public void Dispose()
        {
            IsDisposed = true;
        }
    }

    public class ControlCollection : System.Collections.Generic.List<Control>
    {
        public void Add(Control c, int column, int row)
        {
            Add(c);
        }
    }

    public class Padding
    {
        public Padding(int all)
        {
            Left = all;
            Top = all;
            Right = all;
            Bottom = all;
        }

        public Padding(int left, int top, int right, int bottom)
        {
            Left = left;
            Top = top;
            Right = right;
            Bottom = bottom;
        }

        public int Left { get; set; }

        public int Top { get; set; }

        public int Right { get; set; }

        public int Bottom { get; set; }
    }

    public class ColumnStyle
    {
        public ColumnStyle()
        {
        }

        public ColumnStyle(SizeType sizeType, float width = 0f)
        {
            SizeType = sizeType;
            Width = width;
        }

        public SizeType SizeType { get; set; }

        public float Width { get; set; }
    }

    public class RowStyle
    {
        public RowStyle()
        {
        }

        public RowStyle(SizeType sizeType, float height = 0f)
        {
        }
    }

    public class StyleCollection<T> : System.Collections.Generic.List<T>
    {
    }

    public class CheckBox : Control
    {
        public CheckState CheckState { get; set; }

        public bool Checked
        {
            get => CheckState == CheckState.Checked;
            set => CheckState = value ? CheckState.Checked : CheckState.Unchecked;
        }

        public bool ThreeState { get; set; }

        public bool UseVisualStyleBackColor { get; set; } = true;
    }

    public enum ScrollBars
    {
        None = 0,
        Horizontal = 1,
        Vertical = 2,
        Both = 3,
    }

    public class TextBox : Control
    {
        public bool ReadOnly { get; set; }

        public BorderStyle BorderStyle { get; set; }

        public bool Multiline { get; set; }

        public bool UseSystemPasswordChar { get; set; }

        public char PasswordChar { get; set; }

        public bool AcceptsReturn { get; set; }

        public bool AcceptsTab { get; set; }

        public bool WordWrap { get; set; }

        public ScrollBars ScrollBars { get; set; }

        public string[] Lines { get; set; } = [];
    }

    public class Label : Control
    {
    }

    public class TableLayoutPanel : Control
    {
        public int ColumnCount { get; set; }

        public int RowCount { get; set; }

        public new AutoSizeMode AutoSizeMode { get; set; }

        public StyleCollection<ColumnStyle> ColumnStyles { get; } = new StyleCollection<ColumnStyle>();

        public StyleCollection<RowStyle> RowStyles { get; } = new StyleCollection<RowStyle>();

        public new TableLayoutControlCollection Controls { get; } = new TableLayoutControlCollection();

        public void SetColumnSpan(Control c, int value)
        {
        }
    }

    public class FlowLayoutPanel : Control
    {
        public new AutoSizeMode AutoSizeMode { get; set; }

        public System.Windows.Forms.FlowDirection FlowDirection { get; set; }

        public bool WrapContents { get; set; }
    }

    public enum FlowDirection
    {
        LeftToRight = 0,
        TopDown = 1,
        RightToLeft = 2,
        BottomUp = 3,
    }

    public class GroupBox : Control
    {
    }

    public class TableLayoutControlCollection
    {
        public void Add(Control c, int column, int row)
        {
        }

        public void Add(Control c)
        {
        }
    }

    public class NumericUpDown : Control
    {
        public decimal Minimum { get; set; }

        public decimal Maximum { get; set; }

        public decimal Value { get; set; }
    }

    public class ComboBox : Control
    {
        public ComboBoxStyle DropDownStyle { get; set; }

        public ComboBoxItemCollection Items { get; } = new ComboBoxItemCollection();

        public int SelectedIndex { get; set; } = -1;

        public object? SelectedItem { get; set; }

        public int DropDownWidth { get; set; }

        public string DisplayMember { get; set; } = string.Empty;

        public int SelectionStart { get; set; }

        public int SelectionLength { get; set; }

        public string SelectedText { get; set; } = string.Empty;

        public new void Refresh()
        {
        }
    }

    public class ComboBoxItemCollection : System.Collections.Generic.List<object>
    {
        public void AddRange(string[] items)
        {
            foreach (string item in items)
            {
                Add(item);
            }
        }

        public void AddRange(object[] items)
        {
            foreach (object item in items)
            {
                Add(item);
            }
        }
    }

    public class ListBox : Control
    {
    }

    public class UserControl : Control
    {
        protected override void Dispose(bool disposing)
        {
        }
    }

    public class ToolStripMenuItem : ToolStripItem
    {
    }

    public class ToolStripItem : System.ComponentModel.IComponent
    {
        public bool IsDisposed { get; protected set; }

        public Drawing.Size Size { get; set; }

        public Drawing.Image? Image { get; set; }

        public int ImageIndex { get; set; } = -1;

        public string ImageKey { get; set; } = string.Empty;

        [System.Diagnostics.CodeAnalysis.AllowNull]
        public string Text
        {
            get => _text;
            set => _text = value ?? string.Empty;
        }

        private string _text = string.Empty;

        [System.Diagnostics.CodeAnalysis.AllowNull]
        public string ToolTipText
        {
            get => _toolTipText;
            set => _toolTipText = value ?? string.Empty;
        }

        private string _toolTipText = string.Empty;

        public bool Enabled { get; set; } = true;

        public bool Visible { get; set; } = true;

        public event EventHandler? Disposed
        {
            add { }
            remove { }
        }

        public System.ComponentModel.ISite? Site { get; set; }

        public void Dispose()
        {
            IsDisposed = true;
        }
    }

    public class ContextMenuStrip
    {
    }

    public enum FormWindowState
    {
        Normal = 0,
        Minimized = 1,
        Maximized = 2,
    }

    public class Form : Control, IWin32Window
    {
        public new nint Handle => nint.Zero;

        public static Form? ActiveForm => null;

        public bool ShowInTaskbar { get; set; } = true;

        public Drawing.Icon? Icon { get; set; }

        public FormWindowState WindowState { get; set; }

        public Form? Owner { get; set; }

        public Drawing.Size ClientSize { get; set; }

        public FormBorderStyle FormBorderStyle { get; set; }

        public FormStartPosition StartPosition { get; set; }

        public bool MaximizeBox { get; set; } = true;

        public bool MinimizeBox { get; set; } = true;

        public bool ShowIcon { get; set; } = true;

        public DialogResult ShowDialog()
        {
            return DialogResult.OK;
        }

        public DialogResult ShowDialog(IWin32Window? owner)
        {
            return DialogResult.OK;
        }

        public void Close()
        {
        }

        public IButtonControl? AcceptButton { get; set; }

        public IButtonControl? CancelButton { get; set; }

        public DialogResult DialogResult { get; set; }

        public event EventHandler? Shown
        {
            add { }
            remove { }
        }
    }

    public class KeysConverter : System.ComponentModel.TypeConverter
    {
        public string? ConvertToString(object? context, System.Globalization.CultureInfo? culture, Keys key)
        {
            return key.ToString();
        }
    }

    public class FormCollection : System.Collections.ObjectModel.Collection<Form>
    {
    }

    public class ToolTip
    {
        public string ToolTipTitle { get; set; } = string.Empty;

        public string GetToolTip(Control control)
        {
            return string.Empty;
        }

        public void SetToolTip(Control control, string text)
        {
        }
    }

    public class DataGridViewColumn
    {
        public bool Visible { get; set; } = true;

        public string? HeaderText { get; set; }

        public string? Name { get; set; }
    }

    public static class Application
    {
        public static string ExecutablePath
            => System.Reflection.Assembly.GetEntryAssembly()?.Location ?? string.Empty;

        public static string ProductName
            => System.Reflection.Assembly.GetEntryAssembly()?.GetName().Name ?? "GitExtensions";

        public static FormCollection OpenForms { get; } = new FormCollection();

        public static SystemColorMode SystemColorMode { get; set; } = SystemColorMode.Classic;

        public static bool IsDarkModeEnabled => SystemColorMode == SystemColorMode.Dark;

        public static void OnThreadException(Exception t)
        {
        }

        public static void DoEvents()
        {
        }

        public static event System.Threading.ThreadExceptionEventHandler? ThreadException
        {
            add { }
            remove { }
        }
    }

    public static class MessageBox
    {
        public static DialogResult Show(
            IWin32Window? owner,
            string text,
            string caption,
            MessageBoxButtons buttons,
            MessageBoxIcon icon,
            MessageBoxDefaultButton defaultButton = MessageBoxDefaultButton.Button1)
        {
            return DialogResult.OK;
        }
    }

    public class FolderBrowserDialog : IDisposable
    {
        public string SelectedPath { get; set; } = string.Empty;

        public DialogResult ShowDialog(IWin32Window? owner)
        {
            return DialogResult.Cancel;
        }

        public void Dispose()
        {
        }
    }
}

#pragma warning restore SA1502, SA1136, SA1201, SA1402, SA1649, SA1600, SA1516, SA1203, CS1591
#endif
