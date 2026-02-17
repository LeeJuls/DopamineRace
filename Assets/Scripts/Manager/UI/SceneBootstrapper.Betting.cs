using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 배팅 UI: 패널 빌드, 타입 탭, 캐릭터 선택, 버튼 비주얼
/// </summary>
public partial class SceneBootstrapper
{
    // ══════════════════════════════════════
    //  배팅 화면 빌드
    // ══════════════════════════════════════
    private void BuildBettingUI(Transform parent)
    {
        // === 왼쪽 패널 ===
        GameObject leftPanel = new GameObject("LeftPanel");
        leftPanel.transform.SetParent(parent, false);
        RectTransform lprt = leftPanel.AddComponent<RectTransform>();
        lprt.anchorMin = new Vector2(0, 0);
        lprt.anchorMax = new Vector2(0.25f, 1);
        lprt.offsetMin = Vector2.zero;
        lprt.offsetMax = Vector2.zero;
        Image lpbg = leftPanel.AddComponent<Image>();
        lpbg.color = new Color(0.95f, 0.85f, 0.15f);
        leftPanelObj = leftPanel;

        // 닫기/열기 토글 버튼
        GameObject toggleBtn = new GameObject("ToggleBtn");
        toggleBtn.transform.SetParent(parent, false);
        RectTransform trt = toggleBtn.AddComponent<RectTransform>();
        trt.anchorMin = new Vector2(0, 1);
        trt.anchorMax = new Vector2(0, 1);
        trt.pivot = new Vector2(0, 1);
        trt.anchoredPosition = new Vector2(5, -5);
        trt.sizeDelta = new Vector2(80, 35);
        toggleBtn.AddComponent<Image>().color = new Color(0.3f, 0.3f, 0.3f, 0.9f);
        Button tb = toggleBtn.AddComponent<Button>();
        tb.onClick.AddListener(() => {
            bool isOpen = leftPanelObj.activeSelf;
            leftPanelObj.SetActive(!isOpen);
            toggleBtnText.text = isOpen ? "열기 ▶" : "◀ 닫기";
        });
        toggleBtnText = MkText(toggleBtn.transform, "◀ 닫기",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(80, 35), 18, TextAnchor.MiddleCenter, Color.white);

        // ── 배팅 타입 탭 (상단) ──
        BuildBetTypeTabs(leftPanel.transform);

        // 타이틀 + 안내
        titleText = MkText(leftPanel.transform, "배팅 선택",
            new Vector2(0.5f, 1), new Vector2(0.5f, 1),
            new Vector2(0, -75), new Vector2(300, 30), 22, TextAnchor.MiddleCenter, Color.black);

        infoText = MkText(leftPanel.transform, "",
            new Vector2(0.5f, 1), new Vector2(0.5f, 1),
            new Vector2(0, -100), new Vector2(300, 25), 15, TextAnchor.MiddleCenter, new Color(0.3f, 0.3f, 0.3f));

        // 버튼 목록 (스크롤 가능)
        GameObject scrollObj = new GameObject("ScrollArea");
        scrollObj.transform.SetParent(leftPanel.transform, false);
        RectTransform scrollRT = scrollObj.AddComponent<RectTransform>();
        scrollRT.anchorMin = new Vector2(0.03f, 0.02f);
        scrollRT.anchorMax = new Vector2(0.97f, 0.87f);
        scrollRT.offsetMin = Vector2.zero;
        scrollRT.offsetMax = Vector2.zero;

        ScrollRect scrollRect = scrollObj.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        scrollRect.scrollSensitivity = 30f;

        scrollObj.AddComponent<RectMask2D>();

        GameObject content = new GameObject("Content");
        content.transform.SetParent(scrollObj.transform, false);
        RectTransform contentRT = content.AddComponent<RectTransform>();
        contentRT.anchorMin = new Vector2(0, 1);
        contentRT.anchorMax = new Vector2(1, 1);
        contentRT.pivot = new Vector2(0.5f, 1);
        contentRT.anchoredPosition = Vector2.zero;
        contentRT.sizeDelta = new Vector2(0, 1000);

        VerticalLayoutGroup vlg = content.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 4;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;
        vlg.padding = new RectOffset(4, 4, 4, 4);

        ContentSizeFitter csf = content.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scrollRect.content = contentRT;
        racerScrollContent = contentRT;

        int maxRacers = 12;
        racerButtons = new Button[maxRacers];
        racerTexts = new Text[maxRacers];
        racerLabels = new Text[maxRacers];
        racerBGs = new Image[maxRacers];
        racerIcons = new Image[maxRacers];

        float btnHeight = GameSettings.Instance.bettingButtonHeight;

        for (int i = 0; i < maxRacers; i++)
        {
            GameObject btn = new GameObject("Btn_" + i);
            btn.transform.SetParent(content.transform, false);

            LayoutElement le = btn.AddComponent<LayoutElement>();
            le.preferredHeight = btnHeight;
            le.minHeight = btnHeight;

            Image bg = btn.AddComponent<Image>();
            bg.color = Color.white;
            racerBGs[i] = bg;

            Button b = btn.AddComponent<Button>();
            ColorBlock cb = b.colors;
            cb.normalColor = Color.white;
            cb.highlightedColor = new Color(0.9f, 0.9f, 0.7f);
            cb.pressedColor = new Color(0.8f, 0.8f, 0.5f);
            cb.selectedColor = Color.white;
            b.colors = cb;
            racerButtons[i] = b;

            // ★ 썸네일 컨테이너 (마스크)
            float iconW = btnHeight * 0.95f;
            GameObject iconContainer = new GameObject("IconContainer");
            iconContainer.transform.SetParent(btn.transform, false);
            RectTransform icrt = iconContainer.AddComponent<RectTransform>();
            icrt.anchorMin = new Vector2(0, 0);
            icrt.anchorMax = new Vector2(0, 1);
            icrt.pivot = new Vector2(0, 0.5f);
            icrt.anchoredPosition = new Vector2(2, 0);
            icrt.sizeDelta = new Vector2(iconW, 0);
            iconContainer.AddComponent<RectMask2D>();

            // ★ 실제 이미지 (줌/오프셋은 RefreshRacerButtons에서 소스별 적용)
            GameObject iconObj = new GameObject("Icon");
            iconObj.transform.SetParent(iconContainer.transform, false);
            RectTransform irt = iconObj.AddComponent<RectTransform>();
            irt.anchorMin = Vector2.zero;
            irt.anchorMax = Vector2.one;
            irt.offsetMin = Vector2.zero;
            irt.offsetMax = Vector2.zero;
            Image iconImg = iconObj.AddComponent<Image>();
            iconImg.color = Color.white;
            iconImg.preserveAspect = true;
            racerIcons[i] = iconImg;

            // 이름
            float nameLeft = iconW + 8;
            GameObject nameObj = new GameObject("Name");
            nameObj.transform.SetParent(btn.transform, false);
            RectTransform nrt = nameObj.AddComponent<RectTransform>();
            nrt.anchorMin = new Vector2(0, 0);
            nrt.anchorMax = new Vector2(0.78f, 1);
            nrt.offsetMin = new Vector2(nameLeft, 2);
            nrt.offsetMax = new Vector2(0, -2);
            Text nt = nameObj.AddComponent<Text>();
            nt.font = font;
            nt.text = "";
            nt.fontSize = 26;
            nt.color = Color.black;
            nt.alignment = TextAnchor.MiddleLeft;
            nt.resizeTextForBestFit = true;
            nt.resizeTextMinSize = 16;
            nt.resizeTextMaxSize = 26;
            racerTexts[i] = nt;

            // 선택 라벨 (우측)
            GameObject lblObj = new GameObject("Lbl");
            lblObj.transform.SetParent(btn.transform, false);
            RectTransform lrt = lblObj.AddComponent<RectTransform>();
            lrt.anchorMin = new Vector2(0.78f, 0);
            lrt.anchorMax = new Vector2(1, 1);
            lrt.offsetMin = new Vector2(0, 2);
            lrt.offsetMax = new Vector2(-5, -2);
            Text lt = lblObj.AddComponent<Text>();
            lt.font = font;
            lt.text = "";
            lt.fontSize = 18;
            lt.color = new Color(0.8f, 0.1f, 0.1f);
            lt.alignment = TextAnchor.MiddleCenter;
            lt.fontStyle = FontStyle.Bold;
            racerLabels[i] = lt;

            int idx = i;
            b.onClick.AddListener(() => OnRacerClicked(idx));
        }

        // === 우하단 Start 버튼 ===
        GameObject startObj = new GameObject("StartBtn");
        startObj.transform.SetParent(parent, false);
        RectTransform srt = startObj.AddComponent<RectTransform>();
        srt.anchorMin = new Vector2(1, 0);
        srt.anchorMax = new Vector2(1, 0);
        srt.pivot = new Vector2(1, 0);
        srt.anchoredPosition = new Vector2(-30, 20);
        srt.sizeDelta = new Vector2(200, 65);

        startObj.AddComponent<Image>().color = new Color(0.95f, 0.85f, 0.15f);
        startButton = startObj.AddComponent<Button>();
        ColorBlock scb = startButton.colors;
        scb.normalColor = new Color(0.95f, 0.85f, 0.15f);
        scb.highlightedColor = new Color(1f, 0.95f, 0.4f);
        scb.pressedColor = new Color(0.8f, 0.7f, 0.1f);
        scb.disabledColor = new Color(0.5f, 0.5f, 0.5f);
        startButton.colors = scb;
        startButton.interactable = false;
        startButton.onClick.AddListener(OnStartClicked);

        MkText(startObj.transform, "Start",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(200, 65), 30, TextAnchor.MiddleCenter, Color.black);

        // === 우상단 정보 ===
        roundText = MkText(parent, "Round 1/7",
            new Vector2(1, 1), new Vector2(1, 1),
            new Vector2(-20, -10), new Vector2(250, 30), 22, TextAnchor.MiddleRight, Color.white);

        lapText = MkText(parent, "이번 경기: 1바퀴",
            new Vector2(1, 1), new Vector2(1, 1),
            new Vector2(-20, -38), new Vector2(250, 25), 18, TextAnchor.MiddleRight, new Color(0.8f, 0.9f, 1f));

        scoreText = MkText(parent, "총점: 0",
            new Vector2(1, 1), new Vector2(1, 1),
            new Vector2(-20, -62), new Vector2(250, 25), 20, TextAnchor.MiddleRight, Color.yellow);

        // ★ Top 100 버튼
        GameObject top100Obj = new GameObject("Top100Btn");
        top100Obj.transform.SetParent(parent, false);
        RectTransform t100rt = top100Obj.AddComponent<RectTransform>();
        t100rt.anchorMin = new Vector2(1, 0);
        t100rt.anchorMax = new Vector2(1, 0);
        t100rt.pivot = new Vector2(1, 0);
        t100rt.anchoredPosition = new Vector2(-30, 95);
        t100rt.sizeDelta = new Vector2(200, 40);

        top100Obj.AddComponent<Image>().color = new Color(0.3f, 0.3f, 0.5f);
        Button top100Btn = top100Obj.AddComponent<Button>();
        ColorBlock t100cb = top100Btn.colors;
        t100cb.normalColor = new Color(0.3f, 0.3f, 0.5f);
        t100cb.highlightedColor = new Color(0.4f, 0.4f, 0.65f);
        t100cb.pressedColor = new Color(0.2f, 0.2f, 0.4f);
        top100Btn.colors = t100cb;
        top100Btn.onClick.AddListener(() => ShowLeaderboard());

        MkText(top100Obj.transform, "Top 100",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(200, 40), 20, TextAnchor.MiddleCenter, Color.white);
    }

