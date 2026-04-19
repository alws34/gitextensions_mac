# Agent Guide — GitExtensions Mac Port

This document is the starting point for any agent working on this port. Read it before touching any code.

## What you are doing

Porting GitExtensions from Windows Forms (`net10.0-windows`) to Avalonia UI (`net10.0`) so it runs natively on macOS. The overall design is documented in `docs/superpowers/specs/2026-04-19-mac-port-design.md`.

## Repository layout

```
src/
  app/
    GitCommands/          ← KEEP (mostly unchanged, pure git logic)
    GitExtensions/        ← KEEP (app bootstrapper, audit for Windows refs)
    GitExtensions.Extensibility/ ← KEEP (interfaces, no platform code)
    GitExtUtils/          ← KEEP (utilities)
    ResourceManager/      ← KEEP (translations)
    GitUI/                ← SOURCE to read from (WinForms, do not modify)
    GitUI.Avalonia/       ← TARGET you are building (Avalonia, create/modify here)
  plugins/
    GitUIPluginInterfaces/ ← KEEP
    <each plugin>/        ← Port UI after core is done
docs/
  mac-port/               ← Agent documentation (you are here)
  superpowers/specs/      ← Design spec
```

## Your task has a plan file

Each task has a plan file in `docs/mac-port/tasks/TASK-<N>-<name>.md`. Read it before starting. It tells you:
- Exactly which WinForms files to read
- Exactly which Avalonia files to create
- Which base classes to use
- Done criteria

## How to port a WinForms Form to Avalonia

1. **Read the WinForms source.** Open the `.cs` file and the `.Designer.cs` file side by side. The Designer file shows the layout; the `.cs` file shows the event handlers and logic.

2. **Identify the data.** What data does this form display? Where does it come from? (Usually a `GitModule`, a git command result, or a settings object.)

3. **Create the Avalonia view.** Create a `.axaml` file in `src/app/GitUI.Avalonia/`. Use the Avalonia equivalent controls (see `WINFORMS-TO-AVALONIA.md`).

4. **Create the code-behind.** Create the `.axaml.cs` file. Use the appropriate base class:
   - Top-level windows → inherit `GitExtensionsWindow`
   - Dialogs (shown with ShowDialog) → inherit `GitExtensionsDialog`
   - Embedded controls → inherit `GitModuleControl`

5. **Wire up data.** Bind control properties to data using Avalonia bindings (`{Binding PropertyName}`). Use ReactiveUI for complex state if needed.

6. **Port event handlers.** WinForms event handlers (`button_Click`, `textBox_TextChanged`) become either:
   - Avalonia event handlers (simple cases)
   - ReactiveUI commands (for async operations or MVVM pattern)

7. **Verify done criteria.** See your task plan file.

## Naming conventions

| WinForms | Avalonia |
|----------|---------|
| `FormFoo.cs` | `FooDialog.axaml` + `FooDialog.axaml.cs` |
| `FooUserControl.cs` | `FooControl.axaml` + `FooControl.axaml.cs` |
| `FormBrowse.cs` (main window) | `MainWindow.axaml` + `MainWindow.axaml.cs` |

Files go in the same logical subdirectory structure as the WinForms original:
- `CommandsDialogs/FormFoo.cs` → `Dialogs/FooDialog.axaml`
- `UserControls/FooControl.cs` → `Controls/FooControl.axaml`

## Base classes (from `GitUI.Avalonia/Base/`)

```csharp
// For top-level windows
public class MyWindow : GitExtensionsWindow { }

// For dialogs (modal)
public class MyDialog : GitExtensionsDialog { }

// For embedded controls
public class MyControl : GitModuleControl { }
```

These are defined in Task 1. Do not start your task before Task 1 is complete.

## Threading

WinForms used `Control.Invoke()` to marshal back to the UI thread. In Avalonia:

```csharp
// WinForms
this.Invoke(() => label.Text = "done");

// Avalonia
await Dispatcher.UIThread.InvokeAsync(() => label.Content = "done");
```

The existing `GitCommands` code uses `Microsoft.VisualStudio.Threading` (`JoinableTaskFactory`). This is cross-platform and works unchanged.

## Async pattern

WinForms code often uses `BackgroundWorker`. Replace with `async/await`:

```csharp
// WinForms
backgroundWorker.RunWorkerAsync();
void backgroundWorker_DoWork(object sender, DoWorkEventArgs e) { ... }
void backgroundWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e) { ... }

// Avalonia
await Task.Run(() => DoWork());
UpdateUI(); // already on UI thread after await
```

## MessageBox

WinForms `MessageBox.Show()` does not exist in Avalonia. Use:

```csharp
// Simple info
await MessageBoxManager.GetMessageBoxStandard("Title", "Message").ShowAsync();

// Yes/No
var result = await MessageBoxManager.GetMessageBoxStandard(
    "Confirm", "Are you sure?",
    ButtonEnum.YesNo).ShowAsync();
if (result == ButtonResult.Yes) { ... }
```

Or use the custom `MessageDialog` from `GitUI.Avalonia/Controls/` if it has been created.

## No WinForms references

When you are done with a file, verify it has zero references to:
- `System.Windows.Forms`
- `System.Drawing` (except `System.Drawing.Color` which has an Avalonia equivalent)
- `Microsoft.WindowsAPICodePack`
- `AdysTech.CredentialManager`
- `ConEmu`

Run this to check your output files:
```bash
grep -r "System.Windows.Forms\|System.Drawing\|WindowsAPICodePack\|AdysTech\|ConEmu" src/app/GitUI.Avalonia/
```

## Running a smoke test

After completing your task:

```bash
# Build
dotnet build GitExtensions.Mac.slnx

# Run
dotnet run --project src/app/GitUI.Avalonia/GitUI.Avalonia.csproj
```

Then manually verify the done criteria from your task plan file.

## Update TASK-STATUS.md

When your task is complete, update `docs/mac-port/TASK-STATUS.md`:
- Change your task's status from `in-progress` to `complete`
- Add any notes about decisions you made or issues you encountered
