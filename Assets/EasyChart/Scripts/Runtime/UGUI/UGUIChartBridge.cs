using UnityEngine;
using UnityEngine.UIElements;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace EasyChart.UGUI
{
    /// <summary>
    /// Render mode for UGUIChartBridge.
    /// </summary>
    public enum ChartRenderMode
    {
        /// <summary>
        /// Screen Space Overlay mode. Best quality, no RenderTexture.
        /// Visible in Game view only.
        /// </summary>
        ScreenSpaceOverlay,
        
        /// <summary>
        /// World Space mode using RenderTexture.
        /// Visible in both Scene and Game views.
        /// May have slight quality differences due to RenderTexture.
        /// </summary>
        WorldSpace
    }

    /// <summary>
    /// Bridge component that positions a ChartElement (UI Toolkit) to overlay a UGUI RectTransform.
    /// Supports both Screen Space Overlay mode (default, best quality) and World Space mode (visible in Scene view).
    /// 
    /// Screen Space Overlay: Renders directly without RenderTexture, best quality.
    /// World Space: Uses RenderTexture, visible in Scene view but may have slight quality differences.
    /// 
    /// Requires Unity 2021.2+.
    /// </summary>
    [AddComponentMenu("EasyChart/UGUI Chart Bridge")]
    [ExecuteAlways]
    public class UGUIChartBridge : MonoBehaviour
    {
        [Header("Chart Configuration")]
        [Tooltip("The ChartProfile to display")]
        [SerializeField] private ChartProfile _profile;

        [Tooltip("PanelSettings asset for UI Toolkit rendering. REQUIRED for proper font rendering.")]
        [SerializeField] private PanelSettings _panelSettingsAsset;

        [Header("Render Settings")]
        [Tooltip("Render mode for the chart.\nScreen Space Overlay: Best quality, visible in Game view only.\nWorld Space: Visible in Scene view, uses RenderTexture.")]
        [SerializeField] private ChartRenderMode _renderMode = ChartRenderMode.ScreenSpaceOverlay;

        [Header("Layer Settings")]
        [Tooltip("Sort order for the UIDocument panel. Higher values render on top. (Screen Space Overlay only)")]
        [SerializeField] private int _sortOrder = 100;

        [Header("Debug")]
        [Tooltip("If enabled, logs size/resolution changes (only when changed).")]
        [SerializeField] private bool _logResolutionChanges = false;

        private UIDocument _uiDocument;
        private ChartElement _chartElement;
        private RectTransform _rectTransform;
        private Canvas _parentCanvas;
        private VisualElement _rootContainer;
        private PanelSettings _runtimePanelSettings;
        private bool _createdPanelSettings;
        private bool _isInitialized;

        private bool _loggedOnce;

        private Rect _lastScreenRect;
        
        // World Space mode fields
        private RenderTexture _renderTexture;
        private UnityEngine.UI.RawImage _rawImage;
        private ChartRenderMode _lastRenderMode;

        /// <summary>
        /// The ChartProfile being displayed.
        /// </summary>
        public ChartProfile Profile
        {
            get => _profile;
            set
            {
                if (_profile != value)
                {
                    _profile = value;
                    UpdateChartProfile();
                }
            }
        }

        /// <summary>
        /// The underlying ChartElement instance.
        /// </summary>
        public ChartElement ChartElement => _chartElement;

        /// <summary>
        /// Sort order for the UIDocument panel.
        /// </summary>
        public int SortOrder
        {
            get => _sortOrder;
            set
            {
                if (_sortOrder != value)
                {
                    _sortOrder = value;
                    if (_uiDocument != null)
                    {
                        _uiDocument.sortingOrder = _sortOrder;
                    }
                }
            }
        }

        /// <summary>
        /// Render mode for the chart.
        /// </summary>
        public ChartRenderMode RenderMode
        {
            get => _renderMode;
            set
            {
                if (_renderMode != value)
                {
                    _renderMode = value;
                    if (_isInitialized)
                    {
                        // Need to reinitialize with new mode
                        CleanupUIDocument();
                        CleanupWorldSpace();
                        _isInitialized = false;
                    }
                }
            }
        }

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            _parentCanvas = GetComponentInParent<Canvas>();
        }

        private void OnEnable()
        {
            _rectTransform = GetComponent<RectTransform>();
            _parentCanvas = GetComponentInParent<Canvas>();

            _lastScreenRect = new Rect(float.NaN, float.NaN, float.NaN, float.NaN);

            _loggedOnce = false;

            // Delay initialization to next frame to avoid issues during activation
            _isInitialized = false;
        }

        private void OnDisable()
        {
            // Don't cleanup during activation/deactivation - use OnDestroy instead
            _isInitialized = false;

            _lastScreenRect = new Rect(float.NaN, float.NaN, float.NaN, float.NaN);
            
            // Clear visual elements but don't destroy components
            if (_chartElement != null)
            {
                _chartElement.RemoveFromHierarchy();
                _chartElement = null;
            }
            _rootContainer = null;
        }

        private void OnDestroy()
        {
            CleanupUIDocument();
            CleanupWorldSpace();
        }

        private void LateUpdate()
        {
            // Check if render mode changed
            if (_lastRenderMode != _renderMode && _isInitialized)
            {
                CleanupUIDocument();
                CleanupWorldSpace();
                _isInitialized = false;
            }
            _lastRenderMode = _renderMode;

            // Initialize on first LateUpdate to ensure everything is ready
            if (!_isInitialized)
            {
                _isInitialized = true;
                if (_renderMode == ChartRenderMode.ScreenSpaceOverlay)
                {
                    CreateUIDocument();
                }
                else
                {
                    CreateWorldSpaceUI();
                }
                UpdateChartProfile();
            }

            if (_logResolutionChanges && !_loggedOnce)
            {
                _loggedOnce = true;
                LogDebugSizes();
            }
            
            if (_renderMode == ChartRenderMode.ScreenSpaceOverlay)
            {
                UpdatePosition();
            }
            else
            {
                UpdateWorldSpaceRenderTexture();
            }
        }

        private void OnValidate()
        {
            if (isActiveAndEnabled && _isInitialized)
            {
                if (_uiDocument != null)
                {
                    _uiDocument.sortingOrder = _sortOrder;
                }
                UpdateChartProfile();
            }
        }

        private void CreateUIDocument()
        {
            // Check if UIDocument already exists on this GameObject
            _uiDocument = GetComponent<UIDocument>();
            
            if (_uiDocument == null)
            {
                _uiDocument = gameObject.AddComponent<UIDocument>();
            }

            // Setup PanelSettings
            if (_panelSettingsAsset != null)
            {
                // Create a runtime copy to avoid modifying the asset and to ensure consistent scaling.
                _runtimePanelSettings = ScriptableObject.Instantiate(_panelSettingsAsset);
                _runtimePanelSettings.name = $"UGUIChartBridge_PanelSettings_{GetInstanceID()}";
                _runtimePanelSettings.hideFlags = HideFlags.DontSave;
                _runtimePanelSettings.scaleMode = PanelScaleMode.ConstantPixelSize;
                _createdPanelSettings = true;
            }
            else
            {
                // Only create if we don't have one
                if (_runtimePanelSettings == null)
                {
                    _runtimePanelSettings = ScriptableObject.CreateInstance<PanelSettings>();
                    _runtimePanelSettings.name = $"UGUIChartBridge_PanelSettings_{GetInstanceID()}";
                    _runtimePanelSettings.scaleMode = PanelScaleMode.ConstantPixelSize;
                    _runtimePanelSettings.hideFlags = HideFlags.DontSave;
                    _createdPanelSettings = true;
                }
            }

            _uiDocument.panelSettings = _runtimePanelSettings;
            _uiDocument.sortingOrder = _sortOrder;

            // Wait for rootVisualElement to be available
            if (_uiDocument.rootVisualElement == null)
            {
                // Will retry on next frame
                _isInitialized = false;
                return;
            }

            // Create root container that will be positioned to match RectTransform
            _rootContainer = new VisualElement();
            _rootContainer.name = "ChartBridgeRoot";
            _rootContainer.style.position = Position.Absolute;
            _rootContainer.pickingMode = PickingMode.Ignore;

            // Create ChartElement
            _chartElement = new ChartElement();
            // Let ChartElement use its profile dimensions; Scale affects display only
            _chartElement.IgnoreProfileSize = false;
            _chartElement.style.flexGrow = 1;
            _chartElement.style.width = Length.Percent(100);
            _chartElement.style.height = Length.Percent(100);

            _rootContainer.Add(_chartElement);
            _uiDocument.rootVisualElement.Add(_rootContainer);

            _lastScreenRect = new Rect(float.NaN, float.NaN, float.NaN, float.NaN);
        }

        private void CleanupUIDocument()
        {
            if (_chartElement != null)
            {
                _chartElement.RemoveFromHierarchy();
                _chartElement = null;
            }

            _rootContainer = null;

            // Use delayed destruction to avoid issues during activation/deactivation
            if (_uiDocument != null)
            {
#if UNITY_EDITOR
                if (!Application.isPlaying)
                {
                    EditorApplication.delayCall += () =>
                    {
                        if (_uiDocument != null)
                            DestroyImmediate(_uiDocument);
                    };
                }
                else
#endif
                {
                    Destroy(_uiDocument);
                }
                _uiDocument = null;
            }

            if (_runtimePanelSettings != null && _createdPanelSettings)
            {
#if UNITY_EDITOR
                if (!Application.isPlaying)
                {
                    var ps = _runtimePanelSettings;
                    EditorApplication.delayCall += () =>
                    {
                        if (ps != null)
                            DestroyImmediate(ps);
                    };
                }
                else
#endif
                {
                    Destroy(_runtimePanelSettings);
                }
            }
            _runtimePanelSettings = null;
            _createdPanelSettings = false;
        }

        private void UpdatePosition()
        {
            if (_rectTransform == null || _rootContainer == null) return;

            // Get screen rect of the RectTransform
            Rect screenRect = GetScreenRect();

            // Only update if changed
            if (screenRect == _lastScreenRect) return;
            _lastScreenRect = screenRect;

            // Use profile dimensions for container size, screenRect for position
            Vector2Int profileSize = GetPixelAdjustedSize();

            if (_logResolutionChanges)
            {
                Vector2 sizeDelta = _rectTransform.sizeDelta;
                Rect localRect = _rectTransform.rect;
                Vector3 lossyScale = _rectTransform.lossyScale;
                Debug.Log($"[UGUIChartBridge] ScreenRect: {Mathf.RoundToInt(screenRect.width)}x{Mathf.RoundToInt(screenRect.height)} | " +
                          $"ProfileSize: {profileSize.x}x{profileSize.y} | " +
                          $"Rect: {Mathf.RoundToInt(localRect.width)}x{Mathf.RoundToInt(localRect.height)} | " +
                          $"SizeDelta: {Mathf.RoundToInt(sizeDelta.x)}x{Mathf.RoundToInt(sizeDelta.y)} | " +
                          $"LossyScale: {lossyScale.x:F3},{lossyScale.y:F3}");
            }

            // Position the root container to match the RectTransform's screen position
            // Container uses profile dimensions, CSS transform scale stretches to screen size
            _rootContainer.style.left = screenRect.x;
            _rootContainer.style.top = Screen.height - screenRect.y - screenRect.height; // UI Toolkit Y is from top
            _rootContainer.style.width = profileSize.x;
            _rootContainer.style.height = profileSize.y;

            // Apply CSS transform scale to stretch profile-sized content to screen size
            float scaleX = screenRect.width / profileSize.x;
            float scaleY = screenRect.height / profileSize.y;
            _rootContainer.style.transformOrigin = new TransformOrigin(0, 0);
            _rootContainer.transform.scale = new Vector3(scaleX, scaleY, 1f);
        }

        private Rect GetScreenRect()
        {
            if (_rectTransform == null) return Rect.zero;

            // Get the four corners of the RectTransform in world space
            Vector3[] corners = new Vector3[4];
            _rectTransform.GetWorldCorners(corners);

            // Convert to screen space
            Camera cam = null;
            if (_parentCanvas != null)
            {
                if (_parentCanvas.renderMode == UnityEngine.RenderMode.ScreenSpaceCamera)
                    cam = _parentCanvas.worldCamera;
                else if (_parentCanvas.renderMode == UnityEngine.RenderMode.WorldSpace)
                    cam = _parentCanvas.worldCamera ?? Camera.main;
            }

            Vector2 min = new Vector2(float.MaxValue, float.MaxValue);
            Vector2 max = new Vector2(float.MinValue, float.MinValue);

            for (int i = 0; i < 4; i++)
            {
                Vector2 screenPoint;
                if (cam != null)
                    screenPoint = RectTransformUtility.WorldToScreenPoint(cam, corners[i]);
                else
                    screenPoint = RectTransformUtility.WorldToScreenPoint(null, corners[i]);

                min = Vector2.Min(min, screenPoint);
                max = Vector2.Max(max, screenPoint);
            }

            return new Rect(min.x, min.y, max.x - min.x, max.y - min.y);
        }

        private Vector2Int GetPixelAdjustedSize()
        {
            // Always use ChartProfile's fixed dimensions for rendering
            // Scale is applied to the final display, not the render resolution
            if (_profile != null && _profile.chartWidth > 0 && _profile.chartHeight > 0)
            {
                int pw = Mathf.Max(1, Mathf.RoundToInt(_profile.chartWidth));
                int ph = Mathf.Max(1, Mathf.RoundToInt(_profile.chartHeight));
                return new Vector2Int(pw, ph);
            }

            // Fallback to RectTransform size if profile dimensions are not set
            if (_rectTransform == null) return Vector2Int.one;

            int width = Mathf.Max(1, Mathf.RoundToInt(_rectTransform.rect.width));
            int height = Mathf.Max(1, Mathf.RoundToInt(_rectTransform.rect.height));
            return new Vector2Int(width, height);
        }

        private void UpdateChartProfile()
        {
            if (_chartElement == null) return;
            _chartElement.Profile = _profile;
        }

        [ContextMenu("Log Debug Sizes")]
        private void LogDebugSizes()
        {
            if (_rectTransform == null) _rectTransform = GetComponent<RectTransform>();
            if (_parentCanvas == null) _parentCanvas = GetComponentInParent<Canvas>();

            Rect screenRect = GetScreenRect();
            Vector2Int pixelAdjusted = GetPixelAdjustedSize();

            Vector2 sizeDelta = _rectTransform != null ? _rectTransform.sizeDelta : Vector2.zero;
            Rect localRect = _rectTransform != null ? _rectTransform.rect : Rect.zero;
            Vector3 lossyScale = _rectTransform != null ? _rectTransform.lossyScale : Vector3.one;
            Vector2 anchorMin = _rectTransform != null ? _rectTransform.anchorMin : Vector2.zero;
            Vector2 anchorMax = _rectTransform != null ? _rectTransform.anchorMax : Vector2.zero;
            Vector2 pivot = _rectTransform != null ? _rectTransform.pivot : Vector2.zero;

            string canvasInfo = _parentCanvas == null
                ? "Canvas: null"
                : $"Canvas: {_parentCanvas.name} ({_parentCanvas.renderMode})";

            Debug.Log(
                $"[UGUIChartBridge] RenderMode={_renderMode} | {canvasInfo} | " +
                $"ScreenRect={Mathf.RoundToInt(screenRect.width)}x{Mathf.RoundToInt(screenRect.height)} | " +
                $"PixelAdjusted={pixelAdjusted.x}x{pixelAdjusted.y} | " +
                $"Rect={Mathf.RoundToInt(localRect.width)}x{Mathf.RoundToInt(localRect.height)} | " +
                $"SizeDelta={Mathf.RoundToInt(sizeDelta.x)}x{Mathf.RoundToInt(sizeDelta.y)} | " +
                $"Anchors=({anchorMin.x:F3},{anchorMin.y:F3})-({anchorMax.x:F3},{anchorMax.y:F3}) | " +
                $"Pivot=({pivot.x:F3},{pivot.y:F3}) | " +
                $"LossyScale=({lossyScale.x:F3},{lossyScale.y:F3})");
        }

        /// <summary>
        /// Force refresh the chart display.
        /// </summary>
        public void Refresh()
        {
            UpdatePosition();
            if (_chartElement != null)
            {
                _chartElement.ForceRefreshProfile();
            }
        }

        #region World Space Mode

        private RenderTexture CreateRenderTexture(int width, int height)
        {
            var rt = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32);
            rt.name = $"UGUIChartBridge_RT_{GetInstanceID()}";
            rt.antiAliasing = 2;
            rt.hideFlags = HideFlags.DontSave;
            rt.Create();
            return rt;
        }

        private void CreateWorldSpaceUI()
        {
            if (_rectTransform == null) return;

            // Create RenderTexture
            Vector2Int size = GetPixelAdjustedSize();
            int width = size.x;
            int height = size.y;
            
            _renderTexture = CreateRenderTexture(width, height);

            // Create UIDocument for off-screen rendering
            _uiDocument = GetComponent<UIDocument>();
            if (_uiDocument == null)
            {
                _uiDocument = gameObject.AddComponent<UIDocument>();
            }

            // Setup PanelSettings with RenderTexture target
            // For World Space mode, we need to create a copy of the panel settings to set targetTexture
            if (_panelSettingsAsset != null)
            {
                // Create a runtime copy of the provided panel settings
                _runtimePanelSettings = ScriptableObject.Instantiate(_panelSettingsAsset);
                _runtimePanelSettings.name = $"UGUIChartBridge_WorldSpace_PanelSettings_{GetInstanceID()}";
                _runtimePanelSettings.hideFlags = HideFlags.DontSave;
                _runtimePanelSettings.scaleMode = PanelScaleMode.ConstantPixelSize;
                _createdPanelSettings = true;
            }
            else if (_runtimePanelSettings == null)
            {
                _runtimePanelSettings = ScriptableObject.CreateInstance<PanelSettings>();
                _runtimePanelSettings.name = $"UGUIChartBridge_WorldSpace_PanelSettings_{GetInstanceID()}";
                _runtimePanelSettings.scaleMode = PanelScaleMode.ConstantPixelSize;
                _runtimePanelSettings.hideFlags = HideFlags.DontSave;
                _createdPanelSettings = true;
            }
            
            // Set target texture for World Space rendering
            _runtimePanelSettings.targetTexture = _renderTexture;
            _runtimePanelSettings.clearColor = true;

            _uiDocument.panelSettings = _runtimePanelSettings;

            // Wait for rootVisualElement to be available
            if (_uiDocument.rootVisualElement == null)
            {
                _isInitialized = false;
                return;
            }

            // Create root container
            _rootContainer = new VisualElement();
            _rootContainer.name = "ChartBridgeRoot";
            _rootContainer.style.position = Position.Absolute;
            _rootContainer.style.left = 0;
            _rootContainer.style.top = 0;
            _rootContainer.style.width = width;
            _rootContainer.style.height = height;
            _rootContainer.pickingMode = PickingMode.Ignore;

            // Create ChartElement
            _chartElement = new ChartElement();
            // Let ChartElement use its profile dimensions; Scale affects display only
            _chartElement.IgnoreProfileSize = false;
            _chartElement.style.flexGrow = 1;
            _chartElement.style.width = Length.Percent(100);
            _chartElement.style.height = Length.Percent(100);

            _rootContainer.Add(_chartElement);
            _uiDocument.rootVisualElement.Add(_rootContainer);

            // Create RawImage to display the RenderTexture
            _rawImage = GetComponent<UnityEngine.UI.RawImage>();
            if (_rawImage == null)
            {
                _rawImage = gameObject.AddComponent<UnityEngine.UI.RawImage>();
            }
            _rawImage.texture = _renderTexture;
            _rawImage.raycastTarget = false;

            _lastScreenRect = new Rect(float.NaN, float.NaN, float.NaN, float.NaN);
        }

        private void UpdateWorldSpaceRenderTexture()
        {
            if (_rectTransform == null || _renderTexture == null) return;

            // Check if size changed
            Vector2Int size = GetPixelAdjustedSize();
            int width = size.x;
            int height = size.y;

            if (_renderTexture.width != width || _renderTexture.height != height)
            {
                if (_logResolutionChanges)
                {
                    Debug.Log($"[UGUIChartBridge] Recreate RenderTexture: {_renderTexture.width}x{_renderTexture.height} -> {width}x{height}");
                }

                // Recreate RenderTexture (more reliable than resizing the same instance for UI Toolkit panels)
                var oldRt = _renderTexture;
                _renderTexture = CreateRenderTexture(width, height);

                if (_runtimePanelSettings != null)
                {
                    _runtimePanelSettings.targetTexture = _renderTexture;
                }

                // Update root container size
                if (_rootContainer != null)
                {
                    _rootContainer.style.width = width;
                    _rootContainer.style.height = height;
                }

                // Update RawImage
                if (_rawImage != null)
                {
                    _rawImage.texture = _renderTexture;
                }

                if (oldRt != null)
                {
                    oldRt.Release();
#if UNITY_EDITOR
                    if (!Application.isPlaying)
                    {
                        var rt = oldRt;
                        EditorApplication.delayCall += () =>
                        {
                            if (rt != null)
                                DestroyImmediate(rt);
                        };
                    }
                    else
#endif
                    {
                        Destroy(oldRt);
                    }
                }
            }
        }

        private void CleanupWorldSpace()
        {
            if (_rawImage != null)
            {
#if UNITY_EDITOR
                if (!Application.isPlaying)
                {
                    var img = _rawImage;
                    EditorApplication.delayCall += () =>
                    {
                        if (img != null)
                            DestroyImmediate(img);
                    };
                }
                else
#endif
                {
                    Destroy(_rawImage);
                }
                _rawImage = null;
            }

            if (_renderTexture != null)
            {
                _renderTexture.Release();
#if UNITY_EDITOR
                if (!Application.isPlaying)
                {
                    var rt = _renderTexture;
                    EditorApplication.delayCall += () =>
                    {
                        if (rt != null)
                            DestroyImmediate(rt);
                    };
                }
                else
#endif
                {
                    Destroy(_renderTexture);
                }
                _renderTexture = null;
            }
        }

        #endregion
    }
}
