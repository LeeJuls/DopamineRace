using UnityEngine;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace EasyChart.Editor
{
    [CustomEditor(typeof(ChartTheme))]
    public class ChartThemeEditor : UnityEditor.Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            var root = new VisualElement();
            root.style.paddingTop = 4;
            root.style.paddingBottom = 4;

            var so = serializedObject;

            root.Add(BuildSection(so, "Font", new[]
            {
                ("primaryFont",        "Primary Font",  "Main font for chart text (supports SDF FontAsset)"),
                ("monoFont",           "Mono Font",     "Monospace font for numeric values"),
                ("fontScale",          "Font Scale",    "Global font size multiplier"),
            }));

            root.Add(BuildSection(so, "Font Size", new[]
            {
                ("titleFontSize",       "Title",        "Override title font size (-1 = use USS default)"),
                ("subtitleFontSize",    "Subtitle",     "Override subtitle font size (-1 = use USS default)"),
                ("axisFontSize",        "Axis",         "Override axis label font size (-1 = use USS default)"),
                ("legendFontSize",      "Legend",       "Override legend font size (-1 = use USS default)"),
                ("tooltipFontSize",     "Tooltip",      "Override tooltip font size (-1 = use USS default)"),
                ("seriesLabelFontSize", "Series Label", "Override series label font size (-1 = use USS default)"),
            }));

            root.Add(BuildSection(so, "Default Template", new[]
            {
                ("baseProfile", "Base Profile", "Template profile used when creating new charts. If empty, clones the first profile in current library."),
            }));

            root.Bind(so);
            return root;
        }

        private static VisualElement BuildSection(SerializedObject so, string title, (string prop, string label, string tooltip)[] fields)
        {
            // Create foldout with box styling matching Settings window style
            var foldout = new Foldout { text = title };
            foldout.bindingPath = string.Empty;
            foldout.SetValueWithoutNotify(true);

            var borderColor = new Color(0.1f, 0.1f, 0.1f);
            var backgroundColor = new Color(0.18f, 0.18f, 0.18f);

            foldout.style.borderTopWidth = 1;
            foldout.style.borderBottomWidth = 1;
            foldout.style.borderLeftWidth = 1;
            foldout.style.borderRightWidth = 1;
            foldout.style.borderTopColor = borderColor;
            foldout.style.borderBottomColor = borderColor;
            foldout.style.borderLeftColor = borderColor;
            foldout.style.borderRightColor = borderColor;
            foldout.style.backgroundColor = backgroundColor;
            foldout.style.marginTop = 6;
            foldout.style.marginBottom = 6;
            foldout.style.paddingLeft = 6;
            foldout.style.paddingRight = 6;
            foldout.style.paddingTop = 4;
            foldout.style.paddingBottom = 6;
            foldout.style.borderTopLeftRadius = 3;
            foldout.style.borderTopRightRadius = 3;
            foldout.style.borderBottomLeftRadius = 3;
            foldout.style.borderBottomRightRadius = 3;

            foreach (var (propName, labelText, tooltipText) in fields)
            {
                var prop = so.FindProperty(propName);
                if (prop == null) continue;
                var pf = new PropertyField(prop, labelText) { tooltip = tooltipText };
                foldout.Add(pf);
            }

            return foldout;
        }
    }
}
