# SPEC-007 — 배팅 UI 개선

> 작성일: 2026-02-27
> 상태: Phase 1~5 구현 완료 + Phase 6 QA 통과 + 추가 UI 개선 완료 (2026-03-03)
> 담당: client, qa
> 관련: Google Docs UI 기획서 (2026-02-27), `Docs/20260226_UI디자이너_온보딩가이드.md`

---

## 1. 개요

### 1.1 목적

현재 배팅 화면이 제공하는 정보가 부족하다.
유저는 **"이 캐릭터가 이 트랙에서 강한가?"** 를 직관적으로 판단할 수 없고,
**"내 포인트로 얼마를 벌 수 있는가?"** 를 배팅 전에 확인하기 어렵다.

이 작업은 배팅 UX를 개선하여:
1. 트랙 정보를 명확하게 전달 (트랙 타입 + 거리 구분 + 설명 + 토글)
2. 배팅 전 예상 수익을 실시간으로 표시 (My Point / Get Point)
3. 모든 캐릭터의 배당률을 한눈에 파악 (배당률 배지 항상 표시)
4. 캐릭터 클릭 시 거리별 1등 확률 + 현재 트랙 유리 스탯 강조

### 1.2 변경 없는 것 (명시적 제외)

| 시스템 | 이유 |
|--------|------|
| 레이스 로직 (RacerController, CollisionSystem) | 레이스 밸런스 별도 SPEC |
| 백테스팅 도구 | 레이스 로직 변경 없으니 미러링 불필요 |
| 배팅 타입 계산 로직 (BettingCalculator) | 계산식 변경 없음 |
| ~~레이더차트 30% 확대~~ | ✅ 2026-03-03 완료 (radius 0.30→0.42, 레이블 24px 흰색, 채움 제거) |
| 캐릭터 팝업 슬라이드 애니메이션 좌표 | 확정된 값 변경 금지 |

### 1.3 설계 원칙

1. **기존 StringTable 키 최대 활용** — `str.ui.track.short/mid/long` 이미 존재, 중복 키 생성 금지
2. **SAVE_VERSION 3으로 1회 범프** — CharacterRecord 구조체 변경 → 기존 세이브 자동 초기화
3. **TrackType 열거형 확장** — E_Snow/E_Rain 추가 시 TrackInfo.ParseCSVLine도 함께 수정 필수
4. **프리팹 코드가 소스오브트루스** — BettingUIPrefabCreator 수정 후 반드시 메뉴 재실행
5. **CharacterRecord 거리별 집계는 영구 누적** — recentRaceEntries는 최근 30회만 보존하므로 별도 카운터 필요

---

## 2. Phase 분할

```
Phase 1: 데이터 기반 (TrackType 확장 + CharacterRecord 거리별 집계 + SAVE_VERSION 3)
Phase 2: 트랙 정보 패널 재설계 (레이아웃 + 토글)
Phase 3: My Point / Get Point UI
Phase 4: 배당률 배지 항상 표시
Phase 5: 캐릭터 팝업 — 1등 확률 + 스탯 강조
Phase 6: 통합 QA
```

각 Phase는 **"컴파일 에러 0 + 기본 동작 확인"** 후 다음으로 진행.

---

## 3. Phase 1: 데이터 기반 작업

### 3.1 개요

UI 작업(Phase 2~5)에서 필요한 데이터 계층을 먼저 확보한다.
데이터가 없으면 UI는 뼈대만 남기 때문에, Phase 1은 독립적으로 테스트 가능한 가장 작은 단위다.

---

### 3.2 Phase 1-A: TrackType 열거형 확장

#### 현재 상태

```csharp
// Assets/Scripts/Data/TrackData.cs
public enum TrackType
{
    E_Base,  // 기본
    E_Dirt   // 더트
}
```

`RefreshTrackInfo()`에서 `trackType == TrackType.E_Dirt` 하드코딩 → 새 타입 추가 시 매번 if-else 추가 필요.

#### 변경 후 (TrackData.cs)

```csharp
public enum TrackType
{
    E_Base,   // 기본/일반
    E_Dirt,   // 더트
    E_Snow,   // ← 신규: 눈길
    E_Rain,   // ← 신규: 젖은길
}

/// <summary>TrackType → StringTable 키 변환 (표시용)</summary>
public static string GetTrackTypeKey(TrackType type)
{
    return type switch {
        TrackType.E_Base  => "str.track.type.base",
        TrackType.E_Dirt  => "str.track.type.dirt",
        TrackType.E_Snow  => "str.track.type.snow",
        TrackType.E_Rain  => "str.track.type.rain",
        _                 => "str.track.type.base"
    };
}
// 사용: Loc.Get(TrackData.GetTrackTypeKey(trackInfo.trackType))
```

> **왜 TrackData에 static 메서드?** — TrackType과 같은 파일에 두어야 변경 시 함께 찾기 쉽고,
> TrackInfo/SceneBootstrapper 모두에서 공통으로 호출 가능하다.

#### 변경 후 (TrackInfo.cs — ParseCSVLine)

**파일**: `Assets/Scripts/Racer/TrackInfo.cs`

```csharp
// 기존 (E_Dirt만 처리)
if (typeStr == "E_Dirt")
    t.trackType = TrackType.E_Dirt;
else
    t.trackType = TrackType.E_Base;

// 변경 후 (switch expression)
t.trackType = typeStr switch {
    "E_Dirt" => TrackType.E_Dirt,
    "E_Snow" => TrackType.E_Snow,
    "E_Rain" => TrackType.E_Rain,
    _        => TrackType.E_Base
};
```

> ⚠️ TrackInfo.cs는 `Assets/Scripts/Racer/TrackInfo.cs` 에 있다.
> 직접 수정하지 않으면 CSV 파싱 시 E_Snow/E_Rain이 E_Base로 폴백된다.

#### 변경 후 (TrackDB.csv)

**파일**: `Assets/Resources/Data/TrackDB.csv`

