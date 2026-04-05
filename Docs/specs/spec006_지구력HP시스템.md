# SPEC-006: 지구력 HP 시스템

> 작성일: 2026-02-24
> 상태: 설계 확정 (구현 중)
> 수정: 2026-02-24 — basicRate 0.8→0.2 수정 (QA 리뷰 반영)
> 관련 파일: GameSettings.cs, RacerController.cs, RaceBacktestWindow.cs

---

## 1. 개요

### 1.1 목적
기존 지구력(Endurance) 스탯의 역할을 "피로 페널티"에서 **"HP 기반 전략적 속도 관리"**로 전환한다.
캐릭터 타입(도주/선행/선입/추행)별로 **언제 지구력을 소모하느냐**가 핵심 전략이 된다.

### 1.2 핵심 컨셉
- **지구력 = HP 풀** (레이스 시작 시 만땅, 달리면서 감소)
- **HP 소모 → 속도 부스트** (태울수록 빨라짐, 상한선 존재)
- **타입 = 언제부터 적극적으로 태우냐** (도주는 처음부터, 추행은 72%부터)
- **격차 제약**: 같은 스탯 기준 1등~꼴찌 = **3~4초 차이** (어떤 거리에서든)

### 1.3 설계 원칙
1. 스케일은 작지만 타입 특성은 명확하게
2. 어떤 타입이든 어떤 거리에서든 0% 우승 확률은 없다 (이변 가능)
3. 포지션 보정은 순위를 "확정"하지 않고 "경향"만 만든다
4. 모든 수치는 GameSettings에서 조정 가능

---

## 2. 목표 순위표

### 2.1 타입별 최적 거리

| 타입 | 단거리 (1-2랩) | 중거리 (3랩) | 장거리 (4-5랩) |
|------|--------------|------------|--------------|
| 도주 (Runner) | **1,2등** | 1,3등 | 3,4등 |
| 선행 (Leader) | **1,2등** | **1,2등** | 3,4등 |
| 선입 (Chaser) | 2,3등 | **1,2등** | **1,3등** |
| 추행 (Reckoner) | 2,3등 | 1,3등 | **1,2등** |

### 2.2 이변 확률 (체감 목표)

| | 1랩 | 2랩 | 3랩 | 4랩 | 5랩 |
|--|--|--|--|--|--|
| 도주 우승 | **70%** | 50% | 15% | 8% | 3~5% |
| 선행 우승 | **60%** | 55% | **65%** | 20% | 10% |
| 선입 우승 | 15% | 25% | **60%** | **55%** | **40%** |
| 추행 우승 | 3~5% | 8% | 15% | 45% | **70%** |

이변은 **컨디션 스윙 + 럭 + 충돌 회피**의 조합으로 발생한다.

---

## 3. 핵심 공식

### 3.1 HP 풀

```
maxHP = 50 + charEndurance × 2.5

  내구력 1  → 52.5 HP
  내구력 10 → 75.0 HP (기준점)
  내구력 20 → 100.0 HP
```

### 3.2 HP 소모 공식

```csharp
float consumption = (basicRate + activeRate) * Mathf.Sqrt(currentSpeedRatio) * deltaTime;
enduranceHP -= consumption;
totalConsumedHP += consumption;
```

| 항목 | 설명 |
|------|------|
| `basicRate` | 모든 타입 공통. 달리는 내내 적용 |
| `activeRate` | 타입별 고유값. spurtStartProgress 이후에만 적용 |
| `Sqrt(currentSpeedRatio)` | **댐핑 팩터** — 선형(speed) 대신 제곱근 사용하여 양성 피드백 루프 방지 |
| `deltaTime` | 프레임 시간 |

**왜 제곱근 댐핑인가?**
- 선형(speed 비례): 속도↑ → 소모↑ → 부스트↑ → 속도↑ (런어웨이 위험)
- 제곱근: 속도가 올라도 소모 증가가 완만 → 자연스러운 안정화

### 3.3 속도 부스트 공식 (비율 기반)

