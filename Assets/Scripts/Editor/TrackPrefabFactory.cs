#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

/// <summary>
/// 트랙 프리팹 생성 팩토리 (에디터 전용)
/// 
/// 트랙 프리팹 구조:
///   Track_XXX (Prefab Root)
///     ├── Background       → SpriteRenderer (배경 이미지)
///     ├── Props            → 소품 그룹 (애니메이션 가능)
///     │   ├── Cloud_1      → 구름 이동 애니메이션
///     │   ├── Cloud_2
///     │   └── ...
///     ├── ForegroundProps   → 전경 소품 (캐릭터 위에 표시)
///     └── Effects          → 파티클/셰이더 효과
/// 
/// 프리팹 저장 위치: Assets/Resources/TrackPrefabs/
/// 
/// 사용: 메뉴 → DopamineRace → 트랙 프리팹 생성
/// </summary>
public static class TrackPrefabFactory
{
    private const string PREFAB_FOLDER = "Assets/Resources/TrackPrefabs";
    private const float BG_PPU = 70f;
    private const int BG_SORTING_ORDER = -100;

    [MenuItem("DopamineRace/트랙 프리팹 생성/전체 생성 (5종)")]
    public static void CreateAllTrackPrefabs()
    {
        EnsureFolder();

        CreateTrackPrefab("Track_Normal",  "일반",    "Tracks/bg_normal");
        CreateTrackPrefab("Track_Rainy",   "비",      "Tracks/bg_rainy");
        CreateTrackPrefab("Track_Snow",    "설산",    "Tracks/bg_snow");
        CreateTrackPrefab("Track_Desert",  "사막",    "Tracks/bg_desert");
        CreateTrackPrefab("Track_Highland","고산지대", "Tracks/bg_highland");

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("★ 트랙 프리팹 5종 생성 완료! → " + PREFAB_FOLDER);
    }

    [MenuItem("DopamineRace/트랙 프리팹 생성/일반 트랙")]
    public static void CreateNormal() { EnsureFolder(); CreateTrackPrefab("Track_Normal", "일반", "Tracks/bg_normal"); Finish(); }

    [MenuItem("DopamineRace/트랙 프리팹 생성/비 트랙")]
    public static void CreateRainy() { EnsureFolder(); CreateTrackPrefab("Track_Rainy", "비", "Tracks/bg_rainy"); Finish(); }

    [MenuItem("DopamineRace/트랙 프리팹 생성/설산 트랙")]
    public static void CreateSnow() { EnsureFolder(); CreateTrackPrefab("Track_Snow", "설산", "Tracks/bg_snow"); Finish(); }

    [MenuItem("DopamineRace/트랙 프리팹 생성/사막 트랙")]
    public static void CreateDesert() { EnsureFolder(); CreateTrackPrefab("Track_Desert", "사막", "Tracks/bg_desert"); Finish(); }

    [MenuItem("DopamineRace/트랙 프리팹 생성/고산 트랙")]
    public static void CreateHighland() { EnsureFolder(); CreateTrackPrefab("Track_Highland", "고산지대", "Tracks/bg_highland"); Finish(); }

    // ══════════════════════════════════════
    //  프리팹 생성
    // ══════════════════════════════════════

    private static void CreateTrackPrefab(string prefabName, string trackLabel, string bgImagePath)
    {
        string prefabPath = PREFAB_FOLDER + "/" + prefabName + ".prefab";

        // 이미 존재하면 스킵
        if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null)
        {
            Debug.Log("  이미 존재: " + prefabPath + " (스킵)");
            return;
        }

        // ── 루트 오브젝트 ──
        GameObject root = new GameObject(prefabName);

        // ── Background ──
        GameObject bgObj = new GameObject("Background");
        bgObj.transform.SetParent(root.transform);
        bgObj.transform.localPosition = Vector3.zero;

        SpriteRenderer bgSR = bgObj.AddComponent<SpriteRenderer>();
        bgSR.sortingOrder = BG_SORTING_ORDER;

        // 배경 이미지 로드 시도
        Texture2D bgTex = Resources.Load<Texture2D>(bgImagePath);
        if (bgTex != null)
        {
            bgTex.filterMode = FilterMode.Point;
            bgSR.sprite = Sprite.Create(bgTex,
                new Rect(0, 0, bgTex.width, bgTex.height),
                new Vector2(0.5f, 0.5f), BG_PPU);
        }
        else
        {
            Debug.LogWarning("  배경 이미지 없음: " + bgImagePath + " (나중에 추가 가능)");
        }

        // ── Props (소품 그룹 - 애니메이션 대상) ──
        GameObject propsObj = new GameObject("Props");
        propsObj.transform.SetParent(root.transform);
        propsObj.transform.localPosition = Vector3.zero;

        // ── ForegroundProps (전경 소품 - 캐릭터 위에 표시) ──
        GameObject fgProps = new GameObject("ForegroundProps");
        fgProps.transform.SetParent(root.transform);
        fgProps.transform.localPosition = Vector3.zero;

        // ── Effects (파티클/셰이더) ──
        GameObject effectsObj = new GameObject("Effects");
        effectsObj.transform.SetParent(root.transform);
        effectsObj.transform.localPosition = Vector3.zero;

        // ── 프리팹 저장 ──
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        Object.DestroyImmediate(root);

        Debug.Log("  생성: " + prefabPath + " (" + trackLabel + ")");
    }

    // ══════════════════════════════════════
    //  유틸
    // ══════════════════════════════════════

    private static void EnsureFolder()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            AssetDatabase.CreateFolder("Assets", "Resources");
        if (!AssetDatabase.IsValidFolder("Assets/Resources/TrackPrefabs"))
            AssetDatabase.CreateFolder("Assets/Resources", "TrackPrefabs");
    }

    private static void Finish()
    {
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }
}
#endif