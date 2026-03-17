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
    private float v4LastProgress = 0f;  // 진행도 기반 드레인용

    // Luck 크리티컬
    private float v4LuckTimer = 0f;
    private float v4CritBoostRemaining = 0f;

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
        v4LastProgress = 0f;
        v4LuckTimer   = 0f;
        v4CritBoostRemaining = 0f;

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

        // V4 Luck 크리티컬 판정
        UpdateV4LuckCrit(gs4);
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
        bool burstActive = !gs.v4_disableBurst; // 테스트 옵션: OFF 시 순수 노말 달리기
        if (burstActive && progress >= gs.v4_finalSpurtStart)
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
        else if (burstActive && IsInBurstZone(gs, progress))
        {
            // 타입별 부스트 구간: Vmax 전력질주
            target  = vmax * gs.v4_burstSpeedRatio;
            v4Phase = V4Phase.Burst;
        }
        else
        {
            // 기본 달리기: 체력 비축 (또는 v4_disableBurst=true 시 항상 여기)
            target  = vmax * gs.v4_normalSpeedRatio;
            if (!v4IsSpurting) v4Phase = V4Phase.Normal;
        }

        // ── HP 비율에 따라 vmax 점진적 감소 ──
        // HP 100% → vmax×1.0, HP 0% → vmax×exhaustSpeedFloor (ex: ×0.8)
        float staminaRatio = v4MaxStamina > 0 ? v4CurrentStamina / v4MaxStamina : 0f;
        vmax *= Mathf.Lerp(gs.v4_exhaustSpeedFloor, 1.0f, staminaRatio);
        // target도 감소한 vmax 기준으로 재계산
        if (burstActive && progress >= gs.v4_finalSpurtStart)
            target = vmax * gs.v4_spurtVmaxBonus;
        else if (burstActive && IsInBurstZone(gs, progress))
            target = vmax * gs.v4_burstSpeedRatio;
        else
            target = vmax * gs.v4_normalSpeedRatio;

        // ── Accel 스탯 기반 Lerp ──────────────────
        currentSpeed  = Mathf.Lerp(currentSpeed, target, Time.deltaTime * accelRate);

        // ── V4 Luck 크리티컬 배율 ──
        if (v4CritBoostRemaining > 0f)
            currentSpeed *= gs.v4_luckCritBoost;

        // ── 충돌 페널티 / 슬링샷 배율 (타이머 업데이트 포함) ──
        // V4 early return으로 인해 CalculateSpeed()의 GetCollisionMultiplier()가
        // 호출되지 않으므로 여기서 직접 적용
        currentSpeed *= GetCollisionMultiplier();

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
    //  V4 스태미나 소모 — 바퀴당 절대량 기반
    //  drain = drainPerLap × totalLaps × progressDelta × phaseMul
    //  → 바퀴 수에 비례하여 총 드레인 증가 (진짜 절대 거리 소모)
    //  → 단거리(2바퀴): HP 넉넉 / 장거리(5바퀴): HP 관리 필수
    // ──────────────────────────────────────────────

    private void ConsumeStaminaV4(float dt, GameSettingsV4 gs)
    {
        if (charDataV4 == null || v4CurrentStamina <= 0) return;

        float currentProgress = GetOverallProgress();
        float progressDelta   = Mathf.Max(0f, currentProgress - v4LastProgress);
        v4LastProgress = currentProgress;

        if (progressDelta <= 0f) return;

        // 바퀴당 절대 드레인 × 총 바퀴 수 (절대 거리 소모)
        int totalLaps = GetTotalLaps();
        float drain = gs.v4_drainPerLap * totalLaps * progressDelta;

        // 구간별 추가 소모 (v4_disableBurst=true 시 배율 1.0 고정 — 순수 노말 테스트)
        if (!gs.v4_disableBurst)
        {
            if      (currentProgress >= gs.v4_finalSpurtStart) drain *= gs.v4_spurtDrainMul;
            else if (IsInBurstZone(gs, currentProgress))       drain *= gs.v4_burstDrainMul;
        }

        if (v4InSlipstream) drain *= gs.v4_slipstreamDrainMul;
        if (v4IsPanicking)  drain *= gs.v4_panicDrainMul;

        v4CurrentStamina = Mathf.Max(0f, v4CurrentStamina - drain);
        enduranceHP      = v4CurrentStamina; // 디버그 시스템 호환
    }

    // ──────────────────────────────────────────────
    //  V4 Luck 크리티컬 판정
    //  UpdateV4()에서 매 프레임 호출
    // ──────────────────────────────────────────────

    private void UpdateV4LuckCrit(GameSettingsV4 gs)
    {
        if (charDataV4 == null) return;

        float gameDt = Time.deltaTime * GameSettings.Instance.globalSpeedMultiplier;

        // 크리티컬 진행 중
        if (v4CritBoostRemaining > 0f)
        {
            v4CritBoostRemaining -= gameDt;
            if (v4CritBoostRemaining <= 0f)
            {
                v4CritBoostRemaining = 0f;
                isCritActive = false;
            }
            return;
        }

        // 새 판정
        v4LuckTimer -= gameDt;
        if (v4LuckTimer <= 0f)
        {
            v4LuckTimer = gs.v4_luckCheckInterval;

            float chance = charDataV4.v4Luck * gs.v4_luckCritChance;
            if (UnityEngine.Random.value < chance)
            {
                v4CritBoostRemaining = gs.v4_luckCritDuration;
                isCritActive = true;

                // VFX
                var critVfx = GetComponent<CollisionVFX>();
                if (critVfx == null) critVfx = gameObject.AddComponent<CollisionVFX>();
                critVfx.Show(CollisionVFXType.Crit, 0.8f);

                Debug.Log($"★ [V4] 크리티컬! {charDataV4.charId} (luck:{charDataV4.v4Luck})");
            }
        }
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
