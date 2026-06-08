using UnityEngine;

/// <summary>
/// 고양이 등장확률 설정 (ScriptableObject). 디자이너가 Inspector에서 종별 가중치·마리수 조정.
/// 로드: Resources.Load&lt;CatSpawnConfig&gt;("CatSpawnConfig") + Instance 싱글턴 (GameSettings 패턴).
/// 에셋: Assets/Resources/CatSpawnConfig.asset (CatPrefabFactory가 없으면 생성).
/// </summary>
[CreateAssetMenu(fileName = "CatSpawnConfig", menuName = "DopamineRace/CatSpawnConfig")]
public class CatSpawnConfig : ScriptableObject
{
    [System.Serializable]
    public class CatWeight
    {
        [Tooltip("Cat-N 프리팹 번호 (1~6)")]
        public int catNumber = 1;
        [Range(0f, 100f)]
        [Tooltip("상대 등장 가중치. 0이면 미등장.")]
        public float spawnWeight = 10f;
    }

    [Header("═══ 고양이 종별 등장확률 ═══")]
    public CatWeight[] cats;

    [Header("═══ 동시 등장 마리수 ═══")]
    [Range(0, 6)]
    public int spawnCount = 3;

    [Header("═══ 애니 프레임 간격(초) ═══")]
    [Range(0.02f, 1f)]
    public float frameInterval = 0.1f;

    // 고양이 크기 배율(catScale)은 GameSettings.catScale로 이동.

    // ── 싱글턴 로드 ──
    private static CatSpawnConfig _instance;
    public static CatSpawnConfig Instance
    {
        get
        {
            if (_instance == null) _instance = Resources.Load<CatSpawnConfig>("CatSpawnConfig");
            return _instance; // null 가능 → 호출측 no-op
        }
    }

    /// <summary>가중치 기반 비복원으로 서로 다른 n종의 catNumber 배열 반환. 가중0 제외.</summary>
    public int[] PickWeightedDistinct(int n)
    {
        if (cats == null || cats.Length == 0) return new int[0];
        float[] w = new float[cats.Length];
        for (int i = 0; i < cats.Length; i++)
            w[i] = (cats[i] != null) ? Mathf.Max(0f, cats[i].spawnWeight) : 0f;

        var idxs = WeightedRandomHelper.PickDistinct(w, n);
        var result = new int[idxs.Count];
        for (int i = 0; i < idxs.Count; i++)
            result[i] = cats[idxs[i]].catNumber;
        return result;
    }

    private void OnValidate()
    {
        if (cats == null) return;
        int valid = 0;
        foreach (var c in cats)
            if (c != null && c.spawnWeight > 0f) valid++;
        if (valid < spawnCount)
            Debug.LogWarning($"[CatSpawnConfig] 유효 종({valid}) < spawnCount({spawnCount}) — 가능한 만큼만 등장");
    }
}
