using System.Collections.Generic;
using UnityEngine;
using EasyChart.Layers;

namespace EasyChart
{
    public interface IChartTooltipPolicy
    {
        bool TryBuildTooltip(
            TooltipContext context,
            IList<BaseSeriesRenderer> renderers,
            List<TooltipItem> items,
            ref Vector2? cursorPosition,
            ref string categoryLabel);

        void ClearHover(IList<BaseSeriesRenderer> renderers);
    }
}
