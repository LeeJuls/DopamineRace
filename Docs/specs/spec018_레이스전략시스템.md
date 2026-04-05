# 명세서: 레이스 전략 시스템
**날짜**: 2026-02-26
**브랜치**: main

---

## 1. 개요

### 1.1 목적
캐릭터 타입(도주/선행/선입/추입)에 따른 레이스 전략을 3단계 페이즈로 구현.
- 레이스 초반: 타입별 포지션 정렬
- 중반: 그룹 대형 유지
- 후반: 타입별 전략 실행

### 1.2 적용 조건
- **2바퀴 이상** 레이스에서만 전략 시스템 활성화
- 1바퀴 레이스: 기존 로직 유지 (테스트용)

### 1.3 목표 승률

| 거리 | 도주(Runner) | 선행(Leader) | 선입(Chaser) | 추입(Reckoner) |
|------|-------------|-------------|-------------|---------------|
| 2바퀴 | **35%** | **30%** | **20%** | **15%** |
| 4바퀴 | **20%** | **30%** | **30%** | **20%** |
| 6바퀴 | **15%** | **20%** | **30%** | **35%** |

---

## 2. 페이즈 구조

```
OverallProgress (0.0 ~ 1.0)
│
0.0 ──────────────────────────────────────────── 1.0
│         │              │
│  Phase1 │   Phase2     │        Phase3
│포지셔닝  │  대형유지     │        전략
│0~0.5랩  │  0.5~1.0랩   │        1랩 이후
```

**페이즈 경계** (OverallProgress 기준):
- `positioningEnd = positioningLapEnd / totalLaps` (기본: 0.5 / laps)
- `formationEnd = formationHoldLapEnd / totalLaps` (기본: 1.0 / laps)
- 4바퀴 레이스: 포지셔닝 0~0.125, 대형유지 0.125~0.25, 전략 0.25~1.0

---

## 3. 페이즈 1: 포지셔닝 (0 ~ 0.5랩)

### 3.1 목적
레이스 시작 후 각 타입이 자신의 목표 순위 구간으로 이동.

### 3.2 타입별 목표 순위 구간 (12명 기준)

| 타입 | 목표 구간 | 스프린트 조건 |
|------|---------|------------|
| 도주 | 1~3위 (top 25%) | rank > 3 → activeRate |
| 선행 | 1~6위 (top 50%) | rank > 6 → activeRate |
| 선입 | 1~9위 (top 75%) | rank > 9 → activeRate |
| 추입 | 없음 (항상 후방) | 항상 → inZoneRate |

### 3.3 HP 소모 규칙
- 목표 구간보다 뒤처진 경우: `rate = activeRate` (스프린트)
- 목표 구간 이내인 경우: `rate = inZoneRate` (보존)
- 추입: 항상 `rate = reckoner_baseRate` (보존)

### 3.4 의도한 결과
- 도주가 빠르게 HP 소모 → consumedRatio 상승 → HP 부스트 선점 → 선두권 형성
- 추입이 HP 보존 → 부스트 지연 → 자연스럽게 후미로

---

## 4. 페이즈 2: 대형 유지 (0.5 ~ 1.0랩)

### 4.1 목적
포지셔닝이 완료된 후 상행/하행 그룹 분리를 유지하며 안정화.

### 4.2 그룹 정의

| 그룹 | 타입 | 목표 순위 |
|------|------|---------|
| 상행 (上行) | 도주 + 선행 | 1~6위 (top 50%) |
| 하행 (下行) | 선입 + 추입 | 7~12위 |

### 4.3 상행 규칙
포지셔닝과 동일한 타입별 목표 유지:
- 도주: rank > top25%(3위) → 스프린트, 이내 → 보존
- 선행: rank > top50%(6위) → 스프린트, 이내 → 보존
- → 도주와 선행이 1~6위 안에서 자연스럽게 업치락 뒤치락

### 4.4 하행 제약

**우선순위 순으로 적용:**

1. **상행 영역 침범 금지**
   - 현재 rank ≤ N/2 (상위 절반) → 강제 보존 (`reckoner_baseRate`)
   - 예: 12명 기준 rank ≤ 6이면 하행 타입은 강제 보존

2. **간격 벌어짐 방지** (상행 그룹을 너무 멀리 놓치면 안됨)
   - `gap = lastUpperProgress - myTotalProgress`
   - gap > `formationGapMax` (기본 0.3) → 스프린트 (`activeRate`)

3. **상행 추월 방지**
   - gap < `formationGapMin` (기본 0.05) → 보존 (`inZoneRate`)

