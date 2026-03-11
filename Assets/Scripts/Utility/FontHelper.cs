using UnityEngine;

/// <summary>
/// 폰트 관련 유틸리티.
/// TextMesh/UI Text에 폰트를 적용하는 공통 로직을 한 곳에서 관리.
///
/// 사용 예:
///   FontHelper.ApplyToTextMesh(tm, 102);       // TextMesh(3D) font + material 적용
///   t.fontSize = FontHelper.ScaledFontSize(26); // UI Text 스케일 적용
///   font = FontHelper.GetUIFont();              // 현재 언어에 맞는 폰트 반환
/// </summary>
public static class FontHelper
{
    // ═══════════════════════════════════════
    //  TextMesh (3D) 폰트 적용
    // ═══════════════════════════════════════

    /// <summary>
    /// TextMesh에 현재 언어에 맞는 폰트 + MeshRenderer.material을 한번에 적용.
    /// ※ TextMesh는 font만 바꾸면 material이 이전 폰트 것으로 남아서 글자가 깨지므로
    ///   반드시 material도 같이 설정해야 함.
    /// </summary>
    public static void ApplyToTextMesh(TextMesh tm, int sortingOrder = 0)
    {
        if (tm == null) return;

        Font font = GetFontForText(tm.text);
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
    //  폰트 조회 — 언어 + 텍스트 내용 이중 판별
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
    /// 현재 언어 설정에 따라 적절한 폰트 반환 (한국어 → koreanFont, 그 외 → mainFont).
    /// koreanFont 미설정 시 mainFont 반환. mainFont도 없으면 OS fallback.
    /// ★ SceneBootstrapper.font 초기화, ApplyFontToAllText() 기본 폰트로 사용.
    /// </summary>
    public static Font GetUIFont()
    {
        var gs = GameSettings.Instance;
        if (gs == null) return GetUIFontWithFallback();

        // 한국어 → koreanFont 우선
        if (IsKoreanLang() && gs.koreanFont != null)
            return gs.koreanFont;

        return gs.mainFont != null ? gs.mainFont : GetUIFontWithFallback();
    }

    /// <summary>
    /// 텍스트 내용 기반 폰트 선택 (한글 포함 → koreanFont, 아니면 → 언어별 기본 폰트).
    /// ApplyFontToAllText()에서 개별 Text 컴포넌트에 적용할 때 사용.
    /// </summary>
    public static Font GetFontForText(string text)
    {
        var gs = GameSettings.Instance;
        if (gs == null) return GetUIFontWithFallback();

        // 1순위: 현재 언어가 한국어면 koreanFont 사용
        if (IsKoreanLang() && gs.koreanFont != null)
            return gs.koreanFont;

        // 2순위: 텍스트에 한글이 포함되어 있으면 koreanFont (다른 언어에서도 한글 이름 등)
        if (gs.koreanFont != null && !string.IsNullOrEmpty(text) && ContainsKorean(text))
            return gs.koreanFont;

        // 3순위: mainFont
        return gs.mainFont != null ? gs.mainFont : GetUIFontWithFallback();
    }

    /// <summary>
    /// (하위 호환) 한글 포함 여부에 따라 적절한 폰트 반환.
    /// → GetFontForText()로 대체됨. 기존 호출자 유지용.
    /// </summary>
    public static Font GetFont(string text = null)
    {
        return GetFontForText(text);
    }

    /// <summary>
    /// UI용 OS fallback 폰트 생성 (mainFont/koreanFont 모두 없을 때 사용).
    /// </summary>
    public static Font GetUIFontWithFallback()
    {
        var gs = GameSettings.Instance;

        // 언어별 폰트 먼저 시도
        if (gs != null)
        {
            if (IsKoreanLang() && gs.koreanFont != null) return gs.koreanFont;
            if (gs.mainFont != null) return gs.mainFont;
        }

        Font font = Font.CreateDynamicFontFromOSFont("Malgun Gothic", 24);
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

    /// <summary>현재 언어가 한국어인지 확인</summary>
    public static bool IsKoreanLang()
    {
        return Loc.CurrentLang == "ko";
    }

    /// <summary>한글 음절(가~힣) + 한글 자모(ㄱ~ㅎ,ㅏ~ㅣ) 포함 여부</summary>
    public static bool ContainsKorean(string text)
    {
        if (string.IsNullOrEmpty(text)) return false;
        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];
            if ((c >= 0xAC00 && c <= 0xD7A3) || (c >= 0x3131 && c <= 0x318E))
                return true;
        }
        return false;
    }
}
