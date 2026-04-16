using UnityEngine;

/// <summary>
/// 패시브 스킬 발동 조건 타입
/// CSV char_passive 컬럼 P_ 이후 첫 세그먼트
/// </summary>
public enum PassiveTriggerType
{
    None,       // 패시브 없음
    LastRank,   // 현재 꼴지(최하위) 조건
    LowHP,      // HP N% 이하 조건 (지속)
    BurstZone,  // 타입별 부스트 구간 중 조건
    Spurt,      // 최종 스퍼트 구간(80%~) 중 조건
    Always,     // 항상 적용
    TopRank,    // N등 이내 유지 중 조건
}

/// <summary>
/// 패시브 스킬 효과 타입
/// CSV char_passive 컬럼 효과 세그먼트
/// </summary>
public enum PassiveEffectType
{
    None,
    HpHeal,             // HP N% 즉발 회복 (쿨다운 필수)
    SpeedBonus,         // outputSpeed 배율 (조건 중 지속, 상한 1.15)
    DrainReduce,        // 드레인 배율 감소 (조건 중 지속, 하한 0.55)
    SlipstreamRange,    // 슬립스트림 감지 범위 배율 (조건 중 지속, 상한 1.60)
    CpRegen,            // CP 즉발 충전 (쿨다운 필수)
}

/// <summary>
/// 캐릭터 패시브 스킬 데이터 (CSV char_passive col 19에서 파싱)
///
/// CSV 형식:
///   P_LastRank:HpHeal:0.10:30         ← 꼴지 시 HP 10% 회복, 30초 쿨다운
///   P_LowHP:25:SpeedBonus:1.10        ← HP 25% 이하 시 속도 +10% (지속)
///   P_BurstZone:DrainReduce:0.75      ← 부스트 구간 드레인 25% 감소 (지속)
///   P_Spurt:SlipstreamRange:1.50      ← 스퍼트 구간 슬립스트림 범위 1.5배 (지속)
///   P_Always:CpRegen:0.08:20          ← 20초마다 CP 8% 재생
///   P_TopRank:3:SpeedBonus:1.08       ← 3등 이내 유지 시 속도 +8% (지속)
///
/// 세그먼트 구조:
///   조건 없는 트리거:  P_LastRank/BurstZone/Spurt/Always → [triggerType]:[effectType]:[effectValue]:[cooldown?]
///   조건 있는 트리거:  P_LowHP/TopRank                  → [triggerType]:[triggerValue]:[effectType]:[effectValue]:[cooldown?]
/// </summary>
[System.Serializable]
public class PassiveSkillData
{
    public PassiveTriggerType triggerType;
    public float triggerValue;      // LowHP:25 → 25 (%), TopRank:3 → 3
    public PassiveEffectType effectType;
    public float effectValue;       // SpeedBonus:1.10 → 1.10
    public float cooldownSec;       // 즉발 효과(HpHeal/CpRegen) 재발동 쿨다운

    // 안전 상한/하한
    public const float SPEED_BONUS_MAX       = 1.15f;
    public const float DRAIN_REDUCE_MIN      = 0.55f;
    public const float SLIPSTREAM_RANGE_MAX  = 1.60f;
    public const float HP_HEAL_MAX           = 0.15f;
    public const float COOLDOWN_MIN          = 20f;

