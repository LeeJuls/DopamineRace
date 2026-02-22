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
        [Tooltip("The format mode for generating example JSON")]
        [SerializeField] private ChartJsonExampleMode _exampleMode = ChartJsonExampleMode.Standard;

        [Tooltip("The data format mode for series data")]
        [SerializeField] private ChartJsonDatasMode _datasMode = ChartJsonDatasMode.Standard;

        [Tooltip("Wrap JSON in API response envelope")]
        [SerializeField] private bool _useApiEnvelope = false;

        [Tooltip("Automatically regenerate JSON when ExampleMode or DatasMode changes")]
        [SerializeField] private bool _autoGenerateJson = false;

        [Header("JSON Content")]
        [Tooltip("The JSON string to inject into the chart")]
        [TextArea(10, 30)]
        [SerializeField] private string _jsonContent = "";

        private UGUIChartBridge _bridge;
        private ChartProfile _runtimeProfile;
        private bool _profileCloned;

        /// <summary>
        /// The JSON content to inject.
        /// </summary>
        public string JsonContent
        {
            get => _jsonContent;
            set => _jsonContent = value;
        }

        /// <summary>
        /// The example mode for JSON generation.
        /// </summary>
        public ChartJsonExampleMode ExampleMode
        {
            get => _exampleMode;
            set => _exampleMode = value;
        }

        /// <summary>
        /// The data format mode for series data.
        /// </summary>
        public ChartJsonDatasMode DatasMode
        {
            get => _datasMode;
            set => _datasMode = value;
        }

        /// <summary>
        /// Whether to wrap JSON in API response envelope.
        /// </summary>
        public bool UseApiEnvelope
        {
            get => _useApiEnvelope;
            set => _useApiEnvelope = value;
        }

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

            string json = _jsonContent;

            // Try to extract data from API envelope if present
            if (ChartJsonUtils.TryExtractWrappedDataJson(json, out var dataJson) && !string.IsNullOrEmpty(dataJson))
            {
                json = dataJson;
            }

            // Deserialize and apply JSON to profile
            if (!ChartJsonUtils.TryDeserializeFeed(json, out var feed))
            {
                Debug.LogError("[UGUIRuntimeJsonInjection] Failed to parse JSON.");
                return;
            }

            // Clone profile on first injection to avoid modifying the original asset
            EnsureRuntimeProfile();

            ChartJsonUtils.ApplyFeedToProfile(_runtimeProfile, feed);
            
            // Refresh the chart
            _bridge.Refresh();
            Debug.Log("[UGUIRuntimeJsonInjection] JSON applied to chart.");
        }

        /// <summary>
        /// Ensures we have a runtime copy of the profile to avoid modifying the original asset.
        /// </summary>
        private void EnsureRuntimeProfile()
        {
            if (_profileCloned) return;

            var originalProfile = _bridge.Profile;
            if (originalProfile == null) return;

            // Create a runtime copy
            _runtimeProfile = Object.Instantiate(originalProfile);
            _runtimeProfile.name = originalProfile.name + " (Runtime)";
            _runtimeProfile.hideFlags = HideFlags.DontSave;

            // Assign the cloned profile to the bridge
            _bridge.Profile = _runtimeProfile;
            _profileCloned = true;

            Debug.Log($"[UGUIRuntimeJsonInjection] Created runtime copy of profile: {_runtimeProfile.name}");
        }

        /// <summary>
        /// Generate example JSON from the current chart profile.
        /// </summary>
        public void GenerateExampleJson()
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

            var profile = _bridge.Profile;
            profile.EnsureRuntimeData();

            string json = ChartJsonUtils.BuildInjectionJson(profile, profile.chartId, _exampleMode, _datasMode);
            _jsonContent = _useApiEnvelope ? ChartJsonUtils.WrapAsApiResponse(json) : json;
        }
    }
}
