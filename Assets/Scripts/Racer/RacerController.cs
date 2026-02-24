using UnityEngine;
using System;
using System.Collections.Generic;

public class RacerController : MonoBehaviour
{
    // ── 기본 정보 ──
    private int racerIndex;
    private List<Transform> waypoints;
    private Animator animator;
    private Vector3 lastPosition;

    // ── 이동 상태 ──
    private int currentWP = 0;
    private int currentLap = 0;
    private float currentSpeed = 0f;
    private bool isRacing = false;
    private bool isFinished = false;
    private bool headingToFinish = false;
    private float laneOffset;

    // ── 경로 이탈(흔들림) ──
    private float deviationOffset = 0f;
    private float deviationTarget = 0f;
    private float deviationTimer = 0f;

    // ── ★ 캐릭터 스탯 (A-2) ──
    private CharacterData charData;
    private float noiseValue = 0f;
    private float noiseTimer = 0f;

    // ── ★ Luck 크리티컬 ──
    private float luckTimer = 0f;
    private float critBoostRemaining = 0f;
    private bool isCritActive = false;

    // ── ★ 충돌 상태 (A-3에서 사용, 미리 준비) ──
    private float collisionPenalty = 0f;       // 현재 감속 비율 (0~1)
    private float collisionPenaltyTimer = 0f;  // 감속 남은 시간
    private float slingshotBoost = 0f;         // 슬링샷 가속 비율
    private float slingshotTimer = 0f;         // 슬링샷 남은 시간

    // ── ★ 스킬 시스템 ──
    private int skillCollisionCount = 0;       // 충돌 횟수 누적
    private bool skillActive = false;          // 현재 스킬 발동 중 (무기 꺼냄)
    private float skillRemainingTime = 0f;     // 스킬 남은 시간
    private GameObject normalModel;            // 맨몸 모델
    private GameObject attackModel;            // 무기 든 모델 (미리 생성, 비활성)
    private Animator normalAnimator;           // 맨몸 Animator
    private Animator attackAnimator;           // 무기 Animator

    // ── ★ HP 시스템 (SPEC-006) ──
    private float enduranceHP;                 // 현재 HP (maxHP에서 시작, 0까지 감소)
    private float maxHP;                       // 50 + charEndurance × 2.5
    private float totalConsumedHP;             // 누적 소모량 (부스트 비율 계산용)
    private float hpBoostValue;                // 현재 프레임의 HP 부스트 값 (디버그용)
    private float slipstreamBlend;             // Slipstream 페이드 0~1 (Phase 4)
    private int currentRank;                   // 현재 순위 1~12 (Phase 4)

    // ── 외부 접근 ──
    public int RacerIndex => racerIndex;
    public bool IsFinished => isFinished;
    public int FinishOrder { get; set; } = -1;
    public float CurrentSpeed => currentSpeed;
    public int CurrentLap => currentLap;
    public CharacterData CharData => charData;
    public bool IsCritActive => isCritActive;
    public bool SkillActive => skillActive;           // 스킬 발동 중 여부 (외부 참조용)
    public int SkillCollisionCount => skillCollisionCount;
    public float CollisionPenalty => collisionPenalty;
    public float SlingshotBoost => slingshotBoost;

    // ── HP 시스템 외부 접근 ──
    public float EnduranceHP => enduranceHP;
    public float MaxHP => maxHP;
    public float TotalConsumedHP => totalConsumedHP;
    public float HPBoostValue => hpBoostValue;
    public int CurrentRank { get => currentRank; set => currentRank = value; }

    public float TotalProgress => isFinished ? float.MaxValue :
        (waypoints == null || waypoints.Count == 0) ? 0 :
        currentLap + (float)currentWP / waypoints.Count;

    /// <summary>
    /// 전체 레이스 진행률 (0~1), 바퀴 수 고려
    /// </summary>
    public float OverallProgress
    {
        get
        {
            if (isFinished) return 1f;
            if (waypoints == null || waypoints.Count == 0) return 0f;
            int totalLaps = GetTotalLaps();
            float lapProgress = (float)currentWP / waypoints.Count;
            return (currentLap + lapProgress) / totalLaps;
        }
    }

