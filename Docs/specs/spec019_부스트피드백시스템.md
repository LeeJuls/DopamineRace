# 명세서: 부스트 피드백 시스템 + 타입 밸런스 튜닝 (2차)
**날짜**: 2026-02-26
**브랜치**: main
**관련 히스토리**: `Docs/history/20260226_부스트피드백시스템_히스토리.md`

---

## 1. 배경 및 목적

### 1.1 이전 세션 결과 (1차 밸런스)
이전 세션(포지션 타겟팅 HP 소모 시스템)에서 타입별 승률 불균형을 어느 정도 해결했으나,
레이스 플레이 중 "가속할 때 확 달리는 느낌이 없다"는 피드백이 남아 있었다.

### 1.2 이번 세션 목표
1. **시각적 임팩트 강화**: 가속 시 체감 속도 차이를 2배 이상으로 높이기
2. **HP-부스트 피드백 루프**: 부스트가 높을수록 HP 소모도 증가 → 리스크/리워드 구조
3. **Power 스탯 활성화**: power가 높을수록 가속 곡선이 가팔라지는 메커니즘
4. **타입 밸런스 재조정**: 피드백 루프 도입 후 무너진 밸런스 재튜닝

---

## 2. 시스템 설계

### 2.1 부스트 피드백 루프

#### 개요
기존: HP 소모율 고정 (타입 기반 상수)
신규: HP 소모율이 현재 부스트 값에 비례하여 증가

#### 공식
```
boostAmp = 1 + boostHPDrainCoeff × max(0, hpBoostValue)
effectiveRate = normalRate × boostAmp
```

#### 동작 원리
- 부스트 낮을 때 (hpBoostValue ≈ 0): boostAmp ≈ 1 → 소모율 변화 없음
- 부스트 높을 때 (hpBoostValue = 0.5): boostAmp = 1 + 1.5 × 0.5 = 1.75 → 소모율 1.75배
- 추입 타입 유리 이유: HP 보존 → consumedRatio 낮게 유지 → 스퍼트 시 폭발적 부스트 → 단, 부스트 높아지면 소모도 급증

#### 확정 파라미터
- `boostHPDrainCoeff = 1.5`

### 2.2 Power 기반 가속 강화

#### 개요
기존: 모든 캐릭터가 동일한 accelExp(부스트 상승 지수) 사용
신규: power 스탯이 높을수록 accelExp가 낮아짐 → 부스트 상승 곡선이 가팔라짐

#### 공식
```
powerFactor = 1 + (charBasePower / 20) × powerAccelCoeff
effectiveAccelExp = accelExp / max(powerFactor, 0.1)
```

#### 예시 (reckoner, accelExp=1.8, powerAccelCoeff=0.5)
| power | powerFactor | effectiveAccelExp | 특성 |
|-------|-------------|-------------------|------|
| 0     | 1.0         | 1.8               | 기본 |
| 10    | 1.25        | 1.44              | 중간 |
| 20    | 1.5         | 1.2               | 선행 수준의 가속 |

#### 확정 파라미터
- `powerAccelCoeff = 0.5`

### 2.3 peakBoost 전 타입 2배+

가속 최대치를 높여 "확 달리는 느낌" 구현:

| 타입 | 이전 | 확정 | 배수 |
|------|------|------|------|
| Runner | 0.08 | 0.20 | 2.5× |
| Leader | 0.09 | 0.20 | 2.2× |
| Chaser | 0.12 | 0.24 | 2.0× |
| Reckoner | 0.16 | 0.24 | 1.5× |

---

## 3. 구현 명세

### 3.1 GameSettings.cs

**추가 필드** (`hpLapReference` 아래):
```csharp
[Header("═══ HP: 부스트 피드백 시스템 ═══")]
[Range(0f, 10f)] public float boostHPDrainCoeff = 3.0f;   // Inspector 기본값, 확정은 1.5
[Range(0f, 2f)]  public float powerAccelCoeff    = 0.5f;
```