| track_id | track_type (기존) | track_type (변경) |
|----------|-----------------|-----------------|
| normal   | E_Base          | E_Base (유지)   |
| rainy    | E_Base          | **E_Rain**      |
| snow     | E_Base          | **E_Snow**      |
| desert   | E_Dirt          | E_Dirt (유지)   |
| highland | E_Dirt          | E_Dirt (유지)   |

> CSV 컬럼 24번째(마지막): `track_type`

#### 변경 후 (StringTable.csv — 신규 키 추가)

**파일**: `Assets/Resources/Data/StringTable.csv`

7개 언어 모두 입력 필수. 미입력 시 키명이 그대로 노출됨.

| Key                    | ko    | en     | ja    | zh_CN | zh_TW | es      | fr     |
|------------------------|-------|--------|-------|-------|-------|---------|--------|
| `str.track.type.base`  | 일반  | Normal | 普通  | 普通  | 普通  | Normal  | Normal |
| `str.track.type.dirt`  | 더트  | Dirt   | ダート | 沙地 | 沙地  | Tierra  | Terre  |
| `str.track.type.snow`  | 눈길  | Snow   | 雪道  | 雪地  | 雪地  | Nieve   | Neige  |
| `str.track.type.rain`  | 젖은길 | Wet   | 湿路  | 湿地  | 濕地  | Húmedo  | Mouillé |

#### Phase 1-A QA 체크리스트

| 테스트 항목 | 방법 | 기대 결과 |
|------------|------|---------|
| 컴파일 에러 없음 | Unity 재컴파일 | Console 에러 0 |
| E_Snow TrackDB 파싱 | Play 모드 → snow 트랙 라운드 | `trackInfo.trackType == TrackType.E_Snow` (F1 디버그 확인) |
| E_Rain TrackDB 파싱 | Play 모드 → rainy 트랙 라운드 | `trackInfo.trackType == TrackType.E_Rain` |
| `GetTrackTypeKey()` 반환 | 각 타입별 디버그 로그 | E_Snow→"str.track.type.snow", E_Rain→"str.track.type.rain" |
| StringTable 표시 | 배팅 화면 → 각 트랙 | "일반/더트/눈길/젖은길" 올바른 텍스트 |

---

### 3.3 Phase 1-B: CharacterRecord 거리별 집계

#### 문제: 왜 새 필드가 필요한가?

현재 `recentRaceEntries`는 최근 30회만 보존한다. 유저가 100판을 플레이하면 이전 70판은 사라진다.
"1st.N%" 표시는 **누적 히스토리 기반**이므로, 별도 카운터 없이는 불가능하다.

```
[현재] recentRaceEntries: 최근 30회 (슬라이딩 윈도우)
[문제] 장기 플레이 시 초반 데이터 소실 → 1등 확률 부정확
[해결] shortDistRaces/Wins, midDistRaces/Wins, longDistRaces/Wins 누적 카운터 추가
```

#### 변경 후 (CharacterRecord.cs)

**파일**: `Assets/Scripts/Data/Record/CharacterRecord.cs`

```csharp
[System.Serializable]
public class CharacterRecord
{
    // ... (기존 필드 전부 유지)
    public string charId;
    public List<CharacterTrackRecord> trackRecords;
    public List<int> recentOverallRanks;
    public List<RaceEntry> recentRaceEntries;

    // ── ★ 신규: 거리별 누적 집계 ──
    public int shortDistRaces;   // 단거리(1~2랩) 출전 횟수
    public int shortDistWins;    // 단거리 1등 횟수
    public int midDistRaces;     // 중거리(3~4랩) 출전 횟수
    public int midDistWins;      // 중거리 1등 횟수
    public int longDistRaces;    // 장거리(5랩+) 출전 횟수
    public int longDistWins;     // 장거리 1등 횟수

    // ... (기존 메서드 전부 유지)

    // ── ★ 신규: 거리별 1등 확률 계산 ──
    /// <summary>
    /// distKey: GameSettings.GetDistanceKey() 반환값
    /// ("str.ui.track.short" / "str.ui.track.mid" / "str.ui.track.long")
    /// </summary>
    public float GetDistWinRate(string distKey)
    {
        switch (distKey)
        {
            case "str.ui.track.short":
                return shortDistRaces > 0 ? (float)shortDistWins / shortDistRaces : 0f;
            case "str.ui.track.mid":
                return midDistRaces > 0 ? (float)midDistWins / midDistRaces : 0f;
            case "str.ui.track.long":
                return longDistRaces > 0 ? (float)longDistWins / longDistRaces : 0f;
            default:
                return 0f;
        }
    }

    /// <summary>표시용: 0~100% 정수 변환</summary>
    public int GetDistWinPct(string distKey) =>
        Mathf.RoundToInt(GetDistWinRate(distKey) * 100);
}
```

> **왜 string distKey?** — `GameSettings.GetDistanceKey(laps)` 가 이미 `"str.ui.track.short"` 등을 반환하므로,
> 별도 DistanceCategory 열거형 없이 기존 키를 그대로 활용한다. 중복 분기 제거.

#### 변경 후 (ScoreManager.cs — RecordRound)

**파일**: `Assets/Scripts/Manager/ScoreManager.cs`

```csharp
// ── SAVE_VERSION 변경 ──
const int SAVE_VERSION = 3;  // 2 → 3: Phase 1-B CharacterRecord 거리별 필드 추가

// ── RecordRound 내부: 기존 AddResult 호출 직후에 추가 ──
string distKey = gs.GetDistanceKey(currentLaps);  // "str.ui.track.short/mid/long"
foreach (var rr in racerResults)
{
    var charRecord = charRecordStore.GetOrCreate(rr.charId);
    charRecord.AddResult(trackName, rr.rank, currentLaps);  // 기존 호출 유지

    // ★ 신규: 거리별 누적 카운터 갱신
    switch (distKey)
    {
        case "str.ui.track.short":
            charRecord.shortDistRaces++;
            if (rr.rank == 1) charRecord.shortDistWins++;
            break;
        case "str.ui.track.mid":
            charRecord.midDistRaces++;
            if (rr.rank == 1) charRecord.midDistWins++;
            break;
        case "str.ui.track.long":
            charRecord.longDistRaces++;
            if (rr.rank == 1) charRecord.longDistWins++;
            break;
    }
}
```

