using System;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace EasyChart.Editor
{
    public partial class EasyChartLibraryWindow
    {
        private static readonly string[] _labelFormatDropdownOptions = { "None", "F0", "F1", "F2", "N0", "N2", "P0", "E2", "Custom" };

        private PropertyField AddBoundPropertyField(VisualElement parent, SerializedProperty prop)
        {
            if (parent == null) return null;
            if (prop == null) return null;

            var pf = new PropertyField(prop);
            if (_serializedProfile != null) pf.Bind(_serializedProfile);
            pf.RegisterCallback<SerializedPropertyChangeEvent>(evt =>
            {
                if (_serializedProfile == null) return;
                if (pf.panel == null) return;
                ScheduleUpdatePreview();
            });
            parent.Add(pf);
            return pf;
        }

        private PropertyField AddToggleContainer(VisualElement parent, SerializedProperty toggleProp, VisualElement detailsContainer, bool showWhenToggleIsTrue)
        {
            if (parent == null) return null;
            if (toggleProp == null) return null;
            if (detailsContainer == null) return null;

            var toggleField = new PropertyField(toggleProp);
            if (_serializedProfile != null) toggleField.Bind(_serializedProfile);
            parent.Add(toggleField);
            parent.Add(detailsContainer);

            void UpdateVisibility()
            {
                if (_serializedProfile == null) return;
                if (parent.panel == null) return;
                // Apply any pending changes before updating to avoid losing modifications
                if (_serializedProfile.hasModifiedProperties)
                    _serializedProfile.ApplyModifiedProperties();
                _serializedProfile.Update();

                bool enabled = toggleProp.boolValue;
                bool show = showWhenToggleIsTrue ? enabled : !enabled;
                detailsContainer.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
            }

            UpdateVisibility();
            toggleField.RegisterCallback<SerializedPropertyChangeEvent>(evt =>
            {
                UpdateVisibility();
                ScheduleUpdatePreview();
            });
            return toggleField;
        }

        private void AddLabelFormatDropdown(VisualElement parent, SerializedProperty labelFormatProp)
        {
            if (parent == null) return;
            if (labelFormatProp == null) return;

            var options = _labelFormatDropdownOptions;
            int customIndex = options.Length - 1;

            int ResolveIndex(string fmt)
            {
                if (string.IsNullOrEmpty(fmt)) return 0;
                for (int i = 1; i < options.Length - 1; i++)
                {
                    if (options[i] == fmt) return i;
                }
                return customIndex;
            }

            int index = ResolveIndex(labelFormatProp.stringValue);

            var popup = new PopupField<string>("LabelFormat", options.ToList(), Mathf.Clamp(index, 0, options.Length - 1));
            popup.style.flexGrow = 1;
            popup.style.flexShrink = 1;
            parent.Add(popup);

            var customField = new TextField("Format");
            customField.value = labelFormatProp.stringValue;
            customField.style.flexGrow = 1;
            customField.style.flexShrink = 1;
            customField.style.display = (index == customIndex) ? DisplayStyle.Flex : DisplayStyle.None;
            parent.Add(customField);

            void ApplyIndex(int idx)
            {
                if (_serializedProfile == null) return;
                if (parent.panel == null) return;

                if (idx == 0)
                {
                    labelFormatProp.stringValue = string.Empty;
                }
                else if (idx == customIndex)
                {
                    if (ResolveIndex(labelFormatProp.stringValue) != customIndex)
                    {
                        labelFormatProp.stringValue = string.Empty;
                    }
                    else if (labelFormatProp.stringValue == null)
                    {
                        labelFormatProp.stringValue = string.Empty;
                    }
                }
                else
                {
                    labelFormatProp.stringValue = options[idx];
                }

                customField.value = labelFormatProp.stringValue;
                customField.style.display = (idx == customIndex) ? DisplayStyle.Flex : DisplayStyle.None;

                if (_serializedProfile != null && _serializedProfile.hasModifiedProperties)
                {
                    _serializedProfile.ApplyModifiedProperties();
                }
                ScheduleUpdatePreview();
            }

            popup.RegisterValueChangedCallback(evt =>
            {
                int idx = options.ToList().IndexOf(evt.newValue);
                ApplyIndex(idx < 0 ? 0 : idx);
            });

            customField.RegisterValueChangedCallback(evt =>
            {
                if (_serializedProfile == null) return;
                if (parent.panel == null) return;
                labelFormatProp.stringValue = evt.newValue;

                if (_serializedProfile != null && _serializedProfile.hasModifiedProperties)
                {
                    _serializedProfile.ApplyModifiedProperties();
                }
                ScheduleUpdatePreview();
            });
        }

        private VisualElement CreateHintBox(out Label label)
        {
            var box = new VisualElement();
            box.style.borderTopWidth = 1;
            box.style.borderBottomWidth = 1;
            box.style.borderLeftWidth = 1;
            box.style.borderRightWidth = 1;

            var borderColor = new Color(0.1f, 0.1f, 0.1f);
            box.style.borderTopColor = borderColor;
            box.style.borderBottomColor = borderColor;
            box.style.borderLeftColor = borderColor;
            box.style.borderRightColor = borderColor;

            box.style.backgroundColor = new Color(0.18f, 0.18f, 0.18f);
            box.style.marginTop = 2;
            box.style.marginBottom = 2;
            box.style.paddingLeft = 6;
            box.style.paddingRight = 6;
            box.style.paddingTop = 4;
            box.style.paddingBottom = 4;
            box.style.borderTopLeftRadius = 3;
            box.style.borderTopRightRadius = 3;
            box.style.borderBottomLeftRadius = 3;
            box.style.borderBottomRightRadius = 3;
            box.style.flexShrink = 1;

            label = new Label();
            label.style.whiteSpace = WhiteSpace.Normal;
            label.style.unityTextAlign = TextAnchor.UpperLeft;
            label.style.flexGrow = 1;
            label.style.flexShrink = 1;
            box.Add(label);

            return box;
        }

        private void EnsureSharedIconsLoaded()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                if (_folderIcon == null) _folderIcon = EditorGUIUtility.FindTexture("Folder Icon");
                if (_profileIcon == null) _profileIcon = EditorGUIUtility.FindTexture("ScriptableObject Icon");
                if (_refreshIcon == null) _refreshIcon = EditorGUIUtility.FindTexture("Refresh");
                if (_menuIcon == null) _menuIcon = _refreshIcon;
                return;
            }

            if (_folderIcon == null)
            {
                _folderIcon = AssetDatabase.LoadAssetAtPath<Texture2D>(FolderIconPath);
                if (_folderIcon == null)
                {
                    var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(FolderIconPath);
                    if (sprite != null) _folderIcon = sprite.texture;
                }

                if (_folderIcon == null)
                {
                    _folderIcon = EditorGUIUtility.FindTexture("Folder Icon");
                }
            }
            if (_profileIcon == null)
            {
                _profileIcon = AssetDatabase.LoadAssetAtPath<Texture2D>(ProfileIconPath);
                if (_profileIcon == null)
                {
                    var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(ProfileIconPath);
                    if (sprite != null) _profileIcon = sprite.texture;
                }

                if (_profileIcon == null)
                {
                    _profileIcon = EditorGUIUtility.FindTexture("ScriptableObject Icon");
                }
            }

            if (_addChartIcon == null)
            {
                _addChartIcon = AssetDatabase.LoadAssetAtPath<Texture2D>(AddChartIconPath);
                if (_addChartIcon == null)
                {
                    var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(AddChartIconPath);
                    if (sprite != null) _addChartIcon = sprite.texture;
                }
            }

            if (_addFolderIcon == null)
            {
                _addFolderIcon = AssetDatabase.LoadAssetAtPath<Texture2D>(AddFolderIconPath);
                if (_addFolderIcon == null)
                {
                    var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(AddFolderIconPath);
                    if (sprite != null) _addFolderIcon = sprite.texture;
                }
            }

            if (_refreshIcon == null)
            {
                _refreshIcon = AssetDatabase.LoadAssetAtPath<Texture2D>(RefreshIconPath);
                if (_refreshIcon == null)
                {
                    var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(RefreshIconPath);
                    if (sprite != null) _refreshIcon = sprite.texture;
                }
            }

            if (_saveIcon == null)
            {
                _saveIcon = AssetDatabase.LoadAssetAtPath<Texture2D>(SaveIconPath);
                if (_saveIcon == null)
                {
                    var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SaveIconPath);
                    if (sprite != null) _saveIcon = sprite.texture;
                }

                if (_saveIcon == null)
                {
                    _saveIcon = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/EasyChart/Textures/Icon/Save (1).png");
                    if (_saveIcon == null) _saveIcon = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/EasyChart/Textures/Icon/Save (2).png");

                    if (_saveIcon == null)
                    {
                        var sprite1 = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/EasyChart/Textures/Icon/Save (1).png");
                        if (sprite1 != null) _saveIcon = sprite1.texture;
                    }

                    if (_saveIcon == null)
                    {
                        var sprite2 = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/EasyChart/Textures/Icon/Save (2).png");
                        if (sprite2 != null) _saveIcon = sprite2.texture;
                    }
                }
            }

            if (_menuIcon == null)
            {
                _menuIcon = AssetDatabase.LoadAssetAtPath<Texture2D>(MenuIconPath);
                if (_menuIcon == null)
                {
                    var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(MenuIconPath);
                    if (sprite != null) _menuIcon = sprite.texture;
                }

                if (_menuIcon == null)
                {
                    _menuIcon = _refreshIcon;
                }
            }

            if (_copyIcon == null)
            {
                _copyIcon = AssetDatabase.LoadAssetAtPath<Texture2D>(CopyIconPath);
                if (_copyIcon == null)
                {
                    var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(CopyIconPath);
                    if (sprite != null) _copyIcon = sprite.texture;
                }
            }

            if (_cloneIcon == null)
            {
                _cloneIcon = AssetDatabase.LoadAssetAtPath<Texture2D>(CloneIconPath);
                if (_cloneIcon == null)
                {
                    var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(CloneIconPath);
                    if (sprite != null) _cloneIcon = sprite.texture;
                }
            }

            if (_applyToChartIcon == null)
            {
                _applyToChartIcon = AssetDatabase.LoadAssetAtPath<Texture2D>(ApplyToChartIconPath);
                if (_applyToChartIcon == null)
                {
                    var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(ApplyToChartIconPath);
                    if (sprite != null) _applyToChartIcon = sprite.texture;
                }
            }

            if (_feedIcon == null)
            {
                _feedIcon = AssetDatabase.LoadAssetAtPath<Texture2D>(FeedIconPath);
                if (_feedIcon == null)
                {
                    var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(FeedIconPath);
                    if (sprite != null) _feedIcon = sprite.texture;
                }
            }

            if (_dataIcon == null)
            {
                _dataIcon = AssetDatabase.LoadAssetAtPath<Texture2D>(DataIconPath);
                if (_dataIcon == null)
                {
                    var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(DataIconPath);
                    if (sprite != null) _dataIcon = sprite.texture;
                }
            }

            if (_apiOnIcon == null)
            {
                _apiOnIcon = AssetDatabase.LoadAssetAtPath<Texture2D>(ApiOnIconPath);
                if (_apiOnIcon == null)
                {
                    var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(ApiOnIconPath);
                    if (sprite != null) _apiOnIcon = sprite.texture;
                }
            }

            if (_apiOffIcon == null)
            {
                _apiOffIcon = AssetDatabase.LoadAssetAtPath<Texture2D>(ApiOffIconPath);
                if (_apiOffIcon == null)
                {
                    var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(ApiOffIconPath);
                    if (sprite != null) _apiOffIcon = sprite.texture;
                }
            }

            if (_themeIcon == null)
            {
                _themeIcon = AssetDatabase.LoadAssetAtPath<Texture2D>(ThemeIconPath);
                if (_themeIcon == null)
                {
                    var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(ThemeIconPath);
                    if (sprite != null) _themeIcon = sprite.texture;
                }
            }

            if (_helpIcon == null)
            {
                _helpIcon = AssetDatabase.LoadAssetAtPath<Texture2D>(HelpIconPath);
                if (_helpIcon == null)
                {
                    var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(HelpIconPath);
                    if (sprite != null) _helpIcon = sprite.texture;
                }
            }
        }

        private Image CreateClickableIconImage(Texture2D texture, string tooltip, Action onClick)
        {
            var image = new Image();
            image.image = texture;
            image.scaleMode = ScaleMode.ScaleToFit;
            image.tooltip = tooltip;
            image.tintColor = Color.white;
            image.style.width = 18;
            image.style.height = 18;
            image.style.flexShrink = 0;
            image.style.opacity = 0.75f;
            image.style.unityTextAlign = TextAnchor.MiddleCenter;

            image.RegisterCallback<PointerEnterEvent>(_ =>
            {
                image.style.opacity = 1f;
            });
            image.RegisterCallback<PointerLeaveEvent>(_ =>
            {
                image.style.opacity = 0.75f;
            });

            image.RegisterCallback<PointerUpEvent>(evt =>
            {
                if (evt.button != 0) return;
                onClick?.Invoke();
                evt.StopPropagation();
            });

            return image;
        }
    }
}
