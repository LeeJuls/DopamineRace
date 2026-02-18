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
    public bool isWin => score > 0;
}

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }
    public int TotalScore { get; private set; }
    public int TotalRounds { get; private set; }
    public int TotalWins { get; private set; }
    public int LastRoundScore { get; private set; }
    public float WinRate => TotalRounds > 0 ? (float)TotalWins / TotalRounds * 100f : 0f;
    public event Action<int, int> OnScoreChanged;

    // ═══ 라운드 히스토리 ═══
    public List<RoundRecord> RoundHistory { get; private set; } = new List<RoundRecord>();

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        // 누적 통계는 PlayerPrefs에서 로드 (전체 세션 통계)
        TotalScore = PlayerPrefs.GetInt("DopamineRace_TotalScore", 0);
        TotalRounds = PlayerPrefs.GetInt("DopamineRace_TotalRounds", 0);
        TotalWins = PlayerPrefs.GetInt("DopamineRace_TotalWins", 0);
    }

    /// <summary>
    /// 기존 호환: 점수 추가 (직접 호출 시)
    /// </summary>
    public void AddScore(int a)
    {
        LastRoundScore = a;
        TotalScore += a; TotalRounds++;
        if (a > 0) TotalWins++;
        SaveStats();
        OnScoreChanged?.Invoke(a, TotalScore);
    }

    /// <summary>
    /// ★ 라운드 결과 기록 + 점수 반영
    /// </summary>
    public void RecordRound(BetType betType, int score)
    {
        int roundNum = GameManager.Instance != null ? GameManager.Instance.CurrentRound : RoundHistory.Count + 1;

        RoundRecord record = new RoundRecord
        {
            round = roundNum,
            betType = betType,
            score = score
        };
        RoundHistory.Add(record);

        // 점수 반영
        LastRoundScore = score;
        TotalScore += score;
        TotalRounds++;
        if (score > 0) TotalWins++;
        SaveStats();

        Debug.Log("[점수] R" + roundNum + ": " + BettingCalculator.GetTypeName(betType)
            + " → " + (score > 0 ? "+" + score : "0") + "점"
            + " | 총점: " + TotalScore);

        OnScoreChanged?.Invoke(score, TotalScore);
    }

    /// <summary>
    /// ★ 새 게임 시작 시: 이번 게임 히스토리만 리셋 (누적 통계는 유지)
    /// </summary>
    public void ResetAll()
    {
        RoundHistory.Clear();
        LastRoundScore = 0;
        TotalScore = 0;
        TotalRounds = 0;
        TotalWins = 0;
        SaveStats();
        Debug.Log("[점수] 새 게임 시작 → 점수 초기화 (0점)");
    }

    /// <summary>
    /// ★ Finish 시 리더보드에 저장 (전체 라운드 합산)
    /// </summary>
    public void SaveToLeaderboard()
    {
        if (RoundHistory.Count > 0)
        {
            LeaderboardData.Save(CurrentGameScore, RoundHistory.Count, GetRoundSummary());
            Debug.Log("[리더보드] 게임 결과 저장: " + CurrentGameScore + "점 (" + RoundHistory.Count + "라운드)");
        }
    }

    /// <summary>
    /// 이번 게임의 총 획득 점수
    /// </summary>
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

    /// <summary>
    /// 라운드 결과 요약 문자열 (결과 화면용)
    /// 예: "R1: 쌍승 +6점 | R2: 단승 +0점 | R3: 복연승 +2점"
    /// </summary>
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

    private void SaveStats()
    {
        PlayerPrefs.SetInt("DopamineRace_TotalScore", TotalScore);
        PlayerPrefs.SetInt("DopamineRace_TotalRounds", TotalRounds);
        PlayerPrefs.SetInt("DopamineRace_TotalWins", TotalWins);
        PlayerPrefs.Save();
    }
}