# Wiki 작업 로그

> 항목이 500개 초과 시 `log-YYYY.md`로 이름 변경 후 새 `log.md` 시작.

---

## 2026-07-02

- **[INGEST]** UI 다국어 텍스트 자동축소(UITextFit) → `시스템/다국어_시스템.md` "텍스트 자동 축소 — UITextFit" 섹션 최신화(적용처에 트랙패널·배팅리스트 추가, 함정 메모 2건 추가: ⑤ 인접 고정좌표 라벨은 자기 Shrink만으론 충돌 방지 불가 ⑥ 서수기호 º는 폰트버그 아님·오독위험 없으면 원문자 유지). `아키텍처/주요_클래스.md`에 `UITextFit` 섹션 신규(API·적용처·수정규칙). es/tw 오버플로 신고 3건(타입칩·스킬설명 세로잘림·배당라벨 겹침) 해소 + 후속 확장(트랙패널 5라벨, 배팅리스트 전적·2위횟수 라벨충돌 — 8자↑ 전적이 이웃라벨 침범하는 언어무관 레이아웃 버그). ui-designer·client·qa 3자 플랜 검토로 레이더(XCharts sizeDelta 강제재설정)·배당라벨(point앵커 겹침) 설계 재검토 반영 ← 대화 세션(커밋 95bc837·2709b42·5e4ca5e, 미푸시)
- **[UPDATE]** `히스토리/개발_타임라인.md` — 2026-07-02 UITextFit 마일스톤 추가
- **[UPDATE]** `Index.md` — 다국어 시스템 설명에 UITextFit 언급 추가
- **[INGEST]** 트랙 스탯 상성 데이터 주도 V4 실연결 → 신규 `시스템/트랙_시스템.md` 생성(5트랙 개요·**V4 레버 Live/Dead 현황표**·stat_affinity 콜론 인코딩 스키마·공유 헬퍼 미러·feature flag·확장 T1/T2·밸런스). 사막=`Power:0.08`·고원=`Intelligence:0.10`. `TrackStatAffinity`(V4StatType enum+ParseList+ComputeVmaxMultiplier 공유헬퍼)·`GameSettingsV4.v4_enableTrackStatBonus` 플래그·`TrackStatAffinityValidator`(오타 검출)·헤드리스 `RunHeadless(...,trackId)` 하네스·백테스트 CSV소스화 신설. 기존 트랙 상성이 V1-V3 스탯 기준이라 V4서 死였음을 규명. leader·balance·client·feature-director·qa 3라운드 검토 ← 대화 세션(커밋 전)
- **[UPDATE]** `Index.md` — 게임 시스템에 트랙 시스템 링크 추가
- **[UPDATE]** `히스토리/개발_타임라인.md` — 2026-07-02 트랙 스탯 상성 마일스톤 추가
- **[UPDATE]** `아키텍처/주요_클래스.md` — TrackStatAffinity/TrackData/TrackDatabase 섹션 + 에디터 도구에 TrackStatAffinityValidator 추가
- **[INGEST]** 캐릭터 스토리 팝업 가독성 + 세계관 정본 + 32명 시나리오 8개 언어 번역 → `시스템/다국어_시스템.md`에 "캐릭터 시나리오 번역" 섹션 신규(224셀 번역·시맨틱 컬러태그 규약[skill]/[key]/§TIP§·스탯/캐릭터명 용어일치·**슬립 표준화** 气流/Sog/Rebufo/Vácuo/氣流·CSV안전 전각「」·검증 리터럴0/한글0·032미완성 잔여), frontmatter 8개언어→시나리오 병기·updated 갱신 ← Docs/history/캐릭터스토리팝업_가독성세계관다국어_히스토리_20260702.md
- **[UPDATE]** `캐릭터/캐릭터_개요.md` — **★세계관 정본 확정** 섹션 신규(B안 단일 도파미나 제국 정본 / SPEC-040·세계관_A 5왕국안 폐기 / 정본 3문서 세계관_B·설정집_B안·시나리오집 표 / 폐기 배너·worldview-canon 메모리), "시나리오 정합 보강 3명"(022 주홍 선버스트 해안 재해석·크리스탈 혼동해소 / 028 초원 왕관야구모자 / 029 칵테일 천사날개), "캐릭터 스토리 팝업 가독성" 섹션 추가, frontmatter sources/related 갱신
- **[INGEST]** SFX 시스템 후속 개선 4건 → `시스템/사운드_시스템.md`에 SFX 섹션 신규 작성(아키텍처 3층·SFXEntry 필드·12키 트리거표·하드코딩방지·클릭음 억제·Validator·미러규칙 예외·구현위치). 볼륨 3배 확장(`SFXEntry.volume` Range 0~3), `delay`/`loopInterval` 분리 구현(코루틴 기반 루프 재구현), buff.crit 편중 오인 진단(버그 아님, VFX 재사용+빈도차 확인), START 사운드를 배팅확정(`BetAmountModal.OnConfirm`)으로 이동+`SuppressAutoClick`으로 클릭음 억제, 카운트다운 사운드(`sfx.race.countdown`) 신규 연결 ← 대화 세션(커밋 전)
- **[UPDATE]** `아키텍처/주요_클래스.md` — `SFXManager`/`SFXSettings`/`SFXKeys` 클래스 섹션 신규, 에디터 도구 표에 `SFXSettingsValidator` 행 추가
- **[UPDATE]** `히스토리/개발_타임라인.md` — 2026-07-01 SFX 시스템 신설 마일스톤 + 2026-07-02 SFX 후속 개선 4건 마일스톤 추가
- **[UPDATE]** `Index.md` — 사운드 시스템 설명에 SFX(SFXKeys/SFXSettings/SFXManager, 12키) 추가

