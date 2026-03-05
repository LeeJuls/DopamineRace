#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using System.IO;

/// <summary>
/// 결과 화면 UI 프리팹 자동 생성 Editor 스크립트.
/// Unity 메뉴: DopamineRace > Create Result UI Prefabs
///
/// 생성물:
///   Assets/Prefabs/UI/ResultPanel.prefab
/// </summary>
public static class ResultUIPrefabCreator
{
    private const string PREFAB_DIR = "Assets/Prefabs/UI";
    private const string RESULT_PANEL_PATH = "Assets/Prefabs/UI/ResultPanel.prefab";

    // ──────────────────────────────────────────────
    //  색상 상수
    // ──────────────────────────────────────────────
    private static readonly Color COLOR_BG         = new Color(0f,   0f,   0f,   0.82f);
    private static readonly Color COLOR_WIN        = new Color(1f,   0.85f, 0.2f, 1f);
    private static readonly Color COLOR_WHITE      = Color.white;
    private static readonly Color COLOR_GRAY       = new Color(0.75f, 0.75f, 0.75f, 1f);
    private static readonly Color COLOR_RANK1      = new Color(1f,   0.84f, 0f,   1f);  // Gold
    private static readonly Color COLOR_RANK2      = new Color(0.75f, 0.75f, 0.75f, 1f); // Silver
    private static readonly Color COLOR_RANK3      = new Color(0.80f, 0.50f, 0.20f, 1f); // Bronze
    private static readonly Color COLOR_BTN_NEXT   = new Color(0.25f, 0.50f, 0.90f, 1f);
    private static readonly Color COLOR_SECTION_BG = new Color(1f,   1f,   1f,   0.06f);
    private static readonly Color COLOR_PICK_HIT   = new Color(0.4f,  1f,   0.4f,  1f);
    private static readonly Color COLOR_PICK_MISS  = new Color(1f,   0.4f,  0.4f,  1f);

    // ══════════════════════════════════════════════
    //  메뉴 항목: Create (완전 재생성)
    // ══════════════════════════════════════════════
    [MenuItem("DopamineRace/Create Result UI Prefabs")]
    public static void CreatePrefabs()
    {
        bool ok = EditorUtility.DisplayDialog(
            "⚠️ Result 프리팹 재생성 확인",
            "기존 ResultPanel.prefab을 완전히 삭제하고 새로 만듭니다.\n\n" +
            "🚨 기존 수동 수정 내용이 모두 사라집니다!\n\n" +
            "최초 세팅 시에만 사용하세요.\n\n" +
            "정말로 진행할까요?",
            "Yes, 완전 재생성",
            "No, 취소"
        );
        if (!ok)
        {
            Debug.Log("[ResultUIPrefabCreator] 프리팹 재생성 취소됨.");
            return;
        }

        EnsureDirectory(PREFAB_DIR);

        var gs = AssetDatabase.LoadAssetAtPath<GameSettings>("Assets/Resources/GameSettings.asset");
        Font font = gs != null ? gs.mainFont : null;

        // ResultPanel.prefab 생성
        GameObject panel = CreateResultPanelPrefab(font);
        PrefabUtility.SaveAsPrefabAsset(panel, RESULT_PANEL_PATH);
        Object.DestroyImmediate(panel);
        Debug.Log("[ResultUIPrefabCreator] 생성: " + RESULT_PANEL_PATH);

        // GameSettings에 자동 연결
        if (gs != null)
        {
            gs.resultPanelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(RESULT_PANEL_PATH);
            EditorUtility.SetDirty(gs);
            AssetDatabase.SaveAssets();
            Debug.Log("[ResultUIPrefabCreator] GameSettings.resultPanelPrefab 자동 연결 완료!");
        }
        else
        {
            Debug.LogWarning("[ResultUIPrefabCreator] GameSettings.asset을 찾을 수 없습니다. 수동으로 연결해주세요.");
        }

        AssetDatabase.Refresh();
    }

