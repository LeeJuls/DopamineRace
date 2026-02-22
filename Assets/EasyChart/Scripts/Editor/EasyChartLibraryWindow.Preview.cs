using UnityEditor;

namespace EasyChart.Editor
{
    public partial class EasyChartLibraryWindow
    {
        private void ScheduleUpdatePreview()
        {
            if (_isUpdatingPreview) return;

            // Always unregister first to prevent duplicate callbacks
            EditorApplication.delayCall -= OnDeferredUpdatePreview;
            EditorApplication.delayCall += OnDeferredUpdatePreview;
        }

        private void OnDeferredUpdatePreview()
        {
            if (this == null || rootVisualElement == null) return;
            UpdatePreview();
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
                UpdateInjectionJsonExample();
            }
            catch (System.Exception ex)
            {
                UnityEngine.Debug.LogError($"[EasyChartLibraryWindow] Preview refresh failed: {ex.Message}\n{ex.StackTrace}");
            }
            finally
            {
                _isUpdatingPreview = false;
            }
        }
    }
}
