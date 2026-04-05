# 히스토리: 트랙바 싱크 + 스폰 정렬 + STA 밸런스 (2026-03-29)

## 배경
이전 세션에서 V2/V3 시스템 제거, 프리팹 리넘버링, MD 파일 최적화 완료 후,
실제 플레이 테스트에서 다수의 시각/밸런스 이슈 발견.

## 작업 과정

### 1. 트랙 프로그레스 바 부드러운 이동
- **문제**: 트랙바 마커가 틱틱 순간이동
- **원인**: `OverallProgress`가 WP 인덱스 기반 계단식 점프
- **1차 시도**: `SmoothProgress`(누적 거리) 사용 → 부드럽지만 싱크 불일치
- **최종 해결**: `OverallProgress`에 서브WP 보간(Dot 투영) 추가
  - `_maxOverallProgress` 단조 증가로 골인 시 드롭 방지
  - `trackBarLerpSpeed` Inspector 설정 추가 (기본 12)

### 2. 시작 방향 문제
- **문제**: 뒷줄 캐릭터가 레이스 방향(←)이 아닌 위(↑)로 달려감
- **원인**: 모든 캐릭터가 WP0(결승선, 스폰 대비 측면)을 향해 출발
- **해결**: `currentWP = 0` → `currentWP = 1` (WP1은 확실히 앞쪽)

### 3. 타입별 스폰 정렬
- **문제**: Runner(도주)가 뒷줄 출발 시 회복 불가
- **해결**: `RaceManager.SortByTypePosition()` 추가
  - Runner→앞줄, Leader→앞~중, Chaser→중~뒤, Reckoner→뒷줄
  - 같은 타입 내 랜덤 배치

### 4. 컨디션 HP 미적용
- **문제**: 컨디션 0.90이 HP풀을 줄여 저STA 캐릭터 탈진 가속
- **해결**: `v4MaxStamina *= condMul` 제거 → HP풀은 순수 STA 기반

### 5. STA 최소 11 밸런스
- **문제**: Crystal(STA 5), Secret(STA 8) 등 2바퀴에서도 HP 탈진
- **해결**: STA < 11인 5명의 STA를 11로 조정, 스탯 합계 유지
  - Crystal: STA 5→11 (SPD 20→17, ACC 20→17)
  - Secret: STA 8→11 (INT 20→19, LCK 20→18)
  - Amazon: STA 10→11 (INT 19→18)
  - Juhong: STA 10→11 (SPD 20→19)
  - Powerblade: STA 10→11 (LCK 20→19)

## 산출물
- SPEC-014 갱신: `Docs/specs/SPEC-014_트랙바싱크개선_명세서_20260329.md`
- 코드: RacerController, RacerController_V4, SceneBootstrapper.Racing, GameSettings, RaceManager
- 데이터: CharacterDB_V4.csv (STA 밸런스)

## 미결 사항
- 긴급부스트 Leader Drain 값 (현재 3.2) — 오너가 Inspector에서 추가 조정 예정
- 백테스트 100회 미실행 — 밸런스 정밀 확인 필요
- `SmoothProgress` / `_cumulativeDistance` 데드코드 정리 여부 미결정
