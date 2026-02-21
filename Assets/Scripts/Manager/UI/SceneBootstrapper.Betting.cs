using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 배팅 UI: 프리팹 기반 패널 빌드, 타입 탭, 캐릭터 선택, 배당/트랙 표시
/// </summary>
public partial class SceneBootstrapper
{
    // ══════════════════════════════════════
    //  배팅 화면 빌드 (프리팹 Instantiate)
    // ══════════════════════════════════════
    private void BuildBettingUI(Transform parent)
    {
        var gs = GameSettings.Instance;

        if (gs.bettingPanelPrefab != null)
        {
            // ── 프리팹 기반 ──
            GameObject panel = Instantiate(gs.bettingPanelPrefab, parent);
            bettingPanelRoot = panel.transform;
            CacheBettingUIReferences(bettingPanelRoot);
            InitBetTypeTabs();
            InitCharacterList();
            InitTrackInfo();
            InitOddsDisplay();
            InitHideInfoToggle();
            InitCharacterInfoPopup();
        }
        else
        {
            // ── 프리팹 미생성 시 최소 fallback ──
            Debug.LogWarning("[SceneBootstrapper] bettingPanelPrefab이 없습니다! " +
                "Unity 메뉴 DopamineRace > Create Betting UI Prefabs 실행 후 GameSettings에 연결해주세요.");
            BuildBettingUIFallback(parent);
        }

        // === 우상단 정보 (프리팹 밖 — 항상 표시) ===
        roundText = MkText(parent, Loc.Get("str.hud.round", 1, 7),
            new Vector2(1, 1), new Vector2(1, 1),
            new Vector2(-20, -10), new Vector2(250, 30), 22, TextAnchor.MiddleRight, Color.white);

        lapText = MkText(parent, Loc.Get("str.hud.this_race", 1),
            new Vector2(1, 1), new Vector2(1, 1),
            new Vector2(-20, -38), new Vector2(250, 25), 18, TextAnchor.MiddleRight, new Color(0.8f, 0.9f, 1f));

        scoreText = MkText(parent, Loc.Get("str.hud.total_score", 0),
            new Vector2(1, 1), new Vector2(1, 1),
            new Vector2(-20, -62), new Vector2(250, 25), 20, TextAnchor.MiddleRight, Color.yellow);
    }

    // ══════════════════════════════════════
    //  프리팹 내부 참조 캐싱
    // ══════════════════════════════════════
    private void CacheBettingUIReferences(Transform root)
    {
        // TopArea
        Transform topArea = root.Find("TopArea");
        if (topArea != null)
        {
            betRoundText = FindText(topArea, "RoundText");
            betTitleText = FindText(topArea, "TitleText");
            betDescText  = FindText(topArea, "BetDescText");

            Transform oddsArea = topArea.Find("OddsArea");
            if (oddsArea != null)
            {
                oddsText          = FindText(oddsArea, "OddsText");
                pointsLabelText   = FindText(oddsArea, "PointsLabel");
                pointsFormulaText = FindText(oddsArea, "PointsFormula");
            }
        }

        // TrackInfoPanel
        Transform trackPanel = root.Find("TrackInfoPanel");
        if (trackPanel != null)
        {
            trackRoundLabel = FindText(trackPanel, "TotalRoundLabel");
            trackNameLabel  = FindText(trackPanel, "TrackNameLabel");
            distanceLabel   = FindText(trackPanel, "DistanceLabel");
            trackTypeLabel  = FindText(trackPanel, "TrackTypeLabel");
        }

        // HideInfoToggle
        Transform toggleObj = root.Find("HideInfoToggle");
        if (toggleObj != null)
            hideInfoToggle = toggleObj.GetComponent<Toggle>();

        // CharacterInfoPopup
        Transform popupObj = root.Find("CharacterInfoPopup");
        if (popupObj != null)
            charInfoPopup = popupObj.GetComponent<CharacterInfoPopup>();

        // StartBtn
        Transform startObj = root.Find("StartBtn");
        if (startObj != null)
        {
            startButton = startObj.GetComponent<Button>();
            if (startButton != null)
            {
                // targetGraphic 보정 (클릭 수신 보장)
                Image startBg = startObj.GetComponent<Image>();
                if (startBg != null)
                {
                    startBg.raycastTarget = true;
                    startButton.targetGraphic = startBg;
                }

                startButton.interactable = false;
                startButton.onClick.AddListener(OnStartClicked);
            }

            Text startText = FindText(startObj, "Text");
            if (startText != null)
                startText.text = Loc.Get("str.ui.btn.start");
        }
    }

