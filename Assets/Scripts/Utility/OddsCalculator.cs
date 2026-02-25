using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 배당 계산 + 컨디션 결정 전담 static 클래스
/// GameManager.StartCountdown() 직전에 Calculate() 호출
/// </summary>
public static class OddsCalculator
{
    // ══════════════════════════════════════════
    //  현재 라운드 배당/컨디션 정보 (레이스 동안 유지)
    // ══════════════════════════════════════════

    public static List<PopularityInfo> CurrentOdds { get; private set; }
        = new List<PopularityInfo>();

    // ══════════════════════════════════════════
    //  메인 진입점
    // ══════════════════════════════════════════

    /// <summary>
    /// 매 라운드 시작 시 호출.
    /// 1) 컨디션 뽑기  2) PopScore 계산  3) 인기순위 정렬  4) 배당 산출
    /// </summary>
    public static void Calculate(List<CharacterData> racers, string currentTrackId)
    {
        CurrentOdds.Clear();
        if (racers == null || racers.Count == 0) return;

        var gs  = GameSettings.Instance;
        var sm  = ScoreManager.Instance;

        // ── Step 1: 각 캐릭터 PopScore + 컨디션 계산 ──
        foreach (var racer in racers)
        {
            var record   = sm != null ? sm.GetCharacterRecord(racer.charId) : null;
            float popScore = CalcPopScore(racer, record, currentTrackId);
            Condition cond = ConditionHelper.Roll();

            var info = new PopularityInfo
            {
                charId         = racer.charId,
                popScore       = popScore,
                totalRaces     = record != null ? record.TotalRaces : 0,
                isNew          = (record == null || record.TotalRaces < gs.newCharThreshold),
                recentRankStr  = record != null ? record.GetOverallRankString() : "-",
                condition      = cond,
                conditionMul   = ConditionHelper.GetMultiplier(cond)
            };
            CurrentOdds.Add(info);
        }

        // ── Step 2: PopScore 내림차순 → 인기순위 배정 ──
        CurrentOdds.Sort((a, b) => b.popScore.CompareTo(a.popScore));
        for (int i = 0; i < CurrentOdds.Count; i++)
            CurrentOdds[i].popularityRank = i + 1;

        // ── Step 3: 인기순위 → 단승 배당 산출 ──
        float maxScore = CurrentOdds[0].popScore;
        float minScore = CurrentOdds[CurrentOdds.Count - 1].popScore;

        foreach (var info in CurrentOdds)
        {
            info.winOdds = CalcWinOdds(
                info.popularityRank,
                CurrentOdds.Count,
                info.popScore,
                maxScore,
                minScore,
                info.totalRaces
            );
        }

        // ── 디버그 로그 ──
        Debug.Log(string.Format("[OddsCalculator] 배당 계산 완료: {0}마리 | 트랙: {1}",
            CurrentOdds.Count, currentTrackId));
        foreach (var info in CurrentOdds)
        {
            Debug.Log(string.Format(
                "  {0,-4}: 인기{1,2}위 | 단승{2,5:F1}x | 컨디션:{3}({4:F2}x) | PopScore:{5:F3} | 출전:{6}{7}",
                info.charId, info.popularityRank, info.winOdds,
                ConditionHelper.GetDisplayName(info.condition), info.conditionMul,
                info.popScore, info.totalRaces, info.isNew ? " [신규]" : ""));
        }
    }

    // ══════════════════════════════════════════
    //  배당 조회 (외부 사용)
    // ══════════════════════════════════════════

    /// <summary>특정 캐릭터의 단승 배당 반환 (없으면 기본값)</summary>
    public static float GetWinOdds(string charId)
    {
        foreach (var info in CurrentOdds)
            if (info.charId == charId) return info.winOdds;
        return GameSettings.Instance != null ? GameSettings.Instance.payoutWin : 3f;
    }

    /// <summary>특정 캐릭터의 PopularityInfo 반환 (없으면 null)</summary>
    public static PopularityInfo GetInfo(string charId)
    {
        foreach (var info in CurrentOdds)
            if (info.charId == charId) return info;
        return null;
    }

    /// <summary>특정 캐릭터의 컨디션 배수 반환 (RacerController에서 사용)</summary>
    public static float GetConditionMultiplier(string charId)
    {
        var info = GetInfo(charId);
        return info != null ? info.conditionMul : 1.0f;
    }

