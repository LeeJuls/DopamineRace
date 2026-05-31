using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using EasyChart;
using EasyChart.Internal;

namespace EasyChart.Editor
{
    public partial class EasyChartLibraryWindow : EditorWindow
    {
        private const string RootLibraryName = "<Root>";
        private const string SelectedLibraryPrefsKey = "EasyChart.LibraryWindow.SelectedLibrary";
        private const string PopupOnlyClickPrefsKey = "EasyChart.LibraryWindow.PopupOnlyClick";

        /// <summary>
        /// External callers (e.g. inspector helpers) can invoke this to show the Pro popup
        /// anchored to a given world-space rect.
        /// </summary>
        public static Action<Rect> RequestShowProPopup;

        private const string LibraryThemeAssetFileName = "LibraryTheme.asset";

        [MenuItem("EasyChart/Library Editor")]
        public static void OpenWindow()
        {
            var wnd = GetWindow<EasyChartLibraryWindow>();
            var icon = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/EasyChart/Textures/Icon/UEC.png");
            wnd.titleContent = new GUIContent("EasyChart", icon);
            wnd.minSize = new Vector2(900, 600);
        }

        private string GetSelectedLibraryName()
        {
            string v = EditorPrefs.GetString(SelectedLibraryPrefsKey, RootLibraryName);
            if (string.IsNullOrEmpty(v)) v = RootLibraryName;
            return v;
        }

        private void SetSelectedLibraryName(string libraryName)
        {
            if (string.IsNullOrEmpty(libraryName)) libraryName = RootLibraryName;
            EditorPrefs.SetString(SelectedLibraryPrefsKey, libraryName);
        }

        private string GetActiveProfileRootPath()
        {
            string lib = GetSelectedLibraryName();
            return string.Equals(lib, RootLibraryName, StringComparison.OrdinalIgnoreCase) ? ROOT_PATH : $"{ROOT_PATH}/{lib}";
        }

        private string GetActiveUxmlRootPath()
        {
            string lib = GetSelectedLibraryName();
            return string.Equals(lib, RootLibraryName, StringComparison.OrdinalIgnoreCase) ? UXML_ROOT_PATH : $"{UXML_ROOT_PATH}/{lib}";
        }

        private string GetActiveLibraryThemeAssetPath()
        {
            return $"{GetActiveProfileRootPath()}/{LibraryThemeAssetFileName}";
        }

        private ChartTheme GetActiveLibraryThemeAsset(bool createIfMissing)
        {
            string path = GetActiveLibraryThemeAssetPath();
            var theme = AssetDatabase.LoadAssetAtPath<ChartTheme>(path);
            if (theme != null) return theme;
            if (!createIfMissing) return null;

            EnsureActiveLibraryFolders();
            theme = ScriptableObject.CreateInstance<ChartTheme>();
            AssetDatabase.CreateAsset(theme, path);
            AssetDatabase.SaveAssets();
            return theme;
        }

        private void ApplyActiveLibraryThemeToPreview(bool force)
        {
            if (_previewChart == null) return;

            var theme = GetActiveLibraryThemeAsset(createIfMissing: false);
            if (force)
            {
                _previewChart.Theme = null;
            }
            _previewChart.Theme = theme;
        }

        private void OpenThemeEditorForActiveLibrary()
        {
            var theme = GetActiveLibraryThemeAsset(createIfMissing: true);
            LibraryThemeEditorWindow.Open(this, GetSelectedLibraryName(), theme);
        }

        private void OpenSettingsWindow()
        {
            EasyChartSettingsWindow.Open();
        }

        private sealed class LibraryThemeEditorWindow : EditorWindow
        {
            private EasyChartLibraryWindow _owner;
            private string _libraryName;
            private ChartTheme _theme;
            private ScrollView _scrollView;
            private VisualElement _themeInspectorRoot;

            public static void Open(EasyChartLibraryWindow owner, string libraryName, ChartTheme theme)
            {
                var wnd = GetWindow<LibraryThemeEditorWindow>();
                wnd._owner = owner;
                wnd._libraryName = libraryName;
                wnd._theme = theme;
                wnd.titleContent = new GUIContent("Theme");
                wnd.minSize = new Vector2(320, 400);
                wnd.RebuildUI();
                wnd.Show();
            }

            private void OnEnable()
            {
                RebuildUI();
            }

            private void RebuildUI()
            {
                if (rootVisualElement == null) return;
                rootVisualElement.Clear();

                rootVisualElement.style.paddingTop = 8;
                rootVisualElement.style.paddingLeft = 8;
                rootVisualElement.style.paddingRight = 8;
                rootVisualElement.style.paddingBottom = 8;

                // Header
                var header = new Label(_libraryName != null ? $"Library: {_libraryName}" : string.Empty);
                header.style.unityFontStyleAndWeight = FontStyle.Bold;
                header.style.marginBottom = 6;
                rootVisualElement.Add(header);

                // Theme asset picker
                var themeRow = new VisualElement();
                themeRow.style.flexDirection = FlexDirection.Row;
                themeRow.style.alignItems = Align.Center;
                themeRow.style.marginBottom = 8;

                var themeLabel = new Label("Theme Asset");
                themeLabel.style.width = 100;
                themeRow.Add(themeLabel);

                var themeField = new ObjectField { objectType = typeof(ChartTheme), allowSceneObjects = false };
                themeField.style.flexGrow = 1;
                themeField.SetValueWithoutNotify(_theme);
                themeField.RegisterValueChangedCallback(evt =>
                {
                    _theme = evt.newValue as ChartTheme;
                    _owner?.ApplyActiveLibraryThemeToPreview(force: true);
                    _owner?.ScheduleUpdatePreview();
                    RebuildInspector();
                });
                themeRow.Add(themeField);
                rootVisualElement.Add(themeRow);

                // Separator
                var sep = new VisualElement();
                sep.style.height = 1;
                sep.style.backgroundColor = new Color(0.15f, 0.15f, 0.15f, 1f);
                sep.style.marginBottom = 8;
                rootVisualElement.Add(sep);

                // Scrollable inspector area
                _scrollView = new ScrollView(ScrollViewMode.Vertical);
                _scrollView.style.flexGrow = 1;
                rootVisualElement.Add(_scrollView);

                _themeInspectorRoot = new VisualElement();
                _scrollView.Add(_themeInspectorRoot);

                RebuildInspector();
            }

            private void RebuildInspector()
            {
                if (_themeInspectorRoot == null) return;
                _themeInspectorRoot.Clear();
                _themeInspectorRoot.Unbind();

                if (_theme == null)
                {
                    var msg = EditorStyleHelper.CreateWarningBox("No ChartTheme assigned. Select or create a theme asset.");
                    _themeInspectorRoot.Add(msg);
                    return;
                }

                var so = new SerializedObject(_theme);

                _themeInspectorRoot.Add(BuildSection(so, "Font", new[]
                {
                    ("primaryFont", "Primary Font", "Main font for chart text (supports SDF FontAsset)"),
                    ("monoFont",    "Mono Font",    "Monospace font for numeric values"),
                    ("fontScale",   "Font Scale",   "Global font size multiplier"),
                }));

                _themeInspectorRoot.Add(BuildSection(so, "Font Size", new[]
                {
                    ("titleFontSize",       "Title",        "Override title font size (-1 = use USS default)"),
                    ("subtitleFontSize",    "Subtitle",     "Override subtitle font size (-1 = use USS default)"),
                    ("axisFontSize",        "Axis",         "Override axis label font size (-1 = use USS default)"),
                    ("legendFontSize",      "Legend",       "Override legend font size (-1 = use USS default)"),
                    ("tooltipFontSize",     "Tooltip",      "Override tooltip font size (-1 = use USS default)"),
                    ("seriesLabelFontSize", "Series Label", "Override series label font size (-1 = use USS default)"),
                }));

                _themeInspectorRoot.Add(BuildSection(so, "Default Template", new[]
                {
                    ("baseProfile", "Base Profile", "Template profile used when creating new charts. If empty, clones the first profile in current library."),
                }));

                _themeInspectorRoot.Bind(so);

                // Propagate changes to preview
                _themeInspectorRoot.RegisterCallback<SerializedPropertyChangeEvent>(_ =>
                {
                    _owner?.ApplyActiveLibraryThemeToPreview(force: true);
                    _owner?.ScheduleUpdatePreview();
                });
            }

            private static VisualElement BuildSection(SerializedObject so, string title, (string prop, string label, string tooltip)[] fields)
            {
                // Create foldout with box styling matching Settings window style
                var foldout = new Foldout { text = title };
                foldout.bindingPath = string.Empty;
                foldout.SetValueWithoutNotify(true);

                var borderColor = new Color(0.1f, 0.1f, 0.1f);
                var backgroundColor = new Color(0.18f, 0.18f, 0.18f);

                foldout.style.borderTopWidth = 1;
                foldout.style.borderBottomWidth = 1;
                foldout.style.borderLeftWidth = 1;
                foldout.style.borderRightWidth = 1;
                foldout.style.borderTopColor = borderColor;
                foldout.style.borderBottomColor = borderColor;
                foldout.style.borderLeftColor = borderColor;
                foldout.style.borderRightColor = borderColor;
                foldout.style.backgroundColor = backgroundColor;
                foldout.style.marginTop = 6;
                foldout.style.marginBottom = 6;
                foldout.style.paddingLeft = 6;
                foldout.style.paddingRight = 6;
                foldout.style.paddingTop = 4;
                foldout.style.paddingBottom = 6;
                foldout.style.borderTopLeftRadius = 3;
                foldout.style.borderTopRightRadius = 3;
                foldout.style.borderBottomLeftRadius = 3;
                foldout.style.borderBottomRightRadius = 3;

                foreach (var (propName, labelText, tooltipText) in fields)
                {
                    var prop = so.FindProperty(propName);
                    if (prop == null) continue;
                    var pf = new PropertyField(prop, labelText) { tooltip = tooltipText };
                    foldout.Add(pf);
                }

                return foldout;
            }
        }

        private List<string> GetLibraryOptions()
        {
            EnsureFolderExists(ROOT_PATH);

            var list = new List<string>();
            list.Add(RootLibraryName);

            string[] subFolders = AssetDatabase.GetSubFolders(ROOT_PATH);
            if (subFolders != null && subFolders.Length > 0)
            {
                for (int i = 0; i < subFolders.Length; i++)
                {
                    string name = Path.GetFileName(subFolders[i]);
                    if (string.IsNullOrEmpty(name)) continue;
                    if (string.Equals(name, UXML_BACKUPS_SUBFOLDER, StringComparison.OrdinalIgnoreCase)) continue;
                    list.Add(name);
                }
            }

            list.Sort((a, b) =>
            {
                if (string.Equals(a, RootLibraryName, StringComparison.OrdinalIgnoreCase)) return -1;
                if (string.Equals(b, RootLibraryName, StringComparison.OrdinalIgnoreCase)) return 1;
                return string.Compare(a, b, StringComparison.OrdinalIgnoreCase);
            });

            return list;
        }

        private void EnsureActiveLibraryFolders()
        {
            EnsureFolderExists(ROOT_PATH);
            EnsureFolderExists(UXML_ROOT_PATH);
            EnsureFolderExists(GetActiveProfileRootPath());
            EnsureFolderExists(GetActiveUxmlRootPath());
        }

