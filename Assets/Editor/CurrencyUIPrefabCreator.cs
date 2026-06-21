using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using System.IO;

/// <summary>
/// SPEC-028 Step 2.13 — CurrencyHeader / ExchangeIcon 프리팹 생성기.
/// 메뉴: DopamineRace > 프리팹 생성 > Currency UI Prefabs
///
/// 생성 위치:
///   Assets/Prefabs/UI/CurrencyHeaderPrefab.prefab
///   Assets/Prefabs/UI/ExchangeIconPrefab.prefab
///   Assets/Prefabs/UI/BetAmountModalPrefab.prefab
///   Assets/Prefabs/UI/ExchangeModalPrefab.prefab
/// </summary>
public static class CurrencyUIPrefabCreator
{
    [MenuItem("DopamineRace/프리팹 생성/Currency UI Prefabs")]
    public static void CreateAll()
    {
        EnsureFolder("Assets/Prefabs");
        EnsureFolder("Assets/Prefabs/UI");

        // 디자이너 sprite 우선, 없으면 임시 procedural sprite fallback
        var jellySprite = LoadDesignerOrTempSprite(
            "Assets/Resources/Icon/dopaminejelly_1.png",
            "Assets/Resources/Items/dopamine_jelly_icon.asset");
        var stoneSprite = LoadDesignerOrTempSprite(
            "Assets/Resources/Icon/dopaminestone_1.png",
            "Assets/Resources/Items/dopamine_stone_icon.asset");
        var changeSprite = LoadDesignerOrTempSprite(
            "Assets/Resources/Icon/dopamine_change.png",
            null);

        if (jellySprite == null || stoneSprite == null)
        {
            Debug.LogError("[Prefab] jelly/stone sprite 없음 — Resources/Icon 또는 Items 확인");
            return;
        }

        BuildCurrencyHeaderPrefab(jellySprite, stoneSprite);
        BuildExchangeIconPrefab(changeSprite);
        BuildBetAmountModalPrefab();
        BuildExchangeModalPrefab(stoneSprite);

        // CurrencyItem.asset의 icon 필드도 디자이너 sprite로 갱신
        UpdateCurrencyItemIcon("Assets/Resources/Items/DopamineJelly.asset", jellySprite);
        UpdateCurrencyItemIcon("Assets/Resources/Items/DopamineStone.asset", stoneSprite);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[Prefab] Currency UI 프리팹 4개 생성 완료 + 디자이너 sprite 적용");
    }

    private static Sprite LoadDesignerOrTempSprite(string designerPath, string tempAssetPath)
    {
        // 1순위: 디자이너 PNG
        if (System.IO.File.Exists(designerPath))
        {
            var sp = AssetDatabase.LoadAssetAtPath<Sprite>(designerPath);
            if (sp != null)
            {
                Debug.Log($"[Sprite] 디자이너 sprite 사용: {designerPath}");
                return sp;
            }
        }
        // 2순위: 임시 procedural .asset
        if (!string.IsNullOrEmpty(tempAssetPath))
        {
            var sp = AssetDatabase.LoadAssetAtPath<Sprite>(tempAssetPath);
            if (sp != null)
            {
                Debug.Log($"[Sprite] 임시 sprite fallback: {tempAssetPath}");
                return sp;
            }
        }
        return null;
    }

    private static void UpdateCurrencyItemIcon(string itemAssetPath, Sprite sprite)
    {
        var item = AssetDatabase.LoadAssetAtPath<CurrencyItem>(itemAssetPath);
        if (item == null) return;
        var so = new SerializedObject(item);
        so.FindProperty("icon").objectReferenceValue = sprite;
        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(item);
        Debug.Log($"[CurrencyItem] {itemAssetPath} icon → {sprite.name}");
    }

