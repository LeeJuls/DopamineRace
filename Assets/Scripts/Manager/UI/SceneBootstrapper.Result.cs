using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 라운드 결과 화면: 전체 순위(배지+아이콘+이름+화살표) + 점수
/// ResultPanel.prefab 기반 (ResultUIPrefabCreator로 생성/패치)
///
/// 구조 변경 (v2):
///  - BetResultSection 제거
///  - RankSection에 전체 N위 표시 (최대 12행)
///  - 내 픽 캐릭터의 실제 순위 행에 ← 화살표 + 레이블 표시
/// </summary>
public partial class SceneBootstrapper
{
    private static readonly Color COLOR_RESULT_WIN  = new Color(1f, 0.85f, 0.2f, 1f);
    private static readonly Color COLOR_RESULT_LOSE = new Color(0.7f, 0.7f, 0.7f, 1f);
    private static readonly Color COLOR_PICK_HIT    = new Color(0.4f, 1f,  0.4f,  1f);
    private static readonly Color COLOR_PICK_MISS   = new Color(1f,  0.4f, 0.4f,  1f);

    // 배지 색상
    private static readonly Color COLOR_BADGE_GOLD   = new Color(1f,   0.84f, 0f,   1f);
    private static readonly Color COLOR_BADGE_SILVER = new Color(0.75f, 0.75f, 0.75f, 1f);
    private static readonly Color COLOR_BADGE_BRONZE = new Color(0.80f, 0.50f, 0.20f, 1f);
    private static readonly Color COLOR_BADGE_GRAY   = new Color(0.45f, 0.45f, 0.45f, 1f);

    // ══════════════════════════════════════
    //  UI 구축 (프리팹 기반)
    // ══════════════════════════════════════
    private void BuildResultUI(Transform parent)
    {
        var gs = GameSettings.Instance;

        if (gs != null && gs.resultPanelPrefab != null)
        {
            GameObject panel = Instantiate(gs.resultPanelPrefab, parent);
            panel.name = "ResultPanelInstance";
            resultPanelRoot = panel.transform;
            CacheResultUIReferences(resultPanelRoot);
            Debug.Log("[SceneBootstrapper] ResultPanel 프리팹 기반 초기화 완료");
        }
        else
        {
            Debug.LogWarning("[SceneBootstrapper] resultPanelPrefab 미설정 → 레거시 빌드로 대체. " +
                             "DopamineRace > Create Result UI Prefabs 실행 후 GameSettings에 연결하세요.");
            BuildResultUILegacy(parent);
        }
    }

    /// <summary>프리팹 내부 요소 참조 캐싱 (v3: 9행 고정 + PickArrow)</summary>
    private void CacheResultUIReferences(Transform root)
    {
        // TitleText
        resultTitleText = FindText(root, "TitleText");

        // RankSection — 전체 N위 (최대 12)
        Transform rankSection = root.Find("RankSection");
        if (rankSection != null)
        {
            // SectionLabel — str.result.rank_header (언어 전환 시 ShowResult에서 갱신)
            Transform sectionLblT = rankSection.Find("SectionLabel");
            if (sectionLblT != null) resultSectionLabel = sectionLblT.GetComponent<Text>();

            for (int i = 0; i < MAX_RANK_ROWS; i++)
            {
                Transform row = rankSection.Find("Rank" + (i + 1) + "Row");
                if (row == null) break;

                resultRankRows[i] = row.gameObject;

                // CharIconMask > CharIcon 구조 (v4: 얼굴 크롭) / legacy fallback
                Transform iconT = row.Find("CharIconMask/CharIcon");
                if (iconT == null) iconT = row.Find("CharIcon");
                if (iconT != null) resultRankIcons[i] = iconT.GetComponent<Image>();

                Transform nameT = row.Find("CharName");
                if (nameT != null) resultRankNames[i] = nameT.GetComponent<Text>();

                Transform badgeT = row.Find("RankBadge");
                if (badgeT != null)
                {
                    Transform badgeTextT = badgeT.Find("BadgeText");
                    if (badgeTextT != null) resultRankBadges[i] = badgeTextT.GetComponent<Text>();
                }

                Transform arrowT = row.Find("PickArrow");
                if (arrowT != null) resultRankArrows[i] = arrowT.GetComponent<Text>();
            }
        }

        // ScoreSection
        Transform scoreSection = root.Find("ScoreSection");
        if (scoreSection != null)
        {
            resultScoreFormulaText = FindText(scoreSection, "ScoreFormulaText");
            resultTotalScoreText   = FindText(scoreSection, "TotalScoreText");
        }

        // NextRoundBtn
        Transform btnT = root.Find("NextRoundBtn");
        if (btnT != null)
        {
            nextRoundButton = btnT.GetComponent<Button>();
            if (nextRoundButton != null)
                nextRoundButton.onClick.AddListener(() => GameManager.Instance?.NextRound());
            nextRoundBtnText = FindText(btnT, "BtnText");
        }
    }

