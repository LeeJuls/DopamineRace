using UnityEngine;

// ══════════════════════════════════════════
//  충돌 VFX용 런타임 스프라이트 생성
//  static 유틸 클래스 (인스턴스 불필요)
// ══════════════════════════════════════════
public static class CollisionSpriteFactory
{
    /// <summary>원형 스프라이트</summary>
    public static Sprite CreateCircleSprite(int size, Color color)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        float center = size / 2f;
        float radius = size / 2f - 1f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                if (dist <= radius)
                    tex.SetPixel(x, y, color);
                else if (dist <= radius + 1f)
                    tex.SetPixel(x, y, new Color(color.r, color.g, color.b, 0.5f));
                else
                    tex.SetPixel(x, y, Color.clear);
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }

    /// <summary>별 모양 스프라이트 (충돌)</summary>
    public static Sprite CreateStarSprite(int size, Color color, int points = 6)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        float center = size / 2f;

        Color[] clear = new Color[size * size];
        for (int i = 0; i < clear.Length; i++) clear[i] = Color.clear;
        tex.SetPixels(clear);

        float outerR = size / 2f - 1f;
        float innerR = outerR * 0.45f;

        // 별 꼭짓점 계산
        Vector2[] verts = new Vector2[points * 2];
        for (int i = 0; i < points * 2; i++)
        {
            float angle = (i * Mathf.PI / points) - Mathf.PI / 2f;
            float r = (i % 2 == 0) ? outerR : innerR;
            verts[i] = new Vector2(center + Mathf.Cos(angle) * r, center + Mathf.Sin(angle) * r);
        }

        // 채우기 (간단한 scanline)
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                if (IsPointInPolygon(new Vector2(x, y), verts))
                    tex.SetPixel(x, y, color);
            }
        }

        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }

    /// <summary>위쪽 화살표 스프라이트 (슬링샷)</summary>
    public static Sprite CreateArrowSprite(int size, Color color)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;

        Color[] clear = new Color[size * size];
        for (int i = 0; i < clear.Length; i++) clear[i] = Color.clear;
        tex.SetPixels(clear);

        float center = size / 2f;

        // 화살표 머리 (삼각형)
        Vector2[] head = new Vector2[]
        {
            new Vector2(center, size - 2),           // 꼭대기
            new Vector2(3, size * 0.5f),             // 좌하
            new Vector2(size - 3, size * 0.5f),      // 우하
        };

        // 화살표 몸통 (직사각형)
        float bodyLeft = size * 0.3f;
        float bodyRight = size * 0.7f;
        float bodyTop = size * 0.55f;
        float bodyBottom = 2f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                bool inHead = IsPointInTriangle(new Vector2(x, y), head[0], head[1], head[2]);
                bool inBody = x >= bodyLeft && x <= bodyRight && y >= bodyBottom && y <= bodyTop;
                if (inHead || inBody)
                    tex.SetPixel(x, y, color);
            }
        }

        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }

    /// <summary>방패 모양 스프라이트 (회피)</summary>
    public static Sprite CreateShieldSprite(int size, Color color)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;

        Color[] clear = new Color[size * size];
        for (int i = 0; i < clear.Length; i++) clear[i] = Color.clear;
        tex.SetPixels(clear);

        float centerX = size / 2f;
        float top = size - 2f;
        float bottom = 2f;

        for (int y = 0; y < size; y++)
        {
            float t = (float)y / size;
            // 방패: 위쪽은 넓고 아래로 갈수록 좁아지는 형태
            float halfWidth;
            if (t > 0.5f)
                halfWidth = (size / 2f - 2f);  // 상단: 넓은 폭
            else
                halfWidth = (size / 2f - 2f) * (t / 0.5f);  // 하단: 점점 좁아짐 (뾰족)

            for (int x = 0; x < size; x++)
            {
                float dx = Mathf.Abs(x - centerX);
                if (dx <= halfWidth && y >= bottom && y <= top)
                {
                    // 테두리 효과
                    bool isBorder = (dx >= halfWidth - 1.5f) ||
                                    (y <= bottom + 1.5f) ||
                                    (y >= top - 1.5f) ||
                                    (t <= 0.5f && dx >= halfWidth - 1.5f);
                    if (isBorder)
                        tex.SetPixel(x, y, color);
                    else
                        tex.SetPixel(x, y, new Color(color.r, color.g, color.b, 0.5f));
                }
            }
        }

        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }

    // ── 기하 유틸 ──

    private static bool IsPointInPolygon(Vector2 point, Vector2[] polygon)
    {
        bool inside = false;
        int j = polygon.Length - 1;
        for (int i = 0; i < polygon.Length; i++)
        {
            if (((polygon[i].y > point.y) != (polygon[j].y > point.y)) &&
                (point.x < (polygon[j].x - polygon[i].x) * (point.y - polygon[i].y)
                / (polygon[j].y - polygon[i].y) + polygon[i].x))
            {
                inside = !inside;
            }
            j = i;
        }
        return inside;
    }

    private static bool IsPointInTriangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
    {
        float d1 = Sign(p, a, b);
        float d2 = Sign(p, b, c);
        float d3 = Sign(p, c, a);
        bool hasNeg = (d1 < 0) || (d2 < 0) || (d3 < 0);
        bool hasPos = (d1 > 0) || (d2 > 0) || (d3 > 0);
        return !(hasNeg && hasPos);
    }

    private static float Sign(Vector2 p1, Vector2 p2, Vector2 p3)
    {
        return (p1.x - p3.x) * (p2.y - p3.y) - (p2.x - p3.x) * (p1.y - p3.y);
    }
}