    private int GetTotalLaps()
    {
        return RaceManager.Instance != null ? RaceManager.Instance.CurrentLaps : GameConstants.TOTAL_LAPS;
    }

    public event Action<RacerController> OnFinished;

    // ══════════════════════════════════════
    //  초기화
    // ══════════════════════════════════════

    public void Initialize(int index, List<Transform> wps)
    {
        racerIndex = index;
        waypoints = wps;
        laneOffset = (index - GameConstants.RACER_COUNT / 2f) * GameSettings.Instance.laneOffset;
        animator = GetComponentInChildren<Animator>();
    }

    /// <summary>
    /// ★ A-2: 캐릭터 스탯 주입 (RaceManager에서 호출)
    /// </summary>
    public void SetCharacterData(CharacterData data)
    {
        charData = data;
    }

    public void ConfirmSpawnPosition()
    {
        lastPosition = transform.position;
    }

    // ══════════════════════════════════════
    //  레이스 시작 / 리셋
    // ══════════════════════════════════════

    public void StartRacing()
    {
        isRacing = true; isFinished = false; headingToFinish = false; FinishOrder = -1;
        currentLap = 0; currentWP = 0;
        lastPosition = transform.position;

        // 스탯 기반 초기 속도
        currentSpeed = GetBaseSpeed() * GameSettings.Instance.initialSpeedMultiplier;

        // noise/luck 타이머 초기화
        noiseValue = 0f;
        noiseTimer = 0f;
        luckTimer = 0f;
        critBoostRemaining = 0f;
        isCritActive = false;

        // 충돌 상태 초기화
        collisionPenalty = 0f;
        collisionPenaltyTimer = 0f;
        slingshotBoost = 0f;
        slingshotTimer = 0f;

        // 스킬 상태 초기화
        skillCollisionCount = 0;
        skillActive = false;
        skillRemainingTime = 0f;
        DeactivateSkill();

        // 흔들림 초기화
        deviationOffset = 0f;
        deviationTarget = 0f;
        deviationTimer = 0f;

        // HP 시스템 초기화
        if (charData != null && GameSettings.Instance.useHPSystem)
        {
            maxHP = GameSettings.Instance.CalcMaxHP(charData.charBaseEndurance);
            enduranceHP = maxHP;
            totalConsumedHP = 0f;
            hpBoostValue = 0f;
            slipstreamBlend = 0f;
            currentRank = racerIndex + 1;
        }

        if (animator != null) animator.SetTrigger("Run");
    }

    public void ResetRacer(Vector3 pos)
    {
        isRacing = false; isFinished = false; headingToFinish = false; FinishOrder = -1;
        currentSpeed = 0f; currentWP = 0; currentLap = 0;

        // noise/luck 타이머 (StartRacing 전 깨끗한 상태 보장)
        noiseValue = 0f;
        noiseTimer = 0f;
        luckTimer = 0f;

        // 흔들림
        deviationOffset = 0f;
        deviationTarget = 0f;
        deviationTimer = 0f;

        // 충돌/슬링샷
        collisionPenalty = 0f; collisionPenaltyTimer = 0f;
        slingshotBoost = 0f; slingshotTimer = 0f;

        // 크리티컬/공격
        critBoostRemaining = 0f; isCritActive = false;
        attackCooldown = 0f; attackAnimChecked = false;

        // 스킬
        skillCollisionCount = 0; skillActive = false; skillRemainingTime = 0f;
        DeactivateSkill();

        // HP 시스템
        enduranceHP = 0f; maxHP = 0f; totalConsumedHP = 0f;
        hpBoostValue = 0f; slipstreamBlend = 0f; currentRank = 0;

        transform.position = pos;
        lastPosition = pos;
        if (animator != null) animator.SetTrigger("Idle");
    }

