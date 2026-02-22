using System.Collections.Generic;
using UnityEngine;
using EasyChart.Layers;

namespace EasyChart
{
    public sealed class DefaultChartTooltipPolicy : IChartTooltipPolicy
    {
        public static readonly DefaultChartTooltipPolicy Instance = new DefaultChartTooltipPolicy();

        private DefaultChartTooltipPolicy() { }

        public bool TryBuildTooltip(
            TooltipContext context,
            IList<BaseSeriesRenderer> renderers,
            List<TooltipItem> items,
            ref Vector2? cursorPosition,
            ref string categoryLabel)
        {
            if (renderers == null || items == null) return false;

            bool exclusiveHit = false;
            for (int i = renderers.Count - 1; i >= 0; i--)
            {
                var renderer = renderers[i];
                if (renderer == null) continue;

                bool hit = renderer.GetTooltip(context, items, ref cursorPosition, ref categoryLabel);
                if (hit && renderer is IExclusiveTooltipRenderer)
                {
                    exclusiveHit = true;
                }

                if (exclusiveHit) break;
            }

            return items.Count > 0;
        }

        public void ClearHover(IList<BaseSeriesRenderer> renderers)
        {
            if (renderers == null) return;

            for (int i = 0; i < renderers.Count; i++)
            {
                var r = renderers[i];
                r?.ClearHover();
            }
        }
    }
}
