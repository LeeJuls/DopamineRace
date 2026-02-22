using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using EasyChart;

namespace EasyChart.Editor
{
    public partial class EasyChartLibraryWindow
    {
        private const string UXML_ROOT_PATH = "Assets/EasyChart/LibraryUxml";
        private const string UXML_BACKUPS_SUBFOLDER = "_Backups";
        private const string PROFILE_JSON_EXT = ".chartprofile.json";

        private void ImportUxmlMirrorToRestoreProfiles()
        {
            string projectRoot = Directory.GetCurrentDirectory().Replace('\\', '/');
            string defaultAbs = Path.Combine(projectRoot, GetActiveUxmlRootPath().Replace('/', Path.DirectorySeparatorChar));
            string picked = EditorUtility.OpenFolderPanel("Import UXML Mirror Root", defaultAbs, string.Empty);
            if (string.IsNullOrEmpty(picked)) return;

            string pickedNorm = picked.Replace('\\', '/');
            if (!pickedNorm.StartsWith(projectRoot + "/", StringComparison.OrdinalIgnoreCase))
            {
                EditorUtility.DisplayDialog("Import", "Please pick a folder inside this Unity project (under the project root).", "OK");
                return;
            }

            string uxmlRootAssetPath = pickedNorm.Substring(projectRoot.Length + 1);
            if (!uxmlRootAssetPath.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase) || !AssetDatabase.IsValidFolder(uxmlRootAssetPath))
            {
                EditorUtility.DisplayDialog("Import", $"Invalid folder: {uxmlRootAssetPath}", "OK");
                return;
            }

            string profileLibraryRoot = ROOT_PATH;
            if (uxmlRootAssetPath.StartsWith(UXML_ROOT_PATH, StringComparison.OrdinalIgnoreCase))
            {
                string rel = uxmlRootAssetPath.Substring(UXML_ROOT_PATH.Length).TrimStart('/');
                if (!string.IsNullOrEmpty(rel))
                {
                    int slash = rel.IndexOf('/');
                    string libraryName = slash >= 0 ? rel.Substring(0, slash) : rel;
                    if (!string.IsNullOrEmpty(libraryName) && !string.Equals(libraryName, UXML_BACKUPS_SUBFOLDER, StringComparison.OrdinalIgnoreCase))
                    {
                        profileLibraryRoot = $"{ROOT_PATH}/{libraryName}";
                    }
                }
            }

            string uxmlRootFullPath = Path.Combine(projectRoot, uxmlRootAssetPath.Replace('/', Path.DirectorySeparatorChar));
            if (!Directory.Exists(uxmlRootFullPath))
            {
                EditorUtility.DisplayDialog("Import", "Selected folder does not exist on disk.", "OK");
                return;
            }

            string backupsRoot = (uxmlRootAssetPath + "/" + UXML_BACKUPS_SUBFOLDER).Replace('\\', '/');

            string[] files;
            try
            {
                files = Directory.GetFiles(uxmlRootFullPath, "*.uxml", SearchOption.AllDirectories);
            }
            catch (Exception e)
            {
                EditorUtility.DisplayDialog("Import", e.Message, "OK");
                return;
            }

            int created = 0;
            int createdFromJson = 0;
            int skippedExisting = 0;
            int skippedInvalid = 0;

