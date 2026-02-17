using UnityEngine;
using System.IO;

/// <summary>
/// 트랙 웨이포인트 데이터 (JSON 파일로 저장/불러오기)
/// 
/// 저장 위치:
///   기본: Assets/StreamingAssets/track_waypoints.json
///   트랙별: Assets/StreamingAssets/track_waypoints_{trackId}.json
/// 
/// 로드 우선순위:
///   1) track_waypoints_{trackId}.json (트랙 전용)
///   2) track_waypoints.json (기본/공용)
///   3) 코드 내장 DEFAULTS
/// </summary>
[System.Serializable]
public class TrackPathData
{
    public float[] x;
    public float[] y;

    private static readonly string DEFAULT_FILE = "track_waypoints.json";

    // 기본 웨이포인트 (파일 없을 때 사용)
    private static readonly Vector2[] DEFAULTS = new Vector2[]
    {
        new Vector2(6.3f, 1.3f),
        new Vector2(4.6f, 2.1f),
        new Vector2(3.2f, 2.8f),
        new Vector2(1.6f, 3.3f),
        new Vector2(-0.1f, 3.5f),
        new Vector2(-1.5f, 3.3f),
        new Vector2(-2.6f, 2.7f),
        new Vector2(-3.4f, 1.7f),
        new Vector2(-2.9f, 0.5f),
        new Vector2(-0.3f, -1.1f),
        new Vector2(5.2f, -3.8f),
        new Vector2(7f, -4.1f),
        new Vector2(8.6f, -4f),
        new Vector2(9.9f, -3.4f),
        new Vector2(10.8f, -2.4f),
        new Vector2(10.5f, -1.1f),
        new Vector2(9.2f, -0.1f),
    };

    public int Count => (x != null) ? x.Length : 0;

    public Vector2 GetPoint(int i)
    {
        return new Vector2(x[i], y[i]);
    }

    public static TrackPathData FromPositions(Vector2[] positions)
    {
        var data = new TrackPathData();
        data.x = new float[positions.Length];
        data.y = new float[positions.Length];
        for (int i = 0; i < positions.Length; i++)
        {
            data.x[i] = Mathf.Round(positions[i].x * 10f) / 10f;
            data.y[i] = Mathf.Round(positions[i].y * 10f) / 10f;
        }
        return data;
    }

    public static TrackPathData FromTransforms(Transform[] markers)
    {
        Vector2[] positions = new Vector2[markers.Length];
        for (int i = 0; i < markers.Length; i++)
            positions[i] = new Vector2(markers[i].position.x, markers[i].position.y);
        return FromPositions(positions);
    }

    // ══════════════════════════════════════
    //  파일 경로
    // ══════════════════════════════════════

    private static string GetDirectory()
    {
        string dir = Path.Combine(Application.dataPath, "StreamingAssets");
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
        return dir;
    }

    private static string GetFilePath(string trackId = null)
    {
        string dir = GetDirectory();
        if (string.IsNullOrEmpty(trackId) || trackId == "normal")
            return Path.Combine(dir, DEFAULT_FILE);
        return Path.Combine(dir, "track_waypoints_" + trackId + ".json");
    }

    // ══════════════════════════════════════
    //  저장
    // ══════════════════════════════════════

    /// <summary>
    /// 기본 트랙으로 저장 (기존 호환)
    /// </summary>
    public void Save()
    {
        Save(null);
    }

    /// <summary>
    /// 특정 트랙용으로 저장
    /// </summary>
    public void Save(string trackId)
    {
        string path = GetFilePath(trackId);
        string json = JsonUtility.ToJson(this, true);
        File.WriteAllText(path, json);

        string label = string.IsNullOrEmpty(trackId) ? "기본" : trackId;
        Debug.Log("★ 트랙 웨이포인트 저장 완료! [" + label + "] → " + path);
    }

    // ══════════════════════════════════════
    //  로드
    // ══════════════════════════════════════

    /// <summary>
    /// 기본 트랙 로드 (기존 호환)
    /// </summary>
    public static TrackPathData Load()
    {
        return Load(null);
    }

    /// <summary>
    /// 트랙별 웨이포인트 로드
    /// 우선순위: 트랙 전용 → 기본 → 코드 내장
    /// </summary>
    public static TrackPathData Load(string trackId)
    {
        // ① 트랙 전용 파일 시도
        if (!string.IsNullOrEmpty(trackId) && trackId != "normal")
        {
            string trackPath = GetFilePath(trackId);
            if (File.Exists(trackPath))
            {
                string json = File.ReadAllText(trackPath);
                var data = JsonUtility.FromJson<TrackPathData>(json);
                if (data != null && data.Count > 0)
                {
                    Debug.Log("트랙 웨이포인트 로드 [" + trackId + "] (" + data.Count + "개)");
                    return data;
                }
            }
            Debug.Log("트랙 전용 웨이포인트 없음 [" + trackId + "] → 기본 트랙 사용");
        }

        // ② 기본 파일 시도
        string defaultPath = GetFilePath(null);
        if (File.Exists(defaultPath))
        {
            string json = File.ReadAllText(defaultPath);
            var data = JsonUtility.FromJson<TrackPathData>(json);
            if (data != null && data.Count > 0)
            {
                Debug.Log("트랙 웨이포인트 로드 [기본] (" + data.Count + "개)");
                return data;
            }
        }

        // ③ 코드 내장 기본값
        Debug.Log("트랙 파일 없음 → 코드 기본값 사용");
        return FromPositions(DEFAULTS);
    }

    /// <summary>
    /// 특정 트랙 전용 웨이포인트 파일이 있는지 확인
    /// </summary>
    public static bool HasTrackFile(string trackId)
    {
        if (string.IsNullOrEmpty(trackId)) return false;
        return File.Exists(GetFilePath(trackId));
    }
}