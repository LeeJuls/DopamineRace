using System;
using UnityEngine;
#if UNITY_2019_3_OR_NEWER
using Unity.Profiling;
#endif
using UnityEngine.UIElements;

namespace EasyChart
{
    internal sealed class ChartRefreshPipeline
    {
#if UNITY_2019_3_OR_NEWER
        private static readonly ProfilerMarker s_flushMarker = new ProfilerMarker("EasyChart.Refresh.Flush");
        private static readonly ProfilerMarker s_rebuildRenderersMarker = new ProfilerMarker("EasyChart.Refresh.RebuildRenderers");
        private static readonly ProfilerMarker s_calculateRangeMarker = new ProfilerMarker("EasyChart.Refresh.CalculateRange");
        private static readonly ProfilerMarker s_layersNoLegendMarker = new ProfilerMarker("EasyChart.Refresh.LayersNoLegend");
        private static readonly ProfilerMarker s_axisLayerMarker = new ProfilerMarker("EasyChart.Refresh.AxisLayer");
        private static readonly ProfilerMarker s_gridMarker = new ProfilerMarker("EasyChart.Refresh.Grid");
        private static readonly ProfilerMarker s_seriesRenderersMarker = new ProfilerMarker("EasyChart.Refresh.SeriesRenderers");
        private static readonly ProfilerMarker s_legendMarker = new ProfilerMarker("EasyChart.Refresh.Legend");
        private static readonly ProfilerMarker s_playAnimationMarker = new ProfilerMarker("EasyChart.Refresh.PlayAnimation");
#endif

        private readonly VisualElement _owner;
        private readonly Func<bool> _hasData;

        private readonly Action _rebuildRenderers;
        private readonly Action _calculateRange;
        private readonly Action _refreshAxisLayer;
        private readonly Action _refreshGridLayer;
        private readonly Action _refreshSeriesRenderers;
        private readonly Action _refreshLayersNoLegend;
        private readonly Action _refreshLegendDeferred;
        private readonly Action _playAnimation;

#if UNITY_EDITOR
        private readonly Func<bool> _shouldDeferInEditor;
        private readonly Action _scheduleEditorPostRefresh;
#endif

        private ChartRefreshStage _pending;
        private bool _scheduled;

        public ChartRefreshStage Pending => _pending;
        public bool HasPending => _pending != ChartRefreshStage.None;

        public ChartRefreshPipeline(
            VisualElement owner,
            Func<bool> hasData,
            Action rebuildRenderers,
            Action calculateRange,
            Action refreshLayersNoLegend,
            Action refreshLegendDeferred,
            Action playAnimation
#if UNITY_EDITOR
            ,
            Func<bool> shouldDeferInEditor,
            Action scheduleEditorPostRefresh
#endif
        )
        {
            _owner = owner;
            _hasData = hasData;

            _rebuildRenderers = rebuildRenderers;
            _calculateRange = calculateRange;
            _refreshAxisLayer = null;
            _refreshGridLayer = null;
            _refreshSeriesRenderers = null;
            _refreshLayersNoLegend = refreshLayersNoLegend;
            _refreshLegendDeferred = refreshLegendDeferred;
            _playAnimation = playAnimation;

#if UNITY_EDITOR
            _shouldDeferInEditor = shouldDeferInEditor;
            _scheduleEditorPostRefresh = scheduleEditorPostRefresh;
#endif
        }

        public ChartRefreshPipeline(
            VisualElement owner,
            Func<bool> hasData,
            Action rebuildRenderers,
            Action calculateRange,
            Action refreshAxisLayer,
            Action refreshGridLayer,
            Action refreshSeriesRenderers,
            Action refreshLayersNoLegend,
            Action refreshLegendDeferred,
            Action playAnimation
#if UNITY_EDITOR
            ,
            Func<bool> shouldDeferInEditor,
            Action scheduleEditorPostRefresh
#endif
        )
        {
            _owner = owner;
            _hasData = hasData;

            _rebuildRenderers = rebuildRenderers;
            _calculateRange = calculateRange;
            _refreshAxisLayer = refreshAxisLayer;
            _refreshGridLayer = refreshGridLayer;
            _refreshSeriesRenderers = refreshSeriesRenderers;
            _refreshLayersNoLegend = refreshLayersNoLegend;
            _refreshLegendDeferred = refreshLegendDeferred;
            _playAnimation = playAnimation;

#if UNITY_EDITOR
            _shouldDeferInEditor = shouldDeferInEditor;
            _scheduleEditorPostRefresh = scheduleEditorPostRefresh;
#endif
        }

