using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using EasyChart;

namespace EasyChart.Editor
{
    public partial class EasyChartLibraryWindow
    {
        private VisualElement _profilePropertyTracker;
        private VisualElement _legendSettingsBox;

        private void UpdateLegendSettingsVisibility()
        {
            if (_legendSettingsBox == null) return;

            bool hasPie = false;
            bool hasNonPie = false;
            if (_selectedProfile != null && _selectedProfile.series != null)
            {
                for (int i = 0; i < _selectedProfile.series.Count; i++)
                {
                    var s = _selectedProfile.series[i];
                    if (s == null) continue;
                    if (s.type == SerieType.Pie || s.type == SerieType.RingChart || s.type == SerieType.Pie3D)
                    {
                        hasPie = true;
                    }
                    else
                    {
                        hasNonPie = true;
                    }
                }
            }

            bool isPurePie = hasPie && !hasNonPie;
            _legendSettingsBox.style.display = isPurePie ? DisplayStyle.None : DisplayStyle.Flex;
        }

        private void OnAnySerializedPropertyChanged(SerializedPropertyChangeEvent evt)
        {
            if (_serializedProfile == null) return;
            if (_isUpdatingPreview) return;
            if (this == null || rootVisualElement == null) return;
            if (evt == null) return;
            if (evt.target == null) return;
            UpdateLegendSettingsVisibility();
            ScheduleUpdatePreview();
        }

        private void OnTrackedProfilePropertyChanged(SerializedProperty _)
        {
            if (_serializedProfile == null) return;
            if (_isUpdatingPreview) return;
            if (this == null || rootVisualElement == null) return;
            ScheduleUpdatePreview();
        }

