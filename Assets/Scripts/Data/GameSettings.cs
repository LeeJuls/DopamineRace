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

    [Header("═══ 타입 보너스 (전반 0~35%) ═══")]
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