```csharp
float consumedRatio = totalConsumedHP / maxHP; // 0.0 ~ 1.0+

float boost;
if (consumedRatio <= 0.6f)
{
    // 가속 구간: 0% ~ 60% 소모
    float t = consumedRatio / 0.6f; // 0 ~ 1 정규화
    boost = peakBoost * Mathf.Pow(t, accelExp);
}
else if (enduranceHP > 0f)
{
    // 감속 구간: 60% ~ 100% 소모
    float t = (consumedRatio - 0.6f) / 0.4f; // 0 ~ 1 정규화
    boost = peakBoost * Mathf.Pow(1f - t, decelExp);
}
else
{
    // 탈진: HP = 0
    boost = exhaustionFloor; // 음수값 (예: -0.05)
}
```

**핵심 파라미터 설명:**

| 파라미터 | 역할 | 범위 |
|---------|------|------|
| `peakBoost` | 60% 소모 시 최대 부스트 | +0.09 ~ +0.12 |
| `accelExp` | 가속 곡선 형태. 높을수록 후반 급격 | 1.0 ~ 2.0 |
| `decelExp` | 감속 곡선 형태. 높을수록 완만하게 하락 | 0.5 ~ 2.0 |
| `exhaustionFloor` | HP 0 이후 속도 페널티 | -0.03 ~ -0.05 |

**입력이 비율(consumed/maxHP)인 이유:**
- 내구력 1이든 20이든 60% 지점에서 동일하게 피크 도달
- 내구력이 높으면: HP 풀이 커서 60% 도달 시점이 레이스에서 더 늦음
- → 내구력 = "고속 구간을 얼마나 오래 유지하느냐"

### 3.4 최종 속도 공식

```csharp
float finalSpeed = baseSpeed
    * charSpeedMultiplier        // CharacterData.SpeedMultiplier
    * conditionMultiplier        // 컨디션 보정
    * (1f + boost + positionBonus) // HP 부스트 + 포지션 보정
    + noiseValue;                // 럭/노이즈
```

---

## 4. 타입별 프로필

### 4.1 도주 (Runner) — "초반 폭발, 후반 버팀"

```
spurtStartProgress: 0.00 (처음부터)
activeRate:         높음
peakBoost:          +0.12 (1.12x)
accelExp:           1.5 (지수형 — 후반 급격 상승)
decelExp:           0.8 (급격한 감속)
exhaustionFloor:    -0.05 (0.95x)
```

**속도 프로필:**
```
1바퀴: |▲▲▲■■▼▼×| → 평균 +4~5% → 1,2등
3바퀴: |▲■▼×××| → 평균 +1% → 1,3등
5바퀴: |▲■▼×××××| → 평균 -1% → 3,4등
```

**설계 의도:**
- 짧은 거리에서 강력. 빨리 피크에 도달하고 빨리 소진
- 장거리에서는 탈진하지만 0.95x 바닥 덕분에 완전 붕괴는 아님
- charEndurance가 높은 도주 캐릭터: 탈진 시점이 늦어져 중거리에서도 선전 가능

### 4.2 선행 (Leader) — "꾸준한 선두, 오래 버티기"

```
spurtStartProgress: 0.10
activeRate:         중간
peakBoost:          +0.09 (1.09x)
accelExp:           1.2 (완만한 가속)
decelExp:           1.8 (매우 완만한 감속)
exhaustionFloor:    -0.03 (0.97x)
```

**속도 프로필:**
```
1바퀴: |▲▲▲■■▼| → 평균 +3~4% → 1,2등
3바퀴: |▲▲▲■■■▼▼| → 평균 +3% → 1,2등
5바퀴: |▲▲■■▼▼▼×| → 평균 +0.5% → 3,4등
```

**설계 의도:**
- 피크는 낮지만(0.09) 감속이 매우 완만(decelExp 1.8) → "꾸준히 빠름"
- 단거리에서도 도주와 경쟁 가능 (감속이 거의 없으므로)
- 장거리에서는 결국 감속 + 탈진이 누적되어 후반 타입에게 밀림

### 4.3 선입 (Chaser) — "중간에서 힘 모아 치고 나가기"

```
spurtStartProgress: 0.45
activeRate:         중상
peakBoost:          +0.11 (1.11x)
accelExp:           1.3 (중간 가속)
decelExp:           1.5 (완만한 감속)
exhaustionFloor:    -0.04 (0.96x)
```

**속도 프로필:**
```
1바퀴: |....▲▲■■▼| → 평균 +2~3% → 2,3등
3바퀴: |..▲▲■■■▼| → 평균 +3% → 1,2등
5바퀴: |..▲▲■■■▼▼| → 평균 +2.5% → 1,3등
```

