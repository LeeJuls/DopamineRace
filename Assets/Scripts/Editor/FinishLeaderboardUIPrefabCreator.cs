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
///   ├─ TitleText        (str.finish.title — 금색, 55pt)
///   ├─ RoundDetailText  (700×400, 20pt, rich text)
///   ├─ TotalScoreText   (500×60, 38pt, 노랑)
///   ├─ NewGameBtn       (220×55, 파란)
///   │  └─ BtnText
///   └─ Top100Btn        (220×55, 보라)
///      └─ BtnText
///
/// 생성물:
///   Assets/Prefabs/UI/FinishPanel.prefab
/// </summary>
public static class FinishLeaderboardUIPrefabCreator
{
    private const string PREFAB_DIR        = "Assets/Prefabs/UI";
    private const string FINISH_PANEL_PATH = "Assets/Prefabs/UI/FinishPanel.prefab";

    // 색상
    private static readonly Color COLOR_BG_DARK    = new Color(0f,    0f,    0f,    0.85f);
    private static readonly Color COLOR_GOLD       = new Color(1f,    0.85f, 0.2f,  1f);
    private static readonly Color COLOR_YELLOW     = Color.yellow;
    private static readonly Color COLOR_WHITE      = Color.white;
    private static readonly Color COLOR_BTN_BLUE   = new Color(0.25f, 0.5f,  0.9f,  1f);
    private static readonly Color COLOR_BTN_PURPLE = new Color(0.5f,  0.3f,  0.6f,  1f);

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

        // ── RoundDetailText (anchor y=0.58, 흰색 20pt, rich text) ──
        GameObject detailGo = MkText(root.transform, "RoundDetailText", "",
            new Vector2(0.5f, 0.58f), new Vector2(0.5f, 0.58f),
            Vector2.zero, new Vector2(700f, 400f),
            20, TextAnchor.UpperCenter, COLOR_WHITE, font, true);
        detailGo.GetComponent<Text>().verticalOverflow = VerticalWrapMode.Overflow;

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
