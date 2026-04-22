---
title: GitExtensions Avalonia Mac Port — Bug Fix Design
date: 2026-04-22
status: approved
---

# Design: Fix Avalonia Mac Port (Graph, UI, Crashes)

## Goal

Make the Avalonia Mac port feel like original GitExtensions: continuous graph lines, polished UI, stable button actions.

---

## Problem 1: Segmented Graph Lines

### Root Cause

`GraphRenderer.cs` draws pass-through segments in two separate half-curves that do not share consistent endpoints at row boundaries.

For a segment changing lane from 0 (prev row) to 1 (next row):

- Row N lower half: `(cx=8, midY) → (endX=24, cellHeight)` — ends at x=24
- Row N+1 upper half: `(startX=8, 0) → (cx=24, midY)` — starts at x=8

At the boundary: Row N ends at x=24, Row N+1 starts at x=8 → **visual jump**.

### Fix

For **pass-through segments** (startIdx ≥ 0 AND endIdx ≥ 0), draw a single cubic Bezier:

```
From: (LaneCenterX(startIdx), 0)
To:   (LaneCenterX(endIdx), cellHeight)
CP1:  (LaneCenterX(startIdx), midY)
CP2:  (LaneCenterX(endIdx), midY)
```

This S-curve connects exactly at row boundaries for all lane configurations.

For **terminating segments** (enters from above, ends at commit node):
- Draw from `(LaneCenterX(startIdx), 0)` to `(nodeCenterX, midY)`.

For **originating segments** (starts at commit node, goes down):
- Draw from `(nodeCenterX, midY)` to `(LaneCenterX(endIdx), cellHeight)`.

### Files Changed

- `src/app/GitUI.Avalonia/RevisionGrid/Graph/GraphRenderer.cs`

---

## Problem 2: UI Looks Bad

### Root Cause

- `RevisionDataGrid.axaml`: bare `ListBox`, no styles, 24px row height, no column headers
- `AppTheme.axaml`: lane colors only, no ListBox/row styles
- No ref-label (branch/tag) badges in the commit subject column

### Fix

**AppTheme.axaml** — add:
- `ListBoxItem` style: alternating row background (`SystemChromeLowColor` / transparent), hover (light blue tint), selection (system accent)
- Row height: 26px minimum

**RevisionDataGrid.axaml** — add:
- Column header row (Graph | Subject | Author | Date)
- Increase `GraphCell` Height to 26px
- Subject column: show ref-label badges inline before the subject text

**RevisionRow.cs** — expose `IReadOnlyList<IGitRef> Refs` from `revision.Refs`

### Files Changed

- `src/app/GitUI.Avalonia/AppTheme.axaml`
- `src/app/GitUI.Avalonia/RevisionGrid/RevisionDataGrid.axaml`
- `src/app/GitUI.Avalonia/RevisionGrid/RevisionRow.cs`

---

## Problem 3: Crashes When Pressing Buttons

### Root Causes

1. `MainWindow.axaml.cs` line 89: `new CloneDialog(_module!)` — null-forgiving on nullable `_module`
2. No global unhandled-exception handler → crashes show raw exception dialog
3. `FetchAsync` does not refresh `RevisionGrid` after fetch
4. Fire-and-forget `_ = SomeAsync()` swallows exceptions silently (no status display)

### Fix

**MainWindow.axaml.cs:**
- Change `new CloneDialog(_module!)` → use `ShowModuleDialogAsync` pattern (null-checked)
- Add `RefreshRevisions()` call on RevisionGrid after Fetch completes
- Show git output / error in `StatusLabel` after each operation

**App.axaml.cs:**
- Add `TaskScheduler.UnobservedTaskException` handler → log + show `StatusLabel` error
- Add `AppDomain.CurrentDomain.UnhandledException` handler → show error message box

**RevisionGridControl.axaml.cs:**
- Expose public `RefreshAsync()` method (calls `LoadRevisionsAsync()`)

### Files Changed

- `src/app/GitUI.Avalonia/MainWindow.axaml.cs`
- `src/app/GitUI.Avalonia/App.axaml.cs`
- `src/app/GitUI.Avalonia/RevisionGrid/RevisionGridControl.axaml.cs`

---

## Out of Scope

- Rewriting the 112 dialog AXAML files (too large for one pass)
- Dark mode support
- Toolbar icons / image assets
- Full Windows feature parity

---

## Implementation Order

1. Fix GraphRenderer (most visible, self-contained)
2. Fix crashes (null safety + exception handling)  
3. UI polish (styles, headers, ref labels)
