# 초반 충돌 억제 (Collision Settling Phase) 히스토리

**날짜**: 2026-02-25
**작업 범위**: 레이스 시작 시 충돌 억제 → 속도 기반 대열 정리
**수정 파일**: GameSettings.cs, GameSettings.asset, CollisionSystem.cs, RaceBacktestWindow.cs

---

## 1. 유저 피드백

> "파워가 높은(20) 추입 캐릭터(해일)가 초반에 다 밀치면서 선두권으로 나간다.
>  속도를 일부러 줄이지는 않으니까 선두권을 계속 유지하다가 막판에 스퍼트를 내니까 답이 없다."

## 2. 근본 원인

- 12명이 뭉쳐서 출발 → 충돌 연쇄 발생
- 파워 높은 캐릭터가 충돌 승리 반복 (Power=20 해일: 승률 ~63%)
- 패자 -30% 감속 0.5초 → 상대적으로 파워 캐릭터가 앞으로 밀려남
- earlyBonus(+12% 도주)로 속도 차이를 줘도 충돌 연쇄가 속도 차이를 씹음

## 3. 검토한 해결 방안

| 방안 | 설명 | 판정 |
|------|------|------|
| A. 줄세우기 (Starting Grid) | 타입별 출발 위치 배치 | ⚠️ 자유도 감소 |
| B. Negative EarlyBonus | 추입/선입 초반 속도 억제 | ⚠️ 충돌이 강하면 부족 |
| C. A+B 결합 | 물리배치 + 속도억제 | ❌ 도주/선행 이중버프 |
| **D. 초반 충돌 억제** | **시작 후 N초간 충돌 OFF** | **✅ 채택** |

**D안 채택 이유**:
- 이중 버프/너프 문제 없음
- 줄세우기 필요 없음 (배치 자유도 유지)
- earlyBonus가 설계 의도대로 작동 (속도 기반 분리)
- 구현 간단 (충돌 체크에 시간 조건 한 줄 추가)

## 4. 구현 내용

### 새 파라미터
| 파라미터 | 값 | 목적 |
|---------|---|------|
| collisionSettlingTime | 2.0 | 레이스 시작 후 충돌 억제 시간 (초) |

### GameSettings.cs
- `collisionSettlingTime = 2.0f` 필드 추가 (crowdDampen 아래)

### CollisionSystem.cs
- `raceElapsed` 타이머 추가 (Update에서 deltaTime 누적)
- `CheckCollisions()` 첫 줄: `if (raceElapsed < gs.collisionSettlingTime) return;`
- `ClearAll()`: `raceElapsed = 0f` 리셋

### RaceBacktestWindow.cs
- `SimCollisions()` 첫 줄: `if (simTime < gs.collisionSettlingTime) return;`
- 결과 로그 헤더에 settling 정보 표시

### GameSettings.asset
- `collisionSettlingTime = 2.0` 적용 완료

## 5. 동작 흐름

```
레이스 시작 (0초)
│
├─ 0~2초: Settling Phase (충돌 OFF)
│   도주: earlyBonus +12% → 확 튀어나감
│   선행: earlyBonus +4%  → 선두권 자리잡음
│   선입: earlyBonus +2%  → 중위권 정착
│   추입: earlyBonus  0%  → 자연스럽게 후미
│   ★ 파워=20 해일도 속도가 느려 뒤에 있을 수밖에 없음
│
├─ 2초 이후: 충돌 ON
│   대열이 이미 정리됨
│   해일은 후미에서 충돌 이기며 서서히 전진
│
└─ 후반 (70~80%): 추입 스퍼트
    HP peakBoost 0.16 폭발 → 역전 드라마
```

## 6. 검증

- Unity 컴파일: 에러 없음
- MCP 검증: `collisionSettlingTime = 2` 확인
- 백테스트: settling 정보가 결과 헤더에 표시됨

## 7. 확인 필요 사항

- [ ] 런타임 플레이테스트: 2초 동안 대열 정리 체감
- [ ] 백테스트: settling ON/OFF 비교 (simCollision 토글)
- [ ] 해일(파워20 추입)이 초반 후미 정착 확인
- [ ] settling 시간 조정 필요 시: GameSettings Inspector에서 0~5초 슬라이더
