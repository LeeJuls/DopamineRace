using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace EasyChart
{
    /// <summary>
    /// JSON generation and parsing mode for chart data.
    /// Matches the modes in EasyChartLibraryWindow.JsonPanel.
    /// </summary>
    public enum ChartJsonExampleMode
    {
        /// <summary>Lite format with index-based series data (series with datas only).</summary>
        Lite_Index,
        /// <summary>Lite format with name-based series data (series with name and datas).</summary>
        Lite_Name,
        /// <summary>Lite format with ID-based series data (chartId + series with serieId and datas).</summary>
        Lite_ID,
        /// <summary>Standard format with series names (chartName + series with name and datas).</summary>
        Standard,
        /// <summary>Standard format with axis labels (chartName + axes + series).</summary>
        Standard_Axis,
        /// <summary>Full format with all metadata (chartId, chartName, axes, series with all fields).</summary>
        Full
    }

    /// <summary>
    /// Data format mode for series data in JSON.
    /// Matches the modes in EasyChartLibraryWindow.JsonPanel.
    /// </summary>
    public enum ChartJsonDatasMode
    {
        /// <summary>Values only (e.g., 1 or [x,y,v] for heatmap).</summary>
        Values,
        /// <summary>Standard objects with x and value (e.g., {"x": 0, "value": 1}).</summary>
        Standard,
        /// <summary>Full objects with all fields including id, name, etc.</summary>
        Full
    }

    /// <summary>
    /// Utility class for JSON generation and parsing for EasyChart.
    /// Shared between Editor (LibraryWindow) and Runtime (UGUIRuntimeJsonInjection).
    /// Format matches EasyChartLibraryWindow.JsonPanel exactly.
    /// </summary>
    public static class ChartJsonUtils
    {
        #region JSON Generation

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
        /// </summary>
        private static void AppendDatasArray(StringBuilder sb, Serie s, bool isHeatmap, ChartJsonDatasMode datasMode, bool useIndexAsX, bool prependComma = false)
        {
            if (s == null || s.seriesData == null || s.seriesData.Count == 0) return;

            int dataCount = s.seriesData.Count;

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

                float x = useIndexAsX ? pi : (dp != null ? dp.x : 0f);
                float y = dp != null ? dp.y : 0f;
                float v = dp != null ? dp.value : 0f;

                if (datasMode == ChartJsonDatasMode.Values && !isHeatmap)
                {
                    sb.Append(v.ToString("R", CultureInfo.InvariantCulture));
                }
                else if (datasMode == ChartJsonDatasMode.Values && isHeatmap)
                {
                    sb.Append("[").Append(x.ToString("R", CultureInfo.InvariantCulture));
                    sb.Append(",").Append(y.ToString("R", CultureInfo.InvariantCulture));
                    sb.Append(",").Append(v.ToString("R", CultureInfo.InvariantCulture));
                    sb.Append("]");
                }
                else
                {
                    // Standard or Full mode
                    sb.Append("        { \"x\": ").Append(x.ToString("R", CultureInfo.InvariantCulture));
                    if (isHeatmap) sb.Append(", \"y\": ").Append(y.ToString("R", CultureInfo.InvariantCulture));
                    sb.Append(", \"value\": ").Append(v.ToString("R", CultureInfo.InvariantCulture));
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
                if (first == '{' || first == ']')
                {
                    idx = j + 1;
                    continue;
                }

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
                                var firstText = first.ToString();
                                bool firstIsObject = firstText.StartsWith("{");
                                bool firstIsArray = firstText.StartsWith("[");

                                if (!firstIsObject && !firstIsArray)
                                {
                                    // Values mode: datas: [1,2,3]
                                    int dataIdx = 0;
                                    foreach (var vToken in (System.Collections.IEnumerable)datasToken)
                                    {
                                        if (vToken == null) { dataIdx++; continue; }
                                        var vs = vToken.ToString();
                                        if (!float.TryParse(vs, NumberStyles.Float, CultureInfo.InvariantCulture, out var vv)) vv = 0f;
                                        datas.Add(new DataFeed { x = dataIdx, y = 0f, value = vv });
                                        dataIdx++;
                                    }
                                }
                                else if (firstIsArray)
                                {
                                    // Tuple mode: datas: [[x,value], [x,value]] or [[x,y,value], ...]
                                    foreach (var tupleToken in (System.Collections.IEnumerable)datasToken)
                                    {
                                        if (tupleToken == null) continue;
                                        var tupleText = tupleToken.ToString();
                                        if (string.IsNullOrEmpty(tupleText)) continue;
                                        tupleText = tupleText.Trim();
                                        if (tupleText.Length < 2) continue;
                                        if (!tupleText.StartsWith("[") || !tupleText.EndsWith("]")) continue;
                                        tupleText = tupleText.Substring(1, tupleText.Length - 2);
                                        var parts = tupleText.Split(',');
                                        if (parts.Length < 2) continue;
                                        float tx = 0f, ty = 0f, tv = 0f;
                                        float.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out tx);
                                        if (parts.Length == 2)
                                        {
                                            float.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out tv);
                                        }
                                        else
                                        {
                                            float.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out ty);
                                            float.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out tv);
                                        }
                                        datas.Add(new DataFeed { x = tx, y = ty, value = tv });
                                    }
                                }
                                else
                                {
                                    // Standard/Full object mode
                                    foreach (var dpObj in (System.Collections.IEnumerable)datasToken)
                                    {
                                        if (dpObj == null) continue;
                                        var df = new DataFeed();
                                        df.id = ReadString(dpObj, "id");
                                        df.x = ReadFloat(dpObj, "x", 0f);
                                        df.y = ReadFloat(dpObj, "y", 0f);
                                        df.z = ReadFloat(dpObj, "z", 0f);
                                        df.value = ReadFloat(dpObj, "value", 0f);
                                        df.name = ReadString(dpObj, "name");
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

                    if (af.labels != null)
                    {
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

                for (int i = 0; i < feed.series.Length; i++)
                {
                    var sf = feed.series[i];
                    if (sf == null) continue;

                    bool indexMode = string.IsNullOrEmpty(sf.serieId) && string.IsNullOrEmpty(sf.name);

                    Serie serie = null;
                    if (!string.IsNullOrEmpty(sf.serieId))
                    {
                        for (int si = 0; si < profile.series.Count; si++)
                        {
                            var s = profile.series[si];
                            if (s != null && string.Equals(s.id, sf.serieId, StringComparison.Ordinal)) { serie = s; break; }
                        }
                    }
                    if (serie == null && !string.IsNullOrEmpty(sf.name))
                    {
                        for (int si = 0; si < profile.series.Count; si++)
                        {
                            var s = profile.series[si];
                            if (s != null && string.Equals(s.name, sf.name, StringComparison.Ordinal)) { serie = s; break; }
                        }
                    }

                    if (serie == null && indexMode && i >= 0 && i < profile.series.Count)
                    {
                        serie = profile.series[i];
                    }

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
                        for (int pi = 0; pi < sf.datas.Length; pi++)
                        {
                            var df = sf.datas[pi];
                            if (df == null) continue;

                            var p = serie.seriesData[pi] ?? (serie.seriesData[pi] = new SeriesData());

                            if (!string.IsNullOrEmpty(df.id)) p.id = df.id;
                            p.x = df.x;
                            p.y = df.y;
                            p.z = df.z;
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
