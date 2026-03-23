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
            iterations = EditorGUILayout.IntSlider("반복 횟수", iterations, 1, 100);
            timeScale = EditorGUILayout.Slider("배속", timeScale, 1f, 20f);
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
        }

        EditorGUILayout.Space(4);

        // ── 로그 표시 ──
        EditorGUILayout.LabelField("레이스 로그", EditorStyles.boldLabel);
        logScroll = EditorGUILayout.BeginScrollView(logScroll, GUILayout.Height(180));
        EditorGUILayout.TextArea(displayLog.ToString(), GUILayout.ExpandHeight(true));
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

    private void StartAutoRace()
    {
        if (!EditorApplication.isPlaying) return;

        isRunning = true;
        currentGame = 0;
        gameLogs.Clear();
        displayLog.Clear();
        savedLogPath = null;
        showStats = false;

        Time.timeScale = timeScale;

        Subscribe();

        // 첫 게임 시작: 현재 상태가 Betting이면 바로 자동 배팅
        var gm = GameManager.Instance;
        if (gm != null)
        {
            currentGameLog = new GameIterationLog { gameNumber = currentGame + 1 };
            AppendLog(string.Format("══ 게임 #{0} 시작 ══", currentGame + 1));

            if (gm.CurrentState == GameManager.GameState.Betting)
            {
                ScheduleNextFrame(() => AutoBetAndStart());
            }
        }
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
        if (!isRunning || pendingAction == null) return;

        pendingFrames--;
        if (pendingFrames <= 0)
        {
            var action = pendingAction;
            pendingAction = null;
            action?.Invoke();
        }
    }

    // ═══════════════════════════════════════
    //  상태 전환 핸들러
    // ═══════════════════════════════════════

    private void OnStateChanged(GameManager.GameState state)
    {
        if (!isRunning) return;

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
        }
    }

    // ── 자동 배팅 + 레이스 시작 ──
    private void AutoBetAndStart()
    {
        if (!isRunning) return;
        var gm = GameManager.Instance;
        if (gm == null || gm.CurrentState != GameManager.GameState.Betting) return;

        // 단승 (Win), 0번 캐릭터 자동 선택
        gm.SelectBetType(BetType.Win);
        gm.AddSelection(0);
        gm.StartRace();
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
                var cd = rm.Racers[r.racerIndex].CharData;
                if (cd != null)
                {
                    racerLog.charId = cd.charId;
                    racerLog.type = cd.charType;
                }
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
            isRunning = false;
            Time.timeScale = 1f;
            Unsubscribe();
            SaveLogFile();
            showStats = true;
            AppendLog(string.Format("▶ 전체 {0}게임 완료! 로그 저장됨", iterations));
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

        string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string filePath = Path.Combine(dir, "autorace_" + timestamp + ".md");

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
    }

    private class TypeStat
    {
        public CharacterType type;
        public int totalRank;
        public int count;
        public int wins;
        public int top3;
    }

    private class CharStat
    {
        public string name;
        public CharacterType type;
        public int totalRank;
        public int count;
        public int wins;
        public int top3;
    }
}
