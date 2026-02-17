#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Text;
using System.Linq;

/// <summary>
/// 레이스 백테스팅 에디터 윈도우 (v2)
/// 충돌/슬링샷/회피 시뮬레이션 포함
/// 캐릭터별 손익 분석 출력
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
    private Vector2 scrollPos;
    private string resultText = "";
    private bool isRunning = false;

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
        EditorGUILayout.LabelField("🏇 레이스 백테스팅 v2", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        gameSettings = (GameSettings)EditorGUILayout.ObjectField("GameSettings", gameSettings, typeof(GameSettings), false);
        selectedTrack = (TrackData)EditorGUILayout.ObjectField("트랙 (None=일반)", selectedTrack, typeof(TrackData), false);

        EditorGUILayout.Space();
        simCount = EditorGUILayout.IntSlider("시뮬레이션 횟수", simCount, 10, 1000);
        simLaps = EditorGUILayout.IntSlider("바퀴 수", simLaps, 1, 10);
        simRacers = EditorGUILayout.IntSlider("참가자 수", simRacers, 2, 12);
        simTimeStep = EditorGUILayout.Slider("시간 단위 (초)", simTimeStep, 0.01f, 0.1f);
        simCollision = EditorGUILayout.Toggle("충돌 시뮬레이션", simCollision);
        showPerRace = EditorGUILayout.Toggle("개별 레이스 결과 표시", showPerRace);

        EditorGUILayout.Space();

        GUI.enabled = !isRunning && gameSettings != null;
        if (GUILayout.Button(isRunning ? "시뮬레이션 중..." : "▶ 시뮬레이션 실행", GUILayout.Height(30)))
        {
            RunSimulation();
        }
        GUI.enabled = true;

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

        // 통계
        public int critCount;
        public int collisionWins;
        public int collisionLosses;
        public int dodgeCount;
        public int slingshotCount;
        public float totalDistLost;     // 충돌 감속으로 잃은 거리
        public float totalDistGained;   // 슬링샷+크리티컬로 얻은 거리
    }

    // 슬링샷 예약
    private struct SlingshotReserve
    {
        public SimRacer racer;
        public float triggerTime;
        public float boost;
        public float duration;
    }

    // 쌍별 쿨다운
    private Dictionary<int, float> pairCooldowns = new Dictionary<int, float>();
    private List<SlingshotReserve> slingshotQueue = new List<SlingshotReserve>();

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
    }

    // ══════════════════════════════════════
    //  메인 시뮬레이션
    // ══════════════════════════════════════

    private void RunSimulation()
    {
        isRunning = true;

        TextAsset csv = Resources.Load<TextAsset>("Data/CharacterDB");
        if (csv == null) { resultText = "❌ CharacterDB.csv를 찾을 수 없습니다!"; isRunning = false; return; }

        List<CharacterData> allChars = new List<CharacterData>();
        string[] lines = csv.text.Split('\n');
        for (int i = 1; i < lines.Length; i++)
        {
            string line = lines[i].Trim();
            if (string.IsNullOrEmpty(line)) continue;
            var cd = CharacterData.ParseCSVLine(line);
            if (cd != null) allChars.Add(cd);
        }
        if (allChars.Count == 0) { resultText = "❌ 캐릭터 데이터 비어있음!"; isRunning = false; return; }

        int racerCount = Mathf.Min(simRacers, allChars.Count);
        var gs = gameSettings;

        Dictionary<string, CharStats> stats = new Dictionary<string, CharStats>();
        foreach (var c in allChars)
            stats[c.charName] = new CharStats { name = c.charName, type = c.GetTypeName() };

        // 글로벌 이벤트 카운터
        int globalCollisions = 0, globalDodges = 0, globalSlingshots = 0, globalCrits = 0;

        StringBuilder perRaceLog = new StringBuilder();
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
                racers.Add(new SimRacer
                {
                    data = cd, position = 0f,
                    currentSpeed = GetBaseSpeed(cd) * 0.5f,
                    noiseTimer = 0f, luckTimer = 0f,
                    critRemaining = 0f, critCount = 0,
                    collisionPenalty = 0f, collisionTimer = 0f,
                    slingshotBoost = 0f, slingshotTimer = 0f,
                    finished = false, finishOrder = 0
                });
                stats[cd.charName].raceCount++;
            }

            pairCooldowns.Clear();
            slingshotQueue.Clear();

            int finishedCount = 0;
            float simTime = 0f;

            while (finishedCount < racerCount && simTime < 300f)
            {
                simTime += simTimeStep;

                // 속도 계산 + 이동
                foreach (var r in racers)
                {
                    if (r.finished) continue;

                    float progress = Mathf.Clamp01(r.position / finishDistance);
                    float baseTarget = CalcSpeed(r, progress, simTime);

                    // 충돌 감속 처리
                    float penaltyMul = 1f;
                    if (r.collisionTimer > 0f)
                    {
                        r.collisionTimer -= simTimeStep;
                        penaltyMul = 1f - r.collisionPenalty;
                        float distLost = r.currentSpeed * r.collisionPenalty * simTimeStep;
                        r.totalDistLost += distLost;
                        if (r.collisionTimer <= 0f) { r.collisionPenalty = 0f; }
                    }

                    // 슬링샷 가속 처리
                    float slingshotMul = 1f;
                    if (r.slingshotTimer > 0f)
                    {
                        r.slingshotTimer -= simTimeStep;
                        slingshotMul = 1f + r.slingshotBoost;
                        float distGained = r.currentSpeed * r.slingshotBoost * simTimeStep;
                        r.totalDistGained += distGained;
                        if (r.slingshotTimer <= 0f) { r.slingshotBoost = 0f; }
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

                // 쿨다운 갱신
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
                if (r.finishOrder == 1) s.winCount++;
                if (r.finishOrder <= 3) s.top3Count++;

                globalCrits += r.critCount;
                globalCollisions += r.collisionWins;
                globalDodges += r.dodgeCount;
                globalSlingshots += r.slingshotCount;
            }

            if (showPerRace)
            {
                perRaceLog.AppendFormat("── Race #{0} ──\n", race + 1);
                foreach (var r in racers.OrderBy(r => r.finishOrder))
                    perRaceLog.AppendFormat("  {0}착: {1} crit:{2} col:{3}/{4} dodge:{5} sling:{6}\n",
                        r.finishOrder, r.data.charName, r.critCount,
                        r.collisionWins, r.collisionLosses, r.dodgeCount, r.slingshotCount);
                perRaceLog.AppendLine();
            }

            if (race % 50 == 0)
                EditorUtility.DisplayProgressBar("백테스팅", race + "/" + simCount, (float)race / simCount);
        }

        EditorUtility.ClearProgressBar();
        BuildResult(stats, globalCollisions, globalDodges, globalSlingshots, globalCrits, racerCount, perRaceLog);
        isRunning = false;
    }

    // ══════════════════════════════════════
    //  충돌 시뮬레이션
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

                // 1D 거리 (위치 차이)
                float dist = Mathf.Abs(racers[i].position - racers[j].position);
                if (dist >= range) continue;

                int pairKey = Mathf.Min(i, j) * 100 + Mathf.Max(i, j);
                if (pairCooldowns.ContainsKey(pairKey) && pairCooldowns[pairKey] > 0f) continue;

                // 밀집 감쇄
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

                // 충돌 발생 확률 체크
                if (Random.value > gs.collisionChance) continue;

                // 충돌!
                SimResolve(racers[i], racers[j], gs, track, simTime);
                pairCooldowns[pairKey] = gs.collisionCooldown;
            }
        }
    }

    private void SimResolve(SimRacer a, SimRacer b, GameSettings gs, TrackData track, float simTime)
    {
        // power 확률적 승패 결정
        float powerA = a.data.charBasePower;
        float powerB = b.data.charBasePower;

        float effA = powerA;
        float effB = powerB;
        if (powerA > powerB)
        {
            float benefit = powerA / (powerA + powerB);
            effA = powerA * (1f + benefit);
        }
        else if (powerB > powerA)
        {
            float benefit = powerB / (powerA + powerB);
            effB = powerB * (1f + benefit);
        }

        float totalEff = effA + effB;
        float bWinChance = totalEff > 0f ? effB / totalEff : 0.5f;

        SimRacer winner, loser;
        if (Random.value < bWinChance)
        { winner = b; loser = a; }
        else
        { winner = a; loser = b; }

        // luck 회피 (패자)
        float trackLuckMul = track != null ? track.luckMultiplier : 1f;
        float dodgeChance = loser.data.charBaseLuck * gs.luckDodgeChance * trackLuckMul;
        if (Random.value < dodgeChance)
        {
            loser.dodgeCount++;
            return;
        }

        // 감속 적용
        float trackPenMul = track != null ? track.collisionPenaltyMultiplier : 1f;
        float trackLoserDurMul = track != null ? track.loserPenaltyDurationMultiplier : 1f;

        winner.collisionPenalty = gs.collisionBasePenalty * 0.5f * trackPenMul;
        winner.collisionTimer = gs.winnerPenaltyDuration;
        winner.collisionWins++;

        loser.collisionPenalty = gs.collisionBasePenalty * trackPenMul;
        loser.collisionTimer = gs.loserPenaltyDuration * trackLoserDurMul;
        loser.collisionLosses++;

        // 슬링샷 예약 → 뒤에 있는 쪽
        SimRacer behind = a.position <= b.position ? a : b;
        float brave = behind.data.charBaseBrave;
        float slingshotMul = track != null ? track.slingshotMultiplier : 1f;
        float boost = brave * gs.slingshotFactor * slingshotMul;
        float behindDur = (behind == loser) ? loser.collisionTimer : winner.collisionTimer;

        slingshotQueue.Add(new SlingshotReserve
        {
            racer = behind,
            triggerTime = simTime + behindDur,
            boost = boost,
            duration = gs.slingshotDuration
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
        List<int> expired = new List<int>();
        List<int> keys = new List<int>(pairCooldowns.Keys);
        for (int i = 0; i < keys.Count; i++)
        {
            pairCooldowns[keys[i]] -= simTimeStep;
            if (pairCooldowns[keys[i]] <= 0f) expired.Add(keys[i]);
        }
        foreach (var k in expired) pairCooldowns.Remove(k);
    }

    // ══════════════════════════════════════
    //  결과 출력
    // ══════════════════════════════════════

    private void BuildResult(Dictionary<string, CharStats> stats,
        int globalCol, int globalDodge, int globalSling, int globalCrit,
        int racerCount, StringBuilder perRaceLog)
    {
        var sorted = stats.Values.Where(s => s.raceCount > 0).OrderByDescending(s => s.WinRate).ToList();

        StringBuilder r = new StringBuilder();
        r.AppendLine("═══════════════════════════════════════════════════════════════════");
        r.AppendFormat("  백테스팅 결과: {0}회 × {1}바퀴 × {2}명  |  충돌: {3}\n",
            simCount, simLaps, racerCount, simCollision ? "ON" : "OFF");
        r.AppendFormat("  트랙: {0}\n", selectedTrack != null ? selectedTrack.trackName : "일반");
        r.AppendLine("═══════════════════════════════════════════════════════════════════");

        // ── 글로벌 이벤트 요약 ──
        r.AppendFormat("\n  📊 총 이벤트: ⚡크리티컬 {0}  |  💥충돌 {1}  |  🛡️회피 {2}  |  🚀슬링샷 {3}\n",
            globalCrit, globalCol, globalDodge, globalSling);
        r.AppendFormat("     레이스당 평균: 크리티컬 {0:F1}  충돌 {1:F1}  회피 {2:F1}  슬링샷 {3:F1}\n\n",
            (float)globalCrit / simCount, (float)globalCol / simCount,
            (float)globalDodge / simCount, (float)globalSling / simCount);

        // ── 기본 순위 테이블 ──
        r.AppendLine("  ┌──── 순위/승률 ────────────────────────────────────────────┐");
        r.AppendLine("  이름  타입  출전  1착  Top3  평균순위  승률    Top3율  크리티컬");
        r.AppendLine("  ──── ──── ──── ──── ──── ────── ────── ────── ──────");

        foreach (var s in sorted)
        {
            r.AppendFormat("  {0,-4} {1,-3}  {2,4} {3,4} {4,4}   {5,5:F1}   {6,5:F1}%  {7,5:F1}%   {8,4:F2}\n",
                s.name, s.type, s.raceCount, s.winCount, s.top3Count,
                s.AvgRank, s.WinRate * 100, s.Top3Rate * 100, s.AvgCrits);
        }

        // ── 충돌 손익 테이블 ──
        if (simCollision)
        {
            r.AppendLine("\n  ┌──── 충돌 손익 분석 (레이스당 평균) ────────────────────────┐");
            r.AppendLine("  이름  pow brv lck  충돌승 충돌패 회피  슬링샷  잃은거리  얻은거리  순이득");
            r.AppendLine("  ──── ─── ─── ───  ───── ───── ──── ──────  ──────  ──────  ──────");

            var sortedByNet = sorted.OrderByDescending(s => s.AvgNetGain).ToList();
            foreach (var s in sortedByNet)
            {
                var cd = stats.Values.First(x => x.name == s.name);
                // CharacterData에서 pow/brv/lck 가져오기 위해 원본 참조
                CharacterData charData = null;
                TextAsset csvAsset = Resources.Load<TextAsset>("Data/CharacterDB");
                if (csvAsset != null)
                {
                    foreach (var line in csvAsset.text.Split('\n'))
                    {
                        if (line.Trim().StartsWith(s.name + ","))
                        {
                            charData = CharacterData.ParseCSVLine(line.Trim());
                            break;
                        }
                    }
                }

                int pow = charData != null ? (int)charData.charBasePower : 0;
                int brv = charData != null ? (int)charData.charBaseBrave : 0;
                int lck = charData != null ? (int)charData.charBaseLuck : 0;

                string netColor = s.AvgNetGain >= 0 ? "+" : "";

                r.AppendFormat("  {0,-4} {1,3} {2,3} {3,3}  {4,5:F1} {5,5:F1} {6,4:F1} {7,6:F1}  {8,7:F2}  {9,7:F2}  {10}{11:F2}\n",
                    s.name, pow, brv, lck,
                    s.AvgCollWins, s.AvgCollLosses, s.AvgDodges, s.AvgSlingshots,
                    s.AvgDistLost, s.AvgDistGained, netColor, s.AvgNetGain);
            }

            // ── 스탯별 영향 분석 ──
            r.AppendLine("\n  ┌──── 스탯 효과 분석 ──────────────────────────────────────┐");

            // power 상관관계
            var powerSorted = sortedByNet.OrderByDescending(s =>
            {
                var cd2 = FindCharData(s.name);
                return cd2 != null ? cd2.charBasePower : 0;
            }).ToList();

            r.AppendLine("  [power] 높을수록 충돌 승률 높음:");
            foreach (var s in powerSorted.Take(4))
            {
                var cd3 = FindCharData(s.name);
                int p = cd3 != null ? (int)cd3.charBasePower : 0;
                float winRate = (s.AvgCollWins + s.AvgCollLosses) > 0
                    ? s.AvgCollWins / (s.AvgCollWins + s.AvgCollLosses) * 100 : 0;
                r.AppendFormat("    {0} pow:{1} → 충돌승률:{2:F0}%\n", s.name, p, winRate);
            }

            r.AppendLine("  [brave] 높을수록 슬링샷 이득 큼:");
            var braveSorted = sortedByNet.OrderByDescending(s =>
            {
                var cd4 = FindCharData(s.name);
                return cd4 != null ? cd4.charBaseBrave : 0;
            }).ToList();
            foreach (var s in braveSorted.Take(4))
            {
                var cd5 = FindCharData(s.name);
                int b = cd5 != null ? (int)cd5.charBaseBrave : 0;
                r.AppendFormat("    {0} brv:{1} → 슬링샷:{2:F1}회 얻은거리:{3:F2}\n",
                    s.name, b, s.AvgSlingshots, s.AvgDistGained);
            }

            r.AppendLine("  [luck] 높을수록 회피 잘함:");
            var luckSorted = sortedByNet.OrderByDescending(s =>
            {
                var cd6 = FindCharData(s.name);
                return cd6 != null ? cd6.charBaseLuck : 0;
            }).ToList();
            foreach (var s in luckSorted.Take(4))
            {
                var cd7 = FindCharData(s.name);
                int l = cd7 != null ? (int)cd7.charBaseLuck : 0;
                r.AppendFormat("    {0} lck:{1} → 회피:{2:F1}회 크리티컬:{3:F2}회\n",
                    s.name, l, s.AvgDodges, s.AvgCrits);
            }
        }

        // ── 타입별 통계 ──
        r.AppendLine("\n  ┌──── 타입별 평균 ──────────────────────────────────────────┐");
        var typeGroups = sorted.GroupBy(s => s.type);
        foreach (var g in typeGroups)
        {
            r.AppendFormat("  {0,-3}  순위:{1:F1}  승률:{2:F1}%  크리티컬:{3:F2}",
                g.Key, g.Average(s => s.AvgRank),
                g.Average(s => s.WinRate) * 100, g.Average(s => s.AvgCrits));
            if (simCollision)
                r.AppendFormat("  순이득:{0:+0.00;-0.00}", g.Average(s => s.AvgNetGain));
            r.AppendLine();
        }

        // ── 밸런스 경고 ──
        r.AppendLine("\n═══════════════════════════════════════════════════════════════════");
        float maxWin = sorted.Max(s => s.WinRate);
        float minWin = sorted.Min(s => s.WinRate);
        if (maxWin > 0.3f)
            r.AppendFormat("  ⚠️ {0} 승률 {1:F1}% → 너무 높음!\n", sorted[0].name, maxWin * 100);
        if (minWin < 0.02f)
            r.AppendFormat("  ⚠️ {0} 승률 {1:F1}% → 너무 낮음!\n", sorted.Last().name, minWin * 100);

        float rankRange = sorted.Max(s => s.AvgRank) - sorted.Min(s => s.AvgRank);
        if (rankRange > 4.0f)
            r.AppendLine("  ⚠️ 평균 순위 편차 큼 → 밸런스 불균형 가능성");

        if (simCollision)
        {
            float maxNet = sorted.Max(s => s.AvgNetGain);
            float minNet = sorted.Min(s => s.AvgNetGain);
            if (maxNet - minNet > 2.0f)
                r.AppendLine("  ⚠️ 충돌 손익 편차 큼 → power/brave/luck 밸런스 조정 필요");
        }

        r.AppendLine("═══════════════════════════════════════════════════════════════════");

        if (showPerRace)
        {
            r.AppendLine("\n── 개별 레이스 ──");
            r.Append(perRaceLog.ToString());
        }

        resultText = r.ToString();
        Debug.Log(resultText);
    }

    // CharacterData 캐시
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

    // ══════════════════════════════════════
    //  속도 계산
    // ══════════════════════════════════════

    private float CalcSpeed(SimRacer r, float progress, float simTime)
    {
        var gs = gameSettings;
        var cd = r.data;
        TrackData track = selectedTrack;

        float trackSpeedMul = track != null ? track.speedMultiplier : 1f;
        float baseSpeed = cd.charBaseSpeed * gs.globalSpeedMultiplier * trackSpeedMul;

        // noise (calm)
        r.noiseTimer -= simTimeStep;
        if (r.noiseTimer <= 0f)
        {
            float calm = Mathf.Max(cd.charBaseCalm, 1f);
            float trackNoiseMul = track != null ? track.noiseMultiplier : 1f;
            float maxNoise = (1f / calm) * gs.noiseFactor * trackNoiseMul * gs.globalSpeedMultiplier;
            r.noiseValue = Random.Range(-maxNoise, maxNoise);
            r.noiseTimer = Random.Range(0.5f, 1.5f);
        }

        // fatigue (endurance)
        float trackFatMul = track != null ? track.fatigueMultiplier : 1f;
        float endurance = Mathf.Max(cd.charBaseEndurance, 1f);
        float fatigue = progress * (1f / endurance) * gs.fatigueFactor * trackFatMul;

        // type bonus
        int phase = progress < 0.35f ? 0 : progress < 0.70f ? 1 : 2;
        float typeBonus = gs.GetTypeBonus(cd.charType, phase);
        if (track != null)
        {
            float phaseMul = phase == 0 ? track.earlyBonusMultiplier :
                             phase == 1 ? track.midBonusMultiplier : track.lateBonusMultiplier;
            typeBonus *= phaseMul;
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
            // 크리티컬 거리 이득 추적
            float critGain = r.currentSpeed * (gs.luckCritBoost - 1f) * simTimeStep;
            r.totalDistGained += critGain;
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
        speed -= fatigue;
        speed *= slowMul * critMul;
        return Mathf.Max(speed, 0.1f);
    }

    private float GetBaseSpeed(CharacterData cd)
    {
        float trackMul = selectedTrack != null ? selectedTrack.speedMultiplier : 1f;
        return cd.charBaseSpeed * gameSettings.globalSpeedMultiplier * trackMul;
    }
}
#endif