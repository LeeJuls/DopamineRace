using System;

namespace EasyChart
{
    [Flags]
    internal enum ChartDirtyReason
    {
        None = 0,
        DataAssigned = 1 << 0,
        DataMutated = 1 << 1,
        ProfileApplied = 1 << 2,
        ServicesChanged = 1 << 3,
        GeometryChanged = 1 << 4,
        CategoryWindowChanged = 1 << 5,
        LegendTogglePieSlice = 1 << 6,
        LegendToggleSeriesVisibility = 1 << 7,
        LayoutRefresh = 1 << 8,
        Tooltip = 1 << 9,
    }
}
