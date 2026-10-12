@echo off
setlocal
chcp 65001 >nul

set "MES_PORT=5180"
set "MES_PROJECT=%~dp0src\HTHIUM.Mes.Api\HTHIUM.Mes.Api.csproj"

if not exist "%MES_PROJECT%" (
    echo [MES] Project file was not found:
    echo %MES_PROJECT%
    pause
    exit /b 1
)

powershell.exe -NoProfile -Command ^
  "$listener = Get-NetTCPConnection -LocalPort %MES_PORT% -State Listen -ErrorAction SilentlyContinue | Select-Object -First 1; if ($listener) { Write-Host ('[MES] Port %MES_PORT% is already in use. PID: ' + $listener.OwningProcess); exit 2 }"

if errorlevel 2 (
    echo [MES] The server may already be running. Open http://127.0.0.1:%MES_PORT% to check it.
    pause
    exit /b 2
)

echo [MES] Starting server...
echo [MES] Address: http://127.0.0.1:%MES_PORT%
echo [MES] Press Ctrl+C in this window to stop it.
echo.

dotnet run --project "%MES_PROJECT%"

echo.
echo [MES] The server has stopped.
pause

