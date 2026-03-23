using UnityEngine;

/// <summary>
/// 게임 세팅 데이터 (Unity Inspector에서 편집 가능)
/// </summary>
[CreateAssetMenu(fileName = "GameSettings", menuName = "DopamineRace/GameSettings")]
public partial class GameSettings : ScriptableObject
{
    [Header("═══ 레이서 설정 ═══")]
    [Tooltip("한 레이스당 참가 레이서 수 (고정: 9명)")]
    [Range(9, 9)]
    public int racerCount = 9;

    [Header("═══ 라운드 설정 ═══")]
    [Tooltip("라운드별 바퀴 수 (배열 길이 = 총 라운드 수)\n예: [1,2,1,5,3,1,4] → 7라운드")]
    public int[] roundLaps = new int[] { 2, 2, 3, 5, 3, 2, 4 };

    /// <summary>
    /// 총 라운드 수 (roundLaps 배열 길이)
    /// </summary>
    public int TotalRounds => (roundLaps != null && roundLaps.Length > 0) ? roundLaps.Length : 1;

    /// <summary>
    /// 해당 라운드의 바퀴 수 (1-based round 번호)
    /// </summary>
    public int GetLapsForRound(int round)
    {
        if (roundLaps == null || roundLaps.Length == 0) return 1;
        int idx = Mathf.Clamp(round - 1, 0, roundLaps.Length - 1);
        return Mathf.Max(1, roundLaps[idx]);
    }

    [Header("═══ 배당 점수 설정 ═══")]
    [Tooltip("단승 배당 (1등 맞추기)")]
    public int payoutWin = 3;

    [Tooltip("연승 배당 (3착 이내 1마리, 7두 이하 시 2착)")]
    public int payoutPlace = 1;

    [Tooltip("복승 배당 (1~2등 2마리, 순서 무관)")]
    public int payoutQuinella = 4;

    [Tooltip("쌍승 배당 (1등+2등 정확한 순서)")]
    public int payoutExacta = 6;

    [Tooltip("삼복승 배당 (1~3등 3마리, 순서 무관)")]
    public int payoutTrio = 10;

    [Tooltip("복연승 배당 (3착 이내 2마리, 순서 무관)")]
    public int payoutWide = 2;

    // ──────────────────────────────────────────────────────────────────────
    [Header("═══ 컨디션 확률 (합계 = 1.0) ═══")]
    [Tooltip("최상 컨디션 확률")]
    [Range(0f, 1f)] public float conditionRate_best   = 0.12f;
    [Tooltip("상 컨디션 확률")]
    [Range(0f, 1f)] public float conditionRate_good   = 0.18f;
    [Tooltip("중 컨디션 확률")]
    [Range(0f, 1f)] public float conditionRate_normal = 0.40f;
    [Tooltip("하 컨디션 확률")]
    [Range(0f, 1f)] public float conditionRate_bad    = 0.18f;
    [Tooltip("최하 컨디션 확률")]
    [Range(0f, 1f)] public float conditionRate_worst  = 0.12f;

    [Header("═══ 컨디션 스탯 배수 ═══")]
    [Tooltip("최상: 스탯에 곱해지는 배수")]
    public float conditionMul_best   = 1.20f;
    [Tooltip("상: 스탯에 곱해지는 배수")]
    public float conditionMul_good   = 1.10f;
    [Tooltip("중: 스탯에 곱해지는 배수 (기준값)")]
    public float conditionMul_normal = 1.00f;
    [Tooltip("하: 스탯에 곱해지는 배수")]
    public float conditionMul_bad    = 0.95f;
    [Tooltip("최하: 스탯에 곱해지는 배수")]
    public float conditionMul_worst  = 0.90f;

    // ──────────────────────────────────────────────────────────────────────
    [Header("═══ 인기도 스탯 가중치 ═══")]
    [Tooltip("speed 스탯이 인기도에 미치는 비중")]
    public float oddsStatWeight_speed      = 0.35f;
    [Tooltip("power 스탯이 인기도에 미치는 비중")]
    public float oddsStatWeight_power      = 0.15f;
    [Tooltip("luck 스탯이 인기도에 미치는 비중")]
    public float oddsStatWeight_luck       = 0.20f;
    [Tooltip("endurance 스탯이 인기도에 미치는 비중")]
    public float oddsStatWeight_endurance  = 0.15f;
    [Tooltip("brave 스탯이 인기도에 미치는 비중")]
    public float oddsStatWeight_brave      = 0.10f;
    [Tooltip("calm 스탯이 인기도에 미치는 비중")]
    public float oddsStatWeight_calm       = 0.05f;

    [Header("═══ 인기도 실적 가중치 ═══")]
    [Tooltip("1착 승률이 인기도에 미치는 가중치")]
    public float oddsRecordWeight_win   = 3.0f;
    [Tooltip("2착 비율이 인기도에 미치는 가중치")]
    public float oddsRecordWeight_place = 1.5f;
    [Tooltip("3착 비율이 인기도에 미치는 가중치")]
    public float oddsRecordWeight_show  = 0.8f;

    [Header("═══ 트랙 특화 보정 ═══")]
    [Range(0f, 1f)]
    [Tooltip("트랙별 기록 반영 비율 (0=전체기록만, 1=트랙기록만)")]
    public float trackOddsWeight  = 0.4f;
    [Tooltip("트랙별 기록을 반영하기 위한 최소 출전 횟수")]
    public int trackOddsMinRaces = 3;

    [Header("═══ 신규 캐릭터 불확실성 ═══")]
    [Range(0f, 0.5f)]
    [Tooltip("기록 부족 시 배당에 추가되는 랜덤 변동 폭 (0.3 = ±30%)")]
    public float newCharOddsVariance = 0.3f;
    [Tooltip("이 횟수 미만이면 신규(isNew=true) 처리")]
    public int newCharThreshold = 5;

    [Header("═══ 단승 배당 범위 (인기순위 1~12위) ═══")]
    [Tooltip("인기순위별 배당 최솟값. 배열 길이 = 최대 레이서 수(12)")]
    public float[] oddsRangeMin = { 1.5f, 3.0f, 5.0f, 8.0f, 13f, 20f, 30f, 45f, 55f, 65f, 75f, 85f };
    [Tooltip("인기순위별 배당 최댓값. 배열 길이 = 최대 레이서 수(12)")]
    public float[] oddsRangeMax = { 3.0f, 5.0f, 8.0f, 13f, 20f, 30f, 45f, 55f, 65f, 75f, 85f, 99f };

    [Header("═══ 복합 승식 계수 ═══")]
    [Tooltip("연승 배당 계수 (단승 평균 × 이 값)")]
    public float oddsCoef_place    = 0.35f;
    [Tooltip("복승 배당 계수 (단승A × 단승B × 이 값)")]
    public float oddsCoef_quinella = 0.55f;
    [Tooltip("쌍승 배당 계수 (단승A × 단승B × 이 값, 순서 맞춰야 하므로 높음)")]
    public float oddsCoef_exacta   = 1.10f;
    [Tooltip("복연승 배당 계수")]
    public float oddsCoef_wide     = 0.45f;
    [Tooltip("삼복승 배당 계수 (3개 곱이라 낮게 설정)")]
    public float oddsCoef_trio     = 0.20f;

