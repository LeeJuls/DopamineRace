# 워크트리 동기화 사고 → 롤백 → 재발 방지 히스토리

**날짜**: 2026-03-06
**브랜치**: `claude/keen-khayyam` → 최종 `main` 반영
**안정 기준 커밋**: `6b05c03`

---

## 사고 경위

### 1. 발단: cp 명령어로 파일 덮어쓰기
이전 세션에서 워크트리(`claude/keen-khayyam`) 파일을 메인 프로젝트(`main`)로 동기화할 때
`cp` 명령어를 사용. 이때 **main이 더 최신인 파일을 구버전 worktree 파일로 덮어씀** → regression 유발.

### 2. 발생한 에러들
| 에러 | 원인 |
|------|------|
| `CS0246 RacerCircleUI 타입 없음` | SceneBootstrapper.cs에서 필드 선언 누락 |
| `CS0103 UpdateLiveRankings 없음` | 존재하지 않는 메서드 호출 |
| `CS1061 finishPanelPrefab/leaderboardPanelPrefab 없음` | GameSettings.cs 필드 누락 |
| `CS0103 raceLogScroll/finishLogScroll 없음` | RaceDebugOverlay.cs 삭제된 변수 참조 |
| `FormatException in ShowLeaderboard` | Loc.Get() format args 불일치 |
| `str.hud.goal, str.hud.start 키 없음` | StringTable.csv worktree 버전으로 덮어쓰면서 키 삭제 |
| 결과UI 캐릭터 아이콘 gray 박스 | CharIconMask/CharIcon 경로 처리 누락 |

### 3. 수정 시도 → 실패 반복
각 에러를 개별 수정하는 과정에서 연쇄 오류가 계속 발생. 부분 수정이 오히려 더 꼬임.

### 4. 결정: 6b05c03 기준 전체 롤백
```bash
git checkout 6b05c03 -- \
  Assets/Scripts/Manager/UI/SceneBootstrapper.cs \
  Assets/Scripts/Manager/UI/SceneBootstrapper.Racing.cs \
  Assets/Scripts/Manager/UI/SceneBootstrapper.Finish.cs \
  Assets/Scripts/Manager/UI/SceneBootstrapper.Leaderboard.cs \
  Assets/Scripts/Manager/UI/SceneBootstrapper.Result.cs \
  Assets/Scripts/Debug/RaceDebugOverlay.cs \
  Assets/Scripts/Data/GameSettings.cs \
  Assets/Resources/Data/StringTable.csv
```
- `str.hud.rank` 포맷만 최신 버전(`{0}위,{0}.,...`)으로 재적용

---

## 근본 원인 분석

| 항목 | 내용 |
|------|------|
| **직접 원인** | `cp` 명령어로 버전 확인 없이 파일 덮어쓰기 |
| **구조적 원인** | Partial class(SceneBootstrapper 5개)는 필드 의존성이 있어 일부만 교체 시 컴파일 에러 필연 |
| **StringTable 원인** | CSV도 덮어쓰면서 main에만 있던 키(str.hud.goal 등)가 삭제됨 |

---

## 재발 방지 대책

### 워크트리 동기화 규칙
- `cp worktree → main` **절대 금지**
- 올바른 방법:
  ```bash
  # 특정 커밋만 적용
  git cherry-pick <커밋해시>
  # 또는 브랜치 전체 머지
  git merge claude/keen-khayyam
  ```
- 동기화 전 항상 diff 확인:
  ```bash
  git diff HEAD..claude/keen-khayyam -- <파일경로>
  ```

### Partial class 세트 규칙
SceneBootstrapper.*.cs 5개는 **항상 함께** 비교/동기화. 절대 일부만 교체하지 않는다.

### StringTable 보호
- CSV는 merge/cherry-pick으로만 교체
- 교체 후 반드시 str.hud.goal, str.hud.start 등 필수 키 존재 확인

---

## 개발 도구 개선: StringTableValidator

### 신규 추가 (커밋: `4236b0a`)
- 파일: `Assets/Scripts/Editor/StringTableValidator.cs`
- 코드에서 `Loc.Get("str.xxx")` 패턴 스캔 → CSV 키와 비교
- 누락 키 / 미사용 키 구분 출력

### 개선: Play 버튼 자동 검증 (커밋: `02b2b2d`)
자동 실행 타이밍 추가:

| 타이밍 | 방법 | 동작 |
|--------|------|------|
| 코드 저장·컴파일 완료 | `[DidReloadScripts]` | Console 경고 |
| **Play 버튼 클릭 직전** | `[InitializeOnLoad]` + `playModeStateChanged` | Console 누락 키 목록 |
| 수동 | `DopamineRace > Validate StringTable Keys` | 상세 창 |

---

## 커밋 로그 (이번 세션)

| 커밋 | 내용 |
|------|------|
| `36764ed` | fix: RacerCircleUI 구조체+필드 선언 복원 |
| `59a2623` | fix: 컴파일 오류 수정 (Finish/Leaderboard 필드 복원 등) |
| `ec8ffa2` | fix: GameSettings leaderboardPanelPrefab + trackProgressBarPrefab 추가 |
| `e1fc4e9` | fix: 결과UI 순위 표시 9위까지 확장 |
| `a9ae270` | revert: 결과UI 어제 버전 롤백 (6b05c03 기준) |
| `4236b0a` | feat: StringTable 검증 툴 + ResultUI null 체크 |
| `02b2b2d` | feat: StringTableValidator Play 진입 직전 자동 검증 추가 |
