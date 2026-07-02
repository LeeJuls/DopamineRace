#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.IO;

/// <summary>
/// 레이스 백테스팅 에디터 윈도우 (HP 시스템 미러)
/// 충돌/슬링샷/회피 시뮬레이션 + HP 부스트 + 스탯 기여 분석 + 전체 트랙 비교 + 로그 저장
/// </summary>
public class RaceBacktestWindow : EditorWindow
{
    private GameSettings gameSettings;
    private TrackData selectedTrack;
    private int simCount = 100;
    private int simLaps = 5;
    private int simRacers = 8;
    private float simTimeStep = 0.05f;
    private bool simCollision = true;
    private bool showPerRace = false;
    private bool runAllTracks = false;
    private bool saveLog = true;
    private bool equalStats = false;
    private int equalStatValue = 20;

    // ★ SPEC-033 통제 단일스탯 스윕 계측 (에디터 전용, 게임런타임 무관)
    private bool statSweepMode = false;
    private int sweepStat = 0;        // 0=SPD 1=ACC 2=POW 3=STA 4=INT 5=LUCK
    private int sweepBase = 14;       // 비스윕 스탯 + 슬롯0 기준값
    private int sweepStep = 1;        // 슬롯당 증가폭
    private int sweepType = 1;        // CharacterType (0=Runner 1=Leader 2=Chaser 3=Reckoner)
    private int sweepSeedBase = 73101;
    // 값버킷별 집계 (슬롯-값 로테이션으로 위치편향 상쇄)
    private long[] _sweepBucketWins;
    private long[] _sweepBucketRaces;
    private Vector2 scrollPos;
    private string resultText = "";
    private string lastLogPath = "";
    private bool isRunning = false;
    private bool cancelRequested = false;
    // ★ 헤드리스 배치 실행 (에디터 update 무관 동기 실행 — 백그라운드에서도 즉시 완료)
    private bool headlessMode = false;
    private int  headlessSeed = 0;
    private int  headlessMaxTicks = 0;      // 스톨 진단: race당 최대 tick 수
    private int  headlessStallRaces = 0;    // 스톨 진단: simTime>=300f 상한 도달 race 수 (0 기대)
    // ★ 클러치 튜닝 오버라이드 (null이면 GameSettings/CSV값) — 스윕에서 조합 주입, 파일 수정 없이 격자 측정
    private static float? headlessClutchChance = null;
    private static float? headlessClutchBonus  = null;
    private List<SimRacer> _activeRacers; // V4 시뮬레이션에서 사용
    private Dictionary<string, CharacterDataV4> v4DataMap;

    // ★ 구간별 포지션 추적 체크포인트
    private static readonly float[] segCheckpoints = { 0.10f, 0.20f, 0.35f, 0.50f, 0.65f, 0.80f, 0.90f };

    [MenuItem("DopamineRace/백테스팅")]
    public static void ShowWindow()
    {
        GetWindow<RaceBacktestWindow>("레이스 백테스팅");
    }

    // ★ 헤드리스 멀티랩 밸런스 스윕 — 메뉴 1클릭 실행 (Play 불필요, 결과 Console + Docs/logs 저장)
    [MenuItem("DopamineRace/밸런스 스윕 (2~6바퀴 헤드리스)")]
    public static void MenuRunLapSweep()
    {
        string r = RunLapSweepHeadless(800, new[] { 2, 3, 4, 5, 6 }, 8, 73101);
        Debug.Log("[BalanceSweep] 완료\n" + r);
    }

    private void OnEnable()
    {
        gameSettings = Resources.Load<GameSettings>("GameSettings");
    }