**설계 의도:**
- 높은 피크(0.11) + 완만한 감속(1.5) = 중후반에 안정적으로 강함
- 단거리에서는 가속 시작(45%)이 늦어 불리하지만 2~3등은 가능
- 3바퀴에서 선행과 박빙 (선행 = 오래 유지 vs 선입 = 높은 피크)
- 5바퀴에서도 안정적 성적 (추행과 경쟁)

### 4.4 추행 (Reckoner) — "마지막에 모든 것을 쏟아붓기"

```
spurtStartProgress: 0.72
activeRate:         매우 높음
peakBoost:          +0.12 (1.12x, 도주와 동일)
accelExp:           2.0 (매우 급격한 가속 — 폭발)
decelExp:           0.5 (급격한 감속)
exhaustionFloor:    -0.05 (0.95x)
```

**속도 프로필:**
```
1바퀴: |........▲■| → 평균 +1~2% → 2,3등
3바퀴: |......▲■▼| → 평균 +2% → 1,3등
5바퀴: |......▲■■▼| → 평균 +3% → 1,2등
```

**설계 의도:**
- 피크는 도주와 동일(0.12)하지만 도달 곡선이 극단적(accelExp 2.0)
- 72% 이후 급격히 올라가서 짧은 구간에 집중 폭발
- 단거리에서는 폭발 구간(28%)이 짧아 불리하지만, 2~3등은 가능
- 장거리에서는 Conservation Amplifier로 폭발 강도 증폭 → 가장 유리

---

## 5. 서브 메커닉

### 5.1 Pace Lead — 선행(Leader) 전용

**조건:** 현재 순위 1~3위 (12명 기준)

**효과:**
```csharp
float paceLeadEffect = paceLeadReduction; // 0.15 (15% 소모 감소)

// 레이스 후반 약화 (progress > 0.7부터 선형 감소)
if (overallProgress > 0.7f)
{
    float fade = 1f - (overallProgress - 0.7f) / 0.3f; // 1.0 → 0.0
    paceLeadEffect *= fade;
}

activeRate *= (1f - paceLeadEffect);
```

**후반 약화 이유:**
- 자기강화 루프 방지 (1위 유지 → 계속 절약 → 영구 1위)
- 3바퀴에서 선입이 후반에 역전할 수 있는 여지 확보
- "페이스를 지배하다가 후반에 약간 풀리는" 현실 경마 패턴

### 5.2 Slipstream — 선입(Chaser) 전용

**조건:** 현재 순위 3~7위 (12명 기준, 상위 25%~60%)

**효과:**
```csharp
float slipstreamEffect = slipstreamReduction; // 0.20 (기본 소모 20% 감소)
basicRate *= (1f - slipstreamEffect);
```

**전환 처리 (진동 방지):**
```csharp
// 2초 페이드인/아웃
float targetSlipstream = IsInSlipstreamRange() ? 1f : 0f;
slipstreamBlend = Mathf.MoveTowards(slipstreamBlend, targetSlipstream, deltaTime / 2f);
slipstreamEffect *= slipstreamBlend;
```

**범위가 3~7위인 이유:**
- 초반에 선입은 7위 이하로 밀릴 수 있음 (도주/선행이 상위 차지)
- 3~5위는 너무 좁아서 발동 자체가 어려움
- 3~7위 = "중간 집단" = 선입의 전략 포지션과 일치

### 5.3 Conservation Amplifier — 추행(Reckoner) 전용

**공식:**
```csharp
float remainingRatio = enduranceHP / maxHP; // 버스트 시작 시점의 잔여 HP%
float amplifier = 1f + Mathf.Max(0f, remainingRatio - 0.5f) * ampCoeff;
// ampCoeff = 0.6 (GameSettings)

// activeRate에 곱셈 → 피크 "도달 속도" 증폭
effectiveActiveRate = activeRate * amplifier;
```

**예시 (ampCoeff = 0.6):**

| 잔여 HP% | amplifier | 효과 |
|----------|-----------|------|
| 85% | 1.21 | 21% 빠르게 피크 도달 |
| 75% | 1.15 | 15% 빠르게 |
| 60% | 1.06 | 6% 빠르게 |
| 50% 이하 | 1.00 | 보너스 없음 |

