using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// 타이틀 씬 전체 관리
/// Phase 1: 씬 전환 기반
/// Phase 2: 배경/로고/PressToStart/언어선택
/// Phase 3: 캐릭터 달리기 (TitleCharacterRunner)
/// </summary>
public class TitleSceneManager : MonoBehaviour
{
    // ══════════════════════════════════════
    //  상태
    // ══════════════════════════════════════
    private bool isTransitioning = false;
    private float inputDelay = 0.5f;
    private float elapsedTime = 0f;

    // ══════════════════════════════════════
    //  비주얼 참조
    // ══════════════════════════════════════
    private Camera cam;
    private GameObject bgObj;
    private GameObject logoObj;
    private TextMesh pressText;
    private float blinkTimer = 0f;
    private TitleCharacterRunner characterRunner;

    // 언어 드롭다운
    private string[] langCodes = { "br", "es", "de", "cn", "en", "jp", "ko" };
    private string[] langDisplayCodes = { "BR", "ES", "DE", "CN", "EN", "JP", "KR" };
    private GameObject langOverlay;
    private GameObject langDropdownPanel;
    private Text langMainText;
    private Image[] langDropdownImages;
    private bool isLangDropdownOpen = false;

    // ══════════════════════════════════════
    //  상수
    // ══════════════════════════════════════
    private const float BG_PPU = 70f;
    private const float LOGO_SCALE = 0.75f; // ★ 타이틀 로고 크기 (1.0 = 기본, 1.5 = 150%, 0.8 = 80%)
    private const float LOGO_Y = 0.65f;     // ★ 타이틀 로고 Y 위치 (뷰포트 비율: 0=하단, 1=상단)
    private const float BLINK_SPEED = 3.5f; // 약 1.1초 주기

    private void Start()
    {
        // 다국어 초기화
        Loc.Init();

        // 카메라 설정
        cam = Camera.main;
        if (cam != null)
        {
            cam.orthographic = true;
            cam.orthographicSize = 5.5f;
            cam.transform.position = new Vector3(0, 0, -10);
            cam.backgroundColor = Color.black;
            cam.clearFlags = CameraClearFlags.SolidColor;

            // AudioListener 확보 (BGM 재생에 필수)
            if (cam.GetComponent<AudioListener>() == null)
                cam.gameObject.AddComponent<AudioListener>();
        }

        // 매니저 초기화
        EnsureCharacterDatabase();
        EnsureSceneTransitionManager();
        EnsureBGMManager();

        // 비주얼 구성
        CreateBackground();
        CreateTitleLogo();
        CreatePressToStart();
        CreateLanguageUI();

        // 캐릭터 달리기
        StartCharacterRunner();

        // 타이틀 BGM 재생
        if (BGMManager.Instance != null)
        {
            BGMManager.Instance.PlayBGM("Audio/Title_Bgm");
            BGMManager.Instance.SetVolume(0f);
            float bgmVol = GameSettings.Instance != null ? GameSettings.Instance.bgmVolume : 0.5f;
            BGMManager.Instance.FadeIn(bgmVol, 1.5f);
        }

        Debug.Log("[TitleScene] 타이틀 씬 시작");
    }

    private void Update()
    {
        // Press to Start 깜빡임
        if (pressText != null && !isTransitioning)
        {
            blinkTimer += Time.deltaTime * BLINK_SPEED;
            float alpha = Mathf.Lerp(0.3f, 1.0f, (Mathf.Sin(blinkTimer) + 1f) * 0.5f);
            pressText.color = new Color(1f, 1f, 1f, alpha);
        }

        if (isTransitioning) return;

        elapsedTime += Time.deltaTime;
        if (elapsedTime < inputDelay) return;

        // 아무 입력 감지 (UI 버튼 위 클릭은 무시)
        if (IsPointerOverUI()) return;

        if (Input.anyKeyDown || Input.GetMouseButtonDown(0) || HasTouchBegan())
        {
            StartTransition();
        }
    }

    // ══════════════════════════════════════
    //  배경 이미지
    // ══════════════════════════════════════
    private void CreateBackground()
    {
        Texture2D tex = Resources.Load<Texture2D>("BG/main_title_bg");
        if (tex == null)
        {
            Debug.LogWarning("[TitleScene] main_title_bg 이미지 없음");
            return;
        }

        tex.filterMode = FilterMode.Point;

        bgObj = new GameObject("Background");
        SpriteRenderer sr = bgObj.AddComponent<SpriteRenderer>();
        sr.sprite = Sprite.Create(tex,
            new Rect(0, 0, tex.width, tex.height),
            new Vector2(0.5f, 0.5f), BG_PPU);
        sr.sortingOrder = -10;

        // Cover 방식: 카메라 전체 채움
        FitToCameraCover(bgObj, sr.sprite);
    }

