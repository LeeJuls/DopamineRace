# UI 폰트 시스템 & HideInfoToggle 버그 수정 히스토리

**작업일**: 2026-03-11
**작업자**: Claude (keen-khayyam 워크트리 + main 병행)
**관련 스펙**: SPEC-UI-003_폰트시스템_명세서_20260311.md

---

## 1. HideInfoToggle 버그 수정

### 증상
- HideInfoToggle의 CheckBg가 회색으로 표시됨
- 눌러도 Checkmark 토글이 동작하지 않음
- 스트링(Label) 텍스트 사라지는 현상

### 원인
1. **BettingPanel.prefab**: CheckBg/Checkmark sprite가 null, color가 grey로 설정되어 있었음
2. **Toggle.graphic 미연결**: Unity Toggle의 `graphic` 필드가 Checkmark에 연결되지 않아 `isOn` 변경 시 시각적 반응 없음
3. **PlayerPrefs 의존**: 저장된 상태가 UI 초기 상태와 불일치

### 수정 내용

**`Assets/Prefabs/UI/BettingPanel.prefab`** (MCP로 수정):
- CheckBg: sprite=Btn_Check_01, color=white
- Checkmark: sprite=Btn_Check_02
- Label: color=white

**`Assets/Scripts/Manager/UI/SceneBootstrapper.Betting.cs`**:
- PlayerPrefs 완전 제거
- 항상 `isOn = false`로 시작
- `hideInfoCheckmark` 필드 추가 → `GameObject.SetActive(v)` 직접 제어
- `Toggle.onValueChanged` 핸들러에서 Checkmark 수동 토글

```csharp
private void InitHideInfoToggle()
{
    if (hideInfoToggle == null) return;
    Text toggleLabel = FindText(hideInfoToggle.transform, "Label");
    if (toggleLabel != null)
        toggleLabel.text = Loc.Get("str.ui.betting.hide_info");
    hideInfoCheckmark = hideInfoToggle.transform.Find("Checkmark");
    hideInfoToggle.isOn = false;
    if (hideInfoCheckmark != null)
        hideInfoCheckmark.gameObject.SetActive(false);
    hideInfoToggle.onValueChanged.AddListener(v =>
    {
        if (hideInfoCheckmark != null)
            hideInfoCheckmark.gameObject.SetActive(v);
        if (v && charInfoPopup != null)
            charInfoPopup.Hide();
    });
}
```

---

## 2. 폰트 렌더링 번짐 수정 (fontRenderingMode)

### 증상
픽셀 폰트에서 글자 테두리가 하얗게 번져 보임 (AA 적용됨)

### 원인
모든 픽셀폰트의 `fontRenderingMode`가 0(Smooth/AA)으로 설정되어 있었음

### fontRenderingMode 옵션
| 값 | 이름 | 설명 |
|----|------|------|
| 0 | Smooth | 안티앨리어싱 적용 (픽셀폰트에 부적합) |
| 1 | HintedSmooth | 힌팅 + AA |
| 2 | HintedRaster | 힌팅만, AA 없음 ← **픽셀폰트 권장** |
| 3 | OSDefault | OS 기본값 |

### 수정한 .meta 파일 (fontRenderingMode: 0 → 2)
- `Assets/Fonts/PFstardust3_0_ExtraBold.ttf.meta`
- `Assets/Fonts/PFstardust3_0.ttf.meta`
- `Assets/Fonts/NeoDunggeunmo/neodgm.ttf.meta`
- `Assets/Fonts/ArkPixel/ark-pixel-12px-monospaced-ko.ttf.meta`
- `Assets/Fonts/ArkPixel/ark-pixel-12px-monospaced-latin.ttf.meta`

> **주의**: MCP SerializedObject 방식은 메모리에만 적용되고 .meta에 저장 안 됨.
> .meta 파일을 Edit 도구로 직접 편집해야 영구 적용됨.

---

## 3. 폰트 분리 적용 시스템 구축

### 목표
- Main Font (PFstardust3_0_ExtraBold 영문 픽셀폰트)
- Korean Font (neodgm / NeoDunggeunmoPro) 자동 분기

### `Assets/Scripts/Utility/FontHelper.cs` 전면 개편

**이중 판별 로직**:
1. **언어 기반**: `Loc.CurrentLang == "ko"` → koreanFont
2. **텍스트 내용 기반**: 한글 유니코드 범위(가-힣, 자모 등) 포함 시 → koreanFont

**주요 메서드**:
```csharp
GetUIFont()         // 현재 언어 기반 폰트 반환 (워크트리)
GetMainFont()       // 현재 언어 기반 폰트 반환 (main)
GetFontForText(string text)  // 텍스트 내용 기반 이중 판별
IsKoreanLang()      // Loc.CurrentLang == "ko"
ContainsKorean(string text)  // 한글 유니코드 범위 체크
ApplyToTextMesh(TextMesh tm) // TextMesh에 텍스트 기반 폰트 적용
```

**관련 파일 수정**:
- `SceneBootstrapper.cs`: `GetUIFontWithFallback()` → `GetUIFont()`
- `SceneBootstrapper.Utilities.cs`: `ApplyFontToAllText()` — 텍스트별 폰트 분기
- `TitleSceneManager.cs`: `GetUIFontWithFallback()` → `GetUIFont()`

**GameSettings 필드 활용**:
- `mainFont`: PFstardust3_0_ExtraBold (영문 픽셀폰트)
- `koreanFont`: NeoDunggeunmoPro 또는 neodgm

---

## 4. XCharts 레이더 차트 폰트 적용

### 증상
`radar.axisName.labelStyle.textStyle.font = FontHelper.GetUIFont()` 설정해도 폰트 미적용

### 원인 분석 (XCharts 소스 역추적)
`ChartHelper.AddTextObject()` (line 349):
```csharp
chartText.text.font = textStyle.font == null ? theme.font : textStyle.font;
```
`RadarCoordHandler.cs`에서 라벨 생성 시 `chart.theme.common`을 theme으로 전달.
→ `RemoveData()` → `RefreshChart()` 과정에서 textStyle 객체가 재생성되면 font=null 초기화됨
→ 결국 theme.font fallback 경로가 사용됨

### 해결책
**textStyle 레벨이 아닌 theme 레벨**에서 폰트 설정:
```csharp
radarChart.theme.common.font = FontHelper.GetUIFont(); // InitRadarChart
radarChart.theme.common.font = FontHelper.GetUIFont(); // UpdateRadarChart (RemoveData 후)
```

**적용 파일**: `Assets/Scripts/Manager/UI/CharacterInfoPopup.cs`
- `InitRadarChart()`: Init() 직후 theme.common.font 설정
- `UpdateRadarChart()`: RemoveData() 직후 재설정

> **main 프로젝트 주의**: FontHelper 메서드명이 `GetUIFont()` (워크트리) vs `GetMainFont()` (main)으로 다름.
> main에는 `GetMainFont()` 사용.

---

## 5. 미해결 / 추가 확인 필요

- **neodgm fontSize=96**: 텍스처 오버플로 경고 — fontSize를 16~24로 낮추는 것 권장
- **레이더 차트 폰트**: theme.common.font 방식 적용 — 실제 동작 확인 필요 (Play 후 확인)
- **XCharts 워크트리 메서드명**: worktree의 FontHelper는 `GetUIFont()`, main은 `GetMainFont()` — 머지 시 통일 필요
