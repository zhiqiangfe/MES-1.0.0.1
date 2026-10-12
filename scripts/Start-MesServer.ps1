param(
    [int]$Port = 5180
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$project = Join-Path $root 'src\HTHIUM.Mes.Api\HTHIUM.Mes.Api.csproj'
$runtimeDirectory = Join-Path $root '.runtime'
$logDirectory = Join-Path $root 'logs'
$pidFile = Join-Path $runtimeDirectory 'mes-api.pid'
$stdoutLog = Join-Path $logDirectory 'mes-launcher-output.log'
$stderrLog = Join-Path $logDirectory 'mes-launcher-error.log'

if (-not (Test-Path -LiteralPath $project))
{
    throw "MES project file was not found: $project"
}

$listener = Get-NetTCPConnection -LocalPort $Port -State Listen -ErrorAction SilentlyContinue |
    Select-Object -First 1
if ($listener)
{
    Write-Host "[MES] Server is already running. Port=$Port PID=$($listener.OwningProcess)"
    Write-Host "[MES] Address: http://127.0.0.1:$Port"
    exit 2
}

New-Item -ItemType Directory -Force -Path $runtimeDirectory, $logDirectory | Out-Null

$arguments = @(
    'run',
    '--project',
    "`"$project`"",
    '--launch-profile',
    'http'
)

$process = Start-Process `
    -FilePath 'dotnet' `
    -ArgumentList $arguments `
    -WorkingDirectory $root `
    -WindowStyle Hidden `
    -RedirectStandardOutput $stdoutLog `
    -RedirectStandardError $stderrLog `
    -PassThru

$process.Id | Set-Content -LiteralPath $pidFile -Encoding ascii

for ($attempt = 0; $attempt -lt 40; $attempt++)
{
    Start-Sleep -Milliseconds 500
    $process.Refresh()
    if ($process.HasExited)
    {
        Write-Host "[MES] Server failed to start. ExitCode=$($process.ExitCode)" -ForegroundColor Red
        Write-Host "[MES] Check log: $stderrLog"
        exit 1
    }

    $listener = Get-NetTCPConnection -LocalPort $Port -State Listen -ErrorAction SilentlyContinue |
        Select-Object -First 1
    if ($listener)
    {
        Write-Host '[MES] Server started in the background.' -ForegroundColor Green
        Write-Host "[MES] Address: http://127.0.0.1:$Port"
        Write-Host "[MES] Process PID: $($listener.OwningProcess)"
        Write-Host "[MES] API logs: $logDirectory"
        Write-Host '[MES] This launcher window can now be closed safely.'
        exit 0
    }
}

Write-Host '[MES] Startup timed out. Check launcher logs.' -ForegroundColor Red
Write-Host "[MES] Output: $stdoutLog"
Write-Host "[MES] Error:  $stderrLog"
exit 1
