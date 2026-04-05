# 히스토리: CharacterItem UI 스프라이트 적용 + WaterDropDecor (2026-03-10)

> 브랜치: `main` (워크트리: `claude/keen-khayyam`)
> 전달노트: `Docs/incoming/전달노트_양식_260309_UI적용.md`

---

## 1. 전달노트 260309 — Label → Image+Text 구조 변환

### 대상 3개 오브젝트
`PopularityLabel`, `OddsLabel`, `BetOrderLabel`

### 작업 전 구조
```
PopularityLabel (GameObject, TMP_Text 단독)
```

### 작업 후 구조
```
PopularityLabel (GameObject)
  └─ Image (배경 스프라이트: Img_State / Img_Rank / Img_Multiple)
  └─ Text (TMP_Text 자식)
```

### 스프라이트 매핑
| 오브젝트 | 스프라이트 | 비고 |
|----------|-----------|------|
| `PopularityLabel` | `Img_State` | `POP.{0}` 텍스트 |
| `OddsLabel` | `Img_Multiple` | `{0}x` 텍스트 |
| `BetOrderLabel` | `Img_Rank` | 선택 시만 표시, 컨테이너 on/off |

### 코드 변경: CharacterItemUI.cs
- `FindText()` 경로 수정: `"PopularityLabel"` → `"PopularityLabel/Text"` (동일하게 Odds, BetOrder)
- `SetBetOrder()`: 텍스트 오브젝트 on/off → **부모 Image 컨테이너** on/off로 변경

### MCP 이슈
- `Font` 타입이 MCP Roslyn 어셈블리에 없음
- 해결: `UnityEngine.Object` + SerializedObject `m_Font` 프로퍼티 `objectReferenceValue`로 우회

---

## 2. str.ui.char.popularity 포맷 변경

| 버전 | 포맷 | 이유 |
|------|------|------|
| 구 | `Pop{0}.` | 초기 임시 포맷 |
| 1차 | `pop.{0}` | 소문자 + 점 앞으로 이동 |
| 최종 | `POP.{0}` | 대문자가 시각적으로 명확 |

적용 파일: `Assets/Resources/Data/StringTable.csv` (line 129, `str.ui.char.popularity`)

---

## 3. Img_BG_ListSlot_BG_01~04 — CharacterItem 배경 9-Slice + Button SpriteState

### 배경
- CharacterItem 루트에 흰색 배경 + 검은 테두리 스프라이트 적용
- 4가지 Button 상태 (Normal/Highlighted/Pressed/Disabled)

### Import 설정
| 항목 | 값 |
|------|----|
| Texture Type | Sprite (2D and UI) |
| Filter Mode | Point (no filter) |
| Compression | None |
| spriteBorder | L2 R2 T2 B2 |

### 프리팹 설정
| 항목 | 값 |
|------|----|
| Image → Sprite | `Img_BG_ListSlot_BG_01` (Normal) |
| Image → Type | Sliced |
| Button → Transition | Sprite Swap |
| Highlighted Sprite | `Img_BG_ListSlot_BG_02` |
| Pressed Sprite | `Img_BG_ListSlot_BG_03` |
| Disabled Sprite | `Img_BG_ListSlot_BG_04` |

### CharacterItemUI.cs 색상 상수 변경
```csharp
// 변경 전: 틴트 색상 (어두운 배경 가정)
private static readonly Color COLOR_DEFAULT  = new Color(0.15f, 0.15f, 0.2f, 0.9f);
private static readonly Color COLOR_SELECTED = new Color(0.25f, 0.35f, 0.55f, 0.9f);

// 변경 후: 흰색 스프라이트 기준 (원색 유지)
private static readonly Color COLOR_DEFAULT  = Color.white;
private static readonly Color COLOR_SELECTED = new Color(0.6f, 0.85f, 1f, 1f); // 하늘색 틴트
```

---

## 4. WaterDropDecor — CharacterItem 정적 물방울 장식

### 개요
CharacterItem 패널 우 하단에 반투명 원형 물방울을 정적으로 배치하는 컴포넌트.
애니메이션 없음, 순수 장식 목적.

### 파일
- 신규: `Assets/Scripts/Manager/UI/WaterDropDecor.cs`
- 프리팹에 컴포넌트 추가: `Assets/Prefabs/UI/CharacterItem.prefab`

### 렌더링 순서
- `WaterDropContainer.SetSiblingIndex(1)` → 배경(Index 0) 바로 다음
- 모든 Label/Icon보다 **뒤**에 렌더링됨

### 개발 이슈 및 해결

#### ① 초기 배치: 오른쪽 세로 → 우 하단 밀집으로 변경
초기에 오른쪽 세로 배열로 시작. 오너 요청으로 우 하단 2행 밀집 패턴으로 수정.

#### ② [ExecuteAlways] + OnValidate 도입 → 실패
프리팹 에디터에서 실시간 미리보기를 위해 도입했으나:
```
Setting the parent of a transform which resides in a Prefab Asset is disabled
```
`OnValidate()` → `delayCall` → `Rebuild()`가 Prefab Asset을 직접 수정하려 해 Unity가 차단.
`DestroyImmediate`는 성공했으나 `SetParent`가 실패하여 프리팹에 WaterDropContainer가 잘못 저장됨.

**해결**: `[ExecuteAlways]` + `OnValidate()` 완전 제거, Awake()-only 방식으로 복구.
프리팹에 저장된 WaterDropContainer도 `PrefabUtility.EditPrefabContentsScope`로 제거.

**결론**: Inspector 값 변경 → **Play 모드 진입 시** 반영. 이것이 런타임 생성 오브젝트의 표준 워크플로우.

#### ③ MAX 8 → 20 확장
오너 요청. POSITIONS/SIZE_RATIOS/ALPHAS 배열을 8 → 20개로 확장.
5행 패턴: 하단 진함+큰 → 상단 흐릿+작음 그라데이션.

### 최종 스펙
→ `Docs/specs/SPEC-010_WaterDropDecor_명세서_20260310.md` 참조

---

## 주요 커밋

| 커밋 | 내용 |
|------|------|
| `974cde3` | WaterDropDecor 최초 구현 |
| `b9aaa56` | 우 하단 밀집 패턴 + sibling index 1 |
| `19a0444` | ExecuteAlways 제거 + 프리팹 복구 |
| `dfecc7d` | MAX 8→20, 5행 패턴 확장 |
