using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Top 100 리더보드 팝업
///
/// GameSettings.leaderboardPanelPrefab이 연결된 경우 프리팹 인스턴스화,
/// 없으면 기존 동적 빌드(Legacy)로 폴백.
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
            panel.name       = "LeaderboardPanelInstance";
            leaderboardPanelRoot = panel.transform;
            CacheLeaderboardUIReferences(leaderboardPanelRoot);
        }
        else
        {
            BuildLeaderboardPopupLegacy(parent);
        }
    }

    // ══════════════════════════════════════
    //  프리팹 UI 레퍼런스 캐싱
    // ══════════════════════════════════════
    private void CacheLeaderboardUIReferences(Transform root)
    {
        leaderboardTitleText  = FindText(root, "TitleText");
        leaderboardHeaderText = FindText(root, "HeaderText");

        // ContentScrollView 구조 (스크롤 지원)
        Transform scrollT = root.Find("ContentScrollView");
        if (scrollT != null)
        {
            leaderboardScrollRect  = scrollT.GetComponent<ScrollRect>();
            Transform contentT     = scrollT.Find("Viewport/ContentText");
            leaderboardContentText = contentT?.GetComponent<Text>();
        }
        else
        {
            // 레거시 폴백
            leaderboardContentText = FindText(root, "ContentText");
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
    //  표시 형식 (항목마다 1~N줄):
    //    [등수]  [점수]  [날짜]
    //      R1:Win+27 R2:Exacta+500 R3:Win+0 R4:Win+16 R5:Win+53
    //      R6:Win+11 R7:Win+1 ...
    //
    //  — 등수: 영어는 서수(1st/2nd), 나머지는 숫자만(str.leaderboard.row 포맷이 suffix 처리)
    //  — 날짜: "2026-03-05 16:59" → "26-03-05" (연도 앞 2자리·시간 생략)
    //  — 요약: 5개씩 줄 분리, 2칸 들여쓰기
    //  — 구형 summary(" | " 포함)는 그대로 한 줄 표시 (하위 호환)
    // ══════════════════════════════════════
    private void ShowLeaderboard()
    {
        // Loc 갱신 (프리팹 기본값이 한국어 고정이므로 런타임 갱신)
        if (leaderboardTitleText    != null) leaderboardTitleText.text    = Loc.Get("str.leaderboard.title");
        if (leaderboardHeaderText   != null) leaderboardHeaderText.text   = Loc.Get("str.leaderboard.header");
        if (leaderboardCloseBtnText != null) leaderboardCloseBtnText.text = Loc.Get("str.ui.btn.close");

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

                // 등수: 영어는 서수, 나머지는 숫자만 (str.leaderboard.row 포맷이 위/位 등 suffix 담당)
                string rankStr = Loc.CurrentLang == "en"
                    ? GetEnOrdinal(i + 1)
                    : (i + 1).ToString();

                // 날짜 압축: "2026-03-05 16:59" → "26-03-05"
                string dateStr = e.date.Length >= 10 ? e.date.Substring(2, 8) : e.date;

                // 정보 줄: "1위  1591점  26-03-05" / "1st  1591pt  26-03-05"
                string infoLine = Loc.Get("str.leaderboard.row", rankStr, e.score, dateStr);

                // 요약 블록: 5개씩 줄 분리 (신형 포맷) / 구형 포맷 폴백
                string summaryBlock = BuildSummaryBlock(e.summary);

                string entry = infoLine + summaryBlock;

                if (i < 3)
                    sb.Append("<color=#FFD700>").Append(entry).Append("</color>\n\n");
                else
                    sb.Append(entry).Append("\n\n");
            }
        }

        if (leaderboardContentText != null) leaderboardContentText.text = sb.ToString();
        leaderboardPopup.SetActive(true);

        // 스크롤 맨 위로 리셋 (Canvas 레이아웃 갱신 후)
        if (leaderboardScrollRect != null)
        {
            Canvas.ForceUpdateCanvases();
            leaderboardScrollRect.normalizedPosition = new Vector2(0f, 1f);
        }
    }

    // ══════════════════════════════════════
    //  요약 블록 빌드 (5개씩 줄 분리)
    //
    //  신형 포맷: "R1:Win+27 R2:Exacta+500 ..."  → 5개씩 줄 나눔
    //  구형 포맷: "R1: Win +27pt | ..." → 한 줄로 그대로 표시 (하위 호환)
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
        string[] rounds = summary.Split(new char[]{ ' ' },
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
    //  11/12/13은 예외: 11th / 12th / 13th
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
