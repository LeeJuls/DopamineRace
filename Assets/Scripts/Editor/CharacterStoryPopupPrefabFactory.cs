#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

/// <summary>
/// CharacterStoryPopup 프리팹 생성 + BettingPanel.prefab에 nested instance로 배치.
/// 메뉴: DopamineRace → 프리팹 생성 → CharacterStory Popup Prefab
/// 생성 위치: Assets/Prefabs/UI/CharacterStoryPopup.prefab
/// 재실행 가능(멱등) — 기존 nested instance가 있으면 제거 후 재삽입.
/// </summary>
public static class CharacterStoryPopupPrefabFactory
{
    private const string STORY_PREFAB_PATH   = "Assets/Prefabs/UI/CharacterStoryPopup.prefab";
    private const string BETTING_PREFAB_PATH = "Assets/Prefabs/UI/BettingPanel.prefab";
    private const string BG_SPRITE_PATH      = "Assets/Resources/UI/Img_BG_ListSlot_BG_01.png";

    private static readonly Vector2 BOX_SIZE = new Vector2(720f, 780f);
    private static readonly Color   TEXT_COLOR = new Color(0.15f, 0.25f, 0.35f, 1f);

    [MenuItem("DopamineRace/프리팹 생성/CharacterStory Popup Prefab")]
    public static void CreatePrefab()
    {
        // 1) CharacterStoryPopup.prefab 신규 생성
        var root = BuildHierarchy(out var box, out var storyText, out var scrollRect, out var closeBtn);

        var popup = root.AddComponent<CharacterStoryPopup>();
        AssignReferences(popup, box, storyText, scrollRect, closeBtn);

        PrefabUtility.SaveAsPrefabAsset(root, STORY_PREFAB_PATH);
        Object.DestroyImmediate(root);

        // Canvas가 붙은 루트 GameObject는 SaveAsPrefabAsset 과정에서 RectTransform 앵커가
        // (0,0)-(0,0) 포인트 앵커로 리셋되는 현상 발견 — 저장 후 재로드하여 강제 재설정 후 재저장.
        FixRootStretchAnchor();

        Debug.Log($"[CharacterStoryPopupFactory] 프리팹 생성 완료 → {STORY_PREFAB_PATH}");

        // 2) BettingPanel.prefab 루트 자식으로 nested instance 삽입 (중복 방지 가드)
        // FixRootStretchAnchor()의 재저장 이후 최신 상태를 확실히 반영하도록 다시 로드해서 전달.
        var savedPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(STORY_PREFAB_PATH);
        PlaceIntoBettingPanel(savedPrefab);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private static void FixRootStretchAnchor()
    {
        GameObject root = PrefabUtility.LoadPrefabContents(STORY_PREFAB_PATH);
        if (root == null)
        {
            Debug.LogError($"[CharacterStoryPopupFactory] 앵커 보정용 재로드 실패: {STORY_PREFAB_PATH}");
            return;
        }

        var rt = root.GetComponent<RectTransform>();
        var so = new SerializedObject(rt);
        so.FindProperty("m_AnchorMin").vector2Value = Vector2.zero;
        so.FindProperty("m_AnchorMax").vector2Value = Vector2.one;
        so.FindProperty("m_AnchoredPosition").vector2Value = Vector2.zero;
        so.FindProperty("m_SizeDelta").vector2Value = Vector2.zero;
        so.FindProperty("m_LocalScale").vector3Value = Vector3.one; // Canvas 추가 시 scale까지 0으로 리셋되는 quirk 동반 보정
        so.ApplyModifiedProperties();

        PrefabUtility.SaveAsPrefabAsset(root, STORY_PREFAB_PATH);
        PrefabUtility.UnloadPrefabContents(root);

        Debug.Log("[CharacterStoryPopupFactory] 루트 스트레치 앵커+스케일 보정 완료");
    }

    private static void PlaceIntoBettingPanel(GameObject storyPopupAsset)
    {
        GameObject bettingRoot = PrefabUtility.LoadPrefabContents(BETTING_PREFAB_PATH);
        if (bettingRoot == null)
        {
            Debug.LogError($"[CharacterStoryPopupFactory] BettingPanel.prefab 로드 실패: {BETTING_PREFAB_PATH}");
            return;
        }

        // 중복 방지 가드 — 기존 nested instance 있으면 제거 후 재삽입(멱등)
        Transform existing = bettingRoot.transform.Find("CharacterStoryPopup");
        if (existing != null)
        {
            Object.DestroyImmediate(existing.gameObject);
            Debug.Log("[CharacterStoryPopupFactory] 기존 CharacterStoryPopup 인스턴스 제거 — 재삽입 진행");
        }

        // InstantiatePrefab(prefab, parent) 오버로드는 worldPositionStays=true로 부모를 설정해
        // 스트레치 앵커의 sizeDelta가 역산되며 깨짐 → 부모 없이 생성 후 SetParent(.., false)로 1회만 배치.
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(storyPopupAsset);
        instance.transform.SetParent(bettingRoot.transform, false);
        instance.name = "CharacterStoryPopup";

        // Canvas가 붙은 인스턴스도 동일하게 앵커가 (0,0)-(0,0)으로 깨지는 quirk 발생 →
        // SaveAsPrefabAsset 직전 SerializedObject로 강제 재설정.
        var instRt = instance.GetComponent<RectTransform>();
        var instSo = new SerializedObject(instRt);
        instSo.FindProperty("m_AnchorMin").vector2Value = Vector2.zero;
        instSo.FindProperty("m_AnchorMax").vector2Value = Vector2.one;
        instSo.FindProperty("m_AnchoredPosition").vector2Value = Vector2.zero;
        instSo.FindProperty("m_SizeDelta").vector2Value = Vector2.zero;
        instSo.FindProperty("m_LocalScale").vector3Value = Vector3.one;
        instSo.ApplyModifiedProperties();

        PrefabUtility.SaveAsPrefabAsset(bettingRoot, BETTING_PREFAB_PATH);
        PrefabUtility.UnloadPrefabContents(bettingRoot);

        Debug.Log("[CharacterStoryPopupFactory] BettingPanel.prefab에 nested instance 배치 완료");

        // 위 저장에서도 재차 깨질 가능성을 배제하기 위해 재로드 후 최종 검증+보정.
        FixBettingPanelInstanceAnchor();
    }

    private static void FixBettingPanelInstanceAnchor()
    {
        GameObject bettingRoot = PrefabUtility.LoadPrefabContents(BETTING_PREFAB_PATH);
        if (bettingRoot == null) return;

        Transform story = bettingRoot.transform.Find("CharacterStoryPopup");
        if (story == null)
        {
            PrefabUtility.UnloadPrefabContents(bettingRoot);
            return;
        }

        var rt = story.GetComponent<RectTransform>();
        if (rt.anchorMin == Vector2.zero && rt.anchorMax == Vector2.one && rt.localScale == Vector3.one)
        {
            // 이미 정상 — 재저장 불필요
            PrefabUtility.UnloadPrefabContents(bettingRoot);
            return;
        }

        var so = new SerializedObject(rt);
        so.FindProperty("m_AnchorMin").vector2Value = Vector2.zero;
        so.FindProperty("m_AnchorMax").vector2Value = Vector2.one;
        so.FindProperty("m_AnchoredPosition").vector2Value = Vector2.zero;
        so.FindProperty("m_SizeDelta").vector2Value = Vector2.zero;
        so.FindProperty("m_LocalScale").vector3Value = Vector3.one;
        so.ApplyModifiedProperties();

        PrefabUtility.SaveAsPrefabAsset(bettingRoot, BETTING_PREFAB_PATH);
        PrefabUtility.UnloadPrefabContents(bettingRoot);
        Debug.Log("[CharacterStoryPopupFactory] BettingPanel 내 인스턴스 앵커 최종 보정 완료");
    }

    // ──────────────────────────────────────────────────────────────────────────
    //  계층 구조 생성
    // ──────────────────────────────────────────────────────────────────────────

    private static GameObject BuildHierarchy(
        out GameObject box, out Text storyText, out ScrollRect scrollRect, out Button closeBtn)
    {
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        Sprite whiteBoxSprite = AssetDatabase.LoadAssetAtPath<Sprite>(BG_SPRITE_PATH);

        // 루트 — full-stretch + Canvas(override, sortingOrder=2001)
        var root = new GameObject("CharacterStoryPopup");
        var rootRt = root.AddComponent<RectTransform>();

        var canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        // overrideSorting 직접 대입은 SaveAsPrefabAsset 후 리셋되는 quirk → SerializedObject 필수
        var canvasSo = new SerializedObject(canvas);
        canvasSo.FindProperty("m_OverrideSorting").boolValue = true;
        canvasSo.FindProperty("m_SortingOrder").intValue = 2001; // ItemInfoPopup(2000)과 구분
        canvasSo.ApplyModifiedProperties();
        root.AddComponent<GraphicRaycaster>();

        // AddComponent<Canvas>()가 RectTransform 앵커를 리셋하므로, 스트레치 설정은 Canvas 추가 이후에 적용.
        SetFullStretch(rootRt);

        // Box — 흰 배경, 화면 중앙 고정
        var boxImg = MkImage(root.transform, "Box",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, BOX_SIZE, Color.white);
        boxImg.sprite = whiteBoxSprite;
        boxImg.type = Image.Type.Sliced;
        box = boxImg.gameObject;

        // TitleLabel (선택적 헤더 — 상단)
        MkText(box.transform, "TitleLabel", "",
            new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(0, -16),
            new Vector2(-48, 48), 30, TextAnchor.MiddleCenter, TEXT_COLOR, font);

        // ScrollArea (뷰포트)
        var scrollAreaGo = new GameObject("ScrollArea");
        scrollAreaGo.transform.SetParent(box.transform, false);
        var scrollAreaRt = scrollAreaGo.AddComponent<RectTransform>();
        scrollAreaRt.anchorMin = Vector2.zero; scrollAreaRt.anchorMax = Vector2.one;
        scrollAreaRt.anchoredPosition = new Vector2(0, -30);
        scrollAreaRt.sizeDelta = new Vector2(-48, -110);
        scrollAreaGo.AddComponent<RectMask2D>();
        scrollRect = scrollAreaGo.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        scrollRect.viewport = scrollAreaRt;

        // Content
        var contentGo = new GameObject("Content");
        contentGo.transform.SetParent(scrollAreaGo.transform, false);
        var contentRt = contentGo.AddComponent<RectTransform>();
        contentRt.anchorMin = new Vector2(0, 1); contentRt.anchorMax = new Vector2(1, 1);
        contentRt.pivot = new Vector2(0.5f, 1);
        contentRt.anchoredPosition = Vector2.zero;
        contentRt.sizeDelta = new Vector2(0, 0);
        var fitter = contentGo.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        scrollRect.content = contentRt;

        // StoryText
        storyText = MkText(contentGo.transform, "StoryText", "",
            new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), Vector2.zero,
            new Vector2(0, 0), 26, TextAnchor.UpperLeft, TEXT_COLOR, font);
        storyText.horizontalOverflow = HorizontalWrapMode.Wrap;
        storyText.verticalOverflow = VerticalWrapMode.Overflow;
        storyText.lineSpacing = 1.25f;
        var storyFitter = storyText.gameObject.AddComponent<ContentSizeFitter>();
        storyFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // WaterDropDecor — 다른 자식(TitleLabel/ScrollArea)보다 먼저 부착해야 SetSiblingIndex(1)이
        // "배경 다음, 콘텐츠보다 아래"로 정확히 자리잡음. 순서 반대로 하면 안 됨.
        // (여기서는 이미 TitleLabel/ScrollArea가 생성된 뒤이므로, 아래에서 명시적으로 sibling 재조정)
        var decor = box.AddComponent<WaterDropDecor>();
        var decorSo = new SerializedObject(decor);
        decorSo.FindProperty("dropCount").intValue = 8;
        decorSo.FindProperty("sizeMin").floatValue = 5f;
        decorSo.FindProperty("sizeMax").floatValue = 12f;
        decorSo.ApplyModifiedProperties();
        // WaterDropDecor.Awake()는 에디터 컨텍스트에서 AddComponent 즉시 호출되어 컨테이너를 만들고
        // 스스로 SetSiblingIndex(1)을 실행함 — TitleLabel(0) 다음, ScrollArea/CloseBtn보다 앞(아래)에 위치.

        // CloseBtn — 재사용 헬퍼 사용
        closeBtn = CloseButtonFactory.Attach(box.transform, null);

        return root;
    }