        private string GetCreateTargetFolder()
        {
            if (!string.IsNullOrEmpty(_selectedFolderPath) && AssetDatabase.IsValidFolder(_selectedFolderPath))
            {
                string activeRoot = GetActiveProfileRootPath().Replace('\\', '/').TrimEnd('/') + "/";
                string candidate = _selectedFolderPath.Replace('\\', '/').TrimEnd('/') + "/";
                if (candidate.StartsWith(activeRoot, StringComparison.OrdinalIgnoreCase) || string.Equals(candidate, activeRoot, StringComparison.OrdinalIgnoreCase))
                {
                    return _selectedFolderPath;
                }
            }

            return $"{GetActiveProfileRootPath()}/Custom";
        }

        private static void EnsureFolderExists(string folderPath)
        {
            if (AssetDatabase.IsValidFolder(folderPath)) return;

            string normalized = folderPath.Replace("\\", "/");
            if (!normalized.StartsWith("Assets/")) return;

            string[] parts = normalized.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }
                current = next;
            }
        }

        private class TextPromptWindow : EditorWindow
        {
            private Action<string> _onOk;
            private string _value;
            private string _title;

            public static void Show(string title, string initialValue, Action<string> onOk)
            {
                var wnd = CreateInstance<TextPromptWindow>();
                wnd._title = title;
                wnd._value = initialValue ?? string.Empty;
                wnd._onOk = onOk;
                wnd.titleContent = new GUIContent(title);
                wnd.minSize = new Vector2(300, 70);
                wnd.maxSize = wnd.minSize;
                wnd.ShowUtility();
            }

            public void CreateGUI()
            {
                var root = rootVisualElement;
                root.style.flexDirection = FlexDirection.Column;
                root.style.paddingLeft = 8;
                root.style.paddingRight = 8;
                root.style.paddingTop = 8;
                root.style.paddingBottom = 8;

                var field = new TextField(_title) { value = _value };
                field.style.flexGrow = 1;
                root.Add(field);

                var row = new VisualElement();
                row.style.flexDirection = FlexDirection.Row;
                row.style.justifyContent = Justify.FlexEnd;
                row.style.marginTop = 8;

                System.Action confirmAction = () =>
                {
                    _onOk?.Invoke(field.value);
                    Close();
                };

                var okBtn = new Button(confirmAction) { text = "OK" };
                var cancelBtn = new Button(Close) { text = "Cancel" };

                okBtn.style.marginLeft = 6;
                cancelBtn.style.marginLeft = 6;
                row.Add(cancelBtn);
                row.Add(okBtn);
                root.Add(row);

                // Support Enter key to confirm (use TrickleDown to catch event early)
                field.RegisterCallback<KeyDownEvent>(evt =>
                {
                    if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter)
                    {
                        evt.StopImmediatePropagation();
                        evt.PreventDefault();
                        confirmAction();
                    }
                    else if (evt.keyCode == KeyCode.Escape)
                    {
                        evt.StopImmediatePropagation();
                        Close();
                    }
                }, TrickleDown.TrickleDown);

                field.Q("unity-text-input")?.Focus();
            }
        }

        // UI References
        private TreeView _folderTree;
        private VisualElement _previewContainer;
        private ScrollView _inspectorContainer;
        private ScrollView _seriesContainer;
        private ChartElement _previewChart;

        private VisualElement _jsonExampleContainer;
        private ScrollView _jsonExampleScroll;
        private TextField _jsonExampleField;
        private Image _jsonCopyButton;
        private PopupField<string> _jsonModeDropdown;
        private Image _jsonApiToggleButton;
        private Image _jsonApplyToChartButton;

        private bool _jsonUseApiEnvelope;

        private ChartJsonExampleMode _jsonExampleMode = ChartJsonExampleMode.Lite_Index;
        private string _jsonChartId = "main-chart";
        
        // Data
        private List<TreeViewItemData<string>> _treeRoots;
        private ChartProfile _selectedProfile;
        private SerializedObject _serializedProfile;
        private SerializedProperty _seriesProperty;

        private string _selectedFolderPath;

        private const string FolderIconPath = "Assets/EasyChart/Textures/Icon/folder.png";
        private const string ProfileIconPath = "Assets/EasyChart/Textures/Icon/profile.png";
        private const string AddChartIconPath = "Assets/EasyChart/Textures/Icon/AddChart.png";
        private const string AddFolderIconPath = "Assets/EasyChart/Textures/Icon/AddFolder.png";
        private const string RefreshIconPath = "Assets/EasyChart/Textures/Icon/Refresh.png";
        private const string SaveIconPath = "Assets/EasyChart/Textures/Icon/Save.png";
        private const string MenuIconPath = "Assets/EasyChart/Textures/Icon/menu.png";
        private const string CopyIconPath = "Assets/EasyChart/Textures/Icon/Copy.png";
        private const string CloneIconPath = "Assets/EasyChart/Textures/Icon/clone.png";
        private const string ApplyToChartIconPath = "Assets/EasyChart/Textures/Icon/ApplyToChart.png";
        private const string ThemeIconPath = "Assets/EasyChart/Textures/Icon/Theme.png";
        private const string FeedIconPath = "Assets/EasyChart/Textures/Icon/Feed.png";
        private const string DataIconPath = "Assets/EasyChart/Textures/Icon/Data.png";
        private const string HelpIconPath = "Assets/EasyChart/Textures/Icon/help.png";
        private const string ApiOnIconPath = "Assets/EasyChart/Textures/Icon/ApiOn.png";
        private const string ApiOffIconPath = "Assets/EasyChart/Textures/Icon/ApiOff.png";
        private const string ProIconPath = "Assets/EasyChart/Textures/Icon/Pro.png";
        private const string SupportIconPath = "Assets/EasyChart/Textures/Icon/support.png";
        private const string PaletteIconPath = "Assets/EasyChart/Textures/Icon/ColorPalette.png";
        private const string SettingsIconPath = "Assets/EasyChart/Textures/Icon/setting.png";
        private Texture2D _folderIcon;
        private Texture2D _profileIcon;
        private Texture2D _addChartIcon;
        private Texture2D _addFolderIcon;
        private Texture2D _refreshIcon;
        private Texture2D _saveIcon;
        private Texture2D _menuIcon;
        private Texture2D _copyIcon;
        private Texture2D _cloneIcon;
        private Texture2D _applyToChartIcon;
        private Texture2D _themeIcon;
        private Texture2D _feedIcon;
        private Texture2D _dataIcon;
        private Texture2D _helpIcon;
        private Texture2D _apiOnIcon;
        private Texture2D _proIcon;
        private Texture2D _supportIcon;
        private Texture2D _settingsIcon;

        private Action _onSave;
        private Texture2D _apiOffIcon;

        private const string DragDataKey = "EasyChartLibraryWindow.DragChartAssetPath";
        private const float DragStartThreshold = 6f;

        private string _dragCandidateAssetPath;
        private Vector2 _dragStartPos;
        private int _dragPointerId = -1;
        private bool _dragInProgress;

        private string _inlineRenamePath;
        private string _inlineRenamePendingFocusPath;
        private TextField _inlineRenameField;

        private bool _previewUpdateScheduled;
        private bool _isUpdatingPreview;
        private bool _seriesRefreshScheduled;
        
        // Series sync feature
        private bool _seriesSyncEnabled;
        private bool _isSyncingSeriesProperties;
        private Toggle _seriesSyncToggle;
        private Label _seriesSyncLabel;
        private double _seriesUIRebuildEndTime = -1;
        private int _seriesBindInitEventCount;

        // Color palette feature
        private Texture2D _paletteIcon;
        private VisualElement _paletteBtn;
        private List<SeriesColorPalette> _colorPalettes;

        // Paths
        private const string ROOT_PATH = "Assets/EasyChart/Library";
        private readonly Dictionary<string, bool> _seriesFoldoutState = new Dictionary<string, bool>();

        private static VisualElement CreateGroupBox()
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
            box.style.marginTop = 6;
            box.style.marginBottom = 6;
            box.style.paddingLeft = 6;
            box.style.paddingRight = 6;
            box.style.paddingTop = 4;
            box.style.paddingBottom = 6;
            box.style.borderTopLeftRadius = 3;
            box.style.borderTopRightRadius = 3;
            box.style.borderBottomLeftRadius = 3;
            box.style.borderBottomRightRadius = 3;

            return box;
        }

        public void CreateGUI()
        {
            var windowIcon = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/EasyChart/Textures/Icon/UEC.png");
            titleContent = new GUIContent("Unity Easy Chart", windowIcon);

            var root = rootVisualElement;
            root.style.flexDirection = FlexDirection.Column;
            root.style.height = Length.Percent(100); // Ensure root fills window

#if UNITY_6000_0_OR_NEWER
            // Unity 6 adds an overlay toolbar container above rootVisualElement
            // Hide it by setting negative margin and adjusting height
            root.style.marginTop = -26;
            root.style.paddingTop = 0;
#endif

            root.RegisterCallback<PointerDownEvent>(OnRootPointerDownForInlineRename, TrickleDown.TrickleDown);

            // Ctrl+S shortcut for Save
            root.RegisterCallback<KeyDownEvent>(evt =>
            {
                if (evt.keyCode == KeyCode.S && evt.ctrlKey)
                {
                    evt.StopPropagation();
                    evt.PreventDefault();
                    _onSave?.Invoke();
                }
            });

            EnsureSharedIconsLoaded();

            EnsureActiveLibraryFolders();

            var globalToolbar = new VisualElement();
            globalToolbar.style.flexDirection = FlexDirection.Row;
            globalToolbar.style.alignItems = Align.Center;
            globalToolbar.style.paddingTop = 6;
            globalToolbar.style.paddingBottom = 6;
            globalToolbar.style.paddingLeft = 6;
            globalToolbar.style.paddingRight = 12;
            globalToolbar.style.backgroundColor = new Color(.15f,.15f, .15f);
            globalToolbar.style.borderBottomWidth = 1;
            globalToolbar.style.borderBottomColor = new Color(0.08f, 0.08f, 0.08f);
            globalToolbar.style.borderTopWidth = 1;
            globalToolbar.style.borderTopColor = new Color(0.08f, 0.08f, 0.08f);
            globalToolbar.style.flexShrink = 0;
            root.Add(globalToolbar);

            var mainRow = new VisualElement();
            mainRow.style.flexDirection = FlexDirection.Row;
            mainRow.style.flexGrow = 1;

            // --- Left Panel: Folder Tree ---
            var leftPanel = new VisualElement();
            leftPanel.style.width = 320;
            leftPanel.style.flexDirection = FlexDirection.Column;
            leftPanel.style.paddingBottom = 6;
            leftPanel.style.borderRightWidth = 1;
            leftPanel.style.borderRightColor = new Color(0.15f, 0.15f, 0.15f);
            leftPanel.style.backgroundColor = new Color(0.22f, 0.22f, 0.22f);
            mainRow.Add(leftPanel);

            var libraryHeaderRow = new VisualElement();
            libraryHeaderRow.style.flexDirection = FlexDirection.Row;
            libraryHeaderRow.style.alignItems = Align.Center;
            libraryHeaderRow.style.paddingLeft = 10;
            libraryHeaderRow.style.paddingTop = 5;
            libraryHeaderRow.style.paddingBottom = 5;
            libraryHeaderRow.style.paddingRight = 12;
            libraryHeaderRow.style.backgroundColor = new Color(0.25f, 0.25f, 0.25f);
            leftPanel.Add(libraryHeaderRow);

            var libraryHeader = new Label("Library");
            libraryHeader.style.unityFontStyleAndWeight = FontStyle.Bold;
            libraryHeader.style.flexGrow = 0;
            libraryHeaderRow.Add(libraryHeader);

            var currentLibraryLabel = new Label($"({GetSelectedLibraryName()})");
            currentLibraryLabel.style.fontSize = 10;
            currentLibraryLabel.style.color = new Color(0.7f, 0.7f, 0.7f);
            currentLibraryLabel.style.marginLeft = 6;
            currentLibraryLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            libraryHeaderRow.Add(currentLibraryLabel);

            var libraryOptions = GetLibraryOptions();
            string currentLibrary = GetSelectedLibraryName();
            int currentIndex = libraryOptions.IndexOf(currentLibrary);
            if (currentIndex < 0) currentIndex = 0;
            var libraryPopup = new PopupField<string>(libraryOptions, currentIndex);
            if (libraryPopup.labelElement != null) libraryPopup.labelElement.style.display = DisplayStyle.None;
            libraryPopup.style.width = 140;
            libraryPopup.style.maxWidth = 160;
            libraryPopup.style.flexGrow = 0;
            libraryPopup.style.marginLeft = 6;

            void RefreshLibraryPopupChoices()
            {
                var options = GetLibraryOptions();
                libraryPopup.choices = options;

                string selected = GetSelectedLibraryName();
                if (!options.Contains(selected))
                {
                    selected = (options != null && options.Count > 0) ? options[0] : RootLibraryName;
                    SetSelectedLibraryName(selected);
                }

                libraryPopup.SetValueWithoutNotify(selected);
            }

            libraryPopup.RegisterValueChangedCallback(evt =>
            {
                if (evt == null) return;
                if (string.Equals(evt.newValue, GetSelectedLibraryName(), StringComparison.OrdinalIgnoreCase)) return;
                SetSelectedLibraryName(evt.newValue);
                EnsureActiveLibraryFolders();

                _selectedProfile = null;
                _serializedProfile = null;
                _seriesProperty = null;

                _selectedFolderPath = GetActiveProfileRootPath();

                // Update current library label
                currentLibraryLabel.text = $"({evt.newValue})";

                RefreshTree();
                ScheduleRefreshSeriesList();
                ApplyActiveLibraryThemeToPreview(force: true);
                ScheduleUpdatePreview();
            });
            // libraryPopup will be added to globalToolbar later

            var addLibraryBtn = new Button(() =>
            {
                TextPromptWindow.Show("New Library", string.Empty, rawName =>
                {
                    string name = (rawName ?? string.Empty).Trim();
                    if (string.IsNullOrEmpty(name)) return;

                    var invalid = Path.GetInvalidFileNameChars();
                    var sb = new System.Text.StringBuilder(name.Length);
                    for (int i = 0; i < name.Length; i++)
                    {
                        char c = name[i];
                        if (invalid.Contains(c)) continue;
                        sb.Append(c);
                    }

                    name = sb.ToString().Trim();
                    if (string.IsNullOrEmpty(name)) return;
                    if (string.Equals(name, RootLibraryName, StringComparison.OrdinalIgnoreCase)) return;

                    string libRoot = $"{ROOT_PATH}/{name}";
                    if (AssetDatabase.IsValidFolder(libRoot))
                    {
                        EditorUtility.DisplayDialog("EasyChart", $"Library '{name}' already exists.", "OK");
                        return;
                    }

                    EnsureFolderExists(libRoot);
                    EnsureFolderExists($"{UXML_ROOT_PATH}/{name}");

                    SetSelectedLibraryName(name);
                    EnsureActiveLibraryFolders();

                    _selectedProfile = null;
                    _serializedProfile = null;
                    _seriesProperty = null;
                    _selectedFolderPath = GetActiveProfileRootPath();

                    // Update current library label
                    currentLibraryLabel.text = $"({name})";

                    RefreshLibraryPopupChoices();
                    RefreshTree();
                    ScheduleRefreshSeriesList();
                    ScheduleUpdatePreview();
                });
            }) { text = "+" };
            addLibraryBtn.style.width = 22;
            addLibraryBtn.style.height = 18;
            addLibraryBtn.style.marginLeft = 4;
            // addLibraryBtn will be added to globalToolbar later

            var deleteLibraryBtn = new Button(() =>
            {
                string lib = GetSelectedLibraryName();
                if (string.IsNullOrEmpty(lib)) return;
                if (string.Equals(lib, RootLibraryName, StringComparison.OrdinalIgnoreCase)) return;

                bool ok = EditorUtility.DisplayDialog(
                    "Delete Library",
                    $"Delete library '{lib}'? This will delete:\n- {ROOT_PATH}/{lib}\n- {UXML_ROOT_PATH}/{lib}\n\nThis cannot be undone.",
                    "Delete",
                    "Cancel");
                if (!ok) return;

                string libRoot = $"{ROOT_PATH}/{lib}";
                string uxmlRoot = $"{UXML_ROOT_PATH}/{lib}";

                if (AssetDatabase.IsValidFolder(libRoot)) AssetDatabase.DeleteAsset(libRoot);
                if (AssetDatabase.IsValidFolder(uxmlRoot)) AssetDatabase.DeleteAsset(uxmlRoot);

                SetSelectedLibraryName(RootLibraryName);
                EnsureActiveLibraryFolders();

                _selectedProfile = null;
                _serializedProfile = null;
                _seriesProperty = null;
                _selectedFolderPath = GetActiveProfileRootPath();

                // Update current library label
                currentLibraryLabel.text = $"({RootLibraryName})";

                RefreshLibraryPopupChoices();
                RefreshTree();
                ScheduleRefreshSeriesList();
                ScheduleUpdatePreview();
            }) { text = "-" };
            deleteLibraryBtn.style.width = 22;
            deleteLibraryBtn.style.height = 18;
            deleteLibraryBtn.style.marginLeft = 2;
            // deleteLibraryBtn will be added to globalToolbar later

            VisualElement cloneLibraryBtn;
            void CloneCurrentLibrary(string newLibraryName)
            {
                string srcLib = GetSelectedLibraryName();
                if (string.IsNullOrEmpty(srcLib)) return;
                if (string.Equals(srcLib, RootLibraryName, StringComparison.OrdinalIgnoreCase))
                {
                    EditorUtility.DisplayDialog("EasyChart", "Cannot clone the <Root> library.", "OK");
                    return;
                }

                string name = (newLibraryName ?? string.Empty).Trim();
                if (string.IsNullOrEmpty(name)) return;
                if (string.Equals(name, RootLibraryName, StringComparison.OrdinalIgnoreCase)) return;

                var invalid = Path.GetInvalidFileNameChars();
                var sb = new System.Text.StringBuilder(name.Length);
                for (int i = 0; i < name.Length; i++)
                {
                    char c = name[i];
                    if (invalid.Contains(c)) continue;
                    sb.Append(c);
                }

                name = sb.ToString().Trim();
                if (string.IsNullOrEmpty(name)) return;
                if (string.Equals(name, RootLibraryName, StringComparison.OrdinalIgnoreCase)) return;

                string srcProfileRoot = $"{ROOT_PATH}/{srcLib}";
                string srcUxmlRoot = $"{UXML_ROOT_PATH}/{srcLib}";

                string dstProfileRoot = $"{ROOT_PATH}/{name}";
                string dstUxmlRoot = $"{UXML_ROOT_PATH}/{name}";

                if (AssetDatabase.IsValidFolder(dstProfileRoot) || AssetDatabase.IsValidFolder(dstUxmlRoot))
                {
                    EditorUtility.DisplayDialog("EasyChart", $"Library '{name}' already exists.", "OK");
                    return;
                }

                void CopyFolderContents(string srcFolder, string dstFolder)
                {
                    if (string.IsNullOrEmpty(srcFolder)) return;
                    if (string.IsNullOrEmpty(dstFolder)) return;
                    if (!AssetDatabase.IsValidFolder(srcFolder)) return;

                    string srcNorm = srcFolder.Replace('\\', '/').TrimEnd('/');
                    string dstNorm = dstFolder.Replace('\\', '/').TrimEnd('/');
                    EnsureFolderExists(dstNorm);

                    var guids = AssetDatabase.FindAssets(string.Empty, new[] { srcNorm });
                    if (guids == null || guids.Length == 0) return;

                    for (int i = 0; i < guids.Length; i++)
                    {
                        string assetPath = AssetDatabase.GUIDToAssetPath(guids[i]);
                        if (string.IsNullOrEmpty(assetPath)) continue;
                        assetPath = assetPath.Replace('\\', '/');
                        if (AssetDatabase.IsValidFolder(assetPath)) continue;

                        if (!assetPath.StartsWith(srcNorm, StringComparison.OrdinalIgnoreCase)) continue;
                        string rel = assetPath.Substring(srcNorm.Length).TrimStart('/');
                        if (string.IsNullOrEmpty(rel)) continue;

                        string dstPath = $"{dstNorm}/{rel}";
                        string dstDir = Path.GetDirectoryName(dstPath)?.Replace('\\', '/');
                        if (!string.IsNullOrEmpty(dstDir)) EnsureFolderExists(dstDir);

                        bool ok = AssetDatabase.CopyAsset(assetPath, dstPath);
                        if (!ok)
                        {
                            EasyChartLog.Error($"Failed to copy asset: {assetPath} -> {dstPath}");
                        }
                    }
                }

                EnsureFolderExists(dstProfileRoot);
                EnsureFolderExists(dstUxmlRoot);

                CopyFolderContents(srcProfileRoot, dstProfileRoot);
                if (AssetDatabase.IsValidFolder(srcUxmlRoot))
                {
                    CopyFolderContents(srcUxmlRoot, dstUxmlRoot);
                }

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                SetSelectedLibraryName(name);
                EnsureActiveLibraryFolders();

                _selectedProfile = null;
                _serializedProfile = null;
                _seriesProperty = null;
                _selectedFolderPath = GetActiveProfileRootPath();

                currentLibraryLabel.text = $"({name})";

                RefreshLibraryPopupChoices();
                RefreshTree();
                ScheduleRefreshSeriesList();
                ApplyActiveLibraryThemeToPreview(force: true);
                ScheduleUpdatePreview();
            }

            if (_cloneIcon != null)
            {
                cloneLibraryBtn = CreateClickableIconImage(_cloneIcon, "Clone Library", () =>
                {
                    string lib = GetSelectedLibraryName();
                    if (string.IsNullOrEmpty(lib)) return;
                    if (string.Equals(lib, RootLibraryName, StringComparison.OrdinalIgnoreCase))
                    {
                        EditorUtility.DisplayDialog("EasyChart", "Cannot clone the <Root> library.", "OK");
                        return;
                    }
                    TextPromptWindow.Show("Clone Library", $"{lib} Copy", CloneCurrentLibrary);
                });
            }
            else
            {
                cloneLibraryBtn = new Button(() =>
                {
                    string lib = GetSelectedLibraryName();
                    if (string.IsNullOrEmpty(lib)) return;
                    if (string.Equals(lib, RootLibraryName, StringComparison.OrdinalIgnoreCase))
                    {
                        EditorUtility.DisplayDialog("EasyChart", "Cannot clone the <Root> library.", "OK");
                        return;
                    }
                    TextPromptWindow.Show("Clone Library", $"{lib} Copy", CloneCurrentLibrary);
                }) { text = "Clone" };
            }
            cloneLibraryBtn.style.marginLeft = 2;
            // cloneLibraryBtn will be added to globalToolbar later

            var themeBtn = CreateClickableIconImage(_themeIcon, "Theme", OpenThemeEditorForActiveLibrary);
            themeBtn.style.marginLeft = 6;
            // themeBtn will be added to globalToolbar later

            // Tree menu button stays in libraryHeaderRow
            var treeMenuBtn = CreateClickableIconImage(_menuIcon, "Menu", () =>
            {
                string GetSelectedTreePath()
                {
                    if (_folderTree == null) return null;
                    var indices = _folderTree.selectedIndices;
                    if (indices == null || !indices.Any()) return null;
                    int idx = indices.First();
                    try { return _folderTree.GetItemDataForIndex<string>(idx); }
                    catch { return null; }
                }

                string GetTargetFolder()
                {
                    string path = GetSelectedTreePath();
                    if (string.IsNullOrEmpty(path)) return GetActiveProfileRootPath();
                    if (AssetDatabase.IsValidFolder(path)) return path;

                    string folder = Path.GetDirectoryName(path)?.Replace('\\', '/');
                    if (!string.IsNullOrEmpty(folder) && AssetDatabase.IsValidFolder(folder)) return folder;
                    return GetActiveProfileRootPath();
                }

                string selectedPath = GetSelectedTreePath();
                bool canOperateOnSelection = !string.IsNullOrEmpty(selectedPath) && !string.Equals(selectedPath, GetActiveProfileRootPath(), StringComparison.OrdinalIgnoreCase);
                bool isProfileSelected = !string.IsNullOrEmpty(selectedPath) && selectedPath.EndsWith(".asset") && AssetDatabase.LoadAssetAtPath<ChartProfile>(selectedPath) != null;

                var menu = new GenericMenu();
                menu.AddItem(new GUIContent("New Chart"), false, () => CreateNewProfileInFolder(GetTargetFolder()));
                menu.AddItem(new GUIContent("New Folder"), false, () => CreateNewFolderUnder(GetTargetFolder()));
                menu.AddSeparator(string.Empty);

                if (isProfileSelected) menu.AddItem(new GUIContent("Copy"), false, () => CopyProfileToClipboard(selectedPath));
                else menu.AddDisabledItem(new GUIContent("Copy"));

                if (isProfileSelected && HasProfileInClipboard()) menu.AddItem(new GUIContent("Paste (Overwrite)"), false, () => PasteProfileOverwriteFromClipboard(selectedPath));
                else menu.AddDisabledItem(new GUIContent("Paste (Overwrite)"));

                if (HasProfileInClipboard()) menu.AddItem(new GUIContent("Paste (As New)"), false, () => PasteProfileAsNewFromClipboard(GetTargetFolder()));
                else menu.AddDisabledItem(new GUIContent("Paste (As New)"));

                if (isProfileSelected) menu.AddItem(new GUIContent("Clone %d"), false, () => CloneProfile(selectedPath));
                else menu.AddDisabledItem(new GUIContent("Clone %d"));

                menu.AddSeparator(string.Empty);

                menu.AddItem(new GUIContent("Refresh"), false, () => RefreshTree());
                menu.AddItem(new GUIContent("Expand All"), false, ExpandAllFolders);
                menu.AddItem(new GUIContent("Collapse All"), false, CollapseAllFolders);
                menu.AddSeparator(string.Empty);

                if (canOperateOnSelection) menu.AddItem(new GUIContent("Rename _F2"), false, () => RenameAssetOrFolder(selectedPath));
                else menu.AddDisabledItem(new GUIContent("Rename _F2"));

                if (canOperateOnSelection) menu.AddItem(new GUIContent("Delete _DELETE"), false, () => DeleteAssetOrFolderWithConfirmation(selectedPath));
                else menu.AddDisabledItem(new GUIContent("Delete _DELETE"));

                menu.AddSeparator(string.Empty);

                // Export UGUI Prefab option
                if (isProfileSelected) menu.AddItem(new GUIContent("Export UGUI Prefab"), false, () => ExportSelectedProfileAsUGUIPrefab(selectedPath));
                else menu.AddDisabledItem(new GUIContent("Export UGUI Prefab"));

                menu.AddSeparator(string.Empty);
                menu.AddItem(new GUIContent("Upgrade All Profiles"), false, UpgradeAllProfiles);

                menu.ShowAsContext();
            });
            // Add spacer and tree menu button to libraryHeaderRow
            var libraryHeaderSpacer = new VisualElement();
            libraryHeaderSpacer.style.flexGrow = 1;
            libraryHeaderRow.Add(libraryHeaderSpacer);

            var libraryHelpBtn = CreateClickableIconImage(_helpIcon, "Help", () => EasyChartManualWeb.OpenChapter("01_02-LibraryPanel"));
            libraryHelpBtn.style.marginLeft = 0;
            libraryHeaderRow.Add(libraryHelpBtn);
            
            treeMenuBtn.style.marginLeft = 6;
            libraryHeaderRow.Add(treeMenuBtn);

            // Global Toolbar (editor-wide actions)
            // Logo
            var logoImage = new Image();
            logoImage.image = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/EasyChart/Textures/Icon/UEC_Colorfull.png");
            logoImage.scaleMode = ScaleMode.ScaleToFit;
            logoImage.style.width = 56;
            logoImage.style.height = 28;
            logoImage.style.marginRight = 8;
            logoImage.style.flexShrink = 0;
            globalToolbar.Add(logoImage);
            
            // Import dropdown button
            var importDropdownBtn = new Button(() =>
            {
                var menu = new GenericMenu();
                
                // UXML imports
                menu.AddItem(new GUIContent("UXML/Restore All Profiles (Mirror)"), false, ImportUxmlMirrorToRestoreProfiles);
                
                // Check if a profile is selected for single restore
                string selectedPath = null;
                if (_folderTree != null)
                {
                    var indices = _folderTree.selectedIndices;
                    if (indices != null && indices.Count() > 0)
                    {
                        int id = indices.First();
                        selectedPath = _folderTree.GetItemDataForId<string>(id);
                    }
                }
                bool isProfileSelected = !string.IsNullOrEmpty(selectedPath) && selectedPath.EndsWith(".asset") && AssetDatabase.LoadAssetAtPath<ChartProfile>(selectedPath) != null;
                if (isProfileSelected)
                    menu.AddItem(new GUIContent("UXML/Restore Selected Profile"), false, () => ImportUxmlRestoreSelectedProfile(selectedPath));
                else
                    menu.AddDisabledItem(new GUIContent("UXML/Restore Selected Profile"));
                
                menu.AddItem(new GUIContent("UXML/Restore from Backup"), false, ImportUxmlFromBackup);
                
                // Unity Package import
                menu.AddItem(new GUIContent("Unity Package/Import Package..."), false, ImportUnityPackage);
                
                menu.ShowAsContext();
            }) { text = "Import ▼" };
            importDropdownBtn.style.minWidth = 70;

            // Export dropdown button
            var exportDropdownBtn = new Button(() =>
            {
                var menu = new GenericMenu();
                
                // UXML exports
                menu.AddItem(new GUIContent("UXML/Export Selected Profile"), false, ExportToUxml);
                menu.AddItem(new GUIContent("UXML/Export All Profiles (Mirror)"), false, ExportAllToUxmlMirror);
                menu.AddItem(new GUIContent("UXML/Export All Profiles (Backup)"), false, ExportAllToUxmlMirrorBackup);
                
                // Unity Package exports
                menu.AddItem(new GUIContent("Unity Package/Export Selected Profile"), false, ExportSelectedProfileUnityPackage);
                menu.AddItem(new GUIContent("Unity Package/Export All Profiles"), false, ExportAllProfilesUnityPackage);
                menu.AddItem(new GUIContent("Unity Package/Export Entire Library"), false, ExportEntireLibraryUnityPackage);
                
                // UGUI Prefab export
                string selectedPath = null;
                if (_folderTree != null)
                {
                    var indices = _folderTree.selectedIndices;
                    if (indices != null && indices.Count() > 0)
                    {
                        int id = indices.First();
                        selectedPath = _folderTree.GetItemDataForId<string>(id);
                    }
                }
                bool isProfileSelected = !string.IsNullOrEmpty(selectedPath) && selectedPath.EndsWith(".asset") && AssetDatabase.LoadAssetAtPath<ChartProfile>(selectedPath) != null;
                if (isProfileSelected)
                    menu.AddItem(new GUIContent("UGUI/Export Selected Profile as Prefab"), false, () => ExportSelectedProfileAsUGUIPrefab(selectedPath));
                else
                    menu.AddDisabledItem(new GUIContent("UGUI/Export Selected Profile as Prefab"));
                menu.AddItem(new GUIContent("UGUI/Export All Profiles as Prefabs"), false, ExportAllProfilesAsUGUIPrefabs);
                menu.AddItem(new GUIContent("UGUI/Export All Profiles as Prefabs (Overwrite)"), false, ExportAllProfilesAsUGUIPrefabsOverwrite);
                
                menu.ShowAsContext();
            }) { text = "Export ▼" };
            exportDropdownBtn.style.minWidth = 70;

            var globalSpacer = new VisualElement();
            globalSpacer.style.flexGrow = 1;

            var refreshBtn = CreateClickableIconImage(_refreshIcon, "Refresh", () =>
            {
                RefreshTree();
                ScheduleRefreshSeriesList();
                ScheduleUpdatePreview();
                EasyChartLog.Info(BuildSelectedProfileSummaryForDebug());
            });

            VisualElement saveBtn;
            _onSave = () =>
            {
                try
                {
                    if (_serializedProfile != null && _serializedProfile.hasModifiedProperties)
                    {
                        _serializedProfile.ApplyModifiedProperties();
                    }

                    if (_selectedProfile != null)
                    {
                        EditorUtility.SetDirty(_selectedProfile);
                    }

                    AssetDatabase.SaveAssets();
                }
                catch (Exception ex)
                {
                    EasyChart.Internal.EasyChartLog.Error($"Save failed: {ex.Message}\n{ex.StackTrace}");
                }
            };

            if (_saveIcon != null)
            {
                saveBtn = CreateClickableIconImage(_saveIcon, "Save (Ctrl+S)", _onSave);
            }
            else
            {
                saveBtn = new Button(_onSave) { text = "Save" };
            }

            var refreshSaveSeparator = new Label("|");
            refreshSaveSeparator.style.marginLeft = 5;
            refreshSaveSeparator.style.marginRight = 5;
            refreshSaveSeparator.style.unityTextAlign = TextAnchor.MiddleCenter;
            refreshSaveSeparator.style.color = Color.white;
            refreshSaveSeparator.style.opacity = 0.75f;
            refreshSaveSeparator.style.flexShrink = 0;

            // Import/Export at the beginning
            globalToolbar.Add(importDropdownBtn);
            globalToolbar.Add(exportDropdownBtn);
            
            var importExportSeparator = new Label("|");
            importExportSeparator.style.marginLeft = 8;
            importExportSeparator.style.marginRight = 8;
            importExportSeparator.style.unityTextAlign = TextAnchor.MiddleCenter;
            importExportSeparator.style.color = Color.white;
            importExportSeparator.style.opacity = 0.75f;
            importExportSeparator.style.flexShrink = 0;
            globalToolbar.Add(importExportSeparator);
            
            // Library controls
            var libraryLabel = new Label("Library:");
            libraryLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            libraryLabel.style.marginRight = 4;
            libraryLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            globalToolbar.Add(libraryLabel);
            globalToolbar.Add(libraryPopup);
            globalToolbar.Add(addLibraryBtn);
            globalToolbar.Add(deleteLibraryBtn);
            globalToolbar.Add(cloneLibraryBtn);
            globalToolbar.Add(themeBtn);
            
            var librarySeparator = new Label("|");
            librarySeparator.style.marginLeft = 8;
            librarySeparator.style.marginRight = 8;
            librarySeparator.style.unityTextAlign = TextAnchor.MiddleCenter;
            librarySeparator.style.color = Color.white;
            librarySeparator.style.opacity = 0.75f;
            librarySeparator.style.flexShrink = 0;
            globalToolbar.Add(librarySeparator);
            
            // Save and Refresh (moved here)
            globalToolbar.Add(saveBtn);
            globalToolbar.Add(refreshSaveSeparator);
            globalToolbar.Add(refreshBtn);
            
            globalToolbar.Add(globalSpacer);

            // Settings button (show in all versions)
            var settingsBtn = CreateClickableIconImage(_settingsIcon, "Settings", OpenSettingsWindow);
            settingsBtn.style.marginRight = 6;
            globalToolbar.Add(settingsBtn);

            // Separator between Settings and Support
            var settingsSupportSeparator = new Label("|");
            settingsSupportSeparator.style.marginLeft = 2;
            settingsSupportSeparator.style.marginRight = 6;
            settingsSupportSeparator.style.unityTextAlign = TextAnchor.MiddleCenter;
            settingsSupportSeparator.style.color = Color.white;
            settingsSupportSeparator.style.opacity = 0.75f;
            settingsSupportSeparator.style.flexShrink = 0;
            globalToolbar.Add(settingsSupportSeparator);

            // Support button (show in all versions)
            var supportBtn = CreateClickableIconImage(_supportIcon, "", () =>
            {
                // Click always shows popup (regardless of OnlyClick setting)
            });
            supportBtn.style.marginRight = 6;
            
            // Support hover popup
            var supportPopup = CreateSupportHoverPopup();
            supportBtn.RegisterCallback<PointerEnterEvent>(_ => 
            {
                if (!EditorPrefs.GetBool(PopupOnlyClickPrefsKey, false))
                {
                    ShowHoverPopup(supportPopup, supportBtn);
                }
            });
            supportBtn.RegisterCallback<PointerLeaveEvent>(_ => HideHoverPopup(supportPopup));
            supportBtn.RegisterCallback<ClickEvent>(_ => 
            {
                // Toggle popup on click
                if (supportPopup.style.display == DisplayStyle.None)
                {
                    ShowHoverPopup(supportPopup, supportBtn);
                }
                else
                {
                    supportPopup.style.display = DisplayStyle.None;
                }
            });
            globalToolbar.Add(supportBtn);
            
            // Separator between Support and Pro
            var supportProSeparator = new Label("|");
            supportProSeparator.style.marginLeft = 2;
            supportProSeparator.style.marginRight = 6;
            supportProSeparator.style.unityTextAlign = TextAnchor.MiddleCenter;
            supportProSeparator.style.color = Color.white;
            supportProSeparator.style.opacity = 0.75f;
            supportProSeparator.style.flexShrink = 0;
            globalToolbar.Add(supportProSeparator);
            
            // Pro button (show in all versions)
            var proBtn = CreateClickableIconImage(_proIcon, "", () =>
            {
                // Click always shows popup (regardless of OnlyClick setting)
            });
            proBtn.style.marginRight = 6;
            
            // Pro hover popup
            var proPopup = CreateProHoverPopup();
            proBtn.RegisterCallback<PointerEnterEvent>(_ => 
            {
                if (!EditorPrefs.GetBool(PopupOnlyClickPrefsKey, false))
                {
                    ShowHoverPopup(proPopup, proBtn);
                }
            });
            proBtn.RegisterCallback<PointerLeaveEvent>(_ => HideHoverPopup(proPopup));
            proBtn.RegisterCallback<ClickEvent>(_ => 
            {
                // Toggle popup on click
                if (proPopup.style.display == DisplayStyle.None)
                {
                    ShowHoverPopup(proPopup, proBtn);
                }
                else
                {
                    proPopup.style.display = DisplayStyle.None;
                }
            });
            globalToolbar.Add(proBtn);

            // Register static delegate so external callers can show this popup
            RequestShowProPopup = (worldRect) =>
            {
                if (proPopup.parent == null)
                    rootVisualElement.Add(proPopup);
                proPopup.style.display = DisplayStyle.Flex;
                proPopup.userData = false;
                proPopup.schedule.Execute(() =>
                {
                    var rootRect = rootVisualElement.worldBound;
                    float popupWidth = proPopup.resolvedStyle.width > 10 ? proPopup.resolvedStyle.width : 250;
                    float left = worldRect.xMax - rootRect.x - popupWidth;
                    float top = worldRect.yMax - rootRect.y;
                    if (left < 0) left = 0;
                    proPopup.style.left = left;
                    proPopup.style.top = top;
                }).ExecuteLater(0);
            };

            // Tree View
            _folderTree = new TreeView();
            _folderTree.selectionType = SelectionType.Multiple; // Support Ctrl/Shift multi-select
            _folderTree.style.flexGrow = 1;
            _folderTree.style.flexShrink = 1;
            _folderTree.style.minHeight = 0;
            _folderTree.makeItem = () =>
            {
                var row = new VisualElement();
                row.style.flexDirection = FlexDirection.Row;
                row.style.alignItems = Align.Center;
                row.style.minHeight = 18;

                var icon = new Image { name = "icon" };
                icon.scaleMode = ScaleMode.ScaleToFit;
                icon.style.width = 16;
                icon.style.height = 16;
                icon.style.flexShrink = 0;
                icon.style.marginRight = 6;
                icon.tintColor = Color.white;
                row.Add(icon);

                var label = new Label { name = "label" };
                label.style.flexGrow = 1;
                row.Add(label);

                var renameField = new TextField { name = "renameField" };
                renameField.style.flexGrow = 1;
                renameField.style.display = DisplayStyle.None;
                row.Add(renameField);

                renameField.RegisterCallback<KeyDownEvent>(evt =>
                {
                    if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter)
                    {
                        CommitInlineRename(renameField.userData as string, renameField.value);
                        evt.StopPropagation();
                        evt.PreventDefault();
                    }
                    else if (evt.keyCode == KeyCode.Escape)
                    {
                        CancelInlineRename();
                        evt.StopPropagation();
                        evt.PreventDefault();
                    }
                });

                renameField.RegisterCallback<FocusOutEvent>(evt =>
                {
                    return;
                });

                row.RegisterCallback<ContextClickEvent>(OnTreeItemContextClick, TrickleDown.TrickleDown);

                return row;
            };
            _folderTree.bindItem = (element, index) => 
            {
                EnsureSharedIconsLoaded();
                var item = _folderTree.GetItemDataForIndex<string>(index);
                var row = element as VisualElement;
                if (row == null) return;

                row.userData = item;

                var icon = row.Q<Image>("icon");
                var label = row.Q<Label>("label");
                var renameField = row.Q<TextField>("renameField");
                if (label == null) return;

                bool isFolder = AssetDatabase.IsValidFolder(item);
                if (isFolder)
                {
                    if (icon != null) icon.image = _folderIcon;
                    label.text = Path.GetFileName(item);
                    label.style.unityFontStyleAndWeight = FontStyle.Bold;
                }
                else
                {
                    if (icon != null) icon.image = _profileIcon;
                    label.text = Path.GetFileNameWithoutExtension(item);
                    label.style.unityFontStyleAndWeight = FontStyle.Normal;
                }

                bool isRenamingThis = !string.IsNullOrEmpty(_inlineRenamePath) && string.Equals(_inlineRenamePath, item, StringComparison.OrdinalIgnoreCase);
                if (renameField != null)
                {
                    renameField.userData = item;
                    renameField.value = label.text;
                    renameField.style.display = isRenamingThis ? DisplayStyle.Flex : DisplayStyle.None;
                }
                label.style.display = isRenamingThis ? DisplayStyle.None : DisplayStyle.Flex;

                if (isRenamingThis && renameField != null && !string.IsNullOrEmpty(_inlineRenamePendingFocusPath) && string.Equals(_inlineRenamePendingFocusPath, item, StringComparison.OrdinalIgnoreCase))
                {
                    _inlineRenamePendingFocusPath = null;
                    _inlineRenameField = renameField;
                    renameField.schedule.Execute(() =>
                    {
                        renameField.Focus();
                        renameField.SelectAll();
                    });
                }
            };
            _folderTree.selectionChanged += OnTreeSelectionChanged;
            _folderTree.RegisterCallback<PointerDownEvent>(OnTreePointerDown, TrickleDown.TrickleDown);
            _folderTree.RegisterCallback<PointerMoveEvent>(OnTreePointerMove, TrickleDown.TrickleDown);
            _folderTree.RegisterCallback<PointerUpEvent>(OnTreePointerUp, TrickleDown.TrickleDown);
            _folderTree.RegisterCallback<DragUpdatedEvent>(OnTreeDragUpdated, TrickleDown.TrickleDown);
            _folderTree.RegisterCallback<DragPerformEvent>(OnTreeDragPerform, TrickleDown.TrickleDown);
            _folderTree.RegisterCallback<KeyDownEvent>(OnTreeKeyDown);

            leftPanel.Add(_folderTree);

            BuildInjectionJsonPanel(leftPanel);


            // --- Center Panel: Editor & Preview ---
            var centerPanel = new VisualElement();
            centerPanel.style.flexGrow = 1;
            centerPanel.style.flexDirection = FlexDirection.Column;
            centerPanel.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f);
            centerPanel.style.borderRightWidth = 1;
            centerPanel.style.borderRightColor = new Color(0.15f, 0.15f, 0.15f);
            mainRow.Add(centerPanel);

            // Preview Area (Top)
            var previewHeaderRow = new VisualElement();
            previewHeaderRow.style.flexDirection = FlexDirection.Row;
            previewHeaderRow.style.alignItems = Align.Center;
            previewHeaderRow.style.paddingLeft = 10;
            previewHeaderRow.style.paddingRight = 10;
            previewHeaderRow.style.paddingTop = 5;
            previewHeaderRow.style.paddingBottom = 5;
            previewHeaderRow.style.backgroundColor = new Color(0.25f, 0.25f, 0.25f);

            var previewHeader = new Label("Preview");
            previewHeader.style.unityFontStyleAndWeight = FontStyle.Bold;
            previewHeaderRow.Add(previewHeader);

            var previewHeaderSpacer = new VisualElement();
            previewHeaderSpacer.style.flexGrow = 1;
            previewHeaderRow.Add(previewHeaderSpacer);

            var previewHelpBtn = CreateClickableIconImage(_helpIcon, "Help", () => EasyChartManualWeb.OpenChapter("02_04-PreviewPanel"));
            previewHelpBtn.style.marginLeft = 0;
            previewHeaderRow.Add(previewHelpBtn);

            centerPanel.Add(previewHeaderRow);

            _previewContainer = new VisualElement();
            _previewContainer.style.height = 350;
            _previewContainer.style.borderBottomWidth = 1;
            _previewContainer.style.borderBottomColor = new Color(0.15f, 0.15f, 0.15f);
            _previewContainer.style.justifyContent = Justify.Center;
            _previewContainer.style.alignItems = Align.Center;
            
            _previewContainer.style.backgroundImage = CreateCheckerTexture();
            _previewContainer.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Cover);
            
            centerPanel.Add(_previewContainer);

            // Add the actual chart element
            _previewChart = new ChartElement();
            _previewChart.style.width = 300;
            _previewChart.style.height = 200;
            _previewContainer.Add(_previewChart);

            ApplyActiveLibraryThemeToPreview(force: true);

            // Inspector Area (Bottom)
            var inspectorHeaderRow = new VisualElement();
            inspectorHeaderRow.style.flexDirection = FlexDirection.Row;
            inspectorHeaderRow.style.alignItems = Align.Center;
            inspectorHeaderRow.style.marginTop = 10;
            inspectorHeaderRow.style.paddingLeft = 10;
            inspectorHeaderRow.style.paddingRight = 10;
            inspectorHeaderRow.style.paddingTop = 5;
            inspectorHeaderRow.style.paddingBottom = 5;
            inspectorHeaderRow.style.backgroundColor = new Color(0.25f, 0.25f, 0.25f);

            var inspectorHeader = new Label("Inspector");
            inspectorHeader.style.unityFontStyleAndWeight = FontStyle.Bold;
            inspectorHeaderRow.Add(inspectorHeader);

            var inspectorHeaderSpacer = new VisualElement();
            inspectorHeaderSpacer.style.flexGrow = 1;
            inspectorHeaderRow.Add(inspectorHeaderSpacer);

            var inspectorHelpBtn = CreateClickableIconImage(_helpIcon, "Help", () => EasyChartManualWeb.OpenChapter("02_05-InspectorPanel"));
            inspectorHelpBtn.style.marginLeft = 0;
            inspectorHeaderRow.Add(inspectorHelpBtn);

            centerPanel.Add(inspectorHeaderRow);

            _inspectorContainer = new ScrollView();
            _inspectorContainer.style.flexGrow = 1;
            _inspectorContainer.style.paddingLeft = 15;
            _inspectorContainer.style.paddingRight = 15;
            _inspectorContainer.style.paddingTop = 10;
            _inspectorContainer.style.paddingBottom = 20;
            centerPanel.Add(_inspectorContainer);

            // --- Right Panel: Series Data ---
            var seriesPanel = new VisualElement();
            seriesPanel.style.width = 500;
            seriesPanel.style.height = Length.Percent(100); 
            seriesPanel.style.flexDirection = FlexDirection.Column;
            seriesPanel.style.borderLeftWidth = 1;
            seriesPanel.style.borderLeftColor = new Color(0.15f, 0.15f, 0.15f);
            seriesPanel.style.backgroundColor = new Color(0.18f, 0.18f, 0.18f);
            mainRow.Add(seriesPanel);

            var seriesHeaderRow = new VisualElement();
            seriesHeaderRow.style.flexDirection = FlexDirection.Row;
            seriesHeaderRow.style.alignItems = Align.Center;
            seriesHeaderRow.style.paddingLeft = 10;
            seriesHeaderRow.style.paddingRight = 10;
            seriesHeaderRow.style.paddingTop = 5;
            seriesHeaderRow.style.paddingBottom = 5;
            seriesHeaderRow.style.backgroundColor = new Color(0.25f, 0.25f, 0.25f);

            var seriesHeader = new Label("Series");
            seriesHeader.style.unityFontStyleAndWeight = FontStyle.Bold;
            seriesHeaderRow.Add(seriesHeader);

            var seriesHeaderSpacer = new VisualElement();
            seriesHeaderSpacer.style.flexGrow = 1;
            seriesHeaderRow.Add(seriesHeaderSpacer);

            var seriesHelpBtn = CreateClickableIconImage(_helpIcon, "Help", () => EasyChartManualWeb.OpenChapter("02_06-SeriesPanel"));
            seriesHelpBtn.style.marginLeft = 0;
            seriesHelpBtn.style.marginRight = 6;
            seriesHeaderRow.Add(seriesHelpBtn);

            // Global Sync toggle
            _seriesSyncToggle = new Toggle();
            _seriesSyncToggle.value = _seriesSyncEnabled;
            _seriesSyncToggle.tooltip = "Sync parameter changes to all series of the same type";
            _seriesSyncToggle.style.marginRight = 2;
            _seriesSyncToggle.RegisterValueChangedCallback(evt =>
            {
                _seriesSyncEnabled = evt.newValue;
                UpdateSyncLabelStyle();
            });
            seriesHeaderRow.Add(_seriesSyncToggle);

            _seriesSyncLabel = new Label("Sync");
            _seriesSyncLabel.style.fontSize = 12;
            _seriesSyncLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            _seriesSyncLabel.style.marginRight = 6;
            _seriesSyncLabel.tooltip = "Sync parameter changes to all series of the same type";
            UpdateSyncLabelStyle();
            seriesHeaderRow.Add(_seriesSyncLabel);

            // Color Palette button
            _paletteIcon = AssetDatabase.LoadAssetAtPath<Texture2D>(PaletteIconPath);
            if (_paletteIcon != null)
            {
                _paletteBtn = CreateClickableIconImage(_paletteIcon, "Apply Color Palette", ShowColorPaletteMenu);
                _paletteBtn.style.marginRight = 6;
                seriesHeaderRow.Add(_paletteBtn);
            }

            seriesPanel.Add(seriesHeaderRow);

            _seriesContainer = new ScrollView();
            _seriesContainer.style.height = Length.Percent(100); 
            _seriesContainer.style.flexGrow = 1;
            _seriesContainer.style.paddingLeft = 15;
            _seriesContainer.style.paddingRight = 15;
            _seriesContainer.style.paddingTop = 10;
            _seriesContainer.style.paddingBottom = 20;
            seriesPanel.Add(_seriesContainer);

            // Initial Load
            RefreshTree();

            root.Add(mainRow);
        }

        private void UpdateSyncLabelStyle()
        {
            if (_seriesSyncLabel == null) return;
            // Yellow when unchecked, green when checked, always bold
            _seriesSyncLabel.style.color = _seriesSyncEnabled 
                ? new Color(0.3f, 0.9f, 0.3f) 
                : new Color(1f, 0.85f, 0.2f);
            _seriesSyncLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        }

        private string BuildSelectedProfileSummaryForDebug()
        {
            try
            {
                var p = _selectedProfile;
                if (p == null) return "[EasyChart] Refresh Debug: selectedProfile = null";

                var sb = new System.Text.StringBuilder(2048);
                sb.AppendLine("[EasyChart] Refresh Debug Summary");
                sb.AppendLine($"Profile: {p.name} ({AssetDatabase.GetAssetPath(p)})");
                sb.AppendLine($"CoordinateSystem: {p.coordinateSystem}");
                sb.AppendLine($"AnimationDuration: {p.animationDuration}");
                sb.AppendLine($"Padding: {p.padding} (initialized={p.paddingInitialized})");
                sb.AppendLine($"XAxisId: {p.xAxisId}, YAxisId: {p.yAxisId} (axisSelectionInitialized={p.axisSelectionInitialized})");

                if (p.polarAxes != null)
                {
                    sb.AppendLine("-- PolarAxes --");

                    if (p.polarAxes.angleAxis != null)
                    {
                        var a = p.polarAxes.angleAxis;
                        sb.AppendLine($"AngleAxis: visible={a.visible}, color={a.color}, width={a.width}");
                        sb.AppendLine($"AngleAxis Labels: showLabels={a.showLabels}, fontSize={a.fontSize}, labelColor={a.labelColor}, labelOffset={a.labelOffset}");
                    }
                    else
                    {
                        sb.AppendLine("AngleAxis: null");
                    }

                    if (p.polarAxes.radiusAxis != null)
                    {
                        var r = p.polarAxes.radiusAxis;
                        sb.AppendLine($"RadiusAxis: visible={r.visible}, color={r.color}, width={r.width}");
                        sb.AppendLine($"RadiusAxis Range: autoMin={r.autoRangeMin}, autoMax={r.autoRangeMax}, min={r.minValue}, max={r.maxValue}");
                        sb.AppendLine($"RadiusAxis Ticks: autoTicks={r.autoTicks}, splitCount={r.splitCount}");
                    }
                    else
                    {
                        sb.AppendLine("RadiusAxis: null");
                    }
                }
                else
                {
                    sb.AppendLine("-- PolarAxes: null --");
                }

                // Axes (Category labels)
                sb.AppendLine("-- Axes --");
                if (p.axes == null)
                {
                    sb.AppendLine("axes: null");
                }
                else
                {
                    sb.AppendLine($"axes.Count: {p.axes.Count}");
                    for (int i = 0; i < p.axes.Count; i++)
                    {
                        var ax = p.axes[i];
                        if (ax == null)
                        {
                            sb.AppendLine($"[{i}] null");
                            continue;
                        }

                        int labelCount = ax.labels != null ? ax.labels.Count : 0;
                        sb.AppendLine($"[{i}] id={ax.id}, axisType={ax.axisType}, visible={ax.visible}, labels={labelCount}");
                        if (ax.labels != null && labelCount > 0)
                        {
                            int previewCount = Mathf.Min(12, labelCount);
                            sb.Append("    labelPreview: ");
                            for (int li = 0; li < previewCount; li++)
                            {
                                if (li > 0) sb.Append(" | ");
                                sb.Append(ax.labels[li]);
                            }
                            if (labelCount > previewCount) sb.Append(" | ...");
                            sb.AppendLine();
                        }
                    }
                }

                // Series
                sb.AppendLine("-- Series --");
                if (p.series == null)
                {
                    sb.AppendLine("series: null");
                }
                else
                {
                    sb.AppendLine($"series.Count: {p.series.Count}");
                    for (int si = 0; si < p.series.Count; si++)
                    {
                        var s = p.series[si];
                        if (s == null)
                        {
                            sb.AppendLine($"[{si}] null");
                            continue;
                        }

                        int dataCount = s.seriesData != null ? s.seriesData.Count : 0;
                        sb.AppendLine($"[{si}] name={s.name}, type={s.type}, visible={s.visible}, seriesData={dataCount}");

                        if (s.settings is RadarSettings rs)
                        {
                            sb.AppendLine("    settings=RadarSettings");
                            if (rs.stroke != null) sb.AppendLine($"    stroke: color={rs.stroke.color}, width={rs.stroke.width}");
                            if (rs.area != null) sb.AppendLine($"    area: show={rs.area.show}, color={(rs.area.textureFill != null ? rs.area.textureFill.color : Color.white)}");
                            if (rs.point != null)
                            {
                                string textureName = (rs.point.textureFill != null && rs.point.textureFill.texture != null)
                                    ? rs.point.textureFill.texture.name
                                    : "null";
                                sb.AppendLine($"    point: show={rs.point.show}, size={rs.point.size}, color={(rs.point.textureFill != null ? rs.point.textureFill.color : Color.white)}, texture={textureName}");
                            }
                        }
                        else
                        {
                            sb.AppendLine($"    settings={s.settings?.GetType().Name ?? "null"}");
                        }

                        if (s.seriesData != null && dataCount > 0)
                        {
                            int previewCount = Mathf.Min(12, dataCount);
                            sb.AppendLine("    dataPreview(index:y,name):");
                            for (int pi = 0; pi < previewCount; pi++)
                            {
                                var dp = s.seriesData[pi];
                                if (dp == null)
                                {
                                    sb.AppendLine($"      [{pi}] null");
                                }
                                else
                                {
                                    sb.AppendLine($"      [{pi}] y={dp.y}, name={dp.name}, x={dp.x}, id={dp.id}");
                                }
                            }
                            if (dataCount > previewCount) sb.AppendLine("      ...");
                        }
                    }
                }

                return sb.ToString();
            }
            catch (Exception e)
            {
                return "[EasyChart] Refresh Debug: exception while building summary: " + e;
            }
        }

        private void OnDisable()
        {
            EditorApplication.delayCall -= RefreshSeriesListDelayed;
            _seriesRefreshScheduled = false;
            _previewUpdateScheduled = false;
        }

        private void ScheduleRefreshSeriesList()
        {
            if (_seriesRefreshScheduled) return;
            _seriesRefreshScheduled = true;
            EditorApplication.delayCall += RefreshSeriesListDelayed;
        }

        private void RefreshSeriesListDelayed()
        {
            _seriesRefreshScheduled = false;
            bool savedSync = _seriesSyncEnabled;
            _seriesSyncEnabled = false;
            _seriesBindInitEventCount = 0;
            try
            {
                RefreshSeriesList();
            }
            finally
            {
                _seriesSyncEnabled = savedSync;
                _seriesUIRebuildEndTime = EditorApplication.timeSinceStartup;
            }
        }

        private StyleBackground CreateCheckerTexture()
        {
            int size = 20;
            var texture = new Texture2D(size, size);
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Repeat;
            
            Color c1 = new Color(0.4f, 0.4f, 0.4f);
            Color c2 = new Color(0.35f, 0.35f, 0.35f);
            
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    bool isWhite = (x < size/2 && y < size/2) || (x >= size/2 && y >= size/2);
                    texture.SetPixel(x, y, isWhite ? c1 : c2);
                }
            }
            texture.Apply();
            return new StyleBackground(texture);
        }

        private void CreateNewProfile()
        {
            string targetFolder = GetCreateTargetFolder();
            EnsureFolderExists(targetFolder);
            CreateNewProfileAtFolder(targetFolder);
        }

        private void CreateNewFolder()
        {
            string parentFolder = GetCreateTargetFolder();
            EnsureFolderExists(parentFolder);

            TextPromptWindow.Show("New Folder", "New Folder", folderName =>
            {
                folderName = SanitizeFileName(folderName);
                if (string.IsNullOrWhiteSpace(folderName)) return;

                string guid = AssetDatabase.CreateFolder(parentFolder, folderName);
                if (string.IsNullOrEmpty(guid))
                {
                    EditorUtility.DisplayDialog("Error", "Failed to create folder.", "OK");
                    return;
                }

                RefreshTree();
            });
        }

        private static string SanitizeFileName(string fileName)
        {
            var invalid = Path.GetInvalidFileNameChars();
            char[] chars = fileName.ToCharArray();
            for (int i = 0; i < chars.Length; i++)
            {
                for (int j = 0; j < invalid.Length; j++)
                {
                    if (chars[i] == invalid[j])
                    {
                        chars[i] = '_';
                        break;
                    }
                }
            }

            var sanitized = new string(chars).Trim();
            return string.IsNullOrEmpty(sanitized) ? "Chart" : sanitized;
        }

        private void ExportSelectedProfileAsUGUIPrefab(string profilePath)
        {
            var profile = AssetDatabase.LoadAssetAtPath<ChartProfile>(profilePath);
            if (profile == null)
            {
                EditorUtility.DisplayDialog("Error", "Failed to load ChartProfile.", "OK");
                return;
            }

            // Build folder path: Assets/EasyChart/UGUI/Prefabs/[LibraryName]/[RelativeFolder]
            string libraryName = GetSelectedLibraryName();
            string libraryRoot = GetActiveProfileRootPath();
            
            // Calculate relative path from library root
            string relativePath = profilePath;
            if (relativePath.StartsWith(libraryRoot))
            {
                relativePath = relativePath.Substring(libraryRoot.Length).TrimStart('/');
            }
            string relativeFolder = Path.GetDirectoryName(relativePath)?.Replace('\\', '/');
            
            // Build prefab folder path
            string basePrefabFolder = $"Assets/EasyChart/UGUI/Prefabs/{libraryName}";
            string prefabFolder = string.IsNullOrEmpty(relativeFolder) 
                ? basePrefabFolder 
                : $"{basePrefabFolder}/{relativeFolder}";
            
            // Ensure folder exists
            EnsureFolderExists(prefabFolder);

            // Find or create default PanelSettings
            var panelSettings = GetOrCreateDefaultPanelSettings("Assets/EasyChart/UGUI/Prefabs");

            // Create prefab name from profile name
            string profileName = Path.GetFileNameWithoutExtension(profilePath);
            string prefabPath = GetUniquePrefabPath(prefabFolder, profileName);

            // Create root GameObject
            var root = new GameObject(profileName);

            // Add RectTransform for positioning within Canvas
            var rectTransform = root.AddComponent<RectTransform>();
            float width = profile.chartWidth > 0 ? profile.chartWidth : 400f;
            float height = profile.chartHeight > 0 ? profile.chartHeight : 300f;
            rectTransform.sizeDelta = new Vector2(width, height);

            // Add UGUIChartBridge and set profile + PanelSettings
            var bridge = root.AddComponent<UGUI.UGUIChartBridge>();
            
            // Use SerializedObject to set private fields
            var so = new SerializedObject(bridge);
            so.FindProperty("_profile").objectReferenceValue = profile;
            so.FindProperty("_panelSettingsAsset").objectReferenceValue = panelSettings;
            so.ApplyModifiedPropertiesWithoutUndo();

            // Save as prefab
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);

            // Cleanup temp object
            DestroyImmediate(root);

            // Select the created prefab
            Selection.activeObject = prefab;
            EditorGUIUtility.PingObject(prefab);

            EasyChart.Internal.EasyChartLog.Info($"UGUI Chart prefab created at: {prefabPath}");
        }

        private PanelSettings GetOrCreateDefaultPanelSettings(string folder)
        {
            string panelSettingsPath = $"{folder}/EasyChartPanelSettings.asset";
            
            // Try to load existing
            var existing = AssetDatabase.LoadAssetAtPath<PanelSettings>(panelSettingsPath);
            if (existing != null)
                return existing;

            // Ensure folder exists before creating asset
            EnsureFolderExists(folder);

            // Create new PanelSettings
            var panelSettings = ScriptableObject.CreateInstance<PanelSettings>();
            panelSettings.scaleMode = PanelScaleMode.ConstantPixelSize;
            
            AssetDatabase.CreateAsset(panelSettings, panelSettingsPath);
            AssetDatabase.SaveAssets();
            
            Debug.Log($"[EasyChart] Created default PanelSettings at: {panelSettingsPath}");
            Debug.Log("[EasyChart] Please configure Text Settings in the PanelSettings asset for proper font rendering.");
            
            return panelSettings;
        }

        private static string GetUniquePrefabPath(string folder, string baseName)
        {
            string path = $"{folder}/{baseName}.prefab";
            if (!AssetDatabase.LoadAssetAtPath<GameObject>(path))
                return path;

            int index = 1;
            while (true)
            {
                path = $"{folder}/{baseName}_{index}.prefab";
                if (!AssetDatabase.LoadAssetAtPath<GameObject>(path))
                    return path;
                index++;
            }
        }

        private void ExportEntireLibraryUnityPackage()
        {
            string libraryRoot = GetActiveProfileRootPath();
            if (!AssetDatabase.IsValidFolder(libraryRoot))
            {
                EditorUtility.DisplayDialog("Export Library", $"Library folder not found: {libraryRoot}", "OK");
                return;
            }

            string savePath = EditorUtility.SaveFilePanel("Export Entire Library", "", "EasyChart_Library.unitypackage", "unitypackage");
            if (string.IsNullOrEmpty(savePath)) return;

            AssetDatabase.ExportPackage(libraryRoot, savePath, ExportPackageOptions.Recurse | ExportPackageOptions.IncludeDependencies);
            EasyChart.Internal.EasyChartLog.Info($"Exported entire library to: {savePath}");
        }

        private void ExportAllProfilesAsUGUIPrefabs()
        {
            ExportAllProfilesAsUGUIPrefabsInternal(overwrite: false);
        }

        private void ExportAllProfilesAsUGUIPrefabsOverwrite()
        {
            ExportAllProfilesAsUGUIPrefabsInternal(overwrite: true);
        }

        private void ExportAllProfilesAsUGUIPrefabsInternal(bool overwrite)
        {
            string libraryRoot = GetActiveProfileRootPath();
            var guids = AssetDatabase.FindAssets("t:ChartProfile", new[] { libraryRoot });
            
            if (guids == null || guids.Length == 0)
            {
                EditorUtility.DisplayDialog("Export UGUI Prefabs", "No ChartProfiles found in the library.", "OK");
                return;
            }

            // Build base folder path: Assets/EasyChart/UGUI/Prefabs/[LibraryName]
            string libraryName = GetSelectedLibraryName();
            string basePrefabFolder = $"Assets/EasyChart/UGUI/Prefabs/{libraryName}";
            EnsureFolderExists(basePrefabFolder);

            // Get or create PanelSettings
            var panelSettings = GetOrCreateDefaultPanelSettings("Assets/EasyChart/UGUI/Prefabs");

            int exportedCount = 0;
            int skippedCount = 0;
            int overwrittenCount = 0;

            foreach (var guid in guids)
            {
                string profilePath = AssetDatabase.GUIDToAssetPath(guid);
                var profile = AssetDatabase.LoadAssetAtPath<ChartProfile>(profilePath);
                if (profile == null)
                {
                    skippedCount++;
                    continue;
                }

                // Calculate relative path from library root to maintain folder structure
                string relativePath = profilePath;
                if (relativePath.StartsWith(libraryRoot))
                {
                    relativePath = relativePath.Substring(libraryRoot.Length).TrimStart('/');
                }

                // Get folder structure
                string relativeFolder = Path.GetDirectoryName(relativePath)?.Replace('\\', '/');
                string prefabFolder = string.IsNullOrEmpty(relativeFolder) 
                    ? basePrefabFolder 
                    : $"{basePrefabFolder}/{relativeFolder}";

                // Ensure subfolder exists
                EnsureFolderExists(prefabFolder);

                // Create prefab path
                string profileName = Path.GetFileNameWithoutExtension(profilePath);
                string prefabPath;
                
                if (overwrite)
                {
                    // Use direct path, will overwrite if exists
                    prefabPath = $"{prefabFolder}/{profileName}.prefab";
                    if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null)
                    {
                        overwrittenCount++;
                    }
                }
                else
                {
                    // Use unique path to avoid overwriting
                    prefabPath = GetUniquePrefabPath(prefabFolder, profileName);
                }

                var root = new GameObject(profileName);
                var rectTransform = root.AddComponent<RectTransform>();
                float width = profile.chartWidth > 0 ? profile.chartWidth : 400f;
                float height = profile.chartHeight > 0 ? profile.chartHeight : 300f;
                rectTransform.sizeDelta = new Vector2(width, height);

                var bridge = root.AddComponent<UGUI.UGUIChartBridge>();
                var so = new SerializedObject(bridge);
                so.FindProperty("_profile").objectReferenceValue = profile;
                so.FindProperty("_panelSettingsAsset").objectReferenceValue = panelSettings;
                so.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                DestroyImmediate(root);

                exportedCount++;
            }

            AssetDatabase.Refresh();

            string message = $"Exported {exportedCount} prefabs to {basePrefabFolder}";
            if (overwrittenCount > 0)
                message += $"\nOverwritten {overwrittenCount} existing prefabs.";
            if (skippedCount > 0)
                message += $"\nSkipped {skippedCount} invalid profiles.";
            
            EditorUtility.DisplayDialog("Export UGUI Prefabs", message, "OK");
            EasyChart.Internal.EasyChartLog.Info(message);
        }

        private void UpgradeAllProfiles()
        {
            string rootPath = GetActiveProfileRootPath();
            if (string.IsNullOrEmpty(rootPath) || !AssetDatabase.IsValidFolder(rootPath))
            {
                EditorUtility.DisplayDialog("Upgrade Profiles", "No valid library folder found.", "OK");
                return;
            }

            var guids = AssetDatabase.FindAssets("t:ChartProfile", new[] { rootPath });
            if (guids == null || guids.Length == 0)
            {
                EditorUtility.DisplayDialog("Upgrade Profiles", "No ChartProfile assets found in the current library.", "OK");
                return;
            }

            if (!EditorUtility.DisplayDialog("Upgrade All Profiles", 
                $"This will re-serialize {guids.Length} ChartProfile(s) in the current library to ensure all new fields have default values.\n\nThis operation cannot be undone. Continue?", 
                "Upgrade", "Cancel"))
            {
                return;
            }

            int upgradedCount = 0;
            int errorCount = 0;

            try
            {
                AssetDatabase.StartAssetEditing();

                for (int i = 0; i < guids.Length; i++)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                    var profile = AssetDatabase.LoadAssetAtPath<ChartProfile>(path);
                    if (profile == null)
                    {
                        errorCount++;
                        continue;
                    }

                    EditorUtility.DisplayProgressBar("Upgrading Profiles", $"Processing {profile.name}...", (float)i / guids.Length);

                    // Mark the asset as dirty to trigger re-serialization
                    EditorUtility.SetDirty(profile);
                    upgradedCount++;
                }

                AssetDatabase.SaveAssets();
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                EditorUtility.ClearProgressBar();
            }

            AssetDatabase.Refresh();

            string message = $"Upgraded {upgradedCount} ChartProfile(s).";
            if (errorCount > 0)
                message += $"\nFailed to load {errorCount} profile(s).";

            EditorUtility.DisplayDialog("Upgrade Complete", message, "OK");
            EasyChart.Internal.EasyChartLog.Info(message);
        }

        private void ShowColorPaletteMenu()
        {
            if (_selectedProfile == null)
            {
                EditorUtility.DisplayDialog("Color Palette", "Please select a ChartProfile first.", "OK");
                return;
            }

            // Load or initialize palettes
            {
                const string assetPath = "Assets/EasyChart/Scripts/Editor/SeriesColorPalettes.asset";
                var asset = AssetDatabase.LoadAssetAtPath<SeriesColorPalettesAsset>(assetPath);
                if (asset != null && asset.palettes != null && asset.palettes.Count > 0)
                {
                    _colorPalettes = asset.palettes;
                }
                else if (_colorPalettes == null || _colorPalettes.Count == 0)
                {
                    _colorPalettes = SeriesColorPalettesAsset.GetDefaultPalettes();
                }
            }

            // Show custom picker window with color preview, anchored right next to the palette button
            Rect activatorRect;
            if (_paletteBtn != null)
            {
                // worldBound coords + window screen position = button's screen Rect for ShowAsDropDown
                var btnRect = _paletteBtn.worldBound;
                float dpi = EditorGUIUtility.pixelsPerPoint;
                float screenX = position.x + btnRect.x / dpi;
                float screenY = position.y + btnRect.y / dpi;
                activatorRect = new Rect(screenX, screenY, btnRect.width / dpi, btnRect.height / dpi);
            }
            else
            {
                activatorRect = new Rect(position.x + position.width - 290f, position.y + 90f, 280f, 20f);
            }
            ColorPalettePickerWindow.Show(activatorRect, _colorPalettes, ApplyColorPalette, OpenPaletteManager, ResetColorPalettesToDefaults);
        }

        private void ResetColorPalettesToDefaults()
        {
            const string assetPath = "Assets/EasyChart/Scripts/Editor/SeriesColorPalettes.asset";
            var asset = AssetDatabase.LoadAssetAtPath<SeriesColorPalettesAsset>(assetPath);

            if (asset == null)
            {
                OpenPaletteManager();
                asset = AssetDatabase.LoadAssetAtPath<SeriesColorPalettesAsset>(assetPath);
                if (asset == null) return;
            }

            Undo.RecordObject(asset, "Reset Color Palettes");
            asset.palettes = SeriesColorPalettesAsset.GetDefaultPalettes();
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();

            _colorPalettes = asset.palettes;
        }

        private void ApplyColorPalette(SeriesColorPalette palette)
        {
            if (_selectedProfile == null || palette == null) 
            {
                Debug.LogWarning("[EasyChart] ApplyColorPalette: profile or palette is null");
                return;
            }
            if (_serializedProfile == null) 
            {
                Debug.LogWarning("[EasyChart] ApplyColorPalette: serializedProfile is null");
                return;
            }

            // Temporarily disable sync to prevent colors from being synced across series during apply
            bool wasSync = _seriesSyncEnabled;
            if (wasSync)
            {
                _seriesSyncEnabled = false;
                if (_seriesSyncToggle != null) _seriesSyncToggle.SetValueWithoutNotify(false);
                UpdateSyncLabelStyle();
            }

            _serializedProfile.Update();
            
            var seriesProp = _serializedProfile.FindProperty("series");
            if (seriesProp == null || !seriesProp.isArray) 
            {
                Debug.LogWarning("[EasyChart] ApplyColorPalette: series property not found or not array");
                return;
            }

            int seriesCount = seriesProp.arraySize;
            if (seriesCount == 0)
            {
                EditorUtility.DisplayDialog("Color Palette", "No series found in the current profile.", "OK");
                return;
            }

            Undo.RecordObject(_selectedProfile, "Apply Color Palette");

            int appliedCount = 0;
            for (int i = 0; i < seriesCount; i++)
            {
                var serieProp = seriesProp.GetArrayElementAtIndex(i);
                var typeProp = serieProp.FindPropertyRelative("type");
                if (typeProp == null) 
                {
                    Debug.LogWarning($"[EasyChart] Series {i}: type property not found");
                    continue;
                }

                SerieType serieType = (SerieType)typeProp.intValue;
                var colorSet = palette.GetColorSet(i);

                var settingsProp = serieProp.FindPropertyRelative("settings");
                if (settingsProp == null) 
                {
                    Debug.LogWarning($"[EasyChart] Series {i}: settings property not found");
                    continue;
                }

                // Apply colors based on series type
                bool applied = false;
                switch (serieType)
                {
                    case SerieType.Line:
                        applied = ApplyLineColors(settingsProp, colorSet);
                        break;
                    case SerieType.Bar:
                    case SerieType.HorizontalBar:
                        applied = ApplyBarColors(settingsProp, colorSet);
                        break;
                    case SerieType.Scatter:
                        applied = ApplyScatterColors(settingsProp, colorSet);
                        break;
                    case SerieType.Pie:
                    case SerieType.RingChart:
                        // Pie/Ring use data-driven colors, skip for now
                        break;
                }
                
                if (applied) appliedCount++;
            }

            // Start cooldown BEFORE ApplyModifiedProperties so that binding update events
            // fired by UIElements in subsequent frames are suppressed by SyncSeriesProperty.
            // The count-based guard in CreateSyncablePropertyField handles the exact events;
            // the timestamp is kept as a safety net for nested/composite property events.
            _seriesUIRebuildEndTime = EditorApplication.timeSinceStartup;

            _serializedProfile.ApplyModifiedProperties();
            EditorUtility.SetDirty(_selectedProfile);

            // Restore sync state now — the cooldown above will guard against spurious events
            if (wasSync)
            {
                _seriesSyncEnabled = true;
                if (_seriesSyncToggle != null) _seriesSyncToggle.SetValueWithoutNotify(true);
                UpdateSyncLabelStyle();
            }

            // Immediate preview update for color palette changes (no delay for better UX)
            UpdatePreview();
            ScheduleRefreshSeriesList();
        }

        private bool ApplyLineColors(SerializedProperty settingsProp, SeriesColorSet colorSet)
        {
            bool applied = false;
            
            // Line stroke color
            var strokeProp = settingsProp.FindPropertyRelative("stroke");
            if (strokeProp != null)
            {
                var colorProp = strokeProp.FindPropertyRelative("color");
                if (colorProp != null) 
                {
                    colorProp.colorValue = colorSet.lineColor;
                    applied = true;
                }
            }
            else
            {
                Debug.LogWarning("[EasyChart] Line: stroke property not found");
            }

            // Point color
            var pointProp = settingsProp.FindPropertyRelative("point");
            if (pointProp != null)
            {
                var textureFillProp = pointProp.FindPropertyRelative("textureFill");
                if (textureFillProp != null)
                {
                    var colorProp = textureFillProp.FindPropertyRelative("color");
                    if (colorProp != null) 
                    {
                        colorProp.colorValue = colorSet.pointColor;
                        applied = true;
                    }
                }
            }

            // Area color
            var areaProp = settingsProp.FindPropertyRelative("area");
            if (areaProp != null)
            {
                var textureFillProp = areaProp.FindPropertyRelative("textureFill");
                if (textureFillProp != null)
                {
                    var colorProp = textureFillProp.FindPropertyRelative("color");
                    if (colorProp != null) 
                    {
                        colorProp.colorValue = colorSet.areaColor;
                        applied = true;
                    }
                }
            }
            
            return applied;
        }

        private bool ApplyBarColors(SerializedProperty settingsProp, SeriesColorSet colorSet)
        {
            // Bar fill color
            var textureFillProp = settingsProp.FindPropertyRelative("textureFill");
            if (textureFillProp != null)
            {
                var colorProp = textureFillProp.FindPropertyRelative("color");
                if (colorProp != null) 
                {
                    colorProp.colorValue = colorSet.barColor;
                    return true;
                }
            }
            return false;
        }

        private bool ApplyScatterColors(SerializedProperty settingsProp, SeriesColorSet colorSet)
        {
            // Point color
            var pointProp = settingsProp.FindPropertyRelative("point");
            if (pointProp != null)
            {
                var textureFillProp = pointProp.FindPropertyRelative("textureFill");
                if (textureFillProp != null)
                {
                    var colorProp = textureFillProp.FindPropertyRelative("color");
                    if (colorProp != null) 
                    {
                        colorProp.colorValue = colorSet.pointColor;
                        return true;
                    }
                }
            }
            return false;
        }

        private void OpenPaletteManager()
        {
            // Find or create the palettes asset
            string assetPath = "Assets/EasyChart/Scripts/Editor/SeriesColorPalettes.asset";
            var asset = AssetDatabase.LoadAssetAtPath<SeriesColorPalettesAsset>(assetPath);
            
            if (asset == null)
            {
                // Create the asset with default palettes
                asset = ScriptableObject.CreateInstance<SeriesColorPalettesAsset>();
                asset.palettes = SeriesColorPalettesAsset.GetDefaultPalettes();
                
                // Ensure directory exists
                string dir = System.IO.Path.GetDirectoryName(assetPath);
                if (!AssetDatabase.IsValidFolder(dir))
                {
                    System.IO.Directory.CreateDirectory(dir);
                    AssetDatabase.Refresh();
                }
                
                AssetDatabase.CreateAsset(asset, assetPath);
                AssetDatabase.SaveAssets();
                Debug.Log($"[EasyChart] Created color palettes asset at {assetPath}");
            }

            // Select and ping the asset
            Selection.activeObject = asset;
            EditorGUIUtility.PingObject(asset);
        }
    }
}