    /// <summary>
    /// CSV char_passive 문자열 파싱
    /// "none" 또는 빈 값 → triggerType=None
    /// </summary>
    public static PassiveSkillData Parse(string passiveStr)
    {
        PassiveSkillData data = new PassiveSkillData();
        data.triggerType  = PassiveTriggerType.None;
        data.triggerValue = 0f;
        data.effectType   = PassiveEffectType.None;
        data.effectValue  = 0f;
        data.cooldownSec  = 0f;

        if (string.IsNullOrEmpty(passiveStr) || passiveStr.ToLower() == "none")
            return data;

        // "P_LowHP:25:SpeedBonus:1.10" → ["P_LowHP", "25", "SpeedBonus", "1.10"]
        string[] parts = passiveStr.Split(':');
        if (parts.Length < 1) return data;

        // triggerType 파싱 (P_ 접두사 제거)
        string rawType = parts[0].Trim();
        if (rawType.StartsWith("P_", System.StringComparison.OrdinalIgnoreCase))
            rawType = rawType.Substring(2);

        try
        {
            data.triggerType = (PassiveTriggerType)System.Enum.Parse(typeof(PassiveTriggerType), rawType, true);
        }
        catch
        {
            Debug.LogError(string.Format("[PassiveSkillData] 알 수 없는 패시브 타입: \"{0}\" → 패시브 비활성", passiveStr));
            data.triggerType = PassiveTriggerType.None;
            return data;
        }

        // 조건값이 필요한 트리거인지 판별
        bool hasTriggerValue = data.triggerType == PassiveTriggerType.LowHP
                            || data.triggerType == PassiveTriggerType.TopRank;

        int effectIdx    = hasTriggerValue ? 2 : 1;
        int effectValIdx = hasTriggerValue ? 3 : 2;
        int cooldownIdx  = hasTriggerValue ? 4 : 3;

        // triggerValue 파싱 (LowHP/TopRank)
        if (hasTriggerValue && parts.Length > 1)
            float.TryParse(parts[1].Trim(), System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out data.triggerValue);

        // effectType 파싱
        if (parts.Length > effectIdx && !string.IsNullOrEmpty(parts[effectIdx].Trim()))
        {
            try
            {
                data.effectType = (PassiveEffectType)System.Enum.Parse(
                    typeof(PassiveEffectType), parts[effectIdx].Trim(), true);
            }
            catch
            {
                Debug.LogWarning(string.Format("[PassiveSkillData] 알 수 없는 효과 타입: \"{0}\" → 패시브 비활성", parts[effectIdx]));
                data.triggerType = PassiveTriggerType.None;
                return data;
            }
        }

        // effectValue 파싱
        if (parts.Length > effectValIdx)
            float.TryParse(parts[effectValIdx].Trim(), System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out data.effectValue);

        // cooldownSec 파싱 (즉발 효과만)
        if (parts.Length > cooldownIdx)
            float.TryParse(parts[cooldownIdx].Trim(), System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out data.cooldownSec);

        // 즉발 효과 쿨다운 최소값 강제
        bool isInstant = data.effectType == PassiveEffectType.HpHeal
                      || data.effectType == PassiveEffectType.CpRegen;
        if (isInstant && data.cooldownSec < COOLDOWN_MIN)
        {
            Debug.LogWarning(string.Format(
                "[PassiveSkillData] 즉발 효과 쿨다운 {0}초 → 최소 {1}초로 조정: \"{2}\"",
                data.cooldownSec, COOLDOWN_MIN, passiveStr));
            data.cooldownSec = COOLDOWN_MIN;
        }

        // effectValue 안전 클램핑
        switch (data.effectType)
        {
            case PassiveEffectType.SpeedBonus:
                data.effectValue = Mathf.Clamp(data.effectValue, 1.0f, SPEED_BONUS_MAX);
                break;
            case PassiveEffectType.DrainReduce:
                data.effectValue = Mathf.Clamp(data.effectValue, DRAIN_REDUCE_MIN, 1.0f);
                break;
            case PassiveEffectType.SlipstreamRange:
                data.effectValue = Mathf.Clamp(data.effectValue, 1.0f, SLIPSTREAM_RANGE_MAX);
                break;
            case PassiveEffectType.HpHeal:
                data.effectValue = Mathf.Clamp(data.effectValue, 0.01f, HP_HEAL_MAX);
                break;
            case PassiveEffectType.CpRegen:
                data.effectValue = Mathf.Clamp(data.effectValue, 0.01f, 0.30f);
                break;
        }

        return data;
    }

    /// <summary>
    /// 패시브 발동 조건 체크 (RacerController_V4.CheckPassiveSkill에서 ThinkTick마다 호출)
    /// </summary>
    /// <param name="totalRacers">전체 레이서 수</param>
    /// <param name="currentRank">현재 순위 (1-based)</param>
    /// <param name="hpRatio">현재 HP 비율 (0~1)</param>
    /// <param name="inBurstZone">타입별 부스트 구간 여부</param>
    /// <param name="inSpurtZone">최종 스퍼트 구간 여부</param>
    public bool CheckCondition(int totalRacers, int currentRank,
                               float hpRatio, bool inBurstZone, bool inSpurtZone)
    {
        switch (triggerType)
        {
            case PassiveTriggerType.LastRank:
                return currentRank > 0 && currentRank >= totalRacers;
            case PassiveTriggerType.LowHP:
                return hpRatio <= triggerValue / 100f;
            case PassiveTriggerType.BurstZone:
                return inBurstZone;
            case PassiveTriggerType.Spurt:
                return inSpurtZone;
            case PassiveTriggerType.Always:
                return true;
            case PassiveTriggerType.TopRank:
                return currentRank > 0 && currentRank <= (int)triggerValue;
            default:
                return false;
        }
    }

    public override string ToString()
    {
        if (triggerType == PassiveTriggerType.None) return "None";
        string trigger = triggerValue > 0
            ? string.Format("{0}:{1}", triggerType, triggerValue)
            : triggerType.ToString();
        return string.Format("P_{0} → {1}:{2} (쿨:{3}초)", trigger, effectType, effectValue, cooldownSec);
    }
}
