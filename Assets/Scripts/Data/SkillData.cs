using UnityEngine;

/// <summary>
/// 스킬 발동 조건 타입
/// CSV char_ability 컬럼에서 ':' 앞부분
/// </summary>
public enum SkillTriggerType
{
    None,               // 스킬 없음
    E_Skill_Collision,  // 충돌 N회 시 발동
    E_Skill_Lap,        // N랩 완주 직후 발동
    E_Skill_HP,         // HP N% 이하 도달 시 1회 발동
    E_Skill_Rank,       // N등 이하가 되는 순간 1회 발동
}

/// <summary>
/// 스킬 발동 효과 타입
/// CSV char_ability 3번째 세그먼트
/// </summary>
public enum SkillEffectType
{
    CollisionWin,   // 기본값: skillActive 중 충돌 무조건 승리
    SpeedBoost,     // 속도 outputSpeed 배율 (effectValue = 배율, 상한 1.15)
    HpHeal,         // 즉발 HP 회복 (effectValue = 비율, 상한 0.15)
    DrainReduce,    // HP 드레인 배율 감소 (effectValue = 배율, 하한 0.55)
}

/// <summary>
/// 캐릭터 스킬 데이터 (CSV에서 파싱)
///
/// CSV 형식:
///   기존: "E_Skill_Collision:5"               → CollisionWin 기본값
///   신규: "E_Skill_Collision:5:CollisionWin"   → 명시적
///         "E_Skill_Collision:5:SpeedBoost:1.12:5"
///         "E_Skill_Lap:2:HpHeal:0.12:0"
///         "E_Skill_HP:35:DrainReduce:0.58:8"
///         "E_Skill_Rank:8:SpeedBoost:1.10:5"
///
///   세그먼트: [triggerType]:[triggerValue]:[effectType]:[effectValue]:[durationSec]
///   effectType/effectValue/durationSec 생략 시 기존 폴백 동작 유지
/// </summary>
[System.Serializable]
public class SkillData
{
    public SkillTriggerType triggerType;   // 발동 조건 타입
    public int triggerValue;               // 조건 값 (예: 충돌 5회)
    public float durationSec;             // 발동 지속 시간 (초) — HpHeal은 0 허용
    public SkillEffectType effectType;    // 발동 효과 타입
    public float effectValue;             // 효과 값 (SpeedBoost: 배율, HpHeal/DrainReduce: 비율)

    // 안전 상한/하한 (balance 에이전트 권장)
    public const float SPEED_BOOST_MAX  = 1.15f;
    public const float HP_HEAL_MAX      = 0.15f;
    public const float DRAIN_REDUCE_MIN = 0.55f;

