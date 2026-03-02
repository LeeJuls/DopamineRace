# ============================================
# MCP 워치독 — unity-code-mcp-stdio 자동 재시작
# 실행: powershell -ExecutionPolicy Bypass -File mcp_watchdog.ps1
# 종료: Ctrl+C
# ============================================

Write-Host "🐕 MCP 워치독 시작 (Ctrl+C로 종료)" -ForegroundColor Cyan
Write-Host "   unity-code-mcp-stdio 다운 시 자동 재시작합니다." -ForegroundColor Gray
Write-Host ""

$checkInterval = 5   # 체크 주기 (초)
$restartCount  = 0

while ($true) {
    $proc = Get-Process -Name "unity-code-mcp-stdio" -ErrorAction SilentlyContinue

    if ($null -eq $proc) {
        $restartCount++
        $time = Get-Date -Format "HH:mm:ss"
        Write-Host "[$time] ⚠️  MCP 서버 없음 — Unity에서 Tools > UnityCodeMcpServer > STDIO 로 재시작해 주세요. (감지 #$restartCount)" -ForegroundColor Yellow
    } else {
        # 프로세스가 살아있음 (정상)
        # Write-Host "[$time] ✅ MCP 정상 (PID=$($proc.Id))" -ForegroundColor Green  # 조용히 유지
    }

    Start-Sleep -Seconds $checkInterval
}