    [Header("═══ 복합 승식 배당 상한/하한 ═══")]
    public float oddsMin_place    = 1.0f;  public float oddsMax_place    = 15f;
    public float oddsMin_quinella = 2.0f;  public float oddsMax_quinella = 200f;
    public float oddsMin_exacta   = 3.0f;  public float oddsMax_exacta   = 500f;
    public float oddsMin_wide     = 1.5f;  public float oddsMax_wide     = 100f;
    public float oddsMin_trio     = 5.0f;  public float oddsMax_trio     = 999f;

    [Header("═══ 거리 구분 설정 ═══")]
    [Tooltip("이 바퀴 수 이하 = 단거리")]
    public int shortDistanceMax = 2;
    [Tooltip("이 바퀴 수 이하 = 중거리 (초과 = 장거리)")]
    public int midDistanceMax = 4;

    [Header("═══ UI 프리팹 ═══")]
    [Tooltip("배팅 패널 프리팹 (BettingUIPrefabCreator로 자동 생성)")]
    public GameObject bettingPanelPrefab;
    [Tooltip("캐릭터 아이템 프리팹 (BettingUIPrefabCreator로 자동 생성)")]
    public GameObject characterItemPrefab;
    [Tooltip("결과 패널 프리팹 (ResultUIPrefabCreator로 자동 생성)")]
    public GameObject resultPanelPrefab;
    [Tooltip("최종 결산 패널 프리팹 (FinishLeaderboardUIPrefabCreator로 자동 생성)")]
    public GameObject finishPanelPrefab;
    [Tooltip("리더보드 팝업 프리팹 (FinishLeaderboardUIPrefabCreator로 자동 생성)")]
    public GameObject leaderboardPanelPrefab;
    [Tooltip("트랙 프로그레스 바 프리팹 (추후 디자인 적용 시 사용)")]
    public GameObject trackProgressBarPrefab;

    [Header("═══ 세이브 ═══")]
    [Tooltip("true면 마지막 플레이 라운드를 기억하여 복귀\nfalse면 항상 1라운드부터 시작")]
    public bool enableRoundResume = true;

    /// <summary>
    /// 바퀴 수 → 거리 구분 Loc 키 반환
    /// </summary>
    public string GetDistanceKey(int laps)
    {
        if (laps <= shortDistanceMax) return "str.ui.track.short";
        if (laps <= midDistanceMax)   return "str.ui.track.mid";
        return "str.ui.track.long";
    }

    [Header("═══ 경로 설정 ═══")]
    [Tooltip("경로 이탈 가중치\n0 = 정확히 트랙 위\n0.1~0.3 = 약간 흔들림")]
    [Range(0f, 1f)]
    public float pathDeviation = 0f;

    [Header("═══ 캐릭터 표시 설정 ═══")]
    [Tooltip("캐릭터 크기")]
    public float characterScale = 0.8f;

    [Tooltip("레인 오프셋")]
    public float laneOffset = 0.05f;

    [Tooltip("번호 라벨 크기")]
    public float labelSize = 0.2f;

    [Tooltip("번호 라벨 높이")]
    public float labelHeight = 1.5f;

    [Tooltip("배팅 마커 크기")]
    public float betMarkerSize = 0.05f;

    [Tooltip("배팅 마커 높이")]
    public float betMarkerHeight = 0.3f;

    [Header("═══ BGM 설정 ═══")]
    [Tooltip("BGM 시작 딜레이 (초)")]
    [Range(0f, 10f)]
    public float bgmDelay = 3f;

    [Tooltip("BGM 볼륨")]
    [Range(0f, 1f)]
    public float bgmVolume = 0.5f;

    // ══════════════════════════════════════
    //  레이스 공식 (Inspector에서 실시간 조절)
    // ══════════════════════════════════════

    [Header("═══ 속도 설정 ═══")]
    [Tooltip("기본 달리기 속도 배율. 높일수록 전체 레이스 속도가 빨라짐.\n(캐릭터 Speed 스탯 × 이 값 × 트랙 배율 = 기본속도)")]
    [Range(0.5f, 10.0f)]
    public float globalSpeedMultiplier = 2.5f;

    [Header("═══ 레이스 기본 공식 ═══")]

    [Tooltip("calm 기반 속도 변동 배율\n높을수록 calm 낮은 캐릭터 속도 요동 큼")]
    [Range(0f, 0.5f)]
    public float noiseFactor = 0.1f;

    [Tooltip("endurance 기반 후반 감속 배율\n높을수록 endurance 낮은 캐릭터 후반에 처짐")]
    [Range(0f, 0.5f)]
    public float fatigueFactor = 0.15f;

    [Tooltip("속도 보간 속도 (높을수록 빠르게 목표 속도에 도달)")]
    [Range(1f, 10f)]
    public float raceSpeedLerp = 3f;

    [Tooltip("HP 시스템 사용 여부 (false면 레거시 타입보너스+피로 사용)")]
    public bool useHPSystem = true;

    // ══════════════════════════════════════
    //  지구력 HP 시스템 (SPEC-006)
    // ══════════════════════════════════════

    [Header("═══ HP 시스템 공통 ═══")]
    [Tooltip("maxHP = hpBase + endurance × hpPerEndurance")]
    public float hpBase = 50f;
    [Tooltip("내구력 1당 추가 HP")]
    public float hpPerEndurance = 3f;
    [Tooltip("달리기 기본 HP 소모율 (/초). max(이 값, 존Rate) 중 큰 값 적용 — 최소 소모 바닥")]
    [Range(0.1f, 3f)] public float basicConsumptionRate = 0.5f;
    [Tooltip("가속→감속 전환 임계점 (consumed% 기준)")]
    [Range(0.2f, 0.8f)]
    public float boostThreshold = 0.4f;
    [Tooltip("속도 차이 압축 (0=압축 없음, 1=전원 동일 속도). HP 시스템에서만 적용")]
    [Range(0f, 0.95f)]
    public float hpSpeedCompress = 0.85f;

    [Tooltip("HP 기준 바퀴 수. 이 바퀴 수 이하 레이스는 HP 풀 변동 없음, 초과 시 비례 증가")]
    [Range(1, 5)] public int hpLapReference = 3;

    [Header("═══ HP: 부스트 피드백 시스템 ═══")]
    [Tooltip("부스트가 HP 소모를 증폭하는 계수 (2.0 = 16% 부스트 시 소모 +32%). 가속할수록 HP가 빨리 닳음")]
    [Range(0f, 10f)] public float boostHPDrainCoeff = 3.0f;
    [Tooltip("Power 스탯이 가속 곡선을 가파르게 만드는 계수. power 20 + coeff 0.5 → accelExp ÷1.5")]
    [Range(0f, 2f)] public float powerAccelCoeff = 0.5f;

