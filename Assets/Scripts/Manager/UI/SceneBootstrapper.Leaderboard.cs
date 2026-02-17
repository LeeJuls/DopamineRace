using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Top 100 리더보드 팝업
/// </summary>
public partial class SceneBootstrapper
{
    private void BuildLeaderboardPopup(Transform parent)
    {
        Image bg = parent.gameObject.AddComponent<Image>();
        bg.color = new Color(0, 0, 0, 0.9f);

        leaderboardTitleText = MkText(parent, "Top 100 리더보드",
            new Vector2(0.5f, 0.95f), new Vector2(0.5f, 0.95f),
            Vector2.zero, new Vector2(500, 50), 34, TextAnchor.MiddleCenter, new Color(1f, 0.85f, 0.2f));

        MkText(parent, "<b>순위    점수    적중    날짜              요약</b>",
            new Vector2(0.5f, 0.89f), new Vector2(0.5f, 0.89f),
            Vector2.zero, new Vector2(800, 30), 16, TextAnchor.MiddleCenter, new Color(0.8f, 0.8f, 0.8f));

        leaderboardContentText = MkText(parent, "",
            new Vector2(0.5f, 0.52f), new Vector2(0.5f, 0.52f),
            Vector2.zero, new Vector2(800, 600), 15, TextAnchor.UpperCenter, Color.white);
        leaderboardContentText.verticalOverflow = VerticalWrapMode.Overflow;

        // 닫기 버튼
        GameObject closeBtn = new GameObject("CloseBtn");
        closeBtn.transform.SetParent(parent, false);
        RectTransform crt = closeBtn.AddComponent<RectTransform>();
        crt.anchorMin = new Vector2(0.5f, 0.03f);
        crt.anchorMax = new Vector2(0.5f, 0.03f);
        crt.pivot = new Vector2(0.5f, 0.5f);
        crt.sizeDelta = new Vector2(200, 50);

        closeBtn.AddComponent<Image>().color = new Color(0.5f, 0.3f, 0.3f);
        Button cb = closeBtn.AddComponent<Button>();
        cb.onClick.AddListener(() => leaderboardPopup.SetActive(false));
        MkText(closeBtn.transform, "닫기",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(200, 50), 24, TextAnchor.MiddleCenter, Color.white);
    }

    private void ShowLeaderboard()
    {
        var entries = LeaderboardData.GetTop(100);
        string content = "";

        if (entries.Count == 0)
        {
            content = "\n\n\n기록이 없습니다.\n게임을 완료하면 자동으로 기록됩니다.";
        }
        else
        {
            for (int i = 0; i < entries.Count; i++)
            {
                var e = entries[i];
                string rank = (i + 1).ToString().PadLeft(3);
                string score = e.score.ToString().PadLeft(6);
                string rounds = e.rounds + "R";
                string date = e.date;

                string summary = e.summary;
                if (summary.Length > 40) summary = summary.Substring(0, 40) + "...";

                bool isGold = i < 3;
                string line = rank + "위  " + score + "점  " + rounds + "  " + date + "  " + summary;

                if (isGold)
                    content += "<color=#FFD700>" + line + "</color>\n";
                else
                    content += line + "\n";
            }
        }

        leaderboardContentText.text = content;
        leaderboardPopup.SetActive(true);
    }
}
