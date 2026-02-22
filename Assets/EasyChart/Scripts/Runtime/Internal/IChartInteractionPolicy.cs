using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using EasyChart.Layers;

namespace EasyChart
{
    public interface IChartInteractionPolicy
    {
        void OnPointerMove(
            IChartTooltipRuntime tooltip,
            VisualElement host,
            Vector2 pointerWorldPos,
            ChartData data,
            IList<BaseSeriesRenderer> renderers,
            VisualElement chartArea,
            Vector4 padding,
            bool isTransposed,
            bool isCategorySmoothTranslating,
            Label tooltipLabel,
            VisualElement cursorLine);

        void OnPointerLeave(
            IChartTooltipRuntime tooltip,
            IList<BaseSeriesRenderer> renderers,
            Label tooltipLabel,
            VisualElement cursorLine);
    }
}
