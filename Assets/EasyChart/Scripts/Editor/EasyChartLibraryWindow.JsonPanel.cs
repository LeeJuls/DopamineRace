using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using EasyChart;

namespace EasyChart.Editor
{
    public partial class EasyChartLibraryWindow
    {
        private const string JsonPanelCollapsedPrefsKey = "EasyChart.LibraryWindow.JsonPanelCollapsed";
        private bool _jsonPanelCollapsed;
        private bool _jsonExampleDirtyByUser;

        // Simplified JSON mode choices
        private static readonly List<string> JsonModeChoices = new List<string>
        {
            "Compact",   // Data values only, minimal payload
            "Standard",  // Names + structured data objects
            "Full"       // All metadata including axes, types
        };

        private ChartJsonMode _jsonMode = ChartJsonMode.Standard;

        private void BuildInjectionJsonPanel(VisualElement leftPanel)
        {
            if (leftPanel == null) return;

            _jsonPanelCollapsed = EditorPrefs.GetBool(JsonPanelCollapsedPrefsKey, false);

            _jsonExampleContainer = new VisualElement();
            void ApplyJsonPanelHeight()
            {
                _jsonExampleContainer.style.height = _jsonPanelCollapsed ? 200 : 520;
            }

            ApplyJsonPanelHeight();
            _jsonExampleContainer.style.flexShrink = 0;
            _jsonExampleContainer.style.flexDirection = FlexDirection.Column;
            _jsonExampleContainer.style.minHeight = 0;
            _jsonExampleContainer.style.borderTopWidth = 1;
            _jsonExampleContainer.style.borderTopColor = new Color(0.15f, 0.15f, 0.15f);
            _jsonExampleContainer.style.backgroundColor = new Color(0.20f, 0.20f, 0.20f);
            _jsonExampleContainer.style.paddingLeft = 6;
            _jsonExampleContainer.style.paddingRight = 6;
            _jsonExampleContainer.style.paddingTop = 6;
            _jsonExampleContainer.style.paddingBottom = 6;

            var jsonHeaderRow = new VisualElement();
            jsonHeaderRow.style.flexDirection = FlexDirection.Row;
            jsonHeaderRow.style.alignItems = Align.Center;
            jsonHeaderRow.style.paddingLeft = 10;
            jsonHeaderRow.style.paddingRight = 10;
            jsonHeaderRow.style.paddingTop = 5;
            jsonHeaderRow.style.paddingBottom = 5;
            jsonHeaderRow.style.backgroundColor = new Color(0.25f, 0.25f, 0.25f);

            var jsonHeader = new Label("JSON Injection");
            jsonHeader.style.unityFontStyleAndWeight = FontStyle.Bold;
            jsonHeaderRow.Add(jsonHeader);

            EnsureSharedIconsLoaded();

            var jsonHeaderSpacer = new VisualElement();
            jsonHeaderSpacer.style.flexGrow = 1;
            jsonHeaderRow.Add(jsonHeaderSpacer);

            var jsonHelpBtn = CreateClickableIconImage(_helpIcon, "Help", () => EasyChartManualWeb.OpenChapter("01_03-JsonInjectionPanel"));
            jsonHelpBtn.style.marginLeft = 0;
            jsonHelpBtn.style.marginBottom = 0;
            jsonHeaderRow.Add(jsonHelpBtn);

            // Collapse/Expand button using custom icons
            var maxIcon = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/EasyChart/Textures/Icon/max.png");
            var minIcon = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/EasyChart/Textures/Icon/min.png");
            Image jsonCollapseBtn = null;
            void UpdateCollapseIcon()
            {
                // Use max icon (□) for expand, min icon (-) for collapse
                jsonCollapseBtn.image = _jsonPanelCollapsed ? maxIcon : minIcon;
            }
            jsonCollapseBtn = new Image();
            jsonCollapseBtn.style.width = 16;
            jsonCollapseBtn.style.height = 16;
            jsonCollapseBtn.style.marginLeft = 6;
            jsonCollapseBtn.tooltip = _jsonPanelCollapsed ? "Expand panel" : "Collapse panel";
            jsonCollapseBtn.RegisterCallback<PointerUpEvent>(evt =>
            {
                _jsonPanelCollapsed = !_jsonPanelCollapsed;
                EditorPrefs.SetBool(JsonPanelCollapsedPrefsKey, _jsonPanelCollapsed);
                UpdateCollapseIcon();
                jsonCollapseBtn.tooltip = _jsonPanelCollapsed ? "Expand panel" : "Collapse panel";
                ApplyJsonPanelHeight();
            });
            UpdateCollapseIcon();
            jsonHeaderRow.Add(jsonCollapseBtn);

            _jsonApplyToChartButton = CreateClickableIconImage(_applyToChartIcon, "ApplyToChart", () => ApplyInjectionJsonToSelectedProfile());
            _jsonApplyToChartButton.style.marginLeft = 6;
            _jsonApplyToChartButton.style.marginBottom = 0;
            jsonHeaderRow.Add(_jsonApplyToChartButton);

            _jsonExampleContainer.Add(jsonHeaderRow);

            var jsonBtnRow = new VisualElement();
            jsonBtnRow.style.flexDirection = FlexDirection.Row;
            jsonBtnRow.style.marginTop = 6;
            jsonBtnRow.style.paddingRight = 6;
            jsonBtnRow.style.flexShrink = 0;
            jsonBtnRow.style.flexWrap = Wrap.Wrap;

            void UpdateApiToggleIcon()
            {
                if (_jsonApiToggleButton == null) return;
                _jsonApiToggleButton.image = _jsonUseApiEnvelope ? _apiOnIcon : _apiOffIcon;
            }

            _jsonApiToggleButton = CreateClickableIconImage(_apiOffIcon, "API Envelope", () =>
            {
                _jsonUseApiEnvelope = !_jsonUseApiEnvelope;
                UpdateApiToggleIcon();
                _jsonExampleDirtyByUser = false;
                UpdateInjectionJsonExample(forceOverwrite: true);
            });
            _jsonApiToggleButton.style.marginBottom = 4;
            jsonBtnRow.Add(_jsonApiToggleButton);

            UpdateApiToggleIcon();

            // Simplified single mode dropdown
            _jsonModeDropdown = new PopupField<string>(JsonModeChoices, 1); // Default to Standard
            _jsonModeDropdown.tooltip = "JSON Format Mode:\n• Compact: Data values only\n• Standard: Names + structured data\n• Full: All metadata";
            _jsonModeDropdown.style.width = 100;
            _jsonModeDropdown.style.marginLeft = 6;
            _jsonModeDropdown.style.marginBottom = 4;
            _jsonModeDropdown.RegisterValueChangedCallback(evt =>
            {
                _jsonMode = (ChartJsonMode)JsonModeChoices.IndexOf(evt.newValue);
                _jsonExampleDirtyByUser = false;
                UpdateInjectionJsonExample(forceOverwrite: true);
            });

            InjectPopupIcon(_jsonModeDropdown, _feedIcon, "feed-icon");
            jsonBtnRow.Add(_jsonModeDropdown);

            var jsonBtnSpacer = new VisualElement();
            jsonBtnSpacer.style.flexGrow = 1;
            jsonBtnRow.Add(jsonBtnSpacer);

            _jsonCopyButton = CreateClickableIconImage(_copyIcon, "Copy", () =>
            {
                if (_jsonExampleField == null) return;
                GUIUtility.systemCopyBuffer = _jsonExampleField.value;
            });
            _jsonCopyButton.style.marginLeft = 6;
            _jsonCopyButton.style.marginBottom = 4;
            jsonBtnRow.Add(_jsonCopyButton);

            _jsonExampleContainer.Add(jsonBtnRow);

            _jsonExampleScroll = new ScrollView(ScrollViewMode.Vertical);
            _jsonExampleScroll.style.flexGrow = 1;
            _jsonExampleScroll.style.flexShrink = 1;
            _jsonExampleScroll.style.flexBasis = 0;
            _jsonExampleScroll.style.minHeight = 0;
            _jsonExampleScroll.style.paddingLeft = 2;
            _jsonExampleScroll.style.paddingRight = 2;
            _jsonExampleScroll.style.paddingTop = 2;
            _jsonExampleScroll.style.paddingBottom = 2;
            _jsonExampleScroll.style.marginBottom = 0;
            _jsonExampleScroll.verticalScrollerVisibility = ScrollerVisibility.AlwaysVisible;
            _jsonExampleContainer.Add(_jsonExampleScroll);

            _jsonExampleScroll.contentContainer.style.flexGrow = 0;
            _jsonExampleScroll.contentContainer.style.flexShrink = 0;
            _jsonExampleScroll.contentContainer.style.minHeight = 0;

            _jsonExampleField = new TextField();
            _jsonExampleField.multiline = true;
            _jsonExampleField.isReadOnly = false;
            _jsonExampleField.style.flexGrow = 0;
            _jsonExampleField.style.flexShrink = 0;
            _jsonExampleField.style.minHeight = 0;
            _jsonExampleField.style.whiteSpace = WhiteSpace.Normal;
            _jsonExampleField.style.unityTextAlign = TextAnchor.UpperLeft;
            _jsonExampleField.RegisterValueChangedCallback(_ => { _jsonExampleDirtyByUser = true; });
            _jsonExampleScroll.Add(_jsonExampleField);

            leftPanel.Add(_jsonExampleContainer);

            UpdateJsonModeDropdown();
            _jsonExampleDirtyByUser = false;
            UpdateInjectionJsonExample(forceOverwrite: true);
        }

