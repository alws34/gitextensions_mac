# Mac Port — Plan A: Foundation

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Create the `gitextensions-mac` fork with a clean macOS build and a runnable Avalonia app skeleton with all shared infrastructure.

**Architecture:** Fork the repo, strip Windows-specific build config and packages, add Avalonia, fix the 4 non-UI files with Windows APIs, then build the Avalonia infrastructure layer (base classes, theming, credential store, settings backend) that all screen-port agents depend on.

**Tech Stack:** .NET 10, C# 14, Avalonia 11, Avalonia.ReactiveUI, AvaloniaEdit, MsBox.Avalonia, Microsoft.Extensions.Configuration.Json

**Reference docs:**
- `docs/mac-port/AGENT-GUIDE.md`
- `docs/mac-port/PACKAGE-REPLACEMENTS.md`
- `docs/mac-port/SETTINGS-MIGRATION.md`
- `docs/superpowers/specs/2026-04-19-mac-port-design.md`

---

## File Map

**Modified (existing):**
- `eng/RepoLayout.props` — change target framework
- `Directory.Build.props` — remove UseWindowsForms
- `Directory.Packages.props` — swap packages
- `src/app/GitCommands/Settings/AppSettings.cs` — remove WinForms Application class refs
- `src/app/GitCommands/Git/Extensions/ProcessExtensions.cs` — guard P/Invoke
- `src/app/GitCommands/DiffMergeTools/VsDiffMerge.cs` — already guarded, minor cleanup
- `src/app/GitCommands/ServiceContainerRegistry.cs` — audit (already clean)

**Created:**
- `GitExtensions.Mac.slnx` — Mac-only solution
- `src/app/GitUI.Avalonia/GitUI.Avalonia.csproj`
- `src/app/GitUI.Avalonia/Program.cs`
- `src/app/GitUI.Avalonia/App.axaml` + `App.axaml.cs`
- `src/app/GitUI.Avalonia/AppTheme.axaml`
- `src/app/GitUI.Avalonia/Base/GitExtensionsWindow.axaml` + `.axaml.cs`
- `src/app/GitUI.Avalonia/Base/GitExtensionsDialog.axaml` + `.axaml.cs`
- `src/app/GitUI.Avalonia/Base/GitModuleControl.axaml` + `.axaml.cs`
- `src/app/GitUI.Avalonia/Converters/BoolToVisibilityConverter.cs`
- `src/app/GitUI.Avalonia/Converters/NullToVisibilityConverter.cs`
- `src/app/GitUI.Avalonia/Converters/InverseBoolConverter.cs`
- `src/app/GitUI.Avalonia/Controls/LoadingSpinner.axaml` + `.axaml.cs`
- `src/app/GitUI.Avalonia/Controls/SplitContainer.axaml`
- `src/app/GitUI.Avalonia/Infrastructure/ICredentialStore.cs`
- `src/app/GitUI.Avalonia/Infrastructure/MacKeychainCredentialStore.cs`
- `src/app/GitUI.Avalonia/Infrastructure/ISettingsBackend.cs`
- `src/app/GitUI.Avalonia/Infrastructure/JsonSettingsBackend.cs`
- `src/app/GitUI.Avalonia/Infrastructure/MacDiffMergeTools.cs`
- `src/app/GitUI.Avalonia/Dialogs/MessageDialog.axaml` + `.axaml.cs`
- `src/app/GitCommands/DiffMergeTools/FileMerge.cs` (new Mac tool)
- `src/app/GitCommands/DiffMergeTools/Kaleidoscope.cs` (new Mac tool)
- `tests/app/GitUI.Avalonia.Tests/Infrastructure/JsonSettingsBackendTests.cs`
- `tests/app/GitUI.Avalonia.Tests/Infrastructure/MacKeychainCredentialStoreTests.cs`

---

## Task 1: Fork and change target framework

**Files:**
- Modify: `eng/RepoLayout.props`
- Modify: `Directory.Build.props`

- [ ] **Step 1: Edit `eng/RepoLayout.props`**

Change line 12:
```xml
<SolutionTargetFramework>net10.0-windows</SolutionTargetFramework>
```
to:
```xml
<SolutionTargetFramework>net10.0</SolutionTargetFramework>
```

- [ ] **Step 2: Edit `Directory.Build.props`**

Remove this line:
```xml
<UseWindowsForms>true</UseWindowsForms>
```

- [ ] **Step 3: Delete Windows shell extension**

```bash
rm -rf src/native/GitExtensionsShellEx/
```

- [ ] **Step 4: Commit**

```bash
git add eng/RepoLayout.props Directory.Build.props
git commit -m "build: change target framework to net10.0 for Mac port"
```

---

## Task 2: Update packages

**Files:**
- Modify: `Directory.Packages.props`

- [ ] **Step 1: Remove Windows-only packages from `Directory.Packages.props`**

Remove these `<PackageVersion>` entries:
- `Microsoft-WindowsAPICodePack-Core`
- `Microsoft-WindowsAPICodePack-Shell`
- `ConEmu.Core`
- `AdysTech.CredentialManager`
- `AppInsights.WindowsDesktop`
- `WiX`
- `vswhere`
- `EnvDTE`

- [ ] **Step 2: Add Avalonia packages to `Directory.Packages.props`**

Add inside the `<ItemGroup>`:
```xml
<!-- Avalonia UI stack -->
<PackageVersion Include="Avalonia" Version="11.2.3" />
<PackageVersion Include="Avalonia.Desktop" Version="11.2.3" />
<PackageVersion Include="Avalonia.Themes.Fluent" Version="11.2.3" />
<PackageVersion Include="Avalonia.ReactiveUI" Version="11.2.3" />
<PackageVersion Include="AvaloniaEdit" Version="11.1.0" />
<PackageVersion Include="MsBox.Avalonia" Version="3.1.0" />
<PackageVersion Include="Microsoft.Extensions.Configuration" Version="10.0.0" />
<PackageVersion Include="Microsoft.Extensions.Configuration.Json" Version="10.0.0" />
```

