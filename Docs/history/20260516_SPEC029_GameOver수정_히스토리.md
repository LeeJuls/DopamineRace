# 20260516 SPEC-029 — GameOver 수정 세션 히스토리

**날짜**: 2026-05-16
**관련 SPEC**: spec029
**개발 방식**: Harness Engineering (Anthropic harness-design 기법 — 생성자/평가자 분리)

## 작업 흐름

### 사전: WalletManager 누락 버그 (선행 수정)
- `SceneBootstrapper.cs` 매니저 생성 목록에 `WalletManager` 누락 → Instance null
  → 젤리 미초기화 → BetAmountModal 미표시
- ScoreManager 생성 블록 직후 `WalletManager` 생성 1줄 추가
- QA TC: Jelly=100 초기화 / ResetForNewGame / ShouldGameOver 분기 전부 PASS

### Phase A — GameOver 트리거 (GameManager.cs)
- 근본 원인: 중첩 ChangeState — `CalcScore`(ChangeState(Result) 내부)에서
  `ChangeState(GameOver)` 호출 → 바깥 `OnStateChanged(Result)`가 덮음
- 수정: CalcScore 깨진 블록 제거 + `NextRound()` 최상단에 ShouldGameOver 가드
- **Gate A PASS** — qa가 IL 디스어셈블로 stale 어셈블리 적발 → 강제 재컴파일 후 재검증

### Phase B — GameOver UI 간소화 (SceneBootstrapper.GameOver.cs)
- 모달/설명/도달라운드/랭킹안내/다시도전버튼 전부 제거
- 어두운 백드롭(α0.9) + "GAME OVER" 단일 텍스트(폰트 42)
- `ShowGameOverPanel()` → `Invoke(LoadTitleScene, 2.5s)` → `SceneManager.LoadScene("TitleScene")`
- 미사용 필드 5개 + `OnGameOverRestartClicked()` 제거 (코드베이스 참조 0건 확인)
- **Gate B PASS**

### Phase C — 회귀 + 저장 검증 (코드 변경 없음)
- GC-1 캐릭터 기록 저장 유지 / GC-2 리더보드 미저장 / GC-3 Finish 회귀 / GC-4 에러0
- **Gate C PASS**

## Harness Engineering 적용 효과

| 요소 | 적용 |
|------|------|
| 생성자-평가자 분리 | 구현(메인) vs qa 에이전트(회의적 독립 검증) |
| 단계 분해 + 게이트 | Phase A/B/C, 각 게이트 전부 PASS 후 진입 |
| 결정적 검증 | MCP C# 실행으로 PASS/FAIL 측정 (주관 판단 배제) |
| 구조화 산출물 | plan 파일에 게이트 결과 누적 기록 |

- **핵심 성과**: qa 평가자가 stale 어셈블리 오판정 리스크를 IL 검사로 적발.
  이후 전 Phase에 "강제 재컴파일 → 도메인 리로드 확인 → 검증" 표준화.

## 변경 파일

- `Assets/Scripts/Manager/GameManager.cs`
- `Assets/Scripts/Manager/UI/SceneBootstrapper.GameOver.cs`
- `Assets/Scripts/Manager/UI/SceneBootstrapper.cs` (선행: WalletManager 생성)

## 미결 / 후속

- 실제 Play 풀사이클 수동 확인 (베팅 반복 패배 → GameOver → 타이틀) — 오너 검증 권장
- 커밋 `[C] SPEC-029 GameOver 미발동 수정 + 간소화 + 타이틀 이동` (push는 오너 확인 후)
