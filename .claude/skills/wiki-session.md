---
description: 세션 시작 루틴. 위키 3개 파일을 읽고 현재 프로젝트 상태를 파악한 뒤 오늘 할 작업을 준비합니다.
---

# wiki-session

## 사용법

```
/wiki-session
/wiki-session 오늘 spec041 배팅 개선 작업 예정
```

---

## 실행 절차

### Step 1 — 위키 상태 로딩

다음 3개 파일을 **이 순서대로** 읽는다:

1. `D:/Project/Dopamine/DopamineProject/SCHEMA.md` — 규칙 파악
2. `D:/Project/Dopamine/DopamineProject/Index.md` — 존재 페이지 목록 확인
3. `D:/Project/Dopamine/DopamineProject/log.md` 최근 20줄 — 직전 세션 작업 파악

### Step 2 — 프로젝트 Git 상태 확인

```bash
git log --oneline -5
git status
```

미완료 커밋이나 수정 중인 파일이 있으면 오너에게 알린다.

### Step 3 — 현황 요약 보고

다음 형식으로 간결하게 보고:

```
📋 세션 시작 요약 (YYYY-MM-DD)

[위키 최근 작업]
- 마지막 ingest: spec040 (2026-06-20)
- 미완료: SPEC028 Phase 4~5 (UGS 대기)

[Git 상태]
- 브랜치: main
- 미커밋 파일: Assets/Scripts/Data/GameSettings.cs 외 1개

[오늘 예정 작업]
- (인자로 전달된 내용 또는 "오너가 알려주세요")
```

### Step 4 — 작업 준비

- 오늘 작업이 특정 시스템과 관련되면 해당 위키 페이지를 미리 읽어둔다
- 작업 완료 후 `/wiki-ingest`로 위키 반영할 것을 기억한다
