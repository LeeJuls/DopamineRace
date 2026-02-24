using UnityEngine;

/// <summary>
/// 게임 세팅 데이터 (Unity Inspector에서 편집 가능)
/// </summary>
[CreateAssetMenu(fileName = "GameSettings", menuName = "DopamineRace/GameSettings")]
public class GameSettings : ScriptableObject
{
    [Header("═══ 레이서 설정 ═══")]
    [Tooltip("한 레이스당 참가 레이서 수 (2~12)")]
    [Range(2, 12)]
    public int racerCount = 8;

    [Header("═══ 속도 설정 (레거시 - A-2에서 교체 예정) ═══")]
    [Tooltip("최소 속도")]
    public float racerMinSpeed = 1.0f;

    [Tooltip("최대 속도")]
    public float racerMaxSpeed = 4.0f;

    [Tooltip("속도 변경 간격 (초)")]
    public float speedChangeInterval = 3f;

    [Tooltip("속도 보간 속도")]
    public float speedLerpRate = 3f;

    [Header("═══ 라운드 설정 ═══")]
    [Tooltip("라운드별 바퀴 수 (배열 길이 = 총 라운드 수)\n예: [1,2,1,5,3,1,4] → 7라운드")]
    public int[] roundLaps = new int[] { 1, 2, 1, 5, 3, 1, 4 };

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

    [Header("═══ 아이콘 파일 설정 (얼굴 크롭 PNG) ═══")]
    [Tooltip("아이콘 파일 확대 배율")]
    [Range(0.5f, 3f)]
    public float iconFileZoom = 1.3f;

    [Tooltip("아이콘 파일 수직 오프셋")]
    [Range(-0.5f, 0.5f)]
    public float iconFileOffsetY = 0.05f;

    [Tooltip("아이콘 파일 수평 오프셋")]
    [Range(-0.5f, 0.5f)]
    public float iconFileOffsetX = 0f;

    [Header("═══ 프리팹 스프라이트 설정 (전신샷 fallback) ═══")]
    [Tooltip("프리팹 스프라이트 확대 배율")]
    [Range(0.5f, 7f)]
    public float iconPrefabZoom = 3f;

    [Tooltip("프리팹 스프라이트 수직 오프셋")]
    [Range(-1f, 1f)]
    public float iconPrefabOffsetY = 0.05f;

    [Tooltip("프리팹 스프라이트 수평 오프셋")]
    [Range(-1f, 1f)]
    public float iconPrefabOffsetX = 0f;

    [Header("═══ 배팅 버튼 설정 ═══")]
    public float bettingButtonHeight = 90f;

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

    [Header("═══ 레이스 기본 공식 ═══")]
    [Tooltip("전체 속도 배율 (캐릭터 speed에 곱해짐)")]
    [Range(0.5f, 5.0f)]
    public float globalSpeedMultiplier = 2.5f;

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
    public float hpPerEndurance = 2.5f;
    [Tooltip("기본 소모율 (전 타입 공통, 달리는 내내 적용)")]
    [Range(0.05f, 1.0f)]
    public float basicConsumptionRate = 0.2f;
    [Tooltip("가속→감속 전환 임계점 (consumed% 기준)")]
    [Range(0.3f, 0.8f)]
    public float boostThreshold = 0.6f;

    [Header("═══ HP: 도주 (Runner) ═══")]
    [Tooltip("적극 소모 시작 시점 (OverallProgress)")]
    [Range(0f, 0.9f)]  public float runner_spurtStart = 0.00f;
    [Tooltip("적극 소모율")]
    [Range(0.5f, 5.0f)] public float runner_activeRate = 2.5f;
    [Tooltip("60% 소모 시 최대 부스트")]
    [Range(0.01f, 0.2f)] public float runner_peakBoost = 0.12f;
    [Tooltip("가속 곡선 지수 (높을수록 후반 급격)")]
    [Range(0.5f, 3.0f)] public float runner_accelExp = 1.5f;
    [Tooltip("감속 곡선 지수 (높을수록 완만)")]
    [Range(0.3f, 3.0f)] public float runner_decelExp = 0.8f;
    [Tooltip("탈진 페널티 (음수)")]
    [Range(-0.15f, 0f)] public float runner_exhaustionFloor = -0.05f;

