using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 트랙 CSV 로드 + 트랙 선택 관리
/// Resources/Data/TrackDB.csv 에서 로드
/// 
/// 선택 규칙:
///   Round 1 → 무조건 기본 트랙 (normal)
///   Round 2+ → 전판 제외 + weight 가중 랜덤
/// 
/// 사용:
///   TrackDatabase.Instance.GetDefaultTrack()
///   TrackDatabase.Instance.SelectNextTrack("normal")
/// </summary>
public class TrackDatabase : MonoBehaviour
{
    public static TrackDatabase Instance { get; private set; }

    [Header("CSV 파일 (Resources 내 확장자 제외)")]
    public string csvPath = "Data/TrackDB";

    [Header("기본 트랙 ID")]
    public string defaultTrackId = "normal";

    private List<TrackInfo> allTracks = new List<TrackInfo>();
    private Dictionary<string, TrackInfo> trackMap = new Dictionary<string, TrackInfo>();

    // 현재 적용 중인 트랙 (런타임)
    private TrackInfo currentTrackInfo;
    private TrackData currentTrackDataInstance;

    // 이전 트랙 ID (다음 라운드에서 제외용)
    private string previousTrackId = "";

    /// <summary>전체 트랙 목록</summary>
    public List<TrackInfo> AllTracks => allTracks;

    /// <summary>전체 트랙 수</summary>
    public int TotalCount => allTracks.Count;

    /// <summary>현재 적용된 트랙</summary>
    public TrackInfo CurrentTrackInfo => currentTrackInfo;

