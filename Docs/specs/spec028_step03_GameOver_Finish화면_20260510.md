# SPEC-028 step03 — Phase 3: GAME OVER 화면 + Finish 화면 (메인/부수 분리)

**날짜**: 2026-05-10
**상태**: 작성 (구현 대기)
**선행**: spec028_step01 (Phase 1) + spec028_step02 (Phase 2 — 헤더·모달)
**후속**: spec028_step04
**마스터**: spec028 (개요)

## 빠른 인덱스 (에이전트별 읽기 가이드)

| 에이전트 | 우선 읽기 섹션 | 산출물 |
|---|---|---|
| **leader** | 전체 + Phase 내 협업 시퀀스(끝) + UC4 메인/부수 분리 강조 | Phase 3 브리프, Finish 분리 검토, 산출물 검수 |
| **balance** | §QA TC-C2-03·C2-04 (라운드별 데이터 정확성) | 라운드 결과 데이터 일관성 검증 |
| **client** | §설계(클래스 다이어그램·와이어프레임), §스프린트 분해(8 step), §수정 파일 목록 | GameOverPanelPrefab 신규 + Finish 메인/부수 분리 |
| **qa** | §QA 체크리스트(12 TC), §게이트 통과 기준 | TC 12건 실행 + 두 분기 시나리오 검증 |

> 마스터 SPEC §A~F **공통 필수 숙지**.

## 배경

게임 끝 분기 2개를 시각화. **GAME OVER**(파산, 랭킹 미등록)와 **Finish**(완주, 랭킹 등록 가능)는 완전히 다른 결과 화면이어야 함. Finish 화면은 오너 인사이트 따라 **메인(적중·획득 스톤)** vs **부수(베팅 상세)** 영역을 분리해 도파민 핵심 정보를 강조.

## 요구사항

- **R3.1** `GameState.GameOver` 진입 시 자동으로 GAME OVER 패널 표시 (어두운 백드롭 + 모달).
- **R3.2** GAME OVER 화면 정보: 헤드라인, 도달 라운드 ("X / N 라운드에서 종료"), 랭킹 미등록 안내, [다시 도전] 버튼.
- **R3.3** [다시 도전] 클릭 → 새 게임 시작 (`WalletManager.ResetForNewGame()` + 씬 재로드).
- **R3.4** Finish 화면 메인 영역(가장 큰 글씨, 강조 색): "획득한 도파민 스톤: N💎" + 라운드별 ✓/✗ + 획득 스톤만.
- **R3.5** Finish 화면 부수 영역(작은 글씨, **기본 접힘**, 펼치기 토글): 베팅 타입·베팅액·배당·획득 젤리·최종 보유 젤리.
- **R3.6** Finish 화면 진입 시 메인 영역 페이드인 애니메이션 (드라마틱 강조).
- **R3.7** Finish 화면에 [랭킹 등록] 버튼 (Phase 4 컷오프 진입 시만 활성) / [다시 도전] / [글로벌 랭킹 보기] (Phase 5).
- **R3.8** GAME OVER 화면 메시지 톤은 **직설적 유지** (오너 결정, 추후 String 수정 가능).
- **R3.9** 모든 신규 텍스트는 StringTable UID 키로 등록 (7언어).

## 설계

### 클래스 다이어그램

