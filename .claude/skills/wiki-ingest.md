---
description: 새 SPEC 또는 히스토리 파일을 Obsidian 위키에 자동으로 반영합니다. SPEC_인덱스, 관련 시스템 페이지, 타임라인, log.md를 한 번에 업데이트합니다.
---

# wiki-ingest

## 사용법

```
/wiki-ingest [파일명 또는 경로]

예:
  /wiki-ingest spec041_신기능_20260701.md
  /wiki-ingest Docs/specs/spec041_신기능_20260701.md
  /wiki-ingest his046_신기능구현_히스토리.md
```

인자 없이 실행하면 최근 커밋에서 추가된 SPEC/히스토리 파일을 자동 감지합니다.

---

## 실행 절차

다음 순서를 **반드시** 지켜서 실행한다.

### Step 0 — 컨텍스트 로딩

1. `D:/Project/Dopamine/DopamineProject/SCHEMA.md` 읽기 — 규칙 파악
2. `D:/Project/Dopamine/DopamineProject/Index.md` 읽기 — 존재 페이지 확인
3. `D:/Project/Dopamine/DopamineProject/log.md` 마지막 20줄 읽기 — 최근 작업 확인

### Step 1 — 대상 파일 결정

- 인자가 있으면 해당 파일 사용
- 인자가 없으면 `git log --oneline -5` 확인 → 최근 커밋의 SPEC/히스토리 파일 탐지

대상 파일을 `D:/Unity_Project/DopamineRace/Docs/specs/` 또는 `Docs/history/` 에서 읽는다.

### Step 2 — 대상 파일 분석

읽은 파일에서 다음을 추출한다:
- **제목 / 번호**: spec041, his046 등
- **주요 시스템**: 어느 시스템(레이스/배팅/통화/UI 등)에 해당하는가
- **상태**: ✅ 완료 / 🔄 진행 중 / ⏸ 미착수
- **핵심 변경 내용**: 2~3줄 요약
- **관련 위키 페이지**: Index.md 기준으로 업데이트해야 할 페이지 1~3개 식별

### Step 3 — 위키 페이지 업데이트

식별된 각 페이지에 대해:
1. 해당 페이지 읽기
2. 관련 섹션 업데이트 (상태, 내용 요약, Raw Source 링크 추가)
3. 프론트매터 `updated` 날짜 갱신
4. 교차 링크 `[[]]` 최소 2개 유지 확인

**업데이트 기준:**
- 기존 내용을 덮어쓰지 말고 **보완**한다
- 새 시스템/개념이면 신규 페이지 생성 후 Index.md에 추가
- 200줄 초과 시 분할 권장

### Step 4 — SPEC 인덱스 업데이트

`D:/Project/Dopamine/DopamineProject/도파민 프로젝트/SPEC_인덱스.md`

SPEC 파일이면: 해당 섹션에 행 추가
```
| specXXX | 제목 | ✅/🔄/⏸ |
```

히스토리 파일이면: `도파민 프로젝트/히스토리/개발_타임라인.md` 의 해당 Phase에 항목 추가

### Step 5 — log.md 기록

`D:/Project/Dopamine/DopamineProject/log.md` 맨 위 최신 날짜 섹션에 추가:

```
- **[INGEST]** spec/his 파일명 → 업데이트된 위키 페이지 목록 ← 출처
```

날짜 섹션이 없으면 새 `## YYYY-MM-DD` 섹션 생성.

### Step 6 — 완료 보고

다음 형식으로 요약:
```
✅ wiki-ingest 완료

대상: spec041_신기능_20260701.md
업데이트된 페이지:
  - 도파민 프로젝트/시스템/배팅_시스템.md (신기능 섹션 추가)
  - 도파민 프로젝트/SPEC_인덱스.md (spec041 행 추가)
  - 도파민 프로젝트/히스토리/개발_타임라인.md (Phase 4에 추가)
log.md: [INGEST] 기록 완료
```

---

## 판단 기준

| 상황 | 처리 |
|------|------|
| 기존 페이지에 간단히 추가 가능 | 해당 페이지 업데이트 |
| 완전히 새로운 시스템 | 신규 페이지 생성 + Index.md 추가 |
| 기존 내용과 모순 | 프론트매터에 `contested: true` 표시 후 보고 |
| confidence 불확실 (히스토리 요약 등) | `confidence: medium` 으로 설정 |
| 10개 이상 페이지 수정 필요 | 오너 확인 후 진행 |