    [Header("═══ HP: 선행 (Leader) ═══")]
    [Range(0f, 0.9f)]  public float leader_spurtStart = 0.10f;
    [Range(0.5f, 5.0f)] public float leader_activeRate = 1.5f;
    [Range(0.01f, 0.2f)] public float leader_peakBoost = 0.09f;
    [Range(0.5f, 3.0f)] public float leader_accelExp = 1.2f;
    [Range(0.3f, 3.0f)] public float leader_decelExp = 1.8f;
    [Range(-0.15f, 0f)] public float leader_exhaustionFloor = -0.03f;

    [Header("═══ HP: 선입 (Chaser) ═══")]
    [Range(0f, 0.9f)]  public float chaser_spurtStart = 0.45f;
    [Range(0.5f, 5.0f)] public float chaser_activeRate = 2.0f;
    [Range(0.01f, 0.2f)] public float chaser_peakBoost = 0.11f;
    [Range(0.5f, 3.0f)] public float chaser_accelExp = 1.3f;
    [Range(0.3f, 3.0f)] public float chaser_decelExp = 1.5f;
    [Range(-0.15f, 0f)] public float chaser_exhaustionFloor = -0.04f;

    [Header("═══ HP: 추행 (Reckoner) ═══")]
    [Range(0f, 0.9f)]  public float reckoner_spurtStart = 0.72f;
    [Range(0.5f, 5.0f)] public float reckoner_activeRate = 3.0f;
    [Range(0.01f, 0.2f)] public float reckoner_peakBoost = 0.12f;
    [Range(0.5f, 3.0f)] public float reckoner_accelExp = 2.0f;
    [Range(0.3f, 3.0f)] public float reckoner_decelExp = 0.5f;
    [Range(-0.15f, 0f)] public float reckoner_exhaustionFloor = -0.05f;

    [Header("═══ HP: 포지션 보정 ═══")]
    [Tooltip("Pace Lead: 선행이 상위 순위일 때 activeRate 감소율")]
    [Range(0f, 0.4f)] public float paceLeadReduction = 0.15f;
    [Tooltip("Pace Lead 효과 감소 시작 시점")]
    [Range(0.5f, 0.9f)] public float paceLeadFadeStart = 0.7f;
    [Tooltip("Pace Lead 발동 최대 순위")]
    [Range(1, 6)] public int paceLeadMaxRank = 3;

    [Tooltip("Slipstream: 선입이 중간 순위일 때 basicRate 감소율")]
    [Range(0f, 0.4f)] public float slipstreamReduction = 0.20f;
    [Tooltip("Slipstream 발동 최소 순위")]
    [Range(1, 12)] public int slipstreamMinRank = 3;
    [Tooltip("Slipstream 발동 최대 순위")]
    [Range(1, 12)] public int slipstreamMaxRank = 7;
    [Tooltip("Slipstream 페이드 시간 (초)")]
    [Range(0.5f, 5f)] public float slipstreamFadeTime = 2.0f;

    [Tooltip("Conservation Amplifier 계수 (추행 전용)")]
    [Range(0f, 1.5f)] public float conservationAmpCoeff = 0.6f;

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
    /// charEndurance → maxHP 계산
    /// </summary>
    public float CalcMaxHP(float endurance)
    {
        return hpBase + endurance * hpPerEndurance;
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

    [Tooltip("luck 1당 충돌 회피 확률 (luck 15면 15×0.02=30% 회피)")]
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

    [Header("═══ 디버그 ═══")]
    [Tooltip("레이스 디버그 오버레이 (F1:토글 F2:상세 F3:캐릭터선택)")]
    public bool enableRaceDebug = true;

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
}