- [ ] **Step 3: Commit**

```bash
git add Directory.Packages.props
git commit -m "build: replace Windows-only packages with Avalonia stack"
```

---

## Task 3: Fix AppSettings (remove WinForms Application class dependency)

**Files:**
- Modify: `src/app/GitCommands/Settings/AppSettings.cs`

The problem: `AppSettings.cs` uses `System.Windows.Forms.Application` for version, paths, and executable path. These are WinForms-only.

- [ ] **Step 1: Add using for cross-platform alternatives at top of `AppSettings.cs`**

Ensure these usings exist (remove `using Microsoft.Win32;` if present at top level — it's only needed in `VsDiffMerge.cs`):
```csharp
using System.Reflection;
```

- [ ] **Step 2: Replace `Application.ProductVersion`**

Find:
```csharp
public static string ProductVersion => Application.ProductVersion;
```
Replace with:
```csharp
public static string ProductVersion =>
    Assembly.GetEntryAssembly()?.GetName().Version?.ToString() ?? "0.0.0.0";
```

- [ ] **Step 3: Replace `Application.ExecutablePath`**

Find:
```csharp
private static string _applicationExecutablePath = Application.ExecutablePath;
```
Replace with:
```csharp
private static string _applicationExecutablePath =
    Environment.ProcessPath ?? AppContext.BaseDirectory;
```

- [ ] **Step 4: Replace `Application.UserAppDataPath` usage**

Find the `ApplicationDataPath` lazy initializer that calls `Application.UserAppDataPath`. Replace with cross-platform path:
```csharp
ApplicationDataPath = new Lazy<string?>(() =>
{
    if (IsPortable())
    {
        return GetGitExtensionsDirectory();
    }

    // macOS/Linux: ~/.config/gitextensions
    // Windows: %APPDATA%/GitExtensions (fallback)
    string configBase = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
    return Path.Join(configBase, ApplicationId);
});
```

- [ ] **Step 5: Replace `Application.LocalUserAppDataPath` / `LocalApplicationDataPath`**

Find the `LocalApplicationDataPath` lazy initializer. Replace with:
```csharp
LocalApplicationDataPath = new Lazy<string?>(() =>
{
    if (IsPortable())
    {
        return GetGitExtensionsDirectory();
    }

    string localConfigBase = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
    return Path.Join(localConfigBase, ApplicationId);
});
```

- [ ] **Step 6: Remove any remaining `System.Windows.Forms` using directives from AppSettings.cs**

```bash
grep -n "Windows.Forms\|System.Windows" src/app/GitCommands/Settings/AppSettings.cs
```
Remove any found lines.

- [ ] **Step 7: Commit**

```bash
git add src/app/GitCommands/Settings/AppSettings.cs
git commit -m "fix: replace WinForms Application class with cross-platform equivalents in AppSettings"
```

---

## Task 4: Fix ProcessExtensions (guard Win32 P/Invoke)

**Files:**
- Modify: `src/app/GitCommands/Git/Extensions/ProcessExtensions.cs`

The existing code already has `OperatingSystem.IsWindows()` guard for the logic, but the `[DllImport]` declarations will cause issues on non-Windows builds. Fix by moving the entire `NativeMethods` class behind a compile-time guard.

- [ ] **Step 1: Restructure `ProcessExtensions.cs`**

Replace the entire file with:
```csharp
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace GitCommands.Git.Extensions;

public static class ProcessExtensions
{
    public static void TerminateTree(this Process process)
    {
        if (OperatingSystem.IsWindows())
        {
            WindowsProcessHelpers.SendCtrlC(process);
        }

        if (!process.HasExited)
        {
            process.Kill();
        }
    }
}

[SupportedOSPlatform("windows")]
internal static class WindowsProcessHelpers
{
    public static void SendCtrlC(Process process)
    {
        NativeMethods.AttachConsole(process.Id);
        NativeMethods.SetConsoleCtrlHandler(IntPtr.Zero, add: true);
        NativeMethods.GenerateConsoleCtrlEvent(0, 0);

        if (!process.HasExited)
        {
            process.WaitForExit(500);
        }
    }

    private static class NativeMethods
    {
        [DllImport("kernel32.dll")]
        public static extern bool SetConsoleCtrlHandler(IntPtr handlerRoutine, bool add);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool AttachConsole(int dwProcessId);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GenerateConsoleCtrlEvent(uint dwCtrlEvent, int dwProcessGroupId);
    }
}
```

- [ ] **Step 2: Commit**

```bash
git add src/app/GitCommands/Git/Extensions/ProcessExtensions.cs
git commit -m "fix: guard Windows P/Invoke in ProcessExtensions behind SupportedOSPlatform"
```

---

## Task 5: Add Mac diff/merge tools

**Files:**
- Create: `src/app/GitCommands/DiffMergeTools/FileMerge.cs`
- Create: `src/app/GitCommands/DiffMergeTools/Kaleidoscope.cs`

`RegisteredDiffMergeTools` uses reflection to auto-discover all `DiffMergeTool` subclasses. New tools are found automatically — just create the class.

- [ ] **Step 1: Create `FileMerge.cs`**

```csharp
namespace GitCommands.DiffMergeTools;

/// <summary>
/// Apple FileMerge (ships with Xcode Command Line Tools).
/// Launched via the opendiff command-line tool.
/// </summary>
internal class FileMerge : DiffMergeTool
{
    /// <inheritdoc />
    public override string DiffCommand => "\"$LOCAL\" \"$REMOTE\"";

    /// <inheritdoc />
    public override string ExeFileName => "opendiff";

    /// <inheritdoc />
    public override string MergeCommand => "\"$LOCAL\" \"$REMOTE\" -ancestor \"$BASE\" -merge \"$MERGED\"";

    /// <inheritdoc />
    public override string Name => "opendiff";

    /// <inheritdoc />
    public override IEnumerable<string> SearchPaths =>
    [
        "/usr/bin/opendiff"
    ];
}
```

- [ ] **Step 2: Create `Kaleidoscope.cs`**

```csharp
namespace GitCommands.DiffMergeTools;

/// <summary>
/// Kaleidoscope diff/merge tool for macOS.
/// </summary>
internal class Kaleidoscope : DiffMergeTool
{
    /// <inheritdoc />
    public override string DiffCommand => "\"$LOCAL\" \"$REMOTE\"";

    /// <inheritdoc />
    public override string ExeFileName => "ksdiff";

    /// <inheritdoc />
    public override string MergeCommand => "--merge --output \"$MERGED\" --base \"$BASE\" -- \"$LOCAL\" \"$REMOTE\"";

    /// <inheritdoc />
    public override string Name => "kaleidoscope";

    /// <inheritdoc />
    public override IEnumerable<string> SearchPaths =>
    [
        "/usr/local/bin/ksdiff",
        "/opt/homebrew/bin/ksdiff"
    ];
}
```

- [ ] **Step 3: Commit**

```bash
git add src/app/GitCommands/DiffMergeTools/FileMerge.cs \
        src/app/GitCommands/DiffMergeTools/Kaleidoscope.cs
git commit -m "feat: add macOS diff/merge tools (opendiff, Kaleidoscope)"
```

---

## Task 6: Create Mac solution file

**Files:**
- Create: `GitExtensions.Mac.slnx`

- [ ] **Step 1: Copy the existing solution**

```bash
cp GitExtensions.slnx GitExtensions.Mac.slnx
```

- [ ] **Step 2: Edit `GitExtensions.Mac.slnx`**

Remove these project references from the solution file (they are Windows-only):
- Any project under `setup/` (WiX installer)
- `src/app/BugReporter/` (Windows crash reporter — check if it has WinForms deps)
- `src/native/GitExtensionsShellEx/` (already deleted in Task 1)
- `src/app/GitUI/` (WinForms — replaced by GitUI.Avalonia)

Add the new project:
- `src/app/GitUI.Avalonia/GitUI.Avalonia.csproj`

- [ ] **Step 3: Verify the solution lists cleanly**

```bash
dotnet sln GitExtensions.Mac.slnx list
```

Expected: lists `GitCommands`, `GitExtensions.Extensibility`, `GitExtUtils`, `ResourceManager`, all plugins, `GitUI.Avalonia`, and test projects. Does NOT list `GitUI` or setup projects.

- [ ] **Step 4: Commit**

```bash
git add GitExtensions.Mac.slnx
git commit -m "build: add Mac solution file excluding WinForms and setup projects"
```

---

## Task 7: Create GitUI.Avalonia project skeleton

**Files:**
- Create: `src/app/GitUI.Avalonia/GitUI.Avalonia.csproj`
- Create: `src/app/GitUI.Avalonia/Program.cs`
- Create: `src/app/GitUI.Avalonia/App.axaml`
- Create: `src/app/GitUI.Avalonia/App.axaml.cs`

- [ ] **Step 1: Create `GitUI.Avalonia.csproj`**

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <RootNamespace>GitUI.Avalonia</RootNamespace>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Avalonia" />
    <PackageReference Include="Avalonia.Desktop" />
    <PackageReference Include="Avalonia.Themes.Fluent" />
    <PackageReference Include="Avalonia.ReactiveUI" />
    <PackageReference Include="AvaloniaEdit" />
    <PackageReference Include="MsBox.Avalonia" />
    <PackageReference Include="Microsoft.Extensions.Configuration" />
    <PackageReference Include="Microsoft.Extensions.Configuration.Json" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="../GitCommands/GitCommands.csproj" />
    <ProjectReference Include="../GitExtensions.Extensibility/GitExtensions.Extensibility.csproj" />
    <ProjectReference Include="../GitExtUtils/GitExtUtils.csproj" />
    <ProjectReference Include="../ResourceManager/ResourceManager.csproj" />
    <ProjectReference Include="../../plugins/GitUIPluginInterfaces/GitUIPluginInterfaces.csproj" />
  </ItemGroup>
</Project>
```

- [ ] **Step 2: Create `Program.cs`**

```csharp
using Avalonia;
using GitUI.Avalonia;

AppBuilder.Configure<App>()
    .UsePlatformDetect()
    .WithInterFont()
    .LogToTrace()
    .StartWithClassicDesktopLifetime(args);
```

- [ ] **Step 3: Create `App.axaml`**

```xml
<Application xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             x:Class="GitUI.Avalonia.App">
    <Application.Styles>
        <FluentTheme />
        <StyleInclude Source="avares://GitUI.Avalonia/AppTheme.axaml" />
    </Application.Styles>
</Application>
```

- [ ] **Step 4: Create `App.axaml.cs`**

```csharp
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using GitUI.Avalonia.Infrastructure;

namespace GitUI.Avalonia;

public class App : Application
{
    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Temporary: open a blank window to prove the app runs.
            // MainWindow is added in Plan B (Task 2).
            desktop.MainWindow = new Avalonia.Controls.Window
            {
                Title = "Git Extensions",
                Width = 1024,
                Height = 768
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}
```

- [ ] **Step 5: Verify the project builds**

```bash
dotnet build GitExtensions.Mac.slnx
```

Expected: Build succeeded, 0 errors.

- [ ] **Step 6: Verify the app runs**

```bash
dotnet run --project src/app/GitUI.Avalonia/GitUI.Avalonia.csproj
```

Expected: A blank Avalonia window titled "Git Extensions" opens on macOS.

- [ ] **Step 7: Commit**

```bash
git add src/app/GitUI.Avalonia/
git commit -m "feat: add GitUI.Avalonia project skeleton, blank window runs on Mac"
```

---

## Task 8: AppTheme and brand colors

**Files:**
- Create: `src/app/GitUI.Avalonia/AppTheme.axaml`

- [ ] **Step 1: Create `AppTheme.axaml`**

```xml
<ResourceDictionary xmlns="https://github.com/avaloniaui"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">

    <!-- Graph lane colors (match Windows version) -->
    <Color x:Key="GraphLane1">#FF0000CD</Color>
    <Color x:Key="GraphLane2">#FFFF8C00</Color>
    <Color x:Key="GraphLane3">#FF008000</Color>
    <Color x:Key="GraphLane4">#FF800080</Color>
    <Color x:Key="GraphLane5">#FFFF0000</Color>
    <Color x:Key="GraphLane6">#FF008080</Color>
    <Color x:Key="GraphLane7">#FFFF00FF</Color>

    <!-- Diff colors -->
    <SolidColorBrush x:Key="DiffAddedBackground">#22003300</SolidColorBrush>
    <SolidColorBrush x:Key="DiffAddedForeground">#FF00AA00</SolidColorBrush>
    <SolidColorBrush x:Key="DiffRemovedBackground">#22330000</SolidColorBrush>
    <SolidColorBrush x:Key="DiffRemovedForeground">#FFAA0000</SolidColorBrush>
    <SolidColorBrush x:Key="DiffSectionBackground">#22282800</SolidColorBrush>
    <SolidColorBrush x:Key="DiffSectionForeground">#FFAAAA00</SolidColorBrush>

    <!-- Ref label colors -->
    <SolidColorBrush x:Key="RefLabelLocalBranch">#FF1E90FF</SolidColorBrush>
    <SolidColorBrush x:Key="RefLabelRemoteBranch">#FF32CD32</SolidColorBrush>
    <SolidColorBrush x:Key="RefLabelTag">#FFFFB900</SolidColorBrush>
    <SolidColorBrush x:Key="RefLabelHead">#FFFF4500</SolidColorBrush>

    <!-- Monospace font for diff/editor views -->
    <FontFamily x:Key="MonospaceFont">Menlo, Monaco, Courier New, monospace</FontFamily>

</ResourceDictionary>
```

- [ ] **Step 2: Verify app still builds and runs**

```bash
dotnet run --project src/app/GitUI.Avalonia/GitUI.Avalonia.csproj
```

Expected: Window opens. If theme has AXAML errors they will appear in console.

- [ ] **Step 3: Commit**

```bash
git add src/app/GitUI.Avalonia/AppTheme.axaml
git commit -m "feat: add AppTheme with GitExtensions brand colors and diff palette"
```

---

## Task 9: Base window and dialog classes

**Files:**
- Create: `src/app/GitUI.Avalonia/Base/GitExtensionsWindow.axaml`
- Create: `src/app/GitUI.Avalonia/Base/GitExtensionsWindow.axaml.cs`
- Create: `src/app/GitUI.Avalonia/Base/GitExtensionsDialog.axaml`
- Create: `src/app/GitUI.Avalonia/Base/GitExtensionsDialog.axaml.cs`
- Create: `src/app/GitUI.Avalonia/Base/GitModuleControl.axaml`
- Create: `src/app/GitUI.Avalonia/Base/GitModuleControl.axaml.cs`

- [ ] **Step 1: Create `Base/GitExtensionsWindow.axaml`**

```xml
<Window xmlns="https://github.com/avaloniaui"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        x:Class="GitUI.Avalonia.Base.GitExtensionsWindow">
</Window>
```

- [ ] **Step 2: Create `Base/GitExtensionsWindow.axaml.cs`**

```csharp
using Avalonia.Controls;
using GitExtensions.Extensibility.Git;
using GitUIPluginInterfaces;

namespace GitUI.Avalonia.Base;

/// <summary>
/// Base class for all top-level windows in GitExtensions Mac.
/// Replaces WinForms GitExtensionsForm.
/// </summary>
public class GitExtensionsWindow : Window
{
    public IServiceProvider? ServiceProvider { get; init; }
    public IGitUICommandsSource? UICommandsSource { get; set; }

    protected GitExtensionsWindow() { }
}
```

- [ ] **Step 3: Create `Base/GitExtensionsDialog.axaml`**

```xml
<Window xmlns="https://github.com/avaloniaui"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        x:Class="GitUI.Avalonia.Base.GitExtensionsDialog"
        SizeToContent="WidthAndHeight"
        CanResize="False">
</Window>
```

- [ ] **Step 4: Create `Base/GitExtensionsDialog.axaml.cs`**

```csharp
using Avalonia.Controls;
using Avalonia.Input;

namespace GitUI.Avalonia.Base;

/// <summary>
/// Base class for all modal dialogs in GitExtensions Mac.
/// Replaces WinForms GitExtensionsDialog.
/// Supports typed results: var result = await dialog.ShowDialog&lt;T&gt;(parent);
/// </summary>
public class GitExtensionsDialog : Window
{
    protected GitExtensionsDialog()
    {
        KeyDown += OnKeyDown;
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            Close(null);
        }
    }
}
```

- [ ] **Step 5: Create `Base/GitModuleControl.axaml`**

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             x:Class="GitUI.Avalonia.Base.GitModuleControl">
</UserControl>
```

- [ ] **Step 6: Create `Base/GitModuleControl.axaml.cs`**

```csharp
using Avalonia.Controls;
using GitCommands;
using GitUIPluginInterfaces;

namespace GitUI.Avalonia.Base;

/// <summary>
/// Base class for all embedded controls that need access to a GitModule.
/// Replaces WinForms GitModuleControl.
/// </summary>
public class GitModuleControl : UserControl
{
    private GitModule? _module;
    private IGitUICommandsSource? _uiCommandsSource;

    public GitModule? Module
    {
        get => _module;
        set
        {
            _module = value;
            if (value is not null)
            {
                OnModuleSet();
            }
        }
    }

    public IGitUICommandsSource? UICommandsSource
    {
        get => _uiCommandsSource;
        set
        {
            _uiCommandsSource = value;
            if (value is not null)
            {
                OnUICommandsSourceSet();
            }
        }
    }

    /// <summary>Override to react when the Module is set.</summary>
    protected virtual void OnModuleSet() { }

    /// <summary>Override to react when UICommandsSource is set.</summary>
    protected virtual void OnUICommandsSourceSet() { }
}
```

- [ ] **Step 7: Verify build**

```bash
dotnet build GitExtensions.Mac.slnx
```

Expected: 0 errors.

- [ ] **Step 8: Commit**

```bash
git add src/app/GitUI.Avalonia/Base/
git commit -m "feat: add GitExtensionsWindow, GitExtensionsDialog, GitModuleControl base classes"
```

---

## Task 10: Converters

**Files:**
- Create: `src/app/GitUI.Avalonia/Converters/BoolToVisibilityConverter.cs`
- Create: `src/app/GitUI.Avalonia/Converters/NullToVisibilityConverter.cs`
- Create: `src/app/GitUI.Avalonia/Converters/InverseBoolConverter.cs`

- [ ] **Step 1: Create `Converters/BoolToVisibilityConverter.cs`**

```csharp
using System.Globalization;
using Avalonia.Data.Converters;

namespace GitUI.Avalonia.Converters;

/// <summary>Maps bool → IsVisible. true=visible, false=collapsed.</summary>
public class BoolToVisibilityConverter : IValueConverter
{
    public static readonly BoolToVisibilityConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is true;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is true;
}
```

- [ ] **Step 2: Create `Converters/NullToVisibilityConverter.cs`**

```csharp
using System.Globalization;
using Avalonia.Data.Converters;

namespace GitUI.Avalonia.Converters;

/// <summary>Maps null → IsVisible=false, non-null → IsVisible=true.</summary>
public class NullToVisibilityConverter : IValueConverter
{
    public static readonly NullToVisibilityConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is not null;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
```

- [ ] **Step 3: Create `Converters/InverseBoolConverter.cs`**

```csharp
using System.Globalization;
using Avalonia.Data.Converters;

namespace GitUI.Avalonia.Converters;

/// <summary>Inverts a bool: true→false, false→true.</summary>
public class InverseBoolConverter : IValueConverter
{
    public static readonly InverseBoolConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is false;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is false;
}
```

- [ ] **Step 4: Register converters in `AppTheme.axaml`**

Add to `AppTheme.axaml` inside `<ResourceDictionary>`:
```xml
<local:BoolToVisibilityConverter x:Key="BoolToVisibility" />
<local:NullToVisibilityConverter x:Key="NullToVisibility" />
<local:InverseBoolConverter x:Key="InverseBool" />
```

And add the namespace at the top:
```xml
xmlns:local="using:GitUI.Avalonia.Converters"
```

- [ ] **Step 5: Commit**

```bash
git add src/app/GitUI.Avalonia/Converters/ src/app/GitUI.Avalonia/AppTheme.axaml
git commit -m "feat: add shared value converters (bool/null to visibility, inverse bool)"
```

---

## Task 11: Infrastructure — ISettingsBackend + JsonSettingsBackend

**Files:**
- Create: `src/app/GitUI.Avalonia/Infrastructure/ISettingsBackend.cs`
- Create: `src/app/GitUI.Avalonia/Infrastructure/JsonSettingsBackend.cs`
- Create: `tests/app/GitUI.Avalonia.Tests/Infrastructure/JsonSettingsBackendTests.cs`

- [ ] **Step 1: Write the failing test first**

Create `tests/app/GitUI.Avalonia.Tests/Infrastructure/JsonSettingsBackendTests.cs`:

```csharp
using GitUI.Avalonia.Infrastructure;

namespace GitUI.Avalonia.Tests.Infrastructure;

[TestFixture]
public class JsonSettingsBackendTests
{
    private string _tempDir = null!;
    private string _configPath = null!;
    private JsonSettingsBackend _backend = null!;

    [SetUp]
    public void SetUp()
    {
        _tempDir = Path.Join(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDir);
        _configPath = Path.Join(_tempDir, "settings.json");
        _backend = new JsonSettingsBackend(_configPath);
    }

    [TearDown]
    public void TearDown() => Directory.Delete(_tempDir, recursive: true);

    [Test]
    public void GetString_ReturnsDefault_WhenKeyMissing()
    {
        var result = _backend.GetString("missingKey", "default");
        Assert.That(result, Is.EqualTo("default"));
    }

    [Test]
    public void SetString_ThenGetString_ReturnsSavedValue()
    {
        _backend.SetString("myKey", "myValue");
        var result = _backend.GetString("myKey", "default");
        Assert.That(result, Is.EqualTo("myValue"));
    }

    [Test]
    public void GetBool_ReturnsDefault_WhenKeyMissing()
    {
        var result = _backend.GetBool("missing", defaultValue: true);
        Assert.That(result, Is.True);
    }

    [Test]
    public void SetBool_ThenGetBool_ReturnsSavedValue()
    {
        _backend.SetBool("flag", false);
        var result = _backend.GetBool("flag", defaultValue: true);
        Assert.That(result, Is.False);
    }

    [Test]
    public void Settings_PersistedToDisk_AfterSave()
    {
        _backend.SetString("persistKey", "persistValue");
        _backend.Save();

        var reloaded = new JsonSettingsBackend(_configPath);
        var result = reloaded.GetString("persistKey", "missing");
        Assert.That(result, Is.EqualTo("persistValue"));
    }

    [Test]
    public void GetStringList_ReturnsEmpty_WhenKeyMissing()
    {
        var result = _backend.GetStringList("paths");
        Assert.That(result, Is.Empty);
    }

    [Test]
    public void SetStringList_ThenGetStringList_ReturnsSavedList()
    {
        var paths = new[] { "/Users/user/repo1", "/Users/user/repo2" };
        _backend.SetStringList("paths", paths);
        var result = _backend.GetStringList("paths");
        Assert.That(result, Is.EqualTo(paths));
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

```bash
dotnet test tests/app/GitUI.Avalonia.Tests/ --filter "FullyQualifiedName~JsonSettingsBackendTests"
```

Expected: Build error — `JsonSettingsBackend` not defined yet.

- [ ] **Step 3: Create `Infrastructure/ISettingsBackend.cs`**

```csharp
namespace GitUI.Avalonia.Infrastructure;

/// <summary>
/// Platform-agnostic settings read/write interface.
/// On Mac: backed by JSON at ~/.config/gitextensions/settings.json
/// </summary>
public interface ISettingsBackend
{
    string GetString(string key, string defaultValue);
    void SetString(string key, string value);
    bool GetBool(string key, bool defaultValue);
    void SetBool(string key, bool value);
    int GetInt(string key, int defaultValue);
    void SetInt(string key, int value);
    IReadOnlyList<string> GetStringList(string key);
    void SetStringList(string key, IEnumerable<string> values);
    void Save();
}
```

- [ ] **Step 4: Create `Infrastructure/JsonSettingsBackend.cs`**

```csharp
using System.Text.Json;

namespace GitUI.Avalonia.Infrastructure;

/// <summary>
/// Settings backend that reads/writes JSON at a given file path.
/// Default path: ~/.config/gitextensions/settings.json
/// </summary>
public class JsonSettingsBackend : ISettingsBackend
{
    public static string DefaultConfigPath =>
        Path.Join(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".config", "gitextensions", "settings.json");

    private readonly string _path;
    private Dictionary<string, JsonElement> _data;

    public JsonSettingsBackend(string? path = null)
    {
        _path = path ?? DefaultConfigPath;
        _data = Load();
    }

    public string GetString(string key, string defaultValue)
    {
        if (_data.TryGetValue(key, out var element) && element.ValueKind == JsonValueKind.String)
        {
            return element.GetString() ?? defaultValue;
        }
        return defaultValue;
    }

    public void SetString(string key, string value)
        => _data[key] = JsonSerializer.SerializeToElement(value);

    public bool GetBool(string key, bool defaultValue)
    {
        if (_data.TryGetValue(key, out var element))
        {
            if (element.ValueKind == JsonValueKind.True) return true;
            if (element.ValueKind == JsonValueKind.False) return false;
        }
        return defaultValue;
    }

    public void SetBool(string key, bool value)
        => _data[key] = JsonSerializer.SerializeToElement(value);

    public int GetInt(string key, int defaultValue)
    {
        if (_data.TryGetValue(key, out var element) && element.TryGetInt32(out int v))
        {
            return v;
        }
        return defaultValue;
    }

    public void SetInt(string key, int value)
        => _data[key] = JsonSerializer.SerializeToElement(value);

    public IReadOnlyList<string> GetStringList(string key)
    {
        if (_data.TryGetValue(key, out var element) && element.ValueKind == JsonValueKind.Array)
        {
            return element.EnumerateArray()
                          .Where(e => e.ValueKind == JsonValueKind.String)
                          .Select(e => e.GetString()!)
                          .ToList();
        }
        return [];
    }

    public void SetStringList(string key, IEnumerable<string> values)
        => _data[key] = JsonSerializer.SerializeToElement(values.ToArray());

    public void Save()
    {
        string? dir = Path.GetDirectoryName(_path);
        if (dir is not null && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        string json = JsonSerializer.Serialize(_data, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(_path, json);
    }

    private Dictionary<string, JsonElement> Load()
    {
        if (!File.Exists(_path))
        {
            return new Dictionary<string, JsonElement>();
        }

        try
        {
            string json = File.ReadAllText(_path);
            var doc = JsonDocument.Parse(json);
            return doc.RootElement.EnumerateObject()
                      .ToDictionary(p => p.Name, p => p.Value.Clone());
        }
        catch
        {
            return new Dictionary<string, JsonElement>();
        }
    }
}
```

- [ ] **Step 5: Run tests to verify they pass**

```bash
dotnet test tests/app/GitUI.Avalonia.Tests/ --filter "FullyQualifiedName~JsonSettingsBackendTests"
```

Expected: All 7 tests pass.

- [ ] **Step 6: Commit**

```bash
git add src/app/GitUI.Avalonia/Infrastructure/ISettingsBackend.cs \
        src/app/GitUI.Avalonia/Infrastructure/JsonSettingsBackend.cs \
        tests/app/GitUI.Avalonia.Tests/Infrastructure/JsonSettingsBackendTests.cs
git commit -m "feat: add ISettingsBackend + JsonSettingsBackend with tests"
```

---

## Task 12: Infrastructure — ICredentialStore + MacKeychainCredentialStore

**Files:**
- Create: `src/app/GitUI.Avalonia/Infrastructure/ICredentialStore.cs`
- Create: `src/app/GitUI.Avalonia/Infrastructure/MacKeychainCredentialStore.cs`
- Create: `tests/app/GitUI.Avalonia.Tests/Infrastructure/MacKeychainCredentialStoreTests.cs`

- [ ] **Step 1: Create `Infrastructure/ICredentialStore.cs`**

```csharp
namespace GitUI.Avalonia.Infrastructure;

/// <summary>
/// Platform-agnostic credential storage interface.
/// On Mac: backed by macOS Keychain via Security.framework.
/// </summary>
public interface ICredentialStore
{
    bool TryGetCredential(string target, out string username, out string password);
    void SaveCredential(string target, string username, string password);
    void DeleteCredential(string target);
}
```

- [ ] **Step 2: Write the failing test**

Create `tests/app/GitUI.Avalonia.Tests/Infrastructure/MacKeychainCredentialStoreTests.cs`:

```csharp
using GitUI.Avalonia.Infrastructure;

namespace GitUI.Avalonia.Tests.Infrastructure;

[TestFixture]
[Platform("MacOsX")]
public class MacKeychainCredentialStoreTests
{
    private MacKeychainCredentialStore _store = null!;
    private const string TestTarget = "test.gitextensions.localhost";
    private const string TestUser = "testuser";
    private const string TestPassword = "testpassword123";

    [SetUp]
    public void SetUp() => _store = new MacKeychainCredentialStore();

    [TearDown]
    public void TearDown()
    {
        try { _store.DeleteCredential(TestTarget); } catch { /* ignore */ }
    }

    [Test]
    public void TryGetCredential_ReturnsFalse_WhenNotStored()
    {
        var result = _store.TryGetCredential(TestTarget, out _, out _);
        Assert.That(result, Is.False);
    }

    [Test]
    public void SaveAndRetrieve_RoundTrip()
    {
        _store.SaveCredential(TestTarget, TestUser, TestPassword);
        var found = _store.TryGetCredential(TestTarget, out var user, out var pass);

        Assert.That(found, Is.True);
        Assert.That(user, Is.EqualTo(TestUser));
        Assert.That(pass, Is.EqualTo(TestPassword));
    }

    [Test]
    public void Delete_RemovesCredential()
    {
        _store.SaveCredential(TestTarget, TestUser, TestPassword);
        _store.DeleteCredential(TestTarget);
        var found = _store.TryGetCredential(TestTarget, out _, out _);
        Assert.That(found, Is.False);
    }
}
```

- [ ] **Step 3: Run tests to verify they fail**

```bash
dotnet test tests/app/GitUI.Avalonia.Tests/ --filter "FullyQualifiedName~MacKeychainCredentialStoreTests"
```

Expected: Build error — `MacKeychainCredentialStore` not defined.

- [ ] **Step 4: Create `Infrastructure/MacKeychainCredentialStore.cs`**

```csharp
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;

namespace GitUI.Avalonia.Infrastructure;

/// <summary>
/// macOS Keychain implementation of ICredentialStore.
/// Uses Security.framework via P/Invoke.
/// </summary>
[SupportedOSPlatform("macos")]
public class MacKeychainCredentialStore : ICredentialStore
{
    private const string ServiceName = "GitExtensions";

    public bool TryGetCredential(string target, out string username, out string password)
    {
        username = string.Empty;
        password = string.Empty;

        uint passwordLength = 0;
        IntPtr passwordData = IntPtr.Zero;
        uint accountLength = 0;
        IntPtr accountData = IntPtr.Zero;
        IntPtr itemRef = IntPtr.Zero;

        int result = NativeMethods.SecKeychainFindGenericPassword(
            IntPtr.Zero,
            (uint)Encoding.UTF8.GetByteCount(ServiceName), ServiceName,
            (uint)Encoding.UTF8.GetByteCount(target), target,
            out accountLength, out accountData,
            out passwordLength, out passwordData,
            out itemRef);

        if (result != 0) // errSecSuccess = 0
        {
            return false;
        }

        try
        {
            if (accountData != IntPtr.Zero && accountLength > 0)
            {
                username = Marshal.PtrToStringUTF8(accountData, (int)accountLength) ?? string.Empty;
            }
            if (passwordData != IntPtr.Zero && passwordLength > 0)
            {
                password = Marshal.PtrToStringUTF8(passwordData, (int)passwordLength) ?? string.Empty;
            }
            return true;
        }
        finally
        {
            if (accountData != IntPtr.Zero) NativeMethods.SecKeychainItemFreeContent(IntPtr.Zero, accountData);
            if (passwordData != IntPtr.Zero) NativeMethods.SecKeychainItemFreeContent(IntPtr.Zero, passwordData);
        }
    }

    public void SaveCredential(string target, string username, string password)
    {
        // Try to find existing item first
        uint existingPasswordLength = 0;
        IntPtr existingPasswordData = IntPtr.Zero;
        uint existingAccountLength = 0;
        IntPtr existingAccountData = IntPtr.Zero;
        IntPtr itemRef = IntPtr.Zero;

        int findResult = NativeMethods.SecKeychainFindGenericPassword(
            IntPtr.Zero,
            (uint)Encoding.UTF8.GetByteCount(ServiceName), ServiceName,
            (uint)Encoding.UTF8.GetByteCount(target), target,
            out existingAccountLength, out existingAccountData,
            out existingPasswordLength, out existingPasswordData,
            out itemRef);

        if (existingAccountData != IntPtr.Zero) NativeMethods.SecKeychainItemFreeContent(IntPtr.Zero, existingAccountData);
        if (existingPasswordData != IntPtr.Zero) NativeMethods.SecKeychainItemFreeContent(IntPtr.Zero, existingPasswordData);

        byte[] passwordBytes = Encoding.UTF8.GetBytes(password);

        if (findResult == 0 && itemRef != IntPtr.Zero)
        {
            // Update existing
            NativeMethods.SecKeychainItemModifyContent(itemRef, IntPtr.Zero, (uint)passwordBytes.Length, passwordBytes);
        }
        else
        {
            // Add new
            NativeMethods.SecKeychainAddGenericPassword(
                IntPtr.Zero,
                (uint)Encoding.UTF8.GetByteCount(ServiceName), ServiceName,
                (uint)Encoding.UTF8.GetByteCount(target), target,
                (uint)passwordBytes.Length, passwordBytes,
                out _);
        }
    }

    public void DeleteCredential(string target)
    {
        uint passwordLength = 0;
        IntPtr passwordData = IntPtr.Zero;
        uint accountLength = 0;
        IntPtr accountData = IntPtr.Zero;
        IntPtr itemRef = IntPtr.Zero;

        int result = NativeMethods.SecKeychainFindGenericPassword(
            IntPtr.Zero,
            (uint)Encoding.UTF8.GetByteCount(ServiceName), ServiceName,
            (uint)Encoding.UTF8.GetByteCount(target), target,
            out accountLength, out accountData,
            out passwordLength, out passwordData,
            out itemRef);

        if (accountData != IntPtr.Zero) NativeMethods.SecKeychainItemFreeContent(IntPtr.Zero, accountData);
        if (passwordData != IntPtr.Zero) NativeMethods.SecKeychainItemFreeContent(IntPtr.Zero, passwordData);

        if (result == 0 && itemRef != IntPtr.Zero)
        {
            NativeMethods.SecKeychainItemDelete(itemRef);
        }
    }

    private static class NativeMethods
    {
        private const string SecurityFramework = "/System/Library/Frameworks/Security.framework/Security";

        [DllImport(SecurityFramework)]
        public static extern int SecKeychainFindGenericPassword(
            IntPtr keychainOrArray,
            uint serviceNameLength, string serviceName,
            uint accountNameLength, string accountName,
            out uint passwordLength, out IntPtr passwordData,
            out uint accountLength, out IntPtr accountData,
            out IntPtr itemRef);

        [DllImport(SecurityFramework)]
        public static extern int SecKeychainAddGenericPassword(
            IntPtr keychain,
            uint serviceNameLength, string serviceName,
            uint accountNameLength, string accountName,
            uint passwordLength, byte[] passwordData,
            out IntPtr itemRef);

        [DllImport(SecurityFramework)]
        public static extern int SecKeychainItemModifyContent(
            IntPtr itemRef,
            IntPtr attrList,
            uint length,
            byte[] data);

        [DllImport(SecurityFramework)]
        public static extern int SecKeychainItemDelete(IntPtr itemRef);

        [DllImport(SecurityFramework)]
        public static extern int SecKeychainItemFreeContent(IntPtr attrList, IntPtr data);
    }
}
```

- [ ] **Step 5: Run tests on Mac**

```bash
dotnet test tests/app/GitUI.Avalonia.Tests/ --filter "FullyQualifiedName~MacKeychainCredentialStoreTests"
```

Expected: All 3 tests pass. (These tests write to Keychain — macOS may prompt for permission on first run.)

- [ ] **Step 6: Commit**

```bash
git add src/app/GitUI.Avalonia/Infrastructure/ICredentialStore.cs \
        src/app/GitUI.Avalonia/Infrastructure/MacKeychainCredentialStore.cs \
        tests/app/GitUI.Avalonia.Tests/Infrastructure/MacKeychainCredentialStoreTests.cs
git commit -m "feat: add ICredentialStore + MacKeychainCredentialStore with tests"
```

---

## Task 13: Wire infrastructure into App + final build check

**Files:**
- Modify: `src/app/GitUI.Avalonia/App.axaml.cs`

- [ ] **Step 1: Update `App.axaml.cs` to initialize infrastructure**

```csharp
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using GitUI.Avalonia.Infrastructure;

namespace GitUI.Avalonia;

public class App : Application
{
    public static ISettingsBackend Settings { get; private set; } = null!;
    public static ICredentialStore Credentials { get; private set; } = null!;

    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        // Initialize infrastructure
        Settings = new JsonSettingsBackend();
        Credentials = new MacKeychainCredentialStore();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Temporary blank window — replaced by MainWindow in Plan B
            desktop.MainWindow = new Avalonia.Controls.Window
            {
                Title = "Git Extensions",
                Width = 1024,
                Height = 768
            };

            desktop.Exit += (_, _) => Settings.Save();
        }

        base.OnFrameworkInitializationCompleted();
    }
}
```

- [ ] **Step 2: Full build check**

```bash
dotnet build GitExtensions.Mac.slnx
```

Expected: 0 errors, 0 warnings related to platform APIs.

- [ ] **Step 3: Run all tests**

```bash
dotnet test GitExtensions.Mac.slnx
```

Expected: All existing `GitCommands` unit tests pass. New infrastructure tests pass.

- [ ] **Step 4: Run the app**

```bash
dotnet run --project src/app/GitUI.Avalonia/GitUI.Avalonia.csproj
```

Expected: Window titled "Git Extensions" opens. No errors in console.

- [ ] **Step 5: Final commit for Plan A**

```bash
git add src/app/GitUI.Avalonia/App.axaml.cs
git commit -m "feat: wire ISettingsBackend + ICredentialStore into App, Plan A complete"
```

---

## Plan A Done Criteria

Before moving to Plan B, verify all of these:

- [ ] `dotnet build GitExtensions.Mac.slnx` — 0 errors
- [ ] `dotnet test GitExtensions.Mac.slnx` — all tests pass
- [ ] `dotnet run --project src/app/GitUI.Avalonia/` — blank window opens on Mac
- [ ] `grep -r "System.Windows.Forms" src/app/GitUI.Avalonia/` — empty (zero results)
- [ ] `grep -r "System.Windows.Forms" src/app/GitCommands/` — empty
- [ ] `docs/mac-port/TASK-STATUS.md` — Tasks 0 and 1 marked `complete`

Plan B (`2026-04-19-mac-port-plan-b-critical-path.md`) covers the main window, commit graph, and commit details panel.
