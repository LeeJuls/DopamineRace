using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using EasyChart;

namespace EasyChart.Editor
{
    public partial class EasyChartLibraryWindow
    {
        private void OnTreeItemContextClick(ContextClickEvent evt)
        {
            var el = evt.currentTarget as VisualElement;
            var path = el?.userData as string;
            if (string.IsNullOrEmpty(path)) return;

            var menu = new GenericMenu();
            bool isFolder = AssetDatabase.IsValidFolder(path);
            if (isFolder)
            {
                menu.AddItem(new GUIContent("New Folder..."), false, () => CreateNewFolderUnder(path));
                menu.AddItem(new GUIContent("New Chart..."), false, () => CreateNewProfileInFolder(path));
                menu.AddSeparator(string.Empty);

                if (HasProfileInClipboard()) menu.AddItem(new GUIContent("Paste (As New)"), false, () => PasteProfileAsNewFromClipboard(path));
                else menu.AddDisabledItem(new GUIContent("Paste (As New)"));

                menu.AddSeparator(string.Empty);
                menu.AddItem(new GUIContent("Export Folder to UXML (Mirror)"), false, () => ExportFolderToUxmlMirror(path));
                menu.AddItem(new GUIContent("Export Folder to UXML (Backup)"), false, () => ExportFolderToUxmlMirrorBackup(path));
                menu.AddItem(new GUIContent("Rename..."), false, () => RenameAssetOrFolder(path));
                menu.AddItem(new GUIContent("Delete"), false, () => DeleteAssetOrFolder(path));
            }
            else
            {
                bool isProfile = IsChartProfileAsset(path);
                if (isProfile) menu.AddItem(new GUIContent("Copy"), false, () => CopyProfileToClipboard(path));
                else menu.AddDisabledItem(new GUIContent("Copy"));

                if (isProfile && HasProfileInClipboard()) menu.AddItem(new GUIContent("Paste (Overwrite)"), false, () => PasteProfileOverwriteFromClipboard(path));
                else menu.AddDisabledItem(new GUIContent("Paste (Overwrite)"));

                string folder = Path.GetDirectoryName(path)?.Replace('\\', '/');
                if (HasProfileInClipboard() && !string.IsNullOrEmpty(folder) && AssetDatabase.IsValidFolder(folder))
                    menu.AddItem(new GUIContent("Paste (As New)"), false, () => PasteProfileAsNewFromClipboard(folder));
                else
                    menu.AddDisabledItem(new GUIContent("Paste (As New)"));

                menu.AddSeparator(string.Empty);
                menu.AddItem(new GUIContent("Export to UXML"), false, () =>
                {
                    var profile = AssetDatabase.LoadAssetAtPath<ChartProfile>(path);
                    if (profile != null)
                    {
                        _selectedProfile = profile;
                        ExportToUxml();
                    }
                });
                if (isProfile) menu.AddItem(new GUIContent("Clone"), false, () => CloneProfile(path));
                else menu.AddDisabledItem(new GUIContent("Clone"));
                menu.AddSeparator(string.Empty);
                menu.AddItem(new GUIContent("Rename..."), false, () => RenameAssetOrFolder(path));
                menu.AddItem(new GUIContent("Ping"), false, () =>
                {
                    var obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
                    if (obj != null) EditorGUIUtility.PingObject(obj);
                });
                menu.AddItem(new GUIContent("Delete"), false, () => DeleteAssetOrFolder(path));
            }

            menu.ShowAsContext();
            evt.StopPropagation();
        }