    // ══════════════════════════════════════════════
    //  메뉴 항목: Patch (안전 패치)
    // ══════════════════════════════════════════════
    [MenuItem("DopamineRace/Patch Result UI Prefabs (Safe)")]
    public static void PatchPrefabs()
    {
        bool ok = EditorUtility.DisplayDialog(
            "Result 프리팹 안전 패치",
            "기존 ResultPanel.prefab에 없는 요소만 추가합니다.\n" +
            "기존 설정(위치, 크기, 색상 등)은 유지됩니다.\n\n" +
            "진행할까요?",
            "Yes, 패치",
            "No, 취소"
        );
        if (!ok)
        {
            Debug.Log("[ResultUIPrefabCreator] 패치 취소됨.");
            return;
        }

        if (!File.Exists(RESULT_PANEL_PATH))
        {
            Debug.LogWarning("[ResultUIPrefabCreator] ResultPanel.prefab이 없습니다. Create를 먼저 실행하세요.");
            CreatePrefabs();
            return;
        }

        var gs = AssetDatabase.LoadAssetAtPath<GameSettings>("Assets/Resources/GameSettings.asset");
        Font font = gs != null ? gs.mainFont : null;

        using (var scope = new PrefabUtility.EditPrefabContentsScope(RESULT_PANEL_PATH))
        {
            Transform root = scope.prefabContentsRoot.transform;
            PatchResultPanel(root, font);
        }

        // GameSettings 연결 확인
        if (gs != null && gs.resultPanelPrefab == null)
        {
            gs.resultPanelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(RESULT_PANEL_PATH);
            EditorUtility.SetDirty(gs);
            AssetDatabase.SaveAssets();
        }

        Debug.Log("[ResultUIPrefabCreator] Patch 완료: " + RESULT_PANEL_PATH);
    }

