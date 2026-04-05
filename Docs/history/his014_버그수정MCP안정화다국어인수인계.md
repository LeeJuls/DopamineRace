# 인수인계서 — 버그수정 / MCP 안정화 / 다국어 / UI 협업
> 작업일: 2026-03-02
> 최종 커밋: `93c16fc`
> 브랜치: `main`

---

## 이번 세션 작업 요약

### 1. BetOrderLabel 클릭 차단 버그 수정 ✅
**증상**: 배팅 화면에서 캐릭터 클릭해도 선택 번호(BetOrderLabel)가 표시 안 됨

**원인**: `BettingPanel.prefab`에서 `CharacterInfoPopup`이 `active=True`로 저장되어 있었음.
- `CharacterInfoPopup`은 Image(raycastTarget=True)를 가진 큰 패널
- 캐릭터 리스트 위를 덮어서 모든 클릭을 차단

**수정**: `BettingPanel.prefab` → `CharacterInfoPopup` → `active=False`

**교훈**: 팝업류 오브젝트는 프리팹 기본값을 반드시 `inactive`로 저장할 것.
`Init()`에서 `SetActive(false)` 호출해도, 호출 전 한 프레임이라도 active면 클릭 차단 발생 가능.

---

### 2. ConditionIconFactory 씬 리로드 버그 수정 ✅
**증상**: `Enter Play Mode → Reload Scene Only` 설정 후 컨디션 화살표가 빈 박스로 표시

**원인**: `ConditionIconFactory`는 static 클래스. 씬 리로드 시:
- static `cache` 딕셔너리는 **생존** (Domain Reload 없으므로)
- 딕셔너리 안의 `Sprite`/`Texture2D` 오브젝트는 **파괴됨** (런타임 생성 오브젝트)
- `GetIcon()` 호출 시 파괴된 Sprite 반환 → 빈 박스

**수정** (`ConditionIconFactory.cs` 29번 라인):
```csharp
// 수정 전
if (cache.TryGetValue(condition, out Sprite cached))
    return cached;

// 수정 후
if (cache.TryGetValue(condition, out Sprite cached) && cached != null)
    return cached;
```
Unity의 `==` 오버로딩 덕분에 Destroy된 오브젝트는 `null`로 판별됨 → 재생성 트리거.

---

### 3. MCP 안정화 ✅
**문제**: `unity-code-mcp-stdio` 서버가 자주 다운됨

**원인 분석**:
| 상황 | 원인 |
|------|------|
| Play 모드 진입/종료 | Domain Reload → 자식 프로세스 종료 |
| 스크립트 저장/컴파일 | Domain Reload → 자식 프로세스 종료 |
| MCP 스크립트 타임아웃 | 재귀/긴 루프 → 서버 불안정 |
| 좀비 프로세스 | 이전 세션 잔재 → 포트 충돌 |

**해결책**:
1. `ProjectSettings/EditorSettings.asset`: `Reload Scene Only` 적용 (git 동기화 → 다른 PC도 자동 적용)
2. `Docs/setup/mcp_kill_zombie.bat`: 세션 시작 전 좀비 정리
3. `Docs/setup/mcp_watchdog.ps1`: MCP 다운 감지 워치독
4. MCP 스크립트 작성 규칙: 재귀/전체 순회 금지, 특정 오브젝트만 직접 접근

**알려진 제약**:
- 세션 중 MCP 재시작해도 해당 대화에서는 툴 재등록 안 됨 → **새 Claude 세션 필요**
- 컴파일 발생 시 MCP 끊김은 여전히 발생 (Reload Scene Only는 Play만 해결)

---

### 4. 다국어 텍스트 수정 ✅

**StringTable.csv 변경 키**:
| uid | 변경 내용 |
|-----|----------|
| `str.ui.panel.bet_title` | en: Place Your Bet → **Bet Select**, 전 언어 통일 (ja 유지) |
| `str.ui.betting.title` | en: Place Your Bet! → **Bet Select!**, 전 언어 통일 (ja 유지) |

**BettingPanel.prefab TitleText BestFit 활성화**:
- `m_BestFit: 1`, `m_MinSize: 10`, `m_MaxSize: 40`
- `m_HorizontalOverflow: 0` (Wrap) — BestFit이 가로 경계 인식하려면 필수
- 효과: 다국어 텍스트가 영역 초과 시 최소 10pt까지 자동 축소

---

### 5. 멀티PC + UI 디자이너 협업 환경 ✅

**CLAUDE.md** (프로젝트 루트, git 추적):
- 어느 PC에서나 Claude Code 실행 시 자동 로드
- 핵심 목표, Git 규칙, 아키텍처 요약, charId 체계, 에이전트 구성 포함

**auto_setup.ps1 Step 7**:
- MEMORY.md를 Claude 메모리 폴더(`~/.claude/projects/D--unity-Project-DopamineRace/memory/`)에 자동 복사

**UI 디자이너 파일 전달 시스템**:
- `Assets/Design/Incoming/[날짜_작업명]/` — 디자이너가 파일 놓는 폴더
- `Assets/Design/Incoming/전달노트_양식.md` — 파일 경로/이름 가이드 포함
- Claude에게 "디자인 파일 적용해줘" → 자동으로 올바른 위치로 이동+적용
- 상세 가이드: `Docs/20260301_UI디자인파일전달_워크플로우.md`

---

## 현재 프리팹 상태 (수동 작업 필요 항목)

> **중요**: 프리팹 UI 위치 조정은 Claude 좌표와 실제 Unity 뷰포트가 일치하지 않아 **사용자가 Unity Editor에서 직접** 해야 함

| 작업 | 파일 | 상태 |
|------|------|------|
| OddsLabel(배당률 3.6x) 위치 이동 | CharacterItem.prefab | ⏳ 대기 |
| CharacterListPanel 우측 너비 조절 | BettingPanel.prefab | ⏳ 대기 |

---

## 다음 세션 시작 체크리스트

```
1. Docs/setup/mcp_kill_zombie.bat 실행
2. Unity 열기 → Tools > UnityCodeMcpServer > STDIO > Restart Server
3. 새 Claude 세션 시작 (MCP 툴 재등록)
4. Play 모드 테스트: 컨디션 화살표 정상 표시 확인
5. 클릭 테스트: BetOrderLabel 선택 번호 표시 확인
```

---

## 커밋 이력

| 해시 | 내용 |
|------|------|
| `746314b` | BetOrderLabel 클릭 차단 버그 수정 + MCP 안정화 (프리팹/코드/ProjectSettings) |
| `34887b8` | MCP 안정화 유틸 스크립트 (zombie bat + watchdog ps1) |
| `93c16fc` | TitleText BestFit + Bet Select 번역 통일 |
