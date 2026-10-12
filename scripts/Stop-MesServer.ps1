param(
    [int]$Port = 5180,
    [switch]$Force
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$pidFile = Join-Path $root '.runtime\mes-api.pid'
$processIds = [System.Collections.Generic.HashSet[int]]::new()

$listeners = @(Get-NetTCPConnection -LocalPort $Port -State Listen -ErrorAction SilentlyContinue)
foreach ($listener in $listeners)
{
    [void]$processIds.Add([int]$listener.OwningProcess)
}

if (Test-Path -LiteralPath $pidFile)
{
    $savedProcessId = 0
    if ([int]::TryParse((Get-Content -LiteralPath $pidFile -Raw).Trim(), [ref]$savedProcessId) -and
        (Get-Process -Id $savedProcessId -ErrorAction SilentlyContinue))
    {
        [void]$processIds.Add($savedProcessId)
    }
}

if ($processIds.Count -eq 0)
{
    Remove-Item -LiteralPath $pidFile -Force -ErrorAction SilentlyContinue
    Write-Host "[MES] No server is listening on port $Port."
    exit 3
}

Write-Host '[MES] Server process found:'
foreach ($processId in $processIds)
{
    Get-Process -Id $processId -ErrorAction SilentlyContinue |
        Select-Object Id, ProcessName, Path |
        Format-Table -AutoSize
}

if (-not $Force)
{
    $answer = Read-Host "Stop the MES server on port $Port? Enter Y to confirm"
    if ($answer -notmatch '^[Yy]$')
    {
        Write-Host '[MES] Cancelled.'
        exit 4
    }
}

foreach ($processId in $processIds)
{
    & taskkill.exe /PID $processId /T /F 2>$null | Out-Null
}

Remove-Item -LiteralPath $pidFile -Force -ErrorAction SilentlyContinue
Start-Sleep -Milliseconds 500

if (Get-NetTCPConnection -LocalPort $Port -State Listen -ErrorAction SilentlyContinue)
{
    Write-Host '[MES] A process is still listening on the server port.' -ForegroundColor Red
    exit 1
}

Write-Host '[MES] Server stopped.' -ForegroundColor Green
