# Tasks 4–17 Summary

Tasks 4–17 all follow the same pattern as Tasks 2–3. Each agent should:
1. Read their task row in `TASK-STATUS.md`
2. Read the source files listed in the design spec (`docs/superpowers/specs/2026-04-19-mac-port-design.md`, Section 6)
3. Follow `AGENT-GUIDE.md` and `WINFORMS-TO-AVALONIA.md`
4. Create Avalonia equivalents in `src/app/GitUI.Avalonia/`
5. Verify done criteria and update `TASK-STATUS.md`

Individual task plan files will be created per-task when that task is ready to start. The full source/target file mapping for each task is in the design spec Section 6.

## Quick reference

| Task | Description | Source section in design spec | Depends on |
|------|-------------|-------------------------------|------------|
| 4 | Commit Details Panel | Section 6, Task 4 | Task 3 |
| 5 | Diff & File View | Section 6, Task 5 | Task 1 |
| 6 | Commit & Staging | Section 6, Task 6 | Task 1 |
| 7 | Branch Operations | Section 6, Task 7 | Task 1 |
| 8 | Remote Operations | Section 6, Task 8 | Task 1 |
| 9 | Tags, Stash & Reflog | Section 6, Task 9 | Task 1 |
| 10 | Settings | Section 6, Task 10 | Task 1 |
| 11 | Submodules & Worktrees | Section 6, Task 11 | Task 1 |
| 12 | Conflict Resolution & Patches | Section 6, Task 12 | Task 1 |
| 13 | Repo Maintenance Dialogs | Section 6, Task 13 | Task 1 |
| 14 | Helper & Process Dialogs | Section 6, Task 14 | Task 1 |
| 15 | Small Git Dialogs | Section 6, Task 15 | Task 1 |
| 16 | Shared Controls | Section 6, Task 16 | Task 1 |
| 17 | Plugins | Section 6, Task 17 | Tasks 2–16 |

## Done criteria (applies to all tasks 4–16)

Every task is done when:
- [ ] All listed screens/dialogs render and show real data
- [ ] All interactive operations work (buttons trigger git commands, data refreshes)
- [ ] Zero WinForms references: `grep -r "System.Windows.Forms" src/app/GitUI.Avalonia/` returns empty
- [ ] `dotnet build GitExtensions.Mac.slnx` succeeds
- [ ] `TASK-STATUS.md` updated to `complete`
