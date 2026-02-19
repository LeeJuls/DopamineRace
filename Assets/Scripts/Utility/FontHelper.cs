using UnityEngine;

/// <summary>
/// 폰트 관련 유틸리티.
/// TextMesh/UI Text에 폰트를 적용하는 공통 로직을 한 곳에서 관리.
/// 
/// 사용 예:
///   FontHelper.ApplyToTextMesh(tm, 102);       // TextMesh(3D) font + material 적용
///   t.fontSize = FontHelper.ScaledFontSize(26); // UI Text 스케일 적용
///   font = FontHelper.GetUIFontWithFallback();  // mainFont → OS폰트 fallback
/// </summary>
public static class FontHelper
{
    // ═══════════════════════════════════════
    //  TextMesh (3D) 폰트 적용
    // ═══════════════════════════════════════

    /// <summary>
    /// TextMesh에 GameSettings.mainFont + MeshRenderer.material을 한번에 적용.
    /// ※ TextMesh는 font만 바꾸면 material이 이전 폰트 것으로 남아서 글자가 깨지므로
    ///   반드시 material도 같이 설정해야 함.
    /// </summary>
    public static void ApplyToTextMesh(TextMesh tm, int sortingOrder = 0)
    {
        if (tm == null) return;

        Font font = GetMainFont();
        if (font != null)
            tm.font = font;

        MeshRenderer mr = tm.GetComponent<MeshRenderer>();
        if (mr != null)
        {
            if (sortingOrder != 0)
                mr.sortingOrder = sortingOrder;

            if (tm.font != null && tm.font.material != null)
                mr.material = tm.font.material;
        }
    }

    // ═══════════════════════════════════════
    //  UI Text 폰트 크기 스케일
    // ═══════════════════════════════════════

    /// <summary>
    /// baseSize에 GameSettings.uiFontScale을 곱한 값 반환.
    /// 도트 폰트(고정폭)가 기존 폰트보다 넓을 때 스케일을 낮춰서 대응.
    /// </summary>
    public static int ScaledFontSize(int baseSize)
    {
        float scale = GetUIFontScale();
        return Mathf.Max(1, Mathf.RoundToInt(baseSize * scale));
    }

    // ═══════════════════════════════════════
    //  폰트 조회
    // ═══════════════════════════════════════

    /// <summary>
    /// GameSettings.mainFont 반환. null이면 null (호출자가 fallback 처리).
    /// </summary>
    public static Font GetMainFont()
    {
        var gs = GameSettings.Instance;
        return (gs != null) ? gs.mainFont : null;
    }

    /// <summary>
    /// 한글 포함 여부에 따라 적절한 폰트 반환.
    /// koreanFont 설정 시 한글 텍스트에는 koreanFont, 그 외에는 mainFont.
    /// </summary>
    public static Font GetFont(string text = null)
    {
        var gs = GameSettings.Instance;
        if (gs == null) return null;

        if (gs.koreanFont != null && !string.IsNullOrEmpty(text) && ContainsKorean(text))
            return gs.koreanFont;

        return gs.mainFont;
    }

    /// <summary>
    /// UI용 OS fallback 폰트 생성 (mainFont가 없을 때 사용).
    /// </summary>
    public static Font GetUIFontWithFallback()
    {
        Font font = GetMainFont();
        if (font != null) return font;

        font = Font.CreateDynamicFontFromOSFont("Malgun Gothic", 24);
        if (font == null) font = Font.CreateDynamicFontFromOSFont("Arial", 24);
        return font;
    }

    // ═══════════════════════════════════════
    //  내부 유틸
    // ═══════════════════════════════════════

    private static float GetUIFontScale()
    {
        var gs = GameSettings.Instance;
        return (gs != null) ? gs.uiFontScale : 1f;
    }

    /// <summary>한글 음절(가~힣) + 한글 자모(ㄱ~ㅎ,ㅏ~ㅣ) 포함 여부</summary>
    private static bool ContainsKorean(string text)
    {
        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];
            if ((c >= 0xAC00 && c <= 0xD7A3) || (c >= 0x3131 && c <= 0x318E))
                return true;
        }
        return false;
    }
}