    [Header("═══ HP: 도주 (Runner) ═══")]
    [Tooltip("적극 소모 시작 시점 (OverallProgress)")]
    [Range(0f, 0.9f)]  public float runner_spurtStart = 0.00f;
    [Tooltip("적극 소모율")]
    [Range(0.5f, 5.0f)] public float runner_activeRate = 3.5f;
    [Tooltip("60% 소모 시 최대 부스트")]
    [Range(0.01f, 0.5f)] public float runner_peakBoost = 0.10f;
    [Tooltip("가속 곡선 지수 (높을수록 후반 급격)")]
    [Range(0.5f, 3.0f)] public float runner_accelExp = 1.5f;
    [Tooltip("감속 곡선 지수 (높을수록 완만)")]
    [Range(0.3f, 3.0f)] public float runner_decelExp = 0.7f;
    [Tooltip("탈진 페널티 (음수)")]
    [Range(-0.15f, 0f)] public float runner_exhaustionFloor = -0.08f;

    [Header("═══ HP: 선행 (Leader) ═══")]
    [Range(0f, 0.9f)]  public float leader_spurtStart = 0.05f;
    [Range(0.5f, 5.0f)] public float leader_activeRate = 3.0f;
    [Range(0.01f, 0.5f)] public float leader_peakBoost = 0.10f;
    [Range(0.5f, 3.0f)] public float leader_accelExp = 1.2f;
    [Range(0.3f, 3.0f)] public float leader_decelExp = 1.0f;
    [Range(-0.15f, 0f)] public float leader_exhaustionFloor = -0.02f;

    [Header("═══ HP: 선입 (Chaser) ═══")]
    [Range(0f, 0.9f)]  public float chaser_spurtStart = 0.15f;
    [Range(0.5f, 5.0f)] public float chaser_activeRate = 3.5f;
    [Range(0.01f, 0.5f)] public float chaser_peakBoost = 0.12f;
    [Range(0.5f, 3.0f)] public float chaser_accelExp = 1.3f;
    [Range(0.3f, 3.0f)] public float chaser_decelExp = 0.8f;
    [Range(-0.15f, 0f)] public float chaser_exhaustionFloor = -0.03f;

    [Header("═══ HP: 추행 (Reckoner) ═══")]
    [Range(0f, 0.9f)]  public float reckoner_spurtStart = 0.30f;
    [Range(0.5f, 5.0f)] public float reckoner_activeRate = 4.5f;
    [Range(0.01f, 0.5f)] public float reckoner_peakBoost = 0.16f;
    [Range(0.5f, 3.0f)] public float reckoner_accelExp = 1.8f;
    [Range(0.3f, 3.0f)] public float reckoner_decelExp = 0.4f;
    [Range(-0.15f, 0f)] public float reckoner_exhaustionFloor = -0.01f;

    [Header("═══ HP: 장거리 보정 ═══")]
    [Tooltip("hpLapReference 초과 시 Chaser/Reckoner peakBoost 증폭 계수")]
    [Range(0f, 0.5f)] public float longRaceLateBoostAmp = 0.15f;

    [Header("═══ HP: 마지막 바퀴 기준 스프린트 (SPEC-RC-002) ═══")]
    [Tooltip("선행(Leader): 마지막 바퀴의 X% 남으면 스프린트 (0.20 = 20% 남으면)")]
    [Range(0.05f, 0.60f)] public float leaderSprintLastLapThreshold = 0.20f;
    [Tooltip("선입(Chaser): 마지막 바퀴의 X% 남으면 스프린트 (0.30 = 30% 남으면)")]
    [Range(0.05f, 0.60f)] public float chaserSprintLastLapThreshold = 0.30f;
    [Tooltip("추입(Reckoner): 마지막 바퀴의 X% 남으면 스프린트 (0.40 = 40% 남으면)")]
    [Range(0.05f, 0.60f)] public float reckonerSprintLastLapThreshold = 0.40f;
    [Tooltip("스프린트 진입 순간 speedLerp 배율 (2.5 = 즉각 가속 효과)")]
    [Range(1f, 5f)] public float sprintBurstLerpMult = 2.5f;

    [Header("═══ HP: 스프린트 급소모 (SPEC-RC-002 v2) ═══")]
    [Tooltip("스프린트 시 HP 소모율 배율 (Runner 제외). 3.0 = 3배 빠른 소모.\n" +
             "단거리일수록 hpLapReference/totalLaps 배율 가산 → 단거리 추입 불리, 장거리 유리")]
    [Range(1f, 8f)] public float sprintHPDrainMultiplier = 3.0f;

    [Tooltip("슬립스트림 블렌드 페이드 시간 (초)")]
    [Range(0.5f, 5f)] public float slipstreamFadeTime = 2.0f;

    [Header("═══ HP: 포지션 타겟팅 (1차 타입 밸런스) ═══")]
    [Tooltip("도주 타겟 순위 비율 (0.25 = 상위 25%, 12명 중 top 3)")]
    [Range(0f, 1f)] public float runner_targetZone = 0.25f;
    [Tooltip("도주 존 내 소모율 (/초)")]
    [Range(0.1f, 5f)] public float runner_inZoneRate = 1.0f;
    [Tooltip("도주 존 밖 소모율 (/초) — 패닉 모드")]
    [Range(0.1f, 5f)] public float runner_outZoneRate = 2.0f;

    [Tooltip("선행 타겟 순위 비율 (0.30 = 상위 30%, 12명 중 top 4)")]
    [Range(0f, 1f)] public float leader_targetZone = 0.30f;
    [Tooltip("선행 존 내 소모율")]
    [Range(0.1f, 5f)] public float leader_inZoneRate = 1.2f;
    [Tooltip("선행 존 밖 소모율")]
    [Range(0.1f, 5f)] public float leader_outZoneRate = 1.8f;

    [Tooltip("선입 타겟 순위 비율 (0.70 = 상위 70%, 12명 중 top 9)")]
    [Range(0f, 1f)] public float chaser_targetZone = 0.70f;
    [Tooltip("선입 존 내 소모율 — 극 절약")]
    [Range(0.1f, 5f)] public float chaser_inZoneRate = 0.5f;
    [Tooltip("선입 존 밖 소모율")]
    [Range(0.1f, 5f)] public float chaser_outZoneRate = 1.5f;

    [Tooltip("추입 스퍼트 전 기본 소모율 — 타겟 존 없음, 항상 보존")]
    [Range(0.1f, 5f)] public float reckoner_baseRate = 0.3f;

    [Tooltip("Leader 조건부 스퍼트 최소 HP 잔량 비율 (이하면 스퍼트 포기)")]
    [Range(0f, 0.5f)] public float leaderSpurtMinHP = 0.3f;

    [Header("═══ HP: 초반 타입 보너스 ═══")]
    [Tooltip("도주(Runner) 초반 속도 보너스")]
    [Range(0f, 0.3f)] public float hp_earlyBonus_Runner = 0.12f;
    [Tooltip("선행(Leader) 초반 속도 보너스")]
    [Range(0f, 0.3f)] public float hp_earlyBonus_Leader = 0.04f;
    [Tooltip("선입(Chaser) 초반 속도 보너스")]
    [Range(0f, 0.3f)] public float hp_earlyBonus_Chaser = 0.02f;
    [Tooltip("추행(Reckoner) 초반 속도 보너스")]
    [Range(0f, 0.3f)] public float hp_earlyBonus_Reckoner = 0f;
    [Tooltip("초반 보너스가 0으로 사라지는 진행률")]
    [Range(0.1f, 0.5f)] public float hp_earlyBonusFadeEnd = 0.25f;

