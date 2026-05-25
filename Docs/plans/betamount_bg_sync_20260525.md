# BetAmountModal 배경을 CharacterInfoPopup과 동일하게 (2026-05-25)

## Context
BetAmountModal의 배경(Modal 자식 Image)을 CharacterInfoPopup 루트와 동일한 9-slice 프레임 스프라이트로 통일해 UI 일관성 확보.

## 변경 대상
**`Assets/Prefabs/UI/BetAmountModalPrefab.prefab`의 `Modal` 자식 Image 속성만 수정** — 자식 콘텐츠/위치/크기는 그대로.

| 속성 | 현재 | 변경 후 |
|------|------|---------|
| sprite | 없음 | **GUID `70175cf37e799084daf88c19d04740e5`** (CharacterInfoPopup 루트와 동일) |
| Image Type | 0 (Simple) | **1 (9-Slice)** |
| color | (0.12, 0.1, 0.18, 0.95) 어두운 보라 | **(1, 1, 1, 1) 흰색** |
| raycastTarget | 1 | **1 (유지)** — 모달 입력 |

## 안전 검증
- `BetAmountModal.cs`는 Image 시각 속성(sprite/color/type) 동적 조작 없음
- raycastTarget 1 유지 → 모달 입력 정상 동작
- 자식 콘텐츠 영향 0

## 작업 순서
1. 롤백 태그 `before-betamount-bg-sync` 부착 (HEAD `a08ee3d`)
2. Editor MCP 스크립트로 Modal Image 4개 속성 변경
3. atomic commit (push 보류)
4. Play 모드 시각 확인 → 컨펌 OK → push, NG → 롤백

## 롤백 명령
| 시나리오 | 명령 |
|---------|------|
| 완전 폐기 | `git reset --hard before-betamount-bg-sync` |
| 되돌리기 커밋 | `git revert HEAD --no-edit` |
| 컨펌 후 정리 | `git tag -d before-betamount-bg-sync` |

## 영향 파일
- `Assets/Prefabs/UI/BetAmountModalPrefab.prefab` (Modal 자식 Image만)
- `Docs/plans/betamount_bg_sync_20260525.md` (본 문서)
