using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// SPEC-028 Phase 2 신규 UI 임시 빌더.
///   - BetAmountModal (Step 2.3·2.4)
///   - ExchangeModal (Step 2.16)
///   - CurrencyHeader 배치 (Step 2.2)
///   - 환전 아이콘 (Step 2.15)
///
/// 임시 레이아웃 (Unity UGUI 직접 배치) → 검증 후 BettingUIPrefabCreator에서 프리팹화 (Step 2.13).
/// 디자이너 PNG는 추후 CurrencyItem.icon / Sprite 교체로 자동 반영.
/// </summary>
public partial class SceneBootstrapper : MonoBehaviour
{
    // ══════════════════════════════════════════════════════════════
    //  BetAmountModal (Step 2.3·2.4·2.13) — 프리팹 인스턴스화
    // ══════════════════════════════════════════════════════════════
    private void BuildBetAmountModal(Transform root)
    {
        var prefab = Resources.Load<GameObject>("Prefabs/UI/BetAmountModalPrefab");
        if (prefab != null)
        {
            var instance = Instantiate(prefab, root);
            instance.name = "BetAmountModalRoot";
            var rt = instance.GetComponent<RectTransform>();
            if (rt != null) { rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero; }
            _betAmountModal = instance.GetComponent<BetAmountModal>();
            instance.SetActive(false);
            return;
        }

        Debug.LogWarning("[BetAmountModal] 프리팹 미발견 — 동적 폴백 (DopamineRace > Create Currency UI Prefabs 메뉴 실행 필요)");
        BuildBetAmountModalFallback(root);
    }

