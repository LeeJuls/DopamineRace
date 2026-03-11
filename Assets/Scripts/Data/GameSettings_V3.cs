using UnityEngine;

/// <summary>
/// Race V3 시스템 설정 블록 — 독립 ScriptableObject.
/// GameSettings.asset의 v3Settings 슬롯에 연결하면 V3 활성화됨.
/// Speed → maxSpeed, Endurance → 스태미나 지속력, Brave → 가속도
/// 포지션 피드백(능동 전략) + 3구간 HP-속도 곡선
/// </summary>
[CreateAssetMenu(fileName = "GameSettingsV3", menuName = "DopamineRace/GameSettingsV3")]
public class GameSettingsV3 : ScriptableObject
{
    [Header("═══ Race V3: 스태미나 (Endurance → maxHP) ═══")]
    [Tooltip("endurance=0일 때 기본 maxHP")]
    public float v3_staminaBase = 50f;
    [Tooltip("endurance 1당 추가 maxHP")]
    public float v3_staminaPerEndurance = 3f;

    [Header("═══ Race V3: Speed 스탯 → 최고 속도 ═══")]
    [Tooltip("speed 1당 maxSpeedMul 증가. speed=20 → +20% maxSpeed")]
    [Range(0.001f, 0.05f)]
    public float v3_speedStatPerPoint = 0.01f;

    [Header("═══ Race V3: Brave 스탯 → 가속도 ═══")]
    [Tooltip("brave=0일 때 최고속 도달 시간 (gameDt 기준, globalSpeedMultiplier 포함)")]
    public float v3_sprintAccelTimeBase = 5.0f;
    [Tooltip("brave 1당 가속 배율 증가. brave=20, perPoint=0.05 → accelRate=2.0 → 도달 시간 절반")]
    [Range(0f, 0.2f)]
    public float v3_braveAccelPerPoint = 0.05f;
    // 개념 메모: GetBraveBonus(track)은 향후 트랙 지형 보너스로 재활용 예정 (더트 트랙 등)

    [Header("═══ Race V3: HP→속도 3구간 곡선 ═══")]
    [Tooltip("HP ≥ peakThreshold → maxSpeed 유지 (피크존)")]
    [Range(0.3f, 0.9f)]
    public float v3_peakThreshold = 0.70f;
    [Tooltip("HP ≤ crashThreshold → exhaustion floor (탈진존)")]
    [Range(0.0f, 0.4f)]
    public float v3_crashThreshold = 0.25f;
    [Tooltip("탈진 시 최저 속도 배율 (0.75 = maxSpeed의 75%)")]
    [Range(0.5f, 0.95f)]
    public float v3_exhaustFloor = 0.75f;

    [Header("═══ Race V3: 스태미나 드레인 ═══")]
    [Tooltip("기본 속도(1.0)일 때 드레인 기준값 (gameDt 당). globalSpeedMultiplier=1 기준으로 튜닝")]
    [Range(0.01f, 10f)]
    public float v3_drainBaseRate = 1.0f;
    [Tooltip("속도→드레인 지수. 높을수록 빠른 달리기의 HP 패널티 급증. (2.0=제곱, 3.0=세제곱)")]
    [Range(1.0f, 5.0f)]
    public float v3_drainExponent = 2.0f;

    [Header("═══ Race V3: 포지션 피드백 (능동 전략) ═══")]
    [Tooltip("0=1위, 1=꼴찌 기준. Runner 목표 순위 비율 (상위 20%)")]
    [Range(0f, 1f)]
    public float v3_zoneTarget_Runner   = 0.20f;
    [Tooltip("Leader 목표 순위 비율 (상위 35%)")]
    [Range(0f, 1f)]
    public float v3_zoneTarget_Leader   = 0.35f;
    [Tooltip("Chaser 목표 순위 비율 (중위 60%)")]
    [Range(0f, 1f)]
    public float v3_zoneTarget_Chaser   = 0.60f;
    [Tooltip("Reckoner 목표 순위 비율 (하위 20%)")]
    [Range(0f, 1f)]
    public float v3_zoneTarget_Reckoner = 0.80f;
    [Tooltip("목표 구역 ±허용 범위")]
    [Range(0.05f, 0.4f)]
    public float v3_zoneRange    = 0.15f;
    [Tooltip("구역보다 뒤처질 때 드레인 배율 (추격 비용)")]
    [Range(1.0f, 3.0f)]
    public float v3_zoneDrainMul = 1.5f;
    [Tooltip("구역보다 너무 앞설 때 드레인 배율 (절약)")]
    [Range(0.3f, 1.0f)]
    public float v3_zoneSaveMul  = 0.7f;

    [Header("═══ Race V3: 스프린트 타이밍 ═══")]
    [Tooltip("스프린트 0→최대 가속 시간 (gameDt, 별도 파라미터 — v3_sprintAccelTimeBase와 구분)")]
    public float v3_sprintAccelTime      = 2.5f;
    [Tooltip("전략구간 내 스프린트 발동 시점 (0=즉시, 1=마지막)")]
    [Range(0f, 1f)]
    public float v3_sprintStart_Runner   = 0.00f;
    [Range(0f, 1f)]
    public float v3_sprintStart_Leader   = 0.25f;
    [Range(0f, 1f)]
    public float v3_sprintStart_Chaser   = 0.50f;
    [Range(0f, 1f)]
    public float v3_sprintStart_Reckoner = 0.75f;

    // ──────────────────────────────────────────────────────────────
    //  헬퍼 메서드
    // ──────────────────────────────────────────────────────────────

    /// <summary>speed 스탯 → maxSpeedMultiplier (1.0 + speed × perPoint)</summary>
    public float GetV3MaxSpeedMul(int charSpeed)
        => 1f + charSpeed * v3_speedStatPerPoint;

    /// <summary>
    /// HP 비율 → 속도 배율 (3구간 곡선)
    /// 피크존(HP≥peak): 1.0, 탈진존(HP≤crash): exhaustFloor, 하락존: 선형
    /// </summary>
    public float GetV3SpeedFromHP(float hpRatio)
    {
        if (hpRatio >= v3_peakThreshold) return 1.0f;
        if (hpRatio <= v3_crashThreshold) return v3_exhaustFloor;
        float t = Mathf.InverseLerp(v3_crashThreshold, v3_peakThreshold, hpRatio);
        return Mathf.Lerp(v3_exhaustFloor, 1.0f, t);
    }

    /// <summary>타입 → 목표 포지션 비율 (0=1위, 1=꼴찌)</summary>
    public float GetV3ZoneTarget(CharacterType type) => type switch
    {
        CharacterType.Runner   => v3_zoneTarget_Runner,
        CharacterType.Leader   => v3_zoneTarget_Leader,
        CharacterType.Chaser   => v3_zoneTarget_Chaser,
        _                      => v3_zoneTarget_Reckoner,
    };

    /// <summary>타입 → 스프린트 발동 시점 (전략구간 내 진행률 0~1)</summary>
    public float GetV3SprintStart(CharacterType type) => type switch
    {
        CharacterType.Runner   => v3_sprintStart_Runner,
        CharacterType.Leader   => v3_sprintStart_Leader,
        CharacterType.Chaser   => v3_sprintStart_Chaser,
        _                      => v3_sprintStart_Reckoner,
    };
}
