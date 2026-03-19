using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class HpSpeedThreshold
{
    [Tooltip("HP가 이 비율 이하일 때 적용 (0~1, 예: 0.5 = HP 50% 이하)")]
    [Range(0f, 1f)]
    public float hpRatio = 0.50f;

    [Tooltip("base 속도 감소율 (0~1, 예: 0.10 = 10% 감소 → 배율 0.90)")]
    [Range(0f, 0.9f)]
    public float speedReduction = 0.10f;
}

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

    [Tooltip("진행도 기준 HP 소모량 (레이스 길이 무관)\n" +
             "레이스 전체 진행도(0~1) 소모 시 빠져나가는 고정 HP량\n" +
             "기준: 22.4  burstMul×3.5  spurtMul×4.0 → 지구력20(HP140) 기준 ~20~40% 잔여")]
    public float v4_drainPerLap = 22.4f;

    [Tooltip("슬립스트림 감지 거리 (TotalProgress 기준)\n앞 캐릭터와의 진행도 차이가 이 값 이하면 슬립스트림 발동\n예: 0.08 = 진행도 8% 이내")]
    [Range(0.01f, 0.30f)]
    public float v4_slipstreamRange = 0.08f;

    [Tooltip("슬립스트림(앞 캐릭터 뒤) 효과: 드레인 감소 배율\n예: 0.7 = 30% 절약")]
    [Range(0.5f, 1.0f)]
    public float v4_slipstreamDrainMul = 0.70f;

    [Tooltip("슬립스트림 시 가속도(Lerp rate) 배율\n1.0=원래 가속, 0.7=70%로 부드럽게 따라감")]
    [Range(0.3f, 1.0f)]
    public float v4_slipstreamAccelMul = 0.70f;

    [Tooltip("슬립스트림 가속 발동 확률 공식의 K 상수\n" +
             "prob = Int / (Int + K)\n" +
             "K=20: 지능10→33%, 지능20→50%\n" +
             "K=10: 지능10→50%, 지능20→67%")]
    public float v4_slipstreamSmartK = 20f;

    [Tooltip("슬립스트림 가속 판정 쿨다운 (초)\n성공·실패 모두 판정 후 이 시간 동안 재판정 없음")]
    public float v4_slipstreamRollCooldown = 3f;

    [Tooltip("슬립스트림 가속 발동 성공 시 HP 드레인 추가 배율\n" +
             "기본 드레인 감소(0.70) 위에 추가 적용\n" +
             "drain×0.70×1.80 = drain×1.26 (기본 대비 26% 증가)")]
    public float v4_slipstreamAccelDrainMul = 1.80f;

    [Header("슬립스트림 해금 진행도 (0=시작부터 허용, 1=불가)")]
    [Tooltip("도주(Runner) 슬립스트림 최소 전체 진행도")]
    public float v4_ssUnlockRunner   = 0.00f;
    [Tooltip("선행(Leader) 슬립스트림 최소 전체 진행도")]
    public float v4_ssUnlockLeader   = 0.00f;
    [Tooltip("선입(Chaser) 슬립스트림 최소 전체 진행도")]
    public float v4_ssUnlockChaser   = 0.30f;
    [Tooltip("추입(Reckoner) 슬립스트림 최소 전체 진행도")]
    public float v4_ssUnlockReckoner = 0.50f;

    [Header("═══ HP 기반 속도 감소 임계값 ═══")]
    [Tooltip("HP가 지정 비율 이하일 때 base 속도 감소.\n" +
             "해당 임계값 중 hpRatio가 가장 낮은 항목 적용 (밴드 시스템).\n" +
             "예) hpRatio:0.9, reduction:0.3 → HP 90%이하 → base -30%\n" +
             "+ 버튼으로 추가, − 버튼으로 삭제")]
    public List<HpSpeedThreshold> v4_hpSpeedThresholds = new List<HpSpeedThreshold>();

    // ═══════════════════════════════════════════════
    //  최고속도 (Speed)
    // ═══════════════════════════════════════════════
    [Header("═══ 최고속도 (Speed 스탯) ═══")]
    [Tooltip("Speed 스탯 → Vmax 변환 계수\nVmax = 1.0 + Speed × v4_speedStatFactor")]
    public float v4_speedStatFactor = 0.02f;

    [Tooltip("스퍼트 진입 시 Vmax 상승 배율\n예: 1.20 = 스퍼트 중 20% 더 빠름")]
    [Range(1.0f, 1.5f)]
    public float v4_spurtVmaxBonus = 1.50f;

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

    [Tooltip("지능 스탯 10 기준 ±0, 최대(20)/최소(0) 시 부스트·크리 지속시간 ±이 값만큼 비율 변동\n" +
             "예: 0.10 → 지능20 = +10%, 지능0 = -10%")]
    public float v4_intelligenceModMax = 0.10f;

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
    //  구간제 속도 시스템 (M2 개편)
    //  전체 진행도(0~1) 기준 구간별 목표 속도
    // ═══════════════════════════════════════════════
    [Header("═══ 구간별 속도 ═══")]
    [Tooltip("기본 달리기 속도 배율 (Vmax 대비)\n부스트/최종스퍼트 외 구간의 속도")]
    [Range(0.5f, 1.0f)]
    public float v4_normalSpeedRatio = 1.0f;

    [Tooltip("부스트 구간 속도 배율 (Vmax 대비)\n각 타입 고유 구간에서 전력질주 속도")]
    [Range(0.9f, 2.5f)]
    public float v4_burstSpeedRatio = 2.0f;

    [Tooltip("최종 스퍼트 시작 지점 (전체 진행도 기준)\n예: 0.86 = 마지막 14%부터 전원 스퍼트")]
    [Range(0.5f, 0.95f)]
    public float v4_finalSpurtStart = 0.86f;

    [Tooltip("부스트 구간 HP 추가 소모 배율\n예: 1.8 = Burst 구간에서 Normal의 1.8배 HP 소모")]
    [Range(1.0f, 5.0f)]
    public float v4_burstDrainMul = 1.8f;

    [Tooltip("최종 스퍼트 구간 HP 추가 소모 배율\n예: 2.5 = Spurt 구간에서 2.5배 소모 (HP 연소)")]
    [Range(1.0f, 4.0f)]
    public float v4_spurtDrainMul = 2.5f;

    [Header("  [타입별 부스트 구간 — 전체 진행도 0~1]")]
    [Tooltip("도주 부스트 시작 (0~5% 워밍업 후)")]
    [Range(0f, 0.5f)]
    public float v4_runnerBurstStart = 0.06f;
    [Tooltip("도주 부스트 종료 (테스트: HP 소진까지 풀버스트)")]
    [Range(0.1f, 1.0f)]
    public float v4_runnerBurstEnd = 0.86f;

    [Tooltip("선행 부스트 시작")]
    [Range(0f, 0.5f)]
    public float v4_leaderBurstStart = 0.26f;
    [Tooltip("선행 부스트 종료")]
    [Range(0.1f, 0.8f)]
    public float v4_leaderBurstEnd = 0.45f;

    [Tooltip("선입 부스트 시작")]
    [Range(0f, 0.6f)]
    public float v4_chaserBurstStart = 0.46f;
    [Tooltip("선입 부스트 종료")]
    [Range(0.2f, 0.9f)]
    public float v4_chaserBurstEnd = 0.65f;

    [Tooltip("추입 부스트 시작")]
    [Range(0f, 0.7f)]
    public float v4_reckonerBurstStart = 0.66f;
    [Tooltip("추입 부스트 종료 (finalSpurtStart와 맞닿아야 함)")]
    [Range(0.3f, 1.0f)]
    public float v4_reckonerBurstEnd = 0.85f;

    // ═══════════════════════════════════════════════
    //  테스트 옵션
    // ═══════════════════════════════════════════════
    [Header("═══ 테스트 옵션 ═══")]
    [Tooltip("부스트/스퍼트 완전 비활성화 — 순수 기본 달리기만 사용\n" +
             "ON: 모든 구간이 normalSpeedRatio(×1.0), drainMul 1.0 고정\n" +
             "검증: 5바퀴 Stamina20(HP140) → 완주 후 HP 약 44% 잔여 예상")]
    public bool v4_disableBurst = false;

    [Tooltip("컨디션(컨디션 배율) V4 레이스 적용 여부\n" +
             "ON: 컨디션이 Vmax(최고속도)와 MaxStamina(HP)에 배율로 적용\n" +
             "    컨디션 절정(×1.2) → Vmax·HP 20% 증가\n" +
             "    컨디션 최저(×0.9) → Vmax·HP 10% 감소\n" +
             "OFF: 컨디션 무시 — 밸런스 테스트에 권장")]
    public bool v4_applyCondition = false;

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
    //  긴급 부스트 (Emergency Burst)
    // ═══════════════════════════════════════════════
    [Header("═══ 긴급 부스트 ═══")]
    [Tooltip("ON: 부스트 구간 시작 전 목표 순위 이탈 시 자동 부스트\n" +
             "예) 도주 burstStart=0.05, 순위=4위(목표:1위) → 0.05까지 부스트 발동\n" +
             "부스트 구간 진입 시 자연스럽게 이어짐")]
    public bool v4_emergencyBurstEnabled = true;

    [Tooltip("긴급 부스트 속도 배율 (Vmax 대비)\n정규 부스트(v4_burstSpeedRatio)보다 낮게 설정 권장 (포지셔닝용)")]
    [Range(0.9f, 2.0f)]
    public float v4_emergencyBurstSpeedRatio = 1.20f;

    [Tooltip("긴급 부스트 HP 소모 배율\n정규 부스트(v4_burstDrainMul)보다 낮게 설정 권장")]
    [Range(1.0f, 4.0f)]
    public float v4_emergencyBurstDrainMul = 1.50f;

    // ═══════════════════════════════════════════════
    //  유틸리티 메서드
    // ═══════════════════════════════════════════════

    /// <summary>
    /// 현재 HP 비율에 따른 속도 배율 반환 (1.0 = 감소 없음)
    /// 매칭되는 임계값 중 hpRatio가 가장 낮은 항목 적용 (밴드 시스템)
    /// </summary>
    public float GetHpSpeedMultiplier(float hpRatio)
    {
        if (v4_hpSpeedThresholds == null || v4_hpSpeedThresholds.Count == 0) return 1f;
        float bestReduction = 0f;
        float bestThreshold = float.MaxValue;
        foreach (var entry in v4_hpSpeedThresholds)
        {
            if (hpRatio <= entry.hpRatio && entry.hpRatio < bestThreshold)
            {
                bestThreshold = entry.hpRatio;
                bestReduction = entry.speedReduction;
            }
        }
        return 1f - bestReduction;
    }

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
