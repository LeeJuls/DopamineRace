---
title: HP / CP 시스템
created: 2026-06-21
updated: 2026-06-21
type: concept
tags: [시스템, HP, CP, 밸런스]
related: [[V4_레이스_시스템]], [[../캐릭터/타입별_전략]]
sources: [Docs/specs/spec006_지구력HP시스템.md, Docs/history/his028_V4레이스시스템구간제속도드레인.md]
confidence: high
---

# HP / CP 시스템

> **상태**: ✅ 완료  
> **핵심**: 지구력(HP)이 레이스 중 거리 비례 소모 → 장거리일수록 스태미나 중요

---

## HP (지구력)

### 공식

```
MaxHP = 80 + Stamina × 3

예:
  Stamina  1 → MaxHP  83
  Stamina 10 → MaxHP 110
  Stamina 20 → MaxHP 140
```

### 드레인

```
drain = v4_drainPerLap × totalLaps × progressDelta
```

- `progressDelta`: 이번 프레임의 진행도 증가량 (0~1 정규화)
- `totalLaps`: 레이스 총 바퀴 수
- 결과: **바퀴 수에 비례**해서 소모 → 단거리/장거리 전략 차별화

### 부스트/스퍼트 구간 추가 드레인

타입 부스트 구간·최종 스퍼트에서 기본 달리기보다 HP 소모 증가.  
→ "전력질주할수록 체력 빨리 닳는" 현실감.

---

## CP (체력 포인트 / Critical Power)

> CP는 HP 소모 과정에서 특정 임계치 이하로 떨어질 때 발동하는 부스트 관련 수치.

- HP 특정 % 이하 → CP 발동 조건 활성
- 타입별로 CP 활용 전략 상이

→ 상세 구현: `Assets/Scripts/Racer/RacerController.cs`

---

## 밸런스 캘리브레이션

| 조건 | 목표 |
|------|------|
| Stamina 20, 5바퀴 | HP 20% 잔여 |
| Stamina 1, 5바퀴 | HP 거의 소진 (탈력 위험) |
| 단거리 (2바퀴) | HP 관리 부담 낮음 |
| 장거리 (5바퀴) | HP 관리 필수 |

---

## 관련 히스토리

- `his028`: HP 드레인 절대 거리 기반 수정 (progressDelta 정규화 오류 수정)
- `his031`: STA 밸런스 재조정

→ Raw: `Docs/specs/spec006_지구력HP시스템.md`
