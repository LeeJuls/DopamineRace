# V4 긴급부스트 버그수정 + 자동 레이스 테스트 도구 + GameSettings 정리

> **날짜**: 2026-03-20
> **작업자**: Claude (AI)
> **관련 스펙**: SPEC-012_V4레이스시스템_명세서_20260316.md

---

## 작업 개요

3가지 독립 작업으로 구성된다.
1. 미사용 GameSettings 필드 정리 (V2~V3 레거시)
2. 자동 반복 레이스 테스트 에디터 도구 신규 작성
3. 긴급부스트 구간 판정 버그 수정

---

## 1. 미사용 GameSettings 필드 정리

### 배경
- V4 전환 후 V2~V3 전용 필드와 미사용 아이콘 필드가 잔존
- Inspector 혼란 및 코드 가독성 저하

### 제거 필드 (11개)

| 분류 | 필드명 | 이유 |
|------|--------|------|
| 아이콘 (7개) | `criticalIcon`, `dodgeIcon`, `collisionIcon`, `slingshotIcon`, `sprintIcon`, `conserveIcon`, `exhaustedIcon` | 코드에서 미참조, VFX 시스템으로 대체됨 |
| UI | `bettingButtonHeight` | BettingPanel 프리팹 전환 후 미사용 |
| V2~V3 레거시 (3개) | `paceLeadMin`, `paceLeadMax`, `conservationFactor` | V4에서 구간제 속도 방식으로 대체, V3에서도 미사용 확인 |
| V2~V3 레거시 | `defaultPaceLeadFactor` | 위와 동일 |

### 변경 파일
- `Assets/Scripts/Data/GameSettings.cs`: 해당 필드 선언부 삭제

---

## 2. 자동 반복 레이스 테스트 도구 (AutoRaceRunnerWindow)

### 배경
- 밸런스 검증을 위해 수동으로 배팅→레이스→결과→다음라운드 반복이 비효율적
- N배속 × M회 자동 반복 + 로그 자동 저장 필요

### 신규 파일
- `Assets/Scripts/Editor/AutoRaceRunnerWindow.cs`

### 메뉴
- `DopamineRace > 자동 레이스 테스트`

### 주요 기능

| 기능 | 설명 |
|------|------|
| 반복 횟수 | 1~100회 설정 |
| 배속 | 1~20x (`Time.timeScale` 제어) |
| 자동 진행 | `GameManager.OnStateChanged` 구독 → Betting 자동배팅, Result 순위수집+다음라운드, Finish 다음게임 |
| 중지 버튼 | `isRunning = false` → 현재 레이스 완주 후 정지, 부분 로그 저장 |
| 로그 저장 | `Docs/logs/autorace_YYYYMMDD_HHmmss.md` 마크다운 형식 |
| 집계 통계 | 타입별 평균순위/1위횟수/Top3, 캐릭터별 평균순위/1위횟수 |

### Time.timeScale 검증

`Time.timeScale = 10`이면 `Time.deltaTime`이 10배 → 모든 시간 기반 로직이 균일 스케일:

| 게임 로직 | 시간 의존 방식 | 스케일 여부 |
|-----------|--------------|-----------|
| HP 드레인 | `progressDelta` (이동거리 비례) | ✅ 자동 |
| 긴급 부스트 쿨다운 | `-= Time.deltaTime` | ✅ 자동 |
| 럭/크리티컬 판정 | progress 기반 | ✅ 자동 |
| 충돌 쿨다운 | `-= Time.deltaTime` | ✅ 자동 |
| Lerp 가속 | `Lerp(cur, tgt, accel * dt)` | ✅ 자동 |

**결론**: 배속과 무관하게 레이스 결과(순위)는 동일. 시간만 빨라짐.

### 자동 진행 핵심 로직

```
OnStateChanged(state):
  Betting → 더미 배팅 자동 → GameManager.StartRace()
  Result  → 순위 수집 → GameManager.NextRound()
  Finish  → gameIndex++ → 남은 반복이면 StartNewGame(), 완료면 로그 저장
```

