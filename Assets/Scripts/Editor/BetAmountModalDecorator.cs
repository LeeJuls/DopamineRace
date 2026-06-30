using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class BetAmountModalDecorator
{
    private const string PREFAB_PATH = "Assets/Prefabs/UI/BetAmountModalPrefab.prefab";

    private const string JELLY_NAME_KEY = "str.dopamine.item.name.dopaminejelly";
    private const string JELLY_DESC_KEY = "str.dopamine.item.desc.dopaminejelly";

    // ──────────────────────────────────────────────────────────────
    //  배팅가능 표시 (우상단: "배팅가능:" 라벨 + 젤리 아이콘 + 보유 개수)
    //  젤리 아이콘 클릭 → ItemInfoTrigger → 설명 팝업 (SPEC-050 재사용)
    // ──────────────────────────────────────────────────────────────
    [MenuItem("DopamineRace/Add BetAmountModal Bettable Display")]
    public static void AddBettableDisplay()
    {
        GameObject root = PrefabUtility.LoadPrefabContents(PREFAB_PATH);
        if (root == null) { Debug.LogError("[BetAmountModalDecorator] 프리팹 로드 실패"); return; }

        Transform modal = FindDeep(root.transform, "Modal");
        if (modal == null)
        {
            Debug.LogError("[BetAmountModalDecorator] Modal 노드를 찾을 수 없음");
            PrefabUtility.UnloadPrefabContents(root);
            return;
        }

        // BetAmountModal 컴포넌트는 프리팹 루트에 있음 (Modal 자식 아님)
        var modalComp = root.GetComponentInChildren<BetAmountModal>(true);

        // 멱등: 이미 존재하면 참조 재연결 + 오버레이 보강 후 종료
        var existing = FindDeep(modal, "BettableInfo");
        if (existing != null)
        {
            WireModalRefs(modalComp, FindDeep(existing, "BettableLabel"), FindDeep(existing, "JellyCount"));
            EnsureClickOverlay(existing);
            PrefabUtility.SaveAsPrefabAsset(root, PREFAB_PATH);
            PrefabUtility.UnloadPrefabContents(root);
            Debug.Log("[BetAmountModalDecorator] BettableInfo 이미 존재 — 참조 재연결 + 오버레이 보강 후 저장");
            return;
        }

        // 기존 모달 Text의 폰트 복사 (CJK 라우팅 등 동일 처리 상속)
        Font font = null;
        var anyText = modal.GetComponentInChildren<Text>(true);
        if (anyText != null) font = anyText.font;

        // ── 컨테이너: 우상단 가로 스트립 ──
        var container = new GameObject("BettableInfo", typeof(RectTransform));
        container.transform.SetParent(modal, false);
        var crt = (RectTransform)container.transform;
        crt.anchorMin = new Vector2(0.60f, 0.880f);
        crt.anchorMax = new Vector2(0.98f, 0.965f);
        crt.offsetMin = Vector2.zero;
        crt.offsetMax = Vector2.zero;

        Color labelColor = new Color(0.1f, 0.2f, 0.45f, 1f);

        // ── "배팅가능:" 라벨 (Best Fit → 긴 번역 자동 축소) ──
        var label = MakeText(crt, "BettableLabel", "배팅가능:", font, labelColor, TextAnchor.MiddleRight);
        var lrt = label.rectTransform;
        lrt.anchorMin = new Vector2(0f, 0f);
        lrt.anchorMax = new Vector2(0.52f, 1f);
        lrt.offsetMin = Vector2.zero;
        lrt.offsetMax = new Vector2(-4f, 0f);   // 아이콘과 약간 간격
        label.resizeTextForBestFit = true;
        label.resizeTextMinSize = 10;
        label.resizeTextMaxSize = 26;
        label.fontSize = 26;

        // ── 젤리 아이콘 (클릭 → 설명 팝업) ──
        var iconGo = new GameObject("JellyIcon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        iconGo.transform.SetParent(crt, false);
        var irt = (RectTransform)iconGo.transform;
        irt.anchorMin = new Vector2(0.55f, 0.5f);
        irt.anchorMax = new Vector2(0.55f, 0.5f);
        irt.pivot = new Vector2(0.5f, 0.5f);
        irt.sizeDelta = new Vector2(34f, 34f);
        irt.anchoredPosition = Vector2.zero;
        var iconImg = iconGo.GetComponent<Image>();
        iconImg.sprite = LoadJellySprite();
        iconImg.preserveAspect = true;
        iconImg.raycastTarget = true;   // ItemInfoTrigger 클릭 감지용 Graphic

        // ItemInfoTrigger + 젤리 키 주입 (베이크 타임 직렬화)
        var trigger = iconGo.AddComponent<ItemInfoTrigger>();
        var tso = new SerializedObject(trigger);
        tso.FindProperty("itemNameKey").stringValue = JELLY_NAME_KEY;
        tso.FindProperty("itemDescKey").stringValue = JELLY_DESC_KEY;
        tso.ApplyModifiedProperties();

        // ── 보유 개수 ──
        var count = MakeText(crt, "JellyCount", "100", font, labelColor, TextAnchor.MiddleLeft);
        var nrt = count.rectTransform;
        nrt.anchorMin = new Vector2(0.62f, 0f);
        nrt.anchorMax = new Vector2(1f, 1f);
        nrt.offsetMin = new Vector2(2f, 0f);
        nrt.offsetMax = Vector2.zero;
        count.fontSize = 26;

        // ── BetAmountModal 참조 연결 (컴포넌트는 루트에 있음) ──
        WireModalRefs(modalComp, label.transform, count.transform);

        // ── 전체 영역 클릭 오버레이 ──
        EnsureClickOverlay(crt);

        PrefabUtility.SaveAsPrefabAsset(root, PREFAB_PATH);
        PrefabUtility.UnloadPrefabContents(root);
        Debug.Log("[BetAmountModalDecorator] BettableInfo 추가 완료 → BetAmountModalPrefab.prefab 저장");
    }

    // BettableInfo 전체 클릭 → ItemInfoTrigger
    // - BettableLabel·JellyCount Text: raycastTarget=false (클릭 가로채기 방지)
    // - JellyIcon Image: raycastTarget=false, ItemInfoTrigger 제거 (오버레이가 대신 처리)
    // - BettableClickOverlay: 투명 Image(full-fill) + ItemInfoTrigger (최상단 자식 → 모든 클릭 수신)
    private static void EnsureClickOverlay(Transform bettableInfo)
    {
        // 이미 있으면 스킵
        if (FindDeep(bettableInfo, "BettableClickOverlay") != null) return;

        // Text 노드 raycastTarget=false
        var labelTf = FindDeep(bettableInfo, "BettableLabel");
        if (labelTf != null)
        {
            var t = labelTf.GetComponent<Text>();
            if (t != null) t.raycastTarget = false;
        }
        var countTf = FindDeep(bettableInfo, "JellyCount");
        if (countTf != null)
        {
            var t = countTf.GetComponent<Text>();
            if (t != null) t.raycastTarget = false;
        }

        // JellyIcon: ItemInfoTrigger 제거, raycastTarget=false
        var iconTf = FindDeep(bettableInfo, "JellyIcon");
        if (iconTf != null)
        {
            var trig = iconTf.GetComponent<ItemInfoTrigger>();
            if (trig != null) Object.DestroyImmediate(trig);
            var img = iconTf.GetComponent<Image>();
            if (img != null) img.raycastTarget = false;
        }

        // 투명 오버레이 (BettableInfo의 마지막 자식 → 최상단 렌더, 클릭 먼저 수신)
        var overlay = new GameObject("BettableClickOverlay",
            typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        overlay.transform.SetParent(bettableInfo, false);
        var ort = (RectTransform)overlay.transform;
        ort.anchorMin = Vector2.zero;
        ort.anchorMax = Vector2.one;
        ort.offsetMin = Vector2.zero;
        ort.offsetMax = Vector2.zero;
        var overlayImg = overlay.GetComponent<Image>();
        overlayImg.color = new Color(0f, 0f, 0f, 0f);
        overlayImg.raycastTarget = true;

        var trigger = overlay.AddComponent<ItemInfoTrigger>();
        var tso = new SerializedObject(trigger);
        tso.FindProperty("itemNameKey").stringValue = JELLY_NAME_KEY;
        tso.FindProperty("itemDescKey").stringValue = JELLY_DESC_KEY;
        tso.ApplyModifiedProperties();

        Debug.Log("[BetAmountModalDecorator] BettableClickOverlay 추가 완료 — BettableInfo 전체 클릭 가능");
    }

    private static void WireModalRefs(BetAmountModal modalComp, Transform labelTf, Transform countTf)
    {
        if (modalComp == null)
        {
            Debug.LogWarning("[BetAmountModalDecorator] BetAmountModal 컴포넌트 없음 — 참조 연결 스킵");
            return;
        }
        var mso = new SerializedObject(modalComp);
        mso.FindProperty("bettableLabel").objectReferenceValue =
            labelTf != null ? labelTf.GetComponent<Text>() : null;
        mso.FindProperty("jellyCountText").objectReferenceValue =
            countTf != null ? countTf.GetComponent<Text>() : null;
        mso.ApplyModifiedProperties();
    }

    private static Text MakeText(Transform parent, string name, string content,
                                 Font font, Color color, TextAnchor anchor)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        go.transform.SetParent(parent, false);
        var t = go.GetComponent<Text>();
        t.text = content;
        if (font != null) t.font = font;
        t.color = color;
        t.alignment = anchor;
        t.fontSize = 26;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Truncate;
        return t;
    }

    private static Sprite LoadJellySprite()
    {
        // CurrencyHeader 패턴: CurrencyItem.icon 우선, 폴백 직접 스프라이트
        var item = Resources.Load<CurrencyItem>("Items/DopamineJelly");
        if (item != null && item.icon != null) return item.icon;
        var sp = Resources.Load<Sprite>("Icon/dopaminejelly_1");
        if (sp == null) Debug.LogWarning("[BetAmountModalDecorator] 젤리 스프라이트 로드 실패");
        return sp;
    }

    [MenuItem("DopamineRace/Decorate BetAmountModal (물방울)")]
    public static void Decorate()
    {
        GameObject root = PrefabUtility.LoadPrefabContents(PREFAB_PATH);
        if (root == null) { Debug.LogError("[BetAmountModalDecorator] 프리팹 로드 실패"); return; }

        Transform modal = FindDeep(root.transform, "Modal");
        if (modal == null)
        {
            Debug.LogError("[BetAmountModalDecorator] Modal 노드를 찾을 수 없음");
            PrefabUtility.UnloadPrefabContents(root);
            return;
        }

        // 중복 방지
        if (modal.GetComponent<WaterDropDecor>() != null)
        {
            Debug.LogWarning("[BetAmountModalDecorator] WaterDropDecor 이미 존재 — 스킵");
            PrefabUtility.UnloadPrefabContents(root);
            return;
        }

        WaterDropDecor decor = modal.gameObject.AddComponent<WaterDropDecor>();
        var so = new SerializedObject(decor);
        so.FindProperty("dropCount").intValue = 7;
        so.FindProperty("sizeMin").floatValue = 4f;
        so.FindProperty("sizeMax").floatValue = 10f;
        so.ApplyModifiedProperties();

        PrefabUtility.SaveAsPrefabAsset(root, PREFAB_PATH);
        PrefabUtility.UnloadPrefabContents(root);
        Debug.Log("[BetAmountModalDecorator] BetAmountModalPrefab.prefab 저장 완료");
    }

    private static Transform FindDeep(Transform parent, string name)
    {
        if (parent.name == name) return parent;
        foreach (Transform child in parent)
        {
            Transform found = FindDeep(child, name);
            if (found != null) return found;
        }
        return null;
    }
}
