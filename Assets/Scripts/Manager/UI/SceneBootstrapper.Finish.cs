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

        // SPEC-028 Step 3.5·3.6: 메인/부수 분리 — 메인 영역 (큰 글씨 강조)
        // "획득한 도파민 스톤: N💎" + 라운드별 ✓/✗ 결과
        var wallet = WalletManager.Instance;
        int stoneTotal = wallet != null ? wallet.Stone : 0;

        string mainArea = "<size=42><color=#4DDDDD>" + SafeLocFor("str.finish.stone_total",
            "획득한 도파민 스톤: {0} 💎", stoneTotal) + "</color></size>\n\n";

        // 라운드별 ✓/✗ + 획득 스톤 (베팅액 = 획득 스톤이므로 betAmount가 ScoreManager 저장 안 되는 한
        // 임시로 score>0 케이스는 적중 / 0은 빗나감으로 표시)
        foreach (var r in sm.RoundHistory)
        {
            if (r.isWin)
                mainArea += "<size=24><color=#66FF66>" + SafeLocFor("str.finish.round.hit",
                    "R{0}: ✓ 적중   (+{1} 💎)", r.round, r.score) + "</color></size>\n";
            else
                mainArea += "<size=24><color=#FF6666>" + SafeLocFor("str.finish.round.miss",
                    "R{0}: ✗ 빗나감", r.round) + "</color></size>\n";
        }

        // 부수 영역 (작은 글씨) — 베팅 상세 + 최종 보유 젤리
        string detailArea = "\n<size=18><color=#888888>── " + SafeLocFor("str.finish.detail.section",
            "라운드별 상세") + " ──</color></size>\n";
        foreach (var r in sm.RoundHistory)
        {
            string typeName = BettingCalculator.GetTypeName(r.betType);
            string scoreStr = r.score > 0
                ? "<color=#FFD700>+" + r.score + "</color>"
                : "<color=#888888>+0</color>";
            string result = r.isWin
                ? "<color=#66FF66>적중</color>"
                : "<color=#FF6666>빗나감</color>";
            detailArea += "<size=16>R" + r.round + " | " + typeName + " | " + result + " | " + scoreStr + "</size>\n";
        }

        if (wallet != null)
        {
            detailArea += "\n<size=20><color=#AACFFF>" + SafeLocFor("str.finish.final_jelly",
                "최종 보유: 🟦 {0}", wallet.Jelly) + "</color></size>";
        }

        if (finishRoundDetailText != null)
        {
            finishRoundDetailText.text = mainArea + detailArea;
            // RichText 활성 (size·color 태그 동작용)
            finishRoundDetailText.supportRichText = true;
        }

        // 메인 totalScoreText는 큰 글씨 통화 강조 (이미 mainArea에 포함됐으나 호환성 위해 같이 표시)
        if (finishTotalScoreText != null)
        {
            int total = sm.CurrentGameScore;
            int wins = 0;
            foreach (var r in sm.RoundHistory)
                if (r.isWin) wins++;
            finishTotalScoreText.text = Loc.Get("str.finish.total", total, wins, sm.RoundHistory.Count);
        }

        // 스크롤 맨 위로 리셋 (Canvas 레이아웃 갱신 후)
        if (finishScrollRect != null)
        {
            Canvas.ForceUpdateCanvases();
            finishScrollRect.normalizedPosition = new Vector2(0f, 1f);
        }

        // SPEC-028 Step 3.6: 페이드인 애니메이션 (단순 alpha 보간)
        if (finishUI != null)
        {
            var cg = finishUI.GetComponent<CanvasGroup>();
            if (cg == null) cg = finishUI.AddComponent<CanvasGroup>();
            cg.alpha = 0f;
            StartCoroutine(FadeInFinishPanel(cg));
        }
    }

    private System.Collections.IEnumerator FadeInFinishPanel(CanvasGroup cg)
    {
        const float duration = 0.5f;
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            cg.alpha = Mathf.Clamp01(t / duration);
            yield return null;
        }
        cg.alpha = 1f;
    }
}
