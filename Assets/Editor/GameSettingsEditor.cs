using UnityEngine;
using UnityEditor;
using System.IO;

/// <summary>
/// GameSettings Inspector에 Save / Load / Reset 버튼 추가.
/// 수치 프리셋을 JSON 파일로 관리.
/// ※ Font, Sprite, TrackData 등 오브젝트 참조는 JSON에 포함되지 않음.
/// </summary>
[CustomEditor(typeof(GameSettings))]
public class GameSettingsEditor : Editor
{
    private const string PRESETS_FOLDER = "Assets/Settings/Presets";

    public override void OnInspectorGUI()
    {
        // 기본 Inspector 그리기
        DrawDefaultInspector();

        GUILayout.Space(15);
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        EditorGUILayout.LabelField("═══ 프리셋 관리 ═══", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "수치 설정을 JSON 파일로 저장/불러오기 합니다.\n" +
            "Font, Sprite, TrackData 등 오브젝트 참조는 포함되지 않습니다.",
            MessageType.Info);

        GUILayout.Space(5);

        EditorGUILayout.BeginHorizontal();

        // Save 버튼
        if (GUILayout.Button("💾 Save Preset", GUILayout.Height(30)))
        {
            SavePreset();
        }

        // Load 버튼
        if (GUILayout.Button("📂 Load Preset", GUILayout.Height(30)))
        {
            LoadPreset();
        }

        EditorGUILayout.EndHorizontal();

        GUILayout.Space(3);

        // Reset 버튼
        GUI.backgroundColor = new Color(1f, 0.7f, 0.7f);
        if (GUILayout.Button("🔄 Reset to Default", GUILayout.Height(25)))
        {
            ResetToDefault();
        }
        GUI.backgroundColor = Color.white;
    }

    // ═══════════════════════════════════════
    //  Save
    // ═══════════════════════════════════════

    private void SavePreset()
    {
        EnsurePresetFolder();

        string path = EditorUtility.SaveFilePanel(
            "프리셋 저장",
            GetAbsolutePresetsPath(),
            "preset_new",
            "json");

        if (string.IsNullOrEmpty(path)) return;

        GameSettings gs = (GameSettings)target;

        // 오브젝트 참조 임시 제거 → JSON 직렬화 → 복원
        Font savedMainFont = gs.mainFont;
        Font savedKoreanFont = gs.koreanFont;
        Sprite savedHitIcon = gs.vfxHitIcon;
        Sprite savedDodgeIcon = gs.vfxDodgeIcon;
        Sprite savedSlingshotIcon = gs.vfxSlingshotIcon;
        TrackData savedTrack = gs.currentTrack;

        gs.mainFont = null;
        gs.koreanFont = null;
        gs.vfxHitIcon = null;
        gs.vfxDodgeIcon = null;
        gs.vfxSlingshotIcon = null;
        gs.currentTrack = null;

        string json = JsonUtility.ToJson(gs, true);

        // 복원
        gs.mainFont = savedMainFont;
        gs.koreanFont = savedKoreanFont;
        gs.vfxHitIcon = savedHitIcon;
        gs.vfxDodgeIcon = savedDodgeIcon;
        gs.vfxSlingshotIcon = savedSlingshotIcon;
        gs.currentTrack = savedTrack;

        File.WriteAllText(path, json);
        AssetDatabase.Refresh();

        string fileName = Path.GetFileName(path);
        Debug.Log($"★ 프리셋 저장 완료: {fileName}");
        EditorUtility.DisplayDialog("저장 완료", $"프리셋이 저장되었습니다.\n{fileName}", "OK");
    }

    // ═══════════════════════════════════════
    //  Load
    // ═══════════════════════════════════════

