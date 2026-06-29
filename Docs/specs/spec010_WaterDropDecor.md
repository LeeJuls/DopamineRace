# SPEC-010: WaterDropDecor — 물방울 장식 컴포넌트 가이드

**최초 작성**: 2026-03-10 | **최종 갱신**: 2026-06-27  
**상태**: ✅ 적용 완료  
**파일**: `Assets/Scripts/Manager/UI/WaterDropDecor.cs`

---

## 1. 한 줄 요약

UI 패널 우 하단에 반투명 원형 물방울을 정적으로 배치하는 순수 장식 컴포넌트.  
애니메이션 없음. `Update()` 없음. `Awake()`에서 1회 생성 후 정지.

```
┌──────────────────────────────┐
│ [아이콘]  이름  전적         │
│                   · · ·      │  5행 (tiny, 흐릿)
│                 · · · ·      │  4행
│                ● ● ● ●       │  3행
│               ● ● ● ●        │  2행
│             ● ● ● ● ●        │  1행 (large, 진함)
└──────────────────────────────┘
```

---

## 2. Inspector 파라미터

| 필드 | 타입 | 기본값 | 설명 |
|------|------|--------|------|
| `dropCount` | int | 7 | 표시할 방울 수 (1~20) |
| `dropColor` | Color | 하늘색 (0.45, 0.82, 1.0, 1.0) | 기본 RGB (알파는 ALPHAS 배열로 개별 조정) |
| `sizeMin` | float | 4px | 가장 작은 방울 크기 |
| `sizeMax` | float | 10px | 가장 큰 방울 크기 |

> Inspector에서 값 수정 후 **Play 모드 진입** 시 반영. 에디터 실시간 미리보기 불가 (§5 참고).

---

## 3. 배치 패턴

### 행별 구성 (최대 20개, 정규화 좌표)

| 행 | 개수 | Y 범위 | X 범위 | 특징 |
|----|------|--------|--------|------|
| 1행 | 5 | 0.09~0.12 | 0.65~0.93 | 가장 크고 진함 |
| 2행 | 4 | 0.25~0.28 | 0.68~0.89 | 중간-큰 |
| 3행 | 4 | 0.40~0.43 | 0.71~0.92 | 중간 |
| 4행 | 4 | 0.54~0.57 | 0.69~0.90 | 작음 |
| 5행 | 3 | 0.66~0.69 | 0.74~0.88 | 아주 작고 흐릿 |

### dropCount별 표시 행

| dropCount | 표시 행 |
|-----------|--------|
| 1~5 | 1행만 |
| 6~9 | 1~2행 |
| 10~13 | 1~3행 |
| 14~17 | 1~4행 |
| 18~20 | 전체 5행 |

### SIZE_RATIOS (크기, 0~1)

```
1행: 0.90, 0.80, 0.85, 0.78, 0.83
2행: 0.65, 0.58, 0.62, 0.55
3행: 0.48, 0.42, 0.45, 0.40
4행: 0.34, 0.29, 0.32, 0.27
5행: 0.22, 0.18, 0.20
```

실제 크기 = `Lerp(sizeMin, sizeMax, SIZE_RATIO)`

### ALPHAS (투명도)

```
1행: 0.38, 0.28, 0.35, 0.26, 0.32
2행: 0.22, 0.17, 0.20, 0.15
3행: 0.13, 0.10, 0.12, 0.09
4행: 0.08, 0.06, 0.07, 0.05
5행: 0.05, 0.04, 0.04
```

---

## 4. 렌더링 구조

```
Panel (root)
  Image (배경, 항상 맨 뒤)
  └─ [0] 기존 자식들
  └─ [1] WaterDropContainer   ← Awake()에서 동적 생성, SetSiblingIndex(1)
        └─ Drop_0 ~ Drop_N    ← vfx_circle.png 원형 스프라이트
  └─ [2] Icon/Label 등        ← 물방울 위에 렌더링
```

- **스프라이트**: `Resources/VFX/vfx_circle.png` (CollisionVFX와 공용)
- **드로우콜**: 같은 스프라이트+머티리얼 → Canvas가 1콜로 배칭
- null 안전: 스프라이트 없으면 Unity 기본 흰 사각형으로 대체

---

## 5. 새 패널에 붙이는 법

### 체크리스트

