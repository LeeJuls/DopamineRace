# HIST-001: 캐릭터 정보창 (9번) 레이아웃 수정

## 작업 요약
캐릭터 정보창(CharacterInfoPopup) 팝업의 레이아웃이 기획서 디자인과 불일치하던 문제를 수정.
프리팹 생성기 + 런타임 차트 사이징 두 곳을 함께 수정하여 해결.

**주의**: CharacterInfoPopup 관련 코드**만** 수정. CharacterListPanel / HideInfoToggle 등 기존 레이아웃은 유저 수동 조정값 유지.

## 수정 이력

### Rev.1 (초기 수정 — 부분 실패)
- CharacterListPanel, HideInfoToggle 앵커를 잘못 변경하여 유저 수동 조정값 손실
- 레이더차트 반경 Clamp(0.35, 25, 110) 적용했으나 여전히 오버플로
- 라인차트 가시성 부족

### Rev.2 (수정 보완)
- CharacterListPanel, HideInfoToggle 유저 수동 조정값으로 **원복**
- 레이더차트: 반경 축소 Clamp(0.28, 25, 80) + Layout2_Right에 **RectMask2D** 추가
- 라인차트: RankChartArea 영역 확장 + 그리드 여백 최소화

## 진단된 문제 → 수정 내용

### P1. 레이더차트 반경 과대 (Critical)
- **원인**: `radius = (minDim - 120) * 0.5` → rect=1459x762일 때 radius=321 (화면 절반)
- **수정 (Rev.2)**: `radius = Clamp(minDim * 0.28, 25, 80)` + Layout2_Right에 RectMask2D 클리핑

### P2. 팝업 앵커 과도하게 넓음
- **Before**: (0.08, 0.10)→(0.92, 0.80) = 84% x 70%
- **After**: (0.22, 0.12)→(0.78, 0.78) = 56% x 66%
- 캐릭터 리스트와 겹침 최소화, 중앙 배치

### P3. 내부 레이아웃 비율 불균형
- **Layout1_TopArea**: 22% → 30% (순위 그래프 가독성 향상)
- **Layout2_Left**: 42% → 38% 너비
- **Layout2_Right**: 58% → 62% 너비 (레이더차트 공간 확보) + RectMask2D
- **Layout2 전체**: 0.13~0.78 → 0.13~0.70 (Layout1이 30%로 확장됨에 따라)
- 팝업 배경색: (0.05,0.05,0.1,0.95) → (0.08,0.08,0.12,0.96) + Outline 추가
- **RankChartArea**: (0.02,0.02→0.95,0.72) → (0.02,0.02→0.98,0.78) 영역 확장
- **RadarChartArea**: (0.05,0.05→0.95,0.95) → (0.03,0.03→0.97,0.97) 패딩 조정

### 추가 개선
- LineChart 그리드 여백: left/right 50→15, top 25→20, bottom 10→10
- LineChart 라벨 폰트: 11→10, symbol 6→5 (컴팩트)
- SkillDescLabel 위치 미세 조정: anchorX 0.12→0.10, offset 60→40

## 변경 범위 (CharacterInfoPopup만)
| 파일 | 변경 내용 |
|------|-----------|
| `Assets/Scripts/Editor/BettingUIPrefabCreator.cs` | 팝업 앵커/내부 레이아웃 비율 수정 + Layout2_Right RectMask2D 추가 |
| `Assets/Scripts/Manager/UI/CharacterInfoPopup.cs` | RadarChart radius 상한(80px), LineChart 그리드 여백 조정 |
| `Assets/Prefabs/UI/BettingPanel.prefab` | 재생성 (위 코드 변경 반영) |

### 미변경 (유저 수동 조정값 유지)
- CharacterListPanel: (0.01,0.12)→(0.30,0.70)
- HideInfoToggle: (0.85,0.78)→(0.85,0.78), sizeDelta=(180,28)
- TrackInfoPanel: (0.01,0.01)→(0.75,0.11)

## QA 결과
- [x] 컴파일 에러 없음
- [x] 프리팹 재생성 성공
- [x] GameSettings 프리팹 자동 연결 확인
- [x] 팝업 4개 레이아웃 앵커 값 검증 통과
- [x] CharacterInfoPopup + Outline 컴포넌트 존재 확인
- [x] CharacterListPanel / HideInfoToggle 원래 값 복원 확인
- [x] Layout2_Right에 RectMask2D 추가 확인
- [ ] **런타임 시각적 확인 필요** (사용자 Play 후 스크린샷)

## 사용자 확인 필요
게임 실행 후 캐릭터를 클릭하여:
1. 팝업이 화면 중앙~우측에 적절한 크기로 표시되는지
2. 순위 그래프 (꺾은선) 가 상단에 정상 렌더링되는지
3. 레이더차트가 우측에 적절한 크기로 표시되는지 (오버플로 없이)
4. 일러스트 + 승률이 좌측에 정상 표시되는지
5. 스킬 설명이 하단에 표시되는지
6. **캐릭터 리스트 / 토글 / 트랙정보가 이전과 동일한지**

## 날짜
2026-02-22