        private void OnTreeSelectionChanged(IEnumerable<object> selectedItems)
        {
            var previousProfile = _selectedProfile;

            var path = selectedItems.FirstOrDefault() as string;
            if (string.IsNullOrEmpty(path) || AssetDatabase.IsValidFolder(path))
            {
                _selectedFolderPath = string.IsNullOrEmpty(path) ? null : path;
                _selectedProfile = null;
                _inspectorContainer.Clear();
                _seriesContainer.Clear();

                _jsonExampleDirtyByUser = false;
                UpdateInjectionJsonExample(forceOverwrite: true);
                return;
            }

            var profile = AssetDatabase.LoadAssetAtPath<ChartProfile>(path);
            if (profile == null) return;

            _selectedFolderPath = System.IO.Path.GetDirectoryName(path)?.Replace("\\", "/");
            _selectedProfile = profile;
            _serializedProfile = new SerializedObject(_selectedProfile);
            _serializedProfile.Update();

            if (!ReferenceEquals(previousProfile, _selectedProfile))
            {
                _jsonExampleDirtyByUser = false;
                UpdateInjectionJsonExample(forceOverwrite: true);
            }

            if (_inspectorContainer != null)
            {
                _inspectorContainer.UnregisterCallback<SerializedPropertyChangeEvent>(OnAnySerializedPropertyChanged, TrickleDown.TrickleDown);
                _inspectorContainer.Unbind();
                _inspectorContainer.Clear();
            }
            if (_seriesContainer != null)
            {
                _seriesContainer.UnregisterCallback<SerializedPropertyChangeEvent>(OnAnySerializedPropertyChanged, TrickleDown.TrickleDown);
                _seriesContainer.Unbind();
                _seriesContainer.Clear();
            }

            if (_profilePropertyTracker != null)
            {
                _profilePropertyTracker.RemoveFromHierarchy();
                _profilePropertyTracker = null;
            }

            if (_selectedProfile != null)
            {
                if (_selectedProfile.EnsureRuntimeData())
                {
                    EditorUtility.SetDirty(_selectedProfile);
                    _serializedProfile.Update();
                }
            }

            Foldout CreateFoldout(string title, string prefsKey, bool defaultValue)
            {
                var foldout = new Foldout { text = title };
                foldout.value = EditorPrefs.GetBool(prefsKey, defaultValue);
                foldout.RegisterValueChangedCallback(evt => EditorPrefs.SetBool(prefsKey, evt.newValue));
                return foldout;
            }

            const string foldoutKeyPrefix = "EasyChart.EasyChartLibraryWindow.GeneralProperties.";
            var chartSettingsFoldout = CreateFoldout("Chart Settings", foldoutKeyPrefix + "ChartSettings", true);
            var coordinateSystemFoldout = CreateFoldout("Coordinate System", foldoutKeyPrefix + "CoordinateSystem", true);
            var axesFoldout = CreateFoldout("Axis Settings", foldoutKeyPrefix + "Axes", true);
            var gridSettingsFoldout = CreateFoldout("Grid Settings", foldoutKeyPrefix + "GridSettings", true);
            var hoverSettingsFoldout = CreateFoldout("Hover Settings", foldoutKeyPrefix + "HoverSettings", true);
            var legendSettingsFoldout = CreateFoldout("Legend Settings", foldoutKeyPrefix + "LegendSettings", true);

            VisualElement WrapFoldout(Foldout foldout)
            {
                var box = CreateGroupBox();
                box.Add(foldout);
                return box;
            }

            var chartSettingsBox = WrapFoldout(chartSettingsFoldout);
            var coordinateSystemBox = WrapFoldout(coordinateSystemFoldout);
            var axesBox = WrapFoldout(axesFoldout);
            var gridSettingsBox = WrapFoldout(gridSettingsFoldout);
            var hoverSettingsBox = WrapFoldout(hoverSettingsFoldout);
            var legendSettingsBox = WrapFoldout(legendSettingsFoldout);
            _legendSettingsBox = legendSettingsBox;

            _inspectorContainer.Add(chartSettingsBox);
            _inspectorContainer.Add(coordinateSystemBox);
            _inspectorContainer.Add(axesBox);
            _inspectorContainer.Add(gridSettingsBox);
            _inspectorContainer.Add(hoverSettingsBox);
            _inspectorContainer.Add(legendSettingsBox);

            _profilePropertyTracker = new VisualElement();
            _profilePropertyTracker.style.display = DisplayStyle.None;
            _inspectorContainer.Add(_profilePropertyTracker);

            _inspectorContainer.RegisterCallback<SerializedPropertyChangeEvent>(OnAnySerializedPropertyChanged, TrickleDown.TrickleDown);
            _seriesContainer.RegisterCallback<SerializedPropertyChangeEvent>(OnAnySerializedPropertyChanged, TrickleDown.TrickleDown);

            UpdateLegendSettingsVisibility();

            var chartWidthProp = _serializedProfile.FindProperty("chartWidth");
            if (chartWidthProp != null) _profilePropertyTracker.TrackPropertyValue(chartWidthProp, OnTrackedProfilePropertyChanged);
            var chartHeightProp = _serializedProfile.FindProperty("chartHeight");
            if (chartHeightProp != null) _profilePropertyTracker.TrackPropertyValue(chartHeightProp, OnTrackedProfilePropertyChanged);
            var coordinateSystemTrackProp = _serializedProfile.FindProperty("coordinateSystem");
            if (coordinateSystemTrackProp != null) _profilePropertyTracker.TrackPropertyValue(coordinateSystemTrackProp, OnTrackedProfilePropertyChanged);

            var chartBackgroundProp = _serializedProfile.FindProperty("background");
            if (chartBackgroundProp != null)
            {
                var bgFoldout = CreateFoldout("Background", foldoutKeyPrefix + "Background", true);
                var bgDepth = chartBackgroundProp.depth;
                var bgChild = chartBackgroundProp.Copy();
                if (bgChild.NextVisible(true))
                {
                    while (bgChild.depth > bgDepth)
                    {
                        AddBoundPropertyField(bgFoldout, bgChild.Copy());

                        if (!bgChild.NextVisible(false)) break;
                    }
                }

                var bgBox = CreateGroupBox();
                bgBox.Add(bgFoldout);
                chartSettingsFoldout.Add(bgBox);
            }

            var chartNameProp = _serializedProfile.FindProperty("chartName");
            if (chartNameProp != null)
            {
                var chartNameField = new TextField(chartNameProp.displayName)
                {
                    bindingPath = chartNameProp.propertyPath
                };
                chartSettingsFoldout.Add(chartNameField);
                chartNameField.Bind(_serializedProfile);

                void CommitChartNameAndRenameAsset()
                {
                    if (_selectedProfile == null) return;

                    _serializedProfile.Update();
                    var desiredName = SanitizeFileName(chartNameProp.stringValue);
                    if (string.IsNullOrWhiteSpace(desiredName))
                    {
                        var currentPath = AssetDatabase.GetAssetPath(_selectedProfile);
                        var currentFileName = System.IO.Path.GetFileNameWithoutExtension(currentPath);
                        chartNameProp.stringValue = currentFileName;
                        _serializedProfile.ApplyModifiedProperties();
                        _serializedProfile.Update();
                        chartNameField.SetValueWithoutNotify(currentFileName);
                        return;
                    }

                    var oldPath = AssetDatabase.GetAssetPath(_selectedProfile);
                    if (string.IsNullOrEmpty(oldPath) || !oldPath.EndsWith(".asset")) return;

                    var currentName = System.IO.Path.GetFileNameWithoutExtension(oldPath);
                    if (string.Equals(currentName, desiredName, System.StringComparison.Ordinal))
                    {
                        bool anyDirty = false;
                        if (!string.Equals(_selectedProfile.name, desiredName, System.StringComparison.Ordinal))
                        {
                            _selectedProfile.name = desiredName;
                            anyDirty = true;
                        }
                        if (!string.Equals(_selectedProfile.chartName, desiredName, System.StringComparison.Ordinal))
                        {
                            _selectedProfile.chartName = desiredName;
                            anyDirty = true;
                        }

                        if (anyDirty)
                        {
                            EditorUtility.SetDirty(_selectedProfile);
                            AssetDatabase.SaveAssets();
                            _serializedProfile.Update();
                            chartNameField.SetValueWithoutNotify(desiredName);
                        }
                        return;
                    }

                    string err = AssetDatabase.RenameAsset(oldPath, desiredName);
                    if (!string.IsNullOrEmpty(err))
                    {
                        EditorUtility.DisplayDialog("Error", err, "OK");

                        chartNameProp.stringValue = currentName;
                        _serializedProfile.ApplyModifiedProperties();
                        _serializedProfile.Update();
                        chartNameField.SetValueWithoutNotify(currentName);
                        return;
                    }

                    var parent = System.IO.Path.GetDirectoryName(oldPath)?.Replace('\\', '/');
                    var ext = System.IO.Path.GetExtension(oldPath);
                    var newPath = string.IsNullOrEmpty(parent) ? null : $"{parent}/{desiredName}{ext}";

                    if (!string.Equals(_selectedProfile.name, desiredName, System.StringComparison.Ordinal))
                    {
                        _selectedProfile.name = desiredName;
                        EditorUtility.SetDirty(_selectedProfile);
                    }
                    if (!string.Equals(_selectedProfile.chartName, desiredName, System.StringComparison.Ordinal))
                    {
                        _selectedProfile.chartName = desiredName;
                        EditorUtility.SetDirty(_selectedProfile);
                    }
                    AssetDatabase.SaveAssets();

                    _serializedProfile.Update();
                    chartNameField.SetValueWithoutNotify(desiredName);

                    if (_folderTree != null)
                    {
                        _folderTree.selectionChanged -= OnTreeSelectionChanged;
                        RefreshTree();

                        if (!string.IsNullOrEmpty(newPath))
                        {
                            bool TryFindId(IEnumerable<TreeViewItemData<string>> items, string targetPath, out int foundId)
                            {
                                if (items != null)
                                {
                                    foreach (var it in items)
                                    {
                                        if (string.Equals(it.data, targetPath, System.StringComparison.OrdinalIgnoreCase))
                                        {
                                            foundId = it.id;
                                            return true;
                                        }
                                        if (it.children != null && TryFindId(it.children, targetPath, out foundId))
                                        {
                                            return true;
                                        }
                                    }
                                }
                                foundId = -1;
                                return false;
                            }

                            if (TryFindId(_treeRoots, newPath, out var id) && id >= 0)
                            {
                                _folderTree.SetSelection(new[] { id });
                            }
                        }

                        _folderTree.selectionChanged += OnTreeSelectionChanged;
                    }
                }

                chartNameField.RegisterCallback<FocusOutEvent>(_ => CommitChartNameAndRenameAsset());
                chartNameField.RegisterCallback<KeyDownEvent>(evt =>
                {
                    if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter)
                    {
                        CommitChartNameAndRenameAsset();
                        evt.StopPropagation();
                    }
                });
            }

