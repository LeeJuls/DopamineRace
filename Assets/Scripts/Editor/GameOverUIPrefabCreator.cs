#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using System.IO;

/// <summary>
/// 게임오버 화면 UI 프리팹 자동 생성 Editor 스크립트 (SPEC-029).
/// Unity 메뉴: DopamineRace > Create GameOver UI Prefabs
///
/// 생성물:
///   Assets/Prefabs/UI/GameOverPanel.prefab
///
/// 구조:
///   GameOverPanel (full-rect, 어두운 백드롭)
///   └─ Panel (화면 중앙 모달 박스)
///      └─ HeadlineText ("GAME OVER", 폰트 42)
/// </summary>
public static class GameOverUIPrefabCreator
{
    private const string PREFAB_DIR  = "Assets/Prefabs/UI";
    private const string PANEL_PATH  = "Assets/Prefabs/UI/GameOverPanel.prefab";

    private static readonly Color COLOR_BACKDROP = new Color(0f, 0f, 0f, 0.9f);
    private static readonly Color COLOR_PANEL    = new Color(0.12f, 0.04f, 0.04f, 0.96f);
    private static readonly Color COLOR_HEADLINE = new Color(1f, 0.35f, 0.35f, 1f);
    private static readonly Color COLOR_HINT     = new Color(0.85f, 0.85f, 0.85f, 0.7f);

    // ══════════════════════════════════════════════
    //  메뉴: Create (완전 재생성)
    // ══════════════════════════════════════════════
    [MenuItem("DopamineRace/프리팹 생성/GameOver UI Prefabs")]
    public static void CreatePrefabs()
    {
        bool ok = EditorUtility.DisplayDialog(
            "⚠️ GameOver 프리팹 재생성 확인",
            "기존 GameOverPanel.prefab을 완전히 삭제하고 새로 만듭니다.\n\n" +
            "🚨 기존 수동 수정 내용이 모두 사라집니다!\n\n" +
            "정말로 진행할까요?",
            "Yes, 완전 재생성",
            "No, 취소"
        );
        if (!ok)
        {
            Debug.Log("[GameOverUIPrefabCreator] 재생성 취소됨.");
            return;
        }

        EnsureDirectory(PREFAB_DIR);

        var gs = AssetDatabase.LoadAssetAtPath<GameSettings>("Assets/Resources/GameSettings.asset");
        Font font = gs != null ? gs.mainFont : null;

        GameObject panel = CreateGameOverPanelPrefab(font);
        PrefabUtility.SaveAsPrefabAsset(panel, PANEL_PATH);
        Object.DestroyImmediate(panel);
        Debug.Log("[GameOverUIPrefabCreator] 생성: " + PANEL_PATH);

        if (gs != null)
        {
            gs.gameOverPanelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PANEL_PATH);
            EditorUtility.SetDirty(gs);
            AssetDatabase.SaveAssets();
            Debug.Log("[GameOverUIPrefabCreator] GameSettings.gameOverPanelPrefab 자동 연결 완료!");
        }
        else
        {
            Debug.LogWarning("[GameOverUIPrefabCreator] GameSettings.asset을 찾을 수 없습니다. 수동 연결 필요.");
        }

