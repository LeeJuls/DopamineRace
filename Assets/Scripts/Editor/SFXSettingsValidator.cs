using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

/// <summary>
/// SFX 키 누락·미할당 검증 도구 (StringTableValidator 패턴)
/// DopamineRace > Validate SFX Keys
///
/// 데이터 소스가 CSV가 아니라 SFXKeys(상수클래스)이므로 reflection으로 "선언된 전체 키"를
/// 확실히 수집 — 정규식 리터럴 스캔의 한계(변수 조합·클래스 접두사 누락 등)를 회피한다.
///
/// 자동 실행 타이밍:
///   1) 스크립트 저장·컴파일 완료 시 (DidReloadScripts)
///   2) Play 버튼 클릭 직전 (playModeStateChanged)
/// 수동: DopamineRace > Validate SFX Keys 메뉴
/// </summary>
[InitializeOnLoad]
public class SFXSettingsValidator : EditorWindow
{
    private static readonly string SFX_ASSET_PATH = "Assets/Resources/SFXSettings.asset";
    private static readonly string SCRIPTS_PATH   = "Assets/Scripts";

    private Vector2 scroll;
    private List<string> missingKeys    = new List<string>(); // SFXKeys엔 있는데 asset 미등록
    private List<string> duplicateKeys  = new List<string>(); // asset 내 중복 key
    private List<string> unassignedKeys = new List<string>(); // asset엔 있는데 clips 미할당(경고, 에러 아님)
    private List<string> unusedKeys     = new List<string>(); // SFXKeys엔 있는데 코드에서 미호출
    private bool validated = false;

    static SFXSettingsValidator()
    {
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.ExitingEditMode) return;

