# his046 — TrackInfoPanel 국소 수정 & 물방울 장식 (2026-06-21)

## 작업 요약

TrackInfoPanel(배팅 화면 하단 트랙 정보 패널)에 WaterDropDecor 효과와 흰 배경을 추가.

---

## ⚠️ 실패 경로 — 전체 프리팹 재생성은 금지

### 문제

처음에 `BettingUIPrefabCreator.CreatePrefabs()` (메뉴: `DopamineRace > Create Betting UI Prefabs`)로 BettingPanel.prefab + CharacterItem.prefab 전체를 재생성했더니 **배팅 UI 전체가 깨졌다**.

- 캐릭터 목록 행마다 "PopularityLabel", "OddsLabel" 플레이스홀더 텍스트 노출
- 탭 버튼 레이아웃 붕괴
- TrackInfoPanel 텍스트 겹침

### 근본 원인

`BettingUIPrefabCreator.CreatePrefabs()`는 **프리팹 전체를 스크래치에서 재생성**한다.  
코드에 반영되지 않은 Inspector 수동 설정값이 모두 날아가고,  
`CharacterItemUI.cs`의 `[SerializeField]` 레퍼런스가 끊어져 런타임 데이터 바인딩 실패.

### 규칙

> **`CreatePrefabs()` 는 최초 생성 전용. 이미 존재하는 프리팹을 수정할 때는 절대 사용하지 말 것.**

---

## ✅ 올바른 패턴 — 국소 수정 에디터 스크립트

기존 프리팹을 **그대로 유지**하면서 특정 노드만 수정하는 전용 에디터 스크립트를 새로 만든다.

### 핵심 API

```csharp
// 1. 프리팹 내용 로드 (씬에 올리지 않음)
GameObject prefabRoot = PrefabUtility.LoadPrefabContents("Assets/Prefabs/UI/BettingPanel.prefab");

// 2. 원하는 노드 탐색
Transform target = FindDeep(prefabRoot.transform, "TrackInfoPanel");

// 3. 컴포넌트 수정 / 추가
target.GetComponent<Image>().color = Color.white;
WaterDropDecor decor = target.gameObject.AddComponent<WaterDropDecor>();
var so = new SerializedObject(decor);
so.FindProperty("dropCount").intValue = 8;
so.ApplyModifiedProperties();

// 4. 저장 후 언로드
PrefabUtility.SaveAsPrefabAsset(prefabRoot, path);
PrefabUtility.UnloadPrefabContents(prefabRoot);
```

> `[SerializeField] private` 필드는 반드시 `SerializedObject` + `FindProperty` 로 설정해야 저장된다.  
> 직접 프로퍼티 대입(`decor.dropCount = 8`)은 프리팹에 저장되지 않는다.

### 재귀 탐색 헬퍼

```csharp
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

### 이번 작업 결과물

`Assets/Scripts/Editor/TrackInfoPanelDecorator.cs`  
메뉴: `DopamineRace > Decorate TrackInfoPanel (물방울+흰배경)`

- TrackInfoPanel 배경: `Color.white`
- 텍스트 6종 → 어두운 계열 색상
- WaterDropDecor 컴포넌트 추가 (dropCount=8, sizeMin=5, sizeMax=12)
- CharacterItem / BetAmountModal / ExchangeModal 무변경

---

## WaterDropDecor + HorizontalLayoutGroup 문제

### 문제

WaterDropDecor를 HLG가 있는 패널에 붙이면 `WaterDropContainer`(자식 GO)가 HLG 레이아웃 대상에 포함되어 레이아웃을 밀어냄.

### 수정

`WaterDropDecor.cs` > `BuildDrops()` 내 container 생성 직후에 `LayoutElement.ignoreLayout = true` 추가:

```csharp
LayoutElement le = container.AddComponent<LayoutElement>();
le.ignoreLayout = true;
```

- CharacterItem에도 적용되지만 영향 없음 (WaterDropContainer는 원래 오버레이 역할)

---

## 새 UI 패널에 WaterDropDecor 붙이는 체크리스트

1. `WaterDropDecor` 컴포넌트를 대상 패널 GO에 추가
2. `dropCount`, `sizeMin`, `sizeMax` 파라미터 설정 (`SerializedObject` 사용)
3. 부모에 HLG/VLG 있으면 → `WaterDropDecor.cs`에 `LayoutElement.ignoreLayout` 확인 (현재 반영됨)
4. 흰/밝은 배경 사용 시 → 텍스트 색 동시에 어두운 계열로 변경

---

## 새 국소 수정 스크립트 작성 템플릿

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

    // ── 여기서 수정 ──
    // target.GetComponent<Image>().color = ...;
    // MyComponent comp = target.gameObject.AddComponent<MyComponent>();
    // var so = new SerializedObject(comp);
    // so.FindProperty("fieldName").intValue = 42;
    // so.ApplyModifiedProperties();

    PrefabUtility.SaveAsPrefabAsset(root, path);
    PrefabUtility.UnloadPrefabContents(root);
    Debug.Log($"[Patch] {path} 저장 완료");
}
```

---

## 커밋 히스토리

| 해시 | 내용 |
|------|------|
| `3e80530` | (실패) CreatePrefabs() 전체 재생성 시도 |
| `07b5811` | 롤백 — Revert |
| `688792a` | TrackInfoPanelDecorator.cs 신규 + WaterDropDecor HLG 호환 |
| `ea4eb21` | Decorate 메뉴 실행 결과 BettingPanel.prefab 반영 |
