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
            string manualPath = System.IO.Path.GetFullPath(
                System.IO.Path.Combine(Application.dataPath, "EasyChart/Docs/ManualWeb/manual.html")
            );
            
            string url = $"file:///{manualPath.Replace("\\", "/")}#/{manualPage}";
            
            Debug.Log($"[OpenManualExample] Opening manual: {url}");
            Application.OpenURL(url);
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