            var coordinateSystemProp = _serializedProfile.FindProperty("coordinateSystem");
            PropertyField coordinateSystemField = null;
            if (coordinateSystemProp != null)
            {
                coordinateSystemField = AddBoundPropertyField(coordinateSystemFoldout, coordinateSystemProp);
            }

            var cartesianProp = _serializedProfile.FindProperty("cartesian");
            var axesProp = _serializedProfile.FindProperty("axes");

            var cartesianGridProp = _serializedProfile.FindProperty("cartesianGrid");
            var hoverProp = _serializedProfile.FindProperty("hover");
            var polarAxesProp = _serializedProfile.FindProperty("polarAxes");

            var xAxisIdProp = _serializedProfile.FindProperty("xAxisId");
            var yAxisIdProp = _serializedProfile.FindProperty("yAxisId");

            var cartesianAxisSelectionContainer = CreateGroupBox();
            coordinateSystemFoldout.Add(cartesianAxisSelectionContainer);
            var cartesianAxesContainer = new VisualElement();
            var polarAxesContainer = new VisualElement();

            var categoryAxisFoldout = new Foldout { text = "X Axis Setting", value = true };
            var valueAxisFoldout = new Foldout { text = "Y Axis Setting", value = true };
            var categoryAxisFields = new VisualElement();
            var valueAxisFields = new VisualElement();
            categoryAxisFields.style.marginLeft = 8;
            valueAxisFields.style.marginLeft = 8;
            categoryAxisFoldout.Add(categoryAxisFields);
            valueAxisFoldout.Add(valueAxisFields);

            var categoryAxisBox = CreateGroupBox();
            categoryAxisBox.Add(categoryAxisFoldout);
            cartesianAxesContainer.Add(categoryAxisBox);

            var valueAxisBox = CreateGroupBox();
            valueAxisBox.Add(valueAxisFoldout);
            cartesianAxesContainer.Add(valueAxisBox);

            var angleAxisFoldout = new Foldout { text = "Angle Axis Setting", value = true };
            var radiusAxisFoldout = new Foldout { text = "Radius Axis Setting", value = true };
            var angleAxisFields = new VisualElement();
            var radiusAxisFields = new VisualElement();
            angleAxisFields.style.marginLeft = 8;
            radiusAxisFields.style.marginLeft = 8;
            angleAxisFoldout.Add(angleAxisFields);
            radiusAxisFoldout.Add(radiusAxisFields);

            var angleAxisBox = CreateGroupBox();
            angleAxisBox.Add(angleAxisFoldout);
            polarAxesContainer.Add(angleAxisBox);

            var radiusAxisBox = CreateGroupBox();
            radiusAxisBox.Add(radiusAxisFoldout);
            polarAxesContainer.Add(radiusAxisBox);
            axesFoldout.Add(cartesianAxesContainer);
            axesFoldout.Add(polarAxesContainer);

