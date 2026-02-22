using System;
using System.Collections.Generic;
using UnityEngine;

namespace EasyChart
{
    public enum CoordinateSystemType
    {
        Cartesian2D,
        Polar2D,
        None
    }

    public enum AxisId
    {
        XBottom,
        XTop,
        YLeft,
        YRight
    }

    public enum AxisType
    {
        Category,
        Value
    }

    public enum CategoryLabelPlacement
    {
        Tick,
        CellCenter
    }

    [System.Serializable]
    public class CartesianMapping
    {
        public AxisId xAxisId = AxisId.XBottom;
        public AxisId yAxisId = AxisId.YLeft;
    }

    [System.Serializable]
    public class PlotLayoutSettings
    {
        public Vector2 centerOffset = Vector2.zero;
        public float padding = 10f;
    }

    [System.Serializable]
    public class LabelStyleSettings
    {
        public bool enabled = true;
        public int fontSize = 10;
        public Color color = Color.white;
        public LabelPosition position = LabelPosition.Outside;
        public Vector2 offset = Vector2.zero;
    }

    [System.Serializable]
    public class AxisConfig
    {
        public AxisId id;
        public AxisType axisType = AxisType.Value;

        public List<string> labels = new List<string>();

        public bool visible = true;
        public Color color = Color.white;
        public float width = 2.0f;

        public LabelStyleSettings labelStyle;

        public bool showLabels = true;
        public int fontSize = 10;
        public Color labelColor = Color.white;
        public LabelPosition labelPosition = LabelPosition.Outside;
        [InspectorName("LabelPlacement")]
        public CategoryLabelPlacement labelPlacement = CategoryLabelPlacement.Tick;
        public Vector2 labelOffset = Vector2.zero;
        public bool autoRangeMin = true;
        public bool autoRangeMax = true;
        public AutoRangeRoundingMode autoRangeRounding = AutoRangeRoundingMode.Tens;
        public float autoRangeUnit = 1f;
        public float minValue = 0f;
        public float maxValue = 1f;

        public bool autoTicks = true;
        public int splitCount = 5;
        public string labelFormat;

        public bool categoryAutoScroll = false;
        public bool categorySmoothScroll = true;
        public float categoryScrollInterval = 1.0f;
        public int categoryScrollStep = 1;

        public bool showUnit = false;
        public string unitText;
        public LabelStyleSettings unitLabelStyle;
    }

    public enum AutoRangeRoundingMode
    {
        None,
        Integer,
        Tens,
        Hundreds,
        Custom
    }

    public enum SerieType
    {
        // 折线图：适合展示随时间/类别变化的趋势（连续变化）。常用度：★★★★★ 难度：★★☆☆☆
        Line = 0,
        // 柱状图：适合展示分类对比（离散类别），强调数量大小。常用度：★★★★★ 难度：★★☆☆☆
        Bar = 1,
        // 饼图：适合展示占比构成（总量=100%），不适合类别过多。常用度：★★★★☆ 难度：★★★☆☆
        Pie = 2,
        // 散点图：适合展示二维数据分布、相关性（x-y 点）。常用度：★★★★☆ 难度：★★★☆☆
        Scatter = 4,
        // 雷达图：适合展示多维指标对比（同一维度尺度下的多项指标）。常用度：★★★☆☆ 难度：★★★★☆
        Radar = 5,
        // 环形图：饼图的变体，可在中心留空用于显示总量/标题等。常用度：★★★☆☆ 难度：★★★☆☆
        RingChart = 6,
        // 热力图：适合展示二维矩阵强度/密度（例如日历热力、相关矩阵等）。常用度：★★★☆☆ 难度：★★★★☆
        Heatmap = 7,
        // 仪表盘：展示单一指标当前值/区间（类似速度表）。常用度：★★☆☆☆ 难度：★★★☆☆
        Gauge = 8,
        // 横向柱状图：与柱状图类似，但横向布局更适合长标签类别。常用度：★★★★☆ 难度：★★☆☆☆
        HorizontalBar = 9,
        // K 线图：金融蜡烛图（开/高/低/收），用于行情走势。常用度：★★☆☆☆ 难度：★★★★★
        Candlestick = 21,
        // OHLC 图：与 K 线类似，用线段表示开高低收（更简洁）。常用度：★★☆☆☆ 难度：★★★★★
        OHLC = 22,
        // 箱线图：展示分布统计（四分位数、中位数、异常值）。常用度：★★☆☆☆ 难度：★★★★★
        BoxPlot = 23,
        // 直方图：展示单变量分布（分箱计数）。常用度：★★★☆☆ 难度：★★★☆☆
        Histogram = 24,
        // 瀑布图：展示从起点到终点的增减构成（正负贡献）。常用度：★★☆☆☆ 难度：★★★★☆
        Waterfall = 25,
        // 漏斗图：展示流程转化/阶段漏损（例如注册→下单→支付）。常用度：★★☆☆☆ 难度：★★★☆☆
        Funnel = 26,
        // 桑基图：展示流量/能量/资金流向与分配（有向流图）。常用度：★☆☆☆☆ 难度：★★★★★（不适合用当前 SeriesData，需图结构/边数据，先注释）
        // Sankey = 30,
        // 矩形树图：用矩形面积展示层级与占比（适合层级占比对比）。常用度：★☆☆☆☆ 难度：★★★★★（不适合用当前 SeriesData，需层级节点数据，先注释）
        // TreeMap = 31,
        // 旭日图：环形层级占比图（层级结构的构成展示）。常用度：★☆☆☆☆ 难度：★★★★★（不适合用当前 SeriesData，需层级节点数据，先注释）
        // Sunburst = 32,
        // 弦图：展示类别之间的关联/流量（成对关系强度）。常用度：★☆☆☆☆ 难度：★★★★★（不适合用当前 SeriesData，需矩阵/边数据，先注释）
        // Chord = 33,
        // 3D 饼图：饼图的三维表现形式（通常属于 Pro 扩展）。常用度：★☆☆☆☆ 难度：★★★★★
        Pie3D = 100,
        // 3D 柱状图：三维柱形对比（可能用于二维分类 + 数值）。常用度：★☆☆☆☆ 难度：★★★★★
        Bar3D = 101,
        // 3D 折线图：三维折线/轨迹展示（z 维度叠加）。常用度：★☆☆☆☆ 难度：★★★★★
        Line3D = 102,
        // 3D 散点图：三维坐标点分布（x,y,z）。常用度：★☆☆☆☆ 难度：★★★★★
        Scatter3D = 103,

    }

