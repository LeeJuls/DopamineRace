# his037: 거리별 밸런스 튜닝 — Emergency Drain LapScale 시스템

> **작업일**: 2026-04-05
> **작성자**: Claude
> **관련 스펙**: spec016_거리별밸런스.md

---

## 1. 작업 개요

레이스 거리(바퀴 수)에 따라 타입별 승률이 유의미하게 달라지는 밸런스 구현.
기존 단일 `emergencyDrainMul` 값으로는 단거리~장거리 동시 밸런스 불가 → **거리별 스케일링(A안)** 도입.

---

## 2. 문제 분석 (세션 시작 상태)

### 2.1 자동 재실행 버그 수정
- `AutoRaceRunnerWindow`의 `autoRestart` 루프가 항상 실행되는 버그 → 루프 로직 완전 제거
- 테스트는 N게임 완료 후 항상 정지

### 2.2 진단 로그 강화
- `RacerController_V4.cs`에 신규 필드 추가:
  - `V4EmergencyBurstCount`: 레이스당 Emergency Burst 발동 횟수
  - `V4SpurtEntryRank`: 스퍼트 진입 시 순위
- `AutoRaceRunnerWindow.cs` 로그 포맷 확장:
  - DIST 섹션: `win%/t3%/avg/hp` (거리별 spurtHP 추가)
  - TOTAL 섹션: `emerg` (평균 Emergency 발동) + `spRank` (스퍼트 진입 순위)

---

## 3. 진동(Oscillation) 문제 발견

### 3.1 단순 파라미터 조정의 한계

`runnerEmergencyDrainMul` 값과 `runnerTargetRank` 조합에서 이진 스위치 현상 발생:

| 설정 | 결과 |
|------|------|
| targetRank=1, drain=2.5 | Runner 73% (너무 강) |
| targetRank=1, drain=4.0 | Runner 20% (너무 약) |
| targetRank=2, drain=3.0 | Runner 57.5% (너무 강) |
| targetRank=2, drain=3.5 | Runner 48.5% (너무 강) |
| targetRank=1, drain=3.5 | Runner 27.5% |

### 3.2 근본 원인 파악

**랩 수에 따른 Emergency 발동 횟수 비례 누적**:
- 5바퀴 레이스: Emergency 발동 ~5회 → drain 영향 ×2.5배
- 2바퀴 레이스: Emergency 발동 ~2회 → drain 영향 상대적으로 낮음
- 단일 `emergencyDrainMul` 값으로 전 거리 동시 밸런스 불가 구조적 한계

---

## 4. A안: 거리별 Emergency Drain 스케일링

### 4.1 구현 (`RacerController_V4.cs` + `RaceBacktestWindow.cs` 동시 수정)

```csharp
// 3바퀴 기준 √(laps/3) 스케일링
// 2L: 0.816, 3L: 1.0, 4L: 1.155, 5L: 1.291
float lapScale = Mathf.Pow((float)GetTotalLaps() / 3f, 0.5f);
drain *= gs.GetV4EmergencyBurstDrainMul(charDataV4.charType) * lapScale;
```

**백테스트 윈도우 미러링**:
```csharp
float lapScale = Mathf.Pow((float)simLaps / 3f, 0.5f);
drain *= gs4.GetV4EmergencyBurstDrainMul(dv4.charType) * lapScale;
```

### 4.2 스케일 효과

| 거리 | lapScale | base 3.8 기준 실효 drain |
|------|----------|------------------------|
| 2L | 0.816 | 3.10 → 도주 단거리 유리 |
| 3L | 1.000 | 3.80 → 기준 |
| 4L | 1.155 | 4.39 |
| 5L | 1.291 | 4.91 → 도주 장거리 불리 |

---

## 5. 튜닝 사이클 (A안 적용 후)

### 주요 테스트 결과 요약

