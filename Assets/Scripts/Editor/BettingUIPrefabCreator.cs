#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using System.IO;

/// <summary>
/// 배팅 UI 프리팹 자동 생성 Editor 스크립트.
/// Unity 메뉴: DopamineRace > Create Betting UI Prefabs
///
/// 생성물:
///   Assets/Prefabs/UI/CharacterItem.prefab
///   Assets/Prefabs/UI/BettingPanel.prefab
/// </summary>
public static class BettingUIPrefabCreator
{
    private const string PREFAB_DIR = "Assets/Prefabs/UI";

    [MenuItem("DopamineRace/Create Betting UI Prefabs")]
    public static void CreatePrefabs()
    {
        EnsureDirectory(PREFAB_DIR);

        // GameSettings에서 폰트 참조
        var gs = AssetDatabase.LoadAssetAtPath<GameSettings>(
            "Assets/Resources/GameSettings.asset");
        Font font = gs != null ? gs.mainFont : null;

        // ── A) CharacterItem.prefab ──
        GameObject charItem = CreateCharacterItemPrefab(font);
        string charItemPath = PREFAB_DIR + "/CharacterItem.prefab";
        PrefabUtility.SaveAsPrefabAsset(charItem, charItemPath);
        Object.DestroyImmediate(charItem);
        Debug.Log("[BettingUIPrefabCreator] 생성: " + charItemPath);

        // ── B) BettingPanel.prefab ──
        GameObject bettingPanel = CreateBettingPanelPrefab(font);
        string bettingPanelPath = PREFAB_DIR + "/BettingPanel.prefab";
        PrefabUtility.SaveAsPrefabAsset(bettingPanel, bettingPanelPath);
        Object.DestroyImmediate(bettingPanel);
        Debug.Log("[BettingUIPrefabCreator] 생성: " + bettingPanelPath);

        // ── GameSettings에 프리팹 자동 연결 ──
        if (gs != null)
        {
            gs.characterItemPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(charItemPath);
            gs.bettingPanelPrefab  = AssetDatabase.LoadAssetAtPath<GameObject>(bettingPanelPath);
            EditorUtility.SetDirty(gs);
            AssetDatabase.SaveAssets();
            Debug.Log("[BettingUIPrefabCreator] GameSettings에 프리팹 자동 연결 완료!");
        }
        else
        {
            Debug.LogWarning("[BettingUIPrefabCreator] GameSettings.asset을 찾을 수 없습니다. 수동으로 연결해주세요.");
        }

        AssetDatabase.Refresh();
    }

    // ══════════════════════════════════════════════════════
    //  A) CharacterItem 프리팹
    // ══════════════════════════════════════════════════════