    [System.Serializable]
    public abstract class BaseSerieSettings
    {
    }

    [System.Serializable]
    public class PointSettings : ISerializationCallbackReceiver
    {
        public bool show = true;
        public float size = 4.0f;

        [InspectorName("Texture")]
        public TextureFillSettings textureFill = new TextureFillSettings();

        public void OnBeforeSerialize()
        {
            if (textureFill == null) textureFill = new TextureFillSettings();
        }

        public void OnAfterDeserialize()
        {
            if (textureFill == null) textureFill = new TextureFillSettings();
        }
    }

    [System.Serializable]
    public class TextureMappingSettings
    {
        public Texture2D texture;
        public Vector2 tiling = Vector2.one;
        public Vector2 offset = Vector2.zero;
    }

    public enum TextureFillAnimationType
    {
        None = 0,
        TextureUvMove = 1,
        TextureScale = 2,
        TextureFade = 3
    }

    public enum TextureFillScaleType
    {
        ZoomIn = 0,
        ZoomOut = 1,
        Sin = 2,
        PingPong = 3
    }

    public enum TextureFillColorOverLifeWrap
    {
        Loop = 0,
        PingPong = 1,
        Clamp = 2
    }

    [System.Serializable]
    public class TextureFillSettings : ISerializationCallbackReceiver
    {
        public Texture2D texture;
        public Vector2 tiling = Vector2.one;
        public Vector2 offset = Vector2.zero;
        public Color color = Color.white;

        public TextureFillAnimationType animationType = TextureFillAnimationType.None;

        public Vector2 uvMoveSpeed = Vector2.zero;

        public TextureFillScaleType scaleType = TextureFillScaleType.Sin;
        public float scaleSpeed = 1f;

        public Vector2 scaleFrom = Vector2.one;
        public Vector2 scaleTo = new Vector2(1.2f, 1.2f);
        public Color colorFadeStart = Color.white;
        public Color colorFadeEnd = new Color(1f, 1f, 1f, 0f);
        public float colorFadeSpeed = 1f;

        public TextureFillColorOverLifeWrap colorFadeWrap = TextureFillColorOverLifeWrap.PingPong;
        public Gradient colorFadeGradient;

        public void OnBeforeSerialize()
        {
        }

        public void OnAfterDeserialize()
        {
            if (colorFadeGradient == null)
            {
                colorFadeGradient = new Gradient();
                colorFadeGradient.SetKeys(
                    new[]
                    {
                        new GradientColorKey(colorFadeStart, 0f),
                        new GradientColorKey(colorFadeEnd, 1f)
                    },
                    new[]
                    {
                        new GradientAlphaKey(colorFadeStart.a, 0f),
                        new GradientAlphaKey(colorFadeEnd.a, 1f)
                    }
                );
            }
        }
    }

    [System.Serializable]
    public class SeriesData
    {
        public string id;
        public float x;
        public float value;
        public float y;
        public float z;
        public string name;
        public bool useColor = false;
        public Color color = Color.white;
    }

    [System.Serializable]
    public class SizeMappingSettings
    {
        public bool enabled = false;
        public float minValue = 0f;
        public float maxValue = 1f;
        public float minSize = 4f;
        public float maxSize = 12f;
        public bool clamp = true;
        public AnimationCurve curve = AnimationCurve.Linear(0, 0, 1, 1);
    }

    [System.Serializable]
    public class HoverHighlightSettings : ISerializationCallbackReceiver
    {
        public bool enabled = false;
        [SerializeField, HideInInspector]
        private float _scale = 1.5f;

        [SerializeField, HideInInspector]
        private TextureFillSettings _textureFill = new TextureFillSettings { color = new Color(1f, 1f, 1f, 0.35f) };

        public float scale
        {
            get => _scale;
            set => _scale = value;
        }

        public TextureFillSettings textureFill
        {
            get => _textureFill;
            set => _textureFill = value;
        }

        public float pickRadius = 8f;

        public PointSettings point = new PointSettings();

        public Color lineColor = new Color(1f, 1f, 1f, 0.35f);

        public void OnBeforeSerialize()
        {
            if (_textureFill == null) _textureFill = new TextureFillSettings { color = new Color(1f, 1f, 1f, 0.35f) };
            if (point == null) point = new PointSettings();
        }

        public void OnAfterDeserialize()
        {
            if (_textureFill == null) _textureFill = new TextureFillSettings { color = new Color(1f, 1f, 1f, 0.35f) };
            if (point == null) point = new PointSettings();
        }
    }

    [System.Serializable]
    public class BarHoverSettings : ISerializationCallbackReceiver
    {
        public bool enabled = false;
        public float pickRadius = 8f;
        public float scale = 1.5f;
        public TextureFillSettings textureFill = new TextureFillSettings { color = new Color(1f, 1f, 1f, 0.35f) };

        public void OnBeforeSerialize()
        {
            if (textureFill == null) textureFill = new TextureFillSettings { color = new Color(1f, 1f, 1f, 0.35f) };
        }

        public void OnAfterDeserialize()
        {
            if (textureFill == null) textureFill = new TextureFillSettings { color = new Color(1f, 1f, 1f, 0.35f) };
        }
    }

    [System.Serializable]
    public class LineStrokeSettings
    {
        public LineType lineType = LineType.Straight;
        public float width = 2.0f;
        public Color color = Color.white;

