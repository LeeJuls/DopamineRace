using System.Collections.Generic;
using UnityEngine;

namespace EasyChart
{
    public struct ChartRangeResult
    {
        public float XMin;
        public float XMax;
        public float YMin;
        public float YMax;
        public int CategoryWindowStartX;
        public int CategoryWindowStartY;
    }

    internal static class ChartRangeCalculator
    {
        private static int ClampCategoryVisibleCount(AxisConfig axis, int labelsCount)
        {
            if (labelsCount <= 0) return 0;
            // When autoTicks is true, use all labels
            if (axis != null && axis.autoTicks) return labelsCount;
            int v = axis != null ? axis.splitCount : 0;
            if (v < 2) v = 2;
            if (v > labelsCount) v = labelsCount;
            return v;
        }

        public static ChartRangeResult Calculate(
            ChartData data,
            bool transposed,
            AxisConfig xAxisCfg,
            AxisConfig yAxisCfg,
            List<string> xCategoryLabels,
            List<string> yCategoryLabels,
            int categoryWindowStartX,
            int categoryWindowStartY)
        {
            var result = new ChartRangeResult
            {
                XMin = 0,
                XMax = 5,
                YMin = 0,
                YMax = 10,
                CategoryWindowStartX = categoryWindowStartX,
                CategoryWindowStartY = categoryWindowStartY,
            };

            if (data == null || data.Series == null || data.Series.Count == 0)
            {
                return result;
            }

            float xMin = float.MaxValue;
            float xMax = float.MinValue;
            float yMin = float.MaxValue;
            float yMax = float.MinValue;

            bool hasData = false;

            bool xIsCategory = xAxisCfg != null && xAxisCfg.axisType == AxisType.Category && xCategoryLabels != null && xCategoryLabels.Count > 0;
            bool yIsCategory = yAxisCfg != null && yAxisCfg.axisType == AxisType.Category && yCategoryLabels != null && yCategoryLabels.Count > 0;

            var stackPos = new Dictionary<string, Dictionary<int, float>>();
            var stackNeg = new Dictionary<string, Dictionary<int, float>>();

            foreach (var serie in data.Series)
            {
                if (serie == null || !serie.visible) continue;
                if (serie.type == SerieType.Pie || serie.type == SerieType.RingChart || serie.type == SerieType.Pie3D) continue;
                if (serie.type == SerieType.Radar) continue;
                if (serie.type == SerieType.Funnel) continue;

                // BoxPlot chart uses y=Min, z=Q1, value=Median, w=Q3, v=Max
                if (serie.type == SerieType.BoxPlot)
                {
                    if (serie.seriesData != null && serie.seriesData.Count > 0)
                    {
                        hasData = true;
                        for (int i = 0; i < serie.seriesData.Count; i++)
                        {
                            var p = serie.seriesData[i];
                            if (p == null) continue;

                            // BoxPlot: y=Min, v=Max
                            float min = p.y;
                            float max = p.v;

                            if (min < yMin) yMin = min;
                            if (max > yMax) yMax = max;

                            // X range for category axis
                            if (!xIsCategory)
                            {
                                if (i < xMin) xMin = i;
                                if (i > xMax) xMax = i;
                            }
                        }
                    }
                    continue;
                }

                // Candlestick/OHLC chart uses x=Open, y=High, z=Low, value=Close
                if (serie.type == SerieType.Candlestick || serie.type == SerieType.OHLC)
                {
                    if (serie.seriesData != null && serie.seriesData.Count > 0)
                    {
                        hasData = true;
                        for (int i = 0; i < serie.seriesData.Count; i++)
                        {
                            var p = serie.seriesData[i];
                            if (p == null) continue;

                            // OHLC: x=Open, y=High, z=Low, value=Close
                            float high = p.y;
                            float low = p.z;

                            if (low < yMin) yMin = low;
                            if (high > yMax) yMax = high;

                            // X range for category axis
                            if (!xIsCategory)
                            {
                                if (i < xMin) xMin = i;
                                if (i > xMax) xMax = i;
                            }
                        }
                    }
                    continue;
                }

                // Waterfall chart needs cumulative Y range calculation
                if (serie.type == SerieType.Waterfall)
                {
                    if (serie.seriesData != null && serie.seriesData.Count > 0)
                    {
                        hasData = true;
                        float runningTotal = 0f;
                        float waterfallYMin = 0f;
                        float waterfallYMax = 0f;

                        for (int i = 0; i < serie.seriesData.Count; i++)
                        {
                            var p = serie.seriesData[i];
                            if (p == null) continue;

                            // Check if this is a total bar (z > 0.5 or name contains "total")
                            bool isTotal = p.z > 0.5f || (!string.IsNullOrEmpty(p.name) && p.name.ToLower().Contains("total"));

                            float startY, endY;
                            if (isTotal)
                            {
                                startY = 0f;
                                endY = p.value;
                                runningTotal = p.value;
                            }
                            else
                            {
                                if (p.value >= 0)
                                {
                                    startY = runningTotal;
                                    endY = runningTotal + p.value;
                                }
                                else
                                {
                                    startY = runningTotal + p.value;
                                    endY = runningTotal;
                                }
                                runningTotal += p.value;
                            }

                            waterfallYMin = Mathf.Min(waterfallYMin, Mathf.Min(startY, endY));
                            waterfallYMax = Mathf.Max(waterfallYMax, Mathf.Max(startY, endY));

                            // X range for category axis
                            if (!xIsCategory)
                            {
                                if (i < xMin) xMin = i;
                                if (i > xMax) xMax = i;
                            }
                        }

                        if (waterfallYMin < yMin) yMin = waterfallYMin;
                        if (waterfallYMax > yMax) yMax = waterfallYMax;
                    }
                    continue;
                }

                // HeatMap uses x/y as coordinates, not category index
                bool isHeatmap = serie.type == SerieType.Heatmap;

                bool isStackedBar = false;
                bool isStackedLine = false;
                string stackKey = null;
                if (serie.type == SerieType.Bar && serie.settings is BarSettings barSettings)
                {
                    isStackedBar = barSettings.stacked;
                    if (isStackedBar)
                    {
                        stackKey = string.IsNullOrEmpty(barSettings.stackGroup) ? "__default__" : barSettings.stackGroup;
                    }
                }
                else if (serie.type == SerieType.Line && serie.settings is LineSettings lineSettings)
                {
                    isStackedLine = lineSettings.stacked;
                    if (isStackedLine)
                    {
                        stackKey = "__default_line__";
                    }
                }

                if (serie.seriesData == null) continue;
                foreach (var p in serie.seriesData)
                {
                    if (p == null) continue;
                    hasData = true;

                    float px = p.x;
                    float py = p.value;

                    // HeatMap uses x/y as 2D coordinates
                    if (isHeatmap)
                    {
                        py = p.y;
                        // Update both X and Y ranges for heatmap
                        if (px < xMin) xMin = px;
                        if (px > xMax) xMax = px;
                        if (py < yMin) yMin = py;
                        if (py > yMax) yMax = py;
                        continue;
                    }

                    if (serie.type == SerieType.Scatter && serie.settings is ScatterSettings scatterSettings)
                    {
                        py = p.y;
                        if (Mathf.Approximately(py, 0f) && !Mathf.Approximately(p.value, 0f))
                        {
                            py = p.value;
                        }
                    }

                    if (transposed)
                    {
                        if (py < xMin) xMin = py;
                        if (py > xMax) xMax = py;

                        if (px < yMin) yMin = px;
                        if (px > yMax) yMax = px;
                    }
                    else
                    {
                        if (px < xMin) xMin = px;
                        if (px > xMax) xMax = px;
                    }

                    if (isStackedBar || isStackedLine)
                    {
                        int stackIndex = Mathf.RoundToInt(p.x);
                        float stackValue = p.value;
                        if (stackValue >= 0)
                        {
                            if (!stackPos.TryGetValue(stackKey, out var dict))
                            {
                                dict = new Dictionary<int, float>();
                                stackPos[stackKey] = dict;
                            }
                            if (!dict.ContainsKey(stackIndex)) dict[stackIndex] = 0;
                            dict[stackIndex] += stackValue;
                        }
                        else
                        {
                            if (!stackNeg.TryGetValue(stackKey, out var dict))
                            {
                                dict = new Dictionary<int, float>();
                                stackNeg[stackKey] = dict;
                            }
                            if (!dict.ContainsKey(stackIndex)) dict[stackIndex] = 0;
                            dict[stackIndex] += stackValue;
                        }
                    }
                    else
                    {
                        if (transposed)
                        {
                        }
                        else
                        {
                            if (py < yMin) yMin = py;
                            if (py > yMax) yMax = py;
                        }
                    }
                }
            }

            foreach (var group in stackPos.Values)
            {
                foreach (var kvp in group)
                {
                    if (transposed)
                    {
                        if (kvp.Value > xMax) xMax = kvp.Value;
                    }
                    else
                    {
                        if (kvp.Value > yMax) yMax = kvp.Value;
                    }
                }
            }
            foreach (var group in stackNeg.Values)
            {
                foreach (var kvp in group)
                {
                    if (transposed)
                    {
                        if (kvp.Value < xMin) xMin = kvp.Value;
                    }
                    else
                    {
                        if (kvp.Value < yMin) yMin = kvp.Value;
                    }
                }
            }

            bool hasStacked = stackPos.Count > 0 || stackNeg.Count > 0;
            if (hasStacked)
            {
                if (transposed)
                {
                    if (xMin == float.MaxValue) xMin = 0f;
                    if (xMax == float.MinValue) xMax = 0f;

                    xMin = Mathf.Min(xMin, 0f);
                    xMax = Mathf.Max(xMax, 0f);
                }
                else
                {
                    if (yMin == float.MaxValue) yMin = 0f;
                    if (yMax == float.MinValue) yMax = 0f;

                    yMin = Mathf.Min(yMin, 0f);
                    yMax = Mathf.Max(yMax, 0f);
                }
            }

            if (!hasData)
            {
                return result;
            }

            if (xIsCategory)
            {
                int count = xCategoryLabels.Count;
                int visible = ClampCategoryVisibleCount(xAxisCfg, count);
                if (count <= visible)
                {
                    result.CategoryWindowStartX = 0;
                    xMin = 0;
                    xMax = count > 1 ? count - 1 : 1;
                }
                else
                {
                    result.CategoryWindowStartX = Mathf.Clamp(result.CategoryWindowStartX, 0, count - 1);
                    xMin = result.CategoryWindowStartX;
                    xMax = result.CategoryWindowStartX + (visible - 1);
                }
            }
            else
            {
                if (xMax == xMin) xMax += 1;
            }

            if (yIsCategory)
            {
                int count = yCategoryLabels.Count;
                int visible = ClampCategoryVisibleCount(yAxisCfg, count);
                if (count <= visible)
                {
                    result.CategoryWindowStartY = 0;
                    yMin = 0;
                    yMax = count > 1 ? count - 1 : 1;
                }
                else
                {
                    result.CategoryWindowStartY = Mathf.Clamp(result.CategoryWindowStartY, 0, count - 1);
                    yMin = result.CategoryWindowStartY;
                    yMax = result.CategoryWindowStartY + (visible - 1);
                }
            }
            else
            {
                if (yMax == yMin) yMax += 1;
            }

            bool xIsValue = xAxisCfg != null && xAxisCfg.axisType == AxisType.Value;
            bool yIsValue = yAxisCfg != null && yAxisCfg.axisType == AxisType.Value;

            bool xAutoMin = xIsValue && xAxisCfg != null && xAxisCfg.autoRangeMin;
            bool xAutoMax = xIsValue && xAxisCfg != null && xAxisCfg.autoRangeMax;
            bool yAutoMin = yIsValue && yAxisCfg != null && yAxisCfg.autoRangeMin;
            bool yAutoMax = yIsValue && yAxisCfg != null && yAxisCfg.autoRangeMax;

            bool xManualMin = xIsValue && xAxisCfg != null && !xAxisCfg.autoRangeMin;
            bool xManualMax = xIsValue && xAxisCfg != null && !xAxisCfg.autoRangeMax;
            bool yManualMin = yIsValue && yAxisCfg != null && !yAxisCfg.autoRangeMin;
            bool yManualMax = yIsValue && yAxisCfg != null && !yAxisCfg.autoRangeMax;

            if (xManualMin) xMin = xAxisCfg.minValue;
            if (xManualMax) xMax = xAxisCfg.maxValue;
            if (yManualMin) yMin = yAxisCfg.minValue;
            if (yManualMax) yMax = yAxisCfg.maxValue;

            bool hasStackedBarOrLine = false;
            if (data.Series != null)
            {
                for (int i = 0; i < data.Series.Count; i++)
                {
                    var s = data.Series[i];
                    if (s == null || !s.visible) continue;
                    if (s.type == SerieType.Bar && s.settings is BarSettings bs && bs.stacked)
                    {
                        hasStackedBarOrLine = true;
                        break;
                    }
                    if (s.type == SerieType.Line && s.settings is LineSettings ls && ls.stacked)
                    {
                        hasStackedBarOrLine = true;
                        break;
                    }
                }
            }

            bool preferZeroMinX = xIsValue && transposed && hasStackedBarOrLine;
            bool preferZeroMinY = yIsValue && !transposed && hasStackedBarOrLine;

            if (xIsValue) ExpandAutoRange(ref xMin, ref xMax, xAutoMin, xAutoMax, preferZeroMinX);
            if (yIsValue) ExpandAutoRange(ref yMin, ref yMax, yAutoMin, yAutoMax, preferZeroMinY);

            if (xIsValue) ApplyRounding(ref xMin, ref xMax, xAxisCfg, xAutoMin, xAutoMax);
            if (yIsValue) ApplyRounding(ref yMin, ref yMax, yAxisCfg, yAutoMin, yAutoMax);

            result.XMin = xMin;
            result.XMax = xMax;
            result.YMin = yMin;
            result.YMax = yMax;

            return result;
        }

