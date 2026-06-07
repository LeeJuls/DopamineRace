// DopamineRace 리더보드 Worker — 의존성 0
// 계약:
//   GET  /top?limit=100        -> 200 { entries: [{score,rounds,date,name,summary}, ...] }  (score DESC)
//   POST /submit (X-Write-Token) { entry:{...}, clientNonce } -> 200 { ok:true, rank, duplicate }
//   OPTIONS *                  -> 204 (CORS preflight)

export default {
  async fetch(request, env, ctx) {
    const url = new URL(request.url);
    const cors = corsHeaders(env);

    // --- CORS preflight ---
    if (request.method === "OPTIONS") {
      return new Response(null, { status: 204, headers: cors });
    }

    try {
      if (request.method === "GET" && url.pathname === "/top") {
        return await handleTop(request, env, ctx, cors);
      }
      if (request.method === "POST" && url.pathname === "/submit") {
        return await handleSubmit(request, env, ctx, cors);
      }
      if (request.method === "GET" && url.pathname === "/") {
        return json({ ok: true, service: "dopamine-leaderboard" }, 200, cors);
      }
      return json({ ok: false, error: "not_found" }, 404, cors);
    } catch (err) {
      // 어떤 예외도 5xx로 새지 않게 — 입력/DB 오류는 위에서 4xx로 처리
      return json({ ok: false, error: "internal" }, 500, cors);
    }
  },
};

/* ----------------------- GET /top ----------------------- */
async function handleTop(request, env, ctx, cors) {
  const max = intOr(env.TOP_LIMIT, 100);
  let limit = parseInt(new URL(request.url).searchParams.get("limit"), 10);
  if (!Number.isFinite(limit) || limit < 1) limit = max;
  if (limit > max) limit = max;

  // --- 엣지 캐시 조회 ---
  const ttl = intOr(env.CACHE_TTL, 30);
  const cache = caches.default;
  const cacheKey = new Request(new URL("/top?limit=" + limit, request.url).toString(), { method: "GET" });
  if (ttl > 0) {
    const hit = await cache.match(cacheKey);
    if (hit) return withCors(hit, cors);
  }

  const rs = await env.DB
    .prepare("SELECT score, rounds, date, name, summary FROM leaderboard ORDER BY score DESC, id ASC LIMIT ?1")
    .bind(limit)
    .all();

  const entries = (rs.results || []).map((r) => ({
    score: r.score | 0,
    rounds: r.rounds | 0,
    date: r.date || "",
    name: r.name || "",
    summary: r.summary || "",
  }));

  const body = JSON.stringify({ entries });
  const headers = { ...cors, "Content-Type": "application/json", "Cache-Control": "public, max-age=" + ttl };
  const resp = new Response(body, { status: 200, headers });

  if (ttl > 0) ctx.waitUntil(cache.put(cacheKey, resp.clone()));
  return resp;
}

