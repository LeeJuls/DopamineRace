# SPEC-001: 캐릭터 정보창 (9번) 레이아웃 수정

## 오더
캐릭터 정보창이 원하는 디자인과 다르게 나오고 있음. 레이아웃 및 정보 표시 수정 필요.

## 진단된 문제점

### P1. 레이더차트 반경 과대 (Critical)
- `InitRadarChart`에서 `radius = (minDim - 120) * 0.5`
- 실제 런타임 RadarChartArea rect=1459x762 → radius=321 (거대)
- 차트가 팝업 전체를 덮어버림 → 다른 요소 안 보이는 주 원인

### P2. 팝업 앵커가 전체 화면 기준으로 너무 넓음
- 현재: (0.08, 0.10)→(0.92, 0.80) = 84% width × 70% height
- CharacterListPanel(0.01→0.30)과 크게 겹침
- 원하는 디자인: 캐릭터 리스트 우측에 적절한 크기로 표시

### P3. 내부 레이아웃 비율 불균형
- Layout1_TopArea: 22% → 순위 그래프+타입 라벨에 비해 좁음
- Layout2 영역: 65% → 일러스트+레이더에 적당하나 차트 크기 미제어
- Layout3_Bottom: 13% → 스킬 영역 적절

### P4. HideInfoToggle sizeDelta=(0,0)
- 토글 UI가 크기 0으로 보이지 않음

### P5. CharacterListPanel 앵커 불일치
- 코드: (0.01,0.12)→(0.75,0.80)
- 실제 프리팹: (0.01,0.12)→(0.30,0.70)
- 수동 조정 이력 있음 → 코드 동기화 필요

## 수정 계획

### Step 1: BettingUIPrefabCreator.cs - 프리팹 레이아웃
1. CharacterInfoPopup 앵커 조정: 캐릭터 리스트 우측에 배치
   - 새 앵커: (0.22, 0.12)→(0.78, 0.78)
2. CharacterListPanel 코드를 실제 프리팹 값에 맞춤
   - 새 앵커: (0.01, 0.12)→(0.25, 0.80)
3. Layout1_TopArea 비율 상향: 22% → 30% (순위 그래프 가독성)
4. HideInfoToggle sizeDelta 수정: (180, 28)
5. 팝업 배경색 약간 불투명하게 (가시성 개선)

### Step 2: CharacterInfoPopup.cs - 차트 사이징
1. RadarChart radius 상한 추가: max 100px
2. RadarChart center/radius를 rect 비율로 고정
3. LineChart 그리드 여백 조정 (라벨 겹침 방지)

### Step 3: Unity 프리팹 재생성 + 컴파일 테스트

### Step 4: 런타임 진단 스크립트로 QA

## 영향 파일
- `Assets/Scripts/Editor/BettingUIPrefabCreator.cs`
- `Assets/Scripts/Manager/UI/CharacterInfoPopup.cs`
- `Assets/Prefabs/UI/BettingPanel.prefab` (재생성)

## 테스트 포인트
- [ ] 프리팹 재생성 후 컴파일 에러 없음
- [ ] 팝업 4개 레이아웃 영역 올바른 비율
- [ ] 레이더차트 적절한 크기로 렌더링
- [ ] 순위 그래프 라벨 가독성
- [ ] 일러스트 + 승률 정상 표시
- [ ] 스킬 설명 정상 표시
- [ ] 닫기 버튼 동작
- [ ] 동일 캐릭터 재클릭 토글 동작