```
[SceneBootstrapper.GameOver : partial class]
  - GameObject _gameOverPanel
  - TMP_Text _headlineText
  - TMP_Text _progressText        // "{0} / {1} 라운드에서 종료"
  - TMP_Text _noRankNoticeText
  - Button _btnRestart
  + void ShowGameOverPanel()      // GameState 전이 시 호출
  + void HideGameOverPanel()
  - void OnRestart()              // ResetForNewGame + SceneManager.LoadScene

[SceneBootstrapper.Finish (수정)]
  - GameObject _finishPanel        // 기존
  - TMP_Text _stoneTotalText       // 메인 영역 — 신규
  - GameObject _mainResultsArea    // 메인 영역 컨테이너 — 신규
  - GameObject _detailResultsArea  // 부수 영역 (접힘) — 신규
  - Button _detailToggleButton     // ▼/▲ 토글
  - Transform _roundSimpleList     // 메인: ✓/✗ 라운드 결과 리스트
  - Transform _roundDetailList     // 부수: 베팅 상세 리스트
  - TMP_Text _finalJellyText       // "최종 보유: 🟦 N"
  - Button _btnSubmitRanking       // Phase 4에서 활성화
  - Button _btnRestart, _btnViewGlobalRanking  // Phase 5에서 활성화
  + void ShowFinishPanel()         // 메인+부수 영역 채움 + 페이드인
  - void OnDetailToggle()          // 부수 영역 펼침/접힘
  - void RefreshMainArea()         // R1: ✓ 적중(+25💎) ...
  - void RefreshDetailArea()       // R1 단승 25🟦 → 1.7x → +18🟦 ...

[GameOverPanelPrefab : 신규 프리팹]
  Canvas (Sort Order 100, 어두운 백드롭)
  └─ Modal (중앙 정렬)
      ├─ Headline ("💔 GAME OVER")
      ├─ DescText ("도파민 젤리가 모두 소진됐습니다")
      ├─ ProgressText ("{0} / {1} 라운드에서 종료")
      ├─ NoRankText ("랭킹에 등록되지 않았습니다")
      └─ RestartButton ("다시 도전")

[FinishPanelPrefab (확장)]
  기존 프리팹 + 메인/부수 분리 레이아웃
```

### 데이터 흐름

```
[GameManager.CalcScore]
   ↓
   ├─ Wallet.Jelly == 0 → ChangeState(GameOver)
   │     ↓
   │  [SceneBootstrapper.GameOver.ShowGameOverPanel()]
   │     ↓
   │  유저 [다시 도전] → ResetForNewGame + 씬 재로드
   │
   └─ 마지막 라운드 완주 → ChangeState(Finish)
         ↓
      [SceneBootstrapper.Finish.ShowFinishPanel()]
         ↓
      메인 영역 페이드인 + ScoreManager에서 라운드별 결과 가져와 채움
         ↓
      Phase 4 흐름 (cutoff 조회 → 닉네임 모달 등) ─→ spec028_step04
```

### 와이어프레임

#### GAME OVER 화면

```
                [어두운 반투명 백드롭]
        ┌──────────────────────────────────────┐
        │                                      │
        │           💔  GAME OVER             │
        │                                      │
        │   도파민 젤리가 모두 소진됐습니다     │
        │                                      │
        │      3 / 4 라운드에서 종료           │
        │                                      │
        │      랭킹에 등록되지 않았습니다       │
        │                                      │
        │            [ 다시 도전 ]             │
        │                                      │
        └──────────────────────────────────────┘
```

#### Finish 화면 (메인/부수 분리)

```
        ┌────────────────────────────────────────────────────┐
        │              🏁 게임 종료                            │
        │                                                    │
        │  ╔═════════════════════════════════════════════╗  │
        │  ║   [메인 영역 — 가장 큰 글씨, 강조 색]        ║  │
        │  ║                                              ║  │
        │  ║      획득한 도파민 스톤: 540 💎              ║  │
        │  ║                                              ║  │
        │  ║   R1: ✓ 적중   (+25 💎)                     ║  │
        │  ║   R2: ✗ 빗나감                              ║  │
        │  ║   R3: ✓ 적중   (+30 💎)                     ║  │
        │  ║   R4: ✓ 적중   (+80 💎)                     ║  │
        │  ╚═════════════════════════════════════════════╝  │
        │                                                    │
        │  ── 부수 정보 (작은 글씨, 접힘 가능) [▼ 펼치기] ──    │
        │   R1 단승   25🟦  → 1.7x  → +18🟦                 │
        │   R2 복승   50🟦  → 4.2x  → -50🟦                 │
        │   R3 단승   30🟦  → 6.2x  → +157🟦                │
        │   R4 삼복승 80🟦  → 10x   → +720🟦                │
        │   최종 보유: 🟦 940                                │
        │                                                    │
        │      [ 랭킹 등록 ]   (Phase 4에서 컷오프 진입 시 활성)│
        │      [ 글로벌 랭킹 보기 ]   (Phase 5에서 활성)      │
        │      [ 다시 도전 ]                                  │
        └────────────────────────────────────────────────────┘
```

## 스프린트 분해 (8 atomic steps)

