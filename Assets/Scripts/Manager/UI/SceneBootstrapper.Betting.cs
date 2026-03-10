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

        // 우상단 HUD (roundText / lapText / scoreText) 제거
        // 레이싱 화면은 프리팹으로 재작업 예정 — 해당 시점에 다시 구현
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

            // Best Fit 설정은 Inspector에서 제어 (코드 하드코딩 제거)

            Transform oddsArea = topArea.Find("OddsArea");
            if (oddsArea != null)
            {
                oddsText          = FindText(oddsArea, "OddsText");
                pointsLabelText   = FindText(oddsArea, "PointsLabel");
                pointsFormulaText = FindText(oddsArea, "PointsFormula");
                myPointLabel      = FindText(oddsArea, "MyPointLabel");
            }
            // OddsArea 밖으로 이동한 경우 BetDescText 하위에서 재탐색
            if (oddsText == null)
            {
                Transform betDescText = topArea.Find("BetDescText");
                if (betDescText != null)
                    oddsText = FindText(betDescText, "OddsText");
            }
        }

        // TrackInfoPanel
        Transform trackPanel = root.Find("TrackInfoPanel");
        if (trackPanel != null)
        {
            trackPanelBg    = trackPanel.GetComponent<Image>(); // 배경 Image (닫힐 때 숨김)
            trackRoundLabel = FindText(trackPanel, "TotalRoundLabel");
            trackNameLabel  = FindText(trackPanel, "TrackNameLabel");
            distanceLabel   = FindText(trackPanel, "DistanceLabel");
            trackTypeLabel  = FindText(trackPanel, "TrackTypeLabel");
            trackDescLabel  = FindText(trackPanel, "TrackDescLabel");

            Transform toggleBtnObj = trackPanel.Find("TrackInfoToggleBtn");
            if (toggleBtnObj != null)
            {
                trackInfoToggleBtn = toggleBtnObj.GetComponent<Button>();
                trackToggleBtnText = FindText(toggleBtnObj, "Text");
            }
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

        // 탭 스프라이트 세트 로드 (비선택/선택 각 4상태)
        tabNormalSprites = new Sprite[]
        {
            Resources.Load<Sprite>("UI/Btn_Menu_Normal_01"),
            Resources.Load<Sprite>("UI/Btn_Menu_Normal_02"),
            Resources.Load<Sprite>("UI/Btn_Menu_Normal_03"),
            Resources.Load<Sprite>("UI/Btn_Menu_Normal_04"),
        };
        tabSelectSprites = new Sprite[]
        {
            Resources.Load<Sprite>("UI/Btn_Menu_Select_01"),
            Resources.Load<Sprite>("UI/Btn_Menu_Select_02"),
            Resources.Load<Sprite>("UI/Btn_Menu_Select_03"),
            Resources.Load<Sprite>("UI/Btn_Menu_Select_04"),
        };

        int tabCount = Mathf.Min(types.Length, tabArea.childCount);
        betTypeBtns      = new Button[tabCount];
        betTypeBtnTexts  = new Text[tabCount];
        betTypeBtnBGs    = new Image[tabCount];
        tabTextBaseColors = new Color[tabCount];
        tabTextBaseStyles = new FontStyle[tabCount];

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
                betTypeBtnTexts[i]  = tabText;
                tabTextBaseColors[i] = tabText.color;      // Inspector 값 캐싱
                tabTextBaseStyles[i] = tabText.fontStyle;  // Inspector 값 캐싱
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
        RefreshMyPoint();
    }

    /// <summary>Phase 3: 보유 포인트 표시 갱신</summary>
    private void RefreshMyPoint()
    {
        if (myPointLabel == null) return;
        var sm = ScoreManager.Instance;
        int total = sm != null ? sm.CurrentGameScore : 0;
        myPointLabel.text = Loc.Get("str.hud.total_score", total);
    }

    // ══════════════════════════════════════
    //  트랙 정보 초기화/갱신
    // ══════════════════════════════════════
    private void InitTrackInfo()
    {
        // 토글 버튼 초기화
        if (trackInfoToggleBtn != null)
        {
            trackInfoToggleBtnImage = trackInfoToggleBtn.GetComponent<Image>();
            trackToggleNormalSprite = Resources.Load<Sprite>("UI/Btn_ToggleB_01"); // 기본(닫힘) — ToggleB 세트
            trackToggleOpenSprite   = Resources.Load<Sprite>("UI/Btn_ToggleA_01"); // 열림(눌렀을 때) — ToggleA 세트

            trackInfoToggleBtn.transition = UnityEngine.UI.Selectable.Transition.SpriteSwap;

            trackInfoToggleBtn.onClick.AddListener(() =>
            {
                trackPanelOpen = !trackPanelOpen;
                ApplyTrackPanelState();
            });
        }

        RefreshTrackInfo();
        ApplyTrackPanelState();
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

        // 경기장 상태 : {트랙타입} 형식으로 표시
        if (trackTypeLabel != null)
        {
            string typeStr = trackInfo != null
                ? Loc.Get(TrackTypeUtil.GetTrackTypeKey(trackInfo.trackType))
                : Loc.Get("str.ui.track.type_base");
            trackTypeLabel.text = Loc.Get("str.ui.track.type_label", typeStr);
        }

        // Phase 2: 트랙 설명 표시
        if (trackDescLabel != null)
            trackDescLabel.text = trackInfo != null ? trackInfo.DisplayDesc : "";
    }

    /// <summary>
    /// 트랙 정보 패널 접기/펼치기 상태 적용
    /// static trackPanelOpen → 라운드 간 유지
    /// </summary>
    private void ApplyTrackPanelState()
    {
        // 패널 배경 Image 표시/숨김 (토글 버튼은 자식이라 패널을 비활성화할 수 없으므로 Image만 끔)
        if (trackPanelBg != null) trackPanelBg.enabled = trackPanelOpen;

        // 디테일 요소 표시/숨김
        if (trackRoundLabel != null) trackRoundLabel.gameObject.SetActive(trackPanelOpen);
        if (trackNameLabel != null)  trackNameLabel.gameObject.SetActive(trackPanelOpen);
        if (distanceLabel != null)   distanceLabel.gameObject.SetActive(trackPanelOpen);
        if (trackTypeLabel != null)  trackTypeLabel.gameObject.SetActive(trackPanelOpen);
        if (trackDescLabel != null)  trackDescLabel.gameObject.SetActive(trackPanelOpen);

        // 토글 버튼 스프라이트 갱신 (기본=ToggleB 세트, 열림=ToggleA 세트)
        if (trackInfoToggleBtnImage != null)
        {
            trackInfoToggleBtnImage.sprite = trackPanelOpen ? trackToggleOpenSprite : trackToggleNormalSprite;
            if (trackInfoToggleBtn != null)
            {
                string prefix = trackPanelOpen ? "UI/Btn_ToggleA" : "UI/Btn_ToggleB";
                trackInfoToggleBtn.spriteState = new SpriteState
                {
                    highlightedSprite = Resources.Load<Sprite>($"{prefix}_02"),
                    pressedSprite     = Resources.Load<Sprite>($"{prefix}_03"),
                    disabledSprite    = Resources.Load<Sprite>($"{prefix}_04"),
                };
            }
        }

        // 토글 버튼 텍스트 갱신
        if (trackToggleBtnText != null)
            trackToggleBtnText.text = trackPanelOpen
                ? Loc.Get("str.ui.btn.panel_close")
                : Loc.Get("str.ui.btn.panel_open");
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
        if (charInfoPopup != null) charInfoPopup.Hide();
    }

    private void UpdateTabVisuals(int activeIndex)
    {
        if (betTypeBtnBGs == null) return;

        for (int i = 0; i < betTypeBtnBGs.Length; i++)
        {
            bool isActive = (i == activeIndex);
            Sprite[] sprites = isActive ? tabSelectSprites : tabNormalSprites;

            if (betTypeBtnBGs[i] != null && sprites != null && sprites[0] != null)
            {
                betTypeBtnBGs[i].sprite = sprites[0];
                betTypeBtnBGs[i].color  = Color.white;

                // SpriteState 동적 교체 (선택/비선택 상태별 4상태)
                if (betTypeBtns[i] != null)
                    betTypeBtns[i].spriteState = new SpriteState
                    {
                        highlightedSprite = sprites[1],
                        pressedSprite     = sprites[2],
                        selectedSprite    = sprites[0],
                        disabledSprite    = sprites[3],
                    };
            }

            if (betTypeBtnTexts != null && i < betTypeBtnTexts.Length && betTypeBtnTexts[i] != null)
            {
                // Inspector 설정값 그대로 — 선택/비선택 모두 동일 색
                betTypeBtnTexts[i].color = (tabTextBaseColors != null && i < tabTextBaseColors.Length)
                    ? tabTextBaseColors[i] : Color.white;
                betTypeBtnTexts[i].fontStyle = isActive ? FontStyle.Bold
                    : (tabTextBaseStyles != null && i < tabTextBaseStyles.Length
                        ? tabTextBaseStyles[i] : FontStyle.Normal);
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

                // 동일 캐릭터 재클릭 → 토글 닫기
                if (charInfoPopup.IsShowing && charInfoPopup.CurrentCharId == charData.charId)
                {
                    charInfoPopup.Hide();
                }
                else
                {
                    var oddsInfo = OddsCalculator.GetInfo(charData.charId);
                    var record = ScoreManager.Instance != null
                        ? ScoreManager.Instance.GetCharacterRecord(charData.charId)
                        : null;
                    var trackInfo = TrackDatabase.Instance?.CurrentTrackInfo;
                    charInfoPopup.Show(charData, oddsInfo, record, trackInfo);
                }
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

        // 배팅 타입이 선택된 순간부터 고정 배율 표시
        if (oddsText != null)
        {
            if (bet != null)
                oddsText.text = Loc.Get("str.ui.betting.odds", BettingCalculator.GetPayout(bet.type));
            else
                oddsText.text = Loc.Get("str.ui.betting.odds_empty");
        }

        if (bet == null || bet.selections.Count == 0)
        {
            if (pointsLabelText != null)
                pointsLabelText.text = Loc.Get("str.ui.betting.points_empty");
            if (pointsFormulaText != null)
                pointsFormulaText.text = "";
            return;
        }

        float odds = OddsCalculator.GetExpectedOdds(bet, racers);

        int basePt = BettingCalculator.GetPayout(bet.type);
        int result = Mathf.RoundToInt(basePt * odds);

        if (pointsLabelText != null)
            pointsLabelText.text = Loc.Get("str.ui.betting.points_label",
                BettingCalculator.GetTypeName(bet.type));

        if (pointsFormulaText != null)
            pointsFormulaText.text = string.Format(
                "<color=#FFE000>{0}</color>x<color=#FF8C00>{1}</color> = {2}",
                basePt, odds.ToString("F1"), result);

        RefreshMyPoint();
    }

    // ══════════════════════════════════════
    //  Start 버튼
    // ══════════════════════════════════════
    private void OnStartClicked()
    {
        // 팝업 열려있으면 닫기
        if (charInfoPopup != null)
            charInfoPopup.Hide();

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
        ApplyTrackPanelState();

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
            var infoA = OddsCalculator.GetInfo(db.SelectedCharacters[a].charId);
            var infoB = OddsCalculator.GetInfo(db.SelectedCharacters[b].charId);
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
                var oddsInfo = OddsCalculator.GetInfo(charData.charId);
                var record = sm != null ? sm.GetCharacterRecord(charData.charId) : null;
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