    // ══════════════════════════════════════
    //  배팅 타입 탭 초기화
    // ══════════════════════════════════════
    private void InitBetTypeTabs()
    {
        Transform topArea = bettingPanelRoot.Find("TopArea");
        if (topArea == null) return;

        Transform tabArea = topArea.Find("BetTypeTabs");
        if (tabArea == null) return;

        // HorizontalLayoutGroup childControl 보정 (탭 크기 0 방지)
        HorizontalLayoutGroup hlg = tabArea.GetComponent<HorizontalLayoutGroup>();
        if (hlg != null)
        {
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = true;
            hlg.childForceExpandHeight = true;
        }

        BetType[] types = { BetType.Win, BetType.Place, BetType.Quinella,
                            BetType.Exacta, BetType.Trio, BetType.Wide };

        int tabCount = Mathf.Min(types.Length, tabArea.childCount);
        betTypeBtns = new Button[tabCount];
        betTypeBtnTexts = new Text[tabCount];
        betTypeBtnBGs = new Image[tabCount];

        for (int i = 0; i < tabCount; i++)
        {
            Transform tabObj = tabArea.GetChild(i);
            betTypeBtnBGs[i] = tabObj.GetComponent<Image>();
            betTypeBtns[i] = tabObj.GetComponent<Button>();

            // targetGraphic 보정 (클릭 수신 보장)
            if (betTypeBtns[i] != null && betTypeBtnBGs[i] != null)
            {
                betTypeBtnBGs[i].raycastTarget = true;
                betTypeBtns[i].targetGraphic = betTypeBtnBGs[i];
            }

            Text tabText = FindText(tabObj, "Text");
            if (tabText != null)
            {
                tabText.text = BettingCalculator.GetTypeName(types[i]);
                betTypeBtnTexts[i] = tabText;
            }

            int idx = i;
            BetType bt = types[i];
            if (betTypeBtns[i] != null)
                betTypeBtns[i].onClick.AddListener(() => OnBetTypeClicked(bt, idx));
        }

        UpdateTabVisuals(0); // 기본: 단승(Win)
    }

    // ══════════════════════════════════════
    //  캐릭터 리스트 초기화
    // ══════════════════════════════════════
    private void InitCharacterList()
    {
        var gs = GameSettings.Instance;
        if (gs.characterItemPrefab == null)
        {
            Debug.LogWarning("[SceneBootstrapper] characterItemPrefab이 없습니다!");
            return;
        }

        Transform charListPanel = bettingPanelRoot.Find("CharacterListPanel");
        if (charListPanel == null) return;
        Transform scrollArea = charListPanel.Find("ScrollArea");
        if (scrollArea == null) return;
        Transform content = scrollArea.Find("Content");
        if (content == null) return;

        // VerticalLayoutGroup 보정 (childControl 누락 시 아이템 크기 0 → 클릭 불가 방지)
        VerticalLayoutGroup vlg = content.GetComponent<VerticalLayoutGroup>();
        if (vlg != null)
        {
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
        }

        // ScrollRect에 raycast용 Image 보정
        Image scrollBg = scrollArea.GetComponent<Image>();
        if (scrollBg == null)
        {
            scrollBg = scrollArea.gameObject.AddComponent<Image>();
            scrollBg.color = new Color(0, 0, 0, 0.01f); // 거의 투명, raycast용
        }

        // 기존 자식 제거
        for (int i = content.childCount - 1; i >= 0; i--)
            Destroy(content.GetChild(i).gameObject);

        int maxRacers = 12;
        characterItems = new CharacterItemUI[maxRacers];

        for (int i = 0; i < maxRacers; i++)
        {
            GameObject itemObj = Instantiate(gs.characterItemPrefab, content);
            itemObj.name = "CharItem_" + i;

            CharacterItemUI itemUI = itemObj.GetComponent<CharacterItemUI>();
            if (itemUI == null)
                itemUI = itemObj.AddComponent<CharacterItemUI>();

            itemUI.Init();
            itemUI.RacerIndex = i;
            characterItems[i] = itemUI;

            // Button 설정 (클릭 + 시각 피드백)
            Button btn = itemObj.GetComponent<Button>();
            if (btn == null) btn = itemObj.AddComponent<Button>();

            Image bg = itemObj.GetComponent<Image>();
            if (bg != null)
            {
                bg.raycastTarget = true;
                btn.targetGraphic = bg;
            }

            int idx = i;
            btn.onClick.AddListener(() => OnRacerClicked(idx));
        }
    }