| Step | 작업 | 산출물 | 검증 | 담당 |
|------|------|--------|------|------|
| **3.1** | `SceneBootstrapper.GameOver.cs` 신규 partial | GameOver 상태 분기 | 컴파일 통과 | client |
| **3.2** | `GameOverPanelPrefab` 신규 프리팹 (처음부터 프리팹) | 어두운 배경 + 메시지 + 버튼 | Play 시뮬 진입 표시 | client |
| **3.3** | `GameState.GameOver` 진입 시 자동 표시 | 자동 분기 (Phase 1에서 enum 추가됨) | TC-C1-01 | client |
| **3.4** | [다시 도전] → 새 게임 시작 | ResetForNewGame + SceneManager.LoadScene | TC-C1-02 | client |
| **3.5** | `SceneBootstrapper.Finish.cs` 메인/부수 영역 분리 | 두 영역 GameObject + 토글 | TC-C2-01, C2-02 | client |
| **3.6** | Finish 메인 영역 — 큰 글씨 + 페이드인 + ✓/✗ 라운드 리스트 | 시각적 임팩트 | TC-C2-03 | client |
| **3.7** | StringTable UID 키 ~15개 추가 | str.gameover.* / str.finish.* | StringTableValidator 통과 | client+leader |
| **3.8** | QA 시나리오 — GameOver vs Finish 분리 검증 | Unity MCP 시뮬 2건 | TC-C3-01 | qa |

### 의존성

```
3.1 → 3.2 → 3.3   (partial → 프리팹 → 분기 표시)
3.3 → 3.4         (표시 → 다시 시작)
3.5 → 3.6         (영역 분리 → 메인 강조)
3.7 ← 3.1 + 3.5   (Loc 키 — 두 화면 모두 의존)
3.8 ← all         (전체 시나리오 검증)
```

## QA 체크리스트

| TC | 내용 | Pass 기준 |
|----|------|----------|
| TC-C1-01 | GAME OVER 진입 자동 표시 | Wallet.Jelly=0 → GameState.GameOver → 패널 자동 표시 |
| TC-C1-02 | [다시 도전] 흐름 | 클릭 → Wallet 리셋(100/0) → 씬 재로드 → 베팅 화면 정상 |
| TC-C1-03 | GAME OVER 메시지 정확도 | 3라운드 시점 GameOver → "3 / 4 라운드에서 종료" 표시 |
| TC-C2-01 | Finish 메인 영역 표시 | 마지막 라운드 완주 → 큰 글씨 "획득한 도파민 스톤: N💎" 표시 |
| TC-C2-02 | Finish 부수 영역 토글 | 진입 시 부수 영역 접힘 → [▼ 펼치기] 클릭 → 펼쳐짐 |
| TC-C2-03 | 메인 영역 ✓/✗ 정확도 | R1=적중(+25💎) / R2=빗나감 / R3=적중(+30💎) / R4=적중(+80💎) — 정확히 표시 |
| TC-C2-04 | 부수 영역 데이터 | "R1 단승 25🟦 → 1.7x → +18🟦" 형식 (실제 베팅 정보 일치) |
| TC-C2-05 | Finish 페이드인 애니메이션 | 메인 영역이 0초~0.5초 동안 alpha 0 → 1 페이드인 |
| TC-C3-01 | 두 화면 분기 | (a) Jelly 0 도달 → GameOver, (b) 마지막 라운드 완주 → Finish — 충돌 없음 |
| TC-C3-02 | StringTableValidator | 누락·미사용 0건 |
| TC-C3-03 | 7언어 깨짐 없음 | en/ja/zh_CN/de/es/br 모두 표시 정상 |
| TC-C3-04 | Unity 콘솔 에러 0 | Phase 3 종료 후 read_unity_console_logs 결과 0 |

### Unity MCP 실행 예시 (TC-C1-01)

```csharp
// GameOver 분기 트리거 시뮬레이션
WalletManager.Instance.ResetForNewGame();
WalletManager.Instance.TryBet(100);  // Jelly = 0
// 빗나감 시나리오 (Reward 호출 안함)
GameManager.Instance.ChangeState(GameState.GameOver);
yield return new WaitForSeconds(0.1f);
var panel = GameObject.Find("GameOverPanel");
Debug.Log($"[TC-C1-01] panel.activeSelf={panel?.activeSelf} (expected True)");
```

