# HP 시스템 (지구력 HP) 구현 히스토리

**작성일**: 2026-02-25
**명세서**: `Docs/specs/SPEC-006-지구력HP시스템.md`
**커밋 범위**: `57b279b` ~ `6d56761` (6개 커밋)

---

## 구현 Phase 요약

### Phase 0: 롤백 태그
- `pre-hp-system` 태그 생성 (이전 세션)

### Phase 1: GameSettings HP 파라미터
- `GameSettings.cs`에 HP 시스템 파라미터 20+ 개 추가
- 타입별(Runner/Leader/Chaser/Reckoner) 독립 파라미터 세트
- `GetHPParams()` 메서드로 타입별 파라미터 조회
- `CalculateMaxHP()` 메서드: `hpBase + endurance * hpPerEndurance`
- 커밋: `57b279b`

### Phase 2: RacerController HP 코어
- `enduranceHP`, `maxHP`, `totalConsumedHP`, `hpBoostValue` 필드
- `ConsumeHP()`: basicRate + activeRate 기반 소비, speedRatio 스케일링
- `CalculateHPBoost()`: ratio 기반 가속/감속 곡선 + 탈진 페널티
- `CalculateSpeed()`에서 HP 부스트 typeBonus 대체
- `useHPSystem` 토글로 레거시 시스템 fallback 유지
- 커밋: `20e24d5`

### Phase 3: 백테스팅 SimRacer HP 미러링
- `RaceBacktestWindow.cs`의 SimRacer에 HP 필드/메서드 추가
- `SimConsumeHP()`, `SimCalcHPBoost()` — RacerController와 동일 로직
- `CalcSpeed()`에서 HP 시스템 분기 적용
- 커밋: `b74e23d`

### Phase 4: 포지션 보정
**RacerController** (`b70c611`):
- `UpdateRank()`: 실시간 순위 계산 (TotalProgress 비교)
- `UpdateSlipstreamBlend()`: Chaser 전용 2초 페이드
- `ConsumeHP()` 3종 보정:
  - **Pace Lead** (Leader, 1~3위): activeRate -15%, progress>0.7에서 페이드
  - **Slipstream** (Chaser, 3~7위): basicRate -20% with blend
  - **Conservation Amp** (Reckoner, HP>50%): activeRate 증폭

**BacktestWindow** (`bf616e3`):
- SimRacer에 `currentRank`, `slipstreamBlend` 추가
- 시뮬레이션 루프에 순위 계산 + 슬립스트림 블렌드 업데이트
- `SimConsumeHP()`에 동일 3종 포지션 보정

### Phase 5: 속도 압축 + 수치 튜닝
**핵심 발견 & 해결**:

1. **속도 지배 문제**: SpeedMultiplier 범위 0.81~1.0 (19% 격차)이 HP 부스트 max 12%를 압도
   - → `hpSpeedCompress` 파라미터 도입: midpoint(0.905) 기준 Lerp 압축

2. **고내구 캐릭터 부스트 미작동**: basicConsumptionRate=0.2이 너무 낮아 5바퀴에서도 혜성(HP=100)이 consumedHP 32.5%에 머무름 → boostThreshold(60%) 미달
   - → basicConsumptionRate 0.2→0.8, boostThreshold 0.6→0.4

3. **Runner 장거리 과다 지배**: 태풍(Runner, speed=20, endurance=1)이 HP를 빨리 소진하여 오히려 조기 부스트 획득
   - → runner_activeRate 2.5→3.5, runner_exhaustionFloor -0.05→-0.10

커밋: `6d56761`

---

## 최종 파라미터 세트

| 파라미터 | 초기값 | 최종값 | 설명 |
|---|---|---|---|
| `hpSpeedCompress` | 0 (신규) | **0.85** | 속도 차이 압축률 |
| `basicConsumptionRate` | 0.2 | **0.8** | 기본 HP 소모율 |
| `boostThreshold` | 0.6 | **0.4** | 가속→감속 전환점 |
| `runner_activeRate` | 2.5 | **3.5** | Runner 적극 소모율 |
| `runner_exhaustionFloor` | -0.05 | **-0.10** | Runner 탈진 페널티 |
| `hpBase` | 50 | 50 | 변경 없음 |
| `hpPerEndurance` | 2.5 | 2.5 | 변경 없음 |
| `runner_peakBoost` | 0.12 | 0.12 | 변경 없음 |

