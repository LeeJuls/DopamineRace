using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using EasyChart.Layers;

namespace EasyChart
{
    public interface IChartTooltipRuntime
    {
        void OnPointerMove(
            VisualElement host,
            Vector2 pointerWorldPos,
            ChartData data,
            IList<BaseSeriesRenderer> renderers,
            VisualElement chartArea,
            Vector4 padding,
            bool isTransposed,
            bool isCategorySmoothTranslating,
            Label tooltip,
            VisualElement cursorLine);

        void OnPointerLeave(IList<BaseSeriesRenderer> renderers, Label tooltip, VisualElement cursorLine);

        void Hide(Label tooltip, VisualElement cursorLine);
    }
}
