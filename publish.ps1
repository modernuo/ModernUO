$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $MyInvocation.MyCommand.Path

$toolBinary = Join-Path (Join-Path $repoRoot 'tools') 'build-tool.exe'
$stampFile = Join-Path (Join-Path $repoRoot 'tools') '.build-tool-commit'
$buildToolProject = Join-Path (Join-Path (Join-Path $repoRoot 'Projects') 'BuildTool') 'BuildTool.csproj'

function Get-BuildToolCommit {
    try {
        $hash = & git -C $repoRoot log -1 --format=%H -- Projects/BuildTool/ 2>$null
        if ($LASTEXITCODE -eq 0 -and $hash) { return $hash.Trim() }
    }
    catch { }
    return $null
}

function Get-BuildToolFromRelease {
    try {
        # Determine platform
        $arch = if ([System.Runtime.InteropServices.RuntimeInformation]::ProcessArchitecture -eq 'Arm64') { 'arm64' } else { 'x64' }
        $assetName = "build-tool-win-$arch.exe"

        Write-Host "Downloading build tool..." -ForegroundColor Blue

        $releaseUrl = 'https://api.github.com/repos/modernuo/ModernUO/releases/tags/build-tool-latest'
        $headers = @{ 'User-Agent' = 'ModernUO-BuildTool' }
        $release = Invoke-RestMethod -Uri $releaseUrl -Headers $headers -TimeoutSec 10

        $asset = $release.assets | Where-Object { $_.name -eq $assetName } | Select-Object -First 1
        if (-not $asset) {
            return $false
        }

        $toolsDir = Join-Path $repoRoot 'tools'
        if (-not (Test-Path $toolsDir)) {
            New-Item -ItemType Directory -Path $toolsDir -Force | Out-Null
        }

        Invoke-WebRequest -Uri $asset.browser_download_url -OutFile $toolBinary -TimeoutSec 60

        $commitHash = Get-BuildToolCommit
        if ($null -ne $commitHash) {
            Set-Content -Path $stampFile -Value $commitHash -NoNewline
        }

        return $true
    }
    catch {
        return $false
    }
}

function Test-DotNetAvailable {
    try {
        $null = & dotnet --version 2>$null
        return $LASTEXITCODE -eq 0
    }
    catch {
        return $false
    }
}

# Try native binary first (with staleness check)
if (Test-Path $toolBinary) {
    $currentCommit = Get-BuildToolCommit
    if ($null -ne $currentCommit -and (Test-Path $stampFile)) {
        $storedCommit = (Get-Content $stampFile -Raw).Trim()
        if ($currentCommit -eq $storedCommit) {
            & $toolBinary @args
            exit $LASTEXITCODE
        }
        else {
            Write-Host "Build tool source has changed, updating..." -ForegroundColor Yellow
            Remove-Item $toolBinary -Force
        }
    }
    elseif ($null -eq $currentCommit) {
        # Not in a git repo — use binary as-is
        & $toolBinary @args
        exit $LASTEXITCODE
    }
    else {
        # Binary exists but no stamp — re-download to establish tracking
        Remove-Item $toolBinary -Force
    }
}

# Try to download native binary
if (Get-BuildToolFromRelease) {
    & $toolBinary @args
    exit $LASTEXITCODE
}

# Fall back to dotnet run
if (Test-DotNetAvailable) {
    if (Test-Path $buildToolProject) {
        Write-Host "Using dotnet run fallback..." -ForegroundColor Yellow
        & dotnet run --project $buildToolProject -- @args
        exit $LASTEXITCODE
    }
    else {
        Write-Error "BuildTool project not found at: $buildToolProject"
        exit 1
    }
}

# Nothing works
Write-Host ""
Write-Host "Error: Could not run the build tool." -ForegroundColor Red
Write-Host ""
Write-Host "The .NET 10 SDK is required. Download it from:" -ForegroundColor Yellow
Write-Host "  https://dotnet.microsoft.com/download/dotnet/10.0" -ForegroundColor Cyan
Write-Host ""
exit 1
