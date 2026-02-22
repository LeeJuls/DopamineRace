using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using EasyChart.Layers;

namespace EasyChart
{
    internal sealed class ChartRendererController
    {
        private readonly List<BaseSeriesRenderer> _renderers = new List<BaseSeriesRenderer>(8);
        private readonly Dictionary<SerieType, BaseSeriesRenderer> _dynamicRenderers = new Dictionary<SerieType, BaseSeriesRenderer>();

        private ChartInteractionState _interactionState;

        public IChartRendererSelectionPolicy RendererSelectionPolicy { get; set; }

        private VisualElement _plotViewport;
        private VisualElement _plotContentRoot;
        private VisualElement _labelOverlay;
        private GridLayer _gridLayer;
        private AxisLayer _axisLayer;

        private BarSeriesRenderer _barRenderer;
        private LineSeriesRenderer _lineRenderer;
        private ScatterSeriesRenderer _scatterRenderer;
        private RadarSeriesRenderer _radarRenderer;
        private PieSeriesRenderer _pieRenderer;

#if UNITY_EDITOR
        private bool _editorDeferredRendererVisualApplyScheduled;
        private bool _editorDesiredShowBar;
        private bool _editorDesiredNeedLine;
        private bool _editorDesiredNeedScatter;
        private bool _editorDesiredNeedRadar;
        private bool _editorDesiredNeedPie;
        private bool _editorDesiredShowCartesian;
        private bool _editorDesiredAllowOverflow;
#endif

        public List<BaseSeriesRenderer> Renderers => _renderers;

        public Func<SerieType, BaseSeriesRenderer> DynamicRendererFactory { get; set; }

        public void SetInteractionState(ChartInteractionState state)
        {
            _interactionState = state;

            for (int i = 0; i < _renderers.Count; i++)
            {
                var r = _renderers[i];
                if (r is IChartInteractionStateConsumer c)
                {
                    c.SetInteractionState(_interactionState);
                }
            }
        }

        public void Bind(
            VisualElement plotViewport,
            VisualElement plotContentRoot,
            VisualElement labelOverlay,
            GridLayer gridLayer,
            AxisLayer axisLayer,
            BarSeriesRenderer barRenderer,
            LineSeriesRenderer lineRenderer,
            ScatterSeriesRenderer scatterRenderer,
            RadarSeriesRenderer radarRenderer,
            PieSeriesRenderer pieRenderer)
        {
            _plotViewport = plotViewport;
            _plotContentRoot = plotContentRoot;
            _labelOverlay = labelOverlay;
            _gridLayer = gridLayer;
            _axisLayer = axisLayer;

            _barRenderer = barRenderer;
            _lineRenderer = lineRenderer;
            _scatterRenderer = scatterRenderer;
            _radarRenderer = radarRenderer;
            _pieRenderer = pieRenderer;
        }

