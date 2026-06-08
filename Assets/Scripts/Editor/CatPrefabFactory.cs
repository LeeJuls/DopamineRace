#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Linq;
using System.Text.RegularExpressions;

/// <summary>
/// 고양이 프리팹(6종) + CatSpawnConfig.asset 생성 (에디터 전용). TrackPrefabFactory 패턴.
///
/// 각 Cat-N: SpriteRenderer(order -20) + CatController. Walk/Idle 시트의 sub-sprite를
/// 이름 끝 _정수 기준 숫자 정렬하여 프레임 배열로 주입.
/// 저장: Assets/Resources/Cats/Cat-N.prefab + Assets/Resources/CatSpawnConfig.asset
///
/// 메뉴: DopamineRace → 고양이 프리팹 생성
/// </summary>
public static class CatPrefabFactory
{
    private const string CAT_FOLDER  = "Assets/Resources/Cats";
    private const string SPRITE_ROOT = "Assets/Pet Cats pack/Sprites";
    private const string CONFIG_PATH = "Assets/Resources/CatSpawnConfig.asset";
    private const int CAT_COUNT = 6;
    private const int SORTING_ORDER = -20;

    [MenuItem("DopamineRace/고양이 프리팹 생성")]
    public static void CreateCatPrefabs()
    {
        EnsureFolders();
        for (int n = 1; n <= CAT_COUNT; n++) CreateCatPrefab(n);
        EnsureConfig();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("★ 고양이 프리팹 6종 + CatSpawnConfig 생성 완료! → " + CAT_FOLDER);
    }

    // ── 시트 프레임 로드 (이름 끝 _정수 숫자 정렬) ──
    private static Sprite[] LoadFramesSorted(string pngPath)
    {
        var objs = AssetDatabase.LoadAllAssetsAtPath(pngPath);
        if (objs == null || objs.Length == 0) return new Sprite[0];
        return objs.OfType<Sprite>()
                   .OrderBy(s => FrameIndex(s.name))
                   .ToArray();
    }

    private static int FrameIndex(string name)
    {
        var m = Regex.Match(name, @"_(\d+)$");
        return m.Success ? int.Parse(m.Groups[1].Value) : 0;
    }

    private static void CreateCatPrefab(int n)
    {
        string prefabPath = CAT_FOLDER + "/Cat-" + n + ".prefab";
        string walkPng = $"{SPRITE_ROOT}/Cat-{n}/Cat-{n}-Walk.png";
        string idlePng = $"{SPRITE_ROOT}/Cat-{n}/Cat-{n}-Idle.png";

        var walk = LoadFramesSorted(walkPng);
        var idle = LoadFramesSorted(idlePng);
        if (walk.Length == 0) Debug.LogWarning("  Walk 시트 sub-sprite 없음: " + walkPng);

        var root = new GameObject("Cat-" + n);
        var sr = root.AddComponent<SpriteRenderer>();
        sr.sortingOrder = SORTING_ORDER;
        if (walk.Length > 0) sr.sprite = walk[0];

        var cc = root.AddComponent<CatController>();
        cc.SetFrames(walk, idle);

        PrefabUtility.SaveAsPrefabAsset(root, prefabPath); // 동일 경로 덮어쓰기 (중복 없음)
        Object.DestroyImmediate(root);
        Debug.Log($"  Cat-{n}: walk {walk.Length}f / idle {idle.Length}f → {prefabPath}");
    }

    // ── CatSpawnConfig.asset (없으면 생성, 있으면 스킵=weight 보존) ──
    private static void EnsureConfig()
    {
        if (AssetDatabase.LoadAssetAtPath<CatSpawnConfig>(CONFIG_PATH) != null)
        {
            Debug.Log("  CatSpawnConfig 이미 존재 — 스킵(기존 weight 보존)");
            return;
        }
        var cfg = ScriptableObject.CreateInstance<CatSpawnConfig>();
        cfg.cats = new CatSpawnConfig.CatWeight[CAT_COUNT];
        for (int i = 0; i < CAT_COUNT; i++)
            cfg.cats[i] = new CatSpawnConfig.CatWeight { catNumber = i + 1, spawnWeight = 10f };
        cfg.spawnCount = 3;
        cfg.frameInterval = 0.1f;
        AssetDatabase.CreateAsset(cfg, CONFIG_PATH);
        Debug.Log("  CatSpawnConfig.asset 생성 (6종 weight 10, spawnCount 3)");
    }

    private static void EnsureFolders()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            AssetDatabase.CreateFolder("Assets", "Resources");
        if (!AssetDatabase.IsValidFolder(CAT_FOLDER))
            AssetDatabase.CreateFolder("Assets/Resources", "Cats");
    }
}
#endif
