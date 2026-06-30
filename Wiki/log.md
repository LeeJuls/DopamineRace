# Wiki 작업 로그

> 항목이 500개 초과 시 `log-YYYY.md`로 이름 변경 후 새 `log.md` 시작.

---

## 2026-07-01

- **[INGEST]** BGM 크로스페이드 시스템 → 신규 `시스템/사운드_시스템.md` 생성 (BGMManager 듀얼 AudioSource·API·상태 전환 흐름·주의사항)
- **[UPDATE]** `히스토리/개발_타임라인.md` — 2026-07-01 BGM 크로스페이드 마일스톤 추가
- **[UPDATE]** `Index.md` — 사운드 시스템 링크 추가

---

## 2026-06-30

- **[UPDATE]** `시스템/배팅_시스템.md` — BetAmountModal 배팅가능 표시 섹션 추가 (BettableInfo 구조·BettableClickOverlay 클릭 패턴·StringTable 키)
- **[UPDATE]** `시스템/통화_시스템.md` — ItemInfoPopup sortingOrder=2000 반영, BetAmountModal 재사용 패턴 추가
- **[UPDATE]** `히스토리/개발_타임라인.md` — 2026-06-30 배팅가능 표시 마일스톤 추가
- **[INGEST]** `EnterPlayMode_DomainReload_히스토리_20260630.md` → 신규 `워크플로우/에디터_최적화.md` 생성 (Domain Reload OFF 패턴, SubsystemRegistration 리셋, 싱글턴 OnDestroy, DestroyImmediate 규칙) ← Docs/history/EnterPlayMode_DomainReload_히스토리_20260630.md
- **[UPDATE]** `워크플로우/에이전트_가이드.md` — `performance` 에이전트 추가 (13번째), 위임 기준에 에디터 최적화 항목 추가
- **[UPDATE]** `히스토리/개발_타임라인.md` — 영상 재인코딩(82MB→23MB) + Domain Reload 안전화 마일스톤(2026-06-30) 추가, 다음 예정에 DisableSceneReload 감사 항목 추가
- **[UPDATE]** `Index.md` — 에디터 최적화 링크 추가 (워크플로우 섹션), 에이전트 가이드 12→13개 갱신

---

## 2026-06-28

- **[INGEST]** SPEC-049 라이브 포트레이트 단계 A~D → 신규 `시스템/라이브_포트레이트.md` 생성 (PNG→mp4 비디오, §0 측정·아키텍처·단계표·알려진이슈) ← Docs/specs/SPEC-049_*
- **[UPDATE]** 주요 클래스 (`아키텍처/주요_클래스.md`) — CharacterVideoController·CharacterInfoPopup(비디오 규칙) 섹션 + CharacterVideoPrefabPatcher 에디터 도구 행 추가
- **[UPDATE]** SPEC 인덱스 — 라이브 포트레이트(spec049) 섹션 추가, 제목 spec001~049 갱신, 관련 페이지 링크
- **[UPDATE]** 개발 타임라인 — SPEC-049 단계 A~D 마일스톤(2026-06-28) + Play 버그수정 기록, 다음예정에 E~G 추가
- **[UPDATE]** Index.md — 게임 시스템에 라이브 포트레이트 링크(🔄) 추가
- **[UPDATE]** 라이브 포트레이트 (`시스템/라이브_포트레이트.md`) — 단계 E ✅ 반영(트랜스코드 H264·무음). **알려진 이슈 정정**: `Color primaries 0` 경고는 트랜스코드로 제거 안 됨(Unity 변환본도 메타 없음)→무해 수용, 근본 제거는 ffmpeg bt709 주입 필요 ← SPEC-049 단계 E
- **[INGEST]** 라이브포트레이트_히스토리_20260628 → 라이브_포트레이트(A~G 완료·F 검증·히스토리 source 추가)·SPEC인덱스(SPEC-049 ✅)·개발타임라인(완성 마일스톤·다음예정 정리) 갱신. SPEC-049 구현 완료(빌드 스모크만 owner 잔여) ← Docs/history/라이브포트레이트_히스토리_20260628.md
- **[UPDATE]** 라이브_포트레이트 "알려진 이슈" — **Color primaries 경고 근본 해소(후속)** 반영: 6개 mp4 SPS VUI bt709 무손실 주입(ffmpeg h264_metadata bsf) + 트랜스코드 OFF. WMF는 SPS VUI만 읽음(colr atom 무시). owner Play 경고 0 검증 ← Docs/plans/colorprimaries_fps_재인코딩_20260628.md

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

