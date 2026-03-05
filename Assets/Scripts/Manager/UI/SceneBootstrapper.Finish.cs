using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 최종 결산 화면: 라운드별 결과 테이블, 총점, 새 게임/리더보드 버튼
///
/// GameSettings.finishPanelPrefab이 연결된 경우 프리팹 인스턴스화,
/// 없으면 기존 동적 빌드(Legacy)로 폴백.
/// </summary>
public partial class SceneBootstrapper
{
    // ══════════════════════════════════════
    //  Finish UI 빌드 (프리팹 분기)
    // ══════════════════════════════════════
    private void BuildFinishUI(Transform parent)
    {
        var gs = GameSettings.Instance;
        if (gs != null && gs.finishPanelPrefab != null)
        {
            var panel = Instantiate(gs.finishPanelPrefab, parent);
            panel.name     = "FinishPanelInstance";
            finishUI       = panel;
            finishPanelRoot = panel.transform;
            CacheFinishUIReferences(finishPanelRoot);
        }
        else
        {
            BuildFinishUILegacy(parent);
        }
    }

    // ══════════════════════════════════════
    //  프리팹 UI 레퍼런스 캐싱
    // ══════════════════════════════════════
    private void CacheFinishUIReferences(Transform root)
    {
        finishTitleText      = FindText(root, "TitleText");
        finishTotalScoreText = FindText(root, "TotalScoreText");

        // RoundScrollView 구조 (v2: 스크롤 지원)
        Transform scrollT = root.Find("RoundScrollView");
        if (scrollT != null)
        {
            finishScrollRect      = scrollT.GetComponent<ScrollRect>();
            Transform detailT     = scrollT.Find("Viewport/RoundDetailText");
            finishRoundDetailText = detailT?.GetComponent<Text>();
        }
        else
        {
            // 레거시: 직접 자식 (폴백)
            finishRoundDetailText = FindText(root, "RoundDetailText");
        }

        // NewGameBtn
        Transform ngT = root.Find("NewGameBtn");
        if (ngT != null)
        {
            finishNewGameButton  = ngT.GetComponent<Button>();
            finishNewGameBtnText = FindText(ngT, "BtnText");
            finishNewGameButton?.onClick.AddListener(() => GameManager.Instance?.StartNewGame());
        }

        // Top100Btn
        Transform t1T = root.Find("Top100Btn");
        if (t1T != null)
        {
            finishTop100Button  = t1T.GetComponent<Button>();
            finishTop100BtnText = FindText(t1T, "BtnText");
            finishTop100Button?.onClick.AddListener(ShowLeaderboard);
        }
    }

