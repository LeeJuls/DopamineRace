namespace EasyChart
{
    internal interface IChartRefreshStageModule
    {
        void OnRebuildRenderers();
        void OnCalculateRange();
        void OnRefreshAxisLayer();
        void OnRefreshGridLayer();
        void OnRefreshSeriesRenderers();
        void OnRefreshLayersNoLegend();
        void OnRefreshLegendDeferred();
        void OnPlayAnimation();
    }
}