**peakBoost Range 확장**:
- `[Range(0.01f, 0.2f)]` → `[Range(0.01f, 0.5f)]` (전 타입 공통)

### 3.2 RacerController.cs

**ConsumeHP() 수정** — 부스트 피드백:
```csharp
float effectiveRate = Mathf.Max(gs.basicConsumptionRate, rate);

// ═══ 부스트 피드백: 부스트가 높을수록 HP 소모 증가 ═══
float boostAmp = 1f + gs.boostHPDrainCoeff * Mathf.Max(0f, hpBoostValue);
effectiveRate *= boostAmp;

float consumption = effectiveRate * Mathf.Sqrt(speedRatio) * deltaTime;
```

**CalcHPBoost() 수정** — Power 가속:
```csharp
// ═══ Power 기반 가속 강화 ═══
float powerFactor = 1f + (charData.charBasePower / 20f) * gs.powerAccelCoeff;
float effectiveAccelExp = accelExp / Mathf.Max(powerFactor, 0.1f);
// (이후 accelExp 대신 effectiveAccelExp 사용)
```

### 3.3 RaceBacktestWindow.cs

**SimConsumeHP()** — RacerController.ConsumeHP() 완전 미러링:
```csharp
float effectiveRate = Mathf.Max(gs.basicConsumptionRate, rate);
float boostAmp = 1f + gs.boostHPDrainCoeff * Mathf.Max(0f, r.hpBoostValue);
effectiveRate *= boostAmp;
```

**SimCalcHPBoost()** — RacerController.CalcHPBoost() 완전 미러링:
```csharp
float powerFactor = 1f + (r.data.charBasePower / 20f) * gs.powerAccelCoeff;
float effectiveAccelExp = accelExp / Mathf.Max(powerFactor, 0.1f);
```

**신규 기능**:
- `segCheckpoints[]`: 구간별 포지션 추적 (10/20/35/50/65/80/90% + 결승)
- `equalStats` 토글: 균등 스탯 테스트 (모든 캐릭터 스탯을 동일 값으로 설정)

### 3.4 CharacterDB.csv

`char_name_kr` 컬럼 추가 (col index 2, `char_name`과 `char_base_speed` 사이):
- 목적: 코드 작성/튜닝 시 캐릭터 식별 편의
- 코드 파싱에 미사용 (CharacterData에 필드 없음)
- **중요**: CharacterData.ParseCSVLine() 컬럼 인덱스 +1 시프트 필요

### 3.5 CharacterData.cs

`ParseCSVLine()` 컬럼 인덱스 조정:
- `cols.Length < 12` → `cols.Length < 13`
- cols[2]~cols[16] → cols[3]~cols[17] (전체 +1)
- 주석에 "cols[2] = char_name_kr (미사용)" 명시

---

## 4. 튜닝 과정 요약

### 4.1 초기 도입 후 불균형
`boostHPDrainCoeff=3.0`, `peakBoost×2` 적용 직후:
- Reckoner 53.7% (목표 35%), Runner 4.0% (목표 15%) — 극심한 불균형

**원인**: 피드백 루프가 Reckoner의 기존 장점(HP 보존)을 기하급수적으로 증폭

### 4.2 주요 인사이트
1. **peakBoost 극도 민감**: 0.24→0.26 (+0.02)만으로 Reckoner 30%→46%
   - 원인: 멱함수 형태의 피드백 루프에서 피크값 변화가 비선형적으로 증폭
   - 해결: peakBoost 고정 후 baseRate로 미세 조정

2. **Reckoner baseRate 패러독스**: baseRate 낮추면 오히려 약해짐
   - 원인: HP 소모 적을수록 consumedRatio 낮음 → 부스트 상승 지연
   - 최적값: 1.1 (보존과 부스트 타이밍의 균형점)

3. **거리별 자연적 타입 계층**: 별도 튜닝 없이 발현
   - 2바퀴: Leader/Runner 유리 (스퍼트 발동 전 HP 이점)
   - 4-5바퀴: Reckoner/Chaser 유리 (후반 부스트 폭발)

