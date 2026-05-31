using UnityEngine;
using UnityEngine.UIElements;

namespace EasyChart.UIToolKit
{
    [RequireComponent(typeof(UIDocument))]
    [AddComponentMenu("EasyChart/UI Toolkit Runtime JSON Injection")]
    public class UIToolKitRuntimeJsonInjection : MonoBehaviour
    {
        [Header("Target")]
        [Tooltip("Optional. If set, finds the ChartElement by name from UIDocument.rootVisualElement.")]
        [SerializeField] private string _chartElementName = "";

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

        private UIDocument _uiDocument;
        private ChartProfile _runtimeProfile;

        public string ChartElementName
        {
            get => _chartElementName;
            set => _chartElementName = value;
        }

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

        public bool AutoGenerateJson
        {
            get => _autoGenerateJson;
            set => _autoGenerateJson = value;
        }

        private void Awake()
        {
            _uiDocument = GetComponent<UIDocument>();
        }

        public bool TryGetChartElement(out EasyChart.ChartElement chartElement)
        {
            chartElement = null;

            if (_uiDocument == null)
            {
                _uiDocument = GetComponent<UIDocument>();
            }

            if (_uiDocument == null)
            {
                Debug.LogWarning("[UIToolKitRuntimeJsonInjection] No UIDocument found.");
                return false;
            }

            var root = _uiDocument.rootVisualElement;
            if (root == null)
            {
                Debug.LogWarning("[UIToolKitRuntimeJsonInjection] UIDocument.rootVisualElement is null.");
                return false;
            }

            if (!string.IsNullOrEmpty(_chartElementName))
            {
                // First try: direct ChartElement by name
                chartElement = root.Q<EasyChart.ChartElement>(_chartElementName);
                
                // Second try: TemplateContainer by name, then get ChartElement inside
                if (chartElement == null)
                {
                    var container = root.Q<VisualElement>(_chartElementName);
                    if (container != null)
                    {
                        chartElement = container.Q<EasyChart.ChartElement>();
                    }
                }
            }

            if (chartElement == null)
            {
                chartElement = root.Q<EasyChart.ChartElement>();
            }

            if (chartElement == null)
            {
                Debug.LogWarning("[UIToolKitRuntimeJsonInjection] No ChartElement found in UIDocument.");
                return false;
            }

            return true;
        }

        public void ApplyJsonToChart()
        {
            if (!TryGetChartElement(out var chartElement) || chartElement.Profile == null)
            {
                Debug.LogWarning("[UIToolKitRuntimeJsonInjection] No ChartElement or ChartProfile found.");
                return;
            }

            if (string.IsNullOrWhiteSpace(_jsonContent))
            {
                Debug.LogWarning("[UIToolKitRuntimeJsonInjection] JSON content is empty.");
                return;
            }

            // Use validation method for detailed error reporting
            var validationResult = ChartJsonUtils.ValidateAndParseFeed(_jsonContent);
            
            // Log warnings if any
            if (validationResult.Warnings.Count > 0)
            {
                Debug.LogWarning($"[UIToolKitRuntimeJsonInjection] {validationResult.GetWarningSummary()}");
            }
            
            // Check for errors
            if (!validationResult.IsValid)
            {
                var error = $"[UIToolKitRuntimeJsonInjection] {validationResult.GetErrorSummary()}";
                Debug.LogError(error);
                OnJsonApplied?.Invoke(false, error);
                return;
            }

            // Clone profile on first injection to avoid modifying the original asset
            EnsureRuntimeProfile(chartElement);

            ChartJsonUtils.ApplyFeedToProfile(_runtimeProfile, validationResult.Feed);
            chartElement.ForceRefreshProfile();
            var message = "[UIToolKitRuntimeJsonInjection] JSON applied to chart.";
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
                Debug.LogWarning("[UIToolKitRuntimeJsonInjection] Feed is null.");
                return false;
            }

            if (!TryGetChartElement(out var chartElement) || chartElement.Profile == null)
            {
                Debug.LogWarning("[UIToolKitRuntimeJsonInjection] No ChartElement or ChartProfile found.");
                return false;
            }

            EnsureRuntimeProfile(chartElement);
            ChartJsonUtils.ApplyFeedToProfile(_runtimeProfile, feed);
            chartElement.ForceRefreshProfile();
            return true;
        }

        /// <summary>
        /// Ensures we have a runtime copy of the profile to avoid modifying the original asset.
        /// </summary>
        private void EnsureRuntimeProfile(ChartElement chartElement)
        {
            var originalProfile = chartElement.Profile;
            if (originalProfile == null) return;

            // Check if this specific chart element already has a runtime copy
            // We detect this by checking if the profile has DontSave hideFlags
            if ((originalProfile.hideFlags & HideFlags.DontSave) != 0)
            {
                // Already a runtime copy, just update our reference
                _runtimeProfile = originalProfile;
                return;
            }

            // Create a runtime copy
            _runtimeProfile = Object.Instantiate(originalProfile);
            _runtimeProfile.name = originalProfile.name + " (Runtime)";
            _runtimeProfile.hideFlags = HideFlags.DontSave;

            // Assign the cloned profile to the chart element
            chartElement.Profile = _runtimeProfile;

            Debug.Log($"[UIToolKitRuntimeJsonInjection] Created runtime copy of profile: {_runtimeProfile.name}");
        }

        public string GenerateExampleJson()
        {
            if (!TryGetChartElement(out var chartElement) || chartElement.Profile == null)
            {
                Debug.LogWarning("[UIToolKitRuntimeJsonInjection] No ChartElement or ChartProfile found.");
                return null;
            }

            var profile = chartElement.Profile;
            profile.EnsureRuntimeData();

            string json = ChartJsonUtils.BuildJson(profile, _jsonMode, profile.chartId);
            _jsonContent = _useApiEnvelope ? ChartJsonUtils.WrapAsApiResponse(json) : json;
            return _jsonContent;
        }
    }
}
