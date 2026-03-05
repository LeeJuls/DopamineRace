#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using System.IO;

/// <summary>
/// 결과 화면 UI 프리팹 자동 생성 Editor 스크립트.
/// Unity 메뉴: DopamineRace > Create Result UI Prefabs
///
/// 구조: TitleText / RankSection (9 rows 자유배치 + PickArrow) / ScoreSection / NextRoundBtn
/// LayoutGroup 없음 → Inspector에서 자유롭게 위치/크기 조정 가능
/// 레이서 수: 9명 고정
///
/// 생성물:
///   Assets/Prefabs/UI/ResultPanel.prefab
/// </summary>
public static class ResultUIPrefabCreator
{
    private const string PREFAB_DIR = "Assets/Prefabs/UI";
    private const string RESULT_PANEL_PATH = "Assets/Prefabs/UI/ResultPanel.prefab";
    private const int MAX_RANK_ROWS = 9; // 레이서 수 9명 고정

    // ──────────────────────────────────────────────
    //  레이아웃 상수 (Inspector에서 조정 가능한 기준값)
    // ──────────────────────────────────────────────
    private const float ROW_WIDTH   = 480f;
    private const float ROW_HEIGHT  = 30f;
    private const float ROW_GAP     = 3f;
    private const float LABEL_H     = 22f;
    private const float LABEL_GAP   = 4f;
    private const float SEC_LEFT_PAD = 12f;
    private const float SEC_TOP_PAD  = 8f;

    // Row 내부 열 너비
    private const float COL_BADGE   = 40f;
    private const float COL_ICON    = 30f;
    private const float COL_NAME    = 260f;
    private const float COL_ARROW   = 120f;
    private const float COL_SPACING = 6f;
    private const float COL_LEFT_PAD = 4f;

    // ──────────────────────────────────────────────
    //  색상 상수
    // ──────────────────────────────────────────────
    private static readonly Color COLOR_BG         = new Color(0f,   0f,   0f,   0.82f);
    private static readonly Color COLOR_WIN        = new Color(1f,   0.85f, 0.2f, 1f);
    private static readonly Color COLOR_WHITE      = Color.white;
    private static readonly Color COLOR_GRAY       = new Color(0.75f, 0.75f, 0.75f, 1f);
    private static readonly Color COLOR_RANK1      = new Color(1f,   0.84f, 0f,   1f);   // Gold
    private static readonly Color COLOR_RANK2      = new Color(0.75f, 0.75f, 0.75f, 1f); // Silver
    private static readonly Color COLOR_RANK3      = new Color(0.80f, 0.50f, 0.20f, 1f); // Bronze
    private static readonly Color COLOR_RANK_REST  = new Color(0.45f, 0.45f, 0.45f, 1f); // 4위 이하
    private static readonly Color COLOR_BTN_NEXT   = new Color(0.25f, 0.50f, 0.90f, 1f);
    private static readonly Color COLOR_SECTION_BG = new Color(1f,   1f,   1f,   0.06f);

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
        UnityEngine.Object.DestroyImmediate(panel);
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
    internal static GameObject CreateResultPanelPrefab(Font font)
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

        // ── RankSection (9행 고정, LayoutGroup 없음) ──
        // 높이 계산: topPad + label + labelGap + 9행 + 8gap + botPad
        float sectionH = SEC_TOP_PAD + LABEL_H + LABEL_GAP
                       + MAX_RANK_ROWS * ROW_HEIGHT
                       + (MAX_RANK_ROWS - 1) * ROW_GAP
                       + SEC_TOP_PAD;  // bottom padding = top padding

        GameObject rankSection = MkContainer(root.transform, "RankSection",
            new Vector2(0.5f, 0.60f), new Vector2(0.5f, 0.60f),
            Vector2.zero, new Vector2(640, sectionH));
        rankSection.AddComponent<Image>().color = COLOR_SECTION_BG;
        // ↑ LayoutGroup 없음 — Inspector에서 자유 배치 가능

