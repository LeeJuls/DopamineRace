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

        private static readonly List<string> JsonDatasModeChoices = new List<string>
        {
            "Values",
            "Standard",
            "Full"
        };

        private ChartJsonDatasMode _jsonDatasMode = ChartJsonDatasMode.Standard;
        private PopupField<string> _jsonDatasModeDropdown;

        private struct JsonExampleModeOption
        {
            public string label;
            public ChartJsonExampleMode mode;

            public JsonExampleModeOption(string label, ChartJsonExampleMode mode)
            {
                this.label = label;
                this.mode = mode;
            }
        }

        private static readonly List<JsonExampleModeOption> JsonExampleModeOptions = new List<JsonExampleModeOption>
        {
            new JsonExampleModeOption("Lite", ChartJsonExampleMode.Lite_Index),
            new JsonExampleModeOption("Standard / ID", ChartJsonExampleMode.Lite_ID),
            new JsonExampleModeOption("Standard / Default", ChartJsonExampleMode.Standard),
            new JsonExampleModeOption("Standard / With Axes", ChartJsonExampleMode.Standard_Axis),
            new JsonExampleModeOption("Full", ChartJsonExampleMode.Full)
        };

        private static readonly List<string> JsonExampleModeChoices = new List<string>
        {
            "Lite",
            "Standard / ID",
            "Standard / Default",
            "Standard / With Axes",
            "Full"
        };

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

            var jsonHeaderSpacer = new VisualElement();
            jsonHeaderSpacer.style.flexGrow = 1;
            jsonHeaderRow.Add(jsonHeaderSpacer);

            EnsureSharedIconsLoaded();

            Button jsonCollapseBtn = null;
            jsonCollapseBtn = new Button(() =>
            {
                _jsonPanelCollapsed = !_jsonPanelCollapsed;
                EditorPrefs.SetBool(JsonPanelCollapsedPrefsKey, _jsonPanelCollapsed);
                jsonCollapseBtn.text = _jsonPanelCollapsed ? "Max" : "Min";
                ApplyJsonPanelHeight();
            });
            jsonCollapseBtn.text = _jsonPanelCollapsed ? "Max" : "Min";
            jsonCollapseBtn.tooltip = "Toggle panel height";
            jsonCollapseBtn.style.height = 18;
            jsonCollapseBtn.style.minWidth = 44;
            jsonCollapseBtn.style.marginLeft = 6;
            jsonHeaderRow.Add(jsonCollapseBtn);

            _jsonApplyToChartButton = CreateClickableIconImage(_applyToChartIcon, "ApplyToChart", () => ApplyInjectionJsonToSelectedProfile());
            _jsonApplyToChartButton.style.marginLeft = 6;
            _jsonApplyToChartButton.style.marginBottom = 0;
            jsonHeaderRow.Add(_jsonApplyToChartButton);

            var jsonHelpBtn = CreateClickableIconImage(_helpIcon, "Help", () => EasyChartManualWeb.OpenChapter("01_03-JsonInjectionPanel"));
            jsonHelpBtn.style.marginLeft = 6;
            jsonHelpBtn.style.marginBottom = 0;
            jsonHeaderRow.Add(jsonHelpBtn);

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

            _jsonModeDropdown = new PopupField<string>(JsonExampleModeChoices, 0);
            _jsonModeDropdown.tooltip = "Feed Mode";
            _jsonModeDropdown.style.width = 150;
            _jsonModeDropdown.style.marginLeft = 6;
            _jsonModeDropdown.style.marginBottom = 4;
            _jsonModeDropdown.RegisterValueChangedCallback(evt =>
            {
                _jsonExampleMode = JsonExampleModeFromLabel(evt.newValue);
                _jsonExampleDirtyByUser = false;
                UpdateInjectionJsonExample(forceOverwrite: true);
            });

            InjectPopupIcon(_jsonModeDropdown, _feedIcon, "feed-icon");
            jsonBtnRow.Add(_jsonModeDropdown);

            _jsonDatasModeDropdown = new PopupField<string>(JsonDatasModeChoices, 1);
            _jsonDatasModeDropdown.tooltip = "Datas Format";
            _jsonDatasModeDropdown.style.width = 90;
            _jsonDatasModeDropdown.style.marginLeft = 6;
            _jsonDatasModeDropdown.style.marginBottom = 4;
            _jsonDatasModeDropdown.RegisterValueChangedCallback(evt =>
            {
                _jsonDatasMode = JsonDatasModeFromLabel(evt.newValue);
                _jsonExampleDirtyByUser = false;
                UpdateInjectionJsonExample(forceOverwrite: true);
            });

            InjectPopupIcon(_jsonDatasModeDropdown, _dataIcon, "data-icon");
            jsonBtnRow.Add(_jsonDatasModeDropdown);

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

            var json = ChartJsonUtils.BuildInjectionJson(_selectedProfile, _jsonChartId, _jsonExampleMode, _jsonDatasMode);
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

            bool allowMetaOverwrite = _jsonExampleMode == ChartJsonExampleMode.Full;
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

            var label = JsonExampleModeToLabel(_jsonExampleMode);
            _jsonModeDropdown.SetValueWithoutNotify(label);
        }

        private static string JsonExampleModeToLabel(ChartJsonExampleMode mode)
        {
            for (int i = 0; i < JsonExampleModeOptions.Count; i++)
            {
                if (JsonExampleModeOptions[i].mode == mode) return JsonExampleModeOptions[i].label;
            }

            return JsonExampleModeOptions != null && JsonExampleModeOptions.Count > 0
                ? JsonExampleModeOptions[0].label
                : "Lite_Index";
        }

        private static ChartJsonExampleMode JsonExampleModeFromLabel(string label)
        {
            if (string.IsNullOrEmpty(label)) return ChartJsonExampleMode.Lite_Index;

            for (int i = 0; i < JsonExampleModeOptions.Count; i++)
            {
                if (string.Equals(JsonExampleModeOptions[i].label, label, StringComparison.OrdinalIgnoreCase))
                {
                    return JsonExampleModeOptions[i].mode;
                }
            }

            return ChartJsonExampleMode.Lite_Index;
        }

        private static ChartJsonDatasMode JsonDatasModeFromLabel(string label)
        {
            if (string.IsNullOrEmpty(label)) return ChartJsonDatasMode.Standard;
            if (string.Equals(label, "Values", StringComparison.OrdinalIgnoreCase)) return ChartJsonDatasMode.Values;
            if (string.Equals(label, "Full", StringComparison.OrdinalIgnoreCase)) return ChartJsonDatasMode.Full;
            return ChartJsonDatasMode.Standard;
        }
    }
}
