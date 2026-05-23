# 프리팹 폴더 정리 계획 (2026-05-23)

## 문제
SPEC-028 Phase 2 작업 시 신규 프리팹 4개를 `Assets/Resources/Prefabs/UI/`에 생성.
기존 프리팹 7개는 `Assets/Prefabs/UI/`에 있어 폴더가 두 곳으로 분리된 상태.

## 목표
신규 4개 프리팹을 기존 폴더로 이동 + 로딩 방식을 `GameSettings` 직렬화 필드 방식으로 통일.

## 변경 파일

| 파일 | 변경 내용 |
|------|---------|
| `Assets/Prefabs/UI/` | 프리팹 4개 이동 (AssetDatabase.MoveAsset) |
| `Assets/Scripts/Data/GameSettings.cs` | 직렬화 필드 4개 추가 |
| `Assets/Scripts/Manager/UI/SceneBootstrapper.Spec028UI.cs` | `Resources.Load()` → `GameSettings.Instance.xxxPrefab` |
| `Assets/Resources/Prefabs/` | 이동 후 빈 폴더 삭제 |

## Step 1 — 프리팹 4개 이동
```
Assets/Resources/Prefabs/UI/BetAmountModalPrefab.prefab  →  Assets/Prefabs/UI/
Assets/Resources/Prefabs/UI/CurrencyHeaderPrefab.prefab  →  Assets/Prefabs/UI/
Assets/Resources/Prefabs/UI/ExchangeIconPrefab.prefab    →  Assets/Prefabs/UI/
Assets/Resources/Prefabs/UI/ExchangeModalPrefab.prefab   →  Assets/Prefabs/UI/
```
이동 후 `Assets/Resources/Prefabs/` 빈 폴더 삭제.

## Step 2 — GameSettings.cs 필드 추가
기존 `[Header("═══ UI 프리팹 ═══")]` 블록 하단에 추가:
```csharp
// SPEC-028 Phase 2 — 통화·베팅 모달
public GameObject currencyHeaderPrefab;
public GameObject betAmountModalPrefab;
public GameObject exchangeIconPrefab;
public GameObject exchangeModalPrefab;
```

## Step 3 — SceneBootstrapper.Spec028UI.cs 로딩 방식 변경
4개 메서드에서 `Resources.Load<GameObject>("Prefabs/UI/XxxPrefab")` 제거,
`GameSettings.Instance.xxxPrefab`으로 교체. Fallback 로직은 그대로 유지.

## Step 4 — GameSettings.asset Inspector 자동 연결
MCP C# 스크립트로 4개 필드를 자동 연결 후 `EditorUtility.SetDirty` + `SaveAssets`.

## 검증
1. 콘솔 에러 0건
2. `Assets/Prefabs/UI/` 에 프리팹 11개 존재 (기존 7 + 신규 4)
3. `Assets/Resources/Prefabs/` 폴더 없음
4. GameSettings Inspector에서 4개 필드 연결 확인
5. Play 모드 → 베팅 화면 정상 표시
