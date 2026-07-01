using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

/// <summary>
/// 자동 반복 레이스 테스트 도구
/// Play Mode에서 N배속으로 M회 자동 반복 + 로그 저장
/// </summary>
public class AutoRaceRunnerWindow : EditorWindow
{
    // ═══ 설정 ═══
    private int iterations = 10;
    private float timeScale = 10f;
    private bool saveClaudeLog = true;  // Claude용 컴팩트 요약 로그 저장

    // ── Phase 0: 헤드리스 반복 스윕 (Play 불필요·백그라운드 안전 — RaceBacktestWindow 동기 루프 호출) ──
    private int headlessRacesPerLap = 800;
    private int headlessRacers = 8;
    private int headlessSeed = 73101;
    private string headlessResult = "";

    // ── Phase 1: 스톨 감지 (실시간 기준, force-complete 안 함) ──
    private double lastProgressTime = 0;   // EditorApplication.timeSinceStartup (마지막 상태전환 시각)
    private bool gameOverStopped = false;  // GameOver(젤리소진)로 중단됨

    // ═══ 상태 ═══
    private bool isRunning;
    private bool isSubscribed;
    private int currentGame;        // 0-based
    private int pendingFrames;      // 다음 프레임 대기 카운터
    private Action pendingAction;   // 대기 중인 액션

    // ═══ 로그 ═══
    private List<GameIterationLog> gameLogs = new List<GameIterationLog>();
    private GameIterationLog currentGameLog;
    private Vector2 logScroll;
    private Vector2 statsScroll;
    private StringBuilder displayLog = new StringBuilder();
    private string savedLogPath;
    private string currentTestTimestamp;  // test_YYMMDDHHmmss — 양쪽 파일 공유

    // ═══ 집계 ═══
    private bool showStats;

    [MenuItem("DopamineRace/자동 레이스 테스트")]
    public static void ShowWindow()
    {
        var w = GetWindow<AutoRaceRunnerWindow>("자동 레이스 테스트");
        w.minSize = new Vector2(420, 500);
    }

    private void OnEnable()
    {
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
    }

    private void OnDisable()
    {
        EditorApplication.playModeStateChanged -= OnPlayModeChanged;
        Unsubscribe();
    }

    private void OnPlayModeChanged(PlayModeStateChange change)
    {
        if (change == PlayModeStateChange.ExitingPlayMode && isRunning)
        {
            StopAndSave();
        }
    }

    // ═══════════════════════════════════════
    //  GUI
    // ═══════════════════════════════════════

    private void OnGUI()
    {
        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("자동 레이스 테스트", EditorStyles.boldLabel);
        EditorGUILayout.Space(2);

        // ── 설정 ──
        using (new EditorGUI.DisabledScope(isRunning))
        {
            iterations = EditorGUILayout.IntSlider("반복 횟수", iterations, 1, 200);
            timeScale = EditorGUILayout.Slider("배속", timeScale, 1f, 20f);
            saveClaudeLog = EditorGUILayout.Toggle("Claude 요약 로그 저장", saveClaudeLog);
        }

        EditorGUILayout.Space(4);

        // ── 시작/중지 ──
        using (new EditorGUILayout.HorizontalScope())
        {
            if (!EditorApplication.isPlaying)
            {
                EditorGUILayout.HelpBox("Play Mode에서만 사용 가능", MessageType.Info);
            }
            else if (!isRunning)
            {
                if (GUILayout.Button("▶ 시작", GUILayout.Height(30)))
                    StartAutoRace();
            }
            else
            {
                if (GUILayout.Button("■ 중지", GUILayout.Height(30)))
                    StopAndSave();
            }
        }

        EditorGUILayout.Space(6);

        // ── ⚡ 헤드리스 반복 스윕 (Play 불필요·백그라운드 OK·재현성 보장) ──
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.LabelField("⚡ 헤드리스 반복 스윕 (Play 불필요·백그라운드 OK)", EditorStyles.boldLabel);
            using (new EditorGUI.DisabledScope(isRunning))
            {
                headlessRacesPerLap = Mathf.Max(1, EditorGUILayout.IntField("랩당 레이스 수", headlessRacesPerLap));
                headlessRacers      = Mathf.Clamp(EditorGUILayout.IntField("출전 수", headlessRacers), 2, 12);
                headlessSeed        = EditorGUILayout.IntField("시드", headlessSeed);
                EditorGUILayout.LabelField("랩: 2·3·4·5·6 고정 (거리별)", EditorStyles.miniLabel);

                if (GUILayout.Button("⚡ 헤드리스 스윕 실행 (동기 — 백그라운드에서도 완주)", GUILayout.Height(26)))
                {
                    headlessResult = RaceBacktestWindow.RunLapSweepHeadless(
                        headlessRacesPerLap, new[] { 2, 3, 4, 5, 6 }, headlessRacers, headlessSeed);
                    Debug.Log("[AutoRace] 헤드리스 스윕 완료\n" + headlessResult);
                }
            }
            if (!string.IsNullOrEmpty(headlessResult))
            {
                int idx = headlessResult.LastIndexOf("saved");
                EditorGUILayout.HelpBox(idx >= 0 ? headlessResult.Substring(idx) : "완료 (상세는 Console)", MessageType.Info);
            }
        }

        EditorGUILayout.Space(4);