            AxisId GetXAxisId()
            {
                if (xAxisIdProp == null) return AxisId.XBottom;
                return (AxisId)xAxisIdProp.enumValueIndex;
            }

            AxisId GetYAxisId()
            {
                if (yAxisIdProp == null) return AxisId.YLeft;
                return (AxisId)yAxisIdProp.enumValueIndex;
            }

            var xAxisChoices = new List<AxisId> { AxisId.XBottom, AxisId.XTop };
            var yAxisChoices = new List<AxisId> { AxisId.YLeft, AxisId.YRight };

            var xAxisPopup = new PopupField<AxisId>("X Axis", xAxisChoices, GetXAxisId());
            var yAxisPopup = new PopupField<AxisId>("Y Axis", yAxisChoices, GetYAxisId());

            cartesianAxisSelectionContainer.Add(xAxisPopup);
            cartesianAxisSelectionContainer.Add(yAxisPopup);

            void ApplyAxisSelection(AxisId newX, AxisId newY)
            {
                if (_serializedProfile == null) return;
                if (cartesianAxisSelectionContainer.panel == null) return;
                if (xAxisIdProp != null) xAxisIdProp.enumValueIndex = (int)newX;
                if (yAxisIdProp != null) yAxisIdProp.enumValueIndex = (int)newY;

                if (_serializedProfile != null && _serializedProfile.hasModifiedProperties)
                    _serializedProfile.ApplyModifiedProperties();

                if (_selectedProfile != null)
                {
                    if (_selectedProfile.EnsureAxesIntegrity())
                    {
                        EditorUtility.SetDirty(_selectedProfile);
                    }
                }

                _serializedProfile.Update();
                RefreshActiveAxesUI();
                ScheduleUpdatePreview();
            }

            xAxisPopup.RegisterValueChangedCallback(evt =>
            {
                if (_serializedProfile == null) return;
                if (cartesianAxisSelectionContainer.panel == null) return;
                ApplyAxisSelection(evt.newValue, yAxisPopup.value);
            });

            yAxisPopup.RegisterValueChangedCallback(evt =>
            {
                if (_serializedProfile == null) return;
                if (cartesianAxisSelectionContainer.panel == null) return;
                ApplyAxisSelection(xAxisPopup.value, evt.newValue);
            });

            SerializedProperty FindAxisElement(SerializedProperty listProp, AxisId id)
            {
                if (listProp == null || !listProp.isArray) return null;
                for (int i = 0; i < listProp.arraySize; i++)
                {
                    var el = listProp.GetArrayElementAtIndex(i);
                    var idProp = el.FindPropertyRelative("id");
                    if (idProp != null && idProp.enumValueIndex == (int)id) return el;
                }
                return null;
            }