        // SectionLabel (상단 좌측 기준 배치)
        GameObject sectionLbl = MkTextChild(rankSection.transform, "SectionLabel", "순위",
            new Vector2(ROW_WIDTH, LABEL_H), 15, TextAnchor.MiddleLeft, COLOR_GRAY, font, false);
        SetTopLeft(sectionLbl, SEC_LEFT_PAD, -SEC_TOP_PAD, ROW_WIDTH, LABEL_H);

        // Rank1~9 Row (LayoutGroup 없음 — 자유 배치)
        float yRow = -(SEC_TOP_PAD + LABEL_H + LABEL_GAP);
        for (int i = 0; i < MAX_RANK_ROWS; i++)
        {
            Color badgeColor;
            if      (i == 0) badgeColor = COLOR_RANK1;
            else if (i == 1) badgeColor = COLOR_RANK2;
            else if (i == 2) badgeColor = COLOR_RANK3;
            else             badgeColor = COLOR_RANK_REST;

            string rankLabel = (i + 1) + "위";
            GameObject row = CreateRankRow(rankSection.transform, "Rank" + (i + 1) + "Row",
                rankLabel, badgeColor, font);
            SetTopLeft(row, SEC_LEFT_PAD, yRow, ROW_WIDTH, ROW_HEIGHT);

            yRow -= (ROW_HEIGHT + ROW_GAP);
        }

        // ── ScoreSection ──
        GameObject scoreSection = MkContainer(root.transform, "ScoreSection",
            new Vector2(0.5f, 0.20f), new Vector2(0.5f, 0.20f),
            Vector2.zero, new Vector2(640, 90));
        scoreSection.AddComponent<Image>().color = COLOR_SECTION_BG;
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
        btnRt.anchorMin = new Vector2(0.5f, 0.08f);
        btnRt.anchorMax = new Vector2(0.5f, 0.08f);
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

