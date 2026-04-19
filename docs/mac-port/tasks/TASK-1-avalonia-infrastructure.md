# Task 1 — Avalonia Infrastructure

**Status:** pending  
**Depends on:** Task 0  
**Blocks:** ALL other tasks (Tasks 2–17)  
**Estimated complexity:** Medium — foundational work, no git logic

---

## Goal

Build the shared Avalonia foundation that every screen-port agent builds on. When this task is done, every other agent has a clear contract: base classes, theming, converters, and platform services.

---

## Files to create

All files go in `src/app/GitUI.Avalonia/`.

```
src/app/GitUI.Avalonia/
├── GitUI.Avalonia.csproj        (update the stub from Task 0)
├── App.axaml
├── App.axaml.cs
├── Program.cs
├── AppTheme.axaml
├── Base/
│   ├── GitExtensionsWindow.axaml
│   ├── GitExtensionsWindow.axaml.cs
│   ├── GitExtensionsDialog.axaml
│   ├── GitExtensionsDialog.axaml.cs
│   ├── GitModuleControl.axaml
│   └── GitModuleControl.axaml.cs
├── Converters/
│   ├── BoolToVisibilityConverter.cs
│   ├── NullToVisibilityConverter.cs
│   ├── ColorConverter.cs
│   └── InverseBoolConverter.cs
├── Controls/
│   ├── LoadingSpinner.axaml
│   ├── LoadingSpinner.axaml.cs
│   └── SplitContainer.axaml
├── Infrastructure/
│   ├── ICredentialStore.cs
│   ├── MacKeychainCredentialStore.cs
│   ├── ISettingsBackend.cs
│   ├── JsonSettingsBackend.cs
│   └── MacDiffMergeTools.cs
└── Dialogs/
    └── MessageDialog.axaml        (reusable message box)
    └── MessageDialog.axaml.cs
```

---

## Implementation details

### Program.cs

```csharp
using Avalonia;

AppBuilder.Configure<App>()
    .UsePlatformDetect()
    .WithInterFont()
    .LogToTrace()
    .StartWithClassicDesktopLifetime(args);
```

### App.axaml

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

### App.axaml.cs

Bootstrap the app: initialize services, register `ICredentialStore`, `ISettingsBackend`, configure MEF plugin loading (port the existing plugin loading from `GitExtensions/` project).

### AppTheme.axaml

Define GitExtensions brand colors as Avalonia resources:
```xml
<ResourceDictionary xmlns="https://github.com/avaloniaui"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
    <!-- Graph lane colors -->
    <Color x:Key="GraphLane1">#FF0000CD</Color>
    <Color x:Key="GraphLane2">#FFFF8C00</Color>
    <!-- ... -->

    <!-- Diff colors -->
    <SolidColorBrush x:Key="DiffAddedBackground">#FF002200</SolidColorBrush>
    <SolidColorBrush x:Key="DiffRemovedBackground">#FF220000</SolidColorBrush>
    <SolidColorBrush x:Key="DiffSectionBackground">#FF282800</SolidColorBrush>

    <!-- Monospace font for diff/editor -->
    <FontFamily x:Key="MonospaceFont">Menlo, Monaco, Courier New, monospace</FontFamily>
</ResourceDictionary>
```

### GitExtensionsWindow (base class for all top-level windows)

Must provide:
- `IGitUICommandsSource? UICommandsSource` property
- `IServiceProvider ServiceProvider` property (injected at construction)
- Standard title bar behavior
- Remember window size/position (via `JsonSettingsBackend`)

```csharp
public class GitExtensionsWindow : Window
{
    public IServiceProvider ServiceProvider { get; }
    public IGitUICommandsSource? UICommandsSource { get; set; }

    protected GitExtensionsWindow(IServiceProvider serviceProvider)
    {
        ServiceProvider = serviceProvider;
    }
}
```

### GitExtensionsDialog (base class for modal dialogs)

Must provide:
- `Close(TResult result)` method
- Standard OK/Cancel keyboard handling (Enter = OK, Escape = Cancel)
- Returns typed result via `ShowDialog<TResult>(parentWindow)`

### GitModuleControl (base class for embedded controls)

Must provide:
- `GitModule Module` property
- `IGitUICommandsSource UICommandsSource` property
- `void OnUICommandsSourceSet()` virtual method (override to react to module being set)

### ICredentialStore

```csharp
public interface ICredentialStore
{
    bool TryGetCredential(string target, out string username, out string password);
    void SaveCredential(string target, string username, string password);
    void DeleteCredential(string target);
}
```

### MacKeychainCredentialStore

Use macOS `Security.framework` via P/Invoke:
- `SecKeychainFindInternetPassword` for read
- `SecKeychainAddInternetPassword` for write
- `SecKeychainItemDelete` for delete

Register via DI in `App.axaml.cs`:
```csharp
services.AddSingleton<ICredentialStore, MacKeychainCredentialStore>();
```

### ISettingsBackend + JsonSettingsBackend

`ISettingsBackend` abstracts read/write of key-value settings.
`JsonSettingsBackend` reads/writes `~/.config/gitextensions/settings.json`.

See `docs/mac-port/SETTINGS-MIGRATION.md` for config file location and structure.

### MacDiffMergeTools

Register Mac-native diff/merge tools. Called during app startup to populate the available tools list.

Tools to register: `opendiff` (FileMerge), `ksdiff` (Kaleidoscope), `code --diff` (VS Code), `bbdiff` (BBEdit), `smerge` (Sublime Merge).

---

## Done Criteria

- [ ] `dotnet run --project src/app/GitUI.Avalonia/` launches an empty Avalonia window on Mac
- [ ] `GitExtensionsWindow`, `GitExtensionsDialog`, `GitModuleControl` base classes are usable
- [ ] `AppTheme.axaml` loads without errors (dark/light mode works)
- [ ] `ICredentialStore` interface exists and `MacKeychainCredentialStore` compiles
- [ ] `ISettingsBackend` + `JsonSettingsBackend` compiles and reads/writes JSON
- [ ] No WinForms references in any file in `GitUI.Avalonia/`
- [ ] `docs/mac-port/TASK-STATUS.md` updated to `complete` for Task 1

---

## Notes for next agents

After Task 1, agents for Tasks 2–16 can all start in parallel. Each agent should:
1. Read their task plan file in `docs/mac-port/tasks/`
2. Read the WinForms source files listed in the plan
3. Create the Avalonia equivalents using the base classes from this task
4. Follow `AGENT-GUIDE.md` and `WINFORMS-TO-AVALONIA.md`