> ⚠️ SAVE_VERSION 2→3 변경 시 기존 PlayerPrefs의 `DopamineRace_CharRecords` 자동 삭제.
> 개발 중 테스트 데이터가 초기화됨을 감안. (정상 동작)

#### Phase 1-B QA 체크리스트

| 테스트 항목 | 방법 | 기대 결과 |
|------------|------|---------|
| 컴파일 에러 없음 | Unity 재컴파일 | Console 에러 0 |
| 단거리 출전 카운터 | 2랩 트랙 1라운드 완료 후 로그 확인 | 12캐릭터 전원 shortDistRaces +1 |
| 1등 캐릭터 win 카운터 | 2랩 라운드 완료 후 1등 캐릭터 기록 조회 | shortDistWins = 1 (나머지 0) |
| 나누기 0 예외 없음 | 신규 세이브(기록 0) 상태에서 `GetDistWinPct` 호출 | 0 반환, NullRef/DivisionByZero 없음 |
| SAVE_VERSION 초기화 | 기존 세이브 있는 상태로 최초 Play | "세이브 버전 업그레이드: v2 → v3" 경고 + 기록 초기화 |
| 중거리 카운터 | 3랩 트랙 1라운드 | midDistRaces +1 |
| 장거리 카운터 | 5랩 트랙 1라운드 | longDistRaces +1 |

---

## 4. Phase 2: 트랙 정보 패널 재설계

### 4.1 개요

현재 TrackInfoPanel에 **트랙 설명문**을 추가하고, **">>" 토글 버튼**으로 패널 컨텐츠를 접을 수 있게 한다.

추가 이유: 유저가 "이 트랙이 어떤 특성인지"를 한 문장으로 알 수 있어야 배팅 판단에 도움이 된다.
토글 이유: 화면 공간 확보 필요 시 접을 수 있어야 한다. 상태는 라운드 간 유지.

### 4.2 레이아웃 구조 (ASCII)

```
┌─ TrackInfoPanel ─────────────────────────────┐
│ 총 7라운드!                         [>>] ← ToggleBtn │
│ ─────────────────────────────────────────── │
│ 3 Round                                      │  ← RoundLabel
│ 🏔️ 고원                                     │  ← TrackNameLabel
│ 거리 - 중거리: 3바퀴                         │  ← DistanceLabel
│ 트랙 타입 - 더트                             │  ← TrackTypeLabel
│ 험준한 고원. 의지력 높은 캐릭터 유리.         │  ← TrackDescLabel (★신규)
└───────────────────────────────────────────────┘

닫힌 상태 (ToggleBtn "<<"):
┌─ TrackInfoPanel ─────────────────────────────┐
│                                     [<<]      │
└───────────────────────────────────────────────┘
```

### 4.3 구현 상세

#### 4.3-A. BettingUIPrefabCreator.cs — 신규 오브젝트 추가

**파일**: `Assets/Scripts/Editor/BettingUIPrefabCreator.cs`

TrackInfoPanel 빌드 시 추가:

```
TrackInfoPanel (기존 패널)
├── TotalRoundLabel      (기존 유지)
├── RoundLabel           (기존 유지)
├── TrackNameLabel       (기존 유지)
├── DistanceLabel        (기존 유지)
├── TrackTypeLabel       (기존 유지)
├── TrackDescLabel       ← ★ 신규 (Text, font 14, overflow wrap, 다행)
└── TrackInfoToggleBtn   ← ★ 신규 (Button, 우상단 앵커, Text 자식 ">>")
```

`TrackDescLabel` 스펙:
- TextAnchor: UpperLeft
- resizeTextForBestFit: false (크기 고정)
- font size: 14
- color: `new Color(0.8f, 0.8f, 0.8f)` (회색)

`TrackInfoToggleBtn` 스펙:
- 우상단 앵커, 크기 40×30
- Text 자식 이름: "Text", 기본값 ">>"

#### 4.3-B. SceneBootstrapper.Betting.cs — 토글 로직

**파일**: `Assets/Scripts/Manager/UI/SceneBootstrapper.Betting.cs`

```csharp
// ── 필드 추가 ──
private Text trackDescLabel;
private Button trackInfoToggleBtn;
private Text trackInfoToggleBtnText;
// static: 라운드 전환(재빌드) 시에도 유저 선택 유지
private static bool trackPanelOpen = true;

// ── CacheBettingUIReferences — TrackInfoPanel 캐싱 블록에 추가 ──
Transform trackPanel = root.Find("TrackInfoPanel");
if (trackPanel != null)
{
    // ... 기존 캐싱 유지 (trackRoundLabel 등) ...
    trackDescLabel = FindText(trackPanel, "TrackDescLabel");      // ★

    Transform toggleObj = trackPanel.Find("TrackInfoToggleBtn");  // ★
    if (toggleObj != null)
    {
        trackInfoToggleBtn = toggleObj.GetComponent<Button>();
        trackInfoToggleBtnText = FindText(toggleObj, "Text");
        if (trackInfoToggleBtn != null)
            trackInfoToggleBtn.onClick.AddListener(OnTrackPanelToggle);
    }
}

// ── 토글 핸들러 (신규 메서드) ──
private void OnTrackPanelToggle()
{
    trackPanelOpen = !trackPanelOpen;
    ApplyTrackPanelState();
}

private void ApplyTrackPanelState()
{
    Transform trackPanel = bettingPanelRoot?.Find("TrackInfoPanel");
    if (trackPanel == null) return;

    // ToggleBtn 제외한 컨텐츠만 토글
    string[] contentNames = {
        "TotalRoundLabel", "RoundLabel", "TrackNameLabel",
        "DistanceLabel", "TrackTypeLabel", "TrackDescLabel"
    };
    foreach (string n in contentNames)
    {
        Transform child = trackPanel.Find(n);
        if (child != null) child.gameObject.SetActive(trackPanelOpen);
    }
    if (trackInfoToggleBtnText != null)
        trackInfoToggleBtnText.text = trackPanelOpen ? ">>" : "<<";
}
```