4. **하행 내 경쟁** (위 조건에 해당 없을 때)
   - 선입: rank > top75%(9위) → 스프린트, 이내 → 보존
   - 추입: 항상 보존

**`lastUpperProgress` 계산**: 상행 캐릭터들(도주+선행) 중 가장 낮은 TotalProgress 값

---

## 5. 페이즈 3: 전략 (1랩 이후)

### 5.1 공통 원칙
- `remaining = 1.0 - OverallProgress` (남은 레이스 비율)
- 스퍼트 Lerp 공식: `rate = Lerp(inZoneRate, activeRate, spurtProgress)`
  - `spurtProgress = (progress - spurtThreshold) / spurtStart`

### 5.2 타입별 전략

#### 도주 (Runner)
- **항상 스프린트**: `rate = activeRate` (조건 없음)
- HP 보존 없음. remaining에 관계없이 풀스프린트.
- 단거리(2바퀴)에서 압도적 유리 → HP 소진 전에 레이스 종료

#### 선행 (Leader)
| 조건 | 모드 | HP 소모율 |
|------|------|---------|
| rank ≤ top30% AND remaining > 10% | 보존 | `inZoneRate` |
| rank > top30% (이탈) | 추격 스프린트 | `outZoneRate` |
| remaining ≤ 10% (마지막 10%) | 최종 스퍼트 | `Lerp → activeRate` |

- `leaderSpurtMinHP 체크 제거` → 남은 HP 무관하게 마지막 10%에서 무조건 스퍼트

#### 선입 (Chaser)
| 조건 | 모드 | HP 소모율 |
|------|------|---------|
| rank ≤ top70% AND remaining > 20% | 보존 | `inZoneRate` |
| rank > top70% (이탈) | 추격 스프린트 | `outZoneRate` |
| remaining ≤ 20% (마지막 20%) | 최종 스퍼트 | `Lerp → activeRate` |

#### 추입 (Reckoner)
| 조건 | 모드 | HP 소모율 |
|------|------|---------|
| remaining > 30% | 완전 보존 | `inZoneRate (=baseRate)` |
| remaining ≤ 30% (마지막 30%) | 최종 스퍼트 | `Lerp → activeRate` |

- 순위와 무관하게 보존/스퍼트만 존재
- 포지션 이탈 추격 없음 (순위를 신경쓰지 않음)

### 5.3 스퍼트 파라미터 변경

| 타입 | 파라미터 | 기존 값 | 신규 값 |
|------|---------|--------|--------|
| 도주 | runner_spurtStart | 0.00 | 0.00 (불변, 전략페이즈에서 항상 스프린트) |
| 선행 | leader_spurtStart | 0.10 | 0.10 (불변) |
| 선입 | chaser_spurtStart | 0.40 | **0.20** |
| 추입 | reckoner_spurtStart | 0.25 | **0.30** |

---

## 6. HP 모델 원리

```
HP 소모율 체계:
  reckoner_baseRate ≈ 1.1   ← 추입 보존 (가장 낮음)
  inZoneRate(타입별) ≈ 0.5~1.2  ← 보존 모드
  outZoneRate(타입별) ≈ 1.5~2.0 ← 추격 스프린트
  activeRate(타입별) ≈ 5.5~7.0  ← 최종 스퍼트

consumedRatio = totalConsumedHP / maxHP
→ boostThreshold(0.4) 도달 시 HP 부스트 피크
→ 보존 모드에서는 부스트 상승 지연 (추입의 핵심 전략)
→ 스프린트 시 빠르게 consumedRatio 상승 → 부스트 폭발
```

---

## 7. 수정 파일 목록

### 7.1 GameSettings.cs

**추가 필드** (기존 `GetFormationModifier()` 이후):
```csharp
[Header("═══ 레이스 전략: 구간 설정 ═══")]
public float positioningLapEnd = 0.5f;     // 포지셔닝 종료 (랩 단위)
public float formationHoldLapEnd = 1.0f;   // 대형유지 종료 (랩 단위)

[Header("═══ 레이스 전략: 포지셔닝 타겟 ═══")]
public float runner_posTarget = 0.25f;
public float leader_posTarget = 0.50f;
public float chaser_posTarget = 0.75f;

[Header("═══ 레이스 전략: 대형 유지 ═══")]
public float formationGapMax = 0.3f;   // 상행과 최대 허용 간격 (TotalProgress)
public float formationGapMin = 0.05f;  // 상행과 최소 간격 (추월 방지)
```