    // ══════════════════════════════════════
    //  메인 업데이트
    // ══════════════════════════════════════

    private void Update()
    {
        ApplyLiveSettings();

        if (!isRacing || isFinished) return;
        if (waypoints == null || waypoints.Count == 0) return;

        var gs = GameSettings.Instance;

        // ── 1) 스탯 기반 속도 계산 ──
        float finalSpeed = CalculateSpeed(gs);
        currentSpeed = Mathf.Lerp(currentSpeed, finalSpeed, Time.deltaTime * gs.raceSpeedLerp);

        // ── 2) 경로 이탈 업데이트 ──
        UpdateDeviation();

        // ── 3) 이동 ──
        Vector3 target = headingToFinish ? GetOffsetPosition(0) : GetOffsetPosition(currentWP);
        Vector3 dir = target - transform.position;
        float dist = dir.magnitude;

        if (dist < GameSettings.Instance.waypointArrivalDist)
        {
            if (headingToFinish)
            {
                transform.position = target;
                isFinished = true; isRacing = false; currentSpeed = 0f;
                if (animator != null) animator.SetTrigger("Idle");
                OnFinished?.Invoke(this);
                return;
            }

            currentWP++;
            if (currentWP >= waypoints.Count)
            {
                currentWP = 0;
                currentLap++;
                if (currentLap >= GetTotalLaps())
                {
                    headingToFinish = true;
                }
            }
        }
        else
        {
            float step = currentSpeed * Time.deltaTime;
            transform.position += dir.normalized * Mathf.Min(step, dist);
        }

        FlipSprite();
    }

    // ══════════════════════════════════════
    //  ★ A-2 핵심: 스탯 기반 속도 계산
    // ══════════════════════════════════════

    private float CalculateSpeed(GameSettings gs)
    {
        // 스탯이 없으면 레거시 방식 (fallback)
        if (charData == null)
        {
            return UnityEngine.Random.Range(gs.racerMinSpeed, gs.racerMaxSpeed);
        }

        TrackData track = gs.currentTrack; // null이면 일반 트랙

        // ── 컨디션 배수 (스탯 보너스 계열에 적용) ──
        float condMul = OddsCalculator.GetConditionMultiplier(charData.charName);
        condMul = Mathf.Max(condMul, 0.3f);  // 안전 가드: 0 근처 나눗셈 방지

        float speed = GetBaseTrackSpeed(gs, track);

        if (gs.useHPSystem)
        {
            // ═══ HP 시스템 (SPEC-006) ═══
            ConsumeHP(gs, track, Time.deltaTime);
            float hpBoost = CalcHPBoost(gs);
            speed *= (1f + (hpBoost + GetPowerBonus(track) + GetBraveBonus(track)) * condMul);
            speed += GetNoiseValue(gs, track) * condMul;
        }
        else
        {
            // ═══ 레거시 시스템 ═══
            speed *= (1f + (GetTypeBonus(gs, track) + GetPowerBonus(track) + GetBraveBonus(track)) * condMul);
            speed += GetNoiseValue(gs, track) * condMul;
            speed -= GetFatigue(gs, track) / condMul;
        }

        speed *= GetSlowZoneMultiplier(track);
        speed *= GetLuckCritMultiplier(gs, track);
        speed *= GetCollisionMultiplier();

        // 최소 속도 보장 + 디버그 경고
        if (speed < 0.15f)
        {
            Debug.LogWarning(string.Format("[RacerController] {0} 속도 바닥! speed={1:F3} condMul={2:F2} progress={3:F2}",
                charData.DisplayName, speed, condMul, OverallProgress));
        }
        return Mathf.Max(speed, 0.1f);
    }

    // ── 기본 속도 (캐릭터 스피드 × 글로벌 × 트랙) ──
    // SpeedMultiplier: 0.8 + charBaseSpeed*0.01  (20=1.0, 15=0.95, 10=0.90)
    private float GetBaseTrackSpeed(GameSettings gs, TrackData track)
    {
        float trackSpeedMul = track != null ? track.speedMultiplier : 1f;
        return charData.SpeedMultiplier * gs.globalSpeedMultiplier * trackSpeedMul;
    }