    /// <summary>
    /// HP 시스템용 초반 타입 보너스.
    /// progress 0 → 최대값, hp_earlyBonusFadeEnd → 0 (선형 페이드아웃)
    /// </summary>
    public float GetHPEarlyBonus(CharacterType type, float progress)
    {
        if (progress >= hp_earlyBonusFadeEnd) return 0f;

        float bonus = type switch
        {
            CharacterType.Runner   => hp_earlyBonus_Runner,
            CharacterType.Leader   => hp_earlyBonus_Leader,
            CharacterType.Chaser   => hp_earlyBonus_Chaser,
            _                      => hp_earlyBonus_Reckoner
        };
        // 음수 보너스 허용: 하행 타입(선입/추입)은 초반에 의도적으로 감속
        // bonus == 0이면 효과 없음 (불필요한 계산 스킵)
        if (bonus == 0f) return 0f;

        float fade = 1f - (progress / hp_earlyBonusFadeEnd);
        return bonus * fade;
    }

    [Header("═══ 초반 대형 (Formation Phase) ═══")]
    [Tooltip("대형 유지 구간 종료 진행률 (0.2 = 처음 20%)")]
    [Range(0.05f, 0.5f)] public float formationPhaseEnd = 0.20f;
    [Tooltip("대형 이탈 시 속도 보정 강도 (순위 편차 × 이 값)")]
    [Range(0.01f, 0.5f)] public float formationCorrectionStrength = 0.15f;
    [Tooltip("도주 목표 포지션 (0=선두, 1=꼴찌)")]
    [Range(0f, 1f)] public float formationTarget_Runner = 0.15f;
    [Tooltip("선행 목표 포지션")]
    [Range(0f, 1f)] public float formationTarget_Leader = 0.35f;
    [Tooltip("선입 목표 포지션")]
    [Range(0f, 1f)] public float formationTarget_Chaser = 0.65f;
    [Tooltip("추입 목표 포지션")]
    [Range(0f, 1f)] public float formationTarget_Reckoner = 0.85f;

    /// <summary>
    /// 초반 대형 속도 보정값.
    /// 현재 순위가 목표보다 뒤(높은 번호)면 양수(가속), 앞(낮은 번호)면 음수(감속).
    /// formationPhaseEnd까지 선형 페이드아웃.
    /// </summary>
    public float GetFormationModifier(CharacterType type, float progress, int currentRank, int totalRacers)
    {
        if (progress >= formationPhaseEnd) return 0f;

        float targetPct = type switch
        {
            CharacterType.Runner   => formationTarget_Runner,
            CharacterType.Leader   => formationTarget_Leader,
            CharacterType.Chaser   => formationTarget_Chaser,
            _                      => formationTarget_Reckoner
        };

        float currentPct = (currentRank - 1f) / Mathf.Max(totalRacers - 1f, 1f);
        float deviation = currentPct - targetPct; // +면 뒤처짐, -면 앞서감

        float fade = 1f - (progress / formationPhaseEnd);
        return deviation * formationCorrectionStrength * fade;
    }

    [Header("═══ 레이스 전략: 구간 설정 ═══")]
    [Tooltip("포지셔닝 페이즈 종료 (랩 단위, 0.5 = 첫 랩의 절반)")]
    [Range(0.1f, 1.0f)] public float positioningLapEnd = 0.5f;
    [Tooltip("대형 유지 페이즈 종료 (랩 단위, 1.0 = 첫 랩 완료)")]
    [Range(0.5f, 2.0f)] public float formationHoldLapEnd = 1.0f;

    [Header("═══ 레이스 전략: 포지셔닝 타겟 ═══")]
    [Tooltip("도주 포지셔닝 타겟 (0.25 = top25%, 12명 중 1~3위)")]
    [Range(0f, 1f)] public float runner_posTarget = 0.25f;
    [Tooltip("선행 포지셔닝 타겟 (0.50 = top50%, 12명 중 1~6위)")]
    [Range(0f, 1f)] public float leader_posTarget = 0.50f;
    [Tooltip("선입 포지셔닝 타겟 (0.75 = top75%, 12명 중 1~9위)")]
    [Range(0f, 1f)] public float chaser_posTarget = 0.75f;
    // 추입: 타겟 없음, 항상 보존 (-1 반환)

    [Header("═══ 레이스 전략: 대형 유지 간격 ═══")]
    [Tooltip("하행 그룹이 상행 그룹과의 TotalProgress 간격이 이 값 초과 시 스프린트 (너무 뒤처짐)")]
    [Range(0.05f, 2.0f)] public float formationGapMax = 0.3f;
    [Tooltip("하행 그룹이 상행 그룹과의 TotalProgress 간격이 이 값 미만 시 보존 (추월 방지)")]
    [Range(0.01f, 0.3f)] public float formationGapMin = 0.05f;

    /// <summary>
    /// 포지셔닝 페이즈에서 타입별 목표 순위 비율 반환.
    /// 반환값 -1 = 추입 (타겟 없음, 항상 보존).
    /// </summary>
    public float GetPositioningTarget(CharacterType type)
    {
        return type switch
        {
            CharacterType.Runner   => runner_posTarget,
            CharacterType.Leader   => leader_posTarget,
            CharacterType.Chaser   => chaser_posTarget,
            _                      => -1f  // Reckoner: 항상 보존
        };
    }

    [Header("═══ 선두 페이스 택스 (Lead Pace Tax) ═══")]
    [Tooltip("HP 추가 소모 적용 순위 (2 = 1~2위가 바람막이 세금)")]
    [Range(1, 6)] public int leadPaceTaxRank = 2;
    [Tooltip("선두 HP 추가 소모율 (/초). totalConsumedHP에 미포함 → 순수 탈진 가속")]
    [Range(0f, 3f)] public float leadPaceTaxRate = 0.8f;

    [Header("═══ CP 시스템 (Calm Points) ═══")]
    [Tooltip("CP 최대치 = charBaseCalm × 이 값")]
    [Range(1f, 30f)] public float cpMultiplier = 10f;
    [Tooltip("기본 CP 소모율 (/초, 레이스 중 항상 소모)")]
    [Range(0f, 2f)] public float cpBasicDrain = 0.5f;
    [Tooltip("슬립스트림 활성 시 추가 CP 소모율 (/초)")]
    [Range(0f, 5f)] public float cpSlipstreamDrain = 2.5f;

    [Header("═══ 개선 슬립스트림 (전체 타입, 거리 기반) ═══")]
    [Tooltip("슬립스트림 유효 거리 (TotalProgress 단위)")]
    [Range(0.01f, 0.3f)] public float universalSlipstreamRange = 0.08f;
    [Tooltip("기본 최대 속도 보너스")]
    [Range(0f, 0.1f)] public float universalSlipstreamBonus = 0.015f;
    [Tooltip("Chaser 슬립스트림 효율 배율 (타입 특성)")]
    [Range(1f, 3f)] public float chaserSlipstreamMult = 1.1f;