    private void BuildBetAmountModalFallback(Transform root)
    {
        var modalRoot = new GameObject("BetAmountModalRoot");
        modalRoot.transform.SetParent(root, false);
        AddFullRect(modalRoot);

        // 백드롭 (어두운 반투명, 외부 클릭 차단)
        var backdrop = new GameObject("Backdrop");
        backdrop.transform.SetParent(modalRoot.transform, false);
        var brt = backdrop.AddComponent<RectTransform>();
        SetFullRect(brt);
        var bImg = backdrop.AddComponent<Image>();
        bImg.color = new Color(0, 0, 0, 0.7f);
        bImg.raycastTarget = true;

        // 모달 패널 (중앙)
        var panel = new GameObject("Modal");
        panel.transform.SetParent(modalRoot.transform, false);
        var prt = panel.AddComponent<RectTransform>();
        prt.anchorMin = new Vector2(0.5f, 0.5f);
        prt.anchorMax = new Vector2(0.5f, 0.5f);
        prt.pivot = new Vector2(0.5f, 0.5f);
        prt.sizeDelta = new Vector2(640, 580);
        var pImg = panel.AddComponent<Image>();
        pImg.color = new Color(0.12f, 0.10f, 0.18f, 0.95f);

        // 타이틀
        var title = MkText(panel.transform, "🎰 배팅액을 정하세요",
            new Vector2(0, 1), new Vector2(1, 1),
            new Vector2(0, -50), new Vector2(0, 50),
            32, TextAnchor.MiddleCenter, new Color(1f, 0.85f, 0.4f));

        // 정보 영역
        var betType = MkText(panel.transform, "종목: 단승",
            new Vector2(0, 1), new Vector2(1, 1),
            new Vector2(0, -110), new Vector2(0, 30),
            22, TextAnchor.MiddleCenter, Color.white);
        var selection = MkText(panel.transform, "선택: -",
            new Vector2(0, 1), new Vector2(1, 1),
            new Vector2(0, -145), new Vector2(0, 30),
            22, TextAnchor.MiddleCenter, Color.white);
        var odds = MkText(panel.transform, "배당: 1.1x",
            new Vector2(0, 1), new Vector2(1, 1),
            new Vector2(0, -180), new Vector2(0, 30),
            22, TextAnchor.MiddleCenter, new Color(1f, 0.7f, 0.2f));

        // 슬라이더
        var sliderObj = new GameObject("AmountSlider");
        sliderObj.transform.SetParent(panel.transform, false);
        var srt = sliderObj.AddComponent<RectTransform>();
        srt.anchorMin = new Vector2(0.1f, 0.5f);
        srt.anchorMax = new Vector2(0.6f, 0.5f);
        srt.anchoredPosition = new Vector2(0, 30);
        srt.sizeDelta = new Vector2(0, 30);
        var slider = sliderObj.AddComponent<Slider>();
        slider.minValue = 1; slider.maxValue = 100; slider.wholeNumbers = true; slider.value = 25;

        // Slider 내부 (Background, Fill, Handle 자동 생성)
        var sliderBgGo = new GameObject("SliderBg");
        sliderBgGo.transform.SetParent(sliderObj.transform, false);
        var sliderBgRt = sliderBgGo.AddComponent<RectTransform>();
        SetFullRect(sliderBgRt);
        sliderBgGo.AddComponent<Image>().color = new Color(0.3f, 0.3f, 0.3f);

        var fillArea = new GameObject("FillArea");
        fillArea.transform.SetParent(sliderObj.transform, false);
        var fillAreaRt = fillArea.AddComponent<RectTransform>();
        fillAreaRt.anchorMin = new Vector2(0, 0); fillAreaRt.anchorMax = new Vector2(1, 1);
        fillAreaRt.offsetMin = new Vector2(5, 5); fillAreaRt.offsetMax = new Vector2(-5, -5);
        var fillObj = new GameObject("Fill");
        fillObj.transform.SetParent(fillArea.transform, false);
        var fillRt = fillObj.AddComponent<RectTransform>();
        SetFullRect(fillRt);
        var fillImg = fillObj.AddComponent<Image>();
        fillImg.color = new Color(0.4f, 0.7f, 1.0f);
        slider.fillRect = fillRt;

        var handleArea = new GameObject("HandleArea");
        handleArea.transform.SetParent(sliderObj.transform, false);
        var handleAreaRt = handleArea.AddComponent<RectTransform>();
        handleAreaRt.anchorMin = new Vector2(0, 0); handleAreaRt.anchorMax = new Vector2(1, 1);
        handleAreaRt.offsetMin = new Vector2(10, 0); handleAreaRt.offsetMax = new Vector2(-10, 0);
        var handleObj = new GameObject("Handle");
        handleObj.transform.SetParent(handleArea.transform, false);
        var handleRt = handleObj.AddComponent<RectTransform>();
        handleRt.sizeDelta = new Vector2(20, 30);
        var handleImg = handleObj.AddComponent<Image>();
        handleImg.color = new Color(1f, 0.85f, 0.4f);
        slider.handleRect = handleRt;
        slider.targetGraphic = handleImg;

        // InputField
        var inputObj = new GameObject("AmountInput");
        inputObj.transform.SetParent(panel.transform, false);
        var irt = inputObj.AddComponent<RectTransform>();
        irt.anchorMin = new Vector2(0.65f, 0.5f);
        irt.anchorMax = new Vector2(0.9f, 0.5f);
        irt.anchoredPosition = new Vector2(0, 30);
        irt.sizeDelta = new Vector2(0, 40);
        inputObj.AddComponent<Image>().color = new Color(0.2f, 0.2f, 0.25f);
        var input = inputObj.AddComponent<InputField>();
        input.contentType = InputField.ContentType.IntegerNumber;

        // InputField Text 자식
        var inputTextObj = new GameObject("Text");
        inputTextObj.transform.SetParent(inputObj.transform, false);
        var itRt = inputTextObj.AddComponent<RectTransform>();
        SetFullRect(itRt); itRt.offsetMin = new Vector2(10, 5); itRt.offsetMax = new Vector2(-10, -5);
        var inputText = inputTextObj.AddComponent<Text>();
        inputText.text = "25";
        inputText.color = Color.white;
        inputText.alignment = TextAnchor.MiddleCenter;
        inputText.fontSize = 24;
        inputText.font = font ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        input.textComponent = inputText;

        // 빠른 버튼 4개 (-10/-1/+1/+10)
        var btnMinus10 = MkSimpleButton(panel.transform, "-10", new Vector2(0.1f, 0.35f), new Vector2(0.275f, 0.42f));
        var btnMinus1  = MkSimpleButton(panel.transform, "-1",  new Vector2(0.295f, 0.35f), new Vector2(0.47f, 0.42f));
        var btnPlus1   = MkSimpleButton(panel.transform, "+1",  new Vector2(0.49f, 0.35f), new Vector2(0.665f, 0.42f));
        var btnPlus10  = MkSimpleButton(panel.transform, "+10", new Vector2(0.685f, 0.35f), new Vector2(0.86f, 0.42f));

        // 미리보기
        var previewWin = MkText(panel.transform, "적중 시: +0 🟦 (+0 💎)",
            new Vector2(0, 0.22f), new Vector2(1, 0.28f),
            Vector2.zero, Vector2.zero,
            22, TextAnchor.MiddleCenter, new Color(0.4f, 1f, 0.4f));
        var previewLose = MkText(panel.transform, "실패 시: -0 🟦",
            new Vector2(0, 0.16f), new Vector2(1, 0.22f),
            Vector2.zero, Vector2.zero,
            22, TextAnchor.MiddleCenter, new Color(1f, 0.5f, 0.5f));

        // 에러 텍스트
        var errorTxt = MkText(panel.transform, "",
            new Vector2(0, 0.10f), new Vector2(1, 0.15f),
            Vector2.zero, Vector2.zero,
            18, TextAnchor.MiddleCenter, new Color(1f, 0.4f, 0.4f));

        // 취소·확정 버튼
        var btnCancel  = MkSimpleButton(panel.transform, "취소", new Vector2(0.1f, 0.03f), new Vector2(0.45f, 0.10f),
            new Color(0.3f, 0.3f, 0.35f));
        var btnConfirm = MkSimpleButton(panel.transform, "배팅 확정 →", new Vector2(0.55f, 0.03f), new Vector2(0.9f, 0.10f),
            new Color(0.3f, 0.6f, 0.3f));

        // 컴포넌트 부착 + 참조 주입
        _betAmountModal = modalRoot.AddComponent<BetAmountModal>();
        _betAmountModal.SetReferences(
            title, betType, selection, odds,
            slider, input,
            btnMinus10, btnMinus1, btnPlus1, btnPlus10,
            previewWin, previewLose,
            btnCancel, btnConfirm, errorTxt,
            backdrop);

        modalRoot.SetActive(false);
    }

