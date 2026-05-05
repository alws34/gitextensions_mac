# Settings Parity Audit

**Baseline:** `origin/master` at `a924520cc9b4649b5e459030bff119edddb3ab12`

**Scope:** Compare the Windows GitExtensions Settings dialog with the Avalonia macOS Settings window and identify the missing pages, storage model gaps, and next implementation targets.

## Windows Settings Inventory

Windows GitExtensions exposes a tree of settings pages:

| Group | Pages |
| --- | --- |
| Git Extensions | Checklist, General, Appearance, Revision links, Build server integration, Scripts, Hotkeys, Shell extension, Advanced, Detailed, SSH |
| Appearance | Sorting, Colors, Fonts, Console style |
| Advanced | Confirmations |
| Detailed | Browse repository window, Commit dialog, Diff viewer, Blame viewer |
| Git | Paths, Config, Config advanced, Git intro |
| Plugins | Plugin intro plus dynamic plugin settings pages |

The Shell extension page is Windows-only and should remain hidden or replaced by a macOS-specific integration page when the port grows equivalent Finder integration.

## Avalonia State After This Batch

Avalonia now has these pages:

| Page | Status |
| --- | --- |
| General | Added. Startup repo, recent repo limit, native macOS menu bar toggle. |
| Git | Existing. Basic git executable/path settings only. |
| Revision Grid | Added. Tag visibility, first-parent mode, graph options, max revisions. |
| Diff Viewer | Added. Split view default, syntax highlighting, word wrap. |
| Diff & Merge Tools | Existing. Basic external diff/merge tool fields. |
| Appearance | Existing. Basic theme and compact layout switches. |
| Commit | Existing. Basic commit behavior switches. |
| SSH | Existing. Basic SSH client/key fields. |
| Credentials | Existing. Basic credential storage controls. |
| Advanced | Existing. Basic advanced options. |

## Functional Gaps

The largest gap is storage parity. The Windows Settings dialog writes through shared GitExtensions settings and git config helpers. The Avalonia window currently writes mostly to the Avalonia JSON settings backend. That is acceptable for Avalonia-only UI state, but not enough for settings that must be shared with existing GitExtensions logic, repository config, global git config, plugins, or scripts.

High-impact missing settings pages:

| Missing Area | Expected Windows Behavior | Recommended Target |
| --- | --- | --- |
| Git paths and config | Configure git executable, global/local config values, environment checks. | Expand `GitPage` and back it with shared GitCommands/AppSettings APIs where available. |
| General | Language, startup, shell/file integration, recent repositories, behavior toggles. | Keep Avalonia-only startup state in JSON; move shared behavior toggles to shared settings. |
| Appearance children | Sorting, colors, fonts, console style. | Split current Appearance page into child pages once shared color/font settings are mapped. |
| Detailed children | Browse, Commit dialog, Diff viewer, Blame viewer pages. | Continue adding focused pages that immediately affect browser controls. |
| Confirmations | Reset/clean/delete/checkout confirmation toggles. | Add a Confirmations page before wiring destructive command bypasses. |
| Revision links | Regex-driven issue/commit link behavior. | Port the Windows model and reuse shared revision link parsing. |
| Scripts and Hotkeys | User scripts, toolbar scripts, keyboard shortcut editing. | Prefer shared scripts/hotkey models over JSON-only storage. |
| Build server integration | Configure build server providers and credentials. | Reuse existing provider abstractions; keep secrets in platform credential storage. |
| Plugins | Dynamic plugin settings pages. | Requires plugin discovery and settings-page host in Avalonia. |

## Changes Landed In This Batch

- Added General, Revision Grid, and Diff Viewer pages to the Avalonia Settings window.
- Wired recent repository count and open-last-repository-on-startup into `MainWindow`.
- Wired revision grid max revisions, first-parent log mode, and tag visibility into `RevisionGridControl`.
- Wired diff split-view default, syntax highlighting, and word wrap into `CommitDiffControl`.
- Added a native macOS app/window menu path so Settings can live in the app menu and GitExtensions browser menus can live in the macOS menu bar.

## Next Recommended Settings Work

1. Replace JSON-backed Git settings with shared GitExtensions settings for values that already exist in Windows.
2. Add a Confirmations page before changing destructive-command UX.
3. Split Appearance and Detailed pages into child pages that mirror Windows naming.
4. Add Scripts and Hotkeys pages with shared model bindings.
5. Add plugin settings host after the plugin discovery path is audited.
