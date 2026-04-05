# 히스토리: 배팅 UI 개선 기획 + SPEC-007 작성

- **날짜**: 2026-02-27
- **세션 종류**: 환경 셋업 + 기획 논의 + 명세서 작성 (구현 미착수)
- **참여**: 오너(LeeJuls) + leader + client + qa + balance
- **결과물**:
  - `CLAUDE.md` (프로젝트 루트)
  - `.claude/agents/` — leader / balance / client / qa 에이전트 4종
  - `Docs/specs/20260227_SPEC-007_배팅UI개선_명세서.md`
  - 이 문서

---

## 1. 세션 배경

### 1-1. PC 이전 + 신규 세팅
오너가 기존 작업을 새 PC로 이전하면서 작업 환경을 새로 세팅.
기존에 Git 레포가 중첩 구조로 잘못 연결되어 있었음.

### 1-2. UI 디자이너 합류 예고
당일 오후부터 UI 디자이너가 합류 예정.
배팅 화면 UI 개선 기획서를 미리 확정하고 명세서를 작성하는 것이 이 세션의 핵심 목표.

---

## 2. 작업 과정

### 2-1. Git 중첩 레포 문제 해결

**문제**
```
D:\Unity_Project\DopamineRace        ← 메인 레포 (outer)
D:\Unity_Project\DopamineRace\DopamineRace  ← 실수로 생긴 내부 clone (inner)
```
GitHub Desktop이 inner 폴더를 pull 대상으로 사용 → outer가 구버전 상태.

**해결 과정**
1. outer에서 `git pull --ff-only origin main` → 3c80881 → 02ff6bd (fast-forward)
2. inner의 `.git` 폴더 제거 (`rm -rf`)
3. `.gitignore`에 `/DopamineRace/` 추가 → inner 재생성 방지
4. 커밋: `4b3eafa` — "[C] fix: 중첩 Git 레포 제거 및 .gitignore 업데이트"

**GitHub Desktop 재연결 방법**: File → Add Local Repository → outer 폴더 선택

---

### 2-2. 프로젝트 전체 파악

`Docs/MEMORY.md`, `Docs/BALANCE_GUIDE.md`, `Docs/20260226_UI디자이너_온보딩가이드.md`,
`Docs/specs/` 내 SPEC-001~006 전체 리뷰 완료.

**현재 프로젝트 상태 파악 내용**
- Unity 6000.3.7f1, Library 캐시 존재 → 바로 실행 가능
- 레이스 시스템: HP + CP 이중 자원 구조 (SPEC-006 완료)
- UI: 코드 자동생성 프리팹 방식 (`DopamineRace > Create Betting UI Prefabs`)
- 다국어: StringTable.csv 7개 언어

**GitHub Desktop 4개 파일 변경 처리**
- BettingPanel.prefab, CharacterItem.prefab, GameSettings.asset, ShaderGraphSettings.asset
- Unity 재직렬화 아티팩트(정상) → `git restore .` 로 discard 권장

---

### 2-3. 4-에이전트 시스템 구축

**오더 요약**: 코드방에 4명의 전문 에이전트를 구축
- `leader`: PM / 기획 / 문서 / 업무 배분
- `balance`: 밸런스 수치 설계 / 백테스트
- `client`: Unity 클라이언트 개발
- `qa`: QA 계획 / 검증 / 버그 예측

**생성 파일**
- `CLAUDE.md` (프로젝트 루트): 공용 규칙 + 핵심 목표 + 아키텍처 요약 + 에이전트 목록
- `.claude/agents/leader.md`
- `.claude/agents/balance.md`
- `.claude/agents/client.md`
- `.claude/agents/qa.md`

**gitignore 트러블슈팅**
- `.claude/` 패턴이 `.claude/agents/`도 차단 → `!.claude/agents/` 예외 무력화됨
- 해결: `.claude/*` + `!.claude/agents/` 패턴으로 변경 + `git add -f`로 강제 추가
- 커밋: `263854d`

---

### 2-4. UI 기획서 분석 (구글독스)

오너가 제공한 Google Docs UI 기획서 + 3장의 이미지를 분석.

**분석 내용** (4-에이전트 시각으로 각각 피드백)
- **leader**: 정보 위계 재설계, 트랙 패널 토글 필요성, 거리별 승률 형식 확인 필요
- **client**: TrackType E_Snow/E_Rain 추가, CharacterRecord 신규 필드, Show() 시그니처 변경 예고
- **qa**: SAVE_VERSION 범프 필요성, XCharts Rich Text 불확실성, winOdds 필드 존재 확인 필요
- **balance**: 배당률 배지 단승 고정 이유 타당, 1등 확률 표시가 실제 배팅 전략에 영향

---

### 2-5. 기획 Q&A 확정

오너와 다음 내용을 질의응답으로 확정:

