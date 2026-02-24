@echo off
echo ========================================
echo   DopamineRace 환경 자동 세팅
echo ========================================
echo.
echo PowerShell 스크립트를 실행합니다...
echo.
powershell -ExecutionPolicy Bypass -File "%~dp0auto_setup.ps1"
pause