        public void Rebuild(ChartData data, bool transposed, VisualElement scheduleOwner, bool deferVisualChanges)
        {
            _renderers.Clear();

            var selection = RendererSelectionPolicy != null
                ? RendererSelectionPolicy.Select(data)
                : ChartRendererManager.Select(data);

            bool needLine = selection.NeedLine;
            bool needScatter = selection.NeedScatter;
            bool needBar = selection.NeedBar;
            bool needRadar = selection.NeedRadar;
            bool needPie = selection.NeedPie;
            var needDynamic = selection.NeedDynamic;

            bool allowOverflow = selection.AllowOverflow;

            bool showBar = needBar;

#if UNITY_EDITOR
            if (!Application.isPlaying && deferVisualChanges)
            {
                // During coord-switch apply, defer all VisualElement visibility/style changes.
                _editorDesiredAllowOverflow = allowOverflow;
                _editorDesiredShowBar = showBar;
                _editorDesiredNeedLine = needLine;
                _editorDesiredNeedScatter = needScatter;
                _editorDesiredNeedRadar = needRadar;
                _editorDesiredNeedPie = needPie;
                _editorDesiredShowCartesian = selection.ShowCartesian;

                if (!_editorDeferredRendererVisualApplyScheduled)
                {
                    _editorDeferredRendererVisualApplyScheduled = true;
                    if (scheduleOwner != null)
                    {
                        scheduleOwner.schedule.Execute(() =>
                        {
                            _editorDeferredRendererVisualApplyScheduled = false;
                            if (scheduleOwner.panel == null) return;
                            ApplyRendererVisualsDeferred();
                        }).ExecuteLater(16);
                    }
                }
            }
            else
#endif
            {
                ApplyRendererVisualsImmediate(
                    allowOverflow,
                    showBar,
                    needLine,
                    needScatter,
                    needRadar,
                    needPie,
                    selection.ShowCartesian,
                    needDynamic);
            }

            if (showBar && _barRenderer != null) _renderers.Add(_barRenderer);
            if (needLine && _lineRenderer != null) _renderers.Add(_lineRenderer);
            if (needScatter && _scatterRenderer != null) _renderers.Add(_scatterRenderer);
            if (needRadar && _radarRenderer != null) _renderers.Add(_radarRenderer);
            if (needPie && _pieRenderer != null) _renderers.Add(_pieRenderer);

            if (needDynamic.Count > 0)
            {
                foreach (var t in needDynamic)
                {
                    var r = ChartRendererManager.EnsureDynamicRenderer(t, _dynamicRenderers, _plotContentRoot, DynamicRendererFactory);
                    if (r == null)
                    {
                        Debug.LogError($"[EasyChart] Missing renderer for serie type '{t}'. This chart type may require EasyChart Pro.");
                        continue;
                    }

                    if (_labelOverlay != null) r.SetLabelRoot(_labelOverlay);

                    r.visible = true;
                    _renderers.Add(r);
                }
            }

            if (_interactionState != null)
            {
                for (int i = 0; i < _renderers.Count; i++)
                {
                    var r = _renderers[i];
                    if (r is IChartInteractionStateConsumer c)
                    {
                        c.SetInteractionState(_interactionState);
                    }
                }
            }

            ApplySeriesOrderToRendererLayers(data, showBar, needLine, needScatter, needRadar, needPie, needDynamic);
        }

        private struct RendererOrderEntry
        {
            public SerieType Type;
            public VisualElement Element;
            public int LastSeriesIndex;

            public RendererOrderEntry(SerieType type, VisualElement element, int lastSeriesIndex)
            {
                Type = type;
                Element = element;
                LastSeriesIndex = lastSeriesIndex;
            }
        }

        private static int GetLastSeriesIndexOfType(ChartData data, SerieType type)
        {
            if (data == null || data.Series == null) return -1;
            for (int i = data.Series.Count - 1; i >= 0; i--)
            {
                var s = data.Series[i];
                if (s == null || !s.visible) continue;
                if (s.type == type) return i;
            }
            return -1;
        }

        private void ApplySeriesOrderToRendererLayers(
            ChartData data,
            bool showBar,
            bool needLine,
            bool needScatter,
            bool needRadar,
            bool needPie,
            HashSet<SerieType> needDynamic)
        {
            if (_plotContentRoot == null) return;

            // Keep grid at the bottom.
            if (_gridLayer != null && _gridLayer.parent == _plotContentRoot)
            {
                _plotContentRoot.Insert(0, _gridLayer);
            }

            var entries = new List<RendererOrderEntry>(8);

            if (showBar && _barRenderer != null && _barRenderer.parent == _plotContentRoot)
                entries.Add(new RendererOrderEntry(SerieType.Bar, _barRenderer, GetLastSeriesIndexOfType(data, SerieType.Bar)));

            if (needLine && _lineRenderer != null && _lineRenderer.parent == _plotContentRoot)
                entries.Add(new RendererOrderEntry(SerieType.Line, _lineRenderer, GetLastSeriesIndexOfType(data, SerieType.Line)));

            if (needScatter && _scatterRenderer != null && _scatterRenderer.parent == _plotContentRoot)
                entries.Add(new RendererOrderEntry(SerieType.Scatter, _scatterRenderer, GetLastSeriesIndexOfType(data, SerieType.Scatter)));

            if (needRadar && _radarRenderer != null && _radarRenderer.parent == _plotContentRoot)
                entries.Add(new RendererOrderEntry(SerieType.Radar, _radarRenderer, GetLastSeriesIndexOfType(data, SerieType.Radar)));

            if (needPie && _pieRenderer != null && _pieRenderer.parent == _plotContentRoot)
                entries.Add(new RendererOrderEntry(SerieType.Pie, _pieRenderer, GetLastSeriesIndexOfType(data, SerieType.Pie)));

            if (needDynamic != null && needDynamic.Count > 0)
            {
                foreach (var kv in _dynamicRenderers)
                {
                    if (kv.Value == null) continue;
                    if (!needDynamic.Contains(kv.Key)) continue;
                    if (kv.Value.parent != _plotContentRoot) continue;

                    entries.Add(new RendererOrderEntry(kv.Key, kv.Value, GetLastSeriesIndexOfType(data, kv.Key)));
                }
            }

            // Sort by the last occurrence in the series list; later series should be rendered on top.
            entries.Sort((a, b) =>
            {
                int cmp = a.LastSeriesIndex.CompareTo(b.LastSeriesIndex);
                if (cmp != 0) return cmp;
                return ((int)a.Type).CompareTo((int)b.Type);
            });

            int insertIndex = (_gridLayer != null && _gridLayer.parent == _plotContentRoot) ? 1 : 0;
            for (int i = 0; i < entries.Count; i++)
            {
                var ve = entries[i].Element;
                if (ve == null) continue;
                if (ve.parent != _plotContentRoot) continue;
                _plotContentRoot.Insert(insertIndex, ve);
                insertIndex++;
            }
        }

