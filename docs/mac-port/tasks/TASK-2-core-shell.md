# Task 2 — Core Shell

**Status:** pending  
**Depends on:** Task 1  
**Blocks:** Task 3 (Commit Graph)  
**Estimated complexity:** High — main window is the most complex single form

---

## Goal

Port the main application window (`FormBrowse`) to Avalonia. When done, the app launches, shows a dashboard of recent repositories, and lets the user open a repository. The commit graph area will be a placeholder (Task 3 fills it in).

---

## Source files to read

```
src/app/GitUI/CommandsDialogs/FormBrowse.cs
src/app/GitUI/CommandsDialogs/FormBrowse.InitCommitDetails.cs
src/app/GitUI/CommandsDialogs/FormBrowse.InitMenusAndToolbars.cs
src/app/GitUI/CommandsDialogs/FormBrowse.InitRevisionGrid.cs
src/app/GitUI/CommandsDialogs/FormBrowse.UpdateTargets.cs
src/app/GitUI/CommandsDialogs/BrowseDialog/DashboardControl/
src/app/GitUI/CommandsDialogs/BrowseDialog/FormOpenDirectory.cs
src/app/GitUI/CommandsDialogs/BrowseDialog/FormRecentReposSettings.cs
src/app/GitUI/UserControls/FilterToolBar.cs
src/app/GitUI/UserControls/RepoStateVisualiser.cs
```

---

## Files to create

All in `src/app/GitUI.Avalonia/`:

```
MainWindow.axaml
MainWindow.axaml.cs
Dashboard/
    DashboardView.axaml
    DashboardView.axaml.cs
    RecentRepoItem.axaml           (single recent repo row)
    OpenRepositoryDialog.axaml
    OpenRepositoryDialog.axaml.cs
    RecentReposSettingsDialog.axaml
    RecentReposSettingsDialog.axaml.cs
Controls/
    FilterToolBar.axaml
    FilterToolBar.axaml.cs
    RepoStateVisualiser.axaml
    RepoStateVisualiser.axaml.cs
```

---

## Layout of MainWindow

The main window has this layout (from `FormBrowse`):

```
┌──────────────────────────────────────────────┐
│ Menu Bar (File, View, Repository, Commands…) │
├──────────────────────────────────────────────┤
│ Toolbar (common actions)                      │
├──────────────────────────────────────────────┤
│ Filter toolbar                                │
├────────────────────┬─────────────────────────┤
│                    │ Commit details panel     │
│  Commit graph      │  (Task 4 fills this in)  │
│  (Task 3)          ├─────────────────────────┤
│                    │ File list                │
├────────────────────┴─────────────────────────┤
│ Status bar                                    │
└──────────────────────────────────────────────┘
```

For now, the commit graph area and commit details area are placeholder `Border` controls with a "Loading..." label. Task 3 replaces them.

Use Avalonia `Grid` with a `GridSplitter` for the horizontal split between graph and details.

---

## Menu structure

Port the full menu from `FormBrowse.InitMenusAndToolbars.cs`. Key menus:
- **File:** Start page, open repository, recent repositories, clone, init, settings, exit
- **View:** Commit graph options, layout toggles
- **Repository:** Fetch, pull, push, manage remotes, submodules, worktrees
- **Commands:** Commit, branch operations, stash, cherry-pick, rebase, etc.
- **Plugins:** (populated dynamically from plugin system)
- **Help:** About, documentation

Wire menu items to the existing `GitUICommands` methods where possible — the logic is already there.

---

## Dashboard

The dashboard (shown when no repository is open) shows:
- Recent repositories list (from `JsonSettingsBackend`)
- Buttons: Clone, Open, Init

Each recent repo item shows: repo name, path, last accessed time.

---

## Done Criteria

- [ ] App launches and shows the dashboard
- [ ] Recent repositories list populates from `~/.config/gitextensions/settings.json`
- [ ] User can open a repository via File > Open or dashboard button
- [ ] After opening a repo: menu bar and toolbar are active, central area shows placeholder for commit graph
- [ ] Filter toolbar renders (search box, branch filter, etc.)
- [ ] Repo state visualiser shows in toolbar (clean/dirty indicator)
- [ ] No WinForms references anywhere in created files
- [ ] `docs/mac-port/TASK-STATUS.md` updated to `complete` for Task 2
