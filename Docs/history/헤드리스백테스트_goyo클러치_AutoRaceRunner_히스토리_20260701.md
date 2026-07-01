# 헤드리스 백테스트 툴 + goyo 운빨 클러치 + AutoRaceRunner 프리징 수정 히스토리

> 2026-07-01 · 고속 헤드리스 밸런스 백테스트 툴 신설 → goyo 전거리 지배 진단·클러치 재설계(80%→56%) → AutoRaceRunner 간헐 프리징 근본수정. 커밋 `9cac38c`·`bcd193d`

## 작업 요약

"자동 레이스 200회로 (a) 특정 캐릭터가 타입/거리 무관 전 트랙 지배하는지 (b) 5~6바퀴에서 HP 전멸하는지" 검증 요구에서 출발. 실엔진 `AutoRaceRunner`(Play)는 **에디터 백그라운드 프레임 스로틀 + 랭킹모달 멈춤**으로 대량 자동화 불가 → **헤드리스 동기 시뮬 툴** 신설로 우회. 이 툴로 **goyo 전거리 지배(overall 80%)** 확정 → 원인 진단 → **운빨 클러치**로 재설계(80%→56% 고분산). 이후 AutoRaceRunner의 **간헐 프리징**(자동배팅 선택 소멸 race) 근본 수정 + 헤드리스 버튼 이식.

## 핵심 결정 (오너 + 에이전트 합의)

| 항목 | 결정 | 근거 |
|------|------|------|
| 대량 검증 방식 | 헤드리스 동기 루프(`RunSimulationCore` 재사용) | AutoRaceRunner Play는 백그라운드 스로틀·모달로 자동화 불가 |
| 미러 신뢰범위 | 상대순위·지배·전멸 경향 O / **절대 win% 정밀튜닝 X** | `SimTickV4`는 `RacerController_V4` 근사 |
| goyo 지배 원인 | 로스터 유일 `P_Always:SpeedBonus:1.06`(무조건 상시 +6%) | 동일스탯 goldenday(패시브 X)=42% vs goyo 80% |
| 너프 방향 | 플랫 감소 X → **운빨 클러치**(터지면 압도/안터지면 평범) | 오너 선택, 도파민 테마 부합 |
| 클러치 수치 | chance **0.40** / bonus **×1.20** (진행도 50% 단발 도박) | 튜닝 격자 측정 후 오너 선택 → overall 56%, 발동 75%/미발동 37% |
| (b) 6바퀴 전멸 | **전멸 아님** — 10바퀴도 dnf 0, avgHP ~20% 수렴 | drain=진행도기반(랩수 무관). 6바퀴+ 제작 가능 |
| AutoRaceRunner 방향 | **헤드리스 주력** + Play는 견고화, 백그라운드 A는 best-effort | `QueuePlayerLoopUpdate` in `OnEditorUpdate`는 자기모순(안 불리는 콜백서 킥) |
| 프리징 원인 | `DelayedBettingUI`(1.5s)→`ResetBetting`→`SelectBetType`이 자동배팅 선택 소멸 → `StartRace` `!IsComplete` 가드 데드락 | Round2+ 간헐(1.5s vs LineUp~1.2s 지터), 포커스 무관 구조적 데드락 |
| 프리징 수정 | `StartRace` 직전 `EnsureAutoBet` 선택 재확정(+이중경쟁 재시도) | 게임로직 무변경, 에디터 1파일 |

## 구현 (산출물)

| 산출물 | 내용 |
|--------|------|
| `Editor/RaceBacktestWindow.cs` | 헤드리스 진입점 `RunHeadless`/`RunLapSweepHeadless`/`RunClutchTuningSweep` + 메뉴 `DopamineRace/밸런스 스윕`. 완주HP·dnf 집계, 클러치 튜닝훅(chance/bonus 오버라이드), `Docs/logs/backtest_sweep_*.md` 저장. 동기 루프 → 백그라운드 OK·재현성(`InitState(seed+race)`) |
| `Data/PassiveSkillData.cs` | `PassiveTriggerType.LuckClutch` + `CLUTCH_SPEED_MAX(1.40)` |
| `Data/GameSettingsV4.cs/.asset` | `v4_clutchGateProgress(0.5)`·`v4_clutchChance(0.4)` |
| `Racer/RacerController_V4.cs` | `UpdateV4LuckClutch`(gate 도달 시 1회 roll → 결승까지 latch) + CalcSpeedV4 적용. ↔ 미러 `SimUpdateV4LuckClutch` |
| `SkillDB_V4.csv`·`StringTable.csv`·`CharacterDB_V4.csv` | 클러치 스킬 `skill.p.luckclutch.spd.1_20`(8언어) + goyo 패시브 스왑 |
| `Editor/AutoRaceRunnerWindow.cs` | Phase0 헤드리스 반복 버튼 / Phase1 GameOver 안전중단·스톨경고 UX(force-complete X) / Phase2 best-effort 킥 / **프리징 수정 `EnsureAutoBet`** |

## 검증

- 컴파일 0 · StringTable Validate 누락 0.
- 헤드리스: 4000race **5.5초** 완주 · 재현성(동일 seed 동일결과) · 스톨0·dnf0 · charId 32 병합.
- 클러치 실증: goyo **80%→56%**, 발동시 75% vs 미발동 37%(고분산), 타입×거리 정상(도주 단거리·선입/추입 장거리).
- 프리징: `StartRace` 유일 가드 `!IsComplete` 확인 → `EnsureAutoBet`이 보장. **오너 Play(1x·2x) 멈춤0 완주 확인.**
- ⚠️ 미러 한계: 클러치 절대 win%는 실엔진 재확인 권장.

## 에이전트 협업

client(툴개발·미러·프리징 진단)·qa(단계TC·프리징 확증)·balance(goyo 원인·너프안)·Explore(코드매핑). plan 다회 업그레이드(헤드리스 4단계 → 클러치 → AutoRaceRunner Phase0-3 → 프리징 근본수정).

## 잔여 / 후속

- 클러치 절대 win% 실엔진 소표본 교차검증(미러 근사 보정).
- AutoRaceRunner **백그라운드 완주는 구조상 불가** — 대량 반복은 헤드리스 버튼 사용.
- `char_scenario` 컬럼(스토리 기능): 클러치 테스트 정리 중 `git checkout`으로 실수 삭제 → 32행 결정론적 복원, 커밋 9cac38c 포함.
- `Character_2.mp4` 등 라이브포트레이트 영상: WMF "color primaries 0" 경고(무해, 재인코딩은 후속).
- 위키 반영(`/wiki-ingest`).

참조: `memory/headless-backtest-tool.md` · `memory/balance-goyo-passive-fix.md`
