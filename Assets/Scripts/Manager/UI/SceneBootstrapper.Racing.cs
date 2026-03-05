using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// 레이싱 HUD: 트랙 프로그레스 바, 타이머, 내 배팅 표시, 라운드 정보
/// </summary>
public partial class SceneBootstrapper
{
    // ── 포지션 컬러 (번호 고정, 캐릭터 무관) ──
    private static readonly Color[] POSITION_COLORS = new Color[]
    {
        new Color(0.90f, 0.15f, 0.15f),  // 1 = 빨강
        new Color(1.00f, 0.55f, 0.00f),  // 2 = 주황
        new Color(0.75f, 0.65f, 0.00f),  // 3 = 진한노랑
        new Color(0.15f, 0.70f, 0.15f),  // 4 = 초록
        new Color(0.20f, 0.45f, 0.90f),  // 5 = 파랑
        new Color(0.10f, 0.15f, 0.55f),  // 6 = 남색
        new Color(0.55f, 0.20f, 0.75f),  // 7 = 보라
        new Color(0.50f, 0.50f, 0.50f),  // 8 = 회색
        new Color(0.15f, 0.15f, 0.15f),  // 9 = 검정
        new Color(0.80f, 0.40f, 0.50f),  // 10 = 분홍 (여분)
        new Color(0.00f, 0.60f, 0.60f),  // 11 = 청록 (여분)
        new Color(0.60f, 0.30f, 0.10f),  // 12 = 갈색 (여분)
    };