    // ══════════════════════════════════════
    //  배당률/포인트 표시 초기화
    // ══════════════════════════════════════
    private void InitOddsDisplay()
    {
        if (oddsText != null)
            oddsText.text = Loc.Get("str.ui.betting.odds_empty");
        if (pointsLabelText != null)
            pointsLabelText.text = Loc.Get("str.ui.betting.points_empty");
        if (pointsFormulaText != null)
            pointsFormulaText.text = "";
    }

    // ══════════════════════════════════════
    //  트랙 정보 초기화/갱신
    // ══════════════════════════════════════
    private void InitTrackInfo()
    {
        RefreshTrackInfo();
    }

    private void RefreshTrackInfo()
    {
        var gs = GameSettings.Instance;
        var gm = GameManager.Instance;
        if (gm == null) return;

        var trackDB = TrackDatabase.Instance;
        TrackInfo trackInfo = trackDB != null ? trackDB.CurrentTrackInfo : null;

        if (trackRoundLabel != null)
            trackRoundLabel.text = Loc.Get("str.ui.track.total_round", gs.TotalRounds);

        if (trackNameLabel != null)
            trackNameLabel.text = trackInfo != null ? trackInfo.DisplayName : "???";

        int laps = gs.GetLapsForRound(gm.CurrentRound);
        string distKey = gs.GetDistanceKey(laps);
        if (distanceLabel != null)
            distanceLabel.text = Loc.Get("str.ui.track.distance", Loc.Get(distKey), laps);

        if (trackTypeLabel != null)
        {
            if (trackInfo != null && trackInfo.trackType == TrackType.E_Dirt)
                trackTypeLabel.text = Loc.Get("str.ui.track.type_dirt");
            else
                trackTypeLabel.text = Loc.Get("str.ui.track.type_base");
        }
    }

    // ══════════════════════════════════════
    //  캐릭터 정보 안보기 토글
    // ══════════════════════════════════════
    private void InitHideInfoToggle()
    {
        if (hideInfoToggle == null) return;

        Text toggleLabel = FindText(hideInfoToggle.transform, "Label");
        if (toggleLabel != null)
            toggleLabel.text = Loc.Get("str.ui.betting.hide_info");

        bool saved = PlayerPrefs.GetInt("DR_HideCharInfo", 0) == 1;
        hideInfoToggle.isOn = saved;

        hideInfoToggle.onValueChanged.AddListener(v =>
        {
            PlayerPrefs.SetInt("DR_HideCharInfo", v ? 1 : 0);
            // ON = 캐릭터 클릭 시 정보 팝업 안뜸
            // 현재 팝업이 열려있으면 닫기
            if (v && charInfoPopup != null)
                charInfoPopup.Hide();
        });
    }

    // ══════════════════════════════════════
    //  캐릭터 정보 팝업 초기화
    // ══════════════════════════════════════
    private void InitCharacterInfoPopup()
    {
        if (charInfoPopup != null)
            charInfoPopup.Init();
    }

    // ══════════════════════════════════════
    //  배팅 탭 로직
    // ══════════════════════════════════════
    private void OnBetTypeClicked(BetType type, int tabIndex)
    {
        if (GameManager.Instance == null) return;
        currentTabType = type;
        currentTabIndex = tabIndex;
        GameManager.Instance.SelectBetType(type);
        UpdateTabVisuals(tabIndex);
        UpdateButtonVisuals();
        UpdateBettingArrows();
    }

    private void UpdateTabVisuals(int activeIndex)
    {
        if (betTypeBtnBGs == null) return;

        Color activeColor = new Color(0.3f, 0.4f, 0.7f);
        Color inactiveColor = new Color(0.2f, 0.2f, 0.3f, 0.9f);

        for (int i = 0; i < betTypeBtnBGs.Length; i++)
        {
            if (betTypeBtnBGs[i] != null)
                betTypeBtnBGs[i].color = (i == activeIndex) ? activeColor : inactiveColor;
            if (betTypeBtnTexts != null && i < betTypeBtnTexts.Length && betTypeBtnTexts[i] != null)
            {
                betTypeBtnTexts[i].fontStyle = (i == activeIndex) ? FontStyle.Bold : FontStyle.Normal;
                betTypeBtnTexts[i].color = (i == activeIndex) ? Color.white : new Color(0.6f, 0.6f, 0.6f);
            }
        }

        // 설명 텍스트 갱신
        if (betDescText != null)
        {
            BetType[] types = { BetType.Win, BetType.Place, BetType.Quinella,
                                BetType.Exacta, BetType.Trio, BetType.Wide };
            if (activeIndex < types.Length)
                betDescText.text = BettingCalculator.GetTypeDesc(types[activeIndex]);
        }
    }