        private static void InjectPopupIcon(PopupField<string> popup, Texture2D icon, string iconElementName)
        {
            if (popup == null) return;
            if (icon == null) return;

            var input = popup.Q<VisualElement>(className: "unity-base-popup-field__input");
            if (input == null) return;

            if (!string.IsNullOrEmpty(iconElementName) && input.Q<VisualElement>(iconElementName) != null) return;

            input.style.position = Position.Relative;
            input.style.paddingLeft = 22;

            var img = new Image();
            img.name = iconElementName;
            img.image = icon;
            img.scaleMode = ScaleMode.ScaleToFit;
            img.style.position = Position.Absolute;
            img.style.left = 4;
            img.style.top = 2;
            img.style.width = 16;
            img.style.height = 16;
            input.Add(img);
        }

        private void UpdateInjectionJsonExample(bool forceOverwrite = false)
        {
            if (_jsonExampleField == null) return;
            if (!forceOverwrite && _jsonExampleDirtyByUser) return;

            if (_selectedProfile == null)
            {
                _jsonExampleField.value = string.Empty;
                return;
            }

            if (_selectedProfile.EnsureRuntimeData())
            {
                EditorUtility.SetDirty(_selectedProfile);
            }

            _jsonChartId = _selectedProfile.chartId;

            // Use simplified API
            var json = ChartJsonUtils.BuildJson(_selectedProfile, _jsonMode, _jsonChartId);
            _jsonExampleField.value = _jsonUseApiEnvelope ? ChartJsonUtils.WrapAsApiResponse(json) : json;
            _jsonExampleDirtyByUser = false;
        }