    // ── Rank Row (순위 행: 배지 + 아이콘 + 이름 + 화살표, LayoutGroup 없음) ──
    private static GameObject CreateRankRow(Transform parent, string name, string rankLabel, Color badgeColor, Font font)
    {
        GameObject row = new GameObject(name);
        row.transform.SetParent(parent, false);
        RectTransform rowRt = row.AddComponent<RectTransform>();
        rowRt.sizeDelta = new Vector2(ROW_WIDTH, ROW_HEIGHT);
        // ↑ HorizontalLayoutGroup 없음 — Inspector에서 자유 배치 가능

        // 열 X 위치 계산
        float xBadge  = COL_LEFT_PAD;
        float xIcon   = xBadge  + COL_BADGE  + COL_SPACING;
        float xName   = xIcon   + COL_ICON   + COL_SPACING;
        float xArrow  = xName   + COL_NAME   + COL_SPACING;

        // RankBadge (배경 + 텍스트)
        GameObject badge = new GameObject("RankBadge");
        badge.transform.SetParent(row.transform, false);
        SetLeftCenter(badge, xBadge, COL_BADGE, 26f);
        badge.AddComponent<Image>().color = badgeColor;

        GameObject badgeText = new GameObject("BadgeText");
        badgeText.transform.SetParent(badge.transform, false);
        RectTransform btRt = badgeText.AddComponent<RectTransform>();
        btRt.anchorMin = Vector2.zero;
        btRt.anchorMax = Vector2.one;
        btRt.offsetMin = Vector2.zero;
        btRt.offsetMax = Vector2.zero;
        Text bt = badgeText.AddComponent<Text>();
        bt.text = rankLabel;
        bt.fontSize = 14;
        bt.fontStyle = FontStyle.Bold;
        bt.alignment = TextAnchor.MiddleCenter;
        bt.color = Color.black;
        if (font != null) bt.font = font;

        // CharIconMask (30×30 RectMask2D — 얼굴 영역만 보이도록 클리핑)
        GameObject iconMask = new GameObject("CharIconMask");
        iconMask.transform.SetParent(row.transform, false);
        SetLeftCenter(iconMask, xIcon, COL_ICON, COL_ICON);
        iconMask.AddComponent<RectMask2D>();

        // CharIcon (실제 스프라이트 — 상단 앵커로 얼굴 노출, AspectRatioFitter로 높이 자동 조정)
        GameObject icon = new GameObject("CharIcon");
        icon.transform.SetParent(iconMask.transform, false);
        RectTransform iconRt = icon.AddComponent<RectTransform>();
        iconRt.anchorMin        = new Vector2(0f, 1f);   // 너비 stretch (좌→우)
        iconRt.anchorMax        = new Vector2(1f, 1f);   // y: 마스크 상단 고정
        iconRt.pivot            = new Vector2(0.5f, 1f);
        iconRt.anchoredPosition = Vector2.zero;
        iconRt.sizeDelta        = new Vector2(0f, 75f);  // 너비=부모, 초기 높이=75px
        Image iconImg = icon.AddComponent<Image>();
        iconImg.color          = new Color(0.3f, 0.3f, 0.3f, 1f); // 스프라이트 없을 때 회색
        iconImg.preserveAspect = false;
        // WidthControlsHeight: 30px 너비 기준으로 높이를 비율에 맞게 조정
        // aspectRatio는 런타임(ShowResult)에서 실제 스프라이트 비율로 갱신
        AspectRatioFitter iconFitter = icon.AddComponent<AspectRatioFitter>();
        iconFitter.aspectMode  = AspectRatioFitter.AspectMode.WidthControlsHeight;
        iconFitter.aspectRatio = 0.4f; // 기본값 (너비:높이 ≈ 2:5)

        // CharName
        GameObject charName = new GameObject("CharName");
        charName.transform.SetParent(row.transform, false);
        SetLeftCenter(charName, xName, COL_NAME, 28f);
        Text nameText = charName.AddComponent<Text>();
        nameText.text = "-";
        nameText.fontSize = 20;
        nameText.alignment = TextAnchor.MiddleLeft;
        nameText.color = COLOR_WHITE;
        nameText.resizeTextForBestFit = true;
        nameText.resizeTextMinSize = 13;
        nameText.resizeTextMaxSize = 22;
        if (font != null) nameText.font = font;

        // PickArrow (← 1st / ← pick — 기본 hidden)
        GameObject pickArrow = new GameObject("PickArrow");
        pickArrow.transform.SetParent(row.transform, false);
        SetLeftCenter(pickArrow, xArrow, COL_ARROW, 28f);
        Text arrowText = pickArrow.AddComponent<Text>();
        arrowText.text = "";
        arrowText.fontSize = 18;
        arrowText.fontStyle = FontStyle.Bold;
        arrowText.alignment = TextAnchor.MiddleLeft;
        arrowText.color = COLOR_WHITE;
        if (font != null) arrowText.font = font;
        pickArrow.SetActive(false); // 기본 비활성 (런타임에 화살표 표시 시 활성화)

        return row;
    }

