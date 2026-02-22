using System;

namespace EasyChart
{
    internal readonly struct ChartRefreshCallbacks
    {
        public readonly Action RebuildRenderers;
        public readonly Action CalculateRange;
        public readonly Action RefreshAxisLayer;
        public readonly Action RefreshGridLayer;
        public readonly Action RefreshSeriesRenderers;
        public readonly Action RefreshLayersNoLegend;
        public readonly Action RefreshLegendDeferred;
        public readonly Action PlayAnimation;

        public ChartRefreshCallbacks(
            Action rebuildRenderers,
            Action calculateRange,
            Action refreshAxisLayer,
            Action refreshGridLayer,
            Action refreshSeriesRenderers,
            Action refreshLayersNoLegend,
            Action refreshLegendDeferred,
            Action playAnimation)
        {
            RebuildRenderers = rebuildRenderers;
            CalculateRange = calculateRange;
            RefreshAxisLayer = refreshAxisLayer;
            RefreshGridLayer = refreshGridLayer;
            RefreshSeriesRenderers = refreshSeriesRenderers;
            RefreshLayersNoLegend = refreshLayersNoLegend;
            RefreshLegendDeferred = refreshLegendDeferred;
            PlayAnimation = playAnimation;
        }
    }
}
