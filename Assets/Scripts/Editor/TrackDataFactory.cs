#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

/// <summary>
/// 트랙 ScriptableObject 5개 자동 생성
/// 메뉴: DopamineRace → 트랙 데이터 생성
/// </summary>
public static class TrackDataFactory
{
    [MenuItem("DopamineRace/트랙 데이터 5개 생성")]
    public static void CreateAllTracks()
    {
        string folder = "Assets/Resources/Data/Tracks";
        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            AssetDatabase.CreateFolder("Assets", "Resources");
        if (!AssetDatabase.IsValidFolder("Assets/Resources/Data"))
            AssetDatabase.CreateFolder("Assets/Resources", "Data");
        if (!AssetDatabase.IsValidFolder("Assets/Resources/Data/Tracks"))
            AssetDatabase.CreateFolder("Assets/Resources/Data", "Tracks");

        CreateNormal(folder);
        CreateRainy(folder);
        CreateSnow(folder);
        CreateDesert(folder);
        CreateHighland(folder);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("★ 트랙 데이터 5개 생성 완료! → " + folder);
    }

    private static void CreateNormal(string folder)
    {
        var t = ScriptableObject.CreateInstance<TrackData>();
        t.trackName = "일반";
        t.trackIcon = "🏟️";
        t.trackDescription = "표준 트랙. 모든 계수 기본값.\n순수 스탯 경쟁!";
        // 모든 값 기본 1.0 (생성자 기본값)
        SaveAsset(t, folder, "Track_Normal");
    }

    private static void CreateRainy(string folder)
    {
        var t = ScriptableObject.CreateInstance<TrackData>();
        t.trackName = "비";
        t.trackIcon = "🌧️";
        t.trackDescription = "미끄러운 노면, 수막 현상.\ncalm 높은 캐릭터 유리!";

        t.speedMultiplier = 1.0f;
        t.noiseMultiplier = 2.0f;              // calm 영향 2배
        t.fatigueMultiplier = 1.0f;
        t.collisionRangeMultiplier = 1.4f;     // 미끄러져서 충돌 범위↑
        t.collisionPenaltyMultiplier = 0.6f;   // 충돌 피해↓
        t.slingshotMultiplier = 0.7f;          // 젖은 바닥 슬링샷↓
        t.earlyBonusMultiplier = 0.6f;         // 초반 가속 둔화
        t.midBonusMultiplier = 1.0f;
        t.lateBonusMultiplier = 1.0f;
        t.powerSpeedBonus = 0f;
        t.braveSpeedBonus = 0f;
        t.luckMultiplier = 1.3f;               // 미끄러운 바닥 운 요소↑
        t.loserPenaltyDurationMultiplier = 1.0f;

        SaveAsset(t, folder, "Track_Rainy");
    }

    private static void CreateSnow(string folder)
    {
        var t = ScriptableObject.CreateInstance<TrackData>();
        t.trackName = "설산";
        t.trackIcon = "❄️";
        t.trackDescription = "빙판 구간, 체력 소모 증가.\nendurance 높은 캐릭터 유리!\n충돌 = 고위험 고보상";

        t.speedMultiplier = 0.9f;              // 전체 속도↓
        t.noiseMultiplier = 1.0f;
        t.fatigueMultiplier = 1.8f;            // 체력 급소모
        t.collisionRangeMultiplier = 1.0f;
        t.collisionPenaltyMultiplier = 1.5f;   // 충돌 피해↑↑
        t.slingshotMultiplier = 1.6f;          // 빙판 슬링샷↑↑
        t.earlyBonusMultiplier = 1.0f;
        t.midBonusMultiplier = 1.0f;
        t.lateBonusMultiplier = 1.0f;
        t.powerSpeedBonus = 0f;
        t.braveSpeedBonus = 0f;
        t.luckMultiplier = 0.7f;               // 빙판은 실력 중요
        t.loserPenaltyDurationMultiplier = 1.0f;

        SaveAsset(t, folder, "Track_Snow");
    }

    private static void CreateDesert(string folder)
    {
        var t = ScriptableObject.CreateInstance<TrackData>();
        t.trackName = "사막";
        t.trackIcon = "🏜️";
        t.trackDescription = "모래 저항, 열사병, 모래바람.\npower 높은 캐릭터 유리!";

        t.speedMultiplier = 1.0f;
        t.noiseMultiplier = 1.6f;              // 모래바람 변동↑
        t.fatigueMultiplier = 1.4f;            // 열사병
        t.collisionRangeMultiplier = 1.0f;
        t.collisionPenaltyMultiplier = 1.0f;
        t.slingshotMultiplier = 1.0f;
        t.earlyBonusMultiplier = 1.0f;
        t.midBonusMultiplier = 1.0f;
        t.lateBonusMultiplier = 1.0f;
        t.powerSpeedBonus = 0.15f;             // power→속도 변환
        t.braveSpeedBonus = 0f;
        t.luckMultiplier = 1.2f;               // 모래바람 변수↑
        t.loserPenaltyDurationMultiplier = 1.6f; // 모래 파묻힘

        SaveAsset(t, folder, "Track_Desert");
    }

    private static void CreateHighland(string folder)
    {
        var t = ScriptableObject.CreateInstance<TrackData>();
        t.trackName = "고산지대";
        t.trackIcon = "🏔️";
        t.trackDescription = "산소 부족, 급경사 오르막.\nbrave 높은 캐릭터 유리!\n충돌 적음, 의지력 레이스";

        t.speedMultiplier = 0.85f;             // 산소 부족
        t.noiseMultiplier = 1.0f;
        t.fatigueMultiplier = 1.0f;
        t.collisionRangeMultiplier = 0.7f;     // 얇은 공기 충돌↓
        t.collisionPenaltyMultiplier = 0.7f;   // 충돌 피해↓
        t.slingshotMultiplier = 1.0f;
        t.earlyBonusMultiplier = 1.0f;
        t.midBonusMultiplier = 1.0f;
        t.lateBonusMultiplier = 1.0f;
        t.powerSpeedBonus = 0f;
        t.braveSpeedBonus = 0.12f;             // brave→속도 변환
        t.luckMultiplier = 0.8f;               // 의지력 레이스, 운 < brave
        t.hasMidSlowZone = true;               // 오르막 구간
        t.midSlowZoneStart = 0.4f;
        t.midSlowZoneEnd = 0.6f;
        t.midSlowZoneSpeedMultiplier = 0.8f;
        t.loserPenaltyDurationMultiplier = 1.0f;

        SaveAsset(t, folder, "Track_Highland");
    }

    private static void SaveAsset(TrackData t, string folder, string name)
    {
        string path = folder + "/" + name + ".asset";
        // 이미 있으면 덮어쓰지 않음
        if (AssetDatabase.LoadAssetAtPath<TrackData>(path) != null)
        {
            Debug.Log("  이미 존재: " + path + " (스킵)");
            return;
        }
        AssetDatabase.CreateAsset(t, path);
        Debug.Log("  생성: " + path);
    }
}
#endif