# Quick Start: Create Your First Chart in 2 Minutes

Goal of this chapter: follow the fastest recommended EasyChart path to complete the loop of **Edit → Export → Use in UI**.

---

## Open the Editor Window

In the Unity menu bar, choose:

- `EasyChart/Library Editor`

You will see a window with sections like a library tree, configuration panels, and a preview area (later chapters explain each area).

---

## Clone a Library (Recommended)

If you want to get started quickly and keep a consistent style, it's recommended to:

- Select an existing Library from the top toolbar (e.g. a built-in Demo library)
- Click **Clone** on the toolbar to create your personal library (e.g. `MyLibrary`)

This way, all subsequent changes happen in your own library, avoiding modifications to the original examples.

---

## Clone a ChartProfile (Recommended)

In the library tree, find a chart (`ChartProfile`) close to what you want, then right-click:

- `Clone`

After cloning, you'll get a new Profile (a "variant" of the original). Select it, and the Inspector on the right will show all editable settings.

---

## Modify Settings and Save

Minimal recommended changes:

- `coordinateSystem`: make sure it matches your intended Series (e.g. Line/Bar/Scatter use `Cartesian2D`)
- `series`: confirm `type` is correct, and fill in `seriesData`
- `axes`: at minimum, make sure X/Y axis types match the meaning of your data

After editing, click the save button on the top toolbar (if your version has it), or let Unity auto-save the asset.

---

## Export to UXML (Reusable in UI Builder)

The recommended workflow is exporting the Profile to a reusable `.uxml`:

- Right-click your Profile in the tree
- Choose `Export to UXML`

The exported UXML will be placed under:

- `Assets/EasyChart/LibraryUxml/` (Mirror/Backup operations also manage exported assets under this root)

---

## Use It in the QuickStart Scene via UIDocument + UI Builder

Open the demo scene:

- `Assets/EasyChart/Demo/Scenes/EasyChart_QuickStart.unity`

Then in the Project window, locate:

- `Assets/EasyChart/Demo/UIToolKit/NewUXMLTemplate.uxml`

Double-click to open it (or open with UI Builder). Next:

- Drag the chart `.uxml` you just exported into the hierarchy of `NewUXMLTemplate.uxml`
- Save the UXML
- Make sure the `UIDocument` in the scene references your updated `NewUXMLTemplate.uxml`

Run the scene, and you should see the chart rendered in the UI Toolkit UI.

---

## Alternative: Export as a UGUI Prefab

If you prefer a UGUI (Canvas/RectTransform) workflow, you can also export the selected Profile as a UGUI prefab in the Library Editor and place it directly into your scene UI (exact menu entry and details depend on your current version).

---

## What to Read Next

- To understand the recommended UI Toolkit workflow: `00_02-WorkflowAndLibrary.md`
- To use charts with UGUI (Canvas/RectTransform): `00_03-UGUIWorkflow.md`

