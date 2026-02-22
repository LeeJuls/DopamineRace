using System;
using UnityEngine;
using UnityEngine.UIElements;
using EasyChart.Layers;

namespace EasyChart
{
    internal sealed class ChartRefreshStagesModule : IChartModule, IChartRefreshStageModule
    {
        private ChartElement _owner;
        private ChartKernel _kernel;

        public void Bind(ChartElement owner, ChartKernel kernel)
        {
            _owner = owner;
            _kernel = kernel;
        }

        public void Unbind()
        {
            _owner = null;
            _kernel = null;
        }

        public void OnRebuildRenderers()
        {
            if (_owner == null) return;

            _owner.EditorOnRebuildRenderersStartInternal();
            bool transposed = _owner.IsCartesianTransposedInternal;
            bool deferVisualChanges = _owner.EditorIsDeferredCoordSwitchApplyingInternal;

            _owner.RendererControllerInternal.Rebuild(_owner.Data, transposed, _owner, deferVisualChanges);

            int count = _owner.RenderersInternal != null ? _owner.RenderersInternal.Count : 0;
            bool showCartesian = _owner.Data != null && _owner.Data.CoordinateSystem == CoordinateSystemType.Cartesian2D;
            _owner.EditorOnRebuildRenderersDoneInternal(count, showCartesian);
        }

        public void OnCalculateRange()
        {
            if (_owner == null) return;

            var r = _owner.LayoutModelInternal.CalculateRange(_owner.Data, _owner.GetAxisInternal, _owner.CategoryScrollControllerInternal.WindowStartX, _owner.CategoryScrollControllerInternal.WindowStartY);

            _owner.XMinInternal = r.XMin;
            _owner.XMaxInternal = r.XMax;
            _owner.YMinInternal = r.YMin;
            _owner.YMaxInternal = r.YMax;

            _owner.CategoryScrollControllerInternal.WindowStartX = r.CategoryWindowStartX;
            _owner.CategoryScrollControllerInternal.WindowStartY = r.CategoryWindowStartY;
        }

        public void OnRefreshAxisLayer()
        {
            if (_owner == null) return;
            if (_owner.Data == null) return;
            if (_owner.Data.CoordinateSystem != CoordinateSystemType.Cartesian2D) return;
            if (_owner.EditorShouldDeferVisualWorkInternal()) return;

            var xAxisId = _owner.GetMappedXAxisIdInternal();
            var yAxisId = _owner.GetMappedYAxisIdInternal();
            var xCategoryLabels = _owner.GetCategoryLabelsWindowedInternal(xAxisId);
            var yCategoryLabels = _owner.GetCategoryLabelsWindowedInternal(yAxisId);

            var xAxis = _owner.GetAxisInternal(xAxisId);
            var yAxis = _owner.GetAxisInternal(yAxisId);

            _owner.AxisGridControllerInternal.RefreshAxisLayer(
                _owner.AxisLayerInternal,
                xAxisId,
                yAxisId,
                xCategoryLabels,
                yCategoryLabels,
                xAxis,
                yAxis,
                _owner.XMinInternal,
                _owner.XMaxInternal,
                _owner.YMinInternal,
                _owner.YMaxInternal);
        }

        public void OnRefreshGridLayer()
        {
            if (_owner == null) return;
            if (_owner.Data == null) return;
            if (_owner.Data.CoordinateSystem != CoordinateSystemType.Cartesian2D) return;
            if (_owner.EditorShouldDeferVisualWorkInternal()) return;

            _owner.AxisGridControllerInternal.RefreshGridLayer(_owner.GridLayerInternal, _owner.AxisLayerInternal);
        }

        public void OnRefreshSeriesRenderers()
        {
            if (_owner == null) return;
            if (_owner.Data == null) return;
            if (_owner.EditorShouldDeferVisualWorkInternal()) return;

            var labelController = _owner.LabelControllerInternal;
            if (labelController != null && _owner.PlotViewportInternal != null && _owner.LabelOverlayInternal != null)
            {
                // IMPORTANT: do NOT use worldBound here.
                // worldBound is affected by style.translate (category smooth scrolling), which would make
                // plotRectLocal drift and cause labels to be offset after each refresh.
                // layout is in parent-local coordinates and is stable under translate.
                var pv = _owner.PlotViewportInternal.layout;
                var overlay = _owner.LabelOverlayInternal.layout;
                var plotRectLocal = new Rect(pv.x - overlay.x, pv.y - overlay.y, pv.width, pv.height);
                float edgeFadePx = _owner.Data != null && _owner.Data.Plot != null ? _owner.Data.Plot.labelEdgeFadePx : 0f;
                labelController.BeginFrame(plotRectLocal, _owner, edgeFadePx);
            }

            var renderers = _owner.RenderersInternal;
            if (renderers == null) return;

            for (int i = 0; i < renderers.Count; i++)
            {
                var r = renderers[i];
                if (r == null) continue;

                r.Data = _owner.Data;

                var bg = _owner.ChartProfileInternal != null ? _owner.ChartProfileInternal.background : null;
                r.BackgroundColor = (bg != null && bg.show && bg.textureFill != null) ? bg.textureFill.color : Color.clear;

                r.SetRange(_owner.XMinInternal, _owner.XMaxInternal, _owner.YMinInternal, _owner.YMaxInternal);

                if (!r.visible) continue;
                if (r.parent == null) continue;
                if (r.panel == null) continue;
                r.UpdateLabels();
            }

            if (labelController != null)
            {
                labelController.EndFrame();
            }
        }

        public void OnRefreshLayersNoLegend()
        {
            if (_owner == null) return;
            if (_owner.Data == null) return;

            float tx = _owner.CategoryScrollControllerInternal.SmoothTranslating ? _owner.CategoryScrollControllerInternal.ScrollOffsetX : 0f;
            float ty = _owner.CategoryScrollControllerInternal.SmoothTranslating ? _owner.CategoryScrollControllerInternal.ScrollOffsetY : 0f;
            _owner.ApplyCategoryScrollTranslateInternal(tx, ty);

            bool showCartesian = _owner.Data.CoordinateSystem == CoordinateSystemType.Cartesian2D;
            if (_owner.GridLayerInternal != null) _owner.GridLayerInternal.visible = showCartesian;
            if (_owner.AxisLayerInternal != null) _owner.AxisLayerInternal.visible = showCartesian;

            if (_owner.EditorTryDeferVisualWorkInternal("RefreshLayersNoLegend deferred (coord change flag still set)")) return;

            if (showCartesian) OnRefreshAxisLayer();
            OnRefreshSeriesRenderers();
            if (showCartesian) OnRefreshGridLayer();
        }

        public void OnRefreshLegendDeferred()
        {
            if (_owner == null) return;
            _owner.EditorRefreshLegendDeferredInternal(RefreshLegend);
        }

        public void OnPlayAnimation()
        {
            if (_owner == null) return;
            _owner.PlayAnimationAutoInternal();
        }

        private void RefreshLegend()
        {
            if (_owner == null) return;

            _owner.EditorGetLegendTraceInternal(out bool trace, out Action<string> pushBreadcrumb);

            _owner.LegendControllerInternal.RefreshLegend(
                _owner.Data,
                trace,
                pushBreadcrumb,
                (reason) =>
                {
                    if (_kernel != null) _kernel.Invalidate(reason, immediate: true);
                });
        }
    }
}
