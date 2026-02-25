using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 캐릭터 타입 (경주 스타일)
/// </summary>
public enum CharacterType
{
    Runner,     // 도주: 초반 전력질주
    Leader,     // 선행: 앞쪽에서 안정적으로
    Chaser,     // 선입: 중반부터 치고 올라옴
    Reckoner,   // 추입: 후반 폭발
}

/// <summary>
/// 무기 위치 (충돌 시 애니메이션)
/// L → AttackSlash, R → AttackShoot, N → fallback
/// </summary>
public enum WeaponHand
{
    None,   // N
    Left,   // L → AttackSlash
    Right,  // R → AttackShoot
}

/// <summary>
/// 캐릭터 1명의 전체 데이터 (CSV 1행)
///
/// CSV 컬럼 순서:
///   0:char_id, 1:char_name, 2:speed, 3:power, 4:brave, 5:calm, 6:endurance, 7:luck,
///   8:type, 9:char_ability, 10:char_ability_time_sec,
///   11:char_resource_prefabs, 12:char_attack_resource_prefabs,
///   13:char_icon, 14:char_weapon, 15:char_skill_desc, 16:char_illustration
/// </summary>
[System.Serializable]
public class CharacterData
{
    public string charId;       // UID: "char.leader.thunder.000"
    public string charName;     // Loc 키: "str.char.000.name"
    public float charBaseSpeed;

    /// <summary>
    /// charBaseSpeed(1~20) → 실제 속도 배율
    /// 20 = 1.0배속(최대), 15 = 0.95, 10 = 0.90
    /// 공식: 0.8 + charBaseSpeed * 0.01
    /// </summary>
    public float SpeedMultiplier => 0.8f + charBaseSpeed * 0.01f;
    public float charBasePower;
    public float charBaseBrave;
    public float charBaseCalm;
    public float charBaseEndurance;
    public float charBaseLuck;
    public CharacterType charType;
    public string charAbility;
    public float charAbilityTimeSec;               // 스킬 지속 시간 (초)
    public string charResourcePrefabs;
    public string charAttackResourcePrefabs;       // 무기 든 프리팹 경로
    public string charIcon;
    public WeaponHand charWeapon;                  // 무기 위치 (L/R/N)
    public string charSkillDesc;                   // 스킬 설명 StringUID
    public string charIllustration;                // 일러스트 Resources 경로

    public SkillData skillData;                    // 파싱된 스킬 데이터

    public GameObject LoadPrefab()
    {
        if (string.IsNullOrEmpty(charResourcePrefabs)) return null;
        return LoadPrefabFromPath(charResourcePrefabs);
    }

    /// <summary>
    /// 무기 든 공격 프리팹 로드 (스킬 발동 시 사용)
    /// </summary>
    public GameObject LoadAttackPrefab()
    {
        if (string.IsNullOrEmpty(charAttackResourcePrefabs)) return null;
        return LoadPrefabFromPath(charAttackResourcePrefabs);
    }

    private GameObject LoadPrefabFromPath(string rawPath)
    {
        string path = rawPath.Replace('\\', '/');
        if (path.Contains("Resources/"))
            path = path.Substring(path.IndexOf("Resources/") + 10);
        if (path.EndsWith(".prefab"))
            path = path.Substring(0, path.Length - 7);
        return Resources.Load<GameObject>(path);
    }

