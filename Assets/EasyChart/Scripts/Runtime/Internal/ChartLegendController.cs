using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using EasyChart.Layers;

namespace EasyChart
{
    internal sealed class ChartLegendController
    {
        private VisualElement _legendContainer;

        private ChartInteractionState _state;
        private int _legendHash;

        public IChartLegendTogglePolicy LegendTogglePolicy { get; set; }

        public ChartLegendController()
        {
            LegendTogglePolicy = DefaultChartLegendTogglePolicy.Instance;
        }

        public void SetInteractionState(ChartInteractionState state)
        {
            _state = state;
        }

        public void EnsureContainer(VisualElement host)
        {
            if (_legendContainer != null) return;
            if (host == null) return;

            _legendContainer = new VisualElement();
            _legendContainer.name = "legend";
            _legendContainer.pickingMode = PickingMode.Position;
            if (host is ChartElement chart) _legendContainer.userData = chart;
            else if (host.userData is ChartElement chartFromUserData) _legendContainer.userData = chartFromUserData;
            host.Add(_legendContainer);
        }

        public void ClearHiddenPieSliceIds()
        {
            _state?.ClearHiddenPieSliceIds();
        }

        public void RefreshLegend(
            ChartData data,
            bool trace,
            System.Action<string> pushBreadcrumb,
            System.Action<ChartDirtyReason> requestImmediate)
        {
            if (_legendContainer == null) return;

            if (data == null)
            {
                if (_legendContainer.childCount > 0) _legendContainer.Clear();
                _legendHash = 0;
                return;
            }

            PieSettings pieSettings = null;
            PieLegendSettings pieLegend = null;

            bool hasPieType = false;
            bool hasNonPieType = false;
            if (data.Series != null)
            {
                for (int i = 0; i < data.Series.Count; i++)
                {
                    var s = data.Series[i];
                    if (s == null) continue;

                    bool isPieType = s.type == SerieType.Pie || s.type == SerieType.RingChart || s.type == SerieType.Pie3D;
                    if (isPieType)
                    {
                        hasPieType = true;
                        if (pieSettings == null) pieSettings = s.settings as PieSettings;
                        if (pieLegend == null)
                        {
                            if (s.settings is PieSettings ps) pieLegend = ps.legend;
                            else if (s.settings is RingChartSettings rcs) pieLegend = rcs.legend;
                        }
                    }
                    else
                    {
                        hasNonPieType = true;
                    }
                }
            }

            bool isPurePieChart = hasPieType && !hasNonPieType;
            if (isPurePieChart && pieLegend == null)
            {
                pieLegend = pieSettings != null ? pieSettings.legend : null;
            }

            LegendSettings settings = null;
            if (isPurePieChart)
            {
                settings = CreateLegendSettings(pieLegend);
            }
            else
            {
                settings = data.legend;
            }

            if (settings == null || !settings.enabled)
            {
                if (_legendContainer.childCount > 0) _legendContainer.Clear();
                _legendHash = 0;
                return;
            }

            int nextHash = ComputeLegendHash(data);
            if (nextHash == _legendHash) return;
            _legendHash = nextHash;

            if (trace) pushBreadcrumb?.Invoke("RefreshLegend start");
            _legendContainer.Clear();
            if (trace) pushBreadcrumb?.Invoke("RefreshLegend cleared");

            _legendContainer.style.backgroundColor = settings.backgroundColor;

            _legendContainer.style.position = Position.Absolute;
            _legendContainer.style.flexDirection = (settings.position == LegendPosition.Left || settings.position == LegendPosition.Right)
                ? FlexDirection.Column
                : FlexDirection.Row;
            _legendContainer.style.flexWrap = Wrap.Wrap;
            if (settings.position == LegendPosition.Left || settings.position == LegendPosition.Right)
            {
                _legendContainer.style.alignItems = Align.FlexStart;
                _legendContainer.style.alignContent = Align.FlexStart;
            }
            else
            {
                _legendContainer.style.alignItems = Align.Center;
                _legendContainer.style.alignContent = Align.Center;
            }

            _legendContainer.style.left = StyleKeyword.Null;
            _legendContainer.style.right = StyleKeyword.Null;
            _legendContainer.style.top = StyleKeyword.Null;
            _legendContainer.style.bottom = StyleKeyword.Null;
            _legendContainer.style.translate = new Translate(new Length(0), new Length(0), 0);
            _legendContainer.style.paddingBottom = 0;
            _legendContainer.style.paddingTop = 0;
            _legendContainer.style.paddingLeft = 0;
            _legendContainer.style.paddingRight = 0;
            _legendContainer.style.marginLeft = 0;
            _legendContainer.style.marginRight = 0;
            _legendContainer.style.marginTop = 0;
            _legendContainer.style.marginBottom = 0;
            _legendContainer.style.borderLeftWidth = 0;
            _legendContainer.style.borderRightWidth = 0;
            _legendContainer.style.borderTopWidth = 0;
            _legendContainer.style.borderBottomWidth = 0;

            switch (settings.position)
            {
                case LegendPosition.Bottom:
                    _legendContainer.style.bottom = 0;
                    _legendContainer.style.left = 0;
                    _legendContainer.style.right = 0;
                    _legendContainer.style.justifyContent = Justify.Center;
                    _legendContainer.style.paddingBottom = 5;
                    break;
                case LegendPosition.Top:
                    _legendContainer.style.top = 0;
                    _legendContainer.style.left = 0;
                    _legendContainer.style.right = 0;
                    _legendContainer.style.justifyContent = Justify.Center;
                    _legendContainer.style.paddingTop = 5;
                    break;
                case LegendPosition.Left:
                    _legendContainer.style.left = 0;
                    _legendContainer.style.top = 0;
                    _legendContainer.style.bottom = 0;
                    _legendContainer.style.justifyContent = Justify.Center;
                    _legendContainer.style.paddingLeft = 5;
                    break;
                case LegendPosition.Right:
                    _legendContainer.style.right = 0;
                    _legendContainer.style.top = 0;
                    _legendContainer.style.bottom = 0;
                    _legendContainer.style.justifyContent = Justify.Center;
                    _legendContainer.style.paddingRight = 5;
                    break;
            }

            // Apply offset with position-based defaults to center legend between plot edge and canvas edge
            Vector2 effectiveOffset = settings.offset;
            if (effectiveOffset == Vector2.zero)
            {
                switch (settings.position)
                {
                    case LegendPosition.Top:
                        effectiveOffset = new Vector2(0, 30);
                        break;
                    case LegendPosition.Bottom:
                        effectiveOffset = new Vector2(0, -30);
                        break;
                    case LegendPosition.Right:
                        effectiveOffset = new Vector2(-30, 0);
                        break;
                    case LegendPosition.Left:
                        effectiveOffset = new Vector2(30, 0);
                        break;
                }
            }
            _legendContainer.style.translate = new Translate(new Length(effectiveOffset.x), new Length(-effectiveOffset.y), 0);

            bool isPieChart = isPurePieChart && (pieLegend == null || pieLegend.source != PieLegendSource.Series);
            if (isPieChart)
            {
                Serie pieSerie = null;
                for (int si = 0; si < data.Series.Count; si++)
                {
                    var s = data.Series[si];
                    if (s != null && (s.type == SerieType.Pie || s.type == SerieType.RingChart || s.type == SerieType.Pie3D) && s.seriesData != null)
                    {
                        pieSerie = s;
                        if (pieSettings == null)
                        {
                            pieSettings = s.settings as PieSettings;
                            if (pieSettings == null && s.settings is RingChartSettings rcs)
                            {
                                // RingChartSettings doesn't have aggregation, create a minimal PieSettings for legend
                                pieSettings = null;
                            }
                        }
                        break;
                    }
                }

                if (pieSerie != null && pieSerie.seriesData != null)
                {
                    // For None coordinate system (Pie/etc), only use SeriesData.name (no axis labels fallback)
                    bool isNoneCoord = data != null && data.CoordinateSystem == CoordinateSystemType.None;
                    List<string> labels = null;
                    if (!isNoneCoord)
                    {
                        if (pieLegend != null && pieLegend.source == PieLegendSource.RingSlice)
                        {
                            if (data != null && data.PolarAxes != null && data.PolarAxes.angleAxis != null)
                            {
                                labels = data.PolarAxes.angleAxis.labels;
                            }
                        }
                        else
                        {
                            if (data != null && data.Axes != null)
                            {
                                AxisId preferredAxisId = data.Cartesian != null ? data.Cartesian.xAxisId : AxisId.XBottom;
                                for (int j = 0; j < data.Axes.Count; j++)
                                {
                                    var a = data.Axes[j];
                                    if (a != null && a.id == preferredAxisId && a.axisType == AxisType.Category)
                                    {
                                        labels = a.labels;
                                        break;
                                    }
                                }

                                if (labels == null)
                                {
                                    for (int j = 0; j < data.Axes.Count; j++)
                                    {
                                        var a = data.Axes[j];
                                        if (a != null && a.axisType == AxisType.Category)
                                        {
                                            labels = a.labels;
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                    }

                    var candidates = new List<(int SourceIndex, float Value, SeriesData Data)>();
                    for (int i = 0; i < pieSerie.seriesData.Count; i++)
                    {
                        var dp = pieSerie.seriesData[i];
                        if (dp == null) continue;
                        if (dp.value <= 0) continue;
                        candidates.Add((i, dp.value, dp));
                    }

                    bool doAgg = pieSettings != null && pieSettings.aggregation != null && pieSettings.aggregation.enabled;
                    int keepTopN = doAgg ? Mathf.Max(0, pieSettings.aggregation.keepTopN) : 0;
                    bool sortByValue = pieSettings != null && pieSettings.sortByValue;

                    if (sortByValue)
                    {
                        candidates.Sort((a, b) => b.Value.CompareTo(a.Value));
                    }

                    float othersValue = 0f;
                    int limit = (doAgg && keepTopN > 0) ? Mathf.Min(keepTopN, candidates.Count) : candidates.Count;
                    int sliceIndex = 0;

                    for (int i = 0; i < candidates.Count; i++)
                    {
                        if (i < limit)
                        {
                            var c = candidates[i];

                            string name = null;
                            if (c.Data != null && !string.IsNullOrEmpty(c.Data.name)) name = c.Data.name;
                            else if (labels != null && c.SourceIndex < labels.Count) name = labels[c.SourceIndex];
                            if (string.IsNullOrEmpty(name))
                            {
                                name = (pieLegend != null && pieLegend.source == PieLegendSource.RingSlice)
                                    ? $"Ring {c.SourceIndex}"
                                    : $"Slice {c.SourceIndex}";
                            }

                            ValueDisplayStyle displayStyle = ValueDisplayStyle.None;
                            if (pieLegend != null)
                            {
                                displayStyle = pieLegend.valueDisplayStyle;
                                if (displayStyle == ValueDisplayStyle.None && pieLegend.showValue) displayStyle = ValueDisplayStyle.Parentheses;
                            }

                            string valueText = c.Value.ToString("0.##");
                            if (displayStyle == ValueDisplayStyle.Parentheses)
                            {
                                name = $"{name} ({valueText})";
                            }

                            Color color = (c.Data != null && c.Data.useColor)
                                ? c.Data.color
                                : PieSeriesRenderer.Palette[sliceIndex % PieSeriesRenderer.Palette.Length];

                            string sliceId = (c.Data != null && !string.IsNullOrEmpty(c.Data.id))
                                ? c.Data.id
                                : c.SourceIndex.ToString();
                            string capturedId = sliceId;
                            bool isVisible = _state == null || !_state.HiddenPieSliceIds.Contains(capturedId);


                            if (displayStyle == ValueDisplayStyle.AlignedColumns)
                            {
                                CreateLegendItemAlignedColumns(settings, name, valueText, color, isVisible, (visible) =>
                                {
                                    if (_state != null)
                                    {
                                        if (visible) _state.HiddenPieSliceIds.Remove(capturedId);
                                        else _state.HiddenPieSliceIds.Add(capturedId);
                                    }
                                    requestImmediate?.Invoke(ChartDirtyReason.LegendTogglePieSlice);
                                });
                            }
                            else
                            {
                                CreateLegendItem(settings, name, color, isVisible, (visible) =>
                                {
                                    if (_state != null)
                                    {
                                        if (visible) _state.HiddenPieSliceIds.Remove(capturedId);
                                        else _state.HiddenPieSliceIds.Add(capturedId);
                                    }
                                    requestImmediate?.Invoke(ChartDirtyReason.LegendTogglePieSlice);
                                });
                            }
                            sliceIndex++;
                        }
                        else
                        {
                            othersValue += candidates[i].Value;
                        }
                    }

                    if (doAgg && keepTopN > 0 && candidates.Count > keepTopN && othersValue > 0f)
                    {
                        string name = pieSettings != null && pieSettings.aggregation != null && !string.IsNullOrEmpty(pieSettings.aggregation.othersName)
                            ? pieSettings.aggregation.othersName
                            : "Others";

                        ValueDisplayStyle displayStyle = ValueDisplayStyle.None;
                        if (pieLegend != null)
                        {
                            displayStyle = pieLegend.valueDisplayStyle;
                            if (displayStyle == ValueDisplayStyle.None && pieLegend.showValue) displayStyle = ValueDisplayStyle.Parentheses;
                        }

                        string valueText = othersValue.ToString("0.##");
                        if (displayStyle == ValueDisplayStyle.Parentheses)
                        {
                            name = $"{name} ({valueText})";
                        }

                        Color color = PieSeriesRenderer.Palette[sliceIndex % PieSeriesRenderer.Palette.Length];
                        if (pieSettings != null && pieSettings.aggregation != null && pieSettings.aggregation.useOthersColor) color = pieSettings.aggregation.othersColor;

                        string capturedId = PieSeriesRenderer.OthersSliceId;
                        bool isVisible = _state == null || !_state.HiddenPieSliceIds.Contains(capturedId);

                        if (displayStyle == ValueDisplayStyle.AlignedColumns)
                        {
                            CreateLegendItemAlignedColumns(settings, name, valueText, color, isVisible, (visible) =>
                            {
                                if (_state != null)
                                {
                                    if (visible) _state.HiddenPieSliceIds.Remove(capturedId);
                                    else _state.HiddenPieSliceIds.Add(capturedId);
                                }
                                requestImmediate?.Invoke(ChartDirtyReason.LegendTogglePieSlice);
                            });
                        }
                        else
                        {
                            CreateLegendItem(settings, name, color, isVisible, (visible) =>
                            {
                                if (_state != null)
                                {
                                    if (visible) _state.HiddenPieSliceIds.Remove(capturedId);
                                    else _state.HiddenPieSliceIds.Add(capturedId);
                                }
                                requestImmediate?.Invoke(ChartDirtyReason.LegendTogglePieSlice);
                            });
                        }
                    }
                }
            }
            else
            {
                foreach (var serie in data.Series)
                {
                    Color color = Color.white;
                    if (serie.type == SerieType.Line && serie.settings is LineSettings ls)
                    {
                        if (ls.stroke == null) ls.stroke = new LineStrokeSettings();
                        color = ls.stroke.color;
                    }
                    else if ((serie.type == SerieType.Bar || serie.type == SerieType.HorizontalBar) && serie.settings is BarSettings bs) color = (bs.textureFill != null ? bs.textureFill.color : Color.white);
                    else if (serie.type == SerieType.Scatter && serie.settings is ScatterSettings ss) color = (ss.point != null && ss.point.textureFill != null ? ss.point.textureFill.color : Color.white);
                    else if (serie.type == SerieType.Radar && serie.settings is RadarSettings rs)
                    {
                        // Prefer area fill color if area is shown, otherwise use stroke color
                        if (rs.area != null && rs.area.show && rs.area.textureFill != null)
                        {
                            color = rs.area.textureFill.color;
                        }
                        else
                        {
                            if (rs.stroke == null) rs.stroke = new LineStrokeSettings();
                            color = rs.stroke.color;
                        }
                    }
                    else if ((serie.type == SerieType.Pie || serie.type == SerieType.RingChart || serie.type == SerieType.Pie3D) && serie.seriesData != null)
                    {
                        // Series-level legend for pie: pick a representative color.
                        Color picked = Color.white;
                        int pickedIndex = 0;
                        for (int i = 0; i < serie.seriesData.Count; i++)
                        {
                            var dp = serie.seriesData[i];
                            if (dp == null) continue;
                            if (dp.value <= 0f) continue;
                            pickedIndex = i;
                            picked = dp.useColor ? dp.color : PieSeriesRenderer.Palette[0];
                            break;
                        }
                        if (picked == Color.white)
                        {
                            picked = PieSeriesRenderer.Palette[pickedIndex % PieSeriesRenderer.Palette.Length];
                        }
                        color = picked;
                    }

                    CreateLegendItem(settings, serie.name, color, serie.visible, (visible) =>
                    {
                        serie.visible = visible;
                        requestImmediate?.Invoke(ChartDirtyReason.LegendToggleSeriesVisibility);
                    });
                }
            }
        }

        private static LegendSettings CreateLegendSettings(PieLegendSettings src)
        {
            if (src == null) return null;
            return new LegendSettings
            {
                enabled = src.enabled,
                position = src.position,
                fontSize = src.fontSize,
                color = src.color,
                backgroundColor = src.backgroundColor,
                itemSpacing = src.itemSpacing,
                offset = src.offset,
            };
        }

        private int ComputeLegendHash(ChartData data)
        {
            unchecked
            {
                int h = 17;

                if (data == null) return 0;
                h = h * 31 + (data.CoordinateSystem.GetHashCode());

                PieLegendSettings pieLegend = null;

                bool hasPieType = false;
                bool hasNonPieType = false;
                if (data.Series != null)
                {
                    for (int i = 0; i < data.Series.Count; i++)
                    {
                        var s = data.Series[i];
                        if (s == null) continue;

                        bool isPieType = s.type == SerieType.Pie || s.type == SerieType.RingChart || s.type == SerieType.Pie3D;
                        if (isPieType)
                        {
                            hasPieType = true;
                            if (pieLegend == null)
                            {
                                if (s.settings is PieSettings ps) pieLegend = ps.legend;
                                else if (s.settings is RingChartSettings rcs) pieLegend = rcs.legend;
                            }
                        }
                        else
                        {
                            hasNonPieType = true;
                        }
                    }
                }

                bool isPurePieChart = hasPieType && !hasNonPieType;

                LegendSettings settings = null;
                if (isPurePieChart)
                {
                    settings = CreateLegendSettings(pieLegend);
                }
                else
                {
                    settings = data.legend;
                }

                if (settings == null) return h;
                h = h * 31 + (settings.enabled ? 1 : 0);
                h = h * 31 + settings.position.GetHashCode();
                h = h * 31 + settings.offset.GetHashCode();
                h = h * 31 + settings.backgroundColor.GetHashCode();
                h = h * 31 + settings.itemSpacing.GetHashCode();

                bool isPieChart = isPurePieChart && (pieLegend == null || pieLegend.source != PieLegendSource.Series);
                h = h * 31 + (isPieChart ? 1 : 0);

                if (pieLegend != null)
                {
                    h = h * 31 + pieLegend.source.GetHashCode();
                    h = h * 31 + (pieLegend.enabled ? 1 : 0);
                    h = h * 31 + (pieLegend.showValue ? 1 : 0);
                    h = h * 31 + pieLegend.valueDisplayStyle.GetHashCode();
                    h = h * 31 + pieLegend.position.GetHashCode();
                    h = h * 31 + pieLegend.offset.GetHashCode();
                    h = h * 31 + pieLegend.backgroundColor.GetHashCode();
                    h = h * 31 + pieLegend.itemSpacing.GetHashCode();
                }

                int hiddenHash = 0;
                if (_state != null)
                {
                    foreach (var id in _state.HiddenPieSliceIds)
                    {
                        if (id == null) continue;
                        hiddenHash ^= id.GetHashCode();
                    }
                }
                h = h * 31 + hiddenHash;

                if (data.Series == null) return h;

                if (isPieChart)
                {
                    Serie pieSerie = null;
                    PieSettings pieSettings = null;
                    for (int si = 0; si < data.Series.Count; si++)
                    {
                        var s = data.Series[si];
                        if (s != null && s.visible && (s.type == SerieType.Pie || s.type == SerieType.RingChart || s.type == SerieType.Pie3D) && s.seriesData != null)
                        {
                            pieSerie = s;
                            pieSettings = s.settings as PieSettings;
                            break;
                        }
                    }

                    if (pieSerie != null)
                    {
                        h = h * 31 + pieSerie.seriesData.Count;
                        for (int i = 0; i < pieSerie.seriesData.Count; i++)
                        {
                            var dp = pieSerie.seriesData[i];
                            if (dp == null) continue;
                            h = h * 31 + (dp.id != null ? dp.id.GetHashCode() : i);
                            h = h * 31 + (dp.name != null ? dp.name.GetHashCode() : 0);
                            h = h * 31 + (dp.useColor ? dp.color.GetHashCode() : 0);
                            h = h * 31 + dp.value.GetHashCode();
                        }
                    }

                    if (pieSettings != null)
                    {
                        h = h * 31 + (pieSettings.sortByValue ? 1 : 0);
                        if (pieSettings.aggregation != null)
                        {
                            h = h * 31 + (pieSettings.aggregation.enabled ? 1 : 0);
                            h = h * 31 + pieSettings.aggregation.keepTopN;
                            h = h * 31 + (pieSettings.aggregation.othersName != null ? pieSettings.aggregation.othersName.GetHashCode() : 0);
                            h = h * 31 + (pieSettings.aggregation.useOthersColor ? 1 : 0);
                            h = h * 31 + pieSettings.aggregation.othersColor.GetHashCode();
                        }
                    }
                }
                else
                {
                    for (int i = 0; i < data.Series.Count; i++)
                    {
                        var serie = data.Series[i];
                        if (serie == null) continue;

                        h = h * 31 + serie.type.GetHashCode();
                        h = h * 31 + (serie.name != null ? serie.name.GetHashCode() : 0);
                        h = h * 31 + (serie.visible ? 1 : 0);

                        Color color = Color.white;
                        if (serie.type == SerieType.Line && serie.settings is LineSettings ls)
                        {
                            if (ls.stroke == null) ls.stroke = new LineStrokeSettings();
                            color = ls.stroke.color;
                        }
                        else if (serie.type == SerieType.Bar && serie.settings is BarSettings bs)
                        {
                            color = (bs.textureFill != null ? bs.textureFill.color : Color.white);
                        }
                        else if (serie.type == SerieType.Scatter && serie.settings is ScatterSettings ss)
                        {
                            color = (ss.point != null && ss.point.textureFill != null ? ss.point.textureFill.color : Color.white);
                        }
                        else if (serie.type == SerieType.Radar && serie.settings is RadarSettings rs)
                        {
                            if (rs.stroke == null) rs.stroke = new LineStrokeSettings();
                            color = rs.stroke.color;
                        }
                        else if ((serie.type == SerieType.Pie || serie.type == SerieType.RingChart) && serie.seriesData != null)
                        {
                            // Keep series-level legend hash stable for pie.
                            int pickedIndex = 0;
                            Color picked = Color.white;
                            for (int j = 0; j < serie.seriesData.Count; j++)
                            {
                                var dp = serie.seriesData[j];
                                if (dp == null) continue;
                                if (dp.value <= 0f) continue;
                                pickedIndex = j;
                                picked = dp.useColor ? dp.color : PieSeriesRenderer.Palette[0];
                                break;
                            }
                            if (picked == Color.white)
                            {
                                picked = PieSeriesRenderer.Palette[pickedIndex % PieSeriesRenderer.Palette.Length];
                            }
                            color = picked;
                        }
                        h = h * 31 + color.GetHashCode();
                    }
                }

                return h;
            }
        }

        private static void ApplyLegendElementBaseStyle(VisualElement ve)
        {
            ve.style.marginLeft = 0;
            ve.style.marginRight = 0;
            ve.style.marginTop = 0;
            ve.style.marginBottom = 0;

            ve.style.paddingLeft = 0;
            ve.style.paddingRight = 0;
            ve.style.paddingTop = 0;
            ve.style.paddingBottom = 0;

            ve.style.borderLeftWidth = 0;
            ve.style.borderRightWidth = 0;
            ve.style.borderTopWidth = 0;
            ve.style.borderBottomWidth = 0;

            ve.style.flexGrow = 0;
            ve.style.flexShrink = 0;
        }

        private static void ApplyLegendLabelBaseStyle(Label label)
        {
            ApplyLegendElementBaseStyle(label);
            label.style.whiteSpace = WhiteSpace.NoWrap;
        }

        private void CreateLegendItem(string name, Color color, bool isVisible, System.Action<bool> onToggle)
        {
            CreateLegendItem(null, name, color, isVisible, onToggle);
        }

        private void CreateLegendItem(LegendSettings settings, string name, Color color, bool isVisible, System.Action<bool> onToggle)
        {
            if (_legendContainer == null) return;

            var item = new VisualElement();
            ApplyLegendElementBaseStyle(item);
            item.style.flexDirection = FlexDirection.Row;
            item.style.alignItems = Align.Center;

            if (settings != null)
            {
                bool vertical = settings.position == LegendPosition.Left || settings.position == LegendPosition.Right;
                item.style.marginRight = vertical ? 0 : settings.itemSpacing;
                item.style.marginBottom = settings.itemSpacing;
                item.style.paddingLeft = 2;
                item.style.paddingRight = 2;
                item.style.paddingTop = 1;
                item.style.paddingBottom = 1;
            }

            // Icon
            var icon = new VisualElement();
            ApplyLegendElementBaseStyle(icon);
            icon.style.width = 10;
            icon.style.height = 10;
            icon.style.backgroundColor = color;
            item.Add(icon);

            // Label
            var label = new Label(name);
            ApplyLegendLabelBaseStyle(label);
            if (settings != null)
            {
                if (settings.fontSize > 0) label.style.fontSize = settings.fontSize;
                else label.style.fontSize = StyleKeyword.Null;
                label.style.color = settings.color;
            }
            label.style.marginLeft = 5;
            label.style.unityTextAlign = TextAnchor.MiddleLeft;

            ChartTextStyleApplier.ApplyLabel(label, _legendContainer, ChartTextRole.Legend);
            item.Add(label);

            item.RegisterCallback<PointerDownEvent>(evt =>
            {
                var policy = LegendTogglePolicy ?? DefaultChartLegendTogglePolicy.Instance;
                bool newState = policy.GetNextVisibleState(isVisible, evt);
                onToggle?.Invoke(newState);
            });

            if (!isVisible)
            {
                item.style.opacity = 0.5f;
            }

            _legendContainer.Add(item);
        }

        private void CreateLegendItemAlignedColumns(LegendSettings settings, string name, string value, Color color, bool isVisible, System.Action<bool> onToggle)
        {
            if (_legendContainer == null) return;

            var item = new VisualElement();
            ApplyLegendElementBaseStyle(item);
            item.style.flexDirection = FlexDirection.Row;
            item.style.alignItems = Align.Center;

            if (settings != null)
            {
                bool vertical = settings.position == LegendPosition.Left || settings.position == LegendPosition.Right;
                item.style.marginRight = vertical ? 0 : settings.itemSpacing;
                item.style.marginBottom = settings.itemSpacing;
                item.style.paddingLeft = 2;
                item.style.paddingRight = 2;
                item.style.paddingTop = 1;
                item.style.paddingBottom = 1;
            }

            // Icon
            var icon = new VisualElement();
            ApplyLegendElementBaseStyle(icon);
            icon.style.width = 10;
            icon.style.height = 10;
            icon.style.backgroundColor = color;
            item.Add(icon);

            // Labels container
            var labels = new VisualElement();
            ApplyLegendElementBaseStyle(labels);
            labels.style.flexDirection = FlexDirection.Row;
            labels.style.alignItems = Align.Center;
            labels.style.flexGrow = 1f;
            labels.style.flexShrink = 1f;
            labels.style.minWidth = 0;
            labels.style.marginLeft = 5;
            item.Add(labels);

            var nameLabel = new Label(name);
            ApplyLegendLabelBaseStyle(nameLabel);
            nameLabel.style.flexGrow = 1f;
            nameLabel.style.flexShrink = 1f;
            nameLabel.style.minWidth = 0;
            nameLabel.style.unityTextAlign = TextAnchor.MiddleLeft;

            var valueLabel = new Label(value);
            ApplyLegendLabelBaseStyle(valueLabel);
            valueLabel.style.flexGrow = 0f;
            valueLabel.style.flexShrink = 0f;
            valueLabel.style.unityTextAlign = TextAnchor.MiddleRight;
            valueLabel.style.marginLeft = 10;

            if (settings != null)
            {
                if (settings.fontSize > 0)
                {
                    nameLabel.style.fontSize = settings.fontSize;
                    valueLabel.style.fontSize = settings.fontSize;
                }
                else
                {
                    nameLabel.style.fontSize = StyleKeyword.Null;
                    valueLabel.style.fontSize = StyleKeyword.Null;
                }
                nameLabel.style.color = settings.color;
                valueLabel.style.color = settings.color;
            }

            ChartTextStyleApplier.ApplyLabel(nameLabel, _legendContainer, ChartTextRole.Legend);
            ChartTextStyleApplier.ApplyLabel(valueLabel, _legendContainer, ChartTextRole.Legend);

            labels.Add(nameLabel);
            labels.Add(valueLabel);

            item.RegisterCallback<PointerDownEvent>(evt =>
            {
                var policy = LegendTogglePolicy ?? DefaultChartLegendTogglePolicy.Instance;
                bool newState = policy.GetNextVisibleState(isVisible, evt);
                onToggle?.Invoke(newState);
            });

            if (!isVisible)
            {
                item.style.opacity = 0.5f;
            }

            _legendContainer.Add(item);
        }
    }
}
