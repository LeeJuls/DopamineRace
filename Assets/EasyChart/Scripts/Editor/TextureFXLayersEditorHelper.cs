using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace EasyChart.Editor
{
    /// <summary>
    /// Editor utility class for creating Texture FX Layers UI.
    /// Reusable across different inspector panels.
    /// </summary>
    public static class TextureFXLayersEditorHelper
    {
        private static string _clipboard;
        private static string _singleLayerClipboard;

        private static void Notify(string message)
        {
            EditorWindow.focusedWindow?.ShowNotification(new GUIContent(message), 0.8);
        }

        /// <summary>
        /// Create a complete Texture FX Layers UI with header, icons, and property field.
        /// </summary>
        /// <param name="layersProp">SerializedProperty for the layers list</param>
        /// <param name="label">Header label text</param>
        /// <param name="onChanged">Optional callback when layers are modified</param>
        /// <returns>VisualElement containing the complete UI</returns>
        public static VisualElement CreateTextureFXLayersUI(
            SerializedProperty layersProp,
            string label = "Texture FX Layers",
            Action onChanged = null)
        {
            var root = new VisualElement();

            if (layersProp == null)
                return root;

            var serializedObject = layersProp.serializedObject;
            var propertyPath = layersProp.propertyPath;

            // The list container — rebuilt whenever the layer count changes
            var listContainer = new VisualElement();
            var foldoutStates = new System.Collections.Generic.List<bool>(); // persist expand/collapse per layer

            void SaveFoldoutStates()
            {
                foldoutStates.Clear();
                foreach (var child in listContainer.Children())
                {
                    if (child is Foldout f) foldoutStates.Add(f.value);
                }
            }

            void Rebuild()
            {
                SaveFoldoutStates();
                listContainer.Clear();
                serializedObject.Update();
                var prop = serializedObject.FindProperty(propertyPath);
                if (prop == null || !prop.isArray) return;

                for (int i = 0; i < prop.arraySize; i++)
                {
                    int idx = i;
                    var layerProp = prop.GetArrayElementAtIndex(i);
                    bool isExpanded = i < foldoutStates.Count ? foldoutStates[i] : false;

                    // --- item: Foldout with action buttons injected into toggle ---
                    var item = new Foldout { text = $"Layer {idx}", value = isExpanded };
                    // Set toggle row padding immediately — Q<Toggle>() works before attach
                    var itemToggle = item.Q<Toggle>();
                    if (itemToggle != null)
                    {
                        itemToggle.style.paddingTop = 3;
                        itemToggle.style.paddingBottom = 1;
                    }
                    item.style.borderTopWidth = 1;
                    item.style.borderBottomWidth = 1;
                    item.style.borderLeftWidth = 1;
                    item.style.borderRightWidth = 1;
                    var itemBorder = new Color(0.1f, 0.1f, 0.1f);
                    item.style.borderTopColor = itemBorder;
                    item.style.borderBottomColor = itemBorder;
                    item.style.borderLeftColor = itemBorder;
                    item.style.borderRightColor = itemBorder;
                    item.style.borderTopLeftRadius = 4;
                    item.style.borderTopRightRadius = 4;
                    item.style.borderBottomLeftRadius = 4;
                    item.style.borderBottomRightRadius = 4;
                    item.style.backgroundColor = new Color(0.22f, 0.22f, 0.22f);
                    item.style.paddingBottom = 0;

                    // Inject clone/up/down/delete buttons into the Foldout's toggle after layout
                    item.RegisterCallback<GeometryChangedEvent>(_ =>
                    {
                        var toggle = item.Q<Toggle>();
                        if (toggle == null) return;
                        if (toggle.Q<VisualElement>("layer-btns") != null) return; // already injected

                        // Make toggle a flex row with centered items; push label to grow so buttons land on right
                        toggle.style.flexDirection = FlexDirection.Row;
                        toggle.style.alignItems = Align.Center;
                        var toggleLabel = toggle.Q<Label>();
                        if (toggleLabel != null) toggleLabel.style.flexGrow = 1;

                        var btnRow = new VisualElement();
                        btnRow.name = "layer-btns";
                        btnRow.style.flexDirection = FlexDirection.Row;
                        btnRow.style.alignItems = Align.Center;
                        btnRow.style.marginRight = 0;

                        var copyIcon2 = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/EasyChart/Textures/Icon/Copy.png");
                        var copyImg = new Image { image = copyIcon2 };
                        copyImg.style.width = 16; copyImg.style.height = 16;
                        copyImg.tooltip = "Copy this layer";
                        copyImg.style.marginRight = 4;
                        copyImg.RegisterCallback<PointerDownEvent>(evt => evt.StopPropagation());
                        copyImg.RegisterCallback<ClickEvent>(evt =>
                        {
                            evt.StopPropagation();
                            serializedObject.Update();
                            var p = serializedObject.FindProperty(propertyPath);
                            if (p == null || !p.isArray) return;
                            if (idx < 0 || idx >= p.arraySize) return;
                            CopySingleLayer(p.GetArrayElementAtIndex(idx));
                            Notify("Layer copied");
                        });
                        btnRow.Add(copyImg);

                        var pasteIcon2 = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/EasyChart/Textures/Icon/Paste.png");
                        var pasteImg = new Image { image = pasteIcon2 };
                        pasteImg.style.width = 16; pasteImg.style.height = 16;
                        pasteImg.tooltip = "Paste (overwrite this layer)";
                        pasteImg.style.marginRight = 4;
                        pasteImg.RegisterCallback<PointerDownEvent>(evt => evt.StopPropagation());
                        pasteImg.RegisterCallback<ClickEvent>(evt =>
                        {
                            evt.StopPropagation();
                            if (string.IsNullOrEmpty(_singleLayerClipboard)) { Notify("Clipboard is empty"); return; }
                            serializedObject.Update();
                            var p = serializedObject.FindProperty(propertyPath);
                            if (p == null || !p.isArray) return;
                            if (idx < 0 || idx >= p.arraySize) { Notify("Layer index out of range"); return; }
                            var targetLayer = p.GetArrayElementAtIndex(idx);
                            try
                            {
                                var cb = JsonUtility.FromJson<TextureFXLayersClipboard>(_singleLayerClipboard);
                                if (cb == null || cb.layers == null || cb.layers.Count == 0) return;
                                var data = cb.layers[0];
                                var fillProp = targetLayer.FindPropertyRelative("fill");
                                if (fillProp != null)
                                {
                                    var texProp = fillProp.FindPropertyRelative("texture");
                                    if (texProp != null && !string.IsNullOrEmpty(data.textureGuid))
                                    {
                                        var path = AssetDatabase.GUIDToAssetPath(data.textureGuid);
                                        if (!string.IsNullOrEmpty(path))
                                            texProp.objectReferenceValue = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                                    }
                                    PasteFillProperties(fillProp, data);
                                }
                                PasteLayerProperties(targetLayer, data);
                                serializedObject.ApplyModifiedProperties();
                                onChanged?.Invoke(); Rebuild();
                                Notify("Layer pasted");
                            }
                            catch (Exception ex) { Debug.LogError($"[EasyChart] Paste layer failed: {ex.Message}"); }
                        });
                        btnRow.Add(pasteImg);

                        var cloneIcon = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/EasyChart/Textures/Icon/clone.png");
                        var cloneImg = new Image { image = cloneIcon };
                        cloneImg.style.width = 14; cloneImg.style.height = 14;
                        cloneImg.tooltip = "Clone this layer";
                        cloneImg.style.marginRight = 4;
                        cloneImg.RegisterCallback<PointerDownEvent>(evt => evt.StopPropagation());
                        cloneImg.RegisterCallback<ClickEvent>(evt =>
                        {
                            evt.StopPropagation();
                            SaveFoldoutStates();
                            foldoutStates.Insert(idx + 1, true);
                            serializedObject.Update();
                            var p = serializedObject.FindProperty(propertyPath);
                            if (p == null || !p.isArray) return;
                            p.InsertArrayElementAtIndex(idx);
                            serializedObject.ApplyModifiedProperties();
                            onChanged?.Invoke(); Rebuild();
                            Notify("Layer cloned");
                        });
                        btnRow.Add(cloneImg);

                        var upBtn = new Button(() =>
                        {
                            SaveFoldoutStates();
                            if (idx > 0 && idx < foldoutStates.Count)
                            { var tmp = foldoutStates[idx]; foldoutStates[idx] = foldoutStates[idx - 1]; foldoutStates[idx - 1] = tmp; }
                            serializedObject.Update();
                            var p = serializedObject.FindProperty(propertyPath);
                            if (p == null || !p.isArray) return;
                            p.MoveArrayElement(idx, idx - 1);
                            serializedObject.ApplyModifiedProperties();
                            onChanged?.Invoke(); Rebuild();
                            Notify("Moved up");
                        }) { text = "↑" };
                        upBtn.style.width = 22; upBtn.style.marginLeft = 2;
                        upBtn.style.paddingTop = 0; upBtn.style.paddingBottom = 0;
                        upBtn.style.paddingLeft = 0; upBtn.style.paddingRight = 0;
                        upBtn.style.justifyContent = Justify.Center;
                        upBtn.style.alignItems = Align.Center;
                        upBtn.SetEnabled(idx > 0);
                        btnRow.Add(upBtn);

                        var downBtn = new Button(() =>
                        {
                            SaveFoldoutStates();
                            if (idx >= 0 && idx + 1 < foldoutStates.Count)
                            { var tmp = foldoutStates[idx]; foldoutStates[idx] = foldoutStates[idx + 1]; foldoutStates[idx + 1] = tmp; }
                            serializedObject.Update();
                            var p = serializedObject.FindProperty(propertyPath);
                            if (p == null || !p.isArray) return;
                            p.MoveArrayElement(idx, idx + 1);
                            serializedObject.ApplyModifiedProperties();
                            onChanged?.Invoke(); Rebuild();
                            Notify("Moved down");
                        }) { text = "↓" };
                        downBtn.style.width = 22; downBtn.style.marginLeft = 2;
                        downBtn.style.paddingTop = 0; downBtn.style.paddingBottom = 0;
                        downBtn.style.paddingLeft = 0; downBtn.style.paddingRight = 0;
                        downBtn.style.justifyContent = Justify.Center;
                        downBtn.style.alignItems = Align.Center;
                        downBtn.SetEnabled(idx < serializedObject.FindProperty(propertyPath).arraySize - 1);
                        btnRow.Add(downBtn);

                        var deleteBtn = new Button(() =>
                        {
                            SaveFoldoutStates();
                            if (idx < foldoutStates.Count) foldoutStates.RemoveAt(idx);
                            serializedObject.Update();
                            var p = serializedObject.FindProperty(propertyPath);
                            if (p == null || !p.isArray) return;
                            p.DeleteArrayElementAtIndex(idx);
                            serializedObject.ApplyModifiedProperties();
                            onChanged?.Invoke(); Rebuild();
                            Notify("Layer deleted");
                        }) { text = "X" };
                        deleteBtn.style.width = 22; deleteBtn.style.marginLeft = 2;
                        deleteBtn.style.paddingTop = 0; deleteBtn.style.paddingBottom = 0;
                        deleteBtn.style.paddingLeft = 0; deleteBtn.style.paddingRight = 0;
                        deleteBtn.style.justifyContent = Justify.Center;
                        deleteBtn.style.alignItems = Align.Center;
                        btnRow.Add(deleteBtn);

                        toggle.Add(btnRow);
                    });

                    // --- body ---
                    var body = new VisualElement();

                    // Helper: make a PropertyField bound to a named relative property
                    PropertyField MakeField(SerializedProperty parent, string relName)
                    {
                        var p = parent.FindPropertyRelative(relName);
                        if (p == null) return null;
                        var pf = new PropertyField(p);
                        pf.BindProperty(p);
                        if (onChanged != null)
                            pf.RegisterCallback<SerializedPropertyChangeEvent>(_ => onChanged.Invoke());
                        return pf;
                    }

                    // Helper: flat section with a separator label (no foldout)
                    VisualElement MakeSection(string sectionLabel)
                    {
                        var section = new VisualElement();
                        section.style.marginTop = 6;
                        var sep = new Label(sectionLabel);
                        sep.style.fontSize = 10;
                        sep.style.color = new Color(0.55f, 0.55f, 0.55f);
                        sep.style.marginBottom = 2;
                        sep.style.borderBottomWidth = 1;
                        sep.style.borderBottomColor = new Color(0.35f, 0.35f, 0.35f);
                        section.Add(sep);
                        section.style.paddingLeft = 10;
                        return section;
                    }

                    // --- Fill (always expanded, no foldout) ---
                    var fillProp = layerProp.FindPropertyRelative("fill");
                    if (fillProp != null)
                    {
                        var fillContainer = new VisualElement();
                        fillContainer.style.paddingLeft = 10;
                        foreach (var fname in new[] { "texture", "tiling", "offset", "color", "adaptiveTiling" })
                        {
                            var pf = MakeField(fillProp, fname);
                            if (pf != null) fillContainer.Add(pf);
                        }
                        body.Add(fillContainer);
                    }

                    // --- Animation section ---
                    {
                        var section = MakeSection("Texture Animation");
                        var animTypeProp = layerProp.FindPropertyRelative("animationType");
                        var animTypePf = MakeField(layerProp, "animationType");
                        if (animTypePf != null) section.Add(animTypePf);

                        // UVMove sub-fields
                        var uvSubFields = new VisualElement();
                        foreach (var fname in new[] { "uvMoveSpeed" })
                        { var pf = MakeField(layerProp, fname); if (pf != null) uvSubFields.Add(pf); }
                        section.Add(uvSubFields);

                        // Scale sub-fields (Scale + RotateAndScale)
                        var scaleSubFields = new VisualElement();
                        foreach (var fname in new[] { "scaleType", "scaleSpeed", "scaleFrom", "scaleTo" })
                        { var pf = MakeField(layerProp, fname); if (pf != null) scaleSubFields.Add(pf); }
                        section.Add(scaleSubFields);

                        // RotateAndScale-only sub-fields
                        var rotateSubFields = new VisualElement();
                        foreach (var fname in new[] { "rotateSpeed", "layerCount", "colorOverLife" })
                        { var pf = MakeField(layerProp, fname); if (pf != null) rotateSubFields.Add(pf); }
                        section.Add(rotateSubFields);

                        void UpdateAnimSub()
                        {
                            if (animTypeProp == null) return;
                            var t = (TextureFillAnimationType)animTypeProp.enumValueIndex;
                            uvSubFields.style.display    = t == TextureFillAnimationType.TextureUvMove ? DisplayStyle.Flex : DisplayStyle.None;
                            bool isScale = t == TextureFillAnimationType.TextureScale || t == TextureFillAnimationType.RotateAndScale;
                            scaleSubFields.style.display  = isScale ? DisplayStyle.Flex : DisplayStyle.None;
                            rotateSubFields.style.display = t == TextureFillAnimationType.RotateAndScale ? DisplayStyle.Flex : DisplayStyle.None;
                        }
                        if (animTypeProp != null)
                            body.TrackPropertyValue(animTypeProp, _ => UpdateAnimSub());
                        UpdateAnimSub();
                        body.Add(section);
                    }

                    // --- Color Animation section ---
                    {
                        var section = MakeSection("Color Animation");
                        var colorTypePf = MakeField(layerProp, "colorAnimationType");
                        if (colorTypePf != null) section.Add(colorTypePf);

                        var colorSubFields = new VisualElement();
                        var colorTypeProp = layerProp.FindPropertyRelative("colorAnimationType");
                        void UpdateColorSub()
                        {
                            if (colorTypeProp == null) return;
                            bool visible = colorTypeProp.enumValueIndex != 0;
                            colorSubFields.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
                        }
                        foreach (var fname in new[] { "colorAnimationSpeed", "colorAnimationGradient" })
                        {
                            var pf = MakeField(layerProp, fname);
                            if (pf != null) colorSubFields.Add(pf);
                        }
                        if (colorTypeProp != null)
                            body.TrackPropertyValue(colorTypeProp, _ => UpdateColorSub());
                        UpdateColorSub();
                        section.Add(colorSubFields);
                        body.Add(section);
                    }

                    // --- Fade Effect section ---
                    {
                        var section = MakeSection("Fade Effect");
                        var fadeTypePf = MakeField(layerProp, "fadeType");
                        if (fadeTypePf != null) section.Add(fadeTypePf);

                        var fadeSubFields = new VisualElement();
                        var fadeTypeProp = layerProp.FindPropertyRelative("fadeType");
                        void UpdateFadeSub()
                        {
                            if (fadeTypeProp == null) return;
                            bool visible = fadeTypeProp.enumValueIndex != 0;
                            fadeSubFields.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
                        }
                        foreach (var fname in new[] { "fadeIntensity", "fadeSoftness" })
                        {
                            var pf = MakeField(layerProp, fname);
                            if (pf != null) fadeSubFields.Add(pf);
                        }
                        if (fadeTypeProp != null)
                            body.TrackPropertyValue(fadeTypeProp, _ => UpdateFadeSub());
                        UpdateFadeSub();
                        section.Add(fadeSubFields);
                        body.Add(section);
                    }

                    // --- Deform Effect section ---
                    {
                        var section = MakeSection("Deform Effect");
                        var deformTypePf = MakeField(layerProp, "deformType");
                        if (deformTypePf != null) section.Add(deformTypePf);

                        var deformSubFields = new VisualElement();
                        var deformTypeProp = layerProp.FindPropertyRelative("deformType");
                        void UpdateDeformSub()
                        {
                            if (deformTypeProp == null) return;
                            bool visible = deformTypeProp.enumValueIndex != 0;
                            deformSubFields.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
                        }
                        foreach (var fname in new[] { "deformIntensity", "deformSpeed", "waveFrequency" })
                        {
                            var pf = MakeField(layerProp, fname);
                            if (pf != null) deformSubFields.Add(pf);
                        }
                        if (deformTypeProp != null)
                            body.TrackPropertyValue(deformTypeProp, _ => UpdateDeformSub());
                        UpdateDeformSub();
                        section.Add(deformSubFields);
                        body.Add(section);
                    }

                    item.Add(body);
                    listContainer.Add(item);
                }

            }

            // --- Outer panel with tinted background ---
            var panel = new VisualElement();
            panel.style.borderTopWidth = 1;
            panel.style.borderBottomWidth = 1;
            panel.style.borderLeftWidth = 1;
            panel.style.borderRightWidth = 1;
            var panelBorder = new Color(.7f, 0.7f, 0.6f);
            panel.style.borderTopColor = panelBorder;
            panel.style.borderBottomColor = panelBorder;
            panel.style.borderLeftColor = panelBorder;
            panel.style.borderRightColor = panelBorder;
            panel.style.borderTopLeftRadius = 4;
            panel.style.borderTopRightRadius = 4;
            panel.style.borderBottomLeftRadius = 4;
            panel.style.borderBottomRightRadius = 4;
            panel.style.backgroundColor = new Color(0.15f, 0.15f, 0.15f);
            panel.style.paddingLeft = 8;
            panel.style.paddingRight = 8;
            panel.style.paddingTop = 8;
            panel.style.paddingBottom = 8;
            panel.style.marginBottom = 8;

            // --- Header row (label + Copy/Paste/Clear/Add icon buttons) ---
            var topHeader = new VisualElement();
            topHeader.style.flexDirection = FlexDirection.Row;
            topHeader.style.alignItems = Align.Center;
            topHeader.style.marginBottom = 4;

            var headerLabel = new Label(label);
            headerLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            headerLabel.style.flexGrow = 1;
            topHeader.Add(headerLabel);

            var iconBtns = CreateIconButtons(layersProp, () =>
            {
                onChanged?.Invoke();
                Rebuild();
            }, () =>
            {
                serializedObject.Update();
                var p = serializedObject.FindProperty(propertyPath);
                if (p == null || !p.isArray) return;
                p.InsertArrayElementAtIndex(p.arraySize);
                serializedObject.ApplyModifiedProperties();
                onChanged?.Invoke();
                Rebuild();
            });
            topHeader.Add(iconBtns);

            panel.Add(topHeader);
            panel.Add(listContainer);

            // Wrap panel in a relative container so the Pro overlay can be positioned absolutely on top
            var panelWrapper = new VisualElement();
            panelWrapper.style.position = Position.Relative;
            panelWrapper.Add(panel);

            if (!ProPackage.IsInstalled)
            {
                // Disable all editing interaction on the panel
                panel.SetEnabled(false);

                // Semi-transparent overlay covering the full panel
                var overlay = new VisualElement();
                overlay.style.position = Position.Absolute;
                overlay.style.left = 0; overlay.style.right = 0;
                overlay.style.top = 0; overlay.style.bottom = 8;
                overlay.style.backgroundColor = new Color(0.15f, 0.15f, 0.15f, 0.72f);
                overlay.style.borderTopLeftRadius = 4;
                overlay.style.borderTopRightRadius = 4;
                overlay.style.borderBottomLeftRadius = 4;
                overlay.style.borderBottomRightRadius = 4;
                overlay.style.flexDirection = FlexDirection.Row;
                overlay.style.alignItems = Align.Center;
                overlay.style.justifyContent = Justify.Center;
                overlay.pickingMode = PickingMode.Position;

                // Lock icon + message row — use absolute centering via a flex child
                var overlayInner = new VisualElement();
                overlayInner.style.position = Position.Absolute;
                overlayInner.style.left = StyleKeyword.Auto;
                overlayInner.style.right = StyleKeyword.Auto;
                overlayInner.style.top = new StyleLength(new Length(50, LengthUnit.Percent));
                overlayInner.style.translate = new StyleTranslate(new Translate(0, new Length(-50, LengthUnit.Percent)));
                overlayInner.style.flexDirection = FlexDirection.Row;
                overlayInner.style.alignItems = Align.Center;
                overlayInner.style.alignSelf = Align.Center;

                var lockLabel = new Label("\uD83D\uDD12");
                lockLabel.style.fontSize = 14;
                lockLabel.style.marginRight = 6;
                lockLabel.style.color = new Color(1f, 0.85f, 0.3f);
                overlayInner.Add(lockLabel);

                var msg = new Label("Texture FX Layers  \u00b7  EasyChart Pro");
                msg.style.fontSize = 11;
                msg.style.color = new Color(1f, 0.85f, 0.3f);
                msg.style.unityFontStyleAndWeight = FontStyle.Bold;
                overlayInner.Add(msg);

                overlay.Add(overlayInner);

                // ── Pro popup — delegate to EasyChartLibraryWindow ──────────
                bool OnlyClick() => EditorPrefs.GetBool("EasyChart.LibraryWindow.PopupOnlyClick", false);

                void ShowPopup()
                {
                    EasyChartLibraryWindow.RequestShowProPopup?.Invoke(overlay.worldBound);
                }

                overlay.RegisterCallback<PointerEnterEvent>(_ =>
                {
                    if (!OnlyClick()) ShowPopup();
                });
                overlay.RegisterCallback<ClickEvent>(_ => ShowPopup());

                panelWrapper.Add(overlay);
            }

            root.Add(panelWrapper);

            Rebuild();

            return root;
        }

        /// <summary>
        /// Create the header row with label and icon buttons.
        /// </summary>
        public static VisualElement CreateHeader(
            SerializedProperty layersProp,
            string label,
            Action onChanged = null)
        {
            var header = new VisualElement();
            header.style.flexDirection = FlexDirection.Row;
            header.style.justifyContent = Justify.SpaceBetween;
            header.style.alignItems = Align.Center;
            header.style.marginTop = 8;
            header.style.marginBottom = 4;

            var labelElement = new Label(label);
            labelElement.style.unityFontStyleAndWeight = FontStyle.Bold;
            header.Add(labelElement);

            var buttonsRow = CreateIconButtons(layersProp, onChanged);
            header.Add(buttonsRow);

            return header;
        }

        /// <summary>
        /// Create the icon buttons row (Copy, Paste, Clear).
        /// </summary>
        public static VisualElement CreateIconButtons(
            SerializedProperty layersProp,
            Action onChanged = null,
            Action onAdd = null)
        {
            var buttonsRow = new VisualElement();
            buttonsRow.style.flexDirection = FlexDirection.Row;

            var copyIcon = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/EasyChart/Textures/Icon/Copy.png");
            var pasteIcon = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/EasyChart/Textures/Icon/Paste.png");
            var pasteToAddIcon = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/EasyChart/Textures/Icon/PasteToAdd.png");
            var clearIcon = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/EasyChart/Textures/Icon/clear.png");
            var addIcon = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/EasyChart/Textures/Icon/add.png");

            // Copy button
            var copyBtn = CreateIconButton(copyIcon, "Copy Texture FX Layers", () =>
            {
                CopyLayers(layersProp);
                Notify("All layers copied");
            });
            buttonsRow.Add(copyBtn);

            // Paste button
            var pasteBtn = CreateIconButton(pasteIcon, "Paste Texture FX Layers", () =>
            {
                PasteLayers(layersProp);
                onChanged?.Invoke();
                Notify("Layers pasted");
            });
            pasteBtn.style.marginLeft = 8;
            buttonsRow.Add(pasteBtn);

            // Paste (Add) button — uses single layer clipboard first, falls back to full clipboard
            var pasteToAddBtn = CreateIconButton(pasteToAddIcon, "Paste To Add (uses single layer clipboard if available)", () =>
            {
                if (!string.IsNullOrEmpty(_singleLayerClipboard))
                { PasteSingleLayerToAdd(layersProp, onChanged); Notify("Layer added from clipboard"); }
                else if (!string.IsNullOrEmpty(_clipboard))
                { PasteLayersToAdd(layersProp); onChanged?.Invoke(); Notify("Layers added from clipboard"); }
                else
                    Notify("Clipboard is empty");
            });
            pasteToAddBtn.style.marginLeft = 8;
            buttonsRow.Add(pasteToAddBtn);

            // Clear button
            var clearBtn = CreateIconButton(clearIcon, "Clear all Texture FX Layers", () =>
            {
                ClearLayers(layersProp);
                onChanged?.Invoke();
                Notify("All layers cleared");
            });
            clearBtn.style.marginLeft = 8;
            buttonsRow.Add(clearBtn);

            // Add Layer button
            if (onAdd != null)
            {
                var addBtn = CreateIconButton(addIcon, "Add Layer", () => { onAdd(); Notify("Layer added"); });
                addBtn.style.marginLeft = 8;
                buttonsRow.Add(addBtn);
            }

            return buttonsRow;
        }

        /// <summary>
        /// Create a single icon button.
        /// </summary>
        private static Image CreateIconButton(Texture2D icon, string tooltip, Action onClick)
        {
            var btn = new Image { image = icon };
            btn.style.width = 16;
            btn.style.height = 16;
            btn.tooltip = tooltip;
            btn.RegisterCallback<ClickEvent>(evt => { evt.StopPropagation(); onClick?.Invoke(); });
            return btn;
        }

        /// <summary>
        /// Copy layers to clipboard.
        /// </summary>
        public static void CopyLayers(SerializedProperty layersProp)
        {
            if (layersProp == null || !layersProp.isArray) return;

            var layerDataList = new List<TextureFXLayerData>();
            for (int i = 0; i < layersProp.arraySize; i++)
            {
                var layerProp = layersProp.GetArrayElementAtIndex(i);
                var fillProp = layerProp.FindPropertyRelative("fill");
                if (fillProp == null) continue;

                var data = new TextureFXLayerData();

                // Copy texture as GUID
                var textureProp = fillProp.FindPropertyRelative("texture");
                if (textureProp != null && textureProp.objectReferenceValue != null)
                {
                    var path = AssetDatabase.GetAssetPath(textureProp.objectReferenceValue);
                    data.textureGuid = AssetDatabase.AssetPathToGUID(path);
                }

                CopyFillProperties(fillProp, data);
                CopyLayerProperties(layerProp, data);
                layerDataList.Add(data);
            }

            _clipboard = JsonUtility.ToJson(new TextureFXLayersClipboard { layers = layerDataList });
            _singleLayerClipboard = null;
            Debug.Log($"[EasyChart] Copied {layerDataList.Count} Texture FX Layer(s)");
        }

        /// <summary>
        /// Paste layers from clipboard.
        /// </summary>
        public static void PasteLayers(SerializedProperty layersProp)
        {
            if (layersProp == null || !layersProp.isArray) return;
            if (string.IsNullOrEmpty(_clipboard))
            {
                Debug.LogWarning("[EasyChart] No Texture FX Layers in clipboard");
                return;
            }

            try
            {
                var clipboard = JsonUtility.FromJson<TextureFXLayersClipboard>(_clipboard);
                if (clipboard == null || clipboard.layers == null || clipboard.layers.Count == 0)
                {
                    Debug.LogWarning("[EasyChart] Invalid clipboard data");
                    return;
                }

                layersProp.serializedObject.Update();
                layersProp.ClearArray();

                foreach (var data in clipboard.layers)
                {
                    layersProp.InsertArrayElementAtIndex(layersProp.arraySize);
                    var newLayerProp = layersProp.GetArrayElementAtIndex(layersProp.arraySize - 1);
                    var fillProp = newLayerProp.FindPropertyRelative("fill");
                    if (fillProp == null) continue;

                    // Restore texture from GUID
                    var textureProp = fillProp.FindPropertyRelative("texture");
                    if (textureProp != null && !string.IsNullOrEmpty(data.textureGuid))
                    {
                        var path = AssetDatabase.GUIDToAssetPath(data.textureGuid);
                        if (!string.IsNullOrEmpty(path))
                        {
                            textureProp.objectReferenceValue = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                        }
                    }

                    PasteFillProperties(fillProp, data);
                    PasteLayerProperties(newLayerProp, data);
                }

                layersProp.serializedObject.ApplyModifiedProperties();
                Debug.Log($"[EasyChart] Pasted {clipboard.layers.Count} Texture FX Layer(s)");
            }
            catch (Exception e)
            {
                Debug.LogError($"[EasyChart] Failed to paste Texture FX Layers: {e.Message}");
            }
        }

        /// <summary>
        /// Paste layers from clipboard and append to the current list.
        /// </summary>
        public static void PasteLayersToAdd(SerializedProperty layersProp)
        {
            if (layersProp == null || !layersProp.isArray) return;
            if (string.IsNullOrEmpty(_clipboard))
            {
                Debug.LogWarning("[EasyChart] No Texture FX Layers in clipboard");
                return;
            }

            try
            {
                var clipboard = JsonUtility.FromJson<TextureFXLayersClipboard>(_clipboard);
                if (clipboard == null || clipboard.layers == null || clipboard.layers.Count == 0)
                {
                    Debug.LogWarning("[EasyChart] Invalid clipboard data");
                    return;
                }

                layersProp.serializedObject.Update();

                foreach (var data in clipboard.layers)
                {
                    layersProp.InsertArrayElementAtIndex(layersProp.arraySize);
                    var newLayerProp = layersProp.GetArrayElementAtIndex(layersProp.arraySize - 1);
                    var fillProp = newLayerProp.FindPropertyRelative("fill");
                    if (fillProp == null) continue;

                    // Restore texture from GUID
                    var textureProp = fillProp.FindPropertyRelative("texture");
                    if (textureProp != null && !string.IsNullOrEmpty(data.textureGuid))
                    {
                        var path = AssetDatabase.GUIDToAssetPath(data.textureGuid);
                        if (!string.IsNullOrEmpty(path))
                        {
                            textureProp.objectReferenceValue = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                        }
                    }

                    PasteFillProperties(fillProp, data);
                    PasteLayerProperties(newLayerProp, data);
                }

                layersProp.serializedObject.ApplyModifiedProperties();
                Debug.Log($"[EasyChart] Pasted To Add {clipboard.layers.Count} Texture FX Layer(s)");
            }
            catch (Exception e)
            {
                Debug.LogError($"[EasyChart] Failed to paste-to-add Texture FX Layers: {e.Message}");
            }
        }

        /// <summary>
        /// Clear all layers.
        /// </summary>
        public static void ClearLayers(SerializedProperty layersProp)
        {
            if (layersProp == null || !layersProp.isArray) return;

            layersProp.serializedObject.Update();
            layersProp.ClearArray();
            layersProp.serializedObject.ApplyModifiedProperties();
            Debug.Log("[EasyChart] Cleared all Texture FX Layers");
        }

        public static void CopySingleLayer(SerializedProperty layerProp)
        {
            if (layerProp == null) return;
            var fillProp = layerProp.FindPropertyRelative("fill");
            if (fillProp == null) return;

            var data = new TextureFXLayerData();
            var textureProp = fillProp.FindPropertyRelative("texture");
            if (textureProp != null && textureProp.objectReferenceValue != null)
            {
                var path = AssetDatabase.GetAssetPath(textureProp.objectReferenceValue);
                data.textureGuid = AssetDatabase.AssetPathToGUID(path);
            }
            CopyFillProperties(fillProp, data);
            CopyLayerProperties(layerProp, data);
            _singleLayerClipboard = JsonUtility.ToJson(new TextureFXLayersClipboard { layers = new List<TextureFXLayerData> { data } });
            _clipboard = null;
            Debug.Log("[EasyChart] Copied 1 Texture FX Layer");
        }

        public static void PasteSingleLayerToAdd(SerializedProperty layersProp, Action onChanged = null)
        {
            if (layersProp == null || !layersProp.isArray) return;
            if (string.IsNullOrEmpty(_singleLayerClipboard))
            {
                Debug.LogWarning("[EasyChart] No single layer in clipboard");
                return;
            }
            try
            {
                var clipboard = JsonUtility.FromJson<TextureFXLayersClipboard>(_singleLayerClipboard);
                if (clipboard == null || clipboard.layers == null || clipboard.layers.Count == 0) return;

                layersProp.serializedObject.Update();
                var data = clipboard.layers[0];
                layersProp.InsertArrayElementAtIndex(layersProp.arraySize);
                var newLayerProp = layersProp.GetArrayElementAtIndex(layersProp.arraySize - 1);
                var fillProp = newLayerProp.FindPropertyRelative("fill");
                if (fillProp != null)
                {
                    var textureProp = fillProp.FindPropertyRelative("texture");
                    if (textureProp != null && !string.IsNullOrEmpty(data.textureGuid))
                    {
                        var path = AssetDatabase.GUIDToAssetPath(data.textureGuid);
                        if (!string.IsNullOrEmpty(path))
                            textureProp.objectReferenceValue = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                    }
                    PasteFillProperties(fillProp, data);
                }
                PasteLayerProperties(newLayerProp, data);
                layersProp.serializedObject.ApplyModifiedProperties();
                onChanged?.Invoke();
                Debug.Log("[EasyChart] Pasted 1 Texture FX Layer");
            }
            catch (Exception e)
            {
                Debug.LogError($"[EasyChart] Failed to paste single layer: {e.Message}");
            }
        }

        private static void CopyFillProperties(SerializedProperty fillProp, TextureFXLayerData data)
        {
            // Fill only has base properties now
            var tilingProp = fillProp.FindPropertyRelative("tiling");
            var offsetProp = fillProp.FindPropertyRelative("offset");
            var colorProp = fillProp.FindPropertyRelative("color");

            if (tilingProp != null) data.tiling = tilingProp.vector2Value;
            if (offsetProp != null) data.offset = offsetProp.vector2Value;
            if (colorProp != null) data.color = colorProp.colorValue;
        }

        private static void CopyLayerProperties(SerializedProperty layerProp, TextureFXLayerData data)
        {
            // Animation properties (now on layer)
            var animTypeProp = layerProp.FindPropertyRelative("animationType");
            var uvSpeedProp = layerProp.FindPropertyRelative("uvMoveSpeed");
            var scaleTypeProp = layerProp.FindPropertyRelative("scaleType");
            var scaleSpeedProp = layerProp.FindPropertyRelative("scaleSpeed");
            var scaleFromProp = layerProp.FindPropertyRelative("scaleFrom");
            var scaleToProp = layerProp.FindPropertyRelative("scaleTo");
            var colorAnimTypeProp = layerProp.FindPropertyRelative("colorAnimationType");
            var colorAnimSpeedProp = layerProp.FindPropertyRelative("colorAnimationSpeed");
            // Fade effect properties
            var fadeTypeProp = layerProp.FindPropertyRelative("fadeType");
            var fadeIntensityProp = layerProp.FindPropertyRelative("fadeIntensity");
            var fadeSoftnessProp = layerProp.FindPropertyRelative("fadeSoftness");
            // Deform effect properties
            var deformTypeProp = layerProp.FindPropertyRelative("deformType");
            var deformIntensityProp = layerProp.FindPropertyRelative("deformIntensity");
            var deformSpeedProp = layerProp.FindPropertyRelative("deformSpeed");
            var waveFrequencyProp = layerProp.FindPropertyRelative("waveFrequency");

            if (animTypeProp != null) data.animationType = animTypeProp.enumValueIndex;
            if (uvSpeedProp != null) data.uvMoveSpeed = uvSpeedProp.vector2Value;
            if (scaleTypeProp != null) data.scaleType = scaleTypeProp.enumValueIndex;
            if (scaleSpeedProp != null) data.scaleSpeed = scaleSpeedProp.floatValue;
            if (scaleFromProp != null) data.scaleFrom = scaleFromProp.vector2Value;
            if (scaleToProp != null) data.scaleTo = scaleToProp.vector2Value;
            // RotateAndScale properties
            var rotateSpeedProp = layerProp.FindPropertyRelative("rotateSpeed");
            var layerCountProp = layerProp.FindPropertyRelative("layerCount");
            var colorOverLifeProp = layerProp.FindPropertyRelative("colorOverLife");
            if (rotateSpeedProp != null) data.rotateSpeed = rotateSpeedProp.floatValue;
            if (layerCountProp != null) data.layerCount = layerCountProp.intValue;
            if (colorOverLifeProp != null)
            {
                var gradient = GetGradientFromProperty(colorOverLifeProp);
                data.colorOverLife = GradientData.FromGradient(gradient);
            }
            if (colorAnimTypeProp != null) data.colorAnimationType = colorAnimTypeProp.enumValueIndex;
            if (colorAnimSpeedProp != null) data.colorAnimationSpeed = colorAnimSpeedProp.floatValue;
            // Color animation gradient
            var colorAnimGradientProp = layerProp.FindPropertyRelative("colorAnimationGradient");
            if (colorAnimGradientProp != null)
            {
                var gradient = GetGradientFromProperty(colorAnimGradientProp);
                data.colorAnimationGradient = GradientData.FromGradient(gradient);
            }
            // Fade effect
            if (fadeTypeProp != null) data.fadeType = fadeTypeProp.enumValueIndex;
            if (fadeIntensityProp != null) data.fadeIntensity = fadeIntensityProp.floatValue;
            if (fadeSoftnessProp != null) data.fadeSoftness = fadeSoftnessProp.floatValue;
            // Deform effect
            if (deformTypeProp != null) data.deformType = deformTypeProp.enumValueIndex;
            if (deformIntensityProp != null) data.deformIntensity = deformIntensityProp.floatValue;
            if (deformSpeedProp != null) data.deformSpeed = deformSpeedProp.floatValue;
            if (waveFrequencyProp != null) data.waveFrequency = waveFrequencyProp.floatValue;
        }

        private static void PasteFillProperties(SerializedProperty fillProp, TextureFXLayerData data)
        {
            // Fill only has base properties now
            var tilingProp = fillProp.FindPropertyRelative("tiling");
            var offsetProp = fillProp.FindPropertyRelative("offset");
            var colorProp = fillProp.FindPropertyRelative("color");

            if (tilingProp != null) tilingProp.vector2Value = data.tiling;
            if (offsetProp != null) offsetProp.vector2Value = data.offset;
            if (colorProp != null) colorProp.colorValue = data.color;
        }

        private static Gradient GetGradientFromProperty(SerializedProperty prop)
        {
            if (prop == null) return null;

            // Try using Unity's built-in gradientValue property first
            try
            {
                var gradientValueProp = typeof(SerializedProperty).GetProperty("gradientValue",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
                if (gradientValueProp != null)
                {
                    var gradient = gradientValueProp.GetValue(prop) as Gradient;
                    if (gradient != null) return gradient;
                }
            }
            catch { }

            // Fallback: use reflection to traverse the property path
            var targetObject = prop.serializedObject.targetObject;
            return GetGradientViaReflection(targetObject, prop.propertyPath);
        }

        private static Gradient GetGradientViaReflection(object targetObject, string propertyPath)
        {
            if (targetObject == null || string.IsNullOrEmpty(propertyPath)) return null;

            var path = propertyPath.Split('.');
            object obj = targetObject;

            for (int i = 0; i < path.Length && obj != null; i++)
            {
                var part = path[i];
                if (part == "Array")
                {
                    if (i + 1 < path.Length)
                    {
                        var indexStr = path[++i];
                        var index = int.Parse(indexStr.Replace("data[", "").Replace("]", ""));
                        if (obj is System.Collections.IList list && index < list.Count)
                        {
                            obj = list[index];
                        }
                        else
                        {
                            return null;
                        }
                    }
                }
                else
                {
                    var field = obj.GetType().GetField(part,
                        System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
                    if (field != null)
                    {
                        obj = field.GetValue(obj);
                    }
                    else
                    {
                        return null;
                    }
                }
            }
            return obj as Gradient;
        }

        private static void SetGradientToProperty(SerializedProperty prop, Gradient gradient)
        {
            if (prop == null || gradient == null) return;
            
            // For Gradient, we need to use SerializedProperty's internal methods
            // Unity doesn't expose a direct way to set Gradient via SerializedProperty
            // We'll use the gradientValue property which is available in newer Unity versions
            try
            {
                var gradientValueProp = typeof(SerializedProperty).GetProperty("gradientValue",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
                if (gradientValueProp != null)
                {
                    gradientValueProp.SetValue(prop, gradient);
                }
            }
            catch
            {
                // Fallback: use reflection to set the value directly on the target object
                var targetObject = prop.serializedObject.targetObject;
                SetGradientViaReflection(targetObject, prop.propertyPath, gradient);
                EditorUtility.SetDirty(targetObject);
            }
        }

        private static void SetGradientViaReflection(object targetObject, string propertyPath, Gradient gradient)
        {
            var path = propertyPath.Split('.');
            object obj = targetObject;
            object parent = null;
            string lastField = null;
            int lastIndex = -1;

            for (int i = 0; i < path.Length && obj != null; i++)
            {
                var part = path[i];
                if (part == "Array")
                {
                    var indexStr = path[++i];
                    lastIndex = int.Parse(indexStr.Replace("data[", "").Replace("]", ""));
                    parent = obj;
                    obj = ((System.Collections.IList)obj)[lastIndex];
                    lastField = null;
                }
                else
                {
                    var field = obj.GetType().GetField(part,
                        System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
                    if (i == path.Length - 1)
                    {
                        field?.SetValue(obj, gradient);
                        return;
                    }
                    parent = obj;
                    lastField = part;
                    lastIndex = -1;
                    obj = field?.GetValue(obj);
                }
            }
        }

        private static void PasteLayerProperties(SerializedProperty layerProp, TextureFXLayerData data)
        {
            // Animation properties (now on layer)
            var animTypeProp = layerProp.FindPropertyRelative("animationType");
            var uvSpeedProp = layerProp.FindPropertyRelative("uvMoveSpeed");
            var scaleTypeProp = layerProp.FindPropertyRelative("scaleType");
            var scaleSpeedProp = layerProp.FindPropertyRelative("scaleSpeed");
            var scaleFromProp = layerProp.FindPropertyRelative("scaleFrom");
            var scaleToProp = layerProp.FindPropertyRelative("scaleTo");
            var colorAnimTypeProp = layerProp.FindPropertyRelative("colorAnimationType");
            var colorAnimSpeedProp = layerProp.FindPropertyRelative("colorAnimationSpeed");
            // Fade effect properties
            var fadeTypeProp = layerProp.FindPropertyRelative("fadeType");
            var fadeIntensityProp = layerProp.FindPropertyRelative("fadeIntensity");
            var fadeSoftnessProp = layerProp.FindPropertyRelative("fadeSoftness");
            // Deform effect properties
            var deformTypeProp = layerProp.FindPropertyRelative("deformType");
            var deformIntensityProp = layerProp.FindPropertyRelative("deformIntensity");
            var deformSpeedProp = layerProp.FindPropertyRelative("deformSpeed");
            var waveFrequencyProp = layerProp.FindPropertyRelative("waveFrequency");

            if (animTypeProp != null) animTypeProp.enumValueIndex = data.animationType;
            if (uvSpeedProp != null) uvSpeedProp.vector2Value = data.uvMoveSpeed;
            if (scaleTypeProp != null) scaleTypeProp.enumValueIndex = data.scaleType;
            if (scaleSpeedProp != null) scaleSpeedProp.floatValue = data.scaleSpeed;
            if (scaleFromProp != null) scaleFromProp.vector2Value = data.scaleFrom;
            if (scaleToProp != null) scaleToProp.vector2Value = data.scaleTo;
            // RotateAndScale properties
            var rotateSpeedProp = layerProp.FindPropertyRelative("rotateSpeed");
            var layerCountProp = layerProp.FindPropertyRelative("layerCount");
            var colorOverLifeProp = layerProp.FindPropertyRelative("colorOverLife");
            if (rotateSpeedProp != null) rotateSpeedProp.floatValue = data.rotateSpeed;
            if (layerCountProp != null) layerCountProp.intValue = data.layerCount;
            if (colorOverLifeProp != null && data.colorOverLife != null)
            {
                SetGradientToProperty(colorOverLifeProp, data.colorOverLife.ToGradient());
            }
            if (colorAnimTypeProp != null) colorAnimTypeProp.enumValueIndex = data.colorAnimationType;
            if (colorAnimSpeedProp != null) colorAnimSpeedProp.floatValue = data.colorAnimationSpeed;
            // Color animation gradient
            var colorAnimGradientProp = layerProp.FindPropertyRelative("colorAnimationGradient");
            if (colorAnimGradientProp != null && data.colorAnimationGradient != null)
            {
                SetGradientToProperty(colorAnimGradientProp, data.colorAnimationGradient.ToGradient());
            }
            // Fade effect
            if (fadeTypeProp != null) fadeTypeProp.enumValueIndex = data.fadeType;
            if (fadeIntensityProp != null) fadeIntensityProp.floatValue = data.fadeIntensity;
            if (fadeSoftnessProp != null) fadeSoftnessProp.floatValue = data.fadeSoftness;
            // Deform effect
            if (deformTypeProp != null) deformTypeProp.enumValueIndex = data.deformType;
            if (deformIntensityProp != null) deformIntensityProp.floatValue = data.deformIntensity;
            if (deformSpeedProp != null) deformSpeedProp.floatValue = data.deformSpeed;
            if (waveFrequencyProp != null) waveFrequencyProp.floatValue = data.waveFrequency;
        }

        [Serializable]
        private class TextureFXLayerData
        {
            public string textureGuid;
            public Vector2 tiling = Vector2.one;
            public Vector2 offset = Vector2.zero;
            public Color color = Color.white;
            public int animationType;
            public Vector2 uvMoveSpeed;
            public int scaleType;
            public float scaleSpeed = 1f;
            public Vector2 scaleFrom = Vector2.one;
            public Vector2 scaleTo = new Vector2(1.2f, 1.2f);
            public float rotateSpeed = 45f;
            public int layerCount = 1;
            public GradientData colorOverLife;
            public int colorAnimationType;
            public float colorAnimationSpeed = 1f;
            public GradientData colorAnimationGradient;
            // Fade effect
            public int fadeType;
            public float fadeIntensity = 0.5f;
            public float fadeSoftness = 0.5f;
            // Deform effect
            public int deformType;
            public float deformIntensity = 0.1f;
            public float deformSpeed = 1f;
            public float waveFrequency = 4f;
        }

        [Serializable]
        private class GradientData
        {
            public GradientColorKeyData[] colorKeys;
            public GradientAlphaKeyData[] alphaKeys;
            public int mode;

            public static GradientData FromGradient(Gradient gradient)
            {
                if (gradient == null) return null;
                var data = new GradientData
                {
                    mode = (int)gradient.mode,
                    colorKeys = new GradientColorKeyData[gradient.colorKeys.Length],
                    alphaKeys = new GradientAlphaKeyData[gradient.alphaKeys.Length]
                };
                for (int i = 0; i < gradient.colorKeys.Length; i++)
                {
                    var key = gradient.colorKeys[i];
                    data.colorKeys[i] = new GradientColorKeyData { color = key.color, time = key.time };
                }
                for (int i = 0; i < gradient.alphaKeys.Length; i++)
                {
                    var key = gradient.alphaKeys[i];
                    data.alphaKeys[i] = new GradientAlphaKeyData { alpha = key.alpha, time = key.time };
                }
                return data;
            }

            public Gradient ToGradient()
            {
                var gradient = new Gradient { mode = (GradientMode)mode };
                var colorKeys = new GradientColorKey[this.colorKeys?.Length ?? 0];
                var alphaKeys = new GradientAlphaKey[this.alphaKeys?.Length ?? 0];
                for (int i = 0; i < colorKeys.Length; i++)
                {
                    colorKeys[i] = new GradientColorKey(this.colorKeys[i].color, this.colorKeys[i].time);
                }
                for (int i = 0; i < alphaKeys.Length; i++)
                {
                    alphaKeys[i] = new GradientAlphaKey(this.alphaKeys[i].alpha, this.alphaKeys[i].time);
                }
                gradient.SetKeys(colorKeys, alphaKeys);
                return gradient;
            }
        }

        [Serializable]
        private class GradientColorKeyData
        {
            public Color color;
            public float time;
        }

        [Serializable]
        private class GradientAlphaKeyData
        {
            public float alpha;
            public float time;
        }

        [Serializable]
        private class TextureFXLayersClipboard
        {
            public List<TextureFXLayerData> layers;
        }

    }
}
