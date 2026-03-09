# 인수인계서 — ResultPanel Phase 2 + 디버그 단축키 + 치트 가이드 창
> 작업일: 2026-03-05
> 최신 커밋: `5553482`
> 브랜치: `main`
> Push: 완료 (6개 커밋 `1500d61` → `5553482`)

---

## 이번 세션 작업 요약

### 1. ResultPanel Phase 2 — 전체 순위 + 화살표 표시 ✅ (커밋 `94e2da7`)

#### 변경 전 / 후 구조

| | 변경 전 | 변경 후 |
|---|---|---|
| 섹션 구성 | Top3 순위 / 내결과 / 점수 (3구역) | **전체 순위 / 점수 (2구역)** |
| 순위 표시 | 1·2·3위만 | **전체 최대 12위 동적 표시** |
| 내 픽 표시 | BetResultSection에 별도 패널 | **해당 순위 행에 `← 1st` 화살표** |
| BetResultSection | 있음 | **완전 제거** |

#### 주요 변경 사항

**ResultPanel.prefab 구조 (MCP Reflection으로 재생성)**
```
ResultPanel
├─ TitleText (WIN/LOSE)
├─ RankSection
│   ├─ SectionLabel (str.result.rank_header)
│   ├─ Rank1Row ~ Rank12Row (각 30px 높이)
│   │   ├─ RankBadge / BadgeText (동적: 1위/2위/.../12위)
│   │   ├─ CharIcon (32×32)
│   │   ├─ CharName (260×28)
│   │   └─ PickArrow (120×28, 기본 hidden) ← NEW
│   ...
├─ ScoreSection
│   ├─ ScoreFormulaText
│   └─ TotalScoreText
└─ NextRoundBtn / BtnText
```

**배지 색상**
- 1위: 금 `(1f, 0.84f, 0f)`
- 2위: 은 `(0.75f, 0.75f, 0.75f)`
- 3위: 동 `(0.80f, 0.50f, 0.20f)`
- 4위~: 회색 `(0.45f, 0.45f, 0.45f)` ← NEW

**화살표(PickArrow) 로직**
```csharp
for (int si = 0; si < bet.selections.Count; si++)
{
    int selIdx = bet.selections[si];
    int actualRank = rankings.FindIndex(r => r.racerIndex == selIdx);
    if (actualRank >= 0)
    {
        arrowText.text  = "← " + bet.GetSelectionLabel(si);
        arrowText.color = isHit ? COLOR_PICK_HIT : COLOR_PICK_MISS;
        arrowText.gameObject.SetActive(true);
    }
}
```

**StringTable 추가**
```
str.result.rank_header | 순위 | Rankings | 順位 | 排名 | Rang | Posición | Classificação
```

**수정 파일**
- `Assets/Scripts/Manager/UI/SceneBootstrapper.cs` — 변수 재정의 (12배열, BetResult 제거)
- `Assets/Scripts/Manager/UI/SceneBootstrapper.Result.cs` — ShowResult() 전면 재작성
- `Assets/Scripts/Editor/ResultUIPrefabCreator.cs` — 12 RankRow + PickArrow, BetResultSection 제거
- `Assets/Resources/Data/StringTable.csv` — str.result.rank_header 추가
- `Assets/Prefabs/UI/ResultPanel.prefab` — MCP Reflection 재생성 (20/20 검증 통과)

---

### 2. 결과창 디버그 단축키 F8/F9/F10 ✅ (커밋 `294569d`)

#### 도입 배경
결과 화면 수정·확인 시 매번 1판 플레이 → 결과 도달 과정이 비효율적. Play 모드 진입 즉시 결과 화면으로 점프하는 단축키 추가.

#### 동작

| 키 | 배팅 타입 | 동작 |
|----|---------|------|
| F8 | Win | 1위 캐릭터 선택 → 결과 화면 (무조건 적중) |
| F9 | Exacta | 1·2위 캐릭터 선택 → 결과 화면 (무조건 적중) |
| F10 | Trio | 1·2·3위 캐릭터 선택 → 결과 화면 (무조건 적중) |

- Fisher-Yates 셔플로 캐릭터 인덱스 무작위 섞기 → 화살표 위치 다양하게 표시됨
- `ChangeState(GameState.Result)` 호출 → `CalcScore()` 자동 실행 → 점수 표시까지 완전 동작

#### 신규 파일: SceneBootstrapper.Debug.cs
```csharp
#if UNITY_EDITOR
public partial class SceneBootstrapper
{
    private void UpdateDebugInput()
    {
        if (Input.GetKeyDown(KeyCode.F8))  DebugJumpToResult(BetType.Win,    hitGuaranteed: true);
        if (Input.GetKeyDown(KeyCode.F9))  DebugJumpToResult(BetType.Exacta, hitGuaranteed: true);
        if (Input.GetKeyDown(KeyCode.F10)) DebugJumpToResult(BetType.Trio,   hitGuaranteed: true);
    }
    // Fisher-Yates → fakeRankings → DebugSetFinalRankings()
    // fakeBet (실제 1위부터 선택) → DebugSetCurrentBet()
    // ChangeState(Result)
}
#endif
```

