# DopamineRace — Claude 프로젝트 설정

## 프로젝트 기본 정보
- **게임**: 2D 픽셀아트 경마 배팅 게임 (도파민레이스)
- **엔진**: Unity 6000.3.7f1
- **GitHub**: https://github.com/LeeJuls/DopamineRace
- **상세 기술 문서**: `Docs/MEMORY.md` (세션 시작 시 필독)

## 핵심 목표 (절대 잊지 말 것)
> **캐릭터 레이싱 배팅 게임** — 유저가 캐릭터 레이싱 보는 맛은 **우마무스메처럼 쫄깃한 맛**을 줘야 함.

---

## Git 규칙

### 커밋 작성자 식별 규칙 ← **반드시 준수**
> 누가 커밋했는지 한눈에 알 수 있게 접두사로 구분한다.

| 작성자 | 접두사 | 예시 |
|--------|--------|------|
| **Claude (AI)** | `[C]` | `[C] feat: 배팅 UI 개선` |
| **오너 (LeeJuls)** | `[L]` | `[L] fix: 배당률 수정` |
| **UI 디자이너** | `[UI]` | `[UI] design: 배팅 패널 레이아웃` |
| **기타 작업자** | `[이니셜]` | `[K] art: 캐릭터 스프라이트` |

- **Claude는 커밋 시 반드시 `[C]`로 시작**할 것 — 빠뜨리면 안 됨
- 분류 병기 가능: `[C][DOC]`, `[C][UI]` 등
- **push는 반드시 오너(LeeJuls)에게 확인 후 진행**

---

## 에이전트 공용 규칙
1. **단계별 개발**: 개발 → 테스트 → 수정 → 테스트 사이클 반복
2. **사용자 호출**: 혼자 해결 불가 시 반드시 오너(LeeJuls) 호출
3. **기획 분석 및 제안**: 더 좋은 안 있으면 적극 제안
4. **에이전트 간 논의**: 각자 분석·논의하여 최적안 도출
5. **push 전 확인**: 오너 확인 없이 절대 push 금지
6. **핵심 목표 준수**: 우마무스메급 쫄깃한 레이싱 체감이 모든 개발의 기준
7. **다국어 필수**: 모든 UI 문자열은 `StringTable.csv` 키 추가 필수 — 하드코딩 금지 (7개 언어)

## 에이전트 구성
| 에이전트 | 역할 |
|---------|------|
| `leader` | PM·기획·문서·업무 배분 |
| `balance` | 밸런스 수치 설계·백테스트 |
| `client` | Unity 클라이언트 개발 |
| `qa` | QA 계획·검증·버그 예측 |

---

## 주요 아키텍처 요약
- **씬**: TitleScene(0) → SampleScene(1)
- **레이스 자원**: HP(지구력/스프린트) + CP(침착/슬립스트림)
- **캐릭터 타입**: Runner(도주) / Leader(선행) / Chaser(선입) / Reckoner(추입)
- **ConsumeHP() 4단계**: Positioning(0~0.5랩) → FormationHold(0.5~1랩) → Strategy(1랩~) → Legacy
- **hpSpeedCompress=0.85** → 기본속도 격차 ~0.83% 압축 (HP 부스트가 레이스 결과 결정)
- **MCP**: `Assets/Plugins/UnityCodeMcpServer` (Unity ↔ Claude 직접 제어)
- **프리팹**: 코드 자동생성 → `DopamineRace > Create Betting UI Prefabs` 필수
- **다국어**: `Resources/Data/StringTable.csv` (7개 언어: ko·en·ja·zh_CN·de·es·br)

### 캐릭터 ID 체계 (혼용 금지)
- `charId` = `char.type.name.NNN` — 진짜 UID (예: `char.leader.thunder.000`)
- `charName` = `str.char.NNN.name` — Loc 키 (0-based)
- `DisplayName` = `Loc.Get(charName)` — UI 표시 전용
- `CharacterDatabase.FindById(charId)` — 검색은 항상 charId 기준

### 주요 클래스
| 클래스 | 특징 |
|--------|------|
| `OddsCalculator` | static 클래스, MonoBehaviour 아님 |
| `CharacterRecord` | **charId** 기준 저장 — DisplayName 혼용 금지 |
| `RacerController` | HP+CP 이중 자원 시스템 |
| `RaceBacktestWindow` | RacerController 로직 미러링 — 반드시 동시 수정 |
| `SceneBootstrapper.Betting.cs` | 프리팹 기반 (CharacterItemUI + BettingPanel) |

### CharacterDB.csv 컬럼
`col 0=char_id(UID), col 1=char_name(Loc키), col 15=char_skill_desc, col 16=char_illustration`

---

## 문서 관리 규칙
> 날짜는 **파일명 맨 뒤**에 붙인다 — 제목으로 정렬/검색이 쉽게.

- 명세서: `Docs/specs/SPEC-XXX_제목_명세서_YYYYMMDD.md`
- 히스토리: `Docs/history/제목_히스토리_YYYYMMDD.md`
- 기타 문서: `Docs/제목_YYYYMMDD.md`

---

## 디자인 파일 전달 시스템
UI 디자이너가 파일을 전달하면:
1. `Assets/Design/Incoming/[날짜_작업명]/` 폴더 확인
2. 같은 폴더의 `전달노트.md` 읽기
3. Claude에게: "디자인 파일 적용해줘" → 파일을 올바른 위치로 이동+적용

자세한 가이드: `Docs/20260301_UI디자인파일전달_워크플로우.md`

---

## MCP 주의사항
- Play/Recompile 중 MCP 사용 자제 (좀비 프로세스 누적 가능)
- 새 세션 전: `Get-Process unity-code-mcp-stdio | Stop-Process -Force` 실행 권장
- `SAVE_VERSION = 2` — 구버전 세이브 자동 삭제됨
