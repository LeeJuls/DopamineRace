using UnityEngine;
using UnityEditor;
using UnityEngine.TextCore.Text;

namespace EasyChart.Editor
{
    [CustomEditor(typeof(ChartTheme))]
    public class ChartThemeEditor : UnityEditor.Editor
    {
        private static readonly Color BoxBorderColor = new Color(0.1f, 0.1f, 0.1f);
        private static readonly Color BoxBackgroundColor = new Color(0.18f, 0.18f, 0.18f);

        private bool _fontSettingsExpanded = true;
        private bool _fontSizeExpanded = true;
        private bool _colorsExpanded = true;
        private bool _panelExpanded = true;
        private bool _plotAreaExpanded = true;

        private SerializedProperty _primaryFont;
        private SerializedProperty _monoFont;
        private SerializedProperty _fontScale;

        private SerializedProperty _axisFontSize;
        private SerializedProperty _legendFontSize;
        private SerializedProperty _tooltipFontSize;
        private SerializedProperty _seriesLabelFontSize;
        private SerializedProperty _titleFontSize;
        private SerializedProperty _subtitleFontSize;

        private SerializedProperty _seriesColors;
        private SerializedProperty _paletteSeed;
        private SerializedProperty _positiveColor;
        private SerializedProperty _negativeColor;
        private SerializedProperty _neutralColor;
        private SerializedProperty _disabledAlpha;

        private SerializedProperty _backgroundColor;
        private SerializedProperty _panelPadding;
        private SerializedProperty _panelRadius;
        private SerializedProperty _plotBackgroundColor;
        private SerializedProperty _plotBorderColor;
        private SerializedProperty _plotBorderWidth;

        private void OnEnable()
        {
            _primaryFont = serializedObject.FindProperty("primaryFont");
            _monoFont = serializedObject.FindProperty("monoFont");
            _fontScale = serializedObject.FindProperty("fontScale");

            _axisFontSize = serializedObject.FindProperty("axisFontSize");
            _legendFontSize = serializedObject.FindProperty("legendFontSize");
            _tooltipFontSize = serializedObject.FindProperty("tooltipFontSize");
            _seriesLabelFontSize = serializedObject.FindProperty("seriesLabelFontSize");
            _titleFontSize = serializedObject.FindProperty("titleFontSize");
            _subtitleFontSize = serializedObject.FindProperty("subtitleFontSize");

            _seriesColors = serializedObject.FindProperty("seriesColors");
            _paletteSeed = serializedObject.FindProperty("paletteSeed");
            _positiveColor = serializedObject.FindProperty("positiveColor");
            _negativeColor = serializedObject.FindProperty("negativeColor");
            _neutralColor = serializedObject.FindProperty("neutralColor");
            _disabledAlpha = serializedObject.FindProperty("disabledAlpha");

            _backgroundColor = serializedObject.FindProperty("backgroundColor");
            _panelPadding = serializedObject.FindProperty("panelPadding");
            _panelRadius = serializedObject.FindProperty("panelRadius");
            _plotBackgroundColor = serializedObject.FindProperty("plotBackgroundColor");
            _plotBorderColor = serializedObject.FindProperty("plotBorderColor");
            _plotBorderWidth = serializedObject.FindProperty("plotBorderWidth");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // Font Settings
            DrawFoldoutSection("Font Settings", ref _fontSettingsExpanded, () =>
            {
                // Use ObjectField with FontAsset (SDF) type filter
                EditorGUI.BeginChangeCheck();
                var primaryFont = EditorGUILayout.ObjectField(
                    new GUIContent("Primary Font", "Main SDF font for chart text"),
                    _primaryFont.objectReferenceValue,
                    typeof(FontAsset),
                    false);
                if (EditorGUI.EndChangeCheck())
                {
                    _primaryFont.objectReferenceValue = primaryFont;
                }

                EditorGUI.BeginChangeCheck();
                var monoFont = EditorGUILayout.ObjectField(
                    new GUIContent("Mono Font", "Monospace SDF font for numeric values"),
                    _monoFont.objectReferenceValue,
                    typeof(FontAsset),
                    false);
                if (EditorGUI.EndChangeCheck())
                {
                    _monoFont.objectReferenceValue = monoFont;
                }

                EditorGUILayout.PropertyField(_fontScale, new GUIContent("Font Scale"));
            });

            // Font Size Settings
            DrawFoldoutSection("Font Size", ref _fontSizeExpanded, () =>
            {
                EditorGUILayout.PropertyField(_titleFontSize, new GUIContent("Title"));
                EditorGUILayout.PropertyField(_subtitleFontSize, new GUIContent("Subtitle"));
                EditorGUILayout.PropertyField(_axisFontSize, new GUIContent("Axis"));
                EditorGUILayout.PropertyField(_legendFontSize, new GUIContent("Legend"));
                EditorGUILayout.PropertyField(_tooltipFontSize, new GUIContent("Tooltip"));
                EditorGUILayout.PropertyField(_seriesLabelFontSize, new GUIContent("Series Label"));
            });

            // Color Settings
            DrawFoldoutSection("Colors", ref _colorsExpanded, () =>
            {
                EditorGUILayout.PropertyField(_seriesColors, new GUIContent("Series Colors"), true);
                EditorGUILayout.PropertyField(_paletteSeed, new GUIContent("Palette Seed"));
                EditorGUILayout.Space(4);
                EditorGUILayout.PropertyField(_positiveColor, new GUIContent("Positive"));
                EditorGUILayout.PropertyField(_negativeColor, new GUIContent("Negative"));
                EditorGUILayout.PropertyField(_neutralColor, new GUIContent("Neutral"));
                EditorGUILayout.PropertyField(_disabledAlpha, new GUIContent("Disabled Alpha"));
            });

            // Panel Settings
            DrawFoldoutSection("Panel", ref _panelExpanded, () =>
            {
                EditorGUILayout.PropertyField(_backgroundColor, new GUIContent("Background Color"));
                EditorGUILayout.PropertyField(_panelPadding, new GUIContent("Padding"));
                EditorGUILayout.PropertyField(_panelRadius, new GUIContent("Radius"));
            });

            // Plot Settings
            DrawFoldoutSection("Plot Area", ref _plotAreaExpanded, () =>
            {
                EditorGUILayout.PropertyField(_plotBackgroundColor, new GUIContent("Background Color"));
                EditorGUILayout.PropertyField(_plotBorderColor, new GUIContent("Border Color"));
                EditorGUILayout.PropertyField(_plotBorderWidth, new GUIContent("Border Width"));
            });

            serializedObject.ApplyModifiedProperties();
        }

        private static void DrawFoldoutSection(string title, ref bool expanded, System.Action drawContent)
        {
            EditorGUILayout.Space(4);

            // Use GUIStyle box for proper background
            var boxStyle = new GUIStyle("helpbox")
            {
                padding = new RectOffset(8, 8, 6, 8),
                margin = new RectOffset(0, 0, 2, 2)
            };

            EditorGUILayout.BeginVertical(boxStyle);

            // Header with foldout
            expanded = EditorGUILayout.Foldout(expanded, title, true, EditorStyles.foldoutHeader);

            if (expanded)
            {
                GUILayout.Space(4);
                drawContent?.Invoke();
            }

            EditorGUILayout.EndVertical();
        }
    }
}
