# SPEC-029 — GameOver 미발동 수정 + 간소화 + 타이틀 이동

**날짜**: 2026-05-16
**상태**: ✅ 구현 완료 (GATE A·B·C 전부 PASS)
**선행**: spec028 (도파민 젤리/스톤)
**개발 방식**: Harness Engineering (생성자-평가자 분리)

## 배경

젤리가 0이 되어도 GameOver가 발동하지 않는 버그. 마지막 레이스 전 파산 시
GameOver 되어야 함. GameOver 화면은 간단히 "GAME OVER"(폰트 42)만 보여주고
타이틀 화면으로 자동 이동. 리더보드 점수 미저장, 캐릭터 개별 기록은 유지.

## 근본 원인 (중첩 ChangeState 버그)

`ChangeState(Result)` → 내부 `CalcScore()` 동기 호출 → `CalcScore`가
`ChangeState(GameOver)` 중첩 호출 → 중첩 반환 후 바깥 `ChangeState(Result)`의
`OnStateChanged(Result)`가 GameOver를 덮음 → Result UI로 게임 계속.

## 요구사항

- **R29.1** 젤리=0 + 환전 불가(R19 구제 포함) → GameOver 발동 (마지막 라운드 전 포함)
- **R29.2** GameOver UI = 어두운 백드롭 + "GAME OVER" 텍스트(폰트 42) 단일. 모달/버튼/설명 제거
- **R29.3** GameOver 진입 2.5초 후 TitleScene(빌드 인덱스 0) 자동 이동
- **R29.4** 리더보드 미저장 (기존 Finish 전용 유지), 캐릭터 개별 기록은 저장 유지
- **R29.5** 정상 완주(젤리>0, 마지막 라운드) → Finish 회귀 없음
- **R29.6** GameOver 화면 = 별도 프리팹, 화면 중앙 모달 박스로 표시 (코드 동적생성 → 프리팹화)

## 변경 파일

| 파일 | 변경 |
|------|------|
| `Assets/Scripts/Manager/GameManager.cs` | `CalcScore()` 깨진 GameOver 블록 제거 / `NextRound()` 최상단 `ShouldGameOver()`→`ChangeState(GameOver)` 가드 추가 (IsLastRound보다 먼저) |
| `Assets/Scripts/Manager/UI/SceneBootstrapper.GameOver.cs` | 프리팹 Instantiate 우선 + 코드 폴백(`BuildGameOverUIFallback`). 화면 중앙 박스+폰트42. `LoadTitleScene()` Invoke(2.5s) 유지 |
| `Assets/Scripts/Data/GameSettings.cs` | `gameOverPanelPrefab` 필드 추가 |
| `Assets/Scripts/Editor/GameOverUIPrefabCreator.cs` | **신규** — 메뉴 `DopamineRace/Create(Patch) GameOver UI Prefabs`, ResultUIPrefabCreator 패턴 |
| `Assets/Prefabs/UI/GameOverPanel.prefab` | **신규** — `GameOverPanel`(백드롭 α0.9) → `Panel`(중앙 760×360) → `HeadlineText`(폰트42) |

저장 로직(ScoreManager) — **변경 없음** (이미 요구사항 충족).

## 프리팹 구조 (R29.6)

```
GameOverPanel        full-rect, Image α0.9 (게임 화면 가림)
└─ Panel             anchor(0.5,0.5) 760×360, Image (0.12,0.04,0.04,0.96) — 화면 중앙 박스
   └─ HeadlineText   "GAME OVER" 폰트 42, 중앙정렬, (1,0.35,0.35)
```

- 표준 패턴: Editor Creator → `Assets/Prefabs/UI/` → `GameSettings` 참조 → 런타임 Instantiate (BettingUI/ResultUI와 동일)
- 프리팹 미연결 시 `BuildGameOverUIFallback()`이 동일 레이아웃 코드 생성

