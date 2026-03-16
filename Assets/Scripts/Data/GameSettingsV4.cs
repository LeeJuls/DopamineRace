using UnityEngine;

/// <summary>
/// Race V4 설정 — 5대 스탯(Speed/Accel/Stamina/Power/Intelligence) + Luck 기반 완전 새로운 달리기 시스템
/// GameSettings.v4Settings 슬롯에 연결하면 V4 시스템 활성화 (V4 > V3 > Legacy 우선순위)
///
/// ※ V4는 V1/V3 달리기 파라미터를 일절 사용하지 않는다.
///   공유 설정: globalSpeedMultiplier(배속)만 GameSettings에서 참조.
/// </summary>
[CreateAssetMenu(fileName = "GameSettingsV4", menuName = "DopamineRace/GameSettingsV4")]
public class GameSettingsV4 : ScriptableObject
{
    // ═══════════════════════════════════════════════
    //  레이스 페이즈 구간 (OverallProgress 0~1 기준)
    // ═══════════════════════════════════════════════
    [Header("═══ 레이스 페이즈 구간 ═══")]
    [Tooltip("자리잡기(Positioning) 종료 시점\n예: 0.15 = 전체 진행도 15%까지 포지션 형성")]
    [Range(0.05f, 0.30f)]
    public float positioningEndRatio = 0.15f;

    [Tooltip("스퍼트 시작 기준 — 마지막 바퀴 진입 후 트랙의 X% 남으면 스퍼트 트리거\n도주는 레이스 시작부터 스퍼트 상태")]
    [Range(0.20f, 0.60f)]
    public float spurtTriggerLapRemain = 0.40f;

    // ═══════════════════════════════════════════════
    //  스태미나 (Stamina)
    // ═══════════════════════════════════════════════
    [Header("═══ 스태미나 ═══")]
    [Tooltip("스태미나 스탯 기본값 (Stamina=0일 때 기준 HP)")]
    public float v4_staminaBase = 80f;

    [Tooltip("스태미나 스탯 1당 추가 HP")]
    public float v4_staminaPerStat = 3f;

    [Tooltip("스태미나 소모율 (Vmax 주행 시 초당 maxHP의 몇 %를 소모하는가)\n" +
             "예: 0.025 = Vmax로 30초 달리면 maxHP의 75% 소모\n" +
             "drain = maxHP × drainBaseRate × (currentSpeed / globalSpeedMultiplier)")]
    public float v4_drainBaseRate = 0.025f;

    [Tooltip("슬립스트림(앞 캐릭터 뒤) 효과: 드레인 감소 배율\n예: 0.7 = 30% 절약")]
    [Range(0.5f, 1.0f)]
    public float v4_slipstreamDrainMul = 0.70f;

    [Tooltip("스태미나 0 도달 시 최고속도 강제 감소 배율\n예: 0.50 = 최고속의 50%로 제한")]
    [Range(0.3f, 0.7f)]
    public float v4_exhaustSpeedFloor = 0.50f;

    [Tooltip("스태미나 고갈 후 추가 감속 (초당 속도 감소량)")]
    public float v4_exhaustDecel = 2.0f;

    // ═══════════════════════════════════════════════
    //  최고속도 (Speed)
    // ═══════════════════════════════════════════════
    [Header("═══ 최고속도 (Speed 스탯) ═══")]
    [Tooltip("Speed 스탯 → Vmax 변환 계수\nVmax = 1.0 + Speed × v4_speedStatFactor")]
    public float v4_speedStatFactor = 0.02f;

    [Tooltip("스퍼트 진입 시 Vmax 상승 배율\n예: 1.20 = 스퍼트 중 20% 더 빠름")]
    [Range(1.0f, 1.5f)]
    public float v4_spurtVmaxBonus = 1.20f;

    // ═══════════════════════════════════════════════
    //  가속도 (Accel)
    // ═══════════════════════════════════════════════
    [Header("═══ 가속도 (Accel 스탯) ═══")]
    [Tooltip("Accel 스탯 → Lerp 가중치 변환\ncurrentSpeed = Lerp(cur, target, Accel × v4_accelStatFactor × dt)")]
    public float v4_accelStatFactor = 0.05f;

    [Tooltip("스퍼트 진입 시 Accel 배율 (추입/선입의 폭발적 가속을 위해)")]
    [Range(1.0f, 3.0f)]
    public float v4_spurtAccelBonus = 2.0f;

    // ═══════════════════════════════════════════════
    //  파워 (Power) — 충돌 & 코너링
    // ═══════════════════════════════════════════════
    [Header("═══ 파워 (Power 스탯) ═══")]
    [Tooltip("충돌 기본 속도 감소 페널티 (상대 비교 방식)\nV_drop = basePenalty × (1 - myPower/(myPower+opponentPower))")]
    public float v4_collisionBasePenalty = 0.35f;

    [Tooltip("코너 원심력 기본 강도 (파워 낮을수록 아웃코스로 밀림)")]
    public float v4_cornerDriftBase = 0.15f;

