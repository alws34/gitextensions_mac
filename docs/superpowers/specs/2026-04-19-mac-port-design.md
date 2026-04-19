# GitExtensions Mac Port — Design Spec

**Date:** 2026-04-19  
**Author:** Alon Weiss  
**Status:** Approved — ready for implementation planning  
**Approach:** Screen-by-screen parallel port (Approach B)

---

## 1. Goal

Port GitExtensions to macOS as a fully native-feeling, standalone `.app` with feature parity to the Windows version. Target audience: Mac developers who currently have no free, high-quality Git GUI.

**Priority order:**
1. Runs on Mac without Wine or emulation (functional parity)
2. Cross-platform consistent UI feel (not required to look like a native Mac app, but should feel natural)

**Non-goals:**
- Windows Explorer shell extension on Mac
- ConEmu embedded terminal (replaced with external terminal launch)
- Visual Studio integration (VS is Windows-only)

---

## 2. Repository Strategy

**Create a fork:** `gitextensions-mac` (separate GitHub repo, separate from upstream).  
Reason: move fast without risking Windows regression; no need to maintain dual-platform build during port.

**Fork setup checklist (Task 0):**
- Fork `gitextensions/gitextensions` on GitHub
- Change `SolutionTargetFramework` in `eng/RepoLayout.props` from `net10.0-windows` → `net10.0`
- Remove `<UseWindowsForms>true</UseWindowsForms>` from `Directory.Build.props`
- Create `GitExtensions.Mac.slnx` excluding: `BugReporter`, `GitExtensionsShellEx`, all WiX setup projects
- Delete `src/native/GitExtensionsShellEx/` (Windows Explorer shell extension — no Mac equivalent)
- Keep `src/native/GitExtSshAskPass/` — audit for portability
- Add Avalonia packages (see Section 4)
- Goal: `dotnet build GitExtensions.Mac.slnx` succeeds on macOS (even with stub implementations)

---

## 3. Layer Separation

The codebase has three tiers of work:

### Tier 1 — Keep As-Is (minimal changes)

These projects have no Windows-specific dependencies and port cleanly:

| Project | Notes |
|---------|-------|
| `GitCommands` | Pure git logic. Only 4 files need surgery (see Tier 2). |
| `GitExtensions.Extensibility` | Interfaces only. No platform code. |
| `GitExtUtils` | Utility helpers. Likely clean — audit during build. |
| `ResourceManager` | Strings/translations. Fully portable. |
| `GitUIPluginInterfaces` | Plugin contracts. Portable. |

### Tier 2 — Targeted Surgery (non-UI, has Windows-specific calls)

| File | Problem | Fix |
|------|---------|-----|
| `GitCommands/Settings/AppSettings.cs` | Windows Registry for settings storage | Replace with `Microsoft.Extensions.Configuration` reading JSON/INI from `~/.config/gitextensions/` |
| `GitCommands/Git/Extensions/ProcessExtensions.cs` | Windows-specific process APIs | Replace with cross-platform `System.Diagnostics.Process` equivalents |
| `GitCommands/DiffMergeTools/VsDiffMerge.cs` | Visual Studio diff tool | Stub out — add Mac diff tools in `DiffMergeTools` instead (see Section 7) |
| `GitCommands/ServiceContainerRegistry.cs` | Windows service container | Audit — likely just needs Windows-specific registrations removed |

### Tier 3 — Full Rewrite (UI layer)

All of `src/app/GitUI/` is replaced with Avalonia equivalents.

- **Delete entirely:** `src/app/GitUI/Interops/` — the entire `Gdi32/`, `UxTheme/`, `User32/`, `WinInet/`, `Kernel32/` P/Invoke wrappers. Avalonia handles all rendering; none of these Win32 calls are needed.
- **Delete:** `src/app/GitUI/VisualStudioIntegration.cs`
- **Delete:** `src/app/GitUI/MouseWheelRedirector.cs` (Win32 message hook — Avalonia handles mouse wheel natively)
- **Replace:** Every `Form*.cs`, `UserControl*.cs` → Avalonia `.axaml` + code-behind

**Stats:**
- 97 Form files to port
- 590 total C# files in `GitUI/`
- 52 files with Windows P/Invoke (all in `Interops/` or deleted outright)

**Plugins (after core is done):**
`AutoCompileSubmodules`, `BackgroundFetch`, `BuildServerIntegration`, `CreateLocalBranches`, `DeleteUnusedBranches`, `FindLargeFiles`, `GitHub3`, `Gource`, `ProxySwitcher`, `ReleaseNotesGenerator`, `Statistics` — all ported after the main app works.

