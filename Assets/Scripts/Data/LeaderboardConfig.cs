using UnityEngine;

/// <summary>
/// 원격 리더보드(Cloudflare Worker) 연결 설정. Resources/LeaderboardConfig.asset 로 로드.
/// 백엔드 독립 — apiBaseUrl/writeToken만 바꾸면 다른 백엔드로 교체 가능.
/// asset 없거나 enableRemote=false 면 100% 로컬(오프라인) 동작.
/// </summary>
[CreateAssetMenu(fileName = "LeaderboardConfig", menuName = "DopamineRace/LeaderboardConfig")]
public class LeaderboardConfig : ScriptableObject
{
    [Header("═══ 원격 토글 ═══")]
    [Tooltip("끄면 로컬 전용(오프라인). 클론/개발 기본값")]
    public bool enableRemote = false;        // 기본 OFF — 빈 asset/오프라인 안전

    [Header("═══ 엔드포인트 ═══")]
    [Tooltip("Worker URL (끝 슬래시 없이). 예: https://dopamine-leaderboard.xxx.workers.dev")]
    public string apiBaseUrl = "";
    [Tooltip("쓰기 토큰 — 빌드 동봉(약한 보안, 서버 검증이 본 방어). 비우면 제출만 비활성")]
    public string writeToken = "";

    [Header("═══ 튜닝 ═══")]
    [Tooltip("요청 타임아웃(초)")]
    public int timeoutSeconds = 8;
    [Tooltip("GET으로 가져올 행 수")]
    public int fetchCount = 100;
    [Tooltip("클라 조기 거부용 점수 상한(서버 MAX_SCORE 미러). 서버가 최종 판정")]
    public int maxPlausibleScore = 150000;

    /// <summary>원격 호출 가능 여부 — 토글 ON + URL 존재.</summary>
    public bool RemoteEnabled => enableRemote && !string.IsNullOrEmpty(apiBaseUrl);

    // ═══ 싱글톤 로드 (GameSettings 패턴) ═══
    private static LeaderboardConfig _instance;
    public static LeaderboardConfig Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = Resources.Load<LeaderboardConfig>("LeaderboardConfig");
                if (_instance == null)
                {
                    Debug.LogWarning("[LeaderboardConfig] asset 없음 — 로컬 전용 기본값 사용(enableRemote=false).");
                    _instance = CreateInstance<LeaderboardConfig>();   // enableRemote 기본 false 유지
                }
            }
            return _instance;
        }
    }
}
