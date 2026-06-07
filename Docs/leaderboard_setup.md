# 리더보드 서버 — 셋업 가이드

글로벌 Top100 리더보드는 **Cloudflare Worker + D1**로 동작합니다 (무료·무중단).

> ⚠️ **실제 URL·토큰은 git에 없습니다.** 공개 저장소라 토큰을 커밋하지 않습니다.
> 값은 `server/leaderboard-worker/SECRETS.local.txt`(로컬 전용, .gitignore 제외)에 있습니다.
> 새 PC에선 그 파일을 **수동 복사**(USB/비밀번호 관리자)하세요.

## 구조
| 계층 | 위치 |
|------|------|
| 클라이언트 서비스 | `Assets/Scripts/Manager/LeaderboardService.cs` (코루틴+UnityWebRequest, DDOL) |
| 설정 | `Assets/Resources/LeaderboardConfig.asset` (Resources 로드) |
| 서버 | `server/leaderboard-worker/` (wrangler.toml · schema.sql · src/index.js) |
| 담당 에이전트 | `.claude/agents/server.md` |

**기본값 `enableRemote=false`** → asset/서버 없이도 **로컬 전용(오프라인)으로 안전 동작**. 커밋된 asset은 비활성 상태라 새 클론도 그대로 빌드됨.

## 새 PC에서 원격 켜기 (30초)
1. `SECRETS.local.txt`를 새 PC로 복사.
2. Unity → `Assets/Resources/LeaderboardConfig.asset` Inspector:
   - **Enable Remote** ✅ / **Api Base Url** / **Write Token** ← 값은 `SECRETS.local.txt`
3. `Ctrl+S` 저장. 끝 — Play하면 서버 연동.

## 서버 운영
- **재배포**: `server/leaderboard-worker`에서 `wrangler login`(최초) → `wrangler deploy`
- **토큰 교체 / 초기화 / 점수 조회**: `SECRETS.local.txt` 하단 명령 참고
- **튜닝**(점수 상한 등): `wrangler.toml` `[vars]`(MAX_SCORE=150000, MAX_ROUNDS=12, KEEP_ROWS=200, CACHE_TTL=30) 수정 후 재배포

## REST 계약
- `GET /top?limit=100` → `{"entries":[{score,rounds,date,name,summary}, …]}` (score 내림차순)
- `POST /submit` (헤더 `X-Write-Token`) `{"entry":{…},"clientNonce":"<GUID>"}` → `{"ok":true,"rank":N,"duplicate":bool}`
- `OPTIONS *` → 204 (CORS preflight)

## 보안
- 서버: `score` cap(150000)·`rounds` cap(12)·`client_nonce` 멱등·bound params(SQLi 차단)·(선택) WAF 레이트리밋
- 토큰은 빌드 동봉이라 약함 → **서버측 검증이 본 방어**. 유출 시 토큰 교체로 즉시 차단.
- 진화 경로: HMAC 본문 서명 → Steam 티켓 검증(Worker→Steam Web API)

## 규모 / 비용
하루 1천 게임 = 무료 한도의 ~30배 여유. ~5만 게임/일 근접 시 Workers Paid $5/월. 엣지 캐시(TTL 30초)로 읽기 흡수.