    // ══════════════════════════════════════════════
    //  ResultPanel 프리팹 생성
    // ══════════════════════════════════════════════
    private static GameObject CreateResultPanelPrefab(Font font)
    {
        // 루트
        GameObject root = new GameObject("ResultPanel");
        RectTransform rootRt = root.AddComponent<RectTransform>();
        rootRt.anchorMin = Vector2.zero;
        rootRt.anchorMax = Vector2.one;
        rootRt.offsetMin = Vector2.zero;
        rootRt.offsetMax = Vector2.zero;

        Image bg = root.AddComponent<Image>();
        bg.color = COLOR_BG;

        // ── TitleText (WIN / LOSE) ──
        MkText(root.transform, "TitleText", "WIN",
            new Vector2(0.5f, 0.93f), new Vector2(0.5f, 0.93f),
            Vector2.zero, new Vector2(600, 65),
            52, TextAnchor.MiddleCenter, COLOR_WIN, font, true);

        // ── RankSection ──
        GameObject rankSection = MkContainer(root.transform, "RankSection",
            new Vector2(0.5f, 0.74f), new Vector2(0.5f, 0.74f),
            Vector2.zero, new Vector2(640, 160));
        Image rankBg = rankSection.AddComponent<Image>();
        rankBg.color = COLOR_SECTION_BG;
        VerticalLayoutGroup rankVlg = rankSection.AddComponent<VerticalLayoutGroup>();
        rankVlg.childAlignment = TextAnchor.MiddleLeft;
        rankVlg.spacing = 4f;
        rankVlg.padding = new RectOffset(12, 12, 8, 8);
        rankVlg.childControlWidth = true;
        rankVlg.childControlHeight = false;
        rankVlg.childForceExpandWidth = true;
        rankVlg.childForceExpandHeight = false;

        // 섹션 라벨
        GameObject sectionLbl = MkTextChild(rankSection.transform, "SectionLabel", "순위",
            new Vector2(480, 24), 16, TextAnchor.MiddleLeft, COLOR_GRAY, font, false);
        SetLayoutElement(sectionLbl, 480, 24);

        // Rank1 / Rank2 / Rank3 Row
        Color[] rankColors = { COLOR_RANK1, COLOR_RANK2, COLOR_RANK3 };
        string[] rankLabels = { "1위", "2위", "3위" };
        for (int i = 0; i < 3; i++)
        {
            CreateRankRow(rankSection.transform, "Rank" + (i + 1) + "Row",
                rankLabels[i], rankColors[i], font);
        }

        // ── BetResultSection ──
        GameObject betSection = MkContainer(root.transform, "BetResultSection",
            new Vector2(0.5f, 0.535f), new Vector2(0.5f, 0.535f),
            Vector2.zero, new Vector2(640, 130));
        Image betBg = betSection.AddComponent<Image>();
        betBg.color = COLOR_SECTION_BG;
        VerticalLayoutGroup betVlg = betSection.AddComponent<VerticalLayoutGroup>();
        betVlg.childAlignment = TextAnchor.MiddleLeft;
        betVlg.spacing = 4f;
        betVlg.padding = new RectOffset(12, 12, 8, 8);
        betVlg.childControlWidth = true;
        betVlg.childControlHeight = false;
        betVlg.childForceExpandWidth = true;
        betVlg.childForceExpandHeight = false;

        // 배팅 타입 라벨
        GameObject betTypeLbl = MkTextChild(betSection.transform, "BetTypeLabel", "-",
            new Vector2(480, 26), 18, TextAnchor.MiddleLeft, COLOR_WHITE, font, false);
        SetLayoutElement(betTypeLbl, 480, 26);

        // Pick1 / Pick2 / Pick3 Row (Pick2, Pick3는 기본 hidden)
        for (int i = 0; i < 3; i++)
        {
            GameObject pickRow = CreatePickRow(betSection.transform, "Pick" + (i + 1) + "Row", font);
            if (i > 0) pickRow.SetActive(false);
        }

        // ── ScoreSection ──
        GameObject scoreSection = MkContainer(root.transform, "ScoreSection",
            new Vector2(0.5f, 0.355f), new Vector2(0.5f, 0.355f),
            Vector2.zero, new Vector2(640, 90));
        Image scoreBg = scoreSection.AddComponent<Image>();
        scoreBg.color = COLOR_SECTION_BG;
        VerticalLayoutGroup scoreVlg = scoreSection.AddComponent<VerticalLayoutGroup>();
        scoreVlg.childAlignment = TextAnchor.MiddleCenter;
        scoreVlg.spacing = 4f;
        scoreVlg.padding = new RectOffset(12, 12, 8, 8);
        scoreVlg.childControlWidth = true;
        scoreVlg.childControlHeight = false;
        scoreVlg.childForceExpandWidth = true;
        scoreVlg.childForceExpandHeight = false;

        GameObject formulaLbl = MkTextChild(scoreSection.transform, "ScoreFormulaText", "",
            new Vector2(580, 28), 20, TextAnchor.MiddleCenter, COLOR_WHITE, font, true);
        SetLayoutElement(formulaLbl, 580, 28);

        GameObject totalLbl = MkTextChild(scoreSection.transform, "TotalScoreText", "",
            new Vector2(580, 38), 30, TextAnchor.MiddleCenter, COLOR_WIN, font, true);
        SetLayoutElement(totalLbl, 580, 38);

        // ── NextRoundBtn ──
        GameObject btn = new GameObject("NextRoundBtn");
        btn.transform.SetParent(root.transform, false);
        RectTransform btnRt = btn.AddComponent<RectTransform>();
        btnRt.anchorMin = new Vector2(0.5f, 0.12f);
        btnRt.anchorMax = new Vector2(0.5f, 0.12f);
        btnRt.pivot = new Vector2(0.5f, 0.5f);
        btnRt.sizeDelta = new Vector2(260, 62);
        btn.AddComponent<Image>().color = COLOR_BTN_NEXT;
        btn.AddComponent<Button>();
        MkText(btn.transform, "BtnText", "다음 라운드",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(260, 62),
            26, TextAnchor.MiddleCenter, COLOR_WHITE, font, false);

        return root;
    }