    // ══════════════════════════════════════════════
    //  Patch: 없는 요소만 추가 (v3: 9행 고정, LayoutGroup 없음)
    // ══════════════════════════════════════════════
    private static void PatchResultPanel(Transform root, Font font)
    {
        // TitleText
        PatchEnsureText(root, "TitleText", "WIN", 52, TextAnchor.MiddleCenter, COLOR_WIN, font, true);

        // ── BetResultSection 레거시 제거 ──
        Transform oldBetSection = root.Find("BetResultSection");
        if (oldBetSection != null)
        {
            Debug.Log("[ResultUIPrefabCreator][Patch] BetResultSection 레거시 제거");
            UnityEngine.Object.DestroyImmediate(oldBetSection.gameObject);
        }

        // ── RankSection ──
        Transform rankSection = root.Find("RankSection");
        if (rankSection == null)
        {
            float sectionH = SEC_TOP_PAD + LABEL_H + LABEL_GAP
                           + MAX_RANK_ROWS * ROW_HEIGHT
                           + (MAX_RANK_ROWS - 1) * ROW_GAP
                           + SEC_TOP_PAD;

            Debug.Log("[ResultUIPrefabCreator][Patch] RankSection 없음 → 추가");
            GameObject rSec = MkContainer(root, "RankSection",
                new Vector2(0.5f, 0.60f), new Vector2(0.5f, 0.60f),
                Vector2.zero, new Vector2(640, sectionH));
            rSec.AddComponent<Image>().color = COLOR_SECTION_BG;
            rankSection = rSec.transform;

            GameObject sLbl = MkTextChild(rankSection, "SectionLabel", "순위",
                new Vector2(ROW_WIDTH, LABEL_H), 15, TextAnchor.MiddleLeft, COLOR_GRAY, font, false);
            SetTopLeft(sLbl, SEC_LEFT_PAD, -SEC_TOP_PAD, ROW_WIDTH, LABEL_H);
        }
        else
        {
            // 기존 RankSection에 VLG가 있으면 제거 (구버전 정리)
            var oldVlg = rankSection.GetComponent<VerticalLayoutGroup>();
            if (oldVlg != null)
            {
                Debug.Log("[ResultUIPrefabCreator][Patch] RankSection의 VerticalLayoutGroup 제거");
                UnityEngine.Object.DestroyImmediate(oldVlg);
            }
            if (rankSection.Find("SectionLabel") == null)
            {
                GameObject sLbl = MkTextChild(rankSection, "SectionLabel", "순위",
                    new Vector2(ROW_WIDTH, LABEL_H), 15, TextAnchor.MiddleLeft, COLOR_GRAY, font, false);
                SetTopLeft(sLbl, SEC_LEFT_PAD, -SEC_TOP_PAD, ROW_WIDTH, LABEL_H);
            }
        }

        // 9행 RankRow 보장 (없는 것만 추가, 기존 Row의 HLG는 제거)
        float yRow = -(SEC_TOP_PAD + LABEL_H + LABEL_GAP);
        for (int i = 0; i < MAX_RANK_ROWS; i++)
        {
            string rowName = "Rank" + (i + 1) + "Row";
            Transform existingRow = rankSection.Find(rowName);

            if (existingRow == null)
            {
                Color badgeColor;
                if      (i == 0) badgeColor = COLOR_RANK1;
                else if (i == 1) badgeColor = COLOR_RANK2;
                else if (i == 2) badgeColor = COLOR_RANK3;
                else             badgeColor = COLOR_RANK_REST;

                Debug.Log("[ResultUIPrefabCreator][Patch] " + rowName + " 없음 → 추가");
                GameObject row = CreateRankRow(rankSection, rowName, (i + 1) + "위", badgeColor, font);
                SetTopLeft(row, SEC_LEFT_PAD, yRow, ROW_WIDTH, ROW_HEIGHT);
            }
            else
            {
                // 기존 Row에 HLG가 있으면 제거 (구버전 정리)
                var oldHlg = existingRow.GetComponent<HorizontalLayoutGroup>();
                if (oldHlg != null)
                {
                    Debug.Log("[ResultUIPrefabCreator][Patch] " + rowName + " HorizontalLayoutGroup 제거");
                    UnityEngine.Object.DestroyImmediate(oldHlg);
                }

                // CharIconMask 없으면 추가 (기존 CharIcon을 마스크 방식으로 전환)
                if (existingRow.Find("CharIconMask") == null)
                {
                    float xIconPos = COL_LEFT_PAD + COL_BADGE + COL_SPACING;
                    Debug.Log("[ResultUIPrefabCreator][Patch] " + rowName + "/CharIconMask 없음 → 추가");

                    Transform oldIcon = existingRow.Find("CharIcon");

                    GameObject iconMask = new GameObject("CharIconMask");
                    iconMask.transform.SetParent(existingRow, false);
                    SetLeftCenter(iconMask, xIconPos, COL_ICON, COL_ICON);
                    iconMask.AddComponent<RectMask2D>();

                    if (oldIcon != null)
                    {
                        // 기존 CharIcon을 CharIconMask 안으로 이동
                        oldIcon.SetParent(iconMask.transform, false);
                        RectTransform oldIconRt = oldIcon.GetComponent<RectTransform>();
                        if (oldIconRt != null)
                        {
                            oldIconRt.anchorMin        = new Vector2(0f, 1f);
                            oldIconRt.anchorMax        = new Vector2(1f, 1f);
                            oldIconRt.pivot            = new Vector2(0.5f, 1f);
                            oldIconRt.anchoredPosition = Vector2.zero;
                            oldIconRt.sizeDelta        = new Vector2(0f, 75f);
                        }
                        Image oldImg = oldIcon.GetComponent<Image>();
                        if (oldImg != null) oldImg.preserveAspect = false;
                        if (oldIcon.GetComponent<AspectRatioFitter>() == null)
                        {
                            var fit = oldIcon.gameObject.AddComponent<AspectRatioFitter>();
                            fit.aspectMode  = AspectRatioFitter.AspectMode.WidthControlsHeight;
                            fit.aspectRatio = 0.4f;
                        }
                    }
                    else
                    {
                        // CharIcon 자체가 없으면 새로 생성
                        GameObject icon = new GameObject("CharIcon");
                        icon.transform.SetParent(iconMask.transform, false);
                        RectTransform iconRt = icon.AddComponent<RectTransform>();
                        iconRt.anchorMin        = new Vector2(0f, 1f);
                        iconRt.anchorMax        = new Vector2(1f, 1f);
                        iconRt.pivot            = new Vector2(0.5f, 1f);
                        iconRt.anchoredPosition = Vector2.zero;
                        iconRt.sizeDelta        = new Vector2(0f, 75f);
                        Image iconImg = icon.AddComponent<Image>();
                        iconImg.color          = new Color(0.3f, 0.3f, 0.3f, 1f);
                        iconImg.preserveAspect = false;
                        var fit = icon.AddComponent<AspectRatioFitter>();
                        fit.aspectMode  = AspectRatioFitter.AspectMode.WidthControlsHeight;
                        fit.aspectRatio = 0.4f;
                    }
                }

                // PickArrow 없으면 추가
                if (existingRow.Find("PickArrow") == null)
                {
                    float xArrow = COL_LEFT_PAD + COL_BADGE + COL_SPACING
                                 + COL_ICON + COL_SPACING + COL_NAME + COL_SPACING;
                    Debug.Log("[ResultUIPrefabCreator][Patch] " + rowName + "/PickArrow 없음 → 추가");
                    GameObject pickArrow = new GameObject("PickArrow");
                    pickArrow.transform.SetParent(existingRow, false);
                    SetLeftCenter(pickArrow, xArrow, COL_ARROW, 28f);
                    Text arrowText = pickArrow.AddComponent<Text>();
                    arrowText.fontSize = 18;
                    arrowText.fontStyle = FontStyle.Bold;
                    arrowText.alignment = TextAnchor.MiddleLeft;
                    arrowText.color = COLOR_WHITE;
                    if (font != null) arrowText.font = font;
                    pickArrow.SetActive(false);
                }

                // BadgeText 없으면 추가
                Transform badgeT = existingRow.Find("RankBadge");
                if (badgeT != null && badgeT.Find("BadgeText") == null)
                {
                    Debug.Log("[ResultUIPrefabCreator][Patch] " + rowName + "/RankBadge/BadgeText 없음 → 추가");
                    GameObject badgeText = new GameObject("BadgeText");
                    badgeText.transform.SetParent(badgeT, false);
                    RectTransform btRt = badgeText.AddComponent<RectTransform>();
                    btRt.anchorMin = Vector2.zero;
                    btRt.anchorMax = Vector2.one;
                    btRt.offsetMin = Vector2.zero;
                    btRt.offsetMax = Vector2.zero;
                    Text bt = badgeText.AddComponent<Text>();
                    bt.text = (i + 1) + "위";
                    bt.fontSize = 14;
                    bt.fontStyle = FontStyle.Bold;
                    bt.alignment = TextAnchor.MiddleCenter;
                    bt.color = Color.black;
                    if (font != null) bt.font = font;
                }
            }
            yRow -= (ROW_HEIGHT + ROW_GAP);
        }

        // ── ScoreSection ──
        Transform scoreSection = root.Find("ScoreSection");
        if (scoreSection == null)
        {
            Debug.Log("[ResultUIPrefabCreator][Patch] ScoreSection 없음 → 추가");
            GameObject sSec = MkContainer(root, "ScoreSection",
                new Vector2(0.5f, 0.20f), new Vector2(0.5f, 0.20f),
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
            MkTextChild(scoreSection, "TotalScoreText",   "", new Vector2(580, 38), 30, TextAnchor.MiddleCenter, COLOR_WIN,   font, true);
        }
        else
        {
            if (scoreSection.Find("ScoreFormulaText") == null)
                MkTextChild(scoreSection, "ScoreFormulaText", "", new Vector2(580, 28), 20, TextAnchor.MiddleCenter, COLOR_WHITE, font, true);
            if (scoreSection.Find("TotalScoreText") == null)
                MkTextChild(scoreSection, "TotalScoreText",   "", new Vector2(580, 38), 30, TextAnchor.MiddleCenter, COLOR_WIN,   font, true);
        }

        // ── NextRoundBtn ──
        if (root.Find("NextRoundBtn") == null)
        {
            Debug.Log("[ResultUIPrefabCreator][Patch] NextRoundBtn 없음 → 추가");
            GameObject btn = new GameObject("NextRoundBtn");
            btn.transform.SetParent(root, false);
            RectTransform btnRt = btn.AddComponent<RectTransform>();
            btnRt.anchorMin = new Vector2(0.5f, 0.08f);
            btnRt.anchorMax = new Vector2(0.5f, 0.08f);
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
    //  RectTransform 배치 헬퍼
    // ══════════════════════════════════════════════

    /// <summary>
    /// 부모 상단-좌측 기준 배치 (anchor=TopLeft, pivot=TopLeft).
    /// x: 좌측에서 픽셀, y: 상단에서 픽셀 (음수 = 아래쪽)
    /// </summary>
    private static void SetTopLeft(GameObject go, float x, float y, float w, float h)
    {
        RectTransform rt = go.GetComponent<RectTransform>();
        if (rt == null) rt = go.AddComponent<RectTransform>();
        rt.anchorMin        = new Vector2(0f, 1f);
        rt.anchorMax        = new Vector2(0f, 1f);
        rt.pivot            = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(x, y);
        rt.sizeDelta        = new Vector2(w, h);
    }

    /// <summary>
    /// 부모 좌측-중앙 기준 배치 (anchor=LeftCenter, pivot=LeftCenter).
    /// x: 좌측에서 픽셀, y는 항상 0 (수직 중앙 정렬)
    /// </summary>
    private static void SetLeftCenter(GameObject go, float x, float w, float h)
    {
        RectTransform rt = go.GetComponent<RectTransform>();
        if (rt == null) rt = go.AddComponent<RectTransform>();
        rt.anchorMin        = new Vector2(0f, 0.5f);
        rt.anchorMax        = new Vector2(0f, 0.5f);
        rt.pivot            = new Vector2(0f, 0.5f);
        rt.anchoredPosition = new Vector2(x, 0f);
        rt.sizeDelta        = new Vector2(w, h);
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
