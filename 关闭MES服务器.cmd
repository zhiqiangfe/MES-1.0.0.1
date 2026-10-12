@echo off
setlocal
chcp 65001 >nul

powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0scripts\Stop-MesServer.ps1"
set "RESULT=%ERRORLEVEL%"
echo.
if "%RESULT%"=="3" echo [MES] Nothing needs to be stopped.
if "%RESULT%"=="4" echo [MES] Stop operation was cancelled.
pause
exit /b %RESULT%

