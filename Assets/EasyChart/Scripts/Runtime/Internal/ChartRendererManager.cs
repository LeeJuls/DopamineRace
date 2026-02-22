using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using EasyChart.Layers;

namespace EasyChart
{
    public struct RendererSelection
    {
        public readonly bool NeedLine;
        public readonly bool NeedScatter;
        public readonly bool NeedBar;
        public readonly bool NeedRadar;
        public readonly bool NeedPie;
        public readonly HashSet<SerieType> NeedDynamic;
        public readonly bool ShowCartesian;
        public readonly bool AllowOverflow;

        public RendererSelection(
            bool needLine,
            bool needScatter,
            bool needBar,
            bool needRadar,
            bool needPie,
            HashSet<SerieType> needDynamic,
            bool showCartesian,
            bool allowOverflow)
        {
            NeedLine = needLine;
            NeedScatter = needScatter;
            NeedBar = needBar;
            NeedRadar = needRadar;
            NeedPie = needPie;
            NeedDynamic = needDynamic ?? new HashSet<SerieType>();
            ShowCartesian = showCartesian;
            AllowOverflow = allowOverflow;
        }
    }

    internal static class ChartRendererManager
    {
        public static RendererSelection Select(ChartData data)
        {
            bool needLine = false;
            bool needScatter = false;
            bool needBar = false;
            bool needRadar = false;
            bool needPie = false;
            var needDynamic = new HashSet<SerieType>();

            bool showCartesian = data != null && data.CoordinateSystem == CoordinateSystemType.Cartesian2D;
            bool isPolar = data != null && data.CoordinateSystem == CoordinateSystemType.Polar2D;
            bool isNoneCoord = data != null && data.CoordinateSystem == CoordinateSystemType.None;

            if (data != null && data.Series != null)
            {
                foreach (var s in data.Series)
                {
                    if (s == null || !s.visible) continue;

                    if (s.type == SerieType.Pie)
                    {
                        needPie = true;
                        continue;
                    }

                    if (s.type == SerieType.Pie3D)
                    {
                        needDynamic.Add(s.type);
                        continue;
                    }

                    if (s.type == SerieType.Line) needLine = true;
                    else if (s.type == SerieType.Scatter) needScatter = true;
                    else if (s.type == SerieType.Bar) needBar = true;
                    else if (s.type == SerieType.HorizontalBar) needDynamic.Add(s.type);
                    else if (s.type == SerieType.Radar) needRadar = true;
                    // Heatmap and RingChart are Pro features - handled as dynamic renderers
                    else if (s.type == SerieType.Heatmap) needDynamic.Add(s.type);
                    else if (s.type == SerieType.RingChart) needDynamic.Add(s.type);
                    else needDynamic.Add(s.type);
                }
            }

            if (!needLine && !needScatter && !needBar && !needRadar && !needPie)
            {
                if (isNoneCoord) needPie = true;
                else if (isPolar) needRadar = true;
                else needLine = true;
            }

            bool allowOverflow = isPolar;
            return new RendererSelection(
                needLine,
                needScatter,
                needBar,
                needRadar,
                needPie,
                needDynamic,
                showCartesian,
                allowOverflow);
        }

        public static BaseSeriesRenderer EnsureDynamicRenderer(
            SerieType type,
            Dictionary<SerieType, BaseSeriesRenderer> dynamicRenderers,
            VisualElement plotContentRoot,
            System.Func<SerieType, BaseSeriesRenderer> factory = null)
        {
            if (dynamicRenderers != null && dynamicRenderers.TryGetValue(type, out var existing) && existing != null)
            {
                return existing;
            }

            BaseSeriesRenderer created = null;
            if (factory != null)
            {
                created = factory(type);
            }
            if (created == null)
            {
                EasyChart.Layers.SerieRendererRegistry.TryCreate(type, out created);
            }

            if (created != null)
            {
                created.visible = true;
                created.pickingMode = PickingMode.Ignore;
                created.StretchToParentSize();
                if (plotContentRoot != null) plotContentRoot.Add(created);
                if (dynamicRenderers != null) dynamicRenderers[type] = created;
                return created;
            }

            return null;
        }
    }
}