    // ══════════════════════════════════════
    //  타이틀 로고
    // ══════════════════════════════════════
    private void CreateTitleLogo()
    {
        Texture2D tex = Resources.Load<Texture2D>("BG/main_title");
        if (tex == null)
        {
            Debug.LogWarning("[TitleScene] main_title 이미지 없음");
            return;
        }

        tex.filterMode = FilterMode.Point;

        logoObj = new GameObject("TitleLogo");
        SpriteRenderer sr = logoObj.AddComponent<SpriteRenderer>();
        sr.sprite = Sprite.Create(tex,
            new Rect(0, 0, tex.width, tex.height),
            new Vector2(0.5f, 0.5f), BG_PPU);
        sr.sortingOrder = 5;

        // 로고 크기 조절 (LOGO_SCALE 값 수정으로 조절)
        logoObj.transform.localScale = new Vector3(LOGO_SCALE, LOGO_SCALE, 1f);

        // 화면 위치 (LOGO_Y 값 수정으로 조절)
        if (cam != null)
        {
            Vector3 pos = cam.ViewportToWorldPoint(new Vector3(0.5f, LOGO_Y, 10f));
            logoObj.transform.position = new Vector3(pos.x, pos.y, 0f);
        }
    }

    // ══════════════════════════════════════
    //  "Press to Start" 텍스트
    // ══════════════════════════════════════
    private void CreatePressToStart()
    {
        GameObject textObj = new GameObject("PressToStart");
        pressText = textObj.AddComponent<TextMesh>();

        pressText.text = Loc.Get("str.ui.press_start");
        pressText.fontSize = 60;
        pressText.characterSize = 0.15f;
        pressText.anchor = TextAnchor.MiddleCenter;
        pressText.alignment = TextAlignment.Center;
        pressText.color = Color.white;

        FontHelper.ApplyToTextMesh(pressText, 10);

        // 화면 중하단 (뷰포트 y=0.25)
        if (cam != null)
        {
            Vector3 pos = cam.ViewportToWorldPoint(new Vector3(0.5f, 0.25f, 10f));
            textObj.transform.position = new Vector3(pos.x, pos.y, 0f);
        }
    }

