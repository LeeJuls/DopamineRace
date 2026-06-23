---
name: security-audit
description: DopamineRace 보안 풀 감사 — 6대 영역(크리덴셜·통신·저장·재화·.claude위생·빌드)을 병렬 스캔해 등급별 리포트 + P0~P3 우선순위 산출. 릴리스 전, .claude/·서버·지갑·리더보드 코드 변경 후, 주기 점검 시 사용. 외부 npm 미사용(읽기전용 자작).
---

# /security-audit — 보안 풀 감사

DopamineRace의 치팅 방어·시크릿·설정 위생을 한 번에 점검한다. `security` 에이전트의 6대 영역 체크리스트를 실행.

## 언제
- 릴리스 빌드 전 (게이트)
- `.claude/`·`server/`·`WalletManager`·`LeaderboardService`·`LeaderboardConfig` 변경 후
- 주기적 위생 점검

## 워크플로

1. **6대 영역 병렬 스캔** — 각 영역을 `security`(또는 Explore) 에이전트로 동시 디스패치:
   1. 크리덴셜 노출 — `Assets/`·config·`git ls-files`/history에 token/key/secret 하드코딩, `SECRETS.local.txt` gitignore + `LeaderboardConfig.asset` skip-worktree 상태
   2. 리더보드 통신 — `LeaderboardService` HMAC `X-Sig` 본문서명 유무 · 토큰 헤더 · Worker 타당성(rounds 상한)
   3. 저장 무결성 — PlayerPrefs/`leaderboard.json` 체크섬 유무
   4. 재화·배팅 — `WalletManager` SecureInt 적용 · `Reward()` 상한 · `BetAmountModal` 입력 clamp
   5. `.claude/` 위생 — `settings.local.json` allow 목록 임의실행 와일드카드(`powershell -Command:*`·`python:*`·`reg delete *`·`git push:*`) · 시크릿 분리 · 인젝션 surface
   6. 빌드 하드닝 — `ProjectSettings` scriptingBackend(IL2CPP 여부) · 스트리핑 · 디버그 가드(`#if UNITY_EDITOR`)

2. **수집·정렬** — 각 발견을 `파일:라인` + 등급(CRITICAL/HIGH/MEDIUM/LOW) + 한줄 + 권고로 표준화.

3. **리포트 산출**:
   - 영역별 등급 표
   - **P0~P3 우선순위** (CRITICAL → P0)
   - 합격 기준: **CRITICAL 0건**

## 원칙
- **외부 npm(AgentShield 등) 미사용** — 공급망 리스크 회피, 읽기전용 자작.
- 신뢰 경계: 클라가 보낸 모든 값은 위조 가능. "완전 차단" 아닌 "캐주얼 차단 + 리버서 지연".
- 시크릿 노출 발견 시 즉시 P0 — 로테이션 절차는 `SECRETS.local.txt` / SPEC-044 §4.

## 단일 출처
위협모델·방어 4레이어·체크리스트 상세: `Docs/specs/SPEC-044` / 위키 `시스템/보안_치팅방어`.
감사 자체는 `security` 에이전트에 위임 가능.
