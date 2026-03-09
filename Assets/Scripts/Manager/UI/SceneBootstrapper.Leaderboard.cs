using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Top 100 리더보드 팝업
///
/// GameSettings.leaderboardPanelPrefab이 연결된 경우 프리팹 인스턴스화,
/// 없으면 기존 동적 빌드(Legacy)로 폴백.
///
/// [신형 프리팹 구조]
///   LeaderboardPanel
///   ├─ TitleText
///   ├─ HeaderText
///   ├─ ContentScrollView (ScrollRect)
///   │  └─ Viewport
///   │     └─ EntryContainer (VerticalLayoutGroup + ContentSizeFitter)
///   │        └─ EntryTemplate [active=false — 클론 소스]
///   │           ├─ InfoText    (28pt, white — 등수/점수/날짜)
///   │           └─ SummaryText (22pt, gray  — 라운드 요약)
///   └─ CloseBtn
///      └─ BtnText
/// </summary>
public partial class SceneBootstrapper
{
    // ══════════════════════════════════════
    //  Leaderboard UI 빌드 (프리팹 분기)
    // ══════════════════════════════════════
    private void BuildLeaderboardPopup(Transform parent)
    {
        var gs = GameSettings.Instance;
        if (gs != null && gs.leaderboardPanelPrefab != null)
        {
            var panel = Instantiate(gs.leaderboardPanelPrefab, parent);
            panel.name           = "LeaderboardPanelInstance";
            leaderboardPanelRoot = panel.transform;
            CacheLeaderboardUIReferences(leaderboardPanelRoot);
        }
        else
        {
            BuildLeaderboardPopupLegacy(parent);
        }
    }

    // ══════════════════════════════════════
    //  프리팹 UI 레퍼런스 캐싱 (신형)
    // ══════════════════════════════════════
    private void CacheLeaderboardUIReferences(Transform root)
    {
        leaderboardTitleText  = FindText(root, "TitleText");
        leaderboardHeaderText = FindText(root, "HeaderText");

        Transform scrollT = root.Find("ContentScrollView");
        if (scrollT != null)
        {
            leaderboardScrollRect = scrollT.GetComponent<ScrollRect>();

            // 신형: EntryContainer → EntryTemplate
            Transform containerT = scrollT.Find("Viewport/EntryContainer");
            if (containerT != null)
            {
                leaderboardEntryContainer = containerT;

                // 첫 번째 비활성 자식 = EntryTemplate
                for (int i = 0; i < containerT.childCount; i++)
                {
                    var child = containerT.GetChild(i);
                    if (!child.gameObject.activeSelf)
                    {
                        leaderboardEntryTemplate = child.gameObject;
                        break;
                    }
                }
            }
            else
            {
                // 구형 폴백: ContentText
                Transform contentT     = scrollT.Find("Viewport/ContentText");
                leaderboardContentText = contentT?.GetComponent<Text>();
            }
        }

        // CloseBtn
        Transform closeT = root.Find("CloseBtn");
        if (closeT != null)
        {
            leaderboardCloseButton  = closeT.GetComponent<Button>();
            leaderboardCloseBtnText = FindText(closeT, "BtnText");
            leaderboardCloseButton?.onClick.AddListener(() => leaderboardPopup.SetActive(false));
        }
    }

    // ══════════════════════════════════════
    //  Legacy 빌드 (프리팹 없을 때 폴백)
    // ══════════════════════════════════════
    private void BuildLeaderboardPopupLegacy(Transform parent)
    {
        Image bg = parent.gameObject.AddComponent<Image>();
        bg.color = new Color(0, 0, 0, 0.9f);

        leaderboardTitleText = MkText(parent, Loc.Get("str.leaderboard.title"),
            new Vector2(0.5f, 0.95f), new Vector2(0.5f, 0.95f),
            Vector2.zero, new Vector2(500, 50), 36, TextAnchor.MiddleCenter, new Color(1f, 0.85f, 0.2f));

        MkText(parent, Loc.Get("str.leaderboard.header"),
            new Vector2(0.5f, 0.89f), new Vector2(0.5f, 0.89f),
            Vector2.zero, new Vector2(800, 35), 28, TextAnchor.MiddleCenter, new Color(0.8f, 0.8f, 0.8f));

        leaderboardContentText = MkText(parent, "",
            new Vector2(0.5f, 0.52f), new Vector2(0.5f, 0.52f),
            Vector2.zero, new Vector2(800, 600), 32, TextAnchor.UpperLeft, Color.white);
        leaderboardContentText.verticalOverflow = VerticalWrapMode.Overflow;

        // 닫기 버튼
        GameObject closeBtn = new GameObject("CloseBtn");
        closeBtn.transform.SetParent(parent, false);
        RectTransform crt = closeBtn.AddComponent<RectTransform>();
        crt.anchorMin = new Vector2(0.5f, 0.03f);
        crt.anchorMax = new Vector2(0.5f, 0.03f);
        crt.pivot     = new Vector2(0.5f, 0.5f);
        crt.sizeDelta = new Vector2(200, 55);
        closeBtn.AddComponent<Image>().color = new Color(0.5f, 0.3f, 0.3f);
        Button cb = closeBtn.AddComponent<Button>();
        cb.onClick.AddListener(() => leaderboardPopup.SetActive(false));
        MkText(closeBtn.transform, Loc.Get("str.ui.btn.close"),
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(200, 55), 28, TextAnchor.MiddleCenter, Color.white);
    }