    // ═══════════════════════════════════════════════════════
    //  CurrencyHeaderPrefab
    // ═══════════════════════════════════════════════════════
    private static void BuildCurrencyHeaderPrefab(Sprite jellySprite, Sprite stoneSprite)
    {
        var root = new GameObject("CurrencyHeader");
        var rt = root.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(360, 80);

        // 배경 없음 (오너 요청 — 반투명 판 제거)

        // ── Jelly 영역 (좌측 50%) ──
        var jellyContainer = NewRect("JellyContainer", root.transform,
            new Vector2(0, 0), new Vector2(0.5f, 1),
            new Vector2(8, 8), new Vector2(-4, -8));

        var jellyIconGo = NewRect("JellyIcon", jellyContainer.transform,
            new Vector2(0, 0), new Vector2(0, 1),
            new Vector2(0, 0), new Vector2(56, 0));
        var jellyIcon = jellyIconGo.AddComponent<Image>();
        jellyIcon.sprite = jellySprite;
        jellyIcon.color = Color.white;
        jellyIcon.raycastTarget = false;
        jellyIcon.preserveAspect = true;

        var jellyTextGo = NewRect("JellyText", jellyContainer.transform,
            new Vector2(0, 0), new Vector2(1, 1),
            new Vector2(64, 0), new Vector2(-4, 0));
        var jellyText = jellyTextGo.AddComponent<Text>();
        jellyText.text = "100";
        jellyText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        jellyText.fontSize = 32;
        jellyText.fontStyle = FontStyle.Bold;
        jellyText.alignment = TextAnchor.MiddleLeft;
        jellyText.color = Color.black;   // 오너 요청: 검은색 폰트
        jellyText.raycastTarget = false;

        // ── Stone 영역 (우측 50%) ──
        var stoneContainer = NewRect("StoneContainer", root.transform,
            new Vector2(0.5f, 0), new Vector2(1, 1),
            new Vector2(4, 8), new Vector2(-8, -8));

        var stoneIconGo = NewRect("StoneIcon", stoneContainer.transform,
            new Vector2(0, 0), new Vector2(0, 1),
            new Vector2(0, 0), new Vector2(56, 0));
        var stoneIcon = stoneIconGo.AddComponent<Image>();
        stoneIcon.sprite = stoneSprite;
        stoneIcon.color = Color.white;
        stoneIcon.raycastTarget = false;
        stoneIcon.preserveAspect = true;

        var stoneTextGo = NewRect("StoneText", stoneContainer.transform,
            new Vector2(0, 0), new Vector2(1, 1),
            new Vector2(64, 0), new Vector2(-4, 0));
        var stoneText = stoneTextGo.AddComponent<Text>();
        stoneText.text = "0";
        stoneText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        stoneText.fontSize = 32;
        stoneText.fontStyle = FontStyle.Bold;
        stoneText.alignment = TextAnchor.MiddleLeft;
        stoneText.color = Color.black;   // 오너 요청: 검은색 폰트
        stoneText.raycastTarget = false;

        // CurrencyHeader 컴포넌트 + 참조 주입
        var header = root.AddComponent<CurrencyHeader>();
        header.SetReferences(jellyText, stoneText, jellyIcon, stoneIcon);

        string path = "Assets/Prefabs/UI/CurrencyHeaderPrefab.prefab";
        PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        Debug.Log($"[Prefab] {path}");
    }

    // ═══════════════════════════════════════════════════════
    //  ExchangeIconPrefab — 디자이너 sprite 있으면 Image로, 없으면 텍스트 💱 fallback
    // ═══════════════════════════════════════════════════════
    private static void BuildExchangeIconPrefab(Sprite changeSprite)
    {
        var root = new GameObject("ExchangeIcon");
        var rt = root.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(64, 64);

        var bg = root.AddComponent<Image>();
        // 디자이너 sprite 있으면 그대로 사용 + 배경 투명, 없으면 컬러 박스
        if (changeSprite != null) {
            bg.sprite = changeSprite;
            bg.color = Color.white;
            bg.preserveAspect = true;
        } else {
            bg.color = new Color(0.25f, 0.4f, 0.6f, 0.95f);
        }
        bg.raycastTarget = true;

        var btn = root.AddComponent<Button>();
        btn.targetGraphic = bg;

        // 디자이너 sprite가 없을 때만 💱 텍스트 라벨
        if (changeSprite == null)
        {
            var labelGo = NewRect("Label", root.transform,
                new Vector2(0, 0), new Vector2(1, 1),
                Vector2.zero, Vector2.zero);
            var label = labelGo.AddComponent<Text>();
            label.text = "💱";
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = 36;
            label.fontStyle = FontStyle.Bold;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = Color.white;
            label.raycastTarget = false;
        }

        string path = "Assets/Prefabs/UI/ExchangeIconPrefab.prefab";
        PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        Debug.Log($"[Prefab] {path} {(changeSprite != null ? "(디자이너 sprite)" : "(텍스트 💱)")}");
    }