        private static void EnsureNonZeroRange(ref float minV, ref float maxV, bool canAdjustMin, bool canAdjustMax)
        {
            if (!Mathf.Approximately(minV, maxV)) return;
            if (canAdjustMax) maxV += 10f;
            else if (canAdjustMin) minV -= 10f;
            else maxV += 1f;
        }

        private static void ExpandAutoRange(ref float minV, ref float maxV, bool autoMin, bool autoMax, bool preferZeroMinWhenNearZero)
        {
            if (!autoMin && !autoMax) return;
            EnsureNonZeroRange(ref minV, ref maxV, autoMin, autoMax);

            float range = maxV - minV;
            if (autoMax) maxV += range * 0.1f;
            if (autoMin)
            {
                if (preferZeroMinWhenNearZero && Mathf.Abs(minV) < 0.00001f && maxV > 0f) minV = 0f;
                else if (minV > 0 && minV / (maxV + float.Epsilon) < 0.2f) minV = 0;
                else minV -= range * 0.1f;
            }
        }

        private static void ApplyRounding(ref float minV, ref float maxV, AxisConfig axis, bool autoMin, bool autoMax)
        {
            if (axis == null) return;
            if (!autoMin && !autoMax) return;

            float unit;
            switch (axis.autoRangeRounding)
            {
                case AutoRangeRoundingMode.Integer: unit = 1f; break;
                case AutoRangeRoundingMode.Tens: unit = 10f; break;
                case AutoRangeRoundingMode.Hundreds: unit = 100f; break;
                case AutoRangeRoundingMode.Custom: unit = axis.autoRangeUnit; break;
                case AutoRangeRoundingMode.NiceNumbers:
                    ApplyNiceNumberRounding(ref minV, ref maxV, axis.splitCount, autoMin, autoMax);
                    return;
                default: return;
            }

            unit = Mathf.Abs(unit);
            if (unit <= 0f) return;

            if (autoMin) minV = Mathf.Floor(minV / unit) * unit;
            if (autoMax) maxV = Mathf.Ceil(maxV / unit) * unit;
            if (Mathf.Approximately(minV, maxV))
            {
                if (autoMax) maxV += unit;
                else if (autoMin) minV -= unit;
            }
        }

