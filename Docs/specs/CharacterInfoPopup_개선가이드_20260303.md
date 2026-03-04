# CharacterInfoPopup 크기 및 정보 배열 개선 가이드

> 작성: 2026-03-03
> 목적: CharacterInfoPopup 레이아웃 개선 작업 전 현황 분석 및 작업 방향 정리

---

## 1. 현재 구조 (런타임 실측값)

```
CharacterInfoPopup  1037 x 713 px   anchor x:0.31~0.85 / y:0.12~0.78
│
├─ Layout1_TopArea   1037 x 200 px  (상단 28%, y:0.72~1.00)
│   ├─ CharTypeLabel      120x30
│   ├─ CloseBtn            50x50
│   ├─ RecentRecordHeader 841x32
│   ├─ ShortDistRow       996x40
│   ├─ MidDistRow         996x40
│   └─ LongDistRow        996x36
│
├─ Layout2_Left      394 x 420 px   (중단 좌측, x:0.00~0.38)
│   ├─ Illustration       371x373   ← 일러스트
│   ├─ WinRateBg          270x42    ← 승률
│   └─ StoryIconBtn        28x28
│
├─ Layout2_Right     643 x 420 px   (중단 우측, x:0.38~1.00)
│   └─ RadarChartArea  1184x695 ⚠️  ← 컨테이너(643x420) 초과! 오버플로우 발생
│
└─ Layout3_Bottom   1037 x  93 px   (하단 13%, y:0.00~0.13)
    ├─ SkillIcon           40x40
    └─ SkillDescLabel     500x30
```

---

## 2. 발견된 문제점

### ⚠️ 문제 1: RadarChartArea 오버플로우
- 컨테이너 `Layout2_Right`: **643 x 420 px**
- `RadarChartArea` 실측 크기: **1184 x 695 px** (약 1.8배 초과)
- XCharts가 내부적으로 painter 레이어를 canvas 전체 크기로 생성하는 특성 때문
- 시각적으로 레이더차트가 팝업 영역을 벗어나 보일 수 있음
- **대응**: RadarChartArea에 `RectMask2D` 컴포넌트 추가로 클리핑

### ⚠️ 문제 2: Layout3_Bottom (스킬 영역) 너무 좁음
- 현재 높이: **93px** (전체의 13%)
- `SkillDescLabel`이 500x30으로 텍스트 잘림 가능성 높음
- 스킬 설명이 길 경우 표시 불가

### ⚠️ 문제 3: 정보 밀도 불균형
- 상단(Layout1): 경기 기록 텍스트 3줄이 200px에 빽빽하게 배치
- 하단(Layout3): 스킬 정보가 93px에 압축
- 레이더차트와 일러스트가 중간 420px을 차지하는 구조

---

## 3. 개선 방향 옵션

### Option A: 비율 재조정 (권장 — 최소 코드 변경)
```
Layout1_TopArea:   y 0.70 ~ 1.00  (30%, 현재 28%)  → 214px
Layout2_Left/Right: y 0.18 ~ 0.70  (52%, 현재 59%)  → 371px
Layout3_Bottom:    y 0.00 ~ 0.18  (18%, 현재 13%)  → 128px
```
- 스킬 영역 높이: 93 → 128px (+38%)
- 코드 변경 없음, 프리팹 앵커만 조정

### Option B: 전체 팝업 크기 확대
```
현재: x 0.31~0.85 / y 0.12~0.78  → 1037 x 713 px
변경: x 0.28~0.88 / y 0.08~0.82  → 약 1100 x 785 px (+10%)
```
- 모든 요소가 비례 확대
- 해상도 여유가 있을 때 사용

### Option C: 레이아웃 재설계 (대규모 개선)
```
┌─────────────────────────────────┐
│  CharType  캐릭터명      [X]     │  TopBar (50px)
├──────────┬──────────────────────┤
│          │  레이더차트           │
│ 일러스트  │  (능력치 6각형)       │  Middle (350px)
│  + 승률  │                      │
├──────────┴──────────────────────┤
│  단거리  중거리  장거리 (탭 전환) │  Records (100px)
├─────────────────────────────────┤
│  ⚔ 스킬 설명 텍스트             │  Skill (80px)
└─────────────────────────────────┘
```

---

## 4. 즉시 적용 가능한 수정 (Unity Editor)

### Step 1: RadarChartArea 클리핑 수정
1. `BettingPanel.prefab` 열기
2. `CharacterInfoPopup > Layout2_Right` 선택
3. **Add Component → RectMask2D** 추가
4. → 레이더차트가 Layout2_Right 영역 밖으로 넘치지 않음

### Step 2: Layout 비율 조정
`CharacterInfoPopup` 선택 → 자식 오브젝트 앵커 수정:

| 오브젝트 | 현재 AnchorMin.y | 현재 AnchorMax.y | 변경 후 Min.y | 변경 후 Max.y |
|---------|----------|----------|---------|---------|
| Layout1_TopArea  | 0.72 | 1.00 | **0.70** | 1.00 |
| Layout2_Left     | 0.13 | 0.72 | **0.18** | **0.70** |
| Layout2_Right    | 0.13 | 0.72 | **0.18** | **0.70** |
| Layout3_Bottom   | 0.00 | 0.13 | 0.00 | **0.18** |

### Step 3: SkillDescLabel 크기 확장
1. `Layout3_Bottom > SkillDescLabel` 선택
2. Width: 500 → **900** (팝업 너비 활용)
3. Height: 30 → **60** (2줄 표시 가능)
4. Text 컴포넌트 → Vertical Overflow: **Overflow**

---

## 5. 코드 수정이 필요한 경우

### CharacterInfoPopup.cs 주요 수정 지점

**슬라이드 애니메이션 시작 오프셋** (현재 -200):
```csharp
// ShowSequence() 내
Vector2 startPos = targetPosition + new Vector2(0, -200);
// 팝업 크기 변경 시 이 값도 조정 (팝업 높이의 약 30% 정도가 적당)
```

**레이더차트 반지름** (현재 minDim의 30%, 최대 80px):
```csharp
// InitRadarChart() 내
float radius = Mathf.Clamp(minDim * 0.30f, 25f, 80f);
// Layout2_Right 크기 변경 시 함께 조정
```

---

## 6. 작업 우선순위

| 우선순위 | 작업 | 난이도 | 효과 |
|---------|------|--------|------|
| 1 | RectMask2D 추가 (레이더차트 클리핑) | 쉬움 | 오버플로우 제거 |
| 2 | Layout 비율 재조정 (스킬 영역 확대) | 쉬움 | 정보 가독성 향상 |
| 3 | SkillDescLabel 크기 확장 | 쉬움 | 스킬 텍스트 잘림 해결 |
| 4 | 전체 팝업 크기 확대 | 보통 | 전반적 여유 확보 |
| 5 | 레이아웃 전면 재설계 | 어려움 | 최적 UX |
