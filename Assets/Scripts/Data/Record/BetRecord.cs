using System.Collections.Generic;

// ══════════════════════════════════════════
//  배팅 1건의 기록
// ══════════════════════════════════════════

[System.Serializable]
public class BetRecord
{
    public int round;                        // 라운드 번호
    public string trackName;                 // 트랙명
    public string betTypeName;               // 배팅 타입명 (단승, 쌍승 등)
    public BetType betType;                  // 배팅 타입 enum
    public List<string> selectedCharIds = new List<string>();  // 내가 선택한 캐릭터들
    public List<string> resultRanking = new List<string>();      // 실제 결과 (1착, 2착, 3착...)
    public int score;                        // 획득 점수
    public bool isWin => score > 0;
}

// ══════════════════════════════════════════
//  배팅 타입별 적중 통계
// ══════════════════════════════════════════

[System.Serializable]
public class BetTypeStats
{
    public string betTypeName;
    public BetType betType;
    public int totalBets;     // 총 배팅 횟수
    public int totalWins;     // 적중 횟수

    public float HitRate => totalBets > 0 ? (float)totalWins / totalBets * 100f : 0f;
}

// ══════════════════════════════════════════
//  JSON 직렬화용 래퍼
// ══════════════════════════════════════════

[System.Serializable]
public class BetRecordStore
{
    public List<BetRecord> records = new List<BetRecord>();
    public List<BetTypeStats> typeStats = new List<BetTypeStats>();

    /// <summary>배팅 기록 추가 + 타입별 적중률 갱신</summary>
    public void AddRecord(BetRecord record)
    {
        records.Add(record);
        UpdateTypeStats(record.betType, record.isWin);
    }

    /// <summary>특정 배팅 타입의 적중률 조회</summary>
    public BetTypeStats GetTypeStats(BetType type)
    {
        foreach (var s in typeStats)
            if (s.betType == type) return s;
        return null;
    }

    private void UpdateTypeStats(BetType type, bool isWin)
    {
        BetTypeStats stats = null;
        foreach (var s in typeStats)
        {
            if (s.betType == type) { stats = s; break; }
        }

        if (stats == null)
        {
            stats = new BetTypeStats
            {
                betType = type,
                betTypeName = BettingCalculator.GetTypeName(type)
            };
            typeStats.Add(stats);
        }

        stats.totalBets++;
        if (isWin) stats.totalWins++;
    }
}
