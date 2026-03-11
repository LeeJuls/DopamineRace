#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.IO;

/// <summary>
/// 레이스 백테스팅 에디터 윈도우 (v3.1 — HP 시스템 미러)
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
    private int sweepSimsPerLap = 50;

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
        EditorGUILayout.LabelField("🏇 레이스 백테스팅 v3", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        gameSettings = (GameSettings)EditorGUILayout.ObjectField("GameSettings", gameSettings, typeof(GameSettings), false);

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
        if (GUILayout.Button(isRunning ? "시뮬레이션 중..." : "▶ 시뮬레이션 실행", GUILayout.Height(30)))
        {
            if (runAllTracks)
                RunAllTracksSimulation();
            else
                RunSingleTrackSimulation();
        }
        // ═══ V2 파라미터 스윕 ═══
        if (gameSettings != null && gameSettings.useV2RaceSystem)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("━━━ V2 파라미터 스윕 ━━━");
            sweepSimsPerLap = EditorGUILayout.IntSlider("스윕 시뮬/바퀴", sweepSimsPerLap, 20, 200);
            if (GUILayout.Button(isRunning ? "스윕 중..." : "▶ V2 파라미터 스윕 (최적값 탐색)", GUILayout.Height(30)))
            {
                RunParameterSweep();
            }
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

        // ★ Race V2 시스템
        public float v2SprintAccelProgress; // 전력질주 가속 진행률 0~1
        public bool v2IsSprintActive;       // V2 전력질주 활성 여부

        // ★ Race V3 시스템
        public float v3SprintAccelProgress; // V3 전력질주 가속 진행률 0~1
        public bool v3IsSprintActive;       // V3 전력질주 활성 여부

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
        TextAsset csv = Resources.Load<TextAsset>("Data/CharacterDB");
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

                // HP / V3 스태미나 초기화
                if (gs.useV3RaceSystem)
                {
                    // V3: 스태미나 = staminaBase + endurance × staminaPerEndurance
                    racer.maxHP = gs.v3Settings.v3_staminaBase + cd.charBaseEndurance * gs.v3Settings.v3_staminaPerEndurance;
                    racer.enduranceHP = racer.maxHP;
                    racer.totalConsumedHP = 0f;
                    racer.hpBoostValue = 0f;
                    racer.currentRank = 0;
                    racer.slipstreamBlend = 0f;
                    racer.maxCP = cd.charBaseCalm * gs.cpMultiplier;
                    racer.calmPoints = racer.maxCP;
                    racer.v3SprintAccelProgress = 0f;
                    racer.v3IsSprintActive = false;
                }
                else if (gs.useHPSystem)
                {
                    racer.maxHP = gs.useV2RaceSystem
                        ? gs.CalcMaxHP(cd.charBaseEndurance)        // V2: 랩 스케일링 없음
                        : gs.CalcMaxHP(cd.charBaseEndurance, simLaps);
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

                // ═══ Phase 4: 순위 + 슬립스트림 + CP 소모 갱신 ═══
                if (gs.useHPSystem)
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

                        // 슬립스트림 블렌드 (전체 타입, 거리 기반)
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

                foreach (var r in racers)
                {
                    if (r.finished) continue;

                    float progress = Mathf.Clamp01(r.position / finishDistance);
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
                        r.contrib_power -= distLost;  // 충돌 패배 시 잃은 거리
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
                        r.contrib_brave += distGained;  // 슬링샷 이득
                        if (r.slingshotTimer <= 0f) r.slingshotBoost = 0f;
                    }

                    float targetSpeed = baseTarget * penaltyMul * slingshotMul;
                    // [SPEC-RC-002] 스프린트 진입 시 Burst Lerp
                    float effectiveLerp = r.isSprintMode
                        ? gs.raceSpeedLerp * gs.sprintBurstLerpMult
                        : gs.raceSpeedLerp;
                    r.currentSpeed = Mathf.Lerp(r.currentSpeed, targetSpeed, simTimeStep * effectiveLerp);
                    r.position += r.currentSpeed * simTimeStep;

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

        if (gs.useV3RaceSystem)
        {
            // ═══ Race V3: 스탯 기반 재설계 ═══
            // V3 base: 모든 캐릭터 동일 (speed 스탯은 maxSpeedMul에 반영)
            baseSpeed = globalMul * trackSpeedMul;

            SimUpdateV3Sprint(r, gs, progress);

            float v3MaxSpeedMul = gs.v3Settings.GetV3MaxSpeedMul(cd.charBaseSpeed);
            float v3HpRatio     = r.maxHP > 0f ? Mathf.Clamp01(r.enduranceHP / r.maxHP) : 0f;
            float v3HpSpeedMul  = gs.v3Settings.GetV3SpeedFromHP(v3HpRatio);
            float v3SpeedRatio  = Mathf.Lerp(1.0f, v3MaxSpeedMul, r.v3SprintAccelProgress) * v3HpSpeedMul;

            // 포메이션 속도 캡 (드레인도 자동 감소)
            v3SpeedRatio *= SimCalcV3FormationSpeedCap(r, gs, progress);

            float v3CpRatio = r.maxCP > 0f ? r.calmPoints / r.maxCP : 0f;
            float v3CpEff   = gs.GetCPEfficiency(v3CpRatio);
            float v3SsBonus = gs.GetSlipstreamBonus(cd.charType, r.slipstreamBlend, v3CpEff);
            v3SpeedRatio *= (1f + v3SsBonus);

            float v3ZoneDrainMul = SimCalcV3ZoneDrainMul(r, gs, progress);
            SimConsumeHP_V3(r, gs, v3SpeedRatio, v3ZoneDrainMul);

            typeBonus = v3SpeedRatio - 1f;
            r.contrib_type += baseSpeed * typeBonus * simTimeStep;
        }
        else if (gs.useHPSystem)
        {
            // ═══ 속도 압축 (V1/V2 공유) ═══
            if (gs.hpSpeedCompress > 0f)
            {
                float midSpeed = 0.905f * globalMul * trackSpeedMul;
                baseSpeed = Mathf.Lerp(baseSpeed, midSpeed, gs.hpSpeedCompress);
            }

            if (gs.useV2RaceSystem)
            {
                // ═══ Race V2: 우마무스메 방식 — 구간 속도 계수 + 속도 비례 HP 소모 ═══

                // Step 1: 스프린트 상태 업데이트
                SimUpdateV2Sprint(r, gs, progress);

                // Step 2: 구간 계수 + HP 비례 속도
                int v2Phase = gs.GetV2Phase(progress);
                float phaseCoeff = gs.GetV2PhaseCoeff(cd.charType, v2Phase);
                float hpRatio = r.maxHP > 0f ? Mathf.Clamp01(r.enduranceHP / r.maxHP) : 0f;
                float targetSpeed = gs.GetV2SpeedFromHP(hpRatio);
                float v2SpeedMul = Mathf.Lerp(1f, targetSpeed, r.v2SprintAccelProgress);

                // Step 3: 보너스
                float cpRatioCalc = r.maxCP > 0f ? r.calmPoints / r.maxCP : 0f;
                float cpEff = gs.GetCPEfficiency(cpRatioCalc);
                float ssBonus = gs.GetSlipstreamBonus(cd.charType, r.slipstreamBlend, cpEff);

                // Step 4: speedRatio → HP 소모
                float speedRatio = phaseCoeff * v2SpeedMul * (1f + ssBonus);
                SimConsumeHP_V2(r, gs, speedRatio);

                // Step 5: 기여도
                typeBonus = (speedRatio - 1f);
                r.contrib_type += baseSpeed * typeBonus * simTimeStep;
            }
            else
            {
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
        }
        else
        {
            // ═══ 레거시 시스템 ═══
            float trackFatMul = track != null ? track.fatigueMultiplier : 1f;
            float endurance = Mathf.Max(cd.charBaseEndurance, 1f);
            fatigue = progress * (1f / endurance) * gs.fatigueFactor * trackFatMul;
            r.contrib_endurance -= fatigue * simTimeStep;

            int phase = progress < 0.35f ? 0 : progress < 0.70f ? 1 : 2;
            typeBonus = gs.GetTypeBonus(cd.charType, phase);
            if (track != null)
            {
                float phaseMul = phase == 0 ? track.earlyBonusMultiplier :
                                 phase == 1 ? track.midBonusMultiplier : track.lateBonusMultiplier;
                typeBonus *= phaseMul;
            }
            r.contrib_type += baseSpeed * typeBonus * simTimeStep;
        }

        float powerBonus = 0f, braveBonus = 0f;
        if (track != null && !gs.useV3RaceSystem)
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
        if (!gs.useHPSystem && !gs.useV3RaceSystem) speed -= fatigue; // HP/V3 시스템: fatigue 내장

        // ═══ 초반 대형: 타입별 포지션 정렬 ═══
        float formationMod = gs.GetFormationModifier(
            cd.charType, progress, r.currentRank, simRacers);
        speed *= (1f + formationMod);

        speed *= slowMul * critMul;
        return Mathf.Max(speed, 0.1f);
    }

    // ══════════════════════════════════════
    //  Race V2 HP 소모 미러
    // ══════════════════════════════════════

    /// <summary>V2 스프린트 판정 (RacerController.UpdateV2Sprint 미러)</summary>
    private void SimUpdateV2Sprint(SimRacer r, GameSettings gs, float progress)
    {
        float strategyStart = gs.formationHoldLapEnd / simLaps;

        // 전력질주 시작 판정 (한번 시작하면 멈추지 않음)
        if (progress >= strategyStart)
        {
            float strategyProg = Mathf.InverseLerp(strategyStart, 1f, progress);
            float sprintStart = gs.GetV2SprintStart(r.data.charType);
            if (!r.v2IsSprintActive && strategyProg >= sprintStart)
                r.v2IsSprintActive = true;
        }

        // 그라데이션 가속 (비가역)
        float gameDt = simTimeStep * gs.globalSpeedMultiplier;
        if (r.v2IsSprintActive)
            r.v2SprintAccelProgress = Mathf.MoveTowards(r.v2SprintAccelProgress, 1f, gameDt / gs.v2_sprintAccelTime);
    }

    /// <summary>V2 속도 비례 HP 소모 (RacerController.ConsumeHP_V2 미러)</summary>
    private void SimConsumeHP_V2(SimRacer r, GameSettings gs, float speedRatio)
    {
        float gameDt = simTimeStep * gs.globalSpeedMultiplier;

        if (r.enduranceHP > 0f)
        {
            float drain = gs.CalcV2SpeedDrain(speedRatio);
            float consumption = drain * gameDt;
            consumption = Mathf.Min(consumption, r.enduranceHP);
            r.enduranceHP -= consumption;
            r.totalConsumedHP += consumption;

            // 선두 HP 택스
            if (r.currentRank <= gs.leadPaceTaxRank && r.enduranceHP > 0f)
            {
                float tax = gs.leadPaceTaxRate * gameDt;
                r.enduranceHP = Mathf.Max(0f, r.enduranceHP - tax);
            }
        }

        // Burst Lerp 호환
        r.isSprintMode = r.v2IsSprintActive && r.v2SprintAccelProgress > 0.1f;
    }

    // ══════════════════════════════════════
    //  Race V3 미러 (RacerController_V3 동일 로직)
    // ══════════════════════════════════════

    /// <summary>V3 스프린트 판정 (RacerController_V3.UpdateV3Sprint 미러)</summary>
    private void SimUpdateV3Sprint(SimRacer r, GameSettings gs, float progress)
    {
        float strategyStart = gs.formationHoldLapEnd / simLaps;
        if (progress < strategyStart) return;

        float strategyProg = Mathf.InverseLerp(strategyStart, 1f, progress);
        if (!r.v3IsSprintActive && strategyProg >= gs.v3Settings.GetV3SprintStart(r.data.charType))
            r.v3IsSprintActive = true;

        if (!r.v3IsSprintActive) return;

        float accelRate = 1.0f + r.data.charBaseBrave * gs.v3Settings.v3_braveAccelPerPoint;
        float gameDt    = simTimeStep * gs.globalSpeedMultiplier;
        float step      = gameDt * accelRate / gs.v3Settings.v3_sprintAccelTimeBase;
        r.v3SprintAccelProgress = Mathf.MoveTowards(r.v3SprintAccelProgress, 1f, step);
        r.isSprintMode = r.v3SprintAccelProgress > 0.1f;
    }

    /// <summary>V3 HP 소모 (RacerController_V3.ConsumeHP_V3 미러)</summary>
    private void SimConsumeHP_V3(SimRacer r, GameSettings gs, float speedRatio, float zoneDrainMul)
    {
        if (r.enduranceHP <= 0f) return;

        float gameDt      = simTimeStep * gs.globalSpeedMultiplier;
        float drain       = gs.v3Settings.v3_drainBaseRate
                            * Mathf.Pow(speedRatio, gs.v3Settings.v3_drainExponent)
                            * zoneDrainMul;
        float consumption = Mathf.Min(drain * gameDt, r.enduranceHP);
        r.enduranceHP    -= consumption;
        r.totalConsumedHP += consumption;

        // 선두 HP 택스
        if (r.currentRank <= gs.leadPaceTaxRank && r.enduranceHP > 0f)
        {
            float tax = Mathf.Min(gs.leadPaceTaxRate * gameDt, r.enduranceHP);
            r.enduranceHP -= tax;
        }
    }

    /// <summary>V3 포메이션 속도 캡 (RacerController_V3.CalcV3FormationSpeedCap 미러)</summary>
    private float SimCalcV3FormationSpeedCap(SimRacer r, GameSettings gs, float progress)
    {
        float posEnd = gs.positioningLapEnd / simLaps;
        if (progress >= posEnd) return 1.0f;
        if (simRacers <= 1) return 1.0f;

        float zoneTarget  = gs.v3Settings.GetV3ZoneTarget(r.data.charType);
        float myRankRatio = simRacers > 1
            ? (float)(r.currentRank - 1) / (simRacers - 1) : 0f;
        float diff = myRankRatio - zoneTarget;

        if (diff < -gs.v3Settings.v3_zoneRange)
            return gs.v3Settings.v3_formationSpeedCap;
        return 1.0f;
    }

    /// <summary>V3 포지션 피드백 드레인 배율 (RacerController_V3.CalcV3ZoneDrainMul 미러)</summary>
    private float SimCalcV3ZoneDrainMul(SimRacer r, GameSettings gs, float progress)
    {
        float strategyStart = gs.formationHoldLapEnd / simLaps;
        if (progress >= strategyStart) return 1.0f;
        if (simRacers <= 1) return 1.0f;

        float zoneTarget  = gs.v3Settings.GetV3ZoneTarget(r.data.charType);
        float myRankRatio = simRacers > 1
            ? (float)(r.currentRank - 1) / (simRacers - 1) : 0f;
        float diff = myRankRatio - zoneTarget;

        if (diff >  gs.v3Settings.v3_zoneRange) return gs.v3Settings.v3_zoneDrainMul;
        if (diff < -gs.v3Settings.v3_zoneRange) return gs.v3Settings.v3_zoneSaveMul;
        return 1.0f;
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
        string header = string.Format("백테스팅 v3  |  {0}회 × {1}바퀴 × {2}명  |  충돌:{3}  |  트랙:{4}",
            simCount, simLaps, simRacers, settlingInfo,
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
        else
        {
            md.AppendLine("### 타입 보너스 (레거시)");
            md.AppendLine();
            md.AppendLine("| 타입 | 전반 | 중반 | 후반 |");
            md.AppendLine("|------|------|------|------|");
            md.AppendFormat("| Runner | {0} | {1} | {2} |\n", SF(g.earlyBonus_Runner), SF(g.midBonus_Runner), SF(g.lateBonus_Runner));
            md.AppendFormat("| Leader | {0} | {1} | {2} |\n", SF(g.earlyBonus_Leader), SF(g.midBonus_Leader), SF(g.lateBonus_Leader));
            md.AppendFormat("| Chaser | {0} | {1} | {2} |\n", SF(g.earlyBonus_Chaser), SF(g.midBonus_Chaser), SF(g.lateBonus_Chaser));
            md.AppendFormat("| Reckoner | {0} | {1} | {2} |\n", SF(g.earlyBonus_Reckoner), SF(g.midBonus_Reckoner), SF(g.lateBonus_Reckoner));
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
            TextAsset csv = Resources.Load<TextAsset>("Data/CharacterDB");
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

    // ══════════════════════════════════════
    //  V2 파라미터 스윕 — 최적 계수 자동 탐색
    // ══════════════════════════════════════

    private struct SweepResult
    {
        public float drainRate, exhaustFloor;
        public float runnerLate, leaderLate, chaserLate, reckonerLate;
        public float score;
        public string detail;
    }

    private void RunParameterSweep()
    {
        isRunning = true;
        lastLogPath = "";

        // 원본 값 백업
        var gs = gameSettings;
        float origDrainRate       = gs.v2_drainBaseRate;
        float origExhaustFloor    = gs.v2_exhaustFloor;
        float origRunnerLate      = gs.v2_phaseCoeff_Runner_late;
        float origLeaderLate      = gs.v2_phaseCoeff_Leader_late;
        float origChaserLate      = gs.v2_phaseCoeff_Chaser_late;
        float origReckonerLate    = gs.v2_phaseCoeff_Reckoner_late;
        int   origSimLaps         = simLaps;
        int   origSimRacers       = simRacers;
        int   origSimCount        = simCount;
        bool  origEqualStats      = equalStats;
        int   origEqualStatValue  = equalStatValue;
        bool  origShowPerRace     = showPerRace;
        bool  origSaveLog         = saveLog;

        try
        {
            RunParameterSweepInternal();
        }
        catch (System.Exception e)
        {
            resultText = "❌ 스윕 에러: " + e.Message + "\n" + e.StackTrace;
            Debug.LogError("[파라미터 스윕] " + e);
        }
        finally
        {
            // 원본 복원 (반드시 실행)
            gs.v2_drainBaseRate            = origDrainRate;
            gs.v2_exhaustFloor             = origExhaustFloor;
            gs.v2_phaseCoeff_Runner_late   = origRunnerLate;
            gs.v2_phaseCoeff_Leader_late   = origLeaderLate;
            gs.v2_phaseCoeff_Chaser_late   = origChaserLate;
            gs.v2_phaseCoeff_Reckoner_late = origReckonerLate;
            simLaps          = origSimLaps;
            simRacers        = origSimRacers;
            simCount         = origSimCount;
            equalStats       = origEqualStats;
            equalStatValue   = origEqualStatValue;
            showPerRace      = origShowPerRace;
            saveLog          = origSaveLog;

            EditorUtility.ClearProgressBar();
            isRunning = false;
        }
    }

    private void RunParameterSweepInternal()
    {
        var gs = gameSettings;

        // 스윕 조건 고정
        equalStats = true;
        equalStatValue = 20;
        simRacers = 12;
        showPerRace = false;
        saveLog = false;

        // ═══ 스윕 그리드 정의 (v2: 높은 드레인 + 탈진 패널티 강화) ═══
        // 이전 스윕에서 drainRate가 너무 낮아 5바퀴에서 HP가 거의 안 줄었음
        // → drainRate 대폭 상향, exhaustFloor 강화로 장거리 타입 역전 가능성 확보
        float[] drainRates    = { 0.50f, 1.00f, 1.50f, 2.00f };
        float[] exhaustFloors = { 0.60f, 0.70f };
        float[] runnerLates   = { 0.940f, 0.955f, 0.970f };
        float[] leaderLates   = { 0.970f, 0.980f, 0.990f };
        float[] chaserLates   = { 0.990f, 1.000f, 1.010f };
        float[] reckonerLates = { 1.000f, 1.015f, 1.030f };

        // 목표: (바퀴수, 목표 타입)
        int[] testLaps = { 2, 3, 4, 5 };
        CharacterType[] targetTypes = {
            CharacterType.Runner,
            CharacterType.Leader,
            CharacterType.Chaser,
            CharacterType.Reckoner
        };
        string[] targetTypeNames = { "도주", "선행", "선입", "추입" };

        int totalCombos = drainRates.Length * exhaustFloors.Length * runnerLates.Length
                        * leaderLates.Length * chaserLates.Length * reckonerLates.Length;

        List<CharacterData> allChars = LoadAllCharacters();
        if (allChars == null || allChars.Count == 0) return;

        // 조합 사전 생성 (단일 루프 → 안전한 취소)
        var paramSets = new List<float[]>();
        foreach (float dr  in drainRates)
        foreach (float ef  in exhaustFloors)
        foreach (float rl  in runnerLates)
        foreach (float ll  in leaderLates)
        foreach (float cl  in chaserLates)
        foreach (float rcl in reckonerLates)
            paramSets.Add(new float[] { dr, ef, rl, ll, cl, rcl });

        List<SweepResult> results = new List<SweepResult>();
        cancelRequested = false;

        for (int combo = 0; combo < paramSets.Count; combo++)
        {
            float dr  = paramSets[combo][0];
            float ef  = paramSets[combo][1];
            float rl  = paramSets[combo][2];
            float ll  = paramSets[combo][3];
            float cl  = paramSets[combo][4];
            float rcl = paramSets[combo][5];

            if (combo % 5 == 0)
            {
                bool cancelled = EditorUtility.DisplayCancelableProgressBar(
                    "V2 파라미터 스윕 v2",
                    $"조합 {combo + 1}/{totalCombos} | drain={dr:F2} floor={ef:F2} RL={rl:F3} RCL={rcl:F3}",
                    (float)(combo + 1) / totalCombos);
                if (cancelled) { cancelRequested = true; break; }
            }

            // 파라미터 적용
            gs.v2_drainBaseRate            = dr;
            gs.v2_exhaustFloor             = ef;
            gs.v2_phaseCoeff_Runner_late   = rl;
            gs.v2_phaseCoeff_Leader_late   = ll;
            gs.v2_phaseCoeff_Chaser_late   = cl;
            gs.v2_phaseCoeff_Reckoner_late = rcl;

            float score = 0f;
            StringBuilder detail = new StringBuilder();

            for (int li = 0; li < testLaps.Length; li++)
            {
                simLaps = testLaps[li];
                simCount = sweepSimsPerLap;

                // 시뮬레이션 실행
                var result = RunSimulationCore(allChars, 0, 1, "sweep");

                // 타입별 승수 집계
                var typeWinCount = new Dictionary<CharacterType, int>();
                foreach (CharacterType ct in System.Enum.GetValues(typeof(CharacterType)))
                    typeWinCount[ct] = 0;

                foreach (var kv in result.stats)
                {
                    var cd = allChars.Find(c => c.charId == kv.Key);
                    if (cd != null)
                        typeWinCount[cd.charType] += kv.Value.winCount;
                }

                float totalWins = sweepSimsPerLap;
                CharacterType targetType = targetTypes[li];
                float targetRate = typeWinCount[targetType] / totalWins;

                // 최고 승률 타입 찾기
                float maxOtherRate = 0f;
                string maxOtherName = "";
                foreach (var kv in typeWinCount)
                {
                    if (kv.Key != targetType)
                    {
                        float rate = kv.Value / totalWins;
                        if (rate > maxOtherRate)
                        {
                            maxOtherRate = rate;
                            maxOtherName = kv.Key.ToString();
                        }
                    }
                }

                // 스코어링
                if (targetRate >= maxOtherRate && targetRate > 0f)
                    score += 100f + (targetRate - maxOtherRate) * 300f;
                else if (targetRate >= maxOtherRate * 0.8f)
                    score += 30f - (maxOtherRate - targetRate) * 200f;
                else
                    score -= (maxOtherRate - targetRate) * 300f;

                detail.AppendFormat("{0}L:{1}={2:F0}%",
                    testLaps[li], targetTypeNames[li], targetRate * 100f);
                if (targetRate < maxOtherRate)
                    detail.AppendFormat("(X {0}={1:F0}%)", maxOtherName, maxOtherRate * 100f);
                else
                    detail.Append("(O)");
                if (li < testLaps.Length - 1) detail.Append("  ");
            }

            results.Add(new SweepResult
            {
                drainRate = dr,
                exhaustFloor = ef,
                runnerLate = rl,
                leaderLate = ll,
                chaserLate = cl,
                reckonerLate = rcl,
                score = score,
                detail = detail.ToString()
            });

            if (cancelRequested) break;
        }

        // 점수 내림차순 정렬
        results.Sort((a, b) => b.score.CompareTo(a.score));

        // 결과 출력
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("══════════════════════════════════════════════");
        sb.AppendLine("  V2 파라미터 스윕 결과");
        sb.AppendLine("══════════════════════════════════════════════");
        sb.AppendLine($"조건: equalStats={equalStatValue}, 시뮬={sweepSimsPerLap}회/바퀴, 총 {totalCombos} 조합 (v2 스윕: 높은드레인+탈진패널티)");
        sb.AppendLine($"목표: 2L→도주  3L→선행  4L→선입  5L→추입");
        sb.AppendLine();

        int showCount = Mathf.Min(30, results.Count);
        for (int i = 0; i < showCount; i++)
        {
            var r = results[i];
            sb.AppendFormat("#{0,-2} score={1,7:F1} | drain={2:F2} floor={3:F2}  RunL={4:F3}  LeadL={5:F3}  ChasL={6:F3}  ReckL={7:F3}\n",
                i + 1, r.score, r.drainRate, r.exhaustFloor, r.runnerLate, r.leaderLate, r.chaserLate, r.reckonerLate);
            sb.AppendLine($"     {r.detail}");
        }

        // 최적 파라미터 요약
        if (results.Count > 0)
        {
            var best = results[0];
            sb.AppendLine();
            sb.AppendLine("══════════════════════════════════════════════");
            sb.AppendLine("  BEST 파라미터 (Inspector에 복사)");
            sb.AppendLine("══════════════════════════════════════════════");
            sb.AppendLine($"v2_drainBaseRate            = {best.drainRate:F2}");
            sb.AppendLine($"v2_exhaustFloor             = {best.exhaustFloor:F2}");
            sb.AppendLine($"v2_phaseCoeff_Runner_late   = {best.runnerLate:F3}");
            sb.AppendLine($"v2_phaseCoeff_Leader_late   = {best.leaderLate:F3}");
            sb.AppendLine($"v2_phaseCoeff_Chaser_late   = {best.chaserLate:F3}");
            sb.AppendLine($"v2_phaseCoeff_Reckoner_late = {best.reckonerLate:F3}");
            sb.AppendLine($"Score = {best.score:F1}");
            sb.AppendLine(best.detail);
        }

        resultText = sb.ToString();

        // 로그 파일 저장
        string logDir = "Assets/BacktestLogs";
        if (!System.IO.Directory.Exists(logDir))
            System.IO.Directory.CreateDirectory(logDir);
        string timestamp = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string logPath = $"{logDir}/V2_Sweep_{timestamp}.txt";
        System.IO.File.WriteAllText(logPath, sb.ToString());
        lastLogPath = logPath;
        Debug.Log($"[V2 파라미터 스윕] 완료. {results.Count}개 조합 테스트. 로그: {logPath}");
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
