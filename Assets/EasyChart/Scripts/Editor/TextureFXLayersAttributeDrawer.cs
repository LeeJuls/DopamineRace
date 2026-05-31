using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace EasyChart.Editor
{
    /// <summary>
    /// Custom property drawer for fields marked with TextureFXLayersAttribute.
    /// Provides copy/paste functionality for TextureFXLayer lists.
    /// </summary>
    [CustomPropertyDrawer(typeof(TextureFXLayersAttribute))]
    public class TextureFXLayersAttributeDrawer : PropertyDrawer
    {
        private const float ButtonWidth = 20f;
        private const float ButtonSpacing = 4f;
        private const float HeaderHeight = 20f;

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var attr = attribute as TextureFXLayersAttribute;
            string label = attr?.Label ?? "Texture FX Layers";

            return TextureFXLayersEditorHelper.CreateTextureFXLayersUI(property, label);
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var attr = attribute as TextureFXLayersAttribute;
            string headerLabel = attr?.Label ?? "Texture FX Layers";

            // Header row with label and buttons
            Rect headerRect = new Rect(position.x, position.y, position.width, HeaderHeight);
            
            // Calculate button positions (right-aligned)
            float buttonsWidth = ButtonWidth * 3 + ButtonSpacing * 2;
            Rect labelRect = new Rect(headerRect.x, headerRect.y, headerRect.width - buttonsWidth - 10, HeaderHeight);
            
            // Draw label
            EditorGUI.LabelField(labelRect, headerLabel, EditorStyles.boldLabel);

            // Load icons
            var copyIcon = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/EasyChart/Textures/Icon/Copy.png");
            var pasteIcon = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/EasyChart/Textures/Icon/Paste.png");
            var clearIcon = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/EasyChart/Textures/Icon/clear.png");

            // Draw buttons
            float buttonX = headerRect.xMax - buttonsWidth;
            
            Rect copyRect = new Rect(buttonX, headerRect.y + 2, ButtonWidth, ButtonWidth);
            if (GUI.Button(copyRect, new GUIContent(copyIcon, "Copy Texture FX Layers"), GUIStyle.none))
            {
                TextureFXLayersEditorHelper.CopyLayers(property);
            }

            Rect pasteRect = new Rect(buttonX + ButtonWidth + ButtonSpacing, headerRect.y + 2, ButtonWidth, ButtonWidth);
            if (GUI.Button(pasteRect, new GUIContent(pasteIcon, "Paste Texture FX Layers"), GUIStyle.none))
            {
                TextureFXLayersEditorHelper.PasteLayers(property);
            }

            Rect clearRect = new Rect(buttonX + (ButtonWidth + ButtonSpacing) * 2, headerRect.y + 2, ButtonWidth, ButtonWidth);
            if (GUI.Button(clearRect, new GUIContent(clearIcon, "Clear all Texture FX Layers"), GUIStyle.none))
            {
                TextureFXLayersEditorHelper.ClearLayers(property);
            }

            // Draw the property field below the header
            Rect propertyRect = new Rect(position.x, position.y + HeaderHeight + 2, position.width, 
                EditorGUI.GetPropertyHeight(property, GUIContent.none, true));
            EditorGUI.PropertyField(propertyRect, property, GUIContent.none, true);
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return HeaderHeight + 2 + EditorGUI.GetPropertyHeight(property, GUIContent.none, true);
        }
    }
}