    private void LoadPreset()
    {
        string path = EditorUtility.OpenFilePanel(
            "프리셋 불러오기",
            GetAbsolutePresetsPath(),
            "json");

        if (string.IsNullOrEmpty(path)) return;

        string json = File.ReadAllText(path);

        GameSettings gs = (GameSettings)target;

        // 오브젝트 참조 백업
        Font savedMainFont = gs.mainFont;
        Font savedKoreanFont = gs.koreanFont;
        Sprite savedHitIcon = gs.vfxHitIcon;
        Sprite savedDodgeIcon = gs.vfxDodgeIcon;
        Sprite savedSlingshotIcon = gs.vfxSlingshotIcon;
        TrackData savedTrack = gs.currentTrack;

        // JSON에서 수치만 덮어쓰기
        Undo.RecordObject(gs, "Load Preset");
        JsonUtility.FromJsonOverwrite(json, gs);

        // 오브젝트 참조 복원 (JSON에 없으므로)
        gs.mainFont = savedMainFont;
        gs.koreanFont = savedKoreanFont;
        gs.vfxHitIcon = savedHitIcon;
        gs.vfxDodgeIcon = savedDodgeIcon;
        gs.vfxSlingshotIcon = savedSlingshotIcon;
        gs.currentTrack = savedTrack;

        EditorUtility.SetDirty(gs);

        string fileName = Path.GetFileName(path);
        Debug.Log($"★ 프리셋 로드 완료: {fileName}");
        EditorUtility.DisplayDialog("로드 완료", $"프리셋을 불러왔습니다.\n{fileName}\n\nFont/Sprite/Track 참조는 유지됩니다.", "OK");
    }

    // ═══════════════════════════════════════
    //  Reset to Default
    // ═══════════════════════════════════════

    private void ResetToDefault()
    {
        if (!EditorUtility.DisplayDialog(
            "기본값 초기화",
            "모든 수치를 기본값으로 되돌립니다.\nFont/Sprite/Track 참조는 유지됩니다.\n\n계속하시겠습니까?",
            "초기화", "취소"))
            return;

        GameSettings gs = (GameSettings)target;

        // 오브젝트 참조 백업
        Font savedMainFont = gs.mainFont;
        Font savedKoreanFont = gs.koreanFont;
        Sprite savedHitIcon = gs.vfxHitIcon;
        Sprite savedDodgeIcon = gs.vfxDodgeIcon;
        Sprite savedSlingshotIcon = gs.vfxSlingshotIcon;
        TrackData savedTrack = gs.currentTrack;

        // 새 인스턴스의 기본값으로 덮어쓰기
        Undo.RecordObject(gs, "Reset to Default");
        GameSettings defaults = ScriptableObject.CreateInstance<GameSettings>();
        EditorUtility.CopySerializedIfDifferent(defaults, gs);
        DestroyImmediate(defaults);

        // 오브젝트 참조 복원
        gs.mainFont = savedMainFont;
        gs.koreanFont = savedKoreanFont;
        gs.vfxHitIcon = savedHitIcon;
        gs.vfxDodgeIcon = savedDodgeIcon;
        gs.vfxSlingshotIcon = savedSlingshotIcon;
        gs.currentTrack = savedTrack;

        EditorUtility.SetDirty(gs);
        Debug.Log("★ 기본값으로 초기화 완료 (Font/Sprite/Track 유지)");
    }

    // ═══════════════════════════════════════
    //  유틸
    // ═══════════════════════════════════════

    private void EnsurePresetFolder()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Settings"))
            AssetDatabase.CreateFolder("Assets", "Settings");
        if (!AssetDatabase.IsValidFolder(PRESETS_FOLDER))
            AssetDatabase.CreateFolder("Assets/Settings", "Presets");
    }

    private string GetAbsolutePresetsPath()
    {
        string projectPath = Application.dataPath.Replace("/Assets", "");
        string absPath = Path.Combine(projectPath, PRESETS_FOLDER);
        if (!Directory.Exists(absPath))
            Directory.CreateDirectory(absPath);
        return absPath;
    }
}
