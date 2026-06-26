// ─────────────────────────────────────────────────────────────────────────────
//  SteamAchievements — 도전과제 해금 래퍼 (DopamineRace)
//
//  ▣ 게임 코드에서는 SteamAchievements.Unlock(SteamAchievements.FirstWin) 형태로만 호출.
//  ▣ API Name 상수는 Steamworks 파트너 사이트에 등록한 "API Name"과 정확히 일치해야 함.
//  ▣ DR_STEAM 미정의 또는 Steam 미초기화 시 → 안전하게 무시 (no-op + 로그).
//
//  주의: 여기 API Name 은 내부 식별자(비노출)이므로 StringTable 키 발급 대상 아님.
//        실제 사용자에게 보이는 도전과제 "이름/설명"은 Steamworks 파트너 사이트에서
//        언어별로 입력함 (게임 StringTable.csv 가 아님).
// ─────────────────────────────────────────────────────────────────────────────

using UnityEngine;
#if DR_STEAM
using Steamworks;
#endif

/// <summary>
/// Steam 도전과제 해금 진입점. 모든 호출은 멱등(이미 해금 시 중복 전송 안 함).
/// </summary>
public static class SteamAchievements
{
    // ── API Name 상수 (파트너 사이트 등록값과 1:1 일치) ──
    public const string FirstBet  = "ACH_FIRST_BET";   // 첫 배팅으로 레이스 시작
    public const string FirstWin  = "ACH_FIRST_WIN";   // 첫 적중
    public const string ExactaHit = "ACH_EXACTA_HIT";  // 쌍승 적중
    public const string TrioHit   = "ACH_TRIO_HIT";    // 삼복승 적중
    public const string BigWin    = "ACH_BIG_WIN";     // 한 라운드 스톤 500+ 획득 (SPEC-047)
    public const string Rich      = "ACH_RICH";        // 누적 스톤 3000+ 보유 도달 (SPEC-047)
    public const string CatPower  = "ACH_CAT_POWER";   // 고양이의 힘 첫 사용
    public const string Rescue    = "ACH_RESCUE";      // 구제(파산 직전 생존)
    public const string FullClear = "ACH_FULL_CLEAR";  // 전 라운드 완주
    public const string GameOver  = "ACH_GAME_OVER";   // 첫 파산 경험

    /// <summary>
    /// 도전과제 해금. 이미 달성했으면 아무 것도 하지 않음.
    /// Steam 미연동/미초기화 환경에서도 안전 (no-op).
    /// </summary>
    public static void Unlock(string apiName)
    {
        if (string.IsNullOrEmpty(apiName)) return;

#if DR_STEAM
        if (!SteamManager.Initialized)
        {
            Debug.Log("[SteamAchievements] Steam 미초기화 — Unlock 무시: " + apiName);
            return;
        }

        // 멱등: 이미 해금된 도전과제면 재전송 생략
        if (SteamUserStats.GetAchievement(apiName, out bool already) && already)
            return;

        if (SteamUserStats.SetAchievement(apiName))
        {
            SteamUserStats.StoreStats();   // 즉시 업로드 → 오버레이 팝업 표시
            Debug.Log("[SteamAchievements] 해금: " + apiName);
        }
        else
        {
            Debug.LogWarning("[SteamAchievements] SetAchievement 실패(등록된 API Name인지 확인): " + apiName);
        }
#else
        Debug.Log("[SteamAchievements] (스텁) Unlock " + apiName + " — DR_STEAM 미정의");
#endif
    }

#if DR_STEAM && UNITY_EDITOR
    /// <summary>[에디터/테스트] 모든 도전과제·스탯 초기화. Steam 콘솔 reset_all_stats 대용.</summary>
    public static void DebugResetAll()
    {
        if (!SteamManager.Initialized) { Debug.LogWarning("[SteamAchievements] Steam 미초기화 — Reset 불가"); return; }
        SteamUserStats.ResetAllStats(true);
        SteamUserStats.StoreStats();
        Debug.Log("[SteamAchievements] 전체 도전과제/스탯 초기화 완료");
    }
#endif
}
