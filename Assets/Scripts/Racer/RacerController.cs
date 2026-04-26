using UnityEngine;
using System;
using System.Collections.Generic;

public partial class RacerController : MonoBehaviour
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
    private float skillCooldownTimer = 0f;     // 발동 후 재발동 쿨다운 (0이면 즉시 재발동 가능)
    private GameObject normalModel;            // 맨몸 모델
    private GameObject attackModel;            // 무기 든 모델 (미리 생성, 비활성)
    private Animator normalAnimator;           // 맨몸 Animator
    private Animator attackAnimator;           // 무기 Animator

    // ── ★ 패시브 스킬 런타임 상태 (Group C용) ──
    private bool passiveConditionActive = false;
    private float passiveCooldownTimer = 0f;

    // ── ★ HP 시스템 (SPEC-006) ──
    private float enduranceHP;                 // 현재 HP (maxHP에서 시작, 0까지 감소)
    private float maxHP;                       // 50 + charEndurance × 2.5
    private float totalConsumedHP;             // 누적 소모량 (부스트 비율 계산용)
    private float hpBoostValue;                // 현재 프레임의 HP 부스트 값 (디버그용)
    private float slipstreamBlend;             // 슬립스트림 페이드 0~1 (전체 타입, 거리 기반)
    private int currentRank;                   // 현재 순위 1~12 (Phase 4)

    // ── CP 시스템 (Calm Points) ──
    private float calmPoints;                  // 현재 CP
    private float maxCP;                       // 최대 CP (calm × cpMultiplier)

    // ── 외부 접근 ──
    public int RacerIndex => racerIndex;
    public bool IsFinished => isFinished;
    public int FinishOrder { get; set; } = -1;
    public float CurrentSpeed => currentSpeed;
    public int CurrentLap => currentLap;
    public CharacterData CharData => charData;
    public bool IsCritActive => isCritActive;
    public bool SkillActive => skillActive;           // 스킬 발동 중 여부 (외부 참조용)
    /// <summary>CollisionWin 타입 스킬 활성화 여부 (CollisionSystem에서 충돌 승리 판정용)</summary>
    public bool IsCollisionSkillArmed
    {
        get
        {
            if (!skillActive) return false;
            // V4: effectType == CollisionWin인 경우만 충돌 자동 승리 부여
            if (charDataV4?.skillData != null && charDataV4.skillData.triggerType != SkillTriggerType.None)
                return charDataV4.skillData.effectType == SkillEffectType.CollisionWin;
            return true; // 구버전 폴백: skillActive면 CollisionWin 취급
        }
    }
    public int SkillCollisionCount => skillCollisionCount;
    public float CollisionPenalty => collisionPenalty;
    public float SlingshotBoost => slingshotBoost;
    // ── HP 시스템 외부 접근 ──
    public float EnduranceHP => enduranceHP;
    public float MaxHP => maxHP;
    public float TotalConsumedHP => totalConsumedHP;
    public float HPBoostValue => hpBoostValue;
    public int CurrentRank { get => currentRank; set => currentRank = value; }

    // ── CP 시스템 외부 접근 ──
    public float CalmPoints => calmPoints;
    public float MaxCPValue => maxCP;
    public float CPRatio => maxCP > 0f ? calmPoints / maxCP : 0f;

    // ── 트랙바 좌우 흔들림 ──
    /// <summary>
    /// 캐릭터의 레인 오프셋 + 경로 이탈값.
    /// 트랙바 원의 X 좌표에 그대로 매핑하여 좌우로 펼치기.
    /// </summary>
    public float LateralOffset => laneOffset + deviationOffset;

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
            int wpCount = waypoints.Count;

            // headingToFinish: 마지막 WP → 결승선(WP0) 구간 보간
            if (headingToFinish)
            {
                int lastWP = wpCount - 1;
                Vector3 segDir = waypoints[0].position - waypoints[lastWP].position;
                float segLen = segDir.magnitude;
                Vector3 toRacer = transform.position - waypoints[lastWP].position;
                float projected = (segLen > 0.01f)
                    ? Vector3.Dot(toRacer, segDir.normalized) / segLen : 1f;
                float frac = Mathf.Clamp01(projected);
                float finalLapProg = (wpCount - 1 + frac) / wpCount;
                float rawFinish = Mathf.Clamp01((totalLaps - 1 + finalLapProg) / totalLaps);
                _maxOverallProgress = Mathf.Max(_maxOverallProgress, rawFinish);
                return _maxOverallProgress;
            }

            // 일반 구간: prevWP → currentWP 중심선 투영
            int targetWP = currentWP;
            int prevWP = currentWP - 1;
            if (prevWP < 0) prevWP = (currentLap > 0) ? wpCount - 1 : 0;

            float segLength = Vector3.Distance(
                waypoints[prevWP].position, waypoints[targetWP].position);
            Vector3 dir = waypoints[targetWP].position - waypoints[prevWP].position;
            Vector3 toPos = transform.position - waypoints[prevWP].position;
            float proj = (segLength > 0.01f)
                ? Vector3.Dot(toPos, dir.normalized) / segLength : 0f;
            float frac2 = Mathf.Clamp01(proj);

            float lapProgress = (currentWP + frac2) / wpCount;
            float raw = Mathf.Clamp01((currentLap + lapProgress) / totalLaps);
            _maxOverallProgress = Mathf.Max(_maxOverallProgress, raw);
            return _maxOverallProgress;
        }
    }

    // ── 트랙바 진행률: 이동 거리 누적 방식 ──
    private float _cumulativeDistance = 0f;  // 누적 이동 거리
    private float _maxOverallProgress = 0f; // OverallProgress 단조 증가 보장
    private float _oneLapDistance = 0f;      // 1바퀴 트랙 총 거리

    /// <summary>
    /// 트랙바 UI용 진행률 (0~1).
    /// ★ 캐릭터의 실제 이동 거리를 그대로 누적 → 별도 계산 없음.
    ///   - 캐릭터 움직임과 100% 동기화
    ///   - 바퀴 수가 늘면 자동으로 압축 (같은 바 높이, 더 긴 총 거리)
    /// </summary>
    public float SmoothProgress
    {
        get
        {
            if (isFinished) return 1f;
            float totalDist = _oneLapDistance * GetTotalLaps();
            if (totalDist <= 0f) return 0f;
            return Mathf.Clamp01(_cumulativeDistance / totalDist);
        }
    }

    /// <summary>
    /// 1바퀴 트랙 거리 계산 (웨이포인트 중심선 기준).
    /// Initialize 후 한 번만 호출.
    /// </summary>
    private void CalculateOneLapDistance()
    {
        _oneLapDistance = 0f;
        if (waypoints == null || waypoints.Count < 2) return;
        for (int i = 0; i < waypoints.Count; i++)
        {
            int next = (i + 1) % waypoints.Count;
            _oneLapDistance += Vector3.Distance(
                waypoints[i].position, waypoints[next].position);
        }
    }

    public int GetTotalLaps()
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
        currentLap = 0; currentWP = 1; // WP1부터 시작 — WP0(결승선)은 측면이라 방향 틀어짐 방지
        _cumulativeDistance = 0f;       // 누적 거리 리셋
        _maxOverallProgress = 0f;       // 진행률 최대값 리셋
        CalculateOneLapDistance();       // 1바퀴 트랙 거리 계산
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
        skillCooldownTimer = 0f;
        passiveConditionActive = false;
        passiveCooldownTimer = 0f;
        DeactivateSkill();
        // ── Step F.5: SKILL 리셋 로그 (라운드 간 쿨타임 누수 감시) ──
        if (charData != null || charDataV4 != null)
            Debug.Log(string.Format("[SKILL] {0} RESET", charDataV4?.charId ?? charData?.charId ?? "?"));

        // 흔들림 초기화
        deviationOffset = 0f;
        deviationTarget = 0f;
        deviationTimer = 0f;

        // HP / V4 스태미나 초기화
        if (charData != null)
        {
            var gsInst = GameSettings.Instance;
            if (gsInst.useV4RaceSystem)
            {
                InitV4();
            }
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
        attackCooldown = 0f;

        // 스킬
        skillCollisionCount = 0; skillActive = false; skillRemainingTime = 0f;
        passiveConditionActive = false; passiveCooldownTimer = 0f;
        v4SkillHpTriggered = false; v4SkillRankTriggered = false;
        skillCooldownTimer = 0f;
        DeactivateSkill();
        StopWandering(); // ★ 배회 중이었다면 중단 (백테스트/라운드 리셋 안전)

        // HP 시스템
        enduranceHP = 0f; maxHP = 0f; totalConsumedHP = 0f;
        hpBoostValue = 0f; slipstreamBlend = 0f; currentRank = 0;

        // CP 시스템
        calmPoints = 0f; maxCP = 0f;

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

        // ★ 배회 모드 — isRacing 로직보다 먼저 처리하고 return
        if (_isWandering) { UpdateWander(); FlipSprite(); return; }

        if (!isRacing || isFinished) return;
        if (waypoints == null || waypoints.Count == 0) return;

        var gs = GameSettings.Instance;

        // ── 0) V4 업데이트 ──
        if (gs.useV4RaceSystem)
        {
            UpdateV4(Time.deltaTime);
        }

        // ── 1) 스탯 기반 속도 계산 ──
        float finalSpeed = CalculateSpeed(gs);
        // V4: CalcSpeedV4 내부에서 currentSpeed(Lerp 상태) 직접 관리
        // finalSpeed에는 크리티컬/충돌 등 일시적 배율이 포함됨
        // currentSpeed를 덮어쓰면 배율이 다음 프레임 Lerp에 누적 → 순간이동 버그
        // 이동에만 finalSpeed 사용, currentSpeed는 CalcSpeedV4가 관리

        // ── 2) 경로 이탈 업데이트 ──
        UpdateDeviation();

        // ── 3) 이동 ──
        // V4: finalSpeed = 크리티컬/충돌 배율 포함된 최종 출력 속도
        float moveSpeed = finalSpeed;

        Vector3 target = headingToFinish ? GetOffsetPosition(0) : GetOffsetPosition(currentWP);
        Vector3 dir = target - transform.position;
        float dist = dir.magnitude;

        if (dist < GameSettings.Instance.waypointArrivalDist)
        {
            if (headingToFinish)
            {
                transform.position = target;

                // V4: 최종 스퍼트 종료 로그 (골인 시점 HP 기록)
                if (GameSettings.Instance?.useV4RaceSystem == true && v4IsSpurting && charDataV4 != null)
                {
                    float _r = v4MaxStamina > 0 ? v4CurrentStamina / v4MaxStamina : 0f;
                    string _name = charData?.DisplayName ?? charDataV4.charId;
                    string _msg  = $"{_name} 파이널스퍼트 종료 골인 HP:{_r:P0}({v4CurrentStamina:F0}/{v4MaxStamina:F0})";
                    Debug.Log($"[V4 FinalSpurt 종료] {_msg}");
                    var _ov = RaceManager.Instance?.GetComponent<RaceDebugOverlay>();
                    _ov?.LogEvent(RaceDebugOverlay.EventType.Spurt, _msg);
                }

                isFinished = true; isRacing = false; currentSpeed = 0f;
                // ★ 골인 모션은 OnFinished 구독자(RaceManager)가 입상 여부에 따라 결정 (SPEC-026)
                // → 여기서는 Idle 트리거 호출 안 함 (구독자가 AttackMagic / Death 트리거)
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
                // V4 Lap 트리거 체크
                if (GameSettings.Instance?.useV4RaceSystem == true && !skillActive && charDataV4?.skillData != null)
                {
                    if (charDataV4.skillData.CheckLapTrigger(currentLap))
                        ActivateSkill();
                }
            }
        }
        else
        {
            float step = moveSpeed * Time.deltaTime;
            float actualStep = Mathf.Min(step, dist);
            transform.position += dir.normalized * actualStep;
            _cumulativeDistance += actualStep;   // ★ 실제 이동 거리 누적
        }

        FlipSprite();
    }

    // ══════════════════════════════════════
    //  ★ A-2 핵심: 스탯 기반 속도 계산
    // ══════════════════════════════════════

    private float CalculateSpeed(GameSettings gs)
    {
        // 스탯이 없으면 fallback
        if (charData == null)
        {
            return gs.globalSpeedMultiplier;
        }

        // ═══ Race V4: 5대 스탯 기반 ═══
        return CalcSpeedV4(gs.v4Settings);
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
        noiseTimer -= Time.deltaTime * gs.globalSpeedMultiplier;
        if (noiseTimer <= 0f)
        {
            float calm = Mathf.Max(charData.charBaseCalm, 1f);
            float maxNoise = (1f / calm) * gs.noiseFactor * trackNoiseMul * gs.globalSpeedMultiplier;

            // CP/HP 불안정 배율 적용 (곱연산)
            float cpNoiseMul = gs.GetCPNoiseMul(CPRatio);
            float hpNoiseMul = gs.GetHPNoiseMul(maxHP > 0 ? enduranceHP / maxHP : 1f);
            maxNoise *= cpNoiseMul * hpNoiseMul;

            noiseValue = UnityEngine.Random.Range(-maxNoise, maxNoise);
            noiseTimer = UnityEngine.Random.Range(0.5f, 1.5f);
        }
    }

    // ── luck 크리티컬 판정 ──
    private float UpdateLuckCrit(GameSettings gs, TrackData track)
    {
        float gameDt = Time.deltaTime * gs.globalSpeedMultiplier;

        // 크리티컬 진행 중
        if (critBoostRemaining > 0f)
        {
            critBoostRemaining -= gameDt;
            if (critBoostRemaining <= 0f)
                isCritActive = false;
            return gs.luckCritBoost;
        }

        // 새 판정
        luckTimer -= gameDt;
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
        float gameDt = Time.deltaTime * GameSettings.Instance.globalSpeedMultiplier;

        if (collisionPenaltyTimer > 0f)
        {
            collisionPenaltyTimer -= gameDt;
            if (collisionPenaltyTimer <= 0f)
                collisionPenalty = 0f;
        }

        if (slingshotTimer > 0f)
        {
            slingshotTimer -= gameDt;
            if (slingshotTimer <= 0f)
                slingshotBoost = 0f;
        }

        // 공격 쿨다운 감소 + 공격 끝나면 Run으로 복귀
        if (attackCooldown > 0f)
        {
            attackCooldown -= gameDt;
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
            skillRemainingTime -= gameDt;
            if (skillRemainingTime <= 0f)
            {
                DeactivateSkill();
            }
        }

        // ★ 스킬 쿨다운 감소
        if (skillCooldownTimer > 0f)
        {
            skillCooldownTimer -= gameDt;
            if (skillCooldownTimer < 0f) skillCooldownTimer = 0f;
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
    /// int(지능) 기반 충돌 회피 판정 (CollisionSystem에서 호출)
    /// "영리하게 피했다" — luck이 아닌 intelligence로 발동
    /// </summary>
    public bool TryDodge()
    {
        if (charData == null) return false;
        var gs = GameSettings.Instance;
        var gs4 = gs?.v4Settings;
        if (gs4 == null) return false;
        var charV4 = CharacterDatabaseV4.FindById(charData.charId);
        if (charV4 == null) return false;
        TrackData track = gs.currentTrack;
        float trackMul = track != null ? track.luckMultiplier : 1f;
        float chance = charV4.v4Intelligence * gs4.v4_intDodgeChance * trackMul;
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

    private bool hasSlash = false;
    private bool hasShoot = false;
    private float attackCooldown = 0f;

    // ══════════════════════════════════════
    //  ★ 스킬 시스템
    // ══════════════════════════════════════

    /// <summary>
    /// 충돌 시 호출 (CollisionSystem에서) → 방향 필터 후 카운트 증가 → 조건 달성 시 스킬 발동.
    /// ctx.isChasing true = 뒤→앞 추격자, false = 앞→뒤 피격자.
    /// </summary>
    public void OnSkillCollisionHit(CollisionContext ctx)
    {
        if (skillActive || skillCooldownTimer > 0f) return;
        // V4 우선 사용, 없으면 구버전 폴백
        SkillData sd = (charDataV4?.skillData != null && charDataV4.skillData.triggerType != SkillTriggerType.None)
            ? charDataV4.skillData
            : charData?.skillData;
        if (sd == null) return;

        // 방향별 필터
        bool shouldCount;
        switch (sd.triggerType)
        {
            case SkillTriggerType.E_Skill_Collision:  shouldCount = true; break;
            case SkillTriggerType.E_Skill_ChaseHit:   shouldCount = ctx.isChasing; break;
            case SkillTriggerType.E_Skill_ChasedHit:  shouldCount = !ctx.isChasing; break;
            default: return;
        }
        if (!shouldCount) return;

        skillCollisionCount++;
        if (sd.CheckCollisionTrigger(skillCollisionCount))
            ActivateSkill();
    }

    /// <summary>
    /// 스킬 발동: effectType에 따라 즉발(HpHeal) 또는 타이머 기반 효과 처리
    /// </summary>
    private void ActivateSkill()
    {
        // V4 우선 사용, 없으면 구버전 폴백
        SkillData sd = (charDataV4?.skillData != null && charDataV4.skillData.triggerType != SkillTriggerType.None)
            ? charDataV4.skillData
            : charData?.skillData;
        if (sd == null) return;

        skillCollisionCount = 0; // 카운트 리셋 (재발동 가능)
        string displayName = charData?.DisplayName ?? charDataV4?.charId ?? "?";

        // HpHeal: 즉발 회복 — skillActive 불필요, 쿨타임은 아래 공통 블록에서 설정
        if (sd.effectType == SkillEffectType.HpHeal)
        {
            float heal = v4MaxStamina > 0f ? v4MaxStamina * sd.effectValue
                       : maxHP > 0f        ? maxHP * sd.effectValue : 0f;
            v4CurrentStamina = Mathf.Min(v4MaxStamina, v4CurrentStamina + heal);
            enduranceHP = v4CurrentStamina;
            Debug.Log(string.Format("[스킬 발동] {0} → HP 즉발 회복 +{1:P0} ({2:F0}HP)",
                displayName, sd.effectValue, heal));
        }
        else
        {
            // 타이머 기반 효과 (CollisionWin / SpeedBoost / DrainReduce)
            skillActive = true;
            skillRemainingTime = sd.durationSec;
            Debug.Log(string.Format("[스킬 발동] {0} → {1} ({2}초)",
                displayName, sd.effectType, sd.durationSec));
        }

        // 쿨다운 시작 (거리별 LapScale 지수 0.35 적용) — 모든 효과 공통
        var gs4 = GameSettings.Instance?.v4Settings;
        float realCd = 0f;
        if (gs4 != null && sd.cooldownSec > 0f)
        {
            float cdLapScale = gs4.LapScale(GetTotalLaps(), 0.35f);
            realCd = sd.cooldownSec * cdLapScale;
            skillCooldownTimer = realCd;
        }

        // ── Step F.5: SKILL 집계용 로그 (AutoRaceRunner grep 집계) ──
        Debug.Log(string.Format("[SKILL] L{0} {1} ACTIVATE trigger={2} val={3} effect={4} cd={5:F1}s",
            GetTotalLaps(), charDataV4?.charId ?? charData?.charId ?? "?",
            sd.triggerType, sd.triggerValue, sd.effectType, realCd));
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
        return GameSettings.Instance.globalSpeedMultiplier;
    }

    // ══════════════════════════════════════
    //  경로 이탈 / 스프라이트 / 설정 적용
    // ══════════════════════════════════════

    private void UpdateDeviation()
    {
        var gs = GameSettings.Instance;
        float weight = gs.pathDeviation;
        if (weight <= 0f) { deviationOffset = 0f; return; }

        float gameDt = Time.deltaTime * gs.globalSpeedMultiplier;
        deviationTimer -= gameDt;
        if (deviationTimer <= 0f)
        {
            deviationTarget = UnityEngine.Random.Range(-weight, weight);
            deviationTimer = UnityEngine.Random.Range(0.3f, 1.0f);
        }
        deviationOffset = Mathf.Lerp(deviationOffset, deviationTarget, gameDt * 3f);
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