    // ══════════════════════════════════════
    //  Legacy 빌드 (프리팹 없을 때 폴백)
    // ══════════════════════════════════════
    private void BuildFinishUILegacy(Transform parent)
    {
        Image bg = parent.gameObject.AddComponent<Image>();
        bg.color = new Color(0, 0, 0, 0.85f);

        finishTitleText = MkText(parent, Loc.Get("str.finish.title"),
            new Vector2(0.5f, 0.92f), new Vector2(0.5f, 0.92f),
            Vector2.zero, new Vector2(500, 70), 55, TextAnchor.MiddleCenter, new Color(1f, 0.85f, 0.2f));

        finishRoundDetailText = MkText(parent, "",
            new Vector2(0.5f, 0.58f), new Vector2(0.5f, 0.58f),
            Vector2.zero, new Vector2(700, 400), 20, TextAnchor.UpperCenter, Color.white);
        finishRoundDetailText.verticalOverflow = VerticalWrapMode.Overflow;

        finishTotalScoreText = MkText(parent, "",
            new Vector2(0.5f, 0.18f), new Vector2(0.5f, 0.18f),
            Vector2.zero, new Vector2(500, 60), 38, TextAnchor.MiddleCenter, Color.yellow);

        // 새 게임 버튼
        GameObject newGameBtn = new GameObject("NewGameBtn");
        newGameBtn.transform.SetParent(parent, false);
        RectTransform ngrt = newGameBtn.AddComponent<RectTransform>();
        ngrt.anchorMin = new Vector2(0.3f, 0.05f);
        ngrt.anchorMax = new Vector2(0.3f, 0.05f);
        ngrt.pivot = new Vector2(0.5f, 0.5f);
        ngrt.sizeDelta = new Vector2(220, 55);
        newGameBtn.AddComponent<Image>().color = new Color(0.25f, 0.5f, 0.9f);
        finishNewGameButton = newGameBtn.AddComponent<Button>();
        finishNewGameButton.onClick.AddListener(() => GameManager.Instance?.StartNewGame());
        MkText(newGameBtn.transform, Loc.Get("str.ui.btn.new_game"),
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(220, 55), 26, TextAnchor.MiddleCenter, Color.white);

        // Top 100 버튼
        GameObject top100Btn = new GameObject("Top100Btn");
        top100Btn.transform.SetParent(parent, false);
        RectTransform t100rt = top100Btn.AddComponent<RectTransform>();
        t100rt.anchorMin = new Vector2(0.7f, 0.05f);
        t100rt.anchorMax = new Vector2(0.7f, 0.05f);
        t100rt.pivot = new Vector2(0.5f, 0.5f);
        t100rt.sizeDelta = new Vector2(220, 55);
        top100Btn.AddComponent<Image>().color = new Color(0.5f, 0.3f, 0.6f);
        finishTop100Button = top100Btn.AddComponent<Button>();
        finishTop100Button.onClick.AddListener(ShowLeaderboard);
        MkText(top100Btn.transform, Loc.Get("str.ui.btn.top100"),
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(220, 55), 26, TextAnchor.MiddleCenter, Color.white);
    }

    // ══════════════════════════════════════
    //  ShowFinish — 데이터 채우기
    // ══════════════════════════════════════
    private void ShowFinish()
    {
        var sm = ScoreManager.Instance;
        if (sm == null) return;

        // 버튼 텍스트 Loc 갱신 (프리팹 기본값이 한국어 고정이므로 런타임 갱신)
        if (finishNewGameBtnText != null) finishNewGameBtnText.text = Loc.Get("str.ui.btn.new_game");
        if (finishTop100BtnText  != null) finishTop100BtnText.text  = Loc.Get("str.ui.btn.top100");
        if (finishTitleText      != null) finishTitleText.text      = Loc.Get("str.finish.title");

        string detail = Loc.Get("str.finish.round_header") + "\n";
        detail += "─────────────────────────\n";
        foreach (var r in sm.RoundHistory)
        {
            string typeName = BettingCalculator.GetTypeName(r.betType);
            string scoreStr = r.score > 0
                ? "<color=#FFD700>" + Loc.Get("str.finish.score_plus", r.score) + "</color>"
                : "<color=#888888>" + Loc.Get("str.finish.score_zero") + "</color>";
            string result = r.isWin
                ? "<color=#66FF66>" + Loc.Get("str.finish.hit") + "</color>"
                : "<color=#FF6666>" + Loc.Get("str.finish.miss") + "</color>";
            detail += "R" + r.round + "  |  " + typeName + "  |  " + result + "  " + scoreStr + "\n";
        }
        detail += "─────────────────────────";

        if (finishRoundDetailText != null) finishRoundDetailText.text = detail;

        int total = sm.CurrentGameScore;
        int wins  = 0;
        foreach (var r in sm.RoundHistory)
            if (r.isWin) wins++;

        if (finishTotalScoreText != null)
            finishTotalScoreText.text = Loc.Get("str.finish.total", total, wins, sm.RoundHistory.Count);

        // 스크롤 맨 위로 리셋 (Canvas 레이아웃 갱신 후)
        if (finishScrollRect != null)
        {
            Canvas.ForceUpdateCanvases();
            finishScrollRect.normalizedPosition = new Vector2(0f, 1f);
        }
    }
}