    /// <summary>
    /// 복합 승식 최종 배당배수 계산.
    /// 적중 시 배당배수 반환, 미적중 시 0 반환.
    /// </summary>
    public static float CalcPayout(BetInfo bet, List<int> rankings, List<CharacterData> racers)
    {
        if (!CheckHit(bet, rankings)) return 0f;

        List<float> selectedOdds = new List<float>();
        foreach (int sel in bet.selections)
        {
            if (sel < racers.Count)
                selectedOdds.Add(GetWinOdds(racers[sel].charId));
        }

        return CalcComboOdds(bet.type, selectedOdds);
    }

    /// <summary>
    /// 현재 선택 기준 예상 배당배수 (UI 표시용).
    /// 선택이 완료되지 않아도 계산.
    /// </summary>
    public static float GetExpectedOdds(BetInfo bet, List<CharacterData> racers)
    {
        if (bet == null || bet.selections.Count == 0) return 0f;

        List<float> selectedOdds = new List<float>();
        foreach (int sel in bet.selections)
        {
            if (sel < racers.Count)
                selectedOdds.Add(GetWinOdds(racers[sel].charId));
        }

        return CalcComboOdds(bet.type, selectedOdds);
    }

    // ══════════════════════════════════════════
    //  내부 계산 메서드
    // ══════════════════════════════════════════

    /// <summary>
    /// PopScore 계산.
    /// 기록 없음: 스탯 100%
    /// 기록 1~10판: 스탯 비중 점진적 감소 (최소 30%)
    /// 기록 10판+: 스탯 30% + 실적 70% 고정
    /// 트랙 기록 minRaces+ 있으면 트랙 가중 반영
    /// </summary>
    private static float CalcPopScore(CharacterData racer, CharacterRecord record,
        string trackId)
    {
        var gs = GameSettings.Instance;

        // ── 스탯 점수 (정규화: 스탯 최대합 ≈ 20) ──
        float statScore = (racer.charBaseSpeed    * gs.oddsStatWeight_speed
                         + racer.charBasePower    * gs.oddsStatWeight_power
                         + racer.charBaseLuck     * gs.oddsStatWeight_luck
                         + racer.charBaseEndurance* gs.oddsStatWeight_endurance
                         + racer.charBaseBrave    * gs.oddsStatWeight_brave
                         + racer.charBaseCalm     * gs.oddsStatWeight_calm)
                         / 20f;

        // 기록 없음 → 스탯 100%
        if (record == null || record.TotalRaces == 0)
            return statScore;

        // ── 실적 점수 (전체 기록 기반) ──
        float recordScore = record.WinRate   * gs.oddsRecordWeight_win
                          + record.PlaceRate * gs.oddsRecordWeight_place
                          + record.ShowRate  * gs.oddsRecordWeight_show;

        // ── 스탯/실적 비중 계산 ──
        // 10판 기준으로 실적 비중 최대 70%까지 증가, 스탯 최소 30% 항상 유지
        float recordRatio = Mathf.Min(0.7f, record.TotalRaces / 10f * 0.7f);
        float statRatio   = 1f - recordRatio;
        float overallScore = statScore * statRatio + recordScore * recordRatio;

        // ── 트랙 보정 ──
        var trackRec = record.FindTrackRecord(trackId);
        if (trackRec == null || trackRec.raceCount < gs.trackOddsMinRaces)
            return overallScore;

        float trackWinRate   = CountRankRate(trackRec.recentRanks, 1);
        float trackPlaceRate = CountRankRate(trackRec.recentRanks, 2);
        float trackShowRate  = CountRankRate(trackRec.recentRanks, 3);
        float trackScore = trackWinRate   * gs.oddsRecordWeight_win
                         + trackPlaceRate * gs.oddsRecordWeight_place
                         + trackShowRate  * gs.oddsRecordWeight_show;

        float tw = gs.trackOddsWeight;
        return (1f - tw) * overallScore + tw * trackScore;
    }

