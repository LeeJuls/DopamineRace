using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace EasyChart
{
    /// <summary>
    /// JSON generation mode for chart data.
    /// Simplified to 3 core modes for ease of use.
    /// </summary>
    public enum ChartJsonMode
    {
        /// <summary>
        /// Compact format - data values only.
        /// Output: {"series": [{"datas": [1, 2, 3]}]}
        /// Best for: Quick data updates, minimal payload.
        /// </summary>
        Compact,
        
        /// <summary>
        /// Standard format - includes names and structured data.
        /// Output: {"chartName": "...", "series": [{"name": "...", "datas": [{x, value}]}]}
        /// Best for: Most common use cases.
        /// </summary>
        Standard,
        
        /// <summary>
        /// Full format - all metadata including axes, types, colors.
        /// Output: {"chartId": "...", "chartName": "...", "axes": [...], "series": [{all fields}]}
        /// Best for: Complete chart configuration, import/export.
        /// </summary>
        Full
    }

    // Legacy enums for backward compatibility
    /// <summary>[Obsolete] Use ChartJsonMode instead.</summary>
    public enum ChartJsonExampleMode
    {
        Lite_Index = 0,
        Lite_Name = 1,
        Lite_ID = 2,
        Standard = 3,
        Standard_Axis = 4,
        Full = 5
    }

    /// <summary>[Obsolete] Use ChartJsonMode instead. Data format is now automatic.</summary>
    public enum ChartJsonDatasMode
    {
        Values = 0,
        Standard = 1,
        Full = 2
    }

    /// <summary>
    /// Utility class for JSON generation and parsing for EasyChart.
    /// Shared between Editor (LibraryWindow) and Runtime (UGUIRuntimeJsonInjection).
    /// Format matches EasyChartLibraryWindow.JsonPanel exactly.
    /// </summary>
    public static class ChartJsonUtils
    {
        #region JSON Generation (Simplified API)

        /// <summary>
        /// Build JSON for chart data injection using simplified mode.
        /// </summary>
        /// <param name="profile">The chart profile to serialize.</param>
        /// <param name="mode">JSON generation mode (Compact, Standard, or Full).</param>
        /// <param name="chartId">Optional chart ID for Full mode.</param>
        /// <returns>JSON string for injection.</returns>
        public static string BuildJson(ChartProfile profile, ChartJsonMode mode, string chartId = null)
        {
            if (profile == null) return string.Empty;

            switch (mode)
            {
                case ChartJsonMode.Compact:
                    return BuildCompactJson(profile);
                case ChartJsonMode.Standard:
                    return BuildStandardJson(profile);
                case ChartJsonMode.Full:
                    return BuildFullJson(profile, chartId);
                default:
                    return string.Empty;
            }
        }

        /// <summary>
        /// Build compact JSON - data values only, minimal payload.
        /// Format: {"series": [{"datas": [1, 2, 3]}]}
        /// </summary>
        private static string BuildCompactJson(ChartProfile profile)
        {
            if (profile == null) return string.Empty;
            var sb = new StringBuilder();
            sb.Append("{\n  \"series\": [\n");

            for (int i = 0; i < profile.series.Count; i++)
            {
                var s = profile.series[i];
                if (i > 0) sb.Append(",\n");
                sb.Append("    {\n");
                AppendDatasArraySimplified(sb, s, ChartJsonMode.Compact);
                sb.Append("\n    }");
            }

            sb.Append("\n  ]\n}");
            return sb.ToString();
        }

        /// <summary>
        /// Build standard JSON - includes names and structured data.
        /// Format: {"chartName": "...", "series": [{"name": "...", "datas": [{x, value}]}]}
        /// </summary>
        private static string BuildStandardJson(ChartProfile profile)
        {
            if (profile == null) return string.Empty;
            var sb = new StringBuilder();
            sb.Append("{\n");
            sb.Append($"  \"chartName\": \"{JsonEscape(profile.chartName)}\",\n");
            sb.Append("  \"series\": [\n");

            for (int i = 0; i < profile.series.Count; i++)
            {
                var s = profile.series[i];
                if (i > 0) sb.Append(",\n");
                sb.Append("    {\n");
                sb.Append($"      \"name\": \"{JsonEscape(s.name)}\",\n");
                AppendDatasArraySimplified(sb, s, ChartJsonMode.Standard);
                sb.Append("\n    }");
            }

            sb.Append("\n  ]\n}");
            return sb.ToString();
        }

        /// <summary>
        /// Build full JSON - all metadata including axes, types, colors.
        /// </summary>
        private static string BuildFullJson(ChartProfile profile, string chartId)
        {
            if (profile == null) return string.Empty;
            var sb = new StringBuilder();
            sb.Append("{\n");
            
            if (!string.IsNullOrEmpty(chartId))
                sb.Append($"  \"chartId\": \"{JsonEscape(chartId)}\",\n");
            sb.Append($"  \"chartName\": \"{JsonEscape(profile.chartName)}\",\n");

            // Check if any series needs axes (exclude Gauge, Pie, Radar, Funnel, RingChart)
            bool needsAxes = false;
            foreach (var s in profile.series)
            {
                if (s == null) continue;
                var t = s.type;
                if (t != SerieType.Gauge && t != SerieType.Pie && t != SerieType.Pie3D && 
                    t != SerieType.Radar && t != SerieType.Funnel && t != SerieType.RingChart)
                {
                    needsAxes = true;
                    break;
                }
            }

            // Axes - only include if needed
            if (needsAxes)
            {
                sb.Append("  \"axes\": [\n");
                AppendAxisLabels(sb, profile, AxisId.XBottom);
                sb.Append(",\n");
                AppendAxisLabels(sb, profile, AxisId.YLeft);
                sb.Append("\n  ],\n");
            }

            // Series
            sb.Append("  \"series\": [\n");
            for (int i = 0; i < profile.series.Count; i++)
            {
                var s = profile.series[i];
                if (i > 0) sb.Append(",\n");
                sb.Append("    {\n");
                if (!string.IsNullOrEmpty(s.id))
                    sb.Append($"      \"serieId\": \"{JsonEscape(s.id)}\",\n");
                sb.Append($"      \"name\": \"{JsonEscape(s.name)}\",\n");
                sb.Append($"      \"type\": {(int)s.type},\n");
                AppendDatasArraySimplified(sb, s, ChartJsonMode.Full);
                sb.Append("\n    }");
            }
            sb.Append("\n  ]\n}");

            return sb.ToString();
        }

        /// <summary>
        /// Append datas array with automatic format based on serie type and mode.
        /// </summary>
        private static void AppendDatasArraySimplified(StringBuilder sb, Serie s, ChartJsonMode mode)
        {
            if (s == null || s.seriesData == null || s.seriesData.Count == 0)
            {
                sb.Append("      \"datas\": []");
                return;
            }

            var serieType = s.type;
            bool isCompact = mode == ChartJsonMode.Compact;
            bool isCandlestickOrOHLC = serieType == SerieType.Candlestick || serieType == SerieType.OHLC;
            bool isBoxPlot = serieType == SerieType.BoxPlot;
            bool isHeatmap = serieType == SerieType.Heatmap;
            bool isHorizontalBar = serieType == SerieType.HorizontalBar;
            bool isScatter = serieType == SerieType.Scatter;
            bool isScatter3D = serieType == SerieType.Scatter3D;
            bool is3D = serieType == SerieType.Bar3D || serieType == SerieType.Line3D || 
                        serieType == SerieType.Scatter3D || serieType == SerieType.Pie3D;

            if (isCompact)
                sb.Append("      \"datas\": [");
            else
                sb.Append("      \"datas\": [\n");

            for (int pi = 0; pi < s.seriesData.Count; pi++)
            {
                var dp = s.seriesData[pi];
                if (pi > 0)
                {
                    if (isCompact) sb.Append(", ");
                    else sb.Append(",\n");
                }

                if (dp == null)
                {
                    if (isCompact) sb.Append("0");
                    else sb.Append("        { \"value\": 0 }");
                    continue;
                }

                if (isCompact)
                {
                    // Compact mode: use arrays for special types, single value for standard
                    if (isCandlestickOrOHLC)
                    {
                        sb.Append($"[{dp.x.ToString("R", CultureInfo.InvariantCulture)},{dp.y.ToString("R", CultureInfo.InvariantCulture)},{dp.z.ToString("R", CultureInfo.InvariantCulture)},{dp.value.ToString("R", CultureInfo.InvariantCulture)}]");
                    }
                    else if (isBoxPlot)
                    {
                        sb.Append($"[{dp.y.ToString("R", CultureInfo.InvariantCulture)},{dp.z.ToString("R", CultureInfo.InvariantCulture)},{dp.value.ToString("R", CultureInfo.InvariantCulture)},{dp.w.ToString("R", CultureInfo.InvariantCulture)},{dp.v.ToString("R", CultureInfo.InvariantCulture)}]");
                    }
                    else if (isScatter3D)
                    {
                        // Scatter3D: [x, y, z, value]
                        sb.Append($"[{dp.x.ToString("R", CultureInfo.InvariantCulture)},{dp.y.ToString("R", CultureInfo.InvariantCulture)},{dp.z.ToString("R", CultureInfo.InvariantCulture)},{dp.value.ToString("R", CultureInfo.InvariantCulture)}]");
                    }
                    else if (isScatter)
                    {
                        // Scatter: [x, y, value] - consistent with Scatter3D (x, y, z, value)
                        sb.Append($"[{dp.x.ToString("R", CultureInfo.InvariantCulture)},{dp.y.ToString("R", CultureInfo.InvariantCulture)},{dp.value.ToString("R", CultureInfo.InvariantCulture)}]");
                    }
                    else if (isHeatmap || is3D)
                    {
                        // Heatmap/Bar3D/Line3D/Pie3D: [x, y, value]
                        sb.Append($"[{dp.x.ToString("R", CultureInfo.InvariantCulture)},{dp.y.ToString("R", CultureInfo.InvariantCulture)},{dp.value.ToString("R", CultureInfo.InvariantCulture)}]");
                    }
                    else
                    {
                        sb.Append(dp.value.ToString("R", CultureInfo.InvariantCulture));
                    }
                }
                else
                {
                    // Standard/Full mode: use objects with named fields
                    sb.Append("        { ");
                    if (isCandlestickOrOHLC)
                    {
                        sb.Append($"\"open\": {dp.x.ToString("R", CultureInfo.InvariantCulture)}, \"high\": {dp.y.ToString("R", CultureInfo.InvariantCulture)}, \"low\": {dp.z.ToString("R", CultureInfo.InvariantCulture)}, \"close\": {dp.value.ToString("R", CultureInfo.InvariantCulture)}");
                    }
                    else if (isBoxPlot)
                    {
                        sb.Append($"\"min\": {dp.y.ToString("R", CultureInfo.InvariantCulture)}, \"q1\": {dp.z.ToString("R", CultureInfo.InvariantCulture)}, \"median\": {dp.value.ToString("R", CultureInfo.InvariantCulture)}, \"q3\": {dp.w.ToString("R", CultureInfo.InvariantCulture)}, \"max\": {dp.v.ToString("R", CultureInfo.InvariantCulture)}");
                    }
                    else if (isScatter3D)
                    {
                        // Scatter3D: x, y, z, value
                        sb.Append($"\"x\": {dp.x.ToString("R", CultureInfo.InvariantCulture)}, \"y\": {dp.y.ToString("R", CultureInfo.InvariantCulture)}, \"z\": {dp.z.ToString("R", CultureInfo.InvariantCulture)}, \"value\": {dp.value.ToString("R", CultureInfo.InvariantCulture)}");
                    }
                    else if (isScatter)
                    {
                        // Scatter: x, y, value - consistent with Scatter3D (x, y, z, value)
                        sb.Append($"\"x\": {dp.x.ToString("R", CultureInfo.InvariantCulture)}, \"y\": {dp.y.ToString("R", CultureInfo.InvariantCulture)}, \"value\": {dp.value.ToString("R", CultureInfo.InvariantCulture)}");
                    }
                    else if (isHeatmap || is3D)
                    {
                        // Heatmap/Bar3D/Line3D/Pie3D: x, y, value
                        sb.Append($"\"x\": {dp.x.ToString("R", CultureInfo.InvariantCulture)}, \"y\": {dp.y.ToString("R", CultureInfo.InvariantCulture)}, \"value\": {dp.value.ToString("R", CultureInfo.InvariantCulture)}");
                    }
                    else if (isHorizontalBar)
                    {
                        // HorizontalBar uses y as category index, value as the bar length
                        sb.Append($"\"y\": {pi}, \"value\": {dp.value.ToString("R", CultureInfo.InvariantCulture)}");
                    }
                    else
                    {
                        sb.Append($"\"x\": {pi}, \"value\": {dp.value.ToString("R", CultureInfo.InvariantCulture)}");
                    }
                    sb.Append(" }");
                }
            }

            if (isCompact)
                sb.Append("]");
            else
                sb.Append("\n      ]");
        }

        private static void AppendAxisLabels(StringBuilder sb, ChartProfile profile, AxisId axisId)
        {
            sb.Append($"    {{ \"axisId\": \"{axisId}\", \"labels\": [");
            AxisConfig axis = null;
            if (profile.axes != null)
            {
                for (int i = 0; i < profile.axes.Count; i++)
                {
                    var a = profile.axes[i];
                    if (a != null && a.id == axisId) { axis = a; break; }
                }
            }
            if (axis != null && axis.labels != null && axis.labels.Count > 0)
            {
                for (int i = 0; i < axis.labels.Count; i++)
                {
                    if (i > 0) sb.Append(", ");
                    sb.Append($"\"{JsonEscape(axis.labels[i])}\"");
                }
            }
            sb.Append("] }");
        }

        #endregion

        #region JSON Generation (Legacy API)

        /// <summary>[Obsolete] Use BuildJson(profile, mode) instead.</summary>
        public static string BuildInjectionJson(ChartProfile profile, string chartId, ChartJsonExampleMode mode, ChartJsonDatasMode datasMode)
        {
            if (profile == null) return string.Empty;

            switch (mode)
            {
                case ChartJsonExampleMode.Lite_Index:
                    return BuildLiteIndexInjectionJson(profile, datasMode);
                case ChartJsonExampleMode.Lite_Name:
                    return BuildLiteNameInjectionJson(profile, datasMode);
                case ChartJsonExampleMode.Lite_ID:
                    return BuildLiteIdInjectionJson(profile, chartId, datasMode);
                case ChartJsonExampleMode.Standard:
                    return BuildStandardInjectionJson(profile, datasMode);
                case ChartJsonExampleMode.Standard_Axis:
                    return BuildStandardAxisInjectionJson(profile, datasMode);
                case ChartJsonExampleMode.Full:
                    return BuildFullInjectionJson(profile, chartId, datasMode);
                default:
                    return string.Empty;
            }
        }

        public static string WrapAsApiResponse(string json)
        {
            return $"{{\n  \"code\": 200,\n  \"message\": \"success\",\n  \"data\": {json}\n}}";
        }

        public static bool TryExtractWrappedDataJson(string json, out string dataJson)
        {
            dataJson = null;
            if (string.IsNullOrEmpty(json)) return false;

            int dataIndex = json.IndexOf("\"data\"");
            if (dataIndex < 0) return false;

            int colonIndex = json.IndexOf(':', dataIndex);
            if (colonIndex < 0) return false;

            int braceStart = json.IndexOf('{', colonIndex);
            if (braceStart < 0) return false;

            int braceCount = 1;
            int braceEnd = braceStart + 1;
            while (braceEnd < json.Length && braceCount > 0)
            {
                if (json[braceEnd] == '{') braceCount++;
                else if (json[braceEnd] == '}') braceCount--;
                braceEnd++;
            }

            if (braceCount == 0)
            {
                dataJson = json.Substring(braceStart, braceEnd - braceStart);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Lite_Index: chartName + series with datas only (no name/id).
        /// Matches BuildLiteIndexInjectionJson in LibraryWindow.
        /// </summary>
        private static string BuildLiteIndexInjectionJson(ChartProfile profile, ChartJsonDatasMode datasMode)
        {
            if (profile == null) return string.Empty;

            string resolvedChartName = string.IsNullOrWhiteSpace(profile.chartName) ? profile.name : profile.chartName;

            var sb = new StringBuilder(2048);
            sb.Append("{\n");
            sb.Append("  \"chartName\": \"").Append(JsonEscape(resolvedChartName)).Append("\"");

            bool hasSeries = profile.series != null && profile.series.Count > 0;
            if (hasSeries)
            {
                sb.Append(",\n  \"series\": [\n");
                for (int i = 0; i < profile.series.Count; i++)
                {
                    var s = profile.series[i];
                    if (i > 0) sb.Append(",\n");

                    sb.Append("    {\n");

                    int dataCount = s != null && s.seriesData != null ? s.seriesData.Count : 0;
                    if (dataCount > 0)
                    {
                        bool isHeatmap = s != null && s.type == SerieType.Heatmap;
                        AppendDatasArray(sb, s, isHeatmap, datasMode, useIndexAsX: true);
                    }

                    sb.Append("    }");
                }
                sb.Append("\n  ]");
            }

            sb.Append("\n}");
            return sb.ToString();
        }

        /// <summary>
        /// Lite_Name: chartName + series with name and datas.
        /// Matches BuildLiteNameInjectionJson in LibraryWindow (same as Standard).
        /// </summary>
        private static string BuildLiteNameInjectionJson(ChartProfile profile, ChartJsonDatasMode datasMode)
        {
            return BuildStandardInjectionJson(profile, datasMode);
        }

        /// <summary>
        /// Lite_ID: chartId + series with serieId and datas.
        /// Matches BuildLiteIdInjectionJson in LibraryWindow.
        /// </summary>
        private static string BuildLiteIdInjectionJson(ChartProfile profile, string chartId, ChartJsonDatasMode datasMode)
        {
            if (profile == null) return string.Empty;

            string resolvedChartId = string.IsNullOrWhiteSpace(chartId) ? profile.chartId : chartId;

            var sb = new StringBuilder(2048);
            sb.Append("{\n");
            sb.Append("  \"chartId\": \"").Append(JsonEscape(resolvedChartId)).Append("\"");

            bool hasSeries = profile.series != null && profile.series.Count > 0;
            if (hasSeries)
            {
                sb.Append(",\n  \"series\": [\n");
                for (int i = 0; i < profile.series.Count; i++)
                {
                    var s = profile.series[i];
                    if (i > 0) sb.Append(",\n");

                    sb.Append("    {\n");
                    sb.Append("      \"serieId\": \"").Append(JsonEscape(s != null ? s.id : string.Empty)).Append("\"");

                    int dataCount = s != null && s.seriesData != null ? s.seriesData.Count : 0;
                    if (dataCount > 0)
                    {
                        bool isHeatmap = s != null && s.type == SerieType.Heatmap;
                        AppendDatasArray(sb, s, isHeatmap, datasMode, useIndexAsX: !isHeatmap, prependComma: true);
                    }

                    sb.Append("\n    }");
                }
                sb.Append("\n  ]");
            }

            sb.Append("\n}");
            return sb.ToString();
        }

        /// <summary>
        /// Standard: chartName + series with name and datas.
        /// Matches BuildStandardInjectionJson in LibraryWindow.
        /// </summary>
        private static string BuildStandardInjectionJson(ChartProfile profile, ChartJsonDatasMode datasMode)
        {
            if (profile == null) return string.Empty;

            string resolvedChartName = string.IsNullOrWhiteSpace(profile.chartName) ? profile.name : profile.chartName;

            var sb = new StringBuilder(4096);
            sb.Append("{\n");
            sb.Append("  \"chartName\": \"").Append(JsonEscape(resolvedChartName)).Append("\"");

            if (profile.series != null && profile.series.Count > 0)
            {
                sb.Append(",\n  \"series\": [\n");
                for (int i = 0; i < profile.series.Count; i++)
                {
                    var s = profile.series[i];
                    if (i > 0) sb.Append(",\n");

                    sb.Append("    {\n");
                    sb.Append("      \"name\": \"").Append(JsonEscape(s != null ? s.name : string.Empty)).Append("\"");

                    int dataCount = s != null && s.seriesData != null ? s.seriesData.Count : 0;
                    if (dataCount > 0)
                    {
                        bool isHeatmap = s.type == SerieType.Heatmap;
                        AppendDatasArray(sb, s, isHeatmap, datasMode, useIndexAsX: false, prependComma: true);
                    }

                    sb.Append("\n    }");
                }
                sb.Append("\n  ]");
            }

            sb.Append("\n}");
            return sb.ToString();
        }

        /// <summary>
        /// Standard_Axis: chartName + axes + series with name and datas.
        /// Matches BuildStandardAxisInjectionJson in LibraryWindow.
        /// </summary>
        private static string BuildStandardAxisInjectionJson(ChartProfile profile, ChartJsonDatasMode datasMode)
        {
            if (profile == null) return string.Empty;

            string resolvedChartName = string.IsNullOrWhiteSpace(profile.chartName) ? profile.name : profile.chartName;

            var sb = new StringBuilder(4096);
            sb.Append("{\n");
            sb.Append("  \"chartName\": \"").Append(JsonEscape(resolvedChartName)).Append("\"");

            // Axes (Category only)
            if (profile.axes != null)
            {
                var axisList = new List<AxisConfig>();
                for (int i = 0; i < profile.axes.Count; i++)
                {
                    var a = profile.axes[i];
                    if (a == null) continue;
                    if (a.axisType != AxisType.Category) continue;
                    if (a.labels == null || a.labels.Count == 0) continue;
                    axisList.Add(a);
                }

                if (axisList.Count > 0)
                {
                    sb.Append(",\n  \"axes\": [\n");
                    for (int i = 0; i < axisList.Count; i++)
                    {
                        var a = axisList[i];
                        if (i > 0) sb.Append(",\n");

                        sb.Append("    {\n");
                        sb.Append("      \"axisId\": \"").Append(JsonEscape(a.id.ToString())).Append("\",\n");
                        sb.Append("      \"labels\": [");
                        for (int li = 0; li < a.labels.Count; li++)
                        {
                            if (li > 0) sb.Append(", ");
                            sb.Append("\"").Append(JsonEscape(a.labels[li])).Append("\"");
                        }
                        sb.Append("]\n");
                        sb.Append("    }");
                    }
                    sb.Append("\n  ]");
                }
            }

            // Series
            if (profile.series != null && profile.series.Count > 0)
            {
                sb.Append(",\n  \"series\": [\n");
                for (int i = 0; i < profile.series.Count; i++)
                {
                    var s = profile.series[i];
                    if (i > 0) sb.Append(",\n");

                    sb.Append("    {\n");
                    sb.Append("      \"name\": \"").Append(JsonEscape(s != null ? s.name : string.Empty)).Append("\"");

                    int dataCount = s != null && s.seriesData != null ? s.seriesData.Count : 0;
                    if (dataCount > 0)
                    {
                        bool isHeatmap = s.type == SerieType.Heatmap;
                        AppendDatasArray(sb, s, isHeatmap, datasMode, useIndexAsX: false, prependComma: true);
                    }

                    sb.Append("\n    }");
                }
                sb.Append("\n  ]");
            }

            sb.Append("\n}");
            return sb.ToString();
        }

        /// <summary>
        /// Full: chartId, chartName, axes, series with all fields.
        /// Matches BuildFullInjectionJson in LibraryWindow.
        /// </summary>
        private static string BuildFullInjectionJson(ChartProfile profile, string chartId, ChartJsonDatasMode datasMode)
        {
            if (profile == null) return string.Empty;

            string resolvedChartId = string.IsNullOrWhiteSpace(chartId) ? profile.chartId : chartId;
            string resolvedChartName = string.IsNullOrWhiteSpace(profile.chartName) ? profile.name : profile.chartName;

            var sb = new StringBuilder(8192);
            sb.Append("{\n");
            sb.Append("  \"chartId\": \"").Append(JsonEscape(resolvedChartId)).Append("\",");
            sb.Append("\n  \"chartName\": \"").Append(JsonEscape(resolvedChartName)).Append("\"");

            // Axes (Category only)
            if (profile.axes != null)
            {
                var axisList = new List<AxisConfig>();
                for (int i = 0; i < profile.axes.Count; i++)
                {
                    var a = profile.axes[i];
                    if (a == null) continue;
                    if (a.axisType != AxisType.Category) continue;
                    if (a.labels == null || a.labels.Count == 0) continue;
                    axisList.Add(a);
                }

                if (axisList.Count > 0)
                {
                    sb.Append(",\n  \"axes\": [\n");
                    for (int i = 0; i < axisList.Count; i++)
                    {
                        var a = axisList[i];
                        if (i > 0) sb.Append(",\n");

                        sb.Append("    {\n");
                        sb.Append("      \"axisId\": \"").Append(JsonEscape(a.id.ToString())).Append("\",\n");
                        sb.Append("      \"labels\": [");
                        for (int li = 0; li < a.labels.Count; li++)
                        {
                            if (li > 0) sb.Append(", ");
                            sb.Append("\"").Append(JsonEscape(a.labels[li])).Append("\"");
                        }
                        sb.Append("]\n");
                        sb.Append("    }");
                    }
                    sb.Append("\n  ]");
                }
            }

            if (profile.series != null && profile.series.Count > 0)
            {
                sb.Append(",\n  \"series\": [\n");
                for (int i = 0; i < profile.series.Count; i++)
                {
                    var s = profile.series[i];
                    if (s == null) continue;
                    if (i > 0) sb.Append(",\n");

                    sb.Append("    {\n");
                    sb.Append("      \"serieId\": \"").Append(JsonEscape(s.id)).Append("\",\n");
                    sb.Append("      \"name\": \"").Append(JsonEscape(s.name)).Append("\",\n");
                    sb.Append("      \"type\": \"").Append(JsonEscape(s.type.ToString())).Append("\"");

                    int dataCount = s.seriesData != null ? s.seriesData.Count : 0;
                    if (dataCount > 0)
                    {
                        bool isHeatmap = s.type == SerieType.Heatmap;
                        AppendDatasArray(sb, s, isHeatmap, datasMode, useIndexAsX: false, prependComma: true);
                    }

                    sb.Append("\n    }");
                }
                sb.Append("\n  ]");
            }

            sb.Append("\n}");
            return sb.ToString();
        }

        /// <summary>
        /// Appends the datas array to the StringBuilder.
        /// Matches the format in LibraryWindow exactly.
        /// Supports Pro chart types: Candlestick/OHLC, BoxPlot, Waterfall, 3D charts.
        /// </summary>
        private static void AppendDatasArray(StringBuilder sb, Serie s, bool isHeatmap, ChartJsonDatasMode datasMode, bool useIndexAsX, bool prependComma = false)
        {
            if (s == null || s.seriesData == null || s.seriesData.Count == 0) return;

            int dataCount = s.seriesData.Count;
            var serieType = s.type;

            // Determine special data format based on serie type
            bool isCandlestickOrOHLC = serieType == SerieType.Candlestick || serieType == SerieType.OHLC;
            bool isBoxPlot = serieType == SerieType.BoxPlot;
            bool isWaterfall = serieType == SerieType.Waterfall;
            bool is3D = serieType == SerieType.Bar3D || serieType == SerieType.Line3D || 
                        serieType == SerieType.Scatter3D || serieType == SerieType.Pie3D;

            if (prependComma)
            {
                if (datasMode == ChartJsonDatasMode.Values)
                {
                    sb.Append(",\n      \"datas\": [");
                }
                else
                {
                    sb.Append(",\n      \"datas\": [\n");
                }
            }
            else
            {
                if (datasMode == ChartJsonDatasMode.Values)
                {
                    sb.Append("      \"datas\": [");
                }
                else
                {
                    sb.Append("      \"datas\": [\n");
                }
            }

            for (int pi = 0; pi < dataCount; pi++)
            {
                var dp = s.seriesData[pi];
                if (pi > 0)
                {
                    if (datasMode == ChartJsonDatasMode.Values) sb.Append(", ");
                    else sb.Append(",\n");
                }

                if (dp == null)
                {
                    if (datasMode == ChartJsonDatasMode.Values) sb.Append("0");
                    else sb.Append("        { \"value\": 0 }");
                    continue;
                }

                float x = useIndexAsX ? pi : dp.x;
                float y = dp.y;
                float z = dp.z;
                float w = dp.w;
                float v = dp.v;
                float value = dp.value;

                if (datasMode == ChartJsonDatasMode.Values)
                {
                    if (isCandlestickOrOHLC)
                    {
                        // OHLC: [open, high, low, close]
                        sb.Append("[").Append(x.ToString("R", CultureInfo.InvariantCulture));
                        sb.Append(",").Append(y.ToString("R", CultureInfo.InvariantCulture));
                        sb.Append(",").Append(z.ToString("R", CultureInfo.InvariantCulture));
                        sb.Append(",").Append(value.ToString("R", CultureInfo.InvariantCulture));
                        sb.Append("]");
                    }
                    else if (isBoxPlot)
                    {
                        // BoxPlot: [min, q1, median, q3, max]
                        sb.Append("[").Append(y.ToString("R", CultureInfo.InvariantCulture));
                        sb.Append(",").Append(z.ToString("R", CultureInfo.InvariantCulture));
                        sb.Append(",").Append(value.ToString("R", CultureInfo.InvariantCulture));
                        sb.Append(",").Append(w.ToString("R", CultureInfo.InvariantCulture));
                        sb.Append(",").Append(v.ToString("R", CultureInfo.InvariantCulture));
                        sb.Append("]");
                    }
                    else if (isHeatmap || is3D)
                    {
                        // Heatmap/3D: [x, y, value]
                        sb.Append("[").Append(x.ToString("R", CultureInfo.InvariantCulture));
                        sb.Append(",").Append(y.ToString("R", CultureInfo.InvariantCulture));
                        sb.Append(",").Append(value.ToString("R", CultureInfo.InvariantCulture));
                        sb.Append("]");
                    }
                    else
                    {
                        // Standard: just value
                        sb.Append(value.ToString("R", CultureInfo.InvariantCulture));
                    }
                }
                else
                {
                    // Standard or Full mode - output as object
                    sb.Append("        { ");
                    
                    if (isCandlestickOrOHLC)
                    {
                        // OHLC format: open, high, low, close
                        sb.Append("\"open\": ").Append(x.ToString("R", CultureInfo.InvariantCulture));
                        sb.Append(", \"high\": ").Append(y.ToString("R", CultureInfo.InvariantCulture));
                        sb.Append(", \"low\": ").Append(z.ToString("R", CultureInfo.InvariantCulture));
                        sb.Append(", \"close\": ").Append(value.ToString("R", CultureInfo.InvariantCulture));
                    }
                    else if (isBoxPlot)
                    {
                        // BoxPlot format: min, q1, median, q3, max
                        sb.Append("\"min\": ").Append(y.ToString("R", CultureInfo.InvariantCulture));
                        sb.Append(", \"q1\": ").Append(z.ToString("R", CultureInfo.InvariantCulture));
                        sb.Append(", \"median\": ").Append(value.ToString("R", CultureInfo.InvariantCulture));
                        sb.Append(", \"q3\": ").Append(w.ToString("R", CultureInfo.InvariantCulture));
                        sb.Append(", \"max\": ").Append(v.ToString("R", CultureInfo.InvariantCulture));
                    }
                    else if (isWaterfall)
                    {
                        // Waterfall format: value, isTotal
                        sb.Append("\"value\": ").Append(value.ToString("R", CultureInfo.InvariantCulture));
                        if (z > 0.5f) sb.Append(", \"isTotal\": true");
                    }
                    else if (isHeatmap || is3D)
                    {
                        // Heatmap/3D format: x, y, value
                        sb.Append("\"x\": ").Append(x.ToString("R", CultureInfo.InvariantCulture));
                        sb.Append(", \"y\": ").Append(y.ToString("R", CultureInfo.InvariantCulture));
                        sb.Append(", \"value\": ").Append(value.ToString("R", CultureInfo.InvariantCulture));
                    }
                    else
                    {
                        // Standard format: x, value
                        sb.Append("\"x\": ").Append(x.ToString("R", CultureInfo.InvariantCulture));
                        sb.Append(", \"value\": ").Append(value.ToString("R", CultureInfo.InvariantCulture));
                    }
                    
                    sb.Append(" }");
                }
            }

            if (datasMode == ChartJsonDatasMode.Values) sb.Append("]");
            else sb.Append("\n      ]");
        }

        private static string JsonEscape(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            return s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r").Replace("\t", "\\t");
        }

        #endregion

        #region JSON Validation

        /// <summary>
        /// Result of JSON validation and parsing.
        /// </summary>
        public class JsonValidationResult
        {
            public bool IsValid { get; set; }
            public ChartFeed Feed { get; set; }
            public List<string> Errors { get; private set; } = new List<string>();
            public List<string> Warnings { get; private set; } = new List<string>();
            
            public void AddError(string message) => Errors.Add(message);
            public void AddWarning(string message) => Warnings.Add(message);
            
            public string GetErrorSummary()
            {
                if (Errors.Count == 0) return string.Empty;
                var sb = new StringBuilder();
                sb.AppendLine($"JSON Validation Failed ({Errors.Count} error(s)):");
                for (int i = 0; i < Errors.Count; i++)
                {
                    sb.AppendLine($"  {i + 1}. {Errors[i]}");
                }
                return sb.ToString();
            }
            
            public string GetWarningSummary()
            {
                if (Warnings.Count == 0) return string.Empty;
                var sb = new StringBuilder();
                sb.AppendLine($"JSON Validation Warnings ({Warnings.Count}):");
                for (int i = 0; i < Warnings.Count; i++)
                {
                    sb.AppendLine($"  - {Warnings[i]}");
                }
                return sb.ToString();
            }
        }

        /// <summary>
        /// Validate and parse JSON with detailed error information.
        /// </summary>
        public static JsonValidationResult ValidateAndParseFeed(string json)
        {
            var result = new JsonValidationResult();
            
            // Basic validation
            if (string.IsNullOrWhiteSpace(json))
            {
                result.AddError("JSON content is empty or whitespace only.");
                return result;
            }
            
            json = json.Trim();
            
            // Check for valid JSON structure
            if (!json.StartsWith("{"))
            {
                result.AddError("JSON must start with '{'. Expected an object.");
                return result;
            }
            
            if (!json.EndsWith("}"))
            {
                result.AddError("JSON must end with '}'. Incomplete or malformed JSON.");
                return result;
            }
            
            // Try to extract from API envelope
            if (TryExtractWrappedDataJson(json, out var dataJson) && !string.IsNullOrEmpty(dataJson))
            {
                result.AddWarning("JSON was wrapped in API envelope. Extracted 'data' field.");
                json = dataJson;
            }
            
            // Validate required structure
            ValidateJsonStructure(json, result);
            
            if (result.Errors.Count > 0)
            {
                return result;
            }
            
            // Try to parse
            if (TryDeserializeFeed(json, out var feed))
            {
                result.IsValid = true;
                result.Feed = feed;
                
                // Post-parse validation
                ValidateFeedContent(feed, result);
            }
            else
            {
                result.AddError("Failed to parse JSON. Check syntax and structure.");
                TryIdentifyParseError(json, result);
            }
            
            return result;
        }

        /// <summary>
        /// Validate JSON structure before parsing.
        /// </summary>
        private static void ValidateJsonStructure(string json, JsonValidationResult result)
        {
            // Check for series array
            bool hasSeries = json.Contains("\"series\"");
            bool hasChartName = json.Contains("\"chartName\"");
            bool hasChartId = json.Contains("\"chartId\"");
            bool hasDatas = json.Contains("\"datas\"");
            
            if (!hasSeries)
            {
                result.AddError("Missing required 'series' array. JSON must contain a 'series' field.");
                return;
            }
            
            // Check series array format
            int seriesIdx = json.IndexOf("\"series\"", StringComparison.Ordinal);
            if (seriesIdx >= 0)
            {
                int colonIdx = json.IndexOf(':', seriesIdx);
                if (colonIdx >= 0)
                {
                    int bracketIdx = -1;
                    for (int i = colonIdx + 1; i < json.Length; i++)
                    {
                        char c = json[i];
                        if (char.IsWhiteSpace(c)) continue;
                        if (c == '[') { bracketIdx = i; break; }
                        if (c != '[')
                        {
                            result.AddError($"'series' must be an array. Found '{c}' instead of '['.");
                            return;
                        }
                    }
                    
                    if (bracketIdx < 0)
                    {
                        result.AddError("'series' array is malformed. Expected '[' after 'series:'.");
                    }
                }
            }
            
            // Check bracket balance
            int braceCount = 0;
            int bracketCount = 0;
            bool inString = false;
            char prevChar = '\0';
            
            for (int i = 0; i < json.Length; i++)
            {
                char c = json[i];
                
                if (c == '"' && prevChar != '\\')
                {
                    inString = !inString;
                }
                else if (!inString)
                {
                    if (c == '{') braceCount++;
                    else if (c == '}') braceCount--;
                    else if (c == '[') bracketCount++;
                    else if (c == ']') bracketCount--;
                }
                
                prevChar = c;
            }
            
            if (braceCount != 0)
            {
                string braceMsg = braceCount > 0 
                    ? $"Missing {braceCount} closing '}}'"
                    : $"Extra {-braceCount} closing '}}'";
                result.AddError($"Unbalanced braces '{{}}'. {braceMsg}.");
            }
            
            if (bracketCount != 0)
            {
                string bracketMsg = bracketCount > 0 
                    ? $"Missing {bracketCount} closing ']'"
                    : $"Extra {-bracketCount} closing ']'";
                result.AddError($"Unbalanced brackets '[]'. {bracketMsg}.");
            }
            
            // Warnings for optional fields
            if (!hasChartName)
            {
                result.AddWarning("No 'chartName' field. Chart name will not be updated.");
            }
        }

        /// <summary>
        /// Validate parsed feed content.
        /// </summary>
        private static void ValidateFeedContent(ChartFeed feed, JsonValidationResult result)
        {
            if (feed.series == null || feed.series.Length == 0)
            {
                result.AddWarning("'series' array is empty. No data will be applied.");
                return;
            }
            
            for (int i = 0; i < feed.series.Length; i++)
            {
                var s = feed.series[i];
                if (s == null)
                {
                    result.AddWarning($"Series[{i}] is null.");
                    continue;
                }
                
                if (s.datas == null || s.datas.Length == 0)
                {
                    result.AddWarning($"Series[{i}] '{s.name ?? "(unnamed)"}' has no data points.");
                }
            }
        }

        /// <summary>
        /// Try to identify specific parse errors.
        /// </summary>
        private static void TryIdentifyParseError(string json, JsonValidationResult result)
        {
            // Check for common JSON syntax errors
            
            // Trailing comma before closing bracket/brace
            if (Regex.IsMatch(json, @",\s*[\]\}]"))
            {
                result.AddError("Trailing comma detected before ']' or '}'. Remove the extra comma.");
            }
            
            // Missing comma between elements
            if (Regex.IsMatch(json, @"\}\s*\{") || Regex.IsMatch(json, @"\]\s*\["))
            {
                result.AddError("Missing comma between elements. Add ',' between objects or arrays.");
            }
            
            // Single quotes instead of double quotes
            if (json.Contains("'"))
            {
                result.AddWarning("Single quotes detected. JSON requires double quotes for strings.");
            }
            
            // Unquoted keys
            if (Regex.IsMatch(json, @"[{,]\s*[a-zA-Z_][a-zA-Z0-9_]*\s*:"))
            {
                result.AddError("Unquoted key detected. All keys must be enclosed in double quotes.");
            }
        }

        #endregion

        #region JSON Parsing

        public static bool TryDeserializeFeed(string json, out ChartFeed feed)
        {
            feed = null;

            if (TryExtractWrappedDataJson(json, out var dataJson) && !string.IsNullOrEmpty(dataJson))
            {
                json = dataJson;
            }

            // Check if we need flexible parsing (simple value arrays)
            if (ShouldPreferFlexibleFeedParser(json))
            {
                if (TryDeserializeFeedFlexibleNewtonsoft(json, out feed))
                {
                    return true;
                }
                return false;
            }

            // Try Newtonsoft.Json first
            try
            {
                var jsonConvertType = Type.GetType("Newtonsoft.Json.JsonConvert, Newtonsoft.Json")
                                     ?? Type.GetType("Newtonsoft.Json.JsonConvert, Unity.Newtonsoft.Json");
                if (jsonConvertType != null)
                {
                    var deserialize = jsonConvertType.GetMethod("DeserializeObject", new[] { typeof(string), typeof(Type) });
                    if (deserialize != null)
                    {
                        var obj = deserialize.Invoke(null, new object[] { json, typeof(ChartFeed) });
                        feed = obj as ChartFeed;
                        if (feed != null) return true;
                    }
                }
            }
            catch
            {
            }

            // Try Unity JsonUtility
            try
            {
                json = NormalizeJsonForUnityJsonUtility(json);
                feed = JsonUtility.FromJson<ChartFeed>(json);
                if (feed != null) return true;
            }
            catch
            {
            }

            // Last resort: flexible parser
            return TryDeserializeFeedFlexibleNewtonsoft(json, out feed);
        }

        private static bool ShouldPreferFlexibleFeedParser(string json)
        {
            if (string.IsNullOrEmpty(json)) return false;

            int idx = 0;
            while (idx < json.Length)
            {
                idx = json.IndexOf("\"datas\"", idx, StringComparison.Ordinal);
                if (idx < 0) return false;

                int colon = json.IndexOf(':', idx + 6);
                if (colon < 0) return false;

                int openBracket = -1;
                for (int i = colon + 1; i < json.Length; i++)
                {
                    char c = json[i];
                    if (char.IsWhiteSpace(c)) continue;
                    if (c != '[') break;
                    openBracket = i;
                    break;
                }

                if (openBracket < 0)
                {
                    idx = colon + 1;
                    continue;
                }

                int j = openBracket + 1;
                while (j < json.Length && char.IsWhiteSpace(json[j])) j++;
                if (j >= json.Length) return false;

                char first = json[j];
                if (first == ']')
                {
                    // Empty array, continue searching
                    idx = j + 1;
                    continue;
                }

                // For both object format ({) and array/value format, use flexible parser
                // This ensures Pro chart types (BoxPlot, OHLC, etc.) are correctly parsed
                return true;
            }

            return false;
        }

        private static bool TryDeserializeFeedFlexibleNewtonsoft(string json, out ChartFeed feed)
        {
            feed = null;

            try
            {
                var jObjectType = Type.GetType("Newtonsoft.Json.Linq.JObject, Newtonsoft.Json")
                               ?? Type.GetType("Newtonsoft.Json.Linq.JObject, Unity.Newtonsoft.Json");
                var jArrayType = Type.GetType("Newtonsoft.Json.Linq.JArray, Newtonsoft.Json")
                              ?? Type.GetType("Newtonsoft.Json.Linq.JArray, Unity.Newtonsoft.Json");
                if (jObjectType == null || jArrayType == null) return false;

                var parse = jObjectType.GetMethod("Parse", new[] { typeof(string) });
                if (parse == null) return false;

                var root = parse.Invoke(null, new object[] { json });
                if (root == null) return false;

                feed = new ChartFeed();

                var itemProp = jObjectType.GetProperty("Item", new[] { typeof(string) });
                if (itemProp == null) return false;

                string ReadString(object obj, string key)
                {
                    if (obj == null) return null;
                    var token = itemProp.GetValue(obj, new object[] { key });
                    if (token == null) return null;
                    var s = token.ToString();
                    if (string.IsNullOrEmpty(s)) return null;
                    if (s.Length >= 2 && s[0] == '"' && s[s.Length - 1] == '"') s = s.Substring(1, s.Length - 2);
                    return s;
                }

                float ReadFloat(object obj, string key, float defaultValue = 0f)
                {
                    var s = ReadString(obj, key);
                    if (string.IsNullOrEmpty(s)) return defaultValue;
                    if (float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var v)) return v;
                    return defaultValue;
                }

                bool ReadBool(object obj, string key, bool defaultValue = false)
                {
                    var s = ReadString(obj, key);
                    if (string.IsNullOrEmpty(s)) return defaultValue;
                    if (bool.TryParse(s, out var v)) return v;
                    return defaultValue;
                }

                object GetToken(object obj, string key)
                {
                    if (obj == null) return null;
                    return itemProp.GetValue(obj, new object[] { key });
                }

                bool HasKey(object obj, string key)
                {
                    if (obj == null) return false;
                    var token = itemProp.GetValue(obj, new object[] { key });
                    bool hasIt = token != null;
                    return hasIt;
                }

                feed.chartId = ReadString(root, "chartId");
                feed.chartName = ReadString(root, "chartName");

                // Axes
                var axesToken = GetToken(root, "axes");
                if (axesToken != null && jArrayType.IsInstanceOfType(axesToken))
                {
                    var axes = new List<AxisFeed>();
                    foreach (var axisObj in (System.Collections.IEnumerable)axesToken)
                    {
                        if (axisObj == null) continue;

                        var axisIdStr = ReadString(axisObj, "axisId");
                        if (!Enum.TryParse(axisIdStr, ignoreCase: true, out AxisId axisId)) continue;

                        var labelsToken = GetToken(axisObj, "labels");
                        string[] labels = null;
                        if (labelsToken != null && jArrayType.IsInstanceOfType(labelsToken))
                        {
                            var list = new List<string>();
                            foreach (var l in (System.Collections.IEnumerable)labelsToken)
                            {
                                if (l == null) continue;
                                var s = l.ToString();
                                if (s.Length >= 2 && s[0] == '"' && s[s.Length - 1] == '"') s = s.Substring(1, s.Length - 2);
                                list.Add(s);
                            }
                            labels = list.ToArray();
                        }

                        axes.Add(new AxisFeed { axisId = axisId, labels = labels });
                    }
                    feed.axes = axes.Count > 0 ? axes.ToArray() : null;
                }

                // Series
                var seriesToken = GetToken(root, "series");
                if (seriesToken != null && jArrayType.IsInstanceOfType(seriesToken))
                {
                    var series = new List<SerieFeed>();
                    foreach (var serieObj in (System.Collections.IEnumerable)seriesToken)
                    {
                        if (serieObj == null) continue;

                        var sf = new SerieFeed();
                        sf.serieId = ReadString(serieObj, "serieId");
                        sf.name = ReadString(serieObj, "name");

                        var typeStr = ReadString(serieObj, "type");
                        if (!string.IsNullOrEmpty(typeStr) && int.TryParse(typeStr, out var tInt))
                        {
                            sf.type = (SerieType)tInt;
                        }
                        else if (!string.IsNullOrEmpty(typeStr) && Enum.TryParse(typeStr, ignoreCase: true, out SerieType tEnum))
                        {
                            sf.type = tEnum;
                        }

                        var datasToken = GetToken(serieObj, "datas");
                        if (datasToken != null && jArrayType.IsInstanceOfType(datasToken))
                        {
                            var datas = new List<DataFeed>();

                            object first = null;
                            foreach (var tmp in (System.Collections.IEnumerable)datasToken) { first = tmp; break; }

                            if (first != null)
                            {
                                var firstText = first.ToString().Trim();
                                bool firstIsObject = firstText.StartsWith("{");
                                bool firstIsArray = firstText.StartsWith("[");
                                

                                if (!firstIsObject && !firstIsArray)
                                {
                                    // Values mode: datas: [1,2,3]
                                    // For HorizontalBar, index goes to y (category index)
                                    // For other types, index goes to x
                                    bool isHorizontalBar = sf.type == SerieType.HorizontalBar;
                                    int dataIdx = 0;
                                    foreach (var vToken in (System.Collections.IEnumerable)datasToken)
                                    {
                                        if (vToken == null) { dataIdx++; continue; }
                                        var vs = vToken.ToString();
                                        if (!float.TryParse(vs, NumberStyles.Float, CultureInfo.InvariantCulture, out var vv)) vv = 0f;
                                        if (isHorizontalBar)
                                            datas.Add(new DataFeed { x = 0f, y = dataIdx, value = vv });
                                        else
                                            datas.Add(new DataFeed { x = dataIdx, y = 0f, value = vv });
                                        dataIdx++;
                                    }
                                }
                                else if (firstIsArray)
                                {
                                    // Tuple mode: datas: [[x,value], [x,value]] or [[x,y,value], ...]
                                    // Also supports Pro types:
                                    // - OHLC/Candlestick: [open, high, low, close]
                                    // - BoxPlot: [min, q1, median, q3, max]
                                    foreach (var tupleToken in (System.Collections.IEnumerable)datasToken)
                                    {
                                        if (tupleToken == null) continue;
                                        
                                        // Try to iterate the tuple as JArray directly
                                        var df = new DataFeed();
                                        var values = new List<float>();
                                        
                                        if (jArrayType.IsInstanceOfType(tupleToken))
                                        {
                                            foreach (var elem in (System.Collections.IEnumerable)tupleToken)
                                            {
                                                if (elem == null) continue;
                                                var elemStr = elem.ToString().Trim();
                                                if (float.TryParse(elemStr, NumberStyles.Float, CultureInfo.InvariantCulture, out var fv))
                                                    values.Add(fv);
                                            }
                                        }
                                        else
                                        {
                                            // Fallback: parse from string
                                            var tupleText = tupleToken.ToString();
                                            if (string.IsNullOrEmpty(tupleText)) continue;
                                            tupleText = tupleText.Trim();
                                            if (tupleText.Length < 2) continue;
                                            if (!tupleText.StartsWith("[") || !tupleText.EndsWith("]")) continue;
                                            tupleText = tupleText.Substring(1, tupleText.Length - 2);
                                            var parts = tupleText.Split(',');
                                            foreach (var p in parts)
                                            {
                                                if (float.TryParse(p.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var fv))
                                                    values.Add(fv);
                                            }
                                        }
                                        
                                        if (values.Count < 2) continue;
                                        
                                        if (values.Count == 2)
                                        {
                                            // [x, value]
                                            df.x = values[0];
                                            df.value = values[1];
                                        }
                                        else if (values.Count == 3)
                                        {
                                            // [x, y, value] - Heatmap/3D/Scatter
                                            df.x = values[0];
                                            df.y = values[1];
                                            df.value = values[2];
                                        }
                                        else if (values.Count == 4)
                                        {
                                            // [open, high, low, close] - OHLC/Candlestick
                                            // Maps to: x=open, y=high, z=low, value=close
                                            df.x = values[0];
                                            df.y = values[1];
                                            df.z = values[2];
                                            df.value = values[3];
                                        }
                                        else if (values.Count >= 5)
                                        {
                                            // [min, q1, median, q3, max] - BoxPlot
                                            // Maps to: y=min, z=q1, value=median, w=q3, v=max
                                            df.y = values[0];
                                            df.z = values[1];
                                            df.value = values[2];
                                            df.w = values[3];
                                            df.v = values[4];
                                        }
                                        
                                        datas.Add(df);
                                    }
                                }
                                else
                                {
                                    // Standard/Full object mode
                                    // Also supports Pro types with named fields:
                                    // - OHLC/Candlestick: {open, high, low, close}
                                    // - BoxPlot: {min, q1, median, q3, max}
                                    // - Waterfall: {value, isTotal}
                                    foreach (var dpObj in (System.Collections.IEnumerable)datasToken)
                                    {
                                        if (dpObj == null) continue;
                                        var df = new DataFeed();
                                        df.id = ReadString(dpObj, "id");
                                        df.name = ReadString(dpObj, "name");
                                        
                                        // Check for OHLC format (open/high/low/close)
                                        bool hasOpen = HasKey(dpObj, "open");
                                        bool hasMin = HasKey(dpObj, "min");
                                        
                                        
                                        if (hasOpen)
                                        {
                                            // OHLC format: x=open, y=high, z=low, value=close
                                            df.x = ReadFloat(dpObj, "open", 0f);
                                            df.y = ReadFloat(dpObj, "high", 0f);
                                            df.z = ReadFloat(dpObj, "low", 0f);
                                            df.value = ReadFloat(dpObj, "close", 0f);
                                        }
                                        // Check for BoxPlot format (min/q1/median/q3/max)
                                        else if (hasMin)
                                        {
                                            // BoxPlot format: y=min, z=q1, value=median, w=q3, v=max
                                            df.y = ReadFloat(dpObj, "min", 0f);
                                            df.z = ReadFloat(dpObj, "q1", 0f);
                                            df.value = ReadFloat(dpObj, "median", 0f);
                                            df.w = ReadFloat(dpObj, "q3", 0f);
                                            df.v = ReadFloat(dpObj, "max", 0f);
                                        }
                                        // Check for Waterfall format (value, isTotal)
                                        else if (ReadBool(dpObj, "isTotal", false))
                                        {
                                            df.value = ReadFloat(dpObj, "value", 0f);
                                            df.z = 1f; // isTotal flag
                                        }
                                        else
                                        {
                                            // Standard format
                                            df.x = ReadFloat(dpObj, "x", 0f);
                                            df.y = ReadFloat(dpObj, "y", 0f);
                                            df.z = ReadFloat(dpObj, "z", 0f);
                                            df.w = ReadFloat(dpObj, "w", 0f);
                                            df.v = ReadFloat(dpObj, "v", 0f);
                                            df.value = ReadFloat(dpObj, "value", 0f);
                                            
                                        }
                                        
                                        df.useColor = ReadBool(dpObj, "useColor", false);
                                        if (df.useColor)
                                        {
                                            var colorToken = GetToken(dpObj, "color");
                                            if (colorToken != null)
                                            {
                                                df.color = new Color(
                                                    ReadFloat(colorToken, "r", 1f),
                                                    ReadFloat(colorToken, "g", 1f),
                                                    ReadFloat(colorToken, "b", 1f),
                                                    ReadFloat(colorToken, "a", 1f));
                                            }
                                        }
                                        datas.Add(df);
                                    }
                                }
                            }

                            sf.datas = datas.Count > 0 ? datas.ToArray() : null;
                        }

                        series.Add(sf);
                    }
                    feed.series = series.Count > 0 ? series.ToArray() : null;
                }

                return feed != null;
            }
            catch
            {
                feed = null;
                return false;
            }
        }

        private static string NormalizeJsonForUnityJsonUtility(string json)
        {
            if (string.IsNullOrEmpty(json)) return json;

            // Convert: "type": "Line" -> "type": 0
            json = Regex.Replace(json, "\\\"type\\\"\\s*:\\s*\\\"([^\\\"]+)\\\"", match =>
            {
                var label = match.Groups[1].Value;
                if (Enum.TryParse(label, ignoreCase: true, out SerieType parsed))
                {
                    return "\"type\": " + ((int)parsed).ToString();
                }
                return match.Value;
            });

            // Convert: "axisId": "XBottom" -> "axisId": 0
            json = Regex.Replace(json, "\\\"axisId\\\"\\s*:\\s*\\\"([^\\\"]+)\\\"", match =>
            {
                var label = match.Groups[1].Value;
                if (Enum.TryParse(label, ignoreCase: true, out AxisId parsed))
                {
                    return "\"axisId\": " + ((int)parsed).ToString();
                }
                return match.Value;
            });

            return json;
        }

        /// <summary>
        /// Apply feed data to profile without metadata overwrite.
        /// </summary>
        public static void ApplyFeedToProfile(ChartProfile profile, ChartFeed feed)
        {
            ApplyFeedToProfile(profile, feed, allowMetaOverwrite: false);
        }

        /// <summary>
        /// Apply feed data to profile with optional metadata overwrite.
        /// </summary>
        /// <param name="profile">Target profile to apply data to.</param>
        /// <param name="feed">Source feed data.</param>
        /// <param name="allowMetaOverwrite">If true, allows overwriting chartId, chartName, serieId, serie name and type.</param>
        /// <returns>True if any changes were made.</returns>
        public static bool ApplyFeedToProfile(ChartProfile profile, ChartFeed feed, bool allowMetaOverwrite)
        {
            if (profile == null || feed == null) return false;

            bool changed = false;
            if (profile.EnsureRuntimeData()) changed = true;

            if (allowMetaOverwrite)
            {
                if (!string.IsNullOrEmpty(feed.chartId) && !string.Equals(profile.chartId, feed.chartId, StringComparison.Ordinal))
                {
                    profile.chartId = feed.chartId;
                    changed = true;
                }
                if (!string.IsNullOrEmpty(feed.chartName) && !string.Equals(profile.chartName, feed.chartName, StringComparison.Ordinal))
                {
                    profile.chartName = feed.chartName;
                    changed = true;
                }
            }

            // Apply axes
            if (feed.axes != null && feed.axes.Length > 0)
            {
                if (profile.axes == null) { profile.axes = new List<AxisConfig>(); changed = true; }

                for (int i = 0; i < feed.axes.Length; i++)
                {
                    var af = feed.axes[i];
                    if (af == null) continue;

                    AxisConfig axis = null;
                    for (int ai = 0; ai < profile.axes.Count; ai++)
                    {
                        var a = profile.axes[ai];
                        if (a != null && a.id == af.axisId) { axis = a; break; }
                    }

                    if (axis == null)
                    {
                        axis = new AxisConfig { id = af.axisId };
                        profile.axes.Add(axis);
                        changed = true;
                    }

                    if (af.labels != null && af.labels.Length > 0)
                    {
                        // Only change axis type to Category if there are actual labels
                        axis.axisType = AxisType.Category;
                        if (axis.labels == null) { axis.labels = new List<string>(); changed = true; }

                        axis.labels.Clear();
                        axis.labels.AddRange(af.labels);
                        changed = true;
                    }
                }
            }

            // Apply series data
            if (feed.series != null && feed.series.Length > 0)
            {
                if (profile.series == null) { profile.series = new List<Serie>(); changed = true; }

                // Track which series have been matched to avoid duplicate matching
                var matchedSeries = new HashSet<int>();

                for (int i = 0; i < feed.series.Length; i++)
                {
                    var sf = feed.series[i];
                    if (sf == null) continue;

                    bool indexMode = string.IsNullOrEmpty(sf.serieId) && string.IsNullOrEmpty(sf.name);

                    Serie serie = null;
                    int matchedIndex = -1;
                    
                    if (!string.IsNullOrEmpty(sf.serieId))
                    {
                        for (int si = 0; si < profile.series.Count; si++)
                        {
                            if (matchedSeries.Contains(si)) continue;
                            var s = profile.series[si];
                            if (s != null && string.Equals(s.id, sf.serieId, StringComparison.Ordinal)) { serie = s; matchedIndex = si; break; }
                        }
                    }
                    if (serie == null && !string.IsNullOrEmpty(sf.name))
                    {
                        for (int si = 0; si < profile.series.Count; si++)
                        {
                            if (matchedSeries.Contains(si)) continue;
                            var s = profile.series[si];
                            if (s != null && string.Equals(s.name, sf.name, StringComparison.Ordinal)) { serie = s; matchedIndex = si; break; }
                        }
                    }

                    if (serie == null && indexMode && i >= 0 && i < profile.series.Count && !matchedSeries.Contains(i))
                    {
                        serie = profile.series[i];
                        matchedIndex = i;
                    }
                    
                    if (matchedIndex >= 0) matchedSeries.Add(matchedIndex);

                    if (serie == null)
                    {
                        if (!allowMetaOverwrite && !indexMode) continue;

                        serie = new Serie { name = string.IsNullOrEmpty(sf.name) ? $"Series {i + 1}" : sf.name, type = sf.type, visible = true, seriesData = new List<SeriesData>() };
                        if (!string.IsNullOrEmpty(sf.serieId)) serie.id = sf.serieId;
                        serie.EnsureIntegrity();
                        profile.series.Add(serie);
                        changed = true;
                    }

                    if (allowMetaOverwrite && !indexMode)
                    {
                        if (!string.IsNullOrEmpty(sf.serieId) && string.IsNullOrEmpty(serie.id)) { serie.id = sf.serieId; changed = true; }
                        if (!string.IsNullOrEmpty(sf.name) && !string.Equals(serie.name, sf.name, StringComparison.Ordinal)) { serie.name = sf.name; changed = true; }
                        if (serie.type != sf.type) { serie.SetType(sf.type); changed = true; }
                    }

                    if (serie.seriesData == null) { serie.seriesData = new List<SeriesData>(); changed = true; }

                    if (sf.datas != null)
                    {
                        EnsureSeriesDataCount(serie.seriesData, sf.datas.Length);
                        
                        // For HorizontalBar, if data has x but no y, swap x to y (category index)
                        bool isHorizontalBar = serie.type == SerieType.HorizontalBar;
                        
                        for (int pi = 0; pi < sf.datas.Length; pi++)
                        {
                            var df = sf.datas[pi];
                            if (df == null) continue;

                            var p = serie.seriesData[pi] ?? (serie.seriesData[pi] = new SeriesData());

                            if (!string.IsNullOrEmpty(df.id)) p.id = df.id;
                            
                            // HorizontalBar uses y as category index
                            // If data has x set but y is 0, and this is HorizontalBar, use x as y
                            if (isHorizontalBar && df.x != 0f && df.y == 0f)
                            {
                                p.x = 0f;
                                p.y = df.x;  // Use x as category index
                            }
                            else
                            {
                                p.x = df.x;
                                p.y = df.y;
                            }
                            p.z = df.z;
                            p.w = df.w;  // For BoxPlot Q3
                            p.v = df.v;  // For BoxPlot max
                            p.value = df.value;

                            if (!string.IsNullOrEmpty(df.name)) p.name = df.name;
                            p.useColor = df.useColor;
                            if (df.useColor) p.color = df.color;
                        }
                        changed = true;
                    }

                    if (serie.EnsureIntegrity()) changed = true;
                }
            }

            if (profile.EnsureRuntimeData()) changed = true;
            return changed;
        }

        private static void EnsureSeriesDataCount(List<SeriesData> list, int count)
        {
            if (list == null) return;
            while (list.Count < count) list.Add(new SeriesData());
            if (list.Count > count) list.RemoveRange(count, list.Count - count);
        }

        #endregion
    }
}
