# his044 — FinishPanel 텍스트 프리팹화 + 에디터 메뉴 정리

**날짜**: 2026-06-13
**관련 SPEC**: SPEC-039
**소요**: 1세션

## 작업 배경

- FinishPanel 텍스트가 단일 `RoundDetailText`에 `<size>` RichText 태그로 합쳐져 있어
  프리팹 Inspector 편집이 런타임에 덮어쓰여 적용되지 않는 문제 해결 요청.
- NameEntryModal의 `RankResultText` 위치·크기 역시 코드로 고정되어 프리팹 수정 불가였음.
- DopamineRace 에디터 메뉴에 Create 계열 항목이 1뎁스에 과도하게 나열되어 정리 요청.

## 작업 흐름

### Phase 1: FinishPanel 5-Text 분리

1. **설계 결정**: LeaderboardPanel의 `EntryContainer(VLG+CSF)` 패턴과 동일하게
   `Viewport → Content(VLG+CSF) → 5 Text` 구조 채택.
2. **SceneBootstrapper.cs**: `finishStoneHeaderText`, `finishRoundSummaryText`,
   `finishDetailHeaderText`, `finishFinalJellyText` 필드 4개 추가.
3. **SceneBootstrapper.Finish.cs**:
   - `CacheFinishUIReferences()`: `scrollT → Viewport → Content` 경로로 5개 Text 캐싱,
     구형 프리팹 폴백(단일 RoundDetailText) 유지.
   - `ShowFinish()`: `<size>` 태그 제거, 5개 null-checked 개별 assignment.
   - `BuildFinishUILegacy()`: 단일 MkText → 5개 고정 anchor MkText로 분리.
4. **FinishLeaderboardUIPrefabCreator.cs**:
   - `MkTextInContent()` 헬퍼 추가 (고정 앵커, VLG 호환).
   - Content 노드: anchorMin=(0.5,1), anchorMax=(0.5,1), sizeDelta=(700,0).
   - 각 Text 초기 sizeDelta.x=692 (프리팹 에디터 가로 표시용, VLG가 런타임 override).
   - `LayoutRebuilder.ForceRebuildLayoutImmediate(contentRt)` 추가.
5. `DopamineRace → 프리팹 생성 → Finish UI Prefab` 으로 프리팹 재생성.

### Phase 2: NameEntryModal RankResultText 프리팹화

- `ShowResult()`에서 RectTransform 위치·크기·fontSize 코드 override 제거.
- text/color 할당만 유지 → 프리팹 Inspector에서 자유 편집 가능.
- ContinueText "터치하여 계속" 분리 확인 및 프리팹 재생성으로 정상 표시 확인.

### Phase 3: 에디터 메뉴 프리팹 생성 서브메뉴 통합

8개 Editor 파일의 MenuItem 경로를 `DopamineRace/Create *` → `DopamineRace/프리팹 생성/*` 로 일괄 변경.
`Patch *` 항목과 기타 유틸은 1뎁스 유지.

## 트러블슈팅

| 증상 | 원인 | 해결 |
|------|------|------|
| 프리팹 에디터에서 Text가 세로로 표시 | Canvas 없이 VLG 레이아웃 미계산 → sizeDelta.x=0 | Content 고정 폭(700px) + 각 Text 초기 sizeDelta.x=692 + ForceRebuildLayoutImmediate |
| "터치하여 계속" 텍스트 미표시 | 코드 변경 후 프리팹 미재생성 | `DopamineRace → 프리팹 생성 → NameEntry Modal Prefab` 재실행 |

## 결과

- FinishPanel 5개 Text 모두 Inspector에서 fontSize·RectTransform 독립 편집 가능.
- NameEntryModal RankResultText 위치·크기 프리팹에서 편집 가능.
- 에디터 메뉴: `DopamineRace → 프리팹 생성 →` 서브메뉴로 정리.
