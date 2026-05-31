using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace EasyChart.Editor
{
    /// <summary>
    /// Property drawer for TextureFXLayer (Pro Only).
    /// Displays base texture settings plus animation and effects in a streamlined layout.
    /// </summary>
    [CustomPropertyDrawer(typeof(TextureFXLayer))]
    public class TextureFXLayerDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            if (property == null) return new VisualElement();

            bool hasPro = ProPackage.IsInstalled;

            // Get texture name for foldout title
            var fillProp = property.FindPropertyRelative("fill");
            var textureProp = fillProp?.FindPropertyRelative("texture");
            string textureName = textureProp?.objectReferenceValue != null 
                ? $"Layer ({textureProp.objectReferenceValue.name})" 
                : "Layer (Empty)";

            // Container to override Unity's default selection highlight and add our own indicator
            var container = new VisualElement();
            
            // Main foldout for the layer
            var foldout = new Foldout
            {
                text = textureName,
                value = property.isExpanded
            };

            // Apply expanded style (left border indicator only)
            void ApplyExpandedStyle(bool isExpanded)
            {
                if (isExpanded)
                {
                    container.style.borderLeftWidth = 3;
                    container.style.borderLeftColor = new Color(0.4f, 0.6f, 0.9f, 0.8f);
                    container.style.paddingLeft = 2;
                    container.style.marginTop = 2;
                    container.style.marginBottom = 2;
                }
                else
                {
                    container.style.borderLeftWidth = 0;
                    container.style.paddingLeft = 0;
                    container.style.marginTop = 0;
                    container.style.marginBottom = 0;
                }
            }

            ApplyExpandedStyle(property.isExpanded);

            foldout.RegisterValueChangedCallback(evt =>
            {
                property.isExpanded = evt.newValue;
                property.serializedObject.ApplyModifiedProperties();
                ApplyExpandedStyle(evt.newValue);
            });
            
            // Override Unity's default selection highlight
            container.RegisterCallback<AttachToPanelEvent>(evt =>
            {
                var parent = container.parent;
                while (parent != null)
                {
                    if (parent.ClassListContains("unity-list-view__item") ||
                        parent.ClassListContains("unity-collection-view__item"))
                    {
                        // Remove selection background by setting it to transparent
                        parent.style.backgroundColor = new Color(0, 0, 0, 0);
                        break;
                    }
                    parent = parent.parent;
                }
            });

            // Update foldout title when texture changes
            if (textureProp != null)
            {
                foldout.TrackPropertyValue(textureProp, _ =>
                {
                    foldout.text = textureProp.objectReferenceValue != null 
                        ? $"Layer ({textureProp.objectReferenceValue.name})" 
                        : "Layer (Empty)";
                });
            }

            var content = new VisualElement();
            content.style.marginLeft = 4;

            // ========== Texture Properties ==========
            if (fillProp != null)
            {
                var tilingProp = fillProp.FindPropertyRelative("tiling");
                var offsetProp = fillProp.FindPropertyRelative("offset");
                var colorProp = fillProp.FindPropertyRelative("color");
                var adaptiveTilingProp = fillProp.FindPropertyRelative("adaptiveTiling");

                if (textureProp != null) content.Add(new PropertyField(textureProp, "Texture"));
                if (colorProp != null) content.Add(new PropertyField(colorProp, "Tint"));
                if (tilingProp != null) content.Add(new PropertyField(tilingProp, "Tiling"));
                if (offsetProp != null) content.Add(new PropertyField(offsetProp, "Offset"));
                if (adaptiveTilingProp != null) content.Add(new PropertyField(adaptiveTilingProp, "Adaptive Tiling"));
            }

            // Pro warning
            if (!hasPro)
            {
                var warnBox = CreateProWarningBox();
                content.Add(warnBox);
            }

            // ========== Animation ==========
            var animTypeProp = property.FindPropertyRelative("animationType");
            if (animTypeProp != null)
            {
                var animField = new PropertyField(animTypeProp, "Animation Type");
                animField.SetEnabled(hasPro);
                animField.style.marginTop = 6;
                content.Add(animField);
            }

            // Animation params container
            var animParamsRoot = new VisualElement { style = { marginLeft = 12 } };
            animParamsRoot.SetEnabled(hasPro);

            // UV Move params
            var uvRoot = new VisualElement();
            var uvSpeedProp = property.FindPropertyRelative("uvMoveSpeed");
            if (uvSpeedProp != null) uvRoot.Add(new PropertyField(uvSpeedProp, "Speed"));
            animParamsRoot.Add(uvRoot);

            // Scale params
            var scaleRoot = new VisualElement();
            var scaleTypeProp = property.FindPropertyRelative("scaleType");
            var scaleSpeedProp = property.FindPropertyRelative("scaleSpeed");
            var scaleFromProp = property.FindPropertyRelative("scaleFrom");
            var scaleToProp = property.FindPropertyRelative("scaleTo");
            if (scaleTypeProp != null) scaleRoot.Add(new PropertyField(scaleTypeProp, "Mode"));
            if (scaleSpeedProp != null) scaleRoot.Add(new PropertyField(scaleSpeedProp, "Speed"));
            if (scaleFromProp != null) scaleRoot.Add(new PropertyField(scaleFromProp, "From"));
            if (scaleToProp != null) scaleRoot.Add(new PropertyField(scaleToProp, "To"));
            animParamsRoot.Add(scaleRoot);

            // Rotate params (RotateAndScale only)
            var rotateRoot = new VisualElement();
            var rotateSpeedProp = property.FindPropertyRelative("rotateSpeed");
            var layerCountProp = property.FindPropertyRelative("layerCount");
            var colorOverLifeProp = property.FindPropertyRelative("colorOverLife");
            if (rotateSpeedProp != null) rotateRoot.Add(new PropertyField(rotateSpeedProp, "Rotate Speed"));
            if (layerCountProp != null) rotateRoot.Add(new PropertyField(layerCountProp, "Layer Count"));
            if (colorOverLifeProp != null) rotateRoot.Add(new PropertyField(colorOverLifeProp, "Color Over Life"));
            animParamsRoot.Add(rotateRoot);

            content.Add(animParamsRoot);

            void RefreshAnimMode()
            {
                if (animTypeProp == null) return;
                var t = (TextureFillAnimationType)animTypeProp.enumValueIndex;
                uvRoot.style.display = t == TextureFillAnimationType.TextureUvMove ? DisplayStyle.Flex : DisplayStyle.None;
                bool isScale = t == TextureFillAnimationType.TextureScale || t == TextureFillAnimationType.RotateAndScale;
                scaleRoot.style.display = isScale ? DisplayStyle.Flex : DisplayStyle.None;
                rotateRoot.style.display = t == TextureFillAnimationType.RotateAndScale ? DisplayStyle.Flex : DisplayStyle.None;
            }

            RefreshAnimMode();
            if (animTypeProp != null) content.TrackPropertyValue(animTypeProp, _ => RefreshAnimMode());

            // ========== Color Animation (Independent, can combine with UV/Scale) ==========
            var colorAnimTypeProp = property.FindPropertyRelative("colorAnimationType");
            if (colorAnimTypeProp != null)
            {
                var colorAnimField = new PropertyField(colorAnimTypeProp, "Color Animation");
                colorAnimField.SetEnabled(hasPro);
                colorAnimField.style.marginTop = 6;
                content.Add(colorAnimField);
            }

            var colorAnimRoot = new VisualElement { style = { marginLeft = 12 } };
            colorAnimRoot.SetEnabled(hasPro);
            var colorAnimSpeedProp = property.FindPropertyRelative("colorAnimationSpeed");
            var colorAnimGradientProp = property.FindPropertyRelative("colorAnimationGradient");
            if (colorAnimSpeedProp != null) colorAnimRoot.Add(new PropertyField(colorAnimSpeedProp, "Speed"));
            if (colorAnimGradientProp != null) colorAnimRoot.Add(new PropertyField(colorAnimGradientProp, "Gradient"));
            content.Add(colorAnimRoot);

            void RefreshColorAnimParams()
            {
                if (colorAnimTypeProp != null)
                    colorAnimRoot.style.display = colorAnimTypeProp.enumValueIndex != 0 ? DisplayStyle.Flex : DisplayStyle.None;
            }

            RefreshColorAnimParams();
            if (colorAnimTypeProp != null) content.TrackPropertyValue(colorAnimTypeProp, _ => RefreshColorAnimParams());

            // ========== Fade Effect ==========
            var fadeTypeProp = property.FindPropertyRelative("fadeType");
            if (fadeTypeProp != null)
            {
                var fadeField = new PropertyField(fadeTypeProp, "Fade Type");
                fadeField.SetEnabled(hasPro);
                fadeField.style.marginTop = 4;
                content.Add(fadeField);
            }

            var fadeParamsRoot = new VisualElement { style = { marginLeft = 12 } };
            fadeParamsRoot.SetEnabled(hasPro);
            var fadeIntensityProp = property.FindPropertyRelative("fadeIntensity");
            var fadeSoftnessProp = property.FindPropertyRelative("fadeSoftness");
            if (fadeIntensityProp != null) fadeParamsRoot.Add(new PropertyField(fadeIntensityProp, "Intensity"));
            if (fadeSoftnessProp != null) fadeParamsRoot.Add(new PropertyField(fadeSoftnessProp, "Softness"));
            content.Add(fadeParamsRoot);

            // ========== Deform Effect ==========
            var deformTypeProp = property.FindPropertyRelative("deformType");
            if (deformTypeProp != null)
            {
                var deformField = new PropertyField(deformTypeProp, "Deform Type");
                deformField.SetEnabled(hasPro);
                deformField.style.marginTop = 4;
                content.Add(deformField);
            }

            var deformParamsRoot = new VisualElement { style = { marginLeft = 12 } };
            deformParamsRoot.SetEnabled(hasPro);
            var deformIntensityProp = property.FindPropertyRelative("deformIntensity");
            var deformSpeedProp = property.FindPropertyRelative("deformSpeed");
            var waveFrequencyProp = property.FindPropertyRelative("waveFrequency");
            if (deformIntensityProp != null) deformParamsRoot.Add(new PropertyField(deformIntensityProp, "Intensity"));
            if (deformSpeedProp != null) deformParamsRoot.Add(new PropertyField(deformSpeedProp, "Speed"));
            if (waveFrequencyProp != null) deformParamsRoot.Add(new PropertyField(waveFrequencyProp, "Frequency"));
            content.Add(deformParamsRoot);

            void RefreshEffectsParams()
            {
                if (fadeTypeProp != null)
                    fadeParamsRoot.style.display = fadeTypeProp.enumValueIndex != 0 ? DisplayStyle.Flex : DisplayStyle.None;
                if (deformTypeProp != null)
                    deformParamsRoot.style.display = deformTypeProp.enumValueIndex != 0 ? DisplayStyle.Flex : DisplayStyle.None;
            }

            RefreshEffectsParams();
            if (fadeTypeProp != null) content.TrackPropertyValue(fadeTypeProp, _ => RefreshEffectsParams());
            if (deformTypeProp != null) content.TrackPropertyValue(deformTypeProp, _ => RefreshEffectsParams());

            foldout.Add(content);
            container.Add(foldout);
            return container;
        }

        private VisualElement CreateProWarningBox()
        {
            return EditorStyleHelper.CreateWarningBox("TextureFX animations and effects are EasyChart Pro-only.");
        }
    }
}
