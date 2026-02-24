# ============================================
# DopamineRace - 자동 환경 세팅 스크립트
# 실행: powershell -ExecutionPolicy Bypass -File auto_setup.ps1
# ============================================

param(
    [string]$ProjectPath = "D:/unity/Project/DopamineRace",
    [string]$GitUser = "LeeJuls",
    [string]$GitEmail = "clauzbt@gmail.com",
    [string]$GitRepo = "https://github.com/LeeJuls/DopamineRace.git"
)

$ErrorActionPreference = "Continue"

function Write-Step($msg) { Write-Host "`n=== $msg ===" -ForegroundColor Cyan }
function Write-OK($msg) { Write-Host "  [OK] $msg" -ForegroundColor Green }
function Write-WARN($msg) { Write-Host "  [!] $msg" -ForegroundColor Yellow }
function Write-FAIL($msg) { Write-Host "  [X] $msg" -ForegroundColor Red }

Write-Host ""
Write-Host "========================================" -ForegroundColor Magenta
Write-Host "  DopamineRace 환경 자동 세팅 스크립트" -ForegroundColor Magenta
Write-Host "========================================" -ForegroundColor Magenta
Write-Host ""

# ------------------------------------------
# 1. 필수 소프트웨어 체크
# ------------------------------------------
Write-Step "1/6 - 필수 소프트웨어 확인"

$allGood = $true

# Git
try { $gitVer = git --version 2>$null; Write-OK "Git: $gitVer" }
catch { Write-FAIL "Git 미설치 → https://git-scm.com/download/win"; $allGood = $false }

# Node.js
try { $nodeVer = node --version 2>$null; Write-OK "Node.js: $nodeVer" }
catch { Write-FAIL "Node.js 미설치 → https://nodejs.org/"; $allGood = $false }

# Python
try { $pyVer = python --version 2>$null; Write-OK "Python: $pyVer" }
catch { Write-FAIL "Python 미설치 → https://www.python.org/downloads/"; $allGood = $false }

# UV
try { $uvVer = uv --version 2>$null; Write-OK "UV: $uvVer" }
catch { 
    Write-WARN "UV 미설치 - 자동 설치 시도..."
    irm https://astral.sh/uv/install.ps1 | iex
    $env:PATH += ";$env:USERPROFILE\.local\bin"
}

# Claude Desktop
$claudeExe = "$env:LOCALAPPDATA\AnthropicClaude\claude.exe"
if (Test-Path $claudeExe -ErrorAction SilentlyContinue) { 
    Write-OK "Claude Desktop: 설치됨" 
} else {
    $claudeExe2 = "$env:LOCALAPPDATA\Programs\claude-desktop\claude.exe"
    if (Test-Path $claudeExe2 -ErrorAction SilentlyContinue) {
        Write-OK "Claude Desktop: 설치됨"
    } else {
        Write-WARN "Claude Desktop 미설치 → https://claude.ai/download"
    }
}

if (-not $allGood) {
    Write-Host "`n미설치 프로그램을 먼저 설치한 후 스크립트를 다시 실행하세요." -ForegroundColor Red
    Read-Host "아무 키나 누르면 종료..."
    exit 1
}

# ------------------------------------------
# 2. Git 설정
# ------------------------------------------
Write-Step "2/6 - Git 사용자 설정"

git config --global user.name $GitUser
git config --global user.email $GitEmail
git config --global credential.helper manager
Write-OK "Git 사용자: $GitUser <$GitEmail>"
Write-OK "Credential Helper: manager (Windows)"

# ------------------------------------------
# 3. 프로젝트 클론 (없으면)
# ------------------------------------------
Write-Step "3/6 - 프로젝트 확인"

