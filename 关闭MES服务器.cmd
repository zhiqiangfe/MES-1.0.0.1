@echo off
setlocal
chcp 65001 >nul

set "MES_PORT=5180"

powershell.exe -NoProfile -ExecutionPolicy Bypass -Command ^
  "$listeners = @(Get-NetTCPConnection -LocalPort %MES_PORT% -State Listen -ErrorAction SilentlyContinue | Sort-Object OwningProcess -Unique);" ^
  "if ($listeners.Count -eq 0) { Write-Host '[MES] No server is listening on port %MES_PORT%.'; exit 3 };" ^
  "$processIds = @($listeners | ForEach-Object OwningProcess);" ^
  "$processes = @($processIds | ForEach-Object { Get-Process -Id $_ -ErrorAction SilentlyContinue });" ^
  "Write-Host '[MES] Server process found:';" ^
  "$processes | Format-Table Id, ProcessName, Path -AutoSize;" ^
  "$answer = Read-Host 'Stop the process listening on port %MES_PORT%? Enter Y to confirm';" ^
  "if ($answer -notmatch '^[Yy]$') { Write-Host '[MES] Cancelled.'; exit 4 };" ^
  "$processIds | ForEach-Object { Stop-Process -Id $_ -ErrorAction Stop };" ^
  "Write-Host '[MES] Server stopped.'"

set "RESULT=%ERRORLEVEL%"
echo.
if "%RESULT%"=="3" echo [MES] Nothing needs to be stopped.
if "%RESULT%"=="4" echo [MES] Stop operation was cancelled.
pause
exit /b %RESULT%

