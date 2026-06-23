# Wiki 작업 로그

> 항목이 500개 초과 시 `log-YYYY.md`로 이름 변경 후 새 `log.md` 시작.

---

## 2026-06-21

- **[INIT]** 위키 초기화 — Karpathy LLM Wiki 패턴 기반, Nous Research Hermes Agent 스펙 v2.1.0 적용
- **[INGEST]** 프로젝트 개요 페이지 생성 (`도파민 프로젝트/00_프로젝트_개요.md`)
- **[INGEST]** 기술 스택 페이지 생성 (`도파민 프로젝트/01_기술스택_환경.md`)
- **[INGEST]** V4 레이스 시스템 페이지 생성 (`도파민 프로젝트/시스템/V4_레이스_시스템.md`) ← spec012
- **[INGEST]** HP/CP 시스템 페이지 생성 (`도파민 프로젝트/시스템/HP_CP_시스템.md`) ← spec006
- **[INGEST]** 배팅 시스템 페이지 생성 (`도파민 프로젝트/시스템/배팅_시스템.md`) ← spec007, spec028-step02, spec037, spec038
- **[INGEST]** 통화 시스템 페이지 생성 (`도파민 프로젝트/시스템/통화_시스템.md`) ← spec028 master
- **[INGEST]** 글로벌 랭킹 페이지 생성 (`도파민 프로젝트/시스템/글로벌_랭킹.md`) ← spec028-step04~05
- **[INGEST]** 다국어 시스템 페이지 생성 (`도파민 프로젝트/시스템/다국어_시스템.md`) ← CLAUDE.md
- **[INGEST]** 캐릭터 개요 페이지 생성 (`도파민 프로젝트/캐릭터/캐릭터_개요.md`) ← spec013
- **[INGEST]** 타입별 전략 페이지 생성 (`도파민 프로젝트/캐릭터/타입별_전략.md`) ← spec013
- **[INGEST]** 코드 구조 페이지 생성 (`도파민 프로젝트/아키텍처/코드_구조.md`)
- **[INGEST]** 주요 클래스 페이지 생성 (`도파민 프로젝트/아키텍처/주요_클래스.md`) ← CLAUDE.md
- **[INGEST]** 에이전트 가이드 페이지 생성 (`도파민 프로젝트/워크플로우/에이전트_가이드.md`) ← .claude/agents/
- **[INGEST]** 개발 워크플로우 페이지 생성 (`도파민 프로젝트/워크플로우/개발_워크플로우.md`) ← CLAUDE.md
- **[INGEST]** MCP 사용법 페이지 생성 (`도파민 프로젝트/워크플로우/MCP_사용법.md`) ← CLAUDE.md
- **[INGEST]** 개발 타임라인 페이지 생성 (`도파민 프로젝트/히스토리/개발_타임라인.md`) ← his001~045+
- **[INGEST]** 주요 결정사항 페이지 생성 (`도파민 프로젝트/히스토리/주요_결정사항.md`)
- **[INGEST]** SPEC 인덱스 페이지 생성 (`도파민 프로젝트/SPEC_인덱스.md`) ← spec001~040
- **[LINT]** Nous Research 공식 스펙(v2.1.0) 기준 보완 — `log.md` 추가, 프론트매터 `type`·`confidence`·`sources`·`created` 필드 추가, SCHEMA.md 태그 분류법 보강
- **[UPDATE]** AutoSci 패턴 도입 — `/wiki-ingest` 스킬 생성 (`.claude/skills/wiki-ingest.md`), `queries/` 폴더 + `_index.md` 추가, SCHEMA Query 규칙 보강

---

## 2026-06-24

- **[INGEST]** 보안 & 치팅 방어 페이지 생성 (`도파민 프로젝트/시스템/보안_치팅방어.md`) ← SPEC-044, his048
- **[UPDATE]** SCHEMA.md §4 태그 분류법에 `보안` 그룹 추가 (`보안·치팅방어·HMAC·시크릿·무결성`)
- **[UPDATE]** Index.md — 🔒 보안 섹션 추가
- **[UPDATE]** 에이전트 가이드 — `security` 에이전트 추가 (`.claude/agents/security.md`)
- **[UPDATE]** SPEC 인덱스 — spec044 보안 그룹 추가
- **[UPDATE]** 개발 타임라인 — his048 보안 운영화 마일스톤
- **[UPDATE]** 주요 결정사항 — 치팅 방어 전략 + 시크릿 git 금지 결정 추가
- **[INGEST]** 보안 하드닝 구현 (`시스템/보안_치팅방어.md` 방어 레이어 구현현황 갱신) ← his049 (SecureInt·저장HMAC·.claude deny)
- **[UPDATE]** 개발 타임라인 — his049 보안 하드닝 구현 마일스톤
