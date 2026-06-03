# 캐릭터 정보창 거리별 순위 스크롤 통합 동기화 (2026-06-03)

> SPEC-028과 무관한 독립 핫픽스. client·qa 에이전트 검토 완료.

## Context
캐릭터 정보 팝업의 단/중/장거리 순위 좌우 스크롤 3개가 독립 동작. 오너 요구: **①하나라도 화면 넘으면 3개 함께 ②이동 픽셀 동일**.
현재 `CharacterInfoPopup.LateUpdate`(708~755)는 normalizedPosition 동기화 → ㉠짧은 콘텐츠는 Elastic이라 0복원되어 안 따라옴 ㉡normalized는 폭 비율이라 픽셀 이동 제각각.

## 오너 결정
- **짧은 거리 처리**: 가장 긴 거리 기준 공통 clamp → **빈 공간 허용** (이동 픽셀 완전 동일 우선)

## 변경 파일
**1. `Assets/Prefabs/UI/BettingPanel.prefab`** — 3개 `RanksScroll` movementType **2(Elastic)→0(Unrestricted)** (라인 613/3384/6483).

**2. `Assets/Scripts/Manager/UI/CharacterInfoPopup.cs`**
- LateUpdate(708~755) 가로 동기화를 `content.anchoredPosition.x` 절대값으로 교체 (세로 블록 제거 — vertical=0)
- 공통 clamp: `maxScroll = Max(0, Max(3개 content.rect.width) - viewportWidth)`, x ∈ `[-maxScroll, 0]`, 3개 동일
- 마스터 = `_lastX` 대비 변화 감지된 ScrollRect의 content.x
- 필드 `_lastShortH/MidH/LongH/V…` 6개 → `_lastX` 1개 (초기값 0)

## client·qa 검토 반영 (필수 보완)
1. **폭 확정** — `UpdateRecentRecords()` 끝에서 3개 content `LayoutRebuilder.ForceRebuildLayoutImmediate`. LateUpdate에서 `width<=0`이면 skip
2. **변화감지 임계** — normalized `0.0005f` → 픽셀 `0.5f`
3. **`_lastX`** — 재조회 대신 clampedX 저장, 초기값 0
4. **스크롤 리셋** — `Show()`/`UpdateRecentRecords()`에서 3개 content.x=0 + `_lastX=0`

## 검증
- C1 3개 다 김 → 한 행 드래그 시 3개 동일 픽셀
- C2/C4 폭 다름(단3·장10) → 같은 순번 글자 x좌표 일치 (회귀 게이트)
- 캐릭터 전환 → 새 콘텐츠 좌측 정렬 리셋
- 콘솔 에러 0 / 세로 스크롤·레이더·스킬 회귀 없음

## 롤백
- 완전 폐기: `git reset --hard before-charinfo-scroll-sync`
- 되돌리기: `git revert HEAD --no-edit`
