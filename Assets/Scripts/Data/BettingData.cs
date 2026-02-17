using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 배팅 타입 정의
/// </summary>
public enum BetType
{
    Win,            // 단승: 1등 1마리 맞추기
    Place,          // 연승: 1~2등 안에 드는 말 1마리
    Quinella,       // 복승: 1~2등 2마리 (순서 무관)
    Exacta,         // 쌍승: 1등+2등 정확한 순서
    Trio,           // 삼복승: 1~3등 3마리 (순서 무관)
    Wide,           // 복연승: 1~3등 중 2마리 (순서 무관)
}

/// <summary>
/// 배팅 1건의 정보
/// </summary>
[System.Serializable]
public class BetInfo
{
    public BetType type;
    public List<int> selections = new List<int>();

    public BetInfo(BetType t)
    {
        type = t;
        selections = new List<int>();
    }

    /// <summary>
    /// 이 배팅에 필요한 선택 수
    /// </summary>
    public int RequiredSelections
    {
        get
        {
            switch (type)
            {
                case BetType.Win:      return 1;
                case BetType.Place:    return 1;
                case BetType.Quinella: return 2;
                case BetType.Exacta:   return 2;
                case BetType.Trio:     return 3;
                case BetType.Wide:     return 2;
                default: return 1;
            }
        }
    }

    public bool IsComplete => selections.Count >= RequiredSelections;

    /// <summary>
    /// 현재 선택 안내 텍스트
    /// </summary>
    public string GetSelectionGuide()
    {
        switch (type)
        {
            case BetType.Win:
                return selections.Count == 0 ? "1착 예측 선택!" : "선택 완료!";
            case BetType.Place:
                return selections.Count == 0 ? "3착 이내 들어올 말 선택!" : "선택 완료!";
            case BetType.Quinella:
                if (selections.Count == 0) return "1~2착 할 말 2마리 선택!";
                if (selections.Count == 1) return "1마리 더 선택!";
                return "선택 완료!";
            case BetType.Exacta:
                if (selections.Count == 0) return "1착 예측 선택!";
                if (selections.Count == 1) return "2착 예측 선택!";
                return "선택 완료!";
            case BetType.Trio:
                if (selections.Count == 0) return "1~3착 할 말 3마리 선택!";
                if (selections.Count == 1) return "2마리 더 선택!";
                if (selections.Count == 2) return "1마리 더 선택!";
                return "선택 완료!";
            case BetType.Wide:
                if (selections.Count == 0) return "3착 이내 들어올 말 2마리 선택!";
                if (selections.Count == 1) return "1마리 더 선택!";
                return "선택 완료!";
            default:
                return "";
        }
    }

    /// <summary>
    /// 선택된 레이서에 표시할 라벨
    /// </summary>
    public string GetSelectionLabel(int selectionOrder)
    {
        switch (type)
        {
            case BetType.Win:      return "1착";
            case BetType.Place:    return "입상";
            case BetType.Exacta:   return selectionOrder == 0 ? "1착" : "2착";
            case BetType.Quinella: return "선택";
            case BetType.Wide:     return "선택";
            case BetType.Trio:     return "선택";
            default: return "";
        }
    }
}

/// <summary>
/// 배팅 점수 계산 & 유틸리티
/// </summary>
public static class BettingCalculator
{
    /// <summary>
    /// 배팅 결과 점수 계산
    /// rankings: 인덱스 0=1등, 1=2등, 2=3등... (racerIndex 값)
    /// </summary>
    public static int Calculate(BetInfo bet, List<int> rankings)
    {
        if (bet == null || rankings == null || rankings.Count < 3) return 0;
        if (!bet.IsComplete) return 0;

        var s = bet.selections;
        var gs = GameSettings.Instance;

        switch (bet.type)
        {
            case BetType.Win:
                // 단승: 선택 = 1등
                return rankings[0] == s[0] ? gs.payoutWin : 0;

            case BetType.Place:
                // 연승: 3착 이내 (7두 이하 시 2착 이내)
                int placeCount = GameSettings.Instance.racerCount <= 7 ? 2 : 3;
                for (int i = 0; i < placeCount && i < rankings.Count; i++)
                {
                    if (rankings[i] == s[0]) return gs.payoutPlace;
                }
                return 0;

            case BetType.Quinella:
                // 복승: 두 마리가 1~2등 (순서 무관)
                bool q1 = (rankings[0] == s[0] || rankings[0] == s[1]);
                bool q2 = (rankings[1] == s[0] || rankings[1] == s[1]);
                return (q1 && q2) ? gs.payoutQuinella : 0;

            case BetType.Exacta:
                // 쌍승: 1등, 2등 순서 정확히
                return (rankings[0] == s[0] && rankings[1] == s[1])
                    ? gs.payoutExacta : 0;

            case BetType.Trio:
                // 삼복승: 세 마리가 1~3등 (순서 무관)
                HashSet<int> top3 = new HashSet<int> { rankings[0], rankings[1], rankings[2] };
                return (top3.Contains(s[0]) && top3.Contains(s[1]) && top3.Contains(s[2]))
                    ? gs.payoutTrio : 0;

            case BetType.Wide:
                // 복연승: 두 마리가 1~3등 안에 (순서 무관)
                HashSet<int> top3w = new HashSet<int> { rankings[0], rankings[1], rankings[2] };
                return (top3w.Contains(s[0]) && top3w.Contains(s[1]))
                    ? gs.payoutWide : 0;

            default:
                return 0;
        }
    }

    public static string GetTypeName(BetType type)
    {
        switch (type)
        {
            case BetType.Win:      return "단승";
            case BetType.Place:    return "연승";
            case BetType.Quinella: return "복승";
            case BetType.Exacta:   return "쌍승";
            case BetType.Trio:     return "삼복승";
            case BetType.Wide:     return "복연승";
            default: return "";
        }
    }

    public static string GetTypeDesc(BetType type)
    {
        switch (type)
        {
            case BetType.Win:      return "1착 맞추기";
            case BetType.Place:    return "3착 이내 1마리 (7두↓ 2착)";
            case BetType.Quinella: return "1~2착 2마리 (순서무관)";
            case BetType.Exacta:   return "1착+2착 순서 정확히";
            case BetType.Trio:     return "1~3착 3마리 (순서무관)";
            case BetType.Wide:     return "3착 이내 2마리 (순서무관)";
            default: return "";
        }
    }

    public static int GetPayout(BetType type)
    {
        var gs = GameSettings.Instance;
        switch (type)
        {
            case BetType.Win:      return gs.payoutWin;
            case BetType.Place:    return gs.payoutPlace;
            case BetType.Quinella: return gs.payoutQuinella;
            case BetType.Exacta:   return gs.payoutExacta;
            case BetType.Trio:     return gs.payoutTrio;
            case BetType.Wide:     return gs.payoutWide;
            default: return 0;
        }
    }
}