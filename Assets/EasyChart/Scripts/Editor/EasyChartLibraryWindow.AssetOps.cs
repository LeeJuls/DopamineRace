using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using EasyChart;

namespace EasyChart.Editor
{
    public partial class EasyChartLibraryWindow
    {
        private static string _profileClipboardJson;
        private static string _profileClipboardSourceName;

        private static bool IsChartProfileAsset(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath)) return false;
            if (AssetDatabase.IsValidFolder(assetPath)) return false;
            if (!assetPath.EndsWith(".asset", StringComparison.OrdinalIgnoreCase)) return false;
            return AssetDatabase.LoadAssetAtPath<ChartProfile>(assetPath) != null;
        }

        private static bool HasProfileInClipboard()
        {
            return !string.IsNullOrEmpty(_profileClipboardJson);
        }

        private void CopyProfileToClipboard(string assetPath)
        {
            if (!IsChartProfileAsset(assetPath))
            {
                EditorUtility.DisplayDialog("Copy", "Please select a ChartProfile asset.", "OK");
                return;
            }

            var profile = AssetDatabase.LoadAssetAtPath<ChartProfile>(assetPath);
            if (profile == null)
            {
                EditorUtility.DisplayDialog("Copy", "Failed to load ChartProfile.", "OK");
                return;
            }

            _profileClipboardJson = EditorJsonUtility.ToJson(profile);
            _profileClipboardSourceName = profile.name;
        }

        private void PasteProfileOverwriteFromClipboard(string targetAssetPath)
        {
            if (!HasProfileInClipboard())
            {
                EditorUtility.DisplayDialog("Paste", "Clipboard is empty.", "OK");
                return;
            }

            if (!IsChartProfileAsset(targetAssetPath))
            {
                EditorUtility.DisplayDialog("Paste", "Please select a ChartProfile asset to overwrite.", "OK");
                return;
            }

            var target = AssetDatabase.LoadAssetAtPath<ChartProfile>(targetAssetPath);
            if (target == null)
            {
                EditorUtility.DisplayDialog("Paste", "Failed to load target ChartProfile.", "OK");
                return;
            }

            string srcName = string.IsNullOrEmpty(_profileClipboardSourceName) ? "(Clipboard)" : _profileClipboardSourceName;
            if (!EditorUtility.DisplayDialog(
                    "Paste (Overwrite)",
                    $"This will overwrite ALL properties of:\n\n  {target.name}\n\nwith:\n\n  {srcName}\n\nContinue?",
                    "Overwrite",
                    "Cancel"))
            {
                return;
            }

            Undo.RecordObject(target, "Paste ChartProfile");
            EditorJsonUtility.FromJsonOverwrite(_profileClipboardJson, target);
            EditorUtility.SetDirty(target);

            AssetDatabase.SaveAssets();

            // Refresh UI if we are overwriting current selection
            if (_selectedProfile == target)
            {
                if (_serializedProfile != null)
                {
                    if (_serializedProfile.hasModifiedProperties) _serializedProfile.ApplyModifiedProperties();
                    _serializedProfile.Update();
                }
                ScheduleRefreshSeriesList();
                ScheduleUpdatePreview();
            }

            EditorGUIUtility.PingObject(target);
            OnTreeSelectionChanged(new object[] { targetAssetPath });
        }

        private void PasteProfileAsNewFromClipboard(string targetFolder)
        {
            if (!HasProfileInClipboard())
            {
                EditorUtility.DisplayDialog("Paste", "Clipboard is empty.", "OK");
                return;
            }

            if (string.IsNullOrEmpty(targetFolder) || !AssetDatabase.IsValidFolder(targetFolder))
            {
                EditorUtility.DisplayDialog("Paste", "Target folder is invalid.", "OK");
                return;
            }

            string baseName = string.IsNullOrEmpty(_profileClipboardSourceName) ? "ChartProfile" : _profileClipboardSourceName;
            string destPath = AssetDatabase.GenerateUniqueAssetPath($"{targetFolder}/{baseName} Copy.asset");

            var profile = ScriptableObject.CreateInstance<ChartProfile>();
            EditorJsonUtility.FromJsonOverwrite(_profileClipboardJson, profile);

            AssetDatabase.CreateAsset(profile, destPath);
            AssetDatabase.SaveAssets();

            RefreshTree();

            EditorGUIUtility.PingObject(profile);
            OnTreeSelectionChanged(new object[] { destPath });
        }

        private void CloneProfile(string path)
        {
            if (string.IsNullOrEmpty(path)) return;
            if (AssetDatabase.IsValidFolder(path)) return;

            string folder = Path.GetDirectoryName(path)?.Replace('\\', '/');
            if (string.IsNullOrEmpty(folder) || !AssetDatabase.IsValidFolder(folder)) return;

            string ext = Path.GetExtension(path);
            if (!ext.Equals(".asset", StringComparison.OrdinalIgnoreCase)) return;

            string baseName = Path.GetFileNameWithoutExtension(path);
            if (string.IsNullOrEmpty(baseName)) return;

            // Extract trailing number from baseName if present
            string prefix = baseName;
            int startNum = 1;
            int trailingNumStart = baseName.Length;
            while (trailingNumStart > 0 && char.IsDigit(baseName[trailingNumStart - 1]))
            {
                trailingNumStart--;
            }
            if (trailingNumStart < baseName.Length)
            {
                prefix = baseName.Substring(0, trailingNumStart);
                if (int.TryParse(baseName.Substring(trailingNumStart), out int parsed))
                {
                    startNum = parsed + 1;
                }
            }
            else
            {
                prefix = baseName + " ";
            }

            string newPath = null;
            string newName = null;
            for (int i = startNum; i <= startNum + 9999; i++)
            {
                newName = $"{prefix}{i}";
                string candidate = $"{folder}/{newName}{ext}";
                if (!AssetPathExists(candidate))
                {
                    newPath = candidate;
                    break;
                }
            }

            if (string.IsNullOrEmpty(newPath))
            {
                EditorUtility.DisplayDialog("Error", "Failed to create a unique clone name.", "OK");
                return;
            }

            bool ok = AssetDatabase.CopyAsset(path, newPath);
            if (!ok)
            {
                EditorUtility.DisplayDialog("Error", "Failed to clone ChartProfile.", "OK");
                return;
            }

            AssetDatabase.SaveAssets();

            EditorApplication.delayCall += () =>
            {
                if (!EditorApplication.isCompiling && !EditorApplication.isUpdating) AssetDatabase.Refresh();
            };

            var clonedProfile = AssetDatabase.LoadAssetAtPath<ChartProfile>(newPath);
            if (clonedProfile != null && !string.Equals(clonedProfile.name, newName, StringComparison.Ordinal))
            {
                clonedProfile.name = newName;
                EditorUtility.SetDirty(clonedProfile);
                AssetDatabase.SaveAssets();
            }

            RefreshTree();

            if (clonedProfile != null)
            {
                EditorGUIUtility.PingObject(clonedProfile);
                OnTreeSelectionChanged(new object[] { newPath });
            }
        }

        private static bool AssetPathExists(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath)) return false;
            if (AssetDatabase.IsValidFolder(assetPath)) return true;
            if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath) != null) return true;

            string osPath = assetPath.Replace('/', Path.DirectorySeparatorChar);
            return File.Exists(osPath) || Directory.Exists(osPath);
        }

        private void DeleteAssetOrFolder(string path)
        {
            if (string.IsNullOrEmpty(path)) return;

            string name = AssetDatabase.IsValidFolder(path)
                ? Path.GetFileName(path)
                : Path.GetFileNameWithoutExtension(path);

            if (!EditorUtility.DisplayDialog("Delete", $"Delete '{name}'?", "Delete", "Cancel")) return;

            bool ok = AssetDatabase.DeleteAsset(path);
            if (!ok)
            {
                EditorUtility.DisplayDialog("Error", "Delete failed.", "OK");
                return;
            }

            AssetDatabase.SaveAssets();
            RefreshTree();
        }

        private void CreateNewFolderUnder(string parentFolder)
        {
            if (string.IsNullOrEmpty(parentFolder) || !AssetDatabase.IsValidFolder(parentFolder)) return;

            TextPromptWindow.Show("New Folder", "New Folder", folderName =>
            {
                folderName = SanitizeFileName(folderName);
                if (string.IsNullOrWhiteSpace(folderName)) return;

                string guid = AssetDatabase.CreateFolder(parentFolder, folderName);
                if (string.IsNullOrEmpty(guid))
                {
                    EditorUtility.DisplayDialog("Error", "Failed to create folder.", "OK");
                    return;
                }

                RefreshTree();
            });
        }

        private void CreateNewProfileInFolder(string folder)
        {
            if (string.IsNullOrEmpty(folder) || !AssetDatabase.IsValidFolder(folder)) return;
            CreateNewProfileAtFolder(folder);
        }

        private void CreateNewProfileAtFolder(string folder)
        {
            if (string.IsNullOrEmpty(folder) || !AssetDatabase.IsValidFolder(folder)) return;

            var profile = ScriptableObject.CreateInstance<ChartProfile>();
            string fullPath = AssetDatabase.GenerateUniqueAssetPath($"{folder}/NewChart.asset");

            AssetDatabase.CreateAsset(profile, fullPath);
            AssetDatabase.SaveAssets();
            RefreshTree();
        }
    }
}
