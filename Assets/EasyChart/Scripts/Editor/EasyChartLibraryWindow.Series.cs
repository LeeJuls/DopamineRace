using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using EasyChart;
using EasyChart.Layers;

namespace EasyChart.Editor
{
    public partial class EasyChartLibraryWindow
    {
        private void RefreshSeriesList()
        {
            _seriesContainer.Unbind();
            _seriesContainer.Clear();

            if (_serializedProfile != null)
            {
                if (_serializedProfile.hasModifiedProperties)
                {
                    _serializedProfile.ApplyModifiedProperties();
                }
                _serializedProfile.Update();
                _seriesProperty = _serializedProfile.FindProperty("series");
            }
            if (_seriesProperty == null) return;

            // PASS 2: UI Generation
            for (int i = 0; i < _seriesProperty.arraySize; i++)
            {
                int index = i;
                var elementProp = _seriesProperty.GetArrayElementAtIndex(i);

                string foldoutKey = (_selectedProfile != null ? _selectedProfile.GetInstanceID().ToString() : "null") + ":" + index;

                var container = new VisualElement();
                container.style.borderTopWidth = 1;
                container.style.borderBottomWidth = 1;
                container.style.borderLeftWidth = 1;
                container.style.borderRightWidth = 1;

                var borderColor = new Color(0.1f, 0.1f, 0.1f);
                container.style.borderTopColor = borderColor;
                container.style.borderBottomColor = borderColor;
                container.style.borderLeftColor = borderColor;
                container.style.borderRightColor = borderColor;

                container.style.backgroundColor = new Color(0.22f, 0.22f, 0.22f);
                container.style.marginBottom = 8;
                container.style.paddingLeft = 8;
                container.style.paddingRight = 8;
                container.style.paddingTop = 8;
                container.style.paddingBottom = 8;
                container.style.borderTopLeftRadius = 4;
                container.style.borderTopRightRadius = 4;
                container.style.borderBottomLeftRadius = 4;
                container.style.borderBottomRightRadius = 4;

                var header = new VisualElement();
                header.style.flexDirection = FlexDirection.Row;
                header.style.marginBottom = 5;
                header.style.alignItems = Align.Center;

                // Attempt to get name from property
                var nameProp = elementProp.FindPropertyRelative("name");
                string title = nameProp != null ? nameProp.stringValue : $"Serie {index}";
                if (string.IsNullOrEmpty(title)) title = $"Serie {index}";
                bool expanded = true;
                if (_seriesFoldoutState.TryGetValue(foldoutKey, out bool storedExpanded)) expanded = storedExpanded;

                var foldBtn = new Button();
                foldBtn.text = expanded ? "▼" : "▶";
                foldBtn.style.width = 22;
                foldBtn.style.marginRight = 4;
                foldBtn.style.paddingLeft = 0;
                foldBtn.style.paddingRight = 0;
                foldBtn.style.unityTextAlign = TextAnchor.MiddleCenter;
                header.Add(foldBtn);

                var label = new Label(title);
                label.style.unityFontStyleAndWeight = FontStyle.Bold;
                label.style.alignSelf = Align.Center;
                header.Add(label);

                var headerSpacer = new VisualElement();
                headerSpacer.style.flexGrow = 1;
                header.Add(headerSpacer);

                string GetManualChapterIdForSerieType(SerieType t)
                {
                    switch (t)
                    {
                        case SerieType.Line: return "03_01-LineChart";
                        case SerieType.Bar:
                        case SerieType.HorizontalBar: return "03_02-BarChart";
                        case SerieType.Scatter: return "03_03-ScatterChart";
                        case SerieType.Heatmap: return "03_04-HeatmapChart";
                        case SerieType.Radar: return "03_05-RadarChart";
                        case SerieType.Pie:
                        case SerieType.Pie3D: return "03_06-PieChart";
                        case SerieType.RingChart: return "03_07-RingChart";
                        default: return null;
                    }
                }

                var serieHelpBtn = CreateClickableIconImage(_helpIcon, "Help", () =>
                {
                    var tp = elementProp != null ? elementProp.FindPropertyRelative("type") : null;
                    var t = tp != null ? (SerieType)tp.intValue : SerieType.Line;
                    string chapterId = GetManualChapterIdForSerieType(t);
                    if (!string.IsNullOrEmpty(chapterId))
                    {
                        EasyChartManualWeb.OpenChapter(chapterId);
                    }
                    else
                    {
                        EasyChartManualWeb.OpenChapter("02_06-SeriesPanel");
                    }
                });
                serieHelpBtn.style.marginLeft = 6;
                header.Add(serieHelpBtn);

                container.Add(header);

                var body = new VisualElement();
                body.style.display = expanded ? DisplayStyle.Flex : DisplayStyle.None;
                container.Add(body);

                foldBtn.clicked += () =>
                {
                    expanded = !expanded;
                    _seriesFoldoutState[foldoutKey] = expanded;
                    foldBtn.text = expanded ? "▼" : "▶";
                    body.style.display = expanded ? DisplayStyle.Flex : DisplayStyle.None;
                };

                // Name Field
                var nameField = new PropertyField(nameProp);
                nameField.Bind(_serializedProfile);
                nameField.RegisterCallback<SerializedPropertyChangeEvent>(evt =>
                {
                    if (_serializedProfile == null) return;
                    if (nameField.panel == null) return;
                    if (nameProp != null && nameProp.serializedObject.targetObject != null) label.text = nameProp.stringValue;
                    ScheduleUpdatePreview();
                });
                body.Add(nameField);

                var idProp = elementProp.FindPropertyRelative("id");
                VisualElement idRow = null;
                if (idProp != null)
                {
                    idRow = new VisualElement();
                    idRow.style.flexDirection = FlexDirection.Row;
                    idRow.style.alignItems = Align.Center;
                    idRow.style.width = Length.Percent(100);

                    var idField = new TextField("Serie Id");
                    idField.isReadOnly = true;
                    idField.style.flexGrow = 1f;
                    idField.style.flexBasis = 0;
                    idField.style.flexShrink = 1f;
                    idField.style.minWidth = 0;
                    idField.BindProperty(idProp);
                    idRow.Add(idField);

                    var copyBtn = new Button(() =>
                    {
                        if (_serializedProfile == null) return;
                        if (idRow.panel == null) return;
                        _serializedProfile.Update();
                        GUIUtility.systemCopyBuffer = idProp.stringValue;
                    })
                    { text = "Copy" };
                    copyBtn.style.width = 52;
                    copyBtn.style.marginLeft = 6;
                    copyBtn.style.flexShrink = 0;
                    idRow.Add(copyBtn);

                    body.Add(idRow);
                }

                // Type Field
                var typeProp = elementProp.FindPropertyRelative("type");

                SerieType ReadSerieType()
                {
                    if (typeProp == null) return SerieType.Line;
                    // Use intValue directly for non-contiguous enums
                    return (SerieType)typeProp.intValue;
                }

                void WriteSerieType(SerieType t)
                {
                    if (typeProp == null) return;
                    // Use intValue directly for non-contiguous enums
                    typeProp.intValue = (int)t;
                }

                var currentTypeForFlags = ReadSerieType();
                bool isScatter = currentTypeForFlags == SerieType.Scatter;
                bool isPie = currentTypeForFlags == SerieType.Pie || currentTypeForFlags == SerieType.Pie3D;
                bool isRingChart = currentTypeForFlags == SerieType.RingChart;
                bool isRadar = currentTypeForFlags == SerieType.Radar;
                bool isBar = currentTypeForFlags == SerieType.Bar;
                bool isHeatmap = currentTypeForFlags == SerieType.Heatmap;

                bool profileIsPolar = _selectedProfile != null && _selectedProfile.coordinateSystem == CoordinateSystemType.Polar2D;
                bool profileIsNone = _selectedProfile != null && _selectedProfile.coordinateSystem == CoordinateSystemType.None;
                var allowedTypes = SerieTypeEditorRegistry.GetAllowedTypes();

                SerieType currentType = ReadSerieType();

                if (!allowedTypes.Contains(currentType)) allowedTypes.Insert(0, currentType);

                Label warn = null;
                bool isPieType = currentType == SerieType.Pie || currentType == SerieType.RingChart || currentType == SerieType.Pie3D;
                bool typeCompatible = isPieType
                                      || (profileIsNone && isPieType)
                                      || (profileIsPolar && currentType == SerieType.Radar)
                                      || (!profileIsPolar && !profileIsNone && (currentType == SerieType.Line || currentType == SerieType.Bar || currentType == SerieType.HorizontalBar || currentType == SerieType.Scatter || currentType == SerieType.Heatmap));
                if (!typeCompatible)
                {
                    string cs = _selectedProfile != null ? _selectedProfile.coordinateSystem.ToString() : "<null>";
                    var warnBox = CreateHintBox(out warn);
                    warn.text = $"Type '{currentType}' is not compatible with CoordinateSystem '{cs}'. It will still be rendered, but axes/grid semantics may be inconsistent.";
                    warn.style.opacity = 0.9f;
                    body.Add(warnBox);
                }

                var typeRow = new VisualElement();
                typeRow.style.flexDirection = FlexDirection.Row;
                typeRow.style.alignItems = Align.Center;

                var typeLabel = new Label("Type");
                typeLabel.style.minWidth = 120;
                typeLabel.style.marginRight = 4;
                typeRow.Add(typeLabel);

                var typeMenu = new ToolbarMenu();
                typeMenu.text = currentType.ToString();
                typeMenu.style.flexGrow = 1;
                typeMenu.style.minWidth = 0;
                typeRow.Add(typeMenu);
                body.Add(typeRow);

                var proOnlyHintBox = CreateHintBox(out var proOnlyHint);
                proOnlyHint.style.opacity = 0.9f;
                proOnlyHintBox.style.display = DisplayStyle.None;
                body.Add(proOnlyHintBox);

                void HideProOnlyHintIfProInstalled()
                {
                    proOnlyHintBox.style.display = DisplayStyle.None;
                }

                HideProOnlyHintIfProInstalled();

                bool IsBuiltinFreeType(SerieType t)
                {
                    return t == SerieType.Line
                           || t == SerieType.Bar
                           || t == SerieType.Pie
                           || t == SerieType.Scatter
                           || t == SerieType.Radar;
                }

                bool HasRegisteredImplementation(SerieType t)
                {
                    return SerieSettingsRegistry.HasFactory(t) && SerieRendererRegistry.HasFactory(t);
                }

                bool IsSelectableType(SerieType t)
                {
                    return IsBuiltinFreeType(t) || HasRegisteredImplementation(t);
                }

                // Polymorphic Settings Field
                var settingsProp = elementProp.FindPropertyRelative("settings");

                // Root Settings Foldout (contains all settings except SeriesData)
                string rootSettingsFoldoutKey = foldoutKey + ":settings:root";
                bool rootSettingsExpanded = true;
                if (_seriesFoldoutState.TryGetValue(rootSettingsFoldoutKey, out bool storedRootSettingsExpanded)) rootSettingsExpanded = storedRootSettingsExpanded;

                string rootSettingsTitle;
                if (currentTypeForFlags == SerieType.Bar) rootSettingsTitle = "BarSettings";
                else if (currentTypeForFlags == SerieType.Pie) rootSettingsTitle = "PieSettings";
                else if (currentTypeForFlags == SerieType.RingChart) rootSettingsTitle = "RingChartSettings";
                else if (currentTypeForFlags == SerieType.Pie3D) rootSettingsTitle = "Pie3DSettings";
                else if (currentTypeForFlags == SerieType.Heatmap) rootSettingsTitle = "HeatMapSettings";
                else rootSettingsTitle = currentTypeForFlags + "Settings";

                var rootSettingsFoldout = new Foldout
                {
                    text = rootSettingsTitle,
                    value = rootSettingsExpanded
                };
                rootSettingsFoldout.RegisterValueChangedCallback(evt =>
                {
                    _seriesFoldoutState[rootSettingsFoldoutKey] = evt.newValue;
                });

                var rootSettingsBox = CreateGroupBox();
                rootSettingsBox.Add(rootSettingsFoldout);
                body.Add(rootSettingsBox);

                VisualElement settingsRoot = rootSettingsFoldout;

                // Flatten settings properties to ensure they are visible
                if (settingsProp != null)
                {
                    Foldout barMiscFoldout = null;
                    VisualElement barMiscContainer = null;
                    if (isBar)
                    {
                        string barMiscFoldoutKey = foldoutKey + ":settings:bar:misc";
                        bool barMiscExpanded = true;
                        if (_seriesFoldoutState.TryGetValue(barMiscFoldoutKey, out bool storedBarMiscExpanded)) barMiscExpanded = storedBarMiscExpanded;

                        barMiscFoldout = new Foldout
                        {
                            text = "Bar",
                            value = barMiscExpanded
                        };
                        barMiscFoldout.RegisterValueChangedCallback(evt =>
                        {
                            _seriesFoldoutState[barMiscFoldoutKey] = evt.newValue;
                        });

                        barMiscContainer = barMiscFoldout;
                        var barMiscBox = CreateGroupBox();
                        barMiscBox.Add(barMiscFoldout);
                        settingsRoot.Add(barMiscBox);
                    }

                    if (isRingChart)
                    {
                        string ringFoldoutKey = foldoutKey + ":settings:ring";
                        bool ringExpanded = true;
                        if (_seriesFoldoutState.TryGetValue(ringFoldoutKey, out bool storedRingExpanded)) ringExpanded = storedRingExpanded;

                        var ringFoldout = new Foldout
                        {
                            text = "Ring",
                            value = ringExpanded
                        };
                        ringFoldout.RegisterValueChangedCallback(evt =>
                        {
                            _seriesFoldoutState[ringFoldoutKey] = evt.newValue;
                        });

                        var layoutProp = settingsProp.FindPropertyRelative("layout");
                        if (layoutProp != null)
                        {
                            var startAngleProp = layoutProp.FindPropertyRelative("startAngleDeg");
                            if (startAngleProp != null) AddBoundPropertyField(ringFoldout, startAngleProp);

                            var clockwiseProp = layoutProp.FindPropertyRelative("clockwise");
                            if (clockwiseProp != null) AddBoundPropertyField(ringFoldout, clockwiseProp);

                            var angleRangeProp = layoutProp.FindPropertyRelative("angleRangeDeg");
                            if (angleRangeProp != null) AddBoundPropertyField(ringFoldout, angleRangeProp);

                            var innerRadiusProp = layoutProp.FindPropertyRelative("innerRadius");
                            if (innerRadiusProp != null) AddBoundPropertyField(ringFoldout, innerRadiusProp);

                            var outerRadiusProp = layoutProp.FindPropertyRelative("outerRadius");
                            if (outerRadiusProp != null) AddBoundPropertyField(ringFoldout, outerRadiusProp);

                            var plotProp = layoutProp.FindPropertyRelative("plot");
                            if (plotProp != null)
                            {
                                var centerOffsetProp = plotProp.FindPropertyRelative("centerOffset");
                                if (centerOffsetProp != null) AddBoundPropertyField(ringFoldout, centerOffsetProp);

                                var paddingProp = plotProp.FindPropertyRelative("padding");
                                if (paddingProp != null) AddBoundPropertyField(ringFoldout, paddingProp);
                            }
                        }

                        var valueMappingProp = settingsProp.FindPropertyRelative("valueMapping");
                        if (valueMappingProp != null)
                        {
                            var modeProp = valueMappingProp.FindPropertyRelative("mode");
                            if (modeProp != null) AddBoundPropertyField(ringFoldout, modeProp);

                            var autoRangeProp = valueMappingProp.FindPropertyRelative("autoRange");
                            var minProp = valueMappingProp.FindPropertyRelative("minValue");
                            var maxProp = valueMappingProp.FindPropertyRelative("maxValue");
                            if (autoRangeProp != null && minProp != null && maxProp != null)
                            {
                                var rangeContainer = new VisualElement();
                                rangeContainer.style.marginLeft = 8;
                                AddToggleContainer(ringFoldout, autoRangeProp, rangeContainer, false);
                                AddBoundPropertyField(rangeContainer, minProp);
                                AddBoundPropertyField(rangeContainer, maxProp);
                            }
                            else
                            {
                                if (autoRangeProp != null) AddBoundPropertyField(ringFoldout, autoRangeProp);
                                if (minProp != null) AddBoundPropertyField(ringFoldout, minProp);
                                if (maxProp != null) AddBoundPropertyField(ringFoldout, maxProp);
                            }
                        }

                        // Add Ring-specific properties
                        var showBackgroundProp = settingsProp.FindPropertyRelative("showBackground");
                        if (showBackgroundProp != null) AddBoundPropertyField(ringFoldout, showBackgroundProp);

                        var backgroundAlphaProp = settingsProp.FindPropertyRelative("backgroundAlpha");
                        if (backgroundAlphaProp != null) AddBoundPropertyField(ringFoldout, backgroundAlphaProp);

                        var backgroundColorProp = settingsProp.FindPropertyRelative("backgroundColor");
                        if (backgroundColorProp != null) AddBoundPropertyField(ringFoldout, backgroundColorProp);

                        var cornerRadiusProp = settingsProp.FindPropertyRelative("cornerRadius");
                        if (cornerRadiusProp != null) AddBoundPropertyField(ringFoldout, cornerRadiusProp);

                        var ringGapPxProp = settingsProp.FindPropertyRelative("ringGapPx");
                        if (ringGapPxProp != null) AddBoundPropertyField(ringFoldout, ringGapPxProp);

                        var ringBox = CreateGroupBox();
                        ringBox.Add(ringFoldout);
                        settingsRoot.Add(ringBox);
                    }

                    var settingsDepth = settingsProp.depth;
                    var childSettingsProp = settingsProp.Copy();
                    var hiddenRootAutoRangePaths = new HashSet<string>();

                    // Enter children
                    if (childSettingsProp.NextVisible(true))
                    {
                        while (childSettingsProp.depth > settingsDepth)
                        {
                            if (hiddenRootAutoRangePaths.Contains(childSettingsProp.propertyPath))
                            {
                                if (!childSettingsProp.NextVisible(false)) break;
                                continue;
                            }

                            if (childSettingsProp.name == "animations")
                            {
                                if (!childSettingsProp.NextVisible(false)) break;
                                continue;
                            }

                            if (childSettingsProp.name.StartsWith("_legacy"))
                            {
                                if (!childSettingsProp.NextVisible(false)) break;
                                continue;
                            }

                            // Hide sortByValue at root level for Pie - it will be shown inside Pie (layout) foldout
                            if (isPie && childSettingsProp.name == "sortByValue")
                            {
                                if (!childSettingsProp.NextVisible(false)) break;
                                continue;
                            }

                            if (isRingChart && (childSettingsProp.name == "showBackground" || childSettingsProp.name == "backgroundAlpha" || childSettingsProp.name == "backgroundColor" || childSettingsProp.name == "cornerRadius" || childSettingsProp.name == "ringGapPx"))
                            {
                                if (!childSettingsProp.NextVisible(false)) break;
                                continue;
                            }

                            // HeatmapSettings renderMode - show/hide mode-specific settings
                            if (isHeatmap && childSettingsProp.propertyType == SerializedPropertyType.Enum && childSettingsProp.name == "renderMode")
                            {
                                var renderModeProp = childSettingsProp.Copy();

                                // Find all mode-specific properties using settingsProp.FindPropertyRelative
                                var xSplitCountProp = settingsProp.FindPropertyRelative("xSplitCount");
                                var ySplitCountProp = settingsProp.FindPropertyRelative("ySplitCount");
                                var cellGapPxProp = settingsProp.FindPropertyRelative("cellGapPx");
                                var influenceModeProp2 = settingsProp.FindPropertyRelative("influenceMode");
                                var bleedProp2 = settingsProp.FindPropertyRelative("bleed");
                                var smoothProp2 = settingsProp.FindPropertyRelative("smooth");
                                var gradientProp = settingsProp.FindPropertyRelative("gradient");
                                var contourProp = settingsProp.FindPropertyRelative("contour");

                                // Hide all mode-specific properties from default iteration
                                if (xSplitCountProp != null) hiddenRootAutoRangePaths.Add(xSplitCountProp.propertyPath);
                                if (ySplitCountProp != null) hiddenRootAutoRangePaths.Add(ySplitCountProp.propertyPath);
                                if (cellGapPxProp != null) hiddenRootAutoRangePaths.Add(cellGapPxProp.propertyPath);
                                if (influenceModeProp2 != null) hiddenRootAutoRangePaths.Add(influenceModeProp2.propertyPath);
                                if (bleedProp2 != null) hiddenRootAutoRangePaths.Add(bleedProp2.propertyPath);
                                if (smoothProp2 != null) hiddenRootAutoRangePaths.Add(smoothProp2.propertyPath);
                                if (gradientProp != null) hiddenRootAutoRangePaths.Add(gradientProp.propertyPath);
                                if (contourProp != null) hiddenRootAutoRangePaths.Add(contourProp.propertyPath);

                                var renderModeField = new PropertyField(renderModeProp);
                                renderModeField.Bind(_serializedProfile);
                                settingsRoot.Add(renderModeField);

                                // Grid mode container
                                var gridContainer = new VisualElement();
                                gridContainer.style.marginLeft = 8;
                                if (xSplitCountProp != null) AddBoundPropertyField(gridContainer, xSplitCountProp.Copy());
                                if (ySplitCountProp != null) AddBoundPropertyField(gridContainer, ySplitCountProp.Copy());
                                if (cellGapPxProp != null) AddBoundPropertyField(gridContainer, cellGapPxProp.Copy());
                                if (influenceModeProp2 != null) AddBoundPropertyField(gridContainer, influenceModeProp2.Copy());

                                // Bleed/Smooth sub-containers for Grid mode
                                var bleedContainer2 = new VisualElement();
                                bleedContainer2.style.marginLeft = 8;
                                var smoothContainer2 = new VisualElement();
                                smoothContainer2.style.marginLeft = 8;
                                if (bleedProp2 != null) AddBoundPropertyField(bleedContainer2, bleedProp2.Copy());
                                if (smoothProp2 != null) AddBoundPropertyField(smoothContainer2, smoothProp2.Copy());
                                gridContainer.Add(bleedContainer2);
                                gridContainer.Add(smoothContainer2);

                                settingsRoot.Add(gridContainer);

                                // Gradient mode container
                                var gradientContainer = new VisualElement();
                                gradientContainer.style.marginLeft = 8;
                                if (gradientProp != null) AddBoundPropertyField(gradientContainer, gradientProp.Copy());
                                settingsRoot.Add(gradientContainer);

                                // Contour mode container
                                var contourContainer = new VisualElement();
                                contourContainer.style.marginLeft = 8;
                                if (contourProp != null) AddBoundPropertyField(contourContainer, contourProp.Copy());
                                if (gradientProp != null)
                                {
                                    var gradientForContour = new PropertyField(gradientProp.Copy());
                                    gradientForContour.Bind(_serializedProfile);
                                    contourContainer.Add(gradientForContour);
                                }
                                settingsRoot.Add(contourContainer);

                                void UpdateRenderModeVisibility()
                                {
                                    if (_serializedProfile == null) return;
                                    if (body.panel == null) return;
                                    _serializedProfile.Update();

                                    int mode = renderModeProp.enumValueIndex;
                                    bool isGrid = mode == 0;
                                    bool isGradient = mode == 1;
                                    bool isContour = mode == 2;

                                    gridContainer.style.display = isGrid ? DisplayStyle.Flex : DisplayStyle.None;
                                    gradientContainer.style.display = isGradient ? DisplayStyle.Flex : DisplayStyle.None;
                                    contourContainer.style.display = isContour ? DisplayStyle.Flex : DisplayStyle.None;

                                    // Update influence mode visibility within grid
                                    if (isGrid && influenceModeProp2 != null)
                                    {
                                        int influenceMode = influenceModeProp2.enumValueIndex;
                                        bleedContainer2.style.display = influenceMode == 1 ? DisplayStyle.Flex : DisplayStyle.None;
                                        smoothContainer2.style.display = influenceMode == 2 ? DisplayStyle.Flex : DisplayStyle.None;
                                    }
                                }

                                UpdateRenderModeVisibility();
                                renderModeField.RegisterCallback<SerializedPropertyChangeEvent>(evt =>
                                {
                                    UpdateRenderModeVisibility();
                                    ScheduleUpdatePreview();
                                });

                                // Track changes in all mode containers to update preview
                                gridContainer.RegisterCallback<SerializedPropertyChangeEvent>(evt =>
                                {
                                    UpdateRenderModeVisibility();
                                    ScheduleUpdatePreview();
                                });
                                gradientContainer.RegisterCallback<SerializedPropertyChangeEvent>(evt =>
                                {
                                    ScheduleUpdatePreview();
                                });
                                contourContainer.RegisterCallback<SerializedPropertyChangeEvent>(evt =>
                                {
                                    ScheduleUpdatePreview();
                                });

                                if (!childSettingsProp.NextVisible(false)) break;
                                continue;
                            }

                            if (childSettingsProp.propertyType == SerializedPropertyType.Enum && childSettingsProp.name == "influenceMode")
                            {
                                // Skip if already handled by renderMode (for Heatmap)
                                if (isHeatmap)
                                {
                                    if (!childSettingsProp.NextVisible(false)) break;
                                    continue;
                                }

                                var influenceModeProp = childSettingsProp.Copy();
                                var bleedProp = settingsProp.FindPropertyRelative("bleed");
                                var smoothProp = settingsProp.FindPropertyRelative("smooth");

                                if (bleedProp != null) hiddenRootAutoRangePaths.Add(bleedProp.propertyPath);
                                if (smoothProp != null) hiddenRootAutoRangePaths.Add(smoothProp.propertyPath);

                                var modeField = new PropertyField(influenceModeProp);
                                modeField.Bind(_serializedProfile);
                                settingsRoot.Add(modeField);

                                var bleedContainer = new VisualElement();
                                bleedContainer.style.marginLeft = 8;
                                var smoothContainer = new VisualElement();
                                smoothContainer.style.marginLeft = 8;

                                if (bleedProp != null) AddBoundPropertyField(bleedContainer, bleedProp.Copy());
                                if (smoothProp != null) AddBoundPropertyField(smoothContainer, smoothProp.Copy());

                                settingsRoot.Add(bleedContainer);
                                settingsRoot.Add(smoothContainer);

                                void UpdateInfluenceVisibility()
                                {
                                    if (_serializedProfile == null) return;
                                    if (body.panel == null) return;
                                    _serializedProfile.Update();

                                    int mode = influenceModeProp.enumValueIndex;
                                    bool showBleed = mode == 1;
                                    bool showSmooth = mode == 2;

                                    bleedContainer.style.display = showBleed ? DisplayStyle.Flex : DisplayStyle.None;
                                    smoothContainer.style.display = showSmooth ? DisplayStyle.Flex : DisplayStyle.None;
                                }

                                UpdateInfluenceVisibility();
                                modeField.RegisterCallback<SerializedPropertyChangeEvent>(evt =>
                                {
                                    UpdateInfluenceVisibility();
                                    ScheduleUpdatePreview();
                                });

                                if (!childSettingsProp.NextVisible(false)) break;
                                continue;
                            }

                            if (childSettingsProp.propertyType == SerializedPropertyType.Boolean && childSettingsProp.name == "autoRange")
                            {
                                var minProp = settingsProp.FindPropertyRelative("minValue");
                                var maxProp = settingsProp.FindPropertyRelative("maxValue");

                                if (minProp != null && maxProp != null)
                                {
                                    hiddenRootAutoRangePaths.Add(minProp.propertyPath);
                                    hiddenRootAutoRangePaths.Add(maxProp.propertyPath);

                                    var rangeContainer = new VisualElement();
                                    rangeContainer.style.marginLeft = 8;
                                    AddToggleContainer(settingsRoot, childSettingsProp.Copy(), rangeContainer, false);
                                    AddBoundPropertyField(rangeContainer, minProp.Copy());
                                    AddBoundPropertyField(rangeContainer, maxProp.Copy());

                                    if (!childSettingsProp.NextVisible(false)) break;
                                    continue;
                                }
                            }

                            if (isScatter)
                            {
                                string n = childSettingsProp.name;
                                if (n == "stroke" ||
                                    n == "area")
                                {
                                    if (!childSettingsProp.NextVisible(false)) break;
                                    continue;
                                }
                            }

                            if (childSettingsProp.propertyType == SerializedPropertyType.Generic &&
                                (childSettingsProp.name == "layout" || childSettingsProp.name == "radar" || childSettingsProp.name == "point" || childSettingsProp.name == "stroke" || childSettingsProp.name == "area" || childSettingsProp.name == "border" || childSettingsProp.name == "background" || childSettingsProp.name == "sizeMapping" || childSettingsProp.name == "hover" || childSettingsProp.name == "aggregation" || childSettingsProp.name == "ring" || childSettingsProp.name == "legend" || childSettingsProp.name == "valueMapping"))
                            {
                                if (isRingChart && childSettingsProp.name == "layout")
                                {
                                    // For Pie/Ring/Pie3D we draw layout first (above) with custom UI.
                                    if (!childSettingsProp.NextVisible(false)) break;
                                    continue;
                                }

                                if (isRingChart && childSettingsProp.name == "valueMapping")
                                {
                                    if (!childSettingsProp.NextVisible(false)) break;
                                    continue;
                                }

                                if (childSettingsProp.name == "ring")
                                {
                                    // For RingChart we draw it first (above), and for Pie we hide it.
                                    if (!childSettingsProp.NextVisible(false)) break;
                                    continue;
                                }

                                string settingsFoldoutKey = foldoutKey + ":settings:" + childSettingsProp.name;
                                bool settingsExpanded = true;
                                if (_seriesFoldoutState.TryGetValue(settingsFoldoutKey, out bool storedSettingsExpanded)) settingsExpanded = storedSettingsExpanded;

                                string settingsTitle = childSettingsProp.displayName;
                                if (childSettingsProp.name == "layout") settingsTitle = isPie ? "Pie" : "Layout";
                                if (childSettingsProp.name == "stroke") settingsTitle = currentTypeForFlags == SerieType.Line ? "Line" : "LineSettings";

                                var settingsFoldout = new Foldout
                                {
                                    text = settingsTitle,
                                    value = settingsExpanded
                                };
                                settingsFoldout.RegisterValueChangedCallback(evt =>
                                {
                                    _seriesFoldoutState[settingsFoldoutKey] = evt.newValue;
                                });

                                // For Pie layout foldout, add sortByValue at the top
                                if (isPie && childSettingsProp.name == "layout")
                                {
                                    var sortByValueProp = settingsProp.FindPropertyRelative("sortByValue");
                                    if (sortByValueProp != null)
                                    {
                                        AddBoundPropertyField(settingsFoldout, sortByValueProp);
                                    }
                                }

                                var parentDepth = childSettingsProp.depth;
                                var subProp = childSettingsProp.Copy();
                                var hiddenAutoRangePaths = new HashSet<string>();
                                if (subProp.NextVisible(true))
                                {
                                    while (subProp.depth > parentDepth)
                                    {
                                        if (hiddenAutoRangePaths.Contains(subProp.propertyPath))
                                        {
                                            if (!subProp.NextVisible(false)) break;
                                            continue;
                                        }

                                        if (subProp.name == "animations")
                                        {
                                            if (!subProp.NextVisible(false)) break;
                                            continue;
                                        }

                                        if (subProp.propertyType == SerializedPropertyType.Boolean && subProp.name == "autoRange")
                                        {
                                            var minProp = subProp.serializedObject.FindProperty(subProp.propertyPath.Replace(".autoRange", ".minValue"));
                                            var maxProp = subProp.serializedObject.FindProperty(subProp.propertyPath.Replace(".autoRange", ".maxValue"));

                                            if (minProp != null && maxProp != null)
                                            {
                                                hiddenAutoRangePaths.Add(minProp.propertyPath);
                                                hiddenAutoRangePaths.Add(maxProp.propertyPath);

                                                var rangeContainer = new VisualElement();
                                                rangeContainer.style.marginLeft = 8;
                                                AddToggleContainer(settingsFoldout, subProp.Copy(), rangeContainer, false);
                                                AddBoundPropertyField(rangeContainer, minProp.Copy());
                                                AddBoundPropertyField(rangeContainer, maxProp.Copy());

                                                if (!subProp.NextVisible(false)) break;
                                                continue;
                                            }
                                        }

                                        var subField = new PropertyField(subProp.Copy());
                                        subField.Bind(_serializedProfile);
                                        subField.RegisterCallback<SerializedPropertyChangeEvent>(evt =>
                                        {
                                            if (_serializedProfile == null) return;
                                            if (subField.panel == null) return;
                                            // Avoid capturing loop variables like `subProp` (may be advanced/disposed after the loop).
                                            // Only rely on the window's current SerializedObject.
                                            if (_serializedProfile.targetObject != null) ScheduleUpdatePreview();
                                        });
                                        settingsFoldout.Add(subField);

                                        if (!subProp.NextVisible(false)) break;
                                    }
                                }

                                var settingsBox = CreateGroupBox();
                                settingsBox.Add(settingsFoldout);
                                settingsRoot.Add(settingsBox);

                                if (!childSettingsProp.NextVisible(false)) break;
                                continue;
                            }

                            var pf = new PropertyField(childSettingsProp.Copy());
                            pf.Bind(_serializedProfile);
                            pf.RegisterCallback<SerializedPropertyChangeEvent>(evt =>
                            {
                                if (_serializedProfile == null) return;
                                if (pf.panel == null) return;
                                // Avoid capturing loop variables like `childSettingsProp` (may be advanced/disposed after the loop).
                                // Only rely on the window's current SerializedObject.
                                if (_serializedProfile.targetObject != null) ScheduleUpdatePreview();
                            });
                            if (isBar && barMiscContainer != null) barMiscContainer.Add(pf);
                            else settingsRoot.Add(pf);

                            if (!childSettingsProp.NextVisible(false)) break;
                        }
                    }
                }

                // Label Settings (Hidden for RingChart)
                var labelProp = elementProp.FindPropertyRelative("labelSettings");
                if (labelProp != null && !isRingChart)
                {
                    string labelFoldoutKey = foldoutKey + ":label";
                    bool labelExpanded = true;
                    if (_seriesFoldoutState.TryGetValue(labelFoldoutKey, out bool storedLabelExpanded)) labelExpanded = storedLabelExpanded;

                    var labelFoldout = new Foldout
                    {
                        text = "Label Settings",
                        value = labelExpanded
                    };
                    labelFoldout.RegisterValueChangedCallback(evt =>
                    {
                        _seriesFoldoutState[labelFoldoutKey] = evt.newValue;
                    });

                    var labelDepth = labelProp.depth;
                    var childLabelProp = labelProp.Copy();

                    if (childLabelProp.NextVisible(true))
                    {
                        while (childLabelProp.depth > labelDepth)
                        {
                            var pf = new PropertyField(childLabelProp.Copy());
                            pf.Bind(_serializedProfile);
                            pf.RegisterCallback<SerializedPropertyChangeEvent>(evt =>
                            {
                                if (_serializedProfile == null) return;
                                if (pf.panel == null) return;
                                ScheduleUpdatePreview();
                            });
                            labelFoldout.Add(pf);

                            if (!childLabelProp.NextVisible(false)) break;
                        }
                    }

                    var labelBox = CreateGroupBox();
                    labelBox.Add(labelFoldout);
                    if (settingsRoot != null) settingsRoot.Add(labelBox);
                    else body.Add(labelBox);
                }

                // Actual Data
                var dataPointsProp = elementProp.FindPropertyRelative("seriesData");
                if (dataPointsProp != null)
                {
                    var dataPointsField = new PropertyField(dataPointsProp);
                    dataPointsProp.isExpanded = true;
                    dataPointsField.Bind(_serializedProfile);
                    dataPointsField.RegisterCallback<SerializedPropertyChangeEvent>(evt =>
                    {
                        if (_serializedProfile == null) return;
                        if (dataPointsField.panel == null) return;
                        ScheduleUpdatePreview();
                    });
                    dataPointsField.RegisterCallback<AttachToPanelEvent>(_ =>
                    {
                        var arrayFoldout = dataPointsField.Q<Foldout>();
                        if (arrayFoldout == null) return;

                        arrayFoldout.value = true;

                        var toggle = arrayFoldout.Q<Toggle>();
                        if (toggle != null) toggle.style.display = DisplayStyle.None;

                        var content = arrayFoldout.Q<VisualElement>(className: "unity-foldout__content");
                        if (content != null) content.style.marginLeft = 0;
                    });
                    var dataBox = CreateGroupBox();
                    dataBox.Add(dataPointsField);
                    body.Add(dataBox);
                }

                // Type Change Logic: Swap Settings Instance
                void ApplySerieTypeChange(SerieType newType)
                {
                    if (_serializedProfile == null) return;
                    if (typeMenu.panel == null) return;

                    if (!IsSelectableType(newType))
                    {
                        proOnlyHint.text = $"{newType} is EasyChart Pro-only. Please install EasyChart Pro to use this chart type.";
                        proOnlyHintBox.style.display = DisplayStyle.Flex;
                        return;
                    }

                    proOnlyHintBox.style.display = DisplayStyle.None;

                    WriteSerieType(newType);
                    _serializedProfile.ApplyModifiedProperties(); // Save the enum change first

                    bool settingsAlreadyMatch = false;
                    if (settingsProp != null)
                    {
                        var currentSettings = settingsProp.managedReferenceValue;
                        switch (newType)
                        {
                            case SerieType.Line:
                                settingsAlreadyMatch = currentSettings is LineSettings;
                                break;
                            case SerieType.Scatter:
                                settingsAlreadyMatch = currentSettings is ScatterSettings;
                                break;
                            case SerieType.Bar:
                            case SerieType.HorizontalBar:
                                settingsAlreadyMatch = currentSettings is BarSettings;
                                break;
                            case SerieType.Heatmap:
                                settingsAlreadyMatch = currentSettings is HeatmapSettings;
                                break;
                            case SerieType.Pie:
                                settingsAlreadyMatch = currentSettings is PieSettings;
                                break;
                            case SerieType.RingChart:
                                settingsAlreadyMatch = currentSettings is RingChartSettings;
                                break;
                            case SerieType.Radar:
                                settingsAlreadyMatch = currentSettings is RadarSettings;
                                break;
                            case SerieType.Pie3D:
                                settingsAlreadyMatch = false;
                                break;
                        }
                    }

                    if (settingsAlreadyMatch)
                    {
                        // Also update runtime object when settings already match
                        if (_selectedProfile.series != null && index >= 0 && index < _selectedProfile.series.Count)
                        {
                            var s = _selectedProfile.series[index];
                            if (s != null) s.type = newType;
                        }
                        EditorUtility.SetDirty(_selectedProfile);
                        currentType = newType;
                        typeMenu.text = newType.ToString();
                        ScheduleUpdatePreview();
                        return;
                    }

                    // CRITICAL: Unbind this serie's container immediately to prevent ObjectDisposedException
                    // when managedReference structure changes
                    if (container != null && container.panel != null)
                    {
                        container.Unbind();
                    }

                    // Defer the integrity fix and UI rebuild to next tick.
                    // Changing managedReference while UI fields are still bound can invalidate SerializedProperty handles.
                    EditorApplication.delayCall += () =>
                    {
                        if (_selectedProfile == null || _serializedProfile == null) return;

                        if (_serializedProfile.hasModifiedProperties) _serializedProfile.ApplyModifiedProperties();

                        bool changed = false;
                        if (_selectedProfile.series != null && index >= 0 && index < _selectedProfile.series.Count)
                        {
                            var s = _selectedProfile.series[index];
                            if (s != null)
                            {
                                if (s.SetType(newType)) changed = true;
                            }
                        }

                        if (_selectedProfile.EnsureRuntimeData()) changed = true;

                        if (changed) EditorUtility.SetDirty(_selectedProfile);

                        _serializedProfile.Update();
                        ScheduleRefreshSeriesList();
                        ScheduleUpdatePreview();
                    };
                }

                for (int tIndex = 0; tIndex < allowedTypes.Count; tIndex++)
                {
                    var t = allowedTypes[tIndex];

                    bool selectable = IsSelectableType(t);
                    string labelText = selectable ? t.ToString() : $"{t} (Pro)";

                    typeMenu.menu.AppendAction(labelText,
                        _ => ApplySerieTypeChange(t),
                        _ =>
                        {
                            var status = DropdownMenuAction.Status.Normal;
                            if (t == currentType) status |= DropdownMenuAction.Status.Checked;
                            if (!selectable) status |= DropdownMenuAction.Status.Disabled;
                            return status;
                        });
                }

                typeMenu.RegisterCallback<PointerDownEvent>(_ => HideProOnlyHintIfProInstalled());

                // Footer Controls (Bottom Right)
                var footer = new VisualElement();
                footer.style.flexDirection = FlexDirection.Row;
                footer.style.justifyContent = Justify.FlexEnd;
                footer.style.marginTop = 5;
                footer.style.alignItems = Align.Center;

                var upBtn = new Button(() => {
                    _seriesProperty.MoveArrayElement(index, index - 1);
                    _serializedProfile.ApplyModifiedProperties();
                    ScheduleRefreshSeriesList();
                    ScheduleUpdatePreview();
                }) { text = "↑" };
                upBtn.style.width = 25;
                upBtn.style.marginRight = 2;
                upBtn.SetEnabled(index > 0);
                footer.Add(upBtn);

                var downBtn = new Button(() => {
                    _seriesProperty.MoveArrayElement(index, index + 1);
                    _serializedProfile.ApplyModifiedProperties();
                    ScheduleRefreshSeriesList();
                    ScheduleUpdatePreview();
                }) { text = "↓" };
                downBtn.style.width = 25;
                downBtn.style.marginRight = 2;
                downBtn.SetEnabled(index < _seriesProperty.arraySize - 1);
                footer.Add(downBtn);

                var removeBtn = new Button(() => {
                    _seriesProperty.DeleteArrayElementAtIndex(index);
                    _serializedProfile.ApplyModifiedProperties();
                    ScheduleRefreshSeriesList();
                    ScheduleUpdatePreview();
                }) { text = "X" };
                removeBtn.style.width = 25;
                footer.Add(removeBtn);

                container.Add(footer);

                _seriesContainer.Add(container);
            }

            Button addBtn = null;
            addBtn = new Button(() => {
                if (_serializedProfile == null) return;
                if (_selectedProfile == null) return;
                if (addBtn == null || addBtn.panel == null) return;

                if (_seriesContainer != null && _seriesContainer.panel != null)
                {
                    _seriesContainer.Unbind();
                }

                _serializedProfile.Update();
                _seriesProperty = _serializedProfile.FindProperty("series");
                if (_seriesProperty == null || !_seriesProperty.isArray) return;

                bool hadExisting = _seriesProperty.arraySize > 0;

                _seriesProperty.InsertArrayElementAtIndex(_seriesProperty.arraySize);
                _serializedProfile.ApplyModifiedProperties();

                _serializedProfile.Update();
                _seriesProperty = _serializedProfile.FindProperty("series");
                if (_seriesProperty == null || !_seriesProperty.isArray || _seriesProperty.arraySize <= 0) return;

                int newIndex = _seriesProperty.arraySize - 1;
                var newElement = _seriesProperty.GetArrayElementAtIndex(newIndex);

                var nameProp = newElement.FindPropertyRelative("name");
                if (nameProp != null)
                {
                    nameProp.stringValue = $"Serie {newIndex + 1}";
                }

                var visibleProp = newElement.FindPropertyRelative("visible");
                if (visibleProp != null)
                {
                    visibleProp.boolValue = true;
                }

                // Set default type based on coordinate system
                // NOTE:
                // If we already have at least one series, InsertArrayElementAtIndex(arraySize) duplicates the last element
                // (including type/settings). Do NOT override the type here, otherwise AddSeries always becomes Line.
                // Only set a default type when adding the very first series.
                if (!hadExisting)
                {
                    var coordinateSystemProp = _serializedProfile.FindProperty("coordinateSystem");
                    if (coordinateSystemProp != null)
                    {
                        var typeProp = newElement.FindPropertyRelative("type");

                        if (typeProp != null)
                        {
                            bool isPolar = coordinateSystemProp.enumValueIndex == 1;
                            if (isPolar)
                            {
                                typeProp.intValue = (int)SerieType.Radar;
                            }
                            else
                            {
                                typeProp.intValue = (int)SerieType.Line;
                            }
                        }
                    }
                }

                _serializedProfile.ApplyModifiedProperties();

                EditorApplication.delayCall += () =>
                {
                    if (_selectedProfile == null || _serializedProfile == null) return;

                    bool changed = false;
                    if (_selectedProfile.EnsureRuntimeData()) changed = true;

                    if (changed) EditorUtility.SetDirty(_selectedProfile);
                    _serializedProfile.Update();
                    ScheduleRefreshSeriesList();
                    ScheduleUpdatePreview();
                };
                ScheduleRefreshSeriesList();
                ScheduleUpdatePreview();
            }) { text = "+ Add Series" };
            addBtn.style.height = 30;
            addBtn.style.marginTop = 10;
            addBtn.style.unityFontStyleAndWeight = FontStyle.Bold;
            _seriesContainer.Add(addBtn);
        }
    }
}
