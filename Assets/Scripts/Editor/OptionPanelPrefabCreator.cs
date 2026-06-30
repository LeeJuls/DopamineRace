#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

/// <summary>
/// 옵션 패널 프리팹 자동 생성 Editor 스크립트.
/// Unity 메뉴: DopamineRace > Create Option Panel Prefab
///
/// 생성물:
///   Assets/Prefabs/UI/OptionPanel.prefab
/// </summary>
public static class OptionPanelPrefabCreator
{
    private const string PREFAB_DIR  = "Assets/Prefabs/UI";
    private const string PREFAB_PATH = "Assets/Prefabs/UI/OptionPanel.prefab";

    // 흰 배경 팔레트 (ExchangeModal/BetAmountModal 표준)
    static readonly Color darkNavy   = new Color(0.10f, 0.10f, 0.20f, 1f); // 주요 텍스트
    static readonly Color darkAmber  = new Color(0.55f, 0.30f, 0.00f, 1f); // 제목
    static readonly Color accentBlue = new Color(0.25f, 0.45f, 0.70f, 0.95f); // 액션 버튼
    static readonly Color dangerRed  = new Color(0.65f, 0.15f, 0.15f, 0.95f); // 위험 버튼
    static readonly Color neutralGray= new Color(0.40f, 0.40f, 0.45f, 0.95f); // 취소 버튼
    static readonly Color toggleBg   = new Color(0.88f, 0.90f, 0.95f, 1.00f); // 토글 행 배경
    static readonly Color checkBg    = new Color(0.70f, 0.75f, 0.85f, 1.00f); // 체크박스 배경
    static readonly Color checkMark  = new Color(0.15f, 0.65f, 0.15f, 1.00f); // 체크마크
    static readonly Color divider    = new Color(0.70f, 0.70f, 0.75f, 0.80f); // 구분선

    [MenuItem("DopamineRace/프리팹 생성/Option Panel Prefab")]
    public static void CreateOptionPanelPrefab()
    {
        // 폰트 참조
        var gs = AssetDatabase.LoadAssetAtPath<GameSettings>(
            "Assets/Resources/GameSettings.asset");
        Font font = gs != null ? gs.mainFont : null;

        // BG_01 스프라이트 (가이드 패턴)
        Sprite bgSprite = AssetDatabase.LoadAssetAtPath<Sprite>(
            "Assets/Resources/UI/Img_BG_ListSlot_BG_01.png");

        // ── 루트 (OptionPanel.cs) ──
        GameObject root = new GameObject("OptionPanel");
        RectTransform rootRt = root.AddComponent<RectTransform>();
        rootRt.anchorMin = Vector2.zero;
        rootRt.anchorMax = Vector2.one;
        rootRt.offsetMin = Vector2.zero;
        rootRt.offsetMax = Vector2.zero;
        OptionPanel optionPanel = root.AddComponent<OptionPanel>();

        // ── DimBg (반투명 배경) ──
        GameObject dimBg = MkChild(root, "DimBg", 0f, 0f, 1f, 1f, Vector2.zero, Vector2.zero);
        Image dimImg = dimBg.AddComponent<Image>();
        dimImg.color = new Color(0f, 0f, 0f, 0.5f);
        dimImg.raycastTarget = true;

        // ── PanelBg (옵션 패널 본체) ──
        GameObject panelBg = MkChild(root, "PanelBg",
            0.5f, 0.5f, 0.5f, 0.5f, Vector2.zero, new Vector2(425f, 500f));
        Image panelImg = panelBg.AddComponent<Image>();
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
            Debug.LogWarning("[OptionPanelPrefabCreator] Img_BG_ListSlot_BG_01 없음 — 단색 폴백");
            panelImg.color = new Color(0.12f, 0.10f, 0.18f, 0.95f);
        }

        // TitleLabel
        GameObject titleObj = MkTextObj(panelBg, "TitleLabel", font,
            0.5f, 1f, 0.5f, 1f, new Vector2(0, -40), new Vector2(375, 50),
            28, TextAnchor.MiddleCenter, darkAmber);
        Text titleText = titleObj.GetComponent<Text>();

