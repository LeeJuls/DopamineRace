# 다른 PC에서 작업 이어서 하기

> 리더보드 서버 연동 작업 이후, 다른 PC에서 이어서 작업할 때 셋업 가이드.
> 대부분 git pull로 끝나지만 **리더보드 토큰은 git에 없어서** 1회 수동 셋업 필요.

## 0. 사전 준비물
- Unity **6000.3.7f1** (동일 버전)
- git, Node.js 18+
- (서버 재배포 시) `npm i -g wrangler`

## 1. 코드 받기
```bash
git clone https://github.com/LeeJuls/DopamineRace.git
# 또는 기존 클론이면:  git pull origin main
```
→ 코드·서버(`server/`)·문서·`GameSettings.asset`·**비활성 LeaderboardConfig 껍데기**가 전부 들어옴.
이 상태로 Unity 열면 **리더보드는 로컬 전용(오프라인)으로 안전 동작**.

## 2. 리더보드 원격 켜기 (1회, ~1분)
토큰은 공개 저장소 보호 위해 git에 없음 → 수동 셋업:
1. **이전 PC의 `server/leaderboard-worker/SECRETS.local.txt`를 복사**해 이 PC 같은 위치에 둠
   (USB·비밀번호 관리자·메모앱 등 안전 경로)
2. Unity → `Assets/Resources/LeaderboardConfig.asset` 클릭 → Inspector:
   - **Enable Remote** ✅
   - **Api Base Url** : `https://dopamine-leaderboard.clauzbt.workers.dev`
   - **Write Token**  : (SECRETS.local.txt의 값)
3. `Ctrl+S` 저장
4. **git이 이 변경을 무시하도록**(토큰 실수 커밋 방지):
   ```bash
   git update-index --skip-worktree Assets/Resources/LeaderboardConfig.asset
   ```
   → 이후 `git status`에 안 뜨고, 빌드는 원격 연동됨.
   ※ 자세한 운영(재배포·토큰교체·초기화)은 `Docs/leaderboard_setup.md` 참고.

## 3. (선택) 서버 작업할 때만
```bash
cd server/leaderboard-worker
wrangler login        # 브라우저 인증 (이 PC 최초 1회)
wrangler deploy       # 코드 수정 후 재배포
```
> PowerShell에서 `npm`/`wrangler`가 "스크립트 실행 불가" 에러나면 → **cmd.exe** 사용하거나
> 현재 창에서 `Set-ExecutionPolicy -Scope Process Bypass` 후 실행.

## 4. 알아둘 것 (git 상태)
| 파일 | 상태 |
|------|------|
| `LeaderboardConfig.asset` | git엔 **비활성 껍데기**. 로컬 토큰은 skip-worktree로 숨김 → `M` 안 뜸 |
| `GameSettings.asset` | **커밋됨**. 현재 `roundLaps=[2]`(1라운드 **테스트값**) |
| `SECRETS.local.txt` | gitignore(절대 커밋 안 됨) — 수동 복사 전용 |

⚠️ **출시 전 체크**: `GameSettings.asset`의 `roundLaps`를 프로덕션값으로 복원
(예: `[2,3,4,5]` 또는 코드 기본 `[2,2,3,5,3,2,4]`). 지금은 빠른 테스트용 `[2]`.

## 5. 프로젝트 규칙
- 커밋 접두사 `[C]`(Claude)·`[L]`(오너)·`[UI]`(디자이너) — `CLAUDE.md` 참고
- MCP: Play/Recompile 중 사용 자제. 새 세션 전 `Get-Process unity-code-mcp-stdio | Stop-Process -Force`
