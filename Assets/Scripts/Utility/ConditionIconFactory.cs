using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 컨디션 아이콘(화살표 Sprite) 런타임 생성 + 캐시.
/// 32×32 Texture2D에 화살표 래스터라이즈.
///
/// 사용법: ConditionIconFactory.GetIcon(Condition.Best)
/// </summary>
public static class ConditionIconFactory
{
    private static Dictionary<Condition, Sprite> cache;

    // ═══ 색상 정의 ═══
    private static readonly Color COLOR_BEST   = new Color32(0x00, 0xCC, 0x00, 0xFF); // 초록
    private static readonly Color COLOR_GOOD   = new Color32(0x99, 0xBB, 0x99, 0xFF); // 연녹
    private static readonly Color COLOR_NORMAL = new Color32(0xFF, 0xCC, 0x00, 0xFF); // 노랑
    private static readonly Color COLOR_BAD    = new Color32(0xFF, 0xAA, 0xAA, 0xFF); // 분홍
    private static readonly Color COLOR_WORST  = new Color32(0xAA, 0x00, 0x00, 0xFF); // 진빨강

    /// <summary>
    /// 컨디션에 맞는 화살표 Sprite 반환 (캐시).
    /// </summary>
    public static Sprite GetIcon(Condition condition)
    {
        if (cache == null)
            cache = new Dictionary<Condition, Sprite>();

        if (cache.TryGetValue(condition, out Sprite cached))
            return cached;

        Color color;
        float angle;

        switch (condition)
        {
            case Condition.Best:   color = COLOR_BEST;   angle = 90f;  break; // ↑
            case Condition.Good:   color = COLOR_GOOD;   angle = 45f;  break; // ↗
            case Condition.Normal: color = COLOR_NORMAL;  angle = 0f;   break; // →
            case Condition.Bad:    color = COLOR_BAD;     angle = -45f; break; // ↘
            case Condition.Worst:  color = COLOR_WORST;   angle = -90f; break; // ↓
            default:               color = COLOR_NORMAL;  angle = 0f;   break;
        }

        Sprite sprite = CreateArrowSprite(color, angle);
        cache[condition] = sprite;
        return sprite;
    }

    /// <summary>
    /// 캐시 초기화 (씬 전환 등에서 필요 시 호출)
    /// </summary>
    public static void ClearCache()
    {
        if (cache != null)
        {
            foreach (var kvp in cache)
            {
                if (kvp.Value != null && kvp.Value.texture != null)
                    Object.Destroy(kvp.Value.texture);
                if (kvp.Value != null)
                    Object.Destroy(kvp.Value);
            }
            cache.Clear();
        }
    }

    // ═══ 내부 구현 ═══

    /// <summary>
    /// 32×32 Texture2D에 화살표를 그려 Sprite로 변환.
    /// angle: 0=→, 90=↑, -90=↓, 45=↗, -45=↘
    /// </summary>
    private static Sprite CreateArrowSprite(Color color, float angleDeg)
    {
        const int SIZE = 32;
        Texture2D tex = new Texture2D(SIZE, SIZE, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        tex.wrapMode = TextureWrapMode.Clamp;

        // 투명 배경으로 초기화
        Color clear = new Color(0, 0, 0, 0);
        Color[] pixels = new Color[SIZE * SIZE];
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = clear;

        // 화살표 꼭짓점 (→ 방향 기본, 중심 16,16)
        // 화살표 형태: 삼각형 머리 + 직사각형 몸통
        float rad = angleDeg * Mathf.Deg2Rad;
        float cos = Mathf.Cos(rad);
        float sin = Mathf.Sin(rad);

        float cx = SIZE / 2f;
        float cy = SIZE / 2f;

        // 각 픽셀에 대해 화살표 안에 있는지 판별
        for (int y = 0; y < SIZE; y++)
        {
            for (int x = 0; x < SIZE; x++)
            {
                // 중심 기준 좌표로 변환 후 회전의 역변환
                float dx = x - cx + 0.5f;
                float dy = y - cy + 0.5f;

                // 역회전하여 기본(→ 방향) 좌표로
                float lx =  dx * cos + dy * sin;
                float ly = -dx * sin + dy * cos;

                if (IsInsideArrow(lx, ly))
                    pixels[y * SIZE + x] = color;
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();

        return Sprite.Create(tex, new Rect(0, 0, SIZE, SIZE),
            new Vector2(0.5f, 0.5f), SIZE);
    }

    /// <summary>
    /// → 방향 화살표 형상 판별 (로컬 좌표 기준, 중심=0,0).
    /// 삼각형 머리(우측) + 직사각형 몸통(좌측)
    /// </summary>
    private static bool IsInsideArrow(float lx, float ly)
    {
        // 몸통: -10 ≤ lx ≤ 3, -3 ≤ ly ≤ 3
        if (lx >= -10f && lx <= 3f && ly >= -3f && ly <= 3f)
            return true;

        // 삼각형 머리: lx 3~14, 너비가 lx에 따라 좁아짐
        // 꼭짓점: (14, 0), (3, -8), (3, 8)
        if (lx >= 3f && lx <= 14f)
        {
            float t = (lx - 3f) / 11f; // 0→1
            float halfH = 8f * (1f - t);
            if (ly >= -halfH && ly <= halfH)
                return true;
        }

        return false;
    }
}
