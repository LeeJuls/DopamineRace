using EasyChart;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace EasyChart.Editor
{
    [CustomPropertyDrawer(typeof(TextureFillSettings))]
    public class TextureFillSettingsDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var foldout = new Foldout
            {
                text = property != null ? property.displayName : "TextureFill",
                value = property != null && property.isExpanded
            };

            foldout.RegisterValueChangedCallback(evt =>
            {
                if (property == null) return;
                property.isExpanded = evt.newValue;
                property.serializedObject.ApplyModifiedProperties();
            });

            if (property == null) return foldout;

            var textureProp = property.FindPropertyRelative("texture");
            var tilingProp = property.FindPropertyRelative("tiling");
            var offsetProp = property.FindPropertyRelative("offset");
            var colorProp = property.FindPropertyRelative("color");

            var animationTypeProp = property.FindPropertyRelative("animationType");
            var uvMoveSpeedProp = property.FindPropertyRelative("uvMoveSpeed");
            var scaleTypeProp = property.FindPropertyRelative("scaleType");
            var scaleSpeedProp = property.FindPropertyRelative("scaleSpeed");

            var scaleFromProp = property.FindPropertyRelative("scaleFrom");
            var scaleToProp = property.FindPropertyRelative("scaleTo");

            var colorFadeWrapProp = property.FindPropertyRelative("colorFadeWrap");
            var colorFadeSpeedProp = property.FindPropertyRelative("colorFadeSpeed");
            var colorFadeGradientProp = property.FindPropertyRelative("colorFadeGradient");

            bool hasPro = ProPackage.IsInstalled;

            if (textureProp != null) foldout.Add(new PropertyField(textureProp));
            if (tilingProp != null) foldout.Add(new PropertyField(tilingProp));
            if (offsetProp != null) foldout.Add(new PropertyField(offsetProp));
            if (colorProp != null) foldout.Add(new PropertyField(colorProp));

            if (!hasPro)
            {
                var warnBox = new VisualElement();
                warnBox.style.borderTopWidth = 1;
                warnBox.style.borderBottomWidth = 1;
                warnBox.style.borderLeftWidth = 1;
                warnBox.style.borderRightWidth = 1;

                var borderColor = new Color(0.1f, 0.1f, 0.1f);
                warnBox.style.borderTopColor = borderColor;
                warnBox.style.borderBottomColor = borderColor;
                warnBox.style.borderLeftColor = borderColor;
                warnBox.style.borderRightColor = borderColor;

                warnBox.style.backgroundColor = new Color(0.18f, 0.18f, 0.18f);
                warnBox.style.marginTop = 2;
                warnBox.style.marginBottom = 2;
                warnBox.style.paddingLeft = 6;
                warnBox.style.paddingRight = 6;
                warnBox.style.paddingTop = 4;
                warnBox.style.paddingBottom = 4;
                warnBox.style.borderTopLeftRadius = 3;
                warnBox.style.borderTopRightRadius = 3;
                warnBox.style.borderBottomLeftRadius = 3;
                warnBox.style.borderBottomRightRadius = 3;

                var warn = new Label("Texture fill animations are EasyChart Pro-only. Please install EasyChart Pro to edit/preview them.");
                warn.style.opacity = 0.9f;
                warn.style.whiteSpace = WhiteSpace.Normal;
                warnBox.Add(warn);
                foldout.Add(warnBox);
            }

            if (animationTypeProp != null)
            {
                var animField = new PropertyField(animationTypeProp);
                animField.SetEnabled(hasPro);
                foldout.Add(animField);
            }

            var uvRoot = new VisualElement();
            var scaleRoot = new VisualElement();
            var scaleLerpRoot = new VisualElement();

            var fadeRoot = new VisualElement();

            uvRoot.style.marginLeft = 12;
            scaleRoot.style.marginLeft = 12;

            scaleLerpRoot.style.marginLeft = 12;

            fadeRoot.style.marginLeft = 12;

            if (uvMoveSpeedProp != null) uvRoot.Add(new PropertyField(uvMoveSpeedProp));

            if (scaleTypeProp != null) scaleRoot.Add(new PropertyField(scaleTypeProp));
            if (scaleSpeedProp != null) scaleRoot.Add(new PropertyField(scaleSpeedProp));

            if (scaleFromProp != null) scaleLerpRoot.Add(new PropertyField(scaleFromProp));
            if (scaleToProp != null) scaleLerpRoot.Add(new PropertyField(scaleToProp));

            if (colorFadeWrapProp != null) fadeRoot.Add(new PropertyField(colorFadeWrapProp));
            if (colorFadeSpeedProp != null) fadeRoot.Add(new PropertyField(colorFadeSpeedProp));
            if (colorFadeGradientProp != null) fadeRoot.Add(new PropertyField(colorFadeGradientProp));

            scaleRoot.Add(scaleLerpRoot);

            if (colorFadeGradientProp != null) scaleRoot.Add(new PropertyField(colorFadeGradientProp));

            foldout.Add(uvRoot);
            foldout.Add(scaleRoot);
            foldout.Add(fadeRoot);

            uvRoot.SetEnabled(hasPro);
            scaleRoot.SetEnabled(hasPro);

            fadeRoot.SetEnabled(hasPro);

            void RefreshMode()
            {
                if (animationTypeProp == null)
                {
                    uvRoot.style.display = DisplayStyle.None;
                    scaleRoot.style.display = DisplayStyle.None;
                    fadeRoot.style.display = DisplayStyle.None;
                    return;
                }

                var t = (TextureFillAnimationType)animationTypeProp.enumValueIndex;
                uvRoot.style.display = t == TextureFillAnimationType.TextureUvMove ? DisplayStyle.Flex : DisplayStyle.None;
                scaleRoot.style.display = t == TextureFillAnimationType.TextureScale ? DisplayStyle.Flex : DisplayStyle.None;

                fadeRoot.style.display = t == TextureFillAnimationType.TextureFade ? DisplayStyle.Flex : DisplayStyle.None;
            }

            RefreshMode();
            if (animationTypeProp != null) foldout.TrackPropertyValue(animationTypeProp, _ => RefreshMode());
            if (scaleTypeProp != null) foldout.TrackPropertyValue(scaleTypeProp, _ => RefreshMode());

            return foldout;
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

            bool hasPro = ProPackage.IsInstalled;

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

            var animationTypeProp = property.FindPropertyRelative("animationType");

            DrawProp(property.FindPropertyRelative("texture"));
            DrawProp(property.FindPropertyRelative("tiling"));
            DrawProp(property.FindPropertyRelative("offset"));
            DrawProp(property.FindPropertyRelative("color"));

            if (!hasPro)
            {
                float helpH = EditorGUIUtility.singleLineHeight * 2f;
                EditorGUI.HelpBox(Next(helpH), "Texture fill animations are EasyChart Pro-only. Please install EasyChart Pro to edit/preview them.", MessageType.Info);

                using (new EditorGUI.DisabledScope(true))
                {
                    DrawProp(animationTypeProp);
                    if (animationTypeProp != null)
                    {
                        var t = (TextureFillAnimationType)animationTypeProp.enumValueIndex;
                        if (t == TextureFillAnimationType.TextureUvMove)
                        {
                            DrawProp(property.FindPropertyRelative("uvMoveSpeed"));
                        }
                        else if (t == TextureFillAnimationType.TextureScale)
                        {
                            DrawProp(property.FindPropertyRelative("scaleType"));
                            DrawProp(property.FindPropertyRelative("scaleSpeed"));

                            DrawProp(property.FindPropertyRelative("scaleFrom"));
                            DrawProp(property.FindPropertyRelative("scaleTo"));

                            DrawProp(property.FindPropertyRelative("colorFadeGradient"));

                        }
                        else if (t == TextureFillAnimationType.TextureFade)
                        {
                            DrawProp(property.FindPropertyRelative("colorFadeWrap"));
                            DrawProp(property.FindPropertyRelative("colorFadeSpeed"));
                            DrawProp(property.FindPropertyRelative("colorFadeGradient"));
                        }
                    }
                }
            }
            else
            {
                DrawProp(animationTypeProp);
                if (animationTypeProp != null)
                {
                    var t = (TextureFillAnimationType)animationTypeProp.enumValueIndex;
                    if (t == TextureFillAnimationType.TextureUvMove)
                    {
                        DrawProp(property.FindPropertyRelative("uvMoveSpeed"));
                    }
                    else if (t == TextureFillAnimationType.TextureScale)
                    {
                        DrawProp(property.FindPropertyRelative("scaleType"));
                        DrawProp(property.FindPropertyRelative("scaleSpeed"));

                        DrawProp(property.FindPropertyRelative("scaleFrom"));
                        DrawProp(property.FindPropertyRelative("scaleTo"));

                        DrawProp(property.FindPropertyRelative("colorFadeGradient"));

                    }
                    else if (t == TextureFillAnimationType.TextureFade)
                    {
                        DrawProp(property.FindPropertyRelative("colorFadeWrap"));
                        DrawProp(property.FindPropertyRelative("colorFadeSpeed"));
                        DrawProp(property.FindPropertyRelative("colorFadeGradient"));
                    }
                }
            }

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

            bool hasPro = ProPackage.IsInstalled;
            if (!hasPro)
            {
                h += EditorGUIUtility.singleLineHeight * 2f + EditorGUIUtility.standardVerticalSpacing;
            }

            h += Add(property.FindPropertyRelative("animationType"));

            var animationTypeProp = property.FindPropertyRelative("animationType");
            if (animationTypeProp != null)
            {
                var t = (TextureFillAnimationType)animationTypeProp.enumValueIndex;
                if (t == TextureFillAnimationType.TextureUvMove)
                {
                    h += Add(property.FindPropertyRelative("uvMoveSpeed"));
                }
                else if (t == TextureFillAnimationType.TextureScale)
                {
                    h += Add(property.FindPropertyRelative("scaleType"));
                    h += Add(property.FindPropertyRelative("scaleSpeed"));

                    h += Add(property.FindPropertyRelative("scaleFrom"));
                    h += Add(property.FindPropertyRelative("scaleTo"));

                    h += Add(property.FindPropertyRelative("colorFadeGradient"));

                }
                else if (t == TextureFillAnimationType.TextureFade)
                {
                    h += Add(property.FindPropertyRelative("colorFadeWrap"));
                    h += Add(property.FindPropertyRelative("colorFadeSpeed"));
                    h += Add(property.FindPropertyRelative("colorFadeGradient"));
                }
            }

            return h;
        }
    }
}
