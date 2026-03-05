#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using System.IO;

/// <summary>
/// 최종 결산(Finish) 화면 UI 프리팹 자동 생성 Editor 스크립트.
/// Unity 메뉴: DopamineRace > Create Finish UI Prefab
///
/// 구조:
///   FinishPanel (880×650, 반투명 어두운 배경)
///   ├─ TitleText          (str.finish.title — 금색, 55pt)
///   ├─ RoundScrollView    (700×300, ScrollRect — 라운드 목록 스크롤)
///   │  └─ Viewport        (RectMask2D, fill)
///   │     └─ RoundDetailText (Text + ContentSizeFitter, 20pt, rich text)
///   ├─ TotalScoreText     (500×60, 38pt, 노랑)
///   ├─ NewGameBtn         (220×55, 파란)
///   │  └─ BtnText
///   └─ Top100Btn          (220×55, 보라)
///      └─ BtnText
///
/// 생성물:
///   Assets/Prefabs/UI/FinishPanel.prefab
/// </summary>
public static class FinishLeaderboardUIPrefabCreator
{
    private const string PREFAB_DIR             = "Assets/Prefabs/UI";
    private const string FINISH_PANEL_PATH      = "Assets/Prefabs/UI/FinishPanel.prefab";
    private const string LEADERBOARD_PANEL_PATH = "Assets/Prefabs/UI/LeaderboardPanel.prefab";

    // 색상
    private static readonly Color COLOR_BG_DARK    = new Color(0f,    0f,    0f,    0.85f);
    private static readonly Color COLOR_BG_DARKER  = new Color(0f,    0f,    0f,    0.90f);
    private static readonly Color COLOR_GOLD       = new Color(1f,    0.85f, 0.2f,  1f);
    private static readonly Color COLOR_YELLOW     = Color.yellow;
    private static readonly Color COLOR_WHITE      = Color.white;
    private static readonly Color COLOR_LIGHT_GRAY = new Color(0.8f,  0.8f,  0.8f,  1f);
    private static readonly Color COLOR_BTN_BLUE   = new Color(0.25f, 0.5f,  0.9f,  1f);
    private static readonly Color COLOR_BTN_PURPLE = new Color(0.5f,  0.3f,  0.6f,  1f);
    private static readonly Color COLOR_BTN_RED    = new Color(0.5f,  0.3f,  0.3f,  1f);

    // ══════════════════════════════════════════════
    //  메뉴 항목
    // ══════════════════════════════════════════════
    [MenuItem("DopamineRace/Create Finish UI Prefab")]
    public static void CreateFinishPrefab()
    {
        bool ok = EditorUtility.DisplayDialog(
            "FinishPanel 프리팹 생성",
            "FinishPanel.prefab을 새로 생성합니다.\n" +
            "이미 존재하는 경우 덮어씁니다.\n\n진행할까요?",
            "Yes, 생성",
            "No, 취소"
        );
        if (!ok) { Debug.Log("[FinishLeaderboardUIPrefabCreator] 취소됨."); return; }

        EnsureDirectory(PREFAB_DIR);

        var gs   = AssetDatabase.LoadAssetAtPath<GameSettings>("Assets/Resources/GameSettings.asset");
        Font font = gs != null ? gs.mainFont : null;

        GameObject panel = CreateFinishPanelPrefab(font);
        PrefabUtility.SaveAsPrefabAsset(panel, FINISH_PANEL_PATH);
        UnityEngine.Object.DestroyImmediate(panel);
        Debug.Log("[FinishLeaderboardUIPrefabCreator] 생성 완료: " + FINISH_PANEL_PATH);

        // GameSettings 자동 연결
        if (gs != null)
        {
            gs.finishPanelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(FINISH_PANEL_PATH);
            EditorUtility.SetDirty(gs);
            AssetDatabase.SaveAssets();
            Debug.Log("[FinishLeaderboardUIPrefabCreator] GameSettings.finishPanelPrefab 자동 연결 완료!");
        }
        else
        {
            Debug.LogWarning("[FinishLeaderboardUIPrefabCreator] GameSettings.asset을 찾을 수 없습니다. 수동으로 연결해주세요.");
        }

        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("완료", "FinishPanel.prefab 생성 완료!", "OK");
    }