---

## 테스트 결과 (300회 시뮬레이션, 일반 트랙)

### 1바퀴 (Runner 우위)
| 순위 | 이름 | 타입 | 승률 |
|---|---|---|---|
| 1 | 태풍 | 도주 | 30.3% |
| 2 | 혜성 | 추입 | 17.3% |
| 3 | 유성 | 도주 | 17.3% |
| 4 | 불꽃 | 선입 | 10.7% |

### 3바퀴 (전환기)
| 순위 | 이름 | 타입 | 승률 |
|---|---|---|---|
| 1 | 태풍 | 도주 | 32.0% |
| 2 | 혜성 | 추입 | 19.3% |
| 3 | 불꽃 | 선입 | 17.0% |
| 4 | 번개 | 선행 | 8.3% |

### 5바퀴 (Reckoner/Chaser 역전)
| 순위 | 이름 | 타입 | 승률 |
|---|---|---|---|
| 1 | 혜성 | 추입 | 42.7% |
| 2 | 불꽃 | 선입 | 25.0% |
| 3 | 섬광 | 선입 | 13.7% |
| 4 | 해일 | 추입 | 5.3% |

**타입별 합산 승률:**

| 타입 | 1바퀴 | 3바퀴 | 5바퀴 |
|---|---|---|---|
| 도주 (Runner) | **48.9%** | 38.3% | 0.3% |
| 선행 (Leader) | 11.7% | 17.0% | 7.7% |
| 선입 (Chaser) | 17.3% | 21.9% | **41.4%** |
| 추입 (Reckoner) | 22.0% | 22.8% | **50.7%** |

---

## 발견된 문제 & 향후 과제

### 해결된 문제
1. ~~속도 스탯 지배~~ → hpSpeedCompress 0.85로 해결
2. ~~고내구 캐릭터 부스트 미달~~ → basicRate/threshold 조정으로 해결
3. ~~Runner 장거리 과다 지배~~ → activeRate/exhaustionFloor 강화로 해결

### 잔여 이슈
1. **혜성 5바퀴 42.7%**: speed=20 + endurance=20 조합이 강력 → 캐릭터 스탯 리밸런싱 필요
2. **선녀 전 구간 최하위**: speed=1, endurance=10 → 스탯 자체가 약함
3. **3바퀴 태풍 32.0%**: Runner→Chaser/Reckoner 전환이 완전하지 않음
4. **트랙별 분화 미검증**: 일반 트랙만 테스트, 다른 트랙(비/눈/사막 등)에서의 영향 미확인

### 추천 후속 작업
- 캐릭터별 스탯 리밸런싱 (특히 선녀 speed 상향, 혜성 endurance 하향)
- 트랙별 백테스팅 실행 및 트랙 특성에 따른 HP 소비 배율 검증
- 런타임 플레이테스트 (시각적 체감 확인)
- SPEC-006 목표 대비 세부 조정 (도주 1바퀴 70% 달성 여부)

---

## 수정 파일 목록

| 파일 | 변경 내용 |
|---|---|
| `Assets/Scripts/Data/GameSettings.cs` | HP 파라미터 20+개 추가, 튜닝값 업데이트 |
| `Assets/Scripts/Racer/RacerController.cs` | HP 코어 시스템 + 포지션 보정 + 속도 압축 |
| `Assets/Scripts/Editor/RaceBacktestWindow.cs` | SimRacer HP 미러링 + 포지션 보정 + 속도 압축 |
| `Assets/Resources/GameSettings.asset` | 직렬화된 파라미터값 |
| `Docs/specs/SPEC-006-지구력HP시스템.md` | 명세서 (Phase 1에서 작성) |
