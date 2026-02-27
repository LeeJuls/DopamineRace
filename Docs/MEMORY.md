# DopamineRace Project Memory

## Project Overview
- Unity 2D 경마 게임 (도파민레이스)
- Working dir: `D:\unity\Project\DopamineRace\Assets\Scripts`
- GitHub: https://github.com/LeeJuls/DopamineRace

## Git Rules
- 커밋 메시지에 **[C] 접두사** 필수
- "푸시해줘" 명시 요청 시에만 push
- 토큰은 세션마다 재설정 필요

## Key Architecture
- `OddsCalculator` — static 클래스, MonoBehaviour 아님
- `CurrentOdds`는 StartNewGame/StartNewRound 때만 갱신
- `CharacterRecord`는 **charId**(UID) 기준 저장, DisplayName 혼용 금지
- `CharacterRecord.recentRaceEntries` — 최근 6경기 (rank+trackName+laps), 순위 그래프용
- 기존 payoutWin/Place 등 삭제 금지 (CalculateLegacy fallback 유지)
- conditionRate 합계 1.0 유지, oddsRangeMin/Max 배열 길이 12 이상
- `SceneBootstrapper.Betting.cs`는 프리팹 기반 (CharacterItemUI + BettingPanel prefab)
- `ConditionIconFactory` — static, 런타임 Texture2D 화살표 생성
- `TrackType` enum (E_Base / E_Dirt) — TrackData.cs + TrackDB.csv
- StringTable 키 체계: `str.ui.*` (UI용), `str.bet.*` (배팅), `str.hud.*` (HUD)
- CharacterDB.csv 컬럼: col 0=char_id(UID), col 1=char_name(Loc키), 15=char_skill_desc, 16=char_illustration
- `charId` = `char.type.name.NNN` (예: `char.leader.thunder.000`) — 진짜 UID
- `charName` = `str.char.NNN.name` (0-based) — 순수 Loc 키
- `DisplayName` = `Loc.Get(charName)` — UI 표시용
- `CharacterDatabase.FindById(charId)` — charId 검색용 메서드
- 어택 프리팹: `Character_N_Attack.prefab` (일반 프리팹과 번호 대응)
- `SAVE_VERSION = 2` — ScoreManager, 구버전 세이브 자동 삭제

### 레이스 시스템 (HP + CP)
- **이중 자원**: HP(지구력=Endurance, 스프린트) + CP(침착=Calm, 슬립스트림/안정성)
- `RacerController.CalcHPBoost()`: consumedRatio 기반 (boostThreshold=0.4에서 피크)
- `ConsumeHP()`: 4단계 — Positioning(0~0.5랩) → FormationHold(0.5~1.0랩) → Strategy(1.0+) → Legacy(폴백)
  - Positioning: Runner=activeRate, Leader=Lerp(in,active,0.6), Chaser=inZoneRate, Reckoner=최소소모
  - Strategy: Runner top45%보존/이탈시스프린트, Chaser top70%보존/마지막15%스퍼트, Reckoner 마지막20%스퍼트
- `hpSpeedCompress=0.85`: 기본속도 격차 ~0.83% 압축 → HP 부스트가 레이스 결과 결정
- **슬립스트림**: 전체 타입 거리 기반, CP 소모(2.5/s) + 속도 보너스(+1.5%), Chaser ×1.1
- **불안정**: CP/HP 잔량 낮으면 노이즈 배율 증가 (곱연산)
- **TrailingBonus 삭제됨** — 순위 기반 하위권 보정 제거, CP 시스템으로 대체
- `RaceBacktestWindow.cs`: Editor 윈도우, 100레이스 시뮬레이션 (RacerController 로직 미러링)

