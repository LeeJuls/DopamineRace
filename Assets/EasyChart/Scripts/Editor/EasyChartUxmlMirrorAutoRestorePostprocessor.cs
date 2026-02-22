using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using EasyChart;

namespace EasyChart.Editor
{
    internal sealed class EasyChartUxmlMirrorAutoRestorePostprocessor : AssetPostprocessor
    {
        private const string ROOT_PATH = "Assets/EasyChart/Library";
        private const string UXML_ROOT_PATH = "Assets/EasyChart/LibraryUxml";
        private const string UXML_BACKUPS_SUBFOLDER = "_Backups";
        private const string PROFILE_JSON_EXT = ".chartprofile.json";

        private const string AutoRestoreEnabledPrefsKey = "EasyChart.UxmlMirrorAutoRestore.Enabled";

        private static bool AutoRestoreEnabled
        {
            get => EditorPrefs.GetBool(AutoRestoreEnabledPrefsKey, true);
            set => EditorPrefs.SetBool(AutoRestoreEnabledPrefsKey, value);
        }

        private static bool s_scanScheduled;
        private static double s_lastScanTime;

        private static void OnPostprocessAllAssets(
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            if (!AutoRestoreEnabled) return;

            // Only react to mirror changes. Deleting profiles is a valid user action and should not be auto-undone.
            if (AnyRelevant(importedAssets) || AnyRelevant(movedAssets))
            {
                ScheduleFullScan();
            }
        }