        [InspectorName("Texture")]
        public TextureFillSettings textureFill = new TextureFillSettings();
    }

    [System.Serializable]
    public class AreaFillSettings : ISerializationCallbackReceiver
    {
        public bool show = false;

        [InspectorName("Texture")]
        public TextureFillSettings textureFill = new TextureFillSettings { color = new Color(1, 1, 1, 0.2f) };

        public void OnBeforeSerialize()
        {
            if (textureFill == null) textureFill = new TextureFillSettings { color = new Color(1, 1, 1, 0.2f) };
        }

        public void OnAfterDeserialize()
        {
            if (textureFill == null) textureFill = new TextureFillSettings { color = new Color(1, 1, 1, 0.2f) };
        }
    }

    [System.Serializable]
    public class BorderSettings
    {
        public float width = 0f;
        public Color color = new Color(0, 0, 0, 0);
    }

    [System.Serializable]
    public class BackgroundSettings : ISerializationCallbackReceiver
    {
        public bool show = false;

        [InspectorName("Texture")]
        public TextureFillSettings textureFill = new TextureFillSettings { color = new Color(1, 1, 1, 0.08f) };

        public void OnBeforeSerialize()
        {
            if (textureFill == null) textureFill = new TextureFillSettings { color = new Color(1, 1, 1, 0.08f) };
        }

        public void OnAfterDeserialize()
        {
            if (textureFill == null) textureFill = new TextureFillSettings { color = new Color(1, 1, 1, 0.08f) };
        }
    }

    [System.Serializable]
    public class LineSettings : BaseSerieSettings
    {
        [Header("Stroke")]
        [InspectorName("Stroke")]
        public LineStrokeSettings stroke = new LineStrokeSettings();

        [Header("Point")]
        public PointSettings point = new PointSettings();

        [Header("Hover")]
        public HoverHighlightSettings hover = new HoverHighlightSettings();
        
        [Header("Area")]
        public AreaFillSettings area = new AreaFillSettings();
    }

    [System.Serializable]
    public class ScatterSettings : BaseSerieSettings
    {
        [Header("Point")]
        public PointSettings point = new PointSettings();

        public SizeMappingSettings sizeMapping = new SizeMappingSettings();

        public HoverHighlightSettings hover = new HoverHighlightSettings();
    }

    [System.Serializable]
    public class HeatmapSettings : BaseSerieSettings
    {
        public enum RenderMode
        {
            [Tooltip("Discrete cell-based rendering with sharp boundaries")]
            Grid,
            [Tooltip("Smooth pixel-based rendering using Gaussian interpolation")]
            Gradient,
            [Tooltip("Contour lines with optional filled regions")]
            Contour
        }

        public enum InfluenceMode
        {
            [Tooltip("No influence spreading between cells")]
            None,
            [Tooltip("Values bleed into neighboring cells with falloff")]
            Bleed,
            [Tooltip("Gaussian smoothing applied to the grid")]
            Smooth
        }

        [System.Serializable]
        public class BleedSettings
        {
            [Tooltip("Number of cells the influence spreads to")]
            public int radiusCells = 1;
            [Tooltip("Number of discrete falloff steps")]
            public int steps = 6;
            [Tooltip("Strength of the bleed effect (0-1)")]
            [Range(0f, 1f)]
            public float strength = 0.35f;
        }

        [System.Serializable]
        public class SmoothSettings
        {
            [Tooltip("Radius of the smoothing kernel in cells")]
            public int radiusCells = 1;
            [Tooltip("Number of smoothing passes to apply")]
            public int passes = 1;
            [Tooltip("Standard deviation of the Gaussian kernel")]
            public float sigma = 1f;
        }

        [System.Serializable]
        public class GradientSettings
        {
            [Tooltip("Texture resolution for gradient rendering (higher = smoother but slower)")]
            public int resolution = 256;
            [Tooltip("Gaussian kernel sigma for density estimation (larger = smoother blending)")]
            public float sigma = 30f;
            [Tooltip("Contrast adjustment: higher values increase contrast (sharper peaks), lower values spread colors more evenly")]
            public float intensity = 1f;
        }

        [System.Serializable]
        public class ContourSettings
        {
            [Tooltip("Number of contour levels to generate")]
            public int levels = 10;
            [Tooltip("Width of contour lines in pixels")]
            public float lineWidth = 1f;
            [Tooltip("Show filled regions between contour lines")]
            public bool showFill = true;
            [Tooltip("Show contour lines")]
            public bool showLines = true;
            [Tooltip("Color of the contour lines")]
            public Color lineColor = new Color(0.2f, 0.2f, 0.2f, 0.8f);
        }

        [Tooltip("Rendering mode: Grid (discrete cells), Gradient (smooth interpolation), or Contour (isolines)")]
        public RenderMode renderMode = RenderMode.Grid;

        [Tooltip("Gap between cells in pixels (Grid mode only)")]
        public float cellGapPx = 1f;

        [Tooltip("Number of horizontal cells when using Value axis type")]
        public int xSplitCount = 10;
        [Tooltip("Number of vertical cells when using Value axis type")]
        public int ySplitCount = 10;

        [Tooltip("Automatically calculate value range from data")]
        public bool autoRange = true;
        [Tooltip("Minimum value for color mapping (when autoRange is off)")]
        public float minValue = 0f;
        [Tooltip("Maximum value for color mapping (when autoRange is off)")]
        public float maxValue = 1f;

        [Tooltip("How cell values influence neighboring cells (Grid mode only)")]
        public InfluenceMode influenceMode = InfluenceMode.None;
        [Tooltip("Settings for Bleed influence mode")]
        public BleedSettings bleed = new BleedSettings();
        [Tooltip("Settings for Smooth influence mode")]
        public SmoothSettings smooth = new SmoothSettings();
        [Tooltip("Settings for Gradient render mode")]
        public GradientSettings gradient = new GradientSettings();
        [Tooltip("Settings for Contour render mode")]
        public ContourSettings contour = new ContourSettings();

