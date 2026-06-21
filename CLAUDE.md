# DopamineRace

## 프로젝트
- 2D 픽셀아트 경마 배팅 | Unity 6000.3.7f1 | GitHub: https://github.com/LeeJuls/DopamineRace
- 씬: TitleScene(0) → SampleScene(1) | 레이스: V4 HP+CP | 타입: Runner/Leader/Chaser/Reckoner

## Git
- 커밋: `[C]`=Claude `[L]`=오너 `[UI]`=디자이너 (병기 가능: `[C][DOC]`)
- push는 명시 요청 시에만

## 위키
- 경로: `D:/Project/Dopamine/DopamineProject/`
- **세션 시작 시** SCHEMA.md → Index.md → log.md(최근 20줄) 순서로 읽어 현재 위키 상태 파악
- SPEC/히스토리 완료 후 `/wiki-ingest` 실행하여 위키 반영
- 재활용 가치 있는 Q&A는 `queries/` 에 저장

## 에이전트
`.claude/agents/` — leader(PM) · balance(수치) · client(Unity) · qa(검증) · design(UX/UI) · marketing(Steam)

## 다국어
- `Resources/Data/StringTable.csv` — 7개 언어 (ko·en·ja·zh_CN·de·es·br)
- **모든 문자열 하드코딩 절대 금지** — UI·로그·에러·툴팁·버튼·모달 등 사용자에게 노출되는 문자열은 예외 없이 StringTable.csv에 키 발급 후 `Loc.Get("str.xxx")` 사용
- **키 발급 규칙**: `str.{영역}.{기능}.{항목}` 형식 (예: `str.bet.modal.title`, `str.exchange.btn.close`)
- **커밋 금지 조건**: 코드에 새 문자열 추가 시 StringTable.csv 키 미발급 상태로 커밋 불가
- **검증**: `DopamineRace > Validate StringTable Keys` — 커밋 전 실행, 누락 키 0 확인 필수
- 서수+접미사 라벨은 포맷 키 재활용 (예: `str.bet.label.rank_bet`="{0} 배팅" + 기존 `first`/`second`)
- 간접 참조(동적 키 조합 `$"str.char.{id}.name"`) 사용 시 해당 범위 키 일괄 발급 확인

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
