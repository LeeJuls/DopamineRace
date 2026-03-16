using UnityEngine;

/// <summary>
/// RacerController V4 — 5대 스탯(Speed/Accel/Stamina/Power/Intelligence)+Luck 기반 달리기 로직
///
/// ▶ 레이스 페이즈 (GameSettingsV4 기준)
///   Phase 1 [Positioning]  0 ~ positioningEndRatio
///     - 자리잡기. 도주는 즉시 Spurt.
///
///   Phase 2 [Cruising]     positioningEndRatio ~ 스퍼트 트리거
///     - 타입별 크루징 속도 유지. 추입은 스태미나 비축.
///
///   Phase 3 [Spurt]        타입별 스퍼트 트리거 이후 (도주는 레이스 시작부터)
///     - Vmax × spurtVmaxBonus 해제, Accel × spurtAccelBonus 적용.
/// </summary>
public partial class RacerController : MonoBehaviour  // partial — RacerController.cs와 공유
{
    // ──────────────────────────────────────────────
    //  V4 런타임 상태
    // ──────────────────────────────────────────────

    private CharacterDataV4 charDataV4;

    private float v4CurrentStamina;
    private float v4MaxStamina;
    private float v4CurrentSpeed;

    private V4Phase v4Phase = V4Phase.Positioning;
    private bool v4IsSpurting = false;
    private float v4CruiseRatio = 1.0f; // 크루징 중 Vmax 대비 목표 속도 비율 (캐시)

    private bool v4IsPanicking = false;
    private float v4PanicTimer = 0f;

    private bool v4InSlipstream = false;
    private float v4ThinkTimer = 0f;

    // ──────────────────────────────────────────────
    //  V4 페이즈 열거형
    // ──────────────────────────────────────────────
    private enum V4Phase
    {
        Positioning,
        Cruising,
        Spurt
    }

    // ──────────────────────────────────────────────
    //  V4 초기화
    // ──────────────────────────────────────────────

    private void InitV4()
    {
        var gs = GameSettings.Instance?.v4Settings;
        if (gs == null || charData == null) return;

        charDataV4 = CharacterDatabaseV4.FindById(charData.charId);
        if (charDataV4 == null)
        {
            Debug.LogWarning($"[RacerController V4] charId '{charData.charId}' 를 CharacterDB_V4에서 찾지 못함");
            return;
        }

        v4MaxStamina = gs.v4_staminaBase + charDataV4.v4Stamina * gs.v4_staminaPerStat;
        v4CurrentStamina = v4MaxStamina;
        v4CurrentSpeed = GameSettings.Instance.globalSpeedMultiplier;

        // 디버그 시스템 호환 (enduranceHP = V4 스태미나 미러링)
        maxHP = v4MaxStamina;
        enduranceHP = v4MaxStamina;
        v4Phase = V4Phase.Positioning;
        v4IsSpurting = false;
        v4IsPanicking = false;
        v4InSlipstream = false;
        v4ThinkTimer = 0f;
        v4CruiseRatio = 1.0f;

        // 도주: 즉시 Spurt 페이즈 (Phase 1 풀스피드 정책)
        if (charDataV4.charType == CharacterType.Runner)
        {
            v4IsSpurting = true;
            v4Phase = V4Phase.Spurt;
        }

        Debug.Log($"[RacerController V4] InitV4 완료 — {charDataV4.charId} " +
                  $"Stamina={v4MaxStamina:F1} Phase={v4Phase}");
    }

    // ──────────────────────────────────────────────
    //  V4 업데이트 (매 프레임)
    // ──────────────────────────────────────────────

    private void UpdateV4(float dt)
    {
        var gs4 = GameSettings.Instance?.v4Settings;
        if (gs4 == null || charDataV4 == null) return;

        // 페이즈 전환: Positioning → Cruising
        if (v4Phase == V4Phase.Positioning)
        {
            float progress = GetOverallProgress();
            if (progress >= gs4.positioningEndRatio)
            {
                v4Phase = V4Phase.Cruising;
                v4CruiseRatio = CalcCruiseRatio(gs4); // 크루징 목표 속도 캐시
            }
        }

        // 스퍼트 트리거
        if (!v4IsSpurting && CheckV4SpurtTrigger(gs4))
        {
            v4IsSpurting = true;
            v4Phase = V4Phase.Spurt;
        }

        // 판단 틱 (지능 기반 AI — M3b에서 구현)
        v4ThinkTimer -= dt;
        if (v4ThinkTimer <= 0f)
        {
            ProcessV4ThinkTick(gs4);
            v4ThinkTimer = Mathf.Max(0.1f, gs4.GetV4ThinkTick(charDataV4.v4Intelligence));
        }

        // 스태미나 소모
        ConsumeStaminaV4(dt, gs4);
    }

    // ──────────────────────────────────────────────
    //  V4 속도 계산 (CalculateSpeed에서 호출)
    //  Accel 스탯 기반 Lerp 포함 — 이미 lerped된 currentSpeed 반환
    // ──────────────────────────────────────────────