        // BGMToggle
        GameObject bgmToggle = MkToggle(panelBg, "BGMToggle", "BGMLabel", "BGM 끄기", font,
            new Vector2(0, -125));

        // SFXToggle
        GameObject sfxToggle = MkToggle(panelBg, "SFXToggle", "SFXLabel", "SFX 끄기", font,
            new Vector2(0, -200));

        // Divider
        GameObject dividerObj = MkChild(panelBg, "Divider",
            0.1f, 0.5f, 0.9f, 0.5f, new Vector2(0, -256), new Vector2(0, 2));
        dividerObj.AddComponent<Image>().color = divider;

        // RestartButton
        GameObject restartBtn = MkButton(panelBg, "RestartButton", "RestartLabel", "재시작", font,
            new Vector2(0, -319), default, accentBlue);

        // QuitButton
        GameObject quitBtn = MkButton(panelBg, "QuitButton", "QuitLabel", "게임 종료", font,
            new Vector2(0, -400), default, dangerRed);

        // CloseButton (우상단 X)
        GameObject closeBtn = MkChild(panelBg, "CloseButton",
            1f, 1f, 1f, 1f, new Vector2(-30, -30), new Vector2(45, 45));
        Image closeBg = closeBtn.AddComponent<Image>();
        closeBg.color = new Color(0.6f, 0.1f, 0.1f, 0.9f);
        Button closeButtonComp = closeBtn.AddComponent<Button>();
        closeButtonComp.targetGraphic = closeBg;
        MkTextInObj(closeBtn, "X", font, 20, TextAnchor.MiddleCenter, Color.white);

        // ── ConfirmPopup (초기 비활성) ──
        GameObject confirmRoot = new GameObject("ConfirmPopup");
        confirmRoot.transform.SetParent(root.transform, false);
        RectTransform confirmRt = confirmRoot.AddComponent<RectTransform>();
        confirmRt.anchorMin = Vector2.zero;
        confirmRt.anchorMax = Vector2.one;
        confirmRt.offsetMin = Vector2.zero;
        confirmRt.offsetMax = Vector2.zero;
        ConfirmPopup confirmComp = confirmRoot.AddComponent<ConfirmPopup>();

        // ConfirmPopup DimBg
        GameObject confirmDim = MkChild(confirmRoot, "DimBg", 0f, 0f, 1f, 1f, Vector2.zero, Vector2.zero);
        Image confirmDimImg = confirmDim.AddComponent<Image>();
        confirmDimImg.color = new Color(0f, 0f, 0f, 0.7f);
        confirmDimImg.raycastTarget = true;

        // PopupBg
        GameObject popupBg = MkChild(confirmRoot, "PopupBg",
            0.5f, 0.5f, 0.5f, 0.5f, Vector2.zero, new Vector2(375f, 225f));
        Image popupImg = popupBg.AddComponent<Image>();
        popupImg.raycastTarget = false;
        if (bgSprite != null)
        {
            popupImg.sprite     = bgSprite;
            popupImg.type       = Image.Type.Sliced;
            popupImg.fillCenter = true;
            popupImg.color      = Color.white;
        }
        else
        {
            popupImg.color = new Color(0.12f, 0.10f, 0.18f, 0.98f);
        }

        // MessageLabel
        GameObject msgObj = MkTextObj(popupBg, "MessageLabel", font,
            0.5f, 1f, 0.5f, 1f, new Vector2(0, -62), new Vector2(325, 75),
            20, TextAnchor.MiddleCenter, darkNavy);
        Text msgText = msgObj.GetComponent<Text>();

        // YesButton
        GameObject yesBtn = MkButton(popupBg, "YesButton", "YesLabel", "예", font,
            new Vector2(-87.5f, -162.5f), new Vector2(137.5f, 45f), dangerRed);

        // NoButton
        GameObject noBtn = MkButton(popupBg, "NoButton", "NoLabel", "아니오", font,
            new Vector2(87.5f, -162.5f), new Vector2(137.5f, 45f), neutralGray);

