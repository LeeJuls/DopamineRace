# Enter Play Mode — Disable Domain Reload 히스토리

> 2026-06-30 · 에디터 Play 진입 속도 4.1초 → 목표 0.8~1.5초

## 작업 요약

Unity "Enter Play Mode Options: Disable Domain Reload"를 활성화하여 에디터 플레이 사이클을 단축.
Domain Reload OFF 적용 전 static 필드 오염 문제를 안전화하는 4단계 작업.

## 단계별 구현

### Stage 1 — DontDestroyOnLoad 싱글턴 OnDestroy 추가

| 파일 | 추가 내용 |
|------|-----------|
| `BGMManager.cs` | `OnDestroy`: `StopBGM()` + `Instance=null` |
| `SFXManager.cs` | `OnDestroy`: `CancelInvoke()` + `Instance=null` / `RegisterGlobalClickListener` 내 `CancelInvoke(ScanAndAttachButtons)` 선행 |
| `SceneTransitionManager.cs` | `OnDestroy`: `Instance=null` |
| `LeaderboardService.cs` | `OnDestroy`: `StopAllCoroutines()` + `_fetchInFlight=false` + `Instance=null` |
| `CharacterDatabase.cs` | `OnDestroy`: `Instance=null` |
| `TrackDatabase.cs` | `OnDestroy`: `Instance=null` |

### Stage 2A — SteamManager s_everInitialized 리셋

`SteamManager.cs`에 `[RuntimeInitializeOnLoadMethod(SubsystemRegistration)]` 추가:
```csharp
private static void ResetForDomainReloadOff()
{
    if (s_instance == null) s_everInitialized = false;
}
```
- 기존 `Bootstrap()`의 `BeforeSceneLoad`보다 먼저 실행되어 2회차 Play 예외 방지
- `s_instance == null` 조건: Steam이 정상 초기화된 상태에서는 리셋하지 않음

### Stage 2B — static 필드 초기화

`SceneBootstrapper.cs`:
```csharp
[RuntimeInitializeOnLoadMethod(SubsystemRegistration)]
private static void ResetStaticState()
{
    trackPanelOpen = true;           // 라운드 간 유지값 → Play 시작마다 초기화
#if UNITY_EDITOR
    if (_circleSprite != null) {
        DestroyImmediate(_circleSprite.texture);
        DestroyImmediate(_circleSprite);
    }
#endif
    _circleSprite = null;            // Racing HUD 원형 마커 재생성 보장
}
```

`HiddenStatWeights.cs`:
```csharp
[RuntimeInitializeOnLoadMethod(SubsystemRegistration)]
private static void ResetLoaded() => _loaded = false;
// 에디터에서 CSV 수정 후 재Play 시 즉시 반영
```

### Stage 3 — Enter Play Mode Options 활성화

`Assets/Scripts/Editor/EnterPlayModeSetup.cs` 생성 (`[MenuItem]`).
`unity_execute_code`로 직접 적용:
- `EditorSettings.enterPlayModeOptionsEnabled = true`
- `EditorSettings.enterPlayModeOptions = DisableDomainReload`
- `DisableSceneReload`는 미적용 (SceneBootstrapper Start() 재진입 감사 선행 필요)

## 검증 결과

| TC | 내용 | 결과 |
|----|------|------|
| TC-1-01 | 컴파일 에러 | ✅ 0건 |
| TC-2A-01 | 컴파일 에러 | ✅ 0건 |
| TC-2B-01 | 컴파일 에러 | ✅ 0건 |
| TC-3-01 | EditorSettings 설정값 | ✅ `enabled=True DR_off=True SR_off=False` |
| TC-4 | Play Mode 에러 | ✅ 에러 0건 (배팅→레이싱 정상 로그 확인) |

## 핵심 결정

| 항목 | 결정 | 이유 |
|------|------|------|
| 리셋 타임: `SubsystemRegistration` | `BeforeSceneLoad`가 아닌 앞 단계 | `SteamManager.Bootstrap()`가 `BeforeSceneLoad` 사용 — 순서 충돌 방지 |
| `DisableSceneReload` 미적용 | 이번 범위 제외 | SceneBootstrapper `Start()` 재진입 안전성 별도 감사 필요 |
| `_circleSprite` `DestroyImmediate` | `#if UNITY_EDITOR` 블록 | 빌드에서 `DestroyImmediate` 금지; 에디터 메모리 누수 방지 |
| `WalletManager·ScoreManager` OnDestroy 미추가 | 선택사항 제외 | DontDestroyOnLoad 없음 → 씬 파괴 시 자동 소멸 |

## 에이전트 신규 생성

`.claude/agents/performance.md` — Unity 성능 최적화 전문 에이전트 (Enter Play Mode, static 생명주기, 메모리 누수)

## 커밋

`5993864 [C] perf: Enter Play Mode — Disable Domain Reload 안전화 (Stage 1-3)`