    // ── Rank Row (1위/2위/3위) ──
    private static void CreateRankRow(Transform parent, string name, string rankLabel, Color badgeColor, Font font)
    {
        GameObject row = new GameObject(name);
        row.transform.SetParent(parent, false);
        RectTransform rowRt = row.AddComponent<RectTransform>();
        rowRt.sizeDelta = new Vector2(480, 34);

        HorizontalLayoutGroup hlg = row.AddComponent<HorizontalLayoutGroup>();
        hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.spacing = 8f;
        hlg.padding = new RectOffset(0, 0, 0, 0);
        hlg.childControlWidth = false;
        hlg.childControlHeight = false;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;

        SetLayoutElement(row, 480, 34);

        // RankBadge (배경 + 텍스트)
        GameObject badge = new GameObject("RankBadge");
        badge.transform.SetParent(row.transform, false);
        RectTransform badgeRt = badge.AddComponent<RectTransform>();
        badgeRt.sizeDelta = new Vector2(42, 28);
        Image badgeImg = badge.AddComponent<Image>();
        badgeImg.color = badgeColor;
        SetLayoutElement(badge, 42, 28);

        GameObject badgeText = new GameObject("BadgeText");
        badgeText.transform.SetParent(badge.transform, false);
        RectTransform badgeTextRt = badgeText.AddComponent<RectTransform>();
        badgeTextRt.anchorMin = Vector2.zero;
        badgeTextRt.anchorMax = Vector2.one;
        badgeTextRt.offsetMin = Vector2.zero;
        badgeTextRt.offsetMax = Vector2.zero;
        Text bt = badgeText.AddComponent<Text>();
        bt.text = rankLabel;
        bt.fontSize = 15;
        bt.fontStyle = FontStyle.Bold;
        bt.alignment = TextAnchor.MiddleCenter;
        bt.color = Color.black;
        if (font != null) bt.font = font;

        // CharIcon (픽셀 아이콘 자리)
        GameObject icon = new GameObject("CharIcon");
        icon.transform.SetParent(row.transform, false);
        RectTransform iconRt = icon.AddComponent<RectTransform>();
        iconRt.sizeDelta = new Vector2(36, 36);
        Image iconImg = icon.AddComponent<Image>();
        iconImg.color = new Color(0.3f, 0.3f, 0.3f, 1f);  // 아이콘 없을 때 회색
        iconImg.preserveAspect = true;
        SetLayoutElement(icon, 36, 36);

        // CharName
        GameObject charName = new GameObject("CharName");
        charName.transform.SetParent(row.transform, false);
        RectTransform nameRt = charName.AddComponent<RectTransform>();
        nameRt.sizeDelta = new Vector2(360, 30);
        Text nameText = charName.AddComponent<Text>();
        nameText.text = "-";
        nameText.fontSize = 22;
        nameText.alignment = TextAnchor.MiddleLeft;
        nameText.color = COLOR_WHITE;
        nameText.resizeTextForBestFit = true;
        nameText.resizeTextMinSize = 14;
        nameText.resizeTextMaxSize = 24;
        if (font != null) nameText.font = font;
        SetLayoutElement(charName, 360, 30);
    }

    // ── Pick Row (내 선택 → 실제 순위) ──
    private static GameObject CreatePickRow(Transform parent, string name, Font font)
    {
        GameObject row = new GameObject(name);
        row.transform.SetParent(parent, false);
        RectTransform rowRt = row.AddComponent<RectTransform>();
        rowRt.sizeDelta = new Vector2(480, 28);

        HorizontalLayoutGroup hlg = row.AddComponent<HorizontalLayoutGroup>();
        hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.spacing = 8f;
        hlg.childControlWidth = false;
        hlg.childControlHeight = false;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;

        SetLayoutElement(row, 480, 28);

        // LabelText ("1착:", "2착:", "3착:")
        GameObject lbl = new GameObject("LabelText");
        lbl.transform.SetParent(row.transform, false);
        RectTransform lblRt = lbl.AddComponent<RectTransform>();
        lblRt.sizeDelta = new Vector2(50, 26);
        Text lblText = lbl.AddComponent<Text>();
        lblText.text = "-";
        lblText.fontSize = 18;
        lblText.alignment = TextAnchor.MiddleLeft;
        lblText.color = COLOR_GRAY;
        if (font != null) lblText.font = font;
        SetLayoutElement(lbl, 50, 26);

        // PickName (선택한 캐릭터 이름)
        GameObject pickName = new GameObject("PickName");
        pickName.transform.SetParent(row.transform, false);
        RectTransform pickNameRt = pickName.AddComponent<RectTransform>();
        pickNameRt.sizeDelta = new Vector2(220, 26);
        Text pickNameText = pickName.AddComponent<Text>();
        pickNameText.text = "-";
        pickNameText.fontSize = 18;
        pickNameText.alignment = TextAnchor.MiddleLeft;
        pickNameText.color = COLOR_WHITE;
        if (font != null) pickNameText.font = font;
        SetLayoutElement(pickName, 220, 26);

        // PickResult ("→ 1위" or "→ 5위")
        GameObject pickResult = new GameObject("PickResult");
        pickResult.transform.SetParent(row.transform, false);
        RectTransform pickResultRt = pickResult.AddComponent<RectTransform>();
        pickResultRt.sizeDelta = new Vector2(160, 26);
        Text pickResultText = pickResult.AddComponent<Text>();
        pickResultText.text = "";
        pickResultText.fontSize = 18;
        pickResultText.fontStyle = FontStyle.Bold;
        pickResultText.alignment = TextAnchor.MiddleLeft;
        pickResultText.color = COLOR_PICK_HIT;
        if (font != null) pickResultText.font = font;
        SetLayoutElement(pickResult, 160, 26);

        return row;
    }

