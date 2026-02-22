using System;

namespace EasyChart
{
    [Flags]
    internal enum ChartRefreshStage
    {
        None = 0,
        RebuildRenderers = 1 << 0,
        CalculateRange = 1 << 1,
        LayersNoLegend = 1 << 2,
        AxisLayer = 1 << 3,
        Grid = 1 << 4,
        SeriesRenderers = 1 << 5,
        Legend = 1 << 6,
        PlayAnimation = 1 << 7,
        Layout = 1 << 8,
    }
}
