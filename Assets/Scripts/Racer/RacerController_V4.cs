using UnityEngine;

/// <summary>
/// RacerController V4 — 5대 스탯(Speed/Accel/Stamina/Power/Intelligence)+Luck 기반 달리기 로직
///
/// ▶ 레이스 페이즈 (GameSettingsV4 기준)
///   Phase 1 [Positioning]  0 ~ positioningEndRatio
///     - 각 타입이 목표 순위 범위로 이동. 도주는 무조건 선두 추구.
///
///   Phase 2 [Cruising]     positioningEndRatio ~ 마지막 랩 spurtTriggerLapRemain 전
///     - 페이스 유지. 스태미나 비축.
///
///   Phase 3 [Spurt]        마지막 랩 spurtTriggerLapRemain 이후 (도주는 레이스 시작부터)
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
    private float v4CurrentSpeed;  // CalcSpeedV4가 계산한 목표 속도

    private V4Phase v4Phase = V4Phase.Positioning;
    private bool v4IsSpurting = false;

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
        v4Phase = V4Phase.Positioning;
        v4IsSpurting = false;
        v4IsPanicking = false;
        v4InSlipstream = false;
        v4ThinkTimer = 0f;

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

        // 페이즈 전환
        if (v4Phase == V4Phase.Positioning)
        {
            float progress = GetOverallProgress();
            if (progress >= gs4.positioningEndRatio)
                v4Phase = V4Phase.Cruising;
        }

        if (!v4IsSpurting && CheckV4SpurtTrigger(gs4))
        {
            v4IsSpurting = true;
            v4Phase = V4Phase.Spurt;
        }

        // 판단 틱 (지능 기반 AI)
        v4ThinkTimer -= dt;
        if (v4ThinkTimer <= 0f)
        {
            ProcessV4ThinkTick(gs4);
            v4ThinkTimer = gs4.v4_thinkTickBase;
        }

        // 스태미나 소모
        ConsumeStaminaV4(dt, gs4);
    }

    // ──────────────────────────────────────────────
    //  V4 속도 계산 (CalculateSpeed에서 호출)
    // ──────────────────────────────────────────────

    private float CalcSpeedV4(GameSettingsV4 gs)
    {
        if (charDataV4 == null) return GameSettings.Instance.globalSpeedMultiplier;

        float baseSpeed = GameSettings.Instance.globalSpeedMultiplier;
        float vmax = baseSpeed * (1f + charDataV4.v4Speed * gs.v4_speedStatFactor);

        float staminaRatio = v4MaxStamina > 0 ? v4CurrentStamina / v4MaxStamina : 0f;
        float target;

        if (v4IsSpurting)
            target = vmax * gs.v4_spurtVmaxBonus;
        else if (staminaRatio < 0.1f)
            // 탈진: exhaustSpeedFloor ~ vmax 사이 선형 보간
            target = Mathf.Lerp(vmax * gs.v4_exhaustSpeedFloor, vmax, staminaRatio / 0.1f);
        else
            target = vmax;

        v4CurrentSpeed = target;
        return target;
    }

    // ──────────────────────────────────────────────
    //  V4 스태미나 소모
    // ──────────────────────────────────────────────

    private void ConsumeStaminaV4(float dt, GameSettingsV4 gs)
    {
        if (charDataV4 == null || v4CurrentStamina <= 0) return;

        float drain = currentSpeed * gs.v4_drainBaseRate;
        if (v4InSlipstream) drain *= gs.v4_slipstreamDrainMul;
        if (v4IsPanicking)  drain *= gs.v4_panicDrainMul;

        v4CurrentStamina = Mathf.Max(0f, v4CurrentStamina - drain * dt);
    }

    // ──────────────────────────────────────────────
    //  V4 스퍼트 트리거 체크
    // ──────────────────────────────────────────────

    private bool CheckV4SpurtTrigger(GameSettingsV4 gs)
    {
        if (charDataV4 == null) return false;

        // 도주: 항상 스퍼트 (InitV4에서 이미 설정되나 보험용)
        if (charDataV4.charType == CharacterType.Runner) return true;

        // 나머지: 마지막 랩에서 spurtTriggerLapRemain 이하 남았을 때
        if (waypoints == null || waypoints.Count == 0) return false;

        int totalLaps = GetTotalLaps();
        if (currentLap < totalLaps - 1) return false; // 마지막 랩 아님

        float lapProgress = (float)currentWP / waypoints.Count;
        return lapProgress >= (1f - gs.spurtTriggerLapRemain);
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