    // ══════════════════════════════════════
    //  캐릭터 선택 로직
    // ══════════════════════════════════════
    private void OnRacerClicked(int uiIndex)
    {
        if (GameManager.Instance == null) return;
        var bet = GameManager.Instance.CurrentBet;
        if (bet == null) return;

        // UI 위치 → 실제 레이서 인덱스 변환
        int racerIdx = uiIndex;
        if (characterItems != null && uiIndex < characterItems.Length && characterItems[uiIndex] != null)
            racerIdx = characterItems[uiIndex].RacerIndex;

        // 배팅 선택 로직
        if (bet.selections.Contains(racerIdx))
        {
            GameManager.Instance.RemoveSelection(racerIdx);
        }
        else
        {
            if (bet.IsComplete)
            {
                GameManager.Instance.SelectBetType(bet.type);
                GameManager.Instance.AddSelection(racerIdx);
            }
            else
            {
                GameManager.Instance.AddSelection(racerIdx);
            }
        }

        UpdateButtonVisuals();
        UpdateBettingArrows();

        // 캐릭터 정보 팝업 (hideInfoToggle OFF일 때만 표시)
        bool hidePopup = hideInfoToggle != null && hideInfoToggle.isOn;
        if (!hidePopup && charInfoPopup != null)
        {
            var db = CharacterDatabase.Instance;
            if (db != null && racerIdx < db.SelectedCharacters.Count)
            {
                var charData = db.SelectedCharacters[racerIdx];
                var oddsInfo = OddsCalculator.GetInfo(charData.charName);
                charInfoPopup.Show(charData, oddsInfo);
            }
        }
    }

    private void UpdateButtonVisuals()
    {
        var gm = GameManager.Instance;
        if (gm == null) return;
        var bet = gm.CurrentBet;
        if (bet == null) return;

        if (characterItems != null)
        {
            for (int i = 0; i < characterItems.Length; i++)
            {
                if (characterItems[i] == null) continue;
                // RacerIndex 기반으로 선택 상태 조회 (인기도 정렬 대응)
                int racerIdx = characterItems[i].RacerIndex;
                int selIdx = bet.selections.IndexOf(racerIdx);
                characterItems[i].SetBetOrder(selIdx >= 0 ? selIdx + 1 : 0);
            }
        }

        if (startButton != null)
            startButton.interactable = bet.IsComplete;

        UpdateOddsDisplay();
    }

    // ══════════════════════════════════════
    //  배당률/포인트 실시간 갱신
    // ══════════════════════════════════════
    private void UpdateOddsDisplay()
    {
        var bet = GameManager.Instance?.CurrentBet;
        var racers = CharacterDatabase.Instance?.SelectedCharacters;

        if (bet == null || bet.selections.Count == 0)
        {
            if (oddsText != null)
                oddsText.text = Loc.Get("str.ui.betting.odds_empty");
            if (pointsLabelText != null)
                pointsLabelText.text = Loc.Get("str.ui.betting.points_empty");
            if (pointsFormulaText != null)
                pointsFormulaText.text = "";
            return;
        }

        float odds = OddsCalculator.GetExpectedOdds(bet, racers);

        if (oddsText != null)
            oddsText.text = Loc.Get("str.ui.betting.odds", odds.ToString("F1"));

        int basePt = BettingCalculator.GetPayout(bet.type);
        int result = Mathf.RoundToInt(basePt * odds);

        if (pointsLabelText != null)
            pointsLabelText.text = Loc.Get("str.ui.betting.points_label",
                BettingCalculator.GetTypeName(bet.type));

        if (pointsFormulaText != null)
            pointsFormulaText.text = Loc.Get("str.ui.betting.points_formula",
                basePt, odds.ToString("F1"), result);
    }

    // ══════════════════════════════════════
    //  Start 버튼
    // ══════════════════════════════════════
    private void OnStartClicked()
    {
        GameManager.Instance?.StartRace();
    }