    [Header("═══ CP/HP 불안정 ═══")]
    [Tooltip("CP 잔량 이하 시 슬립스트림 효과 감소 시작")]
    [Range(0.2f, 0.8f)] public float cpWeakThreshold = 0.5f;
    [Tooltip("CP 바닥 시 슬립스트림 최소 효율 (0.5 = 50%)")]
    [Range(0f, 1f)] public float cpMinEfficiency = 0.5f;
    [Tooltip("CP 바닥 시 노이즈 배율")]
    [Range(1f, 4f)] public float cpLowNoiseMul = 2.0f;
    [Tooltip("HP 불안정 시작 잔량 비율")]
    [Range(0.2f, 0.6f)] public float hpUnstableThreshold = 0.4f;
    [Tooltip("HP 바닥 시 노이즈 배율")]
    [Range(1f, 5f)] public float hpLowNoiseMul = 2.5f;

    /// CP 잔량비 → 슬립스트림 효율 (1.0~cpMinEfficiency)
    public float GetCPEfficiency(float cpRatio)
    {
        if (cpRatio >= cpWeakThreshold) return 1f;
        float t = cpRatio / cpWeakThreshold;  // 0~1
        return Mathf.Lerp(cpMinEfficiency, 1f, t);
    }

    /// CP 잔량비 → 노이즈 배율 (1.0~cpLowNoiseMul)
    public float GetCPNoiseMul(float cpRatio)
    {
        if (cpRatio >= cpWeakThreshold) return 1f;
        float t = cpRatio / cpWeakThreshold;
        return Mathf.Lerp(cpLowNoiseMul, 1f, t);
    }

    /// HP 잔량비 → 노이즈 배율 (1.0~hpLowNoiseMul)
    public float GetHPNoiseMul(float hpRatio)
    {
        if (hpRatio >= hpUnstableThreshold) return 1f;
        float t = hpRatio / hpUnstableThreshold;
        return Mathf.Lerp(hpLowNoiseMul, 1f, t);
    }

    /// 슬립스트림 속도 보너스 (blend 0~1, CP 효율 적용)
    public float GetSlipstreamBonus(CharacterType type, float blend, float cpEfficiency)
    {
        if (blend <= 0f) return 0f;
        float bonus = universalSlipstreamBonus * blend * cpEfficiency;
        if (type == CharacterType.Chaser) bonus *= chaserSlipstreamMult;
        return bonus;
    }

    // ══════════════════════════════════════
    //  HP 헬퍼 메서드
    // ══════════════════════════════════════

    /// <summary>
    /// 캐릭터 타입에 맞는 HP 파라미터 반환
    /// </summary>
    public void GetHPParams(CharacterType type,
        out float spurtStart, out float activeRate, out float peakBoost,
        out float accelExp, out float decelExp, out float exhaustionFloor)
    {
        switch (type)
        {
            case CharacterType.Runner:
                spurtStart = runner_spurtStart; activeRate = runner_activeRate;
                peakBoost = runner_peakBoost; accelExp = runner_accelExp;
                decelExp = runner_decelExp; exhaustionFloor = runner_exhaustionFloor;
                return;
            case CharacterType.Leader:
                spurtStart = leader_spurtStart; activeRate = leader_activeRate;
                peakBoost = leader_peakBoost; accelExp = leader_accelExp;
                decelExp = leader_decelExp; exhaustionFloor = leader_exhaustionFloor;
                return;
            case CharacterType.Chaser:
                spurtStart = chaser_spurtStart; activeRate = chaser_activeRate;
                peakBoost = chaser_peakBoost; accelExp = chaser_accelExp;
                decelExp = chaser_decelExp; exhaustionFloor = chaser_exhaustionFloor;
                return;
            default: // Reckoner
                spurtStart = reckoner_spurtStart; activeRate = reckoner_activeRate;
                peakBoost = reckoner_peakBoost; accelExp = reckoner_accelExp;
                decelExp = reckoner_decelExp; exhaustionFloor = reckoner_exhaustionFloor;
                return;
        }
    }

    /// <summary>
    /// 타입별 포지션 존 파라미터 반환.
    /// targetZonePct: 0 = 타겟 없음 (Reckoner), 0.25 = 상위 25%, etc.
    /// </summary>
    public void GetZoneParams(CharacterType type,
        out float targetZonePct, out float inZoneRate, out float outZoneRate)
    {
        switch (type)
        {
            case CharacterType.Runner:
                targetZonePct = runner_targetZone;
                inZoneRate = runner_inZoneRate;
                outZoneRate = runner_outZoneRate;
                return;
            case CharacterType.Leader:
                targetZonePct = leader_targetZone;
                inZoneRate = leader_inZoneRate;
                outZoneRate = leader_outZoneRate;
                return;
            case CharacterType.Chaser:
                targetZonePct = chaser_targetZone;
                inZoneRate = chaser_inZoneRate;
                outZoneRate = chaser_outZoneRate;
                return;
            default: // Reckoner — 타겟 존 없음
                targetZonePct = 0f;
                inZoneRate = reckoner_baseRate;
                outZoneRate = reckoner_baseRate;
                return;
        }
    }

    /// <summary>
    /// charEndurance → maxHP 계산 (기본)
    /// </summary>
    public float CalcMaxHP(float endurance)
    {
        return hpBase + endurance * hpPerEndurance;
    }

    /// <summary>
    /// charEndurance + 바퀴 수 → maxHP 계산 (랩 스케일링 적용)
    /// </summary>
    public float CalcMaxHP(float endurance, int laps)
    {
        float baseHP = hpBase + endurance * hpPerEndurance;
        return baseHP * GetHPLapScale(laps);
    }

    /// <summary>
    /// 바퀴 수에 따른 HP 스케일 팩터. hpLapReference 이하 → 1.0, 초과 → 비례 증가.
    /// </summary>
    public float GetHPLapScale(int laps)
    {
        if (laps <= hpLapReference) return 1f;
        return (float)laps / hpLapReference;
    }

    [Header("═══ 타입 보너스 (레거시 — HP 시스템 OFF 시 사용) ═══")]
    [Header("전반 0~35%")]
    [Range(-0.3f, 0.3f)] public float earlyBonus_Runner = 0.15f;
    [Range(-0.3f, 0.3f)] public float earlyBonus_Leader = 0.08f;
    [Range(-0.3f, 0.3f)] public float earlyBonus_Chaser = 0.0f;
    [Range(-0.3f, 0.3f)] public float earlyBonus_Reckoner = -0.05f;

    [Header("═══ 타입 보너스 (중반 35~70%) ═══")]
    [Range(-0.3f, 0.3f)] public float midBonus_Runner = -0.05f;
    [Range(-0.3f, 0.3f)] public float midBonus_Leader = 0.05f;
    [Range(-0.3f, 0.3f)] public float midBonus_Chaser = 0.10f;
    [Range(-0.3f, 0.3f)] public float midBonus_Reckoner = 0.03f;

    [Header("═══ 타입 보너스 (후반 70~100%) ═══")]
    [Range(-0.3f, 0.3f)] public float lateBonus_Runner = -0.10f;
    [Range(-0.3f, 0.3f)] public float lateBonus_Leader = 0.0f;
    [Range(-0.3f, 0.3f)] public float lateBonus_Chaser = 0.05f;
    [Range(-0.3f, 0.3f)] public float lateBonus_Reckoner = 0.18f;

