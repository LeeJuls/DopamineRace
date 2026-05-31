using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using EasyChart;
using EasyChart.UGUI;

namespace EasyChart.Editor
{
    /// <summary>
    /// Editor window for building example scenes from a Library folder.
    /// Creates a preview scene with left sidebar showing folder names and right panel showing charts.
    /// </summary>
    public class ExampleSceneBuilder : EditorWindow
    {
        private const string LIBRARY_ROOT = "Assets/EasyChart/Library";
        
        private string _selectedLibraryPath;
        private string _selectedLibraryName;
        private Vector2 _scrollPosition;
        private List<string> _availableLibraries = new List<string>();
        private int _selectedLibraryIndex = -1;
        private string _customSceneName = "";
        
        [MenuItem("EasyChart/Example Scene Builder", false, 200)]
        public static void ShowWindow()
        {
            var window = GetWindow<ExampleSceneBuilder>();
            window.titleContent = new GUIContent("Example Scene Builder");
            window.minSize = new Vector2(400, 300);
            window.Show();
        }
        
        private void OnEnable()
        {
            RefreshLibraryList();
        }
        
        private void RefreshLibraryList()
        {
            _availableLibraries.Clear();
            
            if (!Directory.Exists(LIBRARY_ROOT))
            {
                return;
            }
            
            var directories = Directory.GetDirectories(LIBRARY_ROOT);
            foreach (var dir in directories)
            {
                var dirName = Path.GetFileName(dir);
                if (!dirName.StartsWith("."))
                {
                    _availableLibraries.Add(dirName);
                }
            }
        }
        
        private void OnGUI()
        {
            EditorGUILayout.Space(10);
            
            // Title
            EditorGUILayout.LabelField("Example Scene Builder", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);
            EditorGUILayout.HelpBox(
                "Select a Library folder to generate an example scene.\n" +
                "The scene will have a left sidebar with folder names and a right panel showing all charts.",
                MessageType.Info);
            EditorGUILayout.Space(10);
            
            // Library Selection
            EditorGUILayout.LabelField("Select Library", EditorStyles.boldLabel);
            
            if (_availableLibraries.Count == 0)
            {
                EditorGUILayout.HelpBox("No libraries found in " + LIBRARY_ROOT, MessageType.Warning);
                if (GUILayout.Button("Refresh"))
                {
                    RefreshLibraryList();
                }
                return;
            }
            
            var libraryNames = _availableLibraries.ToArray();
            int newIndex = EditorGUILayout.Popup("Library", _selectedLibraryIndex, libraryNames);
            if (newIndex != _selectedLibraryIndex)
            {
                _selectedLibraryIndex = newIndex;
                if (_selectedLibraryIndex >= 0 && _selectedLibraryIndex < _availableLibraries.Count)
                {
                    _selectedLibraryName = _availableLibraries[_selectedLibraryIndex];
                    _selectedLibraryPath = Path.Combine(LIBRARY_ROOT, _selectedLibraryName);
                    // Auto-fill default scene name
                    _customSceneName = $"{_selectedLibraryName}_Preview";
                }
            }
            
            EditorGUILayout.Space(10);
            
            // Show library info
            if (_selectedLibraryIndex >= 0 && !string.IsNullOrEmpty(_selectedLibraryPath))
            {
                EditorGUILayout.LabelField("Library Info", EditorStyles.boldLabel);
                
                var folders = GetChartFolders(_selectedLibraryPath);
                var totalProfiles = 0;
                
                _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, GUILayout.Height(150));
                foreach (var folder in folders)
                {
                    var profiles = GetChartProfiles(folder);
                    totalProfiles += profiles.Count;
                    EditorGUILayout.LabelField($"  {Path.GetFileName(folder)}: {profiles.Count} charts");
                }
                EditorGUILayout.EndScrollView();
                
                EditorGUILayout.LabelField($"Total: {folders.Count} folders, {totalProfiles} charts");
            }
            