            for (int i = 0; i < files.Length; i++)
            {
                string full = files[i];
                string fullNorm = full.Replace('\\', '/');

                string assetPath;
                if (fullNorm.StartsWith(projectRoot + "/", StringComparison.OrdinalIgnoreCase))
                {
                    assetPath = fullNorm.Substring(projectRoot.Length + 1);
                }
                else
                {
                    continue;
                }

                if (!assetPath.StartsWith(uxmlRootAssetPath, StringComparison.OrdinalIgnoreCase)) continue;
                if (assetPath.StartsWith(backupsRoot, StringComparison.OrdinalIgnoreCase)) continue;

                string rel = assetPath.Substring(uxmlRootAssetPath.Length).TrimStart('/');
                if (string.IsNullOrEmpty(rel)) continue;

                string relDir = Path.GetDirectoryName(rel)?.Replace('\\', '/');
                string baseName = Path.GetFileNameWithoutExtension(rel);
                if (string.IsNullOrEmpty(baseName)) { skippedInvalid++; continue; }

                string destFolder = string.IsNullOrEmpty(relDir) ? profileLibraryRoot : $"{profileLibraryRoot}/{relDir}";
                EnsureFolderExists(destFolder);
                string destAssetPath = $"{destFolder}/{baseName}.asset";

                if (AssetDatabase.LoadAssetAtPath<ChartProfile>(destAssetPath) != null)
                {
                    skippedExisting++;
                    continue;
                }

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
                        skippedInvalid++;
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
                created++;
                if (restoredFromJson) createdFromJson++;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            RefreshTree();
            ScheduleRefreshSeriesList();
            ScheduleUpdatePreview();

            EditorUtility.DisplayDialog(
                "Import",
                $"Restored {created} profile(s) ({createdFromJson} from JSON). Skipped {skippedExisting} existing, {skippedInvalid} invalid.",
                "OK");
        }

