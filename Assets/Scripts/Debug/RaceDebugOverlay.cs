using UnityEngine;
using System.Collections.Generic;
using System.Text;

/// <summary>
/// 레이스 디버그 오버레이
/// F1: 토글, F2: 간략/상세, F3: 라운드 순회
/// 
/// 로그 정책:
///   - 라운드별 개별 저장 (R1, R2, R3...)
///   - 라운드가 넘어가도 이전 로그 유지
///   - 새 게임(StartNewGame) 시에만 전체 초기화
/// 
/// 이벤트 분류:
///   - ⚡ 레이싱 중: 크리티컬, 충돌, 회피, 슬링샷
///   - 🏁 완주 기록: 도착 순서
/// </summary>
public class RaceDebugOverlay : MonoBehaviour
{
    private bool showDebug = false;  // 기본 비표시 → F1으로 토글
    private bool showDetail = false;
    private int viewingRound = -1;  // -1 = 현재 라운드
    private Vector2 scrollPos;
    private Vector2 raceLogScroll;
    private Vector2 finishLogScroll;
    private GUIStyle headerStyle;
    private GUIStyle normalStyle;
    private GUIStyle critStyle;
    private GUIStyle copyBtnStyle;
    private bool stylesInitialized = false;

    private void Awake()
    {
        // GameSettings.enableRaceDebug = true → 시작 시 바로 표시
        // false → 숨김 상태로 시작, F1으로 토글 가능
        if (GameSettings.Instance != null)
            showDebug = GameSettings.Instance.enableRaceDebug;
    }

    // ── 갱신 주기 ──
    private float refreshInterval = 0.2f;
    private float refreshTimer = 0f;
    private string cachedSimpleText = "";
    private string cachedDetailText = "";

    // ── 복사 피드백 ──
    private float copyFeedbackTimer = 0f;
    private string copyFeedbackMsg = "";

    // ══════════════════════════════════════
    //  라운드별 이벤트 저장소
    // ══════════════════════════════════════

    /// <summary>모든 라운드의 로그. 새 게임 시에만 Clear</summary>
    private Dictionary<int, RoundLog> allRoundLogs = new Dictionary<int, RoundLog>();
    private int currentRound = 0;
    private const int MAX_EVENTS_PER_ROUND = 300;

    private Dictionary<int, bool> prevCritState = new Dictionary<int, bool>();

    // ── 바퀴별 HP 스냅샷 추적 ──
    private Dictionary<int, int> prevLapState = new Dictionary<int, int>();  // racerIndex → 마지막 기록된 lap
    private int lastSnapshotLap = -1;  // 마지막으로 스냅샷을 찍은 바퀴

    // ── HP 탭 UI ──
    private bool showHPTab = true;
    private Vector2 hpTabScroll;

    public enum EventType { Critical, CollisionHit, CollisionDodge, Slingshot, Attack, Finish, Track }

    public struct RaceEvent
    {
        public float time;
        public EventType type;
        public string description;

        public string GetIcon()
        {
            switch (type)
            {
                case EventType.Critical:       return "⚡";
                case EventType.CollisionHit:   return "💥";
                case EventType.CollisionDodge: return "🛡️";
                case EventType.Slingshot:      return "🚀";
                case EventType.Attack:         return "⚔️";
                case EventType.Finish:         return "🏁";
                case EventType.Track:          return "🗺️";
                default: return "•";
            }
        }

        public bool IsRacingEvent() => type != EventType.Finish;

        /// <summary>복사용 plain text (리치텍스트 제거)</summary>
        public string ToPlainText()
        {
            return string.Format("[{0:F1}s] {1} {2}", time, GetIcon(), description);
        }
    }

    public class RoundLog
    {
        public int round;
        public List<RaceEvent> racingEvents = new List<RaceEvent>();
        public List<RaceEvent> finishEvents = new List<RaceEvent>();
        public string reportText = "";
        public List<LapSnapshot> lapSnapshots = new List<LapSnapshot>();
    }

    /// <summary>바퀴 완료 시 전체 캐릭터 스냅샷</summary>
    public class LapSnapshot
    {
        public int lap;
        public List<LapRacerInfo> racers = new List<LapRacerInfo>();
    }

    public struct LapRacerInfo
    {
        public int rank;
        public string name;
        public string typeName;
        public float hpPercent;   // 0~100
        public float cpPercent;   // 0~100
    }

    private RoundLog GetOrCreateLog(int round)
    {
        if (!allRoundLogs.ContainsKey(round))
            allRoundLogs[round] = new RoundLog { round = round };
        return allRoundLogs[round];
    }

