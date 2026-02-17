using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// 레이싱 HUD: 순위판, 타이머, 내 배팅 표시, 라운드 정보
/// </summary>
public partial class SceneBootstrapper
{
    // ══════════════════════════════════════
    //  레이싱 HUD 빌드
    // ══════════════════════════════════════
    private void BuildRacingUI(Transform parent)
    {
        // 좌측 순위 패널
        GameObject rankPanel = new GameObject("RankPanel");
        rankPanel.transform.SetParent(parent, false);
        RectTransform rrt = rankPanel.AddComponent<RectTransform>();
        rrt.anchorMin = new Vector2(0, 0.02f);
        rrt.anchorMax = new Vector2(0.22f, 0.98f);
        rrt.offsetMin = Vector2.zero;
        rrt.offsetMax = Vector2.zero;
        rankPanel.AddComponent<Image>().color = new Color(0, 0, 0, 0.65f);

        VerticalLayoutGroup rvlg = rankPanel.AddComponent<VerticalLayoutGroup>();
        rvlg.spacing = 1;
        rvlg.padding = new RectOffset(8, 8, 8, 8);
        rvlg.childForceExpandWidth = true;
        rvlg.childForceExpandHeight = true;
        rvlg.childControlWidth = true;
        rvlg.childControlHeight = true;

        rankTexts = new Text[GameConstants.RACER_COUNT];
        for (int i = 0; i < GameConstants.RACER_COUNT; i++)
        {
            GameObject ro = new GameObject("R" + i);
            ro.transform.SetParent(rankPanel.transform, false);
            Text rt = ro.AddComponent<Text>();
            rt.font = font;
            rt.fontSize = 16;
            rt.color = Color.white;
            rt.alignment = TextAnchor.MiddleLeft;
            rt.resizeTextForBestFit = true;
            rt.resizeTextMinSize = 10;
            rt.resizeTextMaxSize = 16;
            rankTexts[i] = rt;
        }

        // 내 배팅 표시 (상단 중앙)
        myBetText = MkText(parent, "",
            new Vector2(0.5f, 1), new Vector2(0.5f, 1),
            new Vector2(0, -10), new Vector2(600, 30), 20, TextAnchor.MiddleCenter, new Color(1f, 0.9f, 0.3f));

        // 라운드+바퀴 (상단 우측)
        racingRoundText = MkText(parent, "",
            new Vector2(1, 1), new Vector2(1, 1),
            new Vector2(-15, -10), new Vector2(250, 30), 20, TextAnchor.MiddleRight, new Color(0.8f, 0.9f, 1f));

        // 타이머
        raceTimerText = MkText(parent, "0.0초",
            new Vector2(1, 1), new Vector2(1, 1),
            new Vector2(-15, -38), new Vector2(130, 30), 22, TextAnchor.MiddleRight, Color.white);
    }

    // ══════════════════════════════════════
    //  실시간 업데이트
    // ══════════════════════════════════════
    private void UpdateLiveRankings()
    {
        if (RaceManager.Instance == null || rankTexts == null) return;
        var live = RaceManager.Instance.GetLiveRankings();
        var bet = GameManager.Instance?.CurrentBet;
        int totalLaps = RaceManager.Instance.CurrentLaps;
        HashSet<int> myPicks = new HashSet<int>();
        if (bet != null)
            foreach (int s in bet.selections)
                myPicks.Add(s);

        for (int i = 0; i < rankTexts.Length && i < live.Count; i++)
        {
            var r = live[i];
            string mark = myPicks.Contains(r.RacerIndex) ? " ★" : "";
            string lapInfo = r.IsFinished ? "완주" : "[" + r.CurrentLap + "/" + totalLaps + "바퀴]";
            string spd = r.IsFinished ? "" : " x" + r.CurrentSpeed.ToString("F1");
            rankTexts[i].text = (i + 1) + "위 " + GameConstants.RACER_NAMES[r.RacerIndex] + " " + lapInfo + spd + mark;
            rankTexts[i].color = myPicks.Contains(r.RacerIndex) ? new Color(1f, 0.9f, 0.3f) : Color.white;
        }
    }

    private void UpdateRoundInfo()
    {
        var gm = GameManager.Instance;
        if (gm == null) return;
        if (roundText != null)
            roundText.text = "Round " + gm.CurrentRound + "/" + gm.TotalRounds;
        if (lapText != null)
            lapText.text = "이번 경기: " + gm.CurrentRoundLaps + "바퀴";
    }

    private void UpdateRacingRoundInfo()
    {
        var gm = GameManager.Instance;
        if (gm == null) return;
        if (racingRoundText != null)
            racingRoundText.text = "Round " + gm.CurrentRound + "/" + gm.TotalRounds + "  |  " + gm.CurrentRoundLaps + "바퀴";
    }

    private void UpdateScore()
    {
        if (scoreText != null && ScoreManager.Instance != null)
            scoreText.text = "총점: " + ScoreManager.Instance.TotalScore;
    }

    private void UpdateMyBet()
    {
        if (myBetText == null || GameManager.Instance == null) return;
        var bet = GameManager.Instance.CurrentBet;
        if (bet == null) return;

        string typeName = BettingCalculator.GetTypeName(bet.type);
        string names = "";
        for (int i = 0; i < bet.selections.Count; i++)
        {
            if (i > 0) names += ", ";
            names += bet.GetSelectionLabel(i) + ":" + GameConstants.RACER_NAMES[bet.selections[i]];
        }
        myBetText.text = "내 배팅 ▶ " + typeName + " | " + names;
    }
}