#### 수정 파일
- `GameManager.cs`: `DebugSetCurrentBet(BetInfo)` 추가 (`CurrentBet` private set 우회)
- `RaceManager.cs`: `DebugSetFinalRankings(List<RankingEntry>)` 추가 (`finalRankings` private 우회)
- `SceneBootstrapper.cs`: Update()에 `#if UNITY_EDITOR UpdateDebugInput(); #endif` 추가

---

### 3. CheatGuideWindow ✅ (커밋 `331eada` + `98757ba`)

#### 개요
Unity 상단 메뉴 `DopamineRace > 치트 기능 보기 (Ctrl+Shift+D)`
EditorWindow — 프로젝트의 모든 숨겨진 디버그 키·기능 목록 일괄 조회

#### 6개 섹션

| 섹션 | 키/항목 | 소스 |
|------|---------|------|
| 결과창 미리보기 | F8/F9/F10 | SceneBootstrapper.Debug.cs |
| 레이스 디버그 오버레이 | F1/F2/F3 | RaceDebugOverlay.cs |
| 트랙 & 스폰 편집기 | E/R/S/D | WaypointEditor / SpawnEditor / TrackDebugPath |
| Editor 메뉴 — 프리팹 관리 | Create/Patch UI Prefabs | BettingUIPrefabCreator / ResultUIPrefabCreator |
| 기타 기능 & 설정값 | Ctrl+Shift+D, SAVE_VERSION=2 등 | — |
| 향후 추가될 치트 기능 | (슬롯) | — |

#### 배지 규칙
- `editorOnly=true` → 파란 **[Editor Only]** (`#if UNITY_EDITOR` 블록, 릴리즈 제외)
- `editorOnly=false` → 주황 **[항상 활성]** (릴리즈 빌드 포함 → 향후 검토 필요)

#### 규칙: 새 치트 기능 추가 시
`Assets/Scripts/Editor/CheatGuideWindow.cs`의 해당 섹션에 `DrawTableRow()` 또는 `DrawKeyRow()` 한 줄 추가

---

### 4. 디버그 키 릴리즈 빌드 제거 ✅ (커밋 `5553482`)

**원칙**: 디버그 키 입력 로직은 전부 `#if UNITY_EDITOR` ~ `#endif` 으로 컴파일 제외

| 파일 | 처리 방법 |
|------|---------|
| `RaceDebugOverlay.cs` | F1/F2/F3 키 입력 블록 + OnGUI 상단 `#if !UNITY_EDITOR return` |
| `WaypointEditor.cs` | Update() 전체 바디 `#if UNITY_EDITOR` 래핑 |
| `SpawnEditor.cs` | Update() 전체 바디 `#if UNITY_EDITOR` 래핑 |
| `TrackDebugPath.cs` | D 키 블록 `#if UNITY_EDITOR` 래핑 |

> **주의**: RaceDebugOverlay의 LapSnapshot 수집(LateUpdate)은 비주얼 분석용으로 항상 동작 유지.
> OnGUI 렌더링만 에디터 전용으로 차단.

---

## 전체 커밋 이력 (이번 세션)

| 커밋 | 내용 |
|------|------|
| `1500d61` | ResultPanel 프리팹화 Phase 1 (이전 세션) |
| `94e2da7` | ResultPanel 전체 순위 + 화살표 표시 (Phase 2) |
| `294569d` | 결과창 디버그 단축키 추가 (F8/F9/F10) |
| `331eada` | DopamineRace > 치트 기능 보기 EditorWindow 추가 |
| `98757ba` | CheatGuideWindow — 기존 디버그 키 전수 추가 |
| `5553482` | 디버그 키 입력 #if UNITY_EDITOR 가드 추가 |

---

## MCP Reflection 패턴 (이번 세션 확립)

ResultUIPrefabCreator처럼 `UnityEngine.UI` 어셈블리를 필요로 하는 Editor 메서드는
MCP Roslyn 컴파일러가 직접 호출 불가 → **Reflection 패턴** 사용:

```csharp
var editorAssembly = System.Reflection.Assembly.Load("Assembly-CSharp-Editor");
var creatorType    = editorAssembly.GetType("ResultUIPrefabCreator");
var method         = creatorType.GetMethod("CreateResultPanelPrefab",
                         System.Reflection.BindingFlags.Static |
                         System.Reflection.BindingFlags.NonPublic);

// Font 획득 (GameSettings에서)
var gs         = UnityEngine.Resources.Load<GameSettings>("GameSettings");
var fontField  = gs.GetType().GetField("mainFont",
                     System.Reflection.BindingFlags.Public |
                     System.Reflection.BindingFlags.Instance);
var font       = fontField.GetValue(gs) as Font;

method.Invoke(null, new object[] { font });
```

---

## 남은 작업

- 배팅 화면 UI 배치 수동 조정 (OddsLabel 위치 등)
- 밸런스 플레이 테스트 (도주 파아악 + 선행 4위권)
- RaceBacktestWindow 5바퀴 백테스트
- SampleScene 직접 시작 시 BGM 재생 처리
- EasyChart 순위 차트 런타임 테스트
