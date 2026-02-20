using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 최종 결산 화면: 라운드별 결과 테이블, 총점, 새 게임/리더보드 버튼
/// </summary>
public partial class SceneBootstrapper
{
    private void BuildFinishUI(Transform parent)
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
        Button ngb = newGameBtn.AddComponent<Button>();
        ngb.onClick.AddListener(() =>
        {
            GameManager.Instance?.StartNewGame();
        });
        MkText(newGameBtn.transform, Loc.Get("str.btn.new_game"),
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
        Button t100b = top100Btn.AddComponent<Button>();
        t100b.onClick.AddListener(() => ShowLeaderboard());
        MkText(top100Btn.transform, Loc.Get("str.btn.top100"),
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(220, 55), 26, TextAnchor.MiddleCenter, Color.white);
    }

    private void ShowFinish()
    {
        var sm = ScoreManager.Instance;
        if (sm == null) return;

        string detail = "<b>" + Loc.Get("str.finish.round_header") + "</b>\n";
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

        finishRoundDetailText.text = detail;

        int total = sm.CurrentGameScore;
        int wins = 0;
        foreach (var r in sm.RoundHistory)
            if (r.isWin) wins++;

        finishTotalScoreText.text = Loc.Get("str.finish.total", total, wins, sm.RoundHistory.Count);
    }
}
