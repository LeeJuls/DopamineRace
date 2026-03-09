# 트랙 프로그레스 바 HUD 구현 히스토리

> 날짜: 2026-03-05
> 관련 스펙: SPEC-008
> 커밋 범위: `09e630f` ~ `d9b6599` (총 12커밋)

---

## 작업 개요

레이싱 HUD 좌측 텍스트 순위 목록(RankPanel)을 **세로 트랙 바 + 색깔 원형 마커**로 전면 교체.
우마무스메 스타일의 직관적 레이서 진행도 시각화.

---

## 타임라인

### Phase 1 — 초기 구현 (`09e630f`)
- `BuildTrackProgressBar()` 신규 작성
- TrackProgressPanel, BarBg(흰색 세로선), GoalLabel/StartLabel, RacerCircle×9 생성
- `InitTrackBarForRace()`: 배팅 정보 읽어 포지션 컬러 적용
- `UpdateTrackProgressBar()`: `OverallProgress` 직접 읽어 Y 위치 결정
- **이슈**: Unity 6에 `Knob.psd` 내장 스프라이트가 없어 원형 생성 실패

### Phase 2 — 코드 생성 원형 스프라이트 (`b155d21`)
- `GetBuiltinResource` → 64×64 원형 픽셀 텍스처 코드 생성으로 교체
- `CircleSprite` 프로퍼티: `Texture2D`에 원형 픽셀 마스킹 → `Sprite.Create()`
- 원 정상 표시 확인

### Phase 3 — 이동 부드럽게 1차 (`e60bfca`, `80d68fa`)
- **문제**: `OverallProgress`가 웨이포인트 도달 시점마다 계단식 업데이트
- 시도: `SmoothDamp(0.15s)`, Lerp(12f*dt) → 여전히 "가다 멈추고 가다 멈추고" 현상
- 바 폭 28→40px로 확대

### Phase 4 — 이동 끊김 해결 + 랩 구분선 초기 추가 (`594e8cf`)
- `SmoothProgress` 프로퍼티: 현재 웨이포인트 간 보간으로 연속 진행률 계산
- 랩 구분선 가시성 개선: 4px 폭, 85% 불투명, 노란 색상

### Phase 5 — 벡터 투영 방식 (`081db5d`, `c4dc5f2`)
- **문제**: deviation(경로이탈)이 투영 기준점을 흔들어 떨림 발생
- 해결: 웨이포인트 원본 좌표 기준으로 Dot product 투영
- `ProjectOnSegmentRaw()`: deviation 없는 순수 웨이포인트 투영
- Lerp(25f*dt) 필터 추가 → 떨림 크게 감소
- 랩 구분선 절반 축소

### Phase 6 — 랩 구분선 형태 개선 (`a432468`, `7de9d73`)
- 문제: anchorMin.x=0 ~ anchorMax.x=1로 패널 전체 폭에 걸침
- 고정: anchorMin=anchorMax=Vector2(0.5f, y) + sizeDelta(14, 6)
- 노란빛 14×6px 작은 사각형으로 확정

### Phase 7 — GOAL/START 폰트 + 고점 턱턱 현상 해결 (`9a6be87`)
- GoalLabel/StartLabel 폰트 13→36pt 확대 (컨테이너 100×24→120×40)
- **고점 턱턱 현상 원인**: Clamp01 때문에 웨이포인트 직전 fraction이 수 프레임 멈춤
- 시도: 모노토닉 래칫(`_smoothProgressRatchet = max(prev, new)`) 추가
- 시도: `ProjectOnSegmentRaw` 상한 클램프 제거

### Phase 8 — 누적 이동 거리 방식으로 전면 교체 (`560bac2`) ★ 핵심
- **유저 인사이트**: "캐릭터와 완전히 동일한 이동 메커니즘을 쓰면 됨 — 트랙이 압축될 뿐"
- 모든 투영/래칫/Lerp 제거
- `RacerController.Update()`에 `_cumulativeDistance += actualStep` 한 줄 추가
- `SmoothProgress = _cumulativeDistance / (_oneLapDistance × totalLaps)`
- **결과**: 캐릭터 이동과 100% 동기, 고점 턱턱 현상 완전 해결
- `_oneLapDistance`: 웨이포인트 중심선 합산, `StartRacing()` 시 1회 계산

### Phase 9 — 좌우 펼치기 (`94e6ffa`, `d9b6599`)
- **문제**: 모든 원이 세로 직선으로 배열 → 아래쪽 원 보이지 않음
- `LateralOffset = laneOffset + deviationOffset` 프로퍼티 추가 (RacerController)
- X 범위 처음 ±40% → ±20%로 축소 (과도한 움직임 방지)
- 우마무스메처럼 원들이 자연스럽게 퍼지며 달리는 효과

---

## 기술 결정 사항

### 이동 메커니즘 선택 과정
| 시도 | 방식 | 결과 |
|------|------|------|
| v1 | OverallProgress 직접 읽기 | 계단식 이동 |
| v2 | SmoothDamp(0.15s) | "멈추고 가고" 현상 |
| v3 | 벡터 투영 + Lerp | 떨림 감소, 고점 막힘 |
| v4 | 모노토닉 래칫 | 고점 개선, 복잡성 증가 |
| **v5** | **누적 이동 거리** | **완전 해결, 구조 단순** |

### 누적 이동 거리 방식의 장점
- 캐릭터 이동코드(`actualStep`)를 그대로 재사용 → 완전 동기화
- 바퀴 수 자동 압축: n바퀴 = 1/n 속도로 매핑
- Lerp/SmoothDamp 불필요 → 오버슈트/지연 없음
- 코드 단순 (`_cumulativeDistance += actualStep` 한 줄)

---

## 수정된 파일

| 파일 | 주요 변경 |
|------|----------|
| `SceneBootstrapper.Racing.cs` | BuildTrackProgressBar, UpdateTrackProgressBar, InitTrackBarForRace, BuildLapDividers 신규 |
| `SceneBootstrapper.cs` | RacerCircleUI struct, 관련 필드 추가 |
| `RacerController.cs` | SmoothProgress, LateralOffset, _cumulativeDistance, CalculateOneLapDistance 추가 |
| `GameSettings.cs` | trackProgressBarPrefab 필드 추가 |
| `StringTable.csv` | str.hud.start, str.hud.goal 7개 언어 추가 |

---

## 잔여 과제
- 트랙바 프리팹 디자인 교체 (현재 프로시저럴 생성)
- 완주 순서별 애니메이션 연출
- 1바퀴 기준 트랙바 세부 디자인 개선