"다음 프레임" = `EditorApplication.update`에 1회성 콜백 등록

### 로그 파일 구조

```markdown
# 자동 레이스 테스트 결과
- 설정: V4 파라미터 요약
- 라운드별: 순위 테이블
- 집계: 타입별/캐릭터별 통계
```

---

## 3. 긴급부스트 구간 판정 버그 수정

### 증상
- 로그 분석 중 발견: 부스트 구간 이후 일반 구간에서 긴급부스트가 전혀 발동하지 않음
- 예) 선입(Chaser) 부스트 구간 46~65% 종료 후, 66~85% 일반 구간에서도 긴급부스트 미발동

### 원인

```csharp
// 버그 코드 (수정 전)
float burstStart = GetV4BurstStart(gs);
if (progress >= burstStart)
{
    if (v4EmergencyBurst) { /* 해제 */ }
    return;  // ← 부스트 구간 시작점 이후 모든 progress에서 무조건 return
}
```

`progress >= burstStart` 조건이 **부스트 구간 시작 이후의 모든 구간**(일반 구간 포함)을 차단.
부스트 구간이 46~65%인 선입의 경우, 66% 이후 일반 구간에서도 `progress >= 0.46`이 true → return.

### 수정

```csharp
// 수정 후: 실제 활성 구간만 체크
float progress = GetOverallProgress();

bool inRegularBurst = IsInBurstZone(gs, progress);  // 부스트 구간 내부인지
bool inSpurt        = progress >= gs.v4_finalSpurtStart;  // 스퍼트 구간인지

if (inRegularBurst || inSpurt)
{
    if (v4EmergencyBurst)
    {
        v4EmergencyBurst = false;
        v4EmergencyBurstCooldownTimer = 0f;
        // 로그: 정규부스트/스퍼트 연결
    }
    return;
}

// 부스트/스퍼트 구간 밖: 정상적으로 긴급부스트 판정 진행
```

### 수정 파일

| 파일 | 메서드 | 변경 |
|------|--------|------|
| `RacerController_V4.cs` | `UpdateV4EmergencyBurst()` | `progress >= burstStart` → `IsInBurstZone() \|\| inSpurt` |
| `RaceBacktestWindow.cs` | `SimUpdateV4EmergencyBurst()` | 동일 미러링 수정 |

### 효과
- 부스트 구간 → 일반 구간 전환 시 긴급부스트 정상 재개
- 정규 부스트/스퍼트 구간 진입 시 긴급부스트 해제 → 정규 부스트로 연결 (기존 동작 유지)

---

## 수정 파일 목록 요약

| 파일 | 변경 내용 |
|------|---------|
| `GameSettings.cs` | 미사용 필드 11개 제거 |
| `AutoRaceRunnerWindow.cs` | **신규** — 자동 반복 레이스 테스트 에디터 도구 |
| `RacerController_V4.cs` | 긴급부스트 구간 판정 버그 수정 |
| `RaceBacktestWindow.cs` | 긴급부스트 구간 판정 미러링 수정 |
| `CharacterDB.csv` | `char_appearance_rate` 컬럼 추가 (col 18, 전원 10) |
| `CharacterDB_V4.csv` | 동일 |
| `CharacterData.cs` | `charAppearanceRate` 필드 + CSV 파싱 |
| `CharacterDatabase.cs` | 타입밸런싱 `SelectRandom()` (2/2/2/2+1 wildcard) |
| `RaceDebugOverlay.cs` | GlobalSpeed 표시 + 긴급부스트 상세 로그 |

---

## 관련 커밋

| 커밋 | 내용 |
|------|------|
| `fcff39d` | 긴급부스트 쿨다운 + 타입별 drain 배율 분리 |
| `78fe46d` | HP탭 최종 랩 100% 스냅샷 순위 재정렬 |
| `4c7512c` | 긴급부스트 수정 + 자동테스트 도구 + GameSettings 정리 |
| `b8c49b6` | main 머지 완료 |
