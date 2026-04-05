# 히스토리: CollisionVFX 재설계 + 결과UI + 순위표시 개선 (2026-03-06)

## 개요

Session 5에서 진행한 3개 작업 그룹.

---

## 1. CollisionVFX 완전 재설계

### 배경
레이싱 중 충돌/회피/슬링샷 이벤트 시 캐릭터 머리 위 VFX에서
텍스트(HIT!/CHASE! 등)는 보였지만 **배경 원 + 아이콘 SpriteRenderer가 안 보이는 버그**.

5번의 수정 시도 (static 플래그, HideFlags, 인스턴스 기반, null 재생성, vfxRoot 재생성) 모두 실패.

### 근본 원인 분석
- Unity 6 "Reload Scene Only" 환경에서 런타임 `Texture2D` + `Sprite.Create()`로
  생성한 스프라이트가 SpriteRenderer 할당 후 무효화됨
- 프로젝트 내 정상 작동 SpriteRenderer는 전부 **프리팹/에셋 기반 스프라이트** 사용 중
- TextMesh는 별도 렌더링 시스템이라 영향 없음

### 해결책
`Resources.Load<Sprite>()` 기반으로 완전 전환.

### 변경 파일
| 파일 | 변경 |
|------|------|
| `Assets/Scripts/Racer/Collision/CollisionVFX.cs` | 완전 재작성 |
| `Assets/Scripts/Racer/Collision/CollisionSpriteFactory.cs` | **삭제** |
| `Assets/Scripts/Editor/CollisionVFXSpriteGenerator.cs` | 신규 — PNG 생성 Editor 도구 |
| `Assets/Resources/VFX/*.png` | 신규 — 5개 PNG 에셋 (64x64, PPU=64) |

### VFX 에셋 목록
| 파일 | 용도 |
|------|------|
| `vfx_circle.png` | 배경 원 |
| `vfx_star6.png` | HIT! 아이콘 (6각 별) |
| `vfx_star5.png` | BOOST! 아이콘 (5각 별) |
| `vfx_arrow.png` | CHASE! 아이콘 (화살표) |
| `vfx_shield.png` | DODGE! 아이콘 (방패) |

### 교훈
> 런타임 `Sprite.Create()` + SpriteRenderer는 Unity 6 "Reload Scene Only"에서 무효화됨.
> PNG 에셋을 Resources에 미리 저장하고 `Resources.Load<Sprite>()` 로 로드하는 것이 정답.

### 커밋
`bdd8367` [C] refactor: CollisionVFX 완전 재설계 — Resources 기반 스프라이트로 전환

---

## 2. 결과 화면 UI 프리팹 기반 리팩터링

### 배경
기존 `SceneBootstrapper.Result.cs`가 UI를 코드로 직접 생성하는 방식.
프리팹 기반으로 전환하여 유지보수성 향상.

### 변경 내용
- `GameSettings`에 `resultPanelPrefab` 필드 추가
- `SceneBootstrapper.Result.cs` 리팩터링:
  - `BuildResultUI()` → 프리팹 인스턴스화 기반으로 전환
  - 하위 요소 캐싱: RankIcons(3), RankNames(3), BetPickRows(3), ScoreText 등
  - 레거시 빌드 폴백 유지 (프리팹 미설정 시)
- `SceneBootstrapper.cs`: 결과 UI 필드 확장 (배열 기반 구조)
- `ResultUIPrefabCreator.cs` 신규 (589줄):
  - 메뉴: `DopamineRace > Create Result UI Prefabs`
  - ResultPanel, FinishPanel, LeaderboardPanel 프리팹 생성

### RaceDebugOverlay 탭 시스템 전환
- 기존: 3개 별도 스크롤뷰 (scrollPos, raceLogScroll, finishLogScroll)
- 변경: 단일 탭 시스템 (`activeTab` 0=상태 / 1=HP / 2=이벤트)
- Odds 섹션 내부 스크롤뷰 제거 (탭 외부 스크롤로 통합)

---

## 3. 순위 표시 개선

### 변경 내용

#### 최근 순위 표시 개수 6→5개
- `CharacterInfoPopup.cs` — `MAX_DIST_DISPLAY = 6` → `5`
- 사유: 6번째 항목이 창 밖으로 잘리는 UI 버그

#### 다국어 순위 포맷 통일 (숫자+1자리)
기준: 접미사가 2자 이상이면 단축.

| 언어 | 변경 전 | 변경 후 | 이유 |
|------|---------|---------|------|
| en | `{0}th` (코드 오버라이드: 1st/2nd/3rd/4th) | `{0}.` | 2자리 접미사 → 1자리 |
| zh_CN | `第{0}名` | `{0}名` | 접두사 "第" 제거 |
| ko/ja/de/es/br | 변경 없음 | — | 이미 1자리 |

- `Loc.cs` — `GetRank()` 영어 전용 서수 오버라이드 제거, StringTable 통일
- `StringTable.csv` — `str.hud.rank` 영어/zh_CN 수정

---

## 다음 작업

- Result UI 프리팹 실제 생성 후 GameSettings에 연결
- CollisionVFX 2번째 플레이 테스트 (이 방에서 이미 스프라이트 로드 확인 완료)
