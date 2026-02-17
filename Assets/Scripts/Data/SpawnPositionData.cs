using UnityEngine;
using System.IO;

/// <summary>
/// 출발 위치 데이터 (JSON 파일로 저장/불러오기)
/// 저장 위치: Assets/StreamingAssets/spawn_positions.json
/// </summary>
[System.Serializable]
public class SpawnPositionData
{
    public float[] x;
    public float[] y;

    private static readonly string FILE_NAME = "spawn_positions.json";

    // 기본 출발 위치 (파일 없을 때 사용)
    private static readonly Vector2[] DEFAULTS = new Vector2[]
    {
        new Vector2(6.5f, 1.6f),
        new Vector2(6.2f, 1.3f),
        new Vector2(5.9f, 1.0f),
        new Vector2(6.1f, 1.8f),
        new Vector2(5.8f, 1.5f),
        new Vector2(5.5f, 1.2f),
        new Vector2(5.7f, 2.0f),
        new Vector2(5.4f, 1.7f),
        new Vector2(5.1f, 1.4f),
        new Vector2(5.3f, 2.2f),
        new Vector2(5.0f, 1.9f),
        new Vector2(4.7f, 1.6f),
    };

    public int Count => (x != null) ? x.Length : 0;

    public Vector2 GetPoint(int i)
    {
        return new Vector2(x[i], y[i]);
    }

    public static SpawnPositionData FromPositions(Vector2[] positions)
    {
        var data = new SpawnPositionData();
        data.x = new float[positions.Length];
        data.y = new float[positions.Length];
        for (int i = 0; i < positions.Length; i++)
        {
            data.x[i] = Mathf.Round(positions[i].x * 10f) / 10f;
            data.y[i] = Mathf.Round(positions[i].y * 10f) / 10f;
        }
        return data;
    }

    public static SpawnPositionData FromTransforms(Transform[] markers)
    {
        Vector2[] positions = new Vector2[markers.Length];
        for (int i = 0; i < markers.Length; i++)
            positions[i] = new Vector2(markers[i].position.x, markers[i].position.y);
        return FromPositions(positions);
    }

    private static string GetFilePath()
    {
        string dir = Path.Combine(Application.dataPath, "StreamingAssets");
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
        return Path.Combine(dir, FILE_NAME);
    }

    public void Save()
    {
        string json = JsonUtility.ToJson(this, true);
        File.WriteAllText(GetFilePath(), json);
        Debug.Log("★ 출발 위치 저장 완료! → " + GetFilePath());
    }

    public static SpawnPositionData Load()
    {
        string path = GetFilePath();
        if (File.Exists(path))
        {
            string json = File.ReadAllText(path);
            var data = JsonUtility.FromJson<SpawnPositionData>(json);
            if (data != null && data.Count > 0)
            {
                Debug.Log("출발 위치 로드 완료 (" + data.Count + "개)");
                return data;
            }
        }

        Debug.Log("출발 위치 파일 없음 → 기본값 사용");
        return FromPositions(DEFAULTS);
    }
}
