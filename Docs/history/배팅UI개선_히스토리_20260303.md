# 배팅 UI 개선 히스토리

> 작업일: 2026-03-03
> 최종 커밋: `a78e3d1`
> 브랜치: `main`

---

## 작업 요약

### 1. Total 점수 버그 수정 ✅
**증상**: 게임을 새로 실행해도 Total 점수가 0이 아닌 이전 세션 점수에서 시작

**원인**: `SceneBootstrapper.Betting.cs`의 `RefreshMyPoint()`에서 `sm.TotalScore`(PlayerPrefs 영구 누적값)를 사용하고 있었음

**수정**:
```csharp
// 수정 전
int total = sm != null ? sm.TotalScore : 0;
// 수정 후
int total = sm != null ? sm.CurrentGameScore : 0;
```

**구조 이해**:
- `TotalScore` = PlayerPrefs 영구 누적 (전체 플레이 통산)
- `CurrentGameScore` = RoundHistory 합산 (이번 게임만, ResetAll() 시 0)

---

### 2. 배팅 타입 변경 시 CharacterInfoPopup 자동 닫기 ✅
**증상**: 캐릭터 선택 후 배팅 탭을 바꾸면 캐릭터 선택은 초기화되지만 팝업은 그대로 떠있어 어색한 UI 상태 발생

**수정**: `OnBetTypeClicked()`에 `charInfoPopup.Hide()` 추가

```csharp
private void OnBetTypeClicked(BetType type, int tabIndex)
{
    ...
    if (charInfoPopup != null) charInfoPopup.Hide();
}
```

---

### 3. Odds 표시 변경 — 동적 배당률 → 배팅 타입 고정 배율 ✅
**변경 전**: 캐릭터 선택 후 동적 계산 배당률 표시 (예: `Odds : 6.0x`)
**변경 후**: 배팅 탭 선택 즉시 해당 타입의 고정 배율 표시 (예: `Odds : 8x`)

**수정**: `UpdateOddsDisplay()`에서 `oddsText` 로직 변경
- `bet != null` 이면 캐릭터 선택 여부와 무관하게 `BettingCalculator.GetPayout(bet.type)` 표시
- `bet == null` 이면 `Odds : -` 표시

---

### 4. 포인트 공식 색상 적용 ✅
**변경**: `8x1.7 = 48` 형식에 Rich Text 색상 적용
- `8` (basePt) → 노란색 `#FFE000`
- `1.7` (odds) → 주황색 `#FF8C00`

```csharp
pointsFormulaText.text = string.Format(
    "<color=#FFE000>{0}</color>x<color=#FF8C00>{1}</color> = {2}",
    basePt, odds.ToString("F1"), result);
```

---

### 5. OddsText 탐색 경로 확장 ✅
**문제**: OddsText를 `OddsArea` → `BetDescText` 하위로 이동 후 참조 실패 (`null`)

**원인**: `FindText()`가 직계 자식만 탐색하는 구조. 코드는 `TopArea/OddsArea/OddsText` 경로로만 탐색.

**수정**: OddsArea에서 못 찾을 경우 `BetDescText` 하위에서 재탐색

```csharp
if (oddsText == null)
{
    Transform betDescText = topArea.Find("BetDescText");
    if (betDescText != null)
        oddsText = FindText(betDescText, "OddsText");
}
```

---

### 6. TitleText 크기 조정 ✅
**문제**: BestFit MaxSize를 올려도 텍스트 크기가 변하지 않음

**원인**: BestFit은 RectTransform Height 안에 들어가는 최대 폰트 크기를 찾음. Height(60px)가 실제 병목이었고 MaxSize는 무의미.

**수정** (`BettingPanel.prefab`):
- `TitleText` Height: `60` → `78` (~30% 증가)
- `TitleText` MaxSize: `40` → `120`

---

## 수정 파일

| 파일 | 변경 내용 |
|------|----------|
| `Assets/Scripts/Manager/UI/SceneBootstrapper.Betting.cs` | 1~5번 수정 |
| `Assets/Prefabs/UI/BettingPanel.prefab` | TitleText 크기 조정 |
| `Docs/setup/mcp_kill_zombie.bat` | 이전 세션 수정 |

---

## BestFit 동작 원리 (교훈)

- BestFit은 **RectTransform의 Height**를 상한으로 삼아 폰트 크기를 결정
- MaxSize는 BestFit의 탐색 상한이지만, Height가 더 작으면 Height가 병목
- 텍스트를 키우려면 → **Height를 늘려야** 함 (MaxSize만 올리면 효과 없음)
- Horizontal Overflow=Wrap인 경우 가로도 제약이 되므로 Width도 고려