    private void OnGUI()
    {
        bool isV4 = gameSettings != null && gameSettings.useV4RaceSystem;
        EditorGUILayout.LabelField("🏇 레이스 백테스팅", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        gameSettings = (GameSettings)EditorGUILayout.ObjectField("GameSettings", gameSettings, typeof(GameSettings), false);

        // ═══ V4 현재 세팅 요약 ═══
        if (isV4)
        {
            var gs4 = gameSettings.v4Settings;
            EditorGUILayout.HelpBox(
                string.Format("V4 활성 | GlobalSpeed×{0:F1} | Normal×{1:F2} Burst×{2:F2} Spurt×{3:F2}\n" +
                    "Drain/Prog:{4:F0} | BurstDrain×{5:F1} SpurtDrain×{6:F1}\n" +
                    "긴급부스트:{7} Spd×{8:F2} 쿨다운:{9:F1}s 도주지속:{10}",
                    gameSettings.globalSpeedMultiplier,
                    gs4.v4_normalSpeedRatio, gs4.v4_burstSpeedRatio, gs4.v4_spurtVmaxBonus,
                    gs4.v4_drainPerLap, gs4.v4_burstDrainMul, gs4.v4_spurtDrainMul,
                    gs4.v4_emergencyBurstEnabled ? "ON" : "OFF",
                    gs4.v4_emergencyBurstSpeedRatio,
                    gs4.v4_emergencyBurstCooldown,
                    gs4.v4_runnerPersistentBurst ? "ON" : "OFF"),
                MessageType.Info);
        }

        EditorGUILayout.Space();
        runAllTracks = EditorGUILayout.Toggle("🌍 전체 트랙 비교 모드", runAllTracks);
        if (!runAllTracks)
            selectedTrack = (TrackData)EditorGUILayout.ObjectField("트랙 (None=일반)", selectedTrack, typeof(TrackData), false);

        EditorGUILayout.Space();
        simCount = EditorGUILayout.IntSlider("시뮬레이션 횟수", simCount, 10, 1000);
        simLaps = EditorGUILayout.IntSlider("바퀴 수", simLaps, 1, 10);
        simRacers = EditorGUILayout.IntSlider("참가자 수", simRacers, 2, 12);
        simTimeStep = EditorGUILayout.Slider("시간 단위 (초)", simTimeStep, 0.01f, 0.1f);
        simCollision = EditorGUILayout.Toggle("충돌 시뮬레이션", simCollision);
        showPerRace = EditorGUILayout.Toggle("개별 레이스 결과 표시", showPerRace);
        saveLog = EditorGUILayout.Toggle("📄 로그 파일 저장", saveLog);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("━━━ 밸런스 테스트 옵션 ━━━");
        equalStats = EditorGUILayout.Toggle("균등 스탯 테스트", equalStats);
        if (equalStats)
            equalStatValue = EditorGUILayout.IntSlider("스탯 값", equalStatValue, 1, 20);

        EditorGUILayout.Space();

        GUI.enabled = !isRunning && gameSettings != null;
        string runLabel = isRunning ? "시뮬레이션 중..."
            : string.Format("▶ 시뮬레이션 ({0}회 × {1}바퀴)", simCount, simLaps);
        if (GUILayout.Button(runLabel, GUILayout.Height(30)))
        {
            if (runAllTracks)
                RunAllTracksSimulation();
            else
                RunSingleTrackSimulation();
        }
        GUI.enabled = true;

        if (!string.IsNullOrEmpty(lastLogPath))
        {
            EditorGUILayout.HelpBox("로그 저장됨: " + lastLogPath, MessageType.Info);
        }

        EditorGUILayout.Space();
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
        EditorGUILayout.TextArea(resultText, GUILayout.ExpandHeight(true));
        EditorGUILayout.EndScrollView();
    }

    // ══════════════════════════════════════
    //  SimRacer
    // ══════════════════════════════════════

    private class SimRacer
    {
        public CharacterData data;
        public float position;
        public float currentSpeed;
        public float noiseValue;
        public float noiseTimer;
        public float luckTimer;
        public float critRemaining;
        public bool isCrit;
        public int finishOrder;
        public bool finished;

        // 충돌 상태
        public float collisionPenalty;
        public float collisionTimer;
        public float slingshotBoost;
        public float slingshotTimer;

        // 기존 통계
        public int critCount;
        public int collisionWins;
        public int collisionLosses;
        public int dodgeCount;
        public int slingshotCount;
        public float totalDistLost;
        public float totalDistGained;

        // ★ 스탯별 기여 거리 (양수=이득, 음수=손해)
        public float contrib_speed;       // SpeedMultiplier 기여 (기준 0.8배속 대비)
        public float contrib_type;        // 타입 보너스 기여 (HP시스템: hpBoost 기여)
        public float contrib_endurance;   // 피로 누적 (HP시스템: 미사용)
        public float contrib_calm;        // 노이즈 누적
        public float contrib_luck;        // 크리티컬 거리 이득
        public float contrib_power;       // 충돌에서 덜 잃은 거리
        public float contrib_brave;       // 슬링샷 거리 이득

        // ★ HP 시스템 (SPEC-006)
        public float enduranceHP;
        public float maxHP;
        public float totalConsumedHP;
        public float hpBoostValue;

        // ★ Phase 4: 포지션 보정
        public int currentRank;           // 실시간 순위 (1~N)
        public float slipstreamBlend;     // 슬립스트림 페이드 (0~1, 전체 타입)

        // ★ CP 시스템
        public float calmPoints;          // 현재 CP
        public float maxCP;               // 최대 CP

        // ★ SPEC-RC-002: 스프린트 상태
        public bool isSprintMode;         // 전력질주 상태

        // ★ Race V4 시스템
        public CharacterDataV4 dataV4;
        public float v4Stamina;
        public float v4MaxStamina;
        public float v4CurrentSpeed;
        public float v4LastProgress;
        public bool v4EmergencyBurst;
        public float v4EmergencyBurstCooldownTimer;
        public float v4CritBoostRemaining;
        public float v4LuckTimer;
        public bool v4InSlipstream;
        public float v4SlipstreamDrainMul;
        public bool v4IsSpurting;
        public float v4SpurtHpRatio;   // 스퍼트 진입 시 HP 비율 스냅샷

        // ★ SPEC-031.P2 INT 진단 (레이스별 리셋)
        public float v4BurstDrainAccum;   // burst 구간 누적 drain
        public float v4BurstFirstEntry;   // burst 첫 진입 progress (-1 = 미진입)
        public float v4BurstZoneWidth;    // 이 레이서 타입의 burst 구간 폭 (드레인 정규화용)

        // ★ 액티브 스킬 (Group B + 쿨타임)
        public bool skillActive;
        public float skillRemainingTime;
        public float skillCooldownTimer;     // 발동 후 재발동 쿨다운 (LapScale 지수 0.35 적용)
        public int skillCollisionCount;
        public bool skillHpTriggered;       // E_Skill_HP 1회 발동 추적
        public bool skillRankTriggered;     // E_Skill_Rank 1회 발동 추적

        // ★ 패시브 스킬 (Group C)
        public bool passiveConditionActive;
        public float passiveCooldownTimer;

        // ★ 클러치 도박 (LuckClutch 미러 — UpdateV4LuckClutch 대응)
        public bool v4ClutchRolled;
        public bool v4ClutchActive;

        // ★ 구간별 포지션 추적
        public bool[] segRecorded;        // 각 체크포인트 통과 여부

        // ★ 랩 구간별 순위 추적 (25%/50%/100%)
        public bool[] lapCP25Done;
        public bool[] lapCP50Done;
        public bool[] lapCP100Done;
        public int[]  lapRankAt25;
        public int[]  lapRankAt50;
        public int[]  lapRankAt100;
    }

    private struct SlingshotReserve
    {
        public SimRacer racer;
        public float triggerTime;
        public float boost;
        public float duration;
    }

    private Dictionary<int, float> pairCooldowns = new Dictionary<int, float>();
    private List<SlingshotReserve> slingshotQueue = new List<SlingshotReserve>();

    // ★ GC 방지: 재사용 리스트
    private List<int> _expiredKeys = new List<int>(32);
    private List<int> _tempKeys = new List<int>(32);

    // ══════════════════════════════════════
    //  통계 구조
    // ══════════════════════════════════════

    private class CharStats
    {
        public string name;
        public string type;
        public int raceCount;
        public int winCount;
        public int top3Count;
        public int totalRank;

        // ★ 완주 HP 집계 (헤드리스 밸런스 백테스트 — 6랩 전멸 판정용)
        public float finishHpSum;   // 종료 시점 HP 비율 합 (완주=결승선 HP, 미완주=타임아웃 시점 HP)
        public int   finishHpCount; // 집계 표본 수 (= raceCount)
        public int   finishHpLow;   // HP ≤ 5% 로 종료한 수 (거의 고갈)
        public int   dnfCount;      // 미완주(300s 타임아웃) 수 (HP 고갈로 완주 실패)

        // ★ 클러치 발동/미발동 분리 집계 (LuckClutch 홀더만 — 튜닝 스윙 가시화)
        public int clutchProcRaces, clutchProcWins;
        public int clutchNoProcRaces, clutchNoProcWins;

        // 이벤트 합계
        public int totalCrits;
        public int totalCollisionWins;
        public int totalCollisionLosses;
        public int totalDodges;
        public int totalSlingshots;
        public float totalDistLost;
        public float totalDistGained;

        // ★ 스탯 기여 합계
        public float totalContrib_speed;
        public float totalContrib_type;
        public float totalContrib_endurance;
        public float totalContrib_calm;
        public float totalContrib_luck;
        public float totalContrib_power;
        public float totalContrib_brave;

        // 기본 평균
        public float AvgRank => raceCount > 0 ? (float)totalRank / raceCount : 0;
        public float WinRate => raceCount > 0 ? (float)winCount / raceCount : 0;
        public float Top3Rate => raceCount > 0 ? (float)top3Count / raceCount : 0;
        public float AvgCrits => raceCount > 0 ? (float)totalCrits / raceCount : 0;
        public float AvgCollWins => raceCount > 0 ? (float)totalCollisionWins / raceCount : 0;
        public float AvgCollLosses => raceCount > 0 ? (float)totalCollisionLosses / raceCount : 0;
        public float AvgDodges => raceCount > 0 ? (float)totalDodges / raceCount : 0;
        public float AvgSlingshots => raceCount > 0 ? (float)totalSlingshots / raceCount : 0;
        public float AvgDistLost => raceCount > 0 ? totalDistLost / raceCount : 0;
        public float AvgDistGained => raceCount > 0 ? totalDistGained / raceCount : 0;
        public float AvgNetGain => AvgDistGained - AvgDistLost;

        // ★ 완주 HP 평균/비율
        public float AvgFinishHp => finishHpCount > 0 ? finishHpSum / finishHpCount : 0;
        public float LowHpRate   => finishHpCount > 0 ? (float)finishHpLow / finishHpCount : 0;
        public float DnfRate     => finishHpCount > 0 ? (float)dnfCount / finishHpCount : 0;
        public float ClutchProcWinRate   => clutchProcRaces   > 0 ? (float)clutchProcWins   / clutchProcRaces   : 0;
        public float ClutchNoProcWinRate => clutchNoProcRaces > 0 ? (float)clutchNoProcWins / clutchNoProcRaces : 0;
        public float ClutchProcRate      => (clutchProcRaces + clutchNoProcRaces) > 0 ? (float)clutchProcRaces / (clutchProcRaces + clutchNoProcRaces) : 0;

        // ★ 스탯 기여 평균
        public float AvgContrib_speed => raceCount > 0 ? totalContrib_speed / raceCount : 0;
        public float AvgContrib_type => raceCount > 0 ? totalContrib_type / raceCount : 0;
        public float AvgContrib_endurance => raceCount > 0 ? totalContrib_endurance / raceCount : 0;
        public float AvgContrib_calm => raceCount > 0 ? totalContrib_calm / raceCount : 0;
        public float AvgContrib_luck => raceCount > 0 ? totalContrib_luck / raceCount : 0;
        public float AvgContrib_power => raceCount > 0 ? totalContrib_power / raceCount : 0;
        public float AvgContrib_brave => raceCount > 0 ? totalContrib_brave / raceCount : 0;
        public float AvgContrib_total => AvgContrib_speed + AvgContrib_type + AvgContrib_endurance
            + AvgContrib_calm + AvgContrib_luck + AvgContrib_power + AvgContrib_brave;
    }

    // ══════════════════════════════════════
    //  트랙별 결과 구조
    // ══════════════════════════════════════

    private class TrackResult
    {
        public string trackName;
        public string trackId;
        public Dictionary<string, CharStats> stats;
        public int globalCrits, globalCollisions, globalDodges, globalSlingshots;

        // ★ 구간별 타입 순위 추적
        public Dictionary<string, float[]> typeSegRankSum;
        public Dictionary<string, int[]> typeSegRankCount;

        // ★ 랩×구간 타입 순위 추적 (index = lap*3 + phase, phase: 0=25%, 1=50%, 2=100%)
        public Dictionary<string, float[]> typeLapPhaseRankSum;
        public Dictionary<string, int[]>   typeLapPhaseRankCount;
        public int lapPhaseSimLaps;

        // ★ SPEC-031.P2 INT 진단 (bucket: 0=INT 0-9, 1=INT 10-14, 2=INT 15-20)
        public Dictionary<string, float[]> intDiagDrainSum;   // [typeName][bucket]
        public Dictionary<string, float[]> intDiagEntrySum;   // [typeName][bucket]
        public Dictionary<string, int[]>   intDiagCount;       // [typeName][bucket]
    }

    // ══════════════════════════════════════
    //  전체 트랙 시뮬레이션
    // ══════════════════════════════════════

    private void RunAllTracksSimulation()
    {
        isRunning = true;
        lastLogPath = "";
        try {
        RunAllTracksSimulationInternal();
        } catch (System.Exception e) {
            resultText = "❌ 에러: " + e.Message + "\n" + e.StackTrace;
            Debug.LogError("[백테스팅] " + e);
        } finally {
            EditorUtility.ClearProgressBar();
            isRunning = false;
        }
    }

    private void RunAllTracksSimulationInternal()
    {

        // CSV에서 트랙 목록 로드
        TextAsset trackCSV = Resources.Load<TextAsset>("Data/TrackDB");
        List<TrackInfo> trackInfos = new List<TrackInfo>();
        if (trackCSV != null)
        {
            string[] tLines = trackCSV.text.Split('\n');
            for (int i = 1; i < tLines.Length; i++)
            {
                string tl = tLines[i].Trim();
                if (string.IsNullOrEmpty(tl)) continue;
                TrackInfo ti = TrackInfo.ParseCSVLine(tl);
                if (ti != null) trackInfos.Add(ti);
            }
        }

        // 캐릭터 로드
        List<CharacterData> allChars = LoadAllCharacters();
        if (allChars == null || allChars.Count == 0) return;

        List<TrackResult> allResults = new List<TrackResult>();

        // null 트랙 (일반) + 각 트랙
        List<TrackData> trackDataList = new List<TrackData>();
        List<string> trackNames = new List<string>();
        List<string> trackIds = new List<string>();

        trackDataList.Add(null);
        trackNames.Add("일반(없음)");
        trackIds.Add("none");

        foreach (var ti in trackInfos)
        {
            trackDataList.Add(ti.ToTrackData());
            trackNames.Add(ti.trackId);
            trackIds.Add(ti.trackId);
        }

        int totalRuns = trackDataList.Count;
        cancelRequested = false;
        for (int t = 0; t < totalRuns; t++)
        {
            if (cancelRequested) break;
            selectedTrack = trackDataList[t];

            var result = RunSimulationCore(allChars, t, totalRuns, trackNames[t]);
            result.trackName = trackNames[t];
            result.trackId = trackIds[t];
            allResults.Add(result);
        }

        if (allResults.Count > 0)
            BuildAllTracksResult(allResults, allChars);
        else
            resultText = "⚠️ 취소됨 또는 결과 없음";
    }

    // ══════════════════════════════════════
    //  단일 트랙 시뮬레이션
    // ══════════════════════════════════════

    private void RunSingleTrackSimulation()
    {
        isRunning = true;
        lastLogPath = "";
        try
        {
            List<CharacterData> allChars = LoadAllCharacters();
            if (allChars == null || allChars.Count == 0) return;

            cancelRequested = false;
            var result = RunSimulationCore(allChars, 0, 1, selectedTrack != null ? selectedTrack.trackName : "일반");
            result.trackName = selectedTrack != null ? selectedTrack.trackName : "일반";
            result.trackId = selectedTrack != null ? selectedTrack.trackName : "none";

            List<TrackResult> results = new List<TrackResult> { result };
            if (!cancelRequested)
                BuildAllTracksResult(results, allChars);
            else
                resultText = "⚠️ 취소됨";
        }
        catch (System.Exception e)
        {
            resultText = "❌ 에러: " + e.Message + "\n" + e.StackTrace;
            Debug.LogError("[백테스팅] " + e);
        }
        finally
        {
            EditorUtility.ClearProgressBar();
            isRunning = false;
        }
    }

    // ══════════════════════════════════════
    //  ★ SPEC-033 통제 단일스탯 스윕 (에디터 전용)
    // ══════════════════════════════════════

    private void RunStatSweep()
    {
        isRunning = true; lastLogPath = "";
        try
        {
            var allChars = LoadAllCharacters();
            if (allChars == null || allChars.Count == 0) return;
            HiddenStatWeights.Reload();   // CSV 편집 즉시 반영 (튜닝 루프)
            statSweepMode = true;
            equalStats = false;
            selectedTrack = null;
            cancelRequested = false;
            var result = RunSimulationCore(allChars, 0, 1, "sweep");
            BuildSweepResult(result, allChars);
        }
        catch (System.Exception e)
        {
            resultText = "❌ sweep 에러: " + e.Message + "\n" + e.StackTrace;
            Debug.LogError("[sweep] " + e);
        }
        finally
        {
            statSweepMode = false;
            EditorUtility.ClearProgressBar();
            isRunning = false;
        }
    }

    private void BuildSweepResult(TrackResult result, List<CharacterData> allChars)
    {
        string[] names = { "SPD", "ACC", "POW", "STA", "INT", "LUCK" };
        string[] typeNames = { "Runner", "Leader", "Chaser", "Reckoner" };
        int rc = Mathf.Min(simRacers, allChars.Count);
        int st = Mathf.Clamp(sweepStat, 0, 5);

        var xs = new List<double>();
        var ys = new List<double>();
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("# 스탯 스윕 — " + names[st]);
        sb.AppendLine();
        sb.AppendLine(string.Format("> 스탯={0} 타입={1} base={2} step={3} N={4} seedBase={5} laps={6}",
            names[st], typeNames[Mathf.Clamp(sweepType, 0, 3)], sweepBase, sweepStep,
            simCount, sweepSeedBase, simLaps));
        sb.AppendLine();
        sb.AppendLine("| 버킷 | 스탯값 | 출전 | 1착 | 승률% |");
        sb.AppendLine("|------|--------|------|-----|-------|");
        for (int i = 0; i < rc; i++)
        {
            long races = (_sweepBucketRaces != null && i < _sweepBucketRaces.Length) ? _sweepBucketRaces[i] : 0;
            long wins  = (_sweepBucketWins  != null && i < _sweepBucketWins.Length)  ? _sweepBucketWins[i]  : 0;
            int sv = sweepBase + i * sweepStep;
            double wr = races > 0 ? 100.0 * wins / races : 0.0;
            xs.Add(sv); ys.Add(wr);
            sb.AppendLine(string.Format("| {0} | {1} | {2} | {3} | {4:0.0} |",
                i, sv, races, wins, wr));
        }

        // 선형회귀 y = a + βx  (β = Δ승률%p / +1스탯pt)
        int n = xs.Count;
        double beta = 0, r2 = 0;
        if (n >= 2)
        {
            double mx = 0, my = 0;
            for (int i = 0; i < n; i++) { mx += xs[i]; my += ys[i]; }
            mx /= n; my /= n;
            double sxy = 0, sxx = 0, syy = 0;
            for (int i = 0; i < n; i++)
            {
                double dx = xs[i] - mx, dy = ys[i] - my;
                sxy += dx * dy; sxx += dx * dx; syy += dy * dy;
            }
            beta = sxx > 0 ? sxy / sxx : 0;
            r2 = (sxx > 0 && syy > 0) ? (sxy * sxy) / (sxx * syy) : 0;
        }
        sb.AppendLine();
        sb.AppendLine(string.Format("**β = {0:0.0000} %p/+1pt | R² = {1:0.000}**", beta, r2));

        resultText = sb.ToString();
        if (saveLog)
        {
            string dir = Path.Combine("Docs", "logs");
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            string fn = string.Format("sweep_{0}_{1}.md", names[st],
                System.DateTime.Now.ToString("yyyyMMdd_HHmmss"));
            lastLogPath = Path.Combine(dir, fn);
            File.WriteAllText(lastLogPath, sb.ToString());
        }
    }

    // ══════════════════════════════════════
    //  캐릭터 로드
    // ══════════════════════════════════════

    private List<CharacterData> LoadAllCharacters()
    {
        TextAsset csv = Resources.Load<TextAsset>("Data/CharacterDB_V4");
        if (csv == null) { resultText = "❌ CharacterDB.csv를 찾을 수 없습니다!"; return null; }

        List<CharacterData> allChars = new List<CharacterData>();
        string[] lines = csv.text.Split('\n');
        for (int i = 1; i < lines.Length; i++)
        {
            string line = lines[i].Trim();
            if (string.IsNullOrEmpty(line)) continue;
            var cd = CharacterData.ParseCSVLine(line);
            if (cd != null) allChars.Add(cd);
        }
        if (allChars.Count == 0) { resultText = "❌ 캐릭터 데이터 비어있음!"; return null; }

        // ★ V4 데이터 로드
        v4DataMap = new Dictionary<string, CharacterDataV4>();
        TextAsset v4csv = Resources.Load<TextAsset>("Data/CharacterDB_V4");
        if (v4csv != null)
        {
            string[] v4lines = v4csv.text.Split('\n');
            for (int i = 1; i < v4lines.Length; i++)
            {
                string vl = v4lines[i].Trim();
                if (string.IsNullOrEmpty(vl)) continue;
                var v4d = CharacterDataV4.ParseCSVLine(vl);
                if (v4d != null) v4DataMap[v4d.charId] = v4d;
            }
        }

        return allChars;
    }

    // ══════════════════════════════════════
    //  ★ 헤드리스 배치 진입점 (MCP/자동화용 — Play 불필요, 백그라운드 OK)
    // ══════════════════════════════════════

    /// <summary>
    /// CSV(TrackDB)에서 trackId로 TrackData를 즉석 빌드(Alt B: asset 미의존).
    /// 런타임 TrackDatabase.ApplyTrackForRound과 동일 경로(TrackInfo.ToTrackData) → 백테스트=실게임 동일 데이터.
    /// trackId null/미발견 → null(일반 트랙).
    /// </summary>
    private static TrackData BuildTrackFromCsv(string trackId)
    {
        if (string.IsNullOrEmpty(trackId)) return null;
        var csv = Resources.Load<TextAsset>("Data/TrackDB");
        if (csv == null) return null;
        var lines = csv.text.Split('\n');
        for (int i = 1; i < lines.Length; i++)
        {
            var line = lines[i].Trim();
            if (string.IsNullOrEmpty(line)) continue;
            var info = TrackInfo.ParseCSVLine(line);
            if (info != null && info.trackId == trackId) return info.ToTrackData();
        }
        Debug.LogWarning("[BuildTrackFromCsv] trackId 미발견: " + trackId + " → 일반 트랙");
        return null;
    }

    /// <summary>
    /// 단일 트랙 N회 시뮬을 동기 실행하고 요약 문자열 반환. trackId 지정 시 해당 트랙(CSV 빌드), null이면 일반.
    /// EditorApplication.update 의존이 없어 백그라운드에서도 즉시 완료. Play 모드 불필요.
    /// </summary>
    public static string RunHeadless(int simCount, int laps, int racers, bool collision, int seed, string trackId = null)
    {
        var w = CreateInstance<RaceBacktestWindow>();
        try
        {
            w.simCount      = Mathf.Max(1, simCount);
            w.simLaps       = Mathf.Clamp(laps, 1, 10);
            w.simRacers     = Mathf.Clamp(racers, 2, 12);
            w.simCollision  = collision;
            w.selectedTrack = BuildTrackFromCsv(trackId);   // 지정 시 CSV 빌드, null이면 일반
            w.runAllTracks  = false;
            w.statSweepMode = false;
            w.headlessMode  = true;
            w.headlessSeed  = seed;
            w.headlessMaxTicks = 0;
            w.headlessStallRaces = 0;
            w.gameSettings  = Resources.Load<GameSettings>("GameSettings");
            if (w.gameSettings == null) return "❌ GameSettings 로드 실패 (Resources/GameSettings)";

            var allChars = w.LoadAllCharacters();
            if (allChars == null || allChars.Count == 0) return "❌ 캐릭터 로드 실패";

            var sw = System.Diagnostics.Stopwatch.StartNew();
            var result = w.RunSimulationCore(allChars, 0, 1, "headless");
            sw.Stop();

            return w.BuildHeadlessSummary(result, sw.ElapsedMilliseconds);
        }
        finally
        {
            Object.DestroyImmediate(w);
        }
    }

    private string BuildHeadlessSummary(TrackResult result, long ms)
    {
        var sb = new StringBuilder();
        sb.AppendFormat("[headless] {0}race x {1}lap x {2}racers | {3}ms\n", simCount, simLaps, simRacers, ms);
        sb.AppendFormat("[stall] maxTick={0} (limit {1}) / stallRaces(simTime>=300)={2}\n",
            headlessMaxTicks, Mathf.CeilToInt(300f / simTimeStep), headlessStallRaces);

        // ★ FINISH-HP (전멸 판정): 전체 레이서 종료 시점 HP 집계
        float sumHp = 0f; int cntHp = 0, lowHp = 0, dnf = 0;
        foreach (var s in result.stats.Values) { sumHp += s.finishHpSum; cntHp += s.finishHpCount; lowHp += s.finishHpLow; dnf += s.dnfCount; }
        sb.AppendFormat("[finish-hp] avgHP={0:P1} / hp<=5%={1}/{2}({3:P1}) / dnf={4}/{2}({5:P1})\n",
            cntHp > 0 ? sumHp / cntHp : 0, lowHp, cntHp, cntHp > 0 ? (float)lowHp / cntHp : 0,
            dnf, cntHp > 0 ? (float)dnf / cntHp : 0);

        var sorted = result.stats.Values.Where(s => s.raceCount > 0).OrderByDescending(s => s.WinRate).ToList();
        sb.AppendLine("top by win% (name win%(w/n) avgRank):");
        int c = 0;
        foreach (var s in sorted)
        {
            if (c++ >= 12) break;
            sb.AppendFormat("  {0}  {1:P0}({2}/{3})  avg={4:F2}\n", s.name, s.WinRate, s.winCount, s.raceCount, s.AvgRank);
        }
        return sb.ToString();
    }

    // ══════════════════════════════════════
    //  ★ 멀티랩 스윕 (2~6랩 자동 반복 + 로그 저장) — 최종 밸런스 판정 산출물
    // ══════════════════════════════════════

    private class CharLapStat   // charId × lap 승률 병합 단위
    {
        public int wins;
        public int count;
        public int totalRank;
        public float WinRate => count > 0 ? (float)wins / count : 0;
        public float AvgRank => count > 0 ? (float)totalRank / count : 0;
    }

    private class FinishHpStat  // lap별 완주 HP 병합 단위 (전멸 판정)
    {
        public float sumHp;
        public int count;
        public int hp0;   // hp <= 5%
        public int dnf;   // 미완주(300s)
        public float AvgHp   => count > 0 ? sumHp / count : 0;
        public float LowRate => count > 0 ? (float)hp0 / count : 0;
        public float DnfRate => count > 0 ? (float)dnf / count : 0;
    }

    /// <summary>
    /// 여러 랩 거리(예: 2·3·4·5·6)를 각 perLapRaces회 시뮬 → charId 기준 병합 → 통합 리포트 저장.
    /// 랩 간 동일 seed(페어드)로 race별 동일 출전 필드 → 거리 변수만 격리, 매트릭스 셀 누락 0.
    /// </summary>
    public static string RunLapSweepHeadless(int perLapRaces, int[] laps, int racers, int seed, string trackId = null)
    {
        if (laps == null || laps.Length == 0) return "❌ laps 비어있음";
        var w = CreateInstance<RaceBacktestWindow>();
        try
        {
            w.simCount      = Mathf.Max(1, perLapRaces);
            w.simRacers     = Mathf.Clamp(racers, 2, 12);
            w.simCollision  = true;
            w.selectedTrack = BuildTrackFromCsv(trackId);
            w.runAllTracks  = false;
            w.statSweepMode = false;
            w.headlessMode  = true;
            w.gameSettings  = Resources.Load<GameSettings>("GameSettings");
            if (w.gameSettings == null) return "❌ GameSettings 로드 실패 (Resources/GameSettings)";

            var allChars = w.LoadAllCharacters();
            if (allChars == null || allChars.Count == 0) return "❌ 캐릭터 로드 실패";

            var charLap = new Dictionary<string, Dictionary<int, CharLapStat>>();
            var typeMap = new Dictionary<string, string>();
            var hpByLap = new Dictionary<int, FinishHpStat>();
            var stallByLap = new Dictionary<int, string>();

            var swAll = System.Diagnostics.Stopwatch.StartNew();
            foreach (int L in laps)
            {
                w.simLaps            = Mathf.Clamp(L, 1, 10);
                w.headlessSeed       = seed;   // 페어드: 랩 무관 동일 필드 (거리 격리)
                w.headlessMaxTicks   = 0;
                w.headlessStallRaces = 0;

                var tr = w.RunSimulationCore(allChars, 0, 1, "sweep-L" + L);

                var hp = new FinishHpStat();
                foreach (var kv in tr.stats)
                {
                    var s = kv.Value;
                    if (s.raceCount == 0) continue;
                    if (!typeMap.ContainsKey(kv.Key)) typeMap[kv.Key] = s.type;
                    if (!charLap.ContainsKey(kv.Key)) charLap[kv.Key] = new Dictionary<int, CharLapStat>();
                    charLap[kv.Key][L] = new CharLapStat { wins = s.winCount, count = s.raceCount, totalRank = s.totalRank };
                    hp.sumHp += s.finishHpSum; hp.count += s.finishHpCount; hp.hp0 += s.finishHpLow; hp.dnf += s.dnfCount;
                }
                hpByLap[L] = hp;
                stallByLap[L] = string.Format("L{0}: maxTick={1} / stall300s={2}", L, w.headlessMaxTicks, w.headlessStallRaces);
            }
            swAll.Stop();

            string report = BuildSweepReport(laps, charLap, hpByLap, typeMap, stallByLap,
                                             perLapRaces, w.simRacers, seed, allChars.Count, swAll.ElapsedMilliseconds);
            string path = SaveSweepReport(report, seed);
            return report + "\n\nsaved → " + path;
        }
        finally
        {
            Object.DestroyImmediate(w);
        }
    }

    private static string BuildSweepReport(int[] laps,
        Dictionary<string, Dictionary<int, CharLapStat>> charLap,
        Dictionary<int, FinishHpStat> hpByLap,
        Dictionary<string, string> typeMap,
        Dictionary<int, string> stallByLap,
        int perLapRaces, int racers, int seed, int totalChars, long ms)
    {
        var sb = new StringBuilder();
        int totalRaces = perLapRaces * laps.Length;
        sb.AppendLine("# 헤드리스 밸런스 스윕 리포트");
        sb.AppendFormat("- 실행: {0}\n", System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
        sb.AppendFormat("- 설정: laps=[{0}] / perLap={1}race / racers={2} / seed={3} → 총 {4}race\n",
            string.Join(",", laps), perLapRaces, racers, seed, totalRaces);
        sb.AppendFormat("- 소요: {0}ms / 병합 charId={1} (전체 {2} — 누락/중복 0 기대)\n", ms, charLap.Count, totalChars);
        sb.AppendLine();

        // 캐릭터별 overall (전 랩 합산)
        var overall = charLap.Select(kv =>
        {
            int wtot = 0, ctot = 0;
            foreach (var ls in kv.Value.Values) { wtot += ls.wins; ctot += ls.count; }
            return new { id = kv.Key, type = typeMap.ContainsKey(kv.Key) ? typeMap[kv.Key] : "?", win = ctot > 0 ? (float)wtot / ctot : 0f, nPerLap = ctot / Mathf.Max(1, laps.Length) };
        }).OrderByDescending(x => x.win).ToList();

        // CHAR×DIST
        sb.AppendLine("## CHAR×DIST — 캐릭터별 랩 거리 win% (overall 내림차순)");
        sb.AppendLine("```");
        sb.Append("charId".PadRight(34)).Append("type".PadRight(10)).Append("overall".PadRight(9));
        foreach (int L in laps) sb.Append(("L" + L).PadRight(8));
        sb.AppendLine("n/lap");
        foreach (var x in overall)
        {
            sb.Append(x.id.PadRight(34)).Append(x.type.PadRight(10)).Append(x.win.ToString("P0").PadRight(9));
            foreach (int L in laps)
            {
                string cell = charLap[x.id].ContainsKey(L) ? charLap[x.id][L].WinRate.ToString("P0") : "-";
                sb.Append(cell.PadRight(8));
            }
            sb.AppendLine(x.nPerLap.ToString());
        }
        sb.AppendLine("```");
        sb.AppendLine();

        // TYPE×DIST (타입은 typeMap에서 수집 — GetTypeName 표기 무관 정합)
        var types = charLap.Keys.Select(id => typeMap.ContainsKey(id) ? typeMap[id] : "?").Distinct().OrderBy(t => t).ToList();
        sb.AppendLine("## TYPE×DIST — 타입별 랩 거리 win%");
        sb.AppendLine("```");
        sb.Append("type".PadRight(12));
        foreach (int L in laps) sb.Append(("L" + L).PadRight(9));
        sb.AppendLine();
        foreach (var tp in types)
        {
            sb.Append(tp.PadRight(12));
            foreach (int L in laps)
            {
                int w = 0, c = 0;
                foreach (var kv in charLap)
                {
                    if ((typeMap.ContainsKey(kv.Key) ? typeMap[kv.Key] : "?") != tp) continue;
                    if (kv.Value.ContainsKey(L)) { w += kv.Value[L].wins; c += kv.Value[L].count; }
                }
                sb.Append((c > 0 ? ((float)w / c).ToString("P0") : "-").PadRight(9));
            }
            sb.AppendLine();
        }
        sb.AppendLine("```");
        sb.AppendLine();

        // FINISH-HP by lap
        sb.AppendLine("## FINISH-HP — 랩별 완주 시점 HP (전멸 판정)");
        sb.AppendLine("```");
        sb.AppendLine("lap    avgHP      hp<=5%     dnf        samples");
        foreach (int L in laps)
        {
            var hp = hpByLap[L];
            sb.AppendFormat("L{0,-5} {1,-10} {2,-10} {3,-10} {4}\n", L,
                hp.AvgHp.ToString("P1"), hp.LowRate.ToString("P1"), hp.DnfRate.ToString("P1"), hp.count);
        }
        sb.AppendLine("```");
        sb.AppendLine();

        // 스톨 계측 요약
        sb.AppendLine("## STALL 계측 (simTime>=300s 도달 race 수 = 0 기대)");
        sb.AppendLine("```");
        foreach (int L in laps) sb.AppendLine(stallByLap.ContainsKey(L) ? stallByLap[L] : ("L" + L + ": -"));
        sb.AppendLine("```");
        sb.AppendLine();

        // ★ 자동 판정 — (a) 전거리 지배 캐릭터 / (b) 랩별 전멸 위험
        sb.AppendLine("## 판정 (자동 임계)");
        sb.AppendLine("```");
        float fair = racers > 0 ? 1f / racers : 0.125f;
        sb.AppendFormat("공정승률(1/racers)={0:P1} | 지배임계: overall>=30% AND 전랩>=20% | 전멸임계: dnf>=5% OR avgHP<10%\n", fair);

        var doms = overall.Where(x =>
        {
            if (x.win < 0.30f) return false;
            foreach (int L in laps)
                if (!charLap[x.id].ContainsKey(L) || charLap[x.id][L].WinRate < 0.20f) return false;
            return true;
        }).ToList();
        if (doms.Count == 0)
            sb.AppendLine("[지배] 없음 — 전 거리(2~6) 지배 캐릭터 미검출");
        else
            foreach (var d in doms)
                sb.AppendFormat("[지배] {0} — overall {1:P0} (전 랩 20% 이상) → 타입/거리 무관 최상위\n", d.id, d.win);

        bool anyWipe = false;
        foreach (int L in laps)
        {
            var hp = hpByLap[L];
            if (hp.DnfRate >= 0.05f || hp.AvgHp < 0.10f)
            {
                anyWipe = true;
                sb.AppendFormat("[전멸] L{0} — dnf {1:P1} / avgHP {2:P1} (임계 초과)\n", L, hp.DnfRate, hp.AvgHp);
            }
        }
        if (!anyWipe)
            sb.AppendLine("[전멸] 없음 — 전 랩 완주 정상 (6바퀴 포함 실행 가능)");
        sb.AppendLine("```");

        return sb.ToString();
    }

    private static string SaveSweepReport(string content, int seed)
    {
        try
        {
            string root = Directory.GetParent(Application.dataPath).FullName;
            string dir = Path.Combine(root, "Docs", "logs");
            Directory.CreateDirectory(dir);
            string ts = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string path = Path.Combine(dir, "backtest_sweep_" + ts + ".md");
            File.WriteAllText(path, content, new System.Text.UTF8Encoding(false));
            return path;
        }
        catch (System.Exception e)
        {
            return "저장 실패: " + e.Message;
        }
    }

    // ══════════════════════════════════════
    //  ★ 클러치 튜닝 스윕 (chance × bonus 격자) — goyo 운빨형 파라미터 탐색
    // ══════════════════════════════════════

    /// <summary>
    /// 클러치 (chance × bonus) 격자 튜닝 스윕 — 대상 캐릭터의 overall/L2/L6 win% + 발동/미발동 win% 비교표.
    /// 파일 수정 없이 static 오버라이드로 조합 주입. 동일 seed로 조합 간 공정 비교(RNG 시퀀스 동일).
    /// </summary>
    public static string RunClutchTuningSweep(int perComboRaces, int[] laps, int racers, int seed,
                                              float[] chances, float[] bonuses, string targetId, string trackId = null)
    {
        if (laps == null || laps.Length == 0 || chances == null || bonuses == null) return "❌ 인자 비어있음";
        var w = CreateInstance<RaceBacktestWindow>();
        float? savedChance = headlessClutchChance, savedBonus = headlessClutchBonus;
        var sb = new StringBuilder();
        try
        {
            w.simRacers     = Mathf.Clamp(racers, 2, 12);
            w.simCollision  = true;
            w.selectedTrack = BuildTrackFromCsv(trackId); w.runAllTracks = false; w.statSweepMode = false;
            w.headlessMode  = true;
            w.gameSettings  = Resources.Load<GameSettings>("GameSettings");
            if (w.gameSettings == null) return "❌ GameSettings 로드 실패";
            var allChars = w.LoadAllCharacters();
            if (allChars == null || allChars.Count == 0) return "❌ 캐릭터 로드 실패";

            sb.AppendLine("# 클러치 튜닝 스윕");
            sb.AppendFormat("- 대상: {0} / laps=[{1}] / perCombo={2}race/랩 / racers={3} / seed={4}\n",
                targetId, string.Join(",", laps), perComboRaces, w.simRacers, seed);
            sb.AppendLine("- proc%=실제 발동율(≈chance sanity) / win|proc=발동 시 승률 / win|noproc=미발동 시 승률 (스윙 크기)");
            sb.AppendLine("```");
            sb.AppendLine("chance bonus | overall   L2      L6    | proc%  win|proc  win|noproc");
            var sw = System.Diagnostics.Stopwatch.StartNew();
            foreach (float ch in chances)
            foreach (float bo in bonuses)
            {
                headlessClutchChance = ch;
                headlessClutchBonus  = bo;
                int wOv=0,nOv=0, wL2=0,nL2=0, wL6=0,nL6=0, pR=0,pW=0,nR=0,nW=0;
                foreach (int L in laps)
                {
                    w.simCount = Mathf.Max(1, perComboRaces);
                    w.simLaps  = Mathf.Clamp(L, 1, 10);
                    w.headlessSeed = seed; w.headlessMaxTicks = 0; w.headlessStallRaces = 0;
                    var tr = w.RunSimulationCore(allChars, 0, 1, "clutch");
                    if (!tr.stats.TryGetValue(targetId, out var s)) continue;
                    wOv += s.winCount; nOv += s.raceCount;
                    pR += s.clutchProcRaces; pW += s.clutchProcWins;
                    nR += s.clutchNoProcRaces; nW += s.clutchNoProcWins;
                    if (L == 2) { wL2 += s.winCount; nL2 += s.raceCount; }
                    if (L == 6) { wL6 += s.winCount; nL6 += s.raceCount; }
                }
                float ov = nOv>0?(float)wOv/nOv:0, l2 = nL2>0?(float)wL2/nL2:0, l6 = nL6>0?(float)wL6/nL6:0;
                float prc = (pR+nR)>0?(float)pR/(pR+nR):0, wp = pR>0?(float)pW/pR:0, wn = nR>0?(float)nW/nR:0;
                sb.AppendFormat("{0:0.00}   {1:0.00} | {2,6:P0} {3,6:P0} {4,6:P0} | {5,5:P0}  {6,6:P0}   {7,6:P0}\n",
                    ch, bo, ov, l2, l6, prc, wp, wn);
            }
            sw.Stop();
            sb.AppendFormat("```\n소요: {0}ms\n", sw.ElapsedMilliseconds);
            string report = sb.ToString();
            string path = SaveSweepReport(report, seed);
            return report + "\nsaved → " + path;
        }
        finally
        {
            headlessClutchChance = savedChance;
            headlessClutchBonus  = savedBonus;
            Object.DestroyImmediate(w);
        }
    }

    // ══════════════════════════════════════
    //  핵심 시뮬레이션 루프
    // ══════════════════════════════════════

    private TrackResult RunSimulationCore(List<CharacterData> allChars, int trackIndex, int totalTracks, string trackName)
    {
        int racerCount = Mathf.Min(simRacers, allChars.Count);
        var gs = gameSettings;
        HiddenStatWeights.Reload();   // SPEC-033: 에디터 재실행마다 CSV 최신값 반영 (stale 방지)

        Dictionary<string, CharStats> stats = new Dictionary<string, CharStats>();
        foreach (var c in allChars)
            stats[c.charId] = new CharStats { name = c.charId, type = c.GetTypeName() };

        int globalCollisions = 0, globalDodges = 0, globalSlingshots = 0, globalCrits = 0;
        float totalTrackLength = 17f;
        float finishDistance = totalTrackLength * simLaps;

        // ★ 구간별 포지션 추적 초기화
        int segCount = segCheckpoints.Length + 1; // +1 for 결승
        Dictionary<string, float[]> typeSegRankSum = new Dictionary<string, float[]>();
        Dictionary<string, int[]> typeSegRankCount = new Dictionary<string, int[]>();
        foreach (string typeName in new[] { "도주", "선행", "선입", "추입" })
        {
            typeSegRankSum[typeName] = new float[segCount];
            typeSegRankCount[typeName] = new int[segCount];
        }

        // ★ SPEC-031.P2 INT 진단 초기화 (bucket: 0=INT 0-9, 1=INT 10-14, 2=INT 15-20)
        var intDiagDrainSum  = new Dictionary<string, float[]>();
        var intDiagEntrySum  = new Dictionary<string, float[]>();
        var intDiagCount     = new Dictionary<string, int[]>();
        foreach (string typeName in new[] { "도주", "선행", "선입", "추입" })
        {
            intDiagDrainSum[typeName] = new float[3];
            intDiagEntrySum[typeName] = new float[3];
            intDiagCount[typeName]    = new int[3];
        }

        // ★ 랩×구간 순위 추적 초기화 (phase: 0=25%, 1=50%, 2=100%)
        int lapPhaseCount = simLaps * 3;
        Dictionary<string, float[]> typeLapPhaseRankSum   = new Dictionary<string, float[]>();
        Dictionary<string, int[]>   typeLapPhaseRankCount = new Dictionary<string, int[]>();
        foreach (string typeName in new[] { "도주", "선행", "선입", "추입" })
        {
            typeLapPhaseRankSum[typeName]   = new float[lapPhaseCount];
            typeLapPhaseRankCount[typeName] = new int[lapPhaseCount];
        }

        if (statSweepMode)
        {
            _sweepBucketWins  = new long[racerCount];
            _sweepBucketRaces = new long[racerCount];
        }

        for (int race = 0; race < simCount; race++)
        {
            // ★ SPEC-033: 스윕 모드 시드 고정 (재현성·before/after 차분)
            if (statSweepMode) UnityEngine.Random.InitState(sweepSeedBase + race);
            else if (headlessMode) UnityEngine.Random.InitState(headlessSeed + race);   // 재현성: 매 race 시드 고정
            int[] slotBucket = null;   // 슬롯i → 값버킷 (로테이션으로 위치편향 상쇄)

            // 선발
            List<CharacterData> selected;
            if (statSweepMode)
            {
                // 결정적 선발: 앞 racerCount개 (서로 다른 charId = 슬롯)
                selected = new List<CharacterData>();
                for (int i = 0; i < racerCount && i < allChars.Count; i++)
                    selected.Add(allChars[i]);
            }
            else
            {
                selected = new List<CharacterData>(allChars);
                while (selected.Count > racerCount)
                    selected.RemoveAt(Random.Range(0, selected.Count));
            }

            // ★ SPEC-033 통제 스윕 오버라이드: 전원 동일 가상캐릭, 슬롯 i에 1스탯만 차등
            if (statSweepMode && gs.useV4RaceSystem && v4DataMap != null)
            {
                slotBucket = new int[selected.Count];
                for (int i = 0; i < selected.Count; i++)
                {
                    // 로테이션: 값버킷이 레이스마다 슬롯을 순회 → 위치편향 상쇄
                    int bucket = (i + race) % selected.Count;
                    slotBucket[i] = bucket;
                    var cd = selected[i];
                    cd.charBaseSpeed = cd.charBasePower = cd.charBaseBrave =
                        cd.charBaseCalm = cd.charBaseEndurance = cd.charBaseLuck = sweepBase;
                    cd.charAbility = "";
                    CharacterDataV4 dvs;
                    if (v4DataMap.TryGetValue(cd.charId, out dvs))
                    {
                        int b = sweepBase, v = sweepBase + bucket * sweepStep;
                        dvs.v4Speed = b; dvs.v4Accel = b; dvs.v4Stamina = b;
                        dvs.v4Power = b; dvs.v4Intelligence = b; dvs.v4Luck = b;
                        switch (sweepStat)
                        {
                            case 0: dvs.v4Speed = v; break;
                            case 1: dvs.v4Accel = v; break;
                            case 2: dvs.v4Power = v; break;
                            case 3: dvs.v4Stamina = v; break;
                            case 4: dvs.v4Intelligence = v; break;
                            case 5: dvs.v4Luck = v; break;
                        }
                        dvs.charType = (CharacterType)sweepType;
                        dvs.skillData = null;
                        dvs.passiveData = null;
                    }
                }
            }

            // 균등 스탯 오버라이드
            if (equalStats)
            {
                foreach (var cd in selected)
                {
                    cd.charBaseSpeed = equalStatValue;
                    cd.charBasePower = equalStatValue;
                    cd.charBaseBrave = equalStatValue;
                    cd.charBaseCalm = equalStatValue;
                    cd.charBaseEndurance = equalStatValue;
                    cd.charBaseLuck = equalStatValue;
                }
                // V4 스탯도 오버라이드
                if (gs.useV4RaceSystem && v4DataMap != null)
                {
                    foreach (var cd in selected)
                    {
                        CharacterDataV4 dv4;
                        if (v4DataMap.TryGetValue(cd.charId, out dv4))
                        {
                            dv4.v4Speed = equalStatValue;
                            dv4.v4Accel = equalStatValue;
                            dv4.v4Stamina = equalStatValue;
                            dv4.v4Power = equalStatValue;
                            dv4.v4Intelligence = equalStatValue;
                            dv4.v4Luck = equalStatValue;
                        }
                    }
                }
            }

            List<SimRacer> racers = new List<SimRacer>();
            _activeRacers = racers; // FormationHold 페이즈에서 상행 그룹 탐색용
            foreach (var cd in selected)
            {
                var racer = new SimRacer
                {
                    data = cd, position = 0f,
                    currentSpeed = GetBaseSpeed(cd) * 0.5f,
                    noiseTimer = 0f, luckTimer = 0f,
                    critRemaining = 0f, critCount = 0,
                    collisionPenalty = 0f, collisionTimer = 0f,
                    slingshotBoost = 0f, slingshotTimer = 0f,
                    finished = false, finishOrder = 0
                };

                // ★ V4 초기화
                if (gs.useV4RaceSystem && v4DataMap != null)
                {
                    CharacterDataV4 dv4 = null;
                    v4DataMap.TryGetValue(cd.charId, out dv4);
                    racer.dataV4 = dv4;
                    if (dv4 != null)
                    {
                        var gs4 = gs.v4Settings;
                        racer.v4MaxStamina = gs4.v4_staminaBase + (dv4.v4Stamina * HiddenStatWeights.Stamina) * gs4.v4_staminaPerStat;
                        racer.v4Stamina = racer.v4MaxStamina;
                        racer.v4CurrentSpeed = gs.globalSpeedMultiplier * 0.5f;
                        racer.v4LastProgress = 0f;
                        racer.v4BurstDrainAccum = 0f;
                        racer.v4BurstFirstEntry = -1f;
                        // burst zone width for drain normalization (SPEC-031.P2)
                        switch (dv4.charType)
                        {
                            case CharacterType.Runner:   racer.v4BurstZoneWidth = gs4.v4_runnerBurstEnd   - gs4.v4_runnerBurstStart;   break;
                            case CharacterType.Leader:   racer.v4BurstZoneWidth = gs4.v4_leaderBurstEnd   - gs4.v4_leaderBurstStart;   break;
                            case CharacterType.Chaser:   racer.v4BurstZoneWidth = gs4.v4_chaserBurstEnd   - gs4.v4_chaserBurstStart;   break;
                            default:                     racer.v4BurstZoneWidth = gs4.v4_reckonerBurstEnd - gs4.v4_reckonerBurstStart; break;
                        }
                        racer.v4EmergencyBurst = false;
                        racer.v4EmergencyBurstCooldownTimer = 0f;
                        racer.v4CritBoostRemaining = 0f;
                        racer.v4LuckTimer = 0f;
                        racer.v4InSlipstream = false;
                        racer.v4SlipstreamDrainMul = 1f;
                        racer.currentRank = 0;
                        // V4 uses its own HP fields — mirror to enduranceHP for shared stats
                        racer.maxHP = racer.v4MaxStamina;
                        racer.enduranceHP = racer.v4MaxStamina;
                        // 스킬 초기화
                        racer.skillActive = false;
                        racer.skillRemainingTime = 0f;
                        racer.skillCollisionCount = 0;
                        racer.skillCooldownTimer = 0f;
                        racer.skillHpTriggered = false;
                        racer.skillRankTriggered = false;
                        racer.passiveConditionActive = false;
                        racer.passiveCooldownTimer = 0f;
                        racer.v4ClutchRolled = false;
                        racer.v4ClutchActive = false;
                    }
                }

                // 구간 추적 초기화
                racer.segRecorded = new bool[segCheckpoints.Length];

                // 랩 구간 순위 추적 초기화
                racer.lapCP25Done  = new bool[simLaps];
                racer.lapCP50Done  = new bool[simLaps];
                racer.lapCP100Done = new bool[simLaps];
                racer.lapRankAt25  = new int[simLaps];
                racer.lapRankAt50  = new int[simLaps];
                racer.lapRankAt100 = new int[simLaps];

                racers.Add(racer);
                stats[cd.charId].raceCount++;
            }

            pairCooldowns.Clear();
            slingshotQueue.Clear();

            int finishedCount = 0;
            float simTime = 0f;

            while (finishedCount < racerCount && simTime < 300f)
            {
                simTime += simTimeStep;

                // ═══ 순위 + 슬립스트림 갱신 ═══
                if (gs.useV4RaceSystem)
                {
                    for (int ri = 0; ri < racers.Count; ri++)
                    {
                        if (racers[ri].finished) continue;

                        // 순위 계산
                        int rank = 1;
                        float myPos = racers[ri].position;
                        float closestGap = float.MaxValue;
                        for (int rj = 0; rj < racers.Count; rj++)
                        {
                            if (ri == rj || racers[rj].finished) continue;
                            if (racers[rj].position > myPos) rank++;
                            float gap = (racers[rj].position - myPos) / finishDistance;
                            if (gap > 0f && gap < closestGap) closestGap = gap;
                        }
                        racers[ri].currentRank = rank;

                        if (gs.useV4RaceSystem)
                        {
                            // ═══ V4 슬립스트림: 범위 내 앞 캐릭터 감지 ═══
                            var gs4 = gs.v4Settings;
                            float myProg = Mathf.Clamp01(myPos / finishDistance);
                            bool inStream = false;
                            if (gs4 != null)
                            {
                                float ssUnlock = SimGetV4SlipstreamUnlock(racers[ri], gs4);
                                if (myProg >= ssUnlock && closestGap < gs4.v4_slipstreamRange && closestGap > 0f)
                                    inStream = true;
                            }
                            racers[ri].v4InSlipstream = inStream;
                            racers[ri].v4SlipstreamDrainMul = inStream ? gs4.v4_slipstreamDrainMul : 1f;
                        }
                        else
                        {
                            // V1-V3 슬립스트림/CP 코드 제거됨 (V4 전용)
                        }
                    }
                }

                foreach (var r in racers)
                {
                    if (r.finished) continue;

                    float progress = Mathf.Clamp01(r.position / finishDistance);

                    if (gs.useV4RaceSystem && r.dataV4 != null)
                    {
                        // ═══ V4 속도 계산 + 이동 ═══
                        SimTickV4(r, racers, gs, simTime, finishDistance);
                    }
                    else
                    {
                        // ═══ V1-V3 속도 계산 + 이동 ═══
                        float baseTarget = CalcSpeed(r, progress, simTime);

                        // 충돌 감속
                        float gameDtTimer = simTimeStep * gs.globalSpeedMultiplier;
                        float penaltyMul = 1f;
                        if (r.collisionTimer > 0f)
                        {
                            r.collisionTimer -= gameDtTimer;
                            penaltyMul = 1f - r.collisionPenalty;
                            float distLost = r.currentSpeed * r.collisionPenalty * simTimeStep;
                            r.totalDistLost += distLost;
                            r.contrib_power -= distLost;
                            if (r.collisionTimer <= 0f) r.collisionPenalty = 0f;
                        }

                        // 슬링샷 가속
                        float slingshotMul = 1f;
                        if (r.slingshotTimer > 0f)
                        {
                            r.slingshotTimer -= gameDtTimer;
                            slingshotMul = 1f + r.slingshotBoost;
                            float distGained = r.currentSpeed * r.slingshotBoost * simTimeStep;
                            r.totalDistGained += distGained;
                            r.contrib_brave += distGained;
                            if (r.slingshotTimer <= 0f) r.slingshotBoost = 0f;
                        }

                        float targetSpeed = baseTarget * penaltyMul * slingshotMul;
                        float effectiveLerp = 3f; // Legacy fallback lerp
                        r.currentSpeed = Mathf.Lerp(r.currentSpeed, targetSpeed, simTimeStep * effectiveLerp);
                        r.position += r.currentSpeed * simTimeStep;
                    }

                    // ★ 랩 구간별 순위 기록 (25%/50%/100%)
                    {
                        float lapF = r.position / totalTrackLength;
                        int   lapI = Mathf.Min((int)lapF, simLaps - 1);
                        float frac = lapF - (int)lapF;
                        if (lapI >= 0 && lapI < simLaps)
                        {
                            if (!r.lapCP25Done[lapI] && frac >= 0.25f) { r.lapRankAt25[lapI] = r.currentRank; r.lapCP25Done[lapI] = true; }
                            if (!r.lapCP50Done[lapI] && frac >= 0.50f) { r.lapRankAt50[lapI] = r.currentRank; r.lapCP50Done[lapI] = true; }
                        }
                        // 완료된 이전 랩 100% 기록
                        for (int prevLap = 0; prevLap < lapI && prevLap < simLaps; prevLap++)
                        {
                            if (!r.lapCP100Done[prevLap]) { r.lapRankAt100[prevLap] = r.currentRank; r.lapCP100Done[prevLap] = true; }
                        }
                    }

                    // ★ 구간별 포지션 기록
                    float prog = Mathf.Clamp01(r.position / finishDistance);
                    for (int si = 0; si < segCheckpoints.Length; si++)
                    {
                        if (!r.segRecorded[si] && prog >= segCheckpoints[si])
                        {
                            r.segRecorded[si] = true;
                            string segType = r.data.GetTypeName();
                            if (typeSegRankSum.ContainsKey(segType))
                            {
                                typeSegRankSum[segType][si] += r.currentRank;
                                typeSegRankCount[segType][si]++;
                            }
                        }
                    }

                    if (r.position >= finishDistance)
                    {
                        r.finished = true;
                        finishedCount++;
                        r.finishOrder = finishedCount;

                        // ★ 결승 순위 기록
                        string finType = r.data.GetTypeName();
                        if (typeSegRankSum.ContainsKey(finType))
                        {
                            typeSegRankSum[finType][segCheckpoints.Length] += r.finishOrder;
                            typeSegRankCount[finType][segCheckpoints.Length]++;
                        }
                    }
                }

                // 충돌 판정
                if (simCollision && gs.enableCollision)
                {
                    SimCollisions(racers, gs, simTime);
                    SimSlingshotQueue(racers, gs, simTime);
                }

                UpdateSimCooldowns();
            }

            // ★ 헤드리스 스톨 진단: race당 최대 tick / 300s 상한 도달 카운트
            if (headlessMode)
            {
                int ticks = Mathf.CeilToInt(simTime / simTimeStep);
                if (ticks > headlessMaxTicks) headlessMaxTicks = ticks;
                if (simTime >= 300f) headlessStallRaces++;
            }

            // 미완주
            var unfinished = racers.Where(r => !r.finished).OrderByDescending(r => r.position).ToList();
            for (int i = 0; i < unfinished.Count; i++) { finishedCount++; unfinished[i].finishOrder = finishedCount; }

            // ★ SPEC-033 값버킷 집계 (racers 순서 == selected/slotBucket 순서)
            if (statSweepMode && slotBucket != null)
            {
                for (int k = 0; k < racers.Count && k < slotBucket.Length; k++)
                {
                    int bk = slotBucket[k];
                    _sweepBucketRaces[bk]++;
                    if (racers[k].finishOrder == 1) _sweepBucketWins[bk]++;
                }
            }

            // 통계 수집
            foreach (var r in racers)
            {
                var s = stats[r.data.charId];
                s.totalRank += r.finishOrder;
                s.totalCrits += r.critCount;
                s.totalCollisionWins += r.collisionWins;
                s.totalCollisionLosses += r.collisionLosses;
                s.totalDodges += r.dodgeCount;
                s.totalSlingshots += r.slingshotCount;
                s.totalDistLost += r.totalDistLost;
                s.totalDistGained += r.totalDistGained;

                // ★ 스탯 기여 수집
                s.totalContrib_speed += r.contrib_speed;
                s.totalContrib_type += r.contrib_type;
                s.totalContrib_endurance += r.contrib_endurance;
                s.totalContrib_calm += r.contrib_calm;
                s.totalContrib_luck += r.contrib_luck;
                s.totalContrib_power += r.contrib_power;
                s.totalContrib_brave += r.contrib_brave;

                if (r.finishOrder == 1) s.winCount++;
                if (r.finishOrder <= 3) s.top3Count++;

                // ★ 완주 HP 집계 (6랩 전멸 판정용) — 완주=결승선 HP, 미완주=타임아웃 시점 HP
                float finHp = r.v4MaxStamina > 0f ? Mathf.Clamp01(r.v4Stamina / r.v4MaxStamina) : 0f;
                s.finishHpSum += finHp;
                s.finishHpCount++;
                if (finHp <= 0.05f) s.finishHpLow++;
                if (!r.finished)    s.dnfCount++;

                // ★ 클러치 발동/미발동별 승패 (LuckClutch 홀더만)
                if (r.dataV4?.passiveData?.triggerType == PassiveTriggerType.LuckClutch)
                {
                    if (r.v4ClutchActive) { s.clutchProcRaces++;   if (r.finishOrder == 1) s.clutchProcWins++; }
                    else                  { s.clutchNoProcRaces++; if (r.finishOrder == 1) s.clutchNoProcWins++; }
                }

                // ★ SPEC-031.P2 INT 진단 수집 (V4 + burst 진입 기록 있는 레이서만)
                if (r.dataV4 != null)
                {
                    string diagType = r.data.GetTypeName();
                    if (intDiagCount.ContainsKey(diagType))
                    {
                        int intVal  = (int)r.dataV4.v4Intelligence;
                        int bucket  = intVal < 10 ? 0 : (intVal < 15 ? 1 : 2);
                        intDiagDrainSum[diagType][bucket] += r.v4BurstDrainAccum;
                        if (r.v4BurstFirstEntry >= 0f)
                            intDiagEntrySum[diagType][bucket] += r.v4BurstFirstEntry;
                        intDiagCount[diagType][bucket]++;
                    }
                }

                // ★ 랩 구간별 순위 누적
                string tname = r.data.GetTypeName();
                if (typeLapPhaseRankSum.ContainsKey(tname))
                {
                    for (int lap = 0; lap < simLaps; lap++)
                    {
                        if (r.lapCP25Done[lap])  { typeLapPhaseRankSum[tname][lap*3+0] += r.lapRankAt25[lap];  typeLapPhaseRankCount[tname][lap*3+0]++; }
                        if (r.lapCP50Done[lap])  { typeLapPhaseRankSum[tname][lap*3+1] += r.lapRankAt50[lap];  typeLapPhaseRankCount[tname][lap*3+1]++; }
                        if (r.lapCP100Done[lap]) { typeLapPhaseRankSum[tname][lap*3+2] += r.lapRankAt100[lap]; typeLapPhaseRankCount[tname][lap*3+2]++; }
                    }
                }

                globalCrits += r.critCount;
                globalCollisions += r.collisionWins;
                globalDodges += r.dodgeCount;
                globalSlingshots += r.slingshotCount;
            }

            if (!headlessMode && race % 10 == 0)
            {
                float overallProgress = ((float)trackIndex * simCount + race) / (totalTracks * simCount);
                string msg = string.Format("트랙 {0}/{1} [{2}]  레이스 {3}/{4}",
                    trackIndex + 1, totalTracks, trackName, race, simCount);
                bool cancelled = EditorUtility.DisplayCancelableProgressBar("백테스팅", msg, overallProgress);
                if (cancelled) { cancelRequested = true; break; }
            }
        }

        if (!runAllTracks && !headlessMode) EditorUtility.ClearProgressBar();

        return new TrackResult
        {
            stats = stats,
            globalCrits = globalCrits,
            globalCollisions = globalCollisions,
            globalDodges = globalDodges,
            globalSlingshots = globalSlingshots,
            typeSegRankSum = typeSegRankSum,
            typeSegRankCount = typeSegRankCount,
            typeLapPhaseRankSum   = typeLapPhaseRankSum,
            typeLapPhaseRankCount = typeLapPhaseRankCount,
            lapPhaseSimLaps       = simLaps,
            intDiagDrainSum  = intDiagDrainSum,
            intDiagEntrySum  = intDiagEntrySum,
            intDiagCount     = intDiagCount,
        };
    }

    // ══════════════════════════════════════
    //  충돌 시뮬레이션 (기존 유지)
    // ══════════════════════════════════════

    private void SimCollisions(List<SimRacer> racers, GameSettings gs, float simTime)
    {
        if (simTime < gs.collisionSettlingTime) return;
        float range = gs.collisionRange;
        TrackData track = selectedTrack;
        if (track != null) range *= track.collisionRangeMultiplier;

        for (int i = 0; i < racers.Count; i++)
        {
            if (racers[i].finished || racers[i].collisionTimer > 0f) continue;

            for (int j = i + 1; j < racers.Count; j++)
            {
                if (racers[j].finished || racers[j].collisionTimer > 0f) continue;

                float dist = Mathf.Abs(racers[i].position - racers[j].position);
                if (dist >= range) continue;

                int pairKey = Mathf.Min(i, j) * 100 + Mathf.Max(i, j);
                if (pairCooldowns.ContainsKey(pairKey) && pairCooldowns[pairKey] > 0f) continue;

                if (gs.crowdThreshold > 0)
                {
                    int nearby = 0;
                    for (int k = 0; k < racers.Count; k++)
                    {
                        if (!racers[k].finished && Mathf.Abs(racers[k].position - racers[i].position) < range)
                            nearby++;
                    }
                    if (nearby >= gs.crowdThreshold && Random.value > gs.crowdDampen) continue;
                }

                if (Random.value > gs.collisionChance) continue;

                SimResolve(racers[i], racers[j], gs, track, simTime);
                pairCooldowns[pairKey] = gs.collisionCooldown;
            }
        }
    }

    private void SimResolve(SimRacer a, SimRacer b, GameSettings gs, TrackData track, float simTime)
    {
        // V4 스킬 충돌 카운트 증가 (양쪽 모두 — CollisionTrigger 발동 체크 포함)
        if (gs.useV4RaceSystem)
        {
            // 뒤에 있는 쪽 = 추격자, 앞에 있는 쪽 = 피격자 (이후 slingshot용 behind와 분리)
            SimRacer skillBehind = a.position <= b.position ? a : b;
            SimRacer skillAhead  = (skillBehind == a) ? b : a;
            SimOnSkillCollisionHit(skillBehind, isChasing: true);
            SimOnSkillCollisionHit(skillAhead, isChasing: false);
        }

        SimRacer winner, loser;

        // CollisionWin 스킬 활성 여부 체크
        bool aArmed = gs.useV4RaceSystem && a.skillActive
                   && a.dataV4?.skillData?.effectType == SkillEffectType.CollisionWin;
        bool bArmed = gs.useV4RaceSystem && b.skillActive
                   && b.dataV4?.skillData?.effectType == SkillEffectType.CollisionWin;

        if (aArmed && !bArmed) { winner = a; loser = b; }
        else if (bArmed && !aArmed) { winner = b; loser = a; }
        else
        {
        float powerA = a.data.charBasePower;
        float powerB = b.data.charBasePower;
        float effA = powerA;
        float effB = powerB;
        if (powerA > powerB) effA = powerA * (1f + powerA / (powerA + powerB));
        else if (powerB > powerA) effB = powerB * (1f + powerB / (powerA + powerB));

        float totalEff = effA + effB;
        float bWinChance = totalEff > 0f ? effB / totalEff : 0.5f;

        if (Random.value < bWinChance) { winner = b; loser = a; }
        else { winner = a; loser = b; }
        }

        // luck 회피
        float trackLuckMul = track != null ? track.luckMultiplier : 1f;
        float dodgeChance = loser.data.charBaseLuck * gs.v4Settings.v4_intDodgeChance * trackLuckMul;
        if (Random.value < dodgeChance)
        {
            loser.dodgeCount++;
            return;
        }

        float trackPenMul = track != null ? track.collisionPenaltyMultiplier : 1f;
        float trackLoserDurMul = track != null ? track.loserPenaltyDurationMultiplier : 1f;

        winner.collisionPenalty = gs.collisionBasePenalty * 0.5f * trackPenMul;
        winner.collisionTimer = gs.winnerPenaltyDuration;
        winner.collisionWins++;

        loser.collisionPenalty = gs.collisionBasePenalty * trackPenMul;
        loser.collisionTimer = gs.loserPenaltyDuration * trackLoserDurMul;
        loser.collisionLosses++;

        SimRacer behind = a.position <= b.position ? a : b;
        float brave = behind.data.charBaseBrave;
        float slingshotMul = track != null ? track.slingshotMultiplier : 1f;
        float boost = brave * gs.slingshotFactor * slingshotMul;
        float behindDur = (behind == loser) ? loser.collisionTimer : winner.collisionTimer;

        slingshotQueue.Add(new SlingshotReserve
        {
            racer = behind, triggerTime = simTime + behindDur,
            boost = boost, duration = gs.slingshotDuration
        });
    }

    private void SimSlingshotQueue(List<SimRacer> racers, GameSettings gs, float simTime)
    {
        for (int i = slingshotQueue.Count - 1; i >= 0; i--)
        {
            var res = slingshotQueue[i];
            if (res.racer.finished) { slingshotQueue.RemoveAt(i); continue; }
            if (simTime >= res.triggerTime)
            {
                res.racer.slingshotBoost = Mathf.Min(res.boost, gs.slingshotMaxBoost);
                res.racer.slingshotTimer = res.duration;
                res.racer.slingshotCount++;
                slingshotQueue.RemoveAt(i);
            }
        }
    }

    private void UpdateSimCooldowns()
    {
        _expiredKeys.Clear();
        _tempKeys.Clear();
        foreach (var kv in pairCooldowns) _tempKeys.Add(kv.Key);
        for (int i = 0; i < _tempKeys.Count; i++)
        {
            int k = _tempKeys[i];
            float v = pairCooldowns[k] - simTimeStep;
            pairCooldowns[k] = v;
            if (v <= 0f) _expiredKeys.Add(k);
        }
        for (int i = 0; i < _expiredKeys.Count; i++) pairCooldowns.Remove(_expiredKeys[i]);
    }

    // ══════════════════════════════════════
    //  속도 계산 + 스탯 기여 추적
    // ══════════════════════════════════════

    private float CalcSpeed(SimRacer r, float progress, float simTime)
    {
        var gs = gameSettings;
        var cd = r.data;
        TrackData track = selectedTrack;

        float trackSpeedMul = track != null ? track.speedMultiplier : 1f;
        float globalMul = gs.globalSpeedMultiplier;
        float baseSpeed = cd.SpeedMultiplier * globalMul * trackSpeedMul;

        // ★ speed 기여: SpeedMultiplier 중 0.8 기준선 초과분
        float speedContrib = (cd.SpeedMultiplier - 0.8f) * globalMul * trackSpeedMul * simTimeStep;
        r.contrib_speed += speedContrib;

        // noise (calm) + CP/HP 불안정 배율
        r.noiseTimer -= simTimeStep * globalMul;
        if (r.noiseTimer <= 0f)
        {
            float calm = Mathf.Max(cd.charBaseCalm, 1f);
            float trackNoiseMul = track != null ? track.noiseMultiplier : 1f;
            float maxNoise = (1f / calm) * gs.noiseFactor * trackNoiseMul * globalMul;

            // CP/HP 불안정 배율 (곱연산)
            float cpRatio = r.maxCP > 0f ? r.calmPoints / r.maxCP : 0f;
            float hpRatio = r.maxHP > 0f ? r.enduranceHP / r.maxHP : 1f;
            maxNoise *= gs.GetCPNoiseMul(cpRatio) * gs.GetHPNoiseMul(hpRatio);

            r.noiseValue = Random.Range(-maxNoise, maxNoise);
            r.noiseTimer = Random.Range(0.5f, 1.5f);
        }
        // ★ calm 기여
        r.contrib_calm += r.noiseValue * simTimeStep;

        float powerBonus = 0f, braveBonus = 0f;
        if (track != null)
        {
            powerBonus = (cd.charBasePower / 20f) * track.powerSpeedBonus;
            braveBonus = (cd.charBaseBrave / 20f) * track.braveSpeedBonus;
        }

        float slowMul = 1f;
        if (track != null && track.hasMidSlowZone)
        {
            if (progress >= track.midSlowZoneStart && progress <= track.midSlowZoneEnd)
                slowMul = track.midSlowZoneSpeedMultiplier;
        }

        // luck crit
        float critMul = 1f;
        float gameDtLuck = simTimeStep * globalMul;
        if (r.critRemaining > 0f)
        {
            r.critRemaining -= gameDtLuck;
            critMul = gs.luckCritBoost;
            float critGain = r.currentSpeed * (gs.luckCritBoost - 1f) * simTimeStep;
            r.totalDistGained += critGain;
            r.contrib_luck += critGain;  // ★ luck 기여
            if (r.critRemaining <= 0f) r.isCrit = false;
        }
        else
        {
            r.luckTimer -= gameDtLuck;
            if (r.luckTimer <= 0f)
            {
                r.luckTimer = gs.luckCheckInterval;
                float trackLuckMul = track != null ? track.luckMultiplier : 1f;
                float chance = cd.charBaseLuck * gs.luckCritChance * trackLuckMul;
                if (Random.value < chance)
                {
                    r.critRemaining = gs.luckCritDuration;
                    r.isCrit = true;
                    r.critCount++;
                    critMul = gs.luckCritBoost;
                }
            }
        }

        float speed = baseSpeed * (1f + powerBonus + braveBonus);
        speed += r.noiseValue;

        speed *= slowMul * critMul;
        return Mathf.Max(speed, 0.1f);
    }


    private float GetBaseSpeed(CharacterData cd)
    {
        float trackMul = selectedTrack != null ? selectedTrack.speedMultiplier : 1f;
        return cd.SpeedMultiplier * gameSettings.globalSpeedMultiplier * trackMul;
    }

    // ══════════════════════════════════════
    //  Race V4 시뮬레이션 (RacerController_V4 미러)
    // ══════════════════════════════════════

    /// <summary>
    /// V4 레이서 1명의 1틱 업데이트: 긴급부스트 + 럭크릿 + 속도계산 + 스태미나 드레인 + 이동
    /// </summary>
    private void SimTickV4(SimRacer r, List<SimRacer> racers, GameSettings gs, float simTime, float finishDistance)
    {
        var gs4 = gs.v4Settings;
        var dv4 = r.dataV4;
        if (gs4 == null || dv4 == null) return;

        float progress = Mathf.Clamp01(r.position / finishDistance);
        float dt = simTimeStep;
        float gameDt = dt * gs.globalSpeedMultiplier;

        // ── 1. 긴급 부스트 판정 ──
        SimUpdateV4EmergencyBurst(r, gs4, progress, dt);

        // ── 2. Luck 크리티컬 ──
        SimUpdateV4LuckCrit(r, gs4, gameDt);

        // ── 2b. 패시브 스킬 체크 (CheckPassiveSkill 미러) ──
        SimCheckPassiveV4(r, gs4, racers, progress);

        // ── 2c. 클러치 도박 판정 (UpdateV4LuckClutch 미러) ──
        SimUpdateV4LuckClutch(r, gs4, progress);

        // ── 3. 속도 계산 (CalcSpeedV4 미러) ──
        float baseSpeed = gs.globalSpeedMultiplier;
        float vmax = baseSpeed * (1f + (dv4.v4Speed * HiddenStatWeights.Speed) * gs4.v4_speedStatFactor)
                              * (1f + (dv4.v4Power * HiddenStatWeights.Power) * gs4.v4_powerSpeedFactor);

        // ── 트랙 스탯 상성 (CalcSpeedV4 미러: 동일 공유 헬퍼 호출 → 드리프트 불가) ──
        TrackData track = selectedTrack;
        if (gs4.v4_enableTrackStatBonus && track != null)
            vmax *= TrackStatAffinity.ComputeVmaxMultiplier(dv4, track.statAffinities);

        // HP 임계값 기반 속도 배율
        float staminaRatio = r.v4MaxStamina > 0 ? r.v4Stamina / r.v4MaxStamina : 0f;
        float hpSpeedMul = gs4.GetHpSpeedMultiplier(staminaRatio);
        vmax *= hpSpeedMul;

        float accelRate = (dv4.v4Accel * HiddenStatWeights.Accel) * gs4.v4_accelStatFactor;

        // 구간 판별
        bool burstActive = !gs4.v4_disableBurst;
        bool hpAvailable = r.v4Stamina > 0f;
        bool inSpurtZone = burstActive && hpAvailable && progress >= gs4.v4_finalSpurtStart;
        bool inRegularBurst = !inSpurtZone && burstActive && hpAvailable && SimIsInBurstZoneV4(dv4, gs4, progress);
        bool inEmergencyBurst = !inSpurtZone && !inRegularBurst && burstActive && hpAvailable && r.v4EmergencyBurst;

        float target;
        if (inSpurtZone)
        {
            if (!r.v4IsSpurting)
            {
                r.v4IsSpurting = true;
                r.v4SpurtHpRatio = r.v4MaxStamina > 0 ? Mathf.Clamp01(r.v4Stamina / r.v4MaxStamina) : 0f;
            }
            float spurtHpSpeedMul = 1f + r.v4SpurtHpRatio * gs4.v4_spurtHpSpeedBonus;
            float spurtHpAccelMul = 1f + r.v4SpurtHpRatio * gs4.v4_spurtHpAccelBonus;
            target = vmax * gs4.v4_spurtVmaxBonus * spurtHpSpeedMul;
            accelRate *= gs4.v4_spurtAccelBonus * spurtHpAccelMul;
        }
        else if (inRegularBurst)
        {
            target = vmax * gs4.v4_burstSpeedRatio;
        }
        else if (inEmergencyBurst)
        {
            target = vmax * gs4.v4_emergencyBurstSpeedRatio;
        }
        else
        {
            target = vmax * gs4.v4_normalSpeedRatio;
        }

        // Lerp 기반 가속
        r.v4CurrentSpeed = Mathf.Lerp(r.v4CurrentSpeed, target, dt * accelRate);
        float outputSpeed = r.v4CurrentSpeed;

        // 크리티컬 배율
        if (r.v4CritBoostRemaining > 0f)
            outputSpeed *= gs4.v4_luckCritBoost;

        // 클러치 배율 (단발 도박 성공 시 결승까지) — bonus 오버라이드 가능(튜닝)
        if (r.v4ClutchActive && r.dataV4?.passiveData?.effectType == PassiveEffectType.SpeedBonus)
        {
            float clutchBonus = headlessClutchBonus ?? r.dataV4.passiveData.effectValue;
            outputSpeed *= Mathf.Min(clutchBonus, PassiveSkillData.CLUTCH_SPEED_MAX);
        }

        // 액티브 SpeedBoost 배율 (CalcSpeedV4 미러)
        if (r.skillActive && r.dataV4?.skillData?.effectType == SkillEffectType.SpeedBoost)
            outputSpeed *= Mathf.Min(r.dataV4.skillData.effectValue, SkillData.SPEED_BOOST_MAX);

        // 패시브 SpeedBonus 배율
        if (r.passiveConditionActive && r.dataV4?.passiveData?.effectType == PassiveEffectType.SpeedBonus)
            outputSpeed *= Mathf.Min(r.dataV4.passiveData.effectValue, PassiveSkillData.SPEED_BONUS_MAX);

        // 스킬 타이머 감소
        if (r.skillActive)
        {
            r.skillRemainingTime -= gameDt;
            if (r.skillRemainingTime <= 0f) { r.skillActive = false; r.skillRemainingTime = 0f; }
        }

        // 스킬 쿨다운 감소 (UpdateCollisionTimers 미러)
        if (r.skillCooldownTimer > 0f)
        {
            r.skillCooldownTimer -= gameDt;
            if (r.skillCooldownTimer < 0f) r.skillCooldownTimer = 0f;
        }

        // V4 HP/Rank 스킬 트리거 체크 (1회성 — ProcessV4ThinkTick 미러)
        if (!r.skillActive && r.dataV4?.skillData != null && r.dataV4.skillData.triggerType != SkillTriggerType.None)
        {
            var sd = r.dataV4.skillData;
            float hpRatio = r.v4MaxStamina > 0f ? r.v4Stamina / r.v4MaxStamina : 0f;
            if (!r.skillHpTriggered && sd.CheckHpTrigger(hpRatio))
            { r.skillHpTriggered = true; SimActivateSkillV4(r, sd); }
            else if (!r.skillRankTriggered && r.currentRank > 0 && sd.CheckRankTrigger(r.currentRank))
            { r.skillRankTriggered = true; SimActivateSkillV4(r, sd); }
        }

        // 충돌 감속 (기존 시스템과 공유)
        if (r.collisionTimer > 0f)
        {
            r.collisionTimer -= gameDt;
            float penaltyMul = 1f - r.collisionPenalty;
            float distLost = outputSpeed * r.collisionPenalty * dt;
            r.totalDistLost += distLost;
            r.contrib_power -= distLost;
            outputSpeed *= penaltyMul;
            if (r.collisionTimer <= 0f) r.collisionPenalty = 0f;
        }

        // 슬링샷 가속 (기존 충돌 시스템의 보너스, V4에서도 유지)
        if (r.slingshotTimer > 0f)
        {
            r.slingshotTimer -= gameDt;
            float slingshotMul = 1f + r.slingshotBoost;
            float distGained = outputSpeed * r.slingshotBoost * dt;
            r.totalDistGained += distGained;
            r.contrib_brave += distGained;
            outputSpeed *= slingshotMul;
            if (r.slingshotTimer <= 0f) r.slingshotBoost = 0f;
        }

        r.currentSpeed = outputSpeed;
        r.position += outputSpeed * dt;

        // ── 4. V4 스태미나 드레인 (진행도 기반) ──
        float currentProgress = Mathf.Clamp01(r.position / finishDistance);
        float progressDelta = Mathf.Max(0f, currentProgress - r.v4LastProgress);
        r.v4LastProgress = currentProgress;

        if (progressDelta > 0f && r.v4Stamina > 0f)
        {
            float drain = gs4.v4_drainPerLap * progressDelta;

            // 구간별 추가 소모
            if (!gs4.v4_disableBurst)
            {
                if (inSpurtZone) drain *= gs4.v4_spurtDrainMul;
                else if (inRegularBurst)
                {
                    // SPEC-031.P2 미러: 구간폭 정규화 드레인 할인 (RacerController_V4 동기)
                    float avgZoneWidth = ((gs4.v4_runnerBurstEnd - gs4.v4_runnerBurstStart)
                                        + (gs4.v4_leaderBurstEnd - gs4.v4_leaderBurstStart)
                                        + (gs4.v4_chaserBurstEnd - gs4.v4_chaserBurstStart)
                                        + (gs4.v4_reckonerBurstEnd - gs4.v4_reckonerBurstStart)) / 4f;
                    float normFloor = 1f - (1f - gs4.v4_intBurstDrainFloor) * avgZoneWidth / r.v4BurstZoneWidth;
                    float tt = Mathf.Clamp01(((dv4.v4Intelligence * HiddenStatWeights.Intelligence) - 10f) / 10f);
                    drain *= gs4.v4_burstDrainMul * Mathf.Lerp(1f, normFloor, tt);
                    // INT 진단: burst 드레인·첫 진입 기록
                    r.v4BurstDrainAccum += drain;
                    if (r.v4BurstFirstEntry < 0f) r.v4BurstFirstEntry = currentProgress;
                }
                else if (inEmergencyBurst)
                {
                    // 거리별 스케일링 미러 (GameSettingsV4.LapScale, drain 지수 0.5)
                    float lapScale = gs4.LapScale(simLaps, 0.5f);
                    drain *= gs4.GetV4EmergencyBurstDrainMul(dv4.charType) * lapScale;
                }
            }

            // 슬립스트림 드레인 감소
            if (r.v4InSlipstream)
                drain *= r.v4SlipstreamDrainMul;

            // 액티브 DrainReduce 배율 (ConsumeStaminaV4 미러)
            if (r.skillActive && r.dataV4?.skillData?.effectType == SkillEffectType.DrainReduce)
                drain *= Mathf.Max(r.dataV4.skillData.effectValue, SkillData.DRAIN_REDUCE_MIN);
            // 패시브 DrainReduce 배율
            if (r.passiveConditionActive && r.dataV4?.passiveData?.effectType == PassiveEffectType.DrainReduce)
                drain *= Mathf.Max(r.dataV4.passiveData.effectValue, PassiveSkillData.DRAIN_REDUCE_MIN);

            r.v4Stamina = Mathf.Max(0f, r.v4Stamina - drain);
            r.enduranceHP = r.v4Stamina; // 디버그 통계 호환
        }

        // 스탯 기여 추적 (V4: 타입 보너스 = phase 속도 차이)
        float typeContrib = (outputSpeed / baseSpeed - 1f) * dt;
        r.contrib_type += typeContrib;
    }

    /// <summary>V4 긴급 부스트 판정 (UpdateV4EmergencyBurst 미러)</summary>
    private void SimUpdateV4EmergencyBurst(SimRacer r, GameSettingsV4 gs4, float progress, float dt)
    {
        if (!gs4.v4_emergencyBurstEnabled || r.dataV4 == null || gs4.v4_disableBurst) return;

        // 쿨다운 타이머
        if (r.v4EmergencyBurstCooldownTimer > 0f)
            r.v4EmergencyBurstCooldownTimer -= dt;

        // 정규 부스트 구간 또는 스퍼트 구간 안이면 긴급 부스트 비활성
        bool inRegularBurst = SimIsInBurstZoneV4(r.dataV4, gs4, progress);
        bool inSpurt        = progress >= gs4.v4_finalSpurtStart;

        if (inRegularBurst || inSpurt)
        {
            // 정규 부스트/스퍼트 구간 → 긴급 해제
            r.v4EmergencyBurst = false;
            r.v4EmergencyBurstCooldownTimer = 0f;
            return;
        }

        if (r.currentRank <= 0) return;
        var (_, targetMax) = gs4.GetV4TargetRankRange(r.dataV4.charType);
        bool shouldEmergency = r.currentRank > targetMax;

        bool isPersistentRunner = gs4.v4_runnerPersistentBurst &&
                                  r.dataV4.charType == CharacterType.Runner;

        if (shouldEmergency && !r.v4EmergencyBurst)
        {
            if (r.v4EmergencyBurstCooldownTimer > 0f && !isPersistentRunner) return;
            r.v4EmergencyBurst = true;
        }
        else if (!shouldEmergency && r.v4EmergencyBurst && !isPersistentRunner)
        {
            r.v4EmergencyBurst = false;
            if (gs4.v4_emergencyBurstCooldown > 0f)
                r.v4EmergencyBurstCooldownTimer = gs4.v4_emergencyBurstCooldown;
        }
    }

    /// <summary>V4 Luck 크리티컬 판정 (UpdateV4LuckCrit 미러)</summary>
    private void SimUpdateV4LuckCrit(SimRacer r, GameSettingsV4 gs4, float gameDt)
    {
        if (r.dataV4 == null) return;

        if (r.v4CritBoostRemaining > 0f)
        {
            r.v4CritBoostRemaining -= gameDt;
            if (r.v4CritBoostRemaining <= 0f) r.v4CritBoostRemaining = 0f;
            return;
        }

        r.v4LuckTimer -= gameDt;
        if (r.v4LuckTimer <= 0f)
        {
            r.v4LuckTimer = gs4.v4_luckCheckInterval;
            float chance = (r.dataV4.v4Luck * HiddenStatWeights.Luck) * gs4.v4_luckCritChance;
            if (Random.value < chance)
            {
                // 지능 modifier
                float intModifier = ((r.dataV4.v4Intelligence * HiddenStatWeights.Intelligence) - 10f) / 10f * gs4.v4_intelligenceModMax;
                r.v4CritBoostRemaining = gs4.v4_luckCritDuration * (1f + intModifier);
                r.critCount++;
            }
        }
    }

    /// <summary>V4 Luck 클러치 판정 (UpdateV4LuckClutch 미러) — 진행도 gate 도달 시 1회 굴림, 성공 시 결승까지 latch</summary>
    private void SimUpdateV4LuckClutch(SimRacer r, GameSettingsV4 gs4, float progress)
    {
        var pd = r.dataV4?.passiveData;
        if (pd == null || pd.triggerType != PassiveTriggerType.LuckClutch) return;
        if (r.v4ClutchRolled) return;
        if (progress < gs4.v4_clutchGateProgress) return;

        r.v4ClutchRolled = true;   // gate 도달 → 1회만 판정 (roll은 chance 무관 항상 1회 소비 → 조합 간 RNG 동일)
        float chance = headlessClutchChance ?? gs4.v4_clutchChance;
        if (Random.value < chance)
            r.v4ClutchActive = true;
    }

    /// <summary>패시브 스킬 체크 (CheckPassiveSkill 미러)</summary>
    private void SimCheckPassiveV4(SimRacer r, GameSettingsV4 gs4, List<SimRacer> racers, float progress)
    {
        var pd = r.dataV4?.passiveData;
        if (pd == null || pd.triggerType == PassiveTriggerType.None) return;

        // 쿨다운 감소
        if (r.passiveCooldownTimer > 0f)
            r.passiveCooldownTimer -= simTimeStep;

        float hpRatio = r.v4MaxStamina > 0f ? r.v4Stamina / r.v4MaxStamina : 0f;
        int totalRacers = racers.Count;

        bool inBurstZone = SimIsInBurstZoneV4(r.dataV4, gs4, progress);
        bool inSpurtZone = progress >= gs4.v4_finalSpurtStart;

        bool condMet = pd.CheckCondition(totalRacers, r.currentRank, hpRatio, inBurstZone, inSpurtZone);
        r.passiveConditionActive = condMet;

        // 즉발 효과: 조건 충족 + 쿨다운 만료
        if (condMet && r.passiveCooldownTimer <= 0f)
        {
            if (pd.effectType == PassiveEffectType.HpHeal)
            {
                float heal = r.v4MaxStamina * pd.effectValue;
                r.v4Stamina = Mathf.Min(r.v4MaxStamina, r.v4Stamina + heal);
                r.enduranceHP = r.v4Stamina;
                r.passiveCooldownTimer = pd.cooldownSec;
            }
            else if (pd.effectType == PassiveEffectType.CpRegen)
            {
                r.calmPoints = Mathf.Min(r.maxCP, r.calmPoints + r.maxCP * pd.effectValue);
                r.passiveCooldownTimer = pd.cooldownSec;
            }
        }
    }

    /// <summary>V4 스킬 발동 (ActivateSkill 미러 — HpHeal 즉발 / 나머지 타이머 + 쿨다운)</summary>
    private void SimActivateSkillV4(SimRacer r, SkillData sd)
    {
        r.skillCollisionCount = 0;
        if (sd.effectType == SkillEffectType.HpHeal)
        {
            float heal = r.v4MaxStamina * sd.effectValue;
            r.v4Stamina = Mathf.Min(r.v4MaxStamina, r.v4Stamina + heal);
            r.enduranceHP = r.v4Stamina;
        }
        else
        {
            r.skillActive = true;
            r.skillRemainingTime = sd.durationSec;
        }
        // 쿨다운 시작 (LapScale 지수 0.35) — RacerController.ActivateSkill 미러
        if (sd.cooldownSec > 0f)
        {
            var gs4 = GameSettings.Instance?.v4Settings;
            float cdLapScale = gs4 != null ? gs4.LapScale(simLaps, 0.35f) : 1f;
            r.skillCooldownTimer = sd.cooldownSec * cdLapScale;
        }
    }

    /// <summary>충돌 시 스킬 카운트 증가 (OnSkillCollisionHit 미러) — 방향 필터 포함</summary>
    private void SimOnSkillCollisionHit(SimRacer r, bool isChasing)
    {
        if (r.skillActive || r.skillCooldownTimer > 0f) return;
        var sd = r.dataV4?.skillData;
        if (sd == null || sd.triggerType == SkillTriggerType.None) return;

        bool shouldCount;
        switch (sd.triggerType)
        {
            case SkillTriggerType.E_Skill_Collision:  shouldCount = true; break;
            case SkillTriggerType.E_Skill_ChaseHit:   shouldCount = isChasing; break;
            case SkillTriggerType.E_Skill_ChasedHit:  shouldCount = !isChasing; break;
            default: return;
        }
        if (!shouldCount) return;

        r.skillCollisionCount++;
        if (sd.CheckCollisionTrigger(r.skillCollisionCount))
            SimActivateSkillV4(r, sd);
    }

    /// <summary>V4 부스트 구간 판별 (IsInBurstZone 미러)</summary>
    private bool SimIsInBurstZoneV4(CharacterDataV4 dv4, GameSettingsV4 gs4, float progress)
    {
        float start, end;
        switch (dv4.charType)
        {
            case CharacterType.Runner:   start = gs4.v4_runnerBurstStart;   end = gs4.v4_runnerBurstEnd;   break;
            case CharacterType.Leader:   start = gs4.v4_leaderBurstStart;   end = gs4.v4_leaderBurstEnd;   break;
            case CharacterType.Chaser:   start = gs4.v4_chaserBurstStart;   end = gs4.v4_chaserBurstEnd;   break;
            case CharacterType.Reckoner: start = gs4.v4_reckonerBurstStart; end = gs4.v4_reckonerBurstEnd; break;
            default: return false;
        }

        // SPEC-031.P2 미러: 타입 무관 절대 시프트 (폭 비례 제거 — RacerController_V4 동기)
        float intModifier = ((dv4.v4Intelligence * HiddenStatWeights.Intelligence) - 10f) / 10f;
        float shift = gs4.v4_intBurstShiftAbs * intModifier;

        return progress >= start + shift && progress < end + shift;
    }

    /// <summary>V4 부스트 구간 시작점</summary>
    private float SimGetV4BurstStart(CharacterDataV4 dv4, GameSettingsV4 gs4)
    {
        switch (dv4.charType)
        {
            case CharacterType.Runner:   return gs4.v4_runnerBurstStart;
            case CharacterType.Leader:   return gs4.v4_leaderBurstStart;
            case CharacterType.Chaser:   return gs4.v4_chaserBurstStart;
            case CharacterType.Reckoner: return gs4.v4_reckonerBurstStart;
            default: return 0f;
        }
    }

    /// <summary>V4 슬립스트림 해금 진행도</summary>
    private float SimGetV4SlipstreamUnlock(SimRacer r, GameSettingsV4 gs4)
    {
        if (r.dataV4 == null) return 0f;
        switch (r.dataV4.charType)
        {
            case CharacterType.Runner:   return gs4.v4_ssUnlockRunner;
            case CharacterType.Leader:   return gs4.v4_ssUnlockLeader;
            case CharacterType.Chaser:   return gs4.v4_ssUnlockChaser;
            case CharacterType.Reckoner: return gs4.v4_ssUnlockReckoner;
            default: return 0f;
        }
    }

    // ══════════════════════════════════════
    //  결과 빌드 (에디터 표시 + 마크다운 로그)
    // ══════════════════════════════════════

    private void BuildAllTracksResult(List<TrackResult> results, List<CharacterData> allChars)
    {
        // ── UID → ko 이름 매핑 (에디터에서도 한국어 표시) ──
        string prevLang = Loc.CurrentLang;
        if (prevLang != "ko") Loc.SetLang("ko");
        Dictionary<string, string> koNames = new Dictionary<string, string>();
        foreach (var c in allChars)
            koNames[c.charId] = Loc.Get(c.charName);
        if (prevLang != "ko") Loc.SetLang(prevLang);
        // koName 헬퍼 — UID → ko 이름, 실패 시 UID 그대로
        System.Func<string, string> KN = (uid) =>
            koNames.ContainsKey(uid) ? koNames[uid] : uid;

        StringBuilder display = new StringBuilder();
        StringBuilder md = new StringBuilder();
        bool multiTrack = results.Count > 1;

        // ── 헤더 ──
        string settlingInfo = simCollision ? string.Format("ON(settling:{0:F1}s)", GameSettings.Instance.collisionSettlingTime) : "OFF";
        string versionTag = (gameSettings != null && gameSettings.useV4RaceSystem) ? "V4" : "HP";
        string header = string.Format("백테스팅 {0}  |  {1}회 × {2}바퀴 × {3}명  |  충돌:{4}  |  트랙:{5}",
            versionTag, simCount, simLaps, simRacers, settlingInfo,
            multiTrack ? "전체 " + results.Count + "종" : results[0].trackName);

        display.AppendLine("═══════════════════════════════════════════════════════════════════");
        display.AppendLine("  " + header);
        display.AppendLine("═══════════════════════════════════════════════════════════════════");

        md.AppendLine("# 🏇 백테스팅 리포트");
        md.AppendLine();
        md.AppendFormat("> **날짜**: {0}  \n", System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
        md.AppendFormat("> **설정**: {0}회 × {1}바퀴 × {2}명 | 충돌:{3}  \n",
            simCount, simLaps, simRacers, settlingInfo);
        md.AppendFormat("> **트랙**: {0}  \n", multiTrack ? "전체 " + results.Count + "종" : results[0].trackName);
        md.AppendFormat("> **SpeedMultiplier 수식**: `0.8 + charBaseSpeed × 0.01`  \n");
        md.AppendLine();

        // ═══════════════════════════════════
        //  1. 트랙별 캐릭터 순위/승률
        // ═══════════════════════════════════
        foreach (var tr in results)
        {
            var sorted = tr.stats.Values.Where(s => s.raceCount > 0).OrderByDescending(s => s.WinRate).ToList();
            string tn = tr.trackName;

            display.AppendFormat("\n──── [{0}] 순위/승률 ────\n", tn);
            display.AppendLine("  이름   타입  출전  1착  Top3  평균순위  승률     Top3율   크리티컬");

            md.AppendFormat("## {0} 트랙\n\n", tn);
            md.AppendLine("### 순위/승률");
            md.AppendLine();
            md.AppendLine("| 이름 | 타입 | 출전 | 1착 | Top3 | 평균순위 | 승률 | Top3율 | 크리티컬 |");
            md.AppendLine("|------|------|------|-----|------|----------|------|--------|----------|");

            foreach (var s in sorted)
            {
                display.AppendFormat("  {0,-5}{1,-4} {2,4} {3,4} {4,4}   {5,5:F1}    {6,5:F1}%   {7,5:F1}%    {8,4:F2}\n",
                    KN(s.name), s.type, s.raceCount, s.winCount, s.top3Count,
                    s.AvgRank, s.WinRate * 100, s.Top3Rate * 100, s.AvgCrits);

                md.AppendFormat("| {0} | {1} | {2} | {3} | {4} | {5:F1} | {6:F1}% | {7:F1}% | {8:F2} |\n",
                    KN(s.name), s.type, s.raceCount, s.winCount, s.top3Count,
                    s.AvgRank, s.WinRate * 100, s.Top3Rate * 100, s.AvgCrits);
            }
            md.AppendLine();

            // ── 스탯 기여 분석 ──
            display.AppendFormat("\n──── [{0}] 스탯별 기여 (레이스당 평균 거리) [V4] ────\n", tn);
            display.AppendLine("  이름   속도    타입    피로     노이즈   럭      파워    용감    합계");

            md.AppendLine("### 스탯별 기여 (레이스당 평균 거리) — V4");
            md.AppendLine();
            md.AppendLine("| 이름 | 속도(SPD) | 타입(TYPE) | 피로(END) | 노이즈(CALM) | 럭(LUCK) | 파워(POW) | 용감(BRV) | 합계 |");
            md.AppendLine("|------|-----------|------------|-----------|--------------|----------|-----------|-----------|------|");

            var sortedByTotal = sorted.OrderByDescending(s => s.AvgContrib_total).ToList();
            foreach (var s in sortedByTotal)
            {
                display.AppendFormat("  {0,-5}{1,6:F2}  {2,6:F2}  {3,7:F2}  {4,7:F2}  {5,6:F2}  {6,6:F2}  {7,6:F2}  {8,6:F2}\n",
                    KN(s.name), s.AvgContrib_speed, s.AvgContrib_type, s.AvgContrib_endurance,
                    s.AvgContrib_calm, s.AvgContrib_luck, s.AvgContrib_power,
                    s.AvgContrib_brave, s.AvgContrib_total);

                md.AppendFormat("| {0} | {1} | {2} | {3} | {4} | {5} | {6} | {7} | {8} |\n",
                    KN(s.name), SF(s.AvgContrib_speed), SF(s.AvgContrib_type), SF(s.AvgContrib_endurance),
                    SF(s.AvgContrib_calm), SF(s.AvgContrib_luck), SF(s.AvgContrib_power),
                    SF(s.AvgContrib_brave), SF(s.AvgContrib_total));
            }
            md.AppendLine();

            // ── 충돌 손익 ──
            if (simCollision)
            {
                display.AppendFormat("\n──── [{0}] 충돌 손익 ────\n", tn);
                md.AppendLine("### 충돌 손익 (레이스당 평균)");
                md.AppendLine();
                md.AppendLine("| 이름 | POW | BRV | LCK | 충돌승 | 충돌패 | 회피 | 슬링샷 | 잃은거리 | 얻은거리 | 순이득 |");
                md.AppendLine("|------|-----|-----|-----|--------|--------|------|--------|----------|----------|--------|");

                var sortedByNet = sorted.OrderByDescending(s => s.AvgNetGain).ToList();
                foreach (var s in sortedByNet)
                {
                    var cd = FindCharData(s.name);
                    int pow = cd != null ? (int)cd.charBasePower : 0;
                    int brv = cd != null ? (int)cd.charBaseBrave : 0;
                    int lck = cd != null ? (int)cd.charBaseLuck : 0;

                    display.AppendFormat("  {0,-5}{1,3} {2,3} {3,3}  {4,5:F1} {5,5:F1} {6,4:F1} {7,6:F1}  {8,7:F2}  {9,7:F2}  {10}\n",
                        KN(s.name), pow, brv, lck,
                        s.AvgCollWins, s.AvgCollLosses, s.AvgDodges, s.AvgSlingshots,
                        s.AvgDistLost, s.AvgDistGained, SF(s.AvgNetGain));

                    md.AppendFormat("| {0} | {1} | {2} | {3} | {4:F1} | {5:F1} | {6:F1} | {7:F1} | {8:F2} | {9:F2} | {10} |\n",
                        KN(s.name), pow, brv, lck,
                        s.AvgCollWins, s.AvgCollLosses, s.AvgDodges, s.AvgSlingshots,
                        s.AvgDistLost, s.AvgDistGained, SF(s.AvgNetGain));
                }
                md.AppendLine();
            }

            // ── 구간별 타입 평균 순위 ──
            if (tr.typeSegRankSum != null)
            {
                string[] segTypeOrder = { "도주", "선행", "선입", "추입" };

                display.AppendFormat("\n──── [{0}] 구간별 타입 평균 순위 ────\n", tn);
                display.Append("  구간  ");
                foreach (var stn in segTypeOrder) display.AppendFormat(" {0,-6}", stn);
                display.AppendLine();

                md.AppendLine("### 구간별 타입 평균 순위");
                md.AppendLine();
                md.AppendLine("| 구간 | 도주 | 선행 | 선입 | 추입 |");
                md.AppendLine("|------|------|------|------|------|");

                int totalSeg = segCheckpoints.Length + 1;
                for (int si = 0; si < totalSeg; si++)
                {
                    string segLabel = si < segCheckpoints.Length
                        ? string.Format("{0:F0}%", segCheckpoints[si] * 100)
                        : "결승";
                    display.AppendFormat("  {0,-6}", segLabel);
                    md.AppendFormat("| {0} |", segLabel);

                    foreach (var stn in segTypeOrder)
                    {
                        int cnt = tr.typeSegRankCount.ContainsKey(stn) ? tr.typeSegRankCount[stn][si] : 0;
                        float avg = cnt > 0 ? tr.typeSegRankSum[stn][si] / cnt : 0;
                        display.AppendFormat(" {0,5:F2} ", avg);
                        md.AppendFormat(" {0:F2} |", avg);
                    }
                    display.AppendLine();
                    md.AppendLine();
                }
                md.AppendLine();
            }

            // ── 랩별 구간 타입 순위 (25%/50%/완료) ──
            if (tr.typeLapPhaseRankSum != null && tr.lapPhaseSimLaps >= 1)
            {
                string[] lpTypeOrder  = { "도주", "선행", "선입", "추입" };
                string[] phaseLabels  = { "25%", "50%", "완료" };

                display.AppendFormat("\n──── [{0}] 랩별 구간 타입 순위 ────\n", tn);
                display.Append("  구간          ");
                foreach (var stn in lpTypeOrder) display.AppendFormat(" {0,-6}", stn);
                display.AppendLine();

                md.AppendLine("### 랩별 구간 타입 순위");
                md.AppendLine();
                md.AppendLine("| 구간 | 도주 | 선행 | 선입 | 추입 |");
                md.AppendLine("|------|------|------|------|------|");

                for (int lap = 0; lap < tr.lapPhaseSimLaps; lap++)
                {
                    for (int phase = 0; phase < 3; phase++)
                    {
                        string label = string.Format("{0}랩 {1}", lap + 1, phaseLabels[phase]);
                        display.AppendFormat("  {0,-14}", label);
                        md.AppendFormat("| {0} |", label);

                        int idx = lap * 3 + phase;
                        foreach (var stn in lpTypeOrder)
                        {
                            int cnt   = tr.typeLapPhaseRankCount.ContainsKey(stn) ? tr.typeLapPhaseRankCount[stn][idx] : 0;
                            float avg = cnt > 0 ? tr.typeLapPhaseRankSum[stn][idx] / cnt : 0f;
                            display.AppendFormat(" {0,5:F2} ", avg > 0 ? avg : 0f);
                            md.AppendFormat(" {0:F2} |", avg > 0 ? avg : 0f);
                        }
                        display.AppendLine();
                        md.AppendLine();
                    }
                    if (lap < tr.lapPhaseSimLaps - 1) { display.AppendLine(); md.AppendLine(); }
                }
                md.AppendLine();
            }
        }

        // ═══════════════════════════════════
        //  2. 트랙별 비교 (멀티트랙 시)
        // ═══════════════════════════════════
        if (multiTrack)
        {
            md.AppendLine("---");
            md.AppendLine();
            md.AppendLine("## 트랙별 성적 비교");
            md.AppendLine();

            // 헤더
            StringBuilder mdHeader = new StringBuilder("| 이름 | 타입 |");
            StringBuilder mdSep = new StringBuilder("|------|------|");
            foreach (var tr in results) { mdHeader.AppendFormat(" {0} |", tr.trackName); mdSep.Append("------|"); }
            mdHeader.Append(" 편차 |"); mdSep.Append("------|");

            display.AppendLine("\n══════ 트랙별 평균 순위 비교 ══════");
            md.AppendLine(mdHeader.ToString());
            md.AppendLine(mdSep.ToString());

            // 캐릭별 행
            var charIds = results[0].stats.Keys.Where(k => results[0].stats[k].raceCount > 0).ToList();
            foreach (var cn in charIds)
            {
                var cd = FindCharData(cn);
                string typeName = cd != null ? cd.GetTypeName() : "?";

                StringBuilder mdRow = new StringBuilder(string.Format("| {0} | {1} |", KN(cn), typeName));
                List<float> ranks = new List<float>();

                foreach (var tr in results)
                {
                    if (tr.stats.ContainsKey(cn) && tr.stats[cn].raceCount > 0)
                    {
                        float avgRank = tr.stats[cn].AvgRank;
                        ranks.Add(avgRank);
                        mdRow.AppendFormat(" {0:F1} |", avgRank);
                    }
                    else
                    {
                        mdRow.Append(" - |");
                    }
                }

                float stdDev = ranks.Count > 1 ? StdDev(ranks) : 0;
                mdRow.AppendFormat(" {0:F2} |", stdDev);
                md.AppendLine(mdRow.ToString());

                display.AppendFormat("  {0,-5}{1,-4}", KN(cn), typeName);
                foreach (var r in ranks) display.AppendFormat(" {0,5:F1}", r);
                display.AppendFormat("  σ={0:F2}\n", stdDev);
            }
            md.AppendLine();

            // ── 트랙별 타입 평균 ──
            md.AppendLine("### 타입별 트랙 성적");
            md.AppendLine();
            StringBuilder typeHeader = new StringBuilder("| 타입 |");
            StringBuilder typeSep = new StringBuilder("|------|");
            foreach (var tr in results) { typeHeader.AppendFormat(" {0} |", tr.trackName); typeSep.Append("------|"); }
            md.AppendLine(typeHeader.ToString());
            md.AppendLine(typeSep.ToString());

            var typeNames = results[0].stats.Values.Where(s => s.raceCount > 0).Select(s => s.type).Distinct().ToList();
            foreach (var tn in typeNames)
            {
                StringBuilder row = new StringBuilder(string.Format("| {0} |", tn));
                foreach (var tr in results)
                {
                    var group = tr.stats.Values.Where(s => s.raceCount > 0 && s.type == tn);
                    float avg = group.Any() ? group.Average(s => s.AvgRank) : 0;
                    row.AppendFormat(" {0:F1} |", avg);
                }
                md.AppendLine(row.ToString());
            }
            md.AppendLine();
        }

        // ═══════════════════════════════════
        //  2-b. SPEC-031.P2 INT 진단 (V4 전용)
        // ═══════════════════════════════════
        bool hasIntDiag = results.Any(tr => tr.intDiagCount != null &&
                          tr.intDiagCount.Values.Any(v => v.Any(c => c > 0)));
        if (hasIntDiag)
        {
            md.AppendLine("---");
            md.AppendLine();
            md.AppendLine("## 🧠 INT 진단 — burst 드레인 & 첫 진입 progress");
            md.AppendLine();
            md.AppendLine("> bucket: Low=INT 0-9 / Mid=INT 10-14 / High=INT 15-20  ");
            md.AppendLine("> drain ↓ with INT = 함정역전 보존 ✅ / entry ↑ with INT = 늦은 킥 ✅");
            md.AppendLine();
            md.AppendLine("| 타입 | 지표 | Low | Mid | High | 방향성 |");
            md.AppendLine("|------|------|-----|-----|------|--------|");

            var diagTypeOrder = new[] { "도주", "선행", "선입", "추입" };
            foreach (var tr in results)
            {
                if (tr.intDiagCount == null) continue;
                foreach (var typeName in diagTypeOrder)
                {
                    if (!tr.intDiagCount.ContainsKey(typeName)) continue;
                    var cnt   = tr.intDiagCount[typeName];
                    var drain = tr.intDiagDrainSum[typeName];
                    var entry = tr.intDiagEntrySum[typeName];

                    float dLow  = cnt[0] > 0 ? drain[0] / cnt[0] : 0f;
                    float dMid  = cnt[1] > 0 ? drain[1] / cnt[1] : 0f;
                    float dHigh = cnt[2] > 0 ? drain[2] / cnt[2] : 0f;
                    float eLow  = cnt[0] > 0 ? entry[0] / cnt[0] : 0f;
                    float eMid  = cnt[1] > 0 ? entry[1] / cnt[1] : 0f;
                    float eHigh = cnt[2] > 0 ? entry[2] / cnt[2] : 0f;

                    bool drainOk = dHigh < dLow && dHigh > 0f;
                    bool entryOk = eHigh > eLow;
                    string drainDir = drainOk ? "✅ Low>High" : "❌";
                    string entryDir = entryOk ? "✅ High>Low" : (cnt[2] == 0 ? "-" : "❌");

                    md.AppendFormat("| {0} | drain | {1:F1} | {2:F1} | {3:F1} | {4} |\n",
                        typeName, dLow, dMid, dHigh, drainDir);
                    md.AppendFormat("| {0} | entry | {1:F3} | {2:F3} | {3:F3} | {4} |\n",
                        "", eLow, eMid, eHigh, entryDir);
                }
            }
            md.AppendLine();
        }

        // ═══════════════════════════════════
        //  3. 밸런스 경고
        // ═══════════════════════════════════
        md.AppendLine("---");
        md.AppendLine();
        md.AppendLine("## ⚠️ 밸런스 경고");
        md.AppendLine();

        display.AppendLine("\n═══════════════════════════════════════════════════════════════════");
        display.AppendLine("  밸런스 경고");

        bool hasWarning = false;
        foreach (var tr in results)
        {
            var sorted = tr.stats.Values.Where(s => s.raceCount > 0).OrderByDescending(s => s.WinRate).ToList();
            if (sorted.Count == 0) continue;

            float maxWin = sorted.Max(s => s.WinRate);
            float minWin = sorted.Min(s => s.WinRate);
            float rankRange = sorted.Max(s => s.AvgRank) - sorted.Min(s => s.AvgRank);

            if (maxWin > 0.3f)
            {
                string w = string.Format("[{0}] {1} 승률 {2:F1}% → 너무 높음!", tr.trackName, KN(sorted[0].name), maxWin * 100);
                display.AppendLine("  ⚠️ " + w); md.AppendLine("- ⚠️ " + w); hasWarning = true;
            }
            if (minWin < 0.02f)
            {
                string w = string.Format("[{0}] {1} 승률 {2:F1}% → 너무 낮음!", tr.trackName, KN(sorted.Last().name), minWin * 100);
                display.AppendLine("  ⚠️ " + w); md.AppendLine("- ⚠️ " + w); hasWarning = true;
            }
            if (rankRange > 4.0f)
            {
                string w = string.Format("[{0}] 평균 순위 편차 {1:F1} → 밸런스 불균형", tr.trackName, rankRange);
                display.AppendLine("  ⚠️ " + w); md.AppendLine("- ⚠️ " + w); hasWarning = true;
            }
        }
        if (!hasWarning)
        {
            display.AppendLine("  ✅ 특이 경고 없음");
            md.AppendLine("- ✅ 특이 경고 없음");
        }
        md.AppendLine();

        // ═══════════════════════════════════
        //  4. 밸런스 조정 가이드 (GameSettings 현재값)
        // ═══════════════════════════════════
        md.AppendLine("---");
        md.AppendLine();
        md.AppendLine("## 📊 현재 GameSettings 주요 밸런스 값");
        md.AppendLine();
        var g = gameSettings;
        md.AppendLine("| 설정 | 값 | 설명 |");
        md.AppendLine("|------|----|------|");
        md.AppendFormat("| globalSpeedMultiplier | {0:F2} | 전역 속도 배율 |\n", g.globalSpeedMultiplier);
        md.AppendFormat("| noiseFactor | {0:F3} | 노이즈 계수 (높으면 변동↑) |\n", g.noiseFactor);
        md.AppendFormat("| luckCritChance | {0:F4} | luck 1당 크리 확률 |\n", g.luckCritChance);
        md.AppendFormat("| luckCritBoost | {0:F2} | 크리 속도 배율 |\n", g.luckCritBoost);
        md.AppendFormat("| luckCritDuration | {0:F1}s | 크리 지속 시간 |\n", g.luckCritDuration);
        md.AppendFormat("| luckCheckInterval | {0:F1}s | 크리 판정 주기 |\n", g.luckCheckInterval);
        md.AppendLine();

        if (equalStats)
        {
            md.AppendLine();
            md.AppendFormat("### 균등 스탯 모드: 모든 스탯 = {0}\n", equalStatValue);
        }
        md.AppendLine();

        display.AppendLine("═══════════════════════════════════════════════════════════════════");

        resultText = display.ToString();
        Debug.Log(resultText);

        // ═══════════════════════════════════
        //  로그 파일 저장
        // ═══════════════════════════════════
        if (saveLog)
        {
            SaveLogFile(md.ToString());
        }
    }

    // ══════════════════════════════════════
    //  로그 파일 저장
    // ══════════════════════════════════════

    private void SaveLogFile(string markdownContent)
    {
        string projectRoot = Application.dataPath.Replace("/Assets", "");
        string logDir = Path.Combine(projectRoot, "Docs", "logs");

        if (!Directory.Exists(logDir))
            Directory.CreateDirectory(logDir);

        string timestamp = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string filename = string.Format("backtest_{0}.md", timestamp);
        string fullPath = Path.Combine(logDir, filename);

        File.WriteAllText(fullPath, markdownContent, System.Text.Encoding.UTF8);
        lastLogPath = Path.Combine("Docs", "logs", filename);
        Debug.Log("[백테스팅] 로그 저장: " + fullPath);
    }

    // ══════════════════════════════════════
    //  유틸리티
    // ══════════════════════════════════════

    private Dictionary<string, CharacterData> charDataCache;
    private CharacterData FindCharData(string charId)
    {
        if (charDataCache == null)
        {
            charDataCache = new Dictionary<string, CharacterData>();
            TextAsset csv = Resources.Load<TextAsset>("Data/CharacterDB_V4");
            if (csv != null)
            {
                foreach (var line in csv.text.Split('\n'))
                {
                    var trimmed = line.Trim();
                    if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith("char_id")) continue;
                    var cd = CharacterData.ParseCSVLine(trimmed);
                    if (cd != null) charDataCache[cd.charId] = cd;
                }
            }
        }
        return charDataCache.ContainsKey(charId) ? charDataCache[charId] : null;
    }

    private static string SF(float v) // Signed Format: +1.23 / -0.45
    {
        return v >= 0 ? string.Format("+{0:F2}", v) : string.Format("{0:F2}", v);
    }

    private static float StdDev(List<float> values)
    {
        if (values.Count <= 1) return 0f;
        float mean = values.Average();
        float sumSqDiff = values.Sum(v => (v - mean) * (v - mean));
        return Mathf.Sqrt(sumSqDiff / values.Count);
    }

}

// ══════════════════════════════════════════
//  캐릭터 기록 초기화 메뉴
// ══════════════════════════════════════════

public static class CharacterRecordResetMenu
{
    [MenuItem("DopamineRace/캐릭터 기록 초기화")]
    public static void ResetCharacterRecords()
    {
        bool confirm = EditorUtility.DisplayDialog(
            "캐릭터 기록 초기화",
            "모든 캐릭터의 성적 기록(승률, 순위, 출전 횟수 등)을 초기화합니다.\n이 작업은 되돌릴 수 없습니다.\n\n계속하시겠습니까?",
            "초기화", "취소");

        if (!confirm) return;

        PlayerPrefs.DeleteKey("DopamineRace_CharRecords");
        PlayerPrefs.Save();
        Debug.Log("[DopamineRace] 캐릭터 성적 기록 전체 초기화 완료");

        // 런타임 ScoreManager가 있으면 동기화
        var sm = Object.FindObjectOfType<ScoreManager>();
        if (sm != null)
            sm.ResetCharacterRecords("all");

        EditorUtility.DisplayDialog("완료", "캐릭터 기록이 초기화되었습니다.", "확인");
    }
}
#endif
