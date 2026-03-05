# 인수인계서 — FinishPanel 프리팹 전환 + F4 디버그 메뉴
> 작업일: 2026-03-05 (세션 2)
> 최신 커밋: `503410b`
> 브랜치: `main`
> Push: 진행 예정

---

## 이번 세션 작업 요약

### 0. ResultPanel 5~9위 행간 균등 조정 ✅ (커밋 `4fecd39`)
- 5위(-90) / 9위(-552) 위치 고정, 6·7·8위 y값 등간격 재배치
- gap = (-552 - (-90)) / 4 = -115.5
  - 6위 → y = -205.5, 7위 → y = -321, 8위 → y = -436.5
- MCP로 ResultPanel.prefab 직접 수정

---

### 1. FinishPanel 프리팹 전환 + F4 디버그 메뉴 ✅ (커밋 `cca998b`)

#### 배경
기존 `ShowFinish()` UI가 코드 동적 생성 방식 → ResultPanel 패턴과 동일하게 프리팹 기반으로 전환.

#### 변경 파일

| 파일 | 변경 내용 |
|------|----------|
| `Assets/Scripts/Data/GameSettings.cs` | `finishPanelPrefab` (GameObject) 필드 추가 |
| `Assets/Scripts/Manager/UI/SceneBootstrapper.cs` | Finish 캐시 필드 9개 추가 (`finishPanelRoot`, `finishScrollRect` 등) |
| `Assets/Scripts/Manager/UI/SceneBootstrapper.Finish.cs` | `BuildFinishUI()` 프리팹 분기 + `CacheFinishUIReferences()` + `BuildFinishUILegacy()` 폴백 |
| `Assets/Scripts/Manager/UI/SceneBootstrapper.Debug.cs` | F4 IMGUI 디버그 메뉴 추가 + `DebugJumpToFinish()` |
| `Assets/Scripts/Editor/FinishLeaderboardUIPrefabCreator.cs` | **신규** — `CreateFinishPanelPrefab()` + 버튼/텍스트 헬퍼 |
| `Assets/Prefabs/UI/FinishPanel.prefab` | **신규** — MCP로 자동 생성, GameSettings 자동 연결 |

#### 프리팹 구조
```
FinishPanel (880×650, 반투명 어두운 배경)
├─ TitleText          (금색 55pt, str.finish.title)
├─ RoundScrollView    (700×300, ScrollRect — 스크롤 지원)
│  └─ Viewport        (RectMask2D, fill)
│     └─ RoundDetailText (Text + ContentSizeFitter, 20pt, UpperLeft)
├─ TotalScoreText     (500×60, 38pt, 노랑)
├─ NewGameBtn         (220×55, 파란)
│  └─ BtnText
└─ Top100Btn          (220×55, 보라)
   └─ BtnText
```

#### F4 디버그 메뉴 설계
```
[DEBUG MENU] (F4 토글, ESC 닫기)

1. 결과창 (Win)
2. 결과창 (Exacta)
3. 결과창 (Trio)
4. 최종 결산 (Finish)

ESC / F4 = 닫기
```
- `#if UNITY_EDITOR` 가드 — 릴리즈 빌드 미포함
- 기존 F8/F9/F10 단축키 그대로 유지 (빠른 접근용)

---

### 2. FinishPanel RoundDetailText 왼쪽 정렬 ✅ (커밋 `c1a3c53`)
- `UpperCenter` → `UpperLeft` 변경 (MCP 직접 수정)
- 라운드별 결과 가독성 향상

---

### 3. FinishPanel ScrollRect 스크롤 구조 적용 ✅ (커밋 `cacc7a9`)

#### 배경
라운드 수가 늘어날 경우(예: 10라운드) RoundDetailText가 NewGameBtn / Top100Btn과 겹치는 문제 발생.

#### 해결
`RoundDetailText` (단순 Text) → `RoundScrollView` (ScrollRect) 구조로 변경.

```
RoundScrollView (ScrollRect)
  └─ Viewport (RectMask2D)
       └─ RoundDetailText (Text + ContentSizeFitter.VerticalPreferred)
```

- `ScrollRect`: `horizontal=false`, `vertical=true`, `Clamped`, `scrollSensitivity=30`
- `ContentSizeFitter`: `VerticalFit = PreferredSize` → 텍스트 내용에 따라 높이 자동 확장
- `ShowFinish()` 끝에 `Canvas.ForceUpdateCanvases()` + `finishScrollRect.normalizedPosition = (0,1)` 추가

#### 검증 결과 (15/15 OK)
```
OK TitleText
OK RoundScrollView (ScrollRect)
OK Viewport (RectMask2D)
OK RoundDetailText (ContentSizeFitter)
OK TotalScoreText
OK NewGameBtn / BtnText
OK Top100Btn / BtnText
OK GameSettings.finishPanelPrefab 연결
```

---

### 4. FinishPanel 폰트 적용 ✅ (커밋 `532c68c`)
- 프리팹 생성 시 `Font` 타입 미지원 환경으로 `null` 전달 → 별도 패치 스크립트로 적용
- 5개 Text 컴포넌트 전체에 `neodgm` 폰트 적용 (MCP Reflection)

---

### 5. bold 태그 제거 ✅ (커밋 `503410b`)
- `SceneBootstrapper.Finish.cs` 138번 줄 `<b>...</b>` 태그 제거
- neodgm 폰트 특성상 bold 합성 시 전체가 두껍게 보이는 현상 수정

---

## 아키텍처 포인트

### 프리팹 전환 패턴 (ResultPanel과 동일)
```
BuildXxxUI(parent)
  ├─ gs.xxxPanelPrefab != null  →  Instantiate() + CacheXxxUIReferences()
  └─ else                       →  BuildXxxUILegacy()  (폴백)
```

### MCP 프리팹 생성 시 주의사항
- MCP 스크립트 환경에서 `UnityEngine.Font`, `UnityEngine.UI.*` 직접 참조 불가
  → `null` 폰트 전달 후 별도 Reflection 패치 스크립트로 폰트 적용
- Editor DLL 수정 후 반드시 Unity 재컴파일 대기 → `isCompiling: False` 확인 후 실행

---

## StringTable 변경사항
없음 — 기존 키 전부 재사용
- `str.finish.title / round_header / hit / miss / score_plus / score_zero / total`
- `str.ui.btn.new_game / top100`

---

## 다음 작업 대기 (Phase 2)
- **LeaderboardPanel.prefab 프리팹 전환**
  - `GameSettings.leaderboardPanelPrefab` 필드 추가
  - `FinishLeaderboardUIPrefabCreator.cs`에 `CreateLeaderboardPanelPrefab()` 추가
  - `SceneBootstrapper.Leaderboard.cs` 프리팹 분기 + `CacheLeaderboardUIReferences()`
  - F4 메뉴 5번 → `DebugShowLeaderboard()` 추가
