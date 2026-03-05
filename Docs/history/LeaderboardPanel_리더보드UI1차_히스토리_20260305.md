# 인수인계서 — LeaderboardPanel UI 1차 완료
> 작업일: 2026-03-05 (세션 3)
> 커밋 범위: `6cc5c7f` ~ `b9ee49c`
> 브랜치: `main`

---

## 이번 세션 작업 요약

### 0. 리더보드 버그 수정 ✅

#### 0-1. 영어 서수 표기 수정 (`6cc5c7f`)
- `str.leaderboard.row` 영어에서 `{0}th` 고정 suffix 제거
- `SceneBootstrapper.Leaderboard.cs`에 `GetEnOrdinal()` 헬퍼 추가
  - 11/12/13 예외(11th/12th/13th) 포함
  - 1st / 2nd / 3rd / 4th+ 정상 처리

#### 0-2. Summary 한글 고정 버그 수정 + pts→pt (`02b2aca`)
- `ScoreManager.GetRoundSummary()`: `"+" + score + "점"` 하드코딩 → `Loc.Get("str.finish.score_plus")` 교체
- StringTable 영어 3개 키: `pts` → `pt`
  - `str.finish.score_plus`, `str.finish.score_zero`, `str.leaderboard.row`
- leaderboard.json 초기화 (MCP)

---

### 1. 리더보드 표시 형식 개선 (`20a0422`)

#### 배경
빽빽한 1줄 형식(등수/포인트/달성일/라운드수/요약) → 가독성 저하

#### 변경 내용
- **헤더**: `9R` 라운드 수 컬럼 제거 → `등수 포인트 달성일`만 표시
- **요약 저장 포맷**: `R1:Win+27 R2:Exacta+500` (공백 구분, BetType 영어 enum 고정 — Locale-neutral)
- **ScoreManager.GetRoundSummary()**: `"+" + r.score + "점"` → `"+" + r.score` (점수만, 단위 없음)
- **BuildSummaryBlock()**: 5개씩 `\n` 분리 + `  ` indent (Legacy ContentText용)

---

### 2. LeaderboardPanel 구조 개편 — EntryTemplate 방식 (`63c4854`)

#### 배경
단일 `ContentText` 블롭 방식 → Inspector에서 개별 항목 조정 불가,
Unity Editor가 prefab 저장 시 SizeDelta를 자동 조정해 HeaderText 레이아웃 붕괴

#### 새 프리팹 구조
```
LeaderboardPanel (880×800, Image black BG a=0.9)
├─ TitleText      (36pt, 금색, anchor y=0.94)
├─ HeaderText     (26pt, 연회색, anchor y=0.87)
├─ ContentScrollView (ScrollRect vertical, 800×590, anchor y=0.49)
│  └─ Viewport (RectMask2D)
│     └─ EntryContainer (VerticalLayoutGroup spacing=10 + CSF)
│        └─ EntryTemplate [active=false — 클론 소스]
│           ├─ InfoText    (36pt, white/gold)
│           └─ SummaryText (28pt, gray 0.75)
└─ CloseBtn (200×55, BTN_RED, anchor y=0.04)
```

#### 주요 변경 파일

| 파일 | 변경 내용 |
|------|----------|
| `Assets/Scripts/Data/GameSettings.cs` | `leaderboardPanelPrefab` 필드 추가 |
| `Assets/Scripts/Manager/UI/SceneBootstrapper.cs` | `leaderboardEntryContainer` / `leaderboardEntryTemplate` 필드 추가 |
| `Assets/Scripts/Manager/UI/SceneBootstrapper.Leaderboard.cs` | `CacheLeaderboardUIReferences()` 신형, `ShowLeaderboard()` Instantiate 방식, `BuildSummaryText()` 추가, Legacy 폴백 유지 |
| `Assets/Scripts/Editor/FinishLeaderboardUIPrefabCreator.cs` | `CreateLeaderboardPanelPrefab()` EntryTemplate 구조로 재작성 |
| `Assets/Prefabs/UI/LeaderboardPanel.prefab` | 신형 구조로 재생성 |

#### 런타임 동작
- `ShowLeaderboard()`: 기존 인스턴스 제거 → `Instantiate(leaderboardEntryTemplate)` × entries.Count
- top3 항목: `InfoText.color = gold`
- `BuildSummaryText()`: 5개씩 `\n` 분리 (indent 없음, 별도 Text 컴포넌트이므로)

---

### 3. 버그 수정 연속 (`abbbbbe`, `e12aa5a`, `e0ffe2f`)

#### 3-1. 헤더 Summary 컬럼 텍스트 제거 (`abbbbbe`)
- `str.leaderboard.header` 7개 언어에서 `요약/Summary/概要...` 제거
- 요약 데이터 표시는 유지, 헤더 텍스트만 제거

#### 3-2. SummaryText Wrap→Overflow 수정 (`e12aa5a`)
- **원인**: `ContentSizeFitter + VerticalLayoutGroup` 조합에서 SummaryText 너비가 0으로 계산
  → 글자 하나씩 줄바꿈 → 패널 밖으로 overflow
- **수정**: `HorizontalWrapMode.Wrap → Overflow` (BuildSummaryText()가 이미 `\n`으로 줄 분리하므로 Unity wrap 불필요)

#### 3-3. LeaderboardPanel 루트 sizeDelta 버그 수정 (`e0ffe2f`)
- **원인**: 자식 GameObject 추가 후 Unity가 rootRt.sizeDelta를 `(1200, 5)`로 자동 변경
  → 패널이 가로로 넓고 세로로 1픽셀짜리 "검은 줄" 하나만 표시
- **수정**: `CreateLeaderboardPanelPrefab()` 리턴 직전에 rootRt 강제 재설정
  ```csharp
  rootRt.sizeDelta = new Vector2(880f, 800f);
  ```

---

### 4. 리더보드 폰트 크기 확대 (`b9ee49c`)

| 항목 | 이전 | 이후 |
|------|------|------|
| InfoText.fontSize | 28pt | **36pt** |
| SummaryText.fontSize | 22pt | **28pt** |

---

## 현재 상태 (1차 완료)

### 완료 ✅
- 리더보드 Top 100 표시
- 항목별 InfoText(등수/점수/날짜) + SummaryText(라운드 요약) 분리
- top3 금색, 이하 흰색
- 요약 5개씩 줄 분리
- 헤더 Summary 컬럼 텍스트 제거
- 영어 서수 표기 (1st/2nd/3rd)
- 다국어 Loc 처리
- ScrollRect 수직 스크롤
- Close 버튼

### 추후 고려 사항
- 리더보드 패널 전체적인 레이아웃/색상 디자인 개선 (2차)
- 날짜 형식 개선 (현재 `26-03-05` 압축)
- 리더보드 초기화 기능 (현재 MCP로만 가능)

---

## 알려진 패턴

### rootRt.sizeDelta 강제 재설정
`PrefabUtility.SaveAsPrefabAsset` 호출 전, 자식 추가 후에 루트 RT를 다시 명시적으로 설정해야 함.
`FinishLeaderboardUIPrefabCreator.cs`의 `CreateLeaderboardPanelPrefab()` 마지막 줄 참조.

### ContentSizeFitter + HorizontalWrapMode
`ContentSizeFitter`를 가진 요소에서 `HorizontalWrapMode.Wrap` 사용 시 너비=0 계산 버그 발생 가능.
텍스트에 명시적 `\n` 삽입 후 `Overflow`로 대체하는 것이 안전.
