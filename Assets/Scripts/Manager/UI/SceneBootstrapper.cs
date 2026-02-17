using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// 도파민 경마 - 메인 UI 컨트롤러
/// partial class로 기능별 분리:
///   .cs             → Core (변수, 초기화, 상태전환)
///   .Betting.cs     → 배팅 패널 + 탭 + 선택 로직
///   .Racing.cs      → 레이싱 HUD + 실시간 순위
///   .Result.cs      → 라운드 결과 화면
///   .Finish.cs      → 최종 결산 화면
///   .Leaderboard.cs → 리더보드 팝업
///   .Utilities.cs   → 공용 유틸리티 (MkText 등)
/// </summary>
public partial class SceneBootstrapper : MonoBehaviour
{
    // ══════════════════════════════════════
    //  공용 참조
    // ══════════════════════════════════════
    private Font font;

    // UI 루트 오브젝트
    private GameObject bettingUI;
    private GameObject racingUI;
    private GameObject resultUI;
    private GameObject countdownUI;
    private GameObject finishUI;
    private GameObject leaderboardPopup;

    // ── 배팅 UI ──
    private Button[] racerButtons;
    private Text[] racerTexts;
    private Text[] racerLabels;
    private Image[] racerBGs;
    private Image[] racerIcons;
    private RectTransform racerScrollContent;
    private Button startButton;
    private Text titleText;
    private Text infoText;
    private Text scoreText;
    private Text roundText;
    private Text lapText;
    private GameObject leftPanelObj;
    private Text toggleBtnText;

    // 배팅 타입 탭
    private Button[] betTypeBtns;
    private Text[] betTypeBtnTexts;
    private Image[] betTypeBtnBGs;
    private BetType currentTabType = BetType.Exacta;

    // ── 레이싱 UI ──
    private Text countdownText;
    private Text raceTimerText;
    private Text[] rankTexts;
    private Text myBetText;
    private Text racingRoundText;

    // ── 결과 UI ──
    private Text resultTitleText;
    private Text resultDetailText;
    private Text resultScoreText;
    private Button nextRoundButton;
    private Text nextRoundBtnText;

    // ── Finish UI ──
    private Text finishTitleText;
    private Text finishRoundDetailText;
    private Text finishTotalScoreText;

    // ── 리더보드 ──
    private Text leaderboardContentText;
    private Text leaderboardTitleText;

    // ── 런타임 ──
    private float raceTimer;
    private float rankUpdateTimer;
    private List<GameObject> betArrows = new List<GameObject>();

    // ══════════════════════════════════════
    //  초기화
    // ══════════════════════════════════════
    private void Awake()
    {
        if (GameManager.Instance != null) return;

        font = Font.CreateDynamicFontFromOSFont("Malgun Gothic", 24);
        if (font == null) font = Font.CreateDynamicFontFromOSFont("Arial", 24);

        new GameObject("GameManager").AddComponent<GameManager>();
        new GameObject("RaceManager").AddComponent<RaceManager>();
        new GameObject("ScoreManager").AddComponent<ScoreManager>();
        new GameObject("CharacterDatabase").AddComponent<CharacterDatabase>();
        new GameObject("TrackDatabase").AddComponent<TrackDatabase>();
        new GameObject("TrackTransition").AddComponent<TrackTransition>();
        new GameObject("BGMManager").AddComponent<BGMManager>();

        Camera cam = Camera.main;
        if (cam != null)
        {
            cam.orthographic = true;
            cam.orthographicSize = 5.5f;
            cam.transform.position = new Vector3(2.5f, 0, -10);
            cam.backgroundColor = new Color(0.10f, 0.10f, 0.35f);
        }

        GameObject track = new GameObject("Track");
        track.AddComponent<TrackVisualizer>();
        track.AddComponent<WaypointEditor>();
        track.AddComponent<TrackDebugPath>();
        track.AddComponent<SpawnEditor>();

        BuildUI();
        Debug.Log("도파민 경마 - 씬 구성 완료!");
    }