            EditorGUILayout.Space(20);
            
            // Build buttons
            EditorGUILayout.LabelField("Build Scene", EditorStyles.boldLabel);
            
            // Custom scene name input
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Scene Name", GUILayout.Width(80));
            _customSceneName = EditorGUILayout.TextField(_customSceneName);
            EditorGUILayout.EndHorizontal();
            
            // Show preview of generated file paths
            if (_selectedLibraryIndex >= 0 && !string.IsNullOrEmpty(_customSceneName))
            {
                EditorGUILayout.HelpBox($"UGUI: {_customSceneName}_UGUI.unity\nUIToolkit: {_customSceneName}_UIToolkit.unity", MessageType.None);
            }
            
            EditorGUILayout.Space(10);
            
            EditorGUI.BeginDisabledGroup(_selectedLibraryIndex < 0);
            
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Build UGUI Preview", GUILayout.Height(40)))
            {
                BuildUGUIPreviewScene();
            }
            
            if (GUILayout.Button("Build UIToolkit Preview", GUILayout.Height(40)))
            {
                BuildUIToolkitPreviewScene();
            }
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUI.EndDisabledGroup();
            
            EditorGUILayout.Space(10);
            
            // Refresh button
            if (GUILayout.Button("Refresh Library List"))
            {
                RefreshLibraryList();
            }
        }
        
        private List<string> GetChartFolders(string libraryPath)
        {
            var folders = new List<string>();
            
            if (!Directory.Exists(libraryPath))
                return folders;
            
            var directories = Directory.GetDirectories(libraryPath);
            foreach (var dir in directories)
            {
                var dirName = Path.GetFileName(dir);
                if (!dirName.StartsWith("."))
                {
                    // Check if folder contains any ChartProfile assets
                    var profiles = GetChartProfiles(dir);
                    if (profiles.Count > 0)
                    {
                        folders.Add(dir);
                    }
                }
            }
            
            return folders.OrderBy(f => Path.GetFileName(f)).ToList();
        }
        
        private List<ChartProfile> GetChartProfiles(string folderPath)
        {
            var profiles = new List<ChartProfile>();
            
            var guids = AssetDatabase.FindAssets("t:ChartProfile", new[] { folderPath });
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                // Only include profiles directly in this folder, not subfolders
                if (Path.GetDirectoryName(path).Replace("\\", "/") == folderPath.Replace("\\", "/"))
                {
                    var profile = AssetDatabase.LoadAssetAtPath<ChartProfile>(path);
                    if (profile != null)
                    {
                        profiles.Add(profile);
                    }
                }
            }
            
