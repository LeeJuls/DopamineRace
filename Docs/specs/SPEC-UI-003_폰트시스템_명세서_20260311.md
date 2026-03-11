# SPEC-UI-003: 폰트 시스템 명세서

**버전**: 1.0
**작성일**: 2026-03-11
**작성자**: Claude
**상태**: 구현 완료 (main + keen-khayyam 워크트리)

---

## 1. 개요

DopamineRace의 UI 폰트는 언어별로 분리 적용된다.
- **영문/기타**: PFstardust3_0_ExtraBold (픽셀폰트)
- **한국어**: NeoDunggeunmoPro-Regular 또는 neodgm (픽셀폰트)

폰트 선택은 `FontHelper` 클래스에서 전담하며, 언어 설정 + 텍스트 내용 이중 판별을 사용한다.

---

## 2. 폰트 에셋

| 용도 | 파일 | GameSettings 필드 |
|------|------|------------------|
| 메인 (영문/기타) | `Assets/Fonts/PFstardust3_0_ExtraBold.ttf` | `mainFont` |
| 한국어 | `Assets/Fonts/NeoDunggeunmoPro-Regular.ttf` | `koreanFont` |
| 한국어 (구버전) | `Assets/Fonts/NeoDunggeunmo/neodgm.ttf` | (대체) |

### 2.1 폰트 Import 설정 기준

| 항목 | 값 | 이유 |
|------|-----|------|
| `fontRenderingMode` | 2 (HintedRaster) | 픽셀폰트 AA 번짐 방지 |
| `Character` | Dynamic | 런타임 한글 렌더링 필수 |
| `fontSize` | 16~24 | 텍스처 오버플로 방지 |

> **주의**: neodgm의 fontSize를 96으로 설정 시 4096 텍스처 오버플로 경고 발생.
> 런타임에는 영향 없으나, 16~24 권장.

---

## 3. FontHelper API

파일: `Assets/Scripts/Utility/FontHelper.cs`

### 3.1 주요 메서드

| 메서드 | 설명 | 반환 |
|--------|------|------|
| `GetUIFont()` | 현재 언어 기반 폰트 (워크트리용) | Font |
| `GetMainFont()` | 현재 언어 기반 폰트 (main용) | Font |
| `GetFontForText(string text)` | 언어 + 텍스트 내용 이중 판별 | Font |
| `GetUIFontWithFallback()` | GameSettings 없을 때 폴백 | Font |
| `IsKoreanLang()` | `Loc.CurrentLang == "ko"` | bool |
| `ContainsKorean(string text)` | 한글 유니코드 범위 체크 | bool |
| `ApplyToTextMesh(TextMesh tm)` | TextMesh에 텍스트 기반 폰트 적용 | void |

> **브랜치별 메서드명 불일치** (머지 후 통일 필요):
> - 워크트리(keen-khayyam): `GetUIFont()`
> - main: `GetMainFont()`

### 3.2 폰트 판별 우선순위

```
1. GameSettings 없음 → GetUIFontWithFallback() (Resources.Load 폴백)
2. IsKoreanLang() && koreanFont != null → koreanFont
3. ContainsKorean(text) && koreanFont != null → koreanFont  (GetFontForText 전용)
4. mainFont != null → mainFont
5. GetUIFontWithFallback()
```

---

## 4. 적용 위치

### 4.1 전체 UI 일괄 적용

`SceneBootstrapper.Utilities.cs` — `ApplyFontToAllText()`:
```csharp
// Text 컴포넌트: 텍스트 내용 기반 분기
Font resolved = FontHelper.GetFontForText(t.text);
t.font = resolved ?? font;

// TextMesh: FontHelper.ApplyToTextMesh()
FontHelper.ApplyToTextMesh(tm);
```

### 4.2 씬별 초기화

| 위치 | 메서드 | 호출 시점 |
|------|--------|-----------|
| `SceneBootstrapper.cs` | `FontHelper.GetUIFont()` | Start() |
| `TitleSceneManager.cs` | `FontHelper.GetUIFont()` | Start() |

### 4.3 XCharts 레이더 차트

`CharacterInfoPopup.cs`:
```csharp
// theme-level 설정 (textStyle 레벨보다 확실)
radarChart.theme.common.font = FontHelper.GetUIFont(); // InitRadarChart
radarChart.theme.common.font = FontHelper.GetUIFont(); // UpdateRadarChart (RemoveData 후)
```

**이유**: XCharts `ChartHelper.AddTextObject()`가 라벨 생성 시:
```csharp
chartText.text.font = textStyle.font == null ? theme.font : textStyle.font;
```
`RemoveData()` 후 textStyle이 초기화될 수 있어 theme.font fallback이 사용됨.
→ `theme.common.font` 직접 설정이 가장 안전.

---

## 5. HideInfoToggle 스펙

### 5.1 동작 정의

| 상태 | CheckBg | Checkmark | 캐릭터 정보 팝업 |
|------|---------|-----------|----------------|
| OFF (기본) | Btn_Check_01 표시, white | 숨김 | 정상 표시 |
| ON | Btn_Check_01 표시, white | Btn_Check_02 표시 | 즉시 숨김 |

### 5.2 구현 규칙

- **초기값**: 항상 OFF (PlayerPrefs 미사용)
- **Toggle.graphic**: 연결하지 않음 (Unity 기본 동작 우회)
- **Checkmark 제어**: `hideInfoCheckmark.gameObject.SetActive(v)` 직접 호출
- **팝업 연동**: ON 시 `charInfoPopup.Hide()` 즉시 호출

### 5.3 BettingPanel.prefab 설정값

```
HideInfoToggle/
├── CheckBg
│   ├── Image.sprite = Btn_Check_01
│   └── Image.color = white (1,1,1,1)
├── Checkmark
│   ├── Image.sprite = Btn_Check_02
│   └── (SetActive로 제어)
└── Label
    └── Text.color = white (1,1,1,1)
```

---

## 6. 의존성

| 컴포넌트 | 의존 |
|----------|------|
| FontHelper | GameSettings.mainFont, GameSettings.koreanFont, Loc.CurrentLang |
| CharacterInfoPopup | XCharts (com.monitor1394.xcharts), FontHelper |
| SceneBootstrapper.Utilities | FontHelper |
| HideInfoToggle | charInfoPopup (CharacterInfoPopup 참조) |
