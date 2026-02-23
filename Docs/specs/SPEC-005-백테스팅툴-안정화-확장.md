# SPEC-005: 백테스팅 툴 안정화 + 확장

## 날짜
2026-02-24

## 대상 파일
- `Assets/Scripts/Editor/RaceBacktestWindow.cs`

---

## 1. FormatException 수정 (근본 원인)

### 문제
백테스팅 실행 시 "시뮬레이션 중..." 상태에서 영구 멈춤.
실제 원인: `BuildAllTracksResult()`에서 `FormatException` 발생 → `isRunning = false` 미도달.

### 원인 포맷 패턴
| 문제 패턴 | 위치 | 설명 |
|-----------|------|------|
| `{1,+6:F2}` | 스탯 기여 display | Mono에서 `+` alignment 미지원 |
| `{N:+0.00;-0.00}` | 스탯 기여 md / 타입 보너스 | `AppendFormat` section format 문제 |

### 해결
- `{N,+M:F2}` → `{N,M:F2}` (alignment `+` 제거)
- `+0.00;-0.00` → `SF()` 헬퍼 함수 (`+1.23` / `-0.45` 문자열 반환)

---

## 2. try-catch-finally 안전 래핑

### 목적
어떤 예외가 발생해도 `isRunning = false` + `EditorUtility.ClearProgressBar()` 보장.

### 적용 대상
- `RunAllTracksSimulation()` → try-catch-finally로 `RunAllTracksSimulationInternal()` 호출
- `RunSingleTrackSimulation()` → 동일 패턴 적용

---

## 3. GC 최적화

### 문제
`UpdateSimCooldowns()`에서 매 시뮬레이션 스텝마다 `new List<int>()` 2개 생성.
- simTimeStep=0.05, 5바퀴 기준 ~444K 스텝 × 2 = ~900K 할당

### 해결
- 클래스 필드 `_expiredKeys`, `_tempKeys` (capacity=32) 재사용
- 매 스텝 `.Clear()` 후 재활용 → GC 압력 제거

---

## 4. 프로그래스 바 + 취소 버튼

- `EditorUtility.DisplayCancelableProgressBar` 사용
- 10레이스마다 진행률 갱신: "트랙 1/6 [일반] 레이스 50/100"
- 취소 시 `cancelRequested` 플래그로 시뮬레이션 중단
- `RunSimulationCore` 시그니처에 `trackIndex`, `totalTracks`, `trackName` 추가

---

## 5. 캐릭터 이름 한국어 표시

### 변경
- `BuildAllTracksResult` 시작 시 `Loc.SetLang("ko")` → `koNames` 딕셔너리 구축 → 완료 후 원래 언어 복원
- `KN()` 람다: UID → ko 이름 변환 (실패 시 UID 그대로)
- 모든 출력 위치의 `s.name` → `KN(s.name)`, `cn` → `KN(cn)` 교체 (7곳 + 경고 2곳)

---

## 6. 캐릭터 기록 초기화 메뉴

- `[MenuItem("DopamineRace/캐릭터 기록 초기화")]` 추가
- `CharacterRecordResetMenu` static 클래스 (RaceBacktestWindow.cs 하단)
- 확인 다이얼로그 → `PlayerPrefs.DeleteKey("DopamineRace_CharRecords")` → 완료 알림
- 런타임 ScoreManager 존재 시 동기화 호출
