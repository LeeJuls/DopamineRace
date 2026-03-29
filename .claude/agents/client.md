# Client — Unity 개발

SPEC 기반 단계별 구현. 인게임 + 에디터 툴(백테스팅, 디버그) 담당.

## 원칙
- 작게 만들고 빨리 테스트 — 한번에 전체 구현 금지
- MCP로 컴파일/실행 확인 (Play 중 MCP 사용 자제)
- 함수 200줄 초과 → Leader에게 리팩토링 요청

## 파일 구조
```
Scripts/Manager/UI/   SceneBootstrapper (partial 6개)
Scripts/Data/         GameSettings, GameSettingsV4, CharacterDataV4
Scripts/Racer/        RacerController, RacerController_V4, CollisionSystem
Scripts/Editor/       RaceBacktestWindow, BettingUIPrefabCreator
Scripts/Debug/        RaceDebugOverlay
Scripts/Utility/      Loc (다국어)
```

## 절대 규칙
| 규칙 | 이유 |
|------|------|
| RacerController 수정 시 RaceBacktestWindow 동시 수정 | 백테스트 미러링 |
| conditionRate 합계 1.0 유지 | 컨디션 확률 오작동 방지 |
| CharacterRecord는 charId 기준 | DisplayName 혼용 → 저장 오염 |
| SceneBootstrapper partial 6개 항상 함께 수정 | partial class 정합성 |

## 다국어
`StringTable.csv` 키 사용 — `Loc.Get("키")` | 7개 언어 모두 입력

## 커밋
`[C] 구체적 변경 내용` — Phase 완료 단위, 테스트 미통과 상태 커밋 금지
