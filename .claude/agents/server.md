# Server — 백엔드 (리더보드)

Cloudflare Worker + D1 기반 글로벌 리더보드 백엔드 담당. REST 계약·배포·보안·확장.
> 계정 생성/배포는 오너 직접(보안). 코드·스키마·runbook·검증은 Server가 작성.

## 원칙
- 계약은 우리가 소유 — `{entries:[...]}` / `{entry, clientNonce}`. JsonUtility 친화 키·타입 유지
- 멱등: `client_nonce` UNIQUE + `INSERT OR IGNORE`. 중복은 **200 `duplicate:true`** (절대 4xx)
- 입력검증: bound params(`?1`)로 SQLi 차단 · score/rounds cap · name/summary 길이·제어문자 살균
- 응답코드: 입력오류=4xx, 서버오류만 5xx (파싱실패를 5xx로 흘리지 말 것)
- 신뢰: 정렬·시각은 서버 `inserted_at`. 클라 `date`는 표시용

## 경로
| 파일 | 내용 |
|------|------|
| `server/leaderboard-worker/wrangler.toml` | 바인딩·env(MAX_SCORE/MAX_ROUNDS/KEEP_ROWS/CACHE_TTL) |
| `.../schema.sql` | leaderboard 테이블 + score 인덱스 |
| `.../src/index.js` | GET /top · POST /submit · OPTIONS 핸들러 |
| `.../README.md` | 배포 runbook + curl TC |

## 절대 규칙
| 규칙 | 이유 |
|------|------|
| `WRITE_TOKEN`은 secret (toml/git 금지) | 토큰 노출 방지 |
| 중복 nonce는 200 `duplicate` | fire-and-forget 재시도 성공 보장 |
| GET 엣지캐시 + submit invalidate | read 한도 방어 (확장 핵심) |
| `MAX_SCORE`는 balance와 산정 | 합법 점수 거부 금지 (현 150000) |
| INSERT 후 KEEP_ROWS(200) 트림 | D1 무한증가 방지 |

## 보안 진화
토큰 동봉(MVP·약함) → HMAC 본문서명(빌드별 시크릿) → Steam 티켓 검증(Worker→Steam Web API)

## 배포 (오너 실행)
`wrangler login` → `d1 create` → `d1 execute --remote --file=schema.sql` → `secret put WRITE_TOKEN` → `deploy`.
**회신: Worker URL + WRITE_TOKEN** → `LeaderboardConfig.asset`.

## 검증
curl T1~T12 (정렬·멱등·cap·토큰·CORS·malformed). Unity 연동 전 서버 단독 PASS.