    // ── 타입 보너스 (구간별 + 트랙 배율) ──
    private float GetTypeBonus(GameSettings gs, TrackData track)
    {
        float progress = OverallProgress;
        int phase = progress < 0.35f ? 0 : progress < 0.70f ? 1 : 2;
        float typeBonus = gs.GetTypeBonus(charData.charType, phase);

        if (track != null)
        {
            float phaseMul = phase == 0 ? track.earlyBonusMultiplier :
                             phase == 1 ? track.midBonusMultiplier :
                                          track.lateBonusMultiplier;
            typeBonus *= phaseMul;
        }
        return typeBonus;
    }

    // ── 트랙 특수: power → 속도 보너스 ──
    private float GetPowerBonus(TrackData track)
    {
        if (track == null) return 0f;
        return (charData.charBasePower / 20f) * track.powerSpeedBonus;
    }

    // ── 트랙 특수: brave → 속도 보너스 ──
    private float GetBraveBonus(TrackData track)
    {
        if (track == null) return 0f;
        return (charData.charBaseBrave / 20f) * track.braveSpeedBonus;
    }

    // ── calm 기반 noise 값 ──
    private float GetNoiseValue(GameSettings gs, TrackData track)
    {
        float trackNoiseMul = track != null ? track.noiseMultiplier : 1f;
        UpdateNoise(gs, trackNoiseMul);
        return noiseValue;
    }

    // ── endurance 기반 후반 피로 ──
    private float GetFatigue(GameSettings gs, TrackData track)
    {
        float trackFatigueMul = track != null ? track.fatigueMultiplier : 1f;
        float progress = OverallProgress;
        float endurance = Mathf.Max(charData.charBaseEndurance, 1f);
        return progress * (1f / endurance) * gs.fatigueFactor * trackFatigueMul;
    }

    // ══════════════════════════════════════
    //  ★ HP 시스템 코어 (SPEC-006)
    // ══════════════════════════════════════

    /// <summary>
    /// HP 소모 처리 (매 프레임 호출).
    /// consumption = (basicRate + activeRate) × √(speedRatio) × deltaTime
    /// </summary>
    private void ConsumeHP(GameSettings gs, TrackData track, float deltaTime)
    {
        if (enduranceHP <= 0f) return; // 이미 탈진

        float progress = OverallProgress;

        // 타입별 파라미터 가져오기
        gs.GetHPParams(charData.charType,
            out float spurtStart, out float activeRate, out _,
            out _, out _, out _);

        // 적극 소모율: spurtStart 이후에만 적용
        float effectiveActiveRate = progress >= spurtStart ? activeRate : 0f;

        // 속도 비율 (댐핑 팩터: √ 사용으로 양성 피드백 방지)
        float baseTrackSpeed = GetBaseTrackSpeed(gs, track);
        float speedRatio = baseTrackSpeed > 0.01f ? currentSpeed / baseTrackSpeed : 1f;
        speedRatio = Mathf.Clamp(speedRatio, 0.1f, 2f);

        // HP 소모량 계산
        float consumption = (gs.basicConsumptionRate + effectiveActiveRate) * Mathf.Sqrt(speedRatio) * deltaTime;
        consumption = Mathf.Min(consumption, enduranceHP); // HP 이하로 내려가지 않음

        enduranceHP -= consumption;
        totalConsumedHP += consumption;
    }