> **왜 `static bool`?** — SceneBootstrapper는 라운드 전환 시 재초기화될 수 있다.
> `static`으로 선언하면 재초기화 후에도 이전 토글 상태를 기억한다.

#### 4.3-C. RefreshTrackInfo 수정

```csharp
private void RefreshTrackInfo()
{
    var gs = GameSettings.Instance;
    var gm = GameManager.Instance;
    if (gm == null) return;

    var trackDB = TrackDatabase.Instance;
    TrackInfo trackInfo = trackDB != null ? trackDB.CurrentTrackInfo : null;

    if (trackRoundLabel != null)
        trackRoundLabel.text = Loc.Get("str.ui.track.total_round", gs.TotalRounds);

    if (trackNameLabel != null)
        trackNameLabel.text = trackInfo != null ? trackInfo.DisplayName : "???";

    int laps = gs.GetLapsForRound(gm.CurrentRound);
    string distKey = gs.GetDistanceKey(laps);
    if (distanceLabel != null)
        distanceLabel.text = Loc.Get("str.ui.track.distance", Loc.Get(distKey), laps);

    // ★ 변경: GetTrackTypeKey() 사용 (하드코딩 if-else 제거)
    if (trackTypeLabel != null)
        trackTypeLabel.text = trackInfo != null
            ? Loc.Get(TrackData.GetTrackTypeKey(trackInfo.trackType))
            : Loc.Get("str.track.type.base");

    // ★ 신규: 트랙 설명문
    if (trackDescLabel != null)
        trackDescLabel.text = trackInfo != null ? trackInfo.DisplayDesc : "";

    // ★ 신규: 토글 상태 복원
    ApplyTrackPanelState();
}
```

#### Phase 2 QA 체크리스트

| 테스트 항목 | 방법 | 기대 결과 |
|------------|------|---------|
| 컴파일 에러 없음 | Unity 재컴파일 | Console 에러 0 |
| TrackDescLabel 표시 | 각 5개 트랙 배팅 화면 | 트랙 설명 문자열 정상 표시 |
| `>>` 클릭 | 패널 열린 상태 | 컨텐츠 숨김 + 버튼 텍스트 "<<" |
| `<<` 클릭 | 패널 닫힌 상태 | 컨텐츠 표시 + 버튼 텍스트 ">>" |
| 토글 상태 유지 | 패널 닫기 → 라운드 전환 → 배팅 화면 | 패널 여전히 닫혀있음 |
| E_Snow 타입 텍스트 | snow 트랙 배팅 화면 | "눈길" 표시 |
| E_Rain 타입 텍스트 | rainy 트랙 배팅 화면 | "젖은길" 표시 |
| 거리 텍스트 (단거리) | 2바퀴 트랙 | "단거리: 2바퀴" 포함 |
| 거리 텍스트 (중거리) | 3바퀴 트랙 | "중거리: 3바퀴" 포함 |
| 거리 텍스트 (장거리) | 5바퀴 트랙 | "장거리: 5바퀴" 포함 |

---

## 5. Phase 3: My Point / Get Point UI

### 5.1 개요

유저는 배팅 전에 **"내가 지금 얼마를 가지고 있고, 맞으면 얼마를 버는가"** 를 알아야 한다.
현재 화면 우상단은 라운드/점수 정보만 있다. 배팅 포인트 관련 정보를 아래에 추가한다.

### 5.2 레이아웃

배팅 화면 우상단 (기존 roundText/lapText/scoreText 아래):

```
[3 Round / 7]                ← roundText (기존, y=-10)
[현재 레이스: 3바퀴]          ← lapText (기존, y=-38)
[총 점수: 1,200점]            ← scoreText (기존, y=-62)
─────────────────────────────
[My Point: 1,200]            ← myPointLabel (★신규, y=-86)
[Get Point: 100×3.2 = 320]   ← getPointLabel (★신규, y=-108)
```

- **배팅 화면 전용**: ShowBettingUI() 시 표시, HideBettingUI() 시 숨김
- **캐릭터 선택/타입 변경** 시 GetPoint 실시간 갱신

### 5.3 갱신 규칙

| 이벤트 | My Point | Get Point |
|--------|----------|-----------|
| 배팅 화면 진입 (RefreshBettingUI) | ✅ | ✅ |
| 배팅 타입 탭 클릭 (OnBetTypeClicked) | ❌ | ✅ |
| 캐릭터 클릭/해제 (OnRacerClicked) | ❌ | ✅ |
| 라운드 정산 완료 후 배팅 진입 | ✅ | ✅ |

### 5.4 구현

**파일**: `Assets/Scripts/Manager/UI/SceneBootstrapper.Betting.cs`

```csharp
// ── 필드 추가 ──
private Text myPointLabel;
private Text getPointLabel;

// ── BuildBettingUI에서: 기존 scoreText 생성 바로 아래 추가 ──
myPointLabel = MkText(parent, "",
    new Vector2(1, 1), new Vector2(1, 1),
    new Vector2(-20, -86), new Vector2(250, 25),
    16, TextAnchor.MiddleRight, new Color(0.9f, 0.9f, 1f));

getPointLabel = MkText(parent, "",
    new Vector2(1, 1), new Vector2(1, 1),
    new Vector2(-20, -108), new Vector2(250, 25),
    16, TextAnchor.MiddleRight, Color.yellow);

// ── RefreshPointDisplay 신규 메서드 ──
private void RefreshPointDisplay()
{
    var sm = ScoreManager.Instance;
    if (sm == null) return;

    // My Point
    if (myPointLabel != null)
        myPointLabel.text = $"My Point: {sm.TotalScore:N0}";

    // Get Point
    if (getPointLabel == null) return;
    var gm = GameManager.Instance;
    if (gm == null) return;

    var currentBet = gm.CurrentBet;
    if (currentBet == null || !currentBet.HasSelection())
    {
        getPointLabel.text = "Get Point: -";
        return;
    }

    float odds = currentBet.CurrentOdds;
    int betAmount = 100;  // 고정 배팅 금액 (추후 GameSettings로 이동 가능)
    int expected = Mathf.RoundToInt(betAmount * odds);
    getPointLabel.text = $"Get Point: {betAmount}×{odds:F1} = {expected}";
}
```