    private static GameObject CreateCharacterItemPrefab(Font font)
    {
        // 루트
        GameObject root = new GameObject("CharacterItem");
        RectTransform rootRt = root.AddComponent<RectTransform>();
        rootRt.sizeDelta = new Vector2(0, 80); // 높이 80, 너비는 부모 Layout에 의해 결정

        // LayoutElement (VerticalLayout용)
        LayoutElement le = root.AddComponent<LayoutElement>();
        le.preferredHeight = 80;
        le.flexibleWidth = 1;

        // Background (raycastTarget = true → 클릭 수신)
        Image bg = root.AddComponent<Image>();
        bg.color = new Color(0.15f, 0.15f, 0.2f, 0.9f);
        bg.raycastTarget = true;

        // Button (프리팹에 미리 추가 → 런타임 AddComponent 불필요)
        Button btn = root.AddComponent<Button>();
        btn.targetGraphic = bg;

        // IconContainer (RectMask2D로 아이콘 크롭)
        GameObject iconContainer = MkChild(root, "IconContainer", 0f, 0.5f, 0f, 0.5f,
            new Vector2(5, 0), new Vector2(70, 70));
        iconContainer.AddComponent<RectMask2D>();
        Image icBg = iconContainer.AddComponent<Image>();
        icBg.color = new Color(0.1f, 0.1f, 0.15f, 0.8f);

        // Icon (inside IconContainer)
        GameObject iconObj = MkChild(iconContainer, "Icon", 0.5f, 0.5f, 0.5f, 0.5f,
            Vector2.zero, new Vector2(64, 64));
        Image iconImg = iconObj.AddComponent<Image>();
        iconImg.color = new Color(0.3f, 0.3f, 0.3f);
        iconImg.preserveAspect = true;

        // ConditionIcon (24x24, 아이콘 우측 하단)
        GameObject condObj = MkChild(root, "ConditionIcon", 0f, 0.5f, 0f, 0.5f,
            new Vector2(60, -20), new Vector2(24, 24));
        Image condImg = condObj.AddComponent<Image>();
        condImg.color = Color.white;

        // PopularityLabel (인기순위)
        MkTextObj(root, "PopularityLabel", font,
            0f, 0.5f, 0f, 0.5f, new Vector2(90, 10), new Vector2(80, 24),
            14, TextAnchor.MiddleLeft, new Color(0.9f, 0.75f, 0.2f));

        // NameLabel (캐릭터 이름)
        MkTextObj(root, "NameLabel", font,
            0f, 0.5f, 0f, 0.5f, new Vector2(170, 10), new Vector2(150, 28),
            20, TextAnchor.MiddleLeft, Color.white);

        // RecordLabel (전적)
        MkTextObj(root, "RecordLabel", font,
            0f, 0.5f, 0f, 0.5f, new Vector2(330, 10), new Vector2(120, 22),
            14, TextAnchor.MiddleLeft, new Color(0.7f, 0.7f, 0.7f));

        // SecondLabel (2착 횟수)
        MkTextObj(root, "SecondLabel", font,
            0f, 0.5f, 0f, 0.5f, new Vector2(460, 10), new Vector2(100, 22),
            14, TextAnchor.MiddleLeft, new Color(0.6f, 0.6f, 0.6f));

        // BetOrderLabel (착순 라벨, 기본 비활성)
        GameObject betOrder = MkTextObj(root, "BetOrderLabel", font,
            1f, 0.5f, 1f, 0.5f, new Vector2(-30, 0), new Vector2(60, 30),
            18, TextAnchor.MiddleCenter, new Color(1f, 0.85f, 0.2f));
        betOrder.SetActive(false);

        // CharacterItemUI 컴포넌트 부착
        root.AddComponent<CharacterItemUI>();

        return root;
    }

    // ══════════════════════════════════════════════════════
    //  B) BettingPanel 프리팹
    // ══════════════════════════════════════════════════════