    [Tooltip("파워 스탯 → 코너 원심력 저항 변환\ndriftResist = Power × v4_cornerDriftResistFactor")]
    public float v4_cornerDriftResistFactor = 0.007f;

    // ═══════════════════════════════════════════════
    //  지능 (Intelligence) — AI 판단력
    // ═══════════════════════════════════════════════
    [Header("═══ 지능 (Intelligence 스탯) ═══")]

    [Header("  [사전 회피 확률 P_smart = min(MaxLimit, Int/(Int+K))]")]
    [Tooltip("P_smart 공식의 K 상수\nK가 클수록 높은 확률을 달성하려면 더 많은 지능이 필요")]
    public float v4_smartK = 10f;

    [Tooltip("P_smart 최대 상한 (지능 만점이어도 이 확률을 넘지 않음)\n예: 0.92 = 8%의 실수 확률 유지")]
    [Range(0.5f, 1.0f)]
    public float v4_smartMaxLimit = 0.92f;

    [Header("  [오버페이스 발동 확률 P_panic = P_base × (1 - Int/IntMax)]")]
    [Tooltip("기본 오버페이스 발동 확률 (지능 0일 때)")]
    [Range(0f, 1f)]
    public float v4_panicBase = 0.30f;

    [Tooltip("오버페이스 시 스태미나 소모 배율")]
    [Range(1.0f, 3.0f)]
    public float v4_panicDrainMul = 1.80f;

    [Tooltip("오버페이스 지속 시간 (초)")]
    public float v4_panicDuration = 2.0f;

    [Tooltip("지능 스탯 최대값 (오버페이스 확률 계산 기준)")]
    public float v4_intelligenceStatMax = 20f;

    [Header("  [판단 틱 T_tick = T_base - (Int/IntMax × T_bonus)]")]
    [Tooltip("AI 기본 판단 주기 (초) — 낮은 지능 캐릭터")]
    public float v4_thinkTickBase = 1.0f;

    [Tooltip("지능 최대치일 때 단축되는 판단 주기 (초)\n최소 tick = T_base - T_bonus")]
    public float v4_thinkTickBonus = 0.80f;

    // ═══════════════════════════════════════════════
    //  럭 (Luck)
    // ═══════════════════════════════════════════════
    [Header("═══ 럭 (Luck 스탯) ═══")]
    [Tooltip("크리티컬 판정 주기 (초)")]
    public float v4_luckCheckInterval = 3.0f;

    [Tooltip("Luck 스탯 → 크리 확률 변환 (luck × 이 값 = 확률/판정주기)")]
    public float v4_luckCritChance = 0.005f;

    [Tooltip("크리티컬 발동 시 속도 배율")]
    [Range(1.0f, 2.0f)]
    public float v4_luckCritBoost = 1.30f;

    [Tooltip("크리티컬 지속 시간 (초)")]
    public float v4_luckCritDuration = 1.5f;

    [Tooltip("Luck 스탯 → 충돌 회피 확률 변환 (luck × 이 값 = 회피 확률)")]
    public float v4_luckDodgeChance = 0.02f;

    // ═══════════════════════════════════════════════
    //  크루징 / 페이스메이커 (M2)
    // ═══════════════════════════════════════════════
    [Header("═══ 크루징 / 페이스메이커 (M2) ═══")]
    [Tooltip("페이스메이커 기준 속도 배율 (Vmax 대비)\n선행·선입의 크루징 기준점")]
    [Range(0.7f, 1.05f)]
    public float v4_pacemakerSpeedRatio = 0.95f;

    [Tooltip("도주 페이스메이커 오프셋 (음수 = 더 빠름)\n크루징 목표 = Vmax × (pacemaker + offset)")]
    public float v4_runnerOffset = -0.02f;

    [Tooltip("선행 페이스메이커 오프셋")]
    public float v4_leaderOffset = 0.0f;

    [Tooltip("선입 페이스메이커 오프셋 (약간 뒤처짐)")]
    public float v4_chaserOffset = 0.05f;

    [Tooltip("추입 페이스메이커 오프셋 (많이 뒤처짐 — 스태미나 비축)")]
    public float v4_reckonerOffset = 0.15f;

    [Header("  [도주 Phase 2 체력 안배]")]
    [Tooltip("도주 크루징 최소 속도 비율 (Vmax 대비)")]
    [Range(0.7f, 1.0f)]
    public float v4_runnerCruiseSpeedMin = 0.80f;

    [Tooltip("도주 크루징 최대 속도 비율 (Vmax 대비)")]
    [Range(0.7f, 1.0f)]
    public float v4_runnerCruiseSpeedMax = 0.90f;

    [Header("  [추입 Phase 2 스태미나 비축]")]
    [Tooltip("추입 크루징 최소 속도 비율 (Vmax 대비)")]
    [Range(0.5f, 0.9f)]
    public float v4_reckonerCruiseSpeedMin = 0.70f;

