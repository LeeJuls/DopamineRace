using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 공용 유틸리티: UI 생성 헬퍼, 화살표, 라벨 관리
/// </summary>
public partial class SceneBootstrapper
{
    // ══════════════════════════════════════
    //  캐릭터 라벨 관리
    // ══════════════════════════════════════
    private System.Collections.IEnumerator HideLabelsNextFrame()
    {
        yield return null;
        HideAllRaceLabels();
    }

    private void HideAllRaceLabels()
    {
        if (RaceManager.Instance == null) return;
        foreach (var r in RaceManager.Instance.Racers)
        {
            Transform lb = r.transform.Find("RaceLabel");
            if (lb == null) continue;

            // 원래 레이서 번호로 복원
            TextMesh tm = lb.GetComponent<TextMesh>();
            if (tm != null) tm.text = (r.RacerIndex + 1).ToString();

            lb.gameObject.SetActive(false);
        }
    }

    private void ShowAllRaceLabels()
    {
        if (RaceManager.Instance == null) return;

        // 배팅 타입에 따라 필요한 순위 수 결정
        int maxRankToShow = 8; // 기본: 전부
        var gm = GameManager.Instance;
        if (gm != null && gm.CurrentBet != null)
        {
            switch (gm.CurrentBet.type)
            {
                case BetType.Win:      maxRankToShow = 1; break; // 단승: 1등만
                case BetType.Place:    maxRankToShow = GameSettings.Instance.racerCount <= 7 ? 2 : 3; break; // 연승
                case BetType.Quinella: maxRankToShow = 2; break; // 복승: 1,2등
                case BetType.Exacta:   maxRankToShow = 2; break; // 쌍승: 1,2등
                case BetType.Trio:     maxRankToShow = 3; break; // 삼복승: 1,2,3등
                case BetType.Wide:     maxRankToShow = 3; break; // 복연승: 1,2,3등
            }
        }

        foreach (var r in RaceManager.Instance.Racers)
        {
            Transform lb = r.transform.Find("RaceLabel");
            if (lb == null) continue;

            TextMesh tm = lb.GetComponent<TextMesh>();
            if (tm == null) continue;

            // 완주 + 해당 배팅에 필요한 순위만 표시
            if (r.IsFinished && r.FinishOrder >= 1 && r.FinishOrder <= maxRankToShow)
            {
                tm.text = r.FinishOrder.ToString();
                lb.gameObject.SetActive(true);
            }
            else
            {
                lb.gameObject.SetActive(false);
            }
        }
    }

    // ══════════════════════════════════════
    //  선택 캐릭터 화살표
    // ══════════════════════════════════════
    private void UpdateBettingArrows()
    {
        DestroyArrows();
        HideAllRaceLabels();
        if (RaceManager.Instance == null || GameManager.Instance == null) return;
        var racers = RaceManager.Instance.Racers;
        var bet = GameManager.Instance.CurrentBet;
        if (bet == null || bet.selections.Count == 0) return;

        Color[] arrowColors = { Color.red, new Color(0.3f, 0.6f, 1f), new Color(0.2f, 0.8f, 0.2f) };

        for (int i = 0; i < bet.selections.Count; i++)
        {
            int sel = bet.selections[i];
            if (sel >= 0 && sel < racers.Count)
            {
                string selLabel = bet.GetSelectionLabel(i);
                string racerName = GameConstants.RACER_NAMES[sel];
                string arrowLabel = selLabel + ":" + racerName;
                Color c = arrowColors[Mathf.Min(i, arrowColors.Length - 1)];
                GameObject arrow = MakeArrow(racers[sel].transform, arrowLabel, c);
                betArrows.Add(arrow);
            }
        }
    }

    private GameObject MakeArrow(Transform parent, string label, Color color)
    {
        GameObject obj = new GameObject("Arrow");
        obj.transform.SetParent(parent);
        obj.transform.localPosition = new Vector3(0, GameSettings.Instance.labelHeight + GameSettings.Instance.betMarkerHeight, 0);
        obj.transform.localScale = Vector3.one;

        TextMesh tm = obj.AddComponent<TextMesh>();
        tm.text = label + "\n▼";
        tm.alignment = TextAlignment.Center;
        tm.anchor = TextAnchor.LowerCenter;
        tm.characterSize = GameSettings.Instance.betMarkerSize;
        tm.fontSize = 48;
        tm.fontStyle = FontStyle.Bold;
        tm.color = color;
        obj.GetComponent<MeshRenderer>().sortingOrder = 50;

        return obj;
    }

    private void DestroyArrows()
    {
        foreach (var arrow in betArrows)
        {
            if (arrow != null)
                Destroy(arrow);
        }
        betArrows.Clear();
    }

    private void UpdateArrowPositions()
    {
        var s = GameSettings.Instance;
        float bounce = Mathf.Sin(Time.time * 4f) * 0.08f;
        float h = s.labelHeight + s.betMarkerHeight;

        foreach (var arrow in betArrows)
        {
            if (arrow != null)
            {
                arrow.transform.localPosition = new Vector3(0, h + bounce, 0);
                TextMesh tm = arrow.GetComponent<TextMesh>();
                if (tm != null) tm.characterSize = s.betMarkerSize;
            }
        }
    }

    // ══════════════════════════════════════
    //  UI 생성 헬퍼
    // ══════════════════════════════════════
    private RectTransform AddFullRect(GameObject obj)
    {
        RectTransform rt = obj.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        return rt;
    }

    private Text MkText(Transform parent, string text,
        Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size,
        int fontSize, TextAnchor align, Color? color = null)
    {
        GameObject o = new GameObject("T");
        o.transform.SetParent(parent, false);
        RectTransform rt = o.AddComponent<RectTransform>();
        rt.anchorMin = anchor; rt.anchorMax = anchor; rt.pivot = pivot;
        rt.anchoredPosition = pos; rt.sizeDelta = size;

        Text t = o.AddComponent<Text>();
        t.font = font; t.text = text; t.fontSize = fontSize;
        t.alignment = align; t.color = color ?? Color.white;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        t.supportRichText = true;
        return t;
    }
}