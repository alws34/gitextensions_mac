# WinForms → Avalonia Cheat Sheet

Quick reference for porting WinForms code to Avalonia. Read `AGENT-GUIDE.md` first.

---

## Control Mapping

| WinForms | Avalonia | Notes |
|----------|---------|-------|
| `Form` | `Window` | |
| `UserControl` | `UserControl` | |
| `Panel` | `Panel` or `Border` | `Border` if you need background/border styling |
| `FlowLayoutPanel` | `WrapPanel` or `StackPanel` | |
| `TableLayoutPanel` | `Grid` | Define `RowDefinitions`/`ColumnDefinitions` |
| `SplitContainer` | `Grid` + `GridSplitter` | |
| `GroupBox` | `GroupBox` | |
| `TabControl` | `TabControl` | |
| `TabPage` | `TabItem` | |
| `DataGridView` | `DataGrid` | |
| `TreeView` | `TreeView` | |
| `ListView` | `ListBox` or `DataGrid` | Use DataGrid for multi-column |
| `ListBox` | `ListBox` | |
| `ComboBox` | `ComboBox` | |
| `TextBox` | `TextBox` | |
| `RichTextBox` | `AvaloniaEdit.TextEditor` | Required for diff/editor views |
| `Label` | `TextBlock` | |
| `Button` | `Button` | |
| `CheckBox` | `CheckBox` | |
| `RadioButton` | `RadioButton` | |
| `PictureBox` | `Image` | |
| `ProgressBar` | `ProgressBar` | |
| `NumericUpDown` | `NumericUpDown` | |
| `TrackBar` | `Slider` | |
| `DateTimePicker` | `CalendarDatePicker` | |
| `ToolStrip` | `Menu` or `ToolBar` | |
| `MenuStrip` | `Menu` | |
| `ContextMenuStrip` | `ContextMenu` | |
| `ToolStripMenuItem` | `MenuItem` | |
| `ToolStripSeparator` | `Separator` | |
| `ToolStripButton` | `Button` in `ToolBar` | |
| `StatusStrip` | Custom `DockPanel` at bottom | |
| `NotifyIcon` | `TrayIcon` | Avalonia 11+ |
| `ToolTip` | `ToolTip.Tip` attached property | |
| `ImageList` | Not needed — use `Image` directly | |
| `Timer` | `DispatcherTimer` | |
| `OpenFileDialog` | `OpenFileDialog` (Avalonia.Platform.Storage) | |
| `SaveFileDialog` | `SaveFileDialog` (Avalonia.Platform.Storage) | |
| `FolderBrowserDialog` | `OpenFolderDialog` | |
| `ColorDialog` | `ColorPicker` (Avalonia.Controls.ColorPicker) | |
| `FontDialog` | Manual implementation | Rare — handle per use case |

---

## Layout

### WinForms anchor/dock → Avalonia layout

```xml
<!-- WinForms: control docked to bottom -->
<control Dock="Bottom" />

<!-- Avalonia: use DockPanel -->
<DockPanel>
    <Button DockPanel.Dock="Bottom" Content="OK" />
    <TextBox />  <!-- fills remaining space -->
</DockPanel>
```

```xml
<!-- WinForms: control fills parent -->
<control Dock="Fill" />

<!-- Avalonia: use HorizontalAlignment/VerticalAlignment -->
<TextBox HorizontalAlignment="Stretch" VerticalAlignment="Stretch" />
```

```xml
<!-- WinForms: TableLayoutPanel 2x2 -->
<TableLayoutPanel ColumnCount="2" RowCount="2" />

<!-- Avalonia: Grid -->
<Grid>
    <Grid.ColumnDefinitions>
        <ColumnDefinition Width="*" />
        <ColumnDefinition Width="*" />
    </Grid.ColumnDefinitions>
    <Grid.RowDefinitions>
        <RowDefinition Height="Auto" />
        <RowDefinition Height="*" />
    </Grid.RowDefinitions>
    <TextBlock Grid.Row="0" Grid.Column="0" Text="Label" />
    <TextBox Grid.Row="0" Grid.Column="1" />
</Grid>
```

---

## Drawing / Graphics

The custom graph renderer uses `System.Drawing`. Replace with Avalonia drawing:

| System.Drawing | Avalonia.Media |
|---------------|---------------|
| `Graphics` | `DrawingContext` (from `OnRender` override) |
| `Color` | `Color` |
| `Color.FromArgb(a,r,g,b)` | `Color.FromArgb(a,r,g,b)` (same API) |
| `Pen(color, width)` | `new Pen(new SolidColorBrush(color), width)` |
| `SolidBrush(color)` | `new SolidColorBrush(color)` |
| `g.DrawLine(pen, x1,y1, x2,y2)` | `ctx.DrawLine(pen, new Point(x1,y1), new Point(x2,y2))` |
| `g.FillEllipse(brush, rect)` | `ctx.DrawEllipse(brush, null, center, rx, ry)` |
| `g.DrawEllipse(pen, rect)` | `ctx.DrawEllipse(null, pen, center, rx, ry)` |
| `g.FillRectangle(brush, rect)` | `ctx.FillRectangle(brush, rect)` |
| `g.DrawRectangle(pen, rect)` | `ctx.DrawRectangle(null, pen, rect)` |
| `g.DrawString(s, font, brush, x, y)` | `ctx.DrawText(formattedText, new Point(x,y))` |
| `new RectangleF(x,y,w,h)` | `new Rect(x,y,w,h)` |
| `new PointF(x,y)` | `new Point(x,y)` |
| `new SizeF(w,h)` | `new Size(w,h)` |
| `Bitmap` | `WriteableBitmap` or `RenderTargetBitmap` |

