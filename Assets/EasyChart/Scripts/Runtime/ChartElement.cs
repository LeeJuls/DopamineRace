using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using EasyChart.Layers;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace EasyChart
{
    public class ChartElement : VisualElement
    {
        /// <summary>
        /// When true, ignores chartWidth/chartHeight from ChartProfile and uses 100% of parent container.
        /// Useful when embedding in UGUIChartBridge or other containers that control sizing externally.
        /// </summary>
        public bool IgnoreProfileSize { get; set; } = false;

        public new class UxmlFactory : UxmlFactory<ChartElement, UxmlTraits> { }
        public new class UxmlTraits : VisualElement.UxmlTraits 
        {
            UxmlStringAttributeDescription m_ProfileName = new UxmlStringAttributeDescription { name = "profile-name", defaultValue = "" };
            UxmlStringAttributeDescription m_ProfileGuid = new UxmlStringAttributeDescription { name = "profile-guid", defaultValue = "" };

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);
                var chart = ve as ChartElement;
                
                string profileGuid = m_ProfileGuid.GetValueFromBag(bag, cc);
#if UNITY_EDITOR
                if (!string.IsNullOrEmpty(profileGuid))
                {
                    if (chart.LoadProfileByGuid(profileGuid))
                    {
                        return;
                    }
                }
#endif

                string profileName = m_ProfileName.GetValueFromBag(bag, cc);
                if (!string.IsNullOrEmpty(profileName))
                {
                    chart.LoadProfileByName(profileName);
                }
            }
        }

        private void ScheduleLayoutRefresh()
        {
            bool showCartesian = Data != null && Data.CoordinateSystem == CoordinateSystemType.Cartesian2D;
            _labelRefresh.Request(
                this,
                showCartesian ? _axisLayer : null,
                _renderers,
                _editor.IsCoordSwitchInProgress,
                EditorDiagnosticsEnabled
            );
        }

        // Layers
        private GridLayer _gridLayer;
        private AxisLayer _axisLayer;
        private VisualElement _chartArea;
        private VisualElement _chartContent;
        private VisualElement _plotViewport;
        private VisualElement _plotContentRoot;
        private VisualElement _backgroundLayer;
        private VisualElement _labelOverlay;
        
        // Dynamic Series Renderers
        private readonly ChartRendererController _rendererController = new ChartRendererController();
        private List<BaseSeriesRenderer> _renderers => _rendererController.Renderers;
        private BarSeriesRenderer _barRenderer;
        private LineSeriesRenderer _lineRenderer;
        private ScatterSeriesRenderer _scatterRenderer;
        private RadarSeriesRenderer _radarRenderer;
        private PieSeriesRenderer _pieRenderer;
        
        private Label _tooltip;
        private VisualElement _cursorLine;

        private readonly List<string> _windowedCategoryLabelsX = new List<string>(16);
        private readonly List<string> _windowedCategoryLabelsY = new List<string>(16);

        private ChartRefreshPipeline _refresh;
        private ChartKernel _kernel;

        internal ChartTooltipController TooltipControllerInternal => _tooltipController;
        internal List<BaseSeriesRenderer> RenderersInternal => _renderers;
        internal ChartRendererController RendererControllerInternal => _rendererController;
        internal VisualElement ChartAreaInternal => _chartArea;
        internal Vector4 PaddingInternal => _padding;
        internal bool IsCartesianTransposedInternal => IsCartesianTransposed();
        internal bool EditorIsDeferredCoordSwitchApplyingInternal => _editor.IsDeferredCoordSwitchApplying;
        internal bool IsCategorySmoothTranslatingInternal => _categoryScroll != null && _categoryScroll.SmoothTranslating;
        internal Label TooltipLabelInternal => _tooltip;
        internal VisualElement CursorLineInternal => _cursorLine;
        internal GridLayer GridLayerInternal => _gridLayer;
        internal AxisLayer AxisLayerInternal => _axisLayer;
        internal ChartAxisGridController AxisGridControllerInternal => _axisGrid;
        internal ChartLegendController LegendControllerInternal => _legend;
        internal ChartProfile ChartProfileInternal => _chartProfile;
        internal IChartLayoutModel LayoutModelInternal => _layoutModel;
        internal CategoryScrollController CategoryScrollControllerInternal => _categoryScroll;
        internal float XMinInternal { get => _xMin; set => _xMin = value; }
        internal float XMaxInternal { get => _xMax; set => _xMax = value; }
        internal float YMinInternal { get => _yMin; set => _yMin = value; }
        internal float YMaxInternal { get => _yMax; set => _yMax = value; }
        internal float CategoryScrollOffsetXInternal => _categoryScroll != null ? _categoryScroll.ScrollOffsetX : 0f;
        internal float CategoryScrollOffsetYInternal => _categoryScroll != null ? _categoryScroll.ScrollOffsetY : 0f;
        internal void ApplyCategoryScrollTranslateInternal(float xPx, float yPx) => ApplyCategoryScrollTranslate(xPx, yPx);
        internal VisualElement BackgroundLayerInternal => _backgroundLayer;
        internal VisualElement PlotViewportInternal => _plotViewport;
        internal VisualElement LabelOverlayInternal => _labelOverlay;
        internal ChartLabelController LabelControllerInternal => _labelController;

        internal AxisId GetMappedXAxisIdInternal() => GetMappedXAxisId();
        internal AxisId GetMappedYAxisIdInternal() => GetMappedYAxisId();
        internal AxisConfig GetAxisInternal(AxisId id) => GetAxis(id);
        internal List<string> GetCategoryLabelsWindowedInternal(AxisId id) => GetCategoryLabelsWindowed(id);

        internal bool EditorShouldDeferVisualWorkInternal() => _editor.ShouldDeferVisualWork();
        internal bool EditorTryDeferVisualWorkInternal(string breadcrumbIfCoordChanged) => _editor.TryDeferVisualWork(breadcrumbIfCoordChanged);
        internal void EditorRefreshLegendDeferredInternal(System.Action refreshLegend) => _editor.RefreshLegendDeferred(refreshLegend);
        internal void EditorGetLegendTraceInternal(out bool trace, out System.Action<string> pushBreadcrumb) => _editor.GetLegendTrace(out trace, out pushBreadcrumb);
        internal void EditorOnRebuildRenderersStartInternal() => _editor.OnRebuildRenderersStart();
        internal void EditorOnRebuildRenderersDoneInternal(int rendererCount, bool showCartesian) => _editor.OnRebuildRenderersDone(rendererCount, showCartesian);
        internal void SetTooltipVisualsInternal(Label tooltip, VisualElement cursorLine)
        {
            _tooltip = tooltip;
            _cursorLine = cursorLine;
        }

        internal bool EditorHandleGeometryChangedInternal(GeometryChangedEvent evt)
        {
            return _editor.HandleGeometryChanged(evt);
        }

        // Data
        private ChartProfile _chartProfile;

        private ChartTheme _theme;

        public ChartTheme Theme
        {
            get => _theme;
            set
            {
                if (ReferenceEquals(_theme, value)) return;
                _theme = value;

                if (_tooltip != null)
                {
                    _tooltip.userData = this;
                    ChartTextStyleApplier.ApplyLabel(_tooltip, this, ChartTextRole.Tooltip);
                }

                ScheduleLayoutRefresh();
                if (_kernel != null)
                {
                    _kernel.QueueStages(ChartRefreshStage.AxisLayer | ChartRefreshStage.SeriesRenderers | ChartRefreshStage.Legend);
                }
            }
        }

        internal ChartTheme GetEffectiveThemeInternal()
        {
            return _theme != null ? _theme : ChartThemeRegistry.GlobalTheme;
        }

        // Animation
        private readonly ChartAnimationController _animation = new ChartAnimationController();

        private readonly CategoryScrollController _categoryScroll = new CategoryScrollController();
        private readonly ChartTooltipController _tooltipController = new ChartTooltipController();
        private readonly ChartLegendController _legend = new ChartLegendController();
        private readonly ChartAxisGridController _axisGrid = new ChartAxisGridController();
        private readonly ChartLabelController _labelController = new ChartLabelController();

        private ChartInteractionState _interactionState = new ChartInteractionState();

        private interface IEditorExecutionPolicy
        {
            bool IsCoordSwitchInProgress { get; }
            bool IsDeferredCoordSwitchApplying { get; }
            bool IsCoordSwitchBusy { get; }

            float GetCategoryScrollDeltaTime(float runtimeDeltaTime);

            void OnPlayAnimationStart();
            System.Action GetEditorAnimationUpdateCallback();

            void OnSetDataStart(ChartData prev, ChartData next, out bool coordChanged);
            bool TryDeferCoordSwitchApply(bool coordChanged);

            bool TryDeferVisualWork(string breadcrumbIfCoordChanged);
            bool ShouldDeferVisualWork();

            void RefreshLegendDeferred(System.Action refreshLegend);

            void ScheduleRuntimeAnimationLoop(System.Action onAnimationUpdate);

            void OnTextureFillAnimationFlagsUpdated(bool has);

            void OnProfileSameAssigned(ChartProfile profile);

            bool HandleGeometryChanged(GeometryChangedEvent evt);

            void GetLegendTrace(out bool trace, out System.Action<string> pushBreadcrumb);

            bool ShouldDeferInEditorRefreshPipeline();
            void SchedulePostRefresh();

            void OnAttachToPanel(AttachToPanelEvent evt);
            void OnDetachFromPanel(DetachFromPanelEvent evt);

            void OnCtorStart();

            void OnApplyProfile(ChartProfile profile);

            void OnSetDataApplyNow();

            void OnRebuildRenderersStart();

            void OnRebuildRenderersDone(int rendererCount, bool showCartesian);

            ChartRefreshPipeline CreateRefreshPipeline(
                ChartElement owner,
                System.Func<bool> hasData,
                System.Action rebuildRenderers,
                System.Action calculateRange,
                System.Action refreshAxisLayer,
                System.Action refreshGridLayer,
                System.Action refreshSeriesRenderers,
                System.Action refreshLayersNoLegend,
                System.Action refreshLegendDeferred,
                System.Action playAnimation);
        }

        private sealed class NullEditorExecutionPolicy : IEditorExecutionPolicy
        {
            private readonly ChartElement _owner;

            public bool IsCoordSwitchInProgress => false;
            public bool IsDeferredCoordSwitchApplying => false;
            public bool IsCoordSwitchBusy => false;

            public NullEditorExecutionPolicy(ChartElement owner)
            {
                _owner = owner;
            }

            public float GetCategoryScrollDeltaTime(float runtimeDeltaTime) => runtimeDeltaTime;

            public void OnPlayAnimationStart() { }

            public System.Action GetEditorAnimationUpdateCallback() => null;

            public void OnSetDataStart(ChartData prev, ChartData next, out bool coordChanged) => coordChanged = false;

            public bool TryDeferCoordSwitchApply(bool coordChanged) => false;

            public bool TryDeferVisualWork(string breadcrumbIfCoordChanged) => false;

            public bool ShouldDeferVisualWork() => false;

            public void RefreshLegendDeferred(System.Action refreshLegend) => refreshLegend?.Invoke();

            public void ScheduleRuntimeAnimationLoop(System.Action onAnimationUpdate)
            {
                if (_owner == null) return;
                _owner.schedule.Execute(onAnimationUpdate).Every(16);
            }

            public void OnTextureFillAnimationFlagsUpdated(bool has) { }

            public void OnProfileSameAssigned(ChartProfile profile) { }

            public bool HandleGeometryChanged(GeometryChangedEvent evt) => false;

            public void GetLegendTrace(out bool trace, out System.Action<string> pushBreadcrumb)
            {
                trace = false;
                pushBreadcrumb = null;
            }

            public bool ShouldDeferInEditorRefreshPipeline() => false;

            public void SchedulePostRefresh() { }

            public void OnAttachToPanel(AttachToPanelEvent evt) { }

            public void OnDetachFromPanel(DetachFromPanelEvent evt) { }

            public void OnCtorStart() { }

            public void OnApplyProfile(ChartProfile profile) { }

            public void OnSetDataApplyNow() { }

            public void OnRebuildRenderersStart() { }

            public void OnRebuildRenderersDone(int rendererCount, bool showCartesian) { }

            public ChartRefreshPipeline CreateRefreshPipeline(
                ChartElement owner,
                System.Func<bool> hasData,
                System.Action rebuildRenderers,
                System.Action calculateRange,
                System.Action refreshAxisLayer,
                System.Action refreshGridLayer,
                System.Action refreshSeriesRenderers,
                System.Action refreshLayersNoLegend,
                System.Action refreshLegendDeferred,
                System.Action playAnimation)
            {
                return new ChartRefreshPipeline(
                    owner,
                    hasData,
                    rebuildRenderers,
                    calculateRange,
                    refreshAxisLayer,
                    refreshGridLayer,
                    refreshSeriesRenderers,
                    refreshLayersNoLegend,
                    refreshLegendDeferred,
                    playAnimation
#if UNITY_EDITOR
                    ,
                    null,
                    null
#endif
                );
            }
        }

        private readonly IEditorExecutionPolicy _editor;

        private const string EditorDiagnosticsPrefsKey = "EasyChart.ChartElement.EditorDiagnosticsEnabled";

        internal static bool EditorDiagnosticsEnabled
        {
            get
            {
#if UNITY_EDITOR
                return UnityEditor.EditorPrefs.GetBool(EditorDiagnosticsPrefsKey, false);
#else
                return false;
#endif
            }
            set
            {
#if UNITY_EDITOR
                UnityEditor.EditorPrefs.SetBool(EditorDiagnosticsPrefsKey, value);
#endif
            }
        }

