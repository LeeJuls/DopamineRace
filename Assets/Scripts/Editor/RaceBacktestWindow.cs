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
    private Vector2 scrollPos;
    private string resultText = "";
    private string lastLogPath = "";
    private bool isRunning = false;
    private bool cancelRequested = false;
    private List<SimRacer> _activeRacers; // SimConsumeHP_FormationHold 에서 상행 그룹 탐색용
    private Dictionary<string, CharacterDataV4> v4DataMap;

    // ★ 구간별 포지션 추적 체크포인트
    private static readonly float[] segCheckpoints = { 0.10f, 0.20f, 0.35f, 0.50f, 0.65f, 0.80f, 0.90f };

    [MenuItem("DopamineRace/백테스팅")]
    public static void ShowWindow()
    {
        GetWindow<RaceBacktestWindow>("레이스 백테스팅");
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
    //  핵심 시뮬레이션 루프
    // ══════════════════════════════════════

    private TrackResult RunSimulationCore(List<CharacterData> allChars, int trackIndex, int totalTracks, string trackName)
    {
        int racerCount = Mathf.Min(simRacers, allChars.Count);
        var gs = gameSettings;

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

        // ★ 랩×구간 순위 추적 초기화 (phase: 0=25%, 1=50%, 2=100%)
        int lapPhaseCount = simLaps * 3;
        Dictionary<string, float[]> typeLapPhaseRankSum   = new Dictionary<string, float[]>();
        Dictionary<string, int[]>   typeLapPhaseRankCount = new Dictionary<string, int[]>();
        foreach (string typeName in new[] { "도주", "선행", "선입", "추입" })
        {
            typeLapPhaseRankSum[typeName]   = new float[lapPhaseCount];
            typeLapPhaseRankCount[typeName] = new int[lapPhaseCount];
        }

        for (int race = 0; race < simCount; race++)
        {
            // 랜덤 선발
            List<CharacterData> selected = new List<CharacterData>(allChars);
            while (selected.Count > racerCount)
                selected.RemoveAt(Random.Range(0, selected.Count));

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
                        racer.v4MaxStamina = gs4.v4_staminaBase + dv4.v4Stamina * gs4.v4_staminaPerStat;
                        racer.v4Stamina = racer.v4MaxStamina;
                        racer.v4CurrentSpeed = gs.globalSpeedMultiplier * 0.5f;
                        racer.v4LastProgress = 0f;
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
                    }
                }
                else if (gs.useHPSystem)
                {
                    racer.maxHP = gs.CalcMaxHP(cd.charBaseEndurance, simLaps);
                    racer.enduranceHP = racer.maxHP;
                    racer.totalConsumedHP = 0f;
                    racer.hpBoostValue = 0f;
                    racer.currentRank = 0;
                    racer.slipstreamBlend = 0f;

                    // CP 시스템 초기화
                    racer.maxCP = cd.charBaseCalm * gs.cpMultiplier;
                    racer.calmPoints = racer.maxCP;
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
                if (gs.useV4RaceSystem || gs.useHPSystem)
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
                            // ═══ V1-V3 슬립스트림 블렌드 ═══
                            float ssTarget = (closestGap < gs.universalSlipstreamRange)
                                ? 1f - (closestGap / gs.universalSlipstreamRange) : 0f;
                            float fadeTime = Mathf.Max(gs.slipstreamFadeTime, 0.01f);
                            float gameDtSS = simTimeStep * gs.globalSpeedMultiplier;
                            racers[ri].slipstreamBlend = Mathf.MoveTowards(
                                racers[ri].slipstreamBlend, ssTarget, gameDtSS / fadeTime);

                            // CP 소모
                            if (racers[ri].calmPoints > 0f)
                            {
                                float drain = gs.cpBasicDrain;
                                if (racers[ri].slipstreamBlend > 0f)
                                    drain += gs.cpSlipstreamDrain * racers[ri].slipstreamBlend;
                                racers[ri].calmPoints = Mathf.Max(0f,
                                    racers[ri].calmPoints - drain * gameDtSS);
                            }
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
                        float effectiveLerp = r.isSprintMode
                            ? gs.raceSpeedLerp * gs.sprintBurstLerpMult
                            : gs.raceSpeedLerp;
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

            // 미완주
            var unfinished = racers.Where(r => !r.finished).OrderByDescending(r => r.position).ToList();
            for (int i = 0; i < unfinished.Count; i++) { finishedCount++; unfinished[i].finishOrder = finishedCount; }

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

            if (race % 10 == 0)
            {
                float overallProgress = ((float)trackIndex * simCount + race) / (totalTracks * simCount);
                string msg = string.Format("트랙 {0}/{1} [{2}]  레이스 {3}/{4}",
                    trackIndex + 1, totalTracks, trackName, race, simCount);
                bool cancelled = EditorUtility.DisplayCancelableProgressBar("백테스팅", msg, overallProgress);
                if (cancelled) { cancelRequested = true; break; }
            }
        }

        if (!runAllTracks) EditorUtility.ClearProgressBar();

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
        float powerA = a.data.charBasePower;
        float powerB = b.data.charBasePower;
        float effA = powerA;
        float effB = powerB;
        if (powerA > powerB) effA = powerA * (1f + powerA / (powerA + powerB));
        else if (powerB > powerA) effB = powerB * (1f + powerB / (powerA + powerB));

        float totalEff = effA + effB;
        float bWinChance = totalEff > 0f ? effB / totalEff : 0.5f;

        SimRacer winner, loser;
        if (Random.value < bWinChance) { winner = b; loser = a; }
        else { winner = a; loser = b; }

        // luck 회피
        float trackLuckMul = track != null ? track.luckMultiplier : 1f;
        float dodgeChance = loser.data.charBaseLuck * gs.luckDodgeChance * trackLuckMul;
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

        // ── HP vs 레거시 분기 ──
        float typeBonus = 0f;
        float fatigue = 0f;

        if (gs.useHPSystem)
        {
            // ═══ 속도 압축 ═══
            if (gs.hpSpeedCompress > 0f)
            {
                float midSpeed = 0.905f * globalMul * trackSpeedMul;
                baseSpeed = Mathf.Lerp(baseSpeed, midSpeed, gs.hpSpeedCompress);
            }

            // ═══ Type 1: 기존 HP 부스트 경로 ═══
            SimConsumeHP(r, gs, progress);
            float hpBoost = SimCalcHPBoost(r, gs);
            float earlyBonus = gs.GetHPEarlyBonus(cd.charType, progress);
            float cpRatioCalc = r.maxCP > 0f ? r.calmPoints / r.maxCP : 0f;
            float cpEff = gs.GetCPEfficiency(cpRatioCalc);
            float ssBonus = gs.GetSlipstreamBonus(cd.charType, r.slipstreamBlend, cpEff);
            typeBonus = hpBoost + earlyBonus + ssBonus;
            r.contrib_type += baseSpeed * typeBonus * simTimeStep;
        }

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

        float speed = baseSpeed * (1f + typeBonus + powerBonus + braveBonus);
        speed += r.noiseValue;
        if (!gs.useHPSystem) speed -= fatigue; // HP 시스템: fatigue 내장

        // ═══ 초반 대형: 타입별 포지션 정렬 ═══
        float formationMod = gs.GetFormationModifier(
            cd.charType, progress, r.currentRank, simRacers);
        speed *= (1f + formationMod);

        speed *= slowMul * critMul;
        return Mathf.Max(speed, 0.1f);
    }

    // ══════════════════════════════════════
    //  HP 시스템 미러 (SPEC-006)
    // ══════════════════════════════════════

    /// <summary>HP 소모 (RacerController.ConsumeHP 미러) — 4페이즈 전략</summary>
    private void SimConsumeHP(SimRacer r, GameSettings gs, float progress)
    {
        if (r.enduranceHP <= 0f) return;

        // 1랩 이하: Legacy 존 기반 로직
        if (simLaps < 2)
        {
            SimConsumeHP_Legacy(r, gs, progress);
            return;
        }

        // progress(OverallProgress 0-1) × simLaps = TotalProgress 환산
        float totalProgressSim = progress * simLaps;
        if (totalProgressSim < gs.positioningLapEnd)
            SimConsumeHP_Positioning(r, gs);
        else if (totalProgressSim < gs.formationHoldLapEnd)
            SimConsumeHP_FormationHold(r, gs);
        else
            SimConsumeHP_Strategy(r, gs, progress);
    }

    // ─── 기존 존 기반 로직 (1랩 이하 폴백) ──────────────────────────
    private void SimConsumeHP_Legacy(SimRacer r, GameSettings gs, float progress)
    {
        gs.GetHPParams(r.data.charType,
            out float spurtStart, out float activeRate, out _,
            out _, out _, out _);
        gs.GetZoneParams(r.data.charType,
            out float targetZonePct, out float inZoneRate, out float outZoneRate);

        bool inSpurt = spurtStart > 0f && progress >= (1f - spurtStart);
        if (inSpurt && r.data.charType == CharacterType.Leader
            && r.maxHP > 0f && r.enduranceHP / r.maxHP < gs.leaderSpurtMinHP)
            inSpurt = false;

        float normalRate;
        if (targetZonePct <= 0f)
        {
            normalRate = inZoneRate;
        }
        else
        {
            int targetMaxRank = Mathf.Max(1, Mathf.CeilToInt(simRacers * targetZonePct));
            normalRate = (r.currentRank <= targetMaxRank) ? inZoneRate : outZoneRate;
        }

        float rate;
        if (inSpurt)
        {
            float spurtThreshold = 1f - spurtStart;
            float spurtProgress = Mathf.Clamp01((progress - spurtThreshold) / spurtStart);
            rate = Mathf.Lerp(normalRate, activeRate, spurtProgress);
        }
        else
        {
            rate = normalRate;
        }

        SimApplyHPConsumption(r, gs, rate);
    }

    // ─── 페이즈 1: 포지셔닝 ─────────────────────────────────────────
    // ★ 타입 기반 포지셔닝 (RacerController 미러링)
    private void SimConsumeHP_Positioning(SimRacer r, GameSettings gs)
    {
        gs.GetHPParams(r.data.charType,
            out _, out float activeRate, out _, out _, out _, out _);
        gs.GetZoneParams(r.data.charType,
            out _, out float inZoneRate, out _);

        float rate;
        switch (r.data.charType)
        {
            case CharacterType.Runner:
                rate = activeRate;
                break;
            case CharacterType.Leader:
                rate = Mathf.Lerp(inZoneRate, activeRate, 0.6f);
                break;
            case CharacterType.Chaser:
                rate = inZoneRate;
                break;
            default: // Reckoner
                rate = Mathf.Max(gs.basicConsumptionRate, inZoneRate * 0.4f);
                break;
        }

        SimApplyHPConsumption(r, gs, rate);
    }

    // ─── 페이즈 2: 대형 유지 ────────────────────────────────────────
    private void SimConsumeHP_FormationHold(SimRacer r, GameSettings gs)
    {
        gs.GetHPParams(r.data.charType,
            out _, out float activeRate, out _, out _, out _, out _);
        gs.GetZoneParams(r.data.charType,
            out _, out float inZoneRate, out float outZoneRate);

        int topHalf = Mathf.Max(1, simRacers / 2);
        float rate;

        bool isUpperType = r.data.charType == CharacterType.Runner
                        || r.data.charType == CharacterType.Leader;
        if (isUpperType)
        {
            float posTarget = gs.GetPositioningTarget(r.data.charType);
            int targetMaxRank = Mathf.Max(1, Mathf.CeilToInt(simRacers * posTarget));
            rate = (r.currentRank <= targetMaxRank) ? inZoneRate : activeRate;
        }
        else
        {
            if (r.currentRank <= topHalf)
            {
                // 상행 영역 침범 금지 → 강제 보존 (reckoner_baseRate)
                gs.GetZoneParams(CharacterType.Reckoner, out _, out float baseConserveRate, out _);
                rate = baseConserveRate;
            }
            else
            {
                const float trackLen = 17f;
                float myTotalProg = r.position / trackLen;
                float gap = SimGetLastUpperTrackProgress() - myTotalProg;

                if (gap > gs.formationGapMax)
                    rate = activeRate; // 상행 따라잡기
                else if (gap < gs.formationGapMin)
                    rate = inZoneRate; // 추월 방지 → 보존
                else if (r.data.charType == CharacterType.Chaser)
                {
                    float chaserTarget = gs.GetPositioningTarget(CharacterType.Chaser);
                    int targetMaxRank = Mathf.Max(1, Mathf.CeilToInt(simRacers * chaserTarget));
                    rate = (r.currentRank <= targetMaxRank) ? inZoneRate : outZoneRate;
                }
                else
                    rate = inZoneRate; // Reckoner: 항상 보존
            }
        }

        SimApplyHPConsumption(r, gs, rate);
    }

    // ─── 페이즈 3: 전략 (SPEC-RC-002 미러링) ────────────────────────
    private void SimConsumeHP_Strategy(SimRacer r, GameSettings gs, float progress)
    {
        gs.GetHPParams(r.data.charType,
            out _, out float activeRate, out _,
            out _, out _, out _);
        gs.GetZoneParams(r.data.charType,
            out _, out float inZoneRate, out float outZoneRate);

        float rate;
        bool sprintThisFrame = false;

        switch (r.data.charType)
        {
            case CharacterType.Runner:
            {
                // [SPEC-RC-002] 도주: 항상 전력질주
                rate = activeRate;
                sprintThisFrame = true;
                break;
            }

            case CharacterType.Leader:
            {
                // [SPEC-RC-002] 선행: 마지막 바퀴 20% 남으면 전력질주
                bool inSprint = SimIsLastLapSprintZone(progress, gs.leaderSprintLastLapThreshold);
                int top30Rank = Mathf.Max(1, Mathf.CeilToInt(simRacers * 0.30f));

                if (inSprint)
                {
                    rate = activeRate;
                    sprintThisFrame = true;
                }
                else if (r.currentRank > top30Rank)
                    rate = outZoneRate;
                else
                    rate = inZoneRate;
                break;
            }

            case CharacterType.Chaser:
            {
                // [SPEC-RC-002] 선입: 마지막 바퀴 30% 남으면 전력질주
                bool inSprint = SimIsLastLapSprintZone(progress, gs.chaserSprintLastLapThreshold);
                int top70Rank = Mathf.Max(1, Mathf.CeilToInt(simRacers * 0.70f));

                if (inSprint)
                {
                    rate = activeRate;
                    sprintThisFrame = true;
                }
                else if (r.currentRank > top70Rank)
                    rate = outZoneRate;
                else
                    rate = inZoneRate;
                break;
            }

            default: // Reckoner
            {
                // [SPEC-RC-002] 추입: 마지막 바퀴 40% 남으면 전력질주
                bool inSprint = SimIsLastLapSprintZone(progress, gs.reckonerSprintLastLapThreshold);

                if (inSprint)
                {
                    rate = activeRate;
                    sprintThisFrame = true;
                }
                else
                    rate = inZoneRate;
                break;
            }
        }

        r.isSprintMode = sprintThisFrame;
        SimApplyHPConsumption(r, gs, rate);
    }

    // ─── 헬퍼: 마지막 바퀴 기준 스프린트 판정 (SPEC-RC-002 미러링) ──
    private bool SimIsLastLapSprintZone(float overallProgress, float threshold)
    {
        if (simLaps < 2) return false;
        float lastLapStart = (float)(simLaps - 1) / simLaps;
        if (overallProgress < lastLapStart) return false;
        float lastLapLen = 1f / simLaps;
        float lastLapProg = (overallProgress - lastLapStart) / lastLapLen;
        float lastLapRemaining = 1f - lastLapProg;
        return lastLapRemaining <= threshold;
    }

    // ─── 공통 tail: boostAmp / speedRatio / leadPaceTax (SPEC-RC-002 미러링) ──
    private void SimApplyHPConsumption(SimRacer r, GameSettings gs, float rate)
    {
        float trackSpeedMul = selectedTrack != null ? selectedTrack.speedMultiplier : 1f;
        float baseTrackSpeed = r.data.SpeedMultiplier * gs.globalSpeedMultiplier * trackSpeedMul;
        float speedRatio = baseTrackSpeed > 0.01f ? r.currentSpeed / baseTrackSpeed : 1f;
        speedRatio = Mathf.Clamp(speedRatio, 0.1f, 2f);

        float effectiveRate = Mathf.Max(gs.basicConsumptionRate, rate);

        // 부스트 피드백: 부스트 높을수록 HP 소모 증가
        float boostAmp = 1f + gs.boostHPDrainCoeff * Mathf.Max(0f, r.hpBoostValue);
        effectiveRate *= boostAmp;

        // ★ 컨디션 역보정: 저컨디션 → HP 소모 증가 (RacerController 미러링)
        float condMul = OddsCalculator.GetConditionMultiplier(r.data.charId);
        condMul = Mathf.Max(condMul, 0.3f);
        effectiveRate /= condMul;

        // [SPEC-RC-002 v2] 스프린트 HP 급소모 시스템 (RacerController 미러링)
        // ① speedRatio 바이패스
        float speedMul = r.isSprintMode
            ? Mathf.Max(1f, Mathf.Sqrt(speedRatio))
            : Mathf.Sqrt(speedRatio);

        // ② 스프린트 급소모 배율 (Runner 제외)
        // lapFactor = hpLapReference / totalLaps → 단거리일수록 높음
        if (r.isSprintMode && r.data.charType != CharacterType.Runner)
        {
            float lapFactor = (float)gs.hpLapReference / Mathf.Max(1, simLaps);
            effectiveRate *= gs.sprintHPDrainMultiplier * lapFactor;
        }

        float gameDtHP = simTimeStep * gs.globalSpeedMultiplier;
        float consumption = effectiveRate * speedMul * gameDtHP;
        consumption = Mathf.Min(consumption, r.enduranceHP);

        r.enduranceHP -= consumption;
        r.totalConsumedHP += consumption;

        // 선두 페이스 택스: 바람막이 추가 소모
        if (r.currentRank <= gs.leadPaceTaxRank && r.enduranceHP > 0f)
        {
            float paceTax = gs.leadPaceTaxRate * Mathf.Sqrt(speedRatio) * gameDtHP;
            r.enduranceHP = Mathf.Max(0f, r.enduranceHP - paceTax);
        }
    }

    // ─── 헬퍼: 상행 그룹(Runner/Leader) 중 최소 TotalProgress ────────
    private float SimGetLastUpperTrackProgress()
    {
        const float trackLen = 17f;
        float minProg = float.MaxValue;
        if (_activeRacers == null) return -0.1f;

        foreach (var r in _activeRacers)
        {
            if (r.finished) continue;
            var type = r.data?.charType ?? CharacterType.Runner;
            if (type == CharacterType.Runner || type == CharacterType.Leader)
            {
                float prog = r.position / trackLen;
                if (prog < minProg) minProg = prog;
            }
        }
        return minProg == float.MaxValue ? -0.1f : minProg;
    }

    /// <summary>HP 부스트 계산 (RacerController.CalcHPBoost 미러)</summary>
    private float SimCalcHPBoost(SimRacer r, GameSettings gs)
    {
        gs.GetHPParams(r.data.charType,
            out _, out _, out float peakBoost,
            out float accelExp, out float decelExp, out float exhaustionFloor);

        // 장거리 보정: Chaser/Reckoner peakBoost 증폭
        if (simLaps > gs.hpLapReference)
        {
            if (r.data.charType == CharacterType.Chaser || r.data.charType == CharacterType.Reckoner)
            {
                float lapExcess = (float)(simLaps - gs.hpLapReference) / gs.hpLapReference;
                peakBoost *= (1f + lapExcess * gs.longRaceLateBoostAmp);
            }
        }

        float consumedRatio = r.maxHP > 0f ? r.totalConsumedHP / r.maxHP : 0f;
        float threshold = gs.boostThreshold;

        // ═══ Power 기반 가속 강화: power 높을수록 부스트 곡선이 가파름 ═══
        float powerFactor = 1f + (r.data.charBasePower / 20f) * gs.powerAccelCoeff;
        float effectiveAccelExp = accelExp / Mathf.Max(powerFactor, 0.1f);

        float boost;
        if (consumedRatio <= threshold)
        {
            float t = threshold > 0f ? consumedRatio / threshold : 0f;
            boost = peakBoost * Mathf.Pow(t, effectiveAccelExp);
        }
        else if (r.enduranceHP > 0f)
        {
            float remain = 1f - threshold;
            float t = remain > 0f ? (consumedRatio - threshold) / remain : 1f;
            t = Mathf.Clamp01(t);
            boost = peakBoost * Mathf.Pow(1f - t, decelExp);
        }
        else
        {
            boost = exhaustionFloor;
        }

        r.hpBoostValue = boost;
        return boost;
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

        // ── 3. 속도 계산 (CalcSpeedV4 미러) ──
        float baseSpeed = gs.globalSpeedMultiplier;
        float vmax = baseSpeed * (1f + dv4.v4Speed * gs4.v4_speedStatFactor);

        // HP 임계값 기반 속도 배율
        float staminaRatio = r.v4MaxStamina > 0 ? r.v4Stamina / r.v4MaxStamina : 0f;
        float hpSpeedMul = gs4.GetHpSpeedMultiplier(staminaRatio);
        vmax *= hpSpeedMul;

        float accelRate = dv4.v4Accel * gs4.v4_accelStatFactor;

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
                else if (inRegularBurst) drain *= gs4.v4_burstDrainMul;
                else if (inEmergencyBurst) drain *= gs4.GetV4EmergencyBurstDrainMul(dv4.charType);
            }

            // 슬립스트림 드레인 감소
            if (r.v4InSlipstream)
                drain *= r.v4SlipstreamDrainMul;

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
            float chance = r.dataV4.v4Luck * gs4.v4_luckCritChance;
            if (Random.value < chance)
            {
                // 지능 modifier
                float intModifier = (r.dataV4.v4Intelligence - 10f) / 10f * gs4.v4_intelligenceModMax;
                r.v4CritBoostRemaining = gs4.v4_luckCritDuration * (1f + intModifier);
                r.critCount++;
            }
        }
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

        // 지능 modifier (지능20 → +10%, 지능10 → ±0%, 지능0 → -10%)
        float intModifier = (dv4.v4Intelligence - 10f) / 10f * gs4.v4_intelligenceModMax;
        float effectiveEnd = start + (end - start) * (1f + intModifier);

        return progress >= start && progress < effectiveEnd;
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
            bool hpOn = gameSettings.useHPSystem;
            string typeColName = hpOn ? "HP부스트" : "타입(TYPE)";
            string endColName = hpOn ? "(내장)" : "피로(END)";
            display.AppendFormat("\n──── [{0}] 스탯별 기여 (레이스당 평균 거리) {1} ────\n", tn, hpOn ? "[HP시스템]" : "[레거시]");
            display.AppendLine("  이름   속도    " + (hpOn ? "HP부스트" : "타입  ") + "  피로     노이즈   럭      파워    용감    합계");

            md.AppendFormat("### 스탯별 기여 (레이스당 평균 거리) {0}\n", hpOn ? "— HP시스템" : "— 레거시");
            md.AppendLine();
            md.AppendFormat("| 이름 | 속도(SPD) | {0} | {1} | 노이즈(CALM) | 럭(LUCK) | 파워(POW) | 용감(BRV) | 합계 |\n", typeColName, endColName);
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
        md.AppendFormat("| fatigueFactor | {0:F3} | 피로 계수 (높으면 후반 감속↑) |\n", g.fatigueFactor);
        md.AppendFormat("| noiseFactor | {0:F3} | 노이즈 계수 (높으면 변동↑) |\n", g.noiseFactor);
        md.AppendFormat("| luckCritChance | {0:F4} | luck 1당 크리 확률 |\n", g.luckCritChance);
        md.AppendFormat("| luckCritBoost | {0:F2} | 크리 속도 배율 |\n", g.luckCritBoost);
        md.AppendFormat("| luckCritDuration | {0:F1}s | 크리 지속 시간 |\n", g.luckCritDuration);
        md.AppendFormat("| luckCheckInterval | {0:F1}s | 크리 판정 주기 |\n", g.luckCheckInterval);
        md.AppendLine();
        if (g.useHPSystem)
        {
            md.AppendLine("### HP 시스템 (SPEC-006) ✅ ON");
            md.AppendLine();
            md.AppendLine("| 설정 | 값 |");
            md.AppendLine("|------|----|");
            md.AppendFormat("| hpBase | {0} |\n", g.hpBase);
            md.AppendFormat("| hpPerEndurance | {0} |\n", g.hpPerEndurance);
            md.AppendFormat("| basicConsumptionRate | {0} |\n", g.basicConsumptionRate);
            md.AppendFormat("| boostThreshold | {0} |\n", g.boostThreshold);
            md.AppendFormat("| hpLapReference | {0} |\n", g.hpLapReference);
            md.AppendFormat("| longRaceLateBoostAmp | {0} |\n", g.longRaceLateBoostAmp);
            md.AppendFormat("| hp_earlyBonus_Runner | {0} |\n", g.hp_earlyBonus_Runner);
            md.AppendFormat("| hp_earlyBonusFadeEnd | {0} |\n", g.hp_earlyBonusFadeEnd);
            md.AppendLine();
            md.AppendLine("| 타입 | spurtStart | activeRate | peakBoost | accelExp | decelExp | exhaustionFloor |");
            md.AppendLine("|------|------------|-----------|-----------|----------|----------|-----------------|");
            md.AppendFormat("| Runner | {0} | {1} | {2} | {3} | {4} | {5} |\n",
                g.runner_spurtStart, g.runner_activeRate, g.runner_peakBoost,
                g.runner_accelExp, g.runner_decelExp, g.runner_exhaustionFloor);
            md.AppendFormat("| Leader | {0} | {1} | {2} | {3} | {4} | {5} |\n",
                g.leader_spurtStart, g.leader_activeRate, g.leader_peakBoost,
                g.leader_accelExp, g.leader_decelExp, g.leader_exhaustionFloor);
            md.AppendFormat("| Chaser | {0} | {1} | {2} | {3} | {4} | {5} |\n",
                g.chaser_spurtStart, g.chaser_activeRate, g.chaser_peakBoost,
                g.chaser_accelExp, g.chaser_decelExp, g.chaser_exhaustionFloor);
            md.AppendFormat("| Reckoner | {0} | {1} | {2} | {3} | {4} | {5} |\n",
                g.reckoner_spurtStart, g.reckoner_activeRate, g.reckoner_peakBoost,
                g.reckoner_accelExp, g.reckoner_decelExp, g.reckoner_exhaustionFloor);

            md.AppendLine();
            md.AppendLine("### 포지션 타겟팅 파라미터");
            md.AppendLine();
            md.AppendLine("| 타입 | targetZone | inZoneRate | outZoneRate |");
            md.AppendLine("|------|------------|-----------|-------------|");
            md.AppendFormat("| Runner | {0:P0} | {1} | {2} |\n", g.runner_targetZone, g.runner_inZoneRate, g.runner_outZoneRate);
            md.AppendFormat("| Leader | {0:P0} | {1} | {2} |\n", g.leader_targetZone, g.leader_inZoneRate, g.leader_outZoneRate);
            md.AppendFormat("| Chaser | {0:P0} | {1} | {2} |\n", g.chaser_targetZone, g.chaser_inZoneRate, g.chaser_outZoneRate);
            md.AppendFormat("| Reckoner | 없음 | {0} (base) | - |\n", g.reckoner_baseRate);

            if (equalStats)
            {
                md.AppendLine();
                md.AppendFormat("### ⚖️ 균등 스탯 모드: 모든 스탯 = {0}\n", equalStatValue);
            }
        }
        // (레거시 타입 보너스 제거됨 — HP 시스템으로 통합)
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
