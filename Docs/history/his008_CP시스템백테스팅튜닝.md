# CP 시스템 + 백테스팅 매직넘버 튜닝 히스토리

**날짜**: 2026-02-25
**작업 범위**: CP 시스템 구현 + 10라운드 백테스팅으로 4타입 균형 튜닝
**수정 파일**: GameSettings.cs, GameSettings.asset, RacerController.cs, RaceBacktestWindow.cs

---

## 1. 구현 내용

### CP (Calm Points) 시스템
- **자원 체계**: HP(지구력 기반 체력) + CP(침착 기반 정신력) 이중 자원
- `maxCP = charBaseCalm × cpMultiplier(10)`
- 기본 소모: 0.5/s, 슬립스트림 활성 시 +2.5/s

### 개선 슬립스트림
- 기존 Chaser 전용 → **전체 타입 사용 가능**
- 기존 순위 기반 → **거리 기반** (universalSlipstreamRange = 0.08)
- HP 절감 방식 → **CP 소모 + 직접 속도 보너스** (+1.5%)
- Chaser 특성: 슬립스트림 효율 ×1.1

### CP/HP 불안정 노이즈
- CP 50% 이하: 슬립스트림 효율 감소 + 노이즈 ×2.0
- HP 40% 이하: 노이즈 ×2.5
- 두 배율은 곱연산

### 제거된 시스템
- TrailingBonus (순위 기반 하위권 부스트)
- Chaser 전용 HP slipstream (HP basicRate 절감)
- slipstreamMinRank/MaxRank (순위 기반 발동 조건)

---

## 2. 백테스팅 과정 (10라운드)

### 핵심 발견사항

1. **hpSpeedCompress=0.85의 영향**: 기본 속도 차이가 ~0.83%로 압축됨 → HP 부스트 적분이 레이스 결과의 주요 결정 요인

2. **boostThreshold=0.4**: HP 40% 소모 시 부스트 피크 → 이전 가정(0.6)과 다름

3. **activeRate 중요성**: Leader(기존 1.5)와 Chaser(기존 2.0)는 HP 소모 속도가 너무 느려 3랩 레이스 내 부스트 피크 도달 불가 → 3.0, 3.5로 대폭 증가

4. **decelExp 민감도**: Chaser가 경쟁력을 갖추려면 decelExp=0.8 필수 (느린 감쇄 = 지속적 부스트)

### 라운드별 요약

| 라운드 | 핵심 변경 | 결과 (R/L/C/K %) |
|--------|----------|-------------------|
| 8 (A-D) | hpSpeedCompress 포함 첫 테스트 | R만 우승, L/C=0% |
| 8b (E-H) | Leader aR=3.0, Chaser aR=3.5 | Leader/Chaser 첫 등장 |
| 8c (I-L) | 최적 요소 병합 | I: 21/36/0/42 |
| 8d (M-P) | Chaser decel=0.8 추가 | **M: 14/36/17/32 ← 4타입 첫 우승** |
| 9 (Q-T) | Set M 미세조정 | S: 7/21/37/34 |
| 10 (U-X) | 최종 정밀 조정 | **W: 12/30/24/32 ← 최종 채택** |

---

## 3. 최종 Set W 파라미터

### HP 타입 커브 (Set W)

| 타입 | spurtStart | activeRate | peakBoost | accelExp | decelExp | exhaustFloor |
|------|-----------|-----------|----------|---------|---------|-------------|
| Runner | 0.00 | 3.5 | 0.10 | 1.5 | 0.7 | -0.08 |
| Leader | 0.05 | 3.0 | 0.10 | 1.2 | 1.0 | -0.02 |
| Chaser | 0.15 | 3.5 | 0.12 | 1.3 | 0.8 | -0.03 |
| Reckoner | 0.30 | 4.5 | 0.16 | 1.8 | 0.4 | -0.01 |

### HP 초반 보너스

| 타입 | earlyBonus | FadeEnd |
|------|-----------|---------|
| Runner | 0.06 | 0.40 |
| Leader | 0.04 | 0.40 |
| Chaser | 0.02 | 0.40 |
| Reckoner | 0.00 | 0.40 |

### CP 시스템

| 필드 | 값 |
|------|-----|
| cpMultiplier | 10 |
| cpBasicDrain | 0.5 |
| cpSlipstreamDrain | 2.5 |
| universalSlipstreamRange | 0.08 |
| universalSlipstreamBonus | 0.015 |
| chaserSlipstreamMult | 1.1 |
| cpWeakThreshold | 0.5 |
| cpMinEfficiency | 0.5 |
| cpLowNoiseMul | 2.0 |
| hpUnstableThreshold | 0.4 |
| hpLowNoiseMul | 2.5 |

---

## 4. 타입별 설계 의도

- **Runner(도주)**: 즉시 부스트 시작, 초반 강세, 후반 급감쇄 → 초반 리드로 도주
- **Leader(선행)**: 약간의 딜레이 후 안정적 부스트, 페이스리드 보정 → 중상위권 운영
- **Chaser(선입)**: 중반 부스트 진입, 느린 감쇄(dc=0.8), 슬립스트림 보너스 → 추월 레이스
- **Reckoner(추행)**: 후반 폭발적 부스트(pk=0.16), 급가속 → 막판 뒤집기

---

## 5. 검증 결과

- Unity 컴파일: 에러 없음
- MCP 스크립트 검증: 모든 asset 값 정상
- 100레이스 백테스트: R=12% L=30% C=24% K=32% (목표 ~25% 균등)
- 트랙 다양성에 의한 자연스러운 편차 확인 (Highland→Reckoner, Desert→Chaser 등)