---

## 4. Package Replacements

### Remove (Windows-only)

| Package | Reason | Action |
|---------|--------|--------|
| `Microsoft-WindowsAPICodePack-Core` | Windows shell integration | Remove — no Mac equivalent needed |
| `Microsoft-WindowsAPICodePack-Shell` | Windows shell integration | Remove — no Mac equivalent needed |
| `ConEmu.Core` | Windows-only terminal emulator | Remove — replaced with external terminal launch |
| `AdysTech.CredentialManager` | Windows Credential Manager | Replace with macOS Keychain (see Section 7) |
| `AppInsights.WindowsDesktop` | Windows desktop telemetry | Replace with `Microsoft.ApplicationInsights` base package or remove |
| `WiX` | Windows installer | Remove from Mac solution |
| `vswhere` | Visual Studio detection | Remove from Mac solution |
| `EnvDTE` | VS automation | Remove from Mac solution |

### Add (Avalonia stack)

| Package | Purpose |
|---------|---------|
| `Avalonia` | Core UI framework |
| `Avalonia.Desktop` | Desktop app host (window chrome, OS integration) |
| `Avalonia.Themes.Fluent` | Base theme (dark/light mode support) |
| `Avalonia.ReactiveUI` | MVVM binding — pairs with `System.Reactive` already in codebase |
| `AvaloniaEdit` | Syntax-highlighted code/diff editor (replaces WinForms `RichTextBox`-based editor) |

### Keep Unchanged (already cross-platform)

`LibGit2Sharp`, `RestSharp`, `SmartFormat`, `StrongOf`, `System.Reactive`, `System.Reactive.Linq`, `System.IO.Abstractions`, `Microsoft.VisualStudio.Threading`, `Microsoft.VisualStudio.Composition`, `JetBrains.Annotations`, `NSubstitute`, `NUnit`, `NUnit3TestAdapter`, `YamlDotNet`, `AwesomeAssertions`

---

## 5. Avalonia Infrastructure (Task 1 — blocks everything else)

**New project:** `src/app/GitUI.Avalonia/`

This project must be completed before any screen-port agent can start work. It defines all shared base classes and contracts.

### Required files

```
src/app/GitUI.Avalonia/
├── GitUI.Avalonia.csproj
├── App.axaml                        # Avalonia app entry point
├── App.axaml.cs
├── Program.cs                       # Desktop host bootstrapper
├── AppTheme.axaml                   # Fluent theme + GitExtensions palette
├── Base/
│   ├── GitExtensionsWindow.axaml    # Base Window (replaces GitExtensionsForm)
│   ├── GitExtensionsWindow.axaml.cs
│   ├── GitExtensionsDialog.axaml    # Base Dialog (replaces GitExtensionsDialog)
│   ├── GitExtensionsDialog.axaml.cs
│   └── GitModuleControl.axaml      # Base UserControl (replaces GitModuleControl)
│   └── GitModuleControl.axaml.cs
├── Converters/
│   ├── BoolToVisibilityConverter.cs
│   ├── ColorConverter.cs
│   └── NullToVisibilityConverter.cs
├── Controls/
│   ├── LoadingSpinner.axaml         # Async loading indicator
│   ├── AvatarControl.axaml          # Commit author avatar
│   └── SplitContainer.axaml        # Replaces WinForms SplitContainer
└── Infrastructure/
    ├── ICredentialStore.cs          # Platform-agnostic credential interface
    ├── MacKeychainCredentialStore.cs # macOS Keychain implementation
    └── AppSettingsMac.cs            # Cross-platform settings (JSON)
```

### Base class contracts

**`GitExtensionsWindow`**: replaces `GitExtensionsForm`. Must expose:
- `IGitUICommandsSource CommandsSource` property
- `IServiceProvider ServiceProvider` property
- Standard window lifecycle hooks

**`GitExtensionsDialog`**: replaces `GitExtensionsDialog`. Must expose:
- `DialogResult` equivalent (use `bool?` return from `ShowDialog`)
- `OkButton` / `CancelButton` wiring

**`GitModuleControl`**: replaces `GitModuleControl`. Must expose:
- `GitModule Module` property
- `IGitUICommandsSource UICommandsSource` property

### Theme

Use `Avalonia.Themes.Fluent`. Define GitExtensions-specific resource dictionary with:
- Brand colors (blues, graph lane colors)
- Diff colors (added = green, removed = red, context = default)
- Monospace font for diff/editor views

---

## 6. Screen Port Strategy

### How each screen port works

