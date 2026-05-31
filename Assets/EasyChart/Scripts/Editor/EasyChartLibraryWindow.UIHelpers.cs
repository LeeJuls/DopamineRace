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

        // Current series index for sync feature (-1 means not in series context)
        private int _currentSeriesIndex = -1;

        private PropertyField AddBoundPropertyField(VisualElement parent, SerializedProperty prop)
        {
            if (parent == null) return null;
            if (prop == null) return null;

            // Special handling for textureFXLayers fields
            if (prop.name == "textureFXLayers" && prop.isArray)
            {
                int seriesIndex = _currentSeriesIndex;
                string propertyPath = prop.propertyPath;
                var fxLayersUI = TextureFXLayersEditorHelper.CreateTextureFXLayersUI(prop, "Texture FX Layers", () =>
                {
                    // Sync to other series of the same type if enabled
                    if (seriesIndex >= 0 && !_isSyncingSeriesProperties)
                    {
                        SyncSeriesProperty(seriesIndex, propertyPath);
                    }
                    ScheduleUpdatePreview();
                });
                parent.Add(fxLayersUI);
                return null;
            }

            var pf = CreateSyncablePropertyField(prop);
            parent.Add(pf);
            return pf;
        }

        private PropertyField CreateSyncablePropertyField(SerializedProperty prop)
        {
            if (prop == null) return null;

            int seriesIndex = _currentSeriesIndex;
            
            var pf = new PropertyField(prop);
            // Count this field so the event handler can distinguish bind-init events from real edits
            _seriesBindInitEventCount++;
            if (_serializedProfile != null) pf.Bind(_serializedProfile);
            pf.RegisterCallback<SerializedPropertyChangeEvent>(evt =>
            {
                if (_serializedProfile == null) return;
                if (pf.panel == null) return;

                // Each PropertyField fires exactly one SerializedPropertyChangeEvent during Bind().
                // Consume that token here so we never sync on initialization events, regardless of timing.
                if (_seriesBindInitEventCount > 0)
                {
                    _seriesBindInitEventCount--;
                    return;
                }
                
                // Sync to other series of the same type if enabled
                if (seriesIndex >= 0 && !_isSyncingSeriesProperties)
                {
                    SyncSeriesProperty(seriesIndex, evt.changedProperty.propertyPath);
                }
                
                ScheduleUpdatePreview();
            });
            return pf;
        }

        /// <summary>
        /// Create a VisualElement for a property, with special handling for textureFXLayers.
        /// Returns PropertyField for normal properties, or custom UI for textureFXLayers.
        /// </summary>
        private VisualElement CreatePropertyElement(SerializedProperty prop)
        {
            if (prop == null) return null;

            // Special handling for textureFXLayers fields
            if (prop.name == "textureFXLayers" && prop.isArray)
            {
                int seriesIndex = _currentSeriesIndex;
                string propertyPath = prop.propertyPath;
                return TextureFXLayersEditorHelper.CreateTextureFXLayersUI(prop, "Texture FX Layers", () =>
                {
                    // Sync to other series of the same type if enabled
                    if (seriesIndex >= 0 && !_isSyncingSeriesProperties)
                    {
                        SyncSeriesProperty(seriesIndex, propertyPath);
                    }
                    ScheduleUpdatePreview();
                });
            }

            return CreateSyncablePropertyField(prop);
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

            if (_proIcon == null)
            {
                _proIcon = AssetDatabase.LoadAssetAtPath<Texture2D>(ProIconPath);
                if (_proIcon == null)
                {
                    var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(ProIconPath);
                    if (sprite != null) _proIcon = sprite.texture;
                }
            }

            if (_supportIcon == null)
            {
                _supportIcon = AssetDatabase.LoadAssetAtPath<Texture2D>(SupportIconPath);
                if (_supportIcon == null)
                {
                    var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SupportIconPath);
                    if (sprite != null) _supportIcon = sprite.texture;
                }
            }

            if (_settingsIcon == null)
            {
                _settingsIcon = AssetDatabase.LoadAssetAtPath<Texture2D>(SettingsIconPath);
                if (_settingsIcon == null)
                {
                    var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SettingsIconPath);
                    if (sprite != null) _settingsIcon = sprite.texture;
                }
                // Fallback to Unity's settings icon
                if (_settingsIcon == null)
                {
                    _settingsIcon = EditorGUIUtility.FindTexture("SettingsIcon");
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

        private VisualElement CreateProHoverPopup()
        {
            var popup = CreateBaseHoverPopup();
            
            var titleLabel = new Label("EasyChart Pro");
            titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            titleLabel.style.fontSize = 15;
            titleLabel.style.marginBottom = 8;
            titleLabel.style.color = new Color(1f, 0.85f, 0.35f);
            popup.Add(titleLabel);

            // Chart Types section
            var chartTypesHeader = new Label("Chart Types:");
            chartTypesHeader.style.unityFontStyleAndWeight = FontStyle.Bold;
            chartTypesHeader.style.fontSize = 12;
            chartTypesHeader.style.color = new Color(1f, 1f, 1f);
            chartTypesHeader.style.marginBottom = 2;
            popup.Add(chartTypesHeader);

            var chartTypesLabel = new Label("• 3D Charts (Bar3D, Line3D, Pie3D)\n• Candlestick & OHLC\n• Waterfall & BoxPlot\n• Heatmap & Ring Chart");
            chartTypesLabel.style.whiteSpace = WhiteSpace.Normal;
            chartTypesLabel.style.fontSize = 11;
            chartTypesLabel.style.color = new Color(0.85f, 0.85f, 0.85f);
            chartTypesLabel.style.marginBottom = 6;
            chartTypesLabel.style.marginLeft = 4;
            popup.Add(chartTypesLabel);

            // Resources section
            var resourcesHeader = new Label("Resources:");
            resourcesHeader.style.unityFontStyleAndWeight = FontStyle.Bold;
            resourcesHeader.style.fontSize = 12;
            resourcesHeader.style.color = new Color(1f, 1f, 1f);
            resourcesHeader.style.marginBottom = 2;
            popup.Add(resourcesHeader);

            var resourcesLabel = new Label("• More Chart Templates Library\n• Font Library");
            resourcesLabel.style.whiteSpace = WhiteSpace.Normal;
            resourcesLabel.style.fontSize = 11;
            resourcesLabel.style.color = new Color(0.85f, 0.85f, 0.85f);
            resourcesLabel.style.marginBottom = 6;
            resourcesLabel.style.marginLeft = 4;
            popup.Add(resourcesLabel);

            // Animation section
            var animationHeader = new Label("Animation:");
            animationHeader.style.unityFontStyleAndWeight = FontStyle.Bold;
            animationHeader.style.fontSize = 12;
            animationHeader.style.color = new Color(1f, 1f, 1f);
            animationHeader.style.marginBottom = 2;
            popup.Add(animationHeader);

            var animationLabel = new Label("• Texture Animation Support\n• Animated Texture Library");
            animationLabel.style.whiteSpace = WhiteSpace.Normal;
            animationLabel.style.fontSize = 11;
            animationLabel.style.color = new Color(0.85f, 0.85f, 0.85f);
            animationLabel.style.marginBottom = 8;
            animationLabel.style.marginLeft = 4;
            popup.Add(animationLabel);

            // Support section
            var supportHeader = new Label("Pro Support:");
            supportHeader.style.unityFontStyleAndWeight = FontStyle.Bold;
            supportHeader.style.fontSize = 12;
            supportHeader.style.color = new Color(1f, 1f, 1f);
            supportHeader.style.marginBottom = 2;
            popup.Add(supportHeader);

            var supportLabel = new Label("• Faster Update Frequency\n• Priority Technical Support\n• Custom Modifications (Benefits Plugin Development)");
            supportLabel.style.whiteSpace = WhiteSpace.Normal;
            supportLabel.style.fontSize = 11;
            supportLabel.style.color = new Color(0.85f, 0.85f, 0.85f);
            supportLabel.style.marginBottom = 8;
            supportLabel.style.marginLeft = 4;
            popup.Add(supportLabel);

            var priceLabel = new Label("💰 $80 → $39.99 (50% OFF in 2026)");
            priceLabel.style.fontSize = 12;
            priceLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            priceLabel.style.color = new Color(0.3f, 1f, 0.5f);
            priceLabel.style.marginBottom = 4;
            popup.Add(priceLabel);

            var priceNote = new Label("⚠ Price may increase with future updates");
            priceNote.style.fontSize = 11;
            priceNote.style.color = new Color(1f, 0.6f, 0.3f);
            priceNote.style.marginBottom = 10;
            popup.Add(priceNote);

            var buyBtn = new Button(() =>
            {
                var settings = EasyChartSettings.Instance;
                Application.OpenURL(settings?.proVersionUrl ?? "https://assetstore.unity.com/packages/slug/348172");
            }) { text = "View on Asset Store" };
            buyBtn.style.backgroundColor = new Color(0.2f, 0.5f, 0.8f);
            buyBtn.style.color = Color.white;
            buyBtn.style.unityFontStyleAndWeight = FontStyle.Bold;
            buyBtn.style.height = 24;
            popup.Add(buyBtn);
            
            // Add Only Click toggle at the bottom
            AddOnlyClickToggleToPopup(popup);

            return popup;
        }

        private VisualElement CreateSupportHoverPopup()
        {
            var popup = CreateBaseHoverPopup();
            
            var titleLabel = new Label("Support (LTS)");
            titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            titleLabel.style.fontSize = 13;
            titleLabel.style.marginBottom = 8;
            titleLabel.style.color = new Color(0.3f, 0.8f, 1f);
            popup.Add(titleLabel);

            // ===== Review Section (First - Encourage positive reviews) =====
            var reviewHeader = new Label("⭐ Enjoying EasyChart?");
            reviewHeader.style.unityFontStyleAndWeight = FontStyle.Bold;
            reviewHeader.style.fontSize = 12;
            reviewHeader.style.color = new Color(1f, 0.85f, 0.3f);
            reviewHeader.style.marginBottom = 4;
            popup.Add(reviewHeader);

            var reviewNote = new Label("Your 5-star review means the world to us!\nIt helps other developers discover EasyChart.");
            reviewNote.style.whiteSpace = WhiteSpace.Normal;
            reviewNote.style.fontSize = 11;
            reviewNote.style.color = new Color(0.85f, 0.85f, 0.85f);
            reviewNote.style.marginBottom = 6;
            popup.Add(reviewNote);

            // Review buttons container
            var reviewBtnContainer = new VisualElement();
            reviewBtnContainer.style.flexDirection = FlexDirection.Row;
            reviewBtnContainer.style.justifyContent = Justify.SpaceBetween;
            reviewBtnContainer.style.marginBottom = 12;

            var litReviewBtn = new Button(() =>
            {
                var settings = EasyChartSettings.Instance;
                var url = settings?.liteVersionUrl ?? "https://assetstore.unity.com/packages/slug/your-lite-id";
                Application.OpenURL(url + "#reviews");
            }) { text = "⭐ Rate Lite" };
            litReviewBtn.style.backgroundColor = new Color(0.2f, 0.5f, 0.8f);
            litReviewBtn.style.color = Color.white;
            litReviewBtn.style.unityFontStyleAndWeight = FontStyle.Bold;
            litReviewBtn.style.height = 26;
            litReviewBtn.style.flexGrow = 1;
            litReviewBtn.style.marginRight = 4;
            reviewBtnContainer.Add(litReviewBtn);

            var proReviewBtn = new Button(() =>
            {
                var settings = EasyChartSettings.Instance;
                var url = settings?.proVersionUrl ?? "https://assetstore.unity.com/packages/slug/your-pro-id";
                Application.OpenURL(url + "#reviews");
            }) { text = "⭐ Rate Pro" };
            proReviewBtn.style.backgroundColor = new Color(0.8f, 0.5f, 0.1f);
            proReviewBtn.style.color = Color.white;
            proReviewBtn.style.unityFontStyleAndWeight = FontStyle.Bold;
            proReviewBtn.style.height = 26;
            proReviewBtn.style.flexGrow = 1;
            reviewBtnContainer.Add(proReviewBtn);

            popup.Add(reviewBtnContainer);

            // ===== Separator =====
            var separator = new VisualElement();
            separator.style.height = 1;
            separator.style.backgroundColor = new Color(0.4f, 0.4f, 0.4f, 0.5f);
            separator.style.marginBottom = 10;
            popup.Add(separator);

            // ===== Feedback Section (Second - Guide to email) =====
            var feedbackHeader = new Label("📧 Need Help or Have Feedback?");
            feedbackHeader.style.unityFontStyleAndWeight = FontStyle.Bold;
            feedbackHeader.style.fontSize = 12;
            feedbackHeader.style.color = new Color(0.85f, 0.95f, 1f);
            feedbackHeader.style.marginBottom = 4;
            popup.Add(feedbackHeader);

            var feedbackNote = new Label("For bugs, feature requests, or questions,\nplease email us for faster response:");
            feedbackNote.style.whiteSpace = WhiteSpace.Normal;
            feedbackNote.style.fontSize = 11;
            feedbackNote.style.color = new Color(0.85f, 0.85f, 0.85f);
            feedbackNote.style.marginBottom = 6;
            popup.Add(feedbackNote);

            // Email row
            var emailRow = new VisualElement();
            emailRow.style.flexDirection = FlexDirection.Row;
            emailRow.style.alignItems = Align.Center;
            emailRow.style.marginBottom = 8;
            
            var emailLink = new Label("📋 support@lumaxforge.com");
            emailLink.style.fontSize = 11;
            emailLink.style.color = new Color(0.4f, 0.7f, 1f);
            emailLink.style.unityFontStyleAndWeight = FontStyle.Bold;
            emailLink.tooltip = "Click to copy email address";
            emailLink.RegisterCallback<PointerUpEvent>(evt =>
            {
                GUIUtility.systemCopyBuffer = "support@lumaxforge.com";
                emailLink.text = "✓ Copied to clipboard!";
                emailLink.style.color = new Color(0.3f, 0.9f, 0.3f);
                // Reset after 2 seconds
                emailLink.schedule.Execute(() =>
                {
                    emailLink.text = "📋 support@lumaxforge.com";
                    emailLink.style.color = new Color(0.4f, 0.7f, 1f);
                }).ExecuteLater(2000);
            });
            emailLink.RegisterCallback<PointerEnterEvent>(_ => 
            {
                if (!emailLink.text.Contains("Copied"))
                    emailLink.style.color = new Color(0.6f, 0.85f, 1f);
            });
            emailLink.RegisterCallback<PointerLeaveEvent>(_ => 
            {
                if (!emailLink.text.Contains("Copied"))
                    emailLink.style.color = new Color(0.4f, 0.7f, 1f);
            });
            emailRow.Add(emailLink);
            
            var clickHint = new Label("(Click to copy)");
            clickHint.style.fontSize = 10;
            clickHint.style.color = new Color(0.6f, 0.8f, 1f);
            clickHint.style.marginLeft = 6;
            emailRow.Add(clickHint);
            
            popup.Add(emailRow);

            // Documentation button
            var docsBtn = new Button(() =>
            {
                Application.OpenURL("https://lumaxforge.com/en/docs/easy-chart-lit/");
            }) { text = "📖 Documentation" };
            docsBtn.style.backgroundColor = new Color(0.25f, 0.45f, 0.35f);
            docsBtn.style.color = Color.white;
            docsBtn.style.height = 22;
            popup.Add(docsBtn);
            
            // Add Only Click toggle at the bottom
            AddOnlyClickToggleToPopup(popup);

            return popup;
        }

        private VisualElement CreateBaseHoverPopup()
        {
            var popup = new VisualElement();
            popup.style.position = Position.Absolute;
            popup.style.backgroundColor = new Color(0.18f, 0.18f, 0.18f, 0.98f);
            popup.style.borderTopWidth = 1;
            popup.style.borderBottomWidth = 1;
            popup.style.borderLeftWidth = 1;
            popup.style.borderRightWidth = 1;
            popup.style.borderTopColor = new Color(0.4f, 0.4f, 0.4f);
            popup.style.borderBottomColor = new Color(0.4f, 0.4f, 0.4f);
            popup.style.borderLeftColor = new Color(0.4f, 0.4f, 0.4f);
            popup.style.borderRightColor = new Color(0.4f, 0.4f, 0.4f);
            popup.style.borderTopLeftRadius = 6;
            popup.style.borderTopRightRadius = 6;
            popup.style.borderBottomLeftRadius = 6;
            popup.style.borderBottomRightRadius = 6;
            popup.style.paddingTop = 10;
            popup.style.paddingBottom = 10;
            popup.style.paddingLeft = 14;
            popup.style.paddingRight = 14;
            popup.style.minWidth = 200;
            popup.style.maxWidth = 280;
            popup.style.display = DisplayStyle.None;

            // Keep popup visible when mouse enters it
            popup.RegisterCallback<PointerEnterEvent>(_ => 
            {
                popup.userData = true; // Mark as hovered
                popup.style.display = DisplayStyle.Flex;
            });
            popup.RegisterCallback<PointerLeaveEvent>(_ => 
            {
                popup.userData = false; // Mark as not hovered
                popup.style.display = DisplayStyle.None;
            });

            return popup;
        }
        
        private void AddOnlyClickToggleToPopup(VisualElement popup)
        {
            // Separator line
            var separator = new VisualElement();
            separator.style.height = 1;
            separator.style.backgroundColor = new Color(0.4f, 0.4f, 0.4f);
            separator.style.marginTop = 10;
            separator.style.marginBottom = 6;
            popup.Add(separator);
            
            // Only Click toggle
            var toggleContainer = new VisualElement();
            toggleContainer.style.flexDirection = FlexDirection.Column;
            toggleContainer.style.alignItems = Align.Center;

            var toggleRow = new VisualElement();
            toggleRow.style.flexDirection = FlexDirection.Row;
            toggleRow.style.alignItems = Align.Center;
            toggleRow.style.justifyContent = Justify.Center;

            var toggle = new Toggle();
            toggle.value = EditorPrefs.GetBool(PopupOnlyClickPrefsKey, false);
            toggle.style.marginRight = 6;
            toggle.RegisterValueChangedCallback(evt =>
            {
                EditorPrefs.SetBool(PopupOnlyClickPrefsKey, evt.newValue);
            });
            toggleRow.Add(toggle);

            var label = new Label("Show popup on click only");
            label.style.fontSize = 12;
            label.style.color = new Color(1f, 1f, 1f);
            toggleRow.Add(label);

            toggleContainer.Add(toggleRow);
            popup.Add(toggleContainer);
        }

        private void ShowHoverPopup(VisualElement popup, VisualElement anchor)
        {
            if (popup.parent == null)
            {
                rootVisualElement.Add(popup);
            }

            popup.style.display = DisplayStyle.Flex;
            
            // Store anchor reference for delayed hide check
            popup.userData = anchor;

            // Position below and to the left of anchor
            popup.schedule.Execute(() =>
            {
                var anchorRect = anchor.worldBound;
                var rootRect = rootVisualElement.worldBound;
                
                float popupWidth = popup.resolvedStyle.width > 0 ? popup.resolvedStyle.width : 250;
                
                // Always position to the left of anchor's right edge
                float left = anchorRect.xMax - rootRect.x - popupWidth;
                float top = anchorRect.yMax - rootRect.y;
                
                // Ensure popup doesn't go off the left edge
                if (left < 0) left = 0;
                
                popup.style.left = left;
                popup.style.top = top;
            }).ExecuteLater(0);
        }

        private void HideHoverPopup(VisualElement popup)
        {
            // Delay hiding to allow mouse to move into popup
            popup.schedule.Execute(() =>
            {
                if (popup.panel == null) return;
                if (popup.style.display == DisplayStyle.None) return;
                
                // Check if popup is being hovered
                if (popup.userData is bool isHovered && isHovered)
                {
                    return; // Don't hide if mouse is over popup
                }
                
                popup.style.display = DisplayStyle.None;
            }).ExecuteLater(200);
        }
    }
}
