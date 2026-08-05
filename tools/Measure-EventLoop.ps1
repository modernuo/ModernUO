# A/B measurement for the event loop scheduler.
#
# Boots the shard twice against identical binaries -- once with idle sleeping disabled
# (server.eventLoopIdleWaitMs = 0) and once with idle sleeping (= 2) -- and samples process CPU
# time over a fixed window. Everything else is held constant, so the delta is the scheduler.
#
# Usage:  pwsh tools/Measure-EventLoop.ps1 [-WarmupSeconds 45] [-SampleSeconds 60]

param(
    [int]$WarmupSeconds = 45,
    [int]$SampleSeconds = 60
)

$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $PSScriptRoot
$dist = Join-Path $root 'Distribution'
$exe = Join-Path $dist 'ModernUO.exe'
$configPath = Join-Path $dist 'Configuration\modernuo.json'

if (-not (Test-Path $exe)) {
    throw "ModernUO.exe not found at $exe. Build with: dotnet build -c Release"
}

function Set-IdleWait([int]$value) {
    $json = Get-Content $configPath -Raw | ConvertFrom-Json
    $json.settings.'server.eventLoopIdleWaitMs' = "$value"
    $json | ConvertTo-Json -Depth 20 | Set-Content $configPath -Encoding UTF8
}

function Measure-Loop([int]$idleWait, [string]$label) {
    Set-IdleWait $idleWait

    Write-Host ""
    Write-Host "=== $label (server.eventLoopIdleWaitMs = $idleWait) ===" -ForegroundColor Cyan

    # Redirect stdin from an empty file so the server runs headless and never blocks on console
    # input. PowerShell cannot redirect from NUL, so an actual empty file stands in for it.
    $stdin = Join-Path $dist 'empty.in'
    if (-not (Test-Path $stdin)) {
        Set-Content -Path $stdin -Value '' -NoNewline
    }

    $proc = Start-Process -FilePath $exe -WorkingDirectory $dist -PassThru `
        -RedirectStandardInput $stdin `
        -RedirectStandardOutput (Join-Path $dist "Logs\measure-$idleWait.out") `
        -RedirectStandardError  (Join-Path $dist "Logs\measure-$idleWait.err")

    try {
        Write-Host "  pid $($proc.Id); warming up for ${WarmupSeconds}s..."
        Start-Sleep -Seconds $WarmupSeconds

        if ($proc.HasExited) {
            throw "Server exited during warmup (code $($proc.ExitCode)). See Logs\measure-$idleWait.err"
        }

        $proc.Refresh()
        $cpuBefore = $proc.TotalProcessorTime
        $wallBefore = Get-Date

        Write-Host "  sampling for ${SampleSeconds}s..."
        Start-Sleep -Seconds $SampleSeconds

        $proc.Refresh()
        $cpuAfter = $proc.TotalProcessorTime
        $wallAfter = Get-Date

        $cpuMs = ($cpuAfter - $cpuBefore).TotalMilliseconds
        $wallMs = ($wallAfter - $wallBefore).TotalMilliseconds
        $pct = $cpuMs / $wallMs * 100

        Write-Host ("  CPU: {0:F2}% of one core  ({1:F0}ms CPU over {2:F0}ms wall)" -f $pct, $cpuMs, $wallMs) -ForegroundColor Yellow

        [pscustomobject]@{ Label = $label; IdleWait = $idleWait; CpuPercent = $pct }
    }
    finally {
        if (-not $proc.HasExited) {
            $proc.Kill()
            $proc.WaitForExit(10000) | Out-Null
        }
    }
}

$legacy = Measure-Loop 0 'Never sleep (max responsiveness)'
$sleeping = Measure-Loop 2 'Idle sleeping'

Write-Host ""
Write-Host "=== Result ===" -ForegroundColor Green
Write-Host ("  never sleep : {0,6:F2}% of one core" -f $legacy.CpuPercent)
Write-Host ("  idle sleep  : {0,6:F2}% of one core" -f $sleeping.CpuPercent)
if ($sleeping.CpuPercent -gt 0) {
    Write-Host ("  reduction   : {0,6:F1}x" -f ($legacy.CpuPercent / $sleeping.CpuPercent))
}

# Leave the config on the new behaviour.
Set-IdleWait 2
