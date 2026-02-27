# DopamineRace — Claude 프로젝트 설정

## 프로젝트 기본 정보
- **게임**: 2D 픽셀아트 경마 배팅 게임 (도파민레이스)
- **엔진**: Unity 6000.3.7f1
- **작업 경로**: `D:\Unity_Project\DopamineRace`
- **GitHub**: https://github.com/LeeJuls/DopamineRace
- **상세 기술 문서**: `Docs/MEMORY.md` (필독)

## 핵심 목표 (절대 잊지 말 것)
> **캐릭터 레이싱 배팅 게임** — 유저가 캐릭터 레이싱 보는 맛은 **우마무스메처럼 쫄깃한 맛**을 줘야 함.

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
- 커밋은 단계별 작업 완료 시점마다

## 에이전트 공용 규칙
1. **단계별 개발**: 한번에 다 개발하지 말고 → 개발 → 테스트 → 수정 → 테스트 사이클 반복
2. **사용자 호출**: 혼자 해결 불가능한 상황 발생 시 반드시 오너(LeeJuls) 호출
3. **기획 분석 및 제안**: 받은 기획안을 각 에이전트 시각으로 분석, 더 좋은 안 있으면 적극 제안
4. **에이전트 간 논의**: 각자의 안을 에이전트끼리 분석·논의하여 최적안 도출
5. **push 전 확인**: 오너 확인 없이 절대 push 금지
6. **핵심 목표 준수**: 우마무스메급 쫄깃한 레이싱 체감이 모든 개발의 기준
7. **다국어 필수**: UI에 표시되는 모든 문자열은 **반드시 `StringTable.csv`에 키를 추가**하여 다국어 처리할 것 — 코드 하드코딩 금지 (7개 언어: ko·en·ja·zh_CN·zh_TW·es·fr)

## 에이전트 구성
| 에이전트 | 역할 |
|---------|------|
| `leader` | PM·기획·문서·업무 배분 |
| `balance` | 밸런스 수치 설계·백테스트 |
| `client` | Unity 클라이언트 개발 |
| `qa` | QA 계획·검증·버그 예측 |

## 주요 아키텍처 요약
- **씬**: TitleScene(0) → SampleScene(1)
- **레이스 자원**: HP(지구력/스프린트) + CP(침착/슬립스트림)
- **캐릭터 타입**: Runner(도주) / Leader(선행) / Chaser(선입) / Reckoner(추입)
- **3페이즈 전략**: 포지셔닝(0~0.5랩) → 대형유지(0.5~1랩) → 전략(1랩~)
- **MCP**: `Assets/Plugins/UnityCodeMcpServer` (Unity ↔ Claude 직접 제어)
- **프리팹**: 코드 자동생성 → `DopamineRace > Create Betting UI Prefabs` 필수
- **다국어**: `Resources/Data/StringTable.csv` (7개 언어)

## 문서 관리 규칙
> 날짜는 **파일명 맨 뒤**에 붙인다 — 제목으로 정렬/검색이 쉽게.

- 명세서: `Docs/specs/SPEC-XXX_제목_명세서_YYYYMMDD.md`
- 히스토리: `Docs/history/제목_히스토리_YYYYMMDD.md`
- 기타 문서: `Docs/제목_YYYYMMDD.md`

## MCP 주의사항
- Play/Recompile 중 MCP 사용 자제 (좀비 프로세스 누적 가능)
- 새 세션 전: `Get-Process unity-code-mcp-stdio | Stop-Process -Force` 실행 권장