    // ══════════════════════════════════════════════
    //  Patch: 없는 요소만 추가
    // ══════════════════════════════════════════════
    private static void PatchResultPanel(Transform root, Font font)
    {
        // TitleText
        PatchEnsureText(root, "TitleText", "WIN", 52, TextAnchor.MiddleCenter, COLOR_WIN, font, true);

        // RankSection
        Transform rankSection = root.Find("RankSection");
        if (rankSection == null)
        {
            Debug.Log("[ResultUIPrefabCreator][Patch] RankSection 없음 → 추가");
            GameObject rSec = MkContainer(root, "RankSection",
                new Vector2(0.5f, 0.74f), new Vector2(0.5f, 0.74f),
                Vector2.zero, new Vector2(640, 160));
            rSec.AddComponent<Image>().color = COLOR_SECTION_BG;
            VerticalLayoutGroup vlg = rSec.AddComponent<VerticalLayoutGroup>();
            vlg.childAlignment = TextAnchor.MiddleLeft;
            vlg.spacing = 4f;
            vlg.padding = new RectOffset(12, 12, 8, 8);
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            rankSection = rSec.transform;
            MkTextChild(rankSection, "SectionLabel", "순위", new Vector2(480, 24), 16, TextAnchor.MiddleLeft, COLOR_GRAY, font, false);
        }
        string[] rankLabels = { "1위", "2위", "3위" };
        Color[] rankColors = { COLOR_RANK1, COLOR_RANK2, COLOR_RANK3 };
        for (int i = 0; i < 3; i++)
        {
            string rowName = "Rank" + (i + 1) + "Row";
            if (rankSection.Find(rowName) == null)
            {
                Debug.Log("[ResultUIPrefabCreator][Patch] " + rowName + " 없음 → 추가");
                CreateRankRow(rankSection, rowName, rankLabels[i], rankColors[i], font);
            }
        }

        // BetResultSection
        Transform betSection = root.Find("BetResultSection");
        if (betSection == null)
        {
            Debug.Log("[ResultUIPrefabCreator][Patch] BetResultSection 없음 → 추가");
            GameObject bSec = MkContainer(root, "BetResultSection",
                new Vector2(0.5f, 0.535f), new Vector2(0.5f, 0.535f),
                Vector2.zero, new Vector2(640, 130));
            bSec.AddComponent<Image>().color = COLOR_SECTION_BG;
            VerticalLayoutGroup vlg = bSec.AddComponent<VerticalLayoutGroup>();
            vlg.childAlignment = TextAnchor.MiddleLeft;
            vlg.spacing = 4f;
            vlg.padding = new RectOffset(12, 12, 8, 8);
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            betSection = bSec.transform;
            MkTextChild(betSection, "BetTypeLabel", "-", new Vector2(480, 26), 18, TextAnchor.MiddleLeft, COLOR_WHITE, font, false);
        }
        for (int i = 0; i < 3; i++)
        {
            string pickName = "Pick" + (i + 1) + "Row";
            if (betSection.Find(pickName) == null)
            {
                Debug.Log("[ResultUIPrefabCreator][Patch] " + pickName + " 없음 → 추가");
                GameObject pickRow = CreatePickRow(betSection, pickName, font);
                if (i > 0) pickRow.SetActive(false);
            }
        }

        // ScoreSection
        Transform scoreSection = root.Find("ScoreSection");
        if (scoreSection == null)
        {
            Debug.Log("[ResultUIPrefabCreator][Patch] ScoreSection 없음 → 추가");
            GameObject sSec = MkContainer(root, "ScoreSection",
                new Vector2(0.5f, 0.355f), new Vector2(0.5f, 0.355f),
                Vector2.zero, new Vector2(640, 90));
            sSec.AddComponent<Image>().color = COLOR_SECTION_BG;
            VerticalLayoutGroup vlg = sSec.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 4f;
            vlg.padding = new RectOffset(12, 12, 8, 8);
            vlg.childAlignment = TextAnchor.MiddleCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            scoreSection = sSec.transform;
            MkTextChild(scoreSection, "ScoreFormulaText", "", new Vector2(580, 28), 20, TextAnchor.MiddleCenter, COLOR_WHITE, font, true);
            MkTextChild(scoreSection, "TotalScoreText", "", new Vector2(580, 38), 30, TextAnchor.MiddleCenter, COLOR_WIN, font, true);
        }
        else
        {
            if (scoreSection.Find("ScoreFormulaText") == null)
                MkTextChild(scoreSection, "ScoreFormulaText", "", new Vector2(580, 28), 20, TextAnchor.MiddleCenter, COLOR_WHITE, font, true);
            if (scoreSection.Find("TotalScoreText") == null)
                MkTextChild(scoreSection, "TotalScoreText", "", new Vector2(580, 38), 30, TextAnchor.MiddleCenter, COLOR_WIN, font, true);
        }

        // NextRoundBtn
        if (root.Find("NextRoundBtn") == null)
        {
            Debug.Log("[ResultUIPrefabCreator][Patch] NextRoundBtn 없음 → 추가");
            GameObject btn = new GameObject("NextRoundBtn");
            btn.transform.SetParent(root, false);
            RectTransform btnRt = btn.AddComponent<RectTransform>();
            btnRt.anchorMin = new Vector2(0.5f, 0.12f);
            btnRt.anchorMax = new Vector2(0.5f, 0.12f);
            btnRt.pivot = new Vector2(0.5f, 0.5f);
            btnRt.sizeDelta = new Vector2(260, 62);
            btn.AddComponent<Image>().color = COLOR_BTN_NEXT;
            btn.AddComponent<Button>();
            MkText(btn.transform, "BtnText", "다음 라운드",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(260, 62),
                26, TextAnchor.MiddleCenter, COLOR_WHITE, font, false);
        }
    }