    // ══════════════════════════════════════
    //  ShowLeaderboard — 데이터 채우기
    //
    //  [신형] EntryContainer != null → Instantiate(EntryTemplate) per entry
    //         InfoText  : 등수/점수/날짜 (top3 금색)
    //         SummaryText : 요약 5개씩 줄 분리
    //
    //  [Legacy] ContentText 단일 블롭 — 구형 프리팹 폴백
    // ══════════════════════════════════════
    private void ShowLeaderboard()
    {
        // Loc 갱신 (프리팹 기본값이 한국어 고정이므로 런타임 갱신)
        if (leaderboardTitleText    != null) leaderboardTitleText.text    = Loc.Get("str.leaderboard.title");
        if (leaderboardHeaderText   != null) leaderboardHeaderText.text   = Loc.Get("str.leaderboard.header");
        if (leaderboardCloseBtnText != null) leaderboardCloseBtnText.text = Loc.Get("str.ui.btn.close");

        // ── 신형: 항목별 인스턴스 방식 ──
        if (leaderboardEntryContainer != null && leaderboardEntryTemplate != null)
        {
            // 기존 인스턴스 제거 (EntryTemplate 제외)
            for (int i = leaderboardEntryContainer.childCount - 1; i >= 0; i--)
            {
                var child = leaderboardEntryContainer.GetChild(i);
                if (child.gameObject != leaderboardEntryTemplate)
                    Destroy(child.gameObject);
            }

            var entries = LeaderboardData.GetTop(100);

            if (entries.Count == 0)
            {
                var emptyGO = Instantiate(leaderboardEntryTemplate, leaderboardEntryContainer);
                emptyGO.SetActive(true);
                var infoT = FindText(emptyGO.transform, "InfoText");
                if (infoT != null) infoT.text = Loc.Get("str.leaderboard.empty");
            }
            else
            {
                Color gold  = new Color(1f, 0.84f, 0f, 1f);
                Color white = Color.white;

                for (int i = 0; i < entries.Count; i++)
                {
                    var e       = entries[i];
                    var entryGO = Instantiate(leaderboardEntryTemplate, leaderboardEntryContainer);
                    entryGO.SetActive(true);

                    // 등수 (영어는 서수, 나머지는 숫자만)
                    string rankStr = Loc.CurrentLang == "en"
                        ? GetEnOrdinal(i + 1) : (i + 1).ToString();

                    // 날짜 압축: "2026-03-05 16:59" → "26-03-05"
                    string dateStr = e.date.Length >= 10 ? e.date.Substring(2, 8) : e.date;

                    string infoLine = Loc.Get("str.leaderboard.row", rankStr, e.score, dateStr);

                    var infoText = FindText(entryGO.transform, "InfoText");
                    if (infoText != null)
                    {
                        infoText.text  = infoLine;
                        infoText.color = (i < 3) ? gold : white;
                    }

                    var summaryText = FindText(entryGO.transform, "SummaryText");
                    if (summaryText != null)
                        summaryText.text = BuildSummaryText(e.summary);
                }
            }
        }
        else
        {
            // ── Legacy: 단일 ContentText 폴백 ──
            ShowLeaderboardLegacy();
        }

        leaderboardPopup.SetActive(true);

        // 스크롤 맨 위로 리셋
        if (leaderboardScrollRect != null)
        {
            Canvas.ForceUpdateCanvases();
            leaderboardScrollRect.normalizedPosition = new Vector2(0f, 1f);
        }
    }