**"도달 속도" 증폭이지 피크 자체를 올리지 않는 이유:**
- 글로벌 피크 캡(+0.12)을 넘지 않음
- 대신 **더 빠르게 피크에 도달** = 고속 구간이 더 길어짐
- 장거리에서 추행이 HP를 많이 남겨둘수록 폭발이 더 효율적

---

## 6. 60/40 임계점 상세

### 6.1 내구력별 임계점 도달 시점 (레이스 진행률)

기준: 내구력 10 (75 HP). 타입 고유 소모율 적용.

| 타입 | 60% 임계점 | 탈진(HP=0) | 비고 |
|------|-----------|-----------|------|
| 도주 | ~35% | ~70% | 조기 피크, 조기 탈진 |
| 선행 | ~70% | 레이스 내 미도달 | 오래 유지 |
| 선입 | ~85% | 레이스 내 미도달 | 늦은 피크, 거의 감속 없음 |
| 추행 | ~92% | 레이스 내 미도달 | 극후반 피크 |

### 6.2 내구력에 따른 변동 범위

| 타입 | 내구력 1 (52.5 HP) | 내구력 10 (75 HP) | 내구력 20 (100 HP) |
|------|-------------------|------------------|-------------------|
| 도주 60% 임계점 | ~28% | ~35% | ~44% |
| 선행 60% 임계점 | ~60% | ~70% | ~80% |

→ **내구력이 높을수록 피크 도달이 늦지만 고속 구간이 더 길다**

---

## 7. 격차 제약 검증

### 7.1 목표: 1등~꼴찌 3~4초 차이

| 거리 | 기준 시간 | 허용 격차 | 평균속도 차 |
|------|---------|---------|-----------|
| 1바퀴 | ~60초 | 3~4초 | ±3~5% |
| 3바퀴 | ~180초 | 3~4초 | ±1.5~2.5% |
| 5바퀴 | ~300초 | 3~4초 | ±1~1.5% |

### 7.2 타입별 예상 평균 속도 (내구력 10 기준)

| 타입 | 1바퀴 | 3바퀴 | 5바퀴 |
|------|------|------|------|
| 도주 | +4~5% | +1% | -1% |
| 선행 | +3~4% | +3% | +0.5% |
| 선입 | +2~3% | +3% | +2.5% |
| 추행 | +1~2% | +2% | +3% |

```
1바퀴 격차: +5% ~ +1% = 4% → 60초 × 4% ≈ 2.4초 ... 약간 부족
  → activeRate 미세 조정 or 컨디션/럭으로 3초 이상 벌어짐

3바퀴 격차: +3% ~ +1% = 2% → 180초 × 2% ≈ 3.6초 ✓
5바퀴 격차: +3% ~ -1% = 4% → 300초 × 4% ÷ (1+avg) ≈ 3.8초 ✓
```

---

## 8. 이변 시나리오

### 8.1 이변을 만드는 3가지 요소

| 요소 | 영향 범위 | 비고 |
|------|---------|------|
| 컨디션 | ±2~3% | 캐릭터별 매 레이스 변동 |
| 럭/노이즈 | ±1% | 프레임별 랜덤 |
| 충돌 | 1~3초 손실 | 밀집 순위군에서 발생 |

### 8.2 이변 시나리오 예시

**도주가 5바퀴에서 우승:**
```
전제: 도주 컨디션 최상(+3%), 추행 컨디션 최하(-3%)
1~2랩: 도주 폭발적 선두 (10초+ 리드)
3랩:   도주 탈진 시작, 하지만 리드 유지
4~5랩: 추행/선입 상위권 밀집 → 충돌로 감속
결과:  도주 리드 유지하며 1위 (확률 3~5%)
```

**추행이 1바퀴에서 우승:**
```
전제: 추행 컨디션 최상(+2%), 도주/선행 컨디션 하(−2%)
출발~50%: 도주/선행 선두 다툼 → 충돌 감속!
72~100%: 추행 폭발 (Conservation Amp + 컨디션 보정)
결과:  추행 사진 판정 1위 (확률 3~5%)
```

### 8.3 이변 불가능 방지 체크

컨디션 스윙이 **±2~3%** 이면:
- 5바퀴(300초)에서 최대 스윙: ±6~9초 → 타입 격차(3~4초) 초과 가능 → 이변 가능 ✓
- 1바퀴(60초)에서 최대 스윙: ±1.2~1.8초 → 충돌(+1~2초)과 합산 시 가능 ✓