        private static void ApplyNiceNumberRounding(ref float minV, ref float maxV, int splitCount, bool autoMin, bool autoMax)
        {
            if (splitCount < 1) splitCount = 5;
            
            float range = maxV - minV;
            if (range <= 0) return;

            // Calculate raw step
            float rawStep = range / splitCount;
            
            // Find nice step (1, 2, 5 or their powers of 10)
            float niceStep = CalculateNiceStep(rawStep);
            
            // Adjust min and max to be multiples of niceStep
            if (autoMin)
            {
                minV = Mathf.Floor(minV / niceStep) * niceStep;
            }
            if (autoMax)
            {
                maxV = Mathf.Ceil(maxV / niceStep) * niceStep;
            }
            
            // Ensure we have at least the requested number of splits
            float newRange = maxV - minV;
            int actualSplits = Mathf.RoundToInt(newRange / niceStep);
            if (actualSplits < splitCount && autoMax)
            {
                maxV = minV + niceStep * splitCount;
            }
            
            if (Mathf.Approximately(minV, maxV))
            {
                if (autoMax) maxV += niceStep;
                else if (autoMin) minV -= niceStep;
            }
        }

        private static float CalculateNiceStep(float rawStep)
        {
            if (rawStep <= 0) return 1f;
            
            // Find the magnitude (power of 10)
            float magnitude = Mathf.Pow(10f, Mathf.Floor(Mathf.Log10(rawStep)));
            
            // Normalize to 1-10 range
            float normalized = rawStep / magnitude;
            
            // Round to nice number (1, 2, 5, 10)
            float niceNormalized;
            if (normalized <= 1f) niceNormalized = 1f;
            else if (normalized <= 2f) niceNormalized = 2f;
            else if (normalized <= 5f) niceNormalized = 5f;
            else niceNormalized = 10f;
            
            return niceNormalized * magnitude;
        }
    }
}