        [Tooltip("Clamp values to min/max range before color mapping")]
        public bool clamp = true;
        [Tooltip("Color for minimum values")]
        public Color lowColor = new Color(0f, 0.55f, 1f, 1f);
        [Tooltip("Color for middle values (used for interpolation)")]
        public Color midColor = Color.white;
        [Tooltip("Color for maximum values")]
        public Color highColor = new Color(1f, 0.25f, 0.25f, 1f);
    }

    [System.Serializable]
    public class RadarSettings : BaseSerieSettings
    {
        [System.Serializable]
        public class RadarLayoutSettings
        {
            public float startAngleDeg = -90f;
            public bool clockwise = true;

            public float innerRadius = 0f;
            public float outerRadius = 0f;

            public PlotLayoutSettings plot = new PlotLayoutSettings();

            public TextureFillSettings background = new TextureFillSettings { color = new Color(0.2f, 0.2f, 0.2f, 0.3f) };
        }

        [Header("Radar")]
        public RadarLayoutSettings radar = new RadarLayoutSettings();

        [Header("LineStroke")]
        [InspectorName("LineStroke")]
        public LineStrokeSettings stroke = new LineStrokeSettings();

        [Header("Area")]
        public AreaFillSettings area = new AreaFillSettings { show = true, textureFill = new TextureFillSettings { color = new Color(1f, 1f, 1f, 0.2f) } };

        [Header("Point")]
        public PointSettings point = new PointSettings();
    }

    [System.Serializable]
    public class BarSettings : BaseSerieSettings, ISerializationCallbackReceiver
    {
        public float barWidth = 10f;

        [InspectorName("Texture")]
        public TextureFillSettings textureFill = new TextureFillSettings();

        public bool stacked = false;

        public float cornerRadius = 0f;
        public int cornerSegments = 4;

        public string stackGroup = "";
        public float barGap = 2f;
        public float categoryGap = 0f;

        public BorderSettings border = new BorderSettings();

        public BackgroundSettings background = new BackgroundSettings();

        public BarHoverSettings hover = new BarHoverSettings();

        public void OnBeforeSerialize()
        {
            if (textureFill == null) textureFill = new TextureFillSettings();
        }

        public void OnAfterDeserialize()
        {
            if (textureFill == null) textureFill = new TextureFillSettings();

            if (hover == null) hover = new BarHoverSettings();
            hover.OnAfterDeserialize();
        }
    }

    public enum SliceGapType
    {
        Radial,
        Translate,
        Uniform
    }

    [System.Serializable]
    public class PieLayoutSettings
    {
        public float startAngleDeg = -90f;
        public bool clockwise = true;
        public float angleRangeDeg = 360f;

        public float innerRadius = 0f;
        public Color innerRadiusColor = new Color(0, 0, 0, 0);
        public float cornerRadius = 0f;
        public float sliceGapPx = 0f;
        public SliceGapType sliceGapType = SliceGapType.Radial;
        public float outerRadius = 0f;

        public PlotLayoutSettings plot = new PlotLayoutSettings();
    }

    [System.Serializable]
    public class RingSettings
    {
        public bool showBackground = false;
        public float backgroundAlpha = 0.25f;
        public Color backgroundColor = new Color(0, 0, 0, 0);
        public float cornerRadius = 0f;
        public float ringGapPx = 0f;
    }

    public enum PieLegendSource
    {
        Slice,
        RingSlice,
        Series
    }

    public enum ValueDisplayStyle
    {
        None,
        Parentheses,
        AlignedColumns
    }

    [System.Serializable]
    public class PieLegendSettings
    {
        [HideInInspector] public bool overrideEnabled = false;
        public PieLegendSource source = PieLegendSource.Slice;

        public bool enabled = true;
        [HideInInspector] public bool showValue = false;
        public ValueDisplayStyle valueDisplayStyle = ValueDisplayStyle.None;
        public LegendPosition position = LegendPosition.Bottom;
        public int fontSize = 12;
        public Color color = Color.white;
        public Color backgroundColor = new Color(0, 0, 0, 0);
        public float itemSpacing = 10f;
        public Vector2 offset = Vector2.zero;
    }

    [System.Serializable]
    public class PieSettings : BaseSerieSettings
    {
        public bool sortByValue = false;
        public PieLayoutSettings layout = new PieLayoutSettings();
        public PieHoverSettings hover = new PieHoverSettings();
        public PieAggregationSettings aggregation = new PieAggregationSettings();
        public RingSettings ring = new RingSettings();
        public PieLegendSettings legend = new PieLegendSettings();
    }

    [System.Serializable]
    public class RingChartLayoutSettings
    {
        public float startAngleDeg = -90f;
        public bool clockwise = true;
        public float angleRangeDeg = 360f;
        public float innerRadius = 0.3f;
        public float outerRadius = 0f;
        public PlotLayoutSettings plot = new PlotLayoutSettings();
    }

    public enum RingValueMappingMode
    {
        Percent,
        Range
    }

    [System.Serializable]
    public class RingValueMappingSettings
    {
        public RingValueMappingMode mode = RingValueMappingMode.Percent;
        public bool autoRange = true;
        public float minValue = 0f;
        public float maxValue = 1f;
    }

    [System.Serializable]
    public class RingChartSettings : BaseSerieSettings
    {
        public RingChartLayoutSettings layout = new RingChartLayoutSettings();
        public RingValueMappingSettings valueMapping = new RingValueMappingSettings();
        public PieHoverSettings hover = new PieHoverSettings();
        public PieLegendSettings legend = new PieLegendSettings();

        public bool showBackground = false;
        public float backgroundAlpha = 0.25f;
        public Color backgroundColor = new Color(0, 0, 0, 0);
        public float cornerRadius = 0f;
        public float ringGapPx = 0f;
    }

    public enum PieExplodeType
    {
        Translate,
        Pull,
        Color,
        Stroke
    }