    private static GameObject CreateBettingPanelPrefab(Font font)
    {
        // ── 루트 ──
        GameObject root = new GameObject("BettingPanel");
        RectTransform rootRt = root.AddComponent<RectTransform>();
        rootRt.anchorMin = Vector2.zero;
        rootRt.anchorMax = Vector2.one;
        rootRt.offsetMin = Vector2.zero;
        rootRt.offsetMax = Vector2.zero;

        Image rootBg = root.AddComponent<Image>();
        rootBg.color = new Color(0.05f, 0.05f, 0.1f, 0.95f);

        // ════════════════════════════
        //  TopArea (상단 고정영역)
        // ════════════════════════════
        GameObject topArea = MkChild(root, "TopArea",
            0f, 0.82f, 1f, 1f, Vector2.zero, Vector2.zero);
        topArea.AddComponent<Image>().color = new Color(0.08f, 0.08f, 0.15f, 0.9f);

        // RoundText
        MkTextObj(topArea, "RoundText", font,
            0f, 1f, 0f, 1f, new Vector2(20, -20), new Vector2(200, 30),
            18, TextAnchor.MiddleLeft, new Color(0.7f, 0.7f, 0.7f));

        // TitleText
        MkTextObj(topArea, "TitleText", font,
            0.5f, 1f, 0.5f, 1f, new Vector2(0, -20), new Vector2(300, 35),
            26, TextAnchor.MiddleCenter, new Color(1f, 0.85f, 0.2f));

        // BetTypeTabs (HorizontalLayoutGroup)
        GameObject tabArea = MkChild(topArea, "BetTypeTabs",
            0.05f, 0.35f, 0.65f, 0.65f, Vector2.zero, Vector2.zero);
        HorizontalLayoutGroup tabLayout = tabArea.AddComponent<HorizontalLayoutGroup>();
        tabLayout.spacing = 4;
        tabLayout.childAlignment = TextAnchor.MiddleCenter;
        tabLayout.childControlWidth = true;
        tabLayout.childControlHeight = true;
        tabLayout.childForceExpandWidth = true;
        tabLayout.childForceExpandHeight = true;

        // 6개 탭 버튼
        string[] tabNames = { "Tab_Win", "Tab_Place", "Tab_Quinella", "Tab_Exacta", "Tab_Trio", "Tab_Wide" };
        foreach (string tabName in tabNames)
        {
            GameObject tab = new GameObject(tabName);
            tab.transform.SetParent(tabArea.transform, false);
            RectTransform tabRt = tab.AddComponent<RectTransform>();
            tabRt.sizeDelta = new Vector2(0, 0);
            Image tabBg = tab.AddComponent<Image>();
            tabBg.color = new Color(0.2f, 0.2f, 0.3f, 0.9f);
            tabBg.raycastTarget = true;
            Button tabBtn = tab.AddComponent<Button>();
            tabBtn.targetGraphic = tabBg;

            // 탭 텍스트
            GameObject tabTextObj = new GameObject("Text");
            tabTextObj.transform.SetParent(tab.transform, false);
            RectTransform ttRt = tabTextObj.AddComponent<RectTransform>();
            ttRt.anchorMin = Vector2.zero;
            ttRt.anchorMax = Vector2.one;
            ttRt.offsetMin = Vector2.zero;
            ttRt.offsetMax = Vector2.zero;
            Text tabText = tabTextObj.AddComponent<Text>();
            tabText.text = tabName.Replace("Tab_", "");
            tabText.fontSize = 13;
            tabText.alignment = TextAnchor.MiddleCenter;
            tabText.color = Color.white;
            if (font != null) tabText.font = font;
        }

        // BetDescText (승식 설명)
        MkTextObj(topArea, "BetDescText", font,
            0.05f, 0.05f, 0.65f, 0.30f, Vector2.zero, Vector2.zero,
            14, TextAnchor.MiddleLeft, new Color(0.8f, 0.8f, 0.8f),
            true /* stretch */);

        // OddsArea (우측)
        GameObject oddsArea = MkChild(topArea, "OddsArea",
            0.68f, 0.05f, 0.98f, 0.95f, Vector2.zero, Vector2.zero);

        // OddsText
        MkTextObj(oddsArea, "OddsText", font,
            0.5f, 0.75f, 0.5f, 0.75f, Vector2.zero, new Vector2(200, 30),
            18, TextAnchor.MiddleCenter, new Color(1f, 0.9f, 0.3f));

        // PointsLabel
        MkTextObj(oddsArea, "PointsLabel", font,
            0.5f, 0.45f, 0.5f, 0.45f, Vector2.zero, new Vector2(200, 24),
            14, TextAnchor.MiddleCenter, Color.white);

        // PointsFormula
        MkTextObj(oddsArea, "PointsFormula", font,
            0.5f, 0.20f, 0.5f, 0.20f, Vector2.zero, new Vector2(200, 24),
            14, TextAnchor.MiddleCenter, new Color(0.6f, 0.8f, 1f));

        // ════════════════════════════
        //  HideInfoToggle
        // ════════════════════════════
        GameObject hideToggleObj = MkChild(root, "HideInfoToggle",
            0.85f, 0.78f, 0.85f, 0.78f, Vector2.zero, new Vector2(180, 28));
        Toggle hideToggle = hideToggleObj.AddComponent<Toggle>();
        Image hideBg = hideToggleObj.AddComponent<Image>();
        hideBg.color = new Color(0.2f, 0.2f, 0.25f, 0.8f);
        hideToggle.targetGraphic = hideBg;

        // 체크 배경 (OFF 상태 — 항상 보이는 회색)
        GameObject checkBg = MkChild(hideToggleObj, "CheckBg",
            0f, 0.5f, 0f, 0.5f, new Vector2(12, 0), new Vector2(20, 20));
        checkBg.AddComponent<Image>().color = new Color(0.4f, 0.4f, 0.4f);

        // 체크마크 (ON 상태 — Toggle이 자동 show/hide)
        GameObject checkMark = MkChild(hideToggleObj, "Checkmark",
            0f, 0.5f, 0f, 0.5f, new Vector2(12, 0), new Vector2(20, 20));
        Image checkImg = checkMark.AddComponent<Image>();
        checkImg.color = new Color(1f, 0.85f, 0.2f);
        hideToggle.graphic = checkImg;

        // 토글 라벨
        MkTextObj(hideToggleObj, "Label", font,
            0f, 0.5f, 0f, 0.5f, new Vector2(35, 0), new Vector2(140, 24),
            12, TextAnchor.MiddleLeft, new Color(0.7f, 0.7f, 0.7f));

        // ════════════════════════════
        //  CharacterListPanel (좌측)
        // ════════════════════════════
        GameObject charListPanel = MkChild(root, "CharacterListPanel",
            0.01f, 0.12f, 0.75f, 0.80f, Vector2.zero, Vector2.zero);

        // ScrollArea
        GameObject scrollArea = MkChild(charListPanel, "ScrollArea",
            0f, 0f, 1f, 1f, Vector2.zero, Vector2.zero);
        ScrollRect scrollRect = scrollArea.AddComponent<ScrollRect>();
        scrollArea.AddComponent<RectMask2D>();
        // raycast 수신용 투명 Image (드래그 스크롤에 필요)
        Image scrollBg = scrollArea.AddComponent<Image>();
        scrollBg.color = new Color(0, 0, 0, 0.01f);

        // Content (VerticalLayoutGroup)
        GameObject content = MkChild(scrollArea, "Content",
            0f, 1f, 1f, 1f, Vector2.zero, Vector2.zero);
        RectTransform contentRt = content.GetComponent<RectTransform>();
        contentRt.pivot = new Vector2(0.5f, 1f);
        VerticalLayoutGroup vlg = content.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 3;
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        ContentSizeFitter csf = content.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scrollRect.content = contentRt;
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.scrollSensitivity = 30f;

        // ════════════════════════════
        //  TrackInfoPanel (하단 반투명)
        // ════════════════════════════
        GameObject trackInfoPanel = MkChild(root, "TrackInfoPanel",
            0.01f, 0.01f, 0.75f, 0.11f, Vector2.zero, Vector2.zero);
        Image trackBg = trackInfoPanel.AddComponent<Image>();
        trackBg.color = new Color(0.1f, 0.1f, 0.15f, 0.85f);

        HorizontalLayoutGroup trackLayout = trackInfoPanel.AddComponent<HorizontalLayoutGroup>();
        trackLayout.spacing = 15;
        trackLayout.padding = new RectOffset(15, 15, 5, 5);
        trackLayout.childAlignment = TextAnchor.MiddleLeft;
        trackLayout.childForceExpandWidth = false;
        trackLayout.childForceExpandHeight = true;

        MkTextObjLayout(trackInfoPanel, "TotalRoundLabel", font, 14,
            new Color(0.9f, 0.9f, 0.9f), 160);
        MkTextObjLayout(trackInfoPanel, "TrackNameLabel", font, 14,
            new Color(0.8f, 0.9f, 1f), 120);
        MkTextObjLayout(trackInfoPanel, "DistanceLabel", font, 14,
            new Color(0.7f, 0.8f, 0.7f), 130);
        MkTextObjLayout(trackInfoPanel, "TrackTypeLabel", font, 14,
            new Color(0.8f, 0.7f, 0.6f), 80);

        // ════════════════════════════
        //  CharacterInfoPopup (기본 비활성, 4개 레이아웃)
        //  기획서 9번: 상단(순위그래프) / 중단좌(일러스트+승률) / 중단우(레이더) / 하단(스킬)
        //  캐릭터 리스트(~0.30) 우측에 배치, 겹침 방지
        // ════════════════════════════
        GameObject infoPopup = MkChild(root, "CharacterInfoPopup",
            0.31f, 0.12f, 0.85f, 0.78f, Vector2.zero, Vector2.zero);
        Image popupBg = infoPopup.AddComponent<Image>();
        popupBg.color = new Color(0.06f, 0.06f, 0.10f, 0.96f);
        popupBg.raycastTarget = true; // 팝업 뒤 클릭 차단
        infoPopup.AddComponent<CharacterInfoPopup>();

        // Outline 효과 (시각적 경계선)
        var popupOutline = infoPopup.AddComponent<Outline>();
        popupOutline.effectColor = new Color(0.3f, 0.4f, 0.6f, 0.5f);
        popupOutline.effectDistance = new Vector2(2, -2);

        // ── Layout1_TopArea (상단 28% — 순위 변동 + 캐릭터 타입) ──
        GameObject layout1 = MkChild(infoPopup, "Layout1_TopArea",
            0f, 0.72f, 1f, 1f, Vector2.zero, Vector2.zero);
        layout1.AddComponent<Image>().color = new Color(0.08f, 0.08f, 0.15f, 0.7f);

        // CharTypeLabel (좌상단)
        MkTextObj(layout1, "CharTypeLabel", font,
            0.03f, 0.82f, 0.03f, 0.82f, Vector2.zero, new Vector2(120, 30),
            20, TextAnchor.MiddleLeft, new Color(1f, 0.4f, 0.4f));

        // CloseBtn (우상단 X — 차트보다 위에 렌더링되도록 마지막 생성)
        GameObject popupCloseBtn = MkChild(layout1, "CloseBtn",
            0.97f, 0.88f, 0.97f, 0.88f, Vector2.zero, new Vector2(30, 30));
        Image closeBg = popupCloseBtn.AddComponent<Image>();
        closeBg.color = new Color(0.5f, 0.3f, 0.3f, 0.8f);
        closeBg.raycastTarget = true;
        Button closeBtnComp = popupCloseBtn.AddComponent<Button>();
        closeBtnComp.targetGraphic = closeBg;
        MkTextObj(popupCloseBtn, "Text", font,
            0.5f, 0.5f, 0.5f, 0.5f, Vector2.zero, new Vector2(30, 30),
            18, TextAnchor.MiddleCenter, Color.white);

        // ── RecentRecordHeader ("최근 경기기록", 노란색) ──
        MkTextObj(layout1, "RecentRecordHeader", font,
            0.03f, 0.66f, 0.97f, 0.78f, Vector2.zero, Vector2.zero,
            18, TextAnchor.MiddleLeft, new Color(1f, 0.85f, 0.2f),
            true /* stretch */);

        // ── ShortDistRow (단거리) ──
        GameObject shortRow = MkChild(layout1, "ShortDistRow",
            0.02f, 0.44f, 0.98f, 0.64f, Vector2.zero, Vector2.zero);
        MkTextObj(shortRow, "ShortDistLabel", font,
            0f, 0f, 0.22f, 1f, Vector2.zero, Vector2.zero,
            16, TextAnchor.MiddleLeft, new Color(0.6f, 0.8f, 1f),
            true);
        MkTextObj(shortRow, "ShortDistRanks", font,
            0.22f, 0f, 1f, 1f, Vector2.zero, Vector2.zero,
            18, TextAnchor.MiddleLeft, Color.white,
            true);

        // ── MidDistRow (중거리) ──
        GameObject midRow = MkChild(layout1, "MidDistRow",
            0.02f, 0.22f, 0.98f, 0.42f, Vector2.zero, Vector2.zero);
        MkTextObj(midRow, "MidDistLabel", font,
            0f, 0f, 0.22f, 1f, Vector2.zero, Vector2.zero,
            16, TextAnchor.MiddleLeft, new Color(0.6f, 0.8f, 1f),
            true);
        MkTextObj(midRow, "MidDistRanks", font,
            0.22f, 0f, 1f, 1f, Vector2.zero, Vector2.zero,
            18, TextAnchor.MiddleLeft, Color.white,
            true);

        // ── LongDistRow (장거리) ──
        GameObject longRow = MkChild(layout1, "LongDistRow",
            0.02f, 0.02f, 0.98f, 0.20f, Vector2.zero, Vector2.zero);
        MkTextObj(longRow, "LongDistLabel", font,
            0f, 0f, 0.22f, 1f, Vector2.zero, Vector2.zero,
            16, TextAnchor.MiddleLeft, new Color(0.6f, 0.8f, 1f),
            true);
        MkTextObj(longRow, "LongDistRanks", font,
            0.22f, 0f, 1f, 1f, Vector2.zero, Vector2.zero,
            18, TextAnchor.MiddleLeft, Color.white,
            true);

        // ── Layout2 (중단 57% — 좌우 분할) ──
        // Layout2_Left (좌측 38% — 일러스트 + 승률)
        GameObject layout2Left = MkChild(infoPopup, "Layout2_Left",
            0f, 0.13f, 0.38f, 0.72f, Vector2.zero, Vector2.zero);
        layout2Left.AddComponent<Image>().color = new Color(0.06f, 0.06f, 0.12f, 0.5f);

        // StoryIconBtn (book 아이콘, 좌상단, 비활성)
        GameObject storyBtn = MkChild(layout2Left, "StoryIconBtn",
            0.08f, 0.93f, 0.08f, 0.93f, Vector2.zero, new Vector2(24, 24));
        Image storyImg = storyBtn.AddComponent<Image>();
        storyImg.color = new Color(0.4f, 0.4f, 0.4f);
        storyBtn.AddComponent<Button>().interactable = false;

        // Illustration (캐릭터 일러스트 — 부모 영역에 반응형 stretch)
        GameObject illustObj = MkChild(layout2Left, "Illustration",
            0.05f, 0.18f, 0.95f, 0.88f, Vector2.zero, Vector2.zero);
        Image illustImg = illustObj.AddComponent<Image>();
        illustImg.color = Color.white;
        illustImg.preserveAspect = true;

        // WinRateLabel (승률)
        MkTextObj(layout2Left, "WinRateLabel", font,
            0.5f, 0.06f, 0.5f, 0.06f, Vector2.zero, new Vector2(160, 28),
            16, TextAnchor.MiddleCenter, new Color(1f, 0.85f, 0.2f));

        // Layout2_Right (우측 62% — 레이더차트, RectMask2D로 오버플로 클리핑)
        GameObject layout2Right = MkChild(infoPopup, "Layout2_Right",
            0.38f, 0.13f, 1f, 0.72f, Vector2.zero, Vector2.zero);
        layout2Right.AddComponent<RectMask2D>();

        // RadarChartArea (레이더차트 컨테이너)
        GameObject radarChartArea = MkChild(layout2Right, "RadarChartArea",
            0.03f, 0.03f, 0.97f, 0.97f, Vector2.zero, Vector2.zero);

        // ── Layout3_Bottom (하단 13% — 스킬) ──
        GameObject layout3 = MkChild(infoPopup, "Layout3_Bottom",
            0f, 0f, 1f, 0.13f, Vector2.zero, Vector2.zero);
        layout3.AddComponent<Image>().color = new Color(0.10f, 0.08f, 0.06f, 0.8f);

        // SkillIcon (sword 아이콘)
        GameObject skillIconObj = MkChild(layout3, "SkillIcon",
            0.04f, 0.5f, 0.04f, 0.5f, Vector2.zero, new Vector2(32, 32));
        Image skillImg = skillIconObj.AddComponent<Image>();
        skillImg.color = Color.white;
        skillImg.preserveAspect = true;

        // SkillDescLabel (스킬 설명)
        MkTextObj(layout3, "SkillDescLabel", font,
            0.10f, 0.5f, 0.10f, 0.5f, new Vector2(40, 0), new Vector2(400, 26),
            14, TextAnchor.MiddleLeft, new Color(0.85f, 0.85f, 0.85f));

        infoPopup.SetActive(false);

        // ════════════════════════════
        //  StartBtn (우측 하단)
        // ════════════════════════════
        GameObject startBtn = MkChild(root, "StartBtn",
            0.82f, 0.02f, 0.82f, 0.02f, Vector2.zero, new Vector2(180, 55));
        Image startBg = startBtn.AddComponent<Image>();
        startBg.color = new Color(0.2f, 0.6f, 0.3f, 0.9f);
        startBg.raycastTarget = true;
        Button startBtnComp = startBtn.AddComponent<Button>();
        startBtnComp.targetGraphic = startBg;
        MkTextObj(startBtn, "Text", font,
            0.5f, 0.5f, 0.5f, 0.5f, Vector2.zero, new Vector2(180, 55),
            24, TextAnchor.MiddleCenter, Color.white);

        return root;
    }