    // ═══════════════════════════════════════════════════════
    //  BetAmountModalPrefab — 베팅액 입력 모달
    // ═══════════════════════════════════════════════════════
    private static void BuildBetAmountModalPrefab()
    {
        var root = new GameObject("BetAmountModalRoot");
        var rootRt = root.AddComponent<RectTransform>();
        SetFullStretch(rootRt);

        // 다른 UI보다 항상 위에 그려지도록 정렬 오버라이드 (sortingOrder=1000 > 메인 캔버스 100)
        AddOverlayCanvas(root, 1000);

        // 백드롭
        var backdrop = NewRect("Backdrop", root.transform,
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        var bdImg = backdrop.AddComponent<Image>();
        bdImg.color = new Color(0, 0, 0, 0.7f);
        bdImg.raycastTarget = true;   // 뒤 UGUI 입력 차단 (명시)

        // 모달 패널 (640×580 → 832×754, +30%)
        var panel = NewRectCenter("Modal", root.transform, new Vector2(832, 754));

        // 배경 이미지 (BG_01 가이드 패턴)
        Sprite bgSprite = AssetDatabase.LoadAssetAtPath<Sprite>(
            "Assets/Resources/UI/Img_BG_ListSlot_BG_01.png");
        var panelImg = panel.AddComponent<Image>();
        panelImg.raycastTarget = false;
        if (bgSprite != null)
        {
            panelImg.sprite     = bgSprite;
            panelImg.type       = Image.Type.Sliced;
            panelImg.fillCenter = true;
            panelImg.color      = Color.white;
        }
        else
        {
            Debug.LogWarning("[BetAmountModal] Img_BG_ListSlot_BG_01 없음 — 단색 폴백");
            panelImg.color = new Color(0.12f, 0.10f, 0.18f, 0.95f);
        }

        // 제목 (fontSize 32→42, offsets ×1.3)
        var title = MakeText(panel.transform, "🎰 배팅액을 정하세요",
            new Vector2(0, 1), new Vector2(1, 1),
            new Vector2(0, -33), new Vector2(0, 98),
            42, FontStyle.Bold, new Color(0.55f, 0.30f, 0.00f), TextAnchor.MiddleCenter);

        // 정보 (fontSize 22→29, offsets ×1.3, 흰색→다크 계열)
        var betType = MakeText(panel.transform, "종목: 단승",
            new Vector2(0, 1), new Vector2(1, 1),
            new Vector2(0, -163), new Vector2(0, -124),
            29, FontStyle.Normal, new Color(0.10f, 0.10f, 0.20f), TextAnchor.MiddleCenter);
        var selection = MakeText(panel.transform, "선택: -",
            new Vector2(0, 1), new Vector2(1, 1),
            new Vector2(0, -208), new Vector2(0, -169),
            29, FontStyle.Normal, new Color(0.10f, 0.10f, 0.20f), TextAnchor.MiddleCenter);
        var odds = MakeText(panel.transform, "배당: 1.1x",
            new Vector2(0, 1), new Vector2(1, 1),
            new Vector2(0, -254), new Vector2(0, -215),
            29, FontStyle.Bold, new Color(0.55f, 0.30f, 0.00f), TextAnchor.MiddleCenter);

        // 슬라이더 (offsets ×1.3)
        var sliderObj = NewRect("AmountSlider", panel.transform,
            new Vector2(0.1f, 0.5f), new Vector2(0.6f, 0.5f),
            new Vector2(0, 20), new Vector2(0, 59));
        var slider = sliderObj.AddComponent<Slider>();
        slider.minValue = 1; slider.maxValue = 100; slider.wholeNumbers = true; slider.value = 25;

        var sliderBg = NewRect("SliderBg", sliderObj.transform,
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        sliderBg.AddComponent<Image>().color = new Color(0.3f, 0.3f, 0.3f);

        var fillArea = NewRect("FillArea", sliderObj.transform,
            new Vector2(0, 0), new Vector2(1, 1),
            new Vector2(7, 7), new Vector2(-7, -7));
        var fillObj = NewRect("Fill", fillArea.transform,
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        var fillImg = fillObj.AddComponent<Image>();
        fillImg.color = new Color(0.4f, 0.7f, 1.0f);
        slider.fillRect = fillObj.GetComponent<RectTransform>();

        var handleArea = NewRect("HandleArea", sliderObj.transform,
            new Vector2(0, 0), new Vector2(1, 1),
            new Vector2(13, 0), new Vector2(-13, 0));
        var handleObj = new GameObject("Handle");
        handleObj.transform.SetParent(handleArea.transform, false);
        var handleRt = handleObj.AddComponent<RectTransform>();
        handleRt.sizeDelta = new Vector2(26, 39);
        var handleImg = handleObj.AddComponent<Image>();
        handleImg.color = new Color(0.55f, 0.30f, 0.00f);
        slider.handleRect = handleRt;
        slider.targetGraphic = handleImg;

        // InputField (offsets ×1.3)
        var inputObj = NewRect("AmountInput", panel.transform,
            new Vector2(0.65f, 0.5f), new Vector2(0.9f, 0.5f),
            new Vector2(0, 13), new Vector2(0, 65));
        inputObj.AddComponent<Image>().color = new Color(0.2f, 0.2f, 0.25f);
        var input = inputObj.AddComponent<InputField>();
        input.contentType = InputField.ContentType.IntegerNumber;

        var inputTextObj = NewRect("Text", inputObj.transform,
            Vector2.zero, Vector2.one, new Vector2(13, 7), new Vector2(-13, -7));
        var inputText = inputTextObj.AddComponent<Text>();
        inputText.text = "25";
        inputText.color = Color.white;   // InputField 배경이 어두우므로 흰색 유지
        inputText.alignment = TextAnchor.MiddleCenter;
        inputText.fontSize = 31;
        inputText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        input.textComponent = inputText;

        // 빠른 버튼 4개 -10/-1/+1/+10 (Img_Multiple 스프라이트 배경)
        Sprite multipleSprite = AssetDatabase.LoadAssetAtPath<Sprite>(
            "Assets/Resources/UI/Img_Multiple.png");
        if (multipleSprite == null)
            Debug.LogWarning("[BetAmountModal] Img_Multiple.png 없음 — 단색 폴백");
        var btnMinus10 = MakeButton(panel.transform, "-10",
            new Vector2(0.1f, 0.35f), new Vector2(0.275f, 0.42f), new Color(0.4f, 0.4f, 0.5f), 29, multipleSprite);
        var btnMinus1 = MakeButton(panel.transform, "-1",
            new Vector2(0.295f, 0.35f), new Vector2(0.47f, 0.42f), new Color(0.4f, 0.4f, 0.5f), 29, multipleSprite);
        var btnPlus1 = MakeButton(panel.transform, "+1",
            new Vector2(0.49f, 0.35f), new Vector2(0.665f, 0.42f), new Color(0.4f, 0.4f, 0.5f), 29, multipleSprite);
        var btnPlus10 = MakeButton(panel.transform, "+10",
            new Vector2(0.685f, 0.35f), new Vector2(0.86f, 0.42f), new Color(0.4f, 0.4f, 0.5f), 29, multipleSprite);

        // 미리보기 (fontSize 22→29, 밝은 색→다크 계열)
        var previewWin = MakeText(panel.transform, "적중 시: +0 🟦 (+0 💎)",
            new Vector2(0, 0.22f), new Vector2(1, 0.28f),
            Vector2.zero, Vector2.zero,
            29, FontStyle.Bold, new Color(0.00f, 0.45f, 0.00f), TextAnchor.MiddleCenter);
        var previewLose = MakeText(panel.transform, "실패 시: -0 🟦",
            new Vector2(0, 0.16f), new Vector2(1, 0.22f),
            Vector2.zero, Vector2.zero,
            29, FontStyle.Normal, new Color(0.65f, 0.00f, 0.00f), TextAnchor.MiddleCenter);

        // 에러 텍스트 (fontSize 18→23)
        var errorTxt = MakeText(panel.transform, "",
            new Vector2(0, 0.10f), new Vector2(1, 0.15f),
            Vector2.zero, Vector2.zero,
            23, FontStyle.Normal, new Color(0.70f, 0.00f, 0.00f), TextAnchor.MiddleCenter);

        // 취소·확정 버튼 (fontSize 22→29)
        var btnCancel = MakeButton(panel.transform, "취소",
            new Vector2(0.1f, 0.03f), new Vector2(0.45f, 0.10f), new Color(0.3f, 0.3f, 0.35f), 29);
        var btnConfirm = MakeButton(panel.transform, "배팅 확정 →",
            new Vector2(0.55f, 0.03f), new Vector2(0.9f, 0.10f), new Color(0.3f, 0.6f, 0.3f), 29);

        // 컴포넌트 연결
        var modal = root.AddComponent<BetAmountModal>();
        modal.SetReferences(
            title, betType, selection, odds,
            slider, input,
            btnMinus10, btnMinus1, btnPlus1, btnPlus10,
            previewWin, previewLose,
            btnCancel, btnConfirm, errorTxt,
            backdrop);

        string path = "Assets/Prefabs/UI/BetAmountModalPrefab.prefab";
        PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        Debug.Log($"[Prefab] {path}");
    }

    // ═══════════════════════════════════════════════════════
    //  ExchangeModalPrefab — 환전 팝업
    // ═══════════════════════════════════════════════════════
    private static void BuildExchangeModalPrefab(Sprite stoneSprite)
    {
        var root = new GameObject("ExchangeModalRoot");
        var rootRt = root.AddComponent<RectTransform>();
        SetFullStretch(rootRt);

        // 다른 UI보다 항상 위에 그려지도록 정렬 오버라이드 (sortingOrder=1000 > 메인 캔버스 100)
        AddOverlayCanvas(root, 1000);

        var backdrop = NewRect("Backdrop", root.transform,
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        var bdImg = backdrop.AddComponent<Image>();
        bdImg.color = new Color(0, 0, 0, 0.7f);
        bdImg.raycastTarget = true;   // 뒤 UGUI 입력 차단 (명시)

        // 패널 크기 560×480 → 672×576 (+20%)
        var panel = NewRectCenter("Modal", root.transform, new Vector2(672, 576));
        Sprite bgSprite = AssetDatabase.LoadAssetAtPath<Sprite>(
            "Assets/Resources/UI/Img_BG_ListSlot_BG_01.png");
        Image panelImg = panel.AddComponent<Image>();
        panelImg.raycastTarget = false;
        if (bgSprite != null)
        {
            panelImg.sprite     = bgSprite;
            panelImg.type       = Image.Type.Sliced;
            panelImg.fillCenter = true;
            panelImg.color      = Color.white;
        }
        else
        {
            Debug.LogWarning("[ExchangeModal] Img_BG_ListSlot_BG_01 없음 — 단색 폴백");
            panelImg.color = new Color(0.12f, 0.10f, 0.18f, 0.95f);
        }

        // 밝은 배경에서 잘 읽히는 어두운 색 팔레트
        var darkNavy   = new Color(0.10f, 0.10f, 0.20f);  // 주요 텍스트
        var darkGray   = new Color(0.35f, 0.35f, 0.45f);  // 보조 텍스트
        var midGray    = new Color(0.40f, 0.40f, 0.50f);  // 구분선·노트
        var darkGreen  = new Color(0.10f, 0.48f, 0.10f);  // 완료 메인
        var midGreen   = new Color(0.20f, 0.42f, 0.20f);  // 완료 서브
        var darkRed    = new Color(0.70f, 0.10f, 0.10f);  // 에러
        var orange     = new Color(1.00f, 0.65f, 0.15f);  // 구제(경고)
        var gold       = new Color(1.00f, 0.82f, 0.30f);  // 제목

        var title = MakeText(panel.transform, "💱 도파민 스톤 환전",
            new Vector2(0, 1), new Vector2(1, 1),
            new Vector2(0, -30), new Vector2(0, 90),
            34, FontStyle.Bold, gold, TextAnchor.MiddleCenter);

        var rate = MakeText(panel.transform, "이번 라운드 환전율: 5:1",
            new Vector2(0, 1), new Vector2(1, 1),
            new Vector2(0, -150), new Vector2(0, -114),
            26, FontStyle.Bold, darkNavy, TextAnchor.MiddleCenter);
        var rateDesc = MakeText(panel.transform, "(스톤 5개 → 젤리 1개)",
            new Vector2(0, 1), new Vector2(1, 1),
            new Vector2(0, -190), new Vector2(0, -158),
            22, FontStyle.Normal, darkGray, TextAnchor.MiddleCenter);

        // 현재 보유 행: 스톤 아이콘 + 소지 개수 텍스트
        var holdingRow = new GameObject("HoldingRow");
        holdingRow.transform.SetParent(panel.transform, false);
        var rowRt = holdingRow.AddComponent<RectTransform>();
        rowRt.anchorMin = new Vector2(0, 1);
        rowRt.anchorMax = new Vector2(1, 1);
        rowRt.offsetMin = new Vector2(0, -258);
        rowRt.offsetMax = new Vector2(0, -222);
        var hlg = holdingRow.AddComponent<HorizontalLayoutGroup>();
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.spacing = 6f;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;
        hlg.childControlWidth = false;
        hlg.childControlHeight = false;

        var iconGo = new GameObject("StoneIconImg");
        iconGo.transform.SetParent(holdingRow.transform, false);
        var iconRt = iconGo.AddComponent<RectTransform>();
        iconRt.sizeDelta = new Vector2(34f, 34f);
        var iconImg = iconGo.AddComponent<Image>();
        iconImg.sprite = stoneSprite;
        iconImg.preserveAspect = true;
        iconImg.raycastTarget = false;
        iconImg.color = Color.white;

        var holdingTextGo = new GameObject("HoldingText");
        holdingTextGo.transform.SetParent(holdingRow.transform, false);
        var holdingTextRt = holdingTextGo.AddComponent<RectTransform>();
        holdingTextRt.sizeDelta = new Vector2(200f, 36f);
        var holding = holdingTextGo.AddComponent<Text>();
        holding.text = "현재 보유: 0개";
        holding.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        holding.fontSize = 24;
        holding.fontStyle = FontStyle.Bold;
        holding.color = darkNavy;
        holding.alignment = TextAnchor.MiddleLeft;
        holding.raycastTarget = false;

        var optionsTitle = MakeText(panel.transform, "─── 환전 옵션 ───",
            new Vector2(0, 1), new Vector2(1, 1),
            new Vector2(0, -329), new Vector2(0, -295),
            22, FontStyle.Normal, midGray, TextAnchor.MiddleCenter);

        // 구제 라벨 컨테이너
        var rescueIndic = NewRect("RescueIndicator", panel.transform,
            new Vector2(0, 1), new Vector2(1, 1),
            new Vector2(0, -371), new Vector2(0, -337));
        var rescueText = MakeText(rescueIndic.transform, "⚡ 구제 환전 (1회 한정)",
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero,
            22, FontStyle.Bold, orange, TextAnchor.MiddleCenter);
        rescueIndic.SetActive(false);

        // 환전 버튼
        var exchangeBtn = MakeButton(panel.transform, "환전: 💎0 → 🟦1",
            new Vector2(0.15f, 0.30f), new Vector2(0.85f, 0.40f), new Color(0.3f, 0.5f, 0.7f), 26);
        var exchangeBtnText = exchangeBtn.GetComponentInChildren<Text>();

        // 완료 라벨 컨테이너
        var completedIndic = NewRect("CompletedIndicator", panel.transform,
            new Vector2(0, 0.30f), new Vector2(1, 0.40f),
            Vector2.zero, Vector2.zero);
        var completedTxt = MakeText(completedIndic.transform, "✅ 이번 라운드 환전 완료",
            new Vector2(0, 0.5f), new Vector2(1, 1),
            Vector2.zero, Vector2.zero,
            26, FontStyle.Bold, darkGreen, TextAnchor.MiddleCenter);
        var completedNextTxt = MakeText(completedIndic.transform, "다음 라운드에 다시 가능",
            new Vector2(0, 0), new Vector2(1, 0.5f),
            Vector2.zero, Vector2.zero,
            19, FontStyle.Normal, midGreen, TextAnchor.MiddleCenter);
        completedIndic.SetActive(false);

        // 에러 텍스트
        var errorTxt = MakeText(panel.transform, "스톤이 부족합니다",
            new Vector2(0, 0.30f), new Vector2(1, 0.40f),
            Vector2.zero, Vector2.zero,
            22, FontStyle.Normal, darkRed, TextAnchor.MiddleCenter);
        errorTxt.gameObject.SetActive(false);

        // 노트
        var note = MakeText(panel.transform, "※ 라운드당 1회만 가능",
            new Vector2(0, 0.18f), new Vector2(1, 0.24f),
            Vector2.zero, Vector2.zero,
            17, FontStyle.Italic, midGray, TextAnchor.MiddleCenter);

        // 닫기 버튼
        var closeBtn = MakeButton(panel.transform, "닫기",
            new Vector2(0.3f, 0.04f), new Vector2(0.7f, 0.13f), new Color(0.3f, 0.3f, 0.35f), 26);

        var modal = root.AddComponent<ExchangeModal>();
        modal.SetReferences(
            title, rate, rateDesc, holding, optionsTitle,
            exchangeBtn, exchangeBtnText,
            rescueIndic, rescueText,
            completedIndic, completedTxt, completedNextTxt,
            errorTxt, note,
            closeBtn, backdrop);

        string path = "Assets/Prefabs/UI/ExchangeModalPrefab.prefab";
        PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        Debug.Log($"[Prefab] {path}");
    }

    // ═══════════════════════════════════════════════════════
    //  Helpers
    // ═══════════════════════════════════════════════════════
    private static GameObject NewRect(string name, Transform parent,
        Vector2 anchorMin, Vector2 anchorMax,
        Vector2 offsetMin, Vector2 offsetMax)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = offsetMin;
        rt.offsetMax = offsetMax;
        return go;
    }

    private static GameObject NewRectCenter(string name, Transform parent, Vector2 size)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;
        return go;
    }

    private static void SetFullStretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    /// <summary>
    /// 모달 root에 정렬 오버라이드 Canvas + GraphicRaycaster 추가.
    /// 메인 캔버스(100)보다 높은 sortingOrder로 뒤 UI 입력을 백드롭이 확실히 차단.
    /// ⚠️ overrideSorting은 SerializedObject로 설정 — 직접 프로퍼티(canvas.overrideSorting=true)는
    ///    SaveAsPrefabAsset 후 false로 저장되는 Unity 직렬화 quirk 있음(실증됨).
    /// </summary>
    private static void AddOverlayCanvas(GameObject root, int sortingOrder)
    {
        var canvas = root.AddComponent<Canvas>();
        var so = new SerializedObject(canvas);
        so.FindProperty("m_OverrideSorting").boolValue = true;
        so.FindProperty("m_SortingOrder").intValue = sortingOrder;
        so.ApplyModifiedProperties();
        root.AddComponent<GraphicRaycaster>();
    }

    private static Text MakeText(Transform parent, string text,
        Vector2 anchorMin, Vector2 anchorMax,
        Vector2 offsetMin, Vector2 offsetMax,
        int fontSize, FontStyle style, Color color, TextAnchor anchor)
    {
        var go = NewRect("Text", parent, anchorMin, anchorMax, offsetMin, offsetMax);
        var t = go.AddComponent<Text>();
        t.text = text;
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.fontSize = fontSize;
        t.fontStyle = style;
        t.color = color;
        t.alignment = anchor;
        t.raycastTarget = false;
        return t;
    }

    private static Button MakeButton(Transform parent, string label,
        Vector2 anchorMin, Vector2 anchorMax, Color color, int fontSize = 22, Sprite bgSprite = null)
    {
        var go = NewRect(label + "Btn", parent, anchorMin, anchorMax, Vector2.zero, Vector2.zero);
        var img = go.AddComponent<Image>();
        if (bgSprite != null)
        {
            img.sprite = bgSprite;
            img.type   = Image.Type.Simple;
            img.color  = Color.white;
        }
        else
        {
            img.color = color;
        }
        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;

        var labelGo = NewRect("Text", go.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        var t = labelGo.AddComponent<Text>();
        t.text = label;
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.fontSize = fontSize;
        t.fontStyle = FontStyle.Bold;
        t.color = Color.white;
        t.alignment = TextAnchor.MiddleCenter;
        t.raycastTarget = false;
        return btn;
    }

    private static void EnsureFolder(string path)
    {
        if (!AssetDatabase.IsValidFolder(path))
        {
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            string name = Path.GetFileName(path);
            AssetDatabase.CreateFolder(parent, name);
        }
    }
}