1. **에디터 스크립트**로 국소 수정 (아래 §6 패턴 사용)
2. `WaterDropDecor` 컴포넌트를 대상 패널 GO에 추가
3. `dropCount`, `sizeMin`, `sizeMax`는 반드시 `SerializedObject`로 설정
4. 부모에 `HorizontalLayoutGroup` / `VerticalLayoutGroup` 있으면 → `LayoutElement.ignoreLayout = true` 확인 (현재 `WaterDropDecor.cs`에 반영됨)
5. 밝은 배경 사용 시 → 텍스트 색을 동시에 어두운 계열로 변경

### PrefabUtility 국소 수정 패턴 (권장)

```csharp
[MenuItem("DopamineRace/내 메뉴")]
public static void MyPrefabPatch()
{
    const string path = "Assets/Prefabs/UI/XXX.prefab";
    GameObject root = PrefabUtility.LoadPrefabContents(path);
    if (root == null) { Debug.LogError("로드 실패"); return; }

    Transform target = FindDeep(root.transform, "TargetNodeName");
    if (target == null)
    {
        Debug.LogError("노드 없음");
        PrefabUtility.UnloadPrefabContents(root);
        return;
    }

    // 배경색 변경
    target.GetComponent<Image>().color = Color.white;

    // WaterDropDecor 추가 — 직접 대입(=) 금지, SerializedObject 필수
    WaterDropDecor decor = target.gameObject.AddComponent<WaterDropDecor>();
    var so = new SerializedObject(decor);
    so.FindProperty("dropCount").intValue = 8;
    so.FindProperty("sizeMin").floatValue = 5f;
    so.FindProperty("sizeMax").floatValue = 12f;
    so.ApplyModifiedProperties();

    PrefabUtility.SaveAsPrefabAsset(root, path);
    PrefabUtility.UnloadPrefabContents(root);
    Debug.Log($"[Patch] {path} 저장 완료");
}

private static Transform FindDeep(Transform parent, string name)
{
    if (parent.name == name) return parent;
    foreach (Transform child in parent)
    {
        Transform found = FindDeep(child, name);
        if (found != null) return found;
    }
    return null;
}
```

---

## 6. 적용 사례

| 패널 | dropCount | sizeMin | sizeMax | 에디터 스크립트 |
|------|-----------|---------|---------|----------------|
| `CharacterItem.prefab` | 7 | 4 | 10 | (his026 때 직접 적용) |
| `TrackInfoPanel` (BettingPanel 내) | 8 | 5 | 12 | `TrackInfoPanelDecorator.cs` → `DopamineRace > Decorate TrackInfoPanel` |

---

## 7. 금지 사항 & 이유

### ❌ `CreatePrefabs()` 재실행 금지

`DopamineRace > Create Betting UI Prefabs`는 **최초 생성 전용**.  
기존 프리팹에 실행하면 Inspector 수동 설정이 전부 초기화되고 SerializeField 레퍼런스가 끊어진다.  
→ **기존 프리팹 수정은 반드시 §5 국소 수정 패턴 사용.**

### ❌ `[ExecuteAlways]` + `OnValidate` 금지

에디터 실시간 미리보기를 위해 도입했으나 Unity가 차단:

```
Setting the parent of a transform which resides in a Prefab Asset is disabled
```

Prefab Asset을 `EditPrefabContentsScope` 없이 직접 수정 불가.  
→ **Awake()-only 방식 유지.** Inspector 변경은 Play 모드 진입 후 확인.

### ❌ WaterDropContainer를 프리팹에 저장 금지

`WaterDropContainer`는 런타임 `Awake()`가 생성한다. 프리팹에 포함되면 Play 진입 시 중복 생성.

### ❌ SerializeField에 직접 대입 금지 (에디터 스크립트에서)

```csharp
decor.dropCount = 8;  // ❌ 프리팹에 저장 안 됨
```

```csharp
so.FindProperty("dropCount").intValue = 8;  // ✅
so.ApplyModifiedProperties();
```

---

## 8. 히스토리

| 문서 | 날짜 | 내용 |
|------|------|------|
| `his026_CharacterItemUI스프라이트WaterDropDecor.md` | 2026-03-10 | 최초 구현·HLG 문제 발견·8→20 확장 |
| `his046_TrackInfoPanel국소수정_물방울장식_20260621.md` | 2026-06-21 | TrackInfoPanel 적용·CreatePrefabs 사고 기록·국소 수정 패턴 정립 |
