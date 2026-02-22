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

        private UIDocument _uiDocument;
        private ChartProfile _runtimeProfile;
        private bool _profileCloned;

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

        public ChartJsonExampleMode ExampleMode
        {
            get => _exampleMode;
            set => _exampleMode = value;
        }

        public ChartJsonDatasMode DatasMode
        {
            get => _datasMode;
            set => _datasMode = value;
        }

        public bool UseApiEnvelope
        {
            get => _useApiEnvelope;
            set => _useApiEnvelope = value;
        }

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
                chartElement = root.Q<EasyChart.ChartElement>(_chartElementName);
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

            string json = _jsonContent;

            if (ChartJsonUtils.TryExtractWrappedDataJson(json, out var dataJson) && !string.IsNullOrEmpty(dataJson))
            {
                json = dataJson;
            }

            if (!ChartJsonUtils.TryDeserializeFeed(json, out var feed))
            {
                Debug.LogError("[UIToolKitRuntimeJsonInjection] Failed to parse JSON.");
                return;
            }

            // Clone profile on first injection to avoid modifying the original asset
            EnsureRuntimeProfile(chartElement);

            ChartJsonUtils.ApplyFeedToProfile(_runtimeProfile, feed);
            chartElement.ForceRefreshProfile();
            Debug.Log("[UIToolKitRuntimeJsonInjection] JSON applied to chart.");
        }

        /// <summary>
        /// Ensures we have a runtime copy of the profile to avoid modifying the original asset.
        /// </summary>
        private void EnsureRuntimeProfile(ChartElement chartElement)
        {
            if (_profileCloned) return;

            var originalProfile = chartElement.Profile;
            if (originalProfile == null) return;

            // Create a runtime copy
            _runtimeProfile = Object.Instantiate(originalProfile);
            _runtimeProfile.name = originalProfile.name + " (Runtime)";
            _runtimeProfile.hideFlags = HideFlags.DontSave;

            // Assign the cloned profile to the chart element
            chartElement.Profile = _runtimeProfile;
            _profileCloned = true;

            Debug.Log($"[UIToolKitRuntimeJsonInjection] Created runtime copy of profile: {_runtimeProfile.name}");
        }

        public void GenerateExampleJson()
        {
            if (!TryGetChartElement(out var chartElement) || chartElement.Profile == null)
            {
                Debug.LogWarning("[UIToolKitRuntimeJsonInjection] No ChartElement or ChartProfile found.");
                return;
            }

            var profile = chartElement.Profile;
            profile.EnsureRuntimeData();

            string json = ChartJsonUtils.BuildInjectionJson(profile, profile.chartId, _exampleMode, _datasMode);
            _jsonContent = _useApiEnvelope ? ChartJsonUtils.WrapAsApiResponse(json) : json;
        }
    }
}
