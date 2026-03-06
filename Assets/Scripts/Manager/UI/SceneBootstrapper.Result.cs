using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 라운드 결과 화면: 순위(캐릭터 아이콘+이름) + 배팅 결과 + 점수
/// ResultPanel.prefab 기반 (ResultUIPrefabCreator로 생성/패치)
/// </summary>
public partial class SceneBootstrapper
{
    private static readonly Color COLOR_RESULT_WIN  = new Color(1f, 0.85f, 0.2f, 1f);
    private static readonly Color COLOR_RESULT_LOSE = new Color(0.7f, 0.7f, 0.7f, 1f);
    private static readonly Color COLOR_PICK_HIT    = new Color(0.4f, 1f,  0.4f,  1f);
    private static readonly Color COLOR_PICK_MISS   = new Color(1f,  0.4f, 0.4f,  1f);

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

    /// <summary>프리팹 내부 요소 참조 캐싱</summary>
    private void CacheResultUIReferences(Transform root)
    {
        // TitleText
        resultTitleText = FindText(root, "TitleText");

        // RankSection
        Transform rankSection = root.Find("RankSection");
        if (rankSection != null)
        {
            for (int i = 0; i < MAX_RANK_ROWS; i++)
            {
                Transform row = rankSection.Find("Rank" + (i + 1) + "Row");
                if (row != null)
                {
                    Transform iconT = row.Find("CharIcon");
                    if (iconT != null) resultRankIcons[i] = iconT.GetComponent<Image>();

                    Transform nameT = row.Find("CharName");
                    if (nameT != null) resultRankNames[i] = nameT.GetComponent<Text>();
                }
            }
        }

        // BetResultSection
        Transform betSection = root.Find("BetResultSection");
        if (betSection != null)
        {
            resultBetTypeLabel = FindText(betSection, "BetTypeLabel");

            for (int i = 0; i < 3; i++)
            {
                Transform row = betSection.Find("Pick" + (i + 1) + "Row");
                if (row != null)
                {
                    resultPickRows[i] = row.gameObject;

                    Transform lblT = row.Find("LabelText");
                    if (lblT != null) resultPickLabels[i] = lblT.GetComponent<Text>();

                    Transform nameT = row.Find("PickName");
                    if (nameT != null) resultPickNames[i] = nameT.GetComponent<Text>();

                    Transform resultT = row.Find("PickResult");
                    if (resultT != null) resultPickResults[i] = resultT.GetComponent<Text>();
                }
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
        if (rankings == null || rankings.Count < 3) return;

        var gm  = GameManager.Instance;
        var bet = gm?.CurrentBet;
        int score = ScoreManager.Instance?.LastRoundScore ?? 0;

        // ── TitleText ──
        if (resultTitleText != null)
        {
            resultTitleText.text  = score > 0 ? Loc.Get("str.result.win") : Loc.Get("str.result.lose");
            resultTitleText.color = score > 0 ? COLOR_RESULT_WIN : COLOR_RESULT_LOSE;
        }

        // ── RankSection: 전체 순위 캐릭터 아이콘 + 이름 ──
        var db = CharacterDatabase.Instance;
        for (int i = 0; i < MAX_RANK_ROWS && i < rankings.Count; i++)
        {
            int racerIdx = rankings[i].racerIndex;

            CharacterData charData = null;
            if (db != null && racerIdx >= 0 && racerIdx < db.AllCharacters.Count)
                charData = db.AllCharacters[racerIdx];

            if (resultRankIcons[i] != null)
            {
                if (charData != null)
                {
                    Sprite spr = charData.LoadIcon();
                    if (spr != null)
                    {
                        resultRankIcons[i].sprite = spr;
                        resultRankIcons[i].color = Color.white;
                    }
                    else
                    {
                        resultRankIcons[i].sprite = null;
                        resultRankIcons[i].color = new Color(0.3f, 0.3f, 0.3f);
                    }
                }
            }

            if (resultRankNames[i] != null)
            {
                string displayName = charData != null
                    ? charData.DisplayName
                    : rankings[i].racerName;
                resultRankNames[i].text = displayName;
            }
        }

        // ── BetResultSection ──
        if (resultBetTypeLabel != null && bet != null)
        {
            string typeName = BettingCalculator.GetTypeName(bet.type);
            string typeDesc = BettingCalculator.GetTypeDesc(bet.type);
            resultBetTypeLabel.text = Loc.Get("str.result.bet_type", typeName, typeDesc);
        }
        else if (resultBetTypeLabel != null)
        {
            resultBetTypeLabel.text = "";
        }

        // Pick Row
        for (int i = 0; i < 3; i++)
        {
            bool active = bet != null && i < bet.selections.Count;
            if (resultPickRows[i] != null)
                resultPickRows[i].SetActive(active);

            if (!active) continue;

            int selIdx = bet.selections[i];

            if (resultPickLabels[i] != null)
                resultPickLabels[i].text = bet.GetSelectionLabel(i) + ":";

            CharacterData selChar = null;
            if (db != null && selIdx >= 0 && selIdx < db.AllCharacters.Count)
                selChar = db.AllCharacters[selIdx];
            if (resultPickNames[i] != null)
                resultPickNames[i].text = selChar != null
                    ? selChar.DisplayName
                    : GameConstants.RACER_NAMES[selIdx];

            int actualRank = -1;
            for (int r = 0; r < rankings.Count; r++)
            {
                if (rankings[r].racerIndex == selIdx) { actualRank = r + 1; break; }
            }
            if (resultPickResults[i] != null)
            {
                bool isHit = score > 0;
                resultPickResults[i].text  = actualRank > 0 ? "→ " + GetOrdinal(actualRank) : "→ ?";
                resultPickResults[i].color = isHit ? COLOR_PICK_HIT : COLOR_PICK_MISS;
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

