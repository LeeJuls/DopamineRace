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
                _currentSeriesIndex = index; // Set current series index for sync feature
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
                        case SerieType.Gauge: return "03_08-GaugeChart";
                        case SerieType.Funnel: return "03_09-FunnelChart";
                        case SerieType.Waterfall: return "03_10-WaterfallChart";
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
                serieHelpBtn.style.marginLeft = 0;
                header.Add(serieHelpBtn);
                
                // Clone button - clone current series
                int cloneSourceIndex = index; // Capture for closure
                var serieCloneBtn = CreateClickableIconImage(_cloneIcon, "Clone this series", () =>
                {
                    if (_serializedProfile == null) return;
                    if (_seriesProperty == null || !_seriesProperty.isArray) return;
                    
                    _serializedProfile.Update();
                    
                    // Insert a new element after the current one
                    int insertIndex = cloneSourceIndex + 1;
                    _seriesProperty.InsertArrayElementAtIndex(cloneSourceIndex);
                    
                    // The inserted element is a copy of the source, now at insertIndex
                    // We need to update its name and id
                    var newElement = _seriesProperty.GetArrayElementAtIndex(insertIndex);
                    
                    var nameProp = newElement.FindPropertyRelative("name");
                    if (nameProp != null)
                    {
                        // Helper to check if name exists in other series
                        bool IsNameDuplicate(string name, int excludeIndex)
                        {
                            for (int si = 0; si < _seriesProperty.arraySize; si++)
                            {
                                if (si == excludeIndex) continue;
                                var otherNameProp = _seriesProperty.GetArrayElementAtIndex(si).FindPropertyRelative("name");
                                if (otherNameProp != null && otherNameProp.stringValue == name)
                                    return true;
                            }
                            return false;
                        }
                        
                        // Helper to generate next name
                        string GenerateNextName(string currentName)
                        {
                            var match = System.Text.RegularExpressions.Regex.Match(currentName, @"^(.+?)(\d+)$");
                            if (match.Success)
                            {
                                string prefix = match.Groups[1].Value;
                                int num = int.Parse(match.Groups[2].Value);
                                return prefix + (num + 1);
                            }
                            else
                            {
                                return currentName + " 1";
                            }
                        }
                        
                        // Generate unique name
                        string newName = GenerateNextName(nameProp.stringValue);
                        while (IsNameDuplicate(newName, insertIndex))
                        {
                            newName = GenerateNextName(newName);
                        }
                        
                        nameProp.stringValue = newName;
                    }
                    
                    // Generate a new unique id
                    var idProp = newElement.FindPropertyRelative("id");
                    if (idProp != null)
                    {
                        idProp.stringValue = System.Guid.NewGuid().ToString("N");
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
                });
                serieCloneBtn.style.marginLeft = 4;
                header.Add(serieCloneBtn);

                // Up/Down/Remove buttons moved to header (right side)
                var upBtn = new Button(() => {
                    _seriesProperty.MoveArrayElement(index, index - 1);
                    _serializedProfile.ApplyModifiedProperties();
                    ScheduleRefreshSeriesList();
                    ScheduleUpdatePreview();
                }) { text = "↑" };
                upBtn.style.width = 22;
                upBtn.style.marginLeft = 4;
                upBtn.SetEnabled(index > 0);
                header.Add(upBtn);

                var downBtn = new Button(() => {
                    _seriesProperty.MoveArrayElement(index, index + 1);
                    _serializedProfile.ApplyModifiedProperties();
                    ScheduleRefreshSeriesList();
                    ScheduleUpdatePreview();
                }) { text = "↓" };
                downBtn.style.width = 22;
                downBtn.style.marginLeft = 2;
                downBtn.SetEnabled(index < _seriesProperty.arraySize - 1);
                header.Add(downBtn);

                var removeBtn = new Button(() => {
                    _seriesProperty.DeleteArrayElementAtIndex(index);
                    _serializedProfile.ApplyModifiedProperties();
                    ScheduleRefreshSeriesList();
                    ScheduleUpdatePreview();
                }) { text = "X" };
                removeBtn.style.width = 22;
                removeBtn.style.marginLeft = 2;
                header.Add(removeBtn);

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

                // Name Field (not synced - name should be unique)
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
                    
                    // Check if this id is duplicated
                    bool IsDuplicateId(string currentId, int currentIndex)
                    {
                        if (string.IsNullOrEmpty(currentId)) return false;
                        if (_seriesProperty == null) return false;
                        for (int si = 0; si < _seriesProperty.arraySize; si++)
                        {
                            if (si == currentIndex) continue;
                            var otherIdProp = _seriesProperty.GetArrayElementAtIndex(si).FindPropertyRelative("id");
                            if (otherIdProp != null && otherIdProp.stringValue == currentId)
                                return true;
                        }
                        return false;
                    }
                    
                    // Warning button for duplicate id (only visible when duplicate)
                    var duplicateBtn = new Button(() =>
                    {
                        if (_serializedProfile == null) return;
                        _serializedProfile.Update();
                        idProp.stringValue = System.Guid.NewGuid().ToString("N");
                        _serializedProfile.ApplyModifiedProperties();
                        EditorUtility.SetDirty(_selectedProfile);
                        ScheduleRefreshSeriesList();
                    })
                    { text = "⚠", tooltip = "Duplicate ID detected! Click to generate a new unique ID." };
                    duplicateBtn.style.width = 24;
                    duplicateBtn.style.marginRight = 4;
                    duplicateBtn.style.flexShrink = 0;
                    duplicateBtn.style.backgroundColor = new Color(0.8f, 0.4f, 0.1f);
                    duplicateBtn.style.color = Color.white;
                    duplicateBtn.style.unityFontStyleAndWeight = FontStyle.Bold;
                    
                    // Check and set visibility
                    bool isDuplicate = IsDuplicateId(idProp.stringValue, index);
                    duplicateBtn.style.display = isDuplicate ? DisplayStyle.Flex : DisplayStyle.None;
                    idRow.Add(duplicateBtn);

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
                bool isLine = currentTypeForFlags == SerieType.Line;
                bool isScatter = currentTypeForFlags == SerieType.Scatter;
                bool isPie = currentTypeForFlags == SerieType.Pie;
                bool isPie3D = currentTypeForFlags == SerieType.Pie3D;
                bool isBar3D = currentTypeForFlags == SerieType.Bar3D;
                bool isLine3D = currentTypeForFlags == SerieType.Line3D;
                bool isRingChart = currentTypeForFlags == SerieType.RingChart;
                bool isRadar = currentTypeForFlags == SerieType.Radar;
                bool isBar = currentTypeForFlags == SerieType.Bar;
                bool isHeatmap = currentTypeForFlags == SerieType.Heatmap;
                bool isGauge = currentTypeForFlags == SerieType.Gauge;
                bool isFunnel = currentTypeForFlags == SerieType.Funnel;
                bool isWaterfall = currentTypeForFlags == SerieType.Waterfall;

                bool profileIsPolar = _selectedProfile != null && _selectedProfile.coordinateSystem == CoordinateSystemType.Polar2D;
                bool profileIsNone = _selectedProfile != null && _selectedProfile.coordinateSystem == CoordinateSystemType.None;
                var allowedTypes = SerieTypeEditorRegistry.GetAllowedTypes();

                SerieType currentType = ReadSerieType();

                if (!allowedTypes.Contains(currentType)) allowedTypes.Insert(0, currentType);

                Label warn = null;
                bool isPieType = currentType == SerieType.Pie || currentType == SerieType.RingChart || currentType == SerieType.Pie3D || currentType == SerieType.Gauge || currentType == SerieType.Funnel;
                bool is3DType = currentType == SerieType.Bar3D || currentType == SerieType.Pie3D || currentType == SerieType.Line3D || currentType == SerieType.Scatter3D;
                bool profileIsCartesian3D = _selectedProfile != null && _selectedProfile.coordinateSystem == CoordinateSystemType.Cartesian3D;
                bool typeCompatible = isPieType
                                      || (profileIsNone && isPieType)
                                      || (profileIsPolar && currentType == SerieType.Radar)
                                      || (profileIsCartesian3D && is3DType)
                                      || (!profileIsPolar && !profileIsNone && !profileIsCartesian3D && (currentType == SerieType.Line || currentType == SerieType.Bar || currentType == SerieType.HorizontalBar || currentType == SerieType.Scatter || currentType == SerieType.Heatmap || currentType == SerieType.Waterfall || currentType == SerieType.Candlestick || currentType == SerieType.OHLC || currentType == SerieType.BoxPlot));
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
                else if (currentTypeForFlags == SerieType.Gauge) rootSettingsTitle = "GaugeSettings";
                else if (currentTypeForFlags == SerieType.Funnel) rootSettingsTitle = "FunnelSettings";
                else if (currentTypeForFlags == SerieType.Waterfall) rootSettingsTitle = "WaterfallSettings";
                else rootSettingsTitle = currentTypeForFlags + "Settings";

                // Helper to create styled foldout with left highlight bar
                (VisualElement container, Foldout foldout) CreateSeriesFoldout(string title, string stateKey, bool defaultExpanded)
                {
                    var cont = new VisualElement();
                    bool expanded = _seriesFoldoutState.TryGetValue(stateKey, out bool storedVal) ? storedVal : defaultExpanded;
                    var fold = new Foldout { text = title };
                    fold.bindingPath = string.Empty;
                    EditorStyleHelper.ApplyExpandedStyle(cont, expanded, false);
                    fold.SetValueWithoutNotify(expanded);
                    bool expectedValue = expanded;
                    bool userInitiated = false;
                    fold.RegisterCallback<PointerDownEvent>(evt => userInitiated = true, TrickleDown.TrickleDown);
                    fold.RegisterValueChangedCallback(evt =>
                    {
                        evt.StopPropagation();
                        if (!userInitiated)
                        {
                            if (evt.newValue != expectedValue)
                            {
                                fold.SetValueWithoutNotify(expectedValue);
                                EditorStyleHelper.ApplyExpandedStyle(cont, expectedValue, false);
                            }
                            return;
                        }
                        userInitiated = false;
                        expectedValue = evt.newValue;
                        _seriesFoldoutState[stateKey] = evt.newValue;
                        EditorStyleHelper.ApplyExpandedStyle(cont, evt.newValue, evt.newValue);
                    });
                    EditorStyleHelper.RegisterFocusCallbacks(cont, fold);
                    cont.Add(fold);
                    return (cont, fold);
                }

                var (rootSettingsContainer, rootSettingsFoldout) = CreateSeriesFoldout(rootSettingsTitle, rootSettingsFoldoutKey, rootSettingsExpanded);

                var rootSettingsBox = CreateGroupBox();
                rootSettingsBox.Add(rootSettingsContainer);
                body.Add(rootSettingsBox);

                VisualElement settingsRoot = rootSettingsFoldout;

                // Flatten settings properties to ensure they are visible
                if (settingsProp != null)
                {
                    // Pie3D specific foldouts - placed at top
                    Foldout pie3DMainFoldout = null;
                    VisualElement pie3DMainContainer = null;
                    Foldout pie3DCameraFoldout = null;
                    VisualElement pie3DCameraContainer = null;
                    Foldout pie3DLightingFoldout = null;
                    VisualElement pie3DLightingContainer = null;
                    Foldout pie3DMaterialFoldout = null;
                    VisualElement pie3DMaterialContainer = null;
                    var pie3DHiddenProps = new HashSet<string>();

                    if (isPie3D)
                    {
                        // Pie3D main foldout (thickness, heightByValue, background settings)
                        string pie3DMainFoldoutKey = foldoutKey + ":settings:pie3d:main";
                        (pie3DMainContainer, pie3DMainFoldout) = CreateSeriesFoldout("Pie3D", pie3DMainFoldoutKey, true);

                        var thicknessProp = settingsProp.FindPropertyRelative("thickness");
                        var heightByValueProp = settingsProp.FindPropertyRelative("heightByValue");
                        var transparentBackgroundProp = settingsProp.FindPropertyRelative("transparentBackground");
                        var backgroundColorProp = settingsProp.FindPropertyRelative("backgroundColor");

                        if (thicknessProp != null) { AddBoundPropertyField(pie3DMainFoldout, thicknessProp); pie3DHiddenProps.Add(thicknessProp.propertyPath); }
                        if (heightByValueProp != null) { AddBoundPropertyField(pie3DMainFoldout, heightByValueProp); pie3DHiddenProps.Add(heightByValueProp.propertyPath); }
                        if (transparentBackgroundProp != null) { AddBoundPropertyField(pie3DMainFoldout, transparentBackgroundProp); pie3DHiddenProps.Add(transparentBackgroundProp.propertyPath); }
                        if (backgroundColorProp != null) { AddBoundPropertyField(pie3DMainFoldout, backgroundColorProp); pie3DHiddenProps.Add(backgroundColorProp.propertyPath); }

                        var pie3DMainBox = CreateGroupBox();
                        pie3DMainBox.Add(pie3DMainContainer);
                        settingsRoot.Add(pie3DMainBox);

                        // Layout foldout (between Pie3D and Camera)
                        string layoutFoldoutKey = foldoutKey + ":settings:pie3d:layout";
                        var (pie3DLayoutContainer, pie3DLayoutFoldout) = CreateSeriesFoldout("Layout", layoutFoldoutKey, true);

                        // Add sortByValue at the top of layout
                        var sortByValueProp = settingsProp.FindPropertyRelative("sortByValue");
                        if (sortByValueProp != null) { AddBoundPropertyField(pie3DLayoutFoldout, sortByValueProp); pie3DHiddenProps.Add(sortByValueProp.propertyPath); }

                        // Add only used layout properties (hide unused: innerRadiusColor, cornerRadius, sliceGapType, plot)
                        var layoutProp = settingsProp.FindPropertyRelative("layout");
                        if (layoutProp != null)
                        {
                            pie3DHiddenProps.Add(layoutProp.propertyPath);
                            
                            // Only add properties that are actually used by Pie3DSeriesRenderer
                            var startAngleProp = layoutProp.FindPropertyRelative("startAngleDeg");
                            var clockwiseProp = layoutProp.FindPropertyRelative("clockwise");
                            var angleRangeProp = layoutProp.FindPropertyRelative("angleRangeDeg");
                            var innerRadiusProp = layoutProp.FindPropertyRelative("innerRadius");
                            var outerRadiusProp = layoutProp.FindPropertyRelative("outerRadius");
                            var sliceGapProp = layoutProp.FindPropertyRelative("sliceGapPx");

                            if (startAngleProp != null) AddBoundPropertyField(pie3DLayoutFoldout, startAngleProp);
                            if (clockwiseProp != null) AddBoundPropertyField(pie3DLayoutFoldout, clockwiseProp);
                            if (angleRangeProp != null) AddBoundPropertyField(pie3DLayoutFoldout, angleRangeProp);
                            if (innerRadiusProp != null) AddBoundPropertyField(pie3DLayoutFoldout, innerRadiusProp);
                            if (outerRadiusProp != null) AddBoundPropertyField(pie3DLayoutFoldout, outerRadiusProp);
                            if (sliceGapProp != null) AddBoundPropertyField(pie3DLayoutFoldout, sliceGapProp);
                        }

                        var layoutBox = CreateGroupBox();
                        layoutBox.Add(pie3DLayoutContainer);
                        settingsRoot.Add(layoutBox);

                        // Camera foldout
                        string cameraFoldoutKey = foldoutKey + ":settings:pie3d:camera";
                        (pie3DCameraContainer, pie3DCameraFoldout) = CreateSeriesFoldout("Camera", cameraFoldoutKey, true);

                        var cameraYawProp = settingsProp.FindPropertyRelative("cameraYawDeg");
                        var cameraPitchProp = settingsProp.FindPropertyRelative("cameraPitchDeg");
                        var cameraDistanceProp = settingsProp.FindPropertyRelative("cameraDistance");
                        var cameraFovProp = settingsProp.FindPropertyRelative("cameraFov");
                        var cameraTargetProp = settingsProp.FindPropertyRelative("cameraTarget");
                        var cameraAutoRotateProp = settingsProp.FindPropertyRelative("cameraAutoRotate");
                        var cameraAutoRotateSpeedProp = settingsProp.FindPropertyRelative("cameraAutoRotateSpeed");

                        if (cameraYawProp != null) { AddBoundPropertyField(pie3DCameraFoldout, cameraYawProp); pie3DHiddenProps.Add(cameraYawProp.propertyPath); }
                        if (cameraPitchProp != null) { AddBoundPropertyField(pie3DCameraFoldout, cameraPitchProp); pie3DHiddenProps.Add(cameraPitchProp.propertyPath); }
                        if (cameraDistanceProp != null) { AddBoundPropertyField(pie3DCameraFoldout, cameraDistanceProp); pie3DHiddenProps.Add(cameraDistanceProp.propertyPath); }
                        if (cameraFovProp != null) { AddBoundPropertyField(pie3DCameraFoldout, cameraFovProp); pie3DHiddenProps.Add(cameraFovProp.propertyPath); }
                        if (cameraTargetProp != null) { AddBoundPropertyField(pie3DCameraFoldout, cameraTargetProp); pie3DHiddenProps.Add(cameraTargetProp.propertyPath); }
                        if (cameraAutoRotateProp != null) { AddBoundPropertyField(pie3DCameraFoldout, cameraAutoRotateProp); pie3DHiddenProps.Add(cameraAutoRotateProp.propertyPath); }
                        if (cameraAutoRotateSpeedProp != null) { AddBoundPropertyField(pie3DCameraFoldout, cameraAutoRotateSpeedProp); pie3DHiddenProps.Add(cameraAutoRotateSpeedProp.propertyPath); }

                        var cameraBox = CreateGroupBox();
                        cameraBox.Add(pie3DCameraContainer);
                        settingsRoot.Add(cameraBox);

                        // Lighting foldout
                        string lightingFoldoutKey = foldoutKey + ":settings:pie3d:lighting";
                        (pie3DLightingContainer, pie3DLightingFoldout) = CreateSeriesFoldout("Lighting", lightingFoldoutKey, true);

                        var lightYawProp = settingsProp.FindPropertyRelative("lightYawDeg");
                        var lightPitchProp = settingsProp.FindPropertyRelative("lightPitchDeg");
                        var lightIntensityProp = settingsProp.FindPropertyRelative("lightIntensity");
                        var ambientColorProp = settingsProp.FindPropertyRelative("ambientColor");
                        var lightColorProp = settingsProp.FindPropertyRelative("lightColor");

                        if (lightYawProp != null) { AddBoundPropertyField(pie3DLightingFoldout, lightYawProp); pie3DHiddenProps.Add(lightYawProp.propertyPath); }
                        if (lightPitchProp != null) { AddBoundPropertyField(pie3DLightingFoldout, lightPitchProp); pie3DHiddenProps.Add(lightPitchProp.propertyPath); }
                        if (lightIntensityProp != null) { AddBoundPropertyField(pie3DLightingFoldout, lightIntensityProp); pie3DHiddenProps.Add(lightIntensityProp.propertyPath); }
                        if (ambientColorProp != null) { AddBoundPropertyField(pie3DLightingFoldout, ambientColorProp); pie3DHiddenProps.Add(ambientColorProp.propertyPath); }
                        if (lightColorProp != null) { AddBoundPropertyField(pie3DLightingFoldout, lightColorProp); pie3DHiddenProps.Add(lightColorProp.propertyPath); }

                        var lightingBox = CreateGroupBox();
                        lightingBox.Add(pie3DLightingContainer);
                        settingsRoot.Add(lightingBox);

                        // Material foldout
                        string materialFoldoutKey = foldoutKey + ":settings:pie3d:material";
                        (pie3DMaterialContainer, pie3DMaterialFoldout) = CreateSeriesFoldout("Material", materialFoldoutKey, true);

                        var shaderPresetProp = settingsProp.FindPropertyRelative("shaderPreset");
                        var materialModeProp = settingsProp.FindPropertyRelative("materialMode");
                        var specularColorProp = settingsProp.FindPropertyRelative("specularColor");
                        var glossProp = settingsProp.FindPropertyRelative("gloss");
                        var rimColorProp = settingsProp.FindPropertyRelative("rimColor");
                        var rimPowerProp = settingsProp.FindPropertyRelative("rimPower");
                        var toonStepsProp = settingsProp.FindPropertyRelative("toonSteps");
                        var sliceAlphaProp = settingsProp.FindPropertyRelative("sliceAlpha");

                        if (shaderPresetProp != null) { AddBoundPropertyField(pie3DMaterialFoldout, shaderPresetProp); pie3DHiddenProps.Add(shaderPresetProp.propertyPath); }
                        if (materialModeProp != null) { AddBoundPropertyField(pie3DMaterialFoldout, materialModeProp); pie3DHiddenProps.Add(materialModeProp.propertyPath); }
                        if (specularColorProp != null) { AddBoundPropertyField(pie3DMaterialFoldout, specularColorProp); pie3DHiddenProps.Add(specularColorProp.propertyPath); }
                        if (glossProp != null) { AddBoundPropertyField(pie3DMaterialFoldout, glossProp); pie3DHiddenProps.Add(glossProp.propertyPath); }
                        if (rimColorProp != null) { AddBoundPropertyField(pie3DMaterialFoldout, rimColorProp); pie3DHiddenProps.Add(rimColorProp.propertyPath); }
                        if (rimPowerProp != null) { AddBoundPropertyField(pie3DMaterialFoldout, rimPowerProp); pie3DHiddenProps.Add(rimPowerProp.propertyPath); }
                        if (toonStepsProp != null) { AddBoundPropertyField(pie3DMaterialFoldout, toonStepsProp); pie3DHiddenProps.Add(toonStepsProp.propertyPath); }
                        if (sliceAlphaProp != null) { AddBoundPropertyField(pie3DMaterialFoldout, sliceAlphaProp); pie3DHiddenProps.Add(sliceAlphaProp.propertyPath); }

                        var materialBox = CreateGroupBox();
                        materialBox.Add(pie3DMaterialContainer);
                        settingsRoot.Add(materialBox);

                        // Hide ring property for Pie3D (not used)
                        var ringProp = settingsProp.FindPropertyRelative("ring");
                        if (ringProp != null) { pie3DHiddenProps.Add(ringProp.propertyPath); }
                    }

                    // Bar3D specific foldouts
                    var bar3DHiddenProps = new HashSet<string>();
                    if (isBar3D)
                    {
                        // Hide background property (always transparent by default)
                        var backgroundProp = settingsProp.FindPropertyRelative("background");
                        if (backgroundProp != null)
                        {
                            bar3DHiddenProps.Add(backgroundProp.propertyPath);
                        }

                        // Layout foldout
                        string bar3DLayoutFoldoutKey = foldoutKey + ":settings:bar3d:layout";
                        var (bar3DLayoutContainer, bar3DLayoutFoldout) = CreateSeriesFoldout("Layout", bar3DLayoutFoldoutKey, true);

                        var layoutProp = settingsProp.FindPropertyRelative("layout");
                        if (layoutProp != null)
                        {
                            bar3DHiddenProps.Add(layoutProp.propertyPath);
                            var barWidthProp = layoutProp.FindPropertyRelative("barWidth");
                            var barDepthProp = layoutProp.FindPropertyRelative("barDepth");
                            var barGapProp = layoutProp.FindPropertyRelative("barGap");
                            var cornerRadiusProp = layoutProp.FindPropertyRelative("cornerRadius");
                            var baseYProp = layoutProp.FindPropertyRelative("baseY");
                            var heightScaleProp = layoutProp.FindPropertyRelative("heightScale");

                            if (barWidthProp != null) AddBoundPropertyField(bar3DLayoutFoldout, barWidthProp);
                            if (barDepthProp != null) AddBoundPropertyField(bar3DLayoutFoldout, barDepthProp);
                            if (barGapProp != null) AddBoundPropertyField(bar3DLayoutFoldout, barGapProp);
                            if (cornerRadiusProp != null) AddBoundPropertyField(bar3DLayoutFoldout, cornerRadiusProp);
                            if (baseYProp != null) AddBoundPropertyField(bar3DLayoutFoldout, baseYProp);
                            if (heightScaleProp != null) AddBoundPropertyField(bar3DLayoutFoldout, heightScaleProp);
                        }

                        var bar3DLayoutBox = CreateGroupBox();
                        bar3DLayoutBox.Add(bar3DLayoutContainer);
                        settingsRoot.Add(bar3DLayoutBox);

                        // Camera foldout
                        string bar3DCameraFoldoutKey = foldoutKey + ":settings:bar3d:camera";
                        var (bar3DCameraContainer, bar3DCameraFoldout) = CreateSeriesFoldout("Camera", bar3DCameraFoldoutKey, true);

                        var cameraProp = settingsProp.FindPropertyRelative("camera");
                        if (cameraProp != null)
                        {
                            bar3DHiddenProps.Add(cameraProp.propertyPath);
                            var yawProp = cameraProp.FindPropertyRelative("yawDeg");
                            var pitchProp = cameraProp.FindPropertyRelative("pitchDeg");
                            var distanceProp = cameraProp.FindPropertyRelative("distance");
                            var fovProp = cameraProp.FindPropertyRelative("fov");
                            var targetProp = cameraProp.FindPropertyRelative("target");
                            var autoRotateProp = cameraProp.FindPropertyRelative("autoRotate");
                            var autoRotateSpeedProp = cameraProp.FindPropertyRelative("autoRotateSpeed");

                            if (yawProp != null) AddBoundPropertyField(bar3DCameraFoldout, yawProp);
                            if (pitchProp != null) AddBoundPropertyField(bar3DCameraFoldout, pitchProp);
                            if (distanceProp != null) AddBoundPropertyField(bar3DCameraFoldout, distanceProp);
                            if (fovProp != null) AddBoundPropertyField(bar3DCameraFoldout, fovProp);
                            if (targetProp != null) AddBoundPropertyField(bar3DCameraFoldout, targetProp);
                            if (autoRotateProp != null) AddBoundPropertyField(bar3DCameraFoldout, autoRotateProp);
                            if (autoRotateSpeedProp != null) AddBoundPropertyField(bar3DCameraFoldout, autoRotateSpeedProp);
                        }

                        var bar3DCameraBox = CreateGroupBox();
                        bar3DCameraBox.Add(bar3DCameraContainer);
                        settingsRoot.Add(bar3DCameraBox);

                        // Lighting foldout
                        string bar3DLightingFoldoutKey = foldoutKey + ":settings:bar3d:lighting";
                        var (bar3DLightingContainer, bar3DLightingFoldout) = CreateSeriesFoldout("Lighting", bar3DLightingFoldoutKey, true);

                        var lightingProp = settingsProp.FindPropertyRelative("lighting");
                        if (lightingProp != null)
                        {
                            bar3DHiddenProps.Add(lightingProp.propertyPath);
                            var ambientColorProp = lightingProp.FindPropertyRelative("ambientColor");
                            var lightColorProp = lightingProp.FindPropertyRelative("lightColor");
                            var specularColorProp = lightingProp.FindPropertyRelative("specularColor");
                            var glossProp = lightingProp.FindPropertyRelative("gloss");
                            var rimColorProp = lightingProp.FindPropertyRelative("rimColor");
                            var rimPowerProp = lightingProp.FindPropertyRelative("rimPower");
                            var toonStepsProp = lightingProp.FindPropertyRelative("toonSteps");
                            var lightYawProp = lightingProp.FindPropertyRelative("lightYawDeg");
                            var lightPitchProp = lightingProp.FindPropertyRelative("lightPitchDeg");
                            var lightIntensityProp = lightingProp.FindPropertyRelative("lightIntensity");

                            if (lightYawProp != null) AddBoundPropertyField(bar3DLightingFoldout, lightYawProp);
                            if (lightPitchProp != null) AddBoundPropertyField(bar3DLightingFoldout, lightPitchProp);
                            if (lightIntensityProp != null) AddBoundPropertyField(bar3DLightingFoldout, lightIntensityProp);
                            if (ambientColorProp != null) AddBoundPropertyField(bar3DLightingFoldout, ambientColorProp);
                            if (lightColorProp != null) AddBoundPropertyField(bar3DLightingFoldout, lightColorProp);
                            if (specularColorProp != null) AddBoundPropertyField(bar3DLightingFoldout, specularColorProp);
                            if (glossProp != null) AddBoundPropertyField(bar3DLightingFoldout, glossProp);
                            if (rimColorProp != null) AddBoundPropertyField(bar3DLightingFoldout, rimColorProp);
                            if (rimPowerProp != null) AddBoundPropertyField(bar3DLightingFoldout, rimPowerProp);
                            if (toonStepsProp != null) AddBoundPropertyField(bar3DLightingFoldout, toonStepsProp);
                        }

                        var bar3DLightingBox = CreateGroupBox();
                        bar3DLightingBox.Add(bar3DLightingContainer);
                        settingsRoot.Add(bar3DLightingBox);

                        // Rendering foldout
                        string bar3DRenderingFoldoutKey = foldoutKey + ":settings:bar3d:rendering";
                        var (bar3DRenderingContainer, bar3DRenderingFoldout) = CreateSeriesFoldout("Rendering", bar3DRenderingFoldoutKey, true);

                        // Add color and useSeriesColor to Rendering foldout
                        var colorProp = settingsProp.FindPropertyRelative("color");
                        var useSeriesColorProp = settingsProp.FindPropertyRelative("useSeriesColor");
                        if (colorProp != null) { AddBoundPropertyField(bar3DRenderingFoldout, colorProp); bar3DHiddenProps.Add(colorProp.propertyPath); }
                        if (useSeriesColorProp != null) { AddBoundPropertyField(bar3DRenderingFoldout, useSeriesColorProp); bar3DHiddenProps.Add(useSeriesColorProp.propertyPath); }

                        var renderingProp = settingsProp.FindPropertyRelative("rendering");
                        if (renderingProp != null)
                        {
                            bar3DHiddenProps.Add(renderingProp.propertyPath);
                            var shaderPresetProp = renderingProp.FindPropertyRelative("shaderPreset");
                            var materialModeProp = renderingProp.FindPropertyRelative("materialMode");
                            var barAlphaProp = renderingProp.FindPropertyRelative("barAlpha");

                            if (shaderPresetProp != null) AddBoundPropertyField(bar3DRenderingFoldout, shaderPresetProp);
                            if (materialModeProp != null) AddBoundPropertyField(bar3DRenderingFoldout, materialModeProp);
                            if (barAlphaProp != null) AddBoundPropertyField(bar3DRenderingFoldout, barAlphaProp);
                        }

                        var bar3DRenderingBox = CreateGroupBox();
                        bar3DRenderingBox.Add(bar3DRenderingContainer);
                        settingsRoot.Add(bar3DRenderingBox);

                        // Hover foldout
                        string bar3DHoverFoldoutKey = foldoutKey + ":settings:bar3d:hover";
                        var (bar3DHoverContainer, bar3DHoverFoldout) = CreateSeriesFoldout("Hover", bar3DHoverFoldoutKey, true);

                        var hoverProp = settingsProp.FindPropertyRelative("hover");
                        if (hoverProp != null)
                        {
                            bar3DHiddenProps.Add(hoverProp.propertyPath);
                            var enabledProp = hoverProp.FindPropertyRelative("enabled");
                            var highlightIntensityProp = hoverProp.FindPropertyRelative("highlightIntensity");
                            var scaleMultiplierProp = hoverProp.FindPropertyRelative("scaleMultiplier");

                            if (enabledProp != null) AddBoundPropertyField(bar3DHoverFoldout, enabledProp);
                            if (highlightIntensityProp != null) AddBoundPropertyField(bar3DHoverFoldout, highlightIntensityProp);
                            if (scaleMultiplierProp != null) AddBoundPropertyField(bar3DHoverFoldout, scaleMultiplierProp);
                        }

                        var bar3DHoverBox = CreateGroupBox();
                        bar3DHoverBox.Add(bar3DHoverContainer);
                        settingsRoot.Add(bar3DHoverBox);
                    }

                    // Line3D specific foldouts
                    var line3DHiddenProps = new HashSet<string>();
                    if (isLine3D)
                    {
                        // Hide background property (always transparent by default)
                        var backgroundProp = settingsProp.FindPropertyRelative("background");
                        if (backgroundProp != null)
                        {
                            line3DHiddenProps.Add(backgroundProp.propertyPath);
                        }

                        // Stroke foldout
                        string line3DStrokeFoldoutKey = foldoutKey + ":settings:line3d:stroke";
                        var (line3DStrokeContainer, line3DStrokeFoldout) = CreateSeriesFoldout("Line", line3DStrokeFoldoutKey, true);

                        var strokeProp = settingsProp.FindPropertyRelative("stroke");
                        if (strokeProp != null)
                        {
                            line3DHiddenProps.Add(strokeProp.propertyPath);
                            var lineTypeProp = strokeProp.FindPropertyRelative("lineType");
                            var lineWidthProp = strokeProp.FindPropertyRelative("width");
                            var lineColorProp = strokeProp.FindPropertyRelative("color");
                            var textureFillProp = strokeProp.FindPropertyRelative("textureFill");
                            var textureFXLayersProp = strokeProp.FindPropertyRelative("textureFXLayers");

                            if (lineTypeProp != null) AddBoundPropertyField(line3DStrokeFoldout, lineTypeProp);
                            if (lineWidthProp != null) AddBoundPropertyField(line3DStrokeFoldout, lineWidthProp);
                            if (lineColorProp != null) AddBoundPropertyField(line3DStrokeFoldout, lineColorProp);
                            if (textureFillProp != null) AddBoundPropertyField(line3DStrokeFoldout, textureFillProp);
                            if (textureFXLayersProp != null) AddBoundPropertyField(line3DStrokeFoldout, textureFXLayersProp);
                        }

                        var line3DStrokeBox = CreateGroupBox();
                        line3DStrokeBox.Add(line3DStrokeContainer);
                        settingsRoot.Add(line3DStrokeBox);

                        // Point foldout
                        string line3DPointFoldoutKey = foldoutKey + ":settings:line3d:point";
                        var (line3DPointContainer, line3DPointFoldout) = CreateSeriesFoldout("Point", line3DPointFoldoutKey, true);

                        var pointProp = settingsProp.FindPropertyRelative("point");
                        if (pointProp != null)
                        {
                            line3DHiddenProps.Add(pointProp.propertyPath);
                            var showProp = pointProp.FindPropertyRelative("show");
                            var sizeProp = pointProp.FindPropertyRelative("size");
                            var textureProp = pointProp.FindPropertyRelative("texture");
                            var colorProp = pointProp.FindPropertyRelative("color");
                            var pointTextureFXLayersProp = pointProp.FindPropertyRelative("textureFXLayers");

                            if (showProp != null) AddBoundPropertyField(line3DPointFoldout, showProp);
                            if (sizeProp != null) AddBoundPropertyField(line3DPointFoldout, sizeProp);
                            if (textureProp != null) AddBoundPropertyField(line3DPointFoldout, textureProp);
                            if (colorProp != null) AddBoundPropertyField(line3DPointFoldout, colorProp);
                            if (pointTextureFXLayersProp != null) AddBoundPropertyField(line3DPointFoldout, pointTextureFXLayersProp);
                        }

                        var line3DPointBox = CreateGroupBox();
                        line3DPointBox.Add(line3DPointContainer);
                        settingsRoot.Add(line3DPointBox);

                        // Camera foldout
                        string line3DCameraFoldoutKey = foldoutKey + ":settings:line3d:camera";
                        var (line3DCameraContainer, line3DCameraFoldout) = CreateSeriesFoldout("Camera", line3DCameraFoldoutKey, true);

                        var cameraProp = settingsProp.FindPropertyRelative("camera");
                        if (cameraProp != null)
                        {
                            line3DHiddenProps.Add(cameraProp.propertyPath);
                            var yawProp = cameraProp.FindPropertyRelative("yawDeg");
                            var pitchProp = cameraProp.FindPropertyRelative("pitchDeg");
                            var distanceProp = cameraProp.FindPropertyRelative("distance");
                            var fovProp = cameraProp.FindPropertyRelative("fov");
                            var targetProp = cameraProp.FindPropertyRelative("target");
                            var autoRotateProp = cameraProp.FindPropertyRelative("autoRotate");
                            var autoRotateSpeedProp = cameraProp.FindPropertyRelative("autoRotateSpeed");

                            if (yawProp != null) AddBoundPropertyField(line3DCameraFoldout, yawProp);
                            if (pitchProp != null) AddBoundPropertyField(line3DCameraFoldout, pitchProp);
                            if (distanceProp != null) AddBoundPropertyField(line3DCameraFoldout, distanceProp);
                            if (fovProp != null) AddBoundPropertyField(line3DCameraFoldout, fovProp);
                            if (targetProp != null) AddBoundPropertyField(line3DCameraFoldout, targetProp);
                            if (autoRotateProp != null) AddBoundPropertyField(line3DCameraFoldout, autoRotateProp);
                            if (autoRotateSpeedProp != null) AddBoundPropertyField(line3DCameraFoldout, autoRotateSpeedProp);
                        }

                        var line3DCameraBox = CreateGroupBox();
                        line3DCameraBox.Add(line3DCameraContainer);
                        settingsRoot.Add(line3DCameraBox);

                        // Hover foldout
                        string line3DHoverFoldoutKey = foldoutKey + ":settings:line3d:hover";
                        var (line3DHoverContainer, line3DHoverFoldout) = CreateSeriesFoldout("Hover", line3DHoverFoldoutKey, true);

                        var hoverProp = settingsProp.FindPropertyRelative("hover");
                        if (hoverProp != null)
                        {
                            line3DHiddenProps.Add(hoverProp.propertyPath);
                            var enabledProp = hoverProp.FindPropertyRelative("enabled");
                            var highlightIntensityProp = hoverProp.FindPropertyRelative("highlightIntensity");
                            var scaleMultiplierProp = hoverProp.FindPropertyRelative("scaleMultiplier");

                            if (enabledProp != null) AddBoundPropertyField(line3DHoverFoldout, enabledProp);
                            if (highlightIntensityProp != null) AddBoundPropertyField(line3DHoverFoldout, highlightIntensityProp);
                            if (scaleMultiplierProp != null) AddBoundPropertyField(line3DHoverFoldout, scaleMultiplierProp);
                        }

                        var line3DHoverBox = CreateGroupBox();
                        line3DHoverBox.Add(line3DHoverContainer);
                        settingsRoot.Add(line3DHoverBox);
                    }

                    // Scatter3D specific foldouts
                    var scatter3DHiddenProps = new HashSet<string>();
                    bool isScatter3D = currentTypeForFlags == SerieType.Scatter3D;
                    if (isScatter3D)
                    {
                        // Hide background property
                        var backgroundProp = settingsProp.FindPropertyRelative("background");
                        if (backgroundProp != null) scatter3DHiddenProps.Add(backgroundProp.propertyPath);

                        // Hide color and useSeriesColor - Point.textureFill has color settings
                        var colorProp = settingsProp.FindPropertyRelative("color");
                        var useSeriesColorProp = settingsProp.FindPropertyRelative("useSeriesColor");
                        if (colorProp != null) scatter3DHiddenProps.Add(colorProp.propertyPath);
                        if (useSeriesColorProp != null) scatter3DHiddenProps.Add(useSeriesColorProp.propertyPath);

                        // Camera foldout
                        string scatter3DCameraFoldoutKey = foldoutKey + ":settings:scatter3d:camera";
                        var (scatter3DCameraContainer, scatter3DCameraFoldout) = CreateSeriesFoldout("Camera", scatter3DCameraFoldoutKey, true);

                        var cameraProp = settingsProp.FindPropertyRelative("camera");
                        if (cameraProp != null)
                        {
                            scatter3DHiddenProps.Add(cameraProp.propertyPath);
                            var yawProp = cameraProp.FindPropertyRelative("yawDeg");
                            var pitchProp = cameraProp.FindPropertyRelative("pitchDeg");
                            var distanceProp = cameraProp.FindPropertyRelative("distance");
                            var fovProp = cameraProp.FindPropertyRelative("fov");
                            var targetProp = cameraProp.FindPropertyRelative("target");
                            var autoRotateProp = cameraProp.FindPropertyRelative("autoRotate");
                            var autoRotateSpeedProp = cameraProp.FindPropertyRelative("autoRotateSpeed");

                            if (yawProp != null) AddBoundPropertyField(scatter3DCameraFoldout, yawProp);
                            if (pitchProp != null) AddBoundPropertyField(scatter3DCameraFoldout, pitchProp);
                            if (distanceProp != null) AddBoundPropertyField(scatter3DCameraFoldout, distanceProp);
                            if (fovProp != null) AddBoundPropertyField(scatter3DCameraFoldout, fovProp);
                            if (targetProp != null) AddBoundPropertyField(scatter3DCameraFoldout, targetProp);
                            if (autoRotateProp != null) AddBoundPropertyField(scatter3DCameraFoldout, autoRotateProp);
                            if (autoRotateSpeedProp != null) AddBoundPropertyField(scatter3DCameraFoldout, autoRotateSpeedProp);
                        }

                        var scatter3DCameraBox = CreateGroupBox();
                        scatter3DCameraBox.Add(scatter3DCameraContainer);
                        settingsRoot.Add(scatter3DCameraBox);

                        // Point foldout
                        string scatter3DPointFoldoutKey = foldoutKey + ":settings:scatter3d:point";
                        var (scatter3DPointContainer, scatter3DPointFoldout) = CreateSeriesFoldout("Point", scatter3DPointFoldoutKey, true);

                        var pointProp = settingsProp.FindPropertyRelative("point");
                        if (pointProp != null)
                        {
                            scatter3DHiddenProps.Add(pointProp.propertyPath);
                            var sizeProp = pointProp.FindPropertyRelative("size");
                            var textureFillProp = pointProp.FindPropertyRelative("textureFill");

                            if (sizeProp != null) AddBoundPropertyField(scatter3DPointFoldout, sizeProp);
                            if (textureFillProp != null) AddBoundPropertyField(scatter3DPointFoldout, textureFillProp);
                        }

                        var scatter3DPointBox = CreateGroupBox();
                        scatter3DPointBox.Add(scatter3DPointContainer);
                        settingsRoot.Add(scatter3DPointBox);

                        // Size Mapping foldout
                        string scatter3DSizeMappingFoldoutKey = foldoutKey + ":settings:scatter3d:sizeMapping";
                        var (scatter3DSizeMappingContainer, scatter3DSizeMappingFoldout) = CreateSeriesFoldout("Size Mapping", scatter3DSizeMappingFoldoutKey, true);

                        var sizeMappingProp = settingsProp.FindPropertyRelative("sizeMapping");
                        if (sizeMappingProp != null)
                        {
                            scatter3DHiddenProps.Add(sizeMappingProp.propertyPath);
                            var enabledProp = sizeMappingProp.FindPropertyRelative("enabled");
                            var minSizeProp = sizeMappingProp.FindPropertyRelative("minSize");
                            var maxSizeProp = sizeMappingProp.FindPropertyRelative("maxSize");
                            var autoRangeProp = sizeMappingProp.FindPropertyRelative("autoRange");
                            var minValueProp = sizeMappingProp.FindPropertyRelative("minValue");
                            var maxValueProp = sizeMappingProp.FindPropertyRelative("maxValue");

                            if (enabledProp != null) AddBoundPropertyField(scatter3DSizeMappingFoldout, enabledProp);
                            if (minSizeProp != null) AddBoundPropertyField(scatter3DSizeMappingFoldout, minSizeProp);
                            if (maxSizeProp != null) AddBoundPropertyField(scatter3DSizeMappingFoldout, maxSizeProp);
                            if (autoRangeProp != null) AddBoundPropertyField(scatter3DSizeMappingFoldout, autoRangeProp);
                            if (minValueProp != null) AddBoundPropertyField(scatter3DSizeMappingFoldout, minValueProp);
                            if (maxValueProp != null) AddBoundPropertyField(scatter3DSizeMappingFoldout, maxValueProp);
                        }

                        var scatter3DSizeMappingBox = CreateGroupBox();
                        scatter3DSizeMappingBox.Add(scatter3DSizeMappingContainer);
                        settingsRoot.Add(scatter3DSizeMappingBox);

                        // Hover foldout
                        string scatter3DHoverFoldoutKey = foldoutKey + ":settings:scatter3d:hover";
                        var (scatter3DHoverContainer, scatter3DHoverFoldout) = CreateSeriesFoldout("Hover", scatter3DHoverFoldoutKey, true);

                        var hoverProp = settingsProp.FindPropertyRelative("hover");
                        if (hoverProp != null)
                        {
                            scatter3DHiddenProps.Add(hoverProp.propertyPath);
                            var hoverEnabledProp = hoverProp.FindPropertyRelative("enabled");
                            var scaleProp = hoverProp.FindPropertyRelative("scale");
                            var hoverTextureFillProp = hoverProp.FindPropertyRelative("textureFill");

                            if (hoverEnabledProp != null) AddBoundPropertyField(scatter3DHoverFoldout, hoverEnabledProp);
                            if (scaleProp != null) AddBoundPropertyField(scatter3DHoverFoldout, scaleProp);
                            if (hoverTextureFillProp != null) AddBoundPropertyField(scatter3DHoverFoldout, hoverTextureFillProp);
                        }

                        var scatter3DHoverBox = CreateGroupBox();
                        scatter3DHoverBox.Add(scatter3DHoverContainer);
                        settingsRoot.Add(scatter3DHoverBox);
                    }

                    Foldout barMiscFoldout = null;
                    VisualElement barMiscContainer = null;
                    if (isBar)
                    {
                        string barMiscFoldoutKey = foldoutKey + ":settings:bar:misc";
                        (barMiscContainer, barMiscFoldout) = CreateSeriesFoldout("Bar", barMiscFoldoutKey, true);
                        var barMiscBox = CreateGroupBox();
                        barMiscBox.Add(barMiscContainer);
                        settingsRoot.Add(barMiscBox);
                    }

                    if (isRingChart)
                    {
                        string ringFoldoutKey = foldoutKey + ":settings:ring";
                        var (ringContainer, ringFoldout) = CreateSeriesFoldout("Ring", ringFoldoutKey, true);

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
                        ringBox.Add(ringContainer);
                        settingsRoot.Add(ringBox);
                    }

                    var settingsDepth = settingsProp.depth;
                    var childSettingsProp = settingsProp.Copy();
                    var hiddenRootAutoRangePaths = new HashSet<string>();

                    // For Line type, hide stacked and legendColorSource from root level (they are shown in Line foldout)
                    if (isLine)
                    {
                        var stackedProp = settingsProp.FindPropertyRelative("stacked");
                        if (stackedProp != null) hiddenRootAutoRangePaths.Add(stackedProp.propertyPath);
                        var legendColorSourceProp = settingsProp.FindPropertyRelative("legendColorSource");
                        if (legendColorSourceProp != null) hiddenRootAutoRangePaths.Add(legendColorSourceProp.propertyPath);
                    }

                    // Enter children
                    if (childSettingsProp.NextVisible(true))
                    {
                        while (childSettingsProp.depth > settingsDepth)
                        {
                            // Skip properties already shown in Pie3D foldouts
                            if (isPie3D && pie3DHiddenProps.Contains(childSettingsProp.propertyPath))
                            {
                                if (!childSettingsProp.NextVisible(false)) break;
                                continue;
                            }

                            // Skip properties already shown in Bar3D foldouts
                            if (isBar3D && bar3DHiddenProps.Contains(childSettingsProp.propertyPath))
                            {
                                if (!childSettingsProp.NextVisible(false)) break;
                                continue;
                            }

                            // Skip properties already shown in Line3D foldouts
                            if (isLine3D && line3DHiddenProps.Contains(childSettingsProp.propertyPath))
                            {
                                if (!childSettingsProp.NextVisible(false)) break;
                                continue;
                            }

                            // Skip properties already shown in Scatter3D foldouts
                            if (isScatter3D && scatter3DHiddenProps.Contains(childSettingsProp.propertyPath))
                            {
                                if (!childSettingsProp.NextVisible(false)) break;
                                continue;
                            }

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

                            // Hide sortByValue at root level for Pie/Pie3D - it will be shown inside layout foldout
                            if ((isPie || isPie3D) && childSettingsProp.name == "sortByValue")
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

                                var renderModeField = CreateSyncablePropertyField(renderModeProp);
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
                                    var gradientForContour = CreateSyncablePropertyField(gradientProp.Copy());
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

                                var modeField = CreateSyncablePropertyField(influenceModeProp);
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

                                // Custom handling for Scatter hover with point.textureFXLayers
                                if (n == "hover")
                                {
                                    string hoverFoldoutKey = foldoutKey + ":settings:hover";
                                    var (hoverContainer, hoverFoldout) = CreateSeriesFoldout("Hover", hoverFoldoutKey, true);

                                    var hoverProp = childSettingsProp;
                                    var hoverEnabledProp = hoverProp.FindPropertyRelative("enabled");
                                    if (hoverEnabledProp != null) AddBoundPropertyField(hoverFoldout, hoverEnabledProp);

                                    // Handle hover.point with custom TextureFXLayers UI
                                    var hoverPointProp = hoverProp.FindPropertyRelative("point");
                                    if (hoverPointProp != null)
                                    {
                                        string hoverPointFoldoutKey = foldoutKey + ":settings:hover:point";
                                        var (hoverPointContainer, hoverPointFoldout) = CreateSeriesFoldout("Point", hoverPointFoldoutKey, true);

                                        var showProp = hoverPointProp.FindPropertyRelative("show");
                                        if (showProp != null) AddBoundPropertyField(hoverPointFoldout, showProp);

                                        var sizeProp = hoverPointProp.FindPropertyRelative("size");
                                        if (sizeProp != null) AddBoundPropertyField(hoverPointFoldout, sizeProp);

                                        var textureFillProp = hoverPointProp.FindPropertyRelative("textureFill");
                                        if (textureFillProp != null) AddBoundPropertyField(hoverPointFoldout, textureFillProp);

                                        var textureFXLayersProp = hoverPointProp.FindPropertyRelative("textureFXLayers");
                                        if (textureFXLayersProp != null) AddBoundPropertyField(hoverPointFoldout, textureFXLayersProp);

                                        var hoverPointBox = CreateGroupBox();
                                        hoverPointBox.Add(hoverPointContainer);
                                        hoverFoldout.Add(hoverPointBox);
                                    }

                                    var hoverBox = CreateGroupBox();
                                    hoverBox.Add(hoverContainer);
                                    settingsRoot.Add(hoverBox);

                                    if (!childSettingsProp.NextVisible(false)) break;
                                    continue;
                                }
                            }

                            if (childSettingsProp.propertyType == SerializedPropertyType.Generic &&
                                (childSettingsProp.name == "layout" || childSettingsProp.name == "radar" || childSettingsProp.name == "point" || childSettingsProp.name == "stroke" || childSettingsProp.name == "area" || childSettingsProp.name == "border" || childSettingsProp.name == "background" || childSettingsProp.name == "sizeMapping" || childSettingsProp.name == "hover" || childSettingsProp.name == "aggregation" || childSettingsProp.name == "ring" || childSettingsProp.name == "legend" || childSettingsProp.name == "valueMapping" ||
                                 childSettingsProp.name == "axis" || childSettingsProp.name == "progress" || childSettingsProp.name == "pointer" || childSettingsProp.name == "ticks" || childSettingsProp.name == "valueDisplay" ||
                                 childSettingsProp.name == "colors" || childSettingsProp.name == "connector" || childSettingsProp.name == "runningTotal" || childSettingsProp.name == "labels"))
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

                                if (isGauge && childSettingsProp.name == "legend")
                                {
                                    // Gauge does not support legend
                                    if (!childSettingsProp.NextVisible(false)) break;
                                    continue;
                                }

                                string settingsFoldoutKey = foldoutKey + ":settings:" + childSettingsProp.name;

                                // Use property name with capitalized first letter for easier API lookup
                                string propName = childSettingsProp.name;
                                string settingsTitle = char.ToUpper(propName[0]) + propName.Substring(1);

                                // For Line/Radar type, rename "Stroke" to "Line"
                                if ((isLine || isRadar) && propName == "stroke")
                                {
                                    settingsTitle = "Line";
                                }

                                var (settingsContainer, settingsFoldout) = CreateSeriesFoldout(settingsTitle, settingsFoldoutKey, true);

                                // For Line type stroke foldout, add lineType first, then stacked
                                if (isLine && propName == "stroke")
                                {
                                    // Add lineType first
                                    var lineTypeProp = childSettingsProp.FindPropertyRelative("lineType");
                                    if (lineTypeProp != null)
                                    {
                                        AddBoundPropertyField(settingsFoldout, lineTypeProp.Copy());
                                    }
                                    // Then add stacked
                                    var stackedProp = settingsProp.FindPropertyRelative("stacked");
                                    if (stackedProp != null)
                                    {
                                        AddBoundPropertyField(settingsFoldout, stackedProp.Copy());
                                    }
                                }

                                // For Pie/Pie3D layout foldout, add sortByValue at the top
                                if ((isPie || isPie3D) && childSettingsProp.name == "layout")
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

                                        // For Line type, skip lineType (already added at top)
                                        if (isLine && propName == "stroke" && subProp.name == "lineType")
                                        {
                                            if (!subProp.NextVisible(false)) break;
                                            continue;
                                        }

                                        // For Radar type, stroke texture/FX are not supported — hide them
                                        if (isRadar && propName == "stroke" && (subProp.name == "textureFill" || subProp.name == "textureFXLayers"))
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

                                        // Handle GaugeProgressSettings.overrideWidth - show width only when overrideWidth is true
                                        if (subProp.propertyType == SerializedPropertyType.Boolean && subProp.name == "overrideWidth")
                                        {
                                            var widthProp = subProp.serializedObject.FindProperty(subProp.propertyPath.Replace(".overrideWidth", ".width"));

                                            if (widthProp != null)
                                            {
                                                hiddenAutoRangePaths.Add(widthProp.propertyPath);

                                                var widthContainer = new VisualElement();
                                                widthContainer.style.marginLeft = 8;
                                                AddToggleContainer(settingsFoldout, subProp.Copy(), widthContainer, true);
                                                AddBoundPropertyField(widthContainer, widthProp.Copy());

                                                if (!subProp.NextVisible(false)) break;
                                                continue;
                                            }
                                        }

                                        var subField = CreatePropertyElement(subProp.Copy());
                                        if (subField != null) settingsFoldout.Add(subField);

                                        if (!subProp.NextVisible(false)) break;
                                    }
                                }

                                // For Line type stroke foldout, add legendColorSource at the end
                                if (isLine && propName == "stroke")
                                {
                                    var legendColorSourceProp = settingsProp.FindPropertyRelative("legendColorSource");
                                    if (legendColorSourceProp != null)
                                    {
                                        AddBoundPropertyField(settingsFoldout, legendColorSourceProp.Copy());
                                    }
                                }

                                var settingsBox = CreateGroupBox();
                                settingsBox.Add(settingsContainer);
                                settingsRoot.Add(settingsBox);

                                if (!childSettingsProp.NextVisible(false)) break;
                                continue;
                            }

                            // Skip Funnel label-related properties (they are shown in Label Settings)
                            if (isFunnel && (childSettingsProp.name == "showPercentage" || childSettingsProp.name == "showValue" || childSettingsProp.name == "labelPosition"))
                            {
                                if (!childSettingsProp.NextVisible(false)) break;
                                continue;
                            }

                            var pf = CreatePropertyElement(childSettingsProp.Copy());
                            if (pf != null)
                            {
                                if (isBar && barMiscContainer != null) barMiscContainer.Add(pf);
                                else settingsRoot.Add(pf);
                            }

                            if (!childSettingsProp.NextVisible(false)) break;
                        }
                    }
                }

                // Label Settings (Hidden for RingChart)
                var labelProp = elementProp.FindPropertyRelative("labelSettings");
                if (labelProp != null && !isRingChart)
                {
                    string labelFoldoutKey = foldoutKey + ":label";

                    var (labelContainer, labelFoldout) = CreateSeriesFoldout("Label Settings", labelFoldoutKey, true);

                    var labelDepth = labelProp.depth;
                    var childLabelProp = labelProp.Copy();

                    if (childLabelProp.NextVisible(true))
                    {
                        while (childLabelProp.depth > labelDepth)
                        {
                            // For Gauge, hide offset field (use ticks.labelOffset instead)
                            if (isGauge && childLabelProp.name == "offset")
                            {
                                // Skip - Gauge uses ticks.labelOffset for label positioning
                            }
                            else
                            {
                                var pf = CreateSyncablePropertyField(childLabelProp.Copy());
                                labelFoldout.Add(pf);
                            }

                            if (!childLabelProp.NextVisible(false)) break;
                        }
                    }

                    // Add Funnel-specific label properties to the label foldout
                    if (isFunnel && settingsProp != null)
                    {
                        var showPercentageProp = settingsProp.FindPropertyRelative("showPercentage");
                        var showValueProp = settingsProp.FindPropertyRelative("showValue");
                        var labelPositionProp = settingsProp.FindPropertyRelative("labelPosition");

                        if (showPercentageProp != null)
                        {
                            var pf = CreateSyncablePropertyField(showPercentageProp);
                            labelFoldout.Add(pf);
                        }
                        if (showValueProp != null)
                        {
                            var pf = CreateSyncablePropertyField(showValueProp);
                            labelFoldout.Add(pf);
                        }
                        if (labelPositionProp != null)
                        {
                            var pf = CreateSyncablePropertyField(labelPositionProp);
                            labelFoldout.Add(pf);
                        }
                    }

                    var labelBox = CreateGroupBox();
                    labelBox.Add(labelContainer);
                    if (settingsRoot != null) settingsRoot.Add(labelBox);
                    else body.Add(labelBox);
                }

                // Legend Override Settings (Hidden for Pie/Funnel/RingChart/Gauge - they use PieLegendSettings instead)
                bool hasPieLegend = isPie || isPie3D || isFunnel || isRingChart || isGauge;
                var legendOverrideProp = elementProp.FindPropertyRelative("legendOverride");
                if (legendOverrideProp != null && !hasPieLegend)
                {
                    string legendOverrideFoldoutKey = foldoutKey + ":legendOverride";

                    var (legendOverrideContainer, legendOverrideFoldout) = CreateSeriesFoldout("Legend Override", legendOverrideFoldoutKey, false);

                    var legendOverrideDepth = legendOverrideProp.depth;
                    var childLegendOverrideProp = legendOverrideProp.Copy();

                    if (childLegendOverrideProp.NextVisible(true))
                    {
                        while (childLegendOverrideProp.depth > legendOverrideDepth)
                        {
                            var pf = CreateSyncablePropertyField(childLegendOverrideProp.Copy());
                            legendOverrideFoldout.Add(pf);

                            if (!childLegendOverrideProp.NextVisible(false)) break;
                        }
                    }

                    var legendOverrideBox = CreateGroupBox();
                    legendOverrideBox.Add(legendOverrideContainer);
                    if (settingsRoot != null) settingsRoot.Add(legendOverrideBox);
                    else body.Add(legendOverrideBox);
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
                            case SerieType.Gauge:
                                settingsAlreadyMatch = false;
                                break;
                            case SerieType.Funnel:
                                settingsAlreadyMatch = false;
                                break;
                            case SerieType.Waterfall:
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

                // Up/Down/Remove buttons moved to header above
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
                
                // Generate a new unique id immediately
                // This is important because InsertArrayElementAtIndex duplicates the last element including its id
                var idProp = newElement.FindPropertyRelative("id");
                if (idProp != null)
                {
                    idProp.stringValue = System.Guid.NewGuid().ToString("N");
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
            
            // Reset current series index after building all series UI
            _currentSeriesIndex = -1;
        }

        private const double SeriesUIRebuildCooldown = 0.5;

        private void SyncSeriesProperty(int sourceIndex, string propertyPath)
        {
            if (_isSyncingSeriesProperties) return;
            if (_serializedProfile == null) return;
            if (_seriesProperty == null) return;
            if (!_seriesSyncEnabled) return;
            // Ignore bind-initialization events that fire in the frames after a UI rebuild
            if (_seriesUIRebuildEndTime >= 0 &&
                EditorApplication.timeSinceStartup - _seriesUIRebuildEndTime < SeriesUIRebuildCooldown)
                return;
            if (sourceIndex < 0 || sourceIndex >= _seriesProperty.arraySize) return;

            // Get source series type
            var sourceElement = _seriesProperty.GetArrayElementAtIndex(sourceIndex);
            var sourceTypeProp = sourceElement.FindPropertyRelative("type");
            if (sourceTypeProp == null) return;
            SerieType sourceType = (SerieType)sourceTypeProp.intValue;

            // Get the relative property path (remove the array element prefix)
            // propertyPath format: "series.Array.data[0].propertyName"
            string relativePath = ExtractRelativePropertyPath(propertyPath, sourceIndex);
            if (string.IsNullOrEmpty(relativePath)) return;

            // Skip syncing name, id, type, and data properties
            if (relativePath == "name" || relativePath == "id" || relativePath == "type" || 
                relativePath.StartsWith("data.") || relativePath.StartsWith("data[")) return;

            var sourceProp = sourceElement.FindPropertyRelative(relativePath);
            if (sourceProp == null) return;

            _isSyncingSeriesProperties = true;
            try
            {
                // Sync to all series of the same type
                for (int i = 0; i < _seriesProperty.arraySize; i++)
                {
                    if (i == sourceIndex) continue;

                    var targetElement = _seriesProperty.GetArrayElementAtIndex(i);
                    var targetTypeProp = targetElement.FindPropertyRelative("type");
                    if (targetTypeProp == null) continue;

                    SerieType targetType = (SerieType)targetTypeProp.intValue;
                    if (targetType != sourceType) continue;

                    var targetProp = targetElement.FindPropertyRelative(relativePath);
                    if (targetProp == null) continue;

                    CopySerializedPropertyValue(sourceProp, targetProp);
                }

                _serializedProfile.ApplyModifiedProperties();
            }
            finally
            {
                _isSyncingSeriesProperties = false;
            }
        }

        private string ExtractRelativePropertyPath(string fullPath, int arrayIndex)
        {
            // fullPath format: "series.Array.data[0].propertyName" or "series.Array.data[0].nested.property"
            string prefix = $"series.Array.data[{arrayIndex}].";
            if (fullPath.StartsWith(prefix))
            {
                return fullPath.Substring(prefix.Length);
            }
            return null;
        }

        private void CopySerializedPropertyValue(SerializedProperty source, SerializedProperty target)
        {
            if (source.propertyType != target.propertyType) return;

            // Handle arrays specially - always deep copy arrays
            if (source.isArray && target.isArray)
            {
                CopySerializedArray(source, target);
                return;
            }

            switch (source.propertyType)
            {
                case SerializedPropertyType.Integer:
                    target.intValue = source.intValue;
                    break;
                case SerializedPropertyType.Boolean:
                    target.boolValue = source.boolValue;
                    break;
                case SerializedPropertyType.Float:
                    target.floatValue = source.floatValue;
                    break;
                case SerializedPropertyType.String:
                    target.stringValue = source.stringValue;
                    break;
                case SerializedPropertyType.Color:
                    target.colorValue = source.colorValue;
                    break;
                case SerializedPropertyType.ObjectReference:
                    target.objectReferenceValue = source.objectReferenceValue;
                    break;
                case SerializedPropertyType.Enum:
                    target.enumValueIndex = source.enumValueIndex;
                    break;
                case SerializedPropertyType.Vector2:
                    target.vector2Value = source.vector2Value;
                    break;
                case SerializedPropertyType.Vector3:
                    target.vector3Value = source.vector3Value;
                    break;
                case SerializedPropertyType.Vector4:
                    target.vector4Value = source.vector4Value;
                    break;
                case SerializedPropertyType.Rect:
                    target.rectValue = source.rectValue;
                    break;
                case SerializedPropertyType.AnimationCurve:
                    target.animationCurveValue = source.animationCurveValue;
                    break;
                case SerializedPropertyType.Bounds:
                    target.boundsValue = source.boundsValue;
                    break;
                case SerializedPropertyType.Gradient:
                    // Gradient requires special handling - copy color keys and alpha keys
                    var sourceGradient = source.gradientValue;
                    if (sourceGradient != null)
                    {
                        var newGradient = new Gradient();
                        newGradient.SetKeys(sourceGradient.colorKeys, sourceGradient.alphaKeys);
                        newGradient.mode = sourceGradient.mode;
                        target.gradientValue = newGradient;
                    }
                    break;
                case SerializedPropertyType.Quaternion:
                    target.quaternionValue = source.quaternionValue;
                    break;
                case SerializedPropertyType.Vector2Int:
                    target.vector2IntValue = source.vector2IntValue;
                    break;
                case SerializedPropertyType.Vector3Int:
                    target.vector3IntValue = source.vector3IntValue;
                    break;
                case SerializedPropertyType.RectInt:
                    target.rectIntValue = source.rectIntValue;
                    break;
                case SerializedPropertyType.BoundsInt:
                    target.boundsIntValue = source.boundsIntValue;
                    break;
                case SerializedPropertyType.Generic:
                    // For generic types (like custom classes), do NOT copy recursively
                    // This prevents unintended side effects like copying Color when only Texture was changed
                    // Individual child properties will be synced when they are modified directly
                    // Exception: arrays are handled separately above
                    break;
            }
        }

        private void CopySerializedArray(SerializedProperty source, SerializedProperty target)
        {
            target.arraySize = source.arraySize;
            for (int i = 0; i < source.arraySize; i++)
            {
                var sourceElement = source.GetArrayElementAtIndex(i);
                var targetElement = target.GetArrayElementAtIndex(i);
                // For array elements, use deep copy to copy entire objects
                CopySerializedPropertyDeep(sourceElement, targetElement);
            }
        }

        private void CopySerializedPropertyDeep(SerializedProperty source, SerializedProperty target)
        {
            if (source.propertyType != target.propertyType) return;

            // Handle arrays
            if (source.isArray && target.isArray)
            {
                CopySerializedArray(source, target);
                return;
            }

            // For Generic types, recursively copy all child properties
            if (source.propertyType == SerializedPropertyType.Generic)
            {
                var sourceChild = source.Copy();
                var targetChild = target.Copy();
                
                var sourceEnd = source.GetEndProperty();
                var targetEnd = target.GetEndProperty();
                
                if (sourceChild.NextVisible(true) && targetChild.NextVisible(true))
                {
                    do
                    {
                        if (SerializedProperty.EqualContents(sourceChild, sourceEnd)) break;
                        if (SerializedProperty.EqualContents(targetChild, targetEnd)) break;
                        
                        CopySerializedPropertyDeep(sourceChild, targetChild);
                    } while (sourceChild.NextVisible(false) && targetChild.NextVisible(false));
                }
                return;
            }

            // For primitive types, use the regular copy
            CopySerializedPropertyValue(source, target);
        }
    }
}
