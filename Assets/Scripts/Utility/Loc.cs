using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// CSV 기반 다국어 로컬라이제이션 모듈.
/// 사용법: Loc.Get("str.ui.btn.start"), Loc.Get("str.hud.lap", 5)
/// </summary>
public static class Loc
{
    // 현재 언어 (기본: "ko")
    public static string CurrentLang { get; private set; } = "ko";

    // 내부 딕셔너리: UID → 번역 문자열
    private static Dictionary<string, string> table = new Dictionary<string, string>();
    private static bool isLoaded = false;

    // 언어 컬럼 인덱스 매핑
    // CSV 헤더: UID(0), ko(1), en(2), jp(3)
    private static readonly Dictionary<string, int> langIndex = new Dictionary<string, int>
    {
        { "ko", 1 }, { "en", 2 }, { "jp", 3 }
    };

    /// <summary>
    /// 언어 설정 + CSV 로드. 런타임에서 언어 전환 시 호출.
    /// lang: "ko" | "en" | "jp"
    /// </summary>
    public static void SetLang(string lang)
    {
        CurrentLang = lang;
        isLoaded = false;
        Load();
        PlayerPrefs.SetString("DR_Lang", lang);
    }

    /// <summary>
    /// 저장된 언어 로드. 최초 실행 시 시스템 언어 감지.
    /// </summary>
    public static void Init()
    {
        string saved = PlayerPrefs.GetString("DR_Lang", "");
        if (string.IsNullOrEmpty(saved))
        {
            // 최초 실행: 시스템 언어 감지
            saved = DetectSystemLanguage();
            PlayerPrefs.SetString("DR_Lang", saved);
            Debug.Log($"[Loc] 시스템 언어 감지: {Application.systemLanguage} → {saved}");
        }
        CurrentLang = saved;
        Load();
    }

    /// <summary>
    /// Application.systemLanguage → 지원 언어 코드 매핑
    /// </summary>
    private static string DetectSystemLanguage()
    {
        switch (Application.systemLanguage)
        {
            case SystemLanguage.Korean:
                return "ko";
            case SystemLanguage.Japanese:
                return "jp";
            case SystemLanguage.English:
            default:
                return "en";
        }
    }

    /// <summary>
    /// 번역 문자열 반환. 포맷 인자 지원.
    /// Loc.Get("str.hud.lap", 5) → "5바퀴"
    /// </summary>
    public static string Get(string uid, params object[] args)
    {
        if (!isLoaded) Load();

        if (string.IsNullOrEmpty(uid))
            return "";

        if (!table.TryGetValue(uid, out string val))
        {
            // UID가 아닌 일반 문자열이면 그대로 반환 (하위 호환)
            if (!uid.StartsWith("str."))
                return uid;

            Debug.LogWarning("[Loc] 키 없음: " + uid);
            return uid; // 키 자체를 fallback으로 반환
        }

        if (args.Length > 0)
            return string.Format(val, args);
        return val;
    }

    /// <summary>
    /// 키가 존재하는지 확인
    /// </summary>
    public static bool HasKey(string uid)
    {
        if (!isLoaded) Load();
        return table.ContainsKey(uid);
    }

    // CSV 로드 내부 메서드
    private static void Load()
    {
        table.Clear();
        TextAsset csv = Resources.Load<TextAsset>("Data/StringTable");
        if (csv == null)
        {
            Debug.LogError("[Loc] StringTable.csv 없음! Resources/Data/StringTable.csv 확인 필요");
            return;
        }

        int colIdx = langIndex.ContainsKey(CurrentLang) ? langIndex[CurrentLang] : 1;

        string[] lines = csv.text.Split('\n');
        for (int i = 1; i < lines.Length; i++) // 0 = 헤더 스킵
        {
            string line = lines[i].Trim();
            if (string.IsNullOrEmpty(line)) continue;

            // CSV 파싱 (쉼표 분리, 큰따옴표 내 쉼표 처리)
            string[] cols = ParseCSVLine(line);
            if (cols.Length <= colIdx) continue;

            string uid = cols[0].Trim();
            string val = cols[colIdx].Trim();

            // 이스케이프 문자 처리
            val = val.Replace("\\n", "\n");

            if (!string.IsNullOrEmpty(uid))
                table[uid] = val;
        }
        isLoaded = true;
        Debug.Log($"[Loc] 로드 완료: {CurrentLang}, {table.Count}개 키");
    }

    /// <summary>
    /// CSV 한 줄 파싱 (큰따옴표 내 쉼표 대응)
    /// </summary>
    private static string[] ParseCSVLine(string line)
    {
        List<string> result = new List<string>();
        bool inQuotes = false;
        string current = "";

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];

            if (c == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    // 이스케이프된 큰따옴표 ""
                    current += '"';
                    i++; // 다음 " 스킵
                }
                else
                {
                    inQuotes = !inQuotes;
                }
            }
            else if (c == ',' && !inQuotes)
            {
                result.Add(current);
                current = "";
            }
            else
            {
                current += c;
            }
        }
        result.Add(current);
        return result.ToArray();
    }
}