---

## 9. 기존 시스템과의 관계

### 9.1 교체되는 시스템

| 기존 | 변경 후 |
|------|--------|
| `GetFatigue()`: progress × (1/endurance) × fatigueFactor | HP 소모 기반 부스트/감속 곡선 |
| `GetTypeBonus()`: progress 기반 고정 보너스 | HP 소모 + 타입별 spurtStart + 포지션 보정 |
| `fatigueFactor` (GameSettings) | basicRate, exhaustionFloor |
| `earlyBonus_Runner/Leader/Chaser/Reckoner` | activeRate, peakBoost, accelExp, decelExp |

### 9.2 유지되는 시스템

| 시스템 | 변경 없음 |
|--------|---------|
| `charBaseSpeed` → `SpeedMultiplier` | 그대로 유지 (0.8 + charBaseSpeed × 0.01) |
| 컨디션 시스템 | 그대로 유지 (conditionMultiplier) |
| 노이즈/럭 | 그대로 유지 |
| 충돌 시스템 | 그대로 유지 |
| 배당 계산 (OddsCalculator) | 그대로 유지 |

### 9.3 `RacerController` 추가 필드

```csharp
// === HP 시스템 필드 ===
private float enduranceHP;        // 현재 HP (maxHP에서 시작, 0까지 감소)
private float maxHP;              // 50 + charEndurance * 2.5
private float totalConsumedHP;    // 누적 소모량 (부스트 계산용)

// === 포지션 보정 ===
private float slipstreamBlend;    // 0~1, 2초 페이드
private int currentRank;          // 현재 순위 (1~12)
```

---

## 10. GameSettings 추가 파라미터

### 10.1 공통 파라미터

```csharp
[Header("=== HP 시스템 공통 ===")]
public float hpBase = 50f;              // maxHP = hpBase + endurance * hpPerEndurance
public float hpPerEndurance = 2.5f;
public float basicConsumptionRate = 0.2f;   // 기본 소모율 (전 타입 공통) — QA 리뷰: 0.8→0.2 하향
public float boostThreshold = 0.6f;         // 가속→감속 전환 임계점 (60%)
public float exhaustionDecayRate = 0.02f;   // 탈진 후 부스트 감소 속도 (/초)
```

### 10.2 타입별 파라미터

```csharp
[Header("=== 도주 (Runner) ===")]
public float runner_spurtStart = 0.00f;
public float runner_activeRate = 2.5f;
public float runner_peakBoost = 0.12f;
public float runner_accelExp = 1.5f;
public float runner_decelExp = 0.8f;
public float runner_exhaustionFloor = -0.05f;

[Header("=== 선행 (Leader) ===")]
public float leader_spurtStart = 0.10f;
public float leader_activeRate = 1.5f;
public float leader_peakBoost = 0.09f;
public float leader_accelExp = 1.2f;
public float leader_decelExp = 1.8f;
public float leader_exhaustionFloor = -0.03f;

[Header("=== 선입 (Chaser) ===")]
public float chaser_spurtStart = 0.45f;
public float chaser_activeRate = 2.0f;
public float chaser_peakBoost = 0.11f;
public float chaser_accelExp = 1.3f;
public float chaser_decelExp = 1.5f;
public float chaser_exhaustionFloor = -0.04f;

[Header("=== 추행 (Reckoner) ===")]
public float reckoner_spurtStart = 0.72f;
public float reckoner_activeRate = 3.0f;
public float reckoner_peakBoost = 0.12f;
public float reckoner_accelExp = 2.0f;
public float reckoner_decelExp = 0.5f;
public float reckoner_exhaustionFloor = -0.05f;
```

### 10.3 포지션 보정 파라미터

```csharp
[Header("=== 포지션 보정 ===")]
public float paceLeadReduction = 0.15f;      // Pace Lead: activeRate 15% 감소
public float paceLeadFadeStart = 0.7f;       // 70% 이후 효과 감소 시작
public int paceLeadMaxRank = 3;              // 1~3위에서 발동

public float slipstreamReduction = 0.20f;    // Slipstream: basicRate 20% 감소
public int slipstreamMinRank = 3;            // 3위부터
public int slipstreamMaxRank = 7;            // 7위까지
public float slipstreamFadeTime = 2.0f;      // 전환 페이드 시간 (초)

public float conservationAmpCoeff = 0.6f;    // Conservation Amplifier 계수
```