        private void OnTreeDragUpdated(DragUpdatedEvent evt)
        {
            var folderPath = TryGetDropTargetFolderUnderTreeMouse(evt.localMousePosition);
            var draggedPath = GetDraggedChartAssetPath();

            bool canDrop = false;
            if (!string.IsNullOrEmpty(folderPath) && AssetDatabase.IsValidFolder(folderPath) && !string.IsNullOrEmpty(draggedPath))
            {
                if (AssetDatabase.IsValidFolder(draggedPath))
                {
                    if (!string.Equals(folderPath, draggedPath, StringComparison.OrdinalIgnoreCase) &&
                        !IsSubFolderOf(folderPath, draggedPath))
                    {
                        canDrop = true;
                    }
                }
                else
                {
                    string srcFolder = Path.GetDirectoryName(draggedPath)?.Replace("\\", "/");
                    canDrop = string.IsNullOrEmpty(srcFolder) || !string.Equals(srcFolder, folderPath, StringComparison.OrdinalIgnoreCase);
                }
            }

            DragAndDrop.visualMode = canDrop ? DragAndDropVisualMode.Move : DragAndDropVisualMode.Rejected;
            evt.StopPropagation();
        }

        private void OnTreeDragPerform(DragPerformEvent evt)
        {
            var folderPath = TryGetDropTargetFolderUnderTreeMouse(evt.localMousePosition);
            var draggedPaths = GetAllDraggedAssetPaths();
            if (string.IsNullOrEmpty(folderPath) || !AssetDatabase.IsValidFolder(folderPath)) return;
            if (draggedPaths.Count == 0) return;

            DragAndDrop.AcceptDrag();
            
            bool anyMoved = false;
            var errors = new List<string>();

            foreach (var draggedPath in draggedPaths)
            {
                if (string.IsNullOrEmpty(draggedPath)) continue;

                if (AssetDatabase.IsValidFolder(draggedPath))
                {
                    if (string.Equals(folderPath, draggedPath, StringComparison.OrdinalIgnoreCase)) continue;
                    if (IsSubFolderOf(folderPath, draggedPath)) continue;

                    string folderName = Path.GetFileName(draggedPath);
                    if (string.IsNullOrEmpty(folderName)) continue;

                    string destFolderPath = $"{folderPath}/{folderName}";
                    if (string.Equals(destFolderPath, draggedPath, StringComparison.OrdinalIgnoreCase)) continue;
                    if (AssetPathExists(destFolderPath)) continue;

                    string err = AssetDatabase.MoveAsset(draggedPath, destFolderPath);
                    if (!string.IsNullOrEmpty(err))
                    {
                        errors.Add(err);
                    }
                    else
                    {
                        anyMoved = true;
                    }
                }
                else
                {
                    string srcFolder = Path.GetDirectoryName(draggedPath)?.Replace("\\", "/");
                    if (!string.IsNullOrEmpty(srcFolder) && AssetDatabase.IsValidFolder(srcFolder) && 
                        string.Equals(srcFolder, folderPath, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    string fileName = Path.GetFileName(draggedPath);
                    if (string.IsNullOrEmpty(fileName)) continue;

                    string destPath = $"{folderPath}/{fileName}";
                    if (string.Equals(destPath, draggedPath, StringComparison.OrdinalIgnoreCase)) continue;
                    if (AssetPathExists(destPath)) continue;

                    string err = AssetDatabase.MoveAsset(draggedPath, destPath);
                    if (!string.IsNullOrEmpty(err))
                    {
                        errors.Add(err);
                    }
                    else
                    {
                        anyMoved = true;
                    }
                }
            }

            if (errors.Count > 0)
            {
                EditorUtility.DisplayDialog("Error", string.Join("\n", errors), "OK");
            }

            if (anyMoved)
            {
                AssetDatabase.SaveAssets();
                RefreshTree();
            }
            
            evt.StopPropagation();
        }

        private string TryGetDropTargetFolderUnderTreeMouse(Vector2 treeLocalMousePosition)
        {
            if (_folderTree == null) return null;
            if (_folderTree.panel == null) return null;

            Vector2 world = _folderTree.LocalToWorld(treeLocalMousePosition);
            var picked = _folderTree.panel.Pick(world);
            while (picked != null)
            {
                var path = picked.userData as string;
                if (!string.IsNullOrEmpty(path))
                {
                    if (AssetDatabase.IsValidFolder(path))
                    {
                        return path;
                    }

                    var folder = Path.GetDirectoryName(path)?.Replace('\\', '/');
                    if (!string.IsNullOrEmpty(folder) && AssetDatabase.IsValidFolder(folder))
                    {
                        return folder;
                    }
                    return null;
                }
                picked = picked.parent;
            }

            if (_folderTree.contentRect.Contains(treeLocalMousePosition))
            {
                return GetActiveProfileRootPath();
            }

            return null;
        }

        private static string GetDraggedChartAssetPath()
        {
            var paths = GetAllDraggedAssetPaths();
            return paths.Count > 0 ? paths[0] : null;
        }

        private static List<string> GetAllDraggedAssetPaths()
        {
            var result = new List<string>();
            
            Debug.Log($"[EasyChart] GetAllDraggedAssetPaths: objectReferences={DragAndDrop.objectReferences?.Length ?? 0}, paths={DragAndDrop.paths?.Length ?? 0}");
            
            // Check generic data first (single item drag from tree) - but only if no multi-select
            var draggedPath = DragAndDrop.GetGenericData(DragDataKey) as string;
            bool hasMultipleObjects = DragAndDrop.objectReferences != null && DragAndDrop.objectReferences.Length > 1;
            
            if (!string.IsNullOrEmpty(draggedPath) && !hasMultipleObjects)
            {
                Debug.Log($"[EasyChart] Found generic data (single): {draggedPath}");
                result.Add(draggedPath);
                return result;
            }

            // Check object references (multi-select from Unity)
            if (DragAndDrop.objectReferences != null)
            {
                Debug.Log($"[EasyChart] Checking objectReferences: {DragAndDrop.objectReferences.Length} items");
                for (int i = 0; i < DragAndDrop.objectReferences.Length; i++)
                {
                    var obj = DragAndDrop.objectReferences[i];
                    Debug.Log($"[EasyChart] objectReferences[{i}]: {obj?.GetType().Name ?? "null"} - {obj?.name ?? "null"}");
                    if (obj == null) continue;
                    if (obj is ChartProfile)
                    {
                        var p = AssetDatabase.GetAssetPath(obj);
                        if (!string.IsNullOrEmpty(p) && !result.Contains(p))
                        {
                            result.Add(p);
                        }
                    }
                }
                
                if (result.Count > 0) return result;

                // Fallback: check all .asset files
                for (int i = 0; i < DragAndDrop.objectReferences.Length; i++)
                {
                    var obj = DragAndDrop.objectReferences[i];
                    if (obj == null) continue;
                    var p = AssetDatabase.GetAssetPath(obj);
                    if (!string.IsNullOrEmpty(p) && p.EndsWith(".asset", StringComparison.OrdinalIgnoreCase))
                    {
                        if (!result.Contains(p)) result.Add(p);
                    }
                }
                
                if (result.Count > 0) return result;
            }

            // Check paths (folders or assets)
            if (DragAndDrop.paths != null && DragAndDrop.paths.Length > 0)
            {
                for (int i = 0; i < DragAndDrop.paths.Length; i++)
                {
                    var p = DragAndDrop.paths[i];
                    if (string.IsNullOrEmpty(p)) continue;
                    
                    if (AssetDatabase.IsValidFolder(p))
                    {
                        if (!result.Contains(p)) result.Add(p);
                    }
                    else if (p.EndsWith(".asset", StringComparison.OrdinalIgnoreCase))
                    {
                        var profile = AssetDatabase.LoadAssetAtPath<ChartProfile>(p);
                        if (profile != null && !result.Contains(p))
                        {
                            result.Add(p);
                        }
                    }
                }
            }

            return result;
        }

        private void OnTreePointerDown(PointerDownEvent evt)
        {
            if (evt.button != 0) return;
            if (_folderTree == null || _folderTree.panel == null) return;

            var pos2 = new Vector2(evt.localPosition.x, evt.localPosition.y);
            var path = TryGetPathUnderTreeMouse(pos2);
            if (string.IsNullOrEmpty(path)) return;

            if (evt.clickCount == 2)
            {
                ClearDragState();
                RenameAssetOrFolder(path);
                evt.StopPropagation();
                evt.PreventDefault();
                return;
            }

            if (!string.IsNullOrEmpty(_inlineRenamePath))
            {
                return;
            }

            _dragCandidateAssetPath = path;
            _dragStartPos = pos2;
            _dragPointerId = evt.pointerId;
            _dragInProgress = false;

            if (!_folderTree.HasPointerCapture(evt.pointerId))
            {
                _folderTree.CapturePointer(evt.pointerId);
            }
        }

        private void OnTreePointerMove(PointerMoveEvent evt)
        {
            if (!string.IsNullOrEmpty(_inlineRenamePath)) return;
            if (_dragPointerId != evt.pointerId) return;
            if (string.IsNullOrEmpty(_dragCandidateAssetPath)) return;
            if (_dragInProgress) return;

            var pos2 = new Vector2(evt.localPosition.x, evt.localPosition.y);
            float sqrDist = (pos2 - _dragStartPos).sqrMagnitude;
            if (sqrDist < DragStartThreshold * DragStartThreshold) return;

            // Collect all selected items for multi-select drag
            var selectedPaths = new List<string>();
            var selectedObjects = new List<UnityEngine.Object>();
            
            if (_folderTree != null && _folderTree.selectedItems != null)
            {
                foreach (var item in _folderTree.selectedItems)
                {
                    var itemPath = item as string;
                    if (!string.IsNullOrEmpty(itemPath))
                    {
                        var itemObj = AssetDatabase.LoadMainAssetAtPath(itemPath);
                        if (itemObj != null)
                        {
                            selectedPaths.Add(itemPath);
                            selectedObjects.Add(itemObj);
                        }
                    }
                }
            }
            
            // If no selection or current drag item not in selection, use single item
            if (selectedPaths.Count == 0 || !selectedPaths.Contains(_dragCandidateAssetPath))
            {
                var obj = AssetDatabase.LoadMainAssetAtPath(_dragCandidateAssetPath);
                if (obj == null)
                {
                    ClearDragState();
                    return;
                }
                selectedPaths.Clear();
                selectedObjects.Clear();
                selectedPaths.Add(_dragCandidateAssetPath);
                selectedObjects.Add(obj);
            }

            DragAndDrop.PrepareStartDrag();
            DragAndDrop.objectReferences = selectedObjects.ToArray();
            DragAndDrop.paths = selectedPaths.ToArray();
            // Only set generic data for single item (backward compatibility)
            if (selectedPaths.Count == 1)
            {
                DragAndDrop.SetGenericData(DragDataKey, selectedPaths[0]);
            }
            DragAndDrop.visualMode = DragAndDropVisualMode.Move;
            string dragLabel = selectedPaths.Count > 1 
                ? $"{selectedPaths.Count} items"
                : (AssetDatabase.IsValidFolder(_dragCandidateAssetPath)
                    ? Path.GetFileName(_dragCandidateAssetPath)
                    : Path.GetFileNameWithoutExtension(_dragCandidateAssetPath));
            DragAndDrop.StartDrag(dragLabel);
            _dragInProgress = true;
        }

        private void OnTreePointerUp(PointerUpEvent evt)
        {
            if (_folderTree != null && _folderTree.HasPointerCapture(evt.pointerId))
            {
                _folderTree.ReleasePointer(evt.pointerId);
            }
            ClearDragState();
        }

        private string TryGetPathUnderTreeMouse(Vector2 treeLocalMousePosition)
        {
            if (_folderTree == null) return null;
            if (_folderTree.panel == null) return null;

            Vector2 world = _folderTree.LocalToWorld(treeLocalMousePosition);
            var picked = _folderTree.panel.Pick(world);
            while (picked != null)
            {
                var path = picked.userData as string;
                if (!string.IsNullOrEmpty(path))
                {
                    return path;
                }
                picked = picked.parent;
            }
            return null;
        }

        private void ClearDragState()
        {
            _dragCandidateAssetPath = null;
            _dragPointerId = -1;
            _dragInProgress = false;
        }

        private void OnTreeKeyDown(KeyDownEvent evt)
        {
            if (_folderTree == null) return;
            
            var selectedItems = _folderTree.selectedItems;
            if (selectedItems == null) return;
            
            string selectedPath = null;
            foreach (var item in selectedItems)
            {
                selectedPath = item as string;
                break;
            }
            
            if (string.IsNullOrEmpty(selectedPath)) return;
            
            // Check if this is the root path (cannot delete/clone root)
            bool canOperateOnSelection = !string.Equals(selectedPath, GetActiveProfileRootPath(), StringComparison.OrdinalIgnoreCase);
            bool isProfileSelected = selectedPath.EndsWith(".asset") && AssetDatabase.LoadAssetAtPath<ChartProfile>(selectedPath) != null;
            
            if (evt.keyCode == KeyCode.F2)
            {
                // F2 to rename selected item (like Windows Explorer)
                if (canOperateOnSelection)
                {
                    RenameAssetOrFolder(selectedPath);
                    evt.StopPropagation();
                    evt.PreventDefault();
                }
            }
            else if (evt.keyCode == KeyCode.Delete)
            {
                // Delete key to delete selected item
                if (canOperateOnSelection)
                {
                    DeleteAssetOrFolderWithConfirmation(selectedPath);
                    evt.StopPropagation();
                    evt.PreventDefault();
                }
            }
            else if (evt.keyCode == KeyCode.D && evt.ctrlKey)
            {
                // Ctrl+D to clone selected profile
                if (isProfileSelected)
                {
                    CloneProfile(selectedPath);
                    evt.StopPropagation();
                    evt.PreventDefault();
                }
            }
        }
        
        private void DeleteAssetOrFolderWithConfirmation(string path)
        {
            if (string.IsNullOrEmpty(path)) return;
            
            string name = System.IO.Path.GetFileNameWithoutExtension(path);
            bool isFolder = AssetDatabase.IsValidFolder(path);
            string itemType = isFolder ? "folder" : "profile";
            
            // Show confirmation dialog that can be confirmed with Enter key
            bool confirmed = EditorUtility.DisplayDialog(
                "Delete " + itemType,
                $"Are you sure you want to delete '{name}'?\n\nThis cannot be undone.",
                "Delete",
                "Cancel");
            
            if (confirmed)
            {
                // Find sibling to select after deletion
                string siblingToSelect = GetSiblingPath(path);
                
                DeleteAssetOrFolderInternal(path, siblingToSelect);
            }
        }
        
        private string GetSiblingPath(string path)
        {
            if (string.IsNullOrEmpty(path)) return null;
            
            string parentFolder = System.IO.Path.GetDirectoryName(path)?.Replace("\\", "/");
            if (string.IsNullOrEmpty(parentFolder)) return null;
            
            // Get all siblings in the same folder
            var siblings = new List<string>();
            
            // Add subfolders
            var subFolders = AssetDatabase.GetSubFolders(parentFolder);
            if (subFolders != null) siblings.AddRange(subFolders);
            
            // Add assets
            var guids = AssetDatabase.FindAssets("t:ChartProfile", new[] { parentFolder });
            foreach (var guid in guids)
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(guid);
                // Only include direct children
                if (System.IO.Path.GetDirectoryName(assetPath)?.Replace("\\", "/") == parentFolder)
                {
                    siblings.Add(assetPath);
                }
            }
            
            // Sort siblings using natural sort
            siblings.Sort((a, b) => NaturalCompare(Path.GetFileName(a), Path.GetFileName(b)));
            
            // Find current index
            int currentIndex = siblings.IndexOf(path);
            if (currentIndex < 0) return parentFolder; // Fallback to parent
            
            // Return next sibling, or previous if at end, or parent if only child
            if (siblings.Count <= 1) return parentFolder;
            if (currentIndex < siblings.Count - 1) return siblings[currentIndex + 1];
            return siblings[currentIndex - 1];
        }
        
        private void DeleteAssetOrFolderInternal(string path, string selectAfterDelete = null)
        {
            if (string.IsNullOrEmpty(path)) return;

            bool ok = AssetDatabase.DeleteAsset(path);
            if (!ok)
            {
                EditorUtility.DisplayDialog("Error", "Delete failed.", "OK");
                return;
            }

            AssetDatabase.SaveAssets();
            RefreshTree(selectAfterDelete);
        }
        
        private int FindTreeItemId(IEnumerable<TreeViewItemData<string>> items, string targetPath)
        {
            if (items == null || string.IsNullOrEmpty(targetPath)) return -1;
            
            foreach (var item in items)
            {
                if (string.Equals(item.data, targetPath, StringComparison.OrdinalIgnoreCase))
                {
                    return item.id;
                }
                if (item.children != null)
                {
                    int childId = FindTreeItemId(item.children, targetPath);
                    if (childId >= 0) return childId;
                }
            }
            return -1;
        }

        private void RenameAssetOrFolder(string path)
        {
            BeginInlineRename(path);
        }

        private void BeginInlineRename(string path)
        {
            if (string.IsNullOrEmpty(path)) return;
            _inlineRenamePath = path;
            _inlineRenamePendingFocusPath = path;
            _inlineRenameField = null;
            if (_folderTree != null) _folderTree.Rebuild();
        }

        private void CancelInlineRename()
        {
            _inlineRenamePath = null;
            _inlineRenamePendingFocusPath = null;
            _inlineRenameField = null;
            if (_folderTree != null) _folderTree.Rebuild();
        }

        private void CommitInlineRename(string path, string newName)
        {
            if (string.IsNullOrEmpty(path))
            {
                CancelInlineRename();
                return;
            }

            string currentName = AssetDatabase.IsValidFolder(path)
                ? Path.GetFileName(path)
                : Path.GetFileNameWithoutExtension(path);

            newName = SanitizeFileName(newName);
            if (string.IsNullOrWhiteSpace(newName) || newName == currentName)
            {
                CancelInlineRename();
                return;
            }

            string ext = Path.GetExtension(path);
            string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            string renamedPath = string.IsNullOrEmpty(parent) ? null : $"{parent}/{newName}{ext}";

            string err = AssetDatabase.RenameAsset(path, newName);
            if (!string.IsNullOrEmpty(err))
            {
                EditorUtility.DisplayDialog("Error", err, "OK");
                CancelInlineRename();
                return;
            }

            if (!string.IsNullOrEmpty(renamedPath) && ext.Equals(".asset", StringComparison.OrdinalIgnoreCase))
            {
                var profile = AssetDatabase.LoadAssetAtPath<ChartProfile>(renamedPath);
                if (profile != null)
                {
                    if (!string.Equals(profile.name, newName, StringComparison.Ordinal))
                    {
                        profile.name = newName;
                        EditorUtility.SetDirty(profile);
                    }

                    if (!string.Equals(profile.chartName, newName, StringComparison.Ordinal))
                    {
                        profile.chartName = newName;
                        EditorUtility.SetDirty(profile);
                    }
                }
            }

            _inlineRenamePath = null;
            _inlineRenamePendingFocusPath = null;
            _inlineRenameField = null;
            AssetDatabase.SaveAssets();
            RefreshTree();
        }

        private void OnRootPointerDownForInlineRename(PointerDownEvent evt)
        {
            if (evt.button != 0) return;
            if (string.IsNullOrEmpty(_inlineRenamePath)) return;
            if (_inlineRenameField == null) return;

            if (evt.target is VisualElement target && _inlineRenameField.Contains(target))
            {
                return;
            }

            string path = _inlineRenameField.userData as string;
            string value = _inlineRenameField.value;

            rootVisualElement.schedule.Execute(() =>
            {
                if (!string.IsNullOrEmpty(_inlineRenamePath))
                {
                    CommitInlineRename(path, value);
                }
            });
        }

        private void RefreshTree(string selectPathAfterRefresh = null)
        {
            EnsureSharedIconsLoaded();

            var expandedFolderPaths = CaptureExpandedFolderPaths();
            EnsureActiveLibraryFolders();

            int idCounter = 0;
            _treeRoots = BuildTreeRecursive(GetActiveProfileRootPath(), ref idCounter);
            _folderTree.SetRootItems(_treeRoots);
            _folderTree.Rebuild();

            RestoreExpandedFolderPaths(expandedFolderPaths);
            
            // Select specified path after refresh
            if (!string.IsNullOrEmpty(selectPathAfterRefresh) && _treeRoots != null)
            {
                int targetId = FindTreeItemId(_treeRoots, selectPathAfterRefresh);
                if (targetId >= 0)
                {
                    _folderTree.SetSelection(new[] { targetId });
                }
            }
        }

        private HashSet<string> CaptureExpandedFolderPaths()
        {
            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (_folderTree == null) return set;
            if (_treeRoots == null) return set;

            CaptureExpandedFolderPathsRecursive(_treeRoots, set);
            return set;
        }

        private void CaptureExpandedFolderPathsRecursive(IEnumerable<TreeViewItemData<string>> items, HashSet<string> set)
        {
            if (items == null) return;
            foreach (var item in items)
            {
                var path = item.data;
                if (!string.IsNullOrEmpty(path) && AssetDatabase.IsValidFolder(path) && _folderTree.IsExpanded(item.id))
                {
                    set.Add(path);
                }

                if (item.children != null && item.children.Any())
                {
                    CaptureExpandedFolderPathsRecursive(item.children, set);
                }
            }
        }

        private void RestoreExpandedFolderPaths(HashSet<string> expandedFolderPaths)
        {
            if (_folderTree == null) return;
            if (expandedFolderPaths == null || expandedFolderPaths.Count == 0) return;
            if (_treeRoots == null) return;

            RestoreExpandedFolderPathsRecursive(_treeRoots, expandedFolderPaths);
        }

        private void ExpandAllFolders()
        {
            if (_folderTree == null) return;
            if (_treeRoots == null) return;
            ExpandAllFoldersRecursive(_treeRoots);
        }

        private void ExpandAllFoldersRecursive(IEnumerable<TreeViewItemData<string>> items)
        {
            if (items == null) return;
            foreach (var item in items)
            {
                var path = item.data;
                if (!string.IsNullOrEmpty(path) && AssetDatabase.IsValidFolder(path))
                {
                    _folderTree.ExpandItem(item.id);
                }

                if (item.children != null && item.children.Any())
                {
                    ExpandAllFoldersRecursive(item.children);
                }
            }
        }

        private void CollapseAllFolders()
        {
            if (_folderTree == null) return;
            if (_treeRoots == null) return;
            CollapseAllFoldersRecursive(_treeRoots);
        }

        private void CollapseAllFoldersRecursive(IEnumerable<TreeViewItemData<string>> items)
        {
            if (items == null) return;
            foreach (var item in items)
            {
                var path = item.data;
                if (!string.IsNullOrEmpty(path) && AssetDatabase.IsValidFolder(path))
                {
                    _folderTree.CollapseItem(item.id);
                }

                if (item.children != null && item.children.Any())
                {
                    CollapseAllFoldersRecursive(item.children);
                }
            }
        }

        private void RestoreExpandedFolderPathsRecursive(IEnumerable<TreeViewItemData<string>> items, HashSet<string> expandedFolderPaths)
        {
            if (items == null) return;
            foreach (var item in items)
            {
                var path = item.data;
                if (!string.IsNullOrEmpty(path) && AssetDatabase.IsValidFolder(path) && expandedFolderPaths.Contains(path))
                {
                    _folderTree.ExpandItem(item.id);
                }

                if (item.children != null && item.children.Any())
                {
                    RestoreExpandedFolderPathsRecursive(item.children, expandedFolderPaths);
                }
            }
        }

        private static bool IsSubFolderOf(string candidateFolder, string parentFolder)
        {
            if (string.IsNullOrEmpty(candidateFolder) || string.IsNullOrEmpty(parentFolder)) return false;
            string candidate = candidateFolder.Replace('\\', '/').TrimEnd('/') + "/";
            string parent = parentFolder.Replace('\\', '/').TrimEnd('/') + "/";
            return candidate.StartsWith(parent, StringComparison.OrdinalIgnoreCase);
        }

        private List<TreeViewItemData<string>> BuildTreeRecursive(string path, ref int idCounter)
        {
            var items = new List<TreeViewItemData<string>>();

            string[] dirs = Directory.GetDirectories(path);
            Array.Sort(dirs, (a, b) => NaturalCompare(Path.GetFileName(a), Path.GetFileName(b)));
            foreach (var dir in dirs)
            {
                string unityPath = dir.Replace("\\", "/");
                var children = BuildTreeRecursive(unityPath, ref idCounter);
                var item = new TreeViewItemData<string>(idCounter++, unityPath, children);
                items.Add(item);
            }

            var orderedFiles = GetProfilesInFolderSortedByName(path);
            for (int i = 0; i < orderedFiles.Count; i++)
            {
                var file = orderedFiles[i];
                var asset = AssetDatabase.LoadAssetAtPath<ChartProfile>(file);
                if (asset != null)
                {
                    items.Add(new TreeViewItemData<string>(idCounter++, file));
                }
            }

            return items;
        }

        private List<string> GetProfilesInFolder(string folderPath)
        {
            var list = new List<string>();
            if (string.IsNullOrEmpty(folderPath)) return list;
            if (!AssetDatabase.IsValidFolder(folderPath)) return list;

            string[] files;
            try
            {
                files = Directory.GetFiles(folderPath, "*.asset");
            }
            catch
            {
                return list;
            }

            for (int i = 0; i < files.Length; i++)
            {
                var file = files[i].Replace('\\', '/');
                var asset = AssetDatabase.LoadAssetAtPath<ChartProfile>(file);
                if (asset != null)
                {
                    list.Add(file);
                }
            }
            return list;
        }

        private List<string> GetProfilesInFolderSortedByName(string folderPath)
        {
            var list = GetProfilesInFolder(folderPath);
            list.Sort((a, b) => NaturalCompare(GetProfileDisplayNameForSort(a), GetProfileDisplayNameForSort(b)));
            return list;
        }

        private static string GetProfileDisplayNameForSort(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath)) return string.Empty;
            return Path.GetFileNameWithoutExtension(assetPath);
        }

        private static int NaturalCompare(string a, string b)
        {
            if (a == null && b == null) return 0;
            if (a == null) return -1;
            if (b == null) return 1;

            var regex = new Regex(@"(\d+)|(\D+)");
            var partsA = regex.Matches(a);
            var partsB = regex.Matches(b);

            int count = Math.Min(partsA.Count, partsB.Count);
            for (int i = 0; i < count; i++)
            {
                string partA = partsA[i].Value;
                string partB = partsB[i].Value;

                bool isNumA = int.TryParse(partA, out int numA);
                bool isNumB = int.TryParse(partB, out int numB);

                int result;
                if (isNumA && isNumB)
                {
                    result = numA.CompareTo(numB);
                }
                else
                {
                    result = string.Compare(partA, partB, StringComparison.OrdinalIgnoreCase);
                }

                if (result != 0) return result;
            }

            return partsA.Count.CompareTo(partsB.Count);
        }
    }
}
