# HIST-002: Layout1_TopArea 텍스트 기반 경기기록 UI 전환

## 작업 일자
2026-02-23

## 커밋 이력
| 커밋 | 내용 |
|------|------|
| `8685d43` | Phase 1 — EasyChart 코드 전면 제거 |
| `5153a40` | Phase 2 — 텍스트 기반 경기기록 UI 구현 + Safe Patch 시스템 |
| `f59f1c3` | 순위 `<b>` 태그 제거 + 캐릭터 일러스트 원본 교체 |

## 작업 과정

### Phase 1: EasyChart 코드 제거 (8685d43)

**CharacterInfoPopup.cs**
- EasyChart 관련 using, 필드, 메서드 전부 제거
  - `InitRankChart()`, `UpdateRankChart()`, `LoadPanelSettings()`
  - `CreatePointLabels()`, `ClearPointLabels()`
  - `RANK_FLIP`, `pointLabelObjects`, `rankChartArea`, `noRecordLabel` 등
- XCharts RadarChart 코드는 유지
- ShowSequence() 간소화: 1프레임 대기 → InitRadarChart → UpdateRadarChart → 슬라이드

**BettingUIPrefabCreator.cs**
- RankChartArea, NoRecordLabel 생성 코드 제거

**결과**: 컴파일 에러 없음 확인 후 중간 커밋+푸시

### Phase 2: 텍스트 기반 UI 구현 (5153a40)

**StringTable.csv**
- `str.ui.char.recent_record` (최근 경기기록)
- `str.ui.char.no_dist_record` (- 경기 기록 없음 -)

**CharacterRecord.cs**
- `MAX_RACE_ENTRIES` 6 → 30

**BettingUIPrefabCreator.cs**
- Layout1_TopArea에 RecentRecordHeader + 3개 거리 행 추가 (Create 메서드)
- **Safe Patch 시스템 신규 도입**:
  - `PrefabUtility.EditPrefabContentsScope` 활용
  - 기존 프리팹 값을 유지하면서 없는 자식만 추가하는 패치 방식
  - 레거시 요소(RankChartArea, NoRecordLabel) 자동 제거
  - 팝업 위치 자동 조정 (0.28 → 0.31)
  - 메뉴: `DopamineRace > Patch Betting UI Prefabs (Safe)`

**CharacterInfoPopup.cs**
- 7개 새 Text 필드 + Init() 캐싱
- `UpdateRecentRecords()` — recentRaceEntries를 거리별 분류, 최대 6개씩 표시
- `FormatRankList()` — 기록 없으면 로컬라이즈된 "기록 없음" 텍스트
- `ColoredRank()` — 순위별 rich text 색상 (금/은/동/회색)
- ShowSequence()에서 호출 통합

### 후속 수정 (f59f1c3)
- `ColoredRank()`의 1~3위 `<b>` 태그 제거 (프리팹 Inspector에서 Bold 조절 가능하도록)
- Character_8, Character_9 일러스트 원본 이미지로 교체

## 발견된 문제 및 해결

### 프리팹 재생성 시 수동 설정 유실 문제
- **문제**: `Create Betting UI Prefabs` 실행 시 `SaveAsPrefabAsset()`이 프리팹을 통째로 덮어씀
- **해결**: `EditPrefabContentsScope` 기반 Safe Patch 시스템 도입
  - 기존 오브젝트/컴포넌트 값 보존
  - 없는 자식만 추가, 레거시만 삭제
  - 앞으로 프리팹에 새 요소 추가 시 Patch 메서드에 넣으면 안전

### 런타임 동적 생성 vs 프리팹 패치
- 초기에 `FindOrCreate` 패턴으로 런타임 동적 생성 구현
- 사용자 피드백: "프리팹에 넣어야 Inspector에서 조절 가능"
- Safe Patch로 전환 후 `FindOrCreate` 코드 제거, 단순 `Find()`로 복원

## 테스트 결과
- Phase 1 후 컴파일 에러 없음 확인
- Phase 2 후 런타임 테스트:
  - 거리별 순위 텍스트 정상 표시 (색상 코딩 동작)
  - "경기 기록 없음" 텍스트 정상 표시
  - 팝업 위치 캐릭터 목록과 겹치지 않음
  - XCharts RadarChart 정상 동작 유지

## 파일 변경 목록
| 파일 | 변경 |
|------|------|
| `Assets/Scripts/Manager/UI/CharacterInfoPopup.cs` | EasyChart 제거, 텍스트 UI 구현 |
| `Assets/Scripts/Editor/BettingUIPrefabCreator.cs` | 텍스트 행 추가 + Safe Patch 시스템 |
| `Assets/Scripts/Data/Record/CharacterRecord.cs` | MAX_RACE_ENTRIES 30 |
| `Assets/Resources/Data/StringTable.csv` | 신규 키 2개 |
| `Assets/Prefabs/UI/BettingPanel.prefab` | Patch로 레거시 제거 + 텍스트 요소 추가 |
| `Assets/Resources/Icon/Char/Character_8.png` | 일러스트 원본 교체 |
| `Assets/Resources/Icon/Char/Character_9.png` | 일러스트 원본 교체 |
