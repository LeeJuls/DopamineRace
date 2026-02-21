using UnityEngine;
using UnityEngine.UI;
using System.Collections;
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

    // ── 배팅 UI (프리팹 기반) ──
    private CharacterItemUI[] characterItems;
    private Transform bettingPanelRoot;
    private Button startButton;
    private Text scoreText;
    private Text roundText;
    private Text lapText;

    // 프리팹 UI 참조
    private Text betTitleText;
    private Text betRoundText;
    private Text betDescText;
    private Text oddsText;
    private Text pointsLabelText;
    private Text pointsFormulaText;
    private Text trackRoundLabel;
    private Text trackNameLabel;
    private Text distanceLabel;
    private Text trackTypeLabel;
    private Toggle hideInfoToggle;
    private CharacterInfoPopup charInfoPopup;

    // 배팅 타입 탭
    private Button[] betTypeBtns;
    private Text[] betTypeBtnTexts;
    private Image[] betTypeBtnBGs;
    private BetType currentTabType = BetType.Win;
    private int currentTabIndex = 0;

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
        // 다국어 초기화 (가장 먼저!)
        Loc.Init();

        // 폰트 설정: FontHelper가 GameSettings → OS fallback 처리
        font = FontHelper.GetUIFontWithFallback();

        // ═══ 매니저 초기화 순서 (변경 시 주의!) ═══
        // 1단계: 데이터 (다른 매니저가 참조)
        // 2단계: 코어 로직 (데이터에 의존)
        // 3단계: 유틸리티 (코어에 의존)
        //
        // AddComponent → 즉시 Awake() → Instance 설정
        // Start()는 다음 프레임에 호출 → 이 시점에 모든 Instance 보장

        // 1단계: 데이터
        if (CharacterDatabase.Instance == null)
            new GameObject("CharacterDatabase").AddComponent<CharacterDatabase>();
        if (TrackDatabase.Instance == null)
            new GameObject("TrackDatabase").AddComponent<TrackDatabase>();

        // 2단계: 코어 로직
        if (GameManager.Instance == null)
            new GameObject("GameManager").AddComponent<GameManager>();
        if (RaceManager.Instance == null)
            new GameObject("RaceManager").AddComponent<RaceManager>();
        if (ScoreManager.Instance == null)
            new GameObject("ScoreManager").AddComponent<ScoreManager>();

        // 3단계: 유틸리티
        if (TrackTransition.Instance == null)
            new GameObject("TrackTransition").AddComponent<TrackTransition>();
        if (BGMManager.Instance == null)
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
            if (raceTimerText != null) raceTimerText.text = Loc.Get("str.hud.timer", raceTimer.ToString("F1"));

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
        canvas.pixelPerfect = true;
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
                var gm = GameManager.Instance;
                if (gm != null && gm.CurrentRound > 1)
                {
                    // 2라운드부터: 새 트랙 잠깐 보여준 후 배팅 UI 표시
                    StartCoroutine(DelayedBettingUI());
                }
                else
                {
                    // 1라운드: 즉시 표시
                    bettingUI.SetActive(true);
                    ResetBetting();
                    UpdateRoundInfo();
                    UpdateScore();
                }
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

    /// <summary>
    /// 2라운드부터: 새 트랙을 잠깐 보여준 뒤 배팅 UI 표시
    /// </summary>
    private IEnumerator DelayedBettingUI()
    {
        // 레이싱 씬(새 트랙)이 잠깐 보이도록 UI를 숨긴 채 대기
        UpdateRoundInfo();
        UpdateScore();
        yield return new WaitForSeconds(1.5f);

        // 배팅 UI 표시
        bettingUI.SetActive(true);
        ResetBetting();
        UpdateRoundInfo();
        UpdateScore();
    }

    private void OnCountdownTick(int t)
    {
        if (countdownText != null)
            countdownText.text = t > 0 ? t.ToString() : "GO!";
    }
}