    // ══════════════════════════════════════════════════════
    //  유틸리티 메서드
    // ══════════════════════════════════════════════════════

    /// <summary>자식 RectTransform 오브젝트 생성 (앵커 기반)</summary>
    private static GameObject MkChild(GameObject parent, string name,
        float anchorMinX, float anchorMinY, float anchorMaxX, float anchorMaxY,
        Vector2 anchoredPos, Vector2 sizeDelta)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent.transform, false);
        RectTransform rt = obj.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(anchorMinX, anchorMinY);
        rt.anchorMax = new Vector2(anchorMaxX, anchorMaxY);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = sizeDelta;
        return obj;
    }

    /// <summary>Text 오브젝트 생성</summary>
    private static GameObject MkTextObj(GameObject parent, string name, Font font,
        float anchorX, float anchorY, float anchorMaxX, float anchorMaxY,
        Vector2 anchoredPos, Vector2 sizeDelta,
        int fontSize, TextAnchor alignment, Color color,
        bool stretch = false)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent.transform, false);
        RectTransform rt = obj.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(anchorX, anchorY);
        rt.anchorMax = new Vector2(anchorMaxX, anchorMaxY);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchoredPos;

        if (stretch)
        {
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
        else
        {
            rt.sizeDelta = sizeDelta;
        }

        Text text = obj.AddComponent<Text>();
        text.text = name;
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = color;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        if (font != null) text.font = font;

        return obj;
    }

    /// <summary>LayoutGroup 자식용 Text 오브젝트 (LayoutElement 포함)</summary>
    private static GameObject MkTextObjLayout(GameObject parent, string name, Font font,
        int fontSize, Color color, float preferredWidth)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent.transform, false);
        obj.AddComponent<RectTransform>();

        Text text = obj.AddComponent<Text>();
        text.text = name;
        text.fontSize = fontSize;
        text.alignment = TextAnchor.MiddleLeft;
        text.color = color;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        if (font != null) text.font = font;

        LayoutElement le = obj.AddComponent<LayoutElement>();
        le.preferredWidth = preferredWidth;

        return obj;
    }

    // ══════════════════════════════════════════════════════
    //  C) 프리팹 패치 — 기존 값 유지, 없는 자식만 추가
    // ══════════════════════════════════════════════════════

    [MenuItem("DopamineRace/Patch Betting UI Prefabs (Safe)")]
    public static void PatchPrefabs()
    {
        string prefabPath = PREFAB_DIR + "/BettingPanel.prefab";
        if (!File.Exists(prefabPath.Replace("Assets/", Application.dataPath + "/")))
        {
            Debug.LogError("[Patch] BettingPanel.prefab이 없습니다. 먼저 'Create Betting UI Prefabs'를 실행하세요.");
            return;
        }

        var gs = AssetDatabase.LoadAssetAtPath<GameSettings>("Assets/Resources/GameSettings.asset");
        Font font = gs != null ? gs.mainFont : null;

        using (var scope = new PrefabUtility.EditPrefabContentsScope(prefabPath))
        {
            GameObject root = scope.prefabContentsRoot;
            Transform infoPopup = root.transform.Find("CharacterInfoPopup");
            if (infoPopup == null)
            {
                Debug.LogError("[Patch] CharacterInfoPopup을 찾을 수 없습니다.");
                return;
            }

            // ── 팝업 위치 조정 (좌측 캐릭터 목록과 겹침 방지) ──
            RectTransform popupRt = infoPopup.GetComponent<RectTransform>();
            if (popupRt != null && popupRt.anchorMin.x < 0.31f)
            {
                popupRt.anchorMin = new Vector2(0.31f, popupRt.anchorMin.y);
                popupRt.anchorMax = new Vector2(0.85f, popupRt.anchorMax.y);
                Debug.Log("[Patch] CharacterInfoPopup 위치 우측 이동 (0.28→0.31)");
            }

            Transform layout1 = infoPopup.Find("Layout1_TopArea");
            if (layout1 == null)
            {
                Debug.LogError("[Patch] Layout1_TopArea를 찾을 수 없습니다.");
                return;
            }

            bool changed = false;

            // ── 레거시 제거 ──
            Transform legacy1 = layout1.Find("RankChartArea");
            if (legacy1 != null) { Object.DestroyImmediate(legacy1.gameObject); changed = true; Debug.Log("[Patch] RankChartArea 제거"); }

            Transform legacy2 = layout1.Find("NoRecordLabel");
            if (legacy2 != null) { Object.DestroyImmediate(legacy2.gameObject); changed = true; Debug.Log("[Patch] NoRecordLabel 제거"); }

            // ── 새 요소 추가 (없을 때만) ──
            if (layout1.Find("RecentRecordHeader") == null)
            {
                PatchMkText(layout1, "RecentRecordHeader", font,
                    0.03f, 0.66f, 0.97f, 0.78f, 18, TextAnchor.MiddleLeft,
                    new Color(1f, 0.85f, 0.2f));
                changed = true; Debug.Log("[Patch] RecentRecordHeader 추가");
            }

            changed |= PatchDistRow(layout1, "ShortDistRow", "ShortDistLabel", "ShortDistRanks",
                0.02f, 0.44f, 0.98f, 0.64f, font);
            changed |= PatchDistRow(layout1, "MidDistRow", "MidDistLabel", "MidDistRanks",
                0.02f, 0.22f, 0.98f, 0.42f, font);
            changed |= PatchDistRow(layout1, "LongDistRow", "LongDistLabel", "LongDistRanks",
                0.02f, 0.02f, 0.98f, 0.20f, font);

            if (!changed)
                Debug.Log("[Patch] 변경 사항 없음 — 모든 요소가 이미 존재합니다.");
            else
                Debug.Log("[Patch] BettingPanel 패치 완료! 기존 설정은 유지됩니다.");
        }

        AssetDatabase.Refresh();
    }

    /// <summary>거리 행 패치 (없으면 생성, 있으면 스킵)</summary>
    private static bool PatchDistRow(Transform parent, string rowName,
        string labelName, string ranksName,
        float yMin, float yMinAnc, float yMax, float yMaxAnc, Font font)
    {
        bool added = false;
        Transform row = parent.Find(rowName);
        if (row == null)
        {
            GameObject rowObj = PatchMkChild(parent, rowName, 0.02f, yMinAnc, 0.98f, yMaxAnc);
            row = rowObj.transform;
            added = true;
            Debug.Log($"[Patch] {rowName} 추가");
        }

        if (row.Find(labelName) == null)
        {
            PatchMkText(row, labelName, font,
                0f, 0f, 0.22f, 1f, 16, TextAnchor.MiddleLeft,
                new Color(0.6f, 0.8f, 1f));
            added = true;
        }

        if (row.Find(ranksName) == null)
        {
            var ranksText = PatchMkText(row, ranksName, font,
                0.22f, 0f, 1f, 1f, 18, TextAnchor.MiddleLeft, Color.white);
            ranksText.supportRichText = true;
            added = true;
        }

        return added;
    }

    /// <summary>패치용 자식 생성 (stretch 앵커)</summary>
    private static GameObject PatchMkChild(Transform parent, string name,
        float ancMinX, float ancMinY, float ancMaxX, float ancMaxY)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        RectTransform rt = obj.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(ancMinX, ancMinY);
        rt.anchorMax = new Vector2(ancMaxX, ancMaxY);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        return obj;
    }

    /// <summary>패치용 Text 생성 (stretch 앵커)</summary>
    private static Text PatchMkText(Transform parent, string name, Font font,
        float ancMinX, float ancMinY, float ancMaxX, float ancMaxY,
        int fontSize, TextAnchor alignment, Color color)
    {
        GameObject obj = PatchMkChild(parent, name, ancMinX, ancMinY, ancMaxX, ancMaxY);
        Text text = obj.AddComponent<Text>();
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = color;
        text.supportRichText = true;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        if (font != null) text.font = font;
        return text;
    }

    // ══════════════════════════════════════════════════════
    //  유틸리티 메서드
    // ══════════════════════════════════════════════════════

    /// <summary>디렉토리 자동 생성</summary>
    private static void EnsureDirectory(string path)
    {
        if (!AssetDatabase.IsValidFolder(path))
        {
            string[] parts = path.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }
    }
}
#endif