    // ── 원형 스프라이트 캐시 (Unity 6 내장 Knob 없음 → 코드 생성) ──
    private static Sprite _circleSprite;
    private static Sprite CircleSprite
    {
        get
        {
            if (_circleSprite != null) return _circleSprite;
            int sz = 64;
            var tex = new Texture2D(sz, sz, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            float c = sz / 2f, r = sz / 2f - 1f;
            for (int y = 0; y < sz; y++)
                for (int x = 0; x < sz; x++)
                {
                    float d = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c));
                    tex.SetPixel(x, y, d <= r ? Color.white : Color.clear);
                }
            tex.Apply();
            _circleSprite = Sprite.Create(tex, new Rect(0, 0, sz, sz), new Vector2(0.5f, 0.5f), 100);
            _circleSprite.name = "GeneratedCircle";
            return _circleSprite;
        }
    }

    // ══════════════════════════════════════
    //  레이싱 HUD 빌드
    // ══════════════════════════════════════
    private void BuildRacingUI(Transform parent)
    {
        // ── 좌측 트랙 프로그레스 패널 ──
        BuildTrackProgressBar(parent);

        // ── 내 배팅 표시 (상단 중앙) ──
        myBetText = MkText(parent, "",
            new Vector2(0.5f, 1), new Vector2(0.5f, 1),
            new Vector2(0, -10), new Vector2(600, 30), 20, TextAnchor.MiddleCenter, new Color(1f, 0.9f, 0.3f));

        // ── 라운드+바퀴 (상단 우측) ──
        racingRoundText = MkText(parent, "",
            new Vector2(1, 1), new Vector2(1, 1),
            new Vector2(-15, -10), new Vector2(250, 30), 20, TextAnchor.MiddleRight, new Color(0.8f, 0.9f, 1f));

        // ── 타이머 ──
        raceTimerText = MkText(parent, Loc.Get("str.hud.timer", "0.0"),
            new Vector2(1, 1), new Vector2(1, 1),
            new Vector2(-15, -38), new Vector2(130, 30), 22, TextAnchor.MiddleRight, Color.white);
    }

    // ══════════════════════════════════════
    //  트랙 프로그레스 바 구축
    // ══════════════════════════════════════
    private void BuildTrackProgressBar(Transform parent)
    {
        int racerCount = GameConstants.RACER_COUNT;

        // ── 프리팹 모드 분기 (추후 디자인 적용 시 사용) ──
        var gs = GameSettings.Instance;
        if (gs != null && gs.trackProgressBarPrefab != null)
        {
            GameObject prefabInst = Object.Instantiate(gs.trackProgressBarPrefab, parent);
            prefabInst.name = "TrackProgressPanel";
            Transform prefabBar = prefabInst.transform.Find("TrackBarArea");
            if (prefabBar != null) trackBarRect = prefabBar as RectTransform ?? prefabBar.GetComponent<RectTransform>();
            racerCircles = new RacerCircleUI[racerCount];
            Sprite prefabKnob = CircleSprite;
            Transform circleParent = trackBarRect != null ? trackBarRect : prefabInst.transform;
            for (int i = racerCount - 1; i >= 0; i--)
                racerCircles[i] = CreateRacerCircle(circleParent, i, prefabKnob);
            return;
        }

        // ── 전체 패널 (좌측 ~8%) — 프로시저럴 생성 ──
        GameObject panel = new GameObject("TrackProgressPanel");
        panel.transform.SetParent(parent, false);
        RectTransform panelRt = panel.AddComponent<RectTransform>();
        panelRt.anchorMin = new Vector2(0.01f, 0.03f);
        panelRt.anchorMax = new Vector2(0.08f, 0.97f);
        panelRt.offsetMin = Vector2.zero;
        panelRt.offsetMax = Vector2.zero;

        // ── GOAL 라벨 (패널 상단) ──
        Text goalText = MkText(panel.transform, Loc.Get("str.hud.goal"),
            new Vector2(0.5f, 1), new Vector2(0.5f, 1),
            new Vector2(0, 0), new Vector2(120, 40),
            FontHelper.ScaledFontSize(36), TextAnchor.MiddleCenter, new Color(1f, 0.85f, 0.3f));
        goalText.resizeTextForBestFit = true;
        goalText.resizeTextMinSize = FontHelper.ScaledFontSize(14);
        goalText.resizeTextMaxSize = FontHelper.ScaledFontSize(36);

        // ── START 라벨 (패널 하단) ──
        Text startText = MkText(panel.transform, Loc.Get("str.hud.start"),
            new Vector2(0.5f, 0), new Vector2(0.5f, 0),
            new Vector2(0, 0), new Vector2(120, 40),
            FontHelper.ScaledFontSize(36), TextAnchor.MiddleCenter, new Color(0.7f, 0.9f, 1f));
        startText.resizeTextForBestFit = true;
        startText.resizeTextMinSize = FontHelper.ScaledFontSize(14);
        startText.resizeTextMaxSize = FontHelper.ScaledFontSize(36);

        // ── 트랙 바 영역 (GOAL/START 라벨 사이) ──
        GameObject barArea = new GameObject("TrackBarArea");
        barArea.transform.SetParent(panel.transform, false);
        trackBarRect = barArea.AddComponent<RectTransform>();
        trackBarRect.anchorMin = new Vector2(0, 0);
        trackBarRect.anchorMax = new Vector2(1, 1);
        // GOAL라벨(40px) 위, START라벨(40px) 아래 여백
        trackBarRect.offsetMin = new Vector2(0, 42);
        trackBarRect.offsetMax = new Vector2(0, -42);

        // ── 흰색 세로 막대 (중앙 4px) ──
        GameObject barBg = new GameObject("BarBg");
        barBg.transform.SetParent(barArea.transform, false);
        RectTransform barBgRt = barBg.AddComponent<RectTransform>();
        barBgRt.anchorMin = new Vector2(0.5f, 0);
        barBgRt.anchorMax = new Vector2(0.5f, 1);
        barBgRt.sizeDelta = new Vector2(6, 0);
        barBgRt.anchoredPosition = Vector2.zero;
        Image barBgImg = barBg.AddComponent<Image>();
        barBgImg.color = new Color(1, 1, 1, 0.7f);

        // ── 레이서 원형 마커 생성 (역순: 높은 번호 먼저 → 낮은 번호가 위에 그려짐) ──
        racerCircles = new RacerCircleUI[racerCount];
        Sprite circleSprt = CircleSprite;

        for (int i = racerCount - 1; i >= 0; i--)
        {
            racerCircles[i] = CreateRacerCircle(barArea.transform, i, circleSprt);
        }
    }

    // ══════════════════════════════════════
    //  레이서 원형 마커 생성
    // ══════════════════════════════════════
    private RacerCircleUI CreateRacerCircle(Transform parent, int index, Sprite knob)
    {
        RacerCircleUI circle;
        circle.racerIndex = index;
        // 루트
        GameObject root = new GameObject("RacerCircle_" + index);
        root.transform.SetParent(parent, false);
        circle.rect = root.AddComponent<RectTransform>();
        circle.rect.anchorMin = new Vector2(0.5f, 0);
        circle.rect.anchorMax = new Vector2(0.5f, 0);
        circle.rect.pivot = new Vector2(0.5f, 0.5f);
        circle.rect.sizeDelta = new Vector2(40, 40);
        circle.rect.anchoredPosition = Vector2.zero;

        // 테두리 이미지 (배팅 마커용, 기본 비활성)
        GameObject outlineObj = new GameObject("Outline");
        outlineObj.transform.SetParent(root.transform, false);
        RectTransform outlineRt = outlineObj.AddComponent<RectTransform>();
        outlineRt.anchorMin = Vector2.zero;
        outlineRt.anchorMax = Vector2.one;
        outlineRt.offsetMin = new Vector2(-5, -5);
        outlineRt.offsetMax = new Vector2(5, 5);
        circle.outlineImage = outlineObj.AddComponent<Image>();
        circle.outlineImage.sprite = knob;
        circle.outlineImage.color = new Color(1f, 0.9f, 0.3f, 0.9f);
        outlineObj.SetActive(false);

        // 원 이미지
        GameObject circleObj = new GameObject("Circle");
        circleObj.transform.SetParent(root.transform, false);
        RectTransform circleRt = circleObj.AddComponent<RectTransform>();
        circleRt.anchorMin = Vector2.zero;
        circleRt.anchorMax = Vector2.one;
        circleRt.offsetMin = Vector2.zero;
        circleRt.offsetMax = Vector2.zero;
        circle.circleImage = circleObj.AddComponent<Image>();
        circle.circleImage.sprite = knob;
        circle.circleImage.color = Color.white; // 초기 흰색

        // 번호 텍스트
        GameObject numObj = new GameObject("Num");
        numObj.transform.SetParent(root.transform, false);
        RectTransform numRt = numObj.AddComponent<RectTransform>();
        numRt.anchorMin = Vector2.zero;
        numRt.anchorMax = Vector2.one;
        numRt.offsetMin = Vector2.zero;
        numRt.offsetMax = Vector2.zero;
        circle.numberText = numObj.AddComponent<Text>();
        circle.numberText.font = font;
        circle.numberText.text = (index + 1).ToString();
        circle.numberText.fontSize = FontHelper.ScaledFontSize(18);
        circle.numberText.alignment = TextAnchor.MiddleCenter;
        circle.numberText.color = Color.gray; // 초기: 회색 번호 (흰 원 위)
        circle.numberText.resizeTextForBestFit = true;
        circle.numberText.resizeTextMinSize = FontHelper.ScaledFontSize(10);
        circle.numberText.resizeTextMaxSize = FontHelper.ScaledFontSize(18);

        return circle;
    }

    // ══════════════════════════════════════
    //  랩 구분선 동적 생성
    // ══════════════════════════════════════
    private void BuildLapDividers(int totalLaps)
    {
        // 기존 구분선 제거
        foreach (var d in lapDividers)
            if (d != null) Object.Destroy(d);
        lapDividers.Clear();

        if (totalLaps <= 1 || trackBarRect == null) return;

        for (int lap = 1; lap < totalLaps; lap++)
        {
            float yNorm = (float)lap / totalLaps;

            // 수평선 (충분히 두껍고 눈에 잘 보이게)
            GameObject divObj = new GameObject("LapDivider_" + lap);
            divObj.transform.SetParent(trackBarRect, false);
            RectTransform drt = divObj.AddComponent<RectTransform>();
            drt.anchorMin = new Vector2(0.5f, yNorm);
            drt.anchorMax = new Vector2(0.5f, yNorm);
            drt.sizeDelta = new Vector2(14, 6); // 짧고 굵은 사각형
            drt.anchoredPosition = Vector2.zero;
            Image dimg = divObj.AddComponent<Image>();
            dimg.color = new Color(1f, 0.85f, 0.3f, 0.85f); // 노란빛 + 높은 불투명도

            // 구분선은 바 위, 원 뒤에 그려지도록
            divObj.transform.SetAsFirstSibling();

            // 라벨 ("1)", "2)" ...)
            GameObject lblObj = new GameObject("LapLabel_" + lap);
            lblObj.transform.SetParent(trackBarRect, false);
            RectTransform lblRt = lblObj.AddComponent<RectTransform>();
            lblRt.anchorMin = new Vector2(1, yNorm);
            lblRt.anchorMax = new Vector2(1, yNorm);
            lblRt.pivot = new Vector2(0, 0.5f);
            lblRt.sizeDelta = new Vector2(30, 20);
            lblRt.anchoredPosition = new Vector2(6, 0);
            Text lblText = lblObj.AddComponent<Text>();
            lblText.font = font;
            lblText.text = lap + ")";
            lblText.fontSize = FontHelper.ScaledFontSize(12);
            lblText.alignment = TextAnchor.MiddleLeft;
            lblText.color = new Color(1f, 0.85f, 0.3f, 0.9f); // 노란빛
            lblText.resizeTextForBestFit = true;
            lblText.resizeTextMinSize = FontHelper.ScaledFontSize(8);
            lblText.resizeTextMaxSize = FontHelper.ScaledFontSize(12);

            lblObj.transform.SetAsFirstSibling();

            lapDividers.Add(divObj);
            lapDividers.Add(lblObj);
        }
    }

    // ══════════════════════════════════════
    //  레이싱 진입 시 트랙바 초기화
    // ══════════════════════════════════════
    private void InitTrackBarForRace()
    {
        if (racerCircles == null) return;

        // 배팅 정보 읽기
        myPickIndices.Clear();
        var bet = GameManager.Instance?.CurrentBet;
        if (bet != null)
            foreach (int s in bet.selections)
                myPickIndices.Add(s);

        // 랩 구분선 생성
        int totalLaps = RaceManager.Instance != null ? RaceManager.Instance.CurrentLaps : 1;
        BuildLapDividers(totalLaps);

        // 원 초기화: 포지션 컬러 적용 + 배팅 마커 테두리
        for (int i = 0; i < racerCircles.Length; i++)
        {
            var c = racerCircles[i];
            int idx = c.racerIndex;

            // 포지션 컬러 적용
            Color posColor = idx < POSITION_COLORS.Length ? POSITION_COLORS[idx] : Color.white;
            c.circleImage.color = posColor;

            // 번호 텍스트 대비 색상
            float luminance = 0.299f * posColor.r + 0.587f * posColor.g + 0.114f * posColor.b;
            c.numberText.color = luminance > 0.45f ? Color.black : Color.white;

            // 배팅 마커 테두리 활성화
            bool isBet = myPickIndices.Contains(idx);
            if (c.outlineImage != null)
                c.outlineImage.gameObject.SetActive(isBet);

            // 위치 초기화 (START)
            c.rect.anchoredPosition = Vector2.zero;
        }
    }

    // ══════════════════════════════════════
    //  매 프레임 트랙바 업데이트
    // ══════════════════════════════════════
    private void UpdateTrackProgressBar()
    {
        if (racerCircles == null || trackBarRect == null || RaceManager.Instance == null) return;

        var racers = RaceManager.Instance.Racers;
        float barHeight = trackBarRect.rect.height;
        if (barHeight <= 0) return;

        for (int i = 0; i < racerCircles.Length && i < racers.Count; i++)
        {
            var circle = racerCircles[i];
            if (circle.racerIndex >= racers.Count) continue;

            var racer = racers[circle.racerIndex];
            // 캐릭터의 실제 이동 거리 누적 → 바 높이로 직접 매핑 (별도 보간 불필요)
            float y = racer.SmoothProgress * barHeight;
            circle.rect.anchoredPosition = new Vector2(0, y);
        }

        // 배팅 마커를 최상위 z-order로
        for (int i = 0; i < racerCircles.Length; i++)
        {
            if (myPickIndices.Contains(racerCircles[i].racerIndex))
            {
                racerCircles[i].rect.SetAsLastSibling();
            }
        }
    }

    // ══════════════════════════════════════
    //  실시간 업데이트 (기존 호환)
    // ══════════════════════════════════════

    private void UpdateRoundInfo()
    {
        var gm = GameManager.Instance;
        if (gm == null) return;
        if (roundText != null)
            roundText.text = Loc.Get("str.hud.round", gm.CurrentRound, gm.TotalRounds);
        if (lapText != null)
            lapText.text = Loc.Get("str.hud.this_race", gm.CurrentRoundLaps);
    }

    private void UpdateRacingRoundInfo()
    {
        var gm = GameManager.Instance;
        if (gm == null) return;
        if (racingRoundText != null)
            racingRoundText.text = Loc.Get("str.hud.race_info", gm.CurrentRound, gm.TotalRounds, gm.CurrentRoundLaps);
    }

    private void UpdateScore()
    {
        if (scoreText != null && ScoreManager.Instance != null)
            scoreText.text = Loc.Get("str.hud.total_score", ScoreManager.Instance.CurrentGameScore);
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
        myBetText.text = Loc.Get("str.hud.my_bet", typeName, names);
    }
}