    // ══════════════════════════════════════
    //  배팅 타입 탭 6개
    // ══════════════════════════════════════
    private void BuildBetTypeTabs(Transform parent)
    {
        BetType[] types = { BetType.Win, BetType.Place, BetType.Quinella, BetType.Exacta, BetType.Trio, BetType.Wide };
        var gs = GameSettings.Instance;
        string[] labels = {
            "단승\n" + gs.payoutWin + "pt",
            "연승\n" + gs.payoutPlace + "pt",
            "복승\n" + gs.payoutQuinella + "pt",
            "쌍승\n" + gs.payoutExacta + "pt",
            "삼복승\n" + gs.payoutTrio + "pt",
            "복연승\n" + gs.payoutWide + "pt"
        };

        GameObject tabArea = new GameObject("TabArea");
        tabArea.transform.SetParent(parent, false);
        RectTransform tart = tabArea.AddComponent<RectTransform>();
        tart.anchorMin = new Vector2(0.02f, 0.93f);
        tart.anchorMax = new Vector2(0.98f, 1f);
        tart.offsetMin = Vector2.zero;
        tart.offsetMax = Vector2.zero;

        HorizontalLayoutGroup hlg = tabArea.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 2;
        hlg.childForceExpandWidth = true;
        hlg.childForceExpandHeight = true;
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;
        hlg.padding = new RectOffset(2, 2, 2, 0);

        betTypeBtns = new Button[types.Length];
        betTypeBtnTexts = new Text[types.Length];
        betTypeBtnBGs = new Image[types.Length];

        for (int i = 0; i < types.Length; i++)
        {
            GameObject tab = new GameObject("Tab_" + types[i]);
            tab.transform.SetParent(tabArea.transform, false);

            Image bg = tab.AddComponent<Image>();
            bg.color = new Color(0.85f, 0.75f, 0.1f);
            betTypeBtnBGs[i] = bg;

            Button b = tab.AddComponent<Button>();
            betTypeBtns[i] = b;

            Text t = MkText(tab.transform, labels[i],
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(70, 60), 12, TextAnchor.MiddleCenter, Color.black);
            t.resizeTextForBestFit = true;
            t.resizeTextMinSize = 9;
            t.resizeTextMaxSize = 14;
            betTypeBtnTexts[i] = t;

            int idx = i;
            BetType bt = types[i];
            b.onClick.AddListener(() => OnBetTypeClicked(bt, idx));
        }

        UpdateTabVisuals(3); // 기본: 쌍승
    }

