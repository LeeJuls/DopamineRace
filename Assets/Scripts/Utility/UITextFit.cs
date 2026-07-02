using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 다국어 텍스트 자동 축소(shrink-to-fit) 공용 헬퍼 (legacy UI.Text 전용).
/// 원칙: 박스(rect)는 그대로 두고, 넘칠 것 같으면 폰트 크기를 줄인다.
///
/// - Shrink       : 일반 라벨용 Best Fit 중앙화. ★ horizontalOverflow=Wrap 필수 —
///                  실측(S0)으로 Overflow 모드에서는 Best Fit이 폭 제약을 무시함을 확인
///                  (extents 100px·'Inteligencia' 202px: Overflow→28 유지, Wrap→14 축소).
/// - FitRichText  : rich text <size=N> 태그 포함 텍스트용. Best Fit은 인라인 size 태그를
///                  스케일하지 못하므로, 태그 값과 fontSize를 동일 비율로 1회 축소한다.
/// - FitFontSize  : "이 라벨들을 이 폭에 넣으려면 폰트 몇?" 순수 계산 —
///                  XCharts 레이더처럼 라이브러리가 Text를 소유해 Best Fit을 걸 수 없는 곳에서
///                  스타일(textStyle.fontSize)에 넣을 값을 구할 때 사용.
///
/// min 하한 기본 10px: fusion-pixel 12px 원본 + CanvasScaler(match=1.0) 특성상
/// 그 이하는 저해상도에서 판독 불가(ui-designer 검토 반영).
/// </summary>
public static class UITextFit
{
    /// <summary>전역 최소 폰트 크기 (판독성 하한)</summary>
    public const int MIN_FONT_SIZE = 10;

    private static readonly Regex SizeTagRegex = new Regex(@"<size=(\d+)>", RegexOptions.Compiled);

    /// <summary>
    /// 일반 라벨 shrink-to-fit. maxSize를 0 이하로 주면 현재 fontSize를 상한으로 사용
    /// (프리팹이 수작업 튜닝돼 코드 상수와 다른 경우가 많아, 현재 값 존중이 기본).
    /// 넘치지 않으면 크기 변화 없음(Best Fit이 max에서 시작) → ko/en 회귀 없음.
    /// </summary>
    public static void Shrink(Text t, int minSize = MIN_FONT_SIZE, int maxSize = 0)
    {
        if (t == null) return;
        if (maxSize <= 0) maxSize = Mathf.Max(t.fontSize, 1);
        t.resizeTextForBestFit = true;
        t.resizeTextMinSize    = Mathf.Max(1, Mathf.Min(minSize, maxSize));
        t.resizeTextMaxSize    = maxSize;
        t.horizontalOverflow   = HorizontalWrapMode.Wrap;      // Overflow면 폭 제약 무시(S0 실측)
        t.verticalOverflow     = VerticalWrapMode.Truncate;    // 최후 방어선(min 도달 시에만 발동)
    }

    /// <summary>
    /// rich text(<size=N> 태그) 텍스트를 rect에 맞게 비율 1회 축소.
    /// text 세팅 "이후" 호출할 것. 태그가 없으면 fontSize만 스케일 대상.
    /// preferredWidth/Height는 현재 overflow 설정 기준(스킬바는 H=Overflow →
    /// 작성된 \n 줄 구성 그대로의 자연 폭/높이)으로 측정된다.
    /// </summary>
    public static void FitRichText(Text t, float minScale = 0.5f)
    {
        if (t == null || string.IsNullOrEmpty(t.text)) return;
        RectTransform rt = t.rectTransform;
        float rw = rt.rect.width, rh = rt.rect.height;
        if (rw < 1f || rh < 1f) return; // 레이아웃 미확정 — 측정 불가 시 무동작

        float pw = t.preferredWidth, ph = t.preferredHeight;
        if (pw <= rw && ph <= rh) return; // 이미 들어감 — 원본 유지(회귀 0)

        float scale = Mathf.Clamp(Mathf.Min(rw / Mathf.Max(pw, 1f), rh / Mathf.Max(ph, 1f)), minScale, 1f);
        ApplyRichTextScale(t, scale);

        // 정수 반올림 오차 재검증 1회 (이진탐색 대신 — client 검토 반영)
        if ((t.preferredWidth > rw || t.preferredHeight > rh) && scale > minScale)
            ApplyRichTextScale(t, 0.94f);
    }

    /// <summary>size 태그 값과 fontSize에 비율 적용 (하한 MIN_FONT_SIZE)</summary>
    private static void ApplyRichTextScale(Text t, float scale)
    {
        t.text = SizeTagRegex.Replace(t.text, m =>
        {
            int v = int.Parse(m.Groups[1].Value);
            return "<size=" + Mathf.Max(MIN_FONT_SIZE, Mathf.FloorToInt(v * scale)) + ">";
        });
        t.fontSize = Mathf.Max(MIN_FONT_SIZE, Mathf.FloorToInt(t.fontSize * scale));
    }

    /// <summary>
    /// 라벨 집합이 budgetWidth(px)에 들어가는 폰트 크기 계산 (rich text 태그 무시 폭 기준).
    /// Best Fit을 걸 수 없는 외부 라이브러리 소유 Text(XCharts 레이더 축라벨 등)용.
    /// 넘치지 않으면 baseSize 그대로 반환(ko/en 회귀 0).
    /// </summary>
    public static int FitFontSize(Font font, System.Collections.Generic.IList<string> labels,
                                  int baseSize, float budgetWidth, int minSize = 16)
    {
        if (font == null || labels == null || labels.Count == 0 || budgetWidth <= 1f) return baseSize;

        var tg = new TextGenerator();
        var settings = new TextGenerationSettings
        {
            font = font, fontSize = baseSize, fontStyle = FontStyle.Normal,
            color = Color.white, lineSpacing = 1f, richText = true, scaleFactor = 1f,
            textAnchor = TextAnchor.MiddleCenter,
            horizontalOverflow = HorizontalWrapMode.Overflow,
            verticalOverflow = VerticalWrapMode.Overflow,
            generationExtents = new Vector2(4000f, 200f),
            resizeTextForBestFit = false,
            updateBounds = true, pivot = new Vector2(0.5f, 0.5f)
        };

        float maxW = 0f;
        foreach (var s in labels)
        {
            if (string.IsNullOrEmpty(s)) continue;
            float w = tg.GetPreferredWidth(s, settings);
            if (w > maxW) maxW = w;
        }
        if (maxW <= budgetWidth) return baseSize;
        return Mathf.Max(minSize, Mathf.FloorToInt(baseSize * budgetWidth / maxW));
    }
}
