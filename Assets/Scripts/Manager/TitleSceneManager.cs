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

    // 언어 버튼
    private Button[] langButtons;
    private Image[] langBtnImages;
    private string[] langCodes = { "ko", "en", "jp" };
    private string[] langLabels = { "한국어", "EN", "日本語" };

    // ══════════════════════════════════════
    //  상수
    // ══════════════════════════════════════
    private const float BG_PPU = 70f;
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

        // 화면 상단~중앙 (뷰포트 y=0.65)
        if (cam != null)
        {
            Vector3 pos = cam.ViewportToWorldPoint(new Vector3(0.5f, 0.65f, 10f));
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
    //  언어 선택 UI
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

        // 우하단 앵커 컨테이너
        GameObject container = new GameObject("LangContainer");
        container.transform.SetParent(canvasObj.transform, false);
        RectTransform crt = container.AddComponent<RectTransform>();
        crt.anchorMin = new Vector2(1f, 0f);
        crt.anchorMax = new Vector2(1f, 0f);
        crt.pivot = new Vector2(1f, 0f);
        crt.anchoredPosition = new Vector2(-30f, 30f);
        crt.sizeDelta = new Vector2(300f, 50f);

        HorizontalLayoutGroup hlg = container.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 10f;
        hlg.childAlignment = TextAnchor.MiddleRight;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = true;

        Font font = FontHelper.GetUIFontWithFallback();

        langButtons = new Button[langCodes.Length];
        langBtnImages = new Image[langCodes.Length];

        for (int i = 0; i < langCodes.Length; i++)
        {
            GameObject btnObj = new GameObject("Btn_" + langCodes[i]);
            btnObj.transform.SetParent(container.transform, false);

            Image btnImg = btnObj.AddComponent<Image>();
            btnImg.color = new Color(0.2f, 0.2f, 0.2f, 0.7f);
            langBtnImages[i] = btnImg;

            Button btn = btnObj.AddComponent<Button>();
            langButtons[i] = btn;

            LayoutElement le = btnObj.AddComponent<LayoutElement>();
            le.preferredWidth = 90f;
            le.preferredHeight = 40f;

            // 텍스트
            GameObject txtObj = new GameObject("Text");
            txtObj.transform.SetParent(btnObj.transform, false);
            Text txt = txtObj.AddComponent<Text>();
            txt.text = langLabels[i];
            txt.font = font;
            txt.fontSize = 22;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = Color.white;

            RectTransform trt = txt.rectTransform;
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.offsetMin = Vector2.zero;
            trt.offsetMax = Vector2.zero;

            // 클릭 이벤트
            int idx = i;
            btn.onClick.AddListener(() => OnLanguageClick(idx));
        }

        UpdateLanguageHighlight();
    }

    private void OnLanguageClick(int idx)
    {
        if (idx < 0 || idx >= langCodes.Length) return;
        Loc.SetLang(langCodes[idx]);
        UpdateLanguageHighlight();
        RefreshTexts();
        Debug.Log("[TitleScene] 언어 변경: " + langCodes[idx]);
    }

    private void UpdateLanguageHighlight()
    {
        for (int i = 0; i < langCodes.Length; i++)
        {
            bool selected = (langCodes[i] == Loc.CurrentLang);
            if (langBtnImages[i] != null)
            {
                langBtnImages[i].color = selected
                    ? new Color(0.9f, 0.6f, 0.1f, 0.9f)   // 선택: 주황
                    : new Color(0.2f, 0.2f, 0.2f, 0.7f);   // 미선택: 어두운 회색
            }
        }
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