---

## 11. 백테스팅 연동

### 11.1 SimRacer 수정 사항

`RaceBacktestWindow.cs`의 `SimRacer` 클래스에 동일한 HP 시스템 적용:

```
추가 필드: enduranceHP, maxHP, totalConsumedHP, currentRank
소모 공식: 실제 RacerController와 동일
부스트 공식: 실제 RacerController와 동일
포지션 보정: 순위 기반 (SimRacer 간 비교)
```

### 11.2 검증 항목

백테스팅으로 확인해야 할 사항:
1. 1/3/5바퀴에서 목표 순위표(§2.1)와 일치하는가
2. 1등~꼴찌 격차가 3~4초 이내인가
3. 이변 확률이 체감 목표(§2.2)에 근접하는가
4. 내구력 1 vs 20에서 의미있는 차이가 있는가
5. 양성 피드백 루프가 발생하지 않는가 (속도 발산 없음)

---

## 12. 구현 순서

```
Phase 1: 기반 구현
  1. GameSettings에 §10 파라미터 추가
  2. RacerController에 HP 필드 추가 + ResetRacer 수정
  3. RacerController.CalculateSpeed에서 기존 GetFatigue/GetTypeBonus를 HP 시스템으로 교체
  4. 기존 fatigue/typeBonus 코드는 주석 처리 (삭제하지 않음, fallback 유지)

Phase 2: 포지션 보정
  5. RacerController에 currentRank 업데이트 로직 추가
  6. Pace Lead (Leader), Slipstream (Chaser), Conservation Amp (Reckoner) 구현
  7. 포지션 보너스 페이드인/아웃 구현

Phase 3: 백테스팅 연동
  8. SimRacer에 HP 시스템 미러링
  9. 시뮬레이션 실행 + 결과 분석
  10. 파라미터 튜닝 (목표 순위표 + 격차 3~4초)

Phase 4: 문서 갱신
  11. BALANCE_GUIDE.md 업데이트
  12. 히스토리 문서 작성
```

---

## 부록 A: 용어 정리

| 용어 | 설명 |
|------|------|
| HP (Hit Point) | 지구력 자원. 레이스 시작 시 maxHP, 달리면서 감소 |
| maxHP | 최대 HP = 50 + charEndurance × 2.5 |
| consumed% | totalConsumedHP / maxHP (0~1+) |
| 60/40 임계점 | consumed% = 0.6 기준, 이전은 가속 / 이후는 감속 |
| peakBoost | 60% 도달 시 최대 속도 부스트 값 |
| accelExp | 가속 곡선 지수. 높을수록 후반 급격 |
| decelExp | 감속 곡선 지수. 높을수록 완만 |
| spurtStartProgress | 적극 소모 시작 시점 (OverallProgress 기준) |
| Pace Lead | 선행 전용. 상위 순위일 때 HP 소모 절감 |
| Slipstream | 선입 전용. 중간 순위일 때 기본 소모 절감 |
| Conservation Amplifier | 추행 전용. 잔여 HP가 많을수록 버스트 강도 증폭 |
| exhaustionFloor | HP 0 이후 적용되는 속도 페널티 (음수값) |
| OverallProgress | (currentLap + lapProgress) / totalLaps (0~1) |

---

## 부록 B: 이전 설계와의 차이점 요약

| 항목 | 이전 (v1) | 현재 (v2) |
|------|---------|---------|
| 피크 부스트 | 1.5x (50%) | 1.09~1.12x (9~12%) |
| 탈진 바닥 | 0.88x | 0.95~0.97x |
| 속도 부스트 입력 | 절대값 (미정의) | **비율 기반** (consumed/maxHP) |
| 소모 기준 | 속도 선형 비례 | **속도 제곱근 비례** (댐핑) |
| 추행 시작점 | 78% | 72% |
| 선입 시작점 | 50% | 45% |
| Pace Lead 범위 | 1~2위 | 1~3위, **후반 약화** |
| Slipstream 범위 | 3~5위 | **3~7위** |
| Conservation Amp | 피크 속도 증폭 | **피크 도달 속도** 증폭 |
| 감속 곡선 | 미정의 | **decelExp 공식화** |
| 기본 소모율 | 미정의 | **basicRate 명시** |
| 격차 제약 | 없음 | **3~4초 이내** |
