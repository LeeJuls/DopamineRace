/// <summary>
/// 캐릭터 컨디션 enum
/// 최상/상/중/하/최하 5단계
/// </summary>
public enum Condition
{
    Best,   // 최상
    Good,   // 상
    Normal, // 중
    Bad,    // 하
    Worst   // 최하
}

/// <summary>
/// 컨디션 유틸리티 (static)
/// </summary>
public static class ConditionHelper
{
    /// <summary>
    /// GameSettings 확률 기반으로 컨디션 랜덤 뽑기
    /// conditionRate_best + good + normal + bad + worst = 1.0
    /// </summary>
    public static Condition Roll()
    {
        var gs = GameSettings.Instance;
        float roll = UnityEngine.Random.value;
        float cumulative = 0f;

        cumulative += gs.conditionRate_best;
        if (roll < cumulative) return Condition.Best;

        cumulative += gs.conditionRate_good;
        if (roll < cumulative) return Condition.Good;

        cumulative += gs.conditionRate_normal;
        if (roll < cumulative) return Condition.Normal;

        cumulative += gs.conditionRate_bad;
        if (roll < cumulative) return Condition.Bad;

        return Condition.Worst;
    }

    /// <summary>컨디션 → 스탯 배수 반환</summary>
    public static float GetMultiplier(Condition condition)
    {
        var gs = GameSettings.Instance;
        switch (condition)
        {
            case Condition.Best:   return gs.conditionMul_best;
            case Condition.Good:   return gs.conditionMul_good;
            case Condition.Normal: return gs.conditionMul_normal;
            case Condition.Bad:    return gs.conditionMul_bad;
            case Condition.Worst:  return gs.conditionMul_worst;
            default: return 1.0f;
        }
    }

    /// <summary>컨디션 → 한국어 이름 (디버그/UI용)</summary>
    public static string GetDisplayName(Condition condition)
    {
        switch (condition)
        {
            case Condition.Best:   return "최상";
            case Condition.Good:   return "상";
            case Condition.Normal: return "중";
            case Condition.Bad:    return "하";
            case Condition.Worst:  return "최하";
            default: return "중";
        }
    }

    /// <summary>컨디션 → 디버그 컬러 (hex)</summary>
    public static string GetColorHex(Condition condition)
    {
        switch (condition)
        {
            case Condition.Best:   return "#FF44FF";  // 보라
            case Condition.Good:   return "#66FF66";  // 초록
            case Condition.Normal: return "#FFFFFF";  // 흰색
            case Condition.Bad:    return "#FFAA44";  // 주황
            case Condition.Worst:  return "#FF4444";  // 빨강
            default: return "#FFFFFF";
        }
    }

    /// <summary>컨디션 → Loc 키 (다국어 지원)</summary>
    public static string GetLocKey(Condition condition)
    {
        switch (condition)
        {
            case Condition.Best:   return "str.ui.condition.best";
            case Condition.Good:   return "str.ui.condition.good";
            case Condition.Normal: return "str.ui.condition.normal";
            case Condition.Bad:    return "str.ui.condition.bad";
            case Condition.Worst:  return "str.ui.condition.worst";
            default: return "str.ui.condition.normal";
        }
    }
}