            return profiles.OrderBy(p => p.name).ToList();
        }
        
        private string GetSceneName()
        {
            if (!string.IsNullOrEmpty(_customSceneName))
            {
                return _customSceneName;
            }
            return $"{_selectedLibraryName}_Preview";
        }
        
        private void BuildUGUIPreviewScene()
        {
            if (string.IsNullOrEmpty(_selectedLibraryPath))
            {
                EditorUtility.DisplayDialog("Error", "Please select a library first.", "OK");
                return;
            }
            
            var sceneName = GetSceneName();
            var scenePath = $"Assets/EasyChart/Demo/Scenes/{sceneName}_UGUI.unity";
            
            // Check if scene already exists
            if (File.Exists(scenePath))
            {
                if (!EditorUtility.DisplayDialog("Overwrite Scene", 
                    $"Scene already exists:\n{scenePath}\n\nDo you want to overwrite it?", 
                    "Overwrite", "Cancel"))
                {
                    return;
                }
            }
            
            // Create new scene
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            
            // Get folders and profiles
            var folders = GetChartFolders(_selectedLibraryPath);
            
            // Create EventSystem for UI interaction
            var eventSystemGO = new GameObject("EventSystem");
            eventSystemGO.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystemGO.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            
            // Create Canvas
            var canvasGO = new GameObject("Canvas");
            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGO.AddComponent<UnityEngine.UI.CanvasScaler>();
            canvasGO.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            
            // Create main layout
            var mainLayout = new GameObject("MainLayout");
            mainLayout.transform.SetParent(canvasGO.transform, false);
            var mainRect = mainLayout.AddComponent<RectTransform>();
            mainRect.anchorMin = Vector2.zero;
            mainRect.anchorMax = Vector2.one;
            mainRect.offsetMin = Vector2.zero;
            mainRect.offsetMax = Vector2.zero;
            var mainHorizontal = mainLayout.AddComponent<UnityEngine.UI.HorizontalLayoutGroup>();
            mainHorizontal.childControlWidth = true;
            mainHorizontal.childControlHeight = true;
            mainHorizontal.childForceExpandWidth = false;
            mainHorizontal.childForceExpandHeight = true;
            
            // Create left sidebar
            var sidebar = new GameObject("Sidebar");
            sidebar.transform.SetParent(mainLayout.transform, false);
            var sidebarRect = sidebar.AddComponent<RectTransform>();
            var sidebarLayout = sidebar.AddComponent<UnityEngine.UI.LayoutElement>();
            sidebarLayout.preferredWidth = 200;
            sidebarLayout.flexibleWidth = 0;
            var sidebarImage = sidebar.AddComponent<UnityEngine.UI.Image>();
            sidebarImage.color = new Color(0.15f, 0.15f, 0.15f, 0.9f); // Slightly transparent
            
            // Create sidebar scroll view
            var sidebarScroll = CreateScrollView(sidebar.transform, "SidebarScroll");
            var sidebarContent = sidebarScroll.transform.Find("Viewport/Content");
            var sidebarVertical = sidebarContent.gameObject.AddComponent<UnityEngine.UI.VerticalLayoutGroup>();
            sidebarVertical.padding = new RectOffset(10, 10, 10, 10);
            sidebarVertical.spacing = 5;
            sidebarVertical.childControlWidth = true;
            sidebarVertical.childControlHeight = false;
            sidebarVertical.childForceExpandWidth = true;
            sidebarVertical.childForceExpandHeight = false;
            
            // Create right content area
            var contentArea = new GameObject("ContentArea");
            contentArea.transform.SetParent(mainLayout.transform, false);
            var contentRect = contentArea.AddComponent<RectTransform>();
            var contentLayout = contentArea.AddComponent<UnityEngine.UI.LayoutElement>();
            contentLayout.flexibleWidth = 1;
            var contentImage = contentArea.AddComponent<UnityEngine.UI.Image>();
            contentImage.color = new Color(0.2f, 0.2f, 0.2f, 0.85f); // Slightly transparent to show 3D scene
            
            // Create content scroll view
            var contentScroll = CreateScrollView(contentArea.transform, "ContentScroll");
            var contentContainer = contentScroll.transform.Find("Viewport/Content");
            // Don't add GridLayoutGroup to Content - each Charts_* container will have its own
            
            // Add ExampleSceneController (from Runtime assembly)
            var controller = canvasGO.AddComponent<EasyChart.ExampleSceneController>();
            
            // Create folder buttons and chart containers
            int folderIndex = 0;
            foreach (var folder in folders)
            {
                var folderName = Path.GetFileName(folder);
                var profiles = GetChartProfiles(folder);
                
                // Create sidebar button
                var buttonGO = new GameObject($"Btn_{folderName}");
                buttonGO.transform.SetParent(sidebarContent, false);
                var buttonRect = buttonGO.AddComponent<RectTransform>();
                buttonRect.sizeDelta = new Vector2(0, 40);
                var buttonLayout = buttonGO.AddComponent<UnityEngine.UI.LayoutElement>();
                buttonLayout.preferredHeight = 40;
                var buttonImage = buttonGO.AddComponent<UnityEngine.UI.Image>();
                buttonImage.color = new Color(0.3f, 0.3f, 0.3f, 1f);
                var button = buttonGO.AddComponent<UnityEngine.UI.Button>();
                button.targetGraphic = buttonImage;
                
                // Button text
                var textGO = new GameObject("Text");
                textGO.transform.SetParent(buttonGO.transform, false);
                var textRect = textGO.AddComponent<RectTransform>();
                textRect.anchorMin = Vector2.zero;
                textRect.anchorMax = Vector2.one;
                textRect.offsetMin = new Vector2(10, 0);
                textRect.offsetMax = new Vector2(-10, 0);
                var text = textGO.AddComponent<UnityEngine.UI.Text>();
                text.text = folderName;
                text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                text.fontSize = 16;
                text.color = Color.white;
                text.alignment = TextAnchor.MiddleLeft;
                
                // Create chart container for this folder - fills the entire content area
                var chartContainer = new GameObject($"Charts_{folderName}");
                chartContainer.transform.SetParent(contentContainer, false);
                var containerRect = chartContainer.AddComponent<RectTransform>();
                // Anchor to fill parent width, height determined by content
                containerRect.anchorMin = new Vector2(0, 1);
                containerRect.anchorMax = new Vector2(1, 1);
                containerRect.pivot = new Vector2(0.5f, 1);
                containerRect.anchoredPosition = Vector2.zero;
                containerRect.offsetMin = Vector2.zero; // Left = 0
                containerRect.offsetMax = Vector2.zero; // Right = 0
                chartContainer.SetActive(folderIndex == 0); // Only first folder visible initially
                
                // Add ContentSizeFitter to auto-size based on children
                var containerFitter = chartContainer.AddComponent<UnityEngine.UI.ContentSizeFitter>();
                containerFitter.horizontalFit = UnityEngine.UI.ContentSizeFitter.FitMode.Unconstrained;
                containerFitter.verticalFit = UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;
                
                var containerGrid = chartContainer.AddComponent<UnityEngine.UI.GridLayoutGroup>();
                containerGrid.padding = new RectOffset(20, 20, 20, 20);
                containerGrid.cellSize = new Vector2(390, 280); // 390x260 chart + 20 for title
                containerGrid.spacing = new Vector2(20, 20);
                containerGrid.constraint = UnityEngine.UI.GridLayoutGroup.Constraint.Flexible;
                containerGrid.childAlignment = TextAnchor.UpperLeft;
                
                // Create charts for this folder
                foreach (var profile in profiles)
                {
                    CreateUGUIChart(chartContainer.transform, profile);
                }
                
                // Register with controller (serialized, events added at runtime in Start())
                controller.AddFolderEntry(folderName, button, chartContainer);
                
                folderIndex++;
            }
            
            // Find PanelSettings
            var panelSettingsGuids = AssetDatabase.FindAssets("t:PanelSettings");
            PanelSettings panelSettings = null;
            foreach (var guid in panelSettingsGuids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.Contains("EasyChart"))
                {
                    panelSettings = AssetDatabase.LoadAssetAtPath<PanelSettings>(path);
                    if (panelSettings != null) break;
                }
            }
            
            // Set panel settings on all UGUIChartBridge components
            if (panelSettings != null)
            {
                var bridges = canvasGO.GetComponentsInChildren<UGUIChartBridge>(true);
                foreach (var bridge in bridges)
                {
                    var so = new SerializedObject(bridge);
                    var prop = so.FindProperty("_panelSettingsAsset");
                    if (prop != null)
                    {
                        prop.objectReferenceValue = panelSettings;
                        so.ApplyModifiedPropertiesWithoutUndo();
                    }
                }
            }
            
            // Save scene (scenePath already defined at the beginning)
            var sceneDir = Path.GetDirectoryName(scenePath);
            if (!Directory.Exists(sceneDir))
            {
                Directory.CreateDirectory(sceneDir);
            }
            
            EditorSceneManager.SaveScene(scene, scenePath);
            AssetDatabase.Refresh();
            
            EditorUtility.DisplayDialog("Success", $"UGUI Preview scene created at:\n{scenePath}", "OK");
        }
        
        private void CreateUGUIChart(Transform parent, ChartProfile profile)
        {
            // Load title background texture
            var titleBgTexture = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/EasyChart/Textures/Title.png");
            
            // Parent container (390x280)
            var containerGO = new GameObject(profile.name);
            containerGO.transform.SetParent(parent, false);
            var containerRect = containerGO.AddComponent<RectTransform>();
            containerRect.sizeDelta = new Vector2(390, 280);
            // Transparent Image for layout purposes
            var containerImage = containerGO.AddComponent<UnityEngine.UI.Image>();
            containerImage.color = new Color(0, 0, 0, 0); // Fully transparent
            
            // Title bar at top (390x20)
            var titleGO = new GameObject("Title");
            titleGO.transform.SetParent(containerGO.transform, false);
            var titleRect = titleGO.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0, 1);
            titleRect.anchorMax = new Vector2(1, 1);
            titleRect.pivot = new Vector2(0.5f, 1);
            titleRect.anchoredPosition = Vector2.zero;
            titleRect.sizeDelta = new Vector2(0, 20);
            
            // Title background image
            var titleBgImage = titleGO.AddComponent<UnityEngine.UI.Image>();
            if (titleBgTexture != null)
            {
                titleBgImage.sprite = titleBgTexture;
                titleBgImage.type = UnityEngine.UI.Image.Type.Sliced;
            }
            else
            {
                titleBgImage.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
            }
            
            // Title text
            var titleTextGO = new GameObject("Text");
            titleTextGO.transform.SetParent(titleGO.transform, false);
            var titleTextRect = titleTextGO.AddComponent<RectTransform>();
            titleTextRect.anchorMin = Vector2.zero;
            titleTextRect.anchorMax = Vector2.one;
            titleTextRect.offsetMin = new Vector2(5, 0);
            titleTextRect.offsetMax = new Vector2(-5, 0);
            var titleText = titleTextGO.AddComponent<UnityEngine.UI.Text>();
            titleText.text = profile.name;
            titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            titleText.fontSize = 12;
            titleText.color = Color.white;
            titleText.alignment = TextAnchor.MiddleCenter;
            
            // Chart area (390x260, below title)
            var chartGO = new GameObject("Chart");
            chartGO.transform.SetParent(containerGO.transform, false);
            var chartRect = chartGO.AddComponent<RectTransform>();
            chartRect.anchorMin = new Vector2(0, 0);
            chartRect.anchorMax = new Vector2(1, 1);
            chartRect.offsetMin = Vector2.zero;
            chartRect.offsetMax = new Vector2(0, -20); // Leave 20px for title
            
            // Add UGUIChartBridge to chart area
            var bridge = chartGO.AddComponent<UGUIChartBridge>();
            var so = new SerializedObject(bridge);
            var profileProp = so.FindProperty("_profile");
            if (profileProp != null)
            {
                profileProp.objectReferenceValue = profile;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }
        
        private GameObject CreateScrollView(Transform parent, string name, bool addScrollbar = true)
        {
            var scrollGO = new GameObject(name);
            scrollGO.transform.SetParent(parent, false);
            var scrollRectTransform = scrollGO.AddComponent<RectTransform>();
            scrollRectTransform.anchorMin = Vector2.zero;
            scrollRectTransform.anchorMax = Vector2.one;
            scrollRectTransform.offsetMin = Vector2.zero;
            scrollRectTransform.offsetMax = Vector2.zero;
            var scroll = scrollGO.AddComponent<UnityEngine.UI.ScrollRect>();
            
            // Viewport - leave space for scrollbar on right
            var viewport = new GameObject("Viewport");
            viewport.transform.SetParent(scrollGO.transform, false);
            var viewportRect = viewport.AddComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = Vector2.zero;
            viewportRect.offsetMax = addScrollbar ? new Vector2(-15, 0) : Vector2.zero; // Leave space for scrollbar
            var viewportMask = viewport.AddComponent<UnityEngine.UI.Mask>();
            viewportMask.showMaskGraphic = false;
            var viewportImage = viewport.AddComponent<UnityEngine.UI.Image>();
            viewportImage.color = new Color(0, 0, 0, 0.01f); // Nearly transparent but raycast target
            
            // Content
            var content = new GameObject("Content");
            content.transform.SetParent(viewport.transform, false);
            var contentRect = content.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot = new Vector2(0.5f, 1);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = new Vector2(0, 0);
            var contentFitter = content.AddComponent<UnityEngine.UI.ContentSizeFitter>();
            contentFitter.verticalFit = UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;
            
            scroll.viewport = viewportRect;
            scroll.content = contentRect;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.scrollSensitivity = 30f;
            scroll.movementType = UnityEngine.UI.ScrollRect.MovementType.Clamped;
            
            // Create vertical scrollbar
            if (addScrollbar)
            {
                var scrollbarGO = new GameObject("Scrollbar Vertical");
                scrollbarGO.transform.SetParent(scrollGO.transform, false);
                var scrollbarRect = scrollbarGO.AddComponent<RectTransform>();
                scrollbarRect.anchorMin = new Vector2(1, 0);
                scrollbarRect.anchorMax = new Vector2(1, 1);
                scrollbarRect.pivot = new Vector2(1, 0.5f);
                scrollbarRect.anchoredPosition = Vector2.zero;
                scrollbarRect.sizeDelta = new Vector2(15, 0);
                var scrollbarImage = scrollbarGO.AddComponent<UnityEngine.UI.Image>();
                scrollbarImage.color = new Color(0.1f, 0.1f, 0.1f, 0.5f);
                var scrollbar = scrollbarGO.AddComponent<UnityEngine.UI.Scrollbar>();
                scrollbar.direction = UnityEngine.UI.Scrollbar.Direction.BottomToTop;
                
                // Sliding area
                var slidingArea = new GameObject("Sliding Area");
                slidingArea.transform.SetParent(scrollbarGO.transform, false);
                var slidingRect = slidingArea.AddComponent<RectTransform>();
                slidingRect.anchorMin = Vector2.zero;
                slidingRect.anchorMax = Vector2.one;
                slidingRect.offsetMin = new Vector2(2, 2);
                slidingRect.offsetMax = new Vector2(-2, -2);
                
                // Handle
                var handle = new GameObject("Handle");
                handle.transform.SetParent(slidingArea.transform, false);
                var handleRect = handle.AddComponent<RectTransform>();
                handleRect.anchorMin = Vector2.zero;
                handleRect.anchorMax = Vector2.one;
                handleRect.offsetMin = Vector2.zero;
                handleRect.offsetMax = Vector2.zero;
                var handleImage = handle.AddComponent<UnityEngine.UI.Image>();
                handleImage.color = new Color(0.5f, 0.5f, 0.5f, 1f);
                
                scrollbar.handleRect = handleRect;
                scrollbar.targetGraphic = handleImage;
                scroll.verticalScrollbar = scrollbar;
                scroll.verticalScrollbarVisibility = UnityEngine.UI.ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;
            }
            
            return scrollGO;
        }
        
        private void BuildUIToolkitPreviewScene()
        {
            if (string.IsNullOrEmpty(_selectedLibraryPath))
            {
                EditorUtility.DisplayDialog("Error", "Please select a library first.", "OK");
                return;
            }
            
            var sceneName = GetSceneName();
            var scenePath = $"Assets/EasyChart/Demo/Scenes/{sceneName}_UIToolkit.unity";
            var uxmlPath = $"Assets/EasyChart/Demo/UIToolkit/{sceneName}.uxml";
            var ussPath = $"Assets/EasyChart/Demo/UIToolkit/{sceneName}.uss";
            
            // Check if scene already exists
            if (File.Exists(scenePath))
            {
                if (!EditorUtility.DisplayDialog("Overwrite Scene", 
                    $"Scene already exists:\n{scenePath}\n\nDo you want to overwrite it?", 
                    "Overwrite", "Cancel"))
                {
                    return;
                }
            }
            
            // Create new scene
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            
            // Get folders and profiles
            var folders = GetChartFolders(_selectedLibraryPath);
            
            // Find PanelSettings
            var panelSettingsGuids = AssetDatabase.FindAssets("t:PanelSettings");
            PanelSettings panelSettings = null;
            foreach (var guid in panelSettingsGuids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.Contains("EasyChart"))
                {
                    panelSettings = AssetDatabase.LoadAssetAtPath<PanelSettings>(path);
                    if (panelSettings != null) break;
                }
            }
            
            // Create UIDocument
            var uiDocGO = new GameObject("UIDocument");
            var uiDoc = uiDocGO.AddComponent<UIDocument>();
            if (panelSettings != null)
            {
                uiDoc.panelSettings = panelSettings;
            }
            
            var uxmlDir = Path.GetDirectoryName(uxmlPath);
            if (!Directory.Exists(uxmlDir))
            {
                Directory.CreateDirectory(uxmlDir);
            }
            
            // Generate UXML content
            var uxmlContent = GenerateUIToolkitUXML(folders);
            File.WriteAllText(uxmlPath, uxmlContent);
            
            // Generate USS content
            var ussContent = GenerateUIToolkitUSS();
            File.WriteAllText(ussPath, ussContent);
            
            AssetDatabase.Refresh();
            
            // Load and assign the UXML
            var visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(uxmlPath);
            if (visualTreeAsset != null)
            {
                uiDoc.visualTreeAsset = visualTreeAsset;
            }
            
            // Add UIToolkitExampleController for runtime button handling
            uiDocGO.AddComponent<EasyChart.UIToolkitExampleController>();
            
            // Save scene (scenePath already defined at the beginning)
            var sceneDir = Path.GetDirectoryName(scenePath);
            if (!Directory.Exists(sceneDir))
            {
                Directory.CreateDirectory(sceneDir);
            }
            
            EditorSceneManager.SaveScene(scene, scenePath);
            AssetDatabase.Refresh();
            
            EditorUtility.DisplayDialog("Success", $"UIToolkit Preview scene created at:\n{scenePath}\n\nUXML: {uxmlPath}\nUSS: {ussPath}", "OK");
        }
        
        private string GenerateUIToolkitUXML(List<string> folders)
        {
            var sceneName = GetSceneName();
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("<ui:UXML xmlns:ui=\"UnityEngine.UIElements\" xmlns:ec=\"EasyChart\">");
            sb.AppendLine($"    <Style src=\"{sceneName}.uss\" />");
            sb.AppendLine("    <ui:VisualElement class=\"main-container\">");
            sb.AppendLine("        <ui:VisualElement class=\"sidebar\">");
            sb.AppendLine("            <ui:ScrollView class=\"sidebar-scroll\" mode=\"Vertical\">");
            
            foreach (var folder in folders)
            {
                var folderName = Path.GetFileName(folder);
                sb.AppendLine($"                <ui:Button text=\"{folderName}\" class=\"sidebar-button\" name=\"btn-{folderName}\" />");
            }
            
            sb.AppendLine("            </ui:ScrollView>");
            sb.AppendLine("        </ui:VisualElement>");
            sb.AppendLine("        <ui:VisualElement class=\"content-area\">");
            sb.AppendLine("            <ui:ScrollView class=\"content-scroll\" mode=\"Vertical\">");
            
            int folderIndex = 0;
            foreach (var folder in folders)
            {
                var folderName = Path.GetFileName(folder);
                var profiles = GetChartProfiles(folder);
                var displayStyle = folderIndex == 0 ? "flex" : "none";
                
                sb.AppendLine($"                <ui:VisualElement name=\"charts-{folderName}\" class=\"charts-container\" style=\"display: {displayStyle};\">");
                
                foreach (var profile in profiles)
                {
                    var profileGuid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(profile));
                    sb.AppendLine($"                    <ui:VisualElement class=\"chart-wrapper\">");
                    sb.AppendLine($"                        <ui:Label text=\"{profile.name}\" class=\"chart-title\" />");
                    sb.AppendLine($"                        <ec:ChartElement profile-guid=\"{profileGuid}\" ignore-profile-size=\"true\" class=\"chart-element\" />");
                    sb.AppendLine($"                    </ui:VisualElement>");
                }
                
                sb.AppendLine("                </ui:VisualElement>");
                folderIndex++;
            }
            
            sb.AppendLine("            </ui:ScrollView>");
            sb.AppendLine("        </ui:VisualElement>");
            sb.AppendLine("    </ui:VisualElement>");
            sb.AppendLine("</ui:UXML>");
            
            return sb.ToString();
        }
        
        private string GenerateUIToolkitUSS()
        {
            return @".main-container {
    flex-direction: row;
    flex-grow: 1;
    overflow: hidden;
}

