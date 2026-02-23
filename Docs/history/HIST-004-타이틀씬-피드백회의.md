# HIST-004 — TitleScene 피드백 회의록

- **날짜**: 2026-02-23
- **참석**: 기획(1번), 개발(2번), QA(3번)
- **원본 기획서**: `Docs/타이틀씬 추가.md`
- **확정 명세서**: `Docs/specs/SPEC-004-타이틀씬.md`

---

## 1. 회의 목적

타이틀씬 기획서를 기획/개발/QA 3명 관점에서 리뷰하고,
구현 전 요구사항 및 스펙 변경 사항을 확정.

---

## 2. 기획자 피드백 및 결정

### 2-1. 언어 변경 UI 누락
- **문제:** 원래 요구사항에 "언어변경"이 있었으나 명세서에 빠져 있음
- **결정:** 타이틀 화면 **우하단에 언어 선택 버튼 추가** (ko/en/jp)
- **추가 논의:** Loc.cs 동적 언어 목록 필요 여부
  - → **현재 하드코딩 유지.** 언어 추가 시 수동 확장 (공수 대비 이점 적음)

### 2-2. 씬 이름 GameScene vs SampleScene
- **문제:** 명세서에 "GameScene"이라 했지만 실제 씬 이름은 SampleScene
- **결정:** **SampleScene 이름 유지** (rename 시 참조 깨짐 리스크)

### 2-3. DontDestroyOnLoad 대상
- **결정:** TitleScene에서 CharacterDatabase, TrackDatabase, BGMManager를 먼저 생성
  - 기존 SceneBootstrapper의 `if (Instance == null)` 가드 덕분에 중복 생성 없음

---

## 3. 개발자 피드백 및 결정

### 3-1. 캐릭터 run 스프라이트 이슈 (Critical)
- **문제:** CharacterData에 "run 스프라이트" API가 없음. 실제로는 프리팹 내 Animator "Run" 트리거
- **질문:** "기존 캐릭터 달리는 거 그대로 쓰면 될 것 같은데 이슈?"
- **답변:** 쓸 수 있지만 **RacerController 비활성화 필수**
  - RacerController.Update()가 waypoints/RaceManager 등 참조 → TitleScene에서 NullRef
  - 해결: Instantiate 후 `RacerController.enabled = false` → Animator "Run"만 사용
- **결정:** 기존 프리팹 Instantiate + RacerController 비활성화 + TitleCharacterRunner로 이동 관리

### 3-2. BGMManager 재작성
- **결정:** 확장이 아닌 **재작성**
  - 이유: 향후 결과 화면 BGM, 상황별 BGM 전환 필수
  - 신규 API: PlayBGM(path), FadeIn(), FadeOut(), CrossFade(), StopBGM()

### 3-3. SceneBootstrapper 대응
- **문제:** SceneBootstrapper가 모든 매니저를 생성하는 God-class
- **결정:** 기존 코드의 `if (Instance == null)` 가드를 활용, 변경 최소화
  - BGMManager.Start() 자동재생만 제거 → 명시적 호출로 변경

### 3-4. TrackTransition 관계
- **분석 결과:**
  - TrackTransition: 풀스크린 단일 Image 페이드 (인게임 트랙 전환)
  - SceneTransitionManager: 160개 격자 블록 디졸브 (씬 간 전환)
  - 구현이 완전히 다르고, 통합 시 기존 트랙 전환 깨질 리스크
- **결정:** **공존** (각각 독립 유지)
  - sortingOrder: TrackTransition=9999, SceneTransitionManager=10000

### 3-5. 씬 로딩 방식
- **결정:** `LoadSceneAsync` + `allowSceneActivation` 패턴 사용

---

## 4. QA 피드백 및 결정

### 4-1. 타이틀 복귀 테스트
- **결정:** **불필요** (단방향만, GameScene → Title 복귀 경로 없음)

### 4-2. 싱글톤 중복 검증 추가
- **결정:** Phase 1 테스트에 추가
  - "BGMManager, CharacterDatabase, TrackDatabase 인스턴스가 씬 전환 후 각 1개인지 확인"

### 4-3. 캐릭터 소환/파괴 vs 재활용
- **QA 제안:** 소환해서 쓰고 끝에 도착하면 지우고 또 소환
- **개발 판단:** 재활용 권장 (position 리셋만, GC 부담 없음)
- **결정:** **재활용 방식** (12마리 전부 등장 후 스폰 중지, position 리셋으로 순환)

### 4-4. 해상도 대응
- **결정:** 기획 리더가 계획 수립
  - 배경: Cover 방식 (카메라 fit, 초과분 잘림 허용)
  - 블록 격자: CanvasScaler 1920×1080 + ceil 처리로 빈틈 방지
  - 캐릭터: ViewportToWorldPoint 기준 하단 고정 비율

---

## 5. 최종 확정 변경 요약

| # | 항목 | 원본 → 확정 |
|---|------|------------|
| 1 | 언어 변경 | 없음 → 우하단 버튼 추가 |
| 2 | 씬 이름 | GameScene → SampleScene 유지 |
| 3 | 캐릭터 달리기 | run 스프라이트 → 프리팹 Instantiate + RacerController 비활성화 |
| 4 | BGMManager | 확장 → 재작성 |
| 5 | TrackTransition | 미정 → 공존 |
| 6 | 씬 로딩 | LoadScene → LoadSceneAsync |
| 7 | Loc.cs | 동적 → 하드코딩 유지 |
| 8 | 타이틀 복귀 | 미정 → 불필요 |

---

*다음 단계: SPEC-004 기준으로 Phase 1부터 구현 시작*