    [System.Serializable]
    public class PieHoverSettings
    {
        public bool enabled = false;
        public float explodeDistance = 10f;
        public PieExplodeType explodeType = PieExplodeType.Translate;
    }

    [System.Serializable]
    public class PieAggregationSettings
    {
        public bool enabled = false;
        public int keepTopN = 0;
        public string othersName = "Others";
        public bool useOthersColor = false;
        public Color othersColor = new Color(0.6f, 0.6f, 0.6f, 1f);
    }

    [System.Serializable]
    public class SerieLabelSettings
    {
        public bool enabled = true;
        public Color color = Color.white;
        public int fontSize = 10;
        public int decimalPlaces = 2;
        public bool showName = false; // For Pie: show category name
        public Vector2 offset = Vector2.zero;
        public LabelPosition position = LabelPosition.Outside;
        public TextureFillSettings background = new TextureFillSettings { color = new Color(0f, 0f, 0f, 0f) };
    }

    public enum LegendPosition
    {
        Top,
        Bottom,
        Left,
        Right
    }

    [System.Serializable]
    public class LegendSettings : ISerializationCallbackReceiver
    {
        public bool enabled = true;
        public LegendPosition position = LegendPosition.Bottom;
        public int fontSize = 12;
        public Color color = Color.white;
        public Color backgroundColor = new Color(0, 0, 0, 0);
        public float itemSpacing = 10f;
        [Tooltip("Offset from edge. Auto-set based on position when zero.")]
        public Vector2 offset = new Vector2(0, -30);

        [System.NonSerialized] private LegendPosition _lastPosition = LegendPosition.Bottom;
        [System.NonSerialized] private bool _initialized;

        public void OnBeforeSerialize() { }

        public void OnAfterDeserialize()
        {
            if (!_initialized)
            {
                _lastPosition = position;
                _initialized = true;
            }
        }

        public void SyncOffsetToPosition()
        {
            if (_lastPosition != position)
            {
                offset = GetDefaultOffsetForPosition(position);
                _lastPosition = position;
            }
        }

        public static Vector2 GetDefaultOffsetForPosition(LegendPosition pos)
        {
            switch (pos)
            {
                case LegendPosition.Top: return new Vector2(0, 30);
                case LegendPosition.Bottom: return new Vector2(0, -30);
                case LegendPosition.Right: return new Vector2(-30, 0);
                case LegendPosition.Left: return new Vector2(30, 0);
                default: return Vector2.zero;
            }
        }
    }

    [System.Serializable]
    public class PlotSettings
    {
        public bool overrideTheme = false;
        public Color backgroundColor = new Color(0f, 0f, 0f, 0f);
        public Color borderColor = new Color(0f, 0f, 0f, 0f);
        public float borderWidth = 0f;

        [Tooltip("Edge fade distance in pixels for series labels. Labels outside PlotArea will fade to transparent over this distance.")]
        public float labelEdgeFadePx = 20f;
    }

    [System.Serializable]
    public class CartesianGridSettings
    {
        public Color xGridColor = new Color(0.5f, 0.5f, 0.5f, 0.2f);
        public float xGridLineWidth = 1.0f;
        public bool xGridDashed = false;
        public float xGridDashLength = 4f;
        public float xGridDashGap = 2f;
        public float xGridDashOffset = 0f;
        public Color yGridColor = new Color(0.5f, 0.5f, 0.5f, 0.2f);
        public float yGridLineWidth = 1.0f;
        public bool yGridDashed = false;
        public float yGridDashLength = 4f;
        public float yGridDashGap = 2f;
        public float yGridDashOffset = 0f;
    }

    [System.Serializable]
    public class HoverSettings
    {
        public bool cursorLineDashed = false;
        public float cursorLineWidth = 1f;
        public Color cursorLineColor = Color.yellow;
        public float cursorLineDashLength = 4f;
        public float cursorLineDashGap = 2f;
        public float cursorLineDashOffset = 0f;
    }

    [System.Serializable]
    public class PolarAxisStyle
    {
        public List<string> labels = new List<string>();

        public bool visible = true;
        public Color color = Color.white;
        public Color edgeColor = Color.white;
        public float width = 2.0f;

        public LabelStyleSettings labelStyle;

        public bool showLabels = true;
        public int fontSize = 10;
        public Color labelColor = Color.white;
        public LabelPosition labelPosition = LabelPosition.Outside;
        public Vector2 labelOffset = Vector2.zero;
        public bool autoRangeMin = true;
        public bool autoRangeMax = true;
        public AutoRangeRoundingMode autoRangeRounding = AutoRangeRoundingMode.Tens;
        public float autoRangeUnit = 1f;
        public float minValue = 0f;
        public float maxValue = 1f;

        public bool autoTicks = true;
        public int splitCount = 5;
        public string labelFormat;
    }

    [System.Serializable]
    public class PolarAxesSettings
    {
        public PolarAxisStyle angleAxis = new PolarAxisStyle();
        public PolarAxisStyle radiusAxis = new PolarAxisStyle();
    }

    [System.Serializable]
    public class ChartData
    {
        public CoordinateSystemType CoordinateSystem = CoordinateSystemType.Cartesian2D;
        public CartesianMapping Cartesian = new CartesianMapping();
        public List<AxisConfig> Axes = new List<AxisConfig>();

        // New axis fields (X/Y based)
        public AxisId XAxisId = AxisId.XBottom;
        public AxisId YAxisId = AxisId.YLeft;

        public CartesianGridSettings CartesianGrid = new CartesianGridSettings();
        public HoverSettings Hover = new HoverSettings();
        public PolarAxesSettings PolarAxes = new PolarAxesSettings();
        public PlotSettings Plot = new PlotSettings();

        public List<Serie> Series = new List<Serie>();
        public LegendSettings legend = new LegendSettings();
        
        public float animationDuration = 1.0f;
    }

    public enum LineType
    {
        Straight,
        Smooth, 
        Step 
    }

