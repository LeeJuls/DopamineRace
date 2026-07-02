# DopamineRace

## 프로젝트
- 2D 픽셀아트 경마 배팅 | Unity 6000.3.7f1 | GitHub: https://github.com/LeeJuls/DopamineRace
- 씬: TitleScene(0) → SampleScene(1) | 레이스: V4 HP+CP | 타입: Runner/Leader/Chaser/Reckoner

## Git
- 커밋: `[C]`=Claude `[L]`=오너 `[UI]`=디자이너 (병기 가능: `[C][DOC]`)
- push는 명시 요청 시에만

## 위키 (메인 저장소)
- 경로: `Wiki/` (레포 내)
- **세션 시작 시** SCHEMA.md → Index.md → log.md(최근 20줄) 순서로 읽어 현재 위키 상태 파악
- SPEC/히스토리 완료 후 `/wiki-ingest` 실행하여 위키 반영
- 재활용 가치 있는 Q&A는 `queries/` 에 저장

## 에이전트
`.claude/agents/` — leader(PM) · balance(수치) · client(Unity) · qa(검증) · design(UX/UI) · marketing(Steam) · security(보안) · build(빌드) · director(오케스트레이션) · setting(세계관) · scenario(시나리오)

## 모델 배분 (에이전트 위임)
- **메인 루프는 한 모델 고정**(기본 Opus), 작업별로 서브에이전트를 해당 티어로 스폰 — 메인 모델 스위칭은 프롬프트 캐시 무효화라 지양
- **★Fable 최소 사용(토큰 절감)**: Fable로 분류된 일도 Fable이 전부 하지 않는다. 큰 작업은 **10%–80%–10%**로 분할:
  1. **핵심 설계·뼈대(≈10%)=Fable** — 방향·구조·핵심 판단만. 이 단계에서 Fable이 **나머지 실행을 Opus/Sonnet 중 어디에 내릴지 배분까지 결정**해 넘긴다.
  2. **실행: 코딩·문서집필·반복작업·병렬리뷰(≈80%)=Opus/Sonnet**(Fable 아님). 다관점 리뷰도 전부 Fable 금지 — 핵심 1개만 Fable, 나머지는 Opus/Sonnet.
  3. **완성본 최종 검토(≈10%)=Fable** — 적대적 최종 점검.
- **Fable 적용 영역**(위 1·3 단계에 한정): 깊은 판단·게임기획·밸런스 설계·플랜 적대검증·세계관/시나리오 창작. **막히면(불가/거부/미제공) → Opus 폴백**
- **Opus**: 오케스트레이션(메인 루프)·난도 높은 구현·문서 집필·미러 정합 판단
- **Sonnet**: 승인된 구현·에디터 툴 코드·커밋/푸시(커밋·push는 항상 Sonnet)
- **Haiku**: 탐사·검색·파일 읽기·결정된 텍스트 단순 정리 (품질 민감한 다국어 정리는 Sonnet)
- 기준: 재작업 비용 큰 **핵심 설계·최종 검토만** → Fable / **실행·검증가능·기계적** → Opus·Sonnet·Haiku / 애매하면 메인(Opus) 직접
- 예시: **선례·가이드 있는 단순 기능 수정**(되돌리기 쉽고 결과 검증가능; 예: 재인코딩發 색경고 재수정) → **Opus 판단 + Sonnet/Haiku 실행, Fable 아님**

## 다국어
- `Resources/Data/StringTable.csv` — 8개 언어 (ko·en·jp·cn·de·es·br·tw) — cn=간체, tw=번체(SPEC-048)
- **모든 문자열 하드코딩 절대 금지** — 사용자에게 노출되는 문자열은 예외 없이 StringTable.csv에 키 발급 후 `Loc.Get("str.xxx")` 사용
- **키 발급 규칙**: `str.{영역}.{기능}.{항목}` 형식 (예: `str.bet.modal.title`)
- **커밋 금지 조건**: 코드에 새 문자열 추가 시 StringTable.csv 키 미발급 상태로 커밋 불가
- **검증**: `DopamineRace > Validate StringTable Keys` — 커밋 전 실행, 누락 키 0 확인 필수
- 서수+접미사 라벨은 포맷 키 재활용 | 간접 참조(`$"str.char.{id}.name"`) 시 해당 범위 키 일괄 발급 확인
- 폰트 상세(CJK 라우팅·FontHelper): `Wiki/도파민 프로젝트/시스템/다국어_시스템.md`

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
- Play/Recompile 중 사용 자제 (좀비 프로세스) | 새 세션 전: `Get-Process unity-code-mcp-stdio | Stop-Process -Force`
- 프리팹 재생성: `DopamineRace > Create Betting UI Prefabs`

## 디자인 파일
`Assets/Design/Incoming/UI/` + `전달노트.md` 확인 → 올바른 위치로 이동+적용