    private static void AssignReferences(CharacterStoryPopup popup,
        GameObject box, Text storyText, ScrollRect scrollRect, Button closeBtn)
    {
        var so = new SerializedObject(popup);
        so.FindProperty("box").objectReferenceValue = box;
        so.FindProperty("storyText").objectReferenceValue = storyText;
        so.FindProperty("scrollRect").objectReferenceValue = scrollRect;
        so.FindProperty("closeBtn").objectReferenceValue = closeBtn;
        so.ApplyModifiedPropertiesWithoutUndo();

        // CloseBtn onClick은 popup 컴포넌트가 확정된 후 연결 (Attach 시점엔 popup 미생성 상태였음)
        closeBtn.onClick.RemoveAllListeners();
        closeBtn.onClick.AddListener(popup.Hide);
    }

    // ──────────────────────────────────────────────────────────────────────────
    //  UI 헬퍼 (NameEntryModalPrefabFactory와 동일 시그니처)
    // ──────────────────────────────────────────────────────────────────────────

    private static void SetFullStretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
    }

    private static Image MkImage(Transform parent, string name,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPos, Vector2 sizeDelta, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchoredPos; rt.sizeDelta = sizeDelta;
        var img = go.AddComponent<Image>();
        img.color = color;
        return img;
    }

    private static Text MkText(Transform parent, string name, string text,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 pos, Vector2 size,
        int fontSize, TextAnchor align, Color color, Font font)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
        rt.pivot = pivot;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        var t = go.AddComponent<Text>();
        t.font = font; t.text = text; t.fontSize = fontSize;
        t.alignment = align; t.color = color;
        t.supportRichText = true;
        return t;
    }
}
#endif
