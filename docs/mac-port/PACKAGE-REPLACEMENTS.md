# Package Replacements — Mac Port

Reference for Task 0 (Fork & Build System) and any agent updating `Directory.Packages.props`.

---

## Remove from Mac solution

These packages are Windows-only and must be removed from `Directory.Packages.props` and all `.csproj` files in the Mac fork:

| Package | Was used for | Action |
|---------|-------------|--------|
| `Microsoft-WindowsAPICodePack-Core` | Windows shell integration (file dialogs, taskbar) | Remove. Avalonia provides cross-platform equivalents. |
| `Microsoft-WindowsAPICodePack-Shell` | Windows shell integration | Remove. |
| `ConEmu.Core` | Embedded terminal emulator | Remove. Replaced by launching Terminal.app externally. |
| `AdysTech.CredentialManager` | Windows Credential Manager | Remove. Replaced by `MacKeychainCredentialStore`. |
| `AppInsights.WindowsDesktop` | Windows desktop telemetry | Remove or replace with `Microsoft.ApplicationInsights` base. |
| `WiX` | Windows installer (.msi/.exe) | Remove. Mac uses .app bundle + .dmg. |
| `vswhere` | Locating Visual Studio installations | Remove. Not relevant on Mac. |
| `EnvDTE` | Visual Studio automation API | Remove. Not relevant on Mac. |

---

## Add to Mac solution

Add these to `Directory.Packages.props` and to `GitUI.Avalonia/GitUI.Avalonia.csproj`:

| Package | Version (check NuGet for latest stable) | Purpose |
|---------|----------------------------------------|---------|
| `Avalonia` | 11.x | Core UI framework |
| `Avalonia.Desktop` | 11.x | Desktop app host |
| `Avalonia.Themes.Fluent` | 11.x | Fluent design theme |
| `Avalonia.ReactiveUI` | 11.x | MVVM / reactive bindings |
| `AvaloniaEdit` | 11.x | Code/diff editor |
| `MsBox.Avalonia` | latest | MessageBox replacement |

For plugins that show charts (Statistics plugin):
| Package | Purpose |
|---------|---------|
| `LiveChartsCore.SkiaSharp.Avalonia` | Charts (replaces WinForms charting) |

---

## Keep unchanged (already cross-platform)

These packages work on macOS without any changes:

| Package |
|---------|
| `LibGit2Sharp` |
| `RestSharp` |
| `SmartFormat` |
| `StrongOf` |
| `System.Reactive` |
| `System.Reactive.Linq` |
| `System.Reactive.Interfaces` |
| `System.IO.Abstractions` |
| `System.IO.Abstractions.TestingHelpers` |
| `Microsoft.VisualStudio.Threading` |
| `Microsoft.VisualStudio.Composition` |
| `System.ComponentModel.Composition` |
| `JetBrains.Annotations` |
| `Ben.Demystifier` |
| `ExCSS` |
| `GitInfo` |
| `NSubstitute` |
| `NUnit` |
| `NUnit3TestAdapter` |
| `NUnit.Analyzers` |
| `NUnit.ConsoleRunner` |
| `Microsoft.NET.Test.Sdk` |
| `AwesomeAssertions` |
| `Verify.NUnit` |
| `YamlDotNet` |
| `StyleCop.Analyzers` |
| `Microsoft.CodeAnalysis.Analyzers` |
| `Microsoft.CodeAnalysis.BannedApiAnalyzers` |
| `Microsoft.CodeAnalysis.CSharp` |
| `Microsoft.SourceLink.GitHub` |

---

## Audit needed

These packages need investigation before deciding:

| Package | Concern |
|---------|---------|
| `Microsoft.VisualStudio.Composition` | Used for MEF plugin loading. Should be cross-platform but verify. |
| `Microsoft.VisualStudio.Threading` | Used for `JoinableTaskFactory`. Cross-platform but verify on Mac. |
| `ConEmu.Core` | Already marked for removal — but check what `ConsoleEmulatorOutputControl` uses from it so you know what to replace in the UI. |
