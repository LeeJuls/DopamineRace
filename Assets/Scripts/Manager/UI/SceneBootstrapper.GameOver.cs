using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// SceneBootstrapper 공용 UI 유틸 partial.
/// (SPEC-029 전용 GameOver 패널 경로는 S6에서 폐기됨 — 게임오버는 ShowFinish(isGameOver:true) 경로로 통일.)
/// 여기 남은 멤버는 여러 partial(Finish/Leaderboard/NameEntry)에서 공유하는 헬퍼:
///   - SafeLocFor : Loc 키 조회 + 폴백 + 포맷
///   - MkImage    : 코드 폴백 UI용 Image 생성
/// </summary>
public partial class SceneBootstrapper : MonoBehaviour
{
    // ───── 공용 유틸 ─────

    private static string SafeLocFor(string key, string fallback, params object[] args)
    {
        string val = Loc.Get(key);
        if (string.IsNullOrEmpty(val) || val == key)
            val = fallback;
        if (args != null && args.Length > 0)
        {
            try { val = string.Format(val, args); } catch { }
        }
        return val;
    }

    private static Image MkImage(Transform parent, string name,
        Vector2 anchorMin, Vector2 anchorMax,
        Vector2 anchoredPos, Vector2 sizeDelta,
        Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = sizeDelta;
        var img = go.AddComponent<Image>();
        img.color = color;
        return img;
    }
}
