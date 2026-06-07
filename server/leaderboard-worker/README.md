# DopamineRace 리더보드 — Cloudflare Worker + D1

글로벌 Top100 리더보드 백엔드. 무료·무중단(엣지). Unity 클라가 `GET /top`(표시) + `POST /submit`(저장).

## REST 계약
| 메서드 | 경로 | 헤더 | 본문 | 응답 |
|--------|------|------|------|------|
| GET | `/top?limit=100` | — | — | `200 {"entries":[{score,rounds,date,name,summary}, …]}` (score 내림차순) |
| POST | `/submit` | `X-Write-Token` | `{"entry":{…},"clientNonce":"<GUID>"}` | `200 {"ok":true,"rank":N,"duplicate":false}` |
| OPTIONS | `*` | — | — | `204` (CORS preflight) |

- 중복 `clientNonce` → `200 {"duplicate":true}` (절대 4xx — 재시도 멱등)
- `score`/`rounds` cap 초과·malformed → `400` · 토큰 없음 `401` / 불일치 `403`

## 배포 (오너 실행, ~5분)
> 사전: Node 18+(설치됨), Cloudflare 계정(생성됨). 이 폴더에서 실행.

```bash
# 0) wrangler 설치 + 로그인 (브라우저 인증창)
npm i -g wrangler
wrangler login

# 1) D1 생성 → 출력된 database_id 복사
wrangler d1 create dopamine-leaderboard-db
#    → wrangler.toml 의 REPLACE_WITH_D1_DATABASE_ID 교체

# 2) 스키마 적용 (원격 D1)
wrangler d1 execute dopamine-leaderboard-db --remote --file=./schema.sql

# 3) 쓰기 토큰 등록 (강한 랜덤값. 예: openssl rand -hex 24)
wrangler secret put WRITE_TOKEN

# 4) 배포 → 출력된 Worker URL 확보
wrangler deploy
#    → https://dopamine-leaderboard.<sub>.workers.dev
```

**회신 2개**: ① Worker URL(끝 슬래시 없이) ② WRITE_TOKEN 값 → `LeaderboardConfig.asset` 입력.

(선택) 대시보드 → Worker → Security/WAF → Rate Limiting: `/submit` 60 req/min/IP.

## env (wrangler.toml [vars] — 재배포로 조정)
| 키 | 기본 | 의미 |
|----|------|------|
| `MAX_SCORE` | 150000 | 점수 상한(7라운드 이론최대 69,930의 ~2.1배). 초과 거부 |
| `MAX_ROUNDS` | 12 | 라운드 상한(프로덕션 7+여유) |
| `KEEP_ROWS` | 200 | 물리 보관 행수(>100 안전마진). GET은 100 |
| `TOP_LIMIT` | 100 | GET 기본/최대 |
| `CACHE_TTL` | 30 | GET 엣지캐시 초(submit 시 무효화) |

## 검증 (curl) — `$URL`/`$TOK` 치환. PowerShell은 `curl.exe`
| TC | 명령 | 기대 |
|----|------|------|
| T1 첫 제출 | `curl.exe -s -X POST "$URL/submit" -H "X-Write-Token: $TOK" -H "Content-Type: application/json" -d '{"entry":{"score":500,"rounds":3,"date":"2026-06-07 12:00","name":"AAA","summary":"R1+5"},"clientNonce":"nonce-aaaa-0001"}'` | `200 {"ok":true,"rank":1,...}` |
| T2 높은 점수 | 위 + `score:900,name:"BBB",clientNonce:"nonce-bbbb-0002"` | `200 rank:1` |
| T3 정렬 | `curl.exe -s "$URL/top?limit=100"` | `entries[0].score=900, [1]=500` |
| T4 중복 nonce | T1 동일 nonce 재전송 | `200 duplicate:true`, GET 1행 불변 |
| T5 cap | `score:99999999` | `400 score_out_of_range` |
| T6 토큰 없음 | T1에서 헤더 제거 | `401 missing_token` |
| T7 토큰 오류 | `X-Write-Token: WRONG` | `403 bad_token` |
| T8 malformed | `-d '{not json'` | `400 bad_json` (5xx 아님) |
| T9 CORS preflight | `curl.exe -s -i -X OPTIONS "$URL/submit" -H "Origin: https://x.com" -H "Access-Control-Request-Method: POST" -H "Access-Control-Request-Headers: X-Write-Token"` | `204` + Allow-Headers X-Write-Token |
| T10 limit 클램프 | `"$URL/top?limit=99999"` / `?limit=-1` | 최대 100건, 크래시 없음 |
| T11 누락 필드 | `-d '{"clientNonce":"n-00000003"}'` | `400 missing_entry` |
| T12 GET CORS | `curl.exe -s -i "$URL/top"` | 헤더 `Access-Control-Allow-Origin: *` |

깨끗한 재테스트: `wrangler d1 execute dopamine-leaderboard-db --remote --command "DELETE FROM leaderboard"`

## 보안 (MVP)
토큰은 빌드 동봉이라 약함 → 서버측 cap·nonce 멱등·bound params·(선택)WAF로 피해 한정.
진화: HMAC 본문서명(빌드별 시크릿) → Steam 티켓 검증(Worker→Steam Web API).

## 로컬 문법 검사
```bash
npm run check    # node --check src/index.js
```