/* ----------------------- POST /submit ----------------------- */
async function handleSubmit(request, env, ctx, cors) {
  // 1) 토큰 (상수시간 비교)
  const token = request.headers.get("X-Write-Token") || "";
  const expected = env.WRITE_TOKEN || "";
  if (!expected) return json({ ok: false, error: "server_misconfig" }, 500, cors);
  if (!token) return json({ ok: false, error: "missing_token" }, 401, cors);
  if (!timingSafeEqual(token, expected)) return json({ ok: false, error: "bad_token" }, 403, cors);

  // 2) 본문 파싱 (malformed -> 400, never 5xx)
  let payload;
  try {
    payload = await request.json();
  } catch {
    return json({ ok: false, error: "bad_json" }, 400, cors);
  }
  const e = payload && payload.entry;
  const nonce = payload && payload.clientNonce;
  if (!e || typeof e !== "object") return json({ ok: false, error: "missing_entry" }, 400, cors);
  if (typeof nonce !== "string" || nonce.length < 8 || nonce.length > 64)
    return json({ ok: false, error: "bad_nonce" }, 400, cors);

  // 3) 검증 + 살균
  const MAX_SCORE = intOr(env.MAX_SCORE, 150000);
  const MAX_ROUNDS = intOr(env.MAX_ROUNDS, 12);

  if (typeof e.score !== "number" || !Number.isFinite(e.score)) return json({ ok: false, error: "bad_score" }, 400, cors);
  const score = Math.trunc(e.score);
  if (score < 0 || score > MAX_SCORE) return json({ ok: false, error: "score_out_of_range" }, 400, cors);

  let rounds = Number.isFinite(e.rounds) ? Math.trunc(e.rounds) : 0;
  if (rounds < 0) rounds = 0;
  if (rounds > MAX_ROUNDS) return json({ ok: false, error: "rounds_out_of_range" }, 400, cors);

  const name = sanitize(e.name, 16);        // 아케이드 이니셜이라 보통 3, 관대히 16
  const summary = sanitize(e.summary, 256);
  const date = isValidDate(e.date) ? e.date : "";
  const now = Date.now();

  // 4) 멱등 INSERT (첫 쓰기 승) — D1은 직렬 처리
  const ins = await env.DB
    .prepare(
      `INSERT OR IGNORE INTO leaderboard (score, rounds, date, name, summary, client_nonce, inserted_at)
       VALUES (?1, ?2, ?3, ?4, ?5, ?6, ?7)`
    )
    .bind(score, rounds, date, name, summary, nonce, now)
    .run();

  const duplicate = !!(ins.meta && ins.meta.changes === 0);

  // 트림: KEEP_ROWS 초과분만 삭제 (보통 no-op) — 백그라운드
  const keep = intOr(env.KEEP_ROWS, 200);
  ctx.waitUntil(
    env.DB.prepare(
      `DELETE FROM leaderboard WHERE id NOT IN
         (SELECT id FROM leaderboard ORDER BY score DESC, id ASC LIMIT ?1)`
    ).bind(keep).run()
  );

  // 5) rank (자신보다 엄격히 높은 점수 수 + 1 — 동점 미자격, Unity Qualifies와 동일)
  const rankRow = await env.DB
    .prepare("SELECT COUNT(*) AS c FROM leaderboard WHERE score > ?1")
    .bind(score)
    .first();
  const rank = ((rankRow && rankRow.c) | 0) + 1;

  // 6) 캐시 무효화 (다음 GET fresh)
  const ttl = intOr(env.CACHE_TTL, 30);
  if (ttl > 0) {
    const cache = caches.default;
    const max = intOr(env.TOP_LIMIT, 100);
    ctx.waitUntil(cache.delete(new Request(new URL("/top?limit=" + max, request.url).toString(), { method: "GET" })));
  }

  return json({ ok: true, rank, duplicate }, 200, cors);
}

/* ----------------------- helpers ----------------------- */
function corsHeaders(env) {
  const origin = env.CORS_ORIGIN && env.CORS_ORIGIN.length ? env.CORS_ORIGIN : "*";
  return {
    "Access-Control-Allow-Origin": origin,
    "Access-Control-Allow-Methods": "GET, POST, OPTIONS",
    "Access-Control-Allow-Headers": "Content-Type, X-Write-Token",
    "Access-Control-Max-Age": "86400",
    "Vary": "Origin",
  };
}
function withCors(resp, cors) {
  const h = new Headers(resp.headers);
  for (const k in cors) h.set(k, cors[k]);
  return new Response(resp.body, { status: resp.status, headers: h });
}
function json(obj, status, cors) {
  return new Response(JSON.stringify(obj), {
    status,
    headers: { ...cors, "Content-Type": "application/json" },
  });
}
function intOr(v, d) {
  const n = parseInt(v, 10);
  return Number.isFinite(n) ? n : d;
}
function sanitize(s, max) {
  if (typeof s !== "string") return "";
  // 제어문자(0x00-0x1F, 0x7F) 제거 + 트림 + 길이 캡
  return s.replace(/[\x00-\x1F\x7F]/g, "").trim().slice(0, max);
}
function isValidDate(s) {
  return typeof s === "string" && /^\d{4}-\d{2}-\d{2} \d{2}:\d{2}$/.test(s);
}
// 상수시간 문자열 비교 (타이밍 공격 완화)
function timingSafeEqual(a, b) {
  if (a.length !== b.length) return false;
  let r = 0;
  for (let i = 0; i < a.length; i++) r |= a.charCodeAt(i) ^ b.charCodeAt(i);
  return r === 0;
}