> **betAmount=100 고정**: 현재 게임 구조상 배팅 금액이 고정이라면 100을 사용한다.
> 만약 가변이라면 `GameSettings` 에 필드 추가 후 오너에게 확인.

#### StringTable 신규 키 (선택 사항 — 다국어 지원 필요 시)

| Key | Korean | English |
|-----|--------|---------|
| `str.ui.bet.my_point` | My Point: {0:N0} | My Point: {0:N0} |
| `str.ui.bet.get_point` | Get Point: {0}×{1:F1} = {2} | Get Point: {0}×{1:F1} = {2} |
| `str.ui.bet.get_point_empty` | Get Point: - | Get Point: - |

> 우선 하드코딩으로 진행. 다국어 UI 정식 작업 시 StringTable로 이동.

#### Phase 3 QA 체크리스트

| 테스트 항목 | 방법 | 기대 결과 |
|------------|------|---------|
| 컴파일 에러 없음 | Unity 재컴파일 | Console 에러 0 |
| My Point 초기 표시 | 배팅 화면 진입 | 현재 TotalScore 정확히 표시 |
| Get Point 미선택 | 배팅 없는 초기 상태 | "Get Point: -" |
| Get Point 갱신 | 캐릭터 클릭 | 해당 캐릭터 배당률 기반 계산값 표시 |
| Get Point 타입 변경 | 배팅 타입 탭 전환 | 새 타입 배당률 기반으로 갱신 |
| My Point 라운드 후 갱신 | 결과 화면 → 배팅 화면 재진입 | 정산 후 점수 반영 |
| 레이스 화면 숨김 | 경기 시작 → 레이스 화면 | myPoint/getPoint 표시 안 됨 |

---

## 6. Phase 4: 배당률 배지 (모든 캐릭터 항상 표시)

### 6.1 개요

현재 `CharacterItemUI`는 배팅 선택된 캐릭터에만 `BetOrderLabel`을 표시한다.
이를 변경하여 **모든 캐릭터에 단승(Win) 기준 배당률** 배지를 항상 표시한다.

**"단승 고정"인 이유**: 배팅 타입이 바뀌어도 캐릭터 기본 강도는 Win 배당률로 직관적으로 파악할 수 있다.
배팅 타입별 배당률 혼용은 오히려 혼란을 준다.

### 6.2 구현

#### 6.2-A. CharacterItemUI.cs — OddsLabel 추가

**파일**: `Assets/Scripts/Manager/UI/CharacterItemUI.cs`

```csharp
// ── 필드 추가 ──
private Text oddsLabel;  // ★ 배당률 배지 (항상 표시)

// ── Init()에 추가 ──
oddsLabel = FindText("OddsLabel");

// ── SetData()에 추가: oddsInfo.winOdds 사용 ──
if (oddsLabel != null)
{
    if (oddsInfo != null)
        oddsLabel.text = $"x{oddsInfo.winOdds:F1}";
    else
        oddsLabel.text = "x-.--";
    oddsLabel.gameObject.SetActive(true);  // 항상 표시
}
```

> **betOrderLabel 유지**: 배팅 선택 순서("1착", "2착" 등) 표시는 기존 `betOrderLabel`이 계속 담당.
> `oddsLabel`은 완전히 독립된 새 UI 요소로, 서로 영향을 주지 않는다.

#### 6.2-B. BettingUIPrefabCreator.cs — OddsLabel 오브젝트 생성

**파일**: `Assets/Scripts/Editor/BettingUIPrefabCreator.cs`

CharacterItem 프리팹 생성 시 `OddsLabel` 추가:

```
CharacterItem 내부 구조
├── Background
├── IconContainer/Icon
├── ConditionIcon
├── PopularityLabel
├── NameLabel
├── RecordLabel
├── SecondLabel
├── BetOrderLabel    (기존 유지)
└── OddsLabel        ← ★ 신규 (우하단 위치, font 14, 노란색)
```

위치 예시 (우하단):
```
┌──────────────────────────────────────┐
│ [🔥] 도주 번개말      3전2승          │
│                           [x2.3] ←OddsLabel │
└──────────────────────────────────────┘
```

`OddsLabel` 생성 스펙:
- 앵커: 우하단 (anchorMin/Max: (1,0))
- anchoredPosition: (-5, 5)
- 크기: 70×20
- TextAnchor: MiddleRight
- font size: 14
- color: Color.yellow
- `BetOrderLabel`과 동일 위치에 겹쳐도 무방 (BetOrderLabel은 선택 시만 표시)

#### 6.2-C. PopularityInfo.winOdds 확인

`oddsInfo.winOdds` 필드가 `PopularityInfo` 클래스에 없을 경우:
- `oddsInfo.odds` 또는 `oddsInfo.winRate` 등 가장 유사한 필드 확인
- 없으면 Phase 4 시작 전 오너에게 확인 후 진행

#### Phase 4 QA 체크리스트

