using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace EasyChart.Editor
{
    public static class EasyChartManualWeb
    {
        private const string ManualRootFolder = "Assets/EasyChart/Docs/Manual";
        private const string WebRootFolder = "Assets/EasyChart/Docs/ManualWeb";
        private const string DataFileName = "manual-data.js";
        private const string DataFileNameZh = "manual-data.zh.js";
        private const string DataFileNameEn = "manual-data.en.js";
        private const string IndexFileName = "manual.html";
        private const string OpenFileName = "manual-open.js";

        [MenuItem("EasyChart/Manual")]
        public static void Open()
        {
            ExportManualData();
            WriteOpenRequestJs(null, null);

            string indexAssetPath = WebRootFolder.TrimEnd('/') + "/" + IndexFileName;
            string indexFullPath = AssetPathToFullPath(indexAssetPath);

            if (string.IsNullOrEmpty(indexFullPath) || !File.Exists(indexFullPath))
            {
                EditorUtility.DisplayDialog("EasyChart Manual", "manual.html not found: " + indexAssetPath, "OK");
                return;
            }

            string url = new Uri(indexFullPath).AbsoluteUri;
            Debug.Log("[EasyChartManualWeb] Open manual url: " + url);
            Application.OpenURL(url);
        }

        public static void OpenChapter(string chapterId)
        {
            OpenChapter(chapterId, null);
        }

        public static void OpenChapter(string chapterId, string anchor)
        {
            ExportManualData();
            WriteOpenRequestJs(chapterId, anchor);

            string indexAssetPath = WebRootFolder.TrimEnd('/') + "/" + IndexFileName;
            string indexFullPath = AssetPathToFullPath(indexAssetPath);

            if (string.IsNullOrEmpty(indexFullPath) || !File.Exists(indexFullPath))
            {
                EditorUtility.DisplayDialog("EasyChart Manual", "manual.html not found: " + indexAssetPath, "OK");
                return;
            }

            // NOTE:
            // Some Windows/browser combinations drop hash/query when opening file:// URLs via shell.
            // We therefore persist the desired chapter into manual-open.js, which manual.html loads.
            string url = new Uri(indexFullPath).AbsoluteUri;

            Debug.Log("[EasyChartManualWeb] Open manual url: " + url);

            Application.OpenURL(url);
        }

        private static void WriteOpenRequestJs(string chapterId, string anchor)
        {
            try
            {
                string webOut = AssetPathToFullPath(WebRootFolder);
                Directory.CreateDirectory(webOut);

                var sb = new StringBuilder(256);
                sb.Append("window.EASYCHART_MANUAL_OPEN = {");
                sb.Append("chapterId: \"").Append(EscapeJsString(chapterId)).Append("\",");
                sb.Append(" anchor: \"").Append(EscapeJsString(anchor)).Append("\",");
                sb.Append(" at: \"").Append(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")).Append("\"");
                sb.Append("};\n");

                File.WriteAllText(Path.Combine(webOut, OpenFileName), sb.ToString(), Encoding.UTF8);

                EditorApplication.delayCall += () =>
                {
                    if (!EditorApplication.isCompiling && !EditorApplication.isUpdating) AssetDatabase.Refresh();
                };
            }
            catch (Exception e)
            {
                Debug.LogError("[EasyChartManualWeb] WriteOpenRequestJs failed: " + e);
            }
        }

        private static void ExportManualData()
        {
            if (!AssetDatabase.IsValidFolder(ManualRootFolder))
            {
                EditorUtility.DisplayDialog("EasyChart Manual", "Manual folder not found: " + ManualRootFolder, "OK");
                return;
            }

            string webOut = AssetPathToFullPath(WebRootFolder);
            Directory.CreateDirectory(webOut);

            string manualRoot = AssetPathToFullPath(ManualRootFolder);
            string zhRoot = Path.Combine(manualRoot, "zh");
            string enRoot = Path.Combine(manualRoot, "en");

            bool hasZh = Directory.Exists(zhRoot);
            bool hasEn = Directory.Exists(enRoot);

            if (hasZh || hasEn)
            {
                if (hasZh)
                {
                    var chaptersZh = LoadChaptersFromFolder(zhRoot);
                    if (chaptersZh.Count > 0)
                    {
                        string jsZh = BuildManualDataJs(chaptersZh, "EASYCHART_MANUAL_ZH");
                        File.WriteAllText(Path.Combine(webOut, DataFileNameZh), jsZh, Encoding.UTF8);

                        string legacy = BuildManualDataJs(chaptersZh, "EASYCHART_MANUAL");
                        File.WriteAllText(Path.Combine(webOut, DataFileName), legacy, Encoding.UTF8);
                    }
                }

                if (hasEn)
                {
                    var chaptersEn = LoadChaptersFromFolder(enRoot);
                    if (chaptersEn.Count > 0)
                    {
                        string jsEn = BuildManualDataJs(chaptersEn, "EASYCHART_MANUAL_EN");
                        File.WriteAllText(Path.Combine(webOut, DataFileNameEn), jsEn, Encoding.UTF8);
                    }
                }
            }
            else
            {
                var chapters = LoadChaptersFromFolder(manualRoot);
                string js = BuildManualDataJs(chapters, "EASYCHART_MANUAL");
                File.WriteAllText(Path.Combine(webOut, DataFileName), js, Encoding.UTF8);
            }

            EditorApplication.delayCall += () =>
            {
                if (!EditorApplication.isCompiling && !EditorApplication.isUpdating) AssetDatabase.Refresh();
            };
        }

        private static List<Chapter> LoadChaptersFromFolder(string manualRoot)
        {
            var mdFiles = Directory.GetFiles(manualRoot, "*.md", SearchOption.AllDirectories)
                .Select(p => p.Replace('\\', '/'))
                .OrderBy(p => p, StringComparer.OrdinalIgnoreCase)
                .ToList();

            var chapters = new List<Chapter>(mdFiles.Count);
            for (int i = 0; i < mdFiles.Count; i++)
            {
                string full = mdFiles[i];
                string rel = full.Substring(manualRoot.Length).TrimStart('/', '\\').Replace('\\', '/');
                string id = Path.GetFileNameWithoutExtension(rel);

                string content = File.ReadAllText(full, Encoding.UTF8);
                string title = TryExtractTitle(content) ?? Path.GetFileNameWithoutExtension(rel);

                chapters.Add(new Chapter
                {
                    id = id,
                    relPath = rel,
                    title = title,
                    content = content
                });
            }

            return chapters;
        }

        [Serializable]
        private class Chapter
        {
            public string id;
            public string relPath;
            public string title;
            public string content;
        }

        private static string BuildManualDataJs(List<Chapter> chapters, string globalName)
        {
            var sb = new StringBuilder(1024 + (chapters.Count * 512));
            if (string.IsNullOrEmpty(globalName)) globalName = "EASYCHART_MANUAL";
            sb.Append("window.").Append(globalName).Append(" = window.").Append(globalName).Append(" || {};\n");
            sb.Append("window.").Append(globalName).Append(".generatedAt = \"").Append(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")).Append("\";\n");
            sb.Append("window.").Append(globalName).Append(".chapters = [\n");

            for (int i = 0; i < chapters.Count; i++)
            {
                var ch = chapters[i];
                sb.Append("  { ");
                sb.Append("id: \"").Append(EscapeJsString(ch.id)).Append("\",");
                sb.Append(" relPath: \"").Append(EscapeJsString(ch.relPath)).Append("\",");
                sb.Append(" title: \"").Append(EscapeJsString(ch.title)).Append("\",");
                sb.Append(" content: \"").Append(EscapeJsString(ch.content)).Append("\" ");
                sb.Append("}");
                if (i != chapters.Count - 1) sb.Append(",");
                sb.Append("\n");
            }

            sb.Append("];\n");
            return sb.ToString();
        }

        private static string EscapeJsString(string s)
        {
            if (string.IsNullOrEmpty(s)) return string.Empty;
            var sb = new StringBuilder(s.Length + 16);
            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];
                switch (c)
                {
                    case '\\': sb.Append("\\\\"); break;
                    case '"': sb.Append("\\\""); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': break;
                    case '\t': sb.Append("\\t"); break;
                    default:
                        if (c < 32) sb.Append(' ');
                        else sb.Append(c);
                        break;
                }
            }
            return sb.ToString();
        }

        private static string TryExtractTitle(string markdown)
        {
            if (string.IsNullOrEmpty(markdown)) return null;
            var lines = markdown.Replace("\r\n", "\n").Replace("\r", "\n").Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                var t = (lines[i] ?? string.Empty).Trim();
                if (string.IsNullOrWhiteSpace(t)) continue;
                if (t.StartsWith("#", StringComparison.Ordinal))
                {
                    int level = 0;
                    while (level < t.Length && t[level] == '#') level++;
                    string title = t.Substring(level).Trim();
                    return string.IsNullOrEmpty(title) ? null : title;
                }
                break;
            }
            return null;
        }

        private static string AssetPathToFullPath(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath)) return string.Empty;
            assetPath = assetPath.Replace('\\', '/');
            if (Path.IsPathRooted(assetPath)) return assetPath;

            string projectRoot = Path.GetDirectoryName(Application.dataPath);
            if (string.IsNullOrEmpty(projectRoot)) return Path.GetFullPath(assetPath);
            return Path.GetFullPath(Path.Combine(projectRoot, assetPath));
        }
    }
}
