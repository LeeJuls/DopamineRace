# 🚀 DopamineRace - 새 PC 환경 세팅 가이드

> 새 PC에서 Claude + GitHub + Unity 개발 환경을 세팅하는 가이드입니다.
> `auto_setup.ps1` 스크립트를 실행하면 대부분 자동으로 세팅됩니다.

---

## 📋 전체 순서 요약

1. 필수 소프트웨어 설치
2. `auto_setup.ps1` 실행 (자동 세팅)
3. Claude Desktop 실행 & 확인
4. Unity 프로젝트 열기

---

## 1단계: 필수 소프트웨어 수동 설치

아래 프로그램들은 **직접 설치**해야 합니다 (자동 스크립트가 설치 여부를 체크해줍니다)

| 프로그램 | 다운로드 링크 | 비고 |
|---------|-------------|------|
| **Git** | https://git-scm.com/download/win | 기본 옵션으로 설치 |
| **Node.js** | https://nodejs.org/ (LTS) | v20 이상 권장 |
| **Python** | https://www.python.org/downloads/ | 3.10 이상, PATH 체크 필수 |
| **UV** (Python 패키지 매니저) | PowerShell: `irm https://astral.sh/uv/install.ps1 \| iex` | pip 대체 |
| **Claude Desktop** | https://claude.ai/download | Anthropic 계정 로그인 필요 |
| **Unity Hub** | https://unity.com/download | Unity 6 설치 |


## 2단계: 자동 세팅 스크립트 실행

### 방법 A: GitHub에서 처음 시작하는 경우

```powershell
# 1. 원하는 폴더에서 레포 클론
git clone https://github.com/LeeJuls/DopamineRace.git D:/unity/Project/DopamineRace

# 2. 세팅 스크립트 실행
cd D:/unity/Project/DopamineRace/Docs/setup
powershell -ExecutionPolicy Bypass -File auto_setup.ps1
```

### 방법 B: 이미 클론한 경우

```powershell
cd D:/unity/Project/DopamineRace/Docs/setup
powershell -ExecutionPolicy Bypass -File auto_setup.ps1
```

### 스크립트가 자동으로 하는 것들:
- ✅ 필수 소프트웨어 설치 여부 확인
- ✅ Git 사용자 설정 (LeeJuls / clauzbt@gmail.com)
- ✅ GitHub 인증 설정
- ✅ Claude Code (npm 패키지) 설치
- ✅ Claude Desktop MCP 서버 설정 파일 생성
- ✅ Unity MCP 브릿지 Python 패키지 설치

---

## 3단계: Claude Desktop 설정 확인

스크립트 실행 후 Claude Desktop을 **재시작**하면 MCP 서버가 자동 연결됩니다.

### 확인 방법:
1. Claude Desktop 실행
2. 좌측 하단에 MCP 서버 아이콘(🔌) 확인
3. `unity-code-mcp-stdio` 서버가 연결된 상태인지 확인

### MCP 설정 파일 위치:
```
%APPDATA%\Claude\claude_desktop_config.json
```


## 4단계: Unity 프로젝트 열기

1. Unity Hub 실행
2. **Add** → `D:/unity/Project/DopamineRace` 선택
3. Unity 6 버전으로 프로젝트 열기
4. Unity 에디터에서 `Tools > Unity Code MCP > Start Server` 실행

---

## 5단계: Claude Code (터미널용) 사용법

```powershell
# 프로젝트 폴더로 이동 후
cd D:/unity/Project/DopamineRace

# Claude Code 실행
claude
```

Claude Code에서 Unity MCP 서버 및 Desktop Commander를 사용할 수 있습니다.

---

## 🔧 트러블슈팅

### MCP 서버 연결 안 될 때
```powershell
# Unity MCP 프로세스 확인
Get-Process -Name "unity-code-mcp-stdio" -ErrorAction SilentlyContinue

# UV로 MCP 서버 수동 실행
uv run --directory "D:/unity/Project/DopamineRace/Assets/Plugins/UnityCodeMcpServer/Editor/STDIO~" unity-code-mcp-stdio --host localhost --port 21088
```

### Git push 안 될 때 (인증 오류)
```powershell
# 크리덴셜 매니저 초기화 후 다시 push
git credential-manager erase
git push origin main
# → 로그인 팝업이 뜨면 GitHub 계정으로 로그인
```

### Claude Desktop 설정 초기화
```powershell
# 설정 파일 백업 후 스크립트 다시 실행
copy "%APPDATA%\Claude\claude_desktop_config.json" "%APPDATA%\Claude\claude_desktop_config.backup.json"
powershell -ExecutionPolicy Bypass -File auto_setup.ps1
```

---

## 📁 프로젝트 구조 참고
```
DopamineRace/
├── .claude/              # Claude Code 설정
│   ├── launch.json       # MCP 브릿지 설정
│   └── settings.local.json # Claude Code 권한 설정
├── Assets/               # Unity 프로젝트 소스
│   └── Plugins/UnityCodeMcpServer/  # Unity MCP 플러그인
├── Docs/                 # 문서
│   ├── setup/            # ← 이 가이드가 여기!
│   └── history/          # 작업 히스토리
└── ProjectSettings/      # Unity 프로젝트 설정
```

---

> 💡 **팁**: 이 가이드와 `auto_setup.ps1`은 GitHub 레포에 포함되어 있으므로,
> 다른 PC에서 `git clone` 후 바로 스크립트를 실행하면 됩니다!
