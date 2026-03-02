@echo off
echo MCP 좀비 프로세스 정리 중...
powershell -Command "Get-Process unity-code-mcp-stdio -ErrorAction SilentlyContinue | Stop-Process -Force"
echo 완료! Unity에서 MCP 서버를 다시 시작하세요.
echo (Tools > UnityCodeMcpServer > STDIO)
pause
