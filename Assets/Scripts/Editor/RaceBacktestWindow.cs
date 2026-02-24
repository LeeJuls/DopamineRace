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
    private Vector2 scrollPos;
    private string resultText = "";
    private string lastLogPath = "";
    private bool isRunning = false;
    private bool cancelRequested = false;

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

        GUI.enabled = !isRunning && gameSettings != null;
        if (GUILayout.Button(isRunning ? "시뮬레이션 중..." : "▶ 시뮬레이션 실행", GUILayout.Height(30)))
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
        public float slipstreamBlend;     // Chaser 슬립스트림 페이드 (0~1)
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
            stats[c.charName] = new CharStats { name = c.charName, type = c.GetTypeName() };

        int globalCollisions = 0, globalDodges = 0, globalSlingshots = 0, globalCrits = 0;
        float totalTrackLength = 17f;
        float finishDistance = totalTrackLength * simLaps;

        for (int race = 0; race < simCount; race++)
        {
            // 랜덤 선발
            List<CharacterData> selected = new List<CharacterData>(allChars);
            while (selected.Count > racerCount)
                selected.RemoveAt(Random.Range(0, selected.Count));

            List<SimRacer> racers = new List<SimRacer>();
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

                // HP 시스템 초기화
                if (gs.useHPSystem)
                {
                    racer.maxHP = gs.CalcMaxHP(cd.charBaseEndurance);
                    racer.enduranceHP = racer.maxHP;
                    racer.totalConsumedHP = 0f;
                    racer.hpBoostValue = 0f;
                    racer.currentRank = 0;
                    racer.slipstreamBlend = 0f;
                }

                racers.Add(racer);
                stats[cd.charName].raceCount++;
            }

            pairCooldowns.Clear();
            slingshotQueue.Clear();

            int finishedCount = 0;
            float simTime = 0f;

            while (finishedCount < racerCount && simTime < 300f)
            {
                simTime += simTimeStep;

                // ═══ Phase 4: 순위 + 슬립스트림 갱신 ═══
                if (gs.useHPSystem)
                {
                    for (int ri = 0; ri < racers.Count; ri++)
                    {
                        if (racers[ri].finished) continue;
                        int rank = 1;
                        for (int rj = 0; rj < racers.Count; rj++)
                        {
                            if (ri != rj && racers[rj].position > racers[ri].position)
                                rank++;
                        }
                        racers[ri].currentRank = rank;

                        // Slipstream 블렌드 (Chaser 전용)
                        if (racers[ri].data.charType == CharacterType.Chaser)
                        {
                            float target = (rank >= 3 && rank <= 7) ? 1f : 0f;
                            racers[ri].slipstreamBlend = Mathf.MoveTowards(
                                racers[ri].slipstreamBlend, target, simTimeStep / 2f);
                        }
                    }
                }

                foreach (var r in racers)
                {
                    if (r.finished) continue;

                    float progress = Mathf.Clamp01(r.position / finishDistance);
                    float baseTarget = CalcSpeed(r, progress, simTime);

                    // 충돌 감속
                    float penaltyMul = 1f;
                    if (r.collisionTimer > 0f)
                    {
                        r.collisionTimer -= simTimeStep;
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
                        r.slingshotTimer -= simTimeStep;
                        slingshotMul = 1f + r.slingshotBoost;
                        float distGained = r.currentSpeed * r.slingshotBoost * simTimeStep;
                        r.totalDistGained += distGained;
                        r.contrib_brave += distGained;  // 슬링샷 이득
                        if (r.slingshotTimer <= 0f) r.slingshotBoost = 0f;
                    }

                    float targetSpeed = baseTarget * penaltyMul * slingshotMul;
                    r.currentSpeed = Mathf.Lerp(r.currentSpeed, targetSpeed, simTimeStep * gs.raceSpeedLerp);
                    r.position += r.currentSpeed * simTimeStep;

                    if (r.position >= finishDistance)
                    {
                        r.finished = true;
                        finishedCount++;
                        r.finishOrder = finishedCount;
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
                var s = stats[r.data.charName];
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
            globalSlingshots = globalSlingshots
        };
    }

    // ══════════════════════════════════════
    //  충돌 시뮬레이션 (기존 유지)
    // ══════════════════════════════════════

    private void SimCollisions(List<SimRacer> racers, GameSettings gs, float simTime)
    {
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

        // noise (calm)
        r.noiseTimer -= simTimeStep;
        if (r.noiseTimer <= 0f)
        {
            float calm = Mathf.Max(cd.charBaseCalm, 1f);
            float trackNoiseMul = track != null ? track.noiseMultiplier : 1f;
            float maxNoise = (1f / calm) * gs.noiseFactor * trackNoiseMul * globalMul;
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
            // ═══ HP 시스템 (SPEC-006) ═══
            // 속도 압축: 캐릭터 간 속도 차이를 줄여 HP 부스트가 역전 가능하게
            // 중간점 = 0.905 (SpeedMultiplier 범위 0.81~1.0의 중앙값)
            if (gs.hpSpeedCompress > 0f)
            {
                float midSpeed = 0.905f * globalMul * trackSpeedMul;
                baseSpeed = Mathf.Lerp(baseSpeed, midSpeed, gs.hpSpeedCompress);
            }
            SimConsumeHP(r, gs, progress);
            float hpBoost = SimCalcHPBoost(r, gs);
            typeBonus = hpBoost;
            // ★ HP 부스트 기여 (type + endurance를 통합 대체)
            r.contrib_type += baseSpeed * hpBoost * simTimeStep;
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
        if (r.critRemaining > 0f)
        {
            r.critRemaining -= simTimeStep;
            critMul = gs.luckCritBoost;
            float critGain = r.currentSpeed * (gs.luckCritBoost - 1f) * simTimeStep;
            r.totalDistGained += critGain;
            r.contrib_luck += critGain;  // ★ luck 기여
            if (r.critRemaining <= 0f) r.isCrit = false;
        }
        else
        {
            r.luckTimer -= simTimeStep;
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
        speed *= slowMul * critMul;
        return Mathf.Max(speed, 0.1f);
    }

    // ══════════════════════════════════════
    //  HP 시스템 미러 (SPEC-006)
    // ══════════════════════════════════════

    /// <summary>HP 소모 (RacerController.ConsumeHP 미러)</summary>
    private void SimConsumeHP(SimRacer r, GameSettings gs, float progress)
    {
        if (r.enduranceHP <= 0f) return;

        gs.GetHPParams(r.data.charType,
            out float spurtStart, out float activeRate, out _,
            out _, out _, out _);

        float effectiveActiveRate = progress >= spurtStart ? activeRate : 0f;
        float effectiveBasicRate = gs.basicConsumptionRate;

        // ═══ Phase 4: 포지션 보정 (SPEC-006 §5) ═══
        switch (r.data.charType)
        {
            case CharacterType.Leader:
                // Pace Lead: 1~3위에서 activeRate 절감, 후반 약화
                if (r.currentRank >= 1 && r.currentRank <= 3)
                {
                    float paceLeadEffect = gs.paceLeadReduction;
                    if (progress > 0.7f)
                    {
                        float fade = 1f - (progress - 0.7f) / 0.3f;
                        paceLeadEffect *= Mathf.Max(0f, fade);
                    }
                    effectiveActiveRate *= (1f - paceLeadEffect);
                }
                break;

            case CharacterType.Chaser:
                // Slipstream: 3~7위에서 basicRate 절감
                effectiveBasicRate *= (1f - gs.slipstreamReduction * r.slipstreamBlend);
                break;

            case CharacterType.Reckoner:
                // Conservation Amp: 잔여 HP 많을수록 activeRate 증폭
                if (effectiveActiveRate > 0f && r.maxHP > 0f)
                {
                    float remainingRatio = r.enduranceHP / r.maxHP;
                    float amplifier = 1f + Mathf.Max(0f, remainingRatio - 0.5f) * gs.conservationAmpCoeff;
                    effectiveActiveRate *= amplifier;
                }
                break;
        }

        float trackSpeedMul = selectedTrack != null ? selectedTrack.speedMultiplier : 1f;
        float baseTrackSpeed = r.data.SpeedMultiplier * gs.globalSpeedMultiplier * trackSpeedMul;
        float speedRatio = baseTrackSpeed > 0.01f ? r.currentSpeed / baseTrackSpeed : 1f;
        speedRatio = Mathf.Clamp(speedRatio, 0.1f, 2f);

        float consumption = (effectiveBasicRate + effectiveActiveRate) * Mathf.Sqrt(speedRatio) * simTimeStep;
        consumption = Mathf.Min(consumption, r.enduranceHP);

        r.enduranceHP -= consumption;
        r.totalConsumedHP += consumption;
    }

    /// <summary>HP 부스트 계산 (RacerController.CalcHPBoost 미러)</summary>
    private float SimCalcHPBoost(SimRacer r, GameSettings gs)
    {
        gs.GetHPParams(r.data.charType,
            out _, out _, out float peakBoost,
            out float accelExp, out float decelExp, out float exhaustionFloor);

        float consumedRatio = r.maxHP > 0f ? r.totalConsumedHP / r.maxHP : 0f;
        float threshold = gs.boostThreshold;

        float boost;
        if (consumedRatio <= threshold)
        {
            float t = threshold > 0f ? consumedRatio / threshold : 0f;
            boost = peakBoost * Mathf.Pow(t, accelExp);
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
            koNames[c.charName] = Loc.Get(c.charName);
        if (prevLang != "ko") Loc.SetLang(prevLang);
        // koName 헬퍼 — UID → ko 이름, 실패 시 UID 그대로
        System.Func<string, string> KN = (uid) =>
            koNames.ContainsKey(uid) ? koNames[uid] : uid;

        StringBuilder display = new StringBuilder();
        StringBuilder md = new StringBuilder();
        bool multiTrack = results.Count > 1;

        // ── 헤더 ──
        string header = string.Format("백테스팅 v3  |  {0}회 × {1}바퀴 × {2}명  |  충돌:{3}  |  트랙:{4}",
            simCount, simLaps, simRacers, simCollision ? "ON" : "OFF",
            multiTrack ? "전체 " + results.Count + "종" : results[0].trackName);

        display.AppendLine("═══════════════════════════════════════════════════════════════════");
        display.AppendLine("  " + header);
        display.AppendLine("═══════════════════════════════════════════════════════════════════");

        md.AppendLine("# 🏇 백테스팅 리포트");
        md.AppendLine();
        md.AppendFormat("> **날짜**: {0}  \n", System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
        md.AppendFormat("> **설정**: {0}회 × {1}바퀴 × {2}명 | 충돌:{3}  \n",
            simCount, simLaps, simRacers, simCollision ? "ON" : "OFF");
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
            var charNames = results[0].stats.Keys.Where(k => results[0].stats[k].raceCount > 0).ToList();
            foreach (var cn in charNames)
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
    private CharacterData FindCharData(string name)
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
                    if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith("char_name")) continue;
                    var cd = CharacterData.ParseCSVLine(trimmed);
                    if (cd != null) charDataCache[cd.charName] = cd;
                }
            }
        }
        return charDataCache.ContainsKey(name) ? charDataCache[name] : null;
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