    /// <summary>
    /// 일러스트 스프라이트 로드 (charIllustration 경로 사용, 없으면 LoadIcon() 대체)
    /// </summary>
    public Sprite LoadIllustration()
    {
        if (!string.IsNullOrEmpty(charIllustration))
        {
            string path = charIllustration.Replace('\\', '/');
            if (path.EndsWith(".png"))
                path = path.Substring(0, path.Length - 4);
            // Texture2D로 로드 → 전체 이미지를 Sprite로 생성 (spriteMode=Multiple 대응)
            Texture2D tex = Resources.Load<Texture2D>(path);
            if (tex != null)
                return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height),
                    new Vector2(0.5f, 0.5f));
        }
        return LoadIcon();
    }

    public bool HasIconFile()
    {
        if (string.IsNullOrEmpty(charIcon)) return false;
        string path = charIcon.Replace('\\', '/');
        if (path.EndsWith(".png"))
            path = path.Substring(0, path.Length - 4);
        return Resources.Load<Sprite>(path) != null;
    }

    public Sprite LoadIcon()
    {
        if (!string.IsNullOrEmpty(charIcon))
        {
            string path = charIcon.Replace('\\', '/');
            if (path.EndsWith(".png"))
                path = path.Substring(0, path.Length - 4);
            Sprite icon = Resources.Load<Sprite>(path);
            if (icon != null) return icon;
        }

        GameObject prefab = LoadPrefab();
        if (prefab != null)
        {
            SpriteRenderer sr = prefab.GetComponentInChildren<SpriteRenderer>();
            if (sr != null && sr.sprite != null)
                return sr.sprite;
        }

        return null;
    }

    /// <summary>
    /// CSV 1행 파싱
    ///
    /// 컬럼 순서:
    ///   0:char_id, 1:char_name, 2:speed, 3:power, 4:brave, 5:calm, 6:endurance, 7:luck,
    ///   8:type, 9:char_ability, 10:char_ability_time_sec,
    ///   11:char_resource_prefabs, 12:char_attack_resource_prefabs,
    ///   13:char_icon, 14:char_weapon, 15:char_skill_desc, 16:char_illustration
    /// </summary>
    public static CharacterData ParseCSVLine(string line)
    {
        char separator = line.Contains('\t') ? '\t' : ',';
        string[] cols = line.Split(separator);
        if (cols.Length < 12) return null;

        CharacterData d = new CharacterData();
        d.charId = cols[0].Trim();
        d.charName = cols[1].Trim();
        float.TryParse(cols[2].Trim(), out d.charBaseSpeed);
        float.TryParse(cols[3].Trim(), out d.charBasePower);
        float.TryParse(cols[4].Trim(), out d.charBaseBrave);
        float.TryParse(cols[5].Trim(), out d.charBaseCalm);
        float.TryParse(cols[6].Trim(), out d.charBaseEndurance);
        float.TryParse(cols[7].Trim(), out d.charBaseLuck);
        d.charType = ParseType(cols[8].Trim());
        d.charAbility = cols[9].Trim();

        // char_ability_time_sec (10번)
        d.charAbilityTimeSec = 5f; // 기본값
        if (cols.Length > 10)
            float.TryParse(cols[10].Trim(), out d.charAbilityTimeSec);

        // char_resource_prefabs (11번)
        d.charResourcePrefabs = cols.Length > 11 ? cols[11].Trim() : "";

        // char_attack_resource_prefabs (12번)
        d.charAttackResourcePrefabs = cols.Length > 12 ? cols[12].Trim() : "";

        // char_icon (13번)
        d.charIcon = cols.Length > 13 ? cols[13].Trim() : "";

        // char_weapon (14번)
        d.charWeapon = WeaponHand.None;
        if (cols.Length > 14)
            d.charWeapon = ParseWeapon(cols[14].Trim());

        // char_skill_desc (15번)
        d.charSkillDesc = cols.Length > 15 ? cols[15].Trim() : "";

        // char_illustration (16번)
        d.charIllustration = cols.Length > 16 ? cols[16].Trim() : "";

        // ★ 스킬 데이터 파싱
        d.skillData = SkillData.Parse(d.charAbility, d.charAbilityTimeSec);

        return d;
    }

    private static CharacterType ParseType(string s)
    {
        switch (s.ToLower())
        {
            case "runner":   return CharacterType.Runner;
            case "leader":   return CharacterType.Leader;
            case "chaser":   return CharacterType.Chaser;
            case "reckoner": return CharacterType.Reckoner;
            default:
                Debug.LogWarning("[CharacterData] 알 수 없는 타입: " + s + " → Runner로 설정");
                return CharacterType.Runner;
        }
    }

    private static WeaponHand ParseWeapon(string s)
    {
        switch (s.ToUpper())
        {
            case "L": return WeaponHand.Left;
            case "R": return WeaponHand.Right;
            default:  return WeaponHand.None;
        }
    }

    /// <summary>
    /// 로컬라이즈된 표시 이름 (charName이 UID면 번역, 아니면 그대로)
    /// </summary>
    public string DisplayName => Loc.Get(charName);

    public string GetTypeName()
    {
        switch (charType)
        {
            case CharacterType.Runner:   return Loc.Get("str.chartype.runner");
            case CharacterType.Leader:   return Loc.Get("str.chartype.leader");
            case CharacterType.Chaser:   return Loc.Get("str.chartype.chaser");
            case CharacterType.Reckoner: return Loc.Get("str.chartype.reckoner");
            default: return "???";
        }
    }

    public Color GetTypeColor()
    {
        switch (charType)
        {
            case CharacterType.Runner:   return new Color(1f, 0.4f, 0.4f);
            case CharacterType.Leader:   return new Color(0.4f, 0.7f, 1f);
            case CharacterType.Chaser:   return new Color(0.4f, 0.9f, 0.4f);
            case CharacterType.Reckoner: return new Color(1f, 0.8f, 0.3f);
            default: return Color.white;
        }
    }
}