#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
        private static void InitEditorDiagnosticsDefault()
        {
            // Default to quiet logs. Developers can turn this on temporarily when debugging.
            UnityEditor.EditorPrefs.SetBool(EditorDiagnosticsPrefsKey, false);
        }

        private static bool s_assertHooked;
        private static bool s_dumpingAssert;
        private static double s_lastAssertDumpTime;
        private static readonly Queue<string> s_breadcrumbs = new Queue<string>(32);

        private static void PushBreadcrumb(string msg)
        {
            if (!EditorDiagnosticsEnabled) return;
            try
            {
                string line = $"{EditorApplication.timeSinceStartup:F3} {msg}";
                s_breadcrumbs.Enqueue(line);
                while (s_breadcrumbs.Count > 32) s_breadcrumbs.Dequeue();
            }
            catch
            {
            }
        }

        private static void EnsureAssertHooked()
        {
            if (!EditorDiagnosticsEnabled) return;
            if (s_assertHooked) return;
            s_assertHooked = true;
            Application.logMessageReceived += OnLogMessageReceived;
        }

        private static void OnLogMessageReceived(string condition, string stackTrace, LogType type)
        {
            if (!EditorDiagnosticsEnabled) return;
            if (s_dumpingAssert) return;
            if (string.IsNullOrEmpty(condition)) return;
            if (!condition.StartsWith("Assertion failed")) return;

            double now = EditorApplication.timeSinceStartup;
            if (now - s_lastAssertDumpTime < 0.2) return;
            s_lastAssertDumpTime = now;

            s_dumpingAssert = true;
            try
            {
                var dump = string.Join("\n", s_breadcrumbs.ToArray());
                Debug.Log("[EasyChart][AssertionBreadcrumbs]\n" + dump);
            }
            finally
            {
                s_dumpingAssert = false;
            }
        }

        private sealed class EditorExecutionPolicy
            : IEditorExecutionPolicy
        {
            private readonly ChartElement _owner;

            private double _categoryScrollLastTime;

            private bool _debugCoordChangedInLastSetData;
            private CoordinateSystemType _debugPrevCoord;
            private CoordinateSystemType _debugNewCoord;
            private bool _debugLogThisAnimation;
            private bool _debugAnimEndLogged;

            private bool _playAnimationAfterDeferredApply;

            private bool _postRefreshScheduled;
            private bool _postRefreshAwaitingAttach;

            private bool _deferredVisualRefreshScheduled;
            private bool _coordSwitchInProgress;
            private int _coordSwitchGeometryChangedCount;
            private bool _geometryCallbackSuppressed;

            private bool _deferredCoordSwitchApplyPending;
            private bool _deferredCoordSwitchApplying;

            public bool IsCoordSwitchInProgress => _coordSwitchInProgress;
            public bool IsDeferredCoordSwitchApplying => _deferredCoordSwitchApplying;
            public bool IsCoordSwitchBusy => _coordSwitchInProgress || _deferredCoordSwitchApplying;

            public EditorExecutionPolicy(ChartElement owner)
            {
                _owner = owner;
            }

            public float GetCategoryScrollDeltaTime(float runtimeDeltaTime)
            {
                if (Application.isPlaying) return runtimeDeltaTime;

                double now = EditorApplication.timeSinceStartup;
                if (_categoryScrollLastTime <= 0) _categoryScrollLastTime = now;
                float dt = (float)(now - _categoryScrollLastTime);
                _categoryScrollLastTime = now;
                return dt;
            }

            public void OnPlayAnimationStart()
            {
                _debugLogThisAnimation = !Application.isPlaying && _debugCoordChangedInLastSetData;
                _debugAnimEndLogged = false;
                if (_debugLogThisAnimation && EditorDiagnosticsEnabled)
                {
                    Debug.Log($"[ChartElement] Preview animation start (coord change: {_debugPrevCoord} -> {_debugNewCoord}, duration={_owner.Data?.animationDuration})");
                }
                _debugCoordChangedInLastSetData = false;
            }

            public void OnEditorAnimationUpdate()
            {
                bool ended = _owner._animation.TickEditorAnimation(_owner, _owner.Data, _owner._renderers);
                if (ended && _debugLogThisAnimation && !Application.isPlaying && !_debugAnimEndLogged && EditorDiagnosticsEnabled)
                {
                    _debugAnimEndLogged = true;
                    Debug.Log("[ChartElement] Preview animation end");
                }
            }

            private void OnEditorTextureFillAnimationUpdate()
            {
                _owner._animation.TickEditorTextureFillAnimation(_owner, _owner._renderers, _owner._backgroundLayer);
            }

            public System.Action GetEditorAnimationUpdateCallback()
            {
                return OnEditorAnimationUpdate;
            }

            public void OnSetDataStart(ChartData prev, ChartData next, out bool coordChanged)
            {
                bool hasPrev = prev != null;
                var prevCoord = hasPrev ? prev.CoordinateSystem : default;
                coordChanged = hasPrev && next != null && prevCoord != next.CoordinateSystem;
                _debugCoordChangedInLastSetData = !Application.isPlaying && coordChanged;

                if (!Application.isPlaying)
                {
                    if (EditorDiagnosticsEnabled)
                    {
                        PushBreadcrumb($"SetData start prevCoord={prevCoord} newCoord={(next != null ? next.CoordinateSystem : default)} coordChanged={coordChanged}");
                    }
                }

                if (_debugCoordChangedInLastSetData)
                {
                    _debugPrevCoord = prevCoord;
                    _debugNewCoord = next.CoordinateSystem;
                    _coordSwitchInProgress = true;
                    _coordSwitchGeometryChangedCount = 0;

                    if (!_geometryCallbackSuppressed)
                    {
                        _geometryCallbackSuppressed = true;
                        _owner.UnregisterCallback<GeometryChangedEvent>(_owner.OnGeometryChanged);
                        if (EditorDiagnosticsEnabled) Debug.Log("[ChartElement] Coord switch: unregister GeometryChanged");
                    }
                }
            }

            public bool TryDeferCoordSwitchApply(bool coordChanged)
            {
                if (Application.isPlaying) return false;
                if (!coordChanged) return false;

                // Defer expensive UI tree rebuild to next editor update to avoid ProcessEvent-time assertions.
                _owner._animation.StopEditorAnimation();
                _owner._animation.ResetRuntimeState();
                _playAnimationAfterDeferredApply = false;
                _deferredCoordSwitchApplyPending = true;
                if (EditorDiagnosticsEnabled) Debug.Log("[ChartElement] Coord switch: defer renderer rebuild to post refresh");
                PushBreadcrumb("SetData deferred apply pending");
                SchedulePostRefresh();
                _debugCoordChangedInLastSetData = false;
                return true;
            }

            public bool TryDeferVisualWork(string breadcrumbIfCoordChanged)
            {
                if (Application.isPlaying) return false;

                if (_debugCoordChangedInLastSetData)
                {
                    if (!string.IsNullOrEmpty(breadcrumbIfCoordChanged)) PushBreadcrumb(breadcrumbIfCoordChanged);
                    SchedulePostRefresh();
                    return true;
                }

                return _coordSwitchInProgress || _deferredCoordSwitchApplying;
            }

            public bool ShouldDeferVisualWork()
            {
                return TryDeferVisualWork(null);
            }

            public void RefreshLegendDeferred(System.Action refreshLegend)
            {
                if (Application.isPlaying)
                {
                    refreshLegend?.Invoke();
                    return;
                }

                if (_owner.panel == null)
                {
                    refreshLegend?.Invoke();
                    return;
                }

                ScheduleDeferredVisualRefresh(refreshLegend);
            }

            private void ScheduleDeferredVisualRefresh(System.Action action)
            {
                if (Application.isPlaying)
                {
                    action?.Invoke();
                    return;
                }

                if (_owner.panel == null)
                {
                    action?.Invoke();
                    return;
                }

                if (_deferredVisualRefreshScheduled) return;
                _deferredVisualRefreshScheduled = true;

                _owner.schedule.Execute(() =>
                {
                    _deferredVisualRefreshScheduled = false;
                    if (_owner.panel == null) return;
                    action?.Invoke();
                }).ExecuteLater(0);
            }

            public void ScheduleRuntimeAnimationLoop(System.Action onAnimationUpdate)
            {
                if (Application.isPlaying)
                {
                    _owner.schedule.Execute(onAnimationUpdate).Every(16);
                }
            }

            public void OnTextureFillAnimationFlagsUpdated(bool has)
            {
                if (Application.isPlaying) return;
                _owner._animation.SetHasTextureFillAnimations(has, _owner, OnEditorTextureFillAnimationUpdate);
            }

            public void OnProfileSameAssigned(ChartProfile profile)
            {
                if (Application.isPlaying) return;
                if (profile == null) return;
                _owner.ApplyProfileToElement();
            }

            public bool HandleGeometryChanged(GeometryChangedEvent evt)
            {
                return HandleGeometryChangedInEditor(evt);
            }

            public void GetLegendTrace(out bool trace, out System.Action<string> pushBreadcrumb)
            {
                trace = !Application.isPlaying && IsCoordSwitchBusy;
                pushBreadcrumb = trace ? PushBreadcrumb : null;
            }

            public bool ShouldDeferInEditorRefreshPipeline()
            {
                return _coordSwitchInProgress || _deferredCoordSwitchApplying;
            }

            public void SchedulePostRefresh()
            {
                if (Application.isPlaying) return;
                if (_postRefreshScheduled) return;
                _postRefreshScheduled = true;

                if (_owner.panel != null)
                {
                    PushBreadcrumb("SchedulePostRefresh via schedule");
                    _owner.schedule.Execute(OnPostRefreshScheduled).ExecuteLater(0);
                }
                else
                {
                    _postRefreshAwaitingAttach = true;
                    PushBreadcrumb("SchedulePostRefresh awaiting attach");
                }
            }

            public void OnAttachToPanel(AttachToPanelEvent evt)
            {
                _owner._animation.OnAttachToPanel(_owner, OnEditorTextureFillAnimationUpdate);
                if (!_postRefreshAwaitingAttach) return;
                if (!_postRefreshScheduled) return;
                _postRefreshAwaitingAttach = false;
                PushBreadcrumb("AttachToPanel -> run scheduled post refresh");
                _owner.schedule.Execute(OnPostRefreshScheduled).ExecuteLater(0);
            }

            public void OnDetachFromPanel(DetachFromPanelEvent evt)
            {
                _postRefreshAwaitingAttach = false;
                _owner._animation.OnDetachFromPanel();
            }

            public void OnCtorStart()
            {
                EnsureAssertHooked();
                PushBreadcrumb("ChartElement.ctor");
            }

            public void OnApplyProfile(ChartProfile profile)
            {
                if (Application.isPlaying) return;
                if (profile == null) return;
                PushBreadcrumb($"Profile set name={profile.name} coord={profile.coordinateSystem}");
            }

            public void OnSetDataApplyNow()
            {
                if (Application.isPlaying) return;
                if (!EditorDiagnosticsEnabled) return;
                PushBreadcrumb("SetData apply now: RebuildRenderers");
            }

            public void OnRebuildRenderersStart()
            {
                if (Application.isPlaying) return;
                if (!EditorDiagnosticsEnabled) return;
                PushBreadcrumb("RebuildRenderers start");
            }

            public void OnRebuildRenderersDone(int rendererCount, bool showCartesian)
            {
                if (Application.isPlaying) return;
                if (!EditorDiagnosticsEnabled) return;
                PushBreadcrumb($"RebuildRenderers done renderers={rendererCount} showCartesian={showCartesian}");
            }

            public ChartRefreshPipeline CreateRefreshPipeline(
                ChartElement owner,
                System.Func<bool> hasData,
                System.Action rebuildRenderers,
                System.Action calculateRange,
                System.Action refreshAxisLayer,
                System.Action refreshGridLayer,
                System.Action refreshSeriesRenderers,
                System.Action refreshLayersNoLegend,
                System.Action refreshLegendDeferred,
                System.Action playAnimation)
            {
                return new ChartRefreshPipeline(
                    owner,
                    hasData,
                    rebuildRenderers,
                    calculateRange,
                    refreshAxisLayer,
                    refreshGridLayer,
                    refreshSeriesRenderers,
                    refreshLayersNoLegend,
                    refreshLegendDeferred,
                    playAnimation,
                    ShouldDeferInEditorRefreshPipeline,
                    SchedulePostRefresh);
            }

            private void OnPostRefreshScheduled()
            {
                PushBreadcrumb("OnEditorPostRefresh(scheduled) start");
                _postRefreshScheduled = false;
                if (_owner.panel == null) return;
                if (_owner.Data == null) return;
                OnPostRefreshBody();
            }

            private void OnPostRefreshBody()
            {
                if (_geometryCallbackSuppressed)
                {
                    _geometryCallbackSuppressed = false;
                    _owner.RegisterCallback<GeometryChangedEvent>(_owner.OnGeometryChanged);
                    if (EditorDiagnosticsEnabled) Debug.Log("[ChartElement] Coord switch: re-register GeometryChanged");
                }

                bool showCartesian = _owner.Data.CoordinateSystem == CoordinateSystemType.Cartesian2D;
                if ((_owner._refresh == null || !_owner._refresh.HasPending) && !_deferredCoordSwitchApplyPending)
                {
                    if (showCartesian)
                    {
                        if (_owner._axisLayer != null) _owner._axisLayer.RefreshLabels();
                    }
                }

                if (_deferredCoordSwitchApplyPending)
                {
                    _deferredCoordSwitchApplyPending = false;
                    _deferredCoordSwitchApplying = true;

                    PushBreadcrumb("OnEditorPostRefresh applying deferred rebuild");

                    _owner._legend.ClearHiddenPieSliceIds();
                    PushBreadcrumb("Deferred: RebuildRenderers");
                    if (_owner._kernel != null) _owner._kernel.ExecuteRebuildRenderers();

                    _deferredCoordSwitchApplying = false;

                    PushBreadcrumb("Deferred: QueuePendingRefresh");
                    if (_owner._kernel != null)
                    {
                        _owner._kernel.QueueStages(ChartRefreshStage.CalculateRange | ChartRefreshStage.LayersNoLegend | ChartRefreshStage.Legend);
                    }

                    if (_playAnimationAfterDeferredApply)
                    {
                        _playAnimationAfterDeferredApply = false;
                        _owner.schedule.Execute(() =>
                        {
                            if (_owner.panel == null || _owner.Data == null) return;
                            if (_owner.Data.animationDuration <= 0f) return;
                            _owner.PlayAnimation();
                        }).ExecuteLater(32);
                    }
                }

                if (_owner._refresh == null || !_owner._refresh.HasPending)
                {
                    foreach (var r in _owner._renderers)
                    {
                        if (r == null) continue;
                        if (r.panel == null) continue;
                        if (r.parent == null) continue;
                        r.SetRange(_owner._xMin, _owner._xMax, _owner._yMin, _owner._yMax);
                        if (!r.visible) continue;
                        r.UpdateLabels();
                    }

                    // Always do legend/repaint via UIElements scheduler in editor to avoid ProcessEvent-time assertions.
                    if (!Application.isPlaying)
                    {
                        ScheduleDeferredVisualRefresh(() =>
                        {
                            bool sc = _owner.Data != null && _owner.Data.CoordinateSystem == CoordinateSystemType.Cartesian2D;
                            PushBreadcrumb("DeferredVisual RefreshLegend");
                            RefreshLegendNow();
                            if (sc && _owner._gridLayer != null) _owner._gridLayer.Redraw();
                        });
                    }
                    else
                    {
                        PushBreadcrumb("OnEditorPostRefresh RefreshLegend");
                        RefreshLegendNow();
                        if (showCartesian && _owner._gridLayer != null) _owner._gridLayer.Redraw();
                        PushBreadcrumb("OnEditorPostRefresh MarkDirtyRepaint");
                        _owner.MarkDirtyRepaint();
                    }
                }

                if (_coordSwitchInProgress)
                {
                    if (EditorDiagnosticsEnabled) Debug.Log($"[ChartElement] Coord switch geometryChanged count: {_coordSwitchGeometryChangedCount}");
                    _coordSwitchInProgress = false;
                    _coordSwitchGeometryChangedCount = 0;
                    if (EditorDiagnosticsEnabled) Debug.Log("[ChartElement] Coord switch post refresh applied");
                    PushBreadcrumb("OnEditorPostRefresh coord switch done");
                }

                if (_owner._refresh != null && _owner._refresh.HasPending)
                {
                    PushBreadcrumb("OnEditorPostRefresh FlushPendingRefresh");
                    _owner._refresh.Flush();
                }
            }

            private void RefreshLegendNow()
            {
                GetLegendTrace(out bool trace, out System.Action<string> pushBreadcrumb);

                _owner._legend.RefreshLegend(
                    _owner.Data,
                    trace,
                    pushBreadcrumb,
                    (reason) =>
                    {
                        if (_owner._kernel != null) _owner._kernel.Invalidate(reason, immediate: true);
                    });
            }

            public bool HandleGeometryChangedInEditor(GeometryChangedEvent evt)
            {
                if (Application.isPlaying) return false;

                if (_coordSwitchInProgress)
                {
                    _coordSwitchGeometryChangedCount++;
                    if (EditorDiagnosticsEnabled && _coordSwitchGeometryChangedCount <= 3)
                    {
                        Debug.Log($"[ChartElement] OnGeometryChanged (coord switch) #{_coordSwitchGeometryChangedCount}: {evt.oldRect} -> {evt.newRect}");
                    }
                    return true;
                }

                // In editor, avoid rebuilding legend / forcing grid redraw from geometry events.
                // These can fire during GUIUtility:ProcessEvent and trigger assertion spam.
                _owner.ScheduleLayoutRefresh();
                return true;
            }
        }