        // ── 진행 상태 ──
        if (isRunning)
        {
            var gm = GameManager.Instance;
            string stateStr = gm != null ? gm.CurrentState.ToString() : "—";
            int round = gm != null ? gm.CurrentRound : 0;
            int totalRounds = gm != null ? gm.TotalRounds : 0;
            EditorGUILayout.LabelField(
                string.Format("진행: {0}/{1} 게임 | Round {2}/{3} | 상태: {4}",
                    currentGame + 1, iterations, round, totalRounds, stateStr));

            // 진행률 바
            float progress = (float)currentGame / iterations;
            if (gm != null && gm.TotalRounds > 0)
                progress += (float)(gm.CurrentRound - 1) / gm.TotalRounds / iterations;
            EditorGUI.ProgressBar(EditorGUILayout.GetControlRect(false, 18), progress,
                string.Format("{0:P0}", progress));

            // ── Phase 1: 백그라운드 안내 + 스톨 감지 (실시간, 강제완료 안 함) ──
            double idleSec = EditorApplication.timeSinceStartup - lastProgressTime;
            float stallLimit = 20f + 120f / Mathf.Max(1f, timeScale);   // 배속 반영 적응 임계
            if (idleSec > stallLimit)
            {
                EditorGUILayout.HelpBox(
                    string.Format("⚠️ {0:F0}초째 진행 없음 — 멈춤 가능성\n" +
                        "· Unity 창 클릭해 포커스 유지(백그라운드 시 Play 정지)\n" +
                        "· 또는 위 '⚡ 헤드리스 반복 스윕'(백그라운드 OK) 사용", idleSec),
                    MessageType.Warning);
            }
            else
            {
                EditorGUILayout.HelpBox("백그라운드 전환 시 Play가 멈춤 — 포커스 유지 필요. 대량은 ⚡헤드리스 권장.",
                    MessageType.Info);
            }
        }

        EditorGUILayout.Space(4);

        // ── 로그 표시 (토글에 따라 Claude 압축 ↔ 상세 스왑) ──
        EditorGUILayout.LabelField(saveClaudeLog ? "Claude 요약 [실시간]" : "레이스 로그", EditorStyles.boldLabel);
        logScroll = EditorGUILayout.BeginScrollView(logScroll, GUILayout.Height(180));
        string shownLog = saveClaudeLog ? BuildClaudeContent() : displayLog.ToString();
        EditorGUILayout.TextArea(shownLog, GUILayout.ExpandHeight(true));
        EditorGUILayout.EndScrollView();

        // ── 저장 경로 ──
        if (!string.IsNullOrEmpty(savedLogPath))
        {
            EditorGUILayout.Space(2);
            EditorGUILayout.LabelField("저장됨: " + savedLogPath, EditorStyles.miniLabel);
        }

        // ── 집계 ──
        if (gameLogs.Count > 0)
        {
            EditorGUILayout.Space(4);
            showStats = EditorGUILayout.Foldout(showStats, "집계 결과", true);
            if (showStats)
            {
                statsScroll = EditorGUILayout.BeginScrollView(statsScroll, GUILayout.Height(200));
                DrawStats();
                EditorGUILayout.EndScrollView();
            }
        }