        confirmRoot.SetActive(false);

        // ── OptionPanel 컴포넌트 필드 연결 (Reflection) ──
        var op = optionPanel;
        SetPrivateField(op, "bgmToggle",     bgmToggle.GetComponent<Toggle>());
        SetPrivateField(op, "sfxToggle",     sfxToggle.GetComponent<Toggle>());
        SetPrivateField(op, "restartButton", restartBtn.GetComponent<Button>());
        SetPrivateField(op, "quitButton",    quitBtn.GetComponent<Button>());
        SetPrivateField(op, "closeButton",   closeBtn.GetComponent<Button>());
        SetPrivateField(op, "confirmPopup",  confirmComp);
        SetPrivateField(op, "titleLabel",    titleText);
        SetPrivateField(op, "bgmLabel",      bgmToggle.transform.Find("BGMLabel")?.GetComponent<Text>());
        SetPrivateField(op, "sfxLabel",      sfxToggle.transform.Find("SFXLabel")?.GetComponent<Text>());
        SetPrivateField(op, "restartLabel",  restartBtn.transform.Find("RestartLabel")?.GetComponent<Text>());
        SetPrivateField(op, "quitLabel",     quitBtn.transform.Find("QuitLabel")?.GetComponent<Text>());

        // ── ConfirmPopup 필드 연결 ──
        SetPrivateField(confirmComp, "messageLabel", msgText);
        SetPrivateField(confirmComp, "yesLabel",     yesBtn.transform.Find("YesLabel")?.GetComponent<Text>());
        SetPrivateField(confirmComp, "noLabel",      noBtn.transform.Find("NoLabel")?.GetComponent<Text>());
        SetPrivateField(confirmComp, "yesButton",    yesBtn.GetComponent<Button>());
        SetPrivateField(confirmComp, "noButton",     noBtn.GetComponent<Button>());

        // ── 프리팹 저장 ──
        EnsureDirectory(PREFAB_DIR);
        PrefabUtility.SaveAsPrefabAsset(root, PREFAB_PATH);
        Object.DestroyImmediate(root);
        Debug.Log("[OptionPanelPrefabCreator] 생성: " + PREFAB_PATH);

