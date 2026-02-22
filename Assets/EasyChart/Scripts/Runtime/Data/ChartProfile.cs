using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace EasyChart
{
    [CreateAssetMenu(fileName = "NewChartProfile", menuName = "EasyChart/Chart Profile")]
    public class ChartProfile : ScriptableObject
    {
        // Cache to avoid redundant EnsureRuntimeData() calls in the same frame
        [NonSerialized] private double _lastEnsureTime;
        [NonSerialized] private int _lastEnsureVersion;
        private static object DeepCloneObject(object source, Dictionary<object, object> visited)
        {
            if (source == null) return null;

            var type = source.GetType();

            // Keep UnityEngine.Object references as-is (Texture2D, etc.).
            if (typeof(UnityEngine.Object).IsAssignableFrom(type)) return source;

            // Immutable / value types.
            if (type.IsPrimitive || type.IsEnum || type == typeof(string) || type == typeof(decimal)) return source;
            if (type.IsValueType) return source;

            if (visited != null && visited.TryGetValue(source, out var existing)) return existing;
            visited ??= new Dictionary<object, object>();

            // Arrays
            if (type.IsArray)
            {
                var arr = (Array)source;
                var elementType = type.GetElementType();
                var cloneArr = Array.CreateInstance(elementType, arr.Length);
                visited[source] = cloneArr;
                for (int i = 0; i < arr.Length; i++)
                {
                    cloneArr.SetValue(DeepCloneObject(arr.GetValue(i), visited), i);
                }
                return cloneArr;
            }

            // Lists / collections
            if (source is IList list)
            {
                var cloneList = (IList)Activator.CreateInstance(type);
                visited[source] = cloneList;
                for (int i = 0; i < list.Count; i++)
                {
                    cloneList.Add(DeepCloneObject(list[i], visited));
                }
                return cloneList;
            }

            // Plain classes
            var clone = Activator.CreateInstance(type);
            visited[source] = clone;

            // Unity serializes public instance fields (and [SerializeField] privates).
            // Here we copy all instance fields to avoid missing important data.
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            var fields = type.GetFields(flags);
            for (int i = 0; i < fields.Length; i++)
            {
                var f = fields[i];
                if (f.IsStatic) continue;
                var v = f.GetValue(source);
                f.SetValue(clone, DeepCloneObject(v, visited));
            }

            return clone;
        }

        private static T DeepCloneSettings<T>(T source) where T : class
        {
            if (source == null) return null;
            return (T)DeepCloneObject(source, null);
        }

        public string chartName;
        public string chartId;

        public CoordinateSystemType coordinateSystem = CoordinateSystemType.Cartesian2D;
        public CartesianMapping cartesian = new CartesianMapping();
        public List<AxisConfig> axes = new List<AxisConfig>();

        // New axis fields (X/Y based)
        public AxisId xAxisId = AxisId.XBottom;
        public AxisId yAxisId = AxisId.YLeft;
        
        public bool axisSelectionInitialized = false;
        public bool axisTypeSelectionInitialized = false;
        public float animationDuration = 1.0f;
        public BackgroundSettings background = new BackgroundSettings
        {
            show = true,
            textureFill = new TextureFillSettings { color = new Color(0.15f, 0.15f, 0.15f, 1f) }
        };

        public float chartWidth = 450f;
        public float chartHeight = 300f;

        [Padding4] public Vector4 padding = new Vector4(40f, 30f, 50f, 50f);
        public bool paddingInitialized = false;

        public Color xGridColor = new Color(0.5f, 0.5f, 0.5f, 0.2f);
        public float xGridLineWidth = 1.0f;
        public Color yGridColor = new Color(0.5f, 0.5f, 0.5f, 0.2f);
        public float yGridLineWidth = 1.0f;

        public CartesianGridSettings cartesianGrid = new CartesianGridSettings();
        public HoverSettings hover = new HoverSettings();
        public PolarAxesSettings polarAxes = new PolarAxesSettings();
        public bool gridSettingsInitialized = false;
        public bool hoverSettingsInitialized = false;

        public LegendSettings legendSettings = new LegendSettings();

        public PlotSettings plotSettings = new PlotSettings();

        [Header("Series Data")] public List<Serie> series = new List<Serie>();

        private AxisConfig GetAxis(AxisId id)
        {
            if (axes == null) return null;
            for (int i = 0; i < axes.Count; i++)
            {
                var a = axes[i];
                if (a != null && a.id == id) return a;
            }
            return null;
        }

        public void EnsureAxes()
        {
            EnsureAxesIntegrity();
        }

        public bool EnsureAxesIntegrity()
        {
            bool changed = false;

            var beforePadding = padding;
            var beforeAxisSelectionInitialized = axisSelectionInitialized;
            var beforeAxisTypeSelectionInitialized = axisTypeSelectionInitialized;
            var beforeXAxisId = xAxisId;
            var beforeYAxisId = yAxisId;
            var beforeGridSettingsInitialized = gridSettingsInitialized;
            AxisId beforeCartesianX = cartesian != null ? cartesian.xAxisId : AxisId.XBottom;
            AxisId beforeCartesianY = cartesian != null ? cartesian.yAxisId : AxisId.YLeft;
            int beforeAxesCount = axes != null ? axes.Count : 0;

            if (axes == null)
            {
                axes = new List<AxisConfig>();
                changed = true;
            }

            if (cartesian == null)
            {
                cartesian = new CartesianMapping();
                changed = true;
            }

            if (background == null)
            {
                background = new BackgroundSettings { show = true };
                changed = true;
            }

            if (!paddingInitialized)
            {
                if (padding == Vector4.zero)
                {
                    padding = new Vector4(40f, 30f, 50f, 50f);
                    changed = true;
                }
                paddingInitialized = true;
                changed = true;
            }

            if (cartesianGrid == null)
            {
                cartesianGrid = new CartesianGridSettings();
                changed = true;
            }
            if (hover == null)
            {
                hover = new HoverSettings();
                changed = true;
            }
            if (polarAxes == null)
            {
                polarAxes = new PolarAxesSettings();
                changed = true;
            }

            if (!gridSettingsInitialized)
            {
                cartesianGrid.xGridColor = xGridColor;
                cartesianGrid.xGridLineWidth = xGridLineWidth;
                cartesianGrid.yGridColor = yGridColor;
                cartesianGrid.yGridLineWidth = yGridLineWidth;
                gridSettingsInitialized = true;
            }

            if (!hoverSettingsInitialized)
            {
                hoverSettingsInitialized = true;
                changed = true;
            }

            bool IsXAxis(AxisId id) => id == AxisId.XBottom || id == AxisId.XTop;
            bool IsYAxis(AxisId id) => id == AxisId.YLeft || id == AxisId.YRight;

            // Initialize axis selection from existing cartesian mapping if not yet done
            if (!axisSelectionInitialized)
            {
                xAxisId = cartesian.xAxisId;
                yAxisId = cartesian.yAxisId;
                axisSelectionInitialized = true;
            }

            // Ensure xAxisId/yAxisId are valid (one X, one Y)
            if (!IsXAxis(xAxisId))
            {
                xAxisId = AxisId.XBottom;
                changed = true;
            }
            if (!IsYAxis(yAxisId))
            {
                yAxisId = AxisId.YLeft;
                changed = true;
            }

            // Sync to cartesian mapping
            if (cartesian.xAxisId != xAxisId || cartesian.yAxisId != yAxisId)
            {
                cartesian.xAxisId = xAxisId;
                cartesian.yAxisId = yAxisId;
                changed = true;
            }

            // Ensure axes referenced by mapping exist with default axisType
            if (GetAxis(xAxisId) == null)
            {
                axes.Add(new AxisConfig
                {
                    id = xAxisId,
                    axisType = AxisType.Category  // Default: X axis is Category
                });
                changed = true;
            }

            if (GetAxis(yAxisId) == null)
            {
                axes.Add(new AxisConfig
                {
                    id = yAxisId,
                    axisType = AxisType.Value  // Default: Y axis is Value
                });
                changed = true;
            }

            // One-time default axis type initialization:
            // - X defaults to Category
            // - Y defaults to Value
            // Only do this if both axes are still at the default Value (uninitialized state),
            // so we don't override user configurations (e.g. transposed charts or dual-category heatmaps).
            if (!axisTypeSelectionInitialized)
            {
                var xAxis = GetAxis(xAxisId);
                var yAxis = GetAxis(yAxisId);
                if (xAxis != null && yAxis != null
                    && xAxis.axisType == AxisType.Value
                    && yAxis.axisType == AxisType.Value)
                {
                    xAxis.axisType = AxisType.Category;
                    yAxis.axisType = AxisType.Value;
                    changed = true;
                }

                axisTypeSelectionInitialized = true;
                changed = true;
            }

            // Ensure labels list exists for all axes
            for (int i = 0; i < axes.Count; i++)
            {
                if (axes[i] != null && axes[i].labels == null)
                {
                    axes[i].labels = new List<string>();
                    changed = true;
                }
            }
            for (int i = 0; i < axes.Count; i++)
            {
                var a = axes[i];
                if (a == null) continue;
                if (a.labelStyle != null) continue;

                a.labelStyle = new LabelStyleSettings
                {
                    enabled = a.showLabels,
                    fontSize = a.fontSize,
                    color = a.labelColor,
                    position = a.labelPosition,
                    offset = a.labelOffset
                };
                changed = true;
            }

            if (padding != beforePadding) changed = true;
            if (gridSettingsInitialized != beforeGridSettingsInitialized) changed = true;
            if (axisSelectionInitialized != beforeAxisSelectionInitialized) changed = true;
            if (axisTypeSelectionInitialized != beforeAxisTypeSelectionInitialized) changed = true;
            if (xAxisId != beforeXAxisId) changed = true;
            if (yAxisId != beforeYAxisId) changed = true;
            if (cartesian.xAxisId != beforeCartesianX) changed = true;
            if (cartesian.yAxisId != beforeCartesianY) changed = true;
            if (axes.Count != beforeAxesCount) changed = true;

            return changed;
        }

        private int GetDataVersion()
        {
            // Simple hash based on key fields to detect data changes
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + (int)coordinateSystem;
                hash = hash * 31 + (series?.Count ?? 0);
                hash = hash * 31 + (axes?.Count ?? 0);
                hash = hash * 31 + (polarAxes?.angleAxis?.labels?.Count ?? 0);
                return hash;
            }
        }

        public bool EnsureRuntimeData()
        {
#if UNITY_EDITOR
            // Cache optimization: skip if called within 1 second and data hasn't changed
            if (!UnityEngine.Application.isPlaying)
            {
                double now = UnityEditor.EditorApplication.timeSinceStartup;
                int currentVersion = GetDataVersion();

                bool needsIdentityFix = string.IsNullOrWhiteSpace(chartId) || string.IsNullOrWhiteSpace(chartName);
                
                if (!needsIdentityFix && now - _lastEnsureTime < 1.0 && currentVersion == _lastEnsureVersion)
                {
                    return false; // No changes, skip execution
                }
                
                _lastEnsureTime = now;
                _lastEnsureVersion = currentVersion;
            }
#endif
            
            bool changed = false;

            if (string.IsNullOrWhiteSpace(chartName))
            {
                chartName = name;
                changed = true;
            }
            if (string.IsNullOrWhiteSpace(chartId))
            {
                chartId = Guid.NewGuid().ToString("N");
                changed = true;
            }

            if (EnsureAxesIntegrity()) changed = true;

            if (polarAxes == null)
            {
                polarAxes = new PolarAxesSettings();
                changed = true;
            }
            if (polarAxes.angleAxis == null)
            {
                polarAxes.angleAxis = new PolarAxisStyle();
                changed = true;
            }
            if (polarAxes.radiusAxis == null)
            {
                polarAxes.radiusAxis = new PolarAxisStyle();
                changed = true;
            }
            if (polarAxes.angleAxis.labels == null)
            {
                polarAxes.angleAxis.labels = new List<string>();
                changed = true;
            }
            if (polarAxes.radiusAxis.labels == null)
            {
                polarAxes.radiusAxis.labels = new List<string>();
                changed = true;
            }

            if (polarAxes.angleAxis.labelStyle == null)
            {
                polarAxes.angleAxis.labelStyle = new LabelStyleSettings
                {
                    enabled = polarAxes.angleAxis.showLabels,
                    fontSize = polarAxes.angleAxis.fontSize,
                    color = polarAxes.angleAxis.labelColor,
                    position = polarAxes.angleAxis.labelPosition,
                    offset = polarAxes.angleAxis.labelOffset
                };
                changed = true;
            }

            if (polarAxes.radiusAxis.labelStyle == null)
            {
                polarAxes.radiusAxis.labelStyle = new LabelStyleSettings
                {
                    enabled = polarAxes.radiusAxis.showLabels,
                    fontSize = polarAxes.radiusAxis.fontSize,
                    color = polarAxes.radiusAxis.labelColor,
                    position = polarAxes.radiusAxis.labelPosition,
                    offset = polarAxes.radiusAxis.labelOffset
                };
                changed = true;
            }

            // One-time migration: Polar2D (Radar dimensions) should be self-consistent.
            // If angle axis labels are empty but legacy Category axis labels exist, copy them.
            if (polarAxes.angleAxis.labels.Count == 0)
            {
                List<string> ResolveLegacyCategoryLabels()
                {
                    if (axes == null) return null;

                    // Use xAxisId as preferred for legacy category labels
                    AxisId preferred = xAxisId;
                    for (int i = 0; i < axes.Count; i++)
                    {
                        var a = axes[i];
                        if (a != null && a.id == preferred && a.axisType == AxisType.Category)
                        {
                            return a.labels;
                        }
                    }

                    for (int i = 0; i < axes.Count; i++)
                    {
                        var a = axes[i];
                        if (a != null && a.axisType == AxisType.Category)
                        {
                            return a.labels;
                        }
                    }

                    return null;
                }

                var legacy = ResolveLegacyCategoryLabels();
                if (legacy != null && legacy.Count > 0)
                {
                    polarAxes.angleAxis.labels = new List<string>(legacy);
                    changed = true;
                }
            }

            if (legendSettings == null)
            {
                legendSettings = new LegendSettings();
                changed = true;
            }

            if (plotSettings == null)
            {
                plotSettings = new PlotSettings();
                changed = true;
            }

            if (series == null)
            {
                series = new List<Serie>();
                changed = true;
            }

            for (int i = 0; i < series.Count; i++)
            {
                if (series[i] == null)
                {
                    series[i] = new Serie { name = $"Serie {i + 1}" };
                    changed = true;
                }

                if (series[i] != null && series[i].EnsureIntegrity())
                {
                    changed = true;
                }
            }

            for (int i = 0; i < series.Count; i++)
            {
                var s = series[i];
                if (s == null) continue;
                if (s.type != SerieType.Heatmap) continue;
                if (s.seriesData == null || s.seriesData.Count == 0) continue;

                bool looksLegacyHeatmap = false;
                for (int pi = 0; pi < s.seriesData.Count; pi++)
                {
                    var p = s.seriesData[pi];
                    if (p == null) continue;
                    if (!Mathf.Approximately(p.z, 0f))
                    {
                        looksLegacyHeatmap = true;
                        break;
                    }
                }

                if (!looksLegacyHeatmap) continue;

                for (int pi = 0; pi < s.seriesData.Count; pi++)
                {
                    var p = s.seriesData[pi];
                    if (p == null) continue;

                    float legacyY = p.value;
                    float legacyZ = p.z;

                    p.y = legacyY;
                    p.value = legacyZ;
                    p.z = 0f;
                }

                changed = true;
                if (s.EnsureIntegrity()) changed = true;
            }

            var radarLabels = polarAxes != null && polarAxes.angleAxis != null ? polarAxes.angleAxis.labels : null;
            if (radarLabels != null && radarLabels.Count > 0)
            {
                for (int i = 0; i < series.Count; i++)
                {
                    var s = series[i];
                    if (s == null) continue;
                    if (s.type != SerieType.Radar) continue;

                    bool empty = s.seriesData == null || s.seriesData.Count == 0;
                    if (!empty) continue;

                    s.seriesData = new List<SeriesData>(radarLabels.Count);
                    for (int di = 0; di < radarLabels.Count; di++)
                    {
                        float t = radarLabels.Count > 1 ? (float)di / (radarLabels.Count - 1) : 0.5f;
                        s.seriesData.Add(new SeriesData
                        {
                            x = di,
                            value = Mathf.Lerp(0f, 100f, t),
                            name = radarLabels[di],
                            id = Guid.NewGuid().ToString("N")
                        });
                    }

                    changed = true;
                    if (s.EnsureIntegrity()) changed = true;
                }
            }

            // Ensure each serie has its own settings instance.
            // UI/editor workflows (and array element duplication) can cause multiple series
            // to reference the same managedReference object, which makes edits leak across series.
            var seenSettings = new HashSet<object>();
            for (int i = 0; i < series.Count; i++)
            {
                var s = series[i];
                if (s == null || s.settings == null) continue;

                if (seenSettings.Contains(s.settings))
                {
                    var cloned = DeepCloneSettings(s.settings);
                    if (cloned != null)
                    {
                        s.settings = cloned;
                        changed = true;

                        if (s.EnsureIntegrity()) changed = true;
                    }
                }

                seenSettings.Add(s.settings);
            }

            bool hasPolar = false;
            bool hasCartesian = false;
            bool hasPie = false;
            for (int i = 0; i < series.Count; i++)
            {
                var s = series[i];
                if (s == null || !s.visible) continue;
                if (s.type == SerieType.Pie || s.type == SerieType.RingChart || s.type == SerieType.Pie3D)
                {
                    hasPie = true;
                    continue;
                }

                if (s.type == SerieType.Radar) hasPolar = true;
                else if (s.type == SerieType.Line || s.type == SerieType.Bar || s.type == SerieType.Scatter || s.type == SerieType.Heatmap) hasCartesian = true;
            }

            // CoordinateSystem is a user-chosen plot space. Do not rewrite it automatically.
            // Incompatible series are filtered at render time (ChartElement), and the editor UI filters selectable types.
            bool warn = true;
#if UNITY_EDITOR
            warn = Application.isPlaying;
#endif
            if (warn)
            {
                if (hasPolar && hasCartesian)
                {
                    Debug.LogWarning("[EasyChart] Mixed Polar (Radar) and Cartesian (Line/Bar/Scatter) series detected in one ChartProfile. This may produce semantically inconsistent axes/grid if CoordinateSystem/Axes settings are not aligned.");
                }
                else if (coordinateSystem == CoordinateSystemType.Cartesian2D && hasPolar)
                {
                    Debug.LogWarning("[EasyChart] CoordinateSystem is Cartesian2D but a Radar series exists. Radar will be rendered, but PolarAxes/Radar settings may be semantically inconsistent with the selected CoordinateSystem.");
                }
                else if (coordinateSystem == CoordinateSystemType.Polar2D && hasCartesian)
                {
                    Debug.LogWarning("[EasyChart] CoordinateSystem is Polar2D but Line/Bar/Scatter series exist. They will be rendered, but Cartesian axes/grid settings may be semantically inconsistent with the selected CoordinateSystem.");
                }
                else if (coordinateSystem == CoordinateSystemType.None && (hasPolar || hasCartesian))
                {
                    Debug.LogWarning("[EasyChart] CoordinateSystem is None but coordinate-based series exist. They will be rendered, but axis/grid settings may be semantically inconsistent.");
                }
                else if (coordinateSystem != CoordinateSystemType.None && hasPie && !hasPolar && !hasCartesian)
                {
                    Debug.LogWarning("[EasyChart] Only Pie series exist but CoordinateSystem is not None. Consider switching to None coordinate system.");
                }
            }

            return changed;
        }

        void Reset()
        {
            EnsureAxes();

            xAxisId = AxisId.XBottom;
            yAxisId = AxisId.YLeft;
            EnsureAxes();

            var xAxis = GetAxis(this.xAxisId);
            if (xAxis != null)
            {
                xAxis.axisType = AxisType.Category;  // Default: X is Category
                xAxis.labels = new List<string> { "A", "B", "C", "D", "E" };
            }

            var yAxis = GetAxis(this.yAxisId);
            if (yAxis != null)
            {
                yAxis.axisType = AxisType.Value;  // Default: Y is Value
            }

            var s = new Serie { name = "Series 1" };
            s.type = SerieType.Line;
            s.settings = new LineSettings { stroke = new LineStrokeSettings { color = Color.cyan } };
            
            s.seriesData.Add(new SeriesData { x = 0, value = 10 });
            s.seriesData.Add(new SeriesData { x = 1, value = 20 });
            s.seriesData.Add(new SeriesData { x = 2, value = 15 });
            s.seriesData.Add(new SeriesData { x = 3, value = 30 });
            s.seriesData.Add(new SeriesData { x = 4, value = 25 });
            series.Add(s);
        }

        public ChartData ToChartData()
        {
            var data = new ChartData();
            EnsureRuntimeData();
            data.CoordinateSystem = coordinateSystem;
            data.Cartesian = cartesian;
            data.Axes = axes;

            data.XAxisId = xAxisId;
            data.YAxisId = yAxisId;

            data.CartesianGrid = cartesianGrid;
            data.Hover = hover;
            data.PolarAxes = polarAxes;
            data.Plot = plotSettings;

            data.Series = this.series;
            data.legend = this.legendSettings;
            data.animationDuration = this.animationDuration;
            return data;
        }

        private void OnValidate()
        {
            EnsureRuntimeData();

            // Sync legend offset when position changes
            if (legendSettings != null)
            {
                legendSettings.SyncOffsetToPosition();
            }

            if (series == null) return;
            AxisConfig GetAxis(AxisId id)
            {
                if (axes == null) return null;
                for (int i = 0; i < axes.Count; i++)
                {
                    var a = axes[i];
                    if (a != null && a.id == id) return a;
                }
                return null;
            }

            bool transposed = false;
            if (coordinateSystem == CoordinateSystemType.Cartesian2D && cartesian != null)
            {
                var xAxis = GetAxis(cartesian.xAxisId);
                var yAxis = GetAxis(cartesian.yAxisId);

                bool hasHeatmap = false;
                if (series != null)
                {
                    for (int i = 0; i < series.Count; i++)
                    {
                        var s = series[i];
                        if (s != null && s.visible && s.type == SerieType.Heatmap)
                        {
                            hasHeatmap = true;
                            break;
                        }
                    }
                }

                if (hasHeatmap)
                {
                    for (int i = 0; i < axes.Count; i++)
                    {
                        var a = axes[i];
                        if (a == null) continue;
                        if (a.axisType != AxisType.Category) continue;
                        a.labelPlacement = CategoryLabelPlacement.CellCenter;
                    }
                }

                var xDim = xAxis != null ? xAxis.axisType : AxisType.Category;
                var yDim = yAxis != null ? yAxis.axisType : AxisType.Value;
                transposed = xDim == AxisType.Value && yDim == AxisType.Category;
            }

            // Data point convention:
            // - Normal: p.x = category index, p.y = value
            // - Transposed: p.y = category index, p.x = value
            bool categoryOnY = transposed;

            foreach (var serie in series)
            {
                if (serie == null) continue;
                if (serie.seriesData == null) continue;

                for (int i = 0; i < serie.seriesData.Count; i++)
                {
                    var p = serie.seriesData[i];
                    if (p == null) continue;
                    if (i > 0)
                    {
                        var prev = serie.seriesData[i - 1];
                        if (prev == null) continue;
                        float pCat = categoryOnY ? p.y : p.x;
                        float prevCat = categoryOnY ? prev.y : prev.x;
                        if (Mathf.Approximately(pCat, prevCat))
                        {
                            if (categoryOnY) p.y = i;
                            else p.x = i;
                        }
                    }
                }
            }
        }
    }
}