    /// <summary>
    /// HP 소모 비율 기반 속도 부스트 계산.
    /// 가속구간(0~60%): peakBoost × t^accelExp
    /// 감속구간(60~100%): peakBoost × (1-t)^decelExp
    /// 탈진(HP=0): exhaustionFloor (음수)
    /// </summary>
    private float CalcHPBoost(GameSettings gs)
    {
        gs.GetHPParams(charData.charType,
            out _, out _, out float peakBoost,
            out float accelExp, out float decelExp, out float exhaustionFloor);

        float consumedRatio = maxHP > 0f ? totalConsumedHP / maxHP : 0f;
        float threshold = gs.boostThreshold; // 0.6

        float boost;
        if (consumedRatio <= threshold)
        {
            // ── 가속 구간: 0% ~ 60% 소모 ──
            float t = threshold > 0f ? consumedRatio / threshold : 0f;
            boost = peakBoost * Mathf.Pow(t, accelExp);
        }
        else if (enduranceHP > 0f)
        {
            // ── 감속 구간: 60% ~ 100% 소모 ──
            float remain = 1f - threshold;
            float t = remain > 0f ? (consumedRatio - threshold) / remain : 1f;
            t = Mathf.Clamp01(t);
            boost = peakBoost * Mathf.Pow(1f - t, decelExp);
        }
        else
        {
            // ── 탈진: HP = 0 ──
            boost = exhaustionFloor; // 음수값 (예: -0.05)
        }

        hpBoostValue = boost;
        return boost;
    }

    // ── 트랙 중반 감속 구간 ──
    private float GetSlowZoneMultiplier(TrackData track)
    {
        if (track == null || !track.hasMidSlowZone) return 1f;
        float progress = OverallProgress;
        if (progress >= track.midSlowZoneStart && progress <= track.midSlowZoneEnd)
            return track.midSlowZoneSpeedMultiplier;
        return 1f;
    }

    // ── Luck 크리티컬 배율 ──
    private float GetLuckCritMultiplier(GameSettings gs, TrackData track)
    {
        return UpdateLuckCrit(gs, track);
    }

    // ── 충돌 페널티/슬링샷 배율 ──
    private float GetCollisionMultiplier()
    {
        UpdateCollisionTimers();
        return 1f - collisionPenalty + slingshotBoost;
    }

    // ── calm 기반 noise ──
    private void UpdateNoise(GameSettings gs, float trackNoiseMul)
    {
        noiseTimer -= Time.deltaTime;
        if (noiseTimer <= 0f)
        {
            float calm = Mathf.Max(charData.charBaseCalm, 1f);
            float maxNoise = (1f / calm) * gs.noiseFactor * trackNoiseMul * gs.globalSpeedMultiplier;
            noiseValue = UnityEngine.Random.Range(-maxNoise, maxNoise);
            noiseTimer = UnityEngine.Random.Range(0.5f, 1.5f);
        }
    }

    // ── luck 크리티컬 판정 ──
    private float UpdateLuckCrit(GameSettings gs, TrackData track)
    {
        // 크리티컬 진행 중
        if (critBoostRemaining > 0f)
        {
            critBoostRemaining -= Time.deltaTime;
            if (critBoostRemaining <= 0f)
                isCritActive = false;
            return gs.luckCritBoost;
        }

        // 새 판정
        luckTimer -= Time.deltaTime;
        if (luckTimer <= 0f)
        {
            luckTimer = gs.luckCheckInterval;

            float trackLuckMul = track != null ? track.luckMultiplier : 1f;
            float chance = charData.charBaseLuck * gs.luckCritChance * trackLuckMul;

            if (UnityEngine.Random.value < chance)
            {
                critBoostRemaining = gs.luckCritDuration;
                isCritActive = true;

                // ★ Lucky 크리티컬 VFX 표시
                var critVfx = GetComponent<CollisionVFX>();
                if (critVfx == null)
                    critVfx = gameObject.AddComponent<CollisionVFX>();
                critVfx.Show(CollisionVFXType.Crit, 0.8f);

                Debug.Log("★ 크리티컬! " + charData.DisplayName + " (luck:" + charData.charBaseLuck + ")");
                return gs.luckCritBoost;
            }
        }

        return 1f; // 크리티컬 아님
    }

