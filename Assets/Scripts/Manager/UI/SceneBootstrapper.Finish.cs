using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

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

        // RoundScrollView 구조 (v3: 5-Text Content 분리)
        Transform scrollT = root.Find("RoundScrollView");
        if (scrollT != null)
        {
            finishScrollRect    = scrollT.GetComponent<ScrollRect>();
            Transform viewportT = scrollT.Find("Viewport");
            Transform contentT  = viewportT?.Find("Content");   // 신형: VLG+CSF 컨테이너
            if (contentT != null)
            {
                finishStoneHeaderText  = FindText(contentT, "StoneHeaderText");
                finishDetailHeaderText = FindText(contentT, "DetailHeaderText");
                finishRoundDetailText  = FindText(contentT, "RoundDetailText");
            }
            else
            {
                // 구형 프리팹 호환 폴백: 단일 RoundDetailText
                Transform detailT = viewportT?.Find("RoundDetailText");
                finishRoundDetailText = detailT?.GetComponent<Text>();
            }
        }
        else
        {
            // 레거시 코드빌드: 직접 자식에서 찾기
            finishStoneHeaderText  = FindText(root, "StoneHeaderText");
            finishDetailHeaderText = FindText(root, "DetailHeaderText");
            finishRoundDetailText  = FindText(root, "RoundDetailText");
        }
        // 프리팹/레거시 공통: FinalJellyText는 항상 root 직계 자식 (ScrollView 외부 고정)
        finishFinalJellyText = FindText(root, "FinalJellyText");

        // S3/S4: 순위 배지 (점수 + 등수 1줄) — FinalJelly 아래 root 직계 자식
        finishMyRankText = FindText(root, "MyRankText");

        // NewGameBtn — TitleScene 파편 전환 (SceneTransitionManager 블록 디졸브)
        Transform ngT = root.Find("NewGameBtn");
        if (ngT != null)
        {
            finishNewGameButton  = ngT.GetComponent<Button>();
            finishNewGameBtnText = FindText(ngT, "BtnText");
            finishNewGameButton?.onClick.RemoveAllListeners();
            finishNewGameButton?.onClick.AddListener(() =>
            {
                if (finishNewGameButton != null)
                    finishNewGameButton.interactable = false;   // 연타 방지
                if (SceneTransitionManager.Instance != null)
                    SceneTransitionManager.Instance.TransitionToScene("TitleScene", autoPlayBGM: false);
                else
                    SceneManager.LoadScene("TitleScene");
            });
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
        bg.color = new Color(0.12f, 0.10f, 0.18f, 0.95f);  // 런타임 폴백 — BG_01 불가

        finishTitleText = MkText(parent, Loc.Get("str.finish.title"),
            new Vector2(0.5f, 0.92f), new Vector2(0.5f, 0.92f),
            Vector2.zero, new Vector2(500, 70), 55, TextAnchor.MiddleCenter, new Color(0.55f, 0.30f, 0.00f));

        // 5개 Text 분리 — BG_01 밝은 배경 기준 색상 (프리팹과 동일 팔레트)
        finishStoneHeaderText = MkText(parent, "",
            new Vector2(0.5f, 0.72f), new Vector2(0.5f, 0.72f),
            Vector2.zero, new Vector2(700, 60), 42, TextAnchor.UpperCenter, new Color(0.00f, 0.42f, 0.52f));
        finishDetailHeaderText = MkText(parent, "",
            new Vector2(0.5f, 0.62f), new Vector2(0.5f, 0.62f),
            Vector2.zero, new Vector2(700, 40), 18, TextAnchor.UpperCenter, new Color(0.40f, 0.40f, 0.50f));
        finishDetailHeaderText.supportRichText = true;
        finishRoundDetailText = MkText(parent, "",
            new Vector2(0.5f, 0.52f), new Vector2(0.5f, 0.52f),
            Vector2.zero, new Vector2(700, 200), 24, TextAnchor.UpperCenter, new Color(0.10f, 0.10f, 0.20f));
        finishRoundDetailText.verticalOverflow = VerticalWrapMode.Overflow;
        finishRoundDetailText.supportRichText  = true;
        finishFinalJellyText = MkText(parent, "",
            new Vector2(0.5f, 0.26f), new Vector2(0.5f, 0.26f),
            Vector2.zero, new Vector2(700, 40), 20, TextAnchor.UpperCenter, new Color(0.10f, 0.30f, 0.60f));

        // S4: 순위 배지 (점수 + 등수 1줄) — FinalJelly 아래, 런타임 빌드 폴백 정합
        finishMyRankText = MkText(parent, "",
            new Vector2(0.5f, 0.20f), new Vector2(0.5f, 0.20f),
            Vector2.zero, new Vector2(700, 40), 22, TextAnchor.UpperCenter, new Color(0.10f, 0.10f, 0.20f));
        finishMyRankText.name = "MyRankText";   // FindText 캐싱 일관 (프리팹 노드명과 동일)

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
        finishNewGameButton.onClick.RemoveAllListeners();
        finishNewGameButton.onClick.AddListener(() =>
        {
            finishNewGameButton.interactable = false;   // 연타 방지
            if (SceneTransitionManager.Instance != null)
                SceneTransitionManager.Instance.TransitionToScene("TitleScene", autoPlayBGM: false);
            else
                SceneManager.LoadScene("TitleScene");
        });
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
    private void ShowFinish(bool isGameOver = false)
    {
        var sm = ScoreManager.Instance;
        if (sm == null) return;

        // 버튼 텍스트 Loc 갱신 (프리팹 기본값이 한국어 고정이므로 런타임 갱신)
        if (finishNewGameBtnText != null) finishNewGameBtnText.text = Loc.Get("str.ui.btn.new_game");
        if (finishTop100BtnText  != null) finishTop100BtnText.text  = Loc.Get("str.ui.btn.top100");

        // 헤더만 분기 — 본문(스톤/라운드/배지)은 정상완료·게임오버 100% 공유
        if (finishTitleText != null)
        {
            if (isGameOver)
            {
                // 색약 대비: 적색 톤 + 사유 텍스트 병기(색 단독 의존 금지).
                // 별도 사유 노드가 없어 타이틀 노드에 인라인(헤드라인 적색 / 사유 회색 1줄).
                finishTitleText.supportRichText = true;
                string headline = "<color=#CC2222>" + Loc.Get("str.gameover.headline") + "</color>";
                string reason   = "<size=24><color=#555555>"
                    + Loc.Get("str.gameover.reason.nojelly") + "</color></size>";
                finishTitleText.text = headline + "\n" + reason;
            }
            else
            {
                finishTitleText.text = Loc.Get("str.finish.title");
            }
        }

        // SPEC-028 Step 3.5·3.6: 메인/부수 분리 — 메인 영역 (큰 글씨 강조)
        // "획득한 도파민 스톤: N💎" + 라운드별 ✓/✗ 결과
        var wallet = WalletManager.Instance;
        int stoneTotal = wallet != null ? wallet.Stone : 0;

        // ── 5개 Text 개별 설정 (size 태그 제거 — prefab fontSize 사용, color 태그 유지) ──

        // 1) 도파민 스톤 총액
        if (finishStoneHeaderText != null)
            finishStoneHeaderText.text = SafeLocFor("str.finish.stone_total",
                "획득한 도파민 스톤: {0} 💎", stoneTotal);

        // 2) 섹션 구분선
        if (finishDetailHeaderText != null)
        {
            finishDetailHeaderText.text = "── " + SafeLocFor("str.finish.detail.section",
                "라운드별 상세") + " ──";
            finishDetailHeaderText.supportRichText = true;
        }

        // 3) 라운드별 통합 행 (타입 + 결과 + 스톤)
        if (finishRoundDetailText != null)
        {
            string detailArea = "";
            foreach (var r in sm.RoundHistory)
            {
                string typeName = BettingCalculator.GetTypeName(r.betType);
                string line = r.isWin
                    ? "<color=#1A7A1A>" + SafeLocFor("str.finish.round.hit",
                        "R{0}: {1} | ✓ 적중 (+{2} 💎)", r.round, typeName, r.stoneGain) + "</color>"
                    : "<color=#CC2222>" + SafeLocFor("str.finish.round.miss",
                        "R{0}: {1} | ✗ 빗나감", r.round, typeName) + "</color>";
                detailArea += line + "\n";
            }
            finishRoundDetailText.text = detailArea.TrimEnd();
            finishRoundDetailText.supportRichText = true;
        }

        // 5) 총 보유 도파민 스톤 (SPEC-047: 젤리는 소멸성 연료라 최종 표시는 스톤으로 통일)
        if (finishFinalJellyText != null && wallet != null)
            finishFinalJellyText.text = SafeLocFor("str.finish.final_jelly",
                "총 보유 도파민 스톤: 💎 {0}", wallet.Stone);

        // S4: 순위 배지 — 요약 표시 시 로컬 등수로 초기 표시(원격 rank 갱신은 S5에서 연결)
        RefreshMyRankBadge(0);

        // 스크롤 맨 위로 리셋 (Canvas 레이아웃 갱신 후)
        if (finishScrollRect != null)
        {
            // ContentSizeFitter 자식들은 2패스 필요 — content 먼저 즉시 재빌드 후 Canvas 갱신
            LayoutRebuilder.ForceRebuildLayoutImmediate(finishScrollRect.content);
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

    // ══════════════════════════════════════
    //  순위 배지 갱신 (점수 + 등수 1줄)
    //  remoteRank>0 → 글로벌 / 폴백 로컬 등수 / 0 → 순위권 밖 (R4 정직 표기)
    //  S5에서 등록 확정 후 원격 rank로 재호출하여 글로벌 표기로 갱신.
    // ══════════════════════════════════════
    private void RefreshMyRankBadge(int remoteRank)
    {
        if (finishMyRankText == null) return;

        int score = ScoreManager.Instance != null ? ScoreManager.Instance.LeaderboardScore : 0;
        int rank  = remoteRank > 0 ? remoteRank : LeaderboardData.GetRank(score);

        string scoreLine = Loc.Get("str.summary.myscore", score);
        string rankLine  = remoteRank > 0 ? Loc.Get("str.summary.myrank", rank)        // 글로벌
                         : rank > 0       ? Loc.Get("str.summary.local_rank", rank)     // 로컬(이 기기)
                         :                  Loc.Get("str.summary.rank_out");            // 순위권 밖

        finishMyRankText.text = scoreLine + "  " + rankLine;
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
