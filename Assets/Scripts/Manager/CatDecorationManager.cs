using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 배팅 화면 장식 고양이 스폰/상태 관리 (싱글턴, SampleScene 전용 — DontDestroyOnLoad 미부착).
/// GameState.Betting에서만 폴리곤 영역 안에 고양이 spawnCount 마리 배회, 그 외 상태는 despawn.
/// 풀링: 종(catNumber)별 1 인스턴스를 재사용(최대 6) — 라운드 반복 시 Instantiate 누적 없음.
/// </summary>
public class CatDecorationManager : MonoBehaviour
{
    public static CatDecorationManager Instance { get; private set; }

    private CatAreaData area;
    private readonly Dictionary<int, GameObject> pool = new Dictionary<int, GameObject>(); // catNumber → 인스턴스
    private bool warnedConfig, warnedArea;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        // DontDestroyOnLoad 미부착 — 씬 전환 시 함께 파괴(좀비 방지)
    }

    private void Start()
    {
        area = CatAreaData.Load();
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnStateChanged += OnStateChanged;
            // 초기 보정: 이미 Betting이면(첫 이벤트 놓침 대비) 즉시 등장
            if (GameManager.Instance.CurrentState == GameManager.GameState.Betting)
                SpawnCats();
        }
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnStateChanged -= OnStateChanged;
        if (Instance == this) Instance = null;
    }

    private void OnStateChanged(GameManager.GameState state)
    {
        if (state == GameManager.GameState.Betting) SpawnCats();
        else DespawnCats();
    }

    /// <summary>Betting 진입 시 호출. 전부 despawn 후 가중치로 spawnCount 종 재선택 (idempotent).</summary>
    private void SpawnCats()
    {
        var config = CatSpawnConfig.Instance;
        if (config == null)
        {
            if (!warnedConfig) { Debug.LogWarning("[Cat] CatSpawnConfig 없음 — 고양이 미등장"); warnedConfig = true; }
            return;
        }
        if (area == null) area = CatAreaData.Load();
        if (area == null || area.Count < 3)
        {
            if (!warnedArea) { Debug.LogWarning("[Cat] 영역 데이터 부족 — 고양이 미등장"); warnedArea = true; }
            return;
        }

        DespawnCats(); // 전부 비활성화 후 재선택 (중복 방지)

        int[] picks = config.PickWeightedDistinct(config.spawnCount);
        foreach (int n in picks)
        {
            var go = GetOrCreate(n);
            if (go == null) continue;
            float scale = (GameSettings.Instance != null) ? GameSettings.Instance.catScale : 3f;
            go.transform.localScale = Vector3.one * Mathf.Max(0.1f, scale); // 크기 배율 (GameSettings)
            var cc = go.GetComponent<CatController>();
            if (cc != null)
            {
                cc.SetFrameInterval(config.frameInterval);
                cc.SetArea(area);
            }
            go.SetActive(true);           // OnEnable → BeginWander(area)
            if (cc != null) cc.BeginWander(area);
        }
    }

    /// <summary>전 풀 비활성화 (코루틴은 CatController.OnDisable에서 정지).</summary>
    private void DespawnCats()
    {
        foreach (var kv in pool)
            if (kv.Value != null && kv.Value.activeSelf) kv.Value.SetActive(false);
    }

    private GameObject GetOrCreate(int n)
    {
        if (pool.TryGetValue(n, out var existing) && existing != null) return existing;

        var prefab = Resources.Load<GameObject>("Cats/Cat-" + n);
        if (prefab == null) { Debug.LogWarning("[Cat] 프리팹 없음: Cats/Cat-" + n); return null; }

        var go = Instantiate(prefab, transform);
        go.name = "Cat-" + n;
        go.SetActive(false);
        pool[n] = go;
        return go;
    }

    // ── 테스트/검증용 조회 ──
    public int ActiveCatCount
    {
        get
        {
            int c = 0;
            foreach (var kv in pool) if (kv.Value != null && kv.Value.activeSelf) c++;
            return c;
        }
    }
    public int PoolSize => pool.Count;
}
