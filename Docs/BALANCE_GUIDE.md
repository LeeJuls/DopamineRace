# 도파민레이스 — 밸런스 설계 가이드

> **대상 독자**: 밸런스 기획자 (Claude 또는 사람)
> **목적**: 레이싱 로직·캐릭터 데이터·트랙 데이터의 상호작용을 이해하고, 의도된 밸런스를 설계·검증하기 위한 참조 문서
> **최종 수정**: 2026-02-24

---

## 목차
1. [속도 공식 (Master Formula)](#1-속도-공식-master-formula)
2. [6대 스탯 상세](#2-6대-스탯-상세)
3. [캐릭터 타입 시스템](#3-캐릭터-타입-시스템)
4. [트랙 시스템](#4-트랙-시스템)
5. [충돌 시스템](#5-충돌-시스템)
6. [스킬 시스템](#6-스킬-시스템)
7. [컨디션 시스템](#7-컨디션-시스템)
8. [캐릭터 로스터](#8-캐릭터-로스터)
9. [GameSettings 전체 밸런스 파라미터](#9-gamesettings-전체-밸런스-파라미터)
10. [백테스팅 시뮬레이션과 런타임 차이](#10-백테스팅-시뮬레이션과-런타임-차이)
11. [밸런스 조정 체크리스트](#11-밸런스-조정-체크리스트)

---

## 1. 속도 공식 (Master Formula)

매 프레임 `RacerController.CalculateSpeed()`에서 최종 속도를 계산한다.

### 전체 흐름

```
baseSpeed = SpeedMultiplier × globalSpeedMultiplier × track.speedMultiplier

bonuses = typeBonus + powerBonus + braveBonus

speed = baseSpeed × (1 + bonuses × condMul)
speed += noiseValue × condMul
speed -= fatigue / condMul
speed *= slowZoneMul
speed *= critMul
speed *= collisionMul         ← (1 - collisionPenalty + slingshotBoost)

finalSpeed = max(speed, 0.1)
currentSpeed = Lerp(currentSpeed, finalSpeed, deltaTime × raceSpeedLerp)
```

### 각 항 설명

| 항목 | 공식 | 관여 스탯/설정 |
|------|------|----------------|
| **baseSpeed** | `(0.8 + charBaseSpeed×0.01) × 2.5 × trackSpeedMul` | Speed, globalSpeedMultiplier |
| **typeBonus** | `gs.GetTypeBonus(type, phase) × trackPhaseMul` | 타입, 레이스 진행도 |
| **powerBonus** | `(charBasePower/20) × track.powerSpeedBonus` | Power (특정 트랙만) |
| **braveBonus** | `(charBaseBrave/20) × track.braveSpeedBonus` | Brave (특정 트랙만) |
| **noiseValue** | `Random(-maxNoise, +maxNoise)` | Calm |
| **fatigue** | `progress × (1/endurance) × fatigueFactor × trackFatMul` | Endurance, 진행도 |
| **critMul** | 크리 발동 시 1.3, 아니면 1.0 | Luck |
| **collisionMul** | `1 - penalty + slingshot` | Power, Brave, Luck |
| **condMul** | 라운드 시작 시 랜덤 결정 (0.90~1.20) | 컨디션 |

---

## 2. 6대 스탯 상세

모든 스탯은 CSV에서 1~20 범위, 12캐릭터 모두 합계 90.

### Speed (속도)

**효과**: 항상 적용되는 기본 속도 결정.

```
SpeedMultiplier = 0.8 + charBaseSpeed × 0.01
```

| Speed | SpeedMultiplier | baseSpeed (기본트랙) |
|-------|-----------------|---------------------|
| 20 | 1.00 | 2.500 |
| 15 | 0.95 | 2.375 |
| 10 | 0.90 | 2.250 |

- 격차 범위: 최대 ~10% (20 vs 10)
- **가장 안정적이고 일관된 스탯** — 매 프레임 무조건 적용
- 다른 스탯은 조건부(트랙, 충돌, 확률)지만 Speed는 항상 영향

### Power (파워)

**효과 1**: 충돌 승리 확률 (비선형)

```
높은 쪽에 추가 이점:
  powerA > powerB일 때:
    effA = powerA × (1 + powerA/(powerA+powerB))
    effB = powerB (그대로)
  승률 = effA / (effA + effB)
```

| A의 Power | B의 Power | A 승률 |
|-----------|-----------|--------|
| 20 | 10 | 76.9% |
| 20 | 15 | 64.6% |
| 15 | 15 | 50.0% |
| 15 | 10 | 66.0% |

**효과 2**: 특정 트랙에서 직접 속도 보너스

```
powerBonus = (charBasePower/20) × track.powerSpeedBonus
```

- 사막(Desert): `powerSpeedBonus = 0.15` → Power 20이면 +15% 보너스
- 일반/비/눈/고원: 0 (효과 없음)

### Brave (용감)

**효과 1**: 슬링샷 부스트량 결정

```
boost = charBaseBrave × slingshotFactor × track.slingshotMultiplier
      = charBaseBrave × 0.02 × trackMul

최대: slingshotMaxBoost = 0.4 (40%)
지속: slingshotDuration = 1.0초
```

| Brave | 일반트랙 부스트 | 눈트랙 (×1.6) |
|-------|----------------|---------------|
| 20 | 0.40 (Max) | 0.40 (Max) |
| 15 | 0.30 | 0.48→0.40 (Max) |
| 10 | 0.20 | 0.32 |
| 5 | 0.10 | 0.16 |

- 슬링샷은 **충돌 후 뒤처진 레이서**에게 발동
- 패널티 지속 후 발동되므로, 충돌이 많은 환경에서 가치 증가

**효과 2**: 특정 트랙에서 직접 속도 보너스

```
braveBonus = (charBaseBrave/20) × track.braveSpeedBonus
```

- 고원(Highland): `braveSpeedBonus = 0.12` → Brave 20이면 +12% 보너스
- 나머지 트랙: 0

### Calm (침착)

**효과**: 속도 노이즈(변동) 폭 결정

```
maxNoise = (1/calm) × noiseFactor × trackNoiseMul × globalSpeedMultiplier
noiseValue = Random(-maxNoise, +maxNoise)
```

| Calm | maxNoise (일반) | maxNoise (비×2.0) |
|------|-----------------|-------------------|
| 20 | 0.0125 | 0.025 |
| 15 | 0.0167 | 0.033 |
| 10 | 0.025 | 0.050 |
| 5 | 0.050 | 0.100 |

- 노이즈는 0.5~1.5초마다 재추출 (양수/음수 모두 가능)
- 평균 기대값은 0이지만, **분산이 큼** → 낮은 Calm은 운에 의존
- 비 트랙(noiseMultiplier=2.0)에서 Calm 가치 2배

### Endurance (지구력)

**효과**: 후반 피로 감속 억제

```
fatigue = progress × (1/endurance) × fatigueFactor × trackFatMul
```

| Endurance | 피로 (종료 시점, 일반) | 피로 (종료 시점, 눈×1.8) |
|-----------|---------------------|------------------------|
| 20 | 0.0075 | 0.0135 |
| 15 | 0.0100 | 0.0180 |
| 10 | 0.0150 | 0.0270 |
| 5 | 0.0300 | 0.0540 |

- **레이스가 길수록 (바퀴 수 ↑) 효과 증가** — progress가 0→1로 선형 증가
- 눈 트랙(fatigueMultiplier=1.8)에서 Endurance 가치 1.8배
- 컨디션에 의한 추가 보정: 좋은 컨디션이면 `fatigue / 1.2`, 나쁘면 `fatigue / 0.9`

### Luck (행운)

**효과 1**: 크리티컬 확률

```
critChance = charBaseLuck × luckCritChance × trackLuckMul
           = luck × 0.005 × trackLuckMul
검사 주기: 3.0초마다
크리 효과: 속도 ×1.3, 1.5초간
```

| Luck | 크리 확률/3초 (일반) | 크리 확률/3초 (비×1.3) |
|------|---------------------|----------------------|
| 20 | 10.0% | 13.0% |
| 15 | 7.5% | 9.75% |
| 10 | 5.0% | 6.5% |
| 5 | 2.5% | 3.25% |

**효과 2**: 충돌 회피 확률

```
dodgeChance = charBaseLuck × luckDodgeChance × trackLuckMul
            = luck × 0.02 × trackLuckMul
```

| Luck | 회피 확률 (일반) | 회피 확률 (비×1.3) |
|------|-----------------|-------------------|
| 20 | 40% | 52% |
| 15 | 30% | 39% |
| 10 | 20% | 26% |
| 5 | 10% | 13% |

- **고분산 스탯** — 크리 발동 여부에 따라 결과가 크게 달라짐
- 비 트랙, 사막 트랙에서 luck 효과 증폭

---

## 3. 캐릭터 타입 시스템

레이스는 3구간으로 나뉜다:

| 구간 | 진행도 | 설명 |
|------|--------|------|
| 전반 (Early) | 0% ~ 35% | 출발 직후 |
| 중반 (Mid) | 35% ~ 70% | 중간 구간 |
| 후반 (Late) | 70% ~ 100% | 결승 직전 |

### 타입별 구간 보너스 (GameSettings 기본값)

| 타입 | 전반 | 중반 | 후반 | 전략 |
|------|------|------|------|------|
| **Runner** | +0.15 | -0.05 | -0.10 | 초반 도주형 — 빨리 앞서고 뒤처짐 |
| **Leader** | +0.08 | +0.05 | 0.00 | 안정형 — 전체적으로 고름 |
| **Chaser** | 0.00 | +0.10 | +0.05 | 추적형 — 중후반 추월 |
| **Reckoner** | -0.05 | +0.03 | +0.18 | 역전형 — 후반 폭발 |

### 트랙에 의한 구간 배율 조정

트랙마다 `earlyBonusMultiplier`, `midBonusMultiplier`, `lateBonusMultiplier`가 있다.

예시: 비 트랙 `earlyBonusMultiplier = 0.6` → Runner의 전반 보너스 +0.15 × 0.6 = +0.09 (약화)

---

## 4. 트랙 시스템

### 트랙 목록 (TrackDB.csv)

| 트랙 | 타입 | 가중치 | 핵심 특성 |
|------|------|--------|-----------|
| **일반 (Normal)** | E_Base | 100 | 모든 배율 1.0, 기준 트랙 |
| **비 (Rainy)** | E_Base | 80 | Calm 중요 (noise×2), 충돌↑(range×1.4), Luck↑(×1.3) |
| **눈 (Snow)** | E_Base | 60 | Endurance 중요 (fatigue×1.8), 속도↓(×0.9), 슬링샷↑(×1.6) |
| **사막 (Desert)** | E_Dirt | 70 | Power→속도 보너스(0.15), 패배 패널티↑(×1.6), Luck↑(×1.2) |
| **고원 (Highland)** | E_Dirt | 50 | Brave→속도 보너스(0.12), 중반 감속구간, 속도↓(×0.85) |

### 트랙별 전체 파라미터

| 파라미터 | 일반 | 비 | 눈 | 사막 | 고원 |
|----------|------|-----|------|------|------|
| speedMultiplier | 1.0 | 1.0 | 0.9 | 1.0 | 0.85 |
| noiseMultiplier | 1.0 | 2.0 | 1.0 | 1.6 | 1.0 |
| fatigueMultiplier | 1.0 | 1.0 | 1.8 | 1.4 | 1.0 |
| collisionRangeMultiplier | 1.0 | 1.4 | 1.0 | 1.0 | 0.7 |
| collisionPenaltyMultiplier | 1.0 | 0.6 | 1.5 | 1.0 | 0.7 |
| slingshotMultiplier | 1.0 | 0.7 | 1.6 | 1.0 | 1.0 |
| earlyBonusMultiplier | 1.0 | 0.6 | 1.0 | 1.0 | 1.0 |
| midBonusMultiplier | 1.0 | 1.0 | 1.0 | 1.0 | 1.0 |
| lateBonusMultiplier | 1.0 | 1.0 | 1.0 | 1.0 | 1.0 |
| powerSpeedBonus | 0 | 0 | 0 | 0.15 | 0 |
| braveSpeedBonus | 0 | 0 | 0 | 0 | 0.12 |
| luckMultiplier | 1.0 | 1.3 | 0.7 | 1.2 | 0.8 |
| hasMidSlowZone | × | × | × | × | ○ (0.4~0.6, ×0.8) |
| loserPenaltyDurationMul | 1.0 | 1.0 | 1.0 | 1.6 | 1.0 |

### 트랙별 유리/불리 스탯

| 트랙 | 유리한 스탯 | 불리해지는 캐릭터 |
|------|------------|------------------|
| 비 | Calm, Luck | 낮은 Calm 캐릭터 (노이즈 폭 2배) |
| 눈 | Endurance, Brave | 낮은 Endurance (피로 1.8배), 낮은 Brave (슬링샷 약함) |
| 사막 | Power, Luck | 낮은 Power (속도 보너스 못 받음 + 패배 시 패널티 지속↑) |
| 고원 | Brave, Speed | 낮은 Brave (속도 보너스 못 받음), 중반 감속구간 |

---

## 5. 충돌 시스템

### 충돌 발생 조건 (순서대로)

1. `enableCollision = true`
2. 글로벌 쿨다운 (0.5초) 경과
3. 두 레이서 모두 현재 충돌 패널티 없음
4. 개별 레이서 쿨다운 확인
5. 페어 쿨다운 (동일 조합 2.0초 재충돌 방지)
6. 밀집 감쇄: 범위 내 3명 이상이면 충돌 확률 50%로 감소
7. 기본 충돌 확률: 60%
8. 프레임당 최대 1건 처리

### 충돌 해결 흐름

```
1. Power 비교 → 승자/패자 결정 (비선형 확률)
2. 패자 Luck 회피 판정 → 성공 시 충돌 취소
3. 승자: 패널티 15% × 0.3초
   패자: 패널티 30% × 0.5초 (트랙에 따라 ×1.6까지)
4. 뒤처진 레이서에게 슬링샷 예약 (Brave 기반)
5. 슬링샷은 패널티 해제 후 발동 (1.0초간)
```

### 충돌의 밸런스 영향

- **Power 20 vs 10**: 77% 승률 (비선형 이점)
- **Luck 20**: 40% 회피 → 충돌 패배를 절반 가까이 무효화
- **Brave 20**: 슬링샷 0.40 (40% 속도 부스트) → 충돌 후 역전 가능
- 충돌은 **밀집 상황에서 더 자주 발생** → 중위권 레이서에게 영향 큼
- 사막 트랙: 패배 패널티 지속 ×1.6 → Power 낮은 캐릭터에게 치명적

---

## 6. 스킬 시스템

현재 모든 12캐릭터 동일: `E_Skill_Collision:5` (5초 지속)

```
충돌 5회 경험 → 스킬 발동 (5초)
  → 스킬 중 충돌 시 무조건 승리 (상대도 스킬 중이면 Power 판정)
  → 5초 후 해제, 카운트 리셋 (재발동 가능)
```

- 현재 밸런스 영향: 모두 동일 스킬이므로 차이 없음
- **향후 캐릭터별 고유 스킬 도입 시 주요 밸런스 변수**

---

## 7. 컨디션 시스템

라운드 시작 시 각 캐릭터에 랜덤 컨디션 부여.

### 확률 분포

| 컨디션 | 확률 | 배율 (condMul) |
|--------|------|----------------|
| 최고 (Best) | 12% | 1.20 |
| 좋음 (Good) | 18% | 1.10 |
| 보통 (Normal) | 40% | 1.00 |
| 나쁨 (Bad) | 18% | 0.95 |
| 최악 (Worst) | 12% | 0.90 |

### condMul 적용 방식

| 적용 대상 | 방식 | 좋은 컨디션 효과 | 나쁜 컨디션 효과 |
|-----------|------|-----------------|-----------------|
| 타입/Power/Brave 보너스 | `× condMul` | 보너스 증폭 | 보너스 약화 |
| 노이즈 | `× condMul` | 변동 증폭 (양날의 검) | 변동 약화 |
| 피로 | `÷ condMul` | 피로 감소 | 피로 증가 |

- condMul 최소값: 0.3 (안전 가드)
- **백테스팅에서는 컨디션이 적용되지 않음** (모든 캐릭터 condMul=1.0 취급)

---

## 8. 캐릭터 로스터

### 현재 12캐릭터 (모두 스탯 합계 90)

| # | 이름 | SPD | POW | BRV | CLM | END | LCK | 타입 | 특징 |
|---|------|-----|-----|-----|-----|-----|-----|------|------|
| 001 | 번개 | 15 | 15 | 15 | 15 | 15 | 15 | Leader | 올라운더 |
| 002 | 태풍 | 20 | 15 | 20 | 10 | 15 | 10 | Runner | 고속+용감, 낮은 Calm |
| 003 | 혜성 | 15 | 20 | 20 | 15 | 10 | 10 | Reckoner | 고파워+용감, 낮은 Endurance |
| 004 | 로켓 | 15 | 15 | 15 | 10 | 20 | 15 | Leader | 고지구력, 낮은 Calm |
| 005 | 불꽃 | 15 | 20 | 20 | 15 | 15 | 5 | Chaser | 고파워+용감, 매우 낮은 Luck |
| 006 | 질풍 | 20 | 15 | 15 | 15 | 15 | 10 | Leader | 고속, 안정형 |
| 007 | 천둥 | 15 | 20 | 15 | 20 | 15 | 5 | Chaser | 고파워+침착, 매우 낮은 Luck |
| 008 | 유성 | 15 | 15 | 15 | 15 | 10 | 20 | Runner | 최고 Luck, 낮은 Endurance |
| 009 | 폭풍 | 20 | 10 | 15 | 15 | 20 | 10 | Runner | 고속+지구력, 낮은 Power |
| 010 | 섬광 | 20 | 10 | 20 | 15 | 15 | 10 | Chaser | 고속+용감, 낮은 Power |
| 011 | 해일 | 15 | 20 | 15 | 20 | 10 | 10 | Reckoner | 고파워+침착, 낮은 Endurance |
| 012 | 선녀 | 15 | 15 | 15 | 15 | 20 | 10 | Reckoner | 고지구력, 안정형 |

### 스탯 분포 패턴

- **Speed 20**: 태풍(002), 질풍(006), 폭풍(009), 섬광(010) — 4명
- **Power 20**: 혜성(003), 불꽃(005), 천둥(007), 해일(011) — 4명
- **Brave 20**: 태풍(002), 혜성(003), 불꽃(005), 섬광(010) — 4명
- **Calm 20**: 천둥(007), 해일(011) — 2명
- **Endurance 20**: 로켓(004), 폭풍(009), 선녀(012) — 3명
- **Luck 20**: 유성(008) — 1명 (유일하게 최고 Luck)
- **Luck 5**: 불꽃(005), 천둥(007) — 2명 (회피/크리 거의 불가)

---

## 9. GameSettings 전체 밸런스 파라미터

### 레이스 코어

| 파라미터 | 기본값 | 설명 | 조정 가이드 |
|----------|--------|------|------------|
| globalSpeedMultiplier | 2.5 | 전역 속도 배율 | ↑=레이스 빠름, ↓=느림 |
| noiseFactor | 0.1 | Calm 기반 노이즈 크기 | ↑=Calm 가치↑, 결과 변동↑ |
| fatigueFactor | 0.15 | Endurance 기반 피로 | ↑=Endurance 가치↑, 장거리 영향↑ |
| raceSpeedLerp | 3.0 | 속도 보간 속도 | ↑=속도 변화 즉각 반영 |
| initialSpeedMultiplier | 0.5 | 출발 시 속도 비율 | ↑=출발 빠름 |

### 충돌

| 파라미터 | 기본값 | 설명 |
|----------|--------|------|
| enableCollision | true | 충돌 ON/OFF |
| collisionChance | 0.6 | 기본 충돌 확률 (60%) |
| collisionRange | 0.8 | 충돌 감지 거리 |
| collisionCooldown | 2.0 | 동일 페어 재충돌 쿨다운 |
| collisionBasePenalty | 0.3 | 패자 30%, 승자 15% 감속 |
| winnerPenaltyDuration | 0.3초 | 승자 감속 시간 |
| loserPenaltyDuration | 0.5초 | 패자 감속 시간 |
| crowdThreshold | 3 | 밀집 감쇄 발동 기준 |
| crowdDampen | 0.5 | 밀집 시 충돌 확률 감소율 |

### 슬링샷

| 파라미터 | 기본값 | 설명 |
|----------|--------|------|
| slingshotFactor | 0.02 | brave × 0.02 = 부스트량 |
| slingshotDuration | 1.0초 | 부스트 지속 시간 |
| slingshotMaxBoost | 0.4 | 최대 40% 부스트 |

### Luck/크리

| 파라미터 | 기본값 | 설명 |
|----------|--------|------|
| luckCheckInterval | 3.0초 | 크리 판정 주기 |
| luckCritChance | 0.005 | luck × 0.005 = 확률/3초 |
| luckCritBoost | 1.3 | 크리 시 속도 ×1.3 |
| luckCritDuration | 1.5초 | 크리 지속 시간 |
| luckDodgeChance | 0.02 | luck × 0.02 = 회피 확률 |

### 타입 보너스

| | Runner | Leader | Chaser | Reckoner |
|---|--------|--------|--------|----------|
| Early (0~35%) | +0.15 | +0.08 | 0.00 | -0.05 |
| Mid (35~70%) | -0.05 | +0.05 | +0.10 | +0.03 |
| Late (70~100%) | -0.10 | 0.00 | +0.05 | +0.18 |

### 컨디션

| 파라미터 | 기본값 |
|----------|--------|
| conditionRate: best/good/normal/bad/worst | 0.12 / 0.18 / 0.40 / 0.18 / 0.12 |
| conditionMul: best/good/normal/bad/worst | 1.20 / 1.10 / 1.00 / 0.95 / 0.90 |

---

## 10. 백테스팅 시뮬레이션과 런타임 차이

| 항목 | 런타임 (RacerController) | 백테스팅 (RaceBacktestWindow) |
|------|-------------------------|------------------------------|
| **컨디션** | condMul 적용 (0.90~1.20) | **미적용** (condMul=1.0 취급) |
| **보너스 적용** | `bonuses × condMul` | `bonuses` (그대로) |
| **피로 적용** | `fatigue / condMul` | `fatigue` (그대로) |
| **시간 단위** | `Time.deltaTime` (가변) | `simTimeStep` (고정, 기본 0.05초) |
| **충돌** | CollisionSystem 컴포넌트 | 인라인 SimCollisions (동일 로직) |
| **스킬** | 5회 충돌→스킬 발동 | **미구현** |
| **VFX/애니** | 있음 | 없음 |
| **스탯 기여 추적** | 없음 | 7개 카테고리 추적 |

### 백테스팅 결과 해석 시 주의

1. **컨디션 미반영**: 백테스팅은 "평균적" 상황만 반영. 실제 런타임에서는 컨디션에 의해 ±20%까지 변동 가능
2. **스킬 미반영**: 충돌 5회 후 무조건 승리 효과가 없으므로, Power가 높은 캐릭터의 실제 성적은 백테스팅보다 약간 더 좋을 수 있음
3. **고정 시간 스텝**: 런타임의 가변 프레임과 달리 일정한 시간 간격 → 노이즈/크리 타이밍이 약간 다를 수 있음

---

## 11. 밸런스 조정 체크리스트

### 백테스팅 활용 절차

1. **Unity 에디터** → `DopamineRace > 백테스팅` 실행
2. "전체 트랙 비교 모드" ON, 시뮬레이션 100~500회
3. 로그 파일 확인: `Docs/logs/backtest_YYYYMMDD_HHmmss.md`

### 건강한 밸런스 지표

| 지표 | 목표 범위 | 경고 기준 |
|------|----------|----------|
| 캐릭터 승률 | 5%~15% (8명 기준) | >30% 또는 <2% |
| 캐릭터 평균 순위 | 3.5~5.5 (8명 기준) | 편차 >4 |
| 트랙간 순위 표준편차 | <2.0 | >3.0 (특정 트랙에서만 극단적) |
| 스탯 기여 합계 | 캐릭터간 격차 <0.5 | 격차 >1.0 |

### 조정 우선순위

1. **승률 30% 초과** → 해당 캐릭터의 주력 스탯 관련 설정 약화
2. **트랙간 편차 큼** → 해당 트랙의 특화 배율 조정
3. **특정 스탯이 지배적** → 해당 스탯 계수 (noiseFactor, fatigueFactor 등) 조정
4. **타입간 불균형** → 구간별 보너스 테이블 조정

### 파일 수정 위치

| 조정 대상 | 파일 | 비고 |
|-----------|------|------|
| 캐릭터 스탯 | `Resources/Data/CharacterDB.csv` | 합계 90 유지 권장 |
| 트랙 설정 | `Resources/Data/TrackDB.csv` | weight는 출현 가중치 |
| 공식 파라미터 | `Resources/GameSettings.asset` | Inspector에서 편집 |
| 타입 보너스 | `Resources/GameSettings.asset` | earlyBonus_XXX 등 |
| 컨디션 분포 | `Resources/GameSettings.asset` | conditionRate 합계 1.0 유지 |