        AssetDatabase.Refresh();
    }

    // ══════════════════════════════════════════════
    //  메뉴: Patch (안전 패치)
    // ══════════════════════════════════════════════
    [MenuItem("DopamineRace/Patch GameOver UI Prefabs (Safe)")]
    public static void PatchPrefabs()
    {
        if (!File.Exists(PANEL_PATH))
        {
            Debug.LogWarning("[GameOverUIPrefabCreator] GameOverPanel.prefab 없음 → Create 실행.");
            CreatePrefabs();
            return;
        }

        var gs = AssetDatabase.LoadAssetAtPath<GameSettings>("Assets/Resources/GameSettings.asset");
        Font font = gs != null ? gs.mainFont : null;

        using (var scope = new PrefabUtility.EditPrefabContentsScope(PANEL_PATH))
        {
            Transform root = scope.prefabContentsRoot.transform;

            // SPEC-029.1: 루트에 전체화면 Button (클릭 → 타이틀 이동) 없으면 추가
            if (scope.prefabContentsRoot.GetComponent<Button>() == null)
            {
                Debug.Log("[GameOverUIPrefabCreator][Patch] 루트 Button 없음 → 추가 (클릭 이동)");
                scope.prefabContentsRoot.AddComponent<Button>();
            }

            Transform panel = root.Find("Panel");
            if (panel == null)
            {
                Debug.Log("[GameOverUIPrefabCreator][Patch] Panel 없음 → 추가");
                CreateCenterPanel(root, font);
            }
            else
            {
                if (panel.Find("HeadlineText") == null)
                {
                    Debug.Log("[GameOverUIPrefabCreator][Patch] HeadlineText 없음 → 추가");
                    MkHeadline(panel, font);
                }
                // SPEC-029.1: 클릭 안내 HintText 없으면 추가
                if (panel.Find("HintText") == null)
                {
                    Debug.Log("[GameOverUIPrefabCreator][Patch] HintText 없음 → 추가");
                    MkHint(panel, font);
                }
            }
        }

        if (gs != null && gs.gameOverPanelPrefab == null)
        {
            gs.gameOverPanelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PANEL_PATH);
            EditorUtility.SetDirty(gs);
            AssetDatabase.SaveAssets();
        }

        Debug.Log("[GameOverUIPrefabCreator] Patch 완료: " + PANEL_PATH);
    }

    // ══════════════════════════════════════════════
    //  GameOverPanel 프리팹 생성
    // ══════════════════════════════════════════════
    private static GameObject CreateGameOverPanelPrefab(Font font)
    {
        // 루트 = 전체 화면 어두운 백드롭
        GameObject root = new GameObject("GameOverPanel");
        RectTransform rootRt = root.AddComponent<RectTransform>();
        rootRt.anchorMin = Vector2.zero;
        rootRt.anchorMax = Vector2.one;
        rootRt.offsetMin = Vector2.zero;
        rootRt.offsetMax = Vector2.zero;
        Image bg = root.AddComponent<Image>();
        bg.color = COLOR_BACKDROP;

        // SPEC-029.1: 전체화면 클릭 타깃 (클릭 시 타이틀 이동, 런타임에서 onClick 연결)
        root.AddComponent<Button>();

        // 중앙 모달 박스
        CreateCenterPanel(root.transform, font);

        return root;
    }

    private static void CreateCenterPanel(Transform parent, Font font)
    {
        GameObject panel = new GameObject("Panel");
        panel.transform.SetParent(parent, false);
        RectTransform rt = panel.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(760, 360);
        Image img = panel.AddComponent<Image>();
        img.color = COLOR_PANEL;

        MkHeadline(panel.transform, font);
        MkHint(panel.transform, font);
    }

    private static void MkHeadline(Transform panel, Font font)
    {
        GameObject go = new GameObject("HeadlineText");
        go.transform.SetParent(panel, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        Text t = go.AddComponent<Text>();
        t.text = "GAME OVER";
        t.fontSize = 42;
        t.alignment = TextAnchor.MiddleCenter;
        t.color = COLOR_HEADLINE;
        t.supportRichText = false;
        if (font != null) t.font = font;
    }

    // SPEC-029.1: 클릭 안내 서브텍스트 (패널 하단)
    private static void MkHint(Transform panel, Font font)
    {
        GameObject go = new GameObject("HintText");
        go.transform.SetParent(panel, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(1f, 0f);
        rt.pivot     = new Vector2(0.5f, 0f);
        rt.anchoredPosition = new Vector2(0f, 28f);
        rt.sizeDelta = new Vector2(0f, 40f);

        Text t = go.AddComponent<Text>();
        t.text = "화면을 클릭하면 타이틀로 돌아갑니다";
        t.fontSize = 20;
        t.alignment = TextAnchor.MiddleCenter;
        t.color = COLOR_HINT;
        t.supportRichText = false;
        if (font != null) t.font = font;
    }

    // ══════════════════════════════════════════════
    //  헬퍼
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