    /// <summary>현재 라운드에 이벤트 기록</summary>
    public void LogEvent(EventType type, string desc)
    {
        var log = GetOrCreateLog(currentRound);
        var evt = new RaceEvent { time = Time.time, type = type, description = desc };

        if (evt.IsRacingEvent())
        {
            log.racingEvents.Add(evt);
            if (log.racingEvents.Count > MAX_EVENTS_PER_ROUND)
                log.racingEvents.RemoveAt(0);
        }
        else
        {
            log.finishEvents.Add(evt);
        }
    }

    // ══════════════════════════════════════
    //  라운드 / 게임 생명주기
    // ══════════════════════════════════════

    /// <summary>
    /// 라운드 시작 시 호출.
    /// ※ 이전 라운드 로그는 절대 지우지 않음!
    /// </summary>
    public void StartRound(int round)
    {
        currentRound = round;
        viewingRound = -1;
        prevCritState.Clear();
        prevLapState.Clear();
        lastSnapshotLap = -1;

        // 새 라운드 로그 생성 (덮어쓰기 아님, 새로 만듦)
        allRoundLogs[round] = new RoundLog { round = round };

        Debug.Log("[Debug] 라운드 " + round + " 로그 시작 (보존중: " + allRoundLogs.Count + "R)");
    }

    /// <summary>
    /// 라운드 종료 시 호출: 리포트 저장
    /// </summary>
    public void SaveRoundReport(int round, List<RaceManager.RankingEntry> rankings)
    {
        if (!allRoundLogs.ContainsKey(round)) return;
        var log = allRoundLogs[round];

        // ── 완주 시점 HP 스냅샷 (골인 후 남은 HP) ──
        var rm = RaceManager.Instance;
        Dictionary<int, float> hpByIndex = new Dictionary<int, float>();
        if (rm != null)
        {
            foreach (var racer in rm.Racers)
            {
                float hpPct = racer.MaxHP > 0f ? (racer.EnduranceHP / racer.MaxHP) * 100f : 0f;
                hpByIndex[racer.RacerIndex] = hpPct;
            }
        }

        StringBuilder sb = new StringBuilder();
        sb.AppendFormat("<color=yellow>═══ 라운드 {0} 리포트 ═══</color>\n", round);

        int critCount = 0, collisionCount = 0, dodgeCount = 0, slingshotCount = 0;
        Dictionary<string, int> critByChar = new Dictionary<string, int>();

        foreach (var e in log.racingEvents)
        {
            switch (e.type)
            {
                case EventType.Critical:
                    critCount++;
                    string cname = e.description.Split(' ')[0];
                    if (!critByChar.ContainsKey(cname)) critByChar[cname] = 0;
                    critByChar[cname]++;
                    break;
                case EventType.CollisionHit: collisionCount++; break;
                case EventType.CollisionDodge: dodgeCount++; break;
                case EventType.Slingshot: slingshotCount++; break;
            }
        }

        sb.AppendFormat("  ⚡ 크리티컬: {0}회\n", critCount);
        foreach (var kv in critByChar)
            sb.AppendFormat("     - {0}: {1}회\n", kv.Key, kv.Value);
        sb.AppendFormat("  💥 충돌: {0}회  |  🛡️ 회피: {1}회  |  🚀 슬링샷: {2}회\n",
            collisionCount, dodgeCount, slingshotCount);
        sb.AppendLine("───────────────────────────");
        sb.AppendLine("  최종 순위:");
        for (int i = 0; i < rankings.Count; i++)
        {
            float hpPct = hpByIndex.ContainsKey(rankings[i].racerIndex) ? hpByIndex[rankings[i].racerIndex] : 0f;
            string hpColor = hpPct > 30f ? "#66FF66" : hpPct > 10f ? "#FFAA44" : "#FF4444";
            sb.AppendFormat("    {0}착: {1}  <color={2}>HP:{3:F0}%</color>\n",
                rankings[i].rank, rankings[i].racerName, hpColor, hpPct);
        }

        log.reportText = sb.ToString();

        // ── 완주 이벤트 누락 보완 ──
        // 마지막 완주자는 raceActive=false 직후라 LateUpdate가 건너뛰어 미기록됨
        if (rm != null)
        {
            foreach (var entry in rankings)
            {
                bool alreadyLogged = false;
                foreach (var fe in log.finishEvents)
                {
                    if (fe.description.Contains(entry.racerName)) { alreadyLogged = true; break; }
                }
                if (!alreadyLogged)
                {
                    float hpPct = hpByIndex.ContainsKey(entry.racerIndex) ? hpByIndex[entry.racerIndex] : 0f;
                    var racer = rm.Racers.Find(r => r.RacerIndex == entry.racerIndex);
                    string typeName = racer?.CharData?.GetTypeName() ?? "?";
                    float spd = racer?.CharData?.charBaseSpeed ?? 0f;
                    LogEvent(EventType.Finish, string.Format("{0} {1}착 완주! (SPD:{2:F2} {3}) HP:{4:F0}%",
                        entry.racerName, entry.rank, spd, typeName, hpPct));
                }
            }
        }

        // Console 출력 (plain text)
        string plain = System.Text.RegularExpressions.Regex.Replace(sb.ToString(), "<[^>]+>", "");
        Debug.Log(plain);
    }