    // ══════════════════════════════════════
    //  결과 표시
    // ══════════════════════════════════════
    private void ShowResult()
    {
        var rankings = RaceManager.Instance?.GetFinalRankings();
        if (rankings == null || rankings.Count == 0) return;

        var gm  = GameManager.Instance;
        var bet = gm?.CurrentBet;
        int score = ScoreManager.Instance?.LastRoundScore ?? 0;
        int totalRacers = rankings.Count;

        // ── TitleText ──
        if (resultTitleText != null)
        {
            resultTitleText.text  = score > 0 ? Loc.Get("str.result.win") : Loc.Get("str.result.lose");
            resultTitleText.color = score > 0 ? COLOR_RESULT_WIN : COLOR_RESULT_LOSE;
        }

        // ── SectionLabel (str.result.rank_header) ──
        if (resultSectionLabel != null)
            resultSectionLabel.text = Loc.Get("str.result.rank_header");

        // ── RankSection: 전체 순위 표시 (9행 고정, 항상 전부 활성) ──
        var db = CharacterDatabase.Instance;
        for (int i = 0; i < MAX_RANK_ROWS; i++)
        {
            if (resultRankRows[i] == null) continue;
            if (i >= totalRacers) continue; // 혹시 레이서 수가 9 미만일 때만 방어

            int racerIdx = rankings[i].racerIndex;

            // SelectedCharacters 우선 (실제 게임: racerIdx = 선발 순서 인덱스)
            // fallback AllCharacters (F8/F9/F10 디버그: racerIdx = AllCharacters 인덱스)
            CharacterData charData = null;
            if (db != null && racerIdx >= 0)
            {
                if (racerIdx < db.SelectedCharacters.Count)
                    charData = db.SelectedCharacters[racerIdx];
                else if (racerIdx < db.AllCharacters.Count)
                    charData = db.AllCharacters[racerIdx];
            }

            // 배지 텍스트: 동적으로 서수 표기 (1위/1st/1着)
            if (resultRankBadges[i] != null)
                resultRankBadges[i].text = GetOrdinal(i + 1);

            // 배지 배경색 동적 설정 (RankBadge Image)
            if (resultRankRows[i] != null)
            {
                Transform badgeT = resultRankRows[i].transform.Find("RankBadge");
                if (badgeT != null)
                {
                    Image badgeImg = badgeT.GetComponent<Image>();
                    if (badgeImg != null)
                    {
                        if (i == 0) badgeImg.color = COLOR_BADGE_GOLD;
                        else if (i == 1) badgeImg.color = COLOR_BADGE_SILVER;
                        else if (i == 2) badgeImg.color = COLOR_BADGE_BRONZE;
                        else badgeImg.color = COLOR_BADGE_GRAY;
                    }
                }
            }

            // 아이콘 (RectMask2D 얼굴 크롭 — AspectRatioFitter로 비율 자동 조정)
            if (resultRankIcons[i] != null)
            {
                if (charData != null)
                {
                    Sprite spr = charData.LoadIcon();
                    if (spr != null)
                    {
                        resultRankIcons[i].sprite = spr;
                        resultRankIcons[i].color  = Color.white;
                        // 스프라이트 실제 비율로 AspectRatioFitter 갱신
                        // → 너비(30px) 고정, 높이를 비율에 맞게 늘려 마스크 밖으로 뻗게 함
                        var fitter = resultRankIcons[i].GetComponent<AspectRatioFitter>();
                        if (fitter != null && spr.rect.height > 0f)
                            fitter.aspectRatio = spr.rect.width / spr.rect.height;
                    }
                    else
                    {
                        resultRankIcons[i].sprite = null;
                        resultRankIcons[i].color  = new Color(0.3f, 0.3f, 0.3f);
                    }
                }
            }

            // 이름
            if (resultRankNames[i] != null)
            {
                string displayName = charData != null
                    ? charData.DisplayName
                    : rankings[i].racerName;
                resultRankNames[i].text = displayName;
            }

            // 화살표 기본 숨김
            if (resultRankArrows[i] != null)
                resultRankArrows[i].gameObject.SetActive(false);
        }

        // ── 내 픽 → 화살표 표시 ──
        bool isHit = score > 0;

        if (bet != null)
        {
            for (int si = 0; si < bet.selections.Count; si++)
            {
                int selIdx = bet.selections[si];

                // 이 캐릭터의 실제 도달 순위 찾기
                int actualRank = -1;
                for (int r = 0; r < rankings.Count; r++)
                {
                    if (rankings[r].racerIndex == selIdx)
                    {
                        actualRank = r;
                        break;
                    }
                }

                if (actualRank >= 0 && actualRank < MAX_RANK_ROWS && resultRankArrows[actualRank] != null)
                {
                    var arrowText = resultRankArrows[actualRank];
                    arrowText.gameObject.SetActive(true);
                    arrowText.text  = "← " + bet.GetSelectionLabel(si);
                    arrowText.color = isHit ? COLOR_PICK_HIT : COLOR_PICK_MISS;
                }
            }
        }

        // ── ScoreSection ──
        if (resultScoreFormulaText != null)
        {
            if (score > 0 && bet != null)
            {
                // basePt × odds = score (역산으로 배율 표시)
                int basePt = BettingCalculator.GetPayout(bet.type);
                float odds = basePt > 0 ? (float)score / basePt : 1f;
                resultScoreFormulaText.text = string.Format(
                    "<color=#FFE000>{0}</color> × <color=#FF8C00>{1}</color> = {2}",
                    basePt, odds.ToString("F1"), score);
            }
            else
            {
                resultScoreFormulaText.text = "";
            }
        }

        int totalScore = ScoreManager.Instance?.CurrentGameScore ?? 0;
        if (resultTotalScoreText != null)
        {
            resultTotalScoreText.text = score > 0
                ? Loc.Get("str.result.score_win", score, totalScore)
                : Loc.Get("str.result.score_lose", totalScore);
            resultTotalScoreText.color = score > 0 ? COLOR_RESULT_WIN : COLOR_RESULT_LOSE;
        }

        // ── 버튼 텍스트 ──
        if (nextRoundBtnText != null)
        {
            nextRoundBtnText.text = (gm != null && gm.IsLastRound)
                ? Loc.Get("str.ui.btn.new_game")
                : Loc.Get("str.ui.btn.next_round");
        }
    }