| 테스트 항목 | 방법 | 기대 결과 |
|------------|------|---------|
| 컴파일 에러 없음 | Unity 재컴파일 | Console 에러 0 |
| 12캐릭터 전원 배지 표시 | 배팅 화면 스크롤 전체 확인 | 모든 캐릭터 "xN.N" 배지 표시 |
| 배당률 서로 다름 | 12캐릭터 배지 값 비교 | 최소 2가지 이상 서로 다른 값 |
| 선택 후 배지 유지 | 캐릭터 클릭 (선택 상태) | OddsLabel 계속 표시 |
| 해제 후 배지 유지 | 재클릭 (해제 상태) | OddsLabel 계속 표시 |
| 라운드 전환 후 갱신 | 결과 → 배팅 화면 | 새 라운드 배당률로 갱신 |
| BetOrderLabel 공존 | 배팅 선택 시 | 선택 순서 배지와 OddsLabel 둘 다 표시 |

---

## 7. Phase 5: 캐릭터 팝업 — 거리별 1등 확률 + 스탯 강조

### 7.1 개요

`CharacterInfoPopup`에 두 가지 개선:
1. **1st.N% 표시**: 각 거리 행 앞에 누적 1등 확률 표시
2. **스탯 노란색 강조**: 현재 트랙의 `powerSpeedBonus/braveSpeedBonus > 0` 스탯 강조

### 7.2 거리별 1등 확률 (1st.N%)

#### 현재 레이아웃

```
ShortDistRow:
  [ShortDistLabel: "단거리"] [ShortDistRanks: "3위 3위 5위 2위 3위 5위"]
```

#### 변경 후 레이아웃

```
ShortDistRow:
  [ShortWinRateLabel: "1st.20%"] [ShortDistLabel: "단거리"] [ShortDistRanks: "3위 3위..."]
```

- `1st.0%` : 기록 없거나 1등 0회 (빈칸 금지, 항상 표시)
- 최대 `1st.100%` (단 1전 1승 시)

#### 구현 (CharacterInfoPopup.cs)

**파일**: `Assets/Scripts/Manager/UI/CharacterInfoPopup.cs`

```csharp
// ── 필드 추가 ──
private Text shortWinRateLabel;
private Text midWinRateLabel;
private Text longWinRateLabel;

// ── Init() — ShortDistRow/MidDistRow/LongDistRow 캐싱 블록에 추가 ──
Transform shortRow = layout1.Find("ShortDistRow");
if (shortRow != null)
{
    shortWinRateLabel = FindText(shortRow, "ShortWinRateLabel");  // ★
    shortDistLabel    = FindText(shortRow, "ShortDistLabel");
    shortDistRanks    = FindText(shortRow, "ShortDistRanks");
}
// MidDistRow, LongDistRow 동일 패턴

// ── UpdateRecentRecords() 마지막에 추가 ──
if (shortWinRateLabel != null)
    shortWinRateLabel.text = record != null
        ? $"1st.{record.GetDistWinPct("str.ui.track.short")}%"
        : "1st.0%";

if (midWinRateLabel != null)
    midWinRateLabel.text = record != null
        ? $"1st.{record.GetDistWinPct("str.ui.track.mid")}%"
        : "1st.0%";

if (longWinRateLabel != null)
    longWinRateLabel.text = record != null
        ? $"1st.{record.GetDistWinPct("str.ui.track.long")}%"
        : "1st.0%";
```

#### BettingUIPrefabCreator — ShortWinRateLabel 등 생성

각 DistRow에 WinRateLabel Text 오브젝트 추가:
- 이름: `ShortWinRateLabel`, `MidWinRateLabel`, `LongWinRateLabel`
- 위치: DistLabel 왼쪽 (HorizontalLayout 첫 번째)
- font size: 13, color: `new Color(1f, 0.85f, 0.3f)` (연한 노란색)
- 크기: 70×20

### 7.3 현재 트랙 유리 스탯 강조

#### 강조 기준

```
TrackInfo.powerSpeedBonus > 0  → Power 인디케이터 노란색
TrackInfo.braveSpeedBonus > 0  → Brave 인디케이터 노란색
(나머지 스탯: 이번 작업 대상 아님)
```

#### 구현 — Show() 시그니처 확장 + UpdateRadarChart 수정

**파일**: `Assets/Scripts/Manager/UI/CharacterInfoPopup.cs`

```csharp
// ── 필드 추가 ──
private TrackInfo pendingTrackInfo;

// ── Show() 시그니처 변경: trackInfo 파라미터 추가 ──
public void Show(CharacterData data, PopularityInfo info,
                 CharacterRecord record, TrackInfo trackInfo = null)
{
    // ... 기존 로직 ...
    pendingTrackInfo = trackInfo;  // ★ 저장
    // ShowSequence에서 UpdateRadarChart → UpdateRadarChartWithHighlight 호출
}

// ── UpdateRadarChart → UpdateRadarChartWithHighlight 로 교체 ──
private void UpdateRadarChartWithHighlight(CharacterData data, TrackInfo trackInfo)
{
    if (radarChart == null || data == null) return;

    radarChart.RemoveData();

    var radar = radarChart.GetChartComponent<RadarCoord>();
    if (radar != null)
    {
        // 강조 여부 결정
        bool highlightPower = trackInfo != null && trackInfo.powerSpeedBonus > 0f;
        bool highlightBrave = trackInfo != null && trackInfo.braveSpeedBonus > 0f;

        bool[] highlights = { false, highlightPower, highlightBrave, false, false, false };
        // 순서: Speed, Power, Brave, Calm, Endurance, Luck

        string[] statKeys = {
            "str.ui.char.stat.speed",  "str.ui.char.stat.power",
            "str.ui.char.stat.brave",  "str.ui.char.stat.calm",
            "str.ui.char.stat.endurance", "str.ui.char.stat.luck"
        };

        radar.indicatorList.Clear();
        for (int i = 0; i < statKeys.Length; i++)
        {
            string label = Loc.Get(statKeys[i]);
            // XCharts Rich Text 지원 시: Rich Text 색상 적용
            // 미지원 시: indicator.textStyle.color 사용 (아래 대안 참고)
            if (highlights[i])
                label = "<color=#FFE000>" + label + "</color>";
            radar.AddIndicator(label, 0, 20);
        }
    }

    // 이하 기존 배경 시리즈 + 스탯 시리즈 추가 로직 동일
    // ...
}

// ── ShowSequence에서 호출 교체 ──
// 기존: UpdateRadarChart(pendingData);
// 변경: UpdateRadarChartWithHighlight(pendingData, pendingTrackInfo);
```