    public enum LabelPosition
    {
        Outside,
        Inside,
        Center
    }

    [System.Serializable]
    public class Serie : ISerializationCallbackReceiver
    {
        public string name;
        public string id;
        public SerieType type = SerieType.Line;
        public bool visible = true;

        [SerializeReference]
        public BaseSerieSettings settings;

        [SerializeField, SerializeReference]
        private BaseSerieSettings _cachedLineSettings;
        [SerializeField, SerializeReference]
        private BaseSerieSettings _cachedBarSettings;
        [SerializeField, SerializeReference]
        private BaseSerieSettings _cachedPieSettings;
        [SerializeField, SerializeReference]
        private BaseSerieSettings _cachedRingChartSettings;
        [SerializeField, SerializeReference]
        private BaseSerieSettings _cachedScatterSettings;
        [SerializeField, SerializeReference]
        private BaseSerieSettings _cachedRadarSettings;
        [SerializeField, SerializeReference]
        private BaseSerieSettings _cachedHeatmapSettings;
        
        public SerieLabelSettings labelSettings = new SerieLabelSettings();

        // Actual Data
        public List<SeriesData> seriesData = new List<SeriesData>();

        public void OnBeforeSerialize()
        {
        }

        public void OnAfterDeserialize()
        {
        }

        public bool SetType(SerieType newType)
        {
            bool changed = false;

            // Preserve current settings into a per-type cache before switching.
            // IMPORTANT: in editor workflows the enum `type` might already be changed
            // (via SerializedObject.ApplyModifiedProperties) before SetType is called,
            // so we cache based on the runtime type of `settings`, not the current `type`.
            if (settings != null)
            {
                if (settings is LineSettings) _cachedLineSettings = settings;
                else if (settings is BarSettings) _cachedBarSettings = settings;
                else if (settings is PieSettings) _cachedPieSettings = settings;
                else if (settings is ScatterSettings) _cachedScatterSettings = settings;
                else if (settings is RadarSettings) _cachedRadarSettings = settings;
                else if (settings is HeatmapSettings) _cachedHeatmapSettings = settings;
            }

            if (type != newType)
            {
                type = newType;
                changed = true;
            }

            // Minimal migration / preservation for common edits.
            // Data loss is acceptable per design, but keeping point settings improves UX.
            if (newType == SerieType.Scatter)
            {
                if (settings is ScatterSettings)
                {
                    // ok
                }
                else if (_cachedScatterSettings is ScatterSettings cached)
                {
                    settings = cached;
                    changed = true;
                }
                else if (settings is LineSettings ls)
                {
                    if (ls.point == null) ls.point = new PointSettings();
                    settings = new ScatterSettings
                    {
                        point = ls.point,
                        sizeMapping = new SizeMappingSettings(),
                        hover = new HoverHighlightSettings()
                    };
                    changed = true;
                }
                else
                {
                    settings = new ScatterSettings();
                    changed = true;
                }
            }
            else if (newType == SerieType.Line)
            {
                if (settings is LineSettings)
                {
                    // ok
                }
                else if (_cachedLineSettings is LineSettings cached)
                {
                    settings = cached;
                    changed = true;
                }
                else if (settings is ScatterSettings ss)
                {
                    if (ss.point == null) ss.point = new PointSettings();
                    settings = new LineSettings
                    {
                        point = ss.point,
                        stroke = new LineStrokeSettings(),
                        area = new AreaFillSettings()
                    };
                    changed = true;
                }
                else
                {
                    settings = new LineSettings();
                    changed = true;
                }
            }
            else if (newType == SerieType.Bar || newType == SerieType.HorizontalBar)
            {
                if (settings is BarSettings)
                {
                    // ok
                }
                else if (_cachedBarSettings is BarSettings cached)
                {
                    settings = cached;
                    changed = true;
                }
                else
                {
                    settings = new BarSettings();
                    changed = true;
                }
            }
            else if (newType == SerieType.Pie)
            {
                if (settings is PieSettings)
                {
                    // ok
                }
                else if (_cachedPieSettings is PieSettings cached)
                {
                    settings = cached;
                    changed = true;
                }
                else
                {
                    settings = new PieSettings();
                    changed = true;
                }
            }
            else if (newType == SerieType.RingChart)
            {
                if (settings is RingChartSettings)
                {
                    // ok
                }
                else if (_cachedRingChartSettings is RingChartSettings cachedRing)
                {
                    settings = cachedRing;
                    changed = true;
                }
                else
                {
                    settings = new RingChartSettings();
                    changed = true;
                }

                if (settings is RingChartSettings ringSettings)
                {
                    if (ringSettings.layout == null) { ringSettings.layout = new RingChartLayoutSettings(); changed = true; }
                    if (ringSettings.layout != null && ringSettings.layout.innerRadius <= 0f) { ringSettings.layout.innerRadius = 0.3f; changed = true; }
                }
            }
            else if (newType == SerieType.Pie3D)
            {
                if (SerieSettingsRegistry.TryCreate(newType, out var created))
                {
                    if (settings is PieSettings oldPs && created is PieSettings newPs && created.GetType() != settings.GetType())
                    {
                        if (oldPs.layout != null) newPs.layout = oldPs.layout;
                        if (oldPs.hover != null) newPs.hover = oldPs.hover;
                        if (oldPs.aggregation != null) newPs.aggregation = oldPs.aggregation;
                        if (oldPs.ring != null) newPs.ring = oldPs.ring;
                        if (oldPs.legend != null) newPs.legend = oldPs.legend;
                        settings = newPs;
                        changed = true;
                    }
                    else if (!(settings is PieSettings))
                    {
                        settings = created;
                        changed = true;
                    }
                }
                else
                {
                    settings = null;
                    changed = true;
                }
            }
            else if (newType == SerieType.Radar)
            {
                if (settings is RadarSettings)
                {
                    // ok
                }
                else if (_cachedRadarSettings is RadarSettings cached)
                {
                    settings = cached;
                    changed = true;
                }
                else
                {
                    settings = new RadarSettings();
                    changed = true;
                }
            }
            else if (newType == SerieType.Heatmap)
            {
                if (settings is HeatmapSettings)
                {
                    // ok
                }
                else if (_cachedHeatmapSettings is HeatmapSettings cached)
                {
                    settings = cached;
                    changed = true;
                }
                else
                {
                    settings = new HeatmapSettings();
                    changed = true;
                }
            }
            else
            {
                if (SerieSettingsRegistry.TryCreate(newType, out var created))
                {
                    settings = created;
                    changed = true;
                }
                else
                {
                    settings = null;
                    changed = true;
                }
            }

            if (EnsureIntegrity()) changed = true;
            return changed;
        }