## 2026-06-29

- **[INGEST]** 아이템 정보 팝업 (SPEC-050) → `시스템/통화_시스템.md`에 "아이템 정보 팝업" 섹션 추가(ItemInfoPopup/Trigger 모듈, 키 규칙), `시스템/다국어_시스템.md`에 `str.dopamine.item.*` 키 규칙 추가, SPEC 인덱스·개발 타임라인 갱신 ← SPEC-050 명세서·히스토리

---

## 2026-06-27

- **[UPDATE]** SPEC 인덱스 — SPEC-046 상태 갱신 (Steam Publish 완료)
- **[UPDATE]** 개발 타임라인 — Steam 도전과제 다국어 로컬라이즈 마일스톤 추가
- **[UPDATE]** 통화 시스템 (`시스템/통화_시스템.md`) — SPEC-047 경제 단방향 전환 반영: 보상공식 개정(적중 시 스톤만, 젤리 반환 없음), 구공식 폐기 기록, 도전과제 임계값 표 추가
- **[UPDATE]** 다국어 시스템 (`시스템/다국어_시스템.md`) — SPEC-048 번체(tw) 추가 반영: 8개 언어 표, Fusion Pixel Font 섹션 신규, 폰트 라우팅 표
- **[UPDATE]** SPEC 인덱스 — SPEC-047·SPEC-048 섹션 추가, 제목 spec001~048로 갱신
- **[UPDATE]** 개발 타임라인 — SPEC-047·SPEC-048 마일스톤 추가, 잔여 작업 목록 갱신
- **[UPDATE]** Index.md — 다국어 시스템 설명 7개→8개 언어 갱신
- **[INGEST]** 스팀도전과제연동완성_히스토리_20260627 → 신규 `시스템/스팀_도전과제.md` 생성 + Index·SPEC인덱스·개발타임라인 갱신. SPEC-046 명세서 DR_STEAM "모든 타겟" 오류를 "Standalone 전용"으로 정정 ← his(연동 완성)

---

## 2026-06-24

- **[INGEST]** 보안 & 치팅 방어 페이지 생성 (`도파민 프로젝트/시스템/보안_치팅방어.md`) ← SPEC-044, his048
- **[UPDATE]** SCHEMA.md §4 태그 분류법에 `보안` 그룹 추가 (`보안·치팅방어·HMAC·시크릿·무결성`)
- **[UPDATE]** Index.md — 🔒 보안 섹션 추가
- **[UPDATE]** 에이전트 가이드 — `security` 에이전트 추가 (`.claude/agents/security.md`)
- **[UPDATE]** SPEC 인덱스 — spec044 보안 그룹 추가
- **[UPDATE]** 개발 타임라인 — his048 보안 운영화 마일스톤
- **[UPDATE]** 주요 결정사항 — 치팅 방어 전략 + 시크릿 git 금지 결정 추가
- **[INGEST]** 빌드 & 배포 페이지 생성 (`도파민 프로젝트/워크플로우/빌드_배포.md`) ← SPEC-045, his050 (IL2CPP·[MenuItem]·Steam)
- **[UPDATE]** SCHEMA §4 태그분류법 워크플로우에 `빌드`·`배포` 추가
- **[UPDATE]** Index.md 워크플로우 — 빌드_배포 링크
- **[UPDATE]** 에이전트 가이드 — `director`·`build` 추가 (10→12개)
- **[UPDATE]** SPEC 인덱스 — spec045 빌드/배포 그룹 + spec023
- **[UPDATE]** 개발 타임라인 — his050 빌드 자동화 마일스톤
- **[INGEST]** 보안 하드닝 구현 (`시스템/보안_치팅방어.md` 방어 레이어 구현현황 갱신) ← his049 (SecureInt·저장HMAC·.claude deny)
- **[UPDATE]** 개발 타임라인 — his049 보안 하드닝 구현 마일스톤