    // ══════════════════════════════════════
    //  배팅 선택 로직
    // ══════════════════════════════════════
    private void OnBetTypeClicked(BetType type, int tabIndex)
    {
        if (GameManager.Instance == null) return;
        currentTabType = type;
        GameManager.Instance.SelectBetType(type);
        UpdateTabVisuals(tabIndex);
        UpdateButtonVisuals();
        UpdateBettingArrows();
    }

    private void UpdateTabVisuals(int activeIndex)
    {
        Color activeColor = new Color(1f, 0.95f, 0.5f);
        Color inactiveColor = new Color(0.7f, 0.63f, 0.1f);

        for (int i = 0; i < betTypeBtnBGs.Length; i++)
        {
            betTypeBtnBGs[i].color = (i == activeIndex) ? activeColor : inactiveColor;
            betTypeBtnTexts[i].fontStyle = (i == activeIndex) ? FontStyle.Bold : FontStyle.Normal;
            betTypeBtnTexts[i].color = (i == activeIndex) ? Color.black : new Color(0.3f, 0.3f, 0.3f);
        }
    }

    private void OnRacerClicked(int index)
    {
        if (GameManager.Instance == null) return;
        var bet = GameManager.Instance.CurrentBet;
        if (bet == null) return;

        if (bet.selections.Contains(index))
        {
            GameManager.Instance.RemoveSelection(index);
        }
        else
        {
            if (bet.IsComplete)
            {
                GameManager.Instance.SelectBetType(bet.type);
                GameManager.Instance.AddSelection(index);
            }
            else
            {
                GameManager.Instance.AddSelection(index);
            }
        }

        UpdateButtonVisuals();
        UpdateBettingArrows();
    }