    [Tooltip("추입 크루징 최대 속도 비율 (Vmax 대비)")]
    [Range(0.5f, 0.9f)]
    public float v4_reckonerCruiseSpeedMax = 0.80f;

    [Header("  [타입별 스퍼트 트리거 — 마지막 랩 남은 비율]")]
    [Tooltip("선행 스퍼트 트리거 (마지막 랩의 이 비율 남으면 스퍼트)")]
    [Range(0.1f, 0.5f)]
    public float v4_leaderSpurtRatio = 0.20f;

    [Tooltip("선입 스퍼트 트리거")]
    [Range(0.1f, 0.5f)]
    public float v4_chaserSpurtRatio = 0.30f;

    [Tooltip("추입 스퍼트 트리거")]
    [Range(0.1f, 0.5f)]
    public float v4_reckonerSpurtRatio = 0.40f;

    // ═══════════════════════════════════════════════
    //  포지션별 목표 순위 범위
    // ═══════════════════════════════════════════════
    [Header("═══ 포지션별 목표 순위 범위 (9명 기준, 1~9) ═══")]
    [Tooltip("도주: 목표 순위 (항상 선두 추구)")]
    public int v4_runnerTargetRank = 1;

    [Tooltip("선행: 목표 순위 상한 (이보다 앞이면 감속)")]
    public int v4_leaderTargetMin = 2;
    [Tooltip("선행: 목표 순위 하한 (이보다 뒤면 가속)")]
    public int v4_leaderTargetMax = 4;

    [Tooltip("선입: 목표 순위 상한")]
    public int v4_chaserTargetMin = 4;
    [Tooltip("선입: 목표 순위 하한")]
    public int v4_chaserTargetMax = 6;

    [Tooltip("추입: 목표 순위 상한")]
    public int v4_reckonerTargetMin = 7;
    [Tooltip("추입: 목표 순위 하한")]
    public int v4_reckonerTargetMax = 9;

    // ═══════════════════════════════════════════════
    //  유틸리티 메서드
    // ═══════════════════════════════════════════════

    /// <summary>캐릭터의 최대 스태미나 (HP) 계산</summary>
    public float GetV4MaxStamina(float staminaStat)
        => v4_staminaBase + staminaStat * v4_staminaPerStat;

    /// <summary>캐릭터의 Vmax 계산 (스퍼트 미발동)</summary>
    public float GetV4Vmax(float speedStat)
        => 1.0f + speedStat * v4_speedStatFactor;

    /// <summary>캐릭터의 Vmax 계산 (스퍼트 발동)</summary>
    public float GetV4VmaxSpurt(float speedStat)
        => GetV4Vmax(speedStat) * v4_spurtVmaxBonus;

    /// <summary>
    /// 지능 기반 사전회피 확률 P_smart = min(MaxLimit, Int/(Int+K))
    /// </summary>
    public float GetV4SmartProb(float intelligenceStat)
        => Mathf.Min(v4_smartMaxLimit, intelligenceStat / (intelligenceStat + v4_smartK));

    /// <summary>
    /// 지능 기반 오버페이스 발동 확률 P_panic = P_base × (1 - Int/IntMax)
    /// </summary>
    public float GetV4PanicProb(float intelligenceStat)
        => v4_panicBase * (1f - Mathf.Clamp01(intelligenceStat / v4_intelligenceStatMax));

    /// <summary>
    /// 지능 기반 판단 틱 주기 T_tick = T_base - (Int/IntMax × T_bonus)
    /// </summary>
    public float GetV4ThinkTick(float intelligenceStat)
        => v4_thinkTickBase - (intelligenceStat / v4_intelligenceStatMax) * v4_thinkTickBonus;

    /// <summary>
    /// 파워 기반 충돌 페널티 (상대 비교 방식)
    /// V_drop = BasePenalty × (1 - myPower / (myPower + opponentPower))
    /// 양쪽 모두 피해를 받되, Power가 높은 쪽이 덜 받음.
    /// </summary>
    public float GetV4CollisionPenalty(float myPower, float opponentPower)
    {
        float total = Mathf.Max(1f, myPower + opponentPower); // 분모 0 방어
        return v4_collisionBasePenalty * (1f - myPower / total);
    }

    /// <summary>
    /// 타입별 크루징 구간 목표 순위 범위 반환 (min, max)
    /// </summary>
    public (int min, int max) GetV4TargetRankRange(CharacterType type)
    {
        switch (type)
        {
            case CharacterType.Runner:   return (1, v4_runnerTargetRank);
            case CharacterType.Leader:   return (v4_leaderTargetMin, v4_leaderTargetMax);
            case CharacterType.Chaser:   return (v4_chaserTargetMin, v4_chaserTargetMax);
            case CharacterType.Reckoner: return (v4_reckonerTargetMin, v4_reckonerTargetMax);
            default: return (4, 6);
        }
    }
}