### 차트 라이브러리
- **EasyChart Lite** (Assets/EasyChart/) — 순위 꺾은선 그래프용
  - `UGUIChartBridge` + `ChartProfile` 조합
  - WorldSpace 렌더모드 (RenderTexture → RawImage)
  - **커스텀 프로필은 렌더링 안 됨** → 반드시 데모 프로필 복제(`Resources/DemoLineProfile.asset`) 후 커스터마이즈
  - `Serie`/`LineType` 네임스페이스 충돌 → 반드시 `EasyChart.Serie` 사용
  - SeriesData: `x` + `value` 사용 (`y` 아님)
  - Hide→Show 사이클마다 ChartElement 파괴→재생성됨 (OnDisable/OnEnable)
  - Unity 6 필수: `ThemeStyleSheet(TSS)` → `Assets/UI Toolkit/UnityThemes/UnityDefaultRuntimeTheme.tss`
  - PanelSettings: `Assets/Resources/EasyChartPanelSettings.asset` (TSS 연결 + m_DisableNoThemeWarning=1)
- **XCharts** — 능력치 레이더차트용 (RadarChart)
  - manifest.json에 git URL로 설치

### CharacterInfoPopup.cs
- EasyChart `UGUIChartBridge` (순위 그래프) + XCharts `RadarChart` (스탯)
- 슬라이드인 애니메이션 + 토글 (동일 캐릭터 재클릭 시 닫기)
- RANK_FLIP(=13): rank 1→y=12(상단), rank 12→y=1(하단)
- ShowSequence에서 ChartElement 폴링 대기 (최대 8프레임)
- **RankChartArea 확정 좌표** (BettingPanel.prefab):
  - Left=-208.95, Top=-125.04, Right=-224.95, Bottom=-2.61
  - Anchors Min(0.02, 0.02) Max(0.98, 0.75), Pivot(0.5, 0.5)

## Completed (as of 035a181+)
- 동적 배당 + 인기도 + 컨디션 시스템
- F1 디버그 오버레이 배당 섹션 (스크롤, 접기)
- CSV 기반 다국어 로컬라이제이션 (ko/en/jp)
- OddsCalculator 배팅 화면 진입 시 계산
- GameSettings asset 배당 시스템 설정값 직렬화 완료
- SampleData asset 외형 업데이트 커밋 완료
- **배팅 UI 전면 개편** (프리팹 기반, 6 Phase 완료):
  - StringTable.csv 키 재분류 + 신규 ~20개 키
  - TrackDB/TrackInfo/TrackData에 track_type 추가
  - GameSettings에 거리 구분 + UI 프리팹 레퍼런스
  - ConditionIconFactory.cs (런타임 화살표 아이콘 5종)
  - CharacterItemUI.cs + CharacterInfoPopup.cs
  - BettingUIPrefabCreator.cs (Editor 프리팹 자동 생성)
  - SceneBootstrapper.Betting.cs 리팩토링
  - GameSettingsEditor.cs 배당 시스템 리셋 버튼 추가

## Completed (as of 51da9d0)
- **캐릭터 정보창 (9번)** 구현 완료
  - CharacterDB: char_skill_desc + char_illustration + 008 luck 20
  - CharacterRecord: RaceEntry 구조 (순위 그래프용)
  - CharacterInfoPopup: 4레이아웃 + XCharts + 슬라이드인 + 토글
  - BettingUIPrefabCreator: 프리팹 4레이아웃 재구축
  - SceneBootstrapper: 동일 캐릭터 재클릭 토글 + record 전달

## Completed (as of current session — 2026-02-23)
- **타이틀 씬 (TitleScene) 전체 구현** (Phase 1~6 완료)
  - TitleSceneManager.cs: 배경/로고/PressToStart/언어선택/캐릭터러너/BGM
  - TitleCharacterRunner.cs: 12캐릭터 순차 스폰 + 좌→우 이동
  - SceneTransitionManager.cs: 16×10 블록 디졸브/빌드업 (DontDestroyOnLoad)
  - BGMManager.cs 재작성: PlayBGM/FadeIn/FadeOut/CrossFade
  - CharacterDatabase/TrackDatabase: DontDestroyOnLoad 추가
  - TitleScene.unity (Build Index 0), SampleScene (Build Index 1)

