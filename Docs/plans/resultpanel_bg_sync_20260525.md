# ResultPanel/RankSection 배경을 CharacterInfoPopup과 동일하게 (2026-05-25)

## Context
ResultPanel의 메인 결과 박스(RankSection)를 CharacterInfoPopup 루트와 동일한 9-slice 프레임 스프라이트로 통일해 팝업 UI 일관성 확보.

## 변경 대상
**`Assets/Prefabs/UI/ResultPanel.prefab`의 `RankSection` 자식 Image 속성만 수정**

| 속성 | 현재 | 변경 후 |
|------|------|---------|
| sprite | 없음 | **GUID `70175cf37e799084daf88c19d04740e5`** (Img_BG_ListSlot_BG_01) |
| Image Type | 0 (Simple) | **1 (9-Slice)** |
| color | (1, 1, 1, 0.06) 거의 투명 | **(1, 1, 1, 1) 흰색** |

→ 메인 순위 박스(1530×856)만 9-slice 프레임으로 덮임. 루트의 검은 반투명 백드롭은 그대로 유지. ScoreSection/BetResultSection 그대로.

## 안전 검증
- ResultPanel 표시 코드는 RankSection Image 속성 동적 조작 없음
- 자식 콘텐츠(Rank1Row~Rank9Row) 영향 0

## 작업 순서
1. 롤백 세이브 포인트 `before-resultpanel-bg-sync` 부착 (HEAD `e1d19f2`)
2. Editor MCP 스크립트로 RankSection Image 3개 속성 변경
3. atomic commit (push 보류)
4. Play 모드 확인 → 컨펌 OK → push, NG → 롤백

## 롤백 명령
| 시나리오 | 명령 |
|---------|------|
| 완전 폐기 | `git reset --hard before-resultpanel-bg-sync` |
| 되돌리기 커밋 | `git revert HEAD --no-edit` |
| 컨펌 후 정리 | `git tag -d before-resultpanel-bg-sync` |

## 영향 파일
- `Assets/Prefabs/UI/ResultPanel.prefab` (RankSection 자식 Image만)
- `Docs/plans/resultpanel_bg_sync_20260525.md` (본 문서)