---

## 2026-07-01

- **[INGEST]** `시스템/V4_레이스_시스템.md` — §5 자동 테스트 도구 갱신(헤드리스 `RaceBacktestWindow` 주력·`AutoRaceRunnerWindow` 보조+프리징수정 반영) + 신규 §5b "패시브 스킬 & goyo 클러치 재설계"(LuckClutch 트리거, 확정수치 chance0.40×1.20, 80%→56% 결과). `아키텍처/주요_클래스.md` — `RaceBacktestWindow` 행 신규(헤드리스 API), `AutoRaceRunnerWindow`·`RacerController` 행 갱신(프리징수정·UpdateV4LuckClutch). 개발_타임라인 마일스톤 추가. tags에 `밸런스` 추가 ← Docs/history/헤드리스백테스트_goyo클러치_AutoRaceRunner_히스토리_20260701.md
- **[INGEST]** `시스템/통화_시스템.md` — 환전/구제/고양이의 힘 섹션 → **럭키 잭팟(SPEC-051)** 전면 갱신 (마네키네코, 무캡 산식·게임오버 SSOT 재정의·0젤리 버그픽스·무캡 백테스트·GameSettings 필드·마네키네코 UI). 관련클래스 잭팟 API 반영. 상태/프론트매터 SPEC-051 추가 ← Docs/history/럭키잭팟_0젤리버그픽스_히스토리_20260701.md
- **[UPDATE]** `SPEC_인덱스.md` — SPEC-051 럭키 잭팟 섹션 추가, 제목 spec001~051 갱신
- **[UPDATE]** `히스토리/개발_타임라인.md` — 2026-07-01 마일스톤 2건 추가 (UI 흰배경통일+NameEntry 오버레이 격리 / 럭키 잭팟 SPEC-051)
- **[INGEST]** BGM 크로스페이드 시스템 → 신규 `시스템/사운드_시스템.md` 생성 (BGMManager 듀얼 AudioSource·API·상태 전환 흐름·주의사항)
- **[UPDATE]** `히스토리/개발_타임라인.md` — 2026-07-01 BGM 크로스페이드 마일스톤 추가
- **[UPDATE]** `Index.md` — 사운드 시스템 링크 추가
- **[UPDATE]** `히스토리/개발_타임라인.md` — CharacterInfoPopup UI 폴리싱 마일스톤 추가 (제목 단순화·타입 영어명·BestFit·스킬 2줄 레이아웃)
- **[INGEST]** SFX 시스템 신설 → `시스템/사운드_시스템.md` frontmatter/상태 갱신(BGM+SFX 병기). `SFXKeys`(키 상수)+`SFXSettings`(ScriptableObject)+`SFXManager`(PlaySFX/PlayLoop/StopLoop) 3단 구조, 무음 불변조건, 9키(충돌3·버프3·발소리·마네키네코2종)+이후 START·잭팟오픈 확장(12키), `SFXSettingsValidator`(reflection) ← 대화 세션(커밋 12e0025)

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
