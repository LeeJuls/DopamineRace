# SPEC-039 — FinishPanel 텍스트 프리팹화 + 에디터 메뉴 정리

**날짜**: 2026-06-13 | **상태**: 구현 완료

## 배경

- FinishPanel의 텍스트 5종이 단일 `RoundDetailText`에 `<size=XX>` RichText 태그로 합쳐져 있어
  프리팹 Inspector에서 크기·위치를 수정해도 런타임 코드가 덮어써 반영되지 않았음.
- NameEntryModal의 `RankResultText`도 `ShowResult()`에서 RectTransform을 코드로 고정해
  프리팹 편집이 불가했음.
- 에디터 메뉴 `DopamineRace/` 아래 Create 계열 항목이 10개 이상 1뎁스에 나열되어 불편.

## 변경 내용

### 1. FinishPanel — 단일 Text → 5개 Text 분리

#### 구조 변경

```
RoundScrollView
  └── Viewport (RectMask2D)
        └── Content (VerticalLayoutGroup + ContentSizeFitter)   ← 신규
              ├── StoneHeaderText   fontSize=42, #4DDDDD 청록
              ├── RoundSummaryText  fontSize=24, white, richText
              ├── DetailHeaderText  fontSize=18, #888888 회색
              ├── RoundDetailText   fontSize=16, white, richText
              └── FinalJellyText    fontSize=20, #AACFFF 하늘색
```

- `Content`: anchorMin=(0.5,1), anchorMax=(0.5,1), sizeDelta=(700,0) — 고정 폭으로 프리팹 에디터에서도 가로 표시
- 각 Text 자식: anchorMin=(0,1), anchorMax=(0,1), sizeDelta=(692,0) 초기값 — VLG가 런타임에 너비 override
- `LayoutRebuilder.ForceRebuildLayoutImmediate(contentRt)` 를 팩토리 마지막에 호출해 프리팹 생성 즉시 레이아웃 계산

#### `<size>` 태그 제거

`ShowFinish()`에서 `<size=XX>` 태그를 전량 제거하고 각 Text 컴포넌트의 prefab fontSize를 그대로 사용.
`<color>` 태그는 유지 (적중=초록, 빗나감=빨강, 스톤=금색).

#### 폴백 호환

`CacheFinishUIReferences()`에서 `Content`가 없는 구형 프리팹은 `Viewport/RoundDetailText` 경로로 폴백.
`GameSettings.finishPanelPrefab == null`이면 `BuildFinishUILegacy()`에서 5개 MkText로 코드 빌드.

### 2. NameEntryModal — RankResultText 프리팹 편집 가능화

`ShowResult()`에서 `_rankResultText`의 RectTransform 위치·크기·fontSize를 런타임에 고정하던 코드 제거.
텍스트 내용(`text`)과 색상(`color`)만 런타임 설정; 나머지는 프리팹 Inspector에서 자유 편집.

### 3. 에디터 메뉴 — 프리팹 생성 서브메뉴 통합

`DopamineRace/Create *` 형태로 1뎁스에 나열되던 8개 항목을 `DopamineRace/프리팹 생성/` 서브메뉴로 이동.

| 변경 전 | 변경 후 |
|---------|---------|
| `DopamineRace/Create Betting UI Prefabs` | `DopamineRace/프리팹 생성/Betting UI Prefabs` |
| `DopamineRace/Create Finish UI Prefab` | `DopamineRace/프리팹 생성/Finish UI Prefab` |
| `DopamineRace/Create GameOver UI Prefabs` | `DopamineRace/프리팹 생성/GameOver UI Prefabs` |
| `DopamineRace/Create Leaderboard UI Prefab` | `DopamineRace/프리팹 생성/Leaderboard UI Prefab` |
| `DopamineRace/Create NameEntry Modal Prefab` | `DopamineRace/프리팹 생성/NameEntry Modal Prefab` |
| `DopamineRace/Create Option Panel Prefab` | `DopamineRace/프리팹 생성/Option Panel Prefab` |
| `DopamineRace/Create Result UI Prefabs` | `DopamineRace/프리팹 생성/Result UI Prefabs` |
| `DopamineRace/고양이 프리팹 생성` | `DopamineRace/프리팹 생성/고양이 프리팹` |

`Patch *`, `트랙 프리팹 생성`, 기타 유틸리티 항목은 1뎁스 유지.

## 변경 파일

| 파일 | 내용 |
|------|------|
| `Assets/Scripts/Manager/UI/SceneBootstrapper.cs` | `finishStoneHeaderText` 등 4개 필드 추가 |
| `Assets/Scripts/Manager/UI/SceneBootstrapper.Finish.cs` | `CacheFinishUIReferences` Content 경로, `ShowFinish` 5개 분리, `BuildFinishUILegacy` 5 MkText |
| `Assets/Scripts/Editor/FinishLeaderboardUIPrefabCreator.cs` | Content(VLG+CSF)+5Text 구조, `MkTextInContent` 헬퍼, `ForceRebuildLayoutImmediate` |
| `Assets/Scripts/UI/NameEntryModal.cs` | `ShowResult()` RectTransform override 제거 |
| `Assets/Scripts/Editor/BettingUIPrefabCreator.cs` | MenuItem 경로 변경 |
| `Assets/Scripts/Editor/CatPrefabFactory.cs` | MenuItem 경로 변경 |
| `Assets/Scripts/Editor/GameOverUIPrefabCreator.cs` | MenuItem 경로 변경 |
| `Assets/Scripts/Editor/NameEntryModalPrefabFactory.cs` | MenuItem 경로 변경 |
| `Assets/Scripts/Editor/OptionPanelPrefabCreator.cs` | MenuItem 경로 변경 |
| `Assets/Scripts/Editor/ResultUIPrefabCreator.cs` | MenuItem 경로 변경 |
| `Assets/Prefabs/UI/FinishPanel.prefab` | 5-Text Content 구조로 재생성 |
| `Assets/Prefabs/UI/NameEntryModal.prefab` | ContinueText 포함, GuideText/ContinueText 분리 |