    private void UpdateButtonVisuals()
    {
        var gm = GameManager.Instance;
        if (gm == null) return;
        var bet = gm.CurrentBet;
        if (bet == null) return;

        Color[] selColors = {
            new Color(1f, 0.7f, 0.7f),
            new Color(0.7f, 0.7f, 1f),
            new Color(0.7f, 1f, 0.7f),
        };

        for (int i = 0; i < GameConstants.RACER_COUNT; i++)
        {
            int selIdx = bet.selections.IndexOf(i);
            if (selIdx >= 0)
            {
                racerBGs[i].color = selColors[Mathf.Min(selIdx, selColors.Length - 1)];
                racerLabels[i].text = bet.GetSelectionLabel(selIdx);
                racerLabels[i].color = selIdx == 0 ? Color.red : selIdx == 1 ? Color.blue : new Color(0, 0.6f, 0);
            }
            else
            {
                racerBGs[i].color = Color.white;
                racerLabels[i].text = "";
            }
        }

        startButton.interactable = bet.IsComplete;

        if (infoText != null)
            infoText.text = bet.GetSelectionGuide();
        if (titleText != null)
            titleText.text = BettingCalculator.GetTypeName(bet.type) + " 배팅";
    }

    private void OnStartClicked()
    {
        GameManager.Instance?.StartRace();
    }

