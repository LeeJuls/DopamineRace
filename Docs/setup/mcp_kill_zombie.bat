@echo off
echo Killing MCP zombie processes...
powershell -Command "Get-Process unity-code-mcp-stdio -ErrorAction SilentlyContinue | Stop-Process -Force"
echo Done. Please restart MCP server in Unity:
echo   Tools > UnityCodeMcpServer > STDIO > Restart Server
pause