if (Test-Path "$ProjectPath/.git") {
    Write-OK "프로젝트 이미 존재: $ProjectPath"
    Set-Location $ProjectPath
    git pull origin main 2>$null
    Write-OK "최신 코드 pull 완료"
} else {
    Write-WARN "프로젝트 없음 - 클론 시작..."
    $parentDir = Split-Path $ProjectPath -Parent
    if (-not (Test-Path $parentDir)) { New-Item -ItemType Directory -Path $parentDir -Force | Out-Null }
    git clone $GitRepo $ProjectPath
    Set-Location $ProjectPath
    Write-OK "클론 완료: $ProjectPath"
}

# ------------------------------------------
# 4. Claude Code 설치
# ------------------------------------------
Write-Step "4/6 - Claude Code (npm) 설치"

$claudeCode = npm list -g @anthropic-ai/claude-code 2>$null
if ($claudeCode -match "claude-code") {
    Write-OK "Claude Code: 이미 설치됨"
} else {
    Write-WARN "Claude Code 설치 중..."
    npm install -g @anthropic-ai/claude-code
    Write-OK "Claude Code 설치 완료"
}

# ------------------------------------------
# 5. Unity MCP Python 패키지 설치
# ------------------------------------------
Write-Step "5/6 - Unity MCP 브릿지 설치"

$mcpDir = "$ProjectPath/Assets/Plugins/UnityCodeMcpServer/Editor/STDIO~"
if (Test-Path $mcpDir) {
    Write-OK "Unity MCP 디렉토리 확인: $mcpDir"
    Push-Location $mcpDir
    uv sync 2>$null
    Pop-Location
    Write-OK "Unity MCP Python 의존성 설치 완료"
} else {
    Write-WARN "Unity MCP 디렉토리 없음 - git pull 후 다시 시도하세요"
}

# ------------------------------------------
# 6. Claude Desktop MCP 설정 생성
# ------------------------------------------
Write-Step "6/6 - Claude Desktop MCP 설정"

$claudeConfigDir = "$env:APPDATA\Claude"
$claudeConfigFile = "$claudeConfigDir\claude_desktop_config.json"

# 기존 설정 백업
if (Test-Path $claudeConfigFile) {
    $backupName = "claude_desktop_config.backup_$(Get-Date -Format 'yyyyMMdd_HHmmss').json"
    Copy-Item $claudeConfigFile "$claudeConfigDir\$backupName"
    Write-OK "기존 설정 백업: $backupName"
}

# 프로젝트 경로의 백슬래시를 슬래시로 변환
$mcpDirForJson = ($mcpDir -replace '\\', '/')

$config = @"
{
  "mcpServers": {
    "unity-code-mcp-stdio": {
      "command": "uv",
      "args": [
        "run",
        "--directory",
        "$mcpDirForJson",
        "unity-code-mcp-stdio",
        "--host",
        "localhost",
        "--port",
        "21088"
      ]
    }
  },
  "preferences": {
    "launchPreviewPersistSession": true,
    "coworkScheduledTasksEnabled": false,
    "sidebarMode": "chat"
  }
}
"@

if (-not (Test-Path $claudeConfigDir)) { 
    New-Item -ItemType Directory -Path $claudeConfigDir -Force | Out-Null 
}
Set-Content -Path $claudeConfigFile -Value $config -Encoding UTF8
Write-OK "Claude Desktop 설정 파일 생성 완료"
Write-OK "경로: $claudeConfigFile"

# ------------------------------------------
# 완료!
# ------------------------------------------
Write-Host ""
Write-Host "========================================" -ForegroundColor Green
Write-Host "  ✅ 환경 세팅 완료!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host ""
Write-Host "다음 단계:" -ForegroundColor White
Write-Host "  1. Claude Desktop을 재시작하세요" -ForegroundColor White
Write-Host "  2. Unity Hub에서 프로젝트를 추가하세요: $ProjectPath" -ForegroundColor White
Write-Host "  3. Unity에서 Tools > Unity Code MCP > Start Server 실행" -ForegroundColor White
Write-Host "  4. 터미널에서 'cd $ProjectPath && claude' 로 Claude Code 시작" -ForegroundColor White
Write-Host ""
Read-Host "아무 키나 누르면 종료..."
