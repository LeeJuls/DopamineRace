using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace EasyChart.Samples
{
    /// <summary>
    /// Example script that opens the EasyChart manual in the default browser when clicked.
    /// Attach this to a UI Button or call OpenManual() from your own code.
    /// </summary>
    public class OpenManualExample : MonoBehaviour
    {
        [Tooltip("The manual page to open (e.g., '00_01-QuickStart')")]
        public string manualPage = "00_01-QuickStart";

        /// <summary>
        /// Opens the EasyChart manual in the default browser.
        /// Call this from a UI Button's OnClick event.
        /// </summary>
        public void OpenManual()
        {
            // Update manual-open.js so the correct chapter opens even if browser drops the hash
            WriteOpenRequestJs(manualPage);

            string manualPath = Path.GetFullPath(
                Path.Combine(Application.dataPath, "EasyChart/Docs/ManualWeb/manual.html")
            );
            
            // Add timestamp to prevent browser caching
            string timestamp = DateTime.Now.Ticks.ToString();
            string url = $"file:///{manualPath.Replace("\\", "/")}?t={timestamp}#/{manualPage}";
            
            Debug.Log($"[OpenManualExample] Opening manual: {url}");
            Application.OpenURL(url);
        }

        /// <summary>
        /// Writes the open request to manual-open.js so the manual opens to the correct page.
        /// This handles browsers that drop the URL hash when opening file:// URLs.
        /// </summary>
        private void WriteOpenRequestJs(string chapterId)
        {
            try
            {
                string webFolder = Path.Combine(Application.dataPath, "EasyChart/Docs/ManualWeb");
                string openFilePath = Path.Combine(webFolder, "manual-open.js");
                
                var sb = new StringBuilder(256);
                sb.Append("window.EASYCHART_MANUAL_OPEN = {");
                sb.Append("chapterId: \"").Append(EscapeJsString(chapterId)).Append("\",");
                sb.Append(" anchor: \"\",");
                sb.Append(" at: \"").Append(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")).Append("\"");
                sb.Append("};\n");
                
                File.WriteAllText(openFilePath, sb.ToString(), Encoding.UTF8);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[OpenManualExample] Failed to write manual-open.js: {ex.Message}");
            }
        }

        private string EscapeJsString(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r");
        }

        /// <summary>
        /// Opens the manual to a specific page.
        /// </summary>
        /// <param name="page">The page name without extension (e.g., "00_01-QuickStart")</param>
        public void OpenManualPage(string page)
        {
            manualPage = page;
            OpenManual();
        }
    }
}