.sidebar {
    width: 200px;
    min-width: 200px;
    max-width: 200px;
    background-color: rgba(40, 40, 40, 0.9);
    overflow: hidden;
}

.sidebar-scroll {
    flex-grow: 1;
}

.sidebar-scroll .unity-scroll-view__content-container {
    flex-grow: 1;
}

/* Slim vertical scrollbar */
.unity-scroller--vertical {
    width: 8px;
}

.unity-scroller--vertical > .unity-slider {
    width: 8px;
    min-width: 8px;
}

.unity-scroller--vertical .unity-base-slider__dragger {
    width: 6px;
    border-radius: 3px;
    background-color: rgba(150, 150, 150, 0.5);
}

.unity-scroller--vertical .unity-base-slider__tracker {
    background-color: transparent;
}

/* Hide scrollbar arrow buttons */
.unity-scroller--vertical .unity-repeat-button {
    display: none;
}

/* Hide horizontal scroller */
.unity-scroller--horizontal {
    display: none;
}

.sidebar-button {
    height: 40px;
    margin: 5px 10px;
    background-color: rgb(60, 60, 60);
    border-width: 0;
    border-radius: 5px;
    color: white;
    -unity-text-align: middle-left;
    padding-left: 15px;
}

.sidebar-button:hover {
    background-color: rgb(80, 80, 80);
}

