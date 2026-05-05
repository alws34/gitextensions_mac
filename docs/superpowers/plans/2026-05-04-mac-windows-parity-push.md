# GitExtensions Mac Windows Parity Push Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make the Avalonia macOS adaptation look, feel, and operate closer to the original GitExtensions Windows UI, focusing first on the main browser window: top bar, sidebar, revision grid, diff view, and commit details.

**Architecture:** Compare `mac_adaptation` with `origin/master` WinForms UI behavior, then port visible and operational gaps into the Avalonia shell without rewriting shared Git logic. Preserve current uncommitted work and use small, focused changes in existing Avalonia controls.

**Tech Stack:** .NET 10, C# 14, Avalonia 11, AvaloniaEdit, GitExtensions shared GitCommands/GitUI domain logic.

**Reference docs:**
- `docs/avalonia-ui-development/SKILL.md`
- `docs/superpowers/plans/2026-04-27-avalonia-full-parity-roadmap.md`
- `docs/mac-port/AGENT-GUIDE.md`
- `src/app/GitUI/CommandsDialogs/FormBrowse*.cs`
- `src/app/GitUI/CommandsDialogs/RevisionDiffControl*.cs`
- `src/app/GitUI/UserControls/RevisionGrid/**`
- `src/app/GitUI.Avalonia/**`

---

## Progress Log

- [x] Installed `caveman` skill from `JuliusBrussee/caveman`.
- [x] Installed Superpowers workflow skills from `obra/superpowers`.
- [x] Installed `avalonia-ui-development` skill from local project docs.
- [x] Fetched `origin/master` at `a924520cc9b4649b5e459030bff119edddb3ab12`.
- [x] Collect subagent gap reports for top bar/sidebar, diff/commit details, and revision grid behavior.
- [x] Implement highest-impact parity fixes that fit safely in current dirty worktree.
- [x] Build and test `GitExtensions.Mac.slnx`.
- [x] Record completed changes and remaining gaps here.
- [x] Fixed dirty worktree compile blockers in sidebar branch item binding, sidebar handlers, revision-grid selected-row/navigation wiring, and nullability/style issues.
- [x] Wired status counts to refresh after open, commit, pull, push, fetch, checkout, reset, stash, and action abort paths.
- [x] Split sidebar branch checkout from submodule/worktree repository switching so filesystem paths are opened as repositories instead of passed to `git checkout`.
- [x] Completed focused native macOS menu audit for the app menu name, application menu, and browser window menu export.
- [x] Completed focused Settings parity audit against the Windows Settings dialog inventory.
- [x] Added General, Revision Grid, and Diff Viewer Settings pages to the Avalonia Settings window.
- [x] Wired Settings values into startup recent repository behavior, revision-grid log/tag behavior, and diff viewer defaults.
- [x] Added `docs/mac-port/settings-parity-audit.md` with the current Settings gap map and recommended next work.
- [x] Re-run full solution build and Avalonia test suite after the native menu and Settings work.

## Task 1: Main Browser Gap Reports

**Files:**
- Compare: `src/app/GitUI/CommandsDialogs/FormBrowse*.cs`
- Compare: `src/app/GitUI.Avalonia/MainWindow.axaml`
- Compare: `src/app/GitUI.Avalonia/MainWindow.axaml.cs`
- Compare: `src/app/GitUI.Avalonia/LeftPanel/RepoBrowserPanel.axaml`
- Compare: `src/app/GitUI.Avalonia/LeftPanel/RepoBrowserPanel.axaml.cs`
- Compare: `src/app/GitUI.Avalonia/RevisionGrid/**`
- Compare: `src/app/GitUI.Avalonia/CommitDetails/**`

- [x] **Step 1: Top bar and menu parity report**

  Subagent compares `FormBrowse.InitMenusAndToolbars.cs`, `FormBrowse.Designer.cs`, and Avalonia `MainWindow` files. Output must list missing commands, visual mismatches, and direct file/method targets for fixes.

- [x] **Step 2: Sidebar parity report**

  Subagent compares Windows left panel/revision tree behavior with Avalonia `RepoBrowserPanel`. Output must list missing sections/actions, selection behavior gaps, and direct file/method targets for fixes.

