using UnityEngine;
using System.IO;

/// <summary>
/// 고양이 배회 영역(폴리곤) 데이터 — JSON 저장/로드. TrackPathData 패턴 복제.
///
/// 저장 위치: Assets/StreamingAssets/cat_area.json
/// 로드 우선순위: 파일 → 코드 내장 DEFAULTS(좌하단 모래밭 근사)
///
/// 좌표 = 폴리곤 꼭짓점(월드 XY). 고양이 transform.position(발 위치) 기준 Contains 판정.
/// </summary>
[System.Serializable]
public class CatAreaData
{
    public float[] x;
    public float[] y;

    private static readonly string FILE = "cat_area.json";

    // 기본 영역 (파일 없을 때) — 화면 좌하단 사각형 근사. 에디터(C키)에서 재편집 가능.
    private static readonly Vector2[] DEFAULTS = new Vector2[]
    {
        new Vector2(-3.5f, -2.0f),   // 좌상
        new Vector2( 1.5f, -3.0f),   // 우상
        new Vector2( 2.5f, -5.2f),   // 우하
        new Vector2(-3.5f, -5.2f),   // 좌하
    };

    public int Count => (x != null) ? x.Length : 0;

    public Vector2 GetPoint(int i) => new Vector2(x[i], y[i]);

    // ══════════════════════════════════════
    //  기하 — Contains / RandomPointInside / Bounds
    // ══════════════════════════════════════

    /// <summary>ray-casting point-in-polygon. 정점 &lt; 3 이면 false.</summary>
    public bool Contains(Vector2 p)
    {
        if (x == null || y == null || x.Length < 3) return false;
        int n = x.Length;
        bool inside = false;
        for (int i = 0, j = n - 1; i < n; j = i++)
        {
            if (((y[i] > p.y) != (y[j] > p.y)) &&
                (p.x < (x[j] - x[i]) * (p.y - y[i]) / (y[j] - y[i]) + x[i]))
                inside = !inside;
        }
        return inside;
    }

    /// <summary>폴리곤 AABB.</summary>
    public Bounds GetBounds()
    {
        if (x == null || y == null || x.Length == 0)
            return new Bounds(Vector3.zero, Vector3.zero);
        float minX = x[0], maxX = x[0], minY = y[0], maxY = y[0];
        for (int i = 1; i < x.Length; i++)
        {
            if (x[i] < minX) minX = x[i]; else if (x[i] > maxX) maxX = x[i];
            if (y[i] < minY) minY = y[i]; else if (y[i] > maxY) maxY = y[i];
        }
        var center = new Vector3((minX + maxX) * 0.5f, (minY + maxY) * 0.5f, 0f);
        var size = new Vector3(maxX - minX, maxY - minY, 0f);
        return new Bounds(center, size);
    }

    /// <summary>
    /// 폴리곤 내부 랜덤 점 (bbox rejection sampling, 30회).
    /// 30회 모두 실패 시 bbox 중심(항상 bbox 내부, NaN/Infinity 없음) 반환.
    /// 오목 폴리곤에서도 합리적 면적이면 30회 내 성공 → Contains true 보장.
    /// </summary>
    public Vector2 RandomPointInside()
    {
        Bounds b = GetBounds();
        var center = new Vector2(b.center.x, b.center.y);
        if (x == null || y == null || x.Length < 3) return center; // 퇴화 → 중심(bbox 내부)

        for (int i = 0; i < 30; i++)
        {
            var p = new Vector2(
                Random.Range(b.min.x, b.max.x),
                Random.Range(b.min.y, b.max.y));
            if (Contains(p)) return p;
        }
        return center; // bbox 중심 — bbox 내부 보장, NaN 없음
    }

    // ══════════════════════════════════════
    //  변환
    // ══════════════════════════════════════

    public static CatAreaData FromPositions(Vector2[] positions)
    {
        var data = new CatAreaData();
        data.x = new float[positions.Length];
        data.y = new float[positions.Length];
        for (int i = 0; i < positions.Length; i++)
        {
            data.x[i] = Mathf.Round(positions[i].x * 10f) / 10f;
            data.y[i] = Mathf.Round(positions[i].y * 10f) / 10f;
        }
        return data;
    }

    public static CatAreaData FromTransforms(Transform[] markers)
    {
        var positions = new Vector2[markers.Length];
        for (int i = 0; i < markers.Length; i++)
            positions[i] = new Vector2(markers[i].position.x, markers[i].position.y);
        return FromPositions(positions);
    }

    // ══════════════════════════════════════
    //  파일 I/O
    // ══════════════════════════════════════

    private static string GetFilePath()
    {
        string dir = Path.Combine(Application.dataPath, "StreamingAssets");
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
        return Path.Combine(dir, FILE);
    }

    public void Save()
    {
        string path = GetFilePath();
        File.WriteAllText(path, JsonUtility.ToJson(this, true));
        Debug.Log("★ 고양이 영역 저장 완료! → " + path);
    }

    /// <summary>파일 → 없으면 코드 DEFAULTS.</summary>
    public static CatAreaData Load()
    {
        string path = GetFilePath();
        if (File.Exists(path))
        {
            var data = JsonUtility.FromJson<CatAreaData>(File.ReadAllText(path));
            if (data != null && data.Count >= 3)
            {
                Debug.Log("고양이 영역 로드 (" + data.Count + "점)");
                return data;
            }
        }
        Debug.Log("고양이 영역 파일 없음 → 코드 기본값");
        return FromPositions(DEFAULTS);
    }
}
