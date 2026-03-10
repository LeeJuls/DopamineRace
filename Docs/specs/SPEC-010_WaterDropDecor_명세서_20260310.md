# SPEC-010: WaterDropDecor — CharacterItem 물방울 장식

**작성일**: 2026-03-10
**상태**: 적용 완료
**관련 파일**: `Assets/Scripts/Manager/UI/WaterDropDecor.cs`

---

## 1. 개요

CharacterItem 패널 우 하단에 반투명 원형 물방울을 정적으로 배치하는 장식 컴포넌트.
애니메이션 없음, 순수 비주얼 데코레이션.

```
┌──────────────────────────────┐
│ [아이콘]  이름  전적         │
│                   · · ·      │  5행 (tiny, 흐릿)
│                 · · · ·      │  4행 (small)
│                ● ● ● ●       │  3행 (medium)
│               ● ● ● ●        │  2행 (medium+)
│             ● ● ● ● ●        │  1행 (large, 진함)
└──────────────────────────────┘
```

---

## 2. Inspector 설정값

| 필드 | 타입 | 기본값 | 설명 |
|------|------|--------|------|
| `dropCount` | int | 7 | 표시할 방울 수 (1~20) |
| `dropColor` | Color | (0.45, 0.82, 1.0, 1.0) 하늘색 | 기본 RGB (알파는 ALPHAS 배열로 개별 조정) |
| `sizeMin` | float | 4px | 가장 작은 방울 크기 |
| `sizeMax` | float | 10px | 가장 큰 방울 크기 |

> **변경 적용 시점**: Inspector에서 값 수정 후 **Play 모드 진입**으로 반영.

---

## 3. 배치 패턴 (20개, 정규화 좌표)

| 행 | 개수 | Y 범위 | X 범위 | 특징 |
|----|------|--------|--------|------|
| 1행 | 5 | 0.09~0.12 | 0.65~0.93 | 가장 크고 진함 |
| 2행 | 4 | 0.25~0.28 | 0.68~0.89 | 중간-큰 |
| 3행 | 4 | 0.40~0.43 | 0.71~0.92 | 중간 |
| 4행 | 4 | 0.54~0.57 | 0.69~0.90 | 작음 |
| 5행 | 3 | 0.66~0.69 | 0.74~0.88 | 아주 작고 흐릿 |

### dropCount별 표시 구성
- `1~5`: 1행만 (하단 밀집)
- `6~9`: 1행 + 2행
- `10~13`: 1~3행
- `14~17`: 1~4행
- `18~20`: 전체 5행

---

## 4. SIZE_RATIOS (크기 비율, 0~1)

```
1행: 0.90, 0.80, 0.85, 0.78, 0.83
2행: 0.65, 0.58, 0.62, 0.55
3행: 0.48, 0.42, 0.45, 0.40
4행: 0.34, 0.29, 0.32, 0.27
5행: 0.22, 0.18, 0.20
```
실제 크기 = `Lerp(sizeMin, sizeMax, SIZE_RATIO)` → 4px~10px 범위

---

## 5. ALPHAS (투명도)

```
1행: 0.38, 0.28, 0.35, 0.26, 0.32
2행: 0.22, 0.17, 0.20, 0.15
3행: 0.13, 0.10, 0.12, 0.09
4행: 0.08, 0.06, 0.07, 0.05
5행: 0.05, 0.04, 0.04
```

---

## 6. 스프라이트

- `Resources/VFX/vfx_circle.png` — 원형 스프라이트 (기존 CollisionVFX 공용)
- `Resources.Load<Sprite>("VFX/vfx_circle")`으로 로드
- null 안전: 스프라이트 없을 시 Unity 기본 흰 사각형으로 대체

---

## 7. 렌더링 구조

```
CharacterItem (root)
  Image (배경 스프라이트, 항상 맨 뒤)
  └─ [0] IconContainer
  └─ [1] WaterDropContainer   ← Awake()에서 동적 생성, SetSiblingIndex(1)
        └─ Drop_0 ~ Drop_N
  └─ [2] ConditionIcon
  └─ [3] PopularityLabel
  └─ ...
```

**렌더 순서**: 배경 Image → WaterDropContainer (물방울) → Icon/Label (전부 물방울 위)

---

## 8. 퍼포먼스

- **드로우콜**: 같은 스프라이트 + 같은 머티리얼 → Canvas가 1 드로우콜로 배칭
- **CPU**: `Update()` 없음 → 생성 시(Awake) 1회 비용만 발생
- **메모리**: GameObject + RectTransform + Image × N (수 KB 수준, 무시 가능)
- 20개까지 퍼포먼스 영향 없음. 이론상 수백 개도 무방.

---

## 9. 주의사항

### ❌ [ExecuteAlways] + OnValidate 사용 금지
프리팹 에디터에서 실시간 미리보기를 위해 시도했으나 Unity가 차단:
```
Setting the parent of a transform which resides in a Prefab Asset is disabled
```
Prefab Asset을 `EditPrefabContentsScope` 없이 직접 수정 불가.
→ Awake()-only 방식 유지. Inspector 변경은 Play 모드 진입 후 확인.

### ✅ 프리팹에 WaterDropContainer 저장 금지
WaterDropContainer는 런타임에 Awake()가 생성하므로 프리팹 파일에 포함되면 안 됨.
(포함될 경우 Play 진입 시 중복 생성)

---

## 10. 관련 문서

- 히스토리: `Docs/history/CharacterItemUI_스프라이트적용_WaterDropDecor_히스토리_20260310.md`
- CollisionVFX 스프라이트: `Docs/history/VFX재설계_결과UI_순위표시개선_히스토리_20260306.md`
