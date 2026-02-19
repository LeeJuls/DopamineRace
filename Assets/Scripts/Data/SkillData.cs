using UnityEngine;

/// <summary>
/// 스킬 발동 조건 타입
/// CSV char_ability 컬럼에서 ':' 앞부분
/// </summary>
public enum SkillTriggerType
{
    None,               // 스킬 없음
    E_Skill_Collision,  // 충돌 N회 시 발동
    // 추후 확장 예시:
    // E_Skill_Lap,     // N랩 완주 시 발동
    // E_Skill_Time,    // N초 경과 시 발동
    // E_Skill_Rank,    // N등 이하일 때 발동
}

/// <summary>
/// 캐릭터 스킬 데이터 (CSV에서 파싱)
/// 스킬 발동 시 무기를 꺼내들고 공격 상태로 전환
/// 
/// CSV 형식 예시:
///   char_ability = "E_Skill_Collision:5"
///   char_ability_time_sec = 5
/// </summary>
[System.Serializable]
public class SkillData
{
    public SkillTriggerType triggerType;   // 발동 조건 타입
    public int triggerValue;               // 조건 값 (예: 충돌 5회)
    public float durationSec;              // 발동 지속 시간 (초)

    /// <summary>
    /// CSV char_ability 문자열 파싱
    /// "E_Skill_Collision:5" → triggerType=E_Skill_Collision, triggerValue=5
    /// "none" 또는 빈 값 → triggerType=None
    /// </summary>
    public static SkillData Parse(string abilityStr, float durationSec)
    {
        SkillData data = new SkillData();
        data.triggerType = SkillTriggerType.None;
        data.triggerValue = 0;
        data.durationSec = 0f;

        // 빈 값 또는 "none"
        if (string.IsNullOrEmpty(abilityStr) || abilityStr.ToLower() == "none")
        {
            return data;
        }

        // "E_Skill_Collision:5" → ["E_Skill_Collision", "5"]
        string[] parts = abilityStr.Split(':');
        string typeName = parts[0].Trim();

        // Enum 파싱 시도
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

        // 값 파싱
        if (parts.Length > 1)
        {
            int.TryParse(parts[1].Trim(), out data.triggerValue);
        }

        // triggerValue 유효성 검증
        if (data.triggerValue <= 0)
        {
            Debug.LogError(string.Format("[SkillData] 스킬 조건 값이 잘못됨: \"{0}\" (값: {1}) → 스킬 비활성",
                abilityStr, data.triggerValue));
            data.triggerType = SkillTriggerType.None;
            return data;
        }

        // durationSec 유효성 검증
        if (durationSec <= 0f)
        {
            Debug.LogError(string.Format("[SkillData] char_ability_time_sec 값이 잘못됨: {0}초 (스킬: \"{1}\") → 스킬 비활성",
                durationSec, abilityStr));
            data.triggerType = SkillTriggerType.None;
            return data;
        }

        data.durationSec = durationSec;
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

    public override string ToString()
    {
        if (triggerType == SkillTriggerType.None) return "None";
        return string.Format("{0}:{1} ({2}초)", triggerType, triggerValue, durationSec);
    }
}