    [Header("═══ 충돌 시스템 ═══")]
    [Tooltip("충돌 시스템 ON/OFF")]
    public bool enableCollision = true;

    [Tooltip("충돌 시각 효과 (흔들림, 이모지) ON/OFF")]
    public bool enableCollisionVFX = true;

    [Tooltip("충돌 발생 확률 (1.0 = 100%, 0.5 = 50%)")]
    [Range(0f, 1f)]
    public float collisionChance = 0.6f;

    [Tooltip("충돌 흔들림 세기 (0이면 흔들림 없음)")]
    [Range(0f, 0.15f)]
    public float shakeMagnitude = 0.05f;

    [Tooltip("충돌 흔들림 시간 - 승자 (초)")]
    [Range(0.05f, 1.0f)]
    public float shakeWinnerDuration = 0.15f;

    [Tooltip("충돌 흔들림 시간 - 패자 (초)")]
    [Range(0.05f, 1.0f)]
    public float shakeLoserDuration = 0.25f;

    [Tooltip("충돌 판정 거리")]
    [Range(0.1f, 3.0f)]
    public float collisionRange = 0.8f;

    [Tooltip("같은 상대 재충돌 쿨다운 (초)")]
    [Range(0.5f, 5.0f)]
    public float collisionCooldown = 2.0f;

    [Tooltip("기본 감속 비율 (0.3 = 30% 감속)")]
    [Range(0.05f, 0.8f)]
    public float collisionBasePenalty = 0.3f;

    [Tooltip("승자 감속 지속시간 (초)")]
    [Range(0.1f, 1.0f)]
    public float winnerPenaltyDuration = 0.3f;

    [Tooltip("패자 감속 지속시간 (초)")]
    [Range(0.1f, 2.0f)]
    public float loserPenaltyDuration = 0.5f;

    [Tooltip("밀집 감쇄: 반경 내 N마리 이상이면 충돌 확률 감소")]
    public int crowdThreshold = 3;

    [Tooltip("밀집 시 충돌 확률 배율 (0.5 = 50%로 감소)")]
    [Range(0.1f, 1.0f)]
    public float crowdDampen = 0.5f;

    [Tooltip("레이스 시작 후 충돌 억제 시간 (초). 이 시간 동안 충돌 판정 OFF → 속도 기반 대열 정리")]
    [Range(0f, 5f)]
    public float collisionSettlingTime = 2.0f;

    [Header("═══ 충돌 VFX 크기 설정 ═══")]
    [Tooltip("VFX 아이콘 크기 (배경 원 크기)\n기본값 0.5, 작게 하려면 0.2~0.3")]
    [Range(0.1f, 1.5f)]
    public float vfxIconScale = 0.5f;

    [Tooltip("VFX 텍스트 크기\n기본값 0.08, 작게 하려면 0.04~0.06")]
    [Range(0.02f, 0.2f)]
    public float vfxLabelSize = 0.08f;

    [Tooltip("VFX 표시 높이 (캐릭터 머리 위 오프셋)\n기본값 1.8")]
    [Range(0.5f, 3.0f)]
    public float vfxHeight = 1.8f;

    [Tooltip("VFX 떠오르는 속도\n기본값 1.5")]
    [Range(0.5f, 4.0f)]
    public float vfxFloatSpeed = 1.5f;

    [Header("═══ 충돌 VFX 커스텀 아이콘 (선택) ═══")]
    [Tooltip("충돌(HIT) 아이콘 스프라이트\n비워두면 기본 별 모양 사용")]
    public Sprite vfxHitIcon;

    [Tooltip("회피(DODGE) 아이콘 스프라이트\n비워두면 기본 방패 모양 사용")]
    public Sprite vfxDodgeIcon;

    [Tooltip("슬링샷(BOOST) 아이콘 스프라이트\n비워두면 기본 화살표 모양 사용")]
    public Sprite vfxSlingshotIcon;

    [Header("═══ 슬링샷 ═══")]
    [Tooltip("brave 1당 슬링샷 가속량")]
    [Range(0.005f, 0.1f)]
    public float slingshotFactor = 0.02f;

    [Tooltip("슬링샷 지속시간 (초)")]
    [Range(0.3f, 3.0f)]
    public float slingshotDuration = 1.0f;

    [Tooltip("슬링샷 최대 가속 배율 (과도한 버프 방지)")]
    [Range(0.1f, 1.0f)]
    public float slingshotMaxBoost = 0.4f;

    [Header("═══ 운 (Luck) ═══")]
    [Tooltip("크리티컬 부스트 판정 간격 (초)")]
    [Range(1f, 10f)]
    public float luckCheckInterval = 3.0f;

    [Tooltip("luck 1당 크리티컬 발동 확률 (luck 10이면 10×0.005=5%)")]
    [Range(0.001f, 0.02f)]
    public float luckCritChance = 0.005f;

    [Tooltip("크리티컬 부스트 속도 배율 (1.3 = 30% 가속)")]
    [Range(1.1f, 2.0f)]
    public float luckCritBoost = 1.3f;

    [Tooltip("크리티컬 부스트 지속시간 (초)")]
    [Range(0.5f, 3.0f)]
    public float luckCritDuration = 1.5f;

    [Tooltip("[V4에서는 미사용 — v4_intDodgeChance 사용]\nluck 1당 충돌 회피 확률 (레거시)")]
    [Range(0.005f, 0.05f)]
    public float luckDodgeChance = 0.02f;

    [Header("═══ 트랙 설정 ═══")]
    [Tooltip("현재 사용할 트랙 (None이면 일반 트랙)")]
    public TrackData currentTrack;

    [Tooltip("라운드별 트랙 랜덤 변경")]
    public bool randomTrackPerRound = false;

    [Tooltip("트랙 전환 연출 ON/OFF")]
    public bool enableTrackTransition = true;

    [Tooltip("트랙 전환 페이드 시간 (초)")]
    [Range(0.1f, 1.0f)]
    public float trackTransitionFadeDuration = 0.3f;

    [Header("═══ 세부 조정 ═══")]
    [Tooltip("웨이포인트 도착 판정 거리: 레이서가 다음 웨이포인트까지 이 거리 이내면 도착 처리. 작을수록 정밀하게 코너를 돔. (기본값: 0.25)")]
    [Range(0.1f, 1.0f)]
    public float waypointArrivalDist = 0.25f;

    [Tooltip("초기 속도 배율: 레이스 시작 시 baseSpeed × 이 값으로 출발. 1.0이면 풀스피드 출발, 낮을수록 천천히 가속. (기본값: 0.5)")]
    [Range(0.1f, 1.0f)]
    public float initialSpeedMultiplier = 0.5f;

    [Tooltip("공격 애니메이션 쿨다운(초): 충돌 시 공격 모션 재생 후 다음 공격까지 대기 시간. 짧으면 연속 공격 가능. (기본값: 0.6)")]
    [Range(0.1f, 2.0f)]
    public float attackAnimCooldown = 0.6f;

