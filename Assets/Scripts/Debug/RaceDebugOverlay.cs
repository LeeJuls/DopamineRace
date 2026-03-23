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
    private int activeTab = 0;      // 0=상태 1=HP 2=이벤트
    private Vector2 tabContentScroll;
    private static readonly string[] TabNames = { "📊 상태", "💪 HP", "⚡ 이벤트" };
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

    // ── 랩 구간별 스냅샷 추적 ──
    private HashSet<string> recordedCheckpoints = new HashSet<string>(); // "L{lap}_{25|50|100}"

    // ── HP 탭 UI ──
    // (탭 시스템으로 전환 — 별도 스크롤 불필요)

    public enum EventType { Critical, CollisionHit, CollisionDodge, Slingshot, Attack, Finish, Track, Burst, Spurt, Slipstream }

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
                case EventType.Burst:          return "🔥";
                case EventType.Spurt:          return "💨";
                case EventType.Slipstream:     return "🌊";
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

    /// <summary>랩 구간별 전체 캐릭터 스냅샷 (25% / 50% / 100%)</summary>
    public class LapSnapshot
    {
        public int lap;
        public float subProgress;  // 0.25, 0.50, or 1.00
        public string GetLabel()
        {
            return subProgress >= 1f
                ? string.Format("{0}바퀴 완료", lap)
                : string.Format("{0}바퀴 {1}%", lap, (int)(subProgress * 100));
        }
        public List<LapRacerInfo> racers = new List<LapRacerInfo>();
    }

    public struct LapRacerInfo
    {
        public int rank;
        public string name;
        public string typeName;
        public float hpPercent;   // 0~100
        public float cpPercent;   // 0~100
        public float speed;       // 현재 속도
        public float time;        // 통과 시각 (Time.time)
        // 속도 분해 정보
        public float vmax;        // HP 감소 적용 전 Vmax
        public float hpSpeedMul;  // HP 속도 배율 (1.0 = 감소 없음)
        public string phase;      // "노말" / "부스트" / "스퍼트"
        public float target;      // phase 배율 적용 후 target 속도
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
        recordedCheckpoints.Clear();

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

        // ── 완주 이벤트 finalRankings 기준으로 재구성 ──
        // LateUpdate 타이밍 문제(raceActive=false 직후 스킵)로 순서가 틀릴 수 있어
        // 기존 이벤트를 보존하되 finalRankings 순서대로 재정렬 + 미기록 항목 추가
        if (rm != null)
        {
            var originalFinishEvents = new List<RaceEvent>(log.finishEvents);
            log.finishEvents.Clear();

            foreach (var entry in rankings)
            {
                // 기존 로그에서 해당 레이서 이벤트 찾기 (타임스탬프 보존)
                bool found = false;
                foreach (var fe in originalFinishEvents)
                {
                    if (fe.description.Contains(entry.racerName))
                    {
                        log.finishEvents.Add(fe); // 타임스탬프 그대로 유지
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    // 미기록 완주자 새로 생성
                    float hpPct = hpByIndex.ContainsKey(entry.racerIndex) ? hpByIndex[entry.racerIndex] : 0f;
                    var racer = rm.Racers.Find(r => r.RacerIndex == entry.racerIndex);
                    string typeName = racer?.CharData?.GetTypeName() ?? "?";
                    float spd = racer?.CharData?.charBaseSpeed ?? 0f;
                    LogEvent(EventType.Finish, string.Format("{0} {1}착 완주! (SPD:{2:F2} {3}) HP:{4:F0}%",
                        entry.racerName, entry.rank, spd, typeName, hpPct));
                }
            }
        }

        // ── 최종 랩 100% 스냅샷 순위도 finalRankings 기준으로 재정렬 ──
        // RecordRacerCheckpoint는 Time.time 기준 정렬 → 프레임 실행 순서에 따라 FinishOrder와 어긋날 수 있음
        LapSnapshot finalSnapshot = null;
        int maxLapNum = 0;
        foreach (var snap in log.lapSnapshots)
        {
            if (snap.subProgress >= 1f && snap.lap > maxLapNum)
            {
                maxLapNum = snap.lap;
                finalSnapshot = snap;
            }
        }
        if (finalSnapshot != null && finalSnapshot.racers.Count > 0)
        {
            var reordered = new List<LapRacerInfo>();
            foreach (var entry in rankings)
            {
                for (int i = 0; i < finalSnapshot.racers.Count; i++)
                {
                    if (finalSnapshot.racers[i].name == entry.racerName)
                    {
                        var r = finalSnapshot.racers[i];
                        r.rank = entry.rank;  // 순위도 finalRankings 기준으로 수정
                        reordered.Add(r);
                        break;
                    }
                }
            }
            // rankings에 없는 레이서(미완주 등)도 보존
            foreach (var r in finalSnapshot.racers)
            {
                bool alreadyAdded = false;
                foreach (var re in reordered)
                    if (re.name == r.name) { alreadyAdded = true; break; }
                if (!alreadyAdded) reordered.Add(r);
            }
            finalSnapshot.racers.Clear();
            finalSnapshot.racers.AddRange(reordered);
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

        // HP 랩별 스냅샷 (25%/50%/100%)
        if (log.lapSnapshots.Count > 0)
        {
            sb.AppendFormat("\n▶ 랩별 순위/HP ({0}개 구간)\n", log.lapSnapshots.Count);
            sb.AppendLine("────────────────────────");
            foreach (var snapshot in log.lapSnapshots)
            {
                sb.AppendFormat("── {0} ──\n", snapshot.GetLabel());
                foreach (var info in snapshot.racers)
                {
                    sb.AppendFormat("  {0}위 {1} ({2})  HP:{3:F0}%  SPD:{4:F2}  [{5:F3}s]\n",
                        info.rank, info.name, info.typeName, info.hpPercent, info.speed, info.time);
                    if (info.vmax > 0f)
                        sb.AppendFormat("     └ Vmax:{0:F2}  hp:x{1:F2}{2}{3}  [{4}]\n",
                            info.vmax, info.hpSpeedMul,
                            info.hpSpeedMul < 0.999f ? string.Format("(-{0:P0})", 1f - info.hpSpeedMul) : "",
                            info.target > 0f ? string.Format("  →T:{0:F2}", info.target) : "",
                            info.phase);
                }
            }
        }

        // 리포트 (있으면)
        if (!string.IsNullOrEmpty(log.reportText))
        {
            sb.AppendLine("\n▶ 라운드 리포트");
            sb.AppendLine("────────────────────────");
            string plain = log.reportText;
            plain = System.Text.RegularExpressions.Regex.Replace(plain, "<[^>]+>", "");
            sb.AppendLine(plain);
        }

        // V4 설정 + 캐릭터 스탯
        sb.AppendLine(BuildV4SettingsText());

        return sb.ToString();
    }

    private string BuildV4SettingsText()
    {
        var gs = GameSettings.Instance;
        var v4 = gs?.v4Settings;
        if (v4 == null) return "";

        var sb = new StringBuilder();
        sb.AppendLine("\n▶ V4 설정");
        sb.AppendLine("────────────────────────");
        sb.AppendFormat("GlobalSpeed×{0:F2}\n", gs.globalSpeedMultiplier);
        sb.AppendFormat("BURST: {0}  Condition: {1}\n",
            v4.v4_disableBurst ? "OFF" : "ON",
            v4.v4_applyCondition ? "ON" : "OFF");
        sb.AppendFormat("Normal×{0:F2}  Burst×{1:F2}  Spurt×{2:F2}  SpurtStart:{3:P0}\n",
            v4.v4_normalSpeedRatio, v4.v4_burstSpeedRatio, v4.v4_spurtVmaxBonus, v4.v4_finalSpurtStart);
        sb.AppendFormat("Drain/Prog:{0:F1}  BurstDrain×{1:F1}  SpurtDrain×{2:F1}\n",
            v4.v4_drainPerLap, v4.v4_burstDrainMul, v4.v4_spurtDrainMul);
        sb.AppendFormat("SpurtHP보너스: 속도+{0:P0}/HP  가속+{1:P0}/HP\n",
            v4.v4_spurtHpSpeedBonus, v4.v4_spurtHpAccelBonus);
        sb.AppendFormat("긴급부스트: {0}  Spd×{1:F2}  Drain×{2:F1}  도주지속:{3}  쿨다운:{4:F1}s\n",
            v4.v4_emergencyBurstEnabled ? "ON" : "OFF",
            v4.v4_emergencyBurstSpeedRatio, v4.v4_emergencyBurstDrainMul,
            v4.v4_runnerPersistentBurst ? "ON" : "OFF",
            v4.v4_emergencyBurstCooldown);
        // 타입별 긴급부스트 drain 오버라이드가 있는 경우만 출력
        bool hasTypeOverride = v4.v4_runnerEmergencyDrainMul > 0f || v4.v4_leaderEmergencyDrainMul > 0f
                            || v4.v4_chaserEmergencyDrainMul > 0f || v4.v4_reckonerEmergencyDrainMul > 0f;
        if (hasTypeOverride)
        {
            sb.AppendFormat("  긴급Drain 타입별: 도주×{0:F1}  선행×{1:F1}  선입×{2:F1}  추입×{3:F1}\n",
                v4.v4_runnerEmergencyDrainMul   > 0f ? v4.v4_runnerEmergencyDrainMul   : v4.v4_emergencyBurstDrainMul,
                v4.v4_leaderEmergencyDrainMul   > 0f ? v4.v4_leaderEmergencyDrainMul   : v4.v4_emergencyBurstDrainMul,
                v4.v4_chaserEmergencyDrainMul   > 0f ? v4.v4_chaserEmergencyDrainMul   : v4.v4_emergencyBurstDrainMul,
                v4.v4_reckonerEmergencyDrainMul > 0f ? v4.v4_reckonerEmergencyDrainMul : v4.v4_emergencyBurstDrainMul);
        }
        if (v4.v4_hpSpeedThresholds != null && v4.v4_hpSpeedThresholds.Count > 0)
        {
            var hpSb = new StringBuilder("HP감속:");
            foreach (var t in v4.v4_hpSpeedThresholds)
                hpSb.AppendFormat("  HP≤{0:P0}→-{1:P0}(×{2:F2})", t.hpRatio, t.speedReduction, 1f - t.speedReduction);
            sb.AppendLine(hpSb.ToString());
        }
        else
        {
            sb.AppendLine("HP감속: (없음)");
        }
        sb.AppendFormat("Runner:{0:P0}~{1:P0}  Leader:{2:P0}~{3:P0}  Chaser:{4:P0}~{5:P0}  Reckoner:{6:P0}~{7:P0}\n",
            v4.v4_runnerBurstStart, v4.v4_runnerBurstEnd,
            v4.v4_leaderBurstStart, v4.v4_leaderBurstEnd,
            v4.v4_chaserBurstStart, v4.v4_chaserBurstEnd,
            v4.v4_reckonerBurstStart, v4.v4_reckonerBurstEnd);
        // 타입별 긴급부스트 상세 (구간 라인 바로 아래)
        sb.AppendFormat("  긴급Spd×{0:F2}  쿨다운:{1:F1}s  도주지속:{2}\n",
            v4.v4_emergencyBurstSpeedRatio,
            v4.v4_emergencyBurstCooldown,
            v4.v4_runnerPersistentBurst ? "ON" : "OFF");
        sb.AppendFormat("  긴급Drain: 전역×{0:F1}  도주×{1:F1}  선행×{2:F1}  선입×{3:F1}  추입×{4:F1}\n",
            v4.v4_emergencyBurstDrainMul,
            v4.GetV4EmergencyBurstDrainMul(CharacterType.Runner),
            v4.GetV4EmergencyBurstDrainMul(CharacterType.Leader),
            v4.GetV4EmergencyBurstDrainMul(CharacterType.Chaser),
            v4.GetV4EmergencyBurstDrainMul(CharacterType.Reckoner));
        sb.AppendFormat("Luck크리티컬: 확률{0:F3}/luck  ×{1:F2}속도  {2:F1}s지속  판정주기{3:F1}s\n",
            v4.v4_luckCritChance, v4.v4_luckCritBoost, v4.v4_luckCritDuration, v4.v4_luckCheckInterval);
        sb.AppendFormat("Int회피: 확률{0:F3}/int\n",
            v4.v4_intDodgeChance);

        // 캐릭터 스탯
        bool applyCond = v4.v4_applyCondition;
        var rm = RaceManager.Instance;
        if (rm != null && rm.Racers != null && rm.Racers.Count > 0)
        {
            sb.AppendLine(applyCond ? "\n▶ 캐릭터 스탯 (V4) [컨디션 적용중]" : "\n▶ 캐릭터 스탯 (V4)");
            sb.AppendLine(applyCond ? "이름    SPD  ACC  STA  POW  INT  LCK  합계  COND" : "이름    SPD  ACC  STA  POW  INT  LCK  합계");
            var rankings = rm.GetLiveRankings();
            foreach (var racer in rankings)
            {
                if (racer.CharData == null) continue;
                var v4c = CharacterDatabaseV4.FindById(racer.CharData.charId);
                if (v4c == null) continue;
                float condMul = applyCond
                    ? Mathf.Max(OddsCalculator.GetConditionMultiplier(racer.CharData.charId), 0.3f)
                    : 1f;
                string spdDelta = applyCond ? FormatCondDeltaPlain(v4c.v4Speed,   condMul) : "";
                string staDelta = applyCond ? FormatCondDeltaPlain(v4c.v4Stamina, condMul) : "";
                if (applyCond)
                {
                    sb.AppendFormat("{0,-4}  {1,3:F0}{2}  {3,3:F0}  {4,3:F0}{5}  {6,3:F0}  {7,3:F0}  {8,3:F0}  {9,3:F0}  ×{10:F2}\n",
                        racer.CharData.DisplayName,
                        v4c.v4Speed, spdDelta, v4c.v4Accel, v4c.v4Stamina, staDelta,
                        v4c.v4Power, v4c.v4Intelligence, v4c.v4Luck,
                        v4c.StatTotal, condMul);
                }
                else
                {
                    sb.AppendFormat("{0,-4}  {1,3:F0}{2}  {3,3:F0}  {4,3:F0}{5}  {6,3:F0}  {7,3:F0}  {8,3:F0}  {9,3:F0}\n",
                        racer.CharData.DisplayName,
                        v4c.v4Speed, spdDelta, v4c.v4Accel, v4c.v4Stamina, staDelta,
                        v4c.v4Power, v4c.v4Intelligence, v4c.v4Luck,
                        v4c.StatTotal);
                }
            }
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
#if UNITY_EDITOR
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
#endif

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

        foreach (var racer in rm.Racers)
        {
            if (racer.CharData == null) continue;
            int idx = racer.RacerIndex;

            bool wasCrit = prevCritState.ContainsKey(idx) && prevCritState[idx];
            bool isCrit = racer.IsCritActive;

            if (isCrit && !wasCrit)
            {
                var gs4 = GameSettings.Instance?.v4Settings;
                float critBoost = gs4 != null ? gs4.v4_luckCritBoost : 1.3f;
                float critDur = racer.V4CritInitialDuration;
                LogEvent(EventType.Critical,
                    string.Format("{0} (luck:{1}) 크리티컬! ×{2:F2} {3:F1}s",
                        racer.CharData.DisplayName, racer.CharData.charBaseLuck,
                        critBoost, critDur));
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

        // 이벤트 탭 활성화 시 자동 스크롤 (최신 이벤트 하단 표시)
        if (viewingRound == -1 && activeTab == 2)
        {
            tabContentScroll.y = float.MaxValue;
        }
    }

    /// <summary>
    /// 각 캐릭터가 자기 자신의 진행도 25%/50%/100% 구간을 통과하는 순간 호출.
    /// RacerController_V4.UpdateV4()에서 직접 호출됨.
    /// → 선두 기준 스냅샷이 아닌 "해당 캐릭터가 실제로 그 거리를 달렸을 때"의 HP를 기록.
    /// </summary>
    public void RecordRacerCheckpoint(RacerController racer, int lap, float subProgress, float hpPct, float speed,
                                      float vmax = 0f, float hpSpeedMul = 1f, string phase = "노말", float target = 0f)
    {
        if (racer?.CharData == null) return;
        var log = GetOrCreateLog(currentRound);

        // 해당 구간 스냅샷이 없으면 생성
        LapSnapshot snapshot = null;
        foreach (var s in log.lapSnapshots)
        {
            if (s.lap == lap && Mathf.Approximately(s.subProgress, subProgress))
            { snapshot = s; break; }
        }
        if (snapshot == null)
        {
            snapshot = new LapSnapshot { lap = lap, subProgress = subProgress };
            // 진행도 오름차순 삽입
            float myKey = (lap - 1) + subProgress;
            int insertAt = log.lapSnapshots.Count;
            for (int i = 0; i < log.lapSnapshots.Count; i++)
            {
                float k = (log.lapSnapshots[i].lap - 1) + log.lapSnapshots[i].subProgress;
                if (myKey < k) { insertAt = i; break; }
            }
            log.lapSnapshots.Insert(insertAt, snapshot);
        }

        // 이미 이 캐릭터 데이터가 있으면 중복 방지
        foreach (var r in snapshot.racers)
            if (r.name == racer.CharData.DisplayName) return;

        snapshot.racers.Add(new LapRacerInfo
        {
            rank      = 0,   // 나중에 time 기준으로 재계산
            name      = racer.CharData.DisplayName,
            typeName  = racer.CharData.GetTypeName(),
            hpPercent = hpPct,
            cpPercent = 0f,   // V4는 CP 없음
            time      = Time.time,
            speed     = speed,
            vmax      = vmax,
            hpSpeedMul = hpSpeedMul,
            phase     = phase,
            target    = target
        });

        // time 오름차순 정렬 후 rank 재할당 (struct이므로 copy-modify-replace)
        snapshot.racers.Sort((a, b) => a.time.CompareTo(b.time));
        for (int i = 0; i < snapshot.racers.Count; i++)
        {
            var r = snapshot.racers[i];
            r.rank = i + 1;
            snapshot.racers[i] = r;
        }
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
            else if (racer.V4InSlipstream) { status = racer.V4SlipstreamAccelActive ? "🌊✓가속" : "🌊✗절약"; col = "#88CCFF"; }

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
            if (racer.V4InSlipstream) sl += racer.V4SlipstreamAccelActive
                ? "<color=#88CCFF>🌊슬립스트림✓</color>  "
                : "<color=#6699AA>🌊슬립스트림✗</color>  ";
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

        // 각 캐릭터 행 (탭 외부 스크롤뷰로 처리)
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
#if !UNITY_EDITOR
        return; // 릴리즈 빌드에서는 오버레이 완전 비활성
#endif
        if (!showDebug) return;
        var rm = RaceManager.Instance;
        if (rm == null) return;
        InitStyles();

        float panelWidth = showDetail ? 600 : 460;
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

        // 라운드 표시
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

        // ★ 복사 버튼 (항상 표시 — 탭 전환해도 전체 로그 복사 가능)
        GUILayout.BeginHorizontal();
        {
            int displayRoundForCopy = viewingRound == -1 ? currentRound : viewingRound;

            if (GUILayout.Button("📋 R" + displayRoundForCopy + " 로그복사", copyBtnStyle, GUILayout.Width(130), GUILayout.Height(22)))
                CopyToClipboard(BuildCopyText(displayRoundForCopy), "R" + displayRoundForCopy + " 로그 복사됨!");

            if (allRoundLogs.Count > 1)
            {
                if (GUILayout.Button("📋 전체 로그복사", copyBtnStyle, GUILayout.Width(120), GUILayout.Height(22)))
                    CopyToClipboard(BuildCopyTextAll(), "전체 " + allRoundLogs.Count + "R 로그 복사됨!");
            }

            if (copyFeedbackTimer > 0f)
                GUILayout.Label("<color=#66FF66>✓ " + copyFeedbackMsg + "</color>", normalStyle);
        }
        GUILayout.EndHorizontal();

        // ── 탭 선택 버튼 ──
        GUILayout.Label("─────────────────────────────────────", normalStyle);
        GUILayout.BeginHorizontal();
        for (int i = 0; i < TabNames.Length; i++)
        {
            GUI.backgroundColor = (activeTab == i)
                ? new Color(0.25f, 0.45f, 0.75f)
                : new Color(0.18f, 0.18f, 0.18f);
            if (GUILayout.Button(TabNames[i], GUILayout.Height(24)))
            {
                activeTab = i;
                tabContentScroll = Vector2.zero;
            }
        }
        GUI.backgroundColor = Color.white;
        GUILayout.EndHorizontal();
        GUILayout.Label("─────────────────────────────────────", normalStyle);

        // ── 탭 콘텐츠 (단일 스크롤뷰 — 고정헤더 약 142px 제외) ──
        int displayRound = viewingRound == -1 ? currentRound : viewingRound;
        RoundLog displayLog = allRoundLogs.ContainsKey(displayRound) ? allRoundLogs[displayRound] : null;

        tabContentScroll = GUILayout.BeginScrollView(tabContentScroll, GUILayout.Height(panelHeight - 142f));

        switch (activeTab)
        {
            case 0: DrawStatusTab(displayLog); break;
            case 1: DrawHPTabContent(displayLog); break;
            case 2: DrawEventsTab(displayLog, displayRound); break;
        }

        GUILayout.EndScrollView();
        GUILayout.EndArea();
    }

    // ── 탭 0: 상태 (인기도/배당 + 실시간 순위) ──
    private void DrawStatusTab(RoundLog displayLog)
    {
        DrawOddsSection();

        GUILayout.Label("─────────────────────────────────────", normalStyle);
        if (viewingRound == -1)
        {
            GUILayout.Label(showDetail ? cachedDetailText : cachedSimpleText, normalStyle);
        }
        else
        {
            GUILayout.Label("<color=#88CCFF>── 라운드 " + viewingRound + " 리포트 ──</color>", headerStyle);
            if (displayLog != null && !string.IsNullOrEmpty(displayLog.reportText))
                GUILayout.Label(displayLog.reportText, normalStyle);
            else
                GUILayout.Label("  (아직 리포트 없음)", normalStyle);
        }

        // ── V4 구간별 속도 세팅 ──
        DrawV4SettingsSection();

        // ── 캐릭터 V4 스탯 테이블 ──
        DrawCharacterStatsSection();
    }

    private bool showStatsSection = true;

    /// <summary>GUI용: 스탯 + 컨디션 델타 (리치텍스트 색상 포함)</summary>
    private string FormatStatWithCond(float stat, float condMul, bool applyCondition)
    {
        if (!applyCondition) return stat.ToString("F0");
        int delta = Mathf.RoundToInt(stat * (condMul - 1f));
        if (delta == 0) return stat.ToString("F0");
        string sign = delta > 0 ? "+" : "";
        string col  = delta > 0 ? "#66FF66" : "#FF6666";
        return string.Format("{0}<color={1}>({2}{3})</color>", (int)stat, col, sign, delta);
    }

    /// <summary>복사 텍스트용: 스탯 + 컨디션 델타 (plain text)</summary>
    private string FormatCondDeltaPlain(float stat, float condMul)
    {
        int delta = Mathf.RoundToInt(stat * (condMul - 1f));
        if (delta == 0) return "";
        return string.Format("({0}{1})", delta > 0 ? "+" : "", delta);
    }

    private void DrawCharacterStatsSection()
    {
        GUILayout.Label("─────────────────────────────────────", normalStyle);

        GUILayout.BeginHorizontal();
        string statsHeader = showStatsSection ? "▼ 📋 캐릭터 스탯 (V4)" : "▶ 📋 캐릭터 스탯 (V4)";
        if (GUILayout.Button(statsHeader, normalStyle, GUILayout.ExpandWidth(true)))
            showStatsSection = !showStatsSection;
        GUILayout.EndHorizontal();

        if (!showStatsSection) return;

        var rm = RaceManager.Instance;
        if (rm == null || rm.Racers == null || rm.Racers.Count == 0) return;

        bool applyCond = GameSettings.Instance?.v4Settings?.v4_applyCondition ?? false;
        string condLabel = applyCond ? " <color=#FFCC44>(컨디션 적용중)</color>" : "";
        GUILayout.Label(
            "<color=yellow>이름    SPD  ACC  STA  POW  INT  LCK  합계</color>" + condLabel,
            normalStyle);

        var rankings = rm.GetLiveRankings();
        foreach (var racer in rankings)
        {
            if (racer.CharData == null) continue;
            var v4 = CharacterDatabaseV4.FindById(racer.CharData.charId);
            if (v4 == null) continue;

            float condMul = applyCond
                ? Mathf.Max(OddsCalculator.GetConditionMultiplier(racer.CharData.charId), 0.3f)
                : 1f;

            string spdStr = FormatStatWithCond(v4.v4Speed,   condMul, applyCond);
            string staStr = FormatStatWithCond(v4.v4Stamina, condMul, applyCond);

            GUILayout.Label(string.Format(
                "{0,-4}  {1}  {2,3:F0}  {3}  {4,3:F0}  {5,3:F0}  {6,3:F0}  <color=#888888>{7,3:F0}</color>",
                racer.CharData.DisplayName,
                spdStr, v4.v4Accel, staStr,
                v4.v4Power, v4.v4Intelligence, v4.v4Luck,
                v4.StatTotal), normalStyle);
        }
    }

    private bool showV4SettingsSection = true;

    private void DrawV4SettingsSection()
    {
        var gs = GameSettings.Instance;
        var v4 = gs?.v4Settings;
        if (v4 == null) return;

        GUILayout.Label("─────────────────────────────────────", normalStyle);
        GUILayout.BeginHorizontal();
        string header = showV4SettingsSection ? "▼ ⚙️ V4 구간별 속도 세팅" : "▶ ⚙️ V4 구간별 속도 세팅";
        if (GUILayout.Button(header, normalStyle, GUILayout.ExpandWidth(true)))
            showV4SettingsSection = !showV4SettingsSection;
        GUILayout.EndHorizontal();

        if (!showV4SettingsSection) return;

        // 부스트 ON/OFF 상태
        string burstState = v4.v4_disableBurst
            ? "<color=#FF4444>● BURST OFF (순수 Normal)</color>"
            : "<color=#66FF66>● BURST ON</color>";
        GUILayout.Label("  " + burstState, normalStyle);

        // 속도 배율
        GUILayout.Label(
            string.Format("  Normal <color=#88CCFF>×{0:F2}</color>  Burst <color=#FF8800>×{1:F2}</color>  Spurt <color=#FF4444>×{2:F2}</color>  (SpurtStart:{3:P0})",
                v4.v4_normalSpeedRatio, v4.v4_burstSpeedRatio, v4.v4_spurtVmaxBonus, v4.v4_finalSpurtStart),
            normalStyle);

        // HP 소모 배율
        GUILayout.Label(
            string.Format("  Drain/Prog <color=#88FF88>{0:F1}</color>  BurstMul <color=#FFAA44>×{1:F1}</color>  SpurtMul <color=#FF6666>×{2:F1}</color>",
                v4.v4_drainPerLap, v4.v4_burstDrainMul, v4.v4_spurtDrainMul),
            normalStyle);

        // 긴급부스트 설정
        GUILayout.Label(
            string.Format("  긴급부스트 {0}  Spd <color=#AADDFF>×{1:F2}</color>  Drain <color=#FFCC88>×{2:F1}</color>",
                v4.v4_emergencyBurstEnabled ? "<color=#88FF88>ON</color>" : "<color=#888888>OFF</color>",
                v4.v4_emergencyBurstSpeedRatio, v4.v4_emergencyBurstDrainMul),
            normalStyle);

        // HP 임계값 기반 속도 감소 (밴드 시스템)
        if (v4.v4_hpSpeedThresholds != null && v4.v4_hpSpeedThresholds.Count > 0)
        {
            GUILayout.Label("<color=yellow>  HP 감속 임계값 (밴드 시스템)</color>", normalStyle);
            foreach (var t in v4.v4_hpSpeedThresholds)
            {
                GUILayout.Label(
                    string.Format("    HP≤<color=#FFAAAA>{0:P0}</color>  →  -<color=#FF6666>{1:P0}</color>  (배율 <color=#FFCC44>×{2:F2}</color>)",
                        t.hpRatio, t.speedReduction, 1f - t.speedReduction),
                    normalStyle);
            }
        }
        else
        {
            GUILayout.Label("  <color=#888888>HP 감속 임계값: (없음)</color>", normalStyle);
        }

        // 타입별 부스트 구간
        GUILayout.Label("<color=yellow>  타입별 부스트 구간 (전체진행도 기준)</color>", normalStyle);
        GUILayout.Label(
            string.Format("  Runner  <color=#88CCFF>{0:P0}~{1:P0}</color>  |  Leader <color=#AAFFAA>{2:P0}~{3:P0}</color>",
                v4.v4_runnerBurstStart, v4.v4_runnerBurstEnd,
                v4.v4_leaderBurstStart, v4.v4_leaderBurstEnd),
            normalStyle);
        GUILayout.Label(
            string.Format("  Chaser  <color=#FFAAFF>{0:P0}~{1:P0}</color>  |  Reckoner <color=#FFCC66>{2:P0}~{3:P0}</color>",
                v4.v4_chaserBurstStart, v4.v4_chaserBurstEnd,
                v4.v4_reckonerBurstStart, v4.v4_reckonerBurstEnd),
            normalStyle);
    }

    // ── 탭 1: HP 랩별 스냅샷 ──
    private void DrawHPTabContent(RoundLog displayLog)
    {
        if (displayLog == null || displayLog.lapSnapshots.Count == 0)
        {
            GUILayout.Label("  <color=#888888>(아직 데이터 없음 — 선두가 25% 구간 도달 시 표시)</color>", normalStyle);
            return;
        }

        foreach (var snapshot in displayLog.lapSnapshots)
        {
            // 100% 완료는 노란색, 25%/50%는 하늘색
            string labelColor = snapshot.subProgress >= 1f ? "#FFDD44" : "#88CCFF";
            GUILayout.Label(string.Format("<color={0}>── {1} ──</color>", labelColor, snapshot.GetLabel()), normalStyle);

            foreach (var info in snapshot.racers)
            {
                string hpColor = info.hpPercent > 50f ? "#66FF66"
                    : info.hpPercent > 20f ? "#FFAA44"
                    : "#FF4444";
                int filled = Mathf.RoundToInt(info.hpPercent / 10f);
                string bar = "";
                for (int b = 0; b < 10; b++)
                    bar += b < filled ? "█" : "░";

                GUILayout.Label(string.Format(
                    "  {0}위 {1,-4} ({2})  <color={3}>{4} {5,3:F0}%</color>  SPD:{6:F2}  <color=#888888>[{7:F3}s]</color>",
                    info.rank, info.name, info.typeName,
                    hpColor, bar, info.hpPercent, info.speed, info.time), normalStyle);

                // 속도 분해 정보 (두 번째 줄)
                if (info.vmax > 0f)
                {
                    string hpMulColor = info.hpSpeedMul < 0.999f ? "#FFAA44" : "#888888";
                    string hpMulStr   = info.hpSpeedMul < 0.999f
                        ? string.Format("×{0:F2}<color=#FF8844>(-{1:P0})</color>", info.hpSpeedMul, 1f - info.hpSpeedMul)
                        : "×1.00";
                    string phaseColor = info.phase == "스퍼트" ? "#FF88FF"
                                      : info.phase == "부스트" ? "#88FFFF"
                                      : "#888888";
                    string targetStr = info.target > 0f
                        ? string.Format("  →T:<color=#AAFFAA>{0:F2}</color>", info.target)
                        : "";
                    GUILayout.Label(string.Format(
                        "  <color=#555555>└</color> Vmax:<color=#FFFFFF>{0:F2}</color>  hp:<color={1}>{2}</color>{3}  <color={4}>[{5}]</color>",
                        info.vmax, hpMulColor, hpMulStr, targetStr, phaseColor, info.phase), normalStyle);
                }
            }
            GUILayout.Space(2);
        }
    }

    // ── 탭 2: 레이싱 이벤트 + 완주 기록 ──
    private void DrawEventsTab(RoundLog displayLog, int displayRound)
    {
        int racingCount = displayLog != null ? displayLog.racingEvents.Count : 0;
        GUILayout.Label("⚡ 레이싱 이벤트 R" + displayRound + " (" + racingCount + "건)", headerStyle);

        if (displayLog != null)
        {
            for (int i = 0; i < displayLog.racingEvents.Count; i++)
            {
                var e = displayLog.racingEvents[i];
                string c = "#FFFFFF";
                switch (e.type)
                {
                    case EventType.Critical:       c = "#FF8800"; break;
                    case EventType.CollisionHit:   c = "#FF6666"; break;
                    case EventType.CollisionDodge: c = "#88CCFF"; break;
                    case EventType.Slingshot:      c = "#66FF66"; break;
                    case EventType.Attack:         c = "#FFD700"; break;
                    case EventType.Track:          c = "#CC88FF"; break;
                }
                GUILayout.Label(string.Format("<color={0}>[{1:F1}s] {2} {3}</color>",
                    c, e.time, e.GetIcon(), e.description), normalStyle);
            }
        }

        GUILayout.Label("─────────────────────────────────────", normalStyle);
        int finishCount = displayLog != null ? displayLog.finishEvents.Count : 0;
        GUILayout.Label("🏁 완주 기록 R" + displayRound + " (" + finishCount + "건)", headerStyle);

        if (displayLog != null)
        {
            for (int i = 0; i < displayLog.finishEvents.Count; i++)
            {
                var e = displayLog.finishEvents[i];
                GUILayout.Label(string.Format("<color=#AAAAAA>[{0:F1}s] {1} {2}</color>",
                    e.time, e.GetIcon(), e.description), normalStyle);
            }
        }

        // 과거 라운드 보기 시 리포트도 표시
        if (viewingRound != -1 && displayLog != null && !string.IsNullOrEmpty(displayLog.reportText))
        {
            GUILayout.Label("─────────────────────────────────────", normalStyle);
            GUILayout.Label("<color=#88CCFF>── 라운드 리포트 ──</color>", headerStyle);
            GUILayout.Label(displayLog.reportText, normalStyle);
        }
    }

}