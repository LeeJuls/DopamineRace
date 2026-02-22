using UnityEditor;
using UnityEngine;

namespace EasyChart.Editor
{
    [CustomPropertyDrawer(typeof(AxisConfig))]
    public class AxisConfigDrawer : PropertyDrawer
    {
        private static readonly string[] _labelFormatOptions =
        {
            "None",
            "F0",
            "F1",
            "F2",
            "N0",
            "N2",
            "P0",
            "E2",
            "Custom"
        };

        private static int ResolveLabelFormatIndex(string fmt, out bool isCustom)
        {
            if (string.IsNullOrEmpty(fmt))
            {
                isCustom = false;
                return 0;
            }

            for (int i = 1; i < _labelFormatOptions.Length - 1; i++)
            {
                if (_labelFormatOptions[i] == fmt)
                {
                    isCustom = false;
                    return i;
                }
            }

            isCustom = true;
            return _labelFormatOptions.Length - 1;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            float line = EditorGUIUtility.singleLineHeight;
            float space = EditorGUIUtility.standardVerticalSpacing;

            // Get all properties
            var idProp = property.FindPropertyRelative("id");
            var axisTypeProp = property.FindPropertyRelative("axisType");

            var visibleProp = property.FindPropertyRelative("visible");
            var colorProp = property.FindPropertyRelative("color");
            var widthProp = property.FindPropertyRelative("width");

            var labelStyleProp = property.FindPropertyRelative("labelStyle");
            var labelPlacementProp = property.FindPropertyRelative("labelPlacement");
            var labelsProp = property.FindPropertyRelative("labels");
            var labelFormatProp = property.FindPropertyRelative("labelFormat");

            var autoRangeMinProp = property.FindPropertyRelative("autoRangeMin");
            var autoRangeMaxProp = property.FindPropertyRelative("autoRangeMax");
            var autoRangeRoundingProp = property.FindPropertyRelative("autoRangeRounding");
            var autoRangeUnitProp = property.FindPropertyRelative("autoRangeUnit");
            var minValueProp = property.FindPropertyRelative("minValue");
            var maxValueProp = property.FindPropertyRelative("maxValue");

            var autoTicksProp = property.FindPropertyRelative("autoTicks");
            var splitCountProp = property.FindPropertyRelative("splitCount");

            var categoryAutoScrollProp = property.FindPropertyRelative("categoryAutoScroll");
            var categorySmoothScrollProp = property.FindPropertyRelative("categorySmoothScroll");
            var categoryScrollIntervalProp = property.FindPropertyRelative("categoryScrollInterval");
            var categoryScrollStepProp = property.FindPropertyRelative("categoryScrollStep");

            var showUnitProp = property.FindPropertyRelative("showUnit");
            var unitTextProp = property.FindPropertyRelative("unitText");
            var unitLabelStyleProp = property.FindPropertyRelative("unitLabelStyle");

            Rect r = new Rect(position.x, position.y, position.width, line);

            property.isExpanded = EditorGUI.Foldout(r, property.isExpanded, label, true);
            r.y += line + space;

            if (property.isExpanded)
            {
                EditorGUI.indentLevel++;

                bool isCategoryAxis = axisTypeProp != null && axisTypeProp.enumValueIndex == (int)AxisType.Category;
                bool isValueAxis = axisTypeProp == null || axisTypeProp.enumValueIndex == (int)AxisType.Value;

                // ===== Section 1: Basic Info =====
                EditorGUI.BeginDisabledGroup(true);
                if (idProp != null) { EditorGUI.PropertyField(r, idProp); r.y += line + space; }
                EditorGUI.EndDisabledGroup();
                if (axisTypeProp != null) { EditorGUI.PropertyField(r, axisTypeProp); r.y += line + space; }
                if (visibleProp != null) { EditorGUI.PropertyField(r, visibleProp); r.y += line + space; }

                // ===== Section 2: Axis Line Style =====
                r.y += space; // Add separator
                EditorGUI.LabelField(r, "Axis Line", EditorStyles.boldLabel);
                r.y += line + space;
                if (colorProp != null) { EditorGUI.PropertyField(r, colorProp); r.y += line + space; }
                if (widthProp != null) { EditorGUI.PropertyField(r, widthProp); r.y += line + space; }

                // ===== Section 3: Range Settings (Value axis only) =====
                if (isValueAxis)
                {
                    r.y += space;
                    EditorGUI.LabelField(r, "Range", EditorStyles.boldLabel);
                    r.y += line + space;

                    if (autoRangeMinProp != null) { EditorGUI.PropertyField(r, autoRangeMinProp); r.y += line + space; }
                    if (autoRangeMinProp != null && !autoRangeMinProp.boolValue)
                    {
                        EditorGUI.indentLevel++;
                        if (minValueProp != null) { EditorGUI.PropertyField(r, minValueProp); r.y += line + space; }
                        EditorGUI.indentLevel--;
                    }

                    if (autoRangeMaxProp != null) { EditorGUI.PropertyField(r, autoRangeMaxProp); r.y += line + space; }
                    if (autoRangeMaxProp != null && !autoRangeMaxProp.boolValue)
                    {
                        EditorGUI.indentLevel++;
                        if (maxValueProp != null) { EditorGUI.PropertyField(r, maxValueProp); r.y += line + space; }
                        EditorGUI.indentLevel--;
                    }

                    if (autoRangeRoundingProp != null) { EditorGUI.PropertyField(r, autoRangeRoundingProp); r.y += line + space; }
                    if (autoRangeRoundingProp != null && autoRangeUnitProp != null)
                    {
                        if (autoRangeRoundingProp.enumValueIndex == 4)
                        {
                            EditorGUI.indentLevel++;
                            EditorGUI.PropertyField(r, autoRangeUnitProp);
                            r.y += line + space;
                            EditorGUI.indentLevel--;
                        }
                    }
                }

                // ===== Section 4: Ticks/Split Settings =====
                r.y += space;
                EditorGUI.LabelField(r, "Ticks", EditorStyles.boldLabel);
                r.y += line + space;

                if (autoTicksProp != null) { EditorGUI.PropertyField(r, autoTicksProp); r.y += line + space; }
                if (autoTicksProp != null && !autoTicksProp.boolValue)
                {
                    EditorGUI.indentLevel++;
                    if (splitCountProp != null) 
                    { 
                        var splitLabel = isCategoryAxis ? new GUIContent("VisibleCount") : new GUIContent("Split Count");
                        EditorGUI.PropertyField(r, splitCountProp, splitLabel); 
                        r.y += line + space; 
                    }
                    EditorGUI.indentLevel--;
                }

                // ===== Section 5: Label Settings =====
                r.y += space;
                EditorGUI.LabelField(r, "Labels", EditorStyles.boldLabel);
                r.y += line + space;

                if (labelStyleProp != null) { EditorGUI.PropertyField(r, labelStyleProp, true); r.y += EditorGUI.GetPropertyHeight(labelStyleProp, true) + space; }
                if (labelPlacementProp != null) { EditorGUI.PropertyField(r, labelPlacementProp); r.y += line + space; }

                if (labelFormatProp != null)
                {
                    string current = labelFormatProp.stringValue;
                    bool custom;
                    int index = ResolveLabelFormatIndex(current, out custom);

                    var popupRect = EditorGUI.PrefixLabel(r, new GUIContent("LabelFormat"));
                    int newIndex = EditorGUI.Popup(popupRect, index, _labelFormatOptions);
                    r.y += line + space;

                    int customIndex = _labelFormatOptions.Length - 1;
                    if (newIndex != index)
                    {
                        if (newIndex == 0)
                        {
                            labelFormatProp.stringValue = string.Empty;
                            custom = false;
                        }
                        else if (newIndex == customIndex)
                        {
                            if (!custom) labelFormatProp.stringValue = string.Empty;
                            custom = true;
                        }
                        else
                        {
                            labelFormatProp.stringValue = _labelFormatOptions[newIndex];
                            custom = false;
                        }
                    }

                    if (newIndex == customIndex)
                    {
                        EditorGUI.indentLevel++;
                        string v = labelFormatProp.stringValue;
                        if (v == null) v = string.Empty;
                        string nv = EditorGUI.TextField(r, new GUIContent("Format"), v);
                        if (nv != v) labelFormatProp.stringValue = nv;
                        r.y += line + space;
                        EditorGUI.indentLevel--;
                    }
                }

                if (isCategoryAxis && labelsProp != null)
                {
                    float h = EditorGUI.GetPropertyHeight(labelsProp, true);
                    EditorGUI.PropertyField(new Rect(r.x, r.y, r.width, h), labelsProp, new GUIContent("Category Labels"), true);
                    r.y += h + space;
                }

                // ===== Section 6: Unit Settings (Value axis only) =====
                if (isValueAxis && showUnitProp != null)
                {
                    r.y += space;
                    EditorGUI.LabelField(r, "Unit", EditorStyles.boldLabel);
                    r.y += line + space;

                    EditorGUI.PropertyField(r, showUnitProp);
                    r.y += line + space;
                    if (showUnitProp.boolValue)
                    {
                        EditorGUI.indentLevel++;
                        if (unitTextProp != null) { EditorGUI.PropertyField(r, unitTextProp); r.y += line + space; }
                        if (unitLabelStyleProp != null)
                        {
                            float uh = EditorGUI.GetPropertyHeight(unitLabelStyleProp, true);
                            EditorGUI.PropertyField(new Rect(r.x, r.y, r.width, uh), unitLabelStyleProp, true);
                            r.y += uh + space;
                        }
                        EditorGUI.indentLevel--;
                    }
                }

                // ===== Section 7: Category Scroll Settings (Category axis only) =====
                if (isCategoryAxis && categoryAutoScrollProp != null)
                {
                    r.y += space;
                    EditorGUI.LabelField(r, "Auto Scroll", EditorStyles.boldLabel);
                    r.y += line + space;

                    EditorGUI.PropertyField(r, categoryAutoScrollProp);
                    r.y += line + space;
                    if (categoryAutoScrollProp.boolValue)
                    {
                        EditorGUI.indentLevel++;
                        if (categorySmoothScrollProp != null) { EditorGUI.PropertyField(r, categorySmoothScrollProp); r.y += line + space; }
                        if (categoryScrollIntervalProp != null) { EditorGUI.PropertyField(r, categoryScrollIntervalProp); r.y += line + space; }
                        if (categoryScrollStepProp != null) { EditorGUI.PropertyField(r, categoryScrollStepProp); r.y += line + space; }
                        EditorGUI.indentLevel--;
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

            float h = line; // Foldout
            if (!property.isExpanded) return h;

            var axisTypeProp = property.FindPropertyRelative("axisType");
            var labelsProp = property.FindPropertyRelative("labels");
            var labelStyleProp = property.FindPropertyRelative("labelStyle");
            var labelFormatProp = property.FindPropertyRelative("labelFormat");
            var autoRangeMinProp = property.FindPropertyRelative("autoRangeMin");
            var autoRangeMaxProp = property.FindPropertyRelative("autoRangeMax");
            var autoRangeRoundingProp = property.FindPropertyRelative("autoRangeRounding");
            var autoRangeUnitProp = property.FindPropertyRelative("autoRangeUnit");
            var autoTicksProp = property.FindPropertyRelative("autoTicks");
            var categoryAutoScrollProp = property.FindPropertyRelative("categoryAutoScroll");
            var showUnitProp = property.FindPropertyRelative("showUnit");
            var unitLabelStyleProp = property.FindPropertyRelative("unitLabelStyle");

            bool isCategoryAxis = axisTypeProp != null && axisTypeProp.enumValueIndex == (int)AxisType.Category;
            bool isValueAxis = axisTypeProp == null || axisTypeProp.enumValueIndex == (int)AxisType.Value;

            // Section 1: Basic Info
            h += line + space; // id
            h += line + space; // axisType
            h += line + space; // visible

            // Section 2: Axis Line (header + fields)
            h += space + line + space; // header
            h += line + space; // color
            h += line + space; // width

            // Section 3: Range (Value axis only)
            if (isValueAxis)
            {
                h += space + line + space; // header
                if (autoRangeMinProp != null)
                {
                    h += line + space; // autoRangeMin
                    if (!autoRangeMinProp.boolValue) h += line + space; // minValue
                }
                if (autoRangeMaxProp != null)
                {
                    h += line + space; // autoRangeMax
                    if (!autoRangeMaxProp.boolValue) h += line + space; // maxValue
                }
                if (autoRangeRoundingProp != null) h += line + space;
                if (autoRangeRoundingProp != null && autoRangeUnitProp != null && autoRangeRoundingProp.enumValueIndex == 4)
                {
                    h += line + space; // autoRangeUnit
                }
            }

            // Section 4: Ticks (header + fields)
            h += space + line + space; // header
            if (autoTicksProp != null) h += line + space; // autoTicks
            if (autoTicksProp != null && !autoTicksProp.boolValue) h += line + space; // splitCount/VisibleCount

            // Section 5: Labels (header + fields)
            h += space + line + space; // header
            if (labelStyleProp != null) h += EditorGUI.GetPropertyHeight(labelStyleProp, true) + space;
            h += line + space; // labelPlacement
            h += line + space; // labelFormat
            if (labelFormatProp != null)
            {
                bool custom;
                int idx = ResolveLabelFormatIndex(labelFormatProp.stringValue, out custom);
                if (idx == _labelFormatOptions.Length - 1) h += line + space; // custom format field
            }
            if (isCategoryAxis && labelsProp != null)
            {
                h += EditorGUI.GetPropertyHeight(labelsProp, true) + space; // Category Labels
            }

            // Section 6: Unit (Value axis only)
            if (isValueAxis && showUnitProp != null)
            {
                h += space + line + space; // header
                h += line + space; // showUnit
                if (showUnitProp.boolValue)
                {
                    h += line + space; // unitText
                    if (unitLabelStyleProp != null) h += EditorGUI.GetPropertyHeight(unitLabelStyleProp, true) + space;
                }
            }

            // Section 7: Auto Scroll (Category axis only)
            if (isCategoryAxis && categoryAutoScrollProp != null)
            {
                h += space + line + space; // header
                h += line + space; // categoryAutoScroll
                if (categoryAutoScrollProp.boolValue)
                {
                    h += line + space; // categorySmoothScroll
                    h += line + space; // categoryScrollInterval
                    h += line + space; // categoryScrollStep
                }
            }

            return h;
        }
    }
}
