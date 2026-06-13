#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// NameEntryModal 프리팹 생성 + SceneBootstrapper 자동 할당.
/// 메뉴: DopamineRace → Create NameEntry Modal Prefab
/// 생성 위치: Assets/Prefabs/UI/NameEntryModal.prefab
/// </summary>
public static class NameEntryModalPrefabFactory
{
    private const string PREFAB_PATH = "Assets/Prefabs/UI/NameEntryModal.prefab";

    [MenuItem("DopamineRace/Create NameEntry Modal Prefab")]
    public static void CreatePrefab()
    {
        // 1. 계층 구조 생성
        var root = BuildHierarchy(
            out var backdrop,
            out var titleText, out var scoreText, out var rankResultText, out var guideText, out var continueText,
            out var slots, out var highlights, out var ups, out var downs, out var confirmBtn);

        // 2. NameEntryModal 컴포넌트 추가 + SerializedObject로 참조 할당
        var modal = root.AddComponent<NameEntryModal>();
        AssignReferences(modal, backdrop, titleText, scoreText, guideText, rankResultText, continueText,
            slots, highlights, ups, downs, confirmBtn);

        // 3. 프리팹으로 저장 (기존 덮어쓰기)
        var savedPrefab = PrefabUtility.SaveAsPrefabAsset(root, PREFAB_PATH);
        Object.DestroyImmediate(root);

        // 4. SceneBootstrapper에 자동 할당
        var sb = Object.FindFirstObjectByType<SceneBootstrapper>();
        if (sb != null)
        {
            var so = new SerializedObject(sb);
            var prop = so.FindProperty("_nameEntryModalPrefab");
            if (prop != null)
            {
                prop.objectReferenceValue = savedPrefab.GetComponent<NameEntryModal>();
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(sb);
                EditorSceneManager.MarkSceneDirty(sb.gameObject.scene);
                Debug.Log("[NameEntryFactory] SceneBootstrapper._nameEntryModalPrefab 자동 할당 완료");
            }
            else
            {
                Debug.LogWarning("[NameEntryFactory] _nameEntryModalPrefab 프로퍼티를 찾지 못했습니다 — Inspector에서 수동 할당 필요");
            }
        }
        else
        {
            Debug.LogWarning("[NameEntryFactory] SceneBootstrapper를 씬에서 찾지 못했습니다 — SampleScene 열고 다시 실행하세요");
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[NameEntryFactory] ★ 프리팹 생성 완료 → {PREFAB_PATH}");
    }

    // ──────────────────────────────────────────────────────────────────────────
    //  계층 구조 생성
    // ──────────────────────────────────────────────────────────────────────────

    private static GameObject BuildHierarchy(
        out GameObject backdrop,
        out Text titleText, out Text scoreText, out Text rankResultText, out Text guideText, out Text continueText,
        out Text[] slots, out Image[] highlights, out Button[] ups, out Button[] downs, out Button confirmBtn)
    {
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        // 루트 — full-stretch RectTransform
        var root = new GameObject("NameEntryModal");
        SetFullStretch(root.AddComponent<RectTransform>());

        // 백드롭
        var backdropImg = MkImage(root.transform, "Backdrop",
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(0f, 0f, 0f, 0.7f));
        backdropImg.raycastTarget = true;
        backdrop = backdropImg.gameObject;

        // 패널
        var panelImg = MkImage(root.transform, "Panel",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(560, 440),
            new Color(0.12f, 0.10f, 0.18f, 0.95f));
        var panel = panelImg.transform;

        // 텍스트 요소
        titleText = MkText(panel, "TitleText", "NEW RECORD!",
            new Vector2(0.5f, 0.88f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(520, 60),
            40, TextAnchor.MiddleCenter, new Color(1f, 0.85f, 0.4f), font);

        scoreText = MkText(panel, "ScoreText", "",
            new Vector2(0.5f, 0.73f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(520, 44),
            28, TextAnchor.MiddleCenter, Color.white, font);

        rankResultText = MkText(panel, "RankResultText", "",
            new Vector2(0.5f, 0.625f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(520, 40),
            18, TextAnchor.MiddleCenter, new Color(1f, 0.85f, 0.4f), font);

        // GuideText: 버튼 위로 올려 겹침 해소 (입력 중 키보드 안내)
        guideText = MkText(panel, "GuideText", "",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -120), new Vector2(520, 36),
            20, TextAnchor.MiddleCenter, new Color(0.8f, 0.8f, 0.8f), font);

        // ContinueText: 결과 팝업 전용 "터치하여 계속" (확인 버튼 자리, 기본 비활성)
        continueText = MkText(panel, "ContinueText", "",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -175), new Vector2(520, 48),
            20, TextAnchor.MiddleCenter, new Color(0.8f, 0.8f, 0.8f), font);
        continueText.gameObject.SetActive(false);

        // 3슬롯 (강조 Image + 글자 Text + ▲▼ 버튼)
        slots      = new Text[3];
        highlights = new Image[3];
        ups        = new Button[3];
        downs      = new Button[3];
        float[] xs = { -130f, 0f, 130f };

        for (int i = 0; i < 3; i++)
        {
            highlights[i] = MkImageCentered(panel, "SlotHL" + i,
                new Vector2(0.5f, 0.47f), new Vector2(xs[i], 0), new Vector2(86, 108),
                new Color(1f, 0.85f, 0.4f, 0.45f));

            slots[i] = MkText(panel, "SlotText" + i, "A",
                new Vector2(0.5f, 0.47f), new Vector2(0.5f, 0.5f), new Vector2(xs[i], 0), new Vector2(78, 96),
                66, TextAnchor.MiddleCenter, Color.white, font);

            ups[i]   = MkBtn(panel, "UpBtn"   + i, "▲", new Vector2(xs[i],  78), new Vector2(64, 40), font);
            downs[i] = MkBtn(panel, "DownBtn" + i, "▼", new Vector2(xs[i], -78), new Vector2(64, 40), font);
        }

        // 확인 버튼
        confirmBtn = MkBtn(panel, "ConfirmBtn", "확인", new Vector2(0, -180), new Vector2(200, 52), font);
        if (confirmBtn.image != null) confirmBtn.image.color = new Color(0.3f, 0.6f, 0.3f);

        return root;
    }

    // ──────────────────────────────────────────────────────────────────────────
    //  SerializedObject로 참조 할당 (직접 필드 접근 없이 직렬화 경로 사용)
    // ──────────────────────────────────────────────────────────────────────────

    private static void AssignReferences(NameEntryModal modal,
        GameObject backdrop, Text titleText, Text scoreText, Text guideText, Text rankResultText, Text continueText,
        Text[] slots, Image[] highlights, Button[] ups, Button[] downs, Button confirmBtn)
    {
        var so = new SerializedObject(modal);
        so.FindProperty("_backdrop").objectReferenceValue       = backdrop;
        so.FindProperty("_titleText").objectReferenceValue      = titleText;
        so.FindProperty("_scoreText").objectReferenceValue      = scoreText;
        so.FindProperty("_guideText").objectReferenceValue      = guideText;
        so.FindProperty("_rankResultText").objectReferenceValue = rankResultText;
        so.FindProperty("_confirmButton").objectReferenceValue  = confirmBtn;
        so.FindProperty("_continueText").objectReferenceValue   = continueText;
        SetSerializedArray(so.FindProperty("_slotTexts"),      slots);
        SetSerializedArray(so.FindProperty("_slotHighlights"), highlights);
        SetSerializedArray(so.FindProperty("_upButtons"),      ups);
        SetSerializedArray(so.FindProperty("_downButtons"),    downs);
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetSerializedArray<T>(SerializedProperty prop, T[] arr) where T : Object
    {
        prop.arraySize = arr.Length;
        for (int i = 0; i < arr.Length; i++)
            prop.GetArrayElementAtIndex(i).objectReferenceValue = arr[i];
    }

    // ──────────────────────────────────────────────────────────────────────────
    //  UI 헬퍼
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
        rt.anchoredPosition = anchoredPos; rt.sizeDelta = sizeDelta;
        var img = go.AddComponent<Image>();
        img.color = color;
        return img;
    }

    private static Image MkImageCentered(Transform parent, string name,
        Vector2 anchor, Vector2 pos, Vector2 size, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = anchor;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        var img = go.AddComponent<Image>();
        img.color = color;
        return img;
    }

    private static Text MkText(Transform parent, string name, string text,
        Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size,
        int fontSize, TextAnchor align, Color color, Font font)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = anchor;
        rt.pivot = pivot;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        var t = go.AddComponent<Text>();
        t.font = font; t.text = text; t.fontSize = fontSize;
        t.alignment = align; t.color = color;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow   = VerticalWrapMode.Overflow;
        t.supportRichText    = true;
        return t;
    }

    private static Button MkBtn(Transform parent, string name, string label,
        Vector2 pos, Vector2 size, Font font)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        var img = go.AddComponent<Image>();
        img.color = new Color(0.4f, 0.4f, 0.5f);
        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;

        var labelGo = new GameObject("Text");
        labelGo.transform.SetParent(go.transform, false);
        var lrt = labelGo.AddComponent<RectTransform>();
        lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
        lrt.offsetMin = Vector2.zero; lrt.offsetMax = Vector2.zero;
        var t = labelGo.AddComponent<Text>();
        t.font = font; t.text = label; t.fontSize = 22;
        t.color = Color.white; t.alignment = TextAnchor.MiddleCenter;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow   = VerticalWrapMode.Overflow;
        return btn;
    }
}
#endif