| 차수 | 주요 변경 | R전체 | L전체 | C전체 | K전체 | 특이사항 |
|------|----------|-------|-------|-------|-------|---------|
| A안 첫 적용 (drain=3.5) | lapScale 도입 | 34.5% | 22% | 13% | 35.5% | 2L R=66% |
| 15차 (drain=4.5) | 2L Runner 억제 | 20% | 25.5% | 17.5% | 32.5% | 4L R=6% 급락 |
| 16차 (drain=4.0) | 4L/5L Runner 회복 | 24% | 25% | 22.5% | 28.5% | 5L L=30% 문제 |
| 17차 (drain=3.7) | 5L Runner 회복 | 29% | 26% | 18% | 24.5% | 4L L=32% 문제 |
| 18차 (leaderTargetMax=3) | 4L 선행 억제 | 24% | 20.5% | 24% | 31.5% | 3L R=18% 급락 |
| 19차 (leaderTargetMax=4, leaderDrain=4.2) | 원복 | 31.5% | 22.5% | 21.5% | 24.5% | 2L R=68% 급등 |
| 20차 (leaderTargetMax=4, leaderDrain=4.5, K=0.65) | Runner+Leader 조정 | 26% | 23.5% | 26% | 24.5% | **전체 균형 첫 달성** |
| 21차 (chaserDrain=1.3, reckonerDrain=0.55) | 5L Chaser 억제 | 27.3% | 23.8% | 22.1% | 26.9% | **최종 확정** |

---

## 6. 최종 파라미터 (GameSettingsV4.asset)

```
v4_runnerTargetRank: 1
v4_leaderTargetMax: 4
v4_reckonerTargetMax: 5

v4_runnerEmergencyDrainMul:   3.8
v4_leaderEmergencyDrainMul:   4.5
v4_chaserEmergencyDrainMul:   1.3
v4_reckonerEmergencyDrainMul: 0.55

lapScale 공식: √(랩수/3)  [RacerController_V4.cs + RaceBacktestWindow.cs]
```

---

## 7. 최종 검증 결과 (100게임 × 4거리 = n=200/거리)

### 거리별 승률

| 거리 | Runner | Leader | Chaser | Reckoner | 평가 |
|------|--------|--------|--------|----------|------|
| 2L | **46%** | 24% | 15% | 16% | 도주 우세 ✅ |
| 3L | 28% | 25% | 27% | 22% | 완전 균형 ✅ |
| 4L | 20% | 25% | 25% | **31%** | 추입 약간 강세 ✅ |
| 5L | 17% | 22% | 23% | **40%** | 추입 우세 ✅ |

### 전체 타입 분포

| Runner | Leader | Chaser | Reckoner |
|--------|--------|--------|----------|
| 27.3% | 23.8% | 22.1% | 26.9% |

**4타입 모두 22~27% 범위** — 목표 달성 ✅

### 검증 체크리스트

- [x] 2L: 도주 승률 > 30% (결과: 46%)
- [x] 5L: 추입 승률 > 도주 승률 (40% > 17%)
- [x] 5L: 추입 > 선행 (40% > 22%)
- [x] 전체 4타입 모두 20~30% 범위 유지
- [x] 선행 전체 ≤ 28% (결과: 23.8%)
- [x] 추입 전체 ≥ 20% (결과: 26.9%)

---

## 8. 수정 파일 목록

| 파일 | 변경 내용 |
|------|----------|
| `Assets/Scripts/Racer/RacerController_V4.cs` | lapScale 적용, V4EmergencyBurstCount/V4SpurtEntryRank 추가 |
| `Assets/Scripts/Editor/RaceBacktestWindow.cs` | lapScale 미러링, 로그 강화 |
| `Assets/Scripts/Editor/AutoRaceRunnerWindow.cs` | autoRestart 제거, emerg/spRank/spurtHP 로그 추가 |
| `Assets/Resources/GameSettingsV4.asset` | 최종 파라미터 값 확정 |
