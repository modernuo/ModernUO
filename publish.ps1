$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $MyInvocation.MyCommand.Path

$toolBinary = Join-Path (Join-Path $repoRoot 'tools') 'build-tool.exe'
$buildToolProject = Join-Path (Join-Path (Join-Path $repoRoot 'Projects') 'BuildTool') 'BuildTool.csproj'

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

# Try native binary first
if (Test-Path $toolBinary) {
    & $toolBinary @args
    exit $LASTEXITCODE
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
