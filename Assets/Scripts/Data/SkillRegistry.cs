using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 스킬 카탈로그 레지스트리 — SkillDB_V4.csv를 로드해 skillKey로 조회.
/// CharacterDB_V4.csv는 skillKey만 저장하고 이 레지스트리에서 실제 SkillData/PassiveSkillData를 얻음.
/// </summary>
public static class SkillRegistry
{
    public class Entry
    {
        public string skillKey;
        public string category;     // "active" / "passive" / "none"
        public string abilityStr;   // SkillData/PassiveSkillData.Parse 입력 포맷
        public string nameLocKey;
        public string descLocKey;
        public string iconPath;     // Resources 상대 경로 (예: "Icon/sword", "Icon/book_icon")

        // 파싱 결과 캐시 (lazy)
        public SkillData activeCache;
        public PassiveSkillData passiveCache;
        public Sprite iconCache;    // Sprite 로드 캐시
    }

    private static Dictionary<string, Entry> _db;

    /// <summary>레지스트리 로드 + 파싱 캐싱.</summary>
    private static void Load()
    {
        _db = new Dictionary<string, Entry>();

        var csv = Resources.Load<TextAsset>("Data/SkillDB_V4");
        if (csv == null)
        {
            Debug.LogWarning("[SkillRegistry] SkillDB_V4.csv 로드 실패 — 빈 DB로 시작");
            return;
        }

        string[] lines = csv.text.Split('\n');
        for (int i = 1; i < lines.Length; i++)
        {
            string line = lines[i].Trim();
            if (string.IsNullOrEmpty(line)) continue;
            string[] cols = line.Split(',');
            if (cols.Length < 3) continue;

            var e = new Entry
            {
                skillKey    = cols[0].Trim(),
                category    = cols.Length > 1 ? cols[1].Trim().ToLower() : "none",
                abilityStr  = cols.Length > 2 ? cols[2].Trim() : "",
                nameLocKey  = cols.Length > 3 ? cols[3].Trim() : "",
                descLocKey  = cols.Length > 4 ? cols[4].Trim() : "",
                iconPath    = cols.Length > 5 ? cols[5].Trim() : "",
            };
            if (string.IsNullOrEmpty(e.skillKey)) continue;
            if (_db.ContainsKey(e.skillKey))
            {
                Debug.LogWarning($"[SkillRegistry] 중복 skillKey: {e.skillKey} — 첫 정의 유지");
                continue;
            }

            // 파싱 캐시
            if (e.category == "active" && !string.IsNullOrEmpty(e.abilityStr))
            {
                e.activeCache = SkillData.Parse(e.abilityStr, 5f);
            }
            else if (e.category == "passive" && !string.IsNullOrEmpty(e.abilityStr))
            {
                e.passiveCache = PassiveSkillData.Parse(e.abilityStr);
            }

            _db[e.skillKey] = e;
        }
        Debug.Log($"[SkillRegistry] {_db.Count}개 스킬 로드 완료");
    }

    private static void EnsureLoaded()
    {
        if (_db == null) Load();
    }

    /// <summary>skillKey로 Entry 조회 (없으면 null).</summary>
    public static Entry Find(string skillKey)
    {
        EnsureLoaded();
        if (string.IsNullOrEmpty(skillKey)) return null;
        _db.TryGetValue(skillKey, out var e);
        return e;
    }

    /// <summary>액티브 SkillData 조회 — 실패 시 기본 None 반환.</summary>
    public static SkillData GetActive(string skillKey)
    {
        var e = Find(skillKey);
        if (e == null || e.activeCache == null)
        {
            var empty = new SkillData();
            empty.triggerType = SkillTriggerType.None;
            return empty;
        }
        return e.activeCache;
    }

    /// <summary>패시브 PassiveSkillData 조회 — 실패 시 기본 None 반환.</summary>
    public static PassiveSkillData GetPassive(string skillKey)
    {
        var e = Find(skillKey);
        if (e == null || e.passiveCache == null)
        {
            var empty = new PassiveSkillData();
            empty.triggerType = PassiveTriggerType.None;
            return empty;
        }
        return e.passiveCache;
    }

    /// <summary>스킬 표시 이름 (Loc).</summary>
    public static string GetDisplayName(string skillKey)
    {
        var e = Find(skillKey);
        if (e == null || string.IsNullOrEmpty(e.nameLocKey)) return skillKey ?? "";
        return Loc.Get(e.nameLocKey);
    }

    /// <summary>스킬 설명 (Loc).</summary>
    public static string GetDescription(string skillKey)
    {
        var e = Find(skillKey);
        if (e == null || string.IsNullOrEmpty(e.descLocKey)) return "";
        return Loc.Get(e.descLocKey);
    }

    /// <summary>스킬 아이콘 Sprite 로드 (Resources). 실패 시 null.</summary>
    public static Sprite GetIcon(string skillKey)
    {
        var e = Find(skillKey);
        if (e == null || string.IsNullOrEmpty(e.iconPath)) return null;
        if (e.iconCache != null) return e.iconCache;
        e.iconCache = Resources.Load<Sprite>(e.iconPath);
        if (e.iconCache == null)
            Debug.LogWarning($"[SkillRegistry] 아이콘 로드 실패: {e.iconPath} (skillKey={skillKey})");
        return e.iconCache;
    }

    /// <summary>디버그용: 등록된 모든 스킬 키 반환.</summary>
    public static IEnumerable<string> AllKeys()
    {
        EnsureLoaded();
        return _db.Keys;
    }

    /// <summary>테스트/CSV 리로드용 — 다음 호출에서 재로드.</summary>
    public static void Reset()
    {
        _db = null;
    }
}