    private void ResetBetting()
    {
        BetType[] types = { BetType.Win, BetType.Place, BetType.Quinella, BetType.Exacta, BetType.Trio, BetType.Wide };
        for (int i = 0; i < types.Length; i++)
        {
            if (types[i] == currentTabType)
            {
                UpdateTabVisuals(i);
                break;
            }
        }
        RefreshRacerButtons();
        HideAllRaceLabels();
        DestroyArrows();
        UpdateButtonVisuals();
    }

    private void RefreshRacerButtons()
    {
        int activeCount = GameConstants.RACER_COUNT;
        var db = CharacterDatabase.Instance;
        var gs = GameSettings.Instance;

        for (int i = 0; i < racerButtons.Length; i++)
        {
            if (i < activeCount)
            {
                racerButtons[i].gameObject.SetActive(true);
                racerTexts[i].text = GameConstants.RACER_NAMES[i];

                if (racerIcons != null && i < racerIcons.Length && racerIcons[i] != null)
                {
                    Sprite icon = null;
                    bool isIconFile = false;

                    if (db != null && i < db.SelectedCharacters.Count)
                    {
                        var charData = db.SelectedCharacters[i];
                        isIconFile = charData.HasIconFile();
                        icon = charData.LoadIcon();
                    }

                    if (icon != null)
                    {
                        racerIcons[i].sprite = icon;
                        racerIcons[i].color = Color.white;

                        // ★ 소스별 줌/오프셋 적용
                        float z, ox, oy;
                        if (isIconFile)
                        {
                            z = gs.iconFileZoom;
                            ox = gs.iconFileOffsetX;
                            oy = gs.iconFileOffsetY;
                        }
                        else
                        {
                            z = gs.iconPrefabZoom;
                            ox = gs.iconPrefabOffsetX;
                            oy = gs.iconPrefabOffsetY;
                        }
                        float half = (z - 1f) / 2f;
                        RectTransform irt = racerIcons[i].rectTransform;
                        irt.anchorMin = new Vector2(-half + ox, -half + oy);
                        irt.anchorMax = new Vector2(1 + half + ox, 1 + half + oy);
                    }
                    else
                    {
                        racerIcons[i].sprite = null;
                        racerIcons[i].color = GameConstants.RACER_COLORS[i];
                    }
                }
            }
            else
            {
                racerButtons[i].gameObject.SetActive(false);
            }
        }

        if (racerScrollContent != null)
            racerScrollContent.anchoredPosition = Vector2.zero;
    }
}