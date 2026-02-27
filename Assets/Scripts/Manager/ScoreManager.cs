using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// 라운드 결과 기록 1건
/// </summary>
[System.Serializable]
public class RoundRecord
{
    public int round;           // 몇 라운드
    public BetType betType;     // 배팅 타입
    public int score;           // 획득 점수 (0이면 실패)
    public string trackName;    // ★ 트랙명
    public List<RoundRacerResult> racerResults = new List<RoundRacerResult>(); // ★ 캐릭터별 결과
    public bool isWin => score > 0;
}

/// <summary>
/// 라운드 내 캐릭터 1명의 결과
/// </summary>
[System.Serializable]
public class RoundRacerResult
{
    public string charId;
    public int rank;
}

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    // ═══════════════════════════════════════
    //  1계층: 누적 통계 (PlayerPrefs, 영구 보존)
    //  → ResetAll()에서 건드리지 않음
    // ═══════════════════════════════════════

    public int TotalScore { get; private set; }
    public int TotalRounds { get; private set; }
    public int TotalWins { get; private set; }
    public float WinRate => TotalRounds > 0 ? (float)TotalWins / TotalRounds * 100f : 0f;

    // ── 캐릭터 영구 기록 ──
    private CharacterRecordStore charRecordStore = new CharacterRecordStore();

    // ── 배팅 영구 기록 ──
    private BetRecordStore betRecordStore = new BetRecordStore();

    // ═══════════════════════════════════════
    //  2계층: 현재 게임 통계 (메모리, 게임 리셋)
    // ═══════════════════════════════════════

    public List<RoundRecord> RoundHistory { get; private set; } = new List<RoundRecord>();
    public int LastRoundScore { get; private set; }

    // ── 현재 게임 내 캐릭터 출현 횟수 ──
    private Dictionary<string, int> currentGameAppearances = new Dictionary<string, int>();

    // ═══════════════════════════════════════
    //  이벤트
    // ═══════════════════════════════════════

    public event Action<int, int> OnScoreChanged;

    // ═══════════════════════════════════════
    //  초기화
    // ═══════════════════════════════════════

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        // 누적 통계 로드
        TotalScore = PlayerPrefs.GetInt("DopamineRace_TotalScore", 0);
        TotalRounds = PlayerPrefs.GetInt("DopamineRace_TotalRounds", 0);
        TotalWins = PlayerPrefs.GetInt("DopamineRace_TotalWins", 0);

        // ★ 세이브 버전 체크 (v2→v3: 거리별 누적 카운터 추가)
        const int SAVE_VERSION = 3;
        int savedVersion = PlayerPrefs.GetInt("DopamineRace_SaveVersion", 1);
        if (savedVersion < SAVE_VERSION)
        {
            Debug.LogWarning("[ScoreManager] 세이브 버전 업그레이드: v" + savedVersion + " → v" + SAVE_VERSION + " (기존 기록 초기화)");
            PlayerPrefs.DeleteKey("DopamineRace_CharRecords");
            PlayerPrefs.DeleteKey("DopamineRace_BetRecords");
            PlayerPrefs.SetInt("DopamineRace_SaveVersion", SAVE_VERSION);
            PlayerPrefs.Save();
        }

        // 캐릭터 기록 로드
        string charJson = PlayerPrefs.GetString("DopamineRace_CharRecords", "");
        if (!string.IsNullOrEmpty(charJson))
            charRecordStore = JsonUtility.FromJson<CharacterRecordStore>(charJson);

        // 배팅 기록 로드
        string betJson = PlayerPrefs.GetString("DopamineRace_BetRecords", "");
        if (!string.IsNullOrEmpty(betJson))
            betRecordStore = JsonUtility.FromJson<BetRecordStore>(betJson);

        // ★ 잘못된 형식의 기록 자동 정리
        CleanupInvalidCharRecords();

        // ★ 레거시 마이그레이션: recentRaceEntries 백필 (필드 추가 이전 세이브 호환)
        foreach (var record in charRecordStore.records)
            record.MigrateRaceEntriesIfNeeded();

        Debug.Log("[ScoreManager 로드] 캐릭터 기록: " + charRecordStore.records.Count + "명"
            + " | 배팅 기록: " + betRecordStore.records.Count + "건"
            + " | 누적: " + TotalScore + "점/" + TotalRounds + "라운드/" + TotalWins + "승");
    }

    /// <summary>
    /// charId 형식(char.*)이 아닌 기록을 제거 (이전 버전 호환)
    /// </summary>
    private void CleanupInvalidCharRecords()
    {
        int removed = charRecordStore.records.RemoveAll(r =>
            r.charId != null && !r.charId.StartsWith("char."));
        if (removed > 0)
        {
            Debug.LogWarning("[ScoreManager] 잘못된 형식의 기록 " + removed + "건 제거");
            SaveCharRecords();
        }
    }

    // ═══════════════════════════════════════
    //  ★ 라운드 결과 기록 (메인 진입점)
    // ═══════════════════════════════════════

    /// <summary>
    /// 레이스 종료 후 호출. 순위 + 배팅 결과를 모두 기록.
    /// </summary>
    public void RecordRound(BetType betType, int score, string trackName,
        List<RoundRacerResult> racerResults, List<string> selectedCharIds)
    {
        int roundNum = GameManager.Instance != null ? GameManager.Instance.CurrentRound : RoundHistory.Count + 1;

        // ── 2계층: 현재 게임 기록 ──
        RoundRecord record = new RoundRecord
        {
            round = roundNum,
            betType = betType,
            score = score,
            trackName = trackName,
            racerResults = racerResults
        };
        RoundHistory.Add(record);

        // 현재 게임 출현 횟수 갱신
        foreach (var rr in racerResults)
        {
            if (currentGameAppearances.ContainsKey(rr.charId))
                currentGameAppearances[rr.charId]++;
            else
                currentGameAppearances[rr.charId] = 1;
        }

        // ── 1계층: 누적 통계 갱신 ──
        LastRoundScore = score;
        TotalScore += score;
        TotalRounds++;
        if (score > 0) TotalWins++;
        SaveCumulativeStats();

        // ── 캐릭터 영구 기록 갱신 ──
        int currentLaps = GameSettings.Instance.GetLapsForRound(roundNum);
        string distKey = GameSettings.Instance.GetDistanceKey(currentLaps);
        foreach (var rr in racerResults)
        {
            var charRecord = charRecordStore.GetOrCreate(rr.charId);
            charRecord.AddResult(trackName, rr.rank, currentLaps);
            charRecord.UpdateDistanceCount(distKey, rr.rank);  // 거리별 누적 카운터
            Debug.Log(string.Format("[ScoreManager] 기록저장: {0} rank={1} → TotalRaces={2} recentRanks=[{3}]",
                rr.charId, rr.rank,
                charRecord.TotalRaces, charRecord.GetOverallRankString()));
        }
        SaveCharRecords();

        // ── 배팅 영구 기록 ──
        var resultIds = new List<string>();
        foreach (var rr in racerResults)
            resultIds.Add(rr.charId);

        BetRecord betRecord = new BetRecord
        {
            round = roundNum,
            trackName = trackName,
            betType = betType,
            betTypeName = BettingCalculator.GetTypeName(betType),
            selectedCharIds = selectedCharIds ?? new List<string>(),
            resultRanking = resultIds,
            score = score
        };
        betRecordStore.AddRecord(betRecord);
        SaveBetRecords();

        // ── 로그 ──
        Debug.Log("[점수] R" + roundNum + ": " + BettingCalculator.GetTypeName(betType)
            + " → " + (score > 0 ? "+" + score : "0") + "점"
            + " | 총점: " + TotalScore
            + " | 트랙: " + trackName);

        OnScoreChanged?.Invoke(score, TotalScore);
    }

    /// <summary>
    /// 기존 호환: 점수만 추가 (순위 데이터 없이)
    /// </summary>
    public void AddScore(int a)
    {
        LastRoundScore = a;
        TotalScore += a; TotalRounds++;
        if (a > 0) TotalWins++;
        SaveCumulativeStats();
        OnScoreChanged?.Invoke(a, TotalScore);
    }

    // ═══════════════════════════════════════
    //  ★ 새 게임 리셋 (현재 게임만, 누적은 유지)
    // ═══════════════════════════════════════

    public void ResetAll()
    {
        // 2계층만 리셋
        RoundHistory.Clear();
        LastRoundScore = 0;
        currentGameAppearances.Clear();

        // 1계층(누적), 캐릭터 기록, 배팅 기록은 유지!
        Debug.Log("[점수] 새 게임 시작 → 게임 히스토리 초기화 (누적 통계 유지)");
    }

    /// <summary>
    /// 캐릭터 성적 기록 리셋
    /// "all"   = 전체 초기화 (배당 균등화)
    /// "decay" = raceCount 절반 압축 (소프트 리셋)
    /// "recent"= 최근 순위만 초기화 (트랙 출전횟수는 유지)
    /// </summary>
    public void ResetCharacterRecords(string mode = "all")
    {
        switch (mode)
        {
            case "all":
                charRecordStore.records.Clear();
                Debug.Log("[ScoreManager] 캐릭터 성적 전체 초기화");
                break;
            case "decay":
                foreach (var r in charRecordStore.records)
                    r.AutoDecayIfNeeded(100);
                Debug.Log("[ScoreManager] 캐릭터 성적 감쇠 처리 (출전 횟수 절반 압축)");
                break;
            case "recent":
                foreach (var r in charRecordStore.records)
                    r.recentOverallRanks.Clear();
                Debug.Log("[ScoreManager] 캐릭터 최근 순위 초기화 (출전 횟수 유지)");
                break;
        }
        SaveCharRecords();
    }

    // ═══════════════════════════════════════
    //  Finish 시 리더보드 저장
    // ═══════════════════════════════════════

    public void SaveToLeaderboard()
    {
        if (RoundHistory.Count > 0)
        {
            LeaderboardData.Save(CurrentGameScore, RoundHistory.Count, GetRoundSummary());
            Debug.Log("[리더보드] 게임 결과 저장: " + CurrentGameScore + "점 (" + RoundHistory.Count + "라운드)");
        }
    }

    // ═══════════════════════════════════════
    //  현재 게임 조회
    // ═══════════════════════════════════════

    /// <summary>이번 게임의 총 획득 점수</summary>
    public int CurrentGameScore
    {
        get
        {
            int sum = 0;
            foreach (var r in RoundHistory)
                sum += r.score;
            return sum;
        }
    }

    /// <summary>현재 게임에서 특정 캐릭터의 출현 횟수</summary>
    public int GetAppearanceCount(string charId)
    {
        return currentGameAppearances.ContainsKey(charId) ? currentGameAppearances[charId] : 0;
    }

    /// <summary>라운드 결과 요약 문자열 (결과 화면용)</summary>
    public string GetRoundSummary()
    {
        if (RoundHistory.Count == 0) return "기록 없음";
        var parts = new List<string>();
        foreach (var r in RoundHistory)
        {
            string typeName = BettingCalculator.GetTypeName(r.betType);
            string scoreStr = r.score > 0 ? "+" + r.score + "점" : "+0점";
            parts.Add("R" + r.round + ": " + typeName + " " + scoreStr);
        }
        return string.Join(" | ", parts.ToArray());
    }

    // ═══════════════════════════════════════
    //  ★ 캐릭터 통계 조회 (배팅 화면용)
    // ═══════════════════════════════════════

    /// <summary>캐릭터 기록 조회 (없으면 null)</summary>
    public CharacterRecord GetCharacterRecord(string charId)
    {
        return charRecordStore.Find(charId);
    }

    /// <summary>전체 캐릭터 기록 목록</summary>
    public List<CharacterRecord> GetAllCharacterRecords()
    {
        return charRecordStore.records;
    }

    // ═══════════════════════════════════════
    //  ★ 배팅 타입별 적중률 조회
    // ═══════════════════════════════════════

    /// <summary>특정 배팅 타입의 적중 통계</summary>
    public BetTypeStats GetBetTypeStats(BetType type)
    {
        return betRecordStore.GetTypeStats(type);
    }

    /// <summary>전체 배팅 타입별 적중 통계 목록</summary>
    public List<BetTypeStats> GetAllBetTypeStats()
    {
        return betRecordStore.typeStats;
    }

    /// <summary>전체 배팅 기록 목록</summary>
    public List<BetRecord> GetAllBetRecords()
    {
        return betRecordStore.records;
    }

    // ═══════════════════════════════════════
    //  저장
    // ═══════════════════════════════════════

    private void SaveCumulativeStats()
    {
        PlayerPrefs.SetInt("DopamineRace_TotalScore", TotalScore);
        PlayerPrefs.SetInt("DopamineRace_TotalRounds", TotalRounds);
        PlayerPrefs.SetInt("DopamineRace_TotalWins", TotalWins);
        PlayerPrefs.Save();
    }

    private void SaveCharRecords()
    {
        string json = JsonUtility.ToJson(charRecordStore);
        PlayerPrefs.SetString("DopamineRace_CharRecords", json);
        PlayerPrefs.Save();
    }

    private void SaveBetRecords()
    {
        string json = JsonUtility.ToJson(betRecordStore);
        PlayerPrefs.SetString("DopamineRace_BetRecords", json);
        PlayerPrefs.Save();
    }
}
