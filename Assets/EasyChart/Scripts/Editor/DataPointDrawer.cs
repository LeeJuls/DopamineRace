using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace EasyChart.Editor
{
    [CustomPropertyDrawer(typeof(SeriesData))]
    public class DataPointDrawer : PropertyDrawer
    {
        private static bool TryGetChartProfile(SerializedProperty property, out ChartProfile profile)
        {
            profile = null;
            if (property == null) return false;

            var so = property.serializedObject;
            if (so == null) return false;

            profile = so.targetObject as ChartProfile;
            return profile != null;
        }

        private static AxisConfig GetAxis(ChartProfile profile, AxisId id)
        {
            if (profile == null || profile.axes == null) return null;
            for (int i = 0; i < profile.axes.Count; i++)
            {
                var a = profile.axes[i];
                if (a != null && a.id == id) return a;
            }
            return null;
        }

        private static bool TryGetAxisCategoryLabels(SerializedProperty property, AxisId axisId, out List<string> labels)
        {
            labels = null;
            if (!TryGetChartProfile(property, out var profile)) return false;

            var axis = GetAxis(profile, axisId);
            if (axis == null) return false;
            if (axis.axisType != AxisType.Category) return false;
            if (axis.labels == null || axis.labels.Count == 0) return false;

            labels = axis.labels;
            return true;
        }

        private static bool IsAxisCategory(SerializedProperty property, AxisId axisId)
        {
            if (!TryGetChartProfile(property, out var profile)) return false;
            var axis = GetAxis(profile, axisId);
            return axis != null && axis.axisType == AxisType.Category;
        }

        private static bool TryGetAxisTypeProperty(SerializedProperty property, AxisId axisId, out SerializedProperty axisTypeProp)
        {
            axisTypeProp = null;
            if (property == null) return false;

            var so = property.serializedObject;
            if (so == null) return false;

            var axesProp = so.FindProperty("axes");
            if (axesProp == null || !axesProp.isArray) return false;

            for (int i = 0; i < axesProp.arraySize; i++)
            {
                var axisProp = axesProp.GetArrayElementAtIndex(i);
                if (axisProp == null) continue;

                var idProp = axisProp.FindPropertyRelative("id");
                if (idProp == null || idProp.propertyType != SerializedPropertyType.Enum) continue;
                if (idProp.enumValueIndex != (int)axisId) continue;

                axisTypeProp = axisProp.FindPropertyRelative("axisType");
                return axisTypeProp != null && axisTypeProp.propertyType == SerializedPropertyType.Enum;
            }

            return false;
        }

        private static int GetCategoryIndexFromFloat(float v, int count)
        {
            if (count <= 0) return 0;
            int idx = Mathf.RoundToInt(v);
            if (idx < 0) idx = 0;
            if (idx > count - 1) idx = count - 1;
            return idx;
        }

        private static List<string> BuildCategoryDisplayOptions(List<string> labels)
        {
            if (labels == null) return null;
            var opts = new List<string>(labels.Count);
            for (int i = 0; i < labels.Count; i++)
            {
                var s = labels[i];
                if (string.IsNullOrEmpty(s)) s = $"[{i}]";
                opts.Add(s);
            }
            return opts;
        }

        private static VisualElement CreateCategoryDropdown(string fieldLabel, SerializedProperty floatProp, AxisId axisId)
        {
            if (floatProp == null)
            {
                return new FloatField(fieldLabel);
            }

            // Create with placeholder choices; we will refresh from AxisConfig.labels on attach / focus / click.
            var popup = new PopupField<string>(fieldLabel, new List<string> { "" }, 0);

            void RefreshOptions()
            {
                if (floatProp.serializedObject == null) return;
                floatProp.serializedObject.Update();

                if (!TryGetAxisCategoryLabels(floatProp, axisId, out var labels) || labels == null || labels.Count == 0)
                {
                    popup.SetEnabled(true);
                    popup.choices = new List<string> { "(No Labels)" };
                    popup.SetValueWithoutNotify("(No Labels)");
                    return;
                }

                popup.SetEnabled(true);
                var options = BuildCategoryDisplayOptions(labels);
                popup.choices = options;

                int idx = GetCategoryIndexFromFloat(floatProp.floatValue, options.Count);
                if (idx < 0) idx = 0;
                if (idx >= options.Count) idx = options.Count - 1;
                popup.SetValueWithoutNotify(options[idx]);
            }

            RefreshOptions();

            // Expose refresh action to outer code (e.g. when axisType toggles and this element becomes visible).
            popup.userData = (Action)RefreshOptions;

            popup.RegisterCallback<AttachToPanelEvent>(_ => RefreshOptions());
            popup.RegisterCallback<FocusInEvent>(_ => RefreshOptions());
            popup.RegisterCallback<MouseDownEvent>(_ => RefreshOptions(), TrickleDown.TrickleDown);

            popup.RegisterValueChangedCallback(evt =>
            {
                if (floatProp.serializedObject == null) return;
                if (popup.choices == null || popup.choices.Count == 0) return;
                if (popup.choices.Count == 1 && popup.choices[0] == "(No Labels)") return;

                int idx = popup.choices.IndexOf(evt.newValue);
                if (idx < 0) idx = 0;
                floatProp.serializedObject.Update();
                floatProp.floatValue = idx;
                floatProp.serializedObject.ApplyModifiedProperties();
            });

            return popup;
        }

        private static bool TryGetDataPointIndex(SerializedProperty property, out int pointIndex)
        {
            pointIndex = -1;
            if (property == null) return false;

            var path = property.propertyPath;
            if (string.IsNullOrEmpty(path)) return false;

            const string key1 = "seriesData.Array.data[";
            const string key2 = "SeriesData.Array.data[";
            const string key3 = "dataPointsV2.Array.data[";
            const string key4 = "DataPointsV2.Array.data[";

            int i = path.LastIndexOf(key1, StringComparison.Ordinal);
            string key = key1;
            if (i < 0)
            {
                i = path.LastIndexOf(key2, StringComparison.Ordinal);
                key = key2;
            }
            if (i < 0)
            {
                i = path.LastIndexOf(key3, StringComparison.Ordinal);
                key = key3;
            }
            if (i < 0)
            {
                i = path.LastIndexOf(key4, StringComparison.Ordinal);
                key = key4;
            }
            if (i < 0) return false;

            int start = i + key.Length;
            int end = path.IndexOf(']', start);
            if (end < 0 || end <= start) return false;

            var s = path.Substring(start, end - start);
            return int.TryParse(s, out pointIndex);
        }

        private static string GetItemLabel(SerializedProperty property)
        {
            if (TryGetDataPointIndex(property, out int idx) && idx >= 0)
            {
                return $"Data Item {idx}";
            }
            return "Data Item";
        }

        private static bool TryGetSeriesIndex(SerializedProperty property, out int seriesIndex)
        {
            seriesIndex = -1;
            if (property == null) return false;

            var path = property.propertyPath;
            if (string.IsNullOrEmpty(path)) return false;

            const string key1 = "series.Array.data[";
            const string key2 = "Series.Array.data[";

            int i = path.IndexOf(key1, StringComparison.Ordinal);
            string key = key1;
            if (i < 0)
            {
                i = path.IndexOf(key2, StringComparison.Ordinal);
                key = key2;
            }
            if (i < 0) return false;

            int start = i + key.Length;
            int end = path.IndexOf(']', start);
            if (end < 0 || end <= start) return false;

            var s = path.Substring(start, end - start);
            return int.TryParse(s, out seriesIndex);
        }

        private static bool TryGetSerieType(SerializedProperty property, out SerieType serieType)
        {
            serieType = SerieType.Line;
            if (property == null) return false;

            if (!TryGetSeriesIndex(property, out int seriesIndex)) return false;

            var so = property.serializedObject;
            if (so == null) return false;

            var seriesProp = so.FindProperty("series");
            if (seriesProp == null) seriesProp = so.FindProperty("Series");
            if (seriesProp == null || !seriesProp.isArray) return false;

            if (seriesIndex < 0 || seriesIndex >= seriesProp.arraySize) return false;

            var serieProp = seriesProp.GetArrayElementAtIndex(seriesIndex);
            if (serieProp == null) return false;

            var typeProp = serieProp.FindPropertyRelative("type");
            if (typeProp == null || typeProp.propertyType != SerializedPropertyType.Enum) return false;

            // NOTE:
            // For non-contiguous enums like SerieType (with Pie3D=100), use intValue directly.
            // enumValueIndex is unreliable for non-contiguous enums.
            serieType = (SerieType)typeProp.intValue;
            return true;
        }

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var foldout = new Foldout
            {
                text = GetItemLabel(property),
                value = property != null && property.isExpanded
            };

            foldout.RegisterValueChangedCallback(evt =>
            {
                if (property == null) return;
                property.isExpanded = evt.newValue;
                property.serializedObject.ApplyModifiedProperties();
            });

            var root = new VisualElement();
            root.style.flexDirection = FlexDirection.Column;

            var xProp = property.FindPropertyRelative("x");
            var valueProp = property.FindPropertyRelative("value");
            var yProp = property.FindPropertyRelative("y");
            var zProp = property.FindPropertyRelative("z");
            var nameProp = property.FindPropertyRelative("name");
            var useColorProp = property.FindPropertyRelative("useColor");
            var colorProp = property.FindPropertyRelative("color");

            bool isPie = false;
            bool isHeatmap = false;
            bool isScatter = false;
            bool isHorizontalBar = false;
            if (TryGetSerieType(property, out var type))
            {
                isPie = type == SerieType.Pie || type == SerieType.RingChart;
                isHeatmap = type == SerieType.Heatmap;
                isScatter = type == SerieType.Scatter;
                isHorizontalBar = type == SerieType.HorizontalBar;
            }

            // For HorizontalBar: show Y (category) and Value, hide X
            // For regular Bar: show X (category) and Value, hide Y category display
            bool showX = !isPie && !isHorizontalBar;
            bool showY = true;
            bool showZ = isHeatmap || isScatter;
            bool showName = isPie;
            bool showUseColor = isPie;
            bool showColor = isPie;

            AxisId xAxisId = AxisId.XBottom;
            AxisId yAxisId = AxisId.YLeft;
            if (TryGetChartProfile(property, out var profile) && profile.cartesian != null)
            {
                xAxisId = profile.xAxisId;
                yAxisId = profile.yAxisId;
            }

            TryGetAxisTypeProperty(property, xAxisId, out var xAxisTypeProp);
            TryGetAxisTypeProperty(property, yAxisId, out var yAxisTypeProp);

            bool xIsCategory = xAxisTypeProp != null
                ? xAxisTypeProp.enumValueIndex == (int)AxisType.Category
                : IsAxisCategory(property, xAxisId);
            bool yIsCategory = yAxisTypeProp != null
                ? yAxisTypeProp.enumValueIndex == (int)AxisType.Category
                : IsAxisCategory(property, yAxisId);

            VisualElement xCategoryField = null;
            VisualElement xValueField = null;
            VisualElement yCategoryField = null;
            VisualElement yValueField = null;

            void RefreshAxisFieldVisibility()
            {
                if (xAxisTypeProp != null)
                {
                    xIsCategory = xAxisTypeProp.enumValueIndex == (int)AxisType.Category;
                }
                else
                {
                    xIsCategory = IsAxisCategory(property, xAxisId);
                }

                if (yAxisTypeProp != null)
                {
                    yIsCategory = yAxisTypeProp.enumValueIndex == (int)AxisType.Category;
                }
                else
                {
                    yIsCategory = IsAxisCategory(property, yAxisId);
                }

                if (xCategoryField != null) xCategoryField.style.display = xIsCategory ? DisplayStyle.Flex : DisplayStyle.None;
                if (xValueField != null) xValueField.style.display = xIsCategory ? DisplayStyle.None : DisplayStyle.Flex;
                if (yCategoryField != null) yCategoryField.style.display = yIsCategory ? DisplayStyle.Flex : DisplayStyle.None;
                // For HorizontalBar: always show yValueField (Value) regardless of yIsCategory
                if (yValueField != null) yValueField.style.display = (yIsCategory && !isHorizontalBar) ? DisplayStyle.None : DisplayStyle.Flex;

                // When switching to Category, force refresh + clamp current numeric value to a valid index immediately.
                if (xIsCategory && xCategoryField != null)
                {
                    if (xCategoryField.userData is Action refreshX) refreshX();

                    if (xProp != null && TryGetAxisCategoryLabels(property, xAxisId, out var labels) && labels != null && labels.Count > 0)
                    {
                        int idx = GetCategoryIndexFromFloat(xProp.floatValue, labels.Count);
                        property.serializedObject.Update();
                        xProp.floatValue = idx;
                        property.serializedObject.ApplyModifiedProperties();
                    }
                }

                if (yIsCategory && yCategoryField != null)
                {
                    if (yCategoryField.userData is Action refreshY) refreshY();

                    if (yProp != null && TryGetAxisCategoryLabels(property, yAxisId, out var labels) && labels != null && labels.Count > 0)
                    {
                        int idx = GetCategoryIndexFromFloat(yProp.floatValue, labels.Count);
                        property.serializedObject.Update();
                        yProp.floatValue = idx;
                        property.serializedObject.ApplyModifiedProperties();
                    }
                }
            }

            if (xAxisTypeProp != null) root.TrackPropertyValue(xAxisTypeProp, _ => RefreshAxisFieldVisibility());
            if (yAxisTypeProp != null) root.TrackPropertyValue(yAxisTypeProp, _ => RefreshAxisFieldVisibility());

            if (isHorizontalBar && yProp != null)
            {
                // HorizontalBar: show Y (category or value) and Value in one row
                var yvRow = new VisualElement();
                yvRow.style.flexDirection = FlexDirection.Row;

                var yWrap = new VisualElement();
                yWrap.style.flexGrow = 1f;
                yWrap.style.marginRight = 6;
                
                // Category dropdown for Y
                yCategoryField = CreateCategoryDropdown("Y", yProp, yAxisId);
                if (yCategoryField is BaseField<float> yBase)
                {
                    yBase.labelElement.style.minWidth = 14;
                    yBase.labelElement.style.maxWidth = 14;
                }
                else if (yCategoryField is BaseField<string> yBaseS)
                {
                    yBaseS.labelElement.style.minWidth = 14;
                    yBaseS.labelElement.style.maxWidth = 14;
                }
                yWrap.Add(yCategoryField);
                
                // Value field for Y (when Y axis is Value type)
                var yNumericField = new FloatField("Y");
                yNumericField.BindProperty(yProp);
                if (yNumericField.labelElement != null)
                {
                    yNumericField.labelElement.style.minWidth = 14;
                    yNumericField.labelElement.style.maxWidth = 14;
                }
                yWrap.Add(yNumericField);

                var vWrap = new VisualElement();
                vWrap.style.flexGrow = 1f;
                yValueField = new FloatField("Value");
                if (yValueField is FloatField vf && valueProp != null) vf.BindProperty(valueProp);
                if (yValueField is BaseField<float> vBase)
                {
                    vBase.labelElement.style.minWidth = 40;
                    vBase.labelElement.style.maxWidth = 40;
                    vBase.labelElement.style.flexGrow = 0;
                    vBase.labelElement.style.flexShrink = 0;
                    vBase.labelElement.style.whiteSpace = WhiteSpace.NoWrap;
                    vBase.labelElement.style.overflow = Overflow.Visible;
                    vBase.labelElement.style.textOverflow = TextOverflow.Clip;
                }
                vWrap.Add(yValueField);

                yvRow.Add(yWrap);
                yvRow.Add(vWrap);
                root.Add(yvRow);

                // Custom visibility logic for HorizontalBar
                void RefreshHorizontalBarVisibility()
                {
                    if (yAxisTypeProp != null)
                    {
                        yIsCategory = yAxisTypeProp.enumValueIndex == (int)AxisType.Category;
                    }
                    else
                    {
                        yIsCategory = IsAxisCategory(property, yAxisId);
                    }
                    
                    // Show category dropdown when Y is category, show numeric field when Y is value
                    yCategoryField.style.display = yIsCategory ? DisplayStyle.Flex : DisplayStyle.None;
                    yNumericField.style.display = yIsCategory ? DisplayStyle.None : DisplayStyle.Flex;
                    // Always show Value field for HorizontalBar
                    yValueField.style.display = DisplayStyle.Flex;
                }
                
                if (yAxisTypeProp != null) root.TrackPropertyValue(yAxisTypeProp, _ => RefreshHorizontalBarVisibility());
                RefreshHorizontalBarVisibility();
            }
            else if (showX && showY && !showZ && xProp != null && yProp != null)
            {
                var xyRow = new VisualElement();
                xyRow.style.flexDirection = FlexDirection.Row;

                var xWrap = new VisualElement();
                xWrap.style.flexGrow = 1f;
                xWrap.style.marginRight = 6;
                xCategoryField = CreateCategoryDropdown("X", xProp, xAxisId);
                xValueField = new FloatField("X");
                if (xValueField is FloatField xf) xf.BindProperty(xProp);
                if (xCategoryField is BaseField<float> xBase)
                {
                    xBase.labelElement.style.minWidth = 14;
                    xBase.labelElement.style.maxWidth = 14;
                }
                else if (xCategoryField is BaseField<string> xBaseS)
                {
                    xBaseS.labelElement.style.minWidth = 14;
                    xBaseS.labelElement.style.maxWidth = 14;
                }
                if (xValueField is BaseField<float> xBase2)
                {
                    xBase2.labelElement.style.minWidth = 14;
                    xBase2.labelElement.style.maxWidth = 14;
                }
                xWrap.Add(xCategoryField);
                xWrap.Add(xValueField);

                var yWrap = new VisualElement();
                yWrap.style.flexGrow = 1f;
                yCategoryField = CreateCategoryDropdown("Y", yProp, yAxisId);
                yValueField = new FloatField("Value");
                if (yValueField is FloatField yf && valueProp != null) yf.BindProperty(valueProp);
                if (yCategoryField is BaseField<float> yBase)
                {
                    yBase.labelElement.style.minWidth = 14;
                    yBase.labelElement.style.maxWidth = 14;
                }
                else if (yCategoryField is BaseField<string> yBaseS)
                {
                    yBaseS.labelElement.style.minWidth = 14;
                    yBaseS.labelElement.style.maxWidth = 14;
                }
                if (yValueField is BaseField<float> yBase2)
                {
                    if (string.Equals(yBase2.label, "Value", StringComparison.Ordinal))
                    {
                        yBase2.labelElement.style.minWidth = 40;
                        yBase2.labelElement.style.maxWidth = 40;
                        yBase2.labelElement.style.flexGrow = 0;
                        yBase2.labelElement.style.flexShrink = 0;
                        yBase2.labelElement.style.whiteSpace = WhiteSpace.NoWrap;
                        yBase2.labelElement.style.overflow = Overflow.Visible;
                        yBase2.labelElement.style.textOverflow = TextOverflow.Clip;
                    }
                    else
                    {
                        yBase2.labelElement.style.minWidth = 14;
                        yBase2.labelElement.style.maxWidth = 14;
                    }
                }
                yWrap.Add(yCategoryField);
                yWrap.Add(yValueField);

                xyRow.Add(xWrap);
                xyRow.Add(yWrap);
                root.Add(xyRow);

                RefreshAxisFieldVisibility();
            }
            else if (showX && showY && showZ && xProp != null && yProp != null && zProp != null)
            {
                var row = new VisualElement();
                row.style.flexDirection = FlexDirection.Row;

                var xWrap = new VisualElement();
                xWrap.style.flexGrow = 1f;
                xWrap.style.marginRight = 6;
                xCategoryField = CreateCategoryDropdown("X", xProp, xAxisId);
                xValueField = new FloatField("X");
                if (xValueField is FloatField xf) xf.BindProperty(xProp);
                if (xCategoryField is BaseField<float> xBase)
                {
                    xBase.labelElement.style.minWidth = 14;
                    xBase.labelElement.style.maxWidth = 14;
                }
                else if (xCategoryField is BaseField<string> xBaseS)
                {
                    xBaseS.labelElement.style.minWidth = 14;
                    xBaseS.labelElement.style.maxWidth = 14;
                }
                if (xValueField is BaseField<float> xBase2)
                {
                    xBase2.labelElement.style.minWidth = 14;
                    xBase2.labelElement.style.maxWidth = 14;
                }
                xWrap.Add(xCategoryField);
                xWrap.Add(xValueField);

                var yWrap = new VisualElement();
                yWrap.style.flexGrow = 1f;
                yWrap.style.marginRight = 6;
                yCategoryField = CreateCategoryDropdown("Y", yProp, yAxisId);
                yValueField = new FloatField("Y");
                if (yValueField is FloatField yf) yf.BindProperty(yProp);
                if (yCategoryField is BaseField<float> yBase)
                {
                    yBase.labelElement.style.minWidth = 14;
                    yBase.labelElement.style.maxWidth = 14;
                }
                else if (yCategoryField is BaseField<string> yBaseS)
                {
                    yBaseS.labelElement.style.minWidth = 14;
                    yBaseS.labelElement.style.maxWidth = 14;
                }
                if (yValueField is BaseField<float> yBase2)
                {
                    yBase2.labelElement.style.minWidth = 14;
                    yBase2.labelElement.style.maxWidth = 14;
                }
                yWrap.Add(yCategoryField);
                yWrap.Add(yValueField);

                var zField = new FloatField(isScatter ? "Value3" : "Value");
                if (isScatter)
                {
                    zField.BindProperty(zProp);
                }
                else
                {
                    if (valueProp != null) zField.BindProperty(valueProp);
                }
                zField.style.flexGrow = 1f;
                zField.labelElement.style.minWidth = 40f;
                zField.labelElement.style.maxWidth = 40f;
                zField.labelElement.style.flexGrow = 0;
                zField.labelElement.style.flexShrink = 0;
                zField.labelElement.style.whiteSpace = WhiteSpace.NoWrap;
                zField.labelElement.style.overflow = Overflow.Visible;
                zField.labelElement.style.textOverflow = TextOverflow.Clip;

                row.Add(xWrap);
                row.Add(yWrap);
                row.Add(zField);
                root.Add(row);

                RefreshAxisFieldVisibility();
            }
            else
            {
                if (showX && xProp != null)
                {
                    xCategoryField = CreateCategoryDropdown("X", xProp, xAxisId);
                    xValueField = new PropertyField(xProp);
                    root.Add(xCategoryField);
                    root.Add(xValueField);
                }
                if (showY && yProp != null)
                {
                    yCategoryField = CreateCategoryDropdown("Y", yProp, yAxisId);
                    yValueField = new PropertyField(valueProp != null ? valueProp : yProp);
                    root.Add(yCategoryField);
                    root.Add(yValueField);
                }
                if (showZ && valueProp != null) root.Add(new PropertyField(valueProp));

                RefreshAxisFieldVisibility();
            }

            if (showName && nameProp != null) root.Add(new PropertyField(nameProp));

            PropertyField colorField = null;

            if (showUseColor && useColorProp != null)
            {
                var useColorField = new PropertyField(useColorProp);
                root.Add(useColorField);

                if (showColor && colorProp != null)
                {
                    colorField = new PropertyField(colorProp);
                    root.Add(colorField);

                    colorField.style.display = useColorProp.boolValue ? DisplayStyle.Flex : DisplayStyle.None;
                    useColorField.RegisterCallback<SerializedPropertyChangeEvent>(_ =>
                    {
                        if (useColorProp != null && colorField != null)
                        {
                            colorField.style.display = useColorProp.boolValue ? DisplayStyle.Flex : DisplayStyle.None;
                        }
                    });
                }
            }
            else if (showColor && colorProp != null)
            {
                colorField = new PropertyField(colorProp);
                root.Add(colorField);
            }

            foldout.Add(root);
            return foldout;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property == null)
            {
                EditorGUI.LabelField(position, label.text, "(null)");
                return;
            }

            EditorGUI.BeginProperty(position, label, property);

            float line = EditorGUIUtility.singleLineHeight;
            float space = EditorGUIUtility.standardVerticalSpacing;
            var r = new Rect(position.x, position.y, position.width, line);

            var itemLabel = new GUIContent(GetItemLabel(property));
            property.isExpanded = EditorGUI.Foldout(r, property.isExpanded, itemLabel, true);
            r.y += line + space;

            if (property.isExpanded)
            {
                EditorGUI.indentLevel++;

                var xProp = property.FindPropertyRelative("x");
                var valueProp = property.FindPropertyRelative("value");
                var yProp = property.FindPropertyRelative("y");
                var zProp = property.FindPropertyRelative("z");
                var nameProp = property.FindPropertyRelative("name");
                var useColorProp = property.FindPropertyRelative("useColor");
                var colorProp = property.FindPropertyRelative("color");

                bool isPie = false;
                bool isHeatmap = false;
                bool isScatter = false;
                bool isHorizontalBar = false;
                if (TryGetSerieType(property, out var type))
                {
                    isPie = type == SerieType.Pie || type == SerieType.RingChart;
                    isHeatmap = type == SerieType.Heatmap;
                    isScatter = type == SerieType.Scatter;
                    isHorizontalBar = type == SerieType.HorizontalBar;
                }

                if (!isPie)
                {
                    AxisId xAxisId = AxisId.XBottom;
                    AxisId yAxisId = AxisId.YLeft;
                    if (TryGetChartProfile(property, out var profile) && profile != null)
                    {
                        xAxisId = profile.xAxisId;
                        yAxisId = profile.yAxisId;
                    }

                    bool xIsCategory = TryGetAxisCategoryLabels(property, xAxisId, out var xLabels);
                    bool yIsCategory = TryGetAxisCategoryLabels(property, yAxisId, out var yLabels);

                    if ((isHeatmap || isScatter) && xProp != null && yProp != null)
                    {
                        float third = (r.width - 12) / 3f;
                        var rx = new Rect(r.x, r.y, third, line);
                        var ry = new Rect(r.x + third + 6, r.y, third, line);
                        var rz = new Rect(r.x + (third + 6) * 2f, r.y, third, line);

                        float oldLabelWidth = EditorGUIUtility.labelWidth;
                        EditorGUIUtility.labelWidth = 12f;

                        if (xIsCategory && xLabels != null && xLabels.Count > 0)
                        {
                            int idx = GetCategoryIndexFromFloat(xProp.floatValue, xLabels.Count);
                            var options = BuildCategoryDisplayOptions(xLabels);
                            var rxField = EditorGUI.PrefixLabel(rx, new GUIContent("X"));
                            idx = EditorGUI.Popup(rxField, idx, options.ToArray());
                            xProp.floatValue = idx;
                        }
                        else
                        {
                            EditorGUI.PropertyField(rx, xProp, new GUIContent("X"));
                        }

                        if (yIsCategory && yLabels != null && yLabels.Count > 0)
                        {
                            int idx = GetCategoryIndexFromFloat(yProp.floatValue, yLabels.Count);
                            var options = BuildCategoryDisplayOptions(yLabels);
                            var ryField = EditorGUI.PrefixLabel(ry, new GUIContent("Y"));
                            idx = EditorGUI.Popup(ryField, idx, options.ToArray());
                            yProp.floatValue = idx;
                        }
                        else
                        {
                            EditorGUI.PropertyField(ry, yProp, new GUIContent(isScatter ? "Y" : "Y"));
                        }

                        if (isScatter && zProp != null)
                        {
                            EditorGUI.PropertyField(rz, zProp, new GUIContent("Value3"));
                        }
                        else if (valueProp != null)
                        {
                            EditorGUI.PropertyField(rz, valueProp, new GUIContent("Value"));
                        }
                        EditorGUIUtility.labelWidth = oldLabelWidth;
                        r.y += line + space;
                    }
                    else if (isHorizontalBar && yProp != null)
                    {
                        // HorizontalBar: show Y (category) and Value
                        float half = (r.width - 6) * 0.5f;
                        var ry = new Rect(r.x, r.y, half, line);
                        var rv = new Rect(r.x + half + 6, r.y, half, line);

                        float oldLabelWidth = EditorGUIUtility.labelWidth;
                        EditorGUIUtility.labelWidth = 12f;

                        if (yIsCategory && yLabels != null && yLabels.Count > 0)
                        {
                            int idx = GetCategoryIndexFromFloat(yProp.floatValue, yLabels.Count);
                            var options = BuildCategoryDisplayOptions(yLabels);
                            var ryField = EditorGUI.PrefixLabel(ry, new GUIContent("Y"));
                            idx = EditorGUI.Popup(ryField, idx, options.ToArray());
                            yProp.floatValue = idx;
                        }
                        else
                        {
                            EditorGUI.PropertyField(ry, yProp, new GUIContent("Y"));
                        }

                        EditorGUIUtility.labelWidth = 40f;
                        if (valueProp != null) EditorGUI.PropertyField(rv, valueProp, new GUIContent("Value"));
                        else EditorGUI.PropertyField(rv, yProp, new GUIContent("Value"));

                        EditorGUIUtility.labelWidth = oldLabelWidth;
                        r.y += line + space;
                    }
                    else if (xProp != null && yProp != null)
                    {
                        float half = (r.width - 6) * 0.5f;
                        var rx = new Rect(r.x, r.y, half, line);
                        var ry = new Rect(r.x + half + 6, r.y, half, line);

                        float oldLabelWidth = EditorGUIUtility.labelWidth;
                        EditorGUIUtility.labelWidth = 12f;

                        if (xIsCategory && xLabels != null && xLabels.Count > 0)
                        {
                            int idx = GetCategoryIndexFromFloat(xProp.floatValue, xLabels.Count);
                            var options = BuildCategoryDisplayOptions(xLabels);
                            var rxField = EditorGUI.PrefixLabel(rx, new GUIContent("X"));
                            idx = EditorGUI.Popup(rxField, idx, options.ToArray());
                            xProp.floatValue = idx;
                        }
                        else
                        {
                            EditorGUI.PropertyField(rx, xProp, new GUIContent("X"));
                        }

                        if (yIsCategory && yLabels != null && yLabels.Count > 0)
                        {
                            int idx = GetCategoryIndexFromFloat(yProp.floatValue, yLabels.Count);
                            var options = BuildCategoryDisplayOptions(yLabels);
                            var ryField = EditorGUI.PrefixLabel(ry, new GUIContent("Y"));
                            idx = EditorGUI.Popup(ryField, idx, options.ToArray());
                            yProp.floatValue = idx;
                        }
                        else
                        {
                            if (isScatter)
                            {
                                EditorGUI.PropertyField(ry, yProp, new GUIContent("Y"));
                            }
                            else
                            {
                                if (valueProp != null) EditorGUI.PropertyField(ry, valueProp, new GUIContent("Value"));
                                else EditorGUI.PropertyField(ry, yProp, new GUIContent("Value"));
                            }
                        }

                        EditorGUIUtility.labelWidth = oldLabelWidth;
                        r.y += line + space;
                    }
                    else
                    {
                        if (xProp != null) { EditorGUI.PropertyField(r, xProp); r.y += line + space; }
                        if (valueProp != null) { EditorGUI.PropertyField(r, valueProp, new GUIContent("Value")); r.y += line + space; }
                        else if (yProp != null) { EditorGUI.PropertyField(r, yProp, new GUIContent("Value")); r.y += line + space; }
                    }
                }
                else
                {
                    if (valueProp != null) { EditorGUI.PropertyField(r, valueProp, new GUIContent("Value")); r.y += line + space; }
                    if (nameProp != null) { EditorGUI.PropertyField(r, nameProp); r.y += line + space; }
                    if (useColorProp != null) { EditorGUI.PropertyField(r, useColorProp); r.y += line + space; }

                    if (useColorProp != null && useColorProp.boolValue && colorProp != null)
                    {
                        EditorGUI.PropertyField(r, colorProp);
                        r.y += line + space;
                    }
                }

                EditorGUI.indentLevel--;
            }

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float line = EditorGUIUtility.singleLineHeight;
            float space = EditorGUIUtility.standardVerticalSpacing;

            float h = line;
            if (property == null || !property.isExpanded) return h;

            var xProp = property.FindPropertyRelative("x");
            var valueProp = property.FindPropertyRelative("value");
            var yProp = property.FindPropertyRelative("y");
            var zProp = property.FindPropertyRelative("z");
            var nameProp = property.FindPropertyRelative("name");
            var useColorProp = property.FindPropertyRelative("useColor");
            var colorProp = property.FindPropertyRelative("color");

            bool isPie = false;
            bool isHeatmap = false;
            if (TryGetSerieType(property, out var type))
            {
                isPie = type == SerieType.Pie || type == SerieType.RingChart;
                isHeatmap = type == SerieType.Heatmap;
            }

            if (!isPie)
            {
                if (xProp != null || yProp != null) h += line + space;
                if (isHeatmap && zProp != null) { /* z is drawn on the same line as x/y */ }
            }
            else
            {
                if (valueProp != null) h += line + space;
                if (nameProp != null) h += line + space;
                if (useColorProp != null) h += line + space;
                if (useColorProp != null && useColorProp.boolValue && colorProp != null) h += line + space;
            }

            return h;
        }
    }
}
