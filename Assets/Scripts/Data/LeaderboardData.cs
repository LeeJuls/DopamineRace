using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;

/// <summary>
/// 리더보드 1건의 기록
/// </summary>
[System.Serializable]
public class LeaderboardEntry
{
    public int score;
    public int rounds;
    public string date;         // "2026-02-11 14:30"
    public string summary;      // "R1:쌍승+6 | R2:단승+0 | R3:복연승+2"
}

/// <summary>
/// 리더보드 전체 데이터 (JSON 직렬화용)
/// </summary>
[System.Serializable]
public class LeaderboardSaveData
{
    public List<LeaderboardEntry> entries = new List<LeaderboardEntry>();
}

/// <summary>
/// Top 100 리더보드 관리 (JSON 파일 저장)
/// </summary>
public static class LeaderboardData
{
    private const int MAX_ENTRIES = 100;
    private const string FILE_NAME = "leaderboard.json";

    private static string FilePath
    {
        get { return Path.Combine(Application.persistentDataPath, FILE_NAME); }
    }

    /// <summary>
    /// 게임 결과 저장 (점수 높은 순 정렬, 100개 초과 시 최하위 삭제)
    /// </summary>
    public static void Save(int score, int rounds, string summary)
    {
        var data = Load();

        LeaderboardEntry entry = new LeaderboardEntry
        {
            score = score,
            rounds = rounds,
            date = DateTime.Now.ToString("yyyy-MM-dd HH:mm"),
            summary = summary
        };

        data.entries.Add(entry);

        // 점수 높은 순 정렬
        data.entries.Sort((a, b) => b.score.CompareTo(a.score));

        // 100개 제한
        if (data.entries.Count > MAX_ENTRIES)
            data.entries.RemoveRange(MAX_ENTRIES, data.entries.Count - MAX_ENTRIES);

        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(FilePath, json);

        Debug.Log("[리더보드] 저장 완료! " + score + "점 (총 " + data.entries.Count + "개 기록)");
    }

    /// <summary>
    /// 리더보드 로드
    /// </summary>
    public static LeaderboardSaveData Load()
    {
        if (!File.Exists(FilePath))
            return new LeaderboardSaveData();

        try
        {
            string json = File.ReadAllText(FilePath);
            var data = JsonUtility.FromJson<LeaderboardSaveData>(json);
            return data ?? new LeaderboardSaveData();
        }
        catch (Exception e)
        {
            Debug.LogWarning("[리더보드] 로드 실패: " + e.Message);
            return new LeaderboardSaveData();
        }
    }

    /// <summary>
    /// Top N 가져오기
    /// </summary>
    public static List<LeaderboardEntry> GetTop(int count = 100)
    {
        var data = Load();
        int n = Mathf.Min(count, data.entries.Count);
        return data.entries.GetRange(0, n);
    }

    /// <summary>
    /// 전체 기록 수
    /// </summary>
    public static int Count
    {
        get { return Load().entries.Count; }
    }

    /// <summary>
    /// 리더보드 초기화 (디버그용)
    /// </summary>
    public static void Clear()
    {
        if (File.Exists(FilePath))
            File.Delete(FilePath);
        Debug.Log("[리더보드] 전체 삭제");
    }
}
