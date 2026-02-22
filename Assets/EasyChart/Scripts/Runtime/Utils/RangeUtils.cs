using System;
using System.Collections.Generic;
using UnityEngine;

namespace EasyChart
{
    public static class RangeUtils
    {
        public static void ResolveAutoRange<T>(IList<T> items,
            Func<T, float> selector,
            bool autoRange,
            float manualMin,
            float manualMax,
            out float min,
            out float max,
            bool ignoreNaN = true,
            bool ignoreInfinity = true)
        {
            min = manualMin;
            max = manualMax;

            if (!autoRange) return;
            if (items == null || items.Count == 0) return;
            if (selector == null) return;

            bool has = false;
            float foundMin = float.MaxValue;
            float foundMax = float.MinValue;

            for (int i = 0; i < items.Count; i++)
            {
                float v;
                try
                {
                    v = selector(items[i]);
                }
                catch
                {
                    continue;
                }

                if (ignoreNaN && float.IsNaN(v)) continue;
                if (ignoreInfinity && float.IsInfinity(v)) continue;

                if (!has)
                {
                    foundMin = foundMax = v;
                    has = true;
                }
                else
                {
                    if (v < foundMin) foundMin = v;
                    if (v > foundMax) foundMax = v;
                }
            }

            if (!has) return;

            min = foundMin;
            max = foundMax;
        }

        public static void ResolveAutoRange(IList<Serie> series,
            int maxPointsPerSerie,
            Func<SeriesData, float> selector,
            bool autoRange,
            float manualMin,
            float manualMax,
            out float min,
            out float max,
            bool ignoreNaN = true,
            bool ignoreInfinity = true)
        {
            min = manualMin;
            max = manualMax;

            if (!autoRange) return;
            if (series == null || series.Count == 0) return;
            if (selector == null) return;

            bool has = false;
            float foundMin = float.MaxValue;
            float foundMax = float.MinValue;

            for (int si = 0; si < series.Count; si++)
            {
                var serie = series[si];
                if (serie == null || serie.seriesData == null) continue;

                int count = Mathf.Min(maxPointsPerSerie, serie.seriesData.Count);
                for (int i = 0; i < count; i++)
                {
                    var dp = serie.seriesData[i];
                    if (dp == null) continue;

                    float v;
                    try
                    {
                        v = selector(dp);
                    }
                    catch
                    {
                        continue;
                    }

                    if (ignoreNaN && float.IsNaN(v)) continue;
                    if (ignoreInfinity && float.IsInfinity(v)) continue;

                    if (!has)
                    {
                        foundMin = foundMax = v;
                        has = true;
                    }
                    else
                    {
                        if (v < foundMin) foundMin = v;
                        if (v > foundMax) foundMax = v;
                    }
                }
            }

            if (!has) return;

            min = foundMin;
            max = foundMax;
        }

        public static void ResolveAutoRange(IList<SeriesData> items,
            bool autoRange,
            float manualMin,
            float manualMax,
            Func<SeriesData, float> selector,
            out float min,
            out float max)
        {
            ResolveAutoRange(items, selector, autoRange, manualMin, manualMax, out min, out max);
        }
    }
}
