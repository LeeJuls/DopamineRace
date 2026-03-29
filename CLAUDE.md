# DopamineRace

## 프로젝트
- 2D 픽셀아트 경마 배팅 게임 | Unity 6000.3.7f1
- GitHub: https://github.com/LeeJuls/DopamineRace
- 핵심 목표: **우마무스메처럼 쫄깃한 레이싱** — 역전 드라마, 타입별 전략

## Git 커밋 접두사
`[C]`=Claude, `[L]`=오너, `[UI]`=디자이너, `[이니셜]`=기타 — 병기 가능: `[C][DOC]`

## 에이전트
`.claude/agents/` 참조 — leader(PM), balance(수치), client(Unity), qa(검증)

## 다국어
- `Resources/Data/StringTable.csv` — 7개 언어 (ko·en·ja·zh_CN·de·es·br)
- UI 문자열 하드코딩 금지 — `Loc.Get("키")` 사용

## 아키텍처
- 씬: TitleScene(0) → SampleScene(1)
- 레이스: V4 시스템 — HP(지구력) + CP(침착) | `GameSettings.asset` + `GameSettingsV4.asset`
- 타입: Runner(도주) / Leader(선행) / Chaser(선입) / Reckoner(추입)
- MCP: `Assets/Plugins/UnityCodeMcpServer` — Play/Recompile 중 사용 자제
- 프리팹: 코드 자동생성 → `DopamineRace > Create Betting UI Prefabs`

## 캐릭터 ID (혼용 금지)
| 필드 | 용도 | 예시 |
|------|------|------|
| `charId` | UID, 검색/저장 기준 | `char.leader.thunder.000` |
| `charName` | Loc 키 (0-based) | `str.char.000.name` |
| `DisplayName` | UI 표시 전용 | `Loc.Get(charName)` |

## 주요 클래스
| 클래스 | 비고 |
|--------|------|
| `RacerController` | V4 HP+CP 이중 자원 |
| `RaceBacktestWindow` | RacerController 미러링 — 반드시 동시 수정 |
| `OddsCalculator` | static, MonoBehaviour 아님 |
| `CharacterRecord` | charId 기준 저장 — DisplayName 금지 |
| `SceneBootstrapper` | partial 6개 — 항상 함께 수정 |

## CharacterDB_V4.csv
`col 0=char_id, 1=char_name, 12=prefab경로, 13=attack_prefab, 16=skill_desc, 17=illustration`

## 문서 규칙
- 명세서: `Docs/specs/SPEC-XXX_제목_명세서_YYYYMMDD.md`
- 히스토리: `Docs/history/제목_히스토리_YYYYMMDD.md`
- 날짜는 파일명 맨 뒤

## 디자인 파일 전달
`Assets/Design/Incoming/UI/` + `전달노트.md` 확인 → 올바른 위치로 이동+적용

## MCP 주의
- Play/Recompile 중 사용 자제 (좀비 프로세스)
- 새 세션 전: `Get-Process unity-code-mcp-stdio | Stop-Process -Force`