        // Play Mode 중 자동 Repaint
        if (isRunning)
            Repaint();
    }

    // ═══════════════════════════════════════
    //  자동 레이스 제어
    // ═══════════════════════════════════════

    // MCP 자동화용 진입점 (private iterations/timeScale/StartAutoRace 우회)
    public void ConfigureAndStart(int iters, float ts)
    {
        iterations    = Mathf.Clamp(iters, 1, 200);
        timeScale     = Mathf.Clamp(ts, 1f, 20f);
        saveClaudeLog = true;
        StartAutoRace();
    }
    public bool IsRunningPublic => isRunning;

    private void StartAutoRace()
    {
        if (!EditorApplication.isPlaying) return;

        isRunning = true;
        currentGame = 0;
        gameLogs.Clear();
        displayLog.Clear();
        savedLogPath = null;
        showStats = false;
        currentTestTimestamp = "test_" + DateTime.Now.ToString("yyMMddHHmmss");

        Time.timeScale = timeScale;
        Application.runInBackground = true;   // Phase 2: best-effort (에디터 Play엔 부분효과지만 무해)
        lastProgressTime = EditorApplication.timeSinceStartup;
        gameOverStopped = false;

        Subscribe();

        // 첫 게임 시작 — 항상 StartNewGame으로 클린 초기화 (Round=1, 배회 포함)
        currentGameLog = new GameIterationLog { gameNumber = currentGame + 1 };
        AppendLog(string.Format("══ 게임 #{0} 시작 ══", currentGame + 1));
        ScheduleNextFrame(() =>
        {
            var g = GameManager.Instance;
            if (g == null) g = UnityEngine.Object.FindFirstObjectByType<GameManager>();
            if (g != null && isRunning)
            {
                g.StartNewGame();
                ScheduleNextFrame(() => AutoBetAndStart());
            }
        });
    }

    private void StopAndSave()
    {
        isRunning = false;
        Time.timeScale = 1f;
        Unsubscribe();

        // 진행 중인 게임 로그 저장
        if (currentGameLog != null && currentGameLog.rounds.Count > 0)
        {
            gameLogs.Add(currentGameLog);
            currentGameLog = null;
        }

        if (gameLogs.Count > 0)
        {
            SaveLogFile();
            AppendLog(string.Format("\n■ 중지 — {0}게임 기록 저장 완료", gameLogs.Count));
        }
        else
        {
            AppendLog("\n■ 중지 — 기록 없음");
        }

        showStats = true;
    }

    // ═══════════════════════════════════════
    //  이벤트 구독
    // ═══════════════════════════════════════

    private void Subscribe()
    {
        if (isSubscribed) return;
        var gm = GameManager.Instance;
        if (gm == null) return;

        gm.OnStateChanged += OnStateChanged;
        EditorApplication.update += OnEditorUpdate;
        isSubscribed = true;
    }

    private void Unsubscribe()
    {
        if (!isSubscribed) return;
        var gm = GameManager.Instance;
        if (gm != null)
            gm.OnStateChanged -= OnStateChanged;

        EditorApplication.update -= OnEditorUpdate;
        pendingAction = null;
        pendingFrames = 0;
        isSubscribed = false;
    }

    // ═══════════════════════════════════════
    //  다음 프레임 예약 (EditorApplication.update)
    // ═══════════════════════════════════════

    private void ScheduleNextFrame(Action action)
    {
        pendingAction = action;
        pendingFrames = 2; // 2프레임 대기 (안전)
    }

    private void OnEditorUpdate()
    {
        if (!isRunning) return;

        // ScheduleNextFrame 지연 액션 처리
        if (pendingAction != null)
        {
            pendingFrames--;
            if (pendingFrames <= 0)
            {
                var action = pendingAction;
                pendingAction = null;
                action?.Invoke();
            }
        }

        // ── Phase 2: best-effort 백그라운드 킥 ──
        // 에디터 '비활성'(백그라운드)일 때만 player-loop 강제 틱 → 포커스 중엔 정상 루프라 중복/과호출 방지.
        // ※ 완전 백그라운드에선 이 콜백 자체가 안 불릴 수 있어 best-effort. 불릴 때만 게임 전진.
        if (EditorApplication.isPlaying && !UnityEditorInternal.InternalEditorUtility.isApplicationActive)
            EditorApplication.QueuePlayerLoopUpdate();
    }

    // ═══════════════════════════════════════
    //  상태 전환 핸들러
    // ═══════════════════════════════════════

    private void OnStateChanged(GameManager.GameState state)
    {
        if (!isRunning) return;

        lastProgressTime = EditorApplication.timeSinceStartup;  // 진행 신호 갱신 (스톨 감지 기준)

        switch (state)
        {
            case GameManager.GameState.Betting:
                ScheduleNextFrame(() => AutoBetAndStart());
                break;

            case GameManager.GameState.Result:
                CollectRoundResult();
                ScheduleNextFrame(() => AutoNextRound());
                break;

            case GameManager.GameState.Finish:
                OnGameFinished();
                break;

            case GameManager.GameState.GameOver:
                // qa 발견: 젤리 소진 → GameOver, 러너 무핸들러라 이전엔 무한 대기. 안전 중단.
                gameOverStopped = true;
                AppendLog("\n⚠️ GameOver(젤리 소진) 감지 — 자동 실행 안전 중단");
                StopAndSave();
                break;
        }
    }

    // ── 자동 배팅 + 레이스 시작 ──
    private void AutoBetAndStart()
    {
        if (!isRunning) return;
        var gm = GameManager.Instance;
        if (gm == null) gm = UnityEngine.Object.FindFirstObjectByType<GameManager>();
        if (gm == null || gm.CurrentState != GameManager.GameState.Betting) return;

        var rm = RaceManager.Instance;
        if (rm == null) rm = UnityEngine.Object.FindFirstObjectByType<RaceManager>();

        // 단승 (Win), 0번 캐릭터 자동 선택
        gm.SelectBetType(BetType.Win);
        gm.AddSelection(0);

        // 배회 중이면 LineUpAndStart 콜백 후 StartRace (SPEC-025 전 라운드 확장 — 상태기반 분기)
        if (rm != null && rm.IsWandering)
        {
            rm.LineUpAndStart(() =>
            {
                var g = GameManager.Instance;
                if (g == null) g = UnityEngine.Object.FindFirstObjectByType<GameManager>();
                if (!isRunning || g == null) return;

                // ★ 프리징 수정: DelayedBettingUI→ResetBetting이 자동배팅 선택을 지웠으면 복구 후 시작
                EnsureAutoBet(g);
                g.StartRace();

                // 안전망: 늦은 ResetBetting 이중경쟁으로 여전히 Betting이면 1회 재확정+재시도
                if (g.CurrentState == GameManager.GameState.Betting)
                    ScheduleNextFrame(() =>
                    {
                        var g2 = GameManager.Instance;
                        if (g2 == null) g2 = UnityEngine.Object.FindFirstObjectByType<GameManager>();
                        if (isRunning && g2 != null && g2.CurrentState == GameManager.GameState.Betting)
                        {
                            EnsureAutoBet(g2);
                            g2.StartRace();
                        }
                    });
            });
        }
        else
        {
            EnsureAutoBet(gm);
            gm.StartRace();
        }
    }

    /// <summary>
    /// StartRace 직전 자동배팅 선택 재확정 (프리징 근본수정).
    /// Round≥2의 DelayedBettingUI→ResetBetting→SelectBetType이 CurrentBet을 빈 것으로 교체해
    /// AddSelection(0)이 소멸하면, StartRace 가드(!IsComplete)에 걸려 Betting에 데드락되던 문제 방어.
    /// </summary>
    private void EnsureAutoBet(GameManager gm)
    {
        if (gm == null) return;
        if (gm.CurrentBet == null || !gm.CurrentBet.IsComplete)
        {
            gm.SelectBetType(BetType.Win);
            gm.AddSelection(0);
        }
    }

    // ── 결과 수집 ──
    private void CollectRoundResult()
    {
        var gm = GameManager.Instance;
        var rm = RaceManager.Instance;
        if (gm == null || rm == null) return;

        var rankings = rm.GetFinalRankings();
        if (rankings == null) return;

        var roundLog = new RoundResultLog
        {
            round = gm.CurrentRound,
            laps = gm.CurrentRoundLaps,
            track = GetCurrentTrackName(),
            rankings = new List<RacerRankLog>()
        };

        var sb = new StringBuilder();
        sb.AppendFormat("  R{0} ({1}바퀴, {2}): ", roundLog.round, roundLog.laps, roundLog.track);

        foreach (var r in rankings)
        {
            var racerLog = new RacerRankLog
            {
                rank = r.rank,
                racerName = r.racerName,
                racerIndex = r.racerIndex
            };

            // 타입 정보
            if (rm.Racers != null && r.racerIndex >= 0 && r.racerIndex < rm.Racers.Count)
            {
                var racer = rm.Racers[r.racerIndex];
                var cd = racer.CharData;
                if (cd != null)
                {
                    racerLog.charId = cd.charId;
                    racerLog.type = cd.charType;
                }
                racerLog.spurtHpRatio = racer.V4SpurtHpRatio;
                racerLog.emergencyBurstCount = racer.V4EmergencyBurstCount;
                racerLog.spurtEntryRank = racer.V4SpurtEntryRank;
                racerLog.finalHpRatio = racer.V4CurrentHpRatio;
            }

            roundLog.rankings.Add(racerLog);

            if (r.rank <= 3)
            {
                sb.AppendFormat("{0}위{1}({2}) ", r.rank, racerLog.racerName,
                    GetTypeShort(racerLog.type));
            }
        }

        currentGameLog?.rounds.Add(roundLog);
        AppendLog(sb.ToString());
    }

    // ── 다음 라운드 ──
    private void AutoNextRound()
    {
        if (!isRunning) return;
        var gm = GameManager.Instance;
        if (gm == null) gm = UnityEngine.Object.FindFirstObjectByType<GameManager>();
        if (gm == null) return;
        gm.NextRound();
    }

    // ── 게임 종료 ──
    private void OnGameFinished()
    {
        if (currentGameLog != null)
        {
            // Finish 직전에 Result에서 이미 마지막 라운드 기록됨
            gameLogs.Add(currentGameLog);
        }

        currentGame++;
        AppendLog(string.Format("══ 게임 #{0} 완료 ══\n", currentGame));

        if (currentGame >= iterations)
        {
            // 전체 완료
            SaveLogFile();
            showStats = true;
            AppendLog(string.Format("▶ 전체 {0}게임 완료! 로그 저장됨", iterations));

            isRunning = false;
            Time.timeScale = 1f;
            Unsubscribe();
        }
        else
        {
            // 다음 게임
            currentGameLog = new GameIterationLog { gameNumber = currentGame + 1 };
            AppendLog(string.Format("══ 게임 #{0} 시작 ══", currentGame + 1));
            ScheduleNextFrame(() =>
            {
                var gm = GameManager.Instance;
                if (gm != null && isRunning)
                    gm.StartNewGame();
            });
        }
    }

    // ═══════════════════════════════════════
    //  집계 표시
    // ═══════════════════════════════════════

    private void DrawStats()
    {
        var typeStats = new Dictionary<CharacterType, TypeStat>();
        var charStats = new Dictionary<string, CharStat>();

        foreach (var game in gameLogs)
        {
            foreach (var round in game.rounds)
            {
                foreach (var r in round.rankings)
                {
                    // 타입별
                    if (!typeStats.ContainsKey(r.type))
                        typeStats[r.type] = new TypeStat { type = r.type };
                    var ts = typeStats[r.type];
                    ts.totalRank += r.rank;
                    ts.count++;
                    if (r.rank == 1) ts.wins++;
                    if (r.rank <= 3) ts.top3++;

                    // 캐릭터별
                    string key = string.IsNullOrEmpty(r.charId) ? r.racerName : r.charId;
                    if (!charStats.ContainsKey(key))
                        charStats[key] = new CharStat { name = r.racerName, type = r.type };
                    var cs = charStats[key];
                    cs.totalRank += r.rank;
                    cs.count++;
                    if (r.rank == 1) cs.wins++;
                    if (r.rank <= 3) cs.top3++;
                }
            }
        }

        int totalRaces = 0;
        foreach (var game in gameLogs)
            totalRaces += game.rounds.Count;

        EditorGUILayout.LabelField(string.Format("총 {0}게임, {1}레이스", gameLogs.Count, totalRaces),
            EditorStyles.boldLabel);

        // 타입별
        EditorGUILayout.LabelField("타입별 집계", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("타입", GUILayout.Width(60));
        EditorGUILayout.LabelField("평균순위", GUILayout.Width(60));
        EditorGUILayout.LabelField("1위", GUILayout.Width(40));
        EditorGUILayout.LabelField("Top3", GUILayout.Width(40));
        EditorGUILayout.LabelField("1위율", GUILayout.Width(50));
        EditorGUILayout.EndHorizontal();

        foreach (var kv in typeStats)
        {
            var s = kv.Value;
            float avg = s.count > 0 ? (float)s.totalRank / s.count : 0;
            float winRate = s.count > 0 ? (float)s.wins / s.count * 100f : 0;
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(GetTypeShort(s.type), GUILayout.Width(60));
            EditorGUILayout.LabelField(avg.ToString("F1"), GUILayout.Width(60));
            EditorGUILayout.LabelField(s.wins.ToString(), GUILayout.Width(40));
            EditorGUILayout.LabelField(s.top3.ToString(), GUILayout.Width(40));
            EditorGUILayout.LabelField(winRate.ToString("F1") + "%", GUILayout.Width(50));
            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.Space(4);

        // 캐릭터별
        EditorGUILayout.LabelField("캐릭터별 집계", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("이름", GUILayout.Width(80));
        EditorGUILayout.LabelField("타입", GUILayout.Width(50));
        EditorGUILayout.LabelField("평균", GUILayout.Width(40));
        EditorGUILayout.LabelField("1위", GUILayout.Width(30));
        EditorGUILayout.LabelField("Top3", GUILayout.Width(35));
        EditorGUILayout.EndHorizontal();

        foreach (var kv in charStats)
        {
            var s = kv.Value;
            float avg = s.count > 0 ? (float)s.totalRank / s.count : 0;
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(s.name, GUILayout.Width(80));
            EditorGUILayout.LabelField(GetTypeShort(s.type), GUILayout.Width(50));
            EditorGUILayout.LabelField(avg.ToString("F1"), GUILayout.Width(40));
            EditorGUILayout.LabelField(s.wins.ToString(), GUILayout.Width(30));
            EditorGUILayout.LabelField(s.top3.ToString(), GUILayout.Width(35));
            EditorGUILayout.EndHorizontal();
        }
    }

    // ═══════════════════════════════════════
    //  로그 파일 저장
    // ═══════════════════════════════════════

    private void SaveLogFile()
    {
        string dir = Path.Combine(Application.dataPath, "..", "Docs", "logs");
        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        if (string.IsNullOrEmpty(currentTestTimestamp))
            currentTestTimestamp = "test_" + DateTime.Now.ToString("yyMMddHHmmss");
        string filePath = Path.Combine(dir, currentTestTimestamp + ".md");

        var sb = new StringBuilder();
        sb.AppendLine("# 자동 레이스 테스트 결과");
        sb.AppendLine();
        sb.AppendFormat("- 일시: {0}\n", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
        sb.AppendFormat("- 게임 수: {0}/{1}\n", gameLogs.Count, iterations);

        // V4 설정 요약
        var gs = GameSettings.Instance;
        if (gs != null)
        {
            sb.AppendFormat("- 레이스 시스템: {0}\n",
                gs.useV4RaceSystem ? "V4" : "Legacy");
            sb.AppendFormat("- GlobalSpeed: {0:F2}\n", gs.globalSpeedMultiplier);

            int[] laps = gs.roundLaps;
            if (laps != null)
            {
                sb.Append("- 라운드 구성: ");
                for (int i = 0; i < laps.Length; i++)
                {
                    if (i > 0) sb.Append(", ");
                    sb.AppendFormat("R{0}={1}바퀴", i + 1, laps[i]);
                }
                sb.AppendLine();
            }

            if (gs.useV4RaceSystem && gs.v4Settings != null)
            {
                var v4 = gs.v4Settings;
                sb.AppendFormat("- V4: Normal×{0:F2} Burst×{1:F2} Spurt×{2:F2} SpurtStart:{3:P0}\n",
                    v4.v4_normalSpeedRatio, v4.v4_burstSpeedRatio,
                    v4.v4_spurtVmaxBonus, v4.v4_finalSpurtStart);
                sb.AppendFormat("- V4: Drain/Prog:{0:F1} BurstDrain×{1:F1} SpurtDrain×{2:F1}\n",
                    v4.v4_drainPerLap, v4.v4_burstDrainMul, v4.v4_spurtDrainMul);
            }
        }
        sb.AppendFormat("- 테스트 배속: {0:F0}x\n", timeScale);
        sb.AppendLine();

        // ── 라운드별 상세 ──
        sb.AppendLine("## 라운드별 결과");
        sb.AppendLine();

        foreach (var game in gameLogs)
        {
            sb.AppendFormat("### 게임 #{0}\n\n", game.gameNumber);

            foreach (var round in game.rounds)
            {
                sb.AppendFormat("**R{0}** ({1}바퀴, {2})\n\n", round.round, round.laps, round.track);
                sb.AppendLine("| 순위 | 캐릭터 | 타입 |");
                sb.AppendLine("|------|--------|------|");

                foreach (var r in round.rankings)
                {
                    sb.AppendFormat("| {0} | {1} | {2} |\n",
                        r.rank, r.racerName, GetTypeShort(r.type));
                }
                sb.AppendLine();
            }
        }

        // ── 집계 ──
        sb.AppendLine("## 집계");
        sb.AppendLine();

        var typeStats = new Dictionary<CharacterType, TypeStat>();
        var charStats = new Dictionary<string, CharStat>();

        int totalRaces = 0;
        foreach (var game in gameLogs)
        {
            totalRaces += game.rounds.Count;
            foreach (var round in game.rounds)
            {
                foreach (var r in round.rankings)
                {
                    if (!typeStats.ContainsKey(r.type))
                        typeStats[r.type] = new TypeStat { type = r.type };
                    var ts = typeStats[r.type];
                    ts.totalRank += r.rank;
                    ts.count++;
                    if (r.rank == 1) ts.wins++;
                    if (r.rank <= 3) ts.top3++;

                    string key = string.IsNullOrEmpty(r.charId) ? r.racerName : r.charId;
                    if (!charStats.ContainsKey(key))
                        charStats[key] = new CharStat { name = r.racerName, type = r.type };
                    var cs = charStats[key];
                    cs.totalRank += r.rank;
                    cs.count++;
                    if (r.rank == 1) cs.wins++;
                    if (r.rank <= 3) cs.top3++;
                }
            }
        }

        sb.AppendFormat("총 {0}게임, {1}레이스\n\n", gameLogs.Count, totalRaces);

        sb.AppendLine("### 타입별");
        sb.AppendLine();
        sb.AppendLine("| 타입 | 평균순위 | 1위 | Top3 | 1위율 |");
        sb.AppendLine("|------|----------|-----|------|-------|");
        foreach (var kv in typeStats)
        {
            var s = kv.Value;
            float avg = s.count > 0 ? (float)s.totalRank / s.count : 0;
            float winRate = s.count > 0 ? (float)s.wins / s.count * 100f : 0;
            sb.AppendFormat("| {0} | {1:F1} | {2} | {3} | {4:F1}% |\n",
                GetTypeShort(s.type), avg, s.wins, s.top3, winRate);
        }
        sb.AppendLine();

        sb.AppendLine("### 캐릭터별");
        sb.AppendLine();
        sb.AppendLine("| 캐릭터 | 타입 | 평균순위 | 1위 | Top3 |");
        sb.AppendLine("|--------|------|----------|-----|------|");
        foreach (var kv in charStats)
        {
            var s = kv.Value;
            float avg = s.count > 0 ? (float)s.totalRank / s.count : 0;
            sb.AppendFormat("| {0} | {1} | {2:F1} | {3} | {4} |\n",
                s.name, GetTypeShort(s.type), avg, s.wins, s.top3);
        }

        File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
        savedLogPath = filePath;
        Debug.Log("[AutoRace] 로그 저장: " + filePath);

        if (saveClaudeLog)
            SaveClaudeLogFile();
    }

    // ═══════════════════════════════════════
    //  Claude 요약 로그 (토큰 최적화)
    // ═══════════════════════════════════════

    // ─ 공용: Claude 압축 포맷 문자열 생성 (파일 저장 + 실시간 표시 양쪽에서 사용) ─
    private string BuildClaudeContent()
    {
        var sb = new StringBuilder();

        // ── 기본 정보 ──
        int totalRaces = 0;
        foreach (var g in gameLogs) totalRaces += g.rounds.Count;
        sb.AppendFormat("GAMES={0} RACES={1} DATE={2}\n", gameLogs.Count, totalRaces, DateTime.Now.ToString("yyMMdd_HHmm"));

        // ── V4 파라미터 핵심값 ──
        var gs = GameSettings.Instance;
        if (gs != null && gs.useV4RaceSystem && gs.v4Settings != null)
        {
            var v4 = gs.v4Settings;
            sb.AppendFormat("BURST R:[{0:F2},{1:F2}] L:[{2:F2},{3:F2}] C:[{4:F2},{5:F2}] K:[{6:F2},{7:F2}]\n",
                v4.v4_runnerBurstStart, v4.v4_runnerBurstEnd,
                v4.v4_leaderBurstStart, v4.v4_leaderBurstEnd,
                v4.v4_chaserBurstStart, v4.v4_chaserBurstEnd,
                v4.v4_reckonerBurstStart, v4.v4_reckonerBurstEnd);
            sb.AppendFormat("EMERG R:{0:F1} L:{1:F1} C:{2:F1} K:{3:F1}\n",
                v4.v4_runnerEmergencyDrainMul, v4.v4_leaderEmergencyDrainMul,
                v4.v4_chaserEmergencyDrainMul, v4.v4_reckonerEmergencyDrainMul);
            sb.AppendFormat("SPURT spd:{0:F2} acc:{1:F2} start:{2:F2}\n",
                v4.v4_spurtHpSpeedBonus, v4.v4_spurtHpAccelBonus, v4.v4_finalSpurtStart);
        }
        sb.AppendLine();

        // ── 데이터 집계 ──
        CharacterType[] types = { CharacterType.Runner, CharacterType.Leader, CharacterType.Chaser, CharacterType.Reckoner };

        // 거리별×타입별 상세 통계
        var distStat = new Dictionary<int, Dictionary<CharacterType, DistTypeStat>>();
        var distTotal = new Dictionary<int, int>();
        // 전체 타입 통계
        var typeStats = new Dictionary<CharacterType, TypeStat>();
        // 캐릭터 통계
        var charStats = new Dictionary<string, CharStat>();
        // 캐릭터×바퀴별 통계 (지배 검증) + 바퀴별 완주 HP (고갈 검증)
        var charDistStats = new Dictionary<string, Dictionary<int, CharDistStat>>();
        var finishHpStats = new Dictionary<int, FinishHpStat>();

        foreach (var game in gameLogs)
        {
            foreach (var round in game.rounds)
            {
                int laps = round.laps;
                if (!distStat.ContainsKey(laps))
                {
                    distStat[laps] = new Dictionary<CharacterType, DistTypeStat>();
                    distTotal[laps] = 0;
                }
                distTotal[laps]++;

                foreach (var r in round.rankings)
                {
                    // 전체 타입 통계
                    if (!typeStats.ContainsKey(r.type)) typeStats[r.type] = new TypeStat { type = r.type };
                    var ts = typeStats[r.type];
                    ts.totalRank += r.rank; ts.count++;
                    if (r.rank == 1) ts.wins++;
                    if (r.rank <= 3) ts.top3++;
                    if (r.spurtHpRatio > 0f) { ts.spurtHpTotal += r.spurtHpRatio; ts.spurtHpCount++; }
                    ts.emergencyTotal += r.emergencyBurstCount;
                    if (r.spurtEntryRank > 0) { ts.spurtRankTotal += r.spurtEntryRank; ts.spurtRankCount++; }

                    // 거리별 타입 통계
                    if (!distStat[laps].ContainsKey(r.type)) distStat[laps][r.type] = new DistTypeStat();
                    var ds = distStat[laps][r.type];
                    ds.count++; ds.totalRank += r.rank;
                    if (r.rank == 1) ds.wins++;
                    if (r.rank <= 3) ds.top3++;
                    if (r.spurtHpRatio > 0f) { ds.spurtHpTotal += r.spurtHpRatio; ds.spurtHpCount++; }

                    // 캐릭터 통계
                    string key = string.IsNullOrEmpty(r.charId) ? r.racerName : r.charId;
                    if (!charStats.ContainsKey(key)) charStats[key] = new CharStat { id = key, name = r.racerName, type = r.type };
                    var cs = charStats[key];
                    cs.totalRank += r.rank; cs.count++;
                    if (r.rank == 1) cs.wins++;
                    if (r.rank <= 3) cs.top3++;

                    // 캐릭터×바퀴별 통계 (특정 캐릭터 전 바퀴 지배 검증)
                    if (!charDistStats.ContainsKey(key)) charDistStats[key] = new Dictionary<int, CharDistStat>();
                    if (!charDistStats[key].ContainsKey(laps)) charDistStats[key][laps] = new CharDistStat();
                    var cds = charDistStats[key][laps];
                    cds.count++; cds.totalRank += r.rank;
                    if (r.rank == 1) cds.wins++;

                    // 바퀴별 완주 HP 통계 (6바퀴 고갈 검증)
                    if (!finishHpStats.ContainsKey(laps)) finishHpStats[laps] = new FinishHpStat();
                    var fh = finishHpStats[laps];
                    fh.finalHpTotal += r.finalHpRatio; fh.count++;
                    if (r.finalHpRatio <= 0.05f) fh.hp0Count++;
                }
            }
        }

        // ── 거리별 상세 (1위율 / Top3율 / avgRank) ──
        // 헤더: win%/t3%/avg  R=도주 L=선행 C=선입 K=추입
        sb.AppendLine("# DIST  win%/t3%/avg/hp  R=도주 L=선행 C=선입 K=추입");
        var sortedLaps = new List<int>(distStat.Keys); sortedLaps.Sort();
        foreach (int lap in sortedLaps)
        {
            int tot = distTotal[lap];
            sb.AppendFormat("{0}L", lap);
            foreach (var type in types)
            {
                char tc = type == CharacterType.Runner ? 'R' : type == CharacterType.Leader ? 'L' : type == CharacterType.Chaser ? 'C' : 'K';
                if (distStat[lap].TryGetValue(type, out var ds) && ds.count > 0)
                {
                    float winPct  = ds.wins  * 100f / tot;
                    float avg     = (float)ds.totalRank / ds.count;
                    string hp     = ds.spurtHpCount > 0 ? $"{ds.spurtHpTotal / ds.spurtHpCount * 100f:F0}%" : "—";
                    sb.AppendFormat("  {0}:{1:F0}%({2})/{3:F0}%/{4:F1}/{5}", tc, winPct, ds.wins, ds.top3 * 100f / ds.count, avg, hp);
                }
                else
                {
                    sb.AppendFormat("  {0}:0%(0)/0%/—/—", tc);
                }
            }
            sb.AppendFormat("  n={0}\n", tot);
        }
        sb.AppendLine();

        // ── 전체 타입 집계 ──
        sb.AppendLine("# TOTAL  wins(%) avgRank top3% spurtHP emergAvg spurtRank");
        foreach (var type in types)
        {
            if (!typeStats.ContainsKey(type)) continue;
            var ts = typeStats[type];
            char tc = type == CharacterType.Runner ? 'R' : type == CharacterType.Leader ? 'L' : type == CharacterType.Chaser ? 'C' : 'K';
            float avg    = ts.count > 0 ? (float)ts.totalRank / ts.count : 0;
            float wr     = totalRaces > 0 ? ts.wins * 100f / totalRaces : 0;
            float t3r    = ts.count  > 0 ? ts.top3 * 100f / ts.count : 0;
            string spHp  = ts.spurtHpCount > 0 ? $"{ts.spurtHpTotal / ts.spurtHpCount * 100f:F0}%" : "—";
            float emAvg  = ts.count > 0 ? (float)ts.emergencyTotal / ts.count : 0;
            string spRk  = ts.spurtRankCount > 0 ? $"{(float)ts.spurtRankTotal / ts.spurtRankCount:F1}" : "—";
            sb.AppendFormat("{0}  wins={1}({2:F1}%) avg={3:F1} top3={4:F0}% spurtHP={5} emerg={6:F1} spRank={7}\n",
                tc, ts.wins, wr, avg, t3r, spHp, emAvg, spRk);
        }
        sb.AppendLine();

        // ── 상위 캐릭터 Top8 (1위 기준) ──
        var sortedChars = new List<CharStat>(charStats.Values);
        sortedChars.Sort((a, b) => b.wins.CompareTo(a.wins));
        sb.AppendLine("# TOP8  name(type) wins avgRank");
        int cnt = 0;
        foreach (var c in sortedChars)
        {
            if (cnt++ >= 8) break;
            char tc = c.type == CharacterType.Runner ? 'R' : c.type == CharacterType.Leader ? 'L' : c.type == CharacterType.Chaser ? 'C' : 'K';
            float avg = c.count > 0 ? (float)c.totalRank / c.count : 0;
            sb.AppendFormat("{0}({1}) w={2} avg={3:F1}\n", c.name, tc, c.wins, avg);
        }
        sb.AppendLine();

        // ── 캐릭터×바퀴별 1위율 (특정 캐릭터 전 바퀴 지배 검증) ──
        // 전체 1위수 상위 12명만 표시(전 바퀴 고승률 = 지배 신호)
        sortedLaps = new List<int>(finishHpStats.Keys); sortedLaps.Sort();
        sb.Append("# CHAR×DIST win%  bylap[");
        foreach (int lap in sortedLaps) sb.AppendFormat("{0}L ", lap);
        sb.AppendLine("]");
        int cc = 0;
        foreach (var c in sortedChars)
        {
            if (cc++ >= 12) break;
            char tc = c.type == CharacterType.Runner ? 'R' : c.type == CharacterType.Leader ? 'L' : c.type == CharacterType.Chaser ? 'C' : 'K';
            sb.AppendFormat("{0}({1})", c.name, tc);
            foreach (int lap in sortedLaps)
            {
                if (charDistStats.TryGetValue(c.id, out var byLap) && byLap.TryGetValue(lap, out var cds) && cds.count > 0)
                    sb.AppendFormat(" {0:F0}%({1}/{2})", cds.wins * 100f / cds.count, cds.wins, cds.count);
                else
                    sb.Append(" —");
            }
            sb.AppendLine();
        }
        sb.AppendLine();

        // ── 바퀴별 완주 HP (6바퀴 HP 고갈 검증) ──
        sb.AppendLine("# FINISH-HP by dist  avgHP / hp≤5%ratio");
        foreach (int lap in sortedLaps)
        {
            var fh = finishHpStats[lap];
            float avgHp = fh.count > 0 ? fh.finalHpTotal / fh.count * 100f : 0f;
            float hp0   = fh.count > 0 ? fh.hp0Count * 100f / fh.count : 0f;
            sb.AppendFormat("{0}L: avgHP={1:F0}%  hp≤5%={2:F0}% (n={3})\n", lap, avgHp, hp0, fh.count);
        }

        return sb.ToString();
    }

    private void SaveClaudeLogFile()
    {
        string dir = Path.Combine(Application.dataPath, "..", "Docs", "logs");
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
        string filePath = Path.Combine(dir, currentTestTimestamp + "_claude.txt");
        File.WriteAllText(filePath, BuildClaudeContent(), Encoding.UTF8);
        Debug.Log("[AutoRace] Claude 요약 로그 저장: " + filePath);
    }

    // ═══════════════════════════════════════
    //  유틸리티
    // ═══════════════════════════════════════

    private void AppendLog(string msg)
    {
        displayLog.AppendLine(msg);
        // 로그가 너무 길면 앞부분 잘라내기
        if (displayLog.Length > 10000)
        {
            string s = displayLog.ToString();
            displayLog.Clear();
            displayLog.Append(s.Substring(s.Length - 8000));
        }
    }

    private string GetCurrentTrackName()
    {
        if (TrackDatabase.Instance?.CurrentTrackInfo != null)
            return TrackDatabase.Instance.CurrentTrackInfo.DisplayName;
        return "기본";
    }

    private static string GetTypeShort(CharacterType type)
    {
        switch (type)
        {
            case CharacterType.Runner: return "도주";
            case CharacterType.Leader: return "선행";
            case CharacterType.Chaser: return "선입";
            case CharacterType.Reckoner: return "추입";
            default: return "?";
        }
    }

    // ═══════════════════════════════════════
    //  데이터 클래스
    // ═══════════════════════════════════════

    private class GameIterationLog
    {
        public int gameNumber;
        public List<RoundResultLog> rounds = new List<RoundResultLog>();
    }

    private class RoundResultLog
    {
        public int round;
        public int laps;
        public string track;
        public List<RacerRankLog> rankings;
    }

    private class RacerRankLog
    {
        public int rank;
        public string charId;
        public string racerName;
        public int racerIndex;
        public CharacterType type;
        public float spurtHpRatio;       // 스퍼트 진입 시 HP 비율 (0=스퍼트 미진입)
        public int emergencyBurstCount;  // Emergency Burst 발동 횟수
        public int spurtEntryRank;       // 스퍼트 진입 시 순위 (0=미진입)
        public float finalHpRatio;       // 완주 시점 HP 비율 (0~1) — 6바퀴 HP 고갈 검증용
    }

    private class TypeStat
    {
        public CharacterType type;
        public int totalRank;
        public int count;
        public int wins;
        public int top3;
        public float spurtHpTotal;       // 스퍼트 진입 HP 합산
        public int spurtHpCount;         // 스퍼트 진입 횟수
        public int emergencyTotal;       // Emergency Burst 총 횟수
        public int spurtRankTotal;       // 스퍼트 진입 순위 합산
        public int spurtRankCount;       // 스퍼트 진입 횟수 (순위용)
    }

    private class DistTypeStat
    {
        public int wins;
        public int top3;
        public int totalRank;
        public int count;
        public float spurtHpTotal;
        public int spurtHpCount;
    }

    // 캐릭터×바퀴별 통계 (특정 캐릭터 전 바퀴 지배 검증용)
    private class CharDistStat
    {
        public int wins;
        public int count;
        public int totalRank;
    }

    // 바퀴별 완주 HP 통계 (6바퀴 고갈 검증용)
    private class FinishHpStat
    {
        public float finalHpTotal;
        public int count;
        public int hp0Count;   // finalHpRatio <= 0.05 (사실상 고갈)
    }

    private class CharStat
    {
        public string id;   // charId (charDistStats 매핑용)
        public string name;
        public CharacterType type;
        public int totalRank;
        public int count;
        public int wins;
        public int top3;
    }
}
