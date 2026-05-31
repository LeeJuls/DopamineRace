# Library Panel (Asset Tree)

This chapter explains the **Library** panel on the left side of `Unity Easy Chart/Library Editor`. It manages your chart assets (`ChartProfile`) and folder structure, and determines which Profile the panels on the right are currently editing.

---

## Feature Overview

- **Structure Display**: shows folders and `ChartProfile` (`.asset`) under the library root in a tree.
- **Selection Drives Editing**: selecting a `ChartProfile` triggers:
  - Inspector binds to that Profile
  - Series binds to that Profile `series`
  - Preview refresh
  - JSON Injection generates example JSON (and can Apply back to that Profile)
- **Asset Management**: create/rename/delete, drag-move, expand/collapse, etc.

---

## UI Structure

The header bar at the top of the Library panel typically contains (left to right):

- **Library**: title.
- **(Current Library Name)**: shows the selected library/root name (useful if you have multiple libraries).
- **Menu**: entry for common actions (similar to right-click, but centralized).
- **Help**: opens this chapter.

In the global window toolbar at the top, on the right side of the Library dropdown you may see:

- **+**: add a new Library.
- **-**: delete the current Library.
- **Clone**: duplicate the current Library (see below).

Below the header is:

- **TreeView**:
  - folders
  - `ChartProfile` assets (chart configuration assets)

---

## Selection Logic (Important)

- **When a Folder is Selected**:
  - Inspector/Series on the right are cleared (no Profile to edit).
  - JSON Injection switches to a "no selected Profile" example or keeps the current example (implementation-dependent).
- **When a ChartProfile is Selected**:
  - Inspector/Series immediately bind to the Profile's serialized data.
  - Any field change triggers a delayed Preview refresh (`delayCall`).

Tip: if the right panels are empty, first confirm a `ChartProfile` (not a folder) is selected on the left.

---

## Common Actions (Header Menu)

Clicking **Menu** (the small menu icon) opens an action menu. Common items include:

- **New Chart**: create a new `ChartProfile` under the target folder.
- **New Folder**: create a new folder under the target folder.
- **Refresh**: rescan and rebuild the tree (useful after manual moves/copies in the Project view).
- **Expand All**: expand all folders.
- **Collapse All**: collapse all folders.
- **Rename / Delete**: rename/delete the currently selected item.
  - If the library root is selected, these are typically disabled.
- **Export UGUI Prefab** (available when a Profile is selected): export the selected Profile as a UGUI prefab (for runtime display).

### How the Target Folder is Determined

**New Chart / New Folder** create assets under the "target folder":

- If a **folder** is selected: the target is that folder.
- If a **ChartProfile** is selected: the target is usually the Profile's parent folder.
- If nothing is selected or unclear: the target usually falls back to the library root.

---

## Clone Library

When you need to duplicate a full chart library (including Profiles and UXML) into a new library (for branching/versions/theme variants), use **Clone** in the top toolbar.

### Entry and Usage

- Click the **Clone** icon to the right of the Library dropdown.
- Enter the new library name and confirm.

### What Gets Cloned

- `Assets/EasyChart/Library/<CurrentLibrary>` is copied to `Assets/EasyChart/Library/<NewLibrary>`.
- `Assets/EasyChart/LibraryUxml/<CurrentLibrary>` is copied to `Assets/EasyChart/LibraryUxml/<NewLibrary>` (if the source UXML folder exists).

### Limitations and Naming Rules

- The `<Root>` library cannot be cloned.
- The new name is sanitized (invalid filename characters removed); blank names are ignored.
- If the target library already exists (folder already exists), it will prompt and cancel.

### After Cloning

- Automatically switches the current Library selection to the new library.
- Refreshes the Library dropdown and tree view, and triggers refresh for right panels/preview.

---

## Common Actions (Context Menu)

You can also **right-click** items in the tree:

### Right-click a Folder

- **New Folder...**: create a subfolder.
- **New Chart...**: create a new `ChartProfile` in that folder.
- **Export Folder to UXML (Mirror/Backup)**: export the folder contents to UXML (backup/distribution/versioning).
- **Rename...**: rename the folder.
- **Delete**: delete the folder (destructive; be careful).

### Right-click a ChartProfile

- **Export to UXML**: export UXML for the current Profile.
- **Clone**: clone a new Profile (quickly derive a similar chart).
- **Rename...**: rename the asset (also tries to sync-update `profile.name` / `profile.chartName`).
- **Ping**: locate the asset in the Project view.
- **Delete**: delete the asset.

---

## Drag-move and Sorting

The Library tree supports dragging folders or `ChartProfile`:

- **Drag a ChartProfile onto a folder**: triggers `AssetDatabase.MoveAsset` to move the `.asset` into the target folder.
- **Drag a folder onto a folder**: moves the whole folder under the target folder.

Notes:

- If the target is invalid (dragging into itself/child folder), it will be rejected (cursor shows Rejected).
- After moving, the tree refreshes automatically.

---

## Rename (Double-click and Inline Editing)

**Double-clicking** a tree item enters inline rename (equivalent to running Rename).

Internally, names are sanitized (invalid filename characters removed). If the new name is blank or unchanged, rename is canceled.

---

## Common Issues & Troubleshooting

- **Right panels are empty**:
  - First confirm a `ChartProfile` (not a folder) is selected.
- **Renamed but chartName didn't update**:
  - ChartProfile may have additional sync logic; verify the `Chart Name` field in Inspector.
- **Drag failed**:
  - Common causes: dragging onto itself/child folder, or a name conflict at target path.

---

## Help

- Click the rightmost **Help** icon in the header to open this chapter.
