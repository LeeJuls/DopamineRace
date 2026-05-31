using UnityEngine;

namespace EasyChart
{
    /// <summary>
    /// Global settings for EasyChart editor and runtime behavior.
    /// Stored in Assets/EasyChart/Resources/EasyChartSettings.asset
    /// </summary>
    [CreateAssetMenu(fileName = "EasyChartSettings", menuName = "EasyChart/Settings")]
    public class EasyChartSettings : ScriptableObject
    {
        [Tooltip("Animation refresh rate in FPS (frames per second)")]
        [Range(1, 120)]
        public int animationFps = 30;

        [Tooltip("Default animation duration in milliseconds")]
        [Range(100, 5000)]
        public float animationDuration = 1000f;

        [Tooltip("URL for Lite version store page or documentation")]
        public string liteVersionUrl = "https://assetstore.unity.com/packages/slug/your-lite-id";

        [Tooltip("URL for Pro version store page")]
        public string proVersionUrl = "https://assetstore.unity.com/packages/slug/your-pro-id";

        [Tooltip("Preview refresh delay in milliseconds")]
        [Range(0, 2000)]
        public int previewDelay = 100;

        [Tooltip("Enable debug log output")]
        public bool enableDebugLogs = false;

        [Tooltip("Log level for debug output")]
        public LogLevel logLevel = LogLevel.Info;

        public enum LogLevel
        {
            Info,
            Warning,
            Error
        }

        #region Singleton Access

        private static EasyChartSettings _instance;

        public static EasyChartSettings Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = LoadSettings();
                }
                return _instance;
            }
        }

        private static EasyChartSettings LoadSettings()
        {
            // Try to load from Resources
            var settings = Resources.Load<EasyChartSettings>("EasyChartSettings");

            if (settings == null)
            {
                // Create default instance if not found
                settings = CreateInstance<EasyChartSettings>();
#if UNITY_EDITOR
                // In editor, create the asset file if it doesn't exist
                CreateSettingsAsset(settings);
#endif
            }

            return settings;
        }

#if UNITY_EDITOR
        private static void CreateSettingsAsset(EasyChartSettings settings)
        {
            string resourcesPath = "Assets/EasyChart/Resources";
            string assetPath = "Assets/EasyChart/Resources/EasyChartSettings.asset";

            // Ensure Resources folder exists
            if (!UnityEditor.AssetDatabase.IsValidFolder(resourcesPath))
            {
                string easyChartFolder = "Assets/EasyChart";
                if (!UnityEditor.AssetDatabase.IsValidFolder(easyChartFolder))
                {
                    UnityEditor.AssetDatabase.CreateFolder("Assets", "EasyChart");
                }
                UnityEditor.AssetDatabase.CreateFolder(easyChartFolder, "Resources");
            }

            // Check if asset already exists
            if (UnityEditor.AssetDatabase.LoadAssetAtPath<EasyChartSettings>(assetPath) == null)
            {
                UnityEditor.AssetDatabase.CreateAsset(settings, assetPath);
                UnityEditor.AssetDatabase.SaveAssets();
                Debug.Log($"[EasyChart] Created default settings at: {assetPath}");
            }
        }
#endif

        #endregion

        /// <summary>
        /// Get animation interval in milliseconds based on FPS setting
        /// </summary>
        public int GetAnimationIntervalMs()
        {
            return Mathf.RoundToInt(1000f / animationFps);
        }

        /// <summary>
        /// Check if a log level should be output based on current settings
        /// </summary>
        public bool ShouldLog(LogLevel level)
        {
            if (!enableDebugLogs) return false;
            return level >= logLevel;
        }
    }
}