> **XCharts Rich Text 미지원 시 대안**:
> `radar.indicatorList[i].textStyle.color = highlights[i] ? Color.yellow : Color.white;`
> 단, `AddIndicator` 반환값이나 `indicatorList` 직접 접근으로 처리.

#### 호출 측 수정 (SceneBootstrapper.Betting.cs)

```csharp
// OnRacerClicked 내부: charInfoPopup.Show() 호출 시 trackInfo 추가
TrackInfo currentTrack = TrackDatabase.Instance?.CurrentTrackInfo;
charInfoPopup.Show(data, oddsInfo, record, currentTrack);  // ★ 4번째 인수 추가
```

#### Phase 5 QA 체크리스트

| 테스트 항목 | 방법 | 기대 결과 |
|------------|------|---------|
| 컴파일 에러 없음 | Unity 재컴파일 | Console 에러 0 |
| 신규 게임 1등 확률 | 기록 0 상태 → 팝업 열기 | 3거리 모두 "1st.0%" |
| 단거리 1승 후 | 2랩 1라운드 1위 후 팝업 | "1st.100%" (1전1승) |
| 단거리 5전 1승 | 2랩 5회, 1회만 1위 후 팝업 | "1st.20%" |
| 사막 트랙 Power 강조 | desert 트랙 배팅 → 캐릭터 팝업 | Power 인디케이터 노란색 |
| 고원 트랙 Brave 강조 | highland 트랙 → 팝업 | Brave 인디케이터 노란색 |
| 일반 트랙 강조 없음 | normal 트랙 → 팝업 | 모든 인디케이터 흰색(기본) |
| snow/rain 트랙 강조 없음 | E_Snow/E_Rain 트랙 → 팝업 | 강조 없음 (bonus=0) |
| 다른 캐릭터 전환 시 강조 유지 | 팝업 열린 상태 → 다른 캐릭터 클릭 | 트랙 강조 상태 동일하게 유지 |
| Hide → Show 반복 5회 | 팝업 닫기/열기 반복 | 차트 오류/깜빡임 없음 |

---

## 8. Phase 6: 통합 QA

### 8.1 회귀 테스트 (기존 기능)

| 항목 | 확인 방법 | 기대 결과 |
|------|---------|---------|
| 6가지 배팅 타입 탭 전환 | 각 탭 클릭 | 타입별 올바른 레이블 + 색상 |
| 캐릭터 클릭 → 팝업 슬라이드인 | 12캐릭터 각 1회 | 0.25초 슬라이드인 정상 |
| 동일 캐릭터 재클릭 → 팝업 토글 | 팝업 열린 상태 → 동일 캐릭터 클릭 | 팝업 닫힘 |
| 배팅 선택 → BetOrderLabel | 캐릭터 선택 | BetOrderLabel + OddsLabel 공존 |
| "경기 시작" → 카운트다운 | 배팅 후 시작 버튼 | 카운트다운 진입 정상 |
| 7라운드 완주 | 레이스 7회 반복 | 모든 라운드 완료, 최종 결과 정상 |

### 8.2 신규 기능 통합 확인

| 항목 | 방법 | 기대 결과 |
|------|------|---------|
| 5가지 트랙 TrackType | 7라운드 자연 순환 | 모든 트랙 올바른 타입 텍스트 |
| 거리별 집계 누적 | 7라운드 후 캐릭터 팝업 | 1st.N% 0%+ 값으로 표시 |
| My Point 최종 동기화 | 7라운드 종료 → 배팅 진입 | 최종 점수 정확히 반영 |
| 배당률 배지 라운드 후 갱신 | 라운드 전환 | 새 배당률로 12캐릭터 전원 갱신 |
| 패널 토글 상태 7라운드 | 3라운드에서 패널 닫기 → 계속 진행 | 이후 라운드에도 닫힌 상태 유지 |

### 8.3 공통 체크 (qa.md 표준)

- [ ] Unity 콘솔 에러 0건
- [ ] Unity 콘솔 경고 (의도하지 않은 것) 0건
- [ ] Play 모드 진입·종료 반복 3회 후 상태 정상
- [ ] 1920×1080 기준 UI 레이아웃 깨짐 없음
- [ ] 7개 언어 전환 시 텍스트 깨짐 없음
- [ ] 12캐릭터 전원 완주 (미완주 없음)
- [ ] HP/CP 수치 음수 없음

---

## 9. 파일 변경 목록

| 파일 | 변경 유형 | 주요 변경 내용 |
|------|---------|-------------|
| `Assets/Scripts/Data/TrackData.cs` | 수정 | E_Snow/E_Rain 추가, `GetTrackTypeKey()` static 메서드 |
| `Assets/Scripts/Racer/TrackInfo.cs` | 수정 | `ParseCSVLine` — E_Snow/E_Rain 처리 switch 확장 |
| `Assets/Resources/Data/TrackDB.csv` | 수정 | rainy→E_Rain, snow→E_Snow 업데이트 |
| `Assets/Resources/Data/StringTable.csv` | 수정 | `str.track.type.*` 4개 키 (7개 언어) |
| `Assets/Scripts/Data/Record/CharacterRecord.cs` | 수정 | shortDistRaces/Wins 등 6개 필드, `GetDistWinRate()`, `GetDistWinPct()` |
| `Assets/Scripts/Manager/ScoreManager.cs` | 수정 | SAVE_VERSION 2→3, `RecordRound` 거리별 카운터 갱신 |
| `Assets/Scripts/Manager/UI/SceneBootstrapper.Betting.cs` | 수정 | 트랙패널 재설계(TrackDesc/Toggle), My/GetPoint, `RefreshTrackInfo` 타입처리, `Show` 호출에 `trackInfo` 추가 |
| `Assets/Scripts/Manager/UI/CharacterItemUI.cs` | 수정 | `oddsLabel` 필드, `Init/SetData` 갱신 |
| `Assets/Scripts/Manager/UI/CharacterInfoPopup.cs` | 수정 | WinRateLabel 필드, `UpdateRadarChartWithHighlight()`, `Show` 시그니처 변경 |
| `Assets/Scripts/Editor/BettingUIPrefabCreator.cs` | 수정 | TrackDescLabel/TrackInfoToggleBtn, OddsLabel, ShortWinRateLabel 등 생성 |

