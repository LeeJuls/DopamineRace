# SPEC-040 — ExchangeModal 배경 이미지 적용 + CurrencyUIPrefabCreator 경로 수정

**날짜**: 2026-06-20 | **상태**: 구현 완료

## 배경

ExchangeModal의 Modal 패널 배경이 단색(어두운 보라 `#1F1A2E`)이어서 BettingPanel 팝업들과
시각적 일관성이 없었음. BettingPanel의 CharacterInfoPopup·TrackInfoPanel은 이미
`Img_BG_ListSlot_BG_01` (9-슬라이스)를 적용 중 — ExchangeModal에도 동일 이미지 적용.

또한 `CurrencyUIPrefabCreator.cs`의 저장 경로가 SPEC-028 폴더 통일(commit fcf460a)에서
반영되지 않아 `Assets/Resources/Prefabs/UI/`(구버전)에 저장하던 버그도 함께 수정.

## 변경 내용

### 1. ExchangeModal 배경 이미지

| 항목 | 변경 전 | 변경 후 |
|------|---------|---------|
| Modal Image sprite | (없음) | `Img_BG_ListSlot_BG_01` |
| Image.type | Simple (단색) | Sliced |
| Image.color | `(0.12, 0.10, 0.18, 0.95)` | `(1, 1, 1, 1)` white |
| raycastTarget | true | false |

**스프라이트 로드**: `AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/UI/Img_BG_ListSlot_BG_01.png")`  
**폴백**: null 시 `Debug.LogWarning` + 기존 단색 유지

### 2. CurrencyUIPrefabCreator 저장 경로 수정

```
[수정 전] Assets/Resources/Prefabs/UI/*.prefab  (구버전, fcf460a 이후 불일치)
[수정 후] Assets/Prefabs/UI/*.prefab            (현재 실제 경로)
```

EnsureFolder 경로, 4개 prefab 저장 경로, 헤더 주석 전부 수정.

### 3. MenuItem 경로 수정 (에디터 메뉴 통일)

```
[수정 전] DopamineRace/Create Currency UI Prefabs
[수정 후] DopamineRace/프리팹 생성/Currency UI Prefabs
```

SPEC-039(his044)에서 다른 8개 Creator를 `프리팹 생성/` 서브메뉴로 이동했으나
`CurrencyUIPrefabCreator.cs`(`Assets/Editor/` 위치)가 누락됐던 것을 이번에 수정.

## 변경 파일

| 파일 | 내용 |
|------|------|
| `Assets/Editor/CurrencyUIPrefabCreator.cs` | BG01 sprite 적용 코드, 저장 경로 수정, MenuItem 수정 |
| `Assets/Prefabs/UI/ExchangeModalPrefab.prefab` | BG01 sprite 적용 후 재생성 |
| `Assets/Prefabs/UI/CurrencyHeaderPrefab.prefab` | (경로 수정으로 동일 위치 재생성) |
| `Assets/Prefabs/UI/ExchangeIconPrefab.prefab` | (동일) |
| `Assets/Prefabs/UI/BetAmountModalPrefab.prefab` | (동일) |

## 검증

- Inspector: `Modal > Image.sprite = Img_BG_ListSlot_BG_01`, type=Sliced, color=white, raycast=false ✅
- 콘솔 Warning 없음 (bgSprite 정상 로드) ✅
- 저장 경로 `Assets/Prefabs/UI/` 확인 ✅