        private void ImportUxmlRestoreSelectedProfile(string profilePath)
        {
            if (string.IsNullOrEmpty(profilePath)) return;
            
            var profile = AssetDatabase.LoadAssetAtPath<ChartProfile>(profilePath);
            if (profile == null)
            {
                EditorUtility.DisplayDialog("Import", "No valid ChartProfile selected.", "OK");
                return;
            }

            string profileName = Path.GetFileNameWithoutExtension(profilePath);
            string libraryRoot = GetActiveProfileRootPath();
            
            // Calculate relative path
            string relativePath = profilePath;
            if (relativePath.StartsWith(libraryRoot))
            {
                relativePath = relativePath.Substring(libraryRoot.Length).TrimStart('/');
            }
            string relativeFolder = Path.GetDirectoryName(relativePath)?.Replace('\\', '/');

            // Build UXML path
            string uxmlRoot = GetActiveUxmlRootPath();
            string uxmlFolder = string.IsNullOrEmpty(relativeFolder) ? uxmlRoot : $"{uxmlRoot}/{relativeFolder}";
            string uxmlPath = $"{uxmlFolder}/{profileName}.uxml";
            string jsonPath = $"{uxmlFolder}/{profileName}{PROFILE_JSON_EXT}";

            string projectRoot = Directory.GetCurrentDirectory().Replace('\\', '/');
            string jsonFullPath = Path.Combine(projectRoot, jsonPath.Replace('/', Path.DirectorySeparatorChar));
            string uxmlFullPath = Path.Combine(projectRoot, uxmlPath.Replace('/', Path.DirectorySeparatorChar));

            bool restoredFromJson = false;
            if (File.Exists(jsonFullPath))
            {
                try
                {
                    string json = File.ReadAllText(jsonFullPath);
                    if (!string.IsNullOrEmpty(json))
                    {
                        Undo.RecordObject(profile, "Restore Profile from UXML");
                        EditorJsonUtility.FromJsonOverwrite(json, profile);
                        EditorUtility.SetDirty(profile);
                        restoredFromJson = true;
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[EasyChart] Failed to restore from JSON: {e.Message}");
                }
            }

            if (!restoredFromJson && File.Exists(uxmlFullPath))
            {
                try
                {
                    string text = File.ReadAllText(uxmlFullPath);
                    if (TryParseChartElementStyle(text, out float w, out float h))
                    {
                        Undo.RecordObject(profile, "Restore Profile from UXML");
                        profile.chartWidth = w;
                        profile.chartHeight = h;
                        EditorUtility.SetDirty(profile);
                        restoredFromJson = true;
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[EasyChart] Failed to restore from UXML: {e.Message}");
                }
            }

            if (restoredFromJson)
            {
                AssetDatabase.SaveAssets();
                ScheduleRefreshSeriesList();
                ScheduleUpdatePreview();
                EditorUtility.DisplayDialog("Import", $"Restored profile '{profileName}' from UXML.", "OK");
            }
            else
            {
                EditorUtility.DisplayDialog("Import", $"No UXML/JSON found for profile '{profileName}'.\nExpected: {uxmlPath}", "OK");
            }
        }

        private void ImportUxmlFromBackup()
        {
            string projectRoot = Directory.GetCurrentDirectory().Replace('\\', '/');
            string backupRoot = $"{GetActiveUxmlRootPath()}/{UXML_BACKUPS_SUBFOLDER}";
            string defaultAbs = Path.Combine(projectRoot, backupRoot.Replace('/', Path.DirectorySeparatorChar));
            
            string picked = EditorUtility.OpenFolderPanel("Select Backup Folder", defaultAbs, string.Empty);
            if (string.IsNullOrEmpty(picked)) return;

            string pickedNorm = picked.Replace('\\', '/');
            if (!pickedNorm.StartsWith(projectRoot + "/", StringComparison.OrdinalIgnoreCase))
            {
                EditorUtility.DisplayDialog("Import", "Please pick a folder inside this Unity project.", "OK");
                return;
            }

            string uxmlRootAssetPath = pickedNorm.Substring(projectRoot.Length + 1);
            if (!uxmlRootAssetPath.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase) || !AssetDatabase.IsValidFolder(uxmlRootAssetPath))
            {
                EditorUtility.DisplayDialog("Import", $"Invalid folder: {uxmlRootAssetPath}", "OK");
                return;
            }

            string profileLibraryRoot = GetActiveProfileRootPath();
            string uxmlRootFullPath = Path.Combine(projectRoot, uxmlRootAssetPath.Replace('/', Path.DirectorySeparatorChar));

            string[] files;
            try
            {
                files = Directory.GetFiles(uxmlRootFullPath, "*.uxml", SearchOption.AllDirectories);
            }
            catch (Exception e)
            {
                EditorUtility.DisplayDialog("Import", e.Message, "OK");
                return;
            }

            int created = 0;
            int createdFromJson = 0;
            int skippedExisting = 0;
            int skippedInvalid = 0;

            for (int i = 0; i < files.Length; i++)
            {
                string full = files[i];
                string fullNorm = full.Replace('\\', '/');

                string assetPath;
                if (fullNorm.StartsWith(projectRoot + "/", StringComparison.OrdinalIgnoreCase))
                {
                    assetPath = fullNorm.Substring(projectRoot.Length + 1);
                }
                else
                {
                    continue;
                }

                if (!assetPath.StartsWith(uxmlRootAssetPath, StringComparison.OrdinalIgnoreCase)) continue;

                string rel = assetPath.Substring(uxmlRootAssetPath.Length).TrimStart('/');
                if (string.IsNullOrEmpty(rel)) continue;

                string relDir = Path.GetDirectoryName(rel)?.Replace('\\', '/');
                string baseName = Path.GetFileNameWithoutExtension(rel);
                if (string.IsNullOrEmpty(baseName)) { skippedInvalid++; continue; }

                string destFolder = string.IsNullOrEmpty(relDir) ? profileLibraryRoot : $"{profileLibraryRoot}/{relDir}";
                EnsureFolderExists(destFolder);
                string destAssetPath = $"{destFolder}/{baseName}.asset";

                if (AssetDatabase.LoadAssetAtPath<ChartProfile>(destAssetPath) != null)
                {
                    skippedExisting++;
                    continue;
                }

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
                        skippedInvalid++;
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
                created++;
                if (restoredFromJson) createdFromJson++;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            RefreshTree();
            ScheduleRefreshSeriesList();
            ScheduleUpdatePreview();

            EditorUtility.DisplayDialog(
                "Import from Backup",
                $"Restored {created} profile(s) ({createdFromJson} from JSON). Skipped {skippedExisting} existing, {skippedInvalid} invalid.",
                "OK");
        }

        private void ImportUnityPackage()
        {
            string picked = EditorUtility.OpenFilePanel("Import Unity Package", "", "unitypackage");
            if (string.IsNullOrEmpty(picked)) return;

            AssetDatabase.ImportPackage(picked, true);
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

        private static string BuildProfileBackupJson(ChartProfile profile)
        {
            if (profile == null) return string.Empty;
            return EditorJsonUtility.ToJson(profile, true);
        }

        private void ExportSelectedProfileUnityPackage()
        {
            try
            {
                if (EditorApplication.isCompiling || EditorApplication.isUpdating)
                {
                    EditorUtility.DisplayDialog("Export Package", "Unity is busy (compiling/updating). Please try again after it finishes.", "OK");
                    return;
                }

                if (_selectedProfile == null)
                {
                    EditorUtility.DisplayDialog("Export Package", "Please select a Chart Profile first.", "OK");
                    return;
                }

                string profilePath = AssetDatabase.GetAssetPath(_selectedProfile);
                if (string.IsNullOrEmpty(profilePath))
                {
                    EditorUtility.DisplayDialog("Export Package", "Selected profile has no asset path.", "OK");
                    return;
                }

                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string baseName = SanitizeFileName(Path.GetFileNameWithoutExtension(profilePath));
                if (string.IsNullOrEmpty(baseName)) baseName = SanitizeFileName(_selectedProfile.name);
                if (string.IsNullOrEmpty(baseName)) baseName = "ChartProfile";

                string destFolder = $"{GetActiveUxmlRootPath()}/{UXML_BACKUPS_SUBFOLDER}/UnityPackages";
                EnsureFolderExists(destFolder);

                string destAssetPath = $"{destFolder}/{baseName}_{timestamp}.unitypackage";
                string projectRoot = Directory.GetCurrentDirectory();
                string destFullPath = Path.Combine(projectRoot, destAssetPath.Replace('/', Path.DirectorySeparatorChar));

                AssetDatabase.ExportPackage(
                    new[] { profilePath },
                    destFullPath,
                    ExportPackageOptions.IncludeDependencies | ExportPackageOptions.Recurse);
                EditorUtility.DisplayDialog("Export Package", $"Exported to {destAssetPath}", "OK");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[EasyChartLibraryWindow] ExportSelectedProfileUnityPackage failed: {ex.Message}\n{ex.StackTrace}");
                EditorUtility.DisplayDialog("Export Package", ex.Message, "OK");
            }
        }

        private void ExportAllProfilesUnityPackage()
        {
            try
            {
                if (EditorApplication.isCompiling || EditorApplication.isUpdating)
                {
                    EditorUtility.DisplayDialog("Export Package", "Unity is busy (compiling/updating). Please try again after it finishes.", "OK");
                    return;
                }

                var guids = AssetDatabase.FindAssets("t:ChartProfile", new[] { GetActiveProfileRootPath() });
                if (guids == null || guids.Length == 0)
                {
                    EditorUtility.DisplayDialog("Export Package", "No ChartProfile found under the library root.", "OK");
                    return;
                }

                var assetPaths = new List<string>(guids.Length);
                for (int i = 0; i < guids.Length; i++)
                {
                    string p = AssetDatabase.GUIDToAssetPath(guids[i]);
                    if (string.IsNullOrEmpty(p)) continue;
                    assetPaths.Add(p);
                }

                if (assetPaths.Count == 0)
                {
                    EditorUtility.DisplayDialog("Export Package", "No valid ChartProfile asset paths found.", "OK");
                    return;
                }

                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string destFolder = $"{GetActiveUxmlRootPath()}/{UXML_BACKUPS_SUBFOLDER}/UnityPackages";
                EnsureFolderExists(destFolder);

                string destAssetPath = $"{destFolder}/EasyChartLibraryProfiles_{timestamp}.unitypackage";
                string projectRoot = Directory.GetCurrentDirectory();
                string destFullPath = Path.Combine(projectRoot, destAssetPath.Replace('/', Path.DirectorySeparatorChar));

                AssetDatabase.ExportPackage(
                    assetPaths.ToArray(),
                    destFullPath,
                    ExportPackageOptions.IncludeDependencies | ExportPackageOptions.Recurse);
                EditorUtility.DisplayDialog("Export Package", $"Exported to {destAssetPath}", "OK");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[EasyChartLibraryWindow] ExportAllProfilesUnityPackage failed: {ex.Message}\n{ex.StackTrace}");
                EditorUtility.DisplayDialog("Export Package", ex.Message, "OK");
            }
        }

        private void ExportToUxml()
        {
            try
            {
                if (EditorApplication.isCompiling || EditorApplication.isUpdating)
                {
                    EditorUtility.DisplayDialog("Export", "Unity is busy (compiling/updating). Please try again after it finishes.", "OK");
                    return;
                }

                if (_selectedProfile == null)
                {
                    EditorUtility.DisplayDialog("Error", "Please select a Chart Profile first.", "OK");
                    return;
                }

                string profilePath = AssetDatabase.GetAssetPath(_selectedProfile);
                string exportFolder = Path.GetDirectoryName(profilePath)?.Replace("\\", "/");
                if (string.IsNullOrEmpty(exportFolder) || !AssetDatabase.IsValidFolder(exportFolder))
                {
                    EditorUtility.DisplayDialog("Error", "Invalid export folder for the selected profile.", "OK");
                    return;
                }

                string baseName = SanitizeFileName(_selectedProfile.name);
                string fileName = baseName + ".uxml";
                string path = $"{exportFolder}/{fileName}";

                string content = BuildUxmlContentForProfile(_selectedProfile);

                string projectRoot = Directory.GetCurrentDirectory();
                string fullPath = Path.Combine(projectRoot, path.Replace('/', Path.DirectorySeparatorChar));
                WriteTextFileIfChanged(fullPath, content);

                EditorApplication.delayCall += () =>
                {
                    if (!EditorApplication.isCompiling && !EditorApplication.isUpdating) AssetDatabase.Refresh();
                };
                EditorUtility.DisplayDialog("Export Successful", $"Exported to {path}", "OK");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[EasyChartLibraryWindow] ExportToUxml failed: {ex.Message}\n{ex.StackTrace}");
                EditorUtility.DisplayDialog("Export", ex.Message, "OK");
            }
        }

        private static bool WriteTextFileIfChanged(string fullPath, string content)
        {
            try
            {
                if (File.Exists(fullPath))
                {
                    string existing = File.ReadAllText(fullPath);
                    if (string.Equals(existing, content, StringComparison.Ordinal))
                    {
                        return false;
                    }
                }
                File.WriteAllText(fullPath, content);
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[EasyChartLibraryWindow] WriteTextFileIfChanged failed: path='{fullPath}' err='{ex.Message}'\n{ex.StackTrace}");
                return false;
            }
        }

        private string GetBackupRootFolder(string label)
        {
            label = SanitizeFileName(label);
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            return $"{GetActiveUxmlRootPath()}/{UXML_BACKUPS_SUBFOLDER}/{label}_{timestamp}";
        }

        private static string BuildUxmlContentForProfile(ChartProfile profile)
        {
            if (profile == null) return string.Empty;
            string profilePath = AssetDatabase.GetAssetPath(profile);
            string profileKey = Path.GetFileNameWithoutExtension(profilePath);
            if (string.IsNullOrEmpty(profileKey)) profileKey = profile.name;
            string profileGuid = AssetDatabase.AssetPathToGUID(profilePath);
            return $@"<ui:UXML xmlns:ui=""UnityEngine.UIElements"" xmlns:ec=""EasyChart"" editor-extension-mode=""False"">
    <ec:ChartElement profile-name=""{profileKey}"" profile-guid=""{profileGuid}"" style=""width: {profile.chartWidth}px; height: {profile.chartHeight}px;"" />
</ui:UXML>";
        }

        private void ExportAllToUxmlMirror()
        {
            ExportFolderToUxmlMirrorInternal(GetActiveProfileRootPath(), GetActiveUxmlRootPath(), pruneOrphans: true);
        }

        private void ExportAllToUxmlMirrorBackup()
        {
            string backupRoot = GetBackupRootFolder("ExportAll");
            ExportFolderToUxmlMirrorInternal(GetActiveProfileRootPath(), backupRoot, pruneOrphans: false);
        }

        private void ExportFolderToUxmlMirror(string folderPath)
        {
            if (string.IsNullOrEmpty(folderPath) || !AssetDatabase.IsValidFolder(folderPath)) return;

            string activeProfileRoot = GetActiveProfileRootPath();
            string activeUxmlRoot = GetActiveUxmlRootPath();

            string rel = string.Empty;
            if (folderPath.StartsWith(activeProfileRoot, StringComparison.OrdinalIgnoreCase))
            {
                rel = folderPath.Substring(activeProfileRoot.Length).TrimStart('/');
            }
            string pruneRoot = string.IsNullOrEmpty(rel) ? activeUxmlRoot : $"{activeUxmlRoot}/{rel}";
            ExportFolderToUxmlMirrorInternal(folderPath, activeUxmlRoot, pruneOrphans: true, pruneRoot: pruneRoot);
        }

        private void ExportFolderToUxmlMirrorBackup(string folderPath)
        {
            if (string.IsNullOrEmpty(folderPath) || !AssetDatabase.IsValidFolder(folderPath)) return;
            string label = Path.GetFileName(folderPath);
            if (string.IsNullOrEmpty(label)) label = "ExportFolder";
            string backupRoot = GetBackupRootFolder(label);
            ExportFolderToUxmlMirrorInternal(folderPath, backupRoot, pruneOrphans: false);
        }

        private void ExportFolderToUxmlMirrorInternal(string folderPath, string destRoot, bool pruneOrphans, string pruneRoot = null)
        {
            try
            {
                if (EditorApplication.isCompiling || EditorApplication.isUpdating)
                {
                    EditorUtility.DisplayDialog("Export", "Unity is busy (compiling/updating). Please try again after it finishes.", "OK");
                    return;
                }

                if (string.IsNullOrEmpty(folderPath) || !AssetDatabase.IsValidFolder(folderPath)) return;
                if (string.IsNullOrEmpty(destRoot)) return;

                EnsureFolderExists(destRoot);

                string activeProfileRoot = GetActiveProfileRootPath();

                var guids = AssetDatabase.FindAssets("t:ChartProfile", new[] { folderPath });
                if (guids == null || guids.Length == 0)
                {
                    EditorUtility.DisplayDialog("Export", "No ChartProfile found in this folder.", "OK");
                    return;
                }

                var expectedAssetPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var usedThisRun = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                int exported = 0;
                int skippedConflicts = 0;
                for (int i = 0; i < guids.Length; i++)
                {
                    string assetPath = AssetDatabase.GUIDToAssetPath(guids[i]);
                    if (string.IsNullOrEmpty(assetPath)) continue;

                    var profile = AssetDatabase.LoadAssetAtPath<ChartProfile>(assetPath);
                    if (profile == null) continue;

                    string assetFolder = Path.GetDirectoryName(assetPath)?.Replace('\\', '/');
                    if (string.IsNullOrEmpty(assetFolder)) continue;

                    string relativeFolder = string.Empty;
                    if (assetFolder.StartsWith(activeProfileRoot, StringComparison.OrdinalIgnoreCase))
                    {
                        relativeFolder = assetFolder.Substring(activeProfileRoot.Length).TrimStart('/');
                    }
                    string destFolder = string.IsNullOrEmpty(relativeFolder) ? destRoot : $"{destRoot}/{relativeFolder}";
                    EnsureFolderExists(destFolder);

                    string baseName = SanitizeFileName(Path.GetFileNameWithoutExtension(assetPath));
                    if (string.IsNullOrEmpty(baseName)) continue;

                    string destAssetPath = $"{destFolder}/{baseName}.uxml";
                    if (usedThisRun.Contains(destAssetPath))
                    {
                        skippedConflicts++;
                        continue;
                    }
                    usedThisRun.Add(destAssetPath);
                    expectedAssetPaths.Add(destAssetPath);

                    string projectRoot = Directory.GetCurrentDirectory();
                    string destFullPath = Path.Combine(projectRoot, destAssetPath.Replace('/', Path.DirectorySeparatorChar));
                    WriteTextFileIfChanged(destFullPath, BuildUxmlContentForProfile(profile));

                    string jsonAssetPath = $"{destFolder}/{baseName}{PROFILE_JSON_EXT}";
                    string jsonFullPath = Path.Combine(projectRoot, jsonAssetPath.Replace('/', Path.DirectorySeparatorChar));
                    WriteTextFileIfChanged(jsonFullPath, BuildProfileBackupJson(profile));

                    exported++;
                }

                if (pruneOrphans)
                {
                    PruneOrphanUxmlFiles(string.IsNullOrEmpty(pruneRoot) ? destRoot : pruneRoot, expectedAssetPaths);
                }

                EditorApplication.delayCall += () =>
                {
                    if (!EditorApplication.isCompiling && !EditorApplication.isUpdating) AssetDatabase.Refresh();
                };
                if (skippedConflicts > 0)
                {
                    EditorUtility.DisplayDialog("Export", $"Exported {exported} UXML file(s). Skipped {skippedConflicts} name conflict(s).", "OK");
                }
                else
                {
                    EditorUtility.DisplayDialog("Export", $"Exported {exported} UXML file(s).", "OK");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[EasyChartLibraryWindow] ExportFolderToUxmlMirrorInternal failed: folder='{folderPath}' destRoot='{destRoot}' pruneOrphans='{pruneOrphans}' err='{ex.Message}'\n{ex.StackTrace}");
                EditorUtility.DisplayDialog("Export", ex.Message, "OK");
            }
        }

        private static void PruneOrphanUxmlFiles(string destRoot, HashSet<string> expectedAssetPaths)
        {
            try
            {
                if (string.IsNullOrEmpty(destRoot)) return;

                if (destRoot.IndexOf("/" + UXML_BACKUPS_SUBFOLDER, StringComparison.OrdinalIgnoreCase) >= 0) return;

                string projectRoot = Directory.GetCurrentDirectory();
                string destRootFullPath = Path.Combine(projectRoot, destRoot.Replace('/', Path.DirectorySeparatorChar));
                if (!Directory.Exists(destRootFullPath)) return;

                string[] files;
                try
                {
                    files = Directory.GetFiles(destRootFullPath, "*.*", SearchOption.AllDirectories);
                }
                catch (Exception e)
                {
                    Debug.LogError($"[EasyChartLibraryWindow] PruneOrphanUxmlFiles scan failed: root='{destRootFullPath}' err='{e.Message}'\n{e.StackTrace}");
                    return;
                }

                for (int i = 0; i < files.Length; i++)
                {
                    string full = files[i];
                    string projectRootNormalized = projectRoot.Replace('\\', '/');
                    string fullNormalized = full.Replace('\\', '/');
                    string assetPath;
                    if (fullNormalized.StartsWith(projectRootNormalized + "/", StringComparison.OrdinalIgnoreCase))
                    {
                        assetPath = fullNormalized.Substring(projectRootNormalized.Length + 1);
                    }
                    else
                    {
                        assetPath = fullNormalized;
                    }
                    if (assetPath.IndexOf("/" + UXML_BACKUPS_SUBFOLDER + "/", StringComparison.OrdinalIgnoreCase) >= 0) continue;

                    bool isUxml = assetPath.EndsWith(".uxml", StringComparison.OrdinalIgnoreCase);
                    bool isJson = assetPath.EndsWith(PROFILE_JSON_EXT, StringComparison.OrdinalIgnoreCase);
                    if (!isUxml && !isJson) continue;

                    if (expectedAssetPaths != null)
                    {
                        if (isUxml && expectedAssetPaths.Contains(assetPath)) continue;
                        if (isJson)
                        {
                            string correspondingUxml = assetPath.Substring(0, assetPath.Length - PROFILE_JSON_EXT.Length) + ".uxml";
                            if (expectedAssetPaths.Contains(correspondingUxml)) continue;
                        }
                    }

                    AssetDatabase.DeleteAsset(assetPath);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[EasyChartLibraryWindow] PruneOrphanUxmlFiles failed: destRoot='{destRoot}' err='{ex.Message}'\n{ex.StackTrace}");
            }
        }
    }
}
