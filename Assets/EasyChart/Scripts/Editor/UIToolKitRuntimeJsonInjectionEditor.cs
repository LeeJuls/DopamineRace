using UnityEngine;
using UnityEditor;
using EasyChart.UIToolKit;

namespace EasyChart.Editor
{
    [CustomEditor(typeof(UIToolKitRuntimeJsonInjection))]
    public class UIToolKitRuntimeJsonInjectionEditor : UnityEditor.Editor
    {
        private SerializedProperty _chartElementNameProp;
        private SerializedProperty _exampleModeProp;
        private SerializedProperty _datasModeProp;
        private SerializedProperty _useApiEnvelopeProp;
        private SerializedProperty _autoGenerateJsonProp;
        private SerializedProperty _jsonContentProp;

        private GUIStyle _jsonTextAreaStyle;
        private Vector2 _scrollPosition;
        private bool _showJsonContent = true;

        private static readonly GUIContent GenerateButtonContent = new GUIContent("Generate Example JSON", "Generate example JSON from the current chart profile");
        private static readonly GUIContent ApplyButtonContent = new GUIContent("Apply JSON to Chart", "Apply the JSON content to the chart");
        private static readonly GUIContent CopyButtonContent = new GUIContent("Copy", "Copy JSON to clipboard");
        private static readonly GUIContent PasteButtonContent = new GUIContent("Paste", "Paste JSON from clipboard");
        private static readonly GUIContent ClearButtonContent = new GUIContent("Clear", "Clear JSON content");

        private void OnEnable()
        {
            _chartElementNameProp = serializedObject.FindProperty("_chartElementName");
            _exampleModeProp = serializedObject.FindProperty("_exampleMode");
            _datasModeProp = serializedObject.FindProperty("_datasMode");
            _useApiEnvelopeProp = serializedObject.FindProperty("_useApiEnvelope");
            _autoGenerateJsonProp = serializedObject.FindProperty("_autoGenerateJson");
            _jsonContentProp = serializedObject.FindProperty("_jsonContent");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            if (_jsonTextAreaStyle == null)
            {
                _jsonTextAreaStyle = new GUIStyle(EditorStyles.textArea)
                {
                    wordWrap = true,
                    font = EditorStyles.standardFont,
                    fontSize = 11
                };
            }

            var injection = (UIToolKitRuntimeJsonInjection)target;

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("JSON Injection (UI Toolkit)", EditorStyles.boldLabel);
            EditorGUILayout.Space(2);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            {
                EditorGUILayout.LabelField("Target", EditorStyles.miniBoldLabel);
                EditorGUILayout.Space(2);
                EditorGUILayout.PropertyField(_chartElementNameProp, new GUIContent("Chart Element Name", "Optional. If empty, uses the first ChartElement found in UIDocument."));
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(5);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            {
                EditorGUILayout.LabelField("Generation Settings", EditorStyles.miniBoldLabel);
                EditorGUILayout.Space(2);

                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(_exampleModeProp, new GUIContent("Example Mode", "The format mode for generating example JSON"));
                EditorGUILayout.PropertyField(_datasModeProp, new GUIContent("Data Mode", "The data format mode for series data"));
                EditorGUILayout.PropertyField(_useApiEnvelopeProp, new GUIContent("API Envelope", "Wrap JSON in API response envelope"));
                bool settingsChanged = EditorGUI.EndChangeCheck();

                EditorGUILayout.PropertyField(_autoGenerateJsonProp, new GUIContent("Auto Generate", "Automatically regenerate JSON when settings change"));

                if (settingsChanged && _autoGenerateJsonProp.boolValue)
                {
                    serializedObject.ApplyModifiedProperties();
                    Undo.RecordObject(injection, "Auto Generate JSON");
                    injection.GenerateExampleJson();
                    EditorUtility.SetDirty(injection);
                    serializedObject.Update();
                }
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(5);

            EditorGUILayout.BeginHorizontal();
            {
                GUI.backgroundColor = new Color(0.6f, 0.8f, 1f);
                if (GUILayout.Button(GenerateButtonContent, GUILayout.Height(28)))
                {
                    Undo.RecordObject(injection, "Generate Example JSON");
                    injection.GenerateExampleJson();
                    EditorUtility.SetDirty(injection);
                    serializedObject.Update();
                }

                GUI.backgroundColor = new Color(0.6f, 1f, 0.6f);
                if (GUILayout.Button(ApplyButtonContent, GUILayout.Height(28)))
                {
                    injection.ApplyJsonToChart();
                }
                GUI.backgroundColor = Color.white;
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);

            _showJsonContent = EditorGUILayout.Foldout(_showJsonContent, "JSON Content", true);
            if (_showJsonContent)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                {
                    EditorGUILayout.BeginHorizontal();
                    {
                        if (GUILayout.Button(CopyButtonContent, EditorStyles.miniButtonLeft, GUILayout.Width(60)))
                        {
                            GUIUtility.systemCopyBuffer = _jsonContentProp.stringValue;
                            Debug.Log("[UIToolKitRuntimeJsonInjection] JSON copied to clipboard.");
                        }

                        if (GUILayout.Button(PasteButtonContent, EditorStyles.miniButtonMid, GUILayout.Width(60)))
                        {
                            Undo.RecordObject(injection, "Paste JSON");
                            _jsonContentProp.stringValue = GUIUtility.systemCopyBuffer;
                            serializedObject.ApplyModifiedProperties();
                            Debug.Log("[UIToolKitRuntimeJsonInjection] JSON pasted from clipboard.");
                        }

                        if (GUILayout.Button(ClearButtonContent, EditorStyles.miniButtonRight, GUILayout.Width(60)))
                        {
                            Undo.RecordObject(injection, "Clear JSON");
                            _jsonContentProp.stringValue = "";
                            serializedObject.ApplyModifiedProperties();
                        }

                        GUILayout.FlexibleSpace();

                        int charCount = _jsonContentProp.stringValue?.Length ?? 0;
                        EditorGUILayout.LabelField($"{charCount} chars", EditorStyles.miniLabel, GUILayout.Width(80));
                    }
                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.Space(2);

                    float minHeight = 150;
                    float maxHeight = 400;
                    float lineCount = _jsonContentProp.stringValue?.Split('\n').Length ?? 1;
                    float calculatedHeight = Mathf.Clamp(lineCount * 14 + 20, minHeight, maxHeight);

                    _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, GUILayout.Height(calculatedHeight));
                    {
                        EditorGUI.BeginChangeCheck();
                        string newValue = EditorGUILayout.TextArea(_jsonContentProp.stringValue, _jsonTextAreaStyle, GUILayout.ExpandHeight(true));
                        if (EditorGUI.EndChangeCheck())
                        {
                            _jsonContentProp.stringValue = newValue;
                        }
                    }
                    EditorGUILayout.EndScrollView();
                }
                EditorGUILayout.EndVertical();
            }

            serializedObject.ApplyModifiedProperties();

            EditorGUILayout.Space(5);

            EditorGUILayout.HelpBox(
                "1. Click 'Generate Example JSON' to create sample JSON from the current ChartProfile (found via UIDocument -> ChartElement).\n" +
                "2. Modify the JSON as needed.\n" +
                "3. Click 'Apply JSON to Chart' to inject the data into the chart.",
                MessageType.Info);
        }
    }
}
