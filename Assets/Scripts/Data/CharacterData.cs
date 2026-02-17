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
/// </summary>
[System.Serializable]
public class CharacterData
{
    public string charName;
    public float charBaseSpeed;
    public float charBasePower;
    public float charBaseBrave;
    public float charBaseCalm;
    public float charBaseEndurance;
    public float charBaseLuck;
    public CharacterType charType;
    public string charAbility;
    public string charResourcePrefabs;
    public string charIcon;
    public WeaponHand charWeapon;     // ★ 무기 위치 (L/R/N)

    public GameObject LoadPrefab()
    {
        if (string.IsNullOrEmpty(charResourcePrefabs)) return null;
        string path = charResourcePrefabs.Replace('\\', '/');
        if (path.Contains("Resources/"))
            path = path.Substring(path.IndexOf("Resources/") + 10);
        if (path.EndsWith(".prefab"))
            path = path.Substring(0, path.Length - 7);
        return Resources.Load<GameObject>(path);
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
    /// </summary>
    public static CharacterData ParseCSVLine(string line)
    {
        char separator = line.Contains('\t') ? '\t' : ',';
        string[] cols = line.Split(separator);
        if (cols.Length < 11) return null;

        CharacterData d = new CharacterData();
        d.charName = cols[0].Trim();
        float.TryParse(cols[1].Trim(), out d.charBaseSpeed);
        float.TryParse(cols[2].Trim(), out d.charBasePower);
        float.TryParse(cols[3].Trim(), out d.charBaseBrave);
        float.TryParse(cols[4].Trim(), out d.charBaseCalm);
        float.TryParse(cols[5].Trim(), out d.charBaseEndurance);
        float.TryParse(cols[6].Trim(), out d.charBaseLuck);
        d.charType = ParseType(cols[7].Trim());
        d.charAbility = cols[8].Trim();
        d.charResourcePrefabs = cols[9].Trim();
        d.charIcon = cols.Length > 10 ? cols[10].Trim() : "";

        // ★ 무기 위치 (12번째 컬럼, 없으면 N)
        d.charWeapon = WeaponHand.None;
        if (cols.Length > 11)
            d.charWeapon = ParseWeapon(cols[11].Trim());

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

    public string GetTypeName()
    {
        switch (charType)
        {
            case CharacterType.Runner:   return "도주";
            case CharacterType.Leader:   return "선행";
            case CharacterType.Chaser:   return "선입";
            case CharacterType.Reckoner: return "추입";
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