### Custom drawing control

```csharp
// WinForms
protected override void OnPaint(PaintEventArgs e)
{
    Graphics g = e.Graphics;
    g.DrawLine(pen, 0, 0, 100, 100);
}

// Avalonia
public override void Render(DrawingContext context)
{
    base.Render(context);
    context.DrawLine(pen, new Point(0, 0), new Point(100, 100));
}
```

---

## Events

| WinForms | Avalonia |
|----------|---------|
| `button.Click += Handler` | `button.Click += Handler` (same) |
| `textBox.TextChanged += Handler` | `textBox.TextChanged += Handler` (same) |
| `listView.SelectedIndexChanged += Handler` | `listBox.SelectionChanged += Handler` |
| `form.Load += Handler` | Override `OnLoaded` or use `Loaded` event |
| `form.FormClosing += Handler` | Override `OnClosing` |
| `form.Shown += Handler` | Override `OnOpened` |
| `Control.KeyDown += Handler` | `Control.KeyDown += Handler` (same) |
| `DataGridView.CellClick` | `DataGrid.CellPointerPressed` |

---

## Threading

```csharp
// WinForms — marshal to UI thread
control.Invoke(() => label.Text = "done");
control.BeginInvoke(() => label.Text = "done");

// Avalonia
await Dispatcher.UIThread.InvokeAsync(() => label.Content = "done");
Dispatcher.UIThread.Post(() => label.Content = "done");
```

```csharp
// WinForms — check if invoke required
if (control.InvokeRequired)
    control.Invoke(action);
else
    action();

// Avalonia — just always use the dispatcher
Dispatcher.UIThread.Post(action);
```

---

## Data Binding

```xml
<!-- Simple binding -->
<TextBlock Text="{Binding AuthorName}" />

<!-- Two-way binding -->
<TextBox Text="{Binding CommitMessage, Mode=TwoWay}" />

<!-- Converter -->
<TextBlock Text="{Binding Date, Converter={StaticResource DateConverter}}" />

<!-- Visibility binding -->
<Border IsVisible="{Binding IsLoading}" />
```

```csharp
// Set DataContext (code-behind)
DataContext = new MyViewModel(module);

// Or bind in AXAML
<Window.DataContext>
    <local:MyViewModel />
</Window.DataContext>
```

---

## Dialogs (ShowDialog equivalent)

```csharp
// WinForms
var form = new FormFoo();
if (form.ShowDialog() == DialogResult.OK)
{
    var result = form.Result;
}

// Avalonia
var dialog = new FooDialog();
var result = await dialog.ShowDialog<FooResult?>(parentWindow);
if (result is not null)
{
    // use result
}
```

To close a dialog and return a result:
```csharp
// In dialog code-behind
private void OkButton_Click(object sender, RoutedEventArgs e)
{
    Close(new FooResult { Value = textBox.Text });
}

private void CancelButton_Click(object sender, RoutedEventArgs e)
{
    Close(null);
}
```

---

## MessageBox

```csharp
// WinForms
MessageBox.Show("Message", "Title");
var result = MessageBox.Show("Sure?", "Confirm", MessageBoxButtons.YesNo);

// Avalonia (using MsBox.Avalonia)
await MessageBoxManager.GetMessageBoxStandard("Title", "Message").ShowAsync();

var result = await MessageBoxManager.GetMessageBoxStandard(
    "Confirm", "Sure?", ButtonEnum.YesNo).ShowAsync();
if (result == ButtonResult.Yes) { ... }
```

---

## Colors and Theming

```csharp
// WinForms
button.BackColor = Color.Red;
label.ForeColor = SystemColors.ControlText;

// Avalonia (code-behind)
button.Background = new SolidColorBrush(Colors.Red);
// Prefer AXAML bindings to theme resources instead of code
```

```xml
<!-- Avalonia AXAML — use theme resources -->
<Button Background="{DynamicResource SystemAccentColor}" />
<TextBlock Foreground="{DynamicResource SystemBaseHighColor}" />
```

---

## Common Patterns in GitExtensions

### GitModule access in a control

```csharp
// The control inherits GitModuleControl
// Module property is provided by the base class
public class MyControl : GitModuleControl
{
    public void LoadData()
    {
        var branches = Module.GetBranches(); // Module is available
    }
}
```

### Running a git command with output

```csharp
// GitCommands provides async wrappers
var result = await Module.GitExecutable.GetOutputAsync("log --oneline -10");
```

### Showing progress

```csharp
// Use FormStatus equivalent — the StatusDialog from Task 14
// Or use the progress binding pattern with a ViewModel
IsLoading = true;
try
{
    await Task.Run(() => DoGitOperation());
}
finally
{
    IsLoading = false;
}
```
