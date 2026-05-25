# TrackInfoPanel RectTransform 정상화 (2026-05-25)

## Context
BettingPanel.prefab의 TrackInfoPanel이 anchor 비정상 값(sizeDelta.x 음수, 큰 anchoredPosition)으로 Prefab Editor 시각과 Play 모드 위치가 어긋남. 우측 중앙 단일 앤커로 정리하면서 Play 모드 시각 위치 유지.

## 변환 값 (옵션 1 — 우측 중앙 단일 앤커)

| 속성 | 현재 | 정상화 후 |
|------|------|---------|
| anchorMin | (0.01, 0.01) | **(1, 0.5)** |
| anchorMax | (0.75, 0.11) | **(1, 0.5)** |
| pivot | (0.5, 0.5) | **(1, 0.5)** |
| anchoredPosition | (984, 482) | **(-25, 7)** |
| sizeDelta | (-1057, 327) | **(364, 435)** ← 양수 |

→ 화면(1920×1080) 우측 약 1531~1895 / 상하 329~764 영역 유지 (Play 모드 시각 동일).

## 안전 검증 ✓
- Canvas reference resolution: 1920×1080 확인
- 코드에서 TrackInfoPanel RectTransform 런타임 조작 **없음**
- TrackInfoToggleBtn(`>>`)은 `SetActive` + `Image.enabled`만 사용 → RectTransform 안 건드림
- 자식 6개(ToggleBtn/TotalRoundLabel/TrackNameLabel/DistanceLabel/TrackTypeLabel/TrackDescLabel)는 자체 anchor → 부모 anchor 변경 영향 없음

## 작업 순서
1. 롤백 태그 `before-trackinfo-rectnormalize` 부착 (HEAD `a0f7c46`)
2. Editor 일회성 스크립트로 TrackInfoPanel RectTransform 변환 (자식 안 건드림)
3. 임시 스크립트 삭제
4. atomic commit (push 보류)
5. Play 모드 시각 확인 → 오너 컨펌 → push 또는 롤백

## 롤백 명령
| 시나리오 | 명령 |
|---------|------|
| 완전 폐기 | `git reset --hard before-trackinfo-rectnormalize` |
| 되돌리기 커밋 | `git revert HEAD --no-edit` |
| 컨펌 후 정리 | `git tag -d before-trackinfo-rectnormalize` |

## 영향 파일
- `Assets/Prefabs/UI/BettingPanel.prefab` (TrackInfoPanel RectTransform만 변경)
- `Docs/plans/trackinfo_rect_cleanup_20260525.md` (본 문서, 신규)
