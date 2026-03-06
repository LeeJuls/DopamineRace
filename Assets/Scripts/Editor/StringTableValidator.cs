using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

/// <summary>
/// StringTable 키 누락 검증 도구
/// DopamineRace > Validate StringTable Keys
///
/// 코드(.cs)에서 Loc.Get("str.xxx") 패턴을 스캔하여
/// StringTable.csv에 없는 키를 Console에 출력한다.
/// </summary>
public class StringTableValidator : EditorWindow
{
    private static readonly string CSV_PATH  = "Assets/Resources/Data/StringTable.csv";
    private static readonly string SCRIPTS_PATH = "Assets/Scripts";

    private Vector2 scroll;
    private List<string> missingKeys = new List<string>();
    private List<string> unusedKeys  = new List<string>();
    private bool validated = false;

    [MenuItem("DopamineRace/Validate StringTable Keys")]
    public static void ShowWindow()
    {
        var w = GetWindow<StringTableValidator>("StringTable 검증");
        w.minSize = new Vector2(500, 400);
        w.RunValidation();
    }

    /// <summary>빌드 전 자동 실행 (BuildPlayerProcessor)</summary>
    [UnityEditor.Callbacks.DidReloadScripts]
    private static void OnScriptsReloaded()
    {
        // 스크립트 리로드 시 자동 검증 (결과는 Console에만 출력)
        var missing = FindMissingKeys(out _);
        if (missing.Count > 0)
        {
            Debug.LogWarning($"[StringTable] 누락된 키 {missing.Count}개 발견! " +
                             "DopamineRace > Validate StringTable Keys 로 확인하세요.");
        }
    }

    private void OnGUI()
    {
        EditorGUILayout.Space(8);
        if (GUILayout.Button("검증 실행", GUILayout.Height(32)))
            RunValidation();

        if (!validated) return;

        EditorGUILayout.Space(4);

        // ── 누락 키 ──
        Color prev = GUI.contentColor;
        if (missingKeys.Count == 0)
        {
            GUI.contentColor = Color.green;
            EditorGUILayout.LabelField($"✅ 누락 키 없음 — 모든 코드 키가 StringTable에 존재합니다.");
        }
        else
        {
            GUI.contentColor = Color.red;
            EditorGUILayout.LabelField($"❌ 누락 키 {missingKeys.Count}개 — StringTable에 추가하세요!");
        }
        GUI.contentColor = prev;

        scroll = EditorGUILayout.BeginScrollView(scroll);

        if (missingKeys.Count > 0)
        {
            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("── 누락 키 (코드에 있지만 CSV에 없음) ──", EditorStyles.boldLabel);
            foreach (var k in missingKeys)
            {
                GUI.contentColor = Color.red;
                EditorGUILayout.LabelField("  " + k);
            }
            GUI.contentColor = prev;
        }

        if (unusedKeys.Count > 0)
        {
            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField($"── 미사용 키 {unusedKeys.Count}개 (CSV에 있지만 코드에 없음) ──", EditorStyles.boldLabel);
            foreach (var k in unusedKeys)
            {
                GUI.contentColor = Color.gray;
                EditorGUILayout.LabelField("  " + k);
            }
            GUI.contentColor = prev;
        }

        EditorGUILayout.EndScrollView();
    }

    private void RunValidation()
    {
        missingKeys = FindMissingKeys(out unusedKeys);

        foreach (var k in missingKeys)
            Debug.LogError($"[StringTable] 누락: {k}");

        validated = true;
        Repaint();
    }

    private static List<string> FindMissingKeys(out List<string> unusedKeys)
    {
        // 1. CSV에서 정의된 키 수집
        var csvKeys = new HashSet<string>();
        string csvFull = Path.Combine(Application.dataPath, "../" + CSV_PATH);
        if (File.Exists(csvFull))
        {
            foreach (var line in File.ReadLines(csvFull))
            {
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#")) continue;
                int comma = line.IndexOf(',');
                if (comma > 0)
                    csvKeys.Add(line.Substring(0, comma).Trim());
            }
        }
        else
        {
            Debug.LogError($"[StringTable] CSV 파일을 찾을 수 없음: {CSV_PATH}");
            unusedKeys = new List<string>();
            return new List<string>();
        }

        // 2. 코드에서 사용된 키 수집 (Loc.Get / Loc.GetRank / HasKey 패턴)
        var codeKeys = new HashSet<string>();
        var pattern  = new Regex(@"(?:Loc\.Get|Loc\.HasKey)\s*\(\s*""(str\.[^""]+)""");
        string scriptRoot = Path.Combine(Application.dataPath, "../" + SCRIPTS_PATH);

        foreach (var file in Directory.GetFiles(scriptRoot, "*.cs", SearchOption.AllDirectories))
        {
            // Editor 폴더의 이 파일 자체는 제외
            if (file.Replace('\\', '/').Contains("/Editor/")) continue;

            string code = File.ReadAllText(file);
            foreach (Match m in pattern.Matches(code))
                codeKeys.Add(m.Groups[1].Value);
        }

        // 3. 비교
        var missing = new List<string>();
        foreach (var k in codeKeys)
            if (!csvKeys.Contains(k))
                missing.Add(k);
        missing.Sort();

        unusedKeys = new List<string>();
        foreach (var k in csvKeys)
            if (k.StartsWith("str.") && !codeKeys.Contains(k))
                unusedKeys.Add(k);
        unusedKeys.Sort();

        return missing;
    }
}