        private void ApplyInjectionJsonToSelectedProfile()
        {
            if (_selectedProfile == null) return;
            if (_jsonExampleField == null) return;

            if (_serializedProfile != null && _serializedProfile.hasModifiedProperties)
            {
                _serializedProfile.ApplyModifiedProperties();
            }

            string json = _jsonExampleField.value;
            if (string.IsNullOrWhiteSpace(json)) return;

            if (!ChartJsonUtils.TryDeserializeFeed(json, out var feed))
            {
                Debug.LogError("[EasyChartLibraryWindow] ApplyToChart failed: invalid JSON or unsupported format.");
                return;
            }

            bool allowMetaOverwrite = _jsonMode == ChartJsonMode.Full;
            bool changed = ChartJsonUtils.ApplyFeedToProfile(_selectedProfile, feed, allowMetaOverwrite);
            if (changed)
            {
                EditorUtility.SetDirty(_selectedProfile);
                AssetDatabase.SaveAssets();

                _serializedProfile?.Update();
                ScheduleRefreshSeriesList();
                ScheduleUpdatePreview();

                _jsonExampleDirtyByUser = false;
            }
        }

        private void UpdateJsonModeDropdown()
        {
            if (_jsonModeDropdown == null) return;
            int idx = (int)_jsonMode;
            if (idx >= 0 && idx < JsonModeChoices.Count)
                _jsonModeDropdown.SetValueWithoutNotify(JsonModeChoices[idx]);
        }
    }
}
