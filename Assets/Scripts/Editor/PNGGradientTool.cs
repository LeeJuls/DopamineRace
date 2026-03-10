using UnityEditor;
using UnityEngine;
using System.IO;

/// <summary>
/// PNG 오른쪽 끝 알파 그라데이션 적용 도구.
/// 메뉴: DopamineRace > Apply TopBG Gradient
/// </summary>
public static class PNGGradientTool
{
    [MenuItem("DopamineRace/Apply TopBG Gradient")]
    public static void ApplyTopBGGradient()
    {
        string imgPath = "Assets/Resources/UI/Img_TopBG.png";
        ApplyHorizontalFade(imgPath, fadeStart: 0.70f);
    }

    public static void ApplyHorizontalFade(string assetPath, float fadeStart)
    {
        var importer = (TextureImporter)AssetImporter.GetAtPath(assetPath);
        if (importer == null) { Debug.LogError($"Importer 없음: {assetPath}"); return; }

        bool wasReadable = importer.isReadable;
        importer.isReadable = true;
        importer.SaveAndReimport();

        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
        int w = tex.width, h = tex.height;
        var pixels = tex.GetPixels32();

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float nx = (float)x / (w - 1);
                if (nx >= fadeStart)
                {
                    float t = (nx - fadeStart) / (1f - fadeStart);
                    t = t * t * (3f - 2f * t); // Smoothstep
                    int idx = y * w + x;
                    var c = pixels[idx];
                    c.a = (byte)(c.a * (1f - t));
                    pixels[idx] = c;
                }
            }
        }

        var newTex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        newTex.SetPixels32(pixels);
        newTex.Apply();

        byte[] pngBytes = newTex.EncodeToPNG();
        string fullPath = Path.GetFullPath(assetPath);
        File.WriteAllBytes(fullPath, pngBytes);

        importer.isReadable = wasReadable;
        importer.SaveAndReimport();
        AssetDatabase.Refresh();

        Debug.Log($"[PNGGradientTool] 완료: {assetPath} ({w}x{h}), fadeStart={fadeStart*100:F0}%");
    }
}