Each agent task for a screen receives:
1. **Source files to read** — the WinForms `.cs` and `.Designer.cs` files
2. **Target files to create** — new `.axaml` + `.axaml.cs` in `GitUI.Avalonia/`
3. **Base class to inherit** — from the infrastructure layer
4. **Done criteria** — screen renders, binds to real data, zero WinForms references

### Batches (parallel after Task 1 completes)

#### Task 2 — Core Shell (blocks Tasks 3–4)
The main window. Everything else lives inside it.

| WinForms source | Avalonia target |
|----------------|-----------------|
| `CommandsDialogs/FormBrowse.cs` + 4 partial files | `MainWindow.axaml` |
| `CommandsDialogs/BrowseDialog/DashboardControl/` | `Dashboard/DashboardView.axaml` |
| `CommandsDialogs/BrowseDialog/FormOpenDirectory.cs` | `Dashboard/OpenRepositoryDialog.axaml` |
| `CommandsDialogs/BrowseDialog/FormRecentReposSettings.cs` | `Dashboard/RecentReposSettings.axaml` |
| `UserControls/FilterToolBar.cs` | `Controls/FilterToolBar.axaml` |

Done criteria: App launches, dashboard shows recent repos, user can open a repository.

#### Task 3 — Commit Graph (depends on Task 2)
The most technically complex task. The graph is a custom-drawn control.

| WinForms source | Avalonia target |
|----------------|-----------------|
| `UserControls/RevisionGrid/RevisionGridControl.cs` + `.Command.cs` | `RevisionGrid/RevisionGridControl.axaml` |
| `UserControls/RevisionGrid/RevisionDataGridView.cs` + `.BackgroundUpdater.cs` | `RevisionGrid/RevisionDataGridView.axaml` |
| `UserControls/RevisionGrid/Graph/Rendering/GraphRenderer.cs` | `RevisionGrid/Graph/GraphRenderer.cs` (port to `Avalonia.Media.DrawingContext`) |
| `UserControls/RevisionGrid/Graph/Rendering/GraphCache.cs` | `RevisionGrid/Graph/GraphCache.cs` |
| `UserControls/RevisionGrid/Graph/Rendering/SegmentRenderer.cs` | `RevisionGrid/Graph/SegmentRenderer.cs` |
| All other `Graph/` files | Keep as-is (pure data structures, no WinForms dependency) |
| `UserControls/RevisionGrid/Columns/RevisionGraphColumnProvider.cs` | `RevisionGrid/Columns/RevisionGraphColumnProvider.cs` |
| `UserControls/RevisionGrid/Columns/MessageColumnProvider.cs` | `RevisionGrid/Columns/MessageColumnProvider.cs` |
| All other `Columns/` files | Port to Avalonia column providers |
| `UserControls/RevisionGrid/RevisionGridRefRenderer.cs` | Port to `Avalonia.Media` |
| `UserControls/RevisionGrid/FormRevisionFilter.cs` | `RevisionGrid/RevisionFilterDialog.axaml` |
| `UserControls/RevisionGrid/FormQuick*.cs` (3 files) | `RevisionGrid/QuickSelectors/` |

