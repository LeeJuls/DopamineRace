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
    // ══════════════════════════════════════
    private void ShowLeaderboard()
    {
        // 버튼 텍스트 Loc 갱신 (프리팹 기본값이 한국어 고정이므로 런타임 갱신)
        if (leaderboardTitleText    != null) leaderboardTitleText.text    = Loc.Get("str.leaderboard.title");
        if (leaderboardHeaderText   != null) leaderboardHeaderText.text   = Loc.Get("str.leaderboard.header");
        if (leaderboardCloseBtnText != null) leaderboardCloseBtnText.text = Loc.Get("str.ui.btn.close");

        var entries = LeaderboardData.GetTop(100);
        string content = "";

        if (entries.Count == 0)
        {
            content = "\n\n\n" + Loc.Get("str.leaderboard.empty");
        }
        else
        {
            for (int i = 0; i < entries.Count; i++)
            {
                var e = entries[i];
                string rank    = (i + 1).ToString().PadLeft(3);
                string score   = e.score.ToString().PadLeft(6);
                string summary = e.summary;
                if (summary.Length > 40) summary = summary.Substring(0, 40) + "...";

                string line = Loc.Get("str.leaderboard.row", rank, score, e.rounds, e.date, summary);

                if (i < 3)
                    content += "<color=#FFD700>" + line + "</color>\n";
                else
                    content += line + "\n";
            }
        }

        if (leaderboardContentText != null) leaderboardContentText.text = content;
        leaderboardPopup.SetActive(true);

        // 스크롤 맨 위로 리셋 (Canvas 레이아웃 갱신 후)
        if (leaderboardScrollRect != null)
        {
            Canvas.ForceUpdateCanvases();
            leaderboardScrollRect.normalizedPosition = new Vector2(0f, 1f);
        }
    }
}