    // ── 충돌 타이머 감소 (값은 A-3 CollisionSystem에서 세팅) ──
    private void UpdateCollisionTimers()
    {
        if (collisionPenaltyTimer > 0f)
        {
            collisionPenaltyTimer -= Time.deltaTime;
            if (collisionPenaltyTimer <= 0f)
                collisionPenalty = 0f;
        }

        if (slingshotTimer > 0f)
        {
            slingshotTimer -= Time.deltaTime;
            if (slingshotTimer <= 0f)
                slingshotBoost = 0f;
        }

        // 공격 쿨다운 감소 + 공격 끝나면 Run으로 복귀
        if (attackCooldown > 0f)
        {
            attackCooldown -= Time.deltaTime;
            if (attackCooldown <= 0f && animator != null && !isFinished)
            {
                // ★ 잔여 Attack Trigger 제거 후 Run 복귀
                animator.ResetTrigger("AttackSlash");
                animator.ResetTrigger("AttackShoot");
                SwapModel(toAttack: false);  // ★ 맨몸 복귀 (Run 트리거 포함)

                // attackModel 없는 경우 fallback
                if (normalModel == null || attackModel == null)
                    animator.SetTrigger("Run");
            }
        }

        // ★ 스킬 타이머
        if (skillActive)
        {
            skillRemainingTime -= Time.deltaTime;
            if (skillRemainingTime <= 0f)
            {
                DeactivateSkill();
            }
        }
    }

    // ══════════════════════════════════════
    //  ★ A-3용: 외부에서 충돌 효과 적용
    // ══════════════════════════════════════

    /// <summary>
    /// 충돌 감속 적용 (CollisionSystem에서 호출)
    /// </summary>
    public void ApplyCollisionPenalty(float penalty, float duration)
    {
        collisionPenalty = Mathf.Clamp01(penalty);
        collisionPenaltyTimer = duration;
    }

    /// <summary>
    /// 슬링샷 가속 적용 (CollisionSystem에서 호출)
    /// </summary>
    public void ApplySlingshot(float boost, float duration)
    {
        slingshotBoost = Mathf.Min(boost, GameSettings.Instance.slingshotMaxBoost);
        slingshotTimer = duration;
    }

    /// <summary>
    /// luck 기반 충돌 회피 판정 (CollisionSystem에서 호출)
    /// </summary>
    public bool TryDodge()
    {
        if (charData == null) return false;
        var gs = GameSettings.Instance;
        TrackData track = gs.currentTrack;
        float trackLuckMul = track != null ? track.luckMultiplier : 1f;
        float chance = charData.charBaseLuck * gs.luckDodgeChance * trackLuckMul;
        return UnityEngine.Random.value < chance;
    }

    /// <summary>
    /// 충돌 승리 시 공격 애니메이션 (CollisionSystem에서 호출)
    /// ★ 스킬 발동 중(skillActive)일 때만 공격 모션 재생
    /// CSV char_weapon: L → AttackSlash, R → AttackShoot
    /// </summary>
    public bool PlayAttackAnim()
    {
        if (animator == null || charData == null) return false;

        // ★ 스킬 발동 중이 아니면 공격 안 함
        if (!skillActive) return false;

        // 쿨다운 체크 (1회만)
        if (attackCooldown > 0f) return false;

        // ★ 먼저 무기 모델로 교체 (Animator도 교체됨)
        SwapModel(toAttack: true);

        // Animator 파라미터 캐싱 (모델 교체 후 재확인)
        attackAnimChecked = false;
        hasSlash = false;
        hasShoot = false;
        if (animator != null)
        {
            foreach (var param in animator.parameters)
            {
                if (param.type == AnimatorControllerParameterType.Trigger)
                {
                    if (param.name == "AttackSlash") hasSlash = true;
                    if (param.name == "AttackShoot") hasShoot = true;
                }
            }
            attackAnimChecked = true;
        }

        // ★ Trigger 큐 클리어 (이전 잔여 Trigger 제거)
        if (hasSlash) animator.ResetTrigger("AttackSlash");
        if (hasShoot) animator.ResetTrigger("AttackShoot");
        animator.ResetTrigger("Run");

        // CSV weapon 기반 선택: L→Slash, R→Shoot
        bool triggered = false;
        if (charData.charWeapon == WeaponHand.Left && hasSlash)
        {
            animator.SetTrigger("AttackSlash");
            triggered = true;
        }
        else if (charData.charWeapon == WeaponHand.Right && hasShoot)
        {
            animator.SetTrigger("AttackShoot");
            triggered = true;
        }
        else if (charData.charWeapon == WeaponHand.None)
        {
            // 무기 없으면 둘 중 하나 (fallback)
            if (hasSlash) { animator.SetTrigger("AttackSlash"); triggered = true; }
            else if (hasShoot) { animator.SetTrigger("AttackShoot"); triggered = true; }
        }

        if (triggered)
        {
            attackCooldown = GameSettings.Instance.attackAnimCooldown;
        }
        else
        {
            // 트리거 실패 시 맨몸 복귀
            SwapModel(toAttack: false);
        }

        return triggered;
    }