- [x] **Step 3: Diff and commit details parity report**

  Subagent compares Windows `RevisionDiffControl`, `CommitInfo`, and `CommitDiff` controls with Avalonia `CommitDetails` controls. Output must list missing view modes, coloring, file context actions, and direct file/method targets for fixes.

## Task 2: Highest-Impact Fix Batch

**Files:**
- Modify: `src/app/GitUI.Avalonia/MainWindow.axaml`
- Modify: `src/app/GitUI.Avalonia/MainWindow.axaml.cs`
- Modify: `src/app/GitUI.Avalonia/LeftPanel/RepoBrowserPanel.axaml`
- Modify: `src/app/GitUI.Avalonia/LeftPanel/RepoBrowserPanel.axaml.cs`
- Modify: `src/app/GitUI.Avalonia/CommitDetails/CommitDetailsPanel.axaml`
- Modify: `src/app/GitUI.Avalonia/CommitDetails/CommitDetailsPanel.axaml.cs`
- Modify: `src/app/GitUI.Avalonia/CommitDetails/FileStatusList.axaml`
- Modify: `src/app/GitUI.Avalonia/CommitDetails/FileStatusList.axaml.cs`
- Modify: `src/app/GitUI.Avalonia/RevisionGrid/RevisionGridControl.axaml.cs`

- [x] **Step 1: Apply safe top bar parity fixes**

  Add or wire missing browser commands where a matching Avalonia dialog or shared command already exists. Do not add placeholder buttons without behavior.

- [x] **Step 2: Apply safe sidebar parity fixes**

  Wire context menu actions for existing branch, remote, tag, stash, submodule, and worktree dialogs/commands. Preserve existing branch tree formatting.

- [x] **Step 3: Apply safe diff/commit details parity fixes**

  Improve diff view mode state, file status actions, and commit details behavior using existing Avalonia controls and shared Git commands.

- [x] **Step 4: Apply safe revision grid parity fixes**

  Wire filter/search/navigation behavior where current controls already expose state or events.

## Task 3: Verification

**Files:**
- Build: `GitExtensions.Mac.slnx`
- Test: `tests/app/GitUI.Avalonia.Tests`

- [x] **Step 1: Build**

  Run: `~/.dotnet/dotnet build GitExtensions.Mac.slnx`
  Result: PASS with 51 existing Avalonia `AVLN3001` warnings about dialog AXAML resources lacking public constructors.

- [x] **Step 2: Tests**

  Run: `~/.dotnet/dotnet test tests/app/GitUI.Avalonia.Tests/GitUI.Avalonia.Tests.csproj`
  Result: PASS. 33 passed, 0 failed, 0 skipped.

- [x] **Step 3: Update this plan**

  Mark completed steps and add a short remaining-gap list.

## Remaining Gap Summary From Subagents

- Top bar/menu: Windows menu taxonomy is still not matched exactly. Missing Start/Dashboard split, Repository Hosts, Plugins, full Tools/Help items, scripts toolbar, layout/commit-info/submodule/worktree toolbar split buttons, and Windows-style default pull behavior.
- Sidebar: Avalonia is still list/expander based, not Windows' `RepoObjectsTree`. Missing root visibility toggles, persisted root ordering, branch/tag folders, remote hierarchy, multi-select, selected-ref filtering, revision-grid sync, and most Windows context menu actions.
- Revision grid: Missing Windows view modes for branch scope, refs, stashes, notes, artificial commits, first-parent/topo/author-date sorting, non-relative gray styling, selected branch highlight, and many commit context actions.
- Diff/file list: File list is tree-only and has only diff/history/blame context actions. Missing flat/group/filter modes, staging/reset/open/difftool/gitignore/submodule/user-script actions, and revision-aware blame/history context.
- Action bars: Avalonia has one generic abort bar. Windows has separate git-action and bisect controls with richer per-state actions.
- Warnings: Build passes, but Avalonia still reports 51 `AVLN3001` warnings for dialogs with non-public/parameter-only constructors.

## Context Compact 2026-05-04

