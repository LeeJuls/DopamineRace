using UnityEngine;

/// <summary>
/// 트랙 배경 표시 관리
/// 
/// 로드 우선순위:
///   1) TrackInfo.bgPrefabPath → 프리팹 Instantiate (애니 프랍 확장 가능)
///   2) 프리팹 없으면 → Tracks/bg_{trackId} 이미지 직접 로드
///   3) 이미지도 없으면 → ground_bg fallback
///   
/// 사용:
///   TrackVisualizer.Instance.LoadTrack(trackInfo)  → 배경 교체
///   TrackVisualizer.Instance.UnloadTrack()          → 배경 제거
/// </summary>
public class TrackVisualizer : MonoBehaviour
{
    public static TrackVisualizer Instance { get; private set; }

    // 현재 배경 오브젝트
    private GameObject currentBG;

    // 기본 배경 리소스 (fallback용)
    private const string FALLBACK_IMAGE = "ground_bg";

    // 배경 설정
    private const float BG_PPU = 70f;
    private const int BG_SORTING_ORDER = -100;
    private static readonly Vector3 BG_POSITION = new Vector3(2.5f, 0f, 0f);

    private void Awake()
    {
        if (Instance != null) { return; }
        Instance = this;
    }

    private void Start()
    {
        // 아직 트랙이 로드 안 됐으면 fallback 배경 표시
        if (currentBG == null)
        {
            LoadFallbackBackground();
        }
    }

    // ══════════════════════════════════════
    //  트랙 배경 로드 (외부 호출용)
    // ══════════════════════════════════════

    /// <summary>
    /// TrackInfo 기반으로 배경 교체
    /// </summary>
    public void LoadTrack(TrackInfo trackInfo)
    {
        if (trackInfo == null)
        {
            Debug.LogWarning("[TrackVisualizer] trackInfo null → fallback");
            LoadFallbackBackground();
            return;
        }

        // 기존 배경 제거
        UnloadTrack();

        // ① 프리팹 로드 시도
        if (!string.IsNullOrEmpty(trackInfo.bgPrefabPath))
        {
            GameObject prefab = Resources.Load<GameObject>(trackInfo.bgPrefabPath);
            if (prefab != null)
            {
                currentBG = Instantiate(prefab, transform);
                currentBG.name = "BG_Track_" + trackInfo.trackId;
                currentBG.transform.position = BG_POSITION;
                Debug.Log("[TrackVisualizer] 프리팹 로드: " + trackInfo.bgPrefabPath);
                return;
            }
            else
            {
                Debug.LogWarning("[TrackVisualizer] 프리팹 없음: " + trackInfo.bgPrefabPath + " → 이미지 시도");
            }
        }

        // ② 이미지 직접 로드 시도
        string imagePath = "Tracks/bg_" + trackInfo.trackId;
        if (LoadBackgroundImage(imagePath, "BG_Track_" + trackInfo.trackId))
        {
            Debug.Log("[TrackVisualizer] 이미지 로드: " + imagePath);
            return;
        }

        // ③ fallback
        Debug.LogWarning("[TrackVisualizer] 이미지/프리팹 없음 → fallback");
        LoadFallbackBackground();
    }

    /// <summary>
    /// 현재 배경 제거
    /// </summary>
    public void UnloadTrack()
    {
        if (currentBG != null)
        {
            Destroy(currentBG);
            currentBG = null;
        }
    }

    // ══════════════════════════════════════
    //  내부 로드 메서드
    // ══════════════════════════════════════

    /// <summary>
    /// fallback 배경 (기존 ground_bg)
    /// </summary>
    private void LoadFallbackBackground()
    {
        UnloadTrack();

        if (!LoadBackgroundImage("Tracks/bg_normal", "BG_Track_fallback"))
        {
            LoadBackgroundImage(FALLBACK_IMAGE, "BG_Track_fallback");
        }
    }

    /// <summary>
    /// Resources에서 이미지 로드 → SpriteRenderer 생성
    /// </summary>
    private bool LoadBackgroundImage(string resourcePath, string objName)
    {
        Texture2D tex = Resources.Load<Texture2D>(resourcePath);
        if (tex == null) return false;

        tex.filterMode = FilterMode.Point;

        currentBG = new GameObject(objName);
        currentBG.transform.SetParent(transform);

        SpriteRenderer sr = currentBG.AddComponent<SpriteRenderer>();
        sr.sprite = Sprite.Create(tex,
            new Rect(0, 0, tex.width, tex.height),
            new Vector2(0.5f, 0.5f), BG_PPU);
        sr.sortingOrder = BG_SORTING_ORDER;

        currentBG.transform.position = BG_POSITION;
        return true;
    }

    // ══════════════════════════════════════
    //  상태 확인
    // ══════════════════════════════════════

    /// <summary>배경이 로드되어 있는지</summary>
    public bool HasBackground => currentBG != null;
}