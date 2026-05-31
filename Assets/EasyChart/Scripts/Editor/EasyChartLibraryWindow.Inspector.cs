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
                    if (s.type == SerieType.Pie || s.type == SerieType.RingChart || s.type == SerieType.Pie3D || s.type == SerieType.Funnel)
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

                // Start cooldown so bind-init events from the new UI don't trigger sync
                _seriesBindInitEventCount = 0;
                _seriesUIRebuildEndTime = EditorApplication.timeSinceStartup;
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

            (VisualElement container, Foldout foldout) CreateStyledFoldout(string title, string prefsKey, bool defaultValue)
            {
                var container = new VisualElement();
                var foldout = new Foldout { text = title };
                
                // IMPORTANT: Clear binding path to prevent SerializedObjectBinding from affecting the foldout
                foldout.bindingPath = string.Empty;
                
                // Read from EditorPrefs to persist user's expand preference
                bool initialValue = EditorPrefs.GetBool(prefsKey, defaultValue);
                
                // Initial state uses default (unfocused) color
                EditorStyleHelper.ApplyExpandedStyle(container, initialValue, false);
                
                // Set value BEFORE registering callback to avoid triggering it
                foldout.SetValueWithoutNotify(initialValue);
                
                // Capture prefsKey in local variable to ensure correct closure
                string capturedKey = prefsKey;
                
                // Track the expected value to handle system-triggered changes
                bool expectedValue = initialValue;
                bool userInitiated = false;
                
                foldout.RegisterCallback<PointerDownEvent>(evt => userInitiated = true, TrickleDown.TrickleDown);
                
                foldout.RegisterValueChangedCallback(evt =>
                {
                    evt.StopPropagation();
                    
                    // Only process if user initiated the change
                    if (!userInitiated) 
                    {
                        // Revert to expected value (not evt.previousValue which may be wrong)
                        if (evt.newValue != expectedValue)
                        {
                            foldout.SetValueWithoutNotify(expectedValue);
                            EditorStyleHelper.ApplyExpandedStyle(container, expectedValue, false);
                        }
                        return;
                    }
                    userInitiated = false;
                    
                    // Update expected value
                    expectedValue = evt.newValue;
                    
                    // Save user's preference
                    EditorPrefs.SetBool(capturedKey, evt.newValue);
                    // When user manually expands, use focused (cyan) color
                    EditorStyleHelper.ApplyExpandedStyle(container, evt.newValue, evt.newValue);
                });
                
                // Register focus callbacks for highlight effect
                EditorStyleHelper.RegisterFocusCallbacks(container, foldout);
                
                container.Add(foldout);
                return (container, foldout);
            }

            const string foldoutKeyPrefix = "EasyChart.EasyChartLibraryWindow.GeneralProperties.";
            var (chartSettingsContainer, chartSettingsFoldout) = CreateStyledFoldout("Chart Settings", foldoutKeyPrefix + "ChartSettings", true);
            var (coordinateSystemContainer, coordinateSystemFoldout) = CreateStyledFoldout("Coordinate System", foldoutKeyPrefix + "CoordinateSystem", true);
            var (axesContainer, axesFoldout) = CreateStyledFoldout("Axis Settings", foldoutKeyPrefix + "Axes", true);
            var (gridSettingsContainer, gridSettingsFoldout) = CreateStyledFoldout("Grid Settings", foldoutKeyPrefix + "GridSettings", true);
            var (hoverSettingsContainer, hoverSettingsFoldout) = CreateStyledFoldout("Hover Settings", foldoutKeyPrefix + "HoverSettings", true);
            var (legendSettingsContainer, legendSettingsFoldout) = CreateStyledFoldout("Legend Settings", foldoutKeyPrefix + "LegendSettings", true);

            VisualElement WrapFoldout(VisualElement container)
            {
                var box = CreateGroupBox();
                box.Add(container);
                return box;
            }

            var chartSettingsBox = WrapFoldout(chartSettingsContainer);
            var coordinateSystemBox = WrapFoldout(coordinateSystemContainer);
            var axesBox = WrapFoldout(axesContainer);
            var gridSettingsBox = WrapFoldout(gridSettingsContainer);
            var hoverSettingsBox = WrapFoldout(hoverSettingsContainer);
            var legendSettingsBox = WrapFoldout(legendSettingsContainer);
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
            var cartesian3DGridProp = _serializedProfile.FindProperty("cartesian3DGrid");
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
            var zAxisFoldout = new Foldout { text = "Z Axis Setting", value = true };
            var categoryAxisFields = new VisualElement();
            var valueAxisFields = new VisualElement();
            var zAxisFields = new VisualElement();
            categoryAxisFields.style.marginLeft = 8;
            valueAxisFields.style.marginLeft = 8;
            zAxisFields.style.marginLeft = 8;
            categoryAxisFoldout.Add(categoryAxisFields);
            valueAxisFoldout.Add(valueAxisFields);
            zAxisFoldout.Add(zAxisFields);

            var categoryAxisBox = CreateGroupBox();
            categoryAxisBox.Add(categoryAxisFoldout);
            cartesianAxesContainer.Add(categoryAxisBox);

            var valueAxisBox = CreateGroupBox();
            valueAxisBox.Add(valueAxisFoldout);
            cartesianAxesContainer.Add(valueAxisBox);

            // Z Axis box (only for Cartesian3D)
            var zAxisBox = CreateGroupBox();
            zAxisBox.Add(zAxisFoldout);
            cartesianAxesContainer.Add(zAxisBox);

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
            var zAxisChoices = new List<AxisId> { AxisId.ZFront, AxisId.ZBack };

            AxisId GetZAxisIdFromProfile()
            {
                var cartesian3DProp = _serializedProfile?.FindProperty("cartesian3D");
                if (cartesian3DProp == null) return AxisId.ZFront;
                var zIdProp = cartesian3DProp.FindPropertyRelative("zAxisId");
                if (zIdProp == null) return AxisId.ZFront;
                return (AxisId)zIdProp.enumValueIndex;
            }

            var xAxisPopup = new PopupField<AxisId>("X Axis", xAxisChoices, GetXAxisId());
            var yAxisPopup = new PopupField<AxisId>("Y Axis", yAxisChoices, GetYAxisId());
            var zAxisPopup = new PopupField<AxisId>("Z Axis", zAxisChoices, GetZAxisIdFromProfile());

            cartesianAxisSelectionContainer.Add(xAxisPopup);
            cartesianAxisSelectionContainer.Add(yAxisPopup);

            // Z axis container (only visible for Cartesian3D)
            var zAxisContainer = new VisualElement();
            zAxisContainer.Add(zAxisPopup);
            cartesianAxisSelectionContainer.Add(zAxisContainer);

            void ApplyAxisSelection(AxisId newX, AxisId newY, AxisId newZ)
            {
                if (_serializedProfile == null) return;
                if (cartesianAxisSelectionContainer.panel == null) return;
                if (xAxisIdProp != null) xAxisIdProp.enumValueIndex = (int)newX;
                if (yAxisIdProp != null) yAxisIdProp.enumValueIndex = (int)newY;

                // Update Z axis in cartesian3D
                var cartesian3DProp = _serializedProfile.FindProperty("cartesian3D");
                if (cartesian3DProp != null)
                {
                    var zIdProp = cartesian3DProp.FindPropertyRelative("zAxisId");
                    if (zIdProp != null) zIdProp.enumValueIndex = (int)newZ;
                    
                    // Also update X and Y in cartesian3D for consistency
                    var xIdProp = cartesian3DProp.FindPropertyRelative("xAxisId");
                    var yIdProp = cartesian3DProp.FindPropertyRelative("yAxisId");
                    if (xIdProp != null) xIdProp.enumValueIndex = (int)newX;
                    if (yIdProp != null) yIdProp.enumValueIndex = (int)newY;
                }

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
                ApplyAxisSelection(evt.newValue, yAxisPopup.value, zAxisPopup.value);
            });

            yAxisPopup.RegisterValueChangedCallback(evt =>
            {
                if (_serializedProfile == null) return;
                if (cartesianAxisSelectionContainer.panel == null) return;
                ApplyAxisSelection(xAxisPopup.value, evt.newValue, zAxisPopup.value);
            });

            zAxisPopup.RegisterValueChangedCallback(evt =>
            {
                if (_serializedProfile == null) return;
                if (cartesianAxisSelectionContainer.panel == null) return;
                ApplyAxisSelection(xAxisPopup.value, yAxisPopup.value, evt.newValue);
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

            AxisId GetZAxisId()
            {
                var cartesian3DProp = _serializedProfile?.FindProperty("cartesian3D");
                if (cartesian3DProp == null) return AxisId.ZFront;
                var zAxisIdProp = cartesian3DProp.FindPropertyRelative("zAxisId");
                if (zAxisIdProp == null) return AxisId.ZFront;
                return (AxisId)zAxisIdProp.enumValueIndex;
            }

            void RefreshActiveAxesUI()
            {
                categoryAxisFields.Unbind();
                valueAxisFields.Unbind();
                zAxisFields.Unbind();
                categoryAxisFields.Clear();
                valueAxisFields.Clear();
                zAxisFields.Clear();
                if (axesProp == null) return;

                SerializedProperty catEl = FindAxisElement(axesProp, GetXAxisId());
                SerializedProperty valEl = FindAxisElement(axesProp, GetYAxisId());
                SerializedProperty zEl = FindAxisElement(axesProp, GetZAxisId());

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
                    zEl = FindAxisElement(axesProp, GetZAxisId());
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

                // Ensure Z axis for Cartesian3D
                bool isCartesian3D = coordinateSystemProp != null && coordinateSystemProp.enumValueIndex == 2;
                if (isCartesian3D && zEl == null)
                {
                    zEl = EnsureAxisElement(axesProp, GetZAxisId(), AxisType.Value);
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

                // Build Z axis UI for Cartesian3D
                if (isCartesian3D && zEl != null)
                {
                    BuildAxisConfigUI(zAxisFields, zEl);
                }

                // Update Z axis box visibility
                zAxisBox.style.display = isCartesian3D ? DisplayStyle.Flex : DisplayStyle.None;
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

            // 3D Grid Settings
            var cartesian3DGridContainer = new VisualElement();
            gridSettingsFoldout.Add(cartesian3DGridContainer);

            if (cartesian3DGridProp != null)
            {
                SerializedProperty Rel3D(string name) => cartesian3DGridProp.FindPropertyRelative(name);

                var showProp = Rel3D("show");
                var gridColorProp = Rel3D("gridColor");
                var gridLineWidthProp = Rel3D("gridLineWidth");
                
                if (showProp != null) AddBoundPropertyField(cartesian3DGridContainer, showProp);
                if (gridColorProp != null) AddBoundPropertyField(cartesian3DGridContainer, gridColorProp);
                if (gridLineWidthProp != null) AddBoundPropertyField(cartesian3DGridContainer, gridLineWidthProp);

                var planesBox = new Box();
                planesBox.style.marginTop = 4;
                cartesian3DGridContainer.Add(planesBox);
                planesBox.Add(new Label("Plane Visibility"));

                var showXYPlane = Rel3D("showXYPlane");
                var showXZPlane = Rel3D("showXZPlane");
                var showYZPlane = Rel3D("showYZPlane");

                if (showXYPlane != null) AddBoundPropertyField(planesBox, showXYPlane);
                if (showXZPlane != null) AddBoundPropertyField(planesBox, showXZPlane);
                if (showYZPlane != null) AddBoundPropertyField(planesBox, showYZPlane);

                var colorsBox = new Box();
                colorsBox.style.marginTop = 4;
                cartesian3DGridContainer.Add(colorsBox);
                colorsBox.Add(new Label("Axis Colors"));

                var useAxisColors = Rel3D("useAxisColors");
                var xGridColor = Rel3D("xGridColor");
                var yGridColor = Rel3D("yGridColor");
                var zGridColor = Rel3D("zGridColor");

                if (useAxisColors != null) AddBoundPropertyField(colorsBox, useAxisColors);
                if (xGridColor != null) AddBoundPropertyField(colorsBox, xGridColor);
                if (yGridColor != null) AddBoundPropertyField(colorsBox, yGridColor);
                if (zGridColor != null) AddBoundPropertyField(colorsBox, zGridColor);

                var dimensionsBox = new Box();
                dimensionsBox.style.marginTop = 4;
                cartesian3DGridContainer.Add(dimensionsBox);
                dimensionsBox.Add(new Label("Grid Dimensions"));

                var gridWidth = Rel3D("gridWidth");
                var gridHeight = Rel3D("gridHeight");
                var gridDepth = Rel3D("gridDepth");

                if (gridWidth != null) AddBoundPropertyField(dimensionsBox, gridWidth);
                if (gridHeight != null) AddBoundPropertyField(dimensionsBox, gridHeight);
                if (gridDepth != null) AddBoundPropertyField(dimensionsBox, gridDepth);

                var labelsBox = new Box();
                labelsBox.style.marginTop = 4;
                cartesian3DGridContainer.Add(labelsBox);
                labelsBox.Add(new Label("Labels"));

                var showLabels = Rel3D("showLabels");
                var labelColor = Rel3D("labelColor");
                var labelFontSize = Rel3D("labelFontSize");

                if (showLabels != null) AddBoundPropertyField(labelsBox, showLabels);
                if (labelColor != null) AddBoundPropertyField(labelsBox, labelColor);
                if (labelFontSize != null) AddBoundPropertyField(labelsBox, labelFontSize);
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
                // 0 = Cartesian2D, 1 = Polar2D, 2 = Cartesian3D, 3 = None
                bool isCartesian2D = coordinateSystemProp.enumValueIndex == 0;
                bool isPolar = coordinateSystemProp.enumValueIndex == 1;
                bool isCartesian3D = coordinateSystemProp.enumValueIndex == 2;
                bool isNone = coordinateSystemProp.enumValueIndex == 3;
                bool isCartesian = isCartesian2D || isCartesian3D;

                // Cartesian axis selection for Cartesian2D and Cartesian3D
                cartesianAxisSelectionContainer.style.display = isCartesian ? DisplayStyle.Flex : DisplayStyle.None;

                // Z axis only for Cartesian3D
                zAxisContainer.style.display = isCartesian3D ? DisplayStyle.Flex : DisplayStyle.None;

                // Cartesian axes for Cartesian2D and Cartesian3D
                cartesianAxesContainer.style.display = isCartesian ? DisplayStyle.Flex : DisplayStyle.None;
                // Polar axes only for Polar2D
                polarAxesContainer.style.display = isPolar ? DisplayStyle.Flex : DisplayStyle.None;

                // Axis Settings box: hide for None coordinate system (no axes needed)
                axesBox.style.display = isNone ? DisplayStyle.None : DisplayStyle.Flex;

                // Grid settings - show 2D grid for Cartesian2D, 3D grid for Cartesian3D
                cartesianGridContainer.style.display = isCartesian2D ? DisplayStyle.Flex : DisplayStyle.None;
                cartesian3DGridContainer.style.display = isCartesian3D ? DisplayStyle.Flex : DisplayStyle.None;
                gridSettingsBox.style.display = isCartesian ? DisplayStyle.Flex : DisplayStyle.None;

                // Hover settings (cursor line) for Cartesian2D only
                hoverSettingsBox.style.display = isCartesian2D ? DisplayStyle.Flex : DisplayStyle.None;
            }

            UpdateCoordinateSpecificVisibility();
            if (coordinateSystemField != null)
            {
                coordinateSystemField.RegisterCallback<SerializedPropertyChangeEvent>(evt =>
                {
                    if (_serializedProfile == null) return;
                    if (coordinateSystemField.panel == null) return;
                    UpdateCoordinateSpecificVisibility();
                    RefreshActiveAxesUI();
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
                    iterator.name == "coordinateSystem" || iterator.name == "cartesian" || iterator.name == "cartesian3D" || iterator.name == "axes" || iterator.name == "axisSelectionInitialized" ||
                    iterator.name == "xAxisId" || iterator.name == "yAxisId" ||
                    iterator.name == "xGridColor" || iterator.name == "xGridLineWidth" || iterator.name == "yGridColor" || iterator.name == "yGridLineWidth" ||
                    iterator.name == "cartesianGrid" || iterator.name == "cartesian3DGrid" || iterator.name == "hover" || iterator.name == "polarAxes" || 
                    iterator.name == "gridSettingsInitialized" || iterator.name == "grid3DSettingsInitialized" || iterator.name == "hoverSettingsInitialized")
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
                    case "backgroundFX":
                        // Skip - handled separately with custom UI
                        continue;
                    default:
                        parent = chartSettingsFoldout;
                        break;
                }

                AddBoundPropertyField(parent, iterator.Copy());

                enterChildren = false;
            }

            // Background Settings (at bottom)
            var chartBackgroundProp = _serializedProfile.FindProperty("background");
            if (chartBackgroundProp != null)
            {
                var (bgContainer, bgFoldout) = CreateStyledFoldout("Background", foldoutKeyPrefix + "Background", true);
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
                bgBox.Add(bgContainer);
                chartSettingsFoldout.Add(bgBox);
            }

            // Background FX Settings (at bottom)
            var backgroundFXProp = _serializedProfile.FindProperty("backgroundFX");
            if (backgroundFXProp != null)
            {
                var (fxContainer, fxFoldout) = CreateStyledFoldout("Background FX", foldoutKeyPrefix + "BackgroundFX", false);
                
                var enabledProp = backgroundFXProp.FindPropertyRelative("enabled");
                var paddingProp = backgroundFXProp.FindPropertyRelative("padding");
                var bgColorProp = backgroundFXProp.FindPropertyRelative("backgroundColor");
                var layersProp = backgroundFXProp.FindPropertyRelative("layers");
                
                if (enabledProp != null) AddBoundPropertyField(fxFoldout, enabledProp);
                if (paddingProp != null) AddBoundPropertyField(fxFoldout, paddingProp);
                if (bgColorProp != null) AddBoundPropertyField(fxFoldout, bgColorProp);
                
                // Use helper class for Texture FX Layers UI
                if (layersProp != null)
                {
                    var layersUI = TextureFXLayersEditorHelper.CreateTextureFXLayersUI(
                        layersProp, 
                        "Texture FX Layers", 
                        ScheduleUpdatePreview);
                    fxFoldout.Add(layersUI);
                }

                var fxBox = CreateGroupBox();
                fxBox.Add(fxContainer);
                chartSettingsFoldout.Add(fxBox);
            }

            ScheduleRefreshSeriesList();
            ScheduleUpdatePreview();
        }

    }
}
