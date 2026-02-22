using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using EasyChart.Layers;

namespace EasyChart
{
    public sealed class DefaultChartInteractionPolicy : IChartInteractionPolicy
    {
        public static readonly DefaultChartInteractionPolicy Instance = new DefaultChartInteractionPolicy();

        private DefaultChartInteractionPolicy() { }

        public void OnPointerMove(
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
            VisualElement cursorLine)
        {
            if (tooltip == null) return;
            tooltip.OnPointerMove(
                host,
                pointerWorldPos,
                data,
                renderers,
                chartArea,
                padding,
                isTransposed,
                isCategorySmoothTranslating,
                tooltipLabel,
                cursorLine);
        }

        public void OnPointerLeave(
            IChartTooltipRuntime tooltip,
            IList<BaseSeriesRenderer> renderers,
            Label tooltipLabel,
            VisualElement cursorLine)
        {
            if (tooltip == null) return;
            tooltip.OnPointerLeave(renderers, tooltipLabel, cursorLine);
        }
    }
}
