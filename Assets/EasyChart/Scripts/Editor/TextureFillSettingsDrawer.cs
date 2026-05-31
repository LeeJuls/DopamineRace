using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace EasyChart.Editor
{
    /// <summary>
    /// Simple property drawer for TextureFillSettings (base texture properties only).
    /// Animation and effects are now in TextureFXLayer.
    /// </summary>
    [CustomPropertyDrawer(typeof(TextureFillSettings))]
    public class TextureFillSettingsDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            // Container with left border indicator
            var container = new VisualElement();
            
            var foldout = new Foldout
            {
                text = property != null ? property.displayName : "Texture Fill",
                value = property != null && property.isExpanded
            };

            // Apply expanded style (left border indicator)
            EditorStyleHelper.ApplyExpandedStyle(container, property != null && property.isExpanded);

            foldout.RegisterValueChangedCallback(evt =>
            {
                if (property == null) return;
                property.isExpanded = evt.newValue;
                property.serializedObject.ApplyModifiedProperties();
                EditorStyleHelper.ApplyExpandedStyle(container, evt.newValue);
            });

            if (property == null)
            {
                container.Add(foldout);
                return container;
            }

            var textureProp = property.FindPropertyRelative("texture");
            var tilingProp = property.FindPropertyRelative("tiling");
            var offsetProp = property.FindPropertyRelative("offset");
            var colorProp = property.FindPropertyRelative("color");
            var adaptiveTilingProp = property.FindPropertyRelative("adaptiveTiling");

            if (textureProp != null) foldout.Add(new PropertyField(textureProp));
            if (tilingProp != null) foldout.Add(new PropertyField(tilingProp));
            if (offsetProp != null) foldout.Add(new PropertyField(offsetProp));
            if (colorProp != null) foldout.Add(new PropertyField(colorProp));
            if (adaptiveTilingProp != null) foldout.Add(new PropertyField(adaptiveTilingProp));

            container.Add(foldout);
            return container;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property == null)
            {
                EditorGUI.LabelField(position, label.text, "(null)");
                return;
            }

            position.height = EditorGUIUtility.singleLineHeight;
            property.isExpanded = EditorGUI.Foldout(position, property.isExpanded, label, true);
            if (!property.isExpanded) return;

            EditorGUI.indentLevel++;

            float y = position.y + EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            float w = position.width;

            Rect Next(float h)
            {
                var r = new Rect(position.x, y, w, h);
                y += h + EditorGUIUtility.standardVerticalSpacing;
                return r;
            }

            void DrawProp(SerializedProperty p)
            {
                if (p == null) return;
                float h = EditorGUI.GetPropertyHeight(p, true);
                EditorGUI.PropertyField(Next(h), p, true);
            }

            DrawProp(property.FindPropertyRelative("texture"));
            DrawProp(property.FindPropertyRelative("tiling"));
            DrawProp(property.FindPropertyRelative("offset"));
            DrawProp(property.FindPropertyRelative("color"));
            DrawProp(property.FindPropertyRelative("adaptiveTiling"));

            EditorGUI.indentLevel--;
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (property == null) return EditorGUIUtility.singleLineHeight;

            float h = EditorGUIUtility.singleLineHeight;
            if (!property.isExpanded) return h;

            float Add(SerializedProperty p)
            {
                return p != null ? EditorGUI.GetPropertyHeight(p, true) + EditorGUIUtility.standardVerticalSpacing : 0f;
            }

            h += EditorGUIUtility.standardVerticalSpacing;
            h += Add(property.FindPropertyRelative("texture"));
            h += Add(property.FindPropertyRelative("tiling"));
            h += Add(property.FindPropertyRelative("offset"));
            h += Add(property.FindPropertyRelative("color"));
            h += Add(property.FindPropertyRelative("adaptiveTiling"));

            return h;
        }
    }
}
