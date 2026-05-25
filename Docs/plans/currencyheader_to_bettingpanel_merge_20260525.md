# CurrencyHeader를 BettingPanel에 통합 (2026-05-25)

## Context
`CurrencyHeaderPrefab`이 단일 사용처(`BuildCurrencyHeader(bettingUI.transform)` @ `SceneBootstrapper.cs:353`)에서만 인스턴스화되는데 별도 프리팹으로 존재. 현재 `bettingUI`의 직속 자식(BettingPanel의 형제)로 떠 있어 UC9 명세("BettingPanel TopArea 총점 영역을 통화 표시로 교체")와 어긋남.

다른 화면 재사용 계획 없음 (Phase 3 Finish는 스톤 총합 별도 UI, Phase 4 닉네임/Phase 5 랭킹은 독자 UI). 직전 ExchangeIcon 통합과 동일한 단순화 방향.

## 변경 대상 파일

| 파일 | 변경 |
|------|------|
| `Assets/Prefabs/UI/BettingPanel.prefab` | CurrencyHeader 자식 객체 추가 (기존 프리팹 구조 그대로 복제) |
| `Assets/Prefabs/UI/CurrencyHeaderPrefab.prefab` | 삭제 (+ .meta) |
| `Assets/Scripts/Data/GameSettings.cs` | `currencyHeaderPrefab` 직렬화 필드 제거 |
| `Assets/Resources/GameSettings.asset` | 해당 필드 직렬화에서 제거 |
| `Assets/Scripts/Manager/UI/SceneBootstrapper.Spec028UI.cs` | `BuildCurrencyHeader`: 재귀 검색 방식으로 변경 |

## 구조 (Before / After)

**Before:**
```
bettingUI
  ├─ BettingPanel
  │   └─ TopArea / OddsArea / MyPointLabel / ...
  └─ CurrencyHeader (Instantiate된 CurrencyHeaderPrefab)
       └─ JellyContainer / StoneContainer / ...
```

**After:**
```
bettingUI
  └─ BettingPanel
       └─ TopArea
            └─ OddsArea
                 └─ CurrencyHeader (자식으로 통합)
                      └─ JellyContainer / StoneContainer / ...
```

## 세부 작업
1. **Editor 일회성 스크립트**: 기존 CurrencyHeaderPrefab → BettingPanel/TopArea/OddsArea 자식으로 Instantiate
2. **CurrencyHeaderPrefab.prefab 삭제**
3. **GameSettings.cs**: `currencyHeaderPrefab` 필드 제거
4. **GameSettings.asset**: SaveAssets로 직렬화 갱신
5. **SceneBootstrapper.Spec028UI.cs**: `BuildCurrencyHeader`를 `GetComponentsInChildren` 재귀 검색 패턴으로 (ExchangeIcon과 동일)

## 롤백 전략
- **작업 전**: `git tag before-currencyheader-merge` (현재 HEAD `9157190`)
- **작업**: 단일 atomic commit, push 보류
- **롤백 옵션**:
  - 완전 폐기: `git reset --hard before-currencyheader-merge`
  - 되돌리기 커밋: `git revert HEAD --no-edit`
- **사후 정리**: 컨펌 시 `git tag -d before-currencyheader-merge`

## 검증
1. Unity 콘솔 에러 0건
2. 컴파일 OK
3. BettingPanel 프리팹에 CurrencyHeader 자식 존재
4. CurrencyHeaderPrefab.prefab 삭제됨
5. Play 모드: 베팅 화면에 🟦 100 / 💎 0 표시 (BettingPanel 내부 위치)
6. WalletManager 변경 → 라벨 즉시 갱신