    // ══════════════════════════════════════
    //  언어 선택 UI (드롭다운 방식)
    // ══════════════════════════════════════
    private void CreateLanguageUI()
    {
        // Canvas 생성
        GameObject canvasObj = new GameObject("LangCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200;

        CanvasScaler sc = canvasObj.AddComponent<CanvasScaler>();
        sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        sc.referenceResolution = new Vector2(1920, 1080);
        sc.matchWidthOrHeight = 0.5f;

        canvasObj.AddComponent<GraphicRaycaster>();

        Font font = FontHelper.GetUIFontWithFallback();

        // ── 전체화면 오버레이 (외부 클릭 시 드롭다운 닫기) ──
        langOverlay = new GameObject("LangOverlay");
        langOverlay.transform.SetParent(canvasObj.transform, false);
        Image overlayImg = langOverlay.AddComponent<Image>();
        overlayImg.color = new Color(0, 0, 0, 0.01f); // 거의 투명하지만 레이캐스트 수신
        RectTransform ort = overlayImg.rectTransform;
        ort.anchorMin = Vector2.zero;
        ort.anchorMax = Vector2.one;
        ort.offsetMin = Vector2.zero;
        ort.offsetMax = Vector2.zero;
        Button overlayBtn = langOverlay.AddComponent<Button>();
        overlayBtn.transition = Selectable.Transition.None;
        overlayBtn.onClick.AddListener(CloseLangDropdown);
        langOverlay.SetActive(false);

        // ── 드롭다운 패널 (언어 목록) ──
        langDropdownPanel = new GameObject("LangDropdown");
        langDropdownPanel.transform.SetParent(canvasObj.transform, false);
        RectTransform drt = langDropdownPanel.AddComponent<RectTransform>();
        drt.anchorMin = new Vector2(1f, 0f);
        drt.anchorMax = new Vector2(1f, 0f);
        drt.pivot = new Vector2(1f, 0f);
        drt.anchoredPosition = new Vector2(-30f, 110f); // 드롭다운 버튼 위치 메인 버튼 바로 위

        // 배경
        Image dropBg = langDropdownPanel.AddComponent<Image>();
        dropBg.color = new Color(0.1f, 0.1f, 0.1f, 0.9f);

        // 레이아웃
        VerticalLayoutGroup vlg = langDropdownPanel.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 6f; // 버튼 간격
        vlg.padding = new RectOffset(8, 8, 8, 8);
        vlg.childAlignment = TextAnchor.MiddleCenter;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        ContentSizeFitter csf = langDropdownPanel.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        csf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;

        // 드롭다운 버튼 생성 (위→아래: BR, ES, DE, CN, EN, JP, KR)
        langDropdownImages = new Image[langCodes.Length];
        for (int i = 0; i < langCodes.Length; i++)
        {
            CreateDropdownButton(langDropdownPanel.transform, i, font);
        }

        langDropdownPanel.SetActive(false);

        // ── 메인 버튼 (현재 언어 코드 표시) ──
        GameObject mainBtnObj = new GameObject("LangMainBtn");
        mainBtnObj.transform.SetParent(canvasObj.transform, false);

        Image mainBtnImg = mainBtnObj.AddComponent<Image>();
        mainBtnImg.color = new Color(0.2f, 0.2f, 0.2f, 0.85f); // 번역 버튼 배경색

        Button mainBtn = mainBtnObj.AddComponent<Button>();
        mainBtn.onClick.AddListener(ToggleLangDropdown);

        RectTransform mrt = mainBtnImg.rectTransform;
        mrt.anchorMin = new Vector2(1f, 0f);
        mrt.anchorMax = new Vector2(1f, 0f);
        mrt.pivot = new Vector2(1f, 0f);
        mrt.anchoredPosition = new Vector2(-30f, 30f); // 번역 버튼 위치
        mrt.sizeDelta = new Vector2(120f, 72f); // 번역 버튼 크기

        // 메인 버튼 텍스트
        GameObject mainTxtObj = new GameObject("Text");
        mainTxtObj.transform.SetParent(mainBtnObj.transform, false);
        langMainText = mainTxtObj.AddComponent<Text>();
        langMainText.font = font;
        langMainText.fontSize = 48; // 번역 메인 버튼 크기
        langMainText.alignment = TextAnchor.MiddleCenter;
        langMainText.color = Color.white;
        // langMainText.fontStyle = FontStyle.Bold;

        RectTransform trt = langMainText.rectTransform;
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = Vector2.zero;
        trt.offsetMax = Vector2.zero;

        UpdateLangMainBtnText();
        UpdateLanguageHighlight();
    }

    private void CreateDropdownButton(Transform parent, int langIdx, Font font)
    {
        GameObject btnObj = new GameObject("Btn_" + langCodes[langIdx]);
        btnObj.transform.SetParent(parent, false);

        Image btnImg = btnObj.AddComponent<Image>();
        btnImg.color = new Color(0.25f, 0.25f, 0.25f, 0.9f);
        langDropdownImages[langIdx] = btnImg;

        Button btn = btnObj.AddComponent<Button>();
        int idx = langIdx;
        btn.onClick.AddListener(() => OnLanguageClick(idx));

        LayoutElement le = btnObj.AddComponent<LayoutElement>();
        le.preferredWidth = 100f; // 언어 버튼 너비
        le.preferredHeight = 60f; // 언어 버튼 높이

        // 텍스트
        GameObject txtObj = new GameObject("Text");
        txtObj.transform.SetParent(btnObj.transform, false);
        Text txt = txtObj.AddComponent<Text>();
        txt.text = langDisplayCodes[langIdx];
        txt.font = font;
        txt.fontSize = 40; // ★ 언어 버튼 텍스트 크기
        txt.alignment = TextAnchor.MiddleCenter;
        txt.color = Color.white;

        RectTransform trt = txt.rectTransform;
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = Vector2.zero;
        trt.offsetMax = Vector2.zero;
    }

    private void OnLanguageClick(int idx)
    {
        if (idx < 0 || idx >= langCodes.Length) return;
        Loc.SetLang(langCodes[idx]);
        UpdateLanguageHighlight();
        UpdateLangMainBtnText();
        RefreshTexts();
        CloseLangDropdown();
        Debug.Log("[TitleScene] 언어 변경: " + langCodes[idx]);
    }

    private void ToggleLangDropdown()
    {
        if (isLangDropdownOpen)
            CloseLangDropdown();
        else
            OpenLangDropdown();
    }

    private void OpenLangDropdown()
    {
        isLangDropdownOpen = true;
        langOverlay.SetActive(true);
        langDropdownPanel.SetActive(true);
        UpdateLanguageHighlight();
    }

    private void CloseLangDropdown()
    {
        isLangDropdownOpen = false;
        if (langOverlay != null) langOverlay.SetActive(false);
        if (langDropdownPanel != null) langDropdownPanel.SetActive(false);
    }

    private void UpdateLanguageHighlight()
    {
        if (langDropdownImages == null) return;
        for (int i = 0; i < langCodes.Length; i++)
        {
            if (langDropdownImages[i] == null) continue;
            bool selected = (langCodes[i] == Loc.CurrentLang);
            langDropdownImages[i].color = selected
                ? new Color(0.9f, 0.6f, 0.1f, 0.95f)   // 선택: 주황
                : new Color(0.25f, 0.25f, 0.25f, 0.9f); // 미선택: 어두운 회색
        }
    }

    private void UpdateLangMainBtnText()
    {
        if (langMainText == null) return;
        int idx = System.Array.IndexOf(langCodes, Loc.CurrentLang);
        langMainText.text = idx >= 0 ? langDisplayCodes[idx] : Loc.CurrentLang.ToUpper();
    }

    private void RefreshTexts()
    {
        if (pressText != null)
            pressText.text = Loc.Get("str.ui.press_start");
    }

    // ══════════════════════════════════════
    //  DB 초기화
    // ══════════════════════════════════════
    private void EnsureCharacterDatabase()
    {
        if (CharacterDatabase.Instance != null) return;
        GameObject dbObj = new GameObject("CharacterDatabase");
        dbObj.AddComponent<CharacterDatabase>();
    }

    private void EnsureSceneTransitionManager()
    {
        if (SceneTransitionManager.Instance != null) return;
        GameObject stmObj = new GameObject("SceneTransitionManager");
        stmObj.AddComponent<SceneTransitionManager>();
    }

    private void EnsureBGMManager()
    {
        if (BGMManager.Instance != null) return;
        GameObject bgmObj = new GameObject("BGMManager");
        bgmObj.AddComponent<BGMManager>();
    }

    // ══════════════════════════════════════
    //  캐릭터 달리기
    // ══════════════════════════════════════
    private void StartCharacterRunner()
    {
        GameObject runnerObj = new GameObject("CharacterRunner");
        characterRunner = runnerObj.AddComponent<TitleCharacterRunner>();
        characterRunner.Begin(cam);
    }

    // ══════════════════════════════════════
    //  유틸리티
    // ══════════════════════════════════════

    /// <summary>
    /// SpriteRenderer를 카메라 뷰에 Cover 방식으로 맞춤
    /// </summary>
    private void FitToCameraCover(GameObject go, Sprite sprite)
    {
        if (cam == null || sprite == null) return;

        float camH = cam.orthographicSize * 2f;
        float camW = camH * cam.aspect;
        float sprH = sprite.bounds.size.y;
        float sprW = sprite.bounds.size.x;

        float scale = Mathf.Max(camW / sprW, camH / sprH);
        go.transform.localScale = new Vector3(scale, scale, 1f);
        go.transform.position = Vector3.zero;
    }

    // ══════════════════════════════════════
    //  입력 + 전환
    // ══════════════════════════════════════
    private bool HasTouchBegan()
    {
        return Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began;
    }

    /// <summary>
    /// 마우스/터치가 UI 위에 있는지 확인 (언어 버튼 등)
    /// </summary>
    private bool IsPointerOverUI()
    {
        if (EventSystem.current == null) return false;

        // 마우스
        if (EventSystem.current.IsPointerOverGameObject())
            return true;

        // 터치
        if (Input.touchCount > 0 && EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId))
            return true;

        return false;
    }

    private void StartTransition()
    {
        isTransitioning = true;

        // 언어 드롭다운 닫기
        CloseLangDropdown();

        // Press to Start 숨기기
        if (pressText != null)
            pressText.gameObject.SetActive(false);

        // 캐릭터 달리기 정지
        if (characterRunner != null)
            characterRunner.StopAll();

        // BGM 페이드아웃
        if (BGMManager.Instance != null)
            BGMManager.Instance.FadeOut(1.5f);

        Debug.Log("[TitleScene] 전환 시작 → SampleScene");

        // 블록 디졸브 전환 사용
        if (SceneTransitionManager.Instance != null)
        {
            SceneTransitionManager.Instance.TransitionToScene("SampleScene");
        }
        else
        {
            // 폴백: 직접 로드
            StartCoroutine(LoadSampleSceneDirect());
        }
    }

    private IEnumerator LoadSampleSceneDirect()
    {
        AsyncOperation op = SceneManager.LoadSceneAsync("SampleScene");
        op.allowSceneActivation = true;
        while (!op.isDone)
            yield return null;
    }
}