    /// <summary>
    /// CSV char_ability 문자열 파싱
    /// "E_Skill_Collision:5" → 기존 동작 유지 (CollisionWin 기본값)
    /// "none" 또는 빈 값 → triggerType=None
    /// </summary>
    public static SkillData Parse(string abilityStr, float fallbackDurationSec)
    {
        SkillData data = new SkillData();
        data.triggerType  = SkillTriggerType.None;
        data.triggerValue = 0;
        data.durationSec  = 0f;
        data.effectType   = SkillEffectType.CollisionWin;
        data.effectValue  = 0f;

        if (string.IsNullOrEmpty(abilityStr) || abilityStr.ToLower() == "none")
            return data;

        // 세그먼트 분리: "E_Skill_Collision:5:SpeedBoost:1.12:5"
        string[] parts = abilityStr.Split(':');
        string typeName = parts[0].Trim();

        // triggerType 파싱
        try
        {
            data.triggerType = (SkillTriggerType)System.Enum.Parse(typeof(SkillTriggerType), typeName, true);
        }
        catch
        {
            Debug.LogError(string.Format("[SkillData] 알 수 없는 스킬 타입: \"{0}\" → 스킬 비활성", abilityStr));
            data.triggerType = SkillTriggerType.None;
            return data;
        }

        // triggerValue 파싱 (세그먼트 1)
        if (parts.Length > 1)
            int.TryParse(parts[1].Trim(), out data.triggerValue);

        // triggerValue 유효성 — E_Skill_HP/Rank는 0도 허용하지 않지만 양수 필수
        if (data.triggerValue <= 0)
        {
            Debug.LogError(string.Format("[SkillData] 스킬 조건 값이 잘못됨: \"{0}\" (값: {1}) → 스킬 비활성",
                abilityStr, data.triggerValue));
            data.triggerType = SkillTriggerType.None;
            return data;
        }

        // effectType 파싱 (세그먼트 2, 생략 시 CollisionWin 기본값)
        if (parts.Length > 2 && !string.IsNullOrEmpty(parts[2].Trim()))
        {
            string effectName = parts[2].Trim();
            try
            {
                data.effectType = (SkillEffectType)System.Enum.Parse(typeof(SkillEffectType), effectName, true);
            }
            catch
            {
                Debug.LogWarning(string.Format("[SkillData] 알 수 없는 효과 타입: \"{0}\" → CollisionWin 기본값", effectName));
                data.effectType = SkillEffectType.CollisionWin;
            }
        }

        // effectValue 파싱 (세그먼트 3)
        if (parts.Length > 3)
            float.TryParse(parts[3].Trim(), System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out data.effectValue);

        // durationSec 파싱 (세그먼트 4, 생략 시 fallbackDurationSec)
        if (parts.Length > 4)
        {
            float parsedDur;
            if (float.TryParse(parts[4].Trim(), System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out parsedDur))
                data.durationSec = parsedDur;
            else
                data.durationSec = fallbackDurationSec;
        }
        else
        {
            data.durationSec = fallbackDurationSec;
        }

        // HpHeal은 duration=0 허용 (즉발)
        bool durationRequired = data.effectType != SkillEffectType.HpHeal;
        if (durationRequired && data.durationSec <= 0f)
        {
            Debug.LogError(string.Format("[SkillData] 지속 효과에 duration=0: \"{0}\" → 스킬 비활성", abilityStr));
            data.triggerType = SkillTriggerType.None;
            return data;
        }

        // effectValue 안전 클램핑
        switch (data.effectType)
        {
            case SkillEffectType.SpeedBoost:
                data.effectValue = Mathf.Clamp(data.effectValue, 1.0f, SPEED_BOOST_MAX);
                break;
            case SkillEffectType.HpHeal:
                data.effectValue = Mathf.Clamp(data.effectValue, 0.01f, HP_HEAL_MAX);
                break;
            case SkillEffectType.DrainReduce:
                data.effectValue = Mathf.Clamp(data.effectValue, DRAIN_REDUCE_MIN, 1.0f);
                break;
        }

        return data;
    }

    /// <summary>
    /// 충돌 기반 스킬 발동 조건 체크
    /// </summary>
    public bool CheckCollisionTrigger(int collisionCount)
    {
        if (triggerType != SkillTriggerType.E_Skill_Collision) return false;
        if (triggerValue <= 0) return false;
        return collisionCount >= triggerValue;
    }

    /// <summary>
    /// 랩 완주 기반 스킬 발동 조건 체크
    /// </summary>
    public bool CheckLapTrigger(int completedLaps)
    {
        if (triggerType != SkillTriggerType.E_Skill_Lap) return false;
        if (triggerValue <= 0) return false;
        return completedLaps >= triggerValue;
    }

    /// <summary>
    /// HP 이하 기반 스킬 발동 조건 체크 (퍼센트, 예: triggerValue=35 → 35% 이하)
    /// </summary>
    public bool CheckHpTrigger(float hpRatio)
    {
        if (triggerType != SkillTriggerType.E_Skill_HP) return false;
        if (triggerValue <= 0) return false;
        return hpRatio <= triggerValue / 100f;
    }

    /// <summary>
    /// 순위 기반 스킬 발동 조건 체크 (triggerValue 등 이하)
    /// </summary>
    public bool CheckRankTrigger(int currentRank)
    {
        if (triggerType != SkillTriggerType.E_Skill_Rank) return false;
        if (triggerValue <= 0) return false;
        return currentRank >= triggerValue;
    }

    public override string ToString()
    {
        if (triggerType == SkillTriggerType.None) return "None";
        return string.Format("{0}:{1} → {2}:{3} ({4}초)",
            triggerType, triggerValue, effectType, effectValue, durationSec);
    }
}
