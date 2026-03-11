using UnityEngine;

/// <summary>
/// Race V3 시스템 — RacerController partial class 확장.
/// Speed → maxSpeed, Endurance → 스태미나 지속력, Brave → 가속도.
/// 포지션 피드백(능동 전략) + 3구간 HP-속도 곡선.
/// </summary>
public partial class RacerController
{
    // ──────────────────────────────────────────────────────────────
    //  V3 메인 속도 계산 (CalculateSpeed에서 호출)
    // ──────────────────────────────────────────────────────────────

    /// <summary>
    /// V3 속도 계산.
    /// base speed = globalSpeedMultiplier × trackSpeedMul (모든 캐릭터 동일)
    /// maxSpeedMul = 1 + charSpeed × perPoint (speed 스탯이 최고속도 결정)
    /// </summary>
    private float CalcSpeedV3(GameSettings gs, TrackData track, float condMul)
    {
        float trackSpeedMul = track != null ? track.speedMultiplier : 1f;
        float v3Base = gs.globalSpeedMultiplier * trackSpeedMul;

        // Step 1: Brave → 가속 업데이트 (스프린트 발동 판정)
        UpdateV3Sprint(gs, Time.deltaTime);

        // Step 2: Speed 스탯 → maxSpeedMul, HP → hpSpeedMul
        float maxSpeedMul = gs.GetV3MaxSpeedMul(charData.charBaseSpeed);
        float hpRatio     = maxHP > 0f ? Mathf.Clamp01(enduranceHP / maxHP) : 0f;
        float hpSpeedMul  = gs.GetV3SpeedFromHP(hpRatio);

        // Step 3: 스프린트 진행률로 속도 배율 결정
        //   v3SprintAccelProgress=0 → 1.0×hpSpeedMul (기본)
        //   v3SprintAccelProgress=1 → maxSpeedMul×hpSpeedMul (전력질주)
        float speedRatio = Mathf.Lerp(1.0f, maxSpeedMul, v3SprintAccelProgress) * hpSpeedMul;

        // Step 4: 슬립스트림 보너스 (CP 연동)
        float cpEff   = gs.GetCPEfficiency(CPRatio);
        float ssBonus = gs.GetSlipstreamBonus(charData.charType, slipstreamBlend, cpEff);
        speedRatio *= (1f + ssBonus * condMul);

        // Step 5: 포지션 피드백 드레인 배율 (포메이션 구간만)
        float zoneDrainMul = CalcV3ZoneDrainMul(gs);

        // Step 6: HP 소모 (속도 × 존 배율)
        ConsumeHP_V3(gs, speedRatio, zoneDrainMul, Time.deltaTime);

        // Step 7: 최종 속도
        float speed = v3Base * speedRatio;
        speed += GetNoiseValue(gs, track) * condMul;  // Calm 기반 노이즈
        return speed;
    }

    // ──────────────────────────────────────────────────────────────
    //  V3 스프린트 업데이트 (Brave가 가속도 결정)
    // ──────────────────────────────────────────────────────────────

    /// <summary>
    /// 전략 구간 진입 후 타입별 시점에 스프린트 활성.
    /// brave가 높을수록 v3SprintAccelProgress가 더 빠르게 1.0에 도달.
    /// </summary>
    private void UpdateV3Sprint(GameSettings gs, float dt)
    {
        float strategyStart = gs.formationHoldLapEnd / GetTotalLaps();
        if (OverallProgress < strategyStart) return;

        // 타입별 스프린트 발동 시점 판정
        float strategyProg = Mathf.InverseLerp(strategyStart, 1f, OverallProgress);
        if (!v3IsSprintActive && strategyProg >= gs.GetV3SprintStart(charData.charType))
            v3IsSprintActive = true;

        if (!v3IsSprintActive) return;

        // brave → accelRate (높을수록 빠르게 최고속 도달)
        float accelRate = 1.0f + charData.charBaseBrave * gs.v3_braveAccelPerPoint;
        float gameDt    = dt * gs.globalSpeedMultiplier;
        float step      = gameDt * accelRate / gs.v3_sprintAccelTimeBase;
        v3SprintAccelProgress = Mathf.MoveTowards(v3SprintAccelProgress, 1f, step);

        isSprintMode = v3SprintAccelProgress > 0.1f;
    }

    // ──────────────────────────────────────────────────────────────
    //  V3 포지션 피드백 (능동 전략)
    // ──────────────────────────────────────────────────────────────

    /// <summary>
    /// 포메이션 구간(전략 구간 이전)에서만 작동.
    /// 목표 구역보다 뒤처지면 드레인 증가 (추격), 너무 앞서면 감소 (절약).
    /// </summary>
    private float CalcV3ZoneDrainMul(GameSettings gs)
    {
        // 전략 구간 이후 → 포지션 피드백 OFF
        if (OverallProgress >= gs.formationHoldLapEnd / GetTotalLaps()) return 1.0f;

        int totalRacers = RaceManager.Instance != null
            ? RaceManager.Instance.Racers.Count : 9;
        if (totalRacers <= 1) return 1.0f;

        float zoneTarget  = gs.GetV3ZoneTarget(charData.charType);
        float myRankRatio = (float)(currentRank - 1) / (totalRacers - 1);  // 0=1위, 1=꼴찌
        float diff        = myRankRatio - zoneTarget;

        if (diff >  gs.v3_zoneRange) return gs.v3_zoneDrainMul;  // 뒤처짐 → 추격 비용
        if (diff < -gs.v3_zoneRange) return gs.v3_zoneSaveMul;   // 너무 앞 → 절약
        return 1.0f;                                               // 구역 내 → 정상
    }

    // ──────────────────────────────────────────────────────────────
    //  V3 HP 소모
    // ──────────────────────────────────────────────────────────────

    /// <summary>
    /// V3 스태미나 소모.
    /// drain = drainBaseRate × speedRatio^drainExponent × zoneDrainMul
    /// 전체 float 연산 — 정수 반올림 없음.
    /// </summary>
    private void ConsumeHP_V3(GameSettings gs, float speedRatio, float zoneDrainMul, float dt)
    {
        if (enduranceHP <= 0f) return;

        float gameDt      = dt * gs.globalSpeedMultiplier;
        float drain       = gs.v3_drainBaseRate
                            * Mathf.Pow(speedRatio, gs.v3_drainExponent)
                            * zoneDrainMul;
        float consumption = Mathf.Min(drain * gameDt, enduranceHP);
        enduranceHP      -= consumption;
        totalConsumedHP  += consumption;

        // 선두 HP 택스 (기존 설정 유지)
        if (currentRank <= gs.leadPaceTaxRank && enduranceHP > 0f)
        {
            float tax = Mathf.Min(gs.leadPaceTaxRate * gameDt, enduranceHP);
            enduranceHP -= tax;
        }
    }
}
