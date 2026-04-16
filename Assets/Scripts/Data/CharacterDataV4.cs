using UnityEngine;

/// <summary>
/// V4 캐릭터 데이터 — 5대 스탯(Speed/Accel/Stamina/Power/Intelligence) + Luck
///
/// CharacterDB_V4.csv 컬럼 순서:
///   0:char_id, 1:char_name, 2:char_name_kr
///   3:v4_speed, 4:v4_accel, 5:v4_stamina, 6:v4_power, 7:v4_intelligence, 8:v4_luck
///   9:char_type
///   10:char_ability, 11:char_ability_time_sec
///   12:char_resource_prefabs, 13:char_attack_resource_prefabs
///   14:char_icon, 15:char_weapon
///   16:char_skill_desc, 17:char_illustration
/// </summary>
[System.Serializable]
public class CharacterDataV4
{
    // ─── 식별 ───
    public string charId;       // UID: "char.leader.thunder.000"
    public string charName;     // Loc 키: "str.char.000.name"
    public CharacterType charType;

    // ─── V4 5대 스탯 + Luck (1~20) ───
    public float v4Speed;           // 최고속도 (Vmax 상한)
    public float v4Accel;           // 가속도 (스퍼트 폭발력, 마군 돌파)
    public float v4Stamina;         // 스태미나 (레이스 자원 HP)
    public float v4Power;           // 파워 (충돌 돌파, 코너 인코스)
    public float v4Intelligence;    // 지능 (AI 판단력, 오버페이스 방지)
    public float v4Luck;            // 럭 (크리, 회피)

    // ─── 공용 메타 ───
    public string charAbility;
    public float charAbilityTimeSec;
    public string charResourcePrefabs;
    public string charAttackResourcePrefabs;
    public string charIcon;
    public WeaponHand charWeapon;
    public string charSkillDesc;
    public string charIllustration;
    public SkillData skillData;

    // ─── 패시브 스킬 (col 19) ───
    public string charPassive;          // 원본 문자열 (예: "P_LastRank:HpHeal:0.10:30")
    public PassiveSkillData passiveData; // 파싱 결과

    // ─── Computed Properties ───

    /// <summary>표시 이름 (로컬라이즈)</summary>
    public string DisplayName => Loc.Get(charName);

    /// <summary>스탯 합계</summary>
    public float StatTotal => v4Speed + v4Accel + v4Stamina + v4Power + v4Intelligence + v4Luck;

    // ─── CSV 파싱 ───

    /// <summary>
    /// CharacterDB_V4.csv 1행 파싱
    /// </summary>
    public static CharacterDataV4 ParseCSVLine(string line)
    {
        char separator = line.Contains('\t') ? '\t' : ',';
        string[] cols = line.Split(separator);
        if (cols.Length < 10) return null;

        CharacterDataV4 d = new CharacterDataV4();
        d.charId   = cols[0].Trim();
        d.charName = cols[1].Trim();
        // cols[2] = char_name_kr (한글 이름, 읽기 편의용)

        float.TryParse(cols[3].Trim(), out d.v4Speed);
        float.TryParse(cols[4].Trim(), out d.v4Accel);
        float.TryParse(cols[5].Trim(), out d.v4Stamina);
        float.TryParse(cols[6].Trim(), out d.v4Power);
        float.TryParse(cols[7].Trim(), out d.v4Intelligence);
        float.TryParse(cols[8].Trim(), out d.v4Luck);

        d.charType = ParseType(cols[9].Trim());
        d.charAbility = cols.Length > 10 ? cols[10].Trim() : "";

        d.charAbilityTimeSec = 5f;
        if (cols.Length > 11) float.TryParse(cols[11].Trim(), out d.charAbilityTimeSec);

        d.charResourcePrefabs       = cols.Length > 12 ? cols[12].Trim() : "";
        d.charAttackResourcePrefabs = cols.Length > 13 ? cols[13].Trim() : "";
        d.charIcon                  = cols.Length > 14 ? cols[14].Trim() : "";

        d.charWeapon = WeaponHand.None;
        if (cols.Length > 15) d.charWeapon = ParseWeapon(cols[15].Trim());

        d.charSkillDesc    = cols.Length > 16 ? cols[16].Trim() : "";
        d.charIllustration = cols.Length > 17 ? cols[17].Trim() : "";
        // col 18 = char_appearance_rate (기존 위치 유지, CharacterDatabase에서 파싱)
        // col 19 = char_passive (신규, 없으면 None으로 폴백)
        d.charPassive  = cols.Length > 19 ? cols[19].Trim() : "";

        d.skillData   = SkillData.Parse(d.charAbility, d.charAbilityTimeSec);
        d.passiveData = PassiveSkillData.Parse(d.charPassive);
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
                Debug.LogWarning("[CharacterDataV4] 알 수 없는 타입: " + s + " → Runner로 설정");
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

    public Sprite LoadIcon()
    {
        if (!string.IsNullOrEmpty(charIcon))
        {
            string path = charIcon.Replace('\\', '/');
            if (path.EndsWith(".png")) path = path.Substring(0, path.Length - 4);
            Sprite icon = Resources.Load<Sprite>(path);
            if (icon != null) return icon;
        }
        return null;
    }

    public Sprite LoadIllustration()
    {
        if (!string.IsNullOrEmpty(charIllustration))
        {
            string path = charIllustration.Replace('\\', '/');
            if (path.EndsWith(".png")) path = path.Substring(0, path.Length - 4);
            Texture2D tex = Resources.Load<Texture2D>(path);
            if (tex != null)
                return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
        }
        return LoadIcon();
    }

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