#endif

        public ChartProfile Profile 
        {
            get => _chartProfile;
            set 
            {
                if (_chartProfile == value)
                {
                    _editor.OnProfileSameAssigned(_chartProfile);
                    return;
                }

                _chartProfile = value;
                ApplyProfileToElement();
            }
        }

        private void ApplyProfileToElement()
        {
            if (_chartProfile == null) return;

            _editor.OnApplyProfile(_chartProfile);
            _chartProfile.EnsureAxes();

            var theme = GetEffectiveThemeInternal();

            if (!IgnoreProfileSize)
            {
                if (_chartProfile.chartWidth > 0) style.width = _chartProfile.chartWidth;
                if (_chartProfile.chartHeight > 0) style.height = _chartProfile.chartHeight;
            }

            if (_chartProfile.background == null) _chartProfile.background = new BackgroundSettings { show = true };
            style.backgroundColor = new StyleColor(theme != null ? theme.backgroundColor : Color.clear);
            style.backgroundImage = StyleKeyword.Null;

            if (_plotViewport != null)
            {
                // Plot settings: profile override takes precedence over theme
                var plotSettings = _chartProfile.plotSettings;
                bool useOverride = plotSettings != null && plotSettings.overrideTheme;

                var plotBg = useOverride ? plotSettings.backgroundColor : (theme != null ? theme.plotBackgroundColor : Color.clear);
                _plotViewport.style.backgroundColor = new StyleColor(plotBg);

                float bw = useOverride ? Mathf.Max(0f, plotSettings.borderWidth) : (theme != null ? Mathf.Max(0f, theme.plotBorderWidth) : 0f);
                var bc = useOverride ? plotSettings.borderColor : (theme != null ? theme.plotBorderColor : Color.clear);

                _plotViewport.style.borderLeftWidth = bw;
                _plotViewport.style.borderRightWidth = bw;
                _plotViewport.style.borderTopWidth = bw;
                _plotViewport.style.borderBottomWidth = bw;

                _plotViewport.style.borderLeftColor = new StyleColor(bc);
                _plotViewport.style.borderRightColor = new StyleColor(bc);
                _plotViewport.style.borderTopColor = new StyleColor(bc);
                _plotViewport.style.borderBottomColor = new StyleColor(bc);
            }

            if (_backgroundLayer != null) _backgroundLayer.MarkDirtyRepaint();

            UpdateTextureFillAnimationFlags();

            _padding = _chartProfile.padding;
            if (_chartArea != null)
            {
                _chartArea.style.left = _padding.x;
                _chartArea.style.right = _padding.y;
                _chartArea.style.top = _padding.z;
                _chartArea.style.bottom = _padding.w;

                ScheduleLayoutRefresh();
            }

            if (theme != null && theme.seriesColors != null && theme.seriesColors.Count > 0 && _chartProfile.series != null)
            {
                int paletteCount = theme.seriesColors.Count;
                int nextPaletteIndex = 0;

                for (int i = 0; i < _chartProfile.series.Count; i++)
                {
                    var serie = _chartProfile.series[i];
                    if (serie == null) continue;

                    Color paletteColor = theme.seriesColors[(nextPaletteIndex + theme.paletteSeed) % paletteCount];
                    bool used = false;

                    if (serie.type == SerieType.Line && serie.settings is LineSettings ls)
                    {
                        if (ls.stroke == null) ls.stroke = new LineStrokeSettings();
                        if (ls.stroke.color == Color.white)
                        {
                            ls.stroke.color = paletteColor;
                            used = true;
                        }
                    }
                    else if (serie.type == SerieType.Radar && serie.settings is RadarSettings rs)
                    {
                        if (rs.stroke == null) rs.stroke = new LineStrokeSettings();
                        if (rs.stroke.color == Color.white)
                        {
                            rs.stroke.color = paletteColor;
                            used = true;
                        }
                    }
                    else if ((serie.type == SerieType.Bar || serie.type == SerieType.HorizontalBar) && serie.settings is BarSettings bs)
                    {
                        if (bs.textureFill == null) bs.textureFill = new TextureFillSettings();
                        if (bs.textureFill.color == Color.white)
                        {
                            bs.textureFill.color = paletteColor;
                            used = true;
                        }
                    }
                    else if (serie.type == SerieType.Scatter && serie.settings is ScatterSettings ss)
                    {
                        if (ss.point == null) ss.point = new PointSettings();
                        if (ss.point.textureFill == null) ss.point.textureFill = new TextureFillSettings();
                        if (ss.point.textureFill.color == Color.white)
                        {
                            ss.point.textureFill.color = paletteColor;
                            used = true;
                        }
                    }

                    if (used) nextPaletteIndex++;
                }
            }

            var data = _chartProfile.ToChartData();

            bool showCartesian = data != null && data.CoordinateSystem == CoordinateSystemType.Cartesian2D;
            if (showCartesian)
            {
                if (_gridLayer != null)
                {
                    var grid = _chartProfile.cartesianGrid;
                    if (grid != null)
                    {
                        _gridLayer.XGridColor = grid.xGridColor;
                        _gridLayer.XGridLineWidth = grid.xGridLineWidth;
                        _gridLayer.XGridDashed = grid.xGridDashed;
                        _gridLayer.XGridDashLength = grid.xGridDashLength;
                        _gridLayer.XGridDashGap = grid.xGridDashGap;
                        _gridLayer.XGridDashOffset = grid.xGridDashOffset;
                        _gridLayer.YGridColor = grid.yGridColor;
                        _gridLayer.YGridLineWidth = grid.yGridLineWidth;
                        _gridLayer.YGridDashed = grid.yGridDashed;
                        _gridLayer.YGridDashLength = grid.yGridDashLength;
                        _gridLayer.YGridDashGap = grid.yGridDashGap;
                        _gridLayer.YGridDashOffset = grid.yGridDashOffset;
                    }
                }

                if (_cursorLine is DashedLineElement dashed && _chartProfile.hover != null)
                {
                    var hover = _chartProfile.hover;
                    dashed.Color = hover.cursorLineColor;
                    dashed.LineWidth = hover.cursorLineWidth;
                    dashed.Dashed = hover.cursorLineDashed;
                    dashed.DashLength = hover.cursorLineDashLength;
                    dashed.DashGap = hover.cursorLineDashGap;
                    dashed.DashOffset = hover.cursorLineDashOffset;
                }
            }

            SetData(data);
        }

        public ChartData Data { get; private set; }

        public ChartServices Services
        {
            get => _services;
            set
            {
                _services = value;
                ApplyServices(_services);
            }
        }

        public ChartServices GetEffectiveServicesSnapshot()
        {
            return new ChartServices
            {
                RendererSelectionPolicy = _rendererController.RendererSelectionPolicy,
                DynamicRendererFactory = _rendererController.DynamicRendererFactory,
                InteractionState = _interactionState,
                TooltipPolicy = _tooltipController.TooltipPolicy,
                HitTestPolicy = _tooltipController.HitTestPolicy,
                InteractionPolicy = _interactionPolicy,
                LegendTogglePolicy = _legend.LegendTogglePolicy,
                LayoutModel = _layoutModel,
            };
        }

        public IChartRendererSelectionPolicy RendererSelectionPolicy
        {
            get => _rendererController.RendererSelectionPolicy;
            set
            {
                _rendererController.RendererSelectionPolicy = value;
                if (_services != null) _services.RendererSelectionPolicy = value;
            }
        }

        public System.Func<SerieType, BaseSeriesRenderer> DynamicRendererFactory
        {
            get => _rendererController.DynamicRendererFactory;
            set
            {
                _rendererController.DynamicRendererFactory = value;
                if (_services != null) _services.DynamicRendererFactory = value;
            }
        }

        public ChartInteractionState InteractionState
        {
            get => _interactionState;
            set
            {
                _interactionState = value ?? new ChartInteractionState();
                _legend.SetInteractionState(_interactionState);
                _rendererController.SetInteractionState(_interactionState);
                if (_services != null) _services.InteractionState = _interactionState;
            }
        }

        public IChartTooltipPolicy TooltipPolicy
        {
            get => _tooltipController.TooltipPolicy;
            set
            {
                _tooltipController.TooltipPolicy = value;
                if (_services != null) _services.TooltipPolicy = value;
            }
        }

        public IChartHitTestPolicy HitTestPolicy
        {
            get => _tooltipController.HitTestPolicy;
            set
            {
                _tooltipController.HitTestPolicy = value;
                if (_services != null) _services.HitTestPolicy = value;
            }
        }

        public IChartInteractionPolicy InteractionPolicy
        {
            get => _interactionPolicy;
            set
            {
                _interactionPolicy = value ?? DefaultChartInteractionPolicy.Instance;
                if (_services != null) _services.InteractionPolicy = _interactionPolicy;
            }
        }

        public IChartLegendTogglePolicy LegendTogglePolicy
        {
            get => _legend.LegendTogglePolicy;
            set
            {
                _legend.LegendTogglePolicy = value;
                if (_services != null) _services.LegendTogglePolicy = value;
            }
        }

        public IChartLayoutModel LayoutModel
        {
            get => _layoutModel;
            set
            {
                _layoutModel = value ?? new DefaultChartLayoutModel();
                if (_services != null) _services.LayoutModel = _layoutModel;
            }
        }

        // Layout Settings
        private Vector4 _padding = new Vector4(40f, 30f, 50f, 50f);
        private readonly ChartLabelRefreshController _labelRefresh = new ChartLabelRefreshController();

        private IChartLayoutModel _layoutModel = new DefaultChartLayoutModel();

        private IChartInteractionPolicy _interactionPolicy = DefaultChartInteractionPolicy.Instance;

        private ChartServices _services;

        private void ApplyServices(ChartServices services)
        {
            if (services == null) return;

            if (services.RendererSelectionPolicy == null)
            {
                services.RendererSelectionPolicy = _rendererController.RendererSelectionPolicy;
            }
            if (services.DynamicRendererFactory == null)
            {
                services.DynamicRendererFactory = _rendererController.DynamicRendererFactory;
            }

            if (services.InteractionState == null)
            {
                services.InteractionState = _interactionState;
            }
            if (services.TooltipPolicy == null)
            {
                services.TooltipPolicy = _tooltipController.TooltipPolicy;
            }
            if (services.HitTestPolicy == null)
            {
                services.HitTestPolicy = _tooltipController.HitTestPolicy;
            }

            if (services.InteractionPolicy == null)
            {
                services.InteractionPolicy = _interactionPolicy;
            }
            if (services.LegendTogglePolicy == null)
            {
                services.LegendTogglePolicy = _legend.LegendTogglePolicy;
            }
            if (services.LayoutModel == null)
            {
                services.LayoutModel = _layoutModel;
            }

            if (services.RendererSelectionPolicy != null)
            {
                _rendererController.RendererSelectionPolicy = services.RendererSelectionPolicy;
            }

            if (services.DynamicRendererFactory != null)
            {
                _rendererController.DynamicRendererFactory = services.DynamicRendererFactory;
            }

            if (services.InteractionState != null)
            {
                _interactionState = services.InteractionState;
                _legend.SetInteractionState(_interactionState);
                _rendererController.SetInteractionState(_interactionState);
            }

            if (services.TooltipPolicy != null)
            {
                _tooltipController.TooltipPolicy = services.TooltipPolicy;
            }

            if (services.HitTestPolicy != null)
            {
                _tooltipController.HitTestPolicy = services.HitTestPolicy;
            }

            if (services.InteractionPolicy != null)
            {
                _interactionPolicy = services.InteractionPolicy;
            }

            if (services.LegendTogglePolicy != null)
            {
                _legend.LegendTogglePolicy = services.LegendTogglePolicy;
            }

            if (services.LayoutModel != null)
            {
                _layoutModel = services.LayoutModel;
            }
        }

        // Computed Range
        private float _xMin, _xMax;
        private float _yMin, _yMax;

        private AxisId GetMappedXAxisId()
        {
            return _layoutModel.GetMappedXAxisId(Data);
        }

        private AxisId GetMappedYAxisId()
        {
            return _layoutModel.GetMappedYAxisId(Data);
        }

        private bool IsCartesianTransposed()
        {
            return _layoutModel.IsCartesianTransposed(Data, GetAxis);
        }

        private static IEditorExecutionPolicy CreateEditorPolicy(ChartElement owner)
        {
#if UNITY_EDITOR
            return new EditorExecutionPolicy(owner);
#else
            return new NullEditorExecutionPolicy(owner);
#endif
        }

        public ChartElement()
        {
            _editor = CreateEditorPolicy(this);

            _editor.OnCtorStart();

            _rendererController.RendererSelectionPolicy = DefaultChartRendererSelectionPolicy.Instance;

            style.width = 300;
            style.height = 200;
            style.backgroundColor = new StyleColor(Color.clear);

            ChartKernel kernel = null;
            var callbacks = new ChartRefreshCallbacks(
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null);

            _refresh = _editor.CreateRefreshPipeline(
                this,
                () => Data != null,
                () => kernel?.ExecuteRebuildRenderers(),
                () => kernel?.ExecuteCalculateRange(),
                () => kernel?.ExecuteRefreshAxisLayer(),
                () => kernel?.ExecuteRefreshGridLayer(),
                () => kernel?.ExecuteRefreshSeriesRenderers(),
                () => kernel?.ExecuteRefreshLayersNoLegend(),
                () => kernel?.ExecuteRefreshLegendDeferred(),
                () => kernel?.ExecutePlayAnimation());

            _kernel = kernel = new ChartKernel(this, _refresh, callbacks);

            _backgroundLayer = new VisualElement();
            _backgroundLayer.pickingMode = PickingMode.Ignore;
            _backgroundLayer.StretchToParentSize();
            Add(_backgroundLayer);

            _gridLayer = new GridLayer();
            _axisLayer = new AxisLayer();
            _axisLayer.userData = this;

            _chartArea = new VisualElement();
            _chartArea.name = "chart-area";
            _chartArea.style.position = Position.Absolute;
            _chartArea.style.left = _padding.x;
            _chartArea.style.right = _padding.y;
            _chartArea.style.top = _padding.z;
            _chartArea.style.bottom = _padding.w;
            _chartArea.style.overflow = Overflow.Visible;
            _chartArea.pickingMode = PickingMode.Ignore;

            var plotViewport = new VisualElement();
            plotViewport.name = "plot-viewport";
            plotViewport.style.position = Position.Absolute;
            plotViewport.style.left = 0;
            plotViewport.style.right = 0;
            plotViewport.style.top = 0;
            plotViewport.style.bottom = 0;
            plotViewport.style.overflow = Overflow.Hidden;
            plotViewport.pickingMode = PickingMode.Ignore;

            var plotContentRoot = new VisualElement();
            plotContentRoot.name = "plot-content";
            plotContentRoot.style.position = Position.Absolute;
            plotContentRoot.style.left = 0;
            plotContentRoot.style.right = 0;
            plotContentRoot.style.top = 0;
            plotContentRoot.style.bottom = 0;
            plotContentRoot.style.overflow = Overflow.Visible;
            plotContentRoot.pickingMode = PickingMode.Ignore;

            _plotViewport = plotViewport;
            _plotContentRoot = plotContentRoot;
            _chartContent = plotViewport;

            _labelOverlay = new VisualElement();
            _labelOverlay.name = "label-overlay";
            _labelOverlay.style.position = Position.Absolute;
            _labelOverlay.style.left = 0;
            _labelOverlay.style.right = 0;
            _labelOverlay.style.top = 0;
            _labelOverlay.style.bottom = 0;
            _labelOverlay.style.overflow = Overflow.Visible;
            _labelOverlay.pickingMode = PickingMode.Ignore;
            _labelOverlay.userData = this;

            _labelController.Bind(_labelOverlay);

            plotContentRoot.Add(_gridLayer);

            _barRenderer = new BarSeriesRenderer();
            _lineRenderer = new LineSeriesRenderer();
            _scatterRenderer = new ScatterSeriesRenderer();
            _radarRenderer = new RadarSeriesRenderer();
            _pieRenderer = new PieSeriesRenderer();

            _barRenderer.visible = false;
            _lineRenderer.visible = false;
            _scatterRenderer.visible = false;
            _radarRenderer.visible = false;
            _pieRenderer.visible = false;

            plotContentRoot.Add(_barRenderer);
            plotContentRoot.Add(_lineRenderer);
            plotContentRoot.Add(_scatterRenderer);
            plotContentRoot.Add(_radarRenderer);
            plotContentRoot.Add(_pieRenderer);

            _barRenderer.SetLabelRoot(_labelOverlay);
            _lineRenderer.SetLabelRoot(_labelOverlay);
            _scatterRenderer.SetLabelRoot(_labelOverlay);
            _radarRenderer.SetLabelRoot(_labelOverlay);
            _pieRenderer.SetLabelRoot(_labelOverlay);

            _rendererController.Bind(
                _plotViewport,
                _plotContentRoot,
                _labelOverlay,
                _gridLayer,
                _axisLayer,
                _barRenderer,
                _lineRenderer,
                _scatterRenderer,
                _radarRenderer,
                _pieRenderer);

            plotViewport.Add(plotContentRoot);
            _chartArea.Add(plotViewport);
            _chartArea.Add(_axisLayer);
            
            Add(_chartArea); 

            _legend.EnsureContainer(_chartArea);
            _chartArea.Add(_labelOverlay);
            _legend.SetInteractionState(_interactionState);
            _rendererController.SetInteractionState(_interactionState);

            _kernel.InstallDefaultModules();
            _kernel.OnAttachToPanel();

            RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            RegisterCallback<PointerMoveEvent>(_kernel.OnPointerMove);
            RegisterCallback<PointerLeaveEvent>(_kernel.OnPointerLeave);
            RegisterCallback<AttachToPanelEvent>((evt) =>
            {
                _editor.OnAttachToPanel(evt);
                _kernel.OnAttachToPanel();
            });
            RegisterCallback<DetachFromPanelEvent>((evt) =>
            {
                _kernel.OnDetachFromPanel();
                _editor.OnDetachFromPanel(evt);
            });

            // Animation Loop
            _editor.ScheduleRuntimeAnimationLoop(OnAnimationUpdate);

            if (Data == null) SetData(new ChartData());
        }

        public void PlayAnimation()
        {
            _editor.OnPlayAnimationStart();

            _animation.Play(this, Application.isPlaying, _renderers, _editor.GetEditorAnimationUpdateCallback());
        }

        internal void PlayAnimationAutoInternal()
        {
            if (!Application.isPlaying && _animation.IsAnimating) return;
            PlayAnimation();
        }

        private void OnAnimationUpdate()
        {
            if (panel == null)
            {
                _animation.ResetRuntimeState();
            }
            if (Data == null) return;

            _animation.TickRuntime(Data, _renderers, _backgroundLayer);
        }

        public void LoadProfileByName(string name)
        {
#if UNITY_EDITOR
            string[] guids = AssetDatabase.FindAssets($"t:ChartProfile {name}");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                var profile = AssetDatabase.LoadAssetAtPath<ChartProfile>(path);
                if (profile != null)
                {
                    Profile = profile;
                }
            }