**Baseline:** Branch `mac_adaptation`, `origin/master` fetched at `a924520cc9b4649b5e459030bff119edddb3ab12`. Latest verified commands before this compact: `~/.dotnet/dotnet build GitExtensions.Mac.slnx` passed with 51 `AVLN3001` warnings, and `~/.dotnet/dotnet test tests/app/GitUI.Avalonia.Tests/GitUI.Avalonia.Tests.csproj` passed with 33 tests.

**Current user ask:** Spin up implementation subagents and test agents; make the macOS Avalonia port look and operate like original GitExtensions for Windows; especially restore missing right-click context menu functionality. Keep this file updated as the progress ledger.

**Non-negotiables:** Preserve user/other-agent dirty work. Use `apply_patch` for manual edits. Keep agent write sets disjoint. Re-run build/tests before claiming progress.

**Immediate implementation focus:** Add real, wired context-menu actions before adding cosmetic placeholders:
- File list context menu: copy path/name, open working file, reveal in Finder, open containing folder, copy full path, reset file, stage/unstage when safe, history/blame already present.
- Revision grid context menu: copy short/full hash, copy subject/body-style data where available, compare/open diff dialogs where existing dialogs support it, suppress commit-only actions for artificial rows.
- Sidebar context menu: continue branch/remote/tag/stash/submodule/worktree actions, but do not attempt a full `RepoObjectsTree` rewrite without a separate focused plan.
- Top/menu: add missing menu entries only when a matching Avalonia dialog or command exists.

**Test focus:** Add/extend Avalonia unit tests where controls can be instantiated headlessly, and add a manual click-test checklist for commands that require real repos/UI dialogs.

## Active Agent Wave 2026-05-04

- [x] Revision-grid context menu agent owns `src/app/GitUI.Avalonia/RevisionGrid/RevisionDataGrid.axaml`, `RevisionDataGrid.axaml.cs`, and `RevisionGridControl.axaml.cs`.
- [x] Sidebar context menu agent owns `src/app/GitUI.Avalonia/LeftPanel/RepoBrowserPanel.axaml` and `RepoBrowserPanel.axaml.cs`.
- [x] Top menu/toolbar agent owns `src/app/GitUI.Avalonia/MainWindow.axaml` and `MainWindow.axaml.cs`.
- [x] Automated test agent owns `tests/app/GitUI.Avalonia.Tests/**`.
- [x] Manual QA agent owns `docs/mac-port/manual-click-test-matrix.md`.
- [x] Local controller owns file-list context menu in `src/app/GitUI.Avalonia/CommitDetails/FileStatusList.axaml`, `FileStatusList.axaml.cs`, and status/error forwarding in `CommitDetailsPanel.axaml.cs`.
- [x] Local build after file-list context menu: `~/.dotnet/dotnet build GitExtensions.Mac.slnx` passed with existing `AVLN3001` warnings.

## Native Menu And Settings Audit 2026-05-05

- [x] Native menu audit agent identified the app title issue, the oversized app menu, and the missing window-level native menu export.
- [x] Settings audit agent inventoried the Windows Settings tree and mapped Avalonia missing pages.
- [x] Added `Name="Git Extensions"` to `App.axaml` and kept `ApplicationTitle`/Info.plist naming aligned.
- [x] Trimmed the app-level native menu to About, Settings, and Quit.
- [x] Exported the existing Avalonia browser menu tree as the macOS native window menu on macOS, with Ctrl shortcuts mapped to Command shortcuts.
- [x] Kept the in-window menu visible on non-macOS or when native menu mode is disabled.
- [x] Added Settings categories for General, Revision Grid, and Diff Viewer.
- [x] Wired first-parent, max revisions, and tag visibility into revision loading.
- [x] Wired split diff default, syntax highlighting, and word wrap into the diff viewer.
- [x] Targeted build: `~/.dotnet/dotnet build src/app/GitUI.Avalonia/GitUI.Avalonia.csproj` passed with the existing 51 `AVLN3001` dialog constructor warnings.
- [x] Full verification: `~/.dotnet/dotnet build GitExtensions.Mac.slnx` passed with the existing 51 `AVLN3001` dialog constructor warnings.
- [x] Full verification: `~/.dotnet/dotnet test tests/app/GitUI.Avalonia.Tests/GitUI.Avalonia.Tests.csproj --nologo --verbosity minimal` passed: 55 passed, 0 failed, 0 skipped.