    /// <summary>
    /// 새 게임 시작 시에만 호출: 모든 라운드 로그 완전 초기화
    /// </summary>
    public void ClearAllLogs()
    {
        allRoundLogs.Clear();
        prevCritState.Clear();
        currentRound = 0;
        viewingRound = -1;
        Debug.Log("[Debug] 전체 로그 초기화 (새 게임)");
    }

    /// <summary>하위 호환 (아무 동작 안 함 - 로그 유지)</summary>
    public void ClearLog() { }

    // ══════════════════════════════════════
    //  ★ 로그 복사 기능
    // ══════════════════════════════════════

    /// <summary>
    /// 특정 라운드의 전체 이벤트 로그를 plain text로 반환
    /// </summary>
    private string BuildCopyText(int round)
    {
        if (!allRoundLogs.ContainsKey(round)) return "(로그 없음)";
        var log = allRoundLogs[round];

        StringBuilder sb = new StringBuilder();
        sb.AppendFormat("═══ 라운드 {0} 이벤트 로그 ═══\n", round);

        // 레이싱 이벤트
        sb.AppendFormat("\n▶ 레이싱 이벤트 ({0}건)\n", log.racingEvents.Count);
        sb.AppendLine("────────────────────────");
        foreach (var e in log.racingEvents)
            sb.AppendLine(e.ToPlainText());

        // 완주 기록
        sb.AppendFormat("\n▶ 완주 기록 ({0}건)\n", log.finishEvents.Count);
        sb.AppendLine("────────────────────────");
        foreach (var e in log.finishEvents)
            sb.AppendLine(e.ToPlainText());

        // HP 바퀴별 스냅샷
        if (log.lapSnapshots.Count > 0)
        {
            sb.AppendFormat("\n▶ HP 바퀴별 ({0}바퀴)\n", log.lapSnapshots.Count);
            sb.AppendLine("────────────────────────");
            foreach (var snapshot in log.lapSnapshots)
            {
                sb.AppendFormat("── {0}바퀴 완료 ──\n", snapshot.lap);
                foreach (var info in snapshot.racers)
                {
                    sb.AppendFormat("  {0}위 {1} ({2})  HP:{3:F0}%\n",
                        info.rank, info.name, info.typeName, info.hpPercent);
                }
            }
        }

        // 리포트 (있으면)
        if (!string.IsNullOrEmpty(log.reportText))
        {
            sb.AppendLine("\n▶ 라운드 리포트");
            sb.AppendLine("────────────────────────");
            // 리치텍스트 태그 제거
            string plain = log.reportText;
            plain = System.Text.RegularExpressions.Regex.Replace(plain, "<[^>]+>", "");
            sb.AppendLine(plain);
        }

        return sb.ToString();
    }

    /// <summary>
    /// 모든 라운드의 이벤트 로그를 plain text로 반환
    /// </summary>
    private string BuildCopyTextAll()
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("═══ 전체 라운드 이벤트 로그 ═══");
        sb.AppendLine();

        var sortedKeys = new List<int>(allRoundLogs.Keys);
        sortedKeys.Sort();

        foreach (int round in sortedKeys)
        {
            sb.AppendLine(BuildCopyText(round));
            sb.AppendLine();
        }