    // ══════════════════════════════════════════════════════════════
    //  ExchangeModal (Step 2.16·2.13) — 환전 팝업 프리팹 인스턴스화
    // ══════════════════════════════════════════════════════════════
    private void BuildExchangeModal(Transform root)
    {
        var prefab = Resources.Load<GameObject>("Prefabs/UI/ExchangeModalPrefab");
        if (prefab != null)
        {
            var instance = Instantiate(prefab, root);
            instance.name = "ExchangeModalRoot";
            var rt = instance.GetComponent<RectTransform>();
            if (rt != null) { rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero; }
            _exchangeModal = instance.GetComponent<ExchangeModal>();
            instance.SetActive(false);
            return;
        }

        Debug.LogWarning("[ExchangeModal] 프리팹 미발견 — 동적 폴백");
        BuildExchangeModalFallback(root);
    }

    private void BuildExchangeModalFallback(Transform root)
    {
        var modalRoot = new GameObject("ExchangeModalRoot");
        modalRoot.transform.SetParent(root, false);
        AddFullRect(modalRoot);

        var backdrop = new GameObject("Backdrop");
        backdrop.transform.SetParent(modalRoot.transform, false);
        var brt = backdrop.AddComponent<RectTransform>();
        SetFullRect(brt);
        backdrop.AddComponent<Image>().color = new Color(0, 0, 0, 0.7f);

        var panel = new GameObject("Modal");
        panel.transform.SetParent(modalRoot.transform, false);
        var prt = panel.AddComponent<RectTransform>();
        prt.anchorMin = new Vector2(0.5f, 0.5f);
        prt.anchorMax = new Vector2(0.5f, 0.5f);
        prt.pivot = new Vector2(0.5f, 0.5f);
        prt.sizeDelta = new Vector2(560, 480);
        panel.AddComponent<Image>().color = new Color(0.12f, 0.10f, 0.18f, 0.95f);

        var title = MkText(panel.transform, "💱 도파민 스톤 환전",
            new Vector2(0, 1), new Vector2(1, 1),
            new Vector2(0, -50), new Vector2(0, 50),
            28, TextAnchor.MiddleCenter, new Color(1f, 0.85f, 0.4f));

        var rate = MkText(panel.transform, "이번 라운드 환전율: 5:1",
            new Vector2(0, 1), new Vector2(1, 1),
            new Vector2(0, -110), new Vector2(0, 30),
            22, TextAnchor.MiddleCenter, Color.white);
        var rateDesc = MkText(panel.transform, "(스톤 5개 → 젤리 1개)",
            new Vector2(0, 1), new Vector2(1, 1),
            new Vector2(0, -145), new Vector2(0, 26),
            18, TextAnchor.MiddleCenter, new Color(0.7f, 0.7f, 0.8f));

        var holding = MkText(panel.transform, "현재 보유: 🟦0  💎0",
            new Vector2(0, 1), new Vector2(1, 1),
            new Vector2(0, -200), new Vector2(0, 30),
            20, TextAnchor.MiddleCenter, Color.white);

        var optionsTitle = MkText(panel.transform, "─── 환전 옵션 ───",
            new Vector2(0, 1), new Vector2(1, 1),
            new Vector2(0, -260), new Vector2(0, 28),
            18, TextAnchor.MiddleCenter, new Color(0.6f, 0.6f, 0.7f));

        // 구제 라벨 컨테이너
        var rescueIndic = new GameObject("RescueIndicator");
        rescueIndic.transform.SetParent(panel.transform, false);
        var rrt = rescueIndic.AddComponent<RectTransform>();
        rrt.anchorMin = new Vector2(0, 1); rrt.anchorMax = new Vector2(1, 1);
        rrt.anchoredPosition = new Vector2(0, -295);
        rrt.sizeDelta = new Vector2(0, 28);
        var rescueText = MkText(rescueIndic.transform, "⚡ 구제 환전 (1회 한정)",
            new Vector2(0, 0), new Vector2(1, 1),
            Vector2.zero, Vector2.zero,
            18, TextAnchor.MiddleCenter, new Color(1f, 0.7f, 0.2f));
        rescueIndic.SetActive(false);

        // 환전 버튼
        var exchangeBtn = MkSimpleButton(panel.transform, "환전: 💎0 → 🟦1",
            new Vector2(0.15f, 0.30f), new Vector2(0.85f, 0.40f),
            new Color(0.3f, 0.5f, 0.7f));
        var exchangeBtnText = exchangeBtn.GetComponentInChildren<Text>();

        // 완료 라벨 컨테이너
        var completedIndic = new GameObject("CompletedIndicator");
        completedIndic.transform.SetParent(panel.transform, false);
        var crt = completedIndic.AddComponent<RectTransform>();
        crt.anchorMin = new Vector2(0, 0.30f); crt.anchorMax = new Vector2(1, 0.40f);
        SetFullRect(crt);
        var completedTxt = MkText(completedIndic.transform, "✅ 이번 라운드 환전 완료",
            new Vector2(0, 0.5f), new Vector2(1, 1),
            Vector2.zero, Vector2.zero,
            22, TextAnchor.MiddleCenter, new Color(0.5f, 1f, 0.5f));
        var completedNextTxt = MkText(completedIndic.transform, "다음 라운드에 다시 가능",
            new Vector2(0, 0), new Vector2(1, 0.5f),
            Vector2.zero, Vector2.zero,
            16, TextAnchor.MiddleCenter, new Color(0.7f, 0.9f, 0.7f));
        completedIndic.SetActive(false);

        // 에러 텍스트
        var errorTxt = MkText(panel.transform, "스톤이 부족합니다",
            new Vector2(0, 0.30f), new Vector2(1, 0.40f),
            Vector2.zero, Vector2.zero,
            18, TextAnchor.MiddleCenter, new Color(1f, 0.5f, 0.5f));
        errorTxt.gameObject.SetActive(false);

        // 노트
        var note = MkText(panel.transform, "※ 라운드당 1회만 가능",
            new Vector2(0, 0.18f), new Vector2(1, 0.24f),
            Vector2.zero, Vector2.zero,
            14, TextAnchor.MiddleCenter, new Color(0.6f, 0.6f, 0.7f));

        // 닫기 버튼
        var closeBtn = MkSimpleButton(panel.transform, "닫기",
            new Vector2(0.3f, 0.04f), new Vector2(0.7f, 0.13f),
            new Color(0.3f, 0.3f, 0.35f));

        // 컴포넌트 부착 + 참조 주입
        _exchangeModal = modalRoot.AddComponent<ExchangeModal>();
        _exchangeModal.SetReferences(
            title, rate, rateDesc, holding, optionsTitle,
            exchangeBtn, exchangeBtnText,
            rescueIndic, rescueText,
            completedIndic, completedTxt, completedNextTxt,
            errorTxt, note,
            closeBtn, backdrop);

        modalRoot.SetActive(false);
    }

