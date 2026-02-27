# 히스토리: SPEC-007 배팅 UI 개선 구현

- **날짜**: 2026-02-27
- **세션 종류**: 구현 + 버그 수정 + MCP 셋업
- **참여**: 오너(LeeJuls) + Claude(client)
- **브랜치**: `main` (워크트리 `claude/reverent-bose` → main 머지 완료)

---

## 1. 구현 완료 내역

### Phase 1: 데이터 기반
- `TrackTypeUtil.cs` — E_Snow, E_Rain TrackType 확장 + 다국어 키 매핑
- `CharacterRecord` — 거리별 누적 집계 (shortDistRaces/Wins, midDist~, longDist~)
- `SAVE_VERSION` 2 → 3 (기존 세이브 자동 초기화)
- 커밋: `e2a8632`, `87c5636`

### Phase 2: 트랙 정보 패널 재설계
- `TrackInfoToggleBtn` — 패널 접기/펼치기 토글
- `TrackDescLabel` — 트랙 설명 표시 (flexibleWidth)
- `TrackTypeUtil.GetTrackTypeKey()` 활용 (하드코딩 제거)
- 커밋: `1510a78`

### Phase 3: My Point / Get Point UI
- `MyPointLabel` — OddsArea 하단에 보유 포인트 표시
- `RefreshMyPoint()` — 배팅 변경 시 자동 갱신
- 커밋: `f3455b1`

### Phase 4: 배당률 배지 항상 표시
- `OddsLabel` — CharacterItem에 단승 배당률 배지
- `CharacterItemUI.SetData()` — oddsInfo.winOdds 표시
- 커밋: `d1f9a7a`

### Phase 5: 캐릭터 팝업 개선
- `UpdateRadarChart()` — TrackInfo 기반 보너스 스탯 ★ 강조 (XCharts rich text)
- `UpdateRecentRecords()` — 거리별 1st.N% 표시 (단/중/장거리)
- `CharacterInfoPopup.Show()` 시그니처 확장 (TrackInfo trackInfo = null)
- 커밋: `d7951f1`

### Phase 6: 통합 QA → 통과 (이전 세션)

---

## 2. 추가 수정 (버그 픽스)

### RaceDebug F1 토글 문제
- **원인**: `enableRaceDebug=false` → `RaceDebugOverlay` 미생성 → F1 무반응
- **수정**: 항상 생성, `showDebug` 초기값만 GameSettings로 제어
- **파일**: `RaceManager.cs`, `RaceDebugOverlay.cs`
- 커밋: `11c0a2b`

### enableRaceDebug .asset 직접 수정
- **원인**: ScriptableObject 코드 기본값 변경은 기존 .asset 파일에 반영 안 됨
- **수정**: `GameSettings.asset`에서 `enableRaceDebug: 1 → 0` 직접 수정
- 커밋: `2f6086f`

### PatchPrefabs() 불완전 문제
- **원인**: `CreatePrefabs()`에만 Phase 3/4 요소 추가, `PatchPrefabs()`에는 누락
- **수정**: CharacterItem OddsLabel + BettingPanel MyPointLabel 패치 추가
- 커밋: `e8dd9a7`

### 프리팹 레이아웃 위치 교정
- **원인**: 수동 편집으로 TrackInfoPanel이 화면 중앙에 위치 (anchoredPosition 틀어짐)
- **수정**: `FixStretchPosition()` 헬퍼 — 주요 패널 앵커+위치 자동 교정
- 커밋: `2180db5`

### 확인 다이얼로그 추가
- `Create Betting UI Prefabs` — 강한 경고 (기존 작업 삭제됨)
- `Patch Betting UI Prefabs (Safe)` — 일반 확인
- 커밋: `5887643`, `d197e1a`

---

## 3. MCP 연동 셋업

- **uv 설치**: `C:\Users\user\.local\bin\uv.exe` (v0.10.6)
- **`.mcp.json` 생성**: Signal-Loop UnityCodeMCPServer 브릿지 설정
- Unity 측 서버: `com.signal-loop.unitycodemcpserver` (port 21088, 이미 설치됨)
- **새 세션에서 MCP 활성화됨** (execute_csharp_script, read_console_logs, run_tests)

---

## 4. 생성/수정된 파일 목록

### 신규 생성
| 파일 | 내용 |
|------|------|
| `.mcp.json` | Claude Code ↔ Unity MCP 연결 설정 |
| `Docs/SPEC-007_프리팹수정가이드_20260227.md` | 프리팹 수동 수정 가이드 |
| 이 문서 | 구현 히스토리 |

### 주요 수정
| 파일 | 변경 |
|------|------|
| `Assets/Scripts/Editor/BettingUIPrefabCreator.cs` | PatchPrefabs 대폭 강화 (확인 다이얼로그, CharacterItem 패치, 위치 교정) |
| `Assets/Scripts/Manager/UI/SceneBootstrapper.Betting.cs` | Phase 2-5 코드 (토글, MyPoint, TrackInfo 전달) |
| `Assets/Scripts/Manager/UI/CharacterInfoPopup.cs` | Phase 5 (레이더차트 강조, 거리별 승률) |
| `Assets/Scripts/Manager/UI/CharacterItemUI.cs` | Phase 4 OddsLabel 참조 |
| `Assets/Scripts/Data/GameSettings.cs` | enableRaceDebug 기본값 false |
| `Assets/Resources/GameSettings.asset` | enableRaceDebug: 0 |
| `Assets/Scripts/Manager/RaceManager.cs` | RaceDebugOverlay 항상 생성 |
| `Assets/Scripts/Debug/RaceDebugOverlay.cs` | showDebug 초기값 Awake에서 설정 |
| `Assets/Prefabs/UI/BettingPanel.prefab` | 패치 적용 (위치 교정 포함) |
| `Assets/Prefabs/UI/CharacterItem.prefab` | OddsLabel 추가 |

---

## 5. 알려진 이슈 / 남은 작업

1. **프리팹 수동 디자인 작업 필요** — 위치/크기/텍스트 정렬은 오너/UI 디자이너가 Unity Editor에서 수동 조정 (가이드: `Docs/SPEC-007_프리팹수정가이드_20260227.md`)
2. **OddsLabel 위치 조정** — 현재 (570, 10)이나 기존 2줄 레이아웃과 안 맞을 수 있음
3. **Patch 시 위치 교정 주의** — Patch를 다시 돌리면 수동 배치가 리셋될 수 있음
4. **MCP 테스트 미완** — 새 세션에서 연결 확인 필요