        return sb.ToString();
    }

    /// <summary>
    /// 클립보드에 복사 + 피드백 표시
    /// </summary>
    private void CopyToClipboard(string text, string feedbackMsg)
    {
        GUIUtility.systemCopyBuffer = text;
        copyFeedbackMsg = feedbackMsg;
        copyFeedbackTimer = 2f;
        Debug.Log("[Debug] 클립보드 복사 완료: " + feedbackMsg);
    }

    // ══════════════════════════════════════
    //  Update / LateUpdate
    // ══════════════════════════════════════

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F1)) showDebug = !showDebug;
        if (Input.GetKeyDown(KeyCode.F2)) showDetail = !showDetail;
        if (Input.GetKeyDown(KeyCode.F3))
        {
            // -1(현재) → 1 → 2 → ... → currentRound → -1
            if (viewingRound == -1)
                viewingRound = allRoundLogs.Count > 0 ? 1 : -1;
            else
            {
                viewingRound++;
                if (viewingRound > currentRound) viewingRound = -1;
            }
        }

        refreshTimer -= Time.deltaTime;
        if (refreshTimer <= 0f)
        {
            refreshTimer = refreshInterval;
            RebuildCache();
        }

        // 복사 피드백 타이머
        if (copyFeedbackTimer > 0f)
            copyFeedbackTimer -= Time.deltaTime;
    }

    private void LateUpdate()
    {
        var rm = RaceManager.Instance;
        if (rm == null || !rm.RaceActive) return;

        // ── 바퀴별 HP 스냅샷 체크 ──
        CheckLapSnapshots(rm);

        foreach (var racer in rm.Racers)
        {
            if (racer.CharData == null) continue;
            int idx = racer.RacerIndex;

            bool wasCrit = prevCritState.ContainsKey(idx) && prevCritState[idx];
            bool isCrit = racer.IsCritActive;

            if (isCrit && !wasCrit)
            {
                LogEvent(EventType.Critical,
                    string.Format("{0} (luck:{1}) 크리티컬!",
                        racer.CharData.DisplayName, racer.CharData.charBaseLuck));
            }

            if (racer.IsFinished && racer.FinishOrder > 0)
            {
                var log = GetOrCreateLog(currentRound);
                bool already = false;
                foreach (var e in log.finishEvents)
                {
                    if (e.description.Contains(racer.CharData.DisplayName)) { already = true; break; }
                }
                if (!already)
                {
                    float hpPct = racer.MaxHP > 0f ? (racer.EnduranceHP / racer.MaxHP) * 100f : 0f;
                    LogEvent(EventType.Finish,
                        string.Format("{0} {1}착 완주! (SPD:{2:F2} {3}) HP:{4:F0}%",
                            racer.CharData.DisplayName, racer.FinishOrder,
                            racer.CharData.charBaseSpeed, racer.CharData.GetTypeName(),
                            hpPct));
                }
            }

            prevCritState[idx] = isCrit;
        }

        // 현재 라운드 보기 시 자동 스크롤
        if (viewingRound == -1)
        {
            raceLogScroll.y = float.MaxValue;
            finishLogScroll.y = float.MaxValue;
        }
    }

    /// <summary>
    /// 바퀴 완료 시 HP 스냅샷 기록.
    /// 선두 주자의 currentLap이 변할 때 전체 레이서 상태를 기록한다.
    /// </summary>
    private void CheckLapSnapshots(RaceManager rm)
    {
        // 선두 주자의 현재 바퀴 확인 (가장 높은 currentLap)
        int maxLap = 0;
        foreach (var racer in rm.Racers)
        {
            if (racer.IsFinished) continue;
            if (racer.CurrentLap > maxLap) maxLap = racer.CurrentLap;
        }

        // 바퀴가 넘어갔고, 아직 스냅샷을 안 찍은 바퀴가 있으면 기록
        // (예: 0→1 넘어가면 "1바퀴 완료" 시점 스냅샷)
        if (maxLap > 0 && maxLap > lastSnapshotLap)
        {
            // 중간에 놓친 바퀴가 있을 수 있으므로 루프
            for (int lap = lastSnapshotLap + 1; lap <= maxLap; lap++)
            {
                if (lap <= 0) continue;
                TakeLapSnapshot(rm, lap);
            }
            lastSnapshotLap = maxLap;
        }
    }

    /// <summary>특정 바퀴 완료 시 전체 레이서 HP 스냅샷 기록</summary>
    private void TakeLapSnapshot(RaceManager rm, int completedLap)
    {
        var log = GetOrCreateLog(currentRound);

        // 중복 방지
        foreach (var existing in log.lapSnapshots)
        {
            if (existing.lap == completedLap) return;
        }

        var snapshot = new LapSnapshot { lap = completedLap };

        // 현재 순위 기반 정렬
        var ranked = rm.GetLiveRankings();
        for (int i = 0; i < ranked.Count; i++)
        {
            var racer = ranked[i];
            if (racer.CharData == null) continue;

            float hpPct = racer.MaxHP > 0f ? (racer.EnduranceHP / racer.MaxHP) * 100f : 0f;
            float cpPct = racer.MaxCPValue > 0f ? (racer.CalmPoints / racer.MaxCPValue) * 100f : 0f;

            snapshot.racers.Add(new LapRacerInfo
            {
                rank = i + 1,
                name = racer.CharData.DisplayName,
                typeName = racer.CharData.GetTypeName(),
                hpPercent = hpPct,
                cpPercent = cpPct
            });
        }

        log.lapSnapshots.Add(snapshot);
        Debug.Log(string.Format("[Debug] {0}바퀴 HP 스냅샷 기록 ({1}명)", completedLap, snapshot.racers.Count));
    }

    // ══════════════════════════════════════
    //  캐시 빌드
    // ══════════════════════════════════════

    private void RebuildCache()
    {
        var rm = RaceManager.Instance;
        if (rm == null || rm.Racers == null || rm.Racers.Count == 0) return;

        var gs = GameSettings.Instance;
        TrackData track = gs.currentTrack;
        var rankings = rm.GetLiveRankings();

        // 간략
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("<color=yellow>순위  이름    타입   속도    진행     상태</color>");
        for (int i = 0; i < rankings.Count; i++)
        {
            var racer = rankings[i];
            var cd = racer.CharData;
            if (cd == null) continue;
            string status = ""; string col = "white";
            if (racer.IsFinished) { status = "🏁" + racer.FinishOrder + "착"; col = "#AAAAAA"; }
            else if (racer.IsCritActive) { status = "⚡크리티컬!"; col = "#FF8800"; }
            else if (racer.CollisionPenalty > 0) { status = "💥-" + (int)(racer.CollisionPenalty * 100) + "%"; col = "#FF6666"; }
            else if (racer.SlingshotBoost > 0) { status = "🚀+" + (int)(racer.SlingshotBoost * 100) + "%"; col = "#66FF66"; }

            sb.AppendFormat("<color={0}>{1,2}위  {2,-4}  {3,-3}  {4,5:F2}  {5,5:F1}%  {6}</color>\n",
                col, i + 1, cd.DisplayName, cd.GetTypeName(),
                racer.CurrentSpeed, racer.OverallProgress * 100, status);
        }
        cachedSimpleText = sb.ToString();

        // 상세
        sb.Clear();
        for (int i = 0; i < rankings.Count; i++)
        {
            var racer = rankings[i];
            var cd = racer.CharData;
            if (cd == null) continue;
            float progress = racer.OverallProgress;
            int phase = progress < 0.35f ? 0 : progress < 0.70f ? 1 : 2;
            string phaseName = phase == 0 ? "전반" : phase == 1 ? "중반" : "후반";
            float typeBonus = gs.GetTypeBonus(cd.charType, phase);
            float trackSpd = track != null ? track.speedMultiplier : 1f;
            float baseSpd = cd.SpeedMultiplier * gs.globalSpeedMultiplier * trackSpd;
            float endurance = Mathf.Max(cd.charBaseEndurance, 1f);
            float trackFat = track != null ? track.fatigueMultiplier : 1f;
            float fatigue = progress * (1f / endurance) * gs.fatigueFactor * trackFat;

            string nameCol = racer.IsCritActive ? "#FF8800" : racer.IsFinished ? "#AAAAAA" : "#FFDD44";
            sb.AppendFormat("<color={0}>── {1}위: {2} ({3}) ──</color>\n",
                nameCol, i + 1, cd.DisplayName, cd.GetTypeName());
            sb.AppendFormat("  SPD:<color=#88CCFF>{0:F2}</color>  POW:{1}  BRV:{2}  CLM:{3}  END:{4}  LCK:{5}\n",
                cd.charBaseSpeed, cd.charBasePower, cd.charBaseBrave,
                cd.charBaseCalm, cd.charBaseEndurance, cd.charBaseLuck);
            sb.AppendFormat("  기본:{0:F2}  구간:{1}(<color=#88FF88>{2:+0.0%;-0.0%}</color>)  피로:<color=#FF8888>-{3:F3}</color>  진행:{4:F1}%\n",
                baseSpd, phaseName, typeBonus, fatigue, progress * 100);

            string sl = "";
            if (racer.IsCritActive) sl += "<color=#FF8800>⚡크리티컬</color>  ";
            if (racer.CollisionPenalty > 0) sl += "<color=#FF6666>💥감속</color>  ";
            if (racer.SlingshotBoost > 0) sl += "<color=#66FF66>🚀슬링샷</color>  ";
            sb.AppendFormat("  최종: <color=#FFFFFF>{0:F2}</color>  |  랩: {1}/{2}  {3}\n\n",
                racer.CurrentSpeed, racer.CurrentLap, rm.CurrentLaps, sl);
        }
        cachedDetailText = sb.ToString();
    }

    // ══════════════════════════════════════
    //  OnGUI
    // ══════════════════════════════════════
    //  인기도 / 배당 / 컨디션 섹션
    // ══════════════════════════════════════

    private bool showOddsSection = true;
    private Vector2 oddsScrollPos;

    /// <summary>charId → DisplayName 변환 (디버그 오버레이용)</summary>
    private string GetCharDisplayName(string charId)
    {
        var db = CharacterDatabase.Instance;
        if (db != null)
        {
            var cd = db.FindById(charId);
            if (cd != null) return cd.DisplayName;
        }
        return charId; // fallback
    }

    private void DrawOddsSection()
    {
        var odds = OddsCalculator.CurrentOdds;

        // 헤더 (클릭으로 접기/펼치기)
        GUILayout.BeginHorizontal();
        string oddsHeader = showOddsSection ? "▼ 🎲 인기도 / 배당 / 컨디션" : "▶ 🎲 인기도 / 배당 / 컨디션";
        if (GUILayout.Button(oddsHeader, normalStyle, GUILayout.ExpandWidth(true)))
            showOddsSection = !showOddsSection;
        GUILayout.EndHorizontal();

        if (!showOddsSection) return;

        if (odds == null || odds.Count == 0)
        {
            GUILayout.Label("  <color=#888888>(배당 데이터 없음 — 게임 시작하면 자동으로 표시됩니다)</color>", normalStyle);
            GUILayout.Label("─────────────────────────────────────", normalStyle);
            return;
        }

        // 헤더 행
        GUILayout.Label(
            "<color=yellow>인기  이름    단승   컨디션         최근순위   출전</color>",
            normalStyle);

        // 1줄 높이 약 16px, 3~4개 = 64px, 최대 6개 = 96px 스크롤 영역
        float rowHeight = 16f;
        float scrollHeight = Mathf.Clamp(odds.Count * rowHeight, rowHeight * 3.5f, rowHeight * 6f);

        oddsScrollPos = GUILayout.BeginScrollView(oddsScrollPos, GUILayout.Height(scrollHeight));

        // 각 캐릭터 행
        foreach (var info in odds)
        {
            // 인기순위 색상
            string rankColor;
            string rankStar;
            if      (info.popularityRank == 1) { rankColor = "#FFD700"; rankStar = "★"; }
            else if (info.popularityRank <= 3)  { rankColor = "#AAAAFF"; rankStar = "☆"; }
            else                                { rankColor = "#888888"; rankStar = " "; }

            // 배당 색상 (낮을수록 초록, 높을수록 빨강)
            string oddsColor;
            if      (info.winOdds < 5f)  oddsColor = "#66FF66";
            else if (info.winOdds < 15f) oddsColor = "#FFFF66";
            else if (info.winOdds < 40f) oddsColor = "#FFAA44";
            else                         oddsColor = "#FF6666";

            // 컨디션 색상 + 이름
            string condColor = ConditionHelper.GetColorHex(info.condition);
            string condName  = ConditionHelper.GetDisplayName(info.condition);
            float  condMul   = info.conditionMul;

            // 신규 표시
            string newTag = info.isNew ? " <color=#88CCFF>[신규]</color>" : "";

            GUILayout.Label(string.Format(
                "<color={0}>{1,2}위{2}</color>  {3,-4}  <color={4}>{5,5:F1}x</color>  " +
                "<color={6}>{7}({8:F2}x)</color>  {9,-10}  {10}판{11}",
                rankColor, info.popularityRank, rankStar,
                GetCharDisplayName(info.charId),
                oddsColor, info.winOdds,
                condColor, condName, condMul,
                info.recentRankStr,
                info.totalRaces, newTag),
                normalStyle);
        }

        GUILayout.EndScrollView();

        // 하단 요약: 평균 배당
        float avgOdds = 0f;
        foreach (var info in odds) avgOdds += info.winOdds;
        avgOdds /= odds.Count;
        GUILayout.Label(string.Format(
            "  <color=#888888>단승 평균배당: {0:F1}x | 출전 {1}마리</color>",
            avgOdds, odds.Count), normalStyle);

        GUILayout.Label("─────────────────────────────────────", normalStyle);
    }

    // ══════════════════════════════════════

    private void InitStyles()
    {
        if (stylesInitialized) return;
        headerStyle = new GUIStyle(GUI.skin.label)
        { fontSize = 14, fontStyle = FontStyle.Bold, richText = true };
        headerStyle.normal.textColor = Color.yellow;

        normalStyle = new GUIStyle(GUI.skin.label)
        { fontSize = 11, richText = true };
        normalStyle.normal.textColor = Color.white;

        critStyle = new GUIStyle(GUI.skin.label)
        { fontSize = 11, fontStyle = FontStyle.Bold, richText = true };
        critStyle.normal.textColor = new Color(1f, 0.5f, 0f);

        copyBtnStyle = new GUIStyle(GUI.skin.button)
        { fontSize = 10, fontStyle = FontStyle.Bold };
        copyBtnStyle.normal.textColor = Color.white;

        stylesInitialized = true;
    }

    private void OnGUI()
    {
        if (!showDebug) return;
        var rm = RaceManager.Instance;
        if (rm == null) return;
        InitStyles();

        float panelWidth = showDetail ? 520 : 440;
        float panelHeight = Screen.height - 20;
        Rect panelRect = new Rect(Screen.width - panelWidth - 10, 10, panelWidth, panelHeight);

        GUI.color = new Color(0, 0, 0, 0.85f);
        GUI.DrawTexture(panelRect, Texture2D.whiteTexture);
        GUI.color = Color.white;

        GUILayout.BeginArea(new Rect(panelRect.x + 8, panelRect.y + 5, panelRect.width - 16, panelRect.height - 10));

        var gs = GameSettings.Instance;
        TrackData track = gs.currentTrack;
        string trackName = track != null ? track.trackIcon + " " + Loc.Get(track.trackName) : "🏟️ 일반";

        GUILayout.Label("🏇 Race Debug [F1:토글 F2:상세 F3:라운드]", headerStyle);

        // 라운드 탭 표시
        string roundLabel = viewingRound == -1
            ? "<color=#66FF66>R" + currentRound + "(LIVE)</color>"
            : "<color=#88CCFF>R" + viewingRound + "(기록)</color>";

        string roundTabs = "";
        for (int r = 1; r <= currentRound; r++)
        {
            bool hasLog = allRoundLogs.ContainsKey(r) && allRoundLogs[r].racingEvents.Count > 0;
            if (viewingRound == r)
                roundTabs += "<color=#FF8800>[R" + r + "]</color> ";
            else if (hasLog)
                roundTabs += "<color=#88CCFF>R" + r + "</color> ";
            else
                roundTabs += "<color=#666666>R" + r + "</color> ";
        }

        GUILayout.Label("트랙: " + trackName + "  |  보기: " + roundLabel + "  |  저장: " + allRoundLogs.Count + "R", normalStyle);
        GUILayout.Label("라운드: " + roundTabs, normalStyle);

        // ★ 복사 버튼 영역
        GUILayout.BeginHorizontal();
        {
            int displayRoundForCopy = viewingRound == -1 ? currentRound : viewingRound;

            if (GUILayout.Button("📋 R" + displayRoundForCopy + " 로그복사", copyBtnStyle, GUILayout.Width(130), GUILayout.Height(22)))
            {
                string text = BuildCopyText(displayRoundForCopy);
                CopyToClipboard(text, "R" + displayRoundForCopy + " 로그 복사됨!");
            }

            if (allRoundLogs.Count > 1)
            {
                if (GUILayout.Button("📋 전체 로그복사", copyBtnStyle, GUILayout.Width(120), GUILayout.Height(22)))
                {
                    string text = BuildCopyTextAll();
                    CopyToClipboard(text, "전체 " + allRoundLogs.Count + "R 로그 복사됨!");
                }
            }

            // 복사 피드백 표시
            if (copyFeedbackTimer > 0f)
            {
                GUILayout.Label("<color=#66FF66>✓ " + copyFeedbackMsg + "</color>", normalStyle);
            }
        }
        GUILayout.EndHorizontal();

        GUILayout.Label("─────────────────────────────────────", normalStyle);

        int displayRound = viewingRound == -1 ? currentRound : viewingRound;
        RoundLog displayLog = allRoundLogs.ContainsKey(displayRound) ? allRoundLogs[displayRound] : null;

        // ── 인기도 / 배당 / 컨디션 섹션 ──
        DrawOddsSection();

        // ── 상단: 레이스 상태 (현재) 또는 리포트 (과거) ──
        float statusHeight = (panelHeight - 160) * 0.25f;

        if (viewingRound == -1)
        {
            scrollPos = GUILayout.BeginScrollView(scrollPos, GUILayout.Height(statusHeight));
            GUILayout.Label(showDetail ? cachedDetailText : cachedSimpleText, normalStyle);
            GUILayout.EndScrollView();
        }
        else
        {
            GUILayout.Label("<color=#88CCFF>── 라운드 " + viewingRound + " 리포트 ──</color>", headerStyle);
            scrollPos = GUILayout.BeginScrollView(scrollPos, GUILayout.Height(statusHeight));
            if (displayLog != null && !string.IsNullOrEmpty(displayLog.reportText))
                GUILayout.Label(displayLog.reportText, normalStyle);
            else
                GUILayout.Label("  (아직 리포트 없음)", normalStyle);
            GUILayout.EndScrollView();
        }

        // ── HP 바퀴별 스냅샷 탭 ──
        DrawHPTab(displayLog, panelHeight);

        // ── 중단: 레이싱 이벤트 ──
        GUILayout.Label("─────────────────────────────────────", normalStyle);
        int racingCount = displayLog != null ? displayLog.racingEvents.Count : 0;
        GUILayout.Label("⚡ 레이싱 이벤트 R" + displayRound + " (" + racingCount + "건)", headerStyle);

        float raceLogHeight = (panelHeight - 160) * 0.2f;
        raceLogScroll = GUILayout.BeginScrollView(raceLogScroll, GUILayout.Height(raceLogHeight));
        if (displayLog != null)
        {
            for (int i = 0; i < displayLog.racingEvents.Count; i++)
            {
                var e = displayLog.racingEvents[i];
                string c = "#FFFFFF";
                switch (e.type)
                {
                    case EventType.Critical: c = "#FF8800"; break;
                    case EventType.CollisionHit: c = "#FF6666"; break;
                    case EventType.CollisionDodge: c = "#88CCFF"; break;
                    case EventType.Slingshot: c = "#66FF66"; break;
                    case EventType.Attack: c = "#FFD700"; break;
                    case EventType.Track: c = "#CC88FF"; break;
                }
                GUILayout.Label(string.Format("<color={0}>[{1:F1}s] {2} {3}</color>",
                    c, e.time, e.GetIcon(), e.description), normalStyle);
            }
        }
        GUILayout.EndScrollView();

        // ── 하단: 완주 기록 ──
        GUILayout.Label("─────────────────────────────────────", normalStyle);
        int finishCount = displayLog != null ? displayLog.finishEvents.Count : 0;
        GUILayout.Label("🏁 완주 기록 R" + displayRound + " (" + finishCount + "건)", headerStyle);

        float finishLogHeight = (panelHeight - 160) * 0.15f;
        finishLogScroll = GUILayout.BeginScrollView(finishLogScroll, GUILayout.Height(finishLogHeight));
        if (displayLog != null)
        {
            for (int i = 0; i < displayLog.finishEvents.Count; i++)
            {
                var e = displayLog.finishEvents[i];
                GUILayout.Label(string.Format("<color=#AAAAAA>[{0:F1}s] {1} {2}</color>",
                    e.time, e.GetIcon(), e.description), normalStyle);
            }
        }
        GUILayout.EndScrollView();

        GUILayout.EndArea();
    }

    // ══════════════════════════════════════
    //  HP 바퀴별 스냅샷 탭
    // ══════════════════════════════════════

    private void DrawHPTab(RoundLog displayLog, float panelHeight)
    {
        GUILayout.Label("─────────────────────────────────────", normalStyle);

        int snapCount = displayLog != null ? displayLog.lapSnapshots.Count : 0;
        string hpHeader = showHPTab
            ? "▼ 💪 HP 바퀴별 (" + snapCount + "바퀴)"
            : "▶ 💪 HP 바퀴별 (" + snapCount + "바퀴)";

        GUILayout.BeginHorizontal();
        if (GUILayout.Button(hpHeader, normalStyle, GUILayout.ExpandWidth(true)))
            showHPTab = !showHPTab;
        GUILayout.EndHorizontal();

        if (!showHPTab) return;

        if (displayLog == null || displayLog.lapSnapshots.Count == 0)
        {
            GUILayout.Label("  <color=#888888>(아직 바퀴 완료 데이터 없음)</color>", normalStyle);
            return;
        }

        float hpTabHeight = (panelHeight - 160) * 0.2f;
        hpTabScroll = GUILayout.BeginScrollView(hpTabScroll, GUILayout.Height(hpTabHeight));

        foreach (var snapshot in displayLog.lapSnapshots)
        {
            GUILayout.Label(string.Format("<color=#FFDD44>── {0}바퀴 완료 ──</color>", snapshot.lap), normalStyle);

            foreach (var info in snapshot.racers)
            {
                // HP 바 색상: >50% 초록, >20% 주황, 그 외 빨강
                string hpColor = info.hpPercent > 50f ? "#66FF66"
                    : info.hpPercent > 20f ? "#FFAA44"
                    : "#FF4444";

                // HP 바 시각화 (10칸)
                int filled = Mathf.RoundToInt(info.hpPercent / 10f);
                string bar = "";
                for (int b = 0; b < 10; b++)
                    bar += b < filled ? "█" : "░";

                GUILayout.Label(string.Format(
                    "  {0}위 {1,-4} ({2})  <color={3}>{4} {5,3:F0}%</color>",
                    info.rank, info.name, info.typeName,
                    hpColor, bar, info.hpPercent), normalStyle);
            }
            GUILayout.Space(4);
        }

        GUILayout.EndScrollView();
    }
}