using UnityEngine;

namespace EasyChart.Internal
{
    /// <summary>
    /// Logging utility that respects EasyChartSettings.enableDebugLogs and logLevel
    /// </summary>
    public static class EasyChartLog
    {
        public static void Info(string message)
        {
            if (ShouldLog(EasyChartSettings.LogLevel.Info))
                Debug.Log($"[EasyChart] {message}");
        }

        public static void Warning(string message)
        {
            if (ShouldLog(EasyChartSettings.LogLevel.Warning))
                Debug.LogWarning($"[EasyChart] {message}");
        }

        public static void Error(string message)
        {
            // Errors always log regardless of logLevel, but respect enableDebugLogs
            if (ShouldLog(EasyChartSettings.LogLevel.Error))
                Debug.LogError($"[EasyChart] {message}");
        }

        private static bool ShouldLog(EasyChartSettings.LogLevel level)
        {
            var settings = EasyChartSettings.Instance;
            if (settings == null) return true; // Default to enabled if settings not loaded
            if (!settings.enableDebugLogs) return false;
            return level >= settings.logLevel;
        }
    }
}
