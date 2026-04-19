# Mac Port — Task Status

Living document. Update your task's row when status changes.

Last updated: 2026-04-19

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
| 0 | Fork & Build System | pending | — | — | — | Creates `gitextensions-mac` fork, changes target framework, new solution file |
| 1 | Avalonia Infrastructure | pending | — | — | — | **Blocks all other tasks** |
| 2 | Core Shell | pending | — | — | — | Depends on Task 1 |
| 3 | Commit Graph | pending | — | — | — | Depends on Task 2. Hardest task. |
| 4 | Commit Details Panel | pending | — | — | — | Depends on Task 3 |
| 5 | Diff & File View | pending | — | — | — | Depends on Task 1 |
| 6 | Commit & Staging | pending | — | — | — | Depends on Task 1 |
| 7 | Branch Operations | pending | — | — | — | Depends on Task 1 |
| 8 | Remote Operations | pending | — | — | — | Depends on Task 1 |
| 9 | Tags, Stash & Reflog | pending | — | — | — | Depends on Task 1 |
| 10 | Settings | pending | — | — | — | Depends on Task 1. Also needs AppSettingsMac. |
| 11 | Submodules & Worktrees | pending | — | — | — | Depends on Task 1 |
| 12 | Conflict Resolution & Patches | pending | — | — | — | Depends on Task 1 |
| 13 | Repo Maintenance Dialogs | pending | — | — | — | Depends on Task 1 |
| 14 | Helper & Process Dialogs | pending | — | — | — | Depends on Task 1 |
| 15 | Small Git Dialogs | pending | — | — | — | Depends on Task 1 |
| 16 | Shared Controls | pending | — | — | — | Depends on Task 1 |
| 17 | Plugins | pending | — | — | — | Depends on Tasks 2–16 |

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