## 핵심 설계

- GameOver 판정을 `CalcScore`(ChangeState 중첩) → `NextRound`(버튼 클릭, 비중첩)로 이관
- 흐름: 패배로 젤리0 → Result 화면 → "다음 라운드" 클릭 → NextRound 가드 → GameOver → 2.5s → TitleScene
- 리더보드: `ChangeState(Finish):198` 단일 호출처 유지 → GameOver 경로 미도달
- 캐릭터 기록: `CalcScore:323` `RecordRound()`→`SaveCharRecords()` 우회 없음

## QA 게이트 결과 (Harness — qa 회의적 독립 검증)

| Gate | 항목 | 결과 |
|------|------|------|
| A | 젤리0/스톤0→GameOver, R19 구제, 회귀, 컴파일 | ✅ PASS |
| B | 폰트42 단일텍스트, TitleScene 유효, 모달/버튼 미생성 | ✅ PASS |
| C | 캐릭터기록 저장유지, 리더보드 미저장, Finish 회귀, 에러0 | ✅ PASS |
| D | 프리팹 존재·구조, GameSettings 연결, 중앙배치, 폴백, 회귀 | ✅ PASS |

**교훈**: 코드 수정 후 `CompilationPipeline.RequestScriptCompilation()` 강제
재컴파일 필수 — 미실행 시 stale 어셈블리로 오판정 (Gate A에서 IL 디스어셈블로 적발).

## 비고

- `SceneBootstrapper.Betting.cs:685` 베팅화면 젤리<1 GameOver = 방어적 이중 가드 (정상)
- 폰트42는 `MkText`→`FontHelper.ScaledFontSize(42)`, uiFontScale=1 기준 (프로젝트 표준)

## SPEC-029.1 — 클릭 이동 변경 (2026-05-16 후속)

오너 요청: GameOver 자동 이동(2.5초) → **화면 클릭 시 이동**으로 변경.

- **R29.1.1** 2.5초 자동 타이틀 이동 제거 (`Invoke`/`CancelInvoke`/`GAMEOVER_TITLE_DELAY` 삭제)
- **R29.1.2** GameOverPanel 루트에 전체화면 `Button` → onClick = `LoadTitleScene` (코드베이스 표준 Button 패턴, ResultUIPrefabCreator 미러)
- **R29.1.3** 패널 하단 안내 텍스트 `HintText`(폰트20, Loc `str.gameover.hint`, 7개 언어) — 클릭 UX 안내
- **R29.1.4** 중복 클릭 방지: `LoadTitleScene` 진입 시 `Button.interactable=false`
- **R29.1.5** 베팅 화면 = 이미 프리팹(`BettingPanel.prefab`/`BetAmountModalPrefab.prefab`) — 작업 불필요 (조사 결론)

### 변경 파일 (SPEC-029.1)
| 파일 | 변경 |
|------|------|
| `GameOverUIPrefabCreator.cs` | 루트 `Button` + `MkHint` 추가, `PatchPrefabs` 안전패치 분기 |
| `SceneBootstrapper.GameOver.cs` | `_gameOverButton`/`_gameOverHintText` 필드, `WireGameOverButton()`, Invoke 제거 |
| `GameOverPanel.prefab` | PatchPrefabs로 루트 Button + Panel/HintText 추가 (재생성) |
| `StringTable.csv` | `str.gameover.hint` 7개 언어 |

### QA 게이트 (SPEC-029.1, Harness)
| Gate | 결과 |
|------|------|
| A 프리팹 구조(Button1·폰트42·HintText·Text2·α0.90) | ✅ PASS |
| B Invoke/상수 제거 + onClick 연결 정적검증 | ✅ PASS |
| C onClick→LoadTitleScene 양경로 연결 (Play 풀사이클은 오너 수동) | ✅ PASS(정적) |
| D GameSettings 연결유지·컴파일0 | ✅ PASS |