    /// <summary>이전 트랙 ID</summary>
    public string PreviousTrackId => previousTrackId;

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        LoadCSV();
    }

    // ══════════════════════════════════════
    //  CSV 로드
    // ══════════════════════════════════════

    /// <summary>
    /// CSV 파일 로드
    /// </summary>
    public void LoadCSV()
    {
        allTracks.Clear();
        trackMap.Clear();

        TextAsset csv = Resources.Load<TextAsset>(csvPath);
        if (csv == null)
        {
            Debug.LogError("[TrackDB] CSV 파일 없음: Resources/" + csvPath + ".csv → 기본 트랙으로 fallback");
            CreateFallbackTrack();
            return;
        }

        string[] lines = csv.text.Split('\n');
        int loaded = 0;

        for (int i = 1; i < lines.Length; i++) // 0번 = 헤더 스킵
        {
            string line = lines[i].Trim();
            if (string.IsNullOrEmpty(line)) continue;

            TrackInfo info = TrackInfo.ParseCSVLine(line);
            if (info != null)
            {
                allTracks.Add(info);
                trackMap[info.trackId] = info;
                loaded++;
            }
            else
            {
                Debug.LogWarning("[TrackDB] 파싱 실패 (line " + i + "): " + line);
            }
        }

        // 기본 트랙이 없으면 생성
        if (!trackMap.ContainsKey(defaultTrackId))
        {
            Debug.LogWarning("[TrackDB] 기본 트랙 '" + defaultTrackId + "'이 CSV에 없음 → fallback 생성");
            CreateFallbackTrack();
        }

        Debug.Log("[TrackDB] 로드 완료: " + loaded + "종 → " +
            string.Join(", ", allTracks.ConvertAll(t => t.ToString())));
    }

    private void CreateFallbackTrack()
    {
        var fallback = new TrackInfo
        {
            trackId = defaultTrackId,
            trackName = "일반",
            trackIcon = "🏟️",
            trackDescription = "기본 트랙",
            bgPrefabPath = "TrackPrefabs/Track_Normal",
            weight = 100
        };

        if (!trackMap.ContainsKey(fallback.trackId))
        {
            allTracks.Add(fallback);
            trackMap[fallback.trackId] = fallback;
        }
    }

    // ══════════════════════════════════════
    //  트랙 조회
    // ══════════════════════════════════════

    /// <summary>기본 트랙 가져오기</summary>
    public TrackInfo GetDefaultTrack()
    {
        if (trackMap.ContainsKey(defaultTrackId))
            return trackMap[defaultTrackId];

        Debug.LogError("[TrackDB] 기본 트랙 없음!");
        return allTracks.Count > 0 ? allTracks[0] : null;
    }

    /// <summary>ID로 트랙 검색</summary>
    public TrackInfo GetTrackById(string id)
    {
        if (trackMap.ContainsKey(id))
            return trackMap[id];
        return null;
    }

    // ══════════════════════════════════════
    //  트랙 선택 (가중 랜덤, 전판 제외)
    // ══════════════════════════════════════

    /// <summary>
    /// 다음 라운드 트랙 선택
    /// excludeId에 해당하는 트랙을 제외하고 weight 가중 랜덤
    /// </summary>
    public TrackInfo SelectNextTrack(string excludeId)
    {
        // 후보 수집 (excludeId 제외, weight > 0)
        List<TrackInfo> candidates = new List<TrackInfo>();
        int totalWeight = 0;

        for (int i = 0; i < allTracks.Count; i++)
        {
            var t = allTracks[i];
            if (t.trackId == excludeId) continue;
            if (t.weight <= 0) continue;

            candidates.Add(t);
            totalWeight += t.weight;
        }

        // 후보가 없으면 (트랙이 1종뿐이면) excludeId 포함해서 선택
        if (candidates.Count == 0)
        {
            Debug.LogWarning("[TrackDB] 제외 후 후보 없음 → 전체에서 선택");
            candidates = new List<TrackInfo>(allTracks);
            totalWeight = 0;
            for (int i = 0; i < candidates.Count; i++)
                totalWeight += Mathf.Max(candidates[i].weight, 1);
        }

        // 가중 랜덤 선택
        int roll = Random.Range(0, totalWeight);
        int cumulative = 0;

        for (int i = 0; i < candidates.Count; i++)
        {
            cumulative += Mathf.Max(candidates[i].weight, 1);
            if (roll < cumulative)
            {
                Debug.Log(string.Format("[TrackDB] 트랙 선택: {0} (제외: {1}, 후보: {2}종, 총 weight: {3})",
                    candidates[i], excludeId, candidates.Count, totalWeight));
                return candidates[i];
            }
        }

        // fallback (도달 불가)
        return candidates[candidates.Count - 1];
    }

    // ══════════════════════════════════════
    //  트랙 적용 (GameSettings 연동)
    // ══════════════════════════════════════

    /// <summary>
    /// 선택된 트랙을 GameSettings에 적용
    /// Round 1 → 기본 트랙, Round 2+ → 가중 랜덤
    /// </summary>
    public TrackInfo ApplyTrackForRound(int roundNumber)
    {
        TrackInfo selected;

        if (roundNumber <= 1)
        {
            selected = GetDefaultTrack();
        }
        else
        {
            selected = SelectNextTrack(previousTrackId);
        }

        if (selected == null)
        {
            Debug.LogError("[TrackDB] 트랙 선택 실패!");
            return null;
        }

        // 이전 TrackData 인스턴스 정리
        if (currentTrackDataInstance != null)
        {
            Destroy(currentTrackDataInstance);
        }

        // TrackInfo → TrackData 변환 + GameSettings에 주입
        currentTrackInfo = selected;
        currentTrackDataInstance = selected.ToTrackData();
        GameSettings.Instance.currentTrack = currentTrackDataInstance;

        // 이전 트랙 ID 갱신
        previousTrackId = selected.trackId;

        Debug.Log(string.Format("[TrackDB] Round {0} 트랙 적용: {1} {2}",
            roundNumber, selected.trackIcon, selected.DisplayName));

        return selected;
    }

    /// <summary>
    /// 특정 트랙을 강제 적용 (디버그/테스트용)
    /// </summary>
    public TrackInfo ForceApplyTrack(string trackId)
    {
        TrackInfo info = GetTrackById(trackId);
        if (info == null)
        {
            Debug.LogError("[TrackDB] 트랙 없음: " + trackId);
            return null;
        }

        if (currentTrackDataInstance != null)
            Destroy(currentTrackDataInstance);

        currentTrackInfo = info;
        currentTrackDataInstance = info.ToTrackData();
        GameSettings.Instance.currentTrack = currentTrackDataInstance;
        previousTrackId = info.trackId;

        Debug.Log("[TrackDB] 강제 적용: " + info);
        return info;
    }

    /// <summary>
    /// 리셋 (새 게임 시작 시)
    /// </summary>
    public void ResetTrackHistory()
    {
        previousTrackId = "";
        currentTrackInfo = null;

        if (currentTrackDataInstance != null)
        {
            Destroy(currentTrackDataInstance);
            currentTrackDataInstance = null;
        }

        GameSettings.Instance.currentTrack = null;
        Debug.Log("[TrackDB] 트랙 히스토리 리셋");
    }
}