            void RefreshActiveAxesUI()
            {
                categoryAxisFields.Unbind();
                valueAxisFields.Unbind();
                categoryAxisFields.Clear();
                valueAxisFields.Clear();
                if (axesProp == null) return;

                SerializedProperty catEl = FindAxisElement(axesProp, GetXAxisId());
                SerializedProperty valEl = FindAxisElement(axesProp, GetYAxisId());

                if ((catEl == null || valEl == null) && _selectedProfile != null)
                {
                    if (_serializedProfile.hasModifiedProperties)
                        _serializedProfile.ApplyModifiedProperties();

                    if (_selectedProfile.EnsureAxesIntegrity())
                    {
                        EditorUtility.SetDirty(_selectedProfile);
                    }

                    _serializedProfile.Update();
                    catEl = FindAxisElement(axesProp, GetXAxisId());
                    valEl = FindAxisElement(axesProp, GetYAxisId());
                }

                SerializedProperty EnsureAxisElement(SerializedProperty listProp, AxisId id, AxisType axisType)
                {
                    var el = FindAxisElement(listProp, id);
                    if (el != null) return el;
                    if (listProp == null || !listProp.isArray) return null;

                    int idx = listProp.arraySize;
                    listProp.InsertArrayElementAtIndex(idx);
                    var created = listProp.GetArrayElementAtIndex(idx);
                    if (created == null) return null;

                    var idProp = created.FindPropertyRelative("id");
                    if (idProp != null) idProp.enumValueIndex = (int)id;
                    var dimProp = created.FindPropertyRelative("axisType");
                    if (dimProp != null) dimProp.enumValueIndex = (int)axisType;
                    return created;
                }

                if (catEl == null)
                {
                    catEl = EnsureAxisElement(axesProp, GetXAxisId(), AxisType.Category);
                    if (_serializedProfile != null && _serializedProfile.hasModifiedProperties) _serializedProfile.ApplyModifiedProperties();
                }

                if (valEl == null)
                {
                    valEl = EnsureAxisElement(axesProp, GetYAxisId(), AxisType.Value);
                    if (_serializedProfile != null && _serializedProfile.hasModifiedProperties) _serializedProfile.ApplyModifiedProperties();
                }

                void BuildAxisConfigUI(VisualElement parent, SerializedProperty axisEl)
                {
                    if (axisEl == null) return;

                    var labelsProp = axisEl.FindPropertyRelative("labels");

                    SerializedProperty Prop(string name) => axisEl.FindPropertyRelative(name);

                    void AddProp(string name)
                    {
                        var p = Prop(name);
                        if (p == null) return;
                        AddBoundPropertyField(parent, p);
                    }

                    AddProp("axisType");
                    AddProp("visible");
                    AddProp("color");
                    AddProp("width");

                    var labelBox = CreateGroupBox();
                    if (labelsProp != null)
                    {
                        var pf = new PropertyField(labelsProp, "LabelTexts");
                        if (_serializedProfile != null) pf.Bind(_serializedProfile);
                        pf.RegisterCallback<SerializedPropertyChangeEvent>(evt =>
                        {
                            if (_serializedProfile == null) return;
                            if (pf.panel == null) return;
                            ScheduleUpdatePreview();
                        });
                        labelBox.Add(pf);
                    }

                    var labelStyleProp = Prop("labelStyle");
                    if (labelStyleProp != null) AddBoundPropertyField(labelBox, labelStyleProp);
                    var labelPlacementProp = Prop("labelPlacement");
                    if (labelPlacementProp != null) AddBoundPropertyField(labelBox, labelPlacementProp);
                    AddLabelFormatDropdown(labelBox, Prop("labelFormat"));

                    parent.Add(labelBox);

                    {
                        var rangeContainer = new VisualElement();
                        parent.Add(rangeContainer);

                        var autoRangeMinProp = Prop("autoRangeMin");
                        var autoRangeMaxProp = Prop("autoRangeMax");
                        var autoRangeRoundingProp = Prop("autoRangeRounding");
                        var autoRangeUnitProp = Prop("autoRangeUnit");
                        var minValueProp = Prop("minValue");
                        var maxValueProp = Prop("maxValue");

                        if (autoRangeMinProp != null)
                        {
                            var minContainer = new VisualElement();
                            minContainer.style.marginLeft = 8;
                            AddToggleContainer(rangeContainer, autoRangeMinProp, minContainer, false);
                            if (minValueProp != null) AddBoundPropertyField(minContainer, minValueProp);
                        }
                        else
                        {
                            if (minValueProp != null) AddBoundPropertyField(rangeContainer, minValueProp);
                        }

                        if (autoRangeMaxProp != null)
                        {
                            var maxContainer = new VisualElement();
                            maxContainer.style.marginLeft = 8;
                            AddToggleContainer(rangeContainer, autoRangeMaxProp, maxContainer, false);
                            if (maxValueProp != null) AddBoundPropertyField(maxContainer, maxValueProp);
                        }
                        else
                        {
                            if (maxValueProp != null) AddBoundPropertyField(rangeContainer, maxValueProp);
                        }

                        if (autoRangeRoundingProp != null)
                        {
                            AddBoundPropertyField(rangeContainer, autoRangeRoundingProp);
                        }
                        if (autoRangeRoundingProp != null && autoRangeUnitProp != null)
                        {
                            var unitField = AddBoundPropertyField(rangeContainer, autoRangeUnitProp);
                            if (unitField != null)
                            {
                                unitField.style.marginLeft = 8;

                                void UpdateAutoRangeUnitVisibility()
                                {
                                    if (_serializedProfile == null) return;
                                    if (rangeContainer.panel == null) return;
                                    if (_serializedProfile.hasModifiedProperties)
                                        _serializedProfile.ApplyModifiedProperties();
                                    _serializedProfile.Update();

                                    bool show = autoRangeRoundingProp.enumValueIndex == 4;
                                    unitField.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
                                }

                                UpdateAutoRangeUnitVisibility();
                                unitField.RegisterCallback<SerializedPropertyChangeEvent>(evt =>
                                {
                                    UpdateAutoRangeUnitVisibility();
                                    ScheduleUpdatePreview();
                                });
                            }
                        }
                    }

                    var axisTypeProp = Prop("axisType");

                    var ticksRoot = new VisualElement();
                    parent.Add(ticksRoot);

                    var categoryRoot = new VisualElement();
                    parent.Add(categoryRoot);

                    var autoTicksProp = Prop("autoTicks");
                    if (autoTicksProp != null)
                    {
                        var ticksContainer = new VisualElement();
                        AddToggleContainer(ticksRoot, autoTicksProp, ticksContainer, false);

                        var splitProp = Prop("splitCount");
                        if (splitProp != null)
                        {
                            // Label changes based on axis type
                            var splitField = new PropertyField(splitProp);
                            if (_serializedProfile != null) splitField.Bind(_serializedProfile);
                            splitField.RegisterCallback<SerializedPropertyChangeEvent>(evt =>
                            {
                                if (_serializedProfile == null) return;
                                if (splitField.panel == null) return;
                                ScheduleUpdatePreview();
                            });
                            ticksContainer.Add(splitField);
                            
                            // Update label based on axis type
                            void UpdateSplitLabel()
                            {
                                if (axisTypeProp == null) return;
                                bool isCategory = axisTypeProp.enumValueIndex == (int)AxisType.Category;
                                splitField.label = isCategory ? "VisibleCount" : "Split Count";
                            }
                            UpdateSplitLabel();
                            splitField.RegisterCallback<AttachToPanelEvent>(_ => UpdateSplitLabel());
                            parent.RegisterCallback<SerializedPropertyChangeEvent>(evt => UpdateSplitLabel());
                        }
                    }

                    var categoryAutoScrollProp = Prop("categoryAutoScroll");
                    var categorySmoothScrollProp = Prop("categorySmoothScroll");
                    var categoryScrollIntervalProp = Prop("categoryScrollInterval");
                    var categoryScrollStepProp = Prop("categoryScrollStep");

                    if (categoryAutoScrollProp != null)
                    {
                        var scrollDetails = new VisualElement();
                        scrollDetails.style.marginLeft = 8;
                        AddToggleContainer(categoryRoot, categoryAutoScrollProp, scrollDetails, true);

                        if (categorySmoothScrollProp != null) AddBoundPropertyField(scrollDetails, categorySmoothScrollProp);
                        if (categoryScrollIntervalProp != null) AddBoundPropertyField(scrollDetails, categoryScrollIntervalProp);
                        if (categoryScrollStepProp != null) AddBoundPropertyField(scrollDetails, categoryScrollStepProp);
                    }

                    var showUnitProp = Prop("showUnit");
                    var unitTextProp = Prop("unitText");
                    var unitLabelStyleProp = Prop("unitLabelStyle");

                    var unitRoot = new VisualElement();
                    parent.Add(unitRoot);

                    var unitDetails = new VisualElement();
                    if (showUnitProp != null)
                    {
                        AddToggleContainer(unitRoot, showUnitProp, unitDetails, true);
                    }
                    else
                    {
                        unitRoot.Add(unitDetails);
                    }

                    if (unitTextProp != null) AddBoundPropertyField(unitDetails, unitTextProp);
                    if (unitLabelStyleProp != null) AddBoundPropertyField(unitDetails, unitLabelStyleProp);

                    void UpdateUnitVisibility()
                    {
                        if (_serializedProfile == null) return;
                        if (unitRoot.panel == null) return;
                        if (_serializedProfile.hasModifiedProperties)
                            _serializedProfile.ApplyModifiedProperties();
                        _serializedProfile.Update();

                        bool isValue = axisTypeProp == null || axisTypeProp.enumValueIndex == (int)AxisType.Value;
                        unitRoot.style.display = isValue ? DisplayStyle.Flex : DisplayStyle.None;
                    }

                    void UpdateAxisTypeVisibility()
                    {
                        if (_serializedProfile == null) return;
                        if (parent.panel == null) return;
                        if (_serializedProfile.hasModifiedProperties)
                            _serializedProfile.ApplyModifiedProperties();
                        _serializedProfile.Update();

                        bool isCategory = axisTypeProp != null && axisTypeProp.enumValueIndex == (int)AxisType.Category;
                        categoryRoot.style.display = isCategory ? DisplayStyle.Flex : DisplayStyle.None;
                        // ticksRoot (autoTicks) is now visible for both Category and Value axis types
                        ticksRoot.style.display = DisplayStyle.Flex;
                    }

                    UpdateUnitVisibility();
                    unitRoot.RegisterCallback<AttachToPanelEvent>(_ => UpdateUnitVisibility());
                    unitRoot.RegisterCallback<SerializedPropertyChangeEvent>(evt =>
                    {
                        UpdateUnitVisibility();
                        ScheduleUpdatePreview();
                    });

                    UpdateAxisTypeVisibility();
                    parent.RegisterCallback<AttachToPanelEvent>(_ => UpdateAxisTypeVisibility());
                    parent.RegisterCallback<SerializedPropertyChangeEvent>(evt =>
                    {
                        UpdateAxisTypeVisibility();
                    });

                    parent.RegisterCallback<SerializedPropertyChangeEvent>(evt =>
                    {
                        UpdateUnitVisibility();
                    });
                }

                if (catEl != null)
                {
                    BuildAxisConfigUI(categoryAxisFields, catEl);
                }

                if (valEl != null)
                {
                    BuildAxisConfigUI(valueAxisFields, valEl);
                }
            }

