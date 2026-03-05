#if UNITY_EDITOR
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// [EDITOR ONLY] 결과창 디버그 미리보기
///
/// F8  : 결과창 즉시 표시 (Win 배팅, 1등 선택 → 무조건 적중)
/// F9  : 결과창 즉시 표시 (Exacta 배팅, 1·2등 선택 → 적중)
/// F10 : 결과창 즉시 표시 (Trio 배팅, 랜덤 → 적중)
///
/// 모든 단축키는 플레이 모드에서만 동작합니다.
/// </summary>
public partial class SceneBootstrapper
{
    // ══════════════════════════════════════
    //  디버그 입력 체크 (Update에서 호출)
    // ══════════════════════════════════════
    private void UpdateDebugInput()
    {
        if (Input.GetKeyDown(KeyCode.F8))
            DebugJumpToResult(BetType.Win,      hitGuaranteed: true);
        if (Input.GetKeyDown(KeyCode.F9))
            DebugJumpToResult(BetType.Exacta,   hitGuaranteed: true);
        if (Input.GetKeyDown(KeyCode.F10))
            DebugJumpToResult(BetType.Trio,     hitGuaranteed: true);
    }

    // ══════════════════════════════════════
    //  결과창 강제 표시
    // ══════════════════════════════════════
    /// <param name="betType">테스트할 배팅 타입</param>
    /// <param name="hitGuaranteed">true면 선택한 캐릭터가 실제 순위에 맞게 픽 → 무조건 적중</param>
    private void DebugJumpToResult(BetType betType, bool hitGuaranteed)
    {
        var db   = CharacterDatabase.Instance;
        int need = GameSettings.Instance?.racerCount ?? 9; // 9명 고정

        // SelectedCharacters 보장 (미선발 상태면 즉시 선발)
        if (db != null && db.SelectedCharacters.Count < need)
            db.SelectRandom(need);

        int count = (db != null) ? Mathf.Min(db.SelectedCharacters.Count, need) : need;

        // ── 1. 랜덤 순위 생성 (SelectedCharacters 기준 — ShowResult와 동일한 인덱스 체계) ──
        var indices = new List<int>();
        for (int i = 0; i < count; i++) indices.Add(i);

        // Fisher-Yates 셔플
        for (int i = count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            int tmp = indices[i]; indices[i] = indices[j]; indices[j] = tmp;
        }

        var fakeRankings = new List<RaceManager.RankingEntry>();
        for (int r = 0; r < count; r++)
        {
            string name = (db != null && indices[r] < db.SelectedCharacters.Count)
                ? db.SelectedCharacters[indices[r]].DisplayName
                : "Racer " + (indices[r] + 1);

            fakeRankings.Add(new RaceManager.RankingEntry
            {
                rank      = r + 1,
                racerName = name,
                racerIndex = indices[r]
            });
        }
        RaceManager.Instance?.DebugSetFinalRankings(fakeRankings);

        // ── 2. 가짜 배팅 생성 ──
        var fakeBet = new BetInfo(betType);

        if (hitGuaranteed)
        {
            // 실제 순위 기준으로 선택 → 맞춤 보장
            switch (betType)
            {
                case BetType.Win:
                    fakeBet.selections.Add(indices[0]); // 실제 1위
                    break;
                case BetType.Place:
                    fakeBet.selections.Add(indices[0]); // 실제 1위 (연대)
                    break;
                case BetType.Exacta:
                    fakeBet.selections.Add(indices[0]); // 실제 1위
                    fakeBet.selections.Add(indices[1]); // 실제 2위
                    break;
                case BetType.Quinella:
                    fakeBet.selections.Add(indices[0]); // 1·2위 (순서무관)
                    fakeBet.selections.Add(indices[1]);
                    break;
                case BetType.Wide:
                    fakeBet.selections.Add(indices[0]); // 3위 내 2마리
                    fakeBet.selections.Add(indices[1]);
                    break;
                case BetType.Trio:
                    fakeBet.selections.Add(indices[0]); // 1·2·3위 (순서무관)
                    fakeBet.selections.Add(indices[1]);
                    fakeBet.selections.Add(indices[2]);
                    break;
            }
        }
        else
        {
            // 틀린 배팅 (꼴찌 선택)
            fakeBet.selections.Add(indices[count - 1]);
        }

        GameManager.Instance?.DebugSetCurrentBet(fakeBet);

        // ── 3. Result 상태 전환 (CalcScore() 자동 실행) ──
        GameManager.Instance?.ChangeState(GameManager.GameState.Result);

        Debug.Log(string.Format(
            "[DEBUG] 결과창 강제 표시 — BetType={0}, Hit={1}, 1위={2}",
            betType, hitGuaranteed,
            fakeRankings.Count > 0 ? fakeRankings[0].racerName : "?"));
    }
}
#endif
