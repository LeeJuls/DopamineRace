#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// Pixem 프리팹 저장 시 이름을 입력받는 에디터 팝업 다이얼로그.
/// ShowUtility()로 최상위 유틸리티 윈도우로 표시.
/// </summary>
public class PixemSaveDialog : EditorWindow
{
    private string _prefabName;
    private System.Action<string> _onSave;
    private bool _focusSet;

    public static void Show(string defaultName, System.Action<string> onSave)
    {
        var window = CreateInstance<PixemSaveDialog>();
        window._prefabName = defaultName;
        window._onSave = onSave;
        window._focusSet = false;
        window.titleContent = new GUIContent("Save Character Prefab");
        window.minSize = new Vector2(360, 110);
        window.maxSize = new Vector2(360, 110);
        window.ShowUtility();
    }

    private void OnGUI()
    {
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Prefab Name:", EditorStyles.boldLabel);

        GUI.SetNextControlName("PrefabNameField");
        _prefabName = EditorGUILayout.TextField(_prefabName);

        if (!_focusSet)
        {
            EditorGUI.FocusTextInControl("PrefabNameField");
            _focusSet = true;
        }

        EditorGUILayout.Space(10);

        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();

        bool enterPressed = Event.current.type == EventType.KeyDown
            && Event.current.keyCode == KeyCode.Return;

        if (GUILayout.Button("Save", GUILayout.Width(80)) || enterPressed)
        {
            if (!string.IsNullOrEmpty(_prefabName?.Trim()))
            {
                _onSave?.Invoke(_prefabName.Trim());
                Close();
            }
        }

        bool escPressed = Event.current.type == EventType.KeyDown
            && Event.current.keyCode == KeyCode.Escape;

        if (GUILayout.Button("Cancel", GUILayout.Width(80)) || escPressed)
        {
            Close();
        }

        EditorGUILayout.EndHorizontal();
    }
}
#endif
