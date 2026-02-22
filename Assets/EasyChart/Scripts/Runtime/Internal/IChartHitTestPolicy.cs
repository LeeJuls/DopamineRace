using System.Collections.Generic;
using UnityEngine;
using EasyChart.Layers;

namespace EasyChart
{
    public interface IChartHitTestPolicy
    {
        bool TryBuildTooltipItems(
            TooltipContext context,
            IList<BaseSeriesRenderer> renderers,
            List<TooltipItem> items,
            ref Vector2? cursorPosition,
            ref string categoryLabel);
    }
}
