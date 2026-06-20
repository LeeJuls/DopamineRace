# his045 — ExchangeModal 배경 이미지 적용 + CurrencyUIPrefabCreator 경로 수정

**날짜**: 2026-06-20
**관련 SPEC**: SPEC-040
**소요**: 1세션

## 작업 배경

오너 요청: ExchangeModal 팝업 배경을 BettingPanel과 동일한 `Img_BG_ListSlot_BG_01~04`
이미지로 통일. 이전 세션(his044)에서 에디터 메뉴 재정리 시 `CurrencyUIPrefabCreator.cs`가
`Assets/Editor/` 위치에 있어 누락됐던 건도 함께 처리.

## 작업 흐름

### 1. 플랜 작성 + client·qa 에이전트 피드백

client 에이전트 지적:
- `Resources.Load` 대신 `AssetDatabase.LoadAssetAtPath` 사용 (Editor 스크립트)
- 서브스프라이트 border override `{0,0,0,0}` 가능성 (Sliced 무효화 리스크)
- `raycastTarget = false` 명시 필요

qa 에이전트 지적:
- 변수명 `panel` 유지 (`modal`로 혼용 시 NRE)
- `Debug.LogWarning` 추가 (sprite null 시 폴백 명시)
- 검증 항목 보강

### 2. CurrencyUIPrefabCreator.cs 수정

`BuildExchangeModalPrefab()` 내 단색 Image 설정 교체:

```csharp
// 기존
panel.AddComponent<Image>().color = new Color(0.12f, 0.10f, 0.18f, 0.95f);

// 변경
Sprite bgSprite = AssetDatabase.LoadAssetAtPath<Sprite>(
    "Assets/Resources/UI/Img_BG_ListSlot_BG_01.png");
Image panelImg = panel.AddComponent<Image>();
panelImg.raycastTarget = false;
if (bgSprite != null)
{
    panelImg.sprite     = bgSprite;
    panelImg.type       = Image.Type.Sliced;
    panelImg.fillCenter = true;
    panelImg.color      = Color.white;
}
else
{
    Debug.LogWarning("[ExchangeModal] Img_BG_ListSlot_BG_01 없음 — 단색 폴백");
    panelImg.color = new Color(0.12f, 0.10f, 0.18f, 0.95f);
}
```

### 3. 경로 버그 발견 및 수정

`CreateAll()`을 MCP로 실행했더니 `Assets/Resources/Prefabs/UI/`에 생성.
git 이력 확인: commit `fcf460a`(SPEC-028 폴더 통일)에서 prefab은 `Prefabs/UI/`로 이동했으나
Creator 스크립트의 저장 경로가 미반영. → 4개 경로 + EnsureFolder + 헤더 주석 수정.

동시에 MenuItem도 `프리팹 생성/Currency UI Prefabs`로 통일 (his044 누락분).

### 4. 재생성 및 검증 (MCP)

```
[최종검증] sprite=Img_BG_ListSlot_BG_01 | type=1 | raycast=False | color=RGBA(1,1,1,1)
```

## 트러블슈팅

| 증상 | 원인 | 해결 |
|------|------|------|
| 첫 `CreateAll()` 후 sprite=null, 단색 유지 | 스크립트 편집 직후 MCP 실행 → 구 어셈블리 사용 | `ImportAsset` + `Refresh()`로 재컴파일 후 재실행 |
| `Assets/Resources/Prefabs/` 폴더 untracked 생성 | Creator 구 경로 버그 | 경로 수정 + `AssetDatabase.DeleteAsset`으로 삭제 |
| MCP 검증 스크립트에서 `UnityEngine.UI.Image` not found | Roslyn 스크립트 UnityEngine.UI 어셈블리 미포함 | `SerializedObject`로 접근 (reflection 우회) |

## 결과

- ExchangeModal 팝업 배경: 단색 보라 → BG_01 텍스처 (Sliced)
- CurrencyUIPrefabCreator: 저장 경로 `Prefabs/UI/`로 정정, 메뉴 `프리팹 생성/` 통합 완료