## StringTable 키 추가 (R3.9)

### GAME OVER (UC3 직설적 톤)
```
str.gameover.headline             = "💔 GAME OVER"
str.gameover.desc                 = "도파민 젤리가 모두 소진됐습니다"
str.gameover.progress             = "{0} / {1} 라운드에서 종료"
str.gameover.no_ranking           = "랭킹에 등록되지 않았습니다"
str.gameover.btn.restart          = "다시 도전"
```

### Finish 화면 (메인/부수 분리)
```
# 메인 영역
str.finish.headline               = "🏁 게임 종료"
str.finish.stone_total            = "획득한 도파민 스톤: {0} 💎"
str.finish.round.hit              = "R{0}: ✓ 적중   (+{1} 💎)"
str.finish.round.miss             = "R{0}: ✗ 빗나감"

# 부수 영역
str.finish.detail.toggle_show     = "▼ 펼치기"
str.finish.detail.toggle_hide     = "▲ 접기"
str.finish.detail.row             = "R{0} {1}  {2}🟦  → {3}x  → {4}🟦"
str.finish.final_jelly            = "최종 보유: 🟦 {0}"

# 버튼
str.finish.btn.submit_ranking     = "랭킹 등록"
str.finish.btn.view_global_ranking = "글로벌 랭킹 보기"
str.finish.btn.restart            = "다시 도전"
str.finish.no_ranking_qualified   = "Top 10,000 미진입"
```

→ 신규 키 약 **15개**, 7언어 × 15 = 105개 항목.

## 수정 파일 목록

### 신규
| 파일 | 변경 |
|------|------|
| `Assets/Scripts/Manager/SceneBootstrapper.GameOver.cs` | partial 신규 (R3.1-R3.3) |
| `Assets/Resources/Prefabs/UI/GameOverPanelPrefab.prefab` | 신규 프리팹 (R3.2) |

### 수정
| 파일 | 변경 |
|------|------|
| `Assets/Scripts/Manager/SceneBootstrapper.cs` | GameOver 필드 추가 (partial 동기화 — CLAUDE.md) |
| `Assets/Scripts/Manager/SceneBootstrapper.Finish.cs` | 메인/부수 영역 분리 (R3.4-R3.7) |
| `Assets/Resources/Prefabs/UI/FinishPanelPrefab.prefab` | 메인/부수 영역 GameObject 추가 |
| `Assets/Resources/Data/StringTable.csv` | 신규 키 15개 (R3.9) |
| `Assets/Editor/BettingUIPrefabCreator.cs` | `CreateGameOverPanel()` 메뉴 추가 |

## 게이트 통과 기준

```
Phase 3 통과 = 다음 모두 충족
  ✓ TC-C1-01 ~ TC-C3-04 모두 PASS
  ✓ 두 분기 시나리오(GameOver vs Finish) 충돌 없음
  ✓ Finish 메인/부수 영역 분리 + 토글 동작
  ✓ Phase 2 의존성 문제 없음 (헤더·모달이 정상 동작 상태에서 Phase 3 검증)
  ✓ StringTableValidator 통과 + 7언어 깨짐 없음
  ✓ 컴파일·런타임 에러 0
```

## 비고

- Phase 4 [랭킹 등록] 버튼은 이번 Phase에서는 **비활성 placeholder**로만 배치 (Phase 4에서 활성화 로직 추가).
- Phase 5 [글로벌 랭킹 보기] 버튼도 동일하게 placeholder.
- 페이드인 애니메이션은 단순 CanvasGroup.alpha 보간으로 구현 (DOTween 의존 X).
- GAME OVER 톤은 추후 마케팅 의견에 따라 String만 변경 가능 — 현재는 직설적·실패감 강조 (도파민 게임 톤).

## 학습된 제약 (반영)

- partial class 추가 시 `SceneBootstrapper.cs` (필드 정의)도 반드시 같이 수정 (CLAUDE.md 규칙)
- Finish 메인 영역의 페이드인은 Update 루프 내 alpha 보간으로 구현 (코루틴 또는 LerpUnclamped)
- Resources/Prefabs/UI/ 경로 일관성 유지 (CurrencyHeaderPrefab, BetAmountModalPrefab과 동일 위치)

---

