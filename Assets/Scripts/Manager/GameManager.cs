using UnityEngine;
using System;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    public enum GameState { Betting, Countdown, Racing, Result, Finish }
    public GameState CurrentState { get; private set; } = GameState.Betting;

    public event Action<GameState> OnStateChanged;
    public event Action<int> OnCountdownTick;
    public event Action OnRaceStart;
    public event Action<int> OnRoundChanged;          // ★ 라운드 변경 알림
    public event Action<TrackInfo> OnTrackChanged;     // ★ 트랙 변경 알림

    private float countdownTimer;

    // ═══ 기존 호환 (쌍승 전용) ═══
    public int BetFirst { get; private set; } = -1;
    public int BetSecond { get; private set; } = -1;

    // ═══ 라운드 시스템 ═══
    public int CurrentRound { get; private set; } = 1;      // 1-based
    public int TotalRounds => GameSettings.Instance.TotalRounds;
    public int CurrentRoundLaps => GameSettings.Instance.GetLapsForRound(CurrentRound);
    public bool IsLastRound => CurrentRound >= TotalRounds;

    // ═══ 배팅 시스템 ═══
    public BetInfo CurrentBet { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        StartNewGame();
    }

    private void Update()
    {
        if (CurrentState == GameState.Countdown)
        {
            countdownTimer -= Time.deltaTime;
            OnCountdownTick?.Invoke(Mathf.CeilToInt(countdownTimer));
            if (countdownTimer <= 0f) ChangeState(GameState.Racing);
        }
    }

    // ═══ 게임 초기화 (새 게임 시작) ═══
    public void StartNewGame()
    {
        CurrentRound = 1;
        CurrentBet = new BetInfo(BetType.Exacta);   // 기본 = 쌍승
        ScoreManager.Instance?.ResetAll();

        // ★ 트랙 히스토리 리셋
        if (TrackDatabase.Instance != null)
        {
            TrackDatabase.Instance.ResetTrackHistory();
        }

        // ★ 캐릭터 선발 (CSV 풀에서 랜덤)
        if (CharacterDatabase.Instance != null)
        {
            CharacterDatabase.Instance.SelectRandom(GameSettings.Instance.racerCount);
        }

        // ★ 선발된 캐릭터로 레이서 재스폰
        if (RaceManager.Instance != null)
        {
            RaceManager.Instance.RespawnRacers();
        }

        // ★ Round 1 = 기본 트랙 적용
        ApplyTrackForCurrentRound();

        ApplyRoundLaps();
        Debug.Log("═══ 새 게임 시작 | 총 " + TotalRounds + " 라운드 | "
            + GameConstants.RACER_COUNT + "명 선발 ═══");

        // ★ 배팅 화면 진입 전 인기도/배당/컨디션 계산
        var racersForOdds = CharacterDatabase.Instance?.SelectedCharacters;
        string trackIdForOdds = TrackDatabase.Instance?.CurrentTrackInfo?.trackId ?? "normal";
        OddsCalculator.Calculate(racersForOdds, trackIdForOdds);

        ChangeState(GameState.Betting);
    }

    // ═══ 배팅 타입 선택 ═══
    public void SelectBetType(BetType type)
    {
        if (CurrentState != GameState.Betting) return;
        CurrentBet = new BetInfo(type);
        BetFirst = -1;
        BetSecond = -1;
        Debug.Log("[배팅] 타입 변경: " + BettingCalculator.GetTypeName(type)
            + " (" + BettingCalculator.GetTypeDesc(type) + ") → "
            + BettingCalculator.GetPayout(type) + "점");
    }

    // ═══ 선택 추가 ═══
    public void AddSelection(int racerIndex)
    {
        if (CurrentState != GameState.Betting) return;
        if (CurrentBet == null) return;
        if (CurrentBet.IsComplete) return;
        if (CurrentBet.selections.Contains(racerIndex)) return;

        CurrentBet.selections.Add(racerIndex);
        Debug.Log("[배팅] 선택 추가: " + GameConstants.RACER_NAMES[racerIndex]
            + " (" + CurrentBet.selections.Count + "/" + CurrentBet.RequiredSelections + ")");

        // 기존 호환: BetFirst / BetSecond 동기화
        SyncLegacyBets();
    }

    // ═══ 선택 제거 ═══
    public void RemoveSelection(int racerIndex)
    {
        if (CurrentState != GameState.Betting) return;
        if (CurrentBet == null) return;

        int idx = CurrentBet.selections.IndexOf(racerIndex);
        if (idx >= 0)
        {
            CurrentBet.selections.RemoveAt(idx);
            Debug.Log("[배팅] 선택 제거: " + GameConstants.RACER_NAMES[racerIndex]
                + " (" + CurrentBet.selections.Count + "/" + CurrentBet.RequiredSelections + ")");
            SyncLegacyBets();
        }
    }

    // 기존 BetFirst/BetSecond와 동기화
    private void SyncLegacyBets()
    {
        BetFirst = CurrentBet.selections.Count > 0 ? CurrentBet.selections[0] : -1;
        BetSecond = CurrentBet.selections.Count > 1 ? CurrentBet.selections[1] : -1;
    }

    // ═══ 상태 변경 ═══
    public void ChangeState(GameState s)
    {
        CurrentState = s;
        if (s == GameState.Betting)
        {
            // 배팅 타입은 유지, 선택만 리셋
            if (CurrentBet != null)
                CurrentBet.selections.Clear();
            BetFirst = -1;
            BetSecond = -1;
        }
        if (s == GameState.Countdown) countdownTimer = 3f;
        if (s == GameState.Racing) OnRaceStart?.Invoke();
        if (s == GameState.Result) CalcScore();
        if (s == GameState.Finish) ScoreManager.Instance?.SaveToLeaderboard();
        OnStateChanged?.Invoke(s);
    }

    // ═══ 기존 호환: PlaceBet ═══
    public void PlaceBet(int first, int second)
    {
        BetFirst = first;
        BetSecond = second;
        // BetInfo에도 반영
        if (CurrentBet != null)
        {
            CurrentBet.selections.Clear();
            CurrentBet.selections.Add(first);
            if (CurrentBet.RequiredSelections > 1)
                CurrentBet.selections.Add(second);
        }
    }

    // ═══ 레이스 시작 ═══
    public void StartRace()
    {
        if (CurrentBet == null || !CurrentBet.IsComplete) return;

        string trackName = TrackDatabase.Instance?.CurrentTrackInfo != null
            ? TrackDatabase.Instance.CurrentTrackInfo.trackIcon + " " + TrackDatabase.Instance.CurrentTrackInfo.DisplayName
            : "일반";

        // ★ 컨디션 뽑기 + 배당 계산 (레이스 시작 전)
        var racers = CharacterDatabase.Instance?.SelectedCharacters;
        string trackId = TrackDatabase.Instance?.CurrentTrackInfo?.trackId ?? "normal";
        OddsCalculator.Calculate(racers, trackId);

        Debug.Log("═══ Round " + CurrentRound + "/" + TotalRounds
            + " | " + CurrentRoundLaps + "바퀴 | 트랙: " + trackName
            + " | " + BettingCalculator.GetTypeName(CurrentBet.type) + " 배팅 ═══");
        ChangeState(GameState.Countdown);
    }

    // ═══ 점수 계산 ═══
    private void CalcScore()
    {
        var rankings = RaceManager.Instance?.GetFinalRankings();
        if (rankings == null || rankings.Count < 3) return;

        // 인덱스 리스트 (0=1등, 1=2등, 2=3등...)
        List<int> rankingIndices = new List<int>();
        foreach (var r in rankings)
            rankingIndices.Add(r.racerIndex);

        int score = BettingCalculator.Calculate(CurrentBet, rankingIndices);

        Debug.Log("[결과] Round " + CurrentRound + " | "
            + BettingCalculator.GetTypeName(CurrentBet.type) + " → "
            + (score > 0 ? "적중! +" + score + "점" : "실패 +0점"));

        // ── 순위 데이터 구성 (charName = UID 사용, DisplayName 혼용 금지) ──
        var racerResults = new List<RoundRacerResult>();
        var allRacers = RaceManager.Instance?.Racers;
        Debug.Log("[CalcScore] allRacers count=" + (allRacers != null ? allRacers.Count : 0));
        foreach (var r in rankings)
        {
            // racerIndex로 CharData에서 charName(UID) 획득
            string uid = r.racerName; // fallback: DisplayName
            if (allRacers != null && r.racerIndex >= 0 && r.racerIndex < allRacers.Count)
            {
                var cd = allRacers[r.racerIndex].CharData;
                if (cd != null) uid = cd.charName;

                // 진단 로그: racerIndex ↔ charName 매핑 검증
                Debug.Log(string.Format("[CalcScore] rank={0} racerIdx={1} DisplayName={2} → UID={3}",
                    r.rank, r.racerIndex, r.racerName, uid));
            }
            else
            {
                Debug.LogWarning(string.Format("[CalcScore] rank={0} racerIdx={1} → allRacers에서 찾지 못함! fallback={2}",
                    r.rank, r.racerIndex, uid));
            }

            racerResults.Add(new RoundRacerResult
            {
                charName = uid,
                rank = r.rank
            });
        }

        // ── 트랙명 ──
        string trackName = "기본";
        if (TrackDatabase.Instance?.CurrentTrackInfo != null)
            trackName = TrackDatabase.Instance.CurrentTrackInfo.DisplayName;

        // ── 내가 선택한 캐릭터 이름들 ──
        var selectedNames = new List<string>();
        if (CurrentBet != null && RaceManager.Instance?.Racers != null)
        {
            var racers = RaceManager.Instance.Racers;
            foreach (int idx in CurrentBet.selections)
            {
                if (idx >= 0 && idx < racers.Count && racers[idx].CharData != null)
                    selectedNames.Add(racers[idx].CharData.DisplayName);
            }
        }

        // ScoreManager에 라운드 결과 기록
        ScoreManager.Instance?.RecordRound(CurrentBet.type, score, trackName, racerResults, selectedNames);
    }

    // ═══ 다음 라운드 ═══
    public void NextRound()
    {
        if (IsLastRound)
        {
            // 마지막 라운드 → Finish 화면
            Debug.Log("═══ 전체 " + TotalRounds + " 라운드 종료! 총점: "
                + ScoreManager.Instance?.CurrentGameScore + " ═══");
            ChangeState(GameState.Finish);
            return;
        }

        CurrentRound++;
        ApplyRoundLaps();
        RaceManager.Instance?.ResetRace();

        // ★ 트랙 변경 (전판 제외 + weight 랜덤)
        ApplyTrackForCurrentRound();

        Debug.Log("═══ Next → Round " + CurrentRound + "/" + TotalRounds
            + " | " + CurrentRoundLaps + "바퀴 ═══");

        OnRoundChanged?.Invoke(CurrentRound);

        // ★ 배팅 화면 진입 전 인기도/배당/컨디션 계산
        var racersForOdds2 = CharacterDatabase.Instance?.SelectedCharacters;
        string trackIdForOdds2 = TrackDatabase.Instance?.CurrentTrackInfo?.trackId ?? "normal";
        OddsCalculator.Calculate(racersForOdds2, trackIdForOdds2);

        ChangeState(GameState.Betting);
    }

    // ═══ 트랙 적용 ═══
    private void ApplyTrackForCurrentRound()
    {
        var trackDB = TrackDatabase.Instance;
        if (trackDB == null)
        {
            Debug.LogWarning("[GameManager] TrackDatabase 없음 → 트랙 미적용");
            return;
        }

        TrackInfo trackInfo = trackDB.ApplyTrackForRound(CurrentRound);
        if (trackInfo == null) return;

        // TrackVisualizer에 배경 교체 요청
        var gs = GameSettings.Instance;
        if (gs.enableTrackTransition && CurrentRound > 1 && TrackTransition.Instance != null)
        {
            // ★ 페이드 전환 연출
            TrackTransition.Instance.PlayTransition(gs.trackTransitionFadeDuration, () =>
            {
                if (TrackVisualizer.Instance != null)
                    TrackVisualizer.Instance.LoadTrack(trackInfo);
            });
        }
        else
        {
            // 연출 OFF 또는 Round 1 → 즉시 교체
            if (TrackVisualizer.Instance != null)
                TrackVisualizer.Instance.LoadTrack(trackInfo);
        }

        // ★ 트랙별 웨이포인트 재로드
        if (RaceManager.Instance != null)
            RaceManager.Instance.ReloadWaypoints();

        OnTrackChanged?.Invoke(trackInfo);

        // ── 트랙 효과 디버그 로그 ──
        LogTrackEffects(gs.currentTrack, trackInfo);
    }

    /// <summary>
    /// 트랙 적용 시 변경된 수치만 디버그 오버레이에 기록
    /// </summary>
    private void LogTrackEffects(TrackData track, TrackInfo trackInfo)
    {
        if (track == null) return;
        var rm = RaceManager.Instance;
        if (rm == null) return;
        var overlay = rm.GetComponent<RaceDebugOverlay>();
        if (overlay == null) return;

        var parts = new System.Collections.Generic.List<string>();

        if (track.speedMultiplier != 1f)
            parts.Add(string.Format("speed:×{0:F1}", track.speedMultiplier));
        if (track.noiseMultiplier != 1f)
            parts.Add(string.Format("noise:×{0:F1}", track.noiseMultiplier));
        if (track.fatigueMultiplier != 1f)
            parts.Add(string.Format("fatigue:×{0:F1}", track.fatigueMultiplier));
        if (track.powerSpeedBonus != 0f)
            parts.Add(string.Format("power→spd:+{0:F2}", track.powerSpeedBonus));
        if (track.braveSpeedBonus != 0f)
            parts.Add(string.Format("brave→spd:+{0:F2}", track.braveSpeedBonus));
        if (track.luckMultiplier != 1f)
            parts.Add(string.Format("luck:×{0:F1}", track.luckMultiplier));
        if (track.earlyBonusMultiplier != 1f)
            parts.Add(string.Format("초반:×{0:F1}", track.earlyBonusMultiplier));
        if (track.midBonusMultiplier != 1f)
            parts.Add(string.Format("중반:×{0:F1}", track.midBonusMultiplier));
        if (track.lateBonusMultiplier != 1f)
            parts.Add(string.Format("후반:×{0:F1}", track.lateBonusMultiplier));
        if (track.hasMidSlowZone)
            parts.Add(string.Format("감속구간:{0:P0}~{1:P0}(×{2:F1})",
                track.midSlowZoneStart, track.midSlowZoneEnd, track.midSlowZoneSpeedMultiplier));
        if (track.collisionRangeMultiplier != 1f)
            parts.Add(string.Format("충돌범위:×{0:F1}", track.collisionRangeMultiplier));
        if (track.slingshotMultiplier != 1f)
            parts.Add(string.Format("슬링샷:×{0:F1}", track.slingshotMultiplier));

        string trackName = trackInfo != null ? trackInfo.DisplayName : track.name;
        string effects = parts.Count > 0 ? string.Join(" ", parts.ToArray()) : "보정 없음";

        overlay.LogEvent(RaceDebugOverlay.EventType.Track,
            string.Format("{0} 적용 | {1}", trackName, effects));
    }

    // 현재 라운드의 바퀴 수를 RaceManager에 적용
    private void ApplyRoundLaps()
    {
        int laps = CurrentRoundLaps;
        RaceManager.Instance?.SetLaps(laps);
        Debug.Log("[라운드] Round " + CurrentRound + ", Laps: " + laps);
    }
}