        private static bool AnyRelevant(string[] assetPaths)
        {
            if (assetPaths == null || assetPaths.Length == 0) return false;

            for (int i = 0; i < assetPaths.Length; i++)
            {
                string p = assetPaths[i]?.Replace('\\', '/');
                if (string.IsNullOrEmpty(p)) continue;

                if (!p.StartsWith(UXML_ROOT_PATH, StringComparison.OrdinalIgnoreCase)) continue;
                if (IsUnderBackups(p)) continue;

                if (p.EndsWith(".uxml", StringComparison.OrdinalIgnoreCase) || p.EndsWith(PROFILE_JSON_EXT, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static void ScheduleFullScan()
        {
            if (s_scanScheduled) return;
            s_scanScheduled = true;

            EditorApplication.delayCall += () =>
            {
                s_scanScheduled = false;

                double now = EditorApplication.timeSinceStartup;
                if (now - s_lastScanTime < 0.25) return;
                s_lastScanTime = now;

                try
                {
                    ScanAndRestoreMissingProfiles();
                }
                catch
                {
                }
            };
        }

        [MenuItem("Tools/EasyChart/Auto Restore Missing Profiles", priority = 2010)]
        private static void ToggleAutoRestore()
        {
            AutoRestoreEnabled = !AutoRestoreEnabled;
        }

        [MenuItem("Tools/EasyChart/Auto Restore Missing Profiles", validate = true)]
        private static bool ToggleAutoRestoreValidate()
        {
            Menu.SetChecked("Tools/EasyChart/Auto Restore Missing Profiles", AutoRestoreEnabled);
            return true;
        }

        private static void ScanAndRestoreMissingProfiles()
        {
            if (!AutoRestoreEnabled) return;
            if (!AssetDatabase.IsValidFolder(UXML_ROOT_PATH)) return;

            string projectRoot = Directory.GetCurrentDirectory().Replace('\\', '/');
            string uxmlRootFullPath = Path.Combine(projectRoot, UXML_ROOT_PATH.Replace('/', Path.DirectorySeparatorChar));
            if (!Directory.Exists(uxmlRootFullPath)) return;

            string[] uxmlFiles;
            try
            {
                uxmlFiles = Directory.GetFiles(uxmlRootFullPath, "*.uxml", SearchOption.AllDirectories);
            }
            catch
            {
                return;
            }

            var createdAssetPaths = new List<string>();

            for (int i = 0; i < uxmlFiles.Length; i++)
            {
                string full = uxmlFiles[i];
                string fullNorm = full.Replace('\\', '/');

                if (!fullNorm.StartsWith(projectRoot + "/", StringComparison.OrdinalIgnoreCase)) continue;

                string uxmlAssetPath = fullNorm.Substring(projectRoot.Length + 1);
                if (!uxmlAssetPath.StartsWith(UXML_ROOT_PATH, StringComparison.OrdinalIgnoreCase)) continue;
                if (IsUnderBackups(uxmlAssetPath)) continue;

                string rel = uxmlAssetPath.Substring(UXML_ROOT_PATH.Length).TrimStart('/');
                if (string.IsNullOrEmpty(rel)) continue;

                string libraryName = null;
                string relInLibrary = rel;
                int slash = rel.IndexOf('/');
                if (slash >= 0)
                {
                    libraryName = rel.Substring(0, slash);
                    relInLibrary = rel.Substring(slash + 1);
                }

                if (string.Equals(libraryName, UXML_BACKUPS_SUBFOLDER, StringComparison.OrdinalIgnoreCase)) continue;

                string relDir = Path.GetDirectoryName(relInLibrary)?.Replace('\\', '/');
                string baseName = Path.GetFileNameWithoutExtension(relInLibrary);
                if (string.IsNullOrEmpty(baseName)) continue;

                string profileLibraryRoot = string.IsNullOrEmpty(libraryName) ? ROOT_PATH : $"{ROOT_PATH}/{libraryName}";
                string destFolder = string.IsNullOrEmpty(relDir) ? profileLibraryRoot : $"{profileLibraryRoot}/{relDir}";
                EnsureFolderExists(destFolder);

                string destAssetPath = $"{destFolder}/{baseName}.asset";
                if (AssetDatabase.LoadAssetAtPath<ChartProfile>(destAssetPath) != null) continue;

                string jsonFullPath = Path.Combine(Path.GetDirectoryName(full) ?? string.Empty, baseName + PROFILE_JSON_EXT);

                var profile = ScriptableObject.CreateInstance<ChartProfile>();
                bool restoredFromJson = false;

                if (File.Exists(jsonFullPath))
                {
                    try
                    {
                        string json = File.ReadAllText(jsonFullPath);
                        if (!string.IsNullOrEmpty(json))
                        {
                            EditorJsonUtility.FromJsonOverwrite(json, profile);
                            restoredFromJson = true;
                        }
                    }
                    catch
                    {
                        restoredFromJson = false;
                    }
                }

                if (!restoredFromJson)
                {
                    string text;
                    try
                    {
                        text = File.ReadAllText(full);
                    }
                    catch
                    {
                        continue;
                    }

                    if (!TryParseChartElementStyle(text, out float w, out float h))
                    {
                        w = 300f;
                        h = 200f;
                    }

                    profile.chartWidth = w;
                    profile.chartHeight = h;
                }

                if (string.IsNullOrEmpty(profile.chartName)) profile.chartName = baseName;

                AssetDatabase.CreateAsset(profile, destAssetPath);
                createdAssetPaths.Add(destAssetPath);
            }

            if (createdAssetPaths.Count > 0)
            {
                AssetDatabase.SaveAssets();

                EditorApplication.delayCall += () =>
                {
                    if (!EditorApplication.isCompiling && !EditorApplication.isUpdating) AssetDatabase.Refresh();
                };
            }
        }

        private static bool IsUnderBackups(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath)) return false;
            string p = assetPath.Replace('\\', '/');
            int idx = p.IndexOf("/" + UXML_BACKUPS_SUBFOLDER + "/", StringComparison.OrdinalIgnoreCase);
            if (idx >= 0) return true;
            return p.EndsWith("/" + UXML_BACKUPS_SUBFOLDER, StringComparison.OrdinalIgnoreCase);
        }

        private static bool TryParseChartElementStyle(string uxml, out float width, out float height)
        {
            width = 0f;
            height = 0f;
            if (string.IsNullOrEmpty(uxml)) return false;

            var m = Regex.Match(uxml, "style=\"([^\"]*)\"", RegexOptions.IgnoreCase);
            if (!m.Success) return false;
            string style = m.Groups[1].Value;

            bool ok = false;

            var mw = Regex.Match(style, "width\\s*:\\s*([0-9]+(?:\\.[0-9]+)?)px", RegexOptions.IgnoreCase);
            if (mw.Success && float.TryParse(mw.Groups[1].Value, out var w))
            {
                width = w;
                ok = true;
            }

            var mh = Regex.Match(style, "height\\s*:\\s*([0-9]+(?:\\.[0-9]+)?)px", RegexOptions.IgnoreCase);
            if (mh.Success && float.TryParse(mh.Groups[1].Value, out var h))
            {
                height = h;
                ok = true;
            }

            return ok;
        }

        private static void EnsureFolderExists(string folderPath)
        {
            if (AssetDatabase.IsValidFolder(folderPath)) return;

            string normalized = folderPath.Replace("\\", "/");
            if (!normalized.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase)) return;

            string[] parts = normalized.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }
                current = next;
            }
        }
    }
}