        var missing = FindMissingKeys(out _, out var dup, out _, out _);
        if (missing.Count > 0 || dup.Count > 0)
        {
            Debug.LogWarning($"[SFXSettings] ⚠️ Play 진입 전 누락 키 {missing.Count}개 / 중복 키 {dup.Count}개 발견! " +
                             "→ DopamineRace > Validate SFX Keys 로 상세 확인");
        }
    }

    [MenuItem("DopamineRace/Validate SFX Keys")]
    public static void ShowWindow()
    {
        var w = GetWindow<SFXSettingsValidator>("SFX 키 검증");
        w.minSize = new Vector2(500, 400);
        w.RunValidation();
    }

    [UnityEditor.Callbacks.DidReloadScripts]
    private static void OnScriptsReloaded()
    {
        var missing = FindMissingKeys(out _, out var dup, out _, out _);
        if (missing.Count > 0 || dup.Count > 0)
        {
            Debug.LogWarning($"[SFXSettings] 누락 키 {missing.Count}개 / 중복 키 {dup.Count}개 발견! " +
                             "DopamineRace > Validate SFX Keys 로 확인하세요.");
        }
    }

    private void OnGUI()
    {
        EditorGUILayout.Space(8);
        if (GUILayout.Button("검증 실행", GUILayout.Height(32)))
            RunValidation();

        if (!validated) return;

        EditorGUILayout.Space(4);
        Color prev = GUI.contentColor;

        if (missingKeys.Count == 0 && duplicateKeys.Count == 0)
        {
            GUI.contentColor = Color.green;
            EditorGUILayout.LabelField("✅ 누락·중복 키 없음 — SFXKeys 전부 SFXSettings.asset에 등록됨.");
        }
        else
        {
            GUI.contentColor = Color.red;
            EditorGUILayout.LabelField($"❌ 누락 {missingKeys.Count}개 / 중복 {duplicateKeys.Count}개");
        }
        GUI.contentColor = prev;

        scroll = EditorGUILayout.BeginScrollView(scroll);

        if (missingKeys.Count > 0)
        {
            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("── 누락 (SFXKeys에 선언됐지만 SFXSettings.asset 미등록) ──", EditorStyles.boldLabel);
            foreach (var k in missingKeys) { GUI.contentColor = Color.red; EditorGUILayout.LabelField("  " + k); }
            GUI.contentColor = prev;
        }
        if (duplicateKeys.Count > 0)
        {
            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("── 중복 key (asset 내 동일 key 2개 이상) ──", EditorStyles.boldLabel);
            foreach (var k in duplicateKeys) { GUI.contentColor = Color.red; EditorGUILayout.LabelField("  " + k); }
            GUI.contentColor = prev;
        }
        if (unassignedKeys.Count > 0)
        {
            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField($"── 미할당 클립 {unassignedKeys.Count}개 (경고 — 무음 상태, 에러 아님) ──", EditorStyles.boldLabel);
            foreach (var k in unassignedKeys) { GUI.contentColor = Color.yellow; EditorGUILayout.LabelField("  " + k); }
            GUI.contentColor = prev;
        }
        if (unusedKeys.Count > 0)
        {
            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField($"── 미사용 키 {unusedKeys.Count}개 (SFXKeys엔 있지만 코드에서 미호출) ──", EditorStyles.boldLabel);
            foreach (var k in unusedKeys) { GUI.contentColor = Color.gray; EditorGUILayout.LabelField("  " + k); }
            GUI.contentColor = prev;
        }

        EditorGUILayout.EndScrollView();
    }

    private void RunValidation()
    {
        missingKeys = FindMissingKeys(out unusedKeys, out duplicateKeys, out unassignedKeys, out _);
        foreach (var k in missingKeys) Debug.LogError($"[SFXSettings] 누락: {k}");
        foreach (var k in duplicateKeys) Debug.LogError($"[SFXSettings] 중복 key: {k}");
        validated = true;
        Repaint();
    }

    private static List<string> FindMissingKeys(out List<string> unused, out List<string> duplicates,
        out List<string> unassigned, out List<string> _unusedReserved)
    {
        _unusedReserved = null;

        // 1. SFXKeys 상수 전체 수집 (reflection — 정규식보다 신뢰도 높음, 변수 조합 등 한계 없음)
        var declaredKeys = new HashSet<string>();
        foreach (var f in typeof(SFXKeys).GetFields(BindingFlags.Public | BindingFlags.Static))
        {
            if (f.IsLiteral && f.FieldType == typeof(string))
                declaredKeys.Add((string)f.GetRawConstantValue());
        }

        // 2. SFXSettings.asset의 entries 수집 (key별 등장 횟수 → 중복 판정)
        var assetKeyCounts = new Dictionary<string, int>();
        var unassignedList = new List<string>();
        var settings = AssetDatabase.LoadAssetAtPath<SFXSettings>(SFX_ASSET_PATH);
        if (settings == null)
        {
            Debug.LogError($"[SFXSettings] 에셋을 찾을 수 없음: {SFX_ASSET_PATH}");
            unused = new List<string>();
            duplicates = new List<string>();
            unassigned = unassignedList;
            return new List<string>(declaredKeys);
        }

        foreach (var e in settings.entries)
        {
            if (string.IsNullOrEmpty(e.key)) continue;
            assetKeyCounts.TryGetValue(e.key, out int c);
            assetKeyCounts[e.key] = c + 1;
            if (e.clips == null || e.clips.Length == 0 || System.Array.TrueForAll(e.clips, c2 => c2 == null))
                unassignedList.Add(e.key);
        }
        unassignedList.Sort();
        unassigned = unassignedList;

        duplicates = assetKeyCounts.Where(kv => kv.Value > 1).Select(kv => kv.Key).OrderBy(k => k).ToList();

        // 3. 누락 = SFXKeys 선언됐지만 asset 미등록
        var missing = declaredKeys.Where(k => !assetKeyCounts.ContainsKey(k)).OrderBy(k => k).ToList();

        // 4. 미사용 = SFXKeys 선언됐지만 코드에서 SFXKeys.필드명 형태로 호출된 적 없음 (정규식, 보조 대조)
        var usedFieldNames = new HashSet<string>();
        var pattern = new Regex(@"SFXKeys\.(\w+)");
        string scriptRoot = Path.Combine(Application.dataPath, "../" + SCRIPTS_PATH);
        foreach (var file in Directory.GetFiles(scriptRoot, "*.cs", SearchOption.AllDirectories))
        {
            if (file.Replace('\\', '/').Contains("/Editor/")) continue; // 검증기 자신 등 Editor 스크립트 제외
            string code = File.ReadAllText(file);
            foreach (Match m in pattern.Matches(code))
                usedFieldNames.Add(m.Groups[1].Value);
        }
        var fieldNameByValue = typeof(SFXKeys).GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(f => f.IsLiteral && f.FieldType == typeof(string))
            .ToDictionary(f => (string)f.GetRawConstantValue(), f => f.Name);

        unused = declaredKeys
            .Where(k => fieldNameByValue.TryGetValue(k, out var fn) && !usedFieldNames.Contains(fn))
            .OrderBy(k => k).ToList();

        return missing;
    }
}