    private bool attackAnimChecked = false;
    private bool hasSlash = false;
    private bool hasShoot = false;
    private float attackCooldown = 0f;

    // ══════════════════════════════════════
    //  ★ 스킬 시스템
    // ══════════════════════════════════════

    /// <summary>
    /// 충돌 시 호출 (CollisionSystem에서) → 충돌 횟수 증가 → 조건 달성 시 스킬 발동
    /// </summary>
    public void OnSkillCollisionHit()
    {
        if (skillActive) return;                      // 이미 발동 중이면 무시
        if (charData == null || charData.skillData == null) return;

        skillCollisionCount++;

        if (charData.skillData.CheckCollisionTrigger(skillCollisionCount))
        {
            ActivateSkill();
        }
    }

    /// <summary>
    /// 스킬 발동: 무기 꺼냄 → 공격 프리팹으로 교체 + 타이머 시작
    /// </summary>
    private void ActivateSkill()
    {
        skillActive = true;
        skillRemainingTime = charData.skillData.durationSec;
        skillCollisionCount = 0; // 카운트 리셋 (재발동 가능)

        // ★ 모델 교체 안 함 (공격 시에만 잠깐 무기 표시)

        Debug.Log(string.Format("[스킬 발동] {0} → 각성! ({1}초)",
            charData.DisplayName, charData.skillData.durationSec));
    }

    /// <summary>
    /// 스킬 종료: 맨몸으로 복귀
    /// </summary>
    private void DeactivateSkill()
    {
        skillActive = false;
        skillRemainingTime = 0f;

        // 맨몸 모델 복귀
        SwapModel(toAttack: false);

        if (charData != null)
            Debug.Log(string.Format("[스킬 종료] {0} → 맨몸 복귀", charData.DisplayName));
    }

    /// <summary>
    /// 공격 프리팹 미리 생성 (RaceManager 스폰 후 호출)
    /// 비활성 상태로 자식에 보관
    /// </summary>
    public void SetupAttackModel()
    {
        if (charData == null) return;
        GameObject attackPrefab = charData.LoadAttackPrefab();
        if (attackPrefab == null) return;

        // 현재 모델을 normalModel로 지정
        normalModel = FindModel();

        // ★ 맨몸 Animator 캐싱 (루트에 있음)
        normalAnimator = GetComponentInChildren<Animator>();

        // 공격 프리팹에서 모델 복제 → 자식으로 추가
        GameObject attackObj = Instantiate(attackPrefab, transform);
        attackModel = attackObj;
        attackModel.transform.localPosition = Vector3.zero;

        // ★ 공격 Animator 캐싱 (비활성 전에 가져와야 함)
        attackAnimator = attackModel.GetComponentInChildren<Animator>();

        attackModel.SetActive(false);

        Debug.Log(string.Format("[스킬 준비] {0} → 공격 프리팹 로드 완료", charData.DisplayName));
    }

