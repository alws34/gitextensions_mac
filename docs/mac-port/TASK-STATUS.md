# Mac Port — Task Status

Living document. Update your task's row when status changes.

Last updated: 2026-04-21

---

## Dependency Graph

```
Task 0 (Fork & Build)
  └── Task 1 (Avalonia Infrastructure)  ← BLOCKS EVERYTHING
        ├── Task 2 (Core Shell)
        │     └── Task 3 (Commit Graph)
        │           └── Task 4 (Commit Details Panel)
        ├── Task 5  (Diff & File View)          ─┐
        ├── Task 6  (Commit & Staging)           │
        ├── Task 7  (Branch Operations)          │
        ├── Task 8  (Remote Operations)          │ all parallel
        ├── Task 9  (Tags, Stash & Reflog)       │
        ├── Task 10 (Settings)                   │
        ├── Task 11 (Submodules & Worktrees)     │
        ├── Task 12 (Conflict Resolution)        │
        ├── Task 13 (Repo Maintenance)           │
        ├── Task 14 (Helper & Process Dialogs)   │
        ├── Task 15 (Small Git Dialogs)          │
        ├── Task 16 (Shared Controls)           ─┘
        └── Task 17 (Plugins) ← after Tasks 2-16 done
```

---

## Task Table

| # | Task | Status | Agent | Started | Completed | Notes |
|---|------|--------|-------|---------|-----------|-------|
| 0 | Fork & Build System | complete | Claude | 2026-04-19 | 2026-04-19 | GitExtensions.Mac.slnx, .NET 10, Avalonia 11 |
| 1 | Avalonia Infrastructure | complete | Claude | 2026-04-19 | 2026-04-19 | Plan A — commit 361598b8a |
| 2 | Core Shell | complete | Claude | 2026-04-19 | 2026-04-19 | Plan B — MainWindow, Dashboard, RevisionGrid |
| 3 | Commit Graph | complete | Claude | 2026-04-19 | 2026-04-19 | Plan B — commit 7ae5894cd |
| 4 | Commit Details Panel | complete | Claude | 2026-04-19 | 2026-04-19 | Plan B — CommitSummaryControl, FileStatusList, DiffControl |
| 5 | Diff & File View | complete | Claude | 2026-04-21 | 2026-04-21 | Plan D1 — DiffDialog, FileHistoryDialog, BlameDialog, LogDialog |
| 6 | Commit & Staging | complete | Claude | 2026-04-20 | 2026-04-20 | Plan C — CommitDialog with staging |
| 7 | Branch Operations | complete | Claude | 2026-04-20 | 2026-04-20 | Plan C — 7 branch dialogs |
| 8 | Remote Operations | complete | Claude | 2026-04-20 | 2026-04-20 | Plan C — Clone, Pull, Push, RemotesDialog |
| 9 | Tags, Stash & Reflog | complete | Claude | 2026-04-20 | 2026-04-20 | Plan C — CreateTag, DeleteTag, Stash, CherryPick, Reflog |
| 10 | Settings | complete | Claude | 2026-04-20 | 2026-04-20 | Plan C — SettingsWindow with 4 pages |
| 11 | Submodules & Worktrees | complete | Claude | 2026-04-20 | 2026-04-20 | Plan C — SubmodulesDialog, ManageWorktreeDialog |
| 12 | Conflict Resolution & Patches | complete | Claude | 2026-04-20 | 2026-04-20 | Plan C — ResolveConflictsDialog, ApplyPatch, FormatPatch |
| 13 | Repo Maintenance Dialogs | complete | Claude | 2026-04-20 | 2026-04-20 | Plan C — Init, Cleanup, GitIgnore, GitAttributes, Archive, Verify, Bisect |
| 14 | Helper & Process Dialogs | complete | Claude | 2026-04-21 | 2026-04-21 | Plan D2 — StatusDialog, ChooseCommit, SelectMultipleBranches |
| 15 | Small Git Dialogs | complete | Claude | 2026-04-21 | 2026-04-21 | Plan D3 — AddFiles, GoToCommit, About, Changelog, Updates |
| 16 | Shared Controls | complete | Claude | 2026-04-21 | 2026-04-21 | Plan D2 — StatusOutputLog control |
| 17 | Plugins | complete | Claude | 2026-04-21 | 2026-04-21 | All 11 plugins — Avalonia settings/forms, 0 build errors |

---

## Status Values

- `pending` — not started
- `in-progress` — agent is actively working
- `blocked` — waiting on a dependency
- `review` — done, needs human review
- `complete` — verified and merged

---

## Known Issues & Decisions

_Agents: add rows here when you make a non-obvious decision or hit an issue._

| Task | Issue / Decision | Resolution |
|------|-----------------|------------|
| — | — | — |