    // ══════════════════════════════════════════════
    //  FinishPanel 프리팹 구조 생성
    // ══════════════════════════════════════════════
    internal static GameObject CreateFinishPanelPrefab(Font font)
    {
        // ── 루트 패널 (880×650, 중앙) ──
        GameObject root = new GameObject("FinishPanel");
        RectTransform rootRt = root.AddComponent<RectTransform>();
        rootRt.anchorMin        = new Vector2(0.5f, 0.5f);
        rootRt.anchorMax        = new Vector2(0.5f, 0.5f);
        rootRt.pivot            = new Vector2(0.5f, 0.5f);
        rootRt.anchoredPosition = Vector2.zero;
        rootRt.sizeDelta        = new Vector2(880f, 650f);
        root.AddComponent<Image>().color = COLOR_BG_DARK;

        // ── TitleText (anchor y=0.92, 금색 55pt) ──
        MkText(root.transform, "TitleText", "Finish!!",
            new Vector2(0.5f, 0.92f), new Vector2(0.5f, 0.92f),
            Vector2.zero, new Vector2(500f, 70f),
            55, TextAnchor.MiddleCenter, COLOR_GOLD, font, false);

        // ── RoundScrollView (anchor y=0.57, 700×300, ScrollRect) ──
        GameObject scrollView = new GameObject("RoundScrollView");
        scrollView.transform.SetParent(root.transform, false);
        RectTransform scrollRt = scrollView.AddComponent<RectTransform>();
        scrollRt.anchorMin        = new Vector2(0.5f, 0.57f);
        scrollRt.anchorMax        = new Vector2(0.5f, 0.57f);
        scrollRt.pivot            = new Vector2(0.5f, 0.5f);
        scrollRt.anchoredPosition = Vector2.zero;
        scrollRt.sizeDelta        = new Vector2(700f, 300f);
        // ScrollRect 설정 (수직 스크롤만)
        ScrollRect sr = scrollView.AddComponent<ScrollRect>();
        sr.horizontal     = false;
        sr.vertical       = true;
        sr.movementType   = ScrollRect.MovementType.Clamped;
        sr.scrollSensitivity = 30f;
        // ScrollRect에 투명 Image (레이캐스트 블로킹용)
        Image srImg = scrollView.AddComponent<Image>();
        srImg.color = new Color(0f, 0f, 0f, 0f);

        // ── Viewport (RectMask2D, fill) ──
        GameObject viewport = new GameObject("Viewport");
        viewport.transform.SetParent(scrollView.transform, false);
        RectTransform vpRt = viewport.AddComponent<RectTransform>();
        vpRt.anchorMin        = Vector2.zero;
        vpRt.anchorMax        = Vector2.one;
        vpRt.pivot            = new Vector2(0f, 1f);
        vpRt.anchoredPosition = Vector2.zero;
        vpRt.sizeDelta        = Vector2.zero;
        viewport.AddComponent<RectMask2D>();

        // ── RoundDetailText (top-stretch, ContentSizeFitter) ──
        GameObject detailGo = new GameObject("RoundDetailText");
        detailGo.transform.SetParent(viewport.transform, false);
        RectTransform detailRt = detailGo.AddComponent<RectTransform>();
        detailRt.anchorMin        = new Vector2(0f, 1f);
        detailRt.anchorMax        = new Vector2(1f, 1f);
        detailRt.pivot            = new Vector2(0.5f, 1f);
        detailRt.anchoredPosition = Vector2.zero;
        detailRt.sizeDelta        = new Vector2(0f, 300f); // 초기값, CSF가 자동 조정
        Text detailText = detailGo.AddComponent<Text>();
        detailText.text           = "";
        detailText.fontSize       = 20;
        detailText.alignment      = TextAnchor.UpperLeft;
        detailText.color          = COLOR_WHITE;
        detailText.supportRichText = true;
        detailText.verticalOverflow = VerticalWrapMode.Overflow;
        if (font != null) detailText.font = font;
        // ContentSizeFitter — 텍스트 내용에 맞춰 높이 자동 확장
        ContentSizeFitter csf = detailGo.AddComponent<ContentSizeFitter>();
        csf.verticalFit   = ContentSizeFitter.FitMode.PreferredSize;
        csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

        // ScrollRect 연결
        sr.viewport = vpRt;
        sr.content  = detailRt;

        // ── TotalScoreText (anchor y=0.18, 노랑 38pt) ──
        MkText(root.transform, "TotalScoreText", "",
            new Vector2(0.5f, 0.18f), new Vector2(0.5f, 0.18f),
            Vector2.zero, new Vector2(500f, 60f),
            38, TextAnchor.MiddleCenter, COLOR_YELLOW, font, false);

        // ── NewGameBtn (좌: anchor x=0.3, y=0.05) ──
        CreateButton(root.transform, "NewGameBtn", "새 게임",
            new Vector2(0.3f, 0.05f), new Vector2(220f, 55f),
            COLOR_BTN_BLUE, 26, font);

        // ── Top100Btn (우: anchor x=0.7, y=0.05) ──
        CreateButton(root.transform, "Top100Btn", "Top 100",
            new Vector2(0.7f, 0.05f), new Vector2(220f, 55f),
            COLOR_BTN_PURPLE, 26, font);

        return root;
    }