**추가 메서드**:
```csharp
public float GetPositioningTarget(CharacterType type)
// Runner→0.25, Leader→0.50, Chaser→0.75, Reckoner→-1
```

### 7.2 RacerController.cs

**변경**: `ConsumeHP()` 전체 교체
- 기존 lines 435–515 → 새 페이즈 분기 구조
- 새 private 메서드 추가:
  - `ConsumeHP_Legacy()` — 1랩 폴백
  - `ConsumeHP_Positioning()` — 페이즈 1
  - `ConsumeHP_FormationHold()` — 페이즈 2
  - `ConsumeHP_Strategy()` — 페이즈 3
  - `ApplyHPConsumption()` — 공통 tail (boostAmp/speedRatio/leadPaceTax)
  - `IsUpperTrack()` — 상행 여부
  - `GetLastUpperTrackProgress()` — 상행 마지막 위치

### 7.3 RaceBacktestWindow.cs

**변경**: `SimConsumeHP()` 전체 교체
- 동일 4페이즈 구조 미러링
- 대형유지 하행 간격: TotalProgress 대신 rank 기반 근사
  - rank ≤ topHalf → 강제 보존
  - rank > topHalf + 2 → 스프린트
- 새 private 메서드:
  - `SimConsumeHP_Legacy()`, `SimConsumeHP_Positioning()`
  - `SimConsumeHP_FormationHold()`, `SimConsumeHP_Strategy()`
  - `SimApplyHPConsumption()`

### 7.4 GameSettings.asset

| 변경 항목 | 기존 | 신규 |
|---------|------|------|
| `chaser_spurtStart` | 0.40 | **0.20** |
| `reckoner_spurtStart` | 0.25 | **0.30** |
| 신규 파라미터 7개 | - | C# 기본값으로 직렬화 |

---

## 8. 변경하지 않는 시스템

| 시스템 | 이유 |
|--------|------|
| `CalcHPBoost()` | consumedRatio 기반 부스트 곡선 유지 |
| `CalculateSpeed()` | 속도 계산 변경 없음 |
| 부스트 피드백 (`boostHPDrainCoeff`) | 기존 1.5 유지 |
| Power 가속 (`powerAccelCoeff`) | 기존 0.5 유지 |
| 충돌/슬립스트림/CP/earlyBonus 시스템 | 변경 없음 |
| 기존 GameSettings 필드 | 삭제 없음 (하위 호환성) |

---

## 9. 예상 동작 흐름 (4바퀴, 12명 기준)

```
0.0~0.125 [포지셔닝]
  도주 ×3: 3위 밖이면 스프린트 → 빠르게 1~3위로
  추입 ×3: 항상 보존 → 10~12위로 자연 낙하
  선입 ×3: 9위 밖이면 스프린트 → 7~9위로 이동

0.125~0.25 [대형유지]
  상행(도주+선행): 각자 목표 순위 유지하며 경쟁
  하행(선입+추입): 6위권 침범 금지, 상행 거리 유지

0.25~1.0 [전략]
  도주: 풀스프린트 유지 → HP 소진 전까지 선두권
  선행: top30% 유지하며 보존, 마지막 10% 스퍼트
  선입: top70% 유지하며 보존, 마지막 20% 스퍼트
  추입: 1랩+0.7×3 = 3.1랩까지 보존, 이후 스퍼트 폭발
```

---

## 10. 검증 계획

### 10.1 컴파일 검증
- Unity 에디터에서 에러 없음 확인

### 10.2 백테스트 (RaceBacktestWindow)

**1차: 4바퀴 균형 조정**
- 설정: 균등스탯 20, 충돌 ON, settling 2.0s, 500회
- 목표: 20% / 30% / 30% / 20% (±5%)
- 조정 파라미터:
  - `formationGapMax` — 하행 그룹 간격
  - `inZoneRate` (타입별) — 보존 소모율
  - `outZoneRate` (타입별) — 이탈 추격률

**2차: 거리별 검증**
- 2바퀴 목표: 35% / 30% / 20% / 15%
- 6바퀴 목표: 15% / 20% / 30% / 35%
- 주요 조정: `formationHoldLapEnd`, `positioningLapEnd`

### 10.3 구간별 포지션 아크 확인
```
기대 패턴 (4바퀴):
10%: 도주 1~3위 / 추입 10~12위 (포지셔닝 효과)
25%: 상행/하행 분리 (대형유지 효과)
70%: 추입 스퍼트 시작 (후방 추격)
결승: 선입/추입 역전 패턴
```
