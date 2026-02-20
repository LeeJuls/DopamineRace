using UnityEngine;
using System.Collections.Generic;

// ══════════════════════════════════════════
//  캐릭터의 특정 맵에서의 기록
// ══════════════════════════════════════════

[System.Serializable]
public class CharacterTrackRecord
{
    public string trackName;
    public int raceCount;                    // 이 맵 출전 횟수
    public List<int> recentRanks = new List<int>();  // 최근 10경기 순위

    private const int MAX_RECENT = 10;

    public void AddRank(int rank)
    {
        recentRanks.Insert(0, rank);
        if (recentRanks.Count > MAX_RECENT)
            recentRanks.RemoveAt(recentRanks.Count - 1);
        raceCount++;
    }

    /// <summary>최근 순위를 "1-3-2-1-4" 형태 문자열로</summary>
    public string GetRankString()
    {
        if (recentRanks.Count == 0) return "-";
        var parts = new List<string>();
        foreach (var r in recentRanks)
            parts.Add(r.ToString());
        return string.Join("-", parts.ToArray());
    }
}

// ══════════════════════════════════════════
//  캐릭터 1명의 전체 기록
// ══════════════════════════════════════════

[System.Serializable]
public class CharacterRecord
{
    public string charName;
    public List<CharacterTrackRecord> trackRecords = new List<CharacterTrackRecord>();
    public List<int> recentOverallRanks = new List<int>();  // 최근 10게임 순위 (맵 무관)

    private const int MAX_RECENT = 10;

    /// <summary>레이스 결과 기록 (맵별 + 전체)</summary>
    public void AddResult(string trackName, int rank)
    {
        // 전체 최근 순위
        recentOverallRanks.Insert(0, rank);
        if (recentOverallRanks.Count > MAX_RECENT)
            recentOverallRanks.RemoveAt(recentOverallRanks.Count - 1);

        // 맵별 기록
        CharacterTrackRecord tr = GetOrCreateTrackRecord(trackName);
        tr.AddRank(rank);
    }

    /// <summary>전체 출전 횟수</summary>
    public int TotalRaces
    {
        get
        {
            int sum = 0;
            foreach (var tr in trackRecords)
                sum += tr.raceCount;
            return sum;
        }
    }

    /// <summary>특정 맵 출전 횟수</summary>
    public int GetTrackRaceCount(string trackName)
    {
        var tr = FindTrackRecord(trackName);
        return tr != null ? tr.raceCount : 0;
    }

    /// <summary>특정 맵 최근 순위 문자열</summary>
    public string GetTrackRankString(string trackName)
    {
        var tr = FindTrackRecord(trackName);
        return tr != null ? tr.GetRankString() : "-";
    }

    // ── 최근 10게임 N착 확률 ──

    public float GetRankRate(int targetRank)
    {
        if (recentOverallRanks.Count == 0) return 0f;
        int count = 0;
        foreach (var r in recentOverallRanks)
            if (r == targetRank) count++;
        return (float)count / recentOverallRanks.Count;
    }

    /// <summary>최근 10게임 1착 확률</summary>
    public float WinRate => GetRankRate(1);

    /// <summary>최근 10게임 2착 확률</summary>
    public float PlaceRate => GetRankRate(2);

    /// <summary>최근 10게임 3착 확률</summary>
    public float ShowRate => GetRankRate(3);

    /// <summary>전체 최근 순위를 "1-3-2-1-4" 형태 문자열로</summary>
    public string GetOverallRankString()
    {
        if (recentOverallRanks.Count == 0) return "-";
        var parts = new List<string>();
        foreach (var r in recentOverallRanks)
            parts.Add(r.ToString());
        return string.Join("-", parts.ToArray());
    }

    // ── 내부 유틸 ──

    /// <summary>특정 트랙 기록 반환 (없으면 null). OddsCalculator에서 사용.</summary>
    public CharacterTrackRecord FindTrackRecord(string trackName)
    {
        foreach (var tr in trackRecords)
            if (tr.trackName == trackName) return tr;
        return null;
    }

    private CharacterTrackRecord GetOrCreateTrackRecord(string trackName)
    {
        var existing = FindTrackRecord(trackName);
        if (existing != null) return existing;

        var newRecord = new CharacterTrackRecord { trackName = trackName };
        trackRecords.Add(newRecord);
        return newRecord;
    }

    /// <summary>
    /// 자동 감쇠: 트랙별 raceCount가 threshold 초과 시 절반 압축
    /// ScoreManager.ResetCharacterRecords("decay")에서 호출
    /// </summary>
    public void AutoDecayIfNeeded(int threshold = 100)
    {
        foreach (var tr in trackRecords)
        {
            if (tr.raceCount > threshold)
                tr.raceCount = threshold / 2;
        }
    }
}

// ══════════════════════════════════════════
//  JSON 직렬화용 래퍼 (JsonUtility는 Dictionary 미지원)
// ══════════════════════════════════════════

[System.Serializable]
public class CharacterRecordStore
{
    public List<CharacterRecord> records = new List<CharacterRecord>();

    public CharacterRecord GetOrCreate(string charName)
    {
        foreach (var r in records)
            if (r.charName == charName) return r;

        var newRecord = new CharacterRecord { charName = charName };
        records.Add(newRecord);
        return newRecord;
    }

    public CharacterRecord Find(string charName)
    {
        foreach (var r in records)
            if (r.charName == charName) return r;
        return null;
    }
}
