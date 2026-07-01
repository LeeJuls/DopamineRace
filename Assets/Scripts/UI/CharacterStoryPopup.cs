using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 캐릭터 스토리(세계관 텍스트) 팝업.
/// CharacterInfoPopup의 PenIconBtn 클릭 시 표시된다.
/// BettingPanel.prefab 루트의 nested PrefabInstance로 1개 인스턴스를 <see cref="Instance"/>로 노출.
///
/// ※ 보이기/숨기기는 자식 <c>box</c>를 토글한다(루트는 항상 활성).
///   루트를 SetActive(false) 하면 Awake가 다시 실행되지 않아 Instance 등록이 깨지므로
///   (ItemInfoPopup과 동일 원칙 — CharacterInfoPopup처럼 루트를 직접 끄는 방식과 혼동 금지).
///
/// 프리팹: Assets/Prefabs/UI/CharacterStoryPopup.prefab
/// 스토리 텍스트는 StringTable 키(str.char.NNN.scenario), CharacterDataV4.charStory 경유 조회.
///
/// 가독성 개선(2026-07): 원문에 §TIP§ 마커로 "배경서사/배팅팁" 경계를 표시하면
/// FormatStoryText가 본문은 2문장씩 문단 구분, 팁은 별도 색상 문단으로 분리한다.
/// 콘텐츠에 [skill]...[/skill](능력치, 골드+bold), [key]...[/key](세계관 핵심어, 오렌지)
/// 태그를 넣으면 색상 강조된다. 태그는 중첩 금지·한 문장 내에서만 사용할 것.
/// </summary>
public class CharacterStoryPopup : MonoBehaviour
{
    public static CharacterStoryPopup Instance { get; private set; }

    [Header("UI 참조")]
    [SerializeField] private GameObject box;         // 보이기/숨기기 대상 (루트 아님)
    [SerializeField] private Text titleText;         // 캐릭터 이름 헤더
    [SerializeField] private Text storyText;         // ScrollRect/Content 안의 본문 텍스트
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private Button closeBtn;

    private const string TIP_MARKER  = "§TIP§";
    private const string COLOR_SKILL = "#A67C00";   // 골드(진하게, WCAG 대비 보강) — 능력치/패시브 수치
    private const string COLOR_KEY   = "#D9622B";   // 오렌지 — 세계관 핵심 단어
    private const string COLOR_TIP   = "#3B6EA5";   // 블루 — 배팅 팁 문단(자동 분리, 수동 태그 아님)

    private static readonly Regex SkillTagRegex = new Regex(@"\[skill\](.*?)\[/skill\]", RegexOptions.Compiled);
    private static readonly Regex KeyTagRegex   = new Regex(@"\[key\](.*?)\[/key\]", RegexOptions.Compiled);
    private static readonly Regex SentenceSplitRegex = new Regex(@"(?<=[.!?])\s+", RegexOptions.Compiled);

    /// <summary>현재 표시 중인 캐릭터 UID</summary>
    public string CurrentCharId { get; private set; }

    /// <summary>팝업이 열려있는지</summary>
    public bool IsShowing => box != null && box.activeSelf;