## ★ Phase 3 내 에이전트 협업 시퀀스

### 작업 순서

```
[leader] Phase 3 브리프 + UC3 직설적 톤 + UC4 메인/부수 분리 강조
   │
   ├─→ [client] Step 3.1 (SceneBootstrapper.GameOver.cs partial)
   │     │
   │     └─→ [client] Step 3.2 (GameOverPanelPrefab — 처음부터 프리팹)
   │           │
   │           └─→ [client] Step 3.3 (GameState.GameOver 진입 시 자동 표시)
   │                 │
   │                 └─→ [client] Step 3.4 (다시 도전 → 새 게임)
   │
   ├─→ [client] Step 3.5 (Finish 메인/부수 영역 분리)
   │     │
   │     └─→ [client] Step 3.6 (메인 영역 페이드인 + ✓/✗)
   │
   ├─→ [client+leader] Step 3.7 (StringTable UID 키 15개 7언어)
   │
   └─→ [qa] Step 3.8 (GameOver vs Finish 분기 시나리오 검증)
         │
         └─→ [leader] 게이트 검수
               ├─ PASS → Phase 4 진입 가능 (Phase 1+3 통과 상태)
               └─ FAIL → 재작업 사이클 (§E)
```

### 핸드오프 체크리스트

| 송출자 | 수신자 | 핸드오프 조건 |
|---|---|---|
| client (Step 3.1~3.4) | client (Step 3.5~3.6) | GameOver 패널 단독 동작 검증 (Wallet.Jelly=0 시뮬) |
| client (Step 3.5~3.6) | qa | Finish 패널 메인/부수 분리 + 페이드인 정상 |
| client (Step 3.7 Loc 키) | leader+qa | StringTableValidator 통과 + 7언어 입력 확인 |
| qa (Step 3.8) | leader | 두 시나리오 (a) Jelly 0 → GameOver, (b) 마지막 라운드 → Finish 모두 PASS |
| leader | 오너 | 게이트 통과 보고 + Phase 4 진입 가능성 명시 |

## 예상 재작업 시나리오 (사전 대비)

| 시나리오 | 발생 단계 | 처리 |
|---|---|---|
| Wallet.Jelly=0 시 GameOver 안 뜸 | TC-C1-01 fail | client → GameManager.CalcScore의 jelly==0 체크 누락 점검 (Phase 1 Step 1.9 의존) |
| [다시 도전] 후 Wallet 리셋 안 됨 | TC-C1-02 fail | client → SceneManager.LoadScene 후 WalletManager.ResetForNewGame 호출 누락 |
| Finish 메인/부수 영역이 동시에 표시 (접힘 무시) | TC-C2-02 fail | client → 부수 영역 GameObject 기본 비활성 설정 누락 |
| 라운드별 ✓/✗ 데이터가 실제 결과와 불일치 | TC-C2-03 fail | balance + client → ScoreManager에서 가져오는 데이터 키 매핑 점검 |
| 페이드인 애니메이션이 너무 느리거나 빠름 | TC-C2-05 | client → CanvasGroup.alpha 보간 시간 조정 (0.5초 권장) |
| 7언어 표시 시 메인 영역 텍스트 잘림 | TC-C3-03 | client → TMP_Text autoSize 또는 wrapping 설정 |
| Phase 2 의존성 누락 (CurrencyHeader 안 보임) | 통합 검증 | leader → Phase 2 게이트 재검증 → 미통과 시 Phase 2 처음부터 |

## Phase 3 게이트 통과 = 다음 Phase 진입 조건

```
✓ TC-C1-01 ~ TC-C3-04 모두 PASS (Unity MCP 결과 첨부)
✓ 두 분기 시나리오 (GameOver vs Finish) 충돌 없음
✓ Finish 메인/부수 영역 분리 + 토글 동작
✓ Phase 1+2 의존성 정상 (회귀 없음)
✓ StringTableValidator 통과 (신규 키 15개)
✓ 7언어 표시 깨짐 없음
✓ leader 검수 통과 → 오너 보고 → Push 컨펌
✓ Phase 4 진입 가능 (Phase 1+3 통과 = Phase 4 시작 가능)
```

> Phase 4의 NicknameInputModal은 Finish 화면 위에 오버레이로 표시되므로 Phase 3 완료 필수.