    /// <summary>인기순위 → 단승 배당 변환</summary>
    private static float CalcWinOdds(int rank, int totalRacers, float popScore,
        float maxScore, float minScore, int totalRaces)
    {
        var gs = GameSettings.Instance;

        // 배당 범위 배열 범위 초과 방지
        int idx = Mathf.Clamp(rank - 1, 0, gs.oddsRangeMin.Length - 1);
        float minOdds = gs.oddsRangeMin[idx];
        float maxOdds = gs.oddsRangeMax[idx];

        // 같은 순위 내에서도 PopScore 차이로 세분화
        float range = maxScore - minScore;
        float t = range > 0.001f ? (popScore - minScore) / range : 0.5f;
        float odds = Mathf.Lerp(maxOdds, minOdds, t); // 점수 높을수록 배당 낮음

        // 신규 캐릭터 불확실성 (±variance)
        if (totalRaces < gs.newCharThreshold)
        {
            float v = gs.newCharOddsVariance;
            odds *= Random.Range(1f - v, 1f + v);
            odds = Mathf.Clamp(odds, minOdds * 0.5f, maxOdds * 1.5f);
        }

        // 소수점 1자리 반올림
        return Mathf.Round(odds * 10f) / 10f;
    }

    /// <summary>복합 승식 배당 계산</summary>
    private static float CalcComboOdds(BetType type, List<float> selectedOdds)
    {
        if (selectedOdds.Count == 0) return 1f;
        var gs = GameSettings.Instance;

        switch (type)
        {
            case BetType.Win:
                return Mathf.Clamp(selectedOdds[0], 1.1f, 99f);

            case BetType.Place:
                // 연승: 여러 말 중 하나 적중 → 평균 단승배당 × 계수
                float sum = 0f;
                foreach (float o in selectedOdds) sum += o;
                return Mathf.Clamp(
                    (sum / selectedOdds.Count) * gs.oddsCoef_place,
                    gs.oddsMin_place, gs.oddsMax_place);

            case BetType.Quinella:
                if (selectedOdds.Count < 2) return gs.oddsMin_quinella;
                return Mathf.Clamp(
                    selectedOdds[0] * selectedOdds[1] * gs.oddsCoef_quinella,
                    gs.oddsMin_quinella, gs.oddsMax_quinella);

            case BetType.Exacta:
                if (selectedOdds.Count < 2) return gs.oddsMin_exacta;
                return Mathf.Clamp(
                    selectedOdds[0] * selectedOdds[1] * gs.oddsCoef_exacta,
                    gs.oddsMin_exacta, gs.oddsMax_exacta);

            case BetType.Wide:
                if (selectedOdds.Count < 2) return gs.oddsMin_wide;
                return Mathf.Clamp(
                    selectedOdds[0] * selectedOdds[1] * gs.oddsCoef_wide,
                    gs.oddsMin_wide, gs.oddsMax_wide);

            case BetType.Trio:
                if (selectedOdds.Count < 3) return gs.oddsMin_trio;
                return Mathf.Clamp(
                    selectedOdds[0] * selectedOdds[1] * selectedOdds[2] * gs.oddsCoef_trio,
                    gs.oddsMin_trio, gs.oddsMax_trio);

            default: return 1f;
        }
    }

    /// <summary>적중 여부 확인</summary>
    private static bool CheckHit(BetInfo bet, List<int> rankings)
    {
        if (bet == null || rankings == null || rankings.Count < 3) return false;
        var s = bet.selections;

        switch (bet.type)
        {
            case BetType.Win:
                return rankings[0] == s[0];

            case BetType.Place:
                int placeCount = GameSettings.Instance.racerCount <= 7 ? 2 : 3;
                for (int i = 0; i < placeCount && i < rankings.Count; i++)
                    if (rankings[i] == s[0]) return true;
                return false;

            case BetType.Quinella:
                if (s.Count < 2) return false;
                return (rankings[0] == s[0] || rankings[0] == s[1])
                    && (rankings[1] == s[0] || rankings[1] == s[1]);

            case BetType.Exacta:
                if (s.Count < 2) return false;
                return rankings[0] == s[0] && rankings[1] == s[1];

            case BetType.Trio:
                if (s.Count < 3) return false;
                var top3 = new System.Collections.Generic.HashSet<int>
                    { rankings[0], rankings[1], rankings[2] };
                return top3.Contains(s[0]) && top3.Contains(s[1]) && top3.Contains(s[2]);

            case BetType.Wide:
                if (s.Count < 2) return false;
                var top3w = new System.Collections.Generic.HashSet<int>
                    { rankings[0], rankings[1], rankings[2] };
                return top3w.Contains(s[0]) && top3w.Contains(s[1]);

            default: return false;
        }
    }

    /// <summary>순위 리스트에서 특정 순위 비율 계산</summary>
    private static float CountRankRate(List<int> ranks, int targetRank)
    {
        if (ranks == null || ranks.Count == 0) return 0f;
        int count = 0;
        foreach (int r in ranks) if (r == targetRank) count++;
        return (float)count / ranks.Count;
    }
}