**Key technical note:** `GraphRenderer` uses `System.Drawing.Graphics`. Replace with `Avalonia.Media.DrawingContext`. The graph layout algorithm in `Graph/` (pure C#, no drawing) stays unchanged.

Done criteria: Commit graph renders with lanes and colors, commits are selectable, ref labels (branches/tags) display correctly.

#### Task 4 — Commit Details Panel (depends on Task 3)

| WinForms source | Avalonia target |
|----------------|-----------------|
| `UserControls/CommitDiff.cs` | `CommitDetails/CommitDiffControl.axaml` |
| `UserControls/CommitSummaryUserControl.cs` | `CommitDetails/CommitSummaryControl.axaml` |
| `UserControls/BlameControl.cs` | `CommitDetails/BlameControl.axaml` |
| `UserControls/FileStatusList.cs` + 9 partial files | `CommitDetails/FileStatusList.axaml` |
| `UserControls/RevisionGrid/SuperProjectInfo.cs` | Keep as data class |

Done criteria: Selecting a commit shows its diff, file list, and author summary.

#### Task 5 — Diff & File View (can run parallel with Task 4)

| WinForms source | Avalonia target |
|----------------|-----------------|
| `CommandsDialogs/FormDiff.cs` | `Dialogs/DiffDialog.axaml` |
| `CommandsDialogs/FormFileHistory.cs` | `Dialogs/FileHistoryDialog.axaml` |
| `CommandsDialogs/FormBlame.cs` | `Dialogs/BlameDialog.axaml` |
| `CommandsDialogs/FormEditor.cs` | `Dialogs/EditorDialog.axaml` |
| `Editor/FormGoToLine.cs` | `Dialogs/GoToLineDialog.axaml` |
| `Editor/FormFindInCommitFilesGitGrep.cs` | `Dialogs/FindInCommitFilesDialog.axaml` |
| `CommandsDialogs/FormLog.cs` | `Dialogs/LogDialog.axaml` |

**Key technical note:** Use `AvaloniaEdit` for the diff viewer and editor. It supports syntax highlighting, line numbers, and diff decoration natively.

#### Task 6 — Commit & Staging (can run parallel with Tasks 4–5)

| WinForms source | Avalonia target |
|----------------|-----------------|
| `CommandsDialogs/FormCommit.cs` | `Dialogs/CommitDialog.axaml` |
| `CommandsDialogs/CommitDialog/FormCommitTemplateSettings.cs` | `Dialogs/CommitTemplateSettingsDialog.axaml` |
| `HelperDialogs/FormChooseCommit.cs` | `Dialogs/ChooseCommitDialog.axaml` |
| `HelperDialogs/FormCommitDiff.cs` | `Dialogs/CommitDiffDialog.axaml` |

#### Task 7 — Branch Operations

| WinForms source | Avalonia target |
|----------------|-----------------|
| `CommandsDialogs/FormCheckoutBranch.cs` | `Dialogs/CheckoutBranchDialog.axaml` |
| `CommandsDialogs/FormCheckoutRevision.cs` | `Dialogs/CheckoutRevisionDialog.axaml` |
| `CommandsDialogs/FormCreateBranch.cs` | `Dialogs/CreateBranchDialog.axaml` |
| `CommandsDialogs/FormDeleteBranch.cs` | `Dialogs/DeleteBranchDialog.axaml` |
| `CommandsDialogs/FormDeleteRemoteBranch.cs` | `Dialogs/DeleteRemoteBranchDialog.axaml` |
| `CommandsDialogs/FormRenameBranch.cs` | `Dialogs/RenameBranchDialog.axaml` |
| `CommandsDialogs/FormMergeBranch.cs` | `Dialogs/MergeBranchDialog.axaml` |
| `CommandsDialogs/FormRebase.cs` | `Dialogs/RebaseDialog.axaml` |
| `CommandsDialogs/FormCherryPick.cs` | `Dialogs/CherryPickDialog.axaml` |
| `CommandsDialogs/FormRevertCommit.cs` | `Dialogs/RevertCommitDialog.axaml` |
| `CommandsDialogs/FormCompareToBranch.cs` | `Dialogs/CompareToBranchDialog.axaml` |
| `HelperDialogs/FormResetCurrentBranch.cs` | `Dialogs/ResetCurrentBranchDialog.axaml` |
| `HelperDialogs/FormResetAnotherBranch.cs` | `Dialogs/ResetAnotherBranchDialog.axaml` |
| `HelperDialogs/FormSelectMultipleBranches.cs` | `Dialogs/SelectMultipleBranchesDialog.axaml` |

#### Task 8 — Remote Operations

| WinForms source | Avalonia target |
|----------------|-----------------|
| `CommandsDialogs/FormClone.cs` + `.RemoteActionResult.cs` | `Dialogs/CloneDialog.axaml` |
| `CommandsDialogs/FormPull.cs` | `Dialogs/PullDialog.axaml` |
| `CommandsDialogs/FormPush.cs` | `Dialogs/PushDialog.axaml` |
| `CommandsDialogs/FormRemotes.cs` + `FormRemotesController.cs` | `Dialogs/RemotesDialog.axaml` |
| `UserControls/RemotesComboboxControl.cs` | `Controls/RemotesCombobox.axaml` |

#### Task 9 — Tags, Stash & Reflog

| WinForms source | Avalonia target |
|----------------|-----------------|
| `CommandsDialogs/FormCreateTag.cs` | `Dialogs/CreateTagDialog.axaml` |
| `CommandsDialogs/FormDeleteTag.cs` | `Dialogs/DeleteTagDialog.axaml` |
| `CommandsDialogs/FormStash.cs` | `Dialogs/StashDialog.axaml` |
| `CommandsDialogs/FormReflog.cs` | `Dialogs/ReflogDialog.axaml` |

#### Task 10 — Settings

| WinForms source | Avalonia target |
|----------------|-----------------|
| `CommandsDialogs/FormSettings.cs` | `Settings/SettingsWindow.axaml` |
| `CommandsDialogs/SettingsDialog/Pages/FormBrowseRepoSettingsPage.cs` | `Settings/Pages/BrowseRepoPage.axaml` |
| `CommandsDialogs/SettingsDialog/Pages/FormChooseTranslation.cs` | `Settings/Pages/ChooseTranslationPage.axaml` |
| `CommandsDialogs/SettingsDialog/Pages/FormFixHome.cs` | `Settings/Pages/FixHomePage.axaml` |
| `CommandsDialogs/SettingsDialog/FormAvailableEncodings.cs` | `Settings/AvailableEncodingsDialog.axaml` |

**Key technical note:** Settings storage changes from Windows Registry to JSON config at `~/.config/gitextensions/settings.json`. This is implemented in Tier 2 (`AppSettingsMac.cs`).

#### Task 11 — Submodules & Worktrees

| WinForms source | Avalonia target |
|----------------|-----------------|
| `CommandsDialogs/FormSubmodules.cs` | `Dialogs/SubmodulesDialog.axaml` |
| `CommandsDialogs/FormMergeSubmodule.cs` | `Dialogs/MergeSubmoduleDialog.axaml` |
| `CommandsDialogs/SubmodulesDialog/FormAddSubmodule.cs` | `Dialogs/AddSubmoduleDialog.axaml` |
| `CommandsDialogs/WorktreeDialog/FormCreateWorktree.cs` | `Dialogs/CreateWorktreeDialog.axaml` |
| `CommandsDialogs/WorktreeDialog/FormManageWorktree.cs` | `Dialogs/ManageWorktreeDialog.axaml` |

#### Task 12 — Conflict Resolution & Patches

| WinForms source | Avalonia target |
|----------------|-----------------|
| `CommandsDialogs/FormResolveConflicts.cs` | `Dialogs/ResolveConflictsDialog.axaml` |
| `CommandsDialogs/FormApplyPatch.cs` | `Dialogs/ApplyPatchDialog.axaml` |
| `CommandsDialogs/FormFormatPatch.cs` | `Dialogs/FormatPatchDialog.axaml` |
| `CommandsDialogs/FormViewPatch.cs` | `Dialogs/ViewPatchDialog.axaml` |

#### Task 13 — Repo Maintenance Dialogs

| WinForms source | Avalonia target |
|----------------|-----------------|
| `CommandsDialogs/FormInit.cs` | `Dialogs/InitDialog.axaml` |
| `CommandsDialogs/FormCleanupRepository.cs` | `Dialogs/CleanupRepositoryDialog.axaml` |
| `CommandsDialogs/FormGitIgnore.cs` | `Dialogs/GitIgnoreDialog.axaml` |
| `CommandsDialogs/FormGitAttributes.cs` | `Dialogs/GitAttributesDialog.axaml` |
| `CommandsDialogs/FormMailMap.cs` | `Dialogs/MailMapDialog.axaml` |
| `CommandsDialogs/FormArchive.cs` | `Dialogs/ArchiveDialog.axaml` |
| `CommandsDialogs/FormSparseWorkingCopy.cs` + `ViewModel` | `Dialogs/SparseWorkingCopyDialog.axaml` |
| `CommandsDialogs/FormVerify.cs` + 2 partials | `Dialogs/VerifyDialog.axaml` |
| `CommandsDialogs/FormBisect.cs` | `Dialogs/BisectDialog.axaml` |

#### Task 14 — Helper & Process Dialogs

| WinForms source | Avalonia target |
|----------------|-----------------|
| `HelperDialogs/FormStatus.cs` | `Dialogs/StatusDialog.axaml` |
| `HelperDialogs/FormProcess.cs` | `Dialogs/ProcessDialog.axaml` |
| `HelperDialogs/FormRemoteProcess.cs` | `Dialogs/RemoteProcessDialog.axaml` |
| `HelperDialogs/FormEdit.cs` | `Dialogs/EditDialog.axaml` |
| `HelperDialogs/FormBuildServerCredentials.cs` | `Dialogs/BuildServerCredentialsDialog.axaml` |
| `FormStatusOutputLog.cs` | `Controls/StatusOutputLog.axaml` |
| `FormPuttyError.cs` | `Dialogs/PuttyErrorDialog.axaml` |
| `ScriptsEngine/FormFilePrompt.cs` | `Dialogs/FilePromptDialog.axaml` |

#### Task 15 — Small Git Dialogs

| WinForms source | Avalonia target |
|----------------|-----------------|
| `CommandsDialogs/FormAddFiles.cs` | `Dialogs/AddFilesDialog.axaml` |
| `CommandsDialogs/FormAddToGitIgnore.cs` | `Dialogs/AddToGitIgnoreDialog.axaml` |
| `CommandsDialogs/FormCommandlineHelp.cs` | `Dialogs/CommandlineHelpDialog.axaml` |
| `CommandsDialogs/BrowseDialog/FormBisect.cs` | `Dialogs/BisectDialog.axaml` |
| `CommandsDialogs/BrowseDialog/FormGitCommandLog.cs` | `Dialogs/GitCommandLogDialog.axaml` |
| `CommandsDialogs/BrowseDialog/FormGoToCommit.cs` | `Dialogs/GoToCommitDialog.axaml` |
| `CommandsDialogs/BrowseDialog/FormChangeLog.cs` | `Dialogs/ChangeLogDialog.axaml` |
| `CommandsDialogs/BrowseDialog/FormUpdates.cs` | `Dialogs/UpdatesDialog.axaml` |
| `CommandsDialogs/BrowseDialog/FormDonate.cs` | `Dialogs/DonateDialog.axaml` |
| `CommandsDialogs/FormAbout.cs` | `Dialogs/AboutDialog.axaml` |
| `CommandsDialogs/AboutBoxDialog/FormContributors.cs` | `Dialogs/ContributorsDialog.axaml` |

#### Task 16 — Shared Controls

| WinForms source | Avalonia target |
|----------------|-----------------|
| `UserControls/AvatarControl.cs` | `Controls/AvatarControl.axaml` |
| `UserControls/BranchComboBox.cs` | `Controls/BranchComboBox.axaml` |
| `UserControls/BranchSelector.cs` | `Controls/BranchSelector.axaml` |
| `UserControls/CommitPickerSmallControl.cs` | `Controls/CommitPickerSmall.axaml` |
| `UserControls/InteractiveGitActionControl.cs` | `Controls/InteractiveGitAction.axaml` |
| `UserControls/OutputHistoryControl.cs` + controllers | `Controls/OutputHistory.axaml` |
| `UserControls/PatchGrid.cs` | `Controls/PatchGrid.axaml` |
| `UserControls/PasswordInput.cs` | `Controls/PasswordInput.axaml` |
| `UserControls/RepoStateVisualiser.cs` | `Controls/RepoStateVisualiser.axaml` |

#### Task 17 — Plugins (after Tasks 2–16 are complete)

Port each plugin's UI to Avalonia. Plugin business logic (no UI) carries over unchanged.

Plugins to port:
- `AutoCompileSubmodules` — likely no UI
- `BackgroundFetch` — settings page only
- `BuildServerIntegration` — settings + status column
- `CreateLocalBranches` — minimal dialog
- `DeleteUnusedBranches` — dialog with branch list
- `FindLargeFiles` — dialog with file list
- `GitHub3` — PR creation dialog, OAuth flow
- `Gource` — launch dialog
- `ProxySwitcher` — settings dialog
- `ReleaseNotesGenerator` — output dialog
- `Statistics` — chart views (may use `LiveChartsCore.SkiaSharp.Avalonia`)

---

## 7. Mac-Specific Integrations

### Credential Storage

Replace `AdysTech.CredentialManager` with macOS Keychain.

Interface: `ICredentialStore` in `GitUI.Avalonia/Infrastructure/`
```csharp
public interface ICredentialStore
{
    bool TryGetCredential(string target, out string username, out string password);
    void SaveCredential(string target, string username, string password);
    void DeleteCredential(string target);
}
```

Mac implementation: `MacKeychainCredentialStore` using `Security.framework` P/Invoke (`SecKeychainFindInternetPassword`, `SecKeychainAddInternetPassword`).

### Settings Storage

Replace Windows Registry with JSON config at `~/.config/gitextensions/settings.json`.

Implement `AppSettingsMac` wrapping `Microsoft.Extensions.Configuration.Json`. Mirror all the same keys/values as the existing `AppSettings` class. The existing `AppSettings` static class gets a platform-abstracted backend via a `ISettingsBackend` interface.

### Embedded Terminal

`ConEmu.Core` is removed. Replace the "Open in terminal" feature with:
```csharp
Process.Start("open", $"-a Terminal \"{repoPath}\"");
// Fallback: check for iTerm2
if (File.Exists("/Applications/iTerm.app"))
    Process.Start("open", $"-a iTerm \"{repoPath}\"");
```

The `ConsoleEmulatorOutputControl` (which wraps ConEmu) is replaced with a simple read-only `AvaloniaEdit` output panel for displaying git command output.

### SSH Key Management

Mac uses the standard `ssh-agent` + `~/.ssh/` path — same as Linux. No special handling needed. `GitExtSshAskPass` may work as-is; audit during build.

### Diff & Merge Tools (Mac)

Add Mac-specific entries to `DiffMergeTools`:
- `opendiff` (FileMerge, ships with Xcode Command Line Tools)
- `Kaleidoscope` (`ksdiff`)
- `VS Code` (`code --wait --diff`)
- `BBEdit` (`bbdiff`)
- `Sublime Merge` (`smerge`)

`VsDiffMerge.cs` is stubbed out (Windows-only).

### File/Folder Dialogs

Avalonia's `OpenFileDialog`, `OpenFolderDialog`, and `SaveFileDialog` are cross-platform direct replacements for WinForms equivalents. No special handling needed.

### System Notifications

Use Avalonia's notification system for background operation completion (fetch, pull). On Mac this maps to Notification Center.

---

## 8. Agent Task Dependency Graph

```
Task 0: Fork & Build System
    └── Task 1: Avalonia Infrastructure
            ├── Task 2: Core Shell
            │       └── Task 3: Commit Graph
            │               └── Task 4: Commit Details Panel
            ├── Task 5: Diff & File View          (parallel)
            ├── Task 6: Commit & Staging          (parallel)
            ├── Task 7: Branch Operations         (parallel)
            ├── Task 8: Remote Operations         (parallel)
            ├── Task 9: Tags, Stash & Reflog      (parallel)
            ├── Task 10: Settings                 (parallel)
            ├── Task 11: Submodules & Worktrees   (parallel)
            ├── Task 12: Conflict Resolution      (parallel)
            ├── Task 13: Repo Maintenance         (parallel)
            ├── Task 14: Helper & Process Dialogs (parallel)
            ├── Task 15: Small Git Dialogs        (parallel)
            ├── Task 16: Shared Controls          (parallel)
            └── Task 17: Plugins (after 2-16 done)
```

**Tasks 5–16 can all run in parallel** after Task 1 completes. Each is fully independent — no shared state, no cross-task dependencies.

### What each agent task must contain

Every agent task plan file (see Section 10) must specify:

1. **Scope:** Exact list of WinForms source files to read
2. **Output:** Exact list of Avalonia files to create
3. **Base classes:** Which infrastructure classes to inherit
4. **Done criteria:** How to verify the task is complete (renders, binds, no WinForms refs)
5. **Dependencies:** Which tasks must be complete before this one starts
6. **Testing:** What manual smoke test to run

---

## 9. Mac Distribution

### Build

```bash
dotnet publish GitExtensions.Mac.slnx \
  -r osx-arm64 \
  --self-contained true \
  -c Release \
  -o artifacts/publish/osx-arm64

dotnet publish GitExtensions.Mac.slnx \
  -r osx-x64 \
  --self-contained true \
  -c Release \
  -o artifacts/publish/osx-x64
```

### App Bundle

Wrap the publish output in a standard macOS `.app` bundle:
```
GitExtensions.app/
├── Contents/
│   ├── Info.plist         # Bundle ID, version, minimum macOS version
│   ├── MacOS/
│   │   └── GitExtensions  # The published executable
│   ├── Resources/
│   │   └── AppIcon.icns   # App icon (convert existing .ico → .icns)
│   └── Frameworks/        # (empty for self-contained publish)
```

`Info.plist` key values:
- `CFBundleIdentifier`: `com.gitextensions.gitextensions`
- `CFBundleName`: `Git Extensions`
- `LSMinimumSystemVersion`: `12.0` (macOS Monterey)
- `NSHighResolutionCapable`: `true`

### Signing & Notarization

```bash
codesign --deep --force --verify --verbose \
  --sign "Developer ID Application: <name>" \
  GitExtensions.app

xcrun notarytool submit GitExtensions.dmg \
  --apple-id <apple-id> \
  --team-id <team-id> \
  --password <app-specific-password> \
  --wait
```

### DMG Packaging

```bash
brew install create-dmg
create-dmg \
  --volname "Git Extensions" \
  --window-size 600 400 \
  --icon-size 128 \
  --app-drop-link 450 200 \
  "GitExtensions-<version>-mac.dmg" \
  "GitExtensions.app"
```

### CI (GitHub Actions)

```yaml
# .github/workflows/mac-build.yml
runs-on: macos-latest
steps:
  - uses: actions/setup-dotnet@v4
    with:
      dotnet-version: '10.0.x'
  - run: dotnet build GitExtensions.Mac.slnx
  - run: dotnet test GitExtensions.Mac.slnx --filter "Category!=Windows"
  - run: dotnet publish GitExtensions.Mac.slnx -r osx-arm64 --self-contained -c Release  # release only
```

### Distribution Roadmap

1. Direct `.dmg` download from GitHub Releases
2. Homebrew cask: `brew install --cask gitextensions-mac`
3. (Future) Mac App Store — requires sandboxing audit

---

## 10. Documentation Files for Future Agents

The following files should be created as part of Task 0 (Fork & Build System) to guide all subsequent agents:

### `docs/mac-port/AGENT-GUIDE.md`
General guide for any agent working on this port. Contents:
- How to read a WinForms Form and create the Avalonia equivalent
- Naming conventions (`Form*.cs` → `*Dialog.axaml`, `UserControl*.cs` → `*Control.axaml`)
- How to use the base classes from `GitUI.Avalonia/Base/`
- How to run a smoke test on Mac
- How to verify no WinForms references remain in a file

### `docs/mac-port/WINFORMS-TO-AVALONIA.md`
Cheat sheet of WinForms → Avalonia API mappings:

| WinForms | Avalonia |
|----------|---------|
| `Form` | `Window` |
| `UserControl` | `UserControl` |
| `Panel` | `Panel` or `Border` |
| `SplitContainer` | `Grid` with `GridSplitter` |
| `TabControl` / `TabPage` | `TabControl` / `TabItem` |
| `DataGridView` | `DataGrid` |
| `TreeView` | `TreeView` |
| `ListView` | `ListBox` or `DataGrid` |
| `RichTextBox` | `AvaloniaEdit.TextEditor` |
| `PictureBox` | `Image` |
| `ComboBox` | `ComboBox` |
| `CheckBox` | `CheckBox` |
| `RadioButton` | `RadioButton` |
| `ProgressBar` | `ProgressBar` |
| `ToolStrip` | `Menu` or `ToolBar` |
| `ContextMenuStrip` | `ContextMenu` |
| `StatusStrip` | custom bottom `DockPanel` |
| `OpenFileDialog` | `OpenFileDialog` (Avalonia) |
| `System.Drawing.Graphics` | `Avalonia.Media.DrawingContext` |
| `System.Drawing.Color` | `Avalonia.Media.Color` |
| `System.Drawing.Pen` | `Avalonia.Media.Pen` |
| `System.Drawing.Brush` | `Avalonia.Media.IBrush` |
| `Control.Invoke()` | `Dispatcher.UIThread.InvokeAsync()` |
| `BackgroundWorker` | `Task` + `ReactiveUI` |
| `Timer` | `DispatcherTimer` |
| `MessageBox.Show()` | Custom `MessageDialog` (Avalonia) |
| `this.BeginInvoke()` | `Dispatcher.UIThread.Post()` |

### `docs/mac-port/PACKAGE-REPLACEMENTS.md`
Exact NuGet package swap list (mirrors Section 4 of this spec).

### `docs/mac-port/SETTINGS-MIGRATION.md`
Documents the old Windows Registry key paths and their new JSON config equivalents.

### `docs/mac-port/TASK-STATUS.md`
Living document. Each agent updates this when their task completes.

```markdown
| Task | Description | Status | Agent | Notes |
|------|------------|--------|-------|-------|
| 0    | Fork & Build System | pending | - | - |
| 1    | Avalonia Infrastructure | pending | - | Blocks all others |
| 2    | Core Shell | pending | - | - |
| 3    | Commit Graph | pending | - | Hardest task |
| 4    | Commit Details Panel | pending | - | - |
| 5-16 | Screen batches | pending | - | All parallel after Task 1 |
| 17   | Plugins | pending | - | After Tasks 2-16 |
```

---

## 11. Testing Strategy

- **Unit tests:** `GitCommands`, `GitExtUtils`, `GitExtensions.Extensibility` — existing tests run unchanged (they have no UI dependency)
- **UI smoke tests:** Manual checklist per task (run on macOS, verify the dialog opens and functions)
- **No mocking of git:** Tests that touch git operations use real repositories (in temp directories)
- **CI:** Run `dotnet test` on `macos-latest` runner in GitHub Actions on every PR

---

## 12. Out of Scope (for this port)

- Windows Explorer shell extension equivalent for Finder
- Spotlight integration
- Touch Bar support
- iOS/iPad port
- Windows support from this fork (this is a Mac fork, not a cross-platform rewrite)
- Linux (can be added later — Avalonia supports Linux, same codebase)
