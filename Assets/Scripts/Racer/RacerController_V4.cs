using System.Collections.Generic;
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
    private float v4LastVmax       = 0f;   // HP 감소 적용 전 Vmax (디버그용)
    private float v4LastHpSpeedMul = 1f;   // 이번 프레임 HP 속도 배율 (디버그용)
    private float v4LastTarget     = 0f;   // 이번 프레임 최종 target 속도 (디버그용)

    private V4Phase v4Phase = V4Phase.Normal;
    private bool v4IsSpurting = false;

    private bool v4IsPanicking = false;
    private float v4PanicTimer = 0f;

    private bool v4EmergencyBurst = false; // 긴급 부스트 (목표 순위 이탈 시)
    private int v4CurrentRank = 0;         // 현재 순위 (ThinkTick에서 업데이트)

    private bool v4InSlipstream = false;
    private float v4SlipstreamLeaderSpeed = 0f; // 슬립스트림 대상(앞 캐릭터)의 현재 속도
    private bool v4SlipstreamAccelActive = false; // 지능 판정 통과 여부
    private float v4SlipstreamRollTimer = 0f;     // 판정 쿨다운 타이머 (0이면 판정 가능)

    // 프로퍼티 (RaceDebugOverlay 표시용)
    public bool V4InSlipstream => v4InSlipstream;
    public bool V4SlipstreamAccelActive => v4SlipstreamAccelActive;
    private float v4ThinkTimer = 0f;
    private float v4LastProgress = 0f;  // 진행도 기반 드레인용

    // Luck 크리티컬
    private float v4LuckTimer = 0f;
    private float v4CritBoostRemaining = 0f;

    // 구간별 HP 체크포인트 (각자 통과 시 RaceDebugOverlay에 보고)
    private HashSet<string> v4PassedCheckpoints = new HashSet<string>();

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

        // 컨디션 적용: MaxStamina에 conditionMul 배율
        if (gs.v4_applyCondition)
        {
            float condMul = OddsCalculator.GetConditionMultiplier(charData.charId);
            v4MaxStamina *= Mathf.Max(condMul, 0.3f);
        }

        v4CurrentStamina = v4MaxStamina;
        v4CurrentSpeed  = GameSettings.Instance.globalSpeedMultiplier;

        // 디버그 시스템 호환 (enduranceHP = V4 스태미나 미러링)
        maxHP      = v4MaxStamina;
        enduranceHP = v4MaxStamina;

        v4Phase       = V4Phase.Normal;
        v4IsSpurting  = false;
        v4IsPanicking = false;
        v4InSlipstream = false;
        v4SlipstreamAccelActive = false;
        v4SlipstreamRollTimer = 0f;
        v4EmergencyBurst = false;
        v4CurrentRank = 0;
        v4ThinkTimer  = 0f;
        v4LastProgress = 0f;
        v4LuckTimer   = 0f;
        v4CritBoostRemaining = 0f;
        v4PassedCheckpoints.Clear();

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

        // 구간별 HP 체크포인트 보고 (각자 통과 시점 기준)
        ReportV4Checkpoints();

        // 긴급 부스트 체크 (부스트 구간 전, 목표 순위 이탈 시)
        UpdateV4EmergencyBurst(gs4);

        // V4 슬립스트림 감지
        UpdateV4Slipstream(gs4);

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

        // 컨디션 적용: Vmax에 conditionMul 배율
        if (gs.v4_applyCondition)
        {
            float condMul = OddsCalculator.GetConditionMultiplier(charDataV4.charId);
            vmax *= Mathf.Max(condMul, 0.3f);
        }

        float progress  = GetOverallProgress(); // 0~1 전체 진행도

        float target;
        float accelRate = charDataV4.v4Accel * gs.v4_accelStatFactor;

        // ── 구간 판별 (boolean으로 먼저 확정 → 두 번째 패스에서 재활용) ──
        bool burstActive  = !gs.v4_disableBurst;
        bool hpAvailable  = v4CurrentStamina > 0f;  // HP 0이면 부스트/스퍼트 불가
        bool inSpurtZone  = burstActive && hpAvailable && progress >= gs.v4_finalSpurtStart;
        bool inBurstZone  = !inSpurtZone && burstActive && hpAvailable && (IsInBurstZone(gs, progress) || v4EmergencyBurst);

        // ── HP 임계값 기반 속도 배율 (부스트/스퍼트 로그에 사용) ──
        float staminaRatio  = v4MaxStamina > 0 ? v4CurrentStamina / v4MaxStamina : 0f;
        float hpSpeedMul    = gs.GetHpSpeedMultiplier(staminaRatio);
        v4LastVmax       = vmax;         // HP 감소 전 Vmax 저장 (디버그 오버레이용)
        v4LastHpSpeedMul = hpSpeedMul;   // HP 배율 저장 (디버그 오버레이용)
        string hpPenaltyTag = hpSpeedMul < 1f
            ? $"[감속 -{(1f - hpSpeedMul):P0}]"
            : "[100%]";

        // ── 헬퍼: 현재 HP 상태 문자열 ──────────────
        string charLabel = charData?.DisplayName ?? charDataV4.charId;
        string HpStr() {
            float r = v4MaxStamina > 0 ? v4CurrentStamina / v4MaxStamina : 0f;
            return $"HP:{r:P0}({v4CurrentStamina:F0}/{v4MaxStamina:F0})";
        }
        var overlay = RaceManager.Instance?.GetComponent<RaceDebugOverlay>();

        if (inSpurtZone)
        {
            // 최종 스퍼트 (80~100%): 전원 남은 HP 연소
            target    = vmax * gs.v4_spurtVmaxBonus;
            accelRate *= gs.v4_spurtAccelBonus;

            if (!v4IsSpurting)
            {
                v4IsSpurting = true;
                v4Phase      = V4Phase.Spurt;
                string msg = $"{charLabel} 파이널스퍼트! {HpStr()} {hpPenaltyTag} (progress:{progress:P0})";
                Debug.Log($"[V4 FinalSpurt 시작] {msg}");
                overlay?.LogEvent(RaceDebugOverlay.EventType.Spurt, msg);
            }
        }
        else if (inBurstZone)
        {
            // 타입별 부스트 구간: Vmax 전력질주
            target = vmax * gs.v4_burstSpeedRatio;

            if (v4Phase != V4Phase.Burst) // 부스트 진입
            {
                string msg = $"{charLabel} 부스트 시작 {HpStr()} {hpPenaltyTag} (progress:{progress:P0})";
                Debug.Log($"[V4 Burst 시작] {msg}");
                overlay?.LogEvent(RaceDebugOverlay.EventType.Burst, msg);
                v4Phase = V4Phase.Burst;
            }
        }
        else
        {
            // 기본 달리기: 체력 비축 (또는 v4_disableBurst=true 시 항상 여기)
            target = vmax * gs.v4_normalSpeedRatio;

            if (v4Phase == V4Phase.Burst) // 부스트 이탈
            {
                string msg = $"{charLabel} 부스트 종료 {HpStr()} (progress:{progress:P0})";
                Debug.Log($"[V4 Burst 종료] {msg}");
                overlay?.LogEvent(RaceDebugOverlay.EventType.Burst, msg);
            }
            if (!v4IsSpurting) v4Phase = V4Phase.Normal;
        }

        // ── HP 임계값 기반 vmax 감소 ──
        // hpSpeedMul: 임계값 설정에 따른 배율 (1.0 = 감소 없음, 설정 없으면 항상 1.0)
        vmax *= hpSpeedMul;
        // target도 감소한 vmax 기준으로 재계산 (bool 재활용 — IsInBurstZone 중복호출 없음)
        if      (inSpurtZone) target = vmax * gs.v4_spurtVmaxBonus;
        else if (inBurstZone) target = vmax * gs.v4_burstSpeedRatio;
        else                  target = vmax * gs.v4_normalSpeedRatio;
        v4LastTarget = target;   // 최종 target 저장 (디버그 오버레이용)

        // ── 슬립스트림: 지능 판정 성공 시만 가속 혜택 ──
        // HP 드레인 감소는 ConsumeStaminaV4에서 별도 처리 (항상 적용)
        if (v4InSlipstream && v4SlipstreamAccelActive && v4SlipstreamLeaderSpeed > 0f)
        {
            target = Mathf.Min(target, v4SlipstreamLeaderSpeed);
            accelRate *= gs.v4_slipstreamAccelMul;
        }

        // ── Accel 스탯 기반 Lerp ──────────────────
        // currentSpeed는 Lerp 상태값 — 매 프레임 유지되므로 여기서만 갱신
        currentSpeed  = Mathf.Lerp(currentSpeed, target, Time.deltaTime * accelRate);

        // ── 일시적 배율은 별도 변수에 적용 (currentSpeed 오염 방지) ──
        // currentSpeed에 직접 곱하면 다음 프레임 Lerp 입력에 누적되어
        // 지수적 속도 증가 → 순간이동 버그 발생
        float outputSpeed = currentSpeed;

        // ── V4 Luck 크리티컬 배율 ──
        if (v4CritBoostRemaining > 0f)
            outputSpeed *= gs.v4_luckCritBoost;

        // ── 충돌 페널티 / 슬링샷 배율 (타이머 업데이트 포함) ──
        // V4 early return으로 인해 CalculateSpeed()의 GetCollisionMultiplier()가
        // 호출되지 않으므로 여기서 직접 적용
        outputSpeed *= GetCollisionMultiplier();

        v4CurrentSpeed = outputSpeed;
        return outputSpeed;
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

        // 지능 modifier: (지능 - 10) / 10 × modMax
        // 지능20 → +10%, 지능10 → ±0%, 지능0 → -10%
        float intModifier = (charDataV4.v4Intelligence - 10f) / 10f * gs.v4_intelligenceModMax;
        float effectiveEnd = start + (end - start) * (1f + intModifier);

        return progress >= start && progress < effectiveEnd;
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
            if      (currentProgress >= gs.v4_finalSpurtStart)                       drain *= gs.v4_spurtDrainMul;
            else if (IsInBurstZone(gs, currentProgress) || v4EmergencyBurst) drain *= gs.v4_burstDrainMul;
        }

        if (v4InSlipstream)
        {
            drain *= gs.v4_slipstreamDrainMul;                       // 항상: 드래프트 HP 절약
            if (v4SlipstreamAccelActive) drain *= gs.v4_slipstreamAccelDrainMul; // 가속 시: 추가 소모
        }
        if (v4IsPanicking)  drain *= gs.v4_panicDrainMul;

        v4CurrentStamina = Mathf.Max(0f, v4CurrentStamina - drain);
        enduranceHP      = v4CurrentStamina; // 디버그 시스템 호환
    }

    // ──────────────────────────────────────────────
    //  V4 슬립스트림 감지
    //  앞 캐릭터의 TotalProgress 차이가 v4_slipstreamRange 이하면 발동
    //  → ConsumeStaminaV4에서 drain × slipstreamDrainMul(0.70) 적용
    // ──────────────────────────────────────────────

    private void UpdateV4Slipstream(GameSettingsV4 gs)
    {
        if (RaceManager.Instance == null) { v4InSlipstream = false; return; }

        // 쿨다운 타이머 감소
        if (v4SlipstreamRollTimer > 0f)
            v4SlipstreamRollTimer -= Time.deltaTime;

        float myProgress = TotalProgress;
        bool inStream = false;
        float closestGap = float.MaxValue;
        float leaderSpeed = 0f;

        foreach (var r in RaceManager.Instance.Racers)
        {
            if (r == this || r.IsFinished) continue;
            float gap = r.TotalProgress - myProgress;
            if (gap > 0f && gap <= gs.v4_slipstreamRange && gap < closestGap)
            {
                inStream = true;
                closestGap = gap;
                leaderSpeed = r.currentSpeed;
            }
        }

        // 타입별 해금 진행도 체크 (해금 전이면 슬립스트림 무효)
        if (inStream && myProgress < GetV4SlipstreamUnlock(gs))
            inStream = false;

        // 슬립스트림 범위 진입 순간 & 쿨다운 만료 시 지능 확률 판정
        if (inStream && !v4InSlipstream && v4SlipstreamRollTimer <= 0f)
        {
            float prob = charDataV4 != null
                ? charDataV4.v4Intelligence / (charDataV4.v4Intelligence + gs.v4_slipstreamSmartK)
                : 0f;
            v4SlipstreamAccelActive = (UnityEngine.Random.value < prob);
            v4SlipstreamRollTimer = gs.v4_slipstreamRollCooldown; // 성공·실패 모두 쿨다운 시작

            // 이벤트 로그
            var overlay = RaceManager.Instance?.GetComponent<RaceDebugOverlay>();
            if (overlay != null && charDataV4 != null)
            {
                string result = v4SlipstreamAccelActive ? "성공 (가속ON)" : "실패 (HP절약만)";
                overlay.LogEvent(RaceDebugOverlay.EventType.Slipstream,
                    string.Format("{0} 슬립스트림 판정 {1} | Int:{2:F0} 확률:{3:P0}",
                        charDataV4.charId.Split('.')[2], result,
                        charDataV4.v4Intelligence, prob));
            }

            // VFX — 가속 성공 시만 표시
            if (v4SlipstreamAccelActive)
            {
                var vfx = GetComponent<CollisionVFX>() ?? gameObject.AddComponent<CollisionVFX>();
                vfx.Show(CollisionVFXType.Slipstream, 1.0f);
            }
        }
        else if (!inStream)
        {
            v4SlipstreamAccelActive = false; // 범위 이탈 시 가속 해제 (쿨다운은 유지)
        }

        v4InSlipstream = inStream;
        v4SlipstreamLeaderSpeed = leaderSpeed;
    }

    private float GetV4SlipstreamUnlock(GameSettingsV4 gs)
    {
        if (charDataV4 == null) return 0f;
        switch (charDataV4.charType)
        {
            case CharacterType.Runner:   return gs.v4_ssUnlockRunner;
            case CharacterType.Leader:   return gs.v4_ssUnlockLeader;
            case CharacterType.Chaser:   return gs.v4_ssUnlockChaser;
            case CharacterType.Reckoner: return gs.v4_ssUnlockReckoner;
            default: return 0f;
        }
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
                // 지능 modifier: (지능 - 10) / 10 × modMax
                // 지능20 → +10%, 지능10 → ±0%, 지능0 → -10%
                float intModifier = (charDataV4.v4Intelligence - 10f) / 10f * gs.v4_intelligenceModMax;
                v4CritBoostRemaining = gs.v4_luckCritDuration * (1f + intModifier);
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

    private void ProcessV4ThinkTick(GameSettingsV4 gs)
    {
        // 현재 순위 업데이트 (ThinkTick 주기로 — 매 프레임 리스트 생성 방지)
        if (RaceManager.Instance != null)
        {
            var rankings = RaceManager.Instance.GetLiveRankings();
            int idx = rankings.IndexOf(this);
            v4CurrentRank = idx >= 0 ? idx + 1 : rankings.Count;
        }
    }

    // ──────────────────────────────────────────────
    //  긴급 부스트 — 부스트 구간 시작 전, 목표 순위 이탈 시 자동 발동
    // ──────────────────────────────────────────────

    private void UpdateV4EmergencyBurst(GameSettingsV4 gs)
    {
        if (!gs.v4_emergencyBurstEnabled || charDataV4 == null || gs.v4_disableBurst) return;

        float progress  = GetOverallProgress();
        float burstStart = GetV4BurstStart(gs);

        if (progress >= burstStart)
        {
            // 부스트 구간 진입 → 긴급 부스트 해제 (정규 부스트로 이어짐)
            if (v4EmergencyBurst)
            {
                v4EmergencyBurst = false;
                var ov = RaceManager.Instance?.GetComponent<RaceDebugOverlay>();
                ov?.LogEvent(RaceDebugOverlay.EventType.Burst,
                    string.Format("{0} 긴급부스트 종료→정규부스트 연결 (rank:{1})",
                        charDataV4.charId.Split('.')[2], v4CurrentRank));
            }
            return;
        }

        // 부스트 구간 전: 목표 순위 체크
        if (v4CurrentRank <= 0) return;
        var (_, targetMax) = gs.GetV4TargetRankRange(charDataV4.charType);

        bool shouldEmergency = v4CurrentRank > targetMax;

        if (shouldEmergency && !v4EmergencyBurst)
        {
            v4EmergencyBurst = true;
            var ov = RaceManager.Instance?.GetComponent<RaceDebugOverlay>();
            ov?.LogEvent(RaceDebugOverlay.EventType.Burst,
                string.Format("{0} 긴급부스트! rank:{1} > 목표:{2} (progress:{3:P0})",
                    charDataV4.charId.Split('.')[2], v4CurrentRank, targetMax, progress));
        }
        else if (!shouldEmergency && v4EmergencyBurst)
        {
            v4EmergencyBurst = false; // 목표 순위 복귀 시 해제
        }
    }

    private float GetV4BurstStart(GameSettingsV4 gs)
    {
        if (charDataV4 == null) return 0f;
        switch (charDataV4.charType)
        {
            case CharacterType.Runner:   return gs.v4_runnerBurstStart;
            case CharacterType.Leader:   return gs.v4_leaderBurstStart;
            case CharacterType.Chaser:   return gs.v4_chaserBurstStart;
            case CharacterType.Reckoner: return gs.v4_reckonerBurstStart;
            default: return 0f;
        }
    }

    // ──────────────────────────────────────────────
    //  유틸
    // ──────────────────────────────────────────────

    // ──────────────────────────────────────────────
    //  구간별 HP 체크포인트 보고
    //  각 캐릭터가 자신의 진행도 25%/50%/100%를 통과하는 순간
    //  RaceDebugOverlay에 직접 보고 → 선두 기준이 아닌 "실제 그 거리를 달렸을 때" HP
    // ──────────────────────────────────────────────

    private void ReportV4Checkpoints()
    {
        if (charDataV4 == null) return;
        var overlay = RaceManager.Instance?.GetComponent<RaceDebugOverlay>();
        if (overlay == null) return;

        int   totalLaps = GetTotalLaps();
        float progress  = GetOverallProgress(); // 0~1 전체 진행도

        for (int lap = 1; lap <= totalLaps; lap++)
        {
            foreach (float sub in new float[] { 0.25f, 0.50f, 1.00f })
            {
                string key = $"L{lap}_{(int)(sub * 100)}";
                if (v4PassedCheckpoints.Contains(key)) continue;

                // 이 캐릭터가 해당 구간에 도달했는지 확인
                // threshold = (lap-1 + sub) / totalLaps (전체 0~1 기준)
                float threshold = ((lap - 1) + sub) / totalLaps;
                if (progress >= threshold)
                {
                    v4PassedCheckpoints.Add(key);
                    float hpPct   = v4MaxStamina > 0 ? v4CurrentStamina / v4MaxStamina * 100f : 0f;
                    string phase  = v4Phase == V4Phase.Spurt ? "스퍼트"
                                  : v4Phase == V4Phase.Burst  ? "부스트"
                                  : "노말";
                    overlay.RecordRacerCheckpoint(this, lap, sub, hpPct, currentSpeed,
                                                  v4LastVmax, v4LastHpSpeedMul, phase, v4LastTarget);
                }
            }
        }
    }

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