        public bool EnsureIntegrity()
        {
            bool changed = false;

            if (string.IsNullOrEmpty(id))
            {
                id = Guid.NewGuid().ToString("N");
                changed = true;
            }

            var before = settings;
            EnsureSettings();
            if (!ReferenceEquals(before, settings)) changed = true;

            if (labelSettings == null)
            {
                labelSettings = new SerieLabelSettings();
                changed = true;
            }

            if (labelSettings != null)
            {
                if (labelSettings.fontSize <= 0)
                {
                    labelSettings.fontSize = 10;
                    changed = true;
                }

                if (labelSettings.decimalPlaces < 0 || labelSettings.decimalPlaces > 8)
                {
                    labelSettings.decimalPlaces = Mathf.Clamp(labelSettings.decimalPlaces, 0, 8);
                    changed = true;
                }

                var c = labelSettings.color;
                bool looksUninitialized = Mathf.Approximately(c.r, 0f)
                    && Mathf.Approximately(c.g, 0f)
                    && Mathf.Approximately(c.b, 0f)
                    && Mathf.Approximately(c.a, 0f);
                if (looksUninitialized)
                {
                    labelSettings.color = Color.white;
                    changed = true;
                }
            }

            if (seriesData == null)
            {
                seriesData = new List<SeriesData>();
                changed = true;
            }

            for (int i = 0; i < seriesData.Count; i++)
            {
                var dp = seriesData[i];
                if (dp == null) continue;
                if (string.IsNullOrEmpty(dp.id))
                {
                    dp.id = Guid.NewGuid().ToString("N");
                    changed = true;
                }
            }

            if (settings is LineSettings ls)
            {
                if (ls.stroke == null) { ls.stroke = new LineStrokeSettings(); changed = true; }
                if (ls.point == null) { ls.point = new PointSettings(); changed = true; }
                if (ls.area == null) { ls.area = new AreaFillSettings(); changed = true; }
            }
            else if (settings is ScatterSettings ss)
            {
                if (ss.point == null) { ss.point = new PointSettings(); changed = true; }
                if (ss.sizeMapping == null) { ss.sizeMapping = new SizeMappingSettings(); changed = true; }
                if (ss.hover == null) { ss.hover = new HoverHighlightSettings(); changed = true; }
            }
            else if (settings is BarSettings bs)
            {
                if (bs.border == null) { bs.border = new BorderSettings(); changed = true; }
                if (bs.background == null) { bs.background = new BackgroundSettings(); changed = true; }
                if (bs.hover == null) { bs.hover = new BarHoverSettings(); changed = true; }
            }
            else if (settings is PieSettings ps)
            {
                if (ps.layout == null) { ps.layout = new PieLayoutSettings(); changed = true; }
                if (ps.hover == null) { ps.hover = new PieHoverSettings(); changed = true; }
                if (ps.aggregation == null) { ps.aggregation = new PieAggregationSettings(); changed = true; }
                if (ps.ring == null) { ps.ring = new RingSettings(); changed = true; }
                if (ps.legend == null) { ps.legend = new PieLegendSettings(); changed = true; }

                if (ps.legend != null
                    && ps.legend.valueDisplayStyle == ValueDisplayStyle.None
                    && ps.legend.showValue)
                {
                    ps.legend.valueDisplayStyle = ValueDisplayStyle.Parentheses;
                    ps.legend.showValue = false;
                    changed = true;
                }
            }
            else if (settings is RadarSettings rs)
            {
                if (rs.radar == null) { rs.radar = new RadarSettings.RadarLayoutSettings(); changed = true; }
                if (rs.radar != null && rs.radar.plot == null) { rs.radar.plot = new PlotLayoutSettings(); changed = true; }
                if (rs.stroke == null) { rs.stroke = new LineStrokeSettings(); changed = true; }
                if (rs.area == null) { rs.area = new AreaFillSettings(); changed = true; }
                if (rs.point == null) { rs.point = new PointSettings(); changed = true; }
            }
            else if (settings is HeatmapSettings hs)
            {
                if (hs.cellGapPx < 0f) { hs.cellGapPx = 0f; changed = true; }
            }

            return changed;
        }