    [Header("═══ 폰트 설정 ═══")]
    [Tooltip("메인 폰트 (Ark Pixel 등 도트 폰트)\n비워두면 Unity 기본 폰트 사용")]
    public Font mainFont;

    [Tooltip("한글 전용 폰트 (메인 폰트에 한글이 없을 경우)\n비워두면 메인 폰트 사용")]
    public Font koreanFont;

    [Tooltip("UI 폰트 크기 배율\n도트 폰트가 기존보다 넓을 때 줄이기 (Neo둥근모: 0.65 권장)")]
    [Range(0.3f, 2.0f)]
    public float uiFontScale = 1.0f;

    // ══════════════════════════════════════
    //  Race V2 시스템 (Type 2)
    // ══════════════════════════════════════

    [Header("═══ Race V2: 시스템 전환 ═══")]
    [Tooltip("V2 레이스 시스템 사용 여부.\ntrue = Type 2 (전력질주+탈진), false = Type 1 (기존 HP 부스트)")]
    public bool useV2RaceSystem = false;

    [Header("═══ Race V2: HP 비례 속도 ═══")]
    [Tooltip("HP 100%일 때 스프린트 최대 보너스 (0.3 = +30% → 1.3×)")]
    [Range(0.1f, 0.5f)] public float v2_sprintMaxBoost = 0.30f;
    [Tooltip("탈진 경계 HP% (이 아래는 기본속도 미만). 0.25 = HP 25%에서 1.0×")]
    [Range(0.1f, 0.5f)] public float v2_sprintThreshold = 0.25f;
    [Tooltip("HP 0%일 때 최저 속도 배율 (0.75 = 25% 감속)")]
    [Range(0.5f, 0.95f)] public float v2_exhaustFloor = 0.70f;

    [Header("═══ Race V2: HP 소모 ═══")]
    [Tooltip("비스프린트 HP 소모율 (/초). 0이면 대기 중 소모 없음")]
    [Range(0f, 1.0f)] public float v2_baseDrain = 0.2f;
    [Tooltip("스프린트 HP 소모율 (/초, 절대값)")]
    [Range(1f, 10f)] public float v2_sprintDrainRate = 5.0f;

    [Header("═══ Race V2: 가속 곡선 ═══")]
    [Tooltip("전력질주 0→최대 도달 시간(초). 자동차 가속처럼 점진적")]
    [Range(1f, 5f)] public float v2_sprintAccelTime = 2.5f;

    [Header("═══ Race V2: 타입별 전략 (전력질주 시작 시점) ═══")]
    [Tooltip("도주(Runner): Strategy 구간 시작 즉시 전력질주")]
    [Range(0f, 0.95f)] public float v2_sprintStart_Runner   = 0.00f;
    [Tooltip("선행(Leader): Strategy 구간의 25% 진행 시 전력질주")]
    [Range(0f, 0.95f)] public float v2_sprintStart_Leader   = 0.25f;
    [Tooltip("선입(Chaser): Strategy 구간의 50% 진행 시 전력질주")]
    [Range(0f, 0.95f)] public float v2_sprintStart_Chaser   = 0.50f;
    [Tooltip("추입(Reckoner): Strategy 구간의 75% 진행 시 전력질주")]
    [Range(0f, 0.95f)] public float v2_sprintStart_Reckoner = 0.75f;

    /// <summary>
    /// V2: 타입별 전력질주 시작 시점 반환 (Strategy 구간의 0~1 비율)
    /// </summary>
    public float GetV2SprintStart(CharacterType type)
    {
        return type switch
        {
            CharacterType.Runner   => v2_sprintStart_Runner,
            CharacterType.Leader   => v2_sprintStart_Leader,
            CharacterType.Chaser   => v2_sprintStart_Chaser,
            _                      => v2_sprintStart_Reckoner
        };
    }

    /// <summary>
    /// V2: HP 비율 → 속도 배율 변환 (2구간 선형)
    /// HP ≥ threshold: 1.0 → 1.0+sprintMaxBoost (스프린트 존)
    /// HP &lt; threshold: exhaustFloor → 1.0 (탈진 존)
    /// </summary>
    public float GetV2SpeedFromHP(float hpRatio)
    {
        hpRatio = Mathf.Clamp01(hpRatio);
        if (hpRatio >= v2_sprintThreshold)
        {
            float t = (hpRatio - v2_sprintThreshold) / (1f - v2_sprintThreshold);
            return 1f + v2_sprintMaxBoost * t;
        }
        else
        {
            float t = hpRatio / v2_sprintThreshold;
            return v2_exhaustFloor + (1f - v2_exhaustFloor) * t;
        }
    }

    [Header("═══ Race V2: 타입별 구간 속도 계수 ═══")]
    [Tooltip("도주(Runner) 초반 속도 계수. 1.0 = 기본")]
    [Range(0.90f, 1.10f)] public float v2_phaseCoeff_Runner_early   = 1.000f;
    [Tooltip("도주(Runner) 중반 속도 계수")]
    [Range(0.90f, 1.10f)] public float v2_phaseCoeff_Runner_mid     = 0.985f;
    [Tooltip("도주(Runner) 후반(스프린트) 속도 계수")]
    [Range(0.90f, 1.10f)] public float v2_phaseCoeff_Runner_late    = 0.970f;

    [Tooltip("선행(Leader) 초반 속도 계수")]
    [Range(0.90f, 1.10f)] public float v2_phaseCoeff_Leader_early   = 0.985f;
    [Tooltip("선행(Leader) 중반 속도 계수")]
    [Range(0.90f, 1.10f)] public float v2_phaseCoeff_Leader_mid     = 0.993f;
    [Tooltip("선행(Leader) 후반(스프린트) 속도 계수")]
    [Range(0.90f, 1.10f)] public float v2_phaseCoeff_Leader_late    = 0.980f;

    [Tooltip("선입(Chaser) 초반 속도 계수")]
    [Range(0.90f, 1.10f)] public float v2_phaseCoeff_Chaser_early   = 0.965f;
    [Tooltip("선입(Chaser) 중반 속도 계수")]
    [Range(0.90f, 1.10f)] public float v2_phaseCoeff_Chaser_mid     = 1.000f;
    [Tooltip("선입(Chaser) 후반(스프린트) 속도 계수")]
    [Range(0.90f, 1.10f)] public float v2_phaseCoeff_Chaser_late    = 0.990f;

    [Tooltip("추입(Reckoner) 초반 속도 계수")]
    [Range(0.90f, 1.10f)] public float v2_phaseCoeff_Reckoner_early = 0.950f;
    [Tooltip("추입(Reckoner) 중반 속도 계수")]
    [Range(0.90f, 1.10f)] public float v2_phaseCoeff_Reckoner_mid   = 1.000f;
    [Tooltip("추입(Reckoner) 후반(스프린트) 속도 계수")]
    [Range(0.90f, 1.10f)] public float v2_phaseCoeff_Reckoner_late  = 1.015f;

    [Header("═══ Race V2: 속도 비례 HP 소모 ═══")]
    [Tooltip("기본 HP 소모율 (/초, 속도비 1.0 기준)")]
    [Range(0.5f, 5.0f)] public float v2_drainBaseRate = 1.0f;
    [Tooltip("속도-소모 지수 (2.0 = 속도비 제곱에 비례)")]
    [Range(1.0f, 3.0f)] public float v2_drainExponent = 2.0f;

