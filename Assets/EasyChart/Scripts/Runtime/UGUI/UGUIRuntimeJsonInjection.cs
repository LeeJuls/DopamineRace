using UnityEngine;

namespace EasyChart.UGUI
{
    /// <summary>
    /// Runtime component for testing JSON data injection into UGUIChartBridge.
    /// Provides an Inspector interface similar to the EasyChart Library Window's JSON Injection panel.
    /// </summary>
    [RequireComponent(typeof(UGUIChartBridge))]
    [AddComponentMenu("EasyChart/UGUI Runtime JSON Injection")]
    public class UGUIRuntimeJsonInjection : MonoBehaviour
    {
        [Header("JSON Generation Settings")]
        [Tooltip("The format mode for generating JSON\n• Compact: Data values only\n• Standard: Names + structured data\n• Full: All metadata including axes, types")]
        [SerializeField] private ChartJsonMode _jsonMode = ChartJsonMode.Standard;

        [Tooltip("Wrap JSON in API response envelope")]
        [SerializeField] private bool _useApiEnvelope = false;

        [Tooltip("Automatically regenerate JSON when JSON Mode or API Envelope changes")]
        [SerializeField] private bool _autoGenerateJson = false;

        [Header("JSON Content")]
        [Tooltip("The JSON string to inject into the chart")]
        [TextArea(10, 30)]
        [SerializeField] private string _jsonContent = "";

        private UGUIChartBridge _bridge;
        private ChartProfile _runtimeProfile;

        /// <summary>
        /// The JSON content to inject.
        /// </summary>
        public string JsonContent
        {
            get => _jsonContent;
            set => _jsonContent = value;
        }

        /// <summary>
        /// The JSON generation mode (Compact, Standard, or Full).
        /// Automatically regenerates JSON if AutoGenerateJson is enabled.
        /// </summary>
        public ChartJsonMode JsonMode
        {
            get => _jsonMode;
            set
            {
                _jsonMode = value;
                if (_autoGenerateJson) GenerateExampleJson();
            }
        }

        /// <summary>
        /// Whether to wrap JSON in API response envelope.
        /// Automatically regenerates JSON if AutoGenerateJson is enabled.
        /// </summary>
        public bool UseApiEnvelope
        {
            get => _useApiEnvelope;
            set
            {
                _useApiEnvelope = value;
                if (_autoGenerateJson) GenerateExampleJson();
            }
        }

        /// <summary>
        /// Event fired when JSON is applied to the chart. Parameter indicates success.
        /// </summary>
        public event System.Action<bool, string> OnJsonApplied;

        /// <summary>
        /// Whether to automatically regenerate JSON when ExampleMode or DatasMode changes.
        /// </summary>
        public bool AutoGenerateJson
        {
            get => _autoGenerateJson;
            set => _autoGenerateJson = value;
        }

        private void Awake()
        {
            _bridge = GetComponent<UGUIChartBridge>();
        }

        /// <summary>
        /// Apply the current JSON content to the chart.
        /// </summary>
        public void ApplyJsonToChart()
        {
            if (_bridge == null)
            {
                _bridge = GetComponent<UGUIChartBridge>();
            }

            if (_bridge == null || _bridge.Profile == null)
            {
                Debug.LogWarning("[UGUIRuntimeJsonInjection] No UGUIChartBridge or ChartProfile found.");
                return;
            }

            if (string.IsNullOrWhiteSpace(_jsonContent))
            {
                Debug.LogWarning("[UGUIRuntimeJsonInjection] JSON content is empty.");
                return;
            }

            // Use validation method for detailed error reporting
            var validationResult = ChartJsonUtils.ValidateAndParseFeed(_jsonContent);
            
            // Log warnings if any
            if (validationResult.Warnings.Count > 0)
            {
                Debug.LogWarning($"[UGUIRuntimeJsonInjection] {validationResult.GetWarningSummary()}");
            }
            
            // Check for errors
            if (!validationResult.IsValid)
            {
                var error = $"[UGUIRuntimeJsonInjection] {validationResult.GetErrorSummary()}";
                Debug.LogError(error);
                OnJsonApplied?.Invoke(false, error);
                return;
            }

            // Clone profile on first injection to avoid modifying the original asset
            EnsureRuntimeProfile();

            ChartJsonUtils.ApplyFeedToProfile(_runtimeProfile, validationResult.Feed);
            
            // Refresh the chart
            _bridge.Refresh();
            var message = "[UGUIRuntimeJsonInjection] JSON applied to chart.";
            Debug.Log(message);
            OnJsonApplied?.Invoke(true, message);
        }

        /// <summary>
        /// Directly apply a parsed feed to the chart. More efficient for periodic updates
        /// when you already have the feed object (avoids re-parsing JSON each time).
        /// </summary>
        /// <param name="feed">The parsed ChartFeed to apply</param>
        /// <returns>True if successful, false otherwise</returns>
        public bool ApplyFeed(ChartFeed feed)
        {
            if (feed == null)
            {
                Debug.LogWarning("[UGUIRuntimeJsonInjection] Feed is null.");
                return false;
            }

            if (_bridge == null)
            {
                _bridge = GetComponent<UGUIChartBridge>();
            }

            if (_bridge == null || _bridge.Profile == null)
            {
                Debug.LogWarning("[UGUIRuntimeJsonInjection] No UGUIChartBridge or ChartProfile found.");
                return false;
            }

            EnsureRuntimeProfile();
            ChartJsonUtils.ApplyFeedToProfile(_runtimeProfile, feed);
            _bridge.Refresh();
            return true;
        }

        /// <summary>
        /// Ensures we have a runtime copy of the profile to avoid modifying the original asset.
        /// </summary>
        private void EnsureRuntimeProfile()
        {
            var originalProfile = _bridge.Profile;
            if (originalProfile == null) return;

            // Check if profile is already a runtime copy (has DontSave hideFlags)
            if ((originalProfile.hideFlags & HideFlags.DontSave) != 0)
            {
                _runtimeProfile = originalProfile;
                return;
            }

            // Create a runtime copy
            _runtimeProfile = Object.Instantiate(originalProfile);
            _runtimeProfile.name = originalProfile.name + " (Runtime)";
            _runtimeProfile.hideFlags = HideFlags.DontSave;

            // Assign the cloned profile to the bridge
            _bridge.Profile = _runtimeProfile;

            Debug.Log($"[UGUIRuntimeJsonInjection] Created runtime copy of profile: {_runtimeProfile.name}");
        }

        /// <summary>
        /// Generate example JSON from the current chart profile configuration.
        /// This exports the current profile settings to JSON format for reference.
        /// </summary>
        /// <returns>The generated JSON string, or null if generation failed.</returns>
        public string GenerateExampleJson()
        {
            if (_bridge == null)
            {
                _bridge = GetComponent<UGUIChartBridge>();
            }

            if (_bridge == null || _bridge.Profile == null)
            {
                Debug.LogWarning("[UGUIRuntimeJsonInjection] No UGUIChartBridge or ChartProfile found.");
                return null;
            }

            var profile = _bridge.Profile;
            profile.EnsureRuntimeData();

            string json = ChartJsonUtils.BuildJson(profile, _jsonMode, profile.chartId);
            _jsonContent = _useApiEnvelope ? ChartJsonUtils.WrapAsApiResponse(json) : json;
            return _jsonContent;
        }
    }
}