        // ── GameSettings에 자동 연결 ──
        if (gs != null)
        {
            gs.optionPanelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB_PATH);
            EditorUtility.SetDirty(gs);
            AssetDatabase.SaveAssets();
            Debug.Log("[OptionPanelPrefabCreator] GameSettings.optionPanelPrefab 자동 연결 완료!");
        }
        else
        {
            Debug.LogWarning("[OptionPanelPrefabCreator] GameSettings.asset 없음. 수동 연결 필요.");
        }

        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("완료", "OptionPanel.prefab 생성 완료!", "확인");
    }

    // ══════════════════════════════════════════════════════
    //  유틸리티
    // ══════════════════════════════════════════════════════

    private static GameObject MkChild(GameObject parent, string name,
        float anchorMinX, float anchorMinY, float anchorMaxX, float anchorMaxY,
        Vector2 anchoredPos, Vector2 sizeDelta)
    {
        var obj = new GameObject(name);
        obj.transform.SetParent(parent.transform, false);
        var rt = obj.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(anchorMinX, anchorMinY);
        rt.anchorMax = new Vector2(anchorMaxX, anchorMaxY);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = sizeDelta;
        return obj;
    }

    private static GameObject MkTextObj(GameObject parent, string name, Font font,
        float anchorMinX, float anchorMinY, float anchorMaxX, float anchorMaxY,
        Vector2 anchoredPos, Vector2 sizeDelta, int fontSize, TextAnchor alignment, Color color)
    {
        var obj = MkChild(parent, name,
            anchorMinX, anchorMinY, anchorMaxX, anchorMaxY, anchoredPos, sizeDelta);
        var t = obj.AddComponent<Text>();
        t.text = name;
        t.fontSize = fontSize;
        t.alignment = alignment;
        t.color = color;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        if (font != null) t.font = font;
        return obj;
    }

    private static void MkTextInObj(GameObject parent, string text, Font font,
        int fontSize, TextAnchor alignment, Color color)
    {
        var obj = new GameObject("Label");
        obj.transform.SetParent(parent.transform, false);
        var rt = obj.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        var t = obj.AddComponent<Text>();
        t.text = text;
        t.fontSize = fontSize;
        t.alignment = alignment;
        t.color = color;
        if (font != null) t.font = font;
    }

    /// <summary>Toggle 생성 (Background + Checkmark + Label)</summary>
    private static GameObject MkToggle(GameObject parent, string name, string labelName,
        string labelText, Font font, Vector2 anchoredPos)
    {
        var obj = MkChild(parent, name,
            0.5f, 1f, 0.5f, 1f, anchoredPos, new Vector2(350f, 50f));

        Image bg = obj.AddComponent<Image>();
        bg.color = toggleBg;

        Toggle toggle = obj.AddComponent<Toggle>();
        toggle.targetGraphic = bg;

        // Background
        var bgObj = MkChild(obj, "Background",
            0f, 0.5f, 0f, 0.5f, new Vector2(25, 0), new Vector2(35, 35));
        Image bgImg = bgObj.AddComponent<Image>();
        bgImg.color = checkBg;
        toggle.graphic = bgImg;

        // Checkmark
        var checkObj = MkChild(bgObj, "Checkmark",
            0.5f, 0.5f, 0.5f, 0.5f, Vector2.zero, new Vector2(25, 25));
        Image checkImg = checkObj.AddComponent<Image>();
        checkImg.color = checkMark;
        toggle.graphic = checkImg;

        // Label
        var labelObj = MkChild(obj, labelName,
            0f, 0f, 1f, 1f, new Vector2(25, 0), Vector2.zero);
        var labelRt = labelObj.GetComponent<RectTransform>();
        labelRt.offsetMin = new Vector2(62, 0);
        labelRt.offsetMax = Vector2.zero;
        Text labelT = labelObj.AddComponent<Text>();
        labelT.text = labelText;
        labelT.fontSize = 20;
        labelT.alignment = TextAnchor.MiddleLeft;
        labelT.color = darkNavy;
        if (font != null) labelT.font = font;

        return obj;
    }

    /// <summary>Button 생성 (Image + Button + Label Text)</summary>
    private static GameObject MkButton(GameObject parent, string name, string labelName,
        string labelText, Font font, Vector2 anchoredPos,
        Vector2 size = default, Color bgColor = default)
    {
        if (size == default) size = new Vector2(300f, 55f);
        if (bgColor == default) bgColor = accentBlue;

        var obj = MkChild(parent, name,
            0.5f, 1f, 0.5f, 1f, anchoredPos, size);

        Image bg = obj.AddComponent<Image>();
        bg.color = bgColor;

        Button btn = obj.AddComponent<Button>();
        btn.targetGraphic = bg;

        // Label
        var labelObj = new GameObject(labelName);
        labelObj.transform.SetParent(obj.transform, false);
        var labelRt = labelObj.AddComponent<RectTransform>();
        labelRt.anchorMin = Vector2.zero;
        labelRt.anchorMax = Vector2.one;
        labelRt.offsetMin = Vector2.zero;
        labelRt.offsetMax = Vector2.zero;
        Text labelT = labelObj.AddComponent<Text>();
        labelT.text = labelText;
        labelT.fontSize = 20;
        labelT.alignment = TextAnchor.MiddleCenter;
        labelT.color = Color.white;
        if (font != null) labelT.font = font;

        return obj;
    }

    private static void EnsureDirectory(string path)
    {
        if (!System.IO.Directory.Exists(path))
            System.IO.Directory.CreateDirectory(path);
    }

    private static void SetPrivateField(object target, string fieldName, object value)
    {
        if (target == null || value == null) return;
        var field = target.GetType().GetField(fieldName,
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (field != null)
            field.SetValue(target, value);
        else
            Debug.LogWarning($"[OptionPanelPrefabCreator] 필드 '{fieldName}' 없음 — Inspector에서 수동 연결 필요");
    }
}
#endif
