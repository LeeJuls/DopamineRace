using UnityEngine;
using UnityEditor;
using System.IO;

// ══════════════════════════════════════════
//  충돌 VFX 스프라이트 PNG 생성 도구
//  메뉴: DopamineRace > Generate Collision VFX Sprites
//  Assets/Resources/VFX/ 에 5개 PNG 저장
// ══════════════════════════════════════════
public static class CollisionVFXSpriteGenerator
{
    private const int SIZE = 64;
    private const string OUTPUT_DIR = "Assets/Resources/VFX";

    [MenuItem("DopamineRace/Generate Collision VFX Sprites")]
    public static void Generate()
    {
        if (!Directory.Exists(OUTPUT_DIR))
            Directory.CreateDirectory(OUTPUT_DIR);

        SavePNG(CreateCircle(SIZE), "vfx_circle");
        SavePNG(CreateStar(SIZE, 6), "vfx_star6");
        SavePNG(CreateStar(SIZE, 5), "vfx_star5");
        SavePNG(CreateArrow(SIZE), "vfx_arrow");
        SavePNG(CreateShield(SIZE), "vfx_shield");

        AssetDatabase.Refresh();

        // Import 설정: Sprite, Point filter, PPU=64
        string[] names = { "vfx_circle", "vfx_star6", "vfx_star5", "vfx_arrow", "vfx_shield" };
        foreach (var name in names)
        {
            string path = $"{OUTPUT_DIR}/{name}.png";
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spritePixelsPerUnit = SIZE;
                importer.filterMode = FilterMode.Point;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.alphaIsTransparency = true;
                importer.SaveAndReimport();
            }
        }

        Debug.Log($"[CollisionVFXSpriteGenerator] {names.Length}개 스프라이트 생성 완료 → {OUTPUT_DIR}/");
    }

    private static void SavePNG(Texture2D tex, string name)
    {
        byte[] png = tex.EncodeToPNG();
        string path = $"{OUTPUT_DIR}/{name}.png";
        File.WriteAllBytes(path, png);
        Object.DestroyImmediate(tex);
    }

    // ── 원형 ──
    private static Texture2D CreateCircle(int size)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        float center = size / 2f;
        float radius = size / 2f - 1f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                if (dist <= radius)
                    tex.SetPixel(x, y, Color.white);
                else if (dist <= radius + 1f)
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, 0.5f));
                else
                    tex.SetPixel(x, y, Color.clear);
            }
        }
        tex.Apply();
        return tex;
    }

    // ── 별 ──
    private static Texture2D CreateStar(int size, int points)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        var clear = new Color[size * size];
        for (int i = 0; i < clear.Length; i++) clear[i] = Color.clear;
        tex.SetPixels(clear);

        float center = size / 2f;
        float outerR = size / 2f - 1f;
        float innerR = outerR * 0.45f;

        var verts = new Vector2[points * 2];
        for (int i = 0; i < points * 2; i++)
        {
            float angle = (i * Mathf.PI / points) - Mathf.PI / 2f;
            float r = (i % 2 == 0) ? outerR : innerR;
            verts[i] = new Vector2(center + Mathf.Cos(angle) * r, center + Mathf.Sin(angle) * r);
        }

        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
                if (IsPointInPolygon(new Vector2(x, y), verts))
                    tex.SetPixel(x, y, Color.white);

        tex.Apply();
        return tex;
    }

    // ── 화살표 ──
    private static Texture2D CreateArrow(int size)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        var clear = new Color[size * size];
        for (int i = 0; i < clear.Length; i++) clear[i] = Color.clear;
        tex.SetPixels(clear);

        float center = size / 2f;
        var head = new Vector2[]
        {
            new Vector2(center, size - 2),
            new Vector2(3, size * 0.5f),
            new Vector2(size - 3, size * 0.5f),
        };
        float bodyLeft = size * 0.3f;
        float bodyRight = size * 0.7f;
        float bodyTop = size * 0.55f;
        float bodyBottom = 2f;

        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                bool inHead = IsPointInTriangle(new Vector2(x, y), head[0], head[1], head[2]);
                bool inBody = x >= bodyLeft && x <= bodyRight && y >= bodyBottom && y <= bodyTop;
                if (inHead || inBody)
                    tex.SetPixel(x, y, Color.white);
            }

        tex.Apply();
        return tex;
    }

    // ── 방패 ──
    private static Texture2D CreateShield(int size)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        var clear = new Color[size * size];
        for (int i = 0; i < clear.Length; i++) clear[i] = Color.clear;
        tex.SetPixels(clear);

        float centerX = size / 2f;
        float top = size - 2f;
        float bottom = 2f;

        for (int y = 0; y < size; y++)
        {
            float t = (float)y / size;
            float halfWidth = t > 0.5f
                ? (size / 2f - 2f)
                : (size / 2f - 2f) * (t / 0.5f);

            for (int x = 0; x < size; x++)
            {
                float dx = Mathf.Abs(x - centerX);
                if (dx <= halfWidth && y >= bottom && y <= top)
                {
                    bool isBorder = (dx >= halfWidth - 1.5f) ||
                                    (y <= bottom + 1.5f) ||
                                    (y >= top - 1.5f) ||
                                    (t <= 0.5f && dx >= halfWidth - 1.5f);
                    tex.SetPixel(x, y, isBorder
                        ? Color.white
                        : new Color(1f, 1f, 1f, 0.5f));
                }
            }
        }

        tex.Apply();
        return tex;
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
                inside = !inside;
            j = i;
        }
        return inside;
    }

    private static bool IsPointInTriangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
    {
        float d1 = Sign(p, a, b);
        float d2 = Sign(p, b, c);
        float d3 = Sign(p, c, a);
        return !((d1 < 0 || d2 < 0 || d3 < 0) && (d1 > 0 || d2 > 0 || d3 > 0));
    }

    private static float Sign(Vector2 p1, Vector2 p2, Vector2 p3)
    {
        return (p1.x - p3.x) * (p2.y - p3.y) - (p2.x - p3.x) * (p1.y - p3.y);
    }
}
