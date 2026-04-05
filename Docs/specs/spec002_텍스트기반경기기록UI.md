# SPEC-002: Layout1_TopArea 텍스트 기반 경기기록 UI 전환

## 오더
EasyChart Lite 순위 그래프가 기대한 품질에 미달 (데이터 라벨 미지원, 축 설정 무시, 아트 퀄리티 부족).
Layout1_TopArea를 그래프 대신 텍스트 기반 최근 경기기록 표시로 전환.

## 배경
- EasyChart Lite의 LineSeriesRenderer는 `DrawSerieLabel()`을 호출하지 않아 데이터 포인트 라벨 표시 불가
- `labelStyle` 객체가 존재하면 `showLabels`/`fontSize` 프로퍼티가 무시됨
- UGUI Text 오버레이로 우회 시도했으나, 근본적으로 그래프 품질 부족

## 작업 범위

### Phase 1: EasyChart 코드 제거
- CharacterInfoPopup.cs에서 EasyChart 관련 코드 전면 제거
  - using 디렉티브, 필드, InitRankChart, UpdateRankChart, LoadPanelSettings 등
  - CreatePointLabels, ClearPointLabels, RANK_FLIP 상수
- BettingUIPrefabCreator.cs에서 RankChartArea, NoRecordLabel 생성 코드 제거
- XCharts RadarChart (Layout2_Right)는 유지

### Phase 2: 텍스트 기반 UI 구현

#### StringTable.csv
- `str.ui.char.recent_record` — 최근 경기기록 / Recent Records / 最近のレース記録
- `str.ui.char.no_dist_record` — - 경기 기록 없음 - / - No record - / - 記録なし -

#### CharacterRecord.cs
- `MAX_RACE_ENTRIES` 6 → 30 (거리별 6개씩 필터링하므로 충분한 풀 필요)

#### BettingUIPrefabCreator.cs
- Layout1_TopArea에 텍스트 행 구조 추가:
  - RecentRecordHeader (노란색, fontSize 18)
  - ShortDistRow > ShortDistLabel + ShortDistRanks
  - MidDistRow > MidDistLabel + MidDistRanks
  - LongDistRow > LongDistLabel + LongDistRanks
- **Safe Patch 시스템** 신규 도입:
  - `PrefabUtility.EditPrefabContentsScope` 활용
  - 기존 프리팹 값 유지하면서 없는 자식만 추가
  - 레거시 요소(RankChartArea, NoRecordLabel) 자동 제거
  - 메뉴: `DopamineRace > Patch Betting UI Prefabs (Safe)`

#### CharacterInfoPopup.cs
- 7개 새 Text 필드 (recentRecordHeader, shortDistLabel/Ranks, midDistLabel/Ranks, longDistLabel/Ranks)
- Init()에서 Find()로 참조 캐싱
- `UpdateRecentRecords(CharacterRecord)` — 거리별 분류 + 텍스트 갱신
- `FormatRankList()` / `ColoredRank()` — rich text 색상 코딩
  - 1위: #FFD700 (금)
  - 2위: #C0C0C0 (은)
  - 3위: #CD7F32 (동)
  - 4위~: #CCCCCC (회색)
- ShowSequence()에서 UpdateRecentRecords() 호출

### 거리 분류 기준
- `GameSettings.GetDistanceKey(laps)`:
  - laps <= shortDistanceMax → 단거리
  - laps <= midDistanceMax → 중거리
  - laps > midDistanceMax → 장거리
- laps == 0 엔트리는 레거시 마이그레이션이므로 스킵

### UI 표시 규칙
- 각 거리별 최대 6개 순위 표시 (최신순)
- 해당 거리에 기록 없으면 "- 경기 기록 없음 -" 표시
- 팝업 위치: anchorMin (0.31, 0.12) → anchorMax (0.85, 0.78)로 우측 이동 (캐릭터 목록 겹침 방지)
