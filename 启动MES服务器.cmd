@echo off
setlocal
chcp 65001 >nul

powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0scripts\Start-MesServer.ps1"
set "RESULT=%ERRORLEVEL%"
echo.
if "%RESULT%"=="0" echo [MES] The API is running in the background. Closing this window will not stop it.
pause
exit /b %RESULT%