## Completed (as of current session — 2026-02-24)
- **버그 수정**
  - RacerController.ResetRacer: 누락 변수 리셋 추가 (noise/luck/deviation)
  - RacerController.CalculateSpeed: condMul 안전 가드 (Mathf.Max 0.3f)
  - SceneBootstrapper.Betting: 영문 텍스트 오버플로우 BestFit 처리
- **다국어 확장**: 7개 언어 지원 (cn/de/es/br 추가), StringTable.csv 154개 키
- **charBaseSpeed 스케일 통일**: DB 1~20, SpeedMultiplier = 0.8 + charBaseSpeed * 0.01f
  - CharacterData.SpeedMultiplier 프로퍼티로 단일 관리
  - 20=1.0배속, 15=0.95, 10=0.90 (격차 최대 10%)

## 문서 폴더 규칙
- **작업 문서는 `Docs/` 하위에만 저장** (`doc/` 폴더 폐기)
  - 히스토리: `Docs/history/YYYYMMDD_제목_히스토리.md`
  - 명세서: `Docs/specs/YYYYMMDD_제목_명세서.md`
  - 그 외 문서: `Docs/YYYYMMDD_제목.md`

## 사용 전 필수 단계
- Unity에서 `DopamineRace > Create Betting UI Prefabs` 메뉴 실행
- GameSettings Inspector에서 bettingPanelPrefab / characterItemPrefab 자동 연결 확인