            void RefreshPolarAxesUI()
            {
                angleAxisFields.Unbind();
                radiusAxisFields.Unbind();
                angleAxisFields.Clear();
                radiusAxisFields.Clear();
                if (polarAxesProp == null) return;

                SerializedProperty angleAxisProp = polarAxesProp.FindPropertyRelative("angleAxis");
                SerializedProperty radiusAxisProp = polarAxesProp.FindPropertyRelative("radiusAxis");

                void BuildPolarAxisUI(VisualElement parent, SerializedProperty axisStyle)
                {
                    if (axisStyle == null) return;

                    SerializedProperty Prop(string name) => axisStyle.FindPropertyRelative(name);

                    void AddProp(string name)
                    {
                        var p = Prop(name);
                        if (p == null) return;
                        AddBoundPropertyField(parent, p);
                    }

                    AddProp("labels");

                    AddProp("visible");
                    AddProp("color");
                    AddProp("width");

                    AddProp("labelStyle");

                    AddLabelFormatDropdown(parent, Prop("labelFormat"));

                    {
                        var rangeContainer = new VisualElement();
                        parent.Add(rangeContainer);

                        var autoRangeMinProp = Prop("autoRangeMin");
                        var autoRangeMaxProp = Prop("autoRangeMax");
                        var autoRangeRoundingProp = Prop("autoRangeRounding");
                        var autoRangeUnitProp = Prop("autoRangeUnit");
                        var minValueProp = Prop("minValue");
                        var maxValueProp = Prop("maxValue");

                        if (autoRangeMinProp != null)
                        {
                            var minContainer = new VisualElement();
                            minContainer.style.marginLeft = 8;
                            AddToggleContainer(rangeContainer, autoRangeMinProp, minContainer, false);
                            if (minValueProp != null) AddBoundPropertyField(minContainer, minValueProp);
                        }
                        else
                        {
                            if (minValueProp != null) AddBoundPropertyField(rangeContainer, minValueProp);
                        }

                        if (autoRangeMaxProp != null)
                        {
                            var maxContainer = new VisualElement();
                            maxContainer.style.marginLeft = 8;
                            AddToggleContainer(rangeContainer, autoRangeMaxProp, maxContainer, false);
                            if (maxValueProp != null) AddBoundPropertyField(maxContainer, maxValueProp);
                        }
                        else
                        {
                            if (maxValueProp != null) AddBoundPropertyField(rangeContainer, maxValueProp);
                        }

                        if (autoRangeRoundingProp != null)
                        {
                            AddBoundPropertyField(rangeContainer, autoRangeRoundingProp);
                        }
                        if (autoRangeRoundingProp != null && autoRangeUnitProp != null)
                        {
                            var unitField = AddBoundPropertyField(rangeContainer, autoRangeUnitProp);
                            if (unitField != null)
                            {
                                unitField.style.marginLeft = 8;

                                void UpdateAutoRangeUnitVisibility()
                                {
                                    if (_serializedProfile == null) return;
                                    if (rangeContainer.panel == null) return;
                                    if (_serializedProfile.hasModifiedProperties)
                                        _serializedProfile.ApplyModifiedProperties();
                                    _serializedProfile.Update();

                                    bool show = autoRangeRoundingProp.enumValueIndex == 4;
                                    unitField.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
                                }

                                UpdateAutoRangeUnitVisibility();
                                unitField.RegisterCallback<SerializedPropertyChangeEvent>(evt =>
                                {
                                    UpdateAutoRangeUnitVisibility();
                                    ScheduleUpdatePreview();
                                });
                            }
                        }
                    }

                    var autoTicksProp = Prop("autoTicks");
                    if (autoTicksProp != null)
                    {
                        var ticksContainer = new VisualElement();
                        var autoTicksField = AddToggleContainer(parent, autoTicksProp, ticksContainer, false);

                        var splitProp = Prop("splitCount");
                        if (splitProp != null)
                        {
                            AddBoundPropertyField(ticksContainer, splitProp);
                        }
                    }
                }

                BuildPolarAxisUI(angleAxisFields, angleAxisProp);
                BuildPolarAxisUI(radiusAxisFields, radiusAxisProp);
            }

