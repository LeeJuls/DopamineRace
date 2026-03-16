using UnityEngine;

/// <summary>
/// RacerController V4 — 구간제 속도 시스템
///
/// ▶ 전체 트랙 진행도(0~1) 기준 구간별 속도 정책:
///
///   [부스트 구간] — 각 타입 고유의 전략 구간, Vmax(100%) 전력질주
///     도주(Runner)   : 0 ~ 20%   (초반 선두 확보)
///     선행(Leader)   : 20 ~ 40%  (초중반 유지)
///     선입(Chaser)   : 40 ~ 60%  (중반 포지션)
///     추입(Reckoner) : 60 ~ 80%  (후반 치고 올라옴)
///
///   [기본 달리기] — 부스트/최종스퍼트 외 구간, Vmax × normalSpeedRatio
///
///   [최종 스퍼트] — 80~100%, 전 타입 남은 HP 연소하며 전력질주
///     Vmax × finalSpurtMultiplier (기본 1.2)
///     추입은 사실상 60~100% 전력질주 상태
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

    private V4Phase v4Phase = V4Phase.Normal;
    private bool v4IsSpurting = false;

    private bool v4IsPanicking = false;
    private float v4PanicTimer = 0f;

    private bool v4InSlipstream = false;
    private float v4ThinkTimer = 0f;

    // ──────────────────────────────────────────────
    //  V4 페이즈 열거형 (로그/디버그용)
    // ──────────────────────────────────────────────
    private enum V4Phase
    {
        Normal,   // 기본 달리기
        Burst,    // 타입별 부스트 구간
        Spurt     // 최종 스퍼트 (80~100%)
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

        v4MaxStamina    = gs.v4_staminaBase + charDataV4.v4Stamina * gs.v4_staminaPerStat;
        v4CurrentStamina = v4MaxStamina;
        v4CurrentSpeed  = GameSettings.Instance.globalSpeedMultiplier;

        // 디버그 시스템 호환 (enduranceHP = V4 스태미나 미러링)
        maxHP      = v4MaxStamina;
        enduranceHP = v4MaxStamina;

        v4Phase       = V4Phase.Normal;
        v4IsSpurting  = false;
        v4IsPanicking = false;
        v4InSlipstream = false;
        v4ThinkTimer  = 0f;

        Debug.Log($"[RacerController V4] InitV4 완료 — {charDataV4.charId} " +
                  $"Type={charDataV4.charType} Stamina={v4MaxStamina:F1}");
    }

    // ──────────────────────────────────────────────
    //  V4 업데이트 (매 프레임)
    // ──────────────────────────────────────────────

    private void UpdateV4(float dt)
    {
        var gs4 = GameSettings.Instance?.v4Settings;
        if (gs4 == null || charDataV4 == null) return;

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
    //  구간제: 진행도 기반으로 Burst / Normal / FinalSpurt 결정
    // ──────────────────────────────────────────────

    private float CalcSpeedV4(GameSettingsV4 gs)
    {
        if (charDataV4 == null) return GameSettings.Instance.globalSpeedMultiplier;

        float baseSpeed = GameSettings.Instance.globalSpeedMultiplier;
        float vmax      = baseSpeed * (1f + charDataV4.v4Speed * gs.v4_speedStatFactor);
        float progress  = GetOverallProgress(); // 0~1 전체 진행도

        float target;
        float accelRate = charDataV4.v4Accel * gs.v4_accelStatFactor;

        // ── 구간 판별 ──────────────────────────────
        if (progress >= gs.v4_finalSpurtStart)
        {
            // 최종 스퍼트 (80~100%): 전원 남은 HP 연소
            target   = vmax * gs.v4_spurtVmaxBonus;
            accelRate *= gs.v4_spurtAccelBonus;

            if (!v4IsSpurting)
            {
                v4IsSpurting = true;
                v4Phase      = V4Phase.Spurt;
                Debug.Log($"[V4 Spurt] {charDataV4.charId} 최종 스퍼트! progress={progress:P0}");
            }
        }
        else if (IsInBurstZone(gs, progress))
        {
            // 타입별 부스트 구간: Vmax 전력질주
            target  = vmax * gs.v4_burstSpeedRatio;
            v4Phase = V4Phase.Burst;
        }
        else
        {
            // 기본 달리기: 체력 비축
            target  = vmax * gs.v4_normalSpeedRatio;
            if (!v4IsSpurting) v4Phase = V4Phase.Normal;
        }

        // ── HP 비율에 따라 vmax 점진적 감소 ──
        // HP 100% → vmax×1.0, HP 0% → vmax×exhaustSpeedFloor (ex: ×0.8)
        float staminaRatio = v4MaxStamina > 0 ? v4CurrentStamina / v4MaxStamina : 0f;
        vmax *= Mathf.Lerp(gs.v4_exhaustSpeedFloor, 1.0f, staminaRatio);
        // target도 감소한 vmax 기준으로 재계산
        if (progress >= gs.v4_finalSpurtStart)
            target = vmax * gs.v4_spurtVmaxBonus;
        else if (IsInBurstZone(gs, progress))
            target = vmax * gs.v4_burstSpeedRatio;
        else
            target = vmax * gs.v4_normalSpeedRatio;

        // ── Accel 스탯 기반 Lerp ──────────────────
        currentSpeed  = Mathf.Lerp(currentSpeed, target, Time.deltaTime * accelRate);
        v4CurrentSpeed = currentSpeed;
        return currentSpeed;
    }

    // ──────────────────────────────────────────────
    //  타입별 부스트 구간 판별
    // ──────────────────────────────────────────────

    private bool IsInBurstZone(GameSettingsV4 gs, float progress)
    {
        float start, end;
        switch (charDataV4.charType)
        {
            case CharacterType.Runner:   start = gs.v4_runnerBurstStart;   end = gs.v4_runnerBurstEnd;   break;
            case CharacterType.Leader:   start = gs.v4_leaderBurstStart;   end = gs.v4_leaderBurstEnd;   break;
            case CharacterType.Chaser:   start = gs.v4_chaserBurstStart;   end = gs.v4_chaserBurstEnd;   break;
            case CharacterType.Reckoner: start = gs.v4_reckonerBurstStart; end = gs.v4_reckonerBurstEnd; break;
            default: return false;
        }
        return progress >= start && progress < end;
    }

    // ──────────────────────────────────────────────
    //  V4 스태미나 소모
    //  drain = maxHP × drainBaseRate × (currentSpeed / globalSpeedMultiplier)
    // ──────────────────────────────────────────────

    private void ConsumeStaminaV4(float dt, GameSettingsV4 gs)
    {
        if (charDataV4 == null || v4CurrentStamina <= 0) return;

        float baseSpeed  = GameSettings.Instance.globalSpeedMultiplier;
        float speedRatio = baseSpeed > 0 ? currentSpeed / baseSpeed : 1f;
        float drain      = v4MaxStamina * gs.v4_drainBaseRate * speedRatio;

        // 구간별 추가 소모 (v4Phase 대신 progress 직접 계산 — phase는 CalcSpeedV4 후 갱신되므로 순서 문제 방지)
        float p = GetOverallProgress();
        if      (p >= gs.v4_finalSpurtStart) drain *= gs.v4_spurtDrainMul;
        else if (IsInBurstZone(gs, p))       drain *= gs.v4_burstDrainMul;

        if (v4InSlipstream) drain *= gs.v4_slipstreamDrainMul;
        if (v4IsPanicking)  drain *= gs.v4_panicDrainMul;

        v4CurrentStamina = Mathf.Max(0f, v4CurrentStamina - drain * dt);
        enduranceHP      = v4CurrentStamina; // 디버그 시스템 호환
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
        int   totalLaps    = GetTotalLaps();
        float totalWPs     = waypoints.Count * totalLaps;
        float completedWPs = currentLap * waypoints.Count + currentWP;
        return totalWPs > 0 ? completedWPs / totalWPs : 0f;
    }
}