### 4.3 확정 파라미터
| 파라미터 | 이전 값 | 확정 값 | 비고 |
|---------|--------|--------|------|
| boostHPDrainCoeff | - | **1.5** | 신규 |
| powerAccelCoeff | - | **0.5** | 신규 |
| runner_peakBoost | 0.08 | **0.20** | 2.5× |
| leader_peakBoost | 0.09 | **0.20** | 2.2× |
| chaser_peakBoost | 0.12 | **0.24** | 2.0× |
| reckoner_peakBoost | 0.16 | **0.24** | 1.5× |
| chaser_activeRate | 6.5 | **5.5** | 스퍼트 완화 |
| reckoner_activeRate | 9 | **7** | 스퍼트 완화 |
| reckoner_baseRate | 0.8 | **1.1** | 보존 소모 적정화 |

---

## 5. 최종 백테스트 결과

**설정**: 균등 스탯 20, 충돌 ON, settling 2.0s, 500회

### 거리별 타입 승률
| 타입 | 목표 | 2바퀴 | **4바퀴 (기준)** | 5바퀴 |
|------|------|-------|-----------------|-------|
| 도주 | 15% | 21.4% | **16.0%** | 13.6% |
| 선행 | 20% | 31.4% | **22.2%** | 18.4% |
| 선입 | 30% | 27.8% | **27.4%** | 33.8% |
| 추입 | 35% | 19.4% | **34.4%** | 34.2% |

### 4바퀴 구간별 포지션 아크
```
구간   도주    선행    선입    추입
10%   2.48   4.34   5.31   5.90  ← 도주 선두, 추입 최후미
50%   4.43   4.10   4.41   5.08  ← 선행 안정 리드
80%   4.46   3.94   4.51   5.11  ← 선행 피크
결승   5.13   4.51   4.04   4.33  ← 선입/추입 역전
```

**평가**: 4바퀴 기준 모든 타입 목표 ±3% 달성 ✅, 거리별 전략 차이 자연 발현 ✅

---

## 6. 알려진 이슈 및 다음 과제

### 6.1 혜성(추입 002) 지배 문제
실제 스탯으로 테스트 시 혜성이 46.8% 승률:
- `power 20` → effectiveAccelExp: 1.8 → 1.2 (추입인데 선행 수준의 가속)
- `brave 20` → 충돌 슬링샷 이득 최대 (+8.07)
- 추입 HP 보존 × Power 빠른 가속 = 이중 시너지
- **근본 원인**: `powerAccelCoeff`가 추입 타입에게 특히 유리

**대응**: 다음 세션에서 캐릭터 스탯 밸런싱 + 타입 밸런스 방향 재검토

### 6.2 다음 세션 방향
- 밸런스 방향 완전 재검토 (현재 구현을 체크포인트로 저장)
- 개별 캐릭터 스탯 튜닝 (혜성 power 하향 등)
- powerAccelCoeff 타입별 차등 적용 검토

---

## 7. 수정 파일 목록

| 파일 | 변경 유형 | 주요 내용 |
|------|---------|---------|
| `Assets/Scripts/Data/GameSettings.cs` | 수정 | boostHPDrainCoeff/powerAccelCoeff 추가, peakBoost Range 확장 |
| `Assets/Scripts/Data/CharacterData.cs` | 수정 | ParseCSVLine 컬럼 인덱스 +1 시프트 |
| `Assets/Scripts/Racer/RacerController.cs` | 수정 | ConsumeHP 피드백, CalcHPBoost Power가속 |
| `Assets/Scripts/Editor/RaceBacktestWindow.cs` | 수정 | SimConsumeHP/SimCalcHPBoost 미러링, 구간 추적, 균등 스탯 토글 |
| `Assets/Resources/GameSettings.asset` | 수정 | 확정 파라미터 값 적용 |
| `Assets/Resources/Data/CharacterDB.csv` | 수정 | char_name_kr 컬럼 추가 |