            var cartesianGridContainer = new VisualElement();
            gridSettingsFoldout.Add(cartesianGridContainer);

            if (cartesianGridProp != null)
            {
                SerializedProperty Rel(string name) => cartesianGridProp.FindPropertyRelative(name);

                var xGridColor = Rel("xGridColor");
                var xGridWidth = Rel("xGridLineWidth");
                var yGridColor = Rel("yGridColor");
                var yGridWidth = Rel("yGridLineWidth");

                if (xGridColor != null) AddBoundPropertyField(cartesianGridContainer, xGridColor);
                if (xGridWidth != null) AddBoundPropertyField(cartesianGridContainer, xGridWidth);
                if (yGridColor != null) AddBoundPropertyField(cartesianGridContainer, yGridColor);
                if (yGridWidth != null) AddBoundPropertyField(cartesianGridContainer, yGridWidth);

                var xDashBox = new Box();
                xDashBox.style.marginTop = 4;
                cartesianGridContainer.Add(xDashBox);
                xDashBox.Add(new Label("X Grid Dashed"));

                var xDashed = Rel("xGridDashed");
                var xDashLen = Rel("xGridDashLength");
                var xDashGap = Rel("xGridDashGap");
                var xDashOff = Rel("xGridDashOffset");

                if (xDashed != null) AddBoundPropertyField(xDashBox, xDashed);
                if (xDashLen != null) AddBoundPropertyField(xDashBox, xDashLen);
                if (xDashGap != null) AddBoundPropertyField(xDashBox, xDashGap);
                if (xDashOff != null) AddBoundPropertyField(xDashBox, xDashOff);

                var yDashBox = new Box();
                yDashBox.style.marginTop = 4;
                cartesianGridContainer.Add(yDashBox);
                yDashBox.Add(new Label("Y Grid Dashed"));

                var yDashed = Rel("yGridDashed");
                var yDashLen = Rel("yGridDashLength");
                var yDashGap = Rel("yGridDashGap");
                var yDashOff = Rel("yGridDashOffset");

                if (yDashed != null) AddBoundPropertyField(yDashBox, yDashed);
                if (yDashLen != null) AddBoundPropertyField(yDashBox, yDashLen);
                if (yDashGap != null) AddBoundPropertyField(yDashBox, yDashGap);
                if (yDashOff != null) AddBoundPropertyField(yDashBox, yDashOff);
            }