---

## 10. 주의사항

### 10.1 절대 규칙

1. **BettingUIPrefabCreator 수정 후 반드시 재실행**: `DopamineRace > Create Betting UI Prefabs`
   미실행 시 새 오브젝트(TrackDescLabel, OddsLabel, WinRateLabel 등)가 프리팹에 없어서 NullRef 발생.

2. **SAVE_VERSION 3 변경으로 기존 세이브 삭제**: 개발 중 반복 테스트 데이터 소실 감안.

3. **CharacterInfoPopup 확정 좌표 변경 금지**: `targetPosition` (슬라이드 앵커 좌표),
   `RankChartArea` 크기·위치는 확정된 값. Phase 5 작업 시 신규 오브젝트만 추가.

4. **TrackInfo.cs는 `Assets/Scripts/Racer/`**: Data 폴더가 아니므로 위치 주의.

### 10.2 개발 순서 권장

```
Phase 1-A (컴파일 확인) → Phase 1-B (카운터 로그 확인)
→ Phase 2 (토글 동작 확인) → Phase 3 (포인트 표시 확인)
→ Phase 4 (배지 표시 확인) → Phase 5 (확률+강조 확인)
→ Phase 6 통합 QA → [C] 커밋
```

각 Phase는 **QA 체크리스트 전 항목 통과 후** 다음으로 진행.

### 10.3 알려진 예외 처리 포인트

| 위험 지점 | 예상 문제 | 대응 방법 |
|----------|---------|---------|
| XCharts Rich Text | 인디케이터 라벨 Rich Text 미지원 가능성 | `indicator.textStyle.color` 직접 수정으로 폴백 |
| `oddsInfo.winOdds` 필드 | `PopularityInfo`에 `winOdds` 없을 수 있음 | Phase 4 전 코드 확인 필수 |
| `GameManager.CurrentBet` | CurrentBet/HasSelection() 구현 여부 | 없으면 oddsText/oddsInfo 기반으로 대체 |
| TrackDescLabel 긴 텍스트 | 설명이 길면 패널 레이아웃 넘침 | ContentSizeFitter or 최대 2줄 제한 |

---

## 부록 A: 거리 분류 기준

`GameSettings.GetDistanceKey(laps)` 반환값 (기존 코드 그대로):

| 바퀴수 | 분류 | StringTable 키 |
|-------|------|---------------|
| 1~2랩 | 단거리 | `str.ui.track.short` |
| 3~4랩 | 중거리 | `str.ui.track.mid` |
| 5랩 이상 | 장거리 | `str.ui.track.long` |

현재 게임 트랙 세트 바퀴수: `[2, 2, 3, 5, 3, 2, 4]` → 단/단/중/장/중/단/중 순서.

## 부록 B: TrackType → StringTable 키 매핑 요약

| TrackType | StringTable 키 | 한국어 | 트랙 |
|-----------|---------------|--------|------|
| E_Base    | str.track.type.base | 일반 | normal |
| E_Dirt    | str.track.type.dirt | 더트 | desert, highland |
| E_Snow    | str.track.type.snow | 눈길 | snow |
| E_Rain    | str.track.type.rain | 젖은길 | rainy |

---

## 부록 C: 추가 UI 개선 (2026-03-03)

SPEC-007 Phase 1~6 완료 후 진행된 추가 개선 작업.

### C.1 캐릭터 정보 팝업 개선

| 항목 | 내용 | 파일 |
|------|------|------|
| CharTypeLabel 오버레이 | Layout2_Left로 이동, Illustration 위에 표시 | CharacterInfoPopup.cs |
| StoryIconBtn 제거 | 책 아이콘 삭제 | BettingPanel.prefab (수동) |
| 레이더차트 확대 | radius 0.30→0.42, max 80→110px | CharacterInfoPopup.cs |
| 레이더차트 레이블 | 16→24px, 흰색 명시 | CharacterInfoPopup.cs |
| 레이더차트 채움 제거 | `areaStyle.show = false` (선만 표시) | CharacterInfoPopup.cs |
| 거리별 승률 제거 | 1st.n% 표시 제거 → 순위만 표시 | CharacterInfoPopup.cs |
| 순위 구분자 변경 | `-` → 공백 (예: `1위 3위 5위`) | CharacterInfoPopup.cs |

### C.2 일러스트 크롭 (IllustrationMask)

| 항목 | 내용 |
|------|------|
| 목적 | 캐릭터 일러스트를 4:3(세로:가로) 형태로 좌우 크롭 표시 |
| 구현 | `IllustrationMask` (Mask+Image white) → `Illustration` (AspectRatioFitter) |
| 런타임 | 스프라이트 로드 시 `fitter.aspectRatio = width/height` 갱신 |
| 프리팹 | Create/Patch 모두 지원, 기존 Illustration 경로 호환 |

### C.3 TrackTypeLabel 표시 형식 개선

- **변경 전**: 트랙 타입 문자열만 표시 (예: `더트`)
- **변경 후**: `경기장 상태 : {0}` 형식 (예: `경기장 상태 : 더트`)
- **StringTable 신규 키**: `str.ui.track.type_label` (7개 언어)
- **파일**: SceneBootstrapper.Betting.cs, StringTable.csv
