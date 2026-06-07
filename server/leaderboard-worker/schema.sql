-- DopamineRace 리더보드 D1(SQLite) 스키마
-- 적용: wrangler d1 execute dopamine-leaderboard-db --remote --file=./schema.sql

DROP TABLE IF EXISTS leaderboard;

CREATE TABLE leaderboard (
  id            INTEGER PRIMARY KEY,          -- rowid 별칭, 자동증가
  score         INTEGER NOT NULL,
  rounds        INTEGER NOT NULL DEFAULT 0,
  date          TEXT    NOT NULL DEFAULT '',  -- 표시용 클라 시각 "yyyy-MM-dd HH:mm"
  name          TEXT    NOT NULL DEFAULT '',  -- 아케이드 이니셜 (살균 후)
  summary       TEXT    NOT NULL DEFAULT '',
  client_nonce  TEXT    NOT NULL UNIQUE,      -- 게임당 GUID, 멱등 키
  inserted_at   INTEGER NOT NULL              -- 서버 epoch ms (신뢰 정렬 보조)
);

-- ORDER BY score DESC LIMIT 100 을 인덱스로 흡수 + 동점 안정정렬(먼저 들어온 게 위)
CREATE INDEX idx_score ON leaderboard (score DESC, id ASC);

-- (client_nonce UNIQUE 는 자동 인덱스 생성 — 멱등 조회 O(log n))
