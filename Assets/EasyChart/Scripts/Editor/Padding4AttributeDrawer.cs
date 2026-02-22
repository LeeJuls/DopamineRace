using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace EasyChart.Editor
{
    [CustomPropertyDrawer(typeof(Padding4Attribute))]
    public class Padding4AttributeDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var root = new VisualElement();

            var header = new Label(property.displayName);
            header.style.unityFontStyleAndWeight = FontStyle.Bold;
            header.style.marginTop = 5;
            root.Add(header);

            var row1 = new VisualElement();
            row1.style.flexDirection = FlexDirection.Row;

            var row2 = new VisualElement();
            row2.style.flexDirection = FlexDirection.Row;

            var leftField = new FloatField("Left");
            var rightField = new FloatField("Right");
            var topField = new FloatField("Top");
            var bottomField = new FloatField("Bottom");

            void StyleCompact(FloatField f)
            {
                f.labelElement.style.minWidth = 48;
                f.labelElement.style.width = 48;
                f.labelElement.style.marginRight = 2;
                f.labelElement.style.flexShrink = 0;

                var input = f.Q<VisualElement>(className: "unity-text-input");
                if (input != null)
                {
                    input.style.minWidth = 64;
                    input.style.width = 64;
                    input.style.flexGrow = 0;
                    input.style.flexShrink = 0;
                }
            }

            StyleCompact(leftField);
            StyleCompact(rightField);
            StyleCompact(topField);
            StyleCompact(bottomField);

            leftField.style.flexGrow = 1;
            rightField.style.flexGrow = 1;
            topField.style.flexGrow = 1;
            bottomField.style.flexGrow = 1;

            row1.Add(leftField);
            row1.Add(rightField);
            row2.Add(topField);
            row2.Add(bottomField);

            root.Add(row1);
            root.Add(row2);

            void RefreshFromProperty()
            {
                if (property.propertyType != SerializedPropertyType.Vector4) return;
                var v = property.vector4Value;
                leftField.SetValueWithoutNotify(v.x);
                rightField.SetValueWithoutNotify(v.y);
                topField.SetValueWithoutNotify(v.z);
                bottomField.SetValueWithoutNotify(v.w);
            }

            RefreshFromProperty();
            root.TrackPropertyValue(property, _ => RefreshFromProperty());

            leftField.RegisterValueChangedCallback(evt =>
            {
                if (property.propertyType != SerializedPropertyType.Vector4) return;
                var v = property.vector4Value;
                v.x = evt.newValue;
                property.vector4Value = v;
                property.serializedObject.ApplyModifiedProperties();
            });

            rightField.RegisterValueChangedCallback(evt =>
            {
                if (property.propertyType != SerializedPropertyType.Vector4) return;
                var v = property.vector4Value;
                v.y = evt.newValue;
                property.vector4Value = v;
                property.serializedObject.ApplyModifiedProperties();
            });

            topField.RegisterValueChangedCallback(evt =>
            {
                if (property.propertyType != SerializedPropertyType.Vector4) return;
                var v = property.vector4Value;
                v.z = evt.newValue;
                property.vector4Value = v;
                property.serializedObject.ApplyModifiedProperties();
            });

            bottomField.RegisterValueChangedCallback(evt =>
            {
                if (property.propertyType != SerializedPropertyType.Vector4) return;
                var v = property.vector4Value;
                v.w = evt.newValue;
                property.vector4Value = v;
                property.serializedObject.ApplyModifiedProperties();
            });

            return root;
        }
    }
}