    [Header("═══ Race V2: 구간 구분 ═══")]
    [Tooltip("초반→중반 전환 진행률 (0.33 = 33%)")]
    [Range(0.15f, 0.50f)] public float v2_phaseEarlyEnd = 0.33f;
    [Tooltip("중반→후반 전환 진행률 (0.66 = 66%)")]
    [Range(0.40f, 0.85f)] public float v2_phaseMidEnd   = 0.66f;

    /// <summary>
    /// V2: 타입 + 구간 → 속도 계수 반환
    /// phase: 0=early, 1=mid, 2=late
    /// </summary>
    public float GetV2PhaseCoeff(CharacterType type, int phase)
    {
        return (type, phase) switch
        {
            (CharacterType.Runner,   0) => v2_phaseCoeff_Runner_early,
            (CharacterType.Runner,   1) => v2_phaseCoeff_Runner_mid,
            (CharacterType.Runner,   _) => v2_phaseCoeff_Runner_late,
            (CharacterType.Leader,   0) => v2_phaseCoeff_Leader_early,
            (CharacterType.Leader,   1) => v2_phaseCoeff_Leader_mid,
            (CharacterType.Leader,   _) => v2_phaseCoeff_Leader_late,
            (CharacterType.Chaser,   0) => v2_phaseCoeff_Chaser_early,
            (CharacterType.Chaser,   1) => v2_phaseCoeff_Chaser_mid,
            (CharacterType.Chaser,   _) => v2_phaseCoeff_Chaser_late,
            (CharacterType.Reckoner, 0) => v2_phaseCoeff_Reckoner_early,
            (CharacterType.Reckoner, 1) => v2_phaseCoeff_Reckoner_mid,
            _                           => v2_phaseCoeff_Reckoner_late,
        };
    }

    /// <summary>
    /// V2: 현재 진행률 → 구간 번호 (0=early, 1=mid, 2=late)
    /// </summary>
    public int GetV2Phase(float overallProgress)
    {
        if (overallProgress < v2_phaseEarlyEnd) return 0;
        if (overallProgress < v2_phaseMidEnd)   return 1;
        return 2;
    }

    /// <summary>
    /// V2: 속도 비례 HP 소모율 계산.
    /// speedRatio = 실효 속도 / 기본 속도. drain ∝ ratio^exponent
    /// </summary>
    public float CalcV2SpeedDrain(float speedRatio)
    {
        speedRatio = Mathf.Clamp(speedRatio, 0.5f, 2.0f);
        return v2_drainBaseRate * Mathf.Pow(speedRatio, v2_drainExponent);
    }

    [Header("═══ 디버그 ═══")]
    [Tooltip("레이스 디버그 오버레이 (F1:토글 F2:상세 F3:캐릭터선택)")]
    public bool enableRaceDebug = false;

    /// <summary>
    /// 타입별 구간 보너스 가져오기
    /// phase: 0=전반, 1=중반, 2=후반
    /// </summary>
    public float GetTypeBonus(CharacterType type, int phase)
    {
        switch (phase)
        {
            case 0: // 전반
                switch (type)
                {
                    case CharacterType.Runner: return earlyBonus_Runner;
                    case CharacterType.Leader: return earlyBonus_Leader;
                    case CharacterType.Chaser: return earlyBonus_Chaser;
                    case CharacterType.Reckoner: return earlyBonus_Reckoner;
                }
                break;
            case 1: // 중반
                switch (type)
                {
                    case CharacterType.Runner: return midBonus_Runner;
                    case CharacterType.Leader: return midBonus_Leader;
                    case CharacterType.Chaser: return midBonus_Chaser;
                    case CharacterType.Reckoner: return midBonus_Reckoner;
                }
                break;
            case 2: // 후반
                switch (type)
                {
                    case CharacterType.Runner: return lateBonus_Runner;
                    case CharacterType.Leader: return lateBonus_Leader;
                    case CharacterType.Chaser: return lateBonus_Chaser;
                    case CharacterType.Reckoner: return lateBonus_Reckoner;
                }
                break;
        }
        return 0f;
    }

    /// <summary>
    /// 트랙 승수 적용된 값 가져오기 (트랙 없으면 1.0)
    /// </summary>
    public float GetTrackFloat(System.Func<TrackData, float> getter, float defaultVal = 1.0f)
    {
        return currentTrack != null ? getter(currentTrack) : defaultVal;
    }

    // ═══ 기존 호환 ═══
    public int firstPlaceScore => payoutExacta;
    public int secondPlaceScore => payoutPlace;

    // ═══ 싱글톤 로드 ═══
    private static GameSettings _instance;
    public static GameSettings Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = Resources.Load<GameSettings>("GameSettings");
                if (_instance == null)
                {
                    Debug.LogWarning("GameSettings 에셋을 찾을 수 없습니다! 기본값을 사용합니다.");
                    _instance = CreateInstance<GameSettings>();
                }
            }
            return _instance;
        }
    }

    // ═══ 리더보드 관리 (Inspector 우클릭 메뉴) ═══
    [ContextMenu("리더보드 초기화 (Top 100 삭제)")]
    private void ClearLeaderboard()
    {
        LeaderboardData.Clear();
        Debug.Log("★ 리더보드 전체 초기화 완료!");
    }

    [ContextMenu("리더보드 확인 (Console 출력)")]
    private void PrintLeaderboard()
    {
        var entries = LeaderboardData.GetTop(10);
        Debug.Log("★ 리더보드 상위 " + entries.Count + "개:");
        for (int i = 0; i < entries.Count; i++)
        {
            var e = entries[i];
            Debug.Log("  " + (i + 1) + "위: " + e.score + "점 (" + e.rounds + "R) " + e.date + " | " + e.summary);
        }
    }

    // ══════════════════════════════════════════════════════════════
    //  Race V3 연결
    // ══════════════════════════════════════════════════════════════

    [Header("═══ Race V3 ═══")]
    [Tooltip("V3 설정 에셋. 연결 시 V3 활성화, None이면 V2/V1 사용")]
    public GameSettingsV3 v3Settings;

    /// <summary>v3Settings가 연결되어 있으면 V3 활성. Inspector 슬롯으로 토글.</summary>
    public bool useV3RaceSystem => v3Settings != null && v4Settings == null;

    // ══════════════════════════════════════════════════════════════
    //  Race V4 연결 (우선순위 최고: V4 > V3 > Legacy)
    // ══════════════════════════════════════════════════════════════

    [Header("═══ Race V4 ═══")]
    [Tooltip("V4 설정 에셋. 연결 시 V4 활성화 (V3보다 우선).\n5대 스탯(Speed/Accel/Stamina/Power/Intelligence)+Luck 기반 완전 새 달리기 시스템")]
    public GameSettingsV4 v4Settings;

    /// <summary>v4Settings가 연결되어 있으면 V4 활성. V4 > V3 > Legacy 우선순위.</summary>
    public bool useV4RaceSystem => v4Settings != null;
}