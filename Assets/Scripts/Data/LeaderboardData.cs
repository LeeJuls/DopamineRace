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
    public string name;         // 아케이드 이니셜 3글자 (레거시 데이터는 "" → 표시 시 "---")
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

    // 무결성 MAC 사이드카 (SPEC-044 Phase E) — 스키마/서버계약 불변 위해 JSON 외부 파일로 분리.
    private static string MacPath
    {
        get { return FilePath + ".mac"; }
    }

    /// <summary>json을 쓰고 localKey HMAC 사이드카(.mac)도 함께 기록.</summary>
    private static void WriteWithMac(string json)
    {
        File.WriteAllText(FilePath, json);
        File.WriteAllText(MacPath, CryptoSign.HmacHex(json, CryptoSign.LocalKey));
    }

    /// <summary>
    /// 게임 결과 저장 (점수 높은 순 정렬, 100개 초과 시 최하위 삭제)
    /// </summary>
    public static void Save(int score, int rounds, string summary, string name = "AAA")
    {
        var data = Load();

        LeaderboardEntry entry = new LeaderboardEntry
        {
            score = score,
            rounds = rounds,
            date = DateTime.Now.ToString("yyyy-MM-dd HH:mm"),
            name = name,
            summary = summary
        };

        data.entries.Add(entry);

        // 점수 높은 순 정렬
        data.entries.Sort((a, b) => b.score.CompareTo(a.score));

        // 100개 제한
        if (data.entries.Count > MAX_ENTRIES)
            data.entries.RemoveRange(MAX_ENTRIES, data.entries.Count - MAX_ENTRIES);

        string json = JsonUtility.ToJson(data, true);
        WriteWithMac(json);

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

            // 무결성 검증 (MAC 사이드카). 변조 시 캐시 무시 — 단, 서버 제출 점수와 무관(로컬 표시 전용).
            if (File.Exists(MacPath))
            {
                string actual = File.ReadAllText(MacPath);
                string expected = CryptoSign.HmacHex(json, CryptoSign.LocalKey);
                if (!CryptoSign.ConstantTimeEquals(expected, actual))
                {
                    Debug.LogWarning("[리더보드] 무결성 검증 실패 — 변조 의심, 로컬 캐시 무시");
                    return new LeaderboardSaveData();
                }
            }
            else
            {
                // 레거시(MAC 없음): 기존 기록 보존 — 1회 수용 후 MAC 부여(grace).
                WriteWithMac(json);
                Debug.Log("[리더보드] 레거시 캐시 → MAC 부여 (grace)");
            }

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
    /// 점수가 Top100 자격(이름 입력·저장 대상)인지 판정.
    /// 미포화(100개 미만)면 점수 무관 true, 포화면 현 최하위 초과만 true
    /// (동점 미자격 — Save 정렬·100컷과 일치).
    /// </summary>
    public static bool Qualifies(int score)
    {
        var data = Load();
        if (data.entries.Count < MAX_ENTRIES) return true;
        int lowest = int.MaxValue;
        foreach (var e in data.entries)
            if (e.score < lowest) lowest = e.score;
        return score > lowest;
    }

    /// <summary>
    /// 이 점수가 로컬 리더보드에서 몇 위로 들어가는지(1-based). 순위권 밖이면 0.
    /// rank = (저장된 entries 중 score 이상(>=) 항목 수) + 1.
    /// 동점은 ">="로 카운트 → 신규 점수는 동점자 **아래**(Save 안정정렬 실배치와 일치).
    /// 빈 목록 → 1. 포화(MAX_ENTRIES)에서 최하 이하·동점최하 → 진입 불가 → 0(Qualifies "동점 미자격"과 일치).
    /// </summary>
    public static int GetRank(int score)
    {
        var data = Load();
        int atOrAbove = 0;
        int lowest = int.MaxValue;
        foreach (var e in data.entries)
        {
            if (e.score >= score) atOrAbove++;    // ">=" 카운트(신규는 동점자 아래 → Save 실배치 일치)
            if (e.score < lowest) lowest = e.score;
        }
        // 포화(100개) 상태에선 Qualifies(score > lowest)와 동일하게 동점·이하는 진입 불가 → 순위권 밖
        if (data.entries.Count >= MAX_ENTRIES && score <= lowest) return 0;
        int rank = atOrAbove + 1;
        return rank <= MAX_ENTRIES ? rank : 0;
    }

    /// <summary>
    /// 전체 기록 수
    /// </summary>
    public static int Count
    {
        get { return Load().entries.Count; }
    }

    /// <summary>
    /// 원격 fetch 성공 시 로컬 캐시를 서버 스냅샷으로 덮어쓰기(write-through).
    /// 빈/null 입력은 no-op(캐시 보존) — 일시적 빈 응답이 기존 기록을 지우지 않게.
    /// </summary>
    public static void WriteThrough(List<LeaderboardEntry> remote)
    {
        if (remote == null || remote.Count == 0) return;   // 보존: 빈 응답 → 캐시 오염 방지

        var data = new LeaderboardSaveData { entries = new List<LeaderboardEntry>(remote) };
        data.entries.Sort((a, b) => b.score.CompareTo(a.score));
        if (data.entries.Count > MAX_ENTRIES)
            data.entries.RemoveRange(MAX_ENTRIES, data.entries.Count - MAX_ENTRIES);

        try
        {
            WriteWithMac(JsonUtility.ToJson(data, true));
        }
        catch (Exception e)
        {
            Debug.LogWarning("[리더보드] 캐시 쓰기 실패: " + e.Message);
        }
    }

    /// <summary>
    /// 리더보드 초기화 (디버그용)
    /// </summary>
    public static void Clear()
    {
        if (File.Exists(FilePath))
            File.Delete(FilePath);
        if (File.Exists(MacPath))
            File.Delete(MacPath);
        Debug.Log("[리더보드] 전체 삭제");
    }
}