        public void Add(ChartRefreshStage request)
        {
            _pending |= request;
        }

        public void RequestImmediate(ChartRefreshStage request)
        {
            _pending |= request;

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                var dangerous = request & (ChartRefreshStage.Legend | ChartRefreshStage.RebuildRenderers | ChartRefreshStage.PlayAnimation);
                if (dangerous == ChartRefreshStage.None)
                {
                    Flush();
                    return;
                }

                Request(ChartRefreshStage.None);
                return;
            }
#endif

            Flush();
        }

        public void Request(ChartRefreshStage request)
        {
            _pending |= request;

            if (_owner.panel == null)
            {
                Flush();
                return;
            }

#if UNITY_EDITOR
            if (!Application.isPlaying && _shouldDeferInEditor != null && _shouldDeferInEditor())
            {
                _scheduleEditorPostRefresh?.Invoke();
                return;
            }
#endif

            if (_scheduled) return;
            _scheduled = true;
            _owner.schedule.Execute(Flush).ExecuteLater(0);
        }

        public void Flush()
        {
#if UNITY_2019_3_OR_NEWER
            using (s_flushMarker.Auto())
#endif
            {
                _scheduled = false;

                if (_hasData != null && !_hasData())
                {
                    _pending = ChartRefreshStage.None;
                    return;
                }

                var req = _pending;
                _pending = ChartRefreshStage.None;

                if ((req & ChartRefreshStage.RebuildRenderers) != 0)
                {
#if UNITY_2019_3_OR_NEWER
                    using (s_rebuildRenderersMarker.Auto())
#endif
                    {
                        _rebuildRenderers?.Invoke();
                    }
                }

                if ((req & ChartRefreshStage.CalculateRange) != 0)
                {
#if UNITY_2019_3_OR_NEWER
                    using (s_calculateRangeMarker.Auto())
#endif
                    {
                        _calculateRange?.Invoke();
                    }
                }

                bool compositeLayers = (req & ChartRefreshStage.LayersNoLegend) != 0;
                if (!compositeLayers)
                {
                    if ((req & ChartRefreshStage.AxisLayer) != 0)
                    {
#if UNITY_2019_3_OR_NEWER
                        using (s_axisLayerMarker.Auto())
#endif
                        {
                            _refreshAxisLayer?.Invoke();
                        }
                    }

                    if ((req & ChartRefreshStage.SeriesRenderers) != 0)
                    {
#if UNITY_2019_3_OR_NEWER
                        using (s_seriesRenderersMarker.Auto())
#endif
                        {
                            _refreshSeriesRenderers?.Invoke();
                        }
                    }

                    if ((req & ChartRefreshStage.Grid) != 0)
                    {
#if UNITY_2019_3_OR_NEWER
                        using (s_gridMarker.Auto())
#endif
                        {
                            _refreshGridLayer?.Invoke();
                        }
                    }
                }

                if ((req & ChartRefreshStage.LayersNoLegend) != 0)
                {
#if UNITY_2019_3_OR_NEWER
                    using (s_layersNoLegendMarker.Auto())
#endif
                    {
                        _refreshLayersNoLegend?.Invoke();
                    }
                }

                if ((req & ChartRefreshStage.Legend) != 0)
                {
#if UNITY_2019_3_OR_NEWER
                    using (s_legendMarker.Auto())
#endif
                    {
                        _refreshLegendDeferred?.Invoke();
                    }
                }

                if ((req & ChartRefreshStage.PlayAnimation) != 0)
                {
#if UNITY_2019_3_OR_NEWER
                    using (s_playAnimationMarker.Auto())
#endif
                    {
                        _playAnimation?.Invoke();
                    }
                }
            }
        }
    }
}
