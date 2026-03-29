# SPEC-014: 트랙바 싱크 + 부드러운 이동 + 스폰 정렬 + STA 밸런스

## 오더 요약
1. 트랙바 마커 싱크 불일치 + 이동 부자연스러움
2. 뒷줄 캐릭터가 레이스 방향이 아닌 위(↑)로 달려감
3. Runner(도주)가 뒷줄 출발 시 회복 불가
4. 저STA 캐릭터 단거리에서도 HP 탈진

## 변경 사항

### 1. OverallProgress 서브WP 보간 (RacerController.cs)
- WP 사이 Dot 투영으로 연속 progress 계산
- headingToFinish 별도 분기: 마지막WP→결승선 구간 보간
- `_maxOverallProgress`로 단조 증가 보장 (progress 드롭 방지)

### 2. 트랙바 데이터 소스 교체 (SceneBootstrapper.Racing.cs)
- `SmoothProgress` → `OverallProgress`

### 3. trackBarLerpSpeed 설정 (GameSettings.cs)
- 기본값 12f, Inspector 조절 가능 (Range 0~30)

### 4. 시작 WP 변경 (RacerController.cs)
- `currentWP = 0` → `currentWP = 1`
- WP0(결승선)이 스폰 위치 대비 측면 → WP1(확실히 앞쪽)로 변경

### 5. 타입별 스폰 정렬 (RaceManager.cs)
- Runner→앞줄(0-2), Leader→앞~중, Chaser→중~뒤, Reckoner→뒷줄(6-8)
- 같은 타입 내 랜덤 배치

### 6. 컨디션 HP 미적용 (RacerController_V4.cs)
- maxStamina에 컨디션 배율 미적용 → HP풀은 순수 STA 기반

### 7. STA 최소 11 밸런스 (CharacterDB_V4.csv)
- Crystal: STA 5→11 (SPD -3, ACC -3)
- Secret: STA 8→11 (INT -1, LCK -2)
- Amazon: STA 10→11 (INT -1)
- Juhong: STA 10→11 (SPD -1)
- Powerblade: STA 10→11 (LCK -1)

## 파일 변경 목록
| 파일 | 변경 |
|------|------|
| `RacerController.cs` | OverallProgress 서브WP 보간 + _maxOverallProgress + currentWP=1 |
| `RacerController_V4.cs` | 컨디션 HP 적용 제거 |
| `SceneBootstrapper.Racing.cs` | SmoothProgress → OverallProgress |
| `GameSettings.cs` | trackBarLerpSpeed 기본값 12f |
| `RaceManager.cs` | SortByTypePosition + GetTypePriority 추가 |
| `CharacterDB_V4.csv` | STA 최소 11 조정 (5명) |
