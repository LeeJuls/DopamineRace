/// <summary>
/// 레이스 1회에 대한 캐릭터별 인기도 + 배당 + 컨디션 정보
/// OddsCalculator.Calculate() 호출 시 생성, 해당 레이스 동안 유지
/// </summary>
[System.Serializable]
public class PopularityInfo
{
    /// <summary>캐릭터 UID</summary>
    public string charId;

    /// <summary>인기순위 (1=최강 → N=언더독)</summary>
    public int popularityRank;

    /// <summary>인기도 점수 (내부 계산용, 스탯+실적 혼합)</summary>
    public float popScore;

    /// <summary>단승 배당배수 (예: 2.5x → 2.5)</summary>
    public float winOdds;

    /// <summary>총 출전 횟수 (신뢰도 표시용)</summary>
    public int totalRaces;

    /// <summary>기록이 newCharThreshold 미만 = 신규</summary>
    public bool isNew;

    /// <summary>최근 순위 문자열 (예: "1-2-1-3-4")</summary>
    public string recentRankStr;

    /// <summary>이번 라운드 컨디션</summary>
    public Condition condition;

    /// <summary>컨디션 스탯 배수 (0.9~1.2)</summary>
    public float conditionMul;
}