    private void Awake()
    {
        Instance = this;
        if (closeBtn != null)
            closeBtn.onClick.AddListener(Hide);
        if (box != null)
            box.SetActive(false);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    /// <summary>
    /// 캐릭터 스토리 표시. charId로 CharacterDatabaseV4 조회 → charStory(StringUID) → Loc.Get.
    /// </summary>
    public void Show(string charId)
    {
        CurrentCharId = charId;

        string storyKey = null;
        var v4 = CharacterDatabaseV4.FindById(charId);
        if (v4 != null) storyKey = v4.charStory;

        if (storyText != null)
        {
            string resolved = !string.IsNullOrEmpty(storyKey) ? Loc.Get(storyKey) : "";
            // 키는 있으나 해당 언어 값이 비어있는 경우(예: 시나리오 미작성 캐릭터)도 폴백 처리
            string display = !string.IsNullOrEmpty(resolved)
                ? resolved
                : Loc.Get("str.char.story.empty");
            storyText.text = FormatStoryText(display);
        }

        if (titleText != null && v4 != null)
            titleText.text = Loc.Get(v4.charName);

        if (box != null) box.SetActive(true);

        // 스크롤 위치 최상단 리셋 + 콘텐츠 높이 강제 리빌드 (ContentSizeFitter 반영 대기 없이 즉시)
        if (scrollRect != null && scrollRect.content != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(scrollRect.content);
            scrollRect.verticalNormalizedPosition = 1f;
        }

        ApplyFont();
    }

    /// <summary>팝업 닫기</summary>
    public void Hide()
    {
        CurrentCharId = null;
        if (box != null) box.SetActive(false);
    }

    /// <summary>현재 언어 폰트 일괄 적용 (CJK·한글 라우팅) — ItemInfoPopup과 동일 정책.</summary>
    private void ApplyFont()
    {
        Font font = FontHelper.GetFontForCurrentLanguage();
        if (font == null) font = FontHelper.GetMainFont();
        if (font == null) return;

        foreach (var t in GetComponentsInChildren<Text>(true))
            t.font = font;
    }

    // ══════════════════════════════════════════════════════════════
    //  가독성 포맷팅 — §TIP§ 분리 + 2문장 문단 그룹핑 + 시맨틱 컬러태그
    // ══════════════════════════════════════════════════════════════

    /// <summary>
    /// 원문(마커·태그 포함 가능)을 화면 표시용으로 가공.
    /// 1) [skill]/[key] → &lt;color&gt; 치환(전체 1회, 문장분리 전)
    /// 2) §TIP§ 마커로 본문/팁 분리(없으면 전체를 본문으로 취급 — 미번역 언어 안전 폴백)
    /// 3) 본문은 2문장씩(단, 문단 최소 글자수 미달 시 다음 문장까지 병합) 빈 줄 구분
    /// 4) 팁은 파란색+이탤릭 문단으로 덧붙임
    /// </summary>
    private static string FormatStoryText(string raw)
    {
        if (string.IsNullOrEmpty(raw)) return raw;

        raw = ApplySemanticTags(raw);

        string profile = raw;
        string tip = null;
        int idx = raw.IndexOf(TIP_MARKER, System.StringComparison.Ordinal);
        if (idx >= 0)
        {
            profile = raw.Substring(0, idx).Trim();
            tip = raw.Substring(idx + TIP_MARKER.Length).Trim();
        }

        string formatted = GroupSentences(profile, perParagraph: 2, minChars: 15);
        if (string.IsNullOrEmpty(tip))
            return formatted;   // 마커 없음(미번역 언어 등) → 문단 청킹만 적용, 안전 폴백

        return formatted + "\n\n<i><color=" + COLOR_TIP + ">" + tip + "</color></i>";
    }

    /// <summary>[skill]/[key] 태그 → Unity RichText &lt;color&gt; 변환. 중첩·미매칭은 콘텐츠 가이드로 금지(코드 방어 안 함).</summary>
    private static string ApplySemanticTags(string text)
    {
        text = SkillTagRegex.Replace(text, "<color=" + COLOR_SKILL + "><b>$1</b></color>");
        text = KeyTagRegex.Replace(text, "<color=" + COLOR_KEY + ">$1</color>");
        return text;
    }

    /// <summary>문장 단위로 묶어 빈 줄(\n\n) 삽입. 문단이 minChars 미만이면 다음 문장까지 병합(단문체 캐릭터 대응).</summary>
    private static string GroupSentences(string text, int perParagraph, int minChars)
    {
        if (string.IsNullOrEmpty(text)) return text;

        string[] sentences = SentenceSplitRegex.Split(text);
        var paragraphs = new List<string>();
        var current = new StringBuilder();
        int countInCurrent = 0;

        foreach (var s in sentences)
        {
            if (current.Length > 0) current.Append(' ');
            current.Append(s);
            countInCurrent++;

            if (countInCurrent >= perParagraph && current.Length >= minChars)
            {
                paragraphs.Add(current.ToString());
                current.Clear();
                countInCurrent = 0;
            }
        }
        if (current.Length > 0) paragraphs.Add(current.ToString());

        return string.Join("\n\n", paragraphs);
    }
}