    /// <summary>순위 숫자 → 현재 언어에 맞는 서수 표기 (예: 1위, 1st)</summary>
    private string GetOrdinal(int rank)
    {
        string lang = Loc.CurrentLang;
        if (lang == "en")
        {
            if (rank == 1) return "1st";
            if (rank == 2) return "2nd";
            if (rank == 3) return "3rd";
            return rank + "th";
        }
        if (lang == "jp") return rank + "着";
        // 한국어/중국어/기타
        return rank + "위";
    }

    // ══════════════════════════════════════
    //  레거시 빌드 (프리팹없을 때 fallback)
    // ══════════════════════════════════════
    private void BuildResultUILegacy(Transform parent)
    {
        Image bg = parent.gameObject.AddComponent<Image>();
        bg.color = new Color(0, 0, 0, 0.75f);

        resultTitleText = MkText(parent, Loc.Get("str.result.title"),
            new Vector2(0.5f, 0.8f), new Vector2(0.5f, 0.8f),
            Vector2.zero, new Vector2(500, 60), 42, TextAnchor.MiddleCenter, COLOR_RESULT_WIN);

        Text legacyDetail = MkText(parent, "",
            new Vector2(0.5f, 0.55f), new Vector2(0.5f, 0.55f),
            Vector2.zero, new Vector2(600, 200), 22, TextAnchor.MiddleCenter, Color.white);

        resultTotalScoreText = MkText(parent, "",
            new Vector2(0.5f, 0.35f), new Vector2(0.5f, 0.35f),
            Vector2.zero, new Vector2(400, 50), 32, TextAnchor.MiddleCenter, Color.yellow);

        GameObject nb = new GameObject("NextBtn");
        nb.transform.SetParent(parent, false);
        RectTransform nrt = nb.AddComponent<RectTransform>();
        nrt.anchorMin = new Vector2(0.5f, 0.15f);
        nrt.anchorMax = new Vector2(0.5f, 0.15f);
        nrt.pivot = new Vector2(0.5f, 0.5f);
        nrt.sizeDelta = new Vector2(250, 60);
        nb.AddComponent<Image>().color = new Color(0.25f, 0.5f, 0.9f);
        nextRoundButton = nb.AddComponent<Button>();
        nextRoundButton.onClick.AddListener(() => GameManager.Instance?.NextRound());
        nextRoundBtnText = MkText(nb.transform, Loc.Get("str.ui.btn.next_round"),
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(250, 60), 26, TextAnchor.MiddleCenter, Color.white);
    }
}