    // ══════════════════════════════════════════════
    //  헬퍼 유틸리티
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

    /// <summary>RectTransform + Text (앵커 기반)</summary>
    private static GameObject MkText(Transform parent, string name, string text,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPos, Vector2 sizeDelta,
        int fontSize, TextAnchor alignment, Color color, Font font, bool richText)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = sizeDelta;

        Text t = go.AddComponent<Text>();
        t.text = text;
        t.fontSize = fontSize;
        t.alignment = alignment;
        t.color = color;
        t.supportRichText = richText;
        if (font != null) t.font = font;
        return go;
    }

    /// <summary>LayoutGroup 자식용 Text (sizeDelta 직접 지정)</summary>
    private static GameObject MkTextChild(Transform parent, string name, string text,
        Vector2 sizeDelta, int fontSize, TextAnchor alignment, Color color, Font font, bool richText)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.sizeDelta = sizeDelta;

        Text t = go.AddComponent<Text>();
        t.text = text;
        t.fontSize = fontSize;
        t.alignment = alignment;
        t.color = color;
        t.supportRichText = richText;
        if (font != null) t.font = font;
        return go;
    }

    /// <summary>빈 컨테이너 RectTransform (앵커 기반)</summary>
    private static GameObject MkContainer(Transform parent, string name,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPos, Vector2 sizeDelta)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = sizeDelta;
        return go;
    }

    private static void SetLayoutElement(GameObject go, float preferredWidth, float preferredHeight)
    {
        LayoutElement le = go.AddComponent<LayoutElement>();
        le.preferredWidth = preferredWidth;
        le.preferredHeight = preferredHeight;
    }

    private static void PatchEnsureText(Transform root, string name, string defaultText,
        int fontSize, TextAnchor alignment, Color color, Font font, bool richText)
    {
        Transform existing = root.Find(name);
        if (existing != null) return;
        Debug.Log("[ResultUIPrefabCreator][Patch] " + name + " 없음 → 추가");
        MkText(root, name, defaultText,
            new Vector2(0.5f, 0.93f), new Vector2(0.5f, 0.93f),
            Vector2.zero, new Vector2(600, 65),
            fontSize, alignment, color, font, richText);
    }
}
#endif