        private void ApplyRendererVisualsImmediate(
            bool allowOverflow,
            bool showBar,
            bool needLine,
            bool needScatter,
            bool needRadar,
            bool needPie,
            bool showCartesian,
            HashSet<SerieType> needDynamic)
        {
            if (_plotViewport != null)
            {
                _plotViewport.style.overflow = allowOverflow ? Overflow.Visible : Overflow.Hidden;
            }

            if (_barRenderer != null)
            {
                if (!showBar && _barRenderer.visible) _barRenderer.ClearLabels();
                _barRenderer.visible = showBar;
            }
            if (_lineRenderer != null)
            {
                if (!needLine && _lineRenderer.visible) _lineRenderer.ClearLabels();
                _lineRenderer.visible = needLine;
            }
            if (_scatterRenderer != null)
            {
                if (!needScatter && _scatterRenderer.visible) _scatterRenderer.ClearLabels();
                _scatterRenderer.visible = needScatter;
            }
            if (_radarRenderer != null)
            {
                if (!needRadar && _radarRenderer.visible) _radarRenderer.ClearLabels();
                _radarRenderer.visible = needRadar;
            }
            if (_pieRenderer != null)
            {
                if (!needPie && _pieRenderer.visible) _pieRenderer.ClearLabels();
                _pieRenderer.visible = needPie;
            }

            foreach (var kv in _dynamicRenderers)
            {
                if (kv.Value == null) continue;
                bool needed = needDynamic != null && needDynamic.Contains(kv.Key);
                if (!needed && kv.Value.visible) kv.Value.ClearLabels();
                kv.Value.visible = needed;
            }

            // Toggle Axis/Grid visibility based on coordinate system
            if (_gridLayer != null) _gridLayer.visible = showCartesian;
            if (_axisLayer != null) _axisLayer.visible = showCartesian;
        }

#if UNITY_EDITOR
        private void ApplyRendererVisualsDeferred()
        {
            if (Application.isPlaying) return;

            if (_plotViewport != null)
            {
                _plotViewport.style.overflow = _editorDesiredAllowOverflow ? Overflow.Visible : Overflow.Hidden;
            }

            if (_barRenderer != null)
            {
                if (!_editorDesiredShowBar && _barRenderer.visible) _barRenderer.ClearLabels();
                _barRenderer.visible = _editorDesiredShowBar;
            }
            if (_lineRenderer != null)
            {
                if (!_editorDesiredNeedLine && _lineRenderer.visible) _lineRenderer.ClearLabels();
                _lineRenderer.visible = _editorDesiredNeedLine;
            }
            if (_scatterRenderer != null)
            {
                if (!_editorDesiredNeedScatter && _scatterRenderer.visible) _scatterRenderer.ClearLabels();
                _scatterRenderer.visible = _editorDesiredNeedScatter;
            }
            if (_radarRenderer != null)
            {
                if (!_editorDesiredNeedRadar && _radarRenderer.visible) _radarRenderer.ClearLabels();
                _radarRenderer.visible = _editorDesiredNeedRadar;
            }
            if (_pieRenderer != null)
            {
                if (!_editorDesiredNeedPie && _pieRenderer.visible) _pieRenderer.ClearLabels();
                _pieRenderer.visible = _editorDesiredNeedPie;
            }

            if (_gridLayer != null) _gridLayer.visible = _editorDesiredShowCartesian;
            if (_axisLayer != null) _axisLayer.visible = _editorDesiredShowCartesian;
        }
#endif
    }
}