    private float CalcSpeedV4(GameSettingsV4 gs)
    {
        if (charDataV4 == null) return GameSettings.Instance.globalSpeedMultiplier;

        float baseSpeed = GameSettings.Instance.globalSpeedMultiplier;
        float vmax = baseSpeed * (1f + charDataV4.v4Speed * gs.v4_speedStatFactor);

        // 목표 속도 결정
        float target;
        if (v4IsSpurting)
        {
            target = vmax * gs.v4_spurtVmaxBonus;
        }
        else
        {
            target = vmax * v4CruiseRatio;
        }

        // 탈진: 스태미나 10% 미만이면 속도 강제 감소
        float staminaRatio = v4MaxStamina > 0 ? v4CurrentStamina / v4MaxStamina : 0f;
        if (staminaRatio < 0.1f)
            target = Mathf.Lerp(vmax * gs.v4_exhaustSpeedFloor, target, staminaRatio / 0.1f);

        // Accel 스탯 기반 Lerp (스퍼트 시 가속 보너스)
        float accelRate = charDataV4.v4Accel * gs.v4_accelStatFactor;
        if (v4IsSpurting) accelRate *= gs.v4_spurtAccelBonus;
        currentSpeed = Mathf.Lerp(currentSpeed, target, Time.deltaTime * accelRate);

        v4CurrentSpeed = currentSpeed;
        return currentSpeed; // 이미 Lerp됨 → 외부 Lerp는 no-op
    }

    // ──────────────────────────────────────────────
    //  V4 스태미나 소모
    //  drain = maxHP × drainBaseRate × (speed / globalSpeedMultiplier)
    // ──────────────────────────────────────────────

    private void ConsumeStaminaV4(float dt, GameSettingsV4 gs)
    {
        if (charDataV4 == null || v4CurrentStamina <= 0) return;

        // 정규화된 드레인: globalSpeedMultiplier 기준으로 속도 비율 계산
        float baseSpeed = GameSettings.Instance.globalSpeedMultiplier;
        float speedRatio = baseSpeed > 0 ? currentSpeed / baseSpeed : 1f;
        float drain = v4MaxStamina * gs.v4_drainBaseRate * speedRatio;

        if (v4InSlipstream) drain *= gs.v4_slipstreamDrainMul;
        if (v4IsPanicking)  drain *= gs.v4_panicDrainMul;

        v4CurrentStamina = Mathf.Max(0f, v4CurrentStamina - drain * dt);
        enduranceHP = v4CurrentStamina; // 디버그 시스템 호환
    }

    // ──────────────────────────────────────────────
    //  V4 크루징 비율 계산 (Cruising 진입 시 1회 캐시)
    // ──────────────────────────────────────────────

    private float CalcCruiseRatio(GameSettingsV4 gs)
    {
        switch (charDataV4.charType)
        {
            case CharacterType.Runner:
                // 도주는 Spurt로 진입하지만 혹시를 대비한 기본값
                return Random.Range(gs.v4_runnerCruiseSpeedMin, gs.v4_runnerCruiseSpeedMax);
            case CharacterType.Reckoner:
                // 추입: 스태미나 비축을 위해 70~80% Vmax
                return Random.Range(gs.v4_reckonerCruiseSpeedMin, gs.v4_reckonerCruiseSpeedMax);
            case CharacterType.Leader:
                return gs.v4_pacemakerSpeedRatio - gs.v4_leaderOffset;
            case CharacterType.Chaser:
                return gs.v4_pacemakerSpeedRatio - gs.v4_chaserOffset;
            default:
                return gs.v4_pacemakerSpeedRatio;
        }
    }

    // ──────────────────────────────────────────────
    //  V4 스퍼트 트리거 (타입별 기준)
    // ──────────────────────────────────────────────

    private bool CheckV4SpurtTrigger(GameSettingsV4 gs)
    {
        if (charDataV4 == null) return false;
        if (charDataV4.charType == CharacterType.Runner) return true; // 항상 스퍼트

        if (waypoints == null || waypoints.Count == 0) return false;

        int totalLaps = GetTotalLaps();
        if (currentLap < totalLaps - 1) return false; // 마지막 랩 아님

        float lapProgress = (float)currentWP / waypoints.Count;

        float spurtRatio;
        switch (charDataV4.charType)
        {
            case CharacterType.Leader:   spurtRatio = gs.v4_leaderSpurtRatio;   break;
            case CharacterType.Chaser:   spurtRatio = gs.v4_chaserSpurtRatio;   break;
            case CharacterType.Reckoner: spurtRatio = gs.v4_reckonerSpurtRatio; break;
            default:                     spurtRatio = gs.spurtTriggerLapRemain; break;
        }
        return lapProgress >= (1f - spurtRatio);
    }

    // ──────────────────────────────────────────────
    //  V4 지능 판단 틱 (M3b에서 구현)
    // ──────────────────────────────────────────────

    private void ProcessV4ThinkTick(GameSettingsV4 gs) { }

    // ──────────────────────────────────────────────
    //  유틸
    // ──────────────────────────────────────────────

    /// <summary>전체 레이스 진행률 0~1</summary>
    private float GetOverallProgress()
    {
        if (waypoints == null || waypoints.Count == 0) return 0f;
        int totalLaps = GetTotalLaps();
        float totalWPs = waypoints.Count * totalLaps;
        float completedWPs = currentLap * waypoints.Count + currentWP;
        return totalWPs > 0 ? completedWPs / totalWPs : 0f;
    }
}