    private void Start()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnStateChanged += OnStateChanged;
            GameManager.Instance.OnCountdownTick += OnCountdownTick;
        }
        OnStateChanged(GameManager.GameState.Betting);
        StartCoroutine(HideLabelsNextFrame());
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnStateChanged -= OnStateChanged;
            GameManager.Instance.OnCountdownTick -= OnCountdownTick;
        }
    }

    private void Update()
    {
        if (GameManager.Instance == null) return;
        if (GameManager.Instance.CurrentState == GameManager.GameState.Racing)
        {
            raceTimer += Time.deltaTime;
            if (raceTimerText != null) raceTimerText.text = raceTimer.ToString("F1") + "초";

            rankUpdateTimer -= Time.deltaTime;
            if (rankUpdateTimer <= 0f)
            {
                UpdateLiveRankings();
                rankUpdateTimer = 0.3f;
            }
            UpdateArrowPositions();
        }
    }

    // ══════════════════════════════════════
    //  UI 구축 (각 partial에 위임)
    // ══════════════════════════════════════
    private void BuildUI()
    {
        GameObject canvasObj = new GameObject("Canvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        CanvasScaler sc = canvasObj.AddComponent<CanvasScaler>();
        sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        sc.referenceResolution = new Vector2(1920, 1080);
        sc.matchWidthOrHeight = 0.5f;

        canvasObj.AddComponent<GraphicRaycaster>();

        if (FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject es = new GameObject("EventSystem");
            es.AddComponent<UnityEngine.EventSystems.EventSystem>();
            es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        Transform root = canvasObj.transform;

        bettingUI = new GameObject("BettingUI");
        bettingUI.transform.SetParent(root, false);
        AddFullRect(bettingUI);
        BuildBettingUI(bettingUI.transform);

        racingUI = new GameObject("RacingUI");
        racingUI.transform.SetParent(root, false);
        AddFullRect(racingUI);
        BuildRacingUI(racingUI.transform);
        racingUI.SetActive(false);

        resultUI = new GameObject("ResultUI");
        resultUI.transform.SetParent(root, false);
        AddFullRect(resultUI);
        BuildResultUI(resultUI.transform);
        resultUI.SetActive(false);

        countdownUI = new GameObject("Countdown");
        countdownUI.transform.SetParent(root, false);
        AddFullRect(countdownUI);
        countdownUI.AddComponent<Image>().color = new Color(0, 0, 0, 0.6f);
        countdownText = MkText(countdownUI.transform, "3",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(300, 200), 130, TextAnchor.MiddleCenter, new Color(1f, 0.85f, 0.2f));
        countdownUI.SetActive(false);

        finishUI = new GameObject("FinishUI");
        finishUI.transform.SetParent(root, false);
        AddFullRect(finishUI);
        BuildFinishUI(finishUI.transform);
        finishUI.SetActive(false);

        leaderboardPopup = new GameObject("LeaderboardPopup");
        leaderboardPopup.transform.SetParent(root, false);
        AddFullRect(leaderboardPopup);
        BuildLeaderboardPopup(leaderboardPopup.transform);
        leaderboardPopup.SetActive(false);
    }

    // ══════════════════════════════════════
    //  상태 전환
    // ══════════════════════════════════════
    private void OnStateChanged(GameManager.GameState state)
    {
        bettingUI.SetActive(false);
        racingUI.SetActive(false);
        resultUI.SetActive(false);
        countdownUI.SetActive(false);
        finishUI.SetActive(false);

        switch (state)
        {
            case GameManager.GameState.Betting:
                bettingUI.SetActive(true);
                ResetBetting();
                UpdateRoundInfo();
                UpdateScore();
                break;
            case GameManager.GameState.Countdown:
                countdownUI.SetActive(true);
                HideAllRaceLabels();
                break;
            case GameManager.GameState.Racing:
                racingUI.SetActive(true);
                raceTimer = 0f;
                UpdateMyBet();
                UpdateRacingRoundInfo();
                HideAllRaceLabels();
                break;
            case GameManager.GameState.Result:
                DestroyArrows();
                ShowAllRaceLabels();
                resultUI.SetActive(true);
                ShowResult();
                break;
            case GameManager.GameState.Finish:
                DestroyArrows();
                ShowAllRaceLabels();
                finishUI.SetActive(true);
                ShowFinish();
                break;
        }
    }

    private void OnCountdownTick(int t)
    {
        if (countdownText != null)
            countdownText.text = t > 0 ? t.ToString() : "GO!";
    }
}