    // ══════════════════════════════════════════════════════════════
    //  유틸 — 단순 버튼 + RectTransform 헬퍼
    // ══════════════════════════════════════════════════════════════

    /// <summary>0~1 anchor 기준의 단순 버튼 생성 (Sprite 없음, 색만).</summary>
    private Button MkSimpleButton(Transform parent, string label,
        Vector2 anchorMin, Vector2 anchorMax, Color? color = null)
    {
        var go = new GameObject(label + "Btn");
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        var img = go.AddComponent<Image>();
        img.color = color ?? new Color(0.4f, 0.4f, 0.5f);
        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;

        var labelGo = new GameObject("Text");
        labelGo.transform.SetParent(go.transform, false);
        var lrt = labelGo.AddComponent<RectTransform>();
        SetFullRect(lrt);
        var t = labelGo.AddComponent<Text>();
        t.text = label;
        t.color = Color.white;
        t.alignment = TextAnchor.MiddleCenter;
        t.fontSize = 22;
        t.font = font ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        return btn;
    }

    private static void SetFullRect(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    // ══════════════════════════════════════════════════════════════
    //  CurrencyHeader (Step 2.1·2.2·2.13) — 프리팹 인스턴스화
    //  Resources/Prefabs/UI/CurrencyHeaderPrefab.prefab
    // ══════════════════════════════════════════════════════════════
    private void BuildCurrencyHeader(Transform bettingUIRoot)
    {
        var prefab = Resources.Load<GameObject>("Prefabs/UI/CurrencyHeaderPrefab");
        if (prefab == null)
        {
            Debug.LogWarning("[CurrencyHeader] 프리팹 미발견 — 동적 폴백 (DopamineRace > Create Currency UI Prefabs 메뉴 실행 필요)");
            BuildCurrencyHeaderFallback(bettingUIRoot);
            return;
        }

        var go = Instantiate(prefab, bettingUIRoot);
        go.name = "CurrencyHeader";
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(1, 1);
        rt.anchorMax = new Vector2(1, 1);
        rt.pivot = new Vector2(1, 1);
        rt.anchoredPosition = new Vector2(-30, -30);

        _currencyHeader = go.GetComponent<CurrencyHeader>();

        // SPEC-028 UC9: 기존 myPointLabel 비활성화 (총점 영역 교체)
        if (myPointLabel != null)
            myPointLabel.gameObject.SetActive(false);
    }

    private void BuildCurrencyHeaderFallback(Transform bettingUIRoot)
    {
        var headerGo = new GameObject("CurrencyHeader");
        headerGo.transform.SetParent(bettingUIRoot, false);
        var rt = headerGo.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(1, 1);
        rt.anchorMax = new Vector2(1, 1);
        rt.pivot = new Vector2(1, 1);
        rt.sizeDelta = new Vector2(360, 60);
        rt.anchoredPosition = new Vector2(-30, -30);

        var bg = headerGo.AddComponent<Image>();
        bg.color = new Color(0, 0, 0, 0.5f);

        var jellyText = MkText(headerGo.transform, "🟦 100",
            new Vector2(0, 0), new Vector2(0.5f, 1),
            Vector2.zero, Vector2.zero,
            24, TextAnchor.MiddleCenter, new Color(0.5f, 0.8f, 1f));
        var stoneText = MkText(headerGo.transform, "💎 0",
            new Vector2(0.5f, 0), new Vector2(1, 1),
            Vector2.zero, Vector2.zero,
            24, TextAnchor.MiddleCenter, new Color(0.4f, 1f, 1f));

        _currencyHeader = headerGo.AddComponent<CurrencyHeader>();
        _currencyHeader.SetReferences(jellyText, stoneText);

        if (myPointLabel != null)
            myPointLabel.gameObject.SetActive(false);
    }

    // ══════════════════════════════════════════════════════════════
    //  Exchange Icon (Step 2.15·2.13) — 프리팹 인스턴스화
    //  Resources/Prefabs/UI/ExchangeIconPrefab.prefab
    // ══════════════════════════════════════════════════════════════
    private void BuildExchangeIcon(Transform bettingUIRoot)
    {
        var prefab = Resources.Load<GameObject>("Prefabs/UI/ExchangeIconPrefab");
        if (prefab == null)
        {
            Debug.LogWarning("[ExchangeIcon] 프리팹 미발견 — 동적 폴백");
            BuildExchangeIconFallback(bettingUIRoot);
            return;
        }

        var go = Instantiate(prefab, bettingUIRoot);
        go.name = "ExchangeIcon";
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(1, 1);
        rt.anchorMax = new Vector2(1, 1);
        rt.pivot = new Vector2(1, 1);
        rt.anchoredPosition = new Vector2(-30, -120);   // 헤더 아래

        _exchangeIconButton = go.GetComponent<Button>();
        _exchangeIconButton?.onClick.AddListener(() => _exchangeModal?.Show());
    }

    private void BuildExchangeIconFallback(Transform bettingUIRoot)
    {
        var btnGo = new GameObject("ExchangeIcon");
        btnGo.transform.SetParent(bettingUIRoot, false);
        var rt = btnGo.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(1, 1);
        rt.anchorMax = new Vector2(1, 1);
        rt.pivot = new Vector2(1, 1);
        rt.sizeDelta = new Vector2(56, 56);
        rt.anchoredPosition = new Vector2(-30, -120);

        var img = btnGo.AddComponent<Image>();
        img.color = new Color(0.2f, 0.35f, 0.5f, 0.9f);

        _exchangeIconButton = btnGo.AddComponent<Button>();
        _exchangeIconButton.targetGraphic = img;
        _exchangeIconButton.onClick.AddListener(() => _exchangeModal?.Show());

        MkText(btnGo.transform, "💱",
            new Vector2(0, 0), new Vector2(1, 1),
            Vector2.zero, Vector2.zero,
            32, TextAnchor.MiddleCenter, Color.white);
    }
}