    // ══════════════════════════════════════
    //  배팅 리셋 (매 라운드)
    // ══════════════════════════════════════
    private void ResetBetting()
    {
        // 배팅 타입 초기화 (CurrentBet 보장)
        if (GameManager.Instance != null)
            GameManager.Instance.SelectBetType(currentTabType);

        UpdateTabVisuals(currentTabIndex);
        RefreshCharacterItems();
        HideAllRaceLabels();
        DestroyArrows();
        UpdateButtonVisuals();
        RefreshTrackInfo();

        // 팝업 닫기
        if (charInfoPopup != null)
            charInfoPopup.Hide();

        var gm = GameManager.Instance;
        if (betRoundText != null && gm != null)
            betRoundText.text = Loc.Get("str.ui.betting.round", gm.CurrentRound);
        if (betTitleText != null)
            betTitleText.text = Loc.Get("str.ui.betting.title");
    }

    // ══════════════════════════════════════
    //  캐릭터 아이템 갱신 (CharacterItemUI 위임)
    // ══════════════════════════════════════
    private void RefreshCharacterItems()
    {
        if (characterItems == null) return;

        int activeCount = GameConstants.RACER_COUNT;
        var db = CharacterDatabase.Instance;
        var sm = ScoreManager.Instance;
        if (db == null) return;

        // 인기도 순위로 정렬된 인덱스 배열 생성 (1등 → 맨 위)
        int count = Mathf.Min(activeCount, db.SelectedCharacters.Count);
        int[] sortedIndices = new int[count];
        for (int i = 0; i < count; i++) sortedIndices[i] = i;

        // OddsCalculator에서 인기순위 기준 정렬
        System.Array.Sort(sortedIndices, (a, b) =>
        {
            var infoA = OddsCalculator.GetInfo(db.SelectedCharacters[a].charName);
            var infoB = OddsCalculator.GetInfo(db.SelectedCharacters[b].charName);
            int rankA = infoA != null ? infoA.popularityRank : 999;
            int rankB = infoB != null ? infoB.popularityRank : 999;
            return rankA.CompareTo(rankB);
        });

        // 정렬 순서대로 UI 아이템에 데이터 할당
        for (int ui = 0; ui < characterItems.Length; ui++)
        {
            if (characterItems[ui] == null) continue;

            if (ui < count)
            {
                int racerIdx = sortedIndices[ui];
                characterItems[ui].gameObject.SetActive(true);

                var charData = db.SelectedCharacters[racerIdx];
                var oddsInfo = OddsCalculator.GetInfo(charData.charName);
                var record = sm != null ? sm.GetCharacterRecord(charData.charName) : null;
                int popRank = oddsInfo != null ? oddsInfo.popularityRank : (racerIdx + 1);

                characterItems[ui].SetData(charData, popRank, oddsInfo, record);
                characterItems[ui].RacerIndex = racerIdx; // 실제 레이서 인덱스 보존
            }
            else
            {
                characterItems[ui].gameObject.SetActive(false);
            }
        }
    }

    // ══════════════════════════════════════
    //  Fallback (프리팹 없을 때 최소 UI)
    // ══════════════════════════════════════
    private void BuildBettingUIFallback(Transform parent)
    {
        GameObject startObj = new GameObject("StartBtn");
        startObj.transform.SetParent(parent, false);
        RectTransform srt = startObj.AddComponent<RectTransform>();
        srt.anchorMin = new Vector2(0.5f, 0.5f);
        srt.anchorMax = new Vector2(0.5f, 0.5f);
        srt.sizeDelta = new Vector2(300, 80);

        startObj.AddComponent<Image>().color = new Color(0.95f, 0.85f, 0.15f);
        startButton = startObj.AddComponent<Button>();
        startButton.onClick.AddListener(OnStartClicked);
        startButton.interactable = true;

        MkText(startObj.transform, "프리팹 없음 - Start",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(300, 80), 24, TextAnchor.MiddleCenter, Color.black);

        MkText(parent, "DopamineRace > Create Betting UI Prefabs 실행 필요",
            new Vector2(0.5f, 0.35f), new Vector2(0.5f, 0.35f),
            Vector2.zero, new Vector2(600, 40), 16, TextAnchor.MiddleCenter, Color.red);
    }

    // ══════════════════════════════════════
    //  유틸리티
    // ══════════════════════════════════════
    private Text FindText(Transform parent, string childName)
    {
        Transform child = parent.Find(childName);
        return child != null ? child.GetComponent<Text>() : null;
    }
}