## Completed (as of 2026-02-25 session 1)
- **캐릭터 데이터 리팩토링** (7 Phase 완료)
  - Phase 1: CharacterDB.csv char_id 컬럼 추가 + 0-base 리넘버링
  - Phase 2: StringTable.csv 36개 키 0-base 리넘버링
  - Phase 3: CharacterData.cs charId 필드 + ParseCSVLine 시프트
  - Phase 4: 16개 파일 charName→charId 마이그레이션 + 버그 수정 2건
  - Phase 5: Pixem 커스텀 네이밍 (InputField 전환)
  - Phase 6: 어택 프리팹 리네이밍 (Character_N_Attack 형태)
  - Phase 7: 검증 + 히스토리 문서
  - 수정 파일: 21개 (CSV 2, C# 15, 프리팹/폴더 리네이밍 24)

## Completed (as of 2026-02-25 session 2)
- **CP 시스템 + 백테스팅 튜닝**
  - CP (Calm Points): `maxCP = calm × 10`, 기본소모 0.5/s, 슬립스트림 +2.5/s
  - 개선 슬립스트림: 전체 타입 거리 기반, CP 소모 + 직접 속도 보너스
  - CP/HP 불안정 노이즈: 잔량 낮으면 노이즈 배율 증가 (곱연산)
  - 제거: TrailingBonus, Chaser 전용 HP slipstream, 순위 기반 발동조건
  - **10라운드 백테스팅** → Set W 최적값 (R=12% L=30% C=24% K=32%)
  - 수정 파일: GameSettings.cs/asset, RacerController.cs, RaceBacktestWindow.cs

### 현재 GameSettings.asset 실제값 (코드 default와 다름 — asset 우선)
```
Runner:   spurt=0.00 aR=3.5 pk=0.20 ac=1.0 dc=0.7  fl=-0.20 EB=0.25
Leader:   spurt=0.10 aR=3.0 pk=0.20 ac=1.2 dc=1.0  fl=-0.08 EB=0.04
Chaser:   spurt=0.15 aR=5.5 pk=0.24 ac=1.3 dc=0.8  fl=-0.03 EB=-0.02
Reckoner: spurt=0.20 aR=7.0 pk=0.24 ac=1.8 dc=0.4  fl=-0.01 EB=-0.06
EarlyBonusFadeEnd=0.25, boostThreshold=0.40, boostHPDrainCoeff=1.5
hpLapReference=3, longRaceLateBoostAmp=0.15
leader_targetZone=0.45, runner_targetZone=0.25, chaser_targetZone=0.70
roundLaps=[2,2,3,5,3,2,4]
```

### 핵심 튜닝 인사이트
- `hpSpeedCompress=0.85` → 기본속도 차이 ~0.83%로 압축, HP 부스트 적분이 결정적
- `activeRate`가 3.0 이상이어야 3랩 내 부스트 피크 도달 가능
- Chaser `decelExp=0.8`이 경쟁력 핵심 (느린 감쇄 = 지속 부스트)
- 트랙 다양성으로 자연스러운 타입별 편차 (Highland→Reckoner, Desert→Chaser)
- **HP 랩 스케일링**: `maxHP × max(1, laps/3)` → 5바퀴에서도 탈진 90% 시점 유지
- **도주 버스트**: earlyBonus=0.25, accelExp=1.0(선형), fadeEnd=0.25 → "파아악" 시각 효과
- **earlyBonus 부호**: Runner=+0.25(초반가속), Chaser=-0.02, Reckoner=-0.06 (초반 의도적 감속 → 대열 분리)
- **leader_targetZone=0.45**: 도주 3마리 점령 시 선행이 4위에서 보존 가능 (0.30이면 항상 패닉소모)
- **장거리 증폭**: Chaser/Reckoner peakBoost ×1.10 (5바퀴), 후반 역전 강화

## Completed (as of 2026-02-27)
- **레이스 디버그 HP 탭 추가** (RaceDebugOverlay.cs)
  - 바퀴별 전 레이서 HP/CP 스냅샷 + 바 시각화 (LapSnapshot/LapRacerInfo 구조체)
  - 완주 로그에 HP% 표시
- **선입/추입 스퍼트 Lerp 제거** — 스퍼트 진입 즉시 activeRate 적용
  - RacerController.cs + RaceBacktestWindow.cs 양쪽 동시 수정 필수 (미러링 규칙)
- **도주 초반 폭발력 강화** — earlyBonus 0.12→0.25, accelExp 1.5→1.0
- **버그수정**: 마지막 완주자 미기록 — SaveRoundReport에서 finishEvents 누락 보완
  - 원인: LateUpdate가 `!raceActive` 즉시 return → 8번째 완주자 로그 누락
- **선행 포지션 개선**: leader_targetZone 0.30→0.45
- **UI 디자이너 온보딩 가이드**: `Docs/20260226_UI디자이너_온보딩가이드.md`

## Remaining Tasks
- **밸런스 플레이 테스트**: 도주 파아악 + 선행 4위권 확인 (이번 세션 변경 후 미검증)
- **RaceBacktestWindow 5바퀴 백테스트** 필수 (간이 스크립트 불충분)
- 사용자 직접 플레이 테스트 (비주얼 확인, 타이밍 조정)
- 디졸브/빌드업 속도 미세 조정 (현재 각 1.2초)
- 캐릭터 스폰 Y위치/속도, 로고 크기/위치 미세 조정
- SampleScene 직접 시작 시 BGM 재생 처리 (SceneBootstrapper 보완)
- EasyChart 순위 차트 런타임 테스트 (이전 세션 미완료)
- Pixem 에디터: `CharacterPrefabNameInput` (InputField) Inspector에서 수동 연결 필요
- 런타임 전체 흐름 테스트 (charId 기반 배팅/결과/세이브)

## MCP 주의사항
- `unity-code-mcp-stdio` 서버가 Unity 상태 변경(Play/Recompile) 시 크래시 → 좀비 프로세스 누적
- 새 세션 시작 전: `Get-Process unity-code-mcp-stdio | Stop-Process -Force` 실행 권장
- Play 모드 전환 중 MCP 사용 자제

## Detail docs
- See [2026-02-22_EasyChart순위그래프전환_인수인계.md](./2026-02-22_EasyChart순위그래프전환_인수인계.md) for latest handover
- See [handover.md](./handover.md) for previous handover (배팅 UI 개편)