#else
            var profile = Resources.Load<ChartProfile>(name);
            if (profile != null) Profile = profile;
#endif
        }

#if UNITY_EDITOR
        public bool LoadProfileByGuid(string guid)
        {
            if (string.IsNullOrEmpty(guid)) return false;
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(path)) return false;
            var profile = AssetDatabase.LoadAssetAtPath<ChartProfile>(path);
            if (profile != null)
            {
                Profile = profile;
                return true;
            }
            return false;
        }
#endif

        public void SetData(ChartData data)
        {
            _editor.OnSetDataStart(Data, data, out bool coordChanged);
            Data = data;

            UpdateTextureFillAnimationFlags();

            if (_editor.TryDeferCoordSwitchApply(coordChanged)) return;

            _editor.OnSetDataApplyNow();

            if (_kernel != null) _kernel.InvalidateDataAssignedWithAnimation(data, immediate: true);
        }

        private static bool HasTextureFillAnimation(TextureFillSettings fill)
        {
            return fill != null && fill.animationType != TextureFillAnimationType.None;
        }

        private void UpdateTextureFillAnimationFlags()
        {
            if (!ProPackage.IsInstalled)
            {
                _animation.SetHasTextureFillAnimations(false);
                _editor.OnTextureFillAnimationFlagsUpdated(false);
                return;
            }

            bool has = false;

            if (_chartProfile != null)
            {
                var bg = _chartProfile.background;
                if (bg != null && bg.show && HasTextureFillAnimation(bg.textureFill)) has = true;
            }

            if (!has && Data != null && Data.Series != null)
            {
                for (int i = 0; i < Data.Series.Count; i++)
                {
                    var s = Data.Series[i];
                    if (s == null) continue;

                    if (s.settings is LineSettings ls)
                    {
                        if (HasTextureFillAnimation(ls.stroke != null ? ls.stroke.textureFill : null)) { has = true; break; }
                        if (HasTextureFillAnimation(ls.area != null ? ls.area.textureFill : null)) { has = true; break; }
                        if (ls.point != null && HasTextureFillAnimation(ls.point.textureFill)) { has = true; break; }
                    }
                    else if (s.settings is ScatterSettings ss)
                    {
                        if (ss.point != null && HasTextureFillAnimation(ss.point.textureFill)) { has = true; break; }
                        if (ss.hover != null && HasTextureFillAnimation(ss.hover.textureFill)) { has = true; break; }
                    }
                    else if (s.settings is BarSettings bs)
                    {
                        if (HasTextureFillAnimation(bs.textureFill)) { has = true; break; }
                        if (bs.background != null && HasTextureFillAnimation(bs.background.textureFill)) { has = true; break; }
                        if (bs.hover != null && HasTextureFillAnimation(bs.hover.textureFill)) { has = true; break; }
                    }
                    else if (s.settings is RadarSettings rs)
                    {
                        if (HasTextureFillAnimation(rs.stroke != null ? rs.stroke.textureFill : null)) { has = true; break; }
                        if (HasTextureFillAnimation(rs.area != null ? rs.area.textureFill : null)) { has = true; break; }
                        if (rs.point != null && HasTextureFillAnimation(rs.point.textureFill)) { has = true; break; }
                    }
                }
            }

            _animation.SetHasTextureFillAnimations(has);

            _editor.OnTextureFillAnimationFlagsUpdated(has);
        }

        public void RefreshData(bool rebuildRenderers = false, bool playAnimation = true)
        {
            if (Data == null) return;

            if (_kernel != null) _kernel.InvalidateDataMutated(rebuildRenderers, playAnimation, immediate: true);
        }

        /// <summary>
        /// Force re-apply the current profile to the element, refreshing all data and visuals.
        /// Use this when the profile's data has been modified in-place.
        /// </summary>
        public void ForceRefreshProfile()
        {
            ApplyProfileToElement();
        }

        private AxisConfig GetAxis(AxisId id)
        {
            if (Data == null || Data.Axes == null) return null;
            for (int i = 0; i < Data.Axes.Count; i++)
            {
                var a = Data.Axes[i];
                if (a != null && a.id == id) return a;
            }
            return null;
        }

        private List<string> GetCategoryLabels(AxisId id)
        {
            var axis = GetAxis(id);
            if (axis == null) return null;
            return axis.labels;
        }

        private List<string> GetCategoryLabelsWindowed(AxisId id)
        {
            var axis = GetAxis(id);
            if (axis == null) return null;

            int start = (id == AxisId.XBottom || id == AxisId.XTop) ? _categoryScroll.WindowStartX : _categoryScroll.WindowStartY;
            var buffer = (id == AxisId.XBottom || id == AxisId.XTop) ? _windowedCategoryLabelsX : _windowedCategoryLabelsY;
            return _layoutModel.GetCategoryLabelsWindowed(id, axis, start, buffer);
        }

        private void ApplyCategoryScrollTranslate(float xPx, float yPx)
        {
            if (_plotContentRoot != null)
            {
                _plotContentRoot.style.translate = new Translate(xPx, yPx, 0);
            }
            if (_labelOverlay != null)
            {
                _labelOverlay.style.translate = new Translate(xPx, yPx, 0);
            }
            if (_axisLayer != null)
            {
                _axisLayer.SetCategoryScrollOffset(xPx, yPx);
            }
        }

        private void UpdateCategoryAxisRangeOnly()
        {
            int wx = _categoryScroll.WindowStartX;
            int wy = _categoryScroll.WindowStartY;
            float xMin = _xMin;
            float xMax = _xMax;
            float yMin = _yMin;
            float yMax = _yMax;

            _layoutModel.UpdateCategoryAxisRangeOnly(Data, GetAxis, ref wx, ref wy, ref xMin, ref xMax, ref yMin, ref yMax);

            _categoryScroll.WindowStartX = wx;
            _categoryScroll.WindowStartY = wy;
            _xMin = xMin;
            _xMax = xMax;
            _yMin = yMin;
            _yMax = yMax;
        }

        internal void OnCategoryScrollUpdate()
        {
            if (panel == null) return;
            if (Data == null) return;

            if (Data.CoordinateSystem != CoordinateSystemType.Cartesian2D)
            {
                if (_categoryScroll.ScrollOffsetX != 0f || _categoryScroll.ScrollOffsetY != 0f)
                {
                    _categoryScroll.ResetOffsets();
                    ApplyCategoryScrollTranslate(0, 0);
                }
                return;
            }

            float dt = _editor.GetCategoryScrollDeltaTime(Time.unscaledDeltaTime);

            if (dt <= 0f || dt > 1f) dt = 0.016f;

            var xAxisId = GetMappedXAxisId();
            var yAxisId = GetMappedYAxisId();
            var xAxis = GetAxis(xAxisId);
            var yAxis = GetAxis(yAxisId);

            float prevOffsetX = _categoryScroll.ScrollOffsetX;
            float prevOffsetY = _categoryScroll.ScrollOffsetY;

            float plotWidth = _chartContent != null ? _chartContent.contentRect.width : 0f;
            float plotHeight = _chartContent != null ? _chartContent.contentRect.height : 0f;
            var update = _categoryScroll.Update(xAxisId, xAxis, yAxis, plotWidth, plotHeight, dt);

            if (update.NeedsRangeUpdate)
            {
                UpdateCategoryAxisRangeOnly();
            }

            if (update.NeedsWindowRefresh)
            {
                if (_kernel != null) _kernel.Invalidate(ChartDirtyReason.CategoryWindowChanged, immediate: true);
            }

            if (_categoryScroll.SmoothTranslating)
            {
                HideTooltip();
                ApplyCategoryScrollTranslate(_categoryScroll.ScrollOffsetX, _categoryScroll.ScrollOffsetY);
            }
            else
            {
                if (prevOffsetX != 0f || prevOffsetY != 0f)
                {
                    ApplyCategoryScrollTranslate(0, 0);
                }
            }
        }

        private void OnGeometryChanged(GeometryChangedEvent evt)
        {
            _kernel?.OnGeometryChanged(evt);
        }

        private void HideTooltip()
        {
            _tooltipController.Hide(_tooltip, _cursorLine);
        }
    }
}
