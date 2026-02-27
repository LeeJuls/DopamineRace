using UnityEngine;

/// <summary>
/// 트랙 타입 (기본/더트/설산/우천)
/// </summary>
public enum TrackType
{
    E_Base,  // 기본
    E_Dirt,  // 더트
    E_Snow,  // 설산
    E_Rain   // 우천
}

/// <summary>
/// TrackType → StringTable UID 변환 헬퍼
/// 이유: 하드코딩된 if/else 분기 대신 enum 기반 매핑으로 확장성 확보
/// </summary>
public static class TrackTypeUtil
{
    public static string GetTrackTypeKey(TrackType type) => type switch
    {
        TrackType.E_Base => "str.ui.track.type_base",
        TrackType.E_Dirt => "str.ui.track.type_dirt",
        TrackType.E_Snow => "str.ui.track.type_snow",
        TrackType.E_Rain => "str.ui.track.type_rain",
        _ => "str.ui.track.type_base"
    };
}

/// <summary>
/// 트랙 지형 데이터 (ScriptableObject)
/// Inspector에서 실시간 편집 가능, Play 중 수정 시 즉시 반영
///
/// 생성: Project 우클릭 → Create → DopamineRace → TrackData
/// 위치: Assets/Resources/Data/Tracks/
/// </summary>
[CreateAssetMenu(fileName = "Track_New", menuName = "DopamineRace/TrackData")]
public class TrackData : ScriptableObject
{
    [Header("═══ 기본 정보 ═══")]
    [Tooltip("트랙 이름 (UI 표시용)")]
    public string trackName = "일반";

    [Tooltip("트랙 타입 (기본/더트)")]
    public TrackType trackType = TrackType.E_Base;

    [Tooltip("트랙 아이콘/이모지 (UI 표시용)")]
    public string trackIcon = "🏟️";

    [Tooltip("트랙 설명 (배팅 화면에서 유저에게 표시)")]
    [TextArea(2, 4)]
    public string trackDescription = "표준 트랙. 모든 계수 기본값.";

    [Header("═══ 전체 속도 ═══")]
    [Tooltip("전체 기본 속도 배율 (1.0 = 기준)")]
    [Range(0.5f, 1.5f)]
    public float speedMultiplier = 1.0f;

    [Header("═══ calm → 속도 변동 ═══")]
    [Tooltip("속도 noise 배율 (높을수록 calm 낮은 캐릭터에게 불리)")]
    [Range(0.5f, 3.0f)]
    public float noiseMultiplier = 1.0f;

    [Header("═══ endurance → 피로 ═══")]
    [Tooltip("피로 배율 (높을수록 endurance 낮은 캐릭터에게 불리)")]
    [Range(0.5f, 3.0f)]
    public float fatigueMultiplier = 1.0f;

    [Header("═══ 충돌 관련 ═══")]
    [Tooltip("충돌 판정 거리 배율")]
    [Range(0.3f, 2.0f)]
    public float collisionRangeMultiplier = 1.0f;

    [Tooltip("충돌 감속 배율 (높을수록 충돌 피해 큼)")]
    [Range(0.3f, 2.0f)]
    public float collisionPenaltyMultiplier = 1.0f;

    [Header("═══ 슬링샷 ═══")]
    [Tooltip("슬링샷 효과 배율 (높을수록 충돌 보상 큼)")]
    [Range(0.3f, 2.0f)]
    public float slingshotMultiplier = 1.0f;

    [Header("═══ 타입 보너스 ═══")]
    [Tooltip("전반 구간 타입보너스 배율 (Runner에게 영향 큼)")]
    [Range(0.0f, 2.0f)]
    public float earlyBonusMultiplier = 1.0f;

    [Tooltip("중반 구간 타입보너스 배율")]
    [Range(0.0f, 2.0f)]
    public float midBonusMultiplier = 1.0f;

    [Tooltip("후반 구간 타입보너스 배율 (Reckoner에게 영향 큼)")]
    [Range(0.0f, 2.0f)]
    public float lateBonusMultiplier = 1.0f;

    [Header("═══ 특수 스탯 → 속도 변환 ═══")]
    [Tooltip("power가 속도에 직접 기여하는 정도 (사막: 모래 저항)")]
    [Range(0f, 0.3f)]
    public float powerSpeedBonus = 0f;

    [Tooltip("brave가 속도에 직접 기여하는 정도 (고산: 의지력)")]
    [Range(0f, 0.3f)]
    public float braveSpeedBonus = 0f;

    [Header("═══ 운 (Luck) ═══")]
    [Tooltip("luck 효과 배율 (크리티컬 확률 + 충돌 회피에 곱해짐)")]
    [Range(0.3f, 2.0f)]
    public float luckMultiplier = 1.0f;

    [Header("═══ 특수 구간 ═══")]
    [Tooltip("중반 감속 구간 사용 여부 (고산: 오르막)")]
    public bool hasMidSlowZone = false;

    [Tooltip("감속 구간 시작 (진행률 0~1)")]
    [Range(0f, 1f)]
    public float midSlowZoneStart = 0.4f;

    [Tooltip("감속 구간 끝 (진행률 0~1)")]
    [Range(0f, 1f)]
    public float midSlowZoneEnd = 0.6f;

    [Tooltip("감속 구간 속도 배율")]
    [Range(0.5f, 1.0f)]
    public float midSlowZoneSpeedMultiplier = 0.8f;

    [Header("═══ 충돌 후 추가 효과 ═══")]
    [Tooltip("패자 감속 지속시간 배율 (사막: 모래 파묻힘)")]
    [Range(0.5f, 2.0f)]
    public float loserPenaltyDurationMultiplier = 1.0f;
}