    // ══════════════════════════════════════════════
    //  헬퍼: 버튼 생성
    // ══════════════════════════════════════════════
    private static void CreateButton(Transform parent, string name, string label,
        Vector2 anchor, Vector2 size, Color bgColor, int fontSize, Font font)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin        = anchor;
        rt.anchorMax        = anchor;
        rt.pivot            = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta        = size;
        go.AddComponent<Image>().color = bgColor;
        go.AddComponent<Button>();

        // BtnText 자식
        MkText(go.transform, "BtnText", label,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, size,
            fontSize, TextAnchor.MiddleCenter, Color.white, font, false);
    }

    // ══════════════════════════════════════════════
    //  헬퍼: Text GameObject 생성 (앵커 기반)
    // ══════════════════════════════════════════════
    private static GameObject MkText(Transform parent, string name, string text,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPos, Vector2 sizeDelta,
        int fontSize, TextAnchor alignment, Color color, Font font, bool richText)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin        = anchorMin;
        rt.anchorMax        = anchorMax;
        rt.pivot            = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta        = sizeDelta;

        Text t = go.AddComponent<Text>();
        t.text          = text;
        t.fontSize      = fontSize;
        t.alignment     = alignment;
        t.color         = color;
        t.supportRichText = richText;
        if (font != null) t.font = font;
        return go;
    }

    // ══════════════════════════════════════════════
    //  메뉴 항목 — LeaderboardPanel
    // ══════════════════════════════════════════════
    [MenuItem("DopamineRace/Create Leaderboard UI Prefab")]
    public static void CreateLeaderboardPrefab()
    {
        bool ok = EditorUtility.DisplayDialog(
            "LeaderboardPanel 프리팹 생성",
            "LeaderboardPanel.prefab을 새로 생성합니다.\n" +
            "이미 존재하는 경우 덮어씁니다.\n\n진행할까요?",
            "Yes, 생성",
            "No, 취소"
        );
        if (!ok) { Debug.Log("[FinishLeaderboardUIPrefabCreator] 취소됨."); return; }

        EnsureDirectory(PREFAB_DIR);

        var gs   = AssetDatabase.LoadAssetAtPath<GameSettings>("Assets/Resources/GameSettings.asset");
        Font font = gs != null ? gs.mainFont : null;

        GameObject panel = CreateLeaderboardPanelPrefab(font);
        PrefabUtility.SaveAsPrefabAsset(panel, LEADERBOARD_PANEL_PATH);
        UnityEngine.Object.DestroyImmediate(panel);
        Debug.Log("[FinishLeaderboardUIPrefabCreator] 생성 완료: " + LEADERBOARD_PANEL_PATH);

        // GameSettings 자동 연결
        if (gs != null)
        {
            gs.leaderboardPanelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(LEADERBOARD_PANEL_PATH);
            EditorUtility.SetDirty(gs);
            AssetDatabase.SaveAssets();
            Debug.Log("[FinishLeaderboardUIPrefabCreator] GameSettings.leaderboardPanelPrefab 자동 연결 완료!");
        }
        else
        {
            Debug.LogWarning("[FinishLeaderboardUIPrefabCreator] GameSettings.asset을 찾을 수 없습니다. 수동으로 연결해주세요.");
        }

        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("완료", "LeaderboardPanel.prefab 생성 완료!", "OK");
    }

    // ══════════════════════════════════════════════
    //  LeaderboardPanel 프리팹 구조 생성
    //
    //  구조:
    //    LeaderboardPanel (880×800)
    //    ├─ TitleText      (36pt, 금색)
    //    ├─ HeaderText     (26pt, 연회색)
    //    ├─ ContentScrollView (ScrollRect)
    //    │  └─ Viewport (RectMask2D)
    //    │     └─ EntryContainer (VerticalLayoutGroup + CSF)
    //    │        └─ EntryTemplate [active=false — 클론 소스]
    //    │           ├─ InfoText    (28pt, 흰색)
    //    │           └─ SummaryText (22pt, 회색)
    //    └─ CloseBtn
    // ══════════════════════════════════════════════
    internal static GameObject CreateLeaderboardPanelPrefab(Font font)
    {
        // ── 루트 패널 (880×800, 중앙) ──
        GameObject root = new GameObject("LeaderboardPanel");
        RectTransform rootRt = root.AddComponent<RectTransform>();
        rootRt.anchorMin        = new Vector2(0.5f, 0.5f);
        rootRt.anchorMax        = new Vector2(0.5f, 0.5f);
        rootRt.pivot            = new Vector2(0.5f, 0.5f);
        rootRt.anchoredPosition = Vector2.zero;
        rootRt.sizeDelta        = new Vector2(880f, 800f);
        root.AddComponent<Image>().color = COLOR_BG_DARKER;

        // ── TitleText (anchor y=0.94, 금색 36pt) ──
        MkText(root.transform, "TitleText", "Top 100 Leaderboard",
            new Vector2(0.5f, 0.94f), new Vector2(0.5f, 0.94f),
            Vector2.zero, new Vector2(500f, 55f),
            36, TextAnchor.MiddleCenter, COLOR_GOLD, font, false);

        // ── HeaderText (anchor y=0.87, 연회색 26pt) ──
        MkText(root.transform, "HeaderText", "등수  포인트  달성일",
            new Vector2(0.5f, 0.87f), new Vector2(0.5f, 0.87f),
            Vector2.zero, new Vector2(800f, 36f),
            26, TextAnchor.MiddleLeft, COLOR_LIGHT_GRAY, font, false);

        // ── ContentScrollView (anchor y=0.49, 800×590, ScrollRect) ──
        GameObject scrollView = new GameObject("ContentScrollView");
        scrollView.transform.SetParent(root.transform, false);
        RectTransform scrollRt = scrollView.AddComponent<RectTransform>();
        scrollRt.anchorMin        = new Vector2(0.5f, 0.49f);
        scrollRt.anchorMax        = new Vector2(0.5f, 0.49f);
        scrollRt.pivot            = new Vector2(0.5f, 0.5f);
        scrollRt.anchoredPosition = Vector2.zero;
        scrollRt.sizeDelta        = new Vector2(800f, 590f);
        ScrollRect sr = scrollView.AddComponent<ScrollRect>();
        sr.horizontal        = false;
        sr.vertical          = true;
        sr.movementType      = ScrollRect.MovementType.Clamped;
        sr.scrollSensitivity = 30f;
        scrollView.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0f);

        // ── Viewport (RectMask2D, fill) ──
        GameObject viewport = new GameObject("Viewport");
        viewport.transform.SetParent(scrollView.transform, false);
        RectTransform vpRt = viewport.AddComponent<RectTransform>();
        vpRt.anchorMin        = Vector2.zero;
        vpRt.anchorMax        = Vector2.one;
        vpRt.pivot            = new Vector2(0f, 1f);
        vpRt.anchoredPosition = Vector2.zero;
        vpRt.sizeDelta        = Vector2.zero;
        viewport.AddComponent<RectMask2D>();

        // ── EntryContainer (VerticalLayoutGroup + CSF, top-stretch) ──
        GameObject container = new GameObject("EntryContainer");
        container.transform.SetParent(viewport.transform, false);
        RectTransform containerRt = container.AddComponent<RectTransform>();
        containerRt.anchorMin        = new Vector2(0f, 1f);
        containerRt.anchorMax        = new Vector2(1f, 1f);
        containerRt.pivot            = new Vector2(0.5f, 1f);
        containerRt.anchoredPosition = Vector2.zero;
        containerRt.sizeDelta        = Vector2.zero;
        VerticalLayoutGroup containerVlg = container.AddComponent<VerticalLayoutGroup>();
        containerVlg.spacing             = 10f;
        containerVlg.childForceExpandWidth  = true;
        containerVlg.childForceExpandHeight = false;
        containerVlg.childAlignment         = TextAnchor.UpperLeft;
        containerVlg.padding = new RectOffset(8, 8, 6, 6);
        ContentSizeFitter containerCSF = container.AddComponent<ContentSizeFitter>();
        containerCSF.verticalFit   = ContentSizeFitter.FitMode.PreferredSize;
        containerCSF.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

        // ── EntryTemplate [active=false — 런타임에 Instantiate해서 사용] ──
        GameObject template = new GameObject("EntryTemplate");
        template.transform.SetParent(container.transform, false);
        RectTransform templateRt = template.AddComponent<RectTransform>();
        templateRt.sizeDelta = new Vector2(0f, 60f);
        VerticalLayoutGroup entryVlg = template.AddComponent<VerticalLayoutGroup>();
        entryVlg.spacing             = 3f;
        entryVlg.childForceExpandWidth  = true;
        entryVlg.childForceExpandHeight = false;
        entryVlg.childAlignment         = TextAnchor.UpperLeft;
        ContentSizeFitter entryCSF = template.AddComponent<ContentSizeFitter>();
        entryCSF.verticalFit   = ContentSizeFitter.FitMode.PreferredSize;
        entryCSF.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

        // InfoText (등수·점수·날짜 한 줄, 28pt 흰색)
        GameObject infoGo = new GameObject("InfoText");
        infoGo.transform.SetParent(template.transform, false);
        RectTransform infoRt = infoGo.AddComponent<RectTransform>();
        infoRt.sizeDelta = new Vector2(0f, 34f);
        Text infoText = infoGo.AddComponent<Text>();
        infoText.text              = "1위  0점  26-01-01";
        infoText.fontSize          = 28;
        infoText.alignment         = TextAnchor.UpperLeft;
        infoText.color             = COLOR_WHITE;
        infoText.horizontalOverflow = HorizontalWrapMode.Overflow;
        infoText.verticalOverflow   = VerticalWrapMode.Overflow;
        infoText.supportRichText   = false;
        if (font != null) infoText.font = font;
        ContentSizeFitter infoCSF = infoGo.AddComponent<ContentSizeFitter>();
        infoCSF.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // SummaryText (요약 줄, 22pt 연회색, 줄 넘김 가능)
        GameObject summaryGo = new GameObject("SummaryText");
        summaryGo.transform.SetParent(template.transform, false);
        RectTransform summaryRt = summaryGo.AddComponent<RectTransform>();
        summaryRt.sizeDelta = new Vector2(0f, 27f);
        Text summaryText = summaryGo.AddComponent<Text>();
        summaryText.text              = "R1:Win+0";
        summaryText.fontSize          = 22;
        summaryText.alignment         = TextAnchor.UpperLeft;
        summaryText.color             = new Color(0.75f, 0.75f, 0.75f, 1f);
        summaryText.horizontalOverflow = HorizontalWrapMode.Wrap;
        summaryText.verticalOverflow   = VerticalWrapMode.Overflow;
        summaryText.supportRichText   = false;
        if (font != null) summaryText.font = font;
        ContentSizeFitter summaryCSF = summaryGo.AddComponent<ContentSizeFitter>();
        summaryCSF.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // EntryTemplate 비활성화 (클론 소스이므로 숨김)
        template.SetActive(false);

        // ScrollRect 연결
        sr.viewport = vpRt;
        sr.content  = containerRt;

        // ── CloseBtn (anchor y=0.04, 어두운 빨강, 28pt) ──
        CreateButton(root.transform, "CloseBtn", "닫기",
            new Vector2(0.5f, 0.04f), new Vector2(200f, 55f),
            COLOR_BTN_RED, 28, font);

        return root;
    }

    // ══════════════════════════════════════════════
    //  헬퍼: 디렉터리 보장
    // ══════════════════════════════════════════════
    private static void EnsureDirectory(string path)
    {
        if (!AssetDatabase.IsValidFolder(path))
        {
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            string folder = Path.GetFileName(path);
            AssetDatabase.CreateFolder(parent, folder);
        }
    }
}
#endif