.sidebar-button:active {
    background-color: rgb(100, 100, 100);
}

.content-area {
    flex-grow: 1;
    background-color: rgba(50, 50, 50, 0.85);
    overflow: hidden;
}

.content-scroll {
    flex-grow: 1;
    padding: 20px;
}

.charts-container {
    flex-direction: row;
    flex-wrap: wrap;
    align-items: flex-start;
    align-content: flex-start;
}

.chart-wrapper {
    width: 390px;
    min-width: 390px;
    max-width: 390px;
    height: 284px;
    min-height: 284px;
    max-height: 284px;
    margin: 10px;
    background-color: transparent;
    border-radius: 5px;
    flex-shrink: 0;
    flex-grow: 0;
    overflow: hidden;
}

.chart-title {
    width: 100%;
    height: 24px;
    min-height: 24px;
    max-height: 24px;
    color: white;
    -unity-text-align: middle-center;
    font-size: 12px;
    -unity-font-style: bold;
    background-image: url('project://database/Assets/EasyChart/Textures/Title.png');
    -unity-background-scale-mode: stretch-to-fill;
    border-top-left-radius: 5px;
    border-top-right-radius: 5px;
    flex-shrink: 0;
}

.chart-element {
    width: 100%;
    height: 260px;
    min-height: 260px;
    max-height: 260px;
    margin-top: 0;
    flex-shrink: 0;
    flex-grow: 0;
    overflow: hidden;
}
";
        }
    }
    
    // ExampleSceneController is now in Runtime assembly: EasyChart.ExampleSceneController
}
