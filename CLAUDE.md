# DopamineRace

## 프로젝트
- 2D 픽셀아트 경마 배팅 | Unity 6000.3.7f1 | GitHub: https://github.com/LeeJuls/DopamineRace
- 씬: TitleScene(0) → SampleScene(1) | 레이스: V4 HP+CP | 타입: Runner/Leader/Chaser/Reckoner

## Git
- 커밋: `[C]`=Claude `[L]`=오너 `[UI]`=디자이너 (병기 가능: `[C][DOC]`)
- push는 명시 요청 시에만

## 에이전트
`.claude/agents/` — leader(PM) · balance(수치) · client(Unity) · qa(검증) · marketing(Steam)

## 다국어
- `Resources/Data/StringTable.csv` — 7개 언어 (ko·en·ja·zh_CN·de·es·br)
- 하드코딩 금지 — `Loc.Get("키")` 사용

## 주요 클래스
| 클래스 | 규칙 |
|--------|------|
| `RacerController` | 수정 시 `RaceBacktestWindow` 반드시 동시 수정 |
| `SceneBootstrapper` | partial 다수 — 모두 함께 수정 |
| `OddsCalculator` | static, MonoBehaviour 아님 |
| `CharacterRecord` | `charId` 기준 저장, DisplayName 금지 |

## 캐릭터 ID
- `charId` = `char.type.name.NNN` (UID) | `charName` = `str.char.NNN.name` (Loc 키) | `DisplayName` = UI 전용
- `CharacterDB_V4.csv`: col0=char_id, col1=char_name, col12=prefab, col13=attack_prefab, col16=skill_desc

## 문서
- 명세서: `Docs/specs/SPEC-XXX_제목_명세서_YYYYMMDD.md`
- 히스토리: `Docs/history/제목_히스토리_YYYYMMDD.md`

## MCP
- Play/Recompile 중 사용 자제 (좀비 프로세스)
- 새 세션 전: `Get-Process unity-code-mcp-stdio | Stop-Process -Force`
- 프리팹 재생성: `DopamineRace > Create Betting UI Prefabs`

## 디자인 파일
`Assets/Design/Incoming/UI/` + `전달노트.md` 확인 → 올바른 위치로 이동+적용