    // ══════════════════════════════════════
    //  Legacy ShowLeaderboard (단일 ContentText 폴백)
    // ══════════════════════════════════════
    private void ShowLeaderboardLegacy()
    {
        var entries = LeaderboardData.GetTop(100);
        var sb = new System.Text.StringBuilder();

        if (entries.Count == 0)
        {
            sb.Append("\n\n\n").Append(Loc.Get("str.leaderboard.empty"));
        }
        else
        {
            for (int i = 0; i < entries.Count; i++)
            {
                var e = entries[i];

                string rankStr = Loc.CurrentLang == "en"
                    ? GetEnOrdinal(i + 1)
                    : (i + 1).ToString();

                string dateStr = e.date.Length >= 10 ? e.date.Substring(2, 8) : e.date;
                string infoLine = Loc.Get("str.leaderboard.row", rankStr, e.score, dateStr);
                string summaryBlock = BuildSummaryBlock(e.summary);
                string entry = infoLine + summaryBlock;

                if (i < 3)
                    sb.Append("<color=#FFD700>").Append(entry).Append("</color>\n\n");
                else
                    sb.Append(entry).Append("\n\n");
            }
        }

        if (leaderboardContentText != null) leaderboardContentText.text = sb.ToString();
    }

    // ══════════════════════════════════════
    //  요약 텍스트 빌드 (신형 — SummaryText 전용)
    //
    //  별도 Text 컴포넌트이므로 indent 불필요.
    //  신형 포맷: "R1:Win+27 R2:Exacta+500 ..." → 5개씩 \n 분리
    //  구형 포맷: "R1: Win +27pt | ..."         → 그대로 반환 (하위 호환)
    // ══════════════════════════════════════
    private static string BuildSummaryText(string summary)
    {
        if (string.IsNullOrEmpty(summary) || summary == "-") return "";

        // 구형 포맷 감지 (" | " 구분자) → 그대로 반환
        if (summary.Contains(" | ")) return summary;

        // 신형 포맷: 공백 split → 5개씩 그룹
        string[] rounds = summary.Split(new char[] { ' ' },
            System.StringSplitOptions.RemoveEmptyEntries);
        if (rounds.Length == 0) return "";

        var sb = new System.Text.StringBuilder();
        for (int j = 0; j < rounds.Length; j += 5)
        {
            if (j > 0) sb.Append("\n");
            int end = Mathf.Min(j + 5, rounds.Length);
            for (int k = j; k < end; k++)
            {
                if (k > j) sb.Append(" ");
                sb.Append(rounds[k]);
            }
        }
        return sb.ToString();
    }

    // ══════════════════════════════════════
    //  요약 블록 빌드 (Legacy — ContentText 단일 블롭용)
    //
    //  신형 포맷: "R1:Win+27 R2:Exacta+500 ..." → 5개씩 줄 나눔 + "  " indent
    //  구형 포맷: "R1: Win +27pt | ..."          → 한 줄로 표시 (하위 호환)
    // ══════════════════════════════════════
    private static string BuildSummaryBlock(string summary)
    {
        if (string.IsNullOrEmpty(summary) || summary == "-")
            return "";

        const string INDENT = "  "; // 2칸 들여쓰기

        // 구형 포맷 감지 (" | " 구분자)
        if (summary.Contains(" | "))
            return "\n" + INDENT + summary;

        // 신형 포맷: 공백으로 split → 5개씩 그룹
        string[] rounds = summary.Split(new char[] { ' ' },
            System.StringSplitOptions.RemoveEmptyEntries);
        if (rounds.Length == 0) return "";

        var sb = new System.Text.StringBuilder();
        for (int j = 0; j < rounds.Length; j += 5)
        {
            sb.Append("\n").Append(INDENT);
            int end = Mathf.Min(j + 5, rounds.Length);
            for (int k = j; k < end; k++)
            {
                if (k > j) sb.Append(" ");
                sb.Append(rounds[k]);
            }
        }
        return sb.ToString();
    }

    // ══════════════════════════════════════
    //  영어 서수 헬퍼 (1→1st, 2→2nd, 3→3rd, 4+→4th)
    //  11/12/13 예외: 11th / 12th / 13th
    // ══════════════════════════════════════
    private static string GetEnOrdinal(int n)
    {
        int mod100 = n % 100;
        string suffix = (mod100 >= 11 && mod100 <= 13) ? "th"
            : (n % 10) == 1 ? "st"
            : (n % 10) == 2 ? "nd"
            : (n % 10) == 3 ? "rd"
            : "th";
        return n.ToString() + suffix;
    }
}
