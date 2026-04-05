# SPEC-008 — 트랙 프로그레스 바 (좌측 HUD 패널)

> 작성일: 2026-03-05
> 상태: **완료 (구현 완료)**
> 관련 커밋: `09e630f` ~ `d9b6599`

---

## 1. 개요

레이싱 HUD 좌측 텍스트 순위 목록(`RankPanel`)을 **세로 트랙 바 + 색깔 원형 마커** 방식으로 전면 교체한다.
우마무스메 스타일의 직관적인 레이서 진행도 시각화가 목적이다.

---

## 2. UI 계층 구조

```
RacingUI
├── TrackProgressPanel   (좌측, anchorMin=(0.01,0.03) ~ anchorMax=(0.08,0.97))
│   ├── GoalLabel        (Text "GOAL", 36pt, 앵커 상단)
│   ├── TrackBarArea     (세로 바 영역, 패널 내부 중앙)
│   │   ├── BarBg        (Image: 흰색 세로 막대, 6px 폭)
│   │   ├── LapDivider_N (수평 구분선, 14×6px, 노란빛, 동적 생성)
│   │   └── RacerCircle_N (색깔 원형 마커, 40×40px, 동적 생성)
│   └── StartLabel       (Text "START", 36pt, 앵커 하단)
├── MyBetText            (기존 유지)
├── RacingRoundText      (기존 유지)
└── RaceTimerText        (기존 유지)
```

---

## 3. 레이서 원형 마커

### 3.1 기본 구조
| 요소 | 크기 | 설명 |
|------|------|------|
| `CircleOutline` | 50×50px | 배팅 마커 전용 노란 테두리 (비배팅 시 투명) |
| `CircleBg` | 40×40px | 포지션 컬러 채워진 원형 (코드 생성 텍스처) |
| `NumberText` | - | 18pt, 중앙 정렬, 어두운 원=흰 글씨/밝은 원=검정 글씨 |

### 3.2 포지션 컬러 (1번~9번 고정)
| 번호 | 색상 | RGB |
|------|------|-----|
| 1 | 빨강 | (0.92, 0.23, 0.23) |
| 2 | 주황 | (0.96, 0.55, 0.14) |
| 3 | 진한 노랑 | (0.95, 0.85, 0.10) |
| 4 | 초록 | (0.18, 0.72, 0.28) |
| 5 | 파랑 | (0.20, 0.52, 0.92) |
| 6 | 남색 | (0.27, 0.24, 0.80) |
| 7 | 보라 | (0.62, 0.23, 0.82) |
| 8 | 회색 | (0.72, 0.72, 0.72) |
| 9 | 검정 | (0.22, 0.22, 0.22) |

> **텍스트 색상**: 6번(남색), 9번(검정) → 흰색 / 나머지 → 검정

### 3.3 z-order 규칙
- 레이서 원은 역순 생성 (8번부터 0번 순) → 낮은 번호가 더 높은 z-order
- **배팅한 레이서**: 매 프레임 `SetAsLastSibling()` 호출 → 무조건 최상위

### 3.4 배팅 마커 테두리
- 색상: `Color(1.0, 0.9, 0.3, 0.9)` (노란빛)
- 크기: 50×50px (CircleBg보다 10px 큰 테두리 효과)
- Win/Exacta/Trio 모두 해당 레이서에 적용

---

## 4. 위치 업데이트 메커니즘

### 4.1 Y축 (진행도) — 누적 이동 거리 방식

```csharp
// RacerController.cs
float actualStep = Mathf.Min(step, dist);
transform.position += dir.normalized * actualStep;
_cumulativeDistance += actualStep;   // ★ 실제 이동 거리 누적

public float SmoothProgress
{
    get
    {
        if (isFinished) return 1f;
        float totalDist = _oneLapDistance * GetTotalLaps();
        if (totalDist <= 0f) return 0f;
        return Mathf.Clamp01(_cumulativeDistance / totalDist);
    }
}
```

- **핵심 원칙**: 인위적 보간(Lerp/SmoothDamp) 없음 — 캐릭터 실제 이동과 1:1 동기
- `_oneLapDistance`: 웨이포인트 중심선 합산, `StartRacing()` 시 1회 계산
- 바퀴 수 증가 → 자동 압축 (2바퀴 = 절반 속도, 5바퀴 = 1/5 속도)
- 완주 후: `isFinished` 체크 → 1.0 고정

### 4.2 X축 (좌우 펼침) — LateralOffset

```csharp
// RacerController.cs
public float LateralOffset => laneOffset + deviationOffset;

// SceneBootstrapper.Racing.cs UpdateTrackProgressBar()
float xRange = barWidth * 0.2f;   // ±20%
float x = (racer.LateralOffset / maxLateral) * xRange;
```

- 캐릭터 실제 레인 오프셋 + 경로이탈값 → 원의 X 위치
- 겹침 방지 + 우마무스메 스타일 자연스러운 퍼짐 효과

---

## 5. 랩 구분선

- 조건: `totalLaps > 1`일 때 `(totalLaps - 1)`개 생성
- 크기: 14×6px 고정 (anchorMin=anchorMax=0.5f, sizeDelta로 크기 지정)
- 색상: `Color(1.0, 0.9, 0.4, 0.85)` (노란빛 반투명)
- 위치: `yNorm = lap / totalLaps` → TrackBarArea 내 Y 좌표
- 생성 시점: `OnStateChanged(Racing)` 진입 시 `CurrentLaps` 읽어 동적 생성
- 기존 구분선은 파괴 후 재생성

---

## 6. 상태별 처리

| 상태 | 원 색상 | 원 위치 |
|------|---------|---------|
| Countdown | 흰색 (알파 50%) | Y=0 (START 위치) |
| Racing 진입 | 포지션 컬러 + 배팅 테두리 | OverallProgress 추적 시작 |
| 레이스 중 | 포지션 컬러 | SmoothProgress × barHeight |
| 완주 | 포지션 컬러 | Y=barHeight (GOAL 위치) |

---

## 7. 다국어 문자열

```csv
str.hud.start,출발,START,スタート,起点,START,SALIDA,LARGADA
str.hud.goal,결승,GOAL,ゴール,终点,ZIEL,META,CHEGADA
```

---

## 8. 관련 파일

| 파일 | 변경 내용 |
|------|----------|
| `Assets/Scripts/Manager/UI/SceneBootstrapper.Racing.cs` | `BuildTrackProgressBar()`, `UpdateTrackProgressBar()`, `InitTrackBarForRace()`, `BuildLapDividers()` 신규 구현 |
| `Assets/Scripts/Manager/UI/SceneBootstrapper.cs` | `RacerCircleUI` struct, `racerCircles[]`, `trackBarRect`, `lapDividers`, `myPickIndices` 필드 |
| `Assets/Scripts/Racer/RacerController.cs` | `SmoothProgress`, `LateralOffset`, `_cumulativeDistance`, `_oneLapDistance`, `CalculateOneLapDistance()` 추가 |
| `Assets/Scripts/Data/GameSettings.cs` | `trackProgressBarPrefab` 필드 추가 (추후 디자인 전환 대비) |
| `Assets/Resources/Data/StringTable.csv` | `str.hud.start`, `str.hud.goal` 7개 언어 추가 |

---

## 9. 구현 제외 항목 (추후 과제)

- `trackProgressBarPrefab` 실제 프리팹 교체 (현재 프로시저럴 생성)
- 완주 순서 별 마커 고정 연출 (현재 isFinished=1.0 위치 고정)
- 트랙바 배경 디자인 개선 (현재 단색 흰선)
