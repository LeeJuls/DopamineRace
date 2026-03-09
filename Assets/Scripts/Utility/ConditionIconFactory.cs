using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 컨디션 아이콘(화살표 Sprite) — Resources/UI/Img_Arr_01~05.png 로드.
/// Best(01) → Good(02) → Normal(03) → Bad(04) → Worst(05)
///
/// 사용법: ConditionIconFactory.GetIcon(Condition.Best)
/// </summary>
public static class ConditionIconFactory
{
    private static Dictionary<Condition, Sprite> cache;

    /// <summary>
    /// 컨디션에 맞는 화살표 Sprite 반환 (캐시).
    /// </summary>
    public static Sprite GetIcon(Condition condition)
    {
        if (cache == null)
            cache = new Dictionary<Condition, Sprite>();

        if (cache.TryGetValue(condition, out Sprite cached) && cached != null)
            return cached;

        string resourcePath = condition switch
        {
            Condition.Best   => "UI/Img_Arr_01",
            Condition.Good   => "UI/Img_Arr_02",
            Condition.Normal => "UI/Img_Arr_03",
            Condition.Bad    => "UI/Img_Arr_04",
            Condition.Worst  => "UI/Img_Arr_05",
            _                => "UI/Img_Arr_03",
        };

        Sprite sprite = Resources.Load<Sprite>(resourcePath);
        if (sprite == null)
            Debug.LogWarning($"[ConditionIconFactory] 스프라이트 없음: {resourcePath}");

        cache[condition] = sprite;
        return sprite;
    }

    /// <summary>
    /// 캐시 초기화 (씬 전환 등에서 필요 시 호출)
    /// </summary>
    public static void ClearCache()
    {
        cache?.Clear();
    }
}