    /// <summary>
    /// 모델 교체 (맨몸 ↔ 무기)
    /// </summary>
    private void SwapModel(bool toAttack)
    {
        if (normalModel == null || attackModel == null) return;

        normalModel.SetActive(!toAttack);
        attackModel.SetActive(toAttack);

        // ★ 캐싱된 Animator 사용 (루트 vs 공격모델)
        animator = toAttack ? attackAnimator : normalAnimator;
        attackAnimChecked = false; // Animator 파라미터 캐시 리셋

        if (animator != null)
            animator.SetTrigger("Run");
    }

    /// <summary>
    /// 현재 모델(자식 중 SpriteRenderer가 있는 것) 찾기
    /// </summary>
    private GameObject FindModel()
    {
        // 자식 중 Animator 또는 SpriteRenderer가 있는 첫 번째 오브젝트
        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            if (child.GetComponentInChildren<SpriteRenderer>() != null)
                return child.gameObject;
        }
        return null;
    }

    // ══════════════════════════════════════
    //  기본 속도 (초기값용)
    // ══════════════════════════════════════

    private float GetBaseSpeed()
    {
        if (charData != null)
        {
            float trackMul = GameSettings.Instance.currentTrack != null
                ? GameSettings.Instance.currentTrack.speedMultiplier : 1f;
            return charData.SpeedMultiplier * GameSettings.Instance.globalSpeedMultiplier * trackMul;
        }
        return GameSettings.Instance.racerMinSpeed;
    }

    // ══════════════════════════════════════
    //  경로 이탈 / 스프라이트 / 설정 적용
    // ══════════════════════════════════════

    private void UpdateDeviation()
    {
        float weight = GameSettings.Instance.pathDeviation;
        if (weight <= 0f) { deviationOffset = 0f; return; }

        deviationTimer -= Time.deltaTime;
        if (deviationTimer <= 0f)
        {
            deviationTarget = UnityEngine.Random.Range(-weight, weight);
            deviationTimer = UnityEngine.Random.Range(0.3f, 1.0f);
        }
        deviationOffset = Mathf.Lerp(deviationOffset, deviationTarget, Time.deltaTime * 3f);
    }

    private void FlipSprite()
    {
        Vector3 movement = transform.position - lastPosition;
        if (Mathf.Abs(movement.x) > 0.001f)
        {
            float scaleX = Mathf.Abs(transform.localScale.x);
            transform.localScale = new Vector3(
                movement.x < 0 ? scaleX : -scaleX,
                transform.localScale.y,
                transform.localScale.z
            );

            float parentSign = Mathf.Sign(transform.localScale.x);
            foreach (Transform child in transform)
            {
                if (child.GetComponent<TextMesh>() != null)
                {
                    Vector3 ls = child.localScale;
                    ls.x = Mathf.Abs(ls.x) * parentSign;
                    child.localScale = ls;
                }
            }
        }
        lastPosition = transform.position;
    }

    private void ApplyLiveSettings()
    {
        var s = GameSettings.Instance;
        float absScale = Mathf.Abs(s.characterScale);
        float sign = transform.localScale.x >= 0 ? 1f : -1f;
        transform.localScale = new Vector3(sign * absScale, absScale, 1f);

        laneOffset = (racerIndex - GameConstants.RACER_COUNT / 2f) * s.laneOffset;

        Transform lb = transform.Find("RaceLabel");
        if (lb != null)
        {
            lb.localPosition = new Vector3(0, s.labelHeight, 0);
            TextMesh tm = lb.GetComponent<TextMesh>();
            if (tm != null) tm.characterSize = s.labelSize;
        }
    }

    private Vector3 GetOffsetPosition(int wpIndex)
    {
        Vector3 wpPos = waypoints[wpIndex].position;
        Vector3 trackCenter = RaceManager.TrackCenter;
        Vector3 outward = (wpPos - trackCenter).normalized;
        return wpPos + outward * (laneOffset + deviationOffset);
    }
}