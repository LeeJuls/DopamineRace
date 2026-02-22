using System;
using System.Collections.Generic;

namespace EasyChart
{
    public interface IChartLayoutModel
    {
        AxisId GetMappedXAxisId(ChartData data);
        AxisId GetMappedYAxisId(ChartData data);

        bool IsCartesianTransposed(ChartData data, Func<AxisId, AxisConfig> getAxis);

        ChartRangeResult CalculateRange(ChartData data, Func<AxisId, AxisConfig> getAxis, int categoryWindowStartX, int categoryWindowStartY);

        void UpdateCategoryAxisRangeOnly(
            ChartData data,
            Func<AxisId, AxisConfig> getAxis,
            ref int categoryWindowStartX,
            ref int categoryWindowStartY,
            ref float xMin,
            ref float xMax,
            ref float yMin,
            ref float yMax);

        List<string> GetCategoryLabelsWindowed(AxisId id, AxisConfig axis, int windowStart, List<string> buffer);
    }
}