            var hoverContainer = new VisualElement();
            hoverSettingsFoldout.Add(hoverContainer);
            if (hoverProp != null)
            {
                SerializedProperty RelHover(string name) => hoverProp.FindPropertyRelative(name);

                var hoverBox = new Box();
                hoverContainer.Add(hoverBox);
                hoverBox.Add(new Label("Cursor Line"));

                var cColor = RelHover("cursorLineColor");
                var cWidth = RelHover("cursorLineWidth");
                var cDashed = RelHover("cursorLineDashed");
                var cDashLen = RelHover("cursorLineDashLength");
                var cDashGap = RelHover("cursorLineDashGap");
                var cDashOff = RelHover("cursorLineDashOffset");

                if (cColor != null) AddBoundPropertyField(hoverBox, cColor);
                if (cWidth != null) AddBoundPropertyField(hoverBox, cWidth);
                if (cDashed != null) AddBoundPropertyField(hoverBox, cDashed);
                if (cDashLen != null) AddBoundPropertyField(hoverBox, cDashLen);
                if (cDashGap != null) AddBoundPropertyField(hoverBox, cDashGap);
                if (cDashOff != null) AddBoundPropertyField(hoverBox, cDashOff);
            }

            void UpdateCoordinateSpecificVisibility()
            {
                if (coordinateSystemProp == null) return;
                // 0 = Cartesian2D, 1 = Polar2D, 2 = None
                bool isCartesian = coordinateSystemProp.enumValueIndex == 0;
                bool isPolar = coordinateSystemProp.enumValueIndex == 1;
                bool isNone = coordinateSystemProp.enumValueIndex == 2;

                // Cartesian axis selection only for Cartesian2D
                cartesianAxisSelectionContainer.style.display = isCartesian ? DisplayStyle.Flex : DisplayStyle.None;

                // Cartesian axes only for Cartesian2D
                cartesianAxesContainer.style.display = isCartesian ? DisplayStyle.Flex : DisplayStyle.None;
                // Polar axes only for Polar2D
                polarAxesContainer.style.display = isPolar ? DisplayStyle.Flex : DisplayStyle.None;

                // Axis Settings box: hide for None coordinate system (no axes needed)
                axesBox.style.display = isNone ? DisplayStyle.None : DisplayStyle.Flex;

                // Grid settings only for Cartesian2D
                cartesianGridContainer.style.display = isCartesian ? DisplayStyle.Flex : DisplayStyle.None;
                gridSettingsBox.style.display = isCartesian ? DisplayStyle.Flex : DisplayStyle.None;

                // Hover settings (cursor line) only for Cartesian2D
                hoverSettingsBox.style.display = isCartesian ? DisplayStyle.Flex : DisplayStyle.None;
            }

            UpdateCoordinateSpecificVisibility();
            if (coordinateSystemField != null)
            {
                coordinateSystemField.RegisterCallback<SerializedPropertyChangeEvent>(evt =>
                {
                    if (_serializedProfile == null) return;
                    if (coordinateSystemField.panel == null) return;
                    UpdateCoordinateSpecificVisibility();
                    ScheduleRefreshSeriesList();
                });
            }
            RefreshActiveAxesUI();
            RefreshPolarAxesUI();

            var iterator = _serializedProfile.GetIterator();
            bool enterChildren = true;
            while (iterator.NextVisible(enterChildren))
            {
                if (iterator.name == "m_Script" || iterator.name == "series")
                {
                    enterChildren = false;
                    continue;
                }

                if (iterator.name == "chartName" || iterator.name == "chartId" ||
                    iterator.name == "coordinateSystem" || iterator.name == "cartesian" || iterator.name == "axes" || iterator.name == "axisSelectionInitialized" ||
                    iterator.name == "xAxisId" || iterator.name == "yAxisId" ||
                    iterator.name == "xGridColor" || iterator.name == "xGridLineWidth" || iterator.name == "yGridColor" || iterator.name == "yGridLineWidth" ||
                    iterator.name == "cartesianGrid" || iterator.name == "hover" || iterator.name == "polarAxes" || iterator.name == "gridSettingsInitialized" || iterator.name == "hoverSettingsInitialized")
                {
                    enterChildren = false;
                    continue;
                }

                if (_legendSettingsBox != null && _legendSettingsBox.style.display == DisplayStyle.None && iterator.name == "legendSettings")
                {
                    enterChildren = false;
                    continue;
                }

                if (iterator.name == "background")
                {
                    enterChildren = false;
                    continue;
                }

                VisualElement parent;
                switch (iterator.name)
                {
                    case "animationDuration":
                    case "padding":
                        parent = chartSettingsFoldout;
                        break;
                    case "xGridColor":
                    case "xGridLineWidth":
                    case "yGridColor":
                    case "yGridLineWidth":
                        parent = gridSettingsFoldout;
                        break;
                    case "legendSettings":
                        parent = legendSettingsFoldout;
                        break;
                    default:
                        parent = chartSettingsFoldout;
                        break;
                }

                AddBoundPropertyField(parent, iterator.Copy());

                enterChildren = false;
            }

            ScheduleRefreshSeriesList();
            ScheduleUpdatePreview();
        }
    }
}
