using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 라운드 결과 화면: 순위, 배팅 결과, 점수
/// </summary>
public partial class SceneBootstrapper
{
    private void BuildResultUI(Transform parent)
    {
        Image bg = parent.gameObject.AddComponent<Image>();
        bg.color = new Color(0, 0, 0, 0.75f);

        resultTitleText = MkText(parent, "레이스 결과",
            new Vector2(0.5f, 0.8f), new Vector2(0.5f, 0.8f),
            Vector2.zero, new Vector2(500, 60), 42, TextAnchor.MiddleCenter, new Color(1f, 0.85f, 0.2f));

        resultDetailText = MkText(parent, "",
            new Vector2(0.5f, 0.55f), new Vector2(0.5f, 0.55f),
            Vector2.zero, new Vector2(600, 200), 22, TextAnchor.MiddleCenter, Color.white);

        resultScoreText = MkText(parent, "",
            new Vector2(0.5f, 0.35f), new Vector2(0.5f, 0.35f),
            Vector2.zero, new Vector2(400, 50), 32, TextAnchor.MiddleCenter, Color.yellow);

        // 다음 라운드 버튼
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

        nextRoundBtnText = MkText(nb.transform, "다음 라운드",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(250, 60), 26, TextAnchor.MiddleCenter, Color.white);
    }

    private void ShowResult()
    {
        var rankings = RaceManager.Instance?.GetFinalRankings();
        if (rankings == null || rankings.Count < 3) return;

        var gm = GameManager.Instance;
        var bet = gm?.CurrentBet;
        int score = ScoreManager.Instance?.LastRoundScore ?? 0;

        resultTitleText.text = score > 0 ? "적중!" : "아쉽네요...";
        resultTitleText.color = score > 0 ? new Color(1f, 0.85f, 0.2f) : new Color(0.7f, 0.7f, 0.7f);

        string detail = "";
        detail += "1착: " + rankings[0].racerName + "  /  2착: " + rankings[1].racerName + "  /  3착: " + rankings[2].racerName + "\n\n";

        if (bet != null)
        {
            string typeName = BettingCalculator.GetTypeName(bet.type);
            detail += "배팅: " + typeName + " (" + BettingCalculator.GetTypeDesc(bet.type) + ")\n";

            for (int i = 0; i < bet.selections.Count; i++)
            {
                int sel = bet.selections[i];
                string selName = GameConstants.RACER_NAMES[sel];
                string label = bet.GetSelectionLabel(i);

                int actualRank = -1;
                for (int r = 0; r < rankings.Count; r++)
                {
                    if (rankings[r].racerIndex == sel) { actualRank = r + 1; break; }
                }
                detail += "내 " + label + " 예측: " + selName + " → 실제 " + actualRank + "착\n";
            }
        }

        resultDetailText.text = detail;

        int totalScore = ScoreManager.Instance?.CurrentGameScore ?? 0;
        resultScoreText.text = score > 0
            ? "+" + score + "점 획득!  (총점: " + totalScore + ")"
            : "0점  (총점: " + totalScore + ")";
        resultScoreText.color = score > 0 ? Color.yellow : new Color(0.7f, 0.7f, 0.7f);

        if (nextRoundBtnText != null)
        {
            if (gm != null && gm.IsLastRound)
                nextRoundBtnText.text = "새 게임";
            else
                nextRoundBtnText.text = "다음 라운드";
        }
    }
}