## Branch Head Context Menu Fix 2026-05-05

- [x] Fixed sidebar right-click selection so local branches, remotes, tags, submodules, worktrees, and stashes select the row under the pointer before opening the context menu.
- [x] Added revision-grid branch-ref checkout actions for commits with local branch heads.
- [x] Added revision-grid remote branch checkout-as-local actions for commits with remote branch heads.
- [x] Wired revision-grid branch checkout requests through `RevisionGridControl` into `MainWindow`.
- [x] Quoted branch names when running checkout commands.
- [x] Targeted verification: `~/.dotnet/dotnet test tests/app/GitUI.Avalonia.Tests/GitUI.Avalonia.Tests.csproj --nologo --verbosity minimal` passed: 58 passed, 0 failed, 0 skipped.
- [x] Full verification: `~/.dotnet/dotnet build GitExtensions.Mac.slnx` passed with 53 existing `AVLN3001` constructor-loader warnings.

## Next Missing Parity Queue

- [ ] Revision-grid ref context menus: add merge/rebase/delete/rename/push for local and remote branch refs, and tag-specific create-branch/delete/copy actions that match Windows `RefContextMenus`.
- [ ] Sidebar branch tree: replace flat branch list with nested branch folders, remote hierarchy, selected-ref filtering, sort modes, expand/collapse root behavior, and persisted root order.
- [ ] File list: add flat/group/tree modes, staged/unstaged file operations, reset/open-with-difftool/open-containing-folder, submodule-specific actions, and user script actions.
- [ ] Revision grid view: add branch scope modes, topological/author-date/first-parent controls, stash/notes/artificial commit visibility controls, selected branch highlight, and non-relative gray styling.
- [ ] Settings: move shared GitExtensions settings off Avalonia-only JSON where Windows already has AppSettings/git-config-backed settings, then add Confirmations, Hotkeys, Scripts, Revision Links, Build Server, and Plugins pages.
- [ ] Native menu and toolbar: keep aligning the menu taxonomy and toolbar split buttons with Windows, including repository hosts/plugins/scripts/help actions where matching Avalonia dialogs exist.
- [ ] State/action bars: split merge/cherry-pick/revert/bisect state banners into Windows-equivalent controls with the richer per-state actions.

## Parity Audit And Polish Slice 2026-05-05

- [x] Launched focused audit agents for revision-grid ref actions, menu/toolbar discoverability, and test coverage.
- [x] Confirmed merge/squash already exists in `MergeBranchDialog`, but is not discoverable enough and left-panel branch merge bypasses the dialog.
- [x] Confirmed revision-grid ref badges still miss Windows actions: local branch merge/rebase/rename/delete/push, remote branch merge/rebase/delete, tag merge/delete.
- [x] Confirmed top toolbar polish gaps: repo selector clipped by fixed `ToolBtn` width, split buttons lack matching style, command order differs from Windows, and fetch/pull/push are missing from the Commands menu.
- [x] Confirmed commit summary layout risk: long commit subjects can consume the fixed summary area and visually run into author/date information.
- [x] Add dialog-backed merge/rebase from sidebar and revision-grid branch/tag refs so squash/no-ff options are reachable from branch context menus.
- [x] Add seeded merge/rebase/push/rename/delete dialogs for branch/ref actions.
- [x] Add revision-grid context menu actions for branch/ref merge, rebase, rename, delete, push, and tag delete.
- [x] Add Commands menu Pull/Fetch and Push entries and relabel Repository fetch commands to match the actual `fetch --all` behavior.
- [x] Reorder and restyle the toolbar into a Windows-like command flow with non-clipping repository selector and styled split buttons.
- [x] Fix commit summary layout so long subjects wrap within the summary area without overrunning author/date.
- [x] Add focused tests for revision-grid ref context menus and commit summary wrapping.
- [x] Run Avalonia tests and full solution build.
  - `~/.dotnet/dotnet test tests/app/GitUI.Avalonia.Tests/GitUI.Avalonia.Tests.csproj --nologo --verbosity minimal`: PASS, 61 passed, 0 failed.
  - `~/.dotnet/dotnet build GitExtensions.Mac.slnx`: PASS, 0 warnings, 0 errors.
- [x] Commit the verified slice without pushing.
