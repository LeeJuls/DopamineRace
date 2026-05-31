using UnityEditor;
using EasyChart.Internal;

namespace EasyChart.Editor
{
    public partial class EasyChartLibraryWindow
    {
        private void ScheduleUpdatePreview()
        {
            if (_isUpdatingPreview) return;
            if (_previewUpdateScheduled) return;

            _previewUpdateScheduled = true;

            var settings = EasyChartSettings.Instance;
            int delayMs = settings?.previewDelay ?? 100;

            // Use schedule with delay from settings
            rootVisualElement?.schedule.Execute(() =>
            {
                _previewUpdateScheduled = false;
                if (this == null || rootVisualElement == null) return;
                UpdatePreview();
            }).ExecuteLater(delayMs);
        }

        private void UpdatePreview()
        {
            // Check if window is still valid
            if (this == null || rootVisualElement == null) return;
            if (_selectedProfile == null || _previewChart == null) return;

            if (_isUpdatingPreview) return;
            _isUpdatingPreview = true;
            try
            {
                if (_serializedProfile != null && _serializedProfile.hasModifiedProperties)
                {
                    _serializedProfile.ApplyModifiedProperties();
                }
                _previewChart.Profile = _selectedProfile;
                // Force refresh even if Profile instance hasn't changed (e.g., color palette applied)
                _previewChart.ForceRefreshProfile();
                UpdateInjectionJsonExample();
            }
            catch (System.Exception ex)
            {
                EasyChartLog.Error($"Preview refresh failed: {ex.Message}\n{ex.StackTrace}");
            }
            finally
            {
                _isUpdatingPreview = false;
            }
        }
    }
}
