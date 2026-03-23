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
    private GameObject normalModel;            // 맨몸 모델
    private GameObject attackModel;            // 무기 든 모델 (미리 생성, 비활성)
    private Animator normalAnimator;           // 맨몸 Animator
    private Animator attackAnimator;           // 무기 Animator

    // ── ★ HP 시스템 (SPEC-006) ──
    private float enduranceHP;                 // 현재 HP (maxHP에서 시작, 0까지 감소)
    private float maxHP;                       // 50 + charEndurance × 2.5
    private float totalConsumedHP;             // 누적 소모량 (부스트 비율 계산용)
    private float hpBoostValue;                // 현재 프레임의 HP 부스트 값 (디버그용)
    private float slipstreamBlend;             // 슬립스트림 페이드 0~1 (전체 타입, 거리 기반)
    private int currentRank;                   // 현재 순위 1~12 (Phase 4)
    private bool isSprintMode = false;         // 전력질주 상태 (SPEC-RC-002: Burst Lerp용)

    // ── Race V3 시스템 ──
    private float v3SprintAccelProgress = 0f;  // V3 전력질주 가속 진행률 0~1
    private bool v3IsSprintActive = false;     // V3 전력질주 활성 여부

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
    public int SkillCollisionCount => skillCollisionCount;
    public float CollisionPenalty => collisionPenalty;
    public float SlingshotBoost => slingshotBoost;
    public bool V3IsSprintActive => v3IsSprintActive;
    public float V3SprintAccelProgress => v3SprintAccelProgress;

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
            float lapProgress = (float)currentWP / waypoints.Count;
            return (currentLap + lapProgress) / totalLaps;
        }
    }

    // ── 트랙바 진행률: 이동 거리 누적 방식 ──
    private float _cumulativeDistance = 0f;  // 누적 이동 거리
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
        currentLap = 0; currentWP = 0;
        _cumulativeDistance = 0f;       // 누적 거리 리셋
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
        DeactivateSkill();

        // 흔들림 초기화
        deviationOffset = 0f;
        deviationTarget = 0f;
        deviationTimer = 0f;

        // V3 스프린트 상태 리셋
        v3SprintAccelProgress = 0f;
        v3IsSprintActive = false;

        // HP / V3 / V4 스태미나 초기화
        if (charData != null)
        {
            var gsInst = GameSettings.Instance;
            if (gsInst.useV4RaceSystem)
            {
                InitV4();
            }
            else if (gsInst.useV3RaceSystem)
            {
                // V3: 스태미나 = staminaBase + endurance × staminaPerEndurance
                maxHP = gsInst.v3Settings.v3_staminaBase + charData.charBaseEndurance * gsInst.v3Settings.v3_staminaPerEndurance;
                enduranceHP = maxHP;
                totalConsumedHP = 0f;
                hpBoostValue = 0f;
                slipstreamBlend = 0f;
                currentRank = racerIndex + 1;
                maxCP = charData.charBaseCalm * gsInst.cpMultiplier;
                calmPoints = maxCP;
            }
            else if (gsInst.useHPSystem)
            {
                int totalLaps = GetTotalLaps();
                maxHP = gsInst.CalcMaxHP(charData.charBaseEndurance, totalLaps);
                enduranceHP = maxHP;
                totalConsumedHP = 0f;
                hpBoostValue = 0f;
                slipstreamBlend = 0f;
                currentRank = racerIndex + 1;
                maxCP = charData.charBaseCalm * gsInst.cpMultiplier;
                calmPoints = maxCP;
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
        DeactivateSkill();

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

        if (!isRacing || isFinished) return;
        if (waypoints == null || waypoints.Count == 0) return;

        var gs = GameSettings.Instance;

        // ── 0) V4 업데이트 또는 HP 시스템 ──
        if (gs.useV4RaceSystem)
        {
            UpdateV4(Time.deltaTime);
        }
        else if (gs.useHPSystem && charData != null)
        {
            UpdateRank();
            UpdateSlipstreamBlend(Time.deltaTime);
            ConsumeCP(gs, Time.deltaTime);
        }

        // ── 1) 스탯 기반 속도 계산 ──
        float finalSpeed = CalculateSpeed(gs);
        if (gs.useV4RaceSystem)
        {
            // V4: CalcSpeedV4 내부에서 currentSpeed(Lerp 상태) 직접 관리
            // finalSpeed에는 크리티컬/충돌 등 일시적 배율이 포함됨
            // currentSpeed를 덮어쓰면 배율이 다음 프레임 Lerp에 누적 → 순간이동 버그
            // 이동에만 finalSpeed 사용, currentSpeed는 CalcSpeedV4가 관리
        }
        else
        {
            // [SPEC-RC-002] 스프린트 진입 시 Burst Lerp로 즉각 가속 연출
            float effectiveLerp = isSprintMode
                ? gs.raceSpeedLerp * gs.sprintBurstLerpMult
                : gs.raceSpeedLerp;
            currentSpeed = Mathf.Lerp(currentSpeed, finalSpeed, Time.deltaTime * effectiveLerp);
        }

        // ── 2) 경로 이탈 업데이트 ──
        UpdateDeviation();

        // ── 3) 이동 ──
        // V4: finalSpeed = 크리티컬/충돌 배율 포함된 최종 출력 속도
        // Legacy: currentSpeed = Lerp 완료된 이동 속도
        float moveSpeed = gs.useV4RaceSystem ? finalSpeed : currentSpeed;

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
        // 스탯이 없으면 레거시 방식 (fallback)
        if (charData == null)
        {
            return gs.globalSpeedMultiplier;
        }

        TrackData track = gs.currentTrack; // null이면 일반 트랙

        // ── 컨디션 배수 (스탯 보너스 계열에 적용) ──
        float condMul = OddsCalculator.GetConditionMultiplier(charData.charId);
        condMul = Mathf.Max(condMul, 0.3f);  // 안전 가드: 0 근처 나눗셈 방지

        float speed = GetBaseTrackSpeed(gs, track);

        if (gs.useV4RaceSystem)
        {
            // ═══ Race V4: 5대 스탯 기반 ═══
            return CalcSpeedV4(gs.v4Settings);
        }
        else if (gs.useV3RaceSystem)
        {
            // ═══ Race V3: 스탯 기반 재설계 ═══
            speed = CalcSpeedV3(gs, track, condMul);
        }
        else if (gs.useHPSystem)
        {
            // ═══ 속도 압축 (V1/V2 공유) ═══
            // 캐릭터 간 기본 속도 차이를 줄여 부스트/전력질주가 역전 가능하게
            if (gs.hpSpeedCompress > 0f)
            {
                float trackSpeedMul = track != null ? track.speedMultiplier : 1f;
                float midSpeed = 0.905f * gs.globalSpeedMultiplier * trackSpeedMul;
                speed = Mathf.Lerp(speed, midSpeed, gs.hpSpeedCompress);
            }

            {
                // ═══ Type 1: 기존 HP 부스트 경로 ═══
                ConsumeHP(gs, track, Time.deltaTime);
                float hpBoost = CalcHPBoost(gs);
                float earlyBonus = gs.GetHPEarlyBonus(charData.charType, OverallProgress);
                float cpEff = gs.GetCPEfficiency(CPRatio);
                float ssBonus = gs.GetSlipstreamBonus(charData.charType, slipstreamBlend, cpEff);
                speed *= (1f + (hpBoost + earlyBonus + ssBonus + GetPowerBonus(track) + GetBraveBonus(track)) * condMul);
                speed += GetNoiseValue(gs, track) * condMul;
            }
        }
        else
        {
            // ═══ 레거시 시스템 ═══
            speed *= (1f + (GetTypeBonus(gs, track) + GetPowerBonus(track) + GetBraveBonus(track)) * condMul);
            speed += GetNoiseValue(gs, track) * condMul;
            speed -= GetFatigue(gs, track) / condMul;
        }

        // ═══ 초반 대형: 타입별 포지션 정렬 ═══
        {
            int totalRacers = RaceManager.Instance != null
                ? RaceManager.Instance.Racers.Count : 12;
            float formationMod = gs.GetFormationModifier(
                charData.charType, OverallProgress, currentRank, totalRacers);
            speed *= (1f + formationMod);
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
    /// HP 소모 처리 (매 프레임 호출). 4페이즈 전략 분기.
    /// 1랩 미만: Legacy 존 기반 로직 / 포지셔닝 / 대형유지 / 전략
    /// </summary>
    private void ConsumeHP(GameSettings gs, TrackData track, float deltaTime)
    {
        if (enduranceHP <= 0f) return;

        int totalLaps = GetTotalLaps();

        // 1랩 이하: 기존 존 기반 로직 유지 (테스트용)
        if (totalLaps < 2)
        {
            ConsumeHP_Legacy(gs, track, deltaTime);
            return;
        }

        float totalProgress = TotalProgress;
        if (totalProgress < gs.positioningLapEnd)
            ConsumeHP_Positioning(gs, track, deltaTime);
        else if (totalProgress < gs.formationHoldLapEnd)
            ConsumeHP_FormationHold(gs, track, deltaTime);
        else
            ConsumeHP_Strategy(gs, track, deltaTime);
    }

    // ─── 기존 존 기반 로직 (1랩 이하 폴백) ──────────────────────────
    private void ConsumeHP_Legacy(GameSettings gs, TrackData track, float deltaTime)
    {
        float progress = OverallProgress;

        gs.GetHPParams(charData.charType,
            out float spurtStart, out float activeRate, out _,
            out _, out _, out _);
        gs.GetZoneParams(charData.charType,
            out float targetZonePct, out float inZoneRate, out float outZoneRate);

        bool inSpurt = spurtStart > 0f && progress >= (1f - spurtStart);

        // Leader 조건부 스퍼트: HP 잔량 미달이면 스퍼트 포기
        if (inSpurt && charData.charType == CharacterType.Leader
            && maxHP > 0f && enduranceHP / maxHP < gs.leaderSpurtMinHP)
            inSpurt = false;

        float normalRate;
        if (targetZonePct <= 0f)
        {
            normalRate = inZoneRate; // Reckoner: 항상 보존
        }
        else
        {
            int total = RaceManager.Instance != null
                ? RaceManager.Instance.Racers.Count : 12;
            int targetMaxRank = Mathf.Max(1, Mathf.CeilToInt(total * targetZonePct));
            normalRate = (currentRank <= targetMaxRank) ? inZoneRate : outZoneRate;
        }

        float rate;
        if (inSpurt)
        {
            float spurtThreshold = 1f - spurtStart;
            float spurtProgress = Mathf.Clamp01((progress - spurtThreshold) / spurtStart);
            rate = Mathf.Lerp(normalRate, activeRate, spurtProgress);
        }
        else
        {
            rate = normalRate;
        }

        ApplyHPConsumption(gs, track, deltaTime, rate);
    }

    // ─── 페이즈 1: 포지셔닝 (TotalProgress < positioningLapEnd) ─────
    // ★ 순위 기반 대신 타입 기반으로 HP 소모율 결정 (출발 직후 순위 부정확 문제 해결)
    //   도주: 풀스프린트 → HP 부스트 빠르게 쌓임 → 선두 확보
    //   선행: 중간 스프린트 → 2번째 그룹 자연 형성
    //   선입: 보존 → 후방 유지
    //   추입: 최소 소모 → 최후방 배치
    private void ConsumeHP_Positioning(GameSettings gs, TrackData track, float deltaTime)
    {
        gs.GetHPParams(charData.charType,
            out _, out float activeRate, out _, out _, out _, out _);
        gs.GetZoneParams(charData.charType,
            out _, out float inZoneRate, out _);

        float rate;
        switch (charData.charType)
        {
            case CharacterType.Runner:
                // 도주: 무조건 스프린트 → 선두권 차지
                rate = activeRate;
                break;
            case CharacterType.Leader:
                // 선행: 적당히 스프린트 → 도주 바로 뒤 포지션
                rate = Mathf.Lerp(inZoneRate, activeRate, 0.6f);
                break;
            case CharacterType.Chaser:
                // 선입: 보존 → 중후반 대기 포지션
                rate = inZoneRate;
                break;
            default: // Reckoner
                // 추입: 최소 소모 → 최후방 자리잡기
                rate = Mathf.Max(gs.basicConsumptionRate, inZoneRate * 0.4f);
                break;
        }

        ApplyHPConsumption(gs, track, deltaTime, rate);
    }

    // ─── 페이즈 2: 대형 유지 (positioningLapEnd ~ formationHoldLapEnd) ─
    private void ConsumeHP_FormationHold(GameSettings gs, TrackData track, float deltaTime)
    {
        gs.GetHPParams(charData.charType,
            out _, out float activeRate, out _, out _, out _, out _);
        gs.GetZoneParams(charData.charType,
            out _, out float inZoneRate, out float outZoneRate);

        int total = RaceManager.Instance != null ? RaceManager.Instance.Racers.Count : 12;
        int topHalf = Mathf.Max(1, total / 2);
        float rate;

        if (IsUpperTrack())
        {
            // 상행 (Runner/Leader): 포지셔닝과 동일한 타입별 목표 유지
            float posTarget = gs.GetPositioningTarget(charData.charType);
            int targetMaxRank = Mathf.Max(1, Mathf.CeilToInt(total * posTarget));
            rate = (currentRank <= targetMaxRank) ? inZoneRate : activeRate;
        }
        else
        {
            // 하행 (Chaser/Reckoner): 우선순위 제약

            // 1. 상행 영역 침범 금지 → 강제 보존 (reckoner_baseRate)
            if (currentRank <= topHalf)
            {
                gs.GetZoneParams(CharacterType.Reckoner, out _, out float baseConserveRate, out _);
                rate = baseConserveRate;
            }
            else
            {
                float gap = RaceManager.Instance.LastUpperTrackProgress - TotalProgress;

                // 2. 간격 너무 벌어짐 → 스프린트 (상행 따라잡기)
                if (gap > gs.formationGapMax)
                {
                    rate = activeRate;
                }
                // 3. 상행 추월 방지 → 보존
                else if (gap < gs.formationGapMin)
                {
                    rate = inZoneRate;
                }
                else
                {
                    // 4. 하행 내 경쟁
                    if (charData.charType == CharacterType.Chaser)
                    {
                        float chaserTarget = gs.GetPositioningTarget(CharacterType.Chaser);
                        int targetMaxRank = Mathf.Max(1, Mathf.CeilToInt(total * chaserTarget));
                        rate = (currentRank <= targetMaxRank) ? inZoneRate : outZoneRate;
                    }
                    else // Reckoner: 항상 보존
                    {
                        rate = inZoneRate;
                    }
                }
            }
        }

        ApplyHPConsumption(gs, track, deltaTime, rate);
    }

    // ─── 페이즈 3: 전략 (formationHoldLapEnd 이후) ──────────────────
    private void ConsumeHP_Strategy(GameSettings gs, TrackData track, float deltaTime)
    {
        gs.GetHPParams(charData.charType,
            out _, out float activeRate, out _,
            out _, out _, out _);
        gs.GetZoneParams(charData.charType,
            out _, out float inZoneRate, out float outZoneRate);

        int total = RaceManager.Instance != null ? RaceManager.Instance.Racers.Count : 12;
        float rate;
        bool sprintThisFrame = false;

        switch (charData.charType)
        {
            case CharacterType.Runner:
            {
                // [SPEC-RC-002] 도주: 항상 전력질주 — HP 신경 안 씀
                rate = activeRate;
                sprintThisFrame = true;
                break;
            }

            case CharacterType.Leader:
            {
                // [SPEC-RC-002] 선행: 마지막 바퀴 20% 남으면 전력질주
                bool inSprint = IsLastLapSprintZone(gs.leaderSprintLastLapThreshold);
                int top30Rank = Mathf.Max(1, Mathf.CeilToInt(total * 0.30f));
                bool isOutOfPos = (currentRank > top30Rank);

                if (inSprint)
                {
                    rate = activeRate;
                    sprintThisFrame = true;
                }
                else if (isOutOfPos)
                    rate = outZoneRate; // top30% 이탈 → 추격
                else
                    rate = inZoneRate;  // top30% 이내 → 보존
                break;
            }

            case CharacterType.Chaser:
            {
                // [SPEC-RC-002] 선입: 마지막 바퀴 30% 남으면 전력질주
                bool inSprint = IsLastLapSprintZone(gs.chaserSprintLastLapThreshold);
                int top70Rank = Mathf.Max(1, Mathf.CeilToInt(total * 0.70f));
                bool isOutOfPos = (currentRank > top70Rank);

                if (inSprint)
                {
                    rate = activeRate;
                    sprintThisFrame = true;
                }
                else if (isOutOfPos)
                    rate = outZoneRate; // top70% 이탈 → 추격
                else
                    rate = inZoneRate;  // top70% 이내 → 보존
                break;
            }

            default: // Reckoner
            {
                // [SPEC-RC-002] 추입: 마지막 바퀴 40% 남으면 전력질주, 그 전까지 극보존
                bool inSprint = IsLastLapSprintZone(gs.reckonerSprintLastLapThreshold);

                if (inSprint)
                {
                    rate = activeRate;
                    sprintThisFrame = true;
                }
                else
                    rate = inZoneRate;  // 극보존 (reckoner_baseRate = 0.15)
                break;
            }
        }

        isSprintMode = sprintThisFrame;
        ApplyHPConsumption(gs, track, deltaTime, rate);
    }

    // ─── 헬퍼: 마지막 바퀴 기준 스프린트 판정 (SPEC-RC-002) ────────
    /// <summary>
    /// 마지막 바퀴의 X% 남았는지 판정.
    /// threshold=0.40 → 마지막 바퀴 40% 이하 남으면 true.
    /// 마지막 바퀴 진입 전이면 항상 false.
    /// </summary>
    private bool IsLastLapSprintZone(float threshold)
    {
        int totalLaps = GetTotalLaps();
        if (totalLaps < 2) return false; // 1랩 이하: Legacy 페이즈 처리
        float lastLapStart = (float)(totalLaps - 1) / totalLaps;
        if (OverallProgress < lastLapStart) return false; // 마지막 바퀴 아직 안 됨
        float lastLapLen = 1f / totalLaps;
        float lastLapProg = (OverallProgress - lastLapStart) / lastLapLen; // 0~1
        float lastLapRemaining = 1f - lastLapProg;                         // 0~1
        return lastLapRemaining <= threshold;
    }

    // ─── 공통 tail: boostAmp / speedRatio / condMul / leadPaceTax ─────
    private void ApplyHPConsumption(GameSettings gs, TrackData track, float deltaTime, float rate)
    {
        float baseTrackSpeed = GetBaseTrackSpeed(gs, track);
        float speedRatio = baseTrackSpeed > 0.01f ? currentSpeed / baseTrackSpeed : 1f;
        speedRatio = Mathf.Clamp(speedRatio, 0.1f, 2f);

        float effectiveRate = Mathf.Max(gs.basicConsumptionRate, rate);

        // 부스트 피드백: 부스트 높을수록 HP 소모 증가
        float boostAmp = 1f + gs.boostHPDrainCoeff * Mathf.Max(0f, hpBoostValue);
        effectiveRate *= boostAmp;

        // ★ 컨디션 역보정: 저컨디션 → HP 소모 증가, 고컨디션 → HP 소모 감소
        // condMul 범위 0.9~1.2 → 역수 0.83~1.11 → 최하 11% 더 소모, 최상 17% 덜 소모
        float condMul = OddsCalculator.GetConditionMultiplier(charData.charId);
        condMul = Mathf.Max(condMul, 0.3f);
        effectiveRate /= condMul;

        // [SPEC-RC-002 v2] 스프린트 HP 급소모 시스템
        // ① speedRatio 바이패스: 스프린트 시 속도 무관 풀 소모
        float speedMul = isSprintMode
            ? Mathf.Max(1f, Mathf.Sqrt(speedRatio))   // 스프린트: 최소 1.0 보장
            : Mathf.Sqrt(speedRatio);                   // 보존: 기존 감쇄 유지

        // ② 스프린트 급소모 배율 (Runner 제외: Runner는 Strategy 전체가 스프린트)
        // lapFactor = hpLapReference / totalLaps → 단거리일수록 높음
        // 2바퀴: 1.5배 / 3바퀴: 1.0배 / 5바퀴: 0.6배
        // → 단거리 추입: 극한 소모 → 탈진 → 꼴찌
        // → 장거리 추입: 완만 소모 → HP 충분 → 부스트 폭발 → 역전
        if (isSprintMode && charData.charType != CharacterType.Runner)
        {
            int totalLaps = GetTotalLaps();
            float lapFactor = (float)gs.hpLapReference / Mathf.Max(1, totalLaps);
            effectiveRate *= gs.sprintHPDrainMultiplier * lapFactor;
        }

        float gameDt = deltaTime * gs.globalSpeedMultiplier;
        float consumption = effectiveRate * speedMul * gameDt;
        consumption = Mathf.Min(consumption, enduranceHP);

        enduranceHP -= consumption;
        totalConsumedHP += consumption;

        // 선두 페이스 택스: 바람막이 추가 소모 (totalConsumedHP 미포함 → 순수 탈진 가속)
        if (currentRank <= gs.leadPaceTaxRank && enduranceHP > 0f)
        {
            float paceTax = gs.leadPaceTaxRate * Mathf.Sqrt(speedRatio) * gameDt;
            enduranceHP = Mathf.Max(0f, enduranceHP - paceTax);
        }
    }

    // ─── 헬퍼: 상행 타입 여부 (Runner/Leader) ────────────────────────
    private bool IsUpperTrack()
    {
        return charData.charType == CharacterType.Runner
            || charData.charType == CharacterType.Leader;
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

        // 장거리 보정: Chaser/Reckoner peakBoost 증폭
        int totalLapsForBoost = GetTotalLaps();
        if (totalLapsForBoost > gs.hpLapReference)
        {
            if (charData.charType == CharacterType.Chaser || charData.charType == CharacterType.Reckoner)
            {
                float lapExcess = (float)(totalLapsForBoost - gs.hpLapReference) / gs.hpLapReference;
                peakBoost *= (1f + lapExcess * gs.longRaceLateBoostAmp);
            }
        }

        float consumedRatio = maxHP > 0f ? totalConsumedHP / maxHP : 0f;
        float threshold = gs.boostThreshold;

        // ═══ Power 기반 가속 강화: power 높을수록 부스트 곡선이 가파름 ═══
        float powerFactor = 1f + (charData.charBasePower / 20f) * gs.powerAccelCoeff;
        float effectiveAccelExp = accelExp / Mathf.Max(powerFactor, 0.1f);

        float boost;
        if (consumedRatio <= threshold)
        {
            // ── 가속 구간: 0% ~ boostThreshold 소모 ──
            float t = threshold > 0f ? consumedRatio / threshold : 0f;
            boost = peakBoost * Mathf.Pow(t, effectiveAccelExp);
        }
        else if (enduranceHP > 0f)
        {
            // ── 감속 구간: boostThreshold ~ 100% 소모 ──
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

    // ═══ Phase 4: 실시간 순위 갱신 (SPEC-006 §5) ═══
    private void UpdateRank()
    {
        if (RaceManager.Instance == null) return;
        var racers = RaceManager.Instance.Racers;
        int rank = 1;
        float myProgress = TotalProgress;
        for (int i = 0; i < racers.Count; i++)
        {
            if (racers[i] != this && racers[i].TotalProgress > myProgress)
                rank++;
        }
        currentRank = rank;
    }

    // ═══ 개선 슬립스트림 (전체 타입, 거리 기반) ═══
    private void UpdateSlipstreamBlend(float deltaTime)
    {
        var gs = GameSettings.Instance;
        if (gs == null || RaceManager.Instance == null) { slipstreamBlend = 0f; return; }

        float myProgress = TotalProgress;
        float closestGap = float.MaxValue;
        foreach (var r in RaceManager.Instance.Racers)
        {
            if (r == this || r.IsFinished) continue;
            float gap = r.TotalProgress - myProgress;
            if (gap > 0f && gap < closestGap) closestGap = gap;
        }

        float target = (closestGap < gs.universalSlipstreamRange)
            ? 1f - (closestGap / gs.universalSlipstreamRange)
            : 0f;
        float fadeTime = Mathf.Max(gs.slipstreamFadeTime, 0.01f);
        slipstreamBlend = Mathf.MoveTowards(slipstreamBlend, target, deltaTime * gs.globalSpeedMultiplier / fadeTime);
    }

    // ═══ CP 소모 ═══
    private void ConsumeCP(GameSettings gs, float deltaTime)
    {
        if (calmPoints <= 0f) return;
        float drain = gs.cpBasicDrain;
        if (slipstreamBlend > 0f)
            drain += gs.cpSlipstreamDrain * slipstreamBlend;
        calmPoints = Mathf.Max(0f, calmPoints - drain * deltaTime * gs.globalSpeedMultiplier);
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