using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using EasyChart;

namespace EasyChart.Editor
{
    /// <summary>
    /// Editor window for EasyChart global settings
    /// </summary>
    public class EasyChartSettingsWindow : EditorWindow
    {
        private EasyChartSettings _settings;
        private SerializedObject _serializedSettings;
        private VisualElement _root;

        [MenuItem("Tools/EasyChart/Settings")]
        public static void ShowWindow()
        {
            var window = GetWindow<EasyChartSettingsWindow>("EasyChart Settings");
            window.minSize = new Vector2(400, 500);
        }

        public static void Open()
        {
            ShowWindow();
        }

        private void OnEnable()
        {
            LoadSettings();
        }

        private void OnFocus()
        {
            LoadSettings();
            BuildUI();
        }

        private void LoadSettings()
        {
            _settings = EasyChartSettings.Instance;
            if (_settings != null)
            {
                _serializedSettings = new SerializedObject(_settings);
            }
        }

        private void BuildUI()
        {
            if (_serializedSettings == null)
            {
                ShowCreateSettingsUI();
                return;
            }

            rootVisualElement.Clear();

            var root = new ScrollView();
            root.style.paddingTop = 8;
            root.style.paddingBottom = 8;
            root.style.paddingLeft = 12;
            root.style.paddingRight = 12;
            rootVisualElement.Add(root);

            var title = new Label("EasyChart Global Settings");
            title.style.fontSize = 18;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.marginBottom = 16;
            root.Add(title);

            // Animation Section
            root.Add(BuildSection(_serializedSettings, "Animation", new[]
            {
                ("animationFps", "FPS", "Animation refresh rate in frames per second"),
                ("animationDuration", "Duration (ms)", "Default animation duration in milliseconds"),
            }));

            // Version Links Section
            root.Add(BuildSection(_serializedSettings, "Version Links", new[]
            {
                ("liteVersionUrl", "Lite URL", "Store page or documentation URL for Lite version"),
                ("proVersionUrl", "Pro URL", "Store page URL for Pro version"),
            }));

            // Editor Behavior Section
            root.Add(BuildSection(_serializedSettings, "Editor Behavior", new[]
            {
                ("previewDelay", "Preview Delay (ms)", "Delay before refreshing preview after changes"),
            }));

            // Debug Section
            root.Add(BuildSection(_serializedSettings, "Debug", new[]
            {
                ("enableDebugLogs", "Enable Logs", "Output debug logs to console"),
                ("logLevel", "Log Level", "Minimum log level to display"),
            }));

            root.Bind(_serializedSettings);

            // Add save notice
            var notice = new Label("Changes are saved automatically");
            notice.style.marginTop = 16;
            notice.style.unityFontStyleAndWeight = FontStyle.Italic;
            notice.style.opacity = 0.6f;
            root.Add(notice);
        }

        private void ShowCreateSettingsUI()
        {
            rootVisualElement.Clear();

            var container = new VisualElement();
            container.style.paddingTop = 20;
            container.style.paddingLeft = 20;
            container.style.paddingRight = 20;
            rootVisualElement.Add(container);

            var warning = new Label("Settings asset not found");
            warning.style.fontSize = 16;
            warning.style.unityFontStyleAndWeight = FontStyle.Bold;
            warning.style.marginBottom = 10;
            container.Add(warning);

            var info = new Label("Click the button below to create default settings.");
            info.style.marginBottom = 16;
            info.style.whiteSpace = WhiteSpace.Normal;
            container.Add(info);

            var createBtn = new Button(() =>
            {
                CreateDefaultSettingsAsset();
                LoadSettings();
                BuildUI();
            });
            createBtn.text = "Create Settings Asset";
            createBtn.style.width = 200;
            createBtn.style.height = 30;
            container.Add(createBtn);
        }

        private void CreateDefaultSettingsAsset()
        {
            string resourcesPath = "Assets/EasyChart/Resources";
            string assetPath = "Assets/EasyChart/Resources/EasyChartSettings.asset";

            // Ensure Resources folder exists
            if (!AssetDatabase.IsValidFolder(resourcesPath))
            {
                string easyChartFolder = "Assets/EasyChart";
                if (!AssetDatabase.IsValidFolder(easyChartFolder))
                {
                    AssetDatabase.CreateFolder("Assets", "EasyChart");
                }
                AssetDatabase.CreateFolder(easyChartFolder, "Resources");
            }

            var settings = CreateInstance<EasyChartSettings>();
            AssetDatabase.CreateAsset(settings, assetPath);
            AssetDatabase.SaveAssets();

            Debug.Log($"[EasyChart] Created settings asset at: {assetPath}");
        }

        private static VisualElement BuildSection(SerializedObject so, string title, (string prop, string label, string tooltip)[] fields)
        {
            // Create foldout with box styling matching Inspector settings (CreateGroupBox style)
            var foldout = new Foldout { text = title };
            foldout.bindingPath = string.Empty;
            foldout.SetValueWithoutNotify(true);

            var borderColor = new Color(0.1f, 0.1f, 0.1f);
            var backgroundColor = new Color(0.18f, 0.18f, 0.18f);

            // Apply styles directly to foldout content container
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
}