        public void EnsureSettings()
        {
            bool needCreate = settings == null;

            if (!needCreate)
            {
                switch (type)
                {
                    case SerieType.Line:
                        needCreate = !(settings is LineSettings);
                        break;
                    case SerieType.Scatter:
                        needCreate = !(settings is ScatterSettings);
                        break;
                    case SerieType.Bar:
                    case SerieType.HorizontalBar:
                        needCreate = !(settings is BarSettings);
                        break;
                    case SerieType.Pie:
                        needCreate = !(settings is PieSettings);
                        break;
                    case SerieType.RingChart:
                        needCreate = !(settings is RingChartSettings);
                        break;
                    case SerieType.Pie3D:
                        if (SerieSettingsRegistry.HasFactory(SerieType.Pie3D))
                        {
                            if (!(settings is PieSettings))
                            {
                                needCreate = true;
                            }
                            else if (SerieSettingsRegistry.TryCreate(SerieType.Pie3D, out var created3D))
                            {
                                needCreate = created3D != null && created3D.GetType() != settings.GetType();
                            }
                            else
                            {
                                needCreate = false;
                            }
                        }
                        else needCreate = false;
                        break;
                    case SerieType.Radar:
                        needCreate = !(settings is RadarSettings);
                        break;
                    case SerieType.Heatmap:
                        needCreate = !(settings is HeatmapSettings);
                        break;
                    default:
                        if (SerieSettingsRegistry.HasFactory(type))
                        {
                            if (settings == null)
                            {
                                needCreate = true;
                            }
                            else if (SerieSettingsRegistry.TryCreate(type, out var createdAny))
                            {
                                needCreate = createdAny != null && createdAny.GetType() != settings.GetType();
                            }
                            else
                            {
                                needCreate = false;
                            }
                        }
                        break;
                }
            }

            // If the type enum changed (or data was migrated) before we got a chance to cache,
            // avoid losing the previous settings instance by stashing it into the matching cache.
            if (needCreate && settings != null)
            {
                if (settings is LineSettings) _cachedLineSettings = settings;
                else if (settings is BarSettings) _cachedBarSettings = settings;
                else if (settings is RingChartSettings) _cachedRingChartSettings = settings;
                else if (settings is PieSettings) _cachedPieSettings = settings;
                else if (settings is ScatterSettings) _cachedScatterSettings = settings;
                else if (settings is RadarSettings) _cachedRadarSettings = settings;
                else if (settings is HeatmapSettings) _cachedHeatmapSettings = settings;
            }

            if (needCreate)
            {
                switch (type)
                {
                    case SerieType.Line:
                        settings = _cachedLineSettings is LineSettings cachedLine ? cachedLine : new LineSettings();
                        break;
                    case SerieType.Scatter:
                        settings = _cachedScatterSettings is ScatterSettings cachedScatter ? cachedScatter : new ScatterSettings();
                        break;
                    case SerieType.Bar:
                    case SerieType.HorizontalBar:
                        settings = _cachedBarSettings is BarSettings cachedBar ? cachedBar : new BarSettings();
                        break;
                    case SerieType.Pie:
                        settings = _cachedPieSettings is PieSettings cachedPie ? cachedPie : new PieSettings();
                        break;
                    case SerieType.RingChart:
                        settings = _cachedRingChartSettings is RingChartSettings cachedRing ? cachedRing : new RingChartSettings();
                        break;
                    case SerieType.Pie3D:
                        if (SerieSettingsRegistry.TryCreate(type, out var created3D))
                        {
                            if (created3D is PieSettings createdPie && _cachedPieSettings is PieSettings cachedPieFor3D)
                            {
                                if (cachedPieFor3D.layout != null) createdPie.layout = cachedPieFor3D.layout;
                                if (cachedPieFor3D.hover != null) createdPie.hover = cachedPieFor3D.hover;
                                if (cachedPieFor3D.aggregation != null) createdPie.aggregation = cachedPieFor3D.aggregation;
                                if (cachedPieFor3D.ring != null) createdPie.ring = cachedPieFor3D.ring;
                            }
                            settings = created3D;
                        }
                        else settings = null;
                        break;
                    case SerieType.Radar:
                        settings = _cachedRadarSettings is RadarSettings cachedRadar ? cachedRadar : new RadarSettings();
                        break;
                    case SerieType.Heatmap:
                        settings = _cachedHeatmapSettings is HeatmapSettings cachedHeatmap ? cachedHeatmap : new HeatmapSettings();
                        break;
                    default:
                        if (SerieSettingsRegistry.TryCreate(type, out var createdAny))
                        {
                            settings = createdAny;
                        }
                        else
                        {
                            settings = null;
                        }
                        break;
                }
            }

            if (type == SerieType.Scatter && settings is ScatterSettings ss)
            {
                if (ss.point == null) ss.point = new PointSettings();
                if (ss.sizeMapping == null) ss.sizeMapping = new SizeMappingSettings();
                if (ss.hover == null) ss.hover = new HoverHighlightSettings();
            }

            if (type == SerieType.Line && settings is LineSettings ls)
            {
                if (ls.stroke == null) ls.stroke = new LineStrokeSettings();
                if (ls.point == null) ls.point = new PointSettings();
                if (ls.area == null) ls.area = new AreaFillSettings();
            }

            if (type == SerieType.Bar && settings is BarSettings bs)
            {
                if (bs.border == null) bs.border = new BorderSettings();
                if (bs.background == null) bs.background = new BackgroundSettings();
            }

            if (type == SerieType.Pie && settings is PieSettings ps)
            {
                if (ps.layout == null) ps.layout = new PieLayoutSettings();
                if (ps.hover == null) ps.hover = new PieHoverSettings();
                if (ps.aggregation == null) ps.aggregation = new PieAggregationSettings();
                if (ps.ring == null) ps.ring = new RingSettings();
            }

            if (type == SerieType.RingChart && settings is RingChartSettings rcs)
            {
                if (rcs.layout == null) rcs.layout = new RingChartLayoutSettings();
                if (rcs.hover == null) rcs.hover = new PieHoverSettings();
                if (rcs.legend == null) rcs.legend = new PieLegendSettings();
            }

            if (type == SerieType.Pie3D && settings is PieSettings ps3d)
            {
                if (ps3d.layout == null) ps3d.layout = new PieLayoutSettings();
                if (ps3d.hover == null) ps3d.hover = new PieHoverSettings();
                if (ps3d.aggregation == null) ps3d.aggregation = new PieAggregationSettings();
                if (ps3d.ring == null) ps3d.ring = new RingSettings();
            }

            if (type == SerieType.Radar && settings is RadarSettings rs)
            {
                if (rs.stroke == null) rs.stroke = new LineStrokeSettings();
                if (rs.point == null) rs.point = new PointSettings();
            }

            if (type == SerieType.Heatmap && settings is HeatmapSettings hs)
            {
                if (hs.cellGapPx < 0f) hs.cellGapPx = 0f;
            }

            if (seriesData == null) seriesData = new List<SeriesData>();
        }
    }
}
