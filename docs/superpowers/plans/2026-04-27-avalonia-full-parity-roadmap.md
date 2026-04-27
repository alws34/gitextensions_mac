# GitExtensions Mac Port — Full Parity Roadmap

> Master index for all remaining work to bring the Avalonia Mac port to full feature parity with the WinForms Windows version.
> Each sub-plan is self-contained and can be executed independently by agents.

---

## Status at roadmap creation (2026-04-27)

The first 11 tasks (toolbar, left panel, tabbed commit details, layout toggle, action bars, ref context menus, menus, grid columns, file tree, recent repos, ref badge colors) are **complete** on branch `mac_adaptation`.

**What works today:**
- Revision grid with graph, refs, author, date, hash
- Left panel: branches / remotes / tags / stashes (basic)
- Commit dialog: stage all / unstage all, diff preview, commit with message
- Push / Pull / Fetch (basic)
- Merge / Rebase / Stash / Cherry-pick (basic dialogs)
- Branch create / checkout / delete
- Settings window (shell, not all pages fully functional)
- Recent repositories
- Dashboard

**What is missing or broken:**
- Diff viewer shows plain text (no green/red line backgrounds)
- Commit dialog has no per-file staging (only Stage All)
- BlameDialog and FileHistoryDialog have logic but no AXAML
- Stash list has no actions from left panel
- RebaseDialog lacks autostash and continue/abort
- Toolbar uses text labels (not icon buttons)
- No Tree tab in commit details
- Filter bar not connected to grid
- Submodule / worktree sections missing from left panel
- Settings Git page may not persist properly

---

## Sub-plans (execute in order A → B → C → D)

### Plan A — Diff Coloring + Per-file Staging (HIGHEST PRIORITY)
**File:** `docs/superpowers/plans/2026-04-27-avalonia-plan-A-diff-staging.md`

| # | Task | Impact |
|---|---|---|
| A1 | `DiffLineColorizer` class (green + / red - lines) | Critical |
| A2 | Apply colorizer to `CommitDiffControl` | Critical |
| A3 | Apply colorizer to `CommitDialog` diff preview | Critical |
| A4 | Per-file stage/unstage via double-tap | Critical |
| A5 | Right-click context menu for file lists | High |
| A6 | Commit success → close dialog + refresh grid | High |

---

### Plan B — Missing Dialog UIs + Dialog Completeness
**File:** `docs/superpowers/plans/2026-04-27-avalonia-plan-B-dialogs-axaml.md`

| # | Task | Impact |
|---|---|---|
| B1 | `BlameDialog.axaml` (create missing layout) | High |
| B2 | `FileHistoryDialog.axaml` (create missing layout) | High |
| B3 | Wire Blame + FileHistory from file context menu | High |
| B4 | `StashDialog` — named stash message | Medium |
| B5 | `RebaseDialog` — autostash + continue/skip/abort | Medium |
| B6 | Left panel — stash pop/apply/drop | Medium |

---

### Plan C — Visual Parity
**File:** `docs/superpowers/plans/2026-04-27-avalonia-plan-C-visual-parity.md`

| # | Task | Impact |
|---|---|---|
| C1 | Colored file status icons (M/A/D/R) | High |
| C2 | CommitDetailsPanel Tree tab | High |
| C3 | Toolbar icon buttons with hover style | Medium |
| C4 | Settings Git page — path + user name/email | Medium |
| C5 | CommitSummaryControl — ref labels | Medium |

---

### Plan D — Advanced Features
**File:** `docs/superpowers/plans/2026-04-27-avalonia-plan-D-advanced.md`

| # | Task | Impact |
|---|---|---|
| D1 | Go To Commit — navigate grid to hash | Medium |
| D2 | Filter bar → revision grid search | Medium |
| D3 | Left panel — Submodules section | Medium |
| D4 | Left panel — Worktrees section | Low |
| D5 | Left panel — Rename branch | Low |

---

## Gap table (reference)

| Feature | WinForms | Avalonia Mac | Plan |
|---|---|---|---|
| Colored diff (+/-) | ✅ | ❌ | A1-A3 |
| Per-file staging | ✅ | ❌ | A4-A5 |
| Commit refresh after commit | ✅ | ❌ | A6 |
| BlameDialog | ✅ | ❌ AXAML missing | B1,B3 |
| FileHistoryDialog | ✅ | ❌ AXAML missing | B2,B3 |
| Named stash | ✅ | ⚠️ | B4 |
| Rebase autostash/continue | ✅ | ⚠️ | B5 |
| Stash ops from left panel | ✅ | ❌ | B6 |
| File status icons (colored) | ✅ | ⚠️ gray | C1 |
| Tree tab in commit details | ✅ | ❌ | C2 |
| Icon toolbar buttons | ✅ | ⚠️ text | C3 |
| Settings git path persist | ✅ | ⚠️ | C4 |
| Ref labels in summary | ✅ | ❌ | C5 |
| Go to commit navigation | ✅ | ⚠️ dialog exists, not wired | D1 |
| Filter bar search | ✅ | ❌ not connected | D2 |
| Submodule panel | ✅ | ❌ | D3 |
| Worktree panel | ✅ | ❌ | D4 |
| Branch rename from panel | ✅ | ❌ | D5 |

---

## Out of scope (intentionally deferred)

- Build server integration (Jenkins, TeamCity, Azure DevOps) — requires plugin architecture
- Plugin system — requires WinForms plugin loader port
- GPG signing in commit dialog — requires gpg binary integration
- Interactive hunk staging (git add -p) — complex, deferred
- Side-by-side diff view — complex, deferred
- Syntax highlighting in diff viewer — requires language detection, deferred
- External merge tool UI — configuration exists, tool launch deferred

---

## Execution instructions for agents

```
Run plans in order: A → B → C → D
Each plan uses superpowers:subagent-driven-development
dotnet binary: ~/.dotnet/dotnet
Branch: mac_adaptation
Working directory: /Users/alon/Desktop/gitextensions
```