| 질문 | 확정 내용 |
|------|---------|
| 1st.{0}% 기준 | 거리별 누적 (1등 횟수 / 거리별 전체 출전 횟수) |
| 스탯 강조 기준 | TrackDB bonus 필드 > 0인 스탯 자동 노란색 |
| 패널 토글 상태 | `static` 변수로 라운드 간 유지 |
| 트랙 툴팁 | 게임 시작 시 항상 표시 |
| CharacterRecord 저장 | 누적 영구 저장 (recentRaceEntries는 최근 30회 한계 → 별도 카운터 필요) |
| 배당률 배지 대상 | 모든 12캐릭터 항상 표시 |
| 배당률 기준 | 단승(Win) 기준 고정 |
| My Point / Get Point | 배팅 화면 전용 (레이스 시작 시 숨김) |
| E_Snow / E_Rain | TrackType 열거형 추가 + StringTable 1:1 매핑 |
| 거리 분류 임계값 | 단거리 1~2랩 / 중거리 3~4랩 / 장거리 5랩+ |

---

### 2-6. SPEC-007 작성

**1차 작성** → 오너 요청으로 기존 SPEC-005/006 양식으로 전면 재작성.

재작성 시 추가된 내용:
- 각 설계 결정의 **이유(근거)** 섹션
- 실제 파일 경로 검증 (`Assets/Scripts/Racer/TrackInfo.cs` 위치 확인 등)
- 기존 StringTable 키 충돌 분석 (`str.ui.track.short` 이미 존재 확인)
- **알려진 위험 포인트** 표 (XCharts Rich Text, winOdds 필드, CurrentBet API)
- ASCII 레이아웃 다이어그램

**최종 SPEC-007 구조**
```
Phase 1-A: TrackType 확장 (E_Snow/E_Rain + GetTrackTypeKey)
Phase 1-B: CharacterRecord 거리별 집계 필드 + SAVE_VERSION 3
Phase 2:   트랙 정보 패널 재설계 (TrackDescLabel + Toggle)
Phase 3:   My Point / Get Point UI
Phase 4:   배당률 배지 (OddsLabel, 항상 표시)
Phase 5:   캐릭터 팝업 (1st.N% + 스탯 강조)
Phase 6:   통합 QA
```

---

## 3. 산출물 목록

| 파일 | 내용 | 커밋 |
|------|------|------|
| `CLAUDE.md` | 프로젝트 공용 규칙 + 에이전트 목록 | `263854d` |
| `.claude/agents/leader.md` | PM/기획 에이전트 | `263854d` |
| `.claude/agents/balance.md` | 밸런스 에이전트 | `263854d` |
| `.claude/agents/client.md` | Unity 개발 에이전트 | `263854d` |
| `.claude/agents/qa.md` | QA 에이전트 | `263854d` |
| `.gitignore` | `/DopamineRace/` + `.claude/agents/` 예외 | `263854d` |
| `Docs/specs/20260227_SPEC-007_배팅UI개선_명세서.md` | 배팅UI 개선 명세서 (6-Phase) | 미커밋 |
| `Docs/history/20260227_배팅UI개선기획_SPEC007_히스토리.md` | 이 문서 | 미커밋 |

---

## 4. 미결 사항 (구현 전 확인 필요)

| 항목 | 내용 | Phase |
|------|------|-------|
| `oddsInfo.winOdds` 필드 | `PopularityInfo` 클래스에 해당 필드 존재 여부 확인 | Phase 4 전 |
| `GameManager.CurrentBet` | `CurrentBet`, `HasSelection()`, `CurrentOdds` API 존재 여부 | Phase 3 전 |
| XCharts Rich Text | 인디케이터 라벨에 `<color>` 태그 적용 가능 여부 | Phase 5 |
| betAmount 기준 | 배팅 금액이 고정(100)인지 가변인지 확인 | Phase 3 |

---

## 5. 다음 단계

1. **오너 컨펌**: SPEC-007 검토 → 이상 없으면 개발 시작 승인
2. **Phase 1-A 개발 시작**: `client` 에이전트가 TrackType 확장부터
3. **UI 디자이너 합류**: 온보딩 가이드(`Docs/20260226_UI디자이너_온보딩가이드.md`) 공유

---

## 6. 교훈 및 메모

- **`.gitignore` 중첩 패턴 주의**: 부모 디렉토리를 와일드카드(`dir/*`)로 차단 시 `!dir/sub/` 예외가 작동함. 반면 `dir/` 패턴 차단 시 예외 무력화.
- **기획 Q&A 선행의 중요성**: 개발 전 모호한 스펙을 확정하지 않으면 구현 중간에 뒤집히는 비용 발생. 이번 세션에서 10개 이상의 모호점을 사전 확정하여 SPEC 품질 향상.
- **CharacterRecord 슬라이딩 윈도우 한계**: `recentRaceEntries` MAX=30은 단기 UI 표시용. 장기 누적 통계는 별도 카운터가 반드시 필요함.
