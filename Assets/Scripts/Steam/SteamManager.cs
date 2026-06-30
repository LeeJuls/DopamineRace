// ─────────────────────────────────────────────────────────────────────────────
//  SteamManager — Steamworks.NET 초기화/종료 부트스트랩 (DopamineRace)
//
//  ▣ 활성 조건: Player Settings > Scripting Define Symbols 에 `DR_STEAM` 추가 +
//    Steamworks.NET 플러그인(.unitypackage) 임포트 완료 시.
//  ▣ DR_STEAM 미정의 시: 전체가 안전한 no-op 스텁으로 컴파일됨 (빌드 안 깨짐).
//
//  설치 순서:
//    1. https://github.com/rlabrecque/Steamworks.NET/releases 에서
//       Steamworks.NET .unitypackage 임포트
//    2. 프로젝트 루트 steam_appid.txt 에 AppID(4532310) 확인 (이미 존재)
//    3. Player Settings > Scripting Define Symbols 에 `DR_STEAM` 추가
//    4. 씬 배치 불필요 — RuntimeInitializeOnLoadMethod(Bootstrap)로 자동 생성됨
// ─────────────────────────────────────────────────────────────────────────────

using UnityEngine;
#if DR_STEAM
using System;
using Steamworks;
#endif

/// <summary>
/// Steam 클라이언트 API 수명주기 관리. 씬에 1개만 존재(DontDestroyOnLoad).
/// 공식 Steamworks.NET SteamManager 패턴을 DopamineRace 규칙에 맞춰 경량화.
/// </summary>
[DisallowMultipleComponent]
public class SteamManager : MonoBehaviour
{
    /// <summary>DopamineRace Steam AppID.</summary>
    public const uint AppId = 4532310;

#if DR_STEAM
    private static SteamManager s_instance;
    private static bool s_everInitialized;

    private bool m_initialized;
    private SteamAPIWarningMessageHook_t m_warningHook;

    /// <summary>Steam API가 정상 초기화되었는가. 도전과제/스탯 호출 전 반드시 확인.</summary>
    public static bool Initialized => s_instance != null && s_instance.m_initialized;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetForDomainReloadOff()
    {
        // BeforeSceneLoad의 Bootstrap()보다 먼저 실행 → s_instance 파괴 후 s_everInitialized 리셋
        if (s_instance == null) s_everInitialized = false;
    }

    /// <summary>씬 배치 없이 게임 로드 시 1회 자동 생성 (DontDestroyOnLoad).</summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (s_instance != null) return;
        var go = new GameObject("SteamManager");
        go.AddComponent<SteamManager>();
    }

    private void Awake()
    {
        if (s_instance != null) { Destroy(gameObject); return; }
        s_instance = this;
        DontDestroyOnLoad(gameObject);

        if (s_everInitialized)
        {
            // 도메인 리로드 없이 재진입한 경우 — 중복 초기화 방지
            throw new Exception("[SteamManager] SteamAPI_Init 중복 호출 시도");
        }

        if (!Packsize.Test())
            Debug.LogError("[SteamManager] Packsize.Test 실패 — Steamworks.NET 빌드/플랫폼 불일치");
        if (!DllCheck.Test())
            Debug.LogError("[SteamManager] DllCheck.Test 실패 — steam_api dll 손상/누락");

        try
        {
            // 에디터·개발 빌드에서 steam_appid.txt 없이도 동작하도록 AppId 명시 재시작 체크
            if (SteamAPI.RestartAppIfNecessary(new AppId_t(AppId)))
            {
                Debug.LogWarning("[SteamManager] Steam을 통해 재시작 필요 → 종료");
                Application.Quit();
                return;
            }
        }
        catch (DllNotFoundException e)
        {
            Debug.LogError("[SteamManager] steam_api.dll 로드 실패 — Plugins 폴더 확인\n" + e);
            Application.Quit();
            return;
        }

        m_initialized = SteamAPI.Init();
        if (!m_initialized)
        {
            Debug.LogWarning("[SteamManager] SteamAPI.Init 실패 — Steam 클라이언트 미실행 또는 미소유. 도전과제 비활성으로 진행");
            return;
        }

        s_everInitialized = true;
        Debug.Log("[SteamManager] Steam 초기화 완료 (AppID " + AppId + ")");
    }

    private void OnEnable()
    {
        if (s_instance == null) s_instance = this;
        if (!m_initialized) return;

        if (m_warningHook == null)
        {
            m_warningHook = SteamAPIDebugTextHook;
            SteamClient.SetWarningMessageHook(m_warningHook);
        }
    }

    private void OnDestroy()
    {
        if (s_instance != this) return;
        s_instance = null;
        if (m_initialized) SteamAPI.Shutdown();
    }

    private void Update()
    {
        if (m_initialized) SteamAPI.RunCallbacks();
    }

    [AOT.MonoPInvokeCallback(typeof(SteamAPIWarningMessageHook_t))]
    private static void SteamAPIDebugTextHook(int severity, System.Text.StringBuilder text)
    {
        Debug.LogWarning("[Steam] " + text);
    }
#else
    // ── DR_STEAM 미정의: 스텁 (도전과제 호출이 안전하게 무시됨) ──
    public static bool Initialized